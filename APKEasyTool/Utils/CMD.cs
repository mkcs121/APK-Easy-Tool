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
                    // Read both pipes at the same time: reading one to the end first can deadlock
                    // when the tool fills the other one. The timeout counts from the start.
                    Task<string> stdout = process.StandardOutput.ReadToEndAsync();
                    Task<string> stderr = process.StandardError.ReadToEndAsync();
                    if (!process.WaitForExit(60000))
                    {
                        try { process.Kill(); } catch { }
                        Debug.WriteLine("Timed out: " + FileName + " " + Arguments);
                    }
                    Task.WaitAll(new Task[] { stdout, stderr }, 5000);
                    result = (stdout.IsCompleted ? stdout.Result.Trim() : "") + (stderr.IsCompleted ? stderr.Result.Trim() : "");
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
        // The process the current operation started; Cancel stops only this one,
        // never other java/adb/zipalign processes on the machine (Android Studio etc.).
        static Process current;
        static readonly object currentLock = new object();

        public static bool CancelCurrent()
        {
            lock (currentLock)
            {
                if (current == null)
                    return false;
                try
                {
                    if (!current.HasExited)
                        current.Kill();
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    return false;
                }
            }
        }

        public static void StartProgram(string filename, string commandLine, bool logoutput, out int ExitCode)
        {
            StartProgram(filename, commandLine, logoutput, null, out ExitCode);
        }

        /// <param name="env">Extra environment variables, e.g. a password passed as "env:NAME" so it never
        /// appears in the command line or the log.</param>
        public static void StartProgram(string filename, string commandLine, bool logoutput, IDictionary<string, string> env, out int ExitCode)
        {
            main.LogOutput("Command: " + filename + " " + commandLine + "\n");

            var info = new ProcessStartInfo();
            if (env != null)
                foreach (var kv in env)
                    info.EnvironmentVariables[kv.Key] = kv.Value;
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
                lock (currentLock)
                    current = p;
                try
                {
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                    p.WaitForExit();
                    ExitCode = p.ExitCode;
                }
                finally
                {
                    lock (currentLock)
                        if (current == p)
                            current = null;
                }
            }
        }

    }
}
