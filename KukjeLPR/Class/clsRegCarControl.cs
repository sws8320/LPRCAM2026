using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Util;

namespace KyungsinLPR
{
    //public static class clsRegCarControl
    //{
    //    public static void Load()
    //    {
    //        Util.Function.IniFileName = string.Format("{0}\\CameraSetting.ini", Util.Global.ROOT);

    //        //정기권 이중 입차 제한
    //        frmLprMain.ENV.RegControl.controlent = new ClsStructure.Control_DupEnt();
    //        frmLprMain.ENV.RegControl.controlent.Use = Util.Function.BoolTryParse(Util.Function.IniReadValue("REGCARCONTROL", "ENTLIMITUSE"));
    //        frmLprMain.ENV.RegControl.controlent.Ment = Util.Function.IniReadValue("REGCARCONTROL", "ENTLIMITMENT");
    //    }

    //    public static void Save(ClsStructure.RegCarControl regcontrol)
    //    {
    //        Util.Function.IniFileName = string.Format("{0}\\CameraSetting.ini", Util.Global.ROOT);

    //        //정기권 이중 입차 제한
    //        Util.Function.IniWriteValue("REGCARCONTROL", "ENTLIMITUSE", regcontrol.controlent.Use);
    //        Util.Function.IniWriteValue("REGCARCONTROL", "ENTLIMITMENT", regcontrol.controlent.Ment);
    //    }
    //}

    public class RegCarControl
    {
        public bool Entcontroluse;
        public string Entcontrolment;
        public bool OtherparkUse;
        public List<park> Otherparks;
        public bool OtherparksTimeuse;
        public string Otherparksstart;
        public string Otherparksend;
        public bool Regautodeluse;
        public string Regautodeltime;
        public bool Regendnotiuse;
        public string Regendnotiday;
        public bool Penaltiuse;
        public string Penaltiment;
        public bool Ilotarea;
        public bool UseGroupGate = false;
        public bool UseExitGroupGate = false;  // 출차장비도 그룹 제한 적용 여부 (기본 false → 입차장비만 제한)
        public int GateGroupNo = 0;
        public string[] GateGroupName;
        public string[] GroupMent;
        public bool[] GroupUse;
        public bool GroupUseTime;
        public string GroupStart;
        public string GroupEnd;
        public int iControlType = 0;

        public RegCarControl Load()
        {
            RegCarControl info = new RegCarControl();
            info.Otherparks = new List<park>();
            try
            {
                Util.Function.IniFileName = string.Format("{0}\\CameraSetting.ini", Util.Global.ROOT);
                info.Entcontroluse = Util.Function.BoolTryParse(Util.Function.IniReadValue("REGCARCONTROL", "ENTLIMITUSE"));
                info.Entcontrolment = Util.Function.IniReadValue("REGCARCONTROL", "ENTLIMITMENT");
                info.OtherparkUse = Util.Function.BoolTryParse(Util.Function.IniReadValue("REGCARCONTROL", "OtherParkUse"));
                string line = Util.Function.IniReadValue("REGCARCONTROL", "OtherParkInfo");
                string[] sp = line.Split('\t');
                //not exists check
                if (frmLprMain.Main.DataProcess != null)
                {
                    DataTable dt = Util.clsMssql.GetTable(frmLprMain.Main.DataProcess.Get_MCon(), "select distinct(iExtendLotArea) from AREADEF");
                    foreach (string item in sp)
                    {
                        if (item != "")
                        {
                            string[] sp1 = item.Split('\\');
                            if (sp1.Length == 3)
                            {
                                park p = new park();
                                p.Use = Util.Function.BoolTryParse(sp1[0]);
                                p.parkno = Util.Function.IntTryParse(sp1[1]);
                                p.ment = sp1[2];
                                if (dt.Select(string.Format("iExtendLotArea = {0}", p.parkno)).Length > 0)
                                    info.Otherparks.Add(p);
                            }
                        }
                    }

                    if (info.Otherparks.Count == 0)
                    {
                        foreach (DataRow item in dt.Rows)
                        {
                            park p = new park();
                            p.Use = false;
                            p.parkno = Util.Function.IntTryParse(item["iExtendLotArea"].ToString());
                            p.ment = "";
                            info.Otherparks.Add(p);
                        }
                    }
                    else
                    {
                        foreach (DataRow item in dt.Rows)
                        {
                            int idx = info.Otherparks.FindIndex(x => x.parkno.ToString() == item["iExtendLotArea"].ToString());
                            if (idx < 0)
                            {
                                park p = new park();
                                p.Use = false;
                                p.parkno = Util.Function.IntTryParse(item["iExtendLotArea"].ToString());
                                p.ment = "";
                                info.Otherparks.Add(p);
                            }
                        }
                    }
                }
                info.OtherparksTimeuse = Util.Function.BoolTryParse(Util.Function.IniReadValue("REGCARCONTROL", "OtherParktimeUse"));
                info.Otherparksstart = Util.Function.IniReadValue("REGCARCONTROL", "OtherParktimestart");
                info.Otherparksend = Util.Function.IniReadValue("REGCARCONTROL", "OtherParktimeend");

                info.Regautodeluse = Util.Function.BoolTryParse(Util.Function.IniReadValue("REGCARCONTROL", "RegAutoDelUse"));
                info.Regautodeltime = Util.Function.IniReadValue("REGCARCONTROL", "RegAutoDelTime");
                info.Regendnotiuse = Util.Function.BoolTryParse(Util.Function.IniReadValue("REGCARCONTROL", "RegEndNotiUse"));
                info.Regendnotiday = Util.Function.IniReadValue("REGCARCONTROL", "RegEndNotiDay");
                info.Penaltiuse = Util.Function.BoolTryParse(Util.Function.IniReadValue("REGCARCONTROL", "PenaltiUse"));
                info.Penaltiment = Util.Function.IniReadValue("REGCARCONTROL", "PenaltiMent");
                info.Ilotarea = Util.Function.BoolTryParse(Util.Function.IniReadValue("REGCARCONTROL", "LotArea"));

                info.UseGroupGate = Util.Function.BoolTryParse(Util.Function.IniReadValue("REGCARCONTROL", "GroupUSE"));
                info.UseExitGroupGate = Util.Function.BoolTryParse(Util.Function.IniReadValue("REGCARCONTROL", "ExitGroupUSE"));
                info.GateGroupNo = Util.Function.IntTryParse(Util.Function.IniReadValue("REGCARCONTROL", "GroupNo"));

                info.GroupUseTime = Util.Function.BoolTryParse(Util.Function.IniReadValue("REGCARCONTROL", "GroupUseTime"));
                info.GroupStart = Util.Function.IniReadValue("REGCARCONTROL", "GroupTimeStart");
                info.GroupEnd = Util.Function.IniReadValue("REGCARCONTROL", "GroupTimeEnd");
                string tmp = Util.Function.IniReadValue("REGCARCONTROL", "GroupList");
                info.GateGroupName = new string[13];
                if (tmp.Trim() != "")
                {
                    int idx = 0;
                    string[] stmp = tmp.Split(',');
                    for (int i = 0; i < stmp.Length; i++)
                    {
                        info.GateGroupName[i] = stmp[i];
                        idx = i;
                    }
                    for (int i = idx + 1; i < 13; i++)
                    {
                        info.GateGroupName[i] = "";
                    }
                }
                tmp = Util.Function.IniReadValue("REGCARCONTROL", "GroupMent");
                info.GroupMent = new string[13];
                if (tmp.Trim() != "")
                {
                    int idx = 0;
                    string[] stmp = tmp.Split(',');
                    for (int i = 0; i < stmp.Length; i++)
                    {
                        info.GroupMent[i] = stmp[i];
                        idx = i;
                    }
                    for (int i = idx + 1; i < 13; i++)
                    {
                        info.GroupMent[i] = "";
                    }
                }
                tmp = Util.Function.IniReadValue("REGCARCONTROL", "GroupGateUse");
                info.GroupUse = new bool[13];
                if (tmp.Trim() != "")
                {
                    string[] btmp= tmp.Split(',');
                    int idx = 0;
                    for (int i = 0; i < btmp.Length; i++)
                    {
                        bool.TryParse(btmp[i], out info.GroupUse[i]);
                        idx = i;
                    }
                    for (int i = idx + 1; i < 13; i++)
                    {
                        info.GroupUse[i] = false;
                    }
                }
            }
            catch(Exception e) { }
            return info;
        }

        public void Save(RegCarControl info)
        {
            Util.Function.IniFileName = string.Format("{0}\\CameraSetting.ini", Util.Global.ROOT);
            Util.Function.IniWriteValue("REGCARCONTROL", "ENTLIMITUSE", info.Entcontroluse);
            Util.Function.IniWriteValue("REGCARCONTROL", "ENTLIMITMENT", info.Entcontrolment);
            Util.Function.IniWriteValue("REGCARCONTROL", "OtherParkUse", info.OtherparkUse);
            string ment = "";
            foreach (park item in info.Otherparks)
            {
                ment += string.Format("{0}\\{1}\\{2}\t", item.Use, item.parkno, item.ment);
            }
            Util.Function.IniWriteValue("REGCARCONTROL", "OtherParkInfo", ment);
            Util.Function.IniWriteValue("REGCARCONTROL", "OtherParktimeUse", info.OtherparksTimeuse);
            Util.Function.IniWriteValue("REGCARCONTROL", "OtherParktimestart", info.Otherparksstart);
            Util.Function.IniWriteValue("REGCARCONTROL", "OtherParktimeend", info.Otherparksend);

            Util.Function.IniWriteValue("REGCARCONTROL", "RegAutoDelUse", info.Regautodeluse);
            Util.Function.IniWriteValue("REGCARCONTROL", "RegAutoDelTime", info.Regautodeltime);
            Util.Function.IniWriteValue("REGCARCONTROL", "RegEndNotiUse", info.Regendnotiuse);
            Util.Function.IniWriteValue("REGCARCONTROL", "RegEndNotiDay", info.Regendnotiday);
            Util.Function.IniWriteValue("REGCARCONTROL", "PenaltiUse", info.Penaltiuse);
            Util.Function.IniWriteValue("REGCARCONTROL", "PenaltiMent", info.Penaltiment);
            Util.Function.IniWriteValue("REGCARCONTROL", "LotArea", info.Ilotarea);

            Util.Function.IniWriteValue("REGCARCONTROL", "GroupUSE", info.UseGroupGate);
            Util.Function.IniWriteValue("REGCARCONTROL", "ExitGroupUSE", info.UseExitGroupGate);
            Util.Function.IniWriteValue("REGCARCONTROL", "GroupNo", info.GateGroupNo);
            Util.Function.IniWriteValue("REGCARCONTROL", "GroupUseTime", info.GroupUseTime);
            Util.Function.IniWriteValue("REGCARCONTROL", "GroupTimeStart", info.GroupStart);
            Util.Function.IniWriteValue("REGCARCONTROL", "GroupTimeEnd", info.GroupEnd);
            if (info.GateGroupName != null)
                Util.Function.IniWriteValue("REGCARCONTROL", "GroupList", string.Join(",", info.GateGroupName));
            if (info.GroupMent != null)
                Util.Function.IniWriteValue("REGCARCONTROL", "GroupMent", string.Join(",", info.GroupMent));
            if (info.GroupUse != null)
                Util.Function.IniWriteValue("REGCARCONTROL", "GroupGateUse", string.Join(",", info.GroupUse));
        }
    }

    public class park
    {
        public bool Use;
        public int parkno;
        public string ment;
    }
}
