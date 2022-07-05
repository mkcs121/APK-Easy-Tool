using System;
using System.Diagnostics;
using System.IO;

namespace APKEasyTool
{
    public static class Variables
    {
        public static readonly string MyDocPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public static readonly string ApkSignerPath = "\"" + RealPath("Resources\\ApkSigner.jar\" sign ");

        public static readonly string GetDiskLetter = Path.GetPathRoot(RealPath()).Replace("\\", "");

        public static string LogPath = Path.Combine(Path.GetTempPath(), "AET", "log.txt");

        public static string TempPath = Path.Combine(Path.GetTempPath(), "AET");

        public static string TempPathCache = Path.Combine(Path.GetTempPath(), "AET", "Cache");

        // public static string Apktool = "\"" + RealPath + "Apktool\\" + apkToolComboBox.Text + "\"";
        public static string Apktool, InsCount, CodePage, ApkToolVer;

        public static string GetTodayDate()
        {
            return DateTime.Now.ToString("yyyy_MM_dd");
        }

        public static string GetPath()
        {
            if (Path.GetPathRoot(RealPath()) == "C:\\" &&
                !Path.GetFullPath(RealPath()).Contains(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
            {
                return MyDocPath + "\\APK Easy Tool\\";
            }
            else
                return RealPath();
        }

        public static string RealPath(string path = "")
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" + path;
        }
    }
}
