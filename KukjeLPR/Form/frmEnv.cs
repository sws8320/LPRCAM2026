using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using KyungsinLPR;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Net;
using System.Security.Cryptography;
using System.Windows.Interop;

namespace KyungsinLPR
{
    public partial class frmEnv : Form
    {
        public ClsStructure.EnvStruct env;        
        private IPCamera Cam1;
        private IPCamera Cam2;
        //leess iNova2추가
        private iNova2.IPCamera Cam1_iNova2;
        private iNova2.IPCamera Cam2_iNova2;
        private clsFunction func = new clsFunction();
        private SSDPUtil ssdpSession = new SSDPUtil();

        public clsSerialPort SerialDev = null;

        frmExposureCheck frm = null;

        //차단기 개방 제외 정기권 그룹
        private int exceptGrpNo;

        public frmEnv(ClsStructure.EnvStruct _env, IPCamera _cam1, IPCamera _cam2, iNova2.IPCamera _cam1_iNova2, iNova2.IPCamera _cam2_iNova2)
        {
            InitializeComponent();
            env = _env;
            Cam1 = _cam1;
            Cam2 = _cam2;
            //leess iNova2추가
            Cam1_iNova2 = _cam1_iNova2;
            Cam2_iNova2 = _cam2_iNova2;
            if(Environment.Is64BitProcess)
            {
                rdbCore.Visible = true;
                panel1.Visible = true;
            }
            lstLprList.View = View.Details;
            lstLprList.FullRowSelect = true;
            lstLprList.GridLines = true;
            lstLprList.MultiSelect = false;
            lstLprList.Columns.Add("No", 50);
            lstLprList.Columns.Add("CHNO", 50);
            lstLprList.Columns.Add("IP", 150);
            lstLprList.Columns.Add("Port", 50);
            lstLprList.Columns.Add("Type", 50);
        }      

        public string folder()
        {
            string rtn = string.Empty;
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            DialogResult ret = dialog.ShowDialog();
            if (ret.Equals(DialogResult.OK))
                rtn = dialog.SelectedPath;
            return rtn;
        }

        private void btnImagePath_Click(object sender, EventArgs e)
        {
            TxtImagePath.Text = folder();
        }

        private void listCamIPlist_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                if (groupBox1.Enabled)
                    txtCamIp.Text = listCamIPlist.SelectedItem.ToString();// listCamIPlist.Items[listCamIPlist.SelectedIndex].ToString();
            }
            catch { }
        }

        private void frmEnv_Load(object sender, EventArgs e)
        {
            cmbDioType.DataSource = Enum.GetValues(typeof(ClsStructure.DeviceList));
            //cmbMessageType.DataSource = Enum.GetValues(typeof(ClsStructure.MessageType));
            cmbBoardType.DataSource = Enum.GetValues(typeof(ClsStructure.DeviceType));
            cmbImageProcType.DataSource = Enum.GetValues(typeof(ClsStructure.ImageProceType));
            CmbBracket1.DataSource = Enum.GetValues(typeof(ClsStructure.Bracket));
            CmbBracket2.DataSource = Enum.GetValues(typeof(ClsStructure.Bracket));
            CmbBracket3.DataSource = Enum.GetValues(typeof(ClsStructure.Bracket));
            CmbLPRType1.DataSource = Enum.GetValues(typeof(ClsStructure.LprDevice));
            CmbLPRType2.DataSource = Enum.GetValues(typeof(ClsStructure.LprDevice));
            CmbLPRInOut1.DataSource = Enum.GetValues(typeof(ClsStructure.InoutType));
            CmbLPRInOut2.DataSource = Enum.GetValues(typeof(ClsStructure.InoutType));
            //CmbLprOutType.DataSource = Enum.GetValues(typeof(ClsStructure.CalculatorType));
            CmbDisplay1Type.DataSource = Enum.GetValues(typeof(ClsStructure.DisPlayType));
            CmbDisplay2Type.DataSource = Enum.GetValues(typeof(ClsStructure.DisPlayType));
            string[] myPort = System.IO.Ports.SerialPort.GetPortNames();
            cmbDioPort.Items.AddRange(myPort);
            cmbDisplay1Port.Items.AddRange(myPort);
            cmbDisplay2Port.Items.AddRange(myPort);
            ssdpSession.SetupSSDPSessions();
            ssdpSession.IpUpdated += IpUpdated;
            CarRegTypeGridInit();
            setEnv();
            gbAuthentication.Visible = !env.CommonEnv.Authentication;
            for (int i = 1; i <= 10; i++)
            {
                cmbChName.Items.Add(string.Format("CH{0:D2}", i));
            }
            //chkLprEntUse.Checked = LprRelay.USE;
            //txtLprEntIp.Text = LprRelay.IP;
            //txtLprEntPort.Text = LprRelay.PORT.ToString();
            //cmbLprEntType.Items.Add("SERVER");
            //cmbLprEntType.Items.Add("CLIENT");
            //cmbLprEntType.Text = LprRelay.TYPE;
            chkRegDelayUse.Checked = DelayReg.Delay;
            txtRegDelayTerm.Text = DelayReg.DelayTerm.ToString();
            chkDuplicateUse.Checked = DelayReg.Duplicate;
            txtDuplicateTerm.Text = DelayReg.Duplicate_Term.ToString();

            chkBusinessUse.Checked = clsBusinessCar.UseBusinessCar;
            chkBusinessEntGateOpen.Checked = clsBusinessCar.UseEntranceGateOpen;
            chkBusinessEntSendData.Checked = clsBusinessCar.UseEntranceSocketDataSend;
            chkBusinessExitGateOpen.Checked = clsBusinessCar.UseExitGateOpen;
            chkBusinessExitSendData.Checked = clsBusinessCar.UseExitSocketDataSend;
            txtBusinessDisplayMent.Text = clsBusinessCar.DisPlayLineMent;
        }

        private void setEnv()
        {
            #region 기본설정
            chkTestMod.Checked = env.TestMode;
            txtServer.Text = env.CommonEnv.DBInfo.Ip;
            txtID.Text = env.CommonEnv.DBInfo.Id;
            txtPW.Text = env.CommonEnv.DBInfo.Pw;
            txtMDB.Text = env.CommonEnv.DBInfo.MstDB;
            txtTDB.Text = env.CommonEnv.DBInfo.TrnsDb;

            txtParkNo.Text = env.CommunicationEnv.ParkInfo.No.ToString();
            txtParkExtNo.Text = env.CommunicationEnv.ParkInfo.Ext_No.ToString();
            txtParkPCNo.Text = env.CommunicationEnv.ParkInfo.Client_No.ToString();

            switch (env.CommunicationEnv.RegCorrection)
            {
                case 1:
                    chkNumberOnly4digit.Checked = true;
                    break;
                case 2:
                    chkNumberOnly6digit.Checked = true;
                    break;
            }

            ckbImageUse.Checked = env.CommunicationEnv.ImageSave.Use;
            txtComSavePath.Text = env.CommunicationEnv.ImageSave.SavePath;

            switch (env.StartType)
            {
                case 0:
                    rdStartCam.Checked = true;
                    break;
                case 1:
                    rdStartCom.Checked = true;
                    break;
                case 2:
                    rdStartBoth.Checked = true;
                    break;
            }

            lblFullControl.Text = string.Format("만차제어 : {0}활성", FullSpaceControl.Use? "": "비");
            chkFullPeriodControl.Checked = FullSpaceControl.Period;
            chkFullReaseGateOpen.Checked = FullSpaceControl.EntGateOpen;
            chkManualFullControl.Checked = FullSpaceControl.Manual;
            grpFullControl.Enabled = FullSpaceControl.Use;

            chkGetMst.Checked = GetMasterInfo.Use;
            txtGetMstPath.Text = GetMasterInfo.SharePath;
            txtGetMstTerm.Text = GetMasterInfo.Term.ToString();
            #endregion

            #region 부제 설정
            chkNoDrivingUse.Checked = NoDriving.Use;
            switch (NoDriving.Option)
            {
                case NoDrive.Type2:
                    rdbNoDriving2.Checked = true;
                    break;
                case NoDrive.Type5:
                    rdbNoDriving5.Checked = true;
                    break;
                case NoDrive.Type10:
                    rdbNoDriving10.Checked = true;
                    break;
                case NoDrive.TypeDayOfWeek:
                    rdbNoDriving67.Checked = true;
                    break;
            }
            chkNoDrivingLpr.Checked = NoDriving.WriteLpr;
            chkNoDrivingDisPlay.Checked = NoDriving.DisPlay;
            chkNoDrivingException.Checked = NoDriving.Exception;
            txtNoDriveMent1.Text = NoDriving.Ment1;
            txtNoDriveMent2.Text = NoDriving.Ment2;
            if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name == ClsStructure.DisPlayType.Color8.ToString())
            {
                cmbNoDriveColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                cmbNoDriveColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
            }
            else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name == ClsStructure.DisPlayType.Color3.ToString())
            {
                cmbNoDriveColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                cmbNoDriveColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
            }
            else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name == ClsStructure.DisPlayType.AmanoSmall.ToString())
            {
                cmbNoDriveColor1.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                cmbNoDriveColor2.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
            }

            cmbNoDriveColor1.Text = NoDriving.Color1;
            cmbNoDriveColor2.Text = NoDriving.Color2;
            #endregion
            //#region 카메라 기본 값 설정
            //if (env.CameraEnv.IPCamera1Info.ChName != null)
            //    cmbChanelName.Items.Add(env.CameraEnv.IPCamera1Info.ChName);
            //if (env.CameraEnv.IPCamera2Info.ChName != null)
            //    cmbChanelName.Items.Add(env.CameraEnv.IPCamera2Info.ChName);
            //#endregion
            chkTestMod.Checked = env.TestMode;
            #region LPR설정
            #region 이미지 저장
            TxtImagePath.Text = env.CameraEnv.ImageSave.SavePath;
            txtImageTerm.Text = env.CameraEnv.ImageSave.SaveTerm.ToString();
            chkEtcImageSave.Checked = env.CameraEnv.ImageSave.EtcSave;
            txtEtcImagePath.Text = env.CameraEnv.ImageSave.EtcPath;
            #endregion
            #region 인식모듈
            switch (env.CameraEnv.RegModule)
            {
                case (int)ClsStructure.RegModule.Elwox:
                    rdElwox.Checked = true;
                    break;
                case (int)ClsStructure.RegModule.Ngis:
                    rdNgis.Checked = true;
                    break;
                case (int)ClsStructure.RegModule.CoreLogic:
                    rdbCore.Checked = true;
                    break;
            }

            switch (env.CameraEnv.CoreType)
            {
                case (int)ClsStructure.CoreType.CPU:
                    rdbCpu.Checked = true;
                    break;
                case (int)ClsStructure.CoreType.GPU:
                    rdbGpu.Checked = true;
                    break;
                case (int)ClsStructure.CoreType.MyriadVPU:
                    rdbMyriad.Checked = true;
                    break;
            }

            if (env.CameraEnv.CoreCountry == CoreLogic.KOR)
                rdbKor.Checked = true;
            else
                rdbTha.Checked = true;

            chkRegCarType.Checked = env.CameraEnv.bRegCarType;

            foreach (ClsStructure.SmallCarRate item in env.CameraEnv.RegCarRate)
            {
                dataGridView1.Rows.Add(item.CarType, item.Rate.ToString());
            }

            cmbImageProcType.SelectedItem = env.CameraEnv.PlateArea.Equals(true) ? ClsStructure.ImageProceType.번호판확인 : ClsStructure.ImageProceType.이미지자르기;
            if (env.CommunicationEnv.Nodetection_Open)
                rdGateOpen.Checked = true;
            else
                rdNotGateOpen.Checked = true;
            #endregion

            #region LPR 장비 설정
            cmbCameraType.SelectedIndex = env.CameraEnv.iNovaType - 1;//leess iNova2추가
            ChkLPRUse1.Checked = env.CommunicationEnv.Lpr1Info.Use;
            txtEqpmNo1.Text = env.CommunicationEnv.Lpr1Info.EqpmNo.ToString();
            txtLPRNo1.Text = env.CommunicationEnv.Lpr1Info.ChNo;
            txtLPRName1.Text = env.CommunicationEnv.Lpr1Info.Name;
            CmbLPRType1.SelectedIndex = env.CommunicationEnv.Lpr1Info.DevType;
            CmbLPRInOut1.SelectedIndex = env.CommunicationEnv.Lpr1Info.InOutType;
            //ChkFreePass1.Checked = env.CommunicationEnv.Lpr1Info.FreePass;
            //chkFreePassGateOpen1.Checked = env.CommunicationEnv.Lpr1Info.FreePassGateOpen;
            txtLPRInfoIP1.Text = env.CommunicationEnv.Lpr1Info.SockInfo.IP;
            txtLPRInfoPort1.Text = env.CommunicationEnv.Lpr1Info.SockInfo.Port.ToString();
            txtLPRInfoPath1.Text = env.CommunicationEnv.Lpr1Info.ImagePath;

            ChkLPRUse2.Checked = env.CommunicationEnv.Lpr2Info.Use;
            txtEqpmNo2.Text = env.CommunicationEnv.Lpr2Info.EqpmNo.ToString();
            txtLPRNo2.Text = env.CommunicationEnv.Lpr2Info.ChNo;
            txtLPRName2.Text = env.CommunicationEnv.Lpr2Info.Name;
            CmbLPRType2.SelectedIndex = env.CommunicationEnv.Lpr2Info.DevType;
            CmbLPRInOut2.SelectedIndex = env.CommunicationEnv.Lpr2Info.InOutType;
            //ChkFreePass2.Checked = env.CommunicationEnv.Lpr2Info.FreePass;
            //chkFreePassGateOpen2.Checked = env.CommunicationEnv.Lpr2Info.FreePassGateOpen;
            txtLPRInfoIP2.Text = env.CommunicationEnv.Lpr2Info.SockInfo.IP;
            txtLPRInfoPort2.Text = env.CommunicationEnv.Lpr2Info.SockInfo.Port.ToString();
            txtLPRInfoPath2.Text = env.CommunicationEnv.Lpr2Info.ImagePath;
            #endregion
            #endregion

            #region 소켓통신
            switch (env.CameraEnv.SockDataFormat)
            {
                case (int)ClsStructure.SockFormat.Kukje:
                    rdK.Checked = true;
                    break;
                case (int)ClsStructure.SockFormat.Amano:
                    rdA.Checked = true;
                    break;
                case (int)ClsStructure.SockFormat.Nexpa:
                    rdN.Checked = true;
                    break;
                case (int)ClsStructure.SockFormat.AmanoOld:
                    rdbOldAmano.Checked = true;
                    break;
            }

            ChkNotiUse.Checked = env.CommunicationEnv.ClientTarget[0].Use;
            txtNotiIP.Text = env.CommunicationEnv.ClientTarget[0].IP;
            txtNotiPort.Text = env.CommunicationEnv.ClientTarget[0].Port.ToString();
            chkNotiType.Checked = env.CommunicationEnv.ClientTarget[0].Type == 1;

            ChkLprOutServer.Checked = env.CommunicationEnv.ClientTarget[1].Use;
            //CmbLprOutType.SelectedItem = env.CommunicationEnv.ClientTarget[1].Type;
            //txtLprOutIp.Text = env.CommunicationEnv.ClientTarget[1].IP;
            txtLprOutPort.Text = env.CommunicationEnv.ClientTarget[1].Port.ToString();

            ChkDisplayRelayUse.Checked = env.CommunicationEnv.ClientTarget[2].Use;
            CmbDisplayRelayNo.Text = env.CommunicationEnv.ClientTarget[2].Type.ToString();
            txtDisplayRelayIp.Text = env.CommunicationEnv.ClientTarget[2].IP;
            txtDisplayRelayPort.Text = env.CommunicationEnv.ClientTarget[2].Port.ToString();

            ChkStoneUse.Checked = env.CommunicationEnv.ClientTarget[3].Use;
            txtStoneIp.Text = env.CommunicationEnv.ClientTarget[3].IP;
            txtStonePort.Text = env.CommunicationEnv.ClientTarget[3].Port.ToString();

            chkLprEntUse.Checked = env.CommunicationEnv.ClientTarget[4].Use;
            //cmbLprEntType.SelectedItem = env.CommunicationEnv.ClientTarget[4].Type;
            //txtLprEntIp.Text = env.CommunicationEnv.ClientTarget[4].IP;
            txtLprEntPort.Text = env.CommunicationEnv.ClientTarget[4].Port.ToString();

            chkCam1SendStxEtx.Checked = env.CameraEnv.IPCamera1Info.SendStxEtx;
            chkCam2SendStxEtx.Checked = env.CameraEnv.IPCamera2Info.SendStxEtx;
            #endregion

            #region 차단기 설정
            cmbDioPort.SelectedItem = env.CommonEnv.Dio.DioSetting.SerialPort;
            txtDioSetting.Text = env.CommonEnv.Dio.DioSetting.Setting;
            cmbDioType.Text = env.CommonEnv.Dio.DioSetting.Dev_Type_Name;
            cmbBoardType.SelectedItem = env.CommonEnv.Dio.DioSetting.Type.Equals(true) ? ClsStructure.DeviceType.이벤트 : ClsStructure.DeviceType.리얼;

            if (env.CommonEnv.Dio.DioOutPut[0].Use && env.CommonEnv.Dio.DioOutPut[0].Keep < 500)
                env.CommonEnv.Dio.DioOutPut[0].Keep = 500;
            if (env.CommonEnv.Dio.DioOutPut[1].Use && env.CommonEnv.Dio.DioOutPut[1].Keep < 500)
                env.CommonEnv.Dio.DioOutPut[1].Keep = 500;

            ChkGate1Use.Checked = env.CommonEnv.Dio.DioOutPut[0].Use;
            CmbGate1Port.SelectedItem = env.CommonEnv.Dio.DioOutPut[0].Port.ToString();
            txtGate1PortDelay.Text = env.CommonEnv.Dio.DioOutPut[0].Delay.ToString();
            txtGate1PortKeep.Text = env.CommonEnv.Dio.DioOutPut[0].Keep.ToString();
            CmbGate1AddPort.SelectedItem = env.CommonEnv.Dio.DioOutPut[0].AddPort.ToString();
            txtGate1AddPortDelay.Text = env.CommonEnv.Dio.DioOutPut[0].AddDelay.ToString();
            txtGate1AddPortKeep.Text = env.CommonEnv.Dio.DioOutPut[0].AddKeep.ToString();

            ChkGate2Use.Checked = env.CommonEnv.Dio.DioOutPut[1].Use;
            CmbGate2Port.SelectedItem = env.CommonEnv.Dio.DioOutPut[1].Port.ToString();
            txtGate2PortDelay.Text = env.CommonEnv.Dio.DioOutPut[1].Delay.ToString();
            txtGate2PortKeep.Text = env.CommonEnv.Dio.DioOutPut[1].Keep.ToString();
            CmbGate2AddPort.SelectedItem = env.CommonEnv.Dio.DioOutPut[1].AddPort.ToString();
            txtGate2AddPortDelay.Text = env.CommonEnv.Dio.DioOutPut[1].AddDelay.ToString();
            txtGate2AddPortKeep.Text = env.CommonEnv.Dio.DioOutPut[1].AddKeep.ToString();

            chkIsolateUse.Checked = env.CommonEnv.Dio.IsolatePort.Out.Use;
            cmbIsolateInPort.Text = env.CommonEnv.Dio.IsolatePort.In.LoopPort.ToString();
            cmbIsolateOutport.Text = env.CommonEnv.Dio.IsolatePort.Out.Port.ToString();
            txtIsolateDelay.Text = env.CommonEnv.Dio.IsolatePort.Out.Delay.ToString();
            txtIsolateKeep.Text = env.CommonEnv.Dio.IsolatePort.Out.Keep.ToString();
            cmbIsolatePortAdd.Text = env.CommonEnv.Dio.IsolatePort.Out.AddPort.ToString();
            txtIsolateAddDelay.Text = env.CommonEnv.Dio.IsolatePort.Out.AddDelay.ToString();
            txtIsolateAddKeep.Text = env.CommonEnv.Dio.IsolatePort.Out.AddKeep.ToString();
            #endregion

            #region 전광판
            ChkDisplay1Use.Checked = env.CommunicationEnv.DisPlay[0].Use;
            cmbDisplay1Port.Text = env.CommunicationEnv.DisPlay[0].Com.SerialPort;
            txtDisplay1Setting.Text = env.CommunicationEnv.DisPlay[0].Com.Setting;
            CmbDisplay1Type.Text = env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name;
            txtDisplay1Text1.Text = env.CommunicationEnv.DisPlay[0].Ment.Ment1Line;
            CmbDisplayText1Color1.Text = env.CommunicationEnv.DisPlay[0].Ment.Ment1Color;
            txtDisplay1Text2.Text = env.CommunicationEnv.DisPlay[0].Ment.Ment2Line;
            CmbDisplayText1Color2.Text = env.CommunicationEnv.DisPlay[0].Ment.Ment2Color;
            txtNormalCar1.Text = env.CommunicationEnv.DisPlay[0].NormalCar;
            CmbDisplayTextNormal1Color1.Text = env.CommunicationEnv.DisPlay[0].Normal1Color;
            CmbDisplayTextNormal1Color2.Text = env.CommunicationEnv.DisPlay[0].Normal2Color;
            txtPeriodCar1.Text = env.CommunicationEnv.DisPlay[0].PeriodCar;
            CmbDisplayTextPeriod1Color1.Text = env.CommunicationEnv.DisPlay[0].Period1Color;
            CmbDisplayTextPeriod1Color2.Text = env.CommunicationEnv.DisPlay[0].Period2Color;
            chkDisplay1NetUse.Checked = env.CommunicationEnv.DisPlay[0].Net.Use;
            txtDisplay1NetIp.Text = env.CommunicationEnv.DisPlay[0].Net.IP;
            txtDisplay1NetPort.Text = env.CommunicationEnv.DisPlay[0].Net.Port.ToString();

            chkUseFixedText1.Checked = env.CommunicationEnv.DisPlay[0].UseFiex;

            ChkDisplay2Use.Checked = env.CommunicationEnv.DisPlay[1].Use;
            cmbDisplay2Port.Text = env.CommunicationEnv.DisPlay[1].Com.SerialPort;
            txtDisplay2Setting.Text = env.CommunicationEnv.DisPlay[1].Com.Setting;
            CmbDisplay2Type.Text = env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name;
            txtDisplay2Text1.Text = env.CommunicationEnv.DisPlay[1].Ment.Ment1Line;
            CmbDisplayText2Color1.Text = env.CommunicationEnv.DisPlay[1].Ment.Ment1Color;
            txtDisplay2Text2.Text = env.CommunicationEnv.DisPlay[1].Ment.Ment2Line;
            CmbDisplayText2Color2.Text = env.CommunicationEnv.DisPlay[1].Ment.Ment2Color;
            txtNormalCar2.Text = env.CommunicationEnv.DisPlay[1].NormalCar;
            CmbDisplayTextNormal2Color1.Text = env.CommunicationEnv.DisPlay[1].Normal1Color;
            CmbDisplayTextNormal2Color2.Text = env.CommunicationEnv.DisPlay[1].Normal2Color;
            txtPeriodCar2.Text = env.CommunicationEnv.DisPlay[1].PeriodCar;
            CmbDisplayTextPeriod2Color1.Text = env.CommunicationEnv.DisPlay[1].Period1Color;
            CmbDisplayTextPeriod2Color2.Text = env.CommunicationEnv.DisPlay[1].Period2Color;
            chkUseFixedText2.Checked = env.CommunicationEnv.DisPlay[1].UseFiex;
            chkDisplay2NetUse.Checked = env.CommunicationEnv.DisPlay[1].Net.Use;
            txtDisplay2NetIp.Text = env.CommunicationEnv.DisPlay[1].Net.IP;
            txtDisplay2NetPort.Text = env.CommunicationEnv.DisPlay[1].Net.Port.ToString();

            //Fixed Ment
            txtFixedMent1.Text = env.CommunicationEnv.FixedMent.Ment1Line;
            cmbFixedColor1.Text = env.CommunicationEnv.FixedMent.Ment1Color;
            txtFixedMent2.Text = env.CommunicationEnv.FixedMent.Ment2Line;
            cmbFixedColor2.Text = env.CommunicationEnv.FixedMent.Ment2Color;
            cmbFixedPort.Text = env.CommunicationEnv.FixedPort.ToString();

            txtStop.Text = env.CommunicationEnv.PeriodMent.Ment1Line;
            txtOver.Text = env.CommunicationEnv.PeriodMent.Ment2Line;
            #endregion

            #region 자료처리
            chkPCarEntSend.Checked = env.CommunicationEnv.Lpr1Info.LprOpt.Period_SendData;
            chkPCarEntLprtrns.Checked = env.CommunicationEnv.Lpr1Info.LprOpt.Period_Lprtrns;
            chkPCarEntPasstrns.Checked = env.CommunicationEnv.Lpr1Info.LprOpt.Period_Passtrns;
            chkPCarEntCountting.Checked = env.CommunicationEnv.Lpr1Info.LprOpt.Period_Counter;
            chkPCarEntGate.Checked = env.CommunicationEnv.Lpr1Info.LprOpt.Period_Gate;
            chkNCarEntSend.Checked = env.CommunicationEnv.Lpr1Info.LprOpt.Normal_SendData;
            chkNCarEntLprtrns.Checked = env.CommunicationEnv.Lpr1Info.LprOpt.Normal_Lprtrns;
            chkNCarEntTckttrns.Checked = env.CommunicationEnv.Lpr1Info.LprOpt.Normal_Tckttrns;
            chkNCarEntCountting.Checked = env.CommunicationEnv.Lpr1Info.LprOpt.Normal_Counter;
            chkNCarEntGate.Checked = env.CommunicationEnv.Lpr1Info.LprOpt.Normal_Gate;
            chkPCarExitSend.Checked = env.CommunicationEnv.Lpr2Info.LprOpt.Period_SendData;
            chkPCarExitLprtrns.Checked = env.CommunicationEnv.Lpr2Info.LprOpt.Period_Lprtrns;
            chkPCarExitPasstrns.Checked = env.CommunicationEnv.Lpr2Info.LprOpt.Period_Passtrns;
            chkPCarExitCountting.Checked = env.CommunicationEnv.Lpr2Info.LprOpt.Period_Counter;
            chkPCarExitGate.Checked = env.CommunicationEnv.Lpr2Info.LprOpt.Period_Gate;
            chkNCarExitSend.Checked = env.CommunicationEnv.Lpr2Info.LprOpt.Normal_SendData;
            chkNCarExitLprtrns.Checked = env.CommunicationEnv.Lpr2Info.LprOpt.Normal_Lprtrns;
            chkNCarExitTckttrns.Checked = env.CommunicationEnv.Lpr2Info.LprOpt.Normal_Tckttrns;
            chkNCarExitCountting.Checked = env.CommunicationEnv.Lpr2Info.LprOpt.Normal_Counter;
            chkNCarExitGate.Checked = env.CommunicationEnv.Lpr2Info.LprOpt.Normal_Gate;
            chkUseReturn.Checked = env.CommunicationEnv.ReturnCar.Use;
            txtReturnTerm.Text = env.CommunicationEnv.ReturnCar.Term.ToString();
            txtReturnMent.Text = env.CommunicationEnv.ReturnCar.Ment;
            #endregion
            chkSendOffice.Checked = env.SendOffice;

            #region 차단기 개방 예외 정기차량 그룹
            cmbExceptGroup.Items.Add("사용안함");
            foreach (string item in clsExceptGroup.GetGroupList())
            {
                cmbExceptGroup.Items.Add(item);
                if (item.Substring(1, item.IndexOf(']') - 1) == clsExceptGroup.ExceptGrpNo.ToString())
                    cmbExceptGroup.SelectedIndex = cmbExceptGroup.Items.Count - 1;
            }
            if (cmbExceptGroup.SelectedIndex == -1)
                cmbExceptGroup.SelectedIndex = 0;
            #endregion

            chkBeforeCalUse.Checked = BeforeCalOpt.Use;
            txtBeforeCalLag.Text = BeforeCalOpt.LagTime.ToString();

            chkOutService.Checked = clsOutService.Use;
            txtOutService.Text = clsOutService.Service.ToString();

            #region 특정 정기차량 그룹만 정기권 처리
            cmbSpeciaGroup.Items.Add("사용안함");
            foreach (string item in clsExceptGroup.GetGroupList())
            {
                cmbSpeciaGroup.Items.Add(item);
                if (item.Substring(1, item.IndexOf(']') - 1) == SpecialGroup.GroupIdx.ToString())
                    cmbSpeciaGroup.SelectedIndex = cmbSpeciaGroup.Items.Count - 1;
            }
            if (SpecialGroup.GroupIdx == -1)
                cmbSpeciaGroup.SelectedIndex = 0;
            #endregion

            chkBlackListUse.Checked = BlackList.Use;
            txtBlackDisplayBadText1.Text = BlackList.Bad1Text;
            txtBlackDisplayBadText2.Text = BlackList.Bad2Text;
            cmbBlackDisplayBadColor1.Text = BlackList.Bad1Color;
            cmbBlackDisplayBadColor2.Text = BlackList.Bad2Color;
            txtBlackDisplayNormalText1.Text = BlackList.Normal1Text;
            txtBlackDisplayNormalText2.Text = BlackList.Normal2Text;
            cmbBlackDisplayNormalColor1.Text = BlackList.Normal1Color;
            cmbBlackDisplayNormalColor2.Text = BlackList.Normal2Color;
            txtBlackDisplayRegText1.Text = BlackList.Period1Text;
            txtBlackDisplayRegText2.Text = BlackList.Period2Text;
            cmbBlackDisplayRegColor1.Text = BlackList.Period1Color;
            cmbBlackDisplayRegColor2.Text = BlackList.Period2Color;
            cmbBlackOutDisplay.Checked = BlackList.UseOutDisPlay;
            cmbBlackOutGateControl.Checked = BlackList.DoNotOpenOutGate;
            mskBlackStart.Text = BlackList.StartTime;
            mskBlackEnd.Text = BlackList.EndTime;

            #region 정기차량 제한
            lstOtherPark.View = View.Details;
            lstOtherPark.GridLines = true;
            lstOtherPark.MultiSelect = false;
            lstOtherPark.CheckBoxes = true;
            lstOtherPark.FullRowSelect = true;
            lstOtherPark.Columns.Add("사용");
            lstOtherPark.Columns.Add("확장번호", 80);
            lstOtherPark.Columns.Add("안내문구", 150);
            lstOtherPark.Items.Clear();
            chkOtherparkuse.Checked = env.RegCarControl.OtherparkUse;
            foreach (park item in env.RegCarControl.Otherparks)
            {
                ListViewItem litem = new ListViewItem();
                litem.Checked = item.Use;
                litem.SubItems.Add(item.parkno.ToString());
                litem.SubItems.Add(item.ment);
                lstOtherPark.Items.Add(litem);
            }

            chkEntLimit.Checked = env.RegCarControl.Entcontroluse;
            txtEntLimitMent.Text = env.RegCarControl.Entcontrolment;
            chkOtherparktimeuse.Checked = env.RegCarControl.OtherparksTimeuse;
            mskOtherparktimestart.Text = env.RegCarControl.Otherparksstart;
            mskOtherparktimeend.Text = env.RegCarControl.Otherparksend;
            chkiLotarea.Checked = env.RegCarControl.Ilotarea;
            chkRegDelayUse.Checked = env.RegCarControl.Regautodeluse;
            mskAutoregdeltime.Text = env.RegCarControl.Regautodeltime;
            for (int i = 1; i < 61; i++)
            {
                cmbRegendnotiterm.Items.Add(i);
            }
            chkRegendnotiuse.Checked = env.RegCarControl.Regendnotiuse;
            cmbRegendnotiterm.Text = env.RegCarControl.Regendnotiday;
            chkusePenalty.Checked = env.RegCarControl.Penaltiuse;
            txtPenaltyment.Text = env.RegCarControl.Penaltiment;

            chkGateGroupUse.Checked = env.RegCarControl.UseGroupGate;
            txtGroupNo.Text = env.RegCarControl.GateGroupNo.ToString();
            lstGroup.View = View.Details;
            lstGroup.GridLines = true;
            lstGroup.MultiSelect = false;
            lstGroup.CheckBoxes = true;
            lstGroup.FullRowSelect = true;
            lstGroup.Columns.Add("사용", 40);
            lstGroup.Columns.Add("No", 30);
            lstGroup.Columns.Add("그룹명칭", 90);
            lstGroup.Columns.Add("안내문구", 90);
            lstGroup.Items.Clear();
            chkGateGroupUse.Checked = env.RegCarControl.UseGroupGate;
            try
            {
                for (int i = 0; i < 13; i++)
                {
                    ListViewItem item = new ListViewItem();
                    if (env.RegCarControl.GroupUse.Length > i)
                        item.Checked = env.RegCarControl.GroupUse[i];
                    item.SubItems.Add((i + 1).ToString());
                    if (env.RegCarControl.GateGroupName.Length > i && env.RegCarControl.GateGroupName[i] != null)
                        item.SubItems.Add(env.RegCarControl.GateGroupName[i]);
                    else
                        item.SubItems.Add("");
                    if (env.RegCarControl.GroupMent.Length > i && env.RegCarControl.GroupMent[i] != null)
                        item.SubItems.Add(env.RegCarControl.GroupMent[i]);
                    else
                        item.SubItems.Add("");
                    lstGroup.Items.Add(item);
                }
                chkGroupTimeUse.Checked = env.RegCarControl.GroupUseTime;
                mskGroupFrom.Text = env.RegCarControl.GroupStart;
                mskGroupTo.Text = env.RegCarControl.GroupEnd;
            }
            catch (Exception) { }
            #endregion


            //leess 긴급차량 개방
            checkEmergencyCar.Checked = env.EmergencyCar;
        }

        private void IpUpdated(object sender, IpUpdatedEventArgs arg)
        {
            UpdateItemToList(listCamIPlist, arg);
        }

        delegate void UpdateItemToListCallback(ListBox list, IpUpdatedEventArgs arg);

        private void UpdateItemToList(ListBox list, IpUpdatedEventArgs arg)
        {
            if (list.InvokeRequired)
            {
                var d = new UpdateItemToListCallback(UpdateItemToList);
                try
                {
                    this.BeginInvoke(d, new object[] { list, arg });
                }
                catch (ObjectDisposedException) { }
            }
            else
            {
                if (arg.added && !list.Items.Contains(arg.ipAddress))
                {
                    list.Items.Add(arg.ipAddress);
                }
                else if (!arg.added && list.Items.Contains(arg.ipAddress))
                {
                    list.Items.Remove(arg.ipAddress);
                }
            }
        }

        private void btnSetROI_Click(object sender, EventArgs e)
        {
            frmPicConfig frm = null;
            switch (groupBox1.Text)
            {
                case "1번 카메라 설정":
                    frm = new frmPicConfig(env, 1);
                    break;
                case "2번 카메라 설정":
                    frm = new frmPicConfig(env, 2);
                    break;
            }
            DialogResult ret = frm.ShowDialog();
            if (ret == System.Windows.Forms.DialogResult.OK)
            {
                switch (groupBox1.Text)
                {
                    case "1번 카메라 설정":
                        env.CameraEnv.IPCamera1Info.Roi = frm.RoiRect;
                        Util.Function.IniWriteValue("CAMERA", "cam1roi", String.Format("{0}, {1}, {2}, {3}", env.CameraEnv.IPCamera1Info.Roi.X, env.CameraEnv.IPCamera1Info.Roi.Y, env.CameraEnv.IPCamera1Info.Roi.Width, env.CameraEnv.IPCamera1Info.Roi.Height));
                        break;
                    case "2번 카메라 설정":
                        env.CameraEnv.IPCamera2Info.Roi = frm.RoiRect;
                        Util.Function.IniWriteValue("CAMERA", "cam2roi", String.Format("{0}, {1}, {2}, {3}", env.CameraEnv.IPCamera2Info.Roi.X, env.CameraEnv.IPCamera2Info.Roi.Y, env.CameraEnv.IPCamera2Info.Roi.Width, env.CameraEnv.IPCamera2Info.Roi.Height));
                        break;
                }
            }
        }

        private void btnCamSetup_Click(object sender, EventArgs e)
        {
            //leess iNova2추가
            if(env.CameraEnv.iNovaType == 1) {
                frmAdvFeature frm = null;
                switch(groupBox1.Text) {
                    case "1번 카메라 설정":
                        frm = new frmAdvFeature(Cam1);
                        break;
                    case "2번 카메라 설정":
                        frm = new frmAdvFeature(Cam2);
                        break;
                }
                frm.ShowDialog();
            } else if(env.CameraEnv.iNovaType == 2) {
                iNova2.frmAdvFeature frm = null;
                switch(groupBox1.Text) {
                    case "1번 카메라 설정":
                        frm = new iNova2.frmAdvFeature(Cam1_iNova2);
                        break;
                    case "2번 카메라 설정":
                        frm = new iNova2.frmAdvFeature(Cam2_iNova2);
                        break;
                }
                frm.ShowDialog();
            }
        }

        private void chkCamUse_CheckedChanged(object sender, EventArgs e)
        {
            groupBox1.Enabled = chkCamUse.Checked;
        }

        private void btnCam1_Click(object sender, EventArgs e)
        {
            getEnv();
            SubCaminfoTextClear(groupBox1);
            groupBox1.Text = "1번 카메라 설정";
            chkCamUse.Checked = env.CameraEnv.IPCamera1Info.Use;
            txtCamIp.Text = env.CameraEnv.IPCamera1Info.IP;
            cmbChName.Text = env.CameraEnv.IPCamera1Info.ChName;
            rdUdpStream.Checked = env.CameraEnv.IPCamera1Info.StreamUdp;

            txtBasicInterval.Text = env.CameraEnv.IPCamera1Info.User_Setting_Resend_Interval.ToString();

            if (env.CameraEnv.IPCamera1Info.User_Setting != null && env.CameraEnv.IPCamera1Info.User_Setting.Length == 3)
            {
                ChkUseTime1.Checked = env.CameraEnv.IPCamera1Info.User_Setting[0].use;
                MskStartTime1.Text = env.CameraEnv.IPCamera1Info.User_Setting[0].StartTime;
                MskEndTime1.Text = env.CameraEnv.IPCamera1Info.User_Setting[0].EndTime;
                txtTimeBright1.Text = env.CameraEnv.IPCamera1Info.User_Setting[0].Exposuer.ToString();
                CmbBracket1.SelectedIndex = env.CameraEnv.IPCamera1Info.User_Setting[0].ModeIdx;
                chkMode1Bra.Checked = env.CameraEnv.IPCamera1Info.User_Setting[0].UseBarkect;
                chkMode1Alc.Checked = env.CameraEnv.IPCamera1Info.User_Setting[0].UseALC;

                ChkUseTime2.Checked = env.CameraEnv.IPCamera1Info.User_Setting[1].use;
                MskStartTime2.Text = env.CameraEnv.IPCamera1Info.User_Setting[1].StartTime;
                MskEndTime2.Text = env.CameraEnv.IPCamera1Info.User_Setting[1].EndTime;
                txtTimeBright2.Text = env.CameraEnv.IPCamera1Info.User_Setting[1].Exposuer.ToString();
                CmbBracket2.SelectedIndex = env.CameraEnv.IPCamera1Info.User_Setting[1].ModeIdx;
                chkMode2Bra.Checked = env.CameraEnv.IPCamera1Info.User_Setting[1].UseBarkect;
                chkMode2Alc.Checked = env.CameraEnv.IPCamera1Info.User_Setting[1].UseALC;

                ChkUseTime3.Checked = env.CameraEnv.IPCamera1Info.User_Setting[2].use;
                MskStartTime3.Text = env.CameraEnv.IPCamera1Info.User_Setting[2].StartTime;
                MskEndTime3.Text = env.CameraEnv.IPCamera1Info.User_Setting[2].EndTime;
                txtTimeBright3.Text = env.CameraEnv.IPCamera1Info.User_Setting[2].Exposuer.ToString();
                CmbBracket3.SelectedIndex = env.CameraEnv.IPCamera1Info.User_Setting[2].ModeIdx;
                chkMode3Bra.Checked = env.CameraEnv.IPCamera1Info.User_Setting[2].UseBarkect;
                chkMode3Alc.Checked = env.CameraEnv.IPCamera1Info.User_Setting[2].UseALC;
            }
            //chkEtcImageSave.Checked = env.CameraEnv.IPCamera1Info.ImageSave.EtcSave;
            //txtEtcImagePath.Text = env.CameraEnv.IPCamera1Info.ImageSave.EtcPath;

            cmbLoop.Text = env.CameraEnv.IPCamera1Info.DioInPut.LoopPort.ToString();
            chkSmallCar.Checked = env.CameraEnv.IPCamera1Info.DioInPut.SmallCar;
            cmbSmallCar.Text = env.CameraEnv.IPCamera1Info.DioInPut.SmallPort.ToString();

            if(env.CameraEnv.IPCamera1Info.User_Brakect != null)
            {
                txtTimeExposure11.Text = env.CameraEnv.IPCamera1Info.User_Brakect[0, 0].Exposure.ToString();
                trackAGain11.Value = env.CameraEnv.IPCamera1Info.User_Brakect[0, 0].AnalogGain;
                trackDGain11.Value = env.CameraEnv.IPCamera1Info.User_Brakect[0, 0].DigitalGain;

                txtTimeExposure12.Text = env.CameraEnv.IPCamera1Info.User_Brakect[0, 1].Exposure.ToString();
                trackAGain12.Value = env.CameraEnv.IPCamera1Info.User_Brakect[0, 1].AnalogGain;
                trackDGain12.Value = env.CameraEnv.IPCamera1Info.User_Brakect[0, 1].DigitalGain;

                txtTimeExposure13.Text = env.CameraEnv.IPCamera1Info.User_Brakect[0, 2].Exposure.ToString();
                trackAGain13.Value = env.CameraEnv.IPCamera1Info.User_Brakect[0, 2].AnalogGain;
                trackDGain13.Value = env.CameraEnv.IPCamera1Info.User_Brakect[0, 2].DigitalGain;

                txtTimeExposure14.Text = env.CameraEnv.IPCamera1Info.User_Brakect[0, 3].Exposure.ToString();
                trackAGain14.Value = env.CameraEnv.IPCamera1Info.User_Brakect[0, 3].AnalogGain;
                trackDGain14.Value = env.CameraEnv.IPCamera1Info.User_Brakect[0, 3].DigitalGain;

                txtTimeExposure21.Text = env.CameraEnv.IPCamera1Info.User_Brakect[1, 0].Exposure.ToString();
                trackAGain21.Value = env.CameraEnv.IPCamera1Info.User_Brakect[1, 0].AnalogGain;
                trackDGain21.Value = env.CameraEnv.IPCamera1Info.User_Brakect[1, 0].DigitalGain;

                txtTimeExposure22.Text = env.CameraEnv.IPCamera1Info.User_Brakect[1, 1].Exposure.ToString();
                trackAGain22.Value = env.CameraEnv.IPCamera1Info.User_Brakect[1, 1].AnalogGain;
                trackDGain22.Value = env.CameraEnv.IPCamera1Info.User_Brakect[1, 1].DigitalGain;

                txtTimeExposure23.Text = env.CameraEnv.IPCamera1Info.User_Brakect[1, 2].Exposure.ToString();
                trackAGain23.Value = env.CameraEnv.IPCamera1Info.User_Brakect[1, 2].AnalogGain;
                trackDGain23.Value = env.CameraEnv.IPCamera1Info.User_Brakect[1, 2].DigitalGain;

                txtTimeExposure24.Text = env.CameraEnv.IPCamera1Info.User_Brakect[1, 3].Exposure.ToString();
                trackAGain24.Value = env.CameraEnv.IPCamera1Info.User_Brakect[1, 3].AnalogGain;
                trackDGain24.Value = env.CameraEnv.IPCamera1Info.User_Brakect[1, 3].DigitalGain;

                txtTimeExposure31.Text = env.CameraEnv.IPCamera1Info.User_Brakect[2, 0].Exposure.ToString();
                trackAGain31.Value = env.CameraEnv.IPCamera1Info.User_Brakect[2, 0].AnalogGain;
                trackDGain31.Value = env.CameraEnv.IPCamera1Info.User_Brakect[2, 0].DigitalGain;

                txtTimeExposure32.Text = env.CameraEnv.IPCamera1Info.User_Brakect[2, 1].Exposure.ToString();
                trackAGain32.Value = env.CameraEnv.IPCamera1Info.User_Brakect[2, 1].AnalogGain;
                trackDGain32.Value = env.CameraEnv.IPCamera1Info.User_Brakect[2, 1].DigitalGain;

                txtTimeExposure33.Text = env.CameraEnv.IPCamera1Info.User_Brakect[2, 2].Exposure.ToString();
                trackAGain33.Value = env.CameraEnv.IPCamera1Info.User_Brakect[2, 2].AnalogGain;
                trackDGain33.Value = env.CameraEnv.IPCamera1Info.User_Brakect[2, 2].DigitalGain;

                txtTimeExposure34.Text = env.CameraEnv.IPCamera1Info.User_Brakect[2, 3].Exposure.ToString();
                trackAGain34.Value = env.CameraEnv.IPCamera1Info.User_Brakect[2, 3].AnalogGain;
                trackDGain34.Value = env.CameraEnv.IPCamera1Info.User_Brakect[2, 3].DigitalGain;
            }

            if (env.CameraEnv.IPCamera1Info.User_Alc != null)
            {
                trackAECTarget1.Value = env.CameraEnv.IPCamera1Info.User_Alc[0].target;
                chkAEC1.Checked = env.CameraEnv.IPCamera1Info.User_Alc[0].AECInfo.enableAEC;
                txtAECRangeMin1.Text = env.CameraEnv.IPCamera1Info.User_Alc[0].AECInfo.minExposure.ToString();
                txtAECRangeMax1.Text = env.CameraEnv.IPCamera1Info.User_Alc[0].AECInfo.maxExposure.ToString();
                chkAGC1.Checked = env.CameraEnv.IPCamera1Info.User_Alc[0].AGCInfo.enableAGC;
                txtAGCRangeMin1.Text = env.CameraEnv.IPCamera1Info.User_Alc[0].AGCInfo.minGain.ToString();
                txtAGCRangeMax1.Text = env.CameraEnv.IPCamera1Info.User_Alc[0].AGCInfo.maxGain.ToString();

                trackAECTarget2.Value = env.CameraEnv.IPCamera1Info.User_Alc[1].target;
                chkAEC2.Checked = env.CameraEnv.IPCamera1Info.User_Alc[1].AECInfo.enableAEC;
                txtAECRangeMin2.Text = env.CameraEnv.IPCamera1Info.User_Alc[1].AECInfo.minExposure.ToString();
                txtAECRangeMax2.Text = env.CameraEnv.IPCamera1Info.User_Alc[1].AECInfo.maxExposure.ToString();
                chkAGC2.Checked = env.CameraEnv.IPCamera1Info.User_Alc[1].AGCInfo.enableAGC;
                txtAGCRangeMin2.Text = env.CameraEnv.IPCamera1Info.User_Alc[1].AGCInfo.minGain.ToString();
                txtAGCRangeMax2.Text = env.CameraEnv.IPCamera1Info.User_Alc[1].AGCInfo.maxGain.ToString();

                trackAECTarget3.Value = env.CameraEnv.IPCamera1Info.User_Alc[2].target;
                chkAEC3.Checked = env.CameraEnv.IPCamera1Info.User_Alc[2].AECInfo.enableAEC;
                txtAECRangeMin3.Text = env.CameraEnv.IPCamera1Info.User_Alc[2].AECInfo.minExposure.ToString();
                txtAECRangeMax3.Text = env.CameraEnv.IPCamera1Info.User_Alc[2].AECInfo.maxExposure.ToString();
                chkAGC3.Checked = env.CameraEnv.IPCamera1Info.User_Alc[2].AGCInfo.enableAGC;
                txtAGCRangeMin3.Text = env.CameraEnv.IPCamera1Info.User_Alc[2].AGCInfo.minGain.ToString();
                txtAGCRangeMax3.Text = env.CameraEnv.IPCamera1Info.User_Alc[2].AGCInfo.maxGain.ToString();
            }
            trackAECTarget1.Value = env.CameraEnv.IPCamera1Info.User_Alc[0].target;
            trackAECTarget1Value.Text = env.CameraEnv.IPCamera1Info.User_Alc[0].target.ToString();
            trackAECTarget2.Value = env.CameraEnv.IPCamera1Info.User_Alc[1].target;
            trackAECTarget2Value.Text = env.CameraEnv.IPCamera1Info.User_Alc[1].target.ToString();
            trackAECTarget3.Value = env.CameraEnv.IPCamera1Info.User_Alc[2].target;
            trackAECTarget3Value.Text = env.CameraEnv.IPCamera1Info.User_Alc[2].target.ToString();

            //Cam Current Info
            //int cnt = 0;
            //bool blBarakect = false;
            //Cam1.GetTriggerImageCount(out cnt);
            //cmbTriggerCnt.Text = cnt.ToString();
            //Cam1.GetBracketMode(out blBarakect, out cnt);
            //cmbBrakectCnt.Text = cnt.ToString();
            cmbTriggerCnt.Text = env.CameraEnv.IPCamera1Info.TriggerCnt.ToString();
            cmbBrakectCnt.Text = env.CameraEnv.IPCamera1Info.BarkectCnt.ToString();
            //double frame = 0;
            //Cam1.GetFrameRate(out frame);
            //txtFrameRate.Text = frame.ToString();
            //Cam1.GetTriggerMode(out cnt, out blBarakect);
            //cmbTriggerMode.SelectedIndex = cnt;
            txtFrameRate.Text = env.CameraEnv.IPCamera1Info.FrameRate.ToString();
            cmbTriggerMode.SelectedIndex = env.CameraEnv.IPCamera1Info.TriggerMode;
        }

        private void btnCam2_Click(object sender, EventArgs e)
        {
            getEnv();
            SubCaminfoTextClear(groupBox1);
            groupBox1.Text = "2번 카메라 설정";
            chkCamUse.Checked = env.CameraEnv.IPCamera2Info.Use;
            txtCamIp.Text = env.CameraEnv.IPCamera2Info.IP;
            cmbChName.Text = env.CameraEnv.IPCamera2Info.ChName;
            rdUdpStream.Checked = env.CameraEnv.IPCamera2Info.StreamUdp;

            txtBasicInterval.Text = env.CameraEnv.IPCamera2Info.User_Setting_Resend_Interval.ToString();
            if (env.CameraEnv.IPCamera2Info.User_Setting != null && env.CameraEnv.IPCamera2Info.User_Setting.Length == 3)
            {
                ChkUseTime1.Checked = env.CameraEnv.IPCamera2Info.User_Setting[0].use;
                MskStartTime1.Text = env.CameraEnv.IPCamera2Info.User_Setting[0].StartTime;
                MskEndTime1.Text = env.CameraEnv.IPCamera2Info.User_Setting[0].EndTime;
                txtTimeBright1.Text = env.CameraEnv.IPCamera2Info.User_Setting[0].Exposuer.ToString();
                CmbBracket1.SelectedIndex = env.CameraEnv.IPCamera2Info.User_Setting[0].ModeIdx;
                chkMode1Bra.Checked = env.CameraEnv.IPCamera2Info.User_Setting[0].UseBarkect;
                chkMode1Alc.Checked = env.CameraEnv.IPCamera2Info.User_Setting[0].UseALC;

                ChkUseTime2.Checked = env.CameraEnv.IPCamera2Info.User_Setting[1].use;
                MskStartTime2.Text = env.CameraEnv.IPCamera2Info.User_Setting[1].StartTime;
                MskEndTime2.Text = env.CameraEnv.IPCamera2Info.User_Setting[1].EndTime;
                txtTimeBright2.Text = env.CameraEnv.IPCamera2Info.User_Setting[1].Exposuer.ToString();
                CmbBracket2.SelectedIndex = env.CameraEnv.IPCamera2Info.User_Setting[1].ModeIdx;
                chkMode2Bra.Checked = env.CameraEnv.IPCamera2Info.User_Setting[1].UseBarkect;
                chkMode2Alc.Checked = env.CameraEnv.IPCamera2Info.User_Setting[1].UseALC;

                ChkUseTime3.Checked = env.CameraEnv.IPCamera2Info.User_Setting[2].use;
                MskStartTime3.Text = env.CameraEnv.IPCamera2Info.User_Setting[2].StartTime;
                MskEndTime3.Text = env.CameraEnv.IPCamera2Info.User_Setting[2].EndTime;
                txtTimeBright3.Text = env.CameraEnv.IPCamera2Info.User_Setting[2].Exposuer.ToString();
                CmbBracket3.SelectedIndex = env.CameraEnv.IPCamera2Info.User_Setting[2].ModeIdx;
                chkMode3Bra.Checked = env.CameraEnv.IPCamera2Info.User_Setting[2].UseBarkect;
                chkMode3Alc.Checked = env.CameraEnv.IPCamera2Info.User_Setting[2].UseALC;
            }
            //chkEtcImageSave.Checked = env.CameraEnv.IPCamera2Info.ImageSave.EtcSave;
            //txtEtcImagePath.Text = env.CameraEnv.IPCamera2Info.ImageSave.EtcPath;

            cmbLoop.Text = env.CameraEnv.IPCamera2Info.DioInPut.LoopPort.ToString();
            chkSmallCar.Checked = env.CameraEnv.IPCamera2Info.DioInPut.SmallCar;
            cmbSmallCar.Text = env.CameraEnv.IPCamera2Info.DioInPut.SmallPort.ToString();

            if (env.CameraEnv.IPCamera2Info.User_Brakect != null)
            {
                txtTimeExposure11.Text = env.CameraEnv.IPCamera2Info.User_Brakect[0, 0].Exposure.ToString();
                trackAGain11.Value = env.CameraEnv.IPCamera2Info.User_Brakect[0, 0].AnalogGain;
                trackDGain11.Value = env.CameraEnv.IPCamera2Info.User_Brakect[0, 0].DigitalGain;

                txtTimeExposure12.Text = env.CameraEnv.IPCamera2Info.User_Brakect[0, 1].Exposure.ToString();
                trackAGain12.Value = env.CameraEnv.IPCamera2Info.User_Brakect[0, 1].AnalogGain;
                trackDGain12.Value = env.CameraEnv.IPCamera2Info.User_Brakect[0, 1].DigitalGain;

                txtTimeExposure13.Text = env.CameraEnv.IPCamera2Info.User_Brakect[0, 2].Exposure.ToString();
                trackAGain13.Value = env.CameraEnv.IPCamera2Info.User_Brakect[0, 2].AnalogGain;
                trackDGain13.Value = env.CameraEnv.IPCamera2Info.User_Brakect[0, 2].DigitalGain;

                txtTimeExposure14.Text = env.CameraEnv.IPCamera2Info.User_Brakect[0, 3].Exposure.ToString();
                trackAGain14.Value = env.CameraEnv.IPCamera2Info.User_Brakect[0, 3].AnalogGain;
                trackDGain14.Value = env.CameraEnv.IPCamera2Info.User_Brakect[0, 3].DigitalGain;

                txtTimeExposure21.Text = env.CameraEnv.IPCamera2Info.User_Brakect[1, 0].Exposure.ToString();
                trackAGain21.Value = env.CameraEnv.IPCamera2Info.User_Brakect[1, 0].AnalogGain;
                trackDGain21.Value = env.CameraEnv.IPCamera2Info.User_Brakect[1, 0].DigitalGain;

                txtTimeExposure22.Text = env.CameraEnv.IPCamera2Info.User_Brakect[1, 1].Exposure.ToString();
                trackAGain22.Value = env.CameraEnv.IPCamera2Info.User_Brakect[1, 1].AnalogGain;
                trackDGain22.Value = env.CameraEnv.IPCamera2Info.User_Brakect[1, 1].DigitalGain;

                txtTimeExposure23.Text = env.CameraEnv.IPCamera2Info.User_Brakect[1, 2].Exposure.ToString();
                trackAGain23.Value = env.CameraEnv.IPCamera2Info.User_Brakect[1, 2].AnalogGain;
                trackDGain23.Value = env.CameraEnv.IPCamera2Info.User_Brakect[1, 2].DigitalGain;

                txtTimeExposure24.Text = env.CameraEnv.IPCamera2Info.User_Brakect[1, 3].Exposure.ToString();
                trackAGain24.Value = env.CameraEnv.IPCamera2Info.User_Brakect[1, 3].AnalogGain;
                trackDGain24.Value = env.CameraEnv.IPCamera2Info.User_Brakect[1, 3].DigitalGain;

                txtTimeExposure31.Text = env.CameraEnv.IPCamera2Info.User_Brakect[2, 0].Exposure.ToString();
                trackAGain31.Value = env.CameraEnv.IPCamera2Info.User_Brakect[2, 0].AnalogGain;
                trackDGain31.Value = env.CameraEnv.IPCamera2Info.User_Brakect[2, 0].DigitalGain;

                txtTimeExposure32.Text = env.CameraEnv.IPCamera2Info.User_Brakect[2, 1].Exposure.ToString();
                trackAGain32.Value = env.CameraEnv.IPCamera2Info.User_Brakect[2, 1].AnalogGain;
                trackDGain32.Value = env.CameraEnv.IPCamera2Info.User_Brakect[2, 1].DigitalGain;

                txtTimeExposure33.Text = env.CameraEnv.IPCamera2Info.User_Brakect[2, 2].Exposure.ToString();
                trackAGain33.Value = env.CameraEnv.IPCamera2Info.User_Brakect[2, 2].AnalogGain;
                trackDGain33.Value = env.CameraEnv.IPCamera2Info.User_Brakect[2, 2].DigitalGain;

                txtTimeExposure34.Text = env.CameraEnv.IPCamera2Info.User_Brakect[2, 3].Exposure.ToString();
                trackAGain34.Value = env.CameraEnv.IPCamera2Info.User_Brakect[2, 3].AnalogGain;
                trackDGain34.Value = env.CameraEnv.IPCamera2Info.User_Brakect[2, 3].DigitalGain;
            }

            if (env.CameraEnv.IPCamera2Info.User_Alc != null)
            {
                trackAECTarget1.Value = env.CameraEnv.IPCamera2Info.User_Alc[0].target;
                chkAEC1.Checked = env.CameraEnv.IPCamera2Info.User_Alc[0].AECInfo.enableAEC;
                txtAECRangeMin1.Text = env.CameraEnv.IPCamera2Info.User_Alc[0].AECInfo.minExposure.ToString();
                txtAECRangeMax1.Text = env.CameraEnv.IPCamera2Info.User_Alc[0].AECInfo.maxExposure.ToString();
                chkAGC1.Checked = env.CameraEnv.IPCamera2Info.User_Alc[0].AGCInfo.enableAGC;
                txtAGCRangeMin1.Text = env.CameraEnv.IPCamera2Info.User_Alc[0].AGCInfo.minGain.ToString();
                txtAGCRangeMax1.Text = env.CameraEnv.IPCamera2Info.User_Alc[0].AGCInfo.maxGain.ToString();

                trackAECTarget2.Value = env.CameraEnv.IPCamera2Info.User_Alc[1].target;
                chkAEC2.Checked = env.CameraEnv.IPCamera2Info.User_Alc[1].AECInfo.enableAEC;
                txtAECRangeMin2.Text = env.CameraEnv.IPCamera2Info.User_Alc[1].AECInfo.minExposure.ToString();
                txtAECRangeMax2.Text = env.CameraEnv.IPCamera2Info.User_Alc[1].AECInfo.maxExposure.ToString();
                chkAGC2.Checked = env.CameraEnv.IPCamera2Info.User_Alc[1].AGCInfo.enableAGC;
                txtAGCRangeMin2.Text = env.CameraEnv.IPCamera2Info.User_Alc[1].AGCInfo.minGain.ToString();
                txtAGCRangeMax2.Text = env.CameraEnv.IPCamera2Info.User_Alc[1].AGCInfo.maxGain.ToString();

                trackAECTarget3.Value = env.CameraEnv.IPCamera2Info.User_Alc[2].target;
                chkAEC3.Checked = env.CameraEnv.IPCamera2Info.User_Alc[2].AECInfo.enableAEC;
                txtAECRangeMin3.Text = env.CameraEnv.IPCamera2Info.User_Alc[2].AECInfo.minExposure.ToString();
                txtAECRangeMax3.Text = env.CameraEnv.IPCamera2Info.User_Alc[2].AECInfo.maxExposure.ToString();
                chkAGC3.Checked = env.CameraEnv.IPCamera2Info.User_Alc[2].AGCInfo.enableAGC;
                txtAGCRangeMin3.Text = env.CameraEnv.IPCamera2Info.User_Alc[2].AGCInfo.minGain.ToString();
                txtAGCRangeMax3.Text = env.CameraEnv.IPCamera2Info.User_Alc[2].AGCInfo.maxGain.ToString();
            }
            trackAECTarget1.Value = env.CameraEnv.IPCamera2Info.User_Alc[0].target;
            trackAECTarget1Value.Text = env.CameraEnv.IPCamera2Info.User_Alc[0].target.ToString();
            trackAECTarget2.Value = env.CameraEnv.IPCamera2Info.User_Alc[1].target;
            trackAECTarget2Value.Text = env.CameraEnv.IPCamera2Info.User_Alc[1].target.ToString();
            trackAECTarget3.Value = env.CameraEnv.IPCamera2Info.User_Alc[2].target;
            trackAECTarget3Value.Text = env.CameraEnv.IPCamera2Info.User_Alc[2].target.ToString();

            //Cam Current Info
            int cnt = 0;
            bool blBarakect = false;
            Cam2.GetTriggerImageCount(out cnt);
            cmbTriggerCnt.Text = cnt.ToString();
            Cam2.GetBracketMode(out blBarakect, out cnt);
            cmbBrakectCnt.Text = cnt.ToString();
            double frame = 0;
            Cam2.GetFrameRate(out frame);
            txtFrameRate.Text = frame.ToString();
            Cam2.GetTriggerMode(out cnt, out blBarakect);
            cmbTriggerMode.SelectedIndex = cnt;
            cmbTriggerCnt.Text = env.CameraEnv.IPCamera2Info.TriggerCnt.ToString();
            cmbBrakectCnt.Text = env.CameraEnv.IPCamera2Info.BarkectCnt.ToString();
            txtFrameRate.Text = env.CameraEnv.IPCamera2Info.FrameRate.ToString();
            cmbTriggerMode.SelectedIndex = env.CameraEnv.IPCamera2Info.TriggerMode;
        }

        private void btnEnvSave_Click(object sender, EventArgs e)
        {
            IPAddress address;
            if (chkDisplay1NetUse.Checked && !IPAddress.TryParse(txtDisplay1NetIp.Text, out address))
            {
                MessageBox.Show("정확한 IP 주소를 입력 하시오");
                txtDisplay1NetIp.Focus();
                return;
            }
            if (chkDisplay1NetUse.Checked && Util.Function.IntTryParse(txtDisplay1NetPort.Text) < 0)
            {
                MessageBox.Show("정확한 포트를 입력 하시오");
                txtDisplay1NetPort.Focus();
                return;
            }

            if (chkDisplay2NetUse.Checked && !IPAddress.TryParse(txtDisplay2NetIp.Text, out address))
            {
                MessageBox.Show("정확한 IP 주소를 입력 하시오");
                txtDisplay2NetIp.Focus();
                return;
            }
            if (chkDisplay2NetUse.Checked && Util.Function.IntTryParse(txtDisplay2NetPort.Text) < 0)
            {
                MessageBox.Show("정확한 포트를 입력 하시오");
                txtDisplay2NetPort.Focus();
                return;
            }
            this.Enabled = false;
            double frame = 0;
            int triggerCnt = 0;
            int brakectCnt = 0;
            int trgMode = 0;
            bool job1 = false;
            bool job2 = false;
            bool job3 = false;
            bool job4 = false;
            switch (groupBox1.Text)
            {
                case "1번 카메라 설정":
                    //leess iNova2추가
                    if(env.CameraEnv.iNovaType == 1) {
                        Cam1.GetTriggerImageCount(out triggerCnt);
                        if(triggerCnt != Util.Function.IntTryParse(cmbTriggerCnt.Text))
                            job1 = Cam1.SetTriggerImageCount(Util.Function.IntTryParse(cmbTriggerCnt.Text));
                        Cam1.GetBracketMode(out job2, out brakectCnt);
                        if(brakectCnt != Util.Function.IntTryParse(cmbBrakectCnt.Text))
                            job2 = Cam1.SetBracketMode(job2, Util.Function.IntTryParse(cmbBrakectCnt.Text));
                        Cam1.GetTriggerMode(out trgMode, out job3);
                        if(trgMode != cmbTriggerMode.SelectedIndex)
                            job3 = Cam1.SetTriggerMode(cmbTriggerMode.SelectedIndex, job3);
                        Cam1.GetFrameRate(out frame);
                        if(frame != Util.Function.DoubleTryParse(txtFrameRate.Text))
                            job4 = Cam1.SetFrameRate(Util.Function.DoubleTryParse(txtFrameRate.Text));
                        Cam1.SaveSetting();
                    } else if(env.CameraEnv.iNovaType == 2) {
                        Cam1_iNova2.GetTriggerImageCount(out triggerCnt);
                        if(triggerCnt != Util.Function.IntTryParse(cmbTriggerCnt.Text))
                            job1 = (Cam1_iNova2.SetTriggerImageCount(Util.Function.IntTryParse(cmbTriggerCnt.Text)) == iNova2.IPCamError.OK);
                        Cam1_iNova2.GetBracketMode(out job2, out brakectCnt);
                        if(brakectCnt != Util.Function.IntTryParse(cmbBrakectCnt.Text))
                            job2 = (Cam1_iNova2.SetBracketMode(job2, Util.Function.IntTryParse(cmbBrakectCnt.Text)) == iNova2.IPCamError.OK);
                        Cam1_iNova2.GetTriggerMode(out trgMode, out job3);
                        if(trgMode != cmbTriggerMode.SelectedIndex)
                            job3 = (Cam1_iNova2.SetTriggerMode(cmbTriggerMode.SelectedIndex, job3) == iNova2.IPCamError.OK);
                        Cam1_iNova2.GetFrameRate(out frame);
                        if(frame != Util.Function.DoubleTryParse(txtFrameRate.Text))
                            job4 = (Cam1_iNova2.SetFrameRate(Util.Function.DoubleTryParse(txtFrameRate.Text)) == iNova2.IPCamError.OK);
                        Cam1_iNova2.SaveSetting();
                    }
                    break;
                case "2번 카메라 설정":
                    //leess iNova2추가
                    if(env.CameraEnv.iNovaType == 1) {
                        Cam2.GetTriggerImageCount(out triggerCnt);
                        if(triggerCnt != Util.Function.IntTryParse(cmbTriggerCnt.Text))
                            job1 = Cam2.SetTriggerImageCount(Util.Function.IntTryParse(cmbTriggerCnt.Text));
                        Cam2.GetBracketMode(out job2, out brakectCnt);
                        if(brakectCnt != Util.Function.IntTryParse(cmbBrakectCnt.Text))
                            job2 = Cam2.SetBracketMode(job2, Util.Function.IntTryParse(cmbBrakectCnt.Text));
                        Cam2.GetTriggerMode(out trgMode, out job3);
                        if(trgMode != cmbTriggerMode.SelectedIndex)
                            job3 = Cam2.SetTriggerMode(cmbTriggerMode.SelectedIndex, job3);
                        Cam2.GetFrameRate(out frame);
                        if(frame != Util.Function.DoubleTryParse(txtFrameRate.Text))
                            job4 = Cam2.SetFrameRate(Util.Function.DoubleTryParse(txtFrameRate.Text));
                        Cam2.SaveSetting();
                    } else if(env.CameraEnv.iNovaType == 2) {
                        Cam2_iNova2.GetTriggerImageCount(out triggerCnt);
                        if(triggerCnt != Util.Function.IntTryParse(cmbTriggerCnt.Text))
                            job1 = (Cam2_iNova2.SetTriggerImageCount(Util.Function.IntTryParse(cmbTriggerCnt.Text)) == iNova2.IPCamError.OK);
                        Cam2_iNova2.GetBracketMode(out job2, out brakectCnt);
                        if(brakectCnt != Util.Function.IntTryParse(cmbBrakectCnt.Text))
                            job2 = (Cam2_iNova2.SetBracketMode(job2, Util.Function.IntTryParse(cmbBrakectCnt.Text)) == iNova2.IPCamError.OK);
                        Cam2_iNova2.GetTriggerMode(out trgMode, out job3);
                        if(trgMode != cmbTriggerMode.SelectedIndex)
                            job3 = (Cam2_iNova2.SetTriggerMode(cmbTriggerMode.SelectedIndex, job3) == iNova2.IPCamError.OK);
                        Cam2_iNova2.GetFrameRate(out frame);
                        if(frame != Util.Function.DoubleTryParse(txtFrameRate.Text))
                            job4 = (Cam2_iNova2.SetFrameRate(Util.Function.DoubleTryParse(txtFrameRate.Text)) == iNova2.IPCamError.OK);
                        Cam2_iNova2.SaveSetting();
                    }
                    break;
            }
            getEnv();
            if (cmbExceptGroup.Text != string.Empty && cmbExceptGroup.Text != "사용안함")
                clsExceptGroup.Set_Except_Group(cmbExceptGroup.Text.Substring(1, cmbExceptGroup.Text.IndexOf(']') - 1));
            else
                clsExceptGroup.Set_Except_Group("-1");

            //leess ini에 저장
            func.SetEnv(env);

            BeforeCalOpt.Save();
            clsOutService.Save(chkOutService.Checked, txtOutService.Text);
            NoDriving.Save();
            BlackList.SaveEnv(chkBlackListUse.Checked
                , txtBlackDisplayNormalText1.Text, txtBlackDisplayNormalText2.Text, cmbBlackDisplayNormalColor1.Text, cmbBlackDisplayNormalColor2.Text
                , txtBlackDisplayRegText1.Text, txtBlackDisplayRegText2.Text, cmbBlackDisplayRegColor1.Text, cmbBlackDisplayRegColor2.Text
                , txtBlackDisplayBadText1.Text, txtBlackDisplayBadText2.Text, cmbBlackDisplayBadColor1.Text, cmbBlackDisplayBadColor2.Text
                , cmbBlackOutDisplay.Checked, cmbBlackOutGateControl.Checked, mskBlackStart.Text, mskBlackEnd.Text);
            if (cmbSpeciaGroup.Text != string.Empty && cmbSpeciaGroup.Text != "사용안함")
                SpecialGroup.GroupIdx = Util.Function.IntTryParse(cmbSpeciaGroup.Text.Substring(1, cmbSpeciaGroup.Text.IndexOf(']') - 1));
            else
                SpecialGroup.GroupIdx = -1;
            SpecialGroup.SaveInfo();
            //LprRelay.Save_Ini(chkLprEntUse.Checked, Util.Function.IntTryParse(txtLprEntPort.Text), txtLprEntIp.Text, cmbLprEntType.Text);

            this.Enabled = true;
            string errMsg = string.Empty;
            if (triggerCnt != Util.Function.IntTryParse(cmbTriggerCnt.Text))
                errMsg += string.Format("트리거 Cnt 설정  {0}\r\n", job1 == true ? "성공" : "실패");
            if (brakectCnt != Util.Function.IntTryParse(cmbBrakectCnt.Text))
                errMsg += string.Format("브라켓 Cnt 설정  {0}\r\n", job2 == true ? "성공" : "실패");
            if (trgMode != cmbTriggerMode.SelectedIndex)
                errMsg += string.Format("트리거 모드 설정  {0}\r\n", job3 == true ? "성공" : "실패");
            if (frame != Util.Function.DoubleTryParse(txtFrameRate.Text))
                errMsg += string.Format("프레임 Rate 설정  {0}\r\n", job4 == true ? "성공" : "실패");
            //MessageBox.Show(errMsg);
            //this.DialogResult = DialogResult.OK;
            clsBusinessCar.SetValue(chkBusinessUse.Checked, chkBusinessEntGateOpen.Checked, chkBusinessEntSendData.Checked, chkBusinessExitGateOpen.Checked, chkBusinessExitSendData.Checked, txtBusinessDisplayMent.Text);
            clsBusinessCar.SaveIni();
            #region regcarcontrol
            env.RegCarControl.Entcontroluse = chkEntLimit.Checked;
            env.RegCarControl.Entcontrolment = txtEntLimitMent.Text;
            env.RegCarControl.OtherparkUse = chkOtherparkuse.Checked;
            env.RegCarControl.Otherparks.Clear();
            foreach (ListViewItem item in lstOtherPark.Items)
            {
                park p = new park();
                p.Use = item.Checked;
                p.parkno = Util.Function.IntTryParse(item.SubItems[1].Text);
                p.ment = item.SubItems[2].Text;
                env.RegCarControl.Otherparks.Add(p);
            }
            env.RegCarControl.OtherparksTimeuse = chkOtherparktimeuse.Checked;
            env.RegCarControl.Otherparksstart = mskOtherparktimestart.Text;
            env.RegCarControl.Otherparksend = mskOtherparktimeend.Text;
            env.RegCarControl.Regautodeluse = chkAutoregdel.Checked;
            env.RegCarControl.Regautodeltime = mskAutoregdeltime.Text;
            env.RegCarControl.Regendnotiuse = chkRegendnotiuse.Checked;
            env.RegCarControl.Regendnotiday = cmbRegendnotiterm.Text;
            env.RegCarControl.Penaltiuse = chkusePenalty.Checked;
            env.RegCarControl.Penaltiment = txtPenaltyment.Text;
            env.RegCarControl.Ilotarea = chkiLotarea.Checked;

            env.RegCarControl.UseGroupGate = chkGateGroupUse.Checked;
            int itmp = 0;
            int.TryParse(txtGroupNo.Text, out itmp);
            env.RegCarControl.GateGroupNo = itmp;

            for (int i = 0; i < lstGroup.Items.Count; i++)
            {
                env.RegCarControl.GateGroupName[i] = lstGroup.Items[i].SubItems[2].Text;
                env.RegCarControl.GroupMent[i] = lstGroup.Items[i].SubItems[3].Text;
                env.RegCarControl.GroupUse[i] = lstGroup.Items[i].Checked;
            }
            env.RegCarControl.GroupUseTime = chkGroupTimeUse.Checked;
            env.RegCarControl.GroupStart = mskGroupFrom.Text;
            env.RegCarControl.GroupEnd = mskGroupTo.Text;
            env.RegCarControl.Save(env.RegCarControl);
            #endregion
        }

        private void frmEnv_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode.Equals(Keys.Escape))
                this.Close();
        }

        private void tabControl1_TabIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.TabIndex.Equals(1))
                btnCam1.PerformClick();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab.Text.Equals("카메라설정"))
                btnCam1.PerformClick();
        }

        private void trackAECTarget1_Scroll(object sender, EventArgs e)
        {
            trackAECTarget1Value.Text = trackAECTarget1.Value.ToString();
        }

        private void trackAECTarget2_Scroll(object sender, EventArgs e)
        {
            trackAECTarget2Value.Text = trackAECTarget2.Value.ToString();
        }

        private void trackAECTarget3_Scroll(object sender, EventArgs e)
        {
            trackAECTarget3Value.Text = trackAECTarget3.Value.ToString();
        }

        private void chkAEC1_CheckedChanged(object sender, EventArgs e)
        {
            txtAECRangeMax1.Enabled = chkAEC1.Checked;
            txtAECRangeMin1.Enabled = chkAEC1.Checked;
        }

        private void chkAGC1_CheckedChanged(object sender, EventArgs e)
        {
            txtAGCRangeMax1.Enabled = chkAEC1.Checked;
            txtAGCRangeMin1.Enabled = chkAEC1.Checked;
        }

        private void chkAEC2_CheckedChanged(object sender, EventArgs e)
        {
            txtAECRangeMax2.Enabled = chkAEC2.Checked;
            txtAECRangeMin2.Enabled = chkAEC2.Checked;
        }

        private void chkAGC2_CheckedChanged(object sender, EventArgs e)
        {
            txtAGCRangeMax2.Enabled = chkAEC2.Checked;
            txtAGCRangeMin2.Enabled = chkAEC2.Checked;
        }

        private void chkAEC3_CheckedChanged(object sender, EventArgs e)
        {
            txtAECRangeMax3.Enabled = chkAEC3.Checked;
            txtAECRangeMin3.Enabled = chkAEC3.Checked;
        }

        private void chkAGC3_CheckedChanged(object sender, EventArgs e)
        {
            txtAGCRangeMax3.Enabled = chkAEC3.Checked;
            txtAGCRangeMin3.Enabled = chkAEC3.Checked;
        }

        private void SubCaminfoTextClear(Control containerControl)
        {
            List<Control> allControls = new List<Control>();

            foreach (Control control in containerControl.Controls)
            {
                switch (control.GetType().ToString())
                {
                    case "System.Windows.Forms.TextBox":
                    case "System.Windows.Forms.MaskedTextBox":
                        control.Text = string.Empty;
                        //Console.WriteLine(control.Name);
                        break;
                }
                //만일 자식 컨트롤이 또 다른 자식 컨트롤을 가지고 있다면…
                if (control.Controls.Count > 0)
                {
                    //자신을 재귀적으로 호출한다
                    SubCaminfoTextClear(control);
                }
            }
        }

        private void getEnv()
        {
            #region 기본설정
            env.TestMode = chkTestMod.Checked;
            env.CommonEnv.DBInfo.Ip = txtServer.Text;
            env.CommonEnv.DBInfo.Id = txtID.Text;
            env.CommonEnv.DBInfo.Pw = txtPW.Text;
            env.CommonEnv.DBInfo.MstDB = txtMDB.Text;
            env.CommonEnv.DBInfo.TrnsDb = txtTDB.Text;

            env.CommunicationEnv.ParkInfo.No = Util.Function.IntTryParse(txtParkNo.Text);
            env.CommunicationEnv.ParkInfo.Ext_No = Util.Function.IntTryParse(txtParkExtNo.Text);
            env.CommunicationEnv.ParkInfo.Client_No = Util.Function.IntTryParse(txtParkPCNo.Text);

            if (chkNumberOnly4digit.Checked)
                env.CommunicationEnv.RegCorrection = 1;
            else if (chkNumberOnly6digit.Checked)
                env.CommunicationEnv.RegCorrection = 2;
            else
                env.CommunicationEnv.RegCorrection = 0;

            env.CommunicationEnv.ImageSave.Use = ckbImageUse.Checked;
            env.CommunicationEnv.ImageSave.SavePath = txtComSavePath.Text;

            //동작모드(공통) : 카메라서버/자료처리/카메라서버+자료처리
            if (rdStartCam.Checked)
                env.StartType = (int)ClsStructure.ProgramStartType.CAM;
            else if (rdStartCom.Checked)
                env.StartType = (int)ClsStructure.ProgramStartType.COM;
            else if (rdStartBoth.Checked)
                env.StartType = (int)ClsStructure.ProgramStartType.BOTH;

            FullSpaceControl.Manual = chkManualFullControl.Checked;
            FullSpaceControl.Period = chkFullPeriodControl.Checked;
            FullSpaceControl.EntGateOpen = chkFullReaseGateOpen.Checked;

            GetMasterInfo.Use = chkGetMst.Checked;
            GetMasterInfo.SharePath = txtGetMstPath.Text;
            GetMasterInfo.Term = Util.Function.IntTryParse(txtGetMstTerm.Text);
            #endregion

            #region 부제 설정
            NoDriving.Use = chkNoDrivingUse.Checked;
            if (rdbNoDriving2.Checked)
                NoDriving.Option = NoDrive.Type2;
            else if (rdbNoDriving5.Checked)
                NoDriving.Option = NoDrive.Type5;
            else if (rdbNoDriving10.Checked)
                NoDriving.Option = NoDrive.Type10;
            else if (rdbNoDriving67.Checked)
                NoDriving.Option = NoDrive.TypeDayOfWeek;
            NoDriving.WriteLpr = chkNoDrivingLpr.Checked;
            NoDriving.DisPlay = chkNoDrivingDisPlay.Checked;
            NoDriving.Exception = chkNoDrivingException.Checked;
            NoDriving.Ment1 = txtNoDriveMent1.Text;
            NoDriving.Ment2 = txtNoDriveMent2.Text;
            NoDriving.Color1 = cmbNoDriveColor1.Text;
            NoDriving.Color2 = cmbNoDriveColor2.Text;
            #endregion

            #region 카메라설정
            //leess iNova2추가
            env.CameraEnv.iNovaType = cmbCameraType.SelectedIndex + 1;

            if(groupBox1.Text.Substring(0, 1).Equals("1"))
            {
                env.CameraEnv.IPCamera1Info.Use = chkCamUse.Checked;
                env.CameraEnv.IPCamera1Info.IP = txtCamIp.Text;
                env.CameraEnv.IPCamera1Info.ChName = cmbChName.Text;
                env.CameraEnv.IPCamera1Info.StreamUdp = rdUdpStream.Checked;

                env.CameraEnv.IPCamera1Info.User_Setting_Resend_Interval = Util.Function.IntTryParse(txtBasicInterval.Text);
                env.CameraEnv.IPCamera1Info.User_Setting[0].use = ChkUseTime1.Checked;
                env.CameraEnv.IPCamera1Info.User_Setting[0].StartTime = MskStartTime1.Text;
                env.CameraEnv.IPCamera1Info.User_Setting[0].EndTime = MskEndTime1.Text;
                env.CameraEnv.IPCamera1Info.User_Setting[0].Exposuer = Util.Function.IntTryParse(txtTimeBright1.Text);
                env.CameraEnv.IPCamera1Info.User_Setting[0].ModeIdx = CmbBracket1.SelectedIndex;
                env.CameraEnv.IPCamera1Info.User_Setting[0].UseBarkect = chkMode1Bra.Checked;
                env.CameraEnv.IPCamera1Info.User_Setting[0].UseALC = chkMode1Alc.Checked;

                env.CameraEnv.IPCamera1Info.User_Setting[1].use = ChkUseTime2.Checked;
                env.CameraEnv.IPCamera1Info.User_Setting[1].StartTime = MskStartTime2.Text;
                env.CameraEnv.IPCamera1Info.User_Setting[1].EndTime = MskEndTime2.Text;
                env.CameraEnv.IPCamera1Info.User_Setting[1].Exposuer = Util.Function.IntTryParse(txtTimeBright2.Text);
                env.CameraEnv.IPCamera1Info.User_Setting[1].ModeIdx = CmbBracket2.SelectedIndex;
                env.CameraEnv.IPCamera1Info.User_Setting[1].UseBarkect = chkMode2Bra.Checked;
                env.CameraEnv.IPCamera1Info.User_Setting[1].UseALC = chkMode2Alc.Checked;

                env.CameraEnv.IPCamera1Info.User_Setting[2].use = ChkUseTime3.Checked;
                env.CameraEnv.IPCamera1Info.User_Setting[2].StartTime = MskStartTime3.Text;
                env.CameraEnv.IPCamera1Info.User_Setting[2].EndTime = MskEndTime3.Text;
                env.CameraEnv.IPCamera1Info.User_Setting[2].Exposuer = Util.Function.IntTryParse(txtTimeBright3.Text);
                env.CameraEnv.IPCamera1Info.User_Setting[2].ModeIdx = CmbBracket3.SelectedIndex;
                env.CameraEnv.IPCamera1Info.User_Setting[2].UseBarkect = chkMode3Bra.Checked;
                env.CameraEnv.IPCamera1Info.User_Setting[2].UseALC = chkMode3Alc.Checked;
                //env.CameraEnv.IPCamera1Info.ImageSave.EtcSave = chkEtcImageSave.Checked;
                //env.CameraEnv.IPCamera1Info.ImageSave.EtcPath = txtEtcImagePath.Text;

                env.CameraEnv.IPCamera1Info.DioInPut.LoopPort = Util.Function.IntTryParse(cmbLoop.Text);
                env.CameraEnv.IPCamera1Info.DioInPut.SmallCar = chkSmallCar.Checked;
                env.CameraEnv.IPCamera1Info.DioInPut.SmallPort = Util.Function.IntTryParse(cmbSmallCar.Text);

                env.CameraEnv.IPCamera1Info.User_Brakect[0, 0].Exposure = Util.Function.IntTryParse(txtTimeExposure11.Text);
                env.CameraEnv.IPCamera1Info.User_Brakect[0, 0].AnalogGain = trackAGain11.Value;
                env.CameraEnv.IPCamera1Info.User_Brakect[0, 0].DigitalGain = trackDGain11.Value;

                env.CameraEnv.IPCamera1Info.User_Brakect[0, 1].Exposure = Util.Function.IntTryParse(txtTimeExposure12.Text);
                env.CameraEnv.IPCamera1Info.User_Brakect[0, 1].AnalogGain = trackAGain12.Value;
                env.CameraEnv.IPCamera1Info.User_Brakect[0, 1].DigitalGain = trackDGain12.Value;

                env.CameraEnv.IPCamera1Info.User_Brakect[0, 2].Exposure = Util.Function.IntTryParse(txtTimeExposure13.Text);
                env.CameraEnv.IPCamera1Info.User_Brakect[0, 2].AnalogGain = trackAGain13.Value;
                env.CameraEnv.IPCamera1Info.User_Brakect[0, 2].DigitalGain = trackDGain13.Value;

                env.CameraEnv.IPCamera1Info.User_Brakect[0, 3].Exposure = Util.Function.IntTryParse(txtTimeExposure14.Text);
                env.CameraEnv.IPCamera1Info.User_Brakect[0, 3].AnalogGain = trackAGain14.Value;
                env.CameraEnv.IPCamera1Info.User_Brakect[0, 3].DigitalGain = trackDGain14.Value;

                env.CameraEnv.IPCamera1Info.User_Brakect[1, 0].Exposure = Util.Function.IntTryParse(txtTimeExposure21.Text);
                env.CameraEnv.IPCamera1Info.User_Brakect[1, 0].AnalogGain = trackAGain21.Value;
                env.CameraEnv.IPCamera1Info.User_Brakect[1, 0].DigitalGain = trackDGain21.Value;

                env.CameraEnv.IPCamera1Info.User_Brakect[1, 1].Exposure = Util.Function.IntTryParse(txtTimeExposure22.Text);
                env.CameraEnv.IPCamera1Info.User_Brakect[1, 1].AnalogGain = trackAGain22.Value;
                env.CameraEnv.IPCamera1Info.User_Brakect[1, 1].DigitalGain = trackDGain22.Value;

                env.CameraEnv.IPCamera1Info.User_Brakect[1, 2].Exposure = Util.Function.IntTryParse(txtTimeExposure23.Text);
                env.CameraEnv.IPCamera1Info.User_Brakect[1, 2].AnalogGain = trackAGain23.Value;
                env.CameraEnv.IPCamera1Info.User_Brakect[1, 2].DigitalGain = trackDGain23.Value;

                env.CameraEnv.IPCamera1Info.User_Brakect[1, 3].Exposure = Util.Function.IntTryParse(txtTimeExposure24.Text);
                env.CameraEnv.IPCamera1Info.User_Brakect[1, 3].AnalogGain = trackAGain24.Value;
                env.CameraEnv.IPCamera1Info.User_Brakect[1, 3].DigitalGain = trackDGain24.Value;

                env.CameraEnv.IPCamera1Info.User_Brakect[2, 0].Exposure = Util.Function.IntTryParse(txtTimeExposure31.Text);
                env.CameraEnv.IPCamera1Info.User_Brakect[2, 0].AnalogGain = trackAGain31.Value;
                env.CameraEnv.IPCamera1Info.User_Brakect[2, 0].DigitalGain = trackDGain31.Value;

                env.CameraEnv.IPCamera1Info.User_Brakect[2, 1].Exposure = Util.Function.IntTryParse(txtTimeExposure32.Text);
                env.CameraEnv.IPCamera1Info.User_Brakect[2, 1].AnalogGain = trackAGain32.Value;
                env.CameraEnv.IPCamera1Info.User_Brakect[2, 1].DigitalGain = trackDGain32.Value;

                env.CameraEnv.IPCamera1Info.User_Brakect[2, 2].Exposure = Util.Function.IntTryParse(txtTimeExposure33.Text);
                env.CameraEnv.IPCamera1Info.User_Brakect[2, 2].AnalogGain = trackAGain33.Value;
                env.CameraEnv.IPCamera1Info.User_Brakect[2, 2].DigitalGain = trackDGain33.Value;

                env.CameraEnv.IPCamera1Info.User_Brakect[2, 3].Exposure = Util.Function.IntTryParse(txtTimeExposure34.Text);
                env.CameraEnv.IPCamera1Info.User_Brakect[2, 3].AnalogGain = trackAGain34.Value;
                env.CameraEnv.IPCamera1Info.User_Brakect[2, 3].DigitalGain = trackDGain34.Value;

                env.CameraEnv.IPCamera1Info.User_Alc[0].target = trackAECTarget1.Value;
                env.CameraEnv.IPCamera1Info.User_Alc[0].AECInfo.enableAEC = chkAEC1.Checked;
                env.CameraEnv.IPCamera1Info.User_Alc[0].AECInfo.minExposure = Util.Function.IntTryParse(txtAECRangeMin1.Text);
                env.CameraEnv.IPCamera1Info.User_Alc[0].AECInfo.maxExposure = Util.Function.IntTryParse(txtAECRangeMax1.Text);
                env.CameraEnv.IPCamera1Info.User_Alc[0].AGCInfo.enableAGC = chkAGC1.Checked;
                env.CameraEnv.IPCamera1Info.User_Alc[0].AGCInfo.minGain = Util.Function.IntTryParse(txtAGCRangeMin1.Text);
                env.CameraEnv.IPCamera1Info.User_Alc[0].AGCInfo.maxGain = Util.Function.IntTryParse(txtAGCRangeMax1.Text);

                env.CameraEnv.IPCamera1Info.User_Alc[1].target = trackAECTarget2.Value;
                env.CameraEnv.IPCamera1Info.User_Alc[1].AECInfo.enableAEC = chkAEC2.Checked;
                env.CameraEnv.IPCamera1Info.User_Alc[1].AECInfo.minExposure = Util.Function.IntTryParse(txtAECRangeMin2.Text);
                env.CameraEnv.IPCamera1Info.User_Alc[1].AECInfo.maxExposure = Util.Function.IntTryParse(txtAECRangeMax2.Text);
                env.CameraEnv.IPCamera1Info.User_Alc[1].AGCInfo.enableAGC = chkAGC2.Checked;
                env.CameraEnv.IPCamera1Info.User_Alc[1].AGCInfo.minGain = Util.Function.IntTryParse(txtAGCRangeMin2.Text);
                env.CameraEnv.IPCamera1Info.User_Alc[1].AGCInfo.maxGain = Util.Function.IntTryParse(txtAGCRangeMax2.Text);

                env.CameraEnv.IPCamera1Info.User_Alc[2].target = trackAECTarget3.Value;
                env.CameraEnv.IPCamera1Info.User_Alc[2].AECInfo.enableAEC = chkAEC3.Checked;
                env.CameraEnv.IPCamera1Info.User_Alc[2].AECInfo.minExposure = Util.Function.IntTryParse(txtAECRangeMin3.Text);
                env.CameraEnv.IPCamera1Info.User_Alc[2].AECInfo.maxExposure = Util.Function.IntTryParse(txtAECRangeMax3.Text);
                env.CameraEnv.IPCamera1Info.User_Alc[2].AGCInfo.enableAGC = chkAGC3.Checked;
                env.CameraEnv.IPCamera1Info.User_Alc[2].AGCInfo.minGain = Util.Function.IntTryParse(txtAGCRangeMin3.Text);
                env.CameraEnv.IPCamera1Info.User_Alc[2].AGCInfo.maxGain = Util.Function.IntTryParse(txtAGCRangeMax3.Text);

                env.CameraEnv.IPCamera1Info.TriggerCnt = Util.Function.IntTryParse(cmbTriggerCnt.Text);
                env.CameraEnv.IPCamera1Info.BarkectCnt = Util.Function.IntTryParse(cmbBrakectCnt.Text);
                env.CameraEnv.IPCamera1Info.TriggerMode = cmbTriggerMode.SelectedIndex;
                env.CameraEnv.IPCamera1Info.FrameRate = Util.Function.IntTryParse(txtFrameRate.Text);
            }
            else if (groupBox1.Text.Substring(0, 1).Equals("2"))
            {
                env.CameraEnv.IPCamera2Info.Use = chkCamUse.Checked;
                env.CameraEnv.IPCamera2Info.IP = txtCamIp.Text;
                env.CameraEnv.IPCamera2Info.ChName = cmbChName.Text;
                env.CameraEnv.IPCamera2Info.StreamUdp = rdUdpStream.Checked;

                env.CameraEnv.IPCamera2Info.User_Setting_Resend_Interval = Util.Function.IntTryParse(txtBasicInterval.Text);
                env.CameraEnv.IPCamera2Info.User_Setting[0].use = ChkUseTime1.Checked;
                env.CameraEnv.IPCamera2Info.User_Setting[0].StartTime = MskStartTime1.Text;
                env.CameraEnv.IPCamera2Info.User_Setting[0].EndTime = MskEndTime1.Text;
                env.CameraEnv.IPCamera2Info.User_Setting[0].Exposuer = Util.Function.IntTryParse(txtTimeBright1.Text);
                env.CameraEnv.IPCamera2Info.User_Setting[0].ModeIdx = CmbBracket1.SelectedIndex;
                env.CameraEnv.IPCamera2Info.User_Setting[0].UseBarkect = chkMode1Bra.Checked;
                env.CameraEnv.IPCamera2Info.User_Setting[0].UseALC = chkMode1Alc.Checked;

                env.CameraEnv.IPCamera2Info.User_Setting[1].use = ChkUseTime2.Checked;
                env.CameraEnv.IPCamera2Info.User_Setting[1].StartTime = MskStartTime2.Text;
                env.CameraEnv.IPCamera2Info.User_Setting[1].EndTime = MskEndTime2.Text;
                env.CameraEnv.IPCamera2Info.User_Setting[1].Exposuer = Util.Function.IntTryParse(txtTimeBright2.Text);
                env.CameraEnv.IPCamera2Info.User_Setting[1].ModeIdx = CmbBracket2.SelectedIndex;
                env.CameraEnv.IPCamera2Info.User_Setting[1].UseBarkect = chkMode2Bra.Checked;
                env.CameraEnv.IPCamera2Info.User_Setting[1].UseALC = chkMode2Alc.Checked;

                env.CameraEnv.IPCamera2Info.User_Setting[2].use = ChkUseTime3.Checked;
                env.CameraEnv.IPCamera2Info.User_Setting[2].StartTime = MskStartTime3.Text;
                env.CameraEnv.IPCamera2Info.User_Setting[2].EndTime = MskEndTime3.Text;
                env.CameraEnv.IPCamera2Info.User_Setting[2].Exposuer = Util.Function.IntTryParse(txtTimeBright3.Text);
                env.CameraEnv.IPCamera2Info.User_Setting[2].ModeIdx = CmbBracket3.SelectedIndex;
                env.CameraEnv.IPCamera2Info.User_Setting[2].UseBarkect = chkMode3Bra.Checked;
                env.CameraEnv.IPCamera2Info.User_Setting[2].UseALC = chkMode3Alc.Checked;
                //env.CameraEnv.IPCamera2Info.ImageSave.EtcSave = chkEtcImageSave.Checked;
                //env.CameraEnv.IPCamera2Info.ImageSave.EtcPath = txtEtcImagePath.Text;

                env.CameraEnv.IPCamera2Info.DioInPut.LoopPort = Util.Function.IntTryParse(cmbLoop.Text);
                env.CameraEnv.IPCamera2Info.DioInPut.SmallCar = chkSmallCar.Checked;
                env.CameraEnv.IPCamera2Info.DioInPut.SmallPort = Util.Function.IntTryParse(cmbSmallCar.Text);

                env.CameraEnv.IPCamera2Info.User_Brakect[0, 0].Exposure = Util.Function.IntTryParse(txtTimeExposure11.Text);
                env.CameraEnv.IPCamera2Info.User_Brakect[0, 0].AnalogGain = trackAGain11.Value;
                env.CameraEnv.IPCamera2Info.User_Brakect[0, 0].DigitalGain = trackDGain11.Value;

                env.CameraEnv.IPCamera2Info.User_Brakect[0, 1].Exposure = Util.Function.IntTryParse(txtTimeExposure12.Text);
                env.CameraEnv.IPCamera2Info.User_Brakect[0, 1].AnalogGain = trackAGain12.Value;
                env.CameraEnv.IPCamera2Info.User_Brakect[0, 1].DigitalGain = trackDGain12.Value;

                env.CameraEnv.IPCamera2Info.User_Brakect[0, 2].Exposure = Util.Function.IntTryParse(txtTimeExposure13.Text);
                env.CameraEnv.IPCamera2Info.User_Brakect[0, 2].AnalogGain = trackAGain13.Value;
                env.CameraEnv.IPCamera2Info.User_Brakect[0, 2].DigitalGain = trackDGain13.Value;

                env.CameraEnv.IPCamera2Info.User_Brakect[0, 3].Exposure = Util.Function.IntTryParse(txtTimeExposure14.Text);
                env.CameraEnv.IPCamera2Info.User_Brakect[0, 3].AnalogGain = trackAGain14.Value;
                env.CameraEnv.IPCamera2Info.User_Brakect[0, 3].DigitalGain = trackDGain14.Value;

                env.CameraEnv.IPCamera2Info.User_Brakect[1, 0].Exposure = Util.Function.IntTryParse(txtTimeExposure21.Text);
                env.CameraEnv.IPCamera2Info.User_Brakect[1, 0].AnalogGain = trackAGain21.Value;
                env.CameraEnv.IPCamera2Info.User_Brakect[1, 0].DigitalGain = trackDGain21.Value;

                env.CameraEnv.IPCamera2Info.User_Brakect[1, 1].Exposure = Util.Function.IntTryParse(txtTimeExposure22.Text);
                env.CameraEnv.IPCamera2Info.User_Brakect[1, 1].AnalogGain = trackAGain22.Value;
                env.CameraEnv.IPCamera2Info.User_Brakect[1, 1].DigitalGain = trackDGain22.Value;

                env.CameraEnv.IPCamera2Info.User_Brakect[1, 2].Exposure = Util.Function.IntTryParse(txtTimeExposure23.Text);
                env.CameraEnv.IPCamera2Info.User_Brakect[1, 2].AnalogGain = trackAGain23.Value;
                env.CameraEnv.IPCamera2Info.User_Brakect[1, 2].DigitalGain = trackDGain23.Value;

                env.CameraEnv.IPCamera2Info.User_Brakect[1, 3].Exposure = Util.Function.IntTryParse(txtTimeExposure24.Text);
                env.CameraEnv.IPCamera2Info.User_Brakect[1, 3].AnalogGain = trackAGain24.Value;
                env.CameraEnv.IPCamera2Info.User_Brakect[1, 3].DigitalGain = trackDGain24.Value;

                env.CameraEnv.IPCamera2Info.User_Brakect[2, 0].Exposure = Util.Function.IntTryParse(txtTimeExposure31.Text);
                env.CameraEnv.IPCamera2Info.User_Brakect[2, 0].AnalogGain = trackAGain31.Value;
                env.CameraEnv.IPCamera2Info.User_Brakect[2, 0].DigitalGain = trackDGain31.Value;

                env.CameraEnv.IPCamera2Info.User_Brakect[2, 1].Exposure = Util.Function.IntTryParse(txtTimeExposure32.Text);
                env.CameraEnv.IPCamera2Info.User_Brakect[2, 1].AnalogGain = trackAGain32.Value;
                env.CameraEnv.IPCamera2Info.User_Brakect[2, 1].DigitalGain = trackDGain32.Value;

                env.CameraEnv.IPCamera2Info.User_Brakect[2, 2].Exposure = Util.Function.IntTryParse(txtTimeExposure33.Text);
                env.CameraEnv.IPCamera2Info.User_Brakect[2, 2].AnalogGain = trackAGain33.Value;
                env.CameraEnv.IPCamera2Info.User_Brakect[2, 2].DigitalGain = trackDGain33.Value;

                env.CameraEnv.IPCamera2Info.User_Brakect[2, 3].Exposure = Util.Function.IntTryParse(txtTimeExposure34.Text);
                env.CameraEnv.IPCamera2Info.User_Brakect[2, 3].AnalogGain = trackAGain34.Value;
                env.CameraEnv.IPCamera2Info.User_Brakect[2, 3].DigitalGain = trackDGain34.Value;

                env.CameraEnv.IPCamera2Info.User_Alc[0].target = trackAECTarget1.Value;
                env.CameraEnv.IPCamera2Info.User_Alc[0].AECInfo.enableAEC = chkAEC1.Checked;
                env.CameraEnv.IPCamera2Info.User_Alc[0].AECInfo.minExposure = Util.Function.IntTryParse(txtAECRangeMin1.Text);
                env.CameraEnv.IPCamera2Info.User_Alc[0].AECInfo.maxExposure = Util.Function.IntTryParse(txtAECRangeMax1.Text);
                env.CameraEnv.IPCamera2Info.User_Alc[0].AGCInfo.enableAGC = chkAGC1.Checked;
                env.CameraEnv.IPCamera2Info.User_Alc[0].AGCInfo.minGain = Util.Function.IntTryParse(txtAGCRangeMin1.Text);
                env.CameraEnv.IPCamera2Info.User_Alc[0].AGCInfo.maxGain = Util.Function.IntTryParse(txtAGCRangeMax1.Text);

                env.CameraEnv.IPCamera2Info.User_Alc[1].target = trackAECTarget2.Value;
                env.CameraEnv.IPCamera2Info.User_Alc[1].AECInfo.enableAEC = chkAEC2.Checked;
                env.CameraEnv.IPCamera2Info.User_Alc[1].AECInfo.minExposure = Util.Function.IntTryParse(txtAECRangeMin2.Text);
                env.CameraEnv.IPCamera2Info.User_Alc[1].AECInfo.maxExposure = Util.Function.IntTryParse(txtAECRangeMax2.Text);
                env.CameraEnv.IPCamera2Info.User_Alc[1].AGCInfo.enableAGC = chkAGC2.Checked;
                env.CameraEnv.IPCamera2Info.User_Alc[1].AGCInfo.minGain = Util.Function.IntTryParse(txtAGCRangeMin2.Text);
                env.CameraEnv.IPCamera2Info.User_Alc[1].AGCInfo.maxGain = Util.Function.IntTryParse(txtAGCRangeMax2.Text);

                env.CameraEnv.IPCamera2Info.User_Alc[2].target = trackAECTarget3.Value;
                env.CameraEnv.IPCamera2Info.User_Alc[2].AECInfo.enableAEC = chkAEC3.Checked;
                env.CameraEnv.IPCamera2Info.User_Alc[2].AECInfo.minExposure = Util.Function.IntTryParse(txtAECRangeMin3.Text);
                env.CameraEnv.IPCamera2Info.User_Alc[2].AECInfo.maxExposure = Util.Function.IntTryParse(txtAECRangeMax3.Text);
                env.CameraEnv.IPCamera2Info.User_Alc[2].AGCInfo.enableAGC = chkAGC3.Checked;
                env.CameraEnv.IPCamera2Info.User_Alc[2].AGCInfo.minGain = Util.Function.IntTryParse(txtAGCRangeMin3.Text);
                env.CameraEnv.IPCamera2Info.User_Alc[2].AGCInfo.maxGain = Util.Function.IntTryParse(txtAGCRangeMax3.Text);

                env.CameraEnv.IPCamera2Info.TriggerCnt = Util.Function.IntTryParse(cmbTriggerCnt.Text);
                env.CameraEnv.IPCamera2Info.BarkectCnt = Util.Function.IntTryParse(cmbBrakectCnt.Text);
                env.CameraEnv.IPCamera2Info.TriggerMode = cmbTriggerMode.SelectedIndex;
                env.CameraEnv.IPCamera2Info.FrameRate = Util.Function.IntTryParse(txtFrameRate.Text);
            }
            env.CameraEnv.IPCamera1Info.SendStxEtx = chkCam1SendStxEtx.Checked;
            env.CameraEnv.IPCamera2Info.SendStxEtx = chkCam2SendStxEtx.Checked;
            #endregion

            #region LPR설정
            env.CameraEnv.ImageSave.SavePath = TxtImagePath.Text;
            env.CameraEnv.ImageSave.SaveTerm = Util.Function.IntTryParse(txtImageTerm.Text);
            env.CameraEnv.ImageSave.EtcSave = chkEtcImageSave.Checked;
            env.CameraEnv.ImageSave.EtcPath = txtEtcImagePath.Text;

            if (rdElwox.Checked)
                env.CameraEnv.RegModule = (int)ClsStructure.RegModule.Elwox;
            else if (rdNgis.Checked)
                env.CameraEnv.RegModule = (int)ClsStructure.RegModule.Ngis;
            else if (rdbCore.Checked)
                env.CameraEnv.RegModule = (int)ClsStructure.RegModule.CoreLogic;

            if (rdbCpu.Checked)
                env.CameraEnv.CoreType = (int)ClsStructure.CoreType.CPU;
            else if (rdbGpu.Checked)
                env.CameraEnv.CoreType = (int)ClsStructure.CoreType.GPU;
            else if (rdbMyriad.Checked)
                env.CameraEnv.CoreType = (int)ClsStructure.CoreType.MyriadVPU;

            if (rdbKor.Checked)
                env.CameraEnv.CoreCountry = CoreLogic.KOR;
            else
                env.CameraEnv.CoreCountry = CoreLogic.THA;

            if (cmbImageProcType.SelectedItem.Equals(ClsStructure.ImageProceType.번호판확인))
                env.CameraEnv.PlateArea = true;
            else if (cmbImageProcType.SelectedItem.Equals(ClsStructure.ImageProceType.이미지자르기))
                env.CameraEnv.PlateArea = false;

            env.CameraEnv.bRegCarType = chkRegCarType.Checked;

            env.CameraEnv.RegCarRate = new List<ClsStructure.SmallCarRate>();
            foreach (DataGridViewRow item in dataGridView1.Rows)
            {
                //tmp += string.Format("{0}/{1},", item.Cells[0].Value.ToString(), item.Cells[1].Value.ToString());
                if (!item.IsNewRow)
                {
                    ClsStructure.SmallCarRate rate = new ClsStructure.SmallCarRate();
                    rate.CarType = item.Cells["CarType"].Value.ToString();
                    int.TryParse(item.Cells[1].Value.ToString(), out rate.Rate);
                    env.CameraEnv.RegCarRate.Add(rate);
                }
            }

            #region LPR 장비 설정
            env.CommunicationEnv.Lpr1Info.Use = ChkLPRUse1.Checked;
            env.CommunicationEnv.Lpr1Info.EqpmNo = Util.Function.IntTryParse(txtEqpmNo1.Text);
            env.CommunicationEnv.Lpr1Info.ChNo = txtLPRNo1.Text;
            env.CommunicationEnv.Lpr1Info.Name = txtLPRName1.Text;
            env.CommunicationEnv.Lpr1Info.DevType = CmbLPRType1.SelectedIndex;
            env.CommunicationEnv.Lpr1Info.InOutType = CmbLPRInOut1.SelectedIndex;
            //env.CommunicationEnv.Lpr1Info.FreePass = ChkFreePass1.Checked;
            //env.CommunicationEnv.Lpr1Info.FreePassGateOpen = chkFreePassGateOpen1.Checked;
            env.CommunicationEnv.Lpr1Info.SockInfo.IP = txtLPRInfoIP1.Text;
            env.CommunicationEnv.Lpr1Info.SockInfo.Port = Util.Function.IntTryParse(txtLPRInfoPort1.Text);
            env.CommunicationEnv.Lpr1Info.ImagePath = txtLPRInfoPath1.Text;

            env.CommunicationEnv.Lpr2Info.Use = ChkLPRUse2.Checked;
            env.CommunicationEnv.Lpr2Info.EqpmNo = Util.Function.IntTryParse(txtEqpmNo2.Text);
            env.CommunicationEnv.Lpr2Info.ChNo = txtLPRNo2.Text;
            env.CommunicationEnv.Lpr2Info.Name = txtLPRName2.Text;
            env.CommunicationEnv.Lpr2Info.DevType = CmbLPRType2.SelectedIndex;
            env.CommunicationEnv.Lpr2Info.InOutType = CmbLPRInOut2.SelectedIndex;
            //env.CommunicationEnv.Lpr2Info.FreePass = ChkFreePass2.Checked;
            //env.CommunicationEnv.Lpr2Info.FreePassGateOpen = chkFreePassGateOpen2.Checked;
            env.CommunicationEnv.Lpr2Info.SockInfo.IP = txtLPRInfoIP2.Text;
            env.CommunicationEnv.Lpr2Info.SockInfo.Port = Util.Function.IntTryParse(txtLPRInfoPort2.Text);
            env.CommunicationEnv.Lpr2Info.ImagePath = txtLPRInfoPath2.Text;
            #endregion
            //인식실패시 차단기 처리 : 오픈
            env.CommunicationEnv.Nodetection_Open = rdGateOpen.Checked;
            #endregion

            #region 소켓설정
            if (rdK.Checked)
                env.CameraEnv.SockDataFormat = (int)ClsStructure.SockFormat.Kukje;
            else if (rdA.Checked)
                env.CameraEnv.SockDataFormat = (int)ClsStructure.SockFormat.Amano;
            else if (rdN.Checked)
                env.CameraEnv.SockDataFormat = (int)ClsStructure.SockFormat.Nexpa;
            else if (rdbOldAmano.Checked)
                env.CameraEnv.SockDataFormat = (int)ClsStructure.SockFormat.AmanoOld;

            env.CommunicationEnv.ClientTarget[0].Use = ChkNotiUse.Checked;
            env.CommunicationEnv.ClientTarget[0].IP = txtNotiIP.Text;
            env.CommunicationEnv.ClientTarget[0].Port = Util.Function.IntTryParse(txtNotiPort.Text);
            env.CommunicationEnv.ClientTarget[0].Type = Convert.ToInt16(chkNotiType.Checked);

            env.CommunicationEnv.ClientTarget[1].Use = ChkLprOutServer.Checked;
            //env.CommunicationEnv.ClientTarget[1].Type = CmbLprOutType.SelectedIndex;
            //env.CommunicationEnv.ClientTarget[1].IP = txtLprOutIp.Text;
            env.CommunicationEnv.ClientTarget[1].Port = Util.Function.IntTryParse(txtLprOutPort.Text);

            env.CommunicationEnv.ClientTarget[2].Use = ChkDisplayRelayUse.Checked;
            env.CommunicationEnv.ClientTarget[2].Type = Util.Function.IntTryParse(CmbDisplayRelayNo.Text);
            env.CommunicationEnv.ClientTarget[2].IP = txtDisplayRelayIp.Text;
            env.CommunicationEnv.ClientTarget[2].Port = Util.Function.IntTryParse(txtDisplayRelayPort.Text);

            env.CommunicationEnv.ClientTarget[3].Use = ChkStoneUse.Checked;
            env.CommunicationEnv.ClientTarget[3].IP = txtStoneIp.Text;
            env.CommunicationEnv.ClientTarget[3].Port = Util.Function.IntTryParse(txtStonePort.Text);

            env.CommunicationEnv.ClientTarget[4].Use = chkLprEntUse.Checked;
            //env.CommunicationEnv.ClientTarget[4].Type = cmbLprEntType.SelectedIndex;
            //env.CommunicationEnv.ClientTarget[4].IP = txtLprEntIp.Text;
            env.CommunicationEnv.ClientTarget[4].Port = Util.Function.IntTryParse(txtLprEntPort.Text);
            #endregion
            //Public info

            #region 차단기 설정
            env.CommonEnv.Dio.DioSetting.SerialPort = cmbDioPort.Text;
            env.CommonEnv.Dio.DioSetting.Setting = txtDioSetting.Text;
            env.CommonEnv.Dio.DioSetting.Dev_Type_Name = cmbDioType.Text;
            if (cmbBoardType.SelectedItem.Equals(ClsStructure.DeviceType.이벤트))
                env.CommonEnv.Dio.DioSetting.Type = true;
            else if (cmbBoardType.SelectedItem.Equals(ClsStructure.DeviceType.리얼))
                env.CommonEnv.Dio.DioSetting.Type = false;

            if (ChkGate1Use.Checked && Util.Function.IntTryParse(txtGate1PortKeep.Text) < 500)
                txtGate1PortKeep.Text = "500";
            if (ChkGate2Use.Checked && Util.Function.IntTryParse(txtGate2PortKeep.Text) < 500)
                txtGate2PortKeep.Text = "500";

            env.CommonEnv.Dio.DioOutPut[0].Use = ChkGate1Use.Checked;
            env.CommonEnv.Dio.DioOutPut[0].Port = Util.Function.IntTryParse(CmbGate1Port.Text);
            env.CommonEnv.Dio.DioOutPut[0].Delay = Util.Function.IntTryParse(txtGate1PortDelay.Text);
            env.CommonEnv.Dio.DioOutPut[0].Keep = Util.Function.IntTryParse(txtGate1PortKeep.Text);
            if (CmbGate1AddPort.Text.Equals(string.Empty))
                env.CommonEnv.Dio.DioOutPut[0].AddPort = -1;
            else
                env.CommonEnv.Dio.DioOutPut[0].AddPort = Util.Function.IntTryParse(CmbGate1AddPort.Text);
            env.CommonEnv.Dio.DioOutPut[0].AddDelay = Util.Function.IntTryParse(txtGate1AddPortDelay.Text);
            env.CommonEnv.Dio.DioOutPut[0].AddKeep = Util.Function.IntTryParse(txtGate1AddPortKeep.Text);

            env.CommonEnv.Dio.DioOutPut[1].Use = ChkGate2Use.Checked;
            env.CommonEnv.Dio.DioOutPut[1].Port = Util.Function.IntTryParse(CmbGate2Port.Text);
            env.CommonEnv.Dio.DioOutPut[1].Delay = Util.Function.IntTryParse(txtGate2PortDelay.Text);
            env.CommonEnv.Dio.DioOutPut[1].Keep = Util.Function.IntTryParse(txtGate2PortKeep.Text);
            if (CmbGate2AddPort.Text.Equals(string.Empty))
                env.CommonEnv.Dio.DioOutPut[1].AddPort = -1;
            else
                env.CommonEnv.Dio.DioOutPut[1].AddPort = Util.Function.IntTryParse(CmbGate2AddPort.Text);
            env.CommonEnv.Dio.DioOutPut[1].AddDelay = Util.Function.IntTryParse(txtGate2AddPortDelay.Text);
            env.CommonEnv.Dio.DioOutPut[1].AddKeep = Util.Function.IntTryParse(txtGate2AddPortKeep.Text);

            env.CommonEnv.Dio.IsolatePort.Out.Use = chkIsolateUse.Checked;
            env.CommonEnv.Dio.IsolatePort.In.LoopPort = Util.Function.IntTryParse(cmbIsolateInPort.Text);
            env.CommonEnv.Dio.IsolatePort.Out.Port = Util.Function.IntTryParse(cmbIsolateOutport.Text);
            env.CommonEnv.Dio.IsolatePort.Out.Delay = Util.Function.IntTryParse(txtIsolateDelay.Text);
            env.CommonEnv.Dio.IsolatePort.Out.Keep = Util.Function.IntTryParse(txtIsolateKeep.Text);
            env.CommonEnv.Dio.IsolatePort.Out.AddPort = Util.Function.IntTryParse(cmbIsolatePortAdd.Text);
            env.CommonEnv.Dio.IsolatePort.Out.AddDelay = Util.Function.IntTryParse(txtIsolateAddDelay.Text);
            env.CommonEnv.Dio.IsolatePort.Out.AddKeep = Util.Function.IntTryParse(txtIsolateAddKeep.Text);
            #endregion

            #region 전광판 설정
            env.CommunicationEnv.DisPlay[0].Use = ChkDisplay1Use.Checked;
            env.CommunicationEnv.DisPlay[0].Com.SerialPort = cmbDisplay1Port.Text;
            env.CommunicationEnv.DisPlay[0].Com.Setting = txtDisplay1Setting.Text;
            env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name = CmbDisplay1Type.Text;
            env.CommunicationEnv.DisPlay[0].Ment.Ment1Line = txtDisplay1Text1.Text;
            env.CommunicationEnv.DisPlay[0].Ment.Ment1Color = CmbDisplayText1Color1.Text;
            env.CommunicationEnv.DisPlay[0].Ment.Ment2Line = txtDisplay1Text2.Text;
            env.CommunicationEnv.DisPlay[0].Ment.Ment2Color = CmbDisplayText1Color2.Text;
            env.CommunicationEnv.DisPlay[0].NormalCar = txtNormalCar1.Text;
            env.CommunicationEnv.DisPlay[0].Normal1Color = CmbDisplayTextNormal1Color1.Text;
            env.CommunicationEnv.DisPlay[0].Normal2Color = CmbDisplayTextNormal1Color2.Text;
            env.CommunicationEnv.DisPlay[0].PeriodCar = txtPeriodCar1.Text;
            env.CommunicationEnv.DisPlay[0].Period1Color = CmbDisplayTextPeriod1Color1.Text;
            env.CommunicationEnv.DisPlay[0].Period2Color = CmbDisplayTextPeriod1Color2.Text;
            env.CommunicationEnv.DisPlay[0].UseFiex = chkUseFixedText1.Checked;
            env.CommunicationEnv.DisPlay[0].Net.Use = chkDisplay1NetUse.Checked;
            env.CommunicationEnv.DisPlay[0].Net.IP = txtDisplay1NetIp.Text;
            env.CommunicationEnv.DisPlay[0].Net.Port = Util.Function.IntTryParse(txtDisplay1NetPort.Text);

            env.CommunicationEnv.DisPlay[1].Use = ChkDisplay2Use.Checked;
            env.CommunicationEnv.DisPlay[1].Com.SerialPort = cmbDisplay2Port.Text;
            env.CommunicationEnv.DisPlay[1].Com.Setting = txtDisplay2Setting.Text;
            env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name = CmbDisplay2Type.Text;
            env.CommunicationEnv.DisPlay[1].Ment.Ment1Line = txtDisplay2Text1.Text;
            env.CommunicationEnv.DisPlay[1].Ment.Ment1Color = CmbDisplayText2Color1.Text;
            env.CommunicationEnv.DisPlay[1].Ment.Ment2Line = txtDisplay2Text2.Text;
            env.CommunicationEnv.DisPlay[1].Ment.Ment2Color = CmbDisplayText2Color2.Text;
            env.CommunicationEnv.DisPlay[1].NormalCar = txtNormalCar2.Text;
            env.CommunicationEnv.DisPlay[1].Normal1Color = CmbDisplayTextNormal2Color1.Text;
            env.CommunicationEnv.DisPlay[1].Normal2Color = CmbDisplayTextNormal2Color2.Text;
            env.CommunicationEnv.DisPlay[1].PeriodCar = txtPeriodCar2.Text;
            env.CommunicationEnv.DisPlay[1].Period1Color = CmbDisplayTextPeriod2Color1.Text;
            env.CommunicationEnv.DisPlay[1].Period2Color = CmbDisplayTextPeriod2Color2.Text;
            env.CommunicationEnv.DisPlay[1].UseFiex = chkUseFixedText2.Checked;
            env.CommunicationEnv.DisPlay[1].Net.Use = chkDisplay2NetUse.Checked;
            env.CommunicationEnv.DisPlay[1].Net.IP = txtDisplay2NetIp.Text;
            env.CommunicationEnv.DisPlay[1].Net.Port = Util.Function.IntTryParse(txtDisplay2NetPort.Text);

            env.CommunicationEnv.FixedMent.Ment1Line = txtFixedMent1.Text;
            env.CommunicationEnv.FixedMent.Ment1Color = cmbFixedColor1.Text;
            env.CommunicationEnv.FixedMent.Ment2Line = txtFixedMent2.Text;
            env.CommunicationEnv.FixedMent.Ment2Color = cmbFixedColor2.Text;
            env.CommunicationEnv.FixedPort = Util.Function.IntTryParse(cmbFixedPort.Text);

            env.CommunicationEnv.PeriodMent.Ment1Line = txtStop.Text;
            env.CommunicationEnv.PeriodMent.Ment2Line = txtOver.Text;
            #endregion

            #region 자료처리
            env.CommunicationEnv.Lpr1Info.LprOpt.Period_SendData = chkPCarEntSend.Checked;
            env.CommunicationEnv.Lpr1Info.LprOpt.Period_Lprtrns = chkPCarEntLprtrns.Checked;
            env.CommunicationEnv.Lpr1Info.LprOpt.Period_Passtrns = chkPCarEntPasstrns.Checked;
            env.CommunicationEnv.Lpr1Info.LprOpt.Period_Counter = chkPCarEntCountting.Checked;
            env.CommunicationEnv.Lpr1Info.LprOpt.Period_Gate = chkPCarEntGate.Checked;
            env.CommunicationEnv.Lpr1Info.LprOpt.Normal_SendData = chkNCarEntSend.Checked;
            env.CommunicationEnv.Lpr1Info.LprOpt.Normal_Lprtrns = chkNCarEntLprtrns.Checked;
            env.CommunicationEnv.Lpr1Info.LprOpt.Normal_Tckttrns = chkNCarEntTckttrns.Checked;
            env.CommunicationEnv.Lpr1Info.LprOpt.Normal_Counter = chkNCarEntCountting.Checked;
            env.CommunicationEnv.Lpr1Info.LprOpt.Normal_Gate = chkNCarEntGate.Checked;
            env.CommunicationEnv.Lpr2Info.LprOpt.Period_SendData = chkPCarExitSend.Checked;
            env.CommunicationEnv.Lpr2Info.LprOpt.Period_Lprtrns = chkPCarExitLprtrns.Checked;
            env.CommunicationEnv.Lpr2Info.LprOpt.Period_Passtrns = chkPCarExitPasstrns.Checked;
            env.CommunicationEnv.Lpr2Info.LprOpt.Period_Counter = chkPCarExitCountting.Checked;
            env.CommunicationEnv.Lpr2Info.LprOpt.Period_Gate = chkPCarExitGate.Checked;
            env.CommunicationEnv.Lpr2Info.LprOpt.Normal_SendData = chkNCarExitSend.Checked;
            env.CommunicationEnv.Lpr2Info.LprOpt.Normal_Lprtrns = chkNCarExitLprtrns.Checked;
            env.CommunicationEnv.Lpr2Info.LprOpt.Normal_Tckttrns = chkNCarExitTckttrns.Checked;
            env.CommunicationEnv.Lpr2Info.LprOpt.Normal_Counter = chkNCarExitCountting.Checked;
            env.CommunicationEnv.Lpr2Info.LprOpt.Normal_Gate = chkNCarExitGate.Checked;
            env.CommunicationEnv.ReturnCar.Use = chkUseReturn.Checked;
            env.CommunicationEnv.ReturnCar.Term = Util.Function.IntTryParse(txtReturnTerm.Text);
            env.CommunicationEnv.ReturnCar.Ment = txtReturnMent.Text;
            #endregion

            //env.DupTerm = Util.Function.IntTryParse(txtCustomerInterval.Text);
            env.SendOffice = chkSendOffice.Checked;
            DelayReg.Delay = chkRegDelayUse.Checked;
            DelayReg.DelayTerm = Util.Function.IntTryParse(txtRegDelayTerm.Text);

            DelayReg.Duplicate = chkDuplicateUse.Checked;
            DelayReg.Duplicate_Term = Util.Function.IntTryParse(txtDuplicateTerm.Text);

            BeforeCalOpt.Use = chkBeforeCalUse.Checked;
            BeforeCalOpt.LagTime = Util.Function.IntTryParse(txtBeforeCalLag.Text);

            if (cmbSpeciaGroup.Text.IndexOf(']') > 2)
                SpecialGroup.GroupIdx = Util.Function.IntTryParse(cmbSpeciaGroup.Text.Substring(1, cmbSpeciaGroup.Text.IndexOf(']') - 1));
            else
                SpecialGroup.GroupIdx = -1;

            #region 정기권 제한
            #endregion

            //leess 긴급차량 개방
            env.EmergencyCar = checkEmergencyCar.Checked;
        }

        private void btnEtcImagePath_Click(object sender, EventArgs e)
        {
            txtEtcImagePath.Text = folder();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Equals(Util.Function.Authentication()))
            {
                Util.Function.WriteAuthentication();
                gbAuthentication.Visible = false;
            }
        }

        private void rdElwox_CheckedChanged(object sender, EventArgs e)
        {
            cmbImageProcType.Enabled = rdElwox.Checked;
        }

        private void cmbDioType_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbLoop.Items.Clear();
            cmbSmallCar.Items.Clear();
            CmbGate1Port.Items.Clear();
            CmbGate2Port.Items.Clear();
            CmbGate1AddPort.Items.Clear();
            CmbGate2AddPort.Items.Clear();
            cmbFixedPort.Items.Clear();
            cmbIsolateOutport.Items.Clear();
            cmbIsolatePortAdd.Items.Clear();
            cmbIsolateInPort.Items.Clear();
            switch ((int) cmbDioType.SelectedItem)
            {
                case (int)ClsStructure.DeviceList.KJC1000:
                    cmbLoop.Items.AddRange(new string[] { "1", "2", "3", "4" });
                    cmbSmallCar.Items.AddRange(new string[] { "1", "2", "3", "4" });
                    CmbGate1Port.Items.AddRange(new string[] { "1", "2", "3", "4" });
                    CmbGate2Port.Items.AddRange(new string[] { "1", "2", "3", "4" });
                    CmbGate1AddPort.Items.AddRange(new string[] { "", "1", "2", "3", "4" });
                    CmbGate2AddPort.Items.AddRange(new string[] { "", "1", "2", "3", "4" });
                    cmbFixedPort.Items.AddRange(new string[] { "1", "2", "3", "4" });
                    cmbIsolateOutport.Items.AddRange(new string[] { "1", "2", "3", "4" });
                    cmbIsolatePortAdd.Items.AddRange(new string[] { "1", "2", "3", "4" });
                    cmbIsolateInPort.Items.AddRange(new string[] { "1", "2", "3" });
                    break;
                case (int)ClsStructure.DeviceList.REALSYS:
                    cmbLoop.Items.AddRange(new string[] { "0", "1", "2", "3", "4", "5", "6" });
                    cmbSmallCar.Items.AddRange(new string[] { "0", "1", "2", "3", "4", "5", "6" });
                    CmbGate1Port.Items.AddRange(new string[] { "0", "1", "2", "3", "4", "5", "6" });
                    CmbGate2Port.Items.AddRange(new string[] { "0", "1", "2", "3", "4", "5", "6" });
                    CmbGate1AddPort.Items.AddRange(new string[] { "", "0", "1", "2", "3", "4", "5", "6" });
                    CmbGate2AddPort.Items.AddRange(new string[] { "", "0", "1", "2", "3", "4", "5", "6" });
                    cmbFixedPort.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6" });
                    break;
            }
        }

        private void chkSmallCar_CheckedChanged(object sender, EventArgs e)
        {
            if (groupBox1.Text.Substring(0, 1).Equals("1"))
            {
                if (env.CameraEnv.IPCamera2Info.DioInPut.SmallCar)
                    if (chkSmallCar.Checked)
                        env.CameraEnv.IPCamera2Info.DioInPut.SmallCar = false;
            }
            else if (groupBox1.Text.Substring(0, 1).Equals("2"))
            {
                if (env.CameraEnv.IPCamera1Info.DioInPut.SmallCar)
                    if (chkSmallCar.Checked)
                        env.CameraEnv.IPCamera1Info.DioInPut.SmallCar = false;
            }
        }

        private void chkModeBra_Alc_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            if (!chk.Checked) return;
            switch (chk.Name)
            {
                case "chkMode1Bra":
                    chkMode1Alc.Checked = false;
                    break;
                case "chkMode1Alc":
                    chkMode1Bra.Checked = false;
                    break;
                case "chkMode2Bra":
                    chkMode2Alc.Checked = false;
                    break;
                case "chkMode2Alc":
                    chkMode2Bra.Checked = false;
                    break;
                case "chkMode3Bra":
                    chkMode3Alc.Checked = false;
                    break;
                case "chkMode3Alc":
                    chkMode3Bra.Checked = false;
                    break;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start("http://" + txtCamIp.Text);
        }

        private void cmbTriggerCnt_SelectedIndexChanged(object sender, EventArgs e)
        {
            //leess iNova2추가
            if(env.CameraEnv.iNovaType == 1) {
                IPCamera cam = new IPCamera();
                switch(groupBox1.Text.Substring(0, 1)) {
                    case "1":
                        cam = Cam1;
                        break;
                    case "2":
                        cam = Cam2;
                        break;
                }
                int cnt = 0;
                int.TryParse(cmbTriggerCnt.Text, out cnt);
                cam.SetTriggerImageCount(cnt);
            } else if(env.CameraEnv.iNovaType == 2) {
                iNova2.IPCamera cam = new iNova2.IPCamera();
                switch(groupBox1.Text.Substring(0, 1)) {
                    case "1":
                        cam = Cam1_iNova2;
                        break;
                    case "2":
                        cam = Cam2_iNova2;
                        break;
                }
                int cnt = 0;
                int.TryParse(cmbTriggerCnt.Text, out cnt);
                cam.SetTriggerImageCount(cnt);
            }
        }

        private void cmbBrakectCnt_SelectedIndexChanged(object sender, EventArgs e)
        {
            //leess iNova2추가
            if(env.CameraEnv.iNovaType == 1) {
                IPCamera cam = new IPCamera();
                switch(groupBox1.Text.Substring(0, 1)) {
                    case "1":
                        cam = Cam1;
                        break;
                    case "2":
                        cam = Cam2;
                        break;
                }
                int cnt = 0;
                bool brakect = false;
                cam.GetBracketMode(out brakect, out cnt);
                int.TryParse(cmbBrakectCnt.Text, out cnt);
                if(!brakect)
                    cam.SetBracketMode(true, cnt);
                cam.SetBracketMode(brakect, cnt);
            } else if(env.CameraEnv.iNovaType == 2) {
                iNova2.IPCamera cam = new iNova2.IPCamera();
                switch(groupBox1.Text.Substring(0, 1)) {
                    case "1":
                        cam = Cam1_iNova2;
                        break;
                    case "2":
                        cam = Cam2_iNova2;
                        break;
                }
                int cnt = 0;
                bool brakect = false;
                cam.GetBracketMode(out brakect, out cnt);
                int.TryParse(cmbBrakectCnt.Text, out cnt);
                if(!brakect)
                    cam.SetBracketMode(true, cnt);
                cam.SetBracketMode(brakect, cnt);
            }
        }

        private void cmbTriggerMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            //leess iNova2추가
            if(env.CameraEnv.iNovaType == 1) {
                IPCamera cam = new IPCamera();
                switch(groupBox1.Text.Substring(0, 1)) {
                    case "1":
                        cam = Cam1;
                        break;
                    case "2":
                        cam = Cam2;
                        break;
                }
                int cnt = 0;
                bool hight = false;
                cam.GetBracketMode(out hight, out cnt);

                cam.SetTriggerMode(cmbTriggerMode.SelectedIndex, hight);
            } else if(env.CameraEnv.iNovaType == 2) {
                iNova2.IPCamera cam = new iNova2.IPCamera();
                switch(groupBox1.Text.Substring(0, 1)) {
                    case "1":
                        cam = Cam1_iNova2;
                        break;
                    case "2":
                        cam = Cam2_iNova2;
                        break;
                }
                int cnt = 0;
                bool hight = false;
                cam.GetBracketMode(out hight, out cnt);

                cam.SetTriggerMode(cmbTriggerMode.SelectedIndex, hight);
            }
        }

        private void btnDBtestMaster_Click(object sender, EventArgs e)
        {
            AutoClosingMessageBox clmsg = new AutoClosingMessageBox();
            if (txtMDB.Text == string.Empty)
            {
                clmsg.Show("마스터 디비명 누락", "연결 테스트", 3000);
                return;
            }
            string ConString = string.Format("data source={0}; database={1}; user id={2}; password={3}; Connection Timeout=3; MultipleActiveResultSets=True;", txtServer.Text, txtMDB.Text, txtID.Text, txtPW.Text);
            SqlConnection con = new SqlConnection(ConString);
            try
            {
                con.Open();
                if (con.State == ConnectionState.Open)
                    clmsg.Show("MST 데이터 베이스 연결 " + (con.State == ConnectionState.Open ? "성공" : "실패"), "연결 테스트", 3000);
                else
                    clmsg.Show("MST 데이터 베이스 연결 " + "실패", "연결 테스트", 3000);
            }
            catch (Exception)
            {
                clmsg.Show("MST 데이터 베이스 연결 " + "실패", "연결 테스트", 3000);
            }
        }

        private void btnDBtestTrans_Click(object sender, EventArgs e)
        {
            AutoClosingMessageBox clmsg = new AutoClosingMessageBox();
            if (txtTDB.Text == string.Empty)
            {
                clmsg.Show("입출내역 디비명 누락", "연결 테스트", 3000);
                return;
            } 
            string ConString = string.Format("data source={0}; database={1}; user id={2}; password={3}; Connection Timeout=3; MultipleActiveResultSets=True;", txtServer.Text, txtTDB.Text, txtID.Text, txtPW.Text);
            SqlConnection con = new SqlConnection(ConString);
            try
            {
                con.Open();
                if (con.State == ConnectionState.Open)
                    clmsg.Show("TRNS 데이터 베이스 연결 " + (con.State == ConnectionState.Open ? "성공" : "실패"), "연결 테스트", 3000);
                else
                    clmsg.Show("TRNS 데이터 베이스 연결 " + "실패", "연결 테스트", 3000);
            }
            catch (Exception)
            {
                clmsg.Show("TRNS 데이터 베이스 연결 " + "실패", "연결 테스트", 3000);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            txtComSavePath.Text = folder();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (frm != null)
                frm.Close();
            this.Close();
        }

        private void CmbLPRType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cmb = (ComboBox)sender;
            if (cmb.SelectedItem == null) return;
            if (cmb.SelectedItem.Equals(ClsStructure.LprDevice.KukjeLpr))
            {
                switch (cmb.Name)
                {
                    case "CmbLPRType1":
                        txtLPRInfoIP1.Enabled = false;
                        txtLPRInfoPort1.Enabled = false;
                        txtLPRInfoPath1.Enabled = false;
                        btnLPRInfoPath1.Enabled = false;
                        break;
                    case "CmbLPRType2":
                        txtLPRInfoIP2.Enabled = false;
                        txtLPRInfoPort2.Enabled = false;
                        txtLPRInfoPath2.Enabled = false;
                        btnLPRInfoPath2.Enabled = false;
                        break;
                }
            }
            else if (!cmb.SelectedItem.Equals(ClsStructure.LprDevice.KukjeLpr))
            {
                switch (cmb.Name)
                {
                    case "CmbLPRType1":
                        txtLPRInfoIP1.Enabled = true;
                        txtLPRInfoPort1.Enabled = true;
                        txtLPRInfoPath1.Enabled = true;
                        btnLPRInfoPath1.Enabled = true;
                        break;
                    case "CmbLPRType2":
                        txtLPRInfoIP2.Enabled = true;
                        txtLPRInfoPort2.Enabled = true;
                        txtLPRInfoPath2.Enabled = true;
                        btnLPRInfoPath2.Enabled = true;
                        break;
                }
            }
        }

        private void CmbLPRInOut_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cmb = (ComboBox)sender;
            if (cmb.SelectedItem == null) return;
            switch (cmb.Name)
            {
                case "CmbLPRInOut1":
                    if (cmb.SelectedItem.Equals(ClsStructure.InoutType.입구용))
                        txtLPRName1.Text = txtLPRName1.Text.Replace("출구","입구");
                    else if (cmb.SelectedItem.Equals(ClsStructure.InoutType.출구용))
                        txtLPRName1.Text = txtLPRName1.Text.Replace("입구", "출구");
                    break;
                case "CmbLPRInOut2":
                    if (cmb.SelectedItem.Equals(ClsStructure.InoutType.입구용))
                        txtLPRName2.Text = txtLPRName1.Text.Replace("출구", "입구");
                    else if (cmb.SelectedItem.Equals(ClsStructure.InoutType.출구용))
                        txtLPRName2.Text = txtLPRName1.Text.Replace("입구", "출구");
                    break;
            }
        }

        private void ChkLPRUse_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox chk = (CheckBox)sender;
            if (chk.Checked)
            {
                switch (chk.Name)
                {
                    case "ChkLPRUse1":
                        txtLPRNo1.Enabled = true;
                        txtLPRName1.Enabled = true;
                        CmbLPRType1.Enabled = true;
                        CmbLPRInOut1.Enabled = true;
                        //ChkFreePass1.Enabled = true;
                        //chkFreePassGateOpen1.Enabled = true;
                        txtEqpmNo1.Enabled = true;
                        //txtLPRInfoIP1.Enabled = true;
                        //txtLPRInfoPort1.Enabled = true;
                        //txtLPRInfoPath1.Enabled = true;
                        //btnLPRInfoPath1.Enabled = true;
                        break;
                    case "ChkLPRUse2":
                        txtLPRNo2.Enabled = true;
                        txtLPRName2.Enabled = true;
                        CmbLPRType2.Enabled = true;
                        CmbLPRInOut2.Enabled = true;
                        //ChkFreePass2.Enabled = true;
                        //chkFreePassGateOpen2.Enabled = true;
                        txtEqpmNo2.Enabled = true;
                        //txtLPRInfoIP2.Enabled = true;
                        //txtLPRInfoPort2.Enabled = true;
                        //txtLPRInfoPath2.Enabled = true;
                        //btnLPRInfoPath2.Enabled = true;
                        break;
                }
            }
            else
            {
                switch (chk.Name)
                {
                    case "ChkLPRUse1":
                        txtLPRNo1.Enabled = false;
                        txtLPRName1.Enabled = false;
                        CmbLPRType1.Enabled = false;
                        CmbLPRInOut1.Enabled = false;
                        //ChkFreePass1.Enabled = false;
                        //chkFreePassGateOpen1.Enabled = false;
                        txtLPRInfoIP1.Enabled = false;
                        txtLPRInfoPort1.Enabled = false;
                        txtLPRInfoPath1.Enabled = false;
                        btnLPRInfoPath1.Enabled = false;
                        txtEqpmNo1.Enabled = false;
                        break;
                    case "ChkLPRUse2":
                        txtLPRNo2.Enabled = false;
                        txtLPRName2.Enabled = false;
                        CmbLPRType2.Enabled = false;
                        CmbLPRInOut2.Enabled = false;
                        //ChkFreePass2.Enabled = false;
                        //chkFreePassGateOpen2.Enabled = false;
                        txtLPRInfoIP2.Enabled = false;
                        txtLPRInfoPort2.Enabled = false;
                        txtLPRInfoPath2.Enabled = false;
                        btnLPRInfoPath2.Enabled = false;
                        txtEqpmNo2.Enabled = false;
                        break;
                }
            }
        }

        private void ChkDisplay1Use_CheckedChanged(object sender, EventArgs e)
        {
            bool chk = ChkDisplay1Use.Checked;
            cmbDisplay1Port.Enabled = chk;
            txtDisplay1Setting.Enabled = chk;
            CmbDisplay1Type.Enabled = chk;
            txtDisplay1Text1.Enabled = chk;
            chkUseFixedText1.Enabled = chk;
            CmbDisplayText1Color1.Enabled = chk;
            txtDisplay1Text2.Enabled = chk;
            CmbDisplayText1Color2.Enabled = chk;
            txtNormalCar1.Enabled = chk;
            CmbDisplayTextNormal1Color1.Enabled = chk;
            CmbDisplayTextNormal1Color2.Enabled = chk;
            txtPeriodCar1.Enabled = chk;
            CmbDisplayTextPeriod1Color1.Enabled = chk;
            CmbDisplayTextPeriod1Color2.Enabled = chk;
            btnDisplay1TestNormal.Enabled = chk;
            btnDisplay1TestPeriod.Enabled = chk;
        }

        private void ChkDisplay2Use_CheckedChanged(object sender, EventArgs e)
        {
            bool chk = ChkDisplay2Use.Checked;
            cmbDisplay2Port.Enabled = chk;
            txtDisplay2Setting.Enabled = chk;
            CmbDisplay2Type.Enabled = chk;
            txtDisplay2Text1.Enabled = chk;
            chkUseFixedText2.Enabled = chk;
            CmbDisplayText2Color1.Enabled = chk;
            txtDisplay2Text2.Enabled = chk;
            CmbDisplayText2Color2.Enabled = chk;
            txtNormalCar2.Enabled = chk;
            CmbDisplayTextNormal2Color1.Enabled = chk;
            CmbDisplayTextNormal2Color2.Enabled = chk;
            txtPeriodCar2.Enabled = chk;
            CmbDisplayTextPeriod2Color1.Enabled = chk;
            CmbDisplayTextPeriod2Color2.Enabled = chk;
            btnDisplay2TestNormal.Enabled = chk;
            btnDisplay2TestPeriod.Enabled = chk;
        }

        private void btnDisplay1TestNormal_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            try
            {
                switch (btn.Name)
                {
                    case "btnDisplay1Test":
                        if (frmLprMain.NetDisPlay1 != null && env.CommunicationEnv.DisPlay[0].Net.Use)
                        {
                            frmLprMain.NetDisPlay1.SendMsg(txtDisplay1Text1.Text, (byte)clsFunction.GetColor8Int(CmbDisplayText1Color1.Text), txtDisplay1Text2.Text, (byte)clsFunction.GetColor8Int(CmbDisplayText1Color2.Text));
                        }
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                            SerialDev.FirstDisPlay3.WriteDisPlay(txtDisplay1Text1.Text, txtDisplay1Text2.Text, clsFunction.GetColor3Int(CmbDisplayText1Color1.Text), clsFunction.GetColor3Int(CmbDisplayText1Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                            SerialDev.FirstDisPlay8.SendDisplay(txtDisplay1Text1.Text, txtDisplay1Text2.Text, (byte)clsFunction.GetColor8Int(CmbDisplayText1Color1.Text), (byte)clsFunction.GetColor8Int(CmbDisplayText1Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                            SerialDev.FirstDisPlayAmano3.SendDisplay(txtDisplay1Text1.Text, clsFunction.GetAmanoColor3uInt(CmbDisplayText1Color1.Text), txtDisplay1Text2.Text, clsFunction.GetAmanoColor3uInt(CmbDisplayText1Color2.Text));
                        break;
                    case "btnDisplay1TestNormal":
                        if (frmLprMain.NetDisPlay1 != null && env.CommunicationEnv.DisPlay[0].Net.Use)
                        {
                            frmLprMain.NetDisPlay1.SendMsg(txtNormalCar1.Text, (byte)clsFunction.GetColor8Int(CmbDisplayTextNormal1Color1.Text), "테스트", (byte)clsFunction.GetColor8Int(CmbDisplayTextNormal1Color2.Text));
                        }
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                            SerialDev.FirstDisPlay3.WriteDisPlay(txtNormalCar1.Text, "테스트", clsFunction.GetColor3Int(CmbDisplayTextNormal1Color1.Text), clsFunction.GetColor3Int(CmbDisplayTextNormal1Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                            SerialDev.FirstDisPlay8.SendDisplay(txtNormalCar1.Text, "테스트", (byte)clsFunction.GetColor8Int(CmbDisplayTextNormal1Color1.Text), (byte)clsFunction.GetColor8Int(CmbDisplayTextNormal1Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                            SerialDev.FirstDisPlayAmano3.SendDisplay(txtNormalCar1.Text, clsFunction.GetAmanoColor3uInt(CmbDisplayTextNormal1Color1.Text), "테스트", clsFunction.GetAmanoColor3uInt(CmbDisplayTextNormal1Color2.Text));
                        break;
                    case "btnDisplay1TestPeriod":
                        if (frmLprMain.NetDisPlay1 != null && env.CommunicationEnv.DisPlay[0].Net.Use)
                        {
                            frmLprMain.NetDisPlay1.SendMsg(txtPeriodCar1.Text, (byte)clsFunction.GetColor8Int(CmbDisplayTextPeriod1Color1.Text), "테스트", (byte)clsFunction.GetColor8Int(CmbDisplayTextPeriod1Color2.Text));
                        }
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                            SerialDev.FirstDisPlay3.WriteDisPlay(txtPeriodCar1.Text, "테스트", clsFunction.GetColor3Int(CmbDisplayTextPeriod1Color1.Text), clsFunction.GetColor3Int(CmbDisplayTextPeriod1Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                            SerialDev.FirstDisPlay8.SendDisplay(txtPeriodCar1.Text, "테스트", (byte)clsFunction.GetColor8Int(CmbDisplayTextPeriod1Color1.Text), (byte)clsFunction.GetColor8Int(CmbDisplayTextPeriod1Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                            SerialDev.FirstDisPlayAmano3.SendDisplay(txtPeriodCar1.Text, clsFunction.GetAmanoColor3uInt(CmbDisplayTextPeriod1Color1.Text), "테스트", clsFunction.GetAmanoColor3uInt(CmbDisplayTextPeriod1Color2.Text));
                        break;
                    case "btnDisplay2Test":
                        if (frmLprMain.NetDisPlay2 != null && env.CommunicationEnv.DisPlay[1].Net.Use)
                        {
                            frmLprMain.NetDisPlay2.SendMsg(txtDisplay2Text1.Text, (byte)clsFunction.GetColor8Int(CmbDisplayText2Color1.Text), txtDisplay2Text2.Text, (byte)clsFunction.GetColor8Int(CmbDisplayText2Color2.Text));
                        }
                        else if (env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                            SerialDev.SecondDisPlay3.WriteDisPlay(txtDisplay2Text1.Text, txtDisplay2Text2.Text, clsFunction.GetColor3Int(CmbDisplayText2Color1.Text), clsFunction.GetColor3Int(CmbDisplayText2Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                            SerialDev.SecondDisPlay8.SendDisplay(txtDisplay2Text1.Text, txtDisplay2Text2.Text, (byte)clsFunction.GetColor8Int(CmbDisplayText2Color1.Text), (byte)clsFunction.GetColor8Int(CmbDisplayText2Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                            SerialDev.SecondDisPlayAmano3.SendDisplay(txtDisplay2Text1.Text, clsFunction.GetAmanoColor3uInt(CmbDisplayText2Color1.Text), txtDisplay2Text2.Text, clsFunction.GetAmanoColor3uInt(CmbDisplayText2Color2.Text));
                        break;
                    case "btnDisplay2TestNormal":
                        if (frmLprMain.NetDisPlay2 != null && env.CommunicationEnv.DisPlay[1].Net.Use)
                        {
                            frmLprMain.NetDisPlay2.SendMsg(txtNormalCar2.Text, (byte)clsFunction.GetColor8Int(CmbDisplayTextNormal2Color1.Text), "테스트", (byte)clsFunction.GetColor8Int(CmbDisplayTextNormal2Color2.Text));
                        }
                        else if (env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                            SerialDev.SecondDisPlay3.WriteDisPlay(txtNormalCar2.Text, "테스트", clsFunction.GetColor3Int(CmbDisplayTextNormal2Color1.Text), clsFunction.GetColor3Int(CmbDisplayTextNormal2Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                            SerialDev.SecondDisPlay8.SendDisplay(txtNormalCar2.Text, "테스트", (byte)clsFunction.GetColor8Int(CmbDisplayTextNormal2Color1.Text), (byte)clsFunction.GetColor8Int(CmbDisplayTextNormal2Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                            SerialDev.SecondDisPlayAmano3.SendDisplay(txtNormalCar2.Text, clsFunction.GetAmanoColor3uInt(CmbDisplayTextNormal2Color1.Text), "테스트", clsFunction.GetAmanoColor3uInt(CmbDisplayTextNormal2Color2.Text));
                        break;
                    case "btnDisplay2TestPeriod":
                        if (frmLprMain.NetDisPlay2 != null && env.CommunicationEnv.DisPlay[1].Net.Use)
                        {
                            frmLprMain.NetDisPlay2.SendMsg(txtPeriodCar2.Text, (byte)clsFunction.GetColor8Int(CmbDisplayTextPeriod2Color1.Text), "테스트", (byte)clsFunction.GetColor8Int(CmbDisplayTextPeriod2Color2.Text));
                        }
                        else if (env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                            SerialDev.SecondDisPlay3.WriteDisPlay(txtPeriodCar2.Text, "테스트", clsFunction.GetColor3Int(CmbDisplayTextPeriod2Color1.Text), clsFunction.GetColor3Int(CmbDisplayTextPeriod2Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                            SerialDev.SecondDisPlay8.SendDisplay(txtPeriodCar2.Text, "테스트", (byte)clsFunction.GetColor8Int(CmbDisplayTextPeriod2Color1.Text), (byte)clsFunction.GetColor8Int(CmbDisplayTextPeriod2Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                            SerialDev.SecondDisPlayAmano3.SendDisplay(txtNormalCar2.Text, clsFunction.GetAmanoColor3uInt(CmbDisplayTextPeriod2Color1.Text), "테스트", clsFunction.GetAmanoColor3uInt(CmbDisplayTextPeriod2Color2.Text));
                        break;
                }
            }
            catch (Exception ex)
            {
                Util.Logger.Log("btnDisplay1TestNormal_Click error " + ex.Message);
            }
        }

        private void DevSerialSetting_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cmb = (ComboBox)sender;
            switch (cmb.Name)
            {
                //case "cmbDisplay1Port":
                case "CmbDisplay1Type":
                    if (env.CommunicationEnv.DisPlay[0].Com.SerialPort != cmbDisplay1Port.Text || env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name != CmbDisplay1Type.Text)
                    {
                        btnDisplay1Test.Enabled = false;
                        btnDisplay1TestNormal.Enabled = false;
                        btnDisplay1TestPeriod.Enabled = false;
                    }
                    else if (env.CommunicationEnv.DisPlay[0].Com.SerialPort == cmbDisplay1Port.Text || env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name == CmbDisplay1Type.Text)
                    {
                        btnDisplay1Test.Enabled = true;
                        btnDisplay1TestNormal.Enabled = true;
                        btnDisplay1TestPeriod.Enabled = true;
                    }
                    if (cmb.Text == "Color8" || cmb.Text == "AmanoSmall")
                        txtDisplay1Setting.Text = "115200,n,8,1";
                    else
                        txtDisplay1Setting.Text = "9600,n,8,1";
                    if (chkDisplay1NetUse.Checked)
                        chkDisplay1NetUse.Checked = cmb.Text == "Color8";
                    chkDisplay1NetUse.Enabled = cmb.Text == "Color8";
                    break;
                //case "cmbDisplay2Port":
                case "CmbDisplay2Type":
                    if (env.CommunicationEnv.DisPlay[1].Com.SerialPort != cmbDisplay2Port.Text || env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name != CmbDisplay2Type.Text)
                    {
                        btnDisplay2Test.Enabled = false;
                        btnDisplay2TestNormal.Enabled = false;
                        btnDisplay2TestPeriod.Enabled = false;
                    }
                    else if (env.CommunicationEnv.DisPlay[1].Com.SerialPort == cmbDisplay2Port.Text || env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name == CmbDisplay2Type.Text)
                    {
                        btnDisplay2Test.Enabled = true;
                        btnDisplay2TestNormal.Enabled = true;
                        btnDisplay2TestPeriod.Enabled = true;
                    }
                    if (cmb.Text == "Color8" || cmb.Text == "AmanoSmall")
                        txtDisplay2Setting.Text = "115200,n,8,1";
                    else
                        txtDisplay2Setting.Text = "9600,n,8,1";
                    if (chkDisplay2NetUse.Checked)
                        chkDisplay2NetUse.Checked = cmb.Text == "Color8";
                    chkDisplay2NetUse.Enabled = cmb.Text == "Color8";
                    break;
            }
            if (cmb.Name.Equals("CmbDisplay1Type"))
                switch (CmbDisplay1Type.SelectedIndex)
                {
                    case (int)ClsStructure.DisPlayType.Color3:
                        CmbDisplayText1Color1.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        CmbDisplayText1Color2.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        CmbDisplayTextNormal1Color1.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        CmbDisplayTextNormal1Color2.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        CmbDisplayTextPeriod1Color1.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        CmbDisplayTextPeriod1Color2.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        cmbFixedColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        cmbFixedColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        cmbBlackDisplayBadColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        cmbBlackDisplayBadColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        cmbBlackDisplayNormalColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        cmbBlackDisplayNormalColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        cmbBlackDisplayRegColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        cmbBlackDisplayRegColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        cmbNoDriveColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        cmbNoDriveColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        break;
                    case (int)ClsStructure.DisPlayType.Color8:
                        CmbDisplayText1Color1.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        CmbDisplayText1Color2.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        CmbDisplayTextNormal1Color1.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        CmbDisplayTextNormal1Color2.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        CmbDisplayTextPeriod1Color1.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        CmbDisplayTextPeriod1Color2.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        cmbFixedColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        cmbFixedColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        cmbBlackDisplayBadColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        cmbBlackDisplayBadColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        cmbBlackDisplayNormalColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        cmbBlackDisplayNormalColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        cmbBlackDisplayRegColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        cmbBlackDisplayRegColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        cmbNoDriveColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        cmbNoDriveColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        break;
                    case (int)ClsStructure.DisPlayType.AmanoSmall:
                        CmbDisplayText1Color1.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        CmbDisplayText1Color2.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        CmbDisplayTextNormal1Color1.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        CmbDisplayTextNormal1Color2.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        CmbDisplayTextPeriod1Color1.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        CmbDisplayTextPeriod1Color2.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        cmbFixedColor1.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        cmbFixedColor2.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        cmbBlackDisplayBadColor1.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        cmbBlackDisplayBadColor2.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        cmbBlackDisplayNormalColor1.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        cmbBlackDisplayNormalColor2.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        cmbBlackDisplayRegColor1.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        cmbBlackDisplayRegColor2.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        cmbNoDriveColor1.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        cmbNoDriveColor2.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        break;
                }
                cmbNoDriveColor1.Text = NoDriving.Color1;
                cmbNoDriveColor2.Text = NoDriving.Color2;
            if (cmb.Name.Equals("CmbDisplay2Type"))
                switch (CmbDisplay2Type.SelectedIndex)
                {
                    case (int)ClsStructure.DisPlayType.Color3:
                        CmbDisplayText2Color1.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        CmbDisplayText2Color2.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        CmbDisplayTextNormal2Color1.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        CmbDisplayTextNormal2Color2.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        CmbDisplayTextPeriod2Color1.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        CmbDisplayTextPeriod2Color2.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        cmbBlackDisplayBadColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        cmbBlackDisplayBadColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        cmbBlackDisplayNormalColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        cmbBlackDisplayNormalColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        cmbBlackDisplayRegColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        cmbBlackDisplayRegColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color3));
                        break;
                    case (int)ClsStructure.DisPlayType.Color8:
                        CmbDisplayText2Color1.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        CmbDisplayText2Color2.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        CmbDisplayTextNormal2Color1.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        CmbDisplayTextNormal2Color2.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        CmbDisplayTextPeriod2Color1.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        CmbDisplayTextPeriod2Color2.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        cmbBlackDisplayBadColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        cmbBlackDisplayBadColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        cmbBlackDisplayNormalColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        cmbBlackDisplayNormalColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        cmbBlackDisplayRegColor1.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        cmbBlackDisplayRegColor2.DataSource = Enum.GetValues(typeof(ClsStructure.Color8));
                        break;
                    case (int)ClsStructure.DisPlayType.AmanoSmall:
                        CmbDisplayText2Color1.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        CmbDisplayText2Color2.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        CmbDisplayTextNormal2Color1.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        CmbDisplayTextNormal2Color2.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        CmbDisplayTextPeriod2Color1.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        CmbDisplayTextPeriod2Color2.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        cmbBlackDisplayBadColor1.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        cmbBlackDisplayBadColor2.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        cmbBlackDisplayNormalColor1.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        cmbBlackDisplayNormalColor2.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        cmbBlackDisplayRegColor1.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        cmbBlackDisplayRegColor2.DataSource = Enum.GetValues(typeof(ClsStructure.AmanoColor3));
                        break;
                }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Util.Logger.Log(string.Format("{0} 포트 차단기 개방 테스트", cbPassTest.SelectedIndex));
            SerialDev.GateOpen(cbPassTest.SelectedIndex);
        }

        private void btnExposureCheck_Click(object sender, EventArgs e)
        {
            frm = new frmExposureCheck(env.CommunicationEnv.ImageSave.SavePath);
            frm.Show();
        }

        private void chkLprRelay_CheckedChanged(object sender, EventArgs e)
        {
            txtLprEntPort.Enabled = chkLprEntUse.Checked;
        }

        private void ChkFreePass1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void chkRegDelayUse_CheckedChanged(object sender, EventArgs e)
        {
            txtRegDelayTerm.Enabled = chkRegDelayUse.Enabled;
        }

        private void chkDuplicateUse_CheckedChanged(object sender, EventArgs e)
        {
            txtDuplicateTerm.Enabled = chkDuplicateUse.Enabled;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (chkIsolateUse.Checked)
            {
                SerialDev.IsolatedGateOpen();
            }
        }

        private void chkBeforeCalUse_CheckedChanged(object sender, EventArgs e)
        {
            //txtBeforeCalLag.Enabled = chkBeforeCalUse.Checked;
            //chkOutService.Checked = !chkBeforeCalUse.Checked;
            txtBeforeCalLag.Enabled = chkBeforeCalUse.Checked;
            if (chkBeforeCalUse.Checked)
                chkOutService.Checked = !chkBeforeCalUse.Checked;
        }

        private void chkOutService_CheckedChanged(object sender, EventArgs e)
        {
            txtOutService.Enabled = chkOutService.Checked;
            if (chkBeforeCalUse.Checked)
                chkBeforeCalUse.Checked = !chkOutService.Checked;
        }

        private void chkDisplay1NetUse_CheckedChanged(object sender, EventArgs e)
        {
            txtDisplay1NetIp.Enabled = chkDisplay1NetUse.Checked;
            txtDisplay1NetPort.Enabled = chkDisplay1NetUse.Checked;
            cmbDisplay1Port.Enabled = !chkDisplay1NetUse.Checked;
            txtDisplay1Setting.Enabled = !chkDisplay1NetUse.Checked;
            CmbDisplay1Type.Enabled = !chkDisplay1NetUse.Checked;
        }

        private void chkDisplay2NetUse_CheckedChanged(object sender, EventArgs e)
        {
            txtDisplay2NetIp.Enabled = chkDisplay2NetUse.Checked;
            txtDisplay2NetPort.Enabled = chkDisplay2NetUse.Checked;
            cmbDisplay2Port.Enabled = !chkDisplay2NetUse.Checked;
            txtDisplay2Setting.Enabled = !chkDisplay2NetUse.Checked;
            CmbDisplay2Type.Enabled = !chkDisplay2NetUse.Checked;
        }

        private void rdbCore_CheckedChanged(object sender, EventArgs e)
        {
            panel1.Enabled = rdbCore.Checked;
            panel5.Enabled = rdbCore.Checked;
            chkRegCarType.Enabled = rdbCore.Checked;
        }

        private void chkNoDrivingUse_CheckedChanged(object sender, EventArgs e)
        {
            groupBox27.Enabled = chkNoDrivingUse.Checked;
        }

        private void BtnDisplay1default_Click(object sender, EventArgs e)
        {

        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            groupBox29.Enabled = chkGetMst.Checked;
        }

        private void Button7_Click(object sender, EventArgs e)
        {
            txtGetMstPath.Text = folder();
        }

        private void rdStartCom_CheckedChanged(object sender, EventArgs e)
        {
            lstLprList.Enabled = rdStartCom.Checked;
        }

        private void lstLprList_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                lstLprList.Items.Remove(lstLprList.SelectedItems[0]);
            }
        }

        private void btnLPRInfoPath1_Click(object sender, EventArgs e)
        {
            txtLPRInfoPath1.Text = folder();
        }

        private void btnLPRInfoPath2_Click(object sender, EventArgs e)
        {
            txtLPRInfoPath2.Text = folder();
        }

        //private void cmbLprRelayType_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    if (cmbLprEntType.Text == "SERVER")
        //        txtLprEntIp.Visible = false;
        //    else if (cmbLprEntType.Text == "CLIENT")
        //        txtLprEntIp.Visible = true;
        //}

        private void chkOtherparkuse_CheckedChanged(object sender, EventArgs e)
        {
            grpOtherParkUse.Enabled = chkOtherparkuse.Checked;
        }

        private void lstOtherPark_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstOtherPark.SelectedItems.Count > 0)
            {
                ListViewItem litem = lstOtherPark.SelectedItems[0];
                txtOtherpark.Text = litem.SubItems[2].Text;
            }
        }

        private void btnOtherpark_Click(object sender, EventArgs e)
        {
            if (lstOtherPark.SelectedItems.Count > 0)
            {
                ListViewItem litem = lstOtherPark.SelectedItems[0];
                litem.SubItems[2].Text = txtOtherpark.Text;
            }
        }

        private void chkOtherparktimeuse_CheckedChanged(object sender, EventArgs e)
        {
            mskOtherparktimestart.Enabled = mskOtherparktimeend.Enabled = chkOtherparktimeuse.Checked;
        }

        private void chkAutoregdel_CheckedChanged(object sender, EventArgs e)
        {
            grpRegdel.Enabled = chkAutoregdel.Checked;
        }

        private void chkRegendnotiuse_CheckedChanged(object sender, EventArgs e)
        {
            grpRegenddaynoti.Enabled = chkRegendnotiuse.Checked;
        }

        private void chkusePenalty_CheckedChanged(object sender, EventArgs e)
        {
            groupBox32.Enabled = chkusePenalty.Checked;
        }

        private void chkGateGroupUse_CheckedChanged(object sender, EventArgs e)
        {
            groupBox33.Enabled = chkGateGroupUse.Checked;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (lstGroup.SelectedItems.Count == 0) return;
            ListViewItem sitem = lstGroup.SelectedItems[0];
            sitem.SubItems[2].Text = txtGroupName.Text;
            sitem.SubItems[3].Text = txtGroupMent.Text;
            txtGroupName.Text = "";
            txtGroupMent.Text = "";
        }

        private void lstGroup_Enter(object sender, EventArgs e)
        {
            
        }

        private void lstGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstGroup.SelectedItems.Count == 0) return;
            ListViewItem sitem = lstGroup.SelectedItems[0];
            txtGroupName.Text = sitem.SubItems[2].Text;
            txtGroupMent.Text = sitem.SubItems[3].Text;
        }

        private void lstGroup_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void chkGroupUse_CheckedChanged(object sender, EventArgs e)
        {
            mskGroupFrom.Enabled = mskGroupTo.Enabled = chkGroupTimeUse.Checked;
        }

        private void txtGroupNo_TextChanged(object sender, EventArgs e)
        {
            int idx = 0;
            if (int.TryParse(txtGroupNo.Text, out idx))
            {
                if (lstGroup.Items.Count > 0)
                {
                    if (lstGroup.Items[idx - 1].Checked)
                        lstGroup.Items[idx - 1].Checked = false;
                }
                for (int i = 0; i < lstGroup.Items.Count; i++)
                {
                    lstGroup.Items[i].BackColor = Color.White;
                    if (idx -1 == i)
                        lstGroup.Items[i].BackColor = Color.Gray;
                }
            }
        }

        private void CarRegTypeGridInit()
        {
            DataGridView grid = dataGridView1;

            grid.Columns.Add("CarType", "차종");
            grid.Columns.Add("Rate", "인식률%");
            grid.Columns[0].Width = 100;
            grid.Columns[1].Width = 80;
        }

        private void chkRegCarType_CheckedChanged(object sender, EventArgs e)
        {
            dataGridView1.Enabled = chkRegCarType.Enabled;
        }

        //leess iNova2추가
        private void cmbCameraType_SelectedIndexChanged(object sender, EventArgs e) {
            env.CameraEnv.iNovaType = cmbCameraType.SelectedIndex + 1;
        }

        private void btnStayCommit_Click(object sender, EventArgs e)
        {
           
        }
    }
}
