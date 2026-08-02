using System;
using System.Collections;
using System.Collections.Generic;
using Numeria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>
    /// 战斗场景控制器:程序化搭建整个 UGUI 界面并驱动战斗流程。
    /// 布局、节奏、零惩罚重试逻辑均移植自已验证的 Web 原型 main.js。
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        private BattleState _state;
        private Rng _rng;
        private AudioSource _audio;
        private Coroutine _voiceRoutine;

        private RectTransform _canvasRoot;
        private RectTransform _shakeRoot;
        private RectTransform _playerSprite;
        private RectTransform _enemySprite;
        private readonly Image[] _playerCells = new Image[10];
        private readonly Image[] _enemyCells = new Image[10];
        private Text _gemText;
        private Text _logText;
        private GameObject _shieldRow;
        private Button _btnTackle;
        private Button _btnFormula;
        private Button _btnShield;

        private const string OpeningLine = "A wild Duplirock appeared! It has a number shield!";

        private void Awake()
        {
            _state = new BattleState(GameData.Addmander(), GameData.Duplirock());
            _rng = new Rng((uint)Environment.TickCount);
            _audio = gameObject.AddComponent<AudioSource>();
            BuildUi();
            RenderAll();
            Say(OpeningLine);
        }

        // ---------- UI 构建 ----------

        private void BuildUi()
        {
            var canvasGo = new GameObject("BattleCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _canvasRoot = (RectTransform)canvasGo.transform;

            // 震屏容器:所有内容挂在它下面
            _shakeRoot = Ui.Node(_canvasRoot, "ShakeRoot");
            Ui.Stretch(_shakeRoot);

            // 背景
            var bg = Ui.SpriteImg(_shakeRoot, "Background", LoadSprite("Art/Backgrounds/forest-battle"));
            Ui.Stretch(bg.rectTransform);

            // 敌方:名牌右上 + 立绘
            var enemyPlate = BuildPlate("EnemyPlate", new Vector2(1, 1), new Vector2(-200, -30),
                _state.Enemy.Name, _enemyCells, out _);
            _shieldRow = BuildIconRow(enemyPlate, "ShieldRow", "Art/Sprites/shield",
                _state.Enemy.Shield?.ToString() ?? "", Ui.ShieldBlue);
            var enemyImg = Ui.SpriteImg(_shakeRoot, "EnemySprite", LoadSprite("Art/Sprites/duplirock"));
            Ui.Place(enemyImg.rectTransform, new Vector2(1, 1), new Vector2(-40, -40), new Vector2(160, 160));
            _enemySprite = enemyImg.rectTransform;

            // 我方:立绘 + 名牌左下
            var playerImg = Ui.SpriteImg(_shakeRoot, "PlayerSprite", LoadSprite("Art/Sprites/addmander"));
            Ui.Place(playerImg.rectTransform, new Vector2(0, 0), new Vector2(60, 170), new Vector2(160, 160));
            _playerSprite = playerImg.rectTransform;
            var playerPlate = BuildPlate("PlayerPlate", new Vector2(0, 0), new Vector2(240, 130),
                _state.Player.Name, _playerCells, out _);
            var gemRow = BuildIconRow(playerPlate, "GemRow", "Art/Sprites/gem", "2", Ui.GemOrange);
            _gemText = gemRow.GetComponentInChildren<Text>();

            // 战斗日志
            var logBg = Ui.Img(_shakeRoot, "Log", new Color(0.15f, 0.2f, 0.22f, 0.9f));
            Ui.Place(logBg.rectTransform, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(560, 56));
            _logText = Ui.Label(logBg.transform, "LogText", "A wild Duplirock appeared!", 26, Color.white);
            Ui.Stretch(_logText.rectTransform);

            // 行动按钮
            var actions = Ui.Node(_shakeRoot, "Actions");
            actions.anchorMin = new Vector2(0.5f, 0);
            actions.anchorMax = new Vector2(0.5f, 0);
            actions.pivot = new Vector2(0.5f, 0);
            actions.anchoredPosition = new Vector2(0, 24);
            actions.sizeDelta = new Vector2(1100, 90);
            var layout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            _btnTackle = Ui.Btn(actions, "BtnTackle", "Tackle", 26);
            _btnTackle.onClick.AddListener(() => StartCoroutine(TackleRoutine()));
            _btnFormula = Ui.Btn(actions, "BtnFormula", "Flame Formula (3 gems)", 24);
            _btnFormula.onClick.AddListener(() => StartCoroutine(FormulaRoutine()));
            _btnShield = Ui.Btn(actions, "BtnShield", "Break Shield", 26);
            _btnShield.onClick.AddListener(() => StartCoroutine(BreakShieldRoutine()));
        }

        private RectTransform BuildPlate(string name, Vector2 anchor, Vector2 offset,
            string title, Image[] cells, out Text titleText)
        {
            var plate = Ui.Img(_shakeRoot, name, Ui.PlateBg);
            Ui.Place(plate.rectTransform, anchor, offset, new Vector2(280, 130));
            Ui.AddOutline(plate.gameObject);

            titleText = Ui.Label(plate.transform, "Name", title, 26, Ui.Ink, TextAnchor.UpperLeft);
            Ui.Place(titleText.rectTransform, new Vector2(0, 1), new Vector2(14, -10), new Vector2(240, 30));

            var frame = Ui.Node(plate.transform, "TenFrame");
            Ui.Place(frame, new Vector2(0, 1), new Vector2(14, -44), new Vector2(200, 56));
            var grid = frame.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(30, 24);
            grid.spacing = new Vector2(5, 5);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            for (int i = 0; i < 10; i++)
            {
                var cell = Ui.Img(frame, $"Cell{i}", Color.white);
                Ui.AddOutline(cell.gameObject);
                cells[i] = cell;
            }
            return plate.rectTransform;
        }

        private GameObject BuildIconRow(RectTransform plate, string name, string spritePath, string value, Color color)
        {
            var row = Ui.Node(plate, name);
            Ui.Place(row, new Vector2(0, 0), new Vector2(14, 8), new Vector2(120, 26));
            var icon = Ui.SpriteImg(row, "Icon", LoadSprite(spritePath));
            Ui.Place(icon.rectTransform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(24, 24));
            var text = Ui.Label(row, "Value", value, 24, color, TextAnchor.MiddleLeft);
            Ui.Place(text.rectTransform, new Vector2(0, 0.5f), new Vector2(32, 0), new Vector2(80, 26));
            return row.gameObject;
        }

        private static Sprite LoadSprite(string path) => Resources.Load<Sprite>(path);

        // ---------- 渲染 ----------

        private void RenderAll()
        {
            RenderTenFrame(_playerCells, _state.PlayerHp);
            RenderTenFrame(_enemyCells, _state.EnemyHp);
            _gemText.text = _state.Gems.ToString();
            _shieldRow.SetActive(_state.EnemyShielded);
            _btnFormula.interactable = _state.Gems >= 3;
            _btnShield.interactable = _state.EnemyShielded;
        }

        private static void RenderTenFrame(Image[] cells, int hp)
        {
            for (int i = 0; i < cells.Length; i++)
                cells[i].color = i < hp ? Ui.CellOn : Color.white;
        }

        private void SetLog(string text) => _logText.text = text;

        private void SetActionsEnabled(bool on)
        {
            _btnTackle.interactable = on;
            _btnFormula.interactable = on;
            _btnShield.interactable = on;
            if (on) RenderAll(); // 恢复宝石/护盾相关禁用逻辑
        }

        // ---------- 战斗流程 ----------

        private IEnumerator TackleRoutine()
        {
            SetActionsEnabled(false);
            yield return Lunge(_playerSprite, new Vector2(60, 30));
            var result = _state.UseSkill("tackle");
            PopDamage(_enemySprite, $"-{result.Damage}", Ui.Hex("#ffd24a"));
            yield return Flash(_enemySprite);
            RenderAll();
            SetLog($"Tackle hits for {result.Damage}!");
            yield return EndPlayerTurn();
        }

        private IEnumerator FormulaRoutine()
        {
            SetActionsEnabled(false);
            bool? correct = null;
            yield return RunFormulaOverlay(v => correct = v);
            yield return Lunge(_playerSprite, new Vector2(60, 30));
            yield return Projectile(_playerSprite, _enemySprite,
                correct.Value ? Ui.Hex("#ff5a2e") : Ui.Hex("#ffd24a"));
            var result = _state.UseSkill("flame-formula", correct.Value);
            PopDamage(_enemySprite, $"-{result.Damage}", result.Powered ? Ui.Hex("#ff9d3a") : Ui.Hex("#ffd24a"));
            if (result.Powered) StartCoroutine(Shake());
            yield return Flash(_enemySprite);
            RenderAll();
            SetLog(result.Powered ? $"Flame Formula! {result.Damage} damage!" : $"Flame fizzles... still {result.Damage} damage!");
            yield return EndPlayerTurn();
        }

        private IEnumerator BreakShieldRoutine()
        {
            SetActionsEnabled(false);
            bool? ok = null;
            yield return RunMakeTenOverlay(v => ok = v);
            if (ok.Value)
            {
                yield return ShatterShield();
                _state.BreakShield();
                RenderAll();
                StartCoroutine(Shake());
                SetLog("Shield shattered! Double damage for 2 turns!");
                yield return Flash(_enemySprite);
            }
            else
            {
                SetLog("The shield holds...");
            }
            yield return EndPlayerTurn();
        }

        private IEnumerator EndPlayerTurn()
        {
            if (_state.Outcome != BattleOutcome.None) { ShowOutcome(); yield break; }
            yield return new WaitForSeconds(0.6f);
            SetLog($"{_state.Enemy.Name} attacks!");
            yield return Lunge(_enemySprite, new Vector2(-60, -30));
            int dmg = _state.EnemyTurn();
            PopDamage(_playerSprite, $"-{dmg}", Ui.Hex("#ff6b6b"));
            yield return Flash(_playerSprite);
            RenderAll();
            SetLog($"{_state.Player.Name} took {dmg} damage!");
            if (_state.Outcome != BattleOutcome.None) { ShowOutcome(); yield break; }
            yield return new WaitForSeconds(0.5f);
            _state.StartPlayerTurn();
            RenderAll();
            SetLog("Your turn! +2 gems");
            SetActionsEnabled(true);
        }

        private void ShowOutcome()
        {
            bool win = _state.Outcome == BattleOutcome.Win;
            Say(win ? "You win! Addmander got five experience points!" : "Oh no! Let's try again!");

            var banner = Ui.Img(_canvasRoot, "Banner", new Color(0.06f, 0.09f, 0.13f, 0.92f));
            Ui.Stretch(banner.rectTransform);
            var title = Ui.Label(banner.transform, "Title", win ? "YOU WIN!" : "Addmander fainted...", 64, Color.white);
            Ui.Place(title.rectTransform, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(800, 90));
            if (win)
            {
                var xp = Ui.Label(banner.transform, "Xp", "Addmander got +5 XP!", 32, Ui.Hex("#ffe082"));
                Ui.Place(xp.rectTransform, new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(800, 50));
            }
            var again = Ui.Btn(banner.transform, "BtnAgain", win ? "Play Again" : "Try Again", 28);
            Ui.Place(((RectTransform)again.transform), new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(320, 80));
            again.onClick.AddListener(() =>
            {
                var host = new GameObject("Battle");
                host.AddComponent<BattleController>();
                Destroy(gameObject);
            });
        }

        // ---------- 谜题遮罩 ----------

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

        private RectTransform MakeSlot(Transform parent, Vector2 anchor, Vector2 offset)
        {
            var slot = Ui.Img(parent, "Slot", new Color(1f, 1f, 1f, 0.1f));
            Ui.Place(slot.rectTransform, anchor, offset, new Vector2(100, 100));
            var outline = slot.gameObject.AddComponent<Outline>();
            outline.effectColor = Ui.Hex("#ffe082");
            outline.effectDistance = new Vector2(4, -4);
            return slot.rectTransform;
        }

        private IEnumerator RunFormulaOverlay(Action<bool> done)
        {
            var p = PuzzleGenerator.GenerateFormula(_rng, 10);
            var overlay = BuildOverlay(p.Prompt, out var crystalRow);

            // 算式行: a + [slot] = sum
            var eq = Ui.Node(overlay, "Equation");
            Ui.Place(eq, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(600, 110));
            var aText = Ui.Label(eq, "A", p.A.ToString(), 64, Ui.Hex("#ffe082"));
            Ui.Place(aText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-180, 0), new Vector2(80, 90));
            var plus = Ui.Label(eq, "Plus", "+", 64, Ui.Hex("#ffe082"));
            Ui.Place(plus.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-100, 0), new Vector2(60, 90));
            var slot = MakeSlot(eq, new Vector2(0.5f, 0.5f), Vector2.zero);
            var slotText = Ui.Label(slot, "SlotValue", "", 56, Ui.Hex("#ffe082"));
            Ui.Stretch(slotText.rectTransform);
            var equals = Ui.Label(eq, "Equals", "=", 64, Ui.Hex("#ffe082"));
            Ui.Place(equals.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(100, 0), new Vector2(60, 90));
            var sumText = Ui.Label(eq, "Sum", p.Sum.ToString(), 64, Ui.Hex("#ffe082"));
            Ui.Place(sumText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(180, 0), new Vector2(80, 90));

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
                    Say("Great job!");
                    result = true;
                }
                else if (attempts == 1)
                {
                    slotText.text = "";
                    Destroy(crystal.gameObject);
                    BuildHintTenFrame(hint, p.A, p.Sum);
                    Say("Hmm, not quite.", p.Prompt);
                }
                else
                {
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
            Destroy(overlay.gameObject);
            done(result.Value);
        }

        private void BuildHintTenFrame(RectTransform parent, int a, int sum)
        {
            foreach (Transform child in parent) Destroy(child.gameObject);
            var label = Ui.Label(parent, "HintLabel", "Let's count together!", 24, Color.white);
            Ui.Place(label.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(400, 30));
            var frame = Ui.Node(parent, "HintFrame");
            Ui.Place(frame, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(200, 56));
            var grid = frame.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(30, 24);
            grid.spacing = new Vector2(5, 5);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            for (int i = 0; i < 10; i++)
            {
                Color c = i < a ? Ui.Hex("#ffb300")
                    : i < sum ? Ui.Hex("#66bb6a")
                    : new Color(1f, 1f, 1f, 0.2f);
                var cell = Ui.Img(frame, $"HintCell{i}", c);
                Ui.AddOutline(cell.gameObject);
            }
        }

        private IEnumerator RunMakeTenOverlay(Action<bool> done)
        {
            var p = PuzzleGenerator.GenerateMakeTen(_rng, 10, 4);
            var overlay = BuildOverlay(p.Prompt, out var crystalRow);

            var eq = Ui.Node(overlay, "Equation");
            Ui.Place(eq, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(600, 110));
            var slotA = MakeSlot(eq, new Vector2(0.5f, 0.5f), new Vector2(-150, 0));
            var slotAText = Ui.Label(slotA, "V", "", 56, Ui.Hex("#ffe082"));
            Ui.Stretch(slotAText.rectTransform);
            var plus = Ui.Label(eq, "Plus", "+", 64, Ui.Hex("#ffe082"));
            Ui.Place(plus.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-60, 0), new Vector2(60, 90));
            var slotB = MakeSlot(eq, new Vector2(0.5f, 0.5f), new Vector2(30, 0));
            var slotBText = Ui.Label(slotB, "V", "", 56, Ui.Hex("#ffe082"));
            Ui.Stretch(slotBText.rectTransform);
            var equals = Ui.Label(eq, "Equals", "=", 64, Ui.Hex("#ffe082"));
            Ui.Place(equals.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(120, 0), new Vector2(60, 90));
            var target = Ui.Label(eq, "Target", p.Target.ToString(), 64, Ui.Hex("#ffe082"));
            Ui.Place(target.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(210, 0), new Vector2(110, 90));

            int attempts = 0;
            bool? result = null;
            var picked = new List<int>();
            var crystals = new List<Crystal>();

            void Judge()
            {
                attempts++;
                if (PuzzleGenerator.CheckMakeTen(p, picked[0], picked[1]))
                {
                    Say("Shield break!");
                    result = true;
                }
                else if (attempts == 1)
                {
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
                    Say("Nice try! The shield holds for now.");
                    result = false;
                }
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
                    if (picked.Count == 2) StartCoroutine(DelayedJudge());
                };
            }

            IEnumerator DelayedJudge()
            {
                yield return new WaitForSeconds(0.35f);
                Judge();
            }

            Say(p.Prompt);
            yield return new WaitUntil(() => result.HasValue);
            yield return new WaitForSeconds(result.Value ? 0.6f : 0.4f);
            Destroy(overlay.gameObject);
            done(result.Value);
        }

        // ---------- 演出 ----------

        private IEnumerator Lunge(RectTransform rt, Vector2 dir)
        {
            Vector2 home = rt.anchoredPosition;
            float t = 0;
            while (t < 0.15f) { t += Time.deltaTime; rt.anchoredPosition = home + dir * (t / 0.15f); yield return null; }
            t = 0;
            while (t < 0.2f) { t += Time.deltaTime; rt.anchoredPosition = home + dir * (1 - t / 0.2f); yield return null; }
            rt.anchoredPosition = home;
        }

        private IEnumerator Flash(RectTransform rt)
        {
            var img = rt.GetComponent<Image>();
            for (int i = 0; i < 3; i++)
            {
                img.enabled = false;
                yield return new WaitForSeconds(0.06f);
                img.enabled = true;
                yield return new WaitForSeconds(0.06f);
            }
        }

        private void PopDamage(RectTransform target, string text, Color color)
        {
            StartCoroutine(PopDamageRoutine(target, text, color));
        }

        private IEnumerator PopDamageRoutine(RectTransform target, string text, Color color)
        {
            var label = Ui.Label(_canvasRoot, "DmgPop", text, 44, color);
            label.rectTransform.position = target.position + new Vector3(0, 60, 0);
            label.rectTransform.sizeDelta = new Vector2(200, 60);
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.6f);
            outline.effectDistance = new Vector2(2, -2);
            float t = 0;
            Vector3 start = label.rectTransform.position;
            while (t < 0.8f)
            {
                t += Time.deltaTime;
                label.rectTransform.position = start + new Vector3(0, 80 * (t / 0.8f), 0);
                var c = label.color;
                c.a = 1f - Mathf.Clamp01((t - 0.4f) / 0.4f);
                label.color = c;
                yield return null;
            }
            Destroy(label.gameObject);
        }

        private IEnumerator Shake()
        {
            float t = 0;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                _shakeRoot.anchoredPosition = UnityEngine.Random.insideUnitCircle * 10f;
                yield return null;
            }
            _shakeRoot.anchoredPosition = Vector2.zero;
        }

        private IEnumerator Projectile(RectTransform from, RectTransform to, Color color)
        {
            var img = Ui.Img(_canvasRoot, "Projectile", color);
            img.rectTransform.sizeDelta = new Vector2(28, 28);
            Vector3 start = from.position;
            Vector3 end = to.position;
            float t = 0;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                img.rectTransform.position = Vector3.Lerp(start, end, t / 0.35f);
                yield return null;
            }
            Destroy(img.gameObject);
        }

        private IEnumerator ShatterShield()
        {
            Vector3 origin = _shieldRow.transform.position;
            var shards = new List<RectTransform>();
            for (int i = 0; i < 6; i++)
            {
                var shard = Ui.Img(_canvasRoot, $"Shard{i}", Ui.Hex("#3f7fc4"));
                shard.rectTransform.sizeDelta = new Vector2(14, 14);
                shard.rectTransform.position = origin;
                shards.Add(shard.rectTransform);
            }
            float t = 0;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                for (int i = 0; i < shards.Count; i++)
                {
                    float dx = (i - 2.5f) * 40f * (t / 0.5f);
                    float dy = -120f * (t / 0.5f) * (t / 0.5f) + 60f * (t / 0.5f);
                    shards[i].position = origin + new Vector3(dx, dy, 0);
                }
                yield return null;
            }
            foreach (var s in shards) Destroy(s.gameObject);
        }

        // ---------- 语音 ----------

        private void Say(params string[] lines)
        {
            if (_voiceRoutine != null) StopCoroutine(_voiceRoutine);
            _voiceRoutine = StartCoroutine(SayRoutine(lines));
        }

        private IEnumerator SayRoutine(string[] lines)
        {
            _audio.Stop();
            foreach (string line in lines)
            {
                var clip = Resources.Load<AudioClip>("Voice/" + VoiceKeys.Sanitize(line));
                if (clip == null) continue;
                _audio.clip = clip;
                _audio.Play();
                yield return new WaitForSeconds(clip.length + 0.1f);
            }
        }
    }
}
