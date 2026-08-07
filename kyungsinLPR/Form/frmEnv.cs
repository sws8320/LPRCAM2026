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
                rdbOptionK.Visible = true;   // Option(K) 도 x64 전용
                panel1.Visible = true;
                // Option(K) 선택 시에도 CPU/GPU(panel1) 활성화되게 같은 핸들러 연결
                rdbOptionK.CheckedChanged += new EventHandler(rdbCore_CheckedChanged);
            }
            // 서버모드 체크 시에만 "서버 카메라 설정" 버튼 활성
            // 서버 카메라 설정 그리드 메뉴 대신, '사용 대수'만 두고 개별설정은 카드 더블클릭으로.
            btnServerCams.Visible = false;
            lblCamCount = new Label { Text = "사용 대수", AutoSize = true, Location = new System.Drawing.Point(8, 116) };
            cboCamCount = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList,
                                         Location = new System.Drawing.Point(72, 112), Size = new System.Drawing.Size(56, 20) };
            // 서버모드는 통상 3대 이상 사용 → 사용대수 선택은 3부터 표시 (1,2 제외)
            for (int i = 3; i <= ServerCamConfig.MAX; i++) cboCamCount.Items.Add(i.ToString());
            groupBox11.Controls.Add(lblCamCount);
            groupBox11.Controls.Add(cboCamCount);
            // 카메라 개별설정용 '카드 표시 이름' 입력칸(탭 위쪽 빈 영역, perCam 모드만 표시)
            lblCamCardName = new Label { Text = "카메라 이름", AutoSize = true, Location = new System.Drawing.Point(16, 11), Visible = false };
            txtCamCardName = new TextBox { Location = new System.Drawing.Point(95, 8), Size = new System.Drawing.Size(220, 22), Visible = false };
            this.Controls.Add(lblCamCardName);
            this.Controls.Add(txtCamCardName);
            // 원격 차번인식/이미지업로드안함 체크박스는 Designer(gbUpload)에 정의됨. 연동만 코드로:
            //  '원격 차번인식 사용' 체크됐을 때만 '이미지 업로드 안함' 선택 가능.
            chkOcrRemote.CheckedChanged += delegate {
                chkOcrRemoteNoUpload.Enabled = chkOcrRemote.Checked;
                if (!chkOcrRemote.Checked) chkOcrRemoteNoUpload.Checked = false;
            };
            // 동작모드 ↔ 원격 차번인식 연동: 원격 인식 모드(기본2CH-원격인식=rdStartCam, 서버모드=rdbServerMode)면
            //  '원격 차번인식 사용' 활성+체크, 그 외 모드(인식X-ONLY자료처리=rdStartCom, 기본2CH모드=rdStartBoth)면 해제+비활성
            rdStartCam.CheckedChanged += delegate { UpdateRemoteOcrByMode(); };
            rdStartBoth.CheckedChanged += delegate { UpdateRemoteOcrByMode(); };
            rdStartCom.CheckedChanged += delegate { UpdateRemoteOcrByMode(); };
            rdbServerMode.CheckedChanged += delegate { UpdateRemoteOcrByMode(); };
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

        // DINGTIAN 이더넷 릴레이 보드 — 시리얼 미사용. 동적으로 IP/NetPort 입력 컨트롤 추가
        private System.Windows.Forms.TextBox _txtDioIp;
        private System.Windows.Forms.TextBox _txtDioNetPort;
        private System.Windows.Forms.Label _lblDioIp;
        private System.Windows.Forms.Label _lblDioNetPort;

        private System.Windows.Forms.GroupBox _gbDingtian;

        private void BuildDingtianControls()
        {
            // 차단기 설정 탭(tabGate)에서 groupBox6 옆 빈 공간(우측)에 DINGTIAN 전용 GroupBox 신설.
            // groupBox6 자체는 그대로 두고(아래에 gbPass가 있어 height 확장 불가), IP/TCP포트 입력만 별도로 표시.
            try
            {
                if (tabGate == null || groupBox6 == null) return;

                _gbDingtian = new System.Windows.Forms.GroupBox();
                _gbDingtian.Text = "DINGTIAN 이더넷 설정";
                _gbDingtian.Location = new System.Drawing.Point(
                    groupBox6.Location.X + groupBox6.Width + 10,
                    groupBox6.Location.Y);
                _gbDingtian.Size = new System.Drawing.Size(220, groupBox6.Height);
                _gbDingtian.Name = "gbDingtian";

                _lblDioIp = new System.Windows.Forms.Label();
                _lblDioIp.AutoSize = true;
                _lblDioIp.Location = new System.Drawing.Point(15, 28);
                _lblDioIp.Text = "보드 IP";
                _gbDingtian.Controls.Add(_lblDioIp);

                _txtDioIp = new System.Windows.Forms.TextBox();
                _txtDioIp.Location = new System.Drawing.Point(85, 25);
                _txtDioIp.Size = new System.Drawing.Size(120, 21);
                _txtDioIp.Name = "txtDioIp";
                _gbDingtian.Controls.Add(_txtDioIp);

                _lblDioNetPort = new System.Windows.Forms.Label();
                _lblDioNetPort.AutoSize = true;
                _lblDioNetPort.Location = new System.Drawing.Point(15, 58);
                _lblDioNetPort.Text = "TCP 포트";
                _gbDingtian.Controls.Add(_lblDioNetPort);

                _txtDioNetPort = new System.Windows.Forms.TextBox();
                _txtDioNetPort.Location = new System.Drawing.Point(85, 55);
                _txtDioNetPort.Size = new System.Drawing.Size(80, 21);
                _txtDioNetPort.Name = "txtDioNetPort";
                _txtDioNetPort.Text = "60001";
                _gbDingtian.Controls.Add(_txtDioNetPort);

                System.Windows.Forms.Label hint = new System.Windows.Forms.Label();
                hint.AutoSize = false;
                hint.Location = new System.Drawing.Point(15, 85);
                hint.Size = new System.Drawing.Size(195, 30);
                hint.Text = "출력: TCP/UDP 60001 ASCII\r\n입력: HTTP /input.cgi 폴링";
                hint.ForeColor = System.Drawing.Color.Gray;
                _gbDingtian.Controls.Add(hint);

                tabGate.Controls.Add(_gbDingtian);
                _gbDingtian.BringToFront();
            }
            catch (Exception ex) { Util.Logger.Log("BuildDingtianControls 실패: " + ex.Message); }
        }

        private void UpdateDioFieldsByType()
        {
            // DINGTIAN(이더넷)이면 시리얼/프로토콜/보드타입 비활성, IP/Port 활성. 그 외 반대
            try
            {
                bool isDingtian = false;
                if (cmbDioType.SelectedItem != null)
                    isDingtian = ((int)cmbDioType.SelectedItem) == (int)ClsStructure.DeviceList.DINGTIAN;
                cmbDioPort.Enabled = !isDingtian;
                txtDioSetting.Enabled = !isDingtian;
                cmbBoardType.Enabled = !isDingtian;
                if (_gbDingtian != null) _gbDingtian.Enabled = isDingtian;
                if (_txtDioIp != null) _txtDioIp.Enabled = isDingtian;
                if (_txtDioNetPort != null) _txtDioNetPort.Enabled = isDingtian;
            }
            catch { }
        }

        private void frmEnv_Load(object sender, EventArgs e)
        {
            BuildDingtianControls();
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
            ApplyServerModeUi();   // 서버모드면 공통 설정만 활성(나머지 비활성)
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

            InitUsbExtension(); // USB 카메라 설정 컨트롤 동적 추가
            InitWgwkExtension(); // WGWK-A05D 접속정보 컨트롤 동적 추가

            // [중요] setEnv()(전역값)로 컨트롤을 모두 채운 '이후'에 서버모드 개별값/필터를 적용.
            //  (SetServerCamMode 는 ShowDialog 전에 호출되어 이 Load 보다 먼저 실행되므로 여기서 다시 적용)
            if (_serverCamIndex >= 0)
                ApplyServerCamConfig();
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
            checkBox1.Checked = NoDriving.Exception2;
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
                case (int)ClsStructure.RegModule.OptionK:
                    rdbOptionK.Checked = true;
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

            // Evo 인식엔진 버전 (V6/V7) — 현장 옛 6버전 SDK 호환용. 기본 V7. Option(C) 전용, KOR/THA와 독립.
            if (env.CameraEnv.EvoVersion == 6)
                rdbEvo6.Checked = true;
            else
                rdbEvo7.Checked = true;
            panelEvoVer.Enabled = rdbCore.Checked;

            chkRegCarType.Checked = env.CameraEnv.bRegCarType;

            // 동영상 인식 방식 설정 로드
            cmbRecogMode.SelectedIndex = (env.CameraEnv.RecogMode == 1) ? 1 : 0;
            txtRtsp1.Text = env.CameraEnv.IPCamera1Info.RtspUrl ?? "";
            txtRtsp2.Text = env.CameraEnv.IPCamera2Info.RtspUrl ?? "";

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
            // iNovaType 1=iNova1(idx0), 2=iNova2(idx1), 4=WGWK-A05D(idx2)
            cmbCameraType.SelectedIndex = (env.CameraEnv.iNovaType == (int)ClsStructure.CameraSourceType.WGWK) ? 2
                                        : (env.CameraEnv.iNovaType == 2) ? 1 : 0;
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
            if (_txtDioIp != null) _txtDioIp.Text = env.CommonEnv.Dio.DioSetting.IpAddress;
            if (_txtDioNetPort != null) _txtDioNetPort.Text = (env.CommonEnv.Dio.DioSetting.NetPort > 0 ? env.CommonEnv.Dio.DioSetting.NetPort : 60001).ToString();
            UpdateDioFieldsByType();

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
            chkUseVisitor.Checked = env.CommunicationEnv.UseVisitor;
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
            chkExitGroupGateUse.Checked = env.RegCarControl.UseExitGroupGate;
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

            // 이미지 업로드 (ParkingWeb) 설정 로드 — Setting.ini [UPLOAD] 섹션
            try
            {
                string en = Util.Function.IniReadValue("UPLOAD", "enabled") ?? "";
                chkUploadEnabled.Checked = en.Equals("true", StringComparison.OrdinalIgnoreCase) || en == "1";
                txtUploadServerUrl.Text  = Util.Function.IniReadValue("UPLOAD", "serverurl") ?? "";
                txtUploadApiKey.Text     = Util.Function.IniReadValue("UPLOAD", "apikey") ?? "";

                // 서버모드(멀티카메라 카드 화면) — [OPTIONK] servermode
                string sm = Util.Function.IniReadValue("OPTIONK", "servermode") ?? "";
                if (sm.Equals("true", StringComparison.OrdinalIgnoreCase) || sm == "1")
                    rdbServerMode.Checked = true;   // 서버모드 선택(시작모드 라디오는 자동 해제)
                // 원격 차번인식 사용 — [OPTIONK] remote (서버모드와 별개; 서버 URL/키는 [UPLOAD] 재사용)
                string rm = Util.Function.IniReadValue("OPTIONK", "remote") ?? "";
                if (chkOcrRemote != null) chkOcrRemote.Checked = (rm.Equals("true", StringComparison.OrdinalIgnoreCase) || rm == "1");
                // 원격 차번인식만 사용(이미지 업로드 안함) — [OPTIONK] remote_noupload
                string rmnu = Util.Function.IniReadValue("OPTIONK", "remote_noupload") ?? "";
                if (chkOcrRemoteNoUpload != null) {
                    chkOcrRemoteNoUpload.Enabled = (chkOcrRemote != null && chkOcrRemote.Checked);
                    chkOcrRemoteNoUpload.Checked = chkOcrRemoteNoUpload.Enabled &&
                        (rmnu.Equals("true", StringComparison.OrdinalIgnoreCase) || rmnu == "1");
                }
                // 동작모드 기준으로 '원격 차번인식 사용' 상태 최종 강제 (모드가 remote 여부를 결정)
                UpdateRemoteOcrByMode();
                // 서버모드 사용 카메라 대수 로드(최소 3, 기본 3 — 1,2는 선택목록에서 제외)
                int camCnt = Util.Function.IntTryParse(Util.Function.IniReadValue("OPTIONK", "camcount"));
                if (camCnt < 3 || camCnt > ServerCamConfig.MAX) camCnt = 3;
                cboCamCount.SelectedItem = camCnt.ToString();
            }
            catch (Exception) { }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // SSDP 타이머/이벤트 정리 — 폼 닫힌 후에도 OnTimer가 발사되어 이벤트 핸들러가
            // 죽은 ListBox에 접근하면 NRE 발생 (관찰 사례). Stop으로 타이머 중지 + 이벤트 클리어.
            try
            {
                if (ssdpSession != null)
                {
                    ssdpSession.IpUpdated -= IpUpdated;
                    ssdpSession.Stop();
                }
            }
            catch (Exception ex)
            {
                try { Util.Logger.Log("[frmEnv.OnFormClosed] " + ex.Message); } catch { }
            }
            base.OnFormClosed(e);
        }

        private void IpUpdated(object sender, IpUpdatedEventArgs arg)
        {
            // SSDP 타이머는 폼 닫힌 뒤에도 발사될 수 있음 — 폼/리스트 상태 가드
            if (this.IsDisposed) return;
            if (arg == null || string.IsNullOrEmpty(arg.ipAddress)) return;
            if (listCamIPlist == null || listCamIPlist.IsDisposed) return;
            try { UpdateItemToList(listCamIPlist, arg); }
            catch (Exception ex) { Util.Logger.Log("[frmEnv.IpUpdated] " + ex.Message); }
        }

        delegate void UpdateItemToListCallback(ListBox list, IpUpdatedEventArgs arg);

        private void UpdateItemToList(ListBox list, IpUpdatedEventArgs arg)
        {
            if (list == null || list.IsDisposed) return;
            if (arg == null || string.IsNullOrEmpty(arg.ipAddress)) return;

            // 핸들 미생성 상태에서 InvokeRequired 자체가 throw할 수 있음 — try로 보호
            bool needsInvoke;
            try { needsInvoke = list.InvokeRequired; }
            catch { return; }

            if (needsInvoke)
            {
                if (!list.IsHandleCreated || this.IsDisposed) return;
                var d = new UpdateItemToListCallback(UpdateItemToList);
                try { this.BeginInvoke(d, new object[] { list, arg }); }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { /* 핸들 파괴 */ }
            }
            else
            {
                try
                {
                    if (arg.added && !list.Items.Contains(arg.ipAddress))
                        list.Items.Add(arg.ipAddress);
                    else if (!arg.added && list.Items.Contains(arg.ipAddress))
                        list.Items.Remove(arg.ipAddress);
                }
                catch (Exception ex)
                {
                    Util.Logger.Log("[frmEnv.UpdateItemToList] " + ex.Message);
                }
            }
        }

        private void btnSetROI_Click(object sender, EventArgs e)
        {
            // 서버캠 개별설정(인덱스 2~14): 카드 스냅샷 이미지에 ROI 설정 → [SVRCAM{n}] 저장
            if (_serverCamIndex >= 2)
            {
                if (string.IsNullOrEmpty(_serverCamRoiImage) || !System.IO.File.Exists(_serverCamRoiImage))
                {
                    MessageBox.Show("영역설정용 카메라 영상이 없습니다.\n(카드에 영상이 표시된 상태에서 다시 시도하세요)", "영역 설정",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string sec = "SVRCAM" + (_serverCamIndex + 1);
                System.Drawing.Rectangle cur = ParseRoiRect(Util.Function.IniReadValue(sec, "roi"));
                frmPicConfig sfrm = new frmPicConfig(env, _serverCamRoiImage, cur);
                if (sfrm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    System.Drawing.Rectangle r = sfrm.RoiRect;
                    string s = String.Format("{0},{1},{2},{3}", r.X, r.Y, r.Width, r.Height);
                    Util.Function.IniWriteValue(sec, "roi", s);
                    Util.Function.IniWriteValue(sec, "pc_roi", s);
                    Util.Logger.Log(string.Format("[서버모드] 카메라{0} 영역설정 저장 {1}", _serverCamIndex + 1, s));
                    MessageBox.Show("영역이 저장되었습니다.", "영역 설정", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }
            // 대상 카메라 번호: 그룹 텍스트 우선, 없으면 개별설정 인덱스(카드1=cam1, 카드2=cam2)
            int camNo = 0;
            if (groupBox1.Text == "1번 카메라 설정") camNo = 1;
            else if (groupBox1.Text == "2번 카메라 설정") camNo = 2;
            else if (_serverCamIndex == 0) camNo = 1;
            else if (_serverCamIndex == 1) camNo = 2;
            if (camNo == 0)
            {
                MessageBox.Show("영역설정 대상 카메라를 확인할 수 없습니다.\n(실제 카메라 1/2만 지원)", "영역 설정",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            frmPicConfig frm = new frmPicConfig(env, camNo);
            if (frm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                System.Drawing.Rectangle r = frm.RoiRect;
                string roiStr = String.Format("{0}, {1}, {2}, {3}", r.X, r.Y, r.Width, r.Height);
                if (camNo == 1)
                {
                    env.CameraEnv.IPCamera1Info.Roi = r;
                    Util.Function.IniWriteValue("CAMERA", "cam1roi", roiStr);
                }
                else
                {
                    env.CameraEnv.IPCamera2Info.Roi = r;
                    Util.Function.IniWriteValue("CAMERA", "cam2roi", roiStr);
                }
            }
        }

        // "x,y,w,h"(공백 허용) → Rectangle. 실패 시 빈 사각형.
        private static System.Drawing.Rectangle ParseRoiRect(string s)
        {
            try
            {
                if (string.IsNullOrEmpty(s)) return System.Drawing.Rectangle.Empty;
                string[] p = s.Split(',');
                if (p.Length < 4) return System.Drawing.Rectangle.Empty;
                return new System.Drawing.Rectangle(
                    Util.Function.IntTryParse(p[0].Trim()), Util.Function.IntTryParse(p[1].Trim()),
                    Util.Function.IntTryParse(p[2].Trim()), Util.Function.IntTryParse(p[3].Trim()));
            }
            catch { return System.Drawing.Rectangle.Empty; }
        }

        private void btnCamSetup_Click(object sender, EventArgs e)
        {
            try
            {
                // 서버캠 개별설정(인덱스 2~14): 라이브 서버카메라 디바이스로 고급설정(iNova2)
                if (_serverCamIndex >= 2)
                {
                    if (_serverCamDev == null)
                    {
                        MessageBox.Show("서버 카메라 연결이 없어 고급설정을 열 수 없습니다.\n(카메라 IP/연결 확인)", "카메라 설정",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    new iNova2.frmAdvFeature(_serverCamDev).ShowDialog();
                    return;
                }
                // 대상 카메라(0/1) 판정 — perCam 에서는 groupBox1.Text 기본값이라 _serverCamIndex 로 보완
                bool isCam1 = groupBox1.Text == "1번 카메라 설정" || _serverCamIndex == 0;
                bool isCam2 = groupBox1.Text == "2번 카메라 설정" || _serverCamIndex == 1;
                if (env.CameraEnv.iNovaType == 1)
                {
                    frmAdvFeature frm = isCam1 ? new frmAdvFeature(Cam1) : isCam2 ? new frmAdvFeature(Cam2) : null;
                    if (frm == null) { MessageBox.Show("대상 카메라를 확인할 수 없습니다."); return; }
                    frm.ShowDialog();
                }
                else if (env.CameraEnv.iNovaType == 2)
                {
                    iNova2.frmAdvFeature frm = isCam1 ? new iNova2.frmAdvFeature(Cam1_iNova2) : isCam2 ? new iNova2.frmAdvFeature(Cam2_iNova2) : null;
                    if (frm == null) { MessageBox.Show("대상 카메라를 확인할 수 없습니다."); return; }
                    frm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Util.Logger.Log("카메라 고급설정 오류: " + ex.Message);
                MessageBox.Show("카메라 설정 오류: " + ex.Message, "카메라 설정", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            RefreshUsbExtensionForCam(1);
            RefreshWgwkExtensionForCam(1);
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
            // USB 카메라이거나 IP 미연결이면 iNova SDK 호출 시 예외 가능 — try 가드
            if (env.CameraEnv.IPCamera2Info.CameraSource != (int)ClsStructure.CameraSourceType.USB)
            {
                try
                {
                    Cam2.GetTriggerImageCount(out cnt);
                    cmbTriggerCnt.Text = cnt.ToString();
                    Cam2.GetBracketMode(out blBarakect, out cnt);
                    cmbBrakectCnt.Text = cnt.ToString();
                    double frame = 0;
                    Cam2.GetFrameRate(out frame);
                    txtFrameRate.Text = frame.ToString();
                    Cam2.GetTriggerMode(out cnt, out blBarakect);
                    cmbTriggerMode.SelectedIndex = cnt;
                }
                catch (Exception ex) { Util.Logger.Log("[btnCam2_Click] iNova SDK 호출 실패: " + ex.Message); }
            }
            cmbTriggerCnt.Text = env.CameraEnv.IPCamera2Info.TriggerCnt.ToString();
            cmbBrakectCnt.Text = env.CameraEnv.IPCamera2Info.BarkectCnt.ToString();
            txtFrameRate.Text = env.CameraEnv.IPCamera2Info.FrameRate.ToString();
            cmbTriggerMode.SelectedIndex = env.CameraEnv.IPCamera2Info.TriggerMode;
            RefreshUsbExtensionForCam(2);
            RefreshWgwkExtensionForCam(2);
        }

        private void btnEnvSave_Click(object sender, EventArgs e)
        {
            // 카메라 개별설정 모드: 전역 설정을 저장하지 않고 [SVRCAM{n}]에만 별도 기록(창은 유지)
            if (_serverCamIndex >= 0)
            {
                SaveServerCam(_serverCamIndex);
                // DialogResult 설정하면 ShowDialog 폼이 닫히므로 설정하지 않음(창 유지)
                MessageBox.Show("카메라 개별설정이 저장되었습니다.", "서버모드",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;   // 창 유지 — 종료는 '닫기' 버튼으로
            }

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
                    // USB 카메라이면 iNova SDK 호출 스킵
                    if(env.CameraEnv.IPCamera1Info.CameraSource == (int)ClsStructure.CameraSourceType.USB) break;
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
                    // USB 카메라이면 iNova SDK 호출 스킵
                    if(env.CameraEnv.IPCamera2Info.CameraSource == (int)ClsStructure.CameraSourceType.USB) break;
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

            ApplyUsbStateToEnv(); // USB 카메라 상태를 ENV 구조체에 반영
            ApplyWgwkStateToEnv(); // WGWK-A05D 접속정보를 ENV 구조체에 반영
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
            env.RegCarControl.UseExitGroupGate = chkExitGroupGateUse.Checked;
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

            // 이미지 업로드 (ParkingWeb) 설정 저장 + 워커 재기동
            try
            {
                clsImageUploader.SaveIni(chkUploadEnabled.Checked, txtUploadServerUrl.Text, txtUploadApiKey.Text);
                // 서버모드(카드 화면) → servermode, 원격 차번인식 → remote (별개 저장)
                Util.Function.IniWriteValue("OPTIONK", "servermode", rdbServerMode.Checked.ToString());
                // 원격 차번인식 = 동작모드로 결정 (기본2CH-원격인식=rdStartCam, 서버모드=rdbServerMode → True / 그 외 False)
                bool remoteByMode = rdStartCam.Checked || rdbServerMode.Checked;
                Util.Function.IniWriteValue("OPTIONK", "remote", remoteByMode.ToString());
                // 원격 차번인식만 사용(이미지 업로드 안함) — remote 일 때만 유효
                bool ocrNoUpload = remoteByMode && chkOcrRemoteNoUpload != null && chkOcrRemoteNoUpload.Checked;
                Util.Function.IniWriteValue("OPTIONK", "remote_noupload", ocrNoUpload.ToString());
                clsImageUploader.Reload();   // remote_noupload 기록 후 재기동(SaveIni의 Reload는 이 값 기록 전이라 한 번 더)
                // 서버모드 사용 카메라 대수 저장
                if (cboCamCount != null && cboCamCount.SelectedItem != null)
                    Util.Function.IniWriteValue("OPTIONK", "camcount", cboCamCount.SelectedItem.ToString());
            }
            catch (Exception) { }
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

            //동작모드(공통) : 카메라서버/자료처리/카메라서버+자료처리/서버모드
            if (rdbServerMode.Checked)
                env.StartType = (int)ClsStructure.ProgramStartType.BOTH;   // 서버모드는 전체 실행(인식만 ParkingWeb)
            else if (rdStartCam.Checked)
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
            NoDriving.Exception2 = checkBox1.Checked;
            NoDriving.Ment1 = txtNoDriveMent1.Text;
            NoDriving.Ment2 = txtNoDriveMent2.Text;
            NoDriving.Color1 = cmbNoDriveColor1.Text;
            NoDriving.Color2 = cmbNoDriveColor2.Text;
            #endregion

            #region 카메라설정
            //leess iNova2추가 / WGWK(idx2)는 카메라별 CameraSource라 전역 iNovaType 미변경
            if (cmbCameraType.SelectedIndex != 2)
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
            else if (rdbOptionK.Checked)
                env.CameraEnv.RegModule = (int)ClsStructure.RegModule.OptionK;

            // 동영상 인식 방식 설정 저장
            env.CameraEnv.RecogMode = cmbRecogMode.SelectedIndex;
            env.CameraEnv.IPCamera1Info.RtspUrl = txtRtsp1.Text;
            env.CameraEnv.IPCamera2Info.RtspUrl = txtRtsp2.Text;

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

            env.CameraEnv.EvoVersion = rdbEvo6.Checked ? 6 : 7;

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
            if (_txtDioIp != null) env.CommonEnv.Dio.DioSetting.IpAddress = _txtDioIp.Text.Trim();
            if (_txtDioNetPort != null)
            {
                int np = Util.Function.IntTryParse(_txtDioNetPort.Text);
                env.CommonEnv.Dio.DioSetting.NetPort = np > 0 ? np : 60001;
            }

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
            env.CommunicationEnv.UseVisitor = chkUseVisitor.Checked;
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
                case (int)ClsStructure.DeviceList.DINGTIAN:
                    // 8채널 이더넷 릴레이. 입력/출력 모두 LPR과 동일하게 1~8(1-based)로 사용 (ClsDingtian이 내부에서 ch+1 매핑)
                    cmbLoop.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6", "7", "8" });
                    cmbSmallCar.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6", "7", "8" });
                    CmbGate1Port.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6", "7", "8" });
                    CmbGate2Port.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6", "7", "8" });
                    CmbGate1AddPort.Items.AddRange(new string[] { "", "1", "2", "3", "4", "5", "6", "7", "8" });
                    CmbGate2AddPort.Items.AddRange(new string[] { "", "1", "2", "3", "4", "5", "6", "7", "8" });
                    cmbFixedPort.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6", "7", "8" });
                    cmbIsolateOutport.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6", "7", "8" });
                    cmbIsolatePortAdd.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6", "7", "8" });
                    cmbIsolateInPort.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6", "7", "8" });
                    break;
            }
            UpdateDioFieldsByType();
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

        // 전광판 'Test' 버튼 전용 송신기 — 화면에 입력된 IP:Port 로 직접 연결/송신(개별설정·서버모드에서도 그 카메라 보드를 정확히 테스트)
        private NetworkDisplay _testDisp;
        private string _testDispKey = "";
        /// <summary>화면 입력 IP:Port 로 테스트 문구 송신.
        /// 같은 보드에 런타임 전광판(welcome 루프)이 이미 연결돼 있으면 그 인스턴스를 재사용한다.
        /// (별도 2번째 소켓으로 같은 보드에 동시 송신하면 welcome 루프와 바이트가 섞여 '깨짐' 발생)
        /// 런타임 연결이 없거나 IP가 다르면 임시 테스트 연결로 송신.</summary>
        private void SendDisplayTest(string ip, string portText, string line1, int color1, string line2, int color2)
        {
            if (string.IsNullOrWhiteSpace(ip)) { MessageBox.Show("전광판 IP를 입력하세요."); return; }
            int port; if (!int.TryParse((portText ?? "").Trim(), out port) || port <= 0) port = 5000;

            // 1) 같은 보드를 쓰는 런타임 전광판이 있으면 그 단일 소켓 재사용(+welcome 루프 비켜서게 DisPlayTime 갱신)
            NetworkDisplay rt = GetRuntimeDisplayForTest(ip.Trim(), port);
            if (rt != null)
            {
                rt.DisPlayTime = DateTime.Now;   // welcome 루프가 Term 초 동안 비켜서도록(충돌 방지)
                rt.SendMsg(line1, color1, line2, color2);
                return;
            }

            // 2) 런타임 연결 없음/IP 다름 → 임시 테스트 연결(같은 대상이면 재사용)
            string key = ip.Trim() + ":" + port;
            if (_testDisp == null || _testDispKey != key)
            {
                try { if (_testDisp != null) _testDisp.SocketClose(); } catch { }
                _testDisp = new NetworkDisplay();
                _testDisp.Tag = "전광판테스트";
                _testDisp.Init(ip.Trim(), port, "TCP");
                _testDispKey = key;
                System.Threading.Thread.Sleep(300);   // 비동기 TCP 연결 대기(첫 송신 누락 방지)
            }
            _testDisp.SendMsg(line1, color1, line2, color2);
        }

        /// <summary>개별설정 중인 카메라(_serverCamIndex)의 런타임 전광판 인스턴스 — IP:Port 가 화면값과 같을 때만 반환(아니면 null).</summary>
        private NetworkDisplay GetRuntimeDisplayForTest(string ip, int port)
        {
            try
            {
                NetworkDisplay nd = null;
                if (_serverCamIndex == 0) nd = frmLprMain.NetDisPlay1;
                else if (_serverCamIndex == 1) nd = frmLprMain.NetDisPlay2;
                else if (_serverCamIndex >= 2) nd = frmLprMain.NetDevForServer(_serverCamIndex);
                if (nd != null && ip.Equals((nd.Ip ?? "").Trim()) && port == nd.Port) return nd;
            }
            catch { }
            return null;
        }

        private void btnDisplay1TestNormal_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            try
            {
                switch (btn.Name)
                {
                    case "btnDisplay1Test":
                        if (chkDisplay1NetUse.Checked)
                        {
                            SendDisplayTest(txtDisplay1NetIp.Text, txtDisplay1NetPort.Text, txtDisplay1Text1.Text, clsFunction.GetColor8Int(CmbDisplayText1Color1.Text), txtDisplay1Text2.Text, clsFunction.GetColor8Int(CmbDisplayText1Color2.Text));
                        }
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                            SerialDev.FirstDisPlay3.WriteDisPlay(txtDisplay1Text1.Text, txtDisplay1Text2.Text, clsFunction.GetColor3Int(CmbDisplayText1Color1.Text), clsFunction.GetColor3Int(CmbDisplayText1Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                            SerialDev.FirstDisPlay8.SendDisplay(txtDisplay1Text1.Text, txtDisplay1Text2.Text, (byte)clsFunction.GetColor8Int(CmbDisplayText1Color1.Text), (byte)clsFunction.GetColor8Int(CmbDisplayText1Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                            SerialDev.FirstDisPlayAmano3.SendDisplay(txtDisplay1Text1.Text, clsFunction.GetAmanoColor3uInt(CmbDisplayText1Color1.Text), txtDisplay1Text2.Text, clsFunction.GetAmanoColor3uInt(CmbDisplayText1Color2.Text));
                        break;
                    case "btnDisplay1TestNormal":
                        if (chkDisplay1NetUse.Checked)
                        {
                            SendDisplayTest(txtDisplay1NetIp.Text, txtDisplay1NetPort.Text, txtNormalCar1.Text, clsFunction.GetColor8Int(CmbDisplayTextNormal1Color1.Text), "테스트", clsFunction.GetColor8Int(CmbDisplayTextNormal1Color2.Text));
                        }
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                            SerialDev.FirstDisPlay3.WriteDisPlay(txtNormalCar1.Text, "테스트", clsFunction.GetColor3Int(CmbDisplayTextNormal1Color1.Text), clsFunction.GetColor3Int(CmbDisplayTextNormal1Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                            SerialDev.FirstDisPlay8.SendDisplay(txtNormalCar1.Text, "테스트", (byte)clsFunction.GetColor8Int(CmbDisplayTextNormal1Color1.Text), (byte)clsFunction.GetColor8Int(CmbDisplayTextNormal1Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                            SerialDev.FirstDisPlayAmano3.SendDisplay(txtNormalCar1.Text, clsFunction.GetAmanoColor3uInt(CmbDisplayTextNormal1Color1.Text), "테스트", clsFunction.GetAmanoColor3uInt(CmbDisplayTextNormal1Color2.Text));
                        break;
                    case "btnDisplay1TestPeriod":
                        if (chkDisplay1NetUse.Checked)
                        {
                            SendDisplayTest(txtDisplay1NetIp.Text, txtDisplay1NetPort.Text, txtPeriodCar1.Text, clsFunction.GetColor8Int(CmbDisplayTextPeriod1Color1.Text), "테스트", clsFunction.GetColor8Int(CmbDisplayTextPeriod1Color2.Text));
                        }
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                            SerialDev.FirstDisPlay3.WriteDisPlay(txtPeriodCar1.Text, "테스트", clsFunction.GetColor3Int(CmbDisplayTextPeriod1Color1.Text), clsFunction.GetColor3Int(CmbDisplayTextPeriod1Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                            SerialDev.FirstDisPlay8.SendDisplay(txtPeriodCar1.Text, "테스트", (byte)clsFunction.GetColor8Int(CmbDisplayTextPeriod1Color1.Text), (byte)clsFunction.GetColor8Int(CmbDisplayTextPeriod1Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[0].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                            SerialDev.FirstDisPlayAmano3.SendDisplay(txtPeriodCar1.Text, clsFunction.GetAmanoColor3uInt(CmbDisplayTextPeriod1Color1.Text), "테스트", clsFunction.GetAmanoColor3uInt(CmbDisplayTextPeriod1Color2.Text));
                        break;
                    case "btnDisplay2Test":
                        if (chkDisplay2NetUse.Checked)
                        {
                            SendDisplayTest(txtDisplay2NetIp.Text, txtDisplay2NetPort.Text, txtDisplay2Text1.Text, clsFunction.GetColor8Int(CmbDisplayText2Color1.Text), txtDisplay2Text2.Text, clsFunction.GetColor8Int(CmbDisplayText2Color2.Text));
                        }
                        else if (env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                            SerialDev.SecondDisPlay3.WriteDisPlay(txtDisplay2Text1.Text, txtDisplay2Text2.Text, clsFunction.GetColor3Int(CmbDisplayText2Color1.Text), clsFunction.GetColor3Int(CmbDisplayText2Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                            SerialDev.SecondDisPlay8.SendDisplay(txtDisplay2Text1.Text, txtDisplay2Text2.Text, (byte)clsFunction.GetColor8Int(CmbDisplayText2Color1.Text), (byte)clsFunction.GetColor8Int(CmbDisplayText2Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                            SerialDev.SecondDisPlayAmano3.SendDisplay(txtDisplay2Text1.Text, clsFunction.GetAmanoColor3uInt(CmbDisplayText2Color1.Text), txtDisplay2Text2.Text, clsFunction.GetAmanoColor3uInt(CmbDisplayText2Color2.Text));
                        break;
                    case "btnDisplay2TestNormal":
                        if (chkDisplay2NetUse.Checked)
                        {
                            SendDisplayTest(txtDisplay2NetIp.Text, txtDisplay2NetPort.Text, txtNormalCar2.Text, clsFunction.GetColor8Int(CmbDisplayTextNormal2Color1.Text), "테스트", clsFunction.GetColor8Int(CmbDisplayTextNormal2Color2.Text));
                        }
                        else if (env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color3.ToString()))
                            SerialDev.SecondDisPlay3.WriteDisPlay(txtNormalCar2.Text, "테스트", clsFunction.GetColor3Int(CmbDisplayTextNormal2Color1.Text), clsFunction.GetColor3Int(CmbDisplayTextNormal2Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.Color8.ToString()))
                            SerialDev.SecondDisPlay8.SendDisplay(txtNormalCar2.Text, "테스트", (byte)clsFunction.GetColor8Int(CmbDisplayTextNormal2Color1.Text), (byte)clsFunction.GetColor8Int(CmbDisplayTextNormal2Color2.Text));
                        else if (env.CommunicationEnv.DisPlay[1].Com.Dev_Type_Name.Equals(ClsStructure.DisPlayType.AmanoSmall.ToString()))
                            SerialDev.SecondDisPlayAmano3.SendDisplay(txtNormalCar2.Text, clsFunction.GetAmanoColor3uInt(CmbDisplayTextNormal2Color1.Text), "테스트", clsFunction.GetAmanoColor3uInt(CmbDisplayTextNormal2Color2.Text));
                        break;
                    case "btnDisplay2TestPeriod":
                        if (chkDisplay2NetUse.Checked)
                        {
                            SendDisplayTest(txtDisplay2NetIp.Text, txtDisplay2NetPort.Text, txtPeriodCar2.Text, clsFunction.GetColor8Int(CmbDisplayTextPeriod2Color1.Text), "테스트", clsFunction.GetColor8Int(CmbDisplayTextPeriod2Color2.Text));
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
            // CPU/GPU(panel1) 는 Option(C) 또는 Option(K) 에서 사용
            panel1.Enabled = rdbCore.Checked || rdbOptionK.Checked;
            panel5.Enabled = rdbCore.Checked;
            panelEvoVer.Enabled = rdbCore.Checked;   // Evo 엔진버전(V6/V7)은 Option(C)에서만
            chkRegCarType.Enabled = rdbCore.Checked;
        }

        // 서버모드 카메라(최대 15대) 설정 다이얼로그 열기
        private void btnServerCams_Click(object sender, EventArgs e)
        {
            using (var f = new frmServerCams())
            {
                f.ShowDialog(this);
            }
        }

        private int _serverCamIndex = -1;   // -1=일반/공통, >=0=카메라 개별설정 모드(인덱스)
        private iNova2.IPCamera _serverCamDev;   // 개별설정 시 라이브 서버카메라 디바이스(고급설정용)
        private string _serverCamRoiImage;       // 개별설정 시 영역설정용 스냅샷 이미지 경로

        /// <summary>카드 더블클릭 개별설정 시 라이브 서버카메라 디바이스/스냅샷 주입(frmLprMain).</summary>
        public void SetServerCamDevice(iNova2.IPCamera dev, string roiImagePath)
        {
            _serverCamDev = dev;
            _serverCamRoiImage = roiImagePath;
        }
        private ComboBox cboCamCount;       // 서버모드 사용 카메라 대수(1~15) — 코드 생성
        private Label lblCamCount;
        private TextBox txtCamCardName;     // 카메라 개별설정: 카드 표시 이름(코드 생성, perCam 모드만 표시)
        private Label lblCamCardName;
        // chkOcrRemote / chkOcrRemoteNoUpload 는 Designer(gbUpload)에 정의됨

        /// <summary>카드 더블클릭 → 카메라 개별설정 모드로 연다(공통설정의 '반대' 필터 + [SVRCAM{n}] 별도 저장).</summary>
        public void SetServerCamMode(int camIndex)
        {
            _serverCamIndex = camIndex;
            try { this.Text = string.Format("카메라 {0} 개별 설정 (서버모드)", camIndex + 1); } catch { }
            // [중요] 실제 개별값/필터 적용은 ApplyServerCamConfig() 에서.
            //  frmEnv_Load 의 setEnv()(전역값으로 컨트롤 채움)가 이 메서드보다 '나중에' 실행되므로,
            //  여기서 바로 LoadServerCam 하면 setEnv 가 덮어써 버린다(재실행 시 개별값 사라짐).
            //  → 폼이 이미 로드됐으면 즉시, 아니면 frmEnv_Load 끝에서 호출.
            if (IsHandleCreated)
                ApplyServerCamConfig();
        }

        /// <summary>서버모드 개별설정 적용 — 반드시 setEnv(전역값 채움) '이후'에 호출해야 개별값이 유지된다.</summary>
        private void ApplyServerCamConfig()
        {
            if (_serverCamIndex < 0) return;
            ApplyServerModeUi();
            LoadServerCam(_serverCamIndex);   // 이 카메라의 [SVRCAM{n}] 저장값을 컨트롤에 반영(setEnv 이후라 유지됨)
            // 카드 표시 이름 입력칸 표시 + 현재 이름 로드(직접 입력)
            try {
                string nm = Util.Function.IniReadValue("SVRCAM" + (_serverCamIndex + 1), "name");
                txtCamCardName.Text = string.IsNullOrEmpty(nm) ? ("카메라" + (_serverCamIndex + 1)) : nm;
                lblCamCardName.Visible = true; txtCamCardName.Visible = true;
                lblCamCardName.BringToFront(); txtCamCardName.BringToFront();
            } catch { }
            // [중요] 장비번호/채널/입출구(gbLPR LPR1) 필드는 디자이너 기본 Enabled=false + ChkLPRUse1 체크 시에만 활성.
            //  서버캠은 항상 자기 장비정보가 필요하므로 강제 활성. LoadServerCam 이후에 실행해야 재비활성화 안 됨.
            try {
                ChkLPRUse1.Checked = true;
                SetEnabled(true, ChkLPRUse1, txtLPRNo1, txtEqpmNo1, CmbLPRInOut1, txtLPRName1, CmbLPRType1);
                if (gbLPR != null) gbLPR.Enabled = true;
            } catch (Exception ex) { Util.Logger.Log("[서버모드] LPR 장비필드 활성화 오류: " + ex.Message); }
        }

        /// <summary>서버모드 환경설정 필터.
        ///  공통모드(_serverCamIndex&lt;0): 공통 항목만 활성.
        ///  개별모드(_serverCamIndex&gt;=0): 그 '반대'(카메라/차단기/전광판/입출차연동/차단기그룹제한만 활성).</summary>
        private void ApplyServerModeUi()
        {
            try
            {
                string rm = Util.Function.IniReadValue("OPTIONK", "servermode") ?? "";
                bool server = rm.Equals("true", StringComparison.OrdinalIgnoreCase) || rm == "1";
                bool perCam = _serverCamIndex >= 0;
                if (!server && !perCam) return;   // 일반 프로그램 모드 → 손대지 않음

                // 서버모드에선 USB 카메라 설정 패널 숨김(서버모드는 iNova1/iNova2/WGWK만 지원)
                if (usbCamPanel != null) usbCamPanel.Visible = false;

                // 카메라설정/차단기설정/전광판설정 탭: 공통=비활성, 개별=활성
                tabCam.Enabled = perCam;
                tabGate.Enabled = perCam;
                tabDisplay.Enabled = perCam;

                // 기본설정 공통항목(DB·동작모드·긴급차량·인식률보정·정기권취득방식·현재주차대수): 공통=활성, 개별=비활성
                SetEnabled(!perCam, pnlDbInfo, groupBox11, checkEmergencyCar, groupBox10, groupBox29,
                                    label159, txtStay, btnStayCommit);
                // 기본설정 비공통(만차/부제/테스트/이미지여부/동일차량/주차장): 두 모드 모두 비활성
                SetEnabled(false, grpFullControl, chkNoDrivingUse, groupBox27, chkTestMod, gbImage, gbCustomer, gbPark);

                // LPR설정: 인식모듈(groupBox8)·이미지저장(groupBox5)=공통만; 장비설정(gbLPR, 장비번호·입출구)=개별만; 통신/인증=항상 비활성
                SetEnabled(!perCam, groupBox8, groupBox5);
                SetEnabled(perCam, gbLPR);
                SetEnabled(false, gbLPRInfo, gbAuthentication);

                // 입출차 정보 연동(groupBox16): 공통=비활성, 개별=활성
                groupBox16.Enabled = perCam;
                // 차단기 그룹 제한: 공통=비활성, 개별=활성
                SetEnabled(perCam, chkGateGroupUse, chkExitGroupGateUse);

                // 소켓통신·블랙리스트: 공통=활성, 개별=비활성
                tabSocket.Enabled = !perCam;
                tabPage4.Enabled = !perCam;

                // --- 개별설정(perCam) 세부: 활성 탭 안에서 추가 비활성 ---
                if (perCam)
                {
                    // 카메라설정: 이미 선택된 카메라 설정이므로 1번/2번 카메라 선택 버튼·USB 메뉴 비활성
                    SetEnabled(false, btnCam1, btnCam2, usbCamPanel);
                    // 전광판: 기본 1개(1번 전광판)만 사용 → 2번 전광판(Display2) 컨트롤 전부 비활성(문구2 포함)
                    DisableByName(tabDisplay, "Display2");
                    // 입출차 정보 연동: LPR2(groupBox12) 비활성 (LPR1만)
                    if (groupBox12 != null) groupBox12.Enabled = false;
                    // 차단기 DIO 출력 PORT: 2번째(Gate2) 비활성 (카메라당 차단기 1개)
                    DisableByName(tabGate, "Gate2");
                    // 장비 설정: 기본 1대(LPR1)만 → LPR2(2번 장비) 비활성
                    SetEnabled(false, txtEqpmNo2, cmbLPRPort2, CmbLPRInOut2, txtLPRProtocol2,
                               CmbLPRType2, txtLPRName2, txtLPRNo2, ChkLPRUse2);
                }
            }
            catch (Exception ex) { Util.Logger.Log("[서버모드] 환경설정 필터 오류: " + ex.Message); }
        }

        /// <summary>parent 하위에서 이름에 nameContains 포함된 컨트롤을 모두 비활성(재귀).</summary>
        private static void DisableByName(System.Windows.Forms.Control parent, string nameContains)
        {
            if (parent == null) return;
            foreach (System.Windows.Forms.Control c in parent.Controls)
            {
                if (!string.IsNullOrEmpty(c.Name) && c.Name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                    c.Enabled = false;
                if (c.Controls.Count > 0) DisableByName(c, nameContains);
            }
        }

        private static void SetEnabled(bool en, params System.Windows.Forms.Control[] cs)
        {
            foreach (var c in cs) if (c != null) c.Enabled = en;
        }

        // 개별설정 대상(카메라/차단기/전광판/입출차연동/차단기그룹제한) 컨트롤 루트.
        private System.Windows.Forms.Control[] PerCamRoots()
        {
            return new System.Windows.Forms.Control[] { tabCam, tabGate, tabDisplay, groupBox16, chkGateGroupUse, chkExitGroupGateUse, gbLPR };
        }

        /// <summary>카메라 개별설정 저장 — 대상 탭의 모든 입력 컨트롤을 [SVRCAM{n}]에 직렬화(전역 미변경).</summary>
        private void SaveServerCam(int index)
        {
            try
            {
                string sec = "SVRCAM" + (index + 1);
                // 저장 가속: 컨트롤마다 IniWriteValue(키 1개당 39KB INI 전체 재파싱·디스크 플러시 = 203키면 수 초)
                //  대신, 값들을 모아 [SVRCAM{n}] 섹션을 1회 읽기 + 1회 쓰기로 배치 저장.
                var pcDict = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                pcDict["percam_configured"] = "true";
                // 카메라 소스(서버모드): 콤보 인덱스 0=iNova1→1, 1=iNova2→2, 2=WGWK-A05D→4
                if (cmbCameraType != null)
                    pcDict["camsource"] = ((cmbCameraType.SelectedIndex == 2) ? 4 : (cmbCameraType.SelectedIndex + 1)).ToString();
                foreach (System.Windows.Forms.Control r in PerCamRoots()) WalkPerCamCollect(pcDict, r);
                // 카드 표시 이름(직접 입력) → [SVRCAM{n}].name
                if (txtCamCardName != null && !string.IsNullOrEmpty(txtCamCardName.Text))
                    pcDict["name"] = txtCamCardName.Text.Trim();
                IniWriteSectionBatch(sec, pcDict);
                // 카드1/2(인덱스0/1)는 기존 cam1/cam2 연결 사용 → 카메라 IP를 [CAMERA]에도 반영(재시작 후 적용)
                if (txtCamIp != null && !string.IsNullOrEmpty(txtCamIp.Text))
                {
                    if (index == 0) Util.Function.IniWriteValue("CAMERA", "cam1ip", txtCamIp.Text.Trim());
                    else if (index == 1) Util.Function.IniWriteValue("CAMERA", "cam2ip", txtCamIp.Text.Trim());
                }
                // 정산 핵심값 저장 확인 로그(장비번호/채널/입출구/게이트포트)
                Util.Logger.Log(string.Format("[서버모드] 카메라{0} 정산설정 저장 확인 — 장비번호={1} 채널={2} 입출구={3} 게이트포트={4}",
                    index + 1,
                    Util.Function.IniReadValue(sec, "pc_txtEqpmNo1"),
                    Util.Function.IniReadValue(sec, "pc_txtLPRNo1"),
                    Util.Function.IniReadValue(sec, "pc_CmbLPRInOut1"),
                    Util.Function.IniReadValue(sec, "pc_CmbGate1Port")));
                Util.Logger.Log(string.Format("[서버모드] 카메라{0} 개별설정 저장([{1}]) — 전역 미변경", index + 1, sec));
            }
            catch (Exception ex) { Util.Logger.Log("[서버모드] 개별설정 저장 오류: " + ex.Message); }
        }

        /// <summary>[SVRCAM{n}]에 저장된 개별설정 값을 대상 컨트롤에 로드(없으면 전역값 유지).</summary>
        private void LoadServerCam(int index)
        {
            try
            {
                string sec = "SVRCAM" + (index + 1);
                if (!"true".Equals(Util.Function.IniReadValue(sec, "percam_configured"), StringComparison.OrdinalIgnoreCase))
                    return;   // 아직 개별설정 저장 전 → 전역값 그대로 표시
                foreach (System.Windows.Forms.Control r in PerCamRoots()) WalkPerCam(sec, false, r);
            }
            catch (Exception ex) { Util.Logger.Log("[서버모드] 개별설정 로드 오류: " + ex.Message); }
        }

        /// <summary>입력 컨트롤(TextBox/ComboBox/CheckBox/RadioButton/NumericUpDown)을 [sec] pc_{Name} 키로
        /// 저장(save=true)/로드(save=false). 자식까지 재귀.</summary>
        private void WalkPerCam(string sec, bool save, System.Windows.Forms.Control c)
        {
            if (c == null) return;
            if (!string.IsNullOrEmpty(c.Name))
            {
                string key = "pc_" + c.Name;
                if (c is System.Windows.Forms.TextBox || c is System.Windows.Forms.MaskedTextBox || c is System.Windows.Forms.ComboBox)
                {
                    if (save) Util.Function.IniWriteValue(sec, key, c.Text ?? "");
                    else { string v = Util.Function.IniReadValue(sec, key); if (!string.IsNullOrEmpty(v)) c.Text = v; }
                }
                else if (c is System.Windows.Forms.CheckBox)
                {
                    System.Windows.Forms.CheckBox cb = (System.Windows.Forms.CheckBox)c;
                    if (save) Util.Function.IniWriteValue(sec, key, cb.Checked ? "1" : "0");
                    else { string v = Util.Function.IniReadValue(sec, key); if (v == "1" || v == "0") cb.Checked = (v == "1"); }
                }
                else if (c is System.Windows.Forms.RadioButton)
                {
                    System.Windows.Forms.RadioButton rb = (System.Windows.Forms.RadioButton)c;
                    if (save) Util.Function.IniWriteValue(sec, key, rb.Checked ? "1" : "0");
                    else { string v = Util.Function.IniReadValue(sec, key); if (v == "1") rb.Checked = true; }
                }
                else if (c is System.Windows.Forms.NumericUpDown)
                {
                    System.Windows.Forms.NumericUpDown nu = (System.Windows.Forms.NumericUpDown)c;
                    if (save) Util.Function.IniWriteValue(sec, key, nu.Value.ToString());
                    else
                    {
                        decimal d; string v = Util.Function.IniReadValue(sec, key);
                        if (decimal.TryParse(v, out d) && d >= nu.Minimum && d <= nu.Maximum) nu.Value = d;
                    }
                }
            }
            foreach (System.Windows.Forms.Control ch in c.Controls) WalkPerCam(sec, save, ch);
        }

        /// <summary>WalkPerCam 저장과 동일 규칙으로 키/값을 dict 에 모은다(파일은 안 씀 → 배치 저장용).</summary>
        private void WalkPerCamCollect(System.Collections.Generic.Dictionary<string, string> dict, System.Windows.Forms.Control c)
        {
            if (c == null) return;
            if (!string.IsNullOrEmpty(c.Name))
            {
                string key = "pc_" + c.Name;
                if (c is System.Windows.Forms.TextBox || c is System.Windows.Forms.MaskedTextBox || c is System.Windows.Forms.ComboBox)
                    dict[key] = c.Text ?? "";
                else if (c is System.Windows.Forms.CheckBox)
                    dict[key] = ((System.Windows.Forms.CheckBox)c).Checked ? "1" : "0";
                else if (c is System.Windows.Forms.RadioButton)
                    dict[key] = ((System.Windows.Forms.RadioButton)c).Checked ? "1" : "0";
                else if (c is System.Windows.Forms.NumericUpDown)
                    dict[key] = ((System.Windows.Forms.NumericUpDown)c).Value.ToString();
            }
            foreach (System.Windows.Forms.Control ch in c.Controls) WalkPerCamCollect(dict, ch);
        }

        /// <summary>한 섹션의 여러 키를 INI 전체 1회 읽기 + 1회 쓰기로 저장(WritePrivateProfileString 키당 재기록 회피).
        /// 인코딩은 WritePrivateProfileString 와 동일한 시스템 ANSI(CP949)로 맞춰 한글 보존. 기존 키는 제자리 갱신, 신규는 섹션 끝 추가.</summary>
        private void IniWriteSectionBatch(string sec, System.Collections.Generic.Dictionary<string, string> kv)
        {
            try
            {
                string path = Util.Function.IniFileName;   // DLL(WritePrivateProfileString)이 쓰는 실제 INI 경로
                System.Text.Encoding enc = System.Text.Encoding.Default;   // = WritePrivateProfileString 의 ANSI(CP949)
                var lines = System.IO.File.Exists(path)
                    ? new System.Collections.Generic.List<string>(System.IO.File.ReadAllLines(path, enc))
                    : new System.Collections.Generic.List<string>();

                // 섹션 [sec] 범위 찾기
                int secStart = -1, secEnd = lines.Count;
                for (int i = 0; i < lines.Count; i++)
                {
                    string t = lines[i].Trim();
                    if (t.StartsWith("[") && t.EndsWith("]"))
                    {
                        string name = t.Substring(1, t.Length - 2).Trim();
                        if (secStart < 0)
                        {
                            if (name.Equals(sec, StringComparison.OrdinalIgnoreCase)) secStart = i;
                        }
                        else { secEnd = i; break; }
                    }
                }

                var remaining = new System.Collections.Generic.Dictionary<string, string>(kv, StringComparer.OrdinalIgnoreCase);

                if (secStart < 0)
                {
                    // 섹션 없음 → 파일 끝에 새로 추가
                    if (lines.Count > 0 && lines[lines.Count - 1].Trim().Length != 0) lines.Add("");
                    lines.Add("[" + sec + "]");
                    foreach (var p in kv) lines.Add(p.Key + "=" + p.Value);
                }
                else
                {
                    // 기존 키 제자리 갱신
                    for (int i = secStart + 1; i < secEnd; i++)
                    {
                        int eq = lines[i].IndexOf('=');
                        if (eq <= 0) continue;
                        string k = lines[i].Substring(0, eq).Trim();
                        string val;
                        if (remaining.TryGetValue(k, out val)) { lines[i] = k + "=" + val; remaining.Remove(k); }
                    }
                    // 신규 키는 섹션 끝(secEnd 직전)에 삽입
                    if (remaining.Count > 0)
                    {
                        var toAdd = new System.Collections.Generic.List<string>();
                        foreach (var p in remaining) toAdd.Add(p.Key + "=" + p.Value);
                        lines.InsertRange(secEnd, toAdd);
                    }
                }
                System.IO.File.WriteAllLines(path, lines, enc);
            }
            catch (Exception ex)
            {
                Util.Logger.Log("[서버모드] 섹션 배치저장 오류(개별저장 폴백): " + ex.Message);
                // 폴백: 실패 시 기존 방식으로라도 저장
                foreach (var p in kv) Util.Function.IniWriteValue(sec, p.Key, p.Value);
            }
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
        }

        /// <summary>
        /// 동작모드에 따라 '원격 차번인식 사용'(chkOcrRemote) 상태 연동.
        /// 원격 인식 모드(기본2CH-원격인식=rdStartCam, 서버모드=rdbServerMode): 활성+체크.
        /// 그 외(인식X-ONLY자료처리=rdStartCom, 기본2CH모드=rdStartBoth): 체크 해제+비활성(선택 불가).
        /// </summary>
        private void UpdateRemoteOcrByMode()
        {
            if (chkOcrRemote == null) return;
            bool remoteMode = rdStartCam.Checked || rdbServerMode.Checked;
            if (remoteMode)
            {
                chkOcrRemote.Enabled = true;
                if (!chkOcrRemote.Checked) chkOcrRemote.Checked = true;
            }
            else
            {
                if (chkOcrRemote.Checked) chkOcrRemote.Checked = false;
                chkOcrRemote.Enabled = false;
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
            // 카메라별 종류 선택: WGWK는 해당 카메라 CameraSource, iNova1/2는 전역 iNovaType
            OnCameraTypeChanged();
        }

        private void btnStayCommit_Click(object sender, EventArgs e)
        {
           
        }
    }
}
