using System;
using Numeria.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    public sealed class ExplorationHud
    {
        private readonly TMP_Text _name, _coins, _objective, _region;
        private readonly Image _portrait;
        private readonly RectTransform _root;
        private readonly Image _xp;
        private static Color Paper => Ui.Hex("#faf1d6");
        private static Color Forest => Ui.Hex("#203d36ee");

        public ExplorationHud(RectTransform root, Action menu, Action repeat, Action world)
        {
            _root = root;
            var buddy = Ui.Img(root, "BuddyCard", Forest);
            Ui.Place(buddy.rectTransform, new Vector2(0, 1), new Vector2(24, -24), new Vector2(285, 92));
            var button = buddy.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => menu());
            _portrait = Ui.SpriteImg(buddy.transform, "BuddyPortrait", null);
            _portrait.preserveAspect = true;
            Ui.Place(_portrait.rectTransform, new Vector2(0, .5f), new Vector2(12, 0), new Vector2(70, 70));
            _name = Ui.Label(buddy.transform, "BuddyName", "", 27, Paper, TextAnchor.MiddleLeft);
            Ui.Place(_name.rectTransform, new Vector2(0, 1), new Vector2(94, -16), new Vector2(185, 40));
            var track = Ui.Img(buddy.transform, "GrowthTrack", Ui.Hex("#496257"));
            Ui.Place(track.rectTransform, new Vector2(0, 0), new Vector2(96, 18), new Vector2(160, 5));
            _xp = Ui.Img(track.transform, "Growth", Ui.Hex("#ecd491"));
            Ui.Stretch(_xp.rectTransform);

            _region = Ui.Label(root, "RegionTitle", "", 29, Paper);
            Ui.Place(_region.rectTransform, new Vector2(.5f, 1), new Vector2(0, -30), new Vector2(350, 44));
            var wallet = Ui.Img(root, "Wallet", Forest);
            Ui.Place(wallet.rectTransform, new Vector2(1, 1), new Vector2(-256, -24), new Vector2(125, 58));
            _coins = Ui.Label(wallet.transform, "Coins", "", 29, Paper);
            Ui.Stretch(_coins.rectTransform);
            var atlas = Ui.Btn(root, "BtnAtlas", "WORLD", 23);
            Ui.Place((RectTransform)atlas.transform, new Vector2(1, 1), new Vector2(-136, -24), new Vector2(112, 58));
            atlas.onClick.AddListener(() => world());
            var menuButton = Ui.Btn(root, "BtnMenu", "MENU", 23);
            Ui.Place((RectTransform)menuButton.transform, new Vector2(1, 1), new Vector2(-24, -24), new Vector2(100, 58));
            menuButton.onClick.AddListener(() => menu());

            var quest = Ui.Img(root, "CurrentObjective", Forest);
            Ui.Place(quest.rectTransform, new Vector2(0, 0), new Vector2(24, 24), new Vector2(490, 78));
            var questButton = quest.gameObject.AddComponent<Button>();
            questButton.onClick.AddListener(() => repeat());
            var label = Ui.Label(quest.transform, "ObjectiveHeading", "YOUR ADVENTURE   /   TAP TO LISTEN", 19,
                Ui.Hex("#d8be79"), TextAnchor.MiddleLeft);
            Ui.Place(label.rectTransform, new Vector2(0, 1), new Vector2(18, -9), new Vector2(454, 24));
            _objective = Ui.Label(quest.transform, "Objective", "", 27, Paper, TextAnchor.MiddleLeft);
            Ui.Place(_objective.rectTransform, new Vector2(0, 0), new Vector2(18, 12), new Vector2(454, 35));
            _objective.enableAutoSizing = true;
            _objective.fontSizeMin = 21;
            _objective.fontSizeMax = 27;
        }

        public void Refresh(Progress p, MapDef map)
        {
            string id = p.CurrentFormId(p.ActiveMonId);
            _portrait.sprite = SpriteLib.EnemyBattleSprite(id);
            _name.text = $"{GameData.ById(id).Name}\nLv. {p.ActiveGrowth.Level}";
            _name.fontSize = 23;
            _coins.text = $"{p.Coins} G";
            _region.text = map.DisplayName;
            _objective.text = map.Id == "forest" ? ForestJourney.Objective(p) :
                map.GateCleared(p) ? "The portal is open! A new world awaits!" : "Explore, find treasure, meet Mathmons.";
            _xp.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(p.ActiveGrowth.Xp / (float)p.ActiveGrowth.XpToNext), 1);
        }

        public void OpenAtlas(Progress p, Action<string> travel, Action close)
        {
            var shade = Ui.Img(_root, "WorldAtlas", Ui.Hex("#0b2422f2"));
            Ui.Stretch(shade.rectTransform);
            var title = Ui.Label(shade.transform, "Title", $"THE SIX CRYSTAL LANDS   ·   {p.DigitCrystalCount}/6", 42, Paper);
            Ui.Place(title.rectTransform, new Vector2(.5f, 1), new Vector2(0, -48), new Vector2(950, 65));
            MapDef[] maps = Maps.All();
            for (int i = 0; i < maps.Length; i++)
            {
                MapDef map = maps[i];
                bool unlocked = i == 0 || maps[i - 1].GateCleared(p);
                var tile = Ui.Img(shade.transform, "Region-" + map.Id, Ui.Hex("#35534e"));
                Ui.PlaceCentered(tile.rectTransform, new Vector2(.5f, .5f),
                    new Vector2((i % 3 - 1) * 330, 132 - (i / 3) * 263), new Vector2(300, 240));
                var art = Ui.SpriteImg(tile.transform, "Terrain", MapArt.Terrain(map.Theme, Tile.Landmark, 0, 0));
                art.preserveAspect = true;
                art.color = unlocked ? Color.white : new Color(.4f, .5f, .5f, 1);
                Ui.PlaceCentered(art.rectTransform, Vector2.one * .5f, new Vector2(0, 24), new Vector2(200, 175));
                var name = Ui.Label(tile.transform, "Name", unlocked ? map.DisplayName : "UNDISCOVERED", 27, Paper);
                Ui.Place(name.rectTransform, new Vector2(.5f, 0), new Vector2(0, 13), new Vector2(285, 40));
                var button = tile.gameObject.AddComponent<Button>();
                button.interactable = unlocked;
                button.onClick.AddListener(() => { UnityEngine.Object.Destroy(shade.gameObject); travel(map.Id); });
            }
            var back = Ui.Btn(shade.transform, "CloseAtlas", "BACK TO ADVENTURE", 26);
            Ui.Place((RectTransform)back.transform, new Vector2(.5f, 0), new Vector2(0, 34), new Vector2(340, 64));
            back.onClick.AddListener(() => { UnityEngine.Object.Destroy(shade.gameObject); close(); });
        }
    }
}
