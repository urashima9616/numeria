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
        private IEnumerator RunLearning(Action<bool> done, LearningKind? kind) =>
            RunLearningQuestion(LearningSession.For(_math).Next(_math, _rng, _chapter, kind), done);

        private IEnumerator RunLearningQuestion(LearningQuestion q, Action<bool> done)
        {
            LastSpell = null;
            string solved = q.Display.Replace("?", q.Answer);
            if (q.Kind == LearningKind.Arithmetic || q.Kind == LearningKind.MissingNumber ||
                q.Kind == LearningKind.Double || q.Kind == LearningKind.Subtraction || q.Kind == LearningKind.ChainSum)
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
                        var label = Ui.Label(diagram, "BarLabel" + i, q.HideValues ? q.Labels[i] : $"{q.Labels[i]}  {q.Values[i]}", 24, Ui.Hex("#253e2b"), TextAnchor.MiddleRight);
                        Ui.PlaceCentered(label.rectTransform, new Vector2(0, 1), new Vector2(-88, -38 - i * 72), new Vector2(140, 44));
                    }
                if (q.Diagram == "clock")
                    for (int i = 1; i <= 12; i++)
                    {
                        float angle = i * Mathf.PI / 6;
                        var digit = Ui.Label(diagram, "Hour" + i, i.ToString(), 24, Ui.Hex("#253e2b"));
                        Ui.PlaceCentered(digit.rectTransform, new Vector2(.5f, .5f), new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * 103, new Vector2(36, 32));
                    }
                if(q.Diagram=="fractionline" || q.Diagram=="coordinate")
                {
                    int max=q.Diagram=="coordinate"?8:1;
                    for(int i=0;i<=max;i++)
                    {
                        var tick=Ui.Label(diagram,"TickX"+i,i.ToString(),22,Ui.Ink);
                        Ui.PlaceCentered(tick.rectTransform,new Vector2(.5f,.5f),new Vector2(-280+i*560f/max,q.Diagram=="coordinate"?-108:-38),new Vector2(42,32));
                        if(q.Diagram=="coordinate")
                        {var y=Ui.Label(diagram,"TickY"+i,i.ToString(),20,Ui.Ink);Ui.PlaceCentered(y.rectTransform,new Vector2(.5f,.5f),new Vector2(-315,-80+i*24),new Vector2(36,28));}
                    }
                }
                if(q.Diagram=="sequence" || q.Diagram=="mirror")
                {
                    int missing=q.Diagram=="sequence"?Array.IndexOf(q.Values,-1):int.Parse(q.Labels[0]);
                    float x=q.Diagram=="sequence"?-300+missing*600f/(q.Values.Length-1):45+missing*75;
                    var mark=Ui.Label(diagram,"MissingShape","?",36,Ui.Ink);
                    Ui.PlaceCentered(mark.rectTransform,new Vector2(.5f,.5f),new Vector2(x,0),new Vector2(50,60));
                }
            }
            var hint = Ui.Label(card.transform, "Hint", q.Difficulty == MathMode.FeelGood ? q.Hint : "", 23, Ui.Hex("#53613b"));
            hint.textWrappingMode = TextWrappingModes.Normal;
            Ui.PlaceCentered(hint.rectTransform, new Vector2(.5f, .09f), Vector2.zero, new Vector2(1050, 65));
            bool? answer = null; int attempts = 0; var buttons = new List<Button>();
            bool interactive=q.Interaction!=LearningInteraction.Choice;
            var picked=new List<int>();
            TMP_Text selection=null;
            if(interactive)
            {
                selection=Ui.Label(card.transform,"Selection","",32,Ui.Ink);
                Ui.PlaceCentered(selection.rectTransform,new Vector2(.5f,.44f),Vector2.zero,new Vector2(1000,52));
                Ui.PlaceCentered(hint.rectTransform,new Vector2(.5f,.035f),Vector2.zero,new Vector2(1050,42));
            }
            void Submit(bool correct)
            {
                if(answer.HasValue)return;
                if(correct)
                {
                    answer=true;Sfx.Play(SfxCue.Correct);Say("Great job!");
                    if(q.Interaction==LearningInteraction.Pair)LastSpell=new SpellTrace {A=int.Parse(q.Tokens[picked[0]]),B=int.Parse(q.Tokens[picked[1]]),Total=q.Values[0]};
                    if(q.Interaction==LearningInteraction.Order)LastSpell=new SpellTrace {Numbers=q.Expected};
                }
                else
                {
                    attempts++;hint.text=q.Hint;Say("Let's try again!",q.Hint);Sfx.Play(SfxCue.SoftMiss);
                    if(attempts>=2) {answer=false;hint.text="The answer is "+(interactive?string.Join(" + ",q.Expected.Select(x=>x.ToString())):q.Answer)+". "+q.Hint;}
                }
            }
            for (int i = 0; i < (interactive?q.Tokens.Length:q.Options.Length); i++)
            {
                int index = i;
                var button = Ui.Btn(card.transform, "Answer" + i, interactive?q.Tokens[i]:q.Options[i], 36); buttons.Add(button);
                var text=button.GetComponentInChildren<TMP_Text>();text.enableAutoSizing=true;text.fontSizeMin=22;text.fontSizeMax=36;
                int shape=Array.IndexOf(new[]{"circle","triangle","square","rectangle","hexagon"},interactive?"":q.Options[i]);
                if(shape>=0)
                {
                    var icon=Ui.Node(button.transform,"ShapeOption");Ui.PlaceCentered(icon,new Vector2(0,.5f),new Vector2(48,0),new Vector2(60,60));
                    var graphic=icon.gameObject.AddComponent<LearningDiagram>();graphic.Question=new LearningQuestion{Diagram="choiceShape",Values=new[]{shape}};graphic.raycastTarget=false;
                    text.rectTransform.offsetMin+=new Vector2(70,0);
                }
                Ui.PlaceCentered((RectTransform)button.transform, new Vector2(.5f, .28f), new Vector2((i % 2 == 0 ? -1 : 1) * 235, i < 2 ? 48 : -48), new Vector2(430, 82));
                button.onClick.AddListener(() =>
                {
                    if (answer.HasValue || !button.interactable) return;
                    if(interactive)
                    {
                        if(picked.Count>=(q.Interaction==LearningInteraction.Pair?2:q.Expected.Length))return;
                        picked.Add(index);button.interactable=false;
                        selection.text=string.Join(q.Interaction==LearningInteraction.Pair?" + ":"  ,  ",picked.Select(i=>q.Tokens[i]));
                        return;
                    }
                    button.interactable=false;Submit(index==q.CorrectIndex);
                });
            }
            if(interactive)
            {
                var check=Ui.Btn(card.transform,"Check","CHECK",26);
                var reset=Ui.Btn(card.transform,"Clear","CLEAR",26);buttons.Add(check);buttons.Add(reset);
                Ui.PlaceCentered((RectTransform)check.transform,new Vector2(.5f,.12f),new Vector2(150,0),new Vector2(260,55));
                Ui.PlaceCentered((RectTransform)reset.transform,new Vector2(.5f,.12f),new Vector2(-150,0),new Vector2(260,55));
                void Clear(){picked.Clear();selection.text="";foreach(var button in buttons)button.interactable=true;}
                reset.onClick.AddListener(()=>{if(!answer.HasValue)Clear();});
                check.onClick.AddListener(()=>
                {
                    if(answer.HasValue || picked.Count!=(q.Interaction==LearningInteraction.Pair?2:q.Expected.Length))return;
                    Submit(MathCurriculum.CheckSelection(q,picked));if(!answer.HasValue)Clear();
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
            void Shape(int shape,Vector2 center,float radius)
            {
                if(shape<0) {Box(center.x-radius,center.y-radius,radius*2,radius*2,pale);return;}
                Color c=new[]{Ui.Hex("#4b86b4"),Ui.Hex("#bf7154"),gold,Ui.Hex("#759d56"),Ui.Hex("#936caa")}[shape%5];
                if(shape==2||shape==3) {float w=shape==3?radius*2.4f:radius*1.6f;Box(center.x-w/2,center.y-radius*.8f,w,radius*1.6f,c);}
                else Disk(center,radius,c,shape==0?40:shape==1?3:6);
            }
            switch (q.Diagram)
            {
                case "shape":Shape(v[0],Vector2.zero,90);break;
                case "choiceShape":Shape(v[0],Vector2.zero,24);break;
                case "dots":
                    for(int i=0;i<v[0]+v[1];i++)Disk(new Vector2(-240+(i%5)*110,60-(i/5)*90),25,i<v[0]?ink:gold);
                    break;
                case "array":
                    for(int r=0;r<v[0];r++)for(int c=0;c<v[1];c++)Disk(new Vector2((c-(v[1]-1)/2f)*55,90-r*42),13,gold);
                    break;
                case "collection":
                    for(int i=0;i<v.Length;i++)Shape(v[i],new Vector2(-240+(i%5)*120,55-(i/5)*110),32);
                    break;
                case "sequence":
                    for(int i=0;i<v.Length;i++)Shape(v[i],new Vector2(-300+i*600f/(v.Length-1),0),28);
                    break;
                case "mirror":
                    for(int i=0;i<v.Length;i++)
                    {Shape(v[i],new Vector2(-45-(v.Length-1-i)*75,0),25);Shape(i==int.Parse(q.Labels[0])?-1:v[v.Length-1-i],new Vector2(45+i*75,0),25);}
                    for(int y=-95;y<95;y+=22)Box(-2,y,4,12,ink);break;
                case "compose":
                    if(v[0]==0)
                    {Polygon(new[]{new Vector2(-90,-90),new Vector2(-90,90),new Vector2(90,-90)},gold);Polygon(new[]{new Vector2(-90,90),new Vector2(90,90),new Vector2(90,-90)},Ui.Hex("#75a394"));for(int i=0;i<9;i++)Line(new Vector2(-90+i*20,90-i*20),new Vector2(-80+i*20,80-i*20),3,ink);}
                    else {Box(-180,-90,180,180,gold);Box(0,-90,180,180,Ui.Hex("#75a394"));for(int y=-90;y<90;y+=20)Box(-2,y,4,10,ink);}
                    break;
                case "balance":
                    Polygon(new[]{new Vector2(0,30),new Vector2(-35,-95),new Vector2(35,-95)},ink);
                    Line(new Vector2(-230,40),new Vector2(230,40),7,ink);
                    foreach(int side in new[]{-1,1}) {Line(new Vector2(side*190,40),new Vector2(side*190,-35),5,ink);Box(side*190-80,-45,160,10,gold);}break;
                case "fractionline":
                    Line(new Vector2(-280,0),new Vector2(280,0),5,ink);
                    for(int i=0;i<=v[1];i++)Line(new Vector2(-280+560f*i/v[1],-15),new Vector2(-280+560f*i/v[1],15),3,ink);
                    Disk(new Vector2(-280+560f*v[0]/v[1],0),13,gold);break;
                case "coordinate":
                    for(int i=0;i<=8;i++)
                    {Line(new Vector2(-280+i*70,-80),new Vector2(-280+i*70,112),2,pale);Line(new Vector2(-280,-80+i*24),new Vector2(280,-80+i*24),2,pale);}
                    Line(new Vector2(-280,-80),new Vector2(280,-80),4,ink);Line(new Vector2(-280,-80),new Vector2(-280,112),4,ink);
                    Disk(new Vector2(-280+v[0]*70,-80+v[1]*24),10,gold);break;
                case "box":
                    Box(-150,-90,250,150,gold);
                    Polygon(new[]{new Vector2(-150,60),new Vector2(-80,110),new Vector2(170,110),new Vector2(100,60)},pale);
                    Polygon(new[]{new Vector2(100,-90),new Vector2(170,-40),new Vector2(170,110),new Vector2(100,60)},Ui.Hex("#aa8744"));break;
                case "clock":
                    Disk(Vector2.zero,133,ink); Disk(Vector2.zero,128,Color.white);
                    for(int i=0;i<60;i++)
                    {float angle=i*Mathf.PI/30;var direction=new Vector2(Mathf.Sin(angle),Mathf.Cos(angle));Line(direction*(i%5==0?114:120),direction*126,i%5==0?3:1.5f,ink);}
                    float hour = (v[0] + v[1]/60f) * Mathf.PI/6, minute = v[1]*Mathf.PI/30;
                    Line(Vector2.zero,new Vector2(Mathf.Sin(hour),Mathf.Cos(hour))*65,8,ink);
                    Line(Vector2.zero,new Vector2(Mathf.Sin(minute),Mathf.Cos(minute))*91,4,Ui.Hex("#bd6432")); Disk(Vector2.zero,6,ink); break;
                case "polygon": Disk(Vector2.zero,106,ink,v[0]); Disk(Vector2.zero,96,gold,v[0]); break;
                case "rectangle":
                    float scale=Math.Min(320f/Math.Max(1,v[0]),190f/Math.Max(1,v[1])),w=v[0]*scale,h=v[1]*scale;
                    Box(-w/2,-h/2,w,h,ink); Box(-w/2+4,-h/2+4,w-8,h-8,gold); break;
                case "fraction":
                    float width = 650f/v[1]; for(int i=0;i<v[1];i++) { Box(-325+i*width,-65,width-3,130,ink); Box(-321+i*width,-61,width-11,122,i<v[0]?gold:pale); } break;
                case "bars":
                    Color[] colors = { Ui.Hex("#c87157"), Ui.Hex("#608dad"),gold };
                    float unitWidth=Math.Min(76,700f/Math.Max(1,v.Max()));
                    for(int i=0;i<v.Length;i++) for(int n=0;n<v[i];n++) Box(-375+n*unitWidth,55-i*72,unitWidth-5,46,colors[i]); break;
                case "tenframe":
                    for(int i=0;i<20;i++) { int group=i/10,k=i%10; float x=-330+(k%5)*60+group*355, y=40-(k/5)*70; Box(x-24,y-24,48,48,pale); if(i<v[0])Disk(new Vector2(x,y),17,i<10?ink:gold); } break;
            }
        }
    }
}
