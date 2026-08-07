using System.Collections.Generic;
using NUnit.Framework;
using Numeria.Core;

namespace Numeria.Core.Tests
{
    public class MapPuzzleTests
    {
        [Test]
        public void ForestCountingAndComparison_AreAnswerable()
        {
            for (uint seed = 1; seed <= 80; seed++)
            {
                var counting = PuzzleGenerator.GenerateCounting(new Rng(seed));
                Assert.Contains(counting.Count, counting.Candidates);
                Assert.AreEqual(4, new HashSet<int>(counting.Candidates).Count);
                Assert.True(PuzzleGenerator.CheckCounting(counting, counting.Count));

                var comparison = PuzzleGenerator.GenerateComparison(new Rng(seed));
                Assert.AreNotEqual(comparison.LeftCount, comparison.RightCount);
                Assert.True(PuzzleGenerator.CheckComparison(comparison, comparison.Answer));
                Assert.AreEqual(comparison.LeftCount > comparison.RightCount,
                    comparison.Answer == ComparisonSide.Left);
            }
        }

        [Test]
        public void ChainSums_AddComplexityWithoutExceedingTwenty()
        {
            for (uint seed = 1; seed <= 80; seed++)
            {
                var tier2 = PuzzleGenerator.GenerateChainSum(new Rng(seed), 20, 3);
                Assert.AreEqual(3, tier2.Terms.Count);
                Assert.AreEqual(tier2.Terms[0] + tier2.Terms[1] + tier2.Terms[2], tier2.Answer);
                Assert.That(tier2.Answer, Is.LessThanOrEqualTo(20));
                Assert.Contains(tier2.Answer, tier2.Candidates);

                var tier3 = PuzzleGenerator.GenerateChainSum(new Rng(seed), 20, 4);
                Assert.AreEqual(4, tier3.Terms.Count);
                Assert.AreEqual(tier3.Terms[0] + tier3.Terms[1] + tier3.Terms[2] + tier3.Terms[3],
                    tier3.Answer);
                Assert.That(tier3.Answer, Is.LessThanOrEqualTo(20));
                Assert.True(PuzzleGenerator.CheckChainSum(tier3, tier3.Answer));
            }
        }

        [Test]
        public void PatternMatching_UsesFourDifferentRulesWithOneValidChoice()
        {
            var rules = new HashSet<PatternMatchRule>();
            for (int tier = 1; tier <= 4; tier++)
            for (uint seed = 1; seed <= 160; seed++)
            {
                var puzzle = PuzzleGenerator.GeneratePatternMatch(new Rng(seed), tier);
                rules.Add(puzzle.Rule);
                Assert.AreEqual(4, puzzle.Candidates.Count);
                Assert.That(puzzle.AnswerIndex, Is.InRange(0, 3));
                int valid = 0;
                for (int i = 0; i < puzzle.Candidates.Count; i++)
                    if (PuzzleGenerator.MatchesPatternRule(puzzle, puzzle.Candidates[i])) valid++;
                Assert.AreEqual(1, valid, $"tier {tier}, seed {seed}, rule {puzzle.Rule}");
                Assert.True(PuzzleGenerator.CheckPatternMatch(puzzle, puzzle.AnswerIndex));
            }
            CollectionAssert.AreEquivalent(new[]
            {
                PatternMatchRule.ExactCopy, PatternMatchRule.MirrorOrder,
                PatternMatchRule.ShapesOnly, PatternMatchRule.ColorsOnly
            }, rules);
        }

        [Test]
        public void BalanceAndNumberPath_HaveBoundedUniqueAnswers()
        {
            for (int tier = 1; tier <= 4; tier++)
            for (uint seed = 1; seed <= 100; seed++)
            {
                int max = PuzzleGenerator.MaxForTier(tier);
                var balance = PuzzleGenerator.GenerateBalance(new Rng(seed), tier);
                Assert.AreEqual(balance.LeftA + balance.LeftB,
                    balance.RightKnown + balance.Answer);
                Assert.Contains(balance.Answer, balance.Candidates);
                Assert.AreEqual(4, new HashSet<int>(balance.Candidates).Count);
                Assert.That(balance.Answer, Is.InRange(0, max));
                Assert.True(PuzzleGenerator.CheckBalance(balance, balance.Answer));

                var path = PuzzleGenerator.GenerateNumberPath(new Rng(seed), tier);
                Assert.AreEqual(5, path.Sequence.Count);
                Assert.That(path.MissingIndex, Is.InRange(1, 3));
                Assert.AreEqual(path.Sequence[path.MissingIndex], path.Answer);
                Assert.Contains(path.Answer, path.Candidates);
                Assert.AreEqual(4, new HashSet<int>(path.Candidates).Count);
                for (int i = 1; i < path.Sequence.Count; i++)
                    Assert.AreEqual(path.Descending ? -path.Step : path.Step,
                        path.Sequence[i] - path.Sequence[i - 1]);
                foreach (int value in path.Sequence) Assert.That(value, Is.InRange(0, max));
                Assert.True(PuzzleGenerator.CheckNumberPath(path, path.Answer));
            }
        }

        [Test]
        public void NumberSequences_HaveValidIncreasingSteps()
        {
            for (int tier = 2; tier <= 4; tier++)
            for (uint seed = 1; seed <= 80; seed++)
            {
                var sequence = PuzzleGenerator.GenerateNumberSequence(new Rng(seed), tier);
                Assert.AreEqual(sequence.Step, sequence.Sequence[1] - sequence.Sequence[0]);
                Assert.AreEqual(sequence.Step, sequence.Sequence[2] - sequence.Sequence[1]);
                Assert.AreEqual(sequence.Sequence[2] + sequence.Step, sequence.Answer);
                Assert.Contains(sequence.Answer, sequence.Candidates);
                Assert.That(sequence.Answer, Is.LessThanOrEqualTo(PuzzleGenerator.MaxForTier(tier)));
                Assert.True(PuzzleGenerator.CheckNumberSequence(sequence, sequence.Answer));
            }
        }

        [Test]
        public void ArithmeticDifficulty_UsesKindergartenTenThenStretchTwentyBounds()
        {
            for (int tier = 1; tier <= 6; tier++)
            {
                int max = PuzzleGenerator.MaxForTier(tier);
                Assert.AreEqual(tier == 1 ? 10 : 20, max);
                for (uint seed = 1; seed <= 80; seed++)
                {
                    var add = PuzzleGenerator.GenerateFormula(new Rng(seed), max);
                    var subtract = PuzzleGenerator.GenerateSubtraction(new Rng(seed), max);
                    var chain = PuzzleGenerator.GenerateChainSum(new Rng(seed), max, tier >= 3 ? 4 : 3);
                    var makeTarget = PuzzleGenerator.GenerateMakeTen(new Rng(seed), max);
                    var balance = PuzzleGenerator.GenerateBalance(new Rng(seed), tier);

                    Assert.AreEqual(add.Sum, add.A + add.Missing);
                    Assert.That(add.A, Is.InRange(0, max));
                    Assert.That(add.Missing, Is.InRange(0, max));
                    Assert.That(add.Sum, Is.InRange(0, max));
                    Assert.AreEqual(subtract.Sum, subtract.A - subtract.Missing);
                    Assert.That(subtract.A, Is.InRange(0, max));
                    Assert.That(subtract.Missing, Is.InRange(0, max));
                    Assert.That(subtract.Sum, Is.InRange(0, max));
                    Assert.That(chain.Answer, Is.InRange(0, max));
                    Assert.AreEqual(max, makeTarget.Target);
                    Assert.That(balance.LeftA + balance.LeftB, Is.InRange(0, max));
                    Assert.That(balance.RightKnown + balance.Answer, Is.InRange(0, max));
                }
            }
        }

        [Test]
        public void HighTierGenerators_NeverExceedTwentyOrThrowAcrossManySeeds()
        {
            int max = PuzzleGenerator.MaxForTier(6);
            Assert.AreEqual(20, max);
            for (uint seed = 1; seed <= 500; seed++)
            {
                Assert.DoesNotThrow(() => PuzzleGenerator.GenerateFormula(new Rng(seed), max));
                Assert.DoesNotThrow(() => PuzzleGenerator.GenerateSubtraction(new Rng(seed), max));
                Assert.DoesNotThrow(() => PuzzleGenerator.GenerateMakeTen(new Rng(seed), max));
                Assert.DoesNotThrow(() => PuzzleGenerator.GenerateChainSum(new Rng(seed), max, 4));
                Assert.DoesNotThrow(() => PuzzleGenerator.GeneratePattern(new Rng(seed), 4));
                Assert.DoesNotThrow(() => PuzzleGenerator.GeneratePatternMatch(new Rng(seed), 4));
                Assert.DoesNotThrow(() => PuzzleGenerator.GenerateBalance(new Rng(seed), 4));
                Assert.DoesNotThrow(() => PuzzleGenerator.GenerateNumberPath(new Rng(seed), 4));
                Assert.DoesNotThrow(() => PuzzleGenerator.GenerateNumberSequence(new Rng(seed), 4));
                Assert.DoesNotThrow(() => PuzzleGenerator.GenerateShape(new Rng(seed), 4));
            }
        }

        [Test]
        public void ShapeQuestions_ProgressFromNamingToProperties()
        {
            for (int tier = 1; tier <= 4; tier++)
            for (uint seed = 1; seed <= 40; seed++)
            {
                var puzzle = PuzzleGenerator.GenerateShape(new Rng(seed), tier);
                Assert.AreEqual(4, new HashSet<ShapeKind>(puzzle.Candidates).Count);
                Assert.Contains(puzzle.Answer, puzzle.Candidates);
                Assert.True(PuzzleGenerator.CheckShape(puzzle, puzzle.Answer));
                if (tier == 1) StringAssert.StartsWith("Find the", puzzle.Prompt);
                else StringAssert.Contains("shape", puzzle.Prompt);
            }
        }

        [Test]
        public void EveryMapTier_HasItsDesignPool_AndGateUsesThreeDifferentKinds()
        {
            CollectionAssert.AreEquivalent(new[]
            {
                MapPuzzleKind.Formula, MapPuzzleKind.Pattern, MapPuzzleKind.Symmetry,
                MapPuzzleKind.NumberPath, MapPuzzleKind.Shape, MapPuzzleKind.Counting,
                MapPuzzleKind.Comparison
            }, PuzzleGenerator.MapPuzzleKindsForTier(1));
            CollectionAssert.AreEquivalent(new[]
            {
                MapPuzzleKind.Formula, MapPuzzleKind.MakeTen, MapPuzzleKind.ChainSum,
                MapPuzzleKind.Pattern, MapPuzzleKind.Symmetry, MapPuzzleKind.Balance,
                MapPuzzleKind.NumberPath, MapPuzzleKind.Shape
            }, PuzzleGenerator.MapPuzzleKindsForTier(2));
            CollectionAssert.AreEquivalent(new[]
            {
                MapPuzzleKind.Formula, MapPuzzleKind.MakeTen, MapPuzzleKind.ChainSum,
                MapPuzzleKind.Pattern, MapPuzzleKind.Symmetry, MapPuzzleKind.Balance,
                MapPuzzleKind.NumberPath, MapPuzzleKind.NumberSequence, MapPuzzleKind.Shape
            }, PuzzleGenerator.MapPuzzleKindsForTier(3));

            for (int tier = 1; tier <= 4; tier++)
            {
                var gate = PuzzleGenerator.GatePuzzleKinds(new Rng((uint)tier), tier);
                Assert.AreEqual(3, gate.Count);
                Assert.AreEqual(3, new HashSet<MapPuzzleKind>(gate).Count);
                Assert.Contains(MapPuzzleKind.Formula, gate);
                foreach (var kind in gate)
                    Assert.Contains(kind, PuzzleGenerator.MapPuzzleKindsForTier(tier));
            }
        }

        [Test]
        public void RandomMapPractice_KeepsArithmeticAsTheCoreSkill()
        {
            for (int tier = 1; tier <= 4; tier++)
            {
                int formulas = 0;
                var rng = new Rng((uint)(800 + tier));
                for (int i = 0; i < 1000; i++)
                    if (PuzzleGenerator.PickMapPuzzleKind(rng, tier) == MapPuzzleKind.Formula) formulas++;
                Assert.That(formulas, Is.InRange(300, 400), $"tier {tier}: {formulas}");
            }
        }
    }
}
