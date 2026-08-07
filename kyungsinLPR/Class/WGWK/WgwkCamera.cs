using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace KyungsinLPR.WGWK
{
    /// <summary>
    /// WGWK-A05D IP 카메라 래퍼 (AJNetSDK HTTP API).
    /// 네이티브 SDK(libNetSdk/AjPlayer)는 x86 전용이라 x64 빌드와 충돌하므로 사용하지 않고,
    /// HTTP API 'GET /HAPI/V1.0/snapshot.cgi' 로 JPEG 스냅샷을 받아 IPCamera/USBCamera와
    /// 동일한 시그니처(ConnectStreamPort/GetImage/SaveLastImage 등)로 노출한다.
    /// 비밀번호는 MD5 16진수로 전송한다(예: 123456 -> e10adc3949ba59abbe56e057f20f883e).
    /// </summary>
    public class WgwkCamera
    {
        private string _ip;
        private int _port = 80;
        private string _user = "admin";
        private string _passMd5 = "";
        private string _passRaw = "123456";  // RTSP URL용 평문 비번 (HTTP는 MD5)
        private int _stream = 1;          // 0=보조(sub), 1=메인(main)

        // RTSP 고해상도 캡처(ffmpeg) — 인식 이미지(SaveLastImage)만 메인스트림 1080p로.
        //  snapshot.cgi 는 D1(704x576) 고정이라, 차번 인식용 고화질은 RTSP 메인에서 ffmpeg로 1프레임 취득.
        //  ffmpeg.exe 가 없으면 자동으로 snapshot(D1) 폴백 → 회귀 없음.
        private const int RtspPort = 554;
        private static string _ffmpegPath;       // 1회 탐색 후 캐시 ("" = 없음)
        private static bool _ffmpegResolved = false;

        // 상시 RTSP 디코딩(persistent) — ffmpeg 1개 상주, 최신 프레임을 _liveFile에 계속 갱신.
        //  카메라 GOP가 최소 25(1초)라 콜드그랩은 ~1.4초 한계 → 스트림을 켜두고 캡처 시 즉시 복사.
        private readonly object _liveLock = new object();
        private Process _liveProc;
        private string _liveFile;   // ASCII 경로의 최신 프레임 파일 (wgwk_live_{ip}.jpg)

        private readonly object _frameLock = new object();
        private byte[] _latestJpeg;       // 최신 스냅샷 원본 JPEG 바이트 (SaveLastImage는 무손실로 그대로 저장)
        private DateTime _lastFrameTime = DateTime.MinValue;

        private volatile bool _connected = false;
        private long _frameCount = 0;

        public string IP { get { return _ip; } }

        /// <summary>장치 초기화 — 접속 정보 저장. 실제 연결은 ConnectStreamPort() 호출.</summary>
        public void Init(string ip, int port, string user, string pass, int stream)
        {
            _ip = (ip ?? "").Trim();
            _port = (port > 0) ? port : 80;
            _user = string.IsNullOrWhiteSpace(user) ? "admin" : user.Trim();
            _passRaw = string.IsNullOrEmpty(pass) ? "123456" : pass;
            _passMd5 = Md5Hex(_passRaw);
            _stream = (stream == 0) ? 0 : 1;
        }

        private static string Md5Hex(string text)
        {
            try
            {
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(text ?? ""));
                    var sb = new StringBuilder(hash.Length * 2);
                    foreach (byte b in hash) sb.Append(b.ToString("x2"));
                    return sb.ToString();
                }
            }
            catch { return ""; }
        }

        private string SnapshotUrl()
        {
            return string.Format(
                "http://{0}:{1}/HAPI/V1.0/snapshot.cgi?stream={2}&username={3}&password={4}",
                _ip, _port, _stream, _user, _passMd5);
        }

        /// <summary>IPCamera.ConnectStreamPort() 와 동일한 역할 — 첫 스냅샷으로 연결 확인.</summary>
        public bool ConnectStreamPort()
        {
            if (string.IsNullOrWhiteSpace(_ip))
            {
                Util.Logger.Log("[WgwkCamera.ConnectStreamPort] IP 비어있음 — 연결 스킵");
                _connected = false;
                return false;
            }
            try
            {
                byte[] jpeg = FetchSnapshot(3000);
                if (jpeg != null && jpeg.Length > 0)
                {
                    StoreJpeg(jpeg);
                    _connected = true;
                    Util.Logger.Log(string.Format("[WgwkCamera] 연결 OK — {0}:{1} stream={2} ({3} bytes)",
                        _ip, _port, _stream, jpeg.Length));
                    StartLiveDecode();   // 상시 RTSP 디코딩 워밍업 (캡처 즉시화)
                    return true;
                }
                _connected = false;
                Util.Logger.Log(string.Format("[WgwkCamera] 연결 실패 — {0}:{1} 스냅샷 응답 없음", _ip, _port));
                return false;
            }
            catch (Exception ex)
            {
                _connected = false;
                Util.Logger.Log("[WgwkCamera.ConnectStreamPort] " + ex.Message);
                return false;
            }
        }

        public bool IsStreamPortConnected() { return _connected; }

        /// <summary>WGWK는 HTTP 무상태라 커맨드 포트 개념이 없음 — 스트림과 동일 취급.</summary>
        public bool IsCommandPortConnected() { return _connected; }

        public void DisconnectStreamPort()
        {
            _connected = false;
            StopLiveDecode();
            lock (_frameLock)
            {
                _latestJpeg = null;
            }
        }

        /// <summary>snapshot.cgi GET → JPEG 바이트. 실패 시 null.</summary>
        private byte[] FetchSnapshot(int timeoutMs)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(SnapshotUrl());
            req.Method = "GET";
            req.Timeout = timeoutMs;
            req.ReadWriteTimeout = timeoutMs;
            req.KeepAlive = true;
            req.AllowAutoRedirect = false;
            req.UserAgent = "KukjeLPR";

            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var rs = resp.GetResponseStream())
            using (var ms = new MemoryStream())
            {
                if (rs == null) return null;
                byte[] buf = new byte[16384];
                int n;
                while ((n = rs.Read(buf, 0, buf.Length)) > 0)
                    ms.Write(buf, 0, n);
                return ms.ToArray();
            }
        }

        private void StoreJpeg(byte[] jpeg)
        {
            lock (_frameLock)
            {
                _latestJpeg = jpeg;
                _lastFrameTime = DateTime.Now;
            }
            Interlocked.Increment(ref _frameCount);
        }

        /// <summary>
        /// IPCamera.GetImage(timeout, out Bitmap) 와 동일한 역할.
        /// snapshot.cgi 를 GET 하여 JPEG → Bitmap 디코딩 후 반환. 원본 JPEG 바이트는 SaveLastImage용으로 캐시.
        /// </summary>
        public IPCamError GetImage(int timeoutMs, out Bitmap bitmap)
        {
            bitmap = null;
            if (string.IsNullOrWhiteSpace(_ip))
                return IPCamError.StreamNotOpened;

            try
            {
                byte[] jpeg = FetchSnapshot(timeoutMs > 0 ? timeoutMs : 1000);
                if (jpeg == null || jpeg.Length == 0)
                    return IPCamError.Timeout;

                StoreJpeg(jpeg);
                _connected = true;

                // 바이트를 복사한 MemoryStream으로 Bitmap 생성 (스트림을 살려둬야 하므로 클론 반환)
                using (var ms = new MemoryStream(jpeg))
                using (var tmp = new Bitmap(ms))
                {
                    bitmap = new Bitmap(tmp);
                }
                return IPCamError.OK;
            }
            catch (WebException)
            {
                _connected = false;
                return IPCamError.Timeout;
            }
            catch (Exception ex)
            {
                Util.Logger.Log("[WgwkCamera.GetImage] " + ex.Message);
                return IPCamError.Timeout;
            }
        }

        /// <summary>
        /// IPCamera.SaveLastImage 와 동일 — 인식용 이미지 저장.
        /// 1) RTSP 메인스트림(ffmpeg)으로 고해상도(1080p) 1프레임 시도 → 차번 인식 화질↑
        /// 2) ffmpeg 없거나 실패하면 최신 snapshot(D1) JPEG 그대로 저장(폴백).
        /// </summary>
        public bool SaveLastImage(string path)
        {
            // 1) 상시 디코딩 최신 프레임 복사 (즉시, 키프레임 대기 없음)
            if (TryCopyLive(path)) return true;
            // 2) 폴백: 콜드 RTSP 그랩 (상시 프레임 아직 없을 때)
            if (TryCaptureRtsp(path)) return true;
            // 3) 폴백: 캐시된 snapshot(D1) 저장
            try
            {
                byte[] jpeg = null;
                lock (_frameLock)
                {
                    if (_latestJpeg != null)
                        jpeg = (byte[])_latestJpeg.Clone();
                }
                if (jpeg == null) return false;

                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string ext = Path.GetExtension(path);
                // 원본이 JPEG이므로 .jpg면 바이트 그대로 저장(무손실/고속). 그 외 확장자는 디코딩 후 변환.
                if (string.IsNullOrEmpty(ext) || ext.ToLowerInvariant() == ".jpg" || ext.ToLowerInvariant() == ".jpeg")
                {
                    File.WriteAllBytes(path, jpeg);
                    return true;
                }

                using (var ms = new MemoryStream(jpeg))
                using (var bmp = new Bitmap(ms))
                {
                    var fmt = System.Drawing.Imaging.ImageFormat.Jpeg;
                    string e = ext.ToLowerInvariant();
                    if (e == ".bmp") fmt = System.Drawing.Imaging.ImageFormat.Bmp;
                    else if (e == ".png") fmt = System.Drawing.Imaging.ImageFormat.Png;
                    bmp.Save(path, fmt);
                }
                return true;
            }
            catch (Exception ex)
            {
                Util.Logger.Log("[WgwkCamera.SaveLastImage] " + ex.Message);
                return false;
            }
        }

        /// <summary>RTSP 메인스트림(ffmpeg)으로 고해상도 1프레임 캡처 → path 저장. ffmpeg 없거나 실패하면 false(폴백).</summary>
        private bool TryCaptureRtsp(string path)
        {
            try
            {
                string ff = ResolveFfmpeg();
                if (string.IsNullOrEmpty(ff)) return false;        // ffmpeg 없음 → snapshot 폴백
                if (string.IsNullOrWhiteSpace(_ip)) return false;

                // ffmpeg는 한글/공백 경로에 약해 ASCII 임시폴더에 쓰고, .NET으로 최종 경로 복사
                string tmp = Path.Combine(Path.GetTempPath(), "wgwk_cap_" + Guid.NewGuid().ToString("N") + ".jpg");
                string rtsp = RtspMainUrl();
                // 저지연 옵션: cold-grab ~0.4초 (probesize/analyzeduration 최소화 + nobuffer/low_delay)
                string args = "-rtsp_transport tcp -fflags nobuffer -flags low_delay -probesize 32 -analyzeduration 0 "
                            + "-i \"" + rtsp + "\" -frames:v 1 -q:v 3 -y \"" + tmp + "\"";

                var psi = new ProcessStartInfo
                {
                    FileName = ff,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                };
                bool ok = false;
                using (var p = Process.Start(psi))
                {
                    if (p == null) return false;
                    p.BeginErrorReadLine();   // stderr/stdout 드레인(파이프 막힘 방지)
                    p.BeginOutputReadLine();
                    if (!p.WaitForExit(4000))
                    {
                        try { p.Kill(); } catch { }
                        Util.Logger.Log("[WgwkCamera.RTSP] ffmpeg 타임아웃 — snapshot 폴백");
                        ok = false;
                    }
                    else ok = (p.ExitCode == 0);
                }

                if (ok && File.Exists(tmp) && new FileInfo(tmp).Length > 0)
                {
                    string dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.Copy(tmp, path, true);   // .NET 복사 = 한글 경로 안전
                    try { File.Delete(tmp); } catch { }
                    return true;
                }
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                return false;
            }
            catch (Exception ex)
            {
                Util.Logger.Log("[WgwkCamera.TryCaptureRtsp] " + ex.Message + " — snapshot 폴백");
                return false;
            }
        }

        /// <summary>상시 RTSP 디코딩 ffmpeg 시작(없거나 죽었으면 (재)시작). 최신 프레임을 _liveFile에 계속 갱신.</summary>
        private void StartLiveDecode()
        {
            lock (_liveLock)
            {
                try
                {
                    if (_liveProc != null && !_liveProc.HasExited) return;   // 이미 정상 실행 중
                    string ff = ResolveFfmpeg();
                    if (string.IsNullOrEmpty(ff) || string.IsNullOrWhiteSpace(_ip)) return;

                    // 최신 프레임 파일 = ffmpeg.exe 폴더(운영 ASCII 경로)에 wgwk_live_{ip}.jpg
                    string baseDir = Path.GetDirectoryName(ff);
                    string safeIp = (_ip ?? "").Replace(".", "_").Replace(":", "_");
                    _liveFile = Path.Combine(baseDir, "wgwk_live_" + safeIp + ".jpg");
                    try { if (File.Exists(_liveFile)) File.Delete(_liveFile); } catch { }

                    string rtsp = RtspMainUrl();
                    // 입력 RTSP 상시 디코딩, 출력은 초당 5장 같은 파일에 덮어씀(-update 1)
                    string args = "-rtsp_transport tcp -fflags nobuffer -flags low_delay "
                                + "-i \"" + rtsp + "\" -an -r 5 -q:v 3 -update 1 -y \"" + _liveFile + "\"";
                    var psi = new ProcessStartInfo
                    {
                        FileName = ff, Arguments = args,
                        UseShellExecute = false, CreateNoWindow = true,
                        RedirectStandardError = true, RedirectStandardOutput = true,
                    };
                    _liveProc = Process.Start(psi);
                    if (_liveProc != null) { _liveProc.BeginErrorReadLine(); _liveProc.BeginOutputReadLine(); }
                    Util.Logger.Log("[WgwkCamera] 상시 RTSP 디코딩 시작 → " + _liveFile);
                }
                catch (Exception ex) { Util.Logger.Log("[WgwkCamera.StartLiveDecode] " + ex.Message); _liveProc = null; }
            }
        }

        /// <summary>상시 디코딩 ffmpeg 종료.</summary>
        private void StopLiveDecode()
        {
            lock (_liveLock)
            {
                try { if (_liveProc != null && !_liveProc.HasExited) _liveProc.Kill(); } catch { }
                _liveProc = null;
            }
        }

        /// <summary>상시 디코딩 최신 프레임을 path로 복사. 프레임 없거나 ffmpeg 없으면 false(폴백).</summary>
        private bool TryCopyLive(string path)
        {
            try
            {
                StartLiveDecode();   // 없거나 죽었으면 (재)시작
                string live;
                lock (_liveLock) { live = _liveFile; if (_liveProc == null) return false; }
                if (string.IsNullOrEmpty(live)) return false;

                // 최신 프레임 파일이 신선해질 때까지 최대 ~2초 대기(최초 워밍업 대비). 평상시엔 즉시 통과.
                for (int i = 0; i < 40; i++)
                {
                    if (File.Exists(live))
                    {
                        var fi = new FileInfo(live);
                        if (fi.Length > 1000 && (DateTime.Now - fi.LastWriteTime).TotalSeconds < 3)
                        {
                            if (CopyAndValidateJpeg(live, path)) return true;   // 쓰기중 부분복사 방지(검증+재시도)
                        }
                    }
                    Thread.Sleep(50);
                }
                return false;
            }
            catch (Exception ex) { Util.Logger.Log("[WgwkCamera.TryCopyLive] " + ex.Message); return false; }
        }

        /// <summary>src JPEG를 메모리로 읽어 유효성(SOI/EOI) 확인 후 dst 기록. 부분쓰기면 재시도.</summary>
        private static bool CopyAndValidateJpeg(string src, string dst)
        {
            for (int r = 0; r < 5; r++)
            {
                try
                {
                    byte[] b;
                    using (var fs = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        b = new byte[fs.Length];
                        int off = 0, n;
                        while (off < b.Length && (n = fs.Read(b, off, b.Length - off)) > 0) off += n;
                    }
                    if (b.Length > 1000 && b[0] == 0xFF && b[1] == 0xD8 && b[b.Length - 2] == 0xFF && b[b.Length - 1] == 0xD9)
                    {
                        string dir = Path.GetDirectoryName(dst);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        File.WriteAllBytes(dst, b);
                        return true;
                    }
                }
                catch { }
                Thread.Sleep(60);
            }
            return false;
        }

        /// <summary>고아 상시-디코딩 ffmpeg(wgwk_live) 정리 — LPR 시작 시 호출(이전 세션 잔재가 소켓 상속·점유 방지).</summary>
        public static void KillStaleLiveDecoders()
        {
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='ffmpeg.exe'"))
                {
                    foreach (System.Management.ManagementObject mo in searcher.Get())
                    {
                        string cmd = (mo["CommandLine"] ?? "").ToString();
                        if (cmd.IndexOf("wgwk_live", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int pid = Convert.ToInt32(mo["ProcessId"]);
                            try { Process.GetProcessById(pid).Kill(); Util.Logger.Log("[WgwkCamera] 고아 상시디코딩 ffmpeg 정리(PID " + pid + ")"); }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex) { Util.Logger.Log("[WgwkCamera] 고아 ffmpeg 정리 오류: " + ex.Message); }
        }

        /// <summary>RTSP 메인스트림 URL (rtsp://user:pass@ip:554/stream0). 특수문자 대비 URL 인코딩.</summary>
        private string RtspMainUrl()
        {
            string u = Uri.EscapeDataString(_user ?? "admin");
            string pw = Uri.EscapeDataString(_passRaw ?? "123456");
            return "rtsp://" + u + ":" + pw + "@" + _ip + ":" + RtspPort + "/stream0";
        }

        /// <summary>ffmpeg.exe 위치 1회 탐색(앱 폴더/현재 폴더). 없으면 "".</summary>
        private static string ResolveFfmpeg()
        {
            if (_ffmpegResolved) return _ffmpegPath;
            _ffmpegResolved = true;
            _ffmpegPath = "";
            try
            {
                string[] cands = {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? "", "ffmpeg.exe"),
                    Path.Combine(Directory.GetCurrentDirectory(), "ffmpeg.exe"),
                };
                foreach (var c in cands)
                    if (!string.IsNullOrEmpty(c) && File.Exists(c)) { _ffmpegPath = c; break; }

                if (string.IsNullOrEmpty(_ffmpegPath))
                    Util.Logger.Log("[WgwkCamera] ffmpeg.exe 없음 — RTSP 고해상도 캡처 비활성(snapshot D1 사용). 고화질 원하면 LPR 폴더에 ffmpeg.exe 배치.");
                else
                    Util.Logger.Log("[WgwkCamera] ffmpeg 사용: " + _ffmpegPath + " — RTSP 메인 고해상도 캡처");
            }
            catch { _ffmpegPath = ""; }
            return _ffmpegPath;
        }

        /// <summary>마지막 프레임 도착 시각 (Watchdog용).</summary>
        public DateTime LastFrameTime
        {
            get { lock (_frameLock) { return _lastFrameTime; } }
        }

        public long FrameCount { get { return Interlocked.Read(ref _frameCount); } }

        /// <summary>호환용 stub — IPCamera 시그니처 맞추기. WGWK는 재연결로 대체.</summary>
        public void ResetCamera() { /* HTTP 무상태 — 별도 리셋 없음 */ }
    }
}
