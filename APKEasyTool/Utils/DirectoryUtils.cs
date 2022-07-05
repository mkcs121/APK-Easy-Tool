using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APKEasyTool.Utils
{
    public class DirectoryUtils
    {
        internal static void selFolder(string desc, Action<string> onSuccess)
        {
            var fbd = new FolderBrowserDialog();
            fbd.Description = desc;
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                onSuccess(fbd.SelectedPath);
            }
        }

        internal static void selFile(string title, string filter, Action<string> onSuccess)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = filter;
            ofd.Title = title;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                onSuccess(ofd.FileName);
            }
        }

    }
}
