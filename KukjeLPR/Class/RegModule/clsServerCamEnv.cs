using System;

namespace KyungsinLPR
{
    /// <summary>
    /// 서버모드 추가 카메라(인덱스 2~14)의 데이터처리(정산)용 설정을 [SVRCAM{n}] 개별설정에서 구성한다.
    /// 카드 1,2(인덱스 0,1)는 기존 Lpr1Info/Lpr2Info·DioOutPut[0/1] 그대로 사용(불변).
    ///
    /// C단계: 게이트는 공유 Dingtian 박스의 '릴레이 포트 번호'로 카메라를 구분하므로
    /// ENV.CommonEnv.Dio.DioOutPut[idx] 를 카메라별 포트로 채우면 SerialDev.GateOpen(idx) 가 그대로 동작.
    /// Lpr_Info(장비번호/입출구)는 cam1(Lpr1Info)을 템플릿으로 복제 후 카메라별 값만 덮어쓴다.
    ///
    /// [중요] ENV 는 환경설정 저장/재구성 때마다 새 구조체로 재대입되어 주입이 사라진다.
    ///  → ApplyCam(idx) 를 '캡처 직전'(ProcessServerCamResult)에 호출해 현재 ENV 에 매번 주입한다(멱등).
    /// </summary>
    public static class clsServerCamEnv
    {
        private static readonly ClsStructure.Lpr_Info[] _lpr = new ClsStructure.Lpr_Info[15];
        private static readonly bool[] _loaded = new bool[15];
        private static readonly string[] _dioIp = new string[15];   // 카메라별 Dingtian 박스 IP(다중박스 게이트 라우팅용)

        /// <summary>카메라(idx)의 Dingtian 박스 IP([SVRCAM] pc_txtDioIp). 없으면 빈문자(메인 공유박스 사용).</summary>
        public static string GetDioIp(int idx) { return (idx >= 0 && idx < _dioIp.Length) ? (_dioIp[idx] ?? "") : ""; }

        /// <summary>서버모드 여부([OPTIONK] servermode). 서버모드면 모든 카메라가 LPR1 처리옵션 사용.</summary>
        public static bool ServerMode = false;

        /// <summary>[OPTIONK] camcount 만큼(인덱스 2~) 전부 적용. ENV 로드 이후에 호출해야 함.</summary>
        public static void Load()
        {
            string sm = Util.Function.IniReadValue("OPTIONK", "servermode") ?? "";
            ServerMode = sm.Equals("true", StringComparison.OrdinalIgnoreCase) || sm == "1";
            int camCount = Util.Function.IntTryParse(Util.Function.IniReadValue("OPTIONK", "camcount"));
            if (camCount < 1 || camCount > 15) camCount = 2;
            for (int i = 2; i < camCount && i < 15; i++) ApplyCam(i);
            // 카드1·2(인덱스 0,1)는 정산/게이트는 기존 그대로 두되, 네트워크 전광판만 개별설정([SVRCAM1/2])으로 교체.
            // (서버모드에서는 공통 전광판 탭이 비활성이라, 카드 개별설정이 유일한 전광판 설정 경로)
            if (ServerMode) { ApplyCardDisplay(0); ApplyCardDisplay(1); }
        }

        /// <summary>서버모드에서 카드1·2(인덱스 0,1)의 네트워크 전광판을 개별설정([SVRCAM{n}])으로 덮어쓴다.
        /// cam0/1 은 기존 DataProcess 전광판 경로(NetDisPlay1/2)를 그대로 사용하되 IP/포트/사용여부만 개별값으로 교체.
        /// 게이트/인식/정산은 건드리지 않음(전광판 한정). 개별 네트워크 전광판 미설정이면 전역값 유지.
        /// Load()(clsServerCamEnv) 가 NetDisPlay1/2 초기화보다 먼저 실행되므로, 여기서 주입하면 초기화가 개별 IP 를 사용한다.</summary>
        public static void ApplyCardDisplay(int idx)
        {
            if (idx != 0 && idx != 1) return;
            try
            {
                var dispArr = frmLprMain.ENV.CommunicationEnv.DisPlay;
                if (dispArr == null || idx >= dispArr.Length) return;
                string sec = "SVRCAM" + (idx + 1);
                string ip = Pc(sec, "pc_txtDisplay1NetIp");
                bool netUse = Pc(sec, "pc_chkDisplay1NetUse") == "1" && !string.IsNullOrEmpty(ip);
                ClsStructure.DisPlay_Info disp = dispArr[idx];
                if (!netUse)
                {
                    // 서버모드는 카메라별 개별설정이 곧 전부. 개별 네트워크 전광판이 꺼져 있으면
                    // 전역 [전광판] 값(예: 192.168.1.115)으로 송신하지 않도록 이 카메라 전광판을 끈다.
                    disp.Use = false; disp.Net.Use = false;
                    frmLprMain.ENV.CommunicationEnv.DisPlay[idx] = disp;
                    Util.Logger.Log(string.Format("[ServerCamEnv{0}] 카드 전광판 미사용 — 전역값({1}) 무시, 전송 안 함", idx + 1, disp.Net.IP));
                    return;
                }
                // 네트워크 전광판 활성(전광판 한정) — DataProcess 전광판 경로(NetDisPlay1/2)가 이 IP/문구를 사용
                disp.Use = true;        // DataProcess 전광판 경로 활성
                disp.UseFiex = true;    // 인식 시 차번 송출 경로 활성
                disp.Net.Use = true;
                disp.Net.IP = ip;
                int dport; disp.Net.Port = int.TryParse(Pc(sec, "pc_txtDisplay1NetPort"), out dport) ? dport : 5000;
                // 문구/색상도 개별설정([SVRCAM]) 값으로 교체 — 전역값 무시. (환영문구/일반/정기 모두 개별 적용)
                disp.Com.Dev_Type_Name = PcOr(sec, "pc_CmbDisplay1Type", "Color8");
                disp.Ment.Ment1Line  = Pc(sec, "pc_txtDisplay1Text1");                       // 환영문구 1열 (예: 안녕)
                disp.Ment.Ment1Color = PcOr(sec, "pc_CmbDisplayText1Color1", "녹색");
                disp.Ment.Ment2Line  = Pc(sec, "pc_txtDisplay1Text2");                       // 환영문구 2열 (예: 어서)
                disp.Ment.Ment2Color = PcOr(sec, "pc_CmbDisplayText1Color2", "백색");
                disp.NormalCar    = PcOr(sec, "pc_txtNormalCar1", "일반차량");
                disp.Normal1Color = PcOr(sec, "pc_CmbDisplayTextNormal1Color1", "녹색");
                disp.Normal2Color = PcOr(sec, "pc_CmbDisplayTextNormal1Color2", "청록");
                disp.PeriodCar    = PcOr(sec, "pc_txtPeriodCar1", "정기차량");
                disp.Period1Color = PcOr(sec, "pc_CmbDisplayTextPeriod1Color1", "백색");
                disp.Period2Color = PcOr(sec, "pc_CmbDisplayTextPeriod1Color2", "분홍");
                frmLprMain.ENV.CommunicationEnv.DisPlay[idx] = disp;
                Util.Logger.Log(string.Format("[ServerCamEnv{0}] 카드 전광판 개별적용 {1}:{2} 환영='{3}/{4}' 일반='{5}' 정기='{6}'",
                    idx + 1, disp.Net.IP, disp.Net.Port, disp.Ment.Ment1Line, disp.Ment.Ment2Line, disp.NormalCar, disp.PeriodCar));
            }
            catch (Exception ex) { Util.Logger.Log(string.Format("[ServerCamEnv{0}] 카드 전광판 적용 오류: {1}", idx + 1, ex.Message)); }
        }

        /// <summary>카메라(idx) 한 대의 [SVRCAM{n}] 설정을 현재 ENV 에 주입 + Lpr 캐시. 멱등·널안전.</summary>
        public static void ApplyCam(int idx)
        {
            if (idx < 2 || idx >= 15) return;
            try
            {
                // ENV 배열이 아직 준비 안 됐으면(설정 로드 전) 조용히 패스 — 캡처 시 재호출됨
                var dioArr = frmLprMain.ENV.CommonEnv.Dio.DioOutPut;
                var dispArr = frmLprMain.ENV.CommunicationEnv.DisPlay;
                if (dioArr == null || dispArr == null || idx >= dioArr.Length || idx >= dispArr.Length)
                    return;

                string sec = "SVRCAM" + (idx + 1);

                // --- 장비 정보(Lpr_Info): cam1 템플릿 복제 후 카메라별 값 덮어쓰기 ---
                ClsStructure.Lpr_Info lpr = frmLprMain.ENV.CommunicationEnv.Lpr1Info;   // 구조체=값복사(LprOpt/SockInfo 상속)
                string chNo = Pc(sec, "pc_txtLPRNo1");
                if (string.IsNullOrEmpty(chNo)) chNo = Util.Function.IniReadValue(sec, "name");
                if (string.IsNullOrEmpty(chNo)) chNo = "SVR" + (idx + 1);
                lpr.Use = true;
                lpr.ChNo = chNo;
                lpr.Name = chNo;
                int eqpm; if (int.TryParse(Pc(sec, "pc_txtEqpmNo1"), out eqpm)) lpr.EqpmNo = eqpm;
                lpr.InOutType = ParseInOut(Pc(sec, "pc_CmbLPRInOut1"), lpr.InOutType);
                ClsStructure.ProcessOption svrOpt;
                if (TryGetLprOpt(idx, lpr.InOutType, out svrOpt)) lpr.LprOpt = svrOpt;   // 개별 입출차 처리옵션 적용(없으면 cam1 상속 유지)
                _lpr[idx] = lpr;
                _loaded[idx] = true;

                // --- 게이트(DioOutPut[idx]): gate1 템플릿 복제 후 포트만 카메라별 ---
                ClsStructure.Dio_OutPut gate = dioArr[0];   // delay/keep 등 상속
                int port; if (int.TryParse(Pc(sec, "pc_CmbGate1Port"), out port) && port > 0) gate.Port = port;
                int dly; if (int.TryParse(Pc(sec, "pc_txtGate1PortDelay"), out dly)) gate.Delay = dly;
                int keep; if (int.TryParse(Pc(sec, "pc_txtGate1PortKeep"), out keep)) gate.Keep = keep;
                gate.Use = true;
                gate.AddPort = -1;   // 2번(추가) 포트는 서버캠에서 미사용
                frmLprMain.ENV.CommonEnv.Dio.DioOutPut[idx] = gate;
                _dioIp[idx] = Pc(sec, "pc_txtDioIp");   // 카메라별 Dingtian 박스 IP(다중박스 라우팅)

                // --- 전광판(DisPlay[idx]): 직렬은 비활성(오출력 방지), 네트워크 전광판은 [SVRCAM]에서 구성 ---
                ClsStructure.DisPlay_Info disp = dispArr[idx];
                disp.Use = false;   // 직렬 전광판 미사용 — 서버캠은 네트워크 전광판만(ProcessServerCamResult가 송신)
                disp.Com.Dev_Type_Name = PcOr(sec, "pc_CmbDisplay1Type", "Color8");
                disp.Ment.Ment1Line  = Pc(sec, "pc_txtDisplay1Text1");
                disp.Ment.Ment1Color = PcOr(sec, "pc_CmbDisplayText1Color1", "녹색");
                disp.Ment.Ment2Line  = Pc(sec, "pc_txtDisplay1Text2");
                disp.Ment.Ment2Color = PcOr(sec, "pc_CmbDisplayText1Color2", "백색");
                disp.NormalCar   = PcOr(sec, "pc_txtNormalCar1", "일반차량");
                disp.Normal1Color = PcOr(sec, "pc_CmbDisplayTextNormal1Color1", "녹색");
                disp.Normal2Color = PcOr(sec, "pc_CmbDisplayTextNormal1Color2", "청록");
                disp.PeriodCar   = PcOr(sec, "pc_txtPeriodCar1", "정기차량");
                disp.Period1Color = PcOr(sec, "pc_CmbDisplayTextPeriod1Color1", "백색");
                disp.Period2Color = PcOr(sec, "pc_CmbDisplayTextPeriod1Color2", "분홍");
                disp.Net.Use = Pc(sec, "pc_chkDisplay1NetUse") == "1";
                disp.Net.IP  = Pc(sec, "pc_txtDisplay1NetIp");
                int dport; disp.Net.Port = int.TryParse(Pc(sec, "pc_txtDisplay1NetPort"), out dport) ? dport : 5000;
                frmLprMain.ENV.CommunicationEnv.DisPlay[idx] = disp;

                Util.Logger.Log(string.Format("[ServerCamEnv{0}] 정산설정 적용 장비번호={1} ChNo={2} 입출구={3} 게이트포트={4} 전광판={5}",
                    idx + 1, lpr.EqpmNo, lpr.ChNo, lpr.InOutType == 1 ? "출구" : "입구", gate.Port,
                    disp.Net.Use ? (disp.Net.IP + ":" + disp.Net.Port) : "없음"));
            }
            catch (Exception ex) { Util.Logger.Log(string.Format("[ServerCamEnv{0}] 설정 적용 오류: {1}", idx + 1, ex.Message)); }
        }

        /// <summary>카메라(idx)의 Lpr_Info. 미로드면 즉시 적용 시도 후 반환. 0/1/실패는 기존 cam1/cam2.</summary>
        public static ClsStructure.Lpr_Info GetLpr(int idx)
        {
            if (idx >= 2 && idx < 15)
            {
                if (!_loaded[idx]) ApplyCam(idx);
                if (_loaded[idx]) return _lpr[idx];
            }
            return idx == 1 ? frmLprMain.ENV.CommunicationEnv.Lpr2Info : frmLprMain.ENV.CommunicationEnv.Lpr1Info;
        }

        /// <summary>카메라(idx)의 입출구 타입(0=입구용,1=출구용).</summary>
        public static int GetInOut(int idx)
        {
            return GetLpr(idx).InOutType;
        }

        /// <summary>카메라(idx)의 입출차 처리옵션(LprOpt)을 개별설정([SVRCAM])에서 읽는다.
        ///  방향(입구/출구)은 LPR 입출구분(CmbLPRInOut1)으로 별도 결정. 처리옵션은 방향 무관 **LPR1(입차/Ent) 섹션** 사용.
        ///  percam_configured 아니면 false(글로벌 유지).</summary>
        public static bool TryGetLprOpt(int idx, int inOutType, out ClsStructure.ProcessOption opt)
        {
            opt = default(ClsStructure.ProcessOption);
            if (idx < 0 || idx >= 15) return false;
            string sec = "SVRCAM" + (idx + 1);
            if (!"true".Equals(Util.Function.IniReadValue(sec, "percam_configured"), StringComparison.OrdinalIgnoreCase))
                return false;
            const string pfx = "Ent";   // 처리옵션은 항상 LPR1(입차) 섹션. 입/출 방향은 InOutType 으로 따로 처리
            opt.Period_SendData = PcBool(sec, "pc_chkPCar" + pfx + "Send");
            opt.Period_Lprtrns  = PcBool(sec, "pc_chkPCar" + pfx + "Lprtrns");
            opt.Period_Passtrns = PcBool(sec, "pc_chkPCar" + pfx + "Passtrns");
            opt.Period_Counter  = PcBool(sec, "pc_chkPCar" + pfx + "Countting");
            opt.Period_Gate     = PcBool(sec, "pc_chkPCar" + pfx + "Gate");
            opt.Normal_SendData = PcBool(sec, "pc_chkNCar" + pfx + "Send");
            opt.Normal_Lprtrns  = PcBool(sec, "pc_chkNCar" + pfx + "Lprtrns");
            opt.Normal_Tckttrns = PcBool(sec, "pc_chkNCar" + pfx + "Tckttrns");
            opt.Normal_Counter  = PcBool(sec, "pc_chkNCar" + pfx + "Countting");
            opt.Normal_Gate     = PcBool(sec, "pc_chkNCar" + pfx + "Gate");
            return true;
        }

        private static bool PcBool(string sec, string key) { return Pc(sec, key) == "1"; }

        private static string Pc(string sec, string key)
        {
            return (Util.Function.IniReadValue(sec, key) ?? "").Trim();
        }

        private static string PcOr(string sec, string key, string def)
        {
            string v = Pc(sec, key);
            return string.IsNullOrEmpty(v) ? def : v;
        }

        // ComboBox 표시 텍스트("입구용"/"출구용") 또는 인덱스("0"/"1") → InoutType
        private static int ParseInOut(string v, int def)
        {
            if (string.IsNullOrEmpty(v)) return def;
            if (v.Contains("출구")) return 1;
            if (v.Contains("입구")) return 0;
            int n; if (int.TryParse(v, out n) && (n == 0 || n == 1)) return n;
            return def;
        }
    }
}
