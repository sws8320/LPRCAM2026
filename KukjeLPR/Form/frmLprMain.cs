//코아 로직 제외 컴파일
//CoreLogic.cs
//frmEnv

#define Core

using LibSocket;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace KyungsinLPR
{
    public partial class frmLprMain : Form {
        private IPCamera m_camera1 = new IPCamera();
        private IPCamera m_camera2 = new IPCamera();
        //leess iNova2추가
        private iNova2.IPCamera m_camera1_inova2 = new iNova2.IPCamera();
        private iNova2.IPCamera m_camera2_inova2 = new iNova2.IPCamera();
        // USB(DirectShow/UVC) 카메라 — 카메라별 CameraSource==USB 일 때 사용
        private KyungsinLPR.USB.USBCamera m_camera1_usb = new KyungsinLPR.USB.USBCamera();
        private KyungsinLPR.USB.USBCamera m_camera2_usb = new KyungsinLPR.USB.USBCamera();
        private Thread m_grabThread1;
        private Thread m_grabThread2;
        private bool m_keepGrab1;
        private bool m_keepGrab2;
        // SDK OpenLiveView 실패 시 WPF MediaElement 폴백 프리뷰 (채널별)
        private RtspPreview _rtspPreview1;
        private RtspPreview _rtspPreview2;
        private FrameRate m_frameRate = new FrameRate();
        private double m_maxBufferSizeKB = 384;
        private string CamIP = string.Empty;
        private clsFunction func = new clsFunction();
        public static ClsStructure.EnvStruct ENV = new ClsStructure.EnvStruct();
        private ClsStructure.IPCamera_Info IpCam1Current = new ClsStructure.IPCamera_Info();
        private ClsStructure.IPCamera_Info IpCam2Current = new ClsStructure.IPCamera_Info();
        private bool Capture1 = false;
        private bool Capture2 = false;
        private bool Loop1 = false;
        private bool Loop2 = false;
        private List<ClsStructure.RegList> RegList1 = new List<ClsStructure.RegList>();
        private List<ClsStructure.RegList> RegList2 = new List<ClsStructure.RegList>();
        private Int32 Cam1ID = 0;
        private Int32 Cam2ID = 0;

        private clsSerialPort ComPort = new clsSerialPort();
        DateTime LastLoopTime1 = DateTime.Now.AddSeconds(-1);
        DateTime LastLoopTime2 = DateTime.Now.AddSeconds(-1);

        #region 인식모듈 엔진 ID
        private uint[,] uEngineID = new uint[,] { { 0, 0 }, { 0, 0 }, { 0, 0 }, { 0, 0 } };
        #endregion
        #region NgisWay 모듈 선언
        public NgisWay_Module NgisWay = new NgisWay_Module();
        #endregion

        private DataTable dtRegList1 = new DataTable();
        private DataTable dtRegList2 = new DataTable();

        private ClsStructure.RegStruct[] RegArray1 = new ClsStructure.RegStruct[4];
        private ClsStructure.RegStruct[] RegArray2 = new ClsStructure.RegStruct[4];
        private long ImgCnt = 0;

        private string lastPlate = "No_Detection";
        private frmLPRComm frm = new frmLPRComm();

        public clsDataTransaction DataProcess;
        public clsSerialPort SerialDev = null;

        public static SerialDevice.ReturnDisPlay FirstDisPlayReturn = null;
        public static SerialDevice.ReturnDisPlay SecondDisPlayReturn = null;
        
        //private Server server = null;

        //private SocketClient client1 = new SocketClient();
        //private SocketClient client2 = new SocketClient();
        //private SocketClient client3 = new SocketClient();
        //private SocketClient client4 = new SocketClient();

        private ClientSocket HomeLan = new ClientSocket();
        public Server LprExitSvr = new Server();
        public Server LprEntSvr = new Server();
        private Server DisPlaySvr = new Server();
        private Server StoneSvr = new Server();

        private string[] SendMsg = new string[4];

        private int reconCnt = 0;

        //public static clsThread clsthread = new clsThread();
        public static frmLprMain Main;

        public string Path1 = string.Empty;
        public string Path2 = string.Empty;

        Thread ThreadImageSaveTermCheck;

        //사무실 전송 소켓
        private ClientSocket OfficeSocket = new ClientSocket();
        public List<string> SendOfficeList = new List<string>();
        //쓰레드 유지 및 종료 변수
        private bool Thread_Alive = true;
        private Thread tExposure;

        //전광판 고정 문자 변수
        public static bool isFixed = false;

        private Thread thCoreInit;

        private ClientSocket LPRCam = new ClientSocket();

        #region Network display
        public static NetworkDisplay NetDisPlay1 = new NetworkDisplay();
        public static NetworkDisplay NetDisPlay2 = new NetworkDisplay();
        #endregion

        #region 삼성 LPRTRNS acRegNo, iInEqpm, iClient, acCarModel1, acCarModel2 추가 필드 대응 20191008
        public static bool ExtendLprtrns = false;
        #endregion

        public frmLprMain() {
            CheckForIllegalCrossThreadCalls = false;
            Util.Logger.LogFile = "LprCam_";
            InitializeComponent();
            clsThread.main = this;
            clsThread.frm = frm;
            NgisWay.main = this;
            Main = this;
            // ParkingWeb 이미지 서버 업로더 시작 (Setting.ini [UPLOAD] 섹션 사용)
            clsImageUploader.Start();
        }

        #region GrapImage

        private void StartGrabLoop1() {
            if(!m_keepGrab1) {
                //leess iNova2추가
                //m_grabThread1 = new Thread(GrabLoop1);
                if(IsUsbCam(1)) m_grabThread1 = new Thread(GrabLoop1_USB);
                else if(ENV.CameraEnv.iNovaType == 1) m_grabThread1 = new Thread(GrabLoop1);
                else if(ENV.CameraEnv.iNovaType == 2) m_grabThread1 = new Thread(GrabLoop1_iNova2);
                m_grabThread1.IsBackground = true;
                m_keepGrab1 = true;
                m_grabThread1.Start();
            }
        }

        private void StartGrabLoop2() {
            if(!m_keepGrab2) {
                //leess iNova2추가
                //m_grabThread2 = new Thread(GrabLoop2);
                if(IsUsbCam(2)) m_grabThread2 = new Thread(GrabLoop2_USB);
                else if(ENV.CameraEnv.iNovaType == 1) m_grabThread2 = new Thread(GrabLoop2);
                else if(ENV.CameraEnv.iNovaType == 2) m_grabThread2 = new Thread(GrabLoop2_iNova2);
                m_grabThread2.IsBackground = true;
                m_keepGrab2 = true;
                m_grabThread2.Start();
            }
        }

        private void StopGrabLoop1() {
            if(m_keepGrab1) {
                m_keepGrab1 = false;
                m_grabThread1.Join(1000);
            }
        }

        private void StopGrabLoop2() {
            if(m_keepGrab2) {
                m_keepGrab2 = false;
                m_grabThread2.Join(1000);
            }
        }

        delegate void SetLabelTextCallback(Control label, string text);

        public void SetLabelText(Control label, string text) {
            if(label.InvokeRequired) {
                var d = new SetLabelTextCallback(SetLabelText);
                try {
                    this.BeginInvoke(d, new object[] { label, text });
                } catch(ObjectDisposedException) { }
            } else {
                label.Text = text;
            }
        }

        private void GrabLoop1(object threadParam) {
            int errCnt = 0;
            int errCnt1 = 0;
            int CapCnt = 0;
            int CurrentCnt = 0;
            while(m_keepGrab1) {
                try {
                    errCnt = 0;
                    errCnt1 = 0;
                    Bitmap bitmap;
                    MetaInfo metaInfo;
                    IPCamError err = m_camera1.GetImage(1000, out bitmap, out metaInfo);
                    if(err == IPCamError.OK) {
                        if(label1.Visible)
                            Util.Function.InvokeControlVisible(label1, false);
                        if(!(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic && ENV.CameraEnv.RecogMode == 1))
                            SetBitmap(PicLpr1Image, bitmap);
                        if(Capture1) {
                            if(IpCam1Current.BracketInfo.Use)
                                CurrentCnt = ENV.CameraEnv.IPCamera1Info.BarkectCnt;
                            else
                                CurrentCnt = ENV.CameraEnv.IPCamera1Info.TriggerCnt;
                            if(FirstDisPlayReturn != null) FirstDisPlayReturn.DisPlayTime = DateTime.Now;
                            //Bitmap savebmp;
                            Cam1ID++;
                            //if (RegList1.Count(x => x.id.Equals(Cam1ID)) >= IpCam1Current.TriggerInfo.CountPerTrigger)
                            if(CurrentCnt == 0)
                                CurrentCnt = 1;
                            //else if (CurrentCnt > IpCam1Current.TriggerInfo.CountPerTrigger)
                            //    CurrentCnt = IpCam1Current.TriggerInfo.CountPerTrigger;
                            if(CapCnt < CurrentCnt) {
                                ImgCnt++;
                                string fname = ENV.CameraEnv.IPCamera1Info.ChName + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ImgCnt.ToString() + ".jpg";
                                while(true) {
                                    Util.Logger.Log(string.Format("CAM1 {0}", fname));
                                    if(m_camera1.SaveLastImage(fname))
                                    //break;
                                    //20161124 Start
                                    {
                                        //try
                                        //{
                                        //    if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\Back"))
                                        //        Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Back");
                                        //    File.Copy(fname, Directory.GetCurrentDirectory() + "\\Back\\" + fname);
                                        //}
                                        //catch (Exception Copy_Error)
                                        //{
                                        //    Util.Logger.Log(string.Format("Cam1 Back Folder Copy Error FileName {0} Error Message {1}", fname, Copy_Error.Message));
                                        //}
                                        break;
                                    }
                                    //20161124 End
                                    ImgCnt++;
                                    fname = ENV.CameraEnv.IPCamera1Info.ChName + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ImgCnt.ToString() + ".jpg";
                                }
                                Util.Logger.Log(string.Format("CAM1 {0} Saved", fname));
                                RECT roi = new RECT();
                                roi.x = ENV.CameraEnv.IPCamera1Info.Roi.Left;
                                roi.y = ENV.CameraEnv.IPCamera1Info.Roi.Top;
                                roi.w = ENV.CameraEnv.IPCamera1Info.Roi.Left + ENV.CameraEnv.IPCamera1Info.Roi.Width;
                                roi.h = ENV.CameraEnv.IPCamera1Info.Roi.Top + ENV.CameraEnv.IPCamera1Info.Roi.Height;
                                //dtRegList1.Rows.Add(null, 1, CapCnt + 1, fname, string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h), "", "", LastLoopTime1, false, 0, IpCam1Current.Generalinfo.Exposure);
                                clsThread.RegArray1[CapCnt].CapCnt = CapCnt;
                                clsThread.RegArray1[CapCnt].SourcePath = fname;
                                clsThread.RegArray1[CapCnt].Roi = string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h);
                                clsThread.RegArray1[CapCnt].PlateRoi = null;
                                clsThread.RegArray1[CapCnt].PlateNo = null;
                                clsThread.RegArray1[CapCnt].FirstCaptureTime = LastLoopTime1.ToString("yyyy-MM-dd HH:mm:ss");
                                clsThread.RegArray1[CapCnt].Send = false;
                                try {
                                    clsThread.RegArray1[CapCnt].Exposure = metaInfo.Exposure;
                                    clsThread.RegArray1[CapCnt].FrmaeCount = metaInfo.FrameCount;
                                } catch(Exception) { }
                                if(File.Exists(fname))
                                    clsThread.RegArray1[CapCnt].Size = new System.IO.FileInfo(fname).Length;
                                //RegArray1[CapCnt].term = 0;
                                clsThread.RegArray1[CapCnt].Exposure = ENV.CameraEnv.IPCamera1Info.CurrentInfo.Generalinfo.Exposure;
                                Util.Logger.Log(string.Format("****CAM1 {0} reg Start CapCnt {1} ROI {2}", fname, CapCnt, clsThread.RegArray1[CapCnt].Roi));
                                //NgisWay.Reg1(RegArray1[CapCnt]);
                                //if (CapCnt.Equals(0))
                                //    frm.pictureBox1.Image = new Bitmap(fname);
                                //clsthread.RegPlateNoNgisWay(0, clsthread.RegArray1[CapCnt]);
                                if(!Environment.Is64BitProcess) {
                                    if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Ngis)
                                        clsThread.RegPlateNoNgisWay(0, CapCnt);
                                    else if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Elwox)
                                        clsThread.RegPlateNoElwox(0, CapCnt);
                                } else {
#if WIN64
                                    //CoreLogic 스트로브 방식만 여기서 인식 (동영상 방식은 FAVEngine 스레드가 처리)
                                    if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic
                                        && ENV.CameraEnv.RecogMode == 0)
                                        CoreLogic.Reg(0, CapCnt, ENV.CameraEnv.bRegCarType);
                                    //Option(K) — 자체 CRNN/ONNX (64bit 전용)
                                    if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.OptionK)
                                        OptionK.Reg(0, CapCnt, ENV.CameraEnv.bRegCarType);
#endif
                                }
                                if(RegList1.Count.Equals(0)) Cam1ID = 0;
                                CapCnt++;
                            }
                        }
                        //if (Capture1 && (CapCnt == IpCam1Current.TriggerInfo.CountPerTrigger || (DateTime.Now - LastLoopTime1).TotalSeconds > 1))
                        if(Capture1 && (CapCnt == CurrentCnt))// || (DateTime.Now - LastLoopTime1).TotalSeconds > 2))
                        {
                            if(CapCnt == 0) {
                                Util.Logger.Log("Cam1 영상 취득 실패 작업 처리 누락");
                                Capture1 = false;
                                CapCnt = 0;
                            } else {
                                Capture1 = false;
                                CapCnt = 0;
                                Util.Logger.Log("AfterRegPlateCam Loop1");
                                // [수정] new Thread → ThreadPool — 트리거마다 OS 스레드 신규 생성 방지
                                ThreadPool.QueueUserWorkItem(_ => {
                                    try { clsThread.AfterRegPlateCam(0, ENV); }
                                    catch (Exception ex) { Util.Logger.Log("AfterRegPlateCam 예외: " + ex.Message); }
                                });
                            }
                        }
                        try {
                            UpdateStatus1(metaInfo);
                        } catch(Exception) { }
                        if(ENV.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals("KJC1000")) {
                            if(!lblCam1Loop.Text.Equals("Loop Off")) {
                                TimeSpan diff = DateTime.Now - LastLoopTime1;
                                if(diff.TotalMilliseconds > ENV.LoopTerm) {
                                    SetLabelText(lblCam1Loop, "Loop Off");
                                    Loop1 = false;
                                }
                            }
                        }
                    } else // error or timeout.
                      {
                        //Util.Logger.Log("Time Out Cam1 ResetCamera");
                        //m_camera1.ResetCamera();
                        //Thread.Sleep(100);
                        //m_camera1.DisconnectStreamPort();
                        //Thread.Sleep(100);
                        //m_camera1.ConnectStreamPort(ENV.CameraEnv.IPCamera1Info.IP, ENV.CameraEnv.IPCamera1Info.StreamUdp);
                        //Thread.Sleep(100);
                        if(!label1.Visible)
                            Util.Function.InvokeControlVisible(label1, true);
                        errCnt++;
                        errCnt1++;
                        if(errCnt > 100) {
                            m_camera1.ResetCamera();
                            errCnt = 0;
                            if(errCnt1 > 1000) {
                                Application.Exit();
                                Application.ExitThread();
                            }
                        }
                    }
                    //if (!m_camera1.IsStreamPortConnected() && !m_camera1.IsCommandPortConnected())
                    //{
                    //    Util.Logger.Log("IsStreamPortConnected IsCommandPortConnected Cam1 ResetCamera");
                    //    //m_camera1.ResetCamera();
                    //    m_camera1.DisconnectStreamPort();
                    //    Thread.Sleep(100);
                    //    m_camera1.ConnectStreamPort(ENV.CameraEnv.IPCamera1Info.IP, ENV.CameraEnv.IPCamera1Info.StreamUdp);
                    //    Thread.Sleep(100);
                    //}
                    //else
                    //{
                    if(!m_camera1.IsStreamPortConnected()) {
                        m_camera1.DisconnectStreamPort();
                        Thread.Sleep(100);
                        m_camera1.ConnectStreamPort(ENV.CameraEnv.IPCamera1Info.IP, ENV.CameraEnv.IPCamera1Info.StreamUdp);
                        Thread.Sleep(100);
                    }
                    if(!m_camera1.IsCommandPortConnected()) {
                        //m_camera1.DisconnectCommandPort();
                        //Thread.Sleep(100);
                        //m_camera1.ConnectCommandPort(ENV.CameraEnv.IPCamera1Info.IP);
                        CamCommandReConnect(m_camera1, ENV.CameraEnv.IPCamera1Info.IP);
                    }
                    //}
                } catch(Exception e) {
                    Util.Logger.Log(string.Format("GrabLoop1 Error {0}", e.Message));
                    Capture1 = false;
                }
                //leess 속도개선 : 이것때문에 반응이 느렸음.
                //Thread.Sleep(20);
            }
        }

        //leess iNova2추가
        private void GrabLoop1_iNova2(object threadParam) {
            int errCnt = 0;
            int errCnt1 = 0;
            int CapCnt = 0;
            int CurrentCnt = 0;
            bool firstFrameLogged = false;
            while(m_keepGrab1) {
                try {
                    errCnt = 0;
                    errCnt1 = 0;
                    Bitmap bitmap;
                    iNova2.MetaInfo metaInfo;
                    iNova2.IPCamError err = m_camera1_inova2.GetImage(1000, out bitmap, out metaInfo);
                    if(!firstFrameLogged) {
                        Util.Logger.Log(string.Format("CAM1 GetImage 첫번째 결과={0}", err));
                        firstFrameLogged = true;
                    }
                    if(err == iNova2.IPCamError.OK) {
                        if(label1.Visible)
                            Util.Function.InvokeControlVisible(label1, false);
                        if(!(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic && ENV.CameraEnv.RecogMode == 1))
                            SetBitmap(PicLpr1Image, bitmap);
                        if(Capture1) {
                            if(IpCam1Current.BracketInfo.Use)
                                CurrentCnt = ENV.CameraEnv.IPCamera1Info.BarkectCnt;
                            else
                                CurrentCnt = ENV.CameraEnv.IPCamera1Info.TriggerCnt;
                            if(FirstDisPlayReturn != null) FirstDisPlayReturn.DisPlayTime = DateTime.Now;
                            //Bitmap savebmp;
                            Cam1ID++;
                            if(CurrentCnt == 0)
                                CurrentCnt = 1;
                            if(CapCnt < CurrentCnt) {
                                ImgCnt++;
                                string fname = ENV.CameraEnv.IPCamera1Info.ChName + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ImgCnt.ToString() + ".jpg";
                                while(true) {
                                    Util.Logger.Log(string.Format("CAM1 {0}", fname));
                                    if(m_camera1_inova2.SaveLastImage(fname)) {
                                        break;
                                    }
                                    ImgCnt++;
                                    fname = ENV.CameraEnv.IPCamera1Info.ChName + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ImgCnt.ToString() + ".jpg";
                                }
                                Util.Logger.Log(string.Format("CAM1 {0} Saved", fname));
                                RECT roi = new RECT();
                                roi.x = ENV.CameraEnv.IPCamera1Info.Roi.Left;
                                roi.y = ENV.CameraEnv.IPCamera1Info.Roi.Top;
                                roi.w = ENV.CameraEnv.IPCamera1Info.Roi.Left + ENV.CameraEnv.IPCamera1Info.Roi.Width;
                                roi.h = ENV.CameraEnv.IPCamera1Info.Roi.Top + ENV.CameraEnv.IPCamera1Info.Roi.Height;
                                //dtRegList1.Rows.Add(null, 1, CapCnt + 1, fname, string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h), "", "", LastLoopTime1, false, 0, IpCam1Current.Generalinfo.Exposure);
                                clsThread.RegArray1[CapCnt].CapCnt = CapCnt;
                                clsThread.RegArray1[CapCnt].SourcePath = fname;
                                clsThread.RegArray1[CapCnt].Roi = string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h);
                                clsThread.RegArray1[CapCnt].PlateRoi = null;
                                clsThread.RegArray1[CapCnt].PlateNo = null;
                                clsThread.RegArray1[CapCnt].FirstCaptureTime = LastLoopTime1.ToString("yyyy-MM-dd HH:mm:ss");
                                clsThread.RegArray1[CapCnt].Send = false;
                                //leess 이미지사이즈 추가 : 설정좌표보다 이미지가 작을 경우 아예 인식동작이 안하는것 방지
                                //clsThread.RegArray1[CapCnt].imgWidth = bitmap.Width;
                                //clsThread.RegArray1[CapCnt].imgHeight = bitmap.Height;
                                try {
                                    clsThread.RegArray1[CapCnt].Exposure = metaInfo.Exposure;
                                    clsThread.RegArray1[CapCnt].FrmaeCount = metaInfo.FrameCount;
                                } catch(Exception) { }
                                if(File.Exists(fname))
                                    clsThread.RegArray1[CapCnt].Size = new System.IO.FileInfo(fname).Length;
                                //RegArray1[CapCnt].term = 0;
                                clsThread.RegArray1[CapCnt].Exposure = ENV.CameraEnv.IPCamera1Info.CurrentInfo.Generalinfo.Exposure;
                                Util.Logger.Log(string.Format("****CAM1 {0} reg Start CapCnt {1} ROI {2}", fname, CapCnt, clsThread.RegArray1[CapCnt].Roi));

                                if(!Environment.Is64BitProcess) {
                                    if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Ngis)
                                        clsThread.RegPlateNoNgisWay(0, CapCnt);
                                    else if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Elwox)
                                        clsThread.RegPlateNoElwox(0, CapCnt);
                                } else {
#if WIN64
                                    //CoreLogic 스트로브 방식만 여기서 인식 (동영상 방식은 FAVEngine 스레드가 처리)
                                    if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic
                                        && ENV.CameraEnv.RecogMode == 0)
                                        CoreLogic.Reg(0, CapCnt, ENV.CameraEnv.bRegCarType);
                                    //Option(K) — 자체 CRNN/ONNX (64bit 전용)
                                    if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.OptionK)
                                        OptionK.Reg(0, CapCnt, ENV.CameraEnv.bRegCarType);
#endif
                                }
                                if(RegList1.Count.Equals(0)) Cam1ID = 0;
                                CapCnt++;
                            }
                        }

                        if(Capture1 && (CapCnt == CurrentCnt))// || (DateTime.Now - LastLoopTime1).TotalSeconds > 2))
                        {
                            if(CapCnt == 0) {
                                Util.Logger.Log("Cam1 영상 취득 실패 작업 처리 누락");
                                Capture1 = false;
                                CapCnt = 0;
                            } else {
                                Capture1 = false;
                                CapCnt = 0;
                                Util.Logger.Log("AfterRegPlateCam Loop1");
                                // [수정] new Thread → ThreadPool
                                ThreadPool.QueueUserWorkItem(_ => {
                                    try { clsThread.AfterRegPlateCam(0, ENV); }
                                    catch (Exception ex) { Util.Logger.Log("AfterRegPlateCam 예외: " + ex.Message); }
                                });
                            }
                        }
                        try {
                            UpdateStatus1_iNova2(metaInfo);
                        } catch(Exception) { }
                        if(ENV.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals("KJC1000")) {
                            if(!lblCam1Loop.Text.Equals("Loop Off")) {
                                TimeSpan diff = DateTime.Now - LastLoopTime1;
                                if(diff.TotalMilliseconds > ENV.LoopTerm) {
                                    SetLabelText(lblCam1Loop, "Loop Off");
                                    Loop1 = false;
                                }
                            }
                        }
                    } else {// error or timeout.
                        if(!label1.Visible)
                            Util.Function.InvokeControlVisible(label1, true);
                        errCnt++;
                        errCnt1++;
                        if(errCnt > 100) {
                            m_camera1_inova2.ResetCamera();
                            errCnt = 0;
                            if(errCnt1 > 1000) {
                                Application.Exit();
                                Application.ExitThread();
                            }
                        }
                    }

                    if(!m_camera1_inova2.IsStreamPortConnected()) {
                        m_camera1_inova2.DisconnectStreamPort();
                        Thread.Sleep(100);
                        m_camera1_inova2.ConnectStreamPort(ENV.CameraEnv.IPCamera1Info.IP, ENV.CameraEnv.IPCamera1Info.StreamUdp);
                        Thread.Sleep(100);
                    }
                    if(!m_camera1_inova2.IsCommandPortConnected()) {
                        CamCommandReConnect_iNova2(m_camera1_inova2, ENV.CameraEnv.IPCamera1Info.IP);
                    }
                } catch(Exception e) {
                    Util.Logger.Log(string.Format("GrabLoop1 Error {0}", e.Message));
                    Capture1 = false;
                }
                //leess 속도개선 : 이것때문에 반응이 느렸음.
                //Thread.Sleep(20);
            }
        }

        private void GrabLoop2(object threadParam) {
            int errCnt = 0;
            int errCnt1 = 0;
            int CapCnt = 0;
            int CurrentCnt = 0;
            if(IpCam2Current.BracketInfo.Use)
                CurrentCnt = ENV.CameraEnv.IPCamera2Info.BarkectCnt;
            else
                CurrentCnt = ENV.CameraEnv.IPCamera2Info.TriggerCnt;
            while(m_keepGrab2) {
                // [수정] 카메라 연결 끊김(SocketException/IOException) 시 그랩 스레드가 죽어
                //        AppDomain.UnhandledException으로 프로세스 전체가 종료되던 문제 방지.
                //        GrabLoop1과 동일하게 루프 내부를 try/catch로 감싸 예외 시 로깅 후 다음 루프에서 재연결·재시도.
                try {
                Bitmap bitmap;
                MetaInfo metaInfo;
                IPCamError err = m_camera2.GetImage(1000, out bitmap, out metaInfo);
                if(err == IPCamError.OK) {
                    if(label2.Visible)
                        Util.Function.InvokeControlVisible(label2, false);
                    errCnt = 0;
                    errCnt1 = 0;
                    SetBitmap(PicLpr2Image, bitmap);
                    if(Capture2) {
                        if(IpCam2Current.BracketInfo.Use)
                            CurrentCnt = ENV.CameraEnv.IPCamera2Info.BarkectCnt;
                        else
                            CurrentCnt = ENV.CameraEnv.IPCamera2Info.TriggerCnt;
                        if(SecondDisPlayReturn != null) SecondDisPlayReturn.DisPlayTime = DateTime.Now;
                        //Bitmap savebmp;
                        Cam2ID++;
                        //if (RegList1.Count(x => x.id.Equals(Cam1ID)) >= IpCam1Current.TriggerInfo.CountPerTrigger)
                        if(CurrentCnt == 0)
                            CurrentCnt = 1;
                        //else if (CurrentCnt > IpCam2Current.TriggerInfo.CountPerTrigger)
                        //    CurrentCnt = IpCam2Current.TriggerInfo.CountPerTrigger;
                        if(CapCnt < CurrentCnt) {
                            ImgCnt++;
                            string fname = ENV.CameraEnv.IPCamera2Info.ChName + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ImgCnt.ToString() + ".jpg";
                            while(true) {
                                Util.Logger.Log(string.Format("CAM2 {0}", fname));
                                if(m_camera2.SaveLastImage(fname))
                                //break;
                                //20161124 Start
                                {
                                    //try
                                    //{
                                    //    if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\Back"))
                                    //        Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Back");
                                    //    File.Copy(fname, Directory.GetCurrentDirectory() + "\\Back\\" + fname);
                                    //}
                                    //catch (Exception Copy_Error)
                                    //{
                                    //    Util.Logger.Log(string.Format("Cam2 Back Folder Copy Error FileName {0} Error Message {1}", fname, Copy_Error.Message));
                                    //}
                                    break;
                                }
                                //20161124 End
                                ImgCnt++;
                                fname = ENV.CameraEnv.IPCamera2Info.ChName + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ImgCnt.ToString() + ".jpg";
                            }
                            Util.Logger.Log(string.Format("CAM2 {0} Saved", fname));
                            RECT roi = new RECT();
                            roi.x = ENV.CameraEnv.IPCamera2Info.Roi.Left;
                            roi.y = ENV.CameraEnv.IPCamera2Info.Roi.Top;
                            roi.w = ENV.CameraEnv.IPCamera2Info.Roi.Left + ENV.CameraEnv.IPCamera2Info.Roi.Width;
                            roi.h = ENV.CameraEnv.IPCamera2Info.Roi.Top + ENV.CameraEnv.IPCamera2Info.Roi.Height;
                            //dtRegList1.Rows.Add(null, 1, CapCnt + 1, fname, string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h), "", "", LastLoopTime1, false, 0, IpCam1Current.Generalinfo.Exposure);
                            clsThread.RegArray2[CapCnt].CapCnt = CapCnt;
                            clsThread.RegArray2[CapCnt].SourcePath = fname;
                            clsThread.RegArray2[CapCnt].Roi = string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h);
                            clsThread.RegArray2[CapCnt].PlateRoi = null;
                            clsThread.RegArray2[CapCnt].PlateNo = null;
                            clsThread.RegArray2[CapCnt].FirstCaptureTime = LastLoopTime2.ToString("yyyy-MM-dd HH:mm:ss");
                            clsThread.RegArray2[CapCnt].Send = false;
                            //RegArray1[CapCnt].term = 0;
                            try {
                                clsThread.RegArray2[CapCnt].Exposure = metaInfo.Exposure; //ENV.CameraEnv.IPCamera2Info.CurrentInfo.Generalinfo.Exposure;
                                clsThread.RegArray2[CapCnt].FrmaeCount = metaInfo.FrameCount;
                            } catch(Exception) { }
                            if(File.Exists(fname))
                                clsThread.RegArray2[CapCnt].Size = new System.IO.FileInfo(fname).Length;
                            //Console.WriteLine(string.Format("{0} {1}", metaInfo.Exposure, metaInfo.FrameCount));
                            //NgisWay.Reg1(RegArray1[CapCnt]);
                            //if (CapCnt.Equals(0))
                            //    frm.pictureBox1.Image = new Bitmap(fname);
                            //clsthread.RegPlateNoNgisWay(0, clsthread.RegArray1[CapCnt]);

                            if(DelayReg.Delay) {
                                Thread.Sleep(DelayReg.DelayTerm);
                            }
                            Util.Logger.Log(string.Format("****CAM2 {0} reg Start CapCnt {1} ROI {2}", fname, CapCnt, clsThread.RegArray2[CapCnt].Roi));
                            if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Ngis)
                                clsThread.RegPlateNoNgisWay(1, CapCnt);
                            else if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Elwox)
                                clsThread.RegPlateNoElwox(1, CapCnt);
                            //CoreLogic 스트로브 방식만 여기서 인식 (동영상 방식은 FAVEngine 스레드가 처리)
                            else if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic
                                && ENV.CameraEnv.RecogMode == 0) {
#if WIN64
                                CoreLogic.Reg(1, CapCnt, ENV.CameraEnv.bRegCarType);
#endif
                            }
                            else if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.OptionK) {
#if WIN64
                                OptionK.Reg(1, CapCnt, ENV.CameraEnv.bRegCarType);
#endif
                            }
                            Path2 = fname;
                            if(RegList2.Count.Equals(0)) Cam2ID = 0;
                            CapCnt++;
                        }
                    }
                    //if (Capture2 && (CapCnt == IpCam2Current.TriggerInfo.CountPerTrigger || (DateTime.Now - LastLoopTime2).TotalSeconds > 1))
                    if(Capture2 && (CapCnt == CurrentCnt))// || (DateTime.Now - LastLoopTime2).TotalSeconds > 2))
                    {
                        if(CapCnt == 0) {
                            Util.Logger.Log("Cam2 영상 취득 실패 작업 처리 누락");
                            Capture2 = false;
                            CapCnt = 0;
                        } else {
                            Capture2 = false;
                            if(DelayReg.Delay) {
                                //2체널 동시 인식 시 오류 발생 하여 지연 처리
                                Thread.Sleep(DelayReg.DelayTerm);
                                for(int i = 0; i < CapCnt; i++) {
                                    Util.Logger.Log(string.Format("****CAM2 {0} reg Start CapCnt {1} ROI {2}", clsThread.RegArray2[i].SourcePath, i, clsThread.RegArray2[i].Roi));
                                    if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Ngis)
                                        clsThread.RegPlateNoNgisWay(1, i);
                                    else if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Elwox)
                                        clsThread.RegPlateNoElwox(1, i);
                                    //Corelogic
                                }
                            }

                            CapCnt = 0;
                            Util.Logger.Log("AfterRegPlateCam Loop2");
                            // [수정] new Thread → ThreadPool
                            ThreadPool.QueueUserWorkItem(_ => {
                                try { clsThread.AfterRegPlateCam(1, ENV); }
                                catch (Exception ex) { Util.Logger.Log("AfterRegPlateCam 예외: " + ex.Message); }
                            });
                        }
                    }
                    if(ENV.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals("KJC1000")) {
                        if(!lblCam2Loop.Text.Equals("Loop Off")) {
                            TimeSpan diff = DateTime.Now - LastLoopTime2;
                            if(diff.TotalMilliseconds > ENV.LoopTerm) {
                                SetLabelText(lblCam2Loop, "Loop Off");
                                Loop2 = false;
                            }
                        }
                    }
                    try {
                        UpdateStatus2(metaInfo);
                    } catch(Exception) { }
                } else // error or timeout.
                  {
                    if(!label2.Visible)
                        Util.Function.InvokeControlVisible(label2, true);
                    errCnt++;
                    errCnt1++;
                    if(errCnt > 100) {
                        m_camera2.ResetCamera();
                        errCnt = 0;
                        if(errCnt1 > 1000) {
                            Application.Exit();
                            Application.ExitThread();
                        }
                    }
                    //m_camera2.ResetCamera();
                    //Thread.Sleep(100);
                    //m_camera2.DisconnectStreamPort();
                    //Thread.Sleep(100);
                    //m_camera2.ConnectStreamPort(ENV.CameraEnv.IPCamera2Info.IP, ENV.CameraEnv.IPCamera2Info.StreamUdp);
                }

                //if (!m_camera2.IsStreamPortConnected() && !m_camera2.IsCommandPortConnected())
                //{
                //    m_camera2.ResetCamera();
                //    Thread.Sleep(100);
                //}
                //else
                //{
                if(!m_camera2.IsStreamPortConnected()) {
                    m_camera2.DisconnectStreamPort();
                    Thread.Sleep(100);
                    m_camera2.ConnectStreamPort(ENV.CameraEnv.IPCamera2Info.IP, ENV.CameraEnv.IPCamera2Info.StreamUdp);
                }
                if(!m_camera2.IsCommandPortConnected()) {
                    m_camera2.DisconnectCommandPort();
                    Thread.Sleep(100);
                    m_camera2.ConnectCommandPort(ENV.CameraEnv.IPCamera2Info.IP);
                }
                //}

                //leess 속도개선 : 이것때문에 반응이 느렸음.
                //Thread.Sleep(30);
                } catch(Exception e) {
                    // 연결 끊김 등으로 프로세스가 죽지 않도록 잡고, 다음 루프에서 위 재연결 로직이 복구.
                    Util.Logger.Log(string.Format("GrabLoop2 Error {0}", e.Message));
                    Capture2 = false;
                    Thread.Sleep(100);
                }
            }
        }

        //leess iNova2추가
        private void GrabLoop2_iNova2(object threadParam) {
            int errCnt = 0;
            int errCnt1 = 0;
            int CapCnt = 0;
            int CurrentCnt = 0;
            if(IpCam2Current.BracketInfo.Use)
                CurrentCnt = ENV.CameraEnv.IPCamera2Info.BarkectCnt;
            else
                CurrentCnt = ENV.CameraEnv.IPCamera2Info.TriggerCnt;
            while(m_keepGrab2) {
                // [수정] 연결 끊김 시 그랩 스레드가 죽어 프로세스가 종료되던 문제 방지(GrabLoop1과 동일 패턴).
                try {
                Bitmap bitmap;
                iNova2.MetaInfo metaInfo;
                iNova2.IPCamError err = m_camera2_inova2.GetImage(1000, out bitmap, out metaInfo);
                if(err == iNova2.IPCamError.OK) {
                    if(label2.Visible)
                        Util.Function.InvokeControlVisible(label2, false);
                    errCnt = 0;
                    errCnt1 = 0;
                    SetBitmap(PicLpr2Image, bitmap);
                    if(Capture2) {
                        if(IpCam2Current.BracketInfo.Use)
                            CurrentCnt = ENV.CameraEnv.IPCamera2Info.BarkectCnt;
                        else
                            CurrentCnt = ENV.CameraEnv.IPCamera2Info.TriggerCnt;
                        if(SecondDisPlayReturn != null) SecondDisPlayReturn.DisPlayTime = DateTime.Now;
                        //Bitmap savebmp;
                        Cam2ID++;

                        if(CurrentCnt == 0)
                            CurrentCnt = 1;

                        if(CapCnt < CurrentCnt) {
                            ImgCnt++;
                            string fname = ENV.CameraEnv.IPCamera2Info.ChName + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ImgCnt.ToString() + ".jpg";
                            while(true) {
                                Util.Logger.Log(string.Format("CAM2 {0}", fname));
                                if(m_camera2_inova2.SaveLastImage(fname)) {
                                    break;
                                }
                                ImgCnt++;
                                fname = ENV.CameraEnv.IPCamera2Info.ChName + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ImgCnt.ToString() + ".jpg";
                            }
                            Util.Logger.Log(string.Format("CAM2 {0} Saved", fname));
                            RECT roi = new RECT();
                            roi.x = ENV.CameraEnv.IPCamera2Info.Roi.Left;
                            roi.y = ENV.CameraEnv.IPCamera2Info.Roi.Top;
                            roi.w = ENV.CameraEnv.IPCamera2Info.Roi.Left + ENV.CameraEnv.IPCamera2Info.Roi.Width;
                            roi.h = ENV.CameraEnv.IPCamera2Info.Roi.Top + ENV.CameraEnv.IPCamera2Info.Roi.Height;
                            //dtRegList1.Rows.Add(null, 1, CapCnt + 1, fname, string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h), "", "", LastLoopTime1, false, 0, IpCam1Current.Generalinfo.Exposure);
                            clsThread.RegArray2[CapCnt].CapCnt = CapCnt;
                            clsThread.RegArray2[CapCnt].SourcePath = fname;
                            clsThread.RegArray2[CapCnt].Roi = string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h);
                            clsThread.RegArray2[CapCnt].PlateRoi = null;
                            clsThread.RegArray2[CapCnt].PlateNo = null;
                            clsThread.RegArray2[CapCnt].FirstCaptureTime = LastLoopTime2.ToString("yyyy-MM-dd HH:mm:ss");
                            clsThread.RegArray2[CapCnt].Send = false;
                            //RegArray1[CapCnt].term = 0;
                            try {
                                clsThread.RegArray2[CapCnt].Exposure = metaInfo.Exposure; //ENV.CameraEnv.IPCamera2Info.CurrentInfo.Generalinfo.Exposure;
                                clsThread.RegArray2[CapCnt].FrmaeCount = metaInfo.FrameCount;
                            } catch(Exception) { }
                            if(File.Exists(fname))
                                clsThread.RegArray2[CapCnt].Size = new System.IO.FileInfo(fname).Length;

                            if(DelayReg.Delay) {
                                Thread.Sleep(DelayReg.DelayTerm);
                            }
                            Util.Logger.Log(string.Format("****CAM2 {0} reg Start CapCnt {1} ROI {2}", fname, CapCnt, clsThread.RegArray2[CapCnt].Roi));
                            if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Ngis)
                                clsThread.RegPlateNoNgisWay(1, CapCnt);
                            else if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Elwox)
                                clsThread.RegPlateNoElwox(1, CapCnt);
                            //CoreLogic 스트로브 방식만 여기서 인식 (동영상 방식은 FAVEngine 스레드가 처리)
                            else if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic
                                && ENV.CameraEnv.RecogMode == 0) {
#if WIN64
                                CoreLogic.Reg(1, CapCnt, ENV.CameraEnv.bRegCarType);
#endif
                            }
                            else if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.OptionK) {
#if WIN64
                                OptionK.Reg(1, CapCnt, ENV.CameraEnv.bRegCarType);
#endif
                            }
                            Path2 = fname;
                            if(RegList2.Count.Equals(0)) Cam2ID = 0;
                            CapCnt++;
                        }
                    }
                    //if (Capture2 && (CapCnt == IpCam2Current.TriggerInfo.CountPerTrigger || (DateTime.Now - LastLoopTime2).TotalSeconds > 1))
                    if(Capture2 && (CapCnt == CurrentCnt))// || (DateTime.Now - LastLoopTime2).TotalSeconds > 2))
                    {
                        if(CapCnt == 0) {
                            Util.Logger.Log("Cam2 영상 취득 실패 작업 처리 누락");
                            Capture2 = false;
                            CapCnt = 0;
                        } else {
                            Capture2 = false;
                            if(DelayReg.Delay) {
                                //2체널 동시 인식 시 오류 발생 하여 지연 처리
                                Thread.Sleep(DelayReg.DelayTerm);
                                for(int i = 0; i < CapCnt; i++) {
                                    Util.Logger.Log(string.Format("****CAM2 {0} reg Start CapCnt {1} ROI {2}", clsThread.RegArray2[i].SourcePath, i, clsThread.RegArray2[i].Roi));
                                    if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Ngis)
                                        clsThread.RegPlateNoNgisWay(1, i);
                                    else if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Elwox)
                                        clsThread.RegPlateNoElwox(1, i);
                                    //Corelogic
                                }
                            }

                            CapCnt = 0;
                            Util.Logger.Log("AfterRegPlateCam Loop2");
                            // [수정] new Thread → ThreadPool
                            ThreadPool.QueueUserWorkItem(_ => {
                                try { clsThread.AfterRegPlateCam(1, ENV); }
                                catch (Exception ex) { Util.Logger.Log("AfterRegPlateCam 예외: " + ex.Message); }
                            });
                        }
                    }
                    if(ENV.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals("KJC1000")) {
                        if(!lblCam2Loop.Text.Equals("Loop Off")) {
                            TimeSpan diff = DateTime.Now - LastLoopTime2;
                            if(diff.TotalMilliseconds > ENV.LoopTerm) {
                                SetLabelText(lblCam2Loop, "Loop Off");
                                Loop2 = false;
                            }
                        }
                    }
                    try {
                        UpdateStatus2_iNova2(metaInfo);
                    } catch(Exception) { }
                } else // error or timeout.
                  {
                    if(!label2.Visible)
                        Util.Function.InvokeControlVisible(label2, true);
                    errCnt++;
                    errCnt1++;
                    if(errCnt > 100) {
                        m_camera2_inova2.ResetCamera();
                        errCnt = 0;
                        if(errCnt1 > 1000) {
                            Application.Exit();
                            Application.ExitThread();
                        }
                    }
                }

                if(!m_camera2_inova2.IsStreamPortConnected()) {
                    m_camera2_inova2.DisconnectStreamPort();
                    Thread.Sleep(100);
                    m_camera2_inova2.ConnectStreamPort(ENV.CameraEnv.IPCamera2Info.IP, ENV.CameraEnv.IPCamera2Info.StreamUdp);
                }
                if(!m_camera2_inova2.IsCommandPortConnected()) {
                    m_camera2_inova2.DisconnectCommandPort();
                    Thread.Sleep(100);
                    m_camera2_inova2.ConnectCommandPort(ENV.CameraEnv.IPCamera2Info.IP);
                }
                //leess 속도개선 : 이것때문에 반응이 느렸음.
                //Thread.Sleep(30);
                } catch(Exception e) {
                    Util.Logger.Log(string.Format("GrabLoop2_iNova2 Error {0}", e.Message));
                    Capture2 = false;
                    Thread.Sleep(100);
                }
            }
        }

        private void AddImageToRegList(int id, List<ClsStructure.RegList> List, Bitmap bmp, DateTime Captime) {
            ClsStructure.RegList item = new ClsStructure.RegList();
            item.bitmap = bmp;
            item.id = id;
            item.Job = false;
            item.result = string.Empty;
            item.FileName = string.Empty;
            item.CapTime = Captime;
            List.Add(item);
        }

        delegate void SetBitmapCallback(PictureBox Pic, Bitmap bitmap);

        private void SetBitmap(PictureBox Pic, Bitmap bitmap) {
            if(bitmap == null) return;

            if(Pic.InvokeRequired) {
                var d = new SetBitmapCallback(SetBitmap);
                if(!m_keepGrab1 && !m_keepGrab2) return;
                try {
                    this.BeginInvoke(d, new object[] { Pic, bitmap });
                } catch(ObjectDisposedException) {
                    // quitting the process?
                }
            } else {
                if(Pic.Image != null) {
                    Pic.Image.Dispose();
                    //GC.WaitForPendingFinalizers();
                }

                Pic.Image = bitmap;
                if(_serverPanel != null) MirrorToServerCard(Pic, bitmap);
            }
        }

        /// <summary>서버모드: 기존 iNova 라이브 프레임을 서버 카드에 복제 표시(cam1→카드0, cam2→카드1).</summary>
        private void MirrorToServerCard(PictureBox Pic, Bitmap bitmap) {
            try {
                if(_serverPanel == null || bitmap == null) return;
                int idx = Pic == PicLpr1Image ? 0 : (Pic == PicLpr2Image ? 1 : -1);
                if(idx < 0) return;
                CameraCard card = _serverPanel.Card(idx);
                if(card != null) card.SetImage((Image)bitmap.Clone());
            } catch { }
        }

        /// <summary>서버모드: 인식 차번을 해당 카드에 표시(camIdx 0→카드1, 1→카드2).</summary>
        public void UpdateServerCard(int camIdx, string plate) {
            try {
                if(_serverPanel == null) return;
                CameraCard card = _serverPanel.Card(camIdx);
                if(card == null) return;
                bool ok = !string.IsNullOrEmpty(plate) && plate != "No_Detection";
                card.SetPlate(plate, ok);
            } catch { }
        }

        /// <summary>서버모드 카드 차량종류 태그 갱신 — 미인식=숨김, 정기=분홍, 일반=회색. (cam0/1·서버캠 공통)</summary>
        public void UpdateServerCardType(int camIdx, string plate, bool reged) {
            try {
                if(_serverPanel == null) return;
                CameraCard card = _serverPanel.Card(camIdx);
                if(card == null) return;
                if(string.IsNullOrEmpty(plate) || plate == "No_Detection") card.SetCarType(0);
                else card.SetCarType(reged ? 2 : 1);
            } catch { }
        }

        /// <summary>서버모드 카드용: 기존 카메라(cam1/cam2) 정보를 ServerCamConfig 슬롯에 채워 카드로 표시.</summary>
        private void SetServerCardFromCam(int idx, ClsStructure.IPCamera_Basic_Setting cam, string defName, int gateType) {
            try {
                ServerCamConfig.CamSetting c = ServerCamConfig.Cams[idx];
                c.Use = true;
                c.Name = string.IsNullOrEmpty(cam.ChName) ? defName : cam.ChName;
                c.Ip = cam.IP;
                c.GateType = gateType;
                ServerCamConfig.Cams[idx] = c;
            } catch { }
        }

        bool m_warningShown = false;
        private void DisplayImageSizeWarning(int bufsize) {
            if(bufsize > m_maxBufferSizeKB * 1024 * 0.8) {
                if(!m_warningShown) {
                    m_warningShown = true;
                    SetLabelText(labelWarning, "WARNING: JPEG Quality Too High");
                }
            } else {
                if(m_warningShown) {
                    m_warningShown = false;
                    SetLabelText(labelWarning, string.Empty);
                }
            }
        }
        #endregion

        private void btnEnv_Click(object sender, EventArgs e) {
            //leess iNova2추가
            frmEnv frm = new frmEnv(ENV, m_camera1, m_camera2, m_camera1_inova2, m_camera2_inova2);

            frm.SerialDev = SerialDev;
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.ShowDialog();

            bool cam1udp = ENV.CameraEnv.IPCamera1Info.StreamUdp;
            bool cam2udp = ENV.CameraEnv.IPCamera2Info.StreamUdp;
            ENV = frm.env;
            if(!cam1udp.Equals(ENV.CameraEnv.IPCamera1Info.StreamUdp) || (!cam2udp.Equals(ENV.CameraEnv.IPCamera2Info.StreamUdp))) {
                StopCamera();
                Thread.Sleep(1000);
                StartCamera();
            }

            GetCameraInfo();
        }

        private ServerModePanel _serverPanel = null;
        private ServerCamManager _serverCamMgr = null;   // 카드3~15 추가 카메라 그랩(A단계)
        private readonly object _serverProcLock = new object();   // 서버캠 정산처리 직렬화(C단계)

        /// <summary>서버모드([OPTIONK] remote=true)면 카드 그리드(5×3) 화면을 메인 위에 띄운다.
        /// 인식은 ParkingWeb, 게이트/전광판/데이터처리는 LPR(추후 단계). 비서버모드면 아무것도 안 함.</summary>
        private void InitServerModeUI() {
            try {
                string rm = Util.Function.IniReadValue("OPTIONK", "servermode") ?? "";
                bool serverMode = rm.Equals("true", StringComparison.OrdinalIgnoreCase) || rm == "1";
                if(!serverMode) return;
                ServerCamConfig.Load();
                // 서버모드 '사용 대수'만큼 카드 표시. 개별설정은 카드 더블클릭([SVRCAM{n}]).
                int camCount = Util.Function.IntTryParse(Util.Function.IniReadValue("OPTIONK", "camcount"));
                if(camCount < 1 || camCount > ServerCamConfig.MAX) camCount = 2;
                for(int i = 0; i < ServerCamConfig.MAX; i++) {
                    ServerCamConfig.CamSetting c = ServerCamConfig.Cams[i];
                    c.Use = (i < camCount);
                    if(string.IsNullOrEmpty(c.Name) || c.Name == "CAM" + (i + 1)) {
                        if(i == 0 && !string.IsNullOrEmpty(ENV.CameraEnv.IPCamera1Info.ChName)) c.Name = ENV.CameraEnv.IPCamera1Info.ChName;
                        else if(i == 1 && !string.IsNullOrEmpty(ENV.CameraEnv.IPCamera2Info.ChName)) c.Name = ENV.CameraEnv.IPCamera2Info.ChName;
                        else c.Name = "카메라" + (i + 1);
                    }
                    ServerCamConfig.Cams[i] = c;
                }
                Util.Logger.Log(string.Format("[서버모드] 사용 대수 {0} → 카드 {0}개", camCount));
                // 주의: 여기(frmCamMain_Load 앞부분)는 아직 ENV(func.GetEnv) 로드 전이라 주입 불가.
                //  정산설정 주입은 ENV 로드 직후(frmCamMain_Load 의 clsServerCamEnv.Load)와 캡처 직전(ApplyCam)에 수행.
                _serverPanel = new ServerModePanel();
                _serverPanel.EnvClicked += delegate {
                    btnEnv.PerformClick();                          // 기존 환경설정 다이얼로그
                    this.BeginInvoke((Action)RebuildServerModeUI);  // 닫힌 뒤 화면 재구성(설정 반영)
                };
                // 카드의 캡처 버튼 → 기존 카메라 캡처 핸들러 직접 호출(가려진 버튼이라 PerformClick은 안 먹힘)
                foreach(CameraCard card in _serverPanel.Cards) {
                    int ci = card.Index;
                    // 입출구 태그 = 카메라별 InOutType([SVRCAM] pc_CmbLPRInOut1). 출구=출차(적), 입구=입차(녹)
                    string _io = Util.Function.IniReadValue("SVRCAM" + (ci + 1), "pc_CmbLPRInOut1") ?? "";
                    card.SetGate(_io.Contains("출구"));
                    card.SetCarType(0);   // 인식 전엔 차량종류 태그 숨김
                    card.CaptureClicked += delegate {
                        try {
                            Util.Logger.Log(string.Format("[서버모드] 카드{0} 캡처 클릭", ci + 1));
                            if(ci == 0) btnCam1Capture_Click(this, EventArgs.Empty);
                            else if(ci == 1) btnCam2Capture_Click(this, EventArgs.Empty);
                            else if(ci >= 2 && _serverCamMgr != null) _serverCamMgr.Capture(ci);   // 카드3~15(B단계)
                        } catch(Exception ex) { Util.Logger.Log("[서버모드] 캡처 오류: " + ex.Message); }
                    };
                    // Alt+S TEST 모드: 카드 TEST 버튼(차번 수동입력 정산) / 우클릭(사진선택 인식)
                    card.TestClicked += delegate {
                        try { OnCardTest(ci, _serverPanel.Card(ci).TestPlate); }
                        catch(Exception ex) { Util.Logger.Log("[서버모드] 카드 TEST 오류: " + ex.Message); }
                    };
                    card.TestPhotoClicked += delegate {
                        try { OnCardTestPhoto(ci); }
                        catch(Exception ex) { Util.Logger.Log("[서버모드] 카드 TEST 사진 오류: " + ex.Message); }
                    };
                    // 카드 더블클릭 → 환경설정을 '서버모드 반대' 필터로 열어 카메라 개별설정([SVRCAM{n}] 별도저장)
                    card.CardDoubleClicked += delegate {
                        try {
                            Util.Logger.Log(string.Format("[서버모드] 카드{0} 더블클릭 — 카메라 개별설정", ci + 1));
                            frmEnv frm = new frmEnv(ENV, m_camera1, m_camera2, m_camera1_inova2, m_camera2_inova2);
                            frm.SerialDev = SerialDev;
                            frm.StartPosition = FormStartPosition.CenterParent;
                            // 서버캠(3~15) 라이브 디바이스 + 영역설정용 스냅샷 전달(고급설정/영역설정 동작용)
                            try {
                                string roiImg = null;
                                CameraCard cc = _serverPanel.Card(ci);
                                if(cc != null) {
                                    Image snap = cc.GetSnapshot();
                                    if(snap != null) {
                                        roiImg = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "svrroi_" + (ci + 1) + ".jpg");
                                        using(Bitmap b = new Bitmap(snap)) b.Save(roiImg, System.Drawing.Imaging.ImageFormat.Jpeg);
                                        snap.Dispose();
                                    }
                                }
                                frm.SetServerCamDevice(_serverCamMgr != null ? _serverCamMgr.GetDevice(ci) : null, roiImg);
                            } catch(Exception ex2) { Util.Logger.Log("[서버모드] 디바이스/스냅샷 전달 오류: " + ex2.Message); }
                            frm.SetServerCamMode(ci);   // 반대 필터 + 개별 저장 모드
                            frm.ShowDialog();
                            this.BeginInvoke((Action)RebuildServerModeUI);   // 닫힌 뒤 카드 이름 등 갱신
                        } catch(Exception ex) { Util.Logger.Log("[서버모드] 개별설정 열기 오류: " + ex.Message); }
                    };
                }
                this.Controls.Add(_serverPanel);
                _serverPanel.BringToFront();
                // 카드 3~15(인덱스 2~14) 추가 카메라 연결·영상(A단계). 카드 1,2는 기존 GrabLoop1/2가 미러링.
                if(_serverCamMgr != null) { _serverCamMgr.Dispose(); _serverCamMgr = null; }
                _serverCamMgr = new ServerCamManager();
                _serverCamMgr.Start(camCount, delegate(int idx) { return _serverPanel.Card(idx); });
                // 가로 5대 기준, 사용 대수에 맞춰 폼 크기 자동 조정.
                // Load 뒷부분(this.Width=1000 등)이 덮어쓰므로 BeginInvoke 로 '그 이후'에 적용 + 화면크기 클램프.
                try {
                    int cols = Math.Min(camCount, ServerCamConfig.COLS);
                    int rows = (camCount + ServerCamConfig.COLS - 1) / ServerCamConfig.COLS;
                    int wantW = Math.Max(900, cols * 298 + 26);          // 카드 286 + 여백 12
                    int wantH = Math.Max(520, 52 + rows * 266 + 26);     // 카드 248 + 여백 12 + 여유
                    this.BeginInvoke((Action)delegate {
                        try {
                            this.WindowState = FormWindowState.Normal;
                            System.Drawing.Rectangle wa = Screen.FromControl(this).WorkingArea;
                            this.ClientSize = new System.Drawing.Size(Math.Min(wantW, wa.Width), Math.Min(wantH, wa.Height));
                            this.CenterToScreen();
                        } catch { }
                    });
                } catch { }
                Util.Logger.Log(string.Format("[서버모드] 카드 화면 표시 — 사용 카메라 {0}개", ServerCamConfig.ActiveCount()));
            } catch(Exception ex) {
                Util.Logger.Log("[서버모드] UI 초기화 오류: " + ex.Message);
            }
        }

        /// <summary>환경설정 닫힌 뒤 서버모드 화면 재구성(카메라 추가/서버모드 해제 반영).</summary>
        private void RebuildServerModeUI() {
            try {
                if(_serverCamMgr != null) { _serverCamMgr.Dispose(); _serverCamMgr = null; }
                if(_serverPanel != null) {
                    this.Controls.Remove(_serverPanel);
                    _serverPanel.Dispose();
                    _serverPanel = null;
                }
                InitServerModeUI();   // 서버모드면 새로 생성, 해제됐으면 안 만들고 기존 화면 노출
            } catch(Exception ex) {
                Util.Logger.Log("[서버모드] 재구성 오류: " + ex.Message);
            }
        }

        /// <summary>서버캠(인덱스 2~14) 캡처·인식 결과 → 정산처리(이미지저장 + DataProcess: 게이트개방/입출차기록/정기권/소켓) + 카드표시. C단계.
        /// 카드 1,2(인덱스 0,1)는 기존 AfterRegPlateCam 경로 사용. 백그라운드 스레드에서 호출됨.</summary>
        public void ProcessServerCamResult(int camIdx, string plate, System.Drawing.Image snapshot, DateTime captureTime) {
            try {
                if(string.IsNullOrEmpty(plate)) plate = "No_Detection";
                clsServerCamEnv.ApplyCam(camIdx);   // ENV 재대입 대비 — 현재 ENV 에 이 카메라 게이트포트/장비 재주입
                ClsStructure.Lpr_Info lpr = clsServerCamEnv.GetLpr(camIdx);
                string chName = string.IsNullOrEmpty(lpr.ChNo) ? ("SVR" + (camIdx + 1)) : lpr.ChNo;
                string datePath;
                if(ENV.CameraEnv.SockDataFormat.Equals((int)ClsStructure.SockFormat.Nexpa))
                    datePath = string.Format(@"{0}\{1}\{2}", captureTime.Year.ToString("D4"), captureTime.Month.ToString("D2"), captureTime.Day.ToString("D2"));
                else
                    datePath = captureTime.ToString("yyyyMMdd");
                // 이미지 저장: {SavePath}\{datePath}\{ChName}_{plate}_{yyyyMMddHHmmss}.jpg (cam1/2와 동일 규칙)
                string fileName = string.Format("{0}_{1}_{2}.jpg", chName, plate, captureTime.ToString("yyyyMMddHHmmss"));
                if(snapshot != null) {
                    try {
                        string dir = string.Format(@"{0}\{1}", ENV.CameraEnv.ImageSave.SavePath, datePath);
                        if(!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        using(Bitmap b = new Bitmap(snapshot))
                            b.Save(System.IO.Path.Combine(dir, fileName), System.Drawing.Imaging.ImageFormat.Jpeg);
                        clsImageUploader.Enqueue(System.IO.Path.Combine(dir, fileName), datePath, fileName);   // ParkingWeb 업로드
                    } catch(Exception se) { Util.Logger.Log("[서버캠] 이미지 저장 오류: " + se.Message); }
                }

                // 정산처리: 일반화된 DataProcess(게이트/입출차/정기권). 모든 카메라 공유락으로 직렬화(DB연결 동시사용 방지).
                string log; bool reged;
                lock(clsDataTransaction.ProcLock) {
                    log = DataProcess.DataProcess(lpr.InOutType, ENV, camIdx, plate, fileName, captureTime.ToString("yyyy-MM-dd HH:mm:ss"), 0);
                    reged = DataProcess.GetRegedCar(camIdx);   // 정기차량 여부(전광판 표시용)
                }
                Util.Logger.Log(string.Format("[서버캠{0}] 정산처리 차번={1} 장비={2}({3}) → {4}",
                    camIdx + 1, plate, chName, lpr.InOutType == 1 ? "출구" : "입구", (log ?? "").Replace('\n', ' ')));
                UpdateServerCard(camIdx, plate);
                UpdateServerCardType(camIdx, plate, reged);   // 일반/정기 태그
                ServerCamDisplay(camIdx, plate, reged);   // 카메라별 네트워크 전광판 출력(D단계)
            } catch(Exception ex) { Util.Logger.Log("[서버캠] 정산처리 오류: " + ex.Message); }
        }

        // 서버캠(idx>=2) 네트워크 전광판 인스턴스 캐시
        private static readonly System.Collections.Generic.Dictionary<int, NetworkDisplay> _svrNetDisp = new System.Collections.Generic.Dictionary<int, NetworkDisplay>();

        /// <summary>서버캠(3~15) 네트워크 전광판을 시작 시 미리 생성·연결(welcome 루프 시작). 카메라1·2(NetDisPlay1/2)와 동일하게
        /// 시작 시 연결해 두어, 첫 인식 때 지연 연결로 첫 송신이 누락되는 문제를 막는다. Net 미설정 카메라는 건너뜀(null).</summary>
        private void InitServerCamDisplays() {
            try {
                if(!clsServerCamEnv.ServerMode) return;
                int camCount = Util.Function.IntTryParse(Util.Function.IniReadValue("OPTIONK", "camcount"));
                if(camCount < 1 || camCount > 15) camCount = 2;
                for(int i = 2; i < camCount && i < 15; i++) {
                    clsServerCamEnv.ApplyCam(i);                 // DisPlay[i].Net 주입 보장(혹시 Load 후 ENV 재대입 대비)
                    NetworkDisplay nd = NetDevForServer(i);      // Net.Use 면 생성+연결+welcome 루프
                    Util.Logger.Log(string.Format("[서버캠{0}] 전광판 시작연결 {1}", i + 1, nd != null ? "완료" : "미설정/건너뜀"));
                }
            } catch(Exception ex) { Util.Logger.Log("[서버캠] 전광판 시작연결 오류: " + ex.Message); }
        }

        /// <summary>서버캠(idx>=2)의 네트워크 전광판 인스턴스(없으면 생성·캐시·환영문구 루프 시작). Net 미설정이면 null.</summary>
        public static NetworkDisplay NetDevForServer(int camIdx) {
            try {
                lock(_svrNetDisp) {
                    NetworkDisplay nd;
                    if(_svrNetDisp.TryGetValue(camIdx, out nd)) return nd;
                    if(camIdx < 2 || ENV.CommunicationEnv.DisPlay == null || camIdx >= ENV.CommunicationEnv.DisPlay.Length) return null;
                    ClsStructure.DisPlay_Info disp = ENV.CommunicationEnv.DisPlay[camIdx];
                    if(!disp.Net.Use || string.IsNullOrEmpty(disp.Net.IP)) return null;
                    nd = new NetworkDisplay();
                    nd.Tag = "카메라" + (camIdx + 1);
                    nd.Init(disp.Net.IP, disp.Net.Port, "TCP");
                    nd.DisPlayTime = DateTime.Now.AddSeconds(-10);
                    nd.Color1 = clsFunction.GetColor8Int(disp.Ment.Ment1Color);
                    nd.Color2 = clsFunction.GetColor8Int(disp.Ment.Ment2Color);
                    nd.Ment1 = disp.Ment.Ment1Line;
                    nd.Ment2 = disp.Ment.Ment2Line;
                    nd.Term = 5;
                    nd.Entrance_Type = clsServerCamEnv.GetInOut(camIdx) == (int)ClsStructure.InoutType.입구용;
                    nd.ReturnStart();   // 평소(차량 없을 때) 환영문구 표시 루프
                    _svrNetDisp[camIdx] = nd;
                    Util.Logger.Log(string.Format("[서버캠{0}] 네트워크 전광판 연결 {1}:{2}", camIdx + 1, disp.Net.IP, disp.Net.Port));
                    return nd;
                }
            } catch(Exception ex) { Util.Logger.Log("[서버캠] 전광판 생성오류: " + ex.Message); return null; }
        }

        /// <summary>전광판 2열 차번 우측정렬 — 카메라1·2 의 CarNoSpace 와 동일(12바이트 폭, 좌측 공백 패딩).
        /// 한글은 CP949 2바이트로 계산(KOR). 서버캠도 카메라1·2 처럼 우측정렬되도록.</summary>
        private static string PlateRightAlign(string carno) {
            if(string.IsNullOrEmpty(carno)) return carno;
            int len;
            try { len = System.Text.Encoding.Default.GetByteCount(carno); } catch { len = carno.Length; }
            return (len < 12 ? new string(' ', 12 - len) : "") + carno;
        }

        /// <summary>서버캠 인식결과를 네트워크 전광판에 출력(정기/일반). 미인식은 환영문구 유지(미송신).</summary>
        private void ServerCamDisplay(int camIdx, string plate, bool reged) {
            try {
                if(camIdx < 2 || ENV.CommunicationEnv.DisPlay == null || camIdx >= ENV.CommunicationEnv.DisPlay.Length) return;
                ClsStructure.DisPlay_Info disp = ENV.CommunicationEnv.DisPlay[camIdx];
                if(!disp.Net.Use) return;
                if(string.IsNullOrEmpty(plate) || plate == "No_Detection") return;   // 미인식 → 환영문구 유지
                NetworkDisplay nd = NetDevForServer(camIdx);
                if(nd == null) return;
                string line1, color1, color2;
                if(reged) { line1 = string.IsNullOrEmpty(disp.PeriodCar) ? "정기차량" : disp.PeriodCar; color1 = disp.Period1Color; color2 = disp.Period2Color; }
                else      { line1 = string.IsNullOrEmpty(disp.NormalCar) ? "일반차량" : disp.NormalCar; color1 = disp.Normal1Color; color2 = disp.Normal2Color; }
                nd.SendMsg(line1, clsFunction.GetColor8Int(color1), PlateRightAlign(plate), clsFunction.GetColor8Int(color2));
                nd.DisPlayTime = DateTime.Now;   // 차량 문구 Term 초 유지 후 환영문구 자동 복귀
                Util.Logger.Log(string.Format("[서버캠{0}] 전광판 출력 '{1}' '{2}'", camIdx + 1, line1, plate));
            } catch(Exception ex) { Util.Logger.Log("[서버캠] 전광판 출력오류: " + ex.Message); }
        }

        /// <summary>Alt+S 카드 TEST 버튼 — 차번 수동입력으로 정산(기존 TEST 와 동일). 카드0/1은 기존 핸들러 재사용.</summary>
        private void OnCardTest(int ci, string plate) {
            if(string.IsNullOrEmpty(plate)) { MessageBox.Show("차번을 입력하세요."); return; }
            if(ci == 0 || ci == 1) {
                txtTestCarNo.Text = plate;
                if(ci == 0) btnTestCapture1_Click(this, EventArgs.Empty);
                else btnTestCapture2_Click(this, EventArgs.Empty);
                // 카드 표시 갱신(차번 + 일반/정기 태그) — 서버캠과 동일하게 카드에 보이도록
                UpdateServerCard(ci, plate);
                try { UpdateServerCardType(ci, plate, DataProcess.GetRegedCar(ci)); } catch { }
            }
            else {
                Thread t = new Thread(delegate () {
                    try { ProcessServerCamResult(ci, plate, null, DateTime.Now); }
                    catch(Exception ex) { Util.Logger.Log("[서버캠] 수동TEST 오류: " + ex.Message); }
                });
                t.IsBackground = true; t.Start();
            }
        }

        /// <summary>Alt+S 카드 TEST 우클릭 — 사진선택 후 인식하여 정산(기존 TEST 우클릭과 동일).</summary>
        private void OnCardTestPhoto(int ci) {
            if(ci == 0) { BtnTestCapture1_MouseUp(btnTestCapture1, new MouseEventArgs(MouseButtons.Right, 1, 0, 0, 0)); return; }
            if(ci == 1) { BtnTestCapture2_MouseUp(btnTestCapture2, new MouseEventArgs(MouseButtons.Right, 1, 0, 0, 0)); return; }
            // 서버캠(2~14): 사진선택 → ParkingWeb 인식 → 정산
            OpenFileDialog dlg = new OpenFileDialog();
            if(dlg.ShowDialog() != DialogResult.OK) return;
            string file = dlg.FileName;
            Thread t = new Thread(delegate () {
                try {
                    System.Collections.Generic.Dictionary<string, object> res = OptionK.RecognizeImage(file, "");
                    string plate = (res != null && res.ContainsKey("plate")) ? Convert.ToString(res["plate"]) : "";
                    if(string.IsNullOrEmpty(plate)) plate = "No_Detection";
                    using(Image img = Image.FromFile(file))
                        ProcessServerCamResult(ci, plate, img, DateTime.Now);
                } catch(Exception ex) { Util.Logger.Log("[서버캠] 사진TEST 오류: " + ex.Message); }
            });
            t.IsBackground = true; t.Start();
        }

        private void frmCamMain_Load(object sender, EventArgs e) {
            try {
                //프로그램 버전 제목 표시줄 컴파일 일자 설정
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                DateTime dt = new DateTime(2000, 1, 1);
                this.Text += dt.AddDays(version.Build).ToString(" Ver:yyyyMMdd");
                Util.Logger.Log("프로그램 기동");
                InitServerModeUI();   // 서버모드면 카드 그리드 화면(5×3)으로 전환
                //20161124 Start
                if(!Directory.Exists(Directory.GetCurrentDirectory() + "\\Back")) {
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Back");
                }
                //20161124 End
                try {
                    Util.Logger.Log("이미지 파일 삭제");
                    DirectoryInfo dir = new DirectoryInfo(Directory.GetCurrentDirectory());
                    FileInfo[] files = dir.GetFiles("*.jpg");
                    for(int i = 0; i < files.Length; i++) {
                        files[i].Delete();
                    }
                } catch(Exception FielDelete) {
                    Util.Logger.Log(string.Format("FielDelete Error {0}", FielDelete));
                }
                try {
                    Util.Logger.Log("로그 파일 삭제");
                    DirectoryInfo di = new DirectoryInfo(string.Format("{0}\\Log", Directory.GetCurrentDirectory()));
                    FileInfo[] files = di.GetFiles();
                    for(int i = 0; i < files.Length; i++) {
                        if((DateTime.Now - files[i].LastWriteTime).TotalDays > 60) {
                            files[i].Delete();
                        }
                    }
                } catch(Exception FielDelete) {
                    Util.Logger.Log(string.Format("FielDelete Error {0}", FielDelete));
                }
                ENV = func.GetEnv(ENV);
                clsServerCamEnv.Load();   // 서버모드 카메라 2~14 정산설정([SVRCAM] 장비/게이트) → ENV 주입(ENV 로드 직후)
                InitServerCamDisplays();  // 서버캠(3~15) 네트워크 전광판 시작 시 선연결(카메라1·2와 동일) — 첫 송신 누락 방지

                BeforeCalOpt.Load();
                clsOutService.Load();
                clsBusinessCar.ReadIni();
                NoDriving.Load();
                this.Show();
                ENV.TestMode = false;
                //Environment.Is64BitOperatingSystem();
                Util.Logger.Log(string.Format("Os 64Bit {0}", Environment.Is64BitOperatingSystem));
                Util.Logger.Log(string.Format("Process 64Bit {0}", Environment.Is64BitProcess));
                Util.Logger.Log("테스트 모드 : " + ENV.TestMode.ToString());
                FullSpaceControl.LoadFullSpace(ENV.CommonEnv.DBInfo);
                SerialDev = new clsSerialPort(ENV);
                Util.Logger.Log("전광판 설정");
                DataProcess = new clsDataTransaction(SerialDev, ENV);
                ENV.RegCarControl = ENV.RegCarControl.Load();
                // [동작모드] CAM 모드에서도 자료처리 수행 → 전광판 설정도 전 모드 공통 적용
                // (전광판 미사용이면 내부 if(DisPlay.Use) 가드로 자동 skip)
                {
                    // 전광판1 설정 — 잘못된 IP/Port/장비타입이면 메시지박스 + 계속 진행
                    try {
                    if(ENV.CommunicationEnv.DisPlay[0].Use) {
                        if(ENV.CommunicationEnv.DisPlay[0].Net.Use) {
                            if(ENV.CameraEnv.CoreCountry == CoreLogic.THA)
                                NetDisPlay1.CharCode = 0x01;
                            NetDisPlay1 = new NetworkDisplay();
                            NetDisPlay1.Tag = "카메라1";
                            NetDisPlay1.Init(ENV.CommunicationEnv.DisPlay[0].Net.IP, ENV.CommunicationEnv.DisPlay[0].Net.Port, "TCP");
                            NetDisPlay1.DisPlayTime = DateTime.Now.AddSeconds(-10);
                            NetDisPlay1.Color1 = clsFunction.GetColor8Int(ENV.CommunicationEnv.DisPlay[0].Ment.Ment1Color);
                            NetDisPlay1.Color2 = clsFunction.GetColor8Int(ENV.CommunicationEnv.DisPlay[0].Ment.Ment2Color);
                            if(FullSpaceControl.Use) {
                                if(ENV.CommunicationEnv.Lpr1Info.InOutType == (int)ClsStructure.InoutType.입구용) {
                                    NetDisPlay1.FullMent = new byte[][] { FullSpaceControl.FullMent1, FullSpaceControl.FullMent2 };
                                    NetDisPlay1.isFull = FullSpaceControl.FullCheck(ENV.CommonEnv.DBInfo, ENV.CommunicationEnv.ParkInfo.No, ENV.CommunicationEnv.ParkInfo.Client_No);
                                }
                            }
                            NetDisPlay1.Ment1 = ENV.CommunicationEnv.DisPlay[0].Ment.Ment1Line;
                            NetDisPlay1.Ment2 = ENV.CommunicationEnv.DisPlay[0].Ment.Ment2Line;
                            NetDisPlay1.Term = 5;
                            NetDisPlay1.Entrance_Type = ENV.CommunicationEnv.Lpr1Info.InOutType == (int)ClsStructure.InoutType.입구용;
                            NetDisPlay1.ReturnStart();
                        } else {
                            FirstDisPlayReturn = new SerialDevice.ReturnDisPlay();
                            FirstDisPlayReturn.DisPlayTime = DateTime.Now.AddSeconds(-10);
                            if(ENV.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString())) {
                                FirstDisPlayReturn.DisPlay8 = SerialDev.FirstDisPlay8;
                                FirstDisPlayReturn.Color1 = clsFunction.GetColor8Int(ENV.CommunicationEnv.DisPlay[0].Ment.Ment1Color);
                                FirstDisPlayReturn.Color2 = clsFunction.GetColor8Int(ENV.CommunicationEnv.DisPlay[0].Ment.Ment2Color);
                                if(FullSpaceControl.Use) {
                                    if(ENV.CommunicationEnv.Lpr1Info.InOutType == (int)ClsStructure.InoutType.입구용) {
                                        FirstDisPlayReturn.FullMent = new byte[][] { FullSpaceControl.FullMent1, FullSpaceControl.FullMent2 };
                                        FirstDisPlayReturn.isFull = FullSpaceControl.FullCheck(ENV.CommonEnv.DBInfo, ENV.CommunicationEnv.ParkInfo.No, ENV.CommunicationEnv.ParkInfo.Client_No);
                                    }
                                }
                            } else if(ENV.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString())) {
                                FirstDisPlayReturn.DisPlay3 = SerialDev.FirstDisPlay3;
                                FirstDisPlayReturn.Color1 = clsFunction.GetColor3Int(ENV.CommunicationEnv.DisPlay[0].Ment.Ment1Color);
                                FirstDisPlayReturn.Color2 = clsFunction.GetColor3Int(ENV.CommunicationEnv.DisPlay[0].Ment.Ment2Color);
                            } else if(ENV.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString())) {
                                FirstDisPlayReturn.DisPlayTime = DateTime.Now.AddSeconds(-10);
                                FirstDisPlayReturn.DisPlayAmano3 = SerialDev.FirstDisPlayAmano3;
                                FirstDisPlayReturn.Color1 = (int)clsFunction.GetAmanoColor3uInt(ENV.CommunicationEnv.DisPlay[0].Ment.Ment1Color);
                                FirstDisPlayReturn.Color2 = (int)clsFunction.GetAmanoColor3uInt(ENV.CommunicationEnv.DisPlay[0].Ment.Ment2Color);
                                //if (FullSpaceControl.Use)
                                //{
                                //    if (ENV.CommunicationEnv.Lpr1Info.InOutType == (int)ClsStructure.InoutType.입구용)
                                //    {
                                //        FirstDisPlayReturn.FullMent = new byte[][] { FullSpaceControl.FullMent1, FullSpaceControl.FullMent2 };
                                //        FirstDisPlayReturn.isFull = FullSpaceControl.FullCheck(ENV.CommonEnv.DBInfo, ENV.CommunicationEnv.ParkInfo.No, ENV.CommunicationEnv.ParkInfo.Client_No);
                                //    }
                                //}
                            }
                            FirstDisPlayReturn.Ment1 = ENV.CommunicationEnv.DisPlay[0].Ment.Ment1Line;
                            FirstDisPlayReturn.Ment2 = ENV.CommunicationEnv.DisPlay[0].Ment.Ment2Line;
                            FirstDisPlayReturn.Term = 5;

                            FirstDisPlayReturn.ReturnStart();
                        }
                    }
                    } catch(Exception disp1Ex) {
                        Util.Logger.Log("[전광판1 설정 오류] " + disp1Ex.ToString());
                        MessageBox.Show(
                            "전광판 1번 설정에 문제가 있습니다.\n\n"
                            + "확인할 항목:\n"
                            + "  - 네트워크 전광판: IP/Port가 올바른가? (빈 값/0 금지)\n"
                            + "  - 시리얼 전광판: COM 포트가 존재하는가? Dev_Type_Name이 올바른가?\n"
                            + "  - 멘트 색상 코드가 유효한가?\n\n"
                            + "예외: " + disp1Ex.GetType().Name + "\n"
                            + "메시지: " + disp1Ex.Message + "\n\n"
                            + "(이 메시지는 전광판 1번에만 해당. 프로그램은 계속 실행됩니다.)",
                            "전광판 1번 설정 오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    // 전광판2 설정 — 잘못된 IP/Port/장비타입이면 메시지박스 + 계속 진행
                    try {
                    if(ENV.CommunicationEnv.DisPlay[1].Use) {
                        if(ENV.CommunicationEnv.DisPlay[1].Net.Use) {
                            if(ENV.CameraEnv.CoreCountry == CoreLogic.THA)
                                NetDisPlay2.CharCode = 0x01;
                            NetDisPlay2 = new NetworkDisplay();
                            NetDisPlay2.Tag = "카메라2";
                            NetDisPlay2.Init(ENV.CommunicationEnv.DisPlay[1].Net.IP, ENV.CommunicationEnv.DisPlay[1].Net.Port, "TCP");
                            NetDisPlay2.DisPlayTime = DateTime.Now.AddSeconds(-10);
                            NetDisPlay2.Color1 = clsFunction.GetColor8Int(ENV.CommunicationEnv.DisPlay[1].Ment.Ment1Color);
                            NetDisPlay2.Color2 = clsFunction.GetColor8Int(ENV.CommunicationEnv.DisPlay[1].Ment.Ment2Color);
                            if(FullSpaceControl.Use) {
                                if(ENV.CommunicationEnv.Lpr2Info.InOutType == (int)ClsStructure.InoutType.입구용) {
                                    NetDisPlay2.FullMent = new byte[][] { FullSpaceControl.FullMent1, FullSpaceControl.FullMent2 };
                                    NetDisPlay2.isFull = FullSpaceControl.FullCheck(ENV.CommonEnv.DBInfo, ENV.CommunicationEnv.ParkInfo.No, ENV.CommunicationEnv.ParkInfo.Client_No);
                                }
                            }
                            NetDisPlay2.Ment1 = ENV.CommunicationEnv.DisPlay[1].Ment.Ment1Line;
                            NetDisPlay2.Ment2 = ENV.CommunicationEnv.DisPlay[1].Ment.Ment2Line;
                            NetDisPlay2.Term = 5;
                            NetDisPlay2.Entrance_Type = ENV.CommunicationEnv.Lpr2Info.InOutType == (int)ClsStructure.InoutType.입구용;
                            NetDisPlay2.ReturnStart();
                        } else {
                            SecondDisPlayReturn = new SerialDevice.ReturnDisPlay();
                            SecondDisPlayReturn.DisPlayTime = DateTime.Now.AddSeconds(-10);
                            if(ENV.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString())) {
                                SecondDisPlayReturn.DisPlay8 = SerialDev.SecondDisPlay8;
                                SecondDisPlayReturn.Color1 = clsFunction.GetColor8Int(ENV.CommunicationEnv.DisPlay[1].Ment.Ment1Color);
                                SecondDisPlayReturn.Color2 = clsFunction.GetColor8Int(ENV.CommunicationEnv.DisPlay[1].Ment.Ment2Color);
                                if(FullSpaceControl.Use) {
                                    if(ENV.CommunicationEnv.Lpr1Info.InOutType == (int)ClsStructure.InoutType.입구용) {
                                        SecondDisPlayReturn.FullMent = new byte[][] { FullSpaceControl.FullMent1, FullSpaceControl.FullMent2 };
                                        SecondDisPlayReturn.isFull = FullSpaceControl.FullCheck(ENV.CommonEnv.DBInfo, ENV.CommunicationEnv.ParkInfo.No, ENV.CommunicationEnv.ParkInfo.Client_No);
                                    }
                                }
                            } else if(ENV.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString())) {
                                SecondDisPlayReturn.DisPlay3 = SerialDev.SecondDisPlay3;
                                SecondDisPlayReturn.Color1 = clsFunction.GetColor3Int(ENV.CommunicationEnv.DisPlay[1].Ment.Ment1Color);
                                SecondDisPlayReturn.Color2 = clsFunction.GetColor3Int(ENV.CommunicationEnv.DisPlay[1].Ment.Ment2Color);
                            } else if(ENV.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString())) {
                                SecondDisPlayReturn.DisPlayTime = DateTime.Now.AddSeconds(-10);
                                SecondDisPlayReturn.DisPlayAmano3 = SerialDev.SecondDisPlayAmano3;
                                SecondDisPlayReturn.Color1 = (int)clsFunction.GetAmanoColor3uInt(ENV.CommunicationEnv.DisPlay[0].Ment.Ment1Color);
                                SecondDisPlayReturn.Color2 = (int)clsFunction.GetAmanoColor3uInt(ENV.CommunicationEnv.DisPlay[0].Ment.Ment2Color);
                                //if (FullSpaceControl.Use)
                                //{
                                //    if (ENV.CommunicationEnv.Lpr1Info.InOutType == (int)ClsStructure.InoutType.입구용)
                                //    {
                                //        FirstDisPlayReturn.FullMent = new byte[][] { FullSpaceControl.FullMent1, FullSpaceControl.FullMent2 };
                                //        FirstDisPlayReturn.isFull = FullSpaceControl.FullCheck(ENV.CommonEnv.DBInfo, ENV.CommunicationEnv.ParkInfo.No, ENV.CommunicationEnv.ParkInfo.Client_No);
                                //    }
                                //}
                            }
                            SecondDisPlayReturn.Ment1 = ENV.CommunicationEnv.DisPlay[1].Ment.Ment1Line;
                            SecondDisPlayReturn.Ment2 = ENV.CommunicationEnv.DisPlay[1].Ment.Ment2Line;
                            SecondDisPlayReturn.Term = 5;
                            SecondDisPlayReturn.ReturnStart();
                        }
                    }
                    } catch(Exception disp2Ex) {
                        Util.Logger.Log("[전광판2 설정 오류] " + disp2Ex.ToString());
                        MessageBox.Show(
                            "전광판 2번 설정에 문제가 있습니다.\n\n"
                            + "확인할 항목:\n"
                            + "  - 네트워크 전광판: IP/Port가 올바른가? (빈 값/0 금지)\n"
                            + "  - 시리얼 전광판: COM 포트가 존재하는가? Dev_Type_Name이 올바른가?\n"
                            + "  - 멘트 색상 코드가 유효한가?\n\n"
                            + "예외: " + disp2Ex.GetType().Name + "\n"
                            + "메시지: " + disp2Ex.Message + "\n\n"
                            + "(이 메시지는 전광판 2번에만 해당. 프로그램은 계속 실행됩니다.)",
                            "전광판 2번 설정 오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                if(ENV.RegCarControl.Regautodeluse) {
                    Thread t = new Thread(new ThreadStart(regdelete));
                    t.IsBackground = true;
                    t.Start();
                }
                //인식 결과 저장 Datatable 컬럼 생성
                DataColumn dc = dtRegList1.Columns.Add("idx", typeof(int));
                dc.AutoIncrement = true;
                dc.AutoIncrementSeed = 1;
                dc.AutoIncrementStep = 1;
                dtRegList1.Columns.Add("Camindex", typeof(int));
                dtRegList1.Columns.Add("CapCnt", typeof(int));
                dtRegList1.Columns.Add("SourcePath", typeof(String));
                dtRegList1.Columns.Add("Roi", typeof(String));
                dtRegList1.Columns.Add("PlateRoi", typeof(String));
                dtRegList1.Columns.Add("PlateNo", typeof(String));
                dtRegList1.Columns.Add("FirstCaptureTime", typeof(String));
                dtRegList1.Columns.Add("Send", typeof(bool));
                dtRegList1.Columns.Add("term", typeof(long));
                dtRegList1.Columns.Add("Exposure", typeof(long));

                DataColumn dc2 = dtRegList2.Columns.Add("idx", typeof(int));
                dc2.AutoIncrement = true;
                dc2.AutoIncrementSeed = 1;
                dc2.AutoIncrementStep = 1;
                dtRegList2.Columns.Add("Camindex", typeof(int));
                dtRegList2.Columns.Add("CapCnt", typeof(int));
                dtRegList2.Columns.Add("SourcePath", typeof(String));
                dtRegList2.Columns.Add("Roi", typeof(String));
                dtRegList2.Columns.Add("PlateRoi", typeof(String));
                dtRegList2.Columns.Add("PlateNo", typeof(String));
                dtRegList2.Columns.Add("FirstCaptureTime", typeof(String));
                dtRegList2.Columns.Add("Send", typeof(bool));
                dtRegList2.Columns.Add("term", typeof(long));
                dtRegList2.Columns.Add("Exposure", typeof(long));
                Util.Logger.Log("폼 설정");
                FormSize(ENV.CameraEnv.IPCamera2Info.Use);

                ExtendLprtrns = DataProcess.CheckLprtrns();
                if(ExtendLprtrns)
                    Util.Logger.Log("LPR촬영 로그 확장");
                if(ENV.StartType != (int)ClsStructure.ProgramStartType.COM) {
                    Util.Logger.Log("인식 모듈 설정");
                    if(Environment.Is64BitProcess && (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Elwox || ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Ngis)) {
                        MessageBox.Show("64비트 프로세스에서 32비트 인식 모듈이 선택 되었습니다!!!\r\n환경설정을 확인 하세요!!!", "인식 모듈 설정 오류", MessageBoxButtons.OK);
                        btnEnv.PerformClick();
                        this.Close();
                    } else if(!Environment.Is64BitProcess && ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic) {
                        MessageBox.Show("32비트 프로세스에서 64비트 인식 모듈이 선택 되었습니다!!!\r\n환경설정을 확인 하세요!!!", "인식 모듈 설정 오류", MessageBoxButtons.OK);
                        btnEnv.PerformClick();
                        this.Close();
                    }

                    if(ENV.StartType != (int)ClsStructure.ProgramStartType.COM) {
                        Util.Logger.Log("인식모듈 설정");
                        switch(ENV.CameraEnv.RegModule) {
                            case (int)ClsStructure.RegModule.Elwox:
                                Util.Logger.Log("Elwox");
                                if(IntPtr.Size == 4) {
                                    Thread Elan = new Thread(new ThreadStart(ElanOpen));
                                    Elan.Start();
                                } else {
                                    Util.Logger.Log("32bit process가 아닙니다.");
                                }
                                break;
                            case (int)ClsStructure.RegModule.Ngis:
                                Util.Logger.Log("Ngis");
                                if(IntPtr.Size == 4) {
                                    string tmp = NgisWay_Module.Module_Init();
                                    NgisWay.SendResult += new NgisWay_Module.eventRegDelegate(PlateRegResult);
                                } else {
                                    Util.Logger.Log("32bit process가 아닙니다.");
                                }
                                break;
#if WIN64
                            case (int)ClsStructure.RegModule.CoreLogic:
                                Util.Logger.Log("CoreLogic");
                                if(Environment.Is64BitProcess) {
                                    if(IntPtr.Size == 8) {
                                        Util.Logger.Log("IntPtr 8");
                                        try {
                                            if(ENV.CameraEnv.RecogMode == 1) {
                                                // 동영상 방식(FAVEngine)
                                                Util.Logger.Log("FAVEngine 동영상 방식 시작");
                                                thCoreInit = new Thread(delegate () {
                                                    // 1단계: 전체 채널 Init → OpenLiveView 완료 후 FAVELoop 시작
                                                    // (PopLPI 블로킹 중 Init 호출 시 SDK 충돌 방지)
                                                    CoreLogic.InitializeForFAVE();
                                                    bool cam1Ok = false, cam2Ok = false;
                                                    if(ENV.CameraEnv.IPCamera1Info.Use) {
                                                        cam1Ok = CoreLogic.InitFAVE(0, ENV.CameraEnv.IPCamera1Info.RtspUrl);
                                                        Util.Logger.Log(string.Format("FAVEngine CAM1 Init 결과={0}", cam1Ok));
                                                    }
                                                    if(ENV.CameraEnv.IPCamera2Info.Use) {
                                                        Thread.Sleep(2000);
                                                        Util.Logger.Log(string.Format("FAVEngine CAM2 시작 RTSP={0}", ENV.CameraEnv.IPCamera2Info.RtspUrl));
                                                        cam2Ok = CoreLogic.InitFAVE(1, ENV.CameraEnv.IPCamera2Info.RtspUrl);
                                                        Util.Logger.Log(string.Format("FAVEngine CAM2 Init 결과={0}", cam2Ok));
                                                    }
                                                    // 2단계: 초기화 성공한 채널만 LiveView 시작
                                                    // SDK OpenLiveView 실패 시 WPF MediaElement 폴백
                                                    if(cam1Ok) {
                                                        this.Invoke(new Action(() => {
                                                            int rc = CoreLogic.StartLiveView(0, PicLpr1Image.IsHandleCreated ? PicLpr1Image.Handle : IntPtr.Zero);
                                                            if(rc != 0) {
                                                                _rtspPreview1 = new RtspPreview(PicLpr1Image, ENV.CameraEnv.IPCamera1Info.RtspUrl);
                                                                Util.Logger.Log("FAVEngine CAM1 FFmpeg RTSP 폴백 프리뷰 시작");
                                                            }
                                                        }));
                                                    }
                                                    if(cam2Ok) {
                                                        this.Invoke(new Action(() => {
                                                            int rc = CoreLogic.StartLiveView(1, PicLpr2Image.IsHandleCreated ? PicLpr2Image.Handle : IntPtr.Zero);
                                                            if(rc != 0) {
                                                                _rtspPreview2 = new RtspPreview(PicLpr2Image, ENV.CameraEnv.IPCamera2Info.RtspUrl);
                                                                Util.Logger.Log("FAVEngine CAM2 FFmpeg RTSP 폴백 프리뷰 시작");
                                                            }
                                                        }));
                                                    }
                                                    // 3단계: 모든 LiveView 완료 후 인식 루프 시작
                                                    if(cam1Ok)
                                                        CoreLogic.StartFAVELoop(0);
                                                    if(cam2Ok)
                                                        CoreLogic.StartFAVELoop(1);
                                                });
                                            } else {
                                                // 스트로브 방식(SSEngine) - 기존
                                                Util.Logger.Log("core init");
                                                thCoreInit = new Thread(delegate () {
                                                    CoreLogic.Initialize();
                                                });
                                            }
                                            thCoreInit.IsBackground = true;
                                            thCoreInit.Start();
                                            timer_Core.Enabled = true;
                                            grpCoreInit.Visible = true;
                                            if(!ENV.CameraEnv.IPCamera2Info.Use)
                                                grpCoreInit.Left = (510 - grpCoreInit.Width) / 2;
                                        } catch(Exception ex) { }
                                    } else {
                                        Util.Logger.Log("64bit process가 아닙니다.");
                                    }
                                }
                                break;
#endif
                        }
#if WIN64
                        // Option(K): LPR 시작 시 사이드카를 새로 띄움(기존 것 종료 후). 로딩+워밍업이 길어
                        // CoreLogic 과 동일한 진행바(grpCoreInit)를 보여주고, 준비되면 자동으로 숨긴다.
                        if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.OptionK) {
                            // 로컬 파이썬 사이드카는 '카메라서버+자료처리(BOTH)' + 로컬 인식(원격 아님)일 때만 띄운다.
                            //  - 원격 차번인식(ParkingWeb 위임): 사이드카 불필요 → 설정만 즉시 준비
                            //  - 그 외 모드: 사이드카 미실행
                            string _rm = Util.Function.IniReadValue("OPTIONK", "remote") ?? "";
                            bool _remoteOcr = _rm.Equals("true", StringComparison.OrdinalIgnoreCase) || _rm == "1";
                            bool _both = ENV.StartType == (int)ClsStructure.ProgramStartType.BOTH;   // 카메라서버+자료처리
                            if(_remoteOcr) {
                                Util.Logger.Log("OptionK 원격 인식 모드 — 로컬 사이드카 미실행(ParkingWeb 위임)");
                                OptionK.Initialize(null);   // 원격 설정만 준비(프로세스 없이 즉시 완료)
                            } else if(_both) {
                                Util.Logger.Log("OptionK 사이드카 시작(백그라운드) — 카메라서버+자료처리 로컬 인식");
                                thCoreInit = new Thread(delegate () { OptionK.Initialize(null); });
                                thCoreInit.IsBackground = true;
                                thCoreInit.Start();
                                timer_Core.Enabled = true;
                                grpCoreInit.Visible = true;
                                grpCoreInit.Left = (510 - grpCoreInit.Width) / 2;
                            } else {
                                Util.Logger.Log("OptionK 로컬 사이드카 미실행 — 카메라서버+자료처리(BOTH) 모드 아님");
                            }
                        }
#endif
                    }
                    Util.Logger.Log("카메라 스타트");
                    StartCamera();
                    Util.Logger.Log("카메라 설정");
                    #region Cam Info Dp
                    Util.Logger.Log("카메라 설정");
                    GetCameraInfo();
                    #endregion
                }

                if(ENV.StartType == (int)ClsStructure.ProgramStartType.COM) {
                    //LPR 접속
                    Util.Logger.Log("LPR 통신 LPR 접속");
                    LPRCam.Connect_Ip = ENV.CommunicationEnv.Lpr1Info.SockInfo.IP;
                    LPRCam.Connect_Port = ENV.CommunicationEnv.Lpr1Info.SockInfo.Port;
                    LPRCam.Connect_Server();
                    Thread t = new Thread(new ThreadStart(LPRCAMChecker));
                    t.IsBackground = true;
                    t.Start();
                }

                Util.Logger.Log("DIO 설정");
                lblCam1BoardType.Text = "보드 타입: " + (ENV.CommonEnv.Dio.DioSetting.Type.Equals(true) ? "이벤트" : "리얼");
                lblCam1RegType.Text = "판독 방식: " + (ENV.CameraEnv.PlateArea.Equals(true) ? "번호판 판독" : "영역 인식");
                lblCam1ChName.Text = "채널명: " + ENV.CameraEnv.IPCamera1Info.ChName;
                if(ENV.CameraEnv.IPCamera2Info.Use) {
                    lblCam2BoardType.Text = "보드 타입: " + (ENV.CommonEnv.Dio.DioSetting.Type.Equals(true) ? "이벤트" : "리얼");
                    lblCam2RegType.Text = "판독 방식: " + (ENV.CameraEnv.PlateArea.Equals(true) ? "번호판 판독" : "영역 인식");
                    lblCam2ChName.Text = "채널명: " + ENV.CameraEnv.IPCamera2Info.ChName;
                }
                //IO Board Connect
                bool isDingtianBoard = ENV.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.DINGTIAN.ToString());
                if(!ENV.CommonEnv.Dio.DioSetting.SerialPort.Equals(String.Empty) || isDingtianBoard) {
                    //Util.Logger.Log(ClsStructure.DeviceList.KJC1000.ToString());
                    if(ENV.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.KJC1000.ToString()))
                        SerialDev.LoopOn += new clsSerialPort.eventInput(LoopDetect);
                    else if(ENV.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.REALSYS.ToString()))
                        SerialDev.LoopOn += new clsSerialPort.eventInput(LoopDetect);
                    else if(isDingtianBoard)
                        SerialDev.LoopOn += new clsSerialPort.eventInput(LoopDetect);
                }
                //인증 버전 제거
                //ENV.CommonEnv.Authentication = Util.Function.CheckAuthentication();
                ENV.CommonEnv.Authentication = true;
                clsThread.Auth = ENV.CommonEnv.Authentication;

                Console.WriteLine(Util.Function.Authentication());
                #region CAM Exposure Setting Thread
                //leess iNova2추가
                tExposure = null;
                // 카메라1·2 둘 다 USB이면 노출 제어 스레드 불필요 (USB는 자동노출)
                bool needExposureThread = !(IsUsbCam(1) && IsUsbCam(2));
                if(needExposureThread)
                {
                    if(ENV.CameraEnv.iNovaType == 1) tExposure = new Thread(new ThreadStart(UserSetting_Exposure_iNova1));
                    else if(ENV.CameraEnv.iNovaType == 2) tExposure = new Thread(new ThreadStart(UserSetting_Exposure_iNova2));
                    if(tExposure != null)
                    {
                        tExposure.IsBackground = true;
                        tExposure.Start();
                    }
                }
                #endregion

                if(ENV.StartType.Equals((int)ClsStructure.ProgramStartType.BOTH)) {
                    frm.Show();
                    frm.Top = this.Top;
                    frm.Left = this.Left;
                    frm.Visible = false;
                }
                SocketInit();
                timer1.Enabled = ENV.TestMode;
                ListItemAdd("프로그램 시작");
                ThreadImageSaveTermCheck = new Thread(new ThreadStart(ImageSaveTermCheck));
                ThreadImageSaveTermCheck.IsBackground = true;
                ThreadImageSaveTermCheck.Start();
                GetMasterInfo.Init();
                if(ENV.SendOffice) {
                    try {
                        Util.Logger.Log(Util.Logger.Log_Level.Event_Log, "사무실 전송 소켓 설정");
                        Util.Logger.Log(Util.Logger.Log_Level.Event_Log, "사무실 전송 소켓 설정");
                        string[] Info = Util.Function.GetInfoFromHomePage("OfficeIP.txt").Split(',');
                        if(Info.Length > 0) {
                            OfficeSocket.Connect_Ip = Info[0];
                            OfficeSocket.Connect_Port = Util.Function.IntTryParse(Info[1].Trim());
                        } else {
                            OfficeSocket.Connect_Ip = "222.104.189.252";
                            OfficeSocket.Connect_Port = 10002;
                        }
                    } catch(Exception ConnectOffice_Error) {
                        Util.Logger.Log(Util.Logger.Log_Level.Event_Log, "ConnectOffice_Error : " + ConnectOffice_Error.Message);
                    }
                    if(OfficeSocket.Connect_Ip != string.Empty && OfficeSocket.Connect_Port != 0) {
                        try {
                            Thread OfficeSend = new Thread(new ThreadStart(OfficeSendThread));
                            OfficeSend.IsBackground = true;
                            OfficeSend.Start();
                        } catch(Exception OfficeThreadStart_Error) {
                            Util.Logger.Log(Util.Logger.Log_Level.Event_Log, "OfficeThreadStart_Error : " + OfficeThreadStart_Error.Message);
                        }
                    }
                }
            } catch(Exception LoadErr) {
                Util.Logger.Log(Util.Logger.Log_Level.Event_Log, "Form_Load Error : " + LoadErr.ToString());
                MessageBox.Show(
                    "프로그램 초기화 중 오류가 발생했습니다.\n\n"
                    + "예외 형식: " + LoadErr.GetType().Name + "\n"
                    + "메시지: " + LoadErr.Message + "\n\n"
                    + "스택 트레이스:\n" + (LoadErr.StackTrace ?? "(없음)") + "\n\n"
                    + "log\\ 폴더의 로그 파일을 확인하세요.",
                    "Form_Load 초기화 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if(!GetMasterInfo.Use) {
                Thread MasterThread = new Thread(new ThreadStart(Get_Master));
                MasterThread.IsBackground = true;
                MasterThread.Start();
            }

            if(ENV.CameraEnv.CoreCountry == CoreLogic.KOR)
                this.Text += " [KOR]";
            else
                this.Text += " [THA]";
        }

        private void FormSize(bool Cam2Use) {
            if(Cam2Use)
                this.Width = 1000;
            else {
                this.Width = 510;
                btnCam2Capture.Visible = false;
                btnLog.Left = btnEnv.Left;
                btnEnv.Left = btnCam2Capture.Left;
            }
        }

        //leess iNova2추가
        private void StartCamera() {
            // 카메라별 소스에 따라 분기 (혼합 시나리오: 카메라1·2 각각 IP 또는 USB)
            bool bothUsbCams = ENV.CameraEnv.IPCamera1Info.Use && IsUsbCam(1)
                            && ENV.CameraEnv.IPCamera2Info.Use && IsUsbCam(2);

            if(ENV.CameraEnv.IPCamera1Info.Use)
            {
                if(IsUsbCam(1)) StartCamera_USB(1);
                else if(GetCamSource(1) == 2 || ENV.CameraEnv.iNovaType == 2) { /* iNova2: StartCamera_iNova2 내부에서 처리 */ }
                // 나머지는 아래 iNovaType 기반 일괄 처리 유지
            }
            if(ENV.CameraEnv.IPCamera2Info.Use)
            {
                if(IsUsbCam(2)) {
                    // USB 카메라 2대 동시 시작 시 DirectShow isoch endpoint 경합 회피
                    // 카메라1의 그래프가 안정 isoch 할당될 때까지 2.5초 대기 후 카메라2 시작
                    if(bothUsbCams) {
                        Util.Logger.Log("[USB] 카메라1 안정화 대기 (2500ms) 후 카메라2 시작");
                        System.Threading.Thread.Sleep(2500);
                    }
                    StartCamera_USB(2);
                }
            }
            // 기존 흐름: iNova IP 카메라가 1개라도 있으면 일괄 시작
            // (USB만 사용 중인 카메라는 위에서 이미 시작됨 — StartCamera_iNova1/2 내부는 IP가 비어있으면 자동 스킵)
            if(ENV.CameraEnv.iNovaType == 1) StartCamera_iNova1();
            else if(ENV.CameraEnv.iNovaType == 2) StartCamera_iNova2();
        }
        private void StartCamera_iNova1() {
            // USB로 설정된 채널은 iNova 시작 스킵 (StartCamera()에서 이미 USB 시작됨)
            if(!IsUsbCam(1) && !ENV.CameraEnv.IPCamera1Info.IP.Equals(string.Empty) && ENV.CameraEnv.IPCamera1Info.Use)
                if(m_camera1.ConnectStreamPort(ENV.CameraEnv.IPCamera1Info.IP, ENV.CameraEnv.IPCamera1Info.StreamUdp)) {
                    m_camera1.ConnectCommandPort(ENV.CameraEnv.IPCamera1Info.IP);
                    StartGrabLoop1();
                }
            if(IsUsbCam(1) && ENV.CameraEnv.IPCamera1Info.Use) StartGrabLoop1();
            if(!IsUsbCam(2) && !ENV.CameraEnv.IPCamera2Info.IP.Equals(string.Empty) && ENV.CameraEnv.IPCamera2Info.Use) {
                bool streamOk = m_camera2.ConnectStreamPort(ENV.CameraEnv.IPCamera2Info.IP, ENV.CameraEnv.IPCamera2Info.StreamUdp);
                Util.Logger.Log(string.Format("CAM2 ConnectStreamPort IP={0} result={1}", ENV.CameraEnv.IPCamera2Info.IP, streamOk));
                if(streamOk) {
                    m_camera2.ConnectCommandPort(ENV.CameraEnv.IPCamera2Info.IP);
                    StartGrabLoop2();
                    Util.Logger.Log("CAM2 GrabLoop2 시작");
                }
            }
            if(IsUsbCam(2) && ENV.CameraEnv.IPCamera2Info.Use) StartGrabLoop2();
        }
        private void StartCamera_iNova2() {
            if(!IsUsbCam(1) && !ENV.CameraEnv.IPCamera1Info.IP.Equals(string.Empty) && ENV.CameraEnv.IPCamera1Info.Use) {
                iNova2.IPCamError cam1Err = m_camera1_inova2.ConnectStreamPort(ENV.CameraEnv.IPCamera1Info.IP, ENV.CameraEnv.IPCamera1Info.StreamUdp);
                Util.Logger.Log(string.Format("CAM1 iNova2 ConnectStreamPort IP={0} result={1}", ENV.CameraEnv.IPCamera1Info.IP, cam1Err));
                if(cam1Err == iNova2.IPCamError.OK) {
                    //leess 속도개선 : 샘플에서는 아래 호출하지 않음
                    //m_camera1_inova2.ConnectCommandPort(ENV.CameraEnv.IPCamera1Info.IP);
                    StartGrabLoop1();
                    Util.Logger.Log("CAM1 GrabLoop1 iNova2 시작");
                }
            }
            if(IsUsbCam(1) && ENV.CameraEnv.IPCamera1Info.Use) StartGrabLoop1();
            if(!IsUsbCam(2) && !ENV.CameraEnv.IPCamera2Info.IP.Equals(string.Empty) && ENV.CameraEnv.IPCamera2Info.Use) {
                iNova2.IPCamError streamErr = m_camera2_inova2.ConnectStreamPort(ENV.CameraEnv.IPCamera2Info.IP, ENV.CameraEnv.IPCamera2Info.StreamUdp);
                Util.Logger.Log(string.Format("CAM2 iNova2 ConnectStreamPort IP={0} result={1}", ENV.CameraEnv.IPCamera2Info.IP, streamErr));
                if(streamErr == iNova2.IPCamError.OK) {
                    m_camera2_inova2.ConnectCommandPort(ENV.CameraEnv.IPCamera2Info.IP);
                    StartGrabLoop2();
                    Util.Logger.Log("CAM2 GrabLoop2 iNova2 시작");
                }
            }
            if(IsUsbCam(2) && ENV.CameraEnv.IPCamera2Info.Use) StartGrabLoop2();
        }

        private void StopCamera() {
            if(ENV.CameraEnv.IPCamera1Info.Use)
                StopGrabLoop1();
            if(ENV.CameraEnv.IPCamera2Info.Use)
                StopGrabLoop2();
            // USB 카메라 명시적 해제
            try { if(IsUsbCam(1)) m_camera1_usb.DisconnectStreamPort(); } catch { }
            try { if(IsUsbCam(2)) m_camera2_usb.DisconnectStreamPort(); } catch { }
        }

        private ClsStructure.IPCamera_Info GetCurrentInfo(IPCamera camera, string CamIp) {
            ClsStructure.IPCamera_Info Cinfo = new ClsStructure.IPCamera_Info();
            //BraketInfo
            Cinfo.BracketInfo.BraketInfo = new ClsStructure.Bracket_Detail[4];
            //int limitRecon = 1;
            bool isBrkMode;
            int brkNumber;
            if(!camera.IsCommandPortConnected())
                CamCommandReConnect(camera, CamIp);

            //if (!camera.GetBracketMode(out isBrkMode, out brkNumber))
            //{
            //    reconCnt = 0;
            //    while (true)
            //    {
            //        if (camera.GetBracketMode(out isBrkMode, out brkNumber) == false || reconCnt > limitRecon)
            //        {
            //            reconCnt = 0;
            //            break;
            //        }
            //        else 
            //        {
            //            CamCommandReConnect(camera, CamIp);
            //            reconCnt++;
            //        }
            //        Thread.Sleep(100);
            //    }
            //}
            if(camera.GetBracketMode(out isBrkMode, out brkNumber)) {
                Cinfo.BracketInfo.Use = isBrkMode;
                Cinfo.BracketInfo.Count = brkNumber;
            }
            for(int ch = 0; ch < 4; ch++) {
                int exp, again;
                double dgain;
                if(camera.GetBracketInfo(ch, out exp, out again, out dgain)) {
                    Cinfo.BracketInfo.BraketInfo[ch].Exposure = exp;
                    try {
                        Cinfo.BracketInfo.BraketInfo[ch].AnalogGain = again;
                        Cinfo.BracketInfo.BraketInfo[ch].DigitalGain = (int)((dgain - 1) * 20);
                    } catch(ArgumentException) {

                    }
                }
                //while (true)
                //{
                //    if (camera.GetBracketInfo(ch, out exp, out again, out dgain) == false || reconCnt > limitRecon)
                //    {
                //        reconCnt = 0;
                //        Cinfo.BracketInfo.BraketInfo[ch].Exposure = exp;
                //        try
                //        {
                //            Cinfo.BracketInfo.BraketInfo[ch].AnalogGain = again;
                //            Cinfo.BracketInfo.BraketInfo[ch].DigitalGain = (int)((dgain - 1) * 20);
                //        }
                //        catch (ArgumentException)
                //        {

                //        }
                //        break;
                //    }
                //    else
                //        CamCommandReConnect(camera, CamIp);
                //}
            }

            //Generalinfo
            double tgain;
            //if (!camera.GetTotalGain(out tgain))
            //{
            //    while (true)
            //    {
            //        CamCommandReConnect(camera, CamIp);
            //        if (camera.GetTotalGain(out tgain) == false || reconCnt > limitRecon)
            //        {
            //            reconCnt = 0;
            //            break;
            //        }
            //        Thread.Sleep(100);
            //    }
            //}
            if(camera.GetTotalGain(out tgain)) {
                Cinfo.Generalinfo.GainDecibel = Math.Log10(tgain) * 20;
                Cinfo.Generalinfo.TotalGain = (int)(Cinfo.Generalinfo.GainDecibel * 10);
            }

            // Read the current values from camera and set them to GUI.
            int exposure;

            //if (!camera.GetExposure(out exposure))
            //{
            //    while (true)
            //    {
            //        if (camera.GetExposure(out exposure) == false || reconCnt > limitRecon)
            //        {
            //            reconCnt = 0;
            //            break;
            //        }
            //        else
            //            CamCommandReConnect(camera, CamIp);
            //        Thread.Sleep(100);
            //    }
            //}
            if(camera.GetExposure(out exposure)) {
                Cinfo.Generalinfo.Exposure = exposure;
            }

            double fps;
            //if (!camera.GetFrameRate(out fps))
            //{
            //    while (true)
            //    {
            //        if (camera.GetFrameRate(out fps) == false || reconCnt > limitRecon)
            //        {
            //            reconCnt = 0;
            //            break;
            //        }
            //        else
            //            CamCommandReConnect(camera, CamIp);
            //        Thread.Sleep(100);
            //    }
            //}
            if(camera.GetFrameRate(out fps)) {
                Cinfo.Generalinfo.FrameRate = fps;
            }

            ALC m_alc = new ALC();
            //if (!camera.GetALC(out m_alc))
            //{
            //    while (true)
            //    {
            //        if (camera.GetALC(out m_alc) == false || reconCnt > limitRecon)
            //        {
            //            reconCnt = 0;
            //            break;
            //        }
            //        else
            //            CamCommandReConnect(camera, CamIp);
            //        Thread.Sleep(100);
            //    }
            //}
            if(camera.GetALC(out m_alc)) {
                Cinfo.Generalinfo.AlcInfo.target = m_alc.target;
                Cinfo.Generalinfo.AlcInfo.AECInfo.enableAEC = m_alc.enableAEC;
                Cinfo.Generalinfo.AlcInfo.AECInfo.minExposure = m_alc.minExposure;
                Cinfo.Generalinfo.AlcInfo.AECInfo.maxExposure = m_alc.maxExposure;
                Cinfo.Generalinfo.AlcInfo.AGCInfo.enableAGC = m_alc.enableAGC;
                Cinfo.Generalinfo.AlcInfo.AGCInfo.minGain = m_alc.minGain;
                Cinfo.Generalinfo.AlcInfo.AGCInfo.maxGain = m_alc.maxGain;
            }

            //while (true)
            //{
            //    var versionStr = camera.GetFirmwareVersion();
            //    if (versionStr == null || versionStr.ToString() == string.Empty || reconCnt > limitRecon)
            //    {
            //        reconCnt = 0;
            //        CamCommandReConnect(camera, CamIp);
            //        break;
            //    }
            //    else
            //    {
            //        Cinfo.Generalinfo.Fw = versionStr;
            //        if (versionStr != null && (versionStr.Contains("0.8.") || versionStr.Contains("0.7.")))
            //            m_maxBufferSizeKB = 256;
            //        break;
            //    }
            //}
            var versionStr = camera.GetFirmwareVersion();
            if(!string.IsNullOrWhiteSpace(versionStr))
                Cinfo.Generalinfo.Fw = versionStr.ToString();
            //while (true)
            //{
            //    var serial = camera.GetSerialNumber();

            //    Cinfo.Generalinfo.Sn = serial;
            //    if (serial.ToString() == string.Empty && reconCnt < limitRecon)
            //    {
            //        //reconCnt = 0;
            //        CamCommandReConnect(camera, CamIp);
            //    }
            //    else
            //    {
            //        reconCnt = 0;
            //        Cinfo.Generalinfo.Sn = serial;
            //        break;
            //    }
            //    Thread.Sleep(100);
            //}
            var serial = camera.GetSerialNumber();
            Cinfo.Generalinfo.Sn = serial.ToString();
            //TriggerInfo
            int cnt = 0;

            //while (true)
            //{
            //    if (!camera.GetTriggerImageCount(out cnt) == false || reconCnt > limitRecon)
            //    {
            //        reconCnt = 0;
            //        CamCommandReConnect(camera, CamIp);
            //    }
            //    else
            //    {
            //        Cinfo.TriggerInfo.CountPerTrigger = cnt;
            //        if (Cinfo.TriggerInfo.CountPerTrigger == 0)
            //            Cinfo.TriggerInfo.CountPerTrigger = 1;
            //        break;
            //    }
            //    Thread.Sleep(100);
            //}
            if(camera.GetTriggerImageCount(out cnt)) {
                Cinfo.TriggerInfo.CountPerTrigger = cnt;
                if(Cinfo.TriggerInfo.CountPerTrigger == 0)
                    Cinfo.TriggerInfo.CountPerTrigger = 1;
            }
            int trigMode;
            bool isActiveHi;

            //while (true)
            //{
            //    if (!camera.GetTriggerMode(out trigMode, out isActiveHi) == false || reconCnt > limitRecon)
            //    {
            //        reconCnt = 0;
            //        CamCommandReConnect(camera, CamIp);
            //    }
            //    else
            //    {
            //        Cinfo.TriggerInfo.TriggerMode = trigMode;
            //        break;
            //    }
            //    Thread.Sleep(100);
            //}
            if(camera.GetTriggerMode(out trigMode, out isActiveHi)) {
                Cinfo.TriggerInfo.TriggerMode = trigMode;
            }
            return Cinfo;
        }

        //leess iNova2추가
        private ClsStructure.IPCamera_Info GetCurrentInfo_iNova2(iNova2.IPCamera camera, string CamIp) {
            ClsStructure.IPCamera_Info Cinfo = new ClsStructure.IPCamera_Info();
            //BraketInfo
            Cinfo.BracketInfo.BraketInfo = new ClsStructure.Bracket_Detail[4];
            //int limitRecon = 1;
            bool isBrkMode;
            int brkNumber;
            if(!camera.IsCommandPortConnected())
                CamCommandReConnect_iNova2(camera, CamIp);

            if(camera.GetBracketMode(out isBrkMode, out brkNumber) == iNova2.IPCamError.OK) {
                Cinfo.BracketInfo.Use = isBrkMode;
                Cinfo.BracketInfo.Count = brkNumber;
            }
            for(int ch = 0; ch < 4; ch++) {
                int exp, again;
                double dgain;
                if(camera.GetBracketInfo(ch, out exp, out again, out dgain) == iNova2.IPCamError.OK) {
                    Cinfo.BracketInfo.BraketInfo[ch].Exposure = exp;
                    try {
                        Cinfo.BracketInfo.BraketInfo[ch].AnalogGain = again;
                        Cinfo.BracketInfo.BraketInfo[ch].DigitalGain = (int)((dgain - 1) * 20);
                    } catch(ArgumentException) {

                    }
                }
            }

            //Generalinfo
            double tgain;
            if(camera.GetTotalGain(out tgain) == iNova2.IPCamError.OK) {
                Cinfo.Generalinfo.GainDecibel = Math.Log10(tgain) * 20;
                Cinfo.Generalinfo.TotalGain = (int)(Cinfo.Generalinfo.GainDecibel * 10);
            }

            // Read the current values from camera and set them to GUI.
            int exposure;

            if(camera.GetExposure(out exposure) == iNova2.IPCamError.OK) {
                Cinfo.Generalinfo.Exposure = exposure;
            }

            double fps;
            if(camera.GetFrameRate(out fps) == iNova2.IPCamError.OK) {
                Cinfo.Generalinfo.FrameRate = fps;
            }

            iNova2.ALC m_alc = new iNova2.ALC();
            if(camera.GetALC(out m_alc) == iNova2.IPCamError.OK) {
                Cinfo.Generalinfo.AlcInfo.target = m_alc.target;
                Cinfo.Generalinfo.AlcInfo.AECInfo.enableAEC = m_alc.enableAEC;
                Cinfo.Generalinfo.AlcInfo.AECInfo.minExposure = m_alc.minExposure;
                Cinfo.Generalinfo.AlcInfo.AECInfo.maxExposure = m_alc.maxExposure;
                Cinfo.Generalinfo.AlcInfo.AGCInfo.enableAGC = m_alc.enableAGC;
                Cinfo.Generalinfo.AlcInfo.AGCInfo.minGain = m_alc.minGain;
                Cinfo.Generalinfo.AlcInfo.AGCInfo.maxGain = m_alc.maxGain;
            }

            //var versionStr = camera.GetFirmwareVersion();
            string versionStr = "";
            camera.GetFirmwareVersion(out versionStr);
            if(!string.IsNullOrWhiteSpace(versionStr))
                Cinfo.Generalinfo.Fw = versionStr.ToString();

            //var serial = camera.GetSerialNumber();
            string serial = "";
            camera.GetSerialNumber(out serial);
            Cinfo.Generalinfo.Sn = serial.ToString();
            //TriggerInfo
            int cnt = 0;

            if(camera.GetTriggerImageCount(out cnt) == iNova2.IPCamError.OK) {
                Cinfo.TriggerInfo.CountPerTrigger = cnt;
                if(Cinfo.TriggerInfo.CountPerTrigger == 0)
                    Cinfo.TriggerInfo.CountPerTrigger = 1;
            }
            int trigMode;
            bool isActiveHi;

            if(camera.GetTriggerMode(out trigMode, out isActiveHi) == iNova2.IPCamError.OK) {
                Cinfo.TriggerInfo.TriggerMode = trigMode;
            }
            return Cinfo;
        }

        private void CamCommandReConnect(IPCamera Cam, string Camip) {
            try {
                Cam.DisconnectCommandPort();
                Thread.Sleep(100);
                Cam.ConnectCommandPort(Camip);
                reconCnt++;
            } catch(Exception) { }
        }

        //leess iNova2추가
        private void CamCommandReConnect_iNova2(iNova2.IPCamera Cam, string Camip) {
            try {
                Cam.DisconnectCommandPort();
                Thread.Sleep(100);
                Cam.ConnectCommandPort(Camip);
                reconCnt++;
            } catch(Exception) { }
        }

        //leess iNova2추가
        private void GetCameraInfo() {
            if(ENV.CameraEnv.iNovaType == 1) GetCameraInfo_iNova1();
            else if(ENV.CameraEnv.iNovaType == 2) GetCameraInfo_iNova2();
        }
        private void GetCameraInfo_iNova1()
        {
            try
            {
                if (ENV.CameraEnv.IPCamera1Info.Use)
                {
                    Util.Logger.Log(ENV.CameraEnv.IPCamera1Info.IP);
                    IpCam1Current = GetCurrentInfo(m_camera1, ENV.CameraEnv.IPCamera1Info.IP);
                    ENV.CameraEnv.IPCamera1Info.CurrentInfo = IpCam1Current;
                    Util.Logger.Log("Compare");
                    if (IpCam1Current.TriggerInfo.CountPerTrigger != ENV.CameraEnv.IPCamera1Info.TriggerCnt)
                    {
                        reconCnt = 0;
                        while (Thread_Alive)
                        {
                            if (m_camera1.SetTriggerImageCount(ENV.CameraEnv.IPCamera1Info.TriggerCnt) == false || reconCnt > 3)
                            {
                                IpCam1Current.TriggerInfo.CountPerTrigger = ENV.CameraEnv.IPCamera1Info.TriggerCnt;
                                reconCnt = 0;
                                break;
                            }
                            else if (reconCnt < 3)
                            {
                                reconCnt++;
                                Util.Logger.Log(reconCnt.ToString());
                                CamCommandReConnect(m_camera1, ENV.CameraEnv.IPCamera1Info.IP);
                            }
                            else
                                break;
                            Thread.Sleep(100);
                        }
                    }
                    reconCnt = 0;
                    if (IpCam1Current.BracketInfo.Count != ENV.CameraEnv.IPCamera1Info.BarkectCnt)
                    {
                        while (Thread_Alive)
                        {
                            if (m_camera1.SetBracketMode(IpCam1Current.BracketInfo.Use, ENV.CameraEnv.IPCamera1Info.BarkectCnt) || reconCnt > 3)
                            {
                                IpCam1Current.BracketInfo.Count = ENV.CameraEnv.IPCamera1Info.BarkectCnt;
                                reconCnt = 0;
                                break;
                            }
                            else if (reconCnt < 3)
                            {
                                reconCnt++;
                                Util.Logger.Log(reconCnt.ToString());
                                CamCommandReConnect(m_camera1, ENV.CameraEnv.IPCamera1Info.IP);
                            }
                            else
                                break;
                            Thread.Sleep(100);
                        }
                    }
                    SetCameraInfo(IpCam1Current, "1");
                }
                reconCnt = 0;
                if (ENV.CameraEnv.IPCamera2Info.Use)
                {
                    Util.Logger.Log(ENV.CameraEnv.IPCamera2Info.IP);
                    IpCam2Current = GetCurrentInfo(m_camera2, ENV.CameraEnv.IPCamera2Info.IP);
                    ENV.CameraEnv.IPCamera2Info.CurrentInfo = IpCam2Current;
                    if (IpCam2Current.TriggerInfo.CountPerTrigger != ENV.CameraEnv.IPCamera2Info.TriggerCnt)
                    {
                        while (Thread_Alive)
                        {
                            if (m_camera2.SetTriggerImageCount(ENV.CameraEnv.IPCamera2Info.TriggerCnt) || reconCnt > 3)
                            {
                                IpCam2Current.TriggerInfo.CountPerTrigger = ENV.CameraEnv.IPCamera2Info.TriggerCnt;
                                reconCnt = 0;
                                break;
                            }
                            else if (reconCnt < 3)
                            {
                                reconCnt++;
                                Util.Logger.Log(reconCnt.ToString());
                                CamCommandReConnect(m_camera2, ENV.CameraEnv.IPCamera2Info.IP);
                            }
                            else
                                break;
                            Thread.Sleep(100);
                        }
                    }
                    reconCnt = 0;
                    if (IpCam2Current.BracketInfo.Count != ENV.CameraEnv.IPCamera2Info.BarkectCnt)
                    {
                        while (Thread_Alive)
                        {
                            if (m_camera2.SetBracketMode(IpCam2Current.BracketInfo.Use, ENV.CameraEnv.IPCamera2Info.BarkectCnt) || reconCnt > 3)
                            {
                                IpCam2Current.BracketInfo.Count = ENV.CameraEnv.IPCamera2Info.BarkectCnt;
                                reconCnt = 0;
                                break;
                            }
                            else if (reconCnt < 3)
                            {
                                reconCnt++;
                                Util.Logger.Log(reconCnt.ToString());
                                CamCommandReConnect(m_camera2, ENV.CameraEnv.IPCamera2Info.IP);
                            }
                            else
                                break;
                            Thread.Sleep(100);
                        }
                    }
                    SetCameraInfo(IpCam2Current, "2");
                }
            }
            catch (Exception) { }
        }
        private void GetCameraInfo_iNova2()
        {
            try
            {
                if (ENV.CameraEnv.IPCamera1Info.Use)
                {
                    Util.Logger.Log(ENV.CameraEnv.IPCamera1Info.IP);
                    IpCam1Current = GetCurrentInfo_iNova2(m_camera1_inova2, ENV.CameraEnv.IPCamera1Info.IP);
                    ENV.CameraEnv.IPCamera1Info.CurrentInfo = IpCam1Current;
                    Util.Logger.Log("Compare");
                    if (IpCam1Current.TriggerInfo.CountPerTrigger != ENV.CameraEnv.IPCamera1Info.TriggerCnt)
                    {
                        reconCnt = 0;
                        while (Thread_Alive)
                        {
                            if (m_camera1_inova2.SetTriggerImageCount(ENV.CameraEnv.IPCamera1Info.TriggerCnt) == iNova2.IPCamError.OK || reconCnt > 3)
                            {
                                IpCam1Current.TriggerInfo.CountPerTrigger = ENV.CameraEnv.IPCamera1Info.TriggerCnt;
                                reconCnt = 0;
                                break;
                            }
                            else if (reconCnt < 3)
                            {
                                reconCnt++;
                                Util.Logger.Log(reconCnt.ToString());
                                CamCommandReConnect_iNova2(m_camera1_inova2, ENV.CameraEnv.IPCamera1Info.IP);
                            }
                            else
                                break;
                            Thread.Sleep(100);
                        }
                    }
                    reconCnt = 0;
                    if (IpCam1Current.BracketInfo.Count != ENV.CameraEnv.IPCamera1Info.BarkectCnt)
                    {
                        while (Thread_Alive)
                        {
                            if (m_camera1_inova2.SetBracketMode(IpCam1Current.BracketInfo.Use, ENV.CameraEnv.IPCamera1Info.BarkectCnt) == iNova2.IPCamError.OK || reconCnt > 3)
                            {
                                IpCam1Current.BracketInfo.Count = ENV.CameraEnv.IPCamera1Info.BarkectCnt;
                                reconCnt = 0;
                                break;
                            }
                            else if (reconCnt < 3)
                            {
                                reconCnt++;
                                Util.Logger.Log(reconCnt.ToString());
                                CamCommandReConnect_iNova2(m_camera1_inova2, ENV.CameraEnv.IPCamera1Info.IP);
                            }
                            else
                                break;
                            Thread.Sleep(100);
                        }
                    }
                    SetCameraInfo(IpCam1Current, "1");
                }
                reconCnt = 0;
                if (ENV.CameraEnv.IPCamera2Info.Use)
                {
                    Util.Logger.Log(ENV.CameraEnv.IPCamera2Info.IP);
                    IpCam2Current = GetCurrentInfo_iNova2(m_camera2_inova2, ENV.CameraEnv.IPCamera2Info.IP);
                    ENV.CameraEnv.IPCamera2Info.CurrentInfo = IpCam2Current;
                    if (IpCam2Current.TriggerInfo.CountPerTrigger != ENV.CameraEnv.IPCamera2Info.TriggerCnt)
                    {
                        while (Thread_Alive)
                        {
                            if (m_camera2_inova2.SetTriggerImageCount(ENV.CameraEnv.IPCamera2Info.TriggerCnt) == iNova2.IPCamError.OK || reconCnt > 3)
                            {
                                IpCam2Current.TriggerInfo.CountPerTrigger = ENV.CameraEnv.IPCamera2Info.TriggerCnt;
                                reconCnt = 0;
                                break;
                            }
                            else if (reconCnt < 3)
                            {
                                reconCnt++;
                                Util.Logger.Log(reconCnt.ToString());
                                CamCommandReConnect_iNova2(m_camera2_inova2, ENV.CameraEnv.IPCamera2Info.IP);
                            }
                            else
                                break;
                            Thread.Sleep(100);
                        }
                    }
                    reconCnt = 0;
                    if (IpCam2Current.BracketInfo.Count != ENV.CameraEnv.IPCamera2Info.BarkectCnt)
                    {
                        while (Thread_Alive)
                        {
                            if (m_camera2_inova2.SetBracketMode(IpCam2Current.BracketInfo.Use, ENV.CameraEnv.IPCamera2Info.BarkectCnt) == iNova2.IPCamError.OK || reconCnt > 3)
                            {
                                IpCam2Current.BracketInfo.Count = ENV.CameraEnv.IPCamera2Info.BarkectCnt;
                                reconCnt = 0;
                                break;
                            }
                            else if (reconCnt < 3)
                            {
                                reconCnt++;
                                Util.Logger.Log(reconCnt.ToString());
                                CamCommandReConnect_iNova2(m_camera2_inova2, ENV.CameraEnv.IPCamera2Info.IP);
                            }
                            else
                                break;
                            Thread.Sleep(100);
                        }
                    }
                    SetCameraInfo(IpCam2Current, "2");
                }
            }
            catch (Exception) { }
        }

        private void SetCameraInfo(ClsStructure.IPCamera_Info Cinfo, string idx)
        {
            string[] ControlName = { "lblCam" + idx + "SN", "lblCam" + idx + "FWVer", "lblCam" + idx + "Exposure", "lblCam" + idx + "FrameRate", "lblCam" + idx + "Mode", 
                                       "lblCam" + idx + "TriggerCnt", "lblCam" + idx + "TriggerMode", "lblCam" + idx + "TcpUdp", "lblCam" + idx + "IP" };

            for (int i = 0; i < ControlName.Length; i++)
            {
                Label lbl = (Label)this.Controls.Find(ControlName[i], true).FirstOrDefault();
                if (lbl != null)
                    switch (i)
                    {
                        case 0:
                            if (lbl.Text == "SN")
                                SetLabelText(lbl, "S/N: " + Cinfo.Generalinfo.Sn);
                            break;
                        case 1:
                            if (lbl.Text == "FW ver")
                                SetLabelText(lbl, "FW: " + Cinfo.Generalinfo.Fw);
                            break;
                        case 2:
                            SetLabelText(lbl, "Exposure: " + Cinfo.Generalinfo.Exposure);
                            break;
                        case 3:
                            SetLabelText(lbl, "FrameRate: " + Cinfo.Generalinfo.FrameRate);
                            break;
                        case 4:
                            if (Cinfo.BracketInfo.Use)
                                SetLabelText(lbl, "Mode: BraketMode");
                            else
                                if (Cinfo.Generalinfo.AlcInfo.AECInfo.enableAEC || Cinfo.Generalinfo.AlcInfo.AGCInfo.enableAGC)
                                    SetLabelText(lbl, "Mode: ALC");
                                else
                                    SetLabelText(lbl, "Mode: Normal");
                            break;
                        case 5:
                            if (Cinfo.BracketInfo.Use)
                                SetLabelText(lbl, "Brakect cnt: " + Cinfo.BracketInfo.Count.ToString());
                            else
                                SetLabelText(lbl, "Trigger per cnt: " + Cinfo.TriggerInfo.CountPerTrigger.ToString());
                            break;
                        case 6:
                            switch (Cinfo.TriggerInfo.TriggerMode)
                            {
                                case 0:
                                    SetLabelText(lbl, "Trigger: Free Run");
                                    break;
                                case 1:
                                    SetLabelText(lbl, "Trigger: One Shot Trigger");
                                    break;
                                case 2:
                                    SetLabelText(lbl, "Trigger: Mixed Trigger");
                                    break;
                                case 3:
                                    SetLabelText(lbl, "Trigger: Pseudo Trigger");
                                    break;
                            }
                            break;
                        case 7:
                            if (idx.Equals("1"))
                                SetLabelText(lbl, "Streaming: " + (ENV.CameraEnv.IPCamera1Info.StreamUdp.Equals(true) ? "UDP" : "TCP"));
                            else
                                SetLabelText(lbl, "Streaming: " + (ENV.CameraEnv.IPCamera2Info.StreamUdp.Equals(true) ? "UDP" : "TCP"));
                            break;
                        case 8:
                            if (idx.Equals("1"))
                                SetLabelText(lbl, "IP: " + ENV.CameraEnv.IPCamera1Info.IP);
                            else if (idx.Equals("2"))
                                SetLabelText(lbl, "IP: " + ENV.CameraEnv.IPCamera2Info.IP);
                            break;
                    }
            }
        }

        // 번호인식모듈 초기화
        private void ElanOpen()
        {
            try
            {
                //인식 모듈 최대 로드 갯수 4개 로드
                clsFunction.Elanpr_Initialize(ref uEngineID[0, 0]);
                if (uEngineID[0, 0] == 0)
                {
                    // 실패
                }
                clsFunction.Elanpr_Initialize(ref uEngineID[1, 0]);
                if (uEngineID[1, 0] == 0)
                {
                    // 실패
                }
                clsFunction.Elanpr_Initialize(ref uEngineID[2, 0]);
                if (uEngineID[2, 0] == 0)
                {
                    // 실패
                }
                clsFunction.Elanpr_Initialize(ref uEngineID[3, 0]);
                if (uEngineID[3, 0] == 0)
                {
                    // 실패
                }
            }
            catch (Exception)
            {
            }
        }

        private void frmCamMain_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.F5))
                btnCam1Capture.PerformClick();
            else if (e.KeyCode.Equals(Keys.F6))
            {
                if (ENV.CameraEnv.IPCamera2Info.Use)
                    btnCam2Capture.PerformClick();
            }
        }

        private void btnCam1Capture_Click(object sender, EventArgs e)
        {
            Util.Logger.Log("캡쳐버튼1 클릭");
            dtRegList1.Rows.Clear();
            Capture1 = true;
            LastLoopTime1 = DateTime.Now;
        }

        private void frmCamMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            StopCamera();
            Util.Logger.Log("프로그램 종료");
            Application.Exit();
            Application.ExitThread();
        }

#region RegPlate
        private void RegPlate(int Camindex)
        {
            String DatePath = string.Empty;
            String FileName = string.Empty;
            String EtcFileName = string.Empty;
            String SFileName = string.Empty;

            ClsStructure.IPCamera_Basic_Setting CamInfo = new ClsStructure.IPCamera_Basic_Setting();
            List<ClsStructure.RegList> list = new List<ClsStructure.RegList>();

            if (Camindex.Equals(0))
            {
                CamInfo = ENV.CameraEnv.IPCamera1Info;
                list = RegList1;
            }
            else
            {
                CamInfo = ENV.CameraEnv.IPCamera2Info;
                list = RegList2;
            }

            if (list.Count > 0)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (!list[i].Job)
                    {
                        Thread t = new Thread(() => getPlate_RegImage(list[i].id, CamInfo.Roi));
                        t.IsBackground = true;
                        t.Start();
                    }
                }
            }

            if (ENV.CameraEnv.SockDataFormat.Equals((int)ClsStructure.SockFormat.Nexpa))
                DatePath = string.Format(@"{0:D4}\{1:D2}\{2:D2}", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString(), DateTime.Now.Day.ToString());
            else
                DatePath = DateTime.Now.ToString("yyyyMMdd");

            while (Thread_Alive)
            {
                int cnt = 0;
                int xidx = -1;
                int noidx = -1;
                DateTime Last = new DateTime();
                try
                {
                    foreach (ClsStructure.RegList item in list)
                    {
                        if (item.Job)
                        {
                            if (item.result.IndexOf('X') == -1 && !item.result.Equals("No_Detection"))
                            {
                                FileName = string.Format("{0}\\{1}\\{2}", ENV.CameraEnv.ImageSave.SavePath, DatePath, item.FileName);
                                EtcFileName = string.Format("{0}\\{1}\\{2}", ENV.CameraEnv.ImageSave.EtcPath, DatePath, item.FileName);
                                SFileName = item.FileName;
                                Util.Logger.Log(String.Format("인식번호 : {0} 인식속도: {1}ms", item.result, item.term));
                                SetLabelText(lblCam1RegSpeed, String.Format("인식속도: {0}ms", item.term));
                                SetLabelText(lblCam1RegResult, "인식결과: " + item.result);
                                break;
                            }
                            else if (item.result.IndexOf('X') > -1 && xidx == -1)
                            {
                                xidx = list.FindIndex(x => x.id == item.id);
                                if (Last < item.CapTime)
                                    Last = item.CapTime;
                            }
                            else if (item.result.Equals("No_Detection") && noidx == -1)
                            {
                                noidx = list.FindIndex(x => x.id == item.id);
                                if (Last < item.CapTime)
                                    Last = item.CapTime;
                            }
                            cnt++;
                        }
                    }
                    if (list.Count.Equals(cnt) || (DateTime.Now - Last).TotalSeconds > 2)
                    {
                        if (xidx > -1)
                        {
                            FileName = string.Format("{0}\\{1}\\{2}", ENV.CameraEnv.ImageSave.SavePath, DatePath, list[xidx].FileName);
                            EtcFileName = string.Format("{0}\\{1}\\{2}", ENV.CameraEnv.ImageSave.EtcPath, DatePath, list[xidx].FileName);
                            SFileName = list[xidx].FileName;
                            Util.Logger.Log(String.Format("인식번호 : {0} 인식속도: {1}ms", list[xidx].result, list[xidx].term));
                            SetLabelText(lblCam1RegSpeed, String.Format("인식속도: {0}ms", list[xidx].term));
                            SetLabelText(lblCam1RegResult, "인식결과: " + list[xidx].result);
                            break;
                        }
                        else if (noidx > -1)
                        {
                            FileName = string.Format("{0}\\{1}\\{2}", ENV.CameraEnv.ImageSave.SavePath, DatePath, list[noidx].FileName);
                            EtcFileName = string.Format("{0}\\{1}\\{2}", ENV.CameraEnv.ImageSave.EtcPath, DatePath, list[noidx].FileName);
                            SFileName = list[noidx].FileName;
                            Util.Logger.Log(String.Format("인식번호 : {0} 인식속도: {1}ms", list[noidx].result, list[noidx].term));
                            SetLabelText(lblCam1RegSpeed, String.Format("인식속도: {0}ms", list[noidx].term));
                            SetLabelText(lblCam1RegResult, "인식결과: " + list[noidx].result);
                            break;
                        }
                        else
                            return;
                    }
                }
                catch (Exception)
                { }
            }
            
            //Util.Logger.Log(String.Format("인식번호 : {0} 인식속도: {1}ms", epr.strPlateNumber, sw.ElapsedMilliseconds));
            //SetLabelText(lblCam1RegSpeed, String.Format("인식속도: {0}ms", sw.ElapsedMilliseconds));
            //SetLabelText(lblCam1RegResult, "인식결과: " + item.result);

            //index = list.FindIndex(x => x.id == JobID);
            //list[index] = item;

            //string DestinationPath = String.Empty;
            //if (ENV.CameraEnv.ImageSave.EtcSave)
            //{
            //    DestinationPath = ENV.CameraEnv.ImageSave.EtcPath + "\\" + clsFunction.GetSavePath(ENV.CameraEnv.SockDataFormat);
            //    if (!Directory.Exists(DestinationPath))
            //        Directory.CreateDirectory(DestinationPath);
            //    DestinationPath += "\\" + Path.GetFileName(FileName).Replace(".jpg", item.result + ".jpg");
            //    clsFunction.SaveImage(FileName, DestinationPath, rcPlateLoc);
            //}
            //DestinationPath = ENV.CameraEnv.ImageSave.SavePath + "\\" + clsFunction.GetSavePath(ENV.CameraEnv.SockDataFormat);
            //if (!Directory.Exists(DestinationPath))
            //    Directory.CreateDirectory(DestinationPath);
            //DestinationPath += "\\" + Path.GetFileName(FileName).Replace(".jpg", item.result + ".jpg");
            //clsFunction.SaveImage(FileName, DestinationPath, rcPlateLoc);
            //switch (Camindex)
            //{
            //    case 0:
            //        Properties.Settings.Default.Ch1File = DestinationPath;
            //        break;
            //    case 1:
            //        Properties.Settings.Default.Ch2File = DestinationPath;
            //        break;
            //}
            //Properties.Settings.Default.Save();
            //#endregion
        }

        private void getPlate_RegImage(int id, Rectangle rect)
        {

        }
#endregion

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (ENV.TestMode)
            {
                if (chkLoop1.Checked || chkLoop2.Checked)
                {
                    //btnLoop.PerformClick();
                    if (chkLoop1.Checked) LoopDetect(ENV.CameraEnv.IPCamera1Info.DioInPut.LoopPort, true);
                    if (chkLoop2.Checked) LoopDetect(ENV.CameraEnv.IPCamera2Info.DioInPut.LoopPort, true);
                }
                else
                {
                    if (ENV.CameraEnv.IPCamera1Info.Use && ENV.CameraEnv.IPCamera2Info.Use)
                    {
                        if ((DateTime.Now.Second % 10).Equals(0))
                            btnCam1Capture.PerformClick();
                        else if ((DateTime.Now.Second % 10).Equals(5))
                            btnCam2Capture.PerformClick();
                    }
                    else if (ENV.CameraEnv.IPCamera1Info.Use)
                        if ((DateTime.Now.Second % 10).Equals(0))
                            btnCam1Capture.PerformClick();
                }
            }
            else
                timer1.Enabled = false;
        }
        private void LoopDetect(int Port, bool Up)
        {
            Console.WriteLine(string.Format("{0} {1}", Port, Up));
            TimeSpan diff;
            // DINGTIAN(이더넷 릴레이)은 InEvent(port, on) 시그니처가 KJC-1000과 동일하므로 동일 분기 처리
            bool isDingtian = ENV.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.DINGTIAN.ToString());
            if (ENV.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.KJC1000.ToString()) || isDingtian)
            {
                //env.CameraEnv.IPCamera1Info.DioInfo.LoopPort
                if (Up)
                {
                    if (Port.Equals(ENV.CameraEnv.IPCamera1Info.DioInPut.LoopPort))
                    {
                        //if (!Loop1 || lblCam1Loop.Text == "Loop Off")
                        if (!Loop1)
                        {
                            //2초 이내 재수신시 무시
                            diff = DateTime.Now - LastLoopTime1;
                            LastLoopTime1 = DateTime.Now;
                            if (diff.TotalSeconds < 2) return;
                            Util.Logger.Log("Loop1 Detect");
                            SetLabelText(lblCam1Loop, "Loop ON");
                            ListItemAdd(ENV.CameraEnv.IPCamera1Info.ChName + " Loop On");
                            dtRegList1.Rows.Clear();
                            Capture1 = true;
                            Loop1 = true;
                            NgisWay.Reg1Cnt = 0;
                        }
                    }
                    else if (Port.Equals(ENV.CameraEnv.IPCamera2Info.DioInPut.LoopPort))
                    {
                        //if (!Loop2 || lblCam2Loop.Text == "Loop Off")
                        if (!Loop2)
                        {
                            //2초 이내 재수신시 무시
                            diff = DateTime.Now - LastLoopTime2;
                            LastLoopTime2 = DateTime.Now;
                            if (diff.TotalSeconds < 2) return;
                            Util.Logger.Log("Loop2 Detect");
                            SetLabelText(lblCam2Loop, "Loop ON");
                            ListItemAdd(ENV.CameraEnv.IPCamera2Info.ChName + " Loop On");
                            dtRegList2.Rows.Clear();
                            Capture2 = true;
                            Loop2 = true;
                            NgisWay.Reg2Cnt = 0;
                        }
                    }
                    else if (ENV.CommonEnv.Dio.IsolatePort.Out.Use && Port.Equals(ENV.CommonEnv.Dio.IsolatePort.In.LoopPort))
                    {
                        SerialDev.IsolatedGateOpen();
                    }
                    else if (Port.Equals(ENV.CommunicationEnv.FixedPort))
                    {
                        if (!isFixed)
                        {
                            if (ENV.CommunicationEnv.DisPlay[0].Use && ENV.CommunicationEnv.DisPlay[0].UseFiex)
                            {
                                if (ENV.CommunicationEnv.DisPlay[0].Net.Use)
                                    NetDisPlay1.SendMsg(ENV.CommunicationEnv.FixedMent.Ment1Line, clsFunction.GetColor8Int(ENV.CommunicationEnv.FixedMent.Ment1Color)
                                    , ENV.CommunicationEnv.FixedMent.Ment2Line, clsFunction.GetColor8Int(ENV.CommunicationEnv.FixedMent.Ment2Color));
                                else
                                    SerialDev.DisPlayMent(0, ENV.CommunicationEnv.FixedMent.Ment1Line, ENV.CommunicationEnv.FixedMent.Ment1Color
                                        , ENV.CommunicationEnv.FixedMent.Ment2Line, ENV.CommunicationEnv.FixedMent.Ment2Color);
                                FirstDisPlayReturn.DisPlayTime = DateTime.Now;
                            }
                            if (ENV.CommunicationEnv.DisPlay[1].Use && ENV.CommunicationEnv.DisPlay[1].UseFiex)
                            {
                                if (ENV.CommunicationEnv.DisPlay[1].Net.Use)
                                    NetDisPlay2.SendMsg(ENV.CommunicationEnv.FixedMent.Ment1Line, clsFunction.GetColor8Int(ENV.CommunicationEnv.FixedMent.Ment1Color)
                                    , ENV.CommunicationEnv.FixedMent.Ment2Line, clsFunction.GetColor8Int(ENV.CommunicationEnv.FixedMent.Ment2Color));
                                else
                                    SerialDev.DisPlayMent(1, ENV.CommunicationEnv.FixedMent.Ment1Line, ENV.CommunicationEnv.FixedMent.Ment1Color
                                      , ENV.CommunicationEnv.FixedMent.Ment2Line, ENV.CommunicationEnv.FixedMent.Ment2Color);
                                SecondDisPlayReturn.DisPlayTime = DateTime.Now;
                            }
                            isFixed = true;
                        }
                        else
                        {
                            if (FirstDisPlayReturn != null && ENV.CommunicationEnv.DisPlay[0].Use && ENV.CommunicationEnv.DisPlay[0].UseFiex)
                                FirstDisPlayReturn.DisPlayTime = DateTime.Now;
                            if (SecondDisPlayReturn != null && ENV.CommunicationEnv.DisPlay[1].Use && ENV.CommunicationEnv.DisPlay[1].UseFiex)
                                SecondDisPlayReturn.DisPlayTime = DateTime.Now;
                        }
                    }
                }
                else
                {
                    if (Port.Equals(ENV.CameraEnv.IPCamera1Info.DioInPut.LoopPort))
                    {
                        if (Loop1)
                        {
                            SetLabelText(lblCam1Loop, "Loop Off");
                            Loop1 = false;
                        }
                        else
                            return;
                        ////2초 이내 재수신시 무시
                        //diff = DateTime.Now - LastLoopTime1;
                        //if (diff.TotalSeconds < 2) return;
                        //Console.WriteLine("Detect");
                        //SetLabelText(lblCam1Loop, "Loop ON");
                        //ListItemAdd(ENV.CameraEnv.IPCamera1Info.ChName + " Loop On");
                        //dtRegList1.Rows.Clear();
                        //Capture1 = true;
                        //LastLoopTime1 = DateTime.Now;
                        //NgisWay.Reg1Cnt = 0;

                    }
                    else if (Port.Equals(ENV.CameraEnv.IPCamera2Info.DioInPut.LoopPort))
                    {
                        if (Loop2)
                        {
                            SetLabelText(lblCam2Loop, "Loop Off");
                            Loop2 = false;
                        }
                        else
                            return;
                        ////2초 이내 재수신시 무시
                        //diff = DateTime.Now - LastLoopTime2;
                        //if (diff.TotalSeconds < 2) return;
                        //Console.WriteLine("Detect");
                        //SetLabelText(lblCam2Loop, "Loop ON");
                        //ListItemAdd(ENV.CameraEnv.IPCamera2Info.ChName + " Loop On");
                        //dtRegList2.Rows.Clear();
                        //Capture2 = true;
                        //LastLoopTime2 = DateTime.Now;
                        //NgisWay.Reg2Cnt = 0;
                    }
                    else if (Port.Equals(ENV.CommunicationEnv.FixedPort))
                    {
                        if (isFixed)
                        {
                            if (ENV.CommunicationEnv.DisPlay[0].Use && ENV.CommunicationEnv.DisPlay[0].UseFiex)
                            {
                                if (ENV.CommunicationEnv.DisPlay[0].Net.Use)
                                    NetDisPlay1.SendMsg(ENV.CommunicationEnv.DisPlay[0].Ment.Ment1Line, clsFunction.GetColor8Int(ENV.CommunicationEnv.DisPlay[0].Ment.Ment1Color)
                                    , ENV.CommunicationEnv.DisPlay[0].Ment.Ment2Line, clsFunction.GetColor8Int(ENV.CommunicationEnv.DisPlay[0].Ment.Ment2Color));
                                else
                                    SerialDev.DisPlayMent(0, ENV.CommunicationEnv.DisPlay[0].Ment.Ment1Line, ENV.CommunicationEnv.DisPlay[0].Ment.Ment1Color
                                    , ENV.CommunicationEnv.DisPlay[0].Ment.Ment2Line, ENV.CommunicationEnv.DisPlay[0].Ment.Ment2Color);
                                FirstDisPlayReturn.DisPlayTime = DateTime.Now;
                            }
                            if (ENV.CommunicationEnv.DisPlay[1].Use && ENV.CommunicationEnv.DisPlay[1].UseFiex)
                            {
                                if (ENV.CommunicationEnv.DisPlay[1].Net.Use)
                                    NetDisPlay2.SendMsg(ENV.CommunicationEnv.DisPlay[1].Ment.Ment1Line, clsFunction.GetColor8Int(ENV.CommunicationEnv.DisPlay[1].Ment.Ment1Color)
                                    , ENV.CommunicationEnv.DisPlay[1].Ment.Ment2Line, clsFunction.GetColor8Int(ENV.CommunicationEnv.DisPlay[1].Ment.Ment2Color));
                                else
                                    SerialDev.DisPlayMent(1, ENV.CommunicationEnv.DisPlay[1].Ment.Ment1Line, ENV.CommunicationEnv.DisPlay[1].Ment.Ment1Color
                                    , ENV.CommunicationEnv.DisPlay[1].Ment.Ment2Line, ENV.CommunicationEnv.DisPlay[1].Ment.Ment2Color);
                                SecondDisPlayReturn.DisPlayTime = DateTime.Now;
                            }
                            isFixed = false;
                        }
                    }
                }
                string msg = "!_s_" + lastPlate;
                if (ENV.CameraEnv.IPCamera1Info.DioInPut.SmallCar)
                {
                    if (Port.Equals(ENV.CameraEnv.IPCamera1Info.DioInPut.SmallPort))
                    {
                        //smallcar
                    }
                }
                else if (ENV.CameraEnv.IPCamera2Info.DioInPut.SmallCar)
                {
                    if (Port.Equals(ENV.CameraEnv.IPCamera2Info.DioInPut.SmallPort))
                    {
                        //smallcar
                    }
                }
            }
            else if (ENV.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.REALSYS.ToString()))
            {
                if (Port.Equals(ENV.CameraEnv.IPCamera1Info.DioInPut.LoopPort))
                {
                    if (Loop1 != Up && Up)
                    {
                        diff = DateTime.Now - LastLoopTime1;
                        Console.WriteLine(diff.TotalSeconds + " " + LastLoopTime1);
                        if (diff.TotalSeconds < 2) return;
                        if (Capture1) return;
                        dtRegList1.Rows.Clear();
                        Capture1 = true;
                        SetLabelText(lblCam1Loop, "Loop ON");
                        LastLoopTime1 = DateTime.Now;
                        Loop1 = true;
                    }
                    else if (Loop1 != Up && !Up)
                    {
                        Capture1 = false;
                        SetLabelText(lblCam1Loop, "Loop Off");
                        Loop1 = false;
                    }
                }
                else if (ENV.CameraEnv.IPCamera1Info.DioInPut.SmallCar && Port.Equals(ENV.CameraEnv.IPCamera1Info.DioInPut.SmallPort))
                {
                    SetLabelText(lblCam1Loop, "경차 ON");
                }
                if (Port.Equals(ENV.CameraEnv.IPCamera2Info.DioInPut.LoopPort))
                {
                    if (Loop2 != Up && Up)
                    {
                        diff = DateTime.Now - LastLoopTime2;
                        Console.WriteLine(diff.TotalSeconds + " " + LastLoopTime2);
                        if (diff.TotalSeconds < 2) return;
                        if (Capture2) return;
                        dtRegList2.Rows.Clear();
                        Capture2 = true;
                        SetLabelText(lblCam2Loop, "Loop ON");
                        LastLoopTime2 = DateTime.Now;
                        Loop2 = true;
                    }
                    else if (Loop2 != Up && !Up)
                    {
                        Capture2 = false;
                        SetLabelText(lblCam2Loop, "Loop Off");
                        Loop2 = false;
                    }
                }
                else if (ENV.CameraEnv.IPCamera2Info.DioInPut.SmallCar && Port.Equals(ENV.CameraEnv.IPCamera2Info.DioInPut.SmallPort))
                {
                    SetLabelText(lblCam2Loop, "경차 ON");
                }
            }
        }

        private void LoopDetect(String Rcv)
        {
            TimeSpan diff;
            if (ENV.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.KJC1000.ToString()))
            {
                //env.CameraEnv.IPCamera1Info.DioInfo.LoopPort
                if (Rcv.IndexOf(ENV.CameraEnv.IPCamera1Info.DioInPut.LoopPort.ToString()) > -1)
                {
                    //2초 이내 재수신시 무시
                    diff = DateTime.Now - LastLoopTime1;
                    if (diff.TotalSeconds < 2) return;
                    Console.WriteLine("Detect");
                    SetLabelText(lblCam1Loop, "Loop ON");
                    ListItemAdd(ENV.CameraEnv.IPCamera1Info.ChName + " Loop On");
                    dtRegList1.Rows.Clear();
                    Capture1 = true;
                    LastLoopTime1 = DateTime.Now;

                }
                else if (Rcv.IndexOf(ENV.CameraEnv.IPCamera2Info.DioInPut.LoopPort.ToString()) > -1)
                {
                    //2초 이내 재수신시 무시
                    diff = DateTime.Now - LastLoopTime2;
                    if (diff.TotalSeconds < 2) return;
                    Console.WriteLine("Detect");
                    SetLabelText(lblCam2Loop, "Loop ON");
                    ListItemAdd(ENV.CameraEnv.IPCamera2Info.ChName + " Loop On");
                    dtRegList2.Rows.Clear();
                    Capture2 = true;
                    LastLoopTime2 = DateTime.Now;
                }
                string msg = "!_s_" + lastPlate;
                if (ENV.CameraEnv.IPCamera1Info.DioInPut.SmallCar)
                {
                    if (Rcv.IndexOf(ENV.CameraEnv.IPCamera1Info.DioInPut.SmallPort.ToString()) > -1)
                    {
                        //smallcar
                    }
                }
                else if (ENV.CameraEnv.IPCamera2Info.DioInPut.SmallCar)
                {
                    if (Rcv.IndexOf(ENV.CameraEnv.IPCamera2Info.DioInPut.SmallPort.ToString()) > -1)
                    {
                        //smallcar
                    }
                }
            }
            else if (ENV.CommonEnv.Dio.DioSetting.Dev_Type_Name.Equals(ClsStructure.DeviceList.REALSYS.ToString()))
            {
                //env.CameraEnv.IPCamera1Info.DioInfo.LoopPort
                Int64 int64 = Int64.Parse(Rcv.Substring(Rcv.IndexOf('!') + 3, 2), NumberStyles.HexNumber);
                string Recv = "00000000" + Convert.ToString(int64, 2);
                char[] c = Recv.Substring(Recv.Length - 8, 8).ToCharArray();
                try
                {
                    if (ENV.CameraEnv.IPCamera1Info.DioInPut.LoopPort >= 0 && c[8 - (ENV.CameraEnv.IPCamera1Info.DioInPut.LoopPort + 1)].Equals('0') && !Capture1)
                    {
                        if (Loop1) return;
                        diff = DateTime.Now - LastLoopTime1;
                        Console.WriteLine(diff.TotalSeconds + " " + LastLoopTime1);
                        if (diff.TotalSeconds < 2) return;
                        if (Capture1) return;
                        dtRegList1.Rows.Clear();
                        Capture1 = true;
                        SetLabelText(lblCam1Loop, "Loop ON");
                        LastLoopTime1 = DateTime.Now;
                        Loop1 = true;
                    }
                    else if (ENV.CameraEnv.IPCamera1Info.DioInPut.SmallCar && c[8 - (ENV.CameraEnv.IPCamera1Info.DioInPut.SmallPort + 1)].Equals('0'))
                    //smallcar
                    {
                        SetLabelText(lblCam1Loop, "경차 ON");
                    }
                    else if (ENV.CameraEnv.IPCamera1Info.DioInPut.LoopPort >= 0 && c[8 - (ENV.CameraEnv.IPCamera1Info.DioInPut.LoopPort + 1)].Equals('1'))
                    {
                        Capture1 = false;
                        SetLabelText(lblCam1Loop, "Loop Off");
                        Loop1 = false;
                    }
                    if (ENV.CameraEnv.IPCamera2Info.Use)
                        if (ENV.CameraEnv.IPCamera2Info.DioInPut.LoopPort >= 0 && c[8 - (ENV.CameraEnv.IPCamera2Info.DioInPut.LoopPort + 1)].Equals('0') && !Capture2)
                        {
                            if (Loop2) return;
                            diff = DateTime.Now - LastLoopTime2;
                            if (diff.TotalSeconds < 2) return;
                            if (Capture2) return;
                            dtRegList2.Rows.Clear();
                            Capture2 = true;
                            SetLabelText(lblCam2Loop, "Loop ON");
                            LastLoopTime2 = DateTime.Now;
                            Loop2 = true;
                        }
                        else if (ENV.CameraEnv.IPCamera2Info.DioInPut.SmallCar && c[8 - (ENV.CameraEnv.IPCamera2Info.DioInPut.SmallPort + 1)].Equals('0'))
                        //smallcar
                        {
                            SetLabelText(lblCam2Loop, "경차 ON");
                        }
                        else if (ENV.CameraEnv.IPCamera2Info.DioInPut.LoopPort >= 0 && c[8 - (ENV.CameraEnv.IPCamera2Info.DioInPut.LoopPort + 1)].Equals('1'))
                        {
                            Capture2 = false;
                            SetLabelText(lblCam2Loop, "Loop Off");
                            Loop2 = false;
                        }
                }
                catch (Exception)
                { }
            }
        }

        public uint getRegID(int camIndex)
        {
            uint sindex = 0;
            uint eindex = 0;
            //2chenel use
            if (ENV.CameraEnv.IPCamera1Info.Use && ENV.CameraEnv.IPCamera2Info.Use)
            {
                switch (camIndex)
                {
                    case 0:
                        sindex = 0;
                        eindex = 2;
                        break;
                    case 1:
                        sindex = 2;
                        eindex = 4;
                        break;
                }
            }
            else
            {
                sindex = 0;
                eindex = 4;
            }

            for (uint j = sindex; j < eindex; j++)
            {
                if (uEngineID[j, 0] > 0 && uEngineID[j, 1].Equals(0))
                {
                    uEngineID[j, 1] = 1;
                    Util.Logger.Log(string.Format("{0}번째 regid {1} 할당", j, uEngineID[j, 0]));
                    return uEngineID[j, 0];
                }
            }

            return 0;
        }

        public void ReleaseRegID(uint regid)
        {
            for (int i = 0; i < 4; i++)
            {
                if (regid.Equals(uEngineID[i, 0]))
                {
                    Util.Logger.Log(string.Format("regid {0} 해제", uEngineID[i, 0]));
                    uEngineID[i, 1] = 0;
                    return;
                }
            }
        }

        //private void PlateRegResult(int camidx, DataRow dr)
        private void PlateRegResult(int camidx, ClsStructure.RegStruct dr)
        {
            ClsStructure.RegStruct[] dtregList = new ClsStructure.RegStruct[4];
            ClsStructure.IPCamera_Info caminfo = new ClsStructure.IPCamera_Info();
            string Chname = string.Empty;
            Util.Logger.Log(camidx.ToString());
            switch (camidx)
            {
                case 1:
                    dtregList = RegArray1;
                    caminfo = IpCam1Current;
                    Chname = ENV.CameraEnv.IPCamera1Info.ChName;
                    break;
                case 2:
                    dtregList = RegArray2;
                    caminfo = IpCam2Current;
                    Chname = ENV.CameraEnv.IPCamera2Info.ChName;
                    break;
                default:
                    return;
            }
            int capcnt = 0;
            if (caminfo.BracketInfo.Use)
                capcnt = caminfo.BracketInfo.Count;
            else
                capcnt = caminfo.TriggerInfo.CountPerTrigger;

            //DataRow[] dtRow = dtregList.Select(string.Format("idx = {0}", dr[0].ToString()));
            //if (dtRow.Length.Equals(0)) return;
            //인증 여부 확인
            //if (!ENV.Authentication && !dtRow[0]["PlateNo"].ToString().Equals("No_Detection"))
            if (!ENV.CommonEnv.Authentication && !dr.PlateNo.Equals("No_Detection"))
            {
                //차량번호 변경
                dr.PlateNo = clsFunction.MagicCarnum();
                ListItemAdd(string.Format("{0} 미인증 프로그램 소요시간 {1}ms 번호판 좌표 차량번호 {2}", Chname, dr.term.ToString().PadLeft(5, ' '), dr.PlateNo));
            }
            else
                ListItemAdd(string.Format("{0} 소요시간 {1}ms 차량번호 {2}", Chname, dr.term.ToString().PadLeft(5, ' '), dr.PlateNo));

            string DatePath = string.Empty;
            if (ENV.CameraEnv.SockDataFormat.Equals((int)ClsStructure.SockFormat.Nexpa))
                DatePath = string.Format(@"{0:D4}\{1:D2}\{2:D2}", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString(), DateTime.Now.Day.ToString());
            else
                DatePath = DateTime.Now.ToString("yyyyMMdd");
            //추가 저장 옵션 확인
            if (ENV.CameraEnv.ImageSave.EtcSave)
            {
                if (!Directory.Exists(string.Format("{0}\\{1}", ENV.CameraEnv.ImageSave.EtcPath, DatePath)))
                    Directory.CreateDirectory(string.Format("{0}\\{1}", ENV.CameraEnv.ImageSave.EtcPath, DatePath));
                if (File.Exists(dr.SourcePath))
                    //File.Copy(dr["SourcePath"].ToString(), string.Format("{0}\\{1}\\{2}_{3}_{4}", env.CameraEnv.ImageSave.EtcPath, DatePath, Chname, dr["PlateNo"].ToString(), dr["SourcePath"].ToString()));
                    clsFunction.SaveImage(dr.SourcePath,
                                string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg", ENV.CameraEnv.ImageSave.EtcPath, DatePath, Chname, dr.PlateNo, dr.SourcePath.Substring(0, 14)),
                                dr.Roi, dr.Exposure.ToString(), dr.PlateNo);
                Util.Logger.Log(string.Format("CH{0} 추가 저장 완료 {1}\\{2}\\{3}_{4}_{5}", camidx, ENV.CameraEnv.ImageSave.EtcPath, DatePath, Chname, dr.PlateNo, dr.SourcePath.Substring(0, 14)));
            }

            if (!Directory.Exists(string.Format("{0}\\{1}", ENV.CameraEnv.ImageSave.SavePath, DatePath)))
                Directory.CreateDirectory(string.Format("{0}\\{1}", ENV.CameraEnv.ImageSave.SavePath, DatePath));

            //var send = Array.Find(dtregList, x => x.Send == true);
            //var part = Array.Find(dtregList, x => x.PlateNo.Length == 5);
            //var nodetec = Array.Find(dtregList, x => x.PlateNo == "No_Detection");
            //var yet = Array.Find(dtregList, x => x.PlateNo == "");
            int send = 0;
            int part = 0; int partidx = 9;
            int nodetec = 0; int nodetecidx = 9;
            int yet = 0;
            dtregList[dr.CapCnt] = dr;
            for (int i = 0; i < capcnt; i++)
            {
                if (dtregList[i].Send == true)
                    send++;
                if (dtregList[i].PlateNo != null && dtregList[i].PlateNo.Length == 5)
                {
                    part++;
                    if (i < partidx)
                        partidx = i;
                }
                if (dtregList[i].PlateNo != null && dtregList[i].PlateNo.Equals("No_Detection"))
                {
                    nodetec++;
                    if (i < nodetecidx)
                        nodetecidx = i;
                }
                if (dtregList[i].PlateNo == null)
                    yet++;
            }

            try
            {
                //if (send > 0)
                //{
                //if (capcnt == 1 || (!dr.PlateNo.Equals("") && !dr.PlateNo.Equals("No_Detection") && !dr.PlateNo.Length.Equals(5) && dr.Send.Equals(false)))
                Util.Logger.Log(string.Format("send {0} part {1} nodetec {2} dr.PlateNo {3} dr.Send {4}", send, part, nodetec, dr.PlateNo, dr.Send));
                if (send.Equals(0) && (!dr.PlateNo.Equals("") && !dr.PlateNo.Equals("No_Detection") && !dr.PlateNo.Length.Equals(5) && dr.Send.Equals(false)))
                {
                    //Socket Send
                    //dr.Send = true;
                    dtregList[dr.CapCnt].Send = true;
                    //ImageMove
                    //File.Copy(dr["SourcePath"].ToString(), string.Format("{0}\\{1}\\{2}_{3}_{4}", env.CameraEnv.ImageSave.SavePath, DatePath, Chname, dr["PlateNo"].ToString(), dr["SourcePath"].ToString()));
                    clsFunction.SaveImage(dr.SourcePath,
                        string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg", ENV.CameraEnv.ImageSave.SavePath, DatePath, Chname, dr.PlateNo, dr.SourcePath.Substring(0, 14)),
                        dr.PlateRoi, dr.Exposure.ToString(), dr.PlateNo);
                    Console.WriteLine("Socket Send");
                    //결과 DP
                    if (send == 0)
                    {
                        if (camidx.Equals(1))
                        {
                            frm.pictureBox1.ImageLocation =string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg", ENV.CameraEnv.ImageSave.SavePath, DatePath, Chname, dr.PlateNo, dr.SourcePath.Substring(0, 14));
                            ListItemAdd(DataProcess.DataProcess(ENV.CommunicationEnv.Lpr1Info.InOutType, ENV, camidx - 1, dr.PlateNo.ToString(), string.Format("{0}_{1}_{2}.jpg", Chname, dr.PlateNo, dr.SourcePath.Substring(0, 14))));
                            SetLabelText(lblCam1RegSpeed, String.Format("인식속도: {0}ms", dr.term));
                            SetLabelText(lblCam1RegResult, "인식결과: " + dr.PlateNo);
                            Properties.Settings.Default.Ch1File = string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg", ENV.CameraEnv.ImageSave.SavePath, DatePath, Chname, dr.PlateNo, dr.SourcePath.Substring(0, 14));
                            Properties.Settings.Default.Save();
                            if (ENV.CameraEnv.IPCamera1Info.DioInPut.SmallCar)
                                lastPlate = dr.PlateNo;
                        }
                        else
                        {
                            frm.pictureBox2.ImageLocation = string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg", ENV.CameraEnv.ImageSave.SavePath, DatePath, Chname, dr.PlateNo, dr.SourcePath.Substring(0, 14));
                            ListItemAdd(DataProcess.DataProcess(ENV.CommunicationEnv.Lpr2Info.InOutType, ENV, camidx - 1, dr.PlateNo.ToString(), string.Format("{0}_{1}_{2}.jpg", Chname, dr.PlateNo, dr.SourcePath.Substring(0, 14))));
                            SetLabelText(lblCam2RegSpeed, String.Format("인식속도: {0}ms", dr.term));
                            SetLabelText(lblCam2RegResult, "인식결과: " + dr.PlateNo);
                            Properties.Settings.Default.Ch2File = string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg", ENV.CameraEnv.ImageSave.SavePath, DatePath, Chname, dr.PlateNo, dr.SourcePath.Substring(0, 14));
                            Properties.Settings.Default.Save();
                            if (ENV.CameraEnv.IPCamera2Info.DioInPut.SmallCar)
                                lastPlate = dr.PlateNo;
                        }
                        SendClient(Chname, dr.PlateNo, DatePath, string.Format("{0}\\{1}_{2}_{3}.jpg", DatePath, Chname, dr.PlateNo, dr.SourcePath.Substring(0, 14)));
                    }
                }
                else if (part + nodetec == capcnt)
                {
                    if (part > 0)
                    {
                        dtregList[partidx].Send = true;
                        //File.Copy(part[0]["SourcePath"].ToString(), string.Format("{0}\\{1}\\{2}_{3}_{4}", env.CameraEnv.ImageSave.SavePath, DatePath, Chname, part[0]["PlateNo"].ToString(), part[0]["SourcePath"].ToString()));
                        clsFunction.SaveImage(dtregList[partidx].SourcePath,
                            string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg", ENV.CameraEnv.ImageSave.SavePath, DatePath, Chname, dtregList[partidx].PlateNo, dtregList[partidx].SourcePath.Substring(0, 14)),
                            dtregList[partidx].PlateRoi, dtregList[partidx].Exposure.ToString(), dr.PlateNo);
                    }
                    else
                    {
                        dtregList[nodetecidx].Send = true;
                        //File.Copy(nodetec[0]["SourcePath"].ToString(), string.Format("{0}\\{1}\\{2}_{3}_{4}", env.CameraEnv.ImageSave.SavePath, DatePath, Chname, nodetec[0]["PlateNo"].ToString(), nodetec[0]["SourcePath"].ToString()));
                        clsFunction.SaveImage(dtregList[nodetecidx].SourcePath,
                            string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg", ENV.CameraEnv.ImageSave.SavePath, DatePath, Chname, dtregList[nodetecidx].PlateNo, dtregList[nodetecidx].SourcePath.Substring(0, 14)),
                            dtregList[nodetecidx].PlateRoi, dtregList[nodetecidx].Exposure.ToString(), dr.PlateNo);
                    }
                    Console.WriteLine("Socket Send");
                    //결과 DP
                    if (send == 0)
                    {
                        if (camidx.Equals(1))
                        {
                            ListItemAdd(DataProcess.DataProcess(ENV.CommunicationEnv.Lpr1Info.InOutType, ENV, camidx - 1, dr.PlateNo.ToString(), string.Format("{0}_{1}_{2}.jpg", Chname, dr.PlateNo, dr.SourcePath.Substring(0, 14))));
                            SetLabelText(lblCam1RegSpeed, String.Format("인식속도: {0}ms", dr.term));
                            SetLabelText(lblCam1RegResult, "인식결과: " + dr.PlateNo);
                            Properties.Settings.Default.Ch1File = string.Format("{0}\\{1}\\{2}_{3}_{4}.jpg", ENV.CameraEnv.ImageSave.SavePath, DatePath, Chname, dr.PlateNo, dr.SourcePath.Substring(0, 14));
                            Properties.Settings.Default.Save();
                        }
                        else
                        {
                            ListItemAdd(DataProcess.DataProcess(ENV.CommunicationEnv.Lpr2Info.InOutType, ENV, camidx - 1, dr.PlateNo.ToString(), string.Format("{0}_{1}_{2}.jpg", Chname, dr.PlateNo, dr.SourcePath.Substring(0, 14))));
                            SetLabelText(lblCam2RegSpeed, String.Format("인식속도: {0}ms", dr.term));
                            SetLabelText(lblCam2RegResult, "인식결과: " + dr.PlateNo);
                            Properties.Settings.Default.Ch2File = string.Format("{0}\\{1}\\{2}_{3}_{4}", ENV.CameraEnv.ImageSave.SavePath, DatePath, Chname, dr.PlateNo, dr.SourcePath.Substring(0, 14));
                            Properties.Settings.Default.Save();
                        }
                        SendClient(Chname, dr.PlateNo, DatePath, string.Format("{0}\\{1}_{2}_{3}.jpg", DatePath, Chname, dr.PlateNo, dr.SourcePath.Substring(0, 14)));
                    }
                }
                //}
            }
            catch (Exception)
            { }
            if (yet > 0) return;

            //send = dtregList.Select(string.Format("FirstCapturetime <= '{0}' and PlateNo <> ''", dr["FirstCapturetime"].ToString()));

            //if (send.Length.Equals(triggercnt))
            //{
            //foreach (DataRow row in send)
            //{
            //    try
            //    {
            //        if (File.Exists(row["SourcePath"].ToString()))
            //            File.Delete(row["SourcePath"].ToString());
            //    }
            //    catch (Exception)
            //    {
            //    }
            //}
            //    for (int i = send.Length - 1; i >= 0; i--)
            //    {
            //        dtRegList1.Rows.Remove(send[i]);
            //    }
            //}
            //Console.WriteLine(dtregList.Rows.Count);
            for (int i = 0; i < dtregList.Length; i++)
            {
                try
                {
                    if (dtregList[i].SourcePath != null && File.Exists(dtregList[i].SourcePath))
                        File.Delete(dtregList[i].SourcePath);
                }
                catch (Exception)
                { }
            }
        }

        private void btnCam2Capture_Click(object sender, EventArgs e)
        {
            Util.Logger.Log("캡쳐버튼2 클릭"); 
            dtRegList2.Rows.Clear();
            Capture2 = true;
            LastLoopTime2 = DateTime.Now;
        }

        private void UpdateStatus1(MetaInfo metaInfo)
        {
            IPCamera cam = new IPCamera();
            ClsStructure.IPCamera_Info caminfo = new ClsStructure.IPCamera_Info();
            Label lblexposure = new Label();
            Label lblstatus = new Label();
            Label lblstatus2 = new Label();
            cam = m_camera1;
            caminfo = IpCam1Current;
            lblexposure = lblCam1Exposure;

            int bufsize = cam.GetLastImageBufferSize();
            m_frameRate.Grab();
            double fps = m_frameRate.GetFrameRate();
            double mbps = bufsize * fps * 8 / 1024 / 1024;
            string status = string.Format("{0:F2} fps, Image size {1}K",
                                            fps,
                                            bufsize / 1024);
            string status2 = string.Format("{0:F2} Mbps", mbps);

            if (metaInfo != null)
            {
                status2 = string.Format("{0:F2} Mbps",
                                                mbps);
                string status3 = string.Format("Exposure {0}, Gain {1:F2}",
                                        metaInfo.Exposure,
                                        metaInfo.Gain);

                caminfo.Generalinfo.Exposure = metaInfo.Exposure;
                SetLabelText(lblexposure, status3);
                ENV.CameraEnv.IPCamera1Info.CurrentInfo.Generalinfo.Exposure = metaInfo.Exposure;
            }
            else
            {
                status2 = string.Format("{0:F2} Mbps", mbps);
            }
            SetLabelText(lblstatus, status);
            SetLabelText(lblstatus2, status2);
            DisplayImageSizeWarning(bufsize);
        }

        //leess iNova2추가
        private void UpdateStatus1_iNova2(iNova2.MetaInfo metaInfo)
        {
            iNova2.IPCamera cam = new iNova2.IPCamera();
            ClsStructure.IPCamera_Info caminfo = new ClsStructure.IPCamera_Info();
            Label lblexposure = new Label();
            Label lblstatus = new Label();
            Label lblstatus2 = new Label();
            cam = m_camera1_inova2;
            caminfo = IpCam1Current;
            lblexposure = lblCam1Exposure;

            int bufsize = cam.GetLastImageBufferSize();
            m_frameRate.Grab();
            double fps = m_frameRate.GetFrameRate();
            double mbps = bufsize * fps * 8 / 1024 / 1024;
            string status = string.Format("{0:F2} fps, Image size {1}K",
                                            fps,
                                            bufsize / 1024);
            string status2 = string.Format("{0:F2} Mbps", mbps);

            if (metaInfo != null)
            {
                status2 = string.Format("{0:F2} Mbps",
                                                mbps);
                string status3 = string.Format("Exposure {0}, Gain {1:F2}",
                                        metaInfo.Exposure,
                                        metaInfo.Gain);

                caminfo.Generalinfo.Exposure = metaInfo.Exposure;
                SetLabelText(lblexposure, status3);
                ENV.CameraEnv.IPCamera1Info.CurrentInfo.Generalinfo.Exposure = metaInfo.Exposure;
            }
            else
            {
                status2 = string.Format("{0:F2} Mbps", mbps);
            }
            SetLabelText(lblstatus, status);
            SetLabelText(lblstatus2, status2);
            DisplayImageSizeWarning(bufsize);
        }

        private void UpdateStatus2(MetaInfo metaInfo)
        {
            IPCamera cam = new IPCamera();
            ClsStructure.IPCamera_Info caminfo = new ClsStructure.IPCamera_Info();
            Label lblexposure = new Label();
            Label lblstatus = new Label();
            Label lblstatus2 = new Label();

            cam = m_camera2;
            caminfo = IpCam2Current;
            lblexposure = lblCam2Exposure;

            int bufsize = cam.GetLastImageBufferSize();
            m_frameRate.Grab();
            double fps = m_frameRate.GetFrameRate();
            double mbps = bufsize * fps * 8 / 1024 / 1024;
            string status = string.Format("{0:F2} fps, Image size {1}K",
                                            fps,
                                            bufsize / 1024);
            string status2 = string.Format("{0:F2} Mbps", mbps);

            if (metaInfo != null)
            {
                status2 = string.Format("{0:F2} Mbps",
                                                mbps);
                string status3 = string.Format("Exposure {0}, Gain {1:F2}",
                                        metaInfo.Exposure,
                                        metaInfo.Gain);

                caminfo.Generalinfo.Exposure = metaInfo.Exposure;
                SetLabelText(lblexposure, status3);
            }
            else
            {
                status2 = string.Format("{0:F2} Mbps", mbps);
            }
            SetLabelText(lblstatus, status);
            SetLabelText(lblstatus2, status2);
            DisplayImageSizeWarning(bufsize);
        }

        //leess iNova2추가
        private void UpdateStatus2_iNova2(iNova2.MetaInfo metaInfo)
        {
            iNova2.IPCamera cam = new iNova2.IPCamera();
            ClsStructure.IPCamera_Info caminfo = new ClsStructure.IPCamera_Info();
            Label lblexposure = new Label();
            Label lblstatus = new Label();
            Label lblstatus2 = new Label();

            cam = m_camera2_inova2;
            caminfo = IpCam2Current;
            lblexposure = lblCam2Exposure;

            int bufsize = cam.GetLastImageBufferSize();
            m_frameRate.Grab();
            double fps = m_frameRate.GetFrameRate();
            double mbps = bufsize * fps * 8 / 1024 / 1024;
            string status = string.Format("{0:F2} fps, Image size {1}K",
                                            fps,
                                            bufsize / 1024);
            string status2 = string.Format("{0:F2} Mbps", mbps);

            if (metaInfo != null)
            {
                status2 = string.Format("{0:F2} Mbps",
                                                mbps);
                string status3 = string.Format("Exposure {0}, Gain {1:F2}",
                                        metaInfo.Exposure,
                                        metaInfo.Gain);

                caminfo.Generalinfo.Exposure = metaInfo.Exposure;
                SetLabelText(lblexposure, status3);
            }
            else
            {
                status2 = string.Format("{0:F2} Mbps", mbps);
            }
            SetLabelText(lblstatus, status);
            SetLabelText(lblstatus2, status2);
            DisplayImageSizeWarning(bufsize);
        }

        private void UserSetting_Exposure_iNova1()
        {
            DateTime LastJob = DateTime.Now.AddMinutes(ENV.CameraEnv.IPCamera1Info.User_Setting_Resend_Interval * -1);
            DateTime LastSync = new DateTime();
            //int CapCnt = 0;
            while (Thread_Alive)
            {
                try
                {
                    Util.Function.MemoryClean();
                    TimeSpan diff = DateTime.Now - LastJob;
                    int cnt = 0;
                    bool blBarakect = false;
                    if (diff.TotalMinutes >= ENV.CameraEnv.IPCamera1Info.User_Setting_Resend_Interval)
                    {
                        //현재 설정된 값을 확인 한다.
                        for (int i = 0; i < 2; i++)
                        {
                            int idx = UseCheck(i);
                            if (idx > -1)
                            {
                                IPCamera cam = new IPCamera();
                                ClsStructure.IPCamera_Basic_Setting caminfo = new ClsStructure.IPCamera_Basic_Setting();
                                switch (i)
                                {
                                    case 0:
                                        cam = m_camera1;
                                        caminfo = ENV.CameraEnv.IPCamera1Info;
                                        break;
                                    case 1:
                                        cam = m_camera2;
                                        caminfo = ENV.CameraEnv.IPCamera2Info;
                                        break;
                                }

                                //if (!cam.IsCommandPortConnected())
                                //    cam.ConnectCommandPort(caminfo.IP);
                                //if (!cam.IsStreamPortConnected())
                                //    cam.ConnectStreamPort(caminfo.IP, caminfo.StreamUdp);

                                int Mode = caminfo.User_Setting[idx].ModeIdx;

                                if (caminfo.Use)
                                {
                                    if (caminfo.User_Setting[idx].UseALC)
                                    {
                                        Util.Logger.Log(string.Format("{0}번 카메라 ALC 모드", i + 1));
                                        //현재 ACL 모드 확인
                                        ALC alc = new ALC();
                                        cam.GetBracketMode(out blBarakect, out cnt);
                                        if (blBarakect)
                                        {
                                            Util.Logger.Log(string.Format("{0}번 카메라 브라켓 모드 비활성", i + 1));
                                            cam.SetBracketMode(false, caminfo.BarkectCnt);
                                        }
                                        cam.GetALC(out alc);
                                        if (!ReferenceEquals(alc, caminfo.User_Alc[Mode]))
                                        {
                                            Util.Logger.Log(string.Format("{0}번 카메라 ALC 정보 설정", i + 1));
                                            alc.enableAEC = caminfo.User_Alc[Mode].AECInfo.enableAEC;
                                            alc.enableAGC = caminfo.User_Alc[Mode].AGCInfo.enableAGC;
                                            alc.minExposure = caminfo.User_Alc[Mode].AECInfo.minExposure;
                                            alc.maxExposure = caminfo.User_Alc[Mode].AECInfo.maxExposure;
                                            alc.minGain = caminfo.User_Alc[Mode].AGCInfo.minGain;
                                            alc.maxGain = caminfo.User_Alc[Mode].AGCInfo.maxGain;
                                            alc.target = caminfo.User_Alc[Mode].target;
                                            cam.SetALC(alc);
                                        }
                                        cam.GetTriggerImageCount(out cnt);
                                        Util.Logger.Log(string.Format("{0}번 카메라 GetTriggerImageCount {1}", i + 1, cnt));
                                        if (cnt != caminfo.TriggerCnt)
                                        {
                                            cam.SetTriggerImageCount(caminfo.TriggerCnt);
                                            Util.Logger.Log(string.Format("{0}번 카메라 SetTriggerImageCount {1}", i + 1, caminfo.TriggerCnt));
                                        }
                                        //cam.SaveSetting();
                                    }
                                    else if (caminfo.User_Setting[idx].UseBarkect)
                                    {
                                        Util.Logger.Log(string.Format("{0}번 카메라 BARAKECT 모드", i + 1));
                                        cam.GetBracketMode(out blBarakect, out cnt);
                                        if (!blBarakect || caminfo.BarkectCnt != cnt)
                                        {
                                            cam.SetBracketMode(true, caminfo.BarkectCnt);
                                            Util.Logger.Log(string.Format("{0}번 카메라 BARKET 모드 활성화 {1}", i + 1, caminfo.BarkectCnt));
                                        }
                                        
                                        for (int ch = 0; ch < 4; ch++)
                                        {
                                            int exposure  = 0;
                                            double dgain = 0;
                                            int again = 0;
                                            cam.GetBracketInfo(ch, out exposure, out again, out dgain);
                                            if (exposure != caminfo.User_Brakect[Mode, ch].Exposure)
                                            {
                                                for (int j = 0; j < 4; j++)
                                                {
                                                    bool ret = cam.SetBracketInfo(j, caminfo.User_Brakect[Mode, j].Exposure, again, dgain);
                                                }
                                                break;
                                            }
                                        }
                                        //cam.SaveSetting();
                                    }
                                }
                            }
                        }
                        LastJob = DateTime.Now;
                        GetCameraInfo();
                    }

                    //for (int i = 0; i < 2; i++)
                    //{
                    //    ClsStructure.RegStruct[] Array = new ClsStructure.RegStruct[4];
                        
                    //    int capcnt = 0;
                    //    for (int j = 0; j < 2; j++)
                    //    {
                    //        switch (j)
                    //        {
                    //            case 0:
                    //                Array = RegArray1;
                    //                if (IpCam1Current.BracketInfo.Use)
                    //                    capcnt = IpCam1Current.BracketInfo.Count;
                    //                else
                    //                    capcnt = IpCam1Current.TriggerInfo.CountPerTrigger;
                    //                break;
                    //            case 1:
                    //                Array = RegArray2;
                    //                if (IpCam2Current.BracketInfo.Use)
                    //                    capcnt = IpCam2Current.BracketInfo.Count;
                    //                else
                    //                    capcnt = IpCam2Current.TriggerInfo.CountPerTrigger;
                    //                break;
                    //        }
                    //        int regcnt = 0;
                    //        for (int k = 0; k < Array.Length; k++)
                    //        {
                    //            if (Array[k].PlateNo != null && !Array[k].PlateNo.Equals(string.Empty))
                    //                regcnt++;
                    //        }
                    //        if (regcnt.Equals(capcnt))
                    //        {
                    //            string[] file = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.jpg");
                    //            foreach (string item in file)
                    //            {
                    //                try
                    //                {
                    //                    //File.Delete(item);
                    //                }
                    //                catch (Exception) { }
                    //            }
                    //        }
                    //    }
                    //}
                    //표준시 동기화
                    //diff = DateTime.Now - LastSync;
                    //if (diff.TotalMinutes > 60)
                    //{
                    //    if (Util.Function.SetTime())
                    //    {
                    //        Util.Logger.Log("표준시간 동기화");
                    //        LastSync = DateTime.Now;
                    //    }
                    //}

                    //DirectoryInfo[] ImageDir = Directory.GetDirectories(ENV.CameraEnv.ImageSave.SavePath, SearchOption.AllDirectories);
                    //DirectoryInfo di = new DirectoryInfo(ENV.CameraEnv.ImageSave.SavePath);
                    //DirectoryInfo[] directories =
                    //    di.GetDirectories(searchPattern, SearchOption.TopDirectoryOnly);
                    //if ((CapCnt % 6).Equals(0))
                    //{
                    //    btnCam1Capture.PerformClick();
                    //    CapCnt = 0;
                    //}
                    //else if ((CapCnt % 6).Equals(3))
                    //{
                    //    btnCam2Capture.PerformClick();
                    //}
                    //CapCnt++;
                }
                catch (Exception)
                {

                }
                Thread.Sleep(60000);
            }
        }

        //leess iNova2추가
        private void UserSetting_Exposure_iNova2()
        {
            DateTime LastJob = DateTime.Now.AddMinutes(ENV.CameraEnv.IPCamera1Info.User_Setting_Resend_Interval * -1);
            DateTime LastSync = new DateTime();
            //int CapCnt = 0;
            while (Thread_Alive)
            {
                try
                {
                    Util.Function.MemoryClean();
                    TimeSpan diff = DateTime.Now - LastJob;
                    int cnt = 0;
                    bool blBarakect = false;
                    if (diff.TotalMinutes >= ENV.CameraEnv.IPCamera1Info.User_Setting_Resend_Interval)
                    {
                        //현재 설정된 값을 확인 한다.
                        for (int i = 0; i < 2; i++)
                        {
                            int idx = UseCheck(i);
                            if (idx > -1)
                            {
                                iNova2.IPCamera cam = new iNova2.IPCamera();
                                ClsStructure.IPCamera_Basic_Setting caminfo = new ClsStructure.IPCamera_Basic_Setting();
                                switch (i)
                                {
                                    case 0:
                                        cam = m_camera1_inova2;
                                        caminfo = ENV.CameraEnv.IPCamera1Info;
                                        break;
                                    case 1:
                                        cam = m_camera2_inova2;
                                        caminfo = ENV.CameraEnv.IPCamera2Info;
                                        break;
                                }

                                int Mode = caminfo.User_Setting[idx].ModeIdx;
                                if (caminfo.Use)
                                {
                                    if (caminfo.User_Setting[idx].UseALC)
                                    {
                                        Util.Logger.Log(string.Format("{0}번 카메라 ALC 모드", i + 1));
                                        //현재 ACL 모드 확인
                                        iNova2.ALC alc = new iNova2.ALC();
                                        cam.GetBracketMode(out blBarakect, out cnt);
                                        if (blBarakect)
                                        {
                                            Util.Logger.Log(string.Format("{0}번 카메라 브라켓 모드 비활성", i + 1));
                                            cam.SetBracketMode(false, caminfo.BarkectCnt);
                                        }
                                        cam.GetALC(out alc);
                                        if (!ReferenceEquals(alc, caminfo.User_Alc[Mode]))
                                        {
                                            Util.Logger.Log(string.Format("{0}번 카메라 ALC 정보 설정", i + 1));
                                            alc.enableAEC = caminfo.User_Alc[Mode].AECInfo.enableAEC;
                                            alc.enableAGC = caminfo.User_Alc[Mode].AGCInfo.enableAGC;
                                            alc.minExposure = caminfo.User_Alc[Mode].AECInfo.minExposure;
                                            alc.maxExposure = caminfo.User_Alc[Mode].AECInfo.maxExposure;
                                            alc.minGain = caminfo.User_Alc[Mode].AGCInfo.minGain;
                                            alc.maxGain = caminfo.User_Alc[Mode].AGCInfo.maxGain;
                                            alc.target = caminfo.User_Alc[Mode].target;
                                            cam.SetALC(alc);
                                        }
                                        cam.GetTriggerImageCount(out cnt);
                                        Util.Logger.Log(string.Format("{0}번 카메라 GetTriggerImageCount {1}", i + 1, cnt));
                                        if (cnt != caminfo.TriggerCnt)
                                        {
                                            cam.SetTriggerImageCount(caminfo.TriggerCnt);
                                            Util.Logger.Log(string.Format("{0}번 카메라 SetTriggerImageCount {1}", i + 1, caminfo.TriggerCnt));
                                        }
                                        //cam.SaveSetting();
                                    }
                                    else if (caminfo.User_Setting[idx].UseBarkect)
                                    {
                                        Util.Logger.Log(string.Format("{0}번 카메라 BARAKECT 모드", i + 1));
                                        cam.GetBracketMode(out blBarakect, out cnt);
                                        if (!blBarakect || caminfo.BarkectCnt != cnt)
                                        {
                                            cam.SetBracketMode(true, caminfo.BarkectCnt);
                                            Util.Logger.Log(string.Format("{0}번 카메라 BARKET 모드 활성화 {1}", i + 1, caminfo.BarkectCnt));
                                        }
                                        
                                        for (int ch = 0; ch < 4; ch++)
                                        {
                                            int exposure  = 0;
                                            double dgain = 0;
                                            int again = 0;
                                            cam.GetBracketInfo(ch, out exposure, out again, out dgain);
                                            if (exposure != caminfo.User_Brakect[Mode, ch].Exposure)
                                            {
                                                for (int j = 0; j < 4; j++)
                                                {
                                                    //bool ret = cam.SetBracketInfo(j, caminfo.User_Brakect[Mode, j].Exposure, again, dgain);
                                                    cam.SetBracketInfo(j, caminfo.User_Brakect[Mode, j].Exposure, again, dgain);
                                                }
                                                break;
                                            }
                                        }
                                        //cam.SaveSetting();
                                    }
                                }
                            }
                        }
                        LastJob = DateTime.Now;
                        GetCameraInfo();
                    }
                }
                catch (Exception) {
                }
                Thread.Sleep(60000);
            }
        }

        private int UseCheck(int camidx)
        {
            int rtn = -1;
            ClsStructure.User_Setting[] camsetting = new ClsStructure.User_Setting[3];
            switch (camidx)
            {
                case 0:
                    camsetting = ENV.CameraEnv.IPCamera1Info.User_Setting;
                    break;
                case 1:
                    camsetting = ENV.CameraEnv.IPCamera2Info.User_Setting;
                    break;
            }
            for (int i = 0; i < camsetting.Length; i++)
            {
                if (camsetting[i].use)
                {
                    string[] sp = camsetting[i].StartTime.Split(':');
                    int hr = 0;
                    int.TryParse(sp[0], out hr);
                    int min = 0;
                    int.TryParse(sp[1],out min);
                    //DateTime StartTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hr, min, 0);
                    int Start = hr * 60 + min;
                    sp = camsetting[i].EndTime.Split(':');
                    hr = 0;
                    int.TryParse(sp[0], out hr);
                    min = 0;
                    int.TryParse(sp[1], out min);
                    //DateTime EndTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hr, min, 0);
                    int End = hr * 60 + min;
                    int n = DateTime.Now.Hour * 60 + DateTime.Now.Minute;
                    //if (StartTime <= EndTime)@
                    //{
                    //    if (StartTime <= DateTime.Now && EndTime > DateTime.Now)
                    //    {
                    //        rtn = i;
                    //        break;
                    //    }
                    //}
                    //else
                    //{
                    //    EndTime.AddDays(1);
                    //    if (StartTime <= DateTime.Now && EndTime < DateTime.Now)
                    //    {
                    //        rtn = i;
                    //        break;
                    //    }
                    //}
                    if (Start <= End)
                    {
                        if (Start <= n && End > n)
                        {
                            rtn = i;
                            break;
                        }
                    }
                    else
                    {
                        if (End <= n || n < Start)
                        {
                            rtn = i;
                            break;
                        }
                    }
                }
            }
            return rtn;
        }

        delegate void SetListItemAddCallback(string txt);
        public void ListItemAdd(string Txt)
        {
            Util.Logger.Log(Txt);
            try
            {
                if (frm != null)
                {
                    if (Txt.IndexOf('\n') < 0)
                        frm.listBox1.Items.Add(string.Format("{0} {1}", DateTime.Now.ToString("HH:mm:ss"), Txt));
                    else
                    {
                        string[] sp = Txt.Split('\n');
                        foreach (string item in sp)
                        {
                            frm.listBox1.Items.Add(string.Format("{0} {1}", DateTime.Now.ToString("HH:mm:ss"), item));
                        }
                    }
                    //if (frm.listBox1.Items.Count > 1000)
                    //    for (int i = 10000; i < frm.listBox1.Items.Count; i++)
                    //    {
                    //        frm.listBox1.Items.RemoveAt(i);
                    //    }

                    frm.listBox1.SelectedIndex = frm.listBox1.Items.Count - 1;
                }
            }
            catch (Exception)
            { }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!frm.Visible)
                frm.Visible = true;
            frm.Activate();
            clsThread.frm = frm;
        }

        private void frmLprMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                clsImageUploader.Stop();
                m_keepGrab1 = false;
                m_keepGrab2 = false;
#if WIN64
                if(ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic && ENV.CameraEnv.RecogMode == 1) {
                    CoreLogic.ReleaseFAVE();
                    _rtspPreview1?.Dispose(); _rtspPreview1 = null;
                    _rtspPreview2?.Dispose(); _rtspPreview2 = null;
                }
#endif
                if (ENV.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                {
                    if (NetDisPlay1 != null && ENV.CommunicationEnv.DisPlay[1].Net.Use)
                    {
                        NetDisPlay1.SendMsg("경신파킹", (byte)ClsStructure.Color8.백색, "전광판테스트", (byte)ClsStructure.Color8.파랑);
                    }
                    else if (SerialDev.FirstDisPlay8 != null)
                    {
                        SerialDev.FirstDisPlay8.TimerSync();
                        SerialDev.FirstDisPlay8.SendDisplay("경신파킹", "전광판테스트", (byte)ClsStructure.Color8.백색, (byte)ClsStructure.Color8.파랑);
                    }
                    //SerialDev.EntranceDisPlay8.SendDisplay(SerialDev.EntranceDisPlay8.GetOneLineMessageByte("국제종합", "전광판테스트", (byte)ClsStructure.Color8.백색, (byte)ClsStructure.Color8.파랑));
                }
                if (ENV.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                {
                    if (NetDisPlay2 != null && ENV.CommunicationEnv.DisPlay[1].Net.Use)
                    {
                        NetDisPlay2.SendMsg("경신파킹", (byte)ClsStructure.Color8.백색, "전광판테스트", (byte)ClsStructure.Color8.파랑);
                    }
                    else if (SerialDev.SecondDisPlay8 != null)
                    {
                        SerialDev.SecondDisPlay8.TimerSync();
                        SerialDev.SecondDisPlay8.SendDisplay("경신파킹", "전광판테스트", (byte)ClsStructure.Color8.백색, (byte)ClsStructure.Color8.파랑);
                    }
                    //SerialDev.ExitDisPlay8.SendDisplay(SerialDev.ExitDisPlay8.GetOneLineMessageByte("국제종합", "전광판테스트", (byte)ClsStructure.Color8.백색, (byte)ClsStructure.Color8.파랑));
                }
                if (ENV.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                {
                    if (NetDisPlay1 != null && ENV.CommunicationEnv.DisPlay[0].Net.Use)
                    {
                        NetDisPlay1.SendMsg("경신파킹", (byte)ClsStructure.Color8.백색, "전광판테스트", (byte)ClsStructure.Color8.파랑);
                    }
                    else if (SerialDev.FirstDisPlayAmano3 != null)
                    {
                        SerialDev.FirstDisPlayAmano3.SendDisplay("경신파킹", 1, "전광판테스트", 2);
                    }
                    //SerialDev.ExitDisPlay8.SendDisplay(SerialDev.ExitDisPlay8.GetOneLineMessageByte("국제종합", "전광판테스트", (byte)ClsStructure.Color8.백색, (byte)ClsStructure.Color8.파랑));
                }
                if (ENV.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                {
                    if (NetDisPlay2 != null && ENV.CommunicationEnv.DisPlay[1].Net.Use)
                    {
                        NetDisPlay2.SendMsg("경신파킹", (byte)ClsStructure.Color8.백색, "전광판테스트", (byte)ClsStructure.Color8.파랑);
                    }
                    else if (SerialDev.SecondDisPlayAmano3 != null)
                    {
                        SerialDev.SecondDisPlayAmano3.SendDisplay("경신파킹", 1, "전광판테스트", 2);
                    }
                    //SerialDev.ExitDisPlay8.SendDisplay(SerialDev.ExitDisPlay8.GetOneLineMessageByte("국제종합", "전광판테스트", (byte)ClsStructure.Color8.백색, (byte)ClsStructure.Color8.파랑));
                }

                Process.GetCurrentProcess().Kill();
            }
            catch (Exception)
            { }
        }


#region Socket
        private void SocketInit()
        {
            try
            {
                //if (ENV.CommunicationEnv.Listen.Use)
                //{
                //    this.server = new Server();
                //    //server.Closeed += new SocketServer.eventOnClose(Server_Closeed);
                //    //server.Connected += new SocketServer.eventOnConnect(Server_Connected);
                //    //server.Receive += new SocketServer.eventReceive(Server_Receive);
                //    //server.SendComplite += new SocketServer.eventSendComplite(Server_SendComplite);
                    
                //    //this.server.InitServer(ENV.CommunicationEnv.Listen.Port);
                //    server.StartServer(ENV.CommunicationEnv.Listen.Port);
                //}
                //if (ENV.CommunicationEnv.ClientTarget.Length > 0)
                //{
                //    //client1.Dele_OnConnect += new SocketClient.eventOnConnect(Client_OnConnect);
                //    //client1.Dele_OnClose += new SocketClient.eventOnClose(Client_OnClose);
                //    //client1.Dele_SendComplite += new SocketClient.eventSendComplite(Client_OnSend);
                //    //client1.Dele_Receive += new SocketClient.eventReceive(Client_OnReceive);
                //    //client1.Dele_OnError += new SocketClient.eventOnError(Client_OnError);

                //    //this.client1 = new SocketClient();
                //    //this.client1.ConnectionEvent += new SocketServerLib.SocketHandler.SocketConnectionDelegate(client1_ConnectionEvent);
                //    //this.client1.CloseConnectionEvent += new SocketServerLib.SocketHandler.SocketConnectionDelegate(client1_CloseConnectionEvent);
                //    //this.client2Guid = Guid.NewGuid();
                //    //this.client2 = new BasicSocketClient();
                //    //this.client2.ConnectionEvent += new SocketServerLib.SocketHandler.SocketConnectionDelegate(client2_ConnectionEvent);
                //    //this.client2.CloseConnectionEvent += new SocketServerLib.SocketHandler.SocketConnectionDelegate(client2_CloseConnectionEvent);
                //    //this.client3Guid = Guid.NewGuid();
                //    //this.client3 = new BasicSocketClient();
                //    //this.client3.ConnectionEvent += new SocketServerLib.SocketHandler.SocketConnectionDelegate(client3_ConnectionEvent);
                //    //this.client3.CloseConnectionEvent += new SocketServerLib.SocketHandler.SocketConnectionDelegate(client3_CloseConnectionEvent);
                //    //this.client4Guid = Guid.NewGuid();
                //    //this.client4 = new BasicSocketClient();
                //    //this.client4.ConnectionEvent += new SocketServerLib.SocketHandler.SocketConnectionDelegate(client4_ConnectionEvent);
                //    //this.client4.CloseConnectionEvent += new SocketServerLib.SocketHandler.SocketConnectionDelegate(client4_CloseConnectionEvent);
                //    //this.client.Connect(new IPEndPoint(IPAddress.Loopback, 8100));
                //}
                bool socketUse = false;
                if (ENV.CommunicationEnv.ClientTarget[0].Use)
                {
                    //HomeLan.Dele_OnConnect += new SocketClient.eventOnConnect(Client_OnConnect);
                    //HomeLan.Dele_OnClose += new SocketClient.eventOnClose(Client_OnClose);
                    //HomeLan.Dele_SendComplite += new SocketClient.eventSendComplite(Client_OnSend);
                    //HomeLan.Dele_Receive += new SocketClient.eventReceive(Client_OnReceive);
                    //HomeLan.Dele_OnError += new SocketClient.eventOnError(Client_OnError);
                    HomeLan.Connect_Ip  = ENV.CommunicationEnv.ClientTarget[0].IP;
                    HomeLan.Connect_Port = ENV.CommunicationEnv.ClientTarget[0].Port;
                    HomeLan.Connect_Server();
                    socketUse = true;
                }
                if (ENV.CommunicationEnv.ClientTarget[1].Use)
                {
                    try {
                        LprExitSvr.StartServer(ENV.CommunicationEnv.ClientTarget[1].Port);
                        Util.Logger.Log(string.Format("[소켓] 요금계산기(출차) 서버 리스닝 시작 포트 {0}", ENV.CommunicationEnv.ClientTarget[1].Port));
                        Thread t = new Thread(new ThreadStart(LprExitSvrProcessPackets));
                        t.IsBackground = true;
                        t.Start();
                    } catch (Exception sx) {
                        Util.Logger.Log(string.Format("[소켓] 요금계산기 서버 시작 실패 포트 {0} — {1} (중복 실행/포트점유 확인! 무인정산기 접속 불가)", ENV.CommunicationEnv.ClientTarget[1].Port, sx.Message));
                    }
                }
                if (ENV.CommunicationEnv.ClientTarget[2].Use)
                {
                    DisPlaySvr.StartServer(ENV.CommunicationEnv.ClientTarget[2].Port);
                    Thread t = new Thread(new ThreadStart(DisPlaySvrProcessPackets));
                    t.IsBackground = true;
                    t.Start();
                }
                if (ENV.CommunicationEnv.ClientTarget[3].Use)
                {
                    StoneSvr.StartServer(ENV.CommunicationEnv.ClientTarget[3].Port);
                }
                if (ENV.CommunicationEnv.ClientTarget[4].Use)
                {
                    try {
                        LprEntSvr.StartServer(ENV.CommunicationEnv.ClientTarget[4].Port);
                        Util.Logger.Log(string.Format("[소켓] 입차 서버 리스닝 시작 포트 {0}", ENV.CommunicationEnv.ClientTarget[4].Port));
                        Thread t = new Thread(new ThreadStart(LprEntSvrProcessPackets));
                        t.IsBackground = true;
                        t.Start();
                    } catch (Exception sx) {
                        Util.Logger.Log(string.Format("[소켓] 입차 서버 시작 실패 포트 {0} — {1} (중복 실행/포트점유 확인!)", ENV.CommunicationEnv.ClientTarget[4].Port, sx.Message));
                    }
                }
                if (socketUse)
                {
                    Thread t = new Thread(new ThreadStart(SocketStatusWatcher));
                    t.IsBackground = true;
                    t.Start();
                }

                if (LprRelay.USE)
                {
                    if (LprRelay.TYPE == "SERVER")
                    { }
                    else if (LprRelay.TYPE == "CLIENT")
                    { }
                }                
            }
            catch (Exception e)
            {
                Util.Logger.Log("Socket Init Error " + e.Message);
            }
        }

        //void Server_Closeed()
        //{
        //    Console.WriteLine("Server_Closeed");
        //}

        //void Server_Connected()
        //{
        //    Console.WriteLine("Server_Connected");
        //}

        void Server_Receive()
        {
            while (Thread_Alive)
            {
                //List<string> RcvList = server.RecvMsgList;
                //lock (RcvList)
                //{
                //    try
                //    {
                //        while (RcvList.Count > 0)
                //        {
                //            string item = RcvList[0];
                //            DataParsing(item);
                //            RcvList.Remove(item);
                //        }
                //    }
                //    catch (Exception)
                //    { }
                //}
                Thread.Sleep(100);
            }
        }

        //private void Server_SendComplite()
        //{
        //    Console.WriteLine("Server_SendComplite");
        //}

        private void Client_OnClose()
        {
            Console.WriteLine("Client_OnClose");
        }

        private void Client_OnConnect()
        {
            Console.WriteLine("Client_OnConnect");
        }

        private void Client_OnError(String Errment)
        {
            Console.WriteLine("Client_OnError " + Errment);
        }

        private void Client_OnReceive(String Message)
        {
            Console.WriteLine("Client_OnReceive");
        }

        private void Client_OnSend()
        {
            Console.WriteLine("Client_OnSend");
        }

        private void DataParsing(string Msg)
        {
            Util.Logger.Log(string.Format("데이터 수신 {0}", Msg));
        }

        public void SendClient(string CHName, string CarNo, string DatePath, string Fname)
        {
            //return;
            //A type : CH01#No_Detection#20160105\CH01_No_Detection_20160105174537.jpg
            //K Type : CH01#No_Detection#20160105\CH01_No_Detection_20160105174537.jpg
            string msg = string.Format("{0}#{1}#{2}\\{0}_{1}_{3}", CHName, CarNo, DatePath, Fname);
            //SocketClient[] client = new SocketClient[] { client1, client2, client3, client4 };
            //int i = 0;
            //foreach (ClsStructure.Sock_Info item in ENV.CommunicationEnv.ClientTarget)
            //{
            //    if (item.IP.Trim() != string.Empty && item.Port > 0)
            //    {
            //        try
            //        {
            //            if (item.Use)
            //            {
            //                SendMsg[i] = msg;
            //                Console.WriteLine(string.Format("{0}, {1}", item.IP, item.Port));
            //                client[i].Connection(item.IP, item.Port, msg);
            //                Util.Logger.Log(string.Format("{0} {1} {2} 전송", item.IP, item.Port, msg));
            //            }
            //        }
            //        catch (Exception)
            //        { }
            //    }
            //    i++;
            //}
            if (ENV.SendOffice) 
                Main.SendOfficeList.Add(string.Format("CAPTURE:{0}", msg));
            if (ENV.CommunicationEnv.ClientTarget[1].Use)
            {
                if (LprExitSvr.ClientCount > 0)
                {
                    Util.Logger.Log(string.Format("{0} 전송", msg));
                    if (ENV.CameraEnv.IPCamera2Info.SendStxEtx)
                        LprExitSvr.SendMsgSTXETX(msg);
                    else
                        LprExitSvr.SendMessage(msg);
                    
                }
            }
        }
#endregion

        private void DisPlaySvrProcessPackets()
        {
            while (true == DisPlaySvr.m_KeepAlive)
            {
                //
                //  Packet Process : PacketQueue -> Packet
                //
                while (DisPlaySvr.m_PacketQueue.Count > 0)
                {
                    lock (DisPlaySvr.m_PacketQueue)
                    {
                        Packet packet = (Packet)DisPlaySvr.m_PacketQueue.Dequeue();
                        Console.WriteLine("DisPlay : " + packet.PacketData);
                        DisPlaySvr.SendMessage(packet.PacketData);
                        string[] strSp = packet.PacketData.Split(new string[] { "CALC^" }, StringSplitOptions.None);
                        switch (strSp[0])
                        {
                            case "Alive":
                                {
                                    packet.Client.ResetLive();
                                }
                                break;
                            default:
                                DisplayServer_Receive(ENV.CommunicationEnv.ClientTarget[2].Type, strSp[0]);
                                break;
                        }
                        //ClsStructure.DisPlay_Info DisPlayInfo;
                        //for (int i = 0; i < strSp.Length; i++)
                        //{
                        //    Console.WriteLine(strSp[i]);
                        //    string[] sp = strSp[i].Split('^');
                        //    switch (sp[0])
                        //    {
                        //        case "!live":
                        //        case "OK":
                        //            packet.Client.ResetLive();
                        //            break;
                        //        case "DISPLAY":
                        //        case "CANCEL":
                        //        case "FINISH":
                        //            break;
                        //    }
                        //}
                    }
                    Thread.Sleep(50);
                }
            }
        }

        private void DisplayServer_Receive(int ID, String Message)
        {
            Util.Logger.Log(string.Format("DisplayServer_Receive {0}", Message));
            //DisplayServer.Send(ID, "ACK");
            SerialDevice.ReturnDisPlay Display = null;

            switch (ENV.CommunicationEnv.ClientTarget[2].Type)
            {
                case 1:
                    Display = FirstDisPlayReturn;
                    break;
                case 2:
                    Display = SecondDisPlayReturn;
                    break;
            }
            //Message = Message.Replace("Alive", "");
            //string ChkStr = Message.Substring(0, 1);
            //switch (ChkStr)
            //{
            //    case "E":
            //        //전광판 초기화
            //        Util.Logger.Log(string.Format("전광판 리셋"));
            //        Display.DisPlayTime = DateTime.Now.AddSeconds(-10);
            //        break;
            //    case "R":
            //    case "W":
            //        //전광판 초기화
            //        Util.Logger.Log(string.Format("전광판 리셋"));
            //        Display.DisPlayTime = DateTime.Now.AddSeconds(-10);
            //        Util.Logger.Log(string.Format("차단기 개방"));
            //        SerialDev.GateOpen(ENV.CommunicationEnv.ClientTarget[2].Type - 1);
            //        break;
            //    case "D":
            //        Util.Logger.Log(string.Format("주차요금 {0}", Message.Substring(1, 6).Trim()));
            //        if (!frmLprMain.isFixed)
            //        {
            //            SerialDev.DisPlayMent(ENV.CommunicationEnv.ClientTarget[2].Type - 1, "주차요금", ENV.CommunicationEnv.DisPlay[ENV.CommunicationEnv.ClientTarget[2].Type - 1].Normal1Color, Message.Substring(1).Trim().PadLeft(12, ' '), ENV.CommunicationEnv.DisPlay[ENV.CommunicationEnv.ClientTarget[2].Type - 1].Normal2Color);
            //            Display.DisPlayTime = default(DateTime);
            //        }
            //        break;
            //    default:

            //        break;
            //}
            while (Message != "")
            {
                switch (Message.Substring(0, 1))
                {
                    case "A":
                        Message = Message.Substring(6);
                        break;
                    case "D":
                        string Fee = Message.Substring(1, 6);
                        Util.Logger.Log(string.Format("주차요금 {0}", Fee));
                        if (!frmLprMain.isFixed)
                        {
                            if (!ENV.CommunicationEnv.DisPlay[0].Net.Use && !ENV.CommunicationEnv.DisPlay[1].Net.Use)
                            {
                                SerialDev.DisPlayMent(ENV.CommunicationEnv.ClientTarget[2].Type - 1, "주차요금", ENV.CommunicationEnv.DisPlay[ENV.CommunicationEnv.ClientTarget[2].Type - 1].Normal1Color, Fee.Trim().PadLeft(12, ' '), ENV.CommunicationEnv.DisPlay[ENV.CommunicationEnv.ClientTarget[2].Type - 1].Normal2Color);
                                FirstDisPlayReturn.DisPlayTime = DateTime.MinValue;
                            }
                            else if (ENV.CommunicationEnv.DisPlay[0].Net.Use || ENV.CommunicationEnv.DisPlay[1].Net.Use)
                            {
                                if (ENV.CommunicationEnv.ClientTarget[2].Type == 1)
                                    NetDisPlay1.SendMsg("주차요금", clsFunction.GetColor8Int(ENV.CommunicationEnv.DisPlay[ENV.CommunicationEnv.ClientTarget[2].Type - 1].Normal1Color),
                                        Fee.Trim().PadLeft(12, ' '), clsFunction.GetColor8Int(ENV.CommunicationEnv.DisPlay[ENV.CommunicationEnv.ClientTarget[2].Type - 1].Normal2Color));
                                else if (ENV.CommunicationEnv.ClientTarget[2].Type == 2)
                                    NetDisPlay2.SendMsg("주차요금", clsFunction.GetColor8Int(ENV.CommunicationEnv.DisPlay[ENV.CommunicationEnv.ClientTarget[2].Type - 1].Normal1Color),
                                        Fee.Trim().PadLeft(12, ' '), clsFunction.GetColor8Int(ENV.CommunicationEnv.DisPlay[ENV.CommunicationEnv.ClientTarget[2].Type - 1].Normal2Color));
                            }
                            Display.DisPlayTime = default(DateTime);
                        }
                        Message = Message.Substring(7);
                        break;
                    case "E":
                        //전광판 초기화
                        Util.Logger.Log(string.Format("전광판 리셋"));
                        Display.DisPlayTime = DateTime.Now.AddSeconds(-10);
                        Message = Message.Substring(1);
                        break;
                    case "R":
                    case "W":
                        //전광판 초기화
                        Util.Logger.Log(string.Format("전광판 리셋"));
                        Display.DisPlayTime = DateTime.Now.AddSeconds(-10);
                        Util.Logger.Log(string.Format("차단기 개방"));
                        SerialDev.GateOpen(ENV.CommunicationEnv.ClientTarget[2].Type - 1);
                        Message = Message.Substring(1);
                        break;
                    case "T":
                        Message = Message.Substring(5);
                        break;
                    default:
                        Message = Message.Substring(1);
                        break;
                }
            }
        }

        private void SocketStatusWatcher()
        {
            while (Thread_Alive)
            {
                try
                {
                    if (ENV.CommunicationEnv.ClientTarget[0].Use)
                    {
                        try
                        {
                            if (HomeLan.IsConnected)
                                HomeLan.SocketSendMsg("!live");
                            else
                            {
                                HomeLan.Close();
                                HomeLan.Connect_Server();
                            }
                        }
                        catch (Exception)
                        {
                            HomeLan.Connect_Server();
                        }
                    }
                    if (ENV.CommunicationEnv.ClientTarget[1].Use)
                    { }
                    if (ENV.CommunicationEnv.ClientTarget[2].Use)
                    { }
                    if (ENV.CommunicationEnv.ClientTarget[3].Use)
                    { }
                }
                catch (Exception)
                { }
                Thread.Sleep(2000);
            }
        }


        private void LPRCAMChecker()
        {
            while (Thread_Alive)
            {
                try
                {
                    try
                    {
                        if (LPRCam.IsConnected)
                        {
                            if (!LPRCam.SocketSendMsg("!live"))
                            {
                                LPRCam.Close();
                                LPRCam.Connect_Server();
                            }
                        }
                        else
                        {
                            LPRCam.Close();
                            LPRCam.Connect_Server();
                        }
                    }
                    catch (Exception)
                    {
                        LPRCam.Connect_Server();
                    }
                }
                catch (Exception)
                { }

                DateTime stime = DateTime.Now;
                while ((DateTime.Now - stime).TotalMilliseconds < 2000)
                {
                    try
                    {
                        string rcv = LPRCam.RecvList;
                        if (rcv != "")
                        {
                            //CH02#42구4671#20200713\\CH02_42구4671_20200713204600.jpg
                            //CH02#42구4671#CH02_20200713204622_42구4671.jpg
                            //CH1#42구4671#\\2020\\07\\13\\CH1_20200713204652_42구4671.jpg
                            Console.WriteLine(rcv);
                            if (rcv.Substring(0, ENV.CommunicationEnv.Lpr1Info.ChNo.Length) != ENV.CommunicationEnv.Lpr1Info.ChNo)
                            {
                                Util.Logger.Log(string.Format("설정 체널 {0} 수신 체널 {1}", ENV.CommunicationEnv.Lpr1Info.ChNo, rcv.Substring(0, ENV.CommunicationEnv.Lpr1Info.ChNo.Length)));
                                continue;
                            }
                            string[] sp = rcv.Split('#');
                            string PlateNo = sp[1];
                            if (PlateNo == "0000000000")
                                PlateNo = "No_Detection";
                            sp = sp[2].Split('\\');
                            string img = sp[sp.Length - 1];
                            ComPlateRegResult(1, PlateNo, img);
                        }
                        Thread.Sleep(100);
                    }
                    catch { }
                }
            }
        }
        public static void Noti(String inoutCode, String CardNo, String CarNo, String InDate, String OutDate, String Addr1, String Addr2, String OwnerName, int ParkNo, int ClientNo)
        {
            string strNoti = string.Empty;
            string hreader = "AMANOKOREA_HOMENET";
#region MyRegion
            // 입차
            //if (inoutCode == "01")
            //{
            //    //strNoti = "AMANOKOREA_HOMENET^len^";

            //    strNoti= "00^"
            //        + "00^"
            //        //+ Function.IniReadValue("PARK", "number", Function.INIPATH).ToString().Trim() + "^"
            //        //+ Function.IniReadValue("PARK", "pcnumber", Function.INIPATH).ToString().Trim() + "^"
            //        + ParkNo.ToString() + "^"
            //        + ClientNo + "^"
            //        + CardNo + "^"
            //        + CarNo + "^"
            //        + InDate + "^"
            //        + Addr1 + "^"
            //        + Addr2 + "^"
            //        + OwnerName + "";

            //    //int len = Func.getStrLength(strNotiTemp);

            //    //strNoti = strNoti.Replace("len", len.ToString().Trim());

            //    //strNoti = strNoti + strNotiTemp;
            //    strNoti = string.Format("AMANOKOREA_HOMENET^{0}^{1}", Encoding.Default.GetByteCount(strNoti), strNoti);
            //}
            //// 출차
            //else
            //{

            //    strNoti = "00^"
            //        + "01^"
            //        //+ Function.IniReadValue("PARK", "number", Function.INIPATH).ToString().Trim() + "^"
            //        //+ Function.IniReadValue("PARK", "pcnumber", Function.INIPATH).ToString().Trim() + "^"
            //        + ParkNo+ "^"
            //        + ClientNo + "^"
            //        + CardNo + "^"
            //        + CarNo + "^"
            //        + InDate + "^"
            //        + OutDate + "^"
            //        + Addr1 + "^"
            //        + Addr2 + "^"
            //        + OwnerName + "";
            //    strNoti = string.Format("AMANOKOREA_HOMENET^{0}^{1}", Encoding.Default.GetByteCount(strNoti), strNoti);

            //}
#endregion
            

            string tmp = string.Format("00^{0}^{1:000}^{2:000}^{3}^{4}^{5}^{6}^{7}^{8}",
                //inoutCode == "01" ? "00" : "01", ParkNo, ClientNo, CardNo, CarNo, inoutCode == "01" ? InDate : InDate + "^" + OutDate, Addr1, Addr2, OwnerName);
                inoutCode, ParkNo, ClientNo, CardNo, CarNo, inoutCode == "00" ? InDate : InDate + "^" + OutDate, Addr1, Addr2, OwnerName);
            int len = Encoding.Default.GetByteCount(tmp) + Encoding.Default.GetByteCount(hreader);
            if (len > 99)
                len += 3;
            else
                len += 2;
            strNoti = string.Format("{0}^{1}^{2}", hreader, len, tmp);
            try
            {
                Util.Logger.Log(string.Format("세대통보 : {0} \r\n접속 상태 : {1}", strNoti, Main.HomeLan.IsConnected));
                //listBox1.Items.Insert(0, logDtReturn() + "세대통보 : " + strNoti);
                //byte[] data = Encoding.Default.GetBytes(strNoti);
                //home.BeginSend(data, 0, data.Length, SocketFlags.None, HomeNetCallback_Send, HomeNetClient);
                Main.HomeLan.SocketSendMsg(strNoti);
                if (ENV.SendOffice) 
                    Main.SendOfficeList.Add(string.Format("HOMELAN:{0}", strNoti));
            }
            catch (Exception)
            {
            }
        }

        private void LprExitSvrProcessPackets()
        {
            while (true == LprExitSvr.m_KeepAlive)
            {
                //
                //  Packet Process : PacketQueue -> Packet
                //
                while (LprExitSvr.m_PacketQueue.Count > 0)
                {
                    lock (LprExitSvr.m_PacketQueue)
                    {
                        //Console.WriteLine("CNT : " + m_PacketQueue.Count);
                        Packet packet = (Packet)LprExitSvr.m_PacketQueue.Dequeue();
                        string msg = packet.PacketData;
                        if (msg.IndexOf("CALC^") > -1 || msg.IndexOf("UNMAN^") > -1)
                        {
                            //Util.Logger.Log(msg);
                            string[] strSp = new string[1];
                            if (msg.IndexOf("CALC^") > -1)
                                strSp = msg.Split(new string[] { "CALC^" }, StringSplitOptions.None);
                            if (msg.IndexOf("UNMAN^") > -1)
                                strSp = msg.Split(new string[] { "UNMAN^" }, StringSplitOptions.None);
                            switch (strSp[0])
                            {
                                case "Alive":
                                    {
                                        packet.Client.ResetLive();
                                    }
                                    break;

                            }
                            strSp[1] = strSp[1].Replace("!", "");
                            strSp[1] = strSp[1].Replace("LIVE", "");
                            strSp[1] = strSp[1].Replace("live", "");
                            strSp[1] = strSp[1].Replace("OK", "");
                            strSp[1] = strSp[1].Replace("LV", "");
                            ClsStructure.DisPlay_Info DisPlayInfo;
                            if (msg.IndexOf("!live") == -1 && msg.IndexOf("OK") == -1)
                                Util.Logger.Log(msg);
                            if (msg.IndexOf("CANCEL") > -1)
                            {
                            }
                            for (int i = 0; i < strSp.Length; i++)
                            {
                                Console.WriteLine(strSp[i]);
                                string[] sp = strSp[i].Split('^');
                                switch (sp[0])
                                {
                                    case "!live":
                                    case "OK":
                                        packet.Client.ResetLive();
                                        // [수정] 무인정산기 Live 신호에 회신 — 서버모드는 인식을 ParkingWeb에 위임해
                                        //        idle 시 LPR이 키오스크 소켓으로 검출 트래픽을 안 보냄 → 무인정산기가
                                        //        "LPR 무수신" 경고. keepalive에 !live 회신하면 LastReceivedAt 갱신되어 해소.
                                        //        검출 송신(4228)과 동일 프레이밍. 무인정산기는 !live/OK 회신을 무시 처리.
                                        try {
                                            if (ENV.CameraEnv.IPCamera2Info.SendStxEtx)
                                                LprExitSvr.SendMsgSTXETX("!live");
                                            else
                                                LprExitSvr.SendMessage("!live");
                                        } catch (Exception lex) { Util.Logger.Log("LprExit live 회신 오류: " + lex.Message); }
                                        break;
                                    case "DISPLAY":
                                    case "CANCEL":
                                        //CALC^DISPLAY^주차요금^2,000원^초록^노랑]
                                    case "FINISH":
                                        SerialDevice.ClsDisplay3Color Display3Color = null;
                                        SerialDevice.ClsDisplay8Color Display8Color = null;
                                        SerialDevice.Amano3ColorSmall DisplayAmano3Color = null;
                                        SerialDevice.ReturnDisPlay DisplayReturn = null;
                                        NetworkDisplay network = null;
                                        int lprno = 0;
                                        if (ENV.CommunicationEnv.Lpr1Info.InOutType.Equals((int)ClsStructure.InoutType.출구용))
                                        {
                                            lprno = 1;
                                            if (ENV.CommunicationEnv.DisPlay[0].Net.Use)
                                                network = NetDisPlay1;
                                            DisPlayInfo = ENV.CommunicationEnv.DisPlay[0];
                                            DisplayReturn = FirstDisPlayReturn;
                                        }
                                        else if (ENV.CommunicationEnv.Lpr2Info.InOutType.Equals((int)ClsStructure.InoutType.출구용))
                                        {
                                            lprno = 2;
                                            if (ENV.CommunicationEnv.DisPlay[1].Net.Use)
                                                network = NetDisPlay2;
                                            DisPlayInfo = ENV.CommunicationEnv.DisPlay[1];
                                            DisplayReturn = SecondDisPlayReturn;
                                        }
                                        else
                                            break;
                                        if (DisPlayInfo.Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                                        {
                                            if (lprno == 1)
                                                Display8Color = SerialDev.FirstDisPlay8;
                                            else
                                                Display8Color = SerialDev.SecondDisPlay8;
                                        }
                                        else if (DisPlayInfo.Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                                        {
                                            if (lprno == 1)
                                                Display3Color = SerialDev.FirstDisPlay3;
                                            else
                                                Display3Color = SerialDev.SecondDisPlay3;
                                        }
                                        else if (DisPlayInfo.Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                                        {
                                            if (lprno == 1)
                                                DisplayAmano3Color = SerialDev.FirstDisPlayAmano3;
                                            else
                                                DisplayAmano3Color = SerialDev.SecondDisPlayAmano3;
                                        }

                                        if (DisPlayInfo.Use)
                                        {
                                            Util.Logger.Log("정산 요금 전광판 출력");
                                            //ExitDisPlayReturn = new SerialDevice.ReturnDisPlay();
                                            //ExitDisPlayReturn.DisPlayTime = DateTime.Now.AddSeconds(-10);
                                            string Ment1;
                                            string Ment2;
                                            byte Color1 = 0;
                                            byte Color2 = 0;
                                            if (sp.Length > 2)
                                            {
                                                Ment1 = "".PadLeft(12 - Encoding.Default.GetByteCount(sp[1])) + sp[1];
                                                Ment2 = "".PadLeft(12 - Encoding.Default.GetByteCount(sp[2])) + sp[2];
                                                if (DisPlayInfo.Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                                                {
                                                    Color1 = (byte)clsFunction.GetColor8Int("녹색");
                                                    Color2 = (byte)clsFunction.GetColor8Int("노랑");
                                                }
                                                else if (DisPlayInfo.Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                                                {
                                                    Color1 = (byte)clsFunction.GetColor3Int("녹색");
                                                    Color2 = (byte)clsFunction.GetColor3Int("노랑");
                                                }
                                            }
                                            else
                                            {
                                                Ment1 = DisPlayInfo.Ment.Ment1Line;
                                                Ment2 = DisPlayInfo.Ment.Ment2Line;
                                                if (DisPlayInfo.Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                                                {
                                                    Color1 = (byte)clsFunction.GetColor8Int(DisPlayInfo.Ment.Ment1Color);
                                                    Color2 = (byte)clsFunction.GetColor8Int(DisPlayInfo.Ment.Ment2Color);
                                                }
                                                else if (DisPlayInfo.Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                                                {
                                                    Color1 = (byte)clsFunction.GetColor3Int(DisPlayInfo.Ment.Ment1Color);
                                                    Color2 = (byte)clsFunction.GetColor3Int(DisPlayInfo.Ment.Ment2Color);
                                                }
                                            }
                                            if (ENV.CommunicationEnv.DisPlay[lprno - 1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                                            {
                                                //ExitDisPlayReturn.DisPlay8 = SerialDev.ExitDisPlay8;
                                                //ExitDisPlayReturn.Color1 = clsFunction.GetColor8Int(strSp[4]);
                                                //ExitDisPlayReturn.Color2 = clsFunction.GetColor8Int(strSp[5]);
                                                //SerialDev.EntranceDisPlay8.SendDisplay(SerialDev.EntranceDisPlay8.GetMessageByte(strSp[2], strSp[3], (byte)clsFunction.GetColor8Int(strSp[4]), (byte)clsFunction.GetColor8Int(strSp[5])));
                                                //DisPlay8.SendDisplay(DisPlay8.GetMessageByte(Ment1, Ment2, (byte)Color1, (byte)Color2));

                                                if (network != null)
                                                {
                                                    network.SendMsg(Ment1, Color1, Ment2, Color2);
                                                    if (sp[0] == "DISPLAY")
                                                        network.DisPlayTime = DateTime.MinValue;
                                                    else
                                                        network.DisPlayTime = DateTime.Now.AddSeconds(-5);
                                                }
                                                else if (Display8Color != null)
                                                {
                                                    Display8Color.SendDisplay(Ment1, Ment2, Color1, Color2);
                                                    DisplayReturn.DisPlayTime = DateTime.Now;

                                                    Console.WriteLine(string.Format("DisPlay : {0} {1}", Ment1, Ment2));
                                                }
                                                if (DisplayReturn != null)
                                                    switch (sp[0])
                                                    {
                                                        case "DISPLAY":
                                                            //DisplayReturn.DisPlayTime = DateTime.Now.AddMinutes(-2);
                                                            PacketManager.SendPacket(packet.Client.TcpClient.GetStream(), Encoding.UTF8.GetBytes("CALCACK^"));
                                                            break;
                                                        case "CANCEL":
                                                        case "FINISH":
                                                            DisplayReturn.DisPlayTime = DateTime.Now.AddMinutes(-1);
                                                            PacketManager.SendPacket(packet.Client.TcpClient.GetStream(), Encoding.UTF8.GetBytes("CALCACK^"));
                                                            break;
                                                    }
                                            }
                                            else if (ENV.CommunicationEnv.DisPlay[lprno - 1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                                            {
                                                DisplayReturn.DisPlay3 = SerialDev.SecondDisPlay3;
                                                //DisplayReturn.Color1 = clsFunction.GetColor3Int(strSp[4]);
                                                //DisplayReturn.Color2 = clsFunction.GetColor3Int(strSp[5]);
                                                DisplayReturn.Color1 = Color1;
                                                DisplayReturn.Color2 = Color2;
                                                Display3Color.WriteDisPlay(Ment1, Ment2, Color1, Color2);
                                            }
                                            else
                                            if (ENV.CommunicationEnv.DisPlay[lprno - 1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                                            {
                                                //ExitDisPlayReturn.DisPlay8 = SerialDev.ExitDisPlay8;
                                                //ExitDisPlayReturn.Color1 = clsFunction.GetColor8Int(strSp[4]);
                                                //ExitDisPlayReturn.Color2 = clsFunction.GetColor8Int(strSp[5]);
                                                //SerialDev.EntranceDisPlay8.SendDisplay(SerialDev.EntranceDisPlay8.GetMessageByte(strSp[2], strSp[3], (byte)clsFunction.GetColor8Int(strSp[4]), (byte)clsFunction.GetColor8Int(strSp[5])));
                                                //DisPlay8.SendDisplay(DisPlay8.GetMessageByte(Ment1, Ment2, (byte)Color1, (byte)Color2));

                                                if (DisplayAmano3Color != null)
                                                {
                                                    DisplayAmano3Color.SendDisplay(Ment1, (uint)clsFunction.GetAmanoColor3uInt(DisPlayInfo.Ment.Ment1Color)
                                                                             , Ment2, (uint)clsFunction.GetAmanoColor3uInt(DisPlayInfo.Ment.Ment2Color));
                                                    if (sp[0] != "DISPLAY")
                                                        DisplayReturn.DisPlayTime = DateTime.Now;
                                                    else
                                                        DisplayReturn.DisPlayTime = DateTime.MinValue;

                                                    Console.WriteLine(string.Format("DisPlay : {0} {1}", Ment1, Ment2));
                                                }
                                                if (DisplayReturn != null)
                                                    switch (sp[0])
                                                    {
                                                        case "DISPLAY":
                                                            //DisplayReturn.DisPlayTime = DateTime.Now.AddMinutes(-2);
                                                            PacketManager.SendPacket(packet.Client.TcpClient.GetStream(), Encoding.UTF8.GetBytes("CALCACK^"));
                                                            break;
                                                        case "CANCEL":
                                                        case "FINISH":
                                                            DisplayReturn.DisPlayTime = DateTime.Now.AddMinutes(-1);
                                                            PacketManager.SendPacket(packet.Client.TcpClient.GetStream(), Encoding.UTF8.GetBytes("CALCACK^"));
                                                            break;
                                                    }
                                            }
                                            FullCheck();
                                        }
                                        break;
                                    case "GATEOPEN":
                                        //"CALC^GATEOPEN^ENTRANCE^" + value
                                        //"CALC^GATEOPEN^EXIT^" + value
                                        ClsStructure.InoutType CompareEnum = new ClsStructure.InoutType();

                                        switch (sp[1])
                                        {
                                            case "ENTRANCE":
                                                CompareEnum = (ClsStructure.InoutType)Enum.Parse(typeof(ClsStructure.InoutType), "입구용");
                                                break;
                                            case "EXIT":
                                                CompareEnum = (ClsStructure.InoutType)Enum.Parse(typeof(ClsStructure.InoutType), "출구용");
                                                break;
                                        }
                                        if (ENV.CommunicationEnv.Lpr1Info.InOutType == (int)CompareEnum)
                                        {
                                            lprno = 0;

                                        }
                                        else if (ENV.CommunicationEnv.Lpr2Info.InOutType == (int)CompareEnum)
                                        {
                                            lprno = 1;
                                        }
                                        else
                                            break;
                                        Util.Logger.Log("차단기 개방 신호 수신");
                                        SerialDev.GateOpen(lprno);
                                        break;
                                    case "STAY":
                                        //STAY^MANUALFULL^RELEASE
                                        if (sp.Length > 1)
                                        {
                                            if (sp[1] == "MANUALFULL")
                                            {
                                                if (sp[2] == "SET")
                                                {
                                                    Util.Logger.Log("수동 만차 설정");
                                                    FullSpaceControl.ForceFull = true;
                                                }
                                                else
                                                {
                                                    Util.Logger.Log("수동 만차 해제");
                                                    if (FullSpaceControl.ForceFull && FullSpaceControl.EntGateOpen)
                                                    {
                                                        Util.Logger.Log("만차 해제시 차단기 개방");
                                                        SerialDev.GateOpen(0);
                                                    }
                                                    FullSpaceControl.ForceFull = false;
                                                }
                                            }
                                            else
                                            {

                                            }
                                        }
                                        FullCheck();
                                        break;
                                    case "OUTCALC":
                                        break;
                                }
                            }
                        }
                        else if (msg.IndexOf("MNG^") > -1)
                        {
                            string[] strSp = msg.Split(new string[] { "MNG^" }, StringSplitOptions.None);
                            string[] sp = strSp[1].Split('^');
                            switch (sp[0])
                            {
                                case "GateOpen":
                                    Util.Logger.Log(string.Format("원격 차단기 열림 접점 {0} {1}", packet.Client.IP, sp[1]));
                                    RemoteRelayOn(Util.Function.IntTryParse(sp[1]), 0, 1000);
                                    PacketManager.SendPacket(packet.Client.TcpClient.GetStream(), Encoding.UTF8.GetBytes("GateOpen^"));
                                    break;
                                case "GateClose":
                                    Util.Logger.Log(string.Format("원격 차단기 닫힘 접점 {0} {1}", packet.Client.IP, sp[1]));
                                    RemoteRelayOn(Util.Function.IntTryParse(sp[1]), 0, 1000);
                                    PacketManager.SendPacket(packet.Client.TcpClient.GetStream(), Encoding.UTF8.GetBytes("GateClose^"));
                                    break;
                                case "OpenFix":
                                    Util.Logger.Log(string.Format("원격 차단기 열림고정 접점 {0} {1} {2}", packet.Client.IP, sp[1], sp[2]));
                                    RemoteRelayFix(Util.Function.IntTryParse(sp[1]), sp[2]);
                                    PacketManager.SendPacket(packet.Client.TcpClient.GetStream(), Encoding.UTF8.GetBytes("GateClose^"));
                                    break;
                            }
                        }
                    }
                }


                System.Threading.Thread.Sleep(300);
            }
        }

        /// <summary>원격 GateOpen/Close — 보드 종류(DINGTIAN/KJC1000/REALSYS) 분기 — null 방어.</summary>
        private void RemoteRelayOn(int port, int delay, int keep)
        {
            try
            {
                if (SerialDev == null) { Util.Logger.Log("[RemoteRelayOn] SerialDev null"); return; }
                if (SerialDev.IsDingtian())
                {
                    if (SerialDev.Dingtian != null) SerialDev.Dingtian.RelayOn(port, delay, keep);
                    return;
                }
                string devName = ENV.CommonEnv.Dio.DioSetting.Dev_Type_Name;
                if (devName.Equals(ClsStructure.DeviceList.KJC1000.ToString()))
                {
                    if (SerialDev.KJC1000 != null) SerialDev.KJC1000.RelayOn(port, delay, keep);
                }
                else if (devName.Equals(ClsStructure.DeviceList.REALSYS.ToString()))
                {
                    if (SerialDev.RealSys != null) SerialDev.RealSys.RelayOn(port, delay, keep);
                }
            }
            catch (Exception ex) { Util.Logger.Log("[RemoteRelayOn] " + ex.Message); }
        }

        /// <summary>원격 OpenFix — KJC1000은 Relay(port,value), 나머지 보드는 RelayOn으로 대체 처리.</summary>
        private void RemoteRelayFix(int port, string value)
        {
            try
            {
                if (SerialDev == null) { Util.Logger.Log("[RemoteRelayFix] SerialDev null"); return; }
                string devName = ENV.CommonEnv.Dio.DioSetting.Dev_Type_Name;
                if (devName.Equals(ClsStructure.DeviceList.KJC1000.ToString()))
                {
                    if (SerialDev.KJC1000 != null) SerialDev.KJC1000.Relay(port, value);
                }
                else
                {
                    // DINGTIAN/REALSYS는 Relay(value) 미지원 — 일반 RelayOn 1초로 대체
                    RemoteRelayOn(port, 0, 1000);
                }
            }
            catch (Exception ex) { Util.Logger.Log("[RemoteRelayFix] " + ex.Message); }
        }

        private void LprEntSvrProcessPackets()
        {
            while (true == LprEntSvr.m_KeepAlive)
            {
                //
                //  Packet Process : PacketQueue -> Packet
                //
                while (LprEntSvr.m_PacketQueue.Count > 0)
                {
                    lock (LprEntSvr.m_PacketQueue)
                    {
                        //Console.WriteLine("CNT : " + m_PacketQueue.Count);
                        Packet packet = (Packet)LprEntSvr.m_PacketQueue.Dequeue();
                        Console.WriteLine(packet.PacketData);
                        string msg = packet.PacketData;
                        if (msg.IndexOf("CALC^") > -1)
                        {
                            string[] strSp = msg.Split(new string[] { "CALC^" }, StringSplitOptions.None);
                            switch (strSp[0])
                            {
                                case "Alive":
                                    {
                                        packet.Client.ResetLive();
                                    }
                                    break;
                                case "OK":
                                    {
                                        packet.Client.ResetLive();
                                    }
                                    break;
                            }
                            ClsStructure.DisPlay_Info DisPlayInfo;
                            for (int i = 0; i < strSp.Length; i++)
                            {
                                Console.WriteLine(strSp[i]);
                                string[] sp = strSp[i].Split('^');
                                switch (sp[0])
                                {
                                    case "!live":
                                        packet.Client.ResetLive();
                                        Console.WriteLine(packet.Client.LiveTime);
                                        // [수정] 무인정산기 Live 신호에 회신 (입차 서버) — 서버모드 idle 무수신 방지.
                                        try {
                                            if (ENV.CameraEnv.IPCamera1Info.SendStxEtx)
                                                LprEntSvr.SendMsgSTXETX("!live");
                                            else
                                                LprEntSvr.SendMessage("!live");
                                        } catch (Exception lex) { Util.Logger.Log("LprEnt live 회신 오류: " + lex.Message); }
                                        break;
                                    case "DISPLAY":
                                    case "CANCEL":
                                    case "FINISH":
                                        //CALC^DISPLAY^주차요금^2,000원^초록^노랑]
                                        SerialDevice.ClsDisplay3Color Display3Color = null;
                                        SerialDevice.ClsDisplay8Color Display8Color = null;
                                        SerialDevice.Amano3ColorSmall DisplayAmano3Color = null;
                                        SerialDevice.ReturnDisPlay DisplayReturn = null;
                                        int lprno = 0;
                                        if (ENV.CommunicationEnv.Lpr1Info.InOutType.Equals((int)ClsStructure.InoutType.출구용))
                                        {
                                            lprno = 1;
                                            DisPlayInfo = ENV.CommunicationEnv.DisPlay[0];
                                            DisplayReturn = FirstDisPlayReturn;
                                        }
                                        else if (ENV.CommunicationEnv.Lpr2Info.InOutType.Equals((int)ClsStructure.InoutType.출구용))
                                        {
                                            lprno = 2;
                                            DisPlayInfo = ENV.CommunicationEnv.DisPlay[1];
                                            DisplayReturn = SecondDisPlayReturn;
                                        }
                                        else
                                            break;
                                        if (DisPlayInfo.Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                                        {
                                            if (lprno == 1)
                                                Display8Color = SerialDev.FirstDisPlay8;
                                            else
                                                Display8Color = SerialDev.SecondDisPlay8;
                                        }
                                        else if (DisPlayInfo.Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                                        {
                                            if (lprno == 1)
                                                Display3Color = SerialDev.FirstDisPlay3;
                                            else
                                                Display3Color = SerialDev.SecondDisPlay3;
                                        }
                                        else if (DisPlayInfo.Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                                        {
                                            if (lprno == 1)
                                                DisplayAmano3Color = SerialDev.FirstDisPlayAmano3;
                                            else
                                                DisplayAmano3Color = SerialDev.SecondDisPlayAmano3;
                                        }

                                        if (DisPlayInfo.Use)
                                        {
                                            Util.Logger.Log("정산 요금 전광판 출력");
                                            //ExitDisPlayReturn = new SerialDevice.ReturnDisPlay();
                                            //ExitDisPlayReturn.DisPlayTime = DateTime.Now.AddSeconds(-10);
                                            string Ment1;
                                            string Ment2;
                                            byte Color1;
                                            byte Color2;
                                            if (sp.Length > 1)
                                            {
                                                Ment1 = "".PadLeft(12 - Encoding.Default.GetByteCount(sp[1])) + sp[1];
                                                Ment2 = "".PadLeft(12 - Encoding.Default.GetByteCount(sp[2])) + sp[2];
                                                Color1 = (byte)clsFunction.GetColor8Int("녹색");
                                                Color2 = (byte)clsFunction.GetColor8Int("노랑");
                                            }
                                            else
                                            {
                                                Ment1 = DisPlayInfo.Ment.Ment1Line;
                                                Ment2 = DisPlayInfo.Ment.Ment2Line;
                                                Color1 = (byte)clsFunction.GetColor8Int(DisPlayInfo.Ment.Ment1Color);
                                                Color2 = (byte)clsFunction.GetColor8Int(DisPlayInfo.Ment.Ment2Color);
                                            }
                                            if (ENV.CommunicationEnv.DisPlay[lprno - 1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                                            {
                                                //ExitDisPlayReturn.DisPlay8 = SerialDev.ExitDisPlay8;
                                                //ExitDisPlayReturn.Color1 = clsFunction.GetColor8Int(strSp[4]);
                                                //ExitDisPlayReturn.Color2 = clsFunction.GetColor8Int(strSp[5]);
                                                //SerialDev.EntranceDisPlay8.SendDisplay(SerialDev.EntranceDisPlay8.GetMessageByte(strSp[2], strSp[3], (byte)clsFunction.GetColor8Int(strSp[4]), (byte)clsFunction.GetColor8Int(strSp[5])));
                                                //DisPlay8.SendDisplay(DisPlay8.GetMessageByte(Ment1, Ment2, (byte)Color1, (byte)Color2));
                                                Display8Color.SendDisplay(Ment1, Ment2, Color1, Color2);
                                                if (DisplayReturn != null)
                                                    switch (sp[0])
                                                    {
                                                        case "DISPLAY":
                                                            DisplayReturn.DisPlayTime = DateTime.Now;
                                                            break;
                                                        case "CANCEL":
                                                        case "FINISH":
                                                            DisplayReturn.DisPlayTime = DateTime.Now.AddMinutes(-1);
                                                            //CalcSvr.SendMessage("CALCACK^" + strSp[i]);
                                                            break;
                                                    }
                                            }
                                            else if (ENV.CommunicationEnv.DisPlay[lprno - 1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                                            {
                                                //ExitDisPlayReturn.DisPlay3 = SerialDev.ExitDisPlay3;
                                                //ExitDisPlayReturn.Color1 = clsFunction.GetColor3Int(strSp[4]);
                                                //ExitDisPlayReturn.Color2 = clsFunction.GetColor3Int(strSp[5]);
                                                Display3Color.WriteDisPlay(Ment1, Ment2, (int)clsFunction.GetColor3Int(strSp[4]), (int)clsFunction.GetColor3Int(strSp[5]));
                                            }
                                            else if (ENV.CommunicationEnv.DisPlay[lprno - 1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                                            {
                                                DisplayAmano3Color.SendDisplay(Ment1, clsFunction.GetAmanoColor3uInt(DisPlayInfo.Ment.Ment1Color)
                                                                         , Ment2, clsFunction.GetAmanoColor3uInt(DisPlayInfo.Ment.Ment2Color));
                                                if (DisplayReturn != null)
                                                    switch (sp[0])
                                                    {
                                                        case "DISPLAY":
                                                            DisplayReturn.DisPlayTime = DateTime.Now;
                                                            break;
                                                        case "CANCEL":
                                                        case "FINISH":
                                                            DisplayReturn.DisPlayTime = DateTime.Now.AddMinutes(-1);
                                                            //CalcSvr.SendMessage("CALCACK^" + strSp[i]);
                                                            break;
                                                    }
                                            }
                                            FullCheck();
                                        }
                                        break;
                                    case "GATEOPEN":
                                        //"CALC^GATEOPEN^ENTRANCE^" + value
                                        //"CALC^GATEOPEN^EXIT^" + value
                                        ClsStructure.InoutType CompareEnum = new ClsStructure.InoutType();
                                        switch (sp[1])
                                        {
                                            case "ENTRANCE":
                                                CompareEnum = (ClsStructure.InoutType)Enum.Parse(typeof(ClsStructure.InoutType), "입구용");
                                                break;
                                            case "EXIT":
                                                CompareEnum = (ClsStructure.InoutType)Enum.Parse(typeof(ClsStructure.InoutType), "출구용");
                                                break;
                                        }
                                        if (ENV.CommunicationEnv.Lpr1Info.InOutType == (int)CompareEnum)
                                        {
                                            lprno = 0;
                                        }
                                        else if (ENV.CommunicationEnv.Lpr2Info.InOutType == (int)CompareEnum)
                                        {
                                            lprno = 1;
                                        }
                                        else
                                            break;
                                        Util.Logger.Log("차단기 개방 신호 수신");
                                        SerialDev.GateOpen(lprno);
                                        break;
                                    case "STAY":
                                        FullCheck();
                                        break;
                                    case "OUTCALC":
                                        break;
                                }
                            }
                        }
                        else if (msg.IndexOf("MNG^") > -1)
                        {
                            //MNG^GateOpen^1
                            string[] strSp = msg.Split(new string[] { "MNG^" }, StringSplitOptions.None);
                            string[] sp = strSp[1].Split('^');
                            switch (sp[0])
                            {
                                case "GateOpen":
                                    Util.Logger.Log(string.Format("원격 차단기 열림 접점 {0} {1}", packet.Client.IP, sp[1]));
                                    RemoteRelayOn(Util.Function.IntTryParse(sp[1]), 0, 1000);
                                    PacketManager.SendPacket(packet.Client.TcpClient.GetStream(), Encoding.UTF8.GetBytes("GateOpen^"));
                                    break;
                                case "GateClose":
                                    Util.Logger.Log(string.Format("원격 차단기 닫힘 접점 {0} {1}", packet.Client.IP, sp[1]));
                                    RemoteRelayOn(Util.Function.IntTryParse(sp[1]), 0, 1000);
                                    PacketManager.SendPacket(packet.Client.TcpClient.GetStream(), Encoding.UTF8.GetBytes("GateClose^"));
                                    break;
                                case "OpenFix":
                                    Util.Logger.Log(string.Format("원격 차단기 열림고정 접점 {0} {1} {2}", packet.Client.IP, sp[1], sp[2]));
                                    RemoteRelayFix(Util.Function.IntTryParse(sp[1]), sp[2]);
                                    PacketManager.SendPacket(packet.Client.TcpClient.GetStream(), Encoding.UTF8.GetBytes("GateClose^"));
                                    break;
                            }
                        }
                        else if (msg == string.Format("{0}CAPTURE{1}", (char)0x02, (char)0x03))
                        {
                            Util.Logger.Log("CAPTURE 수신");
                            dtRegList1.Rows.Clear();
                            Capture1 = true;
                            LastLoopTime1 = DateTime.Now;
                        }
                    }
                }


                System.Threading.Thread.Sleep(300);
            }
        }

        public void FullCheck()
        {
            if (FullSpaceControl.Use)
            {
                if (ENV.CommunicationEnv.DisPlay[0].Use)
                {
                    if (ENV.CommunicationEnv.Lpr1Info.InOutType == (int)ClsStructure.InoutType.입구용)
                    {
                        if (NetDisPlay1 != null && ENV.CommunicationEnv.DisPlay[0].Net.Use)
                        {
                            NetDisPlay1.isFull = FullSpaceControl.FullCheck(ENV.CommonEnv.DBInfo, ENV.CommunicationEnv.ParkInfo.No, ENV.CommunicationEnv.ParkInfo.Client_No);
                        }
                        else
                        {
                            bool full = FirstDisPlayReturn.isFull;
                            FirstDisPlayReturn.isFull = FullSpaceControl.FullCheck(ENV.CommonEnv.DBInfo, ENV.CommunicationEnv.ParkInfo.No, ENV.CommunicationEnv.ParkInfo.Client_No);
                        }
                        if (FirstDisPlayReturn != null)
                        {
                            if (FirstDisPlayReturn.isFull && ENV.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                            {
                                FirstDisPlayReturn.Ment1 = "지금은";
                                FirstDisPlayReturn.Ment2 = "만차입니다.";
                            }
                            else
                            {
                                FirstDisPlayReturn.Ment1 = ENV.CommunicationEnv.DisPlay[0].Ment.Ment1Line;
                                FirstDisPlayReturn.Ment2 = ENV.CommunicationEnv.DisPlay[0].Ment.Ment2Line;
                            }
                        }
                    }
                }

                if (ENV.CommunicationEnv.DisPlay[1].Use)
                {
                    if (NetDisPlay2 != null)
                    {
                        NetDisPlay2.isFull = FullSpaceControl.FullCheck(ENV.CommonEnv.DBInfo, ENV.CommunicationEnv.ParkInfo.No, ENV.CommunicationEnv.ParkInfo.Client_No);
                    }
                    else
                    {
                        if (SecondDisPlayReturn != null)
                        {
                            if (ENV.CommunicationEnv.Lpr2Info.InOutType == (int)ClsStructure.InoutType.입구용)
                            {
                                bool full = SecondDisPlayReturn.isFull;
                                SecondDisPlayReturn.isFull = FullSpaceControl.FullCheck(ENV.CommonEnv.DBInfo, ENV.CommunicationEnv.ParkInfo.No, ENV.CommunicationEnv.ParkInfo.Client_No);

                                if (ENV.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                                {
                                    SecondDisPlayReturn.Ment1 = "지금은";
                                    SecondDisPlayReturn.Ment2 = "만차입니다.";
                                }
                                else
                                {
                                    SecondDisPlayReturn.Ment1 = ENV.CommunicationEnv.DisPlay[1].Ment.Ment1Line;
                                    SecondDisPlayReturn.Ment2 = ENV.CommunicationEnv.DisPlay[1].Ment.Ment2Line;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ImageSaveTermCheck()
        {
            DateTime LastProcessTime = DateTime.MinValue;
            while (Thread_Alive)
            {
                try
                {
                    if (LastProcessTime == DateTime.MinValue || LastProcessTime.Day != DateTime.Now.Day)
                    {
                        Util.Logger.Log("폴더 삭제");
                        Util.Function.DeleteFolder(ENV.CameraEnv.ImageSave.SavePath, ENV.CameraEnv.ImageSave.SaveTerm);
                        if (ENV.CameraEnv.ImageSave.EtcSave)
                            Util.Function.DeleteFolder(ENV.CameraEnv.ImageSave.EtcPath, ENV.CameraEnv.ImageSave.SaveTerm);
                        LastProcessTime = DateTime.Now;
                    }
                    //GrabLoop Check
                    if (ENV.CameraEnv.IPCamera1Info.Use)
                    {
                        if (m_grabThread1 != null)
                        {
                            if (!m_grabThread1.IsAlive)
                            {
                                Util.Logger.Log(string.Format("카메라 1번 쓰레드 중단 재기동"));
                                m_keepGrab1 = false;
                                StartGrabLoop1();
                            }
                        }
                    }
                    if (ENV.CameraEnv.IPCamera2Info.Use)
                    {
                        if (m_grabThread2 != null)
                        {
                            if (!m_grabThread2.IsAlive)
                            {
                                Util.Logger.Log(string.Format("카메라 2번 쓰레드 중단 재기동"));
                                m_keepGrab2 = false;
                                StartGrabLoop2();
                            }
                        }
                    }
                    if (tExposure != null && !tExposure.IsAlive)
                    {
                        Util.Logger.Log(string.Format("Exposure 쓰레드 중단 재기동"));
                        //leess iNova2추가
                        tExposure = null;
                        // 카메라1·2 둘 다 USB이면 재기동 불필요
                        if(!(IsUsbCam(1) && IsUsbCam(2)))
                        {
                            if(ENV.CameraEnv.iNovaType == 1) tExposure = new Thread(new ThreadStart(UserSetting_Exposure_iNova1));
                            else if(ENV.CameraEnv.iNovaType == 2) tExposure = new Thread(new ThreadStart(UserSetting_Exposure_iNova2));
                            if(tExposure != null)
                            {
                                tExposure.IsBackground = true;
                                tExposure.Start();
                            }
                        }
                    }
                }
                catch (Exception ImageSaveTermCheck_Error)
                {
                    Util.Logger.Log(string.Format("ImageSaveTermCheck_Error : {0}", ImageSaveTermCheck_Error.Message));
                }
                Thread.Sleep(10000);//매 10초 마다 
            }
        }

        private void OfficeSendThread()
        {
            while (Thread_Alive)
            {
                try
                {
                    while (SendOfficeList.Count > 0)
                    {
                        if (!OfficeSocket.IsConnected)
                            OfficeSocket.Connect_Server();
                        if (OfficeSocket.IsConnected)
                        {
                            if (OfficeSocket.SocketSendMsg(SendOfficeList[0]))
                            {
                                Util.Logger.Log(Util.Logger.Log_Level.Event_Log, string.Format("OfficeSend : {0}", SendOfficeList[0]));
                                SendOfficeList.RemoveAt(0);
                            }
                        }
                        if (SendOfficeList.Count == 0)
                            OfficeSocket.Close();
                        Thread.Sleep(100);
                    }
                    Thread.Sleep(1000);
                }
                catch (Exception OfficeSendThread_Error)
                {
                    Util.Logger.Log(Util.Logger.Log_Level.Event_Log, string.Format("OfficeSendThread_Error : {0}", OfficeSendThread_Error.Message));
                }
            }
        }

        private void btnTestCapture1_Click(object sender, EventArgs e)
        {
//            Util.Logger.Log("테스트 버튼1 클릭");
//            clsThread.RegArray1[0].CapCnt = 0;
//            clsThread.RegArray1[0].SourcePath = @"D:\Program\KS\Image\20201007\CH01_16오0843_20201007091904.jpg";
//            clsThread.RegArray1[0].Roi = string.Format("{0},{1},{2},{3}", 0, 0, 0, 0);
//            clsThread.RegArray1[0].PlateRoi = null;
//            clsThread.RegArray1[0].PlateNo = null;
//            clsThread.RegArray1[0].FirstCaptureTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
//            clsThread.RegArray1[0].Send = false;

//            if (File.Exists(@"D:\Program\KS\Image\20201007\CH01_16오0843_20201007091904.jpg"))
//                clsThread.RegArray1[0].Size = new System.IO.FileInfo(@"D:\Program\KS\Image\20201007\CH01_16오0843_20201007091904.jpg").Length;
//            clsThread.RegArray1[0].Exposure = ENV.CameraEnv.IPCamera1Info.CurrentInfo.Generalinfo.Exposure;
//            Util.Logger.Log(string.Format("****CAM1 {0} reg Start CapCnt {1} ROI {2}", @"D:\Program\KS\Image\20201007\CH01_16오0843_20201007091904.jpg", 0, clsThread.RegArray1[0].Roi));
//            if (!Environment.Is64BitProcess)
//            {
//                if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Ngis)
//                    clsThread.RegPlateNoNgisWay(0, 0);
//                else if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Elwox)
//                    clsThread.RegPlateNoElwox(0, 0);
//            }
//            else
//            {
//#if WIN64
//                //CoreLogic
//                if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic)
//                    CoreLogic.Reg(0, 0);
//                Util.Logger.Log("AfterRegPlateCam Loop1");
//                Thread thread = new Thread(delegate ()
//                {
//                    clsThread.AfterRegPlateCam(0, ENV);
//                });
//                thread.IsBackground = true;
//                thread.Start();
//#endif
//            }

            string CarNo = txtTestCarNo.Text;
#region
            //if (m_camera1.IsStreamPortConnected())
            //{
            //    LastLoopTime1 = DateTime.Now;
            //    int CaptureCnt = 0;
            //    switch (ENV.CameraEnv.IPCamera1Info.CurrentInfo.BracketInfo.Use)
            //    {
            //        case true:
            //            CaptureCnt = ENV.CameraEnv.IPCamera1Info.BarkectCnt;
            //            break;
            //        default:
            //            CaptureCnt = ENV.CameraEnv.IPCamera1Info.TriggerCnt;
            //            break;
            //    }
            //    for (int i = 0; i < CaptureCnt; i++)
            //    {
            //        string fname = DateTime.Now.ToString("yyyyMMddHHmmssffff") + ImgCnt.ToString() + ".jpg";
            //        m_camera1.SaveLastImage(fname);

            //        Util.Logger.Log(string.Format("CAM1 {0} Test Saved", fname));
            //        RECT roi = new RECT();
            //        roi.x = ENV.CameraEnv.IPCamera1Info.Roi.Left;
            //        roi.y = ENV.CameraEnv.IPCamera1Info.Roi.Top;
            //        roi.w = ENV.CameraEnv.IPCamera1Info.Roi.Left + ENV.CameraEnv.IPCamera1Info.Roi.Width;
            //        roi.h = ENV.CameraEnv.IPCamera1Info.Roi.Top + ENV.CameraEnv.IPCamera1Info.Roi.Height;
            //        //dtRegList1.Rows.Add(null, 1, CapCnt + 1, fname, string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h), "", "", LastLoopTime1, false, 0, IpCam1Current.Generalinfo.Exposure);
            //        clsThread.RegArray1[i].CapCnt = 1;
            //        clsThread.RegArray1[i].SourcePath = fname;
            //        clsThread.RegArray1[i].Roi = string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h);
            //        clsThread.RegArray1[i].PlateRoi = null;
            //        clsThread.RegArray1[i].PlateNo = txtTestCarNo.Text;
            //        clsThread.RegArray1[i].FirstCaptureTime = LastLoopTime1.ToString("yyyy-MM-dd HH:mm:ss");
            //        clsThread.RegArray1[i].Send = false;
            //        //RegArray1[CapCnt].term = 0;
            //        clsThread.RegArray1[0].Exposure = ENV.CameraEnv.IPCamera1Info.CurrentInfo.Generalinfo.Exposure;
            //        Util.Logger.Log(string.Format("****CAM1 {0} reg Start CapCnt {1} ROI {2}", fname, 0, clsThread.RegArray1[0].Roi));

            //    }
            //    Util.Logger.Log("AfterRegPlateCam");
            //    Thread thread = new Thread(delegate()
            //    {
            //        clsThread.AfterRegPlateCam(0, ENV);
            //    });
            //    thread.IsBackground = true;
            //    thread.Start();
            //}
#endregion
            DateTime ptime = DateTime.Now;
            //if (ENV.CommunicationEnv.Lpr1Info.LprOpt.Normal_Tckttrns)
            {
                DataProcess.DataProcess(ENV.CommunicationEnv.Lpr1Info.InOutType, ENV, 0, CarNo,
                            string.Format("{0}_수동{1}_{2}.jpg", ENV.CameraEnv.IPCamera1Info.ChName, CarNo, ptime.ToString("yyyyMMddHHmmss")), ptime.ToString("yyyy-MM-dd HH:mm:ss"));
                FullCheck();
            }

            if (!ENV.CommunicationEnv.Lpr1Info.LprOpt.Normal_SendData)
            {
                string SendCal_Msg = clsFunction.MakeTransMessage(ENV.CameraEnv.SockDataFormat, ENV.CameraEnv.IPCamera1Info.ChName,
                    CarNo, ENV.CameraEnv.ImageSave.SavePath,
                    string.Format("{0}_수동{1}_{2}.jpg", ENV.CameraEnv.IPCamera1Info.ChName, CarNo, ptime.ToString("yyyyMMddHHmmss")), ptime);
                Util.Logger.Log(string.Format("요금계산기 정보 전송 {0}", SendCal_Msg));
                bool stxetx = ENV.CameraEnv.IPCamera1Info.SendStxEtx;
                if (ENV.CommunicationEnv.Lpr1Info.InOutType == 0)
                {
                    if (stxetx)
                        frmLprMain.Main.LprEntSvr.SendMsgSTXETX(SendCal_Msg);
                    else
                        frmLprMain.Main.LprEntSvr.SendMsg(SendCal_Msg);
                }
                else
                {
                    if (stxetx)
                        frmLprMain.Main.LprExitSvr.SendMsgSTXETX(SendCal_Msg);
                    else
                        frmLprMain.Main.LprExitSvr.SendMsg(SendCal_Msg);
                }
            }
        }

        private void btnTestCapture2_Click(object sender, EventArgs e)
        {
            Util.Logger.Log("테스트 캡쳐버튼2 클릭");
            if (!ENV.CameraEnv.IPCamera2Info.Use)
            {
                MessageBox.Show("카메라2 설정을 확인 하세요!!!\r\n미사용 상태 입니다!");
                return;
            }

            string CarNo = txtTestCarNo.Text;
#region
            //if (m_camera2.IsStreamPortConnected())
            //{
            //    LastLoopTime2 = DateTime.Now;
            //    int CaptureCnt = 0;
            //    switch (ENV.CameraEnv.IPCamera2Info.CurrentInfo.BracketInfo.Use)
            //    {
            //        case true:
            //            CaptureCnt = ENV.CameraEnv.IPCamera2Info.BarkectCnt;
            //            break;
            //        default:
            //            CaptureCnt = ENV.CameraEnv.IPCamera2Info.TriggerCnt;
            //            break;
            //    }
            //    for (int i = 0; i < CaptureCnt; i++)
            //    {
            //        string fname = DateTime.Now.ToString("yyyyMMddHHmmssffff") + ImgCnt.ToString() + ".jpg";
            //        m_camera2.SaveLastImage(fname);

            //        Util.Logger.Log(string.Format("CAM2 {0} Test Saved", fname));
            //        RECT roi = new RECT();
            //        roi.x = ENV.CameraEnv.IPCamera2Info.Roi.Left;
            //        roi.y = ENV.CameraEnv.IPCamera2Info.Roi.Top;
            //        roi.w = ENV.CameraEnv.IPCamera2Info.Roi.Left + ENV.CameraEnv.IPCamera2Info.Roi.Width;
            //        roi.h = ENV.CameraEnv.IPCamera2Info.Roi.Top + ENV.CameraEnv.IPCamera2Info.Roi.Height;
            //        //dtRegList1.Rows.Add(null, 1, CapCnt + 1, fname, string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h), "", "", LastLoopTime1, false, 0, IpCam1Current.Generalinfo.Exposure);
            //        clsThread.RegArray2[i].CapCnt = 1;
            //        clsThread.RegArray2[i].SourcePath = fname;
            //        clsThread.RegArray2[i].Roi = string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h);
            //        clsThread.RegArray2[i].PlateRoi = null;
            //        clsThread.RegArray2[i].PlateNo = txtTestCarNo.Text;
            //        clsThread.RegArray2[i].FirstCaptureTime = LastLoopTime2.ToString("yyyy-MM-dd HH:mm:ss");
            //        clsThread.RegArray2[i].Send = false;
            //        //RegArray1[CapCnt].term = 0;
            //        clsThread.RegArray2[0].Exposure = ENV.CameraEnv.IPCamera2Info.CurrentInfo.Generalinfo.Exposure;
            //        Util.Logger.Log(string.Format("****CAM2 {0} reg Start CapCnt {1} ROI {2}", fname, 0, clsThread.RegArray1[0].Roi));
            //    }
            //    Util.Logger.Log("AfterRegPlateCam");
            //    Thread thread = new Thread(delegate()
            //    {
            //        clsThread.AfterRegPlateCam(1, ENV);
            //    });
            //    thread.IsBackground = true;
            //    thread.Start();
            //}
#endregion
            DateTime ptime = DateTime.Now;
            //if (ENV.CommunicationEnv.Lpr2Info.LprOpt.Normal_Tckttrns)
            {
                DataProcess.DataProcess(ENV.CommunicationEnv.Lpr2Info.InOutType, ENV, 1, CarNo,
                        string.Format("{0}_수동{1}_{2}.jpg", ENV.CameraEnv.IPCamera2Info.ChName, CarNo, ptime.ToString("yyyyMMddHHmmss")), ptime.ToString("yyyy-MM-dd HH:mm:ss"));
                FullCheck();
            }

            if (!ENV.CommunicationEnv.Lpr2Info.LprOpt.Normal_SendData)
            {
                string SendCal_Msg = clsFunction.MakeTransMessage(ENV.CameraEnv.SockDataFormat, ENV.CameraEnv.IPCamera2Info.ChName,
                    CarNo, ENV.CameraEnv.ImageSave.SavePath,
                    string.Format("{0}_수동{1}_{2}.jpg", ENV.CameraEnv.IPCamera2Info.ChName, CarNo, ptime.ToString("yyyyMMddHHmmss")), ptime);
                Util.Logger.Log(string.Format("요금계산기 정보 전송 {0}", SendCal_Msg));
                bool stxetx = ENV.CameraEnv.IPCamera2Info.SendStxEtx;
                if (ENV.CommunicationEnv.Lpr2Info.InOutType == 0)
                {
                    if (stxetx)
                        frmLprMain.Main.LprEntSvr.SendMsgSTXETX(SendCal_Msg);
                    else
                        frmLprMain.Main.LprEntSvr.SendMsg(SendCal_Msg);
                }
                else
                {
                    if (stxetx)
                        frmLprMain.Main.LprExitSvr.SendMsgSTXETX(SendCal_Msg);
                    else
                        frmLprMain.Main.LprExitSvr.SendMsg(SendCal_Msg);
                }
            }
        }

        private void btnLoop_Click(object sender, EventArgs e)
        {
            if (chkLoop1.Checked)
            {
                SetLabelText(lblCam1Loop, "Loop ON");
                ListItemAdd(ENV.CameraEnv.IPCamera1Info.ChName + " Loop On");
                dtRegList1.Rows.Clear();
                Capture1 = true;
                LastLoopTime1 = DateTime.Now;
                NgisWay.Reg1Cnt = 0;
            }
            if (chkLoop2.Checked)
            {
                SetLabelText(lblCam2Loop, "Loop ON");
                ListItemAdd(ENV.CameraEnv.IPCamera2Info.ChName + " Loop On");
                dtRegList2.Rows.Clear();
                Capture2 = true;
                LastLoopTime2 = DateTime.Now;
                NgisWay.Reg2Cnt = 0;
            }
        }

        private void frmLprMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Alt || e.KeyCode == Keys.S)
            {
                bool serverMode = _serverPanel != null;
                // 서버모드: 상단 TEST1/2·차번입력 불필요(카드마다 자체 TEST 있음). 일반모드만 표시.
                if (!serverMode)
                {
                    btnTestCapture1.Visible = true;
                    btnTestCapture2.Visible = true;
                    txtTestCarNo.Visible = true;
                    chkLoop1.Visible = true;
                    chkLoop2.Visible = true;
                    btnLoop.Visible = true;
                    btnTestCapture1.BringToFront(); btnTestCapture2.BringToFront(); txtTestCarNo.BringToFront();
                    chkLoop1.BringToFront(); chkLoop2.BringToFront(); btnLoop.BringToFront();
                }
                // 서버모드 카드: 각 카드 캡처버튼 자리에 [차번입력][TEST] 표시
                if (serverMode) _serverPanel.SetTestMode(true);

                Thread th = new Thread(new ThreadStart(timeCheck));
                th.IsBackground = true;
                th.Start();
            }
        }

        private void timeCheck()
        {
            Thread.Sleep(60000);
            Util.Function.InvokeControlVisible(btnTestCapture1, false);
            Util.Function.InvokeControlVisible(btnTestCapture2, false);
            Util.Function.InvokeControlVisible(txtTestCarNo, false);
            Util.Function.InvokeControlVisible(chkLoop1, false);
            Util.Function.InvokeControlVisible(chkLoop2, false);
            Util.Function.InvokeControlVisible(btnLoop, false);
            try { if (_serverPanel != null) _serverPanel.SetTestMode(false); } catch { }   // 카드 캡처버튼 복귀
        }

        private void pictureBox1_DoubleClick(object sender, EventArgs e)
        {
            Application.ExitThread();
            Environment.Exit(0);
        }

        private void Timer_Core_Tick(object sender, EventArgs e)
        {
            progressBar1.Value++;
            if (progressBar1.Value == progressBar1.Maximum)
                progressBar1.Value = 0;
            if (!thCoreInit.IsAlive)
            {
                timer_Core.Enabled = false;
                grpCoreInit.Visible = false;
            }

        }

        private void Get_Master()
        {
            while (true)
            {
                //if (DataProcess.LastGetMst == DateTime.MinValue)
                //    DataProcess.LastGetMst = DateTime.Now;
                TimeSpan diff = DateTime.Now - DataProcess.LastGetMst;
                //20170228 기존 10분 에서 1분으로 변경
                if (diff.TotalSeconds > 60 && !DataProcess.Processing)
                {
                    DataProcess.GetMaster();
                    BlackList.GetBlackList();
                }
                Thread.Sleep(1000);
            }
        }

        private void BtnTestCapture1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                OpenFileDialog dlg = new OpenFileDialog();
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    RECT roi = new RECT();
                    roi.x = ENV.CameraEnv.IPCamera1Info.Roi.Left;
                    roi.y = ENV.CameraEnv.IPCamera1Info.Roi.Top;
                    roi.w = ENV.CameraEnv.IPCamera1Info.Roi.Left + ENV.CameraEnv.IPCamera1Info.Roi.Width;
                    roi.h = ENV.CameraEnv.IPCamera1Info.Roi.Top + ENV.CameraEnv.IPCamera1Info.Roi.Height;
                    //dtRegList1.Rows.Add(null, 1, CapCnt + 1, fname, string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h), "", "", LastLoopTime1, false, 0, IpCam1Current.Generalinfo.Exposure);
                    clsThread.RegArray1[0].CapCnt = 0;
                    // [수정] TEST 사진선택도 일반차와 동일한 캡처 파일명 규칙 사용 → 입출차(TCKTTRNS) 저장 사진명을
                    //        일반차와 동일하게({ChName}_{차번}_{yyyyMMddHHmmss}.jpg) 맞춤. 선택 사진을 작업폴더에
                    //        {ChName}{yyyyMMddHHmmssffff}0.jpg 로 복사해 일반 캡처처럼 SourcePath 지정.
                    string _testCap1 = ENV.CameraEnv.IPCamera1Info.ChName + DateTime.Now.ToString("yyyyMMddHHmmssffff") + "0.jpg";
                    try { File.Copy(dlg.FileName, System.IO.Path.Combine(Directory.GetCurrentDirectory(), _testCap1), true); }
                    catch (Exception cpErr) { Util.Logger.Log("TEST 사진 복사 실패: " + cpErr.Message); }
                    clsThread.RegArray1[0].SourcePath = _testCap1;
                    clsThread.RegArray1[0].Roi = string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h);
                    clsThread.RegArray1[0].PlateRoi = null;
                    clsThread.RegArray1[0].PlateNo = null;
                    clsThread.RegArray1[0].FirstCaptureTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    clsThread.RegArray1[0].Send = false;

                    if (File.Exists(dlg.FileName))
                        clsThread.RegArray1[0].Size = new System.IO.FileInfo(dlg.FileName).Length;
                    //RegArray1[CapCnt].term = 0;
                    clsThread.RegArray1[0].Exposure = ENV.CameraEnv.IPCamera1Info.CurrentInfo.Generalinfo.Exposure;
                    Util.Logger.Log(string.Format("****CAM1 {0} reg Start CapCnt {1} ROI {2}", dlg.FileName, 0, clsThread.RegArray1[0].Roi));
                    //NgisWay.Reg1(RegArray1[CapCnt]);
                    //if (CapCnt.Equals(0))
                    //    frm.pictureBox1.Image = new Bitmap(fname);
                    //clsthread.RegPlateNoNgisWay(0, clsthread.RegArray1[CapCnt]);
                    if (!Environment.Is64BitProcess)
                    {
                        if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Ngis)
                            clsThread.RegPlateNoNgisWay(0, 0);
                        else if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Elwox)
                            clsThread.RegPlateNoElwox(0, 0);
                    }
                    else
                    {
#if WIN64
                        //CoreLogic
                        if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic)
                            CoreLogic.Reg(0, 0, ENV.CameraEnv.bRegCarType);
                        //Option(K)
                        if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.OptionK)
                            OptionK.Reg(0, 0, ENV.CameraEnv.bRegCarType);
#endif
                    }
                    Util.Logger.Log("AfterRegPlateCam Loop1");
                    // [수정] new Thread → ThreadPool
                    ThreadPool.QueueUserWorkItem(_ => {
                        try { clsThread.AfterRegPlateCam(0, ENV); }
                        catch (Exception ex) { Util.Logger.Log("AfterRegPlateCam 예외: " + ex.Message); }
                    });
                }
            }
            else if (e.Button == MouseButtons.Middle)
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Multiselect = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (dlg.FileNames.Length != 2) return;
                    RECT roi = new RECT();
                    roi.x = ENV.CameraEnv.IPCamera1Info.Roi.Left;
                    roi.y = ENV.CameraEnv.IPCamera1Info.Roi.Top;
                    roi.w = ENV.CameraEnv.IPCamera1Info.Roi.Left + ENV.CameraEnv.IPCamera1Info.Roi.Width;
                    roi.h = ENV.CameraEnv.IPCamera1Info.Roi.Top + ENV.CameraEnv.IPCamera1Info.Roi.Height;
                    //dtRegList1.Rows.Add(null, 1, CapCnt + 1, fname, string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h), "", "", LastLoopTime1, false, 0, IpCam1Current.Generalinfo.Exposure);
                    clsThread.RegArray1[0].CapCnt = 0;
                    clsThread.RegArray1[0].SourcePath = dlg.FileNames[0];
                    clsThread.RegArray1[0].Roi = string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h);
                    clsThread.RegArray1[0].PlateRoi = null;
                    clsThread.RegArray1[0].PlateNo = null;
                    clsThread.RegArray1[0].FirstCaptureTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    clsThread.RegArray1[0].Send = false;

                    if (File.Exists(dlg.FileNames[0]))
                        clsThread.RegArray1[0].Size = new System.IO.FileInfo(dlg.FileNames[0]).Length;
                    //RegArray1[CapCnt].term = 0;
                    clsThread.RegArray1[0].Exposure = ENV.CameraEnv.IPCamera1Info.CurrentInfo.Generalinfo.Exposure;
                    //NgisWay.Reg1(RegArray1[CapCnt]);
                    //if (CapCnt.Equals(0))
                    //    frm.pictureBox1.Image = new Bitmap(fname);
                    //clsthread.RegPlateNoNgisWay(0, clsthread.RegArray1[CapCnt]);

                    clsThread.RegArray2[0].CapCnt = 0;
                    clsThread.RegArray2[0].SourcePath = dlg.FileNames[1];
                    clsThread.RegArray2[0].Roi = string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h);
                    clsThread.RegArray2[0].PlateRoi = null;
                    clsThread.RegArray2[0].PlateNo = null;
                    clsThread.RegArray2[0].FirstCaptureTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    clsThread.RegArray2[0].Send = false;

                    if (File.Exists(dlg.FileNames[1]))
                        clsThread.RegArray2[0].Size = new System.IO.FileInfo(dlg.FileNames[1]).Length;
                    //RegArray1[CapCnt].term = 0;
                    clsThread.RegArray2[0].Exposure = ENV.CameraEnv.IPCamera2Info.CurrentInfo.Generalinfo.Exposure;
                    Util.Logger.Log(string.Format("****CAM1 {0} reg Start CapCnt {1} ROI {2}", dlg.FileNames[0], 0, clsThread.RegArray1[0].Roi));
                    if (!Environment.Is64BitProcess)
                    {
                        if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Ngis)
                            clsThread.RegPlateNoNgisWay(0, 0);
                        else if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Elwox)
                            clsThread.RegPlateNoElwox(0, 0);
                    }
                    else
                    {
#if WIN64
                        //CoreLogic
                        if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic)
                            CoreLogic.Reg(0, 0, ENV.CameraEnv.bRegCarType);
                        //Option(K)
                        if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.OptionK)
                            OptionK.Reg(0, 0, ENV.CameraEnv.bRegCarType);
#endif
                    }
                    //NgisWay.Reg1(RegArray1[CapCnt]);
                    //if (CapCnt.Equals(0))
                    //    frm.pictureBox1.Image = new Bitmap(fname);
                    //clsthread.RegPlateNoNgisWay(0, clsthread.RegArray1[CapCnt]);
                    Util.Logger.Log(string.Format("****CAM2 {0} reg Start CapCnt {1} ROI {2}", dlg.FileNames[1], 0, clsThread.RegArray2[0].Roi));
                    if (!Environment.Is64BitProcess)
                    {
                        if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Ngis)
                            clsThread.RegPlateNoNgisWay(0, 0);
                        else if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Elwox)
                            clsThread.RegPlateNoElwox(0, 0);
                    }
                    else
                    {
#if WIN64
                        //CoreLogic
                        if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic)
                            CoreLogic.Reg(0, 0, ENV.CameraEnv.bRegCarType);
                        //Option(K)
                        if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.OptionK)
                            OptionK.Reg(0, 0, ENV.CameraEnv.bRegCarType);
#endif
                    }
                    Util.Logger.Log("AfterRegPlateCam Loop1");
                    // [수정] new Thread → ThreadPool
                    ThreadPool.QueueUserWorkItem(_ => {
                        try { clsThread.AfterRegPlateCam(0, ENV); }
                        catch (Exception ex) { Util.Logger.Log("AfterRegPlateCam 예외: " + ex.Message); }
                    });
                }
            }
        }

        private void BtnTestCapture2_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                OpenFileDialog dlg = new OpenFileDialog();
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    RECT roi = new RECT();
                    roi.x = ENV.CameraEnv.IPCamera1Info.Roi.Left;
                    roi.y = ENV.CameraEnv.IPCamera1Info.Roi.Top;
                    roi.w = ENV.CameraEnv.IPCamera1Info.Roi.Left + ENV.CameraEnv.IPCamera1Info.Roi.Width;
                    roi.h = ENV.CameraEnv.IPCamera1Info.Roi.Top + ENV.CameraEnv.IPCamera1Info.Roi.Height;
                    //dtRegList1.Rows.Add(null, 1, CapCnt + 1, fname, string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h), "", "", LastLoopTime1, false, 0, IpCam1Current.Generalinfo.Exposure);
                    clsThread.RegArray2[0].CapCnt = 0;
                    // [수정] TEST 사진선택도 일반차와 동일한 캡처 파일명 규칙 사용 → 입출차(TCKTTRNS) 저장 사진명을
                    //        일반차와 동일하게({ChName}_{차번}_{yyyyMMddHHmmss}.jpg) 맞춤. cam2는 AfterRegPlateCam(1)
                    //        에서 IPCamera2Info.ChName 사용하므로 그 ChName으로 작업폴더에 복사.
                    string _testCap2 = ENV.CameraEnv.IPCamera2Info.ChName + DateTime.Now.ToString("yyyyMMddHHmmssffff") + "0.jpg";
                    try { File.Copy(dlg.FileName, System.IO.Path.Combine(Directory.GetCurrentDirectory(), _testCap2), true); }
                    catch (Exception cpErr) { Util.Logger.Log("TEST 사진 복사 실패: " + cpErr.Message); }
                    clsThread.RegArray2[0].SourcePath = _testCap2;
                    clsThread.RegArray2[0].Roi = string.Format("{0},{1},{2},{3}", roi.x, roi.y, roi.w, roi.h);
                    clsThread.RegArray2[0].PlateRoi = null;
                    clsThread.RegArray2[0].PlateNo = null;
                    clsThread.RegArray2[0].FirstCaptureTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    clsThread.RegArray2[0].Send = false;

                    if (File.Exists(dlg.FileName))
                        clsThread.RegArray2[0].Size = new System.IO.FileInfo(dlg.FileName).Length;
                    //RegArray1[CapCnt].term = 0;
                    clsThread.RegArray2[0].Exposure = ENV.CameraEnv.IPCamera1Info.CurrentInfo.Generalinfo.Exposure;
                    Util.Logger.Log(string.Format("****CAM2 {0} reg Start CapCnt {1} ROI {2}", dlg.FileName, 0, clsThread.RegArray1[0].Roi));
                    //NgisWay.Reg1(RegArray1[CapCnt]);
                    //if (CapCnt.Equals(0))
                    //    frm.pictureBox1.Image = new Bitmap(fname);
                    //clsthread.RegPlateNoNgisWay(0, clsthread.RegArray1[CapCnt]);
                    if (!Environment.Is64BitProcess)
                    {
                        if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Ngis)
                            clsThread.RegPlateNoNgisWay(1, 0);
                        else if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.Elwox)
                            clsThread.RegPlateNoElwox(1, 0);
                    }
                    else
                    {
#if WIN64
                        //CoreLogic
                        if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.CoreLogic)
                            CoreLogic.Reg(1, 0, ENV.CameraEnv.bRegCarType);
                        //Option(K)
                        if (ENV.CameraEnv.RegModule == (int)ClsStructure.RegModule.OptionK)
                            OptionK.Reg(1, 0, ENV.CameraEnv.bRegCarType);
#endif
                    }
                    Util.Logger.Log("AfterRegPlateCam Loop2");
                    // [수정] new Thread → ThreadPool
                    ThreadPool.QueueUserWorkItem(_ => {
                        try { clsThread.AfterRegPlateCam(1, ENV); }
                        catch (Exception ex) { Util.Logger.Log("AfterRegPlateCam 예외: " + ex.Message); }
                    });
                }
            }
        }

        private void Timer_Full_Check_Tick(object sender, EventArgs e)
        {
            if (FullSpaceControl.Use)
                FullCheck();
            else
                timer_Full_Check.Enabled = false;
        }


        private void ComPlateRegResult(int camidx, string PlateNo, string imgFile)
        {
            ClsStructure.IPCamera_Info caminfo = new ClsStructure.IPCamera_Info();
            string Chname = string.Empty;
            Util.Logger.Log(camidx.ToString());
            switch (camidx)
            {
                case 1:
                    caminfo = IpCam1Current;
                    Chname = ENV.CameraEnv.IPCamera1Info.ChName;
                    break;
                case 2:
                    caminfo = IpCam2Current;
                    Chname = ENV.CameraEnv.IPCamera2Info.ChName;
                    break;
                default:
                    return;
            }
            string DatePath = string.Empty;
            if (ENV.CameraEnv.SockDataFormat.Equals((int)ClsStructure.SockFormat.Nexpa))
                DatePath = string.Format(@"{0:D4}\{1:D2}\{2:D2}", DateTime.Now.Year.ToString(), DateTime.Now.Month.ToString(), DateTime.Now.Day.ToString());
            else
                DatePath = DateTime.Now.ToString("yyyyMMdd");
            
            if (!Directory.Exists(string.Format("{0}\\{1}", ENV.CameraEnv.ImageSave.SavePath, DatePath)))
                Directory.CreateDirectory(string.Format("{0}\\{1}", ENV.CameraEnv.ImageSave.SavePath, DatePath));

            try
            {

                if (camidx.Equals(1))
                {
                    frm.pictureBox1.ImageLocation = string.Format("{0}\\{1}\\{2}.jpg", ENV.CameraEnv.ImageSave.SavePath, DatePath, imgFile);
                    ListItemAdd(DataProcess.DataProcess(ENV.CommunicationEnv.Lpr1Info.InOutType, ENV, camidx - 1, PlateNo.ToString(), imgFile));
                    SetLabelText(lblCam1RegResult, "인식결과: " + PlateNo);
                    Properties.Settings.Default.Ch1File = string.Format("{0}\\{1}\\{2}.jpg", ENV.CameraEnv.ImageSave.SavePath, DatePath, imgFile);
                    Properties.Settings.Default.Save();
                    if (ENV.CameraEnv.IPCamera1Info.DioInPut.SmallCar)
                        lastPlate = PlateNo;
                }
                else
                {
                    frm.pictureBox2.ImageLocation = string.Format("{0}\\{1}\\{2}.jpg", ENV.CameraEnv.ImageSave.SavePath, DatePath, imgFile);
                    ListItemAdd(DataProcess.DataProcess(ENV.CommunicationEnv.Lpr2Info.InOutType, ENV, camidx - 1, PlateNo.ToString(), imgFile));
                    SetLabelText(lblCam2RegResult, "인식결과: " + PlateNo);
                    Properties.Settings.Default.Ch2File = string.Format("{0}\\{1}\\{2}.jpg", ENV.CameraEnv.ImageSave.SavePath, DatePath, imgFile);
                    Properties.Settings.Default.Save();
                    if (ENV.CameraEnv.IPCamera2Info.DioInPut.SmallCar)
                        lastPlate = PlateNo;
                }
                SendClient(Chname, PlateNo, DatePath, string.Format("{0}\\{1}.jpg", DatePath, imgFile));
            }
            catch (Exception)
            { }
        }

        private void regdelete()
        {
            DateTime deltime = new DateTime();
            try
            {
                string[] sp = ENV.RegCarControl.Regautodeltime.Split(':');
                while (true)
                {
                    try
                    {
                        if (DateTime.Now.Hour == Util.Function.IntTryParse(sp[0]) &&
                            DateTime.Now.Minute == Util.Function.IntTryParse(sp[1]))
                        {
                            if ((DateTime.Now - deltime).TotalSeconds > 60)
                            {
                                string query = "delete from custdef where dtValidEndDate < convert(nvarchar(10), getdate(), 121)";
                                Util.clsMssql.ExecQuery(DataProcess.Get_MCon(), query);
                                deltime = DateTime.Now;
                            }
                        }

                    }
                    catch { }
                    Thread.Sleep(10000);
                }
            }
            catch { }
        }
    }
}
