using System.Collections.Generic;

namespace Numeria.Core
{
    public class FormulaPuzzle
    {
        public int A;
        public int Missing;   // 正确答案
        public int Sum;       // 等号右侧的值(SlotIsResult 时 Sum == Missing)
        public char Op = '+';
        public bool SlotIsResult; // true: A op A = □(如翻倍);false: A op □ = Sum
        public List<int> Candidates;
        public string Prompt;
    }

    public class MakeTenPuzzle
    {
        public int Target;
        public List<int> Hand;
        public string Prompt;
    }

    /// <summary>
    /// 谜题生成与校验,移植自 Web 原型 puzzles.js(已验证的逻辑)。
    /// </summary>
    public static class PuzzleGenerator
    {
        private static readonly string[] Words =
        {
            "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
            "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen",
            "eighteen", "nineteen", "twenty"
        };

        public static string NumberWord(int n) => Words[n];

        private static string Cap(string s) => char.ToUpperInvariant(s[0]) + s.Substring(1);

        private static void Shuffle<T>(Rng rng, IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = (int)(rng.Next() * (i + 1));
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public static FormulaPuzzle GenerateFormula(Rng rng, int max = 10)
        {
            int sum = rng.Pick(3, max);
            int a = rng.Pick(1, sum - 1);
            int missing = sum - a;

            return new FormulaPuzzle
            {
                A = a,
                Missing = missing,
                Sum = sum,
                Candidates = BuildCandidates(rng, missing, max),
                Prompt = $"{Cap(NumberWord(a))} plus what makes {NumberWord(sum)}?",
            };
        }

        public static bool CheckFormula(FormulaPuzzle puzzle, int answer) => answer == puzzle.Missing;

        /// <summary>减法填空:A − □ = C。"Nine take away what leaves five?"</summary>
        public static FormulaPuzzle GenerateSubtraction(Rng rng, int max = 10)
        {
            int a = rng.Pick(3, max);
            int missing = rng.Pick(1, a - 1);
            int c = a - missing;
            var candidates = BuildCandidates(rng, missing, max);
            return new FormulaPuzzle
            {
                A = a, Missing = missing, Sum = c, Op = '-',
                Candidates = candidates,
                Prompt = $"{Cap(NumberWord(a))} take away what leaves {NumberWord(c)}?",
            };
        }

        /// <summary>翻倍:N + N = □。"What is double six?"</summary>
        public static FormulaPuzzle GenerateDouble(Rng rng, int max = 20)
        {
            int n = rng.Pick(2, System.Math.Min(10, max / 2));
            int answer = n * 2;
            var candidates = BuildCandidates(rng, answer, max);
            return new FormulaPuzzle
            {
                A = n, Missing = answer, Sum = answer, Op = '+', SlotIsResult = true,
                Candidates = candidates,
                Prompt = $"What is double {NumberWord(n)}?",
            };
        }

        private static List<int> BuildCandidates(Rng rng, int correct, int max)
        {
            var candidates = new List<int> { correct };
            while (candidates.Count < 4)
            {
                int offset = rng.Pick(1, 3) * (rng.Next() < 0.5 ? -1 : 1);
                int c = correct + offset;
                if (c >= 0 && c <= max && !candidates.Contains(c)) candidates.Add(c);
            }
            Shuffle(rng, candidates);
            return candidates;
        }

        public static (int i, int j)? FindMakeTenPair(IList<int> hand, int target)
        {
            for (int i = 0; i < hand.Count; i++)
                for (int j = i + 1; j < hand.Count; j++)
                    if (hand[i] + hand[j] == target) return (i, j);
            return null;
        }

        public static MakeTenPuzzle GenerateMakeTen(Rng rng, int target = 10, int handSize = 4)
        {
            int a = rng.Pick(1, target - 1);
            var hand = new List<int> { a, target - a };
            while (hand.Count < handSize)
            {
                int d = rng.Pick(1, target - 1);
                // 干扰项不得与现有任何牌凑成 target,保证恰好一组解
                bool clashes = false;
                foreach (int h in hand)
                    if (h + d == target) { clashes = true; break; }
                if (!clashes) hand.Add(d);
            }
            Shuffle(rng, hand);

            return new MakeTenPuzzle
            {
                Target = target,
                Hand = hand,
                Prompt = $"Pick two crystals that make {NumberWord(target)}!",
            };
        }

        public static bool CheckMakeTen(MakeTenPuzzle puzzle, int i, int j) =>
            i != j && puzzle.Hand[i] + puzzle.Hand[j] == puzzle.Target;
    }
}
