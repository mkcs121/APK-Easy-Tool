using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APKEasyTool
{
    public class TabSmali
    {
        private static MainForm main;

        public TabSmali(MainForm Main)
        {
            main = Main;
        }

        internal void smaliDisBtn_Click(object sender, EventArgs e)
        {
            if(main.baksFile.Text == "")
            {
                MessageBox.Show(Lang.DEX_NOT_SEL_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (main.baksDir.Text == "")
            {
                MessageBox.Show(Lang.DIR_NOT_SET_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (main.baksNameTxtBox.Text == "")
            {
                MessageBox.Show(Lang.PLZ_ENTER_DIS_NAME_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            main.smaliFile.Text = main.baksDir.Text + "\\" + main.baksNameTxtBox.Text;
            main.smaliNameTxtBox.Text = main.baksNameTxtBox.Text;
            _ = Apktool.Baksmali(main.baksFile.Text, Path.Combine(main.baksDir.Text , main.baksNameTxtBox.Text));
        }

        internal void smaliAssBtn_Click(object sender, EventArgs e)
        {
            if (main.smaliFile.Text == "")
            {
                MessageBox.Show(Lang.DEX_NOT_SEL_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (main.smaliDir.Text == "")
            {
                MessageBox.Show(Lang.DIR_NOT_SET_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (main.smaliNameTxtBox.Text == "")
            {
                MessageBox.Show(Lang.PLZ_ENTER_SMA_NAME_MBOX, Program.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _ = Apktool.Smali(main.smaliFile.Text, Path.Combine(main.smaliDir.Text, main.smaliNameTxtBox.Text));
        }
    }
}
