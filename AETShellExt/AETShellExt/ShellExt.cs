using SharpShell.Attributes;
using SharpShell.SharpContextMenu;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AETShellExt
{
    [ComVisible(true)]
    [COMServerAssociation(AssociationType.Directory)]
    [COMServerAssociation(AssociationType.DirectoryBackground)]
    [COMServerAssociation(AssociationType.AllFiles)]
    public class ShellExt : SharpContextMenu
    {
        public static readonly string realpath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";
        string path;
        string exefile;

        ContextMenuStrip menu = new ContextMenuStrip();

        protected override bool CanShowMenu()
        {
            ////  We can show the item only for a single selection.
            if (SelectedItemPaths.Count() == 1)
            {
                UpdateMenu();
                return true;
            }
            else
            {
                return false;
            }
        }

        // <summary>
        // Updates the context menu. 
        // </summary>
        private void UpdateMenu()
        {
            // release all resources associated to existing menu
            menu.Dispose();
            menu = CreateMenu();
        }

        protected override ContextMenuStrip CreateMenu()
        {
            //menu.Items.Clear();

            var menu = new ContextMenuStrip();

            FileAttributes attr = File.GetAttributes(SelectedItemPaths.First());

            // check if the selected item is a directory
            if (attr.HasFlag(FileAttributes.Directory))
            {
                foreach (var filePath in SelectedItemPaths)
                {
                    path = filePath;

                    menu.Items.Add(FolderMenu());
                }
            }
            else
            {
                foreach (var filePath in SelectedItemPaths)
                {
                    var ext = Path.GetExtension(filePath);
                    path = filePath;
                    if (ext.Equals(".apk", StringComparison.CurrentCultureIgnoreCase) || ext.Equals(".jar", StringComparison.CurrentCultureIgnoreCase) || ext.Equals(".dex", StringComparison.CurrentCultureIgnoreCase))
                    {
                        menu.Items.Add(ApkMenu());
                    }
                    // return the menu item  
                }
            }
            return menu;
        }

        protected ToolStripMenuItem FolderMenu()
        {
            var MainMenu = new ToolStripMenuItem
            {
                Text = "APK Easy Tool",
                Image = Properties.Resources.rsz_apkeasytoolicon
            };

            var subItem6 = new ToolStripMenuItem
            {
                Text = "Open with APK Easy Tool",
                Image = Properties.Resources.rsz_apkeasytoolicon
            };
            MainMenu.DropDownItems.Add(subItem6);

            if (File.Exists(Path.Combine(path, "AndroidManifest.xml")) || File.Exists(Path.Combine(path, "apktool.yml")))
            {
                var subItem2 = new ToolStripMenuItem
                {
                    Text = "Compile APK/JAR with APK Easy Tool",
                    Image = Properties.Resources.rsz_comsmall
                };

                subItem2.Click += (sender, args) => CMD("-c");
                MainMenu.DropDownItems.Add(subItem2);
            }
            var subItem3 = new ToolStripMenuItem
            {
                Text = "Compile DEX with APK Easy Tool",
                Image = Properties.Resources.dexcom
            };

            subItem6.Click += (sender, args) => CMD("");
            subItem3.Click += (sender, args) => CMD("-sma");

            MainMenu.DropDownItems.Add(subItem3);
            //menu.Items.Clear();
            return MainMenu;
        }

        protected ToolStripMenuItem ApkMenu()
        {
            var MainMenu = new ToolStripMenuItem
            {
                Text = "APK Easy Tool",
                Image = Properties.Resources.rsz_apkeasytoolicon
            };

            //  Create the menu strip.
            //var menu = new ContextMenuStrip();

            if (Path.GetExtension(path) == ".apk")
            {
                //  Create a 'count lines' item.
                var decItem = new ToolStripMenuItem
                {
                    Text = "Decompile with APK Easy Tool",
                    Image = Properties.Resources.rsz_decsmall
                };

                var zipAlignItem = new ToolStripMenuItem
                {
                    Text = "Zipalign with APK Easy Tool",
                    Image = Properties.Resources.rsz_zipalign
                };

                var signItem = new ToolStripMenuItem
                {
                    Text = "Sign with APK Easy Tool",
                    Image = Properties.Resources.rsz_signapksmall
                };

                var installItem = new ToolStripMenuItem
                {
                    Text = "Install with APK Easy Tool",
                    Image = Properties.Resources.rsz_installsmall
                };

                var openItem = new ToolStripMenuItem
                {
                    Text = "Open with APK Easy Tool",
                    Image = Properties.Resources.rsz_apkeasytoolicon
                };

                var checkAlignItem = new ToolStripMenuItem
                {
                    Text = "Check alignment with APK Easy Tool",
                    Image = Properties.Resources.rsz_5chkalignsmall
                };

                //  When we click, we'll call the function.
                decItem.Click += (sender, args) => CMD("-d");
                zipAlignItem.Click += (sender, args) => CMD("-a");
                signItem.Click += (sender, args) => CMD("-s");
                installItem.Click += (sender, args) => CMD("-i");
                openItem.Click += (sender, args) => CMD("");
                checkAlignItem.Click += (sender, args) => CMD("-ca");

                MainMenu.DropDownItems.Add(openItem);
                MainMenu.DropDownItems.Add(decItem);
                MainMenu.DropDownItems.Add(zipAlignItem);
                MainMenu.DropDownItems.Add(signItem);
                MainMenu.DropDownItems.Add(installItem);
                MainMenu.DropDownItems.Add(checkAlignItem);
            }
            else if (Path.GetExtension(path) == ".dex")
            {
                //  Create a 'count lines' item.
                var decDexItem = new ToolStripMenuItem
                {
                    Text = "Decompile with APK Easy Tool",
                    Image = Properties.Resources.dex
                };

                //  When we click, we'll call the function.
                decDexItem.Click += (sender, args) => CMD("-bak");

                MainMenu.DropDownItems.Add(decDexItem);
            }
            else if (Path.GetExtension(path) == ".jar")
            {
                //  Create a 'count lines' item.
                var decJarItem = new ToolStripMenuItem
                {
                    Text = "Decompile with APK Easy Tool",
                    Image = Properties.Resources.java
                };
                //  When we click, we'll call the function.
                decJarItem.Click += (sender, args) => CMD("-d");

                MainMenu.DropDownItems.Add(decJarItem);
            }

            //  Add the item to the context menu.

            //menu.Items.Clear();
            return MainMenu;
        }

        private void CMD(string arg)
        {
            if (File.Exists(realpath + "apkeasytool.exe"))
            {
                exefile = "apkeasytool.exe";
            }
            else
            {
                MessageBox.Show("apkeasytool.exe is missing. If you moved the exe, reinstall the shell extension on APK Easy Tool", "APK Easy Tool", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Process process = new Process();
            process.StartInfo.FileName = realpath + exefile;
            process.StartInfo.Arguments = arg + " \"" + path + "\"";
            process.Start();
            //process.WaitForExit(500); // Waits here for the process to exit.
        }
    }
}
