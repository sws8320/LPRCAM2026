using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using AForge.Video;
using AForge.Video.DirectShow;

namespace KyungsinLPR.USB
{
    /// <summary>
    /// USB(DirectShow/UVC) 카메라 래퍼.
    /// AForge.Video.DirectShow.VideoCaptureDevice를 감싸 IPCamera와 동일한 메서드 시그니처로 노출.
    /// NewFrame 이벤트로 받은 Bitmap을 락 보호 하에 보관하여, GetImage() 호출 시 클론 반환.
    /// </summary>
    public class USBCamera
    {
        private VideoCaptureDevice _device;
        private string _moniker;
        private int _resWidth;
        private int _resHeight;

        private readonly object _frameLock = new object();
        private Bitmap _latestFrame;       // 항상 최신 1프레임만 보관 (스트로브 모드에서 GetImage 시 사용)
        private DateTime _lastFrameTime = DateTime.MinValue;

        private volatile bool _connected = false;
        private long _frameCount = 0;
        private DateTime _startTime = DateTime.MinValue;

        public string DeviceName { get; private set; }
        public string Moniker { get { return _moniker; } }
        public int Width { get { return _resWidth; } }
        public int Height { get { return _resHeight; } }

        public delegate void NewFrameDelegate(Bitmap bmp);
        /// <summary>외부에서 실시간 프리뷰가 필요할 때 구독. (Bitmap은 클론본 — 사용 후 Dispose 권장)</summary>
        public event NewFrameDelegate OnNewFrame;

        /// <summary>시스템에 연결된 비디오 입력 장치 목록 조회.</summary>
        public static List<KeyValuePair<string, string>> ListDevices()
        {
            var result = new List<KeyValuePair<string, string>>();
            try
            {
                var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                foreach (FilterInfo fi in devices)
                {
                    result.Add(new KeyValuePair<string, string>(fi.Name, fi.MonikerString));
                }
            }
            catch (Exception ex)
            {
                Util.Logger.Log("[USBCamera.ListDevices] " + ex.Message);
            }
            return result;
        }

        /// <summary>특정 장치의 지원 해상도 목록.</summary>
        public static List<Size> ListResolutions(string moniker)
        {
            var result = new List<Size>();
            if (string.IsNullOrEmpty(moniker)) return result;
            try
            {
                var dev = new VideoCaptureDevice(moniker);
                foreach (var cap in dev.VideoCapabilities)
                {
                    result.Add(cap.FrameSize);
                }
            }
            catch (Exception ex)
            {
                Util.Logger.Log("[USBCamera.ListResolutions] " + ex.Message);
            }
            return result;
        }

        /// <summary>장치 초기화 — moniker / 해상도 저장. 실제 연결은 ConnectStreamPort() 호출.</summary>
        public void Init(string moniker, string deviceName, int width, int height)
        {
            _moniker = moniker;
            DeviceName = deviceName ?? "";
            _resWidth = width;
            _resHeight = height;
        }

        /// <summary>IPCamera.ConnectStreamPort() 와 동일한 역할 — 캡처 시작.</summary>
        public bool ConnectStreamPort()
        {
            if (string.IsNullOrWhiteSpace(_moniker))
            {
                Util.Logger.Log("[USBCamera.ConnectStreamPort] moniker 비어있음 — 연결 스킵");
                return false;
            }
            try
            {
                DisconnectStreamPort();

                _device = new VideoCaptureDevice(_moniker);

                if (_resWidth > 0 && _resHeight > 0 && _device.VideoCapabilities != null && _device.VideoCapabilities.Length > 0)
                {
                    VideoCapabilities matched = null;
                    foreach (var cap in _device.VideoCapabilities)
                    {
                        if (cap.FrameSize.Width == _resWidth && cap.FrameSize.Height == _resHeight)
                        {
                            matched = cap;
                            break;
                        }
                    }
                    if (matched != null) _device.VideoResolution = matched;
                    else Util.Logger.Log(string.Format(
                        "[USBCamera] 요청 해상도 {0}x{1} 미지원 — 기본값 사용",
                        _resWidth, _resHeight));
                }

                _device.NewFrame += Device_NewFrame;
                _device.VideoSourceError += Device_VideoSourceError;
                _device.Start();
                _connected = true;
                _startTime = DateTime.Now;
                _frameCount = 0;
                Util.Logger.Log(string.Format("[USBCamera] 시작 — '{0}' {1}x{2} (그래프 빌드 완료, NewFrame 대기 중)", DeviceName, _resWidth, _resHeight));
                // 5초 후 NewFrame이 한 번도 안 들어왔으면 진단 로그 (대역폭/드라이버 문제 의심)
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        Thread.Sleep(5000);
                        if (_connected && Interlocked.Read(ref _frameCount) == 0)
                        {
                            Util.Logger.Log(string.Format(
                                "[USBCamera.진단] '{0}' 5초간 NewFrame 0건 — USB 대역폭 부족/포맷 미스매치/장치 점유 의심. " +
                                "다른 USB 포트(별도 호스트 컨트롤러) 또는 해상도 낮춰 재시도 권장.", DeviceName));
                        }
                    }
                    catch { }
                });
                return true;
            }
            catch (Exception ex)
            {
                Util.Logger.Log("[USBCamera.ConnectStreamPort] " + ex.ToString());
                _connected = false;
                return false;
            }
        }

        public bool IsStreamPortConnected()
        {
            try { return _device != null && _device.IsRunning && _connected; }
            catch { return false; }
        }

        /// <summary>IPCamera와 동일 — 컨트롤 명령용 포트는 USB에 없으므로 스트림과 동일하게 취급.</summary>
        public bool IsCommandPortConnected() { return IsStreamPortConnected(); }

        public void DisconnectStreamPort()
        {
            try
            {
                if (_device != null)
                {
                    _device.NewFrame -= Device_NewFrame;
                    _device.VideoSourceError -= Device_VideoSourceError;
                    if (_device.IsRunning)
                    {
                        _device.SignalToStop();
                        _device.WaitForStop();
                    }
                    _device = null;
                }
            }
            catch (Exception ex)
            {
                Util.Logger.Log("[USBCamera.DisconnectStreamPort] " + ex.Message);
            }
            finally
            {
                _connected = false;
            }
        }

        private void Device_VideoSourceError(object sender, VideoSourceErrorEventArgs e)
        {
            Util.Logger.Log(string.Format("[USBCamera.SourceError] '{0}' {1}", DeviceName, e.Description));
        }

        private void Device_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                Bitmap clone = (Bitmap)eventArgs.Frame.Clone();
                Bitmap toDispose = null;
                lock (_frameLock)
                {
                    toDispose = _latestFrame;
                    _latestFrame = clone;
                    _lastFrameTime = DateTime.Now;
                }
                if (toDispose != null)
                {
                    try { toDispose.Dispose(); } catch { }
                }
                Interlocked.Increment(ref _frameCount);

                var handler = OnNewFrame;
                if (handler != null)
                {
                    try
                    {
                        Bitmap previewCopy = (Bitmap)clone.Clone();
                        handler(previewCopy);
                    }
                    catch (Exception ex)
                    {
                        Util.Logger.Log("[USBCamera.OnNewFrame handler] " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Util.Logger.Log("[USBCamera.Device_NewFrame] " + ex.Message);
            }
        }

        /// <summary>
        /// IPCamera.GetImage(timeout, out Bitmap) 와 동일한 역할.
        /// USB는 연속 스트림이므로 timeout 안에 새 프레임이 도착할 때까지 대기 후 최신 프레임 클론 반환.
        /// </summary>
        public IPCamError GetImage(int timeoutMs, out Bitmap bitmap)
        {
            bitmap = null;
            if (!IsStreamPortConnected())
                return IPCamError.StreamNotOpened;

            DateTime tStart = DateTime.Now;
            DateTime requestTime = tStart;

            while ((DateTime.Now - tStart).TotalMilliseconds < timeoutMs)
            {
                lock (_frameLock)
                {
                    if (_latestFrame != null && _lastFrameTime >= requestTime)
                    {
                        bitmap = (Bitmap)_latestFrame.Clone();
                        return IPCamError.OK;
                    }
                }
                Thread.Sleep(5);
            }

            // 타임아웃 — 새 프레임이 안 왔지만, 보관된 마지막 프레임이라도 반환 (지연시간이 너무 크지 않을 때)
            lock (_frameLock)
            {
                if (_latestFrame != null && (DateTime.Now - _lastFrameTime).TotalMilliseconds < 1000)
                {
                    bitmap = (Bitmap)_latestFrame.Clone();
                    return IPCamError.OK;
                }
            }
            return IPCamError.Timeout;
        }

        /// <summary>IPCamera.SaveLastImage 와 동일 — 최신 프레임을 파일로 저장.</summary>
        public bool SaveLastImage(string path)
        {
            try
            {
                Bitmap snapshot = null;
                lock (_frameLock)
                {
                    if (_latestFrame != null)
                        snapshot = (Bitmap)_latestFrame.Clone();
                }
                if (snapshot == null) return false;

                string ext = Path.GetExtension(path);
                ImageFormat fmt = ImageFormat.Jpeg;
                if (!string.IsNullOrEmpty(ext))
                {
                    string e = ext.ToLowerInvariant();
                    if (e == ".bmp") fmt = ImageFormat.Bmp;
                    else if (e == ".png") fmt = ImageFormat.Png;
                }

                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                snapshot.Save(path, fmt);
                snapshot.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                Util.Logger.Log("[USBCamera.SaveLastImage] " + ex.Message);
                return false;
            }
        }

        /// <summary>마지막 프레임 도착 시각 (Watchdog용).</summary>
        public DateTime LastFrameTime
        {
            get { lock (_frameLock) { return _lastFrameTime; } }
        }

        public long FrameCount { get { return Interlocked.Read(ref _frameCount); } }

        /// <summary>호환용 stub — iNova IPCamera 시그니처 맞추기. USB는 카메라 측 리셋 불가.</summary>
        public void ResetCamera() { /* USB는 리셋 명령 없음 — 재연결로 대체 */ }
    }
}
