using PoliteCode.Validation;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Linq;

namespace PoliteCode
{
    // כלי עזר ושירותים עבור PoliteCode
    public class PoliteCodeTools
    {
        // מילון המרת סוגים מ-PoliteCode ל-C#
        private readonly Dictionary<string, string> _politeToCs = new Dictionary<string, string>
        {
            { "integer", "int" },
            { "text", "string" },
            { "decimal", "double" },
            { "boolean", "bool" },
            { "void", "void" }
        };

        // מילון המרת סוגים מ-C# ל-PoliteCode
        private readonly Dictionary<string, string> _csToPolite = new Dictionary<string, string>
        {
            { "int", "integer" },
            { "string", "text" },
            { "double", "decimal" },
            { "bool", "boolean" },
            { "void", "void" }
        };

        // שמירת טקסט הקוד המקורי
        private List<string> _sourceLines = new List<string>();
        // הפניה לטופס הראשי
        private Form1 _parentForm;

        // הגדרת הטופס הראשי
        public void SetParentForm(Form1 form)
        {
            _parentForm = form;
        }

        // שמירת קוד המקור
        public void SetSourceCode(string[] lines)
        {
            _sourceLines = new List<string>(lines);
        }

        // בדיקת תקינות הביטוי
        public bool ValidateExpression(string expr, string type, out string errorMessage)
        {
            var validator = new ExpressionValidator();
            return validator.IsValid(expr, type, out errorMessage);
        }

        // תרגום סוג PoliteCode לסוג C#
        public string TranslateType(string politeType)
        {
            if (_politeToCs.ContainsKey(politeType))
            {
                return _politeToCs[politeType];
            }

            ShowError($"Unknown type: '{politeType}' – please use: integer, text, decimal, boolean, or void.");
            return string.Empty;
        }

        // תרגום סוג C# לסוג PoliteCode
        public string TranslateTypeBack(string csharpType)
        {
            if (_csToPolite.ContainsKey(csharpType))
            {
                return _csToPolite[csharpType];
            }

            return "unknown";
        }

        // הצגת הודעת שגיאה מפורטת
        public void ShowError(string message, int lineNumber = -1, int columnNumber = -1)
        {
            string errorMessage = message;

            // הוספת פרטי השורה אם זמינים
            if (lineNumber >= 0 && lineNumber < _sourceLines.Count)
            {
                string lineText = _sourceLines[lineNumber].Trim();
                string linePointer = string.Empty;

                // סימון עמודה ספציפית
                if (columnNumber >= 0 && columnNumber < lineText.Length)
                {
                    linePointer = new string(' ', columnNumber) + "^";
                }

                errorMessage = $"Error at line {lineNumber + 1}:\n\"{lineText}\"\n{linePointer}\n\n{message}";

                // סימון השורה בתיבת הטקסט
                if (_parentForm != null)
                {
                    try
                    {
                        _parentForm.BeginInvoke(new Action(() => {
                            _parentForm.HighlightErrorLine(lineNumber);
                        }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Could not highlight error line: {ex.Message}");
                    }
                }
            }
            else if (lineNumber >= 0)
            {
                errorMessage = $"Error at line {lineNumber + 1}:\n{message}";
            }

            MessageBox.Show(errorMessage, "Compilation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // הצגת הודעת מידע
        public void ShowInfo(string message)
        {
            Console.WriteLine(message);
        }

        // חיפוש מיקום הטוקן בטקסט המקורי
        public bool FindTokenLocation(string token, int startLineIndex, out int foundLineIndex, out int columnIndex)
        {
            foundLineIndex = -1;
            columnIndex = -1;

            if (_sourceLines == null || _sourceLines.Count == 0 || string.IsNullOrEmpty(token))
                return false;

            // הגבלת טווח החיפוש
            int endLineIndex = Math.Min(startLineIndex + 5, _sourceLines.Count - 1);

            for (int i = startLineIndex; i <= endLineIndex; i++)
            {
                string line = _sourceLines[i];
                int index = line.IndexOf(token);
                if (index >= 0)
                {
                    foundLineIndex = i;
                    columnIndex = index;
                    return true;
                }
            }

            return false;
        }

        // קבלת מספר השורה מהקשר
        public int GetLineNumberFromContext(string context)
        {
            if (_sourceLines == null || _sourceLines.Count == 0 || string.IsNullOrEmpty(context))
                return -1;

            context = context.Trim();

            // חיפוש התאמה מדויקת
            for (int i = 0; i < _sourceLines.Count; i++)
            {
                if (_sourceLines[i].Trim() == context)
                    return i;
            }

            // חיפוש חלקי
            if (context.Length > 10)
            {
                string partialContext = context.Substring(0, 10);
                for (int i = 0; i < _sourceLines.Count; i++)
                {
                    if (_sourceLines[i].Contains(partialContext))
                        return i;
                }
            }

            return -1;
        }

        // בדיקת תקינות הקצאת משתנה
        public static bool ValidateVariableAssignment(string variableType, string value)
        {
            if (value == null)
            {
                return true; // Null ניתן להקצאה לכל סוג
            }

            if (variableType == "integer")
            {
                return Regex.IsMatch(value, @"^-?\d+$");
            }
            else if (variableType == "decimal")
            {
                return Regex.IsMatch(value, @"^-?\d+(\.\d+)?$");
            }
            else if (variableType == "boolean")
            {
                return value.ToLower() == "true" || value.ToLower() == "false";
            }
            else if (variableType == "text")
            {
                return value.StartsWith("\"") && value.EndsWith("\"");
            }
            else
            {
                return true; // סוג לא ידוע - אפשר הקצאה
            }
        }
    }
}