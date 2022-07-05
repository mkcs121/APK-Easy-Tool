using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace APKEasyTool
{
    static class Program
    {

        public static readonly string Name = "APK Easy Tool";

        public static readonly string Version = "1.60";


        [DllImport("Shcore.dll")]
        static extern int SetProcessDpiAwareness(int PROCESS_DPI_AWARENESS);

        // According to https://msdn.microsoft.com/en-us/library/windows/desktop/dn280512(v=vs.85).aspx
        private enum DpiAwareness
        {
            None = 0,
            SystemAware = 1,
            PerMonitorAware = 2
        }
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(String[] arg)
        {
            if (Environment.OSVersion.Version.Major == 6)
            {
                SetProcessDPIAware();
                // SetProcessDpiAwareness((int)DpiAwareness.PerMonitorAware);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //LoadLang
            LoadConfig();
            Lang.LoadStr();
            //Force Tls12
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //Check resource
            if (ResourcesExist())
            {
                Application.Run(new MainForm());
            }
            else
            {
                MessageBox.Show(Lang.RESOURCE_MISSING_NOTICE, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        static void LoadConfig()
        {
            try
            {
                if (File.Exists(Variables.GetPath() + "config.xml"))
                {
                    //Vars.GetPath()
                    XmlSerializer xs = new XmlSerializer(typeof(APKEasyTool));
                    FileStream read = new FileStream(Variables.GetPath() + "config.xml", FileMode.Open, FileAccess.Read, FileShare.Read);
                    APKEasyTool info = (APKEasyTool)xs.Deserialize(read);
                    Lang.LoadLocalization(Variables.RealPath("Language\\" + info.Language));
                    read.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        static bool ResourcesExist()
        {
            //Resources
            if (Directory.Exists(Variables.RealPath("Apktool")) && Directory.Exists(Variables.RealPath("Resources")))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
