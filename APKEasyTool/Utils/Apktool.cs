using APKEasyTool.Enums;
using APKEasyTool.Forms;
using APKEasyTool.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APKEasyTool
{
    public class Apktool
    {
        #region fields
        private static MainForm main;

        public static ProcessType processType;

        public Process p = new Process();
        #endregion

        public Apktool(MainForm Main)
        {
            main = Main;
        }

        internal static string GetXmx()
        {
            if (main.useJaxaXmxChkBox.Checked)
                return "-Xmx" + main.javaHeapSizeNum.Text + "m ";
            else
                return "";
        }

        // Apktool 3.x renamed or removed several short flags (-b, -k, -m, -c, -d, -nc, -api, --use-aapt2 ...).
        // Detect the major version from the selected jar name, e.g. "apktool_3.0.3.jar".
        internal static bool IsApktool3OrNewer()
        {
            Match m = Regex.Match(Variables.Apktool ?? "", @"apktool[_\-]?(\d+)\.", RegexOptions.IgnoreCase);
            if (m.Success)
                return int.Parse(m.Groups[1].Value) >= 3;
            return !String.IsNullOrEmpty(Variables.ApkToolVer) && Regex.IsMatch(Variables.ApkToolVer.Trim(), @"^([3-9]|\d{2,})\.");
        }

        internal static string GetJavaPath()
        {
            if (main.useExtJava.Checked)
                return "\"" + main.extJavaTxtBox.Text + "\"";
            else
                return "java.exe";
        }

        public static async Task Decompile(string input, string output)
        {
            processType = ProcessType.Java;

            bool v3 = IsApktool3OrNewer();
            string flag = "";
            if (main.decBcheckBox.Checked)
                flag += v3 ? " --no-debug-info" : " -b";
            if (main.decFcheckBox.Checked)
                flag += " -f";
            if (main.decKcheckBox.Checked)
                flag += v3 ? " --keep-broken-res" : " -k";
            if (main.decMcheckBox.Checked)
                flag += v3 ? " --match-original" : " -m";
            if (main.decRcheckBox.Checked)
                flag += " -r";
            if (main.decScheckBox.Checked)
                flag += " -s";
            if (main.decNoAssetsCheckBox.Checked)
                flag += " --no-assets";
            if (!v3 && !String.IsNullOrEmpty(main.decodeApiLvl.Text))
                flag += " -api " + main.decodeApiLvl.Text;
            // Apktool 3 decodes only the main classes*.dex, which rebuilds most reliably.
            if (!v3 && main.decOnlyMainClassesChkBox.Checked)
                flag += " --only-main-classes";
            if (main.useTagChkBox.Checked)
                flag += " -t \"" + main.tagTxtBox.Text + "\"";
            if (!String.IsNullOrEmpty(main.fwDirTxtBox.Text))
                flag += " -p \"" + main.fwDirTxtBox.Text + "\"";
            if (!v3 && main.decForceManifestChkBox.Checked)
                flag += " --force-manifest";

            main.DisableForm();

            main.LogOutput("---------------------------");
            if (Path.GetExtension(input) == ".apk")
                main.LogOutput("\n" + Lang.DEC_APK_FILE_NOTICE + " " + Lang.CANCEL_ESC, MainForm.Type.Info);
            else
                main.LogOutput("\n" + Lang.DEC_JAR_FILE_NOTICE + " " + Lang.CANCEL_ESC, MainForm.Type.Info);

            string command = GetXmx() + " -jar " + Variables.Apktool + " d" + flag +
                            " -o \"" + output + "\" \"" + input + "\"";

            int exitCode = 0;
            await Task.Factory.StartNew(() =>
            {
                CMD.StartProgram(GetJavaPath(), command, true, out exitCode);
            });

            if (exitCode == 0)
            {
                main.LogOutput(Lang.DEC_SUCCESS_MBOX, MainForm.Type.Info);

                if (Environment.GetCommandLineArgs().Length > 2)
                    Environment.Exit(0);
            }
            else
            {
                main.LogOutput(Lang.DEC_FAIL_MBOX, MainForm.Type.Error);
                main.Log("\n" + Lang.PLEASE_READ_FAQ + "\nhttps://forum.xda-developers.com/t/tool-windows-apk-easy-tool-v1-58-3-dec-2020.3333960/#post-65775601", Color.Yellow);
                if (!MainClass.isSidedLogOpened)
                    main.tMain.SelectedIndex = 2;
            }

            main.EnableForm();
        }

        public static async Task Compile(string input, string output)
        {
            processType = ProcessType.Java;

            bool v3 = IsApktool3OrNewer();
            string flag = "";
            //compile
            if (main.comCcheckBox.Checked)
                flag += v3 ? " --copy-original" : " -c";
            if (main.comDcheckBox.Checked)
                flag += v3 ? " --debuggable" : " -d";
            if (main.comFcheckBox.Checked)
                flag += " -f";
            // Apktool 3 always uses aapt2 and no longer accepts -api.
            if (!v3 && main.chkBoxUseAapt2.Checked)
                flag += " --use-aapt2";
            if (!v3 && !String.IsNullOrEmpty(main.rebuildApiLvl.Text))
                flag += " -api " + main.rebuildApiLvl.Text;
            if (main.comNCcheckBox.Checked)
                flag += v3 ? " --no-crunch" : " -nc";
            if (main.useAaptPathChkBox.Checked)
                flag += " --aapt \"" + main.useAaptPathTxtBox.Text + "\"";
            if (!String.IsNullOrEmpty(main.fwDirTxtBox.Text))
                flag += " -p \"" + main.fwDirTxtBox.Text + "\"";

            main.DisableForm();

            main.LogOutput("---------------------------\n");
            main.LogOutput(Lang.COM_APK_FILE_NOTICE + " " + Lang.CANCEL_ESC, MainForm.Type.Info);

            string command = GetXmx() + "-jar " + Variables.Apktool + " b" + flag +
                            " -o \"" + output + "\" \"" + input + "\"";

            int exitCode = 0;
            await Task.Factory.StartNew(() =>
            {
                CMD.StartProgram(GetJavaPath(), command, true, out exitCode);
            });

            if (exitCode == 0)
            {
                main.LogOutput(Lang.COM_SUCCESS_MBOX, MainForm.Type.Info);

                if (File.Exists(Path.Combine(input, "apktool.yml")) && File.Exists(Path.Combine(input, "AndroidManifest.xml")))
                {
                    if (main.zipAfterSignChkBox.Checked)
                    {
                        await ZipAlign(output, output);
                    }
                    else if (main.signApkCheckBox.Checked)
                    {
                        await Sign(output, output);
                    }
                    else
                    {
                        if (Environment.GetCommandLineArgs().Length > 2)
                            Environment.Exit(0);
                    }
                }
            }
            else
            {
                main.LogOutput(Lang.COM_FAIL_MBOX, MainForm.Type.Error);
                main.Log("\n" + Lang.PLEASE_READ_FAQ + "\nhttps://forum.xda-developers.com/t/tool-windows-apk-easy-tool-v1-58-3-dec-2020.3333960/#post-65775601", Color.Yellow);

                if (!MainClass.isSidedLogOpened)
                    main.tMain.SelectedIndex = 2;
            }

            main.EnableForm();
        }

        /// <summary>
        /// Signs input into output. Signs to a temporary file first and verifies it with
        /// "apksigner verify"; output is replaced only after that, so a failed signing keeps the
        /// previous output. Returns true on success. Installs output (the signed file) if enabled.
        /// </summary>
        public static async Task<bool> Sign(string input, string output)
        {
            processType = ProcessType.Java;

            string flag = "";
            if (main.v2signComboBox.SelectedIndex == 1)
                flag += " --v2-signing-enabled true";
            else if (main.v2signComboBox.SelectedIndex == 2)
                flag += " --v2-signing-enabled false";

            if (main.v3signComboBox.SelectedIndex == 1)
                flag += " --v3-signing-enabled true";
            else if (main.v3signComboBox.SelectedIndex == 2)
                flag += " --v3-signing-enabled false";

            if (main.v4signComboBox.SelectedIndex == 1)
                flag += " --v4-signing-enabled true";
            else if (main.v4signComboBox.SelectedIndex == 2)
                flag += " --v4-signing-enabled false";
            else if (main.v4signComboBox.SelectedIndex == 3)
                flag += " --v4-signing-enabled only";

            // No silent fallback to another key: a different key makes the APK impossible to
            // install as an update of the previous one.
            if (main.useJksCheckBox.Checked ? !File.Exists(main.jksFileTxtBox.Text)
                : !File.Exists(main.pk8FileTxtBox.Text) || !File.Exists(main.pemFileTxtBox.Text))
            {
                string which = main.useJksCheckBox.Checked ? main.jksFileTxtBox.Text : main.pk8FileTxtBox.Text + "\n" + main.pemFileTxtBox.Text;
                main.LogOutput("Signing key not found:\n" + which + "\nChoose it again in Options > Signer.", MainForm.Type.Error);
                MessageBox.Show(Lang.KEY_NOT_SEL_MBOX + "\n\n" + which, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            main.DisableForm();
            main.LogOutput("---------------------------\n");
            main.LogOutput(Lang.SIGN_APK_FILE_NOTICE + " " + Lang.CANCEL_ESC, MainForm.Type.Info);

            string temp = TempNextTo(output, "signing");
            Dictionary<string, string> env = null;
            string command;
            if (main.useJksCheckBox.Checked)
            {
                // The password goes through an environment variable: never in the command line or the log.
                env = new Dictionary<string, string> { { "AET_KS_PASS", main.jksPass.Text } };
                command = GetXmx() + "-jar " + Variables.ApkSignerPath + " --ks \"" + main.jksFileTxtBox.Text +
                    "\" --ks-pass env:AET_KS_PASS " + flag + " --out \"" + temp + "\" \"" + input + "\"";
            }
            else
            {
                command = GetXmx() + "-jar " + Variables.ApkSignerPath + " --key \"" + main.pk8FileTxtBox.Text +
                    "\" --cert \"" + main.pemFileTxtBox.Text + "\" " + flag + " --out \"" + temp + "\" \"" + input + "\"";
            }

            int exitCode = 0, verifyCode = -1;
            await Task.Factory.StartNew(() =>
            {
                CMD.StartProgram(GetJavaPath(), command, true, env, out exitCode);
                if (exitCode == 0 && File.Exists(temp))
                    CMD.StartProgram(GetJavaPath(), GetXmx() + "-jar " + Variables.ApkSignerPath.Replace(" sign ", " verify ") + "\"" + temp + "\"", true, out verifyCode);
            });

            bool ok = exitCode == 0 && verifyCode == 0 && Publish(temp, output);
            DeleteQuietly(temp);
            if (temp.EndsWith(".apk"))
                DeleteQuietly(temp + ".idsig");

            if (ok)
            {
                main.LogOutput(Lang.SIGN_SUCCESS_MBOX, MainForm.Type.Info);
                main.EnableForm();
                if (main.installApkChkBox.Checked)
                    return await InstallApk(output);
                if (Environment.GetCommandLineArgs().Length > 2)
                    Environment.Exit(0);
                return true;
            }

            main.LogOutput(Lang.SIGN_FAIL_MBOX + (exitCode == 0 && verifyCode != 0 ? " (verification failed)" : ""), MainForm.Type.Error);
            main.Log("\n" + Lang.PLEASE_READ_FAQ + "\nhttps://forum.xda-developers.com/t/tool-windows-apk-easy-tool-v1-58-3-dec-2020.3333960/#post-65775601", Color.Yellow);
            if (!MainClass.isSidedLogOpened)
                main.tMain.SelectedIndex = 2;
            main.EnableForm();
            return false;
        }

        // A unique temporary file in the same folder as target, so the final move stays on one disk.
        static string TempNextTo(string target, string what)
        {
            return Path.Combine(Path.GetDirectoryName(target),
                Path.GetFileNameWithoutExtension(target) + " " + what + " " + Guid.NewGuid().ToString("N").Substring(0, 8) + ".tmp.apk");
        }

        // Replaces target with the finished temp file. Returns false (and keeps the old target)
        // when the temp file is missing or the target is locked.
        static bool Publish(string temp, string target)
        {
            if (!File.Exists(temp))
                return false;
            try
            {
                if (File.Exists(target))
                    File.Replace(temp, target, null);
                else
                    File.Move(temp, target);
                return true;
            }
            catch (Exception ex)
            {
                main.LogOutput("Could not write " + target + ": " + ex.Message, MainForm.Type.Error);
                return false;
            }
        }

        static void DeleteQuietly(string file)
        {
            try { if (File.Exists(file)) File.Delete(file); } catch { }
        }

        public static async Task<bool> InstallApk(string input)
        {
            processType = ProcessType.Adb;

            main.DisableForm();

            main.LogOutput("---------------------------\n");
            main.LogOutput(Lang.INS_APK_FILE_LOG + " " + Lang.CANCEL_ESC, MainForm.Type.Info);

            string command = "install -r \"" + input + "\"";

            int exitCode = 0;
            await Task.Factory.StartNew(() =>
            {
                CMD.StartProgram(Variables.RealPath("Resources\\adb.exe"), command, true, out exitCode);
            });

            if (exitCode == 0)
            {
                main.LogOutput("\n" + Lang.INS_SUCCESS_MBOX, MainForm.Type.Info);

                if (Environment.GetCommandLineArgs().Length > 2)
                    Environment.Exit(0);
            }
            else
            {
                main.LogOutput("\n" + Lang.INS_FAIL_MBOX, MainForm.Type.Error);
                main.Log("\n" + Lang.PLEASE_READ_FAQ + "\nhttps://forum.xda-developers.com/t/tool-windows-apk-easy-tool-v1-58-3-dec-2020.3333960/#post-65775601", Color.Yellow);

                if (!MainClass.isSidedLogOpened)
                    main.tMain.SelectedIndex = 2;
            }

            main.EnableForm();
            return exitCode == 0;
        }

        /// <summary>
        /// Zipaligns input into output (through a temporary file; output is replaced only when
        /// zipalign succeeded), then signs output when "Sign APK" is enabled.
        /// Returns true only if every step that ran succeeded.
        /// </summary>
        public static async Task<bool> ZipAlign(string input, string output)
        {
            processType = ProcessType.Zipalign;

            string flag = "";
            if (main.zipFcheckBox.Checked)
                flag += " -f";
            if (main.zipPcheckBox.Checked)
                flag += " -P 16"; // 16 KB pages (Android 15+ devices); also valid on 4 KB devices
            if (main.zipZcheckBox.Checked)
                flag += " -z";
            if (main.zipVcheckBox.Checked)
                flag += " -v";

            main.DisableForm();

            main.LogOutput("---------------------------\n");
            main.LogOutput(Lang.ZIPALIGN_APK_FILE_NOTICE + " " + Lang.CANCEL_ESC, MainForm.Type.Info);

            string temp = TempNextTo(output, "zipalign");
            string command = flag + " 4 \"" + input + "\" \"" + temp + "\"";

            int exitCode = 0;
            await Task.Factory.StartNew(() =>
            {
                CMD.StartProgram(Variables.RealPath("Resources\\zipalign.exe"), command, true, out exitCode);
            });

            bool ok = exitCode == 0 && Publish(temp, output);
            DeleteQuietly(temp);

            if (!ok)
            {
                main.LogOutput(Lang.ZIPALIGN_FAIL_MBOX, MainForm.Type.Error);
                main.Log("\n" + Lang.PLEASE_READ_FAQ + "\nhttps://forum.xda-developers.com/t/tool-windows-apk-easy-tool-v1-58-3-dec-2020.3333960/#post-65775601", Color.Yellow);
                if (!MainClass.isSidedLogOpened)
                    main.tMain.SelectedIndex = 2;
                main.EnableForm();
                return false;
            }

            main.LogOutput(Lang.ZIPALIGN_SUCCESS_MBOX, MainForm.Type.Info);
            main.EnableForm();
            if (main.signApkCheckBox.Checked)
                return await Sign(output, output);
            if (Environment.GetCommandLineArgs().Length > 2)
                Environment.Exit(0);
            return true;
        }

        public static async Task CheckAlign(string input)
        {
            processType = ProcessType.Zipalign;

            string flag = "";
            if (main.zipFcheckBox.Checked)
                flag += " -f";
            if (main.zipPcheckBox.Checked)
                flag += " -P 16"; // 16 KB pages (Android 15+ devices); also valid on 4 KB devices
            if (main.zipZcheckBox.Checked)
                flag += " -z";
            if (main.zipVcheckBox.Checked)
                flag += " -v";

            main.DisableForm();

            main.LogOutput("---------------------------\n");
            main.LogOutput(Lang.VERI_APK_FILE_LOG + " " + Lang.CANCEL_ESC, MainForm.Type.Info);
            if (!main.zipVcheckBox.Checked)
                main.LogOutput("\n\n" + Lang.ZIPALIGN_OUTPUT_DIS_LOG);

            string command = "-c" + flag + " 4 \"" + input + "\"";

            int exitCode = 0;
            await Task.Factory.StartNew(() =>
            {
                CMD.StartProgram(Variables.RealPath("Resources\\zipalign.exe"), command, true, out exitCode);
            });

            if (exitCode == 0)
            {
                main.LogOutput(Lang.VERI_SUCCESS_MBOX, MainForm.Type.Info);
            }
            else
            {
                main.LogOutput(Lang.VERI_FAIL_MBOX, MainForm.Type.Error);
                main.Log("\n" + Lang.PLEASE_READ_FAQ + "\nhttps://forum.xda-developers.com/t/tool-windows-apk-easy-tool-v1-58-3-dec-2020.3333960/#post-65775601", Color.Yellow);

                if (!MainClass.isSidedLogOpened)
                    main.tMain.SelectedIndex = 2;
            }

            main.EnableForm();
        }

        public static async Task Framework(string input)
        {
            processType = ProcessType.Java;

            string fwtag, fwpath;
            if (main.installFwDirTxtBox.Text != "")
                fwpath = "-p \"" + main.installFwDirTxtBox.Text + "\"";
            else
                fwpath = "";

            if (main.tagFwTxtBox.Text != "")
                fwtag = "-t \"" + main.tagFwTxtBox.Text + "\"";
            else
                fwtag = "";

            main.DisableForm();

            main.LogOutput("---------------------------\n");
            main.LogOutput(Lang.FW_APK_FILE_LOG, MainForm.Type.Info);

            string command = GetXmx() + "-jar " + Variables.Apktool + " if \"" + input + "\" " + fwtag + " " + fwpath + " ";

            int exitCode = 0;
            await Task.Factory.StartNew(() =>
            {
                CMD.StartProgram(GetJavaPath(), command, true, out exitCode);
            });

            if (exitCode == 0)
            {
                main.LogOutput(Lang.FW_SUCCESS_MBOX, MainForm.Type.Info);
            }
            else
            {
                main.LogOutput(Lang.FW_FAIL_MBOX, MainForm.Type.Error);
                main.Log("\n" + Lang.PLEASE_READ_FAQ + "\nhttps://forum.xda-developers.com/t/tool-windows-apk-easy-tool-v1-58-3-dec-2020.3333960/#post-65775601", Color.Yellow);

                if (!MainClass.isSidedLogOpened)
                    main.tMain.SelectedIndex = 2;
            }

            main.EnableForm();
        }

        public static async Task FrameworkClear()
        {
            processType = ProcessType.Java;

            main.DisableForm();

            main.LogOutput("---------------------------\n");
            main.LogOutput(Lang.FW_CLR_APK_FILE_LOG, MainForm.Type.Info);

            string command = GetXmx() + " -jar " + Variables.Apktool + " empty-framework-dir --force \"";

            int exitCode = 0;
            await Task.Factory.StartNew(() =>
            {
                CMD.StartProgram(GetJavaPath(), command, true, out exitCode);
            });

            if (exitCode == 0)
            {
                main.LogOutput(Lang.FW_CLR_SUCCESS_MBOX, MainForm.Type.Info);
            }
            else
            {
                main.LogOutput(Lang.FW_CLR_FAIL_MBOX, MainForm.Type.Error);
            }

            main.EnableForm();
        }

        public static async Task ZipApk(string input, string output)
        {
            processType = ProcessType.SevenZip;

            main.DisableForm();

            main.LogOutput("---------------------------\n");
            main.LogOutput(Lang.ZIP_APK_FILE_LOG, MainForm.Type.Info);

            string command = "a -tzip \"" + output + "\" \"" + input + "\\*\" -mx" + main.sevenzComUpDown.Text + " -aoa";

            int exitCode = 0;
            await Task.Factory.StartNew(() =>
            {
                CMD.StartProgram(Variables.RealPath("Resources\\7z.exe"), command, true, out exitCode);
            });

            if (exitCode == 0)
            {
                main.LogOutput(Lang.ZIP_SUCCESS_MBOX, MainForm.Type.Info);
            }
            else
            {
                main.LogOutput(Lang.ZIP_FAIL_MBOX, MainForm.Type.Error);
                main.Log("\n" + Lang.PLEASE_READ_FAQ + "\nhttps://forum.xda-developers.com/t/tool-windows-apk-easy-tool-v1-58-3-dec-2020.3333960/#post-65775601", Color.Yellow);

                if (!MainClass.isSidedLogOpened)
                    main.tMain.SelectedIndex = 2;
            }

            main.EnableForm();
        }

        public static async Task ExtractApk(string input, string output)
        {
            processType = ProcessType.SevenZip;

            main.DisableForm();

            main.LogOutput("---------------------------\n");
            main.LogOutput(Lang.EXT_APK_FILE_LOG, MainForm.Type.Info);

            string command = "x \"" + input + "\" -o\"" + output + "\" -aoa";

            int exitCode = 0;
            await Task.Factory.StartNew(() =>
            {
                CMD.StartProgram(Variables.RealPath("Resources\\7z.exe"), command, true, out exitCode);
            });

            if (exitCode == 0)
            {
                main.LogOutput(Lang.EXT_SUCCESS_MBOX, MainForm.Type.Info);
            }
            else
            {
                main.LogOutput(Lang.EXT_FAIL_MBOX, MainForm.Type.Error);
                main.Log("\n" + Lang.PLEASE_READ_FAQ + "\nhttps://forum.xda-developers.com/t/tool-windows-apk-easy-tool-v1-58-3-dec-2020.3333960/#post-65775601", Color.Yellow);

                if (!MainClass.isSidedLogOpened)
                    main.tMain.SelectedIndex = 2;
            }

            main.EnableForm();
        }

        public static async Task Baksmali(string input, string output)
        {
            processType = ProcessType.Java;

            string de;
            if (main.deodexChkBox.Checked)
                de = "x";
            else
                de = "d";

            main.DisableForm();

            main.LogOutput("---------------------------\n");
            main.LogOutput(Lang.BAKSMALI_LOG, MainForm.Type.Info);

            string command = GetXmx() + " -jar " + "\"" + Variables.RealPath("Resources\\baksmali.jar") + "\" " + de + " \"" + input + "\" -o \"" + output + "\"";

            int exitCode = 0;
            await Task.Factory.StartNew(() =>
            {
                CMD.StartProgram(GetJavaPath(), command, true, out exitCode);
            });

            if (exitCode == 0)
            {
                main.LogOutput(Lang.BAKSMALI_SUCCESS_MBOX, MainForm.Type.Info);
            }
            else
            {
                main.LogOutput(Lang.BAKSMALI_FAIL_MBOX, MainForm.Type.Error);
                main.Log("\n" + Lang.PLEASE_READ_FAQ + "\nhttps://forum.xda-developers.com/t/tool-windows-apk-easy-tool-v1-58-3-dec-2020.3333960/#post-65775601", Color.Yellow);

                if (!MainClass.isSidedLogOpened)
                    main.tMain.SelectedIndex = 2;
            }

            main.EnableForm();
        }

        public static async Task Smali(string input, string output)
        {
            processType = ProcessType.Java;

            main.DisableForm();

            main.LogOutput("---------------------------\n");
            main.LogOutput(Lang.SMALI_LOG, MainForm.Type.Info);

            string command = GetXmx() + " -jar " + "\"" + Variables.RealPath("Resources\\smali.jar") + "\" a \"" + input + "\" -o \"" + output + "\"";

            int exitCode = 0;
            await Task.Factory.StartNew(() =>
            {
                CMD.StartProgram(GetJavaPath(), command, true, out exitCode);
            });

            if (exitCode == 0)
            {
                main.LogOutput(Lang.SMALI_SUCCESS_MBOX, MainForm.Type.Info);
            }
            else
            {
                main.LogOutput(Lang.SMALI_FAIL_MBOX, MainForm.Type.Error);
                main.Log("\n" + Lang.PLEASE_READ_FAQ + "\nhttps://forum.xda-developers.com/t/tool-windows-apk-easy-tool-v1-58-3-dec-2020.3333960/#post-65775601", Color.Yellow);

                if (!MainClass.isSidedLogOpened)
                    main.tMain.SelectedIndex = 2;
            }

            main.EnableForm();
        }
    }
}


