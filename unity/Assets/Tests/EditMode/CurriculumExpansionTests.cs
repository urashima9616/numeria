using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Numeria.Core.Tests
{
    public class CurriculumExpansionTests
    {
        private static decimal Value(string expression)
        {
            var tokens=Regex.Matches(expression,@"\d+(?:\.\d+)?|[()+\-x/]").Cast<Match>().Select(m=>m.Value).ToArray();int pos=0;
            decimal Atom(){if(tokens[pos]=="("){pos++;decimal a=Sum();Assert.AreEqual(")",tokens[pos++]);return a;}return decimal.Parse(tokens[pos++],CultureInfo.InvariantCulture);}
            decimal Product(){decimal a=Atom();while(pos<tokens.Length&&(tokens[pos]=="x"||tokens[pos]=="/")){string op=tokens[pos++];decimal b=Atom();a=op=="x"?a*b:a/b;}return a;}
            decimal Sum(){decimal a=Product();while(pos<tokens.Length&&(tokens[pos]=="+"||tokens[pos]=="-")){string op=tokens[pos++];decimal b=Product();a=op=="+"?a+b:a-b;}return a;}
            decimal result=Sum();Assert.AreEqual(tokens.Length,pos,expression);return result;
        }

        [Test] public void GradeInventoriesAreExplicitAndHaveDistinctCoverage()
        {
            CollectionAssert.DoesNotContain(MathCurriculum.Available(0),LearningKind.Fractions);
            CollectionAssert.DoesNotContain(MathCurriculum.Available(1),LearningKind.EqualGroups);
            CollectionAssert.DoesNotContain(MathCurriculum.Available(2),LearningKind.FractionLine);
            CollectionAssert.DoesNotContain(MathCurriculum.Available(3),LearningKind.Units);
            CollectionAssert.DoesNotContain(MathCurriculum.Available(4),LearningKind.Coordinates);
            Assert.AreEqual(17,MathCurriculum.Available(0).Length);Assert.AreEqual(24,MathCurriculum.Available(5).Length);
            for(int grade=0;grade<6;grade++)
            {var kinds=MathCurriculum.Available(grade);Assert.AreEqual(kinds.Length,kinds.Distinct().Count());Assert.True(kinds.Contains(LearningKind.Bonds)&&kinds.Contains(LearningKind.WordProblem));}
        }

        [Test] public void ShuffleBagCoversEveryFamilyAndAvoidsRecentRepeatsAcrossSessions()
        {
            for(int g=0;g<6;g++)
            {
                var settings=new MathSettings {Grade=g};var rng=new Rng((uint)g+92);
                var session=LearningSession.For(settings);Assert.AreSame(session,LearningSession.For(settings));
                var pool=MathCurriculum.Available(g);var recent=new Queue<LearningKind>();
                for(int round=0;round<4;round++)
                {
                    var seen=new HashSet<LearningKind>();
                    for(int i=0;i<pool.Length;i++)
                    {
                        var q=session.Next(settings,rng,8);Assert.True(seen.Add(q.Kind));
                        // At the very end of a bag only already-recent choices may remain.
                        if(i<pool.Length-3)CollectionAssert.DoesNotContain(recent,q.Kind);
                        recent.Enqueue(q.Kind);if(recent.Count>3)recent.Dequeue();
                    }
                    CollectionAssert.AreEquivalent(pool,seen);
                }
                settings.Grade=0;CollectionAssert.Contains(MathCurriculum.Available(0),session.Next(settings,rng,1).Kind);
            }
        }

        [Test] public void ExpandedQuestionsAreSolvableBoundedAndMathematicallyCorrect()
        {
            var rng=new Rng(497);
            for(int g=0;g<6;g++)for(int mode=0;mode<4;mode++)for(int chapter=1;chapter<=8;chapter++)
            foreach(var kind in MathCurriculum.Available(g).Concat(new[]{LearningKind.Double,LearningKind.Subtraction,LearningKind.ChainSum}))
            for(int sample=0;sample<35;sample++)
            {
                var s=new MathSettings{Grade=g};s.Mode=(MathMode)mode;var q=MathCurriculum.Generate(s,rng,kind,chapter);
                Assert.AreEqual(kind,q.Kind);Assert.IsNotEmpty(q.Template);Assert.IsNotEmpty(q.Prompt);Assert.IsNotEmpty(q.Hint);
                Assert.That(q.Options.Length,Is.InRange(2,4));Assert.AreEqual(q.Options.Length,q.Options.Distinct().Count());
                Assert.AreEqual(q.Answer,q.Options[q.CorrectIndex]);
                int cap=MathCurriculum.ChapterLimit(g,q.Difficulty,chapter);
                if(q.Interaction==LearningInteraction.Pair)
                {
                    bool found=false;
                    for(int i=0;i<q.Tokens.Length;i++)for(int j=i+1;j<q.Tokens.Length;j++)
                        if(MathCurriculum.CheckSelection(q,new[]{i,j}))found=true;
                    Assert.True(found);Assert.False(MathCurriculum.CheckSelection(q,new[]{0,0}));
                    foreach(string n in q.Tokens)Assert.That(int.Parse(n),Is.InRange(0,cap));
                }
                if(q.Interaction==LearningInteraction.Order)
                {
                    var indices=q.Expected.Select(n=>Array.IndexOf(q.Tokens,n.ToString())).ToArray();
                    Assert.True(MathCurriculum.CheckSelection(q,indices));Assert.False(MathCurriculum.CheckSelection(q,indices.Take(2).ToArray()));
                    Assert.True(q.Expected.All(n=>n>=0&&n<=cap));
                }
                if(kind==LearningKind.Balance||kind==LearningKind.DecimalOps||kind==LearningKind.Double||kind==LearningKind.Subtraction||kind==LearningKind.ChainSum ||
                    (kind==LearningKind.Fractions && q.Display.Contains(" = ?") && !q.Display.Contains("?/") ))
                {
                    string[] equation=q.Display.Replace("?",q.Answer).Split('=');
                    Assert.Less(Math.Abs(Value(equation[0])-Value(equation[1])),.00000001m,q.Display);
                }
                if(kind==LearningKind.Time)
                {
                    foreach(string option in q.Options)
                    {var t=option.Split(':');Assert.That(int.Parse(t[0]),Is.InRange(1,12));Assert.That(int.Parse(t[1]),Is.InRange(0,59));if(g==1)Assert.AreEqual(0,int.Parse(t[1])%(mode==1||chapter<5?60:30));if(g==2)Assert.AreEqual(0,int.Parse(t[1])%5);}
                }
                if(kind==LearningKind.Fractions && q.Prompt.Contains("fraction") && !q.Prompt.Contains("Compare") && !q.Display.Contains("?/") )
                {
                    decimal answer=Value(q.Answer);
                    foreach(string wrong in q.Options.Where(x=>x!=q.Answer))Assert.AreNotEqual(answer,Value(wrong),q.Display);
                    if(g<=2) foreach(string option in q.Options)Assert.That(Value(option),Is.InRange(0m,1m));
                }
                if(kind==LearningKind.Data)
                {Assert.True(q.HideValues);Assert.False(LearningSpeech.Lines(q).Contains(q.Values[0].ToString()));}
            }
        }

        [Test] public void ArithmeticIncludesZeroAndThemedSkillsRetainTheirOperations()
        {
            var rng=new Rng(10);var s=new MathSettings();bool zeroOperand=false,zeroAnswer=false;
            for(int i=0;i<500;i++)
            {
                var q=MathCurriculum.Generate(s,rng,LearningKind.Arithmetic,8);
                zeroOperand|=q.Display.Contains(" 0 ")||q.Display.StartsWith("0 ");zeroAnswer|=q.Answer=="0";
                var twice=MathCurriculum.Generate(s,rng,LearningKind.Double,8);var nums=Regex.Matches(twice.Display,@"\d+");Assert.AreEqual(nums[0].Value,nums[1].Value);
                StringAssert.Contains(" - ",MathCurriculum.Generate(s,rng,LearningKind.Subtraction,8).Display);
                Assert.AreEqual(2,MathCurriculum.Generate(s,rng,LearningKind.ChainSum,8).Display.Count(c=>c=='+'));
            }
            Assert.True(zeroOperand&&zeroAnswer);
        }
    }
}
