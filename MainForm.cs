using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using LSL;

namespace LSLMarkerSender
{
    public class MainForm : Form
    {
        // ---- LSL ----
        private StreamInfo _streamInfo;
        private StreamOutlet _outlet;
        private bool _isStreaming;

        // ---- Data ----
        private MarkerProfile _currentProfile;
        private string _currentProfileName = "default";

        // ---- Global Keyboard Hook ----
        private GlobalKeyboardHook _keyHook;

        // ---- UI Controls ----
        private Panel _topPanel;
        private Panel _settingsPanel;
        private FlowLayoutPanel _markerButtonsPanel;
        private SplitContainer _splitRight;
        private Panel _logPanel;

        // Settings controls
        private TextBox _txtStreamName;
        private TextBox _txtStreamType;
        private TextBox _txtSourceId;
        private ComboBox _cmbProfiles;
        private Button _btnStartStop;
        private Label _lblStatus;
        private Label _lblConsumerCount;

        // Marker management
        private DataGridView _dgvMarkers;
        private Button _btnAddMarker;
        private Button _btnRemoveMarker;
        private Button _btnSaveProfile;
        private Button _btnNewProfile;
        private Button _btnDeleteProfile;

        // On-screen button trigger options
        private CheckBox _chkAutoStartStream;
        private CheckBox _chkLargeButtons;
        private TextBox _txtQuickMarker;
        private Button _btnQuickSend;
        private ToolTip _toolTip;

        // Log
        private RichTextBox _rtbLog;
        private Button _btnClearLog;
        private Label _lblMarkerCount;
        private int _markersSent;

        // Timer for updating consumer count
        private System.Windows.Forms.Timer _statusTimer;

        // Dynamically created marker buttons
        private readonly List<Button> _markerButtons = new List<Button>();

        public MainForm()
        {
            InitializeForm();
            InitializeComponents();
            LoadProfileList();
            LoadProfile("default");
            SetupKeyboardHook();
            SetupStatusTimer();
        }

        #region Form Initialization

        private void InitializeForm()
        {
            Text = "LSL Marker Sender";
            Size = new Size(1160, 780);
            MinimumSize = new Size(950, 650);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f);
            BackColor = Color.FromArgb(30, 30, 35);
            ForeColor = Color.FromArgb(230, 230, 235);
            DoubleBuffered = true;
            KeyPreview = true;

            try
            {
                using var stream = typeof(MainForm).Assembly.GetManifestResourceStream("app.ico");
                if (stream != null)
                {
                    Icon = new Icon(stream);
                }
                else
                {
                    Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                }
            }
            catch { }
        }

        private void InitializeComponents()
        {
            // === TOP PANEL (Stream Settings + Status) ===
            _topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 145,
                Padding = new Padding(12, 8, 12, 8),
                BackColor = Color.FromArgb(38, 38, 45),
            };

            var lblTitle = new Label
            {
                Text = "🔬 LSL Marker Sender",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 180, 255),
                AutoSize = true,
                Location = new Point(14, 6)
            };
            _topPanel.Controls.Add(lblTitle);

            // Profile row
            var lblProfile = CreateLabel("Profil:", 14, 40);
            _topPanel.Controls.Add(lblProfile);

            _cmbProfiles = new ComboBox
            {
                Location = new Point(80, 37),
                Size = new Size(160, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(50, 50, 58),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            _cmbProfiles.SelectedIndexChanged += (s, e) =>
            {
                if (_cmbProfiles.SelectedItem != null)
                    LoadProfile(_cmbProfiles.SelectedItem.ToString());
            };
            _topPanel.Controls.Add(_cmbProfiles);

            _btnNewProfile = CreateSmallButton("Yeni", 250, 36, Color.FromArgb(46, 139, 87));
            _btnNewProfile.Click += BtnNewProfile_Click;
            _topPanel.Controls.Add(_btnNewProfile);

            _btnSaveProfile = CreateSmallButton("Kaydet", 320, 36, Color.FromArgb(41, 128, 185));
            _btnSaveProfile.Click += BtnSaveProfile_Click;
            _topPanel.Controls.Add(_btnSaveProfile);

            _btnDeleteProfile = CreateSmallButton("Sil", 398, 36, Color.FromArgb(192, 57, 43));
            _btnDeleteProfile.Click += BtnDeleteProfile_Click;
            _topPanel.Controls.Add(_btnDeleteProfile);

            // Stream settings row
            var lblStreamName = CreateLabel("Stream Adı:", 14, 72);
            _topPanel.Controls.Add(lblStreamName);

            _txtStreamName = CreateTextBox("MarkerStream", 105, 69, 140);
            _topPanel.Controls.Add(_txtStreamName);

            var lblStreamType = CreateLabel("Tip:", 255, 72);
            _topPanel.Controls.Add(lblStreamType);

            _txtStreamType = CreateTextBox("Markers", 290, 69, 100);
            _topPanel.Controls.Add(_txtStreamType);

            var lblSourceId = CreateLabel("Source ID:", 400, 72);
            _topPanel.Controls.Add(lblSourceId);

            _txtSourceId = CreateTextBox("LSLMarkerSender1", 480, 69, 160);
            _topPanel.Controls.Add(_txtSourceId);

            // Start/Stop button + Status
            _btnStartStop = new Button
            {
                Text = "▶  STREAM BAŞLAT",
                Location = new Point(14, 105),
                Size = new Size(200, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            _btnStartStop.FlatAppearance.BorderSize = 0;
            _btnStartStop.Click += BtnStartStop_Click;
            _topPanel.Controls.Add(_btnStartStop);

            _lblStatus = new Label
            {
                Text = "● Durduruldu",
                ForeColor = Color.FromArgb(231, 76, 60),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(225, 111),
            };
            _topPanel.Controls.Add(_lblStatus);

            _lblConsumerCount = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(149, 165, 166),
                Font = new Font("Segoe UI", 9f),
                AutoSize = true,
                Location = new Point(400, 112),
            };
            _topPanel.Controls.Add(_lblConsumerCount);

            Controls.Add(_topPanel);

            // === LEFT PANEL (Marker Definitions DataGridView) ===
            _settingsPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 530,
                Padding = new Padding(8, 4, 4, 8),
                BackColor = Color.FromArgb(33, 33, 40),
            };

            var lblMarkers = new Label
            {
                Text = "📋 Marker Tanımları",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 180, 255),
                AutoSize = true,
                Location = new Point(10, 6),
            };
            _settingsPanel.Controls.Add(lblMarkers);

            // DataGridView for markers
            _dgvMarkers = new DataGridView
            {
                Location = new Point(10, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Color.FromArgb(40, 40, 48),
                ForeColor = Color.White,
                GridColor = Color.FromArgb(60, 60, 70),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(50, 50, 60),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    SelectionBackColor = Color.FromArgb(50, 50, 60),
                    SelectionForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                },
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(40, 40, 48),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(60, 80, 110),
                    SelectionForeColor = Color.White,
                    Padding = new Padding(4, 2, 4, 2),
                },
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 32 },
            };

            _toolTip = new ToolTip();

            // Columns
            var colTrigger = new DataGridViewButtonColumn
            {
                Name = "colTrigger",
                HeaderText = "Tetikle",
                Text = "▶ Gönder",
                UseColumnTextForButtonValue = true,
                FillWeight = 18,
                FlatStyle = FlatStyle.Flat,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(39, 174, 96),
                    ForeColor = Color.White,
                    SelectionBackColor = Color.FromArgb(46, 204, 113),
                    SelectionForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                },
            };
            var colValue = new DataGridViewTextBoxColumn { Name = "colValue", HeaderText = "Marker Değeri", FillWeight = 32 };
            var colShortcut = new DataGridViewButtonColumn
            {
                Name = "colShortcut",
                HeaderText = "Klavye Tuşu",
                FillWeight = 22,
                FlatStyle = FlatStyle.Flat,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(55, 55, 68),
                    ForeColor = Color.FromArgb(255, 200, 60),
                    SelectionBackColor = Color.FromArgb(55, 55, 68),
                    SelectionForeColor = Color.FromArgb(255, 200, 60),
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                },
            };
            var colColor = new DataGridViewTextBoxColumn { Name = "colColor", HeaderText = "Renk", FillWeight = 14 };
            var colShow = new DataGridViewCheckBoxColumn
            {
                Name = "colShow",
                HeaderText = "Düğme",
                FillWeight = 12,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    NullValue = false,
                },
            };

            _dgvMarkers.Columns.AddRange(colTrigger, colValue, colShortcut, colColor, colShow);
            _dgvMarkers.CellClick += DgvMarkers_CellClick;
            _dgvMarkers.CellEndEdit += DgvMarkers_CellEndEdit;
            _dgvMarkers.CellValueChanged += DgvMarkers_CellValueChanged;
            _dgvMarkers.CurrentCellDirtyStateChanged += (s, e) =>
            {
                // Commit checkbox changes immediately
                if (_dgvMarkers.IsCurrentCellDirty)
                    _dgvMarkers.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            _settingsPanel.Controls.Add(_dgvMarkers);

            // Buttons below the grid
            _btnAddMarker = CreateSmallButton("+ Marker Ekle", 10, 0, Color.FromArgb(39, 174, 96));
            _btnAddMarker.Width = 130;
            _btnAddMarker.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _btnAddMarker.Click += BtnAddMarker_Click;
            _settingsPanel.Controls.Add(_btnAddMarker);

            _btnRemoveMarker = CreateSmallButton("− Seçili Sil", 150, 0, Color.FromArgb(192, 57, 43));
            _btnRemoveMarker.Width = 120;
            _btnRemoveMarker.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _btnRemoveMarker.Click += BtnRemoveMarker_Click;
            _settingsPanel.Controls.Add(_btnRemoveMarker);

            Controls.Add(_settingsPanel);

            // === RIGHT PANEL with resizable SplitContainer: Buttons (top) and Log (bottom) ===
            var rightContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(4, 4, 8, 8),
                BackColor = Color.FromArgb(30, 30, 35),
            };

            _splitRight = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 330,
                SplitterWidth = 6,
                BackColor = Color.FromArgb(50, 50, 62),
                Panel1MinSize = 160,
                Panel2MinSize = 120,
            };
            _splitRight.Panel1.BackColor = Color.FromArgb(30, 30, 35);
            _splitRight.Panel2.BackColor = Color.FromArgb(30, 30, 35);

            // Header panel for buttons
            var buttonsHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(33, 33, 40),
                Padding = new Padding(6, 4, 6, 4),
            };

            var lblButtons = new Label
            {
                Text = "🎯 Marker Düğmeleri (Tıklanabilir)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 180, 255),
                AutoSize = true,
                Location = new Point(4, 7),
            };
            buttonsHeader.Controls.Add(lblButtons);

            _chkAutoStartStream = new CheckBox
            {
                Text = "⚡ Tıklanınca oto-başlat",
                Checked = true,
                AutoSize = true,
                ForeColor = Color.FromArgb(46, 204, 113),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(275, 8),
                Cursor = Cursors.Hand,
            };
            _toolTip.SetToolTip(_chkAutoStartStream, "İşaretliyse, stream kapalı olsa bile butona tıklandığında stream otomatik başlatılır.");
            buttonsHeader.Controls.Add(_chkAutoStartStream);

            _chkLargeButtons = new CheckBox
            {
                Text = "🔍 Büyük Butonlar",
                Checked = false,
                AutoSize = true,
                ForeColor = Color.FromArgb(200, 200, 210),
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(445, 8),
                Cursor = Cursors.Hand,
            };
            _chkLargeButtons.CheckedChanged += (s, e) => RefreshMarkerButtons();
            _toolTip.SetToolTip(_chkLargeButtons, "Dokunmatik ekran veya daha geniş tıklama alanı için düğmeleri büyütür.");
            buttonsHeader.Controls.Add(_chkLargeButtons);

            // Quick Send Bar
            var quickBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Color.FromArgb(38, 38, 46),
                Padding = new Padding(6, 4, 6, 4),
            };

            var lblQuick = new Label
            {
                Text = "Anlık Gönder:",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 180, 190),
                AutoSize = true,
                Location = new Point(4, 7),
            };
            quickBar.Controls.Add(lblQuick);

            _txtQuickMarker = new TextBox
            {
                Location = new Point(90, 4),
                Size = new Size(200, 24),
                BackColor = Color.FromArgb(50, 50, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f),
                PlaceholderText = "Metin yazıp Enter'a basın...",
            };
            _txtQuickMarker.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BtnQuickSend_Click(s, e);
                }
            };
            quickBar.Controls.Add(_txtQuickMarker);

            _btnQuickSend = CreateSmallButton("▶ Gönder", 298, 3, Color.FromArgb(39, 174, 96));
            _btnQuickSend.Width = 75;
            _btnQuickSend.Height = 26;
            _btnQuickSend.Click += BtnQuickSend_Click;
            quickBar.Controls.Add(_btnQuickSend);

            _markerButtonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(8),
                BackColor = Color.FromArgb(35, 35, 42),
            };

            // Add controls to Panel1 in order
            _splitRight.Panel1.Controls.Add(_markerButtonsPanel);
            _splitRight.Panel1.Controls.Add(quickBar);
            _splitRight.Panel1.Controls.Add(buttonsHeader);
            _markerButtonsPanel.BringToFront();

            // Log panel inside Panel2
            _logPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(4, 4, 4, 4),
                BackColor = Color.FromArgb(30, 30, 35),
            };

            var logHeader = new Panel { Dock = DockStyle.Top, Height = 30 };

            var lblLog = new Label
            {
                Text = "📝 Log",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 180, 255),
                AutoSize = true,
                Location = new Point(0, 4),
            };
            logHeader.Controls.Add(lblLog);

            _lblMarkerCount = new Label
            {
                Text = "Gönderilen: 0",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(149, 165, 166),
                AutoSize = true,
                Location = new Point(60, 8),
            };
            logHeader.Controls.Add(_lblMarkerCount);

            _btnClearLog = CreateSmallButton("Temizle", 0, 2, Color.FromArgb(127, 140, 141));
            _btnClearLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnClearLog.Click += (s, e) => { _rtbLog.Clear(); };
            logHeader.Controls.Add(_btnClearLog);

            _logPanel.Controls.Add(logHeader);

            _rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(25, 25, 30),
                ForeColor = Color.FromArgb(200, 200, 205),
                Font = new Font("Cascadia Code", 9f, FontStyle.Regular),
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                WordWrap = true,
            };
            _logPanel.Controls.Add(_rtbLog);

            _splitRight.Panel2.Controls.Add(_logPanel);

            rightContainer.Controls.Add(_splitRight);
            Controls.Add(rightContainer);

            // === Layout adjustments after controls are added ===
            Resize += (s, e) => LayoutAdjust();
            Load += (s, e) => LayoutAdjust();
        }

        private void LayoutAdjust()
        {
            // Adjust grid height
            if (_settingsPanel != null && _dgvMarkers != null)
            {
                int gridBottom = _settingsPanel.ClientSize.Height - 50;
                _dgvMarkers.Size = new Size(_settingsPanel.ClientSize.Width - 20, gridBottom - 32);
                _btnAddMarker.Top = gridBottom + 6;
                _btnRemoveMarker.Top = gridBottom + 6;
            }

            // Adjust log clear button position
            if (_btnClearLog != null && _btnClearLog.Parent != null)
            {
                _btnClearLog.Left = _btnClearLog.Parent.ClientSize.Width - _btnClearLog.Width - 4;
            }

            // Adjust marker count label position
            if (_lblMarkerCount != null && _btnClearLog != null)
            {
                _lblMarkerCount.Left = _btnClearLog.Left - _lblMarkerCount.Width - 10;
            }
        }

        #endregion

        #region UI Helpers

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Location = new Point(x, y),
                ForeColor = Color.FromArgb(180, 180, 190),
                Font = new Font("Segoe UI", 9.5f),
            };
        }

        private TextBox CreateTextBox(string text, int x, int y, int width)
        {
            return new TextBox
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 26),
                BackColor = Color.FromArgb(50, 50, 58),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
            };
        }

        private Button CreateSmallButton(string text, int x, int y, Color bgColor)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(70, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = bgColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        #endregion

        #region Profile Management

        private void LoadProfileList()
        {
            _cmbProfiles.Items.Clear();
            var names = ProfileManager.GetProfileNames();
            if (!names.Contains("default"))
            {
                // Create default profile
                ProfileManager.SaveProfile("default", ProfileManager.CreateDefaultProfile());
                names.Insert(0, "default");
            }
            foreach (var name in names)
                _cmbProfiles.Items.Add(name);
        }

        private void LoadProfile(string name)
        {
            _currentProfileName = name;
            _currentProfile = ProfileManager.LoadProfile(name);
            if (_currentProfile == null)
            {
                _currentProfile = ProfileManager.CreateDefaultProfile();
                ProfileManager.SaveProfile(name, _currentProfile);
            }

            // Update UI
            _txtStreamName.Text = _currentProfile.StreamName;
            _txtStreamType.Text = _currentProfile.StreamType;
            _txtSourceId.Text = _currentProfile.SourceId;

            RefreshMarkerGrid();
            RefreshMarkerButtons();

            // Select in combo
            if (_cmbProfiles.Items.Contains(name))
                _cmbProfiles.SelectedItem = name;

            Log($"Profil yüklendi: {name}", Color.FromArgb(100, 180, 255));
        }

        private void BtnSaveProfile_Click(object sender, EventArgs e)
        {
            SyncProfileFromUI();
            ProfileManager.SaveProfile(_currentProfileName, _currentProfile);
            Log($"Profil kaydedildi: {_currentProfileName}", Color.FromArgb(39, 174, 96));
        }

        private void BtnNewProfile_Click(object sender, EventArgs e)
        {
            string name = ShowInputDialog("Yeni Profil", "Profil adı girin:", "profil1");
            if (string.IsNullOrWhiteSpace(name)) return;

            name = name.Trim();
            var profile = ProfileManager.CreateDefaultProfile();
            ProfileManager.SaveProfile(name, profile);
            _cmbProfiles.Items.Add(name);
            _cmbProfiles.SelectedItem = name;
        }

        private void BtnDeleteProfile_Click(object sender, EventArgs e)
        {
            if (_currentProfileName == "default")
            {
                MessageBox.Show("Varsayılan profil silinemez.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"'{_currentProfileName}' profili silinecek. Emin misiniz?", "Profil Sil",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ProfileManager.DeleteProfile(_currentProfileName);
                _cmbProfiles.Items.Remove(_currentProfileName);
                _cmbProfiles.SelectedItem = "default";
            }
        }

        private void SyncProfileFromUI()
        {
            _currentProfile.StreamName = _txtStreamName.Text.Trim();
            _currentProfile.StreamType = _txtStreamType.Text.Trim();
            _currentProfile.SourceId = _txtSourceId.Text.Trim();

            // Markers are already kept in sync via grid events
        }

        #endregion

        #region Marker Grid Management

        private void RefreshMarkerGrid()
        {
            _dgvMarkers.Rows.Clear();
            foreach (var marker in _currentProfile.Markers)
            {
                int rowIdx = _dgvMarkers.Rows.Add(
                    "▶ Gönder",
                    marker.MarkerValue,
                    marker.ShortcutDisplay,
                    marker.ButtonColor,
                    marker.ShowAsButton
                );

                // Color the color cell
                var colorCell = _dgvMarkers.Rows[rowIdx].Cells["colColor"];
                try
                {
                    colorCell.Style.BackColor = ColorTranslator.FromHtml(marker.ButtonColor);
                    colorCell.Style.ForeColor = Color.White;
                }
                catch { }
            }
        }

        private void DgvMarkers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _currentProfile.Markers.Count) return;

            // Click on Trigger column -> send marker immediately
            if (e.ColumnIndex == _dgvMarkers.Columns["colTrigger"].Index)
            {
                SendMarker(_currentProfile.Markers[e.RowIndex]);
            }
            // Click on Shortcut column -> capture next key press
            else if (e.ColumnIndex == _dgvMarkers.Columns["colShortcut"].Index)
            {
                CaptureShortcutForMarker(e.RowIndex);
            }
            // Click on Color column -> open color picker
            else if (e.ColumnIndex == _dgvMarkers.Columns["colColor"].Index)
            {
                using (var cd = new ColorDialog())
                {
                    try { cd.Color = ColorTranslator.FromHtml(_currentProfile.Markers[e.RowIndex].ButtonColor); } catch { }
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        string hex = $"#{cd.Color.R:X2}{cd.Color.G:X2}{cd.Color.B:X2}";
                        _currentProfile.Markers[e.RowIndex].ButtonColor = hex;
                        _dgvMarkers.Rows[e.RowIndex].Cells["colColor"].Value = hex;
                        _dgvMarkers.Rows[e.RowIndex].Cells["colColor"].Style.BackColor = cd.Color;
                        RefreshMarkerButtons();
                    }
                }
            }
        }

        private void DgvMarkers_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _currentProfile.Markers.Count) return;

            if (e.ColumnIndex == _dgvMarkers.Columns["colValue"].Index)
            {
                var val = _dgvMarkers.Rows[e.RowIndex].Cells["colValue"].Value?.ToString() ?? "";
                _currentProfile.Markers[e.RowIndex].MarkerValue = val;
                RefreshMarkerButtons();
            }
        }

        private void DgvMarkers_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _currentProfile.Markers.Count) return;

            if (e.ColumnIndex == _dgvMarkers.Columns["colShow"].Index)
            {
                var val = _dgvMarkers.Rows[e.RowIndex].Cells["colShow"].Value;
                _currentProfile.Markers[e.RowIndex].ShowAsButton = val != null && (bool)val;
                RefreshMarkerButtons();
            }
        }

        private void CaptureShortcutForMarker(int rowIndex)
        {
            var dlg = new ShortcutCaptureForm();
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _currentProfile.Markers[rowIndex].Shortcut = dlg.CapturedKey;
                _dgvMarkers.Rows[rowIndex].Cells["colShortcut"].Value = _currentProfile.Markers[rowIndex].ShortcutDisplay;
                RefreshMarkerButtons();
                Log($"Tuş güncellendi: {_currentProfile.Markers[rowIndex].MarkerValue} → {_currentProfile.Markers[rowIndex].ShortcutDisplay}", Color.FromArgb(241, 196, 15));
            }
        }

        private void BtnAddMarker_Click(object sender, EventArgs e)
        {
            var marker = new MarkerDefinition
            {
                MarkerValue = $"marker_{_currentProfile.Markers.Count + 1}",
                Shortcut = Keys.None,
                ButtonColor = "#4A90D9",
                ShowAsButton = true,
            };
            _currentProfile.Markers.Add(marker);
            RefreshMarkerGrid();
            RefreshMarkerButtons();
        }

        private void BtnRemoveMarker_Click(object sender, EventArgs e)
        {
            if (_dgvMarkers.SelectedRows.Count == 0) return;
            int idx = _dgvMarkers.SelectedRows[0].Index;
            if (idx >= 0 && idx < _currentProfile.Markers.Count)
            {
                _currentProfile.Markers.RemoveAt(idx);
                RefreshMarkerGrid();
                RefreshMarkerButtons();
            }
        }

        #endregion

        #region Marker Buttons

        private void RefreshMarkerButtons()
        {
            // Clear existing buttons
            foreach (var btn in _markerButtons)
                btn.Dispose();
            _markerButtons.Clear();
            _markerButtonsPanel.Controls.Clear();

            if (_currentProfile?.Markers == null) return;

            bool isLarge = _chkLargeButtons != null && _chkLargeButtons.Checked;
            int btnWidth = isLarge ? 185 : 148;
            int btnHeight = isLarge ? 72 : 56;
            float titleFontSize = isLarge ? 10.5f : 9f;

            foreach (var marker in _currentProfile.Markers)
            {
                if (!marker.ShowAsButton) continue;

                Color bgColor;
                try { bgColor = ColorTranslator.FromHtml(marker.ButtonColor); }
                catch { bgColor = Color.FromArgb(74, 144, 217); }

                string shortcutBadge = marker.Shortcut != Keys.None
                    ? $"[⌨ {marker.ShortcutDisplay}]"
                    : "[👆 Buton]";

                var btn = new Button
                {
                    Text = $"{marker.MarkerValue}\n{shortcutBadge}",
                    Size = new Size(btnWidth, btnHeight),
                    Margin = new Padding(5),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = bgColor,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", titleFontSize, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    Tag = marker,
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.BorderColor = Color.FromArgb(
                    Math.Min(255, bgColor.R + 40),
                    Math.Min(255, bgColor.G + 40),
                    Math.Min(255, bgColor.B + 40));

                btn.Click += (s, e) => SendMarker(marker);

                // Hover effects
                btn.MouseEnter += (s, e) =>
                {
                    btn.BackColor = Color.FromArgb(
                        Math.Min(255, bgColor.R + 30),
                        Math.Min(255, bgColor.G + 30),
                        Math.Min(255, bgColor.B + 30));
                };
                btn.MouseLeave += (s, e) => { btn.BackColor = bgColor; };

                _toolTip?.SetToolTip(btn, $"Tıklayarak marker gönder:\n\"{marker.MarkerValue}\"\n(Kısayol: {marker.ShortcutDisplay})");

                _markerButtons.Add(btn);
                _markerButtonsPanel.Controls.Add(btn);
            }
        }

        private void BtnQuickSend_Click(object sender, EventArgs e)
        {
            string val = _txtQuickMarker?.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(val)) return;

            var quickMarker = new MarkerDefinition
            {
                MarkerValue = val,
                Shortcut = Keys.None,
                ButtonColor = "#27AE60",
                ShowAsButton = false,
            };
            SendMarker(quickMarker);
            _txtQuickMarker.SelectAll();
        }

        #endregion

        #region LSL Stream Management

        private void BtnStartStop_Click(object sender, EventArgs e)
        {
            if (_isStreaming)
                StopStream();
            else
                StartStream();
        }

        private void StartStream()
        {
            try
            {
                SyncProfileFromUI();

                string streamName = _currentProfile.StreamName;
                string streamType = _currentProfile.StreamType;
                string sourceId = _currentProfile.SourceId;

                if (string.IsNullOrWhiteSpace(streamName))
                {
                    MessageBox.Show("Stream adı boş olamaz.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _streamInfo = new StreamInfo(streamName, streamType, 1, LSL.LSL.IRREGULAR_RATE,
                    channel_format_t.cf_string, sourceId);
                _outlet = new StreamOutlet(_streamInfo);

                _isStreaming = true;
                _markersSent = 0;
                UpdateStreamUI();

                Log($"Stream başlatıldı: {streamName} (tip: {streamType}, id: {sourceId})", Color.FromArgb(39, 174, 96));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Stream başlatılamadı:\n{ex.Message}\n\nlsl.dll dosyasının exe yanında olduğundan emin olun.",
                    "LSL Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log($"HATA: {ex.Message}", Color.FromArgb(231, 76, 60));
            }
        }

        private void StopStream()
        {
            try
            {
                _outlet?.Dispose();
                _streamInfo?.Dispose();
            }
            catch { }

            _outlet = null;
            _streamInfo = null;
            _isStreaming = false;

            UpdateStreamUI();
            Log("Stream durduruldu.", Color.FromArgb(231, 76, 60));
        }

        private void UpdateStreamUI()
        {
            if (_isStreaming)
            {
                _btnStartStop.Text = "⏹  STREAM DURDUR";
                _btnStartStop.BackColor = Color.FromArgb(192, 57, 43);
                _lblStatus.Text = "● Yayında";
                _lblStatus.ForeColor = Color.FromArgb(39, 174, 96);

                // Disable settings editing while streaming
                _txtStreamName.Enabled = false;
                _txtStreamType.Enabled = false;
                _txtSourceId.Enabled = false;
            }
            else
            {
                _btnStartStop.Text = "▶  STREAM BAŞLAT";
                _btnStartStop.BackColor = Color.FromArgb(39, 174, 96);
                _lblStatus.Text = "● Durduruldu";
                _lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
                _lblConsumerCount.Text = "";

                _txtStreamName.Enabled = true;
                _txtStreamType.Enabled = true;
                _txtSourceId.Enabled = true;
            }
        }

        private void SendMarker(MarkerDefinition marker)
        {
            if (!_isStreaming || _outlet == null)
            {
                if (_chkAutoStartStream != null && _chkAutoStartStream.Checked)
                {
                    Log("⚡ Tıklama algılandı, stream otomatik başlatılıyor...", Color.FromArgb(46, 204, 113));
                    StartStream();
                    if (!_isStreaming || _outlet == null)
                        return;
                }
                else
                {
                    Log($"⚠ Stream aktif değil! Önce stream başlatın veya 'Oto-Başlat' seçeneğini açın. (Tetiklenen: {marker.MarkerValue})", Color.FromArgb(241, 196, 15));
                    return;
                }
            }

            try
            {
                string[] sample = new string[] { marker.MarkerValue };
                _outlet.push_sample(sample);
                _markersSent++;

                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                Log($"[{timestamp}] ✓ Gönderildi: \"{marker.MarkerValue}\"", Color.FromArgb(39, 174, 96));
                _lblMarkerCount.Text = $"Gönderilen: {_markersSent}";

                // Flash the corresponding button
                FlashButton(marker);
            }
            catch (Exception ex)
            {
                Log($"HATA: Marker gönderilemedi - {ex.Message}", Color.FromArgb(231, 76, 60));
            }
        }

        private void FlashButton(MarkerDefinition marker)
        {
            var btn = _markerButtons.FirstOrDefault(b => b.Tag == marker);
            if (btn == null) return;

            Color original = btn.BackColor;
            btn.BackColor = Color.White;

            var flashTimer = new System.Windows.Forms.Timer { Interval = 120 };
            flashTimer.Tick += (s, e) =>
            {
                btn.BackColor = original;
                flashTimer.Stop();
                flashTimer.Dispose();
            };
            flashTimer.Start();
        }

        #endregion

        #region Keyboard Hook

        private void SetupKeyboardHook()
        {
            _keyHook = new GlobalKeyboardHook();
            _keyHook.KeyPressed += OnGlobalKeyPressed;
            _keyHook.Hook();
        }

        private void OnGlobalKeyPressed(Keys key)
        {
            if (_currentProfile?.Markers == null) return;

            foreach (var marker in _currentProfile.Markers)
            {
                if (marker.Shortcut != Keys.None && marker.Shortcut == key)
                {
                    // Must invoke on UI thread
                    if (InvokeRequired)
                        BeginInvoke(new Action(() => SendMarker(marker)));
                    else
                        SendMarker(marker);
                    break;
                }
            }
        }

        #endregion

        #region Status Timer

        private void SetupStatusTimer()
        {
            _statusTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            _statusTimer.Tick += (s, e) =>
            {
                if (_isStreaming && _outlet != null)
                {
                    try
                    {
                        bool hasConsumers = _outlet.have_consumers();
                        _lblConsumerCount.Text = hasConsumers
                            ? "🟢 Consumer bağlı"
                            : "⚪ Consumer bekleniyor...";
                    }
                    catch { }
                }
            };
            _statusTimer.Start();
        }

        #endregion

        #region Logging

        private void Log(string message, Color color)
        {
            if (_rtbLog.InvokeRequired)
            {
                _rtbLog.BeginInvoke(new Action(() => Log(message, color)));
                return;
            }

            _rtbLog.SelectionStart = _rtbLog.TextLength;
            _rtbLog.SelectionColor = color;
            _rtbLog.AppendText(message + Environment.NewLine);
            _rtbLog.ScrollToCaret();

            // Limit log lines
            if (_rtbLog.Lines.Length > 500)
            {
                _rtbLog.SelectionStart = 0;
                _rtbLog.SelectionLength = _rtbLog.GetFirstCharIndexFromLine(100);
                _rtbLog.SelectedText = "";
            }
        }

        #endregion

        #region Input Dialog

        private string ShowInputDialog(string title, string prompt, string defaultValue)
        {
            var dlg = new Form
            {
                Text = title,
                Size = new Size(400, 180),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(40, 40, 48),
                ForeColor = Color.White,
            };

            var lbl = new Label { Text = prompt, Location = new Point(20, 20), AutoSize = true };
            var txt = new TextBox
            {
                Text = defaultValue,
                Location = new Point(20, 50),
                Size = new Size(340, 26),
                BackColor = Color.FromArgb(55, 55, 65),
                ForeColor = Color.White,
            };

            var btnOk = new Button
            {
                Text = "Tamam",
                DialogResult = DialogResult.OK,
                Location = new Point(200, 90),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White,
            };
            btnOk.FlatAppearance.BorderSize = 0;

            var btnCancel = new Button
            {
                Text = "İptal",
                DialogResult = DialogResult.Cancel,
                Location = new Point(290, 90),
                Size = new Size(70, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 100, 110),
                ForeColor = Color.White,
            };
            btnCancel.FlatAppearance.BorderSize = 0;

            dlg.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            return dlg.ShowDialog(this) == DialogResult.OK ? txt.Text : null;
        }

        #endregion

        #region Cleanup

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _keyHook?.Dispose();
            _statusTimer?.Stop();
            _statusTimer?.Dispose();
            _toolTip?.Dispose();

            if (_isStreaming)
                StopStream();

            base.OnFormClosing(e);
        }

        #endregion
    }
}
