using System;
using Numeria.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    public partial class MenuUi
    {
        private static readonly Color Amber = Ui.Hex("#f2b04e");
        private void ConfirmConversion(Image first, string id, MathmonConversionReward reward)
        {
            // Keep the first dialog underneath. Cancelling the final check never changes the roster.
            string name = GameData.ById(_progress.CurrentFormId(id)).Name;
            int level = _progress.EnsureGrowth(id).Level;
            int amount = reward == MathmonConversionReward.Coins ? MathmonConversionSystem.CoinsForLevel(level) : MathmonConversionSystem.XpForLevel(level);
            ConfirmAction("FINAL CHECK - LET GO?", $"{name}  Lv. {level}\nReceive {amount} {(reward == MathmonConversionReward.Coins ? "COINS" : "XP")}.\nThis permanently removes this Mathmon.\nIts accessories will be kept.",
                "YES, LET GO", () => CompleteConversion(first, id, reward), true);
        }

        private void ConfirmAction(string title, string message, string action, Action confirmed, bool delayed = false)
        {
            var shade = Ui.Img(_overlay.transform, "FinalConfirmation", new Color(0, 0, 0, .85f));
            Ui.Stretch(shade.rectTransform);
            var panel = Ui.Img(shade.transform, "Panel", Cream);
            Ui.PlaceCentered(panel.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(780, 420));
            Ui.AddOutline(panel.gameObject);
            var heading = Ui.Label(panel.transform, "Title", title, 32, TitleGreen);
            Ui.Place(heading.rectTransform, new Vector2(.5f, 1), new Vector2(0, -24), new Vector2(720, 54));
            var detail = Ui.Label(panel.transform, "Message", message, 25, SummaryOrange);
            detail.textWrappingMode = TextWrappingModes.Normal;
            Ui.PlaceCentered(detail.rectTransform, new Vector2(.5f, .5f), new Vector2(0, 24), new Vector2(710, 190));
            var cancel = Ui.Btn(panel.transform, "Cancel", "CANCEL / KEEP", 23);
            Ui.Place((RectTransform)cancel.transform, new Vector2(.5f, 0), new Vector2(-188, 32), new Vector2(330, 66));
            var yes = Ui.Btn(panel.transform, "Confirm", action, 23);
            Ui.Place((RectTransform)yes.transform, new Vector2(.5f, 0), new Vector2(188, 32), new Vector2(330, 66));
            if (delayed) { yes.interactable = false; yes.gameObject.AddComponent<ConfirmationDelay>().Arm(yes); }
            bool used = false;
            cancel.onClick.AddListener(() => { used = true; UnityEngine.Object.Destroy(shade.gameObject); });
            yes.onClick.AddListener(() =>
            {
                if (used || !yes.interactable) return;
                used = true; yes.interactable = false; confirmed(); UnityEngine.Object.Destroy(shade.gameObject);
            });
        }

        private void BuildMathSettings(RectTransform content)
        {
            _progress.Learning.Normalize();
            SectionRow(content, "Math learning - choose a grade");
            TMP_Text gradeText = null, scopeText = null;
            var modes = new Button[4];
            void Refresh()
            {
                gradeText.text = MathSettings.GradeName(_progress.Learning.Grade);
                int chapter = Maps.Get(_progress.CurrentMap).Tier;
                scopeText.text = MathSettings.Scope(_progress.Learning.Grade) + $"\nCHAPTER {chapter}/8 - difficulty rises within this grade and mode.";
                for (int i = 0; i < modes.Length; i++) if (modes[i] != null)
                    modes[i].image.color = (int)_progress.Learning.Mode == i ? Amber : Cream;
            }
            ListRow(content, "GradeLabel", 48, row =>
            {
                gradeText = Ui.Label(row, "Value", "", 30, TitleGreen); Ui.Stretch(gradeText.rectTransform);
            });
            ListRow(content, "GradeSlider", 82, row =>
            {
                var root = Ui.Node(row, "Grade");
                Ui.Stretch(root); root.offsetMin = new Vector2(60, 26); root.offsetMax = new Vector2(-60, -26);
                var track = Ui.Img(root, "Track", Ui.Hex("#b8ab80")); Ui.Stretch(track.rectTransform);
                var handleArea = Ui.Node(root, "HandleArea"); Ui.Stretch(handleArea);
                var handle = Ui.Img(handleArea, "Handle", Amber); handle.rectTransform.sizeDelta = new Vector2(46, 54);
                Ui.AddOutline(handle.gameObject);
                var slider = root.gameObject.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 5; slider.wholeNumbers = true;
                slider.handleRect = handle.rectTransform; slider.targetGraphic = handle; slider.SetValueWithoutNotify(_progress.Learning.Grade);
                slider.onValueChanged.AddListener(value => { _progress.Learning.Grade = (int)value; Refresh(); SaveSystem.Save(_progress); });
            });
            ListRow(content, "GradeTicks", 36, row =>
            {
                var ticks = Ui.Node(row, "Ticks"); Ui.Stretch(ticks); ticks.offsetMin = new Vector2(60,0); ticks.offsetMax = new Vector2(-60,0);
                for (int i=0;i<6;i++) { var label = Ui.Label(ticks, "Grade"+i, i==0?"K":i.ToString(), 23, TitleGreen); Ui.PlaceCentered(label.rectTransform,new Vector2(i/5f,.5f),Vector2.zero,new Vector2(40,34)); }
            });
            ListRow(content, "GradeScope", 94, row =>
            {
                scopeText = Ui.Label(row, "Scope", "", 24, SummaryOrange); Ui.Stretch(scopeText.rectTransform);
                scopeText.textWrappingMode = TextWrappingModes.Normal;
            });
            ListRow(content, "MathModes", 66, row =>
            {
                FillButtonRow(row);
                string[] names = { "NORMAL", "FEEL GOOD", "CHALLENGING", "RANDOM" };
                for (int i = 0; i < 4; i++)
                {
                    int index = i; modes[i] = Ui.Btn(row, "Mode-" + i, names[i], 23);
                    modes[i].onClick.AddListener(() => { _progress.Learning.Mode = (MathMode)index; Refresh(); SaveSystem.Save(_progress); });
                }
            });
            ListRow(content, "MathHelp", 68, row =>
            {
                var help = Ui.Label(row, "Help", "Feel Good: smaller steps + hints. Challenging: harder problems within this grade.\nRandom mixes these difficulties, never grades. Each grade remembers its own mode.", 21, TitleGreen);
                Ui.Stretch(help.rectTransform); help.textWrappingMode = TextWrappingModes.Normal;
            });
            Refresh();
        }
    }

    public sealed class ConfirmationDelay : MonoBehaviour
    {
        private Button _button;
        private float _ready;
        public void Arm(Button button) { _button = button; _ready = Time.unscaledTime + .8f; }
        private void Update()
        {
            if (_button != null && Time.unscaledTime >= _ready) { _button.interactable = true; enabled = false; }
        }
    }
}
