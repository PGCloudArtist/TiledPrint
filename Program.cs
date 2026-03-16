// =============================================================
//  TiledPrint — Standalone Windows application
//  Prints any image tiled across multiple pages at true size.
//
//  Usage:
//    1. Export your canvas from Paint.NET (PNG/JPEG/BMP/TIFF)
//    2. Run TiledPrint.exe
//    3. Open the image, set DPI / overlap, preview, print.
//
//  Build:  dotnet build -c Release
//  Run:    bin\Release\net9.0-windows\TiledPrint.exe
// =============================================================

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;

namespace TiledPrint
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    // ── Main window ───────────────────────────────────────────────────────────
    public class MainForm : Form
    {
        // Controls
        private Button        btnOpen, btnPreview, btnTiledPreview, btnPrint;
        private Label         lblDrop, lblImageInfo, lblPageEstimate;
        private Label         lblDpi, lblOverlap, lblPaperSize, lblMargin;
        private NumericUpDown nudDpi, nudOverlap, nudMargin;
        private ComboBox      cboPaper;
        private CheckBox      chkPortrait;
        private PictureBox    picThumb;
        private Panel         pnlSettings;
        // Page range
        private RadioButton   rbAll, rbSingle, rbRange;
        private NumericUpDown nudSingle, nudFrom, nudTo;
        private Label         lblTo;

        // State
        private Bitmap  _image;
        private string  _filePath;
        private static readonly string[] SupportedExts =
            { ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".tif", ".gif", ".webp" };

        public MainForm()
        {
            BuildUI();
            AllowDrop = true;
            DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            };
            DragDrop += (s, e) =>
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files?.Length > 0) LoadImage(files[0]);
            };
        }

        // ── UI construction ───────────────────────────────────────────────────
        private void BuildUI()
        {
            Text            = "Tiled Print — Real Size";
            Size            = new Size(700, 680);
            MinimumSize     = new Size(680, 640);
            FormBorderStyle = FormBorderStyle.Sizable;
            Font            = SystemFonts.MessageBoxFont;
            StartPosition   = FormStartPosition.CenterScreen;
            Icon            = CreateAppIcon();

            // ── Left panel: thumbnail + drop zone ─────────────────────────────
            var pnlLeft = new Panel { Dock = DockStyle.Left, Width = 220, Padding = new Padding(10) };

            picThumb = new PictureBox
            {
                Location    = new Point(10, 10),
                Size        = new Size(200, 180),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode    = PictureBoxSizeMode.Zoom,
                BackColor   = Color.WhiteSmoke
            };

            lblDrop = new Label
            {
                Text      = "Drop an image here\nor click Open",
                Location  = new Point(0, 0),
                Size      = picThumb.Size,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gray
            };
            picThumb.Controls.Add(lblDrop);

            lblImageInfo = new Label
            {
                Location  = new Point(10, 200),
                Size      = new Size(200, 100),
                Text      = "",
                ForeColor = Color.DarkSlateGray,
                Font      = new Font(Font.FontFamily, 8f)
            };

            btnOpen = new Button
            {
                Text     = "📂  Open Image…",
                Location = new Point(10, 308),
                Size     = new Size(200, 32)
            };
            btnOpen.Click += BtnOpen_Click;

            pnlLeft.Controls.AddRange(new Control[] { picThumb, lblImageInfo, btnOpen });

            // ── Right panel: settings ──────────────────────────────────────────
            pnlSettings = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 14, 10) };

            var grpSettings = new GroupBox
                { Text = "Print Settings", Location = new Point(10, 8), Size = new Size(430, 242), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            // DPI
            lblDpi = new Label { Text = "Image DPI:", Location = new Point(14, 28), Size = new Size(100, 20) };
            nudDpi = new NumericUpDown
                { Location = new Point(120, 26), Size = new Size(80, 22), Minimum = 1, Maximum = 9600, Value = 96 };
            nudDpi.ValueChanged += (s, e) => RefreshEstimate();
            var lblDpiNote = new Label
            {
                Text      = "Set to match your Paint.NET canvas DPI\n(Image → Resize in Paint.NET to check)",
                Location  = new Point(210, 26), Size = new Size(210, 36),
                ForeColor = Color.Gray, Font = new Font(Font.FontFamily, 7.5f)
            };

            // Paper size
            lblPaperSize = new Label { Text = "Paper size:", Location = new Point(14, 70), Size = new Size(100, 20) };
            cboPaper = new ComboBox
            {
                Location      = new Point(120, 68), Size = new Size(190, 22),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboPaper.Items.AddRange(new[] { "A4 (210×297 mm)", "A3 (297×420 mm)", "Letter (216×279 mm)", "Legal (216×356 mm)", "Tabloid (279×432 mm)" });
            cboPaper.SelectedIndex = 0;
            cboPaper.SelectedIndexChanged += (s, e) => RefreshEstimate();

            // Orientation — sits right of the wider dropdown
            chkPortrait = new CheckBox
                { Text = "Portrait", Location = new Point(322, 70), Size = new Size(100, 20), Checked = true };
            chkPortrait.CheckedChanged += (s, e) => RefreshEstimate();

            // Overlap
            lblOverlap = new Label { Text = "Overlap (mm):", Location = new Point(14, 112), Size = new Size(100, 20) };
            nudOverlap = new NumericUpDown
            {
                Location = new Point(120, 110), Size = new Size(80, 22),
                Minimum = 0, Maximum = 50, Value = 10, DecimalPlaces = 1, Increment = 0.5m
            };
            nudOverlap.ValueChanged += (s, e) => RefreshEstimate();
            var lblOverlapNote = new Label
            {
                Text      = "Overlap helps you align and tape pages.\n10 mm is a comfortable default.",
                Location  = new Point(210, 110), Size = new Size(210, 36),
                ForeColor = Color.Gray, Font = new Font(Font.FontFamily, 7.5f)
            };

            grpSettings.Controls.AddRange(new Control[]
                { lblDpi, nudDpi, lblDpiNote, lblPaperSize, cboPaper, chkPortrait,
                  lblOverlap, nudOverlap, lblOverlapNote });

            // Margin
            lblMargin = new Label { Text = "Page margin (mm):", Location = new Point(14, 154), Size = new Size(104, 20) };
            nudMargin = new NumericUpDown
            {
                Location = new Point(120, 152), Size = new Size(80, 22),
                Minimum = 0, Maximum = 30, Value = 5, DecimalPlaces = 1, Increment = 0.5m
            };
            nudMargin.ValueChanged += (s, e) => RefreshEstimate();
            var lblMarginNote = new Label
            {
                Text      = "Space between image and paper edge.\n5 mm is a safe minimum for most printers.",
                Location  = new Point(210, 152), Size = new Size(210, 36),
                ForeColor = Color.Gray, Font = new Font(Font.FontFamily, 7.5f)
            };
            grpSettings.Controls.AddRange(new Control[]
                { lblMargin, nudMargin, lblMarginNote });

            // Page estimate
            lblPageEstimate = new Label
            {
                Location  = new Point(10, 260),
                Size      = new Size(440, 52),
                Text      = "Open an image to see the page estimate.",
                ForeColor = Color.DarkBlue,
                Font      = new Font(Font.FontFamily, 10f, FontStyle.Bold),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // Instructions
            var lblInstructions = new Label
            {
                Location  = new Point(16, 320),
                Size      = new Size(430, 90),
                ForeColor = Color.DimGray,
                Font      = new Font(Font.FontFamily, 8.5f),
                Text      =
                    "Assembly guide:\n" +
                    "  • Dashed lines on each page mark the overlap zone.\n" +
                    "  • Trim the right edge of left-column pages at the dashed line.\n" +
                    "  • Trim the bottom edge of top-row pages at the dashed line.\n" +
                    "  • Align the trimmed edges and tape pages together.",
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // ── Page range group ──────────────────────────────────────────────
            var grpRange = new GroupBox
                { Text = "Print Range", Location = new Point(10, 418), Size = new Size(430, 90),
                  Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            rbAll    = new RadioButton { Text = "All pages", Location = new Point(12, 22), Size = new Size(90, 20), Checked = true };
            rbSingle = new RadioButton { Text = "Page:",     Location = new Point(108, 22), Size = new Size(58, 20) };
            rbRange  = new RadioButton { Text = "Pages:",    Location = new Point(12, 54), Size = new Size(60, 20) };

            nudSingle = new NumericUpDown
                { Location = new Point(168, 20), Size = new Size(60, 22), Minimum = 1, Maximum = 999, Value = 1, Enabled = false };
            nudFrom   = new NumericUpDown
                { Location = new Point(74, 52), Size = new Size(60, 22), Minimum = 1, Maximum = 999, Value = 1, Enabled = false };
            lblTo     = new Label { Text = "to", Location = new Point(140, 55), Size = new Size(22, 20) };
            nudTo     = new NumericUpDown
                { Location = new Point(164, 52), Size = new Size(60, 22), Minimum = 1, Maximum = 999, Value = 1, Enabled = false };

            rbAll.CheckedChanged    += (s, e) => UpdateRangeControls();
            rbSingle.CheckedChanged += (s, e) => UpdateRangeControls();
            rbRange.CheckedChanged  += (s, e) => UpdateRangeControls();

            grpRange.Controls.AddRange(new Control[]
                { rbAll, rbSingle, rbRange, nudSingle, nudFrom, lblTo, nudTo });

            // ── Docked button strip — always visible at the bottom ─────────────
            var pnlButtons = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 58,
                BackColor = SystemColors.Control,
            };
            pnlButtons.Paint += (s, pe) =>
            {
                // Draw a simple separator line at the top of the panel
                pe.Graphics.DrawLine(SystemPens.ControlDark, 0, 0, ((Panel)s).Width, 0);
            };

            btnPreview = new Button
            {
                Text     = "🔍  Page Preview",
                Location = new Point(10, 12),
                Size     = new Size(148, 34),
                Enabled  = false
            };
            btnPreview.Click += BtnPreview_Click;

            btnTiledPreview = new Button
            {
                Text     = "🗺️  Tiled Preview",
                Location = new Point(166, 12),
                Size     = new Size(148, 34),
                Enabled  = false
            };
            btnTiledPreview.Click += BtnTiledPreview_Click;

            btnPrint = new Button
            {
                Text      = "🖨️  Print…",
                Location  = new Point(322, 12),
                Size      = new Size(120, 34),
                Enabled   = false,
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnPrint.Click += BtnPrint_Click;

            pnlButtons.Controls.AddRange(new Control[] { btnPreview, btnTiledPreview, btnPrint });

            pnlSettings.Controls.AddRange(new Control[]
                { grpSettings, lblPageEstimate, lblInstructions, grpRange });
            pnlSettings.Controls.Add(pnlButtons);

            // IMPORTANT: WinForms docks in reverse collection order.
            // pnlLeft (Left) must be added LAST so it is processed FIRST,
            // leaving the remaining space for pnlSettings (Fill).
            Controls.Add(pnlSettings);
            Controls.Add(pnlLeft);
        }

        // ── Image loading ─────────────────────────────────────────────────────
        private void BtnOpen_Click(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title  = "Open Image",
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.tiff;*.tif;*.gif|All files|*.*"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                LoadImage(dlg.FileName);
        }

        private void LoadImage(string path)
        {
            if (!File.Exists(path)) return;
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (Array.IndexOf(SupportedExts, ext) < 0)
            {
                MessageBox.Show("Unsupported file type.", "Tiled Print",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _image?.Dispose();
                // Load into a fresh Bitmap so we're not locking the file
                using var tmp = new Bitmap(path);
                _image = new Bitmap(tmp);
                _filePath = path;

                // Read embedded DPI
                float dpiX = _image.HorizontalResolution;
                if (dpiX > 0 && dpiX != 96f)
                    nudDpi.Value = (decimal)Math.Round(dpiX);

                picThumb.Image = _image;
                lblDrop.Visible = false;
                btnPreview.Enabled      = true;
                btnTiledPreview.Enabled = true;
                btnPrint.Enabled        = true;

                RefreshEstimate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load image:\n" + ex.Message, "Tiled Print",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Page estimate ─────────────────────────────────────────────────────
        private (float pageW, float pageH) GetPrintableAreaInches()
        {
            // Standard printable area (paper - 0.5" margins each side)
            bool portrait = chkPortrait.Checked;
            (float w, float h) paper = cboPaper.SelectedIndex switch
            {
                1 => (11.69f, 16.54f),   // A3
                2 => (8.5f,  11.0f),     // Letter
                3 => (8.5f,  14.0f),     // Legal
                4 => (11.0f, 17.0f),     // Tabloid
                _ => (8.27f, 11.69f)     // A4 default
            };
            float printW = (portrait ? paper.w : paper.h) - 1.0f;
            float printH = (portrait ? paper.h : paper.w) - 1.0f;
            return (printW, printH);
        }

        private void RefreshEstimate()
        {
            if (_image == null) return;

            float dpi        = (float)nudDpi.Value;
            float overlapIn  = (float)nudOverlap.Value / 25.4f;
            float imgW       = _image.Width  / dpi;
            float imgH       = _image.Height / dpi;
            float imgWcm     = imgW * 2.54f;
            float imgHcm     = imgH * 2.54f;

            var (pageW, pageH) = GetPrintableAreaInches();
            float effW  = pageW - overlapIn;
            float effH  = pageH - overlapIn;
            int   cols  = Math.Max(1, (int)Math.Ceiling(imgW / effW));
            int   rows  = Math.Max(1, (int)Math.Ceiling(imgH / effH));
            int   total = cols * rows;

            lblImageInfo.Text =
                $"File: {Path.GetFileName(_filePath)}\n" +
                $"Pixels: {_image.Width} × {_image.Height}\n" +
                $"DPI: {dpi:F0}\n" +
                $"Size: {imgW:F2}\" × {imgH:F2}\"\n" +
                $"       ({imgWcm:F1} × {imgHcm:F1} cm)";

            lblPageEstimate.Text =
                $"Pages needed: {cols} col × {rows} row = {total} page{(total != 1 ? "s" : "")}   " +
                $"(overlap: {(float)nudOverlap.Value:F1} mm)";

            // Update page range spinner limits
            if (nudSingle != null)
            {
                nudSingle.Maximum = total;
                nudFrom.Maximum   = total;
                nudTo.Maximum     = total;
                if (nudTo.Value < nudFrom.Value) nudTo.Value = total;
            }
        }

        // ── Page range helpers ────────────────────────────────────────────────
        private void UpdateRangeControls()
        {
            nudSingle.Enabled = rbSingle.Checked;
            nudFrom.Enabled   = rbRange.Checked;
            nudTo.Enabled     = rbRange.Checked;
        }

        private (int from, int to) GetPageRange(int totalPages)
        {
            if (rbSingle.Checked)
            {
                int p = Math.Min((int)nudSingle.Value, totalPages);
                return (p, p);
            }
            if (rbRange.Checked)
            {
                int f = Math.Min((int)nudFrom.Value, totalPages);
                int t = Math.Min((int)nudTo.Value,   totalPages);
                if (t < f) t = f;
                return (f, t);
            }
            return (1, totalPages); // All
        }

        // ── Shared layout calculator ──────────────────────────────────────────
        /// <summary>Returns (cols, rows, pageW_inches, pageH_inches) for current settings.</summary>
        private (int cols, int rows, float pageW, float pageH) GetLayout()
        {
            if (_image == null) return (1, 1, 8.27f, 11.69f);

            float dpi       = (float)nudDpi.Value;
            float overlapIn = (float)nudOverlap.Value / 25.4f;
            int   marg      = Math.Max(1, (int)Math.Round((double)nudMargin.Value / 25.4 * 100));

            // Use A4 defaults as a baseline; actual paper/printer dpi handled in BuildPrintDocument
            (float pw, float ph) = cboPaper.SelectedIndex switch
            {
                1 => (11.69f, 16.54f),
                2 => (8.5f,   11.0f),
                3 => (8.5f,   14.0f),
                4 => (11.0f,  17.0f),
                _ => (8.27f,  11.69f)
            };
            float pageW = (chkPortrait.Checked ? pw : ph) - marg * 2 / 100f;
            float pageH = (chkPortrait.Checked ? ph : pw) - marg * 2 / 100f;

            float effW = pageW - overlapIn;
            float effH = pageH - overlapIn;
            int   cols = Math.Max(1, (int)Math.Ceiling(_image.Width  / dpi / effW));
            int   rows = Math.Max(1, (int)Math.Ceiling(_image.Height / dpi / effH));
            return (cols, rows, pageW, pageH);
        }

        // ── Print document ────────────────────────────────────────────────────
        private PrintDocument BuildPrintDocument(PrinterSettings ps = null, int fromPage = 1, int toPage = int.MaxValue)
        {
            if (_image == null) throw new InvalidOperationException("No image loaded.");

            float dpi       = (float)nudDpi.Value;
            float overlapIn = (float)nudOverlap.Value / 25.4f;
            int marginHundredths = Math.Max(1, (int)Math.Round((double)nudMargin.Value / 25.4 * 100));

            var doc = new PrintDocument();
            if (ps != null) doc.PrinterSettings = ps;
            doc.DefaultPageSettings.Margins = new Margins(
                marginHundredths, marginHundredths, marginHundredths, marginHundredths);

            int pageX = 0, pageY = 0, cols = 0, rows = 0;
            float pageW_in = 0f, pageH_in = 0f;
            int currentPage = 0; // 1-based page counter

            doc.BeginPrint += (sender, e) =>
            {
                var bounds  = doc.DefaultPageSettings.Bounds;
                pageW_in = (bounds.Width  - marginHundredths * 2) / 100f;
                pageH_in = (bounds.Height - marginHundredths * 2) / 100f;
                if (chkPortrait.Checked && pageW_in > pageH_in) (pageW_in, pageH_in) = (pageH_in, pageW_in);
                if (!chkPortrait.Checked && pageH_in > pageW_in) (pageW_in, pageH_in) = (pageH_in, pageW_in);

                float effW = pageW_in - overlapIn;
                float effH = pageH_in - overlapIn;
                cols = Math.Max(1, (int)Math.Ceiling(_image.Width  / dpi / effW));
                rows = Math.Max(1, (int)Math.Ceiling(_image.Height / dpi / effH));

                int total  = cols * rows;
                int clampedFrom = Math.Max(1, Math.Min(fromPage, total));
                // Jump straight to the first page in range
                currentPage = clampedFrom;
                pageX = (clampedFrom - 1) % cols;
                pageY = (clampedFrom - 1) / cols;
            };

            doc.PrintPage += (sender, e) =>
            {
                var g = e.Graphics;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode     = SmoothingMode.HighQuality;

                float effW = pageW_in - overlapIn;
                float effH = pageH_in - overlapIn;
                float srcX = pageX * effW * dpi;
                float srcY = pageY * effH * dpi;
                float srcW = Math.Min(pageW_in * dpi, _image.Width  - srcX);
                float srcH = Math.Min(pageH_in * dpi, _image.Height - srcY);

                if (srcW > 0 && srcH > 0)
                {
                    float dstX = e.MarginBounds.Left;
                    float dstY = e.MarginBounds.Top;
                    float dstW = srcW / dpi * 100f;
                    float dstH = srcH / dpi * 100f;

                    g.DrawImage(_image,
                        new RectangleF(dstX, dstY, dstW, dstH),
                        new RectangleF(srcX, srcY, srcW, srcH),
                        GraphicsUnit.Pixel);

                    // Alignment ticks in margin strip
                    if (overlapIn > 0)
                    {
                        using var markPen = new Pen(Color.DarkGray, 2f);
                        float olX  = overlapIn * 100f;
                        float olY  = overlapIn * 100f;
                        float tick = marginHundredths * 0.7f;
                        if (pageX < cols - 1)
                        {
                            float tx = dstX + dstW - olX;
                            g.DrawLine(markPen, tx, dstY - tick, tx, dstY);
                            g.DrawLine(markPen, tx, dstY + dstH, tx, dstY + dstH + tick);
                        }
                        if (pageY < rows - 1)
                        {
                            float ty = dstY + dstH - olY;
                            g.DrawLine(markPen, dstX - tick, ty, dstX, ty);
                            g.DrawLine(markPen, dstX + dstW, ty, dstX + dstW + tick, ty);
                        }
                    }

                    // Page label
                    using var f = new Font("Arial", 6f);
                    using var b = new SolidBrush(Color.DimGray);
                    string label = $"Page {currentPage}/{cols * rows}  [col {pageX+1}/{cols}, row {pageY+1}/{rows}]  — TiledPrint";
                    g.DrawString(label, f, b, dstX, dstY + dstH + 3f);
                }

                // Advance to next page in range
                pageX++;
                if (pageX >= cols) { pageX = 0; pageY++; }
                currentPage++;
                e.HasMorePages = pageY < rows && currentPage <= toPage;
            };

            return doc;
        }

        // ── Button handlers ───────────────────────────────────────────────────
        private void BtnPreview_Click(object sender, EventArgs e)
        {
            try
            {
                var (cols, rows, _, _) = GetLayout();
                var (from, to) = GetPageRange(cols * rows);
                using var dlg = new PrintPreviewDialog
                    { Document = BuildPrintDocument(null, from, to), WindowState = FormWindowState.Maximized };
                dlg.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Preview error:\n" + ex.Message, "Tiled Print",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnTiledPreview_Click(object sender, EventArgs e)
        {
            try
            {
                var (cols, rows, pageW, pageH) = GetLayout();
                var (from, to) = GetPageRange(cols * rows);
                var form = new TiledPreviewForm(_image, (float)nudDpi.Value,
                    (float)nudOverlap.Value / 25.4f, cols, rows, pageW, pageH, from, to);
                form.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tiled preview error:\n" + ex.Message, "Tiled Print",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            try
            {
                var (cols, rows, _, _) = GetLayout();
                var (from, to) = GetPageRange(cols * rows);
                using var dlg = new PrintDialog { AllowSomePages = false, UseEXDialog = true };
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    BuildPrintDocument(dlg.PrinterSettings, from, to).Print();
                    MessageBox.Show("Print job sent!", "Tiled Print",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Print error:\n" + ex.Message, "Tiled Print",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _image?.Dispose();
            base.OnFormClosed(e);
        }

        // ── App icon: printer with tiled pages, drawn with GDI+ ──────────────
        private static Icon CreateAppIcon()
        {
            using var bmp = new Bitmap(32, 32);
            using var g   = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Paper sheets (offset grid to suggest tiling)
            using var paperBrush = new SolidBrush(Color.White);
            using var paperPen   = new Pen(Color.FromArgb(180, 180, 200), 1f);
            // Back sheet
            g.FillRectangle(paperBrush, 13, 2, 14, 17);
            g.DrawRectangle(paperPen,   13, 2, 14, 17);
            // Front sheet
            g.FillRectangle(paperBrush, 5, 8, 14, 17);
            g.DrawRectangle(paperPen,   5, 8, 14, 17);

            // Printer body
            using var printerBrush = new SolidBrush(Color.FromArgb(60, 120, 210));
            using var printerPen   = new Pen(Color.FromArgb(30, 80, 170), 1f);
            g.FillRectangle(printerBrush, 4, 18, 24, 10);
            g.DrawRectangle(printerPen,   4, 18, 24, 10);

            // Printer slot (paper feed line)
            using var slotPen = new Pen(Color.FromArgb(30, 80, 170), 1.5f);
            g.DrawLine(slotPen, 8, 18, 24, 18);

            // Printer output tray — small white rectangle
            g.FillRectangle(paperBrush, 10, 25, 12, 5);
            g.DrawRectangle(paperPen,   10, 25, 12, 5);

            // Indicator light dot
            using var dotBrush = new SolidBrush(Color.FromArgb(100, 255, 100));
            g.FillEllipse(dotBrush, 22, 21, 4, 4);

            // Convert Bitmap → Icon
            return Icon.FromHandle(bmp.GetHicon());
        }
    }

    // ── Tiled Preview window ──────────────────────────────────────────────────
    // Shows all pages assembled as a grid, scaled to fit the window.
    // Pages outside the selected print range are dimmed.
    public class TiledPreviewForm : Form
    {
        private readonly Bitmap _image;
        private readonly float  _dpi, _overlapIn, _pageW, _pageH;
        private readonly int    _cols, _rows, _fromPage, _toPage;
        private readonly Panel  _canvas;
        private float           _zoom = 1f;

        public TiledPreviewForm(Bitmap image, float dpi, float overlapIn,
            int cols, int rows, float pageW, float pageH, int fromPage, int toPage)
        {
            _image     = image;  _dpi = dpi;  _overlapIn = overlapIn;
            _cols      = cols;   _rows = rows;
            _pageW     = pageW;  _pageH = pageH;
            _fromPage  = fromPage; _toPage = toPage;

            Text          = $"Tiled Preview — {cols} col × {rows} row";
            Size          = new Size(960, 720);
            MinimumSize   = new Size(600, 400);
            StartPosition = FormStartPosition.CenterParent;
            WindowState   = FormWindowState.Maximized;

            // Toolbar
            var toolbar    = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = SystemColors.ControlLight };
            var btnZoomIn  = new Button { Text = "  +  ", Location = new Point(10, 6), Size = new Size(40, 26) };
            var btnZoomOut = new Button { Text = "  −  ", Location = new Point(54, 6), Size = new Size(40, 26) };
            var btnFit     = new Button { Text = "Fit",  Location = new Point(98, 6), Size = new Size(40, 26) };
            int total = cols * rows;
            var lblInfo = new Label
            {
                Text      = fromPage == 1 && toPage >= total
                            ? $"All {total} pages  ({cols} col × {rows} row)"
                            : $"Pages {fromPage}–{Math.Min(toPage, total)} highlighted  |  {total} total  ({cols} col × {rows} row)",
                Location  = new Point(152, 11),
                Size      = new Size(600, 20),
                ForeColor = Color.DimGray
            };
            btnZoomIn.Click  += (s, e) => { _zoom = Math.Min(_zoom * 1.25f, 10f); UpdateCanvas(); };
            btnZoomOut.Click += (s, e) => { _zoom = Math.Max(_zoom / 1.25f, 0.05f); UpdateCanvas(); };
            btnFit.Click     += (s, e) => { FitZoom(); UpdateCanvas(); };
            toolbar.Controls.AddRange(new Control[] { btnZoomIn, btnZoomOut, btnFit, lblInfo });

            _canvas = new Panel
            {
                Dock       = DockStyle.Fill,
                BackColor  = Color.FromArgb(50, 50, 50),
                AutoScroll = true
            };
            _canvas.Paint  += Canvas_Paint;
            _canvas.Resize += (s, e) => { FitZoom(); UpdateCanvas(); };

            // WinForms docks in reverse order: Fill panel must be added FIRST,
            // then Top toolbar LAST so it is processed first and gets its space.
            Controls.Add(_canvas);
            Controls.Add(toolbar);
            Shown += (s, e) => { FitZoom(); UpdateCanvas(); };
        }

        private void UpdateCanvas()
        {
            const int GAP = 8, PAD = 20;
            float pxPerIn = 96f * _zoom;
            int w = (int)(_cols * _pageW * pxPerIn + (_cols - 1) * GAP * _zoom + PAD * 2 * _zoom) + 4;
            int h = (int)(_rows * _pageH * pxPerIn + (_rows - 1) * GAP * _zoom + PAD * 2 * _zoom) + 4;
            _canvas.AutoScrollMinSize = new Size(w, h);
            _canvas.Invalidate();
        }

        private void FitZoom()
        {
            const int GAP = 8, PAD = 20;
            float needW = _cols * _pageW * 96f + (_cols - 1) * GAP + PAD * 2f;
            float needH = _rows * _pageH * 96f + (_rows - 1) * GAP + PAD * 2f;
            float zx = (_canvas.ClientSize.Width  - 4f) / needW;
            float zy = (_canvas.ClientSize.Height - 4f) / needH;
            _zoom = Math.Max(0.05f, Math.Min(zx, zy));
        }

        private void Canvas_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TranslateTransform(_canvas.AutoScrollPosition.X, _canvas.AutoScrollPosition.Y);

            const int GAP = 8, PAD = 20;
            float pxPerIn = 96f * _zoom;
            float cellW   = _pageW * pxPerIn;
            float cellH   = _pageH * pxPerIn;
            float gapPx   = GAP * _zoom;
            float padPx   = PAD * _zoom;
            float effW    = _pageW - _overlapIn;
            float effH    = _pageH - _overlapIn;

            using var borderPen   = new Pen(Color.Silver, 1f);
            using var activePen   = new Pen(Color.FromArgb(0, 120, 212), 2f);
            using var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
            using var dimBrush    = new SolidBrush(Color.FromArgb(130, 40, 40, 40));
            using var labelFont   = new Font("Arial", Math.Max(4f, 6.5f * _zoom));

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _cols; col++)
                {
                    int  pageNum = row * _cols + col + 1;
                    bool inRange = pageNum >= _fromPage && pageNum <= _toPage;
                    float x = padPx + col * (cellW + gapPx);
                    float y = padPx + row * (cellH + gapPx);

                    // Drop shadow
                    g.FillRectangle(shadowBrush, x + 4, y + 4, cellW, cellH);

                    // White page
                    g.FillRectangle(Brushes.White, x, y, cellW, cellH);

                    // Image tile
                    float srcX = col * effW * _dpi;
                    float srcY = row * effH * _dpi;
                    float srcW = Math.Min(_pageW * _dpi, _image.Width  - srcX);
                    float srcH = Math.Min(_pageH * _dpi, _image.Height - srcY);
                    if (srcW > 0 && srcH > 0)
                    {
                        g.DrawImage(_image,
                            new RectangleF(x, y, srcW / _dpi * pxPerIn, srcH / _dpi * pxPerIn),
                            new RectangleF(srcX, srcY, srcW, srcH),
                            GraphicsUnit.Pixel);
                    }

                    // Dim if outside range
                    if (!inRange)
                        g.FillRectangle(dimBrush, x, y, cellW, cellH);

                    // Border — blue highlight for in-range pages
                    var pen = inRange ? activePen : borderPen;
                    g.DrawRectangle(pen, x, y, cellW, cellH);

                    // Page number badge
                    if (cellW > 24)
                    {
                        string lbl = $"p{pageNum}";
                        var sz  = g.MeasureString(lbl, labelFont);
                        float bx = x + 3, by = y + cellH - sz.Height - 3;
                        g.FillRectangle(new SolidBrush(Color.FromArgb(inRange ? 180 : 100, 0, 80, 160)), bx - 1, by - 1, sz.Width + 4, sz.Height + 2);
                        g.DrawString(lbl, labelFont, Brushes.White, bx, by);
                    }
                }
            }
        }
    }
}
