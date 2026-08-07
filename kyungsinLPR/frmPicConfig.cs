using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using novitec;

namespace LPRCamera_Novitec_Toshiba
{
    public partial class frmPicConfig : Form
    {
        string path = string.Empty;
        int cam = 0;
        string FnameHeader = string.Empty;
        int startX = 0;
        int startY = 0;
        int endX = 0;
        int endY = 0;
        string ch = string.Empty;
        string roi = "";
        Rect PlateRect;

        private Function.IPCam IPCamInfo;
        
        private Function.Exposure CamExp;
        string IPcamstr = string.Empty;

        private delegate void SizeConfigControl(frmPicConfig frm, PictureBox pb, Bitmap bmp);
        int cal = 0;

        DateTime Downtime; 

        public frmPicConfig(string imgpath, int cam)
        {
            InitializeComponent();
            path = imgpath;
            this.cam = cam;
            
            if (cam.Equals(1))
            {
                IPCamInfo = Function.Env_Info.IpCam1_Info;
                CamExp = Function.Env_Info.Public_Info.Cam1Exposure;
                IPcamstr = "IPCAM1";
                this.ch = Function.Env_Info.Cam_Info.Chanel1;
            }
            else
            {
                IPCamInfo = Function.Env_Info.IpCam2_Info;
                CamExp = Function.Env_Info.Public_Info.Cam2Exposure;
                IPcamstr = "IPCAM2";
                this.ch = Function.Env_Info.Cam_Info.Chanel2;
            }
        }

        private void SetPosition(Control obj1, Control obj2, bool Visble)
        {
            obj1.Location = obj2.Location;
            obj1.Visible = Visble;
            obj2.Visible = !Visble;
        }
        private void frmPicConfig_Load(object sender, EventArgs e)
        {
            IPCamInfo.Setting = true; 
            //Thread t = new Thread(new ThreadStart(imgView));
            //t.IsBackground = true;
            //t.Start();
            readBrightSetting();

            if (IPCamInfo.UseFlag)
            {
                BtnIPCamSetting.Visible = true;
                //groupBox1.Visible = false;
                //this.Height = 420;
                SetPosition(LblBracket, label5, true);
                SetPosition(CmbBracket1, txtTimeBright1, true);
                SetPosition(CmbBracket2, txtTimeBright2, true);
                SetPosition(CmbBracket3, txtTimeBright3, true);
                LblBasicBright.Visible = false;
                label6.Visible = false;
                TxtBasicBright.Visible = false;
                txtBasicInterval.Visible = false;
            }
            Bitmap bmp;
            Bitmap outbmp;
            if (this.cam.Equals(1))
            {
                if (!File.Exists(Properties.Settings.Default.Ch1File)) return;
                bmp = new Bitmap(Properties.Settings.Default.Ch1File);
                PlateRect = Function.Env_Info.IpCam1_Info.PlateRect;
            }
            else
            {
                if (!File.Exists(Properties.Settings.Default.Ch2File)) return;
                bmp = new Bitmap(Properties.Settings.Default.Ch2File);
                PlateRect = Function.Env_Info.IpCam2_Info.PlateRect;
            }
            outbmp = Function.ResizeImage(bmp, IPCamInfo.resizeX, IPCamInfo.resizeY);
            pictureBox1.Image = outbmp;
            sizecontrol(this, pictureBox1, outbmp);
        }

        private void imgView()
        {
            try
            {
                while (true)
                {
                    if (File.Exists(path))
                    {
                        pictureBox1.ImageLocation = path;
                        break;
                    }
                    Thread.Sleep(100);
                }

                Bitmap bmp = new Bitmap(path);
                sizecontrol(this, pictureBox1, bmp);
            }
            catch (Exception)
            {
            }
        }

        private void sizecontrol(frmPicConfig frm, PictureBox pb, Bitmap bmp)
        {
            if (!this.IsHandleCreated && !this.IsDisposed) return;

            if (pictureBox1.InvokeRequired)
            {
                this.BeginInvoke(new SizeConfigControl(this.sizecontrol), new object[] { this, pictureBox1, bmp });
                return;
            }

            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            //pictureBox1.Width = bmp.Width / 2;
            //pictureBox1.Height = bmp.Height / 2;
            pictureBox1.Width = bmp.Width;
            pictureBox1.Height = bmp.Height;
            pictureBox1.Location = new Point(5, 5);

            this.Width = pictureBox1.Width + 30;
            this.Height = pictureBox1.Height + 50;
            //if (IPCamInfo.UseFlag)
            //{
            //    BtnIPCamSetting.Left = pictureBox1.Left + pictureBox1.Width - BtnIPCamSetting.Width;
            //    BtnIPCamSetting.Top = pictureBox1.Top + pictureBox1.Height + 10;
            //    //this.Height += groupBox1.Height;
            //    //this.Height += 50;
            //    groupBox1.Top = pictureBox1.Height + 55;
            //    label4.Top = groupBox1.Top;
            //}
            //else
            //{
            //    groupBox1.Top = pictureBox1.Height + 55;
            //}
            //this.Location = new Point(100, 100);
        }

        private void btnSaveClose_Click(object sender, EventArgs e)
        {
            Function.IniWriteValue("CAM", "roi" + cam.ToString().Trim(), roi.Trim(), Function.INIPATH);
            if (roi.Equals(string.Empty)) return;
            string[] position = roi.Split(',');
            if (this.cam.Equals(1))
            {
                Function.Env_Info.Cam_Info.ch1img.CropX = Convert.ToInt16(position[0]);
                Function.Env_Info.Cam_Info.ch1img.CropY = Convert.ToInt16(position[1]);
                Function.Env_Info.Cam_Info.ch1img.CropWidth = Convert.ToInt16(position[2]);
                Function.Env_Info.Cam_Info.ch1img.CropHeight = Convert.ToInt16(position[3]);
            }
            else if (this.cam.Equals(2))
            {
                Function.Env_Info.Cam_Info.ch2img.CropX = Convert.ToInt16(position[0]);
                Function.Env_Info.Cam_Info.ch2img.CropY = Convert.ToInt16(position[1]);
                Function.Env_Info.Cam_Info.ch2img.CropWidth = Convert.ToInt16(position[2]);
                Function.Env_Info.Cam_Info.ch2img.CropHeight = Convert.ToInt16(position[3]);
            }
            Function.IniSave();
            this.Close();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                PictureBox pb = sender as PictureBox;
                pb.Invalidate();
                Downtime = DateTime.Now;
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                PictureBox pb = sender as PictureBox;
                pb.Invalidate();
                startX = e.X;
                startY = e.Y;
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Middle)
            {
                if (cal.Equals(0))
                    cal = e.Y;
                else
                {
                    cal = e.Y - cal;
                    MessageBox.Show(cal.ToString());
                    cal = 0;
                }
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                PictureBox pb = sender as PictureBox;
                Graphics g = pb.CreateGraphics();
                
                if (!IPCamInfo.UseFlag)
                    g.DrawRectangle(new Pen(Color.Red, 4), new Rectangle(e.X, e.Y, 150, 30));
                else
                    g.DrawRectangle(new Pen(Color.Red, 4), new Rectangle(e.X, e.Y, 150, 25));
            }

            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                PictureBox pb = sender as PictureBox;
                Graphics g = pb.CreateGraphics();

                endX = e.X;
                endY = e.Y;

                g.DrawRectangle(new Pen(Color.Green, 4), new Rectangle(startX, startY, endX - startX, endY - startY));

                //roi = (startX * 2).ToString() + "," + (startY * 2).ToString() + "," + (endX*2 - startX*2).ToString() + "," + (endY*2 - startY*2).ToString();
                roi = (startX).ToString() + "," + (startY).ToString() + "," + (endX - startX).ToString() + "," + (endY - startY).ToString();
                this.Text = "사진설정(설정시 마우스 오른쪽클릭)" + roi;
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnRestore_Click(object sender, EventArgs e)
        {
            roi = string.Format("0, 0, {0}, {1}", IPCamInfo.resizeX, IPCamInfo.resizeY);
            Function.IniWriteValue("CAM", "roi" + cam.ToString().Trim(), roi.Trim(), Function.INIPATH);
            if (this.cam.Equals(1))
            {
                Function.Env_Info.Cam_Info.ch1img.CropX = 0;
                Function.Env_Info.Cam_Info.ch1img.CropY = 0;
                Function.Env_Info.Cam_Info.ch1img.CropWidth = IPCamInfo.resizeX;
                Function.Env_Info.Cam_Info.ch1img.CropHeight = IPCamInfo.resizeY;
            }
            else if (this.cam.Equals(2))
            {
                Function.Env_Info.Cam_Info.ch2img.CropX = 0;
                Function.Env_Info.Cam_Info.ch2img.CropY = 0;
                Function.Env_Info.Cam_Info.ch2img.CropWidth = IPCamInfo.resizeX;
                Function.Env_Info.Cam_Info.ch2img.CropHeight = IPCamInfo.resizeY;
            }            
            this.Close();
        }

        private void frmPicConfig_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                this.pictureBox1.ImageLocation = "";
                IPCamInfo.Setting = false;
                //File.Delete(path);
            }
            catch (Exception)
            {
            }
        }
        #region Bright_Setting
        private void readBrightSetting()
        {
            string tmp = string.Empty;
            //기본 밝기 프로그램 기동시 카메라에 설정 하는 값
            //string Bright = Function.Env_Info.Public_Info.bright.ToString();
            TxtBasicBright.Text = CamExp.BasicExposure.ToString();

            //string BasicTime = Function.Env_Info.Public_Info.basicinterval.ToString();
            txtBasicInterval.Text = CamExp.BasicInterval.ToString();

            //시간대 1
            if (CamExp.usetime1)
            {
                ChkUseTime1.Checked = true;
            }
            MskStartTime1.Text = CamExp.starttime1;
            MskEndTime1.Text = CamExp.endtime1;
            txtTimeBright1.Text = CamExp.timeExposure1.ToString();

            //시간대 2
            if (CamExp.usetime2)
            {
                ChkUseTime2.Checked = true;
            }
            MskStartTime2.Text = CamExp.starttime2;
            MskEndTime2.Text = CamExp.endtime2;
            txtTimeBright2.Text = CamExp.timeExposure2.ToString();

            //시간대 3
            if (CamExp.usetime3)
            {
                ChkUseTime3.Checked = true;
            }
            MskStartTime3.Text = CamExp.starttime3;
            MskEndTime3.Text = CamExp.endtime3;
            txtTimeBright3.Text = CamExp.timeExposure3.ToString();

            CmbBracket1.Text = IPCamInfo.BracketMode1;
            CmbBracket2.Text = IPCamInfo.BracketMode2;
            CmbBracket3.Text = IPCamInfo.BracketMode3;
        }

        private bool SaveExposureSetting()
        {
            #region 정합성 체크
            if (MaskTimeCheck() == false)
            {
                MessageBox.Show("시간 형식이 잘못 되었습니다!!");
                return false;
            }

            #region 이전 시간 비교
            //if (timecheck(MskStartTime1) > timecheck(MskEndTime1))
            //{
            //    MessageBox.Show("시작 시간은 종료 시간보다 클수 없습니다!!");
            //    MskStartTime1.Focus();
            //    return false;
            //}

            //if (timecheck(MskStartTime2) > timecheck(MskEndTime2))
            //{
            //    MessageBox.Show("시작 시간은 종료 시간보다 클수 없습니다!!");
            //    MskStartTime2.Focus();
            //    return false;
            //}

            //if (timecheck(MskStartTime3) > timecheck(MskEndTime3))
            //{
            //    MessageBox.Show("시작 시간은 종료 시간보다 클수 없습니다!!");
            //    MskStartTime3.Focus();
            //    return false;
            //}

            //if (ChkUseTime1.Enabled && ChkUseTime2.Checked && ChkUseTime3.Checked)
            //{
            //    if (!(timecheck(MskStartTime1) < timecheck(MskStartTime2) && timecheck(MskStartTime2) < timecheck(MskStartTime3)))
            //    {
            //        MessageBox.Show("각 시간은 순서대로 증가 되어야 합니다!!");
            //        return false;
            //    }

            //    if (!(timecheck(MskEndTime1) < timecheck(MskEndTime2) && timecheck(MskEndTime2) < timecheck(MskEndTime3)))
            //    {
            //        MessageBox.Show("각 시간은 순서대로 증가 되어야 합니다!!");
            //        return false;
            //    }
            //}

            //if (ChkUseTime1.Enabled && ChkUseTime2.Checked)
            //{
            //    if (!(timecheck(MskStartTime1) < timecheck(MskStartTime2)))
            //    {
            //        MessageBox.Show("각 시간은 순서대로 증가 되어야 합니다!!");
            //        return false;
            //    }

            //    if (!(timecheck(MskEndTime1) < timecheck(MskEndTime2)))
            //    {
            //        MessageBox.Show("각 시간은 순서대로 증가 되어야 합니다!!");
            //        return false;
            //    }
            //}
            #endregion

            #region 시간 중복 체크
            int[] time = new int[1440];
            if (ChkUseTime1.Checked)
                if (!TimeDupCheck(ref time, MskStartTime1.Text, MskEndTime1.Text, 1))
                {
                    MessageBox.Show("설정 시간이 중복입니다");
                    MskStartTime1.Focus();
                }
            if (ChkUseTime2.Checked)
                if (!TimeDupCheck(ref time, MskStartTime2.Text, MskEndTime2.Text, 2))
                {
                    MessageBox.Show("설정 시간이 중복입니다");
                    MskStartTime2.Focus();
                }
            if (ChkUseTime3.Checked)
                if (!TimeDupCheck(ref time, MskStartTime3.Text, MskEndTime3.Text, 3))
                {
                    MessageBox.Show("설정 시간이 중복입니다");
                    MskStartTime3.Focus();
                }
            #endregion
            #endregion
            string Camstr = string.Empty;
            if (this.cam.Equals(2))
                Camstr = "cam2";
            Function.IniWriteValue("PUBLIC", Camstr + "bright", TxtBasicBright.Text, Function.INIPATH);
            CamExp.BasicExposure = Function.GetIntValue(TxtBasicBright.Text);
            Function.IniWriteValue("PUBLIC", Camstr + "basicInterval", txtBasicInterval.Text, Function.INIPATH);
            CamExp.BasicInterval = Function.GetIntValue(txtBasicInterval.Text);

            if (ChkUseTime1.Checked)
                Function.IniWriteValue("PUBLIC", Camstr + "usetime1", "true", Function.INIPATH);
            else
                Function.IniWriteValue("PUBLIC", Camstr + "usetime1", "false", Function.INIPATH);
            CamExp.usetime1 = ChkUseTime1.Checked;

            Function.IniWriteValue("PUBLIC", Camstr + "starttime1", MskStartTime1.Text, Function.INIPATH);
            CamExp.starttime1 = MskStartTime1.Text;
            Function.IniWriteValue("PUBLIC", Camstr + "endtime1", MskEndTime1.Text, Function.INIPATH);
            CamExp.endtime1 = MskEndTime1.Text;
            Function.IniWriteValue("PUBLIC", Camstr + "timebright1", txtTimeBright1.Text, Function.INIPATH);
            CamExp.timeExposure1 = Function.GetIntValue(txtTimeBright1.Text);
            if (CmbBracket1.SelectedIndex > -1)
            {
                Function.IniWriteValue(IPcamstr, "bracketMode1", CmbBracket1.SelectedItem.ToString(), Function.INIPATH);
                IPCamInfo.BracketMode1 = CmbBracket1.SelectedItem.ToString();
            }
            else
            {
                Function.IniWriteValue(IPcamstr, "bracketMode1", string.Empty, Function.INIPATH);
                IPCamInfo.BracketMode1 = string.Empty;
            }

            if (ChkUseTime2.Checked)
                Function.IniWriteValue("PUBLIC", Camstr + "usetime2", "true", Function.INIPATH);
            else
                Function.IniWriteValue("PUBLIC", Camstr + "usetime2", "false", Function.INIPATH);
            CamExp.usetime2 = ChkUseTime2.Checked;

            Function.IniWriteValue("PUBLIC", Camstr + "starttime2", MskStartTime2.Text, Function.INIPATH);
            CamExp.starttime2 = MskStartTime2.Text;
            Function.IniWriteValue("PUBLIC", Camstr + "endtime2", MskEndTime2.Text, Function.INIPATH);
            CamExp.endtime2 = MskEndTime2.Text;
            Function.IniWriteValue("PUBLIC", Camstr + "timebright2", txtTimeBright2.Text, Function.INIPATH);
            CamExp.timeExposure2 = Function.GetIntValue(txtTimeBright2.Text);
            if (CmbBracket2.SelectedIndex > -1)
            {
                Function.IniWriteValue(IPcamstr, "bracketMode2", CmbBracket2.SelectedItem.ToString(), Function.INIPATH);
                IPCamInfo.BracketMode2 = CmbBracket2.SelectedItem.ToString();
            }
            else
            {
                Function.IniWriteValue(IPcamstr, "bracketMode2", string.Empty, Function.INIPATH);
                IPCamInfo.BracketMode2 = string.Empty;
            }
            if (ChkUseTime3.Checked)
                Function.IniWriteValue("PUBLIC", Camstr + "usetime3", "true", Function.INIPATH);
            else
                Function.IniWriteValue("PUBLIC", Camstr + "usetime3", "false", Function.INIPATH);
            CamExp.usetime3 = ChkUseTime3.Checked;

            Function.IniWriteValue("PUBLIC", Camstr + "starttime3", MskStartTime3.Text, Function.INIPATH);
            CamExp.starttime3 = MskStartTime3.Text;
            Function.IniWriteValue("PUBLIC", Camstr + "endtime3", MskEndTime3.Text, Function.INIPATH);
            CamExp.endtime3 = MskEndTime3.Text;
            Function.IniWriteValue("PUBLIC", Camstr + "timebright3", txtTimeBright3.Text, Function.INIPATH);
            CamExp.timeExposure3 = Function.GetIntValue(txtTimeBright3.Text);
            if (CmbBracket3.SelectedIndex > -1)
            {
                Function.IniWriteValue(IPcamstr, "bracketMode3", CmbBracket3.SelectedItem.ToString(), Function.INIPATH);
                IPCamInfo.BracketMode3 = CmbBracket3.SelectedItem.ToString();
            }
            else
            {
                Function.IniWriteValue(IPcamstr, "bracketMode2", string.Empty, Function.INIPATH);
                IPCamInfo.BracketMode3 = string.Empty;
            }
            if (this.cam.Equals(1))
            {
                Function.Env_Info.Public_Info.Cam1Exposure = CamExp;
                Function.Env_Info.IpCam1_Info = IPCamInfo;
            }
            else
            {
                Function.Env_Info.Public_Info.Cam2Exposure = CamExp;
                Function.Env_Info.IpCam2_Info = IPCamInfo;
            }

            Function.Env_Info.IpCam1_Info.ExposureMode = -1;
            Function.Env_Info.IpCam2_Info.ExposureMode = -1;
            return true;
        }

        private bool TimeDupCheck(ref int[] time, string Stime, string Etime, int chk)
        {
            DateTime StartTime = timecheck(Stime);
            DateTime EndTime = timecheck(Etime);
            DateTime CalTime = timecheck("00:00");

            TimeSpan diff = EndTime - StartTime;
            if (diff.TotalMinutes < 0)
            {
                for (int i = 0; i < time.Length; i++)
                {
                    if (CalTime.AddMinutes(i) < EndTime || CalTime.AddMinutes(i) >= StartTime)
                    {
                        if (time[i].Equals(0))
                            time[i] = chk;
                        else
                            return false;
                    }
                }
            }
            else
            {
                for (int i = 0; i < time.Length; i++)
                {
                    if (CalTime.AddMinutes(i) >= StartTime && CalTime.AddMinutes(i) < EndTime)
                    {
                        if (time[i].Equals(0))
                            time[i] = chk;
                        else
                            return false;
                    }
                }
            }
            return true;
        }

        private void btnBrightSave_Click(object sender, EventArgs e)
        {
            if (SaveExposureSetting())
            {
                this.Close();
            }
        }

        private bool TimeCheck(Control msk)
        {
            try
            {
                if (msk.Text.Equals("") || msk.Text.Equals("  :"))
                    return true;

                string Etime = msk.Text;
                string[] ETime = Etime.Split(':');
                DateTime etime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Convert.ToInt16(ETime[0]), Convert.ToInt16(ETime[1]), 0);
                return true;
            }
            catch
            {
                msk.Focus();
                return false;
            }
        }

        private DateTime timecheck(Control msk)
        {
            try
            {
                string Etime = msk.Text;
                string[] ETime = Etime.Split(':');
                DateTime etime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Convert.ToInt16(ETime[0]), Convert.ToInt16(ETime[1]), 0);
                return etime;
            }
            catch
            {
                return DateTime.Now;
            }
        }

        private DateTime timecheck(string ctime)
        {
            try
            {
                string[] time = ctime.Split(':');
                DateTime etime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, Convert.ToInt16(time[0]), Convert.ToInt16(time[1]), 0);
                return etime;
            }
            catch
            {
                return DateTime.Now;
            }
        }

        private bool MaskTimeCheck()
        {
            if (TimeCheck(MskStartTime1) == false)
                return false;
            if (TimeCheck(MskStartTime2) == false)
                return false;
            if (TimeCheck(MskStartTime3) == false)
                return false;
            if (TimeCheck(MskEndTime1) == false)
                return false;
            if (TimeCheck(MskEndTime2) == false)
                return false;
            if (TimeCheck(MskEndTime3) == false)
                return false;
            return true;
        }
        #endregion

        private void BtnIPCamSetting_Click(object sender, EventArgs e)
        {
            if (IPCamInfo.UseFlag)
            {
                if (Function.m_advForm == null || !Function.m_advForm.Visible)
                {
                    Function.m_advForm = new AdvFeatureForm(this.cam);
                }
                Function.m_advForm.BringToFront();
                Function.m_advForm.Show();
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            label4.Text = String.Format("Mouse Position X: {0}; Y: {1}", e.X, e.Y);
            using (Graphics g = pictureBox1.CreateGraphics())
            {
                if (this.cam.Equals(1))
                {
                    g.DrawRectangle(Pens.Red, Function.Env_Info.Cam_Info.ch1img.CropX, Function.Env_Info.Cam_Info.ch1img.CropY,
                    Function.Env_Info.Cam_Info.ch1img.CropWidth, Function.Env_Info.Cam_Info.ch1img.CropHeight);
                    g.DrawRectangle(Pens.Blue, PlateRect.left / 2, PlateRect.top / 2, (PlateRect.right - PlateRect.left) / 2, (PlateRect.bottom - PlateRect.top) / 2);
                }
                else
                {
                    g.DrawRectangle(Pens.Red, Function.Env_Info.Cam_Info.ch2img.CropX, Function.Env_Info.Cam_Info.ch2img.CropY,
                    Function.Env_Info.Cam_Info.ch2img.CropWidth, Function.Env_Info.Cam_Info.ch2img.CropHeight);
                    g.DrawRectangle(Pens.Blue, PlateRect.left / 2, PlateRect.top / 2, (PlateRect.right - PlateRect.left) / 2, (PlateRect.bottom - PlateRect.top) / 2);
                }
            }
        }

        private void btnSelecttime_Click(object sender, EventArgs e)
        {
            groupBox1.Visible = !groupBox1.Visible;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Function.prgexit = true;
            Application.ExitThread();
            Application.Exit();
        }
    }
}
