using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APKEasyTool
{
    // Auto-translation (OpenAI-compatible API), translation mode, icon and package name.
    public partial class TranslateForm
    {
        string settingsFile;
        CancellationTokenSource cts;

        readonly Button autoBtn = new Button();
        readonly ProgressBar progressBar = new ProgressBar();
        readonly Label statusLbl = new Label();
        readonly RadioButton modeAddRadio = new RadioButton();
        readonly RadioButton modeReplaceRadio = new RadioButton();

        readonly PictureBox currentIconBox = new PictureBox();
        readonly PictureBox newIconBox = new PictureBox();
        readonly Label iconInfo = new Label();
        readonly Label currentPkgLbl = new Label();
        readonly TextBox newPkgBox = new TextBox();
        readonly CheckBox patchCodeChk = new CheckBox();
        string pendingIconPath;

        #region Auto-translate UI
        void AddAiControls(FlowLayoutPanel bar, ToolTip tip)
        {
            autoBtn.Text = "Auto-translate (AI)";
            autoBtn.AutoSize = true;
            autoBtn.Font = new Font(Font, FontStyle.Bold);
            autoBtn.Click += AutoTranslate_Click;
            tip.SetToolTip(autoBtn, "Translates every untranslated text with your AI service (OpenAI-compatible).\nWork is saved after each batch, so you can stop and continue later.");
            var settingsBtn = new Button { Text = "AI settings...", AutoSize = true };
            settingsBtn.Click += (s, e) => OpenAiSettings();
            progressBar.Width = S(160);
            progressBar.Height = autoBtn.Height;
            progressBar.Visible = false;
            progressBar.Margin = new Padding(3, 8, 3, 3);
            statusLbl.AutoSize = true;
            statusLbl.ForeColor = SystemColors.GrayText;
            statusLbl.Padding = new Padding(0, 8, 0, 0);
            bar.Controls.Add(autoBtn);
            bar.Controls.Add(settingsBtn);
            bar.Controls.Add(progressBar);
            bar.Controls.Add(statusLbl);
            FormClosing += (s, e) => { if (cts != null) cts.Cancel(); };
        }

        Control BuildModeRow(ToolTip tip)
        {
            var row = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = new Padding(0, 0, 0, 4) };
            row.Controls.Add(new Label { Text = "Put the translation:", AutoSize = true, Padding = new Padding(0, 5, 0, 0) });
            modeAddRadio.AutoSize = true;
            modeReplaceRadio.AutoSize = true;
            modeAddRadio.Checked = true;
            modeAddRadio.CheckedChanged += (s, e) => { if (modeAddRadio.Checked) applyDefaultChk.Checked = false; };
            modeReplaceRadio.CheckedChanged += (s, e) => { if (modeReplaceRadio.Checked) applyDefaultChk.Checked = true; };
            tip.SetToolTip(modeAddRadio, "Writes res/values-xx/strings.xml. The original language stays the default.");
            tip.SetToolTip(modeReplaceRadio, "Also writes the translation into res/values (the default texts).\nThe original texts are kept in " + BackupFileName + ", so you can switch back.");
            row.Controls.Add(modeAddRadio);
            row.Controls.Add(modeReplaceRadio);
            SyncModeRadios();
            return row;
        }

        void SyncModeRadios()
        {
            string lang = loadedLang ?? LangCode;
            modeAddRadio.Text = "as a new language \"" + lang + "\" (shown when the phone is set to it)";
            modeReplaceRadio.Text = "replace the app's own texts (shown on every phone)";
            if (applyDefaultChk.Checked != modeReplaceRadio.Checked)
            {
                modeReplaceRadio.Checked = applyDefaultChk.Checked;
                modeAddRadio.Checked = !applyDefaultChk.Checked;
            }
        }

        void SetStatus(string text)
        {
            statusLbl.Text = text ?? "";
        }

        bool OpenAiSettings()
        {
            using (var f = new AiSettingsForm(settingsFile))
                return f.ShowDialog(this) == DialogResult.OK;
        }

        async void AutoTranslate_Click(object sender, EventArgs e)
        {
            if (cts != null)
            {
                cts.Cancel();
                autoBtn.Enabled = false;
                SetStatus("Stopping...");
                return;
            }
            if (table == null || projectDir == null)
                return;
            if (!File.Exists(settingsFile) && !OpenAiSettings())
                return;
            AiSettings settings = AiSettings.Load(settingsFile);

            grid.EndEdit();
            var rows = table.Rows.Cast<DataRow>().Where(r => !(r["Translation"] is string) || ((string)r["Translation"]).Trim() == "").ToList();
            if (rows.Count == 0)
            {
                MessageBox.Show(this, "Everything is already translated.", Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string host;
            try { host = new Uri(settings.Endpoint).Host; } catch { host = settings.BaseUrl; }
            if (MessageBox.Show(this, rows.Count + " texts will be translated into \"" + loadedLang + "\" with " + settings.Model + " (" + host + ").\n\n" +
                    "Work is saved after every batch: you can stop at any time and press Auto-translate again later to continue.",
                    Program.Name, MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK)
                return;

            var items = new Dictionary<string, string>();
            foreach (var r in rows)
                items[(string)r["Name"]] = (string)r["Original"];
            var byName = table.Rows.Cast<DataRow>().ToDictionary(r => (string)r["Name"]);

            cts = new CancellationTokenSource();
            autoBtn.Text = "Stop";
            progressBar.Maximum = items.Count;
            progressBar.Value = 0;
            progressBar.Visible = true;
            saveBtn.Enabled = false;
            SetStatus("Starting...");

            int done = 0, failed = 0;
            var progress = new Progress<AiProgress>(p =>
            {
                done = p.Done;
                failed = p.Failed;
                if (p.Translated != null && p.Translated.Count > 0)
                {
                    foreach (var kv in p.Translated)
                    {
                        DataRow row;
                        if (byName.TryGetValue(kv.Key, out row) && (!(row["Translation"] is string) || ((string)row["Translation"]).Trim() == ""))
                            row["Translation"] = kv.Value;
                    }
                    SaveStrings(true);
                }
                progressBar.Value = Math.Min(progressBar.Maximum, p.Done + p.Failed);
                SetStatus(p.Status ?? ("Translated " + p.Done + " of " + p.Total + (p.Failed > 0 ? ", " + p.Failed + " failed" : "") + " (saved)"));
                UpdateCount();
            });

            string summary = null;
            try
            {
                await new AiTranslator(settings).TranslateAll(items, loadedLang, progress, cts.Token);
                await Task.Delay(200); // let the last progress report reach the UI
                summary = "Done: " + done + " of " + items.Count + " texts translated and saved." +
                    (items.Count - done > 0 ? "\n" + (items.Count - done) + " could not be translated; press Auto-translate again to retry them." : "") +
                    "\n\nCheck the result, then press Compile in the main window.";
            }
            catch (OperationCanceledException)
            {
                SetStatus("Stopped. " + done + " translated so far (saved).");
            }
            catch (AiFatalException ex)
            {
                summary = ex.Message;
            }
            catch (Exception ex)
            {
                summary = "Auto-translate stopped:\n" + ex.Message + "\n\n" + done + " texts were translated and saved. Press Auto-translate again to continue.";
            }
            finally
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
                autoBtn.Text = "Auto-translate (AI)";
                autoBtn.Enabled = true;
                saveBtn.Enabled = true;
                progressBar.Visible = false;
                if (!IsDisposed)
                {
                    SaveStrings(true);
                    UpdateCount();
                    grid.Invalidate();
                }
            }
            if (summary != null && !IsDisposed)
            {
                SetStatus("");
                MessageBox.Show(this, summary, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion

        #region Icon and package
        Control BuildIconPackagePage(ToolTip tip)
        {
            var page = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1 };

            // Icon
            page.Controls.Add(Header("App icon"));
            var iconRow = new FlowLayoutPanel { AutoSize = true, WrapContents = true };
            foreach (var box in new[] { currentIconBox, newIconBox })
            {
                box.Size = new Size(S(96), S(96));
                box.SizeMode = PictureBoxSizeMode.Zoom;
                box.BorderStyle = BorderStyle.FixedSingle;
                box.BackColor = Color.White;
            }
            var chooseBtn = new Button { Text = "Choose image...", AutoSize = true };
            chooseBtn.Click += ChooseIcon_Click;
            var undoBtn = new Button { Text = "Keep current icon", AutoSize = true };
            undoBtn.Click += (s, e) => { pendingIconPath = null; SetImage(newIconBox, null); };
            iconRow.Controls.Add(Caption("Current:"));
            iconRow.Controls.Add(currentIconBox);
            iconRow.Controls.Add(Caption("   New:"));
            iconRow.Controls.Add(newIconBox);
            iconRow.Controls.Add(chooseBtn);
            iconRow.Controls.Add(undoBtn);
            page.Controls.Add(iconRow);
            iconInfo.AutoSize = true;
            iconInfo.MaximumSize = new Size(S(760), 0);
            iconInfo.ForeColor = SystemColors.GrayText;
            iconInfo.Text = "PNG or JPG, square, 512x512 or larger works best. It is saved in every size Android needs " +
                "(mipmap-mdpi ... xxxhdpi) and used as the launcher icon and round icon.";
            page.Controls.Add(iconInfo);

            // Package
            page.Controls.Add(Header("Package name"));
            var pkgRow = new TableLayoutPanel { AutoSize = true, ColumnCount = 2 };
            pkgRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pkgRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            currentPkgLbl.AutoSize = true;
            currentPkgLbl.Anchor = AnchorStyles.Left;
            newPkgBox.Width = S(380);
            pkgRow.Controls.Add(Lbl("Current:"), 0, 0);
            pkgRow.Controls.Add(currentPkgLbl, 1, 0);
            pkgRow.Controls.Add(Lbl("New:"), 0, 1);
            pkgRow.Controls.Add(newPkgBox, 1, 1);
            page.Controls.Add(pkgRow);
            patchCodeChk.Text = "Also update matching provider/permission names inside the code (recommended)";
            patchCodeChk.AutoSize = true;
            patchCodeChk.Checked = true;
            tip.SetToolTip(patchCodeChk, "Some apps hard-code their provider authorities (e.g. com.app.fileprovider).\nThose exact strings are updated in smali so file sharing, downloads etc. keep working.");
            page.Controls.Add(patchCodeChk);
            var warn = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(S(760), 0),
                ForeColor = Color.FromArgb(150, 80, 0),
                Text = "A new package name lets the edited app install next to the original. Class names are not touched, " +
                    "only the application id, provider authorities and the app's own permissions, so the app normally keeps working.\n" +
                    "Expect these to stop working in a renamed app: Google/Facebook login, Firebase push notifications, " +
                    "in-app purchases and apps that check their own package name or signature. Keep the original name if you need them."
            };
            page.Controls.Add(warn);
            return page;
        }

        static Label Header(string text)
        {
            return new Label { Text = text, AutoSize = true, Font = new Font(SystemFonts.MessageBoxFont.FontFamily, SystemFonts.MessageBoxFont.Size + 2, FontStyle.Bold), Margin = new Padding(0, 10, 0, 4) };
        }

        static Label Caption(string text)
        {
            return new Label { Text = text, AutoSize = true, Padding = new Padding(0, 40, 0, 0) };
        }

        static void SetImage(PictureBox box, Image img)
        {
            Image old = box.Image;
            box.Image = img;
            if (old != null)
                old.Dispose();
        }

        void LoadIconPackage()
        {
            SyncModeRadios();
            pendingIconPath = null;
            SetImage(newIconBox, null);
            string icon = AppEditTools.FindCurrentIconImage(projectDir);
            try { SetImage(currentIconBox, icon != null ? AppEditTools.LoadImage(icon) : null); }
            catch { SetImage(currentIconBox, null); } // e.g. webp, which GDI+ can't read
            string pkg = AppEditTools.GetPackage(projectDir);
            currentPkgLbl.Text = pkg;
            newPkgBox.Text = pkg;
        }

        void ChooseIcon_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Images (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif";
                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return;
                try
                {
                    SetImage(newIconBox, AppEditTools.LoadImage(ofd.FileName));
                    pendingIconPath = ofd.FileName;
                    dirty = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Could not open the image:\n" + ex.Message, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        // Applies the chosen icon and package name. Returns false if the user cancelled or it failed.
        bool ApplyIconPackage()
        {
            try
            {
                if (pendingIconPath != null)
                {
                    AppEditTools.ApplyIcon(projectDir, pendingIconPath);
                    SetImage(currentIconBox, AppEditTools.LoadImage(pendingIconPath));
                    SetImage(newIconBox, null);
                    pendingIconPath = null;
                }

                string current = AppEditTools.GetPackage(projectDir);
                string wanted = newPkgBox.Text.Trim();
                if (wanted != "" && wanted != current)
                {
                    if (!AppEditTools.IsValidPackage(wanted))
                    {
                        MessageBox.Show(this, "\"" + wanted + "\" is not a valid package name.\nUse at least two parts with letters, digits or _, for example com.example.myapp",
                            Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }
                    if (MessageBox.Show(this, "Change the package name\n\n  " + current + "\n-> " + wanted + "\n\n" +
                            "Login with Google/Facebook, push notifications and in-app purchases usually stop working after this. Continue?",
                            Program.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                        return false;
                    var r = AppEditTools.RenamePackage(projectDir, wanted, patchCodeChk.Checked);
                    currentPkgLbl.Text = AppEditTools.GetPackage(projectDir);
                    SetStatus("Package renamed: " + r.RenamedValues + " authorities/permissions, " + r.PatchedResFiles + " resource files, " + r.PatchedSmaliFiles + " code files updated.");
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not apply the icon/package change:\n" + ex.Message, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        #endregion
    }

    /// <summary>Endpoint, key and model for auto-translation.</summary>
    public class AiSettingsForm : Form
    {
        readonly string file;
        readonly TextBox urlBox = new TextBox();
        readonly TextBox keyBox = new TextBox();
        readonly TextBox modelBox = new TextBox();
        readonly NumericUpDown batchNum = new NumericUpDown();
        readonly NumericUpDown parallelNum = new NumericUpDown();
        readonly NumericUpDown delayNum = new NumericUpDown();
        readonly Label testLbl = new Label();

        public AiSettingsForm(string settingsFile)
        {
            file = settingsFile;
            Text = "AI settings - " + Program.Name;
            Font = SystemFonts.MessageBoxFont;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            int w = (int)(420 * DeviceDpi / 96f);
            var t = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Padding = new Padding(10) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            urlBox.Width = keyBox.Width = modelBox.Width = w;
            keyBox.UseSystemPasswordChar = true;
            batchNum.Minimum = 1; batchNum.Maximum = 200;
            parallelNum.Minimum = 1; parallelNum.Maximum = 8;
            delayNum.Minimum = 0; delayNum.Maximum = 60000; delayNum.Increment = 250;

            AddRow(t, "Base URL:", urlBox, "OpenAI-compatible, e.g. https://api.openai.com/v1 (\"/chat/completions\" is added).");
            AddRow(t, "API key:", keyBox, "Stored encrypted for your Windows user.");
            AddRow(t, "Model ID:", modelBox, "e.g. gpt-4o-mini, deepseek-chat, gemini-2.0-flash, a local model name ...");
            AddRow(t, "Texts per request:", batchNum, "Smaller = more requests, but safer with small models. 30-60 is usual.");
            AddRow(t, "Parallel requests:", parallelNum, "Use 1 if your plan has a low rate limit.");
            AddRow(t, "Pause between requests (ms):", delayNum, "Extra pause to stay under a rate limit. 0 = none.");

            var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
            var ok = new Button { Text = "Save", AutoSize = true };
            ok.Click += (s, e) => { Collect().Save(file); DialogResult = DialogResult.OK; };
            var test = new Button { Text = "Test connection", AutoSize = true };
            test.Click += Test_Click;
            testLbl.AutoSize = true;
            testLbl.Padding = new Padding(0, 6, 8, 0);
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            buttons.Controls.Add(test);
            buttons.Controls.Add(testLbl);
            t.Controls.Add(buttons, 0, t.RowCount);
            t.SetColumnSpan(buttons, 2);
            Controls.Add(t);
            CancelButton = cancel;

            AiSettings st = AiSettings.Load(file);
            urlBox.Text = st.BaseUrl;
            keyBox.Text = st.ApiKey;
            modelBox.Text = st.Model;
            batchNum.Value = Math.Max(1, Math.Min(200, st.BatchSize));
            parallelNum.Value = Math.Max(1, Math.Min(8, st.Parallel));
            delayNum.Value = Math.Max(0, Math.Min(60000, st.DelayMs));
        }

        static void AddRow(TableLayoutPanel t, string label, Control c, string hint)
        {
            int row = t.RowCount++;
            t.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            var cell = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = new Padding(0, 4, 0, 4) };
            cell.Controls.Add(c);
            cell.Controls.Add(new Label { Text = hint, AutoSize = true, ForeColor = SystemColors.GrayText });
            t.Controls.Add(cell, 1, row);
        }

        AiSettings Collect()
        {
            var st = new AiSettings
            {
                BaseUrl = urlBox.Text.Trim(),
                Model = modelBox.Text.Trim(),
                BatchSize = (int)batchNum.Value,
                Parallel = (int)parallelNum.Value,
                DelayMs = (int)delayNum.Value
            };
            st.ApiKey = keyBox.Text.Trim();
            return st;
        }

        async void Test_Click(object sender, EventArgs e)
        {
            testLbl.ForeColor = SystemColors.GrayText;
            testLbl.Text = "Testing...";
            try
            {
                using (var c = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                {
                    string r = await new AiTranslator(Collect()).Test(c.Token);
                    testLbl.ForeColor = r != null ? Color.Green : Color.DarkOrange;
                    testLbl.Text = r != null ? "OK: Hello -> " + r : "Connected, but the answer wasn't usable JSON";
                }
            }
            catch (Exception ex)
            {
                testLbl.ForeColor = Color.Red;
                testLbl.Text = "Failed: " + (ex.Message.Length > 120 ? ex.Message.Substring(0, 120) + "..." : ex.Message);
            }
        }
    }
}
