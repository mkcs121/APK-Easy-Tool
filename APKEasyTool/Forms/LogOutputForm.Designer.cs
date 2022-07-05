namespace APKEasyTool.Forms
{
    partial class LogOutputForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LogOutputForm));
            this.richTextBoxLogsStandalone = new System.Windows.Forms.RichTextBox();
            this.clearLogBtn = new System.Windows.Forms.Button();
            this.saveLogBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // richTextBoxLogsStandalone
            // 
            this.richTextBoxLogsStandalone.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxLogsStandalone.BackColor = System.Drawing.Color.Black;
            this.richTextBoxLogsStandalone.Font = new System.Drawing.Font("Consolas", 9.75F);
            this.richTextBoxLogsStandalone.ForeColor = System.Drawing.Color.White;
            this.richTextBoxLogsStandalone.Location = new System.Drawing.Point(5, 5);
            this.richTextBoxLogsStandalone.Name = "richTextBoxLogsStandalone";
            this.richTextBoxLogsStandalone.ReadOnly = true;
            this.richTextBoxLogsStandalone.Size = new System.Drawing.Size(555, 524);
            this.richTextBoxLogsStandalone.TabIndex = 55;
            this.richTextBoxLogsStandalone.Text = "";
            this.richTextBoxLogsStandalone.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.richTextBoxLogs_LinkClicked);
            this.richTextBoxLogsStandalone.TextChanged += new System.EventHandler(this.richTextBoxLogsStandalone_TextChanged);
            // 
            // clearLogBtn
            // 
            this.clearLogBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.clearLogBtn.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.clearLogBtn.Location = new System.Drawing.Point(5, 533);
            this.clearLogBtn.Name = "clearLogBtn";
            this.clearLogBtn.Size = new System.Drawing.Size(120, 25);
            this.clearLogBtn.TabIndex = 60;
            this.clearLogBtn.Text = "Clear log";
            this.clearLogBtn.UseVisualStyleBackColor = true;
            this.clearLogBtn.Click += new System.EventHandler(this.clearLogBtn_Click);
            // 
            // saveLogBtn
            // 
            this.saveLogBtn.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.saveLogBtn.Location = new System.Drawing.Point(442, 534);
            this.saveLogBtn.Name = "saveLogBtn";
            this.saveLogBtn.Size = new System.Drawing.Size(118, 23);
            this.saveLogBtn.TabIndex = 61;
            this.saveLogBtn.Text = "Save log";
            this.saveLogBtn.UseVisualStyleBackColor = true;
            this.saveLogBtn.Click += new System.EventHandler(this.saveLogBtn_Click);
            // 
            // LogOutputForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(565, 566);
            this.Controls.Add(this.saveLogBtn);
            this.Controls.Add(this.richTextBoxLogsStandalone);
            this.Controls.Add(this.clearLogBtn);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LogOutputForm";
            this.Text = "Log";
            this.Load += new System.EventHandler(this.Log_Load);
            this.ResumeLayout(false);

        }

        #endregion
        public System.Windows.Forms.RichTextBox richTextBoxLogsStandalone;
        internal System.Windows.Forms.Button clearLogBtn;
        internal System.Windows.Forms.Button saveLogBtn;
    }
}