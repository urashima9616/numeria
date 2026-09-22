using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Numeria.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    public partial class PuzzleUi
    {
        private readonly MathSettings _math;
        private readonly int _chapter;
        private int LearningPatternTier()
        {
            var mode = _math.Mode == MathMode.Random ? (MathMode)_rng.Pick(0, 2) : _math.Mode;
            int high = mode == MathMode.FeelGood ? 1 : _math.Grade == 0 ? 2 : 3;
            return MathCurriculum.ChapterSteps(1, high, _chapter);
        }
        private IEnumerator RunLearning(Action<bool> done, LearningKind? kind)
        {
            // Preserve visual-pattern/symmetry interactions in the mixed curriculum as well.
            if (!kind.HasValue && _rng.Next() < .16)
            {
                if (_math.Grade == 0 && _rng.Next() < .5) yield return RunPattern(done);
                else yield return RunSymmetry(done, 1, true);
                yield break;
            }
            var q = MathCurriculum.Generate(_math, _rng, kind, _chapter);
            LastSpell = null;
            string solved = q.Display.Replace("?", q.Answer);
            if (q.Kind == LearningKind.Arithmetic || q.Kind == LearningKind.MissingNumber)
            {
                LastSpell = new SpellTrace { SolvedEquation = solved };
                var match = Regex.Match(solved, @"^(\d+) ([+\-]) (\d+) = (\d+)$");
                if (match.Success)
                {
                    LastSpell.A = int.Parse(match.Groups[1].Value); LastSpell.B = int.Parse(match.Groups[3].Value);
                    LastSpell.Total = int.Parse(match.Groups[4].Value); LastSpell.Operation = match.Groups[2].Value[0];
                }
            }
            else if (q.Kind == LearningKind.Pattern)
                LastSpell = new SpellTrace { Numbers = Regex.Matches(solved,@"\d+").Cast<Match>().Select(m=>int.Parse(m.Value)).ToArray() };
            var shade = Ui.Img(_canvasRoot, "LearningSpell", new Color(.04f, .08f, .12f, .94f)); Ui.Stretch(shade.rectTransform);
            var card = Ui.SpriteImg(shade.transform, "Card", SpriteLib.Pack("UI/Panels/Generic_Panel")); card.type = Image.Type.Sliced;
            var rt = card.rectTransform; rt.anchorMin = new Vector2(.08f, .08f); rt.anchorMax = new Vector2(.92f, .92f); rt.offsetMin = rt.offsetMax = Vector2.zero;
            var title = Ui.Label(card.transform, "Prompt", q.Prompt, 34, Ui.Hex("#253e2b"));
            title.enableAutoSizing = true; title.fontSizeMin = 22; title.fontSizeMax = 34;
            Ui.PlaceCentered(title.rectTransform, new Vector2(.5f, .89f), Vector2.zero, new Vector2(1080, 76));
            var display = Ui.Label(card.transform, "Spell", q.Display, q.Diagram == null ? 58 : 30, Ui.Hex("#805220"));
            display.enableAutoSizing = true; display.fontSizeMin = 24; display.fontSizeMax = q.Diagram == null ? 58 : 34; display.textWrappingMode = TextWrappingModes.Normal;
            Ui.PlaceCentered(display.rectTransform, new Vector2(.5f, q.Diagram == null ? .61f : .77f), Vector2.zero, new Vector2(1080, q.Diagram == null ? 210 : 92));
            if (q.Diagram != null)
            {
                var diagram = Ui.Node(card.transform, "Diagram");
                Ui.PlaceCentered(diagram, new Vector2(.5f, .56f), Vector2.zero, new Vector2(750, 230));
                diagram.gameObject.AddComponent<LearningDiagram>().Question = q;
                if (q.Diagram == "bars")
                    for (int i = 0; i < q.Values.Length; i++)
                    {
                        var label = Ui.Label(diagram, "BarLabel" + i, $"{q.Labels[i]}  {q.Values[i]}", 24, Ui.Hex("#253e2b"), TextAnchor.MiddleRight);
                        Ui.PlaceCentered(label.rectTransform, new Vector2(0, 1), new Vector2(-88, -38 - i * 72), new Vector2(140, 44));
                    }
                if (q.Diagram == "clock")
                    for (int i = 1; i <= 12; i++)
                    {
                        float angle = i * Mathf.PI / 6;
                        var digit = Ui.Label(diagram, "Hour" + i, i.ToString(), 24, Ui.Hex("#253e2b"));
                        Ui.PlaceCentered(digit.rectTransform, new Vector2(.5f, .5f), new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * 103, new Vector2(36, 32));
                    }
            }
            var hint = Ui.Label(card.transform, "Hint", q.Difficulty == MathMode.FeelGood ? q.Hint : "", 23, Ui.Hex("#53613b"));
            hint.textWrappingMode = TextWrappingModes.Normal;
            Ui.PlaceCentered(hint.rectTransform, new Vector2(.5f, .09f), Vector2.zero, new Vector2(1050, 65));
            bool? answer = null; int attempts = 0; var buttons = new List<Button>();
            for (int i = 0; i < q.Options.Length; i++)
            {
                int index = i;
                var button = Ui.Btn(card.transform, "Answer" + i, q.Options[i], 36); buttons.Add(button);
                Ui.PlaceCentered((RectTransform)button.transform, new Vector2(.5f, .28f), new Vector2((i % 2 == 0 ? -1 : 1) * 235, i < 2 ? 48 : -48), new Vector2(430, 82));
                button.onClick.AddListener(() =>
                {
                    if (answer.HasValue || !button.interactable) return;
                    if (index == q.CorrectIndex) { answer = true; Sfx.Play(SfxCue.Correct); Say("Great job!"); }
                    else
                    {
                        attempts++; button.interactable = false; hint.text = q.Hint;
                        Say("Let's try again!", q.Hint); Sfx.Play(SfxCue.SoftMiss);
                        if (attempts >= 2) { answer = false; hint.text = "The answer is " + q.Answer + ". " + q.Hint; }
                    }
                });
            }
            _say(LearningSpeech.Lines(q));
            while (!answer.HasValue) yield return null;
            foreach (var button in buttons) button.interactable = false;
            yield return new WaitForSeconds(answer.Value ? .5f : 2f);
            UnityEngine.Object.Destroy(shade.gameObject); Complete(done, answer.Value);
        }
    }

    /// <summary>Small vector teaching diagrams: exact partitions and hands, never AI-rendered math.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LearningDiagram : MaskableGraphic
    {
        public LearningQuestion Question;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (Question?.Values == null) return;
            var q = Question; var v = q.Values; Color ink = Ui.Hex("#334b39"), gold = Ui.Hex("#edb448"), pale = Ui.Hex("#ded5b5");
            void Polygon(Vector2[] points, Color c)
            {
                int first = vh.currentVertCount;
                foreach (var p in points) vh.AddVert(p, c, Vector2.zero);
                for (int i = 1; i < points.Length - 1; i++) vh.AddTriangle(first, first + i, first + i + 1);
            }
            void Box(float x, float y, float w, float h, Color c) => Polygon(new[] { new Vector2(x,y), new Vector2(x+w,y), new Vector2(x+w,y+h), new Vector2(x,y+h) }, c);
            void Disk(Vector2 center, float radius, Color c, int sides = 48)
            {
                var p = new Vector2[sides]; for (int i = 0; i < sides; i++) { float a = i * Mathf.PI * 2 / sides + Mathf.PI / 2; p[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius; } Polygon(p,c);
            }
            void Line(Vector2 start, Vector2 end, float width, Color c)
            {
                Vector2 d = end-start, normal = new Vector2(-d.y,d.x).normalized * width / 2;
                Polygon(new[] {start-normal,start+normal,end+normal,end-normal},c);
            }
            switch (q.Diagram)
            {
                case "clock":
                    Disk(Vector2.zero,133,ink); Disk(Vector2.zero,128,Color.white);
                    float hour = (v[0] + v[1]/60f) * Mathf.PI/6, minute = v[1]*Mathf.PI/30;
                    Line(Vector2.zero,new Vector2(Mathf.Sin(hour),Mathf.Cos(hour))*65,8,ink);
                    Line(Vector2.zero,new Vector2(Mathf.Sin(minute),Mathf.Cos(minute))*91,4,Ui.Hex("#bd6432")); Disk(Vector2.zero,6,ink); break;
                case "polygon": Disk(Vector2.zero,106,ink,v[0]); Disk(Vector2.zero,96,gold,v[0]); break;
                case "rectangle":
                    Box(-160,-95,320,190,ink); Box(-154,-89,308,178,gold); break;
                case "fraction":
                    float width = 650f/v[1]; for(int i=0;i<v[1];i++) { Box(-325+i*width,-65,width-3,130,ink); Box(-321+i*width,-61,width-11,122,i<v[0]?gold:pale); } break;
                case "bars":
                    Color[] colors = { Ui.Hex("#c87157"), Ui.Hex("#608dad"),gold };
                    for(int i=0;i<v.Length;i++) for(int n=0;n<v[i];n++) Box(-375+n*76,55-i*72,71,46,colors[i]); break;
                case "tenframe":
                    for(int i=0;i<20;i++) { int group=i/10,k=i%10; float x=-330+(k%5)*60+group*355, y=40-(k/5)*70; Box(x-24,y-24,48,48,pale); if(i<v[0])Disk(new Vector2(x,y),17,i<10?ink:gold); } break;
            }
        }
    }
}
