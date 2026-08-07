using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.IO.Ports;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace KyungsinLPR {
    public class clsDataTransaction {
        public DataTable CustDef = new DataTable();
        private DataTable DCList = new DataTable();
        private DataTable WrongDef = new DataTable();
        private DataTable VisitorDef = new DataTable();   // 방문차량 명단(FC_MASTER.Visitor) — 정기권처럼 주기적 로드
        private int VisitorMonthlyHours = 0;              // 세대 월 총 주차시간 한도(시간). 0=무제한. GetMaster에서 VisitorPolicy 로드

        public DateTime LastGetMst;

        private SqlConnection MCon = new SqlConnection();
        private SqlConnection TCon = new SqlConnection();

        private Thread QueryThread;

        private clsSerialPort SerialDev = null;
        private NetworkDisplay NetDev = null;

        private bool[] RegedCar = new bool[15];   // 서버모드 N대(0~14). 0/1=기존, 2~14=서버캠
        private bool[] _lastReged = new bool[15];  // DataProcess 끝(1079)에서 RegedCar 리셋 전 값 보존(표시용)

        /// <summary>직전 DataProcess 의 정기권 등록차량 여부(카드 일반/정기 표시·전광판용).
        ///  RegedCar 는 DataProcess 끝에서 false 로 리셋되므로 리셋 전 보존값(_lastReged)을 반환.</summary>
        public bool GetRegedCar(int idx) { return idx >= 0 && idx < _lastReged.Length && _lastReged[idx]; }

        /// <summary>모든 DataProcess 호출 직렬화용 공유 락 — 단일 DB연결(TCon)·NetDev 동시사용 방지(N대 동시 캡처 대비).
        ///  cam0/1(AfterRegPlateCam)·서버캠(ProcessServerCamResult) 모두 이 락으로 감싼다.</summary>
        public static readonly object ProcLock = new object();

        public bool Processing = false;

        private struct QueryStruct {
            public bool MST;
            public string Query;
        }

        //private List<QueryStruct> QList = new List<QueryStruct>();
        private ClsStructure.EnvStruct Env = new ClsStructure.EnvStruct();

        public clsDataTransaction(clsSerialPort _SerialDev, ClsStructure.EnvStruct _Env) {
            Env = _Env;
            SerialDev = _SerialDev;
            //DB = new Mssql(Env.CommonEnv.DBInfo);
            //MCon = DB.OpenMDB();
            //TCon = DB.OpenTDB();
            //QueryThread = new Thread(new ThreadStart(execquery));
            //QueryThread.IsBackground = true;
            //QueryThread.Start();
            Util.clsMssql.Dbinfo.Server = Env.CommonEnv.DBInfo.Ip;
            Util.clsMssql.Dbinfo.id = Env.CommonEnv.DBInfo.Id;
            Util.clsMssql.Dbinfo.pw = Env.CommonEnv.DBInfo.Pw;
            Util.clsMssql.Dbinfo.db = Env.CommonEnv.DBInfo.MstDB;
            MCon = Util.clsMssql.OpenDB();
            Util.clsMssql.Dbinfo.db = Env.CommonEnv.DBInfo.TrnsDb;
            TCon = Util.clsMssql.OpenDB();
            if(!Util.clsMssql.isStatus()) {
                Util.clsMssql.OpenDB();
            }
        }

        public clsDataTransaction(NetworkDisplay _NetDev, ClsStructure.EnvStruct _Env) {
            Env = _Env;
            NetDev = _NetDev;
            //DB = new Mssql(Env.CommonEnv.DBInfo);
            //MCon = DB.OpenMDB();
            //TCon = DB.OpenTDB();
            //QueryThread = new Thread(new ThreadStart(execquery));
            //QueryThread.IsBackground = true;
            //QueryThread.Start();
            if(!Util.clsMssql.isStatus()) {
                Util.clsMssql.OpenDB();
            }
        }

        //private void execquery()
        //{
        //    //오류 쿼리 Db재접속시 재처리 로직 추가 필요 이과장 요청 사항*********************
        //    while (true)
        //    {
        //        string Job = string.Empty;
        //        string errsql = string.Empty;
        //        try
        //        { 
        //            //DB Connection Check
        //            Job = "DB OPEN";
        //            if (MCon == null)
        //            {
        //                //DB = new Mssql(Env.CommonEnv.DBInfo);
        //                MCon = DB.OpenMDB();
        //            }
        //            else if (MCon.State != ConnectionState.Open)
        //                MCon = DB.OpenMDB();
        //            if (TCon == null)
        //            {
        //                //DB = new Mssql(Env.CommonEnv.DBInfo);
        //                TCon = DB.OpenTDB();
        //            }
        //            else if (TCon.State != ConnectionState.Open)
        //                TCon = DB.OpenTDB();
        //            Job = "QUERY";
        //            while(QList.Count > 0)
        //            {
        //                SqlConnection con = null;
        //                if (QList[0].MST)
        //                    con = MCon;
        //                else
        //                    con = TCon;
        //                if (con != null)
        //                {
        //                    errsql = QList[0].Query;
        //                    if (QList[0].Query != null && !QList[0].Query.Equals(string.Empty))
        //                    {
        //                        //if (DB.ExecQuery(con, QList[0].Query))
        //                        //{
        //                        //    QList.Remove(QList[0]);
        //                        //}
        //                        if (DB.ExecQuery(con, QList[0].Query))
        //                            QList.Remove(QList[0]);
        //                        else
        //                        {
        //                            if (QList[0].MST)
        //                                MCon = DB.OpenMDB();
        //                            else
        //                                TCon = DB.OpenTDB();
        //                        }
        //                    }
        //                }
        //                else
        //                    break;
        //            }

        //            TimeSpan diff = DateTime.Now - LastGetMst;
        //            //20170228 기존 10분 에서 1분으로 변경
        //            if (diff.TotalMinutes > 1)
        //            {
        //                if (MCon != null)
        //                {
        //                    if (MCon.State == ConnectionState.Open)
        //                        GetMaster();
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (e.Message != "ExecuteNonQuery: Connection 속성이 초기화되지 않았습니다.")
        //                Util.Logger.Log(string.Format("execquery {0} 처리 중 오류 {1} {2}", Job, e.Message, errsql));
        //        }
        //        finally
        //        {
        //            Thread.Sleep(100);
        //        }
        //    }
        //}

        public void GetMaster() {
            if(GetMasterInfo.Use) return;
            LastGetMst = DateTime.Now;
            try {
                string query = "SELECT iExtendLotArea, custdef.*, \r\n";
                query += "CASE Len(Custdef.acplate1) \r\n";
                query += "  WHEN 7 THEN \r\n";
                query += "      SUBSTRING(Custdef.acplate1, 1 , 2) + SUBSTRING(Custdef.acplate1, 4 , 4) \r\n";
                query += "  WHEN 9 THEN \r\n";
                query += "      SUBSTRING(Custdef.acplate1, 3 , 2) + SUBSTRING(Custdef.acplate1, 6 , 4) \r\n";
                query += "  ELSE Custdef.acPlate1 \r\n";
                query += "END as SixDigit,  \r\n";
                query += "CASE Len(Custdef.acplate1) \r\n";
                query += "  WHEN 7 THEN \r\n";
                query += "	    SUBSTRING(Custdef.acplate1, 4 , 4) \r\n";
                query += "  WHEN 9 THEN \r\n";
                query += "	    SUBSTRING(Custdef.acplate1, 6 , 4) \r\n";
                query += "  ELSE Custdef.acPlate1 \r\n";
                query += string.Format("END as FourDigit FROM {0}.dbo.Custdef ", frmLprMain.ENV.CommonEnv.DBInfo.MstDB);
                query += string.Format("inner join(select acplate1, max(isnull(iModifiedDate, dtregistdate)) iModifiedDate from {0}.dbo.custdef group by ilotarea, acplate1) grp \r\n", frmLprMain.ENV.CommonEnv.DBInfo.MstDB);
                query += string.Format("on custdef.acPlate1 = grp.acPlate1 and isnull(CUSTDEF.iModifiedDate, dtregistdate) = grp.iModifiedDate\r\n");
                query += string.Format("left outer join {0}.dbo.AREADEF on custdef.iLotArea = AREADEF.iLotArea\t\n", frmLprMain.ENV.CommonEnv.DBInfo.MstDB);
                // 그룹 옵션을 정기차량 로드 단계에서 반영 → 이후 전 구간(입차·출차·전광판·차단기)이 자동으로 일반/정기 처리
                //  · 예외 그룹(clsExceptGroup)      : 선택 그룹은 정기 로드에서 '제외'    → 그 그룹은 일반차량으로 처리
                //  · 특정 그룹만 정기(SpecialGroup) : 선택 그룹만 정기 로드('만 포함')     → 나머지 그룹은 일반차량으로 처리
                string custWhere = "";
                if(frmLprMain.ENV.RegCarControl.Ilotarea)
                    custWhere = string.Format("Custdef.iLotarea = '{0}'", frmLprMain.ENV.CommunicationEnv.ParkInfo.No);
                if(clsExceptGroup.ExceptGrpNo >= 0)
                    custWhere += (custWhere == "" ? "" : " and ") + string.Format("isnull(Custdef.iGroup,0) <> {0}", clsExceptGroup.ExceptGrpNo);
                if(SpecialGroup.GroupIdx > 0)
                    custWhere += (custWhere == "" ? "" : " and ") + string.Format("isnull(Custdef.iGroup,0) = {0}", SpecialGroup.GroupIdx);
                if(custWhere != "")
                    query += "where " + custWhere + " \r\n";
                CustDef = Util.clsMssql.GetTable(MCon, query);
                Util.Logger.Log(string.Format("정기권 정보 {0}건 취득", CustDef.Rows.Count));
                DCList = Util.clsMssql.GetTable(MCon, string.Format("select * from {0}.dbo.Customer_DcList", frmLprMain.ENV.CommonEnv.DBInfo.MstDB));

                WrongDef = Util.clsMssql.GetTable(MCon, string.Format("Select * from {0}.dbo.WrongCarDef", frmLprMain.ENV.CommonEnv.DBInfo.MstDB));

                // 방문차량 명단 — 방문자 처리 사용 현장만 로드(미사용 현장은 조회 생략). 사용중(iUseFlg=0)+미만료(dtValidEnd>=오늘). 유효 시작/종료는 매칭 시 재확인.
                if(frmLprMain.ENV.CommunicationEnv.UseVisitor) {
                    try {
                        string vq = string.Format("select iid, iLotArea, isnull(acDong,'') acDong, isnull(acHo,'') acHo, acPlate1, dtValidStart, dtValidEnd from {0}.dbo.Visitor where isnull(iUseFlg,0)=0 and dtValidEnd >= convert(nvarchar(10), getdate(), 121)", frmLprMain.ENV.CommonEnv.DBInfo.MstDB);
                        if(frmLprMain.ENV.RegCarControl.Ilotarea)
                            vq += string.Format(" and iLotArea = {0}", frmLprMain.ENV.CommunicationEnv.ParkInfo.No);
                        VisitorDef = Util.clsMssql.GetTable(MCon, vq);
                        Util.Logger.Log(string.Format("방문차량 정보 {0}건 취득", VisitorDef.Rows.Count));
                    } catch(Exception ve) {
                        Util.Logger.Log(string.Format("방문차량 명단 취득 오류 {0}", ve.Message));
                    }
                    // 세대 월 총 주차시간 한도 — ParkingWeb가 VisitorPolicy에 저장. 초과 시 입차 차단(월말까지). 0=무제한.
                    try {
                        string mq = string.Format("select isnull(iMonthlyHours,0) mh from {0}.dbo.VisitorPolicy where iLotArea = {1}", frmLprMain.ENV.CommonEnv.DBInfo.MstDB, frmLprMain.ENV.CommunicationEnv.ParkInfo.No);
                        DataTable mdt = Util.clsMssql.GetTable(MCon, mq);
                        VisitorMonthlyHours = (mdt != null && mdt.Rows.Count > 0) ? Util.Function.IntTryParse(mdt.Rows[0]["mh"].ToString()) : 0;
                    } catch(Exception me) {
                        VisitorMonthlyHours = 0;
                        Util.Logger.Log(string.Format("방문차 월한도 취득 오류 {0}", me.Message));
                    }
                } else {
                    VisitorDef = new DataTable();
                    VisitorMonthlyHours = 0;
                }
            } catch(Exception e) {
                Util.Logger.Log(string.Format("GetMaster 오류 {0}", e.Message));
                LastGetMst = LastGetMst.AddMinutes(-9);
            }
        }

        public string DataProcess(int Type, ClsStructure.EnvStruct Env, int CamIdx, string CarNo, string Image, string CaptureTime = "", int irate = 0) {
            string Rtn = string.Empty;
            bool blNoDriving = false;
            bool blNoDriving_Ent = false;
            bool blWriteLprTrns = false;
            bool blNoDrivingException2 = false; // iPsscrdZone=2 부제 제외 대상 여부
            Processing = true;
            DataTable dt = new DataTable();
            try {
                if(Env.CommunicationEnv.DisPlay[0].Net.Use && CamIdx == 0)
                    NetDev = frmLprMain.NetDisPlay1;
                else if(Env.CommunicationEnv.DisPlay[1].Net.Use && CamIdx == 1)
                    NetDev = frmLprMain.NetDisPlay2;
                else if(CamIdx >= 2)
                    NetDev = null;   // 서버캠(2~14): 네트워크 전광판은 후속 단계, 직렬 오출력 방지
                TimeSpan diff = DateTime.Now - LastGetMst;

                ClsStructure.Lpr_Info LprInfo = new ClsStructure.Lpr_Info();
                switch(CamIdx) {
                    case 0:
                        LprInfo = Env.CommunicationEnv.Lpr1Info;
                        break;
                    case 1:
                        LprInfo = Env.CommunicationEnv.Lpr2Info;
                        break;
                    default:
                        LprInfo = clsServerCamEnv.GetLpr(CamIdx);   // 서버캠(2~14): [SVRCAM] 장비번호/입출구, LprOpt=cam1 상속
                        break;
                }
                // 서버모드: 각 카메라의 입출차 처리옵션(LprOpt: 차단기/소켓/LprTrns 등)을 개별설정([SVRCAM])에서 적용.
                //  입구면 Ent 섹션, 출구면 Exit 섹션 체크값. 개별설정 안 된 카메라는 글로벌(switch 결과) 유지.
                //  (cam0/1 포함 — cam2가 글로벌 LPR2를 보던 문제도 개별값으로 일원화)
                if(clsServerCamEnv.ServerMode)
                {
                    ClsStructure.ProcessOption svrOpt;
                    if(clsServerCamEnv.TryGetLprOpt(CamIdx, LprInfo.InOutType, out svrOpt))
                        LprInfo.LprOpt = svrOpt;
                }

                // 소켓(요금계산기/사무실) 전송용 채널명·STXETX — 서버캠(CamIdx>=2)은 IPCamera1/2Info 가 없으므로
                //  채널명은 자기 장비채널(LprInfo.ChNo), STXETX 는 cam1 설정 상속. (기존 0/1 동작 불변)
                string camChName = (CamIdx == 0) ? Env.CameraEnv.IPCamera1Info.ChName
                                 : (CamIdx == 1) ? Env.CameraEnv.IPCamera2Info.ChName
                                 : LprInfo.ChNo;
                bool camStxEtx = (CamIdx == 1) ? Env.CameraEnv.IPCamera2Info.SendStxEtx
                               : Env.CameraEnv.IPCamera1Info.SendStxEtx;

                RegedCar[CamIdx] = false;

                string AlertMsg = string.Empty;

                bool RegResult = false;
                DateTime ProcTime = DateTime.Now;

                if(CaptureTime == string.Empty)
                    ProcTime = DateTime.Now;
                else
                    ProcTime = Util.Function.DateTimeTryParse(CaptureTime); // DateTime.Now;

                if(ProcTime == DateTime.MinValue)
                    ProcTime = DateTime.Now;

                DataRow[] RegedInfo = null;
                DataRow[] row;

                if(Type.Equals((int)ClsStructure.InoutType.입구용))
                    Util.Logger.Log(string.Format("****입차 프로세스 처리 시작"));
                else
                    Util.Logger.Log(string.Format("****출차 프로세스 처리 시작"));

                if(CarNo.Trim().Equals(string.Empty))
                    CarNo = "No_Detection";

                if(WrongDef.Rows.Count > 0) {
                    row = WrongDef.Select(string.Format("WrongCarno = '{0}'", CarNo));
                    if(row.Length > 0) {
                        Util.Logger.Log(string.Format("오인식 번호 조회 후 차량번호 변경 {0} => {1}", CarNo, row[0]["RightCarNo"].ToString()));
                        Rtn += string.Format("오인식 번호 조회 후 차량번호 변경 {0} => {1} {2}", CarNo, row[0]["RightCarNo"].ToString(), '\n');
                        CarNo = row[0]["RightCarNo"].ToString();
                    }
                }

                string ControlMent = "";
                bool isVisitorExit = false;  // 출차 방문차 여부 (전광판 '방문차량' 표시 + VisitorTrns 출차기록용) — 표시/기록 블록과 같은 스코프에 선언
                DataRow visitorExitRow = null;  // 출차 방문차 DataRow (VisitorTrns 출차 UPDATE용)
                //Util.Logger.Log("오인식 정보 검색" + Env.CameraEnv.RegModule.ToString());
                //정기권 조회 오인식 미인식 차량 제외
                string Number = string.Empty;
                if(!CarNo.Equals("No_Detection")) {
                    RegResult = true;
                    int Num = 0;
                    if(Env.CommunicationEnv.RegCorrection.Equals((int)ClsStructure.reg_correction.digit4)) {
                        Util.Logger.Log(string.Format("차량번호 4자리 인식 시작"));
                        Rtn += string.Format("차량번호 4자리 인식 시작 {0}", '\n');
                        Number = Util.Common.Right(CarNo, 4);
                    } else if(Env.CommunicationEnv.RegCorrection.Equals((int)ClsStructure.reg_correction.digit6)) {
                        Util.Logger.Log(string.Format("차량번호 6자리 인식 시작"));
                        Rtn += string.Format("차량번호 6자리 인식 시작 {0}", '\n');
                        for(int i = 0; i < CarNo.Length; i++) {
                            if(int.TryParse(CarNo.Substring(i, 1), out Num))
                                Number += CarNo.Substring(i, 1);
                        }
                    }

                    Util.Logger.Log(string.Format("정기 차량 조회 {0} {1} {2}", Env.CommunicationEnv.RegCorrection, CarNo, Number));
                    RegedInfo = FindRegedCar(Env.CommunicationEnv.RegCorrection, CarNo, Number);
                    if(RegedInfo == null) RegedInfo = new DataRow[0];

                    // [방문차량] 입차만 LPR이 직접 처리(전광판 "방문차량"+차번 / 게이트 / 입차기록 TCKTTRNS+VisitorTrns).
                    //   출차는 인터셉트하지 않고 정상흐름으로 무인정산기(ParkingWeb)에 전달 → 무인정산기가 방문차 인식→무료출차 처리.
                    //   (정기차 아닐 때만, 미인식 제외)
                    if(Env.CommunicationEnv.UseVisitor && Type.Equals((int)ClsStructure.InoutType.입구용) && RegedInfo.Length == 0 && !CarNo.Equals("No_Detection")) {
                        DataRow[] vinfo = FindVisitorCar(Env.CommunicationEnv.RegCorrection, CarNo, Number);
                        if(vinfo != null && vinfo.Length > 0) {
                            string vres = ProcessVisitor(Type, Env, CamIdx, CarNo, Image, ProcTime, LprInfo, vinfo[0], Rtn);
                            if(vres != null) return vres;   // 방문차로 처리 완료
                            // vres==null: 방문차 월 이용시간 초과 → 아래 일반차량 입출차 흐름으로 폴백 처리
                        }
                    }

                    // [방문차량] 출차는 무인정산기(ParkingWeb)가 처리하지만, 로컬 전광판에 '일반차량'이 먼저 떴다가
                    //   무인정산기의 '방문차량'으로 바뀌는 깜빡임 발생 → 출차 방문차면 로컬 전광판도 처음부터
                    //   '방문차량'으로 표시(무인정산기와 동일 문구/색)해 깜빡임 제거. 정산 전달/게이트 흐름은 그대로.
                    if(Env.CommunicationEnv.UseVisitor && Type.Equals((int)ClsStructure.InoutType.출구용) && RegedInfo.Length == 0 && !CarNo.Equals("No_Detection")) {
                        DataRow[] vexit = FindVisitorCar(Env.CommunicationEnv.RegCorrection, CarNo, Number);
                        isVisitorExit = (vexit != null && vexit.Length > 0);
                        if(isVisitorExit) visitorExitRow = vexit[0];
                        else {
                            // 등록 만료/삭제됐어도 미출차 방문차 입차기록 있으면 방문차 출차로 처리(전광판 '방문차량' + VisitorTrns 출차)
                            DataRow openV = FindOpenVisitorTrns(CarNo);
                            if(openV != null) { isVisitorExit = true; visitorExitRow = openV; }
                        }
                    }

                    Env.RegCarControl.iControlType = 0;
                    // 그룹 제한은 기본적으로 입구용에서 적용되며, UseExitGroupGate가 켜져 있으면 출구용에도 적용된다.
                    bool applyGroupGate = Type.Equals((int)ClsStructure.InoutType.입구용) ||
                                          (Type.Equals((int)ClsStructure.InoutType.출구용) && Env.RegCarControl.UseExitGroupGate);
                    if(applyGroupGate) {
                        if(Env.RegCarControl.UseGroupGate && RegedInfo.Length > 0) {
                            if(Env.RegCarControl.GateGroupNo > 0) {
                                bool process = true;
                                if(Env.RegCarControl.GroupUseTime) {
                                    string[] start = Env.RegCarControl.GroupStart.Split(':');
                                    string[] end = Env.RegCarControl.GroupEnd.Split(':');
                                    DateTime dtstart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Util.Function.IntTryParse(start[0]), Util.Function.IntTryParse(start[1]), 0);
                                    DateTime dtend = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Util.Function.IntTryParse(end[0]), Util.Function.IntTryParse(end[1]), 0);
                                    if(dtstart > dtend) dtend.AddDays(1);
                                    if(!(dtstart <= DateTime.Now && dtend > DateTime.Now))
                                        process = false;
                                }
                                string[] usinggroup = new string[13] { RegedInfo[0]["iusingarea00"].ToString(), RegedInfo[0]["iusingarea01"].ToString(),
                                RegedInfo[0]["iusingarea02"].ToString(), RegedInfo[0]["iusingarea03"].ToString(),
                                RegedInfo[0]["iusingarea04"].ToString(), RegedInfo[0]["iusingarea05"].ToString(),
                                RegedInfo[0]["iusingarea06"].ToString(), RegedInfo[0]["iusingarea07"].ToString(),
                                RegedInfo[0]["iusingarea08"].ToString(), RegedInfo[0]["iusingarea09"].ToString(),
                                RegedInfo[0]["iusingarea10"].ToString(), RegedInfo[0]["iusingarea11"].ToString(), RegedInfo[0]["iusingarea12"].ToString() };
                                if(process) {
                                    // 정기권 차량의 등록 게이트그룹 목록(usinggroup)에 본 LPR의 소속 그룹(GateGroupNo)이
                                    // 포함돼 있으면 → 다른 제한 그룹 체크를 건너뛰고 정기차로 통과시킨다.
                                    // (기존: 어느 그룹이든 "제한 그룹"에 걸리면 막혔음 — 복수 게이트그룹 등록 시
                                    //  소속 그룹이 있어도 다른 그룹 때문에 입차 거부되는 문제 수정)
                                    int myIdx = Env.RegCarControl.GateGroupNo - 1;  // 0-based
                                    bool inMyGroup = (myIdx >= 0 && myIdx < 13 && usinggroup[myIdx] == "1");

                                    if(!inMyGroup) {
                                        int findidx = -1;
                                        for(int i = 0; i < 13; i++) {
                                            if(Env.RegCarControl.GateGroupNo != i + 1 &&
                                                Env.RegCarControl.GroupUse[i] && Env.RegCarControl.GroupUse[i] == (usinggroup[i] == "1")) {
                                                findidx = i;
                                                break;
                                            }
                                        }
                                        if(findidx >= 0) {
                                            ControlMent = Env.RegCarControl.GroupMent[findidx];
                                            RegedInfo = null;
                                            Env.RegCarControl.iControlType = 4;
                                        }
                                    }
                                    else {
                                        Util.Logger.Log(string.Format("그룹 통과: 정기권에 소속 그룹({0}) 포함 — 다른 제한 그룹 무시", Env.RegCarControl.GateGroupNo));
                                    }
                                }
                            }
                        }
                    }
                    if(Type.Equals((int)ClsStructure.InoutType.입구용)) {
                        if(Env.RegCarControl.Otherparks.Count > 0) {
                            if(Env.RegCarControl.OtherparkUse) {
                                if(Env.RegCarControl.OtherparksTimeuse) {
                                    if(Env.RegCarControl.Otherparksstart.IndexOf(':') > 0 && Env.RegCarControl.Otherparksend.IndexOf(':') > 0) {
                                        string[] start = Env.RegCarControl.Otherparksstart.Split(':');
                                        string[] end = Env.RegCarControl.Otherparksend.Split(':');
                                        DateTime dtstart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Util.Function.IntTryParse(start[0]), Util.Function.IntTryParse(start[1]), 0);
                                        DateTime dtend = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Util.Function.IntTryParse(end[0]), Util.Function.IntTryParse(end[1]), 0);
                                        if(dtstart > dtend) dtend.AddDays(1);
                                        if(!(dtstart <= DateTime.Now && dtend < DateTime.Now)) {
                                            RegedInfo = null;
                                        }
                                    }
                                }

                                if(RegedInfo != null && RegedInfo.Length > 0) {
                                    bool find = false;
                                    foreach(park pitem in Env.RegCarControl.Otherparks) {
                                        if(pitem.parkno.ToString() == RegedInfo[0]["iExtendLotArea"].ToString()) {
                                            find = true;
                                            if(RegedInfo[0]["iLotArea"].ToString() != Env.CommunicationEnv.ParkInfo.No.ToString())
                                                ControlMent = pitem.ment;
                                            break;
                                        }
                                    }
                                    if(!find && ControlMent != "") {
                                        RegedInfo = null;
                                    }
                                }
                            }
                        }

                        if(RegedInfo != null && RegedInfo.Length > 0) {
                            if(Env.RegCarControl.Regendnotiuse) {
                                DateTime enddate = Util.Function.DateTimeTryParse(RegedInfo[0]["dtValidEndDate"].ToString() + " " + RegedInfo[0]["dtValidEndTime"].ToString() + ":00");
                                int leftday = (int)(enddate - DateTime.Now).TotalDays;
                                if(leftday <= Util.Function.IntTryParse(Env.RegCarControl.Regendnotiday)) {
                                    if(leftday > 0)
                                        ControlMent = string.Format("만료 {0}일전", leftday);
                                    else
                                        ControlMent = "만료 당일";
                                }
                            }
                        }

                        if(Env.RegCarControl.Penaltiuse) {
                            if(RegedInfo != null && RegedInfo.Length > 0) {
                                string custno = RegedInfo[0]["iUser"].ToString();
                                string query = string.Format("select * from {0}.dbo.Penalty where ilotarea = {1} and acPlate = '{2}' and (convert(varchar(10), getdate(), 121) between datefrom and dateto)", Env.CommonEnv.DBInfo.TrnsDb, Env.CommunicationEnv.ParkInfo.No, CarNo);
                                query += "";
                                dt = Util.clsMssql.GetTable(TCon, query);
                                if(dt.Rows.Count > 0) {
                                    Env.RegCarControl.iControlType = 3;
                                    Util.Logger.Log(string.Format("페널티 등록 차량"));
                                    ControlMent = Env.RegCarControl.Penaltiment;
                                    RegedInfo = null;
                                }
                            }
                        }
                    }
                    if(RegedInfo != null && RegedInfo.Length > 0) {
                        RegedCar[CamIdx] = true;
                    }
                }

                //leess 긴급차량 개방
                Util.Logger.Log("긴급차량 개방 " + RegedCar.ToString());
                if(Env.EmergencyCar) {
                    Util.Logger.Log(string.Format("긴급차량 개방 {0}", Env.EmergencyCar.ToString()));
                    Rtn += string.Format("긴급차량 개방 옵션 처리 {0} {1} ", Env.EmergencyCar.ToString(), '\n');
                    try {
                        if(CarNo.StartsWith("999") || CarNo.StartsWith("998")) {
                            GateOpen(CamIdx);
                            Rtn += string.Format("{0} 차단기 개방 {1}", CamIdx + 1, '\n');
                        }
                    } catch(Exception GateOpen_Error) {
                        Util.Logger.Log(string.Format("차단기 개방 오류 {0}", GateOpen_Error.Message));
                    }
                }

                //Balck List
                Black blInfo = new Black();
                bool BlackOutGate = true;
                bool BlackOutDisplay = false;
                blInfo = BlackList.DisplayMent(CarNo, RegedCar[CamIdx]);
                if(blInfo.Apply) {
                    Util.Logger.Log(string.Format("{0} 블랙리스트 대상 차량", CarNo));
                    if(Type.Equals((int)ClsStructure.InoutType.출구용)) {
                        BlackOutDisplay = BlackList.UseOutDisPlay;
                        BlackOutGate = !BlackList.DoNotOpenOutGate;
                        blInfo.Apply = false;
                    } else
                        BlackOutGate = false;
                }

                if(Type.Equals((int)ClsStructure.InoutType.입구용)) {
                    if(NoDriving.Check(CarNo)) {
                        // Exception2: 전광판 출력 전에 먼저 iPsscrdZone=2 여부 판단
                        if(NoDriving.Exception2 && RegedInfo != null && RegedInfo.Length > 0) {
                            if(RegedInfo[0]["iPsscrdZone"].ToString() == "2") {
                                blNoDrivingException2 = true;
                                Rtn += " (iPsscrdZone=2 부제 제외)";
                            }
                        }
                        // iPsscrdZone=2 제외 차량은 부제 LPR기록/전광판 출력 건너뜀
                        if(!blNoDrivingException2) {
                            if(NoDriving.WriteLpr) {
                                QueryStruct Noitem = new QueryStruct();
                                //LPRTRNS
                                Noitem.Query = clsQuery.SetEntranceLprTrns(ProcTime, Env.CommunicationEnv.ParkInfo, RegedCar[CamIdx], CarNo, Image, RegResult, LprInfo.ChNo);
                                Util.Logger.Query(Noitem.Query);
                                Util.clsMssql.ExecQuery(TCon, Noitem.Query);
                                blWriteLprTrns = true;
                            }
                            if(NoDriving.DisPlay) {
                                string Ment1 = NoDriving.Ment1;
                                string Ment2 = NoDriving.Ment2;
                                if(NoDriving.Ment1 == "차량번호")
                                    Ment1 = CarNo;
                                else if(NoDriving.Ment2 == "차량번호")
                                    Ment2 = CarNo;
                                bool _isNet = NetDev != null && ((CamIdx == 0 && Env.CommunicationEnv.DisPlay[0].Net.Use) || (CamIdx == 1 && Env.CommunicationEnv.DisPlay[1].Net.Use));
                                if(_isNet) {
                                    NetDev.SendMsg(Ment1, clsFunction.GetColor8Int(NoDriving.Color1), Ment2, clsFunction.GetColor8Int(NoDriving.Color2));
                                } else {
                                    SerialDev.DisPlayMent(CamIdx, Ment1, NoDriving.Color1, Ment2, NoDriving.Color2);
                                }
                                // 3초 후 기본문구 직접 출력
                                int _camIdx = CamIdx;
                                string _defMent1 = Env.CommunicationEnv.DisPlay[CamIdx].Ment.Ment1Line;
                                string _defMent2 = Env.CommunicationEnv.DisPlay[CamIdx].Ment.Ment2Line;
                                string _defColor1 = Env.CommunicationEnv.DisPlay[CamIdx].Ment.Ment1Color;
                                string _defColor2 = Env.CommunicationEnv.DisPlay[CamIdx].Ment.Ment2Color;
                                var _netDev = NetDev;
                                var _serialDev = SerialDev;
                                System.Threading.Tasks.Task.Run(async () => {
                                    await System.Threading.Tasks.Task.Delay(5000);
                                    try {
                                        if(_isNet && _netDev != null)
                                            _netDev.SendMsg(_defMent1, clsFunction.GetColor8Int(_defColor1), _defMent2, clsFunction.GetColor8Int(_defColor2));
                                        else if(_serialDev != null)
                                            _serialDev.DisPlayMent(_camIdx, _defMent1, _defColor1, _defMent2, _defColor2);
                                    } catch(Exception ex) {
                                        Util.Logger.Log("부제 전광판 기본문구 복귀 오류: " + ex.Message);
                                    }
                                });
                            }
                        }
                        Rtn += "부제 체크 해당";
                        blNoDriving = true;
                        // Exception : 정기차량 전체 부제 제외 (정기권 있으면 통과)
                        if(!NoDriving.Exception && !blNoDrivingException2)
                            return Rtn;
                    }
                }

                Util.Logger.Log("정기권 정합성 체크 " + RegedCar[CamIdx].ToString());
                //정기권 정합성 체크
                if(RegedCar[CamIdx]) {
                    Util.Logger.Log(string.Format("정기권 정보"));
                    Util.Logger.Log(string.Format("유효 기간 : {0} ~ {1}", RegedInfo[0]["dtValidStartDate"].ToString(), RegedInfo[0]["dtValidEndDate"].ToString()));
                    Util.Logger.Log(string.Format("사용 여부 : {0} ", RegedInfo[0]["iUseFlg"].ToString()));
                    Util.Logger.Log(string.Format("고객 성명 : {0} ", RegedInfo[0]["acUserName"].ToString()));
                    Util.Logger.Log(string.Format("차량 번호 : {0} ", CarNo));
                    Util.Logger.Log(string.Format("그룹 번호 : {0} ", RegedInfo[0]["iGroup"].ToString()));

                    Rtn += string.Format("정기권 정보 {0}", '\n');
                    Rtn += string.Format("유효 기간 : {0} ~ {1} {2}", RegedInfo[0]["dtValidStartDate"].ToString(), RegedInfo[0]["dtValidEndDate"].ToString(), '\n');
                    Rtn += string.Format("사용 여부 : {0} {1}", RegedInfo[0]["iUseFlg"].ToString(), '\n');
                    Rtn += string.Format("고객 성명 : {0} {1}", RegedInfo[0]["acUserName"].ToString(), '\n');
                    Rtn += string.Format("차량 번호 : {0} {1}", CarNo, '\n');
                    Rtn += string.Format("차량 번호 : {0} {1}", CarNo, '\n');
                    if(Util.Function.IntTryParse(RegedInfo[0]["iUseFlg"].ToString()) == 0 &&
                        Util.Function.DateTimeTryParse(RegedInfo[0]["dtValidStartDate"].ToString()) <= ProcTime &&
                        Util.Function.DateTimeTryParse(RegedInfo[0]["dtValidEndDate"].ToString()).AddDays(1).AddSeconds(-1) >= ProcTime) {
                        if(blInfo.Apply) {
                            Rtn += string.Format("블랙 리스트 문구 전광판 출력 {0} {1}", blInfo.Ment1, CarNo, '\n');
                            if(NetDev != null && ((CamIdx == 0 && Env.CommunicationEnv.DisPlay[0].Net.Use) || (CamIdx == 1 && Env.CommunicationEnv.DisPlay[1].Net.Use))) {
                                NetDev.SendMsg(blInfo.Ment1, clsFunction.GetColor8Int(blInfo.Color1), blInfo.Ment2, clsFunction.GetColor8Int(blInfo.Color2));
                            } else {
                                SerialDev.DisPlayMent(CamIdx, blInfo.Ment1, blInfo.Color1, blInfo.Ment2, blInfo.Color2);
                            }
                            Util.Logger.Log(string.Format("블랙 리스트 문구 전광판 출력 {0} {1} {2} {3} {4}", CamIdx == 0 ? "CH1" : "CH2", blInfo.Ment1, blInfo.Color1, blInfo.Ment2, blInfo.Color2));
                        } else if(Type.Equals((int)ClsStructure.InoutType.입구용) || ((Type.Equals((int)ClsStructure.InoutType.출구용) && RegedInfo[0]["iGroup"].ToString() != clsExceptGroup.ExceptGrpNo.ToString()))) {
                            if(Type.Equals((int)ClsStructure.InoutType.입구용) && Env.RegCarControl.Entcontroluse) {
                                //동일 아이디 입차 정보 조회
                                string query = string.Format("select top 1 custdef.iLotArea, PASSTRNS.acplate1, dtindate, dtoutdate from {0}.dbo.PASSTRNS\r\n", Env.CommonEnv.DBInfo.TrnsDb);
                                query += string.Format("inner join {0}.dbo.CUSTDEF on PASSTRNS.acPlate1 in (custdef.acPlate1, custdef.acPlate2, custdef.acPlate3) and\r\n", Env.CommonEnv.DBInfo.MstDB);
                                query += string.Format("custdef.iuser = {0} and PASSTRNS.acplate1 != '{1}'\r\n", RegedInfo[0]["iuser"].ToString(), CarNo);
                                query += "order by dtindate desc";
                                DataTable limitdt = Util.clsMssql.GetTable(TCon, query);
                                if(limitdt.Rows.Count > 0) {
                                    if(string.IsNullOrEmpty(limitdt.Rows[0]["dtoutdate"].ToString())) {
                                        Env.RegCarControl.iControlType = 2;
                                        Rtn += string.Format("정기 차량 입차 제한 {0} {1} {2}", Env.RegCarControl.Entcontrolment, CarNo, '\n');
                                        if(!frmLprMain.isFixed) {
                                            if(NetDev != null && ((CamIdx == 0 && Env.CommunicationEnv.DisPlay[0].Net.Use) || (CamIdx == 1 && Env.CommunicationEnv.DisPlay[1].Net.Use))) {
                                                NetDev.SendMsg(Env.RegCarControl.Entcontrolment, clsFunction.GetColor8Int(Env.CommunicationEnv.DisPlay[CamIdx].Period1Color),
                                                    CarNoSpace(CarNo), clsFunction.GetColor8Int(Env.CommunicationEnv.DisPlay[CamIdx].Period2Color));
                                            } else {
                                                SerialDev.DisPlayMent(CamIdx, Env.RegCarControl.Entcontrolment, Env.CommunicationEnv.DisPlay[CamIdx].Period1Color, CarNoSpace(CarNo), Env.CommunicationEnv.DisPlay[CamIdx].Period2Color);
                                            }
                                            //Util.Logger.Log(string.Format("정기 차량 전광판 출력 {0} {1} {2} {3} {4}", CamIdx == 0 ? "CH1" : "CH2", Env.CommunicationEnv.DisPlay[CamIdx].PeriodCar, Env.CommunicationEnv.DisPlay[CamIdx].Period1Color, CarNo, Env.CommunicationEnv.DisPlay[CamIdx].Period2Color));
                                        }

                                        if(CamIdx == 0) {
                                            if(Env.CommunicationEnv.DisPlay[CamIdx].Net.Use) {
                                                frmLprMain.NetDisPlay1.DisPlayTime = DateTime.Now;
                                            } else {
                                                frmLprMain.FirstDisPlayReturn.DisPlayTime = DateTime.Now;
                                            }
                                        } else {
                                            if(Env.CommunicationEnv.DisPlay[CamIdx].Net.Use) {
                                                frmLprMain.NetDisPlay2.DisPlayTime = DateTime.Now;
                                            } else {
                                                frmLprMain.SecondDisPlayReturn.DisPlayTime = DateTime.Now;
                                            }
                                        }
                                        query = clsQuery.SetEntranceLprTrns(ProcTime, Env.CommunicationEnv.ParkInfo, RegedCar[CamIdx], CarNo, Image, RegResult, LprInfo.ChNo
                                                , RegedInfo[0]["acEmpNo"].ToString(), RegedInfo[0]["acCarModel1"].ToString(), RegedInfo[0]["acCarModel2"].ToString());
                                        Util.clsMssql.ExecQuery(TCon, query);
                                        return Rtn;
                                    }
                                }
                            }
                            if(Env.CommonEnv.Dio.DioOutPut[CamIdx].Use && LprInfo.LprOpt.Period_Gate) {
                                if(BlackOutGate) {
                                    if(SpecialGroup.GroupIdx == 0 || SpecialGroup.GroupIdx == -1) {
                                        Util.Logger.Log(string.Format("정기 차량 차단기 개방"));
                                        Rtn += string.Format("{0} 정기 차량 차단기 개방 {1}", CamIdx + 1, '\n');
                                      //if(!FullSpaceControl.Use || (FullSpaceControl.Use && !FullSpaceControl.Period))

                                      //if(!FullSpaceControl.isFull || (FullSpaceControl.isFulle && !FullSpaceControl.Period))

                                            GateOpen(CamIdx);
                                        if(Type.Equals((int)ClsStructure.InoutType.입구용))
                                            blNoDriving_Ent = true;
                                    } else if(SpecialGroup.GroupIdx == Util.Function.IntTryParse(RegedInfo[0]["iGroup"].ToString())) {
                                        Util.Logger.Log(string.Format("특정 정기 차량 차단기 개방"));
                                        Rtn += string.Format("{0} 특정 정기 차량 차단기 개방 {1}", CamIdx + 1, '\n');
                                        GateOpen(CamIdx);
                                        if(Type.Equals((int)ClsStructure.InoutType.입구용))
                                            blNoDriving_Ent = true;
                                    } else {
                                        Util.Logger.Log(string.Format(string.Format("특정 정기 차량 차단기 개방 처리 제외 대상 그룹 번호 [{0}]", RegedInfo[0]["iGroup"].ToString())));
                                        RegedCar[CamIdx] = false;
                                    }
                                }
                            }
                            if(Env.CommunicationEnv.DisPlay[CamIdx].Use && RegedCar[CamIdx]) {
                                if(BlackOutDisplay) {
                                    Rtn += string.Format("블랙 리스트 문구 전광판 출력 {0} {1}", blInfo.Ment1, CarNo, '\n');
                                    //SerialDev.DisPlayMent(CamIdx, blInfo.Ment1, blInfo.Color1, blInfo.Ment2, blInfo.Color2);
                                    if(NetDev != null && ((CamIdx == 0 && Env.CommunicationEnv.DisPlay[0].Net.Use) || (CamIdx == 1 && Env.CommunicationEnv.DisPlay[1].Net.Use))) {
                                        NetDev.SendMsg(blInfo.Ment1, clsFunction.GetColor8Int(blInfo.Color1), blInfo.Ment2, clsFunction.GetColor8Int(blInfo.Color2));
                                    } else {
                                        SerialDev.DisPlayMent(CamIdx, blInfo.Ment1, blInfo.Color1, blInfo.Ment2, blInfo.Color2);
                                    }
                                    Util.Logger.Log(string.Format("블랙 리스트 문구 전광판 출력 {0} {1} {2} {3} {4}", CamIdx == 0 ? "CH1" : "CH2", blInfo.Ment1, blInfo.Color1, blInfo.Ment2, blInfo.Color2));
                                } else {
                                    Rtn += string.Format("정기 차량 전광판 출력 {0} {1} {2}", ControlMent != "" ? ControlMent : Env.CommunicationEnv.DisPlay[CamIdx].PeriodCar, CarNo, '\n');
                                    if(!frmLprMain.isFixed) {
                                        SerialDev.DisPlayMent(CamIdx, ControlMent != "" ? ControlMent : Env.CommunicationEnv.DisPlay[CamIdx].PeriodCar, Env.CommunicationEnv.DisPlay[CamIdx].Period1Color, CarNoSpace(CarNo), Env.CommunicationEnv.DisPlay[CamIdx].Period2Color);
                                        if(NetDev != null && ((CamIdx == 0 && Env.CommunicationEnv.DisPlay[0].Net.Use) || (CamIdx == 1 && Env.CommunicationEnv.DisPlay[1].Net.Use))) {
                                            NetDev.SendMsg(ControlMent != "" ? ControlMent : Env.CommunicationEnv.DisPlay[CamIdx].PeriodCar, clsFunction.GetColor8Int(Env.CommunicationEnv.DisPlay[CamIdx].Period1Color),
                                              CarNoSpace(CarNo), clsFunction.GetColor8Int(Env.CommunicationEnv.DisPlay[CamIdx].Period2Color));
                                        } else {
                                            SerialDev.DisPlayMent(CamIdx, ControlMent != "" ? ControlMent : Env.CommunicationEnv.DisPlay[CamIdx].PeriodCar, Env.CommunicationEnv.DisPlay[CamIdx].Period1Color, CarNoSpace(CarNo), Env.CommunicationEnv.DisPlay[CamIdx].Period2Color);
                                        }
                                        //Util.Logger.Log(string.Format("정기 차량 전광판 출력 {0} {1} {2} {3} {4}", CamIdx == 0 ? "CH1" : "CH2", Env.CommunicationEnv.DisPlay[CamIdx].PeriodCar, Env.CommunicationEnv.DisPlay[CamIdx].Period1Color, CarNo, Env.CommunicationEnv.DisPlay[CamIdx].Period2Color));
                                    }
                                }
                                if(CamIdx == 0) {
                                    if(Env.CommunicationEnv.DisPlay[CamIdx].Net.Use) {
                                        frmLprMain.NetDisPlay1.DisPlayTime = DateTime.Now;
                                    } else {
                                        frmLprMain.FirstDisPlayReturn.DisPlayTime = DateTime.Now;
                                    }
                                } else {
                                    if(Env.CommunicationEnv.DisPlay[CamIdx].Net.Use) {
                                        frmLprMain.NetDisPlay2.DisPlayTime = DateTime.Now;
                                    } else {
                                        frmLprMain.SecondDisPlayReturn.DisPlayTime = DateTime.Now;
                                    }
                                }
                            }
                        } else {
                            Util.Logger.Log(string.Format(string.Format("정기권 처리 제외 대상 그룹 번호 [{0}]", RegedInfo[0]["iGroup"].ToString())));
                            Util.Logger.Log(string.Format("요금 계산 대상 정기권"));
                            RegedCar[CamIdx] = false;
                        }
                    } else {
                        RegedCar[CamIdx] = false;
                        if(Util.Function.IntTryParse(RegedInfo[0]["iUseFlg"].ToString()) == 1) {
                            if(Env.CommunicationEnv.PeriodMent.Ment1Line == "") {
                                AlertMsg = "사용 중지";
                                RegedCar[CamIdx] = false;
                            } else
                                AlertMsg = Env.CommunicationEnv.PeriodMent.Ment1Line;
                        } else if(Util.Function.DateTimeTryParse(RegedInfo[0]["dtValidEndDate"].ToString()).AddDays(1).AddSeconds(-1) < ProcTime) {
                            if(Env.CommunicationEnv.PeriodMent.Ment2Line == "") {
                                RegedCar[CamIdx] = false;
                                AlertMsg = "사용기간경과";
                            } else
                                AlertMsg = Env.CommunicationEnv.PeriodMent.Ment2Line;
                        } else if(Util.Function.DateTimeTryParse(RegedInfo[0]["dtValidStartDate"].ToString()) > ProcTime)
                            AlertMsg = "사용기간이전";
                        Util.Logger.Log(string.Format("정기권 오류 {0}", AlertMsg));
                        Rtn += string.Format("정기권 오류 {0} {1}", AlertMsg, '\n');
                    }
                }

                // 정기차량 전체 부제 제외: 정기권 없으면 차단 (iPsscrdZone=2 제외 차량은 통과)
                if(Type.Equals((int)ClsStructure.InoutType.입구용) && blNoDriving && NoDriving.Exception && !blNoDrivingException2 && !RegedCar[CamIdx] && !blNoDriving_Ent)
                    return Rtn;

                //HomeLan Relay
                if(LprInfo.InOutType == (int)ClsStructure.InoutType.입구용) {
                    if(RegedCar[CamIdx] && Env.CommunicationEnv.ClientTarget[0].Use) {
                        if(!RegedInfo[0]["Dongcode"].ToString().Trim().Equals(string.Empty)
                            && !RegedInfo[0]["Hocode"].ToString().Trim().Equals(string.Empty)) {
                            Util.Logger.Log(string.Format("세대 통보 {0} {1}", RegedInfo[0]["Dongcode"].ToString(), RegedInfo[0]["Hocode"].ToString()));
                            Rtn += string.Format("세대 통보 {0} {1} {2}", RegedInfo[0]["Dongcode"].ToString(), RegedInfo[0]["Hocode"].ToString(), '\n');
                            frmLprMain.Noti(string.Format("{0:D2}", Type), RegedInfo[0]["iTicket"].ToString(), CarNo, ProcTime.ToString("yyyyMMddHHmmss"), "", RegedInfo[0]["Dongcode"].ToString(), RegedInfo[0]["Hocode"].ToString(), RegedInfo[0]["acUserName"].ToString(), Env.CommunicationEnv.ParkInfo.No, Env.CommunicationEnv.ParkInfo.Client_No);
                        }
                    } else if(Env.CommunicationEnv.ClientTarget[0].Type == 1) {
                        //if (RegedInfo != null && RegedInfo.Length > 0)
                        {
                            Util.Logger.Log(string.Format("방문 차량 세대 통보 {0}", CarNo));
                            Rtn += string.Format("방문 차량 세대 통보 {0}", CarNo);
                            //frmLprMain.Noti(string.Format("{0:D2}", Type), "", CarNo, ProcTime.ToString("yyyyMMddHHmmss"), "", "", "", RegedInfo[0]["acUserName"].ToString(), Env.CommunicationEnv.ParkInfo.No, Env.CommunicationEnv.ParkInfo.Client_No);
                            frmLprMain.Noti(string.Format("{0:D2}", Type), "", CarNo, ProcTime.ToString("yyyyMMddHHmmss"), "", "", "", "", Env.CommunicationEnv.ParkInfo.No, Env.CommunicationEnv.ParkInfo.Client_No);
                        }
                    }
                }
                Util.Logger.Log("미등록 차량 무발권 차단기 개방 " + RegedCar.ToString());
                if(!RegedCar[CamIdx]) {
                    // [방문차량 출차] 전광판을 차단기(릴레이)보다 먼저 출력 — 차단기 열고 뒤늦게 표시되던 지연감 제거(입차 ProcessVisitor와 동일 취지).
                    //   아래 전광판 블록(DisPlay.Use)의 isVisitorExit 분기는 재출력 없이 DisPlayTime만 갱신해 return 타이머(~5초) 유지.
                    if(isVisitorExit && Env.CommunicationEnv.DisPlay[CamIdx].Use && !frmLprMain.isFixed) {
                        try {
                            if(NetDev != null && ((CamIdx == 0 && Env.CommunicationEnv.DisPlay[0].Net.Use) || (CamIdx == 1 && Env.CommunicationEnv.DisPlay[1].Net.Use)))
                                NetDev.SendMsg("방문차량", clsFunction.GetColor8Int("녹색"), CarNoSpace(CarNo), clsFunction.GetColor8Int("노랑"));
                            else
                                SerialDev.DisPlayMent(CamIdx, "방문차량", "녹색", CarNoSpace(CarNo), "노랑");
                            Util.Logger.Log(string.Format("방문차량(출차) 전광판 선출력 {0} {1}", CamIdx == 0 ? "CH1" : "CH2", CarNo));
                        } catch(Exception ve) { Util.Logger.Log("방문차 출차 전광판 선출력 오류 " + ve.Message); }
                    }
                    // 무발권 차단기 개방
                    //Util.Logger.Log(string.Format("무발권 처리 {0}", LprInfo.FreePass.ToString()));
                    //Rtn += string.Format("무발권 처리 {0} {1} ", LprInfo.FreePass.ToString(), '\n');
                    Util.Logger.Log(string.Format("미인식 처리 옵션 {0}", Env.CommunicationEnv.Nodetection_Open.ToString()));
                    Rtn += string.Format("미인식 처리 옵션 처리 {0} {1} ", Env.CommunicationEnv.Nodetection_Open.ToString(), '\n');

                    //if (LprInfo.FreePass && LprInfo.FreePassGateOpen)
                    try {
                        if(BlackOutGate) {
                            if(CarNo.Equals("No_Detection") && LprInfo.LprOpt.Normal_Gate && Env.CommunicationEnv.Nodetection_Open) {
                                GateOpen(CamIdx);
                                //Util.Logger.Log(string.Format("{0} 차단기 개방", CamIdx + 1));
                                Rtn += string.Format("{0} 차단기 개방 {1}", CamIdx + 1, '\n');
                            } else if(!CarNo.Equals("No_Detection") && LprInfo.LprOpt.Normal_Gate) {
                                GateOpen(CamIdx);
                                //Util.Logger.Log(string.Format("{0} 차단기 개방", CamIdx + 1));
                                Rtn += string.Format("{0} 차단기 개방 {1}", CamIdx + 1, '\n');
                            }

                            if(clsBusinessCar.UseBusinessCar) {
                                if(clsBusinessCar.IsBusinessCar(CarNo)) {
                                    if(Type.Equals((int)ClsStructure.InoutType.입구용)) {
                                        if(clsBusinessCar.UseEntranceGateOpen) {
                                            GateOpen(CamIdx);
                                            Util.Logger.Log(string.Format("영업용 차량 {0} 차단기 개방", CamIdx + 1));
                                        }
                                    } else {
                                        if(clsBusinessCar.UseExitGateOpen) {
                                            GateOpen(CamIdx);
                                            Util.Logger.Log(string.Format("영업용 차량 {0} 차단기 개방", CamIdx + 1));
                                        }
                                    }
                                }
                            }
                        }
                    } catch(Exception GateOpen_Error) {
                        Util.Logger.Log(string.Format("차단기 개방 오류 {0}", GateOpen_Error.Message));
                    }
                    if(Env.CommunicationEnv.DisPlay[CamIdx].Use) {
                        try {
                            if(isVisitorExit) {
                                // 출차 방문차 — 위 차단기(릴레이) 前에서 이미 '방문차량' 선출력함. 여기선 재출력 없이
                                //   일반차량 표시만 건너뜀. 아래 DisPlayTime=Now 갱신으로 return 타이머(~5초) 유지.
                                Rtn += string.Format("방문차량(출차) 전광판 (선출력됨) {0} {1}", CarNo, '\n');
                            } else if(blInfo.Apply || BlackOutDisplay) {
                                Rtn += string.Format("블랙 리스트 문구 전광판 출력 {0} {1}", blInfo.Ment1, CarNo, '\n');
                                //SerialDev.DisPlayMent(CamIdx, blInfo.Ment1, blInfo.Color1, blInfo.Ment2, blInfo.Color2);
                                if(NetDev != null && ((CamIdx == 0 && Env.CommunicationEnv.DisPlay[0].Net.Use) || (CamIdx == 1 && Env.CommunicationEnv.DisPlay[1].Net.Use))) {
                                    NetDev.SendMsg(blInfo.Ment1, clsFunction.GetColor8Int(blInfo.Color1), blInfo.Ment2, clsFunction.GetColor8Int(blInfo.Color2));
                                } else {
                                    SerialDev.DisPlayMent(CamIdx, blInfo.Ment1, blInfo.Color1, blInfo.Ment2, blInfo.Color2);
                                }
                                Util.Logger.Log(string.Format("블랙 리스트 문구 전광판 출력 {0} {1} {2} {3} {4}", CamIdx == 0 ? "CH1" : "CH2", blInfo.Ment1, blInfo.Color1, blInfo.Ment2, blInfo.Color2));
                            } else if(!AlertMsg.Equals(string.Empty)) {
                                if(!frmLprMain.isFixed) {
                                    //SerialDev.DisPlayMent(CamIdx, AlertMsg, Env.CommunicationEnv.DisPlay[CamIdx].Normal1Color, CarNo, Env.CommunicationEnv.DisPlay[CamIdx].Normal2Color);
                                    if(NetDev != null && ((CamIdx == 0 && Env.CommunicationEnv.DisPlay[0].Net.Use) || (CamIdx == 1 && Env.CommunicationEnv.DisPlay[1].Net.Use))) {
                                        NetDev.SendMsg(AlertMsg, clsFunction.GetColor8Int(Env.CommunicationEnv.DisPlay[CamIdx].Normal1Color),
                                          CarNoSpace(CarNo), clsFunction.GetColor8Int(Env.CommunicationEnv.DisPlay[CamIdx].Normal2Color));
                                    } else {
                                        SerialDev.DisPlayMent(CamIdx, AlertMsg, Env.CommunicationEnv.DisPlay[CamIdx].Normal1Color, CarNoSpace(CarNo), Env.CommunicationEnv.DisPlay[CamIdx].Normal2Color);
                                    }
                                }
                                Rtn += string.Format("일반 차량 전광판 출력 {0} {1} {2}", AlertMsg, CarNo, '\n');
                                //Util.Logger.Log(string.Format("일반 차량 전광판 출력 {0} {1} {2} {3} {4}", CamIdx == 0 ? "CH1" : "CH2", AlertMsg, Env.CommunicationEnv.DisPlay[CamIdx].Normal1Color, CarNo, Env.CommunicationEnv.DisPlay[CamIdx].Normal2Color));
                            } else {
                                if(!frmLprMain.isFixed) {
                                    //SerialDev.DisPlayMent(CamIdx, Env.CommunicationEnv.DisPlay[CamIdx].NormalCar, Env.CommunicationEnv.DisPlay[CamIdx].Normal1Color, CarNo.Equals("No_Detection") ? "인식실패" : CarNo, Env.CommunicationEnv.DisPlay[CamIdx].Normal2Color);
                                    //SerialDev.DisPlayMent(CamIdx, clsBusinessCar.BusinessCarMent(CarNo, Env.CommunicationEnv.DisPlay[CamIdx].NormalCar), Env.CommunicationEnv.DisPlay[CamIdx].Normal1Color, CarNo.Equals("No_Detection") ? "인식실패" : CarNo, Env.CommunicationEnv.DisPlay[CamIdx].Normal2Color);
                                    if(NetDev != null && ((CamIdx == 0 && Env.CommunicationEnv.DisPlay[0].Net.Use) || (CamIdx == 1 && Env.CommunicationEnv.DisPlay[1].Net.Use))) {
                                        NetDev.SendMsg(clsBusinessCar.BusinessCarMent(CarNo, ControlMent != "" ? ControlMent : Env.CommunicationEnv.DisPlay[CamIdx].NormalCar), clsFunction.GetColor8Int(Env.CommunicationEnv.DisPlay[CamIdx].Normal1Color),
                                          CarNo.Equals("No_Detection") ? "인식실패" : CarNoSpace(CarNo), clsFunction.GetColor8Int(Env.CommunicationEnv.DisPlay[CamIdx].Normal2Color));
                                    } else {
                                        SerialDev.DisPlayMent(CamIdx, clsBusinessCar.BusinessCarMent(CarNo, ControlMent != "" ? ControlMent : Env.CommunicationEnv.DisPlay[CamIdx].NormalCar), Env.CommunicationEnv.DisPlay[CamIdx].Normal1Color, CarNoSpace(CarNo.Equals("No_Detection") ? "인식실패" : CarNo), Env.CommunicationEnv.DisPlay[CamIdx].Normal2Color);
                                    }
                                    //Util.Logger.Log(string.Format("일반 차량 전광판 출력 {0} {1} {2} {3} {4}", CamIdx == 0 ? "CH1" : "CH2", clsBusinessCar.BusinessCarMent(CarNo, Env.CommunicationEnv.DisPlay[CamIdx].NormalCar), Env.CommunicationEnv.DisPlay[CamIdx].Normal1Color, CarNo.Equals("No_Detection") ? "인식실패" : CarNo, Env.CommunicationEnv.DisPlay[CamIdx].Normal2Color));
                                }
                                Rtn += string.Format("일반 차량 전광판 출력 {0} {1} {2}", clsBusinessCar.BusinessCarMent(CarNo, Env.CommunicationEnv.DisPlay[CamIdx].NormalCar), CarNo, '\n');
                            }
                            if(CamIdx == 0) {
                                if(Env.CommunicationEnv.DisPlay[CamIdx].Net.Use) {
                                    frmLprMain.NetDisPlay1.DisPlayTime = DateTime.Now;
                                } else {
                                    frmLprMain.FirstDisPlayReturn.DisPlayTime = DateTime.Now;
                                }
                            } else {
                                if(Env.CommunicationEnv.DisPlay[CamIdx].Net.Use) {
                                    frmLprMain.NetDisPlay2.DisPlayTime = DateTime.Now;
                                } else {
                                    frmLprMain.SecondDisPlayReturn.DisPlayTime = DateTime.Now;
                                }
                            }
                        } catch(Exception DisPlay_Error) {
                            Util.Logger.Log(string.Format("일반 차량 전광판 출력 ERROR {0}", DisPlay_Error.Message));
                        }
                    }
                }

                //입차 내역 입력
                QueryStruct item = new QueryStruct();
                item.MST = false;
                string Sql = string.Empty;

                if(DelayReg.CheckAPB(CarNo)) {
                    Util.Logger.Log("입출구 타입 " + Type.ToString());
                    if(Type.Equals((int)ClsStructure.InoutType.입구용)) {
                        Util.Logger.Log("입구용 처리");
                        string SendEnt_Msg = string.Empty;
                        if(!blInfo.Apply) {
                            if(RegedCar[CamIdx]) {

                                if(LprInfo.LprOpt.Period_Passtrns) {
                                    if(RegedInfo[0]["iGroup"].ToString() != clsExceptGroup.ExceptGrpNo.ToString()) {
                                        Util.Logger.Log("정기 차량 입차 내역 기록");
                                        item.Query = clsQuery.SetEntrancePassTrns(ProcTime, Env.CommunicationEnv.ParkInfo, LprInfo, RegedInfo[0], CarNo, Number, Image);
                                    } else {
                                        Util.Logger.Log("요금 계산 대상 정기권 일반 입차 내역 기록");
                                        item.Query = clsQuery.SetEntranceTcktTrns(ProcTime, Env.CommunicationEnv.ParkInfo, LprInfo, CarNo, Image, RegedInfo[0]["iGroup"].ToString(), irate);
                                    }
                                    Util.Logger.Query(item.Query);
                                    //QList.Add(item);
                                    Util.clsMssql.ExecQuery(TCon, item.Query);
                                }

                                if(!LprInfo.LprOpt.Period_Passtrns && RegedInfo[0]["iGroup"].ToString() == clsExceptGroup.ExceptGrpNo.ToString()) {
                                    Util.Logger.Log("일반 차량 입차 내역 기록");
                                    item.Query = clsQuery.SetEntranceTcktTrns(ProcTime, Env.CommunicationEnv.ParkInfo, LprInfo, CarNo, Image, RegedInfo[0]["iGroup"].ToString(), irate);
                                    Util.Logger.Query(item.Query);
                                    //QList.Add(item);
                                    Util.clsMssql.ExecQuery(TCon, item.Query);
                                }
                                if(LprInfo.LprOpt.Period_Counter) {
                                    Util.Logger.Log("입차 카운트 증가");
                                    //FC_COUNTTRNS
                                    item.Query = clsQuery.SetEntranceFcCountTrns(ProcTime, Env.CommunicationEnv.ParkInfo);
                                    Util.Logger.Query(item.Query);
                                    //QList.Add(item);
                                    Util.clsMssql.ExecQuery(TCon, item.Query);
                                    //FC_STAY
                                    item.Query = clsQuery.SetEntranceFcStay(Env.CommunicationEnv.ParkInfo);
                                    Util.Logger.Query(item.Query);
                                    //QList.Add(item);
                                    Util.clsMssql.ExecQuery(TCon, item.Query);
                                }
                                if(LprInfo.LprOpt.Period_SendData) {
                                    SendEnt_Msg = clsFunction.MakeTransMessage(Env.CameraEnv.SockDataFormat, camChName, CarNo, Env.CameraEnv.ImageSave.SavePath, Image, ProcTime);
                                    bool stxetx = camStxEtx;
                                    Util.Logger.Log(string.Format("정기차량 입차 정보 전송 {0}", SendEnt_Msg));
                                    if(stxetx)
                                        frmLprMain.Main.LprEntSvr.SendMsgSTXETX(SendEnt_Msg);
                                    else
                                        frmLprMain.Main.LprEntSvr.SendMsg(SendEnt_Msg);
                                }
                            } else {
                                if(LprInfo.LprOpt.Normal_Tckttrns) {
                                    Util.Logger.Log("일반 차량 입차 내역 기록");
                                    item.Query = clsQuery.SetEntranceTcktTrns(ProcTime, Env.CommunicationEnv.ParkInfo, LprInfo, CarNo, Image, "0", irate);
                                    Util.Logger.Query(item.Query);
                                    //QList.Add(item);
                                    Util.clsMssql.ExecQuery(TCon, item.Query);
                                }
                                if(LprInfo.LprOpt.Normal_Counter) {
                                    Util.Logger.Log("입차 카운트 증가");
                                    //FC_STAY
                                    item.Query = clsQuery.SetEntranceFcStay(Env.CommunicationEnv.ParkInfo);
                                    Util.Logger.Query(item.Query);
                                    //QList.Add(item);
                                    Util.clsMssql.ExecQuery(TCon, item.Query);
                                    //FC_COUNTTRNS
                                    item.Query = clsQuery.SetEntranceFcCountTrns(ProcTime, Env.CommunicationEnv.ParkInfo);
                                    Util.Logger.Query(item.Query);
                                    //QList.Add(item);
                                    Util.clsMssql.ExecQuery(TCon, item.Query);
                                }
                                //if (LprInfo.LprOpt.Normal_SendData || LprInfo.LprOpt.Period_SendData || (clsBusinessCar.IsBusinessCar(CarNo) && clsBusinessCar.UseEntranceSocketDataSend))
                                if((RegedCar[CamIdx] && LprInfo.LprOpt.Period_SendData) || (!RegedCar[CamIdx] && LprInfo.LprOpt.Normal_SendData) || (clsBusinessCar.IsBusinessCar(CarNo) && clsBusinessCar.UseEntranceSocketDataSend)) {
                                    SendEnt_Msg = clsFunction.MakeTransMessage(Env.CameraEnv.SockDataFormat, camChName, CarNo, Env.CameraEnv.ImageSave.SavePath, Image, ProcTime);
                                    bool stxetx = camStxEtx;
                                    Util.Logger.Log("일반 차량 입차 정보 전송");
                                    if(clsBusinessCar.UseBusinessCar)
                                        if(clsBusinessCar.IsBusinessCar(CarNo) && !clsBusinessCar.UseEntranceSocketDataSend) {
                                            SendEnt_Msg = "";
                                            Util.Logger.Log("일반 차량 입차 정보 전송 안함");
                                        }
                                    if(SendEnt_Msg != string.Empty) {
                                        Util.Logger.Log(string.Format("일반차량 입차 정보 전송 {0}", SendEnt_Msg));
                                        if(stxetx)
                                            frmLprMain.Main.LprEntSvr.SendMsgSTXETX(SendEnt_Msg);
                                        else
                                            frmLprMain.Main.LprEntSvr.SendMsg(SendEnt_Msg);
                                    }
                                }
                            }
                        }
                        if((RegedCar[CamIdx] && LprInfo.LprOpt.Period_Lprtrns) || (!RegedCar[CamIdx] && LprInfo.LprOpt.Normal_Lprtrns)) {
                            if(!blWriteLprTrns) {
                                //LPRTRNS
                                //item.Query = clsQuery.SetEntranceLprTrns(ProcTime, Env.CommunicationEnv.ParkInfo, RegedCar, CarNo, Image, RegResult, LprInfo.ChNo);
                                if(RegedInfo != null && RegedInfo.Length > 0) {
                                    if(RegedCar[CamIdx])
                                        item.Query = clsQuery.SetEntranceLprTrns(ProcTime, Env.CommunicationEnv.ParkInfo, RegedCar[CamIdx], CarNo, Image, RegResult, LprInfo.ChNo
                                            , RegedInfo[0]["acEmpNo"].ToString(), RegedInfo[0]["acCarModel1"].ToString(), RegedInfo[0]["acCarModel2"].ToString());
                                    else
                                        item.Query = clsQuery.SetEntranceLprTrns(ProcTime, Env.CommunicationEnv.ParkInfo, RegedCar[CamIdx], CarNo, Image, RegResult, LprInfo.ChNo
                                            , "", CarNo == "No_Detection" ? "인식오류" : "미등록", "");
                                } else
                                    item.Query = clsQuery.SetEntranceLprTrns(ProcTime, Env.CommunicationEnv.ParkInfo, RegedCar[CamIdx], CarNo, Image, RegResult, LprInfo.ChNo
                                        , "", CarNo == "No_Detection" ? "인식오류" : "미등록", "");
                                Util.Logger.Query(item.Query);
                                //QList.Add(item);
                                Util.clsMssql.ExecQuery(TCon, item.Query);
                            }
                        }
                    } else if(Type.Equals((int)ClsStructure.InoutType.출구용)) {
                        BeforeCalOpt.LagReturn LagChk = BeforeCalOpt.LagReturn.Cal;

                        bool OutService = false;
                        if(RegedCar[CamIdx]) {
                            if(LprInfo.LprOpt.Period_Passtrns) {
                                Util.Logger.Log("정기 차량 출차 내역 기록");
                                item.Query = clsQuery.SetExitPassTrns(ProcTime, Env.CommunicationEnv.ParkInfo, LprInfo, RegedInfo[0], CarNo, Number, Image);
                                Util.Logger.Query(item.Query);
                                //QList.Add(item);
                                Util.clsMssql.ExecQuery(TCon, item.Query);
                            }
                            if(LprInfo.LprOpt.Period_Counter) {
                                Util.Logger.Log("출차 카운트 증가");
                                //FC_COUNTTRNS
                                item.Query = clsQuery.SetExitFcCountTrns(ProcTime, Env.CommunicationEnv.ParkInfo);
                                Util.Logger.Query(item.Query);
                                //QList.Add(item);
                                Util.clsMssql.ExecQuery(TCon, item.Query);
                                //FC_STAY
                                item.Query = clsQuery.SetExitFcStay(Env.CommunicationEnv.ParkInfo);
                                Util.Logger.Query(item.Query);
                                //QList.Add(item);
                                Util.clsMssql.ExecQuery(TCon, item.Query);
                            }
                        } else {
                            if(BeforeCalOpt.Use)
                                LagChk = BeforeCalOpt.LagCarCheck(CarNo, Env.CommonEnv.DBInfo.TrnsDb, TCon);
                            if(LagChk == BeforeCalOpt.LagReturn.Lag) {
                                Util.Logger.Log("레그시간 출차 기록");
                                item.Query = clsQuery.SetExitTcktTrns(ProcTime, Env.CommunicationEnv.ParkInfo, LprInfo, CarNo, Image);
                                Util.Logger.Query(item.Query);
                                GateOpen(CamIdx);
                                Util.clsMssql.ExecQuery(TCon, item.Query);
                                //QList.Add(item);
                                if(LprInfo.LprOpt.Normal_Counter) {
                                    Util.Logger.Log("출차 카운트 증가");
                                    item.Query = clsQuery.SetExitFcCountTrns(ProcTime, Env.CommunicationEnv.ParkInfo);
                                    Util.clsMssql.ExecQuery(TCon, item.Query);
                                    Util.Logger.Query(item.Query);
                                    //QList.Add(item);
                                    item.Query = clsQuery.SetExitFcStay(Env.CommunicationEnv.ParkInfo);
                                    Util.clsMssql.ExecQuery(TCon, item.Query);
                                    Util.Logger.Query(item.Query);
                                    //QList.Add(item);
                                }
                                OutService = true;
                                try {
                                    //SerialDev.DisPlayMent(CamIdx, "사전정산출차", Env.CommunicationEnv.DisPlay[CamIdx].Normal1Color, Util.Common.LPadH(CarNo.Equals("No_Detection") ? "인식실패" : CarNo, 12), Env.CommunicationEnv.DisPlay[CamIdx].Normal2Color);
                                    //Thread.Sleep(100);
                                    if(NetDev != null && ((CamIdx == 0 && Env.CommunicationEnv.DisPlay[0].Net.Use) || (CamIdx == 1 && Env.CommunicationEnv.DisPlay[1].Net.Use))) {
                                        NetDev.SendMsg("사전정산출차", clsFunction.GetColor8Int(Env.CommunicationEnv.DisPlay[CamIdx].Normal1Color),
                                          CarNo.Equals("No_Detection") ? "인식실패" : CarNoSpace(CarNo), clsFunction.GetColor8Int(Env.CommunicationEnv.DisPlay[CamIdx].Normal2Color));
                                    } else {
                                        SerialDev.DisPlayMent(CamIdx, "사전정산출차", Env.CommunicationEnv.DisPlay[CamIdx].Normal1Color,
                                            CarNoSpace(CarNo.Equals("No_Detection") ? "인식실패" : CarNo), Env.CommunicationEnv.DisPlay[CamIdx].Normal2Color);
                                    }
                                } catch(Exception) { }
                            } else {
                                if(clsOutService.Use) {
                                    if(clsOutService.Check(CarNo)) {
                                        OutService = true;
                                        GateOpen(CamIdx);
                                        Util.Logger.Log(string.Format("출차 서비스 시간 이내 차량 {0} 차단기 개방", CamIdx + 1));
                                        if(!LprInfo.LprOpt.Normal_Tckttrns) {
                                            string Query = string.Format("update {0}.dbo.tckttrns set dtoutdate = getdate(), acGoOutPicName = '{1}' from {0}.dbo.tckttrns inner join (select acplate1, max(iid) iid from {0}.dbo.tckttrns group by acplate1) grp \r\n"
                                            , Env.CommonEnv.DBInfo.TrnsDb, Image);
                                            Query += string.Format("on tckttrns.acplate1 = grp.acplate1 and tckttrns.iid = grp.iid where tckttrns.acplate1 = '{0}'", CarNo);
                                            Util.Logger.Query(Query);
                                            Util.clsMssql.ExecQuery(TCon, Query);
                                        }
                                    }
                                }
                                if(!OutService) {
                                    //회차 차량 확인
                                    if(Env.CommunicationEnv.ReturnCar.Use) {
                                        string sql = string.Format("select * from {0}.dbo.tckttrns where acplate1 = '{1}' order by dtindate desc", Env.CommonEnv.DBInfo.TrnsDb, CarNo);
                                        DataTable Returndt = Util.clsMssql.GetTable(TCon, sql);
                                        if(Returndt.Rows.Count > 0) {
                                            if(String.IsNullOrEmpty(Returndt.Rows[0]["dtoutdate"].ToString())) {
                                                if((DateTime.Now - Util.Function.DateTimeTryParse(Returndt.Rows[0]["dtindate"].ToString())).TotalMinutes <= Env.CommunicationEnv.ReturnCar.Term) {
                                                    if(NetDev != null && ((CamIdx == 0 && Env.CommunicationEnv.DisPlay[0].Net.Use) || (CamIdx == 1 && Env.CommunicationEnv.DisPlay[1].Net.Use))) {
                                                        NetDev.SendMsg(Env.CommunicationEnv.ReturnCar.Ment == "" ? "회차차량출차" : Env.CommunicationEnv.ReturnCar.Ment,
                                                            clsFunction.GetColor8Int(Env.CommunicationEnv.DisPlay[CamIdx].Normal1Color),
                                                          CarNo.Equals("No_Detection") ? "인식실패" : CarNoSpace(CarNo),
                                                          clsFunction.GetColor8Int(Env.CommunicationEnv.DisPlay[CamIdx].Normal2Color));
                                                    } else {
                                                        SerialDev.DisPlayMent(CamIdx, Env.CommunicationEnv.ReturnCar.Ment == "" ? "회차차량출차" : Env.CommunicationEnv.ReturnCar.Ment,
                                                            Env.CommunicationEnv.DisPlay[CamIdx].Normal1Color, CarNoSpace(CarNo.Equals("No_Detection") ? "인식실패" : CarNo),
                                                            Env.CommunicationEnv.DisPlay[CamIdx].Normal2Color);
                                                    }
                                                    GateOpen(CamIdx);
                                                    if(!LprInfo.LprOpt.Normal_Tckttrns) {
                                                        Util.Logger.Log("일반 차량 출차 내역 기록");
                                                        item.Query = clsQuery.SetExitTcktTrns(ProcTime, Env.CommunicationEnv.ParkInfo, LprInfo, CarNo, Image);
                                                        Util.Logger.Query(item.Query);
                                                        //QList.Add(item);
                                                        Util.clsMssql.ExecQuery(TCon, item.Query);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    if(LprInfo.LprOpt.Normal_Tckttrns) {
                                        Util.Logger.Log("일반 차량 출차 내역 기록");
                                        item.Query = clsQuery.SetExitTcktTrns(ProcTime, Env.CommunicationEnv.ParkInfo, LprInfo, CarNo, Image);
                                        Util.Logger.Query(item.Query);
                                        //QList.Add(item);
                                        Util.clsMssql.ExecQuery(TCon, item.Query);
                                        // 방문차량 — TCKTTRNS 출차와 함께 VisitorTrns도 출차 처리(미출차행 UPDATE + 주차시간 계산).
                                        //   LPR 단독(출구 무인정산기 없음)에서 '일반차량 입출차내역 기록' 옵션 켜졌을 때 방문차 주차시간 누락 방지.
                                        if(isVisitorExit && visitorExitRow != null) {
                                            Util.Logger.Log("방문 차량 출차 내역 기록 (VisitorTrns)");
                                            item.Query = clsQuery.SetExitVisitorTrns(ProcTime, Env.CommunicationEnv.ParkInfo, LprInfo, visitorExitRow, CarNo, Image);
                                            Util.Logger.Query(item.Query);
                                            Util.clsMssql.ExecQuery(TCon, item.Query);
                                        }
                                    }
                                }
                                if(LprInfo.LprOpt.Normal_Counter) {
                                    Util.Logger.Log("출차 카운트 증가");
                                    //FC_COUNTTRNS
                                    item.Query = clsQuery.SetExitFcCountTrns(ProcTime, Env.CommunicationEnv.ParkInfo);
                                    Util.Logger.Query(item.Query);
                                    //QList.Add(item);
                                    Util.clsMssql.ExecQuery(TCon, item.Query);
                                    //FC_STAY
                                    item.Query = clsQuery.SetExitFcStay(Env.CommunicationEnv.ParkInfo);
                                    Util.Logger.Query(item.Query);
                                    //QList.Add(item);
                                    Util.clsMssql.ExecQuery(TCon, item.Query);
                                }
                            }
                        }

                        if((RegedCar[CamIdx] && LprInfo.LprOpt.Period_SendData) || (!RegedCar[CamIdx] && LprInfo.LprOpt.Normal_SendData) || (clsBusinessCar.IsBusinessCar(CarNo) && clsBusinessCar.UseExitSocketDataSend)) {
                            if(!OutService) {
                                Rtn = "요금계산기 자료 전송";
                                string SendCal_Msg = string.Empty;
                                if(LagChk != BeforeCalOpt.LagReturn.Lag)
                                    SendCal_Msg = clsFunction.MakeTransMessage(Env.CameraEnv.SockDataFormat, camChName, CarNo, Env.CameraEnv.ImageSave.SavePath, Image, ProcTime);
                                else
                                    SendCal_Msg = string.Format("!{0}#{1}#{2}#LAGOUT", LprInfo.ChNo, CarNo, Image);
                                //if (Env.CameraEnv.SockDataFormat == (int)ClsStructure.SockFormat.Kukje)
                                bool stxetx = camStxEtx;
                                Util.Logger.Log("요금계산기 정보 전송");
                                if(clsBusinessCar.UseBusinessCar && clsBusinessCar.IsBusinessCar(CarNo) && !clsBusinessCar.UseExitSocketDataSend) {
                                    SendCal_Msg = "";
                                    Util.Logger.Log("영업용 차량 요금계산기 정보 전송 안함");
                                }
                                if(SendCal_Msg != string.Empty) {
                                    Util.Logger.Log(string.Format("요금계산기 정보 전송 {0}", SendCal_Msg));
                                    if(stxetx)
                                        frmLprMain.Main.LprExitSvr.SendMsgSTXETX(SendCal_Msg);
                                    else
                                        frmLprMain.Main.LprExitSvr.SendMsg(SendCal_Msg);
                                }
                            }
                        }
                        if((RegedCar[CamIdx] && LprInfo.LprOpt.Period_Lprtrns) || (!RegedCar[CamIdx] && LprInfo.LprOpt.Normal_Lprtrns)) {
                            //LPRTRNS
                            //item.Query = clsQuery.SetExitLprTrns(ProcTime, Env.CommunicationEnv.ParkInfo, RegedCar, CarNo, Image, RegResult, LprInfo.ChNo);

                            if(RegedInfo != null && RegedInfo.Length > 0)
                                item.Query = clsQuery.SetExitLprTrns(ProcTime, Env.CommunicationEnv.ParkInfo, RegedCar[CamIdx], CarNo, Image, RegResult, LprInfo.ChNo
                                    , RegedInfo[0]["acEmpNo"].ToString(), RegedInfo[0]["acCarModel1"].ToString(), RegedInfo[0]["acCarModel2"].ToString());
                            else
                                item.Query = clsQuery.SetExitLprTrns(ProcTime, Env.CommunicationEnv.ParkInfo, RegedCar[CamIdx], CarNo, Image, RegResult, LprInfo.ChNo
                                    , "", CarNo == "No_Detection" ? "인식오류" : "미등록", "");
                            Util.Logger.Query(item.Query);
                            //QList.Add(item);
                            Util.clsMssql.ExecQuery(TCon, item.Query);
                        }
                    }
                    Color color = Color.Black;
                    Label lbl = null;
                    if(CamIdx == 0)
                        lbl = frmLprMain.Main.lblCam1RegResult;
                    else
                        lbl = frmLprMain.Main.lblCam2RegResult;
                    if(blInfo.Ment1 != "")
                        color = Color.Red;
                    else if(RegedCar[CamIdx])
                        color = Color.Blue;
                    else if(isVisitorExit)   // 방문차 출차 — 입차 때처럼 인식결과 녹색 표시
                        color = Color.Green;
                    lbl.Invoke((MethodInvoker)(() => lbl.ForeColor = color));
                    lbl.Invoke((MethodInvoker)(() => lbl.Text = string.Format("인식결과 : {0}", CarNo)));
                } else {
                    Util.Logger.Log("APB 입력 중단 입출구 타입 " + Type.ToString());
                }
                if(Env.SendOffice) {
                    string OfficeMsg = clsFunction.MakeTransMessage(Env.CameraEnv.SockDataFormat, camChName, CarNo, Env.CameraEnv.ImageSave.SavePath, Image, ProcTime);
                    frmLprMain.Main.SendOfficeList.Add(string.Format("CAPTURE:{0}", OfficeMsg));
                }
            } catch(Exception DataProcess_Error) {
                Util.Logger.Query(string.Format("DataProcess_Error : {0}", DataProcess_Error.Message));
            } finally {
                Processing = false;
                if(CamIdx >= 0 && CamIdx < _lastReged.Length) _lastReged[CamIdx] = RegedCar[CamIdx];   // 리셋 전 정기여부 보존(표시용)
                RegedCar[CamIdx] = false;
            }
            return Rtn;
        }

        private DataRow[] FindRegedCar(int RegCorrection, string CarNo, string Number) {
            ClsStructure.EnvStruct env = frmLprMain.ENV;
            DataRow[] RTN = null;
            try {
                if(CustDef == null) {
                    Util.Logger.Query(string.Format("Custdef is null"));
                    return null;
                }
                if(CustDef.Rows.Count > 0) {
                    if(env.RegCarControl.Ilotarea)
                        RTN = CustDef.Select(string.Format("(acplate1 = '{0}' or acplate2 = '{0}' or acplate3 = '{0}')  and iuseflg = 0 and ilotarea = {1}", CarNo, env.CommunicationEnv.ParkInfo.No));
                    else
                        RTN = CustDef.Select(string.Format("(acplate1 = '{0}' or acplate2 = '{0}' or acplate3 = '{0}')  and iuseflg = 0", CarNo, env.CommunicationEnv.ParkInfo.No));
                }

                if(RTN != null && RTN.Length == 0) {
                    if(RegCorrection.Equals((int)ClsStructure.reg_correction.digit4)) {
                        RTN = CustDef.Select(string.Format("acplate3 like '%{0}'", Number));
                    } else if(RegCorrection.Equals((int)ClsStructure.reg_correction.digit6)) {
                        RTN = CustDef.Select(string.Format("acplate3 like '%{0}'", Number));
                    }
                }
            } catch(Exception FindRegedCar_Error) {
                Util.Logger.Query(string.Format("FindRegedCar_Error : {0}", FindRegedCar_Error.Message));
            }
            return RTN;
        }

        // 방문차량 명단(VisitorDef)에서 차량번호로 유효한 방문차 조회 — 정기차 FindRegedCar 미러. 유효기간(시작~종료) 재확인.
        private DataRow[] FindVisitorCar(int RegCorrection, string CarNo, string Number) {
            try {
                if(VisitorDef == null || VisitorDef.Rows.Count == 0) return new DataRow[0];
                string car = CarNo.Replace("'", "''");
                DataRow[] rtn = VisitorDef.Select(string.Format("acPlate1 = '{0}'", car));
                if(rtn.Length == 0 && !string.IsNullOrEmpty(Number))
                    rtn = VisitorDef.Select(string.Format("acPlate1 like '%{0}'", Number));
                if(rtn.Length == 0) return rtn;
                List<DataRow> valid = new List<DataRow>();
                DateTime now = DateTime.Now;
                foreach(DataRow r in rtn) {
                    DateTime vs, ve;
                    bool okS = DateTime.TryParse(r["dtValidStart"].ToString(), out vs);
                    bool okE = DateTime.TryParse(r["dtValidEnd"].ToString(), out ve);
                    if(okS && okE && now >= vs && now <= ve) valid.Add(r);
                }
                return valid.ToArray();
            } catch(Exception ex) {
                Util.Logger.Query(string.Format("FindVisitorCar_Error : {0}", ex.Message));
                return new DataRow[0];
            }
        }

        // 방문차량 처리 — 정기차 미러: "방문차량"+차번 전광판 + 차단기 + TCKTTRNS(일반흐름 재사용)/VisitorTrns/FC_STAY 기록. 자체 완결 후 즉시 반환.
        // 세대(동/호)의 이번 달 누적 주차시간(분) — VisitorTrns.acCarStayHours(HHH:MM) 합계. dtInDate 기준 당월, 출차완료건만.
        private int GetVisitorMonthlyUsedMinutes(string lotArea, string dong, string ho) {
            try {
                string trns = frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb;
                string d = (dong ?? "").Replace("'", "''");
                string h = (ho ?? "").Replace("'", "''");
                string sql = string.Format(
                    "select isnull(sum(case when charindex(':', acCarStayHours) > 0 then " +
                    "cast(substring(acCarStayHours, 1, charindex(':', acCarStayHours)-1) as int)*60 + " +
                    "cast(substring(acCarStayHours, charindex(':', acCarStayHours)+1, 2) as int) else 0 end), 0) usedmin " +
                    "from {0}.dbo.VisitorTrns where iLotArea = {1} and acDong = N'{2}' and acHo = N'{3}' " +
                    "and dtInDate >= dateadd(month, datediff(month, 0, getdate()), 0) and acCarStayHours is not null",
                    trns, lotArea, d, h);
                DataTable dt = Util.clsMssql.GetTable(TCon, sql);
                if(dt != null && dt.Rows.Count > 0) return Util.Function.IntTryParse(dt.Rows[0]["usedmin"].ToString());
            } catch(Exception ex) { Util.Logger.Log("방문차 월사용시간 조회 오류 " + ex.Message); }
            return 0;
        }

        // 미출차 방문차 입차기록(VisitorTrns dtOutDate NULL) 조회 — 등록 만료/삭제됐어도 입차했으면 방문차 출차로 처리.
        //   반환 행: iid(=iVisitorId), acDong, acHo, acPlate1 (SetExitVisitorTrns V 인자로 사용). 없으면 null.
        private DataRow FindOpenVisitorTrns(string carNo) {
            try {
                string trns = frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb;
                string plate = (carNo ?? "").Replace("'", "''");
                string where = string.Format("acPlate1 = N'{0}' and dtOutDate is null", plate);
                if(frmLprMain.ENV.RegCarControl.Ilotarea)
                    where += string.Format(" and iLotArea = {0}", frmLprMain.ENV.CommunicationEnv.ParkInfo.No);
                string sql = string.Format("select top 1 isnull(iVisitorId,0) iid, isnull(acDong,'') acDong, isnull(acHo,'') acHo, acPlate1 " +
                    "from {0}.dbo.VisitorTrns where {1} order by dtInDate desc", trns, where);
                DataTable dt = Util.clsMssql.GetTable(TCon, sql);
                if(dt != null && dt.Rows.Count > 0) return dt.Rows[0];
            } catch(Exception ex) { Util.Logger.Log("미출차 방문차 조회 오류 " + ex.Message); }
            return null;
        }

        private string ProcessVisitor(int Type, ClsStructure.EnvStruct Env, int CamIdx, string CarNo, string Image, DateTime ProcTime, ClsStructure.Lpr_Info LprInfo, DataRow V, string Rtn) {
            try {
                bool ent = Type.Equals((int)ClsStructure.InoutType.입구용);
                Util.Logger.Log(string.Format("방문차량 처리 {0} 동{1} 호{2} ({3})", CarNo, V["acDong"], V["acHo"], ent ? "입차" : "출차"));
                Rtn += string.Format("방문차량 {0} 동{1} 호{2} {3}", CarNo, V["acDong"], V["acHo"], '\n');
                bool recog = !CarNo.Equals("No_Detection");

                // [월 한도] 입차 시 세대 이번달 누적 주차시간이 한도 초과면 → 방문차(무료) 처리하지 않고 일반차량으로 폴백(null 반환).
                //   호출부가 일반차량 입출차 흐름으로 처리(전광판/게이트/TCKTTRNS 등은 '일반차량 입출차내역 기록' 옵션에 따름). 월 경계로 다음달 자동 리셋.
                if(ent && VisitorMonthlyHours > 0) {
                    int usedMin = GetVisitorMonthlyUsedMinutes(V["iLotArea"].ToString(), V["acDong"].ToString(), V["acHo"].ToString());
                    if(usedMin >= VisitorMonthlyHours * 60) {
                        Util.Logger.Log(string.Format("방문차 월 이용시간 초과 → 일반차량으로 처리 {0} 동{1} 호{2} (사용 {3}분 / 한도 {4}시간)", CarNo, V["acDong"], V["acHo"], usedMin, VisitorMonthlyHours));
                        return null;   // 방문차 미처리 → 일반차량 입출차 흐름으로 폴백
                    }
                }

                // 인식결과 라벨에 차번 표시 (방문차는 early-return이라 정상흐름 라벨갱신(1102)을 못 타므로 직접 갱신). 방문차=초록.
                try {
                    if(CamIdx == 0 || CamIdx == 1) {
                        Label vlbl = (CamIdx == 0) ? frmLprMain.Main.lblCam1RegResult : frmLprMain.Main.lblCam2RegResult;
                        vlbl.Invoke((MethodInvoker)(() => vlbl.ForeColor = Color.Green));
                        vlbl.Invoke((MethodInvoker)(() => vlbl.Text = string.Format("인식결과 : {0}", CarNo)));
                    }
                } catch(Exception le) { Util.Logger.Log("방문차 인식결과 표시 오류 " + le.Message); }

                // 전광판·차단기를 DB기록보다 먼저 처리 — 일반/정기차량과 동일 순서로 응답 지연감 제거
                // (방문차는 DB기록 5개가 앞서 있어 릴레이·전광판이 늦게 반응하던 문제 수정)
                try {
                    if(Env.CommunicationEnv.DisPlay[CamIdx].Use && !frmLprMain.isFixed) {
                        string ment = "방문차량";
                        string c1 = Env.CommunicationEnv.DisPlay[CamIdx].Period1Color;
                        string c2 = Env.CommunicationEnv.DisPlay[CamIdx].Period2Color;
                        if(NetDev != null && ((CamIdx == 0 && Env.CommunicationEnv.DisPlay[0].Net.Use) || (CamIdx == 1 && Env.CommunicationEnv.DisPlay[1].Net.Use)))
                            NetDev.SendMsg(ment, clsFunction.GetColor8Int(c1), CarNoSpace(CarNo), clsFunction.GetColor8Int(c2));
                        else
                            SerialDev.DisPlayMent(CamIdx, ment, c1, CarNoSpace(CarNo), c2);
                        Util.Logger.Log(string.Format("방문차량 전광판 출력 {0} {1} {2}", CamIdx == 0 ? "CH1" : "CH2", ment, CarNo));
                        // 방문차 입차도 ~5초 후 대기복귀 — 일반/정기차와 동일하게 DisPlayTime 갱신(누락 시 '방문차량'이 계속 표시됨)
                        if(CamIdx == 0) {
                            if(Env.CommunicationEnv.DisPlay[CamIdx].Net.Use) frmLprMain.NetDisPlay1.DisPlayTime = DateTime.Now;
                            else frmLprMain.FirstDisPlayReturn.DisPlayTime = DateTime.Now;
                        } else {
                            if(Env.CommunicationEnv.DisPlay[CamIdx].Net.Use) frmLprMain.NetDisPlay2.DisPlayTime = DateTime.Now;
                            else frmLprMain.SecondDisPlayReturn.DisPlayTime = DateTime.Now;
                        }
                    }
                } catch(Exception de) { Util.Logger.Log("방문차 전광판 오류 " + de.Message); }

                try { GateOpen(CamIdx); } catch(Exception ge) { Util.Logger.Log("방문차 차단기 개방 오류 " + ge.Message); }

                if(ent) {
                    try { Util.clsMssql.ExecQuery(TCon, clsQuery.SetEntranceLprTrns(ProcTime, Env.CommunicationEnv.ParkInfo, true, CarNo, Image, recog, LprInfo.ChNo)); } catch(Exception le) { Util.Logger.Log("방문차 입차 LprTrns 오류 " + le.Message); }
                    if(LprInfo.LprOpt.Normal_Tckttrns)
                        try { Util.clsMssql.ExecQuery(TCon, clsQuery.SetEntranceTcktTrns(ProcTime, Env.CommunicationEnv.ParkInfo, LprInfo, CarNo, Image, "0", 0)); } catch(Exception te) { Util.Logger.Log("방문차 입차 TcktTrns 오류 " + te.Message); }
                    try { Util.clsMssql.ExecQuery(TCon, clsQuery.SetEntranceVisitorTrns(ProcTime, Env.CommunicationEnv.ParkInfo, LprInfo, V, CarNo, Image)); } catch(Exception vex) { Util.Logger.Log("방문차 입차 VisitorTrns 오류 " + vex.Message); }
                    if(LprInfo.LprOpt.Normal_Counter) {
                        try { Util.clsMssql.ExecQuery(TCon, clsQuery.SetEntranceFcStay(Env.CommunicationEnv.ParkInfo)); } catch { }
                        try { Util.clsMssql.ExecQuery(TCon, clsQuery.SetEntranceFcCountTrns(ProcTime, Env.CommunicationEnv.ParkInfo)); } catch { }
                    }
                } else {
                    try { Util.clsMssql.ExecQuery(TCon, clsQuery.SetExitLprTrns(ProcTime, Env.CommunicationEnv.ParkInfo, true, CarNo, Image, recog, LprInfo.ChNo)); } catch(Exception le) { Util.Logger.Log("방문차 출차 LprTrns 오류 " + le.Message); }
                    if(LprInfo.LprOpt.Normal_Tckttrns)
                        try { Util.clsMssql.ExecQuery(TCon, clsQuery.SetExitTcktTrns(ProcTime, Env.CommunicationEnv.ParkInfo, LprInfo, CarNo, Image)); } catch(Exception te) { Util.Logger.Log("방문차 출차 TcktTrns 오류 " + te.Message); }
                    try { Util.clsMssql.ExecQuery(TCon, clsQuery.SetExitVisitorTrns(ProcTime, Env.CommunicationEnv.ParkInfo, LprInfo, V, CarNo, Image)); } catch(Exception vex) { Util.Logger.Log("방문차 출차 VisitorTrns 오류 " + vex.Message); }
                    if(LprInfo.LprOpt.Normal_Counter) {
                        try { Util.clsMssql.ExecQuery(TCon, clsQuery.SetExitFcStay(Env.CommunicationEnv.ParkInfo)); } catch { }
                        try { Util.clsMssql.ExecQuery(TCon, clsQuery.SetExitFcCountTrns(ProcTime, Env.CommunicationEnv.ParkInfo)); } catch { }
                    }
                }
            } catch(Exception ex) {
                Util.Logger.Log("방문차량 처리 오류 " + ex.Message);
            }
            return Rtn;
        }

        public SqlConnection Get_MCon() {
            return MCon;
        }

        private void GateOpen(int idx) {
            if(idx < 2) {
                ClsStructure.Lpr_Info lprinfo = new ClsStructure.Lpr_Info();
                if(idx == 0)
                    lprinfo = Env.CommunicationEnv.Lpr1Info;
                else if(idx == 1)
                    lprinfo = Env.CommunicationEnv.Lpr2Info;
                if(lprinfo.InOutType == (int)ClsStructure.InoutType.입구용) {
                    if(FullSpaceControl.Use) {
                        if(FullSpaceControl.isFull || FullSpaceControl.ForceFull) {
                            Util.Logger.Log("만차 차단기 개방 안함");
                            if(!RegedCar[idx])
                                return;
                            else if(RegedCar[idx] && FullSpaceControl.Period)
                                return;
                        }
                    }
                }
            }
            Util.Logger.Log(string.Format("{0} 차단기 개방", idx + 1));
            SerialDev.GateOpen(idx);
        }

        //삼성 LPRTRNS 확장 필드 체크
        public bool CheckLprtrns() {
            int cnt = 0;
            try {
                string sql = string.Format("select top 1 * from {0}.dbo.LprTrns", Env.CommonEnv.DBInfo.TrnsDb);
                DataTable dt = Util.clsMssql.GetTable(TCon, sql);
                foreach(DataColumn item in dt.Columns) {
                    switch(item.ColumnName) {
                        case "acRegNo":
                        case "iInEqpm":
                        case "iClient":
                        case "acCarModel1":
                        case "acCarModel2":
                            cnt++;
                            break;
                    }
                }
            } catch { }
            return cnt == 5;
        }

        private string CarNoSpace(string carno) {
            //string PlateStr = Regex.Replace(carno, @"\d", "");
            //PlateStr = PlateStr.Replace(" ", "");
            //if (IsOtherChar(PlateStr))
            //{
            //    return Util.Common.LPadH(carno.Replace(PlateStr, " ".PadLeft(PlateStr.Length)), 12);
            //}
            //else
            //{
            //    return Util.Common.LPadH(carno, 12);
            //}
            //int len = Encoding.Default.GetBytes(carno).Length;
            char[] c = carno.ToCharArray();
            int len = c.Length;

            if(Env.CameraEnv.CoreCountry == CoreLogic.KOR)
                len = Encoding.Default.GetByteCount(carno);
            else {
                for(int i = 0; i < c.Length; i++) {
                    if(c[i] > 125)
                        len++;
                }
            }
            Console.WriteLine((len < 12 ? " ".PadLeft(12 - len) : "") + carno);
            return (len < 12 ? " ".PadLeft(12 - len) : "") + carno;
        }

        private bool IsOtherChar(string input) {
            if(input.Equals(null) || input.Length.Equals(0)) return false;

            for(int i = 0; i < input.Length; i++) {
                string rtnVal = string.Empty;
                char cStr = input[i];

                rtnVal += cStr + " : ";
                rtnVal += Char.GetUnicodeCategory(Convert.ToChar(cStr)).ToString();
                if(Char.GetUnicodeCategory(Convert.ToChar(cStr)).ToString() == "OtherLetter") {
                    if(!(cStr >= '\xAC00' && cStr <= '\xD7AF') //rtnVal += "    한글완성형";
                       || (cStr >= '\x3130' && cStr <= '\x318F')) //rtnVal += "    한글자음또는모음";
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
