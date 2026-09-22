using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Numeria.Core
{
    public enum MathMode { Normal, FeelGood, Challenging, Random }
    public enum LearningKind { Arithmetic, MissingNumber, Comparison, Pattern, Geometry, PlaceValue, Time, Fractions, Measurement, Data }

    [Serializable]
    public class MathSettings
    {
        public int Grade;
        public MathMode[] Modes = new MathMode[6];
        public MathMode Mode { get => Modes[Grade]; set => Modes[Grade] = value; }
        public void Normalize()
        {
            Grade = Math.Max(0, Math.Min(5, Grade));
            var old = Modes; Modes = new MathMode[6];
            for (int i = 0; i < 6; i++)
                if (old != null && i < old.Length && Enum.IsDefined(typeof(MathMode), old[i])) Modes[i] = old[i];
        }
        public static string GradeName(int grade) => grade == 0 ? "KINDERGARTEN" : "GRADE " + grade;
        public static string Scope(int grade) => new[] {
            "Add/subtract within 10, teen numbers, shapes, lengths and picture graphs.",
            "Add/subtract within 20, tens and ones, hours/half-hours and equal shares.",
            "Add/subtract within 100, hundreds, five-minute time, lengths and graphs.",
            "Multiply/divide within 100, fractions, elapsed time, area and data.",
            "Multi-digit operations, fraction addition, area and perimeter.",
            "Decimals, fraction addition, expressions, volume and data." }[Math.Max(0, Math.Min(5, grade))];
    }

    public class LearningQuestion
    {
        public LearningKind Kind;
        public int Grade;
        public MathMode Difficulty;
        public string Prompt, Display, Answer, Hint;
        public string[] Options;
        public int CorrectIndex;
        // Diagram data stays independent of Unity and is shared by tests and UI.
        public string Diagram;
        public int[] Values;
        public string[] Labels;
    }

    public static class MathCurriculum
    {
        private static string S(decimal n) => n.ToString("0.###", CultureInfo.InvariantCulture);
        public static LearningKind[] Available(int grade) => Enum.GetValues(typeof(LearningKind)).Cast<LearningKind>()
            .Where(k => grade > 0 || (k != LearningKind.Time && k != LearningKind.Fractions)).ToArray();

        public static int ChapterLimit(int grade, MathMode mode, int chapter)
        {
            grade = Math.Max(0, Math.Min(5, grade));
            int band = mode == MathMode.FeelGood ? 0 : mode == MathMode.Challenging ? 2 : 1;
            int[,] starts = { {4,5,7}, {5,10,15}, {20,50,70}, {30,100,500}, {100,1000,5000}, {100,500,700} };
            int[,] ends = { {5,10,10}, {10,20,20}, {50,100,100}, {100,1000,1000}, {1000,10000,10000}, {500,1000,1000} };
            double t = (Math.Max(1, Math.Min(8, chapter)) - 1) / 7d;
            return (int)Math.Round(starts[grade,band] + (ends[grade,band] - starts[grade,band]) * t);
        }
        public static int ChapterSteps(int low, int high, int chapter) => (int)Math.Round(low + (high-low) * (Math.Max(1, Math.Min(8, chapter))-1)/7d);

        public static LearningQuestion Generate(MathSettings settings, Rng rng, LearningKind? requested = null, int chapter = 1)
        {
            int g = Math.Max(0, Math.Min(5, settings.Grade));
            var mode = settings.Modes != null && settings.Modes.Length > g ? settings.Modes[g] : MathMode.Normal;
            if (mode == MathMode.Random) mode = (MathMode)rng.Pick(0, 2);
            int difficulty = mode == MathMode.FeelGood ? 0 : mode == MathMode.Challenging ? 2 : 1;
            var pool = Available(g);
            var kind = requested.HasValue && pool.Contains(requested.Value) ? requested.Value : pool[rng.Pick(0, pool.Length - 1)];
            var q = new LearningQuestion { Kind = kind, Grade = g, Difficulty = mode };
            int cap = ChapterLimit(g, mode, chapter);
            int facts = ChapterSteps(difficulty == 0 ? 3 : 5, difficulty == 0 ? 5 : 10, chapter);
            var wrong = new List<string>();
            void Numeric(decimal answer, decimal step = 1)
            {
                q.Answer = S(answer);
                foreach (int d in new[] { -1, 1, 2, -2, 3, 4 })
                    if (answer + d * step >= 0 && (g > 2 || (kind != LearningKind.Arithmetic && kind != LearningKind.MissingNumber && kind != LearningKind.Pattern && kind != LearningKind.Data) || answer + d * step <= cap))
                        wrong.Add(S(answer + d * step));
            }
            switch (kind)
            {
                case LearningKind.Arithmetic:
                case LearningKind.MissingNumber:
                {
                    bool missing = kind == LearningKind.MissingNumber;
                    int a = rng.Pick(1, cap - 1), b = rng.Pick(1, cap - a);
                    bool subtract = rng.Next() < .5;
                    q.Prompt = missing ? "Find the missing number." : "Solve the number spell.";
                    q.Hint = subtract ? "Count back, or use addition to check." : "Break the numbers into friendly parts.";
                    if (g >= 3 && rng.Next() < .55)
                    {
                        a = rng.Pick(2, facts); b = rng.Pick(2, facts);
                        if (g >= 4 && difficulty > 0) a = rng.Pick(11, ChapterSteps(12, g == 5 ? 99 : 40, chapter));
                        q.Display = subtract ? $"{a * b} / {b} = ?" : $"{a} x {b} = ?";
                        q.Hint = "Use equal groups. Multiplication and division undo each other.";
                        Numeric(subtract ? a : a * b);
                    }
                    else if (g == 5)
                    {
                        decimal x = a / 100m, y = b / 100m;
                        if (difficulty == 2)
                        {
                            decimal z = rng.Pick(1, b) / 100m;
                            q.Display = $"({S(x)} + {S(y)}) - {S(z)} = ?";
                            Numeric(x + y - z, .01m);
                        }
                        else { q.Display = subtract ? $"{S(x + y)} - {S(y)} = ?" : $"{S(x)} + {S(y)} = ?"; Numeric(subtract ? x : x + y, .01m); }
                        q.Hint = "Line up the decimal points. Work with hundredths.";
                    }
                    else if (missing)
                    {
                        q.Display = subtract ? $"{a + b} - ? = {a}" : $"{a} + ? = {a + b}"; Numeric(b);
                    }
                    else if (difficulty == 2 && g <= 1)
                    {
                        q.Prompt = "Find the missing number.";
                        q.Display = subtract ? $"{a + b} - ? = {a}" : $"? + {a} = {a + b}"; Numeric(b);
                    }
                    else if (difficulty == 2 && g >= 2)
                    {
                        int c = rng.Pick(1, b); q.Display = $"({a} + {b}) - {c} = ?"; Numeric(a + b - c);
                    }
                    else { q.Display = subtract ? $"{a + b} - {b} = ?" : $"{a} + {b} = ?"; Numeric(subtract ? a : a + b); }
                    break;
                }
                case LearningKind.Comparison:
                {
                    decimal a = rng.Pick(0, cap), b = rng.Pick(0, cap);
                    if (g == 5) { a /= 1000; b /= 1000; }
                    q.Prompt = "Choose the sign that makes this true."; q.Display = $"{S(a)}   ?   {S(b)}";
                    q.Hint = "Compare the greatest place first. The open side faces the greater number.";
                    q.Answer = a < b ? "<" : a > b ? ">" : "=";
                    wrong.AddRange(new[] { "<", ">", "=" }); break;
                }
                case LearningKind.Pattern:
                {
                    int step = rng.Pick(1, Math.Max(1, Math.Min(cap / 4, g == 0 ? (chapter < 5 ? 1 : 2) : ChapterSteps(2, (g + 1) * 2, chapter))));
                    int start = rng.Pick(0, Math.Max(0, Math.Min(g == 0 ? 2 : facts, cap - 4 * step)));
                    q.Prompt = "Continue the number pattern.";
                    q.Display = $"{start}, {start + step}, {start + step * 2}, {start + step * 3}, ?";
                    q.Hint = "Find how much changes at each step.";
                    if (difficulty == 2) { q.Display = $"{start}, {start + step}, ?, {start + step * 3}, {start + step * 4}"; Numeric(start + step * 2); }
                    else Numeric(start + step * 4); break;
                }
                case LearningKind.Geometry:
                {
                    if (g < 3)
                    {
                        int sides = rng.Pick(3, ChapterSteps(3, g == 0 ? 4 : 6, chapter)); q.Diagram = "polygon"; q.Values = new[] { sides };
                        q.Prompt = "How many sides does this shape have?"; q.Display = "COUNT THE STRAIGHT SIDES";
                        q.Hint = "Start at one corner. Count each straight edge once."; Numeric(sides);
                    }
                    else
                    {
                        int a = rng.Pick(2, facts), b = rng.Pick(2, facts);
                        q.Prompt = "Find the perimeter of the rectangle."; q.Display = $"Length {a} cm    Width {b} cm";
                        q.Diagram = "rectangle"; q.Values = new[] { a, b };
                        q.Hint = "Perimeter is the distance around all four sides.";
                        if (g >= 4 && difficulty == 2) { q.Prompt = "Find the missing width of the rectangle."; q.Display = $"Perimeter {2*(a+b)} cm    Length {a} cm\nWidth ? cm"; Numeric(b); }
                        else Numeric(2 * (a + b));
                    }
                    break;
                }
                case LearningKind.PlaceValue:
                {
                    if (g == 0)
                    {
                        int ones = rng.Pick(1, ChapterSteps(3, 9, chapter)); q.Prompt = "One ten and how many ones?"; q.Display = $"{10 + ones} = 10 + ?";
                        q.Diagram = "tenframe"; q.Values = new[] { 10 + ones };
                        q.Hint = "The full frame is ten. Count the extra dots."; Numeric(ones);
                    }
                    else
                    {
                        int places = ChapterSteps(g < 3 ? 1 : 2, g == 3 ? 3 : Math.Min(5, g + 1), chapter), p = rng.Pick(0, places - 1), pow = (int)Math.Pow(10, p);
                        int n = rng.Pick(pow, pow * 9) + rng.Pick(0, Math.Max(0, pow - 1));
                        int digit = (n / pow) % 10;
                        q.Prompt = "What is the value of the indicated digit?";
                        q.Display = $"{n}\nDigit {digit} in the {new[] { "ones", "tens", "hundreds", "thousands", "ten-thousands" }[p]} place";
                        q.Hint = "Multiply the digit by the value of its place."; Numeric(digit * pow, pow); wrong.Add(S(digit));
                    }
                    break;
                }
                case LearningKind.Time:
                {
                    int hour = rng.Pick(1, 12), minute = g == 1 ? rng.Pick(0, difficulty == 0 ? 0 : ChapterSteps(0,1,chapter)) * 30 : rng.Pick(0, 11) * 5;
                    q.Diagram = "clock"; q.Values = new[] { hour, minute };
                    int elapsed = g >= 3 && difficulty > 0 ? rng.Pick(1, ChapterSteps(2, 6, chapter)) * 5 : 0;
                    q.Prompt = elapsed == 0 ? "What time does the clock show?" : $"What time will it be in {elapsed} minutes?";
                    q.Display = elapsed == 0 ? "READ THE CLOCK" : $"ADD {elapsed} MINUTES";
                    q.Hint = "Short hand: hours. Long hand: minutes. Each number marks five minutes.";
                    string Time(int total) { total = (total + 720) % 720; return $"{(total / 60 == 0 ? 12 : total / 60)}:{total % 60:00}"; }
                    int answer = hour * 60 + minute + elapsed; q.Answer = Time(answer);
                    wrong.AddRange(new[] { Time(answer + 30), Time(answer + 60), Time(answer - 5), Time(answer + 5) }); break;
                }
                case LearningKind.Fractions:
                {
                    int denominator = g <= 2 ? (rng.Next() < .5 ? 2 : 4) : rng.Pick(3, facts);
                    int numerator = rng.Pick(1, denominator - 1);
                    q.Diagram = "fraction"; q.Values = new[] { numerator, denominator };
                    q.Prompt = "What fraction is shaded?"; q.Display = "EQUAL PARTS OF ONE WHOLE";
                    q.Hint = "Bottom number: all equal parts. Top number: shaded parts.";
                    q.Answer = $"{numerator}/{denominator}";
                    wrong.AddRange(new[] { $"{numerator + 1}/{denominator}", $"{numerator}/{denominator + 1}", $"{numerator - 1}/{denominator}" });
                    if (g <= 2)
                    {
                        // Early equal-share questions only show halves/quarters, never fifths
                        // or an equivalent fraction disguised as an incorrect answer.
                        wrong.Clear();
                        foreach (int part in new[] { 0, 1, 2, 3, 4 })
                            if (part * denominator != numerator * 4) wrong.Add($"{part}/4");
                    }
                    if (g >= 4 && difficulty > 0)
                    {
                        q.Diagram = null; int b = rng.Pick(1, denominator - numerator);
                        if (g == 5)
                        {
                            q.Prompt = "Add the fractions. Use the common denominator.";
                            q.Display = $"{numerator}/{denominator} + {b}/{denominator * 2} = ?";
                            numerator = numerator * 2 + b; denominator *= 2;
                        }
                        else { q.Prompt = "Add the fractions."; q.Display = $"{numerator}/{denominator} + {b}/{denominator} = ?"; numerator += b; }
                        q.Hint = "Use equal-sized parts. Then add the numerators.";
                        q.Answer = $"{numerator}/{denominator}"; wrong.Clear();
                        wrong.AddRange(new[] { $"{numerator - 1}/{denominator}", $"{numerator + 1}/{denominator}", $"{numerator + 2}/{denominator}" });
                    }
                    break;
                }
                case LearningKind.Measurement:
                {
                    int a = rng.Pick(2, ChapterSteps(3,8,chapter)), b = rng.Pick(2, ChapterSteps(3,8,chapter)), c = rng.Pick(2, ChapterSteps(3,6,chapter));
                    q.Diagram = "bars"; q.Values = new[] { a, b }; q.Labels = new[] { "A", "B" };
                    if (g == 0)
                    {
                        if (a == b) b = a == 8 ? 7 : a + 1; q.Values[1] = b;
                        q.Prompt = "Which ribbon is longer?"; q.Display = "COMPARE LENGTHS"; q.Answer = a > b ? "A" : "B";
                        wrong.Add(a > b ? "B" : "A"); q.Hint = "Both start at the same place. Look at the far ends.";
                    }
                    else if (g <= 2)
                    {
                        q.Prompt = "How much longer is the longer ribbon?"; q.Display = $"A: {a} cm    B: {b} cm";
                        q.Hint = "Subtract the shorter length from the longer length."; Numeric(Math.Abs(a - b));
                    }
                    else
                    {
                        bool volume = g == 5; q.Diagram = "rectangle"; q.Values = new[] { a, b };
                        q.Prompt = volume ? "Find the volume of this box in cubic units." : "Find the area in square units.";
                        q.Display = volume ? $"Length {a}   Width {b}   Height {c}" : $"Length {a}   Width {b}";
                        q.Hint = volume ? "Volume = length x width x height." : "Area = length x width."; Numeric(volume ? a * b * c : a * b);
                    }
                    break;
                }
                case LearningKind.Data:
                {
                    int maximum = Math.Min(Math.Max(1, g == 0 ? cap : cap / 3), ChapterSteps(2, g == 0 ? 4 : 8, chapter));
                    int a = rng.Pick(1, maximum), b = rng.Pick(1, maximum), c = rng.Pick(1, maximum);
                    q.Diagram = "bars"; q.Values = g == 0 ? new[] { a, b } : new[] { a, b, c }; q.Labels = new[] { "RED", "BLUE", "GOLD" };
                    bool total = g > 0 && difficulty > 0;
                    q.Prompt = total ? "How many votes are there altogether?" : "How many voted for RED?";
                    q.Display = "FAVORITE CRYSTALS - EACH UNIT IS ONE VOTE";
                    q.Hint = total ? "Add the values of all three bars." : "Read the length of the RED bar."; Numeric(total ? a + b + c : a); break;
                }
            }
            var options = wrong.Where(x => x != q.Answer).Distinct().Take(3).ToList(); options.Add(q.Answer);
            for (int i = options.Count - 1; i > 0; i--) { int j = rng.Pick(0, i); var tmp = options[i]; options[i] = options[j]; options[j] = tmp; }
            q.Options = options.ToArray(); q.CorrectIndex = options.IndexOf(q.Answer);
            return q;
        }
    }
}
