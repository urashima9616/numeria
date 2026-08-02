using System.Collections.Generic;
using NUnit.Framework;
using Numeria.Core;

namespace Numeria.Core.Tests
{
    public class PatternPuzzleTests
    {
        [Test]
        public void Pattern_HasOneAnswerAndFourUniqueChoices()
        {
            for (uint seed = 1; seed <= 100; seed++)
            {
                var puzzle = PuzzleGenerator.GeneratePattern(new Rng(seed));
                Assert.AreEqual(4, puzzle.Candidates.Count);
                Assert.AreEqual(4, new HashSet<ShapeKind>(puzzle.Candidates).Count);
                Assert.Contains(puzzle.Answer, puzzle.Candidates);
                Assert.True(PuzzleGenerator.CheckPattern(puzzle, puzzle.Answer));
                Assert.AreEqual("What comes next in the pattern?", puzzle.Prompt);
            }
        }

        [Test]
        public void PatternSequence_MatchesItsDeclaredRule()
        {
            for (uint seed = 1; seed <= 100; seed++)
            {
                var p = PuzzleGenerator.GeneratePattern(new Rng(seed));
                switch (p.Rule)
                {
                    case PatternRule.Alternating:
                        CollectionAssert.AreEqual(
                            new[] { p.Sequence[0], p.Answer, p.Sequence[0], p.Answer, p.Sequence[0] },
                            p.Sequence);
                        break;
                    case PatternRule.Pairs:
                        Assert.AreEqual(p.Sequence[0], p.Sequence[1]);
                        Assert.AreEqual(p.Sequence[2], p.Sequence[3]);
                        Assert.AreEqual(p.Sequence[0], p.Sequence[4]);
                        Assert.AreEqual(p.Sequence[0], p.Sequence[5]);
                        Assert.AreEqual(p.Sequence[2], p.Answer);
                        break;
                    case PatternRule.CycleThree:
                        Assert.AreEqual(p.Sequence[0], p.Sequence[3]);
                        Assert.AreEqual(p.Sequence[1], p.Sequence[4]);
                        Assert.AreEqual(p.Sequence[2], p.Answer);
                        break;
                    case PatternRule.CycleFour:
                        Assert.Fail("Tier-one patterns must not use the tier-three CycleFour rule.");
                        break;
                }
            }
        }

        [Test]
        public void PatternDifficulty_ProgressesByTier()
        {
            for (uint seed = 1; seed <= 50; seed++)
            {
                var tier1 = PuzzleGenerator.GeneratePattern(new Rng(seed), 1);
                Assert.That(tier1.Rule, Is.EqualTo(PatternRule.Alternating).Or.EqualTo(PatternRule.Pairs));

                var tier2 = PuzzleGenerator.GeneratePattern(new Rng(seed), 2);
                Assert.That(tier2.Rule, Is.EqualTo(PatternRule.Pairs).Or.EqualTo(PatternRule.CycleThree));

                var tier3 = PuzzleGenerator.GeneratePattern(new Rng(seed), 3);
                Assert.AreEqual(PatternRule.CycleFour, tier3.Rule);
                Assert.AreEqual(7, tier3.Sequence.Count);
                Assert.AreEqual(tier3.Sequence[0], tier3.Sequence[4]);
                Assert.AreEqual(tier3.Sequence[1], tier3.Sequence[5]);
                Assert.AreEqual(tier3.Sequence[2], tier3.Sequence[6]);
                Assert.Contains(tier3.Answer, tier3.Candidates);
            }
        }
    }
}
