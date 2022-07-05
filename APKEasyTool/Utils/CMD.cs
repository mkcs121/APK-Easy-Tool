using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APKEasyTool.Utils
{
    class CMD
    {
        private static MainForm main;

        public CMD(MainForm Main)
        {
            main = Main;
        }

        public static string ProcessStartWithOutput(string FileName, string Arguments)
        {
            string result = string.Empty;
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = FileName;
                    process.StartInfo.Arguments = Arguments;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.UseShellExecute = false;
                    process.Start();
                    result = process.StandardOutput.ReadToEnd().Trim() + process.StandardError.ReadToEnd().Trim();
                    process.WaitForExit(5000);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return result;
        }

        public static void ProcessStartWithArgs(string FileName, string Arguments)
        {
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = FileName;
                    process.StartInfo.Arguments = Arguments;
                    process.Start();
                    process.WaitForExit(5000);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Start", e);
            }
        }

        //static int ExitCode = 0;
        public static void StartProgram(string filename, string commandLine, bool logoutput, out int ExitCode)
        {
            main.LogOutput("Command: " + filename + " " + commandLine + "\n");

            var info = new ProcessStartInfo();
            info.FileName = filename;
            info.Arguments = commandLine;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            using (var p = new Process())
            {
                p.StartInfo = info;
                // p.EnableRaisingEvents = true;

                p.OutputDataReceived += (s, o) =>
                {
                    if (logoutput && o.Data != null)
                        main.LogOutput(o.Data);
                    //Debug.WriteLine(o.Data);
                };
                p.ErrorDataReceived += (s, o) =>
                {
                    if (o.Data != null)
                        main.LogOutput(o.Data);
                    //Debug.WriteLine(o.Data);
                };
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                ExitCode = p.ExitCode;
            }
        }

    }
}
