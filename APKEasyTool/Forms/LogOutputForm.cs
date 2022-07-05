using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace APKEasyTool.Forms
{
    public partial class LogOutputForm : Form
    {
        private static MainForm main;
        public LogOutputForm()
        {
            //InitializeComponent();

            //Text = Lang.Localize("log_output_tab", Text);
            //clearLogBtn.Text = Lang.Localize("clr_this_log_btn", clearLogBtn.Text);
        }

        public LogOutputForm(MainForm Main)
        {
            InitializeComponent();

            Text = Lang.Localize("log_output_tab", Text);
            clearLogBtn.Text = Lang.Localize("clr_this_log_btn", clearLogBtn.Text);
            saveLogBtn.Text = Lang.Localize("save_log_btn", saveLogBtn.Text);

            main = Main;
        }

        private void Log_Load(object sender, EventArgs e)
        {
            if (main != null)
            {
                richTextBoxLogsStandalone.Text = main.richTextBoxLogs.Text;
                richTextBoxLogsStandalone.SelectionStart = richTextBoxLogsStandalone.Text.Length;
                richTextBoxLogsStandalone.ScrollToCaret();
            }
        }

        private void clearLogBtn_Click(object sender, EventArgs e)
        {
            File.Delete(Variables.LogPath);
            richTextBoxLogsStandalone.Text = "";
            main.richTextBoxLogs.Text = "";
        }

        private void richTextBoxLogs_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            Process.Start(e.LinkText);
        }

        private void richTextBoxLogsStandalone_TextChanged(object sender, EventArgs e)
        {
            richTextBoxLogsStandalone.SelectionStart = richTextBoxLogsStandalone.Text.Length;
            richTextBoxLogsStandalone.ScrollToCaret();
        }

        private void saveLogBtn_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = Lang.SAVE_FILE_FILTER = " (*.txt)|*.txt";
                sfd.FileName = "log";
                sfd.FilterIndex = 2;

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, richTextBoxLogsStandalone.Text);
                }
            }
        }
    }
}
