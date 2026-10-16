using System;
using System.Collections;
using Numeria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    public partial class MapController
    {
        private void BuildDepthEntrance()
        {
            var entrance=DepthJourney.Entrance(_def);if(!entrance.HasValue)return;
            var cell=entrance.Value;var world=TileWorld(cell.x,cell.y);
            var marker=AddSprite(MapArt.Prop(_def.Theme,"portal-glow",0),world,SortOrder(world.y)+18,"DepthPortal");
            ScaleSpriteToHeight(marker,1.5f);marker.color=new Color(.7f,.85f,1f);
            var labelObject=new GameObject("DepthDestination");labelObject.transform.SetParent(marker.transform.parent,false);
            labelObject.transform.position=world+Vector3.up*1.1f;
            var label=labelObject.AddComponent<TMPro.TextMeshPro>();
            label.font=Ui.DefaultTmpFont;
            label.text=_def.IsDepth||_def.IsHalloween?"RETURN":DepthCatalog.ForParent(_def.Id).Name;
            label.fontSize=3;label.alignment=TMPro.TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta=new Vector2(5,1);label.color=new Color(1,.93f,.65f);
            label.GetComponent<MeshRenderer>().sortingOrder=14000;
        }

        private IEnumerator DepthEntranceRoutine()
        {
            _busy=true;
            string target=_def.IsHalloween?"veilmoon_lake":_def.IsDepth?_def.ParentWorld:DepthCatalog.ForParent(_def.Id).Id;
            if(!_def.IsDepth&&!_def.IsHalloween&&!Maps.CanVisit(target,_progress))
            {
                yield return DialogueRoutine("HIDDEN PASSAGE","Defeat this world's guardian to open its hidden depths.",
                    SpriteLib.One("generated/Story/digit_crystal"));_busy=false;yield break;
            }
            bool? enter=null;
            var cover=Ui.Img(_hudCanvasRoot,"DepthTravel",new Color(0,0,0,.7f));Ui.Stretch(cover.rectTransform);
            var panel=Ui.Img(cover.transform,"Panel",Ui.PlateBg);
            Ui.PlaceCentered(panel.rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(820,310));Ui.AddOutline(panel.gameObject);
            var title=Ui.Label(panel.transform,"Destination","TRAVEL TO "+Maps.Get(target).DisplayName.ToUpperInvariant()+"?",31,Ui.Ink);
            Ui.Place(title.rectTransform,new Vector2(.5f,1),new Vector2(0,-40),new Vector2(760,100));
            var yes=Ui.Btn(panel.transform,"Enter","ENTER",26);Ui.Place((RectTransform)yes.transform,new Vector2(.5f,0),new Vector2(-175,45),new Vector2(280,70));
            var no=Ui.Btn(panel.transform,"Stay","STAY HERE",26);Ui.Place((RectTransform)no.transform,new Vector2(.5f,0),new Vector2(175,45),new Vector2(280,70));
            yes.onClick.AddListener(()=>enter=true);no.onClick.AddListener(()=>enter=false);
            yield return new WaitUntil(()=>enter.HasValue);Destroy(cover.gameObject);
            if(enter==true){_progress.CurrentMap=target;SaveSystem.Save(_progress);Respawn(_progress);}else _busy=false;
        }

        private IEnumerator DepthMechanismRoutine(DiscoveryDef discovery)
        {
            _busy=true;
            int kind=Array.FindIndex(DepthCatalog.Worlds,w=>w.Id==_def.Id);
            int stage=Array.IndexOf(_def.Discoveries,discovery);
            var puzzle=new DepthMechanism(kind,stage);bool cancel=false;
            _voice.Say(puzzle.Narration());
            var cover=Ui.Img(_hudCanvasRoot,"DepthMechanism",new Color(.02f,.04f,.06f,.82f));Ui.Stretch(cover.rectTransform);
            var panel=Ui.Img(cover.transform,"Challenge",Ui.PlateBg);
            Ui.PlaceCentered(panel.rectTransform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(1060,790));Ui.AddOutline(panel.gameObject);
            var title=Ui.Label(panel.transform,"Title",discovery.Name.ToUpperInvariant(),34,Ui.Ink);
            Ui.Place(title.rectTransform,new Vector2(.5f,1),new Vector2(-35,-20),new Vector2(820,55));
            var replay=QuestionReplay.Add(panel.transform,lines=>_voice.Replay(lines),puzzle.Narration);
            Ui.Place(replay.GetComponent<RectTransform>(),new Vector2(1,1),new Vector2(-22,-12),new Vector2(88,64));
            var instructions=Ui.Label(panel.transform,"Instructions",puzzle.Instruction,27,Ui.Ink);
            instructions.textWrappingMode=TMPro.TextWrappingModes.Normal;
            Ui.Place(instructions.rectTransform,new Vector2(.5f,1),new Vector2(0,-83),new Vector2(960,85));
            var hint=Ui.Label(panel.transform,"Hint",puzzle.Hint,23,Ui.Ink);
            Ui.Place(hint.rectTransform,new Vector2(.5f,0),new Vector2(0,122),new Vector2(960,75));
            var buttons=new Button[9];var symbols=new DepthMechanismGraphic[9];
            void Refresh()
            {
                replay.interactable=!puzzle.Solved;
                hint.text=puzzle.Hint;
                for(int j=0;j<9;j++)
                {
                    string label=puzzle.Label(j);buttons[j].gameObject.SetActive(label.Length>0);
                    buttons[j].GetComponentInChildren<TMPro.TMP_Text>().text=label;
                    bool storm=kind==2&&puzzle.Cells[j]==1;
                    buttons[j].targetGraphic.color=storm?Ui.Hex("#9eafb2"):
                        (kind==3&&puzzle.Cells[j]==1)||(kind==4&&puzzle.Cells[j]==1)?Ui.Hex("#f4dfa3"):Ui.PlateBg;
                    buttons[j].interactable=kind!=7||j%3==2;
                    symbols[j].Kind=kind;symbols[j].Active=kind==2&&j==puzzle.Position;
                    symbols[j].Value=kind==2&&j==8?2:kind==7&&j%3==1?-1:puzzle.Cells[j];
                    symbols[j].SetVerticesDirty();
                }
            }
            for(int i=0;i<9;i++)
            {
                int cell=i;buttons[i]=Ui.Btn(panel.transform,"Cell"+i,puzzle.Label(i),25);
                bool singleRow=kind==0||kind==1||kind==5;
                Ui.PlaceCentered((RectTransform)buttons[i].transform,new Vector2(.5f,.5f),
                    new Vector2((i%3-1)*(singleRow?295:230),singleRow?10:105-i/3*112),singleRow?new Vector2(270,220):new Vector2(212,100));
                buttons[i].transition=Selectable.Transition.None;
                var text=buttons[i].GetComponentInChildren<TMPro.TMP_Text>();
                Ui.PlaceCentered(text.rectTransform,new Vector2(.5f,0),new Vector2(0,singleRow?38:17),new Vector2(205,singleRow?62:30));
                var icon=Ui.Node(buttons[i].transform,"Symbol");symbols[i]=icon.gameObject.AddComponent<DepthMechanismGraphic>();symbols[i].raycastTarget=false;
                Ui.PlaceCentered(icon,new Vector2(.5f,.5f),new Vector2(0,singleRow?20:12),singleRow?new Vector2(128,128):new Vector2(54,54));
                buttons[i].onClick.AddListener(()=>{puzzle.Tap(cell);Refresh();});
            }
            var reset=Ui.Btn(panel.transform,"Reset","RESET",25);Ui.Place((RectTransform)reset.transform,new Vector2(.5f,0),new Vector2(-165,30),new Vector2(280,65));
            reset.onClick.AddListener(()=>{puzzle.Reset();Refresh();});
            var back=Ui.Btn(panel.transform,"Back","COME BACK LATER",23);Ui.Place((RectTransform)back.transform,new Vector2(.5f,0),new Vector2(165,30),new Vector2(280,65));
            back.onClick.AddListener(()=>cancel=true);Refresh();
            yield return new WaitUntil(()=>cancel||puzzle.Solved);replay.interactable=false;_voice.Stop();Destroy(cover.gameObject);
            if(puzzle.Solved&&_progress.CollectDiscovery(discovery.Id))
            {
                _progress.AddCoins(discovery.Coins);_progress.RecordPuzzle(true);
                DepthJourney.Apply(_map,_def,_progress);SaveSystem.Save(_progress);
                if(_discoveryRenderers.TryGetValue(discovery.Id,out var marker))marker.gameObject.SetActive(false);
                Destroy(_worldScene.gameObject);_worldScene=WorldScene.Build(_mapRoot.transform,_map,_def,_progress);
                RefreshPortalState();UpdateHud();Sfx.Play(SfxCue.Chest,.8f);
                yield return _worldScene.Reveal(_progress,discovery.Id);
            }
            _busy=false;
        }
    }
}
