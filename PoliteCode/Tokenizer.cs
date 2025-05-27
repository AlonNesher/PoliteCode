using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PoliteCode
{
    /// <summary>
    /// אחראי על פירוק טקסט PoliteCode לטוקנים וזיהוי סוגיהם
    /// </summary>
    public class Tokenizer
    {
        // סוגי טוקנים
        public enum TokenType
        {
            Create,         // "please create"
            Print,          // "thank you for printing"
            IfCondition,    // "thank you for checking if"
            Loop,           // "thank you for looping"
            VariableName,   // Any valid variable identifier
            VariableType,   // integer, text, decimal, boolean
            Number,         // Numeric literal
            StringLiteral,  // String in quotes
            Boolean,        // true/false
            Equals,         // =
            OpenBrace,      // {
            CloseBrace,     // }
            OpenParen,      // (
            CloseParen,     // )
            FromKeyword,    // from (for loops)
            ToKeyword,      // to (for loops)
            WhileKeyword,   // while (for while loops)
            Condition,      // ==, !=, <, >, <=, >=
            Terminator,     // newline character
            Unknown,        // Unrecognized token
            DefineFunction, // "please define function"
            Return,         // "thank you for returning"
            CallFunction    // "please call" (new)
        }

        // שמירת מידע על המשתנה הנוכחי בזמן הטוקניזציה
        private string _currentVariableName = "";
        private string _currentVariableType = "";

        // מילון המקשר מילות מפתח של PoliteCode למקבילות שלהן ב-C#
        private readonly Dictionary<string, string> _keywordMapping = new Dictionary<string, string>
        {
            { "please create", "var" },
            { "thank you for printing", "Console.WriteLine" },
            { "thank you for checking if", "if" },
            { "thank you for looping from", "for" },
            { "thank you for looping while", "while" },
            { "integer", "int" },
            { "text", "string" },
            { "decimal", "double" },
            { "boolean", "bool" },
            { "please define function", "public static" },
            { "thank you for returning", "return" }
        };

        // מילון חדש לזיהוי טוקנים - מילות מפתח קבועות
        private static readonly Dictionary<string, TokenType> _tokenTypeMap = new Dictionary<string, TokenType>
        {
            { "please call", TokenType.CallFunction },
            { "please define function", TokenType.DefineFunction },
            { "please create", TokenType.Create },
            { "thank you for printing", TokenType.Print },
            { "thank you for checking if", TokenType.IfCondition },
            { "thank you for looping", TokenType.Loop },
            { "thank you for returning", TokenType.Return },
            { "from", TokenType.FromKeyword },
            { "to", TokenType.ToKeyword },
            { "while", TokenType.WhileKeyword },
            { "true", TokenType.Boolean },
            { "false", TokenType.Boolean },
            { "equals", TokenType.Equals },
            { "{", TokenType.OpenBrace },
            { "}", TokenType.CloseBrace },
            { "(", TokenType.OpenParen },
            { ")", TokenType.CloseParen },
            { "\n", TokenType.Terminator }
        };

        // מילון לטיפוסי משתנים
        private static readonly Dictionary<string, string> _typeMap = new Dictionary<string, string>
        {
            { "integer", "integer" },
            { "text", "text" },
            { "decimal", "decimal" },
            { "boolean", "boolean" },
            { "void", "void" }
        };

        // סט לתנאים
        private static readonly HashSet<string> _conditionSet = new HashSet<string>
        {
            "less then", "greater then", "equal to", "different from",
            "less or equal to", "greater or equal to"
        };

        // ביטויים רגולריים מקומפלים מראש
        // מספר
        private static readonly Regex _numberRegex = new Regex(@"^-?\d+(\.\d+)?$", RegexOptions.Compiled);

        //אם זה סטרינג 
        private static readonly Regex _stringLiteralRegex = new Regex("^\".*\"$", RegexOptions.Compiled);
        
        //אם זה משתנה
        private static readonly Regex _variableNameRegex = new Regex(@"^[a-zA-Z_]\w*$", RegexOptions.Compiled);

        /// <summary>
        /// מבצע טוקניזציה של שורת קלט ב-PoliteCode
        /// </summary>
        public List<string> TokenizeInput(string input)
        {
            string pattern = @"(please define function|please create|please call|thank you for printing|thank you for checking if|thank you for looping|thank you for returning|from|to|while|greater or equal to|less or equal to|equal to|different from|greater then|less then|[a-zA-Z_]\w*|-?\d+(\.\d+)?|""[^""]*""|true|false|equals|\(|\)|\{|\})";
            var matches = Regex.Matches(input, pattern);
            return matches.Cast<Match>().Select(m => m.Value).ToList();
        }

        /// <summary>
        /// מעבד מחרוזות טוקן לסוגי TokenType
        /// </summary>
        public List<TokenType> ProcessTokens(List<string> inputTokens)
        {
            var tokens = new List<TokenType>();

            for (int i = 0; i < inputTokens.Count; i++)
            {
                string token = inputTokens[i];
                TokenType tokenType = IdentifyToken(token);

                if (tokenType == TokenType.Unknown)
                {
                    return null;
                }

                tokens.Add(tokenType);
            }

            return tokens;
        }

        /// <summary>
        /// מזהה את סוג הטוקן - גרסה משופרת עם מילונים
        /// </summary>
        public TokenType IdentifyToken(string value)
        {
            // 1. בדיקת מילות מפתח במילון - חיפוש O(1)
            if (_tokenTypeMap.TryGetValue(value, out TokenType tokenType))
                return tokenType;

            // 2. בדיקת טיפוסי משתנים
            if (_typeMap.ContainsKey(value))
            {
                _currentVariableType = value;
                return TokenType.VariableType;
            }

            // 3. בדיקת תנאים 
            if (_conditionSet.Contains(value))
                return TokenType.Condition;

            // 4. בדיקות עם ביטויים רגולריים
            if (_numberRegex.IsMatch(value))
                return TokenType.Number;

            if (_stringLiteralRegex.IsMatch(value))
                return TokenType.StringLiteral;

            if (_variableNameRegex.IsMatch(value))
            {
                _currentVariableName = value;
                return TokenType.VariableName;
            }

            // ברירת מחדל - טוקן לא מוכר
            return TokenType.Unknown;
        }

        /// <summary>
        /// מחזיר את שם המשתנה הנוכחי המעובד
        /// </summary>
        public string CurrentVariableName => _currentVariableName;

        /// <summary>
        /// מחזיר את סוג המשתנה הנוכחי המעובד
        /// </summary>
        public string CurrentVariableType => _currentVariableType;

        /// <summary>
        /// מילון המקשר מילות מתמטיקה לסמלים
        /// </summary>
        public Dictionary<string, string> MathWordToSymbol { get; } = new Dictionary<string, string>
        {
            { "equal to", "==" },
            { "greater then", ">" },
            { "less then", "<" },
            { "different from", "!=" },
            { "greater or equal to", ">=" },
            { "less or equal to", "<=" },
            { "add", "+" },
            { "sub", "-" },
            { "mul", "*" },
            { "div", "/" },
            { "equals", "=" }
        };
    }
}