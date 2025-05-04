using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PoliteCode
{
    /// <summary>
    /// אחראי על המרת ביטויי והוראות PoliteCode לקוד C#
    /// </summary>
    public class CodeGenerator
    {
        private readonly Tokenizer _tokenizer;

        /// <summary>
        /// בנאי ל-CodeGenerator
        /// </summary>
        /// <param name="tokenizer">מופע Tokenizer לשימוש</param>
        public CodeGenerator(Tokenizer tokenizer)
        {
            _tokenizer = tokenizer;
        }

        /// <summary>
        /// המרת ביטוי מתמטי מ-PoliteCode לתחביר C#
        /// </summary>
        /// <param name="politeExpr">ביטוי PoliteCode</param>
        /// <returns>ביטוי C#</returns>
        public string ConvertExpressionToCSharp(string politeExpr)
        {
            var politeToSymbol = new Dictionary<string, string>
            {
               { "add", "+" },
               { "sub", "-" },
               { "mul", "*" },
               { "div", "/" }
            };

            // פיצול באופן עקבי לכל גרסאות .NET - וסינון רווחים ריקים
            var tokens = politeExpr
                .Split(' ')
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .ToArray();

            for (int i = 0; i < tokens.Length; i++)
            {
                if (politeToSymbol.TryGetValue(tokens[i], out string symbol))
                {
                    tokens[i] = symbol;
                }
            }

            return string.Join(" ", tokens);
        }

        /// <summary>
        /// המרת תנאי מ-PoliteCode לתחביר C#
        /// </summary>
        /// <param name="politeCondition">תנאי PoliteCode</param>
        /// <returns>תנאי C#</returns>
        public string ConvertConditionToCSharp(string politeCondition)
        {
            string result = politeCondition;

            foreach (var pair in _tokenizer.MathWordToSymbol.OrderByDescending(p => p.Key.Length))
            {
                result = Regex.Replace(result, $@"\b{Regex.Escape(pair.Key)}\b", pair.Value);
            }

            return result;
        }
    }
}