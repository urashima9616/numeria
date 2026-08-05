using System;
using System.Collections;
using System.Collections.Generic;
using Numeria.Core;
using TMPro;
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
        private Progress _progress;
        private int _xpReward;
        private SkillDef _themeSkill;

        private RectTransform _canvasRoot;
        private RectTransform _shakeRoot;
        private RectTransform _playerSprite;
        private RectTransform _enemySprite;
        private Image _playerHpFill;
        private Image _enemyHpFill;
        private TMP_Text _playerHpText;
        private TMP_Text _enemyHpText;
        private readonly List<Image> _gemIcons = new List<Image>();
        private TMP_Text _gemLabel;
        private GameObject _shieldRow;
        private TMP_Text _logMain;
        private TMP_Text _logSub;
        private Button _btnTackle;
        private Button _btnFormula;
        private Image _formulaBg;
        private Button _btnShield;
        private Button _btnCatch;
        private TMP_Text _catchChanceText;
        private Button _btnItem;
        private CanvasGroup _dockGroup;
        private int _playerLevel = 1;

        // 参考图配色(与菜单一致)
        private static readonly Color Cream = Ui.Hex("#f6efdc");
        private static readonly Color TitleGreen = Ui.Hex("#3a4d2f");
        private static readonly Color Amber = Ui.Hex("#f2b04e");
        private static readonly Color HpGreen = Ui.Hex("#7ac974");
        private static readonly Color HpTrack = Ui.Hex("#e3d9bd");
        private static readonly Color SubOrange = Ui.Hex("#9b5729");

        private int _tier = 1;
        private string _battleBg = "forest-battle";

        public void Init(CombatantDef enemy, Progress progress, int tier, string battleBg, Action<BattleEnd> onEnd)
        {
            _onEnd = onEnd;
            _progress = progress;
            _tier = tier;
            _battleBg = battleBg;
            var growth = progress.ActiveGrowth;
            _playerLevel = growth.Level;
            _rng = new Rng((uint)Environment.TickCount);
            _state = new BattleState(progress.PlayerCombatant(progress.ActiveMonId), enemy, _rng);
            _themeSkill = System.Array.Find(_state.Player.Skills, skill => skill.Type == SkillType.Formula);
            _state.PlayerAttackBonus = progress.TotalAttackBonus(progress.ActiveMonId);
            _state.PlayerDefenseBonus = progress.TotalDefenseBonus(progress.ActiveMonId);
            _xpReward = GrowthSystem.VictoryXp(enemy.BaseXp, enemy.Level, growth.Level, enemy.IsBoss);
            _voice = gameObject.AddComponent<Voice>();
            BuildUi();
            _puzzles = new PuzzleUi(this, _canvasRoot, _rng, lines => _voice.Say(lines),
                solved => _progress.RecordPuzzle(solved));
            RenderAll();
            string opening = enemy.Shield.HasValue
                ? _tier >= 3
                    ? $"A wild {enemy.Name} appeared! It has a pattern shield!"
                    : $"A wild {enemy.Name} appeared! It has a number shield!"
                : $"A wild {enemy.Name} appeared!";
            SetLog("Your turn", "+2 GEMS");
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
            // 素材包与目标稿均按 16:9 的 1920×1080 画布设计。
            // 继续使用锚点定位，因此在 4:3 iPad 上仍会贴合安全边缘而不是整体拉伸。
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _canvasRoot = (RectTransform)canvasGo.transform;

            _shakeRoot = Ui.Node(_canvasRoot, "ShakeRoot");
            Ui.Stretch(_shakeRoot);

            var bg = Ui.SpriteImg(_shakeRoot, "Background", SpriteLib.One(_battleBg));
            Ui.Stretch(bg.rectTransform);

            // 敌方:左上状态牌。目标稿需要足够的纵向空间让名字、等级、HP 与血条各占一行。
            var enemyPlate = BuildStatusPlate("EnemyPlate", new Vector2(0, 1), new Vector2(28, -28), new Vector2(480, 228),
                _state.Enemy.Name,
                $"Lv. {_state.Enemy.Level}   ATK {_state.Enemy.AttackPower}   DEF {_state.Enemy.DefensePower}",
                out _enemyHpFill, out _enemyHpText);
            BuildShieldRow(enemyPlate);
            // 敌方立绘落在右上草圈，避开状态牌与回合横幅。
            var enemyImg = Ui.SpriteImg(_shakeRoot, "EnemySprite", SpriteLib.EnemyBattleSprite(_state.Enemy.Id));
            enemyImg.preserveAspect = true;
            Ui.PlaceCentered(enemyImg.rectTransform, new Vector2(0.69f, 0.67f), Vector2.zero, new Vector2(360, 360));
            _enemySprite = enemyImg.rectTransform;

            // 我方立绘落在左下草圈，但给底部命令坞留出完整空间。
            var playerImg = Ui.SpriteImg(_shakeRoot, "PlayerSprite", SpriteLib.PlayerBattleSprite(_state.Player.Id));
            playerImg.preserveAspect = true;
            Ui.PlaceCentered(playerImg.rectTransform, new Vector2(0.235f, 0.49f), Vector2.zero, new Vector2(490, 490));
            _playerSprite = playerImg.rectTransform;
            var playerPlate = BuildStatusPlate("PlayerPlate", new Vector2(1, 0), new Vector2(-28, 267), new Vector2(560, 310),
                _state.Player.Name,
                $"Lv. {_playerLevel}   ATK {_state.Player.AttackPower + _state.PlayerAttackBonus}   " +
                $"DEF {_state.Player.DefensePower + _state.PlayerDefenseBonus}",
                out _playerHpFill, out _playerHpText);
            BuildGemRow(playerPlate);

            // 回合横幅只承载短主标题 + 一行结果，避免战斗叙述横穿整个画面。
            var logPlate = Ui.SpriteImg(_shakeRoot, "LogPlate", SpriteLib.Pack("UI/Panels/Turn_Banner"));
            logPlate.type = Image.Type.Sliced;
            Ui.Place(logPlate.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(360, 152));
            _logMain = Ui.DisplayLabel(logPlate.transform, "LogMain", "", 56, TitleGreen);
            Ui.Place(_logMain.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(306, 58));
            FitText(_logMain, 38, 56);
            _logSub = Ui.DisplayLabel(logPlate.transform, "LogSub", "", 34, Ui.Hex("#70491f"));
            Ui.Place(_logSub.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(210, 36));
            FitText(_logSub, 24, 34);
            BuildLogSpark(logPlate.transform, "LeftSpark", -124);
            BuildLogSpark(logPlate.transform, "RightSpark", 124);

            // 行动按钮坞(素材包 Command_Dock,底部整条)
            var dock = Ui.SpriteImg(_shakeRoot, "CommandDock", SpriteLib.Pack("UI/Panels/Command_Dock"));
            dock.type = Image.Type.Sliced;
            var barRt = dock.rectTransform;
            barRt.anchorMin = new Vector2(0, 0);
            barRt.anchorMax = new Vector2(1, 0);
            barRt.pivot = new Vector2(0.5f, 0);
            barRt.anchoredPosition = new Vector2(0, 8);
            barRt.sizeDelta = new Vector2(-160, 304);
            _dockGroup = dock.gameObject.AddComponent<CanvasGroup>();
            var layout = dock.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(56, 56, 61, 31);
            layout.spacing = 34;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            _btnTackle = ActionButton(dock.rectTransform, SpriteLib.Pack("UI/Icons/Tackle"),
                "TACKLE", "FREE", Ui.Hex("#5c8a3f"), out _);
            _btnTackle.onClick.AddListener(() => StartCoroutine(TackleRoutine()));
            var themeIcon = SpriteLib.One(_themeSkill.IconResource) ?? SpriteLib.Pack("UI/Icons/Flame_Formula");
            _btnFormula = ActionButton(dock.rectTransform, themeIcon,
                _themeSkill.Name.ToUpperInvariant(), $"COST {_themeSkill.Cost}", ThemeColor(_themeSkill.Visual), out _formulaBg);
            _btnFormula.onClick.AddListener(() => StartCoroutine(FormulaRoutine()));
            _btnShield = ActionButton(dock.rectTransform, SpriteLib.One("Art/Sprites/shield"),
                "BREAK SHIELD", _tier >= 3 ? "FINISH PATTERN" : $"MAKE {_state.Enemy.Shield ?? 10}",
                Ui.ShieldBlue, out _);
            _btnShield.onClick.AddListener(() => StartCoroutine(BreakShieldRoutine()));
            _btnCatch = ActionButton(dock.rectTransform, SpriteLib.Pack("UI/Icons/Catch"),
                "CATCH", "10% CHANCE", SubOrange, out _);
            _catchChanceText = _btnCatch.transform.Find("SubT").GetComponent<TMP_Text>();
            _btnCatch.onClick.AddListener(() => StartCoroutine(CatchRoutine()));
            _btnItem = ActionButton(dock.rectTransform, SpriteLib.One("Art/Sprites/gem"),
                "ITEMS", "BATTLE ONLY", Ui.Hex("#5c8a3f"), out _);
            _btnItem.onClick.AddListener(() => StartCoroutine(ItemRoutine()));
        }

        /// <summary>状态名牌:名字、等级、HP 数字与血条严格纵向分层，不共享基线。</summary>
        private RectTransform BuildStatusPlate(string plateName, Vector2 anchor, Vector2 offset, Vector2 size,
            string title, string subtitle, out Image hpFill, out TMP_Text hpText)
        {
            var plate = Ui.SpriteImg(_shakeRoot, plateName, SpriteLib.Pack("UI/Panels/Status_Panel"));
            plate.type = Image.Type.Sliced;
            Ui.Place(plate.rectTransform, anchor, offset, size);

            var name = Ui.DisplayLabel(plate.transform, "Name", title.ToUpperInvariant(), 60, TitleGreen, TextAnchor.MiddleLeft);
            Ui.Place(name.rectTransform, new Vector2(0, 1), new Vector2(48, -24), new Vector2(size.x - 96, 58));
            FitText(name, 42, 60);
            var sub = Ui.DisplayLabel(plate.transform, "Sub", subtitle, 38, SubOrange, TextAnchor.MiddleLeft);
            Ui.Place(sub.rectTransform, new Vector2(0, 1), new Vector2(48, -82), new Vector2(size.x - 96, 36));
            hpText = Ui.DisplayLabel(plate.transform, "HpText", "", 38, TitleGreen, TextAnchor.MiddleLeft);
            Ui.Place(hpText.rectTransform, new Vector2(0, 1), new Vector2(48, -120), new Vector2(size.x - 96, 36));

            // 血条使用顶部锚点，避免面板变高后被错误吸到底边。
            var frame = Ui.SpriteImg(plate.transform, "HpFrame", SpriteLib.Pack("UI/Bars/HP_Bar_Frame"));
            Ui.Place(frame.rectTransform, new Vector2(0, 1), new Vector2(48, -154), new Vector2(size.x - 96, 28));
            var fill = Ui.SpriteImg(frame.transform, "HpFill", SpriteLib.Pack("UI/Bars/HP_Bar_Fill"));
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = new Vector2(4, 5);
            fill.rectTransform.offsetMax = new Vector2(-4, -5);
            hpFill = fill;

            return plate.rectTransform;
        }

        /// <summary>敌方名牌等级行右侧的护盾徽章。</summary>
        private void BuildShieldRow(RectTransform plate)
        {
            var row = Ui.Node(plate, "ShieldRow");
            Ui.Place(row, new Vector2(1, 1), new Vector2(-48, -78), new Vector2(100, 34));
            var icon = Ui.SpriteImg(row, "Icon", SpriteLib.One("Art/Sprites/shield"));
            Ui.Place(icon.rectTransform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(32, 32));
            var text = Ui.DisplayLabel(row, "Value", _tier >= 3 ? "?" : _state.Enemy.Shield?.ToString() ?? "",
                34, Ui.ShieldBlue, TextAnchor.MiddleLeft);
            Ui.Place(text.rectTransform, new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(60, 32));
            _shieldRow = row.gameObject;
        }

        /// <summary>我方名牌底部的宝石行:最多展示 3 颗示意图标，数字负责表达准确数量。</summary>
        private void BuildGemRow(RectTransform plate)
        {
            var row = Ui.Node(plate, "GemRow");
            Ui.Place(row, new Vector2(0, 1), new Vector2(48, -198), new Vector2(250, 40));
            var gemSprite = SpriteLib.Pack("UI/Icons/Gem");
            for (int i = 0; i < 3; i++)
            {
                var icon = Ui.SpriteImg(row, $"Gem{i}", gemSprite != null ? gemSprite : SpriteLib.One("Art/Sprites/gem"));
                icon.preserveAspect = true;
                Ui.Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(i * 48, 0), new Vector2(40, 40));
                _gemIcons.Add(icon);
            }
            _gemLabel = Ui.DisplayLabel(plate, "GemLabel", "", 34, SubOrange, TextAnchor.MiddleLeft);
            Ui.Place(_gemLabel.rectTransform, new Vector2(0, 1), new Vector2(176, -198), new Vector2(280, 40));
        }

        /// <summary>大按钮:素材包按钮底(9-slice)+ 图标 + 标题 + 副标题。</summary>
        private Button ActionButton(RectTransform parent, Sprite icon, string title, string subtitle,
            Color subtitleColor, out Image bgImage)
        {
            var bg = Ui.SpriteImg(parent, $"Btn-{title}", SpriteLib.Pack("UI/Buttons/Button_Normal"));
            bg.type = Image.Type.Sliced;
            var ble = bg.gameObject.AddComponent<LayoutElement>();
            ble.minWidth = 270;
            ble.preferredWidth = 500;
            ble.flexibleWidth = 1;
            bgImage = bg;

            var iconImg = Ui.SpriteImg(bg.transform, "Icon", icon);
            iconImg.preserveAspect = true;
            Ui.Place(iconImg.rectTransform, new Vector2(0, 0.5f), new Vector2(34, 0), new Vector2(76, 76));

            var titleText = Ui.DisplayLabel(bg.transform, "Title", title, 50, TitleGreen);
            titleText.rectTransform.anchorMin = new Vector2(0, 0.48f);
            titleText.rectTransform.anchorMax = Vector2.one;
            titleText.rectTransform.offsetMin = new Vector2(112, 0);
            titleText.rectTransform.offsetMax = new Vector2(-18, -22);
            FitText(titleText, 34, 50);
            var subText = Ui.DisplayLabel(bg.transform, "SubT", subtitle, 34, subtitleColor);
            subText.rectTransform.anchorMin = Vector2.zero;
            subText.rectTransform.anchorMax = new Vector2(1, 0.48f);
            subText.rectTransform.offsetMin = new Vector2(112, 20);
            subText.rectTransform.offsetMax = new Vector2(-18, 0);
            FitText(subText, 23, 34);

            var btn = Sfx.WireClick(bg.gameObject.AddComponent<Button>());
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.SpriteSwap;
            var swap = btn.spriteState;
            swap.highlightedSprite = SpriteLib.Pack("UI/Buttons/Button_Selected");
            swap.pressedSprite = SpriteLib.Pack("UI/Buttons/Button_Pressed");
            swap.selectedSprite = SpriteLib.Pack("UI/Buttons/Button_Selected");
            swap.disabledSprite = SpriteLib.Pack("UI/Buttons/Button_Normal");
            btn.spriteState = swap;
            var colors = btn.colors;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.78f);
            btn.colors = colors;
            return btn;
        }

        /// <summary>只在文本真的放不下时向下收缩，正常 16:9 布局保持设计字号。</summary>
        private static void FitText(TMP_Text text, int minSize, int maxSize)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = minSize;
            text.fontSizeMax = maxSize;
        }

        private void BuildLogSpark(Transform parent, string name, float x)
        {
            var spark = Ui.SpriteImg(parent, name, SpriteLib.Pack("UI/Icons/Gem"));
            spark.preserveAspect = true;
            spark.color = new Color(0.52f, 0.30f, 0.08f, 0.9f);
            Ui.PlaceCentered(spark.rectTransform, new Vector2(0.5f, 0), new Vector2(x, 44), new Vector2(28, 28));
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
            _btnFormula.interactable = _state.Gems >= _themeSkill.Cost;
            _formulaBg.sprite = _state.Gems >= _themeSkill.Cost
                ? SpriteLib.Pack("UI/Buttons/Button_Selected")
                : SpriteLib.Pack("UI/Buttons/Button_Normal");
            _formulaBg.color = _state.Gems >= _themeSkill.Cost ? Color.white : new Color(1f, 1f, 1f, 0.85f);
            // 命令坞保持稳定的三栏结构:普通敌人显示 Catch，护盾敌人显示 Break Shield。
            _btnShield.gameObject.SetActive(_state.Enemy.Shield.HasValue);
            _btnShield.interactable = _state.EnemyShielded;
            _btnCatch.gameObject.SetActive(!_state.Enemy.Shield.HasValue && _state.Enemy.Catchable && _state.EnemyHp > 0);
            _btnCatch.interactable = true;
            if (_catchChanceText != null)
                _catchChanceText.text = $"{CatchSystem.Percent(_state.EnemyHp, _state.Enemy.MaxHp)}% CHANCE";
            _btnItem.interactable = _progress.HealthPotions > 0 || _progress.GemSnacks > 0;
        }

        private static void SetHpBar(Image fill, TMP_Text label, int hp, int maxHp)
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
            _btnItem.interactable = on;
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
            RecordDamage(result.Damage);
            Sfx.Play(SfxCue.Hit);
            PopDamage(_enemySprite, result.BreakBonusApplied ? $"-{result.Damage}  2X" : $"-{result.Damage}",
                Ui.Hex("#ffd24a"));
            if (result.BreakBonusApplied) _voice.Say("Double damage!");
            yield return Flash(_enemySprite);
            RenderAll();
            SetLog("Tackle!", result.BreakBonusApplied
                ? $"2X SHIELD BREAK - {result.Damage} DAMAGE"
                : $"{result.Damage} DAMAGE");
            yield return EndPlayerTurn();
        }

        private IEnumerator FormulaRoutine()
        {
            SetActionsEnabled(false);
            bool? correct = null;
            yield return _puzzles.RunTierPuzzle(v => correct = v, _tier);
            yield return Lunge(_playerSprite, new Vector2(60, 30));
            yield return PlayThemeSkill(_themeSkill, correct.Value);
            var result = _state.UseSkill(_themeSkill.Id, correct.Value);
            RecordDamage(result.Damage);
            Sfx.Play(SfxCue.Hit, result.Powered ? 1f : 0.72f);
            PopDamage(_enemySprite, result.BreakBonusApplied ? $"-{result.Damage}  2X" : $"-{result.Damage}", result.Powered
                ? ThemeColor(_themeSkill.Visual)
                : Ui.Hex("#ffd24a"));
            if (result.BreakBonusApplied) _voice.Say("Double damage!");
            if (result.Powered) StartCoroutine(Shake());
            yield return Flash(_enemySprite);
            RenderAll();
            SetLog($"{_themeSkill.Name}!", result.BreakBonusApplied
                ? $"2X SHIELD BREAK - {result.Damage} DAMAGE"
                : result.Powered ? $"{result.Damage} DAMAGE" : $"{result.Damage} DAMAGE - NICE TRY");
            yield return EndPlayerTurn();
        }

        private IEnumerator BreakShieldRoutine()
        {
            SetActionsEnabled(false);
            bool? ok = null;
            if (_tier >= 3) yield return _puzzles.RunPattern(v => ok = v, _tier);
            else yield return _puzzles.RunMakeTen(v => ok = v, _state.Enemy.Shield ?? 10);
            if (ok.Value)
            {
                Sfx.Play(SfxCue.ShieldBreak);
                yield return ShatterShield();
                _state.BreakShield();
                RenderAll();
                StartCoroutine(Shake());
                SetLog("Shield broken!", "DOUBLE NEXT HIT - ENEMY STUNNED");
                yield return Flash(_enemySprite);
            }
            else
            {
                SetLog("Shield holds", "TRY AGAIN NEXT TURN");
            }
            yield return EndPlayerTurn();
        }

        private IEnumerator CatchRoutine()
        {
            SetActionsEnabled(false);
            SetLog("Catch!", "FRIEND PUZZLE");
            bool? correct = null;
            yield return _puzzles.RunTierPuzzle(v => correct = v, _tier);
            if (correct.Value)
            {
                int chance = CatchSystem.Percent(_state.EnemyHp, _state.Enemy.MaxHp);
                if (_state.TryCatch())
                {
                    Sfx.Play(SfxCue.Catch);
                    _voice.Say($"Gotcha! {_state.Enemy.Name} wants to travel with you!");
                    yield return FriendGemBurst(_enemySprite);
                    ShowBanner($"Befriended {_state.Enemy.Name}!", "CATCH COMPLETE", () => _onEnd(BattleEnd.Caught));
                }
                else
                {
                    Sfx.Play(SfxCue.SoftMiss, 0.72f);
                    _voice.Say("It broke free! Lower its health and try again!");
                    SetLog("It broke free!", $"{chance}% CHANCE - LOWER HP");
                    yield return EndPlayerTurn();
                }
            }
            else
            {
                SetLog("So close!", "TRY AGAIN");
                yield return EndPlayerTurn();
            }
        }

        private IEnumerator ItemRoutine()
        {
            SetActionsEnabled(false);
            ConsumableType? choice = null;
            bool cancelled = false;

            var overlay = Ui.Img(_canvasRoot, "ItemOverlay", new Color(0.06f, 0.09f, 0.13f, 0.88f));
            Ui.Stretch(overlay.rectTransform);
            var panel = Ui.Img(overlay.transform, "ItemPanel", Cream);
            Ui.Place(panel.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(820, 420));
            Ui.AddOutline(panel.gameObject);
            var title = Ui.DisplayLabel(panel.transform, "Title", "BATTLE ITEMS", 48, TitleGreen);
            Ui.Place(title.rectTransform, new Vector2(.5f, 1), new Vector2(0, -38), new Vector2(650, 64));

            var potion = Ui.Btn(panel.transform, "Potion", $"HP POTION  x{_progress.HealthPotions}\nRESTORE 40% HP", 26);
            Ui.Place((RectTransform)potion.transform, new Vector2(.5f, .5f), new Vector2(-200, 18), new Vector2(330, 150));
            potion.interactable = _progress.HealthPotions > 0;
            potion.onClick.AddListener(() => choice = ConsumableType.HealthPotion);

            var gems = Ui.Btn(panel.transform, "Gems", $"GEM SNACK  x{_progress.GemSnacks}\nRESTORE 3 GEMS", 26);
            Ui.Place((RectTransform)gems.transform, new Vector2(.5f, .5f), new Vector2(200, 18), new Vector2(330, 150));
            gems.interactable = _progress.GemSnacks > 0;
            gems.onClick.AddListener(() => choice = ConsumableType.GemSnack);

            var back = Ui.Btn(panel.transform, "Back", "BACK", 24);
            Ui.Place((RectTransform)back.transform, new Vector2(.5f, 0), new Vector2(0, 34), new Vector2(260, 64));
            back.onClick.AddListener(() => cancelled = true);
            yield return new WaitUntil(() => choice.HasValue || cancelled);
            Destroy(overlay.gameObject);

            if (cancelled)
            {
                SetActionsEnabled(true);
                yield break;
            }

            int restored = choice == ConsumableType.HealthPotion
                ? _state.HealPlayer(Math.Max(5, (int)Math.Ceiling(_state.Player.MaxHp * .4f)))
                : _state.RestoreGems(3);
            if (restored <= 0)
            {
                _voice.Say(choice == ConsumableType.HealthPotion
                    ? "Health is already full!" : "Gems are already full!");
                SetActionsEnabled(true);
                yield break;
            }

            _progress.UseConsumable(choice.Value);
            Sfx.Play(SfxCue.Correct, .8f);
            RenderAll();
            SetLog(choice == ConsumableType.HealthPotion ? "HP restored!" : "Gems restored!",
                choice == ConsumableType.HealthPotion ? $"+{restored} HP" : $"+{restored} GEMS");
            yield return EndPlayerTurn();
        }

        private void RecordDamage(int damage)
        {
            _progress.Records.HighestDamage = Math.Max(_progress.Records.HighestDamage, damage);
        }

        private IEnumerator EndPlayerTurn()
        {
            if (_state.Outcome != BattleOutcome.None) { ShowOutcome(); yield break; }
            yield return new WaitForSeconds(0.6f);
            if (_state.ConsumeEnemySkipTurn())
            {
                Sfx.Play(SfxCue.ShieldBreak, 0.7f);
                _voice.Say("Shield stun! The enemy misses a turn!");
                SetLog("Shield stun!", "ENEMY MISSES A TURN");
                yield return new WaitForSeconds(1f);
                _state.StartPlayerTurn();
                RenderAll();
                SetLog("Your turn", "+2 GEMS - DOUBLE HIT READY");
                SetActionsEnabled(true);
                yield break;
            }
            SetLog("Enemy turn", $"{_state.Enemy.Name.ToUpperInvariant()} ATTACKS");
            yield return Lunge(_enemySprite, new Vector2(-60, -30));
            int dmg = _state.EnemyTurn();
            Sfx.Play(SfxCue.Hit, 0.82f);
            PopDamage(_playerSprite, $"-{dmg}", Ui.Hex("#ff6b6b"));
            yield return Flash(_playerSprite);
            RenderAll();
            SetLog("Ouch!", $"{dmg} DAMAGE");
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
            Sfx.Play(win ? SfxCue.Victory : SfxCue.SoftMiss);
            if (win) _voice.Say("You win!", $"{_state.Player.Name} is getting stronger!");
            else _voice.Say("Oh no! Let's try again!");
            ShowBanner(win ? "YOU WIN!" : $"{_state.Player.Name} fainted...", win ? $"+{_xpReward} XP" : "",
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

        private static Color ThemeColor(SkillVisualKind visual)
        {
            switch (visual)
            {
                case SkillVisualKind.MakeTenWave: return Ui.Hex("#33c9d7");
                case SkillVisualKind.PatternLeaf: return Ui.Hex("#7bd64a");
                case SkillVisualKind.CountCrunch: return Ui.Hex("#f2c84b");
                case SkillVisualKind.DoubleBoulder: return Ui.Hex("#9f8d72");
                case SkillVisualKind.SymmetryBeam: return Ui.Hex("#69d9ff");
                case SkillVisualKind.MatchingPaws: return Ui.Hex("#e2a44e");
                case SkillVisualKind.SubtractionDash: return Ui.Hex("#f1b33b");
                case SkillVisualKind.TallyStone: return Ui.Hex("#81796d");
                case SkillVisualKind.GeometryPrism: return Ui.Hex("#75b8ff");
                case SkillVisualKind.SequenceSpark: return Ui.Hex("#b6e83f");
                case SkillVisualKind.FairyGlimmer: return Ui.Hex("#f49ee8");
                case SkillVisualKind.DragonSpiral: return Ui.Hex("#9f79e8");
                case SkillVisualKind.ElectricBolt: return Ui.Hex("#ffe047");
                case SkillVisualKind.GrassBloom: return Ui.Hex("#58c957");
                case SkillVisualKind.FlyingGust: return Ui.Hex("#79d8f2");
                default: return Ui.Hex("#ff6a32");
            }
        }

        /// <summary>
        /// 主题技能不再共用一颗橙色圆球。每个数学家族有自己的移动节奏、形状与命中语言，
        /// 同时复用技能图标作为视觉锚点，让按钮与实际演出保持一致。
        /// </summary>
        private IEnumerator PlayThemeSkill(SkillDef skill, bool powered)
        {
            Color color = powered ? ThemeColor(skill.Visual) : Ui.Hex("#e8c974");
            Sprite icon = SpriteLib.One(skill.IconResource);
            switch (skill.Visual)
            {
                case SkillVisualKind.MakeTenWave:
                    yield return WaveVolley(color);
                    break;
                case SkillVisualKind.PatternLeaf:
                    yield return SpiralCast(icon, color);
                    break;
                case SkillVisualKind.CountCrunch:
                    yield return CountVolley(color);
                    break;
                case SkillVisualKind.DoubleBoulder:
                    yield return TwinCrash(icon, color, true);
                    break;
                case SkillVisualKind.SymmetryBeam:
                    yield return MirrorBeam(icon, color);
                    break;
                case SkillVisualKind.MatchingPaws:
                    yield return TwinCrash(icon, color, false);
                    break;
                case SkillVisualKind.SubtractionDash:
                    yield return SubtractionStrike(icon, color);
                    break;
                case SkillVisualKind.TallyStone:
                    yield return TallyRain(color);
                    break;
                case SkillVisualKind.GeometryPrism:
                    yield return GeometryVolley(icon, color);
                    break;
                case SkillVisualKind.SequenceSpark:
                    yield return SequenceVolley(color);
                    break;
                case SkillVisualKind.FairyGlimmer:
                    yield return MirrorBeam(icon, color);
                    break;
                case SkillVisualKind.DragonSpiral:
                    yield return MoveSkillIcon(icon, color, 130f, .52f, 540f);
                    break;
                case SkillVisualKind.ElectricBolt:
                    yield return SequenceVolley(color);
                    break;
                case SkillVisualKind.GrassBloom:
                    yield return SpiralCast(icon, color);
                    break;
                case SkillVisualKind.FlyingGust:
                    yield return WaveVolley(color);
                    break;
                default:
                    yield return MoveSkillIcon(icon, color, 72f, .38f, 220f);
                    break;
            }
            yield return RadialBurst(_enemySprite.position, color, powered ? 10 : 6);
        }

        private Image EffectImage(string name, Sprite sprite, Color color, Vector2 size)
        {
            Image image = sprite != null
                ? Ui.SpriteImg(_canvasRoot, name, sprite)
                : Ui.Img(_canvasRoot, name, color);
            image.preserveAspect = true;
            image.color = color;
            image.raycastTarget = false;
            image.rectTransform.sizeDelta = size;
            return image;
        }

        private IEnumerator MoveSkillIcon(Sprite icon, Color tint, float arcHeight, float duration, float spin)
        {
            var image = EffectImage("SkillGlyph", icon, icon != null ? Color.white : tint, new Vector2(92, 92));
            Vector3 start = _playerSprite.position + new Vector3(50, 60, 0);
            Vector3 end = _enemySprite.position;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                image.rectTransform.position = Vector3.Lerp(start, end, p) + Vector3.up * (Mathf.Sin(p * Mathf.PI) * arcHeight);
                image.rectTransform.localRotation = Quaternion.Euler(0, 0, spin * p);
                image.rectTransform.localScale = Vector3.one * Mathf.Lerp(.72f, 1.08f, p);
                yield return null;
            }
            Destroy(image.gameObject);
        }

        private IEnumerator WaveVolley(Color color)
        {
            var drops = new List<Image>();
            Sprite gem = SpriteLib.Pack("UI/Icons/Gem");
            for (int i = 0; i < 3; i++)
                drops.Add(EffectImage($"WaveDrop{i}", gem, color, new Vector2(44 + i * 8, 44 + i * 8)));

            Vector3 start = _playerSprite.position + new Vector3(50, 30, 0);
            Vector3 end = _enemySprite.position;
            float t = 0;
            while (t < .52f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / .52f);
                for (int i = 0; i < drops.Count; i++)
                {
                    float delayed = Mathf.Clamp01(p * 1.3f - i * .14f);
                    drops[i].rectTransform.position = Vector3.Lerp(start, end, delayed) +
                        Vector3.up * Mathf.Sin(delayed * Mathf.PI * 2f + i * .8f) * 46f;
                    drops[i].rectTransform.localRotation = Quaternion.Euler(0, 0, delayed * 180f);
                }
                yield return null;
            }
            foreach (var drop in drops) Destroy(drop.gameObject);
        }

        private IEnumerator SpiralCast(Sprite icon, Color color)
        {
            var leaves = new List<Image>();
            for (int i = 0; i < 3; i++)
                leaves.Add(EffectImage($"PatternLeaf{i}", icon, Color.white, new Vector2(60, 60)));
            Vector3 start = _playerSprite.position;
            Vector3 end = _enemySprite.position;
            float t = 0;
            while (t < .5f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / .5f);
                Vector3 center = Vector3.Lerp(start, end, p);
                for (int i = 0; i < leaves.Count; i++)
                {
                    float angle = p * Mathf.PI * 5f + i * Mathf.PI * 2f / leaves.Count;
                    leaves[i].rectTransform.position = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * (52f * (1f - p));
                    leaves[i].rectTransform.localRotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
                }
                yield return null;
            }
            foreach (var leaf in leaves) Destroy(leaf.gameObject);
        }

        private IEnumerator CountVolley(Color color)
        {
            Sprite gem = SpriteLib.Pack("UI/Icons/Gem");
            for (int i = 0; i < 3; i++)
            {
                var bead = EffectImage($"CountBead{i + 1}", gem, color, new Vector2(38 + i * 12, 38 + i * 12));
                Vector3 start = _playerSprite.position + new Vector3(i * 24, 40 + i * 18, 0);
                float t = 0;
                while (t < .16f)
                {
                    t += Time.deltaTime;
                    bead.rectTransform.position = Vector3.Lerp(start, _enemySprite.position, t / .16f);
                    yield return null;
                }
                Destroy(bead.gameObject);
            }
        }

        private IEnumerator TwinCrash(Sprite icon, Color color, bool heavy)
        {
            var left = EffectImage("TwinLeft", icon, icon != null ? Color.white : color, new Vector2(82, 82));
            var right = EffectImage("TwinRight", icon, icon != null ? Color.white : color, new Vector2(82, 82));
            Vector3 target = _enemySprite.position;
            Vector3 a = target + new Vector3(-260, heavy ? 180 : 40, 0);
            Vector3 b = target + new Vector3(260, heavy ? 180 : 40, 0);
            float t = 0;
            while (t < .42f)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0, 1, t / .42f);
                left.rectTransform.position = Vector3.Lerp(a, target, p);
                right.rectTransform.position = Vector3.Lerp(b, target, p);
                left.rectTransform.localRotation = Quaternion.Euler(0, 0, p * 120f);
                right.rectTransform.localRotation = Quaternion.Euler(0, 0, -p * 120f);
                yield return null;
            }
            Destroy(left.gameObject);
            Destroy(right.gameObject);
            if (heavy) StartCoroutine(Shake());
        }

        private IEnumerator MirrorBeam(Sprite icon, Color color)
        {
            yield return TwinCrash(icon, color, false);
            var beam = EffectImage("SymmetryBeam", null, color, new Vector2(30, 310));
            beam.rectTransform.position = _enemySprite.position;
            float t = 0;
            while (t < .25f)
            {
                t += Time.deltaTime;
                beam.color = new Color(color.r, color.g, color.b, 1f - t / .25f);
                beam.rectTransform.localScale = new Vector3(1f + t * 3f, 1f, 1f);
                yield return null;
            }
            Destroy(beam.gameObject);
        }

        private IEnumerator SubtractionStrike(Sprite icon, Color color)
        {
            yield return MoveSkillIcon(icon, color, 0, .22f, 0);
            var minus = EffectImage("MinusFlash", null, color, new Vector2(180, 24));
            minus.rectTransform.position = _enemySprite.position;
            minus.rectTransform.localRotation = Quaternion.Euler(0, 0, -18f);
            yield return new WaitForSeconds(.12f);
            Destroy(minus.gameObject);
        }

        private IEnumerator TallyRain(Color color)
        {
            var marks = new List<Image>();
            Vector3 target = _enemySprite.position;
            for (int i = 0; i < 5; i++)
            {
                var mark = EffectImage($"TallyMark{i}", null, color, new Vector2(15, 90));
                mark.rectTransform.position = target + new Vector3((i - 2) * 34f, 250 + Mathf.Abs(i - 2) * 28f, 0);
                marks.Add(mark);
            }
            float t = 0;
            while (t < .38f)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0, 1, t / .38f);
                for (int i = 0; i < marks.Count; i++)
                    marks[i].rectTransform.position = Vector3.Lerp(
                        target + new Vector3((i - 2) * 34f, 250 + Mathf.Abs(i - 2) * 28f, 0),
                        target + new Vector3((i - 2) * 34f, 0, 0), p);
                yield return null;
            }
            foreach (var mark in marks) Destroy(mark.gameObject);
            StartCoroutine(Shake());
        }

        private IEnumerator GeometryVolley(Sprite icon, Color color)
        {
            var prism = EffectImage("GeometryPrism", icon, Color.white, new Vector2(84, 84));
            Vector3 start = _playerSprite.position;
            Vector3 end = _enemySprite.position;
            var shapes = new List<Image>();
            for (int i = 0; i < 3; i++)
                shapes.Add(EffectImage($"GeometryShape{i}", null,
                    i == 0 ? color : i == 1 ? Ui.Hex("#d88cff") : Ui.Hex("#ffe27a"),
                    new Vector2(34 + i * 10, 34 + i * 10)));
            float t = 0;
            while (t < .46f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / .46f);
                prism.rectTransform.position = Vector3.Lerp(start, end, p);
                prism.rectTransform.localRotation = Quaternion.Euler(0, 0, p * 180f);
                for (int i = 0; i < shapes.Count; i++)
                {
                    float q = Mathf.Clamp01(p * 1.25f - i * .12f);
                    shapes[i].rectTransform.position = Vector3.Lerp(start, end, q) + Vector3.up * ((i - 1) * 42f);
                    shapes[i].rectTransform.localRotation = Quaternion.Euler(0, 0, q * (90f + i * 45f));
                }
                yield return null;
            }
            Destroy(prism.gameObject);
            foreach (var shape in shapes) Destroy(shape.gameObject);
        }

        private IEnumerator SequenceVolley(Color color)
        {
            Sprite gem = SpriteLib.Pack("UI/Icons/Gem");
            Vector3 start = _playerSprite.position;
            Vector3 end = _enemySprite.position;
            for (int i = 0; i < 4; i++)
            {
                float p = (i + 1) / 4f;
                var spark = EffectImage($"SequenceSpark{i}", gem, color, Vector2.one * (30 + i * 16));
                spark.rectTransform.position = Vector3.Lerp(start, end, p) + Vector3.up * (Mathf.Sin(p * Mathf.PI) * 85f);
                spark.rectTransform.localScale = Vector3.zero;
                float t = 0;
                while (t < .1f)
                {
                    t += Time.deltaTime;
                    spark.rectTransform.localScale = Vector3.one * Mathf.Clamp01(t / .1f);
                    yield return null;
                }
                yield return new WaitForSeconds(.035f);
                Destroy(spark.gameObject);
            }
        }

        private IEnumerator RadialBurst(Vector3 origin, Color color, int count)
        {
            var sparks = new List<Image>();
            Sprite gem = SpriteLib.Pack("UI/Icons/Gem");
            for (int i = 0; i < count; i++)
            {
                var spark = EffectImage($"HitSpark{i}", gem, color, new Vector2(24, 24));
                spark.rectTransform.position = origin;
                sparks.Add(spark);
            }
            float t = 0;
            while (t < .28f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / .28f);
                for (int i = 0; i < sparks.Count; i++)
                {
                    float angle = i * Mathf.PI * 2f / sparks.Count;
                    sparks[i].rectTransform.position = origin + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * (115f * p);
                    sparks[i].color = new Color(color.r, color.g, color.b, 1f - p);
                }
                yield return null;
            }
            foreach (var spark in sparks) Destroy(spark.gameObject);
        }

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

        private IEnumerator FriendGemBurst(RectTransform target)
        {
            var hearts = new List<RectTransform>();
            for (int i = 0; i < 5; i++)
            {
                // Jersey 10 不含爱心字形；使用 Numeria 自己的宝石图标表达结交成功，
                // 避免为单个符号混入另一套字体或显示缺字方框。
                var heart = Ui.SpriteImg(_canvasRoot, $"FriendGem{i}", SpriteLib.Pack("UI/Icons/Gem"));
                heart.color = Ui.Hex("#ff6b8a");
                heart.preserveAspect = true;
                heart.rectTransform.sizeDelta = new Vector2(40, 40);
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
