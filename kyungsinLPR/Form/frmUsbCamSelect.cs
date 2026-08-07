using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KyungsinLPR.USB;

namespace KyungsinLPR
{
    /// <summary>
    /// USB 카메라 장치/해상도 선택 다이얼로그 (코드 only, 디자이너 미사용).
    /// 환경설정 폼에서 호출. 결과: SelectedMoniker, SelectedDeviceName, SelectedWidth, SelectedHeight.
    /// </summary>
    public class frmUsbCamSelect : Form
    {
        private ComboBox cmbDevice;
        private ComboBox cmbResolution;
        private Button btnOk;
        private Button btnCancel;
        private Button btnRefresh;
        private Label lblDevice;
        private Label lblResolution;
        private Label lblInfo;

        public string SelectedMoniker { get; private set; }
        public string SelectedDeviceName { get; private set; }
        public int SelectedWidth { get; private set; }
        public int SelectedHeight { get; private set; }

        private List<KeyValuePair<string, string>> _devices = new List<KeyValuePair<string, string>>();

        public frmUsbCamSelect(string currentMoniker, int currentWidth, int currentHeight)
        {
            SelectedMoniker = currentMoniker ?? "";
            SelectedWidth = currentWidth;
            SelectedHeight = currentHeight;
            BuildUI();
            LoadDevices(currentMoniker);
        }

        private void BuildUI()
        {
            Text = "USB 카메라 선택";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(440, 200);
            Font = new Font("맑은 고딕", 9F);

            lblDevice = new Label { Text = "장치 :", Location = new Point(15, 22), AutoSize = true };
            cmbDevice = new ComboBox
            {
                Location = new Point(75, 18),
                Width = 270,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            btnRefresh = new Button { Text = "새로고침", Location = new Point(350, 17), Width = 75 };
            btnRefresh.Click += (s, e) => LoadDevices(null);

            lblResolution = new Label { Text = "해상도 :", Location = new Point(15, 62), AutoSize = true };
            cmbResolution = new ComboBox
            {
                Location = new Point(75, 58),
                Width = 270,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            lblInfo = new Label
            {
                Text = "※ UVC 호환 USB 카메라를 인식합니다. 해상도가 비어있으면 카메라 기본값이 사용됩니다.",
                Location = new Point(15, 100),
                Size = new Size(410, 40),
                ForeColor = Color.DimGray
            };

            btnOk = new Button { Text = "확인", Location = new Point(255, 155), Width = 80, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "취소", Location = new Point(345, 155), Width = 80, DialogResult = DialogResult.Cancel };

            btnOk.Click += BtnOk_Click;

            cmbDevice.SelectedIndexChanged += CmbDevice_SelectedIndexChanged;

            Controls.AddRange(new Control[] {
                lblDevice, cmbDevice, btnRefresh,
                lblResolution, cmbResolution,
                lblInfo, btnOk, btnCancel
            });

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void LoadDevices(string preselectMoniker)
        {
            try
            {
                cmbDevice.Items.Clear();
                _devices = USBCamera.ListDevices();
                if (_devices.Count == 0)
                {
                    cmbDevice.Items.Add("(장치 없음)");
                    cmbDevice.SelectedIndex = 0;
                    return;
                }
                foreach (var kv in _devices) cmbDevice.Items.Add(kv.Key);
                int idx = 0;
                if (!string.IsNullOrEmpty(preselectMoniker))
                {
                    for (int i = 0; i < _devices.Count; i++)
                    {
                        if (_devices[i].Value == preselectMoniker) { idx = i; break; }
                    }
                }
                cmbDevice.SelectedIndex = idx;
            }
            catch (Exception ex)
            {
                MessageBox.Show("장치 목록 조회 실패: " + ex.Message, "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CmbDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbResolution.Items.Clear();
            if (_devices.Count == 0 || cmbDevice.SelectedIndex < 0 || cmbDevice.SelectedIndex >= _devices.Count)
                return;

            try
            {
                var moniker = _devices[cmbDevice.SelectedIndex].Value;
                var resolutions = USBCamera.ListResolutions(moniker);

                var distinct = resolutions
                    .Select(r => new { r.Width, r.Height })
                    .Distinct()
                    .OrderByDescending(r => r.Width * r.Height)
                    .ToList();

                foreach (var r in distinct)
                {
                    cmbResolution.Items.Add(string.Format("{0} x {1}", r.Width, r.Height));
                }

                int sel = 0;
                if (SelectedWidth > 0 && SelectedHeight > 0)
                {
                    for (int i = 0; i < distinct.Count; i++)
                    {
                        if (distinct[i].Width == SelectedWidth && distinct[i].Height == SelectedHeight)
                        { sel = i; break; }
                    }
                }
                if (cmbResolution.Items.Count > 0) cmbResolution.SelectedIndex = sel;
            }
            catch (Exception ex)
            {
                Util.Logger.Log("[frmUsbCamSelect.LoadResolutions] " + ex.Message);
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (_devices.Count == 0 || cmbDevice.SelectedIndex < 0 || cmbDevice.SelectedIndex >= _devices.Count)
            {
                MessageBox.Show("USB 카메라가 검색되지 않았습니다.", "확인",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.None;
                return;
            }
            var kv = _devices[cmbDevice.SelectedIndex];
            SelectedDeviceName = kv.Key;
            SelectedMoniker = kv.Value;

            if (cmbResolution.SelectedIndex >= 0)
            {
                var parts = cmbResolution.SelectedItem.ToString().Split('x');
                if (parts.Length == 2)
                {
                    int.TryParse(parts[0].Trim(), out int w);
                    int.TryParse(parts[1].Trim(), out int h);
                    SelectedWidth = w;
                    SelectedHeight = h;
                }
            }
            else
            {
                SelectedWidth = 0;
                SelectedHeight = 0;
            }
        }
    }
}
