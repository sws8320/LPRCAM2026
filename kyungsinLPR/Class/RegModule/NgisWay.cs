using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Data;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Windows.Forms;

namespace KyungsinLPR
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct RECT
    {
        public int x;
        public int y;
        public int w;
        public int h;
    };

    enum ModuleStatus { UnInit, Init, JOB };

    public class NgisWay_Module
    {
        private object obj = new object();
        public struct Result
        {
            public RECT PlateArea;
            public String PlateNumber;
        }
        private int _Reg1Cnt = 0;

        public int Reg1Cnt
        {
            get { return _Reg1Cnt; }
            set { _Reg1Cnt = value; }
        }

        private int _Reg2Cnt = 0;

        public int Reg2Cnt
        {
            get { return _Reg2Cnt; }
            set { _Reg2Cnt = value; }
        }

        public delegate void eventRegDelegate(int camidx, ClsStructure.RegStruct dr);
        //public delegate void eventRegDelegate(int camidx, DataRow dr);
        public event eventRegDelegate SendResult;

        public frmLprMain main;

        private static int x_d = 500;
        private static int y_d = -1500;
        private static int x_r = 110;
        private static int y_r = 100;
        private static Thread ModuleCheck;

        static ModuleStatus[] Module = new ModuleStatus[9];
        static DateTime[] StartTime = new DateTime[9];
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public static string Module_Init()
        {
            string rtn = string.Empty;
            try
            {
                string strtmp = string.Empty;
                strtmp = Util.Function.IniReadValue("DEGREEOPTION", "XD");
                if (strtmp != string.Empty)
                {
                    x_d = Util.Function.IntTryParse(strtmp);
                }
                strtmp = Util.Function.IniReadValue("DEGREEOPTION", "YD");
                if (strtmp != string.Empty)
                {
                    y_d = Util.Function.IntTryParse(strtmp);
                }
                strtmp = Util.Function.IniReadValue("DEGREEOPTION", "XR");
                if (strtmp != string.Empty)
                {
                    x_r = Util.Function.IntTryParse(strtmp);
                }
                strtmp = Util.Function.IniReadValue("DEGREEOPTION", "YR");
                if (strtmp != string.Empty)
                {
                    y_r = Util.Function.IntTryParse(strtmp);
                }
                Util.Logger.Log(string.Format("각도 옵션 : {0} {1} {2} {3}", x_d, y_d, x_r, y_r));
                if (File.Exists(string.Format("Ngis{0}\\NgisCar.dll", 1)))
                {
                    Util.Logger.Log("인식모듈 1");
                    Ngis1.NgisCarOpen();
                    Module[1] = ModuleStatus.Init;
                    Thread.Sleep(50);
                }
                if (File.Exists(string.Format("Ngis{0}\\NgisCar.dll", 2)))
                {
                    Util.Logger.Log("인식모듈 2");
                    Ngis2.NgisCarOpen();
                    Module[2] = ModuleStatus.Init;
                    Thread.Sleep(50);
                }
                if (File.Exists(string.Format("Ngis{0}\\NgisCar.dll", 3)))
                {
                    Util.Logger.Log("인식모듈 3");
                    Ngis3.NgisCarOpen();
                    Module[3] = ModuleStatus.Init;
                    Thread.Sleep(50);
                }
                if (File.Exists(string.Format("Ngis{0}\\NgisCar.dll", 4)))
                {
                    Util.Logger.Log("인식모듈 4");
                    Ngis4.NgisCarOpen();
                    Module[4] = ModuleStatus.Init;
                    Thread.Sleep(50);
                }
                if (File.Exists(string.Format("Ngis{0}\\NgisCar.dll", 5)))
                {
                    Util.Logger.Log("인식모듈 5");
                    Ngis5.NgisCarOpen();
                    Module[5] = ModuleStatus.Init;
                    Thread.Sleep(50);
                }
                if (File.Exists(string.Format("Ngis{0}\\NgisCar.dll", 6)))
                {
                    Util.Logger.Log("인식모듈 6");
                    Ngis6.NgisCarOpen();
                    Module[6] = ModuleStatus.Init;
                    Thread.Sleep(50);
                }
                if (File.Exists(string.Format("Ngis{0}\\NgisCar.dll", 7)))
                {
                    Util.Logger.Log("인식모듈 7");
                    Ngis7.NgisCarOpen();
                    Module[7] = ModuleStatus.Init;
                    Thread.Sleep(50);
                }
                if (File.Exists(string.Format("Ngis{0}\\NgisCar.dll", 8)))
                {
                    Util.Logger.Log("인식모듈 8");
                    Ngis8.NgisCarOpen();
                    Module[8] = ModuleStatus.Init;
                }

                ModuleCheck = new Thread(new ThreadStart(Module_Check));
                ModuleCheck.IsBackground = true;
                ModuleCheck.Start();
            }
            catch (Exception e)
            {
                //rtn = e.Message;
                Util.Logger.Log(string.Format("Module_Init Error {0}", e.Message));
            }
            return rtn;
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public static void Module_Close()
        {
            if (File.Exists(string.Format("Ngis{0}\\NgisCar.dll", 1)))
                Ngis1.NgisCarClose();
            if (File.Exists(string.Format("Ngis{0}\\NgisCar.dll", 2)))
                Ngis2.NgisCarClose();
            if (File.Exists(string.Format("Ngis{0}\\NgisCar.dll", 3)))
                Ngis3.NgisCarClose();
            if (File.Exists(string.Format("Ngis{0}\\NgisCar.dll", 4)))
                Ngis4.NgisCarClose();
            if (File.Exists(string.Format("Ngis{0}\\NgisCar.dll", 5)))
                Ngis5.NgisCarClose();
            if (File.Exists(string.Format("Ngis{0}\\NgisCar.dll", 6)))
                Ngis6.NgisCarClose();
            if (File.Exists(string.Format("Ngis{0}\\NgisCar.dll", 7)))
                Ngis7.NgisCarClose();
            if (File.Exists(string.Format("Ngis{0}\\NgisCar.dll", 8)))
                Ngis8.NgisCarClose();
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public static void Module_CloseOpen(int idx)
        {
            try
            {
                Util.Logger.Log(string.Format("===================={0}번 인식 모듈 재기동", idx));
                switch (idx)
                {
                    case 1:
                        Ngis1.NgisCarClose();
                        Thread.Sleep(100);
                        Ngis1.NgisCarOpen();
                        Module[1] = ModuleStatus.Init;
                        StartTime[1] = DateTime.Now;
                        break;
                    case 2:
                        Ngis2.NgisCarClose();
                        Thread.Sleep(100);
                        Ngis2.NgisCarOpen();
                        Module[2] = ModuleStatus.Init;
                        StartTime[2] = DateTime.Now;
                        break;
                    case 3:
                        Ngis3.NgisCarClose();
                        Thread.Sleep(100);
                        Ngis3.NgisCarOpen();
                        Module[3] = ModuleStatus.Init;
                        StartTime[3] = DateTime.Now;
                        break;
                    case 4:
                        Ngis4.NgisCarClose();
                        Thread.Sleep(100);
                        Ngis4.NgisCarOpen();
                        Module[4] = ModuleStatus.Init;
                        StartTime[4] = DateTime.Now;
                        break;
                    case 5:
                        Ngis5.NgisCarClose();
                        Thread.Sleep(100);
                        Ngis5.NgisCarOpen();
                        Module[5] = ModuleStatus.Init;
                        StartTime[5] = DateTime.Now;
                        break;
                    case 6:
                        Ngis6.NgisCarClose();
                        Thread.Sleep(100);
                        Ngis6.NgisCarOpen();
                        Module[6] = ModuleStatus.Init;
                        StartTime[6] = DateTime.Now;
                        break;
                    case 7:
                        Ngis7.NgisCarClose();
                        Thread.Sleep(100);
                        Ngis7.NgisCarOpen();
                        Module[7] = ModuleStatus.Init;
                        StartTime[7] = DateTime.Now;
                        break;
                    case 8:
                        Ngis8.NgisCarClose();
                        Thread.Sleep(100);
                        Ngis8.NgisCarOpen();
                        Module[8] = ModuleStatus.Init;
                        StartTime[8] = DateTime.Now;
                        break;
                }
            }
            catch (AccessViolationException e)
            {
                Util.Logger.Log(string.Format("Module_CloseOpen AccessViolationException {0}번 인식 모듈 재기동 중 오류 {1}", idx, e.Message));
                //Util.Logger.Log("프로그램 재기동");
                //Application.Restart();

                Module[idx] = ModuleStatus.JOB;
            }
            catch (Exception ex)
            {
                Util.Logger.Log(string.Format("Module_CloseOpen Exception {0}번 인식 모듈 재기동 중 오류 {1}", idx, ex.Message));
                Module[idx] = ModuleStatus.JOB;
            }
        }

        //public void Reg(int Listidx, int CamIdx, String FilePath, RECT ROI)
        //{
        //    Thread t = new Thread(delegate()
        //    {
        //        RegPlate(Listidx, CamIdx, FilePath, ROI);
        //    });
        //    t.IsBackground = true;
        //    t.Start();
        //}

        public void Reg1(DataRow dr)
        {
            Thread t = new Thread(delegate()
            {
                //RegPlate(Listidx, CamIdx, FilePath, ROI);
                RegPlate(1, dr);
            });
            t.IsBackground = true;
            t.Start();
        }

        public void Reg2(DataRow dr)
        {
            Thread t = new Thread(delegate()
            {
                //RegPlate(Listidx, CamIdx, FilePath, ROI);
                RegPlate(2, dr);
            });
            t.IsBackground = true;
            t.Start();
        }

        public void Reg1(ClsStructure.RegStruct dr)
        {
            Thread t = new Thread(delegate()
            {
                //RegPlate(Listidx, CamIdx, FilePath, ROI);
                RegPlate(1, dr);
            });
            t.IsBackground = true;
            t.Start();
        }

        public void Reg2(ClsStructure.RegStruct dr)
        {
            Thread t = new Thread(delegate()
            {
                //RegPlate(Listidx, CamIdx, FilePath, ROI);
                RegPlate(2, dr);
            });
            t.IsBackground = true;
            t.Start();
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        //private void RegPlate(int Listidx, int CamIdx, String FilePath, RECT ROI)
        private void RegPlate(int camindex, DataRow dr)
        {
            Stopwatch sw = new Stopwatch();
            RECT PlateArea = new RECT();
            StringBuilder result = new StringBuilder();
            int Jobidx = 0;
            try
            {
                int Listidx = 0;
                int.TryParse(dr[2].ToString(), out Listidx);
                int CamIdx = 0;
                int.TryParse(dr[1].ToString(), out CamIdx);
                string FilePath = dr[3].ToString();
                RECT ROI = new RECT();
                string[] sp = dr[4].ToString().Split(',');
                int.TryParse(sp[0], out ROI.x);
                int.TryParse(sp[1], out ROI.y);
                int.TryParse(sp[2], out ROI.w);
                int.TryParse(sp[3], out ROI.h);

                long lResult = 0;
                sw.Start();
                if (Module[1] == ModuleStatus.Init)
                {
                    Jobidx = 1;
                    Module[1] = ModuleStatus.JOB;
                    lock (obj)
                    {
                        lResult = Ngis1.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100)
                            lResult = Ngis1.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Module[1] = ModuleStatus.Init;
                }
                else if (Module[2] == ModuleStatus.Init)
                {
                    Jobidx = 2;
                    Module[2] = ModuleStatus.JOB;
                    lock (obj)
                    {
                        lResult = Ngis2.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100)
                            lResult = Ngis2.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Module[2] = ModuleStatus.Init;
                }
                else if (Module[3] == ModuleStatus.Init)
                {
                    Jobidx = 3;
                    Module[3] = ModuleStatus.JOB;
                    lock (obj)
                    {
                        lResult = Ngis3.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100)
                            lResult = Ngis3.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Module[4] = ModuleStatus.Init;
                }
                else if (Module[4] == ModuleStatus.Init)
                {
                    Jobidx = 4;
                    Module[4] = ModuleStatus.JOB;
                    lock (obj)
                    {
                        lResult = Ngis4.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100)
                            lResult = Ngis4.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Module[4] = ModuleStatus.Init;
                }
                else if (Module[5] == ModuleStatus.Init)
                {
                    Jobidx = 5;
                    Module[5] = ModuleStatus.JOB;
                    lock (obj)
                    {
                        lResult = Ngis5.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100)
                            lResult = Ngis5.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Module[5] = ModuleStatus.Init;
                }
                else if (Module[6] == ModuleStatus.Init)
                {
                    Jobidx = 6;
                    Module[6] = ModuleStatus.JOB;
                    lock (obj)
                    {
                        lResult = Ngis6.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100)
                            lResult = Ngis6.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Module[6] = ModuleStatus.Init;
                }
                else if (Module[7] == ModuleStatus.Init)
                {
                    Jobidx = 7;
                    Module[7] = ModuleStatus.JOB;
                    lock (obj)
                    {
                        lResult = Ngis7.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100)
                            lResult = Ngis7.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Module[7] = ModuleStatus.Init;
                }
                else if (Module[8] == ModuleStatus.Init)
                {
                    Jobidx = 8;
                    Module[8] = ModuleStatus.JOB;
                    lock (obj)
                    {
                        lResult = Ngis8.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100)
                            lResult = Ngis8.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Module[8] = ModuleStatus.Init;
                }
            }
            catch (AccessViolationException e)
            {
                Util.Logger.Log(string.Format("RegPlate AccessViolationException JobIDX {0} {1}", Jobidx, e.Message));
                //Module_CloseOpen(Jobidx);
            }
            catch (Exception ex)
            {
                Util.Logger.Log(string.Format("RegPlate Exception JobIDX {0} {1}", Jobidx, ex.Message));
            }
            sw.Stop();
            Result Rtn = new Result();
            PlateArea.w -= PlateArea.x;
            PlateArea.h -= PlateArea.y;
            Rtn.PlateArea = PlateArea;
            Rtn.PlateNumber = result.ToString();
            if (Rtn.PlateNumber.Equals(string.Empty))
                Rtn.PlateNumber = "No_Detection";
            dr["PlateRoi"] = string.Format("{0},{1},{2},{3}", PlateArea.x, PlateArea.y, PlateArea.w, PlateArea.h);
            dr["PlateNo"] = Rtn.PlateNumber;
            dr["Term"] = sw.ElapsedMilliseconds;
            if (this.SendResult != null)
            //this.SendResult(sw.ElapsedMilliseconds, CamIdx, FilePath, Rtn);
            {
                //this.SendResult(camindex, dr);
            }
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        //private void RegPlate(int Listidx, int CamIdx, String FilePath, RECT ROI)
        public void RegPlate(int camindex, ClsStructure.RegStruct dr)
        {
            Stopwatch sw = new Stopwatch();
            RECT PlateArea = new RECT();
            StringBuilder result = new StringBuilder();
            int Jobidx = 0;
            try
            {
                string FilePath = dr.SourcePath;
                RECT ROI = new RECT();
                string[] sp = dr.Roi.ToString().Split(',');
                int.TryParse(sp[0], out ROI.x);
                int.TryParse(sp[1], out ROI.y);
                int.TryParse(sp[2], out ROI.w);
                int.TryParse(sp[3], out ROI.h);

                long lResult = 0;
                sw.Start();
                if (Module[1] == ModuleStatus.Init)
                {
                    Jobidx = 1;
                    Module[1] = ModuleStatus.JOB;
                    lock (obj)
                    {
                        lResult = Ngis1.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100)
                            lResult = Ngis1.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Module[1] = ModuleStatus.Init;
                }
                else if (Module[2] == ModuleStatus.Init)
                {
                    Jobidx = 2;
                    Module[2] = ModuleStatus.JOB;
                    lock (obj)
                    {
                        lResult = Ngis2.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100)
                            lResult = Ngis2.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Module[2] = ModuleStatus.Init;
                }
                else if (Module[3] == ModuleStatus.Init)
                {
                    Jobidx = 3;
                    Module[3] = ModuleStatus.JOB;
                    lock (obj)
                    {
                        lResult = Ngis3.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100)
                            lResult = Ngis3.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Module[3] = ModuleStatus.Init;
                }
                else if (Module[4] == ModuleStatus.Init)
                {
                    Jobidx = 4;
                    Module[4] = ModuleStatus.JOB;
                    lock (obj)
                    {
                        lResult = Ngis4.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100)
                            lResult = Ngis4.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Module[4] = ModuleStatus.Init;
                }
                else if (Module[5] == ModuleStatus.Init)
                {
                    Jobidx = 5;
                    Module[5] = ModuleStatus.JOB;
                    lock (obj)
                    {
                        lResult = Ngis5.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100)
                            lResult = Ngis5.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Module[5] = ModuleStatus.Init;
                }
                else if (Module[6] == ModuleStatus.Init)
                {
                    Jobidx = 6;
                    Module[6] = ModuleStatus.JOB;
                    lock (obj)
                    {
                        lResult = Ngis6.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100)
                            lResult = Ngis6.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Module[6] = ModuleStatus.Init;
                }
                else if (Module[7] == ModuleStatus.Init)
                {
                    Jobidx = 7;
                    Module[7] = ModuleStatus.JOB;
                    lock (obj)
                    {
                        lResult = Ngis7.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100)
                            lResult = Ngis7.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Module[7] = ModuleStatus.Init;
                }
                else if (Module[8] == ModuleStatus.Init)
                {
                    Jobidx = 8;
                    Module[8] = ModuleStatus.JOB;
                    lock (obj)
                    {
                        lResult = Ngis8.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100)
                            lResult = Ngis8.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Module[8] = ModuleStatus.Init;
                }
            }
            catch (AccessViolationException e)
            {
                Util.Logger.Log(string.Format("RegPlate AccessViolationException {0} {1}", e.Message, Jobidx));
                //Module_CloseOpen(Jobidx);
            }
            sw.Stop();
            Util.Logger.Log("**********Jobid : " + Jobidx);
            Util.Logger.Log(string.Format("camindex {0} filename {1} ", camindex, dr.SourcePath));
            //Module[Jobidx] = ModuleStatus.Init;
            Result Rtn = new Result();
            PlateArea.w -= PlateArea.x;
            PlateArea.h -= PlateArea.y;
            Rtn.PlateArea = PlateArea;
            Rtn.PlateNumber = result.ToString();
            if (string.IsNullOrEmpty(Rtn.PlateNumber))
                Rtn.PlateNumber = "No_Detection";
            dr.PlateRoi = string.Format("{0},{1},{2},{3}", PlateArea.x, PlateArea.y, PlateArea.w, PlateArea.h);
            dr.PlateNo = Rtn.PlateNumber;
            dr.term = sw.ElapsedMilliseconds;
            //if (this.SendResult != null)
            ////this.SendResult(sw.ElapsedMilliseconds, CamIdx, FilePath, Rtn);
            //{
            //    Util.Logger.Log("SendResult camindex " + camindex);
            //    this.SendResult(camindex, dr);
            //}

            StartTime[Jobidx] = DateTime.Now;
            //for (int i = 1; i < Module.Length; i++)
            //{
            //    if (Module[i] != ModuleStatus.Init && Module[i] != ModuleStatus.UnInit && StartTime[i] != default(DateTime) && (DateTime.Now - StartTime[i]).TotalSeconds > 10)
            //        //Module_CloseOpen(i);
            //        Module[i] = ModuleStatus.Init;
            //}
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        //private void RegPlate(int Listidx, int CamIdx, String FilePath, RECT ROI)
        public void RegPlate(int camindex, int idx)
        {
            Stopwatch sw = new Stopwatch();
            RECT PlateArea = new RECT();
            StringBuilder result = new StringBuilder();
            ClsStructure.RegStruct[] RegArray = new ClsStructure.RegStruct[4];
            if (camindex == 0)
                RegArray = clsThread.RegArray1;
            else
                RegArray = clsThread.RegArray2;
            int Jobidx = 0;
            try
            {
                string FilePath = RegArray[idx].SourcePath;
                RECT ROI = new RECT();
                string[] sp = RegArray[idx].Roi.ToString().Split(',');
                int.TryParse(sp[0], out ROI.x);
                int.TryParse(sp[1], out ROI.y);
                int.TryParse(sp[2], out ROI.w);
                int.TryParse(sp[3], out ROI.h);

                long lResult = 0;
                sw.Start();
                if (Module[1] == ModuleStatus.Init)
                {
                    Jobidx = 1;
                    StartTime[1] = DateTime.Now;
                    Module[1] = ModuleStatus.JOB;
                    Util.Logger.Log(string.Format("camindex {2} {0}번 인식 모듈 인식 시작 파일 명 {1}", Jobidx, FilePath, camindex));
                    lock (obj)
                    {
                        lResult = Ngis1.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100 && x_d + y_d + x_r + y_r > 0)
                            lResult = Ngis1.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Util.Logger.Log(string.Format("camindex {3} {0}번 인식 모듈 인식 결과 {1} {2}", Jobidx, lResult.ToString(), result.ToString() == null ? "No_Detection" : result.ToString(), camindex));
                    Module[1] = ModuleStatus.Init;
                }
                else if (Module[2] == ModuleStatus.Init)
                {
                    Jobidx = 2;
                    StartTime[2] = DateTime.Now;
                    Module[2] = ModuleStatus.JOB;
                    Util.Logger.Log(string.Format("camindex {2} {0}번 인식 모듈 인식 시작 파일 명 {1}", Jobidx, FilePath, camindex));
                    lock (obj)
                    {
                        lResult = Ngis2.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100 && x_d + y_d + x_r + y_r > 0)
                            lResult = Ngis2.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Util.Logger.Log(string.Format("camindex {3} {0}번 인식 모듈 인식 결과 {1} {2}", Jobidx, lResult.ToString(), result.ToString() == null ? "No_Detection" : result.ToString(), camindex));
                    Module[2] = ModuleStatus.Init;
                }
                else if (Module[3] == ModuleStatus.Init)
                {
                    Jobidx = 3;
                    StartTime[3] = DateTime.Now;
                    Module[3] = ModuleStatus.JOB;
                    Util.Logger.Log(string.Format("camindex {2} {0}번 인식 모듈 인식 시작 파일 명 {1}", Jobidx, FilePath, camindex));
                    lock (obj)
                    {
                        lResult = Ngis3.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100 && x_d + y_d + x_r + y_r > 0)
                            lResult = Ngis3.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Util.Logger.Log(string.Format("camindex {3} {0}번 인식 모듈 인식 결과 {1} {2}", Jobidx, lResult.ToString(), result.ToString() == null ? "No_Detection" : result.ToString(), camindex));
                    Module[3] = ModuleStatus.Init;
                }
                else if (Module[4] == ModuleStatus.Init)
                {
                    Jobidx = 4;
                    StartTime[4] = DateTime.Now;
                    Module[4] = ModuleStatus.JOB;
                    Util.Logger.Log(string.Format("camindex {3} {0}번 인식 모듈 인식 시작 파일 명 {1} {2}", Jobidx, lResult.ToString(), result.ToString() == null ? "No_Detection" : result.ToString(), camindex));
                    lock (obj)
                    {
                        lResult = Ngis4.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100 && x_d + y_d + x_r + y_r > 0)
                            lResult = Ngis4.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Util.Logger.Log(string.Format("camindex {3} {0}번 인식 모듈 인식 결과 {1} {2}", Jobidx, lResult.ToString(), result.ToString() == null ? "No_Detection" : result.ToString(), camindex));
                    Module[4] = ModuleStatus.Init;
                }
                else if (Module[5] == ModuleStatus.Init)
                {
                    Jobidx = 5;
                    StartTime[5] = DateTime.Now;
                    Module[5] = ModuleStatus.JOB;
                    Util.Logger.Log(string.Format("camindex {2} {0}번 인식 모듈 인식 시작 파일 명 {1}", Jobidx, FilePath, camindex));
                    lock (obj)
                    {
                        lResult = Ngis5.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100 && x_d + y_d + x_r + y_r > 0)
                            lResult = Ngis5.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Util.Logger.Log(string.Format("camindex {3} {0}번 인식 모듈 인식 결과 {1} {2}", Jobidx, lResult.ToString(), result.ToString() == null ? "No_Detection" : result.ToString(), camindex));
                    Module[5] = ModuleStatus.Init;
                }
                else if (Module[6] == ModuleStatus.Init)
                {
                    Jobidx = 6;
                    StartTime[6] = DateTime.Now;
                    Module[6] = ModuleStatus.JOB;
                    Util.Logger.Log(string.Format("camindex {2} {0}번 인식 모듈 인식 시작 파일 명 {1}", Jobidx, FilePath, camindex));
                    lock (obj)
                    {
                        lResult = Ngis6.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100 && x_d + y_d + x_r + y_r > 0)
                            lResult = Ngis6.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Util.Logger.Log(string.Format("camindex {3} {0}번 인식 모듈 인식 결과 {1} {2}", Jobidx, lResult.ToString(), result.ToString() == null ? "No_Detection" : result.ToString(), camindex));
                    Module[6] = ModuleStatus.Init;
                }
                else if (Module[7] == ModuleStatus.Init)
                {
                    Jobidx = 7;
                    StartTime[7] = DateTime.Now;
                    Module[7] = ModuleStatus.JOB;
                    Util.Logger.Log(string.Format("camindex {2} {0}번 인식 모듈 인식 시작 파일 명 {1}", Jobidx, FilePath, camindex));
                    lock (obj)
                    {
                        lResult = Ngis7.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100 && x_d + y_d + x_r + y_r > 0)
                            lResult = Ngis7.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Util.Logger.Log(string.Format("camindex {3} {0}번 인식 모듈 인식 결과 {1} {2}", Jobidx, lResult.ToString(), result.ToString() == null ? "No_Detection" : result.ToString(), camindex));
                    Module[7] = ModuleStatus.Init;
                }
                else if (Module[8] == ModuleStatus.Init)
                {
                    Jobidx = 8;
                    StartTime[8] = DateTime.Now;
                    Module[8] = ModuleStatus.JOB;
                    Util.Logger.Log(string.Format("camindex {2} {0}번 인식 모듈 인식 시작 파일 명 {1}", Jobidx, FilePath, camindex));
                    lock (obj)
                    {
                        lResult = Ngis8.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, 0, 0, 0, 0);
                        if (lResult == 100 && x_d + y_d + x_r + y_r > 0)
                            lResult = Ngis8.NgisCarOcrVB(FilePath, 0, ref ROI, result, ref PlateArea, x_d, y_d, x_r, y_r);
                    }
                    Util.Logger.Log(string.Format("camindex {3} {0}번 인식 모듈 인식 결과 {1} {2}", Jobidx, lResult.ToString(), result.ToString() == null ? "No_Detection" : result.ToString(), camindex));
                    Module[8] = ModuleStatus.Init;
                }
            }
            catch (NullReferenceException nullErr)
            {
                Util.Logger.Log(string.Format("camindex {2} RegPlate NullReferenceException {0} {1}", nullErr.Message, Jobidx, camindex));
            }
            catch (AccessViolationException e)
            {
                Util.Logger.Log(string.Format("camindex {2} RegPlate AccessViolationException {0} {1}", e.Message, Jobidx, camindex));
                //Module_CloseOpen(Jobidx);
            }
            sw.Stop();
            // 0 이상이면서 아래의 값을 가지면 인식 성공
            // 번호판유형 (5 가지 번호판유형)
            // 1   : (서울12 가1234)
            // 2   : (서울3 가1234)
            // 3   : (12가 1234)
            // 4   : (가로 1줄짜리 번호판, 현재 경찰차)
            // 5   : (주황색 특장차 번호판)

            // 100 : 부분인식한 경우.
            Util.Logger.Log("**********Jobid : " + Jobidx);
            Util.Logger.Log(string.Format("camindex {0} filename {1} ", camindex, RegArray[idx].SourcePath));
            //Module[Jobidx] = ModuleStatus.Init;
            Result Rtn = new Result();
            PlateArea.w -= PlateArea.x;
            PlateArea.h -= PlateArea.y;
            Rtn.PlateArea = PlateArea;
            Rtn.PlateNumber = result.ToString();
            if (Rtn.PlateNumber.Equals(string.Empty))
                Rtn.PlateNumber = "No_Detection";
            RegArray[idx].PlateRoi = string.Format("camindex {4} {0},{1},{2},{3}", PlateArea.x, PlateArea.y, PlateArea.w, PlateArea.h, camindex);
            //RegArray[idx].PlateNo = Rtn.PlateNumber;
            RegArray[idx].PlateNo = Rtn.PlateNumber == null ? "No_Detection" : Rtn.PlateNumber;
            RegArray[idx].term = sw.ElapsedMilliseconds;
            //if (this.SendResult != null)
            ////this.SendResult(sw.ElapsedMilliseconds, CamIdx, FilePath, Rtn);
            //{
            //    Util.Logger.Log("SendResult camindex " + camindex);
            //    this.SendResult(camindex, dr);
            //}

            StartTime[Jobidx] = DateTime.Now;
            Util.Logger.Log(string.Format("CamIdx {0} JobIdx {1} StartTime {2}", camindex, Jobidx, StartTime[Jobidx]));
            //for (int i = 1; i < Module.Length; i++)
            //{
            //    if (Module[i] != ModuleStatus.Init && Module[i] != ModuleStatus.UnInit && StartTime[i] != default(DateTime) && (DateTime.Now - StartTime[i]).TotalSeconds > 10)
            //        //Module_CloseOpen(i);
            //        Module[i] = ModuleStatus.Init;
            //}
            if (camindex == 0)
                clsThread.Cam1RegCnt++;
            else if (camindex == 1)
                clsThread.Cam2RegCnt++;
            Util.Logger.Log(string.Format("RegPlate End CamIdx {0}", camindex));
        }

        private static void Module_Check()
        {
            Util.Logger.Log("Module_Check Start");
            int AsIsJobCnt = 0;
            while (true)
            {
                int JobCnt = 0;
                for (int i = 1; i < 9; i++)
                {
                    if (Module[i] == ModuleStatus.JOB && StartTime[i] > DateTime.MinValue)
                    {
                        TimeSpan diff = DateTime.Now - StartTime[i];
                        if (diff.TotalSeconds > 3)
                        {
                            //Util.Logger.Log(string.Format("Module_Check Module ReSet {0} {1}", i, StartTime[i]));
                            //Module_CloseOpen(i);
                            JobCnt++;
                        }
                    }
                }
                if (JobCnt >= 7)
                {
                    Application.ExitThread();
                    Application.Exit();
                }
                if (AsIsJobCnt != JobCnt)
                {
                    Util.Logger.Log(string.Format("*****모듈 사용 중 증가 {0}*****", JobCnt));
                    AsIsJobCnt = JobCnt;
                }
                Thread.Sleep(1000);
            }
        }
    }

    class Ngis1
    {
        [DllImport("Ngis1\\NgisCar.dll")]
        public static extern Int32 NgisCarOpen();
        [DllImport("Ngis1\\NgisCar.dll")]
        public static extern Int32 NgisCarClose();
        [DllImport("Ngis1\\NgisCar.dll")]
        public static extern Int32 NgisCarOcrVB(
                string strImgPath, int file_option, ref RECT pr, StringBuilder recog_str,
                ref RECT r, int x_d, int y_d, int x_r,int y_r);
    }
    class Ngis2
    {

        [DllImport("Ngis2\\NgisCar.dll")]
        public static extern Int32 NgisCarOpen();
        [DllImport("Ngis2\\NgisCar.dll")]
        public static extern Int32 NgisCarClose();
        [DllImport("Ngis2\\NgisCar.dll")]
        public static extern Int32 NgisCarOcrVB(
                string strImgPath, int file_option, ref RECT pr, StringBuilder recog_str,
                ref RECT r, int x_d, int y_d, int x_r, int y_r);
    }

    class Ngis3
    {

        [DllImport("Ngis3\\NgisCar.dll")]
        public static extern Int32 NgisCarOpen();
        [DllImport("Ngis3\\NgisCar.dll")]
        public static extern Int32 NgisCarClose();
        [DllImport("Ngis3\\NgisCar.dll")]
        public static extern Int32 NgisCarOcrVB(
                string strImgPath, int file_option, ref RECT pr, StringBuilder recog_str,
                ref RECT r, int x_d, int y_d, int x_r, int y_r);
    }

    class Ngis4
    {

        [DllImport("Ngis4\\NgisCar.dll")]
        public static extern Int32 NgisCarOpen();
        [DllImport("Ngis4\\NgisCar.dll")]
        public static extern Int32 NgisCarClose();
        [DllImport("Ngis4\\NgisCar.dll")]
        public static extern Int32 NgisCarOcrVB(
                string strImgPath, int file_option, ref RECT pr, StringBuilder recog_str,
                ref RECT r, int x_d, int y_d, int x_r, int y_r);
    }

    class Ngis5
    {
        [DllImport("Ngis5\\NgisCar.dll")]
        public static extern Int32 NgisCarOpen();
        [DllImport("Ngis5\\NgisCar.dll")]
        public static extern Int32 NgisCarClose();
        [DllImport("Ngis5\\NgisCar.dll")]
        public static extern Int32 NgisCarOcrVB(
                string strImgPath, int file_option, ref RECT pr, StringBuilder recog_str,
                ref RECT r, int x_d, int y_d, int x_r, int y_r);
    }

    class Ngis6
    {
        [DllImport("Ngis6\\NgisCar.dll")]
        public static extern Int32 NgisCarOpen();
        [DllImport("Ngis6\\NgisCar.dll")]
        public static extern Int32 NgisCarClose();
        [DllImport("Ngis6\\NgisCar.dll")]
        public static extern Int32 NgisCarOcrVB(
                string strImgPath, int file_option, ref RECT pr, StringBuilder recog_str,
                ref RECT r, int x_d, int y_d, int x_r, int y_r);
    }


    class Ngis7
    {
        [DllImport("Ngis7\\NgisCar.dll")]
        public static extern Int32 NgisCarOpen();
        [DllImport("Ngis7\\NgisCar.dll")]
        public static extern Int32 NgisCarClose();
        [DllImport("Ngis7\\NgisCar.dll")]
        public static extern Int32 NgisCarOcrVB(
                string strImgPath, int file_option, ref RECT pr, StringBuilder recog_str,
                ref RECT r, int x_d, int y_d, int x_r, int y_r);
    }

    class Ngis8
    {
        [DllImport("Ngis8\\NgisCar.dll")]
        public static extern Int32 NgisCarOpen();
        [DllImport("Ngis8\\NgisCar.dll")]
        public static extern Int32 NgisCarClose();
        [DllImport("Ngis8\\NgisCar.dll")]
        public static extern Int32 NgisCarOcrVB(
                string strImgPath, int file_option, ref RECT pr, StringBuilder recog_str,
                ref RECT r, int x_d, int y_d, int x_r, int y_r);
    }
}
