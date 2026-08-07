namespace KyungsinLPR.iNova2 {
    partial class AutoFlashConfigForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AutoFlashConfigForm));
            this.label1 = new System.Windows.Forms.Label();
            this.txtMaxExposure = new System.Windows.Forms.TextBox();
            this.txtMinExposure = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.chkControlFilter = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.chkControlMono = new System.Windows.Forms.CheckBox();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(160, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "Maximum Exposure at Day";
            // 
            // txtMaxExposure
            // 
            this.txtMaxExposure.Location = new System.Drawing.Point(195, 17);
            this.txtMaxExposure.Name = "txtMaxExposure";
            this.txtMaxExposure.Size = new System.Drawing.Size(65, 21);
            this.txtMaxExposure.TabIndex = 1;
            // 
            // txtMinExposure
            // 
            this.txtMinExposure.Location = new System.Drawing.Point(195, 44);
            this.txtMinExposure.Name = "txtMinExposure";
            this.txtMinExposure.Size = new System.Drawing.Size(65, 21);
            this.txtMinExposure.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(163, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "Minimum Exposure at Night";
            // 
            // chkControlFilter
            // 
            this.chkControlFilter.AutoSize = true;
            this.chkControlFilter.Checked = true;
            this.chkControlFilter.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkControlFilter.Location = new System.Drawing.Point(25, 82);
            this.chkControlFilter.Name = "chkControlFilter";
            this.chkControlFilter.Size = new System.Drawing.Size(137, 16);
            this.chkControlFilter.TabIndex = 4;
            this.chkControlFilter.Text = "Control Filter Switch";
            this.chkControlFilter.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(198, 170);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // chkControlMono
            // 
            this.chkControlMono.AutoSize = true;
            this.chkControlMono.Checked = true;
            this.chkControlMono.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkControlMono.Location = new System.Drawing.Point(25, 108);
            this.chkControlMono.Name = "chkControlMono";
            this.chkControlMono.Size = new System.Drawing.Size(134, 16);
            this.chkControlMono.TabIndex = 6;
            this.chkControlMono.Text = "Control Color Mode";
            this.chkControlMono.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(4, 135);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(269, 33);
            this.label3.TabIndex = 7;
            this.label3.Text = "* To enable Auto Flash, Auto Exposure must also be enabled.";
            // 
            // AutoFlashConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(285, 201);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.chkControlMono);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.chkControlFilter);
            this.Controls.Add(this.txtMinExposure);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtMaxExposure);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "AutoFlashConfigForm";
            this.Text = "Auto Flash Settings";
            this.Load += new System.EventHandler(this.AutoFlashConfigForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtMaxExposure;
        private System.Windows.Forms.TextBox txtMinExposure;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox chkControlFilter;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.CheckBox chkControlMono;
        private System.Windows.Forms.Label label3;
    }
}