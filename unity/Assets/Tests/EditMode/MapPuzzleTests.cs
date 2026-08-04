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
        public void ChainSums_ScaleFromThreeTermsWithinTwenty_ToFourWithinThirty()
        {
            for (uint seed = 1; seed <= 80; seed++)
            {
                var tier2 = PuzzleGenerator.GenerateChainSum(new Rng(seed), 20, 3);
                Assert.AreEqual(3, tier2.Terms.Count);
                Assert.AreEqual(tier2.Terms[0] + tier2.Terms[1] + tier2.Terms[2], tier2.Answer);
                Assert.That(tier2.Answer, Is.LessThanOrEqualTo(20));
                Assert.Contains(tier2.Answer, tier2.Candidates);

                var tier3 = PuzzleGenerator.GenerateChainSum(new Rng(seed), 30, 4);
                Assert.AreEqual(4, tier3.Terms.Count);
                Assert.AreEqual(tier3.Terms[0] + tier3.Terms[1] + tier3.Terms[2] + tier3.Terms[3],
                    tier3.Answer);
                Assert.That(tier3.Answer, Is.LessThanOrEqualTo(30));
                Assert.True(PuzzleGenerator.CheckChainSum(tier3, tier3.Answer));
            }
        }

        [Test]
        public void SkyPuzzleFamilies_HaveUniqueValidAnswers()
        {
            for (uint seed = 1; seed <= 80; seed++)
            {
                var symmetry = PuzzleGenerator.GenerateSymmetry(new Rng(seed));
                Assert.AreEqual(4, new HashSet<ShapeKind>(symmetry.Candidates).Count);
                Assert.Contains(symmetry.Wing, symmetry.Candidates);
                Assert.True(PuzzleGenerator.CheckSymmetry(symmetry, symmetry.Wing));

                var rotation = PuzzleGenerator.GenerateRotation(new Rng(seed), 3);
                Assert.AreEqual(4, new HashSet<DirectionKind>(rotation.Candidates).Count);
                Assert.AreEqual((DirectionKind)(((int)rotation.Start + rotation.QuarterTurns) % 4),
                    rotation.Answer);
                Assert.True(PuzzleGenerator.CheckRotation(rotation, rotation.Answer));

                var sequence = PuzzleGenerator.GenerateNumberSequence(new Rng(seed), 3);
                Assert.AreEqual(sequence.Step, sequence.Sequence[1] - sequence.Sequence[0]);
                Assert.AreEqual(sequence.Step, sequence.Sequence[2] - sequence.Sequence[1]);
                Assert.AreEqual(sequence.Sequence[2] + sequence.Step, sequence.Answer);
                Assert.Contains(sequence.Answer, sequence.Candidates);
                Assert.That(sequence.Answer, Is.LessThanOrEqualTo(30));
                Assert.That(sequence.Step, Is.InRange(2, 5));
                Assert.True(PuzzleGenerator.CheckNumberSequence(sequence, sequence.Answer));
            }
        }

        [Test]
        public void FormulaDifficulty_UsesStrictTenTwentyThirtyFortyBounds()
        {
            for (int tier = 1; tier <= 4; tier++)
            {
                int max = PuzzleGenerator.MaxForTier(tier);
                Assert.AreEqual(tier * 10, max);
                for (uint seed = 1; seed <= 80; seed++)
                {
                    var add = PuzzleGenerator.GenerateFormula(new Rng(seed), max);
                    var subtract = PuzzleGenerator.GenerateSubtraction(new Rng(seed), max);
                    Assert.That(add.Sum, Is.InRange(0, max));
                    Assert.That(subtract.A, Is.InRange(0, max));
                    Assert.That(subtract.Sum, Is.InRange(0, max));
                }
            }
        }

        [Test]
        public void ShapeQuestions_ProgressFromNamingToProperties()
        {
            for (int tier = 1; tier <= 3; tier++)
            {
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
        }

        [Test]
        public void EveryMapTier_HasItsDesignPool_AndGateUsesThreeDifferentKinds()
        {
            CollectionAssert.AreEquivalent(
                new[] { MapPuzzleKind.Formula, MapPuzzleKind.Pattern, MapPuzzleKind.Symmetry,
                    MapPuzzleKind.Shape, MapPuzzleKind.Counting, MapPuzzleKind.Comparison },
                PuzzleGenerator.MapPuzzleKindsForTier(1));
            CollectionAssert.AreEquivalent(
                new[] { MapPuzzleKind.Formula, MapPuzzleKind.MakeTen, MapPuzzleKind.ChainSum,
                    MapPuzzleKind.Pattern, MapPuzzleKind.Symmetry, MapPuzzleKind.Rotation,
                    MapPuzzleKind.Shape },
                PuzzleGenerator.MapPuzzleKindsForTier(2));
            CollectionAssert.AreEquivalent(
                new[] { MapPuzzleKind.Formula, MapPuzzleKind.MakeTen, MapPuzzleKind.ChainSum,
                    MapPuzzleKind.Pattern, MapPuzzleKind.Symmetry, MapPuzzleKind.Rotation,
                    MapPuzzleKind.NumberSequence, MapPuzzleKind.Shape },
                PuzzleGenerator.MapPuzzleKindsForTier(3));

            for (int tier = 1; tier <= 3; tier++)
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
            for (int tier = 1; tier <= 3; tier++)
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
