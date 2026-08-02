using System;
using Numeria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>
    /// 状态菜单(一页看全,照顾低龄玩家):队伍、进度、设置、退出。
    /// </summary>
    public static class MenuUi
    {
        /// <summary>打开菜单。onClose 在关闭时回调;onReset 在确认重置存档后回调。</summary>
        public static void Open(RectTransform canvasRoot, Progress progress, Action onClose, Action onReset)
        {
            var overlay = Ui.Img(canvasRoot, "MenuOverlay", new Color(0.06f, 0.09f, 0.13f, 0.94f));
            Ui.Stretch(overlay.rectTransform);

            var panel = Ui.Img(overlay.transform, "MenuPanel", Ui.PlateBg);
            Ui.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 720));
            Ui.AddOutline(panel.gameObject);

            var title = Ui.Label(panel.transform, "Title", "NUMERIA", 44, Ui.Ink);
            Ui.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -46), new Vector2(600, 60));

            // ---- Your Team ----
            SectionLabel(panel.transform, "Your Team", -110);
            var teamRow = Ui.Node(panel.transform, "TeamRow");
            Ui.Place(teamRow, new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(920, 170));
            var layout = teamRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TeamCard(teamRow, "addmander", $"Addmander\nLv.{progress.Level}");
            foreach (string id in progress.CaughtIds)
            {
                var def = GameData.ById(id);
                if (def != null) TeamCard(teamRow, id, $"{def.Name}\nFriend");
            }

            // ---- Progress ----
            SectionLabel(panel.transform, "Progress", -348);
            string portal = progress.BossBeaten ? "OPEN" : "locked";
            var stats = Ui.Label(panel.transform, "Stats",
                $"Level {progress.Level}   XP {progress.Xp}/{progress.XpToNext}   ATK +{progress.AttackBonus}\n" +
                $"Chests {progress.OpenedChests.Count}/2   Portal: {portal}   Friends: {progress.CaughtIds.Count}",
                26, Ui.Ink);
            Ui.Place(stats.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -400), new Vector2(900, 80));

            // ---- Settings ----
            SectionLabel(panel.transform, "Settings", -478);
            var voiceBtn = Ui.Btn(panel.transform, "BtnVoice", VoiceLabel(progress), 24);
            Ui.Place((RectTransform)voiceBtn.transform, new Vector2(0.5f, 1f), new Vector2(-260, -540), new Vector2(400, 70));
            voiceBtn.onClick.AddListener(() =>
            {
                progress.VoiceEnabled = !progress.VoiceEnabled;
                Voice.Enabled = progress.VoiceEnabled;
                voiceBtn.GetComponentInChildren<Text>().text = VoiceLabel(progress);
                SaveSystem.Save(progress);
            });

            var resetBtn = Ui.Btn(panel.transform, "BtnReset", "Reset Adventure", 24);
            Ui.Place((RectTransform)resetBtn.transform, new Vector2(0.5f, 1f), new Vector2(260, -540), new Vector2(400, 70));
            resetBtn.onClick.AddListener(() => ConfirmReset(canvasRoot, () =>
            {
                UnityEngine.Object.Destroy(overlay.gameObject);
                onReset();
            }));

            // ---- 底部:继续 / 退出 ----
            var continueBtn = Ui.Btn(panel.transform, "BtnContinue", "Continue", 28);
            Ui.Place((RectTransform)continueBtn.transform, new Vector2(0.5f, 0f), new Vector2(-260, 60), new Vector2(400, 84));
            continueBtn.onClick.AddListener(() =>
            {
                UnityEngine.Object.Destroy(overlay.gameObject);
                onClose();
            });

            var quitBtn = Ui.Btn(panel.transform, "BtnQuit", "Quit Game", 28);
            Ui.Place((RectTransform)quitBtn.transform, new Vector2(0.5f, 0f), new Vector2(260, 60), new Vector2(400, 84));
            quitBtn.onClick.AddListener(() =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }

        private static string VoiceLabel(Progress p) => p.VoiceEnabled ? "Voice: ON" : "Voice: OFF";

        private static void SectionLabel(Transform parent, string text, float y)
        {
            var label = Ui.Label(parent, $"Section-{text}", text, 22, Ui.GemOrange, TextAnchor.MiddleLeft);
            Ui.Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(-410, y), new Vector2(500, 30));
        }

        private static void TeamCard(Transform parent, string id, string caption)
        {
            var card = Ui.Img(parent, $"Card-{id}", Color.white);
            var le = card.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 150;
            le.preferredHeight = 160;
            Ui.AddOutline(card.gameObject);

            var sprite = Ui.SpriteImg(card.transform, "Sprite", SpriteLib.One($"Art/Sprites/{id}"));
            Ui.Place(sprite.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(96, 96));

            var name = Ui.Label(card.transform, "Name", caption, 20, Ui.Ink);
            Ui.Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 28), new Vector2(150, 50));
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
            Ui.Place((RectTransform)yes.transform, new Vector2(0.5f, 0f), new Vector2(-160, 50), new Vector2(280, 74));
            yes.onClick.AddListener(() =>
            {
                UnityEngine.Object.Destroy(confirm.gameObject);
                onConfirm();
            });

            var no = Ui.Btn(panel.transform, "BtnNo", "No, keep going", 24);
            Ui.Place((RectTransform)no.transform, new Vector2(0.5f, 0f), new Vector2(160, 50), new Vector2(280, 74));
            no.onClick.AddListener(() => UnityEngine.Object.Destroy(confirm.gameObject));
        }
    }
}
