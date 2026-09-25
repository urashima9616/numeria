using System;
using System.Linq;
using Numeria.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    public partial class MenuUi
    {
        private int _dexPage,_dexFilter;
        private string _dexSelected="addmander";
        private const int DexPageSize=16;
        private void ShowCollection()
        {
            SelectTab("collection");var wrap=FreshWrap();
            var header=Ui.Label(wrap,"DexProgress",$"MATHMON FIELD GUIDE   {_progress.CollectionCount} / {GameData.Roster.Count}",30,TitleGreen,TextAnchor.MiddleLeft);
            Ui.Place(header.rectTransform,new Vector2(0,1),new Vector2(8,-8),new Vector2(750,48));
            string[] filters={"ALL","COLLECTED","MISSING"};
            for(int i=0;i<3;i++)
            {
                int filter=i;var button=Ui.Btn(wrap,"DexFilter"+i,filters[i],21);
                Ui.Place((RectTransform)button.transform,new Vector2(0,1),new Vector2(8+i*215,-66),new Vector2(205,44));
                button.targetGraphic.color=_dexFilter==i?TabActive:CreamDeep;
                button.onClick.AddListener(()=>{_dexFilter=filter;_dexPage=0;ShowCollection();});
            }
            var entries=GameData.Roster.Select(s=>CollectionBook.Entry(_progress,s.Id))
                .Where(e=>_dexFilter==0||e.Collected==(_dexFilter==1)).ToArray();
            int pages=Math.Max(1,(entries.Length+DexPageSize-1)/DexPageSize);_dexPage=Mathf.Clamp(_dexPage,0,pages-1);
            var page=entries.Skip(_dexPage*DexPageSize).Take(DexPageSize).ToArray();
            if(!page.Any(e=>e.Id==_dexSelected))_dexSelected=page.FirstOrDefault()?.Id;
            for(int i=0;i<page.Length;i++)
            {
                var entry=page[i];var tile=Ui.Img(wrap,"Dex-"+entry.Id,entry.Id==_dexSelected?CardActive:CreamDeep);Ui.AddOutline(tile.gameObject);
                Ui.Place(tile.rectTransform,new Vector2(0,1),new Vector2(8+i%4*162,-125-i/4*110),new Vector2(152,100));
                var number=Ui.Label(tile.transform,"Number",$"#{entry.Number:000}",16,SummaryOrange,TextAnchor.MiddleLeft);
                Ui.Place(number.rectTransform,new Vector2(0,1),new Vector2(8,-4),new Vector2(55,22));
                if(entry.Collected)
                {var portrait=Ui.SpriteImg(tile.transform,"Portrait",SpriteLib.EnemyBattleSprite(entry.PortraitId));portrait.preserveAspect=true;
                 Ui.PlaceCentered(portrait.rectTransform,new Vector2(.5f,.59f),Vector2.zero,new Vector2(72,63));}
                else {var unknown=Ui.Label(tile.transform,"Unknown","?",44,TitleGreen);Ui.PlaceCentered(unknown.rectTransform,new Vector2(.5f,.57f),Vector2.zero,new Vector2(70,62));}
                var name=Ui.Label(tile.transform,"DexName",entry.Name,19,TitleGreen);name.enableAutoSizing=true;name.fontSizeMin=14;name.fontSizeMax=19;
                Ui.Place(name.rectTransform,new Vector2(.5f,0),new Vector2(0,4),new Vector2(145,28));
                tile.gameObject.AddComponent<Button>().onClick.AddListener(()=>{_dexSelected=entry.Id;ShowCollection();});
            }
            void PageButton(string name,string label,int delta,float x)
            {var b=Ui.Btn(wrap,name,label,23);Ui.Place((RectTransform)b.transform,new Vector2(0,0),new Vector2(x,4),new Vector2(146,46));
             b.interactable=_dexPage+delta>=0&&_dexPage+delta<pages;b.onClick.AddListener(()=>{_dexPage+=delta;ShowCollection();});}
            PageButton("DexPrev","PREVIOUS",-1,8);PageButton("DexNext","NEXT",1,505);
            var pageLabel=Ui.Label(wrap,"DexPage",$"PAGE {_dexPage+1} / {pages}",22,TitleGreen);
            Ui.Place(pageLabel.rectTransform,new Vector2(0,0),new Vector2(218,4),new Vector2(225,46));
            var selected=page.FirstOrDefault(e=>e.Id==_dexSelected);if(selected==null)return;
            var panel=Ui.Img(wrap,"DexDetail",CreamDeep);Ui.AddOutline(panel.gameObject);
            Ui.Place(panel.rectTransform,new Vector2(1,1),new Vector2(-4,-64),new Vector2(456,508));
            var title=Ui.Label(panel.transform,"DexTitle",$"#{selected.Number:000}   {selected.Name}",30,TitleGreen);
            title.enableAutoSizing=true;title.fontSizeMin=22;title.fontSizeMax=30;
            Ui.Place(title.rectTransform,new Vector2(.5f,1),new Vector2(0,-14),new Vector2(426,48));
            if(selected.Collected)
            {var art=Ui.SpriteImg(panel.transform,"DexArt",SpriteLib.EnemyBattleSprite(selected.PortraitId));art.preserveAspect=true;
             Ui.PlaceCentered(art.rectTransform,new Vector2(.5f,.62f),Vector2.zero,new Vector2(240,220));}
            else {var mark=Ui.Label(panel.transform,"Unknown","?",110,TitleGreen);Ui.PlaceCentered(mark.rectTransform,new Vector2(.5f,.62f),Vector2.zero,new Vector2(200,220));}
            string text=selected.Collected?$"{selected.Element}\n\n{selected.Description}":"An undiscovered friend.\n\nCatch or evolve this Mathmon to reveal its picture and story.";
            var description=Ui.Label(panel.transform,"DexDescription",text,23,TitleGreen,TextAnchor.UpperLeft);
            description.textWrappingMode=TextWrappingModes.Normal;description.enableAutoSizing=true;description.fontSizeMin=18;description.fontSizeMax=23;
            Ui.Place(description.rectTransform,new Vector2(.5f,0),new Vector2(0,22),new Vector2(410,175));
            if(selected.Collected && !_progress.PendingGiants.Contains(selected.Id))
            {
                var listen=Ui.Btn(wrap,"DexListen","LISTEN TO ITS STORY",22);
                Ui.Place((RectTransform)listen.transform,new Vector2(1,0),new Vector2(-4,4),new Vector2(456,46));
                listen.onClick.AddListener(()=>
                {
                    var voice=_overlay.GetComponent<Voice>()??_overlay.gameObject.AddComponent<Voice>();
                    voice.Say(CollectionBook.Narration(selected.Id));
                });
            }
            if(_progress.PendingGiants.Contains(selected.Id))
            {
                var claim=Ui.Btn(wrap,"ClaimGiant",_progress.TeamIsFull?"MAKE ROOM TO CLAIM":"CLAIM GIANT FRIEND",22);
                Ui.Place((RectTransform)claim.transform,new Vector2(1,0),new Vector2(-4,4),new Vector2(456,46));claim.interactable=!_progress.TeamIsFull;
                claim.onClick.AddListener(()=>{if(_progress.ClaimGiant(selected.Id))SaveSystem.Save(_progress);ShowCollection();});
            }
        }
    }
}
