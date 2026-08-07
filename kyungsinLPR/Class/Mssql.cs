using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;

namespace KukjeLPR
{
    class Mssql
    {
        private String MConString;
        private String TConString;
        private SqlConnection MConn = null;
        private SqlConnection TConn = null;

        public Mssql(ClsStructure.DB_Info DBInfo)
        {
            MConString = string.Format("data source={0}; database={1}; user id={2}; password={3}; Connection Timeout=3", DBInfo.Ip, DBInfo.MstDB, DBInfo.Id, DBInfo.Pw);
            TConString = string.Format("data source={0}; database={1}; user id={2}; password={3}; Connection Timeout=3", DBInfo.Ip, DBInfo.TrnsDb, DBInfo.Id, DBInfo.Pw);
        }

        public SqlConnection OpenMDB()
        {
            try
            {
                MConn = new SqlConnection(MConString);
                MConn.Open();
                return MConn;
            }
            catch (Exception e)
            {
                Util.Logger.Log(string.Format("MST DB OPEN ERROR {0}", e.Message));
                return null;
            }
        }

        public SqlConnection OpenTDB()
        {
            try
            {
                TConn = new SqlConnection(TConString);
                TConn.Open();
                return TConn;
            }
            catch (Exception e)
            {
                Util.Logger.Log(string.Format("TRNS DB OPEN ERROR {0}", e.Message));
                return null;
            }
        }

        public DataTable GetTable(SqlConnection Conn, String Sql)
        {
            DataTable dt = new DataTable();

            try
            {
                SqlCommand cmd = new SqlCommand(Sql, Conn);
                dt.Load(cmd.ExecuteReader());
            }
            catch (SqlException _e)
            {
                Util.Logger.Log(string.Format("GetTable {0} {1}", _e.Message, Sql));
            }
            return dt;
        }

        public bool ExecQuery(SqlConnection Conn, String Sql)
        {
            try
            {
                SqlCommand Cmd = new SqlCommand(Sql, Conn);
                Cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException _e)
            {
                Util.Logger.Log(string.Format("ExecQuery {0} {1}", _e.Message, Sql));
                return false;
            }
        }
    }
}
