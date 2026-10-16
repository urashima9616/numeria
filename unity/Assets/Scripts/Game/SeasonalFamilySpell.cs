using System;
using Numeria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>Family-specific motion and motifs, not damage/status logic. Every effect shares one impact clock.</summary>
    public sealed class SeasonalFamilySpell
    {
        private readonly SkillVisualKind _kind;
        private readonly Vector2 _from, _to;
        private readonly float _scale, _sign;
        private readonly int _stage;
        private readonly RectTransform _caster;
        private readonly Vector3 _home;
        private readonly Image[] _pieces = new Image[12];
        private readonly Image _accent, _light;

        public static string MotifPath(SkillVisualKind kind)
        {
            int index; string theme = "pumpkin";
            switch (kind)
            {
                case SkillVisualKind.RindPounce: index=1; break;
                case SkillVisualKind.RindRam: index=2; break;
                case SkillVisualKind.SeedArray: index=3; break;
                case SkillVisualKind.ShellRoll: index=4; break;
                case SkillVisualKind.LanternBeam: index=5; break;
                case SkillVisualKind.TerracePulse: index=6; break;
                case SkillVisualKind.SliceSwirl: index=7; break;
                case SkillVisualKind.VineSwoop: index=8; break;
                case SkillVisualKind.StrawSpiral: index=9; break;
                case SkillVisualKind.PondRings: index=10; break;
                case SkillVisualKind.CoilCount: index=11; break;
                case SkillVisualKind.BranchCharge: index=12; break;
                case SkillVisualKind.PetalHex: index=13; break;
                case SkillVisualKind.SeedSum: index=14; break;
                default:
                    theme="ghost";
                    switch(kind)
                    {
                        case SkillVisualKind.WispOrbit:index=0;break;
                        case SkillVisualKind.CurtainSweep:index=1;break;
                        case SkillVisualKind.KeyTurn:index=2;break;
                        case SkillVisualKind.InkArc:index=4;break;
                        case SkillVisualKind.SilhouetteLeap:index=5;break;
                        case SkillVisualKind.BellEcho:index=6;break;
                        case SkillVisualKind.SilverFold:index=7;break;
                        case SkillVisualKind.PuppetParade:index=8;break;
                        case SkillVisualKind.WickRush:index=9;break;
                        case SkillVisualKind.PageFan:index=10;break;
                        case SkillVisualKind.TeaSpiral:index=11;break;
                        case SkillVisualKind.EchoSweep:index=12;break;
                        case SkillVisualKind.PatchworkWave:index=13;break;
                        case SkillVisualKind.SecretPassage:index=14;break;
                        default:throw new ArgumentOutOfRangeException(nameof(kind));
                    }
                    break;
            }
            return "generated/Exploration/"+theme+"-spell-"+index;
        }
        public SeasonalFamilySpell(Transform root,SkillVisualKind kind,Vector2 from,Vector2 to,
            float scale,RectTransform caster,int stage)
        {
            _kind=kind;_from=from;_to=to;_scale=scale;_sign=to.x>=from.x?1:-1;
            _stage=Mathf.Clamp(stage,0,2);_caster=caster;_home=caster!=null?caster.localPosition:Vector3.zero;
            var art=Resources.Load<Sprite>(MotifPath(kind));
            for(int i=0;i<_pieces.Length;i++)_pieces[i]=Make(root,"FamilyMotif"+i,art);
            string accent=kind==SkillVisualKind.KeyTurn?"ghost-spell-15":
                kind==SkillVisualKind.SecretPassage?"ghost-spell-5":kind==SkillVisualKind.SeedSum?"pumpkin-spell-15":"ghost-spell-12";
            _accent=Make(root,"FamilyAccent",Resources.Load<Sprite>("generated/Exploration/"+accent));
            if(kind==SkillVisualKind.SecretPassage)_accent.sprite=SpriteLib.LargeIcon("mousoleum");
            _light=Make(root,"FamilyLight",SpellSequence.Glow());
        }
        private static Image Make(Transform root,string name,Sprite sprite)
        {var image=Ui.SpriteImg(root,name,sprite);image.preserveAspect=true;image.raycastTarget=false;return image;}
        private void Put(Image image,Vector2 point,Vector2 size,float alpha,float angle=0)
        {
            image.rectTransform.anchoredPosition=point;image.rectTransform.sizeDelta=new Vector2(Mathf.Max(1,size.x),Mathf.Max(1,size.y))*_scale;
            image.rectTransform.localRotation=Quaternion.Euler(0,0,angle);image.color=new Color(1,1,1,Mathf.Clamp01(alpha));
        }
        private static Vector2 Orbit(float a,float x,float y)=>new Vector2(Mathf.Cos(a)*x,Mathf.Sin(a)*y);
        private void Move(Vector2 offset){if(_caster!=null)_caster.localPosition=_home+(Vector3)offset;}
        public void Sample(float t)
        {
            float charge=Mathf.Clamp01(t/.65f),f=Mathf.Clamp01((t-.65f)/(SpellSequence.ImpactTime-.65f));
            float after=Mathf.Clamp01((t-SpellSequence.ImpactTime)/(SpellSequence.Duration-SpellSequence.ImpactTime));
            float live=charge*(1-after),hit=f>=.98f?live:0;
            Vector2 delta=_to-_from,head=Vector2.Lerp(_from,_to,f);
            Move(Vector2.zero);_accent.color=_light.color=Color.clear;
            int count=4+_stage*2;
            for(int i=0;i<_pieces.Length;i++)
            {
                float a=i*Mathf.PI/4,side=i%2==0?-1:1;
                Vector2 p=head,size=Vector2.one*52;
                float alpha=i<count?live:0,rotation=0;
                switch(_kind)
                {
                    case SkillVisualKind.RindPounce:
                        Move(delta*.65f*(f<1?f:1-after)+Vector2.up*Mathf.Sin(f*Mathf.PI)*100*(1-after));
                        p=_to+new Vector2(side*(1-f)*75,i/2*22);alpha=i<2+_stage?live*Mathf.Clamp01((f-.55f)*4):0;
                        size=Vector2.one*160;rotation=side*35;break;
                    case SkillVisualKind.RindRam:
                        Move(delta*.75f*(f<1?f*f:1-after)+Vector2.down*(1-f)*charge*15);
                        p=Vector2.Lerp(_from,_to,f*f)-new Vector2(_sign*i*32,i*8);
                        alpha=i<3?live*(1-i*.28f):0;size=Vector2.one*(155-i*25);break;
                    case SkillVisualKind.SeedArray:
                        p=Vector2.Lerp(_from+new Vector2(i%4*42*_sign,(i/4==0?-1:1)*65),_to,f);
                        alpha=i<8?live:0;size=Vector2.one*42;break;
                    case SkillVisualKind.ShellRoll:
                        Move(delta*.6f*(f<1?f:1-after));
                        p=Vector2.Lerp(_from,_to,Mathf.Max(0,f-i*.07f))+Vector2.down*20;
                        alpha=i<1+_stage*2?live*(1-i*.16f):0;size=Vector2.one*(i==0?150:62);rotation=-_sign*f*720;break;
                    case SkillVisualKind.LanternBeam:
                        p=_from+new Vector2(_sign*65,(i-_stage*.5f)*65);alpha=i<=_stage?live:0;size=Vector2.one*80;
                        if(i==0)
                        {
                            _light.preserveAspect=false;
                            Put(_light,Vector2.Lerp(_from,_to,f*.5f),new Vector2(delta.magnitude*f,25+_stage*18),live*.65f,
                                Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
                            _light.color=new Color(1,.73f,.25f,live*.65f);
                        }
                        break;
                    case SkillVisualKind.TerracePulse:
                        float wave=Mathf.Clamp01(f*1.35f-i*.16f);
                        p=Vector2.Lerp(_from,_to,wave)+Vector2.down*(35-i*10);
                        alpha=i<3?live*Mathf.Clamp01(wave*8):0;size=new Vector2(100+wave*90,80+i*12);break;
                    case SkillVisualKind.SliceSwirl:
                        p=head+Orbit(i*Mathf.PI/2+t*4,110,90)*(1-f);
                        alpha=i<4?live:0;rotation=i*90+t*180;size=Vector2.one*(80+f*25);break;
                    case SkillVisualKind.VineSwoop:
                        float dive=f<.55f?-f*160:-88+(f-.55f)*196;
                        Move(delta*.65f*(f<1?f:1-after)+Vector2.up*dive*(1-after));
                        p=head+new Vector2(side*(30+i*12),dive+i*14);rotation=side*45+after*120;alpha*=1-i*.08f;break;
                    case SkillVisualKind.StrawSpiral:
                        p=head+Orbit(a+t*7,(20+i*10)*(1-f*.6f),25)+Vector2.up*(i*20-50);
                        rotation=t*220+i*45;size=Vector2.one*50;break;
                    case SkillVisualKind.PondRings:
                        float ripple=Mathf.Clamp01(f*1.4f-i*.2f);p=Vector2.Lerp(_from,_to,ripple)+Vector2.down*45;
                        alpha=i<3?live*Mathf.Clamp01(ripple*8):0;size=new Vector2(90+ripple*200,65+ripple*60);break;
                    case SkillVisualKind.CoilCount:
                        p=_to+Orbit(i*Mathf.PI/4+t*2,105*(1+after),55*(1+after))*Mathf.Lerp(1.6f,1,f);
                        alpha=i<8?live*Mathf.Clamp01(f*5-i*.3f):0;rotation=i*45+t*80;size=Vector2.one*65;break;
                    case SkillVisualKind.BranchCharge:
                        Move(delta*.72f*(f<1?f*f:1-after));p=head+new Vector2(0,70+i*20);
                        alpha=i<1+_stage?live*(1-i*.25f):0;size=Vector2.one*145;break;
                    case SkillVisualKind.PetalHex:
                        p=head+Orbit(i*Mathf.PI/3,100,90)*(1-f*.75f);
                        alpha=i<6?live*Mathf.Clamp01(charge*8-i):0;rotation=i*60+90;break;
                    case SkillVisualKind.SeedSum:
                        p=Vector2.Lerp(_from+new Vector2(side*105,i/2*38-50),head,f);alpha=i<6?live:0;size=Vector2.one*42;
                        Put(_accent,_to,Vector2.one*(130+after*100),hit);break;
                    case SkillVisualKind.WispOrbit:
                        p=head+Orbit(t*(3+i/3)+i*Mathf.PI*2/3,40+i/3*45,30+i/3*28)*(1-f*.65f);
                        alpha=i<(_stage+1)*3?live:0;break;
                    case SkillVisualKind.CurtainSweep:
                        p=head+new Vector2(side*f*50,15);size=new Vector2(35+f*150,170);
                        alpha=i<2?live*.85f:0;rotation=side*f*30;break;
                    case SkillVisualKind.KeyTurn:
                        p=Vector2.Lerp(_from,_to+Vector2.left*_sign*45,Mathf.Min(1,f*1.7f));size=Vector2.one*140;
                        alpha=i==0?live:0;rotation=Mathf.Clamp01((f-.5f)*2)*180;
                        Put(_accent,_to+Vector2.right*_sign*after*90,Vector2.one*160,live*f*(1-after));break;
                    case SkillVisualKind.InkArc:
                        float ink=Mathf.Clamp01(f*1.4f-i*.08f);
                        p=Vector2.Lerp(_from,_to,ink)+Vector2.up*Mathf.Sin(ink*Mathf.PI)*100;
                        alpha=i<6?live*Mathf.Clamp01(ink*6)*(1-i*.12f):0;rotation=ink*-150;size=Vector2.one*(90-i*8);break;
                    case SkillVisualKind.SilhouetteLeap:
                        p=Vector2.Lerp(_from,_to,Mathf.Min(1,f*1.5f))+Vector2.down*55;
                        alpha=i==0?live*.8f:0;size=new Vector2(150,70);
                        Move(delta*.7f*(f<1?f*f:1-after)+Vector2.up*Mathf.Sin(f*Mathf.PI)*80*(1-after));break;
                    case SkillVisualKind.BellEcho:
                        p=_from+new Vector2((i-_stage*.5f)*60,60);alpha=i<=_stage?live:0;
                        size=Vector2.one*90;rotation=Mathf.Sin(t*18)*12;
                        Put(_accent,head,Vector2.one*(90+f*160),live*f,_sign<0?180:0);break;
                    case SkillVisualKind.SilverFold:
                        p=head+Vector2.right*side*100*(1-f);alpha=i<2?live:0;
                        size=new Vector2(100*(1-f*.8f),170);rotation=side*f*25;break;
                    case SkillVisualKind.PuppetParade:
                        p=head+new Vector2(side*65*(1-f),Mathf.Abs(Mathf.Sin(t*12))*25);
                        alpha=i<2?live:0;size=Vector2.one*110;rotation=side*Mathf.Sin(t*12)*12;break;
                    case SkillVisualKind.WickRush:
                        float wick=Mathf.Clamp01(f*1.3f-i*.07f);p=Vector2.Lerp(_from,_to,wick)+Vector2.down*35;
                        alpha=i<3+_stage?live*Mathf.Clamp01(wick*6)*(1-i*.13f):0;size=Vector2.one*(70-i*4);break;
                    case SkillVisualKind.PageFan:
                        float fan=(i-(count-1)*.5f)*17;p=head+Orbit((fan+90)*Mathf.Deg2Rad,100,65)*(1-f*.65f);
                        rotation=-fan;size=Vector2.one*100;break;
                    case SkillVisualKind.TeaSpiral:
                        p=head+Orbit(a+t*4,80,55)*(1-f);alpha=i<3?live*(1-i*.2f):0;
                        size=Vector2.one*100;rotation=Mathf.Sin(t*3+i)*12;
                        Put(_accent,_to,new Vector2(180+after*80,110),hit,90);break;
                    case SkillVisualKind.EchoSweep:
                        float echo=Mathf.Clamp01(f*1.5f-i*.18f);p=Vector2.Lerp(_from,_to,echo);
                        alpha=i<3?live*Mathf.Clamp01(echo*8):0;rotation=_sign<0?0:180;size=Vector2.one*(60+echo*175);break;
                    case SkillVisualKind.PatchworkWave:
                        p=head+new Vector2((i%3-1)*48,(i/3-1)*48)*(1+(1-charge)*2);
                        alpha=i<9?live:0;size=Vector2.one*51;break;
                    case SkillVisualKind.SecretPassage:
                        p=_to+Vector2.right*side*115;alpha=i<2?live*Mathf.Clamp01(f*5):0;size=new Vector2(100,160);
                        Put(_accent,_to+Vector2.right*Mathf.Lerp(-115,115,f),Vector2.one*85,live*Mathf.Clamp01(f*4));break;
                }
                Put(_pieces[i],p,size,alpha,rotation);
            }
        }
    }
}
