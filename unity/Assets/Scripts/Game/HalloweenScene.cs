using Numeria.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Numeria.Game
{
    /// <summary>Actual painted material tiles, blended paths, collision-aligned room walls and harvest props.</summary>
    public sealed class HalloweenScene : MonoBehaviour
    {
        private Texture2D _floor;
        private Sprite _floorSprite;
        private readonly Dictionary<string,SpriteRenderer> _restored=new Dictionary<string,SpriteRenderer>();
        private sealed class RestorationPart
        {
            public SpriteRenderer Sprite;
            public Vector3 Start,End,Scale;
            public float Delay;
            public int Style;
        }
        private readonly Dictionary<string,List<RestorationPart>> _restorationParts=new Dictionary<string,List<RestorationPart>>();
        private readonly Dictionary<string,Vector3> _restorationFocus=new Dictionary<string,Vector3>();
        public Vector3 RestorationFocus(string id)=>_restorationFocus.TryGetValue(id,out var p)?p:transform.position;
        private readonly List<SpriteRenderer> _foreground=new List<SpriteRenderer>();
        private SpriteRenderer _observer;
        public void Observe(Transform player) { _observer=player!=null?player.GetComponent<SpriteRenderer>():null; }
        private void LateUpdate()
        {
            if(_observer!=null)UpdateOcclusion(_observer.transform.position+Vector3.up*.45f,_observer.sortingOrder,Mathf.Clamp01(Time.deltaTime*12));
        }
        public void UpdateOcclusion(Vector3 player,int playerOrder,float blend)
        {
            foreach(var prop in _foreground)
            {
                var b=prop.bounds;
                bool hides=prop.sortingOrder>playerOrder&&player.x>=b.min.x&&player.x<=b.max.x&&player.y>=b.min.y&&player.y<=b.max.y;
                var color=prop.color;color.a=Mathf.Lerp(color.a,hides?.32f:1,blend);prop.color=color;
            }
        }
        private SpriteRenderer Add(Sprite sprite,Vector3 p,float height,int order,string name)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.localPosition=p;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sortingOrder=order;
            if(sprite!=null)go.transform.localScale=Vector3.one*(height/sprite.bounds.size.y);
            if(height>=2&&order>=0)_foreground.Add(sr);
            return sr;
        }
        public static void Build(Transform parent,GridMap map,MapDef def,Progress progress)
        {
            var root=new GameObject("PaintedHalloween");root.transform.SetParent(parent,false);
            var scene=root.AddComponent<HalloweenScene>();scene.Surface(map,def.Id);
            bool house=def.Id=="haunted_house";
            for(int y=0;y<map.Height;y++)for(int x=0;x<map.Width;x++)
            {
                var tile=map.At(x,y);var pos=new Vector3(x,map.Height-1-y,0);
                int order=PaintedTerrainRenderer.SortOrder(pos.y);
                if(tile==Tile.Tree&&(x%4==0||x==map.Width-2)&&(y%3==0||y==map.Height-2))
                    scene.Add(WorldScene.Prop(def.Id,0),pos+Vector3.down*.4f,3.4f,order,"LanternTree");
                if(!house&&tile==Tile.Bush&&x%2==0&&y%2==0)
                    scene.Add(WorldScene.Prop(def.Id,2),pos+new Vector3((x*7%3-1)*.18f,-.3f,0),1.25f+(x+y)%3*.18f,order,"EncounterGarden");
                // One complete bridge spans each crossing; avoid a grid of miniature bridges.
                if(tile==Tile.Bridge&&(x==0||map.At(x-1,y)!=Tile.Bridge)&&(y==0||map.At(x,y-1)!=Tile.Bridge))
                {
                    if(!house&&y==25)continue; // The earned crossing is a growing root bridge, not another timber bridge.
                    int length=1;while(map.InBounds(x+length,y)&&map.At(x+length,y)==Tile.Bridge)length++;
                    var bridge=scene.Add(WorldScene.BridgeSprite("forest"),pos+Vector3.right*(length-1)*.5f,2,-20,"TimberCrossing");
                    if(bridge.sprite!=null)bridge.transform.localScale=new Vector3(length/bridge.sprite.bounds.size.x,2/bridge.sprite.bounds.size.y,1);
                }
                // Bookshelves stand only on blocked wall cells adjacent to the north edge of rooms.
                if(house&&tile==Tile.Cliff&&x%7==0&&map.InBounds(x,y+1)&&map.At(x,y+1)!=Tile.Cliff)
                    scene.Add(WorldScene.Prop(def.Id,0),pos,2.3f,order,"LibraryWall");
            }
            void Prop(int index,float x,float y,float size,string name)
            {var p=new Vector3(x,map.Height-1-y,0);scene.Add(WorldScene.Prop(def.Id,index),p,size,PaintedTerrainRenderer.SortOrder(p.y),name);}
            void Detail(int index,float x,float y,float size,string name)
            {
                var p=new Vector3(x,map.Height-1-y,0);
                var sprite=SpriteLib.One("generated/Exploration/"+def.Id+"-detail-"+index);
                scene.Add(sprite,p+Vector3.up*size*.38f,size,PaintedTerrainRenderer.SortOrder(p.y),name);
            }
            if(!house)
            {
                Prop(1,5.5f,5,4.5f,"HarvestMill");Prop(3,28,4,3.7f,"HarvestShrine");
                Detail(0,20.5f,35,3.5f,"SafeHarvestMarket");Detail(1,4.5f,12,4.5f,"StrawBarn");
                Detail(2,4,22,2.6f,"ScarecrowHayloft");Detail(4,48,35,2.8f,"HarvestCart");
                foreach(int x in new[]{4,50})foreach(int y in new[]{5,15,23,33})Detail(3,x,y,1.5f,"FieldFence");
                Detail(5,43,4,3,"LanternOrchard");
            }
            else
            {
                Prop(1,25.5f,3,3,"MidnightClock");
                Detail(0,40,5,3.2f,"MusicRoomPiano");Detail(1,43,21,2.8f,"ToyRoomHorse");
                Detail(2,5,22,3,"MirrorGallery");Detail(3,26,21,3,"FoyerTeaTable");
                Detail(5,13.5f,5,2.8f,"PortraitLibrary");
                foreach(int x in new[]{6,17,34,46})Detail(4,x,3,3,"MoonWindow");
            }
            foreach(var d in def.Discoveries)
            {
                bool solved=progress.CollectedDiscoveries.Contains(d.Id);
                // Place clue ornaments above each interaction tile; never hide the playable marker.
                if(!house)Prop(2,d.X,d.Y-1,1.4f,"Clue-"+d.Id);
                if(solved)
                {
                    var glow=scene.Add(SpellSequence.Glow(),new Vector3(d.X,map.Height-1-d.Y,0),1.2f,-15,"RestoredLight");
                    glow.color=house?new Color(.3f,.8f,1,.35f):new Color(1,.75f,.2f,.35f);
                    scene._restored[d.Id]=glow;
                }
            }
            scene.BuildRestorations(map,def,progress);
        }
        private void BuildRestorations(GridMap map,MapDef def,Progress progress)
        {
            bool house=def.Id=="haunted_house";
            Vector3 At(float x,float y)=>new Vector3(x,map.Height-1-y,0);
            Sprite Art(string name)=>SpriteLib.One("generated/Exploration/"+name);
            for(int task=0;task<3;task++)
            {
                string id=def.Id+"-task-"+task;var parts=new List<RestorationPart>();_restorationParts[id]=parts;
                void Part(Sprite art,Vector3 start,Vector3 end,float height,int style,float delay,string name)
                {
                    var sprite=Add(art,end,height,PaintedTerrainRenderer.SortOrder(end.y)+1,name);
                    parts.Add(new RestorationPart{Sprite=sprite,Start=start,End=end,Scale=sprite.transform.localScale,Style=style,Delay=delay});
                }
                if(!house&&task==0)
                {
                    _restorationFocus[id]=At(26,24);
                    for(int i=0;i<8;i++)
                    {
                        var p=At(24+i,25);
                        Part(Art("depth-spell-rootter"),p,p,1.7f,0,i*.045f,"LivingBridgeRoot"+i);
                        Part(Art("depth-spell-verdant_vault-accent"),p+Vector3.down*.15f,p+Vector3.up*.3f,.7f,0,.3f+i*.045f,"BridgeLeaf"+i);
                        Part(Art("depth-spell-dewhopper"),At(22,20),p,.4f,3,i*.04f,"IrrigationDrop"+i);
                    }
                }
                else if(!house&&task==1)
                {
                    _restorationFocus[id]=At(8,15);
                    Part(Art(def.Id+"-detail-4"),At(10,16),At(7,13),2.1f,1,0,"HarvestDeliveryCart");
                }
                else if(!house)
                {
                    _restorationFocus[id]=At(40,15);
                    for(int i=0;i<5;i++)Part(SpriteLib.One("generated/Skills/pumpkin_lantern"),At(37+i*1.3f,14.5f),
                        At(37+i*1.3f,14.5f),.85f,2,i*.12f,"RestoredOrchardLantern"+i);
                }
                else if(task==0)
                {
                    _restorationFocus[id]=At(16,23);
                    Part(SpriteLib.LargeIcon("wispip"),At(9,26),At(18,22),1.35f,4,0,"WelcomingMirrorGuest");
                    Part(SpriteLib.LargeIcon("sheetot"),At(10,26),At(18,24),1.3f,4,.12f,"MirrorGuestFriend");
                }
                else if(task==1)
                {
                    _restorationFocus[id]=At(20,11);
                    Part(WorldScene.Prop(def.Id,0),At(21,11),At(23.2f,10),2.5f,1,.15f,"SlidingLibraryShelf");
                    Part(SpriteLib.LargeIcon("inklit"),At(18,10),At(18,10),.9f,5,0,"GreetingPortraitLeft");
                    Part(SpriteLib.LargeIcon("pagepeek"),At(19.5f,10),At(19.5f,10),.9f,5,.15f,"GreetingPortraitRight");
                }
                else
                {
                    _restorationFocus[id]=At(31,7);
                    for(int i=0;i<3;i++)Part(Art("ghost-spell-6"),At(29+i*1.3f,5.4f),At(29+i*1.3f,5.4f),
                        .85f,2,i*.15f,"MusicDoorBell"+i);
                }
                SampleRestoration(id,progress.CollectedDiscoveries.Contains(id)?1:0);
            }
        }
        /// <summary>Deterministic cosmetic state. Progress was persisted before this animation; reopening restores t=1.</summary>
        public void SampleRestoration(string id,float progress)
        {
            if(!_restorationParts.TryGetValue(id,out var parts))return;
            foreach(var part in parts)
            {
                float t=Mathf.Clamp01((progress-part.Delay)/(1-part.Delay)),ease=Mathf.SmoothStep(0,1,t);
                var sr=part.Sprite;sr.transform.localPosition=Vector3.Lerp(part.Start,part.End,ease);
                sr.transform.localScale=part.Scale;sr.transform.localRotation=Quaternion.identity;sr.color=Color.white;
                switch(part.Style)
                {
                    case 0:sr.transform.localScale=Vector3.Scale(part.Scale,new Vector3(Mathf.Max(.001f,ease),Mathf.Max(.001f,ease),1));
                        sr.color=new Color(1,1,1,ease);break;
                    case 1:sr.transform.localPosition+=Vector3.up*Mathf.Sin(t*Mathf.PI*6)*.06f;break;
                    case 2:sr.color=Color.Lerp(new Color(.24f,.23f,.3f),Color.white,ease);
                        sr.transform.localScale=part.Scale*(1+Mathf.Sin(t*Mathf.PI)*.15f);break;
                    case 3:sr.color=new Color(.5f,.85f,1,Mathf.Clamp01(Mathf.Sin(t*Mathf.PI)));break;
                    case 4:sr.color=new Color(1,1,1,ease);sr.transform.localPosition+=Vector3.up*Mathf.Sin(t*Mathf.PI)*.5f;break;
                    case 5:sr.transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*Mathf.PI*4)*12*(1-t));break;
                }
            }
        }
        public IEnumerator Reveal(string id)
        {
            if(!_restored.TryGetValue(id,out var glow))yield break;
            var size=glow.transform.localScale;var color=glow.color;
            for(float t=0;t<1;t+=Time.deltaTime/2.2f)
            {SampleRestoration(id,t);glow.transform.localScale=size*(1+Mathf.Sin(t*Mathf.PI)*2);var c=color;c.a=color.a+Mathf.Sin(t*Mathf.PI)*.4f;glow.color=c;yield return null;}
            SampleRestoration(id,1);glow.transform.localScale=size;glow.color=color;
        }
        private void Surface(GridMap map,string id)
        {
            var atlas=Resources.Load<Texture2D>("generated/Exploration/"+id+"_tiles");
            if(atlas==null)throw new System.InvalidOperationException("Missing Halloween terrain: "+id);
            var source=atlas.GetPixels32();int aw=atlas.width,ah=atlas.height,tw=aw/2,th=ah/2;
            const int density=32;int width=map.Width*density,height=map.Height*density;
            var result=new Color32[width*height];bool house=id=="haunted_house";
            Color Sample(int quadrant,float x,float y)
            {
                int sx=(int)(Mathf.Repeat(x/4,1)*tw),sy=(int)(Mathf.Repeat(y/4,1)*th);
                return source[((1-quadrant/2)*th+sy)*aw+(quadrant%2)*tw+sx];
            }
            bool Path(Tile t)=>t==Tile.Path||t==Tile.Portal||t==Tile.Bridge;
            for(int py=0;py<height;py++)for(int px=0;px<width;px++)
            {
                float x=px/(float)density-.5f,y=map.Height-.5f-py/(float)density;
                int cx=Mathf.Clamp(Mathf.RoundToInt(x),0,map.Width-1),cy=Mathf.Clamp(Mathf.RoundToInt(y),0,map.Height-1);
                Tile t=map.At(cx,cy);Color color=Sample(0,x,y);
                if(house)
                {
                    int q=t==Tile.Cliff?3:t==Tile.Bush||Path(t)?1:0;
                    color=Sample(q,x,y);
                    if(t==Tile.Cliff)
                    {
                        bool edge=map.InBounds(cx,cy+1)&&map.At(cx,cy+1)!=Tile.Cliff;
                        color*=edge?1f:.42f;
                    }
                }
                else
                {
                    float distance=2;
                    for(int yy=cy-1;yy<=cy+1;yy++)for(int xx=cx-1;xx<=cx+1;xx++)
                        if(map.InBounds(xx,yy)&&Path(map.At(xx,yy)))
                            distance=Mathf.Min(distance,new Vector2(Mathf.Max(0,Mathf.Abs(x-xx)-.45f),Mathf.Max(0,Mathf.Abs(y-yy)-.45f)).magnitude);
                    float blend=1-Mathf.SmoothStep(.05f,.38f,distance);
                    color=Color.Lerp(color,Sample(1,x,y),blend);
                    if(t==Tile.Water)
                    {
                        float bankDistance=2;
                        for(int yy=cy-1;yy<=cy+1;yy++)for(int xx=cx-1;xx<=cx+1;xx++)
                            if(map.InBounds(xx,yy)&&map.At(xx,yy)!=Tile.Water&&map.At(xx,yy)!=Tile.Bridge)
                                bankDistance=Mathf.Min(bankDistance,new Vector2(Mathf.Max(0,Mathf.Abs(x-xx)-.5f),Mathf.Max(0,Mathf.Abs(y-yy)-.5f)).magnitude);
                        color=Color.Lerp(Sample(0,x,y)*.72f,Sample(2,x,y),Mathf.SmoothStep(0,.20f,bankDistance));
                    }
                    if(t==Tile.Bush)
                    {
                        float furrow=.14f+.14f*Mathf.Cos(y*Mathf.PI);
                        color=Color.Lerp(color,new Color(.23f,.14f,.08f),furrow);
                    }
                    if(t==Tile.Tree)color=Color.Lerp(color,Sample(3,x,y),.65f);
                }
                color.a=1;result[py*width+px]=color;
            }
            _floor=new Texture2D(width,height,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear};
            _floor.SetPixels32(result);_floor.Apply(false,true);
            _floorSprite=Sprite.Create(_floor,new Rect(0,0,width,height),Vector2.one*.5f,density,0,SpriteMeshType.FullRect);
            Add(_floorSprite,new Vector3((map.Width-1)*.5f,(map.Height-1)*.5f,0),map.Height,-100,"PaintedTerrain");
        }
        private void OnDestroy()
        {
            if(Application.isPlaying){if(_floorSprite!=null)Destroy(_floorSprite);if(_floor!=null)Destroy(_floor);}
            else {if(_floorSprite!=null)DestroyImmediate(_floorSprite);if(_floor!=null)DestroyImmediate(_floor);}
        }
    }
}
