using PoliteCode.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static PoliteCode.Tokenizer;

namespace PoliteCode
{
    /// <summary>
    /// אחראי על ניתוח הטוקנים ויצירת קוד C#
    /// </summary>
    public class Parser
    {
        // מצבי ניתוח
        public enum ParserState
        {
            Start,      // מצב התחלתי
            Declaration, // הצהרת משתנה
            Statement,   // הדפסה או הקצאה
            Condition,   // תנאי if
            Loop,        // לולאה
            End          // סוף ההוראה
        }

        // משתנים ומצב
        private readonly Tokenizer _tokenizer;
        private readonly CodeGenerator _codeGenerator;
        private readonly PoliteCodeTools _tools;

        // בודקי ביטויים משופרים
        private ExpressionValidator _expressionValidator;
        private ExpressionEvaluator _expressionEvaluator;

        // ניהול משתנים ותחומים
        private Stack<Dictionary<string, string>> _variableScopes = new Stack<Dictionary<string, string>>();

        // שם פונקציה -> סוג החזרה
        private Dictionary<string, string> _functionMap = new Dictionary<string, string>();

        // מעקב אחרי פקודות החזרה בפונקציות
        private Dictionary<string, bool> _functionHasBaseReturn = new Dictionary<string, bool>();
        private bool _insideControlBlock = false;

        // פלט
        private StringBuilder _csharpCode = new StringBuilder();

        // לעיבוד רב-שורתי
        private List<string> _lines = new List<string>();
        private int _currentLineIndex = 0;
        private int _indentationLevel = 0;
        private Stack<int> _blockStartIndices = new Stack<int>();

        // מעקב אחרי הפונקציה הנוכחית
        private string _currentFunctionName = null;

        // מילון המקשר כל סוג טוקן לפונקציית העיבוד שלו
        private Dictionary<TokenType, Func<List<TokenType>, List<string>, string>> _statementProcessors;

        /// <summary>
        /// בנאי עבור Parser
        /// </summary>
        public Parser(Tokenizer tokenizer, CodeGenerator codeGenerator, PoliteCodeTools tools)
        {
            _tokenizer = tokenizer;
            _codeGenerator = codeGenerator;
            _tools = tools;

            // אתחול בודקי הביטויים
            _expressionValidator = new ExpressionValidator();
            _expressionEvaluator = new ExpressionEvaluator();

            InitializeProcessors();
        }

        /// <summary>
        /// אתחול המילון המקשר סוגי טוקנים לפונקציות העיבוד שלהם
        /// </summary>
        private void InitializeProcessors()
        {
            _statementProcessors = new Dictionary<TokenType, Func<List<TokenType>, List<string>, string>>
            {
                { TokenType.Create, ProcessVariableDeclaration },
                { TokenType.Print, ProcessPrintStatement },
                { TokenType.Loop, ProcessLoopStatement },
                { TokenType.IfCondition, ProcessIfStatement },
                { TokenType.VariableName, ProcessAssignment },
                { TokenType.DefineFunction, ProcessFunctionDefinition },
                { TokenType.Return, ProcessReturnStatement },
                { TokenType.CallFunction, ProcessCallFunc }

            };
        }

        /// <summary>
        /// איפוס מצב המפענח לקומפילציה חדשה
        /// </summary>
        public void Reset()
        {
            _csharpCode.Clear();
            _lines.Clear();
            _currentLineIndex = 0;
            _variableScopes.Clear();
            PushScope();
            _functionMap.Clear();
            _functionHasBaseReturn.Clear();
            _indentationLevel = 0;
            _blockStartIndices.Clear();
            _currentFunctionName = null;
            _insideControlBlock = false;

            // איפוס בודקי הביטויים עם טיפוסי משתנים ריקים
            _expressionValidator.SetVariableTypes(new Dictionary<string, string>());
            _expressionEvaluator.SetVariableTypes(new Dictionary<string, string>());
        }

        /// <summary>
        /// הגדרת שורות הקוד לעיבוד
        /// </summary>
        /// <param name="inputLines">מערך של שורות קלט</param>
        /// <returns>True אם הקלט תקין, False אחרת</returns>
        public bool SetInput(string[] inputLines)
        {
            foreach (string line in inputLines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    _lines.Add(line.Trim());
            }

            return _lines.Count > 0;
        }

        /// <summary>
        /// התחלת עיבוד הקוד שורה אחר שורה
        /// </summary>
        /// <returns>True אם הקומפילציה הצליחה</returns>
        public bool ProcessCode()
        {
            ProcessNextLine();

            // אחרי עיבוד כל השורות, בדוק אם יש פונקציית void main
            if (!_functionMap.TryGetValue("main", out string returnType) || returnType != "void")
            {
                _tools.ShowError("Missing required entry point: 'please define function void main() {'. A void main function must be defined.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// עיבוד השורה הבאה של הקוד
        /// </summary>
        private void ProcessNextLine()
        {
            if (_currentLineIndex >= _lines.Count)
            {
                // סיימנו לעבד את כל השורות
                ValidateBraceBalance();
                return;
            }

            string currentLine = _lines[_currentLineIndex];

            // במקרה של סגירת בלוק (סוגר מסולסל סוגר)
            if (currentLine.Trim() == "}")
            {
                if (_blockStartIndices.Count == 0)
                {
                    _tools.ShowError($"Unexpected closing curly brace '}}' at line {_currentLineIndex + 1}. No matching opening brace.");
                    _currentLineIndex++;
                    ProcessNextLine();
                    return;
                }

                _blockStartIndices.Pop();
                PopScope();
                _indentationLevel = Math.Max(0, _indentationLevel - 1);
                _csharpCode.AppendLine(new string(' ', _indentationLevel * 4) + "}");

                // אם יצאנו מרמת ההזחה הראשונה, אנחנו יוצאים מפונקציה
                if (_indentationLevel == 0 && _currentFunctionName != null)
                {
                    // בדיקה אם הפונקציה צריכה להחזיר ערך ואין לה הוראת return ברמה הבסיסית
                    if (_functionMap.TryGetValue(_currentFunctionName, out string returnType) &&
                        returnType != "void" &&
                        !_functionHasBaseReturn[_currentFunctionName])
                    {
                        _tools.ShowError($"Function '{_currentFunctionName}' must have a return statement outside of any control blocks to ensure a value is always returned.");
                    }

                    _currentFunctionName = null;
                }

                _insideControlBlock = _indentationLevel > 1; // עדכון מצב הבקרה - אם עדיין בתוך בלוק כלשהו

                _currentLineIndex++;
                ProcessNextLine();
                return;
            }

            // טוקניזציה ועיבוד השורה
            List<string> lineTokens = _tokenizer.TokenizeInput(currentLine);
            List<TokenType> tokens = _tokenizer.ProcessTokens(lineTokens);

            if (tokens == null || tokens.Count == 0)
            {
                _currentLineIndex++;
                ProcessNextLine();
                return;
            }

            // בדיקה אם פותחים בלוק בקרה חדש (if, for, while)
            bool isControlBlockStart = lineTokens.Contains("{") &&
                                     (tokens[0] == TokenType.IfCondition || tokens[0] == TokenType.Loop);

            if (isControlBlockStart)
            {
                _insideControlBlock = true;
            }

            // בדיקה אם יש פתיחת בלוק
            bool containsOpenBrace = lineTokens.Contains("{");

            // ניתוח ההוראה עבור שורה זו
            ParseStatementForLine(tokens, lineTokens);

            _currentLineIndex++;

            // אם יש פתיחת בלוק חדש, הגדל את ההזחה ופתח תחום חדש
            if (containsOpenBrace)
            {
                _indentationLevel++;
                _blockStartIndices.Push(_currentLineIndex);
                PushScope(); // תחום חדש עבור הבלוק
            }

            ProcessNextLine(); // קריאה רקורסיבית לעיבוד השורה הבאה
        }

        /// <summary>
        /// אימות שכל הסוגריים מאוזנים
        /// </summary>
        private void ValidateBraceBalance()
        {
            // בדיקה אם יש בלוקים שלא נסגרו (סוגריים)
            if (_blockStartIndices.Count > 0)
            {
                int firstUnclosedBlock = _blockStartIndices.Pop();
                _tools.ShowError($"Missing closing curly brace '}}'. Block starting at line {firstUnclosedBlock} is not properly closed.");
                return;
            }

            // אם הכל תקין, בנינו בהצלחה את קוד ה-C#
        }

        /// <summary>
        /// ניתוח הוראה מטוקנים ומחרוזות
        /// </summary>
        /// <param name="tokens">רשימת סוגי טוקנים</param>
        /// <param name="inputTokens">רשימת מחרוזות טוקן</param>
        private void ParseStatementForLine(List<TokenType> tokens, List<string> inputTokens)
        {
            if (tokens.Count == 0)
                return;

            string indentation = new string(' ', _indentationLevel * 4);
            StringBuilder statementCode = new StringBuilder();

            // שימוש במפענח לעיבוד ההוראה
            TokenType firstToken = tokens[0];

            if (_statementProcessors.TryGetValue(firstToken, out var processor))
            {
                // מציאת והפעלת המעבד המתאים בהתבסס על הטוקן הראשון
                string processedCode = processor(tokens, inputTokens);
                statementCode.Append(processedCode);
            }
            else
            {
                _tools.ShowError("Unknown command: " + inputTokens[0]);
                return;
            }

            // הוספת ההוראה לקוד ה-C# עם הזחה מתאימה
            if (statementCode.Length > 0)
            {
                string[] lines = statementCode.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    _csharpCode.AppendLine(indentation + line);
                }
            }

            // הצגת הודעת התקדמות
            _tools.ShowInfo($"Processed line {_currentLineIndex + 1}: {_lines[_currentLineIndex]}");
        }

        #region מעבדי הוראות

        /// <summary>
        /// עיבוד הוראת הצהרת משתנה
        /// </summary>
        /// <param name="tokens">רשימת סוגי טוקנים</param>
        /// <param name="inputTokens">רשימת מחרוזות טוקן</param>
        /// <returns>קוד C# מיוצר</returns>
        private string ProcessVariableDeclaration(List<TokenType> tokens, List<string> inputTokens)
        {
            if (tokens.Count < 3)
            {
                _tools.ShowError("Incomplete variable declaration");
                return string.Empty;
            }

            if (tokens[1] != TokenType.VariableType)
            {
                _tools.ShowError("Missing variable type in declaration");
                return string.Empty;
            }

            if (tokens[2] != TokenType.VariableName)
            {
                _tools.ShowError("Missing variable name in declaration");
                return string.Empty;
            }

            string variableName = inputTokens[2];

            if (VariableExists(variableName))
            {
                _tools.ShowError($"Variable '{variableName}' cannot be created because it conflicts with an existing variable.");
                return string.Empty;
            }

            if (_functionMap.ContainsKey(variableName))
            {
                _tools.ShowError($"Variable '{variableName}' cannot be created because it conflicts with an existing function.");
                return string.Empty;
            }

            string variableTypePolite = inputTokens[1];
            string csharpType = _tools.TranslateType(variableTypePolite);
            string code = $"{csharpType} {variableName}";

            if (tokens.Count > 3 && tokens[3] == TokenType.Equals)
            {
                if (tokens.Count < 5)
                {
                    _tools.ShowError("Missing value after 'equals' in variable declaration");
                    return string.Empty;
                }

                // בניית הביטוי המלא
                string expression = string.Join(" ", inputTokens.Skip(4));

                // עדכון מילוני הטיפוסים של הבודקים
                var allVariableTypes = GetAllVariableTypes();
                _expressionValidator.SetVariableTypes(allVariableTypes);
                _expressionEvaluator.SetVariableTypes(allVariableTypes);

                // שלב 1: בדיקת תקינות הביטוי לפי הסוג
                if (!_expressionValidator.IsValid(expression, variableTypePolite, out string errorMsg))
                {
                    _tools.ShowError($"Invalid initialization expression: {errorMsg}");
                    return string.Empty;
                }

                // שלב 2: בדיקה שכל המשתנים בביטוי קיימים
                var tokensInExpr = expression.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var token in tokensInExpr)
                {
                    bool isNumber = Regex.IsMatch(token, @"^-?\d+(\.\d+)?$");
                    bool isOperator = _tokenizer.MathWordToSymbol.ContainsKey(token);
                    bool isString = token.StartsWith("\"") && token.EndsWith("\"");
                    bool isBooleanLiteral = token == "true" || token == "false";

                    if (!isNumber && !isOperator && !isString && !isBooleanLiteral && !VariableExists(token))
                    {
                        _tools.ShowError($"Variable '{token}' used in expression but was not declared.");
                        return string.Empty;
                    }
                }

                // שלב 3: תרגום לביטוי C#
                string csharpExpression = _codeGenerator.ConvertExpressionToCSharp(expression);
                code = $"{csharpType} {variableName} = {csharpExpression}";
            }

            DeclareVariable(variableName, variableTypePolite);

            return code + ";";
        }

        /// <summary>
        /// עיבוד הוראת הקצאת משתנה (כולל קריאה לפונקציה)
        /// </summary>
        /// <param name="tokens">רשימת סוגי טוקנים</param>
        /// <param name="inputTokens">רשימת מחרוזות טוקן</param>
        /// <returns>קוד C# מיוצר</returns>
        private string ProcessAssignment(List<TokenType> tokens, List<string> inputTokens)
        {
            if (tokens.Count < 3)
            {
                _tools.ShowError("Invalid assignment. Expected format: [variable] equals [expression]");
                return string.Empty;
            }

            string variableName = inputTokens[0];

            if (_functionMap.ContainsKey(variableName))
            {
                _tools.ShowError($"Cannot assign value to '{variableName}' because it is a function name.");
                return string.Empty;
            }

            // בדיקה אם המשתנה קיים
            if (!VariableExists(variableName))
            {
                _tools.ShowError($"Variable '{variableName}' does not exist");
                return string.Empty;
            }

            if (tokens[1] != TokenType.Equals)
            {
                _tools.ShowError("Missing 'equals' keyword after variable name");
                return string.Empty;
            }

            TryGetVariableType(variableName, out string currentType);

            // בדיקה אם זו הקצאה עם קריאה לפונקציה (equals please call...)
            if (inputTokens.Count > 3 && inputTokens[2] == "please call")
            {
                // וידוא שיש מספיק טוקנים
                if (inputTokens.Count < 5)
                {
                    _tools.ShowError("Invalid function call format after 'equals please call'");
                    return string.Empty;
                }

                string functionName = inputTokens[3];

                // בדיקה אם הפונקציה קיימת
                if (!_functionMap.ContainsKey(functionName))
                {
                    _tools.ShowError($"Function '{functionName}' does not exist or was not declared.");
                    return string.Empty;
                }

                // וידוא התאמת טיפוסים בין הפונקציה למשתנה
                if (_functionMap.TryGetValue(functionName, out string returnType))
                {
                    if (returnType != currentType && returnType != "void")
                    {
                        // התראה על אי-התאמת טיפוסים
                        _tools.ShowError($"Type mismatch: Function '{functionName}' returns '{returnType}' but variable '{variableName}' is of type '{currentType}'");
                        return string.Empty;
                    }

                    if (returnType == "void")
                    {
                        _tools.ShowError($"Cannot assign result of void function '{functionName}' to variable '{variableName}'");
                        return string.Empty;
                    }
                }

                // וידוא שיש סוגר פתיחה
                int openParenIndex = inputTokens.IndexOf("(");
                if (openParenIndex == -1 || openParenIndex <= 3)
                {
                    _tools.ShowError($"Missing opening parenthesis after function name '{functionName}'");
                    return string.Empty;
                }

                // חיפוש הסוגר הסוגר
                int closeParenIndex = -1;
                int openParenCount = 0;

                for (int i = openParenIndex; i < inputTokens.Count; i++)
                {
                    if (inputTokens[i] == "(")
                    {
                        openParenCount++;
                    }
                    else if (inputTokens[i] == ")")
                    {
                        openParenCount--;
                        if (openParenCount == 0)
                        {
                            closeParenIndex = i;
                            break;
                        }
                    }
                }

                if (closeParenIndex == -1)
                {
                    _tools.ShowError("Missing closing parenthesis in function call");
                    return string.Empty;
                }

                // עיבוד הפרמטרים
                List<string> parameters = new List<string>();

                if (closeParenIndex > openParenIndex + 1)
                {
                    // קבלת טקסט הפרמטרים
                    string paramText = string.Join(" ", inputTokens.Skip(openParenIndex + 1).Take(closeParenIndex - openParenIndex - 1));

                    // פיצול פרמטרים לפי פסיקים
                    string[] paramArr = paramText.Split(',');

                    foreach (var param in paramArr)
                    {
                        string trimmedParam = param.Trim();

                        // בדיקה אם הפרמטר הוא ביטוי מורכב
                        if (trimmedParam.Contains(" "))
                        {
                            trimmedParam = _codeGenerator.ConvertExpressionToCSharp(trimmedParam);
                        }
                        // בדיקה אם הפרמטר הוא משתנה
                        else if (Regex.IsMatch(trimmedParam, @"^[a-zA-Z_]\w*$") && !trimmedParam.StartsWith("\""))
                        {
                            if (!VariableExists(trimmedParam))
                            {
                                _tools.ShowError($"Parameter '{trimmedParam}' is not a declared variable.");
                                return string.Empty;
                            }
                        }

                        if (!string.IsNullOrEmpty(trimmedParam))
                        {
                            parameters.Add(trimmedParam);
                        }
                    }
                }

                // יצירת קוד C# לקריאה לפונקציה
                return $"{variableName} = {functionName}({string.Join(", ", parameters)});";
            }

            // אחרת, מדובר בהקצאה רגילה
            // עדכון מילוני הטיפוסים של הבודקים
            var allVariableTypes = GetAllVariableTypes();
            _expressionValidator.SetVariableTypes(allVariableTypes);
            _expressionEvaluator.SetVariableTypes(allVariableTypes);

            // בניית ביטוי מהטוקן השלישי ואילך
            string expression = string.Join(" ", inputTokens.Skip(2));

            expression = Regex.Replace(expression, @"(add|sub|mul|div)(\d+)", "$1 $2");

            if (!_expressionValidator.IsValid(expression, currentType, out string errorMsg))
            {
                _tools.ShowError($"Invalid expression: {errorMsg}");
                return string.Empty;
            }

            return $"{variableName} = {_codeGenerator.ConvertExpressionToCSharp(expression)};";
        }

        /// <summary>
        /// עיבוד הוראת הדפסה
        /// </summary>
        /// <param name="tokens">רשימת סוגי טוקנים</param>
        /// <param name="inputTokens">רשימת מחרוזות טוקן</param>
        /// <returns>קוד C# מיוצר</returns>
        private string ProcessPrintStatement(List<TokenType> tokens, List<string> inputTokens)
        {
            if (tokens.Count < 2)
            {
                _tools.ShowError("Missing text to print");
                return string.Empty;
            }

            // בדיקה אם יש את המילה "add" בטוקנים
            bool hasAddOperator = inputTokens.Contains("add");

            if (hasAddOperator)
            {
                // יצירת ביטוי מלא עבור הדפסה
                StringBuilder expression = new StringBuilder();

                // הוספת הטוקן הראשון (אחרי "thank you for printing")
                if (tokens[1] == TokenType.StringLiteral)
                {
                    expression.Append(inputTokens[1]);
                }
                else if (tokens[1] == TokenType.VariableName)
                {
                    if (!VariableExists(inputTokens[1]))
                    {
                        _tools.ShowError($"Variable '{inputTokens[1]}' does not exist");
                        return string.Empty;
                    }
                    expression.Append(inputTokens[1]);
                }

                // טיפול בשאר הטוקנים
                for (int i = 2; i < inputTokens.Count; i++)
                {
                    if (inputTokens[i] == "add")
                    {
                        // להוסיף אופרטור + במקום add
                        expression.Append(" + ");
                    }
                    else if (tokens[i] == TokenType.VariableName)
                    {
                        // בדיקה שהמשתנה קיים (אבל לא כשמדובר ב-add שהוא אופרטור)
                        if (!VariableExists(inputTokens[i]))
                        {
                            _tools.ShowError($"Variable '{inputTokens[i]}' does not exist");
                            return string.Empty;
                        }
                        expression.Append(inputTokens[i]);
                    }
                    else if (tokens[i] == TokenType.StringLiteral)
                    {
                        expression.Append(inputTokens[i]);
                    }
                    else
                    {
                        // טוקנים אחרים
                        expression.Append(inputTokens[i]);
                    }
                }

                // החזרת קוד C# להדפסת הביטוי
                return $"Console.WriteLine({expression.ToString()});";
            }
            else
            {
                // טיפול במקרה פשוט - הדפסת מחרוזת או משתנה בודד
                string valueToPrint;

                if (tokens[1] == TokenType.StringLiteral)
                {
                    valueToPrint = inputTokens[1];
                }
                else if (tokens[1] == TokenType.VariableName)
                {
                    if (!VariableExists(inputTokens[1]))
                    {
                        _tools.ShowError($"Variable '{inputTokens[1]}' does not exist");
                        return string.Empty;
                    }
                    valueToPrint = inputTokens[1];
                }
                else
                {
                    _tools.ShowError("Invalid print value. Expected string or variable name.");
                    return string.Empty;
                }

                return $"Console.WriteLine({valueToPrint});";
            }
        }

        /// <summary>
        /// עיבוד הוראת לולאה (for או while)
        /// </summary>
        /// <param name="tokens">רשימת סוגי טוקנים</param>
        /// <param name="inputTokens">רשימת מחרוזות טוקן</param>
        /// <returns>קוד C# מיוצר</returns>
        private string ProcessLoopStatement(List<TokenType> tokens, List<string> inputTokens)
        {
            if (tokens.Count < 2)
            {
                _tools.ShowError("Incomplete loop statement");
                return string.Empty;
            }

            if (inputTokens.Contains("from"))
            {
                return ProcessForLoop(tokens, inputTokens);
            }
            else if (inputTokens.Contains("while"))
            {
                return ProcessWhileLoop(tokens, inputTokens);
            }
            else
            {
                _tools.ShowError("Not clear which loop type to use. Use 'from' for for-loops or 'while' for while-loops.");
                return string.Empty;
            }
        }

        /// <summary>
        /// עיבוד הוראת לולאת for
        /// </summary>
        /// <param name="tokens">רשימת סוגי טוקנים</param>
        /// <param name="inputTokens">רשימת מחרוזות טוקן</param>
        /// <returns>קוד C# מיוצר</returns>
        private string ProcessForLoop(List<TokenType> tokens, List<string> inputTokens)
        {
            // מציאת האינדקסים למילות מפתח
            int fromIndex = inputTokens.IndexOf("from");
            int toIndex = inputTokens.IndexOf("to");

            if (fromIndex == -1 || toIndex == -1 || fromIndex >= toIndex)
            {
                _tools.ShowError("Invalid for-loop format. Expected 'thank you for looping from [start] to [end]'");
                return string.Empty;
            }

            // קבלת ערכי התחלה וסיום
            string startValue = inputTokens[fromIndex + 1];
            string endValue = inputTokens[toIndex + 1];

            // בדיקה אם הערכים תקינים
            if (string.IsNullOrEmpty(startValue) || string.IsNullOrEmpty(endValue))
            {
                _tools.ShowError("Missing start or end value in for-loop");
                return string.Empty;
            }

            // בדיקה אם יש סוגר פתיחה (חובה)
            if (!inputTokens.Contains("{"))
            {
                _tools.ShowError("Missing opening brace '{' in for-loop. Please add it at the end of your loop statement.");
                return string.Empty;
            }

            // יצירת קוד C#
            StringBuilder code = new StringBuilder();
            code.AppendLine($"for (int i = {startValue}; i <= {endValue}; i++)");
            code.AppendLine("{");

            return code.ToString();
        }

        /// <summary>
        /// עיבוד הוראת לולאת while
        /// </summary>
        /// <param name="tokens">רשימת סוגי טוקנים</param>
        /// <param name="inputTokens">רשימת מחרוזות טוקן</param>
        /// <returns>קוד C# מיוצר</returns>
        private string ProcessWhileLoop(List<TokenType> tokens, List<string> inputTokens)
        {
            int whileIndex = inputTokens.IndexOf("while");

            if (whileIndex == -1 || whileIndex + 1 >= inputTokens.Count)
            {
                _tools.ShowError("Invalid while-loop format. Expected 'thank you for looping while [condition]'");
                return string.Empty;
            }

            // בניית הביטוי המלא אחרי מילת המפתח "while"
            string conditionExpression = string.Join(" ", inputTokens.Skip(whileIndex + 1)
                                                                     .TakeWhile(token => token != "{"))
                                                .Trim();

            if (string.IsNullOrWhiteSpace(conditionExpression))
            {
                _tools.ShowError("Missing condition in while loop");
                return string.Empty;
            }

            if (!inputTokens.Contains("{"))
            {
                _tools.ShowError("Missing opening brace '{' in while-loop. Please add it at the end.");
                return string.Empty;
            }

            // ניסיון לקבל את שם המשתנה הראשון ובדיקת הסוג שלו
            string firstToken = inputTokens[whileIndex + 1];
            if (!VariableExists(firstToken) || !TryGetVariableType(firstToken, out string varType))
            {
                _tools.ShowError($"Unknown or undeclared variable: '{firstToken}'");
                return string.Empty;
            }

            // עדכון מילוני הטיפוסים של הבודקים
            var allVariableTypes = GetAllVariableTypes();
            _expressionValidator.SetVariableTypes(allVariableTypes);
            _expressionEvaluator.SetVariableTypes(allVariableTypes);

            if (!_expressionEvaluator.IsValidCondition(conditionExpression, varType, out string errorMsg))
            {
                _tools.ShowError("Invalid condition syntax in while-loop: " + errorMsg);
                return string.Empty;
            }

            // תרגום לפורמט C#
            string csharpCondition = _codeGenerator.ConvertConditionToCSharp(conditionExpression);

            var code = new StringBuilder();
            code.AppendLine($"while ({csharpCondition})");
            code.AppendLine("{");

            return code.ToString();
        }

        /// <summary>
        /// עיבוד הוראת if
        /// </summary>
        /// <param name="tokens">רשימת סוגי טוקנים</param>
        /// <param name="inputTokens">רשימת מחרוזות טוקן</param>
        /// <returns>קוד C# מיוצר</returns>
        private string ProcessIfStatement(List<TokenType> tokens, List<string> inputTokens)
        {
            int ifIndex = inputTokens.IndexOf("thank you for checking if");

            if (ifIndex == -1 || ifIndex + 1 >= inputTokens.Count)
            {
                _tools.ShowError("Invalid if statement format. Expected 'thank you for checking if [condition]'");
                return string.Empty;
            }

            string conditionExpression = string.Join(" ", inputTokens.Skip(ifIndex + 1)
                                                                     .TakeWhile(token => token != "{"))
                                                .Trim();

            if (string.IsNullOrWhiteSpace(conditionExpression))
            {
                _tools.ShowError("Missing condition in if statement");
                return string.Empty;
            }

            if (!inputTokens.Contains("{"))
            {
                _tools.ShowError("Missing opening brace '{' in if statement. Please add it at the end.");
                return string.Empty;
            }

            string firstToken = inputTokens[ifIndex + 1];
            if (!VariableExists(firstToken) || !TryGetVariableType(firstToken, out string varType))
            {
                _tools.ShowError($"Unknown or undeclared variable: '{firstToken}'");
                return string.Empty;
            }

            // עדכון מילוני הטיפוסים של הבודקים
            var allVariableTypes = GetAllVariableTypes();
            _expressionValidator.SetVariableTypes(allVariableTypes);
            _expressionEvaluator.SetVariableTypes(allVariableTypes);

            if (!_expressionEvaluator.IsValidCondition(conditionExpression, varType, out string errorMsg))
            {
                _tools.ShowError("Invalid condition syntax in if-statement: " + errorMsg);
                return string.Empty;
            }

            string csharpCondition = _codeGenerator.ConvertConditionToCSharp(conditionExpression);

            var code = new StringBuilder();
            code.AppendLine($"if ({csharpCondition})");
            code.AppendLine("{");

            return code.ToString();
        }

        /// <summary>
        /// עיבוד הוראת החזרה (return)
        /// </summary>
        /// <param name="tokens">רשימת סוגי טוקנים</param>
        /// <param name="inputTokens">רשימת מחרוזות טוקן</param>
        /// <returns>קוד C# מיוצר</returns>
        private string ProcessReturnStatement(List<TokenType> tokens, List<string> inputTokens)
        {
            // בדיקה אם אנחנו בתוך פונקציה
            if (_currentFunctionName == null)
            {
                _tools.ShowError("'thank you for returning' ניתן להשתמש רק בתוך פונקציה");
                return string.Empty;
            }

            // מציאת סוג ההחזרה של הפונקציה הנוכחית
            if (!_functionMap.TryGetValue(_currentFunctionName, out string expectedReturnType))
            {
                _tools.ShowError($"לא ניתן לקבוע את סוג ההחזרה של הפונקציה '{_currentFunctionName}'");
                return string.Empty;
            }

            // אם הפונקציה מסוג void, אין להחזיר ערך
            if (expectedReturnType == "void")
            {
                if (tokens.Count > 1)
                {
                    _tools.ShowError("פונקציות מסוג void לא יכולות להחזיר ערך");
                    return string.Empty;
                }
                return "return;";
            }

            // לפונקציות שאינן void, צריך ערך החזרה
            if (tokens.Count < 2)
            {
                _tools.ShowError($"פונקציה '{_currentFunctionName}' צריכה להחזיר ערך מסוג '{expectedReturnType}'");
                return string.Empty;
            }

            // בניית ביטוי ההחזרה מהטוקנים אחרי "thank you for returning"
            string returnExpression = string.Join(" ", inputTokens.Skip(1));

            // עדכון מילוני הטיפוסים של הבודקים
            var allVariableTypes = GetAllVariableTypes();
            _expressionValidator.SetVariableTypes(allVariableTypes);
            _expressionEvaluator.SetVariableTypes(allVariableTypes);

            // בדיקה שטיפוס הביטוי תואם את טיפוס ההחזרה המצופה
            if (!_expressionValidator.IsValid(returnExpression, expectedReturnType, out string errorMsg))
            {
                _tools.ShowError($"ביטוי החזרה לא תקין: {errorMsg}");
                return string.Empty;
            }

            // אם ה-return נמצא ברמה הבסיסית של הפונקציה (לא בתוך בלוק שליטה)
            if (!_insideControlBlock)
            {
                _functionHasBaseReturn[_currentFunctionName] = true;
            }

            // תרגום לקוד C#
            string csharpExpression = _codeGenerator.ConvertExpressionToCSharp(returnExpression);
            return $"return {csharpExpression};";
        }

        private string ProcessCallFunc(List<TokenType> tokens, List<string> inputTokens)
        {
            // Finding the "please call" token (which is at index 0 in your case)
            int callIndex = 0;

            // בדיקה אם מדובר בהקצאה או בקריאה עצמאית
            bool isAssignment = false;
            string variableName = string.Empty;

            // מציאת שם הפונקציה (אחרי "please call")
            string functionName = inputTokens[callIndex + 1];

            // בדיקה אם הפונקציה קיימת
            if (!_functionMap.ContainsKey(functionName))
            {
                _tools.ShowError($"Function '{functionName}' does not exist or was not declared.");
                return string.Empty;
            }

            // בדיקת התאמת טיפוס החזרה לסוג המשתנה (אם יש הקצאה)
            if (isAssignment)
            {
                // קבלת סוג ההחזרה של הפונקציה
                if (_functionMap.TryGetValue(functionName, out string returnType))
                {
                    // אם זו פונקציית void, אי אפשר להקצות את התוצאה שלה למשתנה
                    if (returnType == "void")
                    {
                        _tools.ShowError($"Cannot assign result of void function '{functionName}' to variable '{variableName}'");
                        return string.Empty;
                    }

                    // בדיקת התאמת טיפוסים בין הפונקציה למשתנה
                    if (TryGetVariableType(variableName, out string variableType))
                    {
                        if (returnType != variableType)
                        {
                            _tools.ShowError($"Type mismatch: Function '{functionName}' returns '{returnType}' but variable '{variableName}' is of type '{variableType}'");
                            return string.Empty;
                        }
                    }
                }
            }

            // וידוא שיש סוגר פתיחה
            if (callIndex + 2 >= inputTokens.Count || inputTokens[callIndex + 2] != "(")
            {
                _tools.ShowError($"Missing opening parenthesis after function name '{functionName}'");
                return string.Empty;
            }

            // חיפוש הסוגר הסוגר
            int openParenIndex = callIndex + 2; // מיקום ה-"("
            int closeParenIndex = -1;

            // מחפש את הסוגר הסוגר המתאים
            for (int i = openParenIndex + 1; i < inputTokens.Count; i++)
            {
                if (inputTokens[i] == ")")
                {
                    closeParenIndex = i;
                    break;
                }
            }

            if (closeParenIndex == -1)
            {
                _tools.ShowError("Missing closing parenthesis in function call");
                return string.Empty;
            }

            // בניית רשימת הפרמטרים
            List<string> parameters = new List<string>();

            // בדיקה אם יש פרמטרים בין הסוגריים (אם יש תוכן בין ה-"(" וה-")")
            if (closeParenIndex > openParenIndex + 1)
            {
                // לוקח את כל הטוקנים בין הסוגריים
                string paramText = string.Join(" ", inputTokens.Skip(openParenIndex + 1).Take(closeParenIndex - openParenIndex - 1));

                // פיצול לפי פסיקים (אם יש יותר מפרמטר אחד)
                string[] paramList = paramText.Split(',');

                foreach (var param in paramList)
                {
                    string trimmedParam = param.Trim();

                    // בדיקה אם הפרמטר הוא ביטוי מורכב
                    if (trimmedParam.Contains(" "))
                    {
                        // המרת ביטוי PoliteCode לביטוי C#
                        trimmedParam = _codeGenerator.ConvertExpressionToCSharp(trimmedParam);
                    }
                    // בדיקה אם הפרמטר הוא משתנה
                    else if (Regex.IsMatch(trimmedParam, @"^[a-zA-Z_]\w*$") && !trimmedParam.StartsWith("\""))
                    {
                        if (!VariableExists(trimmedParam))
                        {
                            _tools.ShowError($"Parameter '{trimmedParam}' is not a declared variable.");
                            return string.Empty;
                        }
                    }

                    parameters.Add(trimmedParam);
                }
            }

            // יצירת קוד C# מתאים
            if (isAssignment)
            {
                return $"{variableName} = {functionName}({string.Join(", ", parameters)});";
            }
            else
            {
                return $"{functionName}({string.Join(", ", parameters)});";
            }
        }



        /// <summary>
        /// עיבוד הגדרת פונקציה
        /// </summary>
        /// <param name="tokens">רשימת סוגי טוקנים</param>
        /// <param name="inputTokens">רשימת מחרוזות טוקן</param>
        /// <returns>קוד C# מיוצר</returns>
        private string ProcessFunctionDefinition(List<TokenType> tokens, List<string> inputTokens)
        {
            if (tokens.Count < 6)
            {
                _tools.ShowError("Function definition is too short. Must include return type, name, parameters, and braces.");
                return string.Empty;
            }

            if (tokens[3] != TokenType.OpenParen)
            {
                _tools.ShowError("Expected '(' at position 3 in function definition");
                return string.Empty;
            }

            if (tokens[tokens.Count - 2] != TokenType.CloseParen)
            {
                _tools.ShowError("Expected ')' to be the second to last token");
                return string.Empty;
            }

            if (tokens[tokens.Count - 1] != TokenType.OpenBrace)
            {
                _tools.ShowError("Expected '{' to be the last token of the function header");
                return string.Empty;
            }

            string returnTypePolite = inputTokens[1];
            string functionName = inputTokens[2];

            // שמירת שם הפונקציה הנוכחית
            _currentFunctionName = functionName;

            // איפוס המצב של הפונקציה הנוכחית - אין לה עדיין הוראת return בבסיס הפונקציה
            _functionHasBaseReturn[functionName] = returnTypePolite == "void";

            if (_functionMap.ContainsKey(functionName))
            {
                _tools.ShowError($"Function '{functionName}' is already defined.");
                return string.Empty;
            }

            string returnTypeCSharp = _tools.TranslateType(returnTypePolite);

            // שמירת הפונקציה במפה
            _functionMap[functionName] = returnTypePolite;

            if (functionName == "main" && tokens.Count != 6)
            {
                _tools.ShowError("if you create the main function, the build is like this:" +
                    "\nplease define function main(){\n you cannot use any parameters!");
                return string.Empty;
            }

            // בניית פרמטרים ושמירה במפה
            List<string> parameters = new List<string>();
            _variableScopes.Clear();
            PushScope();

            for (int i = 4; i < tokens.Count - 2; i += 2)
            {
                if (i + 1 >= tokens.Count - 2)
                {
                    _tools.ShowError("Incomplete parameter pair in function definition");
                    return string.Empty;
                }

                if (tokens[i] != TokenType.VariableType || tokens[i + 1] != TokenType.VariableName)
                {
                    _tools.ShowError($"Invalid parameter at position {i}: expected [type name]");
                    return string.Empty;
                }

                string typePolite = inputTokens[i];
                string name = inputTokens[i + 1];

                if (_functionMap.ContainsKey(name))
                {
                    _tools.ShowError($"Parameter name '{name}' conflicts with an existing function name.");
                    return string.Empty;
                }

                if (VariableExists(name))
                {
                    _tools.ShowError($"Parameter name '{name}' conflicts with an existing variable.");
                    return string.Empty;
                }

                string typeCSharp = _tools.TranslateType(typePolite);
                parameters.Add($"{typeCSharp} {name}");

                // שמירה במפת משתנים
                DeclareVariable(name, typePolite);
            }

            // בניית הקוד
            var code = new StringBuilder();
            code.AppendLine($"public static {returnTypeCSharp} {functionName}({string.Join(", ", parameters)})");
            code.AppendLine("{");

            return code.ToString();
        }

        #endregion

        #region ניהול תחומים

        /// <summary>
        /// דחיפת תחום משתנים חדש למחסנית
        /// </summary>
        private void PushScope()
        {
            _variableScopes.Push(new Dictionary<string, string>());
        }

        /// <summary>
        /// שליפת תחום משתנים מהמחסנית
        /// </summary>
        private void PopScope()
        {
            if (_variableScopes.Count > 0)
                _variableScopes.Pop();
        }

        /// <summary>
        /// בדיקה אם משתנה קיים בתחום כלשהו
        /// </summary>
        /// <param name="name">שם המשתנה</param>
        /// <returns>True אם המשתנה קיים</returns>
        private bool VariableExists(string name)
        {
            return _variableScopes.Any(scope => scope.ContainsKey(name));
        }

        /// <summary>
        /// ניסיון לקבל את סוג המשתנה
        /// </summary>
        /// <param name="name">שם המשתנה</param>
        /// <param name="type">סוג הפלט</param>
        /// <returns>True אם נמצא משתנה</returns>
        private bool TryGetVariableType(string name, out string type)
        {
            foreach (var scope in _variableScopes)
            {
                if (scope.TryGetValue(name, out type))
                    return true;
            }

            type = null;
            return false;
        }

        /// <summary>
        /// הצהרה על משתנה בתחום הנוכחי
        /// </summary>
        /// <param name="name">שם המשתנה</param>
        /// <param name="type">סוג המשתנה</param>
        private void DeclareVariable(string name, string type)
        {
            if (_variableScopes.Count == 0)
                PushScope();

            _variableScopes.Peek()[name] = type;
        }

        /// <summary>
        /// קבלת כל טיפוסי המשתנים מכל התחומים לבדיקה
        /// </summary>
        private Dictionary<string, string> GetAllVariableTypes()
        {
            Dictionary<string, string> allTypes = new Dictionary<string, string>();

            // הוספת כל המשתנים מכל התחומים
            foreach (var scope in _variableScopes)
            {
                foreach (var pair in scope)
                {
                    if (!allTypes.ContainsKey(pair.Key))
                    {
                        allTypes.Add(pair.Key, pair.Value);
                    }
                }
            }

            return allTypes;
        }

        /// <summary>
        /// פיצול רשימת פרמטרים המופרדים בפסיקים, תוך התחשבות במחרוזות
        /// </summary>
        private List<string> SplitParameterList(string paramString)
        {
            List<string> result = new List<string>();
            bool insideString = false;
            int startIndex = 0;

            for (int i = 0; i < paramString.Length; i++)
            {
                char c = paramString[i];

                if (c == '"')
                {
                    insideString = !insideString;
                }
                else if (c == ',' && !insideString)
                {
                    result.Add(paramString.Substring(startIndex, i - startIndex));
                    startIndex = i + 1;
                }
            }

            // הוספת הפרמטר האחרון
            if (startIndex < paramString.Length)
            {
                result.Add(paramString.Substring(startIndex));
            }

            return result;
        }

        // עדכן את מתודת GetCSharpCode() שנמצאת בסוף הקובץ Parser.cs
        public string GetCSharpCode()
        {
            // הוסף את המעטפת הדרושה לקוד
            StringBuilder fullCode = new StringBuilder();

            // הוסף using statements
            fullCode.AppendLine("using System;");
            fullCode.AppendLine("using System.Collections.Generic;");
            fullCode.AppendLine("using System.Linq;");
            fullCode.AppendLine("using System.Text;");
            fullCode.AppendLine();

            // הוסף namespace
            fullCode.AppendLine("namespace PoliteCodeGenerated");
            fullCode.AppendLine("{");
            fullCode.AppendLine("    public class Program");
            fullCode.AppendLine("    {");

            // הוסף את הקוד המתורגם עם הזחה נוספת
            string[] lines = _csharpCode.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                // הוסף הזחה של 8 רווחים (כי הקוד כבר בהזחה של 4 רווחים פנימה בתוך הפונקציות)
                fullCode.AppendLine("        " + line);
            }

            // סגור את מחלקת Program והנפיימספייס
            fullCode.AppendLine("    }");
            fullCode.AppendLine("}");

            return fullCode.ToString();
        }

        #endregion
    }
}