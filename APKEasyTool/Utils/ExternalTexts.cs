using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace APKEasyTool
{
    /// <summary>One translatable text outside res/values (a JSON language file entry or a web app string).</summary>
    public class ExtText
    {
        public string Key;       // stable key, used in the sidecar file
        public string Original;  // text as the user sees it
        public string Where;     // file(s) it comes from
    }

    /// <summary>
    /// Texts kept outside Android resources:
    ///  - JSON language files (games like Cocos2d, i18n folders of web apps): assets/**/en.json ...
    ///  - Web apps (Ionic/Capacitor/Cordova/React Native bundles): string literals in assets JS/HTML.
    /// Files are always rebuilt from a backup of the original plus the saved translations
    /// (aet_texts_*.json), so saving is repeatable and an empty translation restores the original.
    /// </summary>
    public static class ExternalTexts
    {
        static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
        public const string JsonBackupDir = "aet_backup_json";
        public const string WebBackupDir = "aet_backup_web";
        const string JsonSidecar = "aet_texts_json.json";
        const string WebSidecar = "aet_texts_web.json";

        static JavaScriptSerializer Ser()
        {
            return new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 200 };
        }

        static string Rel(string projectDir, string file)
        {
            return file.Substring(projectDir.Length + 1).Replace('\\', '/');
        }

        // Reads from the backup when present: that is the untouched original.
        static string ReadOriginal(string projectDir, string backupDir, string file)
        {
            string backup = Path.Combine(projectDir, backupDir, file.Substring(projectDir.Length + 1));
            return File.ReadAllText(File.Exists(backup) ? backup : file, Encoding.UTF8);
        }

        static void WriteWithBackup(string projectDir, string backupDir, string file, string text)
        {
            string backup = Path.Combine(projectDir, backupDir, file.Substring(projectDir.Length + 1));
            if (!File.Exists(backup))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                File.Copy(file, backup);
            }
            File.WriteAllText(file, text, Utf8NoBom);
        }

        // One translation memory per language: aet_texts_json.fa.json, aet_texts_web.fa.json ...
        static string SidecarPath(string projectDir, bool web, string lang)
        {
            string name = web ? WebSidecar : JsonSidecar;
            return Path.Combine(projectDir, Path.GetFileNameWithoutExtension(name) + "." + lang + ".json");
        }

        public static Dictionary<string, string> LoadSidecar(string projectDir, bool web, string lang)
        {
            string f = SidecarPath(projectDir, web, lang);
            if (!File.Exists(f))
                f = Path.Combine(projectDir, web ? WebSidecar : JsonSidecar); // v1.64 kept one file for all languages
            var map = new Dictionary<string, string>();
            if (File.Exists(f))
                try
                {
                    foreach (var kv in Ser().Deserialize<Dictionary<string, object>>(File.ReadAllText(f, Encoding.UTF8)))
                        if (kv.Value != null) map[kv.Key] = Convert.ToString(kv.Value);
                }
                catch { }
            return map;
        }

        static void SaveSidecar(string projectDir, bool web, string lang, Dictionary<string, string> map)
        {
            File.WriteAllText(SidecarPath(projectDir, web, lang), Ser().Serialize(map), Utf8NoBom);
        }

        #region JSON language files
        static readonly Regex LangToken = new Regex(@"(?<=^|[._\-])(en)(?=$|[._\-])", RegexOptions.IgnoreCase);
        static readonly Regex AnyLangToken = new Regex(@"(?<=^|[._\-])([a-z]{2})(?:[_\-][A-Za-z]{2})?(?=$|[._\-])");

        /// <summary>English language files under assets (a folder with several language files, or an i18n/lang folder).</summary>
        public static List<string> FindJsonLanguageFiles(string projectDir)
        {
            var result = new List<string>();
            string assets = Path.Combine(projectDir, "assets");
            if (!Directory.Exists(assets))
                return result;
            foreach (var group in Directory.EnumerateFiles(assets, "*.json", SearchOption.AllDirectories).GroupBy(Path.GetDirectoryName))
            {
                var names = group.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
                string dirName = Path.GetFileName(group.Key).ToLowerInvariant();
                bool langFolder = Regex.IsMatch(dirName, "^(lang|langs|language|languages|locale|locales|i18n|l10n|translations?|strings)$");
                int langFiles = names.Count(n => AnyLangToken.IsMatch(n));
                if (!langFolder && langFiles < 2)
                    continue;
                result.AddRange(group.Where(f => LangToken.IsMatch(Path.GetFileNameWithoutExtension(f))));
            }
            return result.OrderBy(f => f).ToList();
        }

        public static List<ExtText> LoadJson(string projectDir)
        {
            var list = new List<ExtText>();
            foreach (string file in FindJsonLanguageFiles(projectDir))
            {
                object root;
                try { root = Ser().DeserializeObject(ReadOriginal(projectDir, JsonBackupDir, file)); }
                catch { continue; } // not plain JSON
                string rel = Rel(projectDir, file);
                Walk(root, "", (path, value) =>
                {
                    if (Regex.IsMatch(value, @"\p{L}"))
                        list.Add(new ExtText { Key = rel + "|" + path, Original = value, Where = rel });
                });
            }
            return list;
        }

        static void Walk(object node, string path, Action<string, string> visit)
        {
            var dict = node as IDictionary<string, object>;
            if (dict != null)
            {
                foreach (var kv in dict)
                    if (kv.Value is string) visit(path + "/" + kv.Key, (string)kv.Value);
                    else Walk(kv.Value, path + "/" + kv.Key, visit);
                return;
            }
            var arr = node as IList;
            if (arr != null && !(node is string))
                for (int i = 0; i < arr.Count; i++)
                    if (arr[i] is string) visit(path + "/" + i, (string)arr[i]);
                    else Walk(arr[i], path + "/" + i, visit);
        }

        static object Replace(object node, string path, Func<string, string, string> map)
        {
            var dict = node as IDictionary<string, object>;
            if (dict != null)
            {
                var copy = new Dictionary<string, object>();
                foreach (var kv in dict)
                    copy[kv.Key] = kv.Value is string ? map(path + "/" + kv.Key, (string)kv.Value) : Replace(kv.Value, path + "/" + kv.Key, map);
                return copy;
            }
            var arr = node as IList;
            if (arr != null && !(node is string))
            {
                var copy = new List<object>();
                for (int i = 0; i < arr.Count; i++)
                    copy.Add(arr[i] is string ? map(path + "/" + i, (string)arr[i]) : Replace(arr[i], path + "/" + i, map));
                return copy;
            }
            return node;
        }

        /// <param name="replace">true: write into the English files (shown in the app right away);
        /// false: write a new file for the language (en.json -> fa.json), used only if the app looks for it.</param>
        public static int SaveJson(string projectDir, Dictionary<string, string> translations, bool replace, string lang)
        {
            SaveSidecar(projectDir, false, lang, translations.Where(kv => kv.Value != "").ToDictionary(kv => kv.Key, kv => kv.Value));
            int files = 0;
            foreach (string file in FindJsonLanguageFiles(projectDir))
            {
                object root;
                try { root = Ser().DeserializeObject(ReadOriginal(projectDir, JsonBackupDir, file)); }
                catch { continue; }
                string rel = Rel(projectDir, file);
                object updated = Replace(root, "", (path, value) =>
                {
                    string tr;
                    return translations.TryGetValue(rel + "|" + path, out tr) && tr != "" ? tr : value;
                });
                string json = PrettyJson(Ser().Serialize(updated));
                if (replace)
                {
                    WriteWithBackup(projectDir, JsonBackupDir, file, json);
                }
                else
                {
                    // the English file stays original
                    string backup = Path.Combine(projectDir, JsonBackupDir, file.Substring(projectDir.Length + 1));
                    if (File.Exists(backup)) { File.Copy(backup, file, true); }
                    string name = NewLanguageFileName(Path.GetFileName(file), lang);
                    File.WriteAllText(Path.Combine(Path.GetDirectoryName(file), name), json, Utf8NoBom);
                }
                files++;
            }
            return files;
        }

        /// <summary>en.json -> fa.json, com.en.json -> com.pt-BR.json (Android "pt-rBR" -> "pt-BR").
        /// Throws when the result would overwrite the source file.</summary>
        public static string NewLanguageFileName(string sourceFileName, string lang)
        {
            string code = Regex.Replace(lang, "-r([A-Za-z]{2})$", "-$1");
            string name = LangToken.Replace(sourceFileName, code, 1);
            if (name.Equals(sourceFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The new language \"" + lang + "\" would overwrite the source file " + sourceFileName +
                    ". Choose another language, or \"replace the app's own texts\".");
            return name;
        }

        // JavaScriptSerializer writes everything on one line and escapes non-ASCII; make it readable.
        static string PrettyJson(string json)
        {
            json = Regex.Replace(json, @"\\u([0-9a-fA-F]{4})", m =>
            {
                char c = (char)Convert.ToInt32(m.Groups[1].Value, 16);
                return c < 0x20 || c == '"' || c == '\\' ? m.Value : c.ToString();
            });
            var sb = new StringBuilder();
            int indent = 0;
            bool inString = false;
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (inString)
                {
                    sb.Append(c);
                    if (c == '\\') { sb.Append(json[++i]); }
                    else if (c == '"') inString = false;
                    continue;
                }
                switch (c)
                {
                    case '"': inString = true; sb.Append(c); break;
                    case '{': case '[': sb.Append(c).Append('\n').Append(' ', ++indent * 2); break;
                    case '}': case ']': sb.Append('\n').Append(' ', --indent * 2).Append(c); break;
                    case ',': sb.Append(c).Append('\n').Append(' ', indent * 2); break;
                    case ':': sb.Append(": "); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }
        #endregion

        #region Web apps (JS / HTML)
        static readonly string[] WebRoots = { "assets/public", "assets/www", "assets/flutter_assets/web", "assets" };
        static readonly Regex JsString = new Regex(@"""(?:[^""\\\r\n]|\\.)*""|'(?:[^'\\\r\n]|\\.)*'|`(?:[^`\\$]|\\.|\$(?!\{))*`");
        static readonly Regex HtmlText = new Regex(@"(>)([^<>]+)(<)");
        static readonly Regex HtmlAttr = new Regex(@"(\s(?:placeholder|title|alt|aria-label)="")([^""]+)("")", RegexOptions.IgnoreCase);
        static readonly Regex ScriptStyle = new Regex(@"<(script|style)\b.*?</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Words that are code even though they look like UI text (JS globals, HTTP headers, events ...).
        static readonly HashSet<string> TechWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Object","Array","String","Number","Boolean","Symbol","Function","Promise","Error","TypeError","RangeError","SyntaxError",
            "ReferenceError","Date","Math","JSON","Intl","Reflect","Proxy","Map","Set","WeakMap","WeakSet","RegExp","Infinity","NaN",
            "Arguments","Generator","Window","Document","Element","Node","Event","Blob","File","Image","Audio","Worker","Buffer",
            "Authorization","Accept","Content","Cookie","Origin","Referer","Location","Host","Range","Expires","Pragma","Vary",
            "GET","POST","PUT","DELETE","PATCH","HEAD","OPTIONS","Bearer","Basic","Android","Capacitor","Cordova","Ionic","Vue",
            "React","Chrome","Safari","Firefox","Edge","Opera","Windows","Linux","Macintosh","iPhone","iPad","Mobile","Default",
            "Enter","Escape","Tab","Backspace","Delete","ArrowUp","ArrowDown","ArrowLeft","ArrowRight","Shift","Control","Alt","Meta"
        };

        // Libraries ship their own (non-UI) strings: syntax highlighters, polyfills, service workers ...
        static readonly Regex VendorFile = new Regex(@"^(sw|service-?worker|workbox|registersw)\b|polyfill|vendor|highlight|prism|katex|mermaid|mathjax|codemirror|monaco|lodash|moment|chart|swiper|stencil|^ionic[-.]|capacitor|core-js|zone\.js|hammer", RegexOptions.IgnoreCase);

        public static bool IsWebApp(string projectDir)
        {
            return WebFiles(projectDir).Any();
        }

        static IEnumerable<string> WebFiles(string projectDir)
        {
            foreach (string root in WebRoots)
            {
                string dir = Path.Combine(projectDir, root.Replace('/', '\\'));
                if (!Directory.Exists(dir))
                    continue;
                var files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
                    .Where(f =>
                    {
                        string ext = Path.GetExtension(f).ToLowerInvariant();
                        string name = Path.GetFileName(f).ToLowerInvariant();
                        if (ext != ".js" && ext != ".html" && ext != ".htm" && ext != ".bundle")
                            return false;
                        if (name.StartsWith("cordova") || name == "native-bridge.js" || name.EndsWith(".map") || VendorFile.IsMatch(name))
                            return false;
                        return new FileInfo(f).Length < 20 * 1024 * 1024;
                    }).ToList();
                if (files.Count == 0)
                    continue;
                foreach (string f in files)
                    yield return f;
                yield break; // first root that has web files
            }
        }

        public static bool IsUiText(string s, bool singleWords)
        {
            s = s.Trim();
            if (s.Length < 2 || s.Length > 500 || !Regex.IsMatch(s, @"\p{L}"))
                return false;
            if (s.Contains("://") || s.Contains("=>") || s.Contains("&&") || s.Contains("||") || Regex.IsMatch(s, @"[;{}<>=\\|]"))
                return false;
            if (s == "use strict" || s == "use client")
                return false;
            // code cut out by a mis-paired quote: ")) return x(" etc.
            int open = s.IndexOf('('), close = s.IndexOf(')');
            if (s.Count(c => c == '(') != s.Count(c => c == ')') || (close >= 0 && (open < 0 || close < open)))
                return false;
            // all-lowercase Latin word lists without punctuation are keywords/identifiers, not sentences
            if (!Regex.IsMatch(s, @"\p{Lu}") && Regex.IsMatch(s, @"^[\x20-\x7E]+$") && !Regex.IsMatch(s, @"[.!?']"))
                return false;
            // type tags, CSS selectors, bracketed tokens
            if (s.StartsWith("[object ") || s.Contains("[") || s.Contains("]"))
                return false;
            // date/time format patterns: "ddd MMM DD YYYY HH:mm:ss", "EEEE do MMMM y"
            if (s.Split(' ').All(t => t == "" || Regex.IsMatch(t, @"^[,\-/.:]+$|^[,\-/.:]*(j?[dDMYyHhmsSEeaAZzoQqWwGgxX]{1,5}|do|Do)[,\-/.:]*$")))
                return false;
            if (Regex.IsMatch(s, @"^[.#/@$_\-]|\.(js|css|png|jpe?g|svg|json|html?|webp|gif|mp3|ttf|woff2?)$", RegexOptions.IgnoreCase))
                return false;
            if (Regex.IsMatch(s, @"\b\d+(px|rem|em|vh|vw|ms)\b|rgba?\(|calc\(|var\(--|translate\(|cubic-bezier", RegexOptions.IgnoreCase))
                return false;
            if (Regex.IsMatch(s, @"^[YMDdHhmsaAZz:\-/ .,]+$")) // date formats
                return false;
            if (!s.Contains(" "))
            {
                if (!singleWords || TechWords.Contains(s.TrimEnd('.', '!', '?', ':')))
                    return false;
                return Regex.IsMatch(s, @"^\p{Lu}\p{Ll}+(['’]\p{Ll}+)?[.!?:]?$"); // "Cancel", "Settings"
            }
            // looks like code joined by spaces? (e.g. "ion-page can-go-back", "flex row")
            if (Regex.IsMatch(s, @"^[a-z0-9\-_:.]+( [a-z0-9\-_:.]+)*$") && Regex.IsMatch(s, @"[-_:]"))
                return false;
            int letters = s.Count(char.IsLetter);
            return letters * 2 >= s.Length;
        }

        // Methods whose string arguments are names/keys, not text shown to the user.
        static readonly Regex KeyApiBefore = new Regex(
            @"(\.(includes|indexOf|lastIndexOf|startsWith|endsWith|getItem|setItem|removeItem|getAttribute|setAttribute|hasAttribute|" +
            @"removeAttribute|emit|\$emit|on|off|once|addEventListener|removeEventListener|querySelector|querySelectorAll|" +
            @"getElementById|getElementsByClassName|createElement|commit|dispatch|get|set|has|delete|hasOwnProperty|" +
            @"matchMedia|postMessage|getPlugin|registerPlugin|addListener|send|call|invoke)|\brequire|\bimport|\bdefine)\s*\(\s*$");

        /// <summary>
        /// True when the literal at [start, end) is part of the program's logic rather than shown text:
        /// an object key ({"x": or ,"x":), a property name (o["x"]), a comparison (== "x", "x" ===),
        /// a case label, the left side of "in", or an argument of a key-taking API (getItem("x") ...).
        /// Such literals are neither listed nor replaced, so translating a text never changes a key
        /// that happens to have the same words.
        /// </summary>
        public static bool IsCodeUse(string content, int start, int end)
        {
            int p = start - 1;
            while (p >= 0 && char.IsWhiteSpace(content[p])) p--;
            int n = end;
            while (n < content.Length && char.IsWhiteSpace(content[n])) n++;
            char prev = p >= 0 ? content[p] : '\0';
            char next = n < content.Length ? content[n] : '\0';

            if (next == ':' && (prev == '{' || prev == ','))
                return true; // object key
            if (prev == '[' && next == ']')
                return true; // o["key"]
            if (next == '=' && n + 1 < content.Length && content[n + 1] == '=')
                return true; // "x" == / ===
            if (next == '!' && n + 1 < content.Length && content[n + 1] == '=')
                return true; // "x" != / !==
            if (prev == '=' && p >= 1 && (content[p - 1] == '=' || content[p - 1] == '!'))
                return true; // == "x", != "x", === "x"
            string before = content.Substring(Math.Max(0, p - 60), p + 1 - Math.Max(0, p - 60));
            if (Regex.IsMatch(before, @"\bcase$"))
                return true; // case "x":
            if (n + 2 < content.Length && content[n] == 'i' && content[n + 1] == 'n' && !char.IsLetterOrDigit(content[n + 2]) && content[n + 2] != '_')
                return true; // "x" in obj
            if (KeyApiBefore.IsMatch(content.Substring(Math.Max(0, start - 60), start - Math.Max(0, start - 60))))
                return true; // getItem("x"), $emit("x") ...
            return false;
        }

        static string UnescapeJs(string body)
        {
            return Regex.Replace(body, @"\\(u\{?[0-9a-fA-F]{4,5}\}?|x[0-9a-fA-F]{2}|.)", m =>
            {
                string e = m.Groups[1].Value;
                switch (e[0])
                {
                    case 'n': return e.Length == 1 ? "\n" : m.Value;
                    case 't': return e.Length == 1 ? "\t" : m.Value;
                    case 'r': return e.Length == 1 ? "" : m.Value;
                    case 'u':
                        int code = Convert.ToInt32(e.Trim('u', '{', '}'), 16);
                        return code >= 0xD800 && code <= 0xDFFF ? ((char)code).ToString() : char.ConvertFromUtf32(code);
                    case 'x': return ((char)Convert.ToInt32(e.Substring(1), 16)).ToString();
                    default: return e;
                }
            });
        }

        static string EscapeJs(string text, char quote)
        {
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (c == '\\') sb.Append("\\\\");
                else if (c == quote) sb.Append('\\').Append(c);
                else if (c == '\n') sb.Append(quote == '`' ? "\n" : "\\n");
                else if (c == '\r') { }
                else if (c == '$' && quote == '`') sb.Append("\\$");
                else sb.Append(c);
            }
            return sb.ToString();
        }

        // Calls visit(text) for every UI text in a web file; with map != null returns the file with translations applied.
        static string ScanWeb(string file, string content, bool singleWords, Action<string> visit, Dictionary<string, string> map)
        {
            if (content.IndexOf('\0') >= 0)
                return content; // binary (e.g. Hermes bytecode bundle): never touch
            string ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext == ".html" || ext == ".htm")
            {
                // keep <script>/<style> untouched: mask them, scan, unmask
                var blocks = new List<string>();
                string masked = ScriptStyle.Replace(content, m => { blocks.Add(m.Value); return "\u0001" + (blocks.Count - 1) + "\u0002"; });
                MatchEvaluator text = m =>
                {
                    string raw = m.Groups[2].Value, t = System.Net.WebUtility.HtmlDecode(raw).Trim();
                    if (!IsUiText(t, true)) return m.Value;
                    if (visit != null) visit(t);
                    string tr;
                    if (map != null && map.TryGetValue(t, out tr) && tr != "")
                        return m.Groups[1].Value + raw.Replace(raw.Trim(), System.Net.WebUtility.HtmlEncode(tr)) + m.Groups[3].Value;
                    return m.Value;
                };
                masked = HtmlText.Replace(masked, text);
                masked = HtmlAttr.Replace(masked, text);
                return Regex.Replace(masked, "\u0001(\\d+)\u0002", m => blocks[int.Parse(m.Groups[1].Value)]);
            }
            return JsString.Replace(content, m =>
            {
                char quote = m.Value[0];
                string t = UnescapeJs(m.Value.Substring(1, m.Value.Length - 2));
                if (!IsUiText(t, singleWords)) return m.Value;
                if (IsCodeUse(content, m.Index, m.Index + m.Length)) return m.Value;
                if (visit != null) visit(t);
                string tr;
                if (map != null && map.TryGetValue(t, out tr) && tr != "")
                    return quote + EscapeJs(tr, quote) + quote;
                return m.Value;
            });
        }

        public static List<ExtText> LoadWeb(string projectDir, bool singleWords)
        {
            var where = new Dictionary<string, List<string>>();
            var order = new List<string>();
            foreach (string file in WebFiles(projectDir))
            {
                string rel = Rel(projectDir, file);
                ScanWeb(file, ReadOriginal(projectDir, WebBackupDir, file), singleWords, t =>
                {
                    List<string> l;
                    if (!where.TryGetValue(t, out l)) { where[t] = l = new List<string>(); order.Add(t); }
                    string n = Path.GetFileName(rel);
                    if (!l.Contains(n)) l.Add(n);
                }, null);
            }
            return order.Select(t => new ExtText
            {
                Key = t,
                Original = t,
                Where = where[t][0] + (where[t].Count > 1 ? " (+" + (where[t].Count - 1) + ")" : "")
            }).ToList();
        }

        /// <summary>Rebuilds every web file from its original with the translations (text -> translation).</summary>
        public static int SaveWeb(string projectDir, Dictionary<string, string> translations, bool singleWords, string lang, bool rtl)
        {
            string rtlLang = rtl ? lang : null;
            SaveSidecar(projectDir, true, lang, translations.Where(kv => kv.Value != "").ToDictionary(kv => kv.Key, kv => kv.Value));
            int changed = 0;
            foreach (string file in WebFiles(projectDir))
            {
                string original = ReadOriginal(projectDir, WebBackupDir, file);
                string updated = ScanWeb(file, original, singleWords, null, translations);
                string name = Path.GetFileName(file).ToLowerInvariant();
                if (name == "index.html" && rtlLang != null)
                    updated = Regex.Replace(updated, @"<html\b[^>]*>", m =>
                    {
                        string tag = Regex.Replace(m.Value, @"\s(dir|lang)=""[^""]*""", "");
                        return tag.Insert(5, " dir=\"rtl\" lang=\"" + rtlLang + "\"");
                    }, RegexOptions.IgnoreCase);
                string current = File.ReadAllText(file, Encoding.UTF8);
                if (updated != current)
                {
                    WriteWithBackup(projectDir, WebBackupDir, file, updated);
                    changed++;
                }
            }
            return changed;
        }
        #endregion
    }
}
