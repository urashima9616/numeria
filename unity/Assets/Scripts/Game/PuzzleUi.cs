using System;
using System.Collections;
using System.Collections.Generic;
using Numeria.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>
    /// 谜题遮罩(咒语算式 / 凑十),战斗与地图宝箱共用。
    /// 零惩罚:答错引导重试一次,重试答对同样算成功。
    /// </summary>
    public class PuzzleUi
    {
        private readonly MonoBehaviour _host;
        private readonly RectTransform _canvasRoot;
        private readonly Rng _rng;
        private readonly Action<string[]> _say;

        public PuzzleUi(MonoBehaviour host, RectTransform canvasRoot, Rng rng, Action<string[]> say)
        {
            _host = host;
            _canvasRoot = canvasRoot;
            _rng = rng;
            _say = say;
        }

        private void Say(params string[] lines) => _say(lines);

        // ---------- 通用构件 ----------

        private RectTransform BuildOverlay(string prompt, out RectTransform crystalRow)
        {
            var overlay = Ui.Img(_canvasRoot, "Overlay", new Color(0.06f, 0.09f, 0.13f, 0.88f));
            Ui.Stretch(overlay.rectTransform);

            var promptText = Ui.Label(overlay.transform, "Prompt", prompt, 34, Color.white);
            Ui.Place(promptText.rectTransform, new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(1200, 60));

            crystalRow = Ui.Node(overlay.transform, "Crystals");
            Ui.Place(crystalRow, new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(500, 100));
            var layout = crystalRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 24;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return overlay.rectTransform;
        }

        private Crystal MakeCrystal(Transform parent, int value)
        {
            var img = Ui.Img(parent, $"Crystal{value}", Ui.Hex("#5e35b1"));
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 96;
            le.preferredHeight = 96;
            Ui.AddOutline(img.gameObject);
            var label = Ui.Label(img.transform, "Value", value.ToString(), 40, Color.white);
            Ui.Stretch(label.rectTransform);
            var crystal = img.gameObject.AddComponent<Crystal>();
            crystal.Value = value;
            return crystal;
        }

        private RectTransform MakeSlot(Transform parent, Vector2 offset)
        {
            var slot = Ui.Img(parent, "Slot", new Color(1f, 1f, 1f, 0.1f));
            Ui.Place(slot.rectTransform, new Vector2(0.5f, 0.5f), offset, new Vector2(100, 100));
            var outline = slot.gameObject.AddComponent<Outline>();
            outline.effectColor = Ui.Hex("#ffe082");
            outline.effectDistance = new Vector2(4, -4);
            return slot.rectTransform;
        }

        private TMP_Text EqLabel(Transform parent, string name, string text, Vector2 offset, Vector2 size)
        {
            var label = Ui.Label(parent, name, text, 64, Ui.Hex("#ffe082"));
            Ui.Place(label.rectTransform, new Vector2(0.5f, 0.5f), offset, size);
            return label;
        }

        // ---------- 咒语算式 ----------

        /// <summary>按难度层选题:tier1 = 10 以内加/减;tier2 = 20 以内加/减 + 翻倍。</summary>
        public FormulaPuzzle PickPuzzle(int tier)
        {
            double roll = _rng.Next();
            if (tier >= 2)
            {
                if (roll < 0.4) return PuzzleGenerator.GenerateFormula(_rng, 20);
                if (roll < 0.75) return PuzzleGenerator.GenerateSubtraction(_rng, 20);
                return PuzzleGenerator.GenerateDouble(_rng, 20);
            }
            return roll < 0.65
                ? PuzzleGenerator.GenerateFormula(_rng, 10)
                : PuzzleGenerator.GenerateSubtraction(_rng, 10);
        }

        public IEnumerator RunFormula(Action<bool> done, int tier = 1)
        {
            var p = PickPuzzle(tier);
            var overlay = BuildOverlay(p.Prompt, out var crystalRow);

            var eq = Ui.Node(overlay, "Equation");
            Ui.Place(eq, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(600, 110));
            RectTransform slot;
            if (p.SlotIsResult)
            {
                // A + A = □(翻倍)
                EqLabel(eq, "A", p.A.ToString(), new Vector2(-190, 0), new Vector2(90, 90));
                EqLabel(eq, "Op", p.Op.ToString(), new Vector2(-105, 0), new Vector2(60, 90));
                EqLabel(eq, "A2", p.A.ToString(), new Vector2(-20, 0), new Vector2(90, 90));
                EqLabel(eq, "Equals", "=", new Vector2(70, 0), new Vector2(60, 90));
                slot = MakeSlot(eq, new Vector2(165, 0));
            }
            else
            {
                // A op □ = Sum
                EqLabel(eq, "A", p.A.ToString(), new Vector2(-190, 0), new Vector2(90, 90));
                EqLabel(eq, "Op", p.Op.ToString(), new Vector2(-105, 0), new Vector2(60, 90));
                slot = MakeSlot(eq, Vector2.zero);
                EqLabel(eq, "Equals", "=", new Vector2(105, 0), new Vector2(60, 90));
                EqLabel(eq, "Sum", p.Sum.ToString(), new Vector2(190, 0), new Vector2(90, 90));
            }
            var slotText = Ui.Label(slot, "SlotValue", "", 56, Ui.Hex("#ffe082"));
            Ui.Stretch(slotText.rectTransform);

            var hint = Ui.Node(overlay, "Hint");
            Ui.Place(hint, new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(400, 90));

            int attempts = 0;
            bool? result = null;

            void Submit(int value, Crystal crystal)
            {
                if (result.HasValue) return;
                attempts++;
                slotText.text = value.ToString();
                if (PuzzleGenerator.CheckFormula(p, value))
                {
                    Sfx.Play(SfxCue.Correct);
                    Say("Great job!");
                    result = true;
                }
                else if (attempts == 1)
                {
                    Sfx.Play(SfxCue.SoftMiss, 0.7f);
                    slotText.text = "";
                    UnityEngine.Object.Destroy(crystal.gameObject);
                    // 数块提示:加法/翻倍 = 已有(橙)补到结果(绿);减法 = 总数里剩下(橙)拿走(绿)
                    if (p.Op == '-') BuildHintFrame(hint, p.Sum, p.A);
                    else if (p.SlotIsResult) BuildHintFrame(hint, p.A, p.A * 2);
                    else BuildHintFrame(hint, p.A, p.Sum);
                    Say("Hmm, not quite.", p.Prompt);
                }
                else
                {
                    Sfx.Play(SfxCue.SoftMiss, 0.7f);
                    Say("Nice try! Your move still works!");
                    result = false;
                }
            }

            foreach (int value in p.Candidates)
            {
                var crystal = MakeCrystal(crystalRow, value);
                crystal.SlotRect = slot;
                crystal.OnSubmit = Submit;
            }

            Say(p.Prompt);
            yield return new WaitUntil(() => result.HasValue);
            yield return new WaitForSeconds(result.Value ? 0.6f : 0.4f);
            UnityEngine.Object.Destroy(overlay.gameObject);
            done(result.Value);
        }

        /// <summary>数块提示:前 orange 格橙色、到 total 为绿色。20 以内自动排成两行十格阵。</summary>
        private void BuildHintFrame(RectTransform parent, int orange, int total)
        {
            foreach (Transform child in parent) UnityEngine.Object.Destroy(child.gameObject);
            var label = Ui.Label(parent, "HintLabel", "Let's count together!", 24, Color.white);
            Ui.Place(label.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(400, 30));
            int cellCount = total <= 10 ? 10 : 20;
            var frame = Ui.Node(parent, "HintFrame");
            Ui.Place(frame, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(320, cellCount > 10 ? 56 : 30));
            var grid = frame.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(26, 22);
            grid.spacing = new Vector2(4, 4);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 10;
            for (int i = 0; i < cellCount; i++)
            {
                Color c = i < orange ? Ui.Hex("#ffb300")
                    : i < total ? Ui.Hex("#66bb6a")
                    : new Color(1f, 1f, 1f, 0.2f);
                var cell = Ui.Img(frame, $"HintCell{i}", c);
                Ui.AddOutline(cell.gameObject);
            }
        }

        // ---------- 凑十 ----------

        public IEnumerator RunMakeTen(Action<bool> done, int target = 10)
        {
            var p = PuzzleGenerator.GenerateMakeTen(_rng, target, 4);
            var overlay = BuildOverlay(p.Prompt, out var crystalRow);

            var eq = Ui.Node(overlay, "Equation");
            Ui.Place(eq, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(600, 110));
            var slotA = MakeSlot(eq, new Vector2(-150, 0));
            var slotAText = Ui.Label(slotA, "V", "", 56, Ui.Hex("#ffe082"));
            Ui.Stretch(slotAText.rectTransform);
            EqLabel(eq, "Plus", "+", new Vector2(-60, 0), new Vector2(60, 90));
            var slotB = MakeSlot(eq, new Vector2(30, 0));
            var slotBText = Ui.Label(slotB, "V", "", 56, Ui.Hex("#ffe082"));
            Ui.Stretch(slotBText.rectTransform);
            EqLabel(eq, "Equals", "=", new Vector2(120, 0), new Vector2(60, 90));
            EqLabel(eq, "Target", p.Target.ToString(), new Vector2(210, 0), new Vector2(110, 90));

            int attempts = 0;
            bool? result = null;
            var picked = new List<int>();
            var crystals = new List<Crystal>();

            void Judge()
            {
                attempts++;
                if (PuzzleGenerator.CheckMakeTen(p, picked[0], picked[1]))
                {
                    Sfx.Play(SfxCue.Correct);
                    Say("Shield break!");
                    result = true;
                }
                else if (attempts == 1)
                {
                    Sfx.Play(SfxCue.SoftMiss, 0.7f);
                    Say("Hmm, not quite.", "Try again!");
                    picked.Clear();
                    slotAText.text = "";
                    slotBText.text = "";
                    foreach (var c in crystals) if (c != null) c.transform.localScale = Vector3.one;
                    var pair = PuzzleGenerator.FindMakeTenPair(p.Hand, p.Target);
                    if (pair.HasValue && crystals[pair.Value.i] != null)
                        crystals[pair.Value.i].transform.localScale = Vector3.one * 1.15f; // 高亮提示
                }
                else
                {
                    Sfx.Play(SfxCue.SoftMiss, 0.7f);
                    Say("Nice try! The shield holds for now.");
                    result = false;
                }
            }

            IEnumerator DelayedJudge()
            {
                yield return new WaitForSeconds(0.35f);
                Judge();
            }

            for (int idx = 0; idx < p.Hand.Count; idx++)
            {
                int index = idx;
                var crystal = MakeCrystal(crystalRow, p.Hand[idx]);
                crystals.Add(crystal);
                crystal.OnSubmit = (value, c) =>
                {
                    if (result.HasValue || picked.Contains(index)) return;
                    picked.Add(index);
                    c.transform.localScale = Vector3.one * 1.15f;
                    if (picked.Count == 1) slotAText.text = value.ToString();
                    else slotBText.text = value.ToString();
                    if (picked.Count == 2) _host.StartCoroutine(DelayedJudge());
                };
            }

            Say(p.Prompt);
            yield return new WaitUntil(() => result.HasValue);
            yield return new WaitForSeconds(result.Value ? 0.6f : 0.4f);
            UnityEngine.Object.Destroy(overlay.gameObject);
            done(result.Value);
        }
    }
}
