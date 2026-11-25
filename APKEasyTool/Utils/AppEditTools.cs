using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace APKEasyTool
{
    /// <summary>Icon and package-name changes on a decompiled (apktool) project.</summary>
    public static class AppEditTools
    {
        public const string IconName = "aet_icon";

        static readonly string[] ComponentTags = { "application", "activity", "activity-alias", "service", "receiver", "provider", "instrumentation" };
        static readonly string[] ComponentAttrs = { "name", "targetActivity", "backupAgent", "manageSpaceActivity", "parentActivityName" };
        static readonly KeyValuePair<string, int>[] Densities =
        {
            new KeyValuePair<string, int>("mdpi", 48),
            new KeyValuePair<string, int>("hdpi", 72),
            new KeyValuePair<string, int>("xhdpi", 96),
            new KeyValuePair<string, int>("xxhdpi", 144),
            new KeyValuePair<string, int>("xxxhdpi", 192),
        };
        static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        static string ManifestPath(string projectDir)
        {
            return Path.Combine(projectDir, "AndroidManifest.xml");
        }

        public static string GetPackage(string projectDir)
        {
            Match m = Regex.Match(File.ReadAllText(ManifestPath(projectDir), Encoding.UTF8), @"<manifest\b[^>]*?\spackage=""([^""]+)""", RegexOptions.Singleline);
            return m.Success ? m.Groups[1].Value : "";
        }

        public static bool IsValidPackage(string pkg)
        {
            return Regex.IsMatch(pkg ?? "", @"^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)+$");
        }

        #region Icon
        /// <summary>Best-effort path of a PNG/JPG for the current launcher icon (for preview), or null.</summary>
        public static string FindCurrentIconImage(string projectDir)
        {
            try
            {
                string manifest = File.ReadAllText(ManifestPath(projectDir), Encoding.UTF8);
                Match app = Regex.Match(manifest, @"<application\b[^>]*>", RegexOptions.Singleline);
                Match icon = Regex.Match(app.Value, @"android:icon=""@(mipmap|drawable)/([^""]+)""");
                if (!icon.Success)
                    return null;
                string found = FindImage(projectDir, icon.Groups[2].Value);
                if (found != null)
                    return found;
                // Adaptive icon: look at its foreground layer.
                foreach (string xml in Directory.GetFiles(Path.Combine(projectDir, "res"), icon.Groups[2].Value + ".xml", SearchOption.AllDirectories))
                {
                    Match fg = Regex.Match(File.ReadAllText(xml), @"<foreground\b[^>]*android:drawable=""@(mipmap|drawable)/([^""]+)""");
                    if (fg.Success)
                        return FindImage(projectDir, fg.Groups[2].Value);
                }
            }
            catch { }
            return null;
        }

        static string FindImage(string projectDir, string name)
        {
            string res = Path.Combine(projectDir, "res");
            string[] order = { "xxxhdpi", "xxhdpi", "xhdpi", "hdpi", "mdpi", "" };
            foreach (string d in order)
                foreach (string dir in Directory.GetDirectories(res).Where(x => (Path.GetFileName(x).StartsWith("mipmap") || Path.GetFileName(x).StartsWith("drawable")) && (d == "" || Path.GetFileName(x).Contains(d))))
                    foreach (string ext in new[] { ".png", ".jpg" })
                    {
                        string f = Path.Combine(dir, name + ext);
                        if (File.Exists(f))
                            return f;
                    }
            return null;
        }

        /// <summary>
        /// Writes the image as res/mipmap-*/aet_icon.png in every density and points the launcher
        /// icon (and round icon) at it. A plain PNG works on every Android version; adaptive icon
        /// XML of the original app is left untouched but no longer used.
        /// </summary>
        public static void ApplyIcon(string projectDir, string imagePath)
        {
            using (var src = LoadImage(imagePath))
            {
                foreach (var d in Densities)
                {
                    string dir = Path.Combine(projectDir, "res", "mipmap-" + d.Key);
                    Directory.CreateDirectory(dir);
                    using (var bmp = Render(src, d.Value))
                        bmp.Save(Path.Combine(dir, IconName + ".png"), ImageFormat.Png);
                }
            }

            string path = ManifestPath(projectDir);
            string manifest = File.ReadAllText(path, Encoding.UTF8);
            string value = "@mipmap/" + IconName;

            manifest = Regex.Replace(manifest, @"<application\b[^>]*>", m =>
            {
                string tag = SetAttr(m.Value, "android:icon", value, true);
                return SetAttr(tag, "android:roundIcon", value, false);
            }, RegexOptions.Singleline);

            // Launcher activities may set their own icon, which wins over the application icon.
            manifest = Regex.Replace(manifest, @"<(activity-alias|activity)\b[^>]*?(/>|>.*?</\1>)", m =>
            {
                if (!m.Value.Contains("android.intent.category.LAUNCHER"))
                    return m.Value;
                Match open = Regex.Match(m.Value, @"^<[^>]*>", RegexOptions.Singleline);
                string tag = SetAttr(open.Value, "android:icon", value, false);
                tag = SetAttr(tag, "android:roundIcon", value, false);
                return tag + m.Value.Substring(open.Length);
            }, RegexOptions.Singleline);

            File.WriteAllText(path, manifest, Utf8NoBom);
        }

        public static Image LoadImage(string file)
        {
            // Copy into memory so the file isn't locked.
            using (var fs = new MemoryStream(File.ReadAllBytes(file)))
            using (var img = Image.FromStream(fs))
                return new Bitmap(img);
        }

        static Bitmap Render(Image src, int size)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                float scale = Math.Min((float)size / src.Width, (float)size / src.Height);
                float w = src.Width * scale, h = src.Height * scale;
                using (var attrs = new ImageAttributes())
                {
                    attrs.SetWrapMode(WrapMode.TileFlipXY); // no dark edges
                    g.DrawImage(src, new Rectangle((int)Math.Round((size - w) / 2), (int)Math.Round((size - h) / 2), (int)Math.Round(w), (int)Math.Round(h)),
                        0, 0, src.Width, src.Height, GraphicsUnit.Pixel, attrs);
                }
            }
            return bmp;
        }

        // Sets attr="value" on an opening tag. When the attribute is missing it is added only if addIfMissing.
        static string SetAttr(string tag, string attr, string value, bool addIfMissing)
        {
            var re = new Regex(Regex.Escape(attr) + @"=""[^""]*""");
            if (re.IsMatch(tag))
                return re.Replace(tag, attr + "=\"" + value + "\"", 1);
            if (!addIfMissing)
                return tag;
            Match name = Regex.Match(tag, @"^<[\w\-:]+");
            return tag.Insert(name.Length, " " + attr + "=\"" + value + "\"");
        }
        #endregion

        #region Package
        public class RenameResult
        {
            public string OldPackage;
            public int RenamedValues, PatchedSmaliFiles, PatchedResFiles;
        }

        /// <summary>
        /// Gives the app a new application id so it installs next to the original. Java/Kotlin class
        /// names stay as they are (relative component names are made absolute first); provider
        /// authorities and the app's own permissions get the new prefix, so they don't clash with
        /// the original app. Optionally patches string constants in smali that equal one of the
        /// renamed authorities/permissions, so code that hard-codes them keeps working.
        /// </summary>
        public static RenameResult RenamePackage(string projectDir, string newPkg, bool patchSmali)
        {
            string path = ManifestPath(projectDir);
            string manifest = File.ReadAllText(path, Encoding.UTF8);
            string oldPkg = GetPackage(projectDir);
            var result = new RenameResult { OldPackage = oldPkg };
            if (oldPkg == "" || oldPkg == newPkg)
                return result;

            // 1. Relative component names (".Main", "Main") -> absolute with the OLD package.
            string tagPattern = @"<(" + String.Join("|", ComponentTags.OrderByDescending(t => t.Length).Select(Regex.Escape)) + @")\b[^>]*>";
            manifest = Regex.Replace(manifest, tagPattern, m =>
            {
                string tag = m.Value;
                foreach (string a in ComponentAttrs)
                    tag = Regex.Replace(tag, @"(android:" + a + @"="")([^""]*)("")", am =>
                    {
                        string v = am.Groups[2].Value;
                        if (v.StartsWith("."))
                            v = oldPkg + v;
                        else if (v.Length > 0 && !v.Contains(".") && !v.StartsWith("@"))
                            v = oldPkg + "." + v;
                        return am.Groups[1].Value + v + am.Groups[3].Value;
                    });
                return tag;
            }, RegexOptions.Singleline);

            // 2. Values that must move to the new package: own permissions and provider authorities.
            var map = new Dictionary<string, string>();
            foreach (Match m in Regex.Matches(manifest, @"<(permission|permission-group|permission-tree)\b[^>]*?android:name=""([^""]+)""", RegexOptions.Singleline))
            {
                string v = m.Groups[2].Value;
                if (v.StartsWith(oldPkg + "."))
                    map[v] = newPkg + v.Substring(oldPkg.Length);
            }
            foreach (Match m in Regex.Matches(manifest, @"android:authorities=""([^""]+)"""))
                foreach (string auth in m.Groups[1].Value.Split(';'))
                    if (auth == oldPkg || auth.StartsWith(oldPkg + "."))
                        map[auth] = newPkg + auth.Substring(oldPkg.Length);

            manifest = Regex.Replace(manifest, @"(android:authorities="")([^""]+)("")", m =>
                m.Groups[1].Value + String.Join(";", m.Groups[2].Value.Split(';').Select(a => map.ContainsKey(a) ? map[a] : a)) + m.Groups[3].Value);
            manifest = ReplaceAttrValues(manifest, map);

            // 3. The application id itself.
            manifest = Regex.Replace(manifest, @"(<manifest\b[^>]*?\spackage="")" + Regex.Escape(oldPkg) + @"""", m => m.Groups[1].Value + newPkg + "\"", RegexOptions.Singleline);
            File.WriteAllText(path, manifest, Utf8NoBom);
            result.RenamedValues = map.Count;

            // 4. Resource XML: shortcut/intent targetPackage and references to renamed authorities.
            string resDir = Path.Combine(projectDir, "res");
            var resMap = new Dictionary<string, string>(map);
            foreach (string xml in Directory.GetFiles(resDir, "*.xml", SearchOption.AllDirectories))
            {
                string dirName = Path.GetFileName(Path.GetDirectoryName(xml));
                if (dirName.StartsWith("values"))
                    continue;
                string text = File.ReadAllText(xml, Encoding.UTF8);
                if (!text.Contains(oldPkg))
                    continue;
                string updated = Regex.Replace(text, @"(android:targetPackage="")" + Regex.Escape(oldPkg) + @"""", m => m.Groups[1].Value + newPkg + "\"");
                updated = ReplaceAttrValues(updated, resMap);
                if (updated != text)
                {
                    File.WriteAllText(xml, updated, Utf8NoBom);
                    result.PatchedResFiles++;
                }
            }

            // 5. Smali string constants equal to a renamed value.
            if (patchSmali && map.Count > 0)
            {
                var constRe = new Regex(@"(const-string(?:/jumbo)? [vp]\d+, "")([^""]*)("")");
                foreach (string dir in Directory.GetDirectories(projectDir, "smali*"))
                    foreach (string smali in Directory.EnumerateFiles(dir, "*.smali", SearchOption.AllDirectories))
                    {
                        string text = File.ReadAllText(smali, Encoding.UTF8);
                        if (!text.Contains(oldPkg))
                            continue;
                        string updated = constRe.Replace(text, m =>
                        {
                            string v;
                            return map.TryGetValue(m.Groups[2].Value, out v) ? m.Groups[1].Value + v + m.Groups[3].Value : m.Value;
                        });
                        if (updated != text)
                        {
                            File.WriteAllText(smali, updated, Utf8NoBom);
                            result.PatchedSmaliFiles++;
                        }
                    }
            }
            return result;
        }

        // Replaces any attribute value that exactly equals a key of the map.
        static string ReplaceAttrValues(string xml, Dictionary<string, string> map)
        {
            if (map.Count == 0)
                return xml;
            return Regex.Replace(xml, @"(\s[\w:]+="")([^""]*)("")", m =>
            {
                string v;
                return map.TryGetValue(m.Groups[2].Value, out v) ? m.Groups[1].Value + v + m.Groups[3].Value : m.Value;
            });
        }
        #endregion
    }
}
