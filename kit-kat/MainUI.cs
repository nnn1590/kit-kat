using CefSharp;
using CefSharp.WinForms;
using kit_kat.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace kit_kat
{
    public partial class MainUI : Form
    {
        #region General Application Handling

        #region Drag Function Variables
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        #endregion
        #region Dropshadow

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn
        (
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
        );

        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        public static extern int DwmIsCompositionEnabled(ref int pfEnabled);

        private bool m_aeroEnabled;
        private const int CS_DROPSHADOW = 0x00020000;
        private const int WM_NCPAINT = 0x0085;
        private const int WM_ACTIVATEAPP = 0x001C;

        public struct MARGINS
        {
            public int leftWidth;
            public int rightWidth;
            public int topHeight;
            public int bottomHeight;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                m_aeroEnabled = CheckAeroEnabled();

                CreateParams cp = base.CreateParams;
                if (!m_aeroEnabled)
                    cp.ClassStyle |= CS_DROPSHADOW;

                return cp;
            }
        }

        private bool CheckAeroEnabled()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                int enabled = 0;
                DwmIsCompositionEnabled(ref enabled);
                return (enabled == 1) ? true : false;
            }
            return false;
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_NCPAINT:
                    if (m_aeroEnabled)
                    {
                        var v = 2;
                        DwmSetWindowAttribute(Handle, 2, ref v, 4);
                        MARGINS margins = new MARGINS()
                        {
                            bottomHeight = 1,
                            leftWidth = 0,
                            rightWidth = 0,
                            topHeight = 0
                        };
                        DwmExtendFrameIntoClientArea(Handle, ref margins);

                    }
                    break;
                default:
                    break;
            }
            base.WndProc(ref m);

        }

        #endregion

        #endregion
        #region Variables
        public static Form mForm = null;
        static public ChromiumWebBrowser ui;
        #endregion

        public MainUI()
        {
            delLog = new LogDelegate(log);
            Program.viewer.Connected += onConnect;
            InitializeComponent();
            ui = new ChromiumWebBrowser("file:///C:/Users/impra/Desktop/kit-kat/kit-kat/bin/x86/Debug/ui/index.html") { KeyboardHandler = new KeyboardHandler() };
            ui.RegisterJsObject("callbackObj", new mainFunctions());
            Controls.Add(ui);
            #region Once loaded
            bool loaded = false;
            ui.LoadingStateChanged += (sender, args) =>
            {
                if (args.IsLoading == false && !loaded)
                {
                    loaded = true;
                    ui.LoadingStateChanged += null;
                    #region Load Version
                    ExecuteOnScope("document.getElementById('version').innerHTML='" + FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.CodeBase.Replace("file:///", "")).ProductVersion + "';");
                    #endregion
                    #region Load Settings
                    ExecuteOnScope("settings.ipAddress='" + Settings.Default.IPAddress + "';", true);
                    ExecuteOnScope("settings.ntr.autoConnect=" + Settings.Default.AutoConnect.ToString().ToLower() + ";", true);
                    ExecuteOnScope("settings.ntr.showConsole=" + Settings.Default.ShowConsole.ToString().ToLower() + ";", true);
                    ExecuteOnScope("settings.ntr.tScale=" + Settings.Default.tScale + ";", true);
                    ExecuteOnScope("settings.ntr.bScale=" + Settings.Default.bScale + ";", true);
                    ExecuteOnScope("settings.ntr.priority=" + Settings.Default.ScreenPriority.ToString() + ";", true);
                    ExecuteOnScope("settings.ntr.priorityFactor=" + Settings.Default.PriorityFactor.ToString() + ";", true);
                    ExecuteOnScope("settings.ntr.viewMode=" + Settings.Default.ViewMode.ToString() + ";", true);
                    ExecuteOnScope("settings.ntr.quality=" + Settings.Default.Quality.ToString() + ";", true);
                    ExecuteOnScope("settings.ntr.QoS=" + Settings.Default.QOSValue.ToString() + ";", true);
                    #endregion
                    #region AutoConnect if Enabled
                    if (Settings.Default.AutoConnect) { new mainFunctions().connectToNTR(Settings.Default.IPAddress); }
                    #endregion
                    #region Load Changelog
                    openChangelog();
                    #endregion
                }
            };
            #endregion
        }

        // OnLoad/OnClose & Handlers
        #region OnLoad
        private void MainUI_Load(object sender, EventArgs e)
        {
            mForm = Application.OpenForms[0];
        }
        #endregion
        #region onConnect
        public void onConnect(object sender, EventArgs e)
        {
            #region Tell UI, NTR is Connected
            ExecuteOnScope("settings.connected=true", true);
            #endregion
            #region Tell NTR Viewer to start capturing (Send NTR Settings)
            Program.viewer.sendEmptyPacket(901, (uint)Settings.Default.ScreenPriority << 8 | (uint)Settings.Default.PriorityFactor, (uint)Settings.Default.Quality, (uint)(Settings.Default.QOSValue * 1024 * 1024 / 8));
            #endregion
            #region Open NTRViewer (Set Viewer Settings)
            if (File.Exists(Path.Combine(Path.GetTempPath(), "NTRViewer.exe")))
            {
                try
                {
                    ProcessStartInfo p = new ProcessStartInfo(Path.Combine(Path.GetTempPath(), "NTRViewer.exe"))
                    {
                        Verb = "runas",
                        Arguments = ("-l " + Settings.Default.ViewMode.ToString() + " -t " + Settings.Default.tScale + " -b " + Settings.Default.bScale).Replace(',', '.')
                    };
                    if (!Settings.Default.ShowConsole)
                    {
                        p.UseShellExecute = false;
                        p.CreateNoWindow = true;
                    }
                    Process.Start(p);
                }
                catch (Exception err)
                {
                    log(err.Message);
                }
            }
            else
            {
                log("NTRViewer failed to extract, try downloading and running NTRViewer manually as an Administrator.");
            }
            #endregion
            #region Run the Batch File
            if (Settings.Default.BatchFile != string.Empty) { Process.Start(Settings.Default.BatchFile); }
            #endregion
        }
        #endregion
        #region Log Handler
        public delegate void LogDelegate(string msg);
        public LogDelegate delLog;
        public string fullLog = "Created by PRAGMA\ntwitter.com/PRAGMA\nEnjoy!\n\n";
        public string lastlog;
        public void log(string msg)
        {
            lastlog = msg;
            if (!msg.EndsWith("\n")) msg += "\r\n\r\n";
            if (msg != "") {
                fullLog += msg;
                sendLog(fullLog);
            }
            return;
        }
        #region actualLog
        public static void sendLog(string message)
        {
            ExecuteOnScope("settings.log = '" + message.Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r") + "';", true);
        }
        #endregion
        #endregion
        #region OnFormClose
        private void MainUI_FormClosed(object sender, FormClosedEventArgs e)
        {
            #region Shut down NTRViewer
            foreach (Process p in Process.GetProcessesByName("NTRViewer"))
            {
                p.Kill();
                p.WaitForExit();
            }
            #endregion
            #region Remove Extracted files
            File.Delete(Path.Combine(Path.GetTempPath(), "NTRViewer.exe"));
            File.Delete(Path.Combine(Path.GetTempPath(), "SDL2.dll"));
            File.Delete(Path.Combine(Path.GetTempPath(), "turbojpeg.dll"));
            #endregion
        }
        #endregion
        #region Heartbeat Handler
        private void Heartbeat_Tick(object sender, EventArgs e)
        {
            try
            {
                Program.viewer.sendHeartbeatPacket();
            }
            catch (Exception)
            {
            }
        }
        #endregion

        // UI Related Functions
        #region openChangelog
        public static void openChangelog()
        {
            string lastVersion = Settings.Default.lastUpdate;
            string currentVersion = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.CodeBase.Replace("file:///", "")).ProductVersion;
            if (lastVersion == "")
            {
                Settings.Default.lastUpdate = currentVersion;
                Settings.Default.Save();
            }
            else
            {
                if (int.Parse(currentVersion.Replace(".", "")) > int.Parse(lastVersion.Replace(".", "")))
                {
                    ExecuteOnScope("window.clCurrv='v" + currentVersion + "';");
                    ExecuteOnScope("showChangelog();", true);
                    Settings.Default.lastUpdate = currentVersion;
                    Settings.Default.Save();
                }
            }
        }
        #endregion
        #region executeOnScope
        public static void ExecuteOnScope(string js, bool asScope = false)
        {
            try
            {
                if (asScope)
                {
                    ui.ExecuteScriptAsync("angular.element(document.getElementById('bodytag')).scope().$apply(function() { angular.element(document.getElementById('bodytag')).scope()." + js + " });");
                }
                else
                {
                    ui.ExecuteScriptAsync("angular.element(document.getElementById('bodytag')).scope().$apply(function() { " + js + " });");
                }
            }
            catch (Exception)
            {
            }
        }
        #endregion

        // UI View Methods
        public class mainFunctions
        {
            #region ControlBox
            public void closeButton()
            {
                mForm.Invoke(new MethodInvoker(() => mForm.Close()));
            }
            public void minimizeButton()
            {
                mForm.Invoke(new MethodInvoker(() => mForm.WindowState = FormWindowState.Minimized));
            }
            public void helpButton()
            {
                Process.Start("https://www.youtube.com/watch?v=wNfV6A44nMw");
            }
            #endregion
            #region formDrag
            public void formDrag()
            {
                try
                {
                    if (MouseButtons == MouseButtons.Left)
                    {
                        mForm.Invoke((MethodInvoker)delegate
                        {
                            ReleaseCapture();
                            SendMessage(mForm.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                        });
                    }
                }
                catch (Exception)
                {
                }
            }
            #endregion

            #region ConnectToNTR
            public void connectToNTR(string host)
            {
                // Store IP Address
                Settings.Default["IPAddress"] = host;
                Settings.Default.Save();

                // Make sure NTRViewer is closed
                foreach (Process p in Process.GetProcessesByName("NTRViewer")) { p.Kill(); p.WaitForExit(); }
                    
                // Connect to Server
                Program.viewer.setServer(host, 8000);
                Program.viewer.connectToServer();
                Program.viewer.sendEmptyPacket(5);
            }
            #endregion
            #region DisconnectFromNTR
            public void disconnectFromNTR()
            {
                Program.viewer.disconnect();
                ExecuteOnScope("settings.connected=false", true);
                #region Shut down NTRViewer
                foreach (Process p in Process.GetProcessesByName("NTRViewer"))
                {
                    p.Kill();
                    p.WaitForExit();
                }
                #endregion
            }
            #endregion
            #region storeSettings
            public void storeSettings(bool autoConnect, bool showConsole, string tScale, string bScale, int priority, int priorityFactor, int viewMode, int quality, int QoS)
            {
                Settings.Default.AutoConnect = autoConnect;
                Settings.Default.ShowConsole = showConsole;
                Settings.Default.tScale = tScale;
                Settings.Default.bScale = bScale;
                Settings.Default.ScreenPriority = priority;
                Settings.Default.PriorityFactor = priorityFactor;
                Settings.Default.ViewMode = viewMode;
                Settings.Default.Quality = quality;
                Settings.Default.QOSValue = QoS;
                Settings.Default.Save();
            }
            #endregion
        }

    }
}
