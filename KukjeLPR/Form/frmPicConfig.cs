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
        // 실제 적용된 표시 스케일 (원본 대비). 마우스 좌표 → 원본 좌표 환산에 사용.
        // USB 5MP 등 고해상도 카메라 지원을 위해 화면 작업영역 fit으로 동적 결정.
        private double _displayScale = 0.5;

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

        // 서버캠(개별설정)용: 지정 이미지/현재 ROI 로 영역설정 (cam1/2 Path 의존 없음)
        private Rectangle _explicitRoi;
        public frmPicConfig(ClsStructure.EnvStruct _env, string _imagePath, Rectangle _roi)
        {
            InitializeComponent();
            Env = _env;
            Camidx = -1;            // 서버캠 모드
            path = _imagePath ?? string.Empty;
            _explicitRoi = _roi;
        }

        private void frmPicConfig_Load(object sender, EventArgs e)
        {
            Bitmap bmp;
            Bitmap outbmp;
            RoiRect = (Camidx == -1) ? _explicitRoi : IPCamInfo.Roi;   // 서버캠은 명시 ROI
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
                imgWidth = bmp.Width;
                imgHeight = bmp.Height;

                // 화면 작업영역에 맞도록 표시 스케일 동적 계산 (기본 0.5, 큰 카메라는 더 축소)
                var workArea = Screen.FromControl(this).WorkingArea;
                int maxW = workArea.Width - 60;
                int maxH = workArea.Height - 140; // 폼 헤더/버튼 영역 확보
                _displayScale = 0.5;
                if (bmp.Width * _displayScale > maxW || bmp.Height * _displayScale > maxH)
                {
                    double sw = (double)maxW / bmp.Width;
                    double sh = (double)maxH / bmp.Height;
                    _displayScale = Math.Min(sw, sh);
                }
                int newW = Math.Max(1, (int)(bmp.Width * _displayScale));
                int newH = Math.Max(1, (int)(bmp.Height * _displayScale));
                outbmp = clsFunction.ResizeImage(bmp, newW, newH);
                pictureBox1.Image = outbmp;
                sizecontrol(this, pictureBox1, outbmp);
                this.Text = string.Format("사진설정(설정시 마우스 오른쪽클릭)  원본 {0}x{1}, 표시 {2}x{3} (×{4:F2})",
                    bmp.Width, bmp.Height, newW, newH, _displayScale);
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

            // 화면 작업영역 초과 방지 — 그래도 넘으면 화면 fit
            var wa = Screen.FromControl(this).WorkingArea;
            if (this.Width > wa.Width) this.Width = wa.Width;
            if (this.Height > wa.Height) this.Height = wa.Height;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(wa.X + Math.Max(0, (wa.Width - this.Width) / 2),
                                       wa.Y + Math.Max(0, (wa.Height - this.Height) / 2));
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

                // 표시 좌표 → 원본 픽셀 좌표 환산 (스케일 역수 적용)
                double inv = (_displayScale > 0) ? 1.0 / _displayScale : 2.0;
                RoiRect.X = (int)(startX * inv);
                RoiRect.Y = (int)(startY * inv);
                RoiRect.Width = (int)((endX - startX) * inv);
                RoiRect.Height = (int)((endY - startY) * inv);
                roi = string.Format("{0},{1},{2},{3}", RoiRect.X, RoiRect.Y, RoiRect.Width, RoiRect.Height);
                this.Text = string.Format("사진설정 ROI(원본) {0}", roi);
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
                // 원본 좌표(RoiRect) → 표시 좌표 환산 시 저장 시 사용한 _displayScale 적용.
                // 기존 /2 고정 코드는 USB 5MP(2592x1944) 등 _displayScale≠0.5 카메라에서 어긋남.
                double s = (_displayScale > 0) ? _displayScale : 0.5;
                g.DrawRectangle(Pens.Red,
                    (int)(RoiRect.X * s), (int)(RoiRect.Y * s),
                    (int)(RoiRect.Width * s), (int)(RoiRect.Height * s));
                g.DrawRectangle(Pens.Blue,
                    (int)(PlateRect.X * s), (int)(PlateRect.Y * s),
                    (int)(PlateRect.Width * s), (int)(PlateRect.Height * s));
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Application.ExitThread();
            Application.Exit();
        }
    }
}
