using CefSharp;
using kit_kat.Properties;
using ntrbase;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace kit_kat
{
    static class Program
    {
        public static NTR viewer = new NTR();
        public static MainUI mainform;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            #region Recover Settings
            if (Settings.Default.updateSettings)
            {
                Settings.Default.Upgrade();
                Settings.Default.updateSettings = false;
                Settings.Default.Save();
            }
            #endregion
            #region Get NTRViewer Ready
            try
            {
                // Shut down NTRViewer
                foreach (Process p in Process.GetProcessesByName("NTRViewer")) { p.Kill(); p.WaitForExit(); }
                // Extract to Temp Directory
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "NTRViewer.exe"), Resources.NTRViewer);
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "SDL2.dll"), Resources.SDL2);
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "turbojpeg.dll"), Resources.turbojpeg);
            }
            catch (Exception)
            {
            }
            #endregion
            #region Check for Updates
            using(WebClient wc = new WebClient())
            {
                try
                {
                    if (wc.DownloadString("https://raw.githubusercontent.com/PR4GM4/kit-kat/master/VERSION.txt") != FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.CodeBase.Replace("file:///", "")).ProductVersion)
                    {
                        MessageBox.Show("Download the latest version at github.com/PR4GM4/kit-kat/releases/latest\nUpdating is not enforced, but it is recommended.\nIf your having issues, updating might help.", "Update Available!");
                    }
                }
                catch (WebException)
                {
                    //Ignore errors, count it as a failed update check and move on.
                }
            }
            #endregion
            #region CEFSharp Library Loader
            CefLibraryHandle libraryLoader = new CefLibraryHandle(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"libs\libcef.dll"));
            Console.WriteLine($"Library is valid: {!libraryLoader.IsInvalid}");
            #endregion
            #region CEFSharp Settings
            CefSettings cefSettings = new CefSettings()
            {
                LogSeverity = LogSeverity.Disable,
                MultiThreadedMessageLoop = true,
                BrowserSubprocessPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"libs\CefSharp.BrowserSubprocess.exe"),
                LocalesDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"libs\locales\"),
                ResourcesDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"libs\")
            };
            if (cefSettings != null && !Cef.Initialize(cefSettings))
            {
                MessageBox.Show("The reason is unknown, make sure you have every file and folder extracted and try again.", "CEFSharp Failed to initialize!");
            }
            #endregion
            //Start
            mainform = new MainUI();
            Application.EnableVisualStyles();
            Application.Run(mainform);
        }

    }
}
