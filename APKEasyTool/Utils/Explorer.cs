using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APKEasyTool.Utils
{
    class Explorer
    {
        public static void Restart()
        {
            Process[] explorer = Process.GetProcessesByName("explorer");
            foreach (Process process in explorer)
            {
                process.Kill();
            }

            // start a new explorer process
            Process.Start("explorer.exe");
        }
    }
}
