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
        public void MountainChainSum_StaysWithinTwenty()
        {
            for (uint seed = 1; seed <= 80; seed++)
            {
                var puzzle = PuzzleGenerator.GenerateChainSum(new Rng(seed));
                Assert.AreEqual(3, puzzle.Terms.Count);
                Assert.AreEqual(puzzle.Terms[0] + puzzle.Terms[1] + puzzle.Terms[2], puzzle.Answer);
                Assert.That(puzzle.Answer, Is.LessThanOrEqualTo(20));
                Assert.Contains(puzzle.Answer, puzzle.Candidates);
                Assert.True(PuzzleGenerator.CheckChainSum(puzzle, puzzle.Answer));
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

                var rotation = PuzzleGenerator.GenerateRotation(new Rng(seed));
                Assert.AreEqual(4, new HashSet<DirectionKind>(rotation.Candidates).Count);
                Assert.AreEqual((DirectionKind)(((int)rotation.Start + rotation.QuarterTurns) % 4),
                    rotation.Answer);
                Assert.True(PuzzleGenerator.CheckRotation(rotation, rotation.Answer));

                var sequence = PuzzleGenerator.GenerateNumberSequence(new Rng(seed));
                Assert.AreEqual(sequence.Step, sequence.Sequence[1] - sequence.Sequence[0]);
                Assert.AreEqual(sequence.Step, sequence.Sequence[2] - sequence.Sequence[1]);
                Assert.AreEqual(sequence.Sequence[2] + sequence.Step, sequence.Answer);
                Assert.Contains(sequence.Answer, sequence.Candidates);
                Assert.True(PuzzleGenerator.CheckNumberSequence(sequence, sequence.Answer));
            }
        }

        [Test]
        public void EveryMapTier_HasItsDesignPool_AndGateUsesThreeDifferentKinds()
        {
            CollectionAssert.AreEquivalent(
                new[] { MapPuzzleKind.Formula, MapPuzzleKind.Counting, MapPuzzleKind.Comparison },
                PuzzleGenerator.MapPuzzleKindsForTier(1));
            CollectionAssert.AreEquivalent(
                new[] { MapPuzzleKind.Formula, MapPuzzleKind.MakeTen, MapPuzzleKind.ChainSum },
                PuzzleGenerator.MapPuzzleKindsForTier(2));
            CollectionAssert.AreEquivalent(
                new[] { MapPuzzleKind.Pattern, MapPuzzleKind.Symmetry,
                    MapPuzzleKind.Rotation, MapPuzzleKind.NumberSequence },
                PuzzleGenerator.MapPuzzleKindsForTier(3));

            for (int tier = 1; tier <= 3; tier++)
            {
                var gate = PuzzleGenerator.GatePuzzleKinds(new Rng((uint)tier), tier);
                Assert.AreEqual(3, gate.Count);
                Assert.AreEqual(3, new HashSet<MapPuzzleKind>(gate).Count);
                foreach (var kind in gate)
                    Assert.Contains(kind, PuzzleGenerator.MapPuzzleKindsForTier(tier));
            }
        }
    }
}
