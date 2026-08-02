using System.Collections;
using Numeria.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>
    /// 地图控制器:按 MapDef 构建世界(ASCII 网格 + Cainos 美术),
    /// 点触寻路、草丛遇敌、数学宝箱、Boss 守门传送、进化试炼。
    /// </summary>
    public class MapController : MonoBehaviour
    {
        private const double EncounterChance = 0.35;

        private MapDef _def;
        private GridMap _map;
        private Progress _progress;
        private Rng _rng;
        private Voice _voice;
        private PuzzleUi _puzzles;

        private GameObject _mapRoot;
        private GameObject _hudRoot;
        private RectTransform _hudCanvasRoot;
        private Transform _avatar;
        private TMP_Text _hudText;
        private SpriteRenderer _portalGlow;
        private readonly System.Collections.Generic.Dictionary<(int, int), SpriteRenderer> _chestRenderers =
            new System.Collections.Generic.Dictionary<(int, int), SpriteRenderer>();

        private (int x, int y) _pos;
        private bool _busy;

        private string PlayerId => _progress.CurrentFormId(_progress.ActiveMonId);
        private string PlayerName => GameData.ById(PlayerId).Name;

        private void Awake()
        {
            _progress = SaveSystem.Load();
            Voice.Enabled = _progress.VoiceEnabled;
            Sfx.Enabled = _progress.SfxEnabled;
            Music.Enabled = _progress.MusicEnabled;
            _def = Maps.Get(_progress.CurrentMap);
            _map = GridMap.Parse(_def.Rows);
            _rng = new Rng((uint)System.Environment.TickCount);
            _voice = gameObject.AddComponent<Voice>();

            BuildWorld();
            BuildHud();
            _puzzles = new PuzzleUi(this, _hudCanvasRoot, _rng, lines => _voice.Say(lines));
            SetupCamera();
            UpdateHud();
            Music.PlayMap(_def.Id);
            _voice.Say(_def.WelcomeLine);
        }

        // ---------- 世界构建 ----------

        private Vector3 TileWorld(int x, int y) => new Vector3(x, _map.Height - 1 - y, 0);

        private static int SortOrder(float worldY) => 1000 - (int)(worldY * 10);

        private void BuildWorld()
        {
            _mapRoot = new GameObject("MapRoot");
            _mapRoot.transform.SetParent(transform, false);

            for (int y = 0; y < _map.Height; y++)
                for (int x = 0; x < _map.Width; x++)
                {
                    var world = TileWorld(x, y);
                    int hash = (x * 73856093) ^ (y * 19349663);
                    int variant = ((hash % 97) + 97) % 97;
                    if (_def.Theme == "sky")
                    {
                        AddSprite(SpriteLib.Cainos("TX Tileset Stone Ground",
                            $"TX Tileset Stone Ground_{variant % 50}"), world, 0, "sky-stone");
                    }
                    else
                    {
                        string groundName = variant < 6
                            ? $"TX Tileset Grass Flower {variant}"
                            : $"TX Tileset Grass {variant % 4}";
                        AddSprite(SpriteLib.Cainos("TX Tileset Grass", groundName), world, 0, "ground");
                    }

                    switch (_map.At(x, y))
                    {
                        case Tile.Tree:
                            if (_def.Theme == "sky")
                            {
                                string pillar = variant % 4 == 0 ? "TX Props Pillar Broken" : "TX Props Pillar";
                                AddSprite(SpriteLib.Cainos("TX Props", pillar), world, SortOrder(world.y), "sky-pillar");
                            }
                            else
                            {
                                int t = (variant % 3) + 1;
                                AddSprite(SpriteLib.Cainos("TX Plant", $"TX Tree T{t} Lower"), world, SortOrder(world.y), "tree");
                                AddSprite(SpriteLib.Cainos("TX Plant", $"TX Tree T{t} Upper"), world + Vector3.up, SortOrder(world.y), "tree");
                            }
                            break;
                        case Tile.Bush:
                            if (_def.Theme == "sky")
                            {
                                var rune = AddSprite(SpriteLib.Cainos("TX Props", $"TX Props Altar Rune {(variant % 4) + 1}"),
                                    world, SortOrder(world.y), "pattern-rune");
                                rune.color = Ui.Hex("#7fe7ff");
                            }
                            else
                            {
                                AddSprite(SpriteLib.Cainos("TX Plant", $"TX Bush T{(variant % 6) + 1}"), world,
                                    SortOrder(world.y), "bush");
                            }
                            break;
                        case Tile.Chest:
                            bool opened = _progress.OpenedChests.Contains(ChestId(x, y));
                            _chestRenderers[(x, y)] = AddSprite(
                                SpriteLib.Cainos("TX Props", opened ? "TX Props Chest Opened" : "TX Props Chest"),
                                world, SortOrder(world.y), "chest");
                            break;
                        case Tile.Portal:
                            AddSprite(SpriteLib.Cainos("TX Props", "TX Props Altar"), world, SortOrder(world.y), "portal");
                            _portalGlow = AddSprite(SpriteLib.Cainos("TX Props", "TX Props Altar Rune 1"),
                                world, SortOrder(world.y) + 1, "portal-glow");
                            _portalGlow.gameObject.SetActive(_def.GateCleared(_progress));
                            break;
                    }
                }

            _pos = _map.Spawn;
            var avatarGo = new GameObject("Avatar");
            avatarGo.transform.SetParent(_mapRoot.transform, false);
            var sr = avatarGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = SortOrder(TileWorld(_pos.x, _pos.y).y) + 1;
            avatarGo.transform.position = TileWorld(_pos.x, _pos.y);
            _avatar = avatarGo.transform;
            ApplyAvatarSprite();
        }

        private void ApplyAvatarSprite()
        {
            var sr = _avatar.GetComponent<SpriteRenderer>();
            sr.sprite = SpriteLib.MapSprite(PlayerId);
            if (sr.sprite == null) return;
            float targetHeight = 1f + 0.14f * _progress.ActiveGrowth.Stage;
            float scale = targetHeight / Mathf.Max(0.01f, sr.sprite.bounds.size.y);
            _avatar.localScale = Vector3.one * scale;
        }

        private SpriteRenderer AddSprite(Sprite sprite, Vector3 pos, int order, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_mapRoot.transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        private void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = _map.Height / 2f + 0.5f;
            cam.transform.position = new Vector3(_map.Width / 2f - 0.5f, _map.Height / 2f - 0.5f, -10);
            cam.backgroundColor = Ui.Hex(_def.CameraBg);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }

        private void BuildHud()
        {
            _hudRoot = new GameObject("HudCanvas", typeof(RectTransform));
            _hudRoot.transform.SetParent(transform, false);
            var canvas = _hudRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = _hudRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
            _hudRoot.AddComponent<GraphicRaycaster>();
            _hudCanvasRoot = (RectTransform)_hudRoot.transform;

            var plate = Ui.Img(_hudCanvasRoot, "HudPlate", Ui.PlateBg);
            Ui.Place(plate.rectTransform, new Vector2(0, 1), new Vector2(20, -20), new Vector2(500, 54));
            Ui.AddOutline(plate.gameObject);
            _hudText = Ui.Label(plate.transform, "HudText", "", 22, Ui.Ink);
            Ui.Stretch(_hudText.rectTransform);

            var menuBtn = Ui.Btn(_hudCanvasRoot, "BtnMenu", "MENU", 24);
            Ui.Place((RectTransform)menuBtn.transform, new Vector2(1, 1), new Vector2(-20, -20), new Vector2(140, 54));
            menuBtn.onClick.AddListener(OpenMenu);
        }

        private void OpenMenu()
        {
            if (_busy) return;
            _busy = true;
            MenuUi.Open(_hudCanvasRoot, _progress,
                onClose: () =>
                {
                    SaveSystem.Save(_progress);
                    // 出战数灵可能在菜单里换了,刷新头像与 HUD
                    ApplyAvatarSprite();
                    UpdateHud();
                    _busy = false;
                },
                onReset: () =>
                {
                    SaveSystem.Delete();
                    Respawn();
                },
                onTravel: mapId =>
                {
                    _progress.CurrentMap = mapId;
                    SaveSystem.Save(_progress);
                    Respawn();
                });
        }

        /// <summary>销毁并重建地图控制器(切图/重置共用)。</summary>
        private void Respawn()
        {
            var fresh = new GameObject("Map");
            fresh.AddComponent<MapController>();
            Destroy(gameObject);
        }

        private void UpdateHud()
        {
            var growth = _progress.ActiveGrowth;
            _hudText.text = $"{PlayerName} Lv.{growth.Level}  XP {growth.Xp}/{growth.XpToNext}  " +
                            $"ATK +{growth.AttackBonus}  DEF +{growth.DefenseBonus}  {_def.DisplayName}";
        }

        private string ChestId(int x, int y) => $"{_def.Id}-chest-{x}-{y}";

        // ---------- 输入与移动 ----------

        private void Update()
        {
            if (_busy) return;
            var pointer = Pointer.current;
            if (pointer == null || !pointer.press.wasReleasedThisFrame) return;

            Vector2 screen = pointer.position.ReadValue();
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10));
            int x = Mathf.RoundToInt(world.x);
            int yRow = _map.Height - 1 - Mathf.RoundToInt(world.y);
            if (!_map.Walkable(x, yRow)) return;

            var path = _map.FindPath(_pos, (x, yRow));
            if (path.Count > 0) StartCoroutine(WalkRoutine(path));
        }

        private IEnumerator WalkRoutine(System.Collections.Generic.List<(int x, int y)> path)
        {
            _busy = true;
            var sr = _avatar.GetComponent<SpriteRenderer>();
            foreach (var step in path)
            {
                Vector3 from = TileWorld(_pos.x, _pos.y);
                Vector3 to = TileWorld(step.x, step.y);
                sr.flipX = to.x < from.x;
                float t = 0;
                while (t < 0.14f)
                {
                    t += Time.deltaTime;
                    _avatar.position = Vector3.Lerp(from, to, t / 0.14f);
                    yield return null;
                }
                _avatar.position = to;
                _pos = (step.x, step.y);
                sr.sortingOrder = SortOrder(to.y) + 1;

                bool interrupted = HandleTile(step.x, step.y);
                if (interrupted) yield break; // 战斗/宝箱协程接管 _busy
            }
            _busy = false;
        }

        private bool HandleTile(int x, int y)
        {
            switch (_map.At(x, y))
            {
                case Tile.Bush:
                    if (_rng.Next() < EncounterChance)
                    {
                        StartBattle(GameData.RollWild(_def.Wild(), _def.Tier, _rng), false);
                        return true;
                    }
                    return false;
                case Tile.Chest:
                    if (!_progress.OpenedChests.Contains(ChestId(x, y)))
                    {
                        StartCoroutine(ChestRoutine(x, y));
                        return true;
                    }
                    return false;
                case Tile.Portal:
                    if (!_def.GateCleared(_progress))
                    {
                        _voice.Say(_def.BossLine);
                        StartBattle(_def.Boss(), true);
                    }
                    else if (_def.PortalTargetMap != null)
                    {
                        _progress.CurrentMap = _def.PortalTargetMap;
                        SaveSystem.Save(_progress);
                        Respawn();
                    }
                    else
                    {
                        StartCoroutine(ComingSoonRoutine());
                    }
                    return true;
                default:
                    return false;
            }
        }

        // ---------- 事件 ----------

        private void StartBattle(CombatantDef enemy, bool isBoss)
        {
            _busy = true;
            Music.Play(isBoss ? MusicMood.Boss : MusicMood.Battle);
            _mapRoot.SetActive(false);
            _hudRoot.SetActive(false);

            var battleGo = new GameObject("Battle");
            var battle = battleGo.AddComponent<BattleController>();
            battle.Init(enemy, _progress, _def.Tier, _def.BattleBg,
                end => StartCoroutine(AfterBattle(end, enemy, isBoss, battleGo)));
        }

        private IEnumerator AfterBattle(BattleEnd end, CombatantDef enemy, bool isBoss, GameObject battleGo)
        {
            Destroy(battleGo);
            _mapRoot.SetActive(true);
            _hudRoot.SetActive(true);
            SetupCamera();
            Music.PlayMap(_def.Id);

            int levelUps = 0;
            switch (end)
            {
                case BattleEnd.Win:
                    levelUps = _progress.GainXp(5);
                    if (isBoss && !_def.GateCleared(_progress))
                    {
                        yield return GateTrialRoutine();
                        _def.ClearGate(_progress);
                        if (_portalGlow != null) _portalGlow.gameObject.SetActive(true);
                        _voice.Say(_def.GateClearLine);
                        yield return new WaitForSeconds(2.5f);
                    }
                    break;
                case BattleEnd.Caught:
                    bool isNew = _progress.Catch(enemy.Id);
                    levelUps = _progress.GainXp(isNew ? 5 : 10); // 重复捕捉转化为双倍经验
                    if (!isNew)
                    {
                        _voice.Say("Already best friends! Bonus experience!");
                        yield return new WaitForSeconds(1.8f);
                    }
                    break;
                case BattleEnd.Lose:
                    _voice.Say("Let's rest and try again!");
                    break;
            }

            if (levelUps > 0)
            {
                Sfx.Play(SfxCue.LevelUp);
                _voice.Say($"Level up! {PlayerName} is getting stronger!");
                yield return new WaitForSeconds(1.5f);
            }

            SaveSystem.Save(_progress);
            UpdateHud();

            yield return MaybeEvolve();
            _busy = false;
        }

        /// <summary>
        /// 传送门钥匙的另一半：Boss 胜利后完成三种不同的本地图谜题。
        /// 答错不惩罚、同题型可无限重试，直到三枚符文全部点亮。
        /// </summary>
        private IEnumerator GateTrialRoutine()
        {
            _voice.Say("Portal trial! Solve three magic puzzles!");
            yield return new WaitForSeconds(1.8f);
            foreach (MapPuzzleKind kind in PuzzleGenerator.GatePuzzleKinds(_rng, _def.Tier))
            {
                bool solved = false;
                while (!solved)
                {
                    bool? ok = null;
                    yield return _puzzles.RunPuzzleKind(kind, value => ok = value, _def.Tier);
                    solved = ok.Value;
                }
            }
            Sfx.Play(SfxCue.Victory, 0.8f);
        }

        private IEnumerator ChestRoutine(int x, int y)
        {
            _busy = true;
            _voice.Say("A math chest! Solve the lock!");
            yield return new WaitForSeconds(1.2f);
            bool? ok = null;
            yield return _puzzles.RunTierPuzzle(v => ok = v, _def.Tier);
            if (ok.Value)
            {
                Sfx.Play(SfxCue.Chest);
                string id = ChestId(x, y);
                _progress.OpenChest(id);
                if (_chestRenderers.TryGetValue((x, y), out var sr))
                    sr.sprite = SpriteLib.Cainos("TX Props", "TX Props Chest Opened");
                string itemName = null;
                if (_def.ChestItems != null && _def.ChestItems.TryGetValue(id, out itemName))
                    _progress.Items.Add(itemName);

                if (id == _def.EvoChestId)
                {
                    _progress.AddEvolutionStone();
                    _voice.Say("You found the Evolution Stone!");
                    yield return new WaitForSeconds(2f);
                    var growth = _progress.ActiveGrowth;
                    int requiredLevel = GameData.NextEvolutionLevel(growth.BaseId, growth.Stage);
                    if (requiredLevel > 0 && growth.Level < requiredLevel)
                    {
                        _voice.Say($"Reach level {requiredLevel} to evolve!");
                        yield return new WaitForSeconds(1.5f);
                    }
                }
                else
                {
                    bool defenseReward = !string.IsNullOrEmpty(itemName) &&
                        (itemName.Contains("Charm") || itemName.Contains("Ring") || itemName.Contains("Shield"));
                    if (defenseReward)
                    {
                        _progress.ActiveGrowth.DefenseBonus++;
                        _voice.Say("Defense goes up by one!");
                    }
                    else
                    {
                        _progress.ActiveGrowth.AttackBonus++;
                        _voice.Say("Attack goes up by one!");
                    }
                }
                SaveSystem.Save(_progress);
                UpdateHud();
            }
            // 没解开:宝箱保持关闭,随时可以再来试——零惩罚
            yield return MaybeEvolve();
            _busy = false;
        }

        /// <summary>进化条件满足时触发该家族亲和题型的三题试炼，零惩罚无限重试。</summary>
        private IEnumerator MaybeEvolve()
        {
            var growth = _progress.ActiveGrowth;
            if (!_progress.CanEvolve(growth.BaseId)) yield break;
            var line = GameData.LineFor(growth.BaseId);
            string fromId = GameData.FormId(growth.BaseId, growth.Stage);
            string toId = GameData.FormId(growth.BaseId, growth.Stage + 1);
            string fromName = GameData.ById(fromId).Name;
            string toName = GameData.ById(toId).Name;

            _voice.Say("Evolution trial! Solve three puzzles!");
            yield return new WaitForSeconds(2f);
            int solved = 0;
            while (solved < 3)
            {
                bool? ok = null;
                switch (line.Affinity)
                {
                    case PuzzleAffinity.MakeTen:
                        yield return _puzzles.RunMakeTen(v => ok = v, growth.Stage == 0 ? 10 : 12);
                        break;
                    case PuzzleAffinity.Pattern:
                        if (growth.Stage == 0) yield return _puzzles.RunPattern(v => ok = v);
                        else yield return _puzzles.RunSymmetry(v => ok = v);
                        break;
                    case PuzzleAffinity.Counting:
                        if (_rng.Next() < 0.5) yield return _puzzles.RunCounting(v => ok = v);
                        else yield return _puzzles.RunComparison(v => ok = v);
                        break;
                    case PuzzleAffinity.RepeatedAddition:
                        if (_rng.Next() < 0.5) yield return _puzzles.RunDouble(v => ok = v);
                        else yield return _puzzles.RunChainSum(v => ok = v);
                        break;
                    case PuzzleAffinity.Symmetry:
                        if (_rng.Next() < 0.5) yield return _puzzles.RunSymmetry(v => ok = v);
                        else yield return _puzzles.RunRotation(v => ok = v);
                        break;
                    default:
                        yield return _puzzles.RunFormula(v => ok = v, growth.Stage == 0 ? 2 : 3);
                        break;
                }
                if (ok.Value) solved++;
            }

            _voice.Say($"{fromName} is evolving!");
            Music.Play(MusicMood.Evolution);
            Sfx.Play(SfxCue.Evolution);
            var sr = _avatar.GetComponent<SpriteRenderer>();
            // 蜕变演出:白闪 + 放大
            for (int i = 0; i < 6; i++)
            {
                sr.color = i % 2 == 0 ? Color.white * 5f : Color.white;
                _avatar.localScale = Vector3.one * (1f + 0.12f * i);
                yield return new WaitForSeconds(0.25f);
            }
            _progress.AdvanceEvolution(growth.BaseId);
            ApplyAvatarSprite();
            sr.color = Color.white;
            _voice.Say($"Amazing! {fromName} evolved into {toName}!");
            SaveSystem.Save(_progress);
            UpdateHud();
            yield return new WaitForSeconds(2.5f);
            Music.PlayMap(_def.Id);
        }

        private IEnumerator ComingSoonRoutine()
        {
            _busy = true;
            var banner = Ui.Img(_hudCanvasRoot, "PortalBanner", new Color(0.06f, 0.09f, 0.13f, 0.92f));
            Ui.Stretch(banner.rectTransform);
            var title = Ui.Label(banner.transform, "Title", _def.NextName, 64, Color.white);
            Ui.Place(title.rectTransform, new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(900, 90));
            var sub = Ui.Label(banner.transform, "Sub", "Coming soon!", 32, Ui.Hex("#ffe082"));
            Ui.Place(sub.rectTransform, new Vector2(0.5f, 0.46f), Vector2.zero, new Vector2(800, 50));
            var btn = Ui.Btn(banner.transform, "BtnBack", "Back", 28);
            Ui.Place((RectTransform)btn.transform, new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(320, 80));
            bool done = false;
            btn.onClick.AddListener(() => done = true);
            yield return new WaitUntil(() => done);
            Destroy(banner.gameObject);
            _busy = false;
        }
    }
}
