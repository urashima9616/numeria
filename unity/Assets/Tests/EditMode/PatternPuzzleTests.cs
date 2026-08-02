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
                }
            }
        }
    }
}
