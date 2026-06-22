using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace KyungsinLPR
{
    public class clsQuery
    {
        //private Mssql DB;

        public static string SetEntrancePassTrns(DateTime ProcTime, ClsStructure.Park_Info ParkInfo, ClsStructure.Lpr_Info LprInfo, 
            DataRow Row, String Carno, String Number, String EntrancePic)
        {
            String Sql = string.Empty;
            //Sql = " INSERT INTO PASSTRNS "
            //        + " ( "
            //        + " iLotArea, iInClient, iInEqpm, iPaymentType, iticket, dTInDate, iCardType, dPasscardNo, acInTime, acUserName, acPlate1, acPlate2, dtMgmntDate, acEntrancePicName, iGroup, DongCode, HoCode "
            //        + " ) "
            //        + " VALUES "
            //        + " ( ";
            //Sql += string.Format("{0}, {1}, {2}, 0, 0, '{3}', {4}, {5}, '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', {12}, '{13}', '{14}')",
            //    ParkInfo.No, ParkInfo.Client_No, LprInfo.EqpmNo, ProcTime.ToString("yyyy-MM-dd HH:mm:ss"), Row["iPsscrdType"].ToString(), Row["iUser"].ToString(),
            //    ProcTime.ToString("HH:mm"), Row["acUserName"].ToString(), Row["acPlate1"].ToString(), Number, ProcTime.ToString("yyyy-MM-dd"),
            //    EntrancePic, Row["iGroup"].ToString(), Row["DongCode"].ToString(), Row["HoCode"].ToString());
            Sql = string.Format("INSERT INTO {0}.dbo.PASSTRNS (iLotArea, iInClient, iInEqpm, iPaymentType, iticket, dTInDate, dFee, dPaid, dChange, dIncome, iAccountFlag, iVoidUseFlag, ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            Sql += "iOutClient, iOutEqpm, iPayClient, iPayEqpm, iCardType, iInOutStatus, iRate, dShortAmount, iCardRate, dTransNo, dDebitAmount, dMisc1, dMisc2, dMisc3, ";
            Sql += "iOperator, dInsffcntPayout, iPaymentMode, dPasscardNo, dDebitCardNo, iInEqpmType, iOutEqpmType, acInTime, acUserName, acPlate1, acPlate2, ";
            Sql += "acTelNo, iGroup, dtMgmntDate, iExtendLotArea, dParkingAmount, acEntrancePicName, Dongcode, Hocode, iSrvrupdtFlag) \r\n";
            // 정기차량 입차 — iticket: CUSTDEF.iticket (정기권번호) 사용 (기존 0 하드코딩 수정)
            Sql += string.Format("select {0}, {1}, {2}, 0, iticket, '{3}', 0, 0, 0, 0, 0, 0, ", ParkInfo.No, ParkInfo.Client_No, LprInfo.EqpmNo, ProcTime.ToString("yyyy-MM-dd HH:mm:00"));
            Sql += string.Format("0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, iticket, 0, 0, 0, '{0}', acusername, N'{3}', acplate2, '', igroup, '{1}', 0, 0, N'{2}', dongcode, hocode, 0 "
                , ProcTime.ToString("HH:mm"), ProcTime.ToString("yyyy-MM-dd"), EntrancePic, Carno);
            Sql += string.Format("from {0}.dbo.custdef \r\n", frmLprMain.ENV.CommonEnv.DBInfo.MstDB);
            //Sql += string.Format("where {0}.dbo.custdef.acplate1 = '{1}'", frmLprMain.ENV.CommonEnv.DBInfo.MstDB, Carno);
            Sql += string.Format("inner join(select acplate1, max(iModifiedDate) iModifiedDate from {0}.dbo.custdef group by acplate1) grp ", frmLprMain.ENV.CommonEnv.DBInfo.MstDB);
            Sql += "on custdef.acPlate1 = grp.acPlate1 and CUSTDEF.iModifiedDate = grp.iModifiedDate ";
            Sql += string.Format("where ilotarea = {2} and N'{1}' in ({0}.dbo.custdef.acplate1, {0}.dbo.custdef.acplate2, {0}.dbo.custdef.acplate3) ", frmLprMain.ENV.CommonEnv.DBInfo.MstDB, Carno, frmLprMain.ENV.CommunicationEnv.ParkInfo.No);
            //Util.Logger.Log(Sql);
            return Sql;
        }

        public static string SetExitPassTrns(DateTime ProcTime, ClsStructure.Park_Info ParkInfo, ClsStructure.Lpr_Info LprInfo, DataRow Row, String Carno, String Number, String ExitPic)
        {
            String Sql = string.Empty;
            //Sql = string.Format("if not exists (select * from passtrns where acplate1 = '{0}' ", Carno);
            //Sql += string.Format("and iid = (select max(iid) from passtrns where acplate1 = '{0}') and dtoutdate is null) ", Carno);
            //Sql += " INSERT INTO PASSTRNS ("
            //        + " iLotArea, iPaymentType, dtOutDate, dtPayDate, iOutClient, iOutEqpm, iPayClient, iPayEqpm, iCardType, iInOutStatus, "
            //        + " dPasscardNo, acOutTime, acPayTime, acUserName, acPlate1, acPlate2, iGroup, dtMgmntDate, acGoOutPicName, DongCode, HoCode) "
            //        + " VALUES (";
            //Sql += string.Format("{0}, 0, '{1}', '{1}', {2}, {3}, {2}, {3}, {4}, {5}, {6}, '{7}', '{7}', '{8}', '{9}', '{10}', {11}, {12}, '{13}', '{14}', '{15}')",
            //    ParkInfo.No, ProcTime.ToString("yyyy-MM-dd HH:mm:ss"), ParkInfo.Client_No, LprInfo.EqpmNo, Row["iPsscrdType"].ToString(), 1
            //    , Row["iUser"].ToString(), ProcTime.ToString("HH:mm"), Row["acUserName"].ToString(), Row["acPlate1"].ToString(), Number, Row["iGroup"].ToString(), ProcTime.ToString("yyyy-MM-dd"),
            //    ExitPic, Row["DongCode"].ToString(), Row["HoCode"].ToString());
            //Sql += "else ";
            //Sql += " Update PASSTRNS Set ";
            //Sql += "iPaymentType = 0, ";
            //Sql += string.Format(" dtOutDate = '{0}', ", ProcTime.ToString("yyyy-MM-dd HH:mm:ss"));
            //Sql += string.Format(" dtPayDate = '{0}', ", ProcTime.ToString("yyyy-MM-dd HH:mm:ss"));
            //Sql += string.Format(" iOutClient = '{0}', ", ParkInfo.Client_No);
            //Sql += string.Format(" iOutEqpm = '{0}', ", LprInfo.EqpmNo);
            //Sql += string.Format(" iPayClient = '{0}', ", ParkInfo.Client_No);
            //Sql += string.Format(" iPayEqpm = '{0}', ", LprInfo.EqpmNo);
            //Sql += string.Format(" iCardType = '{0}', ", Row["iPsscrdType"].ToString());
            //Sql += string.Format(" iInOutStatus = {0}, ", 1);
            //Sql += string.Format(" dPasscardNo = '{0}', ", Row["iUser"].ToString());
            //Sql += string.Format(" acOutTime = '{0}', ", ProcTime.ToString("HH:mm"));
            //Sql += string.Format(" acPayTime = '{0}', ", ProcTime.ToString("HH:mm"));
            //Sql += string.Format(" acUserName = '{0}', ", Row["acUserName"].ToString());
            //Sql += string.Format(" acPlate1 = '{0}', ", Row["acPlate1"].ToString());
            //Sql += string.Format(" acPlate2 = '{0}', ", Row["acPlate2"].ToString());
            //Sql += string.Format(" iGroup = '{0}', ", Row["iGroup"].ToString());
            //Sql += string.Format(" dtMgmntDate = '{0}', ", ProcTime.ToString("yyyy-MM-dd"));
            //Sql += string.Format(" acGoOutPicName = '{0}', ", ExitPic);
            //Sql += string.Format(" DongCode = '{0}', ", Row["DongCode"].ToString());
            //Sql += string.Format(" HoCode = '{0}' ", Row["HoCode"].ToString());
            //Sql += string.Format(" where iid = (select max(iid) from passtrns where acplate1 = '{0}') ", Carno);

            //Sql = string.Format("if not exists(select * from {0}.dbo.passtrns where iid = (select max(iid) from {0}.dbo.passtrns where acplate1 = '{1}') and dtoutdate is null)", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, Carno);
            Sql = string.Format("if not exists(select * from {0}.dbo.passtrns where iid = (select max(iid) from {0}.dbo.passtrns where iLotArea = {1} and N'{2}' = acplate1) and dtoutdate is null)", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, frmLprMain.ENV.CommunicationEnv.ParkInfo.No, Carno);
            //출차 데이터 Insert
            Sql += string.Format("INSERT INTO {0}.dbo.PASSTRNS (iLotArea, iInClient, iInEqpm, iPaymentType, iticket, dtOutDate, dtPayDate, dFee, dPaid, dChange, dIncome, iAccountFlag, iVoidUseFlag, \r\n", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            Sql += "iOutClient, iOutEqpm, iPayClient, iPayEqpm, iCardType, iInOutStatus, iRate, dShortAmount, iCardRate, dTransNo, dDebitAmount, dMisc1, dMisc2, dMisc3, \r\n";
            Sql += "iOperator, dInsffcntPayout, iPaymentMode, dPasscardNo, dDebitCardNo, iInEqpmType, iOutEqpmType, acOutTime, acPayTime, acUserName, acPlate1, acPlate2, \r\n";
            Sql += "acTelNo, iGroup, dtMgmntDate, iExtendLotArea, dParkingAmount, acGoOutPicName, Dongcode, Hocode, iSrvrupdtFlag) \r\n";
            Sql += string.Format("select {0}, 0, 0, 0, 0, '{3}', '{3}', 0, 0, 0, 0, 0, 0, {1}, {2}, 0, 0, 1, 1, 0, 0, 0, 0, 0, \r\n", ParkInfo.No, ParkInfo.Client_No, LprInfo.EqpmNo, ProcTime.ToString("yyyy-MM-dd HH:mm:ss"));
            Sql += string.Format("0, 0, 0, 0, 0, 0, iticket, 0, 0, 0, '{0}', '{0}', acusername, N'{3}', acplate2, '', igroup, '{1}', 0, 0, N'{2}', dongcode, hocode, 0 \r\n"
                , ProcTime.ToString("HH:mm"), ProcTime.ToString("yyyy-MM-dd"), ExitPic, Carno);
            Sql += string.Format("from {0}.dbo.custdef ", frmLprMain.ENV.CommonEnv.DBInfo.MstDB);
            //Sql += string.Format("where {0}.dbo.custdef.acplate1 = '{1}' \r\n", frmLprMain.ENV.CommonEnv.DBInfo.MstDB, Carno);
            Sql += string.Format("inner join(select acplate1, max(iModifiedDate) iModifiedDate from {0}.dbo.custdef group by acplate1) grp ", frmLprMain.ENV.CommonEnv.DBInfo.MstDB);
            Sql += "on custdef.acPlate1 = grp.acPlate1 and CUSTDEF.iModifiedDate = grp.iModifiedDate ";
            Sql += string.Format("where iLotArea = {0} and N'{1}' in (custdef.acplate1, custdef.acplate2, custdef.acplate3) \r\n"
                , frmLprMain.ENV.CommunicationEnv.ParkInfo.No, Carno);
            Sql += "Else \r\n";
            Sql += string.Format("Update {0}.dbo.PASSTRNS Set \r\n", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            Sql += "iPaymentType = 6, \r\n";
            Sql += string.Format("dtoutdate = '{0}', ", ProcTime.ToString("yyyy-MM-dd HH:mm:ss"));
            Sql += string.Format("dtpaydate = '{0}', \r\n", ProcTime.ToString("yyyy-MM-dd HH:mm:ss"));
            Sql += string.Format("dFee = 0, dPaid = 0, \r\n");
            Sql += string.Format("dChange = 0, dIncome = 0, \r\n");
            Sql += string.Format("iAccountFlag = 0, iVoidUseFlag = 0, \r\n");
            Sql += string.Format("iOutClient = {0}, ", ParkInfo.Client_No);
            Sql += string.Format("iOutEqpm = {0}, \r\n", LprInfo.EqpmNo);
            Sql += string.Format("iPayClient = {0}, ", ParkInfo.Client_No);
            Sql += string.Format("iPayEqpm = {0}, \r\n", LprInfo.EqpmNo);
            Sql += string.Format("iCardType = 1, iInOutStatus = 1, \r\n");
            Sql += string.Format("iRate = 0, dShortAmount = 0, \r\n");
            Sql += string.Format("iCardRate = 0, dTransNo = 0, \r\n");
            Sql += string.Format("dDebitAmount = 0, dMisc1 = 0, \r\n");
            Sql += string.Format("dMisc2 = 0, dMisc3 = 0, \r\n");
            Sql += string.Format("acCarStayHours = '0' + substring(convert(nvarchar(16), '{0}' - dtindate,121), 12,5), \r\n", ProcTime.ToString("yyyy-MM-dd HH:mm:00"));
            Sql += string.Format("iOperator = 0, dInsffcntPayout = 0, \r\n");
            Sql += string.Format("iPaymentMode = 0, dDebitCardNo = 0, \r\n");
            Sql += string.Format("iInEqpmType = 0, iOutEqpmType = 0, \r\n");
            Sql += string.Format("iExtendLotArea = 0, dParkingAmount = 0, \r\n");
            Sql += string.Format("iSrvrupdtFlag = 0, \r\n");
            Sql += string.Format("dtPaymentDate = '{0}', ", ProcTime.ToString("yyyy-MM-dd HH:mm:ss"));
            Sql += string.Format("acOutTime = '{0}', \r\n", ProcTime.ToString("HH:mm"));
            Sql += string.Format("acPayTime = '{0}', ", ProcTime.ToString("HH:mm"));
            Sql += string.Format("acGoOutPicName = N'{0}' \r\n", ExitPic);
            //Sql += string.Format("where iid = (select max(iid) from {0}.dbo.PASSTRNS where acplate1 = '{1}')", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, Carno);
            Sql += string.Format("where iid = (select max(iid) from {0}.dbo.PASSTRNS where iLotArea = {1} and N'{2}' = acplate1)", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, frmLprMain.ENV.CommunicationEnv.ParkInfo.No, Carno);
            //Util.Logger.Log(Sql);
            return Sql;
        }

        public static string SetEntranceTcktTrns(DateTime ProcTime, ClsStructure.Park_Info ParkInfo, ClsStructure.Lpr_Info LprInfo, String Carno, String EntrancePic, string Group = "0", int irate = 0)
        {
            String Sql = string.Empty;
            if (Group == string.Empty)
                Group = "0";
            //유진 현장 ProcTime DateTime MinValue 경우 발생 보완
            if (ProcTime == DateTime.MinValue)
            {
                string[] sp = EntrancePic.Split('_');
                //year  sp[sp.Length - 1].Substring(0, 4)
                //month sp[sp.Length - 1].Substring(4, 2)
                //day   sp[sp.Length - 1].Substring(6, 2)
                //hour  sp[sp.Length - 1].Substring(8, 2)
                //min   sp[sp.Length - 1].Substring(10, 2)
                //sec   sp[sp.Length - 1].Substring(12, 2)
                string ddHHmmss = string.Format("{0}{1}{2}{3}", sp[sp.Length - 1].Substring(6, 2), sp[sp.Length - 1].Substring(8, 2), sp[sp.Length - 1].Substring(10, 2), sp[sp.Length - 1].Substring(12, 2));
                string yyyymmddhhmmss = string.Format("{0}-{1}-{2} {3}:{4}:00", sp[sp.Length - 1].Substring(0, 4), sp[sp.Length - 1].Substring(4, 2), sp[sp.Length - 1].Substring(6, 2),
                    sp[sp.Length - 1].Substring(8, 2), sp[sp.Length - 1].Substring(10, 2), sp[sp.Length - 1].Substring(12, 2));
                string hh_mm = string.Format("{0}:{1}", sp[sp.Length - 1].Substring(8, 2), sp[sp.Length - 1].Substring(10, 2));
                string yyyymmdd = string.Format("{0}-{1}-{2}", sp[sp.Length - 1].Substring(0, 4), sp[sp.Length - 1].Substring(4, 2), sp[sp.Length - 1].Substring(6, 2));
                Sql = string.Format(" INSERT INTO {0}.dbo.TCKTTRNS (iLotArea, iInClient, iInEqpm, iPaymentType, iTicket, dtInDate, dFee, dPaid, dChange, dIncome, ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
                Sql += "iServiceA, dServiceA, iServiceB, dServiceB, iServiceC, iAccountFlag, iVoidUseFlag, iOutClient, iOutEqpm, iPayClient, iPayEqpm, iCardType, iInOutStatus, ";
                Sql += "iRate, dShortAmount, iOTPUsedAmount, iCardRate, dTransNo, dDebitAmount, iShopNo1, dShopAmount1, iShopNo2, dShopAmount2, iShopNo3, dShopAmount3, ";
                Sql += "sTax, dMisc1, dMisc2, dMisc3, iOperator, dInsffcntPayout, iPaymentMode, dPasscardNo, dDebitCardNo, dEventCardNo, dOPT_CardNo, iDsctnc_A_SvcCard, ";
                Sql += "iDsctnc_B_SvcCard, iDsctnc_C_SvcCard, iInEqpmType, iOutEqpmType, acInTime, acUserName, acPlate1, iGroup, dtMgmntdate, iExtendLotArea, dParkingAmount, ";
                Sql += "acEntrancePicName, dPayBack, iSrvrupdtFlag) VALUES (";
                Sql += string.Format("{0}, {1}, {1}, 0, '{3}', '{4}', 0, 0, 0, 0, ", ParkInfo.No, ParkInfo.Client_No, LprInfo.EqpmNo, ddHHmmss, yyyymmddhhmmss);
                Sql += "0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, ";
                Sql += string.Format("{0}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, ", irate);
                Sql += "0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, ";
                Sql += string.Format("0, 0, 0, 0, '{0}', N'{1}', N'{2}', {5}, '{3}', {4}, 0, ", hh_mm, Carno, Carno, yyyymmdd, ParkInfo.No, Group);
                Sql += string.Format("N'{0}', 0, 0)", EntrancePic);
            }
            else
            {
                Sql = string.Format(" INSERT INTO {0}.dbo.TCKTTRNS (iLotArea, iInClient, iInEqpm, iPaymentType, iTicket, dtInDate, dFee, dPaid, dChange, dIncome, ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
                Sql += "iServiceA, dServiceA, iServiceB, dServiceB, iServiceC, iAccountFlag, iVoidUseFlag, iOutClient, iOutEqpm, iPayClient, iPayEqpm, iCardType, iInOutStatus, ";
                Sql += "iRate, dShortAmount, iOTPUsedAmount, iCardRate, dTransNo, dDebitAmount, iShopNo1, dShopAmount1, iShopNo2, dShopAmount2, iShopNo3, dShopAmount3, ";
                Sql += "sTax, dMisc1, dMisc2, dMisc3, iOperator, dInsffcntPayout, iPaymentMode, dPasscardNo, dDebitCardNo, dEventCardNo, dOPT_CardNo, iDsctnc_A_SvcCard, ";
                Sql += "iDsctnc_B_SvcCard, iDsctnc_C_SvcCard, iInEqpmType, iOutEqpmType, acInTime, acUserName, acPlate1, iGroup, dtMgmntdate, iExtendLotArea, dParkingAmount, ";
                Sql += "acEntrancePicName, dPayBack, iSrvrupdtFlag) VALUES (";
                Sql += string.Format("{0}, {1}, {2}, 0, '{3}', '{4}', 0, 0, 0, 0, ", ParkInfo.No, ParkInfo.Client_No, LprInfo.EqpmNo, ProcTime.ToString("ddHHmmss"), ProcTime.ToString("yyyy-MM-dd HH:mm:00"));
                Sql += "0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, ";
                Sql += string.Format("{0}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, ", irate);
                Sql += "0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, ";
                Sql += string.Format("0, 0, 0, 0, '{0}', N'{1}', N'{2}', {5}, '{3}', {4}, 0, ", ProcTime.ToString("HH:mm"), Carno, Carno, ProcTime.ToString("yyyy-MM-dd"), ParkInfo.No, Group);
                Sql += string.Format("N'{0}', 0, 0)", EntrancePic);
            }
            //Util.Logger.Log(Sql);
            return Sql;
        }

        public static string SetExitTcktTrns(DateTime ProcTime, ClsStructure.Park_Info ParkInfo, ClsStructure.Lpr_Info LprInfo, String Carno, String ExitPic)
        {
            String Sql = string.Empty;
            //Sql = string.Format("if not exists (select * from {0}.dbo.TCKTTRNS where acplate1 = '{1}' ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, Carno);
            //Sql = string.Format("if not exists (select * from {0}.dbo.TCKTTRNS where acplate1 = '{1}' ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, Carno);
            ////Sql += string.Format("and iid = (select max(iid) from {0}.dbo.TCKTTRNS where acplate1 = '{1}') and dtoutdate is null )", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, Carno);
            //Sql += string.Format("and iid = (select max(iid) from {0}.dbo.TCKTTRNS where acplate1 = '{1}') and dtoutdate is null )", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, Carno);
            //Sql += " INSERT INTO TCKTTRNS "
            //        + " ( "
            //        + " iLotArea, iTicket, dtOutDate, dtPayDate, iOutClient, iOutEqpm, acOutTime, acUserName, acPlate1, dtMgmntDate, iExtendLotArea, acGoOutPicName"
            //        + " ) "
            //        + " VALUES "
            //        + " ( ";
            string Out_Insert = string.Format(" INSERT INTO {0}.dbo.TCKTTRNS (iLotArea, iInClient, iInEqpm, iPaymentType, iTicket, dtOutDate, dFee, dPaid, dChange, dIncome, ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            Out_Insert += "iServiceA, dServiceA, iServiceB, dServiceB, iServiceC, iAccountFlag, iVoidUseFlag, iOutClient, iOutEqpm, iPayClient, iPayEqpm, iCardType, iInOutStatus, ";
            Out_Insert += "iRate, dShortAmount, iOTPUsedAmount, iCardRate, dTransNo, dDebitAmount, iShopNo1, dShopAmount1, iShopNo2, dShopAmount2, iShopNo3, dShopAmount3, ";
            Out_Insert += "sTax, dMisc1, dMisc2, dMisc3, iOperator, dInsffcntPayout, iPaymentMode, dPasscardNo, dDebitCardNo, dEventCardNo, dOPT_CardNo, iDsctnc_A_SvcCard, ";
            Out_Insert += "iDsctnc_B_SvcCard, iDsctnc_C_SvcCard, iInEqpmType, iOutEqpmType, acOutTime, acUserName, acPlate1, iGroup, dtMgmntdate, iExtendLotArea, dParkingAmount, ";
            Out_Insert += "acGoOutPicName, dPayBack, iSrvrupdtFlag) VALUES (";
            Out_Insert += string.Format("{0}, 0, 0, 0, '{1}', '{2}', 0, 0, 0, 0, ", ParkInfo.No, ProcTime.ToString("ddHHmmss"), ProcTime.ToString("yyyy-MM-dd HH:mm:ss"));
            Out_Insert += string.Format("0, 0, 0, 0, 0, 0, 0, {0}, {1}, {0}, {1}, 0, 1, ", ParkInfo.Client_No, LprInfo.EqpmNo);
            Out_Insert += "0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, ";
            Out_Insert += "0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, ";
            Out_Insert += string.Format("0, 0, 0, 0, '{0}', N'{1}', N'{2}', 0, '{3}', {4}, 0, ", ProcTime.ToString("HH:mm"), Carno, Carno, ProcTime.ToString("yyyy-MM-dd"), ParkInfo.No);
            Out_Insert += string.Format("N'{0}', 0, 0)", ExitPic);
            //Sql += string.Format("'{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}')",
            //    ParkInfo.No, ProcTime.ToString("ddHHmmss"), ProcTime.ToString("yyyy-MM-dd HH:mm:ss"), ParkInfo.Client_No, LprInfo.EqpmNo,
            //    ProcTime.ToString("HH:mm"), Carno, Carno, ProcTime.ToString("yyyy-MM-dd"),
            //    ParkInfo.No, ExitPic);
            string PayUpdate = string.Format("update {0}.dbo.tckttrns Set dtoutdate = getdate(), iOutClient = {1}, iOutEqpm = {2}, acGoOutPicName = '{3}' "
                , frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, frmLprMain.ENV.CommunicationEnv.ParkInfo.Client_No, LprInfo.EqpmNo, ExitPic);
            PayUpdate += "where iid = @iid";
            
            string OutUpdate = string.Format("UPDATE {0}.dbo.TCKTTRNS SET ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            OutUpdate += string.Format("dtOutDate = '{0}', ", ProcTime.ToString("yyyy-MM-dd HH:mm:ss"));
            OutUpdate += string.Format("dtPayDate = '{0}', ", ProcTime.ToString("yyyy-MM-dd HH:mm:ss"));
            OutUpdate += string.Format("iOutClient = '{0}', ", ParkInfo.Client_No);
            OutUpdate += string.Format("iOutEqpm = '{0}', ", LprInfo.EqpmNo);
            OutUpdate += string.Format("acCarStayHours = substring(convert(nvarchar(16), '{0}' - dtindate,121), 12,5), ", ProcTime.ToString("yyyy-MM-dd HH:mm:00"));
            OutUpdate += string.Format("acOutTime = '{0}', ", ProcTime.ToString("HH:mm"));
            OutUpdate += string.Format("acUserName = N'{0}', ", Carno);
            OutUpdate += string.Format("acPlate1 = N'{0}', ", Carno);
            OutUpdate += string.Format("acPlate2 = N'{0}', ", Carno);
            OutUpdate += string.Format("dtMgmntDate = '{0}', ", ProcTime.ToString("yyyy-MM-dd HH:mm:ss"));
            OutUpdate += string.Format("acGoOutPicName = N'{0}' ", ExitPic);
            OutUpdate += "where iid = @iid";
            //Util.Logger.Log(Sql);
            Sql = "declare @Ent datetime \r\n";
            Sql += "declare @Exit datetime \r\n";
            Sql += "declare @Pay datetime \r\n";
            Sql += "declare @iid int \r\n";
            Sql += string.Format("select @iid = max(iid) from {0}.dbo.tckttrns where acplate1 = '{1}' \r\n", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, Carno);
            Sql += string.Format("select @Ent = dtindate from {0}.dbo.tckttrns where iid = @iid \r\n", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            Sql += string.Format("select @Exit = dtoutdate from {0}.dbo.tckttrns where iid = @iid \r\n", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            Sql += string.Format("select @Pay = dtpaydate from {0}.dbo.tckttrns where iid = @iid \r\n", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            Sql += "if (@iid is null or @ent is null) \r\n";
            Sql += "	" + Out_Insert + " \r\n";
            Sql += "else \r\n";
            Sql += "	begin \r\n";
            Sql += "	if (@ent is not null) \r\n";
            Sql += "		begin \r\n";
            Sql += "			if (@pay is not null and @exit is null) \r\n";
            Sql += "				" + PayUpdate + " \r\n";
            Sql += "			else if (@pay is null and @exit is null) \r\n";
            Sql += "				" + OutUpdate + " \r\n";
            Sql += "			else if (@ent is not null and @exit is not null) \r\n";
            Sql += "				" + Out_Insert + " \r\n";
            Sql += "		end \r\n";
            Sql += "	end \r\n";
            return Sql;
        }

        public static string SetEntranceLprTrns(DateTime ProcTime, ClsStructure.Park_Info ParkInfo, bool Reged, String Carno, String EntrancePic, bool Recognition)
        {
            String Sql = string.Empty;
            Sql = string.Format(" INSERT INTO {0}.dbo.LPRTRNS (iLotArea, acPlate, dtTrnsDate, iInOutStatus, iCardType, acPicName, iRecognitionFlg) "
                + " VALUES "
                + " ( ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            Sql += string.Format("{0}, N'{1}', '{2}', {3}, {4}, N'{5}', {6})",
                ParkInfo.No, Carno, ProcTime.ToString("yyyy-MM-dd HH:mm:ss"), 0, Reged == true ? 1 : 0,
                EntrancePic, Recognition == true ? 0 : 1);
            //Util.Logger.Log(Sql);
            return Sql;
        }

        public static string SetEntranceLprTrns(DateTime ProcTime, ClsStructure.Park_Info ParkInfo, bool Reged, String Carno, String EntrancePic, bool Recognition, string ChNo)
        {
            String Sql = string.Empty;
            int chno = Util.Function.IntTryParse(ChNo.Replace("CH", ""));
            Sql = string.Format(" INSERT INTO {0}.dbo.LPRTRNS (iLotArea, acPlate, dtTrnsDate, iInOutStatus, iCardType, acPicName, iRecognitionFlg, iEqpm) "
                + " VALUES "
                + " ( ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            Sql += string.Format("{0}, N'{1}', '{2}', {3}, {4}, N'{5}', {6}, {7})",
                ParkInfo.No, Carno, ProcTime.ToString("yyyy-MM-dd HH:mm:ss"), 0, Reged == true ? 1 : 0,
                EntrancePic, Recognition == true ? 0 : frmLprMain.ENV.RegCarControl.iControlType == 0 ? 1 : frmLprMain.ENV.RegCarControl.iControlType, chno);
            //Util.Logger.Log(Sql);
            return Sql;
        }

        //삼성 LPRTRNS 
        public static string SetEntranceLprTrns(DateTime ProcTime, ClsStructure.Park_Info ParkInfo, bool Reged, String Carno, String EntrancePic, bool Recognition, string ChNo
            , string EmpNo, string CarModel1, string CarModel2)
        {
            String Sql = string.Empty;
            int chno = Util.Function.IntTryParse(ChNo.Replace("CH", ""));
            //Sql = string.Format("use {0}\r\n", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            //Sql += "IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE  TABLE_NAME = 'LPRTRNS' AND COLUMN_NAME in ('acRegNo', 'iInEqpm', 'iClient', 'acCarModel1', 'acCarModel2'))\r\n";
            //Sql += "    Begin\r\n";
            //Sql += string.Format("      INSERT INTO {0}.dbo.LPRTRNS (iLotArea, acPlate, dtTrnsDate, iInOutStatus, iCardType, acPicName, iRecognitionFlg, iEqpm) "
            //    + " VALUES "
            //    + " ( ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            //Sql += string.Format("{0}, '{1}', '{2}', {3}, {4}, '{5}', {6}, {7})\r\n",
            //    ParkInfo.No, Carno, ProcTime.ToString("yyyy-MM-dd HH:mm:ss"), 0, Reged == true ? 1 : 0,
            //    EntrancePic, Recognition == true ? 0 : 1, chno);
            //Sql += "    End\r\n";
            //Sql += "Else\r\n";
            //Sql += "    Begin\r\n";
            //Sql += string.Format("      INSERT INTO {0}.dbo.LPRTRNS (iLotArea, acPlate, dtTrnsDate, iInOutStatus, iCardType, acPicName "
            //    + ", iRecognitionFlg, iSrvrupdtFlag, acRegNo, iInEqpm, iClient, acCarModel1, acCarModel2, iEqpm) VALUES ("
            //    , frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            //Sql += string.Format("{0}, '{1}', '{2}', {3}, {4}, '{5}', {6}, 0, '{7}', {8}, {9}, '{10}', '{11}', 0)\r\n",
            //    ParkInfo.No, Carno, ProcTime.ToString("yyyy-MM-dd HH:mm:ss"), 0, Reged == true ? 1 : 0,
            //    EntrancePic, Recognition == true ? 0 : 1, EmpNo, chno, ParkInfo.Client_No, CarModel1, CarModel2);
            //Sql += "    End\r\n";
            int iCardType = 0;
            if (frmLprMain.ENV.RegCarControl.iControlType == 0)
                iCardType = Reged == true ? 1 : 0;
            else
                iCardType = frmLprMain.ENV.RegCarControl.iControlType;
            if (!frmLprMain.ExtendLprtrns)
            {
                Sql = string.Format("      INSERT INTO {0}.dbo.LPRTRNS (iLotArea, acPlate, dtTrnsDate, iInOutStatus, iCardType, acPicName, iRecognitionFlg, iEqpm) "
                    + " VALUES "
                    + " ( ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
                Sql += string.Format("{0}, N'{1}', '{2}', {3}, {4}, N'{5}', {6}, {7})\r\n",
                    ParkInfo.No, Carno, ProcTime.ToString("yyyy-MM-dd HH:mm:ss"), 0, iCardType,
                    EntrancePic, Recognition == true ? 0 : 1, chno);
            }
            else
            {
                Sql = string.Format("      INSERT INTO {0}.dbo.LPRTRNS (iLotArea, acPlate, dtTrnsDate, iInOutStatus, iCardType, acPicName "
                    + ", iRecognitionFlg, iSrvrupdtFlag, acRegNo, iInEqpm, iClient, acCarModel1, acCarModel2, iEqpm) VALUES ("
                    , frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
                Sql += string.Format("{0}, N'{1}', '{2}', {3}, {4}, N'{5}', {6}, 0, '{7}', {8}, {9}, N'{10}', N'{11}', 0)\r\n",
                    ParkInfo.No, Carno, ProcTime.ToString("yyyy-MM-dd HH:mm:ss"), 0, iCardType,
                    EntrancePic, Recognition == true ? 0 : 1, EmpNo, chno, ParkInfo.Client_No, CarModel1, CarModel2);
            }
            //Util.Logger.Log(Sql);
            return Sql;
        }

        public static string SetExitLprTrns(DateTime ProcTime, ClsStructure.Park_Info ParkInfo, bool Reged, String Carno, String ExitPic, bool Recognition)
        {
            String Sql = string.Empty;
            Sql = string.Format(" INSERT INTO {0}.dbo.LPRTRNS (iLotArea, acPlate, dtTrnsDate, iInOutStatus, iCardType, acPicName, iRecognitionFlg) "
                + " VALUES "
                + " ( ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            Sql += string.Format("'{0}', N'{1}', '{2}', '{3}', '{4}', N'{5}', '{6}')",
                ParkInfo.No, Carno, ProcTime.ToString("yyyy-MM-dd HH:mm:ss"), 1, Reged == true ? 1 : 0,
                ExitPic, Recognition == true ? 0 : 1);
            //Util.Logger.Log(Sql);
            return Sql;
        }

        public static string SetExitLprTrns(DateTime ProcTime, ClsStructure.Park_Info ParkInfo, bool Reged, String Carno, String ExitPic, bool Recognition, string ChNo)
        {
            String Sql = string.Empty;
            int chno = Util.Function.IntTryParse(ChNo.Replace("CH", ""));
            Sql = string.Format(" INSERT INTO {0}.dbo.LPRTRNS (iLotArea, acPlate, dtTrnsDate, iInOutStatus, iCardType, acPicName, iRecognitionFlg, iEqpm) "
                + " VALUES "
                + " ( ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            Sql += string.Format("'{0}', N'{1}', '{2}', '{3}', '{4}', N'{5}', '{6}', {7})",
                ParkInfo.No, Carno, ProcTime.ToString("yyyy-MM-dd HH:mm:ss"), 1, Reged == true ? 1 : 0,
                ExitPic, Recognition == true ? 0 : 1, chno);
            //Util.Logger.Log(Sql);
            return Sql;
        }

        //삼성 LPRTRNS 
        public static string SetExitLprTrns(DateTime ProcTime, ClsStructure.Park_Info ParkInfo, bool Reged, String Carno, String EntrancePic, bool Recognition, string ChNo
            , string EmpNo, string CarModel1, string CarModel2)
        {
            String Sql = string.Empty;
            int chno = Util.Function.IntTryParse(ChNo.Replace("CH", ""));
            //Sql = string.Format("use {0}\r\n", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            //Sql += "IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE  TABLE_NAME = 'LPRTRNS' AND COLUMN_NAME in ('acRegNo', 'iInEqpm', 'iClient', 'acCarModel1', 'acCarModel2'))\r\n";
            //Sql += "    Begin\r\n";
            //Sql += string.Format("      INSERT INTO {0}.dbo.LPRTRNS (iLotArea, acPlate, dtTrnsDate, iInOutStatus, iCardType, acPicName, iRecognitionFlg, iEqpm) "
            //    + " VALUES "
            //    + " ( ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            //Sql += string.Format("{0}, '{1}', '{2}', {3}, {4}, '{5}', {6}, {7})\r\n",
            //    ParkInfo.No, Carno, ProcTime.ToString("yyyy-MM-dd HH:mm:ss"), 1, Reged == true ? 1 : 0,
            //    EntrancePic, Recognition == true ? 0 : 1, chno);
            //Sql += "    End\r\n";
            //Sql += "Else\r\n";
            //Sql += "    Begin\r\n";
            //Sql += string.Format("      INSERT INTO {0}.dbo.LPRTRNS (iLotArea, acPlate, dtTrnsDate, iInOutStatus, iCardType, acPicName "
            //    + ", iRecognitionFlg, iSrvrupdtFlag, acRegNo, iInEqpm, iClient, acCarModel1, acCarModel2, iEqpm) VALUES ("
            //    , frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            //Sql += string.Format("{0}, '{1}', '{2}', {3}, {4}, '{5}', {6}, 0, '{7}', {8}, {9}, '{10}', '{11}', 0)\r\n",
            //    ParkInfo.No, Carno, ProcTime.ToString("yyyy-MM-dd HH:mm:ss"), 1, Reged == true ? 1 : 0,
            //    EntrancePic, Recognition == true ? 0 : 1, EmpNo, chno, ParkInfo.Client_No, CarModel1, CarModel2);
            //Sql += "    End\r\n";
            if (!frmLprMain.ExtendLprtrns)
            {
                Sql = string.Format("INSERT INTO {0}.dbo.LPRTRNS (iLotArea, acPlate, dtTrnsDate, iInOutStatus, iCardType, acPicName, iRecognitionFlg, iEqpm) "
                    + " VALUES "
                    + " ( ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
                Sql += string.Format("{0}, N'{1}', '{2}', {3}, {4}, N'{5}', {6}, {7})\r\n",
                    ParkInfo.No, Carno, ProcTime.ToString("yyyy-MM-dd HH:mm:ss"), 1, Reged == true ? 1 : 0,
                    EntrancePic, Recognition == true ? 0 : 1, chno);
            }
            else
            {
                Sql = string.Format("      INSERT INTO {0}.dbo.LPRTRNS (iLotArea, acPlate, dtTrnsDate, iInOutStatus, iCardType, acPicName "
                    + ", iRecognitionFlg, iSrvrupdtFlag, acRegNo, iInEqpm, iClient, acCarModel1, acCarModel2, iEqpm) VALUES ("
                    , frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
                Sql += string.Format("{0}, N'{1}', '{2}', {3}, {4}, N'{5}', {6}, 0, '{7}', {8}, {9}, N'{10}', N'{11}', 0)\r\n",
                    ParkInfo.No, Carno, ProcTime.ToString("yyyy-MM-dd HH:mm:ss"), 1, Reged == true ? 1 : 0,
                    EntrancePic, Recognition == true ? 0 : 1, EmpNo, chno, ParkInfo.Client_No, CarModel1, CarModel2);
            }
            //Util.Logger.Log(Sql);insert into Tckttrns
            return Sql;
        }

        public static string SetEntranceFcCountTrns(DateTime ProcTime, ClsStructure.Park_Info ParkInfo)
        {
            String Sql = string.Empty;
            Sql = string.Format("if Not Exists(select * from {0}.dbo.FC_COUNTTRNS where dtMgmntDate = '{1}' and iExtendLotArea = {2} and iLotArea = {2} and iClient = 0) ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, ProcTime.ToString("yyyy-MM-dd"), ParkInfo.No);
            Sql += string.Format("INSERT INTO {0}.dbo.FC_COUNTTRNS (iExtendLotArea, iLotArea, iClient, InCount, OutCount, dtMgmntDate) VALUES ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            //Sql += string.Format("({0}, {1}, {2}, {3}, {4}, '{5}')",
            Sql += string.Format("({0}, {1}, 0, {3}, {4}, '{5}')",
                ParkInfo.No, ParkInfo.No, ParkInfo.Client_No, 1, 0, ProcTime.ToString("yyyy-MM-dd"));
            Sql += "else ";
            Sql += string.Format(" UPDATE {0}.dbo.FC_COUNTTRNS SET ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            Sql += " InCount = InCount + 1";
            Sql += " , dtMgmntDate = '" + ProcTime.ToString("yyyy-MM-dd") + "' ";
            Sql += " WHERE ";
            //Sql += string.Format(" iExtendLotArea = {0} and iLotArea = {1} and iClient = {2} and dtMgmntDate = '{3}'",
            Sql += string.Format(" iExtendLotArea = {0} and iLotArea = {1} and dtMgmntDate = '{3}'",
            ParkInfo.No, ParkInfo.No, ParkInfo.Client_No, ProcTime.ToString("yyyy-MM-dd"));
            //Util.Logger.Log(Sql);
            return Sql;
        }

        public static string SetExitFcCountTrns(DateTime ProcTime, ClsStructure.Park_Info ParkInfo)
        {
            String Sql = string.Empty;
            Sql = string.Format("if Not Exists(select * from {0}.dbo.FC_COUNTTRNS where dtMgmntDate = '{1}' and iExtendLotArea = {2} and iLotArea = {2} and iClient = 0) ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, ProcTime.ToString("yyyy-MM-dd"), ParkInfo.No);
            Sql += string.Format("INSERT INTO {0}.dbo.FC_COUNTTRNS (iExtendLotArea, iLotArea, iClient, InCount, OutCount, dtMgmntDate) VALUES ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            //Sql += string.Format("({0}, {1}, {2}, {3}, {4}, '{5}')",
            Sql += string.Format("({0}, {1}, 0, {3}, {4}, '{5}')",
                ParkInfo.No, ParkInfo.No, ParkInfo.Client_No, 0, 1, ProcTime.ToString("yyyy-MM-dd"));
            Sql += "else ";
            Sql += string.Format(" UPDATE {0}.dbo.FC_COUNTTRNS SET ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            Sql += " OutCount = OutCount + 1";
            //Sql += string.Format("where iExtendLotArea = {0} and iLotArea = {1} and iClient = {2} and dtMgmntDate = '{3}'",
            Sql += string.Format("where iExtendLotArea = {0} and iLotArea = {1} and dtMgmntDate = '{3}'",
                ParkInfo.No, ParkInfo.No, ParkInfo.Client_No, ProcTime.ToString("yyyy-MM-dd"));
            //Util.Logger.Log(Sql);
            return Sql;
        }

        public static string SetEntranceFcStay(ClsStructure.Park_Info ParkInfo)
        {
            // iClient는 무시하고 iLotArea만 체크 — 클라이언트 단위로 카운트 분산 방지
            String Sql = string.Empty;
            Sql  = string.Format("if Not exists(select stay from {0}.dbo.fc_stay where iLotArea = {1}) ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, ParkInfo.No);
            Sql += string.Format(" INSERT INTO {0}.dbo.FC_STAY ( iExtendLotArea, iLotArea, iClient, Stay ) VALUES ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            Sql += string.Format("({0}, {1}, 0, 1) ", ParkInfo.No, ParkInfo.No);
            Sql += "else ";
            Sql += string.Format(" UPDATE {0}.dbo.FC_STAY SET Stay = Stay + 1 WHERE iLotArea = {1}",
                frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, ParkInfo.No);
            return Sql;
        }

        public static string SetExitFcStay(ClsStructure.Park_Info ParkInfo)
        {
            // iClient는 무시하고 iLotArea만 체크 — 클라이언트 단위로 카운트 분산 방지
            String Sql = string.Empty;
            Sql  = string.Format("if Not exists(select stay from {0}.dbo.fc_stay where iLotArea = {1}) ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, ParkInfo.No);
            Sql += string.Format(" INSERT INTO {0}.dbo.FC_STAY ( iExtendLotArea, iLotArea, iClient, Stay ) VALUES ", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb);
            Sql += string.Format("({0}, {1}, 0, 0) ", ParkInfo.No, ParkInfo.No);
            Sql += "else ";
            Sql += string.Format(" UPDATE {0}.dbo.FC_STAY SET Stay = Stay - 1 WHERE iLotArea = {1} and Stay > 0",
                frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, ParkInfo.No);
            return Sql;
        }

        //public static string SetExitPassTrnsUpdate(DateTime ProcTime, ClsStructure.Park_Info ParkInfo, ClsStructure.Lpr_Info LprInfo, DataRow Row, String Carno, String Number, String ExitPic, String Id)
        //{
        //    String Sql = string.Empty;
        //    Sql = " Update PASSTRNS Set";
        //    Sql += string.Format(" dtOutDate = '{0}', ", ProcTime.ToString("yyyy-MM-dd HH:mm:ss"));
        //    Sql += string.Format(" dtPayDate = '{0}', ", ProcTime.ToString("yyyy-MM-dd HH:mm:ss"));
        //    Sql += string.Format(" iOutClient = '{0}', ", ParkInfo.Client_No);
        //    Sql += string.Format(" iOutEqpm = '{0}', ", LprInfo.EqpmNo);
        //    Sql += string.Format(" iPayClient = '{0}', ", ParkInfo.Client_No);
        //    Sql += string.Format(" iPayEqpm = '{0}', ", LprInfo.EqpmNo);
        //    Sql += string.Format(" iCardType = '{0}', ", Row["iPsscrdType"].ToString());
        //    Sql += string.Format(" iInOutStatus = {0}, ", 1);
        //    Sql += string.Format(" dPasscardNo = '{0}', ", Row["iUser"].ToString());
        //    Sql += string.Format(" acOutTime = '{0}', ", ProcTime.ToString("HH:mm"));
        //    Sql += string.Format(" acPayTime = '{0}', ", ProcTime.ToString("HH:mm"));
        //    Sql += string.Format(" acUserName = '{0}', ", Row["acUserName"].ToString());
        //    Sql += string.Format(" acPlate1 = '{0}', ", Row["acPlate1"].ToString());
        //    Sql += string.Format(" acPlate2 = '{0}', ", Row["acPlate2"].ToString());
        //    Sql += string.Format(" iGroup = '{0}', ", Row["iGroup"].ToString());
        //    Sql += string.Format(" dtMgmntDate = '{0}', ", ProcTime.ToString("yyyy-MM-dd"));
        //    Sql += string.Format(" acGoOutPicName = '{0}', ", ExitPic);
        //    Sql += string.Format(" DongCode = '{0}', ", Row["DongCode"].ToString());
        //    Sql += string.Format(" HoCode = '{0}' ", Row["HoCode"].ToString());
        //    Sql += string.Format(" where iid = {0}", Id);
        //    Util.Logger.Log(Sql);
        //    return Sql;
        //}

        //public static string SetExitTcktTrnsUpdate(DateTime ProcTime, ClsStructure.Park_Info ParkInfo, ClsStructure.Lpr_Info LprInfo, String Carno, String ExitPic, String Id, String StayHour)
        //{
        //    String Sql = string.Empty;
        //    Sql = " UPDATE TCKTTRNS SET ";
        //    Sql += string.Format("dtOutDate = '{0}', ", ProcTime.ToString("yyyy-MM-dd HH:mm:ss"));
        //    Sql += string.Format("iOutClient = '{0}', ", ParkInfo.Client_No);
        //    Sql += string.Format("iOutEqpm = '{0}', ", LprInfo.EqpmNo);
        //    Sql += string.Format("acCarStayHours = '{0}', ", StayHour);
        //    Sql += string.Format("acOutTime = '{0}', ", ProcTime.ToString("HH:mm"));
        //    Sql += string.Format("acUserName = '{0}', ", Carno);
        //    Sql += string.Format("acPlate1 = '{0}', ", Carno);
        //    Sql += string.Format("acPlate2 = '{0}', ", Carno);
        //    Sql += string.Format("dtMgmntDate = '{0}', ", ProcTime.ToString("yyyy-MM-dd HH:mm:ss"));
        //    Sql += string.Format("acGoOutPicName = '{0}' ", ExitPic);
        //    Sql += string.Format("where iid = {0}", Id);
        //    Util.Logger.Log(Sql);
        //    return Sql;
        //}
    }
}
