using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace PoliteCode.Validation
{
    public class ExpressionValidator
    {
        public enum T { num, plus, min, mul, div, open, close, var, str, bool_val, wtf }
        public enum S { start, op, num, open, close, str, var }

        private Dictionary<S, Dictionary<T, S>> states;
        private HashSet<S> endingStates;

        // מעקב אחר סוגי משתנים במהלך האימות
        private Dictionary<string, string> _variableTypes;

        public ExpressionValidator()
        {
            InitializeStates();
            _variableTypes = new Dictionary<string, string>();
        }

        // הגדרת משתנים זמינים וסוגיהם לאימות
        public void SetVariableTypes(Dictionary<string, string> variableTypes)
        {
            _variableTypes = variableTypes ?? new Dictionary<string, string>();
        }

        private void InitializeStates()
        {
            states = new Dictionary<S, Dictionary<T, S>>
            {
                { S.start, new Dictionary<T, S>
                    {
                        { T.num, S.num },
                        { T.plus, S.op },
                        { T.min, S.op },
                        { T.open, S.open },
                        { T.str, S.str },
                        { T.var, S.var },
                        { T.bool_val, S.num }
                    }
                },
                { S.op, new Dictionary<T, S>
                    {
                        { T.num, S.num },
                        { T.open, S.open },
                        { T.str, S.str },
                        { T.var, S.var }
                    }
                },
                { S.num, new Dictionary<T, S>
                    {
                        { T.plus, S.op },
                        { T.min, S.op },
                        { T.mul, S.op },
                        { T.div, S.op },
                        { T.close, S.close }
                    }
                },
                { S.str, new Dictionary<T, S>
                    {
                        { T.plus, S.op },
                        { T.close, S.close }
                    }
                },
                { S.var, new Dictionary<T, S>
                    {
                        { T.plus, S.op },
                        { T.min, S.op },
                        { T.mul, S.op },
                        { T.div, S.op },
                        { T.close, S.close }
                    }
                },
                { S.open, new Dictionary<T, S>
                    {
                        { T.num, S.num },
                        { T.open, S.open },
                        { T.plus, S.op },
                        { T.min, S.op },
                        { T.str, S.str },
                        { T.var, S.var },
                    }
                },
                { S.close, new Dictionary<T, S>
                    {
                        { T.plus, S.op },
                        { T.min, S.op },
                        { T.mul, S.op },
                        { T.div, S.op },
                        { T.close, S.close }
                    }
                }
            };

            endingStates = new HashSet<S> { S.num, S.close, S.str, S.var };
        }

        public bool IsValid(string expr, string type, out string errorMsg)
        {
            errorMsg = null;
            if (type == "integer") return IsValid_integer(expr, out errorMsg);
            if (type == "decimal") return IsValid_decimal(expr, out errorMsg);
            if (type == "boolean") return IsValid_boolean(expr, out errorMsg);
            if (type == "text") return IsValid_text(expr, out errorMsg);

            errorMsg = $"Unknown type: {type}";
            return false;
        }

        private List<string> Tokenize(string input)
        {
            input = Regex.Replace(input, @"(add|sub|mul|div)(?=\d)", "$1 ");

            // זיהוי מספרים, אופרטורים, סוגריים, ושמות משתנים (אלפא-נומריים שמתחילים באות)
            string pattern = @"-?\d+(\.\d+)?|add|sub|mul|div|\(|\)|""[^""]*""|true|false|[a-zA-Z_]\w*";

            var matches = Regex.Matches(input, pattern);
            return matches.Cast<Match>().Select(m => m.Value).ToList();
        }

        private List<T> ConvertToTokens(List<string> tokens, string expectedType, out string errorMsg)
        {
            List<T> result = new List<T>();
            errorMsg = null;

            int i = 0;
            while (i < tokens.Count)
            {
                string token = tokens[i];

                if (IsUnary(token, i, tokens))
                {
                    if (i + 1 < tokens.Count && int.TryParse(tokens[i + 1], out int val))
                    {
                        tokens[i + 1] = (-val).ToString(); // משנה ל־"-10"
                        result.Add(T.num);
                        i += 2; // מדלג על sub והמספר
                    }
                    else
                    {
                        errorMsg = $"Invalid unary usage near: {token}";
                        return null;
                    }
                }
                else
                {
                    var tokenType = GetToken(token);

                    // בדיקת סוגי משתנים בביטוי
                    if (tokenType == T.var)
                    {
                        if (_variableTypes.TryGetValue(token, out string varType))
                        {
                            // בדיקה אם סוג המשתנה תואם לסוג הצפוי
                            if (!AreTypesCompatible(varType, expectedType))
                            {
                                errorMsg = $"Type mismatch: variable '{token}' is of type '{varType}' but expression expects '{expectedType}'";
                                return null;
                            }
                        }
                        else
                        {
                            // משתנה לא נמצא, זה צריך להיקלט קודם
                            errorMsg = $"Variable '{token}' not found in current scope";
                            return null;
                        }
                    }

                    // בדיקת מחרוזות בהקשרים שאינם טקסט
                    if (tokenType == T.str && expectedType != "text")
                    {
                        errorMsg = $"Type mismatch: string literal cannot be used in a '{expectedType}' expression";
                        return null;
                    }

                    // בדיקת ערכים מספריים בהקשרי טקסט
                    if (tokenType == T.num && expectedType == "text" &&
                        (i > 0 && GetToken(tokens[i - 1]) == T.str))
                    {
                        // אפשר מספרים אחרי מחרוזות עם אופרטור add
                        if (i >= 2 && tokens[i - 2] == "add")
                        {
                            // זה מותר (string "hi" add 5)
                        }
                        else
                        {
                            errorMsg = $"Type mismatch: numeric literal {token} cannot be concatenated with a string without 'add' operator";
                            return null;
                        }
                    }

                    // בדיקת ערכים בוליאניים בהקשרים שאינם בוליאניים
                    if (tokenType == T.bool_val && expectedType != "boolean")
                    {
                        errorMsg = $"Type mismatch: boolean literal cannot be used in a '{expectedType}' expression";
                        return null;
                    }

                    if (tokenType == T.wtf)
                    {
                        errorMsg = $"Invalid token: {token}";
                        return null;
                    }

                    result.Add(tokenType);
                    i++; // ממשיך לטוקן הבא כרגיל
                }
            }

            return result;
        }

        private bool AreTypesCompatible(string type1, string type2)
        {
            // תאימות בסיסית של סוגים
            if (type1 == type2) return true;

            // מקרים מיוחדים
            if (type1 == "integer" && type2 == "decimal") return true; // שלם יכול לשמש בביטויים עשרוניים
            if (type1 == "decimal" && type2 == "integer") return true; // עשרוני יכול לשמש בביטויים שלמים אך יקוצץ

            return false;
        }

        private bool IsUnary(string token, int index, List<string> tokens)
        {
            return token == "sub" && (index == 0 ||
                new[] { "add", "sub", "mul", "div", "(" }.Contains(tokens[index - 1]));
        }

        private T GetToken(string token)
        {
            if (double.TryParse(token, out _)) return T.num;
            if (token.StartsWith("\"") && token.EndsWith("\"")) return T.str; // זיהוי מחרוזות
            if (token == "true" || token == "false") return T.bool_val; // זיהוי ערכים בוליאניים
            if (token == "add") return T.plus;
            if (token == "sub") return T.min;
            if (token == "mul") return T.mul;
            if (token == "div") return T.div;
            if (token == "(") return T.open;
            if (token == ")") return T.close;
            if (Regex.IsMatch(token, @"^[a-zA-Z_]\w*$")) return T.var; // שמות משתנים
            return T.wtf;
        }

        #region מאמתי סוגים
        public bool IsValid_integer(string expr, out string errorMsg)
        {
            var inputTokens = Tokenize(expr);
            var tokenTypes = ConvertToTokens(inputTokens, "integer", out errorMsg);

            if (tokenTypes == null)
                return false;

            S currentState = S.start;
            Stack<T> brackets = new Stack<T>();

            foreach (var token in tokenTypes)
            {
                if (!states[currentState].ContainsKey(token))
                {
                    errorMsg = $"Invalid token sequence near: {token}";
                    return false;
                }

                if (token == T.open)
                    brackets.Push(token);
                else if (token == T.close)
                {
                    if (brackets.Count == 0)
                    {
                        errorMsg = "Unmatched closing parenthesis";
                        return false;
                    }
                    brackets.Pop();
                }

                currentState = states[currentState][token];
            }

            if (brackets.Count > 0)
            {
                errorMsg = "Unmatched opening parenthesis";
                return false;
            }

            if (!endingStates.Contains(currentState))
            {
                errorMsg = "Expression does not end properly";
                return false;
            }

            errorMsg = null;
            return true;
        }

        public bool IsValid_decimal(string expr, out string errorMsg)
        {
            errorMsg = null;

            // בדיקת תווים
            if (!Regex.IsMatch(expr, @"^[\d\.\saddsubmuldiv\(\)-]+$") && !Regex.IsMatch(expr, @"[a-zA-Z_]\w*"))
            {
                errorMsg = "Decimal expression contains invalid characters.";
                return false;
            }

            // רווחים בין מילים למספרים
            expr = Regex.Replace(expr, @"(add|sub|mul|div)(?=\d)", "$1 ");

            // שליחה לבדיקה דרך FSM
            var inputTokens = Tokenize(expr);
            var tokenTypes = ConvertToTokens(inputTokens, "decimal", out errorMsg);

            if (tokenTypes == null)
                return false;

            S currentState = S.start;
            Stack<T> brackets = new Stack<T>();

            foreach (var token in tokenTypes)
            {
                if (!states[currentState].ContainsKey(token))
                {
                    errorMsg = $"Invalid token sequence near: {token}";
                    return false;
                }

                if (token == T.open)
                    brackets.Push(token);
                else if (token == T.close)
                {
                    if (brackets.Count == 0)
                    {
                        errorMsg = "Unmatched closing parenthesis";
                        return false;
                    }
                    brackets.Pop();
                }

                currentState = states[currentState][token];
            }

            if (brackets.Count > 0)
            {
                errorMsg = "Unmatched opening parenthesis";
                return false;
            }

            if (!endingStates.Contains(currentState))
            {
                errorMsg = "Expression does not end properly";
                return false;
            }

            return true;
        }

        public bool IsValid_boolean(string expr, out string errorMsg)
        {
            expr = expr.Trim().ToLower();
            if (expr == "true" || expr == "false")
            {
                errorMsg = null;
                return true;
            }

            // אפשר משתנים מסוג בוליאני
            if (Regex.IsMatch(expr, @"^[a-zA-Z_]\w*$") &&
                _variableTypes.TryGetValue(expr, out string varType) &&
                varType == "boolean")
            {
                errorMsg = null;
                return true;
            }

            errorMsg = $"Boolean type must be either 'true' or 'false', or a boolean variable. Cannot accept expressions.";
            return false;
        }

        public bool IsValid_text(string expr, out string errorMsg)
        {
            var tokens = Tokenize(expr);
            if (tokens.Count == 0)
            {
                errorMsg = "Empty string expression";
                return false;
            }

            // מקרה מיוחד למשתנה יחיד מסוג טקסט
            if (tokens.Count == 1 && Regex.IsMatch(tokens[0], @"^[a-zA-Z_]\w*$"))
            {
                if (_variableTypes.TryGetValue(tokens[0], out string varType) && varType == "text")
                {
                    errorMsg = null;
                    return true;
                }
                else
                {
                    errorMsg = $"Variable '{tokens[0]}' is not of type 'text'";
                    return false;
                }
            }

            // אימות בסיסי של מחרוזות
            var tokenTypes = ConvertToTokens(tokens, "text", out errorMsg);
            if (tokenTypes == null)
                return false;

            // בדיקת דפוסים של ביטויי מחרוזת תקינים
            for (int i = 0; i < tokens.Count; i++)
            {
                // בדיקת טוקנים לפי מיקום ודפוס
                if (i % 2 == 0) // צריך להיות מחרוזות או משתנים
                {
                    bool isStringLiteral = tokens[i].StartsWith("\"") && tokens[i].EndsWith("\"");
                    bool isTextVariable = Regex.IsMatch(tokens[i], @"^[a-zA-Z_]\w*$") &&
                                         _variableTypes.TryGetValue(tokens[i], out string varType) &&
                                         varType == "text";

                    if (!isStringLiteral && !isTextVariable)
                    {
                        errorMsg = $"Expected string literal or text variable at position {i + 1}, got '{tokens[i]}'";
                        return false;
                    }
                }
                else // צריך להיות אופרטור 'add'
                {
                    if (tokens[i] != "add")
                    {
                        errorMsg = $"Expected 'add' between strings at position {i + 1}, got '{tokens[i]}'";
                        return false;
                    }
                }
            }

            // תמיכה בחיבור מחרוזות עם מספרים
            // לדוגמה: "hello" add 5
            if (tokens.Count >= 3 && tokens[tokens.Count - 2] == "add")
            {
                string lastToken = tokens[tokens.Count - 1];
                bool isNumber = double.TryParse(lastToken, out _);
                bool isVariable = Regex.IsMatch(lastToken, @"^[a-zA-Z_]\w*$");

                if (isNumber)
                {
                    // חיבור מחרוזת עם מספר תקין
                }
                else if (isVariable)
                {
                    // בדיקה אם המשתנה תואם (מחרוזת, מספר שלם, עשרוני)
                    if (_variableTypes.TryGetValue(lastToken, out string varType))
                    {
                        if (varType != "text" && varType != "integer" && varType != "decimal")
                        {
                            errorMsg = $"Cannot concatenate variable '{lastToken}' of type '{varType}' with a string";
                            return false;
                        }
                    }
                    else
                    {
                        errorMsg = $"Variable '{lastToken}' not found in current scope";
                        return false;
                    }
                }
                else if (!lastToken.StartsWith("\"") || !lastToken.EndsWith("\""))
                {
                    errorMsg = $"Invalid value to concatenate with string: '{lastToken}'";
                    return false;
                }
            }

            errorMsg = null;
            return true;
        }
        #endregion
    }
}