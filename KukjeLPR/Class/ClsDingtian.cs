using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace KyungsinLPR
{
    /// <summary>
    /// Dingtian 이더넷 릴레이 보드 — D:\1\relay_sdk SDK 기반 ASCII 프로토콜
    ///  - 출력 (RelayOn): TCP 60001 → UDP 60001 폴백, ASCII "1{ch}" ON / "2{ch}" OFF
    ///  - 입력 (InEvent): HTTP /input.cgi 폴링 (응답 "&0&0&8&v1&v2&...&v8&" 형식)
    ///  - SerialDevice.dll의 ClsKJC_1000 / ClsRealSys 와 동일한 RelayOn/InEvent 시그니처
    /// </summary>
    public class ClsDingtian : IDisposable
    {
        public delegate void eventInput(int Port, bool On);
        public event eventInput InEvent;

        private string Ip = "";
        private int NetPort = 60001;
        private int InputPollMs = 500;
        private bool PreferUdp = false;
        private Thread PollingThread;
        private bool Stop = false;
        private bool[] LastInput = new bool[8];
        private bool LoggedSample = false;
        private int FailCnt = 0;
        private int _udpProbe = 0;   // UDP 우선 모드에서 주기적 TCP 재시도 카운터

        public ClsDingtian(string ip, int port)
        {
            Ip = ip;
            NetPort = port > 0 ? port : 60001;
            // HTTP 핸드셰이크 오버헤드 제거 (입력 폴링 응답성 향상)
            try
            {
                ServicePointManager.Expect100Continue = false;
                ServicePointManager.UseNagleAlgorithm = false;
                ServicePointManager.DefaultConnectionLimit = 16;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            }
            catch { }
        }

        /// <summary>입력 폴링 시작 (HTTP /input.cgi). 차량 검지 응답성 위해 100ms 권장</summary>
        public void StartInputPolling(int pollMs)
        {
            // 30ms ~ 5000ms 사이로 제한. 너무 짧으면 보드 부하 증가
            if (pollMs < 30) pollMs = 30;
            if (pollMs > 5000) pollMs = 5000;
            InputPollMs = pollMs;
            Stop = false;
            Util.Logger.Log(string.Format("DINGTIAN 입력 폴링 시작: http://{0}/input.cgi (주기 {1}ms)", Ip, InputPollMs));
            PollingThread = new Thread(InputLoop);
            PollingThread.IsBackground = true;
            PollingThread.Start();
        }

        public void StopInputPolling()
        {
            Stop = true;
            try { if (PollingThread != null && PollingThread.IsAlive) PollingThread.Join(1000); }
            catch { }
            PollingThread = null;
        }

        /// <summary>
        /// 릴레이 펄스 — KJC1000.RelayOn 과 동일 시그니처. delay ms 후 ON, keep ms 후 OFF
        /// </summary>
        public void RelayOn(int port, int delay, int keep)
        {
            if (port < 1 || port > 8) return;
            try
            {
                if (delay > 0) Thread.Sleep(delay);
                byte[] onCmd = Encoding.ASCII.GetBytes(string.Format("1{0}", port));
                byte[] offCmd = Encoding.ASCII.GetBytes(string.Format("2{0}", port));
                int holdMs = keep > 0 ? keep : 800;
                // TCP를 안 받는 보드(UDP 전용)에서 매 차단기마다 TCP 타임아웃(지연)이 걸리지 않도록:
                //  - TCP 실패하면 PreferUdp 전환 → 이후 차단기는 곧바로 UDP(지연 없음)
                //  - 단 30회마다 TCP 한 번 재시도해 복구 감지(영구 고착 방지)
                bool tryTcp = !PreferUdp || (++_udpProbe % 30 == 0);
                if (tryTcp)
                {
                    try
                    {
                        SendTcp(onCmd);
                        Thread.Sleep(holdMs);
                        SendTcp(offCmd);
                        if (PreferUdp) Util.Logger.Log("DINGTIAN TCP 복구 — TCP 전환");
                        PreferUdp = false;
                        return;
                    }
                    catch (Exception tcpEx)
                    {
                        if (!PreferUdp)
                            Util.Logger.Log(string.Format("DINGTIAN TCP 실패({0}) → UDP 전환(이후 차단기는 UDP 우선)", tcpEx.Message));
                        PreferUdp = true;
                    }
                }
                SendUdp(onCmd);
                Thread.Sleep(holdMs);
                SendUdp(offCmd);
            }
            catch (Exception ex)
            {
                Util.Logger.Log("DINGTIAN RelayOn 실패: " + ex.Message);
            }
        }

        private void SendTcp(byte[] data)
        {
            using (TcpClient tcp = new TcpClient())
            {
                tcp.SendTimeout = 600;
                tcp.ReceiveTimeout = 600;
                IAsyncResult ar = tcp.BeginConnect(Ip, NetPort, null, null);
                // 600ms: TCP 미지원 보드에서 빨리 실패→UDP 전환(첫 차단기 1회만 부담). 정상 보드는 LAN 연결 충분
                if (!ar.AsyncWaitHandle.WaitOne(600))
                    throw new SocketException((int)SocketError.TimedOut);
                tcp.EndConnect(ar);
                using (NetworkStream s = tcp.GetStream())
                {
                    s.Write(data, 0, data.Length);
                    s.Flush();
                }
            }
        }

        private void SendUdp(byte[] data)
        {
            using (UdpClient udp = new UdpClient())
            {
                udp.Send(data, data.Length, Ip, NetPort);
            }
        }

        // ---------- 입력 폴링 (HTTP /input.cgi) ----------

        private void InputLoop()
        {
            while (!Stop)
            {
                try { PollOnce(); }
                catch (Exception ex) { Util.Logger.Log("DINGTIAN 입력 폴링 예외: " + ex.Message); }
                if (Stop) break;
                Thread.Sleep(InputPollMs);
            }
        }

        private void PollOnce()
        {
            if (string.IsNullOrEmpty(Ip)) return;
            string url = string.Format("http://{0}/input.cgi", Ip);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout = 500;        // 폴링 주기 100ms이므로 짧게 (실패 시 빨리 포기하고 다음 폴링)
            req.ReadWriteTimeout = 500;
            req.Method = "GET";
            req.KeepAlive = true;     // TCP 연결 재사용 — 핸드셰이크 오버헤드 제거 → 응답성 향상
            req.Pipelined = true;
            req.UserAgent = "KyungsinLPR";
            string body;
            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                {
                    body = sr.ReadToEnd();
                }
                FailCnt = 0;
            }
            catch (WebException ex)
            {
                FailCnt++;
                if (FailCnt <= 3 || (FailCnt % 30) == 0)
                    Util.Logger.Log(string.Format("DINGTIAN HTTP 실패({0}회) {1}: {2}", FailCnt, url, ex.Message));
                return;
            }

            if (string.IsNullOrEmpty(body)) return;

            // HTML/JS 등에 둘러싸여 올 수 있음 — `&숫자&숫자&...&` 패턴만 추출
            string raw = body;
            Match m = Regex.Match(body, @"&\d+(?:&\d+)+&");
            if (m.Success) body = m.Value;

            // 첫 1회 + 변화 시 raw 응답 로그 (디버깅용)
            if (!LoggedSample)
            {
                LoggedSample = true;
                Util.Logger.Log("DINGTIAN /input.cgi 첫 응답 raw: " + (raw.Length > 200 ? raw.Substring(0, 200) : raw));
                Util.Logger.Log("DINGTIAN 추출 패턴: " + body);
            }

            string[] parts = body.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            int chCnt = 0, chOff = -1;
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                int n;
                if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out n) && n > 1)
                { chCnt = n; chOff = i; break; }
            }
            if (chOff < 0 || chCnt == 0) return;
            int max = Math.Min(chCnt, LastInput.Length);
            for (int ch = 0; ch < max; ch++)
            {
                int idx = chOff + 1 + ch;
                if (idx >= parts.Length) break;
                int v;
                if (!int.TryParse(parts[idx], NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) continue;
                // 차량 검지(루프 ON) = 입력 회로 닫힘 = 보드 응답 0 (active-low)
                // 보드 응답 1 = 입력 없음 (default high). 외부 결선 표준 가정 — REALSYS 분기와 동일 의미
                bool on = (v == 0);
                if (LastInput[ch] != on)
                {
                    LastInput[ch] = on;
                    // LPR 설정의 LoopPort/Gate Port와 매칭 위해 1-based로 발행 (보드 raw ch0 → 외부 1)
                    int extPort = ch + 1;
                    Util.Logger.Log(string.Format("DINGTIAN 입력 ch{0} → port {1} {2}", ch, extPort, on ? "ON" : "OFF"));
                    if (InEvent != null)
                    {
                        try { InEvent(extPort, on); } catch { }
                    }
                }
            }
        }

        public void Dispose()
        {
            StopInputPolling();
        }
    }
}
