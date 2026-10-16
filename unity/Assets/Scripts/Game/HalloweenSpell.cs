using Numeria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>Six authored seasonal effects; the battle sequence remains the only damage owner.</summary>
    public sealed class HalloweenSpell
    {
        private readonly SkillVisualKind _kind;
        private readonly Vector2 _from,_to;
        private readonly float _scale;
        private readonly Image[] _pieces=new Image[8];
        private readonly SeasonalFamilySpell _family;
        public static bool Supports(SkillVisualKind kind) => kind>=SkillVisualKind.SeedVolley&&kind<=SkillVisualKind.SecretPassage;
        public HalloweenSpell(Transform root,SkillVisualKind kind,Vector2 from,Vector2 to,float scale,RectTransform caster=null,int stage=0)
        {
            if(kind>=SkillVisualKind.RindPounce)
            { _family=new SeasonalFamilySpell(root,kind,from,to,scale,caster,stage);return; }
            _kind=kind;_from=from;_to=to;_scale=scale;
            int art=kind==SkillVisualKind.SeedVolley?0:kind<=SkillVisualKind.HarvestCrown?1:kind==SkillVisualKind.GhostOrbit?2:3;
            for(int i=0;i<8;i++)
            {
                _pieces[i]=Ui.SpriteImg(root,"SeasonalMotif"+i,SpellChoreography.Material("halloween",art));
                _pieces[i].preserveAspect=true;_pieces[i].raycastTarget=false;
            }
        }
        public void Sample(float t)
        {
            if(_family!=null){_family.Sample(t);return;}
            float charge=Mathf.Clamp01(t/.65f),flight=Mathf.Clamp01((t-.65f)/(SpellSequence.ImpactTime-.65f));
            float fade=1-Mathf.Clamp01((t-SpellSequence.ImpactTime)/(SpellSequence.Duration-SpellSequence.ImpactTime));
            for(int i=0;i<8;i++)
            {
                float u=i/7f,a=u*Mathf.PI*2,side=i%2==0?-1:1;
                var point=Vector2.Lerp(_from,_to,flight);float size=50,alpha=charge*fade,rotation=0;
                switch(_kind)
                {
                    case SkillVisualKind.SeedVolley:
                        float f=Mathf.Clamp01(flight*1.4f-u*.4f);
                        point=Vector2.Lerp(_from+new Vector2((i%4-1.5f)*28,i/4*35),_to,f)+Vector2.up*Mathf.Sin(f*Mathf.PI)*(90+u*65);
                        size=30;rotation=f*180;break;
                    case SkillVisualKind.VineLantern:
                        point=Vector2.Lerp(_from,_to,u)+Vector2.up*(Mathf.Sin(u*Mathf.PI)*100+15);
                        alpha*=Mathf.Clamp01((flight-u*.8f)*6);size=45+charge*15;break;
                    case SkillVisualKind.HarvestCrown:
                        point=_to+new Vector2(Mathf.Cos(a)*125*(1-flight),150*(1-flight)+Mathf.Sin(a)*45);
                        size=55;rotation=Mathf.Sin(t*2+a)*15;break;
                    case SkillVisualKind.GhostOrbit:
                        point+=new Vector2(Mathf.Cos(a+t*5)*95,Mathf.Sin(a+t*5)*60)*(1-flight);
                        size=60;break;
                    case SkillVisualKind.PendulumArc:
                        float swing=Mathf.Sin(t*5-u*.5f)*1.1f;
                        point+=new Vector2(Mathf.Sin(swing)*150,-Mathf.Cos(swing)*80)*(1-flight);
                        size=i==0?110:40;alpha*=1-u*.75f;rotation=swing*Mathf.Rad2Deg;break;
                    case SkillVisualKind.MidnightChime:
                        point=_to+new Vector2(Mathf.Cos(a+t)*95,Mathf.Sin(a+t)*70)*(1-flight*.7f);
                        size=70+Mathf.Sin(t*10-u)*12;rotation=-t*30;break;
                }
                var image=_pieces[i];image.rectTransform.anchoredPosition=point;
                image.rectTransform.sizeDelta=Vector2.one*size*_scale;image.rectTransform.localRotation=Quaternion.Euler(0,0,rotation);
                image.color=new Color(1,1,1,alpha);
            }
        }
    }
}
