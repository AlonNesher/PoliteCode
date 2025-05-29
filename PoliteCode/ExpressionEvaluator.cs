using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PoliteCode.Validation
{
    public class ExpressionEvaluator
    {
        private readonly Dictionary<string, string> _logicOps = new Dictionary<string, string>
        {
            { "equal to", "==" },
            { "greater then", ">" },
            { "less then", "<" },
            { "different from", "!=" },
            { "greater or equal to", ">=" },
            { "less or equal to", "<=" }
        };

        // For tracking variable types during validation
        private Dictionary<string, string> _variableTypes;

        public ExpressionEvaluator()
        {
            _variableTypes = new Dictionary<string, string>();
        }

        // Set available variables and their types for validation
        public void SetVariableTypes(Dictionary<string, string> variableTypes)
        {
            _variableTypes = variableTypes ?? new Dictionary<string, string>();
        }

        public bool IsValidCondition(string expr, string firstVarType, out string errorMsg)
        {
            errorMsg = null;
            expr = expr.Trim();

            var tokens = Tokenize(expr);
            if (tokens.Count == 0)
            {
                errorMsg = "Empty condition";
                return false;
            }

            if (firstVarType == "boolean")
            {
                return ValidateBooleanCondition(tokens, out errorMsg);
            }
            else if (firstVarType == "integer" || firstVarType == "decimal")
            {
                return ValidateNumericCondition(tokens, out errorMsg);
            }
            else if (firstVarType == "text")
            {
                return ValidateTextCondition(tokens, out errorMsg);
            }
            else
            {
                errorMsg = $"Unsupported variable type '{firstVarType}'";
                return false;
            }
        }

        private List<string> Tokenize(string input)
        {
            // תומך באופרטורים מבוססי מילים כמו equal to, different from וכו'
            string pattern = @"equal to|greater or equal to|less or equal to|greater then|less then|different from|""[^""]*""|-?\d+(\.\d+)?|[a-zA-Z_]\w*|true|false";
            var matches = Regex.Matches(input, pattern);
            return matches.Cast<Match>().Select(m => m.Value).ToList();
        }

        private bool ValidateBooleanCondition(List<string> tokens, out string errorMsg)
        {
            errorMsg = null;
            if (tokens.Count == 1)
            {
                // בדוק שהטוקן הוא אכן משתנה בוליאני או ליטרל בוליאני
                string token = tokens[0];

                // אם זה משתנה, בדוק את סוג המשתנה
                if (Regex.IsMatch(token, @"^[a-zA-Z_]\w*$"))
                {
                    if (_variableTypes.TryGetValue(token, out string varType))
                    {
                        if (varType != "boolean")
                        {
                            errorMsg = $"Variable '{token}' is of type '{varType}', expected 'boolean'";
                            return false;
                        }
                    }
                    else
                    {
                        errorMsg = $"Variable '{token}' not found in current scope";
                        return false;
                    }
                }
                else if (token != "true" && token != "false")
                {
                    errorMsg = $"Expected boolean variable or literal, got '{token}'";
                    return false;
                }

                return true;
            }
            else if (tokens.Count == 3)
            {
                string variable = tokens[0];
                string op = tokens[1];
                string value = tokens[2];

                // בדוק שהמשתנה הוא בוליאני
                if (Regex.IsMatch(variable, @"^[a-zA-Z_]\w*$"))
                {
                    if (_variableTypes.TryGetValue(variable, out string varType))
                    {
                        if (varType != "boolean")
                        {
                            errorMsg = $"Variable '{variable}' is of type '{varType}', expected 'boolean'";
                            return false;
                        }
                    }
                    else
                    {
                        errorMsg = $"Variable '{variable}' not found in current scope";
                        return false;
                    }
                }
                else if (variable != "true" && variable != "false")
                {
                    errorMsg = $"Left side of condition must be a boolean variable or literal";
                    return false;
                }

                if (op != "equal to" && op != "different from")
                {
                    errorMsg = $"Invalid operator for boolean: '{op}'";
                    return false;
                }

                // בדוק שהערך הוא בוליאני
                if (Regex.IsMatch(value, @"^[a-zA-Z_]\w*$"))
                {
                    if (_variableTypes.TryGetValue(value, out string varType))
                    {
                        if (varType != "boolean")
                        {
                            errorMsg = $"Variable '{value}' is of type '{varType}', expected 'boolean'";
                            return false;
                        }
                    }
                    else
                    {
                        errorMsg = $"Variable '{value}' not found in current scope";
                        return false;
                    }
                }
                else if (value != "true" && value != "false")
                {
                    errorMsg = $"Expected boolean literal after operator. Got: '{value}'";
                    return false;
                }

                return true;
            }

            errorMsg = "Invalid boolean condition syntax";
            return false;
        }

        private bool ValidateNumericCondition(List<string> tokens, out string errorMsg)
        {
            errorMsg = null;

            int opIndex = FindLogicOperatorIndex(tokens);
            if (opIndex == -1)
            {
                errorMsg = "Missing or invalid logical operator";
                return false;
            }

            string op = tokens[opIndex];
            if (!_logicOps.ContainsKey(op))
            {
                errorMsg = $"Invalid logical operator: {op}";
                return false;
            }

            var leftTokens = tokens.Take(opIndex).ToList();
            var rightTokens = tokens.Skip(opIndex + 1).ToList();

            // בדוק שצד שמאל הוא ביטוי מספרי או משתנה מספרי
            if (!ValidateMathSide(leftTokens, "numeric", out errorMsg)) return false;

            // בדוק שצד ימין הוא ביטוי מספרי או משתנה מספרי
            if (!ValidateMathSide(rightTokens, "numeric", out errorMsg)) return false;

            return true;
        }

        private bool ValidateTextCondition(List<string> tokens, out string errorMsg)
        {
            errorMsg = null;

            int opIndex = FindLogicOperatorIndex(tokens);
            if (opIndex == -1)
            {
                errorMsg = "Missing or invalid logical operator";
                return false;
            }

            string op = tokens[opIndex];
            if (op != "equal to" && op != "different from")
            {
                errorMsg = $"Only 'equal to' and 'different from' are allowed for text";
                return false;
            }

            var left = tokens.Take(opIndex).ToList();
            var right = tokens.Skip(opIndex + 1).ToList();

            // בדוק שצד שמאל הוא משתנה מסוג טקסט
            if (left.Count != 1 || !Regex.IsMatch(left[0], @"^[a-zA-Z_]\w*$"))
            {
                errorMsg = $"Invalid left side text operand: '{string.Join(" ", left)}'";
                return false;
            }

            // בדוק שטיפוס המשתנה הוא טקסט
            if (_variableTypes.TryGetValue(left[0], out string leftVarType) && leftVarType != "text")
            {
                errorMsg = $"Variable '{left[0]}' is of type '{leftVarType}', expected 'text'";
                return false;
            }

            // בדוק שצד ימין הוא סטרינג ליטרל או משתנה מסוג טקסט
            if (right.Count != 1)
            {
                errorMsg = $"Invalid right side text operand: '{string.Join(" ", right)}'";
                return false;
            }

            if (Regex.IsMatch(right[0], @"^[a-zA-Z_]\w*$"))
            {
                // אם זה משתנה, בדוק שהוא מסוג טקסט
                if (_variableTypes.TryGetValue(right[0], out string rightVarType))
                {
                    if (rightVarType != "text")
                    {
                        errorMsg = $"Variable '{right[0]}' is of type '{rightVarType}', expected 'text'";
                        return false;
                    }
                }
                else
                {
                    errorMsg = $"Variable '{right[0]}' not found in current scope";
                    return false;
                }
            }
            else if (!Regex.IsMatch(right[0], "^\".*\"$"))
            {
                errorMsg = $"Expected string literal or text variable after operator";
                return false;
            }

            return true;
        }

        private int FindLogicOperatorIndex(List<string> tokens)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                if (_logicOps.ContainsKey(tokens[i]))
                    return i;
            }
            return -1;
        }

        private bool ValidateMathSide(List<string> tokens, string expectedType, out string errorMsg)
        {
            errorMsg = null;
            bool expectValue = true;

            if (tokens.Count == 1)
            {
                string tok = tokens[0];
                if (Regex.IsMatch(tok, @"^-?\d+(\.\d+)?$"))
                {
                    // זהו ליטרל מספרי - בסדר
                    return true;
                }
                else if (Regex.IsMatch(tok, @"^[a-zA-Z_]\w*$"))
                {
                    // בדוק את טיפוס המשתנה
                    if (_variableTypes.TryGetValue(tok, out string varType))
                    {
                        if (varType == "integer" || varType == "decimal")
                        {
                            // משתנה מספרי - בסדר
                            return true;
                        }
                        else
                        {
                            errorMsg = $"Variable '{tok}' is of type '{varType}', expected numeric type (integer or decimal)";
                            return false;
                        }
                    }
                    else
                    {
                        errorMsg = $"Variable '{tok}' not found in current scope";
                        return false;
                    }
                }

                errorMsg = $"Expected number or variable, got '{tok}'";
                return false;
            }

            foreach (var tok in tokens)
            {
                if (expectValue)
                {
                    if (Regex.IsMatch(tok, @"^-?\d+(\.\d+)?$"))
                    {
                        // ליטרל מספרי - בסדר
                    }
                    else if (Regex.IsMatch(tok, @"^[a-zA-Z_]\w*$"))
                    {
                        // בדוק את טיפוס המשתנה
                        if (_variableTypes.TryGetValue(tok, out string varType))
                        {
                            if (varType != "integer" && varType != "decimal")
                            {
                                errorMsg = $"Variable '{tok}' is of type '{varType}', expected numeric type (integer or decimal)";
                                return false;
                            }
                        }
                        else
                        {
                            errorMsg = $"Variable '{tok}' not found in current scope";
                            return false;
                        }
                    }
                    else
                    {
                        errorMsg = $"Expected value or variable, got '{tok}'";
                        return false;
                    }
                }
                else
                {
                    if (!new[] { "add", "sub", "mul", "div" }.Contains(tok))
                    {
                        errorMsg = $"Expected operator, got '{tok}'";
                        return false;
                    }
                }
                expectValue = !expectValue;
            }

            if (expectValue)
            {
                errorMsg = "Expression ends with operator";
                return false;
            }

            return true;
        }

        public string ConvertToCSharp(string condition)
        {
            foreach (var op in _logicOps.OrderByDescending(o => o.Key.Length))
            {
                condition = Regex.Replace(condition, $@"\b{Regex.Escape(op.Key)}\b", _logicOps[op.Key]);
            }

            condition = Regex.Replace(condition, @"\badd\b", "+");
            condition = Regex.Replace(condition, @"\bsub\b", "-");
            condition = Regex.Replace(condition, @"\bmul\b", "*");
            condition = Regex.Replace(condition, @"\bdiv\b", "/");

            return condition;
        }
    }
}