using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
//using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;

namespace KyungsinLPR
{
    public partial class frmAdvFeature : Form
    {
        private IPCamera m_camera;
        private bool m_loaded = false;

        private ALC m_alc = new ALC();
        TextBox[] m_exposures = new TextBox[4];
        System.Windows.Forms.TrackBar []m_aGains = new System.Windows.Forms.TrackBar[4];
        System.Windows.Forms.TrackBar []m_dGains = new System.Windows.Forms.TrackBar[4];

        public frmAdvFeature(IPCamera camera)
        {
            InitializeComponent();

            m_camera = camera;
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

            m_loaded = true;
        }

        private void SetupTriggerFlashGUI()
        {
            int trigMode, count;
            bool isActiveHi;

            if (m_camera.GetTriggerMode(out trigMode, out isActiveHi))
            {
                switch (trigMode)
                {
                    case 0: 
                        radioFreeRun.Checked = true;
                        btnSWTrigger.Enabled = false;
                        break;
                    case 1: radioTriggerOneShot.Checked = true; break;
                    case 2: radioTriggerMixed.Checked = true; break;
                    case 3: radioTriggerPseudo.Checked = true; break;
                }
                radioActiveHi.Checked = isActiveHi;
                radioActiveLow.Checked = !isActiveHi;
            }

            bool trigSource = false;
            if (m_camera.GetTriggerSource(out trigSource))
            {
                chkSWTrigger.Checked = trigSource;
            }

            if (m_camera.GetTriggerImageCount(out count))
            {
                textTrigImgNum.Text = count.ToString();
            }

            int flashMode;
            if (m_camera.GetFlash(out flashMode, out isActiveHi))
            {
                switch (flashMode)
                {
                    case 0:
                        radioflashoff.Checked = true;
                        break;
                    case 1:
                        radioflashint.Checked = true;
                        break;
                }
                radioflashhigh.Checked = isActiveHi;
                radioflashlow.Checked = !isActiveHi;
            }
        }

        private void SetupGeneralGUI()
        {
            try
            {
                double tgain;
                if (m_camera.GetTotalGain(out tgain))
                {
                    double decibel = Math.Log10(tgain) * 20;
                    trackGain.Value = (int)(decibel * 10);
                    textGain.Text = Convert.ToString(decibel);
                }

                int again;
                if (m_camera.GetAnalogGain(out again))
                {
                    trackAGain.Value = again;
                }

                double dgain;
                if (m_camera.GetDigitalGain(out dgain))
                {
                    trackDGain.Value = (int)((dgain - 1) * 20);
                }

                double rgain, bgain;
                if (m_camera.GetWhiteBalance(out bgain, out rgain))
                {
                    trackBGain.Value = (int)(bgain * 10);
                    trackRGain.Value = (int)(rgain * 10);
                }
                else
                {
                    trackBGain.Enabled = false;
                    trackRGain.Enabled = false;
                }

                int exposure;
                if (m_camera.GetExposure(out exposure))
                    textExposure.Text = exposure.ToString();

                double fps;
                if (m_camera.GetFrameRate(out fps))
                    textFrameRate.Text = fps.ToString();

                if (m_camera.GetALC(out m_alc))
                {
                    chkAEC.Checked = m_alc.enableAEC;
                    checkAGC.Checked = m_alc.enableAGC;
                    trackAECTarget.Value = m_alc.target;
                    textAECRangeMin.Text = m_alc.minExposure.ToString();
                    textAECRangeMax.Text = m_alc.maxExposure.ToString();
                    textAGCRangeMin.Text = m_alc.minGain.ToString();
                    textAGCRangeMax.Text = m_alc.maxGain.ToString();
                }
                else // disable ALC GUI if not supported.
                {
                    chkAEC.Enabled = false;
                    checkAGC.Enabled = false;
                    trackAECTarget.Enabled = false;
                    textAECRangeMin.Enabled = false;
                    textAECRangeMax.Enabled = false;
                    textAGCRangeMin.Enabled = false;
                    textAGCRangeMax.Enabled = false;
                }

            }
            catch (Exception) { }
        }

        private void SetupBracketGUI()
        {
            string[] BrkNoList = { "1", "2", "3", "4" };
            cmbBracketNo.Items.Clear();
            cmbBracketNo.Items.AddRange(BrkNoList);

            bool isBrkMode;
            int brkNumber;
            if (m_camera.GetBracketMode(out isBrkMode, out brkNumber))
            {
                SetBracketCount(brkNumber);
                chkBracket.Checked = isBrkMode;

                for (int ch = 0; ch < 4; ch++)
                {
                    int exp, again;
                    double dgain;
                    if (m_camera.GetBracketInfo(ch, out exp, out again, out dgain))
                    {
                        m_exposures[ch].Text = exp.ToString();
                        try
                        {
                            m_aGains[ch].Value = again;
                            m_dGains[ch].Value = (int)((dgain - 1) * 20);
                        }
                        catch (ArgumentException) { 
                        }
                    }
                }
            }
            else
            {
                cmbBracketNo.Enabled = false;
                chkBracket.Enabled = false;
            }
        }

        private void SetupCodecGUI()
        {
            try
            {
                int h264Quality;
                if (m_camera.GetH264Quality(out h264Quality))
                    trackH264Qual.Value = trackH264Qual.Maximum + trackH264Qual.Minimum - h264Quality;

                int jpegQuality;
                if (m_camera.GetJPEGQuality(out jpegQuality))
                    trackJPEGQual.Value = trackJPEGQual.Maximum + trackJPEGQual.Minimum - jpegQuality;

                bool jpegCBREnabled;
                double jpegCBR;
                if (m_camera.GetJPEGCBR(out jpegCBREnabled, out jpegCBR))
                {
                    chkEnableJPEGCBR.Checked = jpegCBREnabled;
                    if (jpegCBREnabled)
                    {
                        trackJPEGQual.Enabled = false;
                        trackCBR.Enabled = true;
                        trackCBR.Value = (int)(jpegCBR * 10);
                        textBitrate.Text = jpegCBR.ToString();
                    }
                    else
                    {
                        trackJPEGQual.Enabled = true;
                        trackCBR.Enabled = false;
                    }
                }
            }
            catch (Exception) { }
        }

        private void SetBracketCount(int count)
        {
            if (count < 1 || count > 4) return;

            cmbBracketNo.SelectedIndex = count - 1;

            for (int i = 0; i < 4; i++)
            {
                m_exposures[i].Enabled = i < count;
                m_aGains[i].Enabled = i < count;
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
                if (m_camera.ReadSensorRegister(addr, out value))
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
                if (m_camera.ReadISPRegister(addr, out value))
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
            UpdateGains();
        }

        private void UpdateGains()
        {
            m_loaded = false;

            double tgain;
            if (m_camera.GetTotalGain(out tgain))
            {
                double decibel = Math.Log10(tgain) * 20;
                trackGain.Value = (int)(decibel * 10);
                textGain.Text = Convert.ToString(decibel);
            }
            try
            {
                int again;
                if (m_camera.GetAnalogGain(out again))
                {
                    trackAGain.Value = again;
                }

                double dgain;
                if (m_camera.GetDigitalGain(out dgain))
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

            int trigMode = radioFreeRun.Checked ? 0 : (radioTriggerOneShot.Checked ? 1 : radioTriggerMixed.Checked ? 2 : 3);
            m_camera.SetTriggerMode(trigMode, radioActiveHi.Checked);
        }

        private void radioFreeRun_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            if (radioFreeRun.Checked)
            {
                m_camera.SetTriggerMode(0, false);
                btnSWTrigger.Enabled = false;
            }
        }

        private void radioTriggerOneShot_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            if (radioTriggerOneShot.Checked)
            {
                m_camera.SetTriggerSource(chkSWTrigger.Checked);
                m_camera.SetTriggerMode(1, radioActiveHi.Checked);
                btnSWTrigger.Enabled = true;
            }
        }

        private void radioTriggerMixed_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            if (radioTriggerMixed.Checked)
            {
                m_camera.SetTriggerSource(chkSWTrigger.Checked);
                m_camera.SetTriggerMode(2, radioActiveHi.Checked);
                btnSWTrigger.Enabled = true;
            }
        }
        private void radioTriggerPseudo_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return; 
            if (radioTriggerPseudo.Checked)
            {
                m_camera.SetTriggerSource(chkSWTrigger.Checked);
                m_camera.SetTriggerMode(3, radioActiveHi.Checked);
                btnSWTrigger.Enabled = true;
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
                    if (!m_camera.SetTriggerImageCount(num))
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

        private void checkGPIO_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            m_camera.SetGPIO(checkGPIO.Checked);
        }

        private void radioflashhigh_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            if (radioflashint.Checked)
            {
                m_camera.SetFlash(1, radioflashhigh.Checked);
            }
        }

        private void radioflashoff_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            if (radioflashoff.Checked)
            {
                m_camera.SetFlash(0, radioflashhigh.Checked);
            }
        }

        private void radioflashint_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            if (radioflashint.Checked)
            {
                m_camera.SetFlash(1, radioflashhigh.Checked);
            }
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
            Application.Exit();
            Application.ExitThread();
        }

        private void btnRestoreSetting_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("This will restore the default settings on camera and the camera will restart. Are you sure to proceed?",
                                "Restore Camera Setting",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning)
                    == DialogResult.Yes)
            {
                if (!m_camera.RestoreDefaultSetting())
                {
                    MessageBox.Show("Sorry, the firmware doesn't support this function. Please update the camera firmware.");
                }
            }
        }

        private void chkSWTrigger_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            m_camera.SetTriggerSource(chkSWTrigger.Checked);            
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
                    int again = m_aGains[ch].Value;
                    double dgain = (double)m_dGains[ch].Value / 20 + 1;
                    bool ret = m_camera.SetBracketInfo(ch, exp, again, dgain);
                    break;
                }
            }
        }
        
        private void chkBracket_CheckedChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;
            if (chkBracket.Checked == true)
            {
                int brkNo = cmbBracketNo.SelectedIndex + 1;
                m_camera.SetBracketMode(true, brkNo);
            }
            else
            {
                int brkNo = cmbBracketNo.SelectedIndex + 1;
                m_camera.SetBracketMode(false, brkNo);
            }
        }

        private void cmbBracketNo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!m_loaded) return;

            int brkNo = cmbBracketNo.SelectedIndex + 1;
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
            if (chkAEC.Checked || checkAGC.Checked)
            {
                m_alc.target = trackAECTarget.Value;
                UpdateALCRange();
                m_camera.SetALC(m_alc);
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

        private void textAECRangeMin_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                UpdateALCRange();
                m_camera.SetALC(m_alc);
            }
        }

        private void textAGCRangeMin_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                UpdateALCRange();
                m_camera.SetALC(m_alc);
            }
        }

    }
}
