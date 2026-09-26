using System;
using System.Linq;

namespace Numeria.Core
{
    /// <summary>Small, untimed environmental challenges. All states can be reset without a cost.</summary>
    public sealed class DepthMechanism
    {
        public readonly int Kind, Stage;
        public readonly int[] Cells = new int[9];
        public readonly int[] Goal = new int[9];
        public int Step, Position;
        private int _selected = -1;
        public bool Solved { get; private set; }
        public string Instruction => new[] {
            "Turn each channel to match the water-route arrows below.",
            "Read the mountain echo. Tap the numbered bells in that order.",
            "Guide the wind from START to the garden. Avoid the dark storm cells.",
            "Light every sun seal. Touching one also changes its neighbors.",
            "Power every square once, ending at 9. Follow adjacent squares only.",
            "Share the cooling water equally. Tap a tank to pour one drop into the next.",
            "Restore the mosaic to 1 through 9. Tap two tiles to swap them.",
            "Make the lake reflections match. Tap the right-hand moon tiles."
        }[Kind];
        public DepthMechanism(int kind, int stage)
        {
            Kind=kind;Stage=stage;Reset();
        }
        public void Reset()
        {
            Array.Clear(Cells,0,9);Array.Clear(Goal,0,9);Step=0;Position=0;_selected=-1;Solved=false;
            for(int i=0;i<9;i++)Goal[i]=(i+Stage)%4;
            if(Kind==0)for(int i=0;i<3;i++)Cells[i]=(Goal[i]+1+i)%4;
            if(Kind==1)for(int i=0;i<4+Stage;i++)Goal[i]=(i*2+Stage+i/3)%3;
            if(Kind==2){Cells[1+Stage]=1;Cells[4]=1;Cells[8]=0;Cells[0]=0;}
            if(Kind==3){for(int i=0;i<9;i++)Cells[i]=1;ToggleCross(0);ToggleCross(4);ToggleCross(8-Stage);}
            if(Kind==4){Cells[0]=1;Position=0;Goal[0]=0;Goal[1]=3;Goal[2]=6;Goal[3]=7;Goal[4]=8;}
            if(Kind==5){Cells[0]=4+Stage;Cells[1]=2+Stage;Cells[2]=Stage;}
            if(Kind==6){for(int i=0;i<9;i++)Cells[i]=i+1;Swap(0,5);Swap(2,7);Swap(3,8-Stage);}
            if(Kind==7)for(int i=0;i<3;i++)Cells[i*3]=((i+Stage)%2)+1;
        }
        private void Swap(int a,int b){int t=Cells[a];Cells[a]=Cells[b];Cells[b]=t;}
        private void ToggleCross(int index)
        {
            for(int i=0;i<9;i++)if(Math.Abs(i%3-index%3)+Math.Abs(i/3-index/3)<=1)Cells[i]=1-Cells[i];
        }
        public void Tap(int index)
        {
            if(Solved||index<0||index>=9)return;
            switch(Kind)
            {
                case 0: if(index<3)Cells[index]=(Cells[index]+1)%4;Solved=Enumerable.Range(0,3).All(i=>Cells[i]==Goal[i]);break;
                case 1:
                    if(index>2)return;
                    Step=index==Goal[Step]?Step+1:0;Solved=Step==4+Stage;break;
                case 2:
                    if(Cells[index]!=0||Math.Abs(index%3-Position%3)+Math.Abs(index/3-Position/3)!=1)return;
                    Position=index;Solved=Position==8;break;
                case 3:ToggleCross(index);Solved=Cells.All(v=>v==1);break;
                case 4:
                    if(index==0){Array.Clear(Cells,0,9);Cells[0]=1;Position=0;Step=0;break;}
                    if(Cells[index]!=0||Math.Abs(index%3-Position%3)+Math.Abs(index/3-Position/3)!=1)return;
                    Cells[index]=1;Position=index;Step++;Solved=index==8&&Cells.All(v=>v==1);break;
                case 5:
                    if(index>=3||Cells[index]==0)return;
                    Cells[index]--;Cells[(index+1)%3]++;Solved=Cells[0]==Cells[1]&&Cells[1]==Cells[2];break;
                case 6:
                    if(_selected<0)_selected=index;else{Swap(_selected,index);_selected=-1;}
                    Solved=Enumerable.Range(0,9).All(i=>Cells[i]==i+1);break;
                case 7:
                    if(index%3!=2)return;Cells[index]=(Cells[index]+1)%3;
                    Solved=Enumerable.Range(0,3).All(i=>Cells[i*3]==Cells[i*3+2]);break;
            }
        }
        public string Label(int i)
        {
            string[] arrows={"UP","RIGHT","DOWN","LEFT"};
            switch(Kind)
            {
                case 0:return i<3?arrows[Cells[i]]:"";
                case 1:return i<3?"BELL "+(i+1):"";
                case 2:return i==Position?"WIND":i==8?"GARDEN":Cells[i]==1?"STORM":"o";
                case 3:return Cells[i]==1?"SUN":"MOON";
                case 4:return i==Position?"POWER":Cells[i]==1?"--":(i+1).ToString();
                case 5:return i<3?"TANK "+(i+1)+"\n"+Cells[i]+" DROPS":"";
                case 6:return (_selected==i?"[ ":"")+Cells[i]+(_selected==i?" ]":"");
                default:return i%3==1?"|":Cells[i]==0?"NEW":Cells[i]==1?"HALF":"FULL";
            }
        }
        public string Hint => Kind==0?"ROUTE: "+string.Join(" / ",Goal.Take(3).Select(i=>new[]{"UP","RIGHT","DOWN","LEFT"}[i])):
            Kind==1?"ECHO: "+string.Join(" - ",Goal.Take(4+Stage).Select(i=>(i+1).ToString()))+"   "+Step+" / "+(4+Stage):
            Kind==5?"Equal tanks: "+(2+Stage)+" each":"Take your time. RESET is always free.";

        // Read visible task information, not a computed solution or the player's last feedback.
        public string[] Narration()
        {
            var lines=new System.Collections.Generic.List<string>{Instruction};
            if(Kind==0)
            {
                lines.Add("Tap a channel to turn it. From left to right, match these directions.");
                lines.AddRange(Goal.Take(3).Select(i=>new[]{"up","right","down","left"}[i]));
            }
            if(Kind==1)
            {lines.Add("Tap the bells in this order.");foreach(int bell in Goal.Take(4+Stage))lines.AddRange(LearningSpeech.Number(bell+1));}
            if(Kind==4)lines.Add("Tap the starting square to start again.");
            if(Kind==5)
            {lines.Add("Tank three pours into tank one. Make each tank hold this many drops.");lines.AddRange(LearningSpeech.Number(2+Stage));}
            return lines.ToArray();
        }
    }
}
