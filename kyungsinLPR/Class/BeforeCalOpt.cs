using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace KyungsinLPR
{
    public static class BeforeCalOpt
    {
        public static bool Use;
        public static int LagTime;
        public enum LagReturn { NotEntrance, Lag, Cal };

        public static void Save()
        {
            Util.Function.IniWriteValue("BEFORECAL", "USE", Use.ToString());
            Util.Function.IniWriteValue("BEFORECAL", "LAG", LagTime.ToString());
        }

        public static void Load()
        {
            Use = Util.Function.BoolTryParse(Util.Function.IniReadValue("BEFORECAL", "USE"));
            LagTime = Util.Function.IntTryParse(Util.Function.IniReadValue("BEFORECAL", "LAG"));
        }

        public static LagReturn LagCarCheck(string CarNo, string Tdb, SqlConnection Con)
        {
            string Sql = string.Format("select * from {0}.dbo.tckttrns where acPlate1 = '{1}' order by iid desc", Tdb, CarNo);
            DataTable dt = Util.clsMssql.GetTable(Con, Sql);
            if (dt.Rows.Count == 0 || dt.Rows[0]["dtoutdate"].ToString() != string.Empty)
                return LagReturn.NotEntrance;
            else if (dt.Rows[0]["dtpaydate"].ToString() == string.Empty)
                return LagReturn.Cal;
            else
            {
                DateTime paytime = Util.Function.DateTimeTryParse(dt.Rows[0]["dtpaydate"].ToString());
                if ((DateTime.Now - paytime).TotalMinutes < LagTime + 1)
                    return LagReturn.Lag;
                else
                    return LagReturn.Cal;
            }
        }
    }
}
