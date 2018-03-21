namespace OrangeCountyCA_Scraper
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
            this.tbName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btn_Search = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.lb_logs = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.lb_filePath = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tbStartDate = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbEndDate = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // tbName
            // 
            this.tbName.Location = new System.Drawing.Point(93, 12);
            this.tbName.Name = "tbName";
            this.tbName.Size = new System.Drawing.Size(148, 20);
            this.tbName.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Name:";
            // 
            // btn_Search
            // 
            this.btn_Search.Location = new System.Drawing.Point(359, 12);
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
            this.label2.Location = new System.Drawing.Point(12, 229);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(33, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Logs:";
            // 
            // lb_logs
            // 
            this.lb_logs.Location = new System.Drawing.Point(15, 256);
            this.lb_logs.Name = "lb_logs";
            this.lb_logs.Size = new System.Drawing.Size(419, 285);
            this.lb_logs.TabIndex = 5;
            this.lb_logs.Text = "";
            this.lb_logs.TextChanged += new System.EventHandler(this.lb_logs_TextChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(93, 198);
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
            this.label3.Location = new System.Drawing.Point(12, 177);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(82, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Output file path:";
            // 
            // lb_filePath
            // 
            this.lb_filePath.AutoSize = true;
            this.lb_filePath.Location = new System.Drawing.Point(90, 177);
            this.lb_filePath.Name = "lb_filePath";
            this.lb_filePath.Size = new System.Drawing.Size(0, 13);
            this.lb_filePath.TabIndex = 8;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 53);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(58, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Start Date:";
            // 
            // tbStartDate
            // 
            this.tbStartDate.Location = new System.Drawing.Point(93, 53);
            this.tbStartDate.Name = "tbStartDate";
            this.tbStartDate.Size = new System.Drawing.Size(148, 20);
            this.tbStartDate.TabIndex = 9;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 100);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(53, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "End date:";
            // 
            // tbEndDate
            // 
            this.tbEndDate.Location = new System.Drawing.Point(93, 100);
            this.tbEndDate.Name = "tbEndDate";
            this.tbEndDate.Size = new System.Drawing.Size(148, 20);
            this.tbEndDate.TabIndex = 11;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(446, 553);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.tbEndDate);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.tbStartDate);
            this.Controls.Add(this.lb_filePath);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.lb_logs);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btn_Search);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbName);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btn_Search;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox lb_logs;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lb_filePath;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbStartDate;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbEndDate;
    }
}

