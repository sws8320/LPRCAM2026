using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Data;

namespace KyungsinLPR
{
    public class ClsStructure
    {
        public struct EnvStruct
        {
            //public int OnceServer;
            //public bool UseCalServer;
            //public int CalServer;
            //public Destination_info[] DestinationClient;
            //public bool SendInfoCal;
            public CamEnv CameraEnv;
            public PublicEnv CommonEnv;
            public ComEnv CommunicationEnv;
            public int StartType;
            public bool TestMode;
            public bool SendOffice;
            //public bool PeriodExitSend;
            //public bool PeriodEntSend;
            public uint LoopTerm;
            //public RegCarControl RegControl;
            public KyungsinLPR.RegCarControl RegCarControl;
            //leess 긴급차량 개방
            public bool EmergencyCar;
        }

        public struct PublicEnv
        {
            public DB_Info DBInfo;
            public bool Authentication;
            public DIo_Info Dio;
        }

        public struct ComEnv
        {
            public Park_Info ParkInfo;
            public int RegCorrection;
            public SaveImage ImageSave;
            public bool Nodetection_Open;
            public Lpr_Info Lpr1Info;
            public Lpr_Info Lpr2Info;
            public Sock_Info[] ClientTarget;
            public DisPlay_Info[] DisPlay;
            public DisPlay_Ment FixedMent;
            public DisPlay_Ment PeriodMent;
            public int FixedPort;
            public Sock_Info Listen;
            public Return_Car ReturnCar;
        }

        public struct CamEnv
        {
            public int iNovaType;//leess 카메라종류 iNova1(1), iNova2(2)
            public IPCamera_Basic_Setting IPCamera1Info;
            public IPCamera_Basic_Setting IPCamera2Info;
            public SaveImage ImageSave;
            //public Dev_Info DioInfo;
            public bool PlateArea;
            public int RegModule;
            public int SockDataFormat;
            public int CoreType;
            public int CoreCountry;
            public bool bRegCarType;
            public List<SmallCarRate> RegCarRate;
            public int RecogMode; // 0=스트로브(SSEngine), 1=동영상(FAVEngine)
        }

        public struct IPCamera_Basic_Setting
        {
            public bool Use;
            public string IP;
            public string ChName;
            public bool StreamUdp;
            public IPCamera_Info CurrentInfo;
            //public SaveImage ImageSave;
            public int User_Setting_Resend_Interval;
            public User_Setting[] User_Setting;
            public Bracket_Detail[,] User_Brakect;
            public ALC_Control[] User_Alc;
            public Dio_Input DioInPut;
            public Rectangle Roi;
            public int TriggerCnt;
            public int BarkectCnt;
            public double FrameRate;
            public int TriggerMode;
            public bool SendStxEtx;
            public string RtspUrl; // FAVEngine 동영상 방식용 RTSP URL
            // 카메라별 소스 — 0 = iNova(전역 iNovaType 사용), 1 = iNova1, 2 = iNova2, 3 = USB(DirectShow)
            // 혼합 시나리오: 카메라1과 카메라2 각각 다른 소스 선택 가능
            public int CameraSource;
            public string UsbMoniker; // DirectShow MonikerString (예: @device:pnp:\\?\usb#vid_xxxx...)
            public string UsbDeviceName; // 표시용 이름 (예: "OBSBOT Tiny 4K")
            public int UsbResolutionWidth;
            public int UsbResolutionHeight;
        }

        // 카메라 소스 종류 — IPCamera_Basic_Setting.CameraSource 값
        public enum CameraSourceType
        {
            Default = 0,   // 전역 iNovaType 적용 (하위호환)
            iNova1 = 1,
            iNova2 = 2,
            USB = 3
        }

        public struct IPCamera_Info
        {
            public General_Control Generalinfo;
            public Trigger_Control TriggerInfo;
            public Barcket_Control BracketInfo;
        }

        public struct General_Control
        {
            public string Fw;
            public string Sn;
            public int TotalGain;
            public double GainDecibel;
            public int Exposure;
            public double FrameRate;
            public ALC_Control AlcInfo;
        }

        public struct ALC_Control
        {
            public int target;
            public AEC_Coltrol AECInfo;
            public AGC_Coltrol AGCInfo;
        }

        public struct AEC_Coltrol
        {
            public bool enableAEC;
            public int minExposure;
            public int maxExposure;
        }

        public struct AGC_Coltrol
        {
            public bool enableAGC;
            public double minGain;
            public double maxGain;
        }

        public struct Trigger_Control
        {
            public int TriggerMode;
            public int CountPerTrigger;
        }

        public struct Barcket_Control
        {
            public bool Use;
            public int Count;
            public Bracket_Detail[] BraketInfo;
        }

        public struct Bracket_Detail
        {
            public bool Use;
            public int Exposure;
            public int AnalogGain;
            public int DigitalGain;
        }

        public struct User_Setting
        {
            public bool use;
            public string StartTime;
            public string EndTime;
            public int Exposuer;
            public int ModeIdx;
            public bool UseBarkect;
            public bool UseALC;
        }

        public struct SaveImage
        {
            public bool Use;
            public string SavePath;
            public int SaveTerm;
            public bool EtcSave;
            public string EtcPath;
        }

        //image crop
        public struct ImgSize
        {
            public int CropX;
            public int CropY;
            public int CropWidth;
            public int CropHeight;
        }

        public struct DB_Info
        {
            public string Ip;
            public string Id;
            public string Pw;
            public string MstDB;
            public string TrnsDb;
        }

        public struct Destination_info
        {
            public string chName;
            public string IP;
            public int Port;
        }

        public struct Exposure
        {
            public int BasicExposure;
            public int BasicInterval;
            //시간대별 밝기 전송 사용 여부 시간대 밝기 재전송 시간 설정 변수
            public bool usetime1;
            public string starttime1;
            public string endtime1;
            public int timeExposure1;
            public int resendtime1;
            public bool usetime2;
            public string starttime2;
            public string endtime2;
            public int timeExposure2;
            public int resendtime2;
            public bool usetime3;
            public string starttime3;
            public string endtime3;
            public int timeExposure3;
            public int resendtime3;
            public int LineInverter; //전압 상승(1), 하강(0) 동작방식 선택
        }

        public struct AlcTarget
        {
            public int Target;
            public int Target1;
            public int Target2;
            public int Target3;
        }

        public enum DeviceList { KJC1000, REALSYS, DINGTIAN };
        public enum DeviceType { 이벤트, 리얼 };
        public enum ImageProceType { 이미지자르기, 번호판확인 };
        public enum Bracket { 모드1, 모드2, 모드3 };

        public struct RegList
        {
            public int id;
            public Bitmap bitmap;
            public bool Job;
            public string result;
            public string FileName;
            public DateTime CapTime;
            public int term;
        }

        public enum RegModule
        {
            Elwox, Ngis, CoreLogic, OptionK   // OptionK=3 (자체 CRNN/ONNX). 기존 값 보존 위해 끝에 추가.
        };

        public enum CoreType
        {
            CPU, GPU, MyriadVPU
        };

        public enum SockFormat
        {
            Kukje, Amano, Nexpa, AmanoOld
        };

        public struct RegStruct
        {
            public int CapCnt;
            public string SourcePath;
            public string Roi;
            public string PlateRoi;
            public string PlateNo;
            public string FirstCaptureTime;
            public bool Send;
            public long term;
            public long Exposure;
            public int FrmaeCount;
            public long Size;
            public string CarType;
            public float Confidence;

            //leess 이미지사이즈 추가 : 설정좌표보다 이미지가 작을 경우 아예 인식동작이 안하는것 방지
            //public int imgWidth, imgHeight;
        }

        public struct Park_Info
        {
            public int No;
            public int Ext_No;
            public int Client_No;
        }

        public enum reg_correction { nothing, digit4, digit6 };

        public enum ProgramStartType { CAM, COM, BOTH };

        public struct Lpr_Info
        {
            public bool Use;
            public int EqpmNo;
            public string ChNo;
            public string Name;
            public int DevType;
            public int InOutType;
            public bool FreePass;   //무발권
            public bool FreePassGateOpen;
            public Sock_Info SockInfo;
            public string ImagePath;
            public ProcessOption LprOpt;
        }

        public struct Sock_Info
        {
            public bool Use;
            public string IP;
            public int Port;
            public int Type;
        }

        public enum LprDevice { KukjeLpr, AmanoLPR };

        public enum InoutType { 입구용, 출구용 };

        public enum FreePassType { Nothing, Open };

        public enum CalculatorType { 국제요금계산기, 아마노요금계산기 };

        public struct DIo_Info
        {
            public Dev_Setting DioSetting;
            public Dio_OutPut[] DioOutPut;
            public Add_Dio_InOut IsolatePort;
        }

        public struct Add_Dio_InOut
        {
            public Dio_Input In;
            public Dio_OutPut Out;
        }

        public struct Dev_Setting
        {
            public string SerialPort;
            public string Setting;
            public string Dev_Type_Name;
            public bool Type;
            public string IpAddress;   // DINGTIAN 등 이더넷 릴레이 보드 IP
            public int NetPort;        // DINGTIAN TCP/UDP 포트 (기본 60001)
        }

        public struct Dio_OutPut
        {
            public bool Use;
            public int Port;
            public int Delay;
            public int Keep;
            public int AddPort;
            public int AddDelay;
            public int AddKeep;
        }

        public struct Dio_Input
        {
            public int LoopPort;
            public bool SmallCar;
            public int SmallPort;
        }

        public struct DisPlay_Info
        {
            public bool Use;
            public Dev_Setting Com;
            public DisPlay_Ment Ment;
            public string NormalCar;
            public string Normal1Color;
            public string Normal2Color;
            public string PeriodCar;
            public string Period1Color;
            public string Period2Color;
            public bool UseFiex;
            public Sock_Info Net;
        }

        public struct DisPlay_Ment
        {
            public string Ment1Line;
            public string Ment1Color;
            public string Ment2Line;
            public string Ment2Color;
        }

        public enum DisPlayType
        {
            Color3, Color8, AmanoSmall
        };

        public enum Color8
        {
            검정 = 0, 적색 = 1, 녹색 = 2, 노랑 = 3, 파랑 = 4, 분홍 = 5, 청록 = 6, 백색 = 7
        }

        public enum Color3
        {
            검정 = 0, 적색 = 1, 노랑 = 2, 주황 = 3
        }

        public enum AmanoColor3
        {
            적색 = 0, 녹색 = 1, 주황 = 2
        }

        public struct ProcessOption
        {
            public bool Normal_SendData;
            public bool Normal_Lprtrns;
            public bool Normal_Tckttrns;
            public bool Normal_Counter;
            public bool Normal_Gate;
            public bool Period_SendData;
            public bool Period_Lprtrns;
            public bool Period_Passtrns;
            public bool Period_Counter;
            public bool Period_Gate;
        }

        public struct Return_Car
        {
            public bool Use;
            public int Term;
            public string Ment;
        }

        public struct RegCarControl
        {
            public Control_DupEnt controlent;
        }

        public struct Control_DupEnt
        {
            public bool Use;
            public string Ment;
        }

        public struct SmallCarRate
        {
            public string CarType;
            public int Rate;
        }
    }
}
