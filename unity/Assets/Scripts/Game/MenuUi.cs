using System;
using System.Collections.Generic;
using Numeria.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>
    /// 状态菜单(参考设计:羊皮纸面板 + 图标 tab + TEAM 双栏 master-detail)。
    /// 左栏:队伍卡片(HP 条 + 等级,空位显示 +);右栏:选中数灵详情
    /// (大立绘、类型徽章、属性表、进化路线、设为出战)。
    /// </summary>
    public class MenuUi
    {
        private readonly Progress _progress;
        private readonly Action _onClose;
        private readonly Action _onReset;
        private readonly Action<string> _onTravel;
        private readonly RectTransform _canvasRoot;

        private Image _overlay;
        private RectTransform _contentArea;
        private readonly Dictionary<string, Image> _tabImages = new Dictionary<string, Image>();
        private readonly Dictionary<string, Image> _tabBars = new Dictionary<string, Image>();
        private string _selectedId;

        // 参考图配色
        private static readonly Color Cream = Ui.Hex("#f6efdc");
        private static readonly Color CreamDeep = Ui.Hex("#efe5cb");
        private static readonly Color TitleGreen = Ui.Hex("#3a4d2f");
        private static readonly Color SummaryOrange = Ui.Hex("#c77b3a");
        private static readonly Color TabActive = Ui.Hex("#f2b04e");
        private static readonly Color CardActive = Ui.Hex("#dcefc8");
        private static readonly Color HpGreen = Ui.Hex("#7ac974");
        private static readonly Color HpTrack = Ui.Hex("#e3d9bd");

        public static void Open(RectTransform canvasRoot, Progress progress, Action onClose, Action onReset,
            Action<string> onTravel)
        {
            new MenuUi(canvasRoot, progress, onClose, onReset, onTravel).Build();
        }

        private MenuUi(RectTransform canvasRoot, Progress progress, Action onClose, Action onReset,
            Action<string> onTravel)
        {
            _canvasRoot = canvasRoot;
            _progress = progress;
            _onClose = onClose;
            _onReset = onReset;
            _onTravel = onTravel;
            _selectedId = progress.ActiveMonId;
        }

        // ---------- 数灵展示数据 ----------

        private string DisplaySpriteId(string id) =>
            id == "addmander" && _progress.Evolved ? "sumdrake" : id;

        private static (string label, Color color) TypeOf(string id)
        {
            switch (id)
            {
                case "addmander":
                case "sumdrake": return ("FIRE", Ui.Hex("#e8703a"));
                case "countipillar": return ("BUG", Ui.Hex("#6a9f3f"));
                case "doublit": return ("ROCK", Ui.Hex("#7d8894"));
                case "mirrowl": return ("SKY", Ui.Hex("#49a9d1"));
                default: return ("???", Ui.Hex("#7d8894"));
            }
        }

        private List<string> TeamIds()
        {
            var ids = new List<string> { "addmander" };
            ids.AddRange(_progress.CaughtIds);
            return ids;
        }

        // ---------- 框架 ----------

        private void Build()
        {
            _overlay = Ui.Img(_canvasRoot, "MenuOverlay", new Color(0.03f, 0.06f, 0.04f, 0.78f));
            Ui.Stretch(_overlay.rectTransform);

            // 大外框采用三层硬边，避免 256px 面板的阴影像素被拉伸成角块；内容卡片继续使用角花素材。
            var frame = Ui.Img(_overlay.transform, "Frame", TitleGreen);
            Ui.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1220, 840));
            var trim = Ui.Img(frame.transform, "GoldTrim", Ui.Hex("#8d6039"));
            Ui.Stretch(trim.rectTransform);
            trim.rectTransform.offsetMin = new Vector2(6, 6);
            trim.rectTransform.offsetMax = new Vector2(-6, -6);
            var surface = Ui.Img(trim.transform, "Parchment", Cream);
            Ui.Stretch(surface.rectTransform);
            surface.rectTransform.offsetMin = new Vector2(4, 4);
            surface.rectTransform.offsetMax = new Vector2(-4, -4);

            var panel = Ui.Node(surface.transform, "MenuPanel");
            Ui.Stretch(panel);
            panel.offsetMin = new Vector2(32, 22);
            panel.offsetMax = new Vector2(-32, 0);

            var column = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            column.spacing = 6;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            BuildHeader(panel.transform);
            BuildTabs(panel.transform);

            var area = Ui.Node(panel.transform, "ContentArea");
            area.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            _contentArea = area;

            ShowTeam();
        }

        private void BuildHeader(Transform parent)
        {
            var row = Ui.Node(parent, "Header");
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 98;
            le.minHeight = 98;
            le.flexibleHeight = 0;

            var title = Ui.Label(row, "Title", "NUMERIA", 56, TitleGreen, TextAnchor.UpperLeft);
            Ui.Place(title.rectTransform, new Vector2(0, 1), new Vector2(4, 0), new Vector2(500, 58));
            var summary = Ui.Label(row, "Summary",
                $"Lv. {_progress.Level}   XP {_progress.Xp}/{_progress.XpToNext}   ATK +{_progress.AttackBonus}",
                28, SummaryOrange, TextAnchor.UpperLeft);
            Ui.Place(summary.rectTransform, new Vector2(0, 1), new Vector2(6, -61), new Vector2(700, 34));

            var close = Ui.Img(row, "BtnClose", Cream);
            Ui.Place(close.rectTransform, new Vector2(1, 1), new Vector2(-2, -2), new Vector2(68, 68));
            Ui.AddOutline(close.gameObject);
            var closeInset = Ui.Img(close.transform, "Inset", Cream);
            Ui.Stretch(closeInset.rectTransform);
            closeInset.rectTransform.offsetMin = new Vector2(7, 7);
            closeInset.rectTransform.offsetMax = new Vector2(-7, -7);
            Ui.AddOutline(closeInset.gameObject);
            var x = Ui.Label(close.transform, "X", "X", 38, TitleGreen);
            Ui.Stretch(x.rectTransform);
            var btn = Sfx.WireClick(close.gameObject.AddComponent<Button>());
            btn.onClick.AddListener(() =>
            {
                UnityEngine.Object.Destroy(_overlay.gameObject);
                _onClose();
            });
        }

        private void BuildTabs(Transform parent)
        {
            var row = Ui.Node(parent, "Tabs");
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 76;
            le.minHeight = 76;
            le.flexibleHeight = 0; // 内部 LayoutGroup 会对外汇报可伸缩,必须封死
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;

            TabButton(row, "team", "TEAM", SpriteLib.One($"Art/Sprites/{DisplaySpriteId("addmander")}"), ShowTeam);
            TabButton(row, "items", "ITEMS", SpriteLib.Cainos("TX Props", "TX Props Chest"), ShowItems);
            TabButton(row, "settings", "SETTINGS", SpriteLib.One("Art/Sprites/shield"), ShowSettings);
        }

        private void TabButton(RectTransform parent, string key, string label, Sprite icon, Action onClick)
        {
            var tab = Ui.Img(parent, $"Tab-{key}", Cream);
            Ui.AddOutline(tab.gameObject);
            _tabImages[key] = tab;

            var icImg = Ui.SpriteImg(tab.transform, "Icon", icon);
            Ui.Place(icImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-72, 2), new Vector2(44, 44));
            var text = Ui.Label(tab.transform, "Label", label, 30, TitleGreen);
            Ui.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(30, 0), new Vector2(220, 40));

            var activeBar = Ui.Img(tab.transform, "ActiveBar", TabActive);
            activeBar.rectTransform.anchorMin = Vector2.zero;
            activeBar.rectTransform.anchorMax = new Vector2(1, 0);
            activeBar.rectTransform.pivot = new Vector2(0.5f, 0);
            activeBar.rectTransform.anchoredPosition = new Vector2(0, -5);
            activeBar.rectTransform.sizeDelta = new Vector2(-8, 8);
            _tabBars[key] = activeBar;

            var btn = Sfx.WireClick(tab.gameObject.AddComponent<Button>());
            btn.onClick.AddListener(() => onClick());
        }

        private void SelectTab(string key)
        {
            foreach (var pair in _tabImages)
                pair.Value.color = pair.Key == key ? TabActive : Cream;
            foreach (var pair in _tabBars)
                pair.Value.gameObject.SetActive(pair.Key == key);
        }

        private void ClearContent()
        {
            foreach (Transform child in _contentArea) UnityEngine.Object.Destroy(child.gameObject);
        }

        /// <summary>清空内容区并返回全新的包装节点——布局/滚动组件都挂在它上面,切 tab 随之销毁。</summary>
        private RectTransform FreshWrap()
        {
            ClearContent();
            var wrap = Ui.Node(_contentArea, "Wrap");
            Ui.Stretch(wrap);
            return wrap;
        }

        /// <summary>在容器里建一个可垂直滚动的列表,返回内容根。</summary>
        private RectTransform MakeScrollList(RectTransform parent)
        {
            var bg = parent.gameObject.GetComponent<Image>();
            if (bg == null)
            {
                bg = parent.gameObject.AddComponent<Image>();
                bg.color = new Color(0, 0, 0, 0.02f);
            }
            parent.gameObject.AddComponent<RectMask2D>();
            var scroll = parent.gameObject.AddComponent<ScrollRect>();

            var content = Ui.Node(parent, "Content");
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = new Vector2(4, 0);
            content.offsetMax = new Vector2(-4, 0);
            var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(4, 4, 6, 6);
            v.spacing = 10;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;
            scroll.viewport = parent;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
            return content;
        }

        private static void ListRow(Transform parent, string name, float height, Action<RectTransform> build)
        {
            var row = Ui.Node(parent, name);
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            le.flexibleHeight = 0;
            build(row);
        }

        // ---------- TEAM(双栏 master-detail) ----------

        private void ShowTeam()
        {
            SelectTab("team");
            var wrap = FreshWrap();

            var split = wrap.gameObject.AddComponent<HorizontalLayoutGroup>();
            split.spacing = 14;
            split.childControlWidth = true;
            split.childControlHeight = true;
            split.childForceExpandWidth = false;
            split.childForceExpandHeight = true;

            // 左栏:队伍列表
            var left = Ui.Node(wrap, "LeftCol");
            var lle = left.gameObject.AddComponent<LayoutElement>();
            lle.preferredWidth = 478;
            lle.minWidth = 478;
            lle.flexibleWidth = 0;
            BuildTeamList(left);

            // 右栏:详情
            var right = Ui.Node(wrap, "RightCol");
            var rle = right.gameObject.AddComponent<LayoutElement>();
            rle.preferredWidth = 650;
            rle.minWidth = 650;
            rle.flexibleWidth = 1;
            BuildMonDetail(right, _selectedId);
        }

        private void BuildTeamList(RectTransform parent)
        {
            var v = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 7;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            ListRow(parent, "TeamHeader", 42, row =>
            {
                Ui.Stretch(Ui.Label(row, "Text", "YOUR TEAM", 28, TitleGreen).rectTransform);
                var leftGem = Ui.SpriteImg(row, "LeftGem", SpriteLib.Pack("UI/Icons/Gem"));
                leftGem.color = Ui.Hex("#5c8a3f");
                Ui.PlaceCentered(leftGem.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-112, 0),
                    new Vector2(24, 24));
                var rightGem = Ui.SpriteImg(row, "RightGem", SpriteLib.Pack("UI/Icons/Gem"));
                rightGem.color = Ui.Hex("#5c8a3f");
                Ui.PlaceCentered(rightGem.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(112, 0),
                    new Vector2(24, 24));
            });

            var team = TeamIds();
            const int slots = 6;
            for (int i = 0; i < slots; i++)
            {
                if (i < team.Count)
                {
                    string id = team[i];
                    ListRow(parent, $"Mon-{id}", 106, row => BuildTeamCard(row, id));
                }
                else
                {
                    ListRow(parent, $"Empty-{i}", 58, row =>
                    {
                        var img = row.gameObject.AddComponent<Image>();
                        img.color = CreamDeep;
                        Ui.AddOutline(row.gameObject);
                        var plus = Ui.Label(row, "Plus", "+", 30, Ui.Hex("#b0a88e"));
                        Ui.Stretch(plus.rectTransform);
                    });
                }
            }

            ListRow(parent, "Hint", 38, row =>
                Ui.Stretch(Ui.Label(row, "Text", "Tap a teammate to view details", 23, SummaryOrange).rectTransform));
        }

        private void BuildTeamCard(RectTransform row, string id)
        {
            var def = GameData.PlayerMon(id, _progress.Evolved);
            bool active = id == _progress.ActiveMonId;
            bool selected = id == _selectedId;

            var img = row.gameObject.AddComponent<Image>();
            img.color = active ? CardActive : selected ? Ui.Hex("#f6e6c4") : Cream;
            Ui.AddOutline(row.gameObject);

            if (selected)
            {
                var accent = Ui.Img(row, "SelectedAccent", TabActive);
                accent.rectTransform.anchorMin = Vector2.zero;
                accent.rectTransform.anchorMax = new Vector2(0, 1);
                accent.rectTransform.pivot = new Vector2(0, 0.5f);
                accent.rectTransform.anchoredPosition = Vector2.zero;
                accent.rectTransform.sizeDelta = new Vector2(9, -4);
            }

            var sprite = Ui.SpriteImg(row, "Sprite", SpriteLib.MapSprite(DisplaySpriteId(id)));
            sprite.preserveAspect = true;
            Ui.Place(sprite.rectTransform, new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(82, 82));

            var name = Ui.Label(row, "Name", def.Name, 29, Ui.Ink, TextAnchor.UpperLeft);
            Ui.Place(name.rectTransform, new Vector2(0, 1), new Vector2(118, -9), new Vector2(270, 34));
            var lv = Ui.Label(row, "Lv", $"Lv. {_progress.Level}", 24, SummaryOrange, TextAnchor.UpperLeft);
            Ui.Place(lv.rectTransform, new Vector2(0, 1), new Vector2(118, -43), new Vector2(140, 28));

            // HP 条(地图上恒满)
            var track = Ui.Img(row, "HpTrack", HpTrack);
            Ui.Place(track.rectTransform, new Vector2(0, 0), new Vector2(118, 16), new Vector2(236, 15));
            var fill = Ui.Img(track.transform, "HpFill", HpGreen);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(1f, 1f); // 满血
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            var hpText = Ui.Label(row, "HpText", $"{def.MaxHp} / {def.MaxHp}", 22, Ui.Ink, TextAnchor.LowerRight);
            Ui.Place(hpText.rectTransform, new Vector2(1, 0), new Vector2(-12, 10), new Vector2(108, 26));

            var btn = Sfx.WireClick(row.gameObject.AddComponent<Button>());
            btn.onClick.AddListener(() =>
            {
                _selectedId = id;
                ShowTeam();
            });
        }

        private void BuildMonDetail(RectTransform parent, string id)
        {
            var def = GameData.PlayerMon(id, _progress.Evolved);
            bool active = id == _progress.ActiveMonId;
            var (typeLabel, typeColor) = TypeOf(DisplaySpriteId(id));

            var v = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 14;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var identity = Ui.SpriteImg(parent, "IdentityPanel", SpriteLib.Pack("UI/Panels/Generic_Panel"));
            identity.type = Image.Type.Sliced;
            var identityLe = identity.gameObject.AddComponent<LayoutElement>();
            identityLe.preferredHeight = 315;
            identityLe.minHeight = 315;
            identityLe.flexibleHeight = 0;

            var sprite = Ui.SpriteImg(identity.transform, "BigSprite", SpriteLib.LargeIcon(DisplaySpriteId(id)));
            sprite.preserveAspect = true;
            Ui.PlaceCentered(sprite.rectTransform, new Vector2(0, 0.55f), new Vector2(145, 0), new Vector2(240, 240));

            var name = Ui.Label(identity.transform, "Name", def.Name.ToUpperInvariant(), 42, TitleGreen,
                TextAnchor.MiddleCenter);
            Ui.Place(name.rectTransform, new Vector2(1, 1), new Vector2(-28, -24), new Vector2(390, 48));

            var chip = Ui.Img(identity.transform, "TypeChip", typeColor);
            Ui.Place(chip.rectTransform, new Vector2(1, 1), new Vector2(-148, -82), new Vector2(150, 44));
            Ui.AddOutline(chip.gameObject);
            var chipText = Ui.Label(chip.transform, "Text", typeLabel, 27, Color.white);
            Ui.Stretch(chipText.rectTransform);

            if (active)
            {
                var ready = Ui.Label(identity.transform, "Ready", "- BATTLE READY -", 27,
                    Ui.Hex("#5c8a3f"));
                Ui.Place(ready.rectTransform, new Vector2(1, 1), new Vector2(-28, -128), new Vector2(390, 38));
            }
            else
            {
                var pick = Ui.Btn(identity.transform, "BtnPick", "MAKE BATTLE BUDDY", 24);
                pick.image.color = TabActive;
                Ui.Place((RectTransform)pick.transform, new Vector2(1, 1), new Vector2(-58, -132),
                    new Vector2(330, 46));
                pick.onClick.AddListener(() =>
                {
                    _progress.ActiveMonId = id;
                    ShowTeam();
                });
            }

            int atk = 2 + _progress.AttackBonus;
            var formula = Array.Find(def.Skills, s => s.Id == "flame-formula");
            BuildStatTable(identity.transform, def.MaxHp, atk, formula);

            var evolution = Ui.SpriteImg(parent, "EvolutionPanel", SpriteLib.Pack("UI/Panels/Generic_Panel"));
            evolution.type = Image.Type.Sliced;
            var evoLe = evolution.gameObject.AddComponent<LayoutElement>();
            evoLe.minHeight = 260;
            evoLe.flexibleHeight = 1;
            BuildEvolutionBlock(evolution.rectTransform, id);
        }

        private void BuildStatTable(Transform parent, int hp, int atk, SkillDef formula)
        {
            var table = Ui.Img(parent, "Stats", Cream);
            Ui.Place(table.rectTransform, new Vector2(1, 0), new Vector2(-28, 0), new Vector2(398, 142));
            Ui.AddOutline(table.gameObject);

            string[] labels = { "HP", "ATK", formula != null ? formula.Name.ToUpperInvariant() : "SKILL" };
            string[] values = { $"{hp} / {hp}", atk.ToString(), formula != null ? $"POWER {formula.Power}" : "-" };
            string[] icons = { "Art/Sprites/gem", "Art/Sprites/icon-sword", "Art/Sprites/icon-flame" };
            for (int i = 0; i < labels.Length; i++)
            {
                float y = -(i * 46 + 6);
                var icon = Ui.SpriteImg(table.transform, $"Icon{i}", SpriteLib.One(icons[i]));
                icon.preserveAspect = true;
                if (i == 0) icon.color = Ui.Hex("#d94a3d");
                Ui.Place(icon.rectTransform, new Vector2(0, 1), new Vector2(14, y), new Vector2(34, 34));
                var label = Ui.Label(table.transform, $"Label{i}", labels[i], 26, Ui.Ink, TextAnchor.MiddleLeft);
                Ui.Place(label.rectTransform, new Vector2(0, 1), new Vector2(58, y), new Vector2(240, 36));
                var value = Ui.Label(table.transform, $"Value{i}", values[i], 25, TitleGreen, TextAnchor.MiddleRight);
                Ui.Place(value.rectTransform, new Vector2(1, 1), new Vector2(-14, y), new Vector2(160, 36));
                if (i < labels.Length - 1)
                {
                    var divider = Ui.Img(table.transform, $"Divider{i}", Ui.Hex("#a99a75"));
                    divider.rectTransform.anchorMin = new Vector2(0, 1);
                    divider.rectTransform.anchorMax = new Vector2(1, 1);
                    divider.rectTransform.pivot = new Vector2(0.5f, 1);
                    divider.rectTransform.anchoredPosition = new Vector2(0, -(i + 1) * 46);
                    divider.rectTransform.sizeDelta = new Vector2(-8, 2);
                }
            }
        }

        private void BuildEvolutionBlock(RectTransform parent, string id)
        {
            string toId = null, text;
            Color color = Ui.Ink;
            switch (id)
            {
                case "addmander":
                    toId = "sumdrake";
                    if (_progress.Evolved) { text = "Evolved! Learned Blaze Equation!"; color = Ui.Hex("#2e7d32"); }
                    else
                    {
                        string lv = _progress.Level >= 5 ? "YES" : $"now Lv.{_progress.Level}";
                        string stone = _progress.HasEvoStone ? "YES" : "in Silent Peaks";
                        text = $"Needs Lv.5 ({lv}) + Evolution Stone ({stone})";
                    }
                    break;
                case "countipillar":
                    text = "Evolves into Numberfly - coming soon!";
                    break;
                case "doublit":
                    toId = "duplirock";
                    text = "Evolves into Duplirock - coming soon!";
                    break;
                default:
                    text = "No evolution.";
                    break;
            }

            var title = Ui.Label(parent, "EvoTitle", "EVOLUTION", 32, TitleGreen);
            Ui.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(300, 40));

            if (toId != null)
            {
                // Match the portrait used by the detail/status card, scaled down for the evolution path.
                EvoIconAt(parent, "addmander", new Vector2(0.34f, 0.58f), new Vector2(84, 84), true);
                var arrow = Ui.Label(parent, "Arrow", ">", 42, SummaryOrange);
                Ui.PlaceCentered(arrow.rectTransform, new Vector2(0.5f, 0.60f), Vector2.zero, new Vector2(50, 50));
                EvoIconAt(parent, toId, new Vector2(0.67f, 0.65f), new Vector2(140, 140), true);

                var levels = Ui.Label(parent, "Levels", $"Lv. {_progress.Level}     >     Lv. 5", 26,
                    SummaryOrange);
                Ui.Place(levels.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 104), new Vector2(430, 34));

                RequirementRow(parent, "Stone", SpriteLib.Cainos("TX Props", "TX Props - Stone 01"),
                    _progress.Evolved ? "Evolution complete" : "Requires Evolution Stone", 68,
                    _progress.Evolved || _progress.HasEvoStone ? Ui.Hex("#4f7e3d") : Ui.Ink);
                RequirementRow(parent, "Peaks", SpriteLib.Cainos("TX Props", "TX Props Altar"),
                    _progress.Evolved ? "Blaze Equation learned" : "Found in Silent Peaks", 34, color);
            }
            else
            {
                var condition = Ui.Label(parent, "EvoCond", text, 24, color);
                Ui.PlaceCentered(condition.rectTransform, new Vector2(0.5f, 0.45f), Vector2.zero,
                    new Vector2(560, 70));
            }
        }

        private static void EvoIconAt(RectTransform parent, string id, Vector2 anchor, Vector2 size, bool large)
        {
            var sprite = large ? SpriteLib.LargeIcon(id) : SpriteLib.One($"Art/Sprites/{id}");
            var img = Ui.SpriteImg(parent, $"Evo-{id}", sprite);
            img.preserveAspect = true;
            Ui.PlaceCentered(img.rectTransform, anchor, Vector2.zero, size);
        }

        private static void RequirementRow(RectTransform parent, string name, Sprite sprite, string text,
            float bottom, Color color)
        {
            var icon = Ui.SpriteImg(parent, name, sprite);
            icon.preserveAspect = true;
            Ui.Place(icon.rectTransform, new Vector2(0, 0), new Vector2(108, bottom), new Vector2(34, 34));
            var label = Ui.Label(parent, $"{name}Text", text, 23, color, TextAnchor.MiddleLeft);
            Ui.Place(label.rectTransform, new Vector2(0, 0), new Vector2(154, bottom), new Vector2(470, 34));
        }

        // ---------- ITEMS ----------

        private void ShowItems()
        {
            SelectTab("items");
            var content = MakeScrollList(FreshWrap());

            if (_progress.Items.Count == 0)
            {
                ListRow(content, "NoItems", 60, row =>
                    Ui.Stretch(Ui.Label(row, "Text", "No items yet - open math chests!", 26, Ui.Ink).rectTransform));
                return;
            }
            foreach (string item in _progress.Items)
            {
                string captured = item;
                string effect = captured == "Evolution Stone" ? "Evolution material" : "+1 ATK";
                ListRow(content, $"Item-{item}", 70, row =>
                {
                    var img = row.gameObject.AddComponent<Image>();
                    img.color = Cream;
                    Ui.AddOutline(row.gameObject);
                    var icon = Ui.SpriteImg(row, "Icon", SpriteLib.One("Art/Sprites/gem"));
                    Ui.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(38, 38));
                    var name = Ui.Label(row, "Name", captured, 26, Ui.Ink, TextAnchor.MiddleLeft);
                    Ui.Place(name.rectTransform, new Vector2(0, 0.5f), new Vector2(70, 0), new Vector2(480, 40));
                    var eff = Ui.Label(row, "Effect", effect, 24, SummaryOrange, TextAnchor.MiddleRight);
                    Ui.Place(eff.rectTransform, new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(360, 40));
                });
            }
        }

        // ---------- SETTINGS ----------

        private void ShowSettings()
        {
            SelectTab("settings");
            var content = MakeScrollList(FreshWrap());

            SectionRow(content, "Sound");
            ListRow(content, "VoiceRow", 66, row =>
            {
                FillButtonRow(row);
                var voiceBtn = Ui.Btn(row, "BtnVoice", _progress.VoiceEnabled ? "Voice: ON" : "Voice: OFF", 24);
                voiceBtn.onClick.AddListener(() =>
                {
                    _progress.VoiceEnabled = !_progress.VoiceEnabled;
                    Voice.Enabled = _progress.VoiceEnabled;
                    voiceBtn.GetComponentInChildren<TMP_Text>().text = _progress.VoiceEnabled ? "Voice: ON" : "Voice: OFF";
                    SaveSystem.Save(_progress);
                });
            });
            ListRow(content, "SfxRow", 66, row =>
            {
                FillButtonRow(row);
                var sfxBtn = Ui.Btn(row, "BtnSfx", _progress.SfxEnabled ? "SFX: ON" : "SFX: OFF", 24);
                sfxBtn.onClick.AddListener(() =>
                {
                    _progress.SfxEnabled = !_progress.SfxEnabled;
                    Sfx.Enabled = _progress.SfxEnabled;
                    sfxBtn.GetComponentInChildren<TMP_Text>().text = _progress.SfxEnabled ? "SFX: ON" : "SFX: OFF";
                    if (_progress.SfxEnabled) Sfx.Play(SfxCue.Click);
                    SaveSystem.Save(_progress);
                });
            });
            ListRow(content, "MusicRow", 66, row =>
            {
                FillButtonRow(row);
                var musicBtn = Ui.Btn(row, "BtnMusic", _progress.MusicEnabled ? "Music: ON" : "Music: OFF", 24);
                musicBtn.onClick.AddListener(() =>
                {
                    _progress.MusicEnabled = !_progress.MusicEnabled;
                    Music.Enabled = _progress.MusicEnabled;
                    musicBtn.GetComponentInChildren<TMP_Text>().text = _progress.MusicEnabled ? "Music: ON" : "Music: OFF";
                    if (_progress.MusicEnabled) Music.PlayMap(_progress.CurrentMap);
                    SaveSystem.Save(_progress);
                });
            });

            SectionRow(content, "Travel");
            ListRow(content, "TravelRow", 66, row =>
            {
                FillButtonRow(row);
                var forestBtn = Ui.Btn(row, "BtnForest", "Mystic Forest", 22);
                forestBtn.interactable = _progress.CurrentMap != "forest";
                forestBtn.onClick.AddListener(() => CloseThen(() => _onTravel("forest")));
                var peaksBtn = Ui.Btn(row, "BtnPeaks",
                    _progress.BossBeaten ? "Silent Peaks" : "Silent Peaks (locked)", 22);
                peaksBtn.interactable = _progress.BossBeaten && _progress.CurrentMap != "mountains";
                peaksBtn.onClick.AddListener(() => CloseThen(() => _onTravel("mountains")));
                var skyUnlocked = _progress.ClearedGates.Contains("mountains");
                var skyBtn = Ui.Btn(row, "BtnSky", skyUnlocked ? "Azure Sky City" : "Sky City (locked)", 22);
                skyBtn.interactable = skyUnlocked && _progress.CurrentMap != "sky";
                skyBtn.onClick.AddListener(() => CloseThen(() => _onTravel("sky")));
            });

            SectionRow(content, "Danger Zone");
            ListRow(content, "DangerRow", 66, row =>
            {
                FillButtonRow(row);
                var resetBtn = Ui.Btn(row, "BtnReset", "Reset Adventure", 22);
                resetBtn.onClick.AddListener(ConfirmReset);
                var quitBtn = Ui.Btn(row, "BtnQuit", "Quit Game", 22);
                quitBtn.onClick.AddListener(() =>
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
            });
        }

        private static void SectionRow(Transform parent, string text)
        {
            ListRow(parent, $"Section-{text}", 34, row =>
                Ui.Stretch(Ui.Label(row, "Text", text, 22, Ui.GemOrange, TextAnchor.MiddleLeft).rectTransform));
        }

        private static void FillButtonRow(RectTransform row)
        {
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 20;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;
        }

        private void CloseThen(Action action)
        {
            UnityEngine.Object.Destroy(_overlay.gameObject);
            action();
        }

        private void ConfirmReset()
        {
            var confirm = Ui.Img(_canvasRoot, "ConfirmOverlay", new Color(0, 0, 0, 0.7f));
            Ui.Stretch(confirm.rectTransform);
            var panel = Ui.Img(confirm.transform, "ConfirmPanel", Cream);
            Ui.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 320));
            Ui.AddOutline(panel.gameObject);
            var msg = Ui.Label(panel.transform, "Msg", "Start a brand new adventure?\nAll progress will be lost!", 28, Ui.Ink);
            Ui.Place(msg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(640, 90));

            var yes = Ui.Btn(panel.transform, "BtnYes", "Yes, restart", 24);
            Ui.Place((RectTransform)yes.transform, new Vector2(0.5f, 0f), new Vector2(-160, 60), new Vector2(280, 74));
            yes.onClick.AddListener(() =>
            {
                UnityEngine.Object.Destroy(confirm.gameObject);
                CloseThen(_onReset);
            });

            var no = Ui.Btn(panel.transform, "BtnNo", "No, keep going", 24);
            Ui.Place((RectTransform)no.transform, new Vector2(0.5f, 0f), new Vector2(160, 60), new Vector2(280, 74));
            no.onClick.AddListener(() => UnityEngine.Object.Destroy(confirm.gameObject));
        }
    }
}
