using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APKEasyTool
{
    public partial class UpdateForm : Form
    {
        private static MainForm main;
        string txt;

        public UpdateForm(MainForm Main)
        {
            main = Main;
        }

        public UpdateForm()
        {
            InitializeComponent();

            Text = Lang.Localize("update_title", Text);
            yourVerLbl.Text = Lang.Localize("update_lbl", yourVerLbl.Text);
            dlXdaLbl.Text = Lang.Localize("visit_xda_link", dlXdaLbl.Text);
            label75.Text = Lang.Localize("changelog_lbl", label75.Text);
            label1.Text = Lang.Localize("down_lbl", label1.Text);
            label2.Text = Lang.Localize("down_note_lbl", label2.Text);
            staLbl.Text = Lang.Localize("ready_stat_lbl", staLbl.Text);
            cloBtn.Text = Lang.Localize("close_btn", cloBtn.Text);
            dlBtn.Text = Lang.Localize("down_lbl", dlBtn.Text);

            StartPosition = FormStartPosition.Manual;
            Location = new Point(main.Location.X + 30, main.Location.Y + 30);
            backgroundWorker1.RunWorkerAsync();

            string ver = Updates.RemoteVersion();
            if (ver == "ERROR")
            {
                MessageBox.Show(Lang.Localize("update_error", "Could not connect to the server"), Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            newVerLbl.Text = Lang.Localize("update_svr_ver", "Latest version: ") + " " + ver;
            yourVerLbl.Text = Lang.Localize("update_cli_ver", "Your version: ") + " " + Program.Version;
        }

        private void dlXdaLbl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://forum.xda-developers.com/android/software-hacking/tool-apk-easy-tool-v1-02-windows-gui-t3333960");
        }

        //private void insBtn_Click(object sender, EventArgs e)
        //{
        //    dlBtn.Enabled = false;
        //    staLbl.Text = Lang.DOWN_STAT_LBL;
        //    try
        //    {
        //        WebClient wc = new WebClient();
        //        wc.DownloadProgressChanged += wc_DownloadProgressChanged;
        //        wc.DownloadFileCompleted += new AsyncCompletedEventHandler(wc_DownloadProgressCompleted);
        //        //wc.DownloadDataCompleted += wc_DownloadProgressCompleted;
        //        wc.DownloadFileAsync(new Uri("https://evildog1.bitbucket.io/apkeasytool/APK Easy Tool Setup.msi"),
        //            Variables.TempPath + "APK Easy Tool Setup.msi");
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine(ex.ToString());
        //        MessageBox.Show(ex.Message, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //    }
        //}

        private void porBtn_Click(object sender, EventArgs e)
        {
            dlBtn.Enabled = false;
            staLbl.Text = Lang.DOWNLOADING_LBL;
            try
            {
                WebClient wc = new WebClient();
                wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                wc.DownloadFileCompleted += new AsyncCompletedEventHandler(wc_DownloadProgressCompleted);
                //wc.DownloadDataCompleted += wc_DownloadProgressCompleted;
                    wc.DownloadFileAsync(new Uri("https://evildog1.bitbucket.io/apkeasytool/APK Easy Tool portable.zip"),
                   Variables.TempPath + "APK Easy Tool portable.zip");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        void wc_DownloadProgressCompleted(object sender, AsyncCompletedEventArgs e)
        {
            dlBtn.Enabled = true;
            staLbl.Text = Lang.DOWN_OK_LBL;
            Process.Start(Variables.TempPath + "APK Easy Tool portable.zip");
        }


        private void cloBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            WebClient wc = new WebClient();
                txt = wc.DownloadString("https://evildog1.bitbucket.io/apkeasytool/clog.txt");
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            richTextBox1.Text = txt;
        }
    }
}
