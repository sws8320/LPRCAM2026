using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.IO;

namespace KyungsinLPR
{
    public class clsFunction
    {
        public ClsStructure.EnvStruct GetEnv(ClsStructure.EnvStruct env)
        {
            ClsStructure.EnvStruct EnvInfo = new ClsStructure.EnvStruct();
            Util.Function.IniFileName = string.Format("{0}\\CameraSetting.ini", Util.Global.ROOT);

            EnvInfo.LoopTerm = Util.Function.uIntTryParse(ReadIni("LOOP", "TERM"));
            if (EnvInfo.LoopTerm < 100)
            {
                EnvInfo.LoopTerm = 2000;
                WriteIni("LOOP", "예시I", "단위 (ms) 1000 = 1초 // 기본값 2000ms");
                WriteIni("LOOP", "예시II", "설정 값이 100미만 이면 기본값 2000으로 설정");
                WriteIni("LOOP", "TERM", EnvInfo.LoopTerm);
            }

            #region 기본설정
            #region TestMode
            EnvInfo.TestMode = Util.Function.BoolTryParse(ReadIni("Public", "Test"));
            #endregion
            #region 데이터 베이스
            EnvInfo.CommonEnv.DBInfo.Ip = ReadIni("COMMON", "dbip");
            EnvInfo.CommonEnv.DBInfo.Id = ReadIni("COMMON", "dbid");
            EnvInfo.CommonEnv.DBInfo.Pw = ReadIni("COMMON", "dbpw");
            EnvInfo.CommonEnv.DBInfo.MstDB = ReadIni("COMMON", "masterdb");
            EnvInfo.CommonEnv.DBInfo.TrnsDb = ReadIni("COMMON", "trnsdb");
            #endregion

            #region 주차장 설정
            EnvInfo.CommunicationEnv.ParkInfo.No = Util.Function.IntTryParse(ReadIni("COMMON", "parkno"));
            EnvInfo.CommunicationEnv.ParkInfo.Ext_No = Util.Function.IntTryParse(ReadIni("PARK", "extno"));
            EnvInfo.CommunicationEnv.ParkInfo.Client_No = Util.Function.IntTryParse(ReadIni("COMMON", "clientno"));
            #endregion

            #region 인식 보정
            EnvInfo.CommunicationEnv.RegCorrection = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "correction"));
            #endregion

            #region 통신 프로그램 이미지 저장
            EnvInfo.CommunicationEnv.ImageSave.Use = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "imagesaveuse"));
            EnvInfo.CommunicationEnv.ImageSave.SavePath = ReadIni("COMMUNICATION", "imagesavepath");
            #endregion

            #region 프로그램 시작 타입
            EnvInfo.StartType = Util.Function.IntTryParse(ReadIni("COMMON", "starttype"));
            #endregion

            #region 마스터 취득 II
            GetMasterInfo.Use = Util.Function.BoolTryParse(ReadIni("GetMaster", "Use"));
            GetMasterInfo.SharePath = ReadIni("GetMaster", "SharePath");
            GetMasterInfo.Term = Util.Function.IntTryParse(ReadIni("GetMaster", "Term"));
            #endregion
            #endregion

            #region 카메라 설정
            //leess iNova 카메라종류 설정
            EnvInfo.CameraEnv.iNovaType = Util.Function.IntTryParse(ReadIni("CAMERA", "iNovaType"));
            if(EnvInfo.CameraEnv.iNovaType == -1) EnvInfo.CameraEnv.iNovaType = 1;//디폴트 1
            #region Camera1
            EnvInfo.CameraEnv.IPCamera1Info.Use = Util.Function.BoolTryParse(ReadIni("CAMERA", "cam1useflag"));
            EnvInfo.CameraEnv.IPCamera1Info.IP = ReadIni("CAMERA", "cam1ip");
            EnvInfo.CameraEnv.IPCamera1Info.RtspUrl = ReadIni("CAMERA", "cam1rtspurl");
            EnvInfo.CameraEnv.IPCamera1Info.ChName = ReadIni("CAMERA", "cam1chname");
            EnvInfo.CameraEnv.IPCamera1Info.StreamUdp = Util.Function.BoolTryParse(ReadIni("CAMERA", "cam1udp"));
            //EnvInfo.CameraEnv.IPCamera1Info.ImageSave.EtcSave = Util.Function.BoolTryParse(ReadIni("IPCAM1", "etcsave"));
            //EnvInfo.CameraEnv.IPCamera1Info.ImageSave.EtcPath = ReadIni("IPCAM1", "etcpath");
            EnvInfo.CameraEnv.IPCamera1Info.User_Setting_Resend_Interval = Util.Function.IntTryParse(ReadIni("CAMERA", "cam1interval"));
            EnvInfo.CameraEnv.IPCamera1Info.User_Setting = new ClsStructure.User_Setting[3];
            for (int i = 0; i < 3; i++)
            {
                EnvInfo.CameraEnv.IPCamera1Info.User_Setting[i].use = Util.Function.BoolTryParse(ReadIni("CAMERA", string.Format("cam1time{0}1useflag", i)));
                EnvInfo.CameraEnv.IPCamera1Info.User_Setting[i].StartTime = ReadIni("CAMERA", string.Format("cam1time{0}starttime", i));
                EnvInfo.CameraEnv.IPCamera1Info.User_Setting[i].EndTime = ReadIni("CAMERA", string.Format("cam1time{0}endtime", i));
                EnvInfo.CameraEnv.IPCamera1Info.User_Setting[i].Exposuer = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam1time{0}exposure", i)));
                EnvInfo.CameraEnv.IPCamera1Info.User_Setting[i].ModeIdx = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam1time{0}bracket", i)));
                EnvInfo.CameraEnv.IPCamera1Info.User_Setting[i].UseBarkect = Util.Function.BoolTryParse(ReadIni("CAMERA", string.Format("cam1time{0}usebracket", i)));
                EnvInfo.CameraEnv.IPCamera1Info.User_Setting[i].UseALC = Util.Function.BoolTryParse(ReadIni("CAMERA", string.Format("cam1time{0}usealc", i)));
            }
            EnvInfo.CameraEnv.IPCamera1Info.TriggerCnt = Util.Function.IntTryParse(ReadIni("CAMERA", "cam1triggercnt"));
            if (EnvInfo.CameraEnv.IPCamera1Info.TriggerCnt == 0) EnvInfo.CameraEnv.IPCamera1Info.TriggerCnt = 2;

            EnvInfo.CameraEnv.IPCamera1Info.User_Brakect = new ClsStructure.Bracket_Detail[3, 4];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    EnvInfo.CameraEnv.IPCamera1Info.User_Brakect[i, j].Exposure = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam1time{0}{1}exposure", i + 1, j + 1)));
                    if (EnvInfo.CameraEnv.IPCamera1Info.User_Brakect[i, j].Exposure.Equals(0)) EnvInfo.CameraEnv.IPCamera1Info.User_Brakect[i, j].Exposure = 33000;
                    EnvInfo.CameraEnv.IPCamera1Info.User_Brakect[i, j].AnalogGain = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam1time{0}{1}analoggain", i + 1, j + 1)));
                    EnvInfo.CameraEnv.IPCamera1Info.User_Brakect[i, j].DigitalGain = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam1time{0}{1}digitalgain", i + 1, j + 1)));
                }
            }
            EnvInfo.CameraEnv.IPCamera1Info.BarkectCnt = Util.Function.IntTryParse(ReadIni("CAMERA", "cam1brakect"));
            if (EnvInfo.CameraEnv.IPCamera1Info.BarkectCnt == 0) EnvInfo.CameraEnv.IPCamera1Info.BarkectCnt = 2;

            env.CameraEnv.IPCamera1Info.FrameRate = Util.Function.DoubleTryParse(ReadIni("CAMERA", "cam1framerate"));
            if (EnvInfo.CameraEnv.IPCamera1Info.FrameRate == 0) EnvInfo.CameraEnv.IPCamera1Info.FrameRate = 15;

            env.CameraEnv.IPCamera1Info.TriggerMode = Util.Function.IntTryParse(ReadIni("CAMERA", "cam1TriggerMode"));
            if (EnvInfo.CameraEnv.IPCamera1Info.TriggerMode == 0) EnvInfo.CameraEnv.IPCamera1Info.TriggerMode = 0;

            EnvInfo.CameraEnv.IPCamera1Info.User_Alc = new ClsStructure.ALC_Control[3];
            for (int i = 0; i < 3; i++)
            {
                EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].target = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam1time{0}alctarget", i + 1)));
                if (EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].target.Equals(0)) EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].target = 1;
                EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].AECInfo.enableAEC = Util.Function.BoolTryParse(ReadIni("CAMERA", string.Format("cam1time{0}aecuse", i + 1)));
                EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].AECInfo.minExposure = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam1time{0}aecmin", i + 1)));
                if (EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].AECInfo.minExposure.Equals(0)) EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].AECInfo.minExposure = 23;
                EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].AECInfo.maxExposure = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam1time{0}aecmax", i + 1)));
                if (EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].AECInfo.maxExposure.Equals(0)) EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].AECInfo.maxExposure = 33000;
                EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].AGCInfo.enableAGC = Util.Function.BoolTryParse(ReadIni("CAMERA", string.Format("cam1time{0}agcuse", i + 1)));
                EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].AGCInfo.minGain = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam1time{0}agcmin", i + 1)));
                if (EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].AGCInfo.minGain.Equals(0)) EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].AGCInfo.minGain = 1;
                EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].AGCInfo.maxGain = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam1time{0}agcmax", i + 1)));
                if (EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].AGCInfo.maxGain.Equals(0)) EnvInfo.CameraEnv.IPCamera1Info.User_Alc[i].AGCInfo.maxGain = 4;
            }

            EnvInfo.CameraEnv.IPCamera1Info.DioInPut.LoopPort = Util.Function.IntTryParse(ReadIni("CAMERA", "cam1dioport"));
            EnvInfo.CameraEnv.IPCamera1Info.DioInPut.SmallCar = Util.Function.BoolTryParse(ReadIni("CAMERA", "cam1samllcaruse"));
            EnvInfo.CameraEnv.IPCamera1Info.DioInPut.SmallPort = Util.Function.IntTryParse(ReadIni("CAMERA", "cam1smallcarport"));

            String[] Roi1 = ReadIni("CAMERA", "cam1roi").Trim().Split(',');
            if (Roi1.Length == 4)
                EnvInfo.CameraEnv.IPCamera1Info.Roi = new Rectangle(Convert.ToInt32(Roi1[0].Trim()), Convert.ToInt32(Roi1[1].Trim()), Convert.ToInt32(Roi1[2].Trim()), Convert.ToInt32(Roi1[3].Trim()));
            EnvInfo.CameraEnv.IPCamera1Info.SendStxEtx = Util.Function.BoolTryParse(ReadIni("CAMERA", "cam1SendSTXETX"));
            #endregion

            #region Camera2
            EnvInfo.CameraEnv.IPCamera2Info.Use = Util.Function.BoolTryParse(ReadIni("CAMERA", "cam2useflag"));
            EnvInfo.CameraEnv.IPCamera2Info.IP = ReadIni("CAMERA", "cam2ip");
            EnvInfo.CameraEnv.IPCamera2Info.RtspUrl = ReadIni("CAMERA", "cam2rtspurl");
            EnvInfo.CameraEnv.IPCamera2Info.ChName = ReadIni("CAMERA", "cam2chname");
            EnvInfo.CameraEnv.IPCamera2Info.StreamUdp = Util.Function.BoolTryParse(ReadIni("CAMERA", "cam2udp"));
            //EnvInfo.CameraEnv.IPCamera2Info.ImageSave.EtcSave = Util.Function.BoolTryParse(ReadIni("IPCAM2", "etcsave"));
            //EnvInfo.CameraEnv.IPCamera2Info.ImageSave.EtcPath = ReadIni("IPCAM2", "etcpath");
            EnvInfo.CameraEnv.IPCamera2Info.User_Setting_Resend_Interval = Util.Function.IntTryParse(ReadIni("CAMERA", "cam2interval"));
            EnvInfo.CameraEnv.IPCamera2Info.User_Setting = new ClsStructure.User_Setting[3];
            for (int i = 0; i < 3; i++)
            {
                EnvInfo.CameraEnv.IPCamera2Info.User_Setting[i].use = Util.Function.BoolTryParse(ReadIni("CAMERA", string.Format("cam2time{0}1useflag", i)));
                EnvInfo.CameraEnv.IPCamera2Info.User_Setting[i].StartTime = ReadIni("CAMERA", string.Format("cam2time{0}starttime", i));
                EnvInfo.CameraEnv.IPCamera2Info.User_Setting[i].EndTime = ReadIni("CAMERA", string.Format("cam2time{0}endtime", i));
                EnvInfo.CameraEnv.IPCamera2Info.User_Setting[i].Exposuer = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam2time{0}exposure", i)));
                EnvInfo.CameraEnv.IPCamera2Info.User_Setting[i].ModeIdx = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam2time{0}bracket", i)));
                EnvInfo.CameraEnv.IPCamera2Info.User_Setting[i].UseBarkect = Util.Function.BoolTryParse(ReadIni("CAMERA", string.Format("cam2time{0}usebracket", i)));
                EnvInfo.CameraEnv.IPCamera2Info.User_Setting[i].UseALC = Util.Function.BoolTryParse(ReadIni("CAMERA", string.Format("cam2time{0}usealc", i)));
            }
            EnvInfo.CameraEnv.IPCamera2Info.TriggerCnt = Util.Function.IntTryParse(ReadIni("CAMERA", "cam2triggercnt"));
            if (EnvInfo.CameraEnv.IPCamera2Info.TriggerCnt == 0) EnvInfo.CameraEnv.IPCamera2Info.TriggerCnt = 2;
            EnvInfo.CameraEnv.IPCamera2Info.User_Brakect = new ClsStructure.Bracket_Detail[3, 4];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    EnvInfo.CameraEnv.IPCamera2Info.User_Brakect[i, j].Exposure = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam2time{0}{1}exposure", i + 1, j + 1)));
                    if (EnvInfo.CameraEnv.IPCamera2Info.User_Brakect[i, j].Exposure.Equals(0)) EnvInfo.CameraEnv.IPCamera2Info.User_Brakect[i, j].Exposure = 33000;
                    EnvInfo.CameraEnv.IPCamera2Info.User_Brakect[i, j].AnalogGain = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam2time{0}{1}analoggain", i + 1, j + 1)));
                    EnvInfo.CameraEnv.IPCamera2Info.User_Brakect[i, j].DigitalGain = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam2time{0}{1}digitalgain", i + 1, j + 1)));
                }
            }
            EnvInfo.CameraEnv.IPCamera2Info.BarkectCnt = Util.Function.IntTryParse(ReadIni("CAMERA", "cam2brakect"));
            if (EnvInfo.CameraEnv.IPCamera2Info.BarkectCnt == 0) EnvInfo.CameraEnv.IPCamera2Info.BarkectCnt = 2;

            env.CameraEnv.IPCamera2Info.FrameRate = Util.Function.DoubleTryParse(ReadIni("CAMERA", "cam2framerate"));
            if (EnvInfo.CameraEnv.IPCamera2Info.FrameRate == 0) EnvInfo.CameraEnv.IPCamera2Info.FrameRate = 15;

            env.CameraEnv.IPCamera2Info.TriggerMode = Util.Function.IntTryParse(ReadIni("CAMERA", "cam2TriggerMode"));
            if (EnvInfo.CameraEnv.IPCamera2Info.TriggerMode == 0) EnvInfo.CameraEnv.IPCamera2Info.TriggerMode = 0;

            EnvInfo.CameraEnv.IPCamera2Info.User_Alc = new ClsStructure.ALC_Control[3];
            for (int i = 0; i < 3; i++)
            {
                EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].target = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam2time{0}alctarget", i + 1)));
                if (EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].target.Equals(0)) EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].target = 1;
                EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].AECInfo.enableAEC = Util.Function.BoolTryParse(ReadIni("CAMERA", string.Format("cam2time{0}aecuse", i + 1)));
                EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].AECInfo.minExposure = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam2time{0}aecmin", i + 1)));
                if (EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].AECInfo.minExposure.Equals(0)) EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].AECInfo.minExposure = 23;
                EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].AECInfo.maxExposure = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam2time{0}aecmax", i + 1)));
                if (EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].AECInfo.maxExposure.Equals(0)) EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].AECInfo.maxExposure = 33000;
                EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].AGCInfo.enableAGC = Util.Function.BoolTryParse(ReadIni("CAMERA", string.Format("cam2time{0}agcuse", i + 1)));
                EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].AGCInfo.minGain = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam2time{0}agcmin", i + 1)));
                if (EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].AGCInfo.minGain.Equals(0)) EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].AGCInfo.minGain = 1;
                EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].AGCInfo.maxGain = Util.Function.IntTryParse(ReadIni("CAMERA", string.Format("cam2time{0}agcmax", i + 1)));
                if (EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].AGCInfo.maxGain.Equals(0)) EnvInfo.CameraEnv.IPCamera2Info.User_Alc[i].AGCInfo.maxGain = 4;
            }
            EnvInfo.CameraEnv.IPCamera2Info.DioInPut.LoopPort = Util.Function.IntTryParse(ReadIni("CAMERA", "cam2dioport"));
            EnvInfo.CameraEnv.IPCamera2Info.DioInPut.SmallCar = Util.Function.BoolTryParse(ReadIni("CAMERA", "cam2samllcaruse"));
            EnvInfo.CameraEnv.IPCamera2Info.DioInPut.SmallPort = Util.Function.IntTryParse(ReadIni("CAMERA", "cam2smallcarport"));

            String[] Roi2 = ReadIni("CAMERA", "cam2roi").Trim().Split(',');
            if (Roi2.Length == 4)
                EnvInfo.CameraEnv.IPCamera2Info.Roi = new Rectangle(Convert.ToInt32(Roi2[0].Trim()), Convert.ToInt32(Roi2[1].Trim()), Convert.ToInt32(Roi2[2].Trim()), Convert.ToInt32(Roi2[3].Trim()));
            EnvInfo.CameraEnv.IPCamera2Info.SendStxEtx = Util.Function.BoolTryParse(ReadIni("CAMERA", "cam2SendSTXETX"));
            #endregion
            #endregion

            #region LPR설정
            #region 카메라 이미지 저장 설정
            //이미지 저장 경로
            EnvInfo.CameraEnv.ImageSave.SavePath = ReadIni("CAMERA", "imgsavepath");
            EnvInfo.CameraEnv.ImageSave.SaveTerm = Util.Function.IntTryParse(ReadIni("CAMERA", "imgdeleteterm"));
            EnvInfo.CameraEnv.ImageSave.EtcPath = ReadIni("CAMERA", "etcpath");
            EnvInfo.CameraEnv.ImageSave.EtcSave = Util.Function.BoolTryParse(ReadIni("CAMERA", "etcpathuse"));
            #endregion

            #region 인식 모듈
            int.TryParse(Util.Function.IniReadValue("CAMERA", "regmodule"), out EnvInfo.CameraEnv.RegModule);
            int.TryParse(Util.Function.IniReadValue("CAMERA", "regtype"), out EnvInfo.CameraEnv.CoreType);
            int.TryParse(Util.Function.IniReadValue("CAMERA", "regcountrytype"), out EnvInfo.CameraEnv.CoreCountry);
            int.TryParse(Util.Function.IniReadValue("CAMERA", "recogmode"), out EnvInfo.CameraEnv.RecogMode);
            if (EnvInfo.CameraEnv.CoreCountry <= 0)
                EnvInfo.CameraEnv.CoreCountry = CoreLogic.KOR;
            CoreLogic.cc = EnvInfo.CameraEnv.CoreCountry;
            EnvInfo.CameraEnv.PlateArea = Util.Function.BoolTryParse(ReadIni("CAMERA", "plateregtype"));
            bool.TryParse(Util.Function.IniReadValue("CAMERA", "regcartype"), out EnvInfo.CameraEnv.bRegCarType);
            string tmp = Util.Function.IniReadValue("CAMERA", "regcarrate");
            string[] sp = tmp.Split(',');
            if (tmp == "")
                sp = new string[5] { "MATIZ/60", "SPARK/60", "MORNING/60", "RAY/60", "CLICK/60" };
            EnvInfo.CameraEnv.RegCarRate = new List<ClsStructure.SmallCarRate>();
            foreach (string item in sp)
            {
                ClsStructure.SmallCarRate rate = new ClsStructure.SmallCarRate();
                string[] sp1 = item.Split('/');
                rate.CarType = sp1[0];
                int.TryParse(sp1[1], out rate.Rate);
                EnvInfo.CameraEnv.RegCarRate.Add(rate);
            }
            #endregion

            #region LPR 장비 설정
            EnvInfo.CommunicationEnv.Lpr1Info.Use = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "lpr1use"));
            EnvInfo.CommunicationEnv.Lpr1Info.EqpmNo = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "lpr1eqpmno"));
            EnvInfo.CommunicationEnv.Lpr1Info.ChNo = ReadIni("COMMUNICATION", "lpr1chname");
            EnvInfo.CommunicationEnv.Lpr1Info.Name = ReadIni("COMMUNICATION", "lpr1name");
            EnvInfo.CommunicationEnv.Lpr1Info.DevType = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "lpr1devtype"));
            EnvInfo.CommunicationEnv.Lpr1Info.InOutType = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "lpr1inouttype"));
            //EnvInfo.CommunicationEnv.Lpr1Info.FreePass = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "lpr1freepass"));
            //EnvInfo.CommunicationEnv.Lpr1Info.FreePassGateOpen = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "lpr1freepassgateopen"));
            EnvInfo.CommunicationEnv.Lpr1Info.SockInfo.IP = ReadIni("COMMUNICATION", "lpr1ip");
            EnvInfo.CommunicationEnv.Lpr1Info.SockInfo.Port = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "lpr1port"));
            EnvInfo.CommunicationEnv.Lpr1Info.ImagePath = ReadIni("COMMUNICATION", "lpr1imagepath");

            EnvInfo.CommunicationEnv.Lpr2Info.Use = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "lpr2use"));
            EnvInfo.CommunicationEnv.Lpr2Info.EqpmNo = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "lpr2eqpmno"));
            EnvInfo.CommunicationEnv.Lpr2Info.ChNo = ReadIni("COMMUNICATION", "lpr2chname");
            EnvInfo.CommunicationEnv.Lpr2Info.Name = ReadIni("COMMUNICATION", "lpr2name");
            EnvInfo.CommunicationEnv.Lpr2Info.DevType = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "lpr2devtype"));
            EnvInfo.CommunicationEnv.Lpr2Info.InOutType = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "lpr2inouttype"));
            //EnvInfo.CommunicationEnv.Lpr2Info.FreePass = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "lpr2freepass"));
            //EnvInfo.CommunicationEnv.Lpr2Info.FreePassGateOpen = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "lpr2freepassgateopen"));
            EnvInfo.CommunicationEnv.Lpr2Info.SockInfo.IP = ReadIni("COMMUNICATION", "lpr2ip");
            EnvInfo.CommunicationEnv.Lpr2Info.SockInfo.Port = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "lpr2port"));
            EnvInfo.CommunicationEnv.Lpr2Info.ImagePath = ReadIni("COMMUNICATION", "lpr2imagepath");
            #endregion
            EnvInfo.CommunicationEnv.Nodetection_Open = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "nodetectiongateopen"));
            #endregion

            #region 소켓 설정
            EnvInfo.CameraEnv.SockDataFormat = Util.Function.IntTryParse(ReadIni("CAMERA", "socketformat"));

            EnvInfo.CommunicationEnv.ClientTarget = new ClsStructure.Sock_Info[5];

            EnvInfo.CommunicationEnv.ClientTarget[0].Use = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "homelanuse"));
            EnvInfo.CommunicationEnv.ClientTarget[0].IP = ReadIni("COMMUNICATION", "homelanip");
            EnvInfo.CommunicationEnv.ClientTarget[0].Port = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "homelanport"));
            EnvInfo.CommunicationEnv.ClientTarget[0].Type = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "homelantype"));

            EnvInfo.CommunicationEnv.ClientTarget[1].Use = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "calcuse"));
            EnvInfo.CommunicationEnv.ClientTarget[1].Type = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "calctype"));
            EnvInfo.CommunicationEnv.ClientTarget[1].IP = ReadIni("COMMUNICATION", "calcip");
            EnvInfo.CommunicationEnv.ClientTarget[1].Port = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "calcport"));

            EnvInfo.CommunicationEnv.ClientTarget[2].Use = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "relaydisplayuse"));
            EnvInfo.CommunicationEnv.ClientTarget[2].Type = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "relaydisplayno"));
            EnvInfo.CommunicationEnv.ClientTarget[2].IP = ReadIni("COMMUNICATION", "relaydisplayip");
            EnvInfo.CommunicationEnv.ClientTarget[2].Port = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "relaydisplayport"));

            EnvInfo.CommunicationEnv.ClientTarget[3].Use = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "stoneuse"));
            EnvInfo.CommunicationEnv.ClientTarget[3].IP = ReadIni("COMMUNICATION", "stoneip");
            EnvInfo.CommunicationEnv.ClientTarget[3].Port = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "stoneport"));

            EnvInfo.CommunicationEnv.ClientTarget[4].Use = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "outuse"));
            EnvInfo.CommunicationEnv.ClientTarget[4].Type = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "outtype"));
            EnvInfo.CommunicationEnv.ClientTarget[4].IP = ReadIni("COMMUNICATION", "outip");
            EnvInfo.CommunicationEnv.ClientTarget[4].Port = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "outport"));
            #endregion

            #region 차단기 설정
            EnvInfo.CommonEnv.Dio.DioSetting.SerialPort = ReadIni("COMMON", "dioport");
            EnvInfo.CommonEnv.Dio.DioSetting.Setting = ReadIni("COMMON", "diosetting");
            EnvInfo.CommonEnv.Dio.DioSetting.Dev_Type_Name = ReadIni("COMMON", "diotypename");
            EnvInfo.CommonEnv.Dio.DioSetting.Type = Util.Function.BoolTryParse(ReadIni("COMMON", "boardtype"));

            EnvInfo.CommonEnv.Dio.DioOutPut = new ClsStructure.Dio_OutPut[2];
            EnvInfo.CommonEnv.Dio.DioOutPut[0].Use = Util.Function.BoolTryParse(ReadIni("COMMON", "gate1use"));
            EnvInfo.CommonEnv.Dio.DioOutPut[0].Port = Util.Function.IntTryParse(ReadIni("COMMON", "gate1port"));
            EnvInfo.CommonEnv.Dio.DioOutPut[0].Delay = Util.Function.IntTryParse(ReadIni("COMMON", "gate1delay"));
            EnvInfo.CommonEnv.Dio.DioOutPut[0].Keep = Util.Function.IntTryParse(ReadIni("COMMON", "gate1keep"));
            if (!ReadIni("COMMON", "gate1addport").Equals(string.Empty))
                EnvInfo.CommonEnv.Dio.DioOutPut[0].AddPort = Util.Function.IntTryParse(ReadIni("COMMON", "gate1addport"));
            else
                EnvInfo.CommonEnv.Dio.DioOutPut[0].AddPort = -1;
            EnvInfo.CommonEnv.Dio.DioOutPut[0].AddDelay = Util.Function.IntTryParse(ReadIni("COMMON", "gate1adddelay"));
            EnvInfo.CommonEnv.Dio.DioOutPut[0].AddKeep = Util.Function.IntTryParse(ReadIni("COMMON", "gate1addkeep"));

            EnvInfo.CommonEnv.Dio.DioOutPut[1].Use = Util.Function.BoolTryParse(ReadIni("COMMON", "gate2use"));
            EnvInfo.CommonEnv.Dio.DioOutPut[1].Port = Util.Function.IntTryParse(ReadIni("COMMON", "gate2port"));
            EnvInfo.CommonEnv.Dio.DioOutPut[1].Delay = Util.Function.IntTryParse(ReadIni("COMMON", "gate2delay"));
            EnvInfo.CommonEnv.Dio.DioOutPut[1].Keep = Util.Function.IntTryParse(ReadIni("COMMON", "gate2keep"));
            EnvInfo.CommonEnv.Dio.DioOutPut[1].AddPort = Util.Function.IntTryParse(ReadIni("COMMON", "gate2addport"));
            EnvInfo.CommonEnv.Dio.DioOutPut[1].AddDelay = Util.Function.IntTryParse(ReadIni("COMMON", "gate2adddelay"));
            EnvInfo.CommonEnv.Dio.DioOutPut[1].AddKeep = Util.Function.IntTryParse(ReadIni("COMMON", "gate2addkeep"));

            EnvInfo.CommonEnv.Dio.IsolatePort = new ClsStructure.Add_Dio_InOut();
            EnvInfo.CommonEnv.Dio.IsolatePort.Out.Use = Util.Function.BoolTryParse(ReadIni("COMMON", "isolateuse"));
            EnvInfo.CommonEnv.Dio.IsolatePort.In.LoopPort = Util.Function.IntTryParse(ReadIni("COMMON", "isolateinput"));
            EnvInfo.CommonEnv.Dio.IsolatePort.Out.Port = Util.Function.IntTryParse(ReadIni("COMMON", "isolateoutport"));
            EnvInfo.CommonEnv.Dio.IsolatePort.Out.Delay = Util.Function.IntTryParse(ReadIni("COMMON", "isolateoutdelay"));
            EnvInfo.CommonEnv.Dio.IsolatePort.Out.Keep = Util.Function.IntTryParse(ReadIni("COMMON", "isolateoutkeep"));
            EnvInfo.CommonEnv.Dio.IsolatePort.Out.AddPort = Util.Function.IntTryParse(ReadIni("COMMON", "isolateoutaddport"));
            EnvInfo.CommonEnv.Dio.IsolatePort.Out.AddDelay = Util.Function.IntTryParse(ReadIni("COMMON", "isolateoutadddelay"));
            EnvInfo.CommonEnv.Dio.IsolatePort.Out.AddKeep = Util.Function.IntTryParse(ReadIni("COMMON", "isolateoutaddkeep"));
            #endregion

            #region 전광판
            EnvInfo.CommunicationEnv.DisPlay = new ClsStructure.DisPlay_Info[2];
            EnvInfo.CommunicationEnv.DisPlay[0].Use = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "display1Use"));
            EnvInfo.CommunicationEnv.DisPlay[0].Com.SerialPort = ReadIni("COMMUNICATION", "display1port");
            EnvInfo.CommunicationEnv.DisPlay[0].Com.Setting = ReadIni("COMMUNICATION", "display1setting");
            EnvInfo.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name = ReadIni("COMMUNICATION", "display1Type");
            EnvInfo.CommunicationEnv.DisPlay[0].Ment.Ment1Line = ReadIni("COMMUNICATION", "display1ment1line");
            EnvInfo.CommunicationEnv.DisPlay[0].Ment.Ment1Color = ReadIni("COMMUNICATION", "display1ment1color");
            EnvInfo.CommunicationEnv.DisPlay[0].Ment.Ment2Line = ReadIni("COMMUNICATION", "display1ment2line");
            EnvInfo.CommunicationEnv.DisPlay[0].Ment.Ment2Color = ReadIni("COMMUNICATION", "display1ment2color");
            EnvInfo.CommunicationEnv.DisPlay[0].NormalCar = ReadIni("COMMUNICATION", "display1normalcar");
            EnvInfo.CommunicationEnv.DisPlay[0].Normal1Color = ReadIni("COMMUNICATION", "display1normal1color");
            EnvInfo.CommunicationEnv.DisPlay[0].Normal2Color = ReadIni("COMMUNICATION", "display1normal2color");
            EnvInfo.CommunicationEnv.DisPlay[0].PeriodCar = ReadIni("COMMUNICATION", "display1periodcar");
            EnvInfo.CommunicationEnv.DisPlay[0].Period1Color = ReadIni("COMMUNICATION", "display1period1color");
            EnvInfo.CommunicationEnv.DisPlay[0].Period2Color = ReadIni("COMMUNICATION", "display1period2color");
            EnvInfo.CommunicationEnv.DisPlay[0].Net.Use = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "display1netuse"));
            EnvInfo.CommunicationEnv.DisPlay[0].Net.IP = ReadIni("COMMUNICATION", "display1netip");
            EnvInfo.CommunicationEnv.DisPlay[0].Net.Port = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "display1netport"));
            EnvInfo.CommunicationEnv.DisPlay[0].UseFiex = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "usefixtext1"));

            EnvInfo.CommunicationEnv.DisPlay[1].Use = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "display2Use"));
            EnvInfo.CommunicationEnv.DisPlay[1].Com.SerialPort = ReadIni("COMMUNICATION", "display2port");
            EnvInfo.CommunicationEnv.DisPlay[1].Com.Setting = ReadIni("COMMUNICATION", "display2setting");
            EnvInfo.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name = ReadIni("COMMUNICATION", "display2Type");
            EnvInfo.CommunicationEnv.DisPlay[1].Ment.Ment1Line = ReadIni("COMMUNICATION", "display2ment1line");
            EnvInfo.CommunicationEnv.DisPlay[1].Ment.Ment1Color = ReadIni("COMMUNICATION", "display2ment1color");
            EnvInfo.CommunicationEnv.DisPlay[1].Ment.Ment2Line = ReadIni("COMMUNICATION", "display2ment2line");
            EnvInfo.CommunicationEnv.DisPlay[1].Ment.Ment2Color = ReadIni("COMMUNICATION", "display2ment2color");
            EnvInfo.CommunicationEnv.DisPlay[1].NormalCar = ReadIni("COMMUNICATION", "display2normalcar");
            EnvInfo.CommunicationEnv.DisPlay[1].Normal1Color = ReadIni("COMMUNICATION", "display2normal1color");
            EnvInfo.CommunicationEnv.DisPlay[1].Normal2Color = ReadIni("COMMUNICATION", "display2normal2color");
            EnvInfo.CommunicationEnv.DisPlay[1].PeriodCar = ReadIni("COMMUNICATION", "display2periodcar");
            EnvInfo.CommunicationEnv.DisPlay[1].Period1Color = ReadIni("COMMUNICATION", "display2period1color");
            EnvInfo.CommunicationEnv.DisPlay[1].Period2Color = ReadIni("COMMUNICATION", "display2period2color");
            EnvInfo.CommunicationEnv.DisPlay[1].Net.Use = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "display2netuse"));
            EnvInfo.CommunicationEnv.DisPlay[1].Net.IP = ReadIni("COMMUNICATION", "display2netip");
            EnvInfo.CommunicationEnv.DisPlay[1].Net.Port = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "display2netport"));
            EnvInfo.CommunicationEnv.DisPlay[1].UseFiex = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "usefixtext2"));

            EnvInfo.CommunicationEnv.FixedMent.Ment1Line = ReadIni("COMMUNICATION", "fixedment1");
            EnvInfo.CommunicationEnv.FixedMent.Ment1Color = ReadIni("COMMUNICATION", "fixedcolor1");
            EnvInfo.CommunicationEnv.FixedMent.Ment2Line = ReadIni("COMMUNICATION", "fixedment2");
            EnvInfo.CommunicationEnv.FixedMent.Ment2Color = ReadIni("COMMUNICATION", "fixedcolor2");
            EnvInfo.CommunicationEnv.PeriodMent.Ment1Line = ReadIni("COMMUNICATION", "periodment1");
            EnvInfo.CommunicationEnv.PeriodMent.Ment2Line = ReadIni("COMMUNICATION", "periodment2");
            EnvInfo.CommunicationEnv.FixedPort = Util.Function.IntTryParse(ReadIni("COMMUNICATION", "fixedport"));
            #endregion

            #region 자료 처리
            EnvInfo.CommunicationEnv.Lpr1Info.LprOpt.Period_SendData = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR1Period_SendData"));
            EnvInfo.CommunicationEnv.Lpr1Info.LprOpt.Period_Lprtrns = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR1Period_Lprtrns"));
            EnvInfo.CommunicationEnv.Lpr1Info.LprOpt.Period_Passtrns = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR1Period_Passtrns"));
            EnvInfo.CommunicationEnv.Lpr1Info.LprOpt.Period_Counter = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR1Period_Fcstay"));
            EnvInfo.CommunicationEnv.Lpr1Info.LprOpt.Period_Gate = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR1Period_Fccounttrns"));
            EnvInfo.CommunicationEnv.Lpr1Info.LprOpt.Normal_SendData = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR1Normal_SendData"));
            EnvInfo.CommunicationEnv.Lpr1Info.LprOpt.Normal_Lprtrns = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR1Normal_Lprtrns"));
            EnvInfo.CommunicationEnv.Lpr1Info.LprOpt.Normal_Tckttrns = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR1Normal_Tckttrns"));
            EnvInfo.CommunicationEnv.Lpr1Info.LprOpt.Normal_Counter = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR1Normal_Fcstay"));
            EnvInfo.CommunicationEnv.Lpr1Info.LprOpt.Normal_Gate = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR1Normal_Fccounttrns"));
            EnvInfo.CommunicationEnv.Lpr2Info.LprOpt.Period_SendData = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR2Period_SendData"));
            EnvInfo.CommunicationEnv.Lpr2Info.LprOpt.Period_Lprtrns = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR2Period_Lprtrns"));
            EnvInfo.CommunicationEnv.Lpr2Info.LprOpt.Period_Passtrns = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR2Period_Passtrns"));
            EnvInfo.CommunicationEnv.Lpr2Info.LprOpt.Period_Counter = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR2Period_Fcstay"));
            EnvInfo.CommunicationEnv.Lpr2Info.LprOpt.Period_Gate = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR2Period_Fccounttrns"));
            EnvInfo.CommunicationEnv.Lpr2Info.LprOpt.Normal_SendData = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR2Normal_SendData"));
            EnvInfo.CommunicationEnv.Lpr2Info.LprOpt.Normal_Lprtrns = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR2Normal_Lprtrns"));
            EnvInfo.CommunicationEnv.Lpr2Info.LprOpt.Normal_Tckttrns = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR2Normal_Tckttrns"));
            EnvInfo.CommunicationEnv.Lpr2Info.LprOpt.Normal_Counter = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR2Normal_Fcstay"));
            EnvInfo.CommunicationEnv.Lpr2Info.LprOpt.Normal_Gate = Util.Function.BoolTryParse(ReadIni("DataProcess", "LPR2Normal_Fccounttrns"));
            EnvInfo.CommunicationEnv.ReturnCar.Use = Util.Function.BoolTryParse(ReadIni("DataProcess", "ReturnUse"));
            EnvInfo.CommunicationEnv.ReturnCar.Term =  Util.Function.IntTryParse(ReadIni("DataProcess", "ReturnTerm"));
            EnvInfo.CommunicationEnv.ReturnCar.Ment = ReadIni("DataProcess", "ReturnMent");
            #endregion
            EnvInfo.SendOffice = Util.Function.BoolTryParse(ReadIni("COMMUNICATION", "sendOffice"));


            //leess 긴급차량 개방
            EnvInfo.EmergencyCar = Util.Function.BoolTryParse(ReadIni("COMMON", "emergencyCar", "true"));

            //인식 지연 
            DelayReg.ReadDelay();

            SpecialGroup.LoadInfo();

            BlackList.ReadEnv();
            EnvInfo.RegCarControl = new RegCarControl();
            EnvInfo.RegCarControl = EnvInfo.RegCarControl.Load();
            return EnvInfo;
        }

        private string ReadIni(string Param1, string Param2)
        {
            return Util.Function.IniReadValue(Param1, Param2);
        }
        
        //leess 기본값 추가
        private string ReadIni(string Param1, string Param2, string defaultVal)
        {
            string val = Util.Function.IniReadValue(Param1, Param2);
            if(val == null || val.Length == 0) {
                return defaultVal;
            } else {
                return val;
            }
        }

        private void WriteIni(string Param1, string Param2, string Param3)
        {
            Util.Function.IniWriteValue(Param1, Param2, Param3);
        }

        private void WriteIni(string Param1, string Param2, int Param3)
        {
            Util.Function.IniWriteValue(Param1, Param2, Param3);
        }

        private void WriteIni(string Param1, string Param2, bool Param3)
        {
            Util.Function.IniWriteValue(Param1, Param2, Param3);
        }

        private void WriteIni(string Param1, string Param2, double Param3)
        {
            Util.Function.IniWriteValue(Param1, Param2, Param3);
        }

        public void SetEnv(ClsStructure.EnvStruct env)
        {
            #region 기본설정
            #region TestMode
            WriteIni("Public", "Test", env.TestMode);
            #endregion

            #region DB 정보
            WriteIni("COMMON", "dbip", env.CommonEnv.DBInfo.Ip);
            WriteIni("COMMON", "dbid", env.CommonEnv.DBInfo.Id);
            WriteIni("COMMON", "dbpw", env.CommonEnv.DBInfo.Pw);
            WriteIni("COMMON", "masterdb", env.CommonEnv.DBInfo.MstDB);
            WriteIni("COMMON", "trnsdb", env.CommonEnv.DBInfo.TrnsDb);
            #endregion

            #region 주차장 정보
            WriteIni("COMMON", "parkno", env.CommunicationEnv.ParkInfo.No);
            WriteIni("PARK", "extno", env.CommunicationEnv.ParkInfo.Ext_No);
            WriteIni("COMMON", "clientno", env.CommunicationEnv.ParkInfo.Client_No);
            #endregion

            #region 인식률 보정
            WriteIni("COMMUNICATION", "correction", env.CommunicationEnv.RegCorrection);
            #endregion

            #region 이미지 저장
            WriteIni("COMMUNICATION", "imagesaveuse", env.CommunicationEnv.ImageSave.Use);
            WriteIni("COMMUNICATION", "imagesavepath", env.CommunicationEnv.ImageSave.SavePath);
            #endregion

            #region 프로그램 시작
            WriteIni("COMMON", "starttype",  env.StartType);
            #endregion
            
            WriteIni("FullControl", "Manual", FullSpaceControl.Manual);
            WriteIni("FullControl", "Period", FullSpaceControl.Period);
            WriteIni("FullControl", "EntGateOpen", FullSpaceControl.EntGateOpen);

            #region 마스터 취득 II
            WriteIni("GetMaster", "Use", GetMasterInfo.Use);
            WriteIni("GetMaster", "SharePath", GetMasterInfo.SharePath);
            WriteIni("GetMaster", "Term", GetMasterInfo.Term);
            #endregion
            #endregion

            #region 카메라 설정
            //leess iNova 카메라종류 설정
            WriteIni("CAMERA", "iNovaType", env.CameraEnv.iNovaType);
            #region Camera1
            WriteIni("CAMERA", "cam1useflag", env.CameraEnv.IPCamera1Info.Use);
            WriteIni("CAMERA", "cam1ip", env.CameraEnv.IPCamera1Info.IP);
            WriteIni("CAMERA", "cam1rtspurl", env.CameraEnv.IPCamera1Info.RtspUrl ?? "");
            WriteIni("CAMERA", "cam1chname", env.CameraEnv.IPCamera1Info.ChName);
            WriteIni("CAMERA", "cam1udp", env.CameraEnv.IPCamera1Info.StreamUdp);
            //WriteIni("IPCAM1", "etcsave", env.CameraEnv.IPCamera1Info.ImageSave.EtcSave);
            //WriteIni("IPCAM1", "etcpath", env.CameraEnv.IPCamera1Info.ImageSave.EtcPath);
            WriteIni("CAMERA", "cam1interval", env.CameraEnv.IPCamera1Info.User_Setting_Resend_Interval);

            if (env.CameraEnv.IPCamera1Info.User_Setting != null)
                for (int i = 0; i < 3; i++)
                {
                    WriteIni("CAMERA", string.Format("cam1time{0}1useflag", i), env.CameraEnv.IPCamera1Info.User_Setting[i].use);
                    WriteIni("CAMERA", string.Format("cam1time{0}starttime", i), env.CameraEnv.IPCamera1Info.User_Setting[i].StartTime);
                    WriteIni("CAMERA", string.Format("cam1time{0}endtime", i), env.CameraEnv.IPCamera1Info.User_Setting[i].EndTime);
                    WriteIni("CAMERA", string.Format("cam1time{0}exposure", i), env.CameraEnv.IPCamera1Info.User_Setting[i].Exposuer);
                    WriteIni("CAMERA", string.Format("cam1time{0}bracket", i), env.CameraEnv.IPCamera1Info.User_Setting[i].ModeIdx);
                    WriteIni("CAMERA", string.Format("cam1time{0}usebracket", i), env.CameraEnv.IPCamera1Info.User_Setting[i].UseBarkect);
                    WriteIni("CAMERA", string.Format("cam1time{0}usealc", i), env.CameraEnv.IPCamera1Info.User_Setting[i].UseALC);
                }
            WriteIni("CAMERA", "cam1triggercnt", env.CameraEnv.IPCamera1Info.TriggerCnt);

            if (env.CameraEnv.IPCamera1Info.User_Brakect != null)
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        WriteIni("CAMERA", string.Format("cam1time{0}{1}exposure", i + 1, j + 1), env.CameraEnv.IPCamera1Info.User_Brakect[i, j].Exposure);
                        WriteIni("CAMERA", string.Format("cam1time{0}{1}analoggain", i + 1, j + 1), env.CameraEnv.IPCamera1Info.User_Brakect[i, j].AnalogGain);
                        WriteIni("CAMERA", string.Format("cam1time{0}{1}digitalgain", i + 1, j + 1), env.CameraEnv.IPCamera1Info.User_Brakect[i, j].DigitalGain);
                    }
                }
            WriteIni("CAMERA", "cam1brakect", env.CameraEnv.IPCamera1Info.BarkectCnt);

            if (env.CameraEnv.IPCamera1Info.User_Alc != null)
                for (int i = 0; i < 3; i++)
                {
                    WriteIni("CAMERA", string.Format("cam1time{0}alctarget", i + 1), env.CameraEnv.IPCamera1Info.User_Alc[i].target);
                    WriteIni("CAMERA", string.Format("cam1time{0}aecuse", i + 1), env.CameraEnv.IPCamera1Info.User_Alc[i].AECInfo.enableAEC);
                    WriteIni("CAMERA", string.Format("cam1time{0}aecmin", i + 1), env.CameraEnv.IPCamera1Info.User_Alc[i].AECInfo.minExposure);
                    WriteIni("CAMERA", string.Format("cam1time{0}aecmax", i + 1), env.CameraEnv.IPCamera1Info.User_Alc[i].AECInfo.maxExposure);
                    WriteIni("CAMERA", string.Format("cam1time{0}agcuse", i + 1), env.CameraEnv.IPCamera1Info.User_Alc[i].AGCInfo.enableAGC);
                    WriteIni("CAMERA", string.Format("cam1time{0}agcmin", i + 1), env.CameraEnv.IPCamera1Info.User_Alc[i].AGCInfo.minGain);
                    WriteIni("CAMERA", string.Format("cam1time{0}agcmax", i + 1), env.CameraEnv.IPCamera1Info.User_Alc[i].AGCInfo.maxGain);
                }

            WriteIni("CAMERA", "cam1framerate", env.CameraEnv.IPCamera1Info.FrameRate);
            WriteIni("CAMERA", "cam1TriggerMode", env.CameraEnv.IPCamera1Info.TriggerMode);

            WriteIni("CAMERA", "cam1dioport", env.CameraEnv.IPCamera1Info.DioInPut.LoopPort);
            WriteIni("CAMERA", "cam1samllcaruse", env.CameraEnv.IPCamera1Info.DioInPut.SmallCar);
            WriteIni("CAMERA", "cam1smallcarport", env.CameraEnv.IPCamera1Info.DioInPut.SmallPort);

            WriteIni("CAMERA", "cam1roi", String.Format("{0}, {1}, {2}, {3}", env.CameraEnv.IPCamera1Info.Roi.X, env.CameraEnv.IPCamera1Info.Roi.Y, env.CameraEnv.IPCamera1Info.Roi.Width, env.CameraEnv.IPCamera1Info.Roi.Height));
            WriteIni("CAMERA", "cam1SendSTXETX", env.CameraEnv.IPCamera1Info.SendStxEtx);
            #endregion

            #region Camera2
            WriteIni("CAMERA", "cam2useflag", env.CameraEnv.IPCamera2Info.Use);
            WriteIni("CAMERA", "cam2ip", env.CameraEnv.IPCamera2Info.IP);
            WriteIni("CAMERA", "cam2rtspurl", env.CameraEnv.IPCamera2Info.RtspUrl ?? "");
            WriteIni("CAMERA", "cam2chname", env.CameraEnv.IPCamera2Info.ChName);
            WriteIni("CAMERA", "cam2udp", env.CameraEnv.IPCamera2Info.StreamUdp);
            //WriteIni("IPCAM2", "etcsave", env.CameraEnv.IPCamera2Info.ImageSave.EtcSave);
            //WriteIni("IPCAM2", "etcpath", env.CameraEnv.IPCamera2Info.ImageSave.EtcPath);
            WriteIni("CAMERA", "cam2interval", env.CameraEnv.IPCamera2Info.User_Setting_Resend_Interval);

            if (env.CameraEnv.IPCamera2Info.User_Setting != null)
                for (int i = 0; i < 3; i++)
                {
                    WriteIni("CAMERA", string.Format("cam2time{0}1useflag", i), env.CameraEnv.IPCamera2Info.User_Setting[i].use);
                    WriteIni("CAMERA", string.Format("cam2time{0}starttime", i), env.CameraEnv.IPCamera2Info.User_Setting[i].StartTime);
                    WriteIni("CAMERA", string.Format("cam2time{0}endtime", i), env.CameraEnv.IPCamera2Info.User_Setting[i].EndTime);
                    WriteIni("CAMERA", string.Format("cam2time{0}exposure", i), env.CameraEnv.IPCamera2Info.User_Setting[i].Exposuer);
                    WriteIni("CAMERA", string.Format("cam2time{0}bracket", i), env.CameraEnv.IPCamera2Info.User_Setting[i].ModeIdx);
                    WriteIni("CAMERA", string.Format("cam2time{0}usebracket", i), env.CameraEnv.IPCamera2Info.User_Setting[i].UseBarkect);
                    WriteIni("CAMERA", string.Format("cam2time{0}usealc", i), env.CameraEnv.IPCamera2Info.User_Setting[i].UseALC);
                }
            WriteIni("CAMERA", "cam2triggercnt", env.CameraEnv.IPCamera2Info.TriggerCnt);

            if (env.CameraEnv.IPCamera2Info.User_Brakect != null)
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        WriteIni("CAMERA", string.Format("cam2time{0}{1}exposure", i + 1, j + 1), env.CameraEnv.IPCamera2Info.User_Brakect[i, j].Exposure);
                        WriteIni("CAMERA", string.Format("cam2time{0}{1}analoggain", i + 1, j + 1), env.CameraEnv.IPCamera2Info.User_Brakect[i, j].AnalogGain);
                        WriteIni("CAMERA", string.Format("cam2time{0}{1}digitalgain", i + 1, j + 1), env.CameraEnv.IPCamera2Info.User_Brakect[i, j].DigitalGain);
                    }
                }
            WriteIni("CAMERA", "cam2brakect", env.CameraEnv.IPCamera2Info.BarkectCnt);

            if (env.CameraEnv.IPCamera2Info.User_Alc != null)
                for (int i = 0; i < 3; i++)
                {
                    WriteIni("CAMERA", string.Format("cam2time{0}alctarget", i + 1), env.CameraEnv.IPCamera2Info.User_Alc[i].target);
                    WriteIni("CAMERA", string.Format("cam2time{0}aecuse", i + 1), env.CameraEnv.IPCamera2Info.User_Alc[i].AECInfo.enableAEC);
                    WriteIni("CAMERA", string.Format("cam2time{0}aecmin", i + 1), env.CameraEnv.IPCamera2Info.User_Alc[i].AECInfo.minExposure);
                    WriteIni("CAMERA", string.Format("cam2time{0}aecmax", i + 1), env.CameraEnv.IPCamera2Info.User_Alc[i].AECInfo.maxExposure);
                    WriteIni("CAMERA", string.Format("cam2time{0}agcuse", i + 1), env.CameraEnv.IPCamera2Info.User_Alc[i].AGCInfo.enableAGC);
                    WriteIni("CAMERA", string.Format("cam2time{0}agcmin", i + 1), env.CameraEnv.IPCamera2Info.User_Alc[i].AGCInfo.minGain);
                    WriteIni("CAMERA", string.Format("cam2time{0}agcmax", i + 1), env.CameraEnv.IPCamera2Info.User_Alc[i].AGCInfo.maxGain);
                }

            WriteIni("CAMERA", "cam2framerate", env.CameraEnv.IPCamera2Info.FrameRate);
            WriteIni("CAMERA", "cam2TriggerMode", env.CameraEnv.IPCamera2Info.TriggerMode);

            WriteIni("CAMERA", "cam2dioport", env.CameraEnv.IPCamera2Info.DioInPut.LoopPort);
            WriteIni("CAMERA", "cam2samllcaruse", env.CameraEnv.IPCamera2Info.DioInPut.SmallCar);
            WriteIni("CAMERA", "cam2smallcarport", env.CameraEnv.IPCamera2Info.DioInPut.SmallPort);

            WriteIni("CAMERA", "cam2roi", String.Format("{0}, {1}, {2}, {3}", env.CameraEnv.IPCamera2Info.Roi.X, env.CameraEnv.IPCamera2Info.Roi.Y, env.CameraEnv.IPCamera2Info.Roi.Width, env.CameraEnv.IPCamera2Info.Roi.Height));
            WriteIni("CAMERA", "cam2SendSTXETX", env.CameraEnv.IPCamera2Info.SendStxEtx);
            #endregion
            #endregion

            #region LPR설정
            #region 이미지 저장
            WriteIni("CAMERA", "imgsavepath", env.CameraEnv.ImageSave.SavePath);
            WriteIni("CAMERA", "imgdeleteterm", env.CameraEnv.ImageSave.SaveTerm);
            WriteIni("CAMERA", "etcpath", env.CameraEnv.ImageSave.EtcPath);
            WriteIni("CAMERA", "etcpathuse", env.CameraEnv.ImageSave.EtcSave);
            #endregion

            #region 인식 모듈
            WriteIni("CAMERA", "regmodule", env.CameraEnv.RegModule);
            WriteIni("CAMERA", "plateregtype", env.CameraEnv.PlateArea);
            WriteIni("CAMERA", "regtype", env.CameraEnv.CoreType);
            WriteIni("CAMERA", "regcountrytype", env.CameraEnv.CoreCountry);
            WriteIni("CAMERA", "regcartype", env.CameraEnv.bRegCarType);
            WriteIni("CAMERA", "recogmode", env.CameraEnv.RecogMode);

            string tmp = "";
            foreach (ClsStructure.SmallCarRate item in env.CameraEnv.RegCarRate)
            {
                tmp += string.Format("{0}/{1},", item.CarType, item.Rate);
            }
            tmp = tmp.Substring(0, tmp.Length - 1);
            WriteIni("CAMERA", "regcarrate", tmp);
            #endregion

            #region LPR 장비
            WriteIni("COMMUNICATION", "lpr1use", env.CommunicationEnv.Lpr1Info.Use);
            WriteIni("COMMUNICATION", "lpr1eqpmno", env.CommunicationEnv.Lpr1Info.EqpmNo);
            WriteIni("COMMUNICATION", "lpr1chname", env.CommunicationEnv.Lpr1Info.ChNo);
            WriteIni("COMMUNICATION", "lpr1name", env.CommunicationEnv.Lpr1Info.Name);
            WriteIni("COMMUNICATION", "lpr1devtype", env.CommunicationEnv.Lpr1Info.DevType);
            WriteIni("COMMUNICATION", "lpr1inouttype", env.CommunicationEnv.Lpr1Info.InOutType);
            //WriteIni("COMMUNICATION", "lpr1freepass", env.CommunicationEnv.Lpr1Info.FreePass);
            //WriteIni("COMMUNICATION", "lpr1freepassgateopen", env.CommunicationEnv.Lpr1Info.FreePassGateOpen);
            WriteIni("COMMUNICATION", "lpr1ip", env.CommunicationEnv.Lpr1Info.SockInfo.IP);
            WriteIni("COMMUNICATION", "lpr1port", env.CommunicationEnv.Lpr1Info.SockInfo.Port);
            WriteIni("COMMUNICATION", "lpr1imagepath", env.CommunicationEnv.Lpr1Info.ImagePath);

            WriteIni("COMMUNICATION", "lpr2use", env.CommunicationEnv.Lpr2Info.Use);
            WriteIni("COMMUNICATION", "lpr2eqpmno", env.CommunicationEnv.Lpr2Info.EqpmNo);
            WriteIni("COMMUNICATION", "lpr2chname", env.CommunicationEnv.Lpr2Info.ChNo);
            WriteIni("COMMUNICATION", "lpr2name", env.CommunicationEnv.Lpr2Info.Name);
            WriteIni("COMMUNICATION", "lpr2devtype", env.CommunicationEnv.Lpr2Info.DevType);
            WriteIni("COMMUNICATION", "lpr2inouttype", env.CommunicationEnv.Lpr2Info.InOutType);
            //WriteIni("COMMUNICATION", "lpr2freepass", env.CommunicationEnv.Lpr2Info.FreePass);
            //WriteIni("COMMUNICATION", "lpr2freepassgateopen", env.CommunicationEnv.Lpr2Info.FreePassGateOpen);
            WriteIni("COMMUNICATION", "lpr2ip", env.CommunicationEnv.Lpr2Info.SockInfo.IP);
            WriteIni("COMMUNICATION", "lpr2port", env.CommunicationEnv.Lpr2Info.SockInfo.Port);
            WriteIni("COMMUNICATION", "lpr2imagepath", env.CommunicationEnv.Lpr2Info.ImagePath);
            #endregion
            WriteIni("COMMUNICATION", "nodetectiongateopen", env.CommunicationEnv.Nodetection_Open);
            #endregion

            #region 소켓통신
            WriteIni("CAMERA", "socketformat", env.CameraEnv.SockDataFormat);
            WriteIni("COMMUNICATION", "homelanuse", env.CommunicationEnv.ClientTarget[0].Use);
            WriteIni("COMMUNICATION", "homelanip", env.CommunicationEnv.ClientTarget[0].IP);
            WriteIni("COMMUNICATION", "homelanport", env.CommunicationEnv.ClientTarget[0].Port);
            WriteIni("COMMUNICATION", "homelantype", env.CommunicationEnv.ClientTarget[0].Type);

            WriteIni("COMMUNICATION", "calcuse", env.CommunicationEnv.ClientTarget[1].Use);
            WriteIni("COMMUNICATION", "calctype", env.CommunicationEnv.ClientTarget[1].Type);
            WriteIni("COMMUNICATION", "calcip", env.CommunicationEnv.ClientTarget[1].IP);
            WriteIni("COMMUNICATION", "calcport", env.CommunicationEnv.ClientTarget[1].Port);

            WriteIni("COMMUNICATION", "relaydisplayuse", env.CommunicationEnv.ClientTarget[2].Use);
            WriteIni("COMMUNICATION", "relaydisplayno", env.CommunicationEnv.ClientTarget[2].Type);
            WriteIni("COMMUNICATION", "relaydisplayip", env.CommunicationEnv.ClientTarget[2].IP);
            WriteIni("COMMUNICATION", "relaydisplayport", env.CommunicationEnv.ClientTarget[2].Port);

            WriteIni("COMMUNICATION", "stoneuse", env.CommunicationEnv.ClientTarget[3].Use);
            WriteIni("COMMUNICATION", "stoneip", env.CommunicationEnv.ClientTarget[3].IP);
            WriteIni("COMMUNICATION", "stoneport", env.CommunicationEnv.ClientTarget[3].Port);

            WriteIni("COMMUNICATION", "outuse", env.CommunicationEnv.ClientTarget[4].Use);
            WriteIni("COMMUNICATION", "outtype", env.CommunicationEnv.ClientTarget[4].Type);
            WriteIni("COMMUNICATION", "outip", env.CommunicationEnv.ClientTarget[4].IP);
            WriteIni("COMMUNICATION", "outport", env.CommunicationEnv.ClientTarget[4].Port);
            #endregion

            #region 차단기 설정
            WriteIni("COMMON", "dioport", env.CommonEnv.Dio.DioSetting.SerialPort);
            WriteIni("COMMON", "diosetting", env.CommonEnv.Dio.DioSetting.Setting);
            WriteIni("COMMON", "diotypename", env.CommonEnv.Dio.DioSetting.Dev_Type_Name);
            WriteIni("COMMON", "boardtype", env.CommonEnv.Dio.DioSetting.Type);

            WriteIni("COMMON", "gate1use", env.CommonEnv.Dio.DioOutPut[0].Use);
            WriteIni("COMMON", "gate1port", env.CommonEnv.Dio.DioOutPut[0].Port);
            WriteIni("COMMON", "gate1delay", env.CommonEnv.Dio.DioOutPut[0].Delay);
            WriteIni("COMMON", "gate1keep", env.CommonEnv.Dio.DioOutPut[0].Keep);
            if (env.CommonEnv.Dio.DioOutPut[0].AddPort>-1)
                WriteIni("COMMON", "gate1addport", env.CommonEnv.Dio.DioOutPut[0].AddPort.ToString());
            else
                WriteIni("COMMON", "gate1addport", "");
            WriteIni("COMMON", "gate1adddelay", env.CommonEnv.Dio.DioOutPut[0].AddDelay);
            WriteIni("COMMON", "gate1addkeep", env.CommonEnv.Dio.DioOutPut[0].AddKeep);

            WriteIni("COMMON", "gate2use", env.CommonEnv.Dio.DioOutPut[1].Use);
            WriteIni("COMMON", "gate2port", env.CommonEnv.Dio.DioOutPut[1].Port);
            WriteIni("COMMON", "gate2delay", env.CommonEnv.Dio.DioOutPut[1].Delay);
            WriteIni("COMMON", "gate2keep", env.CommonEnv.Dio.DioOutPut[1].Keep);

            if (env.CommonEnv.Dio.DioOutPut[1].AddPort > -1)
                WriteIni("COMMON", "gate2addport", env.CommonEnv.Dio.DioOutPut[1].AddPort.ToString());
            else
                WriteIni("COMMON", "gate2addport", "");
            WriteIni("COMMON", "gate2adddelay", env.CommonEnv.Dio.DioOutPut[1].AddDelay);
            WriteIni("COMMON", "gate2addkeep", env.CommonEnv.Dio.DioOutPut[1].AddKeep);

            //추가 포트
            WriteIni("COMMON", "Isolateuse", env.CommonEnv.Dio.IsolatePort.Out.Use);
            WriteIni("COMMON", "Isolateinput", env.CommonEnv.Dio.IsolatePort.In.LoopPort);
            WriteIni("COMMON", "Isolateoutport", env.CommonEnv.Dio.IsolatePort.Out.Port);
            WriteIni("COMMON", "Isolateoutdelay", env.CommonEnv.Dio.IsolatePort.Out.Delay);
            WriteIni("COMMON", "isolateoutkeep", env.CommonEnv.Dio.IsolatePort.Out.Keep);
            WriteIni("COMMON", "isolateoutaddport", env.CommonEnv.Dio.IsolatePort.Out.AddPort);
            WriteIni("COMMON", "isolateoutadddelay", env.CommonEnv.Dio.IsolatePort.Out.AddDelay);
            WriteIni("COMMON", "isolateoutaddkeep", env.CommonEnv.Dio.IsolatePort.Out.AddKeep);
            #endregion

            #region 전광판
            WriteIni("COMMUNICATION", "display1Use", env.CommunicationEnv.DisPlay[0].Use);
            WriteIni("COMMUNICATION", "display1port", env.CommunicationEnv.DisPlay[0].Com.SerialPort);
            WriteIni("COMMUNICATION", "display1setting", env.CommunicationEnv.DisPlay[0].Com.Setting);
            WriteIni("COMMUNICATION", "display1Type", env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name);
            WriteIni("COMMUNICATION", "display1ment1line", env.CommunicationEnv.DisPlay[0].Ment.Ment1Line);
            WriteIni("COMMUNICATION", "display1ment1color", env.CommunicationEnv.DisPlay[0].Ment.Ment1Color);
            WriteIni("COMMUNICATION", "display1ment2line", env.CommunicationEnv.DisPlay[0].Ment.Ment2Line);
            WriteIni("COMMUNICATION", "display1ment2color", env.CommunicationEnv.DisPlay[0].Ment.Ment2Color);
            WriteIni("COMMUNICATION", "display1normalcar", env.CommunicationEnv.DisPlay[0].NormalCar);
            WriteIni("COMMUNICATION", "display1normal1color", env.CommunicationEnv.DisPlay[0].Normal1Color);
            WriteIni("COMMUNICATION", "display1normal2color", env.CommunicationEnv.DisPlay[0].Normal2Color);
            WriteIni("COMMUNICATION", "display1periodcar", env.CommunicationEnv.DisPlay[0].PeriodCar);
            WriteIni("COMMUNICATION", "display1period1color", env.CommunicationEnv.DisPlay[0].Period1Color);
            WriteIni("COMMUNICATION", "display1period2color", env.CommunicationEnv.DisPlay[0].Period2Color);
            WriteIni("COMMUNICATION", "display1netuse", env.CommunicationEnv.DisPlay[0].Net.Use);
            WriteIni("COMMUNICATION", "display1netip", env.CommunicationEnv.DisPlay[0].Net.IP);
            WriteIni("COMMUNICATION", "display1netport", env.CommunicationEnv.DisPlay[0].Net.Port);

            WriteIni("COMMUNICATION", "display2Use", env.CommunicationEnv.DisPlay[1].Use);
            WriteIni("COMMUNICATION", "display2port", env.CommunicationEnv.DisPlay[1].Com.SerialPort);
            WriteIni("COMMUNICATION", "display2setting", env.CommunicationEnv.DisPlay[1].Com.Setting);
            WriteIni("COMMUNICATION", "display2Type", env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name);
            WriteIni("COMMUNICATION", "display2ment1line", env.CommunicationEnv.DisPlay[1].Ment.Ment1Line);
            WriteIni("COMMUNICATION", "display2ment1color", env.CommunicationEnv.DisPlay[1].Ment.Ment1Color);
            WriteIni("COMMUNICATION", "display2ment2line", env.CommunicationEnv.DisPlay[1].Ment.Ment2Line);
            WriteIni("COMMUNICATION", "display2ment2color", env.CommunicationEnv.DisPlay[1].Ment.Ment2Color);
            WriteIni("COMMUNICATION", "display2normalcar", env.CommunicationEnv.DisPlay[1].NormalCar);
            WriteIni("COMMUNICATION", "display2normal1color", env.CommunicationEnv.DisPlay[1].Normal1Color);
            WriteIni("COMMUNICATION", "display2normal2color", env.CommunicationEnv.DisPlay[1].Normal2Color);
            WriteIni("COMMUNICATION", "display2periodcar", env.CommunicationEnv.DisPlay[1].PeriodCar);
            WriteIni("COMMUNICATION", "display2period1color", env.CommunicationEnv.DisPlay[1].Period1Color);
            WriteIni("COMMUNICATION", "display2period2color", env.CommunicationEnv.DisPlay[1].Period2Color);
            WriteIni("COMMUNICATION", "display2period2color", env.CommunicationEnv.DisPlay[1].Period2Color);
            WriteIni("COMMUNICATION", "usefixtext2", env.CommunicationEnv.DisPlay[1].UseFiex);
            WriteIni("COMMUNICATION", "display2netuse", env.CommunicationEnv.DisPlay[1].Net.Use);
            WriteIni("COMMUNICATION", "display2netip", env.CommunicationEnv.DisPlay[1].Net.IP);
            WriteIni("COMMUNICATION", "display2netport", env.CommunicationEnv.DisPlay[1].Net.Port);

            WriteIni("COMMUNICATION", "usefixtext1", env.CommunicationEnv.DisPlay[0].UseFiex);
            WriteIni("COMMUNICATION", "fixedment1", env.CommunicationEnv.FixedMent.Ment1Line);
            WriteIni("COMMUNICATION", "fixedcolor1", env.CommunicationEnv.FixedMent.Ment1Color);
            WriteIni("COMMUNICATION", "fixedment2", env.CommunicationEnv.FixedMent.Ment2Line);
            WriteIni("COMMUNICATION", "fixedcolor2", env.CommunicationEnv.FixedMent.Ment2Color);
            WriteIni("COMMUNICATION", "periodment1", env.CommunicationEnv.PeriodMent.Ment1Line);
            WriteIni("COMMUNICATION", "periodment2", env.CommunicationEnv.PeriodMent.Ment2Line);
            WriteIni("COMMUNICATION", "fixedport", env.CommunicationEnv.FixedPort);
            #endregion

            #region 자료 처리
            WriteIni("DataProcess", "LPR1Period_SendData", env.CommunicationEnv.Lpr1Info.LprOpt.Period_SendData);
            WriteIni("DataProcess", "LPR1Period_Lprtrns", env.CommunicationEnv.Lpr1Info.LprOpt.Period_Lprtrns);
            WriteIni("DataProcess", "LPR1Period_Passtrns", env.CommunicationEnv.Lpr1Info.LprOpt.Period_Passtrns);
            WriteIni("DataProcess", "LPR1Period_Fcstay", env.CommunicationEnv.Lpr1Info.LprOpt.Period_Counter);
            WriteIni("DataProcess", "LPR1Period_Fccounttrns", env.CommunicationEnv.Lpr1Info.LprOpt.Period_Gate);
            WriteIni("DataProcess", "LPR1Normal_SendData", env.CommunicationEnv.Lpr1Info.LprOpt.Normal_SendData);
            WriteIni("DataProcess", "LPR1Normal_Lprtrns", env.CommunicationEnv.Lpr1Info.LprOpt.Normal_Lprtrns);
            WriteIni("DataProcess", "LPR1Normal_Tckttrns", env.CommunicationEnv.Lpr1Info.LprOpt.Normal_Tckttrns);
            WriteIni("DataProcess", "LPR1Normal_Fcstay", env.CommunicationEnv.Lpr1Info.LprOpt.Normal_Counter);
            WriteIni("DataProcess", "LPR1Normal_Fccounttrns", env.CommunicationEnv.Lpr1Info.LprOpt.Normal_Gate);
            WriteIni("DataProcess", "LPR2Period_SendData", env.CommunicationEnv.Lpr2Info.LprOpt.Period_SendData);
            WriteIni("DataProcess", "LPR2Period_Lprtrns", env.CommunicationEnv.Lpr2Info.LprOpt.Period_Lprtrns);
            WriteIni("DataProcess", "LPR2Period_Passtrns", env.CommunicationEnv.Lpr2Info.LprOpt.Period_Passtrns);
            WriteIni("DataProcess", "LPR2Period_Fcstay", env.CommunicationEnv.Lpr2Info.LprOpt.Period_Counter);
            WriteIni("DataProcess", "LPR2Period_Fccounttrns", env.CommunicationEnv.Lpr2Info.LprOpt.Period_Gate);
            WriteIni("DataProcess", "LPR2Normal_SendData", env.CommunicationEnv.Lpr2Info.LprOpt.Normal_SendData);
            WriteIni("DataProcess", "LPR2Normal_Lprtrns", env.CommunicationEnv.Lpr2Info.LprOpt.Normal_Lprtrns);
            WriteIni("DataProcess", "LPR2Normal_Tckttrns", env.CommunicationEnv.Lpr2Info.LprOpt.Normal_Tckttrns);
            WriteIni("DataProcess", "LPR2Normal_Fcstay", env.CommunicationEnv.Lpr2Info.LprOpt.Normal_Counter);
            WriteIni("DataProcess", "LPR2Normal_Fccounttrns", env.CommunicationEnv.Lpr2Info.LprOpt.Normal_Gate);
            WriteIni("DataProcess", "ReturnUse", env.CommunicationEnv.ReturnCar.Use);
            WriteIni("DataProcess", "ReturnTerm", env.CommunicationEnv.ReturnCar.Term);
            WriteIni("DataProcess", "ReturnMent", env.CommunicationEnv.ReturnCar.Ment);
            #endregion

            WriteIni("COMMUNICATION", "sendOffice", env.SendOffice.ToString());

            //leess 긴급차량 개방
            WriteIni("COMMON", "emergencyCar", env.EmergencyCar);

            DelayReg.SaveDelay();
        }

        public static UInt32 Elanpr_Initialize(ref UInt32 pEngineID)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_Initialize(ref pEngineID);
            else
                return Elanpr.Elanpr_Initialize(ref pEngineID);
        }

        public static UInt32 Elanpr_RecognizePlate(UInt32 uEngineID, string pathName, ref ELANPRESULT result)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_RecognizePlate(uEngineID, pathName, ref result);
            else
                return Elanpr.Elanpr_RecognizePlate(uEngineID, pathName, ref result);
        }

        public static UInt32 Elanpr_RecognizePlateExt(UInt32 uEngineID, string pathName, Int32 nScalePercent, ref ELANPRESULT result)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_RecognizePlateExt(uEngineID, pathName, nScalePercent, ref result);
            else
                return Elanpr.Elanpr_RecognizePlateExt(uEngineID, pathName, nScalePercent, ref result);
        }

        public static UInt32 Elanpr_RecognizePlateBuffer(UInt32 dwEngineID, byte[] pBufferImage, int nBufferSize, ref ELANPRESULT pResult)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_RecognizePlateBuffer(dwEngineID, pBufferImage, nBufferSize, ref pResult);
            else
                return Elanpr.Elanpr_RecognizePlateBuffer(dwEngineID, pBufferImage, nBufferSize, ref pResult);
        }

        public static UInt32 Elanpr_RecognizePlateStruct(UInt32 uEngineID, ref IMAGE_INFO imgInfo, ref ELANPRESULT result)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_RecognizePlateStruct(uEngineID, ref imgInfo, ref result);
            else
                return Elanpr.Elanpr_RecognizePlateStruct(uEngineID, ref imgInfo, ref result);
        }

        public static UInt32 Elanpr_RecognizePlateStructExt(UInt32 uEngineID, Int32 nScalePercent, ref IMAGE_INFO imgInfo, ref ELANPRESULT result)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_RecognizePlateStructExt(uEngineID, nScalePercent, ref imgInfo, ref result);
            else
                return Elanpr.Elanpr_RecognizePlateStructExt(uEngineID, nScalePercent, ref imgInfo, ref result);
        }

        public static UInt32 Elanpr_GetRecogAccuracyInPercent(UInt32 uEngineID, ref UInt64 pValAccuracyInPercent)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_GetRecogAccuracyInPercent(uEngineID, ref pValAccuracyInPercent);
            else
                return Elanpr.Elanpr_GetRecogAccuracyInPercent(uEngineID, ref pValAccuracyInPercent);
        }

        public static UInt32 Elanpr_FillRecogAccuracyArray(UInt32 uEngineID, ref float pValAccuracy8Rooms)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_FillRecogAccuracyArray(uEngineID, ref pValAccuracy8Rooms);
            else
                return Elanpr.Elanpr_FillRecogAccuracyArray(uEngineID, ref pValAccuracy8Rooms);
        }

        public static UInt32 Elanpr_SetWarpingAngle(UInt32 uEngineID, int iAngleToPull)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_SetWarpingAngle(uEngineID, iAngleToPull);
            else
                return Elanpr.Elanpr_SetWarpingAngle(uEngineID, iAngleToPull);
        }

        public static UInt32 Elanpr_SetPlateLocation(UInt32 uEngineID, Rect rcPlateLocation)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_SetPlateLocation(uEngineID, rcPlateLocation);
            else
                return Elanpr.Elanpr_SetPlateLocation(uEngineID, rcPlateLocation);
        }

        public static UInt32 Elanpr_DoesExistNumberPlate(UInt32 uEngineID, string lpszFileName, int minNumPix, int maxNumPix)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_DoesExistNumberPlate(uEngineID, lpszFileName, minNumPix, maxNumPix);
            else
                return Elanpr.Elanpr_DoesExistNumberPlate(uEngineID, lpszFileName, minNumPix, maxNumPix);
        }

        public static UInt32 Elanpr_DoesExistNumberPlateBuffer(UInt32 uEngineID, byte[] pImageBuffer, int nBufferSize, int minNumPix, int maxNumPix)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_DoesExistNumberPlateBuffer(uEngineID, pImageBuffer, nBufferSize, minNumPix, maxNumPix);
            else
                return Elanpr.Elanpr_DoesExistNumberPlateBuffer(uEngineID, pImageBuffer, nBufferSize, minNumPix, maxNumPix);
        }

        public static UInt32 Elanpr_DoesExistNumberPlateStruct(UInt32 uEngineID, ref IMAGE_INFO pImageBuffer, int minNumPix, int maxNumPix)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_DoesExistNumberPlateStruct(uEngineID, ref pImageBuffer, minNumPix, maxNumPix);
            else
                return Elanpr.Elanpr_DoesExistNumberPlateStruct(uEngineID, ref pImageBuffer, minNumPix, maxNumPix);
        }

        public static UInt32 Elanpr_RetrievePlateCandidates(UInt32 uEngineID, ref ElanprPlateCandidates pPlateCandidates)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_RetrievePlateCandidates(uEngineID, ref pPlateCandidates);
            else
                return Elanpr.Elanpr_RetrievePlateCandidates(uEngineID, ref pPlateCandidates);
        }

        public static UInt32 Elanpr_SetMinMaxNumberPix(UInt32 uEngineID, int minNumberPix, int maxNumberPix)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_SetMinMaxNumberPix(uEngineID, minNumberPix, maxNumberPix);
            else
                return Elanpr.Elanpr_SetMinMaxNumberPix(uEngineID, minNumberPix, maxNumberPix);
        }

        public static UInt32 Elanpr_ReduceNoiseOpt(UInt32 uEngineID, int bReduceNoiseOpt)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_ReduceNoiseOpt(uEngineID, bReduceNoiseOpt);
            else
                return Elanpr.Elanpr_ReduceNoiseOpt(uEngineID, bReduceNoiseOpt);
        }

        public static UInt32 Elanpr_SetRecogQualityOpt(UInt32 uEngineID, int nRecogQualityPercent)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_SetRecogQualityOpt(uEngineID, nRecogQualityPercent);
            else
                return Elanpr.Elanpr_SetRecogQualityOpt(uEngineID, nRecogQualityPercent);
        }

        public static UInt32 Elanpr_RecognizeMultiPlates(UInt32 uEngineID, string lpszFileName, ref int pVehicleCount, ref ELANPRESULT ppResult)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_RecognizeMultiPlates(uEngineID, lpszFileName, ref pVehicleCount, ref ppResult);
            else
                return Elanpr.Elanpr_RecognizeMultiPlates(uEngineID, lpszFileName, ref pVehicleCount, ref ppResult);
        }

        public static UInt32 Elanpr_RecognizeMultiPlatesExt(UInt32 uEngineID, string lpszFileName, int nScalePercent, ref int pVehicleCount, ref ELANPRESULT ppResult)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_RecognizeMultiPlatesExt(uEngineID, lpszFileName, nScalePercent, ref pVehicleCount, ref ppResult);
            else
                return Elanpr.Elanpr_RecognizeMultiPlatesExt(uEngineID, lpszFileName, nScalePercent, ref pVehicleCount, ref ppResult);
        }

        public static UInt32 Elanpr_RecognizeMultiPlatesBuffer(UInt32 uEngineID, byte[] pBufferImage, int nScalePercent, ref int pVehicleCount, ref ELANPRESULT ppResult)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_RecognizeMultiPlatesBuffer(uEngineID, pBufferImage, nScalePercent, ref pVehicleCount, ref ppResult);
            else
                return Elanpr.Elanpr_RecognizeMultiPlatesBuffer(uEngineID, pBufferImage, nScalePercent, ref pVehicleCount, ref ppResult);
        }

        public static UInt32 Elanpr_RecognizeMultiPlatesStruct(UInt32 uEngineID, ref IMAGE_INFO pImageInfo, ref int pVehicleCount, ref ELANPRESULT ppResult)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_RecognizeMultiPlatesStruct(uEngineID, ref pImageInfo, ref pVehicleCount, ref ppResult);
            else
                return Elanpr.Elanpr_RecognizeMultiPlatesStruct(uEngineID, ref pImageInfo, ref pVehicleCount, ref ppResult);
        }

        public static UInt32 Elanpr_Finalize(UInt32 uEngineID)
        {
            if (Environment.Is64BitProcess)
                return Elanpr64.Elanpr_Finalize(uEngineID);
            else
                return Elanpr.Elanpr_Finalize(uEngineID);
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static Rect ConvertRect(Rectangle rt)
        {
            Rect rtn = new Rect();
            rtn.left = rt.X;
            rtn.top = rt.Y;
            rtn.left = rt.Width + rt.X;
            rtn.bottom = rt.Height + rt.Y;
            return rtn;
        }

        public static Rectangle ConvertRectangle(Rect rt)
        {
            //return new Rectangle(rt.left, rt.top, rt.right + rt.left, rt.top + rt.bottom);
            return new Rectangle(rt.left, rt.top, rt.right - rt.left, rt.bottom - rt.top);
        }

        public static Rectangle ConvertRectangle(uint left, uint top, uint right, uint bottom)
        {
            //return new Rectangle((int)left, (int)top, (int)right + (int)left, (int)top + (int)bottom);
            return new Rectangle((int)left, (int)top, (int)right - (int)left, (int)bottom - (int)top);
        }

        public static String GetSavePath(int MsgType)
        {
            String path = String.Empty;
            switch (MsgType)
            {
                case (int)ClsStructure.SockFormat.Nexpa:
                    path = string.Format(@"{0}\{1}\{2}", DateTime.Now.Year.ToString().PadLeft(4, '0')
                    , DateTime.Now.Month.ToString().PadLeft(2, '0')
                    , DateTime.Now.Day.ToString().PadLeft(2, '0'));
                    break;
                default:
                    path = DateTime.Now.ToString("yyyyMMdd");
                    break;
            }
            return path;
        }

        public static void SaveImage(String Source, String Destination, Rectangle rcPlateLoc)
        {
            try
            {
                var image = Image.FromFile(Source);
                PropertyItem pItem = image.PropertyItems[0];
                pItem.Id = 0x9286;
                pItem.Type = 2;// string
                pItem.Value = Encoding.Default.GetBytes(string.Format("{0},{1},{2},{3}", rcPlateLoc.X, rcPlateLoc.Y, rcPlateLoc.Width, rcPlateLoc.Height));
                Util.Logger.Log(string.Format("번호판 판독 좌표 {0},{1},{2},{3}", rcPlateLoc.X, rcPlateLoc.Y, rcPlateLoc.Width, rcPlateLoc.Height));
                pItem.Len = pItem.Value.Length;
                image.SetPropertyItem(pItem);
                image.Save(Destination);
            }
            catch (FileNotFoundException e)
            {
                Util.Logger.Log(string.Format("SaveImage {0}", e.Message));
            }
            catch (Exception)
            {
                File.Copy(Source, Destination);
            }
        }

        public static void SaveImage(String Source, String Destination, string PlateLoc, string Exposure, string PlateNo)
        {
            try
            {
                if (!File.Exists(Source))
                {
                    Util.Logger.Log(string.Format("소스 이미지 없음"));
                    return;
                }
                //using (Image image = Image.FromFile(Directory.GetCurrentDirectory() + "\\" + Source))
                //{
                //    PropertyItem pItem = image.PropertyItems[0];
                //    pItem.Id = 0x9286;
                //    pItem.Type = 2;// string
                //    pItem.Value = Encoding.Unicode.GetBytes(string.Format("{0} {1} {2} {3}", (PlateLoc + Exposure).Length + 1, PlateLoc, Exposure, PlateNo));
                //    Util.Logger.Log(string.Format("번호판 판독 좌표 {0}", PlateLoc));
                //    pItem.Len = pItem.Value.Length;
                //    image.SetPropertyItem(pItem);
                //    image.Save(Destination);
                //}
                Util.Logger.Log(string.Format("번호판 판독 좌표 {0} {1}", Source, PlateLoc));
                System.Drawing.Image originalImage = System.Drawing.Image.FromFile(Source);

                // Get the list of existing PropertyItems. i.e. the metadata
                PropertyItem[] properties = originalImage.PropertyItems;

                // Create a bitmap image to assign attributes and do whatever else..
                Bitmap bmpImage = new Bitmap((Bitmap)originalImage);

                // Don't need this anymore
                originalImage.Dispose();

                // Get / setup a PropertyItem
                PropertyItem item = properties[0]; // We have to copy an existing one since no constructor exists

                // This will assign "Joe Doe" to the "Authors" metadata field
                string sTmp = string.Format("{0} {1} {2} {3} ", (PlateLoc + Exposure).Length + 1, PlateLoc, Exposure, PlateNo); // The X will be replaced with a null.  String must be null terminated.
                Util.Logger.Log(string.Format("메타 데이터 기록 {0} {1} {2} {3} ", (PlateLoc + Exposure).Length + 1, PlateLoc, Exposure, PlateNo));
                var itemData = System.Text.Encoding.Unicode.GetBytes(sTmp);
                itemData[itemData.Length - 1] = 0;// Strings must be null terminated or they will run together
                itemData = System.Text.Encoding.UTF8.GetBytes(sTmp);
                itemData[itemData.Length - 1] = 0; // Strings must be null terminated or they will run together
                item.Type = 2; //String (ASCII)
                item.Id = 305; // Program Name, 305 is mapped to the "Program Name" field
                item.Len = itemData.Length;
                item.Value = itemData;
                bmpImage.SetPropertyItem(item);

                // Save the image
                bmpImage.Save(Destination, System.Drawing.Imaging.ImageFormat.Jpeg);

                //Clean up
                bmpImage.Dispose();
                Util.Logger.Log(string.Format("이미지 저장 완료 {0}", Destination));
            }
            catch (Exception SaveImage_Error)
            {
                Util.Logger.Log(string.Format("SaveImage_Error 이미지 저장 재시도 {0}", SaveImage_Error.Message));
                try
                {
                    File.Copy(Source, Destination);
                }
                catch (Exception err)
                {
                    Util.Logger.Log(string.Format("Copy Error {0}", err.Message));
                }

            }
        }

        private static ImageCodecInfo GetEncodeInfo(string mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; j++)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        public static string GetMetaData(string ImagePath)
        {
            System.Drawing.Image theImage = new Bitmap(ImagePath);

            System.Drawing.Imaging.PropertyItem[] propItems = theImage.PropertyItems;
            string value = string.Empty;
            foreach (System.Drawing.Imaging.PropertyItem items in propItems)
            {
                System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

                if (items.Id == 305)
                    value = encoding.GetString(items.Value);
            }
            theImage.Dispose();
            return value;
        }

        public static string MagicCarnum()
        {
            string CarNum = string.Empty;
            Random r = new Random();
            string[] MName = new string[] { "가", "나", "다", "라", "마", "거", "너", "더", "러", "머", "버", "서", "어", "저", "고", "노", "도", "로", "모", "보", "소", "오", "조", "구", "누", "두", "루", "무", "부", "수", "우", "주", "바", "사", "아", "자", "하", "허", "호", "배" };
            int rnum = r.Next(0, 9999);
            int lnum = r.Next(0, 99);
            int nnum = r.Next(0, MName.Length - 1);
            string MNum = MName[nnum];
            CarNum = string.Format("{0:00}{1}{2:0000}", lnum, MNum, rnum);

            return CarNum;
        }

        public static void WriteMetaData(string Fpath, string plate, string exposure)
        {
            using (Image file = Image.FromFile(Fpath))
            {
                PropertyItem item = file.PropertyItems[0];
                item.Id = 0x9286;
                item.Type = 2;// string
                item.Value = Encoding.Default.GetBytes(plate);
                item.Len = item.Value.Length;
                file.SetPropertyItem(item);
                item.Id = 0x9287;
                item.Type = 2;// string
                item.Value = Encoding.Default.GetBytes(exposure);
                item.Len = item.Value.Length;
                file.SetPropertyItem(item);

                file.Save(Fpath.Replace(".jpg", "1.jpg"), ImageFormat.Jpeg);
            }
            File.Delete(Fpath);
            File.Move(Fpath.Replace(".jpg", "1.jpg"), Fpath);
        }

        public static Rectangle ReadMetadata(string Fpath)
        {
            string result = string.Empty;
            Rectangle rect = new Rectangle();
            using (Image file = Image.FromFile(Fpath))
            {
                foreach (var item in file.PropertyItems)
                {
                    if (item.Id.Equals(0x9286))
                    {
                        result = string.Empty;
                        Console.WriteLine(item.Id);
                        for (int i = 0; i < item.Value.Length; i++)
                        {
                            if (item.Value[i] >= 48 && item.Value[i] <= 57 || item.Value[i].Equals(44))
                                result += (char)item.Value[i];
                        }
                    }
                }
            }

            if (!result.Equals(string.Empty) && result.IndexOf(',') > -1)
            {
                string[] sp = result.Split(',');
                if (sp.Length >= 4)
                {
                    int x = 0; int y = 0; int w = 0; int h = 0;
                    int.TryParse(sp[0], out x);
                    int.TryParse(sp[1], out y);
                    int.TryParse(sp[2], out w);
                    int.TryParse(sp[3], out h);
                    rect = new Rectangle(x, y, w, h);
                }
            }
            return rect;
        }

        public static int GetColor8Int(String Color)
        {
            int rtn = 1;
            try
            {
                switch (Color)
                {
                    case "":
                        break;
                    default:
                        rtn = (int)Enum.Parse(typeof(ClsStructure.Color8), Color);
                        break;
                }
            }
            catch (Exception)
            { }
            return rtn;
        }

        public static int GetColor3Int(String Color)
        {
            int rtn = 1;
            try
            {
                switch (Color)
                {
                    case "":
                        break;
                    default:
                        rtn = (int)Enum.Parse(typeof(ClsStructure.Color3), Color);
                        break;
                }
            }
            catch (Exception)
            { }
            return rtn;
        }

        public static uint GetAmanoColor3uInt(String Color)
        {
            int rtn = 1;
            try
            {
                switch (Color)
                {
                    case "":
                        break;
                    default:
                        rtn = (int)Enum.Parse(typeof(ClsStructure.AmanoColor3), Color);
                        break;
                }
            }
            catch (Exception e)
            { }
            return (uint)rtn;
        }

        public static string MakeTransMessage(int SockectFormat, string ChName, string CarNo, string Path, string Fname, DateTime MsgTime)
        {
            string LprString = string.Empty;
            switch (SockectFormat)
            {
                case (int)ClsStructure.SockFormat.Kukje:
                    //LprString = "!"
                    //    + ChName
                    //    + "#"
                    //    + CarNo
                    //    + "#"
                    //    + Fname.Trim()
                    //    + "#"
                    //    + MsgTime.ToString("yyyyMMdd")
                    //    + "#"
                    //    + MsgTime.ToString("HHmmss");
                    LprString = string.Format("!{0}#{1}#{2}#{3}#{4}", ChName, CarNo, Fname, MsgTime.ToString("yyyyMMdd"), MsgTime.ToString("HHmmss"));
                    break;
                case (int)ClsStructure.SockFormat.Amano:
                    //CH01#21고0021#20160411\\CH01_21고0021_20160411142924.jpg
                    //CH01#66어3219#20170330\CH01_66어3219_20170330084101.jpg
                    //LprString = ChName
                    //    + "#"
                    //    + CarNo
                    //    + "#"
                    //    + MsgTime.ToString("yyyyMMdd")
                    //    + @"\"
                    //    + Fname;
                    LprString = string.Format("{0}#{1}#{2}\\{3}", ChName, CarNo, MsgTime.ToString("yyyyMMdd"), Fname);
                    break;
                case (int)ClsStructure.SockFormat.Nexpa:
                    //CH02#86거8654#\2016\04\06\CH02_20160406000731_86거8654.jpg
                    //CH2#대구3노8592#\2009\10\22\CH2_20091022095012_대구3노8592.jpg
                    //CH1#XXXXXX아3560#\2009\10\22\CH1_20091022095552_XXXXXX아3560.jpg
                    //CH1#0000000000#\2009\10\22\CH1_20091022100348_0000000000.jpg
                    //CH1#000000000000#2015\08\12CH1_20150812175342_000000000000.jpg
                    //CH1#0000000000#\2015\08\12\CH1_20150812182923_0000000000.jpg
                    if (CarNo.Equals("No_Detection"))
                        CarNo = "0000000000";
                    else 
                    {
                        int i = 0;
                        if (int.TryParse(CarNo, out i))
                            CarNo = Util.Common.Mid("XXXXXXXXXXXX", 1, 12 - CarNo.Length) + CarNo;
                        else if (CarNo.IndexOf('X') > -1)
                            CarNo = "XXXXXXXXXXXX".Substring(0, 12 - Util.Common.GetStringLength(CarNo)) + CarNo;
                    }
                    LprString = string.Format("{0}#{1}#\\{2}\\{3,2:00}\\{4,2:00}\\{0}_{5}_{6}.jpg", ChName, CarNo, MsgTime.Year, MsgTime.Month, MsgTime.Day, MsgTime.ToString("yyyyMMddHHmmss"), CarNo);
                    break;
                case (int)ClsStructure.SockFormat.AmanoOld:
                //CH03#12부9137#\20140722\CH03_20140722001826_12부9137.jpg
                    LprString = string.Format("{0}#{1}#\\{2}\\{3}", ChName, CarNo, MsgTime.ToString("yyyyMMdd"), Fname);
                    break;
            }
            return LprString;
        }
    }
}
