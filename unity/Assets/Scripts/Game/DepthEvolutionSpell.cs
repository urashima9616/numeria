using System;
using System.Collections.Generic;
using System.Linq;
using Numeria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>Visual-only, explicitly authored pairs for the 88 depth families. Base forms keep their original spells.</summary>
    public sealed class DepthEvolutionSpell
    {
        public enum Motion
        {
            GroupDrop, SproutRow, HopTrail, BeadChain, PairOpen, Converge,
            Orbit, Layers, Fork, Merge, RepeatSweep, Fan, Grid, GridBeam,
            Straighten, Chase, Spiral, RollingRings, Plates, Ram, Roots, LeafRain,
            Steps, Leap, Stairs, Bells, CrossRings, PairedArc, CircleFall,
            Curve, CloseRing, Spikes, TripleArc, Lift, Arches, Flags,
            CloudMerge, WindBand, NestedRings, Point, LinkedRings, Sail,
            Jet, Island, GardenRing, Bridge, TripleWave, Bloom, Refract,
            BeadRail, BeadArray, Gather, Overflow, Prism, Focus, Dial,
            GateBeam, GroundTap, Vein, WingSpark, Geode, Pulse, Circuit,
            Coal, CrystalFlower, Rail, SilverLine, Stones, Magnet, OrePush,
            Dust, Rhythm, SoundWave, Reflect, Vent, StarChain, Crater,
            Glyph, Mosaic, Fold, Gears, Repair, Scroll, Constellation, CrownTide
        }

        public sealed class Profile
        {
            public readonly string BaseId, WorldId;
            public readonly Motion Second, Third;
            public readonly bool Giant;
            public string MotifPath => "generated/Exploration/depth-spell-"+BaseId;
            public string AccentPath => "generated/Exploration/depth-spell-"+WorldId+"-accent";
            internal Profile(string id,Motion second,Motion third)
            {
                var world=DepthCatalog.Worlds.Single(w=>w.Species.Any(s=>s.Id==id));
                BaseId=id;WorldId=world.Id;Second=second;Third=third;Giant=world.Giant.Id==id;
            }
        }
        private static Profile P(string id,Motion second,Motion third)=>new Profile(id,second,third);
        public static readonly Profile[] Profiles =
        {
            P("acornib",Motion.GroupDrop,Motion.SproutRow),
            P("dewhopper",Motion.HopTrail,Motion.BeadChain),
            P("fernfawn",Motion.PairOpen,Motion.Converge),
            P("mushmellow",Motion.Orbit,Motion.Layers),
            P("rootter",Motion.Fork,Motion.Merge),
            P("petalark",Motion.RepeatSweep,Motion.Fan),
            P("honeyhex",Motion.Grid,Motion.GridBeam),
            P("twiggle",Motion.Straighten,Motion.Chase),
            P("sprigsnail",Motion.Spiral,Motion.RollingRings),
            P("seedillo",Motion.Plates,Motion.Ram),
            P("arboracle",Motion.Roots,Motion.LeafRain),
            P("snowmote",Motion.PairOpen,Motion.Converge),
            P("rimehare",Motion.Steps,Motion.Leap),
            P("icicub",Motion.Grid,Motion.Stairs),
            P("chimegoat",Motion.Bells,Motion.CrossRings),
            P("frostfinch",Motion.PairedArc,Motion.Converge),
            P("glintmarmot",Motion.GroupDrop,Motion.CircleFall),
            P("flurryfox",Motion.Curve,Motion.CloseRing),
            P("quartzquill",Motion.Spikes,Motion.Fan),
            P("sleetseal",Motion.RollingRings,Motion.TripleArc),
            P("peakpuff",Motion.Lift,Motion.Stairs),
            P("glaciomamm",Motion.PairOpen,Motion.Arches),
            P("kitelet",Motion.Curve,Motion.Flags),
            P("cloudcalf",Motion.CloudMerge,Motion.Ram),
            P("gustgecko",Motion.Steps,Motion.WindBand),
            P("nimbusquid",Motion.Orbit,Motion.NestedRings),
            P("vanevulp",Motion.Point,Motion.PairedArc),
            P("plumepup",Motion.Steps,Motion.Leap),
            P("halohare",Motion.CrossRings,Motion.LinkedRings),
            P("drifturtle",Motion.Sail,Motion.Layers),
            P("zephyray",Motion.PairOpen,Motion.Jet),
            P("aerolotl",Motion.Stairs,Motion.Arches),
            P("stratocetus",Motion.Island,Motion.GardenRing),
            P("sandib",Motion.CrossRings,Motion.Bridge),
            P("duneling",Motion.Spiral,Motion.TripleWave),
            P("cactot",Motion.Pulse,Motion.Bloom),
            P("miragecko",Motion.Refract,Motion.Prism),
            P("scarabacus",Motion.BeadRail,Motion.BeadArray),
            P("suncamel",Motion.Gather,Motion.Curve),
            P("oasisprout",Motion.Plates,Motion.Overflow),
            P("glasscobra",Motion.Prism,Motion.Focus),
            P("saffronfox",Motion.Curve,Motion.Chase),
            P("moonjerboa",Motion.Steps,Motion.Leap),
            P("solarsphinx",Motion.Dial,Motion.GateBeam),
            P("oremole",Motion.GroundTap,Motion.Vein),
            P("copperbat",Motion.WingSpark,Motion.PairedArc),
            P("geodillo",Motion.Geode,Motion.Pulse),
            P("voltweevil",Motion.Point,Motion.Circuit),
            P("coalcrab",Motion.Coal,Motion.Merge),
            P("amethystot",Motion.Pulse,Motion.CrystalFlower),
            P("railrook",Motion.Fork,Motion.Rail),
            P("nickelnewt",Motion.SilverLine,Motion.Chase),
            P("lumenlug",Motion.Grid,Motion.GridBeam),
            P("pyritoad",Motion.Stones,Motion.CircleFall),
            P("magnetaur",Motion.Magnet,Motion.OrePush),
            P("ashaxol",Motion.GroupDrop,Motion.Dust),
            P("basaltot",Motion.Lift,Motion.Stairs),
            P("cindercricket",Motion.Rhythm,Motion.SoundWave),
            P("steamink",Motion.Curve,Motion.Chase),
            P("obsidianole",Motion.Reflect,Motion.Fan),
            P("magmole",Motion.Roots,Motion.Merge),
            P("emberfin",Motion.RollingRings,Motion.Fan),
            P("ventviper",Motion.Vent,Motion.Merge),
            P("pumipup",Motion.Stones,Motion.Leap),
            P("sootsprite",Motion.Gather,Motion.StarChain),
            P("calderadon",Motion.Crater,Motion.Bloom),
            P("glyphimp",Motion.Plates,Motion.Glyph),
            P("tesselkid",Motion.Repair,Motion.Mosaic),
            P("papyrill",Motion.Fold,Motion.Fan),
            P("cogcrab",Motion.Gears,Motion.RollingRings),
            P("sundialynx",Motion.Dial,Motion.Curve),
            P("mosaicarp",Motion.Grid,Motion.Mosaic),
            P("abacat",Motion.BeadRail,Motion.Focus),
            P("relicub",Motion.Repair,Motion.Ram),
            P("scrollfinch",Motion.Scroll,Motion.Chase),
            P("prismidol",Motion.Prism,Motion.Focus),
            P("chronogolem",Motion.Orbit,Motion.GateBeam),
            P("ripplewisp",Motion.CrossRings,Motion.NestedRings),
            P("moonotter",Motion.GroupDrop,Motion.Merge),
            P("lotusquid",Motion.Bloom,Motion.CloseRing),
            P("veilmoth",Motion.PairOpen,Motion.Converge),
            P("pearlfin",Motion.Spiral,Motion.NestedRings),
            P("mistfawn",Motion.Steps,Motion.Leap),
            P("reedrake",Motion.Mosaic,Motion.Bridge),
            P("tideshell",Motion.Layers,Motion.RollingRings),
            P("gloamray",Motion.Gather,Motion.Jet),
            P("starnewt",Motion.StarChain,Motion.Constellation),
            P("lunabyss",Motion.Crater,Motion.CrownTide)
        };
        private static readonly Dictionary<string,Profile> ById=Profiles.ToDictionary(p=>p.BaseId);
        public static Profile For(string speciesId)
        {
            string id=ExpansionCatalog.DepthBase(speciesId);
            return id!=null && ById.TryGetValue(id,out var profile)?profile:null;
        }
        public static bool Applies(string speciesId,SkillVisualKind kind)=>kind!=SkillVisualKind.Physical
            &&GameData.StageIndex(speciesId)>0&&For(speciesId)!=null;

        private readonly Profile _profile;
        private readonly Motion _motion;
        private readonly Vector2 _from,_to;
        private readonly float _scale,_sign;
        private readonly RectTransform _caster;
        private readonly Vector3 _home;
        private readonly Image[] _pieces=new Image[12],_accents=new Image[12],_links=new Image[12];
        public DepthEvolutionSpell(Transform root,RectTransform caster,string speciesId,Vector2 from,Vector2 to,float scale)
        {
            _profile=For(speciesId)??throw new ArgumentException("Missing depth spell: "+speciesId);
            _motion=GameData.StageIndex(speciesId)==1?_profile.Second:_profile.Third;
            _from=from;_to=to;_scale=scale*(_profile.Giant?1.12f:1);_sign=to.x>=from.x?1:-1;
            _caster=caster;_home=caster.localPosition;
            var art=Resources.Load<Sprite>(_profile.MotifPath);
            var accent=Resources.Load<Sprite>(_profile.BaseId=="magnetaur"
                ?"generated/Exploration/depth-spell-pyritoad":_profile.BaseId=="chimegoat"
                ?"generated/Exploration/depth-spell-sleetseal":_profile.AccentPath);
            if(art==null||accent==null)throw new InvalidOperationException("Missing depth spell art: "+speciesId);
            for(int i=0;i<12;i++)
            {
                _links[i]=Make(root,"DepthLink"+i,SpellSequence.Glow());_links[i].preserveAspect=false;
                _pieces[i]=Make(root,"DepthMotif"+i,art);
                _accents[i]=Make(root,"DepthAccent"+i,accent);
            }
        }
        private static Image Make(Transform parent,string name,Sprite sprite)
        {var image=Ui.SpriteImg(parent,name,sprite);image.preserveAspect=true;image.raycastTarget=false;return image;}
        private void Put(Image image,Vector2 position,Vector2 size,float alpha,float angle=0)
        {
            image.rectTransform.anchoredPosition=position;
            image.rectTransform.sizeDelta=new Vector2(Mathf.Max(1,size.x),Mathf.Max(1,size.y))*_scale;
            image.rectTransform.localRotation=Quaternion.Euler(0,0,angle);
            image.color=new Color(1,1,1,Mathf.Clamp01(alpha));
        }
        private void Link(int i,Vector2 a,Vector2 b,float alpha,float width=8)
        {
            Vector2 d=b-a;
            Put(_links[i],(a+b)*.5f,new Vector2(d.magnitude,width),alpha,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
            _links[i].color=new Color(.9f,.93f,1,Mathf.Clamp01(alpha)*.55f);
        }
        private Vector2 Along(float u,float height=0)=>Vector2.Lerp(_from,_to,u)+Vector2.up*height;
        private static Vector2 Ring(float angle,float x,float y)=>new Vector2(Mathf.Cos(angle)*x,Mathf.Sin(angle)*y);
        private void Move(float progress,float height,float after)
        {_caster.localPosition=_home+(Vector3)((_to-_from)*progress*.65f+Vector2.up*height)*(1-after);}

        public void Sample(float t)
        {
            float c=Mathf.Clamp01(t/.65f),f=Mathf.Clamp01((t-.65f)/(SpellSequence.ImpactTime-.65f));
            float after=Mathf.Clamp01((t-SpellSequence.ImpactTime)/(SpellSequence.Duration-SpellSequence.ImpactTime));
            float live=c*(1-after),impact=f>=1?1-after:0;
            _caster.localPosition=_home;
            for(int i=0;i<12;i++){_accents[i].color=Color.clear;_links[i].color=Color.clear;}
            for(int i=0;i<12;i++)
            {
                float u=i/11f,a=i*Mathf.PI/3,side=i%2==0?-1:1;
                float q=Mathf.Clamp01(f*1.5f-i*.055f),alpha=live,angle=0;
                Vector2 p=Along(f),size=Vector2.one*55;
                int count=6;
                switch(_motion)
                {
                    case Motion.GroupDrop:
                        p=_from+new Vector2(_sign*(i%3*58+30),i/3*65+150*(1-c));
                        p=Vector2.Lerp(p,_to+new Vector2((i%3-1)*50,i/3*40-25),f);break;
                    case Motion.SproutRow:
                        p=Along((i+1)/7f,-45);size=new Vector2(70,35+70*q);
                        alpha=live*Mathf.Clamp01(f*7-i);Put(_accents[i],p+Vector2.up*q*45,Vector2.one*(35+35*q),alpha);break;
                    case Motion.HopTrail:
                    case Motion.BeadChain:
                        p=Along(q,Mathf.Sin(q*Mathf.PI)*155);count=_motion==Motion.HopTrail?4:10;
                        size=Vector2.one*(_motion==Motion.HopTrail?65:43);alpha*=Mathf.Clamp01(q*8);break;
                    case Motion.PairOpen:
                        count=2;p=_from+new Vector2(side*(35+70*c),80*c);size=Vector2.one*130;
                        angle=side*35*c;p=Vector2.Lerp(p,_to+new Vector2(side*50,25),f);break;
                    case Motion.Converge:
                        p=Vector2.Lerp(_from+Ring(a,145,110),_to+Ring(a,20,15),f);angle=-a*Mathf.Rad2Deg;
                        size=Vector2.one*(75+20*c);break;
                    case Motion.Orbit:
                        p=Along(f*.75f)+Ring(a+t*2,110,75);angle=Mathf.Sin(t*4+i)*12;break;
                    case Motion.Layers:
                        count=3;p=Along(q)+new Vector2(0,50+i*62);size=Vector2.one*(130-i*24);
                        alpha*=.5f+.5f*Mathf.Sin(t*5-i);break;
                    case Motion.Fork:
                    case Motion.Roots:
                        p=Along(u,side*Mathf.Sin(u*Mathf.PI)*90-45);count=12;
                        alpha*=Mathf.Clamp01(c*12-i);angle=side*25;
                        Link(i,Along(Mathf.Max(0,u-.12f),-45),p,alpha);break;
                    case Motion.Merge:
                        count=8;p=Along(f,side*(1-f)*(50+i/2*28));angle=side*40*(1-f);break;
                    case Motion.RepeatSweep:
                        p=Along(q,(i%3-1)*60);angle=i%3*30;alpha*=Mathf.Clamp01(q*6);count=9;break;
                    case Motion.Fan:
                        p=Along(f)+Ring(-1.1f+i*.44f,140,120)*Mathf.Sin(f*Mathf.PI);
                        angle=-55+i*22;size=Vector2.one*(75+30*f);break;
                    case Motion.Grid:
                    case Motion.GridBeam:
                        p=_from+new Vector2(_sign*(55+i%3*52),(i/3-1)*48+60);count=9;
                        p=Vector2.Lerp(p,_from+new Vector2(_sign*120,60),_motion==Motion.GridBeam?f:0);
                        alpha*=Mathf.Clamp01(c*9-i);
                        if(_motion==Motion.GridBeam&&i==0)Link(0,p,Along(f),live*f,35);break;
                    case Motion.Straighten:
                        p=Along(u,(1-c)*Mathf.Sin(u*9)*65);angle=(1-c)*Mathf.Sin(i)*40;count=12;break;
                    case Motion.Chase:
                    case Motion.SilverLine:
                        p=Along(_motion==Motion.Chase?q:u,Mathf.Sin((_motion==Motion.Chase?q:u)*Mathf.PI)*95);
                        alpha*=Mathf.Clamp01((_motion==Motion.Chase?q:c*1.5f-u)*8);count=10;angle=_sign*20;
                        if(i>0)Link(i,Along(Mathf.Max(0,(_motion==Motion.Chase?q:u)-.12f),75),p,alpha,5);break;
                    case Motion.Spiral:
                        count=1;p=Along(f);size=Vector2.one*(110+50*c);angle=-_sign*t*220;break;
                    case Motion.RollingRings:
                        count=3;p=Along(q);size=Vector2.one*(110+i*25);angle=-_sign*q*360;break;
                    case Motion.Plates:
                        count=4;p=Along(f)+Ring(i*Mathf.PI/2,80*c,70*c);size=Vector2.one*85;angle=i*90*c;break;
                    case Motion.Ram:
                        count=1;p=Along(f*f);size=Vector2.one*210;angle=-_sign*f*360;Move(f*f,0,after);break;
                    case Motion.LeafRain:
                        count=12;p=_to+new Vector2((i%4-1.5f)*65,160-q*220+i/4*25);
                        alpha*=Mathf.Clamp01(q*8);angle=Mathf.Sin(t*3+i)*35;
                        Put(_accents[i],p,Vector2.one*60,alpha);alpha=0;break;
                    case Motion.Steps:
                    case Motion.Leap:
                        count=3;p=Along((i+1)/4f,-35+Mathf.Sin((i+1)*Mathf.PI/4)*30);
                        alpha*=Mathf.Clamp01(c*3-i);size=Vector2.one*90;
                        if(_motion==Motion.Leap)Move(f,Mathf.Abs(Mathf.Sin(f*Mathf.PI*3))*70,after);break;
                    case Motion.Stairs:
                        count=5;p=Along((i+1)/6f,-45+i*28);p=Vector2.Lerp(p,_to+Vector2.up*i*12,q);
                        alpha*=Mathf.Clamp01(c*5-i);size=new Vector2(100,90);break;
                    case Motion.Bells:
                        count=2;p=_from+new Vector2(side*85,80);size=Vector2.one*110;
                        angle=Mathf.Sin(t*9+i*Mathf.PI)*20;alpha*=.65f+.35f*Mathf.Sin(t*9+i*Mathf.PI);break;
                    case Motion.CrossRings:
                        count=2;p=Along(f*.8f)+new Vector2(side*50,0);size=Vector2.one*(130+70*f);
                        angle=side*t*30;
                        if(_profile.BaseId=="chimegoat")
                        {Put(_accents[i],p,Vector2.one*(180+90*f),live);size=Vector2.one*75;angle=Mathf.Sin(t*9+i*Mathf.PI)*15;}
                        break;
                    case Motion.PairedArc:
                        count=2;p=Along(f,side*Mathf.Sin(f*Mathf.PI)*145);size=Vector2.one*170;angle=side*(45-90*f);break;
                    case Motion.CircleFall:
                        p=_to+Ring(a,105,50)+Vector2.up*(1-f)*170;size=Vector2.one*65;break;
                    case Motion.Curve:
                        count=3;p=Along(q,Mathf.Sin(q*Mathf.PI)*140);size=Vector2.one*(100-i*15);angle=45-90*q;break;
                    case Motion.CloseRing:
                        count=8;p=_to+Ring(i*Mathf.PI/4,155-105*f,110-60*f);
                        alpha*=Mathf.Clamp01(c*8-i);angle=i*45;break;
                    case Motion.Spikes:
                        p=_from+new Vector2(_sign*(35+i*34),i%2*45);p=Vector2.Lerp(p,_to,f);
                        alpha*=Mathf.Clamp01(c*6-i);size=new Vector2(60,110);break;
                    case Motion.TripleArc:
                        count=3;p=Along(f,(i-1)*Mathf.Sin(f*Mathf.PI)*150);size=Vector2.one*115;break;
                    case Motion.Lift:
                        count=4;p=_from+new Vector2(_sign*i*65,(-60+i*45)*c);
                        p=Vector2.Lerp(p,_to+Vector2.up*i*30,f);size=Vector2.one*90;break;
                    case Motion.Arches:
                        count=3;p=Along((i+1)/4f,30+Mathf.Sin((i+1)*Mathf.PI/4)*75);
                        alpha*=Mathf.Clamp01((c+f)*3-i);size=Vector2.one*(150+i*15);
                        Put(_accents[i],p+Vector2.down*f*90,Vector2.one*60,live*f);break;
                    case Motion.Flags:
                        count=3;p=Along(q,(i-1)*70);size=Vector2.one*150;angle=Mathf.Sin(t*7+i)*18;break;
                    case Motion.CloudMerge:
                        p=Vector2.Lerp(_from+Ring(a,140,100),_from+Vector2.up*50,c);
                        p=Vector2.Lerp(p,_to,f);size=Vector2.one*(60+70*c);break;
                    case Motion.WindBand:
                        count=8;p=Along(u,Mathf.Sin(u*6-t*3)*35);size=Vector2.one*95;
                        alpha*=Mathf.Clamp01(f*10-i);angle=-t*100;break;
                    case Motion.NestedRings:
                        count=4;p=Along(f*.8f);size=Vector2.one*(55+(i+1)*40*c+after*80);
                        alpha*=Mathf.Clamp01(c*4-i*.7f);break;
                    case Motion.Point:
                        count=2;p=_from+new Vector2(_sign*70,side*65);angle=-side*45*(1-c);size=Vector2.one*95;
                        if(i==0)Link(0,p,p+Vector2.down*130,live*c);break;
                    case Motion.LinkedRings:
                        count=5;p=Along((i+1)/6f,Mathf.Sin((i+1)*Mathf.PI/6)*90);size=Vector2.one*125;
                        alpha*=Mathf.Clamp01((c+f)*5-i);break;
                    case Motion.Sail:
                        count=1;p=Along(f);size=new Vector2(80+90*c,160);angle=Mathf.Sin(t*4)*10;break;
                    case Motion.Jet:
                        count=4;p=Along(f*f)+new Vector2(-_sign*i*40,(i-1.5f)*30);
                        size=new Vector2(180+f*130,80);angle=Mathf.Atan2((_to-_from).y,Mathf.Abs((_to-_from).x))*Mathf.Rad2Deg;break;
                    case Motion.Island:
                        count=1;p=Along(f*.65f,70*c);size=Vector2.one*210;break;
                    case Motion.GardenRing:
                        count=1;p=Along(f*.8f,70);size=Vector2.one*200;
                        for(int j=0;j<8;j++)Put(_accents[j],p+Ring(j*Mathf.PI/4+t,160,80),Vector2.one*60,live);break;
                    case Motion.Bridge:
                        count=5;p=Along((i+1)/6f,-30);size=Vector2.one*130;
                        alpha*=Mathf.Clamp01(c*5-i);Link(i,p,Along(Mathf.Min(1,(i+2)/6f),-30),live*f,22);break;
                    case Motion.TripleWave:
                        count=3;p=Along(q);size=new Vector2(180,110);angle=Mathf.Sin(t*4+i)*15;break;
                    case Motion.Bloom:
                    case Motion.CrystalFlower:
                        p=Along(f*.85f)+Ring(a,110*c,90*c);angle=i*60;size=Vector2.one*(80+40*f);
                        if(_profile.BaseId=="calderadon")
                        {Put(_accents[i],p,size,live,angle);alpha=i==0?live:0;p=_from+Vector2.up*65;size=Vector2.one*190;}
                        break;
                    case Motion.Refract:
                        count=1;p=Along(f,Mathf.Sin(f*Mathf.PI)*50);size=Vector2.one*160;
                        Put(_accents[0],p+new Vector2(_sign*50,20),Vector2.one*100,live*.5f);break;
                    case Motion.Prism:
                        count=3;p=Along(f,(i-1)*Mathf.Sin(f*Mathf.PI)*125);size=Vector2.one*90;
                        Link(i,_from,p,live*f,12);break;
                    case Motion.Focus:
                        p=Vector2.Lerp(_from+Ring(a,140,100),Along(f),c);size=Vector2.one*(70-35*f);
                        if(i==0)Link(0,_from,Along(f),live*f,30);break;
                    case Motion.BeadRail:
                        count=8;p=_from+new Vector2(_sign*(35+i%4*45)*(1-c),side*55);
                        p=Vector2.Lerp(p,_to,f);size=Vector2.one*45;Link(i,p-new Vector2(40,0),p+new Vector2(40,0),live*.6f,4);break;
                    case Motion.BeadArray:
                        count=9;p=Along(f)+new Vector2((i%3-1)*50,(i/3-1)*50);size=Vector2.one*45;break;
                    case Motion.Gather:
                        p=_from+Vector2.up*90+Ring(a,145*(1-c),105*(1-c));p=Vector2.Lerp(p,_to,f);
                        size=Vector2.one*(30+65*c);break;
                    case Motion.Overflow:
                        count=3;p=_from+new Vector2(_sign*i*70,110-i*55);size=Vector2.one*110;
                        Put(_accents[i],Vector2.Lerp(p,_to,q),Vector2.one*70,live*f);break;
                    case Motion.Dial:
                        count=1;p=_from+Vector2.up*60;size=Vector2.one*180;angle=c*140;
                        Link(0,p,p+Ring(c*2.4f,120,80),live,7);break;
                    case Motion.GateBeam:
                        count=1;p=_from+Vector2.up*60;size=Vector2.one*220;angle=c*35;
                        Link(0,p,Along(f),live*f,65);break;
                    case Motion.GroundTap:
                        count=2;p=Along((i+1)/3f,-45);size=Vector2.one*(80+30*Mathf.Sin(t*8-i));
                        alpha*=Mathf.Clamp01(c*2-i);break;
                    case Motion.Vein:
                    case Motion.Circuit:
                        count=8;p=Along((i+1)/9f,(i%2==0?0:30)-35);size=Vector2.one*80;
                        alpha*=Mathf.Clamp01((c+f)*8-i);
                        if(i>0)Link(i,Along(i/9f,(i%2==0?30:0)-35),p,alpha,10);
                        if(_motion==Motion.Circuit)Put(_accents[i],Along(q),Vector2.one*50,live*f);break;
                    case Motion.WingSpark:
                        count=8;p=_from+Ring((i/2+1)*.35f*side+Mathf.PI/2,130,100);size=Vector2.one*70;
                        alpha*=Mathf.Clamp01(c*8-i);Put(_accents[i],p,Vector2.one*35,alpha*.7f);break;
                    case Motion.Geode:
                        count=1;p=Along(f*.8f);size=Vector2.one*(100+100*c);angle=-360*c;break;
                    case Motion.Pulse:
                        count=3;p=Along(f*.8f)+new Vector2((i-1)*80,35);size=Vector2.one*(100+25*Mathf.Sin(t*6));break;
                    case Motion.Coal:
                        count=4;p=_from+new Vector2(_sign*(30+i*50),25);size=Vector2.one*80;
                        alpha*=Mathf.Clamp01(c*4-i);Put(_accents[i],p,Vector2.one*65,alpha*f);break;
                    case Motion.Rail:
                        count=5;p=Along((i+1)/6f,-30);size=Vector2.one*100;alpha*=Mathf.Clamp01(c*5-i);
                        Put(_accents[i],Along(q,-30),Vector2.one*65,live*f);Move(f,20*Mathf.Sin(f*Mathf.PI),after);break;
                    case Motion.Stones:
                        count=4;p=Along(q,Mathf.Abs(Mathf.Sin(q*Mathf.PI*2))*80);size=Vector2.one*70;break;
                    case Motion.Magnet:
                        count=2;p=_from+new Vector2(side*110,50);size=Vector2.one*125;
                        Put(_accents[i],Vector2.Lerp(_to,p,c),Vector2.one*70,live);break;
                    case Motion.OrePush:
                        count=1;p=_from;size=Vector2.one*170;
                        for(int j=0;j<6;j++)Put(_accents[j],Vector2.Lerp(_from+Ring(j*Mathf.PI/3+t*4,150,100),_to,f),Vector2.one*65,live);
                        break;
                    case Motion.Dust:
                        count=12;p=Along(q)+Ring(i*2.4f,80*q,100*q);size=Vector2.one*(30+30*q);angle=t*30+i*20;break;
                    case Motion.Rhythm:
                        count=4;p=_from+new Vector2(_sign*(30+i*55),Mathf.Sin(t*8-i)*25+60);
                        size=Vector2.one*70;alpha*=.6f+.4f*Mathf.Sin(t*8-i);break;
                    case Motion.SoundWave:
                        count=3;p=Along(q);size=Vector2.one*(100+q*100);angle=Mathf.Sin(t*8)*8;
                        Put(_accents[i],p+Vector2.up*50,Vector2.one*60,live);break;
                    case Motion.Reflect:
                        count=4;p=Along((i+1)/5f);size=new Vector2(110,65);angle=side*30;
                        Link(i,Along(i/5f),p,live*f,20);break;
                    case Motion.Vent:
                        count=4;p=_from+new Vector2(_sign*i*55,30+Mathf.Max(0,Mathf.Sin(t*7-i))*70);
                        size=new Vector2(75,130);alpha*=.5f+.5f*Mathf.Sin(t*7-i);break;
                    case Motion.StarChain:
                        count=5;p=Along((i+1)/6f,Mathf.Sin(i*1.5f)*45);size=Vector2.one*75;
                        alpha*=Mathf.Clamp01(c*5-i);
                        if(i>0)Link(i,Along(i/6f,Mathf.Sin((i-1)*1.5f)*45),p,alpha);break;
                    case Motion.Crater:
                        count=1;p=_from+Vector2.up*65;size=Vector2.one*(120+80*c);
                        for(int j=0;j<6;j++)Put(_accents[j],p+Ring(j*Mathf.PI/3+t,120*c,70*c),Vector2.one*45,live);break;
                    case Motion.Glyph:
                        count=8;p=Along(f*.8f)+Ring(i*Mathf.PI/4,120,95);size=Vector2.one*70;
                        alpha*=Mathf.Clamp01(c*8-i);angle=i*45;break;
                    case Motion.Mosaic:
                        count=9;p=Along(f)+new Vector2((i%3-1)*60,(i/3-1)*60)*c;
                        size=Vector2.one*80;angle=(1-c)*side*45;break;
                    case Motion.Fold:
                        count=2;p=_from+new Vector2(side*70*(1-c),65);size=new Vector2(100*(1-c)+25,130);
                        p=Vector2.Lerp(p,_to,f);angle=side*90*c;break;
                    case Motion.Gears:
                        count=2;p=Along(f)+new Vector2(side*65,0);size=Vector2.one*140;angle=side*t*100;break;
                    case Motion.Repair:
                        count=4;p=_from+Ring(i*Mathf.PI/2,130-80*c,100-50*c);p=Vector2.Lerp(p,_to,f);
                        size=Vector2.one*100;angle=(1-c)*side*35;break;
                    case Motion.Scroll:
                        count=1;p=Along(c*.4f,35);size=new Vector2(50+200*c,120);angle=10;
                        Link(0,_from,Along(c*.85f,35),live,20);break;
                    case Motion.Constellation:
                        count=6;p=Vector2.Lerp(_from+Ring(a,135,100),_to+Ring(a,70,55),f);
                        size=Vector2.one*(50+impact*30);Link(i,p,Vector2.Lerp(_from+Ring(a+Mathf.PI/3,135,100),_to+Ring(a+Mathf.PI/3,70,55),f),live);break;
                    case Motion.CrownTide:
                        count=1;p=_from+Vector2.up*70;size=Vector2.one*200;
                        for(int j=0;j<4;j++)Put(_accents[j],Along(Mathf.Clamp01(f*1.25f-j*.08f)),new Vector2(180,180+j*30),live);
                        break;
                    default:throw new ArgumentOutOfRangeException();
                }
                Put(_pieces[i],p,size,i<count?alpha:0,angle);
                if(i>=count)
                {
                    _links[i].color=Color.clear;
                    if(_motion!=Motion.GardenRing&&_motion!=Motion.OrePush&&_motion!=Motion.Crater&&_motion!=Motion.CrownTide)
                        _accents[i].color=Color.clear;
                }
                // Subtle local impact accent; never creates another damage event or covers the full screen.
                if(i==11)Put(_accents[i],_to,Vector2.one*(100+after*90),impact*.7f,after*30);
            }
        }
    }
}
