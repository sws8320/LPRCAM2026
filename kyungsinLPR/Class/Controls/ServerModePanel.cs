using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace KyungsinLPR
{
    /// <summary>
    /// 서버모드 메인 화면 — "실시간 입출차 모니터" 제목 + 카메라 카드 5×3 그리드.
    /// 사용(Use=true) 카메라만 카드로 표시. 카드 갱신은 Card(index) 로 접근.
    /// frmLprMain 위에 Dock=Fill 로 올려 서버모드 전용 화면을 구성한다.
    /// </summary>
    public class ServerModePanel : Panel
    {
        private readonly Dictionary<int, CameraCard> _cards = new Dictionary<int, CameraCard>();
        private readonly Timer _clock;

        /// <summary>제목바의 "환경설정" 버튼 클릭 — frmLprMain 이 기존 환경설정을 연다.</summary>
        public event EventHandler EnvClicked;

        public ServerModePanel()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(238, 241, 246);
            this.AutoScroll = true;

            // --- 제목 바 (환경설정 버튼 포함) ---
            Panel titleBar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.FromArgb(238, 241, 246), Padding = new Padding(0, 8, 10, 8) };
            int active = ServerCamConfig.ActiveCount();
            Button btnEnv = new Button
            {
                Text = "환경설정",
                Dock = DockStyle.Right,
                Width = 110,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 65, 95),
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 10f, FontStyle.Bold)
            };
            btnEnv.FlatAppearance.BorderSize = 0;
            btnEnv.Click += delegate { if (EnvClicked != null) EnvClicked(this, EventArgs.Empty); };
            Label title = new Label
            {
                Text = "실시간 입출차 모니터   (전체 " + active + "개)",
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(40, 55, 80),
                Font = new Font("맑은 고딕", 13f, FontStyle.Bold),
                Padding = new Padding(14, 0, 0, 0)
            };
            // Dock: Right 버튼 먼저, Fill 라벨 나중에 추가(권장 순서)
            titleBar.Controls.Add(btnEnv);
            titleBar.Controls.Add(title);

            // --- 카드 그리드(5×3) ---
            TableLayoutPanel grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = ServerCamConfig.COLS,
                RowCount = ServerCamConfig.ROWS,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8),
                BackColor = Color.FromArgb(238, 241, 246)
            };
            for (int c = 0; c < ServerCamConfig.COLS; c++)
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            for (int r = 0; r < ServerCamConfig.ROWS; r++)
                grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            int col = 0, row = 0;
            for (int i = 0; i < ServerCamConfig.MAX; i++)
            {
                if (!ServerCamConfig.Cams[i].Use) continue;
                CameraCard card = new CameraCard(i, ServerCamConfig.Cams[i]);
                _cards[i] = card;
                grid.Controls.Add(card, col, row);
                col++;
                if (col >= ServerCamConfig.COLS) { col = 0; row++; }
            }

            this.Controls.Add(grid);
            this.Controls.Add(titleBar);

            // --- 시계(헤더 시각 갱신) ---
            _clock = new Timer { Interval = 1000 };
            _clock.Tick += delegate
            {
                DateTime now = DateTime.Now;
                foreach (CameraCard cc in _cards.Values) cc.SetTime(now);
            };
            _clock.Start();
        }

        /// <summary>index 카메라의 카드(없으면 null).</summary>
        public CameraCard Card(int index)
        {
            CameraCard c;
            return _cards.TryGetValue(index, out c) ? c : null;
        }

        public IEnumerable<CameraCard> Cards { get { return _cards.Values; } }

        /// <summary>Alt+S TEST 모드 — 모든 카드의 캡처버튼을 [차번입력][TEST]로 토글.</summary>
        public void SetTestMode(bool on)
        {
            foreach (CameraCard cc in _cards.Values) cc.SetTestMode(on);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _clock != null) { _clock.Stop(); _clock.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
