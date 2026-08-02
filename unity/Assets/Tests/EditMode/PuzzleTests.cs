using System.Collections.Generic;
using NUnit.Framework;
using Numeria.Core;

namespace Numeria.Core.Tests
{
    public class PuzzleTests
    {
        [Test]
        public void Rng_IsDeterministic_InUnitRange()
        {
            var a = new Rng(42);
            var b = new Rng(42);
            for (int i = 0; i < 10; i++)
            {
                double v = a.Next();
                Assert.AreEqual(v, b.Next());
                Assert.That(v, Is.GreaterThanOrEqualTo(0).And.LessThan(1));
            }
        }

        [Test]
        public void NumberWord_Covers0To30()
        {
            Assert.AreEqual("zero", PuzzleGenerator.NumberWord(0));
            Assert.AreEqual("seven", PuzzleGenerator.NumberWord(7));
            Assert.AreEqual("thirteen", PuzzleGenerator.NumberWord(13));
            Assert.AreEqual("twenty", PuzzleGenerator.NumberWord(20));
            Assert.AreEqual("twenty-five", PuzzleGenerator.NumberWord(25));
            Assert.AreEqual("thirty", PuzzleGenerator.NumberWord(30));
        }

        [Test]
        public void FormulaPuzzle_IsConsistentAndAnswerable()
        {
            for (uint seed = 1; seed <= 50; seed++)
            {
                var p = PuzzleGenerator.GenerateFormula(new Rng(seed), max: 10);
                Assert.AreEqual(p.Sum, p.A + p.Missing, $"seed {seed}");
                Assert.That(p.Sum, Is.LessThanOrEqualTo(10));
                Assert.That(p.A, Is.GreaterThanOrEqualTo(1));
                Assert.That(p.Missing, Is.GreaterThanOrEqualTo(1));
                Assert.AreEqual(4, p.Candidates.Count);
                Assert.Contains(p.Missing, p.Candidates);
                Assert.AreEqual(4, new HashSet<int>(p.Candidates).Count);
                foreach (int c in p.Candidates)
                {
                    Assert.That(c, Is.InRange(0, 10));
                    if (c != p.Missing) Assert.False(PuzzleGenerator.CheckFormula(p, c));
                }
                Assert.True(PuzzleGenerator.CheckFormula(p, p.Missing));
            }
        }

        [Test]
        public void FormulaPrompt_ReadsNumbersAsWords()
        {
            var p = PuzzleGenerator.GenerateFormula(new Rng(7), max: 10);
            string cap = char.ToUpperInvariant(PuzzleGenerator.NumberWord(p.A)[0]) +
                         PuzzleGenerator.NumberWord(p.A).Substring(1);
            Assert.AreEqual($"{cap} plus what makes {PuzzleGenerator.NumberWord(p.Sum)}?", p.Prompt);
        }

        [Test]
        public void MakeTenPuzzle_HasExactlyOneValidPair()
        {
            for (uint seed = 1; seed <= 50; seed++)
            {
                var p = PuzzleGenerator.GenerateMakeTen(new Rng(seed), target: 10, handSize: 4);
                Assert.AreEqual(4, p.Hand.Count);

                int pairs = 0;
                for (int i = 0; i < 4; i++)
                    for (int j = i + 1; j < 4; j++)
                        if (p.Hand[i] + p.Hand[j] == 10) pairs++;
                Assert.AreEqual(1, pairs, $"seed {seed} hand [{string.Join(",", p.Hand)}]");

                var pair = PuzzleGenerator.FindMakeTenPair(p.Hand, 10);
                Assert.NotNull(pair);
                Assert.True(PuzzleGenerator.CheckMakeTen(p, pair.Value.i, pair.Value.j));
                Assert.AreEqual("Pick two crystals that make ten!", p.Prompt);
            }
        }

        [Test]
        public void FindMakeTenPair_ReturnsNullWhenNoPair()
        {
            Assert.IsNull(PuzzleGenerator.FindMakeTenPair(new List<int> { 1, 2, 3, 4 }, 10));
        }
    }
}
