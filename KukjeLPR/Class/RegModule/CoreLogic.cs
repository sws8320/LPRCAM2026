using System;
using System.Drawing;
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
        private static IntPtr ctxDDI = IntPtr.Zero; // Context of DDI.
        private static IntPtr[] handSSE = new IntPtr[2] { IntPtr.Zero, IntPtr.Zero }; // Handle of Snapshot Engine
        private static int rc;
        private static object obj = new object();
        public const int KOR = 410;
        public const int THA = 764;

        public static bool Initialize() {
            // 1. Initialize the Evo engine library. Optionally can get DDI context.
            if(cc == 0)
                cc = KOR;
            //leess 6.x 모듈변경
            string language = "KOR";
            if(cc == THA) language = "THA";
            //rc = Evo.Func.Initialize(cc, "latency", null, ref ctxDDI);
            Evo.Func.Initialize(language, "latency", null, ref ctxDDI);
            if(rc != 0) {
                Console.WriteLine("Evo.Func.Initialize() failed. : {0}", rc);
                return false;
            }

            // [+] Optionally examine the DDI.
            // Get total number of available DNN devices.
            rc = DDI.GetNumDevices(ctxDDI, out numDev);
            Console.WriteLine(rc == 0);
            Console.WriteLine("<< DNN Device Information >>");
            for(uint i = 0; i < numDev; i++) {
                uint numFmt;
                string devInfo;
                StringBuilder dev = new StringBuilder(128);
                // Get name of specific DNN device.
                rc = DDI.GetDevice(ctxDDI, i, dev);
                Console.WriteLine(rc == 0);

                // Get total number of DNN model formats which are supported by specified DNN device.
                rc = DDI.GetNumFormats(ctxDDI, dev.ToString(), out numFmt);
                Console.WriteLine(rc == 0);
                devInfo = dev.ToString() + " : ";
                // Print all the DNN model formats which are supported
                // by the specified DNN device.
                for(uint j = 0; j < numFmt; j++) {
                    StringBuilder format = new StringBuilder(16);
                    // Get specific DNN model format.
                    rc = DDI.GetFormat(ctxDDI, dev.ToString(), j, format);
                    Console.WriteLine(rc == 0);
                    devInfo += format.ToString();
                    if(j < numFmt - 1)
                        devInfo += ", ";
                }
                Console.WriteLine(devInfo);
            }
            Console.WriteLine();

            // Must destroy the DDI context at last.
            //leess 6.x 모듈변경
            //DDI.FreeContext(ref ctxDDI);
            DDI.Free(ref ctxDDI);
            // [-]
            // 2. Allocate a new snapshot engine.
            for(int i = 0; i < 2; i++) {
                //leess 6.x 모듈변경
                //handSSE[i] = Evo.SSEngine.Allocate();
                handSSE[i] = Evo.SSEngine.Create();
                if(handSSE[i] == IntPtr.Zero) {
                    Console.WriteLine("SnapshotEngine.Allocate() failed. : {0}", Evo.Func.GetLastRC());
                    return false;
                }

                // 3. Initialize the engine with DNN Devcie Descriptor.
                //leess 6.x 모듈변경
                //rc = Evo.SSEngine.Init(handSSE[i], "FP32:CPU");
                rc = Evo.SSEngine.Init(handSSE[i], language, "FP32:CPU");
                if(rc != 0) {
                    Console.WriteLine("SnapshotEngine.Allocate() failed. : {0}", rc);
                    return false;
                }
            }
            return true;
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

                Evo.Output output = new Evo.Output();
                //leess 6.x 모듈변경
                //Evo.Func.GetParamOutput(handSSE[idx], out output);
                Evo.Engine.GetParamOutput(handSSE[idx], out output);

                if(bRegCarType)
                    output.enAmType = 1;
                else
                    output.enAmType = 0; // 0:Disable 1:Enable

                //leess 6.x 모듈변경
                //Evo.Func.SetParamOutput(handSSE[idx], in output);
                //rc = Evo.Func.SetParamSearchRect(handSSE[idx], in searchRect);
                Evo.Engine.SetParamOutput(handSSE[idx], in output);
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
                    IntPtr ctxLPI = IntPtr.Zero; // Context of License Plate Information.
                                                 // 5. Run the engine with input image.
                    ctxLPI = Evo.SSEngine.Run(handSSE[idx], RegArray.SourcePath);
                    // Get the return code.
                    rc = Evo.Func.GetLastRC();
                    if(ctxLPI != IntPtr.Zero) {
                        uint num;
                        //
                        // 6. Examine the LPI context.
                        //
                        // Get total number of the detected license plates.
                        rc = Evo.LPI.GetNumber(ctxLPI, out num);
                        Console.WriteLine(rc == 0);

                        //leess 여러개 인식일 경우 가장 사이즈가 큰것 사용
                        int maxRegSize = 0;
                        for(uint i = 0; i < num; i++) {
                            Evo.Rect bbox;
                            int type;
                            float confidence;
                            StringBuilder strBuf = new StringBuilder(512);
                            // Get text string.
                            rc = Evo.LPI.GetString(ctxLPI, i, strBuf);
                            Console.WriteLine(rc == 0);
                            Console.WriteLine("\n{0}", strBuf.ToString());
                            //leess 아래로 내림
                            //if(RegArray.PlateNo != strBuf.ToString()) {
                            //    if(RegArray.PlateNo.Length > 0) RegArray.PlateNo += ",";//leess 이게 이상함. 멀티로 줄려고 만든것 같은데, 아래에서 그냥 대체가 되버림!
                            //    RegArray.PlateNo = strBuf.ToString();
                            //}
                            // Get the bounding box.
                            rc = Evo.LPI.GetPosition(ctxLPI, i, out bbox);
                            Console.WriteLine(rc == 0);
                            Console.WriteLine(" --> Position: {0}, {1}, {2}, {3}", bbox.x, bbox.y, bbox.width, bbox.height);
                            // Get the type and its confidence.
                            rc = Evo.LPI.GetType(ctxLPI, i, out type, out confidence);
                            Console.WriteLine(rc == 0);
                            Console.WriteLine(" --> Type: {0} ({1}%)", type, confidence);

                            //leess 사이즈 제일큰것 선택
                            int regSize = bbox.width * bbox.height;
                            if(regSize > maxRegSize) {
                                maxRegSize = regSize;
                                if(RegArray.PlateNo != strBuf.ToString()) {
                                    RegArray.PlateNo = strBuf.ToString();
                                }
                            } else {
                                Util.Logger.Log(string.Format("X 작은 사이즈 인식 결과 버림 : {0}", strBuf.ToString()));
                            }

                            if(cc == KOR) {
                                rc = Evo.LPI.GetAmType(ctxLPI, i, strBuf, out confidence);
                                Console.WriteLine(rc == 0);
                                Console.WriteLine("\n{0}", strBuf.ToString());
                                Console.WriteLine(string.Format(" --> confidence: {0} ({1}%)", type, confidence));
                                if(RegArray.CarType != strBuf.ToString()) {
                                    if(RegArray.CarType.Length > 0) RegArray.CarType += ",";
                                    RegArray.CarType = strBuf.ToString();
                                    RegArray.Confidence = confidence;
                                    RegArray.term = (long)(DateTime.Now - dateTime).TotalMilliseconds; // sp.ElapsedMilliseconds;
                                }
                            }
                        }
                        // [-]
                        // 7. Destroy the LPI context if no more used.
                        //leess 6.x 모듈변경
                        //Evo.LPI.FreeContext(ref ctxLPI);
                        Evo.Engine.FreeLPI(handSSE[idx], ref ctxLPI);
                    } else {
                        Console.WriteLine("Evo.SnapshotEngine.Run('{0}') failed. : {1}", RegArray.SourcePath, rc);
                        RegArray.PlateNo = "No_Detection";
                        RegArray.PlateRoi = string.Format("{0},{1},{2},{3}", 0, 0, 0, 0);
                        RegArray.term = (long)(DateTime.Now - dateTime).TotalMilliseconds; // sp.ElapsedMilliseconds;
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
                // 8. Deinitialize the engine.
                Evo.SSEngine.Deinit(handSSE[i]);
                // 9. Destroy the engine if no more used.
                //leess 6.x 모듈변경
                //Evo.SSEngine.FreeHandle(ref handSSE[i]);
                Evo.SSEngine.Deinit(handSSE[i]);
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
