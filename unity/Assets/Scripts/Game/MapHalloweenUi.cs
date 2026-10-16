using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Numeria.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Numeria.Game
{
    public partial class MapController
    {
        private IEnumerator HalloweenMechanismRoutine(DiscoveryDef discovery)
        {
            _busy=true;
            int index=Array.FindIndex(_def.Discoveries,d=>d.Id==discovery.Id);
            if(index<0)throw new ArgumentException("Discovery does not belong to this world.",nameof(discovery));
            int kind=index+(_def.Id=="haunted_house"?3:0);
            var puzzle=new HalloweenMechanism(kind,_progress.Learning,_rng);
            bool cancel=false,playing=false;
            var cover=Ui.Img(_hudCanvasRoot,"HalloweenMechanism",new Color(.03f,.02f,.08f,.88f));Ui.Stretch(cover.rectTransform);
            var panel=Ui.Img(cover.transform,"Challenge",Ui.PlateBg);
            Ui.PlaceCentered(panel.rectTransform,Vector2.one*.5f,Vector2.zero,new Vector2(1100,790));Ui.AddOutline(panel.gameObject);
            var title=Ui.Label(panel.transform,"Title",discovery.Name.ToUpperInvariant(),32,Ui.Ink);
            Ui.Place(title.rectTransform,new Vector2(.5f,1),new Vector2(-40,-20),new Vector2(850,50));
            var instruction=Ui.Label(panel.transform,"Instruction",puzzle.Instruction,26,Ui.Ink);
            instruction.textWrappingMode=TMPro.TextWrappingModes.Normal;
            Ui.Place(instruction.rectTransform,new Vector2(.5f,1),new Vector2(0,-82),new Vector2(1000,84));
            var hint=Ui.Label(panel.transform,"Hint",puzzle.Hint,22,Ui.Ink);
            hint.textWrappingMode=TMPro.TextWrappingModes.Normal;
            Ui.Place(hint.rectTransform,new Vector2(.5f,0),new Vector2(0,102),new Vector2(1000,66));
            var buttons=new Dictionary<int,Button>();
            var glyphs=new Dictionary<int,DepthMechanismGraphic>();
            DepthMechanismGraphic harvestPile=null;
            var audio=cover.gameObject.AddComponent<AudioSource>();audio.volume=.3f;
            var tones=new AudioClip[3];
            cover.gameObject.AddComponent<HalloweenPuzzleAudio>().Clips=tones;
            for(int i=0;i<3;i++)
            {
                const int rate=22050;var samples=new float[rate/3];float hz=new[]{440f,554.37f,659.25f}[i];
                for(int s=0;s<samples.Length;s++){float t=s/(float)rate;samples[s]=Mathf.Sin(2*Mathf.PI*hz*t)*Mathf.Exp(-t*10)*Mathf.Min(1,t*100);}
                tones[i]=AudioClip.Create("LearningBell"+i,samples.Length,1,rate,false);tones[i].SetData(samples,0);
            }
            DepthMechanismGraphic Glyph(Transform parent,string name,int glyphKind,int value,Vector2 position,Vector2 size)
            {
                var node=Ui.Node(parent,name);Ui.PlaceCentered(node,Vector2.one*.5f,position,size);
                var glyph=node.gameObject.AddComponent<DepthMechanismGraphic>();glyph.Kind=glyphKind;glyph.Value=value;glyph.raycastTarget=false;return glyph;
            }
            Button Cell(int index,Vector2 position,Vector2 size,int glyphKind,string label="")
            {
                var button=Ui.Btn(panel.transform,"Cell"+index,label,23);
                Ui.PlaceCentered((RectTransform)button.transform,Vector2.one*.5f,position,size);
                button.transition=Selectable.Transition.None;buttons[index]=button;
                glyphs[index]=Glyph(button.transform,"Picture",glyphKind,0,Vector2.zero,size*.72f);
                if(label.Length>0)Ui.PlaceCentered(button.GetComponentInChildren<TMPro.TMP_Text>().rectTransform,
                    new Vector2(.5f,0),new Vector2(0,18),new Vector2(size.x-8,30));
                button.onClick.AddListener(()=>{if(playing)return;puzzle.Tap(index);if(kind==5)audio.PlayOneShot(tones[index]);Refresh();});
                return button;
            }
            void Refresh()
            {
                if(puzzle.Solved)QuestionReplay.Disable(panel.transform);
                if(harvestPile!=null){harvestPile.Value=puzzle.Remaining;harvestPile.SetVerticesDirty();}
                hint.text=puzzle.Hint+(kind==1?"   "+puzzle.Remaining+" pumpkins left.":kind==5?"   "+puzzle.Step+" / "+puzzle.Melody.Length:"");
                foreach(var item in buttons)
                {
                    int i=item.Key;var button=item.Value;var glyph=glyphs[i];
                    button.interactable=!playing&&!puzzle.Solved&&(kind!=3||i%4>=2)&&(kind!=0||puzzle.Cells[i]!=0);
                    button.targetGraphic.color=kind==0&&puzzle.Cells[i]==0?Ui.Hex("#c7c4a3"):
                        kind==4&&puzzle.Selected==i?Ui.Hex("#f1c45e"):Ui.PlateBg;
                    glyph.Value=kind==2?i-12:kind==5?i:puzzle.Cells[i];
                    glyph.Active=kind==0&&puzzle.Wet[i];glyph.SetVerticesDirty();
                    if(kind==1||kind==4)button.GetComponentInChildren<TMPro.TMP_Text>().text=kind==1?puzzle.BasketContentsLabel(i):puzzle.Cells[i].ToString();
                    if(kind==2&&puzzle.NumberPattern)button.GetComponentInChildren<TMPro.TMP_Text>().text=puzzle.PatternLabel(puzzle.PatternChoices[i-12]);
                }
            }
            if(kind==0||kind==3)
            {
                for(int i=0;i<16;i++)Cell(i,new Vector2((i%4-1.5f)*100,145-i/4*100),new Vector2(94,94),kind==0?8:9);
                if(kind==0)
                {
                    var spring=Ui.Label(panel.transform,"Spring","SPRING",22,Ui.Ink);
                    Ui.PlaceCentered(spring.rectTransform,Vector2.one*.5f,new Vector2(-315,95),new Vector2(140,50));
                    Glyph(panel.transform,"WaterSource",14,0,new Vector2(-315,160),new Vector2(84,84));
                    Glyph(panel.transform,"WaterInlet",0,1,new Vector2(-238,145),new Vector2(38,38));
                    foreach(int y in new[]{145,-155})
                    {
                        var seed=Ui.Label(panel.transform,"Seed","SEED",22,Ui.Ink);Ui.PlaceCentered(seed.rectTransform,Vector2.one*.5f,new Vector2(295,y-50),new Vector2(130,50));
                        Glyph(panel.transform,"WaterOutlet",0,1,new Vector2(238,y),new Vector2(38,38));
                        var art=Ui.SpriteImg(panel.transform,"GrowingSeed",SpriteLib.One("generated/Exploration/pumpkin-spell-3"));art.preserveAspect=true;
                        Ui.PlaceCentered(art.rectTransform,Vector2.one*.5f,new Vector2(295,y+15),new Vector2(84,84));
                    }
                }
                else
                {
                    var axis=Ui.Img(panel.transform,"MirrorLine",Ui.Hex("#67a8b0"));
                    Ui.PlaceCentered(axis.rectTransform,Vector2.one*.5f,new Vector2(0,-5),new Vector2(4,400));
                    var label=Ui.Label(panel.transform,"MirrorLabels","ORIGINAL                    YOUR REFLECTION",20,Ui.Ink);
                    Ui.PlaceCentered(label.rectTransform,Vector2.one*.5f,new Vector2(0,218),new Vector2(620,40));
                    if(puzzle.CoordinateMirror)
                    {
                        for(int n=0;n<4;n++)
                        {
                            var x=Ui.Label(panel.transform,"CoordinateX"+n,(n+1).ToString(),22,Ui.Ink);
                            Ui.PlaceCentered(x.rectTransform,Vector2.one*.5f,new Vector2((n-1.5f)*100,-220),new Vector2(50,40));
                            var y=Ui.Label(panel.transform,"CoordinateY"+n,(4-n).ToString(),22,Ui.Ink);
                            Ui.PlaceCentered(y.rectTransform,Vector2.one*.5f,new Vector2(-240,145-n*100),new Vector2(50,40));
                        }
                        var xAxis=Ui.Label(panel.transform,"CoordinateXAxis","x",22,Ui.Ink);
                        Ui.PlaceCentered(xAxis.rectTransform,Vector2.one*.5f,new Vector2(230,-220),new Vector2(40,40));
                        var yAxis=Ui.Label(panel.transform,"CoordinateYAxis","y",22,Ui.Ink);
                        Ui.PlaceCentered(yAxis.rectTransform,Vector2.one*.5f,new Vector2(-240,210),new Vector2(40,40));
                    }
                }
            }
            else if(kind==2)
            {
                for(int i=0;i<8;i++)
                {
                    var pos=new Vector2((i-3.5f)*100,90);
                    if(i==puzzle.PatternGap)
                    {var gap=Ui.Label(panel.transform,"Gap","?",48,Ui.Ink);Ui.PlaceCentered(gap.rectTransform,Vector2.one*.5f,pos,new Vector2(85,85));}
                    else if(puzzle.NumberPattern)
                    {
                        var value=Ui.Label(panel.transform,"Pattern"+i,puzzle.PatternLabel(puzzle.Initial[i]),32,Ui.Ink);
                        Ui.PlaceCentered(value.rectTransform,Vector2.one*.5f,pos,new Vector2(96,80));
                        var lantern=Ui.SpriteImg(panel.transform,"NumberLantern"+i,SpriteLib.One("generated/Exploration/pumpkin-spell-5"));
                        lantern.preserveAspect=true;Ui.PlaceCentered(lantern.rectTransform,Vector2.one*.5f,pos+Vector2.up*70,new Vector2(50,55));
                    }
                    else Glyph(panel.transform,"Pattern"+i,11,puzzle.Initial[i],pos,new Vector2(82,82));
                }
                for(int i=0;i<3;i++)
                {
                    var button=Cell(12+i,new Vector2((i-1)*230,-115),new Vector2(180,155),11);
                    if(puzzle.NumberPattern){glyphs[12+i].gameObject.SetActive(false);button.GetComponentInChildren<TMPro.TMP_Text>().fontSize=40;}
                }
            }
            else
            {
                int count=kind==5?3:puzzle.Count;
                for(int i=0;i<count;i++)Cell(i,new Vector2((i-(count-1)*.5f)*(kind==4?190:285),puzzle.FractionShares||puzzle.WeightShares?-60:0),
                    new Vector2(kind==4?175:250,235),kind==5?1:kind==4?12:10,kind==5?"BELL "+(i+1):"0");
            }
            if(kind==1)
            {
                if(puzzle.FractionShares||puzzle.WeightShares)
                {
                    for(int i=0;i<puzzle.Count;i++)
                    {
                        var target=Ui.Label(panel.transform,"BasketTarget"+i,"BASKET "+(i+1)+"\n"+puzzle.BasketTargetLabel(i),23,Ui.Ink);
                        Ui.PlaceCentered(target.rectTransform,Vector2.one*.5f,new Vector2((i-(puzzle.Count-1)*.5f)*285,85),new Vector2(250,48));
                    }
                    var unit=Ui.Label(panel.transform,"HarvestUnits",puzzle.HarvestLabel,23,Ui.Ink);
                    Ui.PlaceCentered(unit.rectTransform,Vector2.one*.5f,new Vector2(0,200),new Vector2(850,34));
                }
                var source=Ui.Img(panel.transform,"HarvestPile",Color.clear);
                Ui.PlaceCentered(source.rectTransform,Vector2.one*.5f,new Vector2(0,puzzle.FractionShares||puzzle.WeightShares?150:180),new Vector2(210,70));
                harvestPile=Glyph(source.transform,"UnsharedPumpkins",13,puzzle.Remaining,Vector2.zero,new Vector2(160,puzzle.FractionShares||puzzle.WeightShares?70:110));
                var drag=source.gameObject.AddComponent<HarvestDragSource>();
                drag.Drop=data=> {
                    if(playing||puzzle.Solved||puzzle.Remaining<=0)return;
                    foreach(var entry in buttons)
                        if(RectTransformUtility.RectangleContainsScreenPoint((RectTransform)entry.Value.transform,data.position,data.pressEventCamera))
                        {puzzle.Tap(entry.Key);Refresh();break;}
                };
            }
            Coroutine melody=null;
            IEnumerator PlayMelody(bool manual)
            {
                playing=true;Refresh();
                if(manual||Voice.Enabled)
                {
                    _voice.Replay(puzzle.Narration());
                    float duration=puzzle.Narration().Sum(line=>(Resources.Load<AudioClip>("Voice/"+VoiceKeys.Sanitize(line))?.length??0)+.1f);
                    yield return new WaitForSeconds(duration);
                }
                foreach(int note in puzzle.Melody)
                {
                    buttons[note].targetGraphic.color=Ui.Hex("#ffd66b");
                    if(manual||Sfx.Enabled)audio.PlayOneShot(tones[note]);
                    yield return new WaitForSeconds(.55f);
                    buttons[note].targetGraphic.color=Ui.PlateBg;
                    yield return new WaitForSeconds(.18f);
                }
                playing=false;Refresh();melody=null;
            }
            void Replay(string[] lines)
            {
                if(kind!=5){_voice.Replay(lines);return;}
                if(melody!=null)StopCoroutine(melody);audio.Stop();_voice.Stop();
                melody=StartCoroutine(PlayMelody(true));
            }
            var replay=QuestionReplay.Add(panel.transform,Replay,puzzle.Narration);
            Ui.Place(replay.GetComponent<RectTransform>(),new Vector2(1,1),new Vector2(-24,-15),new Vector2(88,64));
            var undo=Ui.Btn(panel.transform,"Undo","UNDO",23);
            Ui.Place((RectTransform)undo.transform,new Vector2(.5f,0),new Vector2(-310,25),new Vector2(190,64));
            undo.onClick.AddListener(()=>{if(!playing){puzzle.Undo();Refresh();}});
            var reset=Ui.Btn(panel.transform,"Reset","RESET",23);
            Ui.Place((RectTransform)reset.transform,new Vector2(.5f,0),new Vector2(-70,25),new Vector2(190,64));
            reset.onClick.AddListener(()=>{if(!playing){puzzle.Reset();Refresh();}});
            var back=Ui.Btn(panel.transform,"Back","COME BACK LATER",23);
            Ui.Place((RectTransform)back.transform,new Vector2(.5f,0),new Vector2(235,25),new Vector2(325,64));
            back.onClick.AddListener(()=>cancel=true);
            Refresh();
            if(kind==5)melody=StartCoroutine(PlayMelody(false));else _voice.Say(puzzle.Narration());
            yield return new WaitUntil(()=>cancel||puzzle.Solved);
            if(melody!=null)StopCoroutine(melody);
            _voice.Stop();audio.Stop();Destroy(cover.gameObject);
            if(puzzle.Solved&&_progress.CollectDiscovery(discovery.Id))
            {
                _progress.RecordPuzzle(true);_progress.AddCoins(discovery.Coins);
                HalloweenWorlds.Apply(_map,_def,_progress);SaveSystem.Save(_progress);
                if(_discoveryRenderers.TryGetValue(discovery.Id,out var marker))marker.gameObject.SetActive(false);
                Destroy(_worldScene.gameObject);_worldScene=WorldScene.Build(_mapRoot.transform,_map,_def,_progress);
                RefreshPortalState();UpdateHud();Sfx.Play(SfxCue.Correct);
                var restored=_worldScene.GetComponentInChildren<HalloweenScene>();
                restored?.Observe(_avatar);
                if(restored!=null)
                {
                    Vector3 previous=_worldCamera.transform.position,focus=restored.RestorationFocus(discovery.Id);
                    _worldCamera.transform.position=new Vector3(focus.x,focus.y,previous.z);
                    try{yield return restored.Reveal(discovery.Id);}
                    finally{if(_worldCamera!=null)_worldCamera.transform.position=previous;}
                }
            }
            _busy=false;
        }
    }

    public sealed class HarvestDragSource : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        public Action<PointerEventData> Drop;
        private RectTransform _preview;
        public void OnBeginDrag(PointerEventData data)
        {
            _preview=Ui.Node(transform.parent,"DraggedPumpkin");_preview.sizeDelta=new Vector2(70,70);
            var picture=_preview.gameObject.AddComponent<DepthMechanismGraphic>();picture.Kind=13;picture.Value=1;picture.raycastTarget=false;
            OnDrag(data);
        }
        public void OnDrag(PointerEventData data)
        {
            if(_preview!=null&&RectTransformUtility.ScreenPointToWorldPointInRectangle((RectTransform)transform.parent,data.position,data.pressEventCamera,out var world))_preview.position=world;
        }
        public void OnEndDrag(PointerEventData data){Clear();Drop?.Invoke(data);}
        private void OnDisable(){Clear();}
        private void Clear(){if(_preview!=null){if(Application.isPlaying)Destroy(_preview.gameObject);else DestroyImmediate(_preview.gameObject);_preview=null;}}
    }

    // Owned by the overlay, so leaving the map also releases generated bell clips.
    [ExecuteAlways]
    public sealed class HalloweenPuzzleAudio : MonoBehaviour
    {
        public AudioClip[] Clips;
        private void OnDestroy()
        {
            if(Clips==null)return;
            foreach(var clip in Clips)if(clip!=null)
            {if(Application.isPlaying)Destroy(clip);else DestroyImmediate(clip);}
        }
    }
}
