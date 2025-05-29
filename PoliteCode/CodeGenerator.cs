using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PoliteCode
{
    // אחראי על המרת ביטויי והוראות PoliteCode לקוד C#
    public class CodeGenerator
    {
        private readonly Tokenizer _tokenizer;

        public CodeGenerator(Tokenizer tokenizer)
        {
            _tokenizer = tokenizer;
        }

        // המרת ביטוי מתמטי מ-PoliteCode לתחביר C#
        
        public string ConvertExpressionToCSharp(string politeExpr)
        {
            var politeToSymbol = new Dictionary<string, string>
            {
               { "add", "+" },
               { "sub", "-" },
               { "mul", "*" },
               { "div", "/" }
            };

            // סינון רווחים ריקים
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

        /// המרת תנאי מ-PoliteCode לתחביר C#
       
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