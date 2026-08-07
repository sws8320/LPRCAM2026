namespace KyungsinLPR
{
    partial class UsbCamSettingPanel
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 구성 요소 디자이너에서 생성한 코드

        private void InitializeComponent()
        {
            this.chkUsbCam = new System.Windows.Forms.CheckBox();
            this.btnSelect = new System.Windows.Forms.Button();
            this.lblInfo = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // chkUsbCam
            //
            this.chkUsbCam.AutoSize = true;
            this.chkUsbCam.BackColor = System.Drawing.Color.Gold;
            this.chkUsbCam.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.chkUsbCam.Location = new System.Drawing.Point(6, 8);
            this.chkUsbCam.Name = "chkUsbCam";
            this.chkUsbCam.Size = new System.Drawing.Size(108, 19);
            this.chkUsbCam.TabIndex = 0;
            this.chkUsbCam.Text = "USB 카메라 사용";
            this.chkUsbCam.UseVisualStyleBackColor = false;
            this.chkUsbCam.CheckedChanged += new System.EventHandler(this.chkUsbCam_CheckedChanged);
            //
            // btnSelect
            //
            this.btnSelect.BackColor = System.Drawing.Color.White;
            this.btnSelect.Enabled = false;
            this.btnSelect.Location = new System.Drawing.Point(125, 4);
            this.btnSelect.Name = "btnSelect";
            this.btnSelect.Size = new System.Drawing.Size(115, 26);
            this.btnSelect.TabIndex = 1;
            this.btnSelect.Text = "USB 장치 선택...";
            this.btnSelect.UseVisualStyleBackColor = false;
            this.btnSelect.Click += new System.EventHandler(this.btnSelect_Click);
            //
            // lblInfo
            //
            this.lblInfo.BackColor = System.Drawing.Color.Gold;
            this.lblInfo.Font = new System.Drawing.Font("맑은 고딕", 8F);
            this.lblInfo.ForeColor = System.Drawing.Color.Black;
            this.lblInfo.Location = new System.Drawing.Point(246, 6);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(120, 30);
            this.lblInfo.TabIndex = 2;
            this.lblInfo.Text = "(미설정)";
            //
            // UsbCamSettingPanel
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Gold;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.btnSelect);
            this.Controls.Add(this.chkUsbCam);
            this.Name = "UsbCamSettingPanel";
            this.Size = new System.Drawing.Size(370, 38);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.CheckBox chkUsbCam;
        private System.Windows.Forms.Button btnSelect;
        private System.Windows.Forms.Label lblInfo;
    }
}
