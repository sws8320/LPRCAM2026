using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KyungsinLPR
{
    public static class GetMasterInfo
    {
        public static string SharePath;
        public static int Term;
        public static bool Use;
        private static Thread jobThread;
        private static string LastFile;
        
        public static void Init()
        {
            if (Use)
            {
                jobThread = new Thread(new ThreadStart(JOB));
                jobThread.IsBackground = true;
                jobThread.Start();
            }
        }

        private static void JOB()
        {
            if (!Directory.Exists("Master"))
                Directory.CreateDirectory("Master");
            while(true)
            {
                string[] files = Directory.GetFiles(SharePath, "CUSTDEF*.mst");
                DataTable dt = new DataTable();
                double max = 0;
                string LastFile = "";
                try
                {
                    if (files.Length > 0)
                    {
                        if (files.Length == 1)
                            LastFile = files[0];
                        else
                        {
                            foreach (string file in files)
                            {
                                if (max < double.Parse(Path.GetFileName(file).Replace("CUSTDEF", "").Replace(".mst", "")))
                                {
                                    max = double.Parse(Path.GetFileName(file).Replace("CUSTDEF", "").Replace(".mst", ""));
                                    LastFile = file;
                                }
                            }
                        }
                        File.Copy(LastFile, @"Master\" + Path.GetFileName(LastFile), true);
                        LastFile = @"Master\" + Path.GetFileName(LastFile);
                        files = Directory.GetFiles(@"Master", "CUSTDEF*.mst");
                        foreach (string file in files)
                        {
                            if (Path.GetFileName(LastFile) != Path.GetFileName(file))
                                File.Delete(file);
                        }
                    }
                }
                catch
                {
                    LastFile = "";
                }
                if (LastFile == "")
                {
                    files = Directory.GetFiles(@"Master", "CUSTDEF*.mst");
                    if (files.Length == 1)
                        LastFile = files[0];
                    else
                    {
                        max = 0;
                        foreach (string file in files)
                        {
                            if (max < double.Parse(Path.GetFileName(file).Replace("CUSTDEF", "").Replace(".mst", "")))
                            {
                                max = double.Parse(Path.GetFileName(file).Replace("CUSTDEF", "").Replace(".mst", ""));
                                LastFile = file;
                            }
                        }
                        foreach (string file in files)
                        {
                            if (Path.GetFileName(LastFile) != Path.GetFileName(file))
                                File.Delete(file);
                        }
                    }
                }
                dt = GetMaster(LastFile);
                if (dt != null && dt.Rows.Count > 0)
                {
                    frmLprMain.Main.DataProcess.CustDef = dt;
                    Util.Logger.Log(string.Format("정기권 정보 취득 {0}건 {1}", dt.Rows.Count, LastFile));
                }
                Thread.Sleep(Term * 60 * 1000);
            }
        }

        public static DataTable GetMaster(string FilePath)
        {
            DataTable dt = new DataTable();
            string mst = File.ReadAllText(FilePath, Encoding.UTF8);
            string[] stringSeparators = new string[] { "\r\n" };
            string[] lines = mst.Split(stringSeparators, StringSplitOptions.None);
            if (lines.Length > 1)
            {
                //데이터 테이블 컬럼 설정
                string[] line = lines[0].Split('\t');
                foreach (string item in line)
                {
                    if (item != "")
                    {
                        string[] col = item.Split('/');
                        switch (col[1])
                        {
                            case "System.Int32":
                                dt.Columns.Add(new DataColumn(col[0], typeof(System.Int32)));
                                break;
                            case "System.Int16":
                                dt.Columns.Add(new DataColumn(col[0], typeof(System.Int16)));
                                break;
                            case "System.Double":
                                dt.Columns.Add(new DataColumn(col[0], typeof(System.Double)));
                                break;
                            case "System.String":
                                dt.Columns.Add(new DataColumn(col[0], typeof(System.String)));
                                break;
                            case "System.DateTime":
                                dt.Columns.Add(new DataColumn(col[0], typeof(System.DateTime)));
                                break;
                        }
                    }
                }

                for (int i = 1; i < lines.Length; i++)
                {
                    if (lines[i] != "")
                    {
                        line = lines[i].Split('\t');
                        DataRow row = dt.NewRow();
                        for (int j = 0; j < dt.Columns.Count; j++)
                        {
                            if (line[j] != "null")
                                row[j] = line[j];
                            else
                                row[j] = DBNull.Value;
                        }
                        dt.Rows.Add(row);
                    }
                }
            }
            return dt;
        }
    }
}
