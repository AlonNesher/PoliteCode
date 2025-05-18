using PoliteCode.Validation;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Linq;

namespace PoliteCode
{
    /// <summary>
    /// כלי עזר ושירותים עבור PoliteCode, כולל טיפול בשגיאות מפורטות יותר
    /// </summary>
    public class PoliteCodeTools
    {
        // שמירת טקסט הקוד המקורי לצורך הצגת הודעות שגיאה מדויקות יותר
        private List<string> _sourceLines = new List<string>();
        private Form1 _parentForm; // הפניה לטופס הראשי

        /// <summary>
        /// הגדרת הטופס הראשי לצורך סימון שורות שגיאה
        /// </summary>
        public void SetParentForm(Form1 form)
        {
            _parentForm = form;
        }

        /// <summary>
        /// שמירת קוד המקור לצורך הודעות שגיאה מדויקות יותר
        /// </summary>
        public void SetSourceCode(string[] lines)
        {
            _sourceLines = new List<string>(lines);
        }

        /// <summary>
        /// בדיקת תקינות הביטוי מבחינת תחביר וסמנטיקה
        /// </summary>
        /// <param name="expr">ביטוי לבדיקה</param>
        /// <param name="type">סוג ביטוי צפוי</param>
        /// <param name="errorMessage">הודעת שגיאה אם הבדיקה נכשלת</param>
        /// <returns>True אם הביטוי תקין</returns>
        public bool ValidateExpression(string expr, string type, out string errorMessage)
        {
            var validator = new ExpressionValidator();
            return validator.IsValid(expr, type, out errorMessage);
        }

        /// <summary>
        /// תרגום סוג PoliteCode לסוג C#
        /// </summary>
        /// <param name="politeType">סוג PoliteCode</param>
        /// <returns>סוג C#</returns>
        public string TranslateType(string politeType)
        {
            // המרת סוג PoliteCode לסוג C#
            switch (politeType)
            {
                case "integer": return "int";
                case "text": return "string";
                case "decimal": return "double";
                case "boolean": return "bool";
                case "void": return "void";
                default:
                    ShowError($"Unknown type: '{politeType}' – please use: integer, text, decimal, boolean, or void.");
                    return string.Empty;
            }
        }

        /// <summary>
        /// תרגום סוג C# לסוג PoliteCode
        /// </summary>
        /// <param name="csharpType">סוג C#</param>
        /// <returns>סוג PoliteCode</returns>
        public string TranslateTypeBack(string csharpType)
        {
            switch (csharpType)
            {
                case "int": return "integer";
                case "string": return "text";
                case "double": return "decimal";
                case "bool": return "boolean";
                default: return "unknown";
            }
        }

        /// <summary>
        /// הצגת הודעת שגיאה מפורטת למשתמש
        /// </summary>
        /// <param name="message">הודעת השגיאה</param>
        /// <param name="lineNumber">מספר השורה בה התרחשה השגיאה, -1 אם לא ידוע</param>
        /// <param name="columnNumber">מספר העמודה בה התרחשה השגיאה, -1 אם לא ידוע</param>
        public void ShowError(string message, int lineNumber = -1, int columnNumber = -1)
        {
            string errorMessage = message;

            // אם מספר שורה תקף נמסר, הוסף אותו ואת תוכן השורה להודעה
            if (lineNumber >= 0 && lineNumber < _sourceLines.Count)
            {
                string lineText = _sourceLines[lineNumber].Trim();
                string linePointer = string.Empty;

                // הוספת סימון לעמודה הספציפית אם ידועה
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
                        // במקרה של שגיאה בסימון, פשוט המשך ללא סימון
                        Console.WriteLine($"Warning: Could not highlight error line: {ex.Message}");
                    }
                }
            }
            else if (lineNumber >= 0)
            {
                // מקרה שיש מספר שורה אך לא ניתן לקבל את תוכן השורה
                errorMessage = $"Error at line {lineNumber + 1}:\n{message}";
            }

            MessageBox.Show(errorMessage, "Compilation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// הצגת הודעת מידע (לניפוי שגיאות והתקדמות)
        /// </summary>
        /// <param name="message">הודעת מידע</param>
        public void ShowInfo(string message)
        {
            Console.WriteLine(message); // לניפוי שגיאות ודיווח התקדמות
        }

        /// <summary>
        /// חיפוש מיקום הטוקן בטקסט המקורי
        /// </summary>
        /// <param name="token">הטוקן לחיפוש</param>
        /// <param name="startLineIndex">שורת ההתחלה לחיפוש</param>
        /// <param name="foundLineIndex">שורה בה נמצא הטוקן (מוחזר)</param>
        /// <param name="columnIndex">עמודה בה נמצא הטוקן (מוחזר)</param>
        /// <returns>True אם הטוקן נמצא</returns>
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

        /// <summary>
        /// קבלת מספר השורה הנוכחית מהשוואה עם טקסט המקור
        /// </summary>
        /// <param name="context">טקסט הקשר</param>
        /// <returns>מספר השורה, או -1 אם לא נמצא</returns>
        public int GetLineNumberFromContext(string context)
        {
            if (_sourceLines == null || _sourceLines.Count == 0 || string.IsNullOrEmpty(context))
                return -1;

            context = context.Trim();

            // ניסיון למצוא התאמה מדויקת של השורה
            for (int i = 0; i < _sourceLines.Count; i++)
            {
                if (_sourceLines[i].Trim() == context)
                    return i;
            }

            // אם לא נמצא, ננסה לחפש חלקים מהשורה
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

        /// <summary>
        /// כלי עזר סטטי לבדיקת הקצאות משתנים
        /// </summary>
        public static class ValidationHelper
        {
            /// <summary>
            /// בדיקה שערך יכול להיות מוקצה למשתנה מסוג מסוים
            /// </summary>
            /// <param name="variableType">סוג המשתנה</param>
            /// <param name="value">ערך להקצאה</param>
            /// <returns>True אם ההקצאה תקפה</returns>
            public static bool ValidateVariableAssignment(string variableType, string value)
            {
                if (value == null)
                {
                    return true; // Null ניתן להקצאה לכל סוג (מוסק כברירת מחדל)
                }

                switch (variableType)
                {
                    case "integer":
                        return Regex.IsMatch(value, @"^-?\d+$");
                    case "decimal":
                        return Regex.IsMatch(value, @"^-?\d+(\.\d+)?$");
                    case "boolean":
                        return value.ToLower() == "true" || value.ToLower() == "false";
                    case "text":
                        return value.StartsWith("\"") && value.EndsWith("\"");
                    default:
                        return true; // סוג לא ידוע - אפשר הקצאה (או לטפל כשגיאה במקום אחר)
                }
            }
        }
    }
}