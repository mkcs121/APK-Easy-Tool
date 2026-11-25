using APKEasyTool.Enums;
using APKEasyTool.Forms;
using APKEasyTool.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace APKEasyTool
{
    /// <summary>
    /// Split APK bundles (.apks / .xapk / .apkm): merge them into one APK with APKEditor
    /// (REAndroid, Apache-2.0), or install all parts (+ OBB data) with adb.
    /// </summary>
    public static class SplitApk
    {
        static readonly string[] Extensions = { ".apks", ".xapk", ".apkm" };
        public const string DialogFilter = "Split APK (*.apks;*.xapk;*.apkm)|*.apks;*.xapk;*.apkm";

        public static bool IsSplit(string path)
        {
            return Extensions.Contains((Path.GetExtension(path ?? "") ?? "").ToLowerInvariant());
        }

        static string MergedPath(MainForm main, string input)
        {
            string dir = Directory.Exists(main.pathOfExt.Text) ? main.pathOfExt.Text : Path.GetDirectoryName(input);
            return Path.Combine(dir, Path.GetFileNameWithoutExtension(input) + " (merged).apk");
        }

        /// <summary>Merges the bundle into one unsigned APK. Returns its path, or null on failure.</summary>
        public static async Task<string> Merge(MainForm main, string input)
        {
            string jar = Variables.RealPath("Resources\\APKEditor.jar");
            if (!File.Exists(jar))
            {
                main.LogOutput("APKEditor.jar is missing from the Resources folder; split APKs can't be merged.", MainForm.Type.Error);
                return null;
            }
            string output = MergedPath(main, input);
            Apktool.processType = ProcessType.Java;
            main.DisableForm();
            main.LogOutput("---------------------------\n");
            main.LogOutput("Merging split APK into one APK (all languages, screen sizes and CPU types)...", MainForm.Type.Info);

            string command = Apktool.GetXmx() + "-jar \"" + jar + "\" m -i \"" + input + "\" -o \"" + output + "\" -f -clean-meta";
            int exitCode = 0;
            await Task.Factory.StartNew(() => CMD.StartProgram(Apktool.GetJavaPath(), command, true, out exitCode));
            main.EnableForm();

            if (exitCode != 0 || !File.Exists(output))
            {
                main.LogOutput("Merging failed. Please read the log.", MainForm.Type.Error);
                return null;
            }
            main.LogOutput("Merged: " + output, MainForm.Type.Info);
            return output;
        }

        /// <summary>Merges, then zipaligns and signs (per the options), so the result installs directly.</summary>
        public static async Task MergeToInstallable(MainForm main, string input)
        {
            string merged = await Merge(main, input);
            if (merged == null)
                return;
            if (main.zipAfterSignChkBox.Checked)
                await Apktool.ZipAlign(merged, merged); // signs afterwards when "Sign APK after compile" is on
            else if (main.signApkCheckBox.Checked)
                await Apktool.Sign(merged, merged);
            main.LogOutput("Ready: " + merged, MainForm.Type.Info);
        }

        /// <summary>Installs every APK in the bundle with "adb install-multiple" and copies OBB data.</summary>
        public static async Task Install(MainForm main, string input)
        {
            string temp = Path.Combine(Path.GetTempPath(), "aet_split_" + Guid.NewGuid().ToString("N"));
            Apktool.processType = ProcessType.Adb;
            main.DisableForm();
            main.LogOutput("---------------------------\n");
            main.LogOutput("Installing split APK: " + Path.GetFileName(input), MainForm.Type.Info);
            try
            {
                var apks = new List<string>();
                var obbs = new List<KeyValuePair<string, string>>(); // local file -> package
                await Task.Factory.StartNew(() =>
                {
                    using (ZipArchive zip = ZipFile.OpenRead(input))
                        foreach (ZipArchiveEntry e in zip.Entries)
                        {
                            if (e.FullName.EndsWith("/") || e.Length == 0)
                                continue;
                            string name = e.FullName.Replace('\\', '/');
                            bool apk = name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) && !name.Contains("/");
                            string[] parts = name.Split('/');
                            // XAPK: Android/obb/<package>/<file>.obb
                            bool obb = parts.Length == 4 && parts[0].Equals("Android", StringComparison.OrdinalIgnoreCase)
                                && parts[1].Equals("obb", StringComparison.OrdinalIgnoreCase);
                            if (!apk && !obb)
                                continue;
                            string dest = Path.Combine(temp, name.Replace('/', Path.DirectorySeparatorChar));
                            Directory.CreateDirectory(Path.GetDirectoryName(dest));
                            e.ExtractToFile(dest, true);
                            if (apk)
                                apks.Add(dest);
                            else
                                obbs.Add(new KeyValuePair<string, string>(dest, parts[2]));
                        }
                });
                if (apks.Count == 0)
                {
                    main.LogOutput("No APK files found inside " + Path.GetFileName(input), MainForm.Type.Error);
                    return;
                }

                string adb = Variables.RealPath("Resources\\adb.exe");
                int exitCode = 0;
                string command = "install-multiple -r " + String.Join(" ", apks.Select(a => "\"" + a + "\""));
                await Task.Factory.StartNew(() => CMD.StartProgram(adb, command, true, out exitCode));
                if (exitCode != 0)
                {
                    main.LogOutput("\n" + Lang.INS_FAIL_MBOX, MainForm.Type.Error);
                    return;
                }

                foreach (var obb in obbs)
                {
                    string remoteDir = "/sdcard/Android/obb/" + obb.Value;
                    int code = 0;
                    await Task.Factory.StartNew(() =>
                    {
                        CMD.StartProgram(adb, "shell mkdir -p \"" + remoteDir + "\"", true, out code);
                        CMD.StartProgram(adb, "push \"" + obb.Key + "\" \"" + remoteDir + "/\"", true, out code);
                    });
                    if (code != 0)
                        main.LogOutput("Copying OBB data failed: " + Path.GetFileName(obb.Key), MainForm.Type.Error);
                }
                main.LogOutput("\n" + Lang.INS_SUCCESS_MBOX + (obbs.Count > 0 ? " (" + obbs.Count + " OBB file(s) copied)" : ""), MainForm.Type.Info);
            }
            catch (Exception ex)
            {
                main.LogOutput("Installing failed: " + ex.Message, MainForm.Type.Error);
            }
            finally
            {
                main.EnableForm();
                try { Directory.Delete(temp, true); } catch { }
            }
        }
    }
}
