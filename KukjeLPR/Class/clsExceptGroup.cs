using System;
using System.Collections.Generic;
using System.Data;

namespace KyungsinLPR
{
    public static class clsExceptGroup
    {
        private static int Except_GrpNo=-9;
        public static int ExceptGrpNo
        {
            get
            {
                if (Except_GrpNo == -9)
                    Except_GrpNo = Get_Except_Group();
                return Except_GrpNo;
            }
            set { Except_GrpNo = value; }
        }

        public static List<string> GetGroupList()
        {
            List<string> lst = new List<string>();
            try
            {
                DataTable dtGrp = Util.clsMssql.GetTable(frmLprMain.Main.DataProcess.Get_MCon(), string.Format("select * from {0}.dbo.groupdef where iLotarea = {1}", frmLprMain.ENV.CommonEnv.DBInfo.MstDB, frmLprMain.ENV.CommunicationEnv.ParkInfo.No));

                foreach (DataRow item in dtGrp.Rows)
                {
                    lst.Add(string.Format("[{0}] {1}", item["iGroup"].ToString(), item["acGroupName1"].ToString()));
                }
            }
            catch (Exception) { }
            return lst;
        }

        public static int Get_Except_Group()
        {
            int rtn = -1;
            string read = Util.Function.IniReadValue("PASSCAR", "EXCEPT");
            if (read != string.Empty)
            {
                rtn = Util.Function.IntTryParse(read);
            }
            return rtn;
        }

        public static void Set_Except_Group(string GrpNo)
        {
            try
            {
                Util.Function.IniWriteValue("PASSCAR", "EXCEPT", GrpNo);
            }
            catch (Exception) { }
        }
    }
}
