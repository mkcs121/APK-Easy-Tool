using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace APKEasyTool
{
    // Texts outside res/values: JSON language files (games, i18n folders) and web apps (Ionic/Capacitor/Cordova).
    public partial class TranslateForm
    {
        enum TextSource { Android, Json, Web }

        TextSource currentSource = TextSource.Android;
        readonly ComboBox sourceBox = new ComboBox();
        readonly CheckBox singleWordsChk = new CheckBox();
        readonly CheckBox rtlChk = new CheckBox();
        readonly Dictionary<string, string> extKeys = new Dictionary<string, string>(); // row Name -> stable key
        bool suppressSourceChange;
        string autoSourceFolder;

        void AddSourceControls(FlowLayoutPanel bar, ToolTip tip)
        {
            sourceBox.DropDownStyle = ComboBoxStyle.DropDownList;
            sourceBox.Width = S(230);
            sourceBox.Items.AddRange(new object[] { "Android strings (res/values)", "Language files (JSON)", "Web app texts (JS/HTML)" });
            sourceBox.SelectedIndex = 0;
            tip.SetToolTip(sourceBox, "Where the app keeps its texts.\n" +
                "Android strings: normal Android apps.\n" +
                "Language files: games (Cocos2d ...) and apps with assets/.../en.json.\n" +
                "Web app: Ionic / Capacitor / Cordova / React Native apps (texts inside the JS and HTML files).");
            sourceBox.SelectedIndexChanged += SourceBox_Changed;

            singleWordsChk.Text = "Include single words";
            singleWordsChk.AutoSize = true;
            singleWordsChk.Visible = false;
            singleWordsChk.Padding = new Padding(0, 4, 0, 0);
            tip.SetToolTip(singleWordsChk, "Also list one-word texts such as \"Cancel\" or \"Settings\".\nRiskier: in code a single word can also be a key. Test the app after compiling.");
            singleWordsChk.CheckedChanged += (s, e) => { if (!suppressSourceChange && projectDir != null) LoadProject(); };

            rtlChk.Text = "Right-to-left layout";
            rtlChk.AutoSize = true;
            rtlChk.Visible = false;
            rtlChk.Padding = new Padding(0, 4, 0, 0);
            tip.SetToolTip(rtlChk, "Sets dir=\"rtl\" on the web app's <html>, so Persian/Arabic pages are laid out right-to-left.");
            rtlChk.CheckedChanged += (s, e) => dirty = true;

            bar.Controls.Add(new Label { Text = "Texts from:", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
            bar.Controls.Add(sourceBox);
            bar.Controls.Add(singleWordsChk);
            bar.Controls.Add(rtlChk);
        }

        void SourceBox_Changed(object sender, EventArgs e)
        {
            if (suppressSourceChange)
                return;
            if (!ConfirmDiscard())
            {
                SetSource(currentSource);
                return;
            }
            currentSource = (TextSource)sourceBox.SelectedIndex;
            if (projectDir != null)
                LoadProject();
        }

        void SetSource(TextSource source)
        {
            suppressSourceChange = true;
            sourceBox.SelectedIndex = (int)source;
            suppressSourceChange = false;
            currentSource = source;
        }

        // First time a folder is opened: if it has (almost) no Android strings but has JSON language
        // files or web texts, show those instead.
        void AutoPickSource(int androidStrings)
        {
            if (autoSourceFolder == projectDir)
                return;
            autoSourceFolder = projectDir;
            TextSource pick = TextSource.Android;
            if (androidStrings < 15)
            {
                if (ExternalTexts.FindJsonLanguageFiles(projectDir).Count > 0)
                    pick = TextSource.Json;
                else if (ExternalTexts.IsWebApp(projectDir))
                    pick = TextSource.Web;
            }
            SetSource(pick);
        }

        /// <summary>Replaces the table with JSON/web texts when that source is selected.</summary>
        void LoadExternalTable(bool rtl)
        {
            bool web = currentSource == TextSource.Web;
            singleWordsChk.Visible = web;
            rtlChk.Visible = web;
            grid.Columns["Where"].Visible = currentSource != TextSource.Android;
            modeAddRadio.Enabled = currentSource != TextSource.Web;
            if (currentSource == TextSource.Android)
                return;

            suppressSourceChange = true;
            rtlChk.Checked = rtl;
            suppressSourceChange = false;
            // Games and web apps only show their own files: replacing the texts is what makes them visible.
            modeReplaceRadio.Checked = true;

            List<ExtText> items = web ? ExternalTexts.LoadWeb(projectDir, singleWordsChk.Checked) : ExternalTexts.LoadJson(projectDir);
            var saved = ExternalTexts.LoadSidecar(projectDir, web);
            extKeys.Clear();
            table.Rows.Clear();
            int i = 0;
            foreach (var it in items)
            {
                string id = (web ? "w" : "j") + (++i);
                extKeys[id] = it.Key;
                string tr;
                saved.TryGetValue(it.Key, out tr);
                table.Rows.Add(id, Lines(it.Original), Lines(tr ?? ""), it.Where);
            }
            table.AcceptChanges();
            if (items.Count == 0)
                SetStatus(web ? "No web texts found in assets (JS/HTML)." : "No JSON language files found in assets.");
        }

        static string Lines(string s)
        {
            return (s ?? "").Replace("\r\n", "\n").Replace("\n", "\\n");
        }

        bool SaveExternal(bool silent)
        {
            if (!silent)
                grid.EndEdit();
            try
            {
                var map = new Dictionary<string, string>();
                foreach (DataRow r in table.Rows)
                {
                    string key;
                    if (!extKeys.TryGetValue((string)r["Name"], out key))
                        continue;
                    string tr = r["Translation"] as string;
                    map[key] = tr == null ? "" : tr.Trim().Replace("\\n", "\n");
                }
                bool rtl = RtlLanguages.Contains(loadedLang.Split('-')[0].ToLowerInvariant());
                if (currentSource == TextSource.Json)
                    ExternalTexts.SaveJson(projectDir, map, modeReplaceRadio.Checked, loadedLang);
                else
                    ExternalTexts.SaveWeb(projectDir, map, singleWordsChk.Checked, rtlChk.Checked && rtl ? loadedLang : null);
                SaveAppNameOnly();
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

        // The app name lives in Android resources even when the texts come from JSON/web files.
        void SaveAppNameOnly()
        {
            string appName = appNameBox.Text.Trim();
            if (!appNameBox.Enabled || appName == "" || appName == originalAppName)
                return;
            if (appLabelStringNames.Count > 0)
            {
                if (!File.Exists(BackupPath))
                    File.Copy(DefaultStringsPath, BackupPath);
                foreach (string path in new[] { DefaultStringsPath, TargetStringsPath })
                {
                    if (!File.Exists(path))
                        continue;
                    var doc = new XmlDocument { PreserveWhitespace = true };
                    doc.Load(path);
                    foreach (string name in appLabelStringNames)
                    {
                        XmlElement el = FindString(doc, name);
                        if (el != null)
                            SetValue(el, appName);
                    }
                    SaveXml(doc, path);
                }
            }
            if (appLabelIsLiteral)
                RenameLiteralLabels(appName);
        }
    }
}
