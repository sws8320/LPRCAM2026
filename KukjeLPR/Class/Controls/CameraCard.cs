using System;
using System.Drawing;
using System.Windows.Forms;

namespace KyungsinLPR
{
    /// <summary>
    /// 서버모드 카메라 카드 1장 — 헤더(이름+시각) · 영상 · 인식결과 · 입출차/일반 태그 ·
    /// 열기/닫기/고정 버튼 · 최근이력. 동적 생성(코드 빌드).
    /// 데이터 갱신은 SetImage/SetPlate/SetTime(스레드 안전).
    /// </summary>
    public class CameraCard : UserControl
    {
        public readonly int Index;
        public ServerCamConfig.CamSetting Cfg;

        private readonly PictureBox _video;
        private readonly Label _plate;
        private readonly Label _time;
        private readonly Label _stamp;
        private Color _accent;
        private Label _tGate;            // 입차/출차 태그
        private Label _tType;            // 일반/정기 태그
        private Button _capBtn;          // 평소: 캡처 버튼
        private Panel _testPanel;        // Alt+S: 차번입력창 + TEST 버튼
        private TextBox _plateBox;
        private Button _testBtn;

        public event EventHandler CaptureClicked;
        public event EventHandler HistoryClicked;
        public event EventHandler CardDoubleClicked;   // 카드 더블클릭 → 카메라 개별설정
        public event EventHandler TestClicked;         // TEST 버튼 클릭(차번 수동입력 정산)
        public event EventHandler TestPhotoClicked;    // TEST 버튼 우클릭(사진선택 인식)

        public PictureBox Video { get { return _video; } }
        /// <summary>TEST 차번입력창의 현재 차번.</summary>
        public string TestPlate { get { return _plateBox != null ? (_plateBox.Text ?? "").Trim() : ""; } }

        public CameraCard(int index, ServerCamConfig.CamSetting cfg)
        {
            Index = index;
            Cfg = cfg;
            _accent = cfg.GateType == 1 ? Color.FromArgb(220, 80, 90) : Color.FromArgb(70, 200, 130);

            // Dock 콘텐츠 합(헤더26+영상120+차번42+태그22+버튼32=242). 최근이력 제거로 높이 축소
            this.Size = new Size(286, 248);
            this.Margin = new Padding(6);
            this.BackColor = Color.FromArgb(26, 41, 66);
            this.Padding = new Padding(1);

            // --- 헤더 ---
            Panel header = new Panel { Dock = DockStyle.Top, Height = 26, BackColor = Color.FromArgb(20, 32, 52) };
            _time = new Label
            {
                Text = "● --:--:--",
                Dock = DockStyle.Right,
                Width = 90,
                ForeColor = Color.FromArgb(120, 220, 150),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("맑은 고딕", 8.5f)
            };
            Label name = new Label
            {
                Text = string.IsNullOrEmpty(cfg.Name) ? ("CAM" + (index + 1)) : cfg.Name,
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("맑은 고딕", 9f, FontStyle.Bold),
                Padding = new Padding(8, 0, 0, 0)
            };
            header.Controls.Add(name);
            header.Controls.Add(_time);

            // --- 영상 ---
            _video = new PictureBox { Dock = DockStyle.Top, Height = 120, BackColor = Color.Black, SizeMode = PictureBoxSizeMode.Zoom };
            _stamp = new Label { AutoSize = true, ForeColor = Color.White, BackColor = Color.Transparent, Font = new Font("Segoe UI", 7.5f), Text = "" };
            _video.Controls.Add(_stamp);
            _video.Resize += delegate { _stamp.Location = new Point(Math.Max(0, _video.Width - _stamp.Width - 4), Math.Max(0, _video.Height - _stamp.Height - 2)); };

            // --- 인식 결과 ---
            _plate = new Label
            {
                Text = "No_Detection",
                Dock = DockStyle.Top,
                Height = 42,
                ForeColor = _accent,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("맑은 고딕", 15f, FontStyle.Bold)
            };

            // --- 태그(입차/출차 · 일반/정기) — SetGate/SetCarType 으로 갱신 ---
            Panel tags = new Panel { Dock = DockStyle.Top, Height = 22 };
            _tGate = MakePill(cfg.GateType == 1 ? "출차" : "입차", _accent);
            _tType = MakePill("일반", Color.FromArgb(120, 128, 140));
            _tGate.Location = new Point(86, 2);
            _tType.Location = new Point(150, 2);
            tags.Controls.Add(_tGate);
            tags.Controls.Add(_tType);

            // --- 캡처 버튼 / (Alt+S) 차번입력+TEST ---
            Panel btns = new Panel { Dock = DockStyle.Top, Height = 32, Padding = new Padding(6, 2, 6, 2) };
            _capBtn = new Button
            {
                Text = "캡처",
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(46, 130, 200),
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 10.5f, FontStyle.Bold)
            };
            _capBtn.FlatAppearance.BorderSize = 0;
            _capBtn.Click += delegate { if (CaptureClicked != null) CaptureClicked(this, EventArgs.Empty); };

            // TEST 모드 패널: [차번입력][TEST]  (기본 숨김, Alt+S로 표시)
            _testPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
            _testBtn = new Button
            {
                Text = "TEST",
                Dock = DockStyle.Right,
                Width = 70,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(210, 120, 40),
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 9.5f, FontStyle.Bold)
            };
            _testBtn.FlatAppearance.BorderSize = 0;
            _testBtn.Click += delegate { if (TestClicked != null) TestClicked(this, EventArgs.Empty); };
            // 우클릭 → 사진선택 인식(기존 TEST 우클릭과 동일)
            _testBtn.MouseUp += delegate (object s, MouseEventArgs me) {
                if (me.Button == MouseButtons.Right && TestPhotoClicked != null) TestPhotoClicked(this, EventArgs.Empty);
            };
            _plateBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center
            };
            // Enter 키로도 TEST 실행
            _plateBox.KeyDown += delegate (object s, KeyEventArgs ke) {
                if (ke.KeyCode == Keys.Enter) { ke.SuppressKeyPress = true; if (TestClicked != null) TestClicked(this, EventArgs.Empty); }
            };
            _testPanel.Controls.Add(_plateBox);   // Fill 먼저
            _testPanel.Controls.Add(_testBtn);    // Right

            btns.Controls.Add(_testPanel);
            btns.Controls.Add(_capBtn);

            // Dock=Top 은 '나중에 추가한 것이 위'로 쌓이므로 아래→위 순서로 추가 (최근이력 제거됨)
            this.Controls.Add(btns);
            this.Controls.Add(tags);
            this.Controls.Add(_plate);
            this.Controls.Add(_video);
            this.Controls.Add(header);

            // 카드/영상/헤더 더블클릭 → 카메라 개별설정
            EventHandler dbl = delegate { if (CardDoubleClicked != null) CardDoubleClicked(this, EventArgs.Empty); };
            this.DoubleClick += dbl;
            _video.DoubleClick += dbl;
            header.DoubleClick += dbl;
            name.DoubleClick += dbl;
            _plate.DoubleClick += dbl;
        }

        // ---- 갱신 API (스레드 안전) ----
        public void SetTime(DateTime t)
        {
            Ui(delegate
            {
                _time.Text = "● " + t.ToString("HH:mm:ss");
                _stamp.Text = t.ToString("yyyy-MM-dd HH:mm:ss");
            });
        }

        public void SetPlate(string plate, bool recognized)
        {
            Ui(delegate
            {
                _plate.Text = string.IsNullOrEmpty(plate) ? "No_Detection" : plate;
                // 인식 차번 색은 SetCarType(일반/정기)이 최종 결정. 여기선 기본(인식=흰색, 미인식=흐림)
                _plate.ForeColor = recognized ? Color.White : Color.FromArgb(150, 160, 175);
            });
        }

        public void SetImage(Image img)
        {
            Ui(delegate
            {
                Image old = _video.Image;
                _video.Image = img;
                if (old != null && old != img) old.Dispose();
            });
        }

        /// <summary>입출구 태그 — 입구장비=입차(녹색), 출구장비=출차(적색). (차번 색은 일반/정기로 별도)</summary>
        public void SetGate(bool isExit)
        {
            Ui(delegate
            {
                _accent = isExit ? Color.FromArgb(220, 80, 90) : Color.FromArgb(70, 200, 130);
                _tGate.Text = isExit ? "출차" : "입차";
                _tGate.BackColor = _accent;
            });
        }

        /// <summary>차량종류 태그 + 차번 글자색 — 0=숨김/흐림(미인식), 1=일반(회색태그·흰차번), 2=정기(분홍태그·분홍차번).</summary>
        public void SetCarType(int state)
        {
            Ui(delegate
            {
                if (state == 2)
                {
                    _tType.Text = "정기"; _tType.BackColor = Color.FromArgb(225, 120, 180); _tType.Visible = true;
                    _plate.ForeColor = Color.FromArgb(245, 130, 200);   // 정기차량 = 분홍
                }
                else if (state == 1)
                {
                    _tType.Text = "일반"; _tType.BackColor = Color.FromArgb(120, 128, 140); _tType.Visible = true;
                    _plate.ForeColor = Color.White;                      // 일반차량 = 흰색
                }
                else { _tType.Visible = false; }   // 미인식: 태그 숨김(차번 색은 SetPlate 흐림)
            });
        }

        /// <summary>Alt+S TEST 모드 토글 — on이면 캡처버튼 자리에 [차번입력][TEST] 표시.</summary>
        public void SetTestMode(bool on)
        {
            Ui(delegate
            {
                if (_testPanel == null || _capBtn == null) return;
                _testPanel.Visible = on;
                _capBtn.Visible = !on;
                if (on) _testPanel.BringToFront();
                else _capBtn.BringToFront();
            });
        }

        /// <summary>현재 표시 중인 영상 프레임의 복제(캡처용, 스레드 안전). 없으면 null.</summary>
        public Image GetSnapshot()
        {
            Image result = null;
            Action a = delegate { try { if (_video.Image != null) result = (Image)_video.Image.Clone(); } catch { } };
            try { if (IsHandleCreated && InvokeRequired) Invoke(a); else a(); } catch { }
            return result;
        }

        private void Ui(Action a)
        {
            if (!IsHandleCreated) { try { a(); } catch { } return; }
            try { if (InvokeRequired) BeginInvoke(a); else a(); } catch { }
        }

        private static Label MakePill(string text, Color back)
        {
            return new Label
            {
                Text = text,
                AutoSize = false,
                Size = new Size(46, 18),
                BackColor = back,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("맑은 고딕", 8f)
            };
        }

        private static Button MakeBtn(string text, Color back, int x)
        {
            Button b = new Button
            {
                Text = text,
                Size = new Size(86, 26),
                Location = new Point(x, 2),
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 9.5f, FontStyle.Bold)
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}
