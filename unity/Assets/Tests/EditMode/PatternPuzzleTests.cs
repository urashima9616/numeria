using System.Collections.Generic;
using NUnit.Framework;
using Numeria.Core;

namespace Numeria.Core.Tests
{
    public class PatternPuzzleTests
    {
        [Test]
        public void Pattern_HasOneAnswerAndFourUniqueShapeColorChoices()
        {
            for (int tier = 1; tier <= 4; tier++)
            for (uint seed = 1; seed <= 100; seed++)
            {
                var puzzle = PuzzleGenerator.GeneratePattern(new Rng(seed), tier);
                Assert.AreEqual(4, puzzle.Candidates.Count);
                Assert.AreEqual(4, new HashSet<PatternToken>(puzzle.Candidates).Count);
                Assert.Contains(puzzle.Answer, puzzle.Candidates);
                Assert.AreEqual(puzzle.Sequence[puzzle.MissingIndex], puzzle.Answer);
                Assert.True(PuzzleGenerator.CheckPattern(puzzle, puzzle.Answer));
                Assert.That(puzzle.MissingIndex, Is.InRange(1, puzzle.Sequence.Count - 1));
            }
        }

        [Test]
        public void PatternSequence_RepeatsItsDeclaredPeriod()
        {
            for (int tier = 1; tier <= 4; tier++)
            for (uint seed = 1; seed <= 100; seed++)
            {
                var puzzle = PuzzleGenerator.GeneratePattern(new Rng(seed), tier);
                Assert.That(puzzle.Period, Is.InRange(2, 6));
                for (int i = puzzle.Period; i < puzzle.Sequence.Count; i++)
                    Assert.AreEqual(puzzle.Sequence[i % puzzle.Period], puzzle.Sequence[i],
                        $"tier {tier}, seed {seed}, index {i}");
            }
        }

        [Test]
        public void PatternDifficulty_UsesSeveralRulesAndBothPromptStyles()
        {
            var tier1Rules = new HashSet<PatternRule>();
            var tier2Rules = new HashSet<PatternRule>();
            var tier3Rules = new HashSet<PatternRule>();
            var prompts = new HashSet<string>();
            for (uint seed = 1; seed <= 200; seed++)
            {
                var tier1 = PuzzleGenerator.GeneratePattern(new Rng(seed), 1);
                var tier2 = PuzzleGenerator.GeneratePattern(new Rng(seed), 2);
                var tier3 = PuzzleGenerator.GeneratePattern(new Rng(seed), 3);
                tier1Rules.Add(tier1.Rule);
                tier2Rules.Add(tier2.Rule);
                tier3Rules.Add(tier3.Rule);
                prompts.Add(tier1.Prompt);

                Assert.That(tier1.Rule, Is.EqualTo(PatternRule.Alternating)
                    .Or.EqualTo(PatternRule.Pairs).Or.EqualTo(PatternRule.AlternatingColors));
                Assert.That(tier2.Rule, Is.EqualTo(PatternRule.Pairs)
                    .Or.EqualTo(PatternRule.CycleThree).Or.EqualTo(PatternRule.AlternatingColors)
                    .Or.EqualTo(PatternRule.AabCycle));
                Assert.That(tier3.Rule, Is.EqualTo(PatternRule.CycleThree)
                    .Or.EqualTo(PatternRule.CycleFour).Or.EqualTo(PatternRule.AabCycle)
                    .Or.EqualTo(PatternRule.MirrorRepeat).Or.EqualTo(PatternRule.ShapeColorCycle));
            }

            Assert.GreaterOrEqual(tier1Rules.Count, 3);
            Assert.GreaterOrEqual(tier2Rules.Count, 4);
            Assert.GreaterOrEqual(tier3Rules.Count, 5);
            CollectionAssert.Contains(prompts, "What tile comes next in the pattern?");
            CollectionAssert.Contains(prompts, "Which tile fills the pattern gap?");
        }
    }
}
