namespace Package_Generator_Service
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            headerPanel = new Panel();
            subtitleLabel = new Label();
            titleLabel = new Label();
            cardPanel = new Panel();
            statusCaptionLabel = new Label();
            label1 = new Label();
            label2 = new Label();
            button1 = new Button();
            checkBox1 = new CheckBox();
            progressBar1 = new ProgressBar();
            logHeaderLabel = new Label();
            richTextBox1 = new RichTextBox();
            footerPanel = new Panel();
            footerLabel = new Label();
            headerPanel.SuspendLayout();
            cardPanel.SuspendLayout();
            footerPanel.SuspendLayout();
            SuspendLayout();
            //
            // headerPanel
            //
            headerPanel.BackColor = Color.FromArgb(27, 42, 74);
            headerPanel.Controls.Add(subtitleLabel);
            headerPanel.Controls.Add(titleLabel);
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Location = new Point(0, 0);
            headerPanel.Name = "headerPanel";
            headerPanel.Size = new Size(940, 84);
            headerPanel.TabIndex = 0;
            headerPanel.Paint += headerPanel_Paint;
            //
            // subtitleLabel
            //
            subtitleLabel.AutoSize = true;
            subtitleLabel.BackColor = Color.Transparent;
            subtitleLabel.Font = new Font("Segoe UI", 9.75F);
            subtitleLabel.ForeColor = Color.FromArgb(157, 176, 204);
            subtitleLabel.Location = new Point(28, 50);
            subtitleLabel.Name = "subtitleLabel";
            subtitleLabel.Size = new Size(279, 17);
            subtitleLabel.TabIndex = 1;
            subtitleLabel.Text = "DDEX / Metadata Package Automation Service";
            //
            // titleLabel
            //
            titleLabel.AutoSize = true;
            titleLabel.BackColor = Color.Transparent;
            titleLabel.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;
            titleLabel.Location = new Point(25, 14);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(307, 32);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "Package Generator Service";
            //
            // cardPanel
            //
            cardPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cardPanel.BackColor = Color.White;
            cardPanel.BorderStyle = BorderStyle.FixedSingle;
            cardPanel.Controls.Add(statusCaptionLabel);
            cardPanel.Controls.Add(label1);
            cardPanel.Controls.Add(label2);
            cardPanel.Controls.Add(button1);
            cardPanel.Controls.Add(checkBox1);
            cardPanel.Controls.Add(progressBar1);
            cardPanel.Location = new Point(20, 100);
            cardPanel.Name = "cardPanel";
            cardPanel.Size = new Size(900, 140);
            cardPanel.TabIndex = 1;
            //
            // statusCaptionLabel
            //
            statusCaptionLabel.AutoSize = true;
            statusCaptionLabel.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            statusCaptionLabel.ForeColor = Color.FromArgb(138, 151, 172);
            statusCaptionLabel.Location = new Point(22, 16);
            statusCaptionLabel.Name = "statusCaptionLabel";
            statusCaptionLabel.Size = new Size(118, 13);
            statusCaptionLabel.TabIndex = 0;
            statusCaptionLabel.Text = "CURRENT PACKAGE";
            //
            // label1
            //
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 16F);
            label1.ForeColor = Color.FromArgb(43, 58, 85);
            label1.Location = new Point(22, 40);
            label1.Name = "label1";
            label1.Size = new Size(155, 30);
            label1.TabIndex = 1;
            label1.Text = "Current Pkg is :";
            //
            // label2
            //
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            label2.ForeColor = Color.FromArgb(27, 138, 90);
            label2.Location = new Point(185, 40);
            label2.Name = "label2";
            label2.Size = new Size(0, 30);
            label2.TabIndex = 2;
            //
            // button1
            //
            button1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            button1.BackColor = Color.FromArgb(45, 108, 223);
            button1.FlatAppearance.BorderSize = 0;
            button1.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 84, 184);
            button1.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 124, 235);
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            button1.ForeColor = Color.White;
            button1.Location = new Point(722, 16);
            button1.Name = "button1";
            button1.Size = new Size(154, 42);
            button1.TabIndex = 0;
            button1.Text = "⟳  Refresh";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            //
            // checkBox1
            //
            checkBox1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            checkBox1.AutoSize = true;
            checkBox1.Font = new Font("Segoe UI", 10F);
            checkBox1.ForeColor = Color.FromArgb(43, 58, 85);
            checkBox1.Location = new Point(740, 70);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(126, 23);
            checkBox1.TabIndex = 3;
            checkBox1.Text = "Auto Generate";
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            //
            // progressBar1
            //
            progressBar1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar1.Location = new Point(22, 100);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(680, 20);
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.TabIndex = 4;
            //
            // logHeaderLabel
            //
            logHeaderLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            logHeaderLabel.AutoSize = true;
            logHeaderLabel.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            logHeaderLabel.ForeColor = Color.FromArgb(43, 58, 85);
            logHeaderLabel.Location = new Point(22, 252);
            logHeaderLabel.Name = "logHeaderLabel";
            logHeaderLabel.Size = new Size(91, 19);
            logHeaderLabel.TabIndex = 2;
            logHeaderLabel.Text = "Activity Log";
            //
            // richTextBox1
            //
            richTextBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            richTextBox1.BackColor = Color.FromArgb(24, 28, 38);
            richTextBox1.BorderStyle = BorderStyle.None;
            richTextBox1.Font = new Font("Consolas", 10F);
            richTextBox1.ForeColor = Color.FromArgb(220, 224, 232);
            richTextBox1.Location = new Point(20, 278);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.Size = new Size(900, 332);
            richTextBox1.TabIndex = 5;
            richTextBox1.Text = "";
            //
            // footerPanel
            //
            footerPanel.BackColor = Color.FromArgb(27, 42, 74);
            footerPanel.Controls.Add(footerLabel);
            footerPanel.Dock = DockStyle.Bottom;
            footerPanel.Location = new Point(0, 632);
            footerPanel.Name = "footerPanel";
            footerPanel.Size = new Size(940, 28);
            footerPanel.TabIndex = 3;
            //
            // footerLabel
            //
            footerLabel.AutoSize = true;
            footerLabel.BackColor = Color.Transparent;
            footerLabel.Font = new Font("Segoe UI", 8.25F);
            footerLabel.ForeColor = Color.FromArgb(157, 176, 204);
            footerLabel.Location = new Point(12, 6);
            footerLabel.Name = "footerLabel";
            footerLabel.Size = new Size(258, 13);
            footerLabel.TabIndex = 0;
            footerLabel.Text = "© Mazzika Group  •  Package Generator Service";
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(238, 241, 246);
            ClientSize = new Size(940, 660);
            Controls.Add(richTextBox1);
            Controls.Add(logHeaderLabel);
            Controls.Add(cardPanel);
            Controls.Add(footerPanel);
            Controls.Add(headerPanel);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimumSize = new Size(720, 520);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Package-Generator";
            Load += Form1_Load;
            headerPanel.ResumeLayout(false);
            headerPanel.PerformLayout();
            cardPanel.ResumeLayout(false);
            cardPanel.PerformLayout();
            footerPanel.ResumeLayout(false);
            footerPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel headerPanel;
        private Label titleLabel;
        private Label subtitleLabel;
        private Panel cardPanel;
        private Label statusCaptionLabel;
        private Label logHeaderLabel;
        private Panel footerPanel;
        private Label footerLabel;
        private Button button1;
        private RichTextBox richTextBox1;
        private CheckBox checkBox1;
        private ProgressBar progressBar1;
        private Label label1;
        private Label label2;
    }
}
