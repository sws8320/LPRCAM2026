using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;   // JavaScriptSerializer (System.Web.Extensions, .NET 내장)

namespace KyungsinLPR
{
    /// <summary>
    /// Option(K) — 자체 학습 CRNN 인식 모듈 (Python 사이드카 방식).
    /// 검출(EasyOCR)+인식(CRNN) 을 로컬 Python 서비스(optionk_server.py)로 돌리고,
    /// C# 는 캡처 이미지 절대경로 + ROI 를 HTTP 로 보내 번호판을 받는다.
    ///
    /// GPU/CPU 는 환경설정의 CPU/GPU 선택(CameraEnv.CoreType)을 따른다.
    /// 사이드카 장치가 설정과 다르면 자동으로 재시작한다.
    /// 서버 파일: 실행파일 옆 OptionK_server\optionk_server.py
    /// </summary>
    public static class OptionK
    {
        private static readonly object _lock = new object();
        private static bool _initialized = false;
        private static string _url = "http://127.0.0.1:8765";
        private static string _serverDir = "";
        private static string _pythonExe = "python";
        private static bool _autoStart = true;
        private static Process _proc = null;
        private static string _device = "";          // 현재 사이드카 장치("cuda"/"cpu"/"remote")
        private static readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        // ---- 원격(ParkingWeb 서버) 인식 모드 ----
        // LPR PC에 GPU가 없을 때, 캡처 이미지를 서버(ParkingWeb)로 보내 차번을 받는다.
        // Setting.ini [OPTIONK] remote/serverurl/apikey (url/apikey 빈값이면 [UPLOAD] 값 재사용).
        private static bool _remote = false;
        private static string _remoteUrl = "";
        private static string _remoteApiKey = "";
        private static readonly HttpClient _rhttp = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        private static void LoadRemoteConfig()
        {
            try
            {
                string r = Util.Function.IniReadValue("OPTIONK", "remote") ?? "";
                _remote = r.Equals("true", StringComparison.OrdinalIgnoreCase) || r == "1";
                _remoteUrl = (Util.Function.IniReadValue("OPTIONK", "serverurl") ?? "").Trim().TrimEnd('/');
                _remoteApiKey = (Util.Function.IniReadValue("OPTIONK", "apikey") ?? "").Trim();
                if (string.IsNullOrEmpty(_remoteUrl))
                    _remoteUrl = (Util.Function.IniReadValue("UPLOAD", "serverurl") ?? "").Trim().TrimEnd('/');
                if (string.IsNullOrEmpty(_remoteApiKey))
                    _remoteApiKey = (Util.Function.IniReadValue("UPLOAD", "apikey") ?? "").Trim();
            }
            catch { _remote = false; }
        }

        private static string DevName(string d)
        {
            if (string.IsNullOrEmpty(d)) return "CPU";
            if (d == "remote") return "서버(ParkingWeb)";
            if (d.IndexOf("cuda") >= 0) return "GPU/CUDA";
            if (d.IndexOf("gpu") >= 0) return "GPU/iGPU(OpenVINO)";
            if (d.IndexOf("ov") >= 0) return "CPU(OpenVINO)";
            return "CPU";
        }

        private static bool IsGpuDev(string d)
        {
            return !string.IsNullOrEmpty(d) && (d.IndexOf("cuda") >= 0 || d.IndexOf("gpu") >= 0);
        }

        /// <summary>초기화 성공 표시 + 현재 장치(CPU/GPU) 로그.</summary>
        private static bool MarkReady()
        {
            _device = GetHealthDevice() ?? "";
            _initialized = true;
            Util.Logger.Log("[OptionK] 인식 장치 = " + DevName(_device));
            return true;
        }

        public static void Configure(string url = null, string pythonExe = null,
                                     string serverDir = null, bool? autoStart = null)
        {
            if (url != null) _url = url;
            if (pythonExe != null) _pythonExe = pythonExe;
            if (serverDir != null) _serverDir = serverDir;
            if (autoStart.HasValue) _autoStart = autoStart.Value;
        }

        /// <summary>환경설정 CPU/GPU 선택(CoreType==GPU)이면 true.</summary>
        private static bool DesiredGpu()
        {
            try { return frmLprMain.ENV.CameraEnv.CoreType == (int)ClsStructure.CoreType.GPU; }
            catch { return false; }
        }

        private static void ResolveServerDir(string dataDir)
        {
            if (!string.IsNullOrEmpty(_serverDir)) return;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] cands = {
                Path.Combine(baseDir, "OptionK_server"),
                string.IsNullOrEmpty(dataDir) ? null : Path.Combine(dataDir, "OptionK_server"),
            };
            foreach (var c in cands)
                if (c != null && File.Exists(Path.Combine(c, "optionk_server.py"))) { _serverDir = c; break; }
        }

        public static bool Initialize(string dataDir)
        {
            try
            {
                lock (_lock)
                {
                    if (_initialized) return true;

                    // 원격 모드: 로컬 파이썬 사이드카 없이 ParkingWeb 서버로 인식 위임 → 즉시 준비완료
                    LoadRemoteConfig();
                    if (_remote)
                    {
                        if (string.IsNullOrEmpty(_remoteUrl))
                        {
                            Util.Logger.Log("[OptionK] 원격 인식 ON 이지만 서버 URL 없음 ([OPTIONK] 또는 [UPLOAD] serverurl 확인)");
                            return false;
                        }
                        _device = "remote";
                        _initialized = true;
                        // 원격모드는 로컬 사이드카 불필요 → 이전 세션에서 남은 고아 사이드카 정리(상속 소켓포트 점유 방지)
                        KillStaleSidecars();
                        Util.Logger.Log("[OptionK] 원격 인식 모드(ParkingWeb): " + _remoteUrl);
                        return true;
                    }

                    bool desiredGpu = DesiredGpu();
                    ResolveServerDir(dataDir);
                    bool canStart = _autoStart && !string.IsNullOrEmpty(_serverDir)
                                    && File.Exists(Path.Combine(_serverDir, "optionk_server.py"));

                    if (canStart)
                    {
                        // 이미 떠 있는 사이드카가 건강하고(모델 동일 + 장치 호환)이면 재사용 →
                        // 13초 재초기화 생략. 모델이 갱신됐거나(서명 불일치) CPU 설정인데 GPU로
                        // 떠 있는 경우에만 종료 후 새로 시작한다.
                        string rdev, rsig;
                        if (GetHealth(out rdev, out rsig))
                        {
                            bool modelSame = !string.IsNullOrEmpty(rsig) && rsig == LocalModelSig();
                            bool cpuButRunningGpu = !desiredGpu && IsGpuDev(rdev);
                            if (modelSame && !cpuButRunningGpu)
                            {
                                Util.Logger.Log("[OptionK] 기존 사이드카 재사용 — 재시작 생략 (device=" + rdev + ")");
                                return MarkReady();
                            }
                            Util.Logger.Log("[OptionK] 사이드카 재시작 (모델동일=" + modelSame + ", device=" + rdev + ")");
                        }
                        Shutdown();          // /shutdown(graceful) + 내가 띄운 프로세스 Kill
                        KillPort(8765);      // 포트 점유 프로세스 강제 종료(구버전·안죽는 것 대비)
                        Thread.Sleep(1500);
                        if (StartServer(desiredGpu) && WaitReady(60000)) return MarkReady();
                    }
                    // 자동시작 불가(서버폴더 없음)면 이미 떠 있는 사이드카라도 사용
                    if (GetHealthDevice() != null) return MarkReady();

                    Util.Logger.Log("[OptionK] 사이드카 시작 실패. OptionK_server\\optionk_server.py 확인.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Util.Logger.Log("[OptionK.Initialize] " + ex.ToString());
                return false;
            }
        }

        public static void Reg(int camIndex, int idx, bool bRegCarType)
        {
            Util.Logger.Log(string.Format("[OptionK] Reg Start cam={0} idx={1}", camIndex, idx));
            var t = new Thread(() => RegPlateNo(camIndex, idx, bRegCarType));
            t.IsBackground = true;
            t.Start();
        }

        public static Result RegPlateNo(int camIndex, int idx, bool bRegCarType)
        {
            var result = new Result();
            ClsStructure.RegStruct regArray = camIndex == 0 ? clsThread.RegArray1[idx]
                                                            : clsThread.RegArray2[idx];
            try
            {
                Rectangle roi = camIndex == 0
                    ? frmLprMain.ENV.CameraEnv.IPCamera1Info.Roi
                    : frmLprMain.ENV.CameraEnv.IPCamera2Info.Roi;

                regArray.PlateNo = "";
                regArray.CarType = "";

                var imgPath = regArray.SourcePath;
                // 사이드카는 별도 프로세스(작업폴더 다름) → 절대경로로 변환해 전달
                if (!string.IsNullOrEmpty(imgPath))
                {
                    try { imgPath = Path.GetFullPath(imgPath); } catch { }
                }
                if (string.IsNullOrEmpty(imgPath) || !File.Exists(imgPath))
                {
                    Util.Logger.Log(string.Format("[OptionK] 입력 이미지 없음: {0}", imgPath));
                    regArray.PlateNo = "No_Detection";
                    regArray.PlateRoi = "0,0,0,0";
                }
                else
                {
                    if (!_initialized && !Initialize(null))
                    {
                        regArray.PlateNo = "No_Detection";
                        regArray.PlateRoi = "0,0,0,0";
                    }
                    else
                    {
                        var sw = DateTime.Now;
                        string roiStr = string.Format("{0},{1},{2},{3}", roi.X, roi.Y, roi.Width, roi.Height);
                        var res = Recognize(imgPath, roiStr);
                        regArray.term = (long)(DateTime.Now - sw).TotalMilliseconds;

                        string plate = res != null && res.ContainsKey("plate") ? Convert.ToString(res["plate"]) : "";
                        bool ok = res != null && res.ContainsKey("ok") && Convert.ToBoolean(res["ok"]);
                        if (ok && !string.IsNullOrEmpty(plate))
                        {
                            regArray.PlateNo = plate;
                            regArray.PlateRoi = res.ContainsKey("bbox") ? Convert.ToString(res["bbox"]) : roiStr;
                            double conf = res.ContainsKey("conf") ? Convert.ToDouble(res["conf"]) : 0.0;
                            Util.Logger.Log(string.Format("[OptionK] 인식 OK: {0} ROI={1} conf={2:F1}% time={3}ms [{4}]",
                                plate, regArray.PlateRoi, conf * 100.0, regArray.term, DevName(_device)));
                        }
                        else
                        {
                            regArray.PlateNo = "No_Detection";
                            regArray.PlateRoi = "0,0,0,0";
                            string err = res != null && res.ContainsKey("err") ? Convert.ToString(res["err"]) : "";
                            Util.Logger.Log(string.Format("[OptionK] 인식 실패({0}) time={1}ms", err, regArray.term));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Util.Logger.Log("[OptionK.RegPlateNo] " + ex.ToString());
            }
            finally
            {
                try
                {
                    if (string.IsNullOrEmpty(regArray.PlateNo))
                    {
                        regArray.PlateNo = "No_Detection";
                        regArray.PlateRoi = "0,0,0,0";
                    }
                    if (camIndex == 0) clsThread.RegArray1[idx] = regArray;
                    else clsThread.RegArray2[idx] = regArray;
                }
                catch { }
            }
            return result;
        }

        // ---- HTTP ----
        private static Dictionary<string, object> Recognize(string path, string roi)
        {
            if (_remote) return RecognizeRemote(path, roi);
            return RecognizeLocal(path, roi);
        }

        /// <summary>서버모드 추가 카메라(3~15)용 공개 인식 — 절대경로 이미지+ROI → {ok,plate,conf,bbox}.
        /// 초기화 안 됐으면 Initialize. (원격모드면 ParkingWeb, 아니면 로컬 사이드카)</summary>
        public static Dictionary<string, object> RecognizeImage(string path, string roi)
        {
            try
            {
                if (!_initialized) Initialize(null);
                return Recognize(path, roi ?? "");
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object> { { "ok", false }, { "err", ex.Message } };
            }
        }

        /// <summary>원격(ParkingWeb): 이미지 파일을 multipart 로 /api/lpr/recognize 에 전송 → 차번 수신.</summary>
        private static Dictionary<string, object> RecognizeRemote(string path, string roi)
        {
            try
            {
                using (var content = new MultipartFormDataContent())
                using (var fs = File.OpenRead(path))
                {
                    var fc = new StreamContent(fs);
                    fc.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                    content.Add(fc, "file", Path.GetFileName(path));
                    content.Add(new StringContent(roi ?? ""), "roi");
                    content.Add(new StringContent(_remoteApiKey ?? ""), "apiKey");

                    string url = _remoteUrl.TrimEnd('/') + "/api/lpr/recognize";
                    using (var resp = _rhttp.PostAsync(url, content).GetAwaiter().GetResult())
                    {
                        string txt = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        if (!resp.IsSuccessStatusCode)
                            return new Dictionary<string, object> { { "ok", false }, { "err", "HTTP " + (int)resp.StatusCode } };
                        return _json.Deserialize<Dictionary<string, object>>(txt);
                    }
                }
            }
            catch (Exception ex)
            {
                return new Dictionary<string, object> { { "ok", false }, { "err", ex.Message } };
            }
        }

        /// <summary>로컬: 사이드카(optionk_server.py)에 절대경로+ROI 를 JSON 으로 전송.</summary>
        private static Dictionary<string, object> RecognizeLocal(string path, string roi)
        {
            string body = _json.Serialize(new Dictionary<string, string> { { "path", path }, { "roi", roi } });
            byte[] data = Encoding.UTF8.GetBytes(body);
            var req = (HttpWebRequest)WebRequest.Create(_url + "/recognize");
            req.Method = "POST";
            req.ContentType = "application/json; charset=utf-8";
            req.ContentLength = data.Length;
            req.Timeout = 60000;   // cold first inference(iGPU 커널 컴파일) 대비 여유
            using (var s = req.GetRequestStream()) s.Write(data, 0, data.Length);
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                return _json.Deserialize<Dictionary<string, object>>(sr.ReadToEnd());
        }

        /// <summary>실행 중이고 ready 면 true + device/model_sig 반환.</summary>
        private static bool GetHealth(out string device, out string sig)
        {
            device = ""; sig = "";
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(_url + "/health");
                req.Timeout = 1500;
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    var h = _json.Deserialize<Dictionary<string, object>>(sr.ReadToEnd());
                    if (h == null || !h.ContainsKey("ready") || !Convert.ToBoolean(h["ready"])) return false;
                    device = h.ContainsKey("device") ? Convert.ToString(h["device"]) : "cpu";
                    sig = h.ContainsKey("model_sig") ? Convert.ToString(h["model_sig"]) : "";
                    return true;
                }
            }
            catch { device = ""; sig = ""; return false; }
        }

        /// <summary>실행 중이면 device("cuda"/"cpu") 반환, 아니면 null.</summary>
        private static string GetHealthDevice()
        {
            string d, s;
            return GetHealth(out d, out s) ? (string.IsNullOrEmpty(d) ? "cpu" : d) : null;
        }

        /// <summary>모델+코드 파일들의 결합 서명(size_mtime|...) — 사이드카 model_sig 와 비교.
        /// Python _bundle_sig() 와 동일 규칙·동일 순서. 모델이나 코드가 바뀌면 재시작한다.</summary>
        private static string LocalModelSig()
        {
            string[] rels = { @"models\plate_crnn.pth", "optionk_server.py",
                              "crnn_recognizer.py", "optionk_core.py" };
            var parts = new List<string>();
            foreach (var rel in rels)
            {
                try
                {
                    string p = Path.Combine(_serverDir, rel);
                    if (!File.Exists(p)) { parts.Add("0"); continue; }
                    var fi = new FileInfo(p);
                    long mtime = (long)(fi.LastWriteTimeUtc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                    parts.Add(fi.Length + "_" + mtime);
                }
                catch { parts.Add("0"); }
            }
            return string.Join("|", parts);
        }

        /// <summary>해당 포트를 LISTENING 중인 프로세스를 강제 종료(구버전 사이드카 등).</summary>
        /// <summary>고아 OptionK 사이드카(python optionk_server.py) 정리. 부모 LPR이 죽은 뒤 남아
        ///  상속받은 소켓포트(요금계산기 40000 등)를 물고 있어 새 LPR이 바인딩 못 하는 문제 방지.</summary>
        public static void KillStaleSidecars()
        {
            try
            {
                using (var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT ProcessId, CommandLine FROM Win32_Process WHERE Name='python.exe'"))
                {
                    foreach (System.Management.ManagementObject mo in searcher.Get())
                    {
                        string cmd = (mo["CommandLine"] ?? "").ToString();
                        if (cmd.IndexOf("optionk_server.py", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int pid = Convert.ToInt32(mo["ProcessId"]);
                            try { Process.GetProcessById(pid).Kill(); Util.Logger.Log("[OptionK] 고아 사이드카 정리(PID " + pid + ")"); }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex) { Util.Logger.Log("[OptionK] 고아 사이드카 정리 오류: " + ex.Message); }
        }

        private static void KillPort(int port)
        {
            try
            {
                var psi = new ProcessStartInfo("netstat", "-ano")
                { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                var p = Process.Start(psi);
                string outp = p.StandardOutput.ReadToEnd();
                p.WaitForExit(3000);
                foreach (var line in outp.Split('\n'))
                {
                    if (line.IndexOf(":" + port, StringComparison.Ordinal) >= 0
                        && line.IndexOf("LISTENING", StringComparison.Ordinal) >= 0)
                    {
                        var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5 && int.TryParse(parts[parts.Length - 1], out int pid) && pid > 0)
                        {
                            try { Process.GetProcessById(pid).Kill(); Util.Logger.Log("[OptionK] 기존 사이드카 종료(PID " + pid + ")"); }
                            catch { }
                        }
                    }
                }
            }
            catch { }
        }

        private static void Shutdown()
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(_url + "/shutdown");
                req.Method = "POST"; req.ContentLength = 0; req.Timeout = 2000;
                using (req.GetResponse()) { }
            }
            catch { }
            try { if (_proc != null && !_proc.HasExited) _proc.Kill(); } catch { }
            _proc = null;
        }

        /// <summary>카메라 ROI 크기를 --warmup 인자로(첫 차 컴파일 지연 방지).</summary>
        private static string WarmupArg()
        {
            try
            {
                var sizes = new List<string>();
                var r1 = frmLprMain.ENV.CameraEnv.IPCamera1Info.Roi;
                if (r1.Width > 0 && r1.Height > 0) sizes.Add(r1.Width + "x" + r1.Height);
                var r2 = frmLprMain.ENV.CameraEnv.IPCamera2Info.Roi;
                if (r2.Width > 0 && r2.Height > 0 && (r2.Width != r1.Width || r2.Height != r1.Height))
                    sizes.Add(r2.Width + "x" + r2.Height);
                if (sizes.Count > 0) return " --warmup \"" + string.Join(",", sizes) + "\"";
            }
            catch { }
            return "";
        }

        private static bool StartServer(bool gpu)
        {
            try
            {
                string script = Path.Combine(_serverDir, "optionk_server.py");
                if (!File.Exists(script)) { Util.Logger.Log("[OptionK] optionk_server.py 없음: " + script); return false; }
                string args = "\"" + script + "\" --port 8765" + (gpu ? "" : " --cpu") + WarmupArg();
                var psi = new ProcessStartInfo
                {
                    FileName = _pythonExe,
                    Arguments = args,
                    WorkingDirectory = _serverDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                psi.EnvironmentVariables["PYTHONUTF8"] = "1";
                psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
                _proc = Process.Start(psi);
                Util.Logger.Log("[OptionK] 사이드카 시작: " + _pythonExe + " " + args);
                return true;
            }
            catch (Exception ex)
            {
                Util.Logger.Log("[OptionK.StartServer] " + ex.Message);
                return false;
            }
        }

        private static bool WaitReady(int timeoutMs)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (GetHealthDevice() != null) { Util.Logger.Log("[OptionK] 사이드카 준비(" + sw.ElapsedMilliseconds + "ms)"); return true; }
                Thread.Sleep(1000);
            }
            return false;
        }

        public static void Release()
        {
            lock (_lock)
            {
                try { if (_proc != null && !_proc.HasExited) _proc.Kill(); }
                catch (Exception ex) { Util.Logger.Log("[OptionK.Release] " + ex.Message); }
                _proc = null;
                _initialized = false;
                Util.Logger.Log("[OptionK] Release OK");
            }
        }
    }
}
