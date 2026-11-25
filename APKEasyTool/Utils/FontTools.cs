using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace APKEasyTool
{
    /// <summary>Find, replace and set the fonts of a decompiled app. Changed files are backed up for Restore.</summary>
    public static class FontTools
    {
        public const string BackupDirName = "aet_backup_fonts";
        public const string DefaultFontName = "aet_font";
        static readonly string[] FontExt = { ".ttf", ".otf", ".ttc" };
        static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        // System families we keep: replacing a monospace/serif font with a text font would look wrong.
        static readonly Regex KeepFamily = new Regex(@"mono|serif-|^serif$|casual|cursive|smallcaps|condensed", RegexOptions.IgnoreCase);
        static readonly Regex StyleFontItem = new Regex(@"(<item name=""(?:android:)?fontFamily"">)([^<]*)(</item>)");
        static readonly Regex LayoutFontAttr = new Regex(@"((?:android|app):fontFamily="")([^""]*)("")");

        public class FontFile
        {
            public string FullPath, RelPath, Family;
            public long Size;
            public override string ToString()
            {
                return RelPath + "   [" + (Family ?? "?") + ", " + (Size / 1024) + " KB]";
            }
        }

        public static List<FontFile> FindFonts(string projectDir)
        {
            var list = new List<FontFile>();
            var roots = new List<string>();
            string res = Path.Combine(projectDir, "res");
            if (Directory.Exists(res))
                roots.AddRange(Directory.GetDirectories(res, "font*"));
            string assets = Path.Combine(projectDir, "assets");
            if (Directory.Exists(assets))
                roots.Add(assets);
            foreach (string root in roots)
                foreach (string f in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
                {
                    if (!FontExt.Contains(Path.GetExtension(f).ToLowerInvariant()))
                        continue;
                    list.Add(new FontFile
                    {
                        FullPath = f,
                        RelPath = f.Substring(projectDir.Length + 1).Replace('\\', '/'),
                        Family = FamilyName(f),
                        Size = new FileInfo(f).Length
                    });
                }
            return list.OrderBy(x => x.RelPath).ToList();
        }

        /// <summary>Family name from the font's "name" table (TTF/OTF/TTC), or null.</summary>
        public static string FamilyName(string fontFile)
        {
            try
            {
                byte[] d = File.ReadAllBytes(fontFile);
                int font = 0;
                if (d.Length > 16 && d[0] == 't' && d[1] == 't' && d[2] == 'c' && d[3] == 'f')
                    font = (int)U32(d, 12); // first font of a collection
                int numTables = U16(d, font + 4);
                for (int i = 0; i < numTables; i++)
                {
                    int rec = font + 12 + i * 16;
                    if (Encoding.ASCII.GetString(d, rec, 4) != "name")
                        continue;
                    int table = (int)U32(d, rec + 8);
                    int count = U16(d, table + 2), strings = table + U16(d, table + 4);
                    string fallback = null;
                    for (int j = 0; j < count; j++)
                    {
                        int r = table + 6 + j * 12;
                        int platform = U16(d, r), nameId = U16(d, r + 6), len = U16(d, r + 8), off = U16(d, r + 10);
                        if (nameId != 1 && nameId != 16)
                            continue;
                        string v = platform == 3 || platform == 0
                            ? Encoding.BigEndianUnicode.GetString(d, strings + off, len)
                            : Encoding.ASCII.GetString(d, strings + off, len);
                        if (nameId == 16 && platform == 3)
                            return v; // typographic family, the nicest name
                        if (fallback == null || platform == 3)
                            fallback = v;
                    }
                    return fallback;
                }
            }
            catch { }
            return null;
        }

        static int U16(byte[] d, int i)
        {
            return (d[i] << 8) | d[i + 1];
        }

        static uint U32(byte[] d, int i)
        {
            return (uint)((d[i] << 24) | (d[i + 1] << 16) | (d[i + 2] << 8) | d[i + 3]);
        }

        /// <summary>How text styles/layouts choose their font, e.g. "sans-serif" -> 23.</summary>
        public static List<KeyValuePair<string, int>> FontUsage(string projectDir)
        {
            var counts = new Dictionary<string, int>();
            Action<string> add = v => { counts[v] = counts.ContainsKey(v) ? counts[v] + 1 : 1; };
            var strings = ReadDefaultStrings(projectDir);
            foreach (string file in StyleFiles(projectDir))
                foreach (Match m in StyleFontItem.Matches(File.ReadAllText(file, Encoding.UTF8)))
                    add(Resolve(m.Groups[2].Value.Trim(), strings));
            foreach (string file in LayoutFiles(projectDir))
                foreach (Match m in LayoutFontAttr.Matches(File.ReadAllText(file, Encoding.UTF8)))
                    add(Resolve(m.Groups[2].Value.Trim(), strings));
            return counts.OrderByDescending(kv => kv.Value).ToList();
        }

        /// <summary>Overwrites one font file with another font (file name stays, so the app still finds it).</summary>
        public static void ReplaceFont(string projectDir, string fontRelPath, string newFont)
        {
            string target = Path.Combine(projectDir, fontRelPath.Replace('/', '\\'));
            Backup(projectDir, target);
            File.Copy(newFont, target, true);
        }

        public class DefaultFontResult
        {
            public int StyleItems, LayoutAttrs, Themes;
        }

        /// <summary>
        /// Adds the font as res/font/aet_font.* and points the app/activity themes plus every
        /// text style and layout that used a system sans-serif font at it.
        /// </summary>
        public static DefaultFontResult SetDefaultFont(string projectDir, string newFont)
        {
            var result = new DefaultFontResult();
            string fontDir = Path.Combine(projectDir, "res", "font");
            Directory.CreateDirectory(fontDir);
            foreach (string old in Directory.GetFiles(fontDir, DefaultFontName + ".*"))
                File.Delete(old);
            string ext = Path.GetExtension(newFont).ToLowerInvariant() == ".otf" ? ".otf" : ".ttf";
            File.Copy(newFont, Path.Combine(fontDir, DefaultFontName + ext), true);
            string fontRef = "@font/" + DefaultFontName;

            var strings = ReadDefaultStrings(projectDir);
            bool hasAppCompatAttr = File.Exists(Path.Combine(projectDir, "res", "values", "attrs.xml")) &&
                File.ReadAllText(Path.Combine(projectDir, "res", "values", "attrs.xml"), Encoding.UTF8).Contains("<attr name=\"fontFamily\"");
            var themes = new HashSet<string>(Regex.Matches(File.ReadAllText(Path.Combine(projectDir, "AndroidManifest.xml"), Encoding.UTF8),
                @"android:theme=""@style/([^""]+)""").Cast<Match>().Select(m => m.Groups[1].Value));

            foreach (string file in StyleFiles(projectDir))
            {
                string text = File.ReadAllText(file, Encoding.UTF8);
                int items = 0, themed = 0;
                string updated = StyleFontItem.Replace(text, m =>
                {
                    string v = Resolve(m.Groups[2].Value.Trim(), strings);
                    if (v == fontRef || KeepFamily.IsMatch(v) || v.StartsWith("@font/"))
                        return m.Value;
                    items++;
                    return m.Groups[1].Value + fontRef + m.Groups[3].Value;
                });
                // Themes of the app and its activities get the font too, so plain TextViews use it.
                updated = Regex.Replace(updated, @"<style name=""([^""]+)""([^>]*?)(/>|>(.*?)</style>)", m =>
                {
                    if (!themes.Contains(m.Groups[1].Value))
                        return m.Value;
                    string body = m.Groups[3].Value == "/>" ? "" : m.Groups[4].Value;
                    string add = "";
                    if (!body.Contains("name=\"android:fontFamily\""))
                        add += "\n        <item name=\"android:fontFamily\">" + fontRef + "</item>";
                    if (hasAppCompatAttr && !body.Contains("name=\"fontFamily\""))
                        add += "\n        <item name=\"fontFamily\">" + fontRef + "</item>";
                    if (add == "")
                        return m.Value;
                    themed++;
                    return "<style name=\"" + m.Groups[1].Value + "\"" + m.Groups[2].Value + ">" + body.TrimEnd() + add + "\n    </style>";
                }, RegexOptions.Singleline);
                if (updated != text)
                {
                    Backup(projectDir, file);
                    File.WriteAllText(file, updated, Utf8NoBom);
                    result.StyleItems += items;
                    result.Themes += themed;
                }
            }

            foreach (string file in LayoutFiles(projectDir))
            {
                string text = File.ReadAllText(file, Encoding.UTF8);
                int n = 0;
                string updated = LayoutFontAttr.Replace(text, m =>
                {
                    string v = Resolve(m.Groups[2].Value.Trim(), strings);
                    if (v == fontRef || KeepFamily.IsMatch(v) || v.StartsWith("@font/") || v.StartsWith("?"))
                        return m.Value;
                    n++;
                    return m.Groups[1].Value + fontRef + m.Groups[3].Value;
                });
                if (n > 0)
                {
                    Backup(projectDir, file);
                    File.WriteAllText(file, updated, Utf8NoBom);
                    result.LayoutAttrs += n;
                }
            }
            return result;
        }

        public static bool HasBackup(string projectDir)
        {
            return Directory.Exists(Path.Combine(projectDir, BackupDirName));
        }

        /// <summary>Puts back every changed style/layout/font file and removes the added default font.</summary>
        public static int Restore(string projectDir)
        {
            string backupRoot = Path.Combine(projectDir, BackupDirName);
            int n = 0;
            if (Directory.Exists(backupRoot))
            {
                foreach (string f in Directory.EnumerateFiles(backupRoot, "*", SearchOption.AllDirectories))
                {
                    File.Copy(f, Path.Combine(projectDir, f.Substring(backupRoot.Length + 1)), true);
                    n++;
                }
                Directory.Delete(backupRoot, true);
            }
            string fontDir = Path.Combine(projectDir, "res", "font");
            if (Directory.Exists(fontDir))
            {
                foreach (string f in Directory.GetFiles(fontDir, DefaultFontName + ".*"))
                    File.Delete(f);
                if (!Directory.EnumerateFileSystemEntries(fontDir).Any())
                    Directory.Delete(fontDir);
            }
            return n;
        }

        static void Backup(string projectDir, string file)
        {
            string backup = Path.Combine(projectDir, BackupDirName, file.Substring(projectDir.Length + 1));
            if (File.Exists(backup))
                return; // keep the very first original
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            File.Copy(file, backup);
        }

        static IEnumerable<string> StyleFiles(string projectDir)
        {
            string res = Path.Combine(projectDir, "res");
            if (!Directory.Exists(res))
                return new string[0];
            return Directory.GetDirectories(res, "values*").Select(d => Path.Combine(d, "styles.xml")).Where(File.Exists);
        }

        static IEnumerable<string> LayoutFiles(string projectDir)
        {
            string res = Path.Combine(projectDir, "res");
            if (!Directory.Exists(res))
                return new string[0];
            return Directory.GetDirectories(res, "layout*").SelectMany(d => Directory.GetFiles(d, "*.xml"));
        }

        static Dictionary<string, string> ReadDefaultStrings(string projectDir)
        {
            var map = new Dictionary<string, string>();
            string f = Path.Combine(projectDir, "res", "values", "strings.xml");
            if (File.Exists(f))
                foreach (Match m in Regex.Matches(File.ReadAllText(f, Encoding.UTF8), @"<string name=""([^""]+)""[^>]*>([^<]*)</string>"))
                    map[m.Groups[1].Value] = m.Groups[2].Value;
            return map;
        }

        // "@string/m3_ref_typeface_plain_regular" -> "sans-serif"
        static string Resolve(string value, Dictionary<string, string> strings)
        {
            string v;
            if (value.StartsWith("@string/") && strings.TryGetValue(value.Substring(8), out v))
                return v.Trim();
            return value;
        }
    }
}
