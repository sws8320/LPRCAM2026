using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace KyungsinLPR
{
    public static class clsOutService
    {
        public static bool Use = false;
        public static int Service = 0;

        public static void Save(bool use, string service)
        {
            Use = use;
            Service = Util.Function.IntTryParse(service);
            Util.Function.IniWriteValue("OUTSERVICE", "USE", Use.ToString());
            Util.Function.IniWriteValue("OUTSERVICE", "SERVICE", Service.ToString());
        }

        public static void Load()
        {
            Use = Util.Function.BoolTryParse(Util.Function.IniReadValue("OUTSERVICE", "USE"));
            Service = Util.Function.IntTryParse(Util.Function.IniReadValue("OUTSERVICE", "SERVICE"));
        }

        public static bool Check(string CarNo)
        {
            bool rtn = false;
            
            string Query = string.Format("select max(dtoutdate) dtoutdate from {0}.dbo.tckttrns where acplate1 = '{1}'", frmLprMain.ENV.CommonEnv.DBInfo.TrnsDb, CarNo);
            DataTable dt = Util.clsMssql.GetTable(frmLprMain.Main.DataProcess.Get_MCon(), Query);
            if (dt.Rows.Count > 0)
            {
                string OutTime = dt.Rows[0][0].ToString();
                if (OutTime != "")
                {
                    DateTime outtime = Util.Function.DateTimeTryParse(OutTime);
                    if ((DateTime.Now - outtime).TotalMinutes <= Service)
                        rtn = true;
                }
            }
            return rtn;
        }
    }
}
