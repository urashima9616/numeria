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

    public enum PatternColor { Blue, Gold, Coral, Purple }

    public struct PatternToken : System.IEquatable<PatternToken>
    {
        public ShapeKind Shape;
        public PatternColor Color;

        public PatternToken(ShapeKind shape, PatternColor color)
        {
            Shape = shape;
            Color = color;
        }

        public bool Equals(PatternToken other) => Shape == other.Shape && Color == other.Color;
        public override bool Equals(object obj) => obj is PatternToken other && Equals(other);
        public override int GetHashCode() => ((int)Shape * 397) ^ (int)Color;
        public static bool operator ==(PatternToken left, PatternToken right) => left.Equals(right);
        public static bool operator !=(PatternToken left, PatternToken right) => !left.Equals(right);
    }

    public enum PatternRule
    {
        Alternating,
        Pairs,
        CycleThree,
        CycleFour,
        AlternatingColors,
        AabCycle,
        MirrorRepeat,
        ShapeColorCycle,
    }

    public class ShapePuzzle
    {
        public ShapeKind Answer;
        public List<ShapeKind> Candidates;
        public string Prompt;
    }

    public class PatternPuzzle
    {
        public PatternRule Rule;
        public int Period;
        public int MissingIndex;
        public List<PatternToken> Sequence;
        public PatternToken Answer;
        public List<PatternToken> Candidates;
        public string Prompt;
    }

    public enum PatternMatchRule { ExactCopy, MirrorOrder, ShapesOnly, ColorsOnly }

    public class PatternMatchPuzzle
    {
        public PatternMatchRule Rule;
        public List<PatternToken> Target;
        public List<List<PatternToken>> Candidates;
        public int AnswerIndex;
        public string Prompt;
    }

    public class BalancePuzzle
    {
        public int LeftA;
        public int LeftB;
        public int RightKnown;
        public int Answer;
        public List<int> Candidates;
        public string Prompt;
    }

    public class NumberPathPuzzle
    {
        public List<int> Sequence;
        public int MissingIndex;
        public int Answer;
        public int Step;
        public bool Descending;
        public List<int> Candidates;
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
        Balance,
        NumberPath,
        NumberSequence,
        Shape
    }

    /// <summary>
    /// 谜题生成与校验,移植自 Web 原型 puzzles.js(已验证的逻辑)。
    /// </summary>
    public static class PuzzleGenerator
    {
        private static readonly string[] SmallWords =
        {
            "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
            "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen",
            "eighteen", "nineteen"
        };

        private static readonly string[] TensWords =
        {
            "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"
        };

        /// <summary>安全覆盖 0–99，避免高等级地图因固定查表越界而中断谜题协程。</summary>
        public static string NumberWord(int n)
        {
            if (n < 0 || n > 99) throw new System.ArgumentOutOfRangeException(nameof(n), "NumberWord supports 0-99.");
            if (n < SmallWords.Length) return SmallWords[n];
            int tens = n / 10;
            int ones = n % 10;
            return ones == 0 ? TensWords[tens] : $"{TensWords[tens]}-{SmallWords[ones]}";
        }

        /// <summary>
        /// 算术数字边界：第一章保持 Kindergarten 核心的 10 以内，后续章节只扩展到 20。
        /// 高章节通过更多项、等式拆分、正反数列和图形规律增加难度，不再靠 30/40 的大数字。
        /// </summary>
        public static int MaxForTier(int tier) => tier <= 1 ? 10 : 20;

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
        /// 图形规律同时使用形状和颜色，并混合“下一个”与“补中间空格”。每条规则都以明确周期
        /// 构造，既能逐关提升难度，也能机器验证唯一答案。
        /// </summary>
        public static PatternPuzzle GeneratePattern(Rng rng, int tier = 1)
        {
            var shapes = new List<ShapeKind>
            {
                ShapeKind.Circle, ShapeKind.Triangle, ShapeKind.Square, ShapeKind.Diamond
            };
            var colors = new List<PatternColor>
            {
                PatternColor.Blue, PatternColor.Gold, PatternColor.Coral, PatternColor.Purple
            };
            Shuffle(rng, shapes);
            Shuffle(rng, colors);
            var a = new PatternToken(shapes[0], colors[0]);
            var b = new PatternToken(shapes[1], colors[1]);
            var c = new PatternToken(shapes[2], colors[2]);
            var d = new PatternToken(shapes[3], colors[3]);

            PatternRule[] rules = tier >= 3
                ? new[] { PatternRule.CycleThree, PatternRule.CycleFour, PatternRule.AabCycle,
                    PatternRule.MirrorRepeat, PatternRule.ShapeColorCycle }
                : tier == 2
                    ? new[] { PatternRule.Pairs, PatternRule.CycleThree, PatternRule.AlternatingColors,
                        PatternRule.AabCycle }
                    : new[] { PatternRule.Alternating, PatternRule.Pairs, PatternRule.AlternatingColors };
            var rule = rules[rng.Pick(0, rules.Length - 1)];
            var unit = new List<PatternToken>();

            switch (rule)
            {
                case PatternRule.Pairs:
                    unit.AddRange(new[] { a, a, b, b });
                    break;
                case PatternRule.CycleThree:
                    unit.AddRange(new[] { a, b, c });
                    break;
                case PatternRule.CycleFour:
                    unit.AddRange(new[] { a, b, c, d });
                    break;
                case PatternRule.AlternatingColors:
                    unit.AddRange(new[]
                    {
                        new PatternToken(shapes[0], colors[0]),
                        new PatternToken(shapes[0], colors[1]),
                    });
                    break;
                case PatternRule.AabCycle:
                    unit.AddRange(new[] { a, a, b });
                    break;
                case PatternRule.MirrorRepeat:
                    unit.AddRange(new[] { a, b, c, b, a });
                    break;
                case PatternRule.ShapeColorCycle:
                    unit.AddRange(new[]
                    {
                        new PatternToken(shapes[0], colors[0]),
                        new PatternToken(shapes[1], colors[1]),
                        new PatternToken(shapes[0], colors[2]),
                        new PatternToken(shapes[1], colors[0]),
                        new PatternToken(shapes[0], colors[1]),
                        new PatternToken(shapes[1], colors[2]),
                    });
                    break;
                default:
                    unit.AddRange(new[] { a, b });
                    break;
            }

            int sequenceLength = System.Math.Min(9, unit.Count + 4);
            var sequence = new List<PatternToken>();
            for (int i = 0; i < sequenceLength; i++) sequence.Add(unit[i % unit.Count]);

            bool fillGap = tier >= 2 || rng.Next() < 0.45;
            int missingIndex = fillGap ? rng.Pick(1, sequence.Count - 2) : sequence.Count - 1;
            PatternToken answer = sequence[missingIndex];
            return new PatternPuzzle
            {
                Rule = rule,
                Period = unit.Count,
                MissingIndex = missingIndex,
                Sequence = sequence,
                Answer = answer,
                Candidates = BuildPatternCandidates(rng, answer),
                Prompt = missingIndex == sequence.Count - 1
                    ? "What tile comes next in the pattern?"
                    : "Which tile fills the pattern gap?"
            };
        }

        public static bool CheckPattern(PatternPuzzle puzzle, PatternToken answer) => answer == puzzle.Answer;

        /// <summary>
        /// “Match pattern”不再只是找同一个图形：低阶混合精确复制与镜像顺序，高阶还会要求
        /// 忽略颜色只配形状、或忽略形状只配颜色。
        /// </summary>
        public static PatternMatchPuzzle GeneratePatternMatch(Rng rng, int tier = 1)
        {
            int length = tier >= 3 ? 4 : tier == 2 ? 3 : rng.Pick(2, 3);
            var all = AllPatternTokens();
            Shuffle(rng, all);
            var target = all.GetRange(0, length);
            PatternMatchRule[] rules = tier >= 3
                ? new[] { PatternMatchRule.MirrorOrder, PatternMatchRule.ShapesOnly, PatternMatchRule.ColorsOnly }
                : tier == 2
                    ? new[] { PatternMatchRule.ExactCopy, PatternMatchRule.MirrorOrder,
                        PatternMatchRule.ShapesOnly }
                    : new[] { PatternMatchRule.ExactCopy, PatternMatchRule.MirrorOrder };
            var rule = rules[rng.Pick(0, rules.Length - 1)];
            var answer = MatchAnswer(target, rule);
            var candidates = new List<List<PatternToken>> { answer };
            for (int i = 0; i < 3; i++)
            {
                var distractor = new List<PatternToken>(answer);
                int position = i % distractor.Count;
                PatternToken token = distractor[position];
                if (rule == PatternMatchRule.ShapesOnly ||
                    (rule != PatternMatchRule.ColorsOnly && i % 2 == 0))
                    token.Shape = (ShapeKind)(((int)token.Shape + i + 1) % 4);
                else
                    token.Color = (PatternColor)(((int)token.Color + i + 1) % 4);
                distractor[position] = token;
                candidates.Add(distractor);
            }
            Shuffle(rng, candidates);
            int answerIndex = candidates.FindIndex(candidate => SequencesEqual(candidate, answer));
            string prompt;
            switch (rule)
            {
                case PatternMatchRule.MirrorOrder: prompt = "Find the tiles in mirror order!"; break;
                case PatternMatchRule.ShapesOnly: prompt = "Match the shapes. Colors can change!"; break;
                case PatternMatchRule.ColorsOnly: prompt = "Match the colors. Shapes can change!"; break;
                default: prompt = "Find the exact tile pattern!"; break;
            }
            return new PatternMatchPuzzle
            {
                Rule = rule, Target = target, Candidates = candidates, AnswerIndex = answerIndex, Prompt = prompt
            };
        }

        public static bool CheckPatternMatch(PatternMatchPuzzle puzzle, int candidateIndex) =>
            candidateIndex >= 0 && candidateIndex < puzzle.Candidates.Count &&
            MatchesPatternRule(puzzle, puzzle.Candidates[candidateIndex]);

        public static bool MatchesPatternRule(PatternMatchPuzzle puzzle, IList<PatternToken> candidate)
        {
            if (candidate == null || candidate.Count != puzzle.Target.Count) return false;
            var expected = MatchAnswer(puzzle.Target, puzzle.Rule);
            for (int i = 0; i < expected.Count; i++)
            {
                if (puzzle.Rule == PatternMatchRule.ShapesOnly)
                {
                    if (candidate[i].Shape != expected[i].Shape) return false;
                }
                else if (puzzle.Rule == PatternMatchRule.ColorsOnly)
                {
                    if (candidate[i].Color != expected[i].Color) return false;
                }
                else if (candidate[i] != expected[i]) return false;
            }
            return true;
        }

        /// <summary>用等式两边平衡替代方向旋转题，练习同一个总数的不同拆分方法。</summary>
        public static BalancePuzzle GenerateBalance(Rng rng, int tier = 2)
        {
            int max = MaxForTier(tier);
            int total = rng.Pick(4, max);
            int leftA = rng.Pick(1, total - 1);
            int leftB = total - leftA;
            int rightKnown = rng.Pick(1, total - 1);
            int answer = total - rightKnown;
            return new BalancePuzzle
            {
                LeftA = leftA, LeftB = leftB, RightKnown = rightKnown, Answer = answer,
                Candidates = BuildCandidates(rng, answer, max),
                Prompt = "Balance both sides. What number is missing?"
            };
        }

        public static bool CheckBalance(BalancePuzzle puzzle, int answer) =>
            answer == puzzle.Answer && puzzle.LeftA + puzzle.LeftB == puzzle.RightKnown + answer;

        /// <summary>数字路径可向前或向后，并隐藏中间位置；与只问末项的 NumberSequence 明确区分。</summary>
        public static NumberPathPuzzle GenerateNumberPath(Rng rng, int tier = 1)
        {
            int max = MaxForTier(tier);
            int step = tier >= 3 ? new[] { 1, 2, 5 }[rng.Pick(0, 2)]
                : tier == 2 ? rng.Pick(1, 2) : 1;
            bool descending = tier >= 2 && rng.Next() < 0.5;
            int distance = step * 4;
            int start = descending ? rng.Pick(distance, max) : rng.Pick(0, max - distance);
            var sequence = new List<int>();
            for (int i = 0; i < 5; i++) sequence.Add(start + (descending ? -step * i : step * i));
            int missingIndex = rng.Pick(1, 3);
            int answer = sequence[missingIndex];
            return new NumberPathPuzzle
            {
                Sequence = sequence, MissingIndex = missingIndex, Answer = answer, Step = step,
                Descending = descending, Candidates = BuildCandidates(rng, answer, max),
                Prompt = "Which number fills the path?"
            };
        }

        public static bool CheckNumberPath(NumberPathPuzzle puzzle, int answer) => answer == puzzle.Answer;

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
                    MapPuzzleKind.Pattern, MapPuzzleKind.Symmetry, MapPuzzleKind.Balance,
                    MapPuzzleKind.NumberPath, MapPuzzleKind.NumberSequence, MapPuzzleKind.Shape
                };
            if (tier == 2)
                return new List<MapPuzzleKind>
                {
                    MapPuzzleKind.Formula, MapPuzzleKind.MakeTen, MapPuzzleKind.ChainSum,
                    MapPuzzleKind.Pattern, MapPuzzleKind.Symmetry, MapPuzzleKind.Balance,
                    MapPuzzleKind.NumberPath, MapPuzzleKind.Shape
                };
            return new List<MapPuzzleKind>
            {
                MapPuzzleKind.Formula, MapPuzzleKind.Pattern, MapPuzzleKind.Symmetry,
                MapPuzzleKind.NumberPath, MapPuzzleKind.Shape, MapPuzzleKind.Counting,
                MapPuzzleKind.Comparison
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

        private static List<PatternToken> BuildPatternCandidates(Rng rng, PatternToken answer)
        {
            var pool = AllPatternTokens();
            Shuffle(rng, pool);
            var candidates = new List<PatternToken> { answer };
            foreach (PatternToken token in pool)
            {
                if (!candidates.Contains(token)) candidates.Add(token);
                if (candidates.Count == 4) break;
            }
            Shuffle(rng, candidates);
            return candidates;
        }

        private static List<PatternToken> AllPatternTokens()
        {
            var result = new List<PatternToken>();
            foreach (ShapeKind shape in AllShapes())
                for (int color = 0; color < 4; color++)
                    result.Add(new PatternToken(shape, (PatternColor)color));
            return result;
        }

        private static List<PatternToken> MatchAnswer(IList<PatternToken> target, PatternMatchRule rule)
        {
            var answer = new List<PatternToken>(target);
            if (rule == PatternMatchRule.MirrorOrder)
            {
                answer.Reverse();
                return answer;
            }

            // For the two selective matching modes, visibly change the ignored attribute. This
            // teaches that shape and color can be compared independently instead of rewarding a
            // literal copy that happens to satisfy the rule.
            for (int i = 0; i < answer.Count; i++)
            {
                PatternToken token = answer[i];
                if (rule == PatternMatchRule.ShapesOnly)
                    token.Color = (PatternColor)(((int)token.Color + 1) % 4);
                else if (rule == PatternMatchRule.ColorsOnly)
                    token.Shape = (ShapeKind)(((int)token.Shape + 1) % 4);
                answer[i] = token;
            }
            return answer;
        }

        private static bool SequencesEqual(IList<PatternToken> left, IList<PatternToken> right)
        {
            if (left == null || right == null || left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
                if (left[i] != right[i]) return false;
            return true;
        }

        private static List<ShapeKind> AllShapes() => new List<ShapeKind>
        {
            ShapeKind.Circle, ShapeKind.Triangle, ShapeKind.Square, ShapeKind.Diamond
        };
    }
}
