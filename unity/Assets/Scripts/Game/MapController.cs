using System.Collections;
using Numeria.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>
    /// 神秘森林地图:ASCII 网格 + Cainos 美术,点触寻路、草丛遇敌、
    /// 数学宝箱、Boss 守门传送门。地面/植被用 Cainos,数灵用自绘像素。
    /// </summary>
    public class MapController : MonoBehaviour
    {
        // '.'草地 'T'树 'b'草丛(遇敌) 'C'宝箱 'P'传送门 'S'出生点
        private static readonly string[] ForestRows =
        {
            "TTTTTTTTTTTTTTTTTTTT",
            "T....bb....T...bb..T",
            "T.S..bb........bb..T",
            "T..........T.......T",
            "T...T..bbb.....C...T",
            "T...T..bbb.........T",
            "T......bbb...TT....T",
            "T.C.........TTT..P.T",
            "T....bb............T",
            "T....bb....bbb.....T",
            "T..........bbb.....T",
            "TTTTTTTTTTTTTTTTTTTT",
        };

        private const double EncounterChance = 0.35;

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

        private void Awake()
        {
            _map = GridMap.Parse(ForestRows);
            _progress = SaveSystem.Load();
            Voice.Enabled = _progress.VoiceEnabled;
            _rng = new Rng((uint)System.Environment.TickCount);
            _voice = gameObject.AddComponent<Voice>();

            BuildWorld();
            BuildHud();
            _puzzles = new PuzzleUi(this, _hudCanvasRoot, _rng, lines => _voice.Say(lines));
            SetupCamera();
            UpdateHud();
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
                    // 地面:每格铺草地变体(确定性伪随机保证画面稳定)
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
                            var lower = SpriteLib.Cainos("TX Plant", $"TX Tree T{t} Lower");
                            var upper = SpriteLib.Cainos("TX Plant", $"TX Tree T{t} Upper");
                            AddSprite(lower, world, SortOrder(world.y), "tree");
                            AddSprite(upper, world + Vector3.up, SortOrder(world.y), "tree");
                            break;
                        case Tile.Bush:
                            var bush = SpriteLib.Cainos("TX Plant", $"TX Bush T{(variant % 6) + 1}");
                            AddSprite(bush, world, SortOrder(world.y), "bush");
                            break;
                        case Tile.Chest:
                            bool opened = _progress.OpenedChests.Contains(ChestId(x, y));
                            var chestSprite = SpriteLib.Cainos("TX Props",
                                opened ? "TX Props Chest Opened" : "TX Props Chest");
                            _chestRenderers[(x, y)] = AddSprite(chestSprite, world, SortOrder(world.y), "chest");
                            break;
                        case Tile.Portal:
                            AddSprite(SpriteLib.Cainos("TX Props", "TX Props Altar"), world, SortOrder(world.y), "portal");
                            _portalGlow = AddSprite(SpriteLib.Cainos("TX Props", "TX Props Altar Rune 1"),
                                world, SortOrder(world.y) + 1, "portal-glow");
                            _portalGlow.gameObject.SetActive(_progress.BossBeaten);
                            break;
                    }
                }

            // 玩家头像(自绘 Addmander)
            _pos = _map.Spawn;
            var avatarGo = new GameObject("Avatar");
            avatarGo.transform.SetParent(_mapRoot.transform, false);
            var sr = avatarGo.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteLib.One("Art/Sprites/addmander");
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
            cam.backgroundColor = Ui.Hex("#2f4f2f");
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
            Ui.Place(plate.rectTransform, new Vector2(0, 1), new Vector2(20, -20), new Vector2(430, 54));
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
                    UpdateHud();
                    _busy = false;
                },
                onReset: () =>
                {
                    SaveSystem.Delete();
                    var fresh = new GameObject("ForestMap");
                    fresh.AddComponent<MapController>();
                    Destroy(gameObject);
                });
        }

        private void UpdateHud()
        {
            _hudText.text = $"Addmander Lv.{_progress.Level}  XP {_progress.Xp}/{_progress.XpToNext}  " +
                            $"ATK +{_progress.AttackBonus}  Team {1 + _progress.CaughtIds.Count}";
        }

        private static string ChestId(int x, int y) => $"forest-chest-{x}-{y}";

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
                        StartBattle(GameData.Countipillar(), false);
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
                    if (!_progress.BossBeaten)
                    {
                        _voice.Say("Duplirock guards the portal!");
                        StartBattle(GameData.Duplirock(), true);
                    }
                    else
                    {
                        StartCoroutine(PortalRoutine());
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
            battle.Init(enemy, _progress, end => StartCoroutine(AfterBattle(end, enemy, isBoss, battleGo)));
        }

        private IEnumerator AfterBattle(BattleEnd end, CombatantDef enemy, bool isBoss, GameObject battleGo)
        {
            Destroy(battleGo);
            _mapRoot.SetActive(true);
            _hudRoot.SetActive(true);
            SetupCamera(); // 战斗可能动过相机设置,恢复

            int levelUps = 0;
            switch (end)
            {
                case BattleEnd.Win:
                    levelUps = _progress.GainXp(5);
                    if (isBoss && !_progress.BossBeaten)
                    {
                        _progress.BossBeaten = true;
                        if (_portalGlow != null) _portalGlow.gameObject.SetActive(true);
                        _voice.Say("The portal is open! A new world awaits!");
                        yield return new WaitForSeconds(2.5f);
                    }
                    break;
                case BattleEnd.Caught:
                    _progress.Catch(enemy.Id);
                    levelUps = _progress.GainXp(5);
                    break;
                case BattleEnd.Lose:
                    _voice.Say("Let's rest and try again!");
                    break;
            }

            if (levelUps > 0)
            {
                _voice.Say("Level up! Addmander is getting stronger!");
                yield return new WaitForSeconds(1.5f);
            }

            SaveSystem.Save(_progress);
            UpdateHud();
            _busy = false;
        }

        private IEnumerator ChestRoutine(int x, int y)
        {
            _busy = true;
            _voice.Say("A math chest! Solve the lock!");
            yield return new WaitForSeconds(1.2f);
            bool? ok = null;
            yield return _puzzles.RunFormula(v => ok = v);
            if (ok.Value)
            {
                _progress.OpenChest(ChestId(x, y));
                _progress.AttackBonus++;
                if (_chestRenderers.TryGetValue((x, y), out var sr))
                    sr.sprite = SpriteLib.Cainos("TX Props", "TX Props Chest Opened");
                _voice.Say("Attack goes up by one!");
                SaveSystem.Save(_progress);
                UpdateHud();
            }
            // 没解开:宝箱保持关闭,随时可以再来试——零惩罚
            _busy = false;
        }

        private IEnumerator PortalRoutine()
        {
            _busy = true;
            _voice.Say("The portal is open! A new world awaits!");
            var banner = Ui.Img(_hudCanvasRoot, "PortalBanner", new Color(0.06f, 0.09f, 0.13f, 0.92f));
            Ui.Stretch(banner.rectTransform);
            var title = Ui.Label(banner.transform, "Title", "Silent Peaks", 64, Color.white);
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
