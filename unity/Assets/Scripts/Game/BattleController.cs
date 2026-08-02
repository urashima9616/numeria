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
        private Image _playerHpFill;
        private Image _enemyHpFill;
        private Text _playerHpText;
        private Text _enemyHpText;
        private readonly List<Image> _gemIcons = new List<Image>();
        private Text _gemLabel;
        private GameObject _shieldRow;
        private Text _logMain;
        private Text _logSub;
        private Button _btnTackle;
        private Button _btnFormula;
        private Image _formulaBg;
        private Button _btnShield;
        private Button _btnCatch;
        private CanvasGroup _dockGroup;
        private int _playerLevel = 1;

        // 参考图配色(与菜单一致)
        private static readonly Color Cream = Ui.Hex("#f6efdc");
        private static readonly Color TitleGreen = Ui.Hex("#3a4d2f");
        private static readonly Color Amber = Ui.Hex("#f2b04e");
        private static readonly Color HpGreen = Ui.Hex("#7ac974");
        private static readonly Color HpTrack = Ui.Hex("#e3d9bd");
        private static readonly Color SubOrange = Ui.Hex("#c77b3a");

        private int _tier = 1;
        private string _battleBg = "forest-battle";

        public void Init(CombatantDef enemy, Progress progress, int tier, string battleBg, Action<BattleEnd> onEnd)
        {
            _onEnd = onEnd;
            _tier = tier;
            _battleBg = battleBg;
            _playerLevel = progress.Level;
            _state = new BattleState(GameData.PlayerMon(progress.ActiveMonId, progress.Evolved), enemy);
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

            var bg = Ui.SpriteImg(_shakeRoot, "Background", SpriteLib.One(_battleBg));
            Ui.Stretch(bg.rectTransform);

            // 敌方:名牌左上,立绘右上(经典宝可梦布局)
            var enemyPlate = BuildStatusPlate("EnemyPlate", new Vector2(0, 1), new Vector2(26, -22), new Vector2(380, 158),
                _state.Enemy.Name, _state.Enemy.Shield.HasValue ? "BOSS" : "WILD",
                out _enemyHpFill, out _enemyHpText);
            BuildShieldRow(enemyPlate);
            var enemyImg = Ui.SpriteImg(_shakeRoot, "EnemySprite", SpriteLib.EnemyBattleSprite(_state.Enemy.Id));
            enemyImg.preserveAspect = true;
            Ui.Place(enemyImg.rectTransform, new Vector2(1, 1), new Vector2(-140, -140), new Vector2(230, 230));
            _enemySprite = enemyImg.rectTransform;

            // 我方:立绘左下,名牌右侧
            var playerImg = Ui.SpriteImg(_shakeRoot, "PlayerSprite", SpriteLib.PlayerBattleSprite(_state.Player.Id));
            playerImg.preserveAspect = true;
            Ui.Place(playerImg.rectTransform, new Vector2(0, 0), new Vector2(190, 330), new Vector2(260, 260));
            _playerSprite = playerImg.rectTransform;
            var playerPlate = BuildStatusPlate("PlayerPlate", new Vector2(1, 0), new Vector2(-26, 210), new Vector2(410, 180),
                _state.Player.Name, $"Lv. {_playerLevel}",
                out _playerHpFill, out _playerHpText);
            BuildGemRow(playerPlate);

            // 回合横幅(素材包 Turn_Banner,顶部居中)
            var logPlate = Ui.SpriteImg(_shakeRoot, "LogPlate", SpriteLib.Pack("UI/Panels/Turn_Banner"));
            logPlate.type = Image.Type.Sliced;
            Ui.Place(logPlate.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(400, 110));
            _logMain = Ui.Label(logPlate.transform, "LogMain", "", 26, TitleGreen);
            Ui.Place(_logMain.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(360, 34));
            _logSub = Ui.Label(logPlate.transform, "LogSub", "", 20, Ui.Hex("#8a5a1a"));
            Ui.Place(_logSub.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(360, 28));

            // 行动按钮坞(素材包 Command_Dock,底部整条)
            var dock = Ui.SpriteImg(_shakeRoot, "CommandDock", SpriteLib.Pack("UI/Panels/Command_Dock"));
            dock.type = Image.Type.Sliced;
            var barRt = dock.rectTransform;
            barRt.anchorMin = new Vector2(0, 0);
            barRt.anchorMax = new Vector2(1, 0);
            barRt.pivot = new Vector2(0.5f, 0);
            barRt.anchoredPosition = new Vector2(0, 16);
            barRt.sizeDelta = new Vector2(-52, 150);
            _dockGroup = dock.gameObject.AddComponent<CanvasGroup>();
            var layout = dock.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 22, 16, 16);
            layout.spacing = 16;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var formulaSkill = System.Array.Find(_state.Player.Skills, s => s.Id == "flame-formula");
            _btnTackle = ActionButton(dock.rectTransform, SpriteLib.Pack("UI/Icons/Tackle"),
                "TACKLE", "FREE", Ui.Hex("#5c8a3f"), out _);
            _btnTackle.onClick.AddListener(() => StartCoroutine(TackleRoutine()));
            _btnFormula = ActionButton(dock.rectTransform, SpriteLib.Pack("UI/Icons/Flame_Formula"),
                formulaSkill.Name.ToUpperInvariant(), $"COST {formulaSkill.Cost}", SubOrange, out _formulaBg);
            _btnFormula.onClick.AddListener(() => StartCoroutine(FormulaRoutine()));
            _btnShield = ActionButton(dock.rectTransform, SpriteLib.One("Art/Sprites/shield"),
                "BREAK SHIELD", $"MAKE {_state.Enemy.Shield ?? 10}", Ui.ShieldBlue, out _);
            _btnShield.onClick.AddListener(() => StartCoroutine(BreakShieldRoutine()));
            _btnCatch = ActionButton(dock.rectTransform, SpriteLib.Pack("UI/Icons/Catch"),
                "CATCH", "FRIEND PUZZLE", SubOrange, out _);
            _btnCatch.onClick.AddListener(() => StartCoroutine(CatchRoutine()));
        }

        /// <summary>状态名牌:素材包 Status_Panel(9-slice),名字/副标题/HP 文字/血条。</summary>
        private RectTransform BuildStatusPlate(string plateName, Vector2 anchor, Vector2 offset, Vector2 size,
            string title, string subtitle, out Image hpFill, out Text hpText)
        {
            var plate = Ui.SpriteImg(_shakeRoot, plateName, SpriteLib.Pack("UI/Panels/Status_Panel"));
            plate.type = Image.Type.Sliced;
            Ui.Place(plate.rectTransform, anchor, offset, size);

            var name = Ui.Label(plate.transform, "Name", title.ToUpperInvariant(), 28, TitleGreen, TextAnchor.UpperLeft);
            Ui.Place(name.rectTransform, new Vector2(0, 1), new Vector2(22, -16), new Vector2(300, 32));
            var sub = Ui.Label(plate.transform, "Sub", subtitle, 20, SubOrange, TextAnchor.UpperLeft);
            Ui.Place(sub.rectTransform, new Vector2(0, 1), new Vector2(22, -52), new Vector2(200, 24));
            hpText = Ui.Label(plate.transform, "HpText", "", 20, TitleGreen, TextAnchor.UpperLeft);
            Ui.Place(hpText.rectTransform, new Vector2(0, 1), new Vector2(22, -82), new Vector2(240, 24));

            // 血条:素材包外框 + 填充
            var frame = Ui.SpriteImg(plate.transform, "HpFrame", SpriteLib.Pack("UI/Bars/HP_Bar_Frame"));
            Ui.Place(frame.rectTransform, new Vector2(0, 0), new Vector2(22, 16), new Vector2(size.x - 84, 26));
            frame.rectTransform.pivot = new Vector2(0, 0);
            var fill = Ui.SpriteImg(frame.transform, "HpFill", SpriteLib.Pack("UI/Bars/HP_Bar_Fill"));
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = new Vector2(4, 5);
            fill.rectTransform.offsetMax = new Vector2(-4, -5);
            hpFill = fill;

            return plate.rectTransform;
        }

        /// <summary>敌方名牌右上角的护盾徽章。</summary>
        private void BuildShieldRow(RectTransform plate)
        {
            var row = Ui.Node(plate, "ShieldRow");
            Ui.Place(row, new Vector2(1, 1), new Vector2(-16, -14), new Vector2(96, 32));
            var icon = Ui.SpriteImg(row, "Icon", SpriteLib.One("Art/Sprites/shield"));
            Ui.Place(icon.rectTransform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(28, 28));
            var text = Ui.Label(row, "Value", _state.Enemy.Shield?.ToString() ?? "", 24, Ui.ShieldBlue, TextAnchor.MiddleLeft);
            Ui.Place(text.rectTransform, new Vector2(0, 0.5f), new Vector2(36, 0), new Vector2(60, 30));
            _shieldRow = row.gameObject;
        }

        /// <summary>我方名牌底部的宝石行:宝石图标 × 数量 + "N GEMS"。</summary>
        private void BuildGemRow(RectTransform plate)
        {
            var row = Ui.Node(plate, "GemRow");
            Ui.Place(row, new Vector2(1, 1), new Vector2(-16, -50), new Vector2(220, 30));
            var gemSprite = SpriteLib.Pack("UI/Icons/Gem");
            for (int i = 0; i < 8; i++)
            {
                var icon = Ui.SpriteImg(row, $"Gem{i}", gemSprite != null ? gemSprite : SpriteLib.One("Art/Sprites/gem"));
                icon.preserveAspect = true;
                Ui.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(i * 26, 0), new Vector2(24, 24));
                _gemIcons.Add(icon);
            }
            _gemLabel = Ui.Label(plate, "GemLabel", "", 20, SubOrange, TextAnchor.LowerRight);
            Ui.Place(_gemLabel.rectTransform, new Vector2(1, 0), new Vector2(-18, 36), new Vector2(160, 24));
        }

        /// <summary>大按钮:素材包按钮底(9-slice)+ 图标 + 标题 + 副标题。</summary>
        private Button ActionButton(RectTransform parent, Sprite icon, string title, string subtitle,
            Color subtitleColor, out Image bgImage)
        {
            var bg = Ui.SpriteImg(parent, $"Btn-{title}", SpriteLib.Pack("UI/Buttons/Button_Normal"));
            bg.type = Image.Type.Sliced;
            bgImage = bg;

            var iconImg = Ui.SpriteImg(bg.transform, "Icon", icon);
            iconImg.preserveAspect = true;
            Ui.Place(iconImg.rectTransform, new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(52, 52));

            var titleText = Ui.Label(bg.transform, "Title", title, 24, TitleGreen);
            Ui.Place(titleText.rectTransform, new Vector2(0.5f, 1), new Vector2(26, -36), new Vector2(280, 30));
            var subText = Ui.Label(bg.transform, "SubT", subtitle, 18, subtitleColor);
            Ui.Place(subText.rectTransform, new Vector2(0.5f, 0), new Vector2(26, 28), new Vector2(280, 24));

            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.SpriteSwap;
            var swap = btn.spriteState;
            swap.highlightedSprite = SpriteLib.Pack("UI/Buttons/Button_Selected");
            swap.pressedSprite = SpriteLib.Pack("UI/Buttons/Button_Pressed");
            swap.selectedSprite = SpriteLib.Pack("UI/Buttons/Button_Selected");
            swap.disabledSprite = SpriteLib.Pack("UI/Buttons/Button_Normal");
            btn.spriteState = swap;
            return btn;
        }

        // ---------- 渲染 ----------

        private void RenderAll()
        {
            SetHpBar(_playerHpFill, _playerHpText, _state.PlayerHp, _state.Player.MaxHp);
            SetHpBar(_enemyHpFill, _enemyHpText, _state.EnemyHp, _state.Enemy.MaxHp);

            for (int i = 0; i < _gemIcons.Count; i++)
                _gemIcons[i].gameObject.SetActive(i < _state.Gems);
            _gemLabel.text = $"{_state.Gems} GEMS";

            _shieldRow.SetActive(_state.EnemyShielded);
            _btnFormula.interactable = _state.Gems >= 3;
            _formulaBg.sprite = _state.Gems >= 3
                ? SpriteLib.Pack("UI/Buttons/Button_Selected")
                : SpriteLib.Pack("UI/Buttons/Button_Normal");
            _formulaBg.color = _state.Gems >= 3 ? Color.white : new Color(1f, 1f, 1f, 0.85f);
            _btnShield.gameObject.SetActive(_state.Enemy.Shield.HasValue);
            _btnShield.interactable = _state.EnemyShielded;
            _btnCatch.gameObject.SetActive(
                _state.Enemy.Catchable && _state.EnemyHp > 0 && _state.EnemyHp <= 3);
        }

        private static void SetHpBar(Image fill, Text label, int hp, int maxHp)
        {
            float t = maxHp == 0 ? 0 : (float)hp / maxHp;
            fill.rectTransform.anchorMax = new Vector2(t, 1);
            // 填充是素材包绿色贴图,低血用染色提示
            fill.color = t > 0.5f ? Color.white : t > 0.25f ? Ui.Hex("#ffd27a") : Ui.Hex("#ff9c9c");
            label.text = $"HP {hp} / {maxHp}";
        }

        private void SetLog(string text, string sub = "")
        {
            _logMain.text = text.ToUpperInvariant();
            _logSub.text = sub;
        }

        private void SetActionsEnabled(bool on)
        {
            _btnTackle.interactable = on;
            _btnFormula.interactable = on;
            _btnShield.interactable = on;
            _btnCatch.interactable = on;
            if (_dockGroup != null)
            {
                _dockGroup.alpha = on ? 1f : 0.6f;
                _dockGroup.interactable = on;
            }
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
            SetLog("Your turn", "+2 GEMS");
            SetActionsEnabled(true);
        }

        private void ShowOutcome()
        {
            bool win = _state.Outcome == BattleOutcome.Win;
            _voice.Say(win ? $"You win! {_state.Player.Name} got five experience points!" : "Oh no! Let's try again!");
            ShowBanner(win ? "YOU WIN!" : $"{_state.Player.Name} fainted...", win ? "+5 XP" : "",
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
