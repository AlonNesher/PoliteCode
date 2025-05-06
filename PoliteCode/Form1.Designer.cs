using System;
using System.Drawing;
using System.Windows.Forms;

namespace PoliteCode
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
            this.components = new System.ComponentModel.Container();
            this.input = new System.Windows.Forms.TextBox();
            this.writeCode = new System.Windows.Forms.Button();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.CodeC = new System.Windows.Forms.TextBox();
            this.btnInbox = new System.Windows.Forms.Button(); // כפתור Inbox
            this.runCodeBtn = new System.Windows.Forms.Button(); // כפתור Run Code חדש
            this.SuspendLayout();
            // 
            // input
            // 
            this.input.Font = new System.Drawing.Font("Consolas", 14F);
            this.input.Location = new System.Drawing.Point(12, 50);
            this.input.Multiline = true;
            this.input.Name = "input";
            this.input.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.input.Size = new System.Drawing.Size(532, 614);
            this.input.TabIndex = 0;
            // 
            // writeCode
            // 
            this.writeCode.Font = new System.Drawing.Font("Consolas", 14F);
            this.writeCode.Location = new System.Drawing.Point(100, 670);
            this.writeCode.Name = "writeCode";
            this.writeCode.Size = new System.Drawing.Size(400, 60);
            this.writeCode.TabIndex = 1;
            this.writeCode.Text = "Translate to C#";
            this.writeCode.UseVisualStyleBackColor = true;
            this.writeCode.Click += new System.EventHandler(this.button1_Click);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // CodeC
            // 
            this.CodeC.Font = new System.Drawing.Font("Consolas", 14F);
            this.CodeC.Location = new System.Drawing.Point(550, 50);
            this.CodeC.Multiline = true;
            this.CodeC.Name = "CodeC";
            this.CodeC.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.CodeC.Size = new System.Drawing.Size(500, 680);
            this.CodeC.TabIndex = 4;
            // 
            // btnInbox - כפתור אינבוקס!
            // 
            this.btnInbox.Font = new System.Drawing.Font("Consolas", 12F, System.Drawing.FontStyle.Bold);
            this.btnInbox.Location = new System.Drawing.Point(12, 12);
            this.btnInbox.Name = "btnInbox";
            this.btnInbox.Size = new System.Drawing.Size(177, 35);
            this.btnInbox.TabIndex = 5;
            this.btnInbox.Text = "inbox";
            this.btnInbox.UseVisualStyleBackColor = true;
            this.btnInbox.Click += new System.EventHandler(this.btnInbox_Click);
            // 
            // runCodeBtn - כפתור Run Code חדש!
            // 
            this.runCodeBtn.Font = new System.Drawing.Font("Consolas", 14F);
            this.runCodeBtn.Location = new System.Drawing.Point(700, 740);
            this.runCodeBtn.Name = "runCodeBtn";
            this.runCodeBtn.Size = new System.Drawing.Size(200, 50);
            this.runCodeBtn.TabIndex = 6;
            this.runCodeBtn.Text = "run code";
            this.runCodeBtn.UseVisualStyleBackColor = true;
            this.runCodeBtn.Click += new System.EventHandler(this.runCodeBtn_Click);
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(1200, 800);
            this.Controls.Add(this.runCodeBtn); // הוסף את הכפתור החדש לטופס
            this.Controls.Add(this.btnInbox);
            this.Controls.Add(this.CodeC);
            this.Controls.Add(this.writeCode);
            this.Controls.Add(this.input);
            this.Name = "Form1";
            this.Text = "Polite Code Translator";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        #endregion

        private System.Windows.Forms.TextBox input;
        private System.Windows.Forms.Button writeCode;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.TextBox CodeC;
        private System.Windows.Forms.Button btnInbox;
        private System.Windows.Forms.Button runCodeBtn; // הצהרה על הכפתור החדש
    }
}