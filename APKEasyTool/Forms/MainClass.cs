using APKEasyTool.Forms;
using APKEasyTool.Utils;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace APKEasyTool
{
    public class MainClass
    {
        private static MainForm main;

        internal static bool chkForUpdate, isSidedLogOpened;
        public static LogOutputForm _logform;
        public MainClass(MainForm Main)
        {
            main = Main;
        }

        public static int GetWindowsScaling()
        {
            return (int)(100 * Screen.PrimaryScreen.Bounds.Width / SystemParameters.PrimaryScreenWidth);
        }

        internal async void Init()
        {
            // ----- Window ----- //
            main.tMain.SizeMode = TabSizeMode.Fixed;
            main.oTab.SizeMode = TabSizeMode.Fixed;

            //System.Windows.MessageBox.Show(GetWindowsScaling().ToString());
            if (GetWindowsScaling() >= 124)
            {
                main.tMain.ItemSize = new System.Drawing.Size((main.tMain.Width) / main.tMain.TabCount, 35);
                main.oTab.ItemSize = new System.Drawing.Size((main.oTab.Width - 2) / main.oTab.TabCount, 35);
                //main.tMain.ItemSize = new System.Drawing.Size(143, 35);
                // main.oTab.ItemSize = new System.Drawing.Size(210, 35);
            }
            if (GetWindowsScaling() >= 150)
            {
                main.tMain.ItemSize = new System.Drawing.Size((main.tMain.Width - 3) / main.tMain.TabCount, 45);
                main.oTab.ItemSize = new System.Drawing.Size((main.oTab.Width - 6) / main.oTab.TabCount, 45);
            }
            if (GetWindowsScaling() >= 240)
            {
                main.tMain.ItemSize = new System.Drawing.Size((main.tMain.Width - 20) / main.tMain.TabCount, 85);
                main.oTab.ItemSize = new System.Drawing.Size((main.oTab.Width - 30) / main.oTab.TabCount, 85);
            }

            // ----- Auto set combobox ----- //
            main.v2signComboBox.SelectedIndex = 0;
            main.v3signComboBox.SelectedIndex = 0;
            main.v4signComboBox.SelectedIndex = 2;

            //get apktool version
            string[] filePaths = Directory.GetFiles(Variables.RealPath("Apktool"), "*.jar", SearchOption.AllDirectories);
            var namesOnly = filePaths.Select(f => Path.GetFileName(f)).ToArray();
            main.apkToolComboBox.Items.AddRange(namesOnly);

            main.apkToolComboBox.SelectedIndex = 0;
            Variables.Apktool = "\"" + Variables.RealPath("Apktool\\" + main.apkToolComboBox.Text + "\"");

            //Logs
            main.LogOutput(DateTime.Now + "\n");
            Language();

            LoadConfig();
            InitCMDArgsFolder();

            //Task

            //JavaCheck
            var java = new Java();
            bool hasJava = false;

            await Task.Factory.StartNew(() =>
            {
                hasJava = java.CheckJava();
            });

            if (hasJava == false && main.extJavaTxtBox.Text == "")
            {
                main.LogOutput(Lang.JAVA_NOT_INSTALLED_NOTICE, MainForm.Type.Error);
            }
            else
            {
                if (main.useExtJava.Checked)
                    main.LogOutput(CMD.ProcessStartWithOutput(main.extJavaTxtBox.Text, "-version"));
                else
                    main.LogOutput(CMD.ProcessStartWithOutput("java.exe", "-version"));
            }

            Variables.ApkToolVer = CMD.ProcessStartWithOutput("cmd.exe", "/c \"java -jar " + Variables.Apktool + " -version \"");

            main.isLoaded = true;

            InitResources();
            InitTitle();

            TabOptions.SetButtonShield(main.insBtn, true);
            TabOptions.SetButtonShield(main.uninsBtn, true);

            if (main.apkToolComboBox.Text == "")
            {
                main.LogOutput(Lang.APKTOOL_NOT_SEL_MBOX, MainForm.Type.Warning);
                main.tabFw.Enabled = false;
            }

            //await Task.Factory.StartNew(() =>
            //{
            //    if (chkForUpdate == true && Environment.GetCommandLineArgs().Length < 2 && NetworkInterface.GetIsNetworkAvailable() == true)
            //    {
            //        main.LogOutput(Lang.CHK_FOR_UPDATE, MainForm.Type.Info);
            //        string ver = Updates.RemoteVersion();
            //        if (ver != Program.Version)
            //            main.LogOutput(Lang.Localize("update_lbl", "A new version is available:") + " " + ver, MainForm.Type.Info);
            //        else
            //            main.LogOutput(Lang.NO_UPDATE, MainForm.Type.Info);
            //    }
            //});

            InitCmdArgs();
        }

        private void InitResources()
        {
            //Check folders
            try
            {
                if (main.pathOfDec.Text == "" || !Directory.Exists(main.pathOfDec.Text))
                {
                    Directory.CreateDirectory(Variables.GetPath() + "1-Decompiled APKs");
                    main.pathOfDec.Text = Variables.GetPath() + "1-Decompiled APKs";
                }
                if (main.pathOfCom.Text == "" || !Directory.Exists(main.pathOfCom.Text))
                {
                    Directory.CreateDirectory(Variables.GetPath() + "2-Recompiled APKs");
                    main.pathOfCom.Text = Variables.GetPath() + "2-Recompiled APKs";
                }
                if (main.pathOfExt.Text == "" || !Directory.Exists(main.pathOfExt.Text))
                {
                    Directory.CreateDirectory(Variables.GetPath() + "3-Extracted APKs");
                    main.pathOfExt.Text = Variables.GetPath() + "3-Extracted APKs";
                }
                if (main.pathOfZip.Text == "" || !Directory.Exists(main.pathOfZip.Text))
                {
                    Directory.CreateDirectory(Variables.GetPath() + "4-Zipped APKs");
                    main.pathOfZip.Text = Variables.GetPath() + "4-Zipped APKs";
                }
                if (main.baksDir.Text == "" || !Directory.Exists(main.baksDir.Text))
                {
                    Directory.CreateDirectory(Variables.GetPath() + "5-Baksmali");
                    main.baksDir.Text = Variables.GetPath() + "5-Baksmali";
                }
                if (main.smaliDir.Text == "" || !Directory.Exists(main.smaliDir.Text))
                {
                    Directory.CreateDirectory(Variables.GetPath() + "6-Smali");
                    main.smaliDir.Text = Variables.GetPath() + "6-Smali";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            //Keys
            if (main.pk8FileTxtBox.Text == "" || !File.Exists(main.pk8FileTxtBox.Text))
                main.pk8FileTxtBox.Text = Variables.RealPath("Resources\\apkeasytool.pk8");

            if (main.pemFileTxtBox.Text == "" || !File.Exists(main.pemFileTxtBox.Text))
                main.pemFileTxtBox.Text = Variables.RealPath("Resources\\apkeasytool.pem");

            if (main.pathOfApk.Text != "")
                main.tabMainInstance.selectApk(main.pathOfApk.Text);
        }

        private void Language()
        {
            //Resources
            if (Directory.Exists(Variables.RealPath("Language")))
            {
                try
                {
                    string[] filePaths = Directory.GetFiles(Variables.RealPath("Language"), "*.xml");
                    var namesOnly = filePaths.Select(f => Path.GetFileName(f)).ToArray();
                    main.langComboBox.Items.AddRange(namesOnly);
                    if (main.langComboBox.Text == "")
                        main.langComboBox.SelectedItem = "English (Default)";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }
            }
        }

        private async void InitCmdArgs()
        {
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                if (Environment.GetCommandLineArgs().Length > 2)
                {
                    string file = Environment.GetCommandLineArgs()[2];
                    if (Environment.GetCommandLineArgs()[1] == "-d")
                    {
                        await Apktool.Decompile(file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));
                    }
                    else if (Environment.GetCommandLineArgs()[1] == "-c")
                    {
                        if (File.Exists(Path.Combine(file, "androidmanifest.xml")) && File.Exists(Path.Combine(file, "Apktool.yml")))
                        {
                            await Apktool.Compile(file, file + " compiled.apk");
                        }
                        else if (File.Exists(Path.Combine(file, "Apktool.yml")))
                            await Apktool.Compile(file, file + " compiled.jar");
                    }
                    else if (Environment.GetCommandLineArgs()[1] == "-s")
                    {
                        await Apktool.Sign(file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + " signed.apk"));
                    }
                    else if (Environment.GetCommandLineArgs()[1] == "-a")
                    {
                        await Apktool.ZipAlign(file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + " zipaligned.apk"));
                    }
                    else if (Environment.GetCommandLineArgs()[1] == "-ca")
                    {
                        await Apktool.CheckAlign(file);
                    }
                    else if (Environment.GetCommandLineArgs()[1] == "-i")
                    {
                        await Apktool.InstallApk(file);
                    }
                    else if (Environment.GetCommandLineArgs()[1] == "-e")
                    {
                        await Apktool.ExtractApk(file, Path.Combine(Path.GetDirectoryName(file) + "\\" + Path.GetFileNameWithoutExtension(file)));
                    }
                    else if (Environment.GetCommandLineArgs()[1] == "-z")
                    {
                        await Apktool.ZipApk(file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileName(file) + " zipped.apk"));
                    }
                    else if (Environment.GetCommandLineArgs()[1] == "-bak")
                    {
                        await Apktool.Baksmali(file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));
                    }
                    else if (Environment.GetCommandLineArgs()[1] == "-sma")
                    {
                        await Apktool.Smali(file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + " compiled.dex"));
                    }
                }
                else
                {
                    main.tabMainInstance.selectApk(Environment.GetCommandLineArgs()[1]);
                    main.pathOfApk.Text = Environment.GetCommandLineArgs()[1];
                    main.decNameTextBox.Text = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[1]);
                    main.comNameTextBox.Text = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[1]);
                }
            }
        }

        private void InitCMDArgsFolder()
        {
            if (Environment.GetCommandLineArgs().Length > 1)
            {
                chkForUpdate = false;
                if (Environment.GetCommandLineArgs()[1] == "DecDir")
                {
                    Process.Start("explorer.exe", main.pathOfDec.Text);
                    Environment.Exit(0);
                }
                else if (Environment.GetCommandLineArgs()[1] == "ComDir")
                {
                    Process.Start("explorer.exe", main.pathOfCom.Text);
                    Environment.Exit(0);
                }
                else if (Environment.GetCommandLineArgs()[1] == "ExtDir")
                {
                    Process.Start("explorer.exe", main.pathOfExt.Text);
                    Environment.Exit(0);
                }
                else if (Environment.GetCommandLineArgs()[1] == "ZipDir")
                {
                    Process.Start("explorer.exe", main.pathOfZip.Text);
                    Environment.Exit(0);
                }
                else if (Environment.GetCommandLineArgs()[1] == "BakDir")
                {
                    Process.Start("explorer.exe", main.baksDir.Text);
                    Environment.Exit(0);
                }
                else if (Environment.GetCommandLineArgs()[1] == "SmaDir")
                {
                    Process.Start("explorer.exe", main.smaliDir.Text);
                    Environment.Exit(0);
                }
            }
        }

        private void InitTitle()
        {
            //count instances
            int count = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length;
            if (count > 1)
                main.Text = Program.Name + " v" + Program.Version + " " + Lang.INSTANCE_TITLE + " " + count.ToString();
            else
                main.Text = Program.Name + " v" + Program.Version;
            Variables.InsCount = count.ToString();
        }

        //Load Config
        private void LoadConfig()
        {
            main.Invoke(new Action(delegate ()
            {
                try
                {
                    if (File.Exists(Variables.GetPath() + "config.xml"))
                    {
                        XmlSerializer xs = new XmlSerializer(typeof(APKEasyTool));
                        FileStream read = new FileStream(Variables.GetPath() + "config.xml", FileMode.Open, FileAccess.Read, FileShare.Read);
                        APKEasyTool info = (APKEasyTool)xs.Deserialize(read);

                        main.decodeApiLvl.Text = info.DecApiLvl;
                        main.rebuildApiLvl.Text = info.ComApiLvl;
                        main.pathOfApk.Text = info.SelectedPathMain;
                        main.baksFile.Text = info.SelectedDexOdexOat;
                        main.smaliFile.Text = info.SelectedSmali;
                        main.pathOfDec.Text = info.PathDec;
                        main.pathOfCom.Text = info.PathCom;
                        main.pathOfExt.Text = info.PathExt;
                        main.pathOfZip.Text = info.PathZip;
                        main.baksDir.Text = info.PathBaksmali;
                        main.smaliDir.Text = info.PathSmali;
                        main.pemFileTxtBox.Text = info.PathPem;
                        main.pk8FileTxtBox.Text = info.PathPk8;
                        main.jksFileTxtBox.Text = info.PathJksFile;
                        TabMain.fbdfolder = info.PathTempDec;
                        TabFramework.tempfwfolder = info.PathTempFw;
                        TabFramework.tempfwfolder2 = info.PathTempFw2;
                        main.fwDirTxtBox.Text = info.PathFw;
                        main.extJavaTxtBox.Text = info.PathJava;
                        main.decNameTextBox.Text = info.FileNameDec;
                        main.comNameTextBox.Text = info.FileNameCom;
                        main.baksNameTxtBox.Text = info.FileNameDexOdexOat;
                        main.smaliNameTxtBox.Text = info.FileNameSmali;
                        main.apkToolComboBox.Text = info.DropDownApkTool;
                        main.javaHeapSizeNum.Text = info.DwordXmx;
                        main.langComboBox.Text = info.Language;
                        main.decBcheckBox.Checked = info.CheckBoxDecB;
                        main.decFcheckBox.Checked = info.CheckBoxDecF;
                        main.decKcheckBox.Checked = info.CheckBoxDecK;
                        main.decMcheckBox.Checked = info.CheckBoxDecM;
                        main.decRcheckBox.Checked = info.CheckBoxDecR;
                        main.decScheckBox.Checked = info.CheckBoxDecS;
                        main.decNoAssetsCheckBox.Checked = info.CheckBoxDecNC;
                        main.decOnlyMainClassesChkBox.Checked = info.CheckBoxDecOMC;
                        main.comCcheckBox.Checked = info.CheckBoxComC;
                        main.comDcheckBox.Checked = info.CheckBoxComD;
                        main.comFcheckBox.Checked = info.CheckBoxComF;
                        main.comNCcheckBox.Checked = info.CheckBoxComNC;
                        main.chkBoxUseAapt2.Checked = info.CheckBoxComAapt2;
                        main.zipFcheckBox.Checked = info.CheckBoxZipF;
                        main.zipPcheckBox.Checked = info.CheckBoxZipP;
                        main.zipZcheckBox.Checked = info.CheckBoxZipZ;
                        main.zipVcheckBox.Checked = info.CheckBoxZipV;
                        main.useJksCheckBox.Checked = info.CheckBoxUseJks;
                        main.signApkCheckBox.Checked = info.CheckBoxSignApk;
                        main.signApkCheckBox.Checked = info.CheckBoxSignApk;
                        main.installApkChkBox.Checked = info.CheckBoxInstallApk;
                        main.overApkChecked.Checked = info.CheckBoxOverApk;
                        main.zipAfterSignChkBox.Checked = info.CheckBoxZipAfterSign;
                        main.chkUpdateChkBox.Checked = info.CheckBoxChkUpdate;
                        main.winPosCheckBox.Checked = info.CheckBoxWinPos;
                        main.disHisBox.Checked = info.CheckBoxDisHistory;
                        main.useExtJava.Checked = info.CheckBoxUseExtJava;
                        main.taskBarCheckBox.Checked = info.CheckBoxTaskBar;
                        main.useAaptPathChkBox.Checked = info.CheckBoxAapt;
                        main.useAaptPathTxtBox.Text = info.TextBoxAapt;
                        main.useJaxaXmxChkBox.Checked = info.CheckBoxUseJavaXmx;
                        main.decForceManifestChkBox.Checked = info.CheckBoxForceManifest;
                        if (info.v2SigningEnabled != "")
                            main.v2signComboBox.Text = info.v2SigningEnabled;
                        if (info.v3SigningEnabled != "")
                            main.v3signComboBox.Text = info.v3SigningEnabled;
                        if (info.v4SigningEnabled != "")
                            main.v4signComboBox.Text = info.v4SigningEnabled;

                        if (main.winPosCheckBox.Checked)
                        {
                            main.StartPosition = FormStartPosition.Manual;
                            int formX = info.DwordX;
                            int formY = info.DwordY;
                            main.Location = new System.Drawing.Point(formX, formY);
                        }

                        byte[] bytes = Convert.FromBase64String(info.PassJks);
                        main.jksPass.Text = Encoding.UTF8.GetString(bytes);


                        read.Close();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }

                try
                {
                    if (main.disHisBox.Checked == false)
                    {
                        if (File.Exists(Variables.TempPath + "apkhistory.txt"))
                        {
                            string[] Lines = File.ReadAllLines(Variables.TempPath + "apkhistory.txt");

                            foreach (string Line in Lines)
                                main.pathOfApk.Items.Add(Line);
                        }

                        if (File.Exists(Variables.TempPath + "dechistory.txt"))
                        {
                            string[] Lines = File.ReadAllLines(Variables.TempPath + "dechistory.txt");

                            foreach (string Line in Lines)
                                main.decNameTextBox.Items.Add(Line);
                        }

                        if (File.Exists(Variables.TempPath + "comhistory.txt"))
                        {
                            string[] Lines = File.ReadAllLines(Variables.TempPath + "comhistory.txt");

                            foreach (string Line in Lines)
                                main.comNameTextBox.Items.Add(Line);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                }

                if (main.chkUpdateChkBox.Checked)
                {
                    chkForUpdate = true;
                }

                if (isSidedLogOpened)
                {
                    main.viewLogSidedBtn.Text = Lang.LOG_OUTPUT_BTN + " << ";
                    main.tabMainInstance.viewSidedLog();
                }
            }));
        }

        internal void SaveConfig()
        {
            try
            {
                APKEasyTool info = new APKEasyTool();
                info.DecApiLvl = main.decodeApiLvl.Text;
                info.ComApiLvl = main.rebuildApiLvl.Text;
                info.SelectedPathMain = main.pathOfApk.Text;
                info.SelectedDexOdexOat = main.baksFile.Text;
                info.SelectedSmali = main.smaliFile.Text;
                info.PathDec = main.pathOfDec.Text;
                info.PathCom = main.pathOfCom.Text;
                info.PathExt = main.pathOfExt.Text;
                info.PathZip = main.pathOfZip.Text;
                info.PathBaksmali = main.baksDir.Text;
                info.PathSmali = main.smaliDir.Text;
                info.PathPem = main.pemFileTxtBox.Text;
                info.PathPk8 = main.pk8FileTxtBox.Text;
                info.PathJksFile = main.jksFileTxtBox.Text;
                info.PathTempDec = TabMain.fbdfolder;
                info.PathTempFw = TabFramework.tempfwfolder;
                info.PathTempFw2 = TabFramework.tempfwfolder2;
                info.PathFw = main.fwDirTxtBox.Text;
                info.PathJava = main.extJavaTxtBox.Text;
                info.FileNameDec = main.decNameTextBox.Text;
                info.FileNameCom = main.comNameTextBox.Text;
                info.FileNameDexOdexOat = main.baksNameTxtBox.Text;
                info.FileNameSmali = main.smaliNameTxtBox.Text;
                info.DropDownApkTool = main.apkToolComboBox.Text;
                // info.DropDownCmdMode = main.cmdModeChkBox.Text;
                info.DwordX = main.Location.X;
                info.DwordY = main.Location.Y;
                info.DwordXmx = main.javaHeapSizeNum.Text;
                info.Language = main.langComboBox.Text;
                info.CheckBoxDecB = main.decBcheckBox.Checked;
                info.CheckBoxDecF = main.decFcheckBox.Checked;
                info.CheckBoxDecK = main.decKcheckBox.Checked;
                info.CheckBoxDecM = main.decMcheckBox.Checked;
                info.CheckBoxDecR = main.decRcheckBox.Checked;
                info.CheckBoxDecS = main.decScheckBox.Checked;
                info.CheckBoxDecNC = main.decNoAssetsCheckBox.Checked;
                info.CheckBoxDecOMC = main.decOnlyMainClassesChkBox.Checked;
                info.CheckBoxComC = main.comCcheckBox.Checked;
                info.CheckBoxComD = main.comDcheckBox.Checked;
                info.CheckBoxComF = main.comFcheckBox.Checked;
                info.CheckBoxComNC = main.comNCcheckBox.Checked;
                info.CheckBoxComAapt2 = main.chkBoxUseAapt2.Checked;
                info.CheckBoxZipF = main.zipFcheckBox.Checked;
                info.CheckBoxZipP = main.zipPcheckBox.Checked;
                info.CheckBoxZipZ = main.zipZcheckBox.Checked;
                info.CheckBoxZipV = main.zipVcheckBox.Checked;
                info.CheckBoxUseJks = main.useJksCheckBox.Checked;
                info.CheckBoxSignApk = main.signApkCheckBox.Checked;
                info.CheckBoxInstallApk = main.installApkChkBox.Checked;
                info.CheckBoxOverApk = main.overApkChecked.Checked;
                info.CheckBoxZipAfterSign = main.zipAfterSignChkBox.Checked;
                info.CheckBoxChkUpdate = main.chkUpdateChkBox.Checked;
                info.CheckBoxWinPos = main.winPosCheckBox.Checked;
                info.CheckBoxDisHistory = main.disHisBox.Checked;
                info.CheckBoxUseExtJava = main.useExtJava.Checked;
                info.CheckBoxUseJavaXmx = main.useJaxaXmxChkBox.Checked;
                info.IsSidedLogOpened = isSidedLogOpened;
                info.CheckBoxTaskBar = main.taskBarCheckBox.Checked;
                info.CheckBoxAapt = main.useAaptPathChkBox.Checked;
                info.TextBoxAapt = main.useAaptPathTxtBox.Text;
                info.v2SigningEnabled = main.v2signComboBox.Text;
                info.v3SigningEnabled = main.v3signComboBox.Text;
                info.v4SigningEnabled = main.v4signComboBox.Text;
                info.CheckBoxForceManifest = main.decForceManifestChkBox.Checked;

                var enc = Encoding.UTF8.GetBytes(main.jksPass.Text);
                info.PassJks = Convert.ToBase64String(enc);

                XMLSave.SaveData(info, Variables.GetPath() + "config.xml");

                if (main.disHisBox.Checked == false)
                {
                    StreamWriter SaveFile = new StreamWriter(Variables.TempPath + "apkhistory.txt");
                    foreach (string item in main.pathOfApk.Items)
                    {
                        if (item != "")
                        {
                            SaveFile.WriteLine(item);
                        }
                    }
                    SaveFile.Close();

                    StreamWriter SaveFile2 = new StreamWriter(Variables.TempPath + "dechistory.txt");
                    foreach (string item in main.decNameTextBox.Items)
                    {
                        if (item != "")
                        {
                            SaveFile2.WriteLine(item);
                        }
                    }
                    SaveFile2.Close();

                    StreamWriter SaveFile3 = new StreamWriter(Variables.TempPath + "comhistory.txt");
                    foreach (string item in main.comNameTextBox.Items)
                    {
                        if (item != "")
                        {
                            SaveFile3.WriteLine(item);
                        }
                    }
                    SaveFile3.Close();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText(Variables.TempPath + "OnCloseErrorLog.txt", ex.ToString());
                }
                catch (Exception exx)
                {
                    Debug.WriteLine(exx.ToString());
                }
            }
        }
    }
}
