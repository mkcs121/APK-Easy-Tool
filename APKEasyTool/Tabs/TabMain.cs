using APKEasyTool.Enums;
using APKEasyTool.Forms;
using APKEasyTool.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APKEasyTool
{
    public class TabMain
    {
        private static MainForm main;

        // fields
        readonly string[] png = { "mipmap-hdpi-v4", "drawable-hdpi-v4", "mipmap-hdpi" };
        public static string fbdfolder = "", apkinfo = "";
        string launchableActivity;
        public static LogOutputForm _logform;

        public TabMain(MainForm Main)
        {
            main = Main;
        }

        internal static void Worktype(WorkType type)
        {
            switch (type)
            {
                case WorkType.Apk:
                    {
                        main.chkAliBtn.Enabled = true;
                        main.decApkBtn.Enabled = true;
                        main.comApkBtn.Enabled = true;
                        main.decFcheckBox.Enabled = true;
                        main.comFcheckBox.Enabled = true;
                        main.comCcheckBox.Enabled = true;
                        main.installApkBtn.Enabled = true;
                        main.zipAlignBtn.Enabled = true;
                        main.signApkBtn.Enabled = true;
                        main.decNameTextBox.Enabled = true;
                        main.comNameTextBox.Enabled = true;
                        main.decNameTextBox.Enabled = true;
                        main.decLbl.Enabled = true;
                        main.extractApkBtn.Enabled = true;
                        main.zipApkBtn.Enabled = true;
                    }
                    break;
                case WorkType.DecompiledApk:
                    {
                        main.chkAliBtn.Enabled = true;
                        main.decApkBtn.Enabled = false;
                        main.decNameTextBox.Text = "";
                        main.decNameTextBox.Enabled = false;
                        main.comNameTextBox.Enabled = true;
                        main.extractApkBtn.Enabled = false;
                        main.zipApkBtn.Enabled = false;
                        main.decFcheckBox.Enabled = true;
                        main.comFcheckBox.Enabled = true;
                        main.comCcheckBox.Enabled = true;
                        main.installApkBtn.Enabled = true;
                        main.zipAlignBtn.Enabled = true;
                        main.signApkBtn.Enabled = true;
                        main.decNameTextBox.Enabled = true;
                        main.decLbl.Enabled = true;
                    }
                    break;
                case WorkType.Jar:
                    {
                        main.apkicon.Image = null;
                        main.pakLbl.Text = "---";
                        main.vercLbl.Text = "---";
                        main.verLbl.Text = "---";
                        main.minLbl.Text = "---";
                        main.tarLbl.Text = "---";
                        main.chkAliBtn.Enabled = false;
                        main.decApkBtn.Enabled = true;
                        main.comApkBtn.Enabled = true;
                        main.decFcheckBox.Enabled = true;
                        main.comFcheckBox.Enabled = true;
                        main.comCcheckBox.Enabled = false;
                        main.installApkBtn.Enabled = false;
                        main.zipAlignBtn.Enabled = false;
                        main.signApkBtn.Enabled = false;
                        main.decNameTextBox.Enabled = true;
                        main.comNameTextBox.Enabled = true;
                        main.decNameTextBox.Enabled = true;
                        main.decLbl.Enabled = true;
                        main.extractApkBtn.Enabled = false;
                        main.zipApkBtn.Enabled = false;
                    }
                    break;
                case WorkType.DecompiledJar:
                    {
                        main.chkAliBtn.Enabled = false;
                        main.decApkBtn.Enabled = false;
                        main.decNameTextBox.Text = "";
                        main.decNameTextBox.Enabled = false;
                        main.comNameTextBox.Enabled = true;
                        main.extractApkBtn.Enabled = false;
                        main.zipApkBtn.Enabled = false;
                        main.decFcheckBox.Enabled = true;
                        main.comFcheckBox.Enabled = true;
                        main.comCcheckBox.Enabled = true;
                        main.installApkBtn.Enabled = false;
                        main.zipAlignBtn.Enabled = false;
                        main.signApkBtn.Enabled = false;
                        main.decNameTextBox.Enabled = true;
                        main.decLbl.Enabled = true;
                    }
                    break;
            }
        }

        public async void Readapk(string path)
        {
            await Task.Factory.StartNew(() =>
            {
                string SignVer = "v1", PackageName = "";

                //main.LogOutput(Lang.READ_APK, MainForm.Type.Info);

                apkinfo = CMD.ProcessStartWithOutput(Variables.RealPath("Resources\\aapt.exe"), "dump badging \"" + path + "\"");

                //remove image if not null
                if (main.apkicon.Image != null)
                {
                    main.apkicon.Image.Dispose();
                    main.apkicon.Image = null;
                }

                try
                {
                    main.Invoke(new Action(delegate ()
                    {
                        main.pakLbl.Text = StringExtension.Regex(@"(?<=package: name=\')(.*?)(?=\')", apkinfo);
                        PackageName = main.pakLbl.Text;
                        main.vercLbl.Text = StringExtension.Regex(@"(?<=versionCode=\')(.*?)(?=\')", apkinfo);
                        main.verLbl.Text = StringExtension.Regex(@"(?<=versionName=\')(.*?)(?=\')", apkinfo);
                        main.minLbl.Text = StringExtension.Regex(@"(?<=sdkVersion:\')(.*?)(?=\')", apkinfo);
                        main.tarLbl.Text = StringExtension.Regex(@"(?<=targetSdkVersion:\')(.*?)(?=\')", apkinfo);
                    }));

                    try
                    {
                        string icon = StringExtension.Regex(@"(?<=application-icon-160:\')(.*?)(?=\')", apkinfo);
                        string mat = StringExtension.Regex(@"(?<=launchable-activity: name=\')(.*?)(?=\')", apkinfo);

                        main.Invoke(new Action(delegate ()
                        {
                            main.launchLbl.Text = mat;
                        }));

                        launchableActivity = mat;

                        if (Path.GetExtension(icon) == ".xml")
                        {
                            icon = icon.Replace(".xml", ".png");
                        }

                        string geticonfile = Variables.TempPathCache + PackageName + "\\" + icon;

                        Debug.WriteLine("[i] icon: " + geticonfile);
                        string getcachedir = Path.GetDirectoryName(geticonfile);
                        Debug.WriteLine(getcachedir);
                        // apk.apk/res/drawable-hdpi-v4/ic_launcher_android.png/
                        if (icon.Contains("anydpi-v26"))
                        {
                            foreach (string Png in png)
                            {
                                string icon2 = icon.Replace("mipmap-anydpi-v26", Png).Replace("drawable-anydpi-v26", Png);
                                CMD.ProcessStartWithOutput(Variables.RealPath("Resources\\7z.exe"), "e \"" + path + "\" \"" + icon2 + "\" -o\"" + getcachedir + "\" -aoa");
                            }
                        }
                        else
                        {
                            CMD.ProcessStartWithOutput(Variables.RealPath("Resources\\7z.exe"), "e \"" + path + "\" \"" + icon + "\" -o\"" + getcachedir + "\" -aoa");
                        }
                        CMD.ProcessStartWithOutput(Variables.RealPath("Resources\\7z.exe"), "e \"" + path + "\" \"META-INF\" -o\"" + Variables.TempPathCache + PackageName + "\\META-INF\" -aoa");
                        string[] files = Directory.GetFiles(Variables.TempPathCache + PackageName + "\\META-INF", "*.SF");
                        if (!Directory.Exists(Variables.TempPathCache + PackageName + "\\META-INF"))
                        {
                            SignVer = "N/A";
                        }
                        else if (files.Length > 0)
                        {
                            string[] sig = File.ReadAllLines(files[0]);
                            foreach (string s in sig)
                            {
                                if (s.Contains("X-Android-APK-Signed: 2, 3"))
                                {
                                    SignVer = "v2, v3";
                                    break;
                                }
                                else if (s.Contains("X-Android-APK-Signed: 2"))
                                {
                                    SignVer = "v2";
                                    break;
                                }
                            }
                            //Regex regSign = new Regex(@"(?<=X-Android-APK-Signed: )\S*(?:\s\S+)?", RegexOptions.None);
                            //Match mat = myRegex.Match(sig);
                        }
                        main.Invoke(new Action(delegate ()
                        {
                            try
                            {
                                //Debug.WriteLine("[i] icon 2: " + geticonfile);
                                Debug.WriteLine("load icon: " + geticonfile);
                                main.apkicon.Image = Image.FromFile(geticonfile);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.ToString());
                                main.apkicon.Image = Properties.Resources.noicon;
                            }
                            main.sigVer.Text = SignVer;
                            main.fullApkInfoBtn.Enabled = true;
                        }));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.ToString());
                        main.Invoke(new Action(delegate ()
                        {
                            main.apkicon.Image = Properties.Resources.noicon;
                            main.fullApkInfoBtn.Enabled = true;
                            main.sigVer.Text = "--";
                        }));
                    }
                    //main.LogOutput(Lang.DONE, MainForm.Type.Info, null);
                }
                catch (Exception ex)
                {
                    main.LogOutput(Lang.ERROR_READ_APK, MainForm.Type.Error);
                    Debug.WriteLine(ex.ToString());
                    main.Invoke(new Action(delegate ()
                    {
                        main.apkicon.Image = null;
                        main.pakLbl.Text = "---";
                        main.launchLbl.Text = "---";
                        main.vercLbl.Text = "---";
                        main.verLbl.Text = "---";
                        main.minLbl.Text = "---";
                        main.tarLbl.Text = "---";
                        main.sigVer.Text = "---";
                    }));
                }
            });
        }

        internal async void Readymlxml(string path)
        {
            await Task.Factory.StartNew(() =>
            {
                main.Invoke(new Action(delegate ()
            {
                main.apkicon.Image = null;
                main.fullApkInfoBtn.Enabled = true;
            }));
                try
                {
                    if (File.Exists(path + "\\Apktool.yml"))
                    {
                        string YML = File.ReadAllText(path + "\\Apktool.yml");
                        string readxml = File.ReadAllText(path + "\\AndroidManifest.xml");

                        apkinfo = YML;

                        main.Invoke(new Action(delegate ()
                        {
                            main.minLbl.Text = StringExtension.Regex(@"(?<=minSdkVersion: \')(.*?)(?=\')", YML);
                            main.tarLbl.Text = StringExtension.Regex(@"(?<=targetSdkVersion: \')(.*?)(?=\')", YML);
                            main.vercLbl.Text = StringExtension.Regex(@"(?<=versionCode: \')(.*?)(?=\')", YML);
                            main.verLbl.Text = StringExtension.Regex(@"(?<=versionName: \')(.*?)(?=\')", YML);
                            main.pakLbl.Text = StringExtension.Regex(@"(?<=package=\"")(.*?)(?=\"")", readxml);
                        }));

                        List<string> reses = new List<string>()
                         {
                              "drawable-hdpi-v4", "drawable-ldpi-v4", "drawable-mdpi-v4",
                          "drawable-xhdpi-v4", "mipmap-hdpi-v4", "mipmap-mdpi-v4", "mipmap-xhdpi-v4",
                          "mipmap-xxhdpi-v4", "mipmap-xxxhdpi-v4", "mipmap-hdpi", "mipmap-mdpi", "mipmap-xhdpi",
                          "mipmap-xxhdpi", "mipmap-xxxhdpi"
                         };

                        if (File.Exists(path + "\\AndroidManifest.xml"))
                        {
                            string iconPath = StringExtension.Regex(@"(?<=android:icon=\"")(.*?)(?=\"")", readxml);
                            Debug.WriteLine("icon " + iconPath);
                            foreach (string res in reses)
                            {
                                string getIconManifest;

                                if (iconPath.Contains("mipmap"))
                                {
                                    getIconManifest = "res/" + iconPath.Replace("@", "").Replace("mipmap", res) + ".png";
                                }
                                else
                                {
                                    getIconManifest = "res/" + iconPath.Replace("@", "").Replace("drawable", res) + ".png";
                                }
                                Debug.WriteLine(path + "\\" + getIconManifest.Replace("/", "\\"));
                                if (File.Exists(path + "\\" + getIconManifest.Replace("/", "\\")))
                                {

                                    main.Invoke(new Action(delegate ()
                                    {
                                        main.apkicon.Image = Image.FromFile(path + "\\" + getIconManifest.Replace("/", "\\"));

                                    }));
                                    return;
                                }
                            }
                            main.Invoke(new Action(delegate ()
                            {
                                main.apkicon.Image = Properties.Resources.noicon;
                            }));
                        }
                    }
                    else
                    {
                        main.Invoke(new Action(delegate ()
                        {
                            if (main.apkicon.Image != null)
                            {
                                main.apkicon.Image.Dispose();
                                main.apkicon.Image = null;
                            }
                            main.pakLbl.Text = "---";
                            main.vercLbl.Text = "---";
                            main.verLbl.Text = "---";
                            main.minLbl.Text = "---";
                            main.tarLbl.Text = "---";
                            main.sigVer.Text = "---";
                            main.decNameTextBox.Text = "";
                            main.comNameTextBox.Text = "";
                            main.LogOutput(Lang.Localize("not_a_dec_notice", "Not a decompiled APK folder"), MainForm.Type.Error);
                            Worktype(WorkType.Apk);
                        }));
                    }
                }
                catch (Exception ex)
                {
                    main.apkicon.Image = Properties.Resources.noicon;
                    Debug.WriteLine(ex.ToString());
                }
            });
        }

        //https://developer.android.com/studio/releases/platforms 
        public string GetAndroidVer(string sdk)
        {
            switch (sdk)
            {
                case "32":
                    return "32 (A12L)";
                case "31":
                    return "31 (A12)";
                case "30":
                    return "30 (A11)";
                case "29":
                    return "29 (A10)";
                case "28":
                    return "28 (A9)";
                case "27":
                    return "27 (A8.1)";
                case "26":
                    return "26 (A8.0)";
                case "25":
                    return "25 (A7.1.x)";
                case "24":
                    return "24 (A7.0)";
                case "23":
                    return "23 (A6.0)";
                case "22":
                    return "22 (A5.1)";
                case "21":
                    return "21 (A5.0)";
                case "20":
                    return "20 (A4.4W)";
                case "19":
                    return "19 (A4.4)";
                case "18":
                    return "18 (A4.3)";
                case "17":
                    return "17 (A4.2.x)";
                case "16":
                    return "16 (A4.1.x)";
                case "15":
                    return "15 (A4.0.x)";
                case "14":
                    return "14 (A4.0.x)";
                case "13":
                    return "13 (A3.2.)";
                case "12":
                    return "12 (A3.1.x)";
                case "11":
                    return "11 (A3.0.x)";
                case "10":
                    return "10 (A2.3.x)";
                case "9":
                    return "9 (A2.3.x)";
                default:
                    return sdk;
            }
        }

        internal void viewSidedLog()
        {
            if (main.logOutputForm != null)
            {
                if (main.logOutputForm.Visible)
                {
                    MainClass.isSidedLogOpened = false;
                    string ss = main.viewLogSidedBtn.Text.Replace("<<", ">>");
                    main.viewLogSidedBtn.Text = ss;
                    main.logOutputForm.Hide();
                    return;
                }
                MainClass.isSidedLogOpened = true;

                main.logOutputForm.StartPosition = FormStartPosition.Manual;
                main.logOutputForm.Top = main.Location.Y;
                main.logOutputForm.Left = main.Location.X + main.Width;
                main.logOutputForm.Show();
                main.viewLogSidedBtn.Text = main.viewLogSidedBtn.Text.Replace(">>", "<<");
            }
        }

        //select apk
        internal void selectApk_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Android Package (*.apk)|*.apk|JAR file (*.jar)|*.jar";
            ofd.Title = Lang.SEL_FILES_DIAG;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                main.decNameTextBox.Text = Path.GetFileNameWithoutExtension(ofd.FileName);
                main.comNameTextBox.Text = Path.GetFileNameWithoutExtension(ofd.FileName);
                selectApk(ofd.FileName);
            }
        }

        internal void selectDecDir_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog();
            fbd.SelectedPath = fbdfolder;
            fbd.Description = Lang.SEL_DEC_DIAG;
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                main.pathOfApk.Text = fbd.SelectedPath; //Show the path in label
                fbdfolder = fbd.SelectedPath;
                DirectoryInfo info = new DirectoryInfo(main.pathOfApk.Text);
                main.comNameTextBox.Text = info.Name;
                main.decNameTextBox.Text = "";
                selectApk(fbd.SelectedPath);
            }
        }

        internal void selectApk(string path)
        {
            try
            {
                if (Directory.Exists(path) && File.Exists(path + "\\androidmanifest.xml") && File.Exists(path + "\\apktool.yml"))
                {
                    main.pathOfApk.Text = path;
                    Worktype(WorkType.DecompiledApk);
                    Readymlxml(path);
                }
                else if (Directory.Exists(path) && File.Exists(path + "\\Apktool.yml"))
                {
                    main.pathOfApk.Text = path;
                    Worktype(WorkType.DecompiledJar);
                    main.pakLbl.Text = "---";
                    main.vercLbl.Text = "---";
                    main.verLbl.Text = "---";
                    main.minLbl.Text = "---";
                    main.tarLbl.Text = "---";
                    main.decNameTextBox.Text = "";
                    if (main.apkicon.Image != null)
                    {
                        main.apkicon.Image.Dispose();
                        main.apkicon.Image = null;
                    }
                }
                else if (File.Exists(path))
                {
                    main.pathOfApk.Text = path;
                    if (Path.GetExtension(path) == ".jar")
                    {
                        Worktype(WorkType.Jar);
                        main.pakLbl.Text = "---";
                        main.vercLbl.Text = "---";
                        main.verLbl.Text = "---";
                        main.minLbl.Text = "---";
                        main.tarLbl.Text = "---";
                        if (main.apkicon.Image != null)
                        {
                            main.apkicon.Image.Dispose();
                            main.apkicon.Image = null;
                        }
                    }
                    else
                    {
                        Worktype(WorkType.Apk);
                        Readapk(path);
                    }
                }
                else
                {
                    if (main.apkicon.Image != null)
                    {
                        main.apkicon.Image.Dispose();
                        main.apkicon.Image = null;
                    }
                    main.pakLbl.Text = "---";
                    main.vercLbl.Text = "---";
                    main.verLbl.Text = "---";
                    main.minLbl.Text = "---";
                    main.tarLbl.Text = "---";
                    main.decNameTextBox.Text = "";
                    main.comNameTextBox.Text = "";
                    main.pathOfApk.Text = "";
                    main.LogOutput(Lang.INVALID_NOTICE, MainForm.Type.Error);
                    Worktype(WorkType.Apk);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                MessageBox.Show(ex.ToString());
            }
        }

        internal void pathOfApk_Validated(object sender, EventArgs e)
        {
            if (main.pathOfApk.Text != "")
            {
                if (main.disHisBox.Checked == false)
                {
                    if (Directory.Exists(main.pathOfApk.Text) || File.Exists(main.pathOfApk.Text))
                    {
                        if (main.pathOfApk.Text != "" && !main.pathOfApk.Items.Contains(main.pathOfApk.Text))
                        {
                            main.pathOfApk.Items.Insert(0, main.pathOfApk.Text);
                        }
                        if (main.pathOfApk.Text != "")
                        {

                            main.tabMainInstance.selectApk(main.pathOfApk.Text);
                        }
                    }
                    else
                    {
                        if (main.apkicon.Image != null)
                        {
                            main.apkicon.Image.Dispose();
                            main.apkicon.Image = null;
                        }
                        main.pakLbl.Text = "---";
                        main.vercLbl.Text = "---";
                        main.verLbl.Text = "---";
                        main.minLbl.Text = "---";
                        main.tarLbl.Text = "---";
                        main.decNameTextBox.Text = "";
                        main.comNameTextBox.Text = "";

                        main.LogOutput(Lang.INVALID_NOTICE, MainForm.Type.Error);
                        Worktype(WorkType.Apk);
                    }
                }
            }
        }

        internal void pathOfApk_SelectedValueChanged(object sender, EventArgs e)
        {
            if (main.isLoaded)
            {
                selectApk(main.pathOfApk.Text);
                main.decNameTextBox.Text = Path.GetFileNameWithoutExtension(main.pathOfApk.Text);
                main.comNameTextBox.Text = Path.GetFileNameWithoutExtension(main.pathOfApk.Text);
            }
        }

        internal void pakLbl_Click(object sender, EventArgs e)
        {
            try
            {
                StringBuilder clipboard = new StringBuilder();
                clipboard.Append(main.pakLbl.Text);
                Clipboard.SetText(clipboard.ToString());
                main.LogOutput(Lang.PAK_ID_COPY_NOTICE, MainForm.Type.Info);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        internal void launchLbl_Click(object sender, EventArgs e)
        {
            for (int i = 1; i < 200; i++)
            {
                string smaliFolder = (i == 1) ? "smali" : "smali_classes" + i;
                if (Directory.Exists(Path.Combine(main.pathOfDec.Text, main.decNameTextBox.Text, smaliFolder)))
                {
                    Process.Start("explorer.exe", string.Format("/select,\"{0}\"", Path.Combine(main.pathOfDec.Text, main.decNameTextBox.Text, smaliFolder, launchableActivity.Replace(".", "\\") + ".smali")));
                    return;
                }
            }

            MessageBox.Show(Lang.ACTIVITY_NOT_FOUND, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        //action buttonsv
        internal void decApkBtn_Click(object sender, EventArgs e)
        {
            if (main.apkToolComboBox.Text == "")
            {
                MessageBox.Show(Lang.APKTOOL_NOT_SEL_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                main.tMain.SelectedIndex = 4;
                return;
            }
            if (String.IsNullOrEmpty(main.pathOfApk.Text) == true)
            {
                MessageBox.Show(Lang.APK_NOT_SEL_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (String.IsNullOrEmpty(main.pathOfDec.Text) == true)
            {
                MessageBox.Show(Lang.APK_DEC_NOT_SET_MBOX);
                return;
            }
            if (String.IsNullOrEmpty(main.decNameTextBox.Text) == true)
            {
                MessageBox.Show(Lang.PLZ_NAME_DEC_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (Directory.Exists(main.pathOfDec.Text + "\\" + main.decNameTextBox.Text))
            {
                DialogResult dialogResult = MessageBox.Show(Lang.DEC_AGAIN_MBOX, Program.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (dialogResult == DialogResult.No)
                    return;
            }
            _ = Apktool.Decompile(main.pathOfApk.Text, Path.Combine(main.pathOfDec.Text, main.decNameTextBox.Text));
        }

        internal void comApkBtn_Click(object sender, EventArgs e)
        {
            if (main.apkToolComboBox.Text == "")
            {
                MessageBox.Show(Lang.APKTOOL_NOT_SEL_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                main.tMain.SelectedIndex = 4;
                return;
            }
            if (String.IsNullOrEmpty(main.pathOfApk.Text) == true)
            {
                MessageBox.Show(Lang.APK_DEX_NOT_SEL_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (String.IsNullOrEmpty(main.pathOfCom.Text) == true)
            {
                MessageBox.Show(Lang.APK_DEC_NOT_SET_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (String.IsNullOrEmpty(main.comNameTextBox.Text) == true)
            {
                MessageBox.Show(Lang.ENTER_NAME_COM_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!Directory.Exists(main.pathOfDec.Text + "\\" + main.decNameTextBox.Text) && main.decApkBtn.Enabled == true)
            {
                MessageBox.Show(Lang.DEC_APK_FIRST_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //If path is decompiled folder
            if (main.decApkBtn.Enabled == false)
            {
                if (File.Exists(Path.Combine(main.pathOfApk.Text, "androidmanifest.xml")))
                    _ = Apktool.Compile(main.pathOfApk.Text, Path.Combine(main.pathOfCom.Text, main.comNameTextBox.Text + ".apk"));
                else
                    _ = Apktool.Compile(main.pathOfApk.Text, Path.Combine(main.pathOfCom.Text, main.comNameTextBox.Text + ".jar"));
            }
            else
            {
                if (File.Exists(Path.Combine(main.pathOfApk.Text, "androidmanifest.xml")) || Path.GetExtension(main.pathOfApk.Text) == ".apk")
                    _ = Apktool.Compile(Path.Combine(main.pathOfDec.Text, main.decNameTextBox.Text), Path.Combine(main.pathOfCom.Text, main.comNameTextBox.Text + ".apk"));
                else
                    _ = Apktool.Compile(Path.Combine(main.pathOfDec.Text, main.decNameTextBox.Text), Path.Combine(main.pathOfCom.Text, main.comNameTextBox.Text + ".jar"));
            }
        }

        internal void signapk()
        {
            if (String.IsNullOrEmpty(main.pathOfApk.Text) == true)
            {
                MessageBox.Show(Lang.APK_NOT_SEL_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (Path.GetExtension(main.pathOfApk.Text) == ".apk")
            {
                if (!File.Exists(main.pathOfCom.Text + "\\" + main.comNameTextBox.Text + ".apk"))
                {
                    _ = Apktool.Sign(main.pathOfApk.Text, Path.Combine(Path.GetDirectoryName(main.pathOfApk.Text), Path.GetFileNameWithoutExtension(main.pathOfApk.Text) + " signed.apk"));
                    return;
                }
            }

            if (main.zipAfterSignChkBox.Checked)
                _ = Apktool.Sign(Path.Combine(main.pathOfCom.Text, main.comNameTextBox.Text + ".apk"), Path.Combine(main.pathOfCom.Text, main.comNameTextBox.Text + ".apk"));
        }

        internal void Zipalign()
        {
            if (String.IsNullOrEmpty(main.pathOfApk.Text) == true)
            {
                MessageBox.Show(Lang.APK_NOT_SEL_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!File.Exists(Path.Combine(main.pathOfCom.Text, main.comNameTextBox.Text + ".apk")))
            {
                MessageBox.Show(Lang.COM_APK_FIRST_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _ = Apktool.ZipAlign(Path.Combine(main.pathOfCom.Text, main.comNameTextBox.Text + ".apk"), Path.Combine(main.pathOfCom.Text + "\\" + main.comNameTextBox.Text + ".apk"));
        }

        internal void installApk()
        {
            if (String.IsNullOrEmpty(main.pathOfApk.Text) == true)
            {
                MessageBox.Show(Lang.APK_NOT_SEL_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!File.Exists(Path.Combine(main.pathOfCom.Text, main.comNameTextBox.Text + ".apk")))
            {
                MessageBox.Show(Lang.COM_APK_FIRST_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (Process.GetProcessesByName("adb").Length > 0)
            {
                DialogResult dialogResult = MessageBox.Show(Lang.ADB_BUSY_MBOX, Program.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (dialogResult == DialogResult.Yes)
                {
                    foreach (var process in Process.GetProcessesByName("adb"))
                    {
                        process.Kill();
                    }
                }
                else
                {
                    return;
                }
            }
            _ = Apktool.InstallApk(Path.Combine(main.pathOfCom.Text, main.comNameTextBox.Text + ".apk"));
        }

        internal void chkAliBtn_Click(object sender, EventArgs e)
        {
            //if (File.Exists(Path.Combine(main.pathOfCom.Text, main.comNameTextBox.Text + " zipaligned.apk")))
            //{
            //    Apktool.CheckAlign(Path.Combine(main.pathOfCom.Text, main.comNameTextBox.Text + " zipaligned.apk"));
            //}
            if (File.Exists(Path.Combine(main.pathOfCom.Text, main.comNameTextBox.Text + ".apk")))
            {
                _ = Apktool.CheckAlign(Path.Combine(main.pathOfCom.Text, main.comNameTextBox.Text + ".apk"));
            }
            else
            {
                MessageBox.Show(Lang.ZIPALIGN_FIRST_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        internal void extractApkBtn_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(main.pathOfApk.Text) == true)
            {
                MessageBox.Show(Lang.APK_NOT_SEL_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _ = Apktool.ExtractApk(main.pathOfApk.Text, Path.Combine(main.pathOfExt.Text, Path.GetFileName(main.pathOfApk.Text)));
        }

        internal void zipApkBtn_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(main.pathOfApk.Text) == true)
            {
                MessageBox.Show(Lang.APK_NOT_SEL_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!Directory.Exists(Path.Combine(main.pathOfExt.Text, Path.GetFileName(main.pathOfApk.Text))))
            {
                MessageBox.Show(Lang.EXT_APK_FIRST_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _ = Apktool.ZipApk(Path.Combine(main.pathOfExt.Text, Path.GetFileName(main.pathOfApk.Text)), Path.Combine(main.pathOfZip.Text, Path.GetFileName(main.pathOfApk.Text)));
        }

        internal void Cancel()
        {
            DialogResult dialogResult = MessageBox.Show(Lang.CANCEL_MBOX, Program.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                switch (Apktool.processType)
                {
                    case ProcessType.SevenZip:
                        foreach (var process in Process.GetProcessesByName("7z"))
                        {
                            process.Kill();
                        }
                        break;
                    case ProcessType.Java:
                        foreach (var process in Process.GetProcessesByName("java"))
                        {
                            process.Kill();
                        }
                        break;
                    case ProcessType.Adb:
                        foreach (var process in Process.GetProcessesByName("adb"))
                        {
                            process.Kill();
                        }
                        break;
                    case ProcessType.Zipalign:
                        foreach (var process in Process.GetProcessesByName("zipalign"))
                        {
                            process.Kill();
                        }
                        break;
                }

                main.LogOutput(Lang.CANCELLED_LOG, MainForm.Type.Info);
                main.tMain.Enabled = true;
            }
        }
    }
}