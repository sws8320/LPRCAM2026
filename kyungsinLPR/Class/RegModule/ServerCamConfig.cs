using System;
using System.Drawing;

namespace KyungsinLPR
{
    /// <summary>
    /// 서버모드(원격 인식) 전용 멀티카메라 설정 — 최대 15대(가로 5 × 세로 3).
    /// 기존 2카메라(CAMERA cam1/cam2) 모델과 분리된 자기완결 설정 → 기존 동작에 영향 없음.
    /// 인식은 ParkingWeb 서버에 위임하고, 캡처·DIO·전광판은 LPR이 카메라별로 관리한다.
    /// 저장 위치: Setting.ini [SVRCAM1] ~ [SVRCAM15].
    /// </summary>
    public static class ServerCamConfig
    {
        public const int MAX = 15;   // 가로 5 × 세로 3
        public const int COLS = 5;
        public const int ROWS = 3;

        public struct CamSetting
        {
            public bool Use;
            public string Name;        // 카드 제목 (예: "입구LPR")
            public string Ip;          // 카메라 IP
            public string Rtsp;        // RTSP URL(비우면 IP로 기본형 생성). EffectiveRtsp() 참고
            public bool Udp;           // RTSP UDP 전송
            public int GateType;       // 0=입구, 1=출구
            public Rectangle Roi;      // 인식 영역(ParkingWeb 로 함께 전송)
            // Dingtian DIO (카메라별)
            public string DioIp;
            public int DioPort;
            public int DioInTrigger;   // 입력 채널(차량 검지 → 캡처 트리거)
            public int DioOutGate;     // 출력 채널(차단기 개방)
            // 전광판 (카메라별)
            public string DispIp;
            public int DispPort;
        }

        public static CamSetting[] Cams = NewArray();

        private static CamSetting[] NewArray()
        {
            var a = new CamSetting[MAX];
            for (int i = 0; i < MAX; i++) a[i].Name = "CAM" + (i + 1);
            return a;
        }

        /// <summary>표시/캡처에 쓸 실제 RTSP URL. Rtsp 지정 시 그대로, IP만 있으면 기본형 생성.
        /// (기존 카메라 형식 rtsp://host:60000/h264 기준. 다르면 Rtsp 에 전체 URL 입력)</summary>
        public static string EffectiveRtsp(CamSetting c)
        {
            if (!string.IsNullOrEmpty(c.Rtsp)) return c.Rtsp.Trim();
            string ip = (c.Ip ?? "").Trim();
            if (ip.Length == 0) return "";
            if (ip.StartsWith("rtsp://", StringComparison.OrdinalIgnoreCase)) return ip;
            return "rtsp://" + ip + ":60000/h264";
        }

        /// <summary>Use=true 인 카메라 수(화면 카드 개수).</summary>
        public static int ActiveCount()
        {
            int n = 0;
            for (int i = 0; i < MAX; i++) if (Cams[i].Use) n++;
            return n;
        }

        public static void Load()
        {
            for (int i = 0; i < MAX; i++)
            {
                string sec = "SVRCAM" + (i + 1);
                CamSetting c = new CamSetting();
                c.Use = Bool(Read(sec, "use"));
                c.Name = Read(sec, "name");
                c.Ip = Read(sec, "ip");
                c.Rtsp = Read(sec, "rtsp");
                c.Udp = Bool(Read(sec, "udp"));
                c.GateType = Int(Read(sec, "gatetype"));
                c.Roi = ParseRoi(Read(sec, "roi"));
                c.DioIp = Read(sec, "dioip");
                c.DioPort = Int(Read(sec, "dioport"));
                c.DioInTrigger = Int(Read(sec, "dioin"));
                c.DioOutGate = Int(Read(sec, "dioout"));
                c.DispIp = Read(sec, "dispip");
                c.DispPort = Int(Read(sec, "dispport"));
                if (string.IsNullOrEmpty(c.Name)) c.Name = "CAM" + (i + 1);
                Cams[i] = c;
            }
        }

        public static void Save()
        {
            for (int i = 0; i < MAX; i++)
            {
                string sec = "SVRCAM" + (i + 1);
                CamSetting c = Cams[i];
                Write(sec, "use", c.Use.ToString());
                Write(sec, "name", c.Name ?? "");
                Write(sec, "ip", c.Ip ?? "");
                Write(sec, "rtsp", c.Rtsp ?? "");
                Write(sec, "udp", c.Udp.ToString());
                Write(sec, "gatetype", c.GateType.ToString());
                Write(sec, "roi", string.Format("{0},{1},{2},{3}", c.Roi.X, c.Roi.Y, c.Roi.Width, c.Roi.Height));
                Write(sec, "dioip", c.DioIp ?? "");
                Write(sec, "dioport", c.DioPort.ToString());
                Write(sec, "dioin", c.DioInTrigger.ToString());
                Write(sec, "dioout", c.DioOutGate.ToString());
                Write(sec, "dispip", c.DispIp ?? "");
                Write(sec, "dispport", c.DispPort.ToString());
            }
        }

        // ---- helpers ----
        private static string Read(string sec, string key)
        {
            try { return Util.Function.IniReadValue(sec, key) ?? ""; } catch { return ""; }
        }
        private static void Write(string sec, string key, string val)
        {
            try { Util.Function.IniWriteValue(sec, key, val); } catch { }
        }
        private static bool Bool(string s)
        {
            return !string.IsNullOrEmpty(s) && (s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1");
        }
        private static int Int(string s)
        {
            int v; return int.TryParse(s, out v) ? v : 0;
        }
        private static Rectangle ParseRoi(string s)
        {
            try
            {
                string[] p = (s ?? "").Split(',');
                if (p.Length == 4)
                    return new Rectangle(int.Parse(p[0].Trim()), int.Parse(p[1].Trim()),
                                         int.Parse(p[2].Trim()), int.Parse(p[3].Trim()));
            }
            catch { }
            return new Rectangle(0, 0, 0, 0);
        }
    }
}
