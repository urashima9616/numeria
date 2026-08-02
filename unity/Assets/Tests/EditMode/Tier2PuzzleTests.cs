using System.Collections.Generic;
using NUnit.Framework;
using Numeria.Core;

namespace Numeria.Core.Tests
{
    public class Tier2PuzzleTests
    {
        [Test]
        public void Subtraction_IsConsistentAndAnswerable()
        {
            for (uint seed = 1; seed <= 50; seed++)
            {
                var p = PuzzleGenerator.GenerateSubtraction(new Rng(seed), max: 20);
                Assert.AreEqual('-', p.Op);
                Assert.False(p.SlotIsResult);
                Assert.AreEqual(p.Sum, p.A - p.Missing, $"seed {seed}");
                Assert.That(p.A, Is.InRange(3, 20));
                Assert.That(p.Missing, Is.GreaterThanOrEqualTo(1));
                Assert.That(p.Sum, Is.GreaterThanOrEqualTo(1));
                Assert.AreEqual(4, p.Candidates.Count);
                Assert.Contains(p.Missing, p.Candidates);
                Assert.AreEqual(4, new HashSet<int>(p.Candidates).Count);
                Assert.True(PuzzleGenerator.CheckFormula(p, p.Missing));
            }
        }

        [Test]
        public void Subtraction_PromptReadsNumbers()
        {
            var p = PuzzleGenerator.GenerateSubtraction(new Rng(9), max: 10);
            StringAssert.Contains("take away what leaves", p.Prompt);
            StringAssert.Contains(PuzzleGenerator.NumberWord(p.Sum), p.Prompt);
        }

        [Test]
        public void Double_IsConsistent()
        {
            for (uint seed = 1; seed <= 50; seed++)
            {
                var p = PuzzleGenerator.GenerateDouble(new Rng(seed), max: 20);
                Assert.True(p.SlotIsResult);
                Assert.AreEqual(p.A * 2, p.Missing, $"seed {seed}");
                Assert.AreEqual(p.Missing, p.Sum);
                Assert.That(p.A, Is.InRange(2, 10));
                Assert.That(p.Missing, Is.LessThanOrEqualTo(20));
                Assert.Contains(p.Missing, p.Candidates);
                Assert.AreEqual(p.Prompt, $"What is double {PuzzleGenerator.NumberWord(p.A)}?");
            }
        }

        [Test]
        public void MakeTen_SupportsTarget12()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                var p = PuzzleGenerator.GenerateMakeTen(new Rng(seed), target: 12, handSize: 4);
                int pairs = 0;
                for (int i = 0; i < 4; i++)
                    for (int j = i + 1; j < 4; j++)
                        if (p.Hand[i] + p.Hand[j] == 12) pairs++;
                Assert.AreEqual(1, pairs, $"seed {seed}");
                Assert.AreEqual("Pick two crystals that make twelve!", p.Prompt);
            }
        }

        [Test]
        public void Progress_NewFieldsDefaults()
        {
            var p = new Progress();
            Assert.False(p.HasEvoStone);
            Assert.False(p.Evolved);
            Assert.AreEqual("forest", p.CurrentMap);
            Assert.IsEmpty(p.ClearedGates);
        }
    }
}
