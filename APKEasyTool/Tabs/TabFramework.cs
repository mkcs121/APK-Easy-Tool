using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace APKEasyTool
{
    public class TabFramework
    {
        private static MainForm main;

        public static string tempfwfolder = "";
        public static string tempfwfolder2 = "";

        public TabFramework(MainForm Main)
        {
            main = Main;
        }

        internal void installFwBtn_Click(object sender, EventArgs e)
        {
            if (main.apkToolComboBox.Text == "")
            {
                MessageBox.Show(Lang.APKTOOL_NOT_SEL_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (String.IsNullOrEmpty(main.pathOfFw.Text) == true)
            {
                MessageBox.Show(Lang.FW_NOT_SEL_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _ = Apktool.Framework(main.pathOfFw.Text);
        }

        internal void changeInsFwTxtbox_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog();

            fbd.Description = Lang.SEL_FW_FOLDER_DIAG;
            fbd.SelectedPath = tempfwfolder;
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                main.installFwDirTxtBox.Text = fbd.SelectedPath;
                tempfwfolder = fbd.SelectedPath;
            }
        }

        internal void changeDirFwTxtbox_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog();

            fbd.Description = Lang.SEL_FW_STORED_DIAG;
            fbd.SelectedPath = tempfwfolder;
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                main.fwDirTxtBox.Text = fbd.SelectedPath;
                tempfwfolder2 = fbd.SelectedPath;
            }
        }

        internal void openFwDirBtn_Click(object sender, EventArgs e)
        {
            if (main.fwDirTxtBox.Text != ""){
                Process.Start("explorer.exe", main.fwDirTxtBox.Text);
            }
            else
            {
                if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Apktool\framework"))
                {
                    Process.Start("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Apktool\framework");
                }
                else
                {
                    Process.Start("explorer.exe", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\temp");
                }
            }
        }

        internal void clrFwCacheBtn_Click(object sender, EventArgs e)
        {
            if (main.apkToolComboBox.Text == "")
            {
                MessageBox.Show(Lang.APKTOOL_NOT_SEL_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                main.tMain.SelectedIndex = 4;
                return;
            }
            _ = Apktool.FrameworkClear();
        }
    }
}
