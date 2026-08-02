using System;
using System.Collections.Generic;
using Numeria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>
    /// 状态菜单:TEAM / ITEMS / SETTINGS 三个 tab,内容区可滚动。
    /// TEAM 列表点击数灵进入详情页(状态、装备加成、进化路线与条件、设为出战)。
    /// </summary>
    public class MenuUi
    {
        private readonly Progress _progress;
        private readonly Action _onClose;
        private readonly Action _onReset;
        private readonly Action<string> _onTravel;
        private readonly RectTransform _canvasRoot;

        private Image _overlay;
        private RectTransform _content;
        private readonly Dictionary<string, Button> _tabButtons = new Dictionary<string, Button>();

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
        }

        // ---------- 框架 ----------

        private void Build()
        {
            _overlay = Ui.Img(_canvasRoot, "MenuOverlay", new Color(0.06f, 0.09f, 0.13f, 0.94f));
            Ui.Stretch(_overlay.rectTransform);

            var panel = Ui.Img(_overlay.transform, "MenuPanel", Ui.PlateBg);
            Ui.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080, 800));
            Ui.AddOutline(panel.gameObject);
            var column = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(24, 24, 18, 18);
            column.spacing = 12;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            // 标题行:NUMERIA + 进度摘要 + 关闭
            FixedRow(panel.transform, "Header", 56, header =>
            {
                var h = header.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.childControlWidth = false;
                h.childControlHeight = true;
                h.childForceExpandHeight = true;
                h.spacing = 16;
                var title = Ui.Label(header, "Title", "NUMERIA", 36, Ui.Ink, TextAnchor.MiddleLeft);
                title.gameObject.AddComponent<LayoutElement>().preferredWidth = 260;
                var summary = Ui.Label(header, "Summary",
                    $"Lv.{_progress.Level}  XP {_progress.Xp}/{_progress.XpToNext}  ATK +{_progress.AttackBonus}",
                    22, Ui.GemOrange, TextAnchor.MiddleLeft);
                summary.gameObject.AddComponent<LayoutElement>().preferredWidth = 480;
                var close = Ui.Btn(header, "BtnClose", "Close", 22);
                var le = close.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 180;
                close.onClick.AddListener(() =>
                {
                    UnityEngine.Object.Destroy(_overlay.gameObject);
                    _onClose();
                });
            });

            // Tab 行
            FixedRow(panel.transform, "Tabs", 60, tabs =>
            {
                var h = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 12;
                h.childControlWidth = true;
                h.childControlHeight = true;
                h.childForceExpandWidth = true;
                h.childForceExpandHeight = true;
                _tabButtons["team"] = TabButton(tabs, "TEAM", ShowTeamList);
                _tabButtons["items"] = TabButton(tabs, "ITEMS", ShowItems);
                _tabButtons["settings"] = TabButton(tabs, "SETTINGS", ShowSettings);
            });

            // 可滚动内容区
            BuildScrollArea(panel.transform);

            ShowTeamList();
        }

        private Button TabButton(RectTransform parent, string label, Action onClick)
        {
            var btn = Ui.Btn(parent, $"Tab-{label}", label, 24);
            btn.onClick.AddListener(() => onClick());
            return btn;
        }

        private void SelectTab(string key)
        {
            foreach (var pair in _tabButtons)
                pair.Value.image.color = pair.Key == key ? Ui.Hex("#ffe6ad") : Ui.PlateBg;
        }

        private void BuildScrollArea(Transform parent)
        {
            var area = Ui.Node(parent, "ScrollArea");
            var le = area.gameObject.AddComponent<LayoutElement>();
            le.flexibleHeight = 1;
            var bg = area.gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.04f);
            area.gameObject.AddComponent<RectMask2D>();
            var scroll = area.gameObject.AddComponent<ScrollRect>();

            _content = Ui.Node(area, "Content");
            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1);
            _content.offsetMin = new Vector2(6, 0);
            _content.offsetMax = new Vector2(-6, 0);
            var v = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(8, 8, 8, 8);
            v.spacing = 10;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = _content;
            scroll.viewport = area;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
        }

        private void ClearContent()
        {
            foreach (Transform child in _content) UnityEngine.Object.Destroy(child.gameObject);
            _content.anchoredPosition = Vector2.zero; // 回到顶部
        }

        private static void FixedRow(Transform parent, string name, float height, Action<RectTransform> build)
        {
            var row = Ui.Node(parent, name);
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            build(row);
        }

        private void ContentRow(string name, float height, Action<RectTransform> build) =>
            FixedRow(_content, name, height, build);

        private void SectionLabel(string text) =>
            ContentRow($"Section-{text}", 30, row =>
                Ui.Stretch(Ui.Label(row, "Text", text, 22, Ui.GemOrange, TextAnchor.MiddleLeft).rectTransform));

        // ---------- TEAM ----------

        private List<string> TeamIds()
        {
            var ids = new List<string> { "addmander" };
            ids.AddRange(_progress.CaughtIds);
            return ids;
        }

        private string DisplaySpriteId(string id) =>
            id == "addmander" && _progress.Evolved ? "sumdrake" : id;

        private void ShowTeamList()
        {
            SelectTab("team");
            ClearContent();
            SectionLabel("Your Team - tap a Mathmon for details");

            foreach (string id in TeamIds())
            {
                var def = GameData.PlayerMon(id, _progress.Evolved);
                bool active = id == _progress.ActiveMonId;
                string capturedId = id;
                ContentRow($"Mon-{id}", 92, row =>
                {
                    var rowImg = row.gameObject.AddComponent<Image>();
                    rowImg.color = active ? Ui.Hex("#d7f0c8") : Color.white;
                    Ui.AddOutline(row.gameObject);
                    var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                    h.padding = new RectOffset(14, 14, 8, 8);
                    h.spacing = 18;
                    h.childAlignment = TextAnchor.MiddleLeft;
                    h.childControlWidth = false;
                    h.childControlHeight = false;

                    var sprite = Ui.SpriteImg(row, "Sprite", SpriteLib.One($"Art/Sprites/{DisplaySpriteId(capturedId)}"));
                    var sle = sprite.gameObject.AddComponent<LayoutElement>();
                    sle.preferredWidth = 68;
                    sle.preferredHeight = 68;

                    var name = Ui.Label(row, "Name", $"{def.Name}   Lv.{_progress.Level}", 26, Ui.Ink, TextAnchor.MiddleLeft);
                    name.gameObject.AddComponent<LayoutElement>().preferredWidth = 560;

                    var badge = Ui.Label(row, "Badge", active ? "BATTLE BUDDY" : "", 20, Ui.Hex("#2e7d32"), TextAnchor.MiddleRight);
                    badge.gameObject.AddComponent<LayoutElement>().preferredWidth = 260;

                    var btn = row.gameObject.AddComponent<Button>();
                    btn.onClick.AddListener(() => ShowMonDetail(capturedId));
                });
            }
        }

        private void ShowMonDetail(string id)
        {
            SelectTab("team");
            ClearContent();
            var def = GameData.PlayerMon(id, _progress.Evolved);

            ContentRow("Back", 52, row =>
            {
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.childControlWidth = false;
                h.childControlHeight = true;
                h.childForceExpandHeight = true;
                var back = Ui.Btn(row, "BtnBack", "< Back to team", 20);
                back.gameObject.AddComponent<LayoutElement>().preferredWidth = 280;
                back.onClick.AddListener(ShowTeamList);
            });

            // 头部:大立绘 + 名字/等级
            ContentRow("MonHeader", 140, row =>
            {
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 24;
                h.padding = new RectOffset(10, 10, 6, 6);
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = false;
                h.childControlHeight = false;
                var sprite = Ui.SpriteImg(row, "Sprite", SpriteLib.One($"Art/Sprites/{DisplaySpriteId(id)}"));
                var sle = sprite.gameObject.AddComponent<LayoutElement>();
                sle.preferredWidth = 120;
                sle.preferredHeight = 120;
                var name = Ui.Label(row, "Name", $"{def.Name}\nLv.{_progress.Level}", 30, Ui.Ink, TextAnchor.MiddleLeft);
                name.gameObject.AddComponent<LayoutElement>().preferredWidth = 500;
            });

            // 状态
            SectionLabel("Stats");
            int gearBonus = 0;
            foreach (string item in _progress.Items) if (item != "Evolution Stone") gearBonus++;
            int levelBonus = _progress.AttackBonus - gearBonus;
            ContentRow("Stats", 84, row =>
                Ui.Stretch(Ui.Label(row, "Text",
                    $"HP 10    ATK bonus +{_progress.AttackBonus}  (levels +{levelBonus}, gear +{gearBonus})",
                    24, Ui.Ink, TextAnchor.MiddleLeft).rectTransform));

            // 技能
            SectionLabel("Skills");
            foreach (var skill in def.Skills)
            {
                var captured = skill;
                ContentRow($"Skill-{skill.Id}", 44, row =>
                    Ui.Stretch(Ui.Label(row, "Text",
                        captured.Cost > 0
                            ? $"{captured.Name}   ({captured.Cost} gems, power {captured.Power})"
                            : $"{captured.Name}   (free, power {captured.Power})",
                        22, Ui.Ink, TextAnchor.MiddleLeft).rectTransform));
            }

            // 装备加成
            SectionLabel("Gear");
            if (gearBonus == 0)
            {
                ContentRow("NoGear", 44, row =>
                    Ui.Stretch(Ui.Label(row, "Text", "No gear yet - open math chests!", 22, Ui.Ink, TextAnchor.MiddleLeft).rectTransform));
            }
            else
            {
                foreach (string item in _progress.Items)
                {
                    if (item == "Evolution Stone") continue;
                    string captured = item;
                    ContentRow($"Gear-{item}", 44, row =>
                        Ui.Stretch(Ui.Label(row, "Text", $"{captured}   (+1 ATK)", 22, Ui.Ink, TextAnchor.MiddleLeft).rectTransform));
                }
            }

            // 进化路线
            SectionLabel("Evolution");
            BuildEvolutionRow(id);

            // 设为出战
            ContentRow("MakeActive", 72, row =>
            {
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.childControlWidth = true;
                h.childControlHeight = true;
                h.childForceExpandWidth = true;
                h.childForceExpandHeight = true;
                bool active = id == _progress.ActiveMonId;
                var btn = Ui.Btn(row, "BtnActive", active ? "Already your battle buddy!" : "Make battle buddy!", 24);
                btn.interactable = !active;
                btn.onClick.AddListener(() =>
                {
                    _progress.ActiveMonId = id;
                    ShowMonDetail(id);
                });
            });
        }

        private void BuildEvolutionRow(string id)
        {
            string fromId, toId, text;
            Color color = Ui.Ink;
            switch (id)
            {
                case "addmander":
                    fromId = "addmander";
                    toId = "sumdrake";
                    if (_progress.Evolved) { text = "Evolved! Sumdrake learned Blaze Equation!"; color = Ui.Hex("#2e7d32"); }
                    else
                    {
                        string lv = _progress.Level >= 5 ? "YES" : $"now Lv.{_progress.Level}";
                        string stone = _progress.HasEvoStone ? "YES" : "find it in Silent Peaks";
                        text = $"Needs Lv.5 ({lv})\n+ Evolution Stone ({stone})";
                    }
                    break;
                case "countipillar":
                    fromId = "countipillar"; toId = null;
                    text = "Evolves into Numberfly - coming soon!";
                    break;
                case "doublit":
                    fromId = "doublit"; toId = "duplirock";
                    text = "Evolves into Duplirock - coming soon!";
                    break;
                default:
                    fromId = id; toId = null; text = "No evolution.";
                    break;
            }

            ContentRow("EvoRow", 96, row =>
            {
                var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 14;
                h.padding = new RectOffset(10, 10, 6, 6);
                h.childAlignment = TextAnchor.MiddleLeft;
                h.childControlWidth = false;
                h.childControlHeight = false;

                EvoSprite(row, fromId);
                if (toId != null)
                {
                    var arrow = Ui.Label(row, "Arrow", ">", 40, Ui.GemOrange);
                    var ale = arrow.gameObject.AddComponent<LayoutElement>();
                    ale.preferredWidth = 44;
                    ale.preferredHeight = 80;
                    EvoSprite(row, toId);
                }
                var cond = Ui.Label(row, "Cond", text, 22, color, TextAnchor.MiddleLeft);
                var cle = cond.gameObject.AddComponent<LayoutElement>();
                cle.preferredWidth = 620;
                cle.preferredHeight = 90;
            });
        }

        private static void EvoSprite(RectTransform parent, string id)
        {
            var img = Ui.SpriteImg(parent, $"Evo-{id}", SpriteLib.One($"Art/Sprites/{id}"));
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 80;
            le.preferredHeight = 80;
        }

        // ---------- ITEMS ----------

        private void ShowItems()
        {
            SelectTab("items");
            ClearContent();
            SectionLabel("Items");
            if (_progress.Items.Count == 0)
            {
                ContentRow("NoItems", 48, row =>
                    Ui.Stretch(Ui.Label(row, "Text", "No items yet - open math chests!", 24, Ui.Ink, TextAnchor.MiddleLeft).rectTransform));
                return;
            }
            foreach (string item in _progress.Items)
            {
                string captured = item;
                string effect = captured == "Evolution Stone" ? "Evolution material" : "+1 ATK";
                ContentRow($"Item-{item}", 64, row =>
                {
                    var rowImg = row.gameObject.AddComponent<Image>();
                    rowImg.color = Ui.Hex("#fff3d6");
                    Ui.AddOutline(row.gameObject);
                    var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                    h.padding = new RectOffset(14, 14, 8, 8);
                    h.spacing = 16;
                    h.childAlignment = TextAnchor.MiddleLeft;
                    h.childControlWidth = false;
                    h.childControlHeight = false;
                    var icon = Ui.SpriteImg(row, "Icon", SpriteLib.One("Art/Sprites/gem"));
                    var ile = icon.gameObject.AddComponent<LayoutElement>();
                    ile.preferredWidth = 36;
                    ile.preferredHeight = 36;
                    var name = Ui.Label(row, "Name", captured, 24, Ui.Ink, TextAnchor.MiddleLeft);
                    name.gameObject.AddComponent<LayoutElement>().preferredWidth = 480;
                    var eff = Ui.Label(row, "Effect", effect, 22, Ui.GemOrange, TextAnchor.MiddleRight);
                    eff.gameObject.AddComponent<LayoutElement>().preferredWidth = 360;
                });
            }
        }

        // ---------- SETTINGS ----------

        private void ShowSettings()
        {
            SelectTab("settings");
            ClearContent();

            SectionLabel("Sound");
            ContentRow("VoiceRow", 64, row =>
            {
                FillButtonRow(row, out var h);
                var voiceBtn = Ui.Btn(row, "BtnVoice", _progress.VoiceEnabled ? "Voice: ON" : "Voice: OFF", 24);
                voiceBtn.onClick.AddListener(() =>
                {
                    _progress.VoiceEnabled = !_progress.VoiceEnabled;
                    Voice.Enabled = _progress.VoiceEnabled;
                    voiceBtn.GetComponentInChildren<Text>().text = _progress.VoiceEnabled ? "Voice: ON" : "Voice: OFF";
                    SaveSystem.Save(_progress);
                });
            });

            SectionLabel("Travel");
            ContentRow("TravelRow", 64, row =>
            {
                FillButtonRow(row, out var h);
                var forestBtn = Ui.Btn(row, "BtnForest", "Mystic Forest", 22);
                forestBtn.interactable = _progress.CurrentMap != "forest";
                forestBtn.onClick.AddListener(() => { CloseThen(() => _onTravel("forest")); });
                var peaksBtn = Ui.Btn(row, "BtnPeaks",
                    _progress.BossBeaten ? "Silent Peaks" : "Silent Peaks (locked)", 22);
                peaksBtn.interactable = _progress.BossBeaten && _progress.CurrentMap != "mountains";
                peaksBtn.onClick.AddListener(() => { CloseThen(() => _onTravel("mountains")); });
            });

            SectionLabel("Danger Zone");
            ContentRow("DangerRow", 64, row =>
            {
                FillButtonRow(row, out var h);
                var resetBtn = Ui.Btn(row, "BtnReset", "Reset Adventure", 22);
                resetBtn.onClick.AddListener(() => ConfirmReset());
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

        private static void FillButtonRow(RectTransform row, out HorizontalLayoutGroup h)
        {
            h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
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
            var panel = Ui.Img(confirm.transform, "ConfirmPanel", Ui.PlateBg);
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
