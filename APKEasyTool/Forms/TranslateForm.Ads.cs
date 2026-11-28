using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APKEasyTool
{
    // "Remove ads" tab.
    public partial class TranslateForm
    {
        readonly CheckedListBox adList = new CheckedListBox();
        readonly Button adScanBtn = new Button();
        readonly Button adRemoveBtn = new Button();
        readonly Button adRestoreBtn = new Button();
        readonly Label adStatus = new Label();

        Control BuildAdsPage()
        {
            var page = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
            page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            page.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            page.Controls.Add(Header("Remove ads"), 0, 0);
            page.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(S(820), 0),
                Text = "Finds known ad SDKs (AdMob, AppLovin, Unity, Meta, ironSource, Tapsell, Adivery ...) and disables them: " +
                    "their load/show/initialize methods do nothing and \"is the ad ready?\" checks answer no, so no ad is ever shown. " +
                    "The original code is backed up, so \"Restore\" undoes it. Then press Compile in the main window."
            }, 0, 1);

            adList.Dock = DockStyle.Fill;
            adList.CheckOnClick = true;
            adList.IntegralHeight = false;
            page.Controls.Add(adList, 0, 2);

            var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
            adScanBtn.Text = "Scan again";
            adScanBtn.AutoSize = true;
            adScanBtn.Click += (s, e) => ScanAds();
            adRemoveBtn.Text = "Remove ads (checked SDKs)";
            adRemoveBtn.AutoSize = true;
            adRemoveBtn.Font = new Font(Font, FontStyle.Bold);
            adRemoveBtn.Click += RemoveAds_Click;
            adRestoreBtn.Text = "Restore original code";
            adRestoreBtn.AutoSize = true;
            adRestoreBtn.Click += RestoreAds_Click;
            adStatus.AutoSize = true;
            adStatus.Padding = new Padding(0, 8, 0, 0);
            buttons.Controls.Add(adScanBtn);
            buttons.Controls.Add(adRemoveBtn);
            buttons.Controls.Add(adRestoreBtn);
            buttons.Controls.Add(adStatus);
            page.Controls.Add(buttons, 0, 3);

            page.Controls.Add(new Label
            {
                AutoSize = true,
                MaximumSize = new Size(S(820), 0),
                ForeColor = Color.FromArgb(150, 80, 0),
                Text = "Test the app afterwards: a few apps wait for an ad before they continue, or give rewards only after a video ad. " +
                    "If something gets stuck, restore and uncheck that SDK. Ads the app serves from its own server are not affected. " +
                    "Ads pay the developers: use this for your own use, not to republish other people's apps."
            }, 0, 4);
            return page;
        }

        async void ScanAds()
        {
            if (projectDir == null)
                return;
            string dir = projectDir;
            adList.Items.Clear();
            SetAdButtons(false);
            adStatus.Text = "Scanning...";
            List<AdRemover.AdSdk> found;
            try
            {
                found = await Task.Run(() => AdRemover.Detect(dir));
            }
            catch (Exception ex)
            {
                adStatus.Text = "Scan failed: " + ex.Message;
                SetAdButtons(true);
                return;
            }
            if (dir != projectDir || IsDisposed)
                return;
            foreach (var sdk in found)
                adList.Items.Add(sdk, !sdk.Disabled);
            int disabled = found.Count(x => x.Disabled);
            if (found.Count == 0)
                SetAdStatus("No known ad SDK found in this app.", false);
            else if (disabled == found.Count)
                SetAdStatus("✔ Ads are disabled in this project (" + disabled + " SDK). If you haven't yet, press Compile in the main window.", true);
            else if (disabled > 0)
                SetAdStatus(disabled + " of " + found.Count + " SDKs already disabled; the checked ones are not yet.", false);
            else
                SetAdStatus(found.Count + " ad SDK(s) found. Ads are still active: press \"Remove ads\".", false);
            SetAdButtons(true);
        }

        void SetAdStatus(string text, bool done)
        {
            adStatus.Text = text;
            adStatus.ForeColor = done ? Color.FromArgb(0, 128, 0) : SystemColors.ControlText;
            adStatus.Font = new Font(Font, done ? FontStyle.Bold : FontStyle.Regular);
        }

        void SetAdButtons(bool enabled)
        {
            adScanBtn.Enabled = enabled;
            adRemoveBtn.Enabled = enabled && adList.Items.Count > 0;
            adRestoreBtn.Enabled = enabled && projectDir != null && AdRemover.HasBackup(projectDir);
        }

        async void RemoveAds_Click(object sender, EventArgs e)
        {
            var sdks = adList.CheckedItems.Cast<AdRemover.AdSdk>().ToList();
            if (sdks.Count == 0)
            {
                MessageBox.Show(this, "Check at least one SDK.", Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string dir = projectDir;
            SetAdButtons(false);
            adStatus.Text = "Patching...";
            try
            {
                var progress = new Progress<string>(t => adStatus.Text = t);
                IProgress<string> p = progress;
                AdRemover.Result r = await Task.Run(() => AdRemover.Remove(dir, sdks, t => p.Report(t)));
                var lines = new List<string>();
                if (r.Methods > 0)
                    lines.Add("• " + r.Methods + " ad methods disabled in " + r.Files + " code files (no ad is loaded or shown).");
                if (r.AlreadyDone > 0)
                    lines.Add("• " + r.AlreadyDone + " code files were already disabled by an earlier run (left as they are).");
                if (r.Layouts > 0)
                    lines.Add("• " + r.Layouts + " ad boxes in the layouts hidden, so no empty space is left where an ad was.");
                if (lines.Count == 0)
                    lines.Add("• Nothing left to change: ads were already disabled.");
                ScanAds();
                MessageBox.Show(this, "✔ Ads are disabled in this project.\n\n" + String.Join("\n", lines) +
                    "\n\nNext: press Compile in the main window, then install and test the app." +
                    "\n(Pressing \"Remove ads\" again is safe: nothing is changed twice.)",
                    Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                adStatus.Text = "Failed: " + ex.Message;
                MessageBox.Show(this, "Removing ads failed:\n" + ex.Message, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            SetAdButtons(true);
        }

        async void RestoreAds_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "Put the original ad code back?", Program.Name, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            string dir = projectDir;
            SetAdButtons(false);
            try
            {
                int n = await Task.Run(() => AdRemover.Restore(dir));
                ScanAds(); // list shows the SDKs as active again; Compile to get the ads back
            }
            catch (Exception ex)
            {
                adStatus.Text = "Restore failed: " + ex.Message;
            }
            SetAdButtons(true);
        }
    }
}
