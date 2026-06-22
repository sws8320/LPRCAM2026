using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using Evo;

namespace KyungsinLPR {
    #region asis evo
    //public static class CoreLogicAsis
    //{
    //    private static int rc; // Return Code
    //    private static int num;
    //    private static UIntPtr dic = (UIntPtr)0; // Detection Information Context
    //    //private static UIntPtr handSSE = (UIntPtr)0; // Handle of Snapshot Engine
    //    private static UIntPtr[] handSSE; // Handle of Snapshot Engine
    //    private static object obj = new object();

    //    public static void Init(bool ch2)
    //    {
    //        try
    //        {
    //            int cnt = 1;
    //            Util.Logger.Log("Evo.Setup.Run");
    //            if (ch2)
    //                cnt = 2;
    //            handSSE = new UIntPtr[cnt];
    //            // Initialize Evo LPR Engine with the installation root directory.
    //            rc = Evo.Setup.Run();
    //            if (rc != 0)
    //            {
    //                Util.Logger.Log("Evo.Setup.Run() failed... : " + rc.ToString());
    //                return;
    //            }
    //            for (int i = 0; i < cnt; i++)
    //            {
    //                Util.Logger.Log("SnapshotEngine.Allocate()");
    //                // Allocate a Snapshot Engine.
    //                rc = Evo.SnapshotEngine.Allocate(ref handSSE[i]);
    //                if (rc != 0)
    //                {
    //                    Util.Logger.Log("SnapshotEngine.Allocate() failed... : " + rc.ToString());
    //                    return;
    //                }
    //                Util.Logger.Log("SnapshotEngine.Initialize()");
    //                // Initialize the allocated engine.
    //                // Available 'dtd' values: 0(CPU), 1(GPU), 2(Myriad VPU)
    //                rc = Evo.SnapshotEngine.Initailize(handSSE[i], frmLprMain.ENV.CameraEnv.CoreType);
    //                if (rc != 0)
    //                {
    //                    Util.Logger.Log("SnapshotEngine.Initialize() failed... : " + rc.ToString());
    //                    Evo.SnapshotEngine.Release(ref handSSE[i]);
    //                    return;
    //                }
    //            }
    //            Util.Logger.Log("Initialize 완료");
    //        }
    //        catch (Exception)
    //        { }
    //        return;
    //    }

    //    public static void Reg(int Camindex, int idx)
    //    {
    //        Util.Logger.Log("Reg Start");
    //        Thread t = new Thread(delegate ()
    //        {
    //            //RegPlate(Listidx, CamIdx, FilePath, ROI);
    //            RegPlateNo(Camindex, idx);
    //        });
    //        t.IsBackground = true;
    //        t.Start();
    //    }

    //    public static Result RegPlateNo(int Camindex, int idx)
    //    {
    //        Util.Logger.Log("RegPlateNo Start");
    //        Result result = new Result();
    //        //Stopwatch sp = new Stopwatch();
    //        DateTime dateTime = new DateTime();
    //        ClsStructure.RegStruct RegArray = new ClsStructure.RegStruct();
    //        //UIntPtr dic = (UIntPtr)0;
    //        switch (Camindex)
    //        {
    //            case 0:
    //                RegArray = clsThread.RegArray1[idx];
    //                break;
    //            case 1:
    //                RegArray = clsThread.RegArray2[idx];
    //                break;
    //        }
    //        Util.Logger.Log(string.Format("Cam Idx {0} image idx {1} image path {2}", Camindex, idx, RegArray.SourcePath));
    //        //sp.Start();
    //        dateTime = DateTime.Now;
    //        lock (obj)
    //        {
    //            //dic = (UIntPtr)0; //재호출 시 0 설정 해야됨
    //            // Detect license plates and get Detection Information Context if any.
    //            //rc = Evo.SnapshotEngine.Detect(handSSE[Camindex], RegArray.SourcePath, 0.1F, ref dic);
    //            const float minConf = 2.5F; // Recommend 2.5% as minimum detection confidence.
    //            UIntPtr ctxLPI = UIntPtr.Zero; // Context of LPI.
    //            Evo.Rect validRegion = new Evo.Rect(); // All over the image.
    //            if (RegArray.SourcePath.ToLower().EndsWith(".bmp"))
    //            {
    //                Bitmap bm = new Bitmap(RegArray.SourcePath);

    //                // !!! Currently support only RGB24 pixel format. !!!
    //                rc = Evo.SnapshotEngine.Detect(handSSE[Camindex], bm, ref validRegion, minConf, ref ctxLPI);
    //            }
    //            else
    //            {
    //                rc = Evo.SnapshotEngine.Detect(handSSE[Camindex], RegArray.SourcePath, ref validRegion, minConf, ref ctxLPI);
    //            }
    //            if (rc != 0)
    //            {
    //                Util.Logger.Log("Evo.SnapshotEngine.Detect() failed... : " + rc.ToString());
    //                //Console.ReadKey();

    //                //Evo.SnapshotEngine.Deinitialize(handSSE);
    //                //Evo.SnapshotEngine.Release(ref handSSE);
    //                result.Error = "Evo.SnapshotEngine.Detect() failed... : " + rc.ToString();
    //                return result;
    //            }

    //            // In case there are detected ones.
    //            if (ctxLPI != UIntPtr.Zero)
    //            {
    //                uint num;
    //                // Get total number of the detected ones.
    //                rc = Evo.LPI.GetNumber(ctxLPI, out num);
    //                if (rc != 0)
    //                {
    //                    Util.Logger.Log("Evo.LPI.GetNumber() failed... : " + rc);
    //                    //Console.ReadKey();gine.Release(ref handSSE);
    //                    result.Error = "Evo.LPI.GetNumber() failed... : " + rc;
    //                }
    //                else if (num > 0)
    //                {
    //                    //Console.WriteLine("=======================================================");
    //                    //Console.WriteLine("Detection Information of '{0}'", RegArray.SourcePath);
    //                    //Console.WriteLine("-------------------------------------------------------");
    //                    result.CarNo = new string[num];
    //                    result.rect = new Rectangle[num];
    //                    result.Term = new long[num];
    //                    result.Confidence = new int[num];
    //                    for (uint i = 0; i < num; i++)
    //                    {
    //                        float confidence;
    //                        //int x, y, width, height;
    //                        //byte[] strBuf = new byte[1024];

    //                        Console.WriteLine("[{0}]", i);

    //                        // Get detection confidence.
    //                        rc = Evo.LPI.GetConfidenceEVLP(ctxLPI, i, out confidence);
    //                        if (rc != 0)
    //                        {
    //                            result.CarNo[i] = "No_Detection";
    //                            result.Term[i] = (long)(DateTime.Now - dateTime).TotalMilliseconds; // sp.ElapsedMilliseconds;
    //                            result.Error = "No Confidence";
    //                            RegArray.PlateNo = "No_Detection";
    //                            RegArray.term = (long)(DateTime.Now - dateTime).TotalMilliseconds; // sp.ElapsedMilliseconds;
    //                            break;
    //                        }
    //                        else
    //                        {
    //                            //Console.WriteLine("    Confidence : " + confidence.ToString());
    //                            result.Confidence[i] = (int)(confidence * 100);
    //                        }
    //                        // Get detection position.
    //                        //rc = Evo.DIContext.GetPos(dic, i, out x, out y, out width, out height);
    //                        Evo.Rect bbox;

    //                        // Get detection position.
    //                        rc = Evo.LPI.GetPosition(ctxLPI, i, out bbox);
    //                        if (rc != 0)
    //                        {
    //                            result.CarNo[i] = "No_Detection";
    //                            result.Term[i] = (long)(DateTime.Now - dateTime).TotalMilliseconds; // sp.ElapsedMilliseconds;
    //                            result.Error = "No Pos";
    //                            RegArray.PlateNo = "No_Detection";
    //                            RegArray.term = (long)(DateTime.Now - dateTime).TotalMilliseconds; // sp.ElapsedMilliseconds;
    //                            break;
    //                        }
    //                        else
    //                        {
    //                            //Console.WriteLine("    Position   : X({0}), Y({1}), Width({2}), Height({3})", x, y, width, height);
    //                            result.rect[i].X = bbox.x;
    //                            result.rect[i].Y = bbox.y;
    //                            result.rect[i].Width = bbox.width;
    //                            result.rect[i].Height = bbox.height;
    //                            RegArray.PlateRoi = string.Format("{0},{1},{2},{3}", bbox.x, bbox.y, bbox.width, bbox.height);
    //                            RegArray.term = (long)(DateTime.Now - dateTime).TotalMilliseconds; // sp.ElapsedMilliseconds;
    //                        }
    //                        // Get detection string.
    //                        StringBuilder strBuf = new StringBuilder(125);
    //                        rc = Evo.LPI.GetString(ctxLPI, i, strBuf);
    //                        //sp.Stop();
    //                        if (rc != 0)
    //                        {
    //                            result.CarNo[i] = "No_Detection";
    //                            result.Term[i] = (long)(DateTime.Now - dateTime).TotalMilliseconds; // sp.ElapsedMilliseconds;
    //                            RegArray.PlateNo = "No_Detection";
    //                            RegArray.term = (long)(DateTime.Now - dateTime).TotalMilliseconds; // sp.ElapsedMilliseconds;
    //                            break;
    //                        }
    //                        else
    //                        {
    //                            Util.Logger.Log("    String     : " + strBuf);
    //                            result.CarNo[i] = strBuf.ToString();
    //                            result.Term[i] = (long)(DateTime.Now - dateTime).TotalMilliseconds; // sp.ElapsedMilliseconds;
    //                            RegArray.PlateNo = strBuf.ToString();
    //                            RegArray.term = (long)(DateTime.Now - dateTime).TotalMilliseconds; // sp.ElapsedMilliseconds;
    //                        }
    //                        //Console.WriteLine("-------------------------------------------------------");
    //                    }

    //                    if (rc != 0)
    //                    {
    //                        Util.Logger.Log("\n\n\nEvo.DIContext.GetXXX() failed... : " + rc.ToString());
    //                        result.Error = "Evo.DIContext.GetXXX() failed... : " + rc.ToString();
    //                    }
    //                    //Console.ReadKey();
    //                }
    //            }
    //            else
    //            {
    //                RegArray.PlateNo = "No_Detection";
    //                RegArray.PlateRoi = string.Format("{0},{1},{2},{3}", 0, 0, 0, 0);
    //                RegArray.term = (long)(DateTime.Now - dateTime).TotalMilliseconds; // sp.ElapsedMilliseconds;
    //            }
    //            // Free the context.
    //            //Evo.SnapshotEngine.FreeDIC(handSSE[Camindex], ref dic);
    //            Evo.LPI.FreeContext(ref ctxLPI);
    //            // Finalize the allocated engine.
    //            //Evo.SnapshotEngine.Deinitialize(handSSE);
    //            //Evo.SnapshotEngine.Release(ref handSSE);
    //            if (Camindex == 0)
    //                clsThread.RegArray1[idx] = RegArray;
    //            else if (Camindex == 1)
    //                clsThread.RegArray2[idx] = RegArray;
    //        }
    //        return result;
    //    }
    //}
    #endregion

    #region 차량 타입 인식 전
    //public static class CoreLogic
    //{
    //    private static int rc; // Return Code
    //    private static uint numDev;
    //    private static UIntPtr dic = (UIntPtr)0; // Detection Information Context
    //    //private static UIntPtr handSSE = (UIntPtr)0; // Handle of Snapshot Engine
    //    private static UIntPtr[] handSSE; // Handle of Snapshot Engine
    //    private static UIntPtr ctxDDI = UIntPtr.Zero; // Context of DNN Device Information
    //    private static object obj = new object();

    //    public static void Init(bool ch2)
    //    {
    //        try
    //        {
    //            int cnt = 1;
    //            Util.Logger.Log("Evo.Setup.Run");
    //            if (ch2)
    //                cnt = 2;
    //            handSSE = new UIntPtr[cnt];
    //            // Initialize Evo LPR Engine with the installation root directory.

    //            rc = Evo.Func.Setup(string.Empty, ref ctxDDI);

    //            if (rc != 0)
    //            {
    //                Util.Logger.Log("Evo.Setup.Run() failed... : " + rc.ToString());
    //                return;
    //            }

    //            #region DeviceInfo
    //            bool[] CPU = new bool[4] { false, false, false, false };
    //            bool[] GPU = new bool[4] { false, false, false, false };
    //            string strDev = "";

    //            // Get total number of available DNN devices.
    //            if (DDI.GetNumDevices(ctxDDI, out numDev) != 0)
    //            {
    //                DDI.FreeContext(ref ctxDDI);
    //                Console.WriteLine("DDI.GetNumDevices() failed... : {0}", rc);
    //                //return;
    //            }
    //            else
    //            {
    //                Console.WriteLine("<< DNN Device Information >>");

    //                for (uint i = 0; i < numDev; i++)
    //                {
    //                    uint numFmt;
    //                    string devInfo;
    //                    StringBuilder dev = new StringBuilder(100);

    //                    // Get name of specific DNN device.
    //                    rc = DDI.GetDevice(ctxDDI, i, dev);
    //                    if (rc != 0)
    //                    {
    //                        DDI.FreeContext(ref ctxDDI);
    //                        Console.WriteLine("DDI.GetDevice() failed... : {0}", rc);
    //                        goto next;
    //                    }

    //                    // Get total number of DNN model formats which are
    //                    // supported by specified DNN device.
    //                    rc = DDI.GetNumFormats(ctxDDI, dev.ToString(), out numFmt);
    //                    if (rc != 0)
    //                    {
    //                        DDI.FreeContext(ref ctxDDI);
    //                        Console.WriteLine("DDI.GetNumFormats() failed... : {0}", rc);
    //                        goto next;
    //                    }

    //                    devInfo = dev.ToString() + " : ";

    //                    // Print all the DNN model format which is supported
    //                    // by the specified DNN device.
    //                    for (uint j = 0; j < numFmt; j++)
    //                    {
    //                        StringBuilder format = new StringBuilder(10);

    //                        rc = DDI.GetFormat(ctxDDI, dev.ToString(), j, format);
    //                        if (rc != 0)
    //                        {
    //                            DDI.FreeContext(ref ctxDDI);
    //                            Console.WriteLine("DDI.GetFormat() failed... : {0}", rc);
    //                            goto next;
    //                        }

    //                        devInfo += format.ToString();
    //                        if (j < numFmt - 1)
    //                            devInfo += ", ";
    //                    }

    //                    Console.WriteLine(devInfo);
    //                    strDev += string.Format("{0}\t", devInfo);
    //                }

    //                Console.WriteLine();

    //            next:
    //                // Destroy the DDI context if no more used.
    //                DDI.FreeContext(ref ctxDDI);
    //            }
    //            string[] sp = strDev.Split('\t');
    //            foreach (string item in sp)
    //            {
    //                if (item.IndexOf("CPU") >=0)
    //                {
    //                    CPU[0] = true;
    //                    CPU[1] = item.IndexOf("FP32") > 0;
    //                    CPU[2] = item.IndexOf("FP16") > 0;
    //                    CPU[3] = item.IndexOf("I8") > 0;
    //                }
    //                else if (item.IndexOf("GPU") >= 0)
    //                {
    //                    GPU[0] = true;
    //                    GPU[1] = item.IndexOf("FP32") > 0;
    //                    GPU[2] = item.IndexOf("FP16") > 0;
    //                    GPU[3] = item.IndexOf("I8") > 0;
    //                }
    //            }

    //            if (!GPU[0])
    //            {
    //                Util.Logger.Log("★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★");
    //                Util.Logger.Log("★===========================================================================================================================================================================================================================================================★");
    //                Util.Logger.Log("★그래픽 드라이버에 문제가 있습니다. 드라이버를 재 설치 하세요!! 드라이버를 삭제 할 수 없을 때 에는 장치 관리자 => 디스플레이 어댑터 => 인텔그레픽 드라이버 => 우클릭 => 디바이스 제거 => ★이 장치의 드라이버 소프트웨어를 삭제합니다.(선택)★ => 확인 후 그레픽 드라이버를 다시 설치 하세요★");
    //                Util.Logger.Log("★===========================================================================================================================================================================================================================================================★");
    //                Util.Logger.Log("★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★");
    //            }

    //            string strinit = "";

    //            if (!ch2)
    //            {
    //                if (GPU[0])
    //                {
    //                    if (GPU[1])
    //                        strinit = "FP32:GPU";
    //                    else if (GPU[2])
    //                        strinit = "FP16:GPU";
    //                }
    //                else if (CPU[0])
    //                {
    //                    if (CPU[1])
    //                        strinit = "FP32:CPU";
    //                    else if (CPU[2])
    //                        strinit = "FP16:CPU";
    //                }
    //            }
    //            else
    //            {
    //                if (CPU[0] && GPU[0] && CPU[1] && GPU[1])
    //                {
    //                    strinit = "FP32:GPU,CPU";
    //                }
    //                else if (CPU[0] && GPU[0] && CPU[2] && GPU[2])
    //                {
    //                    strinit = "FP16:GPU,CPU";
    //                }
    //                else if (GPU[0] && GPU[1])
    //                {
    //                    strinit = "FP32:GPU";
    //                }
    //                else if (CPU[0] && CPU[1])
    //                {
    //                    strinit = "FP32:CPU";
    //                }
    //            }
    //            Util.Logger.Log(string.Format("Initailize 설정 장비 {0}", strinit));
    //            #endregion
    //            for (int i = 0; i < cnt; i++)
    //            {

    //                Util.Logger.Log("SnapshotEngine.Allocate()");
    //                // Allocate a Snapshot Engine.
    //                //rc = Evo.SnapshotEngine.Allocate(ref handSSE[i]);
    //                handSSE[i] = Evo.SnapshotEngine.Allocate();
    //                rc = Evo.Func.GetLastRC();
    //                if (rc != 0)
    //                {
    //                    Util.Logger.Log("SnapshotEngine.Allocate() failed... : " + rc.ToString());
    //                    return;
    //                }
    //                Util.Logger.Log("SnapshotEngine.Initialize()");
    //                // Initialize the allocated engine.
    //                // Available 'dtd' values: 0(CPU), 1(GPU), 2(Myriad VPU)
    //                rc = Evo.SnapshotEngine.Initialize(handSSE[i], strinit);
    //                if (rc != 0)
    //                {
    //                    Util.Logger.Log("SnapshotEngine.Initialize() failed... : " + rc.ToString());
    //                    Evo.SnapshotEngine.Release(ref handSSE[i]);
    //                    return;
    //                }
    //            }
    //            Util.Logger.Log("Initialize 완료");
    //        }
    //        catch (Exception)
    //        { }
    //        return;
    //    }

    //    public static void Reg(int Camindex, int idx)
    //    {
    //        Util.Logger.Log("Reg Start");
    //        Thread t = new Thread(delegate ()
    //        {
    //            //RegPlate(Listidx, CamIdx, FilePath, ROI);
    //            RegPlateNo(Camindex, idx);
    //        });
    //        t.IsBackground = true;
    //        t.Start();
    //    }

    //    public static Result RegPlateNo(int Camindex, int idx)
    //    {
    //        Result result = new Result();
    //        try
    //        {
    //            Util.Logger.Log("RegPlateNo Start");
    //            //Stopwatch sp = new Stopwatch();
    //            DateTime dateTime = new DateTime();
    //            ClsStructure.RegStruct RegArray = new ClsStructure.RegStruct();
    //            //UIntPtr dic = (UIntPtr)0;
    //            Rectangle rectangle = new Rectangle();
    //            switch (Camindex)
    //            {
    //                case 0:
    //                    RegArray = clsThread.RegArray1[idx];
    //                    rectangle = frmLprMain.ENV.CameraEnv.IPCamera1Info.Roi;
    //                    break;
    //                case 1:
    //                    RegArray = clsThread.RegArray2[idx];
    //                    rectangle = frmLprMain.ENV.CameraEnv.IPCamera2Info.Roi;
    //                    break;
    //            }
    //            Util.Logger.Log(string.Format("Cam Idx {0} image idx {1} image path {2}", Camindex, idx, RegArray.SourcePath));
    //            //sp.Start();
    //            dateTime = DateTime.Now;
    //            lock (obj)
    //            {
    //                //dic = (UIntPtr)0; //재호출 시 0 설정 해야됨
    //                // Detect license plates and get Detection Information Context if any.
    //                //rc = Evo.SnapshotEngine.Detect(handSSE[Camindex], RegArray.SourcePath, 0.1F, ref dic);
    //                const float minConf = 2.5F; // Recommend 2.5% as minimum detection confidence.
    //                UIntPtr ctxLPI = UIntPtr.Zero; // Context of LPI.
    //                Util.Logger.Log(string.Format("인식 영역 설정 X : {0} Y :{1} W : {2} H : {3}", rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height));
    //                //Evo.Rect validRegion = new Evo.Rect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height); // All over the image.
    //                Evo.Rect validRegion = new Evo.Rect();
    //                validRegion.x = rectangle.X;
    //                validRegion.y = rectangle.Y;
    //                validRegion.width = rectangle.Width;
    //                validRegion.height = rectangle.Height;
    //                if (RegArray.SourcePath.ToLower().EndsWith(".bmp"))
    //                {
    //                    Bitmap bm = new Bitmap(RegArray.SourcePath);

    //                    // !!! Currently support only RGB24 pixel format. !!!
    //                    //rc = Evo.SnapshotEngine.Detect(handSSE[Camindex], bm, ref validRegion, minConf, ref ctxLPI);
    //                    ctxLPI = Evo.SnapshotEngine.Detect(handSSE[Camindex], bm, ref validRegion);
    //                    rc = Evo.Func.GetLastRC();
    //                }
    //                else
    //                {
    //                    //rc = Evo.SnapshotEngine.Detect(handSSE[Camindex], RegArray.SourcePath, ref validRegion, minConf, ref ctxLPI);
    //                    ctxLPI = Evo.SnapshotEngine.Detect(handSSE[Camindex], RegArray.SourcePath, ref validRegion);
    //                    rc = Evo.Func.GetLastRC();
    //                }
    //                if (rc != 0)
    //                {
    //                    Util.Logger.Log("Evo.SnapshotEngine.Detect() failed... : " + rc.ToString());
    //                    //Console.ReadKey();

    //                    //Evo.SnapshotEngine.Deinitialize(handSSE);
    //                    //Evo.SnapshotEngine.Release(ref handSSE);
    //                    result.Error = "Evo.SnapshotEngine.Detect() failed... : " + rc.ToString();
    //                    return result;
    //                }

    //                // In case there are detected ones.
    //                if (ctxLPI != UIntPtr.Zero)
    //                {
    //                    uint num;
    //                    // Get total number of the detected ones.
    //                    rc = Evo.LPI.GetNumber(ctxLPI, out num);
    //                    if (rc != 0)
    //                    {
    //                        Util.Logger.Log("Evo.LPI.GetNumber() failed... : " + rc);
    //                        //Console.ReadKey();gine.Release(ref handSSE);
    //                        result.Error = "Evo.LPI.GetNumber() failed... : " + rc;
    //                    }
    //                    else if (num > 0)
    //                    {
    //                        //Console.WriteLine("=======================================================");
    //                        //Console.WriteLine("Detection Information of '{0}'", RegArray.SourcePath);
    //                        //Console.WriteLine("-------------------------------------------------------");
    //                        //result.CarNo = new string[num];
    //                        //result.rect = new Rectangle[num];
    //                        //result.Term = new long[num];
    //                        //result.Confidence = new int[num];
    //                        result.rect = new Rectangle[num];
    //                        for (uint i = 0; i < num; i++)
    //                        {
    //                            //result.rect[i] = new Rectangle();
    //                            int type;
    //                            Evo.Rect bbox;
    //                            float confidence;
    //                            StringBuilder strBuf;
    //                            //int x, y, width, height;
    //                            //byte[] strBuf = new byte[1024];

    //                            Console.WriteLine("[{0}]", i);
    //                            // Get type of license plate.
    //                            rc = Evo.LPI.GetType(ctxLPI, i, out type, out confidence);
    //                            if (rc != 0)
    //                            {
    //                                Console.WriteLine("Evo.LPI.GetType() failed... : {0}", rc);
    //                                Util.Logger.Log("Evo.LPI.GetType() failed... : " + rc);
    //                                break;
    //                            }
    //                            else
    //                            {
    //                                //
    //                                // Values of License Plate Types
    //                                //
    //                                //    10 : Legacy Type
    //                                //    20 : Normal Type
    //                                //    30 : Electric-Vehicle Type
    //                                //    40 : Construction-Vehicle Type
    //                                //    50 : Temporary Type
    //                                //
    //                                Console.WriteLine("    Type       : {0} ({1}%)", type, confidence);
    //                            }

    //                            // Get bounding box.
    //                            rc = Evo.LPI.GetPosition(ctxLPI, i, out bbox);
    //                            if (rc != 0)
    //                            {
    //                                Console.WriteLine("Evo.LPI.GetPosition() failed... : {0}", rc);
    //                                Util.Logger.Log("Evo.LPI.GetPosition() failed... : " + rc);
    //                                break;
    //                            }
    //                            else
    //                            {
    //                                Console.WriteLine("    Position   : X({0}), Y({1}), Width({2}), Height({3})", bbox.x, bbox.y, bbox.width, bbox.height);
    //                            }

    //                            // Get text string.
    //                            strBuf = new StringBuilder(125);
    //                            rc = Evo.LPI.GetString(ctxLPI, i, strBuf);
    //                            if (rc != 0)
    //                            {
    //                                Console.WriteLine("Evo.LPI.GetString() failed... : {0}", rc);
    //                                Util.Logger.Log("Evo.LPI.GetString() failed... : " + rc);
    //                                result.CarNo[i] = "No_Detection";
    //                                result.Term[i] = (long)(DateTime.Now - dateTime).TotalMilliseconds; // sp.ElapsedMilliseconds;
    //                                result.Error = "No Pos";
    //                                RegArray.PlateNo = "No_Detection";
    //                                RegArray.term = (long)(DateTime.Now - dateTime).TotalMilliseconds; // sp.ElapsedMilliseconds;
    //                                break;
    //                            }
    //                            else
    //                            {
    //                                Console.WriteLine("    String     : " + strBuf.ToString());
    //                                result.rect[i].X = bbox.x;
    //                                result.rect[i].Y = bbox.y;
    //                                result.rect[i].Width = bbox.width;
    //                                result.rect[i].Height = bbox.height;
    //                                RegArray.PlateRoi = string.Format("{0},{1},{2},{3}", bbox.x, bbox.y, bbox.width, bbox.height);
    //                                RegArray.term = (long)(DateTime.Now - dateTime).TotalMilliseconds; // sp.ElapsedMilliseconds;
    //                                RegArray.PlateNo = strBuf.ToString();
    //                            }
    //                        }

    //                        if (rc != 0)
    //                        {
    //                            Util.Logger.Log("Evo.DIContext.GetXXX() failed... : " + rc.ToString());
    //                            result.Error = "Evo.DIContext.GetXXX() failed... : " + rc.ToString();
    //                        }
    //                        //Console.ReadKey();
    //                    }
    //                }
    //                else
    //                {
    //                    RegArray.PlateNo = "No_Detection";
    //                    RegArray.PlateRoi = string.Format("{0},{1},{2},{3}", 0, 0, 0, 0);
    //                    RegArray.term = (long)(DateTime.Now - dateTime).TotalMilliseconds; // sp.ElapsedMilliseconds;
    //                }
    //                // Free the context.
    //                //Evo.SnapshotEngine.FreeDIC(handSSE[Camindex], ref dic);
    //                Evo.LPI.FreeContext(ref ctxLPI);
    //                // Finalize the allocated engine.
    //                //Evo.SnapshotEngine.Deinitialize(handSSE);
    //                //Evo.SnapshotEngine.Release(ref handSSE);
    //                if (Camindex == 0)
    //                    clsThread.RegArray1[idx] = RegArray;
    //                else if (Camindex == 1)
    //                    clsThread.RegArray2[idx] = RegArray;
    //                Util.Logger.Log(string.Format("인식 결과 : {0} {1}", RegArray.PlateNo, RegArray.PlateRoi));
    //            }
    //        }
    //        catch(Exception ex)
    //        {

    //        }
    //        return result;
    //    }
    //}
    #endregion

    public static class CoreLogic {
        // [+]1. Initialize the Evo engine library.
        private static uint numDev;
        public static int cc = 410; // Country code for South Korea.
        private static int ctxDDI = -1; // Context of DDI.
        private static int[] handSSE = new int[2] { -1, -1 }; // Handle of Snapshot Engine
        private static int rc;
        private static object obj = new object();
        public const int KOR = 410;
        public const int THA = 764;

        /// <summary>EvoEngine Data 경로 결정: EXE 옆 Data 폴더 우선, 없으면 SDK 설치 경로</summary>
        private static string GetEvoDataDir() {
            string local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if(Directory.Exists(local) && File.Exists(Path.Combine(local, "EvoEngine.lic")))
                return local;
            return @"C:\Program Files\EvoEngineSDK\Data";
        }

        /// <summary>
        /// 환경설정의 CoreType → Evo SDK 디바이스 문자열.
        /// 형식: "mode(quality):device1,device2,..." — CoreLogic 박선임 안내.
        /// GPU/MYRIAD 우선 + CPU 폴백을 SDK 자체에 위임. CPU 선택 시 null(SDK 기본).
        /// </summary>
        private static string GetEvoDeviceString() {
            try {
                int ct = frmLprMain.ENV.CameraEnv.CoreType;
                switch ((ClsStructure.CoreType)ct) {
                    case ClsStructure.CoreType.GPU: return "normal(accuracy):GPU,CPU";
                    case ClsStructure.CoreType.MyriadVPU: return "normal(accuracy):MYRIAD,CPU";
                    case ClsStructure.CoreType.CPU:
                    default: return null;  // 변경 전과 동일 (SDK 기본 = CPU)
                }
            }
            catch { return null; }
        }

        /// <summary>마지막으로 적용된 디바이스 (null = SDK 기본 = CPU)</summary>
        public static string LastAppliedDevice { get; private set; } = null;

        /// <summary>FAVEngine 모드용 - 라이선스 전역 초기화만 수행 (SSEngine 생성 안 함)</summary>
        public static bool InitializeForFAVE() {
            if(cc == 0) cc = KOR;
            string dataDir = GetEvoDataDir();
            Util.Logger.Log("EvoDataDir: " + dataDir);

            // Func.Initialize는 박선임 안내에서 언급 없음 → null (SDK 기본). FAVEngine.Init에서 ddd 적용.
            rc = Evo.Func.Initialize(null, dataDir, out ctxDDI);
            if(rc != 0) {
                Util.Logger.Log("Evo.Func.Initialize() failed: " + rc);
                return false;
            }
            LastAppliedDevice = GetEvoDeviceString();
            Util.Logger.Log("InitializeForFAVE OK — ddd 예정=" + (LastAppliedDevice ?? "(SDK기본)"));
            Evo.Func.FreeObj(ref ctxDDI);
            return true;
        }

        public static bool Initialize() {
            try {
                // 1. Initialize the Evo engine library. Optionally can get DDI context.
                if(cc == 0)
                    cc = KOR;
                //leess 6.x 모듈변경
                string language = "KOR";
                if(cc == THA) language = "THA";

                // Func.Initialize는 박선임 안내에서 언급 없음 → null 유지 (변경 전과 동일).
                // 디바이스 우선순위는 SSEngine.Init의 ddd 인자에서 처리.
                rc = Evo.Func.Initialize(null, GetEvoDataDir(), out ctxDDI);
                if(rc != 0) {
                    Console.WriteLine("Evo.Func.Initialize() failed. : {0}", rc);
                    ShowEvoEngineError("Evo.Func.Initialize() 반환 코드 " + rc + " (정상 실행 시 0).", null);
                    return false;
                }
                // GetEvoDeviceString의 결과를 SSEngine/FAVEngine.Init 에 전달할 수 있도록 보관
                LastAppliedDevice = GetEvoDeviceString();

                // DDI 디바이스 목록 — SDK가 실제로 인식하는 디바이스 ID/Name을 로그에 기록.
                // GPU/MYRIAD 활성화 시 정확한 문자열은 여기 출력된 ID 사용.
                rc = Evo.DDI.GetNumDev(ctxDDI, out numDev);
                Util.Logger.Log("=== Evo DDI 디바이스 목록 (numDev=" + numDev + ") ===");
                for(uint i = 0; i < numDev; i++) {
                    StringBuilder devId = new StringBuilder(128);
                    StringBuilder devName = new StringBuilder(128);
                    rc = Evo.DDI.SelectDev(ctxDDI, i);
                    rc = Evo.DDI.GetDevID(ctxDDI, devId);
                    rc = Evo.DDI.GetFullName(ctxDDI, devName);
                    Util.Logger.Log(string.Format("  [{0}] ID='{1}'  Name='{2}'", i, devId, devName));
                }
                Util.Logger.Log("=== /Evo DDI ===");

                Evo.Func.FreeObj(ref ctxDDI);
                // [-]
                // 2. Allocate a new snapshot engine.
                for(int i = 0; i < 2; i++) {
                    //leess 6.x 모듈변경
                    //handSSE[i] = Evo.SSEngine.Allocate();
                    handSSE[i] = Evo.SSEngine.Create();
                    if(handSSE[i] < 0) {
                        var lastRc = Evo.Func.GetLastRC();
                        Console.WriteLine("SnapshotEngine.Create() failed. : {0}", lastRc);
                        ShowEvoEngineError("SSEngine.Create() 실패. 반환 코드: " + lastRc, null);
                        return false;
                    }

                    // 3. Initialize the engine.
                    // ddd 형식: "normal(accuracy):GPU,CPU" (CoreLogic 박선임 안내).
                    // null이면 SDK 기본 = CPU (변경 전과 동일).
                    Util.Logger.Log("SSEngine.Init idx=" + i + " ddd=" + (LastAppliedDevice ?? "(null=SDK기본)"));
                    rc = Evo.SSEngine.Init(handSSE[i], language, LastAppliedDevice);
                    if(rc != 0 && LastAppliedDevice != null) {
                        Util.Logger.Log("SSEngine.Init idx=" + i + " ddd=" + LastAppliedDevice + " 실패 rc=" + rc + " → null 재시도");
                        rc = Evo.SSEngine.Init(handSSE[i], language, null);
                        if(rc == 0) LastAppliedDevice = null;
                    }
                    if(rc != 0) {
                        Console.WriteLine("SnapshotEngine.Allocate() failed. : {0}", rc);
                        ShowEvoEngineError("SSEngine.Init() 실패. 반환 코드: " + rc, null);
                        return false;
                    }
                    Util.Logger.Log("SSEngine.Init OK idx=" + i + " 적용 ddd=" + (LastAppliedDevice ?? "(SDK기본)"));
                }
                return true;
            }
            catch(DllNotFoundException ex) {
                // HRESULT 0x8007007F (ERROR_PROC_NOT_FOUND):
                //   DLL 파일은 찾았지만 그 안에 필요한 함수(프로시저)가 없음
                //   → SDK 버전 불일치(옛 버전 EvoEngineSDK 등)가 가장 흔한 원인
                bool isProcNotFound = (ex.HResult == unchecked((int)0x8007007F)) ||
                                      (ex.Message != null && (ex.Message.Contains("프로시저") || ex.Message.Contains("procedure")));
                string headline = isProcNotFound
                    ? "DLL 파일은 찾았지만 그 안의 함수(프로시저)를 찾지 못했습니다.\n  → EvoEngineSDK 버전이 옛 버전이거나, 코드와 DLL의 시그니처가 일치하지 않습니다."
                    : "필수 DLL 파일을 찾지 못했습니다. (DllNotFoundException)";
                ShowEvoEngineError(headline, ex, isProcNotFound);
                return false;
            }
            catch(EntryPointNotFoundException ex) {
                ShowEvoEngineError("DLL의 함수 엔트리포인트를 찾지 못했습니다.\n  → EvoEngineSDK 버전이 옛 버전이거나, P/Invoke 시그니처가 일치하지 않습니다.", ex, true);
                return false;
            }
            catch(BadImageFormatException ex) {
                ShowEvoEngineError("DLL의 비트(x86/x64)가 실행 파일과 일치하지 않습니다. (BadImageFormatException)\n\nKyungsinLPR는 64비트(x64) 빌드이므로 Evo*.dll도 64비트여야 합니다.", ex, false);
                return false;
            }
            catch(System.IO.FileNotFoundException ex) {
                ShowEvoEngineError("파일을 찾을 수 없습니다. (FileNotFoundException)", ex, false);
                return false;
            }
            catch(Exception ex) {
                ShowEvoEngineError("예상치 못한 예외가 발생했습니다. (" + ex.GetType().Name + ")", ex, false);
                return false;
            }
        }

        /// <summary>Evo 엔진 초기화 실패 시 사용자에게 상세 진단 메시지박스 표시</summary>
        /// <param name="isVersionMismatch">true면 SDK 버전 불일치 의심 — 해결 방법 안내를 그쪽으로 강조</param>
        private static void ShowEvoEngineError(string headline, Exception ex, bool isVersionMismatch = false) {
            try {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] requiredDlls = {
                    "EvoCAPI.dll", "EvoCSAPI.dll", "EvoCC.dll", "EvoEngine.dll",
                    "EvoFAVE.dll", "EvoGIC.dll", "EvoSSE.dll", "EvoTSSE.dll", "EvoUtils.dll"
                };
                var sb = new StringBuilder();
                sb.AppendLine("■ LPR 차번인식 엔진(Evo) 초기화 실패");
                sb.AppendLine();
                sb.AppendLine("증상: " + headline);
                if(ex != null) {
                    sb.AppendLine();
                    sb.AppendLine("예외 메시지: " + ex.Message);
                    if(ex.HResult != 0)
                        sb.AppendLine(string.Format("HRESULT: 0x{0:X8}", ex.HResult));
                }
                sb.AppendLine();
                sb.AppendLine("실행 경로:");
                sb.AppendLine("  " + baseDir);
                sb.AppendLine();
                sb.AppendLine("DLL 점검 결과:");
                int missing = 0;
                int present = 0;
                foreach(var dll in requiredDlls) {
                    string path = System.IO.Path.Combine(baseDir, dll);
                    bool exists = System.IO.File.Exists(path);
                    if(!exists) missing++; else present++;
                    string ver = "";
                    if(exists) {
                        try {
                            var vi = System.Diagnostics.FileVersionInfo.GetVersionInfo(path);
                            if(!string.IsNullOrEmpty(vi.FileVersion))
                                ver = "  v" + vi.FileVersion;
                        } catch { }
                    }
                    sb.AppendLine("  [" + (exists ? "OK  " : "누락") + "] " + dll + ver);
                }
                sb.AppendLine();
                sb.AppendLine("Evo 데이터 폴더 (DDI):");
                string ddi = GetEvoDataDir();
                sb.AppendLine("  " + (string.IsNullOrEmpty(ddi) ? "(미설정)" : ddi));
                if(!string.IsNullOrEmpty(ddi))
                    sb.AppendLine("  존재 여부: " + (System.IO.Directory.Exists(ddi) ? "OK" : "없음"));
                sb.AppendLine();
                sb.AppendLine("─────────────────────────");
                sb.AppendLine("해결 방법:");
                if(missing > 0) {
                    // 1순위: DLL 누락
                    sb.AppendLine("  → 누락된 DLL " + missing + "개를 실행 폴더에 복사 후 재실행");
                    sb.AppendLine("  → bin\\x64\\Debug 또는 EvoEngineSDK 폴더에서 가져올 수 있습니다");
                } else if(isVersionMismatch) {
                    // 1순위: SDK 버전 불일치 (DLL 모두 있는데 함수가 없음)
                    sb.AppendLine("  ★ EvoEngineSDK 버전 불일치가 가장 유력한 원인입니다.");
                    sb.AppendLine();
                    sb.AppendLine("  1. 최신 EvoEngineSDK 패키지를 받아 모든 Evo*.dll 교체");
                    sb.AppendLine("     - EvoCAPI.dll, EvoCSAPI.dll 뿐 아니라 EvoEngine/EvoCC/EvoFAVE/EvoGIC/EvoSSE/EvoTSSE/EvoUtils.dll");
                    sb.AppendLine("       모두 같은 버전 세트로 교체해야 함");
                    sb.AppendLine("  2. EvoCSAPI.dll(.NET 래퍼)의 P/Invoke 시그니처가");
                    sb.AppendLine("     현재 EvoCAPI.dll 버전과 맞는지 확인");
                    sb.AppendLine("  3. EvoEngineSDK 폴더의 데이터(DDI) 파일도 같이 교체 필요할 수 있음");
                    sb.AppendLine();
                    sb.AppendLine("  ※ DLL 파일은 모두 있어도 \"옛 버전\"이면 새 함수를 못 찾아");
                    sb.AppendLine("     0x8007007F (ERROR_PROC_NOT_FOUND) 오류가 발생합니다.");
                } else {
                    sb.AppendLine("  1. Visual C++ 재배포 패키지 (x64) 설치 확인");
                    sb.AppendLine("     https://aka.ms/vs/17/release/vc_redist.x64.exe");
                    sb.AppendLine("  2. DLL의 종속 라이브러리가 모두 설치되어 있는지 확인");
                    sb.AppendLine("  3. EvoCAPI.dll 파일이 손상되지 않았는지 재배포로 교체");
                }
                System.Windows.Forms.MessageBox.Show(sb.ToString(),
                    "LPR 엔진 초기화 실패",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
            catch { /* 메시지박스 자체가 실패하면 무시 */ }
        }

        //public static string[] Reg(int idx, string imgPath)
        //{
        //    string[] rtn = new string[3] { "", "", "" };

        //    // [+] 4. If required, set or get engine parameters of interest.
        //    Evo.Rect searchRect = new Evo.Rect();
        //    rc = Evo.Func.GetParamSearchRect(handSSE[idx], out searchRect);
        //    Console.WriteLine(rc == 0);
        //    //searchRect.x = 0;
        //    //searchRect.y = 0;
        //    searchRect.width = 0;
        //    searchRect.height = 0;
        //    //Output output = new Output();
        //    //output.enAmType = 1;
        //    rc = Evo.Func.SetParamSearchRect(handSSE[idx], in searchRect);
        //    Console.WriteLine(rc == 0);

        //    // [-]
        //    //
        //    // Repeat the stpes 5-7 if there are more than two images.
        //    //
        //    string inputSource = "ImageFile";
        //    //string inputSource = "EncodedImage";
        //    UIntPtr ctxLPI = UIntPtr.Zero; // Context of License Plate Information.
        //                                   // 5. Run the engine with input image.
        //    if (inputSource == "ImageFile")
        //        ctxLPI = Evo.SnapshotEngine.Run(handSSE[idx], imgPath);
        //    else // "EncodedImage"
        //        ctxLPI = Evo.SnapshotEngine.Run(handSSE[idx], new Bitmap(imgPath));
        //    // Get the return code.
        //    rc = Evo.Func.GetLastRC();
        //    if (rc != 0)
        //    {
        //        Console.WriteLine("Evo.SnapshotEngine.Run('{0}') failed. : {1}", imgPath, rc);
        //    }
        //    else if (ctxLPI != UIntPtr.Zero)
        //    {
        //        uint num;
        //        //
        //        // [+] 6. Examine the LPI context.
        //        //
        //        // Get total number of the detected license plates.
        //        rc = Evo.LPI.GetNumber(ctxLPI, out num);
        //        Console.WriteLine(rc == 0);

        //        for (uint i = 0; i < num; i++)
        //        {
        //            Evo.Rect bbox;
        //            int type;
        //            float confidence;
        //            StringBuilder strBuf = new StringBuilder(512);
        //            // Get text string.
        //            rc = Evo.LPI.GetString(ctxLPI, i, strBuf);
        //            Console.WriteLine(rc == 0);
        //            Console.WriteLine("\n{0}", strBuf.ToString());
        //            if (rtn[0] != strBuf.ToString())
        //            {
        //                if (rtn[0].Length > 0) rtn[0] += ",";
        //                rtn[0] = strBuf.ToString();
        //            }
        //            // Get the bounding box.
        //            rc = Evo.LPI.GetPosition(ctxLPI, i, out bbox);
        //            Console.WriteLine(rc == 0);
        //            Console.WriteLine(" --> Position: {0}, {1}, {2}, {3}", bbox.x, bbox.y, bbox.width, bbox.height);
        //            // Get the type and its confidence.
        //            rc = Evo.LPI.GetType(ctxLPI, i, out type, out confidence);
        //            Console.WriteLine(rc == 0);
        //            Console.WriteLine(" --> Type: {0} ({1}%)", type, confidence);
        //            rc = Evo.LPI.GetAmType(ctxLPI, i, strBuf, out confidence);
        //            Console.WriteLine(rc == 0);
        //            Console.WriteLine("\n{0}", strBuf.ToString());
        //            Console.WriteLine(string.Format(" --> confidence: {0} ({1}%)", type, confidence));
        //            if (rtn[1] != strBuf.ToString())
        //            {
        //                if (rtn[1].Length > 0) rtn[1] += ",";
        //                rtn[1] = strBuf.ToString();
        //                rtn[2] = confidence.ToString();
        //            }
        //        }
        //        // [-]
        //        // 7. Destroy the LPI context if no more used.
        //        Evo.LPI.FreeContext(ref ctxLPI);
        //    }
        //    return rtn;
        //}

        public static void Reg(int Camindex, int idx, bool bRegCarType) {
            Util.Logger.Log("Reg Start");
            Thread t = new Thread(delegate () {
                //RegPlate(Listidx, CamIdx, FilePath, ROI);
                RegPlateNo(Camindex, idx, bRegCarType);
            });
            t.IsBackground = true;
            t.Start();
        }

        public static Result RegPlateNo(int Camindex, int idx, bool bRegCarType) {
            Result result = new Result();
            try {
                Util.Logger.Log("RegPlateNo Start");
                //Stopwatch sp = new Stopwatch();
                DateTime dateTime = new DateTime();
                ClsStructure.RegStruct RegArray = new ClsStructure.RegStruct();
                //UIntPtr dic = (UIntPtr)0;
                //Rectangle rectangle = new Rectangle();

                // [+] 4. If required, set or get engine parameters of interest.
                Evo.Rect searchRect = new Evo.Rect();
                //leess 6.x 모듈변경
                //rc = Evo.Func.GetParamSearchRect(handSSE[idx], out searchRect);
                rc = Evo.Engine.GetParamSearchRect(handSSE[idx], out searchRect);

                switch(Camindex) {
                    case 0:
                        RegArray = clsThread.RegArray1[idx];
                        //rectangle = frmLprMain.ENV.CameraEnv.IPCamera1Info.Roi;
                        searchRect.x = frmLprMain.ENV.CameraEnv.IPCamera1Info.Roi.X;
                        searchRect.y = frmLprMain.ENV.CameraEnv.IPCamera1Info.Roi.Y;
                        searchRect.width = frmLprMain.ENV.CameraEnv.IPCamera1Info.Roi.Width;
                        searchRect.height = frmLprMain.ENV.CameraEnv.IPCamera1Info.Roi.Height;
                        //leess 이미지사이즈 추가 : 설정좌표보다 이미지가 작을 경우 아예 인식동작이 안하는것 방지
                        //if(searchRect.x + searchRect.width > RegArray.imgWidth) {
                        //    searchRect.width = RegArray.imgWidth - searchRect.x - 4;
                        //}
                        //if(searchRect.y + searchRect.height > RegArray.imgHeight) {
                        //    searchRect.height = RegArray.imgHeight - searchRect.y - 4;
                        //}
                        break;
                    case 1:
                        RegArray = clsThread.RegArray2[idx];
                        //rectangle = frmLprMain.ENV.CameraEnv.IPCamera2Info.Roi;
                        searchRect.x = frmLprMain.ENV.CameraEnv.IPCamera2Info.Roi.X;
                        searchRect.y = frmLprMain.ENV.CameraEnv.IPCamera2Info.Roi.Y;
                        searchRect.width = frmLprMain.ENV.CameraEnv.IPCamera2Info.Roi.Width;
                        searchRect.height = frmLprMain.ENV.CameraEnv.IPCamera2Info.Roi.Height;
                        //leess 이미지사이즈 추가 : 설정좌표보다 이미지가 작을 경우 아예 인식동작이 안하는것 방지
                        //if(searchRect.x + searchRect.width > RegArray.imgWidth) {
                        //    searchRect.width = RegArray.imgWidth - searchRect.x - 4;
                        //}
                        //if(searchRect.y + searchRect.height > RegArray.imgHeight) {
                        //    searchRect.height = RegArray.imgHeight - searchRect.y - 4;
                        //}
                        break;
                }
                RegArray.PlateNo = "";
                RegArray.CarType = "";

                //searchRect.x = 0;
                //searchRect.y = 0;
                //leess 20221104 영역설정 안먹히는게 이것 때문이엇나..? width, height가 모두 0으로 설정되어 있었음
                //searchRect.width = 0;
                //searchRect.height = 0;

                rc = Evo.Engine.SetParamSearchRect(handSSE[idx], in searchRect);

                // [-]
                //
                // Repeat the stpes 5 ~ 6 if there are more than two images.
                //

                Util.Logger.Log(string.Format("Cam Idx {0} image idx {1} image path {2}", Camindex, idx, RegArray.SourcePath));
                //sp.Start();
                dateTime = DateTime.Now;
                lock(obj) {
                    //string inputSource = "EncodedImage";
                    int ctxLPI = -1;
                    ctxLPI = Evo.SSEngine.Run(handSSE[idx], RegArray.SourcePath);
                    rc = Evo.Func.GetLastRC();
                    // 인식속도 측정 — 정상/실패 분기 공통. (기존엔 실패 분기에서만 설정되어 정상 인식 시 0ms 표시 버그)
                    RegArray.term = (long)(DateTime.Now - dateTime).TotalMilliseconds;
                    if(ctxLPI >= 0) {
                        uint num;
                        rc = Evo.LPI.GetNumLP(ctxLPI, out num);
                        Console.WriteLine(rc == 0);

                        int maxRegSize = 0;
                        for(uint i = 0; i < num; i++) {
                            Evo.Rect bbox;
                            int type;
                            float confidence;
                            StringBuilder strBuf = new StringBuilder(512);
                            rc = Evo.LPI.SelectLP(ctxLPI, i);
                            rc = Evo.LPI.GetStr(ctxLPI, strBuf);
                            Console.WriteLine(rc == 0);
                            Console.WriteLine("\n{0}", strBuf.ToString());
                            rc = Evo.LPI.GetPos(ctxLPI, out bbox);
                            Console.WriteLine(rc == 0);
                            Console.WriteLine(" --> Position: {0}, {1}, {2}, {3}", bbox.x, bbox.y, bbox.width, bbox.height);
                            rc = Evo.LPI.GetType(ctxLPI, out type, out confidence);
                            Console.WriteLine(rc == 0);
                            Console.WriteLine(" --> Type: {0} ({1}%)", type, confidence);

                            int regSize = bbox.width * bbox.height;
                            if(regSize > maxRegSize) {
                                maxRegSize = regSize;
                                if(RegArray.PlateNo != strBuf.ToString()) {
                                    RegArray.PlateNo = strBuf.ToString();
                                }
                            } else {
                                Util.Logger.Log(string.Format("X 작은 사이즈 인식 결과 버림 : {0}", strBuf.ToString()));
                            }
                        }
                        Evo.Func.FreeObj(ref ctxLPI);
                    } else {
                        Console.WriteLine("Evo.SSEngine.Run('{0}') failed. : {1}", RegArray.SourcePath, rc);
                        RegArray.PlateNo = "No_Detection";
                        RegArray.PlateRoi = string.Format("{0},{1},{2},{3}", 0, 0, 0, 0);
                    }
                    if(Camindex == 0)
                        clsThread.RegArray1[idx] = RegArray;
                    else if(Camindex == 1)
                        clsThread.RegArray2[idx] = RegArray;
                    Util.Logger.Log(string.Format("인식 결과 : {0} {1}", RegArray.PlateNo, RegArray.PlateRoi));
                }
            } catch(Exception ex) {

            }
            return result;
        }

        public static void Release() {
            for(int i = 0; i < 2; i++) {
                Evo.SSEngine.Deinit(handSSE[i]);
                Evo.Func.FreeObj(ref handSSE[i]);
            }
        }

        // ──────────────────────────────────────────────────
        // FAVEngine (동영상 방식) 지원
        // ──────────────────────────────────────────────────

        private static int[] handFAVE = new int[2] { -1, -1 };
        private static Thread[] faveThreads = new Thread[2];
        private static volatile bool[] faveRunning = new bool[2] { false, false };
        private static DateTime[] lastFAVERegTime = new DateTime[2] { DateTime.MinValue, DateTime.MinValue };
        private const int FaveRegIntervalMs = 5000; // 동일 채널 중복 인식 방지 (5초)

        /// <summary>
        /// 지정된 카메라 채널에 FAVEngine을 초기화하고 백그라운드 스레드를 시작합니다.
        /// </summary>
        /// <summary>RTSP URL의 호스트:포트 TCP 연결 가능 여부를 timeoutMs 내에 확인</summary>
        private static bool IsRtspReachable(string rtspUrl, int timeoutMs = 5000) {
            try {
                Uri uri = new Uri(rtspUrl);
                int port = uri.Port > 0 ? uri.Port : 554;
                using(var tcp = new System.Net.Sockets.TcpClient()) {
                    var ar = tcp.BeginConnect(uri.Host, port, null, null);
                    bool ok = ar.AsyncWaitHandle.WaitOne(timeoutMs);
                    if(ok && tcp.Connected) { tcp.EndConnect(ar); return true; }
                }
            } catch { }
            return false;
        }

        public static bool InitFAVE(int camIdx, string rtspUrl) {
            if(string.IsNullOrEmpty(rtspUrl)) {
                Util.Logger.Log(string.Format("FAVEngine CAM{0}: RTSP URL 없음, 동영상 방식 불가", camIdx + 1));
                return false;
            }
            // RTSP 서버 도달 가능 여부 사전 확인 (5초) — 불가 시 Init 블로킹 방지
            Util.Logger.Log(string.Format("FAVEngine CAM{0} RTSP 연결 확인 중: {1}", camIdx + 1, rtspUrl));
            if(!IsRtspReachable(rtspUrl, 5000)) {
                Util.Logger.Log(string.Format("FAVEngine CAM{0} RTSP 연결 불가 (5초 타임아웃): {1}", camIdx + 1, rtspUrl));
                return false;
            }
            Util.Logger.Log(string.Format("FAVEngine CAM{0} RTSP 연결 확인 OK", camIdx + 1));
            string language = (cc == THA) ? "THA" : "KOR";
            try {
                handFAVE[camIdx] = Evo.FAVEngine.Create();
                if(handFAVE[camIdx] < 0) {
                    Util.Logger.Log(string.Format("FAVEngine CAM{0} Create() 실패 rc={1}", camIdx + 1, Evo.Func.GetLastRC()));
                    return false;
                }
                Util.Logger.Log(string.Format("FAVEngine CAM{0} Init ddd={1}", camIdx + 1, LastAppliedDevice ?? "(null=SDK기본)"));
                int frc = Evo.FAVEngine.Init(handFAVE[camIdx], language, LastAppliedDevice, rtspUrl);
                if(frc != 0 && LastAppliedDevice != null) {
                    Util.Logger.Log(string.Format("FAVEngine CAM{0} ddd={1} 실패 rc={2} → null 재시도", camIdx + 1, LastAppliedDevice, frc));
                    frc = Evo.FAVEngine.Init(handFAVE[camIdx], language, null, rtspUrl);
                    if(frc == 0) LastAppliedDevice = null;
                }
                if(frc != 0) {
                    Util.Logger.Log(string.Format("FAVEngine CAM{0} Init() 실패 rc={1}", camIdx + 1, frc));
                    Evo.Func.FreeObj(ref handFAVE[camIdx]);
                    return false;
                }
                // ROI(인식 영역) 설정 — 환경설정의 카메라 ROI 좌표 적용
                Evo.Rect searchRect = new Evo.Rect();
                Evo.Engine.GetParamSearchRect(handFAVE[camIdx], out searchRect);
                var roi = (camIdx == 0)
                    ? frmLprMain.ENV.CameraEnv.IPCamera1Info.Roi
                    : frmLprMain.ENV.CameraEnv.IPCamera2Info.Roi;
                searchRect.x = roi.X;
                searchRect.y = roi.Y;
                searchRect.width = roi.Width;
                searchRect.height = roi.Height;
                int rrc = Evo.Engine.SetParamSearchRect(handFAVE[camIdx], in searchRect);
                Util.Logger.Log(string.Format("FAVEngine CAM{0} SearchRect x={1} y={2} w={3} h={4} rc={5}",
                    camIdx + 1, searchRect.x, searchRect.y, searchRect.width, searchRect.height, rrc));
                Util.Logger.Log(string.Format("FAVEngine CAM{0} 시작 URL={1}", camIdx + 1, rtspUrl));
                return true;
            } catch(Exception ex) {
                Util.Logger.Log(string.Format("FAVEngine CAM{0} InitFAVE 예외: {1}", camIdx + 1, ex.Message));
                return false;
            }
        }

        private static void RunFAVELoop(int camIdx) {
            int hFAVE = handFAVE[camIdx];
            Util.Logger.Log(string.Format("FAVEngine CAM{0} 루프 시작", camIdx + 1));
            while(faveRunning[camIdx]) {
                int ctxGOP = -1;
                int ctxLPI = -1;
                try {
                    ctxLPI = Evo.FAVEngine.PopLPI(hFAVE, out ctxGOP);
                    if(ctxLPI < 0) {
                        Util.Logger.Log(string.Format("FAVEngine CAM{0} PopLPI 반환 NULL rc={1}", camIdx + 1, Evo.Func.GetLastRC()));
                        break;
                    }

                    // PopLPI 반환 이후 처리 시간을 인식속도로 사용
                    // (PopLPI 자체는 차량 대기까지 블로킹이라 측정 의미 없음 — 결과 추출 + 저장 시간만 의미 있음)
                    DateTime favProcStart = DateTime.Now;

                    uint num = 0;
                    int lrc = Evo.LPI.GetNumLP(ctxLPI, out num);
                    string plateNo = "No_Detection";
                    if(lrc == 0 && num > 0) {
                        StringBuilder strBuf = new StringBuilder(512);
                        Evo.LPI.SelectLP(ctxLPI, 0);
                        lrc = Evo.LPI.GetStr(ctxLPI, strBuf);
                        if(lrc == 0 && strBuf.Length > 0)
                            plateNo = strBuf.ToString();
                    }
                    Util.Logger.Log(string.Format("FAVEngine CAM{0} 인식 결과: {1}", camIdx + 1, plateNo));

                    string imgPath = SaveFAVEImage(camIdx, ctxGOP);

                    string captureTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    long favTerm = (long)(DateTime.Now - favProcStart).TotalMilliseconds;
                    ClsStructure.RegStruct[] regArr = (camIdx == 0) ? clsThread.RegArray1 : clsThread.RegArray2;
                    for(int j = 0; j < regArr.Length; j++) {
                        regArr[j].PlateNo = plateNo;
                        regArr[j].SourcePath = imgPath;
                        regArr[j].FirstCaptureTime = captureTime;
                        regArr[j].CapCnt = j;
                        regArr[j].term = favTerm;
                        regArr[j].CarType = "";
                    }
                    if(camIdx == 0)
                        clsThread.RegArray1 = regArr;
                    else
                        clsThread.RegArray2 = regArr;

                    // 동일 채널 중복 인식 방지
                    DateTime nowReg = DateTime.Now;
                    double elapsedMs = (nowReg - lastFAVERegTime[camIdx]).TotalMilliseconds;
                    if(elapsedMs < FaveRegIntervalMs) {
                        Util.Logger.Log(string.Format("FAVEngine CAM{0} 중복 인식 무시 (간격 {1}ms)", camIdx + 1, (int)elapsedMs));
                        continue;
                    }
                    lastFAVERegTime[camIdx] = nowReg;

                    int ci = camIdx;
                    Thread th = new Thread(() => clsThread.AfterRegPlateCam(ci, frmLprMain.ENV));
                    th.IsBackground = true;
                    th.Start();
                } catch(Exception ex) {
                    Util.Logger.Log(string.Format("FAVEngine CAM{0} RunFAVELoop 예외: {1}", camIdx + 1, ex.Message));
                } finally {
                    if(ctxGOP >= 0)
                        Evo.Func.FreeObj(ref ctxGOP);
                    if(ctxLPI >= 0)
                        Evo.Func.FreeObj(ref ctxLPI);
                }
            }
            Util.Logger.Log(string.Format("FAVEngine CAM{0} 루프 종료", camIdx + 1));
        }

        private static string SaveFAVEImage(int camIdx, int ctxGOP) {
            if(ctxGOP < 0) return "";
            try {
                string basePath = frmLprMain.ENV.CameraEnv.ImageSave.SavePath;
                if(string.IsNullOrEmpty(basePath)) return "";
                string dateDir = System.IO.Path.Combine(basePath, DateTime.Now.ToString("yyyyMMdd"));
                if(!System.IO.Directory.Exists(dateDir))
                    System.IO.Directory.CreateDirectory(dateDir);
                string chName = (camIdx == 0)
                    ? frmLprMain.ENV.CameraEnv.IPCamera1Info.ChName
                    : frmLprMain.ENV.CameraEnv.IPCamera2Info.ChName;
                string fname = string.Format("{0}_{1}.jpg", chName, DateTime.Now.ToString("yyyyMMddHHmmssfff"));
                string fullPath = System.IO.Path.Combine(dateDir, fname);

                // GOP 사진 수 확인
                uint numPic = 0;
                int numRc = Evo.GOP.GetNumPic(ctxGOP, out numPic);
                Util.Logger.Log(string.Format("FAVEngine CAM{0} GOP numPic={1} numRc={2}", camIdx + 1, numPic, numRc));
                if(numRc != 0 || numPic == 0) {
                    Util.Logger.Log(string.Format("FAVEngine CAM{0} GOP 사진 없음 — 이미지 저장 불가", camIdx + 1));
                    return "";
                }
                int selRc = Evo.GOP.SelectPic(ctxGOP, 0);
                if(selRc != 0) {
                    Util.Logger.Log(string.Format("FAVEngine CAM{0} SelectPic 실패 rc={1}", camIdx + 1, selRc));
                    return "";
                }
                // SavePicInJPEG는 ASCII 경로만 지원
                bool hasNonAscii = false;
                foreach(char c in fullPath) { if(c > 127) { hasNonAscii = true; break; } }
                if(hasNonAscii) {
                    string tmpPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), fname);
                    int grc = Evo.GOP.SavePicInJPEG(ctxGOP, tmpPath, 95);
                    if(grc == 0 && System.IO.File.Exists(tmpPath))
                        System.IO.File.Move(tmpPath, fullPath);
                    else
                        Util.Logger.Log(string.Format("FAVEngine CAM{0} SavePicInJPEG(tmp) 실패 rc={1}", camIdx + 1, grc));
                } else {
                    int grc = Evo.GOP.SavePicInJPEG(ctxGOP, fullPath, 95);
                    if(grc != 0)
                        Util.Logger.Log(string.Format("FAVEngine CAM{0} SavePicInJPEG 실패 rc={1}", camIdx + 1, grc));
                }
                // 저장 결과 확인
                if(!System.IO.File.Exists(fullPath) || new System.IO.FileInfo(fullPath).Length == 0) {
                    Util.Logger.Log(string.Format("FAVEngine CAM{0} 이미지 파일 없거나 0바이트 — 경로 반환 안 함", camIdx + 1));
                    if(System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
                    return "";
                }
                return fullPath;
            } catch(Exception ex) {
                Util.Logger.Log(string.Format("FAVEngine CAM{0} SaveFAVEImage 예외: {1}", camIdx + 1, ex.Message));
                return "";
            }
        }

        public static void StartFAVELoop(int camIdx) {
            if(camIdx < 0 || camIdx >= handFAVE.Length || handFAVE[camIdx] < 0) return;
            faveRunning[camIdx] = true;
            int capturedIdx = camIdx;
            faveThreads[camIdx] = new Thread(() => RunFAVELoop(capturedIdx));
            faveThreads[camIdx].IsBackground = true;
            faveThreads[camIdx].Start();
        }

        /// <returns>0 = 성공, 그 외 = SDK LiveView 실패 (LPR 인식에는 영향 없음)</returns>
        public static int StartLiveView(int camIdx, IntPtr winHandle) {
            if(camIdx < 0 || camIdx >= handFAVE.Length || handFAVE[camIdx] < 0) {
                Util.Logger.Log(string.Format("FAVEngine CAM{0} StartLiveView guard 실패 handFAVE={1}", camIdx + 1, camIdx < handFAVE.Length ? handFAVE[camIdx].ToString() : "범위초과"));
                return -1;
            }
            if(winHandle == IntPtr.Zero) {
                Util.Logger.Log(string.Format("FAVEngine CAM{0} OpenLiveView 건너뜀: winHandle=0 (LPR 인식은 정상)", camIdx + 1));
                return -1;
            }
            Util.Logger.Log(string.Format("FAVEngine CAM{0} OpenLiveView 호출 handle={1}", camIdx + 1, handFAVE[camIdx]));
            int rc = Evo.FAVEngine.OpenLiveView(handFAVE[camIdx], winHandle, 0); // keepAR=0: 화면에 꽉 채움
            if(rc != 0)
                Util.Logger.Log(string.Format("FAVEngine CAM{0} OpenLiveView rc={1} (화면 표시 불가, LPR 인식은 정상)", camIdx + 1, rc));
            else
                Util.Logger.Log(string.Format("FAVEngine CAM{0} OpenLiveView rc={1}", camIdx + 1, rc));
            return rc;
        }

        public static void StopLiveView(int camIdx) {
            if(camIdx < 0 || camIdx >= handFAVE.Length || handFAVE[camIdx] < 0) return;
            Evo.FAVEngine.CloseLiveView(handFAVE[camIdx]);
        }

        /// <summary>
        /// 모든 채널의 FAVEngine을 정지하고 리소스를 해제합니다.
        /// </summary>
        public static void ReleaseFAVE() {
            for(int i = 0; i < 2; i++) {
                if(handFAVE[i] < 0) continue;
                faveRunning[i] = false;
                try {
                    StopLiveView(i);
                    Evo.FAVEngine.Deinit(handFAVE[i]); // PopLPI 블로킹 해제
                    if(faveThreads[i] != null && faveThreads[i].IsAlive)
                        faveThreads[i].Join(3000);
                    Evo.Func.FreeObj(ref handFAVE[i]);
                    Util.Logger.Log(string.Format("FAVEngine CAM{0} 해제 완료", i + 1));
                } catch(Exception ex) {
                    Util.Logger.Log(string.Format("FAVEngine CAM{0} ReleaseFAVE 예외: {1}", i + 1, ex.Message));
                    handFAVE[i] = -1;
                }
            }
        }
    }
    public class Result {
        public string[] CarNo;
        public Rectangle[] rect;
        public long[] Term;
        public int[] Confidence;
        public string Error;
    }
}
