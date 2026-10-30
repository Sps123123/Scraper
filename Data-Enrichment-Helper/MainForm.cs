// using System;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using System.Drawing;
// using System.Drawing.Drawing2D;
// using System.IO;
// using System.Linq;
// using System.Net.Http;
// using System.Runtime.InteropServices;
// using System.Text;
// using System.Threading;
// using System.Threading.Tasks;
// using System.Windows.Forms;

// namespace Data_Enrichment_Helper;

// public class MainForm : Form
// {
//     private readonly HttpClient _httpClient;
//     private readonly WebScraperService _scraper;
//     private const int MaxConcurrentSites = 24;
//     private const int WinRadius = 14;
//     private const int Grip = 7;

//     private string _inputPath = "";
//     private string _lastFolder = "";
//     private static bool _isDarkTheme = true; 

//     private int _activeTab = 0;
//     private readonly TabState[] _tabStates = { new TabState(), new TabState(), new TabState(), new TabState() };
//     private readonly object _stateLock = new object();

//     private ModernButton btnTabPhone = null!, btnTabMap = null!, btnTabCountry = null!, btnTabDnc = null!;
//     private SectionCard panelPhone = null!, panelMap = null!, panelCountry = null!, panelDnc = null!;

//     private static Color CBg      = Color.FromArgb(15, 16, 22);
//     private static Color CBgDeep  = Color.FromArgb(10, 11, 16);
//     private static Color CCard    = Color.FromArgb(24, 26, 34);
//     private static Color CCardHi  = Color.FromArgb(30, 32, 42);
//     private static Color CBorder  = Color.FromArgb(42, 45, 59);
//     private static Color CText    = Color.FromArgb(244, 245, 247);
//     private static Color CSub     = Color.FromArgb(148, 154, 168);
//     private static Color CViolet  = Color.FromArgb(124, 92, 255);
//     private static Color CViolet2 = Color.FromArgb(168, 140, 255);
//     private static Color CCyan    = Color.FromArgb(56, 225, 198);
//     private static Color CGreen   = Color.FromArgb(52, 211, 153);
//     private static Color CRed     = Color.FromArgb(248, 113, 113);
//     private static Color CAmber   = Color.FromArgb(251, 191, 36);
//     private static Color CBlue    = Color.FromArgb(96, 165, 250);

//     private static string SettingsFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deh_ui.ini");

//     private Panel titleBar = null!;
//     private PictureBox pbIcon = null!;
//     private Label lblWindowTitle = null!;
//     private WindowButton btnWinMin = null!, btnWinMax = null!, btnWinClose = null!, btnWinTheme = null!;
//     private PictureBox pbLogo2 = null!;
//     private Label lblTitle = null!, lblSubtitle = null!;
//     private StatCard cardTotal = null!, cardFound = null!, cardMissing = null!, cardRate = null!;
//     private StatusPill statusPill = null!;
//     private Label lblProgressPct = null!;
//     private GradientProgress progressBar = null!;
//     private SectionCard logCard = null!;
//     private Label lblLogHeader = null!;
//     private ModernButton btnClearLog = null!;
//     private RichTextBox txtLog = null!;

//     private Label lblFile = null!;
//     private NumericUpDown numMaxPages = null!;
//     private ModernButton btnSelect = null!, btnStart = null!, btnCancelPhone = null!;
//     private TextBox txtKeywords = null!, txtLocations = null!;
//     private ComboBox cmbCountry = null!;
//     private CheckBox chkRunWebScrape = null!, chkRunCountryDetect = null!, chkRunDnc = null!;
//     private ModernButton btnScrapeMaps = null!, btnCancelMap = null!;
//     private ModernButton btnDetect = null!, btnCancelCountry = null!;

//     private Label lblDncFile = null!;
//     private ModernButton btnSelectDnc = null!, btnStartDnc = null!, btnCancelDnc = null!;

//     [DllImport("user32.dll")] public static extern bool ReleaseCapture();
//     [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

//     public MainForm()
//     {
//         Text = "Lime Software Logics - Data Enrichment Helper";
//         Width = 1300; Height = 950;
//         MinimumSize = new Size(1100, 740);
//         StartPosition = FormStartPosition.CenterScreen;
//         FormBorderStyle = FormBorderStyle.None;
//         DoubleBuffered = true;
//         KeyPreview = true;

//         string assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
//         try { var iconImg = TryLoadImage(assetsPath, "logo2.ico", "logo2.png"); if (iconImg != null) Icon = Icon.FromHandle(((Bitmap)iconImg).GetHicon()); } catch { }

//         LoadSettings();
//         BuildTitleBar(assetsPath);
//         BuildHero(assetsPath);
//         BuildTabs();
//         BuildStatsRow();
//         BuildProgress();
//         BuildLog();
//         ApplyTheme();

//         _httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true, AutomaticDecompression = System.Net.DecompressionMethods.All, UseCookies = false, MaxConnectionsPerServer = 32 });
//         _httpClient.Timeout = TimeSpan.FromSeconds(15);
//         _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
//         _scraper = new WebScraperService(_httpClient);

//         UpdateUIState();
//         SwitchTab(0); 
//     }

//     private class TabState
//     {
//         public bool IsRunning { get; set; }
//         public CancellationTokenSource? Cts { get; set; }
//         public StringBuilder LogBuilder { get; } = new StringBuilder();
//         public int ProgressValue { get; set; }
//         public int ProgressMax { get; set; } = 1;
//         public int Total { get; set; }
//         public int Found { get; set; }
//         public int Missing { get; set; }
//         public string StatusText { get; set; } = "Ready.";
//         public Color StatusColor { get; set; } = CCyan;
//     }

//     private void SwitchTab(int tabIndex)
//     {
//         _activeTab = tabIndex;
//         panelPhone.Visible = tabIndex == 0;
//         panelMap.Visible = tabIndex == 1;
//         panelCountry.Visible = tabIndex == 2;
//         panelDnc.Visible = tabIndex == 3;

//         var state = _tabStates[tabIndex];

//         lock (_stateLock)
//         {
//             txtLog.Text = state.LogBuilder.ToString();
//             txtLog.SelectionStart = txtLog.TextLength;
//             txtLog.ScrollToCaret();
//         }

//         if (tabIndex == 0) { cardTotal.Caption = "TOTAL ROWS"; cardFound.Caption = "FOUND"; cardMissing.Caption = "MISSING"; }
//         else if (tabIndex == 1) { cardTotal.Caption = "QUERIES"; cardFound.Caption = "EXTRACTED"; cardMissing.Caption = "FAILED"; }
//         else if (tabIndex == 2) { cardTotal.Caption = "TOTAL ROWS"; cardFound.Caption = "VALID PHONES"; cardMissing.Caption = "INVALID"; }
//         else { cardTotal.Caption = "TOTAL ROWS"; cardFound.Caption = "VALIDATED"; cardMissing.Caption = "FAILED"; }

//         cardTotal.SetValue(state.Total.ToString("N0"));
//         cardFound.SetValue(state.Found.ToString("N0"));
//         cardMissing.SetValue(state.Missing.ToString("N0"));
//         int processed = state.Found + state.Missing;
//         cardRate.SetValue(processed == 0 ? "—" : $"{100.0 * state.Found / processed:F0}%");

//         progressBar.Maximum = state.ProgressMax;
//         progressBar.Value = Math.Min(state.ProgressValue, state.ProgressMax);
//         lblProgressPct.Text = $"{(int)(100.0 * progressBar.Value / Math.Max(1, progressBar.Maximum))}%";

//         statusPill.Set(state.StatusText, state.StatusColor);

//         UpdateTabColors();
//         UpdateUIState();
//     }

//     private void UpdateUIState()
//     {
//         btnSelect.Enabled = !_tabStates[0].IsRunning;
//         btnStart.Enabled = !_tabStates[0].IsRunning && !string.IsNullOrEmpty(_inputPath);
//         btnCancelPhone.Enabled = _tabStates[0].IsRunning;
//         numMaxPages.Enabled = !_tabStates[0].IsRunning;

//         btnScrapeMaps.Enabled = !_tabStates[1].IsRunning;
//         btnCancelMap.Enabled = _tabStates[1].IsRunning;
//         chkRunWebScrape.Enabled = !_tabStates[1].IsRunning;
//         chkRunCountryDetect.Enabled = !_tabStates[1].IsRunning;
//         chkRunDnc.Enabled = !_tabStates[1].IsRunning;
        
//         btnDetect.Enabled = !_tabStates[2].IsRunning;
//         btnCancelCountry.Enabled = _tabStates[2].IsRunning;

//         btnSelectDnc.Enabled = !_tabStates[3].IsRunning;
//         btnStartDnc.Enabled = !_tabStates[3].IsRunning && !string.IsNullOrEmpty(lblDncFile.Text) && lblDncFile.Text != "no file selected";
//         btnCancelDnc.Enabled = _tabStates[3].IsRunning;
//     }

//     private void LogLine(int tabIndex, string text, Color color)
//     {
//         var state = _tabStates[tabIndex];
//         string stamped = $"[{DateTime.Now:HH:mm:ss}]  {text}";

//         lock (_stateLock) { state.LogBuilder.AppendLine(stamped); }

//         if (_activeTab == tabIndex)
//         {
//             if (txtLog.InvokeRequired) txtLog.BeginInvoke(new Action(() => AppendToLogUI(stamped, color)));
//             else AppendToLogUI(stamped, color);
//         }
//     }

//     private void AppendToLogUI(string text, Color color)
//     {
//         int start = txtLog.TextLength;
//         txtLog.AppendText(text + Environment.NewLine);
//         txtLog.Select(start, text.Length);
//         txtLog.SelectionColor = color;
//         txtLog.SelectionStart = txtLog.TextLength;
//         txtLog.SelectionLength = 0;
//         txtLog.SelectionColor = CSub;
//         txtLog.ScrollToCaret();
//     }

//     private void SetStats(int tabIndex, int total, int found, int missing)
//     {
//         var state = _tabStates[tabIndex];
//         state.Total = total; state.Found = found; state.Missing = missing;

//         if (_activeTab == tabIndex)
//         {
//             if (InvokeRequired) BeginInvoke(new Action(() => SetStats(tabIndex, total, found, missing)));
//             else
//             {
//                 cardTotal.SetValue(total.ToString("N0"));
//                 cardFound.SetValue(found.ToString("N0"));
//                 cardMissing.SetValue(missing.ToString("N0"));
//                 int processed = found + missing;
//                 cardRate.SetValue(processed == 0 ? "—" : $"{100.0 * found / processed:F0}%");
//             }
//         }
//     }

//     private void SetProgress(int tabIndex, int value, int max)
//     {
//         var state = _tabStates[tabIndex];
//         state.ProgressValue = value; state.ProgressMax = Math.Max(1, max);

//         if (_activeTab == tabIndex)
//         {
//             if (InvokeRequired) BeginInvoke(new Action(() => SetProgress(tabIndex, value, max)));
//             else
//             {
//                 progressBar.Maximum = state.ProgressMax;
//                 progressBar.Value = Math.Min(value, state.ProgressMax);
//                 lblProgressPct.Text = $"{(int)(100.0 * progressBar.Value / progressBar.Maximum)}%";
//             }
//         }
//     }

//     private void SetStatus(int tabIndex, string text, Color color)
//     {
//         var state = _tabStates[tabIndex];
//         state.StatusText = text; state.StatusColor = color;

//         if (_activeTab == tabIndex)
//         {
//             if (InvokeRequired) BeginInvoke(new Action(() => SetStatus(tabIndex, text, color)));
//             else statusPill.Set(text, color);
//         }
//     }

//     private void ApplyTheme()
//     {
//         if (_isDarkTheme)
//         {
//             CBg = Color.FromArgb(15, 16, 22); CBgDeep = Color.FromArgb(10, 11, 16); CCard = Color.FromArgb(24, 26, 34);
//             CCardHi = Color.FromArgb(30, 32, 42); CBorder = Color.FromArgb(42, 45, 59); CText = Color.FromArgb(244, 245, 247);
//             CSub = Color.FromArgb(148, 154, 168); CCyan = Color.FromArgb(56, 225, 198);
//             if (txtLog != null) txtLog.BackColor = CBgDeep;
//         }
//         else
//         {
//             CBg = Color.FromArgb(245, 245, 247); CBgDeep = Color.FromArgb(235, 235, 240); CCard = Color.FromArgb(255, 255, 255);
//             CCardHi = Color.FromArgb(249, 249, 251); CBorder = Color.FromArgb(229, 229, 234); CText = Color.FromArgb(29, 29, 31);
//             CSub = Color.FromArgb(134, 134, 139); CCyan = Color.FromArgb(0, 184, 212);
//             if (txtLog != null) txtLog.BackColor = Color.FromArgb(250, 250, 250);
//         }

//         BackColor = CBg; ForeColor = CText;
//         if (txtLog != null) txtLog.ForeColor = CText;
//         if (lblWindowTitle != null) lblWindowTitle.ForeColor = CText;
//         if (lblSubtitle != null) lblSubtitle.ForeColor = CSub;
//         if (lblFile != null) lblFile.ForeColor = CSub;
//         if (lblDncFile != null) lblDncFile.ForeColor = CSub;
//         if (lblLogHeader != null) lblLogHeader.ForeColor = CSub;
//         if (lblProgressPct != null) lblProgressPct.ForeColor = CSub;
        
//         if (numMaxPages != null) { numMaxPages.BackColor = CCardHi; numMaxPages.ForeColor = CText; }
//         if (chkRunWebScrape != null) chkRunWebScrape.ForeColor = CText;
//         if (chkRunCountryDetect != null) chkRunCountryDetect.ForeColor = CText;
//         if (chkRunDnc != null) chkRunDnc.ForeColor = CText;

//         UpdateTabColors();
//         Invalidate(true);
//         foreach (Control c in Controls) c.Invalidate(true);
//     }

//     private void BuildTitleBar(string assetsPath)
//     {
//         titleBar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.Transparent };
//         titleBar.MouseDown += Drag;
//         titleBar.DoubleClick += (s, e) => ToggleMaximize();
//         Controls.Add(titleBar);

//         pbIcon = new PictureBox { Left = 14, Top = 9, Width = 28, Height = 28, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent, Image = TryLoadImage(assetsPath, "logo1.png", "logo.png") };
//         pbIcon.MouseDown += Drag;
//         titleBar.Controls.Add(pbIcon);

//         lblWindowTitle = new Label { Text = "Data Enrichment Helper", Left = 50, Top = 13, AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = CText, BackColor = Color.Transparent };
//         lblWindowTitle.MouseDown += Drag;
//         titleBar.Controls.Add(lblWindowTitle);

//         btnWinClose = new WindowButton { Glyph = "✕", IsClose = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(titleBar.Width - 44, 8) };
//         btnWinClose.Click += (s, e) => { if (_tabStates.Any(x => x.IsRunning) && MessageBox.Show("Tasks running. Close?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.No) return; Application.Exit(); };
//         titleBar.Controls.Add(btnWinClose);

//         btnWinMax = new WindowButton { Glyph = "▢", Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(titleBar.Width - 88, 8) };
//         btnWinMax.Click += (s, e) => ToggleMaximize();
//         titleBar.Controls.Add(btnWinMax);

//         btnWinMin = new WindowButton { Glyph = "—", Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(titleBar.Width - 132, 8) };
//         btnWinMin.Click += (s, e) => WindowState = FormWindowState.Minimized;
//         titleBar.Controls.Add(btnWinMin);

//         btnWinTheme = new WindowButton { Glyph = "☀", Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(titleBar.Width - 176, 8) };
//         btnWinTheme.Click += (s, e) => { _isDarkTheme = !_isDarkTheme; btnWinTheme.Glyph = _isDarkTheme ? "☀" : "🌙"; ApplyTheme(); };
//         titleBar.Controls.Add(btnWinTheme);

//         titleBar.Resize += (s, e) => { btnWinClose.Left = titleBar.Width - 44; btnWinMax.Left = titleBar.Width - 88; btnWinMin.Left = titleBar.Width - 132; btnWinTheme.Left = titleBar.Width - 176; };
//     }

//     private void BuildHero(string assetsPath)
//     {
//         lblTitle = new Label { Text = "Data Enrichment Helper", Left = 28, Top = 62, Width = 600, Height = 34, Font = new Font("Segoe UI", 20f, FontStyle.Bold), ForeColor = CText, BackColor = Color.Transparent };
//         lblTitle.Paint += (s, e) => { var r = lblTitle.ClientRectangle; using var br = new LinearGradientBrush(r, CText, CViolet2, 0f); TextRenderer.DrawText(e.Graphics, lblTitle.Text, lblTitle.Font, r, CText, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix); };
//         Controls.Add(lblTitle);

//         lblSubtitle = new Label { Text = "By NIZAMI ⓒ   ·   Website Phone & Email Scraper   ·   Country Detector   ·   Map Scraper   ·   DNC Validator", Left = 30, Top = 100, Width = 800, Height = 20, Font = new Font("Segoe UI", 9.5f), ForeColor = CSub, BackColor = Color.Transparent };
//         Controls.Add(lblSubtitle);

//         pbLogo2 = new PictureBox { Left = ClientSize.Width - 110, Top = 45, Width = 70, Height = 70, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right };
//         pbLogo2.Image = TryLoadImage(assetsPath, "logo2.png", "logo2.ico");
//         if (pbLogo2.Image != null) { var bmp = new Bitmap(pbLogo2.Image); bmp.MakeTransparent(Color.White); pbLogo2.Image = bmp; using var path = new GraphicsPath(); path.AddEllipse(0, 0, pbLogo2.Width, pbLogo2.Height); pbLogo2.Region = new Region(path); }
//         Controls.Add(pbLogo2);
//     }

//     private void BuildTabs()
//     {
//         int y = 140, w = 150;
        
//         btnTabPhone = new ModernButton { Text = "✉  Email & Phone", Location = new Point(28, y), Size = new Size(w, 36), ColorA = CViolet, ColorB = CViolet2, Radius = 8 };
//         btnTabPhone.Click += (s, e) => SwitchTab(0);
        
//         btnTabMap = new ModernButton { Text = "🗺  Map Scraper", Location = new Point(28 + w + 10, y), Size = new Size(w, 36), ColorA = CViolet, ColorB = CViolet2, Radius = 8 };
//         btnTabMap.Click += (s, e) => SwitchTab(1);
        
//         btnTabCountry = new ModernButton { Text = "🌍  Country Detector", Location = new Point(28 + (w + 10) * 2, y), Size = new Size(w, 36), ColorA = CViolet, ColorB = CViolet2, Radius = 8 };
//         btnTabCountry.Click += (s, e) => SwitchTab(2);

//         btnTabDnc = new ModernButton { Text = "📞  DNC Validator", Location = new Point(28 + (w + 10) * 3, y), Size = new Size(w, 36), ColorA = CViolet, ColorB = CViolet2, Radius = 8 };
//         btnTabDnc.Click += (s, e) => SwitchTab(3);

//         Controls.Add(btnTabPhone); Controls.Add(btnTabMap); Controls.Add(btnTabCountry); Controls.Add(btnTabDnc);

//         // --- Panel 1: Email Scraper ---
//         panelPhone = new SectionCard { Left = 28, Top = 190, Width = ClientSize.Width - 56, Height = 150, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
//         Controls.Add(panelPhone);

//         btnSelect = new ModernButton { Text = "Select CSV…", ColorA = CBlue, ColorB = Color.FromArgb(130, 180, 255), Size = new Size(150, 36), Location = new Point(20, 20) };
//         btnSelect.Click += BtnSelect_Click;
//         panelPhone.Controls.Add(btnSelect);

//         lblFile = new Label { Text = "no file selected", Left = 184, Top = 28, Width = panelPhone.Width - 560, Height = 20, AutoEllipsis = true, Font = new Font("Segoe UI", 9.5f), ForeColor = CSub, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
//         panelPhone.Controls.Add(lblFile);

//         var lblPages = new Label { Text = "Max pages / site", AutoSize = true, Font = new Font("Segoe UI", 9f), ForeColor = CSub, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right };
//         panelPhone.Controls.Add(lblPages);

//         numMaxPages = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 15, Width = 70, Height = 26, BorderStyle = BorderStyle.None, BackColor = CCardHi, ForeColor = CText, Font = new Font("Segoe UI", 9.5f), Anchor = AnchorStyles.Top | AnchorStyles.Right };
//         numMaxPages.Location = new Point(panelPhone.Width - 20 - numMaxPages.Width, 18);
//         lblPages.Location = new Point(numMaxPages.Left - lblPages.Width - 10, 25);
//         panelPhone.Controls.Add(numMaxPages);
//         panelPhone.Resize += (s, e) => { numMaxPages.Left = panelPhone.Width - 20 - numMaxPages.Width; lblPages.Left = numMaxPages.Left - lblPages.Width - 10; };

//         btnStart = new ModernButton { Text = "▶   Start Scraping", ColorA = Color.FromArgb(16, 163, 110), ColorB = CGreen, Size = new Size(180, 38), Location = new Point(20, 80) };
//         btnStart.Click += BtnStart_Click;
//         panelPhone.Controls.Add(btnStart);

//         btnCancelPhone = new ModernButton { Text = "■   Cancel", ColorA = Color.FromArgb(200, 70, 70), ColorB = CRed, Size = new Size(130, 38), Location = new Point(212, 80) };
//         btnCancelPhone.Click += (s, e) => _tabStates[0].Cts?.Cancel();
//         panelPhone.Controls.Add(btnCancelPhone);

//         // --- Panel 2: Map Scraper ---
//         panelMap = new SectionCard { Left = 28, Top = 190, Width = ClientSize.Width - 56, Height = 150, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
//         Controls.Add(panelMap);

//         var lblKeywords = new Label { Text = "Keywords (1 per line):", Left = 20, Top = 15, AutoSize = true, Font = new Font("Segoe UI", 8f), ForeColor = CSub, BackColor = Color.Transparent };
//         txtKeywords = new TextBox { Left = 20, Top = 35, Width = 250, Height = 80, Multiline = true, ScrollBars = ScrollBars.Vertical, Text = "Roofing contractor\r\nDentist", BorderStyle = BorderStyle.None, BackColor = CCardHi, ForeColor = CText, Font = new Font("Segoe UI", 9.5f) };

//         var lblCountry = new Label { Text = "Country:", Left = 290, Top = 15, AutoSize = true, Font = new Font("Segoe UI", 8f), ForeColor = CSub, BackColor = Color.Transparent };
//         cmbCountry = new ComboBox { Left = 290, Top = 35, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CCardHi, ForeColor = CText, FlatStyle = FlatStyle.Flat };
//         cmbCountry.Items.AddRange(new object[] { "United States", "Canada" });
//         cmbCountry.SelectedIndex = 0;
//         cmbCountry.SelectedIndexChanged += (s, e) => UpdateLocations();

//         var lblLocations = new Label { Text = "Locations (1 per line):", Left = 430, Top = 15, AutoSize = true, Font = new Font("Segoe UI", 8f), ForeColor = CSub, BackColor = Color.Transparent };
//         txtLocations = new TextBox { Left = 430, Top = 35, Width = 250, Height = 80, Multiline = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None, BackColor = CCardHi, ForeColor = CText, Font = new Font("Segoe UI", 9.5f) };

//         btnScrapeMaps = new ModernButton { Text = "🗺️   Run Bulk Scraper", ColorA = Color.FromArgb(234, 88, 12), ColorB = Color.FromArgb(249, 115, 22), Size = new Size(160, 38), Location = new Point(700, 35) };
//         btnScrapeMaps.Click += BtnScrapeMaps_Click;

//         btnCancelMap = new ModernButton { Text = "■   Cancel", ColorA = Color.FromArgb(200, 70, 70), ColorB = CRed, Size = new Size(130, 38), Location = new Point(700, 80) };
//         btnCancelMap.Click += (s, e) => _tabStates[1].Cts?.Cancel();

//         // ✅ THREE CHECKBOXES ON A SINGLE ROW
//         chkRunWebScrape    = new CheckBox { Text = "Run Phone & Email Scraper", Left = 20,  Top = 125, AutoSize = true, ForeColor = CText, BackColor = Color.Transparent, Checked = true };
//         chkRunCountryDetect = new CheckBox { Text = "Run Country Detector",     Left = 230, Top = 125, AutoSize = true, ForeColor = CText, BackColor = Color.Transparent, Checked = true };
//         chkRunDnc          = new CheckBox { Text = "Run DNC Validator",         Left = 420, Top = 125, AutoSize = true, ForeColor = CText, BackColor = Color.Transparent, Checked = false };

//         panelMap.Controls.AddRange(new Control[] { lblKeywords, txtKeywords, lblCountry, cmbCountry, lblLocations, txtLocations, btnScrapeMaps, btnCancelMap, chkRunWebScrape, chkRunCountryDetect, chkRunDnc });
//         UpdateLocations();

//         // --- Panel 3: Country Detector ---
//         panelCountry = new SectionCard { Left = 28, Top = 190, Width = ClientSize.Width - 56, Height = 150, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
//         Controls.Add(panelCountry);

//         var lblCountryDesc = new Label { Text = "Select a CSV containing a 'Phone Number' column. The app detects the country using the prefix\n(e.g., +91 = India, +92 = Pakistan, +1 = US/Canada) and adds a 'Detected Country' column.", Left = 20, Top = 20, Width = 800, Height = 40, Font = new Font("Segoe UI", 9.5f), ForeColor = CSub, BackColor = Color.Transparent };
//         panelCountry.Controls.Add(lblCountryDesc);

//         btnDetect = new ModernButton { Text = "🌍   Detect & Save", ColorA = CViolet, ColorB = CViolet2, Size = new Size(180, 38), Location = new Point(20, 80) };
//         btnDetect.Click += BtnDetect_Click;
//         panelCountry.Controls.Add(btnDetect);

//         btnCancelCountry = new ModernButton { Text = "■   Cancel", ColorA = Color.FromArgb(200, 70, 70), ColorB = CRed, Size = new Size(130, 38), Location = new Point(212, 80) };
//         btnCancelCountry.Click += (s, e) => _tabStates[2].Cts?.Cancel();
//         panelCountry.Controls.Add(btnCancelCountry);

//         // --- Panel 4: DNC Validator (standalone) ---
//         panelDnc = new SectionCard { Left = 28, Top = 190, Width = ClientSize.Width - 56, Height = 150, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
//         Controls.Add(panelDnc);

//         btnSelectDnc = new ModernButton { Text = "Select CSV…", ColorA = CBlue, ColorB = Color.FromArgb(130, 180, 255), Size = new Size(150, 36), Location = new Point(20, 20) };
//         btnSelectDnc.Click += BtnSelectDnc_Click;
//         panelDnc.Controls.Add(btnSelectDnc);

//         lblDncFile = new Label { Text = "no file selected", Left = 184, Top = 28, Width = 600, Height = 20, AutoEllipsis = true, Font = new Font("Segoe UI", 9.5f), ForeColor = CSub, BackColor = Color.Transparent };
//         panelDnc.Controls.Add(lblDncFile);

//         btnStartDnc = new ModernButton { Text = "▶   Start DNC Validation", ColorA = Color.FromArgb(16, 163, 110), ColorB = CGreen, Size = new Size(180, 38), Location = new Point(20, 80) };
//         btnStartDnc.Click += BtnStartDnc_Click;
//         panelDnc.Controls.Add(btnStartDnc);

//         btnCancelDnc = new ModernButton { Text = "■   Cancel", ColorA = Color.FromArgb(200, 70, 70), ColorB = CRed, Size = new Size(130, 38), Location = new Point(212, 80) };
//         btnCancelDnc.Click += (s, e) => _tabStates[3].Cts?.Cancel();
//         panelDnc.Controls.Add(btnCancelDnc);
//     }

//     private void UpdateTabColors()
//     {
//         if (btnTabPhone == null) return;
//         btnTabPhone.ColorA = _activeTab == 0 ? CViolet : ( _isDarkTheme ? CCardHi : Color.FromArgb(220, 220, 225));
//         btnTabPhone.ColorB = _activeTab == 0 ? CViolet2 : ( _isDarkTheme ? Color.FromArgb(52, 55, 70) : Color.FromArgb(240, 240, 245));
        
//         btnTabMap.ColorA = _activeTab == 1 ? CViolet : ( _isDarkTheme ? CCardHi : Color.FromArgb(220, 220, 225));
//         btnTabMap.ColorB = _activeTab == 1 ? CViolet2 : ( _isDarkTheme ? Color.FromArgb(52, 55, 70) : Color.FromArgb(240, 240, 245));
        
//         btnTabCountry.ColorA = _activeTab == 2 ? CViolet : ( _isDarkTheme ? CCardHi : Color.FromArgb(220, 220, 225));
//         btnTabCountry.ColorB = _activeTab == 2 ? CViolet2 : ( _isDarkTheme ? Color.FromArgb(52, 55, 70) : Color.FromArgb(240, 240, 245));

//         btnTabDnc.ColorA = _activeTab == 3 ? CViolet : ( _isDarkTheme ? CCardHi : Color.FromArgb(220, 220, 225));
//         btnTabDnc.ColorB = _activeTab == 3 ? CViolet2 : ( _isDarkTheme ? Color.FromArgb(52, 55, 70) : Color.FromArgb(240, 240, 245));

//         btnTabPhone.Invalidate(); btnTabMap.Invalidate(); btnTabCountry.Invalidate(); btnTabDnc.Invalidate();
//     }

//     private void UpdateLocations()
//     {
//         if (cmbCountry.SelectedIndex == 0) txtLocations.Text = string.Join(Environment.NewLine, LocationData.GetUSLocations());
//         else txtLocations.Text = string.Join(Environment.NewLine, LocationData.GetCanadaLocations());
//     }

//     private void BuildStatsRow()
//     {
//         int y = 355, h = 100, gap = 16, x0 = 28;
//         int w = (ClientSize.Width - 2 * x0 - 3 * gap) / 4;
//         cardTotal  = new StatCard { Caption = "TOTAL ROWS",   Glyph = "◈", Accent = CViolet, Location = new Point(x0, y), Size = new Size(w, h) };
//         cardFound  = new StatCard { Caption = "FOUND",        Glyph = "✓", Accent = CGreen,  Location = new Point(x0 + (w + gap), y), Size = new Size(w, h) };
//         cardMissing = new StatCard { Caption = "MISSING",     Glyph = "✗", Accent = CRed,    Location = new Point(x0 + 2 * (w + gap), y), Size = new Size(w, h) };
//         cardRate   = new StatCard { Caption = "SUCCESS RATE", Glyph = "%", Accent = CBlue,   Location = new Point(x0 + 3 * (w + gap), y), Size = new Size(w, h) };
//         Controls.Add(cardTotal); Controls.Add(cardFound); Controls.Add(cardMissing); Controls.Add(cardRate);
//     }

//     private void BuildProgress()
//     {
//         statusPill = new StatusPill { Location = new Point(28, 475), Text = "Ready." };
//         Controls.Add(statusPill);

//         lblProgressPct = new Label { Text = "0%", Left = ClientSize.Width - 78, Top = 475, Width = 50, Height = 20, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = CSub, TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right };
//         Controls.Add(lblProgressPct);

//         progressBar = new GradientProgress { Left = 28, Top = 503, Width = ClientSize.Width - 56, Height = 12, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
//         Controls.Add(progressBar);
//     }

//     private void BuildLog()
//     {
//         logCard = new SectionCard { Left = 28, Top = 530, Width = ClientSize.Width - 56, Height = ClientSize.Height - 530 - 18, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
//         Controls.Add(logCard);

//         lblLogHeader = new Label { Text = "ACTIVITY LOG", Left = 20, Top = 12, Width = 300, Height = 16, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = CSub, BackColor = Color.Transparent };
//         logCard.Controls.Add(lblLogHeader);

//         btnClearLog = new ModernButton { Text = "Clear", ColorA = CCardHi, ColorB = Color.FromArgb(52, 55, 70), TextOnly = true, Size = new Size(64, 24) };
//         btnClearLog.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
//         btnClearLog.Click += (s, e) => { lock(_stateLock) { _tabStates[_activeTab].LogBuilder.Clear(); } txtLog.Clear(); };
//         logCard.Controls.Add(btnClearLog);
//         logCard.Resize += (s, e) => btnClearLog.Location = new Point(logCard.Width - 20 - btnClearLog.Width, 10);

//         txtLog = new RichTextBox { Left = 20, Top = 40, Width = logCard.Width - 40, Height = logCard.Height - 58, Multiline = true, ReadOnly = true, ScrollBars = RichTextBoxScrollBars.Vertical, Font = new Font("Consolas", 9.5f), BorderStyle = BorderStyle.None, BackColor = CBgDeep, ForeColor = CText, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
//         logCard.Controls.Add(txtLog);
//         logCard.Resize += (s, e) => { txtLog.Width = logCard.Width - 40; txtLog.Height = logCard.Height - 58; };
//     }

//     private void BtnSelect_Click(object? sender, EventArgs e)
//     {
//         using var ofd = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", InitialDirectory = Directory.Exists(_lastFolder) ? _lastFolder : "" };
//         if (ofd.ShowDialog() == DialogResult.OK)
//         {
//             _inputPath = ofd.FileName; _lastFolder = Path.GetDirectoryName(_inputPath) ?? "";
//             SaveSettings();
//             string preview = Path.GetFileName(_inputPath);
//             try { int lines = File.ReadLines(_inputPath).Take(500000).Count(); preview += $"    ·    {Math.Max(0, lines - 1):N0} rows"; } catch { }
//             lblFile.Text = preview; lblFile.ForeColor = CText; btnStart.Enabled = true;
//         }
//     }

//     private async void BtnStart_Click(object? sender, EventArgs e)
//     {
//         if (_tabStates[0].IsRunning) return;
//         if (string.IsNullOrEmpty(_inputPath) || !File.Exists(_inputPath)) { MessageBox.Show("Please select a CSV file first."); return; }
//         using var sfd = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = Path.GetFileNameWithoutExtension(_inputPath) + "_with_contacts.csv", InitialDirectory = Directory.Exists(_lastFolder) ? _lastFolder : "" };
//         if (sfd.ShowDialog() != DialogResult.OK) return;

//         _tabStates[0].IsRunning = true; _tabStates[0].Cts = new CancellationTokenSource(); UpdateUIState();
//         SetProgress(0, 0, 1); SetStats(0, 0, 0, 0); SetStatus(0, "Working...", CAmber);

//         try
//         {
//             await ProcessCsvAsync(0, _inputPath, sfd.FileName, (int)numMaxPages.Value);
//             MessageBox.Show("Done!\nOutput saved to:\n" + sfd.FileName, "Success");
//             SetStatus(0, $"Done.", CGreen);
//         }
//         catch (OperationCanceledException) { LogLine(0, "═══ CANCELLED BY USER ═══", CAmber); SetStatus(0, "Cancelled.", CAmber); }
//         catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "Error"); SetStatus(0, "Error.", CRed); }
//         finally { _tabStates[0].IsRunning = false; _tabStates[0].Cts?.Dispose(); _tabStates[0].Cts = null; UpdateUIState(); }
//     }

//     private async void BtnScrapeMaps_Click(object? sender, EventArgs e)
//     {
//         if (_tabStates[1].IsRunning) return;

//         var keywords = txtKeywords.Lines.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()).ToList();
//         var locations = txtLocations.Lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToList();
//         string country = cmbCountry.SelectedItem?.ToString() ?? "United States";

//         if (keywords.Count == 0 || locations.Count == 0) { MessageBox.Show("Please enter both keywords and locations."); return; }

//         var queries = new List<string>();
//         foreach (var kw in keywords) { foreach (var loc in locations) { queries.Add($"{kw} in {loc}, {country}"); } }

//         using var sfd = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "maps_bulk_results.csv" };
//         if (sfd.ShowDialog() != DialogResult.OK) return;

//         _tabStates[1].IsRunning = true; _tabStates[1].Cts = new CancellationTokenSource(); UpdateUIState();
//         SetStats(1, queries.Count, 0, 0); SetStatus(1, "Working...", CAmber);

//         try
//         {
//             var mapScraper = new MapScraperService();
//             var mapCts = _tabStates[1].Cts ?? throw new InvalidOperationException("Map scraper started without a CancellationTokenSource.");
            
//             // ═══ STEP 1: MAP SCRAPER ═══
//             await mapScraper.ScrapeMultipleAsync(queries, sfd.FileName, 
//                 (msg, color) => LogLine(1, msg, color), 
//                 (done, total, extracted, failedQueries) => 
//                 {
//                     SetProgress(1, done, total);
//                     SetStats(1, total, extracted, failedQueries); 
//                 }, 
//                 mapCts.Token);

//             string currentCsv = sfd.FileName;

//             // ═══ STEP 2: PHONE & EMAIL SCRAPER (optional) ═══
//             if (chkRunWebScrape.Checked)
//             {
//                 LogLine(1, "═══ STARTING PHONE & EMAIL SCRAPER ═══", CViolet2);
//                 string webScrapedCsv = Path.Combine(Path.GetDirectoryName(currentCsv) ?? "", Path.GetFileNameWithoutExtension(currentCsv) + "_with_contacts.csv");
                
//                 SetProgress(1, 0, 1); SetStats(1, 0, 0, 0); SetStatus(1, "Scraping Phone/Email...", CAmber);
//                 await ProcessCsvAsync(1, currentCsv, webScrapedCsv, 15);
//                 currentCsv = webScrapedCsv;
//             }

//             // ═══ STEP 3: COUNTRY DETECTOR (optional) ═══
//             if (chkRunCountryDetect.Checked)
//             {
//                 LogLine(1, "═══ STARTING COUNTRY DETECTOR ═══", CViolet2);
//                 string analyzedCsv = Path.Combine(Path.GetDirectoryName(currentCsv) ?? "", Path.GetFileNameWithoutExtension(currentCsv) + "_analyzed.csv");
                
//                 SetProgress(1, 0, 1); SetStats(1, 0, 0, 0); SetStatus(1, "Detecting Countries...", CAmber);
//                 await Task.Run(() => ProcessCountryAnalysis(1, currentCsv, analyzedCsv, mapCts.Token));
//                 currentCsv = analyzedCsv;
//             }

//             // ═══ STEP 4: DNC VALIDATOR (optional) ═══
//             if (chkRunDnc.Checked)
//             {
//                 LogLine(1, "═══ STARTING DNC VALIDATOR ═══", CViolet2);
//                 string dncCsv = Path.Combine(Path.GetDirectoryName(currentCsv) ?? "", Path.GetFileNameWithoutExtension(currentCsv) + "_dnc.csv");
                
//                 SetProgress(1, 0, 1); SetStats(1, 0, 0, 0); SetStatus(1, "Running DNC Validator...", CAmber);
                
//                 var dncService = new DncValidatorService();
//                 await dncService.RunAsync(currentCsv, dncCsv,
//                     (msg, color) => LogLine(1, msg, color),
//                     (done, total) => 
//                     {
//                         SetProgress(1, done, total);
//                         SetStats(1, total, done, 0);
//                     },
//                     mapCts.Token);

//                 currentCsv = dncCsv;
//             }

//             MessageBox.Show($"All Selected Tasks Complete!\nFinal output saved to:\n" + currentCsv, "Success");
//             SetStatus(1, "Done.", CGreen);
//         }
//         catch (OperationCanceledException) { LogLine(1, "═══ CANCELLED BY USER ═══", CAmber); SetStatus(1, "Cancelled.", CAmber); }
//         catch (Exception ex) { MessageBox.Show("Map Scraper Error: " + ex.Message, "Error"); SetStatus(1, "Error.", CRed); }
//         finally { _tabStates[1].IsRunning = false; _tabStates[1].Cts?.Dispose(); _tabStates[1].Cts = null; UpdateUIState(); }
//     }

//     private async void BtnDetect_Click(object? sender, EventArgs e)
//     {
//         if (_tabStates[2].IsRunning) return;

//         using var ofd = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", InitialDirectory = Directory.Exists(_lastFolder) ? _lastFolder : "" };
//         if (ofd.ShowDialog() != DialogResult.OK) return;
//         using var sfd = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = Path.GetFileNameWithoutExtension(ofd.FileName) + "_analyzed.csv", InitialDirectory = Directory.Exists(_lastFolder) ? _lastFolder : "" };
//         if (sfd.ShowDialog() != DialogResult.OK) return;

//         _tabStates[2].IsRunning = true; _tabStates[2].Cts = new CancellationTokenSource(); UpdateUIState();
//         SetStatus(2, "Working...", CAmber);

//         try
//         {
//             LogLine(2, "═══ COUNTRY DETECTOR STARTED ═══", CViolet2);
//             var countryCts = _tabStates[2].Cts ?? throw new InvalidOperationException("Country detector started without a CancellationTokenSource.");
//             await Task.Run(() => ProcessCountryAnalysis(2, ofd.FileName, sfd.FileName, countryCts.Token));
//             LogLine(2, $"✔ Saved: {sfd.FileName}", CGreen);
//             MessageBox.Show("Analysis Complete!\nOutput saved to:\n" + sfd.FileName, "Success");
//             SetStatus(2, "Done.", CGreen);
//         }
//         catch (OperationCanceledException) { LogLine(2, "═══ CANCELLED BY USER ═══", CAmber); SetStatus(2, "Cancelled.", CAmber); }
//         catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "Error"); SetStatus(2, "Error.", CRed); }
//         finally { _tabStates[2].IsRunning = false; _tabStates[2].Cts?.Dispose(); _tabStates[2].Cts = null; UpdateUIState(); }
//     }

//     private void BtnSelectDnc_Click(object? sender, EventArgs e)
//     {
//         using var ofd = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", InitialDirectory = Directory.Exists(_lastFolder) ? _lastFolder : "" };
//         if (ofd.ShowDialog() == DialogResult.OK)
//         {
//             lblDncFile.Text = ofd.FileName; 
//             lblDncFile.ForeColor = CText; 
//             btnStartDnc.Enabled = true;
//         }
//     }

//     private async void BtnStartDnc_Click(object? sender, EventArgs e)
//     {
//         if (_tabStates[3].IsRunning) return;
//         if (string.IsNullOrEmpty(lblDncFile.Text) || lblDncFile.Text == "no file selected") { MessageBox.Show("Please select a CSV file first."); return; }
        
//         using var sfd = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = Path.GetFileNameWithoutExtension(lblDncFile.Text) + "_dnc_checked.csv" };
//         if (sfd.ShowDialog() != DialogResult.OK) return;

//         _tabStates[3].IsRunning = true; _tabStates[3].Cts = new CancellationTokenSource(); UpdateUIState();
//         SetProgress(3, 0, 1); SetStats(3, 0, 0, 0); SetStatus(3, "Working...", CAmber);

//         try
//         {
//             var dncService = new DncValidatorService();
//             var dncCts = _tabStates[3].Cts ?? throw new InvalidOperationException("DNC Validator started without a CancellationTokenSource.");

//             await dncService.RunAsync(lblDncFile.Text, sfd.FileName,
//                 (msg, color) => LogLine(3, msg, color),
//                 (done, total) => 
//                 {
//                     SetProgress(3, done, total);
//                     SetStats(3, total, done, 0);
//                 },
//                 dncCts.Token);

//             MessageBox.Show($"DNC Validation Complete!\nFinal output saved to:\n" + sfd.FileName, "Success");
//             SetStatus(3, "Done.", CGreen);
//         }
//         catch (OperationCanceledException) { LogLine(3, "═══ CANCELLED BY USER ═══", CAmber); SetStatus(3, "Cancelled.", CAmber); }
//         catch (Exception ex) { MessageBox.Show("DNC Validator Error: " + ex.Message, "Error"); SetStatus(3, "Error.", CRed); }
//         finally { _tabStates[3].IsRunning = false; _tabStates[3].Cts?.Dispose(); _tabStates[3].Cts = null; UpdateUIState(); }
//     }

//     private async Task ProcessCsvAsync(int tabIndex, string inputPath, string outputPath, int maxPages)
//     {
//         var log = new StringBuilder();
//         void Log(string line) { log.AppendLine(line); LogLine(tabIndex, line, CText); }
//         var (headers, rows) = CsvService.Read(inputPath);

//         Log("═══ HEADERS FOUND ═══"); for (int i = 0; i < headers.Count; i++) Log($"  [{i}] '{headers[i]}'"); Log($"Total rows: {rows.Count}  |  MaxPagesPerSite={maxPages}");
//         if (rows.Count == 0) { Log("CSV has no data rows."); MessageBox.Show("CSV file is empty."); return; }
        
//         int urlIndex = headers.FindIndex(h => h.Trim().Equals("Website URL", StringComparison.OrdinalIgnoreCase));
//         if (urlIndex < 0) { Log("ERROR: No column named 'Website URL' found."); MessageBox.Show("CSV must contain a 'Website URL' column."); return; }

//         bool hasPhoneCol = headers.Any(h => h.Equals("Phone Number", StringComparison.OrdinalIgnoreCase));
//         string targetColumn = hasPhoneCol ? "Additional Phone Number" : "Phone Number";
//         int targetIndex = headers.FindIndex(h => h.Equals(targetColumn, StringComparison.OrdinalIgnoreCase));
//         if (targetIndex < 0) { headers.Add(targetColumn); targetIndex = headers.Count - 1; }

//         bool hasEmailCol = headers.Any(h => h.Equals("Email", StringComparison.OrdinalIgnoreCase));
//         string emailColumn = hasEmailCol ? "Additional Email" : "Email";
//         int emailIndex = headers.FindIndex(h => h.Equals(emailColumn, StringComparison.OrdinalIgnoreCase));
//         if (emailIndex < 0) { headers.Add(emailColumn); emailIndex = headers.Count - 1; }

//         Log($"Writing results to columns: '{targetColumn}', '{emailColumn}'"); Log(""); foreach (var row in rows) while (row.Count < headers.Count) row.Add("");
        
//         using var writer = CsvService.CreateWriter(outputPath); 
//         writer.WriteLine(string.Join(",", headers.Select(CsvService.Escape))); 
//         writer.Flush();
        
//         var urlCache = new ConcurrentDictionary<string, Lazy<Task<SiteResult>>>(StringComparer.OrdinalIgnoreCase);
//         using var siteSlots = new SemaphoreSlim(MaxConcurrentSites);
//         int total = rows.Count, done = 0, success = 0, failed = 0, cachedCount = 0; SetProgress(tabIndex, 0, total); SetStats(tabIndex, total, 0, 0);
        
//         string Describe(SiteResult r) => (string.IsNullOrEmpty(r.Phones) ? "" : "phone: " + r.Phones) + (!string.IsNullOrEmpty(r.Phones) && !string.IsNullOrEmpty(r.Emails) ? " | " : "") + (string.IsNullOrEmpty(r.Emails) ? "" : "email: " + r.Emails);
        
//         async Task ProcessRowAsync(List<string> row, int current)
//         {
//             var url = row[urlIndex].Trim();
//             if (string.IsNullOrWhiteSpace(url)) { LogLine(tabIndex, $"[{current}/{total}] (empty URL)", CSub); Interlocked.Increment(ref done); SetProgress(tabIndex, done, total); SetStats(tabIndex, total, success, failed); return; }
            
//             await siteSlots.WaitAsync();
//             try
//             {
//                 _tabStates[tabIndex].Cts!.Token.ThrowIfCancellationRequested();
//                 var fresh = new Lazy<Task<SiteResult>>(() => _scraper.FindPhoneFastAsync(url, maxPages));
//                 var lazy = urlCache.GetOrAdd(url, fresh); bool isCached = !ReferenceEquals(lazy, fresh);
                
//                 try
//                 {
//                     var result = await lazy.Value; row[targetIndex] = result.Phones; row[emailIndex] = result.Emails; bool any = !string.IsNullOrEmpty(result.Phones) || !string.IsNullOrEmpty(result.Emails);
//                     if (isCached) { Interlocked.Increment(ref cachedCount); if (any) Interlocked.Increment(ref success); else Interlocked.Increment(ref failed); LogLine(tabIndex, $"[{current}/{total}] CACHED {url} → {(any ? Describe(result) : "(none)")}", CBlue); }
//                     else if (any) { Interlocked.Increment(ref success); LogLine(tabIndex, $"[{current}/{total}] FOUND  {url}  ({result.Pages} pages, on {result.FoundOn}) → {Describe(result)}", CGreen); }
//                     else { Interlocked.Increment(ref failed); LogLine(tabIndex, $"[{current}/{total}] NONE   {url}  ({result.Pages} pages) → no contacts", CAmber); }
//                 }
//                 catch (OperationCanceledException) { throw; }
//                 catch (Exception ex) { Interlocked.Increment(ref failed); row[targetIndex] = ""; row[emailIndex] = ""; LogLine(tabIndex, $"[{current}/{total}] FAIL   {url} → {ex.GetType().Name}", CRed); }
//             }
//             finally { siteSlots.Release(); int d = Interlocked.Increment(ref done); SetProgress(tabIndex, d, total); SetStats(tabIndex, total, success, failed); }
//         }

//         var rowTasks = new List<Task>(total); 
//         for (int i = 0; i < total; i++) 
//         {
//             int idx = i;
//             rowTasks.Add(Task.Run(async () => 
//             {
//                 await ProcessRowAsync(rows[idx], idx + 1);
                
//                 lock (writer) 
//                 {
//                     writer.WriteLine(string.Join(",", rows[idx].Select(CsvService.Escape)));
//                     writer.Flush();
//                 }
//             }));
//         }
        
//         await Task.WhenAll(rowTasks);
        
//         LogLine(tabIndex, $"═══ DONE ═══  {success} with contacts, {failed} without, {cachedCount} from cache.", CGreen);
//         File.WriteAllText(outputPath + ".log", log.ToString(), Encoding.UTF8);
//     }

//     private void ProcessCountryAnalysis(int tabIndex, string inputPath, string outputPath, CancellationToken token)
//     {
//         void Log(string line, Color color) => LogLine(tabIndex, line, color);
//         var (headers, rows) = CsvService.Read(inputPath);
//         if (rows.Count == 0) { Log("CSV has no data rows.", CAmber); return; }
        
//         int phoneIndex = headers.FindIndex(h => h.Trim().Equals("Phone Number", StringComparison.OrdinalIgnoreCase));
//         if (phoneIndex < 0) phoneIndex = headers.FindIndex(h => h.Trim().Equals("Additional Phone Number", StringComparison.OrdinalIgnoreCase));
//         if (phoneIndex < 0) { Log("ERROR: No column named 'Phone Number' or 'Additional Phone Number' found.", CRed); MessageBox.Show("CSV must contain a 'Phone Number' column."); return; }
        
//         string countryCol = "Detected Country";
//         int countryIndex = headers.FindIndex(h => h.Equals(countryCol, StringComparison.OrdinalIgnoreCase));
//         if (countryIndex < 0) { headers.Add(countryCol); countryIndex = headers.Count - 1; }
//         foreach (var row in rows) while (row.Count < headers.Count) row.Add("");
        
//         using var writer = CsvService.CreateWriter(outputPath); 
//         writer.WriteLine(string.Join(",", headers.Select(CsvService.Escape))); 
//         writer.Flush();
        
//         int total = rows.Count, current = 0, detected = 0, unknown = 0;
        
//         SetProgress(tabIndex, 0, total);
        
//         foreach (var row in rows)
//         {
//             token.ThrowIfCancellationRequested();
//             current++; string phones = row[phoneIndex]; string detectedCountry = "";
//             if (!string.IsNullOrWhiteSpace(phones))
//             {
//                 var phoneList = phones.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries); var countries = new HashSet<string>();
//                 foreach (var phone in phoneList) { string country = CountryDetectorService.Detect(phone.Trim()); if (country != "Unknown" && country != "Invalid") countries.Add(country); }
//                 if (countries.Count > 0) { detectedCountry = string.Join(" / ", countries); detected++; } 
//                 else { detectedCountry = "Unknown/Invalid"; unknown++; }
//             }
//             else { unknown++; }
            
//             row[countryIndex] = detectedCountry; 
//             writer.WriteLine(string.Join(",", row.Select(CsvService.Escape))); 
//             writer.Flush();
            
//             if (current % 500 == 0 || current == total)
//             {
//                 SetProgress(tabIndex, current, total);
//                 SetStats(tabIndex, total, detected, unknown);
//                 Log($"  Processed {current:N0} / {total:N0} rows…", CSub);
//             }
//         }
//         Log($"═══ DONE ═══ Detected countries for {detected:N0} rows.", CGreen);
//     }

//     private void Drag(object? s, MouseEventArgs e) { if (e.Button == MouseButtons.Left && WindowState != FormWindowState.Maximized) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); } }
//     private void ToggleMaximize() { WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; }
    
//     protected override void OnResize(EventArgs e) { base.OnResize(e); Region = WindowState == FormWindowState.Maximized ? null : new Region(RoundRect(new Rectangle(0, 0, Width, Height), WinRadius)); }
//     protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using var pen = new Pen(CBorder, 1); e.Graphics.DrawPath(pen, RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), WinRadius)); }
//     protected override void OnPaintBackground(PaintEventArgs e) { var g = e.Graphics; using var bg = new LinearGradientBrush(ClientRectangle, CBg, CBgDeep, 90f); g.FillRectangle(bg, ClientRectangle); if (_isDarkTheme) { var glowPath = new GraphicsPath(); glowPath.AddEllipse(ClientSize.Width - 520, -220, 640, 400); using var glow = new PathGradientBrush(glowPath) { CenterColor = Color.FromArgb(30, CViolet), SurroundColors = new[] { Color.FromArgb(0, CViolet) }, FocusScales = new PointF(0.4f, 0.4f) }; g.FillEllipse(glow, ClientSize.Width - 520, -220, 640, 400); glowPath.Dispose(); } }

//     protected override void WndProc(ref Message m)
//     {
//         const int WM_NCHITTEST = 0x84;
//         if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal)
//         {
//             var p = PointToClient(Cursor.Position); int w = ClientSize.Width, h = ClientSize.Height;
//             bool l = p.X < Grip, r = p.X >= w - Grip, t = p.Y < Grip, b = p.Y >= h - Grip;
//             if (t && l) { m.Result = (IntPtr)13; return; } if (t && r) { m.Result = (IntPtr)14; return; }
//             if (b && l) { m.Result = (IntPtr)16; return; } if (b && r) { m.Result = (IntPtr)17; return; }
//             if (l) { m.Result = (IntPtr)10; return; } if (r) { m.Result = (IntPtr)11; return; }
//             if (t) { m.Result = (IntPtr)12; return; } if (b) { m.Result = (IntPtr)15; return; }
//         }
//         base.WndProc(ref m);
//     }

//     private static GraphicsPath RoundRect(Rectangle rect, int radius) { var path = new GraphicsPath(); if (rect.Width <= 0 || rect.Height <= 0) return path; if (radius <= 0) { path.AddRectangle(rect); return path; } float d = radius * 2f; if (d > rect.Width) d = rect.Width; if (d > rect.Height) d = rect.Height; path.AddArc(rect.X, rect.Y, d, d, 180, 90); path.AddArc(rect.X + rect.Width - d, rect.Y, d, d, 270, 90); path.AddArc(rect.X + rect.Width - d, rect.Y + rect.Height - d, d, d, 0, 90); path.AddArc(rect.X, rect.Y + rect.Height - d, d, d, 90, 90); path.CloseFigure(); return path; }
//     private static Color Lerp(Color a, Color b, float t) => Color.FromArgb((int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
//     private Image? TryLoadImage(string folder, params string[] filenames) { foreach (var name in filenames) { string path = Path.Combine(folder, name); if (File.Exists(path)) { try { if (path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)) { using var ico = new Icon(path); return ico.ToBitmap(); } return Image.FromFile(path); } catch { } } } return null; }
//     private void LoadSettings() { try { if (File.Exists(SettingsFile)) { var lines = File.ReadAllLines(SettingsFile); if (lines.Length > 0 && Directory.Exists(lines[0])) _lastFolder = lines[0]; } } catch { } }
//     private void SaveSettings() { try { File.WriteAllText(SettingsFile, _lastFolder); } catch { } }

//     private sealed class SectionCard : Panel { public int Radius { get; set; } = 12; public SectionCard() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true); } protected override void OnPaint(PaintEventArgs e) { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using var path = RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius); using var fill = new SolidBrush(CCard); e.Graphics.FillPath(fill, path); using var pen = new Pen(CBorder, 1); e.Graphics.DrawPath(pen, path); } protected override void OnResize(EventArgs e) { base.OnResize(e); Region = new Region(RoundRect(new Rectangle(0, 0, Width, Height), Radius)); } }
//     private sealed class StatCard : Control { public string Caption { get; set; } = ""; public string Value { get; set; } = "0"; public string Glyph { get; set; } = "◈"; public Color Accent { get; set; } = CViolet; public StatCard() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); } public void SetValue(string v) { Value = v; Invalidate(); } protected override void OnPaint(PaintEventArgs e) { var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; var rc = new Rectangle(0, 0, Width - 1, Height - 1); using (var path = RoundRect(rc, 12)) using (var fill = new SolidBrush(CCardHi)) g.FillPath(fill, path); using (var path = RoundRect(rc, 12)) using (var pen = new Pen(CBorder, 1)) g.DrawPath(pen, path); using var barPath = RoundRect(new Rectangle(0, 0, Width, 5), 4); using var bar = new LinearGradientBrush(new Rectangle(0, 0, Width, 5), Accent, Color.FromArgb(0, Accent), 0f); g.FillPath(bar, barPath); using var capFont = new Font("Segoe UI", 8f, FontStyle.Bold); TextRenderer.DrawText(g, Caption.ToUpperInvariant(), capFont, new Rectangle(18, 16, Width - 70, 16), CSub, TextFormatFlags.Left | TextFormatFlags.VerticalCenter); using var valFont = new Font("Segoe UI", 24f, FontStyle.Bold); TextRenderer.DrawText(g, Value, valFont, new Rectangle(16, 34, Width - 60, Height - 40), CText, TextFormatFlags.Left | TextFormatFlags.VerticalCenter); int d = 34, gx = Width - d - 16, gy = 16; using var chipPath = RoundRect(new Rectangle(gx, gy, d, d), 9); using var chip = new SolidBrush(Color.FromArgb(38, Accent)); g.FillPath(chip, chipPath); using var glyphFont = new Font("Segoe UI", 12f, FontStyle.Bold); TextRenderer.DrawText(g, Glyph, glyphFont, new Rectangle(gx, gy, d, d), Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); } }
//     private sealed class ModernButton : Control { private float _t; private bool _hover, _down; private readonly System.Windows.Forms.Timer _anim; public Color ColorA { get; set; } = CViolet; public Color ColorB { get; set; } = CViolet2; public int Radius { get; set; } = 10; public bool TextOnly { get; set; } public ModernButton() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Size = new Size(150, 36); Cursor = Cursors.Hand; Font = new Font("Segoe UI", 9f, FontStyle.Bold); _anim = new System.Windows.Forms.Timer { Interval = 15 }; _anim.Tick += (s, e) => { float target = (_hover || _down) ? 1f : 0f; _t += Math.Sign(target - _t) * 0.14f; if (Math.Abs(target - _t) < 0.02f) _t = target; Invalidate(); }; MouseEnter += (s, e) => { _hover = true; _anim.Start(); }; MouseLeave += (s, e) => { _hover = false; _down = false; _anim.Start(); }; MouseDown += (s, e) => { _down = true; Invalidate(); }; MouseUp += (s, e) => { _down = false; Invalidate(); }; } protected override void OnPaint(PaintEventArgs e) { var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; var rc = new Rectangle(0, 0, Width - 1, Height - 1); if (!Enabled) { using var path = RoundRect(rc, Radius); using var fill = new SolidBrush(_isDarkTheme ? Color.FromArgb(34, 36, 46) : Color.FromArgb(230, 230, 235)); g.FillPath(fill, path); TextRenderer.DrawText(g, Text, Font, ClientRectangle, _isDarkTheme ? Color.FromArgb(110, 115, 128) : Color.FromArgb(160, 160, 165), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); return; } float lift = 0.16f * _t - (_down ? 0.10f : 0f); var a = Lerp(ColorA, Color.White, lift); var b = Lerp(ColorB, Color.White, lift); using (var path = RoundRect(rc, Radius)) using (var grad = new LinearGradientBrush(rc, a, b, 35f)) g.FillPath(grad, path); TextRenderer.DrawText(g, Text, Font, ClientRectangle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); } }
//     private sealed class GradientProgress : Control { private int _value; private int _max = 1; public int Value { get => _value; set { _value = Math.Min(Math.Max(0, value), _max); Invalidate(); } } public int Maximum { get => _max; set { _max = Math.Max(1, value); Invalidate(); } } public GradientProgress() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Height = 12; } protected override void OnPaint(PaintEventArgs e) { var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; using (var track = RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), Height / 2)) using (var tb = new SolidBrush(_isDarkTheme ? Color.FromArgb(38, 41, 54) : Color.FromArgb(220, 220, 225))) g.FillPath(tb, track); int fillW = (int)(Width * ((float)_value / _max)); if (fillW <= 4) return; var rc = new Rectangle(0, 0, fillW, Height); using var path = RoundRect(rc, Height / 2); using var grad = new LinearGradientBrush(rc, CViolet, CCyan, 0f); g.FillPath(grad, path); } }
//     private sealed class StatusPill : Control { private string _label = ""; public Color Dot { get; private set; } = CCyan; public StatusPill() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Height = 22; Width = 260; } public void Set(string text, Color dot) { _label = text; Dot = dot; Invalidate(); } protected override void OnPaint(PaintEventArgs e) { var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; int w = Width - 1, h = Height - 1; using (var path = RoundRect(new Rectangle(0, 0, w, h), h / 2)) using (var fill = new SolidBrush(CCardHi)) g.FillPath(fill, path); using (var path = RoundRect(new Rectangle(0, 0, w, h), h / 2)) using (var pen = new Pen(CBorder, 1)) g.DrawPath(pen, path); int d = 8, dx = 14, dy = (Height - d) / 2; using var dotBrush = new SolidBrush(Dot); g.FillEllipse(dotBrush, dx, dy, d, d); using var f = new Font("Segoe UI", 8.5f); TextRenderer.DrawText(g, _label, f, new Rectangle(dx + d + 8, 0, Width - dx - d - 20, Height), CSub, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis); } }
//     private sealed class WindowButton : Control { private bool _hover; public string Glyph { get; set; } = "✕"; public bool IsClose { get; set; } public WindowButton() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true); Size = new Size(36, 30); Cursor = Cursors.Hand; Font = new Font("Segoe UI", 9.5f); BackColor = Color.Transparent; MouseEnter += (s, e) => { _hover = true; Invalidate(); }; MouseLeave += (s, e) => { _hover = false; Invalidate(); }; } protected override void OnPaint(PaintEventArgs e) { var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; if (_hover) { Color c = IsClose ? Color.FromArgb(232, 61, 74) : (_isDarkTheme ? Color.FromArgb(55, 58, 72) : Color.FromArgb(220, 220, 225)); using var path = RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), 8); using var fill = new SolidBrush(c); g.FillPath(fill, path); } TextRenderer.DrawText(g, Glyph, Font, ClientRectangle, _hover ? Color.White : CSub, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); } }
// }



using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Data_Enrichment_Helper;

public class MainForm : Form
{
    private readonly HttpClient _httpClient;
    private readonly WebScraperService _scraper;
    private const int MaxConcurrentSites = 50; // ✅ INCREASED FROM 24
    private const int WinRadius = 14;
    private const int Grip = 7;

    private string _inputPath = "";
    private string _lastFolder = "";
    private static bool _isDarkTheme = true; 

    private int _activeTab = 0;
    private readonly TabState[] _tabStates = { new TabState(), new TabState(), new TabState(), new TabState() };
    private readonly object _stateLock = new object();

    private ModernButton btnTabPhone = null!, btnTabMap = null!, btnTabCountry = null!, btnTabDnc = null!;
    private SectionCard panelPhone = null!, panelMap = null!, panelCountry = null!, panelDnc = null!;

    private static Color CBg      = Color.FromArgb(15, 16, 22);
    private static Color CBgDeep  = Color.FromArgb(10, 11, 16);
    private static Color CCard    = Color.FromArgb(24, 26, 34);
    private static Color CCardHi  = Color.FromArgb(30, 32, 42);
    private static Color CBorder  = Color.FromArgb(42, 45, 59);
    private static Color CText    = Color.FromArgb(244, 245, 247);
    private static Color CSub     = Color.FromArgb(148, 154, 168);
    private static Color CViolet  = Color.FromArgb(124, 92, 255);
    private static Color CViolet2 = Color.FromArgb(168, 140, 255);
    private static Color CCyan    = Color.FromArgb(56, 225, 198);
    private static Color CGreen   = Color.FromArgb(52, 211, 153);
    private static Color CRed     = Color.FromArgb(248, 113, 113);
    private static Color CAmber   = Color.FromArgb(251, 191, 36);
    private static Color CBlue    = Color.FromArgb(96, 165, 250);

    private static string SettingsFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "deh_ui.ini");

    private Panel titleBar = null!;
    private PictureBox pbIcon = null!;
    private Label lblWindowTitle = null!;
    private WindowButton btnWinMin = null!, btnWinMax = null!, btnWinClose = null!, btnWinTheme = null!;
    private PictureBox pbLogo2 = null!;
    private Label lblTitle = null!, lblSubtitle = null!;
    private StatCard cardTotal = null!, cardFound = null!, cardMissing = null!, cardRate = null!;
    private StatusPill statusPill = null!;
    private Label lblProgressPct = null!;
    private GradientProgress progressBar = null!;
    private SectionCard logCard = null!;
    private Label lblLogHeader = null!;
    private ModernButton btnClearLog = null!;
    private RichTextBox txtLog = null!;

    private Label lblFile = null!;
    private NumericUpDown numMaxPages = null!;
    private ModernButton btnSelect = null!, btnStart = null!, btnCancelPhone = null!;
    private TextBox txtKeywords = null!, txtLocations = null!;
    private ComboBox cmbCountry = null!;
    private CheckBox chkRunWebScrape = null!, chkRunCountryDetect = null!, chkRunDnc = null!;
    private ModernButton btnScrapeMaps = null!, btnCancelMap = null!;
    private ModernButton btnDetect = null!, btnCancelCountry = null!;

    private Label lblDncFile = null!;
    private ModernButton btnSelectDnc = null!, btnStartDnc = null!, btnCancelDnc = null!;

    [DllImport("user32.dll")] public static extern bool ReleaseCapture();
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    public MainForm()
    {
        Text = "Lime Software Logics - Data Enrichment Helper";
        Width = 1300; Height = 950;
        MinimumSize = new Size(1100, 740);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        DoubleBuffered = true;
        KeyPreview = true;

        string assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
        try { var iconImg = TryLoadImage(assetsPath, "logo2.ico", "logo2.png"); if (iconImg != null) Icon = Icon.FromHandle(((Bitmap)iconImg).GetHicon()); } catch { }

        LoadSettings();
        BuildTitleBar(assetsPath);
        BuildHero(assetsPath);
        BuildTabs();
        BuildStatsRow();
        BuildProgress();
        BuildLog();
        ApplyTheme();

        _httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true, AutomaticDecompression = System.Net.DecompressionMethods.All, UseCookies = false, MaxConnectionsPerServer = 64 });
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
        _scraper = new WebScraperService(_httpClient);

        UpdateUIState();
        SwitchTab(0); 
    }

    private class TabState
    {
        public bool IsRunning { get; set; }
        public CancellationTokenSource? Cts { get; set; }
        public StringBuilder LogBuilder { get; } = new StringBuilder();
        public int ProgressValue { get; set; }
        public int ProgressMax { get; set; } = 1;
        public int Total { get; set; }
        public int Found { get; set; }
        public int Missing { get; set; }
        public string StatusText { get; set; } = "Ready.";
        public Color StatusColor { get; set; } = CCyan;
    }

    private void SwitchTab(int tabIndex)
    {
        _activeTab = tabIndex;
        panelPhone.Visible = tabIndex == 0;
        panelMap.Visible = tabIndex == 1;
        panelCountry.Visible = tabIndex == 2;
        panelDnc.Visible = tabIndex == 3;

        var state = _tabStates[tabIndex];

        lock (_stateLock)
        {
            txtLog.Text = state.LogBuilder.ToString();
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        if (tabIndex == 0) { cardTotal.Caption = "TOTAL ROWS"; cardFound.Caption = "FOUND"; cardMissing.Caption = "MISSING"; }
        else if (tabIndex == 1) { cardTotal.Caption = "QUERIES"; cardFound.Caption = "EXTRACTED"; cardMissing.Caption = "FAILED"; }
        else if (tabIndex == 2) { cardTotal.Caption = "TOTAL ROWS"; cardFound.Caption = "VALID PHONES"; cardMissing.Caption = "INVALID"; }
        else { cardTotal.Caption = "TOTAL ROWS"; cardFound.Caption = "VALIDATED"; cardMissing.Caption = "FAILED"; }

        cardTotal.SetValue(state.Total.ToString("N0"));
        cardFound.SetValue(state.Found.ToString("N0"));
        cardMissing.SetValue(state.Missing.ToString("N0"));
        int processed = state.Found + state.Missing;
        cardRate.SetValue(processed == 0 ? "—" : $"{100.0 * state.Found / processed:F0}%");

        progressBar.Maximum = state.ProgressMax;
        progressBar.Value = Math.Min(state.ProgressValue, state.ProgressMax);
        lblProgressPct.Text = $"{(int)(100.0 * progressBar.Value / Math.Max(1, progressBar.Maximum))}%";

        statusPill.Set(state.StatusText, state.StatusColor);

        UpdateTabColors();
        UpdateUIState();
    }

    private void UpdateUIState()
    {
        btnSelect.Enabled = !_tabStates[0].IsRunning;
        btnStart.Enabled = !_tabStates[0].IsRunning && !string.IsNullOrEmpty(_inputPath);
        btnCancelPhone.Enabled = _tabStates[0].IsRunning;
        numMaxPages.Enabled = !_tabStates[0].IsRunning;

        btnScrapeMaps.Enabled = !_tabStates[1].IsRunning;
        btnCancelMap.Enabled = _tabStates[1].IsRunning;
        chkRunWebScrape.Enabled = !_tabStates[1].IsRunning;
        chkRunCountryDetect.Enabled = !_tabStates[1].IsRunning;
        chkRunDnc.Enabled = !_tabStates[1].IsRunning;
        
        btnDetect.Enabled = !_tabStates[2].IsRunning;
        btnCancelCountry.Enabled = _tabStates[2].IsRunning;

        btnSelectDnc.Enabled = !_tabStates[3].IsRunning;
        btnStartDnc.Enabled = !_tabStates[3].IsRunning && !string.IsNullOrEmpty(lblDncFile.Text) && lblDncFile.Text != "no file selected";
        btnCancelDnc.Enabled = _tabStates[3].IsRunning;
    }

    private void LogLine(int tabIndex, string text, Color color)
    {
        var state = _tabStates[tabIndex];
        string stamped = $"[{DateTime.Now:HH:mm:ss}]  {text}";

        lock (_stateLock) { state.LogBuilder.AppendLine(stamped); }

        if (_activeTab == tabIndex)
        {
            if (txtLog.InvokeRequired) txtLog.BeginInvoke(new Action(() => AppendToLogUI(stamped, color)));
            else AppendToLogUI(stamped, color);
        }
    }

    private void AppendToLogUI(string text, Color color)
    {
        int start = txtLog.TextLength;
        txtLog.AppendText(text + Environment.NewLine);
        txtLog.Select(start, text.Length);
        txtLog.SelectionColor = color;
        txtLog.SelectionStart = txtLog.TextLength;
        txtLog.SelectionLength = 0;
        txtLog.SelectionColor = CSub;
        txtLog.ScrollToCaret();
    }

    private void SetStats(int tabIndex, int total, int found, int missing)
    {
        var state = _tabStates[tabIndex];
        state.Total = total; state.Found = found; state.Missing = missing;

        if (_activeTab == tabIndex)
        {
            if (InvokeRequired) BeginInvoke(new Action(() => SetStats(tabIndex, total, found, missing)));
            else
            {
                cardTotal.SetValue(total.ToString("N0"));
                cardFound.SetValue(found.ToString("N0"));
                cardMissing.SetValue(missing.ToString("N0"));
                int processed = found + missing;
                cardRate.SetValue(processed == 0 ? "—" : $"{100.0 * found / processed:F0}%");
            }
        }
    }

    private void SetProgress(int tabIndex, int value, int max)
    {
        var state = _tabStates[tabIndex];
        state.ProgressValue = value; state.ProgressMax = Math.Max(1, max);

        if (_activeTab == tabIndex)
        {
            if (InvokeRequired) BeginInvoke(new Action(() => SetProgress(tabIndex, value, max)));
            else
            {
                progressBar.Maximum = state.ProgressMax;
                progressBar.Value = Math.Min(value, state.ProgressMax);
                lblProgressPct.Text = $"{(int)(100.0 * progressBar.Value / progressBar.Maximum)}%";
            }
        }
    }

    private void SetStatus(int tabIndex, string text, Color color)
    {
        var state = _tabStates[tabIndex];
        state.StatusText = text; state.StatusColor = color;

        if (_activeTab == tabIndex)
        {
            if (InvokeRequired) BeginInvoke(new Action(() => SetStatus(tabIndex, text, color)));
            else statusPill.Set(text, color);
        }
    }

    private void ApplyTheme()
    {
        if (_isDarkTheme)
        {
            CBg = Color.FromArgb(15, 16, 22); CBgDeep = Color.FromArgb(10, 11, 16); CCard = Color.FromArgb(24, 26, 34);
            CCardHi = Color.FromArgb(30, 32, 42); CBorder = Color.FromArgb(42, 45, 59); CText = Color.FromArgb(244, 245, 247);
            CSub = Color.FromArgb(148, 154, 168); CCyan = Color.FromArgb(56, 225, 198);
            if (txtLog != null) txtLog.BackColor = CBgDeep;
        }
        else
        {
            CBg = Color.FromArgb(245, 245, 247); CBgDeep = Color.FromArgb(235, 235, 240); CCard = Color.FromArgb(255, 255, 255);
            CCardHi = Color.FromArgb(249, 249, 251); CBorder = Color.FromArgb(229, 229, 234); CText = Color.FromArgb(29, 29, 31);
            CSub = Color.FromArgb(134, 134, 139); CCyan = Color.FromArgb(0, 184, 212);
            if (txtLog != null) txtLog.BackColor = Color.FromArgb(250, 250, 250);
        }

        BackColor = CBg; ForeColor = CText;
        if (txtLog != null) txtLog.ForeColor = CText;
        if (lblWindowTitle != null) lblWindowTitle.ForeColor = CText;
        if (lblSubtitle != null) lblSubtitle.ForeColor = CSub;
        if (lblFile != null) lblFile.ForeColor = CSub;
        if (lblDncFile != null) lblDncFile.ForeColor = CSub;
        if (lblLogHeader != null) lblLogHeader.ForeColor = CSub;
        if (lblProgressPct != null) lblProgressPct.ForeColor = CSub;
        
        if (numMaxPages != null) { numMaxPages.BackColor = CCardHi; numMaxPages.ForeColor = CText; }
        if (chkRunWebScrape != null) chkRunWebScrape.ForeColor = CText;
        if (chkRunCountryDetect != null) chkRunCountryDetect.ForeColor = CText;
        if (chkRunDnc != null) chkRunDnc.ForeColor = CText;

        UpdateTabColors();
        Invalidate(true);
        foreach (Control c in Controls) c.Invalidate(true);
    }

    private void BuildTitleBar(string assetsPath)
    {
        titleBar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.Transparent };
        titleBar.MouseDown += Drag;
        titleBar.DoubleClick += (s, e) => ToggleMaximize();
        Controls.Add(titleBar);

        pbIcon = new PictureBox { Left = 14, Top = 9, Width = 28, Height = 28, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent, Image = TryLoadImage(assetsPath, "logo1.png", "logo.png") };
        pbIcon.MouseDown += Drag;
        titleBar.Controls.Add(pbIcon);

        lblWindowTitle = new Label { Text = "Data Enrichment Helper", Left = 50, Top = 13, AutoSize = true, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = CText, BackColor = Color.Transparent };
        lblWindowTitle.MouseDown += Drag;
        titleBar.Controls.Add(lblWindowTitle);

        btnWinClose = new WindowButton { Glyph = "✕", IsClose = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(titleBar.Width - 44, 8) };
        btnWinClose.Click += (s, e) => { if (_tabStates.Any(x => x.IsRunning) && MessageBox.Show("Tasks running. Close?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.No) return; Application.Exit(); };
        titleBar.Controls.Add(btnWinClose);

        btnWinMax = new WindowButton { Glyph = "▢", Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(titleBar.Width - 88, 8) };
        btnWinMax.Click += (s, e) => ToggleMaximize();
        titleBar.Controls.Add(btnWinMax);

        btnWinMin = new WindowButton { Glyph = "—", Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(titleBar.Width - 132, 8) };
        btnWinMin.Click += (s, e) => WindowState = FormWindowState.Minimized;
        titleBar.Controls.Add(btnWinMin);

        btnWinTheme = new WindowButton { Glyph = "☀", Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(titleBar.Width - 176, 8) };
        btnWinTheme.Click += (s, e) => { _isDarkTheme = !_isDarkTheme; btnWinTheme.Glyph = _isDarkTheme ? "☀" : "🌙"; ApplyTheme(); };
        titleBar.Controls.Add(btnWinTheme);

        titleBar.Resize += (s, e) => { btnWinClose.Left = titleBar.Width - 44; btnWinMax.Left = titleBar.Width - 88; btnWinMin.Left = titleBar.Width - 132; btnWinTheme.Left = titleBar.Width - 176; };
    }

    private void BuildHero(string assetsPath)
    {
        lblTitle = new Label { Text = "Data Enrichment Helper", Left = 28, Top = 62, Width = 600, Height = 34, Font = new Font("Segoe UI", 20f, FontStyle.Bold), ForeColor = CText, BackColor = Color.Transparent };
        lblTitle.Paint += (s, e) => { var r = lblTitle.ClientRectangle; using var br = new LinearGradientBrush(r, CText, CViolet2, 0f); TextRenderer.DrawText(e.Graphics, lblTitle.Text, lblTitle.Font, r, CText, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix); };
        Controls.Add(lblTitle);

        lblSubtitle = new Label { Text = "By NIZAMI ⓒ   ·   Website Phone & Email Scraper   ·   Country Detector   ·   Map Scraper   ·   DNC Validator", Left = 30, Top = 100, Width = 800, Height = 20, Font = new Font("Segoe UI", 9.5f), ForeColor = CSub, BackColor = Color.Transparent };
        Controls.Add(lblSubtitle);

        pbLogo2 = new PictureBox { Left = ClientSize.Width - 110, Top = 45, Width = 70, Height = 70, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        pbLogo2.Image = TryLoadImage(assetsPath, "logo2.png", "logo2.ico");
        if (pbLogo2.Image != null) { var bmp = new Bitmap(pbLogo2.Image); bmp.MakeTransparent(Color.White); pbLogo2.Image = bmp; using var path = new GraphicsPath(); path.AddEllipse(0, 0, pbLogo2.Width, pbLogo2.Height); pbLogo2.Region = new Region(path); }
        Controls.Add(pbLogo2);
    }

    private void BuildTabs()
    {
        int y = 140, w = 150;
        
        btnTabPhone = new ModernButton { Text = "✉  Email & Phone", Location = new Point(28, y), Size = new Size(w, 36), ColorA = CViolet, ColorB = CViolet2, Radius = 8 };
        btnTabPhone.Click += (s, e) => SwitchTab(0);
        
        btnTabMap = new ModernButton { Text = "🗺  Map Scraper", Location = new Point(28 + w + 10, y), Size = new Size(w, 36), ColorA = CViolet, ColorB = CViolet2, Radius = 8 };
        btnTabMap.Click += (s, e) => SwitchTab(1);
        
        btnTabCountry = new ModernButton { Text = "🌍  Country Detector", Location = new Point(28 + (w + 10) * 2, y), Size = new Size(w, 36), ColorA = CViolet, ColorB = CViolet2, Radius = 8 };
        btnTabCountry.Click += (s, e) => SwitchTab(2);

        btnTabDnc = new ModernButton { Text = "📞  DNC Validator", Location = new Point(28 + (w + 10) * 3, y), Size = new Size(w, 36), ColorA = CViolet, ColorB = CViolet2, Radius = 8 };
        btnTabDnc.Click += (s, e) => SwitchTab(3);

        Controls.Add(btnTabPhone); Controls.Add(btnTabMap); Controls.Add(btnTabCountry); Controls.Add(btnTabDnc);

        // --- Panel 1: Email Scraper ---
        panelPhone = new SectionCard { Left = 28, Top = 190, Width = ClientSize.Width - 56, Height = 150, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        Controls.Add(panelPhone);

        btnSelect = new ModernButton { Text = "Select CSV…", ColorA = CBlue, ColorB = Color.FromArgb(130, 180, 255), Size = new Size(150, 36), Location = new Point(20, 20) };
        btnSelect.Click += BtnSelect_Click;
        panelPhone.Controls.Add(btnSelect);

        lblFile = new Label { Text = "no file selected", Left = 184, Top = 28, Width = panelPhone.Width - 560, Height = 20, AutoEllipsis = true, Font = new Font("Segoe UI", 9.5f), ForeColor = CSub, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        panelPhone.Controls.Add(lblFile);

        var lblPages = new Label { Text = "Max pages / site", AutoSize = true, Font = new Font("Segoe UI", 9f), ForeColor = CSub, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        panelPhone.Controls.Add(lblPages);

        numMaxPages = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 15, Width = 70, Height = 26, BorderStyle = BorderStyle.None, BackColor = CCardHi, ForeColor = CText, Font = new Font("Segoe UI", 9.5f), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        numMaxPages.Location = new Point(panelPhone.Width - 20 - numMaxPages.Width, 18);
        lblPages.Location = new Point(numMaxPages.Left - lblPages.Width - 10, 25);
        panelPhone.Controls.Add(numMaxPages);
        panelPhone.Resize += (s, e) => { numMaxPages.Left = panelPhone.Width - 20 - numMaxPages.Width; lblPages.Left = numMaxPages.Left - lblPages.Width - 10; };

        btnStart = new ModernButton { Text = "▶   Start Scraping", ColorA = Color.FromArgb(16, 163, 110), ColorB = CGreen, Size = new Size(180, 38), Location = new Point(20, 80) };
        btnStart.Click += BtnStart_Click;
        panelPhone.Controls.Add(btnStart);

        btnCancelPhone = new ModernButton { Text = "■   Cancel", ColorA = Color.FromArgb(200, 70, 70), ColorB = CRed, Size = new Size(130, 38), Location = new Point(212, 80) };
        btnCancelPhone.Click += (s, e) => _tabStates[0].Cts?.Cancel();
        panelPhone.Controls.Add(btnCancelPhone);

        // --- Panel 2: Map Scraper ---
        panelMap = new SectionCard { Left = 28, Top = 190, Width = ClientSize.Width - 56, Height = 150, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        Controls.Add(panelMap);

        var lblKeywords = new Label { Text = "Keywords (1 per line):", Left = 20, Top = 15, AutoSize = true, Font = new Font("Segoe UI", 8f), ForeColor = CSub, BackColor = Color.Transparent };
        txtKeywords = new TextBox { Left = 20, Top = 35, Width = 250, Height = 80, Multiline = true, ScrollBars = ScrollBars.Vertical, Text = "Roofing contractor\r\nDentist", BorderStyle = BorderStyle.None, BackColor = CCardHi, ForeColor = CText, Font = new Font("Segoe UI", 9.5f) };

        var lblCountry = new Label { Text = "Country:", Left = 290, Top = 15, AutoSize = true, Font = new Font("Segoe UI", 8f), ForeColor = CSub, BackColor = Color.Transparent };
        cmbCountry = new ComboBox { Left = 290, Top = 35, Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CCardHi, ForeColor = CText, FlatStyle = FlatStyle.Flat };
        cmbCountry.Items.AddRange(new object[] { "United States", "Canada" });
        cmbCountry.SelectedIndex = 0;
        cmbCountry.SelectedIndexChanged += (s, e) => UpdateLocations();

        var lblLocations = new Label { Text = "Locations (1 per line):", Left = 430, Top = 15, AutoSize = true, Font = new Font("Segoe UI", 8f), ForeColor = CSub, BackColor = Color.Transparent };
        txtLocations = new TextBox { Left = 430, Top = 35, Width = 250, Height = 80, Multiline = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None, BackColor = CCardHi, ForeColor = CText, Font = new Font("Segoe UI", 9.5f) };

        btnScrapeMaps = new ModernButton { Text = "🗺️   Run Bulk Scraper", ColorA = Color.FromArgb(234, 88, 12), ColorB = Color.FromArgb(249, 115, 22), Size = new Size(160, 38), Location = new Point(700, 35) };
        btnScrapeMaps.Click += BtnScrapeMaps_Click;

        btnCancelMap = new ModernButton { Text = "■   Cancel", ColorA = Color.FromArgb(200, 70, 70), ColorB = CRed, Size = new Size(130, 38), Location = new Point(700, 80) };
        btnCancelMap.Click += (s, e) => _tabStates[1].Cts?.Cancel();

        chkRunWebScrape    = new CheckBox { Text = "Run Phone & Email Scraper", Left = 20,  Top = 125, AutoSize = true, ForeColor = CText, BackColor = Color.Transparent, Checked = true };
        chkRunCountryDetect = new CheckBox { Text = "Run Country Detector",     Left = 230, Top = 125, AutoSize = true, ForeColor = CText, BackColor = Color.Transparent, Checked = true };
        chkRunDnc          = new CheckBox { Text = "Run DNC Validator",         Left = 420, Top = 125, AutoSize = true, ForeColor = CText, BackColor = Color.Transparent, Checked = false };

        panelMap.Controls.AddRange(new Control[] { lblKeywords, txtKeywords, lblCountry, cmbCountry, lblLocations, txtLocations, btnScrapeMaps, btnCancelMap, chkRunWebScrape, chkRunCountryDetect, chkRunDnc });
        UpdateLocations();

        // --- Panel 3: Country Detector ---
        panelCountry = new SectionCard { Left = 28, Top = 190, Width = ClientSize.Width - 56, Height = 150, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        Controls.Add(panelCountry);

        var lblCountryDesc = new Label { Text = "Select a CSV containing a 'Phone Number' column. The app detects the country using the prefix\n(e.g., +91 = India, +92 = Pakistan, +1 = US/Canada) and adds a 'Detected Country' column.", Left = 20, Top = 20, Width = 800, Height = 40, Font = new Font("Segoe UI", 9.5f), ForeColor = CSub, BackColor = Color.Transparent };
        panelCountry.Controls.Add(lblCountryDesc);

        btnDetect = new ModernButton { Text = "🌍   Detect & Save", ColorA = CViolet, ColorB = CViolet2, Size = new Size(180, 38), Location = new Point(20, 80) };
        btnDetect.Click += BtnDetect_Click;
        panelCountry.Controls.Add(btnDetect);

        btnCancelCountry = new ModernButton { Text = "■   Cancel", ColorA = Color.FromArgb(200, 70, 70), ColorB = CRed, Size = new Size(130, 38), Location = new Point(212, 80) };
        btnCancelCountry.Click += (s, e) => _tabStates[2].Cts?.Cancel();
        panelCountry.Controls.Add(btnCancelCountry);

        // --- Panel 4: DNC Validator (standalone) ---
        panelDnc = new SectionCard { Left = 28, Top = 190, Width = ClientSize.Width - 56, Height = 150, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        Controls.Add(panelDnc);

        btnSelectDnc = new ModernButton { Text = "Select CSV…", ColorA = CBlue, ColorB = Color.FromArgb(130, 180, 255), Size = new Size(150, 36), Location = new Point(20, 20) };
        btnSelectDnc.Click += BtnSelectDnc_Click;
        panelDnc.Controls.Add(btnSelectDnc);

        lblDncFile = new Label { Text = "no file selected", Left = 184, Top = 28, Width = 600, Height = 20, AutoEllipsis = true, Font = new Font("Segoe UI", 9.5f), ForeColor = CSub, BackColor = Color.Transparent };
        panelDnc.Controls.Add(lblDncFile);

        btnStartDnc = new ModernButton { Text = "▶   Start DNC Validation", ColorA = Color.FromArgb(16, 163, 110), ColorB = CGreen, Size = new Size(180, 38), Location = new Point(20, 80) };
        btnStartDnc.Click += BtnStartDnc_Click;
        panelDnc.Controls.Add(btnStartDnc);

        btnCancelDnc = new ModernButton { Text = "■   Cancel", ColorA = Color.FromArgb(200, 70, 70), ColorB = CRed, Size = new Size(130, 38), Location = new Point(212, 80) };
        btnCancelDnc.Click += (s, e) => _tabStates[3].Cts?.Cancel();
        panelDnc.Controls.Add(btnCancelDnc);
    }

    private void UpdateTabColors()
    {
        if (btnTabPhone == null) return;
        btnTabPhone.ColorA = _activeTab == 0 ? CViolet : ( _isDarkTheme ? CCardHi : Color.FromArgb(220, 220, 225));
        btnTabPhone.ColorB = _activeTab == 0 ? CViolet2 : ( _isDarkTheme ? Color.FromArgb(52, 55, 70) : Color.FromArgb(240, 240, 245));
        
        btnTabMap.ColorA = _activeTab == 1 ? CViolet : ( _isDarkTheme ? CCardHi : Color.FromArgb(220, 220, 225));
        btnTabMap.ColorB = _activeTab == 1 ? CViolet2 : ( _isDarkTheme ? Color.FromArgb(52, 55, 70) : Color.FromArgb(240, 240, 245));
        
        btnTabCountry.ColorA = _activeTab == 2 ? CViolet : ( _isDarkTheme ? CCardHi : Color.FromArgb(220, 220, 225));
        btnTabCountry.ColorB = _activeTab == 2 ? CViolet2 : ( _isDarkTheme ? Color.FromArgb(52, 55, 70) : Color.FromArgb(240, 240, 245));

        btnTabDnc.ColorA = _activeTab == 3 ? CViolet : ( _isDarkTheme ? CCardHi : Color.FromArgb(220, 220, 225));
        btnTabDnc.ColorB = _activeTab == 3 ? CViolet2 : ( _isDarkTheme ? Color.FromArgb(52, 55, 70) : Color.FromArgb(240, 240, 245));

        btnTabPhone.Invalidate(); btnTabMap.Invalidate(); btnTabCountry.Invalidate(); btnTabDnc.Invalidate();
    }

    private void UpdateLocations()
    {
        if (cmbCountry.SelectedIndex == 0) txtLocations.Text = string.Join(Environment.NewLine, LocationData.GetUSLocations());
        else txtLocations.Text = string.Join(Environment.NewLine, LocationData.GetCanadaLocations());
    }

    private void BuildStatsRow()
    {
        int y = 355, h = 100, gap = 16, x0 = 28;
        int w = (ClientSize.Width - 2 * x0 - 3 * gap) / 4;
        cardTotal  = new StatCard { Caption = "TOTAL ROWS",   Glyph = "◈", Accent = CViolet, Location = new Point(x0, y), Size = new Size(w, h) };
        cardFound  = new StatCard { Caption = "FOUND",        Glyph = "✓", Accent = CGreen,  Location = new Point(x0 + (w + gap), y), Size = new Size(w, h) };
        cardMissing = new StatCard { Caption = "MISSING",     Glyph = "✗", Accent = CRed,    Location = new Point(x0 + 2 * (w + gap), y), Size = new Size(w, h) };
        cardRate   = new StatCard { Caption = "SUCCESS RATE", Glyph = "%", Accent = CBlue,   Location = new Point(x0 + 3 * (w + gap), y), Size = new Size(w, h) };
        Controls.Add(cardTotal); Controls.Add(cardFound); Controls.Add(cardMissing); Controls.Add(cardRate);
    }

    private void BuildProgress()
    {
        statusPill = new StatusPill { Location = new Point(28, 475), Text = "Ready." };
        Controls.Add(statusPill);

        lblProgressPct = new Label { Text = "0%", Left = ClientSize.Width - 78, Top = 475, Width = 50, Height = 20, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = CSub, TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        Controls.Add(lblProgressPct);

        progressBar = new GradientProgress { Left = 28, Top = 503, Width = ClientSize.Width - 56, Height = 12, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        Controls.Add(progressBar);
    }

    private void BuildLog()
    {
        logCard = new SectionCard { Left = 28, Top = 530, Width = ClientSize.Width - 56, Height = ClientSize.Height - 530 - 18, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
        Controls.Add(logCard);

        lblLogHeader = new Label { Text = "ACTIVITY LOG", Left = 20, Top = 12, Width = 300, Height = 16, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = CSub, BackColor = Color.Transparent };
        logCard.Controls.Add(lblLogHeader);

        btnClearLog = new ModernButton { Text = "Clear", ColorA = CCardHi, ColorB = Color.FromArgb(52, 55, 70), TextOnly = true, Size = new Size(64, 24) };
        btnClearLog.Font = new Font("Segoe UI", 8f, FontStyle.Bold);
        btnClearLog.Click += (s, e) => { lock(_stateLock) { _tabStates[_activeTab].LogBuilder.Clear(); } txtLog.Clear(); };
        logCard.Controls.Add(btnClearLog);
        logCard.Resize += (s, e) => btnClearLog.Location = new Point(logCard.Width - 20 - btnClearLog.Width, 10);

        txtLog = new RichTextBox { Left = 20, Top = 40, Width = logCard.Width - 40, Height = logCard.Height - 58, Multiline = true, ReadOnly = true, ScrollBars = RichTextBoxScrollBars.Vertical, Font = new Font("Consolas", 9.5f), BorderStyle = BorderStyle.None, BackColor = CBgDeep, ForeColor = CText, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
        logCard.Controls.Add(txtLog);
        logCard.Resize += (s, e) => { txtLog.Width = logCard.Width - 40; txtLog.Height = logCard.Height - 58; };
    }

    private void BtnSelect_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", InitialDirectory = Directory.Exists(_lastFolder) ? _lastFolder : "" };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            _inputPath = ofd.FileName; _lastFolder = Path.GetDirectoryName(_inputPath) ?? "";
            SaveSettings();
            string preview = Path.GetFileName(_inputPath);
            try { int lines = File.ReadLines(_inputPath).Take(500000).Count(); preview += $"    ·    {Math.Max(0, lines - 1):N0} rows"; } catch { }
            lblFile.Text = preview; lblFile.ForeColor = CText; btnStart.Enabled = true;
        }
    }

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        if (_tabStates[0].IsRunning) return;
        if (string.IsNullOrEmpty(_inputPath) || !File.Exists(_inputPath)) { MessageBox.Show("Please select a CSV file first."); return; }
        using var sfd = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = Path.GetFileNameWithoutExtension(_inputPath) + "_with_contacts.csv", InitialDirectory = Directory.Exists(_lastFolder) ? _lastFolder : "" };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        _tabStates[0].IsRunning = true; _tabStates[0].Cts = new CancellationTokenSource(); UpdateUIState();
        SetProgress(0, 0, 1); SetStats(0, 0, 0, 0); SetStatus(0, "Working...", CAmber);

        try
        {
            await ProcessCsvAsync(0, _inputPath, sfd.FileName, (int)numMaxPages.Value);
            MessageBox.Show("Done!\nOutput saved to:\n" + sfd.FileName, "Success");
            SetStatus(0, $"Done.", CGreen);
        }
        catch (OperationCanceledException) { LogLine(0, "═══ CANCELLED BY USER ═══", CAmber); SetStatus(0, "Cancelled.", CAmber); }
        catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "Error"); SetStatus(0, "Error.", CRed); }
        finally { _tabStates[0].IsRunning = false; _tabStates[0].Cts?.Dispose(); _tabStates[0].Cts = null; UpdateUIState(); }
    }

    private async void BtnScrapeMaps_Click(object? sender, EventArgs e)
    {
        if (_tabStates[1].IsRunning) return;

        var keywords = txtKeywords.Lines.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()).ToList();
        var locations = txtLocations.Lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()).ToList();
        string country = cmbCountry.SelectedItem?.ToString() ?? "United States";

        if (keywords.Count == 0 || locations.Count == 0) { MessageBox.Show("Please enter both keywords and locations."); return; }

        var queries = new List<string>();
        foreach (var kw in keywords) { foreach (var loc in locations) { queries.Add($"{kw} in {loc}, {country}"); } }

        using var sfd = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "maps_bulk_results.csv" };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        _tabStates[1].IsRunning = true; _tabStates[1].Cts = new CancellationTokenSource(); UpdateUIState();
        SetStats(1, queries.Count, 0, 0); SetStatus(1, "Working...", CAmber);

        try
        {
            var mapScraper = new MapScraperService();
            var mapCts = _tabStates[1].Cts ?? throw new InvalidOperationException("Map scraper started without a CancellationTokenSource.");
            
            // STEP 1: MAP SCRAPER
            await mapScraper.ScrapeMultipleAsync(queries, sfd.FileName, 
                (msg, color) => LogLine(1, msg, color), 
                (done, total, extracted, failedQueries) => 
                {
                    SetProgress(1, done, total);
                    SetStats(1, total, extracted, failedQueries); 
                }, 
                mapCts.Token);

            string currentCsv = sfd.FileName;

            // STEP 2: PHONE & EMAIL SCRAPER
            if (chkRunWebScrape.Checked)
            {
                LogLine(1, "═══ STARTING PHONE & EMAIL SCRAPER ═══", CViolet2);
                string webScrapedCsv = Path.Combine(Path.GetDirectoryName(currentCsv) ?? "", Path.GetFileNameWithoutExtension(currentCsv) + "_with_contacts.csv");
                
                SetProgress(1, 0, 1); SetStats(1, 0, 0, 0); SetStatus(1, "Scraping Phone/Email...", CAmber);
                await ProcessCsvAsync(1, currentCsv, webScrapedCsv, 15);
                currentCsv = webScrapedCsv;
            }

            // STEP 3: COUNTRY DETECTOR
            if (chkRunCountryDetect.Checked)
            {
                LogLine(1, "═══ STARTING COUNTRY DETECTOR ═══", CViolet2);
                string analyzedCsv = Path.Combine(Path.GetDirectoryName(currentCsv) ?? "", Path.GetFileNameWithoutExtension(currentCsv) + "_analyzed.csv");
                
                SetProgress(1, 0, 1); SetStats(1, 0, 0, 0); SetStatus(1, "Detecting Countries...", CAmber);
                await Task.Run(() => ProcessCountryAnalysis(1, currentCsv, analyzedCsv, mapCts.Token));
                currentCsv = analyzedCsv;
            }

            // STEP 4: DNC VALIDATOR
            if (chkRunDnc.Checked)
            {
                LogLine(1, "═══ STARTING DNC VALIDATOR ═══", CViolet2);
                string dncCsv = Path.Combine(Path.GetDirectoryName(currentCsv) ?? "", Path.GetFileNameWithoutExtension(currentCsv) + "_dnc.csv");
                
                SetProgress(1, 0, 1); SetStats(1, 0, 0, 0); SetStatus(1, "Running DNC Validator...", CAmber);
                
                var dncService = new DncValidatorService();
                await dncService.RunAsync(currentCsv, dncCsv,
                    (msg, color) => LogLine(1, msg, color),
                    (done, total) => 
                    {
                        SetProgress(1, done, total);
                        SetStats(1, total, done, 0);
                    },
                    mapCts.Token);

                currentCsv = dncCsv;
            }

            MessageBox.Show($"All Selected Tasks Complete!\nFinal output saved to:\n" + currentCsv, "Success");
            SetStatus(1, "Done.", CGreen);
        }
        catch (OperationCanceledException) { LogLine(1, "═══ CANCELLED BY USER ═══", CAmber); SetStatus(1, "Cancelled.", CAmber); }
        catch (Exception ex) { MessageBox.Show("Map Scraper Error: " + ex.Message, "Error"); SetStatus(1, "Error.", CRed); }
        finally { _tabStates[1].IsRunning = false; _tabStates[1].Cts?.Dispose(); _tabStates[1].Cts = null; UpdateUIState(); }
    }

    private async void BtnDetect_Click(object? sender, EventArgs e)
    {
        if (_tabStates[2].IsRunning) return;

        using var ofd = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", InitialDirectory = Directory.Exists(_lastFolder) ? _lastFolder : "" };
        if (ofd.ShowDialog() != DialogResult.OK) return;
        using var sfd = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = Path.GetFileNameWithoutExtension(ofd.FileName) + "_analyzed.csv", InitialDirectory = Directory.Exists(_lastFolder) ? _lastFolder : "" };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        _tabStates[2].IsRunning = true; _tabStates[2].Cts = new CancellationTokenSource(); UpdateUIState();
        SetStatus(2, "Working...", CAmber);

        try
        {
            LogLine(2, "═══ COUNTRY DETECTOR STARTED ═══", CViolet2);
            var countryCts = _tabStates[2].Cts ?? throw new InvalidOperationException("Country detector started without a CancellationTokenSource.");
            await Task.Run(() => ProcessCountryAnalysis(2, ofd.FileName, sfd.FileName, countryCts.Token));
            LogLine(2, $"✔ Saved: {sfd.FileName}", CGreen);
            MessageBox.Show("Analysis Complete!\nOutput saved to:\n" + sfd.FileName, "Success");
            SetStatus(2, "Done.", CGreen);
        }
        catch (OperationCanceledException) { LogLine(2, "═══ CANCELLED BY USER ═══", CAmber); SetStatus(2, "Cancelled.", CAmber); }
        catch (Exception ex) { MessageBox.Show("Error: " + ex.Message, "Error"); SetStatus(2, "Error.", CRed); }
        finally { _tabStates[2].IsRunning = false; _tabStates[2].Cts?.Dispose(); _tabStates[2].Cts = null; UpdateUIState(); }
    }

    private void BtnSelectDnc_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", InitialDirectory = Directory.Exists(_lastFolder) ? _lastFolder : "" };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            lblDncFile.Text = ofd.FileName; 
            lblDncFile.ForeColor = CText; 
            btnStartDnc.Enabled = true;
        }
    }

    private async void BtnStartDnc_Click(object? sender, EventArgs e)
    {
        if (_tabStates[3].IsRunning) return;
        if (string.IsNullOrEmpty(lblDncFile.Text) || lblDncFile.Text == "no file selected") { MessageBox.Show("Please select a CSV file first."); return; }
        
        using var sfd = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = Path.GetFileNameWithoutExtension(lblDncFile.Text) + "_dnc_checked.csv" };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        _tabStates[3].IsRunning = true; _tabStates[3].Cts = new CancellationTokenSource(); UpdateUIState();
        SetProgress(3, 0, 1); SetStats(3, 0, 0, 0); SetStatus(3, "Working...", CAmber);

        try
        {
            var dncService = new DncValidatorService();
            var dncCts = _tabStates[3].Cts ?? throw new InvalidOperationException("DNC Validator started without a CancellationTokenSource.");

            await dncService.RunAsync(lblDncFile.Text, sfd.FileName,
                (msg, color) => LogLine(3, msg, color),
                (done, total) => 
                {
                    SetProgress(3, done, total);
                    SetStats(3, total, done, 0);
                },
                dncCts.Token);

            MessageBox.Show($"DNC Validation Complete!\nFinal output saved to:\n" + sfd.FileName, "Success");
            SetStatus(3, "Done.", CGreen);
        }
        catch (OperationCanceledException) { LogLine(3, "═══ CANCELLED BY USER ═══", CAmber); SetStatus(3, "Cancelled.", CAmber); }
        catch (Exception ex) { MessageBox.Show("DNC Validator Error: " + ex.Message, "Error"); SetStatus(3, "Error.", CRed); }
        finally { _tabStates[3].IsRunning = false; _tabStates[3].Cts?.Dispose(); _tabStates[3].Cts = null; UpdateUIState(); }
    }

    private async Task ProcessCsvAsync(int tabIndex, string inputPath, string outputPath, int maxPages)
    {
        var log = new StringBuilder();
        void Log(string line) { log.AppendLine(line); LogLine(tabIndex, line, CText); }
        var (headers, rows) = CsvService.Read(inputPath);

        Log("═══ HEADERS FOUND ═══"); for (int i = 0; i < headers.Count; i++) Log($"  [{i}] '{headers[i]}'"); Log($"Total rows: {rows.Count}  |  MaxPagesPerSite={maxPages}");
        if (rows.Count == 0) { Log("CSV has no data rows."); MessageBox.Show("CSV file is empty."); return; }
        
        int urlIndex = headers.FindIndex(h => h.Trim().Equals("Website URL", StringComparison.OrdinalIgnoreCase));
        if (urlIndex < 0) { Log("ERROR: No column named 'Website URL' found."); MessageBox.Show("CSV must contain a 'Website URL' column."); return; }

        bool hasPhoneCol = headers.Any(h => h.Equals("Phone Number", StringComparison.OrdinalIgnoreCase));
        string targetColumn = hasPhoneCol ? "Additional Phone Number" : "Phone Number";
        int targetIndex = headers.FindIndex(h => h.Equals(targetColumn, StringComparison.OrdinalIgnoreCase));
        if (targetIndex < 0) { headers.Add(targetColumn); targetIndex = headers.Count - 1; }

        bool hasEmailCol = headers.Any(h => h.Equals("Email", StringComparison.OrdinalIgnoreCase));
        string emailColumn = hasEmailCol ? "Additional Email" : "Email";
        int emailIndex = headers.FindIndex(h => h.Equals(emailColumn, StringComparison.OrdinalIgnoreCase));
        if (emailIndex < 0) { headers.Add(emailColumn); emailIndex = headers.Count - 1; }

        Log($"Writing results to columns: '{targetColumn}', '{emailColumn}'"); Log(""); foreach (var row in rows) while (row.Count < headers.Count) row.Add("");
        
        using var writer = CsvService.CreateWriter(outputPath); 
        writer.WriteLine(string.Join(",", headers.Select(CsvService.Escape))); 
        writer.Flush();
        
        var urlCache = new ConcurrentDictionary<string, Lazy<Task<SiteResult>>>(StringComparer.OrdinalIgnoreCase);
        using var siteSlots = new SemaphoreSlim(MaxConcurrentSites);
        int total = rows.Count, done = 0, success = 0, failed = 0, cachedCount = 0; SetProgress(tabIndex, 0, total); SetStats(tabIndex, total, 0, 0);
        
        string Describe(SiteResult r) => (string.IsNullOrEmpty(r.Phones) ? "" : "phone: " + r.Phones) + (!string.IsNullOrEmpty(r.Phones) && !string.IsNullOrEmpty(r.Emails) ? " | " : "") + (string.IsNullOrEmpty(r.Emails) ? "" : "email: " + r.Emails);
        
        async Task ProcessRowAsync(List<string> row, int current)
        {
            var url = row[urlIndex].Trim();
            if (string.IsNullOrWhiteSpace(url)) { LogLine(tabIndex, $"[{current}/{total}] (empty URL)", CSub); Interlocked.Increment(ref done); SetProgress(tabIndex, done, total); SetStats(tabIndex, total, success, failed); return; }
            
            await siteSlots.WaitAsync();
            try
            {
                _tabStates[tabIndex].Cts!.Token.ThrowIfCancellationRequested();
                var fresh = new Lazy<Task<SiteResult>>(() => _scraper.FindPhoneFastAsync(url, maxPages));
                var lazy = urlCache.GetOrAdd(url, fresh); bool isCached = !ReferenceEquals(lazy, fresh);
                
                try
                {
                    var result = await lazy.Value; row[targetIndex] = result.Phones; row[emailIndex] = result.Emails; bool any = !string.IsNullOrEmpty(result.Phones) || !string.IsNullOrEmpty(result.Emails);
                    if (isCached) { Interlocked.Increment(ref cachedCount); if (any) Interlocked.Increment(ref success); else Interlocked.Increment(ref failed); LogLine(tabIndex, $"[{current}/{total}] CACHED {url} → {(any ? Describe(result) : "(none)")}", CBlue); }
                    else if (any) { Interlocked.Increment(ref success); LogLine(tabIndex, $"[{current}/{total}] FOUND  {url}  ({result.Pages} pages, on {result.FoundOn}) → {Describe(result)}", CGreen); }
                    else { Interlocked.Increment(ref failed); LogLine(tabIndex, $"[{current}/{total}] NONE   {url}  ({result.Pages} pages) → no contacts", CAmber); }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { Interlocked.Increment(ref failed); row[targetIndex] = ""; row[emailIndex] = ""; LogLine(tabIndex, $"[{current}/{total}] FAIL   {url} → {ex.GetType().Name}", CRed); }
            }
            finally { siteSlots.Release(); int d = Interlocked.Increment(ref done); SetProgress(tabIndex, d, total); SetStats(tabIndex, total, success, failed); }
        }

        var rowTasks = new List<Task>(total); 
        for (int i = 0; i < total; i++) 
        {
            int idx = i;
            rowTasks.Add(Task.Run(async () => 
            {
                await ProcessRowAsync(rows[idx], idx + 1);
                
                lock (writer) 
                {
                    writer.WriteLine(string.Join(",", rows[idx].Select(CsvService.Escape)));
                    writer.Flush();
                }
            }));
        }
        
        await Task.WhenAll(rowTasks);
        
        LogLine(tabIndex, $"═══ DONE ═══  {success} with contacts, {failed} without, {cachedCount} from cache.", CGreen);
        File.WriteAllText(outputPath + ".log", log.ToString(), Encoding.UTF8);
    }

    private void ProcessCountryAnalysis(int tabIndex, string inputPath, string outputPath, CancellationToken token)
    {
        void Log(string line, Color color) => LogLine(tabIndex, line, color);
        var (headers, rows) = CsvService.Read(inputPath);
        if (rows.Count == 0) { Log("CSV has no data rows.", CAmber); return; }
        
        int phoneIndex = headers.FindIndex(h => h.Trim().Equals("Phone Number", StringComparison.OrdinalIgnoreCase));
        if (phoneIndex < 0) phoneIndex = headers.FindIndex(h => h.Trim().Equals("Additional Phone Number", StringComparison.OrdinalIgnoreCase));
        if (phoneIndex < 0) { Log("ERROR: No column named 'Phone Number' or 'Additional Phone Number' found.", CRed); MessageBox.Show("CSV must contain a 'Phone Number' column."); return; }
        
        string countryCol = "Detected Country";
        int countryIndex = headers.FindIndex(h => h.Equals(countryCol, StringComparison.OrdinalIgnoreCase));
        if (countryIndex < 0) { headers.Add(countryCol); countryIndex = headers.Count - 1; }
        foreach (var row in rows) while (row.Count < headers.Count) row.Add("");
        
        using var writer = CsvService.CreateWriter(outputPath); 
        writer.WriteLine(string.Join(",", headers.Select(CsvService.Escape))); 
        writer.Flush();
        
        int total = rows.Count, current = 0, detected = 0, unknown = 0;
        
        SetProgress(tabIndex, 0, total);
        
        foreach (var row in rows)
        {
            token.ThrowIfCancellationRequested();
            current++; string phones = row[phoneIndex]; string detectedCountry = "";
            if (!string.IsNullOrWhiteSpace(phones))
            {
                var phoneList = phones.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries); var countries = new HashSet<string>();
                foreach (var phone in phoneList) { string country = CountryDetectorService.Detect(phone.Trim()); if (country != "Unknown" && country != "Invalid") countries.Add(country); }
                if (countries.Count > 0) { detectedCountry = string.Join(" / ", countries); detected++; } 
                else { detectedCountry = "Unknown/Invalid"; unknown++; }
            }
            else { unknown++; }
            
            row[countryIndex] = detectedCountry; 
            writer.WriteLine(string.Join(",", row.Select(CsvService.Escape))); 
            writer.Flush();
            
            if (current % 500 == 0 || current == total)
            {
                SetProgress(tabIndex, current, total);
                SetStats(tabIndex, total, detected, unknown);
                Log($"  Processed {current:N0} / {total:N0} rows…", CSub);
            }
        }
        Log($"═══ DONE ═══ Detected countries for {detected:N0} rows.", CGreen);
    }

    private void Drag(object? s, MouseEventArgs e) { if (e.Button == MouseButtons.Left && WindowState != FormWindowState.Maximized) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); } }
    private void ToggleMaximize() { WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; }
    
    protected override void OnResize(EventArgs e) { base.OnResize(e); Region = WindowState == FormWindowState.Maximized ? null : new Region(RoundRect(new Rectangle(0, 0, Width, Height), WinRadius)); }
    protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using var pen = new Pen(CBorder, 1); e.Graphics.DrawPath(pen, RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), WinRadius)); }
    protected override void OnPaintBackground(PaintEventArgs e) { var g = e.Graphics; using var bg = new LinearGradientBrush(ClientRectangle, CBg, CBgDeep, 90f); g.FillRectangle(bg, ClientRectangle); if (_isDarkTheme) { var glowPath = new GraphicsPath(); glowPath.AddEllipse(ClientSize.Width - 520, -220, 640, 400); using var glow = new PathGradientBrush(glowPath) { CenterColor = Color.FromArgb(30, CViolet), SurroundColors = new[] { Color.FromArgb(0, CViolet) }, FocusScales = new PointF(0.4f, 0.4f) }; g.FillEllipse(glow, ClientSize.Width - 520, -220, 640, 400); glowPath.Dispose(); } }

    protected override void WndProc(ref Message m)
    {
        const int WM_NCHITTEST = 0x84;
        if (m.Msg == WM_NCHITTEST && WindowState == FormWindowState.Normal)
        {
            var p = PointToClient(Cursor.Position); int w = ClientSize.Width, h = ClientSize.Height;
            bool l = p.X < Grip, r = p.X >= w - Grip, t = p.Y < Grip, b = p.Y >= h - Grip;
            if (t && l) { m.Result = (IntPtr)13; return; } if (t && r) { m.Result = (IntPtr)14; return; }
            if (b && l) { m.Result = (IntPtr)16; return; } if (b && r) { m.Result = (IntPtr)17; return; }
            if (l) { m.Result = (IntPtr)10; return; } if (r) { m.Result = (IntPtr)11; return; }
            if (t) { m.Result = (IntPtr)12; return; } if (b) { m.Result = (IntPtr)15; return; }
        }
        base.WndProc(ref m);
    }

    private static GraphicsPath RoundRect(Rectangle rect, int radius) { var path = new GraphicsPath(); if (rect.Width <= 0 || rect.Height <= 0) return path; if (radius <= 0) { path.AddRectangle(rect); return path; } float d = radius * 2f; if (d > rect.Width) d = rect.Width; if (d > rect.Height) d = rect.Height; path.AddArc(rect.X, rect.Y, d, d, 180, 90); path.AddArc(rect.X + rect.Width - d, rect.Y, d, d, 270, 90); path.AddArc(rect.X + rect.Width - d, rect.Y + rect.Height - d, d, d, 0, 90); path.AddArc(rect.X, rect.Y + rect.Height - d, d, d, 90, 90); path.CloseFigure(); return path; }
    private static Color Lerp(Color a, Color b, float t) => Color.FromArgb((int)(a.A + (b.A - a.A) * t), (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    private Image? TryLoadImage(string folder, params string[] filenames) { foreach (var name in filenames) { string path = Path.Combine(folder, name); if (File.Exists(path)) { try { if (path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)) { using var ico = new Icon(path); return ico.ToBitmap(); } return Image.FromFile(path); } catch { } } } return null; }
    private void LoadSettings() { try { if (File.Exists(SettingsFile)) { var lines = File.ReadAllLines(SettingsFile); if (lines.Length > 0 && Directory.Exists(lines[0])) _lastFolder = lines[0]; } } catch { } }
    private void SaveSettings() { try { File.WriteAllText(SettingsFile, _lastFolder); } catch { } }

    private sealed class SectionCard : Panel { public int Radius { get; set; } = 12; public SectionCard() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true); } protected override void OnPaint(PaintEventArgs e) { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using var path = RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), Radius); using var fill = new SolidBrush(CCard); e.Graphics.FillPath(fill, path); using var pen = new Pen(CBorder, 1); e.Graphics.DrawPath(pen, path); } protected override void OnResize(EventArgs e) { base.OnResize(e); Region = new Region(RoundRect(new Rectangle(0, 0, Width, Height), Radius)); } }
    private sealed class StatCard : Control { public string Caption { get; set; } = ""; public string Value { get; set; } = "0"; public string Glyph { get; set; } = "◈"; public Color Accent { get; set; } = CViolet; public StatCard() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); } public void SetValue(string v) { Value = v; Invalidate(); } protected override void OnPaint(PaintEventArgs e) { var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; var rc = new Rectangle(0, 0, Width - 1, Height - 1); using (var path = RoundRect(rc, 12)) using (var fill = new SolidBrush(CCardHi)) g.FillPath(fill, path); using (var path = RoundRect(rc, 12)) using (var pen = new Pen(CBorder, 1)) g.DrawPath(pen, path); using var barPath = RoundRect(new Rectangle(0, 0, Width, 5), 4); using var bar = new LinearGradientBrush(new Rectangle(0, 0, Width, 5), Accent, Color.FromArgb(0, Accent), 0f); g.FillPath(bar, barPath); using var capFont = new Font("Segoe UI", 8f, FontStyle.Bold); TextRenderer.DrawText(g, Caption.ToUpperInvariant(), capFont, new Rectangle(18, 16, Width - 70, 16), CSub, TextFormatFlags.Left | TextFormatFlags.VerticalCenter); using var valFont = new Font("Segoe UI", 24f, FontStyle.Bold); TextRenderer.DrawText(g, Value, valFont, new Rectangle(16, 34, Width - 60, Height - 40), CText, TextFormatFlags.Left | TextFormatFlags.VerticalCenter); int d = 34, gx = Width - d - 16, gy = 16; using var chipPath = RoundRect(new Rectangle(gx, gy, d, d), 9); using var chip = new SolidBrush(Color.FromArgb(38, Accent)); g.FillPath(chip, chipPath); using var glyphFont = new Font("Segoe UI", 12f, FontStyle.Bold); TextRenderer.DrawText(g, Glyph, glyphFont, new Rectangle(gx, gy, d, d), Accent, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); } }
    private sealed class ModernButton : Control { private float _t; private bool _hover, _down; private readonly System.Windows.Forms.Timer _anim; public Color ColorA { get; set; } = CViolet; public Color ColorB { get; set; } = CViolet2; public int Radius { get; set; } = 10; public bool TextOnly { get; set; } public ModernButton() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Size = new Size(150, 36); Cursor = Cursors.Hand; Font = new Font("Segoe UI", 9f, FontStyle.Bold); _anim = new System.Windows.Forms.Timer { Interval = 15 }; _anim.Tick += (s, e) => { float target = (_hover || _down) ? 1f : 0f; _t += Math.Sign(target - _t) * 0.14f; if (Math.Abs(target - _t) < 0.02f) _t = target; Invalidate(); }; MouseEnter += (s, e) => { _hover = true; _anim.Start(); }; MouseLeave += (s, e) => { _hover = false; _down = false; _anim.Start(); }; MouseDown += (s, e) => { _down = true; Invalidate(); }; MouseUp += (s, e) => { _down = false; Invalidate(); }; } protected override void OnPaint(PaintEventArgs e) { var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; var rc = new Rectangle(0, 0, Width - 1, Height - 1); if (!Enabled) { using var path = RoundRect(rc, Radius); using var fill = new SolidBrush(_isDarkTheme ? Color.FromArgb(34, 36, 46) : Color.FromArgb(230, 230, 235)); g.FillPath(fill, path); TextRenderer.DrawText(g, Text, Font, ClientRectangle, _isDarkTheme ? Color.FromArgb(110, 115, 128) : Color.FromArgb(160, 160, 165), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); return; } float lift = 0.16f * _t - (_down ? 0.10f : 0f); var a = Lerp(ColorA, Color.White, lift); var b = Lerp(ColorB, Color.White, lift); using (var path = RoundRect(rc, Radius)) using (var grad = new LinearGradientBrush(rc, a, b, 35f)) g.FillPath(grad, path); TextRenderer.DrawText(g, Text, Font, ClientRectangle, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); } }
    private sealed class GradientProgress : Control { private int _value; private int _max = 1; public int Value { get => _value; set { _value = Math.Min(Math.Max(0, value), _max); Invalidate(); } } public int Maximum { get => _max; set { _max = Math.Max(1, value); Invalidate(); } } public GradientProgress() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Height = 12; } protected override void OnPaint(PaintEventArgs e) { var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; using (var track = RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), Height / 2)) using (var tb = new SolidBrush(_isDarkTheme ? Color.FromArgb(38, 41, 54) : Color.FromArgb(220, 220, 225))) g.FillPath(tb, track); int fillW = (int)(Width * ((float)_value / _max)); if (fillW <= 4) return; var rc = new Rectangle(0, 0, fillW, Height); using var path = RoundRect(rc, Height / 2); using var grad = new LinearGradientBrush(rc, CViolet, CCyan, 0f); g.FillPath(grad, path); } }
    private sealed class StatusPill : Control { private string _label = ""; public Color Dot { get; private set; } = CCyan; public StatusPill() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true); Height = 22; Width = 260; } public void Set(string text, Color dot) { _label = text; Dot = dot; Invalidate(); } protected override void OnPaint(PaintEventArgs e) { var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; int w = Width - 1, h = Height - 1; using (var path = RoundRect(new Rectangle(0, 0, w, h), h / 2)) using (var fill = new SolidBrush(CCardHi)) g.FillPath(fill, path); using (var path = RoundRect(new Rectangle(0, 0, w, h), h / 2)) using (var pen = new Pen(CBorder, 1)) g.DrawPath(pen, path); int d = 8, dx = 14, dy = (Height - d) / 2; using var dotBrush = new SolidBrush(Dot); g.FillEllipse(dotBrush, dx, dy, d, d); using var f = new Font("Segoe UI", 8.5f); TextRenderer.DrawText(g, _label, f, new Rectangle(dx + d + 8, 0, Width - dx - d - 20, Height), CSub, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis); } }
    private sealed class WindowButton : Control { private bool _hover; public string Glyph { get; set; } = "✕"; public bool IsClose { get; set; } public WindowButton() { SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true); Size = new Size(36, 30); Cursor = Cursors.Hand; Font = new Font("Segoe UI", 9.5f); BackColor = Color.Transparent; MouseEnter += (s, e) => { _hover = true; Invalidate(); }; MouseLeave += (s, e) => { _hover = false; Invalidate(); }; } protected override void OnPaint(PaintEventArgs e) { var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; if (_hover) { Color c = IsClose ? Color.FromArgb(232, 61, 74) : (_isDarkTheme ? Color.FromArgb(55, 58, 72) : Color.FromArgb(220, 220, 225)); using var path = RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), 8); using var fill = new SolidBrush(c); g.FillPath(fill, path); } TextRenderer.DrawText(g, Glyph, Font, ClientRectangle, _hover ? Color.White : CSub, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); } }
}