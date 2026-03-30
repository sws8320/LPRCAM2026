using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace KyungsinLPR.iNova2 {
    public partial class AutoFlashConfigForm : Form
    {
        private IPCamera m_camera;
        private ToolTip m_tooltip = new ToolTip();

        public AutoFlashConfigForm(IPCamera camera)
        {
            m_camera = camera;
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                MaxExposure = Convert.ToInt32(txtMaxExposure.Text);
                MinExposure = Convert.ToInt32(txtMinExposure.Text);
            }
            catch (FormatException)
            {
                MessageBox.Show("Number format error");
                Close();
                return;
            }

            ControlFilter = chkControlFilter.Checked;
            ControlMono = chkControlMono.Checked;

            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }

        private void AutoFlashConfigForm_Load(object sender, EventArgs e)
        {
            txtMaxExposure.Text = MaxExposure.ToString();
            txtMinExposure.Text = MinExposure.ToString();
            if (m_camera.IsInova2_Compact())
                chkControlFilter.Enabled = false;
            else
                chkControlFilter.Checked = ControlFilter;
            chkControlMono.Checked = ControlMono;

            m_tooltip.AutoPopDelay = 5000;
            m_tooltip.InitialDelay = 500;
            m_tooltip.ReshowDelay = 500;
            m_tooltip.SetToolTip(txtMaxExposure, "Switch to night mode if the exposure exceeds this value. This has to be lower than the maximum value of the auto exposure range.");
            m_tooltip.SetToolTip(txtMinExposure, "Switch to day mode if the exposure goes below this value. This has to be larger than the minimum value of the auto exposure range.");
            m_tooltip.SetToolTip(chkControlFilter, "Determines if filter switcher should be turned off during night mode.");
            m_tooltip.SetToolTip(chkControlMono, "Determines if monochrome mode should be enabled during night mode.");
        }

        public int MaxExposure { get; set; }
        public int MinExposure { get; set; }
        public bool ControlFilter { get; set; }
        public bool ControlMono { get; set; }
    }
}
