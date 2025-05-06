using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace PoliteCode
{
    public partial class Form1 : Form
    {
        // רכיבי ליבה
        private Tokenizer _tokenizer;
        private CodeGenerator _codeGenerator;
        private PoliteCodeTools _tools;
        private Parser _parser;

        /// <summary>
        /// בנאי המחלקה הראשית
        /// </summary>
        public Form1()
        {
            InitializeComponent();
            InitializeComponents();
        }

        /// <summary>
        /// אתחול רכיבי המערכת
        /// </summary>
        private void InitializeComponents()
        {
            // יצירת מופעים של הרכיבים
            _tokenizer = new Tokenizer();
            _tools = new PoliteCodeTools();
            _codeGenerator = new CodeGenerator(_tokenizer);
            _parser = new Parser(_tokenizer, _codeGenerator, _tools);
        }

        /// <summary>
        /// אירוע לחיצה על כפתור התרגום
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // איפוס מצב המפענח
                _parser.Reset();

                // פיצול הקלט לשורות
                string[] inputLines = input.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (!_parser.SetInput(inputLines))
                {
                    _tools.ShowInfo("No code to process.");
                    return;
                }

                // עיבוד הקוד
                if (_parser.ProcessCode())
                {
                    // הצגת הקוד הסופי
                    ShowFinalCode();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}\n\n{ex.StackTrace}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// הצגת הקוד המתורגם
        /// </summary>
        private void ShowFinalCode()
        {
            CodeC.Text = _parser.GetCSharpCode();
        }

        /// <summary>
        /// טיפול באירוע טעינת הטופס
        /// </summary>
        private void Form1_Load(object sender, EventArgs e)
        {
            // אתחול נוסף בעת טעינת הטופס אם נדרש
        }

        /// <summary>
        /// אירוע לחיצה על כפתור האינבוקס
        /// </summary>
        private void btnInbox_Click(object sender, EventArgs e)
        {
            // יצירת מופע של טופס האינבוקס
            InboxForm inboxForm = new InboxForm();

            // הצגת הטופס כדיאלוג והמתנה לתוצאה
            DialogResult result = inboxForm.ShowDialog();

            // אם המשתמש בחר קוד (לחץ על 'use')
            if (result == DialogResult.OK && !string.IsNullOrEmpty(inboxForm.SelectedCode))
            {
                // הצב את הקוד הנבחר בתיבת הטקסט של הקלט
                input.Text = inboxForm.SelectedCode;
            }
        }

        private void runCodeBtn_Click(object sender, EventArgs e)
        {
            try
            {
                string csharpCode = CodeC.Text;
                if (string.IsNullOrWhiteSpace(csharpCode))
                {
                    MessageBox.Show("No C# code to run!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // יצירת תיקייה זמנית
                string tempFolder = Path.Combine(Path.GetTempPath(), "PoliteCodeRunner");
                Directory.CreateDirectory(tempFolder);

                // שמירת הקוד לקובץ
                string filePath = Path.Combine(tempFolder, "Program.cs");
                File.WriteAllText(filePath, csharpCode);

                // יצירת קובץ .csproj פשוט יותר
                string csprojPath = Path.Combine(tempFolder, "TempProject.csproj");
                string csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net472</TargetFramework>
  </PropertyGroup>
</Project>";
                File.WriteAllText(csprojPath, csprojContent);

                // הרצת הקוד
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"build \"{csprojPath}\" -c Release",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = tempFolder
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(error) && error.Contains("error"))
                {
                    MessageBox.Show($"Build error: {error}", "Compilation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // בנייה הצליחה, עכשיו הרץ את התוכנית
                var runProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"run --project \"{csprojPath}\" --no-build",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = tempFolder
                    }
                };

                runProcess.Start();
                string runOutput = runProcess.StandardOutput.ReadToEnd();
                string runError = runProcess.StandardError.ReadToEnd();
                runProcess.WaitForExit();

                if (!string.IsNullOrEmpty(runError))
                {
                    MessageBox.Show($"Runtime error: {runError}", "Execution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (!string.IsNullOrEmpty(runOutput))
                {
                    MessageBox.Show($"Program Output:\n\n{runOutput}", "Execution Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Program executed with no output.", "Execution Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}