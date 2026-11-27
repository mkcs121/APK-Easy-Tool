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
        public static async Task<bool> MergeToInstallable(MainForm main, string input)
        {
            string merged = await Merge(main, input);
            if (merged == null)
                return false;
            bool ok = true;
            if (main.zipAfterSignChkBox.Checked)
                ok = await Apktool.ZipAlign(merged, merged); // signs afterwards when "Sign APK after compile" is on
            else if (main.signApkCheckBox.Checked)
                ok = await Apktool.Sign(merged, merged);
            if (ok)
                main.LogOutput("Ready: " + merged, MainForm.Type.Info);
            else
                main.LogOutput("Merged, but zipalign/signing failed, so it can't be installed yet: " + merged, MainForm.Type.Error);

            // The log line is easy to miss: say where the file is and offer to show it.
            string msg = (ok ? "The split APK was merged into one installable APK:" : "The split APK was merged, but zipalign/signing failed (see the log):")
                + "\n\n" + merged + "\n\nOpen the folder?";
            if (System.Windows.Forms.MessageBox.Show(main, msg, Program.Name, System.Windows.Forms.MessageBoxButtons.YesNo,
                    ok ? System.Windows.Forms.MessageBoxIcon.Information : System.Windows.Forms.MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
                System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + merged + "\"");
            return ok;
        }

        static readonly System.Text.RegularExpressions.Regex PackageName =
            new System.Text.RegularExpressions.Regex(@"^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+$");

        // Where an archive entry may be extracted, or null when the entry is not wanted / not safe.
        // Only top-level *.apk files and Android/obb/<package>/<name>.obb are accepted, and the final
        // path must stay inside the temporary folder (no "..", drive letters or absolute names).
        static string SafeDestination(string tempRoot, string entryName, out string obbPackage)
        {
            obbPackage = null;
            string name = entryName.Replace('\\', '/');
            string[] parts = name.Split('/');
            bool apk = parts.Length == 1 && name.EndsWith(".apk", StringComparison.OrdinalIgnoreCase);
            bool obb = parts.Length == 4 && parts[0].Equals("Android", StringComparison.OrdinalIgnoreCase)
                && parts[1].Equals("obb", StringComparison.OrdinalIgnoreCase)
                && PackageName.IsMatch(parts[2]) && parts[3].EndsWith(".obb", StringComparison.OrdinalIgnoreCase);
            if (!apk && !obb)
                return null;
            foreach (string part in parts)
                if (part == "" || part == "." || part == ".." || part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    throw new InvalidDataException("Unsafe file name inside the bundle: " + entryName);
            string root = Path.GetFullPath(tempRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string dest = Path.GetFullPath(Path.Combine(root, name.Replace('/', Path.DirectorySeparatorChar)));
            if (!dest.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Unsafe file name inside the bundle: " + entryName);
            if (obb)
                obbPackage = parts[2];
            return dest;
        }

        /// <summary>Installs every APK in the bundle with "adb install-multiple" and copies OBB data.</summary>
        public static async Task<bool> Install(MainForm main, string input)
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
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    using (ZipArchive zip = ZipFile.OpenRead(input))
                    {
                        // Check every entry before writing anything.
                        var plan = new List<KeyValuePair<ZipArchiveEntry, string>>();
                        var packages = new Dictionary<string, string>();
                        foreach (ZipArchiveEntry e in zip.Entries)
                        {
                            if (e.FullName.EndsWith("/") || e.Length == 0)
                                continue;
                            string pkg;
                            string dest = SafeDestination(temp, e.FullName, out pkg);
                            if (dest == null)
                                continue;
                            if (!seen.Add(dest))
                                throw new InvalidDataException("The bundle contains the same file twice: " + e.FullName);
                            plan.Add(new KeyValuePair<ZipArchiveEntry, string>(e, dest));
                            if (pkg != null)
                                packages[dest] = pkg;
                        }
                        foreach (var item in plan)
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(item.Value));
                            item.Key.ExtractToFile(item.Value, true);
                            string pkg;
                            if (packages.TryGetValue(item.Value, out pkg))
                                obbs.Add(new KeyValuePair<string, string>(item.Value, pkg));
                            else
                                apks.Add(item.Value);
                        }
                    }
                });
                if (apks.Count == 0)
                {
                    main.LogOutput("No APK files found inside " + Path.GetFileName(input), MainForm.Type.Error);
                    return false;
                }

                string adb = Variables.RealPath("Resources\\adb.exe");
                int exitCode = 0;
                string command = "install-multiple -r " + String.Join(" ", apks.Select(a => "\"" + a + "\""));
                await Task.Factory.StartNew(() => CMD.StartProgram(adb, command, true, out exitCode));
                if (exitCode != 0)
                {
                    main.LogOutput("\n" + Lang.INS_FAIL_MBOX, MainForm.Type.Error);
                    return false;
                }
                main.LogOutput("\n" + Lang.INS_SUCCESS_MBOX, MainForm.Type.Info);

                var failed = new List<string>();
                foreach (var obb in obbs)
                {
                    string remoteDir = "/sdcard/Android/obb/" + obb.Value;
                    int mkdirCode = 0, pushCode = 0;
                    await Task.Factory.StartNew(() =>
                    {
                        CMD.StartProgram(adb, "shell mkdir -p \"" + remoteDir + "\"", true, out mkdirCode);
                        if (mkdirCode == 0)
                            CMD.StartProgram(adb, "push \"" + obb.Key + "\" \"" + remoteDir + "/\"", true, out pushCode);
                    });
                    if (mkdirCode != 0 || pushCode != 0)
                        failed.Add(Path.GetFileName(obb.Key) + (mkdirCode != 0 ? " (could not create " + remoteDir + ")" : ""));
                }
                if (obbs.Count > 0)
                {
                    int copied = obbs.Count - failed.Count;
                    main.LogOutput("OBB data: " + copied + " of " + obbs.Count + " file(s) copied" +
                        (failed.Count > 0 ? ". Failed: " + String.Join(", ", failed) : "."), failed.Count > 0 ? MainForm.Type.Error : MainForm.Type.Info);
                }
                return failed.Count == 0;
            }
            catch (Exception ex)
            {
                main.LogOutput("Installing failed: " + ex.Message, MainForm.Type.Error);
                return false;
            }
            finally
            {
                main.EnableForm();
                try { Directory.Delete(temp, true); } catch { }
            }
        }
    }
}
