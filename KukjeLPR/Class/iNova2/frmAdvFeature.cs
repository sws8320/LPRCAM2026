using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace KyungsinLPR.iNova2 {
    /// <summary>
    /// A WinForm class that provides camera control GUI for i-Nova.
    /// </summary>
    public partial class frmAdvFeature : Form
    {
        private IPCamera m_camera;
        private bool m_loaded = false;

        private ALC m_alc = new ALC();
        private TextBox[] m_exposures = new TextBox[4];
        private System.Windows.Forms.TrackBar []m_aGains = new System.Windows.Forms.TrackBar[4];
        private System.Windows.Forms.TrackBar []m_dGains = new System.Windows.Forms.TrackBar[4];
        private ToolTip m_tooltip = new ToolTip();
        private string m_serial = null;

        public frmAdvFeature(IPCamera camera)
        {
            InitializeComponent();

            m_camera = camera;
            m_tooltip.AutoPopDelay = 5000;
            m_tooltip.InitialDelay = 500;
            m_tooltip.ReshowDelay = 500;
        }

        private void AdvFeatureForm_Load(object sender, EventArgs e)
        {
            m_exposures[0] = textExposure1;
            m_exposures[1] = textExposure2;
            m_exposures[2] = textExposure3;
            m_exposures[3] = textExposure4;
            m_aGains[0] = trackAGain1;
            m_aGains[1] = trackAGain2;
            m_aGains[2] = trackAGain3;
            m_aGains[3] = trackAGain4;
            m_dGains[0] = trackDGain1;
            m_dGains[1] = trackDGain2;
            m_dGains[2] = trackDGain3;
            m_dGains[3] = trackDGain4;

            SetupGeneralGUI();
            SetupTriggerFlashGUI();
            SetupBracketGUI();
            SetupCodecGUI();

            if (m_camera.IsInova2())
            {
                SetupiNova2GUI();
                tabControl1.TabPages.Remove(tabiNova1);
            }
            else
            {
                label31.Text = "ALC/AWB Area";
                label31.Location = new System.Drawing.Point(6, 366);
                label31.Size = new System.Drawing.Size(91, 12);
                SetupiNova1GUI();
                tabControl1.TabPages.Remove(tabiNova2);
            }

            if (m_camera.IsInova2_Zoom())
            {
                m_camera.GetSerialNumber(out m_serial);
                SetupZoomGUI();
            }
            else
            {
                tabControl1.TabPages.Remove(tabZoom);
            }

            if (m_camera.IsInova2_Motor())
            {
                // nothing to setup for now.
            }
            else
            {
                tabControl1.TabPages.Remove(tabLens);
            }

            m_loaded = true;
        }

        private void AdvFeatureForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            m_runPresetLoop = false;
        }

        private ComboBox[] comboEVs = new ComboBox[8];

        private void SetupiNova1GUI()
        {
            comboEVs[0] = comboEV1;
            comboEVs[1] = comboEV2;
            comboEVs[2] = comboEV3;
            comboEVs[3] = comboEV4;
            comboEVs[4] = comboEV5;
            comboEVs[5] = comboEV6;
            comboEVs[6] = comboEV7;
            comboEVs[7] = comboEV8;

            bool enable;
            double []ev_sequence;
            var err = m_camera.GetSmartBracket(out enable, out ev_sequence);
            if (err == IPCamError.OK)
            {
                chkEnableSB.Checked = enable;

                int trigImageCount = 0;
                try
                {
                    trigImageCount = Convert.ToInt32(textTrigImgNum.Text);
                    comboFrameCount.SelectedIndex = Math.Min(Math.Max(trigImageCount - 3, 0), comboFrameCount.Items.Count - 1);
                    comboFrameCount.Enabled = enable;
                    // Frame 1 is fixed to 0 EV.
                    comboEVs[0].SelectedIndex = 5;
                    for (int i = 1; i < comboEVs.Length; i++)
                    {
                        comboEVs[i].SelectedIndex = Math.Min(Math.Max(0, (int)((-ev_sequence[i] + 2.5) * 2)), comboEVs[i].Items.Count - 1);
                        comboEVs[i].Visible = (i < trigImageCount);
                        comboEVs[i].Enabled = enable;
                    }
                }
                catch (Exception) {
                }
            }
            else
            {
                chkEnableSB.Enabled = false;
            }

            int mode, value;
            err = m_camera.GetWDR(out mode, out value);
            if (err == IPCamError.OK)
            {
                chkWDR.Checked = mode == 1;
                try
                {
                    trackWDR.Value = value;
                    labelWDR.Text = value.ToString();
                }
                catch (Exception) { }
            }
            else
            {
                chkWDR.Enabled = false;
                trackWDR.Enabled = false;
            }

            m_tooltip.SetToolTip(chkEnableSB, "For smart bracket mode to take effect, Auto Exposure must be enabled and One-Shot trigger mode must be selected. Also, Auto Gain settings are ignored.");
            m_tooltip.SetToolTip(comboFrameCount, "The number of images to be captured for each trigger.");
            m_tooltip.SetToolTip(comboEV2, "Relative intensity difference against target value. 1 EV corresponds to twice of the target and -1 EV corresponds to half value.");
        }

        private void SetupiNova2GUI()
        {
            int osd;
            if (m_camera.GetOSD(out osd) == IPCamError.OK)
            {
                comboOSD.SelectedIndex = osd;
            }

            int filter;
            if (m_camera.IsInova2_Compact())
                chkFilter.Enabled = false;
            else if (m_camera.GetFilterSwitch(out filter) == IPCamError.OK)
            {
                chkFilter.Checked = filter == 1;
            }

            int mono;
            if (m_camera.GetMonochrome(out mono) == IPCamError.OK)
            {
                chkMono.Checked = mono == 1;
            }

            if (m_camera.IsInova2_Standard() || m_camera.IsInova2_Zoom())
            {
                int format;
                if (m_camera.GetVideoFormat(out format) == IPCamError.OK)
                    chkEnableUncompressed.Checked = format == 1;
            }
            else
                chkEnableUncompressed.Enabled = false;

            if (m_camera.IsVersion05())
            {
                this.comboGamma.Items.Clear();
                this.comboGamma.Items.AddRange(new object[] {
                "Off",
                "0.45",
                "0.55",
                "0.65",
                "0.75"});

                double gamma;
                if (m_camera.GetGamma(out gamma) == IPCamError.OK)
                {
                    int val = 2;
                    if (0 <= gamma && gamma < 0.5f) val = 1; // 0.45
                    else if (0.5f <= gamma && gamma < 0.6f) val = 2; // 0.55
                    else if (0.6f <= gamma && gamma < 0.7f) val = 3; // 0.65
                    else if (0.7f <= gamma && gamma < 0.8f) val = 4; // 0.75
                    else if (1.0f <= gamma && gamma < 1.5f) val = 0; // 1.0 which means gamma is disabled

                    // This part is only for old firmware earlier than 0.5.1
                    else if (2.0f <= gamma && gamma < 2.5f) val = 2; // set index to i-Nova 2's default setting
                    else { }

                    comboGamma.SelectedIndex = val;
                }
            }
            else
            {
                this.comboGamma.Items.Clear();
                this.comboGamma.Items.AddRange(new object[] {
                "Off",
                "0.45",
                "0.5",
                "0.55",
                "0.6",
                "0.65",
                "0.7",
                "0.75"});

                double gamma;
                if (m_camera.GetGamma(out gamma) == IPCamError.OK)
                {
                    int val = 2;
                    if (0 <= gamma && gamma < 0.48f) val = 1; // 0.45
                    else if (gamma < 0.53f) val = 2; // 0.5
                    else if (gamma < 0.58f) val = 3; // 0.55
                    else if (gamma < 0.63f) val = 4; // 0.6
                    else if (gamma < 0.68f) val = 5; // 0.65
                    else if (gamma < 0.73f) val = 6; // 0.7
                    else if (gamma < 0.78f) val = 7; // 0.75
                    else if (1.0f <= gamma && gamma < 1.5f) val = 0; // 1.0 which means gamma is disabled

                    comboGamma.SelectedIndex = val;
                }
            }

            AutoWhiteBalance awb;
            if (m_camera.GetAWB2(out awb) == IPCamError.OK)
            {
                comboWB.SelectedIndex = awb.modeAWB;
                comboCTemp.SelectedIndex = awb.colorTemp;

                trackColorRGain.Value = awb.colorRGain;
                trackColorGGain.Value = awb.colorGGain;
                trackColorBGain.Value = awb.colorBGain;

                track2RGain.Value = awb.RGain;
                track2BGain.Value = awb.BGain;
            }

            if (!m_camera.IsInova2_Standard())
                btnStartIrisCalib.Enabled = false;

            // ONSemi
            int index;
            if (m_camera.GetDebayerCompGain(out index) == IPCamError.OK)
            {
                trackDebayerCmpGain.Visible = true;
                label49.Visible = true;
                textDCgain.Visible = true;
                trackDebayerCmpGain.Value = index;
                textDCgain.Text = index.ToString();
            }

            m_tooltip.SetToolTip(chkFilter, "Toggles filter switcher open/close status.");
            m_tooltip.SetToolTip(chkEnableUncompressed, "Enables uncompressed YUV stream instead of JPEG. This consumes more network bandwidth with less frame rate.");
            m_tooltip.SetToolTip(comboGamma, "Sets gamma correction.");
            m_tooltip.SetToolTip(comboOSD, "Selects the type of on screen display.");
            m_tooltip.SetToolTip(btnStartIrisCalib, "Starts DC iris calibration. You need to do this when the DC iris has been newly attached or changed.");
        }

        private void SetupZoomGUI()
        {
            LoadZoomConfigFile();

            int zoom, focus;
            m_camera.GetZoomFocusPosition(out zoom, out focus);
            numTargetZoom.Value = zoom; numTargetFocus.Value = focus;

            int iris;
            if (m_camera.GetIris(out iris) == IPCamError.OK)
            {
                trackIris.Value = iris;
                lblIris.Text = iris.ToString();
            }
        }

        private void SetupTriggerFlashGUI()
        {
            try
            {

                int trigMode, minActivePulse, minInactivePulse;
                bool isActiveHi;

                if (m_camera.GetTriggerMode(out trigMode, out isActiveHi, out minActivePulse, out minInactivePulse) == IPCamError.OK)
                {
                    switch (trigMode)
                    {
                        case 0: radioFreeRun.Checked = true; break;
                        case 1: radioTriggerOneShot.Checked = true; break;
                        case 2: radioTriggerMixed.Checked = true; break;
                        case 3: radioTriggerPseudo.Checked = true; break;
                    }
                    btnSWTrigger.Enabled = (comboTrigSrc2.SelectedIndex != 0) && !radioFreeRun.Checked;
                    comboTrigSrc2.Enabled = !radioFreeRun.Checked;

                    radioActiveHi.Checked = isActiveHi;
                    radioActiveLow.Checked = !isActiveHi;
                    if (minActivePulse == -1)
                        txtMinTrig.Enabled = false;
                    else
                        txtMinTrig.Text = (minActivePulse / 1000).ToString();

                    if (minInactivePulse == -1)
                        txtMinTrigInactive.Enabled = false;
                    else
                        txtMinTrigInactive.Text = (minInactivePulse / 1000).ToString();
                }

                if (m_camera.IsInova2())
                {
                    radioTriggerMixed.Enabled = false;
                    int trigSource = 0;
                    IPCamError res = m_camera.GetTriggerSource2(out trigSource);
                    if (res == IPCamError.OK)
                    {
                        chkSWTrigger.Visible = false;
                        this.comboTrigSrc2.Items.AddRange(new object[] {
                            "HW",
                            "SW",
                            "HW+SW"
                        });
                        comboTrigSrc2.SelectedIndex = trigSource;
                        btnSWTrigger.Enabled = (trigSource > 0) && (!radioFreeRun.Checked);
                    }
                    else // for old firmwares ( <= 0.9.13 )
                    {
                        chkSWTrigger.Enabled = false;
                        comboTrigSrc2.Visible = false;
                        lblTrigSrc2.Visible = false;
                    }
                }
                else
                {
                    lblTrigSrc2.Visible = false;
                    comboTrigSrc2.Visible = false;
                    bool trigSource = false;
                    if (m_camera.GetTriggerSource(out trigSource) == IPCamError.OK)
                    {
                        chkSWTrigger.Checked = trigSource;
                        btnSWTrigger.Enabled = trigSource;
                    }
                }

                int count;
                if (m_camera.GetTriggerImageCount(out count) == IPCamError.OK)
                {
                    textTrigImgNum.Text = count.ToString();
                }

                int flashMode;
                if (m_camera.GetFlash(out flashMode, out isActiveHi) == IPCamError.OK)
                {
                    switch (flashMode)
                    {
                        case 0:
                            radioflashoff.Checked = true;
                            break;
                        case 1:
                            radioflashint.Checked = true;
                            break;
                        case 2:
                            radioFlashAuto.Checked = true;
                            break;
                    }
                    radioflashhigh.Checked = isActiveHi;
                    radioflashlow.Checked = !isActiveHi;
                }

                int flashDelay;
                if (m_camera.GetFlashOnDelay(out flashDelay) == IPCamError.OK)
                {
                    txtFlashOnDelay.Text = flashDelay.ToString();
                }

                bool gpio;
                if (m_camera.GetGPIO(out gpio) == IPCamError.OK)
                {
                    comboGPIO.SelectedIndex = gpio ? 0 : 1;
                }

                if (m_camera.IsInova2())
                {
                    int out1, out2;
                    if (m_camera.GetOutputPort(out out1, out out2) == IPCamError.OK)
                    {
                        comboOut1.SelectedIndex = out1;
                        comboOut2.SelectedIndex = out2;
                        comboOut1.Enabled = true;
                        comboOut2.Enabled = true;
                    }

                    // Trigger Debouncing - capable models
                    if (!(m_camera.IsInova2_Compact() || m_camera.IsInova2_Motor()))
                    {
                        txtMinTrig.Enabled = false;
                        txtMinTrigInactive.Enabled = false;
                    }

                    txtFlashOffDelay.Enabled = false;

                    // Old firmware?
                    int a, b; 
                    bool c, d;
                    if (m_camera.GetAutoFlash(out a, out b, out c, out d) == IPCamError.CommandNotFound)
                    {
                        radioFlashAuto.Enabled = false;
                        btnAutoFlashConf.Enabled = false;
                    }
                }
                else
                {
                    radioFlashAuto.Enabled = false;
                    btnAutoFlashConf.Enabled = false;
                }

                m_tooltip.SetToolTip(radioFreeRun, "Streaming images with the specified frame rate");
                m_tooltip.SetToolTip(radioTriggerOneShot, "Grab one or more images for each rising or falling edge of trigger input");
                m_tooltip.SetToolTip(radioTriggerMixed, "Grab images while the trigger signal is active (high or low)");
                m_tooltip.SetToolTip(radioTriggerPseudo, "Grab one or more images for each trigger, while the image sensor is running at the specified frame rate. \nThe timing of trigger and image is not synchronized.");
                m_tooltip.SetToolTip(radioActiveHi, "Detect the rising edge for one-shot trigger, or high level for mixed trigger mode.");
                m_tooltip.SetToolTip(radioActiveLow, "Detect the falling edge for one-shot trigger, or low level for mixed trigger mode.");
                m_tooltip.SetToolTip(chkSWTrigger, "Enable software triggering. Hardware triggering through GPIO port is disabled.");
                m_tooltip.SetToolTip(btnSWTrigger, "Fire software trigger.");
                m_tooltip.SetToolTip(textTrigImgNum, "The number of images to grab for one-shot and pseudo-trigger mdoes.");
                m_tooltip.SetToolTip(txtMinTrig, "The trigger pulses with active duration shorter than this will be ignored. Useful for removing noises when the signal is inactive.");
                m_tooltip.SetToolTip(txtMinTrigInactive, "The minimum inactive duration required to recognize the next trigger. Useful for removing noises when the signal is active.");
                m_tooltip.SetToolTip(txtFlashOnDelay, "The delay of flash's response to signal in microseconds. When set with positive value, the flash output precedes the exposure by this amount.");
            }
            catch (Exception) { }
        }

        private void SetupGeneralGUI()
        {
            try
            {
                if(m_camera.IsInova2_Motor_ONSemi())
                {
                    trackGain.Maximum = 160;
                }

                double tgain;
                if (m_camera.GetTotalGain(out tgain) == IPCamError.OK)
                {
                    double decibel = Math.Log10(tgain) * 20;
                    int gv = (int)(decibel * 10);
                    if (gv <= trackGain.Maximum && gv >= trackGain.Minimum)
                        trackGain.Value = gv;
                    textGain.Text = Convert.ToString(decibel);
                }

                if (!m_camera.IsInova2())
                {
                    int again;
                    if (m_camera.GetAnalogGain(out again) == IPCamError.OK)
                    {
                        trackAGain.Value = again;
                    }

                    double dgain;
                    if (m_camera.GetDigitalGain(out dgain) == IPCamError.OK)
                    {
                        trackDGain.Value = (int)((dgain - 1) * 20);
                    }

                    double rgain, bgain;
                    if (m_camera.GetWhiteBalance(out bgain, out rgain) == IPCamError.OK)
                    {
                        trackBGain.Value = (int)(bgain * 10);
                        trackRGain.Value = (int)(rgain * 10);

                        int mode;
                        if (m_camera.GetAWB(out mode) == IPCamError.OK)
                        {
                            chkEnableAWB.Checked = mode == 1;
                        }
                        else
                        {
                            // Camera doesn't support this. (1.2.1 or older)
                            chkEnableAWB.Enabled = false;
                            btnOneShotAWB.Enabled = false;
                        }
                    }
                    else
                    {
                        // Disable color controls for mono model.
                        trackBGain.Enabled = false;
                        trackRGain.Enabled = false;
                        chkEnableAWB.Enabled = false;
                        btnOneShotAWB.Enabled = false;
                    }
                }
                else
                {
                    chkEnableAWB.Enabled = false;
                    btnOneShotAWB.Enabled = false;
                }

                int exposure;
                if (m_camera.GetExposure(out exposure) == IPCamError.OK)
                    textExposure.Text = exposure.ToString();

                double fps;
                if (m_camera.GetFrameRate(out fps) == IPCamError.OK)
                    textFrameRate.Text = fps.ToString();

                if (m_camera.GetALC(out m_alc) == IPCamError.OK)
                {
                    chkAEC.Checked = m_alc.enableAEC;
                    checkAGC.Checked = m_alc.enableAGC;
                    chkEnableAIC.Checked = m_alc.enableAIC;
                    trackAECTarget.Value = m_alc.target;
                    lblTarget.Text = m_alc.target.ToString();
                    textAECRangeMin.Text = m_alc.minExposure.ToString();
                    textAECRangeMax.Text = m_alc.maxExposure.ToString();
                    textAGCRangeMin.Text = m_alc.minGain.ToString();
                    textAGCRangeMax.Text = m_alc.maxGain.ToString();
                }
                else // disable ALC GUI if not supported.
                {
                    chkAEC.Enabled = false;
                    checkAGC.Enabled = false;
                    chkEnableAIC.Enabled = false;
                    trackAECTarget.Enabled = false;
                    textAECRangeMin.Enabled = false;
                    textAECRangeMax.Enabled = false;
                    textAGCRangeMin.Enabled = false;
                    textAGCRangeMax.Enabled = false;
                }

                int x, y, width, height;
                if (m_camera.GetALCArea(out x, out y, out width, out height) == IPCamError.OK)
                {
                    textAreaX.Text = x.ToString();
                    textAreaY.Text = y.ToString();
                    textAreaWidth.Text = width.ToString();
                    textAreaHeight.Text = height.ToString();
                }
                else
                {
                    textAreaX.Enabled = false;
                    textAreaY.Enabled = false;
                    textAreaWidth.Enabled = false;
                    textAreaHeight.Enabled = false;
                }

                if (m_camera.IsInova2())
                {
                    trackAGain.Enabled = false;
                    trackBGain.Enabled = false;
                    trackRGain.Enabled = false;
                    trackDGain.Enabled = false;
                    trackBlackLevel.Enabled = false;
                    textFrameRate.Enabled = false;
                    chkEnableAIC.Enabled = m_camera.IsInova2_Standard();
                }

                m_tooltip.SetToolTip(chkAEC, "Enable Auto Exposure Control");
                m_tooltip.SetToolTip(checkAGC, "Enable Auto Gain Control");
                m_tooltip.SetToolTip(textAECRangeMin, "Minimum exposure in microseconds");
                m_tooltip.SetToolTip(textAECRangeMax, "Maximum exposure in microseconds");
                m_tooltip.SetToolTip(textAGCRangeMin, "Minimum gain value in multiplication factor (i-Nova1) or in dB (i-Nova2)");
                m_tooltip.SetToolTip(textAGCRangeMax, "Maximum gain value in multiplication factor (i-Nova1) or in dB (i-Nova2)");
                m_tooltip.SetToolTip(chkEnableAWB, "Enable Auto White Balance (continuous)");
                m_tooltip.SetToolTip(btnOneShotAWB, "Execute one-shot auto white balance");
                m_tooltip.SetToolTip(trackAECTarget, "Target intensity value for ALC");
                m_tooltip.SetToolTip(chkEnableAIC, "Enable Auto Iris Control. This requires DC-Iris lens attached.");

                m_tooltip.SetToolTip(btnSaveSetting, "Saves the settings in the camera. The settings will be maintained after power cycling.");
                m_tooltip.SetToolTip(btnResetCamera, "Reboot the camera. All unsaved settings will be lost.");
                m_tooltip.SetToolTip(btnRestoreSetting, "Restore all the settings to the factory-default. The current settings will be deleted.");

            }
            catch (Exception) { }
        }

        private void SetupBracketGUI()
        {
            try
            {
                string[] BrkNoList = { "1", "2", "3", "4" };
                cmbBracketNo.Items.Clear();
                cmbBracketNo.Items.AddRange(BrkNoList);

                bool isBrkMode;
                int brkNumber;
                if (m_camera.GetBracketMode(out isBrkMode, out brkNumber) == IPCamError.OK)
                {
                    if (m_camera.IsInova2())
                    {
                        if (m_camera.GetModel() == Model.iN2M_23OC)
                        {
                            label17.Text = "Gain";
                            cmbBracketNo.Items.Remove("1");
                            cmbBracketNo.Items.Remove("2");
                        }
                        else {
                            label17.Text = "Gain";
                            cmbBracketNo.Items.Remove("1");
                            cmbBracketNo.Items.Remove("3");
                            cmbBracketNo.SelectedIndex = 1;
                            cmbBracketNo.Enabled = false;
                        }
                    }

                    SetBracketCount(brkNumber);
                    chkBracket.Checked = isBrkMode;

                    for (int ch = 0; ch < 4; ch++)
                    {
                        int exp, again;
                        double dgain;
                        if (m_camera.IsInova2())
                        {
                            if (m_camera.GetBracketInfo2(ch, out exp, out again) == IPCamError.OK)
                            {
                                m_exposures[ch].Text = exp.ToString();
                                try
                                {
                                    m_aGains[ch].Enabled = false;
                                    if (m_camera.GetModel() != Model.iN2M_23OC) {
                                        if ((radioTriggerOneShot.Checked) && (ch != 0)) {
                                            // if using One Shot Trigger mode in i-Nova2 except Onsemi model,
                                            // all gains are unified with gain of channel 1.
                                            m_dGains[ch].Enabled = false;
                                        }
                                    }
                                    double correctionValue = m_camera.IsInova2_Motor_ONSemi() ? 1.6 : 4.8;

                                    m_dGains[ch].Value = (int)(again / correctionValue); // [0, 160] or [0, 480] => [0, 100]
                                    switch (ch)
                                    {
                                        case 0: brackDGain1.Text = ((double)again / 10).ToString(); break;
                                        case 1: brackDGain2.Text = ((double)again / 10).ToString(); break;
                                        case 2: brackDGain3.Text = ((double)again / 10).ToString(); break;
                                        case 3: brackDGain4.Text = ((double)again / 10).ToString(); break;
                                        default: break;
                                    }
                                }
                                catch (ArgumentException)
                                {
                                }
                            }
                        }
                        else
                        {
                            if (m_camera.GetBracketInfo(ch, out exp, out again, out dgain) == IPCamError.OK)
                            {
                                m_exposures[ch].Text = exp.ToString();
                                try
                                {
                                    m_aGains[ch].Value = again;
                                    m_dGains[ch].Value = (int)((dgain - 1) * 20);
                                }
                                catch (ArgumentException)
                                {
                                }
                            }
                        }
                    }
                }
                else
                {
                    cmbBracketNo.Enabled = false;
                    chkBracket.Enabled = false;
                }
                m_tooltip.SetToolTip(chkBracket, "Enable bracket mode. Up to 4 combinations of exposure and gain setting will be cyclically applied for each frame.");
            }
            catch (Exception) { }
        }

        private void SetupCodecGUI()
        {
            try
            {
                int h264Quality;
                if (m_camera.GetH264Quality(out h264Quality) == IPCamError.OK)
                    trackH264Qual.Value = trackH264Qual.Maximum + trackH264Qual.Minimum - h264Quality;

                int jpegQuality;
                if (m_camera.GetJPEGQuality(out jpegQuality) == IPCamError.OK)
                {
                    if (m_camera.IsInova2())
                    {
                        trackJPEGQual.Maximum = 99;
                        trackJPEGQual.Minimum = 5;
                    }
                    trackJPEGQual.Value = trackJPEGQual.Maximum + trackJPEGQual.Minimum - jpegQuality;
                    lblJPEGQual.Text = jpegQuality.ToString();
                }

                bool jpegCBREnabled;
                double jpegCBR;
                if (m_camera.GetJPEGCBR(out jpegCBREnabled, out jpegCBR) == IPCamError.OK)
                {
                    chkEnableJPEGCBR.Checked = jpegCBREnabled;
                    // Moved "trackCBR.Value = (int)(jpegCBR * 10)" up to properly show JPEGCBR in FW ver being 0.9.6 and up
                    trackCBR.Value = (int)(jpegCBR * 10);
                    if (jpegCBREnabled)
                    {
                        trackJPEGQual.Enabled = false;
                        trackCBR.Enabled = true;
                        //trackCBR.Value = (int)(jpegCBR * 10);
                        textBitrate.Text = jpegCBR.ToString();
                    }
                    else
                    {
                        trackJPEGQual.Enabled = true;
                        trackCBR.Enabled = false;
                        textBitrate.Text = jpegCBR.ToString();
                    }
                }

                if (m_camera.IsInova2())
                {
                    trackH264Qual.Enabled = false;
                }

                m_tooltip.SetToolTip(chkEnableJPEGCBR, "Enable constant bit rate streaming for JPEG");
            }
            catch (Exception) { }
        }

        private void SetBracketCount(int count)
        {
            if (m_camera.IsInova2()) {
                if (m_camera.GetModel() == Model.iN2M_23OC)
                    cmbBracketNo.SelectedIndex = count - 3;
            }
            else
            {
                if (count < 1 || count > 4) return;
                cmbBracketNo.SelectedIndex = count - 1;
            }

            for (int i = 0; i < 4; i++)
            {
                m_exposures[i].Enabled = i < count;
                m_aGains[i].Enabled = i < count && !m_camera.IsInova2();
                m_dGains[i].Enabled = i < count;
            }
        }

        private void btnWriteSensor_Click(object sender, EventArgs e)
        {
            try
            {
                int addr = Convert.ToInt32(textAddrSensor.Text, 16);
                int val = Convert.ToInt32(textValSensor.Text, 16);
                m_camera.WriteSensorRegister(addr, val);
            }
            catch (Exception) { MessageBox.Show("Bad number format"); }
        }

        private void btnWriteISP_Click(object sender, EventArgs e)
        {
            try{
                int addr = Convert.ToInt32(textAddrISP.Text, 16);
                int val = Convert.ToInt32(textValISP.Text, 16);
                m_camera.WriteISPRegister(addr, val);
            }
            catch (Exception) { MessageBox.Show("Bad number format"); }
        }

        private void btnReadSensor_Click(object sender, EventArgs e)
        {
            try
            {
                int addr = Convert.ToInt32(textAddrSensor.Text, 16);
                int value;
                if (m_camera.ReadSensorRegister(addr, out value) == IPCamError.OK)
                {
                    textValSensor.Text = value.ToString("X");
                }
            }
            catch (Exception) { MessageBox.Show("Bad number format"); }
        }

        private void btnReadISP_Click(object sender, EventArgs e)
        {
            try
            {
                int addr = Convert.ToInt32(textAddrISP.Text, 16);
                int value;
                if (m_camera.ReadISPRegister(addr, out value) == IPCamError.OK)
                {
                    textValISP.Text = value.ToString("X");
                }
            }
            catch (Exception) { MessageBox.Show("Bad number format"); }
        }

        private void trackAGain_Scroll(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            m_camera.SetAnalogGain(trackAGain.Value);
            UpdateGains();
        }

        private void trackDGain_Scroll(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            double gain = (double)trackDGain.Value / 20 + 1;
            m_camera.SetDigitalGain(gain);
            UpdateGains();
        }

        private void trackGain_Scroll(object sender, EventArgs e)
        {
            if (!m_loaded) return; 
            double decibel = (double)trackGain.Value / 10; // Base DB
            double multiplier = Math.Pow(10, decibel / 20);
            m_camera.SetTotalGain(multiplier);
            if (m_camera.IsInova2())
                textGain.Text = Convert.ToString(decibel);
            else
                UpdateGains();

        }

        private void UpdateGains()
        {
            m_loaded = false;

            double tgain;
            if (m_camera.GetTotalGain(out tgain) == IPCamError.OK)
            {
                double decibel = Math.Log10(tgain) * 20;
                int gv = (int)(decibel * 10);
                if (gv <= trackGain.Maximum && gv >= trackGain.Minimum)
                    trackGain.Value = gv;
                textGain.Text = Convert.ToString(decibel);
            }
            try
            {
                int again;
                if (m_camera.GetAnalogGain(out again) == IPCamError.OK)
                {
                    trackAGain.Value = again;
                }

                double dgain;
                if (m_camera.GetDigitalGain(out dgain) == IPCamError.OK)
                {
                    trackDGain.Value = (int)((dgain - 1) * 20);
                }
            }
            catch (Exception) { }

            m_loaded = true;
        }

        private void radioActiveHi_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;

            SetTriggerMode();
        }

        private void radioFreeRun_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            if (radioFreeRun.Checked)
            {
                SetTriggerMode();
            }
        }

        private void radioTriggerOneShot_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            if (radioTriggerOneShot.Checked)
            {
                if (m_camera.IsInova2()) {
                    if (m_camera.GetModel() != Model.iN2M_23OC) {
                        // if using One Shot Trigger mode in i-Nova2,
                        // all gains are unified with gain of channel 1.
                        for (int ch = 1; ch < 4; ch++)
                            m_dGains[ch].Enabled = false;
                    }
                }
                else
                    m_camera.SetTriggerSource(chkSWTrigger.Checked);

                SetTriggerMode();
            }
            else
            {
                if (m_camera.IsInova2()) {
                    if (m_camera.GetModel() != Model.iN2M_23OC) {
                        for (int ch = 1; ch < 4; ch++)
                            m_dGains[ch].Enabled = true;
                    }
                }
            }
        }

        private void radioTriggerMixed_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            if (radioTriggerMixed.Checked)
            {
                if (!m_camera.IsInova2())
                    m_camera.SetTriggerSource(chkSWTrigger.Checked);
                SetTriggerMode();
            }
        }
        private void radioTriggerPseudo_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return; 
            if (radioTriggerPseudo.Checked)
            {
                if (!m_camera.IsInova2())
                    m_camera.SetTriggerSource(chkSWTrigger.Checked);
                SetTriggerMode();
            }
        }
        private void trackBlackLevel_Scroll(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            m_camera.SetBlackLevel(trackBlackLevel.Value);
        }

        private void textTrigImgNum_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                try
                {
                    int num = Convert.ToInt32(textTrigImgNum.Text);
                    if (m_camera.SetTriggerImageCount(num) != IPCamError.OK)
                    {
                        MessageBox.Show("Error (bracket is on or too big count?)");
                    }
                }
                catch (FormatException)
                {
                    MessageBox.Show("Bad number format");
                }
            }
        }

        private void txtMinTrig_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                SetTriggerMode();
            }
        }

        private void radioFlash_Changed(object sender, EventArgs e)
        {
            if (!m_loaded) return;

            RadioButton btn = sender as RadioButton;
            if (btn.Checked == false)
                return;

            int mode = 0;
            if (radioflashint.Checked)
                mode = 1;
            else if (radioFlashAuto.Checked)
                mode = 2;
            m_camera.SetFlash(mode, radioflashhigh.Checked);            
        }

        private void txtFlashOnDelay_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                try
                {
                    int num = Convert.ToInt32(txtFlashOnDelay.Text);
                    m_camera.SetFlashOnDelay(num);
                }
                catch (FormatException)
                {
                    MessageBox.Show("Bad number format");
                }
            }

        }

        private void txtFlashOffDelay_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                try
                {
                    int num = Convert.ToInt32(txtFlashOffDelay.Text);
                    m_camera.SetFlashOffDelay(num);
                }
                catch (FormatException)
                {
                    MessageBox.Show("Bad number format");
                }
            }
        }

        private void btnSWTrigger_Click(object sender, EventArgs e)
        {
            if (radioFreeRun.Checked != true)
                m_camera.SetForcedTrigger();
            else
            {
                MessageBox.Show("Not on Trigger Mode!!!");
            }
        }

        private void trackH264Qual_Scroll(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            int h264q = trackH264Qual.Maximum + trackH264Qual.Minimum - trackH264Qual.Value;
            m_camera.SetH264Quality(h264q);
        }

        private void btnResetCamera_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Camera will restart and the unsaved settings will be lost. Also, you need to reconnect the camera. Are you sure to proceed?",
                                "Restart Camera",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning)
                    == DialogResult.Yes)
            {
                m_camera.ResetCamera();
            }
        }

        private void btnSaveSetting_Click(object sender, EventArgs e)
        {
            m_camera.SaveSetting();
        }

        private void btnRestoreSetting_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("This will restore the default settings on camera and the camera will restart. Are you sure to proceed?",
                                "Restore Camera Setting",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning)
                                        == DialogResult.Yes)
            {
                if (m_camera.RestoreDefaultSetting() == IPCamError.CommandNotFound)
                {
                    MessageBox.Show("Sorry, the firmware doesn't support this function. Please update the camera firmware.");
                }
            }
        }

        private void chkSWTrigger_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;

            m_camera.SetTriggerSource(chkSWTrigger.Checked);
            btnSWTrigger.Enabled = chkSWTrigger.Checked;
        }

        private void bracketGUI_ValueChanged(object sender, EventArgs e)
        {
            if(!m_loaded) return;

            for (int ch = 0; ch < 4; ch++)
            {
                if (m_dGains[ch] == sender || m_aGains[ch] == sender || m_exposures[ch] == sender)
                {
                    int exp = 0;
                    try
                    {
                        exp = Convert.ToInt32(m_exposures[ch].Text);
                    }
                    catch (FormatException) { }

                    if (m_camera.IsInova2())
                    {
                        double correctionValue = m_camera.IsInova2_Motor_ONSemi() ? 1.6 : 4.8;

                        int gain = (int)(m_dGains[ch].Value * correctionValue);
                        m_camera.SetBracketInfo2(ch, exp, gain);
                        double dGain = gain / 10.0;
                        switch (ch)
                        {
                            case 0: brackDGain1.Text = dGain.ToString(); break;
                            case 1: brackDGain2.Text = dGain.ToString(); break;
                            case 2: brackDGain3.Text = dGain.ToString(); break;
                            case 3: brackDGain4.Text = dGain.ToString(); break;
                        }
                    }
                    else
                    {
                        int again = m_aGains[ch].Value;
                        double dgain = (double)m_dGains[ch].Value / 20 + 1;
                        m_camera.SetBracketInfo(ch, exp, again, dgain);
                    }
                    break;
                }
            }
        }

        private void chkBracket_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;

            int brkNo = cmbBracketNo.SelectedIndex + 1;
            if (m_camera.IsInova2())
                brkNo = cmbBracketNo.SelectedIndex + 3;
            m_camera.SetBracketMode(chkBracket.Checked, brkNo);
        }

        private void cmbBracketNo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;

            int brkNo = cmbBracketNo.SelectedIndex + 1;

            if (m_camera.IsInova2())
                brkNo = cmbBracketNo.SelectedIndex + 3;

            m_camera.SetBracketMode(chkBracket.Checked, brkNo);
            SetBracketCount(brkNo);
        }

        private void trackBGain_Scroll(object sender, EventArgs e)
        {
            if (!m_loaded) return; 
            m_camera.SetWhiteBalance((double)trackBGain.Value / 10, (double)trackRGain.Value / 10);
        }

        private void trackRGain_Scroll(object sender, EventArgs e)
        {
            if (!m_loaded) return; 
            m_camera.SetWhiteBalance((double)trackBGain.Value / 10, (double)trackRGain.Value / 10);
        }

        private void trackJPEGQual_Scroll(object sender, EventArgs e)
        {
            if (!m_loaded) return; 
            int jpegq = trackJPEGQual.Maximum + trackJPEGQual.Minimum - trackJPEGQual.Value;
            lblJPEGQual.Text = jpegq.ToString();
            m_camera.SetJPEGQuality(jpegq);
        }

        private void chkEnableJPEGCBR_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return; 
            if (chkEnableJPEGCBR.Checked)
            {
                trackCBR.Enabled = true;
                textBitrate.Enabled = true;
                trackJPEGQual.Enabled = false;

                double mbps = (double)trackCBR.Value / 10.0;
                m_camera.SetJPEGCBR(true, mbps);
                textBitrate.Text = mbps.ToString();
            }
            else
            {
                trackCBR.Enabled = false;
                textBitrate.Enabled = false;
                trackJPEGQual.Enabled = true;

                m_camera.SetJPEGCBR(false, 0);
            }
        }

        private void trackCBR_Scroll(object sender, EventArgs e)
        {
            if (!m_loaded) return; 
            chkEnableJPEGCBR_CheckedChanged(sender, e);
        }

        private void trackH264Qual_Scroll_1(object sender, EventArgs e)
        {
            if (!m_loaded) return; 
            int h264q = trackH264Qual.Maximum + trackH264Qual.Minimum - trackH264Qual.Value;
            m_camera.SetH264Quality(h264q);
        }

        private void textExposure_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                try
                {
                    int exposure = Convert.ToInt32(textExposure.Text);
                    m_camera.SetExposure(exposure);
                }
                catch (Exception)
                {
                }
            }
        }

        private void textFrameRate_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                try
                {
                    double fps = Convert.ToDouble(textFrameRate.Text);
                    m_camera.SetFrameRate(fps);
                }
                catch (Exception)
                {
                }
            }
        }

        private void chkAEC_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            m_alc.enableAEC = chkAEC.Checked;
            m_alc.target = trackAECTarget.Value;
            UpdateALCRange();

            m_camera.SetALC(m_alc);
        }

        private void checkAGC_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            m_alc.enableAGC = checkAGC.Checked;
            m_alc.target = trackAECTarget.Value;
            UpdateALCRange();

            m_camera.SetALC(m_alc);
        }

        private void trackAECTarget_Scroll(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            //if (chkAEC.Checked || checkAGC.Checked) // do we need this? at least we need to check AIC status, too.
            {
                m_alc.target = trackAECTarget.Value;
                UpdateALCRange();
                m_camera.SetALC(m_alc);
                lblTarget.Text = m_alc.target.ToString();
            }
        }

        private void SetTriggerMode()
        {
            if (!m_loaded) return;

            try
            {
                int trigMode = radioFreeRun.Checked ? 0 : (radioTriggerOneShot.Checked ? 1 : radioTriggerMixed.Checked ? 2 : 3);
                btnSWTrigger.Enabled = !radioFreeRun.Checked;
                if (comboTrigSrc2.Visible) {
                    btnSWTrigger.Enabled = btnSWTrigger.Enabled && (comboTrigSrc2.SelectedIndex != 0);
                    comboTrigSrc2.Enabled = !radioFreeRun.Checked;
                }
                int minPulseActive = Convert.ToInt32(txtMinTrig.Text) * 1000;
                int minPulseInactive = Convert.ToInt32(txtMinTrigInactive.Text) * 1000;
                m_camera.SetTriggerMode(trigMode, radioActiveHi.Checked, minPulseActive, minPulseInactive);
            }
            catch (FormatException)
            {
                MessageBox.Show("Bad number format");
            }
        }

        private void UpdateALCRange()
        {
            try
            {
                m_alc.minExposure = Convert.ToInt32(textAECRangeMin.Text);
                m_alc.maxExposure = Convert.ToInt32(textAECRangeMax.Text);
                m_alc.minGain = Convert.ToDouble(textAGCRangeMin.Text);
                m_alc.maxGain = Convert.ToDouble(textAGCRangeMax.Text);
            }
            catch (Exception)
            {
            }

        }

        private void textAECRange_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                UpdateALCRange();
                m_camera.SetALC(m_alc);
            }
        }

        private void textAGCRange_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                UpdateALCRange();
                m_camera.SetALC(m_alc);
            }
        }

        private void chkEnableAWB_CheckedChanged(object sender, EventArgs e)
        {
            m_camera.SetAWB(chkEnableAWB.Checked ? 1 : 0);
        }

        private void btnOneShotAWB_Click(object sender, EventArgs e)
        {
            m_camera.SetAWB(2);
        }

        public event EventHandler<AreaChangedEventArgs> AreaChanged;

        private void textArea_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                try
                {
                    int x = Convert.ToInt32(textAreaX.Text);
                    int y = Convert.ToInt32(textAreaY.Text);
                    int width = Convert.ToInt32(textAreaWidth.Text);
                    int height = Convert.ToInt32(textAreaHeight.Text);

                    m_camera.SetALCArea(x, y, width, height);

                    AreaChanged?.Invoke(this, new AreaChangedEventArgs(x, y, width, height));
                }
                catch (Exception)
                {
                }
            }
        }

        private void chkFilter_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return; 

            if (chkFilter.Checked)
                m_camera.SetFilterSwitch(1);
            else
                m_camera.SetFilterSwitch(0);
        }

        private void chkMono_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;

            if (chkMono.Checked)
                m_camera.SetMonochrome(1);
            else
                m_camera.SetMonochrome(0);

        }

        private void comboOSD_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return; 
            m_camera.SetOSD(comboOSD.SelectedIndex);
        }

        private void comboGamma_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            if (m_camera.IsVersion05())
            {
                switch (comboGamma.SelectedIndex)
                {
                    case 0: m_camera.SetGamma(1.0f); break;
                    case 1: m_camera.SetGamma(0.45f); break;
                    case 2: m_camera.SetGamma(0.55f); break;
                    case 3: m_camera.SetGamma(0.65f); break;
                    case 4: m_camera.SetGamma(0.75f); break;
                }
            }
            else
            {
                switch (comboGamma.SelectedIndex)
                {
                    case 0: m_camera.SetGamma(1.0f); break;
                    case 1: m_camera.SetGamma(0.45f); break;
                    case 2: m_camera.SetGamma(0.5f); break;
                    case 3: m_camera.SetGamma(0.55f); break;
                    case 4: m_camera.SetGamma(0.6f); break;
                    case 5: m_camera.SetGamma(0.65f); break;
                    case 6: m_camera.SetGamma(0.7f); break;
                    case 7: m_camera.SetGamma(0.75f); break;
                }
            }
        }

        private void btnStartIrisCalib_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure to run the iris calibration? This will reset the current iris settings. Also, the camera will need to have motionless images with adequate lighting to achieve good calibration result. The calibration procedure will take about two minutes.",
                                    "Iris Calibration",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Warning)
                                            == DialogResult.Yes)
            {
                m_camera.SetIris(1);
            }
        }

        private void chkEnableUncompressed_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return; 

            int format = chkEnableUncompressed.Checked ? 1 : 0;
            m_camera.SetVideoFormat(format);
        }

        private void chkEnableAIC_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            m_alc.enableAIC = chkEnableAIC.Checked;
            UpdateALCRange();

            m_camera.SetALC(m_alc);
        }

        private void UpdateAWB2(object sender, EventArgs e)
        {
            if (!m_loaded) return;

            try
            {
                AutoWhiteBalance awb = new AutoWhiteBalance();

                awb.modeAWB = comboWB.SelectedIndex;
                awb.colorRGain = int.Parse(txtColorRGain.Text);
                awb.colorGGain = int.Parse(txtColorGGain.Text);
                awb.colorBGain = int.Parse(txtColorBGain.Text);

                awb.colorTemp = comboCTemp.SelectedIndex;
                awb.RGain = int.Parse(txtRGain.Text);
                awb.BGain = int.Parse(txtBGain.Text);

                m_camera.SetAWB2(awb);
            }
            catch (Exception) { }

        }

        private void btnResetZoom_Click(object sender, EventArgs e)
        {
            m_camera.ReadjustZoom();
        }

        private void btnSetTarget_Click(object sender, EventArgs e)
        {
            try
            {
                int zoom = (int)numTargetZoom.Value;
                int focus = (int)numTargetFocus.Value;
                m_camera.SetZoomFocusPosition(zoom, focus);
            }
            catch (Exception) { }
        }

        private void btnGetPos_Click(object sender, EventArgs e)
        {
            int zoom, focus;
            if (m_camera.GetZoomFocusPosition(out zoom, out focus) == IPCamError.OK)
            {
                numTargetFocus.Value = focus;
                numTargetZoom.Value = zoom;
            }
        }

        private void btnPreset1_Click(object sender, EventArgs e)
        {
            RunPresetPosition(0);
        }

        private void btnPreset2_Click(object sender, EventArgs e)
        {
            RunPresetPosition(1);
        }

        private void btnPreset3_Click(object sender, EventArgs e)
        {
            RunPresetPosition(2);
        }

        private void btnPreset4_Click(object sender, EventArgs e)
        {
            RunPresetPosition(3);
        }

        private void btnPreset5_Click(object sender, EventArgs e)
        {
            RunPresetPosition(4);
        }

        private void reset_button_background_color()
        {
            btnPreset1.BackColor = Color.MintCream;
            btnPreset2.BackColor = Color.MintCream;
            btnPreset3.BackColor = Color.MintCream;
            btnPreset4.BackColor = Color.MintCream;
            btnPreset5.BackColor = Color.MintCream;
        }

        const int PRESET_NUM = 5;
        int []m_zoom;
        int []m_focus;

        private void RunPresetPosition(int phase)
        {
            m_camera.SetZoomFocusPosition(m_zoom[phase], m_focus[phase]);

            numTargetFocus.Value = m_focus[phase];
            numTargetZoom.Value = m_zoom[phase];

            reset_button_background_color();

            switch (phase)
            {
                case 0: btnPreset1.BackColor = Color.Gold; break;
                case 1: btnPreset2.BackColor = Color.Gold; break;
                case 2: btnPreset3.BackColor = Color.Gold; break;
                case 3: btnPreset4.BackColor = Color.Gold; break;
                case 4: btnPreset5.BackColor = Color.Gold; break;
            }
        }

        void LoadZoomConfigFile()
        {
            var exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            var configPath = System.IO.Path.GetDirectoryName(exePath) + "\\zoom_" + m_serial + ".conf";

            try
            {
                var config = System.IO.File.ReadAllLines(configPath);
                m_zoom = new int[PRESET_NUM];
                m_focus = new int[PRESET_NUM];
                for (int i = 0; i < PRESET_NUM; i++)
                {
                    var values = config[i].Split(',');
                    m_zoom[i] = Convert.ToInt32(values[0]);
                    m_focus[i] = Convert.ToInt32(values[1]);
                }
            }
            catch (Exception) 
            {
                // Failed to load or parse the config file.
                // Assign default values.
                m_zoom = new int[PRESET_NUM] { -10000, -6000, -2000, 1000, 3500,};
                m_focus = new int[PRESET_NUM] { -2055, -755, 875, 1735, -6000};
            }
        }

        void SaveZoomConfigFile()
        {
            var exePath = System.Reflection.Assembly.GetEntryAssembly().Location;
            var configPath = System.IO.Path.GetDirectoryName(exePath) + "\\zoom_" + m_serial + ".conf";

            string []config = new string[PRESET_NUM];
            for (int i = 0; i < PRESET_NUM; i++)
                config[i] = string.Format("{0},{1}", m_zoom[i], m_focus[i]);

            System.IO.File.WriteAllLines(configPath, config);
        }

        private void SetPresetZoomPosition(int num)
        {
            int zoom, focus;
            if (m_camera.GetZoomFocusPosition(out zoom, out focus) == IPCamError.OK)
            {
                numTargetFocus.Value = focus;
                numTargetZoom.Value = zoom;

                m_zoom[num] = zoom;
                m_focus[num] = focus;

                SaveZoomConfigFile();
            }
        }

        private void btnSet1_Click(object sender, EventArgs e)
        {
            SetPresetZoomPosition(0);
        }

        private void btnSet2_Click(object sender, EventArgs e)
        {
            SetPresetZoomPosition(1);
        }

        private void btnSet3_Click(object sender, EventArgs e)
        {
            SetPresetZoomPosition(2);
        }

        private void btnSet4_Click(object sender, EventArgs e)
        {
            SetPresetZoomPosition(3);
        }

        private void btnSet5_Click(object sender, EventArgs e)
        {
            SetPresetZoomPosition(4);
        }

        private void btnFocusMInus2_Click(object sender, EventArgs e)
        {
            int zoom, focus;
            if (m_camera.GetZoomFocusPosition(out zoom, out focus) == IPCamError.OK)
            {
                focus -= 100;
                numTargetFocus.Value = focus;
                numTargetZoom.Value = zoom;
                m_camera.SetZoomFocusPosition(zoom, focus);
            }
        }

        private void btnFocusMinus_Click(object sender, EventArgs e)
        {
            int zoom, focus;
            if (m_camera.GetZoomFocusPosition(out zoom, out focus) == IPCamError.OK)
            {
                focus -= 10;
                numTargetFocus.Value = focus;
                numTargetZoom.Value = zoom;
                m_camera.SetZoomFocusPosition(zoom, focus);
            }
        }

        private void btnFocusPlus_Click(object sender, EventArgs e)
        {
            int zoom, focus;
            if (m_camera.GetZoomFocusPosition(out zoom, out focus) == IPCamError.OK)
            {
                focus += 10;
                numTargetFocus.Value = focus;
                numTargetZoom.Value = zoom;
                m_camera.SetZoomFocusPosition(zoom, focus);
            }
        }

        private void btnFocusPlus2_Click(object sender, EventArgs e)
        {
            int zoom, focus;
            if (m_camera.GetZoomFocusPosition(out zoom, out focus) == IPCamError.OK)
            {
                focus += 100;
                numTargetFocus.Value = focus;
                numTargetZoom.Value = zoom;
                m_camera.SetZoomFocusPosition(zoom, focus);
            }
        }

        private void btnZoomMinus2_Click(object sender, EventArgs e)
        {
            int zoom, focus;
            if (m_camera.GetZoomFocusPosition(out zoom, out focus) == IPCamError.OK)
            {
                zoom -= 100;
                numTargetFocus.Value = focus;
                numTargetZoom.Value = zoom;
                m_camera.SetZoomFocusPosition(zoom, focus);
            }
        }

        private void btnZoomMinus_Click(object sender, EventArgs e)
        {
            int zoom, focus;
            if (m_camera.GetZoomFocusPosition(out zoom, out focus) == IPCamError.OK)
            {
                zoom -= 10;
                numTargetFocus.Value = focus;
                numTargetZoom.Value = zoom;
                m_camera.SetZoomFocusPosition(zoom, focus);
            }
        }

        private void btnZoomPlus_Click(object sender, EventArgs e)
        {
            int zoom, focus;
            if (m_camera.GetZoomFocusPosition(out zoom, out focus) == IPCamError.OK)
            {
                zoom += 10;
                numTargetFocus.Value = focus;
                numTargetZoom.Value = zoom;
                m_camera.SetZoomFocusPosition(zoom, focus);
            }
        }

        private void btnZoomPlus2_Click(object sender, EventArgs e)
        {
            int zoom, focus;
            if (m_camera.GetZoomFocusPosition(out zoom, out focus) == IPCamError.OK)
            {
                zoom += 100;
                numTargetFocus.Value = focus;
                numTargetZoom.Value = zoom;
                m_camera.SetZoomFocusPosition(zoom, focus);
            }
        }

        private void btnAutoFlashConf_Click(object sender, EventArgs e)
        {
            int minExp, maxExp;
            bool controlFilter, controlMono;
            if (m_camera.GetAutoFlash(out maxExp, out minExp, out controlFilter, out controlMono) != IPCamError.OK)
                return;

            var form = new AutoFlashConfigForm(m_camera);
            form.MaxExposure = maxExp;
            form.MinExposure = minExp;
            form.ControlFilter = controlFilter;
            form.ControlMono = controlMono;

            if (form.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                minExp = form.MinExposure;
                maxExp = form.MaxExposure;
                controlFilter = form.ControlFilter;
                controlMono = form.ControlMono;

                m_camera.SetAutoFlash(maxExp, minExp, controlFilter, controlMono);
            }

        }

        private void trackIris_Scroll(object sender, EventArgs e)
        {
            m_camera.SetIris(trackIris.Value);
            lblIris.Text = trackIris.Value.ToString();
        }

        private void btnGetSysInfo_Click(object sender, EventArgs e)
        {
            string info;
            if (m_camera.GetSystemInfo(out info) == IPCamError.OK)
            {
                var vals = info.Split(':');
                MessageBox.Show(string.Format("Uptime:{0} days {1} hours {2} minutes {3} seconds",
                    vals[1], vals[2], vals[3], vals[4]));
            }
        }

        private bool m_runPresetLoop = false;

        private void btnLoop_Click(object sender, EventArgs e)
        {
            if (!m_runPresetLoop)
            {
                btnLoop.Text = "Stop Loop";
                var loopThread = new Thread(LoopThread);
                loopThread.IsBackground = true;
                m_runPresetLoop = true;
                loopThread.Start();
            }
            else
            {
                btnLoop.Text = "Start Loop";
                m_runPresetLoop = false;
            }
        }

        delegate void RunPresetInvoke(int pos);

        private void LoopThread(object threadParam)
        {
            int count = 0;
            var v = new RunPresetInvoke(RunPresetPosition);

            while (m_runPresetLoop)
            {
                int phase = count % 350; // 35-second cycle, 100 ms unit counter.
                switch (phase)
                {
                    case 0: BeginInvoke(v, new object[] { 0 }); break;
                    case 70: BeginInvoke(v, new object[] { 1 }); break;
                    case 140: BeginInvoke(v, new object[] { 2 }); break;
                    case 210: BeginInvoke(v, new object[] { 3 }); break;
                    case 280: BeginInvoke(v, new object[] { 4 }); break;
                }
                count++;
                Thread.Sleep(100);
            }
        }

        private void comboOut1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            m_camera.SetOutputPort(1, comboOut1.SelectedIndex);
        }

        private void comboOut2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            m_camera.SetOutputPort(2, comboOut2.SelectedIndex);
        }

        private void comboGPIO_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            m_camera.SetGPIO(comboGPIO.SelectedIndex == 0);
        }

        private void chkWDR_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            if (chkWDR.Checked)
            {
                m_camera.SetWDR(1, trackWDR.Value);
            }
            else
            {
                m_camera.SetWDR(0, 0);
            }
        }

        private void SmartBracketSettingChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;

            comboFrameCount.Enabled = chkEnableSB.Checked;

            for (int i = 1; i < comboEVs.Length; i++) // Frame 1 must always be EV 0.
            {
                comboEVs[i].Visible = i < comboFrameCount.SelectedIndex + 3;
                comboEVs[i].Enabled = chkEnableSB.Checked;
            }

            int numEv = comboFrameCount.SelectedIndex + 3;
            double[] ev_seq = new double[numEv];

            for (int i = 0; i < numEv; i++)
            {
                ev_seq[i] = -comboEVs[i].SelectedIndex * 0.5 + 2.5;
            }

            m_camera.SetSmartBracket(chkEnableSB.Checked, ev_seq);
        }

        private void trackWDR_ValueChanged(object sender, EventArgs e)
        {
            labelWDR.Text = trackWDR.Value.ToString();
            chkWDR_CheckedChanged(null, null);
        }

        private void trackColorRGain_ValueChanged(object sender, EventArgs e)
        {
            txtColorRGain.Text = trackColorRGain.Value.ToString();
            UpdateAWB2(sender, e);
        }

        private void trackColorGGain_ValueChanged(object sender, EventArgs e)
        {
            txtColorGGain.Text = trackColorGGain.Value.ToString();
            UpdateAWB2(sender, e);
        }

        private void trackColorBGain_ValueChanged(object sender, EventArgs e)
        {
            txtColorBGain.Text = trackColorBGain.Value.ToString();
            UpdateAWB2(sender, e);
        }

        private void track2RGain_ValueChanged(object sender, EventArgs e)
        {
            txtRGain.Text = track2RGain.Value.ToString();
            UpdateAWB2(sender, e);
        }

        private void track2BGain_ValueChanged(object sender, EventArgs e)
        {
            txtBGain.Text = track2BGain.Value.ToString();
            UpdateAWB2(sender, e);
        }

        private void trackDebayerCmpGain_Scroll(object sender, EventArgs e)
        {
            textDCgain.Text = trackDebayerCmpGain.Value.ToString();
            m_camera.SetDebayerCompGain(trackDebayerCmpGain.Value);
        }

        private void comboWB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboWB.SelectedIndex != 3)
            {
                track2RGain.Enabled = false;
                track2BGain.Enabled = false;
                comboCTemp.Enabled = false;
            }
            else
            {
                track2RGain.Enabled = true;
                track2BGain.Enabled = true;
                comboCTemp.Enabled = true;
            }
            UpdateAWB2(sender, e);
        }

        private void comboCTemp_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateAWB2(sender, e);
        }

        private void btnFocus1_Click(object sender, EventArgs e)
        {
            m_camera.SetZoomFocusPosition(0, +10);
        }

        private void btnFocus2_Click(object sender, EventArgs e)
        {
            m_camera.SetZoomFocusPosition(0, +1);
        }

        private void btnFocus3_Click(object sender, EventArgs e)
        {
            m_camera.SetZoomFocusPosition(0, -1);
        }

        private void btnFocus4_Click(object sender, EventArgs e)
        {
            m_camera.SetZoomFocusPosition(0, -10);
        }

        private void btnZoom1_Click(object sender, EventArgs e)
        {
            m_camera.SetZoomFocusPosition(-20, 0);
        }

        private void btnZoom2_Click(object sender, EventArgs e)
        {
            m_camera.SetZoomFocusPosition(-2, 0);
        }

        private void btnZoom3_Click(object sender, EventArgs e)
        {
            m_camera.SetZoomFocusPosition(+2, 0);
        }

        private void btnZoom4_Click(object sender, EventArgs e)
        {
            m_camera.SetZoomFocusPosition(+20, 0);
        }

        private void btnOnsemi_Click(object sender, EventArgs e)
        {
            try
            {
                var r_gain = Convert.ToDouble(txtRedGain.Text);
                var g_gain = Convert.ToDouble(txtGreenGain.Text);
                var b_gain = Convert.ToDouble(txtBlueGain.Text);

                m_camera.SetRGBGain(r_gain, g_gain, b_gain);
            }
            catch (Exception ex) { }
        }

        private void Form_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Escape) this.Close();
        }

        private void btnIrisAbsPos_Click(object sender, EventArgs e)
        {
            var btnName = (sender as Button).Name;
            int pos = Convert.ToInt32(btnName.Substring(4));
            m_camera.SetIrisAbs(pos);
        }

        private void comboTrigSrc2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;

            int trigSrc = comboTrigSrc2.SelectedIndex;

            btnSWTrigger.Enabled = (trigSrc != 0) && (!radioFreeRun.Checked);
            m_camera.SetTriggerSource2(trigSrc);
        }

    }

    public class AreaChangedEventArgs : System.EventArgs
    {
        public readonly int x, y, width, height;

        public AreaChangedEventArgs(int _x, int _y, int _width, int _height)
        {
            x = _x;
            y = _y;
            width = _width;
            height = _height;
        }
    }
}