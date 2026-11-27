using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace APKEasyTool
{
    // "Fonts" tab: see which fonts the app uses and change them.
    public partial class TranslateForm
    {
        readonly ListBox fontList = new ListBox();
        readonly Label fontUsageLbl = new Label();
        readonly Label fontStatus = new Label();
        readonly Button fontRestoreBtn = new Button();

        const string FontFilter = "Fonts (*.ttf;*.otf)|*.ttf;*.otf";

        Control BuildFontsPage()
        {
            var page = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 8 };
            for (int i = 0; i < 8; i++)
                page.RowStyles.Add(i == 3 ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));

            page.Controls.Add(Header("Default text font"), 0, 0);
            fontUsageLbl.AutoSize = true;
            fontUsageLbl.MaximumSize = new Size(S(860), 0);
            page.Controls.Add(fontUsageLbl, 0, 1);

            var defaultRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
            var setDefaultBtn = new Button { Text = "Set app font...", AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
            setDefaultBtn.Click += SetDefaultFont_Click;
            defaultRow.Controls.Add(setDefaultBtn);
            defaultRow.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(S(700), 0),
                Padding = new Padding(0, 6, 0, 0),
                ForeColor = SystemColors.GrayText,
                Text = "Adds the font to the app and makes the app theme and all text styles that use the system font (sans-serif / Roboto) use it. " +
                    "For a Persian translation pick a font with Persian letters, e.g. Vazirmatn. Bold text is drawn bold automatically."
            });
            page.Controls.Add(defaultRow, 0, 2);

            var filesBox = new GroupBox { Text = "Font files inside the app", Dock = DockStyle.Fill };
            fontList.Dock = DockStyle.Fill;
            fontList.IntegralHeight = false;
            filesBox.Controls.Add(fontList);
            page.Controls.Add(filesBox, 0, 3);

            var fileButtons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
            var replaceOneBtn = new Button { Text = "Replace selected font...", AutoSize = true };
            replaceOneBtn.Click += (s, e) => ReplaceFonts(false);
            var replaceAllBtn = new Button { Text = "Replace all font files...", AutoSize = true };
            replaceAllBtn.Click += (s, e) => ReplaceFonts(true);
            fontRestoreBtn.Text = "Restore original fonts";
            fontRestoreBtn.AutoSize = true;
            fontRestoreBtn.Click += RestoreFonts_Click;
            fontStatus.AutoSize = true;
            fontStatus.Padding = new Padding(0, 8, 0, 0);
            fileButtons.Controls.Add(replaceOneBtn);
            fileButtons.Controls.Add(replaceAllBtn);
            fileButtons.Controls.Add(fontRestoreBtn);
            fileButtons.Controls.Add(fontStatus);
            page.Controls.Add(fileButtons, 0, 4);

            page.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(S(860), 0),
                ForeColor = Color.FromArgb(150, 80, 0),
                Text = "Changes are applied right away; press Compile in the main window afterwards. " +
                    "Apps that choose fonts only in code (Jetpack Compose, games) may not change. Monospace and serif fonts are left alone."
            }, 0, 5);
            return page;
        }

        void ScanFonts()
        {
            fontList.Items.Clear();
            if (projectDir == null)
                return;
            try
            {
                foreach (var f in FontTools.FindFonts(projectDir))
                    fontList.Items.Add(f);
                var usage = FontTools.FontUsage(projectDir);
                string desc = usage.Count == 0
                    ? "No text style sets a font: the app uses the phone's system font (Roboto on most phones)."
                    : "Text styles use: " + String.Join(", ", usage.Take(6).Select(kv => Describe(kv.Key) + " ×" + kv.Value)) + ".";
                if (usage.Any(kv => kv.Key == "@font/" + FontTools.DefaultFontName))
                    desc += "\nYour font is already set as the app font.";
                fontUsageLbl.Text = desc;
                fontStatus.Text = fontList.Items.Count == 0 ? "No font files in this app." : "";
            }
            catch (Exception ex)
            {
                fontStatus.Text = "Scan failed: " + ex.Message;
            }
            fontRestoreBtn.Enabled = FontTools.HasBackup(projectDir);
        }

        static string Describe(string family)
        {
            if (family.StartsWith("sans-serif") || family == "roboto")
                return family + " (system font)";
            return family;
        }

        string PickFont(string title)
        {
            using (var ofd = new OpenFileDialog { Filter = FontFilter, Title = title })
                return ofd.ShowDialog(this) == DialogResult.OK ? ofd.FileName : null;
        }

        void SetDefaultFont_Click(object sender, EventArgs e)
        {
            if (projectDir == null)
                return;
            string font = PickFont("Choose the font for the app's text");
            if (font == null)
                return;
            try
            {
                var r = FontTools.SetDefaultFont(projectDir, font);
                ScanFonts();
                fontStatus.Text = "App font set: " + r.Themes + " themes, " + r.StyleItems + " text styles, " + r.LayoutAttrs + " layout attributes.";
                MessageBox.Show(this, "\"" + (FontTools.FamilyName(font) ?? Path.GetFileName(font)) + "\" is now the app font.\n\n" + fontStatus.Text +
                    "\n\nPress Compile in the main window.", Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not set the font:\n" + ex.Message, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void ReplaceFonts(bool all)
        {
            if (projectDir == null)
                return;
            var targets = all ? fontList.Items.Cast<FontTools.FontFile>().ToList()
                : fontList.SelectedItem != null ? new[] { (FontTools.FontFile)fontList.SelectedItem }.ToList() : null;
            if (targets == null || targets.Count == 0)
            {
                MessageBox.Show(this, all ? "This app has no font files." : "Select a font in the list first.", Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string font = PickFont(all ? "Font that replaces every font file" : "Font that replaces " + targets[0].RelPath);
            if (font == null)
                return;
            try
            {
                foreach (var t in targets)
                    FontTools.ReplaceFont(projectDir, t.RelPath, font);
                ScanFonts();
                fontStatus.Text = targets.Count + " font file(s) replaced. Press Compile in the main window.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not replace the font:\n" + ex.Message, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void RestoreFonts_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "Put the original fonts and text styles back?", Program.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                int n = FontTools.Restore(projectDir);
                ScanFonts();
                fontStatus.Text = n + " files restored.";
            }
            catch (Exception ex)
            {
                fontStatus.Text = "Restore failed: " + ex.Message;
            }
        }
    }
}
