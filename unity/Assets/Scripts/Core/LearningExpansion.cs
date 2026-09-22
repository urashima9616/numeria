using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Numeria.Core
{
    public static partial class MathCurriculum
    {
        // Explicit grade inventories, not an enum-wide pool. Theme-only operations are
        // supported separately so a double/subtraction move keeps its mathematical identity.
        private static LearningKind[] GradePool(int g)
        {
            var kinds = new List<LearningKind> { LearningKind.Arithmetic, LearningKind.MissingNumber,
                LearningKind.Comparison, LearningKind.Pattern, LearningKind.Geometry, LearningKind.PlaceValue,
                LearningKind.Measurement, LearningKind.Data, LearningKind.Bonds, LearningKind.Balance,
                LearningKind.NumberPath, LearningKind.VisualPattern, LearningKind.Symmetry, LearningKind.WordProblem };
            if (g <= 1) kinds.AddRange(new[] { LearningKind.Counting, LearningKind.Sorting, LearningKind.ShapeCompose });
            if (g >= 1) kinds.AddRange(new[] { LearningKind.Time, LearningKind.Fractions });
            if (g >= 2) kinds.AddRange(new[] { LearningKind.EqualGroups, LearningKind.Money });
            if (g >= 3) kinds.AddRange(new[] { LearningKind.Rounding, LearningKind.FractionLine });
            if (g >= 4) kinds.AddRange(new[] { LearningKind.Units, LearningKind.Factors });
            if (g == 5) kinds.AddRange(new[] { LearningKind.Coordinates, LearningKind.DecimalOps });
            return kinds.ToArray();
        }
        public static bool Supports(int grade, LearningKind kind) => Available(grade).Contains(kind) ||
            kind == LearningKind.Double || kind == LearningKind.Subtraction || kind == LearningKind.ChainSum;
        private static void Shuffle<T>(IList<T> values, Rng rng)
        { for (int i = values.Count-1; i > 0; i--) { int j=rng.Pick(0,i); T v=values[i]; values[i]=values[j]; values[j]=v; } }
        private static void Choices(LearningQuestion q, Rng rng, string answer, IEnumerable<string> distractors)
        {
            q.Answer=answer;
            var options=distractors.Where(x=>x!=answer).Distinct().Take(3).ToList(); options.Add(answer); Shuffle(options,rng);
            q.Options=options.ToArray(); q.CorrectIndex=options.IndexOf(answer);
        }
        private static void NumberChoices(LearningQuestion q, Rng rng, decimal answer, decimal step=1, decimal max=decimal.MaxValue)
        {
            var wrong=new List<string>();
            foreach(int delta in new[]{-1,1,-2,2,-3,3,4})
                if(answer+delta*step>=0 && answer+delta*step<=max) wrong.Add(S(answer+delta*step));
            Choices(q,rng,S(answer),wrong);
        }
        private static int Gcd(int a,int b) { while(b!=0) { int r=a%b; a=b; b=r; } return Math.Max(1,Math.Abs(a)); }
        public static string Fraction(int n,int d) { int gcd=Gcd(n,d); return d/gcd==1 ? (n/gcd).ToString() : $"{n/gcd}/{d/gcd}"; }
        private static void FractionChoices(LearningQuestion q,Rng rng,int n,int d)
        {
            var wrong=new List<string>();
            foreach(int delta in new[]{-1,1,2,3,-2}) if(n+delta>=0) wrong.Add(Fraction(n+delta,d));
            Choices(q,rng,Fraction(n,d),wrong);
        }
        private static int Span(int low,int high,int chapter,int difficulty) => ChapterSteps(low,
            difficulty==0 ? Math.Max(low,(low+high)/2) : high, chapter);
        private static readonly string[] Shapes={"circle","triangle","square","rectangle","hexagon"};

        private static bool Expand(LearningQuestion q,Rng rng,int chapter,int cap,int facts)
        {
            int g=q.Grade, d=q.Difficulty==MathMode.FeelGood?0:q.Difficulty==MathMode.Challenging?2:1;
            int v=d==2?rng.Pick(1,2):rng.Pick(0,1), small=Math.Min(cap,Span(4,g<2?10:20,chapter,d));
            q.Template=q.Kind+"-"+v;
            void Num(decimal answer,decimal step=1,decimal max=decimal.MaxValue)=>NumberChoices(q,rng,answer,step,max);
            void Pick(string answer,params string[] wrong)=>Choices(q,rng,answer,wrong);
            switch(q.Kind)
            {
                case LearningKind.Double:
                case LearningKind.Subtraction:
                case LearningKind.ChainSum:
                {
                    int a=rng.Pick(0,cap/2), b=rng.Pick(0,cap-a);
                    q.Prompt="Complete the spell."; q.Hint="Use the numbers in the spell. Check each step.";
                    if(q.Kind==LearningKind.Double) { q.Display=$"{a} + {a} = ?"; Num(2*a,1,cap); }
                    else if(q.Kind==LearningKind.Subtraction) { q.Display=$"{a+b} - {b} = ?"; Num(a,1,cap); }
                    else { int c=rng.Pick(0,cap-a-b); q.Display=$"{a} + {b} + {c} = ?"; Num(a+b+c,1,cap); }
                    break;
                }
                case LearningKind.Counting:
                {
                    int a=rng.Pick(0,small), b=rng.Pick(0,small-a);
                    q.Diagram="dots"; q.Values=new[]{a,b}; q.HideValues=true;
                    q.Prompt=v==0?"How many crystals are there?":"How many gold crystals are there?";
                    q.Display="COUNT THE CRYSTALS"; q.Hint="Touch each crystal once as you count.";
                    Num(v==0?a+b:b,1,cap); break;
                }
                case LearningKind.Bonds:
                {
                    int target=Math.Min(g==0?10:g==1?20:cap,small);
                    if(g>=2) target=rng.Pick(2,cap);
                    int a=rng.Pick(0,target), b=target-a;
                    q.Prompt="Choose two crystals to make the target."; q.Display=$"TARGET {target}";
                    q.Hint="Pick one part, then count what is still needed.";
                    // Four distinct tile identities. Multiple valid pairs are intentionally accepted.
                    q.Tokens=new[]{a,b,rng.Pick(0,target),rng.Pick(0,target)}.Select(x=>x.ToString()).ToArray(); Shuffle(q.Tokens,rng);
                    q.Interaction=LearningInteraction.Pair; q.Values=new[]{target}; q.Expected=new[]{a,b};
                    Num(target,1,cap); break;
                }
                case LearningKind.Balance:
                {
                    int total=rng.Pick(2,cap), a=rng.Pick(0,total), b=rng.Pick(0,total);
                    q.Prompt="Balance both sides of the scale."; q.Hint="Both sides must have the same total.";
                    q.Diagram="balance"; q.Values=new[]{a,total-a,b};
                    q.Display=v==0?$"{a} + {total-a} = {b} + ?":$"{total} - {a} = ? + {rng.Pick(0,total-a)}";
                    if(v==0) Num(total-b,1,cap);
                    else { int c=int.Parse(q.Display.Substring(q.Display.LastIndexOf(' ')+1)); Num(total-a-c,1,cap); }
                    break;
                }
                case LearningKind.NumberPath:
                {
                    int step=g==0?1:rng.Pick(1,Math.Max(1,Math.Min(small/4,facts)));
                    int start=rng.Pick(0,Math.Max(0,cap-3*step));
                    var path=Enumerable.Range(0,4).Select(i=>start+i*step).ToArray();
                    bool descending=v==2; if(descending) Array.Reverse(path);
                    q.Prompt=descending?"Build the path from greatest to least.":"Build the path from least to greatest.";
                    q.Display="TAP THE STONES IN ORDER"; q.Hint=descending?"Start with the greatest number.":"Start with the smallest number.";
                    q.Interaction=LearningInteraction.Order; q.Expected=path; q.Tokens=path.Select(x=>x.ToString()).ToArray(); Shuffle(q.Tokens,rng);
                    Num(path[0],1,cap); break;
                }
                case LearningKind.VisualPattern:
                {
                    int period=Span(2,g<2?3:4,chapter,d), length=period*2;
                    var unit=Enumerable.Range(0,period).Select(_=>rng.Pick(0,4)).ToArray();
                    if(unit.Distinct().Count()==1) unit[1]=(unit[0]+1)%5;
                    var sequence=Enumerable.Range(0,length).Select(i=>unit[i%period]).ToArray();
                    int missing=v==2?period:length-1, answer=sequence[missing]; sequence[missing]=-1;
                    q.Diagram="sequence"; q.Values=sequence; q.Prompt="Which shape completes the repeating pattern?";
                    q.Display="FIND THE REPEATING UNIT"; q.Hint="Find the small group that repeats.";
                    Choices(q,rng,Shapes[answer],Shapes); break;
                }
                case LearningKind.Symmetry:
                {
                    int length=Span(2,g<2?3:4,chapter,d); var pattern=Enumerable.Range(0,length).Select(_=>rng.Pick(0,4)).ToArray();
                    int missing=rng.Pick(0,length-1); q.Values=pattern; q.Diagram="mirror";
                    q.Labels=new[]{missing.ToString()}; q.Prompt="Complete the mirror image."; q.Display="REFLECT ACROSS THE DOTTED LINE";
                    q.Hint="Match shapes the same distance from the mirror."; Choices(q,rng,Shapes[pattern[length-1-missing]],Shapes); break;
                }
                case LearningKind.Sorting:
                {
                    int count=rng.Pick(3,Math.Min(small,10)); var shapes=Enumerable.Range(0,count).Select(_=>rng.Pick(0,2)).ToArray();
                    int target=rng.Pick(0,2); q.Diagram="collection"; q.Values=shapes;
                    q.Prompt="Count only the matching shapes."; q.Display=$"FIND EVERY {Shapes[target].ToUpperInvariant()}";
                    q.Hint="Ignore other shapes. Count the matching group."; Num(shapes.Count(x=>x==target),1,cap); break;
                }
                case LearningKind.ShapeCompose:
                {
                    q.Diagram="compose"; q.Values=new[]{v}; q.Prompt="What shape do these pieces make together?";
                    q.Display="JOIN THE PIECES ALONG THE DOTTED EDGE"; q.Hint="Look at the outside edge of the joined pieces.";
                    Pick(v==0?"square":"rectangle","triangle","circle","hexagon",v==0?"rectangle":"square"); break;
                }
                case LearningKind.WordProblem:
                {
                    int total=rng.Pick(2,cap), a=rng.Pick(0,total), b=total-a;
                    q.Prompt="Solve the adventure story."; q.Hint="Decide what joins, leaves, or is missing. Then choose an operation.";
                    if(g>=3 && v==2)
                    {
                        a=rng.Pick(2,facts);b=rng.Pick(2,facts);int used=rng.Pick(1,a*b);
                        q.Display=$"Lucas finds {a} bags with {b} gems in each.\nHe uses {used} gems. How many remain?"; Num(a*b-used);
                    }
                    else if(v==0) { q.Display=$"Lucas has {a} gems. He finds {b} more.\nHow many gems does he have now?"; Num(total,1,cap); }
                    else if(v==1) { q.Display=$"Lucas has {total} gems. He gives away {b}.\nHow many gems remain?"; Num(a,1,cap); }
                    else { q.Display=$"Lucas needs {total} gems. He has {a}.\nHow many more does he need?"; Num(b,1,cap); }
                    break;
                }
                case LearningKind.EqualGroups:
                {
                    int rows=rng.Pick(2,Math.Min(5,facts)),cols=rng.Pick(2,Math.Min(5,facts));
                    q.Diagram="array"; q.Values=new[]{rows,cols}; q.HideValues=true;
                    q.Hint="Each row is an equal group. Count groups and items in each group.";
                    if(g==2 || v==0) { q.Prompt=g==2&&v==1?"How many crystals are in each row?":"How many crystals are in this array?"; q.Display="EQUAL ROWS";Num(g==2&&v==1?cols:rows*cols); }
                    else if(v==1) { q.Prompt="Share the crystals equally.";q.Display=$"{rows*cols} crystals shared by {rows} friends.\nHow many does each friend get?";q.Diagram=null;Num(cols); }
                    else if(g==3) {q.Prompt="How many full bags can Lucas make?";q.Display=$"{rows*cols} gems. Each bag holds {cols}.";q.Diagram=null;Num(rows);}
                    else { q.Prompt="How many full bags and how many left over?";int remainder=rng.Pick(1,cols-1);q.Diagram=null;q.Display=$"Pack {rows*cols+remainder} gems.\nEach bag holds {cols}.";Pick($"{rows} bags, {remainder} left",$"{rows+1} bags, {remainder} left",$"{rows} bags, 0 left",$"{rows-1} bags, {remainder} left"); }
                    break;
                }
                case LearningKind.Money:
                {
                    int nickels=rng.Pick(1,Span(2,6,chapter,d)),dimes=rng.Pick(1,Span(2,6,chapter,d)),total=5*nickels+10*dimes;
                    q.Prompt=v==0?"Count the value of the coins.":"How much change should Lucas receive?";
                    q.Hint="A nickel is five cents. A dime is ten cents.";
                    if(v==0) { q.Display=$"{nickels} nickels and {dimes} dimes\nHow many cents?"; Num(total); }
                    else { int cost=rng.Pick(1,total); q.Display=$"Lucas pays {total} cents for an item costing {cost} cents.\nChange in cents?";Num(total-cost); }
                    break;
                }
                case LearningKind.Rounding:
                {
                    int unit=g==3?10:g==4?100:1000,n=rng.Pick(1,Span(unit*2,unit*9,chapter,d));
                    q.Prompt="Round to the nearest marked place.";q.Display=$"Round {n} to the nearest {unit}.";
                    q.Hint="Find the two neighboring multiples. Halfway rounds up."; Num(((n+unit/2)/unit)*unit,unit);break;
                }
                case LearningKind.FractionLine:
                {
                    var denominators=(g==3?new[]{2,3,4,6,8}:new[]{2,3,4,5,6,7,8}).Where(x=>x<=Math.Max(3,facts)).ToArray();
                    int den=denominators[rng.Pick(0,denominators.Length-1)],n=rng.Pick(1,den-1);
                    q.Diagram="fractionline";q.Values=new[]{n,den};q.Prompt="What fraction is marked on the number line?";
                    q.Display="FROM ZERO TO ONE";q.Hint="Count equal spaces from zero, not tick marks.";FractionChoices(q,rng,n,den);break;
                }
                case LearningKind.Units:
                {
                    int factor=v==0?100:v==1?1000:60,n=rng.Pick(1,Span(2,9,chapter,d));
                    string big=v==0?"meters":v==1?"kilograms":"hours",little=v==0?"centimeters":v==1?"grams":"minutes";
                    bool reverse=d==2; q.Prompt="Convert the measurement.";
                    q.Display=reverse?$"{n*factor} {little} = ? {big}":$"{n} {big} = ? {little}";
                    q.Hint=v==0?"One meter is one hundred centimeters.":v==1?"One kilogram is one thousand grams.":"One hour is sixty minutes.";
                    Num(reverse?n:n*factor,reverse?1:factor);break;
                }
                case LearningKind.Factors:
                {
                    int a=rng.Pick(2,facts),b=rng.Pick(2,facts),n=a*b;
                    q.Prompt=v==0?"Which number is a factor?":"Which number is a multiple?";
                    q.Display=v==0?$"FACTOR OF {n}":$"MULTIPLE OF {a}";q.Hint="A factor divides exactly. A multiple is made by multiplying.";
                    if(v==0) Choices(q,rng,a.ToString(),Enumerable.Range(2,30).Where(x=>n%x!=0).Select(x=>x.ToString()));
                    else Choices(q,rng,n.ToString(),Enumerable.Range(Math.Max(1,n-5),15).Where(x=>x%a!=0).Select(x=>x.ToString()));break;
                }
                case LearningKind.Coordinates:
                {
                    int x=rng.Pick(1,Span(2,6,chapter,d)),y=rng.Pick(1,Span(2,6,chapter,d));q.Diagram="coordinate";q.Values=new[]{x,y};
                    q.Prompt=v==0?"Read the coordinates of the crystal.":"Where is the crystal after the move?";
                    q.Hint="Read across first, then up. Coordinates are written x, then y.";
                    int dx=v==0?0:rng.Pick(1,2);q.Display=v==0?"READ THE GRID":$"Move {dx} spaces right. Keep the same height.";
                    Pick($"({x+dx}, {y})",$"({x+dx+1}, {y})",$"({x+dx}, {y+1})",$"({x+dx-1}, {y})");break;
                }
                case LearningKind.DecimalOps:
                {
                    decimal a=rng.Pick(1,cap)/100m;int b=rng.Pick(2,facts);
                    q.Prompt="Solve the decimal spell.";q.Hint="Think in hundredths. Keep the place values aligned.";
                    if(v==0) { q.Display=$"{S(a)} x {b} = ?";Num(a*b,.01m); }
                    else if(v==1) { q.Display=$"{S(a*b)} / {b} = ?";Num(a,.01m); }
                    else { decimal c=rng.Pick(1,9)/10m,e=rng.Pick(1,9)/10m;q.Display=$"{S(c)} x {S(e)} = ?";Num(c*e,.01m); }break;
                }
                case LearningKind.Geometry:
                {
                    if(g<3)
                    {
                        int shape=rng.Pick(0,g==0?3:4);q.Diagram="shape";q.Values=new[]{shape};
                        q.Prompt=v==0?"Name this shape.":"How many straight sides does this shape have?";
                        q.Display="LOOK AT THE SHAPE";q.Hint="Look at the straight sides and corners.";
                        if(v==0) Choices(q,rng,Shapes[shape],Shapes);else Num(new[]{0,3,4,4,6}[shape]);
                    }
                    else
                    {
                        int a=rng.Pick(2,facts),b=rng.Pick(2,facts);q.Diagram="rectangle";q.Values=new[]{a,b};
                        q.Prompt=v==0?"Find the perimeter of the rectangle.":v==1?"Find the area of the rectangle.":"Find the missing width of the rectangle.";
                        q.Hint=v==1?"Area counts square units. Multiply length by width.":"Perimeter counts the outside edge. Add all four sides.";
                        if(v<2) {q.Display=$"Length {a} cm    Width {b} cm";Num(v==0?2*(a+b):a*b);}
                        else {q.Display=$"Perimeter {2*(a+b)} cm    Length {a} cm\nWidth ? cm";Num(b);}
                    }break;
                }
                case LearningKind.PlaceValue:
                {
                    if(g==0) return false; // Keep the exact teen-number ten-frame model.
                    int places=Span(2,g==1?2:g==2?3:g==3?3:5,chapter,d),unit=(int)Math.Pow(10,rng.Pick(0,places-1));
                    int digit=rng.Pick(1,9),tail=rng.Pick(0,unit-1),n=digit*unit+tail;
                    q.Hint="Break the number into the value of each place.";
                    if(g==5 && v==2)
                    { q.Prompt="What is the value of the indicated digit?";q.Display=$"{S(n/100m)}\nDigit {digit} in the {new[]{"hundredths","tenths","ones","tens","hundreds"}[(int)Math.Log10(unit)]} place";Num(digit*unit/100m,unit/100m); }
                    else if(v==0) {q.Prompt="What is the value of the indicated digit?";q.Display=$"{n}\nDigit {digit} in the {new[]{"ones","tens","hundreds","thousands","ten-thousands"}[(int)Math.Log10(unit)]} place";Num(digit*unit,unit);}
                    else if(v==1) {q.Prompt="Build the number from its parts.";q.Display=$"{digit} groups of {unit} and {tail} ones = ?";Num(n,unit);}
                    else {q.Prompt="Find the missing part of the number.";q.Display=$"{n} = {digit*unit} + ?";Num(tail);}
                    break;
                }
                case LearningKind.Time:
                {
                    int step=g==1?(d==0||chapter<5?60:30):g==2?5:1;
                    int h=rng.Pick(1,12),m=rng.Pick(0,60/step-1)*step; q.Diagram="clock";q.Values=new[]{h,m};
                    int elapsed=g>=3&&v>0?rng.Pick(1,Span(10,60,chapter,d)):0;
                    if(v==2) elapsed=-elapsed;
                    string Time(int t) {t=(t%720+720)%720;return $"{(t/60==0?12:t/60)}:{t%60:00}";}
                    q.Prompt=elapsed==0?"What time does the clock show?":elapsed>0?"What time will it be later?":"What time was it earlier?";
                    q.Display=elapsed==0?"READ THE CLOCK":$"{Math.Abs(elapsed)} MINUTES {(elapsed>0?"LATER":"EARLIER")}";
                    q.Hint="Read the short hand for hours and the long hand for minutes.";
                    int answer=h*60+m+elapsed;
                    Pick(Time(answer),Time(answer+step),Time(answer-step),Time(answer+step*2));break;
                }
                case LearningKind.Fractions:
                {
                    var denominators=(g==1?new[]{2,4}:g==2?new[]{2,3,4}:g==3?new[]{2,3,4,6,8}:new[]{2,3,4,5,6,7,8,9,10})
                        .Where(x=>g<=2||x<=Math.Max(3,facts)).ToArray();
                    int den=denominators[rng.Pick(0,denominators.Length-1)],n=rng.Pick(1,den-1);
                    q.Hint="Use equal-sized parts of the same whole.";
                    if(g<=2 || d==0)
                    {
                        q.Diagram="fraction";q.Values=new[]{n,den};q.Prompt=v==0?"How many equal parts make this whole?":"What fraction is shaded?";
                        q.Display="EQUAL PARTS OF ONE WHOLE";
                        if(v==0) Num(den);
                        else if(g<=2) Choices(q,rng,Fraction(n,den),g==1?new[]{"0","1/4","1/2","3/4","1"}:new[]{"0","1/3","2/3","1/4","1/2","3/4","1"});
                        else FractionChoices(q,rng,n,den);
                    }
                    else if(g==3)
                    {
                        q.Prompt=v==0?"Find an equivalent fraction.":"Compare the fractions.";
                        if(v==0) {den=rng.Pick(2,Math.Min(4,Math.Max(2,facts/2)));n=rng.Pick(1,den-1);q.Display=$"{n}/{den} = ?/{den*2}";Num(n*2);}
                        else {int b=rng.Pick(1,den-1);q.Display=$"{n}/{den} ? {b}/{den}";Pick(n<b?"<":n>b?">":"=","<",">","=");}
                    }
                    else if(g==4)
                    {
                        int b=rng.Pick(1,den),a=n;
                        q.Prompt=v==0?"Add the fractions.":v==1?"Subtract the fractions.":"Multiply the fraction by a whole number.";
                        if(v==0) {q.Display=$"{a}/{den} + {b}/{den} = ?";FractionChoices(q,rng,a+b,den);}
                        else if(v==1) {q.Display=$"{Math.Max(a,b)}/{den} - {Math.Min(a,b)}/{den} = ?";FractionChoices(q,rng,Math.Abs(a-b),den);}
                        else {q.Display=$"{a}/{den} x {b} = ?";FractionChoices(q,rng,a*b,den);}
                    }
                    else
                    {
                        int other=rng.Pick(2,Math.Min(9,facts)),b=rng.Pick(1,other-1),op=rng.Pick(0,d==2?4:3);
                        q.Template="Fractions-5-"+op;
                        if(op==0) {q.Prompt="Add the fractions.";q.Display=$"{n}/{den} + {b}/{other} = ?";FractionChoices(q,rng,n*other+b*den,den*other);}
                        else if(op==1) {q.Prompt="Subtract the fractions.";bool swap=n*other<b*den;q.Display=swap?$"{b}/{other} - {n}/{den} = ?":$"{n}/{den} - {b}/{other} = ?";FractionChoices(q,rng,Math.Abs(n*other-b*den),den*other);}
                        else if(op==2) {q.Prompt="Multiply the fractions.";q.Display=$"{n}/{den} x {b}/{other} = ?";FractionChoices(q,rng,n*b,den*other);}
                        else {q.Prompt="Divide using equal parts.";q.Display=op==3?$"(1/{den}) / {other} = ?":$"{other} / (1/{den}) = ?";FractionChoices(q,rng,op==3?1:other*den,op==3?den*other:1);}
                    }break;
                }
                case LearningKind.Measurement:
                {
                    if(g<3) return false;
                    int a=rng.Pick(2,facts),b=rng.Pick(2,facts),c=rng.Pick(2,Span(3,8,chapter,d));
                    q.Diagram=g==5?"box":"rectangle";q.Values=new[]{a,b,c};
                    q.Prompt=g==5?"Find the volume in cubic units.":"Find the area in square units.";
                    q.Display=g==5?$"Length {a}   Width {b}   Height {c}":$"Length {a}   Width {b}";
                    q.Hint=g==5?"Volume counts unit cubes. Multiply length, width, and height.":"Area counts square units. Multiply length by width.";
                    if(v==2) {q.Prompt="Find the missing measurement.";q.Display=g==5?$"Volume {a*b*c}   Length {a}   Width {b}\nHeight ?":$"Area {a*b}   Length {a}\nWidth ?";Num(g==5?c:b);}else Num(g==5?a*b*c:a*b);break;
                }
                case LearningKind.Data:
                {
                    int max=Span(2,g==0?4:8,chapter,d),scale=g>=3?Span(1,g>=4?10:5,chapter,d):1;
                    int a=rng.Pick(1,max),b=rng.Pick(1,max),c=rng.Pick(1,max);
                    if(g<=2) {max=Math.Max(1,Math.Min(max,cap/3));a=rng.Pick(1,max);b=rng.Pick(1,max);c=rng.Pick(1,max);}
                    q.Diagram="bars";q.Values=g==0?new[]{a,b}:new[]{a,b,c};q.Labels=new[]{"RED","BLUE","GOLD"};q.HideValues=true;
                    q.Prompt=v==0?"How many voted for RED?":v==1?"How many more votes does the larger group have?":"How many votes are there altogether?";
                    q.Display=$"EACH BLOCK IS {scale} {(scale==1?"VOTE":"VOTES")}";q.Hint="Count blocks, then use the value of each block.";
                    Num((v==0?a:v==1?Math.Abs(a-b):a+b+(g==0?0:c))*scale,scale,g<=2?cap:decimal.MaxValue);
                    if(v==1)q.Display+="\nCOMPARE RED AND BLUE";break;
                }
                default:return false;
            }
            return true;
        }

        public static bool CheckSelection(LearningQuestion q,IList<int> indices)
        {
            if(q.Tokens==null || indices.Distinct().Count()!=indices.Count || indices.Any(i=>i<0||i>=q.Tokens.Length))return false;
            if(q.Interaction==LearningInteraction.Pair) return indices.Count==2 && indices.Sum(i=>int.Parse(q.Tokens[i]))==q.Values[0];
            if(q.Interaction==LearningInteraction.Order) return indices.Select(i=>int.Parse(q.Tokens[i])).SequenceEqual(q.Expected);
            return false;
        }
    }

    /// <summary>In-memory, per-adventure shuffle bag. Does not mutate or serialize progress.</summary>
    public sealed class LearningSession
    {
        private static readonly ConditionalWeakTable<MathSettings,LearningSession> Sessions=new ConditionalWeakTable<MathSettings,LearningSession>();
        public static LearningSession For(MathSettings settings)=>Sessions.GetValue(settings,_=>new LearningSession());
        private readonly List<LearningKind> _bag=new List<LearningKind>();
        private readonly Queue<LearningKind> _recentKinds=new Queue<LearningKind>();
        private readonly Queue<string> _recentQuestions=new Queue<string>();
        private readonly Dictionary<LearningKind,string> _lastTemplate=new Dictionary<LearningKind,string>();
        private int _grade=-1;
        public LearningQuestion Next(MathSettings settings,Rng rng,int chapter,LearningKind? requested=null)
        {
            if(_grade!=settings.Grade) {_grade=settings.Grade;_bag.Clear();_recentKinds.Clear();_lastTemplate.Clear();_recentQuestions.Clear();}
            var kinds=MathCurriculum.Available(settings.Grade);
            LearningKind kind;
            if(requested.HasValue && MathCurriculum.Supports(settings.Grade,requested.Value))kind=requested.Value;
            else
            {
                if(_bag.Count==0)_bag.AddRange(kinds);
                var choices=_bag.Where(k=>!_recentKinds.Contains(k)).ToArray();
                if(choices.Length==0)choices=_bag.ToArray();
                kind=choices[rng.Pick(0,choices.Length-1)];_bag.Remove(kind);
            }
            LearningQuestion q=null;
            // Resolve Random once for a question: retrying a duplicate must not change its chosen mode.
            var resolved=new MathSettings {Grade=settings.Grade};
            resolved.Mode=settings.Mode==MathMode.Random?(MathMode)rng.Pick(0,2):settings.Mode;
            for(int tries=0;tries<24;tries++)
            {
                q=MathCurriculum.Generate(resolved,rng,kind,chapter);
                string key=q.Prompt+q.Display+string.Join(",",q.Values??Array.Empty<int>());
                if(!_recentQuestions.Contains(key) && (!_lastTemplate.TryGetValue(kind,out var last)||last!=q.Template||tries>=12))break;
            }
            _lastTemplate[kind]=q.Template;_recentKinds.Enqueue(kind);if(_recentKinds.Count>3)_recentKinds.Dequeue();
            _recentQuestions.Enqueue(q.Prompt+q.Display+string.Join(",",q.Values??Array.Empty<int>()));if(_recentQuestions.Count>8)_recentQuestions.Dequeue();
            return q;
        }
    }
}
