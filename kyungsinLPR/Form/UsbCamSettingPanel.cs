using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace KyungsinLPR
{
    /// <summary>
    /// USB 카메라 설정 UserControl — frmEnv 카메라설정 탭에 디자이너로 배치 가능.
    /// 외부에서 SetInfo / SetIdle 호출로 표시 갱신, UseChanged / SelectRequested 이벤트로 알림.
    /// </summary>
    public partial class UsbCamSettingPanel : UserControl
    {
        public event EventHandler UseChanged;
        public event EventHandler SelectRequested;

        public UsbCamSettingPanel()
        {
            InitializeComponent();
        }

        /// <summary>USB 사용 여부 (CheckBox 상태와 동기). 외부 코드가 카메라 전환 시 설정.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsUsbUsed
        {
            get { return chkUsbCam.Checked; }
            set
            {
                if (chkUsbCam.Checked != value)
                {
                    chkUsbCam.Checked = value;
                }
                btnSelect.Enabled = value;
            }
        }

        /// <summary>장치 정보 표시 갱신.</summary>
        public void SetInfo(string deviceName, int width, int height)
        {
            lblInfo.Text = string.Format("{0}\n{1}x{2}",
                string.IsNullOrEmpty(deviceName) ? "(장치 미선택)" : deviceName,
                width, height);
        }

        /// <summary>미설정 상태 표시.</summary>
        public void SetIdle()
        {
            lblInfo.Text = "(미설정)";
        }

        private void chkUsbCam_CheckedChanged(object sender, EventArgs e)
        {
            btnSelect.Enabled = chkUsbCam.Checked;
            if (UseChanged != null) UseChanged(this, EventArgs.Empty);
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            if (SelectRequested != null) SelectRequested(this, EventArgs.Empty);
        }
    }
}
