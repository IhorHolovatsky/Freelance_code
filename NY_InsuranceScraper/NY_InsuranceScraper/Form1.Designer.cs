﻿namespace NY_InsuranceScraper
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
            this.tbLastName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btn_Search = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.lb_logs = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.lb_filePath = new System.Windows.Forms.Label();
            this.lbPercentDone = new System.Windows.Forms.Label();
            this.pbCaptcha = new System.Windows.Forms.PictureBox();
            this.tbCaptcha = new System.Windows.Forms.TextBox();
            this.btnSubmitCaptcha = new System.Windows.Forms.Button();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            ((System.ComponentModel.ISupportInitialize)(this.pbCaptcha)).BeginInit();
            this.SuspendLayout();
            // 
            // tbLastName
            // 
            this.tbLastName.Location = new System.Drawing.Point(109, 51);
            this.tbLastName.Name = "tbLastName";
            this.tbLastName.Size = new System.Drawing.Size(148, 20);
            this.tbLastName.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 51);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Last Name:";
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
            this.label2.Location = new System.Drawing.Point(12, 191);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(33, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Logs:";
            // 
            // lb_logs
            // 
            this.lb_logs.Location = new System.Drawing.Point(15, 207);
            this.lb_logs.Name = "lb_logs";
            this.lb_logs.Size = new System.Drawing.Size(419, 285);
            this.lb_logs.TabIndex = 5;
            this.lb_logs.Text = "";
            this.lb_logs.TextChanged += new System.EventHandler(this.lb_logs_TextChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(93, 151);
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
            this.label3.Location = new System.Drawing.Point(12, 130);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(82, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Output file path:";
            // 
            // lb_filePath
            // 
            this.lb_filePath.AutoSize = true;
            this.lb_filePath.Location = new System.Drawing.Point(90, 130);
            this.lb_filePath.Name = "lb_filePath";
            this.lb_filePath.Size = new System.Drawing.Size(0, 13);
            this.lb_filePath.TabIndex = 8;
            // 
            // lbPercentDone
            // 
            this.lbPercentDone.AutoSize = true;
            this.lbPercentDone.Location = new System.Drawing.Point(401, 191);
            this.lbPercentDone.Name = "lbPercentDone";
            this.lbPercentDone.Size = new System.Drawing.Size(0, 13);
            this.lbPercentDone.TabIndex = 13;
            // 
            // pbCaptcha
            // 
            this.pbCaptcha.Location = new System.Drawing.Point(286, 51);
            this.pbCaptcha.Name = "pbCaptcha";
            this.pbCaptcha.Size = new System.Drawing.Size(148, 92);
            this.pbCaptcha.TabIndex = 14;
            this.pbCaptcha.TabStop = false;
            // 
            // tbCaptcha
            // 
            this.tbCaptcha.Location = new System.Drawing.Point(286, 151);
            this.tbCaptcha.Name = "tbCaptcha";
            this.tbCaptcha.Size = new System.Drawing.Size(101, 20);
            this.tbCaptcha.TabIndex = 15;
            // 
            // btnSubmitCaptcha
            // 
            this.btnSubmitCaptcha.Location = new System.Drawing.Point(393, 149);
            this.btnSubmitCaptcha.Name = "btnSubmitCaptcha";
            this.btnSubmitCaptcha.Size = new System.Drawing.Size(41, 23);
            this.btnSubmitCaptcha.TabIndex = 16;
            this.btnSubmitCaptcha.Text = "OK";
            this.btnSubmitCaptcha.UseVisualStyleBackColor = true;
            this.btnSubmitCaptcha.Click += new System.EventHandler(this.btnSubmitCaptcha_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(446, 504);
            this.Controls.Add(this.btnSubmitCaptcha);
            this.Controls.Add(this.tbCaptcha);
            this.Controls.Add(this.pbCaptcha);
            this.Controls.Add(this.lbPercentDone);
            this.Controls.Add(this.lb_filePath);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.lb_logs);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btn_Search);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbLastName);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.pbCaptcha)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbLastName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btn_Search;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox lb_logs;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lb_filePath;
        private System.Windows.Forms.Label lbPercentDone;
        private System.Windows.Forms.PictureBox pbCaptcha;
        private System.Windows.Forms.TextBox tbCaptcha;
        private System.Windows.Forms.Button btnSubmitCaptcha;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
    }
}

