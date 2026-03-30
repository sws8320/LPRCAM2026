using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace KyungsinLPR
{
    public static class FullSpaceControl
    {
        public static bool Use { get; set; }
        public static int FullSet { get; set; }
        public static int FullOff { get; set; }
        public static bool isFull { get; set; }
        public static bool ForceFull;
        public static byte[] FullMent1 = new byte[] { 0x10, 0x02, 0x00, 0x00, 0x1B, 0x94, 0x00, 0x00, 0x63, 0x01, 0x00, 0x07, 0x31, 0x00, 0x00, 0x14, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x01, 0x00, 0x20, 0xB8, 0xB8, 0xC2, 0xF7, 0x10, 0x03 };
        public static byte[] FullMent2 = new byte[] { 0x10, 0x02, 0x00, 0x00, 0x11, 0x94, 0x00, 0x01, 0x00, 0x00, 0x00, 0x03, 0x01, 0x00, 0x00, 0x14, 0x02, 0x00, 0x04, 0x00, 0x00, 0x00, 0x10, 0x03 };

        /// <summary>
        /// 수동 만차 제어 1개소 대응 20190416
        /// 차 후 n개소 UDP 통신 기능 추가 해야 됨
        /// </summary>
        public static bool Manual { get; set; }
        /// <summary>
        /// 정기권 처리 여부 True 입차 안함 False 입차함
        /// </summary>
        public static bool Period { get; set; }
        /// <summary>
        /// 만차 해제 시 입구 차단기 개방
        /// </summary>
        public static bool EntGateOpen { get; set; }

        public static void LoadFullSpace(ClsStructure.DB_Info _dbinfo)
        {
            try
            {
                Manual = Util.Function.BoolTryParse(Util.Function.IniReadValue("FullControl", "Manual"));
                Period = Util.Function.BoolTryParse(Util.Function.IniReadValue("FullControl", "Period"));
                EntGateOpen = Util.Function.BoolTryParse(Util.Function.IniReadValue("FullControl", "EntGateOpen"));
                Use = false;
                FullSet = 0;
                FullOff = 0;
                isFull = false;
                string sql = string.Format("select * from {0}.dbo.areadef where iLotarea = {1}", frmLprMain.ENV.CommonEnv.DBInfo.MstDB, frmLprMain.ENV.CommunicationEnv.ParkInfo.No);
                Util.clsMssql.Dbinfo.Server = _dbinfo.Ip;
                Util.clsMssql.Dbinfo.id = _dbinfo.Id;
                Util.clsMssql.Dbinfo.pw = _dbinfo.Pw;
                Util.clsMssql.Dbinfo.db = _dbinfo.MstDB;
                SqlConnection Conn = Util.clsMssql.OpenDB();
                DataTable dt = Util.clsMssql.GetTable(Conn, sql);
                foreach (DataRow row in dt.Rows)
                {
                    int tmp = 0;
                    Use = "1" == row["iFullSpaceFlg"].ToString();
                    int.TryParse(row["iFullSpace"].ToString(), out tmp);
                    FullSet = tmp;
                    int.TryParse(row["iFullSpaceOff"].ToString(), out tmp);
                    FullOff = tmp;
                }
            }
            catch (Exception e)
            {
                Util.Logger.Log("Full Error : " + e.Message);
            }
        }

        public static bool FullCheck(ClsStructure.DB_Info _dbinfo, int extno, int client)
        {
            bool rtn = false;
            try
            {
                if (Use)
                {
                    Util.clsMssql.Dbinfo.Server = _dbinfo.Ip;
                    Util.clsMssql.Dbinfo.id = _dbinfo.Id;
                    Util.clsMssql.Dbinfo.pw = _dbinfo.Pw;
                    Util.clsMssql.Dbinfo.db = _dbinfo.TrnsDb;
                    SqlConnection Conn = Util.clsMssql.OpenDB();
                    //string sql = string.Format("select stay from {0}.dbo.fc_stay where iExtendLotarea = {1} and iLotArea = {1} and iClient = {2}", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, extno, client);
                    string sql = string.Format("select stay from {0}.dbo.fc_stay where iExtendLotarea = {1} and iLotArea = {1} and iClient = 0", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, extno, client);
                    DataTable dt = Util.clsMssql.GetTable(Conn, sql);
                    int stay = Util.Function.IntTryParse(dt.Rows[0][0].ToString());
                    if (FullSet <= stay)
                    {
                        rtn = true;
                        isFull = true;
                    }
                    else if (FullOff > stay && isFull)
                        rtn = false;
                    else
                    {
                        rtn = false;
                        if (isFull && EntGateOpen && !ForceFull)
                        {
                            if (frmLprMain.ENV.CommunicationEnv.Lpr1Info.InOutType == (int)ClsStructure.InoutType.입구용 &&
                                (frmLprMain.ENV.CommunicationEnv.Lpr1Info.LprOpt.Normal_Gate || frmLprMain.ENV.CommunicationEnv.Lpr1Info.LprOpt.Period_Gate))
                            {
                                Util.Logger.Log("만차 해제시 차단기 개방");
                                frmLprMain.Main.SerialDev.GateOpen(0);
                            }
                        }
                        isFull = false;
                    }
                }
            }
            catch (Exception)
            { }
            return rtn;
        }
    }
}
