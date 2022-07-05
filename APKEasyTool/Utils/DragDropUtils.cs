using APKEasyTool.Enums;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace APKEasyTool
{
    public static class DragDropUtils
    {
        private static readonly string[] EmptyStrings = new string[0];

        public static string[] GetFilesDrop(this DragEventArgs args)
        {
            return (string[])args.Data.GetData(DataFormats.FileDrop) ?? EmptyStrings;
        }

        public static bool CheckDragOver(this DragEventArgs e, params string[] extensions)
        {
            string[] files = e.GetFilesDrop();
            if (extensions.Any(ext => files[0].EndsWith(ext, StringComparison.Ordinal)))
            {
                return true;
            }
            return false;
        }

        public static bool CheckDragOver(this DragEventArgs e)
        {
            string[] files = e.GetFilesDrop();
            if (File.Exists(Path.Combine(files[0], "apktool.yml")))
            {
                return true;
            }
            return false;
        }

        public static void CheckDragEnter(this DragEventArgs e, params string[] extensions)
        {
            string[] files = e.GetFilesDrop();
            if (extensions.Any(ext => files[0].EndsWith(ext, StringComparison.Ordinal)))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        
        public static void CheckDragEnter(this DragEventArgs e)
        {
            string[] files = e.GetFilesDrop();
            if (File.Exists(Path.Combine(files[0], "apktool.yml")))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        internal static void tab1_DragDrop(this MainForm main, object sender, DragEventArgs e)
        {
            main.tabMain.BackColor = Color.White;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (ext.Equals(".apk", StringComparison.CurrentCultureIgnoreCase))
                {
                    main.pathOfApk.Text = file.ToString();
                    main.decNameTextBox.Text = Path.GetFileNameWithoutExtension(main.pathOfApk.Text);
                    main.comNameTextBox.Text = Path.GetFileNameWithoutExtension(main.pathOfApk.Text);
                    if (main.disHisBox.Checked == false)
                    {
                        if (main.pathOfApk.Text != "" && !main.pathOfApk.Items.Contains(main.pathOfApk.Text))
                        {
                            main.pathOfApk.Items.Insert(0, main.pathOfApk.Text);
                        }
                    }
                    main.tabMainInstance.selectApk(main.pathOfApk.Text);
                    TabMain.Worktype(WorkType.Apk);
                }
                if (ext.Equals(".jar", StringComparison.CurrentCultureIgnoreCase))
                {
                    main.pathOfApk.Text = file.ToString();
                    main.decNameTextBox.Text = Path.GetFileNameWithoutExtension(main.pathOfApk.Text);
                    main.comNameTextBox.Text = Path.GetFileNameWithoutExtension(main.pathOfApk.Text);
                    if (main.disHisBox.Checked == false)
                    {
                        if (main.pathOfApk.Text != "" && !main.pathOfApk.Items.Contains(main.pathOfApk.Text))
                        {
                            main.pathOfApk.Items.Insert(0, main.pathOfApk.Text);
                        }
                    }
                    main.tabMainInstance.selectApk(main.pathOfApk.Text);
                    TabMain.Worktype(WorkType.Jar);
                }
                if (Directory.Exists(file))
                {
                    if (File.Exists(Path.Combine(file, "AndroidManifest.xml")) && File.Exists(Path.Combine(file, "apktool.yml")))
                    {
                        if (main.disHisBox.Checked == false)
                        {
                            Debug.WriteLine(File.Exists(Path.Combine(file, "AndroidManifest.xml")));
                            if (main.pathOfApk.Text != "" && !main.pathOfApk.Items.Contains(main.pathOfApk.Text) && File.Exists(Path.Combine(file, "AndroidManifest.xml")))
                            {
                                main.pathOfApk.Items.Insert(0, main.pathOfApk.Text);
                            }
                        }
                        main.tabMainInstance.selectApk(file);
                        TabMain.Worktype(WorkType.DecompiledApk);
                        main.pathOfApk.Text = file.ToString();
                        main.comNameTextBox.Text = Path.GetFileName(main.pathOfApk.Text);
                    }
                    else if (File.Exists(Path.Combine(file, "Apktool.yml")))
                    {
                        if (main.disHisBox.Checked == false)
                        {
                            if (main.pathOfApk.Text != "" && !main.pathOfApk.Items.Contains(main.pathOfApk.Text) && File.Exists(Path.Combine(file, "apktool.yml")))
                            {
                                main.pathOfApk.Items.Insert(0, main.pathOfApk.Text);
                            }
                        }
                        main.tabMainInstance.selectApk(main.pathOfApk.Text);
                        TabMain.Worktype(WorkType.DecompiledJar);
                        main.pathOfApk.Text = file.ToString();
                        main.comNameTextBox.Text = Path.GetFileName(main.pathOfApk.Text);
                    }
                }
            }
        }

        //// Decompile
        internal static async void decApkBtn_DragDrop(this MainForm main, object sender, DragEventArgs e)
        {
            main.decApkBtn.BackColor = Color.FromArgb(0, 225, 225, 225);
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (ext.Equals(".apk", StringComparison.CurrentCultureIgnoreCase) || ext.Equals(".jar", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (main.apkToolComboBox.Text == "")
                    {
                        MessageBox.Show(Lang.APKTOOL_NOT_SEL_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        main.tMain.SelectedIndex = 4;
                        return;
                    }
                    await Apktool.Decompile(file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));
                }
                if (ext.Equals(".apk", StringComparison.CurrentCultureIgnoreCase) || ext.Equals(".jar", StringComparison.CurrentCultureIgnoreCase))
                {
                    e.Effect = DragDropEffects.Copy;
                }
            }
        }

        //// Compile
        internal static async void comApkBtn_DragDrop(this MainForm main, object sender, DragEventArgs e)
        {
            main.comApkBtn.BackColor = Color.FromArgb(0, 225, 225, 225);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var path in paths)
                {
                    if (main.apkToolComboBox.Text == "")
                    {
                        MessageBox.Show(Lang.APKTOOL_NOT_SEL_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        main.tMain.SelectedIndex = 4;
                        return;
                    }
                    if (File.Exists(Path.Combine(path, "androidmanifest.xml")) && File.Exists(Path.Combine(path, "Apktool.yml")))
                        await Apktool.Compile(path, path + " compiled.apk");
                    else if (File.Exists(Path.Combine(path, "Apktool.yml")))
                        await Apktool.Compile(path, path + " compiled.jar");
                }
            }
        }

        //// Sign APK
        internal static async void signApkBtn_DragDrop(this MainForm main, object sender, DragEventArgs e)
        {
            main.signApkBtn.BackColor = Color.FromArgb(0, 225, 225, 225);
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (ext.Equals(".apk", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (main.overApkChecked.Checked)
                    {
                        await Apktool.Sign(file, file);
                    }
                    else
                    {
                        await Apktool.Sign(file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + " signed.apk"));
                    }
                }
            }
        }

        //// Zipalign
        internal static async void zipAlignBtn_DragDrop(this MainForm main, object sender, DragEventArgs e)
        {
            main.zipAlignBtn.BackColor = Color.FromArgb(0, 225, 225, 225);
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (ext.Equals(".apk", StringComparison.CurrentCultureIgnoreCase))
                {
                    await Apktool.ZipAlign(file, file);
                }
            }
        }

        //// CheckAlign
        internal static async void chkAliBtn_DragDrop(this MainForm main, object sender, DragEventArgs e)
        {
            main.chkAliBtn.BackColor = Color.FromArgb(0, 225, 225, 225);
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (ext.Equals(".apk", StringComparison.CurrentCultureIgnoreCase))
                {
                    await Apktool.CheckAlign(file);
                }
            }
        }

        //// Install APK
        internal static async void installApkBtn_DragDrop(this MainForm main, object sender, DragEventArgs e)
        {
            main.installApkBtn.BackColor = Color.FromArgb(0, 225, 225, 225);
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (ext.Equals(".apk", StringComparison.CurrentCultureIgnoreCase))
                {
                    await Apktool.InstallApk(file);
                }
            }
        }

        //// Extract APK
        internal static async void extractApkBtn_DragDrop(this MainForm main, object sender, DragEventArgs e)
        {
            main.extractApkBtn.BackColor = Color.FromArgb(0, 225, 225, 225);
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (ext.Equals(".apk", StringComparison.CurrentCultureIgnoreCase))
                {
                    await Apktool.ExtractApk(file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));
                }
            }
        }

        //// Zip APK
        internal static async void zipApkBtn_DragDrop(this MainForm main, object sender, DragEventArgs e)
        {
            main.zipApkBtn.BackColor = Color.FromArgb(0, 225, 225, 225);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var path in paths)
                {
                    await Apktool.ZipApk(path, Path.Combine(Path.GetDirectoryName(path), Path.GetFileName(path) + " zipped.apk"));
                }
            }
        }

        //// Install FW
        internal static async void installFwBtn_DragDrop(this MainForm main, object sender, DragEventArgs e)
        {
            main.installFwBtn.BackColor = Color.FromArgb(0, 225, 225, 225);
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (ext.Equals(".apk", StringComparison.CurrentCultureIgnoreCase))
                {
                    await Apktool.Framework(file);
                }
            }
        }

        //// Baksmali-smali
        internal static void tabPage1_DragDrop(this MainForm main, object sender, DragEventArgs e)
        {
            main.tabSmali.BackColor = Color.White;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (ext.Equals(".dex", StringComparison.CurrentCultureIgnoreCase) ||
                    ext.Equals(".odex", StringComparison.CurrentCultureIgnoreCase) ||
                    ext.Equals(".oat", StringComparison.CurrentCultureIgnoreCase))
                {
                    main.baksFile.Text = file;
                    main.baksNameTxtBox.Text = Path.GetFileName(file);
                }
                if (Directory.Exists(file))
                {
                    main.smaliFile.Text = file;
                    main.smaliNameTxtBox.Text = Path.GetFileName(file);
                }
            }
        }

        internal static async void smaliDisBtn_DragDrop(this MainForm main, object sender, DragEventArgs e)
        {
            main.smaliDisBtn.BackColor = Color.FromArgb(0, 225, 225, 225);
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (ext.Equals(".dex", StringComparison.CurrentCultureIgnoreCase) ||
                    ext.Equals(".odex", StringComparison.CurrentCultureIgnoreCase) ||
                    ext.Equals(".oat", StringComparison.CurrentCultureIgnoreCase))
                {
                    await Apktool.Baksmali(file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));
                }
            }
        }

        internal static async void smaliAssBtn_DragDrop(this MainForm main, object sender, DragEventArgs e)
        {
            main.smaliAssBtn.BackColor = Color.FromArgb(0, 225, 225, 225);
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var path = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                if (Directory.Exists(path))
                {
                    await Apktool.Smali(path, Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + " compiled.dex"));
                }
            }
        }
    }
}
