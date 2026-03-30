using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace KyungsinLPR
{
    public partial class frmLPRComm : Form
    {
        private delegate void UpdateText(Control ctrl, string text);

        public frmLPRComm()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void frmLPRComm_Load(object sender, EventArgs e)
        {
            //ShowImageInit();
        }

        private void ShowImageInit()
        {
            Bitmap bm = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            using (Graphics g = Graphics.FromImage(bm))
            {
                using (SolidBrush myBrush = new SolidBrush(Color.BlueViolet))
                {
                    using (Font myFont = new Font("Times New Roman", 24))
                    {
                        g.DrawString("NO Image \r\n available", myFont, myBrush, 150, 120);
                        pictureBox1.Image = bm;
                        pictureBox2.Image = bm;
                    }
                }
            }
        }

        delegate void SetListItemAddCallback(Control label, string text);

        private void SetListItemAddText(Control label, string text)
        {
            if (label.InvokeRequired)
            {
                var d = new SetListItemAddCallback(SetListItemAddText);
                try
                {
                    this.BeginInvoke(d, new object[] { label, text });
                }
                catch (ObjectDisposedException) { }
            }
            else
            {
                label.Text = text;
            }
        }
        public void UpdateTextFunc(Control ctrl, string text)
        {
            //if (ctrl.InvokeRequired)
            //{
            //    ctrl.Invoke(new UpdateText(UpdateTextFunc), new object[] { ctrl, text });
            //}
            //else
                ctrl.Text = text;
        }

        private void frmLPRComm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Visible = false;
        }

        private void frmLPRComm_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible && frmLprMain.Main != null)
                this.Location = frmLprMain.Main.Location;
        }
    }
}
