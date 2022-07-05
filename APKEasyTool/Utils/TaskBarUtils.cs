using Microsoft.WindowsAPICodePack.Taskbar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APKEasyTool.Utils
{
    class TaskBarUtils
    {
        public static void TaskBar(int state)
        {
            TaskbarManager.Instance.SetProgressState((TaskbarProgressBarState)state);
        }
    }
}
