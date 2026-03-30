namespace KyungsinLPR
{
    partial class frmLprMain
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다.
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마십시오.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmLprMain));
            this.labelWarning = new System.Windows.Forms.Label();
            this.btnEnv = new System.Windows.Forms.Button();
            this.PicLpr1Image = new System.Windows.Forms.PictureBox();
            this.PicLpr2Image = new System.Windows.Forms.PictureBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblCam1ChName = new System.Windows.Forms.Label();
            this.lblCam1RegResult = new System.Windows.Forms.Label();
            this.lblCam1RegSpeed = new System.Windows.Forms.Label();
            this.lblCam1BoardType = new System.Windows.Forms.Label();
            this.lblCam1RegType = new System.Windows.Forms.Label();
            this.lblCam1TriggerMode = new System.Windows.Forms.Label();
            this.lblCam1TriggerCnt = new System.Windows.Forms.Label();
            this.lblCam1Mode = new System.Windows.Forms.Label();
            this.lblCam1FrameRate = new System.Windows.Forms.Label();
            this.lblCam1Exposure = new System.Windows.Forms.Label();
            this.lblCam1IP = new System.Windows.Forms.Label();
            this.lblCam1TcpUdp = new System.Windows.Forms.Label();
            this.lblCam1Loop = new System.Windows.Forms.Label();
            this.lblCam1SN = new System.Windows.Forms.Label();
            this.lblCam1FWVer = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.lblCam2ChName = new System.Windows.Forms.Label();
            this.lblCam2RegResult = new System.Windows.Forms.Label();
            this.lblCam2RegSpeed = new System.Windows.Forms.Label();
            this.lblCam2BoardType = new System.Windows.Forms.Label();
            this.lblCam2RegType = new System.Windows.Forms.Label();
            this.lblCam2TriggerMode = new System.Windows.Forms.Label();
            this.lblCam2TriggerCnt = new System.Windows.Forms.Label();
            this.lblCam2Mode = new System.Windows.Forms.Label();
            this.lblCam2FrameRate = new System.Windows.Forms.Label();
            this.lblCam2Exposure = new System.Windows.Forms.Label();
            this.lblCam2IP = new System.Windows.Forms.Label();
            this.lblCam2TcpUdp = new System.Windows.Forms.Label();
            this.lblCam2Loop = new System.Windows.Forms.Label();
            this.lblCam2SN = new System.Windows.Forms.Label();
            this.lblCam2FWVer = new System.Windows.Forms.Label();
            this.btnCam1Capture = new System.Windows.Forms.Button();
            this.btnCam2Capture = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.btnLog = new System.Windows.Forms.Button();
            this.btnTestCapture1 = new System.Windows.Forms.Button();
            this.btnTestCapture2 = new System.Windows.Forms.Button();
            this.txtTestCarNo = new System.Windows.Forms.TextBox();
            this.chkLoop1 = new System.Windows.Forms.CheckBox();
            this.chkLoop2 = new System.Windows.Forms.CheckBox();
            this.btnLoop = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.grpCoreInit = new System.Windows.Forms.GroupBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.timer_Core = new System.Windows.Forms.Timer(this.components);
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.timer_Full_Check = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.PicLpr1Image)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PicLpr2Image)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.grpCoreInit.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelWarning
            // 
            this.labelWarning.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelWarning.AutoSize = true;
            this.labelWarning.BackColor = System.Drawing.Color.Transparent;
            this.labelWarning.ForeColor = System.Drawing.Color.Red;
            this.labelWarning.Image = ((System.Drawing.Image)(resources.GetObject("labelWarning.Image")));
            this.labelWarning.Location = new System.Drawing.Point(10, 256);
            this.labelWarning.Name = "labelWarning";
            this.labelWarning.Size = new System.Drawing.Size(0, 12);
            this.labelWarning.TabIndex = 76;
            // 
            // btnEnv
            // 
            this.btnEnv.Location = new System.Drawing.Point(409, 34);
            this.btnEnv.Name = "btnEnv";
            this.btnEnv.Size = new System.Drawing.Size(75, 23);
            this.btnEnv.TabIndex = 78;
            this.btnEnv.Text = "환경설정";
            this.btnEnv.UseVisualStyleBackColor = true;
            this.btnEnv.Click += new System.EventHandler(this.btnEnv_Click);
            this.btnEnv.KeyUp += new System.Windows.Forms.KeyEventHandler(this.frmCamMain_KeyUp);
            // 
            // PicLpr1Image
            // 
            this.PicLpr1Image.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("PicLpr1Image.BackgroundImage")));
            this.PicLpr1Image.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.PicLpr1Image.Location = new System.Drawing.Point(10, 63);
            this.PicLpr1Image.Name = "PicLpr1Image";
            this.PicLpr1Image.Size = new System.Drawing.Size(474, 334);
            this.PicLpr1Image.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.PicLpr1Image.TabIndex = 79;
            this.PicLpr1Image.TabStop = false;
            // 
            // PicLpr2Image
            // 
            this.PicLpr2Image.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("PicLpr2Image.BackgroundImage")));
            this.PicLpr2Image.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.PicLpr2Image.Location = new System.Drawing.Point(500, 63);
            this.PicLpr2Image.Name = "PicLpr2Image";
            this.PicLpr2Image.Size = new System.Drawing.Size(474, 334);
            this.PicLpr2Image.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.PicLpr2Image.TabIndex = 80;
            this.PicLpr2Image.TabStop = false;
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.groupBox1.Controls.Add(this.lblCam1ChName);
            this.groupBox1.Controls.Add(this.lblCam1RegResult);
            this.groupBox1.Controls.Add(this.lblCam1RegSpeed);
            this.groupBox1.Controls.Add(this.lblCam1BoardType);
            this.groupBox1.Controls.Add(this.lblCam1RegType);
            this.groupBox1.Controls.Add(this.lblCam1TriggerMode);
            this.groupBox1.Controls.Add(this.lblCam1TriggerCnt);
            this.groupBox1.Controls.Add(this.lblCam1Mode);
            this.groupBox1.Controls.Add(this.lblCam1FrameRate);
            this.groupBox1.Controls.Add(this.lblCam1Exposure);
            this.groupBox1.Controls.Add(this.lblCam1IP);
            this.groupBox1.Controls.Add(this.lblCam1TcpUdp);
            this.groupBox1.Controls.Add(this.lblCam1Loop);
            this.groupBox1.Controls.Add(this.lblCam1SN);
            this.groupBox1.Controls.Add(this.lblCam1FWVer);
            this.groupBox1.Location = new System.Drawing.Point(10, 403);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(474, 127);
            this.groupBox1.TabIndex = 81;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "LPR Information";
            // 
            // lblCam1ChName
            // 
            this.lblCam1ChName.AutoSize = true;
            this.lblCam1ChName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.lblCam1ChName.Location = new System.Drawing.Point(336, 101);
            this.lblCam1ChName.Name = "lblCam1ChName";
            this.lblCam1ChName.Size = new System.Drawing.Size(41, 12);
            this.lblCam1ChName.TabIndex = 131;
            this.lblCam1ChName.Text = "채널명";
            // 
            // lblCam1RegResult
            // 
            this.lblCam1RegResult.AutoSize = true;
            this.lblCam1RegResult.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.lblCam1RegResult.Location = new System.Drawing.Point(336, 80);
            this.lblCam1RegResult.Name = "lblCam1RegResult";
            this.lblCam1RegResult.Size = new System.Drawing.Size(53, 12);
            this.lblCam1RegResult.TabIndex = 123;
            this.lblCam1RegResult.Text = "인식결과";
            // 
            // lblCam1RegSpeed
            // 
            this.lblCam1RegSpeed.AutoSize = true;
            this.lblCam1RegSpeed.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.lblCam1RegSpeed.Location = new System.Drawing.Point(336, 59);
            this.lblCam1RegSpeed.Name = "lblCam1RegSpeed";
            this.lblCam1RegSpeed.Size = new System.Drawing.Size(53, 12);
            this.lblCam1RegSpeed.TabIndex = 122;
            this.lblCam1RegSpeed.Text = "인식속도";
            // 
            // lblCam1BoardType
            // 
            this.lblCam1BoardType.AutoSize = true;
            this.lblCam1BoardType.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.lblCam1BoardType.Location = new System.Drawing.Point(336, 38);
            this.lblCam1BoardType.Name = "lblCam1BoardType";
            this.lblCam1BoardType.Size = new System.Drawing.Size(69, 12);
            this.lblCam1BoardType.TabIndex = 84;
            this.lblCam1BoardType.Text = "IO보드 타입";
            // 
            // lblCam1RegType
            // 
            this.lblCam1RegType.AutoSize = true;
            this.lblCam1RegType.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.lblCam1RegType.Location = new System.Drawing.Point(336, 17);
            this.lblCam1RegType.Name = "lblCam1RegType";
            this.lblCam1RegType.Size = new System.Drawing.Size(57, 12);
            this.lblCam1RegType.TabIndex = 83;
            this.lblCam1RegType.Text = "인식 방식";
            // 
            // lblCam1TriggerMode
            // 
            this.lblCam1TriggerMode.AutoSize = true;
            this.lblCam1TriggerMode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.lblCam1TriggerMode.Location = new System.Drawing.Point(166, 59);
            this.lblCam1TriggerMode.Name = "lblCam1TriggerMode";
            this.lblCam1TriggerMode.Size = new System.Drawing.Size(77, 12);
            this.lblCam1TriggerMode.TabIndex = 130;
            this.lblCam1TriggerMode.Text = "TriggerMode";
            // 
            // lblCam1TriggerCnt
            // 
            this.lblCam1TriggerCnt.AutoSize = true;
            this.lblCam1TriggerCnt.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.lblCam1TriggerCnt.Location = new System.Drawing.Point(166, 38);
            this.lblCam1TriggerCnt.Name = "lblCam1TriggerCnt";
            this.lblCam1TriggerCnt.Size = new System.Drawing.Size(64, 12);
            this.lblCam1TriggerCnt.TabIndex = 129;
            this.lblCam1TriggerCnt.Text = "TriggerCnt";
            // 
            // lblCam1Mode
            // 
            this.lblCam1Mode.AutoSize = true;
            this.lblCam1Mode.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.lblCam1Mode.Location = new System.Drawing.Point(166, 17);
            this.lblCam1Mode.Name = "lblCam1Mode";
            this.lblCam1Mode.Size = new System.Drawing.Size(37, 12);
            this.lblCam1Mode.TabIndex = 128;
            this.lblCam1Mode.Text = "Mode";
            // 
            // lblCam1FrameRate
            // 
            this.lblCam1FrameRate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblCam1FrameRate.AutoSize = true;
            this.lblCam1FrameRate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.lblCam1FrameRate.ForeColor = System.Drawing.Color.Black;
            this.lblCam1FrameRate.Image = ((System.Drawing.Image)(resources.GetObject("lblCam1FrameRate.Image")));
            this.lblCam1FrameRate.Location = new System.Drawing.Point(6, 80);
            this.lblCam1FrameRate.Name = "lblCam1FrameRate";
            this.lblCam1FrameRate.Size = new System.Drawing.Size(66, 12);
            this.lblCam1FrameRate.TabIndex = 127;
            this.lblCam1FrameRate.Text = "FrameRate";
            // 
            // lblCam1Exposure
            // 
            this.lblCam1Exposure.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblCam1Exposure.AutoSize = true;
            this.lblCam1Exposure.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.lblCam1Exposure.ForeColor = System.Drawing.Color.Black;
            this.lblCam1Exposure.Image = ((System.Drawing.Image)(resources.GetObject("lblCam1Exposure.Image")));
            this.lblCam1Exposure.Location = new System.Drawing.Point(6, 59);
            this.lblCam1Exposure.Name = "lblCam1Exposure";
            this.lblCam1Exposure.Size = new System.Drawing.Size(59, 12);
            this.lblCam1Exposure.TabIndex = 126;
            this.lblCam1Exposure.Text = "Exposure";
            // 
            // lblCam1IP
            // 
            this.lblCam1IP.AutoSize = true;
            this.lblCam1IP.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.lblCam1IP.Location = new System.Drawing.Point(166, 101);
            this.lblCam1IP.Name = "lblCam1IP";
            this.lblCam1IP.Size = new System.Drawing.Size(52, 12);
            this.lblCam1IP.TabIndex = 125;
            this.lblCam1IP.Text = "카메라IP";
            // 
            // lblCam1TcpUdp
            // 
            this.lblCam1TcpUdp.AutoSize = true;
            this.lblCam1TcpUdp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.lblCam1TcpUdp.Location = new System.Drawing.Point(166, 80);
            this.lblCam1TcpUdp.Name = "lblCam1TcpUdp";
            this.lblCam1TcpUdp.Size = new System.Drawing.Size(55, 12);
            this.lblCam1TcpUdp.TabIndex = 124;
            this.lblCam1TcpUdp.Text = "Tcp/Udp";
            // 
            // lblCam1Loop
            // 
            this.lblCam1Loop.AutoSize = true;
            this.lblCam1Loop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.lblCam1Loop.Location = new System.Drawing.Point(6, 101);
            this.lblCam1Loop.Name = "lblCam1Loop";
            this.lblCam1Loop.Size = new System.Drawing.Size(33, 12);
            this.lblCam1Loop.TabIndex = 121;
            this.lblCam1Loop.Text = "Loop";
            // 
            // lblCam1SN
            // 
            this.lblCam1SN.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblCam1SN.AutoSize = true;
            this.lblCam1SN.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.lblCam1SN.ForeColor = System.Drawing.Color.Black;
            this.lblCam1SN.Image = ((System.Drawing.Image)(resources.GetObject("lblCam1SN.Image")));
            this.lblCam1SN.Location = new System.Drawing.Point(6, 17);
            this.lblCam1SN.Name = "lblCam1SN";
            this.lblCam1SN.Size = new System.Drawing.Size(22, 12);
            this.lblCam1SN.TabIndex = 120;
            this.lblCam1SN.Text = "SN";
            // 
            // lblCam1FWVer
            // 
            this.lblCam1FWVer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblCam1FWVer.AutoSize = true;
            this.lblCam1FWVer.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.lblCam1FWVer.ForeColor = System.Drawing.Color.Black;
            this.lblCam1FWVer.Image = ((System.Drawing.Image)(resources.GetObject("lblCam1FWVer.Image")));
            this.lblCam1FWVer.Location = new System.Drawing.Point(6, 38);
            this.lblCam1FWVer.Name = "lblCam1FWVer";
            this.lblCam1FWVer.Size = new System.Drawing.Size(43, 12);
            this.lblCam1FWVer.TabIndex = 119;
            this.lblCam1FWVer.Text = "FW ver";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.lblCam2ChName);
            this.groupBox2.Controls.Add(this.lblCam2RegResult);
            this.groupBox2.Controls.Add(this.lblCam2RegSpeed);
            this.groupBox2.Controls.Add(this.lblCam2BoardType);
            this.groupBox2.Controls.Add(this.lblCam2RegType);
            this.groupBox2.Controls.Add(this.lblCam2TriggerMode);
            this.groupBox2.Controls.Add(this.lblCam2TriggerCnt);
            this.groupBox2.Controls.Add(this.lblCam2Mode);
            this.groupBox2.Controls.Add(this.lblCam2FrameRate);
            this.groupBox2.Controls.Add(this.lblCam2Exposure);
            this.groupBox2.Controls.Add(this.lblCam2IP);
            this.groupBox2.Controls.Add(this.lblCam2TcpUdp);
            this.groupBox2.Controls.Add(this.lblCam2Loop);
            this.groupBox2.Controls.Add(this.lblCam2SN);
            this.groupBox2.Controls.Add(this.lblCam2FWVer);
            this.groupBox2.Location = new System.Drawing.Point(500, 403);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(474, 127);
            this.groupBox2.TabIndex = 82;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "LPR Information";
            // 
            // lblCam2ChName
            // 
            this.lblCam2ChName.AutoSize = true;
            this.lblCam2ChName.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.lblCam2ChName.Location = new System.Drawing.Point(336, 101);
            this.lblCam2ChName.Name = "lblCam2ChName";
            this.lblCam2ChName.Size = new System.Drawing.Size(41, 12);
            this.lblCam2ChName.TabIndex = 147;
            this.lblCam2ChName.Text = "채널명";
            // 
            // lblCam2RegResult
            // 
            this.lblCam2RegResult.AutoSize = true;
            this.lblCam2RegResult.BackColor = System.Drawing.Color.Transparent;
            this.lblCam2RegResult.Location = new System.Drawing.Point(336, 80);
            this.lblCam2RegResult.Name = "lblCam2RegResult";
            this.lblCam2RegResult.Size = new System.Drawing.Size(53, 12);
            this.lblCam2RegResult.TabIndex = 139;
            this.lblCam2RegResult.Text = "인식결과";
            // 
            // lblCam2RegSpeed
            // 
            this.lblCam2RegSpeed.AutoSize = true;
            this.lblCam2RegSpeed.BackColor = System.Drawing.Color.Transparent;
            this.lblCam2RegSpeed.Location = new System.Drawing.Point(336, 59);
            this.lblCam2RegSpeed.Name = "lblCam2RegSpeed";
            this.lblCam2RegSpeed.Size = new System.Drawing.Size(57, 12);
            this.lblCam2RegSpeed.TabIndex = 138;
            this.lblCam2RegSpeed.Text = "인식 속도";
            // 
            // lblCam2BoardType
            // 
            this.lblCam2BoardType.AutoSize = true;
            this.lblCam2BoardType.BackColor = System.Drawing.Color.Transparent;
            this.lblCam2BoardType.Location = new System.Drawing.Point(336, 38);
            this.lblCam2BoardType.Name = "lblCam2BoardType";
            this.lblCam2BoardType.Size = new System.Drawing.Size(69, 12);
            this.lblCam2BoardType.TabIndex = 134;
            this.lblCam2BoardType.Text = "IO보드 타입";
            // 
            // lblCam2RegType
            // 
            this.lblCam2RegType.AutoSize = true;
            this.lblCam2RegType.BackColor = System.Drawing.Color.Transparent;
            this.lblCam2RegType.Location = new System.Drawing.Point(336, 17);
            this.lblCam2RegType.Name = "lblCam2RegType";
            this.lblCam2RegType.Size = new System.Drawing.Size(57, 12);
            this.lblCam2RegType.TabIndex = 133;
            this.lblCam2RegType.Text = "인식 방식";
            // 
            // lblCam2TriggerMode
            // 
            this.lblCam2TriggerMode.AutoSize = true;
            this.lblCam2TriggerMode.BackColor = System.Drawing.Color.Transparent;
            this.lblCam2TriggerMode.Location = new System.Drawing.Point(188, 59);
            this.lblCam2TriggerMode.Name = "lblCam2TriggerMode";
            this.lblCam2TriggerMode.Size = new System.Drawing.Size(77, 12);
            this.lblCam2TriggerMode.TabIndex = 146;
            this.lblCam2TriggerMode.Text = "TriggerMode";
            // 
            // lblCam2TriggerCnt
            // 
            this.lblCam2TriggerCnt.AutoSize = true;
            this.lblCam2TriggerCnt.BackColor = System.Drawing.Color.Transparent;
            this.lblCam2TriggerCnt.Location = new System.Drawing.Point(188, 38);
            this.lblCam2TriggerCnt.Name = "lblCam2TriggerCnt";
            this.lblCam2TriggerCnt.Size = new System.Drawing.Size(64, 12);
            this.lblCam2TriggerCnt.TabIndex = 145;
            this.lblCam2TriggerCnt.Text = "TriggerCnt";
            // 
            // lblCam2Mode
            // 
            this.lblCam2Mode.AutoSize = true;
            this.lblCam2Mode.BackColor = System.Drawing.Color.Transparent;
            this.lblCam2Mode.Location = new System.Drawing.Point(188, 17);
            this.lblCam2Mode.Name = "lblCam2Mode";
            this.lblCam2Mode.Size = new System.Drawing.Size(37, 12);
            this.lblCam2Mode.TabIndex = 144;
            this.lblCam2Mode.Text = "Mode";
            // 
            // lblCam2FrameRate
            // 
            this.lblCam2FrameRate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblCam2FrameRate.AutoSize = true;
            this.lblCam2FrameRate.BackColor = System.Drawing.Color.Transparent;
            this.lblCam2FrameRate.ForeColor = System.Drawing.Color.Black;
            this.lblCam2FrameRate.Image = ((System.Drawing.Image)(resources.GetObject("lblCam2FrameRate.Image")));
            this.lblCam2FrameRate.Location = new System.Drawing.Point(6, 80);
            this.lblCam2FrameRate.Name = "lblCam2FrameRate";
            this.lblCam2FrameRate.Size = new System.Drawing.Size(66, 12);
            this.lblCam2FrameRate.TabIndex = 143;
            this.lblCam2FrameRate.Text = "FrameRate";
            // 
            // lblCam2Exposure
            // 
            this.lblCam2Exposure.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblCam2Exposure.AutoSize = true;
            this.lblCam2Exposure.BackColor = System.Drawing.Color.Transparent;
            this.lblCam2Exposure.ForeColor = System.Drawing.Color.Black;
            this.lblCam2Exposure.Image = ((System.Drawing.Image)(resources.GetObject("lblCam2Exposure.Image")));
            this.lblCam2Exposure.Location = new System.Drawing.Point(6, 59);
            this.lblCam2Exposure.Name = "lblCam2Exposure";
            this.lblCam2Exposure.Size = new System.Drawing.Size(59, 12);
            this.lblCam2Exposure.TabIndex = 142;
            this.lblCam2Exposure.Text = "Exposure";
            // 
            // lblCam2IP
            // 
            this.lblCam2IP.AutoSize = true;
            this.lblCam2IP.BackColor = System.Drawing.Color.Transparent;
            this.lblCam2IP.Location = new System.Drawing.Point(188, 101);
            this.lblCam2IP.Name = "lblCam2IP";
            this.lblCam2IP.Size = new System.Drawing.Size(52, 12);
            this.lblCam2IP.TabIndex = 141;
            this.lblCam2IP.Text = "카메라IP";
            // 
            // lblCam2TcpUdp
            // 
            this.lblCam2TcpUdp.AutoSize = true;
            this.lblCam2TcpUdp.BackColor = System.Drawing.Color.Transparent;
            this.lblCam2TcpUdp.Location = new System.Drawing.Point(188, 80);
            this.lblCam2TcpUdp.Name = "lblCam2TcpUdp";
            this.lblCam2TcpUdp.Size = new System.Drawing.Size(55, 12);
            this.lblCam2TcpUdp.TabIndex = 140;
            this.lblCam2TcpUdp.Text = "Tcp/Udp";
            // 
            // lblCam2Loop
            // 
            this.lblCam2Loop.AutoSize = true;
            this.lblCam2Loop.BackColor = System.Drawing.Color.Transparent;
            this.lblCam2Loop.Location = new System.Drawing.Point(6, 101);
            this.lblCam2Loop.Name = "lblCam2Loop";
            this.lblCam2Loop.Size = new System.Drawing.Size(33, 12);
            this.lblCam2Loop.TabIndex = 137;
            this.lblCam2Loop.Text = "Loop";
            // 
            // lblCam2SN
            // 
            this.lblCam2SN.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblCam2SN.AutoSize = true;
            this.lblCam2SN.BackColor = System.Drawing.Color.Transparent;
            this.lblCam2SN.ForeColor = System.Drawing.Color.Black;
            this.lblCam2SN.Image = ((System.Drawing.Image)(resources.GetObject("lblCam2SN.Image")));
            this.lblCam2SN.Location = new System.Drawing.Point(6, 17);
            this.lblCam2SN.Name = "lblCam2SN";
            this.lblCam2SN.Size = new System.Drawing.Size(22, 12);
            this.lblCam2SN.TabIndex = 136;
            this.lblCam2SN.Text = "SN";
            // 
            // lblCam2FWVer
            // 
            this.lblCam2FWVer.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblCam2FWVer.AutoSize = true;
            this.lblCam2FWVer.BackColor = System.Drawing.Color.Transparent;
            this.lblCam2FWVer.ForeColor = System.Drawing.Color.Black;
            this.lblCam2FWVer.Image = ((System.Drawing.Image)(resources.GetObject("lblCam2FWVer.Image")));
            this.lblCam2FWVer.Location = new System.Drawing.Point(6, 38);
            this.lblCam2FWVer.Name = "lblCam2FWVer";
            this.lblCam2FWVer.Size = new System.Drawing.Size(43, 12);
            this.lblCam2FWVer.TabIndex = 135;
            this.lblCam2FWVer.Text = "FW ver";
            // 
            // btnCam1Capture
            // 
            this.btnCam1Capture.Location = new System.Drawing.Point(247, 34);
            this.btnCam1Capture.Name = "btnCam1Capture";
            this.btnCam1Capture.Size = new System.Drawing.Size(75, 23);
            this.btnCam1Capture.TabIndex = 138;
            this.btnCam1Capture.Text = "캡쳐1(F5)";
            this.btnCam1Capture.UseVisualStyleBackColor = true;
            this.btnCam1Capture.Click += new System.EventHandler(this.btnCam1Capture_Click);
            this.btnCam1Capture.KeyUp += new System.Windows.Forms.KeyEventHandler(this.frmCamMain_KeyUp);
            // 
            // btnCam2Capture
            // 
            this.btnCam2Capture.Location = new System.Drawing.Point(328, 34);
            this.btnCam2Capture.Name = "btnCam2Capture";
            this.btnCam2Capture.Size = new System.Drawing.Size(75, 23);
            this.btnCam2Capture.TabIndex = 139;
            this.btnCam2Capture.Text = "캡쳐2(F6)";
            this.btnCam2Capture.UseVisualStyleBackColor = true;
            this.btnCam2Capture.Click += new System.EventHandler(this.btnCam2Capture_Click);
            this.btnCam2Capture.KeyUp += new System.Windows.Forms.KeyEventHandler(this.frmCamMain_KeyUp);
            // 
            // timer1
            // 
            this.timer1.Interval = 5000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // btnLog
            // 
            this.btnLog.Location = new System.Drawing.Point(490, 34);
            this.btnLog.Name = "btnLog";
            this.btnLog.Size = new System.Drawing.Size(75, 23);
            this.btnLog.TabIndex = 140;
            this.btnLog.Text = "통신 상태";
            this.btnLog.UseVisualStyleBackColor = true;
            this.btnLog.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnTestCapture1
            // 
            this.btnTestCapture1.Location = new System.Drawing.Point(247, 5);
            this.btnTestCapture1.Name = "btnTestCapture1";
            this.btnTestCapture1.Size = new System.Drawing.Size(75, 23);
            this.btnTestCapture1.TabIndex = 141;
            this.btnTestCapture1.Text = "TEST1";
            this.btnTestCapture1.UseVisualStyleBackColor = true;
            this.btnTestCapture1.Visible = false;
            this.btnTestCapture1.Click += new System.EventHandler(this.btnTestCapture1_Click);
            this.btnTestCapture1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.BtnTestCapture1_MouseUp);
            // 
            // btnTestCapture2
            // 
            this.btnTestCapture2.Location = new System.Drawing.Point(328, 5);
            this.btnTestCapture2.Name = "btnTestCapture2";
            this.btnTestCapture2.Size = new System.Drawing.Size(75, 23);
            this.btnTestCapture2.TabIndex = 142;
            this.btnTestCapture2.Text = "TEST2";
            this.btnTestCapture2.UseVisualStyleBackColor = true;
            this.btnTestCapture2.Visible = false;
            this.btnTestCapture2.Click += new System.EventHandler(this.btnTestCapture2_Click);
            this.btnTestCapture2.MouseUp += new System.Windows.Forms.MouseEventHandler(this.BtnTestCapture2_MouseUp);
            // 
            // txtTestCarNo
            // 
            this.txtTestCarNo.ImeMode = System.Windows.Forms.ImeMode.Hangul;
            this.txtTestCarNo.Location = new System.Drawing.Point(409, 5);
            this.txtTestCarNo.Name = "txtTestCarNo";
            this.txtTestCarNo.Size = new System.Drawing.Size(100, 21);
            this.txtTestCarNo.TabIndex = 143;
            this.txtTestCarNo.Visible = false;
            // 
            // chkLoop1
            // 
            this.chkLoop1.AutoSize = true;
            this.chkLoop1.Location = new System.Drawing.Point(584, 9);
            this.chkLoop1.Name = "chkLoop1";
            this.chkLoop1.Size = new System.Drawing.Size(58, 16);
            this.chkLoop1.TabIndex = 144;
            this.chkLoop1.Text = "Loop1";
            this.chkLoop1.UseVisualStyleBackColor = true;
            this.chkLoop1.Visible = false;
            // 
            // chkLoop2
            // 
            this.chkLoop2.AutoSize = true;
            this.chkLoop2.Location = new System.Drawing.Point(584, 38);
            this.chkLoop2.Name = "chkLoop2";
            this.chkLoop2.Size = new System.Drawing.Size(58, 16);
            this.chkLoop2.TabIndex = 145;
            this.chkLoop2.Text = "Loop2";
            this.chkLoop2.UseVisualStyleBackColor = true;
            this.chkLoop2.Visible = false;
            // 
            // btnLoop
            // 
            this.btnLoop.Location = new System.Drawing.Point(650, 12);
            this.btnLoop.Name = "btnLoop";
            this.btnLoop.Size = new System.Drawing.Size(75, 45);
            this.btnLoop.TabIndex = 146;
            this.btnLoop.Text = "Loop Active";
            this.btnLoop.UseVisualStyleBackColor = true;
            this.btnLoop.Visible = false;
            this.btnLoop.Click += new System.EventHandler(this.btnLoop_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(10, 1);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(220, 60);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 147;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.DoubleClick += new System.EventHandler(this.pictureBox1_DoubleClick);
            // 
            // grpCoreInit
            // 
            this.grpCoreInit.Controls.Add(this.progressBar1);
            this.grpCoreInit.Location = new System.Drawing.Point(343, 239);
            this.grpCoreInit.Name = "grpCoreInit";
            this.grpCoreInit.Size = new System.Drawing.Size(298, 64);
            this.grpCoreInit.TabIndex = 148;
            this.grpCoreInit.TabStop = false;
            this.grpCoreInit.Text = "인식 모듈 초기화 중...";
            this.grpCoreInit.Visible = false;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(6, 21);
            this.progressBar1.Maximum = 50;
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(286, 23);
            this.progressBar1.TabIndex = 0;
            // 
            // timer_Core
            // 
            this.timer_Core.Tick += new System.EventHandler(this.Timer_Core_Tick);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Red;
            this.label1.Location = new System.Drawing.Point(11, 64);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(10, 10);
            this.label1.TabIndex = 149;
            this.label1.Visible = false;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.Red;
            this.label2.Location = new System.Drawing.Point(501, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(10, 10);
            this.label2.TabIndex = 150;
            this.label2.Visible = false;
            // 
            // timer_Full_Check
            // 
            this.timer_Full_Check.Enabled = true;
            this.timer_Full_Check.Interval = 1000;
            this.timer_Full_Check.Tick += new System.EventHandler(this.Timer_Full_Check_Tick);
            // 
            // frmLprMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(172)))), ((int)(((byte)(220)))), ((int)(((byte)(244)))));
            this.ClientSize = new System.Drawing.Size(984, 542);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.grpCoreInit);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.btnLoop);
            this.Controls.Add(this.chkLoop2);
            this.Controls.Add(this.chkLoop1);
            this.Controls.Add(this.txtTestCarNo);
            this.Controls.Add(this.btnTestCapture2);
            this.Controls.Add(this.btnTestCapture1);
            this.Controls.Add(this.btnLog);
            this.Controls.Add(this.btnCam2Capture);
            this.Controls.Add(this.btnCam1Capture);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.PicLpr2Image);
            this.Controls.Add(this.PicLpr1Image);
            this.Controls.Add(this.btnEnv);
            this.Controls.Add(this.labelWarning);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Name = "frmLprMain";
            this.Text = "KSP-LPR1000(화상 인식 시스템)";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmLprMain_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmCamMain_FormClosed);
            this.Load += new System.EventHandler(this.frmCamMain_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.frmLprMain_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.frmCamMain_KeyUp);
            ((System.ComponentModel.ISupportInitialize)(this.PicLpr1Image)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PicLpr2Image)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.grpCoreInit.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelWarning;
        private System.Windows.Forms.Button btnEnv;
        private System.Windows.Forms.PictureBox PicLpr1Image;
        private System.Windows.Forms.PictureBox PicLpr2Image;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label lblCam1BoardType;
        private System.Windows.Forms.Label lblCam1RegType;
        private System.Windows.Forms.Label lblCam1IP;
        private System.Windows.Forms.Label lblCam1TcpUdp;
        private System.Windows.Forms.Label lblCam1Loop;
        private System.Windows.Forms.Label lblCam1SN;
        private System.Windows.Forms.Label lblCam1FWVer;
        private System.Windows.Forms.Button btnCam1Capture;
        private System.Windows.Forms.Button btnCam2Capture;
        private System.Windows.Forms.Label lblCam1Exposure;
        private System.Windows.Forms.Label lblCam1FrameRate;
        private System.Windows.Forms.Label lblCam1TriggerCnt;
        private System.Windows.Forms.Label lblCam1Mode;
        private System.Windows.Forms.Label lblCam1TriggerMode;
        private System.Windows.Forms.Label lblCam2TriggerMode;
        private System.Windows.Forms.Label lblCam2TriggerCnt;
        private System.Windows.Forms.Label lblCam2Mode;
        private System.Windows.Forms.Label lblCam2FrameRate;
        private System.Windows.Forms.Label lblCam2Exposure;
        private System.Windows.Forms.Label lblCam2IP;
        private System.Windows.Forms.Label lblCam2TcpUdp;
        private System.Windows.Forms.Label lblCam2Loop;
        private System.Windows.Forms.Label lblCam2SN;
        private System.Windows.Forms.Label lblCam2FWVer;
        private System.Windows.Forms.Label lblCam2BoardType;
        private System.Windows.Forms.Label lblCam2RegType;
        private System.Windows.Forms.Label lblCam1ChName;
        private System.Windows.Forms.Label lblCam2ChName;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button btnLog;
        public System.Windows.Forms.Label lblCam1RegResult;
        public System.Windows.Forms.Label lblCam1RegSpeed;
        public System.Windows.Forms.Label lblCam2RegResult;
        public System.Windows.Forms.Label lblCam2RegSpeed;
        private System.Windows.Forms.Button btnTestCapture1;
        private System.Windows.Forms.Button btnTestCapture2;
        private System.Windows.Forms.TextBox txtTestCarNo;
        private System.Windows.Forms.CheckBox chkLoop1;
        private System.Windows.Forms.CheckBox chkLoop2;
        private System.Windows.Forms.Button btnLoop;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.GroupBox grpCoreInit;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Timer timer_Core;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Timer timer_Full_Check;
    }
}

