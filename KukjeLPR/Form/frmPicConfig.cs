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
using KyungsinLPR;

namespace KyungsinLPR
{
    public partial class frmPicConfig : Form
    {
        ClsStructure.EnvStruct Env;
        public string path = string.Empty;
        public Rectangle PlateRect;
        public Rectangle RoiRect;
        private int Camidx = 0;
        ClsStructure.IPCamera_Basic_Setting IPCamInfo;

        int startX = 0;
        int startY = 0;
        int endX = 0;
        int endY = 0;
        string roi = "";

        string IPcamstr = string.Empty;

        private delegate void SizeConfigControl(frmPicConfig frm, PictureBox pb, Bitmap bmp);
        int cal = 0;

        DateTime Downtime;

        //leess 사이즈 동적 변경
        int imgWidth = 0, imgHeight = 0;//불러온 캡쳐이미지 사이즈

        public frmPicConfig(ClsStructure.EnvStruct _env, int _CamIdx)
        {
            InitializeComponent();
            Env = _env;
            Camidx = _CamIdx;
            
            if (Camidx.Equals(1))
            {
                IPCamInfo = Env.CameraEnv.IPCamera1Info;
                path = frmLprMain.Main.Path1;
            }
            else
            {
                IPCamInfo = Env.CameraEnv.IPCamera2Info;
                path = frmLprMain.Main.Path2;
            }
        }

        private void frmPicConfig_Load(object sender, EventArgs e)
        {
            Bitmap bmp;
            Bitmap outbmp;
            RoiRect = IPCamInfo.Roi;
            //plate area
            if (File.Exists(path))
            {
                Bitmap Platebmp = new Bitmap(path);
                Image file = Image.FromFile(path);
                string result = string.Empty;
                foreach (var fitem in file.PropertyItems)
                {
                    if (fitem.Id.Equals(0x9286))
                    {
                        Console.WriteLine(fitem.Id);
                        result = Encoding.UTF8.GetString(fitem.Value).Replace("\0", string.Empty);
                        string[] sp = result.Split(' ');
                        int length = 0;
                        int.TryParse(sp[0], out length);
                        result = Util.Common.Mid(result, 3, length);
                    }
                }

                if (!result.Equals(String.Empty))
                {
                    String[] sp = result.Split(' ');
                    String[] sp1 = sp[0].Split(',');
                    int rtX = 0;
                    int rtY = 0;
                    int rtWidth = 0;
                    int rtHeith = 0;
                    int.TryParse(sp1[0], out rtX);
                    int.TryParse(sp1[1], out rtY);
                    int.TryParse(sp1[2], out rtWidth);
                    int.TryParse(sp1[3], out rtHeith);
                    PlateRect = new Rectangle(rtX, rtY, rtWidth, rtHeith);

                    Console.WriteLine(string.Format("{0} {1} {2} {3}", PlateRect.X, PlateRect.Y, PlateRect.Width, PlateRect.Height));
                    Util.Logger.Log(string.Format("{0} {1} {2} {3}", sp1[0], sp1[1], sp1[2], sp1[3]));
                    Util.Logger.Log(string.Format("{0} {1} {2} {3}", PlateRect.X, PlateRect.Y, PlateRect.Width, PlateRect.Height));
                }
                bmp = new Bitmap(path);
                //leess 사이즈 동적 변경
                //outbmp = clsFunction.ResizeImage(bmp, 800, 600);
                imgWidth = bmp.Width;
                imgHeight = bmp.Height;
                outbmp = clsFunction.ResizeImage(bmp, bmp.Width/2, bmp.Height/2);
                pictureBox1.Image = outbmp;
                sizecontrol(this, pictureBox1, outbmp);
            }
            else
            {
                MessageBox.Show("마지막 촬영된 이미지 정보가 없습니다!");
            }
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
            pictureBox1.Width = bmp.Width;
            pictureBox1.Height = bmp.Height;
            pictureBox1.Location = new Point(5, 60);

            this.Width = pictureBox1.Width + 30;
            this.Height = pictureBox1.Height + pictureBox1.Top + 50;
        }

        private void btnSaveClose_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.OK;
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
                
                if (!IPCamInfo.Use)
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

                RoiRect.X = startX * 2;
                RoiRect.Y = startY * 2;
                RoiRect.Width = (endX - startX) * 2;
                RoiRect.Height = (endY - startY) * 2;
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
            roi = string.Format("0, 0, {0}, {1}", 1600, 1200);

            RoiRect.X = 0;
            RoiRect.Y = 0;
            //leess 사이즈 동적 변경
            //RoiRect.Width = 1600;
            //RoiRect.Height = 1200;
            if(imgWidth > 0 && imgHeight > 0) {//저장된 이미지 있다면
                RoiRect.Width = imgWidth;
                RoiRect.Height = imgHeight;
            } else {//없다면 1, 2에따라 달리 처리
                if(Env.CameraEnv.iNovaType == 1) {
                    RoiRect.Width = 1600;
                    RoiRect.Height = 1200;
                } else {
                    RoiRect.Width = 1920;
                    RoiRect.Height = 1080;
                }
            }
        }

        private void frmPicConfig_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                this.pictureBox1.ImageLocation = "";
                //File.Delete(path);
            }
            catch (Exception)
            {
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            label4.Text = String.Format("Mouse Position X: {0}; Y: {1}", e.X, e.Y);
            using (Graphics g = pictureBox1.CreateGraphics())
            {
                g.DrawRectangle(Pens.Red, RoiRect.X / 2, RoiRect.Y / 2, RoiRect.Width / 2, RoiRect.Height / 2);
                g.DrawRectangle(Pens.Blue, PlateRect.X / 2, PlateRect.Y / 2, PlateRect.Width / 2, PlateRect.Height / 2);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.ExitThread();
            Application.Exit();
        }
    }
}
