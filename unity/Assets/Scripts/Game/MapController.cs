using System.Collections;
using Numeria.Core;
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
        private Text _hudText;
        private SpriteRenderer _portalGlow;
        private readonly System.Collections.Generic.Dictionary<(int, int), SpriteRenderer> _chestRenderers =
            new System.Collections.Generic.Dictionary<(int, int), SpriteRenderer>();

        private (int x, int y) _pos;
        private bool _busy;

        private string PlayerId =>
            _progress.ActiveMonId == "addmander" && _progress.Evolved ? "sumdrake" : _progress.ActiveMonId;
        private string PlayerName => GameData.PlayerMon(_progress.ActiveMonId, _progress.Evolved).Name;

        private void Awake()
        {
            _progress = SaveSystem.Load();
            Voice.Enabled = _progress.VoiceEnabled;
            _def = Maps.Get(_progress.CurrentMap);
            _map = GridMap.Parse(_def.Rows);
            _rng = new Rng((uint)System.Environment.TickCount);
            _voice = gameObject.AddComponent<Voice>();

            BuildWorld();
            BuildHud();
            _puzzles = new PuzzleUi(this, _hudCanvasRoot, _rng, lines => _voice.Say(lines));
            SetupCamera();
            UpdateHud();
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
                    string groundName = variant < 6
                        ? $"TX Tileset Grass Flower {variant}"
                        : $"TX Tileset Grass {variant % 4}";
                    AddSprite(SpriteLib.Cainos("TX Tileset Grass", groundName), world, 0, "ground");

                    switch (_map.At(x, y))
                    {
                        case Tile.Tree:
                            int t = (variant % 3) + 1;
                            AddSprite(SpriteLib.Cainos("TX Plant", $"TX Tree T{t} Lower"), world, SortOrder(world.y), "tree");
                            AddSprite(SpriteLib.Cainos("TX Plant", $"TX Tree T{t} Upper"), world + Vector3.up, SortOrder(world.y), "tree");
                            break;
                        case Tile.Bush:
                            AddSprite(SpriteLib.Cainos("TX Plant", $"TX Bush T{(variant % 6) + 1}"), world, SortOrder(world.y), "bush");
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
            sr.sprite = SpriteLib.One($"Art/Sprites/{PlayerId}");
            sr.sortingOrder = SortOrder(TileWorld(_pos.x, _pos.y).y) + 1;
            avatarGo.transform.position = TileWorld(_pos.x, _pos.y);
            _avatar = avatarGo.transform;
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
                    _avatar.GetComponent<SpriteRenderer>().sprite = SpriteLib.One($"Art/Sprites/{PlayerId}");
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
            _hudText.text = $"{PlayerName} Lv.{_progress.Level}  XP {_progress.Xp}/{_progress.XpToNext}  " +
                            $"ATK +{_progress.AttackBonus}  {_def.DisplayName}";
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
                        StartBattle(_def.Wild(), false);
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

            int levelUps = 0;
            switch (end)
            {
                case BattleEnd.Win:
                    levelUps = _progress.GainXp(5);
                    if (isBoss && !_def.GateCleared(_progress))
                    {
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
                _voice.Say($"Level up! {PlayerName} is getting stronger!");
                yield return new WaitForSeconds(1.5f);
            }

            SaveSystem.Save(_progress);
            UpdateHud();

            yield return MaybeEvolve();
            _busy = false;
        }

        private IEnumerator ChestRoutine(int x, int y)
        {
            _busy = true;
            _voice.Say("A math chest! Solve the lock!");
            yield return new WaitForSeconds(1.2f);
            bool? ok = null;
            yield return _puzzles.RunFormula(v => ok = v, _def.Tier);
            if (ok.Value)
            {
                string id = ChestId(x, y);
                _progress.OpenChest(id);
                if (_chestRenderers.TryGetValue((x, y), out var sr))
                    sr.sprite = SpriteLib.Cainos("TX Props", "TX Props Chest Opened");
                if (_def.ChestItems != null && _def.ChestItems.TryGetValue(id, out var itemName))
                    _progress.Items.Add(itemName);

                if (id == _def.EvoChestId)
                {
                    _progress.HasEvoStone = true;
                    _voice.Say("You found the Evolution Stone!");
                    yield return new WaitForSeconds(2f);
                    if (!_progress.Evolved && _progress.Level < 5)
                    {
                        _voice.Say("Reach level five to evolve!");
                        yield return new WaitForSeconds(1.5f);
                    }
                }
                else
                {
                    _progress.AttackBonus++;
                    _voice.Say("Attack goes up by one!");
                }
                SaveSystem.Save(_progress);
                UpdateHud();
            }
            // 没解开:宝箱保持关闭,随时可以再来试——零惩罚
            yield return MaybeEvolve();
            _busy = false;
        }

        /// <summary>进化条件满足时触发试炼:三道 tier2 谜题,零惩罚无限重试。</summary>
        private IEnumerator MaybeEvolve()
        {
            if (_progress.Evolved || !_progress.HasEvoStone || _progress.Level < 5) yield break;

            _voice.Say("Evolution trial! Solve three puzzles!");
            yield return new WaitForSeconds(2f);
            int solved = 0;
            while (solved < 3)
            {
                bool? ok = null;
                yield return _puzzles.RunFormula(v => ok = v, 2);
                if (ok.Value) solved++;
            }

            _voice.Say($"{PlayerName} is evolving!");
            var sr = _avatar.GetComponent<SpriteRenderer>();
            // 蜕变演出:白闪 + 放大
            for (int i = 0; i < 6; i++)
            {
                sr.color = i % 2 == 0 ? Color.white * 5f : Color.white;
                _avatar.localScale = Vector3.one * (1f + 0.12f * i);
                yield return new WaitForSeconds(0.25f);
            }
            _progress.Evolved = true;
            sr.sprite = SpriteLib.One($"Art/Sprites/{PlayerId}");
            sr.color = Color.white;
            _avatar.localScale = Vector3.one * 1.15f; // 进化体稍大
            _voice.Say("Amazing! Addmander evolved into Sumdrake!");
            SaveSystem.Save(_progress);
            UpdateHud();
            yield return new WaitForSeconds(2.5f);
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
