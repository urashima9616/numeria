using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>Exact, state-driven teaching symbols; never bake puzzle answers into artwork.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DepthMechanismGraphic : MaskableGraphic
    {
        public int Kind,Value;
        public bool Active;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Color ink=Ui.Hex("#304e49"),gold=Ui.Hex("#eab847"),blue=Ui.Hex("#569cc1"),pale=Ui.Hex("#e9dfbf");
            float scale=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)/80f;
            void Poly(Color color,params Vector2[] points)
            {
                int first=mesh.currentVertCount;
                foreach(var p in points)mesh.AddVert(p*scale,color,Vector2.zero);
                for(int i=1;i<points.Length-1;i++)mesh.AddTriangle(first,first+i,first+i+1);
            }
            void Box(float x,float y,float w,float h,Color c)=>Poly(c,new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h));
            void Disk(float x,float y,float radius,Color c)
            {
                var points=new Vector2[32];for(int i=0;i<32;i++){float a=i*Mathf.PI/16;points[i]=new Vector2(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius);}Poly(c,points);
            }
            void Line(Vector2 a,Vector2 b,float width,Color c)
            {var n=new Vector2(a.y-b.y,b.x-a.x).normalized*width/2;Poly(c,a-n,a+n,b+n,b-n);}
            void Arrow(int direction,Color c)
            {
                Vector2 Rotate(float x,float y){float a=-direction*Mathf.PI/2;return new Vector2(x*Mathf.Cos(a)-y*Mathf.Sin(a),x*Mathf.Sin(a)+y*Mathf.Cos(a));}
                Poly(c,Rotate(-7,-31),Rotate(7,-31),Rotate(7,8),Rotate(23,8),Rotate(0,33),Rotate(-23,8),Rotate(-7,8));
            }
            void Moon(int phase)
            {
                Disk(0,0,32,ink);Disk(0,0,27,phase==2?gold:pale);
                if(phase==0)Disk(0,0,23,ink);
                if(phase==1)
                {
                    var half=new Vector2[18];half[0]=Vector2.zero;
                    for(int i=0;i<=16;i++){float a=Mathf.PI/2+i*Mathf.PI/16;half[i+1]=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*27;}Poly(ink,half);
                }
            }
            switch(Kind)
            {
                case 8:
                    // Actual pipe ports, clockwise from up. Blue means reachable from the spring.
                    Color water=Active?blue:Ui.Hex("#857966");
                    if((Value&1)!=0)Box(-6,0,12,40,water);
                    if((Value&2)!=0)Box(0,-6,40,12,water);
                    if((Value&4)!=0)Box(-6,-40,12,40,water);
                    if((Value&8)!=0)Box(-40,-6,40,12,water);
                    if(Value!=0)Disk(0,0,9,water);
                    break;
                case 9:
                    if(Value==1){Poly(gold,new Vector2(0,28),new Vector2(24,0),new Vector2(0,-28),new Vector2(-24,0));Disk(0,0,9,blue);}
                    else {Box(-25,-25,50,3,pale);Box(-25,22,50,3,pale);}
                    break;
                case 10:case 13:
                    if(Kind==10)
                    {Poly(Ui.Hex("#9a6944"),new Vector2(-38,-25),new Vector2(38,-25),new Vector2(30,-39),new Vector2(-30,-39));Box(-38,-27,76,4,ink);}
                    for(int i=0;i<Value;i++)
                    {
                        float x=Kind==13&&Value==1?0:-28+i%5*14,y=Kind==13&&Value==1?0:27-i/5*18;
                        Disk(x,y,6,Ui.Hex("#d97b2b"));Box(x-1,y+4,2,5,ink);
                    }
                    break;
                case 12:
                    Box(-39,-39,78,78,gold);Box(-35,-35,70,70,pale);
                    Disk(0,25,10,Ui.Hex("#a7d9ea"));Disk(-3,26,1.6f,ink);Disk(3,26,1.6f,ink);
                    for(int i=0;i<Value;i++)
                    {
                        float x=-26+i%5*13,y=3-i/5*17;
                        Box(x-3,y-4,6,7,Ui.Hex("#aa6aa8"));
                        Poly(gold,new Vector2(x-3,y+5),new Vector2(x,y+10),new Vector2(x+3,y+5));
                    }
                    break;
                case 14:
                    Disk(0,-9,23,blue);
                    Poly(blue,new Vector2(-22,-3),new Vector2(0,35),new Vector2(22,-3));
                    Disk(-7,-8,6,Ui.Hex("#bfe7f4"));break;
                case 11:
                    Box(-2,27,4,9,ink);
                    if(Value==0)Disk(0,0,25,gold);
                    else if(Value==1)Poly(blue,new Vector2(0,27),new Vector2(28,-24),new Vector2(-28,-24));
                    else Poly(Ui.Hex("#aa6aa8"),new Vector2(0,29),new Vector2(27,0),new Vector2(0,-29),new Vector2(-27,0));
                    break;
                case 0:Arrow(Value,blue);break;
                case 1:
                    Disk(0,28,6,ink);Poly(gold,new Vector2(-25,-17),new Vector2(-18,22),new Vector2(18,22),new Vector2(25,-17));
                    Box(-31,-22,62,8,ink);Disk(0,-29,6,gold);break;
                case 2:
                    if(Active)Arrow(1,blue);
                    else if(Value==1)
                    {Disk(-13,7,17,ink);Disk(7,14,21,ink);Disk(22,4,15,ink);Poly(gold,new Vector2(4,-1),new Vector2(-12,-22),new Vector2(0,-22),new Vector2(-5,-36),new Vector2(19,-13),new Vector2(5,-13));}
                    else if(Value==2)
                    {Line(new Vector2(0,-31),new Vector2(0,27),5,ink);Poly(Ui.Hex("#68a36d"),new Vector2(0,-1),new Vector2(-30,8),new Vector2(-22,27),new Vector2(0,16));Poly(Ui.Hex("#91b76e"),new Vector2(0,-14),new Vector2(28,0),new Vector2(28,19),new Vector2(0,6));}
                    else Disk(0,0,6,blue);break;
                case 3:
                    if(Value==0){Moon(1);break;}
                    for(int i=0;i<8;i++){float a=i*Mathf.PI/4;var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));Line(d*27,d*36,5,gold);}Disk(0,0,21,gold);break;
                case 4:
                    Disk(0,0,30,Value==1?gold:pale);Disk(0,0,24,ink);
                    Poly(Value==1?gold:blue,new Vector2(7,22),new Vector2(-16,-3),new Vector2(-2,-3),new Vector2(-8,-25),new Vector2(18,5),new Vector2(3,5));break;
                case 5:
                    Box(-30,-34,60,68,ink);Box(-25,-29,50,58,pale);
                    for(int i=0;i<Value;i++)Disk(-16+i%3*16,-22+i/3*13,5,blue);break;
                case 6:
                    Poly(gold,new Vector2(0,34),new Vector2(34,0),new Vector2(0,-34),new Vector2(-34,0));
                    Poly(blue,new Vector2(0,23),new Vector2(23,0),new Vector2(0,-23),new Vector2(-23,0));break;
                case 7:if(Value<0){for(int y=-35;y<35;y+=14)Box(-2,y,4,7,blue);}else Moon(Value);break;
            }
        }
    }
}
