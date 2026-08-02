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
            _overlay = Ui.Img(_canvasRoot, "MenuOverlay", new Color(0.05f, 0.08f, 0.05f, 0.9f));
            Ui.Stretch(_overlay.rectTransform);

            // 外层深绿描边框 → 内层羊皮纸面板(双层边框感)
            var frame = Ui.Img(_overlay.transform, "Frame", TitleGreen);
            Ui.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180, 830));
            var panel = Ui.Img(frame.transform, "MenuPanel", Cream);
            Ui.Stretch(panel.rectTransform);
            panel.rectTransform.offsetMin = new Vector2(8, 8);
            panel.rectTransform.offsetMax = new Vector2(-8, -8);

            var column = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(28, 28, 18, 18);
            column.spacing = 10;
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
            le.preferredHeight = 96;
            le.minHeight = 96;
            le.flexibleHeight = 0;

            var title = Ui.Label(row, "Title", "NUMERIA", 46, TitleGreen, TextAnchor.UpperLeft);
            Ui.Place(title.rectTransform, new Vector2(0, 1), new Vector2(4, -2), new Vector2(500, 54));
            var summary = Ui.Label(row, "Summary",
                $"Lv. {_progress.Level}   XP {_progress.Xp}/{_progress.XpToNext}   ATK +{_progress.AttackBonus}",
                24, SummaryOrange, TextAnchor.UpperLeft);
            Ui.Place(summary.rectTransform, new Vector2(0, 1), new Vector2(6, -60), new Vector2(600, 30));

            var close = Ui.Img(row, "BtnClose", Cream);
            Ui.Place(close.rectTransform, new Vector2(1, 1), new Vector2(-4, -4), new Vector2(64, 64));
            Ui.AddOutline(close.gameObject);
            var x = Ui.Label(close.transform, "X", "X", 32, TitleGreen);
            Ui.Stretch(x.rectTransform);
            var btn = close.gameObject.AddComponent<Button>();
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
            le.preferredHeight = 70;
            le.minHeight = 70;
            le.flexibleHeight = 0; // 内部 LayoutGroup 会对外汇报可伸缩,必须封死
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10;
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
            Ui.Place(icImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-70, 0), new Vector2(40, 40));
            var text = Ui.Label(tab.transform, "Label", label, 26, TitleGreen);
            Ui.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(30, 0), new Vector2(220, 40));

            var btn = tab.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
        }

        private void SelectTab(string key)
        {
            foreach (var pair in _tabImages)
                pair.Value.color = pair.Key == key ? TabActive : Cream;
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
            split.spacing = 22;
            split.childControlWidth = true;
            split.childControlHeight = true;
            split.childForceExpandWidth = false;
            split.childForceExpandHeight = true;

            // 左栏:队伍列表
            var left = Ui.Node(wrap, "LeftCol");
            var lle = left.gameObject.AddComponent<LayoutElement>();
            lle.preferredWidth = 440;
            lle.minWidth = 440;
            BuildTeamList(left);

            // 右栏:详情
            var right = Ui.Node(wrap, "RightCol");
            right.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            BuildMonDetail(right, _selectedId);
        }

        private void BuildTeamList(RectTransform parent)
        {
            var v = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            ListRow(parent, "TeamHeader", 34, row =>
                Ui.Stretch(Ui.Label(row, "Text", "- YOUR TEAM -", 24, TitleGreen).rectTransform));

            var team = TeamIds();
            const int slots = 6;
            for (int i = 0; i < slots; i++)
            {
                if (i < team.Count)
                {
                    string id = team[i];
                    ListRow(parent, $"Mon-{id}", 88, row => BuildTeamCard(row, id));
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

            ListRow(parent, "Hint", 34, row =>
                Ui.Stretch(Ui.Label(row, "Text", "Tap a teammate to view details", 20, SummaryOrange).rectTransform));
        }

        private void BuildTeamCard(RectTransform row, string id)
        {
            var def = GameData.PlayerMon(id, _progress.Evolved);
            bool active = id == _progress.ActiveMonId;
            bool selected = id == _selectedId;

            var img = row.gameObject.AddComponent<Image>();
            img.color = active ? CardActive : selected ? Ui.Hex("#f6e6c4") : Cream;
            Ui.AddOutline(row.gameObject);

            var sprite = Ui.SpriteImg(row, "Sprite", SpriteLib.One($"Art/Sprites/{DisplaySpriteId(id)}"));
            Ui.Place(sprite.rectTransform, new Vector2(0, 0.5f), new Vector2(12, 0), new Vector2(64, 64));

            var name = Ui.Label(row, "Name", def.Name, 24, Ui.Ink, TextAnchor.UpperLeft);
            Ui.Place(name.rectTransform, new Vector2(0, 1), new Vector2(90, -10), new Vector2(240, 28));
            var lv = Ui.Label(row, "Lv", $"Lv. {_progress.Level}", 20, SummaryOrange, TextAnchor.UpperLeft);
            Ui.Place(lv.rectTransform, new Vector2(0, 1), new Vector2(90, -38), new Vector2(120, 24));

            // HP 条(地图上恒满)
            var track = Ui.Img(row, "HpTrack", HpTrack);
            Ui.Place(track.rectTransform, new Vector2(0, 0), new Vector2(90, 14), new Vector2(200, 14));
            var fill = Ui.Img(track.transform, "HpFill", HpGreen);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(1f, 1f); // 满血
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            var hpText = Ui.Label(row, "HpText", $"{def.MaxHp} / {def.MaxHp}", 18, Ui.Ink, TextAnchor.LowerRight);
            Ui.Place(hpText.rectTransform, new Vector2(1, 0), new Vector2(-12, 10), new Vector2(100, 22));

            var btn = row.gameObject.AddComponent<Button>();
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

            // 内框 + 纵向布局,全部由布局组驱动,不用绝对坐标
            var box = parent.gameObject.AddComponent<Image>();
            box.color = CreamDeep;
            Ui.AddOutline(parent.gameObject);
            var v = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(22, 22, 18, 14);
            v.spacing = 12;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            // 头部:大立绘 | 名字/类型/状态
            ListRow(parent, "DetailHeader", 200, row =>
            {
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 20;
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = true;
                h.childControlHeight = true;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = false;

                var sprite = Ui.SpriteImg(row, "BigSprite", SpriteLib.LargeIcon(DisplaySpriteId(id)));
                sprite.preserveAspect = true;
                var sle = sprite.gameObject.AddComponent<LayoutElement>();
                sle.preferredWidth = 180;
                sle.preferredHeight = 180;

                var info = Ui.Node(row, "Info");
                var ile = info.gameObject.AddComponent<LayoutElement>();
                ile.preferredWidth = 380;
                ile.preferredHeight = 190;
                var iv = info.gameObject.AddComponent<VerticalLayoutGroup>();
                iv.spacing = 12;
                iv.childAlignment = TextAnchor.UpperLeft;
                iv.childControlWidth = true;
                iv.childControlHeight = true;
                iv.childForceExpandWidth = false;
                iv.childForceExpandHeight = false;

                var name = Ui.Label(info, "Name", def.Name.ToUpperInvariant(), 36, TitleGreen, TextAnchor.MiddleLeft);
                var nle = name.gameObject.AddComponent<LayoutElement>();
                nle.preferredWidth = 380;
                nle.preferredHeight = 46;

                var chip = Ui.Img(info, "TypeChip", typeColor);
                Ui.AddOutline(chip.gameObject);
                var cle = chip.gameObject.AddComponent<LayoutElement>();
                cle.preferredWidth = 160;
                cle.preferredHeight = 44;
                var chipText = Ui.Label(chip.transform, "Text", typeLabel, 22, Color.white);
                Ui.Stretch(chipText.rectTransform);

                if (active)
                {
                    var ready = Ui.Label(info, "Ready", "- BATTLE BUDDY -", 22, Ui.Hex("#5c8a3f"), TextAnchor.MiddleLeft);
                    var rle = ready.gameObject.AddComponent<LayoutElement>();
                    rle.preferredWidth = 380;
                    rle.preferredHeight = 40;
                }
                else
                {
                    var pick = Ui.Btn(info, "BtnPick", "Make battle buddy!", 20);
                    pick.image.color = TabActive;
                    var ple = pick.gameObject.AddComponent<LayoutElement>();
                    ple.preferredWidth = 300;
                    ple.preferredHeight = 50;
                    pick.onClick.AddListener(() =>
                    {
                        _progress.ActiveMonId = id;
                        ShowTeam();
                    });
                }
            });

            // 属性表
            int atk = 2 + _progress.AttackBonus;
            var formula = Array.Find(def.Skills, s => s.Id == "flame-formula");
            StatRow(parent, "HP", def.MaxHp.ToString());
            StatRow(parent, "ATK", atk.ToString());
            StatRow(parent, formula != null ? formula.Name.ToUpperInvariant() : "SKILL",
                formula != null ? $"power {formula.Power}" : "-");

            // 进化路线
            BuildEvolutionBlock(parent, id);
        }

        private void StatRow(RectTransform parent, string label, string value)
        {
            ListRow(parent, $"Stat-{label}", 52, row =>
            {
                var img = row.gameObject.AddComponent<Image>();
                img.color = Cream;
                Ui.AddOutline(row.gameObject);
                var l = Ui.Label(row, "L", label, 23, Ui.Ink, TextAnchor.MiddleLeft);
                Ui.Place(l.rectTransform, new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(320, 40));
                var val = Ui.Label(row, "V", value, 23, TitleGreen, TextAnchor.MiddleRight);
                Ui.Place(val.rectTransform, new Vector2(1, 0.5f), new Vector2(-18, 0), new Vector2(180, 40));
            });
        }

        private void BuildEvolutionBlock(RectTransform parent, string id)
        {
            string fromId = id, toId = null, text;
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

            ListRow(parent, "EvoTitle", 30, row =>
                Ui.Stretch(Ui.Label(row, "Text", "- EVOLUTION -", 20, TitleGreen).rectTransform));

            ListRow(parent, "EvoIcons", 64, row =>
            {
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 12;
                h.childAlignment = TextAnchor.MiddleCenter;
                h.childControlWidth = true;
                h.childControlHeight = true;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = false;

                EvoIcon(row, fromId);
                if (toId != null)
                {
                    var arrow = Ui.Label(row, "Arrow", ">", 30, SummaryOrange);
                    var ale = arrow.gameObject.AddComponent<LayoutElement>();
                    ale.preferredWidth = 34;
                    ale.preferredHeight = 40;
                    EvoIcon(row, toId);
                }
            });

            ListRow(parent, "EvoCond", 34, row =>
                Ui.Stretch(Ui.Label(row, "Text", text, 19, color).rectTransform));
        }

        private static void EvoIcon(RectTransform parent, string id)
        {
            var img = Ui.SpriteImg(parent, $"Evo-{id}", SpriteLib.One($"Art/Sprites/{id}"));
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 56;
            le.preferredHeight = 56;
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
