using PoliteCode.Validation;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PoliteCode
{
    /// <summary>
    /// כלי עזר ושירותים עבור PoliteCode
    /// </summary>
    public class PoliteCodeTools
    {
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
        /// הצגת הודעת שגיאה למשתמש
        /// </summary>
        /// <param name="message">הודעת שגיאה</param>
        public void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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