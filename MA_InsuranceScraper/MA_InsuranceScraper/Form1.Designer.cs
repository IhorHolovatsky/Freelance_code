namespace MA_InsuranceScraper
{
    partial class Form1
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
            this.tb_licenseName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btn_Search = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.lb_logs = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.lb_filePath = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lbStates = new System.Windows.Forms.ListBox();
            this.lbLines = new System.Windows.Forms.ListBox();
            this.cbDuplicates = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // tb_licenseName
            // 
            this.tb_licenseName.Location = new System.Drawing.Point(93, 33);
            this.tb_licenseName.Name = "tb_licenseName";
            this.tb_licenseName.Size = new System.Drawing.Size(148, 20);
            this.tb_licenseName.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 33);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Last Name:";
            // 
            // btn_Search
            // 
            this.btn_Search.Location = new System.Drawing.Point(404, 12);
            this.btn_Search.Name = "btn_Search";
            this.btn_Search.Size = new System.Drawing.Size(75, 23);
            this.btn_Search.TabIndex = 2;
            this.btn_Search.Text = "Search";
            this.btn_Search.UseVisualStyleBackColor = true;
            this.btn_Search.Click += new System.EventHandler(this.btn_Search_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 250);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(33, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Logs:";
            // 
            // lb_logs
            // 
            this.lb_logs.Location = new System.Drawing.Point(12, 266);
            this.lb_logs.Name = "lb_logs";
            this.lb_logs.Size = new System.Drawing.Size(467, 215);
            this.lb_logs.TabIndex = 5;
            this.lb_logs.Text = "";
            this.lb_logs.TextChanged += new System.EventHandler(this.lb_logs_TextChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(93, 211);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 6;
            this.button1.Text = "Change";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 190);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(82, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Output file path:";
            // 
            // lb_filePath
            // 
            this.lb_filePath.AutoSize = true;
            this.lb_filePath.Location = new System.Drawing.Point(90, 190);
            this.lb_filePath.Name = "lb_filePath";
            this.lb_filePath.Size = new System.Drawing.Size(0, 13);
            this.lb_filePath.TabIndex = 8;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 75);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(40, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "States:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(243, 75);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(82, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Lines Insurance";
            // 
            // lbStates
            // 
            this.lbStates.FormattingEnabled = true;
            this.lbStates.Location = new System.Drawing.Point(89, 75);
            this.lbStates.Name = "lbStates";
            this.lbStates.Size = new System.Drawing.Size(148, 95);
            this.lbStates.TabIndex = 13;
            // 
            // lbLines
            // 
            this.lbLines.FormattingEnabled = true;
            this.lbLines.Location = new System.Drawing.Point(331, 75);
            this.lbLines.Name = "lbLines";
            this.lbLines.Size = new System.Drawing.Size(148, 95);
            this.lbLines.TabIndex = 14;
            // 
            // cbDuplicates
            // 
            this.cbDuplicates.AutoSize = true;
            this.cbDuplicates.Location = new System.Drawing.Point(246, 190);
            this.cbDuplicates.Name = "cbDuplicates";
            this.cbDuplicates.Size = new System.Drawing.Size(117, 17);
            this.cbDuplicates.TabIndex = 15;
            this.cbDuplicates.Text = "Exclude Duplicates";
            this.cbDuplicates.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(328, 59);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(81, 13);
            this.label6.TabIndex = 16;
            this.label6.Text = "Here is OR filter";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(491, 493);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.cbDuplicates);
            this.Controls.Add(this.lbLines);
            this.Controls.Add(this.lbStates);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.lb_filePath);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.lb_logs);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btn_Search);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tb_licenseName);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tb_licenseName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btn_Search;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox lb_logs;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lb_filePath;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ListBox lbStates;
        private System.Windows.Forms.ListBox lbLines;
        private System.Windows.Forms.CheckBox cbDuplicates;
        private System.Windows.Forms.Label label6;
    }
}

