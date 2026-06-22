using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;

namespace KyungsinLPR
{
    /// <summary>
    /// 서버모드 추가 카메라(카드 3번 이상 = 인덱스 2~14) 연결·그랩 관리.
    /// iNova2 스트림에서 프레임을 받아 해당 카드에 표시한다.
    /// (카드 1,2 = 인덱스 0,1 은 기존 GrabLoop1/2 + MirrorToServerCard 가 처리)
    /// A단계: 영상 표시만. 캡처/인식/데이터처리는 B/C단계.
    /// 카메라 IP 는 카드 더블클릭 개별설정의 카메라설정 IP(= [SVRCAM{n}] pc_txtCamIp).
    /// </summary>
    public class ServerCamManager : IDisposable
    {
        private class Cam
        {
            public int Index;
            public string Ip;
            public bool Udp;
            public iNova2.IPCamera Dev;
            public Thread Grab;
            public volatile bool Keep;
            public CameraCard Card;
        }

        private readonly List<Cam> _cams = new List<Cam>();

        /// <summary>인덱스 2~(camCount-1) 카메라를 [SVRCAM{n}] IP 로 연결·그랩 시작.</summary>
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
                Cam c = new Cam
                {
                    Index = i,
                    Ip = ip,
                    Udp = ReadUdp(sec),
                    Card = (cardOf != null) ? cardOf(i) : null,
                    Dev = new iNova2.IPCamera(),
                    Keep = true
                };
                c.Grab = new Thread(delegate () { GrabLoop(c); });
                c.Grab.IsBackground = true;
                c.Grab.Start();
                _cams.Add(c);
                Util.Logger.Log(string.Format("[ServerCam{0}] 그랩 시작 IP={1} udp={2}", i + 1, ip, c.Udp));
            }
        }

        private void GrabLoop(Cam c)
        {
            int errCnt = 0;
            try { c.Dev.ConnectStreamPort(c.Ip, c.Udp); c.Dev.ConnectCommandPort(c.Ip); } catch { }
            while (c.Keep)
            {
                try
                {
                    Bitmap bmp;
                    iNova2.MetaInfo meta;
                    iNova2.IPCamError err = c.Dev.GetImage(1000, out bmp, out meta);
                    if (err == iNova2.IPCamError.OK)
                    {
                        errCnt = 0;
                        if (c.Card != null && bmp != null) c.Card.SetImage(bmp);
                    }
                    else
                    {
                        errCnt++;
                        if (errCnt > 100) { try { c.Dev.ResetCamera(); } catch { } errCnt = 0; }
                    }
                    if (!c.Dev.IsStreamPortConnected())
                    {
                        try { c.Dev.DisconnectStreamPort(); } catch { }
                        Thread.Sleep(100);
                        try { c.Dev.ConnectStreamPort(c.Ip, c.Udp); } catch { }
                        Thread.Sleep(100);
                    }
                    if (!c.Dev.IsCommandPortConnected())
                    {
                        try { c.Dev.DisconnectCommandPort(); c.Dev.ConnectCommandPort(c.Ip); } catch { }
                    }
                }
                catch (Exception ex)
                {
                    Util.Logger.Log(string.Format("[ServerCam{0}] 그랩 오류 {1}", c.Index + 1, ex.Message));
                    Thread.Sleep(200);
                }
            }
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

        // 개별 영역설정(roi). "0,0,0,0"/빈값이면 전체(서버 검출) → "" 반환
        private static string ReadRoi(string sec)
        {
            string r = (Util.Function.IniReadValue(sec, "roi") ?? "").Replace(" ", "").Trim();
            if (string.IsNullOrEmpty(r) || r == "0,0,0,0") return "";
            return r;
        }

        /// <summary>카드 캡처 버튼/트리거 → 현재 프레임을 ParkingWeb 인식 → 카드 차번표시(B단계).
        /// (게이트/입출차 DB 처리는 C단계). 인식은 백그라운드 스레드에서.</summary>
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
                // C단계: 정산처리(이미지저장+게이트/입출차/정기권) — frmLprMain.ProcessServerCamResult
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

        /// <summary>인덱스 카메라의 iNova2 디바이스(개별설정 고급기능/영역설정용). 없으면 null.</summary>
        public iNova2.IPCamera GetDevice(int index)
        {
            foreach (Cam x in _cams) if (x.Index == index) return x.Dev;
            return null;
        }

        public void Dispose()
        {
            foreach (Cam c in _cams)
            {
                c.Keep = false;
                try { if (c.Grab != null) c.Grab.Join(1000); } catch { }
                try { c.Dev.DisconnectStreamPort(); } catch { }
                try { c.Dev.DisconnectCommandPort(); } catch { }
            }
            _cams.Clear();
        }
    }
}
