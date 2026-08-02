using System;
using Numeria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>
    /// 状态菜单(一页看全,照顾低龄玩家):队伍、进度、设置、退出。
    /// 全部用 LayoutGroup 自动排版,内容按序堆叠不重叠。
    /// </summary>
    public static class MenuUi
    {
        /// <summary>打开菜单。onClose 在关闭时回调;onReset 在确认重置存档后回调。</summary>
        public static void Open(RectTransform canvasRoot, Progress progress, Action onClose, Action onReset)
        {
            var overlay = Ui.Img(canvasRoot, "MenuOverlay", new Color(0.06f, 0.09f, 0.13f, 0.94f));
            Ui.Stretch(overlay.rectTransform);

            var panel = Ui.Img(overlay.transform, "MenuPanel", Ui.PlateBg);
            Ui.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 0));
            Ui.AddOutline(panel.gameObject);

            var column = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(36, 36, 26, 26);
            column.spacing = 14;
            column.childAlignment = TextAnchor.UpperCenter;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Row(panel.transform, "Title", 56, row =>
                Fill(Ui.Label(row, "TitleText", "NUMERIA", 44, Ui.Ink)));

            // ---- Your Team ----
            Row(panel.transform, "SectionTeam", 26, row =>
                Fill(Ui.Label(row, "Text", "Your Team", 22, Ui.GemOrange, TextAnchor.MiddleLeft)));
            Row(panel.transform, "TeamRow", 168, row =>
            {
                var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 18;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                TeamCard(row, "addmander", $"Addmander\nLv.{progress.Level}");
                foreach (string id in progress.CaughtIds)
                {
                    var def = GameData.ById(id);
                    if (def != null) TeamCard(row, id, $"{def.Name}\nFriend");
                }
            });

            // ---- Progress ----
            Row(panel.transform, "SectionProgress", 26, row =>
                Fill(Ui.Label(row, "Text", "Progress", 22, Ui.GemOrange, TextAnchor.MiddleLeft)));
            string portal = progress.BossBeaten ? "OPEN" : "locked";
            Row(panel.transform, "Stats", 72, row =>
                Fill(Ui.Label(row, "Text",
                    $"Level {progress.Level}   XP {progress.Xp}/{progress.XpToNext}   ATK +{progress.AttackBonus}\n" +
                    $"Chests {progress.OpenedChests.Count}/2   Portal: {portal}   Friends: {progress.CaughtIds.Count}",
                    26, Ui.Ink)));

            // ---- Settings ----
            Row(panel.transform, "SectionSettings", 26, row =>
                Fill(Ui.Label(row, "Text", "Settings", 22, Ui.GemOrange, TextAnchor.MiddleLeft)));
            Row(panel.transform, "SettingsRow", 72, row =>
            {
                ButtonRowLayout(row);
                var voiceBtn = Ui.Btn(row, "BtnVoice", VoiceLabel(progress), 24);
                voiceBtn.onClick.AddListener(() =>
                {
                    progress.VoiceEnabled = !progress.VoiceEnabled;
                    Voice.Enabled = progress.VoiceEnabled;
                    voiceBtn.GetComponentInChildren<Text>().text = VoiceLabel(progress);
                    SaveSystem.Save(progress);
                });
                var resetBtn = Ui.Btn(row, "BtnReset", "Reset Adventure", 24);
                resetBtn.onClick.AddListener(() => ConfirmReset(canvasRoot, () =>
                {
                    UnityEngine.Object.Destroy(overlay.gameObject);
                    onReset();
                }));
            });

            // ---- 底部:继续 / 退出 ----
            Row(panel.transform, "BottomRow", 84, row =>
            {
                ButtonRowLayout(row);
                var continueBtn = Ui.Btn(row, "BtnContinue", "Continue", 28);
                continueBtn.onClick.AddListener(() =>
                {
                    UnityEngine.Object.Destroy(overlay.gameObject);
                    onClose();
                });
                var quitBtn = Ui.Btn(row, "BtnQuit", "Quit Game", 28);
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

        /// <summary>竖排里的一行:固定高度,内部再自由布局。</summary>
        private static void Row(Transform parent, string name, float height, Action<RectTransform> build)
        {
            var row = Ui.Node(parent, name);
            var le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            build(row);
        }

        /// <summary>行内单个元素铺满整行。</summary>
        private static void Fill(Text text) => Ui.Stretch(text.rectTransform);

        /// <summary>并排按钮行:等宽平分。</summary>
        private static void ButtonRowLayout(RectTransform row)
        {
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }

        private static string VoiceLabel(Progress p) => p.VoiceEnabled ? "Voice: ON" : "Voice: OFF";

        private static void TeamCard(RectTransform parent, string id, string caption)
        {
            var card = Ui.Img(parent, $"Card-{id}", Color.white);
            card.rectTransform.sizeDelta = new Vector2(148, 160);
            Ui.AddOutline(card.gameObject);

            var sprite = Ui.SpriteImg(card.transform, "Sprite", SpriteLib.One($"Art/Sprites/{id}"));
            Ui.Place(sprite.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -12), new Vector2(92, 92));

            var name = Ui.Label(card.transform, "Name", caption, 20, Ui.Ink);
            Ui.Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 28), new Vector2(148, 50));
        }

        private static void ConfirmReset(RectTransform canvasRoot, Action onConfirm)
        {
            var confirm = Ui.Img(canvasRoot, "ConfirmOverlay", new Color(0, 0, 0, 0.7f));
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
                onConfirm();
            });

            var no = Ui.Btn(panel.transform, "BtnNo", "No, keep going", 24);
            Ui.Place((RectTransform)no.transform, new Vector2(0.5f, 0f), new Vector2(160, 60), new Vector2(280, 74));
            no.onClick.AddListener(() => UnityEngine.Object.Destroy(confirm.gameObject));
        }
    }
}
