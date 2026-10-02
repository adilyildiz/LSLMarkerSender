using System;
using System.Drawing;
using System.Windows.Forms;

namespace LSLMarkerSender
{
    /// <summary>
    /// A modal dialog that captures a single key press and returns it.
    /// Used for assigning keyboard shortcuts to markers.
    /// </summary>
    public class ShortcutCaptureForm : Form
    {
        /// <summary>
        /// The key that was captured when the user pressed a key.
        /// </summary>
        public Keys CapturedKey { get; private set; } = Keys.None;

        private Label _lblInstruction;
        private Label _lblKey;
        private Button _btnClear;
        private Button _btnCancel;

        public ShortcutCaptureForm()
        {
            Text = "Tuş Ata";
            Size = new Size(440, 210);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            KeyPreview = true;
            BackColor = Color.FromArgb(35, 35, 42);
            ForeColor = Color.White;

            try
            {
                using var stream = typeof(ShortcutCaptureForm).Assembly.GetManifestResourceStream("app.ico");
                if (stream != null) Icon = new Icon(stream);
            }
            catch { }

            _lblInstruction = new Label
            {
                Text = "Atamak istediğiniz tuşa basın...",
                Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(200, 200, 210),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(0, 12, 0, 0),
            };

            _lblKey = new Label
            {
                Text = "Bekleniyor...",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 200, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 50,
            };

            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(20, 8, 20, 8),
            };

            _btnClear = new Button
            {
                Text = "Kısayolu Kaldır (Yalnızca Buton)",
                Size = new Size(230, 32),
                Location = new Point(20, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(243, 156, 18),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            _btnClear.FlatAppearance.BorderSize = 0;
            _btnClear.Click += (s, e) =>
            {
                CapturedKey = Keys.None;
                DialogResult = DialogResult.OK;
                Close();
            };

            _btnCancel = new Button
            {
                Text = "İptal",
                Size = new Size(80, 32),
                Location = new Point(270, 8),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(100, 100, 110),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            buttonPanel.Controls.Add(_btnClear);
            buttonPanel.Controls.Add(_btnCancel);

            Controls.Add(_lblKey);
            Controls.Add(_lblInstruction);
            Controls.Add(buttonPanel);

            // Bring instruction to top
            _lblInstruction.BringToFront();

            KeyDown += ShortcutCaptureForm_KeyDown;
        }

        private void ShortcutCaptureForm_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;

            CapturedKey = e.KeyCode;
            _lblKey.Text = e.KeyCode.ToString();
            _lblKey.ForeColor = Color.FromArgb(39, 174, 96);

            // Small delay to show the captured key before closing
            var timer = new System.Windows.Forms.Timer { Interval = 300 };
            timer.Tick += (s2, e2) =>
            {
                timer.Stop();
                timer.Dispose();
                DialogResult = DialogResult.OK;
                Close();
            };
            timer.Start();
        }
    }
}
