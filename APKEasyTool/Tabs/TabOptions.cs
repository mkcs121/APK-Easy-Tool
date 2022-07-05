using APKEasyTool.Utils;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APKEasyTool
{
    public class TabOptions
    {
        private static MainForm main;

        public TabOptions(MainForm Main)
        {
            main = Main;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(HandleRef hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        public static void SetButtonShield(Button btn, bool showShield)
        {
            // BCM_SETSHIELD = 0x0000160C
            SendMessage(new HandleRef(btn, btn.Handle), 0x160C, IntPtr.Zero, showShield ? new IntPtr(1) : IntPtr.Zero);
        }

        //select Apktool
        internal async void apkToolComboBox_SelectedValueChanged_1(object sender, EventArgs e)
        {
            try
            {
                Variables.Apktool = "\"" + Variables.RealPath("Apktool\\" + main.apkToolComboBox.Text) + "\"";
                //Vars.apkvLbl.Text = Vars.apkToolComboBox.Text;

                await Task.Factory.StartNew(() =>
                {
                    GetApktoolVer();
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        internal void setupDirBtn_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog();
            fbd.Description = Lang.SEL_ALL_DIR_DIAG;
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Directory.CreateDirectory(fbd.SelectedPath + "\\1-Decompiled APKs");
                    main.pathOfDec.Text = (fbd.SelectedPath + "\\1-Decompiled APKs").Replace(@"\\", @"\");

                    Directory.CreateDirectory(fbd.SelectedPath + "\\2-Recompiled APKs");
                    main.pathOfCom.Text = (fbd.SelectedPath + "\\2-Recompiled APKs").Replace(@"\\", @"\");

                    Directory.CreateDirectory(fbd.SelectedPath + "\\3-Extracted APKs");
                    main.pathOfExt.Text = (fbd.SelectedPath + "\\3-Extracted APKs").Replace(@"\\", @"\");

                    Directory.CreateDirectory(fbd.SelectedPath + "\\4-Zipped APKs");
                    main.pathOfZip.Text = (fbd.SelectedPath + "\\4-Zipped APKs").Replace(@"\\", @"\");

                    Directory.CreateDirectory(fbd.SelectedPath + "\\5-Baksmali");
                    main.baksDir.Text = (fbd.SelectedPath + "\\5-Baksmali").Replace(@"\\", @"\");

                    Directory.CreateDirectory(fbd.SelectedPath + "\\6-Smali");
                    main.smaliDir.Text = (fbd.SelectedPath + "\\6-Smali").Replace(@"\\", @"\");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    MessageBox.Show(ex.ToString(), Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        internal void clrHisBtn_Click(object sender, EventArgs e)
        {
            main.LogOutput(Lang.HIS_CLEAR_MBOX, MainForm.Type.Info);
            main.pathOfApk.Items.Clear();
            main.decNameTextBox.Items.Clear();
            main.comNameTextBox.Items.Clear();
        }

        internal void resBtn_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show(Lang.RES_DIAG, Program.Name, MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                try
                {
                    File.Delete(Variables.GetPath() + "config.xml");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                Application.Restart();
            }
        }

        private void GetApktoolVer()
        {
            try
            {
                Variables.ApkToolVer = CMD.ProcessStartWithOutput("cmd.exe", "/c \"java -jar " + Variables.Apktool + " -version \"");

                main.Invoke(new Action(delegate ()
                {
                    main.apkvLbl.Text = Variables.ApkToolVer;
                }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
