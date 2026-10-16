using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace Numeria.Core
{
    /// <summary>Six untimed spatial challenges. No answer-copying target arrays are exposed in UI.</summary>
    public sealed class HalloweenMechanism
    {
        public readonly int Kind, Grade, Complexity, Count, Share, Total;
        public readonly int[] BasketTargets, PatternChoices;
        public readonly int PatternDenominator=1;
        public readonly decimal PumpkinWeight;
        public bool NumberPattern=>Kind==2&&Grade>0;
        public bool FractionShares=>Kind==1&&Grade==4;
        public bool WeightShares=>Kind==1&&Grade==5;
        public bool CoordinateMirror=>Kind==3&&Grade==5;
        public readonly int[] Initial = new int[16], Cells = new int[16], Melody;
        public readonly int[] PortraitOrder;
        public readonly bool[] Wet = new bool[16];
        public int Step, Selected=-1;
        public bool Solved { get; private set; }
        private readonly Stack<int[]> _history=new Stack<int[]>();
        private readonly int _gap,_patternAnswer;
        private readonly bool _portraitClues;
        public int Remaining => Total-Cells.Take(Count).Sum();
        public int PatternGap => _gap;
        public string Instruction => FractionShares?"Share the harvest. Give each basket its labeled fraction of all the pumpkins.":
            WeightShares?"Each pumpkin has the same mass. Fill each basket to its labeled mass.":
            NumberPattern?"Find the missing number on the lanterns.":
            CoordinateMirror?"Reflect the guests across the mirror line. Use the grid coordinates to check their places.":
            _portraitClues?"Use the clues to arrange the portraits. Tap two portraits to swap them.":new[]{
            "Turn the channels. Carry water from the spring to both seeds.",
            "Share all the pumpkins equally between the baskets.",
            "Find the missing lantern in the pattern.",
            "Make the right side a mirror of the left side.",
            "Put the portraits in order, from fewest candles to most candles. Tap two portraits to swap them.",
            "Listen to the bells. Then play the same melody."
        }[Kind];
        public string Hint => Solved?"You did it!":FractionShares?"The whole is the harvest pile. Divide it into equal parts, then combine the parts you need.":
            WeightShares?"Add one pumpkin mass at a time. UNDO takes the last pumpkin back.":
            NumberPattern?"Compare neighboring lanterns. Use the same change at every step.":
            CoordinateMirror?"Keep the same y coordinate. Matching guests are equally far from the mirror.":
            Kind==0?"Blue channels carry water. Both seeds need a drink.":
            Kind==1?"Tap a basket to add one pumpkin. UNDO takes the last one back.":
            Kind==3?"The mirror line is in the middle. Look at matching places.":
            Kind==4?(_portraitClues?string.Join("   ",Enumerable.Range(0,Count-1).Select(i=>PortraitOrder[i]+" before "+PortraitOrder[i+1]+".")):"Compare the candles. The smallest group goes first."):
            Kind==5?"LISTEN plays the melody again. Take your time.":"Look for the part that repeats.";
        public HalloweenMechanism(int kind,MathSettings settings,Rng rng)
        {
            if(kind<0||kind>5)throw new ArgumentOutOfRangeException(nameof(kind));
            Kind=kind;int grade=Grade=Math.Max(0,Math.Min(5,settings.Grade));
            _portraitClues=kind==4&&grade>0;
            var mode=settings.Modes!=null&&settings.Modes.Length>grade?settings.Modes[grade]:MathMode.Normal;
            if(mode==MathMode.Random)mode=(MathMode)rng.Pick(0,2);
            Complexity=mode==MathMode.FeelGood?0:mode==MathMode.Challenging?2:1;
            Count=kind==1?(grade==0||Complexity==0?2:3):kind==4?(Complexity==0?3:grade<2?4:5):3;
            Share=grade==0?2+Complexity:Math.Min(6,3+Complexity+grade/2);
            Total=kind==1?Count*Share:0;
            BasketTargets=Enumerable.Repeat(Share,Count).ToArray();
            if(FractionShares)
            {
                int quarter=Complexity==0?2:Complexity==1?3:4;
                Total=quarter*4;
                BasketTargets=Count==2?new[]{quarter*2,quarter*2}:new[]{quarter,quarter*2,quarter};
            }
            if(WeightShares)
            {
                PumpkinWeight=Complexity==0?.5m:Complexity==1?.25m:.2m;
                BasketTargets=Enumerable.Range(0,Count).Select(i=>rng.Pick(2,Complexity==0?4:6)).ToArray();
                Total=BasketTargets.Sum();
            }
            if(kind==0)
            {
                int[] solution={14,10,10,10,5,0,0,0,5,0,0,0,3,10,10,10};
                int[] editable={0,2,12,1,4,8,3,13,14,15};
                int changed=Complexity==0?3:Complexity==1?6:editable.Length;
                Array.Copy(solution,Initial,16);
                foreach(int i in editable.Take(changed)){int mask=solution[i],turns=rng.Pick(1,3);for(int turn=0;turn<turns;turn++)mask=Rotate(mask);Initial[i]=mask;}
                // Never deliver an already-solved puzzle.
                Initial[0]=7;
            }
            if(kind==2)
            {
                if(grade==0)
                {
                    int length=Complexity==0?2:3;int offset=rng.Pick(0,2);
                    for(int i=0;i<8;i++)Initial[i]=(i%length+offset)%3;
                }
                else
                {
                    int cap=MathCurriculum.ChapterLimit(grade,mode,8);
                    int step=grade==1?Math.Min(Complexity==0?1:2,cap/8):grade==2?rng.Pick(2,3+Complexity):
                        grade==3?rng.Pick(2,4+Complexity*2):Complexity+1;
                    PatternDenominator=grade==4?(Complexity==0?2:Complexity==1?4:8):grade==5?10:1;
                    int start=rng.Pick(1,grade<=2?Math.Max(1,Math.Min(5,cap-7*step)):3);
                    for(int i=0;i<8;i++)Initial[i]=start+i*step;
                }
                _gap=Complexity==0?7:rng.Pick(3,6);_patternAnswer=Initial[_gap];
                PatternChoices=grade==0?new[]{0,1,2}:new[]{_patternAnswer-1,_patternAnswer,_patternAnswer+1};
                if(grade>0&&grade<=2&&PatternChoices[2]>MathCurriculum.ChapterLimit(grade,mode,8))
                    PatternChoices[2]=_patternAnswer-2;
                if(grade>0)for(int i=0;i<2;i++){int j=rng.Pick(i,2),v=PatternChoices[i];PatternChoices[i]=PatternChoices[j];PatternChoices[j]=v;}
            }
            if(kind==3)
            {
                var places=Enumerable.Range(0,8).ToArray();
                for(int i=0;i<4+Complexity;i++){int j=rng.Pick(i,7);int a=places[i];places[i]=places[j];places[j]=a;Initial[(places[i]/2)*4+places[i]%2]=1;}
            }
            if(kind==4)
            {
                int start=rng.Pick(1,3);
                int step=Math.Max(1,Math.Min(Complexity+1,(MathCurriculum.ChapterLimit(grade,mode,8)-start)/(Count-1)));
                for(int i=0;i<Count;i++)Initial[i]=start+i*step;
                PortraitOrder=Initial.Take(Count).ToArray();
                if(_portraitClues)
                    for(int i=0;i<Count-1;i++){int j=rng.Pick(i,Count-1);int a=PortraitOrder[i];PortraitOrder[i]=PortraitOrder[j];PortraitOrder[j]=a;}
                for(int i=0;i<Count-1;i++){int j=rng.Pick(i,Count-1);int a=Initial[i];Initial[i]=Initial[j];Initial[j]=a;}
                if(Initial.Take(Count).SequenceEqual(PortraitOrder)){int a=Initial[0];Initial[0]=Initial[Count-1];Initial[Count-1]=a;}
            }
            Melody=Enumerable.Range(0,3+Complexity+(grade>=2?1:0)).Select(i=>rng.Pick(0,2)).ToArray();
            Reset();
        }
        private static int Rotate(int mask)=>((mask<<1)&15)|((mask>>3)&1);
        public void Reset(){Array.Copy(Initial,Cells,16);_history.Clear();Step=0;Selected=-1;Solved=false;UpdateWater();}
        public void Undo()
        {
            if(Solved)return;
            if(_history.Count>0)Array.Copy(_history.Pop(),Cells,16);
            Step=0;Selected=-1;UpdateWater();
        }
        public void RestartMelody(){if(!Solved)Step=0;}
        public void Tap(int index)
        {
            if(Solved||index<0||index>=16)return;
            _history.Push((int[])Cells.Clone());
            switch(Kind)
            {
                case 0: Cells[index]=Rotate(Cells[index]);UpdateWater();Solved=Wet[3]&&Wet[15]&&(Cells[3]&2)!=0&&(Cells[15]&2)!=0;break;
                case 1: if(index>=Count||Remaining==0)break;Cells[index]++;Solved=Remaining==0&&Cells.Take(Count).SequenceEqual(BasketTargets);break;
                case 2: if(index>=12&&index<=14){Cells[_gap]=PatternChoices[index-12];Solved=Cells[_gap]==_patternAnswer;}break;
                case 3:
                    if(index%4<2)break;Cells[index]=1-Cells[index];
                    Solved=Enumerable.Range(0,4).All(y=>Cells[y*4]==Cells[y*4+3]&&Cells[y*4+1]==Cells[y*4+2]);break;
                case 4:
                    if(index>=Count)break;
                    if(Selected<0)Selected=index;else{int a=Cells[Selected];Cells[Selected]=Cells[index];Cells[index]=a;Selected=-1;}
                    Solved=Cells.Take(Count).SequenceEqual(PortraitOrder);break;
                case 5:
                    if(index>2)break;Step=index==Melody[Step]?Step+1:0;Solved=Step==Melody.Length;break;
            }
            if(_history.Count>100)_history.Clear();
        }
        private void UpdateWater()
        {
            Array.Clear(Wet,0,16);if(Kind!=0||(Cells[0]&8)==0)return;
            var queue=new Queue<int>();Wet[0]=true;queue.Enqueue(0);
            int[] delta={-4,1,4,-1};
            while(queue.Count>0)
            {
                int cell=queue.Dequeue();
                for(int d=0;d<4;d++)
                {
                    int next=cell+delta[d];if(next<0||next>=16||(d%2==1&&next/4!=cell/4)||Wet[next])continue;
                    if((Cells[cell]&(1<<d))==0||(Cells[next]&(1<<((d+2)%4)))==0)continue;
                    Wet[next]=true;queue.Enqueue(next);
                }
            }
        }
        public string[] Narration()
        {
            if(CoordinateMirror)
            {
                var lines=new List<string>{Instruction,"The mirror is halfway between x equals two and x equals three."};
                for(int y=0;y<4;y++)for(int x=0;x<2;x++)if(Initial[y*4+x]==1)
                {lines.Add("A guest is at x coordinate");lines.AddRange(LearningSpeech.Number(x+1));lines.Add("and y coordinate");lines.AddRange(LearningSpeech.Number(4-y));}
                lines.Add(Hint);return lines.ToArray();
            }
            if(NumberPattern)
            {
                var lines=new List<string>{Instruction};
                for(int i=0;i<8;i++)
                    if(i==_gap)lines.Add("what");else lines.AddRange(SpeakValue(PatternLabel(Initial[i]),Grade==4));
                lines.Add(Hint);return lines.ToArray();
            }
            if(FractionShares||WeightShares)
            {
                var lines=new List<string>{Instruction,"There are "+Total+" pumpkins and "+Count+" baskets."};
                if(WeightShares){lines.Add("Each pumpkin has a mass of");lines.AddRange(SpeakValue(Decimal(PumpkinWeight)));lines.Add("kilograms.");}
                for(int i=0;i<Count;i++)
                {
                    lines.Add("Basket");lines.AddRange(LearningSpeech.Number(i+1));lines.Add("needs");
                    lines.AddRange(SpeakValue(BasketTargetLabel(i).Replace(" kg",""),FractionShares));
                    lines.Add(FractionShares?"of the whole harvest.":"kilograms.");
                }
                lines.Add(Hint);return lines.ToArray();
            }
            if(_portraitClues)
            {
                var lines=new List<string>{Instruction};
                for(int i=0;i<Count-1;i++)
                {
                    lines.Add("The portrait with");lines.AddRange(LearningSpeech.Number(PortraitOrder[i]));
                    lines.Add("candles comes immediately before the portrait with");lines.AddRange(LearningSpeech.Number(PortraitOrder[i+1]));lines.Add("candles.");
                }
                return lines.ToArray();
            }
            return Kind==1?new[]{Instruction,"There are "+Total+" pumpkins and "+Count+" baskets.",Hint}:new[]{Instruction,Hint};
        }
        private static string Decimal(decimal value)=>value.ToString("0.##",CultureInfo.InvariantCulture);
        private static IEnumerable<string> SpeakValue(string value,bool fraction=false)=>LearningSpeech.Lines(new LearningQuestion
            {Prompt="",Display=value,Kind=fraction?LearningKind.Fractions:LearningKind.Arithmetic}).Where(s=>s.Length>0);
        public string PatternLabel(int value)=>Grade==4?value+"/"+PatternDenominator:
            Grade==5?Decimal(value/(decimal)PatternDenominator):value.ToString();
        public string BasketTargetLabel(int index)=>FractionShares?(Complexity==2&&index==1?"1/4 + 1/4":Count==2||index==1?"1/2":"1/4"):
            WeightShares?Decimal(BasketTargets[index]*PumpkinWeight)+" kg":"EQUAL SHARE";
        public string BasketContentsLabel(int index)=>WeightShares?Decimal(Cells[index]*PumpkinWeight)+" kg":Cells[index].ToString();
        public string HarvestLabel=>WeightShares?"EACH PUMPKIN: "+Decimal(PumpkinWeight)+" kg":FractionShares?"WHOLE HARVEST: "+Total+" PUMPKINS":"";
    }
}
