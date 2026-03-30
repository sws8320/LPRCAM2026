using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace KyungsinLPR
{
    public static class BlackList
    {
        public static bool Use = false;
        public static DataTable dtBlackList = new DataTable();

        public static string Normal1Text = "";
        public static string Normal2Text = "";
        public static string Normal1Color = "";
        public static string Normal2Color = "";
        public static string Period1Text = "";
        public static string Period2Text = "";
        public static string Period1Color = "";
        public static string Period2Color = "";
        public static string Bad1Text = "";
        public static string Bad2Text = "";
        public static string Bad1Color = "";
        public static string Bad2Color = "";
        public static bool UseOutDisPlay = false;
        public static bool DoNotOpenOutGate = false;
        public static string StartTime = "";
        public static string EndTime = "";
        
        public static void ReadEnv()
        {
            Use = Util.Function.BoolTryParse(Util.Function.IniReadValue("BLACKLIST", "USE"));
            Normal1Text = Util.Function.IniReadValue("BLACKLIST", "Normal1");
            Normal2Text = Util.Function.IniReadValue("BLACKLIST", "Normal2");
            Normal1Color = Util.Function.IniReadValue("BLACKLIST", "Normal1Color");
            Normal2Color = Util.Function.IniReadValue("BLACKLIST", "Normal2Color");
            Period1Text = Util.Function.IniReadValue("BLACKLIST", "Period1");
            Period2Text = Util.Function.IniReadValue("BLACKLIST", "Period2");
            Period1Color = Util.Function.IniReadValue("BLACKLIST", "Period1Color");
            Period2Color = Util.Function.IniReadValue("BLACKLIST", "Period2Color");
            Bad1Text = Util.Function.IniReadValue("BLACKLIST", "Bad1");
            Bad2Text = Util.Function.IniReadValue("BLACKLIST", "Bad2");
            Bad1Color = Util.Function.IniReadValue("BLACKLIST", "Bad1Color");
            Bad2Color = Util.Function.IniReadValue("BLACKLIST", "Bad2Color");
            UseOutDisPlay = Util.Function.BoolTryParse(Util.Function.IniReadValue("BLACKLIST", "UseOutDisPlay"));
            DoNotOpenOutGate = Util.Function.BoolTryParse(Util.Function.IniReadValue("BLACKLIST", "DoNotOpenOutGate"));
            StartTime = Util.Function.IniReadValue("BLACKLIST", "StartTime");
            EndTime = Util.Function.IniReadValue("BLACKLIST", "EndTime");
            if (StartTime == "") StartTime = "00:00";
            if (EndTime == "") EndTime = "23:59";
        }

        public static void SaveEnv(bool use, string Ntxt1, string Ntxt2, string Ncolor1, string Ncolor2
            , string Ptxt1, string Ptxt2, string Pcolor1, string Pcolor2
            , string Btxt1, string Btxt2, string Bcolor1, string Bcolor2, bool DisPlay, bool Gate, string Start, string End)
        {
            Use = use;
            Normal1Text = Ntxt1;            Normal2Text = Ntxt2;            Normal1Color = Ncolor1;            Normal2Color = Ncolor2;
            Period1Text = Ptxt1;            Period2Text = Ptxt2;            Period1Color = Pcolor1;            Period2Color = Pcolor2;
            Bad1Text = Btxt1;               Bad2Text = Btxt2;               Bad1Color = Bcolor1;               Bad2Color = Bcolor2;
            UseOutDisPlay = DisPlay;        DoNotOpenOutGate = Gate;        StartTime = Start;                 EndTime = End;
            Util.Function.IniWriteValue("BLACKLIST", "USE", use.ToString());
            Util.Function.IniWriteValue("BLACKLIST", "Normal1", Normal1Text);
            Util.Function.IniWriteValue("BLACKLIST", "Normal2", Normal2Text);
            Util.Function.IniWriteValue("BLACKLIST", "Normal1Color", Normal1Color);
            Util.Function.IniWriteValue("BLACKLIST", "Normal2Color", Normal2Color);
            Util.Function.IniWriteValue("BLACKLIST", "Period1", Period1Text);
            Util.Function.IniWriteValue("BLACKLIST", "Period2", Period2Text);
            Util.Function.IniWriteValue("BLACKLIST", "Period1Color", Period1Color);
            Util.Function.IniWriteValue("BLACKLIST", "Period2Color", Period2Color);
            Util.Function.IniWriteValue("BLACKLIST", "Bad1", Bad1Text);
            Util.Function.IniWriteValue("BLACKLIST", "Bad2", Bad2Text);
            Util.Function.IniWriteValue("BLACKLIST", "Bad1Color", Bad1Color);
            Util.Function.IniWriteValue("BLACKLIST", "Bad2Color", Bad2Color);
            Util.Function.IniWriteValue("BLACKLIST", "UseOutDisPlay", UseOutDisPlay.ToString());
            Util.Function.IniWriteValue("BLACKLIST", "DoNotOpenOutGate", DoNotOpenOutGate.ToString());
            Util.Function.IniWriteValue("BLACKLIST", "StartTime", StartTime);
            Util.Function.IniWriteValue("BLACKLIST", "EndTime", EndTime);
        }

        public static Black DisplayMent(string CarNo, bool Period = false)
        {
            Black rtn = new Black();
            try
            {
                if (Use)
                {
                    DateTime Start = Util.Function.DateTimeTryParse(string.Format("{0} {1}:00", DateTime.Now.ToString("yyyy-MM-dd"), StartTime));
                    DateTime End = Util.Function.DateTimeTryParse(string.Format("{0} {1}:59", DateTime.Now.ToString("yyyy-MM-dd"), EndTime));
                    //if (Start > End) End.AddDays(1);
                    //if (!(Start >= DateTime.Now && End >= DateTime.Now)) return rtn;
                    if (Start > End)
                    {
                        if (DateTime.Now <= End)
                            Start = Start.AddDays(-1);
                        else if (DateTime.Now >= Start)
                            End = End.AddDays(1);
                        else if (End > DateTime.Now && Start < DateTime.Now)
                            return rtn;
                    }
                    else
                    {
                        if (DateTime.Now < Start || DateTime.Now > End)
                            return rtn;
                    }
                    if (dtBlackList.Rows.Count > 0)
                    {
                        DataRow[] row = dtBlackList.Select(string.Format("acPlateno = '{0}'", CarNo));
                        if (row.Length > 0)
                        {
                            try
                            {
                                Util.Logger.Log(string.Format("차량번호 {0} 사용 {1} 자동 해지 {2} 수동 등록 {3} 시작일 {4} 종료일 {5} 최초 등록 {6} 최종 수정 {7}",
                                    row[0]["acPlateNo"].ToString(), row[0]["bUse"].ToString(), row[0]["blAuto"].ToString(), row[0]["blManual"].ToString(),
                                    row[0]["dtStartDate"].ToString(), row[0]["dtEndDate"].ToString(), row[0]["dtFirstRegDate"].ToString(), row[0]["dtLastUpdateDate"].ToString()));
                                if (row[0]["bUse"].ToString() == "True")
                                {
                                    if (row[0]["blAuto"].ToString() == "False")
                                    {
                                        rtn.Apply = true;
                                        rtn.Ment1 = Bad1Text;
                                        if (Bad1Text.IndexOf('N') >= 0)
                                        {
                                            rtn.Ment1 = ChangeDay(Bad1Text, row[0]["dtEndDate"].ToString());
                                        }
                                        rtn.Ment2 = Bad2Text;
                                        if (Bad2Text.IndexOf('N') >= 0)
                                        {
                                            rtn.Ment2 = ChangeDay(Bad2Text, row[0]["dtEndDate"].ToString());
                                        }
                                        rtn.Color1 = Bad1Color;
                                        rtn.Color2 = Bad2Color;
                                    }
                                    else if (Util.Function.DateTimeTryParse(row[0]["dtStartDate"].ToString()) <= DateTime.Now && Util.Function.DateTimeTryParse(row[0]["dtEndDate"].ToString()) >= DateTime.Now)
                                    {
                                        rtn.Apply = true;
                                        rtn.Ment1 = Period ? Period1Text : Normal1Text;
                                        if (rtn.Ment1.IndexOf('N') >= 0)
                                        {
                                            rtn.Ment1 = ChangeDay(rtn.Ment1, row[0]["dtEndDate"].ToString());
                                        }
                                        rtn.Ment2 = Period ? Period2Text : Normal2Text;
                                        if (rtn.Ment2.IndexOf('N') >= 0)
                                        {
                                            rtn.Ment2 = ChangeDay(rtn.Ment2, row[0]["dtEndDate"].ToString());
                                        }
                                        rtn.Color1 = Period ? Period1Color : Normal1Color;
                                        rtn.Color2 = Period ? Period2Color : Normal2Color;
                                    }
                                }
                                if (rtn.Ment2 == string.Empty)
                                    rtn.Ment2 = CarNo;
                                Util.Logger.Log(string.Format("전광판 출력 문구 {0} {1}", rtn.Ment1, rtn.Ment2));
                            }
                            catch (Exception e)
                            {
                                Util.Logger.Log(string.Format("BlackList Checker Error {0}", e.Message));
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
            return rtn;
        }

        private static string ChangeDay(string Ment, string EndDay)
        {
            TimeSpan diff = Util.Function.DateTimeTryParse(EndDay) - DateTime.Now;
            string msg = "";
            if (diff.TotalDays < 1)
            {
                if (Ment.IndexOf("N일 ") > -1)
                    Ment = Ment.Replace("N일 ", "오늘까지");
                else
                    Ment = Ment.Replace("N일", "오늘까지");
            }
            else
                Ment = Ment.Replace("N", ((int)diff.TotalDays).ToString());
            return Ment;
        }

        public static void GetBlackList()
        {
            try
            {
                if (Use)
                {
                    DataTable dt = new DataTable();
                    string sql = "select acPlateNo, bUse, blAuto, blManual, convert(nvarchar(19), dtStartDate, 121) dtStartDate, ";
                    sql += "convert(nvarchar(19), dtStartDate, 121) dtStartDate, convert(nvarchar(19), dtEndDate, 121) dtEndDate, ";
                    sql += "convert(nvarchar(19), dtFirstRegDate, 121) dtFirstRegDate, convert(nvarchar(19), dtLastUpdateDate, 121) dtLastUpdateDate ";
                    sql += string.Format("from {0}.dbo.blacklistmst where getdate() between dtstartdate and dtenddate and buse = 1 or blauto = 0", frmLprMain.ENV.CommonEnv.DBInfo.MstDB);
                    dt = Util.clsMssql.GetTable(frmLprMain.Main.DataProcess.Get_MCon(), sql);
                    if (dt != new DataTable())
                    {
                        dtBlackList = dt;
                    }
                }
            }
            catch { }
        }
    }

    public class Black
    {
        public bool Apply = false;
        public string Ment1 = "";
        public string Ment2 = "";
        public string Color1 = "";
        public string Color2 = "";
    }
}
