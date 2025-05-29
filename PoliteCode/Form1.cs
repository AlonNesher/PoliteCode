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
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;

namespace PoliteCode
{
    public partial class Form1 : Form
    {
        //  רכיבי ליבה של התוכנית
        private Tokenizer _tokenizer;
        private CodeGenerator _codeGenerator;
        private PoliteCodeTools _tools;
        private Parser _parser;

        // הקונסולה עכשיו מוגדרת בDesigner - לא צריך להגדיר כאן!

        public Form1()
        {
            InitializeComponent();
            InitializeComponents();
            // הוסרה הקריאה InitializeOutputTextBox() כי הקונסולה עכשיו בDesigner
        }

        /// אתחול רכיבי המערכת
        private void InitializeComponents()
        {
            // יצירת מופעים של הרכיבים
            _tokenizer = new Tokenizer();
            _tools = new PoliteCodeTools();
            _tools.SetParentForm(this); // העברת הפניה לטופס הנוכחי
            _codeGenerator = new CodeGenerator(_tokenizer);
            _parser = new Parser(_tokenizer, _codeGenerator, _tools);
        }

        // כפתור הרצה (תרגום)
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //parser איפוס  - כל קוד שונה רלוונטי בנפרד 
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

        /// הצגת הקוד המתורגם
        private void ShowFinalCode()
        {
            CodeC.Text = _parser.GetCSharpCode();
        }

        // טיפול באירוע טעינת הטופס
        private void Form1_Load(object sender, EventArgs e)
        {
            // אתחול נוסף בעת טעינת הטופס אם נדרש
        }

        // לחיצה על כפתור האינבוקס
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

        // כפתור הרצת הקוד
        private void runCodeBtn_Click(object sender, EventArgs e)
        {
            try
            {
                string csharpCode = CodeC.Text;
                if (string.IsNullOrWhiteSpace(csharpCode))
                {
                    ShowOutput("No C# code to run!", true);
                    return;
                }

                ShowOutput("Compiling and running code...", false);

                // הרצה באופן אסינכרוני כדי לא לחסום את הUI
                Task.Run(() => RunGeneratedCode(csharpCode));
            }
            catch (Exception ex)
            {
                ShowOutput($"Error: {ex.Message}", true);
            }
        }

        // כפתור ניקוי הקונסולה - חדש!
        private void clearConsoleBtn_Click(object sender, EventArgs e)
        {
            outputTextBox.Clear();
            outputTextBox.Text = "🖥️ Console Output\r\n" + new string('─', 35) + "\r\n";
            outputTextBox.SelectionStart = outputTextBox.Text.Length;
            outputTextBox.ScrollToCaret();
        }

        /// הרצת הקוד בקומפיילר פנימי
        private void RunGeneratedCode(string csharpCode)
        {
            try
            {
                // יצירת קומפיילר
                CSharpCodeProvider provider = new CSharpCodeProvider();
                CompilerParameters parameters = new CompilerParameters();

                // הגדרות קומפיילציה
                parameters.GenerateInMemory = true;
                parameters.GenerateExecutable = false; // DLL במקום EXE
                parameters.TreatWarningsAsErrors = false;
                parameters.WarningLevel = 3;

                // הוספת רפרנסים נדרשים
                parameters.ReferencedAssemblies.Add("System.dll");
                parameters.ReferencedAssemblies.Add("System.Core.dll");
                parameters.ReferencedAssemblies.Add("System.Console.dll");
                parameters.ReferencedAssemblies.Add("System.Runtime.dll");
                parameters.ReferencedAssemblies.Add("netstandard.dll");

                // קומפיילציה
                CompilerResults results = provider.CompileAssemblyFromSource(parameters, csharpCode);

                // בדיקת שגיאות קומפיילציה
                if (results.Errors.HasErrors)
                {
                    StringBuilder errorMsg = new StringBuilder();
                    errorMsg.AppendLine("Compilation Errors:");

                    foreach (CompilerError error in results.Errors)
                    {
                        if (!error.IsWarning)
                        {
                            errorMsg.AppendLine($"Line {error.Line}: {error.ErrorText}");
                        }
                    }

                    ShowOutput(errorMsg.ToString(), true);
                    return;
                }

                // הרצת הקוד
                Assembly assembly = results.CompiledAssembly;
                Type programType = assembly.GetType("Program");

                if (programType == null)
                {
                    ShowOutput("Error: Program class not found", true);
                    return;
                }

                MethodInfo mainMethod = programType.GetMethod("Main", BindingFlags.Static | BindingFlags.Public);

                if (mainMethod == null)
                {
                    ShowOutput("Error: Main method not found", true);
                    return;
                }

                // שמירת הקונסולה המקורית
                TextWriter originalConsoleOut = Console.Out;

                // יצירת StringWriter לתפיסת הפלט
                using (StringWriter stringWriter = new StringWriter())
                {
                    Console.SetOut(stringWriter);

                    try
                    {
                        // הרצת הפונקציה
                        if (mainMethod.GetParameters().Length == 0)
                            mainMethod.Invoke(null, null);
                        else
                            mainMethod.Invoke(null, new object[] { new string[0] });

                        // קבלת הפלט
                        string output = stringWriter.ToString();
                        ShowOutput("Program Output:", false);
                        ShowOutput(output.Length > 0 ? output : "(No output produced)", false);
                    }
                    catch (Exception ex)
                    {
                        ShowOutput($"Runtime Error: {ex.InnerException?.Message ?? ex.Message}", true);
                    }
                    finally
                    {
                        // החזרת הקונסולה המקורית
                        Console.SetOut(originalConsoleOut);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowOutput($"Compilation Error: {ex.Message}", true);
            }
        }

        /// הצגת פלט בTextBox הקונסولה - מעודכן עם גלילה טובה יותר
        private void ShowOutput(string output, bool isError)
        {
            if (outputTextBox.InvokeRequired)
            {
                outputTextBox.Invoke(new Action(() => {
                    outputTextBox.AppendText(output + "\r\n");
                    outputTextBox.ForeColor = isError ? Color.Red : Color.LimeGreen;
                    // גלילה אוטומטית לסוף
                    outputTextBox.SelectionStart = outputTextBox.Text.Length;
                    outputTextBox.ScrollToCaret();
                }));
            }
            else
            {
                outputTextBox.AppendText(output + "\r\n");
                outputTextBox.ForeColor = isError ? Color.Red : Color.LimeGreen;
                // גלילה אוטומטית לסוף
                outputTextBox.SelectionStart = outputTextBox.Text.Length;
                outputTextBox.ScrollToCaret();
            }
        }

        // סימון שורה עם שגיאה בתיבת הטקסט -error handling
        public void HighlightErrorLine(int lineNumber)
        {
            if (lineNumber < 0 || input == null || lineNumber >= input.Lines.Length)
                return;

            try
            {
                // מציאת מיקום השורה בתיבת הטקסט
                int lineStartPos = input.GetFirstCharIndexFromLine(lineNumber);
                int lineEndPos;

                if (lineNumber < input.Lines.Length - 1)
                    lineEndPos = input.GetFirstCharIndexFromLine(lineNumber + 1) - 1;
                else
                    lineEndPos = input.Text.Length;

                // סימון השורה
                input.Select(lineStartPos, lineEndPos - lineStartPos);
                input.Focus();

                // אפשרות: שינוי צבע הרקע של השורה
                // צריך לדאוג לשחזור את הצבע המקורי בהמשך
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error highlighting line: {ex.Message}");
            }
        }

        private void outputTextBox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}