using System;
using System.Drawing;
using System.Windows.Forms;

namespace KyungsinLPR
{
    /// <summary>
    /// 서버모드 멀티카메라(최대 15대) 설정 다이얼로그.
    /// ServerCamConfig 를 DataGridView 로 편집 → 저장(Setting.ini [SVRCAM1~15]).
    /// </summary>
    public partial class frmServerCams : Form
    {
        public frmServerCams()
        {
            InitializeComponent();
            SetupColumns();
            LoadRows();
        }

        private void SetupColumns()
        {
            dgvCams.AutoGenerateColumns = false;
            dgvCams.Columns.Clear();
            dgvCams.Columns.Add(new DataGridViewCheckBoxColumn { Name = "use", HeaderText = "사용", Width = 40 });
            dgvCams.Columns.Add(new DataGridViewTextBoxColumn { Name = "name", HeaderText = "이름", Width = 90 });
            dgvCams.Columns.Add(new DataGridViewTextBoxColumn { Name = "ip", HeaderText = "카메라 IP", Width = 115 });
            dgvCams.Columns.Add(new DataGridViewCheckBoxColumn { Name = "udp", HeaderText = "UDP", Width = 42 });
            DataGridViewComboBoxColumn gate = new DataGridViewComboBoxColumn { Name = "gate", HeaderText = "입출구", Width = 60 };
            gate.Items.Add("입구"); gate.Items.Add("출구");
            gate.FlatStyle = FlatStyle.Flat;
            dgvCams.Columns.Add(gate);
            dgvCams.Columns.Add(new DataGridViewTextBoxColumn { Name = "roi", HeaderText = "ROI(x,y,w,h)", Width = 115 });
            dgvCams.Columns.Add(new DataGridViewTextBoxColumn { Name = "dioip", HeaderText = "Dingtian IP", Width = 110 });
            dgvCams.Columns.Add(new DataGridViewTextBoxColumn { Name = "dioport", HeaderText = "포트", Width = 50 });
            dgvCams.Columns.Add(new DataGridViewTextBoxColumn { Name = "dioin", HeaderText = "입력ch", Width = 48 });
            dgvCams.Columns.Add(new DataGridViewTextBoxColumn { Name = "dioout", HeaderText = "출력ch", Width = 48 });
            dgvCams.Columns.Add(new DataGridViewTextBoxColumn { Name = "dispip", HeaderText = "전광판 IP", Width = 110 });
            dgvCams.Columns.Add(new DataGridViewTextBoxColumn { Name = "dispport", HeaderText = "포트", Width = 50 });
            // 콤보 값이 목록에 없을 때 예외 대신 무시
            dgvCams.DataError += delegate (object s, DataGridViewDataErrorEventArgs e) { e.ThrowException = false; };
        }

        private void LoadRows()
        {
            ServerCamConfig.Load();
            dgvCams.Rows.Clear();
            for (int i = 0; i < ServerCamConfig.MAX; i++)
            {
                ServerCamConfig.CamSetting c = ServerCamConfig.Cams[i];
                string roi = string.Format("{0},{1},{2},{3}", c.Roi.X, c.Roi.Y, c.Roi.Width, c.Roi.Height);
                int r = dgvCams.Rows.Add(c.Use, c.Name, c.Ip, c.Udp, c.GateType == 1 ? "출구" : "입구",
                                         roi, c.DioIp, c.DioPort, c.DioInTrigger, c.DioOutGate, c.DispIp, c.DispPort);
                dgvCams.Rows[r].HeaderCell.Value = (i + 1).ToString();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvCams.IsCurrentCellInEditMode) dgvCams.EndEdit();
                for (int i = 0; i < ServerCamConfig.MAX && i < dgvCams.Rows.Count; i++)
                {
                    DataGridViewRow row = dgvCams.Rows[i];
                    ServerCamConfig.CamSetting c = new ServerCamConfig.CamSetting();
                    c.Use = ToBool(row.Cells["use"].Value);
                    c.Name = ToStr(row.Cells["name"].Value);
                    c.Ip = ToStr(row.Cells["ip"].Value);
                    c.Udp = ToBool(row.Cells["udp"].Value);
                    c.GateType = ToStr(row.Cells["gate"].Value) == "출구" ? 1 : 0;
                    c.Roi = ParseRoi(ToStr(row.Cells["roi"].Value));
                    c.DioIp = ToStr(row.Cells["dioip"].Value);
                    c.DioPort = ToInt(row.Cells["dioport"].Value);
                    c.DioInTrigger = ToInt(row.Cells["dioin"].Value);
                    c.DioOutGate = ToInt(row.Cells["dioout"].Value);
                    c.DispIp = ToStr(row.Cells["dispip"].Value);
                    c.DispPort = ToInt(row.Cells["dispport"].Value);
                    if (string.IsNullOrEmpty(c.Name)) c.Name = "CAM" + (i + 1);
                    ServerCamConfig.Cams[i] = c;
                }
                ServerCamConfig.Save();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("저장 오류: " + ex.Message, "서버 카메라 설정",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private static string ToStr(object o) { return o == null ? "" : o.ToString().Trim(); }
        private static bool ToBool(object o)
        {
            if (o == null) return false;
            if (o is bool) return (bool)o;
            bool b; return bool.TryParse(o.ToString(), out b) && b;
        }
        private static int ToInt(object o)
        {
            int v; return (o != null && int.TryParse(o.ToString().Trim(), out v)) ? v : 0;
        }
        private static Rectangle ParseRoi(string s)
        {
            try
            {
                string[] p = (s ?? "").Split(',');
                if (p.Length == 4)
                    return new Rectangle(int.Parse(p[0].Trim()), int.Parse(p[1].Trim()),
                                         int.Parse(p[2].Trim()), int.Parse(p[3].Trim()));
            }
            catch { }
            return new Rectangle(0, 0, 0, 0);
        }
    }
}
