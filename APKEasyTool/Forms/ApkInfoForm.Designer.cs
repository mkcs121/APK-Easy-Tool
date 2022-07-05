namespace APKEasyTool
{
    partial class ApkInfoForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ApkInfoForm));
            this.apkInfoRichTextBox = new System.Windows.Forms.RichTextBox();
            this.saveAsBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // apkInfoRichTextBox
            // 
            this.apkInfoRichTextBox.BackColor = System.Drawing.SystemColors.InfoText;
            this.apkInfoRichTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.apkInfoRichTextBox.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.apkInfoRichTextBox.ForeColor = System.Drawing.SystemColors.Info;
            this.apkInfoRichTextBox.Location = new System.Drawing.Point(0, 0);
            this.apkInfoRichTextBox.Name = "apkInfoRichTextBox";
            this.apkInfoRichTextBox.ReadOnly = true;
            this.apkInfoRichTextBox.Size = new System.Drawing.Size(619, 490);
            this.apkInfoRichTextBox.TabIndex = 0;
            this.apkInfoRichTextBox.Text = "";
            // 
            // saveAsBtn
            // 
            this.saveAsBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.saveAsBtn.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.saveAsBtn.Location = new System.Drawing.Point(451, 455);
            this.saveAsBtn.Name = "saveAsBtn";
            this.saveAsBtn.Size = new System.Drawing.Size(126, 23);
            this.saveAsBtn.TabIndex = 1;
            this.saveAsBtn.Text = "Save as...";
            this.saveAsBtn.UseVisualStyleBackColor = true;
            this.saveAsBtn.Click += new System.EventHandler(this.saveAsBtn_Click);
            // 
            // AIF
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(619, 490);
            this.Controls.Add(this.saveAsBtn);
            this.Controls.Add(this.apkInfoRichTextBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "AIF";
            this.Text = "APK Infomation";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox apkInfoRichTextBox;
        private System.Windows.Forms.Button saveAsBtn;
    }
}