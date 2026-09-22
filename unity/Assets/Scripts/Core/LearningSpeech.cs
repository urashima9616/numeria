using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Numeria.Core
{
    public static class LearningSpeech
    {
        private static readonly string[] Small = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
        private static readonly string[] Tens = { "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
        public static IEnumerable<string> Number(int n)
        {
            if (n >= 1000) { foreach (var t in Number(n / 1000)) yield return t; yield return "thousand"; n %= 1000; if (n == 0) yield break; }
            if (n >= 100) { yield return Small[n / 100]; yield return "hundred"; n %= 100; if (n == 0) yield break; }
            if (n >= 20) { yield return Tens[n / 10]; n %= 10; if (n == 0) yield break; }
            yield return Small[n];
        }
        public static string[] Lines(LearningQuestion q)
        {
            var lines = new List<string> { q.Prompt };
            // Reading a clock/fraction must not speak its answer. Visible bar labels may be read.
            if (q.Diagram == "clock" || q.Diagram == "fraction" || q.Diagram == "polygon") return lines.ToArray();
            if (q.Diagram == "bars")
            {
                for (int i = 0; i < q.Values.Length; i++) { lines.Add(q.Labels[i].ToLowerInvariant()); lines.AddRange(Number(q.Values[i])); }
                return lines.ToArray();
            }
            foreach (Match match in Regex.Matches(q.Display.ToLowerInvariant(), @"\d+(?:\.\d+)?|[a-z]+|[+\-x/=?<>():]"))
            {
                string token = match.Value;
                if (char.IsDigit(token[0]))
                {
                    var parts = token.Split('.'); lines.AddRange(Number(int.Parse(parts[0])));
                    if (parts.Length > 1) { lines.Add("point"); foreach (char digit in parts[1]) lines.Add(Small[digit - '0']); }
                }
                else
                {
                    switch (token)
                    {
                        case "+": lines.Add("plus"); break;
                        case "-": lines.Add("minus"); break;
                        case "x": lines.Add("times"); break;
                        case "/": lines.Add(q.Kind == LearningKind.Fractions ? "over" : "divided by"); break;
                        case "=": lines.Add("equals"); break;
                        case "?": lines.Add("what"); break;
                        case "(": lines.Add("open parenthesis"); break;
                        case ")": lines.Add("close parenthesis"); break;
                        case ":": break;
                        case "cm": lines.Add("centimeters"); break;
                        default: lines.Add(token); break;
                    }
                }
            }
            return lines.ToArray();
        }
    }
}
