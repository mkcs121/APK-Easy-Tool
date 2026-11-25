using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace APKEasyTool
{
    /// <summary>
    /// Translate the strings of a decompiled APK (res/values/strings.xml) into another
    /// language (res/values-xx/strings.xml) and rename the app.
    /// </summary>
    public partial class TranslateForm : Form
    {
        // Copy of the untouched default strings, written before we ever modify res/values/strings.xml.
        // Apktool only packs known folders/files, so a file in the project root is ignored on build.
        const string BackupFileName = "aet_strings_original.xml";

        static readonly string[] RtlLanguages = { "fa", "ar", "ur", "he", "iw", "ckb", "ku", "ps", "sd", "yi" };
        static readonly Regex FormatSpec = new Regex(@"%(\d+\$)?[-#+ 0,(]*\d*(\.\d+)?[sdfxXoeEgGcbBhHaAn%]");

        readonly TextBox folderBox = new TextBox();
        readonly ComboBox langBox = new ComboBox();
        readonly CheckBox applyDefaultChk = new CheckBox();
        readonly TextBox appNameBox = new TextBox();
        readonly Label appNameInfo = new Label();
        readonly Label langInfo = new Label();
        static readonly string[] CommonLanguages = { "fa", "ar", "tr", "ur", "ku", "ckb", "en", "ru", "de", "fr", "es", "it", "pt", "zh", "ja", "ko", "hi", "id" };
        readonly TextBox searchBox = new TextBox();
        readonly CheckBox untranslatedChk = new CheckBox();
        readonly Label countLbl = new Label();
        readonly DataGridView grid = new DataGridView();
        readonly Button saveBtn = new Button();

        DataTable table;
        string projectDir;
        string loadedLang;
        string originalAppName = "";
        // Labels shown under the launcher icon: either "@string/name" references or literal manifest values.
        readonly List<string> appLabelStringNames = new List<string>();
        bool appLabelIsLiteral;
        bool dirty;

        public TranslateForm(string decompiledDir, string aiSettingsFile)
        {
            settingsFile = aiSettingsFile;
            Text = "Edit app - " + Program.Name;
            Font = SystemFonts.MessageBoxFont;
            // Fixed pixel sizes were too small on high-DPI screens: use most of the screen instead.
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            Size = new Size(wa.Width * 85 / 100, wa.Height * 85 / 100);
            MinimumSize = new Size(S(720), S(480));
            StartPosition = FormStartPosition.CenterScreen;
            ShowIcon = false;
            BuildUi();

            folderBox.Text = decompiledDir ?? "";
            langBox.Text = "fa";
            Shown += (s, e) => { if (Directory.Exists(folderBox.Text)) LoadProject(); };
            FormClosing += TranslateForm_FormClosing;
        }

        #region UI
        // Scale a 96-dpi pixel size to the screen's DPI.
        int S(int px)
        {
            return (int)(px * DeviceDpi / 96f);
        }

        void BuildUi()
        {
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(8) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            // Row 1: folder + language
            var row1 = NewRow(6);
            row1.ColumnStyles[1] = new ColumnStyle(SizeType.Percent, 100);
            folderBox.ReadOnly = true;
            folderBox.Dock = DockStyle.Fill;
            var browseBtn = new Button { Text = "Browse...", AutoSize = true };
            browseBtn.Click += BrowseBtn_Click;
            langBox.DropDownStyle = ComboBoxStyle.DropDown;
            langBox.Width = S(90);
            langBox.Items.AddRange(CommonLanguages);
            var loadBtn = new Button { Text = "Load", AutoSize = true };
            loadBtn.Click += (s, e) => LoadProject();
            langBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadProject(); } };
            langBox.SelectionChangeCommitted += (s, e) => { if (projectDir != null) BeginInvoke(new Action(LoadProject)); };
            row1.Controls.Add(Lbl("Decompiled folder:"), 0, 0);
            row1.Controls.Add(folderBox, 1, 0);
            row1.Controls.Add(browseBtn, 2, 0);
            row1.Controls.Add(Lbl("Language code:"), 3, 0);
            row1.Controls.Add(langBox, 4, 0);
            row1.Controls.Add(loadBtn, 5, 0);
            root.Controls.Add(row1, 0, 0);

            // Row 2: app name
            var row2 = NewRow(4);
            row2.ColumnStyles[1] = new ColumnStyle(SizeType.Absolute, S(320));
            appNameBox.Dock = DockStyle.Fill;
            appNameBox.TextChanged += (s, e) => dirty = true;
            appNameInfo.AutoSize = true;
            appNameInfo.ForeColor = SystemColors.GrayText;
            appNameInfo.Anchor = AnchorStyles.Left;
            row2.Controls.Add(Lbl("App name:"), 0, 0);
            row2.Controls.Add(appNameBox, 1, 0);
            row2.Controls.Add(appNameInfo, 2, 0);
            langInfo.AutoSize = true;
            langInfo.ForeColor = SystemColors.GrayText;
            langInfo.Anchor = AnchorStyles.Left;
            langInfo.Padding = new Padding(S(24), 0, 0, 0);
            row2.Controls.Add(langInfo, 3, 0);
            root.Controls.Add(row2, 0, 1);

            // Row 3: search + filters + AI helpers
            // Flow panel so the toolbar wraps onto a second line on narrow windows instead of being cut off.
            var row3 = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = new Padding(0, 0, 0, 4) };
            searchBox.Width = S(220);
            searchBox.TextChanged += (s, e) => ApplyFilter();
            untranslatedChk.Text = "Untranslated only";
            untranslatedChk.AutoSize = true;
            untranslatedChk.Anchor = AnchorStyles.Left;
            untranslatedChk.CheckedChanged += (s, e) => ApplyFilter();
            countLbl.AutoSize = true;
            countLbl.Anchor = AnchorStyles.Left;
            var copyAiBtn = new Button { Text = "Copy for AI", AutoSize = true };
            copyAiBtn.Click += CopyForAi_Click;
            var pasteAiBtn = new Button { Text = "Paste from AI", AutoSize = true };
            pasteAiBtn.Click += PasteFromAi_Click;
            var tip = new ToolTip();
            tip.SetToolTip(copyAiBtn, "Copies the untranslated strings with instructions.\nPaste them into ChatGPT (or any AI), then copy its answer and press \"Paste from AI\".");
            tip.SetToolTip(pasteAiBtn, "Reads lines like  name: translated text  from the clipboard.");
            // applyDefaultChk is not shown: the two mode radio buttons drive it.
            applyDefaultChk.CheckedChanged += (s, e) => { dirty = true; SyncModeRadios(); };
            row3.Controls.Add(Lbl("Search:"));
            row3.Controls.Add(searchBox);
            row3.Controls.Add(untranslatedChk);
            row3.Controls.Add(countLbl);
            row3.Controls.Add(copyAiBtn);
            row3.Controls.Add(pasteAiBtn);
            AddAiControls(row3, tip);

            // Grid
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AutoGenerateColumns = false;
            grid.RowHeadersVisible = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", DataPropertyName = "Name", HeaderText = "Name", ReadOnly = true, FillWeight = 22 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Original", DataPropertyName = "Original", HeaderText = "Original", ReadOnly = true, FillWeight = 39 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Translation", DataPropertyName = "Translation", HeaderText = "Translation", FillWeight = 39 });
            grid.Columns["Name"].DefaultCellStyle.ForeColor = SystemColors.GrayText;
            grid.Columns["Original"].DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.CellFormatting += Grid_CellFormatting;
            grid.CellValueChanged += (s, e) => { dirty = true; UpdateCount(); };
            grid.DataError += (s, e) => { e.ThrowException = false; };

            // Tabs: texts | icon & package
            var textsPage = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            textsPage.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            textsPage.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            textsPage.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            textsPage.Controls.Add(row3, 0, 0);
            textsPage.Controls.Add(BuildModeRow(tip), 0, 1);
            textsPage.Controls.Add(grid, 0, 2);
            var tabs = new TabControl { Dock = DockStyle.Fill };
            var p1 = new TabPage("Texts / translation") { Padding = new Padding(6) };
            p1.Controls.Add(textsPage);
            var p2 = new TabPage("Icon and package name") { Padding = new Padding(6), AutoScroll = true };
            p2.Controls.Add(BuildIconPackagePage(tip));
            tabs.TabPages.Add(p1);
            tabs.TabPages.Add(p2);
            var p3 = new TabPage("Remove ads") { Padding = new Padding(6) };
            p3.Controls.Add(BuildAdsPage());
            tabs.TabPages.Add(p3);
            var p4 = new TabPage("Fonts") { Padding = new Padding(6) };
            p4.Controls.Add(BuildFontsPage());
            tabs.TabPages.Add(p4);
            root.Controls.Add(tabs, 0, 2);

            // Bottom buttons
            var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
            var closeBtn = new Button { Text = "Close", AutoSize = true };
            closeBtn.Click += (s, e) => Close();
            saveBtn.Text = "Save";
            saveBtn.AutoSize = true;
            saveBtn.Click += (s, e) => Save();
            var hint = new Label { AutoSize = true, ForeColor = SystemColors.GrayText, Padding = new Padding(0, 6, 12, 0),
                Text = "After saving, press Compile in the main window to build the translated APK." };
            bottom.Controls.Add(closeBtn);
            bottom.Controls.Add(saveBtn);
            bottom.Controls.Add(hint);
            root.Controls.Add(bottom, 0, 3);

            AcceptButton = null;
            KeyPreview = true;
            KeyDown += (s, e) => { if (e.Control && e.KeyCode == Keys.S) { e.SuppressKeyPress = true; Save(); } };
        }

        static TableLayoutPanel NewRow(int columns)
        {
            var p = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = columns, RowCount = 1, Margin = new Padding(0, 0, 0, 4) };
            for (int i = 0; i < columns; i++)
                p.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            return p;
        }

        static Label Lbl(string text)
        {
            return new Label { Text = text, AutoSize = true, Anchor = AnchorStyles.Left };
        }
        #endregion

        #region Load
        void BrowseBtn_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select a decompiled APK folder (the one containing AndroidManifest.xml)";
                if (Directory.Exists(folderBox.Text))
                    fbd.SelectedPath = folderBox.Text;
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    folderBox.Text = fbd.SelectedPath;
                    LoadProject();
                }
            }
        }

        string LangCode
        {
            get { return (langBox.Text ?? "").Trim(); }
        }

        string DefaultStringsPath { get { return Path.Combine(projectDir, "res", "values", "strings.xml"); } }
        string BackupPath { get { return Path.Combine(projectDir, BackupFileName); } }
        string TargetStringsPath { get { return Path.Combine(projectDir, "res", "values-" + loadedLang, "strings.xml"); } }

        void LoadProject()
        {
            if (!ConfirmDiscard())
                return;

            string dir = folderBox.Text;
            if (!File.Exists(Path.Combine(dir, "AndroidManifest.xml")) || !File.Exists(Path.Combine(dir, "res", "values", "strings.xml")))
            {
                MessageBox.Show(this, "This folder doesn't look like a decompiled APK.\nDecompile the APK first, then open its folder here.\n\nExpected: AndroidManifest.xml and res\\values\\strings.xml",
                    Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!Regex.IsMatch(LangCode,@"^[a-zA-Z]{2,3}(-r[a-zA-Z]{2})?$|^b\+[a-zA-Z0-9+]+$"))
            {
                MessageBox.Show(this, "Enter a language code like fa, ar, tr or pt-rBR.", Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            projectDir = dir;
            loadedLang = LangCode;

            try
            {
                // Originals: prefer our backup, so text already overwritten by "every device" stays visible.
                string originalsFile = File.Exists(BackupPath) ? BackupPath : DefaultStringsPath;
                var originals = ReadStrings(originalsFile, true);
                var existing = File.Exists(TargetStringsPath) ? ReadStrings(TargetStringsPath, false) : new Dictionary<string, string>();

                literalTagNames.Clear();
                foreach (var kv in originals)
                    if (kv.Value.Contains("&lt;"))
                        literalTagNames.Add(kv.Key);

                table = new DataTable();
                table.Columns.Add("Name", typeof(string));
                table.Columns.Add("Original", typeof(string));
                table.Columns.Add("Translation", typeof(string));
                foreach (var kv in originals)
                {
                    string tr;
                    existing.TryGetValue(kv.Key, out tr);
                    table.Rows.Add(kv.Key, ToDisplay(kv.Value), tr == null ? "" : ToDisplay(tr));
                }
                table.AcceptChanges();
                grid.DataSource = table;

                bool rtl = RtlLanguages.Contains(loadedLang.Split('-')[0].ToLowerInvariant());
                grid.Columns["Translation"].DefaultCellStyle.Alignment = rtl ? DataGridViewContentAlignment.MiddleRight : DataGridViewContentAlignment.MiddleLeft;
                appNameBox.RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No;
                applyDefaultChk.Checked = File.Exists(BackupPath);

                LoadAppName(originals, existing);
                ShowAppLanguages();
                LoadIconPackage();
                ApplyFilter();
                dirty = false;
                Text = "Edit app (" + loadedLang + ") - " + Path.GetFileName(projectDir.TrimEnd('\\', '/')) + " - " + Program.Name;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not read the strings:\n" + ex.Message, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Languages the app already has (res/values-xx/strings.xml) go first in the list and are shown next to it.
        void ShowAppLanguages()
        {
            var langs = Directory.GetDirectories(Path.Combine(projectDir, "res"), "values-*")
                .Where(d => File.Exists(Path.Combine(d, "strings.xml")))
                .Select(d => Path.GetFileName(d).Substring(7))
                .Where(q => Regex.IsMatch(q, @"^([a-z]{2,3}(-r[A-Z]{2})?|b\+[A-Za-z0-9+]+)$"))
                .Distinct().OrderBy(q => q).ToList();
            string current = langBox.Text;
            langBox.Items.Clear();
            langBox.Items.AddRange(langs.Cast<object>().ToArray());
            langBox.Items.AddRange(CommonLanguages.Where(c => !langs.Contains(c)).Cast<object>().ToArray());
            langBox.Text = current;
            langInfo.Text = "Languages in the app: default (res/values, the \"Original\" column)" +
                (langs.Count > 0 ? " + " + String.Join(", ", langs.Take(25)) + (langs.Count > 25 ? " ..." : "") : " only");
        }

        // name -> raw inner XML of <string>. Skips translatable="false" when reading originals.
        static Dictionary<string, string> ReadStrings(string file, bool originals)
        {
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(file);
            var result = new Dictionary<string, string>();
            foreach (XmlElement el in doc.DocumentElement.SelectNodes("string"))
            {
                string name = el.GetAttribute("name");
                if (String.IsNullOrEmpty(name) || result.ContainsKey(name))
                    continue;
                if (originals && el.GetAttribute("translatable") == "false")
                    continue;
                if (originals && (el.InnerXml.Trim().Length == 0 || IsTechnical(el.InnerText.Trim())))
                    continue;
                result[name] = el.InnerXml;
            }
            return result;
        }

        // Strings the user never sees and must not translate: class names, keys, URLs, IDs.
        static readonly Regex TechnicalValue = new Regex(
            @"^([a-zA-Z_][\w$]*(\.[\w$]+){2,}" +   // com.google.android.material.behavior.Foo
            @"|[a-z][a-z0-9+.-]*://\S*" +           // https://..., market://...
            @"|[A-Za-z0-9+/=_\-]{32,}" +            // base64 keys, tokens, long ids
            @"|[0-9a-fA-F:\-]{8,}" +                // hex ids, uuids
            @"|[^\p{L}]*)$");                       // no letters at all: %1$s • %2$s, numbers, symbols

        static bool IsTechnical(string value)
        {
            return TechnicalValue.IsMatch(value);
        }

        void LoadAppName(Dictionary<string, string> originals, Dictionary<string, string> existing)
        {
            appLabelStringNames.Clear();
            appLabelIsLiteral = false;
            originalAppName = "";

            foreach (string label in LauncherLabels(File.ReadAllText(Path.Combine(projectDir, "AndroidManifest.xml"), Encoding.UTF8)))
            {
                if (label.StartsWith("@string/"))
                {
                    string name = label.Substring(8);
                    if (!appLabelStringNames.Contains(name))
                        appLabelStringNames.Add(name);
                }
                else if (!label.StartsWith("@"))
                {
                    appLabelIsLiteral = true;
                    if (originalAppName == "")
                        originalAppName = XmlDecode(label);
                }
            }

            string current = null;
            foreach (string name in appLabelStringNames)
            {
                string v;
                if (current == null && existing.TryGetValue(name, out v) && v.Trim() != "")
                    current = ToDisplay(v);
                if (originalAppName == "" && originals.TryGetValue(name, out v))
                    originalAppName = ToDisplay(v);
            }
            if (current == null && appLabelIsLiteral)
                current = originalAppName;

            appNameBox.Text = current ?? "";
            appNameInfo.Text = originalAppName == "" ? "(app name not found in the manifest)" : "Original: " + originalAppName;
            appNameBox.Enabled = appLabelIsLiteral || appLabelStringNames.Count > 0;
        }

        // android:label of <application> and of launcher activities / aliases (the one shown under the icon wins).
        static List<string> LauncherLabels(string manifest)
        {
            var labels = new List<string>();
            Match app = Regex.Match(manifest, @"<application\b[^>]*>", RegexOptions.Singleline);
            if (app.Success)
            {
                Match l = Regex.Match(app.Value, @"android:label=""([^""]*)""");
                if (l.Success) labels.Add(l.Groups[1].Value);
            }
            foreach (Match act in Regex.Matches(manifest, @"<(activity-alias|activity)\b[^>]*?(/>|>(.*?)</\1>)", RegexOptions.Singleline))
            {
                string body = act.Value;
                if (!body.Contains("android.intent.category.LAUNCHER"))
                    continue;
                Match open = Regex.Match(body, @"^<[^>]*>", RegexOptions.Singleline);
                Match l = Regex.Match(open.Value, @"android:label=""([^""]*)""");
                if (l.Success) labels.Add(l.Groups[1].Value);
            }
            return labels;
        }
        #endregion

        #region Grid helpers
        void ApplyFilter()
        {
            if (table == null)
                return;
            var parts = new List<string>();
            string q = searchBox.Text.Trim();
            if (q != "")
            {
                string like = EscapeLike(q);
                parts.Add("(Name LIKE '%" + like + "%' OR Original LIKE '%" + like + "%' OR Translation LIKE '%" + like + "%')");
            }
            if (untranslatedChk.Checked)
                parts.Add("(Translation IS NULL OR Translation = '')");
            grid.EndEdit();
            table.DefaultView.RowFilter = String.Join(" AND ", parts);
            UpdateCount();
        }

        static string EscapeLike(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s)
            {
                if (c == '\'') sb.Append("''");
                else if (c == '[' || c == ']' || c == '*' || c == '%') sb.Append('[').Append(c).Append(']');
                else sb.Append(c);
            }
            return sb.ToString();
        }

        void UpdateCount()
        {
            if (table == null)
            {
                countLbl.Text = "";
                return;
            }
            int done = table.Rows.Cast<DataRow>().Count(r => r["Translation"] is string && ((string)r["Translation"]).Trim() != "");
            countLbl.Text = done + " / " + table.Rows.Count + " translated";
        }

        void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || grid.Columns[e.ColumnIndex].Name != "Translation")
                return;
            var row = grid.Rows[e.RowIndex];
            string tr = row.Cells["Translation"].Value as string;
            string orig = row.Cells["Original"].Value as string;
            if (!String.IsNullOrEmpty(tr) && !SamePlaceholders(orig, tr))
            {
                e.CellStyle.BackColor = Color.FromArgb(255, 225, 225);
                SetTip(row.Cells["Translation"], "Placeholders (%s, %d, %1$s ...) differ from the original. The app may crash when it shows this text.");
            }
            else
            {
                SetTip(row.Cells["Translation"], "");
            }
        }

        static void SetTip(DataGridViewCell cell, string tip)
        {
            if (cell.ToolTipText != tip)
                cell.ToolTipText = tip;
        }

        static bool SamePlaceholders(string a, string b)
        {
            Func<string, string> key = s => String.Join(",", FormatSpec.Matches(s ?? "").Cast<Match>().Select(m => m.Value).OrderBy(x => x, StringComparer.Ordinal));
            return key(a) == key(b);
        }
        #endregion

        #region AI copy / paste
        void CopyForAi_Click(object sender, EventArgs e)
        {
            if (table == null)
                return;
            grid.EndEdit();
            var rows = table.Rows.Cast<DataRow>().Where(r => !(r["Translation"] is string) || ((string)r["Translation"]).Trim() == "").ToList();
            if (rows.Count == 0)
            {
                MessageBox.Show(this, "Everything is already translated.", Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var sb = new StringBuilder();
            sb.AppendLine("Translate the text after the colon on each line into the language with code \"" + loadedLang + "\" for an Android app.");
            sb.AppendLine("Rules: keep the part before the colon exactly as it is; keep placeholders like %s %d %1$s, \\n, and HTML tags unchanged;");
            sb.AppendLine("reply with only the translated lines in the same format, one per line, no explanations.");
            sb.AppendLine();
            foreach (var r in rows)
                sb.Append((string)r["Name"]).Append(": ").AppendLine(OneLine((string)r["Original"]));
            Clipboard.SetText(sb.ToString());
            MessageBox.Show(this, rows.Count + " untranslated strings were copied.\n\nPaste them into ChatGPT (or another AI), copy its whole answer, then press \"Paste from AI\".\nFor very long lists, translate with \"Untranslated only\" in a few rounds.",
                Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void PasteFromAi_Click(object sender, EventArgs e)
        {
            if (table == null)
                return;
            string text = Clipboard.ContainsText() ? Clipboard.GetText() : "";
            var byName = table.Rows.Cast<DataRow>().ToDictionary(r => (string)r["Name"]);
            int applied = 0;
            foreach (string rawLine in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                Match m = Regex.Match(rawLine.Trim().Trim('`', '*', '-', ' '), @"^([A-Za-z0-9_.]+)\s*[:：]\s?(.*)$");
                if (!m.Success)
                    continue;
                DataRow row;
                if (!byName.TryGetValue(m.Groups[1].Value, out row))
                    continue;
                string value = m.Groups[2].Value.Trim();
                if (value == "")
                    continue;
                row["Translation"] = value;
                applied++;
            }
            if (applied > 0)
                dirty = true;
            UpdateCount();
            grid.Invalidate();
            MessageBox.Show(this, applied == 0
                ? "No translated lines were found in the clipboard.\nCopy the AI's answer (lines like  name: text) and try again."
                : applied + " translations were filled in. Check them, then press Save.",
                Program.Name, MessageBoxButtons.OK, applied == 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }

        static string OneLine(string s)
        {
            return (s ?? "").Replace("\r\n", "\\n").Replace("\n", "\\n");
        }
        #endregion

        #region Save
        void Save()
        {
            if (table == null || projectDir == null)
                return;
            if (!SaveStrings(false))
                return;
            if (!ApplyIconPackage())
                return;
            MessageBox.Show(this, "Saved.\n\nNow press Compile in the main window, then install the new APK" +
                "\n(uninstall the original app first, unless you changed the package name: the signature is different).",
                Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Writes the strings. silent = used by auto-translate after every batch.
        bool SaveStrings(bool silent)
        {
            if (table == null || projectDir == null)
                return false;
            if (!silent)
                grid.EndEdit();
            try
            {
                var translations = new Dictionary<string, string>();
                foreach (DataRow r in table.Rows)
                {
                    string tr = r["Translation"] as string;
                    translations[(string)r["Name"]] = tr == null ? "" : tr.Trim();
                }

                // App name goes into the translation and, so the launcher shows it on every phone, the default too.
                string appName = appNameBox.Text.Trim();
                bool renameApp = appNameBox.Enabled && appName != "" && appName != originalAppName;
                if (renameApp)
                    foreach (string name in appLabelStringNames)
                    {
                        translations[name] = appName;
                        foreach (DataRow r in table.Select("Name = '" + name.Replace("'", "''") + "'"))
                            r["Translation"] = appName;
                    }

                WriteTarget(translations);

                bool writeDefault = applyDefaultChk.Checked;
                if (writeDefault || (renameApp && appLabelStringNames.Count > 0) || File.Exists(BackupPath))
                    WriteDefault(translations, writeDefault, renameApp ? appName : null);

                if (renameApp && appLabelIsLiteral)
                    RenameLiteralLabels(appName);

                table.AcceptChanges();
                dirty = false;
                return true;
            }
            catch (Exception ex)
            {
                if (silent)
                    SetStatus("Auto-save failed: " + ex.Message);
                else
                    MessageBox.Show(this, "Saving failed:\n" + ex.Message, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        void WriteTarget(Dictionary<string, string> translations)
        {
            string path = TargetStringsPath;
            var doc = LoadOrCreate(path);
            bool any = false;
            foreach (var kv in translations)
            {
                XmlElement el = FindString(doc, kv.Key);
                if (kv.Value == "")
                {
                    if (el != null) el.ParentNode.RemoveChild(el);
                    continue;
                }
                if (el == null)
                    el = AppendString(doc, kv.Key);
                SetValue(el, kv.Value);
                any = true;
            }
            if (!any && !File.Exists(path))
                return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            SaveXml(doc, path);
        }

        // res/values/strings.xml: translated text when "every device" is on, otherwise the original back from the backup.
        void WriteDefault(Dictionary<string, string> translations, bool useTranslations, string appName)
        {
            if (!File.Exists(BackupPath))
                File.Copy(DefaultStringsPath, BackupPath);

            var originals = ReadStrings(BackupPath, false);
            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(DefaultStringsPath);
            foreach (var kv in originals)
            {
                XmlElement el = FindString(doc, kv.Key);
                if (el == null)
                    continue;
                string tr;
                bool isAppName = appName != null && appLabelStringNames.Contains(kv.Key);
                if (isAppName)
                    SetValue(el, appName);
                else if (useTranslations && translations.TryGetValue(kv.Key, out tr) && tr != "")
                    SetValue(el, tr);
                else
                    el.InnerXml = kv.Value;
            }
            SaveXml(doc, DefaultStringsPath);

            if (!useTranslations && appName == null)
                File.Delete(BackupPath); // default strings are back to the original
        }

        void RenameLiteralLabels(string appName)
        {
            string manifestPath = Path.Combine(projectDir, "AndroidManifest.xml");
            string manifest = File.ReadAllText(manifestPath, Encoding.UTF8);
            string encoded = XmlEncodeAttr(appName);
            foreach (string label in LauncherLabels(manifest).Where(l => !l.StartsWith("@")).Distinct().ToList())
                manifest = manifest.Replace("android:label=\"" + label + "\"", "android:label=\"" + encoded + "\"");
            File.WriteAllText(manifestPath, manifest, new UTF8Encoding(false));
            originalAppName = appName;
        }

        static XmlDocument LoadOrCreate(string path)
        {
            var doc = new XmlDocument { PreserveWhitespace = true };
            if (File.Exists(path))
                doc.Load(path);
            else
                doc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources>\n</resources>");
            return doc;
        }

        static XmlElement FindString(XmlDocument doc, string name)
        {
            foreach (XmlElement el in doc.DocumentElement.SelectNodes("string"))
                if (el.GetAttribute("name") == name)
                    return el;
            return null;
        }

        static XmlElement AppendString(XmlDocument doc, string name)
        {
            XmlElement el = doc.CreateElement("string");
            el.SetAttribute("name", name);
            XmlElement root = doc.DocumentElement;
            // Insert before the trailing newline of </resources> so the file stays one string per line.
            XmlNode tail = root.LastChild != null && root.LastChild.NodeType == XmlNodeType.Whitespace ? root.LastChild : null;
            root.InsertBefore(doc.CreateWhitespace("\n    "), tail);
            root.InsertBefore(el, tail);
            if (tail == null)
                root.AppendChild(doc.CreateWhitespace("\n"));
            return el;
        }

        static void SaveXml(XmlDocument doc, string path)
        {
            var settings = new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false };
            using (var w = XmlWriter.Create(path, settings))
                doc.Save(w);
        }

        // Strings whose original shows HTML tags as text (&lt;b&gt;): keep tags in translations as text too.
        readonly HashSet<string> literalTagNames = new HashSet<string>();

        // User text -> valid Android string resource content.
        void SetValue(XmlElement el, string text)
        {
            string s = text.Replace("\r\n", "\\n").Replace("\n", "\\n");
            if (literalTagNames.Contains(el.GetAttribute("name")))
                s = s.Replace("<", "&lt;");
            s = Regex.Replace(s, @"(?<!\\)'", "\\'");
            s = Regex.Replace(s, @"(?<!\\)""", "\\\"");
            if (s.StartsWith("@") || s.StartsWith("?"))
                s = "\\" + s;
            s = Regex.Replace(s, @"&(?!(amp|lt|gt|quot|apos|#\d+|#x[0-9a-fA-F]+);)", "&amp;");
            try
            {
                el.InnerXml = s; // keeps simple markup such as <b>..</b>
            }
            catch (XmlException)
            {
                el.InnerText = XmlDecode(s);
            }
        }

        // Raw inner XML -> what the user sees and edits.
        static string ToDisplay(string innerXml)
        {
            string s = XmlDecode(innerXml ?? "");
            string t = s.Trim();
            // Android "quoted" strings: the quotes are syntax (they keep apostrophes and line breaks), not text.
            if (t.Length >= 2 && t[0] == '"' && t[t.Length - 1] == '"' && !t.EndsWith("\\\""))
                s = t.Substring(1, t.Length - 2).Replace("\r\n", "\n").Replace("\n", "\\n");
            // Translations saved by v1.62-1.64 kept those quotes as visible \"...\" text: drop them.
            else if (t.Length >= 4 && t.StartsWith("\\\"") && t.EndsWith("\\\""))
                s = t.Substring(2, t.Length - 4);
            return s;
        }

        static string XmlDecode(string s)
        {
            return s.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'").Replace("&amp;", "&");
        }

        static string XmlEncodeAttr(string s)
        {
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        bool ConfirmDiscard()
        {
            grid.EndEdit();
            if (!dirty)
                return true;
            var r = MessageBox.Show(this, "Save your changes first?", Program.Name, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (r == DialogResult.Cancel)
                return false;
            if (r == DialogResult.Yes)
                Save();
            dirty = false;
            return true;
        }

        void TranslateForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ConfirmDiscard())
                e.Cancel = true;
        }
        #endregion
    }
}
