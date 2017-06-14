using kit_kat.Properties;
using ntrbase;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace kit_kat
{
    static class Program
    {
        public static NTR viewer;
        public static NTR ir;
        public static MainForm mainform;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            viewer = new NTR();
            ir = new NTR();
            try
            {
                // Shut down NTRViewer
                foreach (Process p in Process.GetProcessesByName("NTRViewer")) { p.Kill(); p.WaitForExit(); }
                // Extract to Temp Directory
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "NTRViewer.exe"), Resources.NTRViewer);
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "SDL2.dll"), Resources.SDL2);
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "turbojpeg.dll"), Resources.turbojpeg);
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "3dstool.exe"), Resources._3dstool);
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "ctrtool.exe"), Resources.ctrtool);
                File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "MakeRom.exe"), Resources.MakeRom);
                //Start
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                mainform = new MainForm();
                Application.Run(mainform);
            }
            catch (Exception)
            {
            }
        }
    }
}
