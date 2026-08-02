using System;
using System.Collections;
using System.Collections.Generic;
using Numeria.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game
{
    public enum BattleEnd { Win, Lose, Caught }

    /// <summary>
    /// 战斗场景控制器:程序化搭建 UGUI 并驱动战斗流程。
    /// 由 MapController 通过 Init 启动,结束后回调 onEnd。
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        private BattleState _state;
        private Rng _rng;
        private Voice _voice;
        private PuzzleUi _puzzles;
        private Action<BattleEnd> _onEnd;

        private RectTransform _canvasRoot;
        private RectTransform _shakeRoot;
        private RectTransform _playerSprite;
        private RectTransform _enemySprite;
        private readonly Image[] _playerCells = new Image[10];
        private readonly Image[] _enemyCells = new Image[10];
        private Text _gemText;
        private GameObject _shieldRow;
        private Text _logText;
        private Button _btnTackle;
        private Button _btnFormula;
        private Button _btnShield;
        private Button _btnCatch;

        private int _tier = 1;
        private string _battleBg = "forest-battle";

        public void Init(CombatantDef enemy, Progress progress, int tier, string battleBg, Action<BattleEnd> onEnd)
        {
            _onEnd = onEnd;
            _tier = tier;
            _battleBg = battleBg;
            _state = new BattleState(GameData.Player(progress.Evolved), enemy);
            _state.PlayerAttackBonus = progress.AttackBonus;
            _rng = new Rng((uint)Environment.TickCount);
            _voice = gameObject.AddComponent<Voice>();
            BuildUi();
            _puzzles = new PuzzleUi(this, _canvasRoot, _rng, lines => _voice.Say(lines));
            RenderAll();
            string opening = enemy.Shield.HasValue
                ? $"A wild {enemy.Name} appeared! It has a number shield!"
                : $"A wild {enemy.Name} appeared!";
            SetLog($"A wild {enemy.Name} appeared!");
            _voice.Say(opening);
        }

        // ---------- UI 构建 ----------

        private void BuildUi()
        {
            var canvasGo = new GameObject("BattleCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _canvasRoot = (RectTransform)canvasGo.transform;

            _shakeRoot = Ui.Node(_canvasRoot, "ShakeRoot");
            Ui.Stretch(_shakeRoot);

            var bg = Ui.SpriteImg(_shakeRoot, "Background", SpriteLib.One($"Art/Backgrounds/{_battleBg}"));
            Ui.Stretch(bg.rectTransform);

            // 敌方
            var enemyPlate = BuildPlate("EnemyPlate", new Vector2(1, 1), new Vector2(-200, -30),
                _state.Enemy.Name, _enemyCells);
            _shieldRow = BuildIconRow(enemyPlate, "ShieldRow", "Art/Sprites/shield",
                _state.Enemy.Shield?.ToString() ?? "", Ui.ShieldBlue);
            var enemyImg = Ui.SpriteImg(_shakeRoot, "EnemySprite",
                SpriteLib.One($"Art/Sprites/{_state.Enemy.Id}"));
            Ui.Place(enemyImg.rectTransform, new Vector2(1, 1), new Vector2(-40, -40), new Vector2(160, 160));
            _enemySprite = enemyImg.rectTransform;

            // 我方
            var playerImg = Ui.SpriteImg(_shakeRoot, "PlayerSprite", SpriteLib.One($"Art/Sprites/{_state.Player.Id}"));
            Ui.Place(playerImg.rectTransform, new Vector2(0, 0), new Vector2(60, 170), new Vector2(160, 160));
            _playerSprite = playerImg.rectTransform;
            var playerPlate = BuildPlate("PlayerPlate", new Vector2(0, 0), new Vector2(240, 130),
                _state.Player.Name, _playerCells);
            var gemRow = BuildIconRow(playerPlate, "GemRow", "Art/Sprites/gem", "2", Ui.GemOrange);
            _gemText = gemRow.GetComponentInChildren<Text>();

            // 日志
            var logBg = Ui.Img(_shakeRoot, "Log", new Color(0.15f, 0.2f, 0.22f, 0.9f));
            Ui.Place(logBg.rectTransform, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(620, 56));
            _logText = Ui.Label(logBg.transform, "LogText", "", 26, Color.white);
            Ui.Stretch(_logText.rectTransform);

            // 行动按钮
            var actions = Ui.Node(_shakeRoot, "Actions");
            actions.anchorMin = new Vector2(0.5f, 0);
            actions.anchorMax = new Vector2(0.5f, 0);
            actions.pivot = new Vector2(0.5f, 0);
            actions.anchoredPosition = new Vector2(0, 24);
            actions.sizeDelta = new Vector2(1200, 90);
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
            _btnCatch = Ui.Btn(actions, "BtnCatch", "Catch!", 26);
            _btnCatch.onClick.AddListener(() => StartCoroutine(CatchRoutine()));
        }

        private RectTransform BuildPlate(string name, Vector2 anchor, Vector2 offset, string title, Image[] cells)
        {
            var plate = Ui.Img(_shakeRoot, name, Ui.PlateBg);
            Ui.Place(plate.rectTransform, anchor, offset, new Vector2(280, 130));
            Ui.AddOutline(plate.gameObject);

            var titleText = Ui.Label(plate.transform, "Name", title, 26, Ui.Ink, TextAnchor.UpperLeft);
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
            var icon = Ui.SpriteImg(row, "Icon", SpriteLib.One(spritePath));
            Ui.Place(icon.rectTransform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(24, 24));
            var text = Ui.Label(row, "Value", value, 24, color, TextAnchor.MiddleLeft);
            Ui.Place(text.rectTransform, new Vector2(0, 0.5f), new Vector2(32, 0), new Vector2(80, 26));
            return row.gameObject;
        }

        // ---------- 渲染 ----------

        private void RenderAll()
        {
            RenderTenFrame(_playerCells, _state.PlayerHp);
            RenderTenFrame(_enemyCells, _state.EnemyHp);
            _gemText.text = _state.Gems.ToString();
            _shieldRow.SetActive(_state.EnemyShielded);
            _btnFormula.interactable = _state.Gems >= 3;
            _btnShield.gameObject.SetActive(_state.Enemy.Shield.HasValue);
            _btnShield.interactable = _state.EnemyShielded;
            _btnCatch.gameObject.SetActive(
                _state.Enemy.Catchable && _state.EnemyHp > 0 && _state.EnemyHp <= 3);
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
            _btnCatch.interactable = on;
            if (on) RenderAll();
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
            yield return _puzzles.RunFormula(v => correct = v, _tier);
            yield return Lunge(_playerSprite, new Vector2(60, 30));
            yield return Projectile(_playerSprite, _enemySprite,
                correct.Value ? Ui.Hex("#ff5a2e") : Ui.Hex("#ffd24a"));
            var result = _state.UseSkill("flame-formula", correct.Value);
            PopDamage(_enemySprite, $"-{result.Damage}", result.Powered ? Ui.Hex("#ff9d3a") : Ui.Hex("#ffd24a"));
            if (result.Powered) StartCoroutine(Shake());
            yield return Flash(_enemySprite);
            RenderAll();
            SetLog(result.Powered
                ? $"Flame Formula! {result.Damage} damage!"
                : $"Flame fizzles... still {result.Damage} damage!");
            yield return EndPlayerTurn();
        }

        private IEnumerator BreakShieldRoutine()
        {
            SetActionsEnabled(false);
            bool? ok = null;
            yield return _puzzles.RunMakeTen(v => ok = v, _state.Enemy.Shield ?? 10);
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

        private IEnumerator CatchRoutine()
        {
            SetActionsEnabled(false);
            SetLog($"{_state.Enemy.Name} is watching you...");
            bool? correct = null;
            yield return _puzzles.RunFormula(v => correct = v, _tier);
            if (correct.Value)
            {
                _voice.Say($"Gotcha! {_state.Enemy.Name} joined your team!");
                yield return HeartBurst(_enemySprite);
                ShowBanner($"Caught {_state.Enemy.Name}!", "+5 XP", () => _onEnd(BattleEnd.Caught));
            }
            else
            {
                SetLog($"{_state.Enemy.Name} slipped away... try again!");
                yield return EndPlayerTurn();
            }
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
            _voice.Say(win ? $"You win! {_state.Player.Name} got five experience points!" : "Oh no! Let's try again!");
            ShowBanner(win ? "YOU WIN!" : "Addmander fainted...", win ? "+5 XP" : "",
                () => _onEnd(win ? BattleEnd.Win : BattleEnd.Lose));
        }

        private void ShowBanner(string title, string subtitle, Action onContinue)
        {
            var banner = Ui.Img(_canvasRoot, "Banner", new Color(0.06f, 0.09f, 0.13f, 0.92f));
            Ui.Stretch(banner.rectTransform);
            var titleText = Ui.Label(banner.transform, "Title", title, 64, Color.white);
            Ui.Place(titleText.rectTransform, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(900, 90));
            if (!string.IsNullOrEmpty(subtitle))
            {
                var sub = Ui.Label(banner.transform, "Sub", subtitle, 32, Ui.Hex("#ffe082"));
                Ui.Place(sub.rectTransform, new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(800, 50));
            }
            var btn = Ui.Btn(banner.transform, "BtnContinue", "Continue", 28);
            Ui.Place((RectTransform)btn.transform, new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(320, 80));
            btn.onClick.AddListener(() => onContinue());
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

        private IEnumerator HeartBurst(RectTransform target)
        {
            var hearts = new List<RectTransform>();
            for (int i = 0; i < 5; i++)
            {
                var heart = Ui.Label(_canvasRoot, $"Heart{i}", "♥", 40, Ui.Hex("#ff6b8a"));
                heart.rectTransform.position = target.position;
                hearts.Add(heart.rectTransform);
            }
            float t = 0;
            while (t < 0.7f)
            {
                t += Time.deltaTime;
                for (int i = 0; i < hearts.Count; i++)
                {
                    float angle = (i / 5f) * Mathf.PI * 2 + 1;
                    hearts[i].position = target.position + new Vector3(
                        Mathf.Cos(angle) * 90f * (t / 0.7f),
                        Mathf.Sin(angle) * 90f * (t / 0.7f) + 30f * (t / 0.7f), 0);
                }
                yield return null;
            }
            foreach (var h in hearts) Destroy(h.gameObject);
        }
    }
}
