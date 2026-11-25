using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace APKEasyTool
{
    /// <summary>
    /// Disables known ad SDKs inside a decompiled app by patching their smali:
    /// void load/show/init-style methods return immediately and isLoaded/isReady-style
    /// checks return false, so the app never gets an ad to display. Every touched file is
    /// backed up first, so Restore puts the original code back.
    /// </summary>
    public static class AdRemover
    {
        public const string BackupDirName = "aet_backup_ads";

        public class AdSdk
        {
            public string Name;
            public string[] Packages;          // smali package paths, e.g. "com/applovin"
            public string[] Exclude = new string[0];
            public int Files;                  // filled by Detect
            public override string ToString()
            {
                return Name + "  (" + Files + " classes)";
            }
        }

        public class Result
        {
            public int Files, Methods;
        }

        static AdSdk Sdk(string name, string[] packages, params string[] exclude)
        {
            return new AdSdk { Name = name, Packages = packages, Exclude = exclude };
        }

        public static List<AdSdk> KnownSdks()
        {
            return new List<AdSdk>
            {
                // Advertising ID (gms/ads/identifier) is shared with analytics/Firebase: keep it.
                Sdk("Google AdMob / Ad Manager", new[] { "com/google/android/gms/ads", "com/google/android/gms/internal/ads" }, "com/google/android/gms/ads/identifier"),
                Sdk("AppLovin / MAX", new[] { "com/applovin" }),
                Sdk("Unity Ads", new[] { "com/unity3d/ads", "com/unity3d/services" }),
                Sdk("Meta Audience Network", new[] { "com/facebook/ads" }),
                Sdk("ironSource / LevelPlay", new[] { "com/ironsource", "com/unity3d/mediation" }),
                Sdk("Vungle / Liftoff", new[] { "com/vungle" }),
                Sdk("Chartboost", new[] { "com/chartboost" }),
                Sdk("Mintegral", new[] { "com/mbridge" }),
                Sdk("InMobi", new[] { "com/inmobi" }),
                Sdk("Pangle (TikTok)", new[] { "com/bytedance/sdk/openadsdk" }),
                Sdk("Yandex Ads", new[] { "com/yandex/mobile/ads" }),
                Sdk("Start.io", new[] { "com/startapp" }),
                Sdk("AdColony", new[] { "com/adcolony" }),
                Sdk("Fyber / DT Exchange", new[] { "com/fyber" }),
                Sdk("Tapsell", new[] { "ir/tapsell" }),
                Sdk("Adivery", new[] { "com/adivery" }),
                Sdk("Yektanet", new[] { "co/yektanet", "ir/yektanet" }),
            };
        }

        static IEnumerable<string> SmaliRoots(string projectDir)
        {
            return Directory.GetDirectories(projectDir).Where(d =>
            {
                string n = Path.GetFileName(d);
                return n == "smali" || n.StartsWith("smali_");
            });
        }

        static IEnumerable<string> SdkFiles(string projectDir, AdSdk sdk)
        {
            foreach (string root in SmaliRoots(projectDir))
                foreach (string pkg in sdk.Packages)
                {
                    string dir = Path.Combine(root, pkg.Replace('/', Path.DirectorySeparatorChar));
                    if (!Directory.Exists(dir))
                        continue;
                    foreach (string f in Directory.EnumerateFiles(dir, "*.smali", SearchOption.AllDirectories))
                    {
                        string rel = f.Substring(root.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
                        if (!sdk.Exclude.Any(x => rel.StartsWith(x + "/")))
                            yield return f;
                    }
                }
        }

        /// <summary>Ad SDKs present in the project, with their class counts.</summary>
        public static List<AdSdk> Detect(string projectDir)
        {
            var found = new List<AdSdk>();
            foreach (AdSdk sdk in KnownSdks())
            {
                sdk.Files = SdkFiles(projectDir, sdk).Count();
                if (sdk.Files > 0)
                    found.Add(sdk);
            }
            return found;
        }

        public static bool HasBackup(string projectDir)
        {
            return Directory.Exists(Path.Combine(projectDir, BackupDirName));
        }

        // void methods that fetch/show/start ads
        static readonly Regex VoidName = new Regex(@"^(load|show|init|initialize|fetch|request|preload|start|display|present|play|cache)", RegexOptions.IgnoreCase);
        // boolean "is there an ad?" checks
        static readonly Regex BoolName = new Regex(@"^(is|has|can)(Ad)?(Loaded|Ready|Available|Cached|Valid|Initialized)", RegexOptions.IgnoreCase);
        static readonly Regex MethodRe = new Regex(@"^\.method ([^\r\n]*?)([\w$<>]+)\(([^)\r\n]*)\)([^\r\n]+)\r?\n.*?^\.end method", RegexOptions.Singleline | RegexOptions.Multiline);

        public static Result Remove(string projectDir, IEnumerable<AdSdk> sdks, Action<string> status)
        {
            var result = new Result();
            string backupRoot = Path.Combine(projectDir, BackupDirName);
            foreach (AdSdk sdk in sdks)
            {
                if (status != null)
                    status("Patching " + sdk.Name + "...");
                foreach (string file in SdkFiles(projectDir, sdk).ToList())
                {
                    string text = File.ReadAllText(file, Encoding.UTF8);
                    int count = 0;
                    string patched = MethodRe.Replace(text, m =>
                    {
                        string mods = m.Groups[1].Value, name = m.Groups[2].Value, ret = m.Groups[4].Value.Trim();
                        if (mods.Contains("abstract") || mods.Contains("native") || name.StartsWith("<"))
                            return m.Value;
                        string header = ".method " + mods + name + "(" + m.Groups[3].Value + ")" + ret;
                        if (ret == "V" && VoidName.IsMatch(name))
                        {
                            count++;
                            return header + "\n    .locals 0\n\n    return-void\n.end method";
                        }
                        if (ret == "Z" && BoolName.IsMatch(name))
                        {
                            count++;
                            return header + "\n    .locals 1\n\n    const/4 v0, 0x0\n\n    return v0\n.end method";
                        }
                        return m.Value;
                    });
                    if (count == 0)
                        continue;

                    string root = SmaliRoots(projectDir).First(r => file.StartsWith(r + Path.DirectorySeparatorChar));
                    string backup = Path.Combine(backupRoot, Path.GetFileName(root), file.Substring(root.Length + 1));
                    if (!File.Exists(backup)) // keep the very first original
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(backup));
                        File.Copy(file, backup);
                    }
                    File.WriteAllText(file, patched, new UTF8Encoding(false));
                    result.Files++;
                    result.Methods += count;
                }
            }
            return result;
        }

        /// <summary>Puts every backed-up smali file back and removes the backup.</summary>
        public static int Restore(string projectDir)
        {
            string backupRoot = Path.Combine(projectDir, BackupDirName);
            if (!Directory.Exists(backupRoot))
                return 0;
            int n = 0;
            foreach (string f in Directory.EnumerateFiles(backupRoot, "*.smali", SearchOption.AllDirectories))
            {
                string dest = Path.Combine(projectDir, f.Substring(backupRoot.Length + 1));
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                File.Copy(f, dest, true);
                n++;
            }
            Directory.Delete(backupRoot, true);
            return n;
        }
    }
}
