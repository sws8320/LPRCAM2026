using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace KyungsinLPR
{
    /// <summary>
    /// 서버모드 추가 카메라(카드 3번 이상 = 인덱스 2~14) 연결·그랩 관리.
    /// 카메라별 소스(iNova1 / iNova2 / WGWK-A05D)를 선택해 해당 스트림에서 프레임을 받아 카드에 표시.
    /// (USB는 서버모드 미지원). 카드 1,2(인덱스 0,1)는 기존 GrabLoop1/2 + MirrorToServerCard 가 처리.
    /// 캡처/인식/정산은 카드에 표시된 프레임 기반이라 소스와 무관(ProcessServerCamResult).
    /// 카메라 IP = 카드 더블클릭 개별설정의 카메라설정 IP(= [SVRCAM{n}] pc_txtCamIp).
    /// 소스 = [SVRCAM{n}] camsource(1/2/4) — 없으면 pc_cmbCameraType 텍스트, 둘 다 없으면 iNova2(기존 호환).
    /// </summary>
    public class ServerCamManager : IDisposable
    {
        // ── 카메라 소스별 공통 어댑터 ──────────────────────────────
        private interface ISvrCamDev
        {
            bool Connect();
            bool IsConnected();
            bool GetFrame(int timeoutMs, out Bitmap bmp);
            void Reset();
            void Disconnect();
            object Raw { get; }   // iNova2 고급기능 다이얼로그용(그 외 null)
        }

        private class Inova1Dev : ISvrCamDev
        {
            private readonly IPCamera _d = new IPCamera();
            private readonly string _ip; private readonly bool _udp;
            public Inova1Dev(string ip, bool udp) { _ip = ip; _udp = udp; }
            public bool Connect() { bool ok = _d.ConnectStreamPort(_ip, _udp); if (ok) _d.ConnectCommandPort(_ip); return ok; }
            public bool IsConnected() { return _d.IsStreamPortConnected(); }
            public bool GetFrame(int t, out Bitmap bmp) { return _d.GetImage(t, out bmp) == IPCamError.OK; }
            public void Reset() { try { _d.ResetCamera(); } catch { } }
            public void Disconnect() { try { _d.DisconnectStreamPort(); } catch { } try { _d.DisconnectCommandPort(); } catch { } }
            public object Raw { get { return null; } }
        }

        private class Inova2Dev : ISvrCamDev
        {
            private readonly iNova2.IPCamera _d = new iNova2.IPCamera();
            private readonly string _ip; private readonly bool _udp;
            public Inova2Dev(string ip, bool udp) { _ip = ip; _udp = udp; }
            public bool Connect() { bool ok = _d.ConnectStreamPort(_ip, _udp) == iNova2.IPCamError.OK; if (ok) _d.ConnectCommandPort(_ip); return ok; }
            public bool IsConnected() { return _d.IsStreamPortConnected(); }
            public bool GetFrame(int t, out Bitmap bmp) { iNova2.MetaInfo m; return _d.GetImage(t, out bmp, out m) == iNova2.IPCamError.OK; }
            public void Reset() { try { _d.ResetCamera(); } catch { } }
            public void Disconnect() { try { _d.DisconnectStreamPort(); } catch { } try { _d.DisconnectCommandPort(); } catch { } }
            public object Raw { get { return _d; } }
        }

        private class WgwkDev : ISvrCamDev
        {
            private readonly WGWK.WgwkCamera _d = new WGWK.WgwkCamera();
            private readonly string _ip; private readonly string _user; private readonly string _pass;
            public WgwkDev(string ip, string user, string pass) { _ip = ip; _user = user; _pass = pass; }
            public bool Connect() { _d.Init(_ip, 80, _user, _pass, 1); return _d.ConnectStreamPort(); } // 포트80/메인 고정
            public bool IsConnected() { return _d.IsStreamPortConnected(); }
            public bool GetFrame(int t, out Bitmap bmp) { return _d.GetImage(t, out bmp) == IPCamError.OK; }
            public void Reset() { }
            public void Disconnect() { try { _d.DisconnectStreamPort(); } catch { } }
            public object Raw { get { return null; } }
        }

        private class Cam
        {
            public int Index;
            public int Source;       // 1=iNova1, 2=iNova2, 4=WGWK
            public string Ip;
            public ISvrCamDev Dev;
            public Thread Grab;
            public volatile bool Keep;
            public CameraCard Card;
        }

        private readonly List<Cam> _cams = new List<Cam>();

        /// <summary>인덱스 2~(camCount-1) 카메라를 [SVRCAM{n}] 소스/IP 로 연결·그랩 시작.</summary>
        public void Start(int camCount, Func<int, CameraCard> cardOf)
        {
            for (int i = 2; i < camCount && i < ServerCamConfig.MAX; i++)
            {
                string sec = "SVRCAM" + (i + 1);
                string ip = ReadIp(sec);
                if (string.IsNullOrEmpty(ip))
                {
                    Util.Logger.Log(string.Format("[ServerCam{0}] IP 미설정 — 카드 더블클릭→카메라설정에서 IP 입력 필요", i + 1));
                    continue;
                }
                int src = ReadSource(sec);
                bool udp = ReadUdp(sec);
                Cam c = new Cam
                {
                    Index = i,
                    Source = src,
                    Ip = ip,
                    Card = (cardOf != null) ? cardOf(i) : null,
                    Dev = CreateDev(sec, src, ip, udp),
                    Keep = true
                };
                c.Grab = new Thread(delegate () { GrabLoop(c); });
                c.Grab.IsBackground = true;
                c.Grab.Start();
                _cams.Add(c);
                Util.Logger.Log(string.Format("[ServerCam{0}] 그랩 시작 소스={1} IP={2} udp={3}", i + 1, SourceName(src), ip, udp));
            }
        }

        private static ISvrCamDev CreateDev(string sec, int src, string ip, bool udp)
        {
            switch (src)
            {
                case 1: return new Inova1Dev(ip, udp);
                case 4:
                    string u = Read(sec, "pc_txtWgwkUser"); if (string.IsNullOrWhiteSpace(u)) u = "admin";
                    string p = Read(sec, "pc_txtWgwkPass"); if (string.IsNullOrEmpty(p)) p = "123456";
                    return new WgwkDev(ip, u, p);
                case 2:
                default: return new Inova2Dev(ip, udp);
            }
        }

        private static string SourceName(int src)
        {
            switch (src) { case 1: return "iNova1"; case 4: return "WGWK-A05D"; default: return "iNova2"; }
        }

        private void GrabLoop(Cam c)
        {
            int errCnt = 0;
            try { c.Dev.Connect(); } catch { }
            while (c.Keep)
            {
                try
                {
                    Bitmap bmp;
                    if (c.Dev.GetFrame(1000, out bmp))
                    {
                        errCnt = 0;
                        if (c.Card != null && bmp != null) c.Card.SetImage(bmp);
                    }
                    else
                    {
                        errCnt++;
                        if (errCnt > 100) { c.Dev.Reset(); errCnt = 0; }
                    }
                    if (!c.Dev.IsConnected())
                    {
                        try { c.Dev.Disconnect(); } catch { }
                        Thread.Sleep(100);
                        try { c.Dev.Connect(); } catch { }
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    Util.Logger.Log(string.Format("[ServerCam{0}] 그랩 오류 {1}", c.Index + 1, ex.Message));
                    Thread.Sleep(200);
                }
            }
        }

        // 카메라 소스 = [SVRCAM{n}] camsource(1/2/4). 없으면 pc_cmbCameraType 텍스트, 둘 다 없으면 2(iNova2 기존 호환)
        private static int ReadSource(string sec)
        {
            string s = Util.Function.IniReadValue(sec, "camsource");
            int v;
            if (int.TryParse(s, out v) && (v == 1 || v == 2 || v == 4)) return v;
            string t = (Util.Function.IniReadValue(sec, "pc_cmbCameraType") ?? "").Trim();
            if (t.IndexOf("WGWK", StringComparison.OrdinalIgnoreCase) >= 0) return 4;
            if (t == "iNova1") return 1;
            if (t == "iNova2") return 2;
            return 2;
        }

        // 카메라 IP = 개별설정 카메라설정 txtCamIp → [SVRCAM{n}] pc_txtCamIp (없으면 ip 키 폴백)
        private static string ReadIp(string sec)
        {
            string ip = Util.Function.IniReadValue(sec, "pc_txtCamIp");
            if (string.IsNullOrEmpty(ip)) ip = Util.Function.IniReadValue(sec, "ip");
            return (ip ?? "").Trim();
        }
        private static bool ReadUdp(string sec)
        {
            string v = Util.Function.IniReadValue(sec, "udp");
            return v == "1" || (!string.IsNullOrEmpty(v) && v.Equals("true", StringComparison.OrdinalIgnoreCase));
        }
        private static string Read(string sec, string key)
        {
            return (Util.Function.IniReadValue(sec, key) ?? "").Trim();
        }

        // 개별 영역설정(roi). "0,0,0,0"/빈값이면 전체(서버 검출) → "" 반환
        private static string ReadRoi(string sec)
        {
            string r = (Util.Function.IniReadValue(sec, "roi") ?? "").Replace(" ", "").Trim();
            if (string.IsNullOrEmpty(r) || r == "0,0,0,0") return "";
            return r;
        }

        /// <summary>카드 캡처 버튼/트리거 → 현재 프레임을 ParkingWeb 인식 → 카드 차번표시.
        /// 게이트/입출차 DB 처리는 ProcessServerCamResult. 인식은 백그라운드 스레드에서.</summary>
        public void Capture(int index)
        {
            Cam c = null;
            foreach (Cam x in _cams) if (x.Index == index) { c = x; break; }
            if (c == null || c.Card == null) { Util.Logger.Log(string.Format("[ServerCam{0}] 캡처: 카메라/카드 없음", index + 1)); return; }
            Image snap = c.Card.GetSnapshot();
            if (snap == null) { Util.Logger.Log(string.Format("[ServerCam{0}] 캡처: 영상 없음", index + 1)); return; }
            CameraCard card = c.Card;
            Thread t = new Thread(delegate () { CaptureWork(index, card, snap); });
            t.IsBackground = true;
            t.Start();
        }

        private void CaptureWork(int index, CameraCard card, Image snap)
        {
            string fname = null;
            DateTime capTime = DateTime.Now;
            try
            {
                fname = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(),
                    string.Format("SVR{0}_{1}.jpg", index + 1, capTime.ToString("yyyyMMddHHmmssfff")));
                using (Bitmap bmp = new Bitmap(snap))
                    bmp.Save(fname, System.Drawing.Imaging.ImageFormat.Jpeg);
                Util.Logger.Log(string.Format("[ServerCam{0}] 캡처→인식 {1}", index + 1, fname));
                string roi = ReadRoi("SVRCAM" + (index + 1));   // 개별 영역설정([SVRCAM] roi). 없으면 전체
                Dictionary<string, object> res = OptionK.RecognizeImage(fname, roi);
                string plate = (res != null && res.ContainsKey("plate")) ? Convert.ToString(res["plate"]) : "";
                bool ok = res != null && res.ContainsKey("ok") && Convert.ToBoolean(res["ok"]);
                card.SetPlate(string.IsNullOrEmpty(plate) ? "No_Detection" : plate, ok);
                Util.Logger.Log(string.Format("[ServerCam{0}] 인식결과 '{1}' ok={2}", index + 1, plate, ok));
                try
                {
                    if (clsThread.main != null)
                        clsThread.main.ProcessServerCamResult(index, plate, snap, capTime);
                }
                catch (Exception pe) { Util.Logger.Log(string.Format("[ServerCam{0}] 정산처리 호출오류 {1}", index + 1, pe.Message)); }
            }
            catch (Exception ex)
            {
                Util.Logger.Log(string.Format("[ServerCam{0}] 캡처 인식오류 {1}", index + 1, ex.Message));
            }
            finally
            {
                try { snap.Dispose(); } catch { }
                if (fname != null) { try { System.IO.File.Delete(fname); } catch { } }
            }
        }

        /// <summary>인덱스 카메라의 iNova2 디바이스(개별설정 고급기능용). iNova2 소스가 아니면 null.</summary>
        public iNova2.IPCamera GetDevice(int index)
        {
            foreach (Cam x in _cams) if (x.Index == index) return x.Dev != null ? (x.Dev.Raw as iNova2.IPCamera) : null;
            return null;
        }

        public void Dispose()
        {
            foreach (Cam c in _cams)
            {
                c.Keep = false;
                try { if (c.Grab != null) c.Grab.Join(1000); } catch { }
                try { if (c.Dev != null) c.Dev.Disconnect(); } catch { }
            }
            _cams.Clear();
        }
    }
}
