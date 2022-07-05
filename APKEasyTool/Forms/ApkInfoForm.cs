using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APKEasyTool
{
    public partial class ApkInfoForm : Form
    {
        private static MainForm main;

        public ApkInfoForm(MainForm Main)
        {
            main = Main;
        }

        public ApkInfoForm()
        {
            InitializeComponent();
            Text = Lang.Localize("apk_info_title", Text);
            saveAsBtn.Text = Lang.Localize("save_as_btn", saveAsBtn.Text);

            apkInfoRichTextBox.Text = TabMain.apkinfo;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(main.Location.X + 30, main.Location.Y + 30);

            ContextMenu contextMenu = new ContextMenu();
            MenuItem menuItem = new MenuItem();
            menuItem = new MenuItem("Copy");
            menuItem.Click += new EventHandler(CopyAction);
            contextMenu.MenuItems.Add(menuItem);
            apkInfoRichTextBox.ContextMenu = contextMenu;
        }

        void CopyAction(object sender, EventArgs e)
        {
            if (apkInfoRichTextBox.Text != "")
            {
                Clipboard.SetText(apkInfoRichTextBox.SelectedText);
            }
        }

        private void saveAsBtn_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = Lang.SAVE_FILE_FILTER = " (*.txt)|*.txt";
                sfd.FilterIndex = 2;

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(sfd.FileName, apkInfoRichTextBox.Text);
                }
            }
        }
    }
}
