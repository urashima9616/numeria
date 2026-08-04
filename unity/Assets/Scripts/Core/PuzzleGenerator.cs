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

    public class CountingPuzzle
    {
        public int Count;
        public List<int> Candidates;
        public string Prompt;
    }

    public enum ComparisonSide { Left, Right }

    public class ComparisonPuzzle
    {
        public int LeftCount;
        public int RightCount;
        public ComparisonSide Answer;
        public string Prompt;
    }

    public class ChainSumPuzzle
    {
        public List<int> Terms;
        public int Answer;
        public List<int> Candidates;
        public string Prompt;
    }

    public enum ShapeKind { Circle, Triangle, Square, Diamond }

    public enum PatternRule { Alternating, Pairs, CycleThree, CycleFour }

    public class ShapePuzzle
    {
        public ShapeKind Answer;
        public List<ShapeKind> Candidates;
        public string Prompt;
    }

    public class PatternPuzzle
    {
        public PatternRule Rule;
        public List<ShapeKind> Sequence;
        public ShapeKind Answer;
        public List<ShapeKind> Candidates;
        public string Prompt;
    }

    public class SymmetryPuzzle
    {
        public ShapeKind Wing;
        public List<ShapeKind> Candidates;
        public string Prompt;
    }

    public enum DirectionKind { Up, Right, Down, Left }

    public class RotationPuzzle
    {
        public DirectionKind Start;
        public int QuarterTurns;
        public DirectionKind Answer;
        public List<DirectionKind> Candidates;
        public string Prompt;
    }

    public class NumberSequencePuzzle
    {
        public List<int> Sequence;
        public int Step;
        public int Answer;
        public List<int> Candidates;
        public string Prompt;
    }

    /// <summary>每张地图的题型池。传送门从同一池中抽取三种不同题型。</summary>
    public enum MapPuzzleKind
    {
        Formula,
        Counting,
        Comparison,
        MakeTen,
        ChainSum,
        Pattern,
        Symmetry,
        Rotation,
        NumberSequence,
        Shape
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
            "eighteen", "nineteen", "twenty", "twenty-one", "twenty-two", "twenty-three",
            "twenty-four", "twenty-five", "twenty-six", "twenty-seven", "twenty-eight",
            "twenty-nine", "thirty"
        };

        public static string NumberWord(int n) => Words[n];

        /// <summary>幼儿园课程的三段数字边界:10、20、30。</summary>
        public static int MaxForTier(int tier) => tier >= 4 ? 40 : tier == 3 ? 30 : tier == 2 ? 20 : 10;

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

        public static CountingPuzzle GenerateCounting(Rng rng, int max = 10)
        {
            int count = rng.Pick(3, max);
            return new CountingPuzzle
            {
                Count = count,
                Candidates = BuildCandidates(rng, count, max),
                Prompt = "How many fireflies do you see?"
            };
        }

        public static bool CheckCounting(CountingPuzzle puzzle, int answer) => answer == puzzle.Count;

        public static ComparisonPuzzle GenerateComparison(Rng rng, int max = 10)
        {
            int left = rng.Pick(2, max);
            int right;
            do right = rng.Pick(2, max); while (right == left);
            return new ComparisonPuzzle
            {
                LeftCount = left,
                RightCount = right,
                Answer = left > right ? ComparisonSide.Left : ComparisonSide.Right,
                Prompt = "Which side has more mushrooms?"
            };
        }

        public static bool CheckComparison(ComparisonPuzzle puzzle, ComparisonSide answer) =>
            answer == puzzle.Answer;

        public static ChainSumPuzzle GenerateChainSum(Rng rng, int max = 20, int termCount = 3)
        {
            termCount = System.Math.Max(2, System.Math.Min(4, termCount));
            var terms = new List<int>();
            int answer = 0;
            for (int i = 0; i < termCount; i++)
            {
                int remainingTerms = termCount - i - 1;
                int largest = System.Math.Min(9, max - answer - remainingTerms);
                int term = rng.Pick(1, System.Math.Max(1, largest));
                terms.Add(term);
                answer += term;
            }
            return new ChainSumPuzzle
            {
                Terms = terms,
                Answer = answer,
                Candidates = BuildCandidates(rng, answer, max),
                Prompt = "Add them all up!"
            };
        }

        public static bool CheckChainSum(ChainSumPuzzle puzzle, int answer) => answer == puzzle.Answer;

        /// <summary>
        /// 天空城图形规律。三种规则都只要求选择“下一个”，候选形状互不重复且答案唯一。
        /// </summary>
        public static PatternPuzzle GeneratePattern(Rng rng, int tier = 1)
        {
            var shapes = new List<ShapeKind>
            {
                ShapeKind.Circle, ShapeKind.Triangle, ShapeKind.Square, ShapeKind.Diamond
            };
            Shuffle(rng, shapes);
            ShapeKind a = shapes[0], b = shapes[1], c = shapes[2];
            // 第一关只出现 ABAB/AABB；第二关加入 ABC；第三关使用更长的四图形循环。
            var rule = tier >= 3 ? PatternRule.CycleFour
                : tier == 2 ? (PatternRule)rng.Pick(1, 2)
                : (PatternRule)rng.Pick(0, 1);
            var sequence = new List<ShapeKind>();
            ShapeKind answer;

            switch (rule)
            {
                case PatternRule.Pairs:
                    sequence.AddRange(new[] { a, a, b, b, a, a });
                    answer = b;
                    break;
                case PatternRule.CycleThree:
                    sequence.AddRange(new[] { a, b, c, a, b });
                    answer = c;
                    break;
                case PatternRule.CycleFour:
                    ShapeKind d = shapes[3];
                    sequence.AddRange(new[] { a, b, c, d, a, b, c });
                    answer = d;
                    break;
                default:
                    sequence.AddRange(new[] { a, b, a, b, a });
                    answer = b;
                    break;
            }

            var candidates = new List<ShapeKind>
            {
                ShapeKind.Circle, ShapeKind.Triangle, ShapeKind.Square, ShapeKind.Diamond
            };
            Shuffle(rng, candidates);
            return new PatternPuzzle
            {
                Rule = rule,
                Sequence = sequence,
                Answer = answer,
                Candidates = candidates,
                Prompt = "What comes next in the pattern?"
            };
        }

        public static bool CheckPattern(PatternPuzzle puzzle, ShapeKind answer) => answer == puzzle.Answer;

        public static SymmetryPuzzle GenerateSymmetry(Rng rng)
        {
            var candidates = AllShapes();
            Shuffle(rng, candidates);
            return new SymmetryPuzzle
            {
                Wing = candidates[rng.Pick(0, candidates.Count - 1)],
                Candidates = candidates,
                Prompt = "Find the matching wing!"
            };
        }

        public static bool CheckSymmetry(SymmetryPuzzle puzzle, ShapeKind answer) => answer == puzzle.Wing;

        public static RotationPuzzle GenerateRotation(Rng rng, int tier = 2)
        {
            var start = (DirectionKind)rng.Pick(0, 3);
            int turns = tier >= 3 ? rng.Pick(2, 3) : 1;
            var candidates = new List<DirectionKind>
            {
                DirectionKind.Up, DirectionKind.Right, DirectionKind.Down, DirectionKind.Left
            };
            Shuffle(rng, candidates);
            return new RotationPuzzle
            {
                Start = start,
                QuarterTurns = turns,
                Answer = (DirectionKind)(((int)start + turns) % 4),
                Candidates = candidates,
                Prompt = "Which one is it after a turn?"
            };
        }

        public static bool CheckRotation(RotationPuzzle puzzle, DirectionKind answer) => answer == puzzle.Answer;

        public static NumberSequencePuzzle GenerateNumberSequence(Rng rng, int tier = 3)
        {
            int max = MaxForTier(tier);
            int step = tier >= 3 ? rng.Pick(2, 5) : rng.Pick(1, 3);
            int start = rng.Pick(1, System.Math.Max(1, max - step * 3));
            var sequence = new List<int> { start, start + step, start + step * 2 };
            int answer = start + step * 3;
            return new NumberSequencePuzzle
            {
                Sequence = sequence,
                Step = step,
                Answer = answer,
                Candidates = BuildCandidates(rng, answer, max),
                Prompt = "What number comes next?"
            };
        }

        public static bool CheckNumberSequence(NumberSequencePuzzle puzzle, int answer) => answer == puzzle.Answer;

        public static ShapePuzzle GenerateShape(Rng rng, int tier = 1)
        {
            var candidates = AllShapes();
            Shuffle(rng, candidates);
            ShapeKind answer;
            string prompt;
            if (tier >= 3)
            {
                answer = rng.Next() < 0.5 ? ShapeKind.Square : ShapeKind.Diamond;
                prompt = answer == ShapeKind.Square
                    ? "Find the four-sided shape with a flat top!"
                    : "Find the four-sided shape standing on a point!";
            }
            else if (tier == 2)
            {
                answer = rng.Next() < 0.5 ? ShapeKind.Triangle : ShapeKind.Circle;
                prompt = answer == ShapeKind.Triangle
                    ? "Which shape has three straight sides?"
                    : "Which shape has no straight sides?";
            }
            else
            {
                answer = candidates[rng.Pick(0, candidates.Count - 1)];
                prompt = $"Find the {answer.ToString().ToLowerInvariant()}!";
            }
            return new ShapePuzzle { Answer = answer, Candidates = candidates, Prompt = prompt };
        }

        public static bool CheckShape(ShapePuzzle puzzle, ShapeKind answer) => answer == puzzle.Answer;

        public static List<MapPuzzleKind> MapPuzzleKindsForTier(int tier)
        {
            if (tier >= 3)
                return new List<MapPuzzleKind>
                {
                    MapPuzzleKind.Formula, MapPuzzleKind.MakeTen, MapPuzzleKind.ChainSum,
                    MapPuzzleKind.Pattern, MapPuzzleKind.Symmetry, MapPuzzleKind.Rotation,
                    MapPuzzleKind.NumberSequence, MapPuzzleKind.Shape
                };
            if (tier == 2)
                return new List<MapPuzzleKind>
                {
                    MapPuzzleKind.Formula, MapPuzzleKind.MakeTen, MapPuzzleKind.ChainSum,
                    MapPuzzleKind.Pattern, MapPuzzleKind.Symmetry, MapPuzzleKind.Rotation,
                    MapPuzzleKind.Shape
                };
            return new List<MapPuzzleKind>
            {
                MapPuzzleKind.Formula, MapPuzzleKind.Pattern, MapPuzzleKind.Symmetry,
                MapPuzzleKind.Shape, MapPuzzleKind.Counting, MapPuzzleKind.Comparison
            };
        }

        public static MapPuzzleKind PickMapPuzzleKind(Rng rng, int tier)
        {
            var pool = MapPuzzleKindsForTier(tier);
            // 加减法是每关主线，保持约 35% 出现率；其余题型分享剩余机会。
            if (rng.Next() < 0.35) return MapPuzzleKind.Formula;
            pool.Remove(MapPuzzleKind.Formula);
            return pool[rng.Pick(0, pool.Count - 1)];
        }

        public static List<MapPuzzleKind> GatePuzzleKinds(Rng rng, int tier)
        {
            var pool = MapPuzzleKindsForTier(tier);
            pool.Remove(MapPuzzleKind.Formula);
            Shuffle(rng, pool);
            var result = new List<MapPuzzleKind> { MapPuzzleKind.Formula };
            result.AddRange(pool.GetRange(0, System.Math.Min(2, pool.Count)));
            return result;
        }

        private static List<ShapeKind> AllShapes() => new List<ShapeKind>
        {
            ShapeKind.Circle, ShapeKind.Triangle, ShapeKind.Square, ShapeKind.Diamond
        };
    }
}
