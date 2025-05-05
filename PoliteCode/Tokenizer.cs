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
        /// מזהה את סוג הטוקן
        /// </summary>
        public TokenType IdentifyToken(string value)
        {
            // זיהוי סוג הטוקן בהתבסס על מחרוזת הקלט
            if (value == "please call") 
                return TokenType.CallFunction;
            if (value == "please define function") return TokenType.DefineFunction;
            if (value == "please create") return TokenType.Create;
            if (value == "thank you for printing") return TokenType.Print;
            if (value == "thank you for checking if") return TokenType.IfCondition;
            if (value == "thank you for looping") return TokenType.Loop;
            if (value == "thank you for returning") return TokenType.Return;
            if (value == "from") return TokenType.FromKeyword;
            if (value == "to") return TokenType.ToKeyword;
            if (value == "while") return TokenType.WhileKeyword;
            if (value == "true" || value == "false") return TokenType.Boolean;
            if (Regex.IsMatch(value, @"^(less then|greater then|equal to|different from|less or equal to|greater or equal to)$")) return TokenType.Condition;

            if (value == "integer" || value == "text" || value == "decimal" || value == "boolean" || value == "void")
            {
                _currentVariableType = value;
                return TokenType.VariableType;
            }

            if (Regex.IsMatch(value, @"^-?\d+(\.\d+)?$")) return TokenType.Number;
            if (Regex.IsMatch(value, "^\".*\"$")) return TokenType.StringLiteral;
            if (value == "\n") return TokenType.Terminator;
            if (value == "equals") return TokenType.Equals;
            if (value == "{") return TokenType.OpenBrace;
            if (value == "}") return TokenType.CloseBrace;
            if (value == "(") return TokenType.OpenParen;
            if (value == ")") return TokenType.CloseParen;

            if (Regex.IsMatch(value, @"^[a-zA-Z_]\w*$"))
            {
                _currentVariableName = value;
                return TokenType.VariableName;
            }

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