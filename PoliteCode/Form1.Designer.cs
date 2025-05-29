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
            this.btnInbox = new System.Windows.Forms.Button();
            this.runCodeBtn = new System.Windows.Forms.Button();
            this.outputTextBox = new System.Windows.Forms.TextBox();
            this.clearConsoleBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // input
            // 
            this.input.Font = new System.Drawing.Font("Consolas", 14F);
            this.input.Location = new System.Drawing.Point(12, 50);
            this.input.Multiline = true;
            this.input.Name = "input";
            this.input.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.input.Size = new System.Drawing.Size(420, 614);
            this.input.TabIndex = 0;
            // 
            // writeCode
            // 
            this.writeCode.Font = new System.Drawing.Font("Consolas", 14F);
            this.writeCode.Location = new System.Drawing.Point(50, 670);
            this.writeCode.Name = "writeCode";
            this.writeCode.Size = new System.Drawing.Size(350, 50);
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
            this.CodeC.Location = new System.Drawing.Point(440, 50);
            this.CodeC.Multiline = true;
            this.CodeC.Name = "CodeC";
            this.CodeC.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.CodeC.Size = new System.Drawing.Size(420, 614);
            this.CodeC.TabIndex = 4;
            // 
            // btnInbox
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
            // runCodeBtn
            // 
            this.runCodeBtn.Font = new System.Drawing.Font("Consolas", 14F);
            this.runCodeBtn.Location = new System.Drawing.Point(520, 670);
            this.runCodeBtn.Name = "runCodeBtn";
            this.runCodeBtn.Size = new System.Drawing.Size(200, 50);
            this.runCodeBtn.TabIndex = 6;
            this.runCodeBtn.Text = "run code";
            this.runCodeBtn.UseVisualStyleBackColor = true;
            this.runCodeBtn.Click += new System.EventHandler(this.runCodeBtn_Click);
            // 
            // outputTextBox
            // 
            this.outputTextBox.BackColor = System.Drawing.Color.Black;
            this.outputTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.outputTextBox.Font = new System.Drawing.Font("Consolas", 10F);
            this.outputTextBox.ForeColor = System.Drawing.Color.LimeGreen;
            this.outputTextBox.Location = new System.Drawing.Point(870, 50);
            this.outputTextBox.Multiline = true;
            this.outputTextBox.Name = "outputTextBox";
            this.outputTextBox.ReadOnly = true;
            this.outputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.outputTextBox.Size = new System.Drawing.Size(350, 614);
            this.outputTextBox.TabIndex = 7;
            this.outputTextBox.Text = "🖥️ Console Output\r\n───────────────────────────────────\r\n";
            this.outputTextBox.TextChanged += new System.EventHandler(this.outputTextBox_TextChanged);
            // 
            // clearConsoleBtn
            // 
            this.clearConsoleBtn.BackColor = System.Drawing.Color.DarkSlateGray;
            this.clearConsoleBtn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.clearConsoleBtn.Font = new System.Drawing.Font("Consolas", 10F);
            this.clearConsoleBtn.ForeColor = System.Drawing.Color.White;
            this.clearConsoleBtn.Location = new System.Drawing.Point(1150, 12);
            this.clearConsoleBtn.Name = "clearConsoleBtn";
            this.clearConsoleBtn.Size = new System.Drawing.Size(70, 30);
            this.clearConsoleBtn.TabIndex = 8;
            this.clearConsoleBtn.Text = "Clear";
            this.clearConsoleBtn.UseVisualStyleBackColor = false;
            this.clearConsoleBtn.Click += new System.EventHandler(this.clearConsoleBtn_Click);
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(1240, 750);
            this.Controls.Add(this.clearConsoleBtn);
            this.Controls.Add(this.outputTextBox);
            this.Controls.Add(this.runCodeBtn);
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
        private System.Windows.Forms.Button runCodeBtn;
        private System.Windows.Forms.TextBox outputTextBox; // הצהרה על הקונסולה
        private System.Windows.Forms.Button clearConsoleBtn; // הצהרה על כפתור Clear
    }
}