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

        public DateTime LastGetMst;

        private SqlConnection MCon = new SqlConnection();
        private SqlConnection TCon = new SqlConnection();

        private Thread QueryThread;

        private clsSerialPort SerialDev = null;
        private NetworkDisplay NetDev = null;

        private bool[] RegedCar = new bool[2];

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
                if(frmLprMain.ENV.RegCarControl.Ilotarea)
                    query += string.Format("where Custdef.iLotarea = '{0}'", frmLprMain.ENV.CommunicationEnv.ParkInfo.No);
                CustDef = Util.clsMssql.GetTable(MCon, query);
                Util.Logger.Log(string.Format("정기권 정보 {0}건 취득", CustDef.Rows.Count));
                DCList = Util.clsMssql.GetTable(MCon, string.Format("select * from {0}.dbo.Customer_DcList", frmLprMain.ENV.CommonEnv.DBInfo.MstDB));

                WrongDef = Util.clsMssql.GetTable(MCon, string.Format("Select * from {0}.dbo.WrongCarDef", frmLprMain.ENV.CommonEnv.DBInfo.MstDB));
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
                TimeSpan diff = DateTime.Now - LastGetMst;

                ClsStructure.Lpr_Info LprInfo = new ClsStructure.Lpr_Info();
                switch(CamIdx) {
                    case 0:
                        LprInfo = Env.CommunicationEnv.Lpr1Info;
                        break;
                    case 1:
                        LprInfo = Env.CommunicationEnv.Lpr2Info;
                        break;
                }

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
                    Env.RegCarControl.iControlType = 0;
                    if(Type.Equals((int)ClsStructure.InoutType.입구용)) {
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
                            }
                        }
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
                                if(NetDev != null && ((CamIdx == 0 && Env.CommunicationEnv.DisPlay[0].Net.Use) || (CamIdx == 1 && Env.CommunicationEnv.DisPlay[1].Net.Use))) {
                                    NetDev.SendMsg(Ment1, clsFunction.GetColor8Int(NoDriving.Color1), Ment2, clsFunction.GetColor8Int(NoDriving.Color2));
                                } else {
                                    SerialDev.DisPlayMent(CamIdx, Ment1, NoDriving.Color1, Ment2, NoDriving.Color2);
                                }
                                // 3초 후 기본문구로 복귀
                                int _camIdx = CamIdx;
                                bool _netUse0 = Env.CommunicationEnv.DisPlay[0].Net.Use;
                                bool _netUse1 = Env.CommunicationEnv.DisPlay[1].Net.Use;
                                System.Threading.Tasks.Task.Run(async () => {
                                    await System.Threading.Tasks.Task.Delay(3000);
                                    if(_camIdx == 0) {
                                        if(_netUse0 && frmLprMain.NetDisPlay1 != null)
                                            frmLprMain.NetDisPlay1.DisPlayTime = DateTime.MinValue;
                                        else if(frmLprMain.FirstDisPlayReturn != null)
                                            frmLprMain.FirstDisPlayReturn.DisPlayTime = DateTime.MinValue;
                                    } else {
                                        if(_netUse1 && frmLprMain.NetDisPlay2 != null)
                                            frmLprMain.NetDisPlay2.DisPlayTime = DateTime.MinValue;
                                        else if(frmLprMain.SecondDisPlayReturn != null)
                                            frmLprMain.SecondDisPlayReturn.DisPlayTime = DateTime.MinValue;
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
                            if(blInfo.Apply || BlackOutDisplay) {
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
                                        item.Query = clsQuery.SetEntrancePassTrns(ProcTime, Env.CommunicationEnv.ParkInfo, (CamIdx.Equals(0) ? Env.CommunicationEnv.Lpr1Info : Env.CommunicationEnv.Lpr2Info), RegedInfo[0], CarNo, Number, Image);
                                    } else {
                                        Util.Logger.Log("요금 계산 대상 정기권 일반 입차 내역 기록");
                                        item.Query = clsQuery.SetEntranceTcktTrns(ProcTime, Env.CommunicationEnv.ParkInfo, (CamIdx.Equals(0) ? Env.CommunicationEnv.Lpr1Info : Env.CommunicationEnv.Lpr2Info), CarNo, Image, RegedInfo[0]["iGroup"].ToString(), irate);
                                    }
                                    Util.Logger.Query(item.Query);
                                    //QList.Add(item);
                                    Util.clsMssql.ExecQuery(TCon, item.Query);
                                }

                                if(!LprInfo.LprOpt.Period_Passtrns && RegedInfo[0]["iGroup"].ToString() == clsExceptGroup.ExceptGrpNo.ToString()) {
                                    Util.Logger.Log("일반 차량 입차 내역 기록");
                                    item.Query = clsQuery.SetEntranceTcktTrns(ProcTime, Env.CommunicationEnv.ParkInfo, (CamIdx.Equals(0) ? Env.CommunicationEnv.Lpr1Info : Env.CommunicationEnv.Lpr2Info), CarNo, Image, RegedInfo[0]["iGroup"].ToString(), irate);
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
                                    SendEnt_Msg = clsFunction.MakeTransMessage(Env.CameraEnv.SockDataFormat, CamIdx.Equals(0) ? Env.CameraEnv.IPCamera1Info.ChName : Env.CameraEnv.IPCamera2Info.ChName, CarNo, Env.CameraEnv.ImageSave.SavePath, Image, ProcTime);
                                    bool stxetx = CamIdx == 0 ? Env.CameraEnv.IPCamera1Info.SendStxEtx : Env.CameraEnv.IPCamera2Info.SendStxEtx;
                                    Util.Logger.Log(string.Format("정기차량 입차 정보 전송 {0}", SendEnt_Msg));
                                    if(stxetx)
                                        frmLprMain.Main.LprEntSvr.SendMsgSTXETX(SendEnt_Msg);
                                    else
                                        frmLprMain.Main.LprEntSvr.SendMsg(SendEnt_Msg);
                                }
                            } else {
                                if(LprInfo.LprOpt.Normal_Tckttrns) {
                                    Util.Logger.Log("일반 차량 입차 내역 기록");
                                    item.Query = clsQuery.SetEntranceTcktTrns(ProcTime, Env.CommunicationEnv.ParkInfo, (CamIdx.Equals(0) ? Env.CommunicationEnv.Lpr1Info : Env.CommunicationEnv.Lpr2Info), CarNo, Image, "0", irate);
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
                                    SendEnt_Msg = clsFunction.MakeTransMessage(Env.CameraEnv.SockDataFormat, CamIdx.Equals(0) ? Env.CameraEnv.IPCamera1Info.ChName : Env.CameraEnv.IPCamera2Info.ChName, CarNo, Env.CameraEnv.ImageSave.SavePath, Image, ProcTime);
                                    bool stxetx = CamIdx == 0 ? Env.CameraEnv.IPCamera1Info.SendStxEtx : Env.CameraEnv.IPCamera2Info.SendStxEtx;
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
                                item.Query = clsQuery.SetExitPassTrns(ProcTime, Env.CommunicationEnv.ParkInfo, (CamIdx.Equals(0) ? Env.CommunicationEnv.Lpr1Info : Env.CommunicationEnv.Lpr2Info), RegedInfo[0], CarNo, Number, Image);
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
                                item.Query = clsQuery.SetExitTcktTrns(ProcTime, Env.CommunicationEnv.ParkInfo, (CamIdx.Equals(0) ? Env.CommunicationEnv.Lpr1Info : Env.CommunicationEnv.Lpr2Info), CarNo, Image);
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
                                                        item.Query = clsQuery.SetExitTcktTrns(ProcTime, Env.CommunicationEnv.ParkInfo, (CamIdx.Equals(0) ? Env.CommunicationEnv.Lpr1Info : Env.CommunicationEnv.Lpr2Info), CarNo, Image);
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
                                        item.Query = clsQuery.SetExitTcktTrns(ProcTime, Env.CommunicationEnv.ParkInfo, (CamIdx.Equals(0) ? Env.CommunicationEnv.Lpr1Info : Env.CommunicationEnv.Lpr2Info), CarNo, Image);
                                        Util.Logger.Query(item.Query);
                                        //QList.Add(item);
                                        Util.clsMssql.ExecQuery(TCon, item.Query);
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
                                    SendCal_Msg = clsFunction.MakeTransMessage(Env.CameraEnv.SockDataFormat, CamIdx.Equals(0) ? Env.CameraEnv.IPCamera1Info.ChName : Env.CameraEnv.IPCamera2Info.ChName, CarNo, Env.CameraEnv.ImageSave.SavePath, Image, ProcTime);
                                else
                                    SendCal_Msg = string.Format("!{0}#{1}#{2}#LAGOUT", LprInfo.ChNo, CarNo, Image);
                                //if (Env.CameraEnv.SockDataFormat == (int)ClsStructure.SockFormat.Kukje)
                                bool stxetx = CamIdx == 0 ? Env.CameraEnv.IPCamera1Info.SendStxEtx : Env.CameraEnv.IPCamera2Info.SendStxEtx;
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
                    lbl.Invoke((MethodInvoker)(() => lbl.ForeColor = color));
                    lbl.Invoke((MethodInvoker)(() => lbl.Text = string.Format("인식결과 : {0}", CarNo)));
                } else {
                    Util.Logger.Log("APB 입력 중단 입출구 타입 " + Type.ToString());
                }
                if(Env.SendOffice) {
                    string OfficeMsg = clsFunction.MakeTransMessage(Env.CameraEnv.SockDataFormat, CamIdx.Equals(0) ? Env.CameraEnv.IPCamera1Info.ChName : Env.CameraEnv.IPCamera2Info.ChName, CarNo, Env.CameraEnv.ImageSave.SavePath, Image, ProcTime);
                    frmLprMain.Main.SendOfficeList.Add(string.Format("CAPTURE:{0}", OfficeMsg));
                }
            } catch(Exception DataProcess_Error) {
                Util.Logger.Query(string.Format("DataProcess_Error : {0}", DataProcess_Error.Message));
            } finally {
                Processing = false;
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
