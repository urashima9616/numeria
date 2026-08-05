using System;
using System.Collections;
using Numeria.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Numeria.Game
{
    /// <summary>
    /// 地图控制器:按 MapDef 构建世界(ASCII 网格 + 邻接感知的主题像素美术),
    /// 点触寻路、草丛遇敌、数学宝箱、Boss 守门传送、进化试炼。
    /// </summary>
    public class MapController : MonoBehaviour
    {
        private const double EncounterChance = 0.35;

        private enum BootMode
        {
            Title,
            Resume,
            NewGame
        }

        // AddComponent 会同步执行 Awake；先设置这两个值即可把明确选择的存档传给新地图。
        private static BootMode _nextBootMode = BootMode.Title;
        private static Progress _nextProgress;

        // 项目关闭了 Enter Play Mode 的 domain reload；每次真正启动仍必须回到主菜单。
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetBootState()
        {
            _nextBootMode = BootMode.Title;
            _nextProgress = null;
        }

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
        private SpriteRenderer _bossMarker;
        private Camera _worldCamera;
        private readonly System.Collections.Generic.Dictionary<(int, int), SpriteRenderer> _chestRenderers =
            new System.Collections.Generic.Dictionary<(int, int), SpriteRenderer>();
        private readonly System.Collections.Generic.Dictionary<string, SpriteRenderer> _discoveryRenderers =
            new System.Collections.Generic.Dictionary<string, SpriteRenderer>();
        private SpriteRenderer _merchantRenderer;

        private (int x, int y) _pos;
        private bool _busy;

        private string PlayerId => _progress.CurrentFormId(_progress.ActiveMonId);
        private string PlayerName => GameData.ById(PlayerId).Name;

        private void Awake()
        {
            BootMode bootMode = _nextBootMode;
            _nextBootMode = BootMode.Resume;
            _progress = _nextProgress ?? SaveSystem.Load();
            _nextProgress = null;
            Voice.Enabled = _progress.VoiceEnabled;
            Sfx.Enabled = _progress.SfxEnabled;
            Music.Enabled = _progress.MusicEnabled;
            _def = Maps.Get(_progress.CurrentMap);
            _map = GridMap.Parse(_def.Rows);
            _rng = new Rng((uint)System.Environment.TickCount);
            _voice = gameObject.AddComponent<Voice>();

            BuildWorld();
            BuildHud();
            _puzzles = new PuzzleUi(this, _hudCanvasRoot, _rng, lines => _voice.Say(lines),
                solved => _progress.RecordPuzzle(solved));
            SetupCamera();
            UpdateHud();
            Music.PlayMap(_def.Id);
            if (bootMode == BootMode.Title)
            {
                _busy = true;
                StartCoroutine(StartScreenRoutine());
            }
            else if (bootMode == BootMode.NewGame)
            {
                _busy = true;
                StartCoroutine(NewGameIntroRoutine());
            }
            else _voice.Say(_def.WelcomeLine);
        }

        // ---------- 世界构建 ----------

        private Vector3 TileWorld(int x, int y) => new Vector3(x, _map.Height - 1 - y, 0);

        private static int SortOrder(float worldY) => PaintedTerrainRenderer.SortOrder(worldY);

        private void BuildWorld()
        {
            _mapRoot = new GameObject("MapRoot");
            _mapRoot.transform.SetParent(transform, false);
            PaintedTerrainRenderer.Build(_mapRoot.transform, _map, _def.Theme);

            for (int y = 0; y < _map.Height; y++)
                for (int x = 0; x < _map.Width; x++)
                {
                    var world = TileWorld(x, y);
                    int hash = (x * 73856093) ^ (y * 19349663);
                    int variant = ((hash % 97) + 97) % 97;
                    Tile tile = _map.At(x, y);

                    switch (tile)
                    {
                        case Tile.Water:
                            if (!MapArt.PaintedReady && _def.Theme == "sky" && variant % 17 == 0)
                            {
                                var cloud = AddSprite(MapArt.Prop(_def.Theme, "obstacle", variant),
                                    world + Vector3.up * .08f, 1, "sky-cloud");
                                cloud.color = new Color(1f, 1f, 1f, .86f);
                                ScaleSpriteToHeight(cloud, .58f);
                            }
                            break;
                        case Tile.Cliff:
                            if (!MapArt.PaintedReady && (_def.Theme == "mountains" || _def.Theme == "desert" ||
                                _def.Theme == "dark_mines" || _def.Theme == "underground") && variant % 3 == 0)
                            {
                                var rock = AddSprite(MapArt.Prop(_def.Theme, "obstacle", variant),
                                    world + Vector3.up * .06f, SortOrder(world.y), $"{_def.Theme}-rock");
                                rock.color = MapArt.Tint(_def.Theme, tile, "obstacle");
                                ScaleSpriteToHeight(rock,
                                    _def.Theme == "mountains" || _def.Theme == "dark_mines" ? .72f : .62f);
                            }
                            break;
                        case Tile.Tree:
                            if (!MapArt.PaintedReady)
                            {
                                var obstacle = AddSprite(MapArt.Prop(_def.Theme, "obstacle", variant),
                                    world + Vector3.up * .2f, SortOrder(world.y) + 10, $"{_def.Theme}-obstacle");
                                obstacle.color = MapArt.Tint(_def.Theme, tile, "obstacle");
                                ScaleSpriteToHeight(obstacle, MapArt.PropHeight(_def.Theme, "obstacle"));
                            }
                            break;
                        case Tile.Bush:
                            if (!IsEncounterClusterAnchor(x, y)) break;
                            var encounter = AddSprite(MapArt.Prop(_def.Theme, "encounter", variant),
                                world + Vector3.up * .08f, SortOrder(world.y) + 12, $"{_def.Theme}-encounter");
                            encounter.color = MapArt.Tint(_def.Theme, tile, "encounter");
                            ScaleSpriteToHeight(encounter, MapArt.PropHeight(_def.Theme, "encounter"));
                            break;
                        case Tile.Landmark:
                            if (!MapArt.PaintedReady)
                            {
                                var landmark = AddSprite(MapArt.Prop(_def.Theme, "landmark", variant),
                                    world + Vector3.up * .52f, SortOrder(world.y) + 12, $"{_def.Theme}-landmark");
                                ScaleSpriteToHeight(landmark, MapArt.PropHeight(_def.Theme, "landmark"));
                            }
                            break;
                        case Tile.Bridge:
                            var bridge = AddSprite(MapArt.Prop(_def.Theme, "bridge", variant), world,
                                SortOrder(world.y) + 5, $"{_def.Theme}-bridge");
                            ScaleSpriteToHeight(bridge, MapArt.PropHeight(_def.Theme, "bridge"));
                            break;
                        case Tile.Chest:
                            bool opened = _progress.OpenedChests.Contains(ChestId(x, y));
                            string treasureKind = opened ? "treasure-opened" : "treasure";
                            var treasure = AddSprite(MapArt.Prop(_def.Theme, treasureKind, variant),
                                world + Vector3.up * .08f, SortOrder(world.y) + 15, treasureKind);
                            ScaleSpriteToHeight(treasure, MapArt.PropHeight(_def.Theme, treasureKind));
                            _chestRenderers[(x, y)] = treasure;
                            break;
                        case Tile.Portal:
                            // 主题建筑作为关卡出口，位于角色身后；水沫精灵提供统一的魔法光环。
                            if (!MapArt.PaintedReady)
                            {
                                var portal = AddSprite(MapArt.Prop(_def.Theme, "portal", variant),
                                    world + Vector3.up * .48f, SortOrder(world.y) + 4, $"{_def.Theme}-portal");
                                ScaleSpriteToHeight(portal, MapArt.PropHeight(_def.Theme, "portal"));
                            }
                            _portalGlow = AddSprite(MapArt.Prop(_def.Theme, "portal-glow", variant),
                                world + Vector3.up * .03f, SortOrder(world.y) + 8, "portal-glow");
                            ScaleSpriteToHeight(_portalGlow, MapArt.PropHeight(_def.Theme, "portal-glow"));
                            _bossMarker = AddSprite(SpriteLib.EnemyBattleSprite(_def.BossSpeciesId),
                                world + new Vector3(0, .28f, 0), SortOrder(world.y) + 24, "boss-marker");
                            if (_bossMarker.sprite != null)
                            {
                                float markerScale = .9f / Mathf.Max(.01f, _bossMarker.sprite.bounds.size.y);
                                _bossMarker.transform.localScale = Vector3.one * markerScale;
                            }
                            break;
                    }
                }

            foreach (var discovery in _def.Discoveries ?? Array.Empty<DiscoveryDef>())
            {
                if (_progress.CollectedDiscoveries.Contains(discovery.Id)) continue;
                Vector3 world = TileWorld(discovery.X, discovery.Y);
                var marker = AddSprite(SpriteLib.One("generated/Economy/numeria_coin"),
                    world + Vector3.up * .22f, SortOrder(world.y) + 25, $"discovery-{discovery.Id}");
                ScaleSpriteToHeight(marker, .62f);
                _discoveryRenderers[discovery.Id] = marker;
            }

            if (_def.Merchant != null)
            {
                Vector3 world = TileWorld(_def.Merchant.X, _def.Merchant.Y);
                _merchantRenderer = AddSprite(SpriteLib.One(_def.Merchant.SpriteResource),
                    world + Vector3.up * .32f, SortOrder(world.y) + 30, $"merchant-{_def.Merchant.Id}");
                ScaleSpriteToHeight(_merchantRenderer, 1.35f);
            }

            _pos = _map.Spawn;
            var avatarGo = new GameObject("Avatar");
            avatarGo.transform.SetParent(_mapRoot.transform, false);
            var sr = avatarGo.AddComponent<SpriteRenderer>();
            sr.sortingOrder = SortOrder(TileWorld(_pos.x, _pos.y).y) + 40;
            avatarGo.transform.position = TileWorld(_pos.x, _pos.y);
            _avatar = avatarGo.transform;
            ApplyAvatarSprite();
            RefreshPortalState();
        }

        private bool IsEncounterClusterAnchor(int x, int y) =>
            (y == 0 || _map.At(x, y - 1) != Tile.Bush) &&
            (x == 0 || _map.At(x - 1, y) != Tile.Bush);

        private void ApplyAvatarSprite()
        {
            var sr = _avatar.GetComponent<SpriteRenderer>();
            bool hasLucas = SpriteLib.LucasExplorer() != null;
            sr.sprite = hasLucas ? SpriteLib.LucasExplorer() : SpriteLib.MapSprite(PlayerId);
            if (sr.sprite == null) return;
            float targetHeight = hasLucas ? 1.35f : 1f + 0.14f * _progress.ActiveGrowth.Stage;
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

        private static void ScaleSpriteToHeight(SpriteRenderer renderer, float targetHeight)
        {
            if (renderer == null || renderer.sprite == null) return;
            float scale = targetHeight / Mathf.Max(.01f, renderer.sprite.bounds.size.y);
            renderer.transform.localScale = Vector3.one * scale;
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
            // 扩图后保留可读的探索尺度，不再把整张 30+ 列地图缩成一屏。
            cam.orthographicSize = 7.2f;
            cam.backgroundColor = Ui.Hex(_def.CameraBg);
            cam.clearFlags = CameraClearFlags.SolidColor;
            _worldCamera = cam;
            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            if (_worldCamera == null || _avatar == null) return;
            float halfHeight = _worldCamera.orthographicSize;
            float halfWidth = halfHeight * Mathf.Max(1f, _worldCamera.aspect);
            float minX = Math.Min(halfWidth, (_map.Width - 1) * .5f);
            float maxX = Math.Max(minX, _map.Width - 1 - halfWidth);
            float minY = Math.Min(halfHeight, (_map.Height - 1) * .5f);
            float maxY = Math.Max(minY, _map.Height - 1 - halfHeight);
            _worldCamera.transform.position = new Vector3(
                Mathf.Clamp(_avatar.position.x, minX, maxX),
                Mathf.Clamp(_avatar.position.y, minY, maxY), -10);
        }

        private void RefreshPortalState()
        {
            bool cleared = _def.GateCleared(_progress);
            bool bossReady = !cleared && _def.AllChestsOpened(_progress);
            if (_portalGlow != null) _portalGlow.gameObject.SetActive(cleared || bossReady);
            if (_bossMarker != null) _bossMarker.gameObject.SetActive(bossReady);
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
            Ui.Place(plate.rectTransform, new Vector2(0, 1), new Vector2(20, -20), new Vector2(850, 54));
            Ui.AddOutline(plate.gameObject);
            var coin = Ui.SpriteImg(plate.transform, "Coin", SpriteLib.One("generated/Economy/numeria_coin"));
            coin.preserveAspect = true;
            Ui.Place(coin.rectTransform, new Vector2(0, .5f), new Vector2(10, 0), new Vector2(40, 40));
            _hudText = Ui.Label(plate.transform, "HudText", "", 22, Ui.Ink);
            Ui.Stretch(_hudText.rectTransform);
            _hudText.rectTransform.offsetMin = new Vector2(52, 0);

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
                    // 出战数灵可能在菜单里换了；Lucas 头像不变，HUD 属性需要刷新。
                    ApplyAvatarSprite();
                    UpdateHud();
                    _busy = false;
                },
                onReturnToMenu: saveFirst =>
                {
                    if (saveFirst) SaveSystem.Save(_progress);
                    // 不保存时不传递内存中的进度；标题背景从最后一次磁盘存档重新构建。
                    Respawn(null, BootMode.Title);
                },
                onTravel: mapId =>
                {
                    _progress.CurrentMap = mapId;
                    SaveSystem.Save(_progress);
                    Respawn(_progress);
                },
                onLoad: loaded =>
                {
                    _progress = loaded;
                    Respawn(_progress);
                });
        }

        /// <summary>销毁并重建地图控制器；可明确指定下一次启动所使用的进度和入口。</summary>
        private void Respawn(Progress progress = null, BootMode bootMode = BootMode.Resume)
        {
            _nextProgress = progress;
            _nextBootMode = bootMode;
            var fresh = new GameObject("Map");
            fresh.AddComponent<MapController>();
            Destroy(gameObject);
        }

        private void UpdateHud()
        {
            var growth = _progress.ActiveGrowth;
            var combatant = _progress.PlayerCombatant(_progress.ActiveMonId);
            string xp = growth.Level >= GrowthSystem.MaxLevel ? "MAX" : $"{growth.Xp}/{growth.XpToNext}";
            _hudText.text = $"{_progress.Coins} COINS   {_progress.DigitCrystalCount}/6 CRYSTALS   " +
                            $"{PlayerName} Lv.{growth.Level}  XP {xp}  " +
                            $"ATK {combatant.AttackPower + _progress.TotalAttackBonus(_progress.ActiveMonId)}  " +
                            $"DEF {combatant.DefensePower + _progress.TotalDefenseBonus(_progress.ActiveMonId)}  {_def.DisplayName}";
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
                sr.sortingOrder = SortOrder(to.y) + 10;
                UpdateCameraPosition();

                bool interrupted = HandleTile(step.x, step.y);
                if (interrupted) yield break; // 战斗/宝箱协程接管 _busy
            }
            _busy = false;
        }

        private bool HandleTile(int x, int y)
        {
            DiscoveryDef discovery = _def.DiscoveryAt(x, y);
            if (discovery != null && !_progress.CollectedDiscoveries.Contains(discovery.Id))
            {
                StartCoroutine(DiscoveryRoutine(discovery));
                return true;
            }
            if (_def.MerchantAt(x, y))
            {
                StartCoroutine(MerchantRoutine(_def.Merchant));
                return true;
            }

            switch (_map.At(x, y))
            {
                case Tile.Bush:
                    if (_rng.Next() < EncounterChance)
                    {
                        StartBattle(_def.RollWildEncounter(_progress.ActiveGrowth.Level, _rng), false);
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
                        if (!_def.AllChestsOpened(_progress))
                        {
                            StartCoroutine(BossLockedRoutine());
                            return true;
                        }
                        StartCoroutine(BossEncounterRoutine());
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

        private IEnumerator StartScreenRoutine()
        {
            var cover = Ui.SpriteImg(_hudCanvasRoot, "TitleScreen", SpriteLib.One(_def.BattleBg));
            Ui.Stretch(cover.rectTransform);
            cover.color = new Color(.72f, .76f, .66f, 1f);
            var veil = Ui.Img(cover.transform, "Veil", new Color(.02f, .06f, .05f, .48f));
            Ui.Stretch(veil.rectTransform);

            var panel = Ui.Img(cover.transform, "TitlePanel", new Color(.99f, .96f, .84f, .96f));
            Ui.PlaceCentered(panel.rectTransform, new Vector2(.5f, .5f), new Vector2(220, 0), new Vector2(900, 620));
            Ui.AddOutline(panel.gameObject);
            var title = Ui.DisplayLabel(panel.transform, "Title", "NUMERIA", 92, Ui.Border);
            Ui.Place(title.rectTransform, new Vector2(.5f, 1), new Vector2(0, -48), new Vector2(760, 110));
            var subtitle = Ui.DisplayLabel(panel.transform, "Subtitle", "LUCAS AND THE DIGIT CRYSTALS", 38,
                Ui.Hex("#a65b28"));
            Ui.Place(subtitle.rectTransform, new Vector2(.5f, 1), new Vector2(0, -158), new Vector2(780, 64));

            var crystal = Ui.SpriteImg(panel.transform, "DigitCrystal", SpriteLib.One("generated/Story/digit_crystal"));
            crystal.preserveAspect = true;
            Ui.PlaceCentered(crystal.rectTransform, new Vector2(.5f, .5f), new Vector2(0, 20), new Vector2(140, 140));
            var quest = Ui.Label(panel.transform, "Quest", "BEFRIEND MATHMONS  •  SOLVE MATH MAGIC  •  FIND 6 CRYSTALS",
                25, Ui.Ink);
            Ui.Place(quest.rectTransform, new Vector2(.5f, 0), new Vector2(0, 200), new Vector2(820, 50));

            var lucas = Ui.SpriteImg(cover.transform, "Lucas", SpriteLib.LucasExplorer());
            lucas.preserveAspect = true;
            Ui.PlaceCentered(lucas.rectTransform, new Vector2(0, .5f), new Vector2(235, -5), new Vector2(430, 720));

            bool hasSave = false;
            for (int slot = 1; slot <= SaveSystem.SlotCount; slot++)
                hasSave |= SaveSystem.SlotExists(slot);

            while (true)
            {
                int action = 0;
                var newGame = Ui.Btn(panel.transform, "BtnNewGame", "START A NEW GAME", 32);
                Ui.Place((RectTransform)newGame.transform, new Vector2(.5f, 0), new Vector2(0, 112),
                    new Vector2(520, 76));
                newGame.onClick.AddListener(() => action = 1);
                var loadGame = Ui.Btn(panel.transform, "BtnLoadGame", "LOAD GAME", 32);
                Ui.Place((RectTransform)loadGame.transform, new Vector2(.5f, 0), new Vector2(0, 18),
                    new Vector2(520, 76));
                loadGame.interactable = hasSave;
                loadGame.onClick.AddListener(() => action = 2);
                yield return new WaitUntil(() => action != 0);

                Destroy(newGame.gameObject);
                Destroy(loadGame.gameObject);
                panel.gameObject.SetActive(false);
                lucas.gameObject.SetActive(false);

                var picker = Ui.Img(cover.transform, "TitleSlotPicker", new Color(.99f, .96f, .84f, .98f));
                Ui.PlaceCentered(picker.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1080, 730));
                Ui.AddOutline(picker.gameObject);
                string heading = action == 1 ? "CHOOSE A SLOT FOR YOUR NEW GAME" : "CHOOSE A GAME TO LOAD";
                var pickerTitle = Ui.DisplayLabel(picker.transform, "PickerTitle", heading, 40, Ui.Border);
                Ui.Place(pickerTitle.rectTransform, new Vector2(.5f, 1), new Vector2(0, -28), new Vector2(980, 70));

                int selectedSlot = 0;
                bool back = false;
                for (int slot = 1; slot <= SaveSystem.SlotCount; slot++)
                {
                    int capturedSlot = slot;
                    var summary = SaveSystem.GetSlotSummary(slot);
                    string details = summary.Exists
                        ? $"{summary.MathmonName.ToUpperInvariant()}  LV.{summary.Level}  {summary.MapName.ToUpperInvariant()}"
                        : "EMPTY";
                    string label = $"SLOT {slot}\n{details}";
                    int column = (slot - 1) / 5;
                    int row = (slot - 1) % 5;
                    var slotButton = Ui.Btn(picker.transform, $"TitleSlot{slot}", label, 23);
                    Ui.PlaceCentered((RectTransform)slotButton.transform, new Vector2(.5f, .5f),
                        new Vector2(column == 0 ? -250 : 250, 205 - row * 92), new Vector2(460, 78));
                    slotButton.interactable = action == 1 || summary.Exists;
                    slotButton.onClick.AddListener(() => selectedSlot = capturedSlot);
                }

                var backButton = Ui.Btn(picker.transform, "BtnBackToTitle", "BACK", 25);
                Ui.Place((RectTransform)backButton.transform, new Vector2(.5f, 0), new Vector2(0, 24),
                    new Vector2(300, 64));
                backButton.onClick.AddListener(() => back = true);
                yield return new WaitUntil(() => selectedSlot != 0 || back);
                if (back)
                {
                    Destroy(picker.gameObject);
                    panel.gameObject.SetActive(true);
                    lucas.gameObject.SetActive(true);
                    continue;
                }

                if (action == 1 && SaveSystem.SlotExists(selectedSlot))
                {
                    bool answered = false;
                    bool overwrite = false;
                    var confirm = Ui.Img(cover.transform, "NewGameConfirm", new Color(0, 0, 0, .72f));
                    Ui.Stretch(confirm.rectTransform);
                    var confirmPanel = Ui.Img(confirm.transform, "Panel", Ui.PlateBg);
                    Ui.PlaceCentered(confirmPanel.rectTransform, new Vector2(.5f, .5f), Vector2.zero,
                        new Vector2(760, 340));
                    Ui.AddOutline(confirmPanel.gameObject);
                    var warning = Ui.Label(confirmPanel.transform, "Warning",
                        $"START A NEW GAME IN SLOT {selectedSlot}?\nThe existing game in this slot will be replaced.",
                        29, Ui.Ink);
                    Ui.Place(warning.rectTransform, new Vector2(.5f, 1), new Vector2(0, -54),
                        new Vector2(700, 110));
                    var yes = Ui.Btn(confirmPanel.transform, "BtnConfirmNewGame", "REPLACE & START", 23);
                    Ui.Place((RectTransform)yes.transform, new Vector2(.5f, 0), new Vector2(-155, 48),
                        new Vector2(280, 72));
                    yes.onClick.AddListener(() => { overwrite = true; answered = true; });
                    var no = Ui.Btn(confirmPanel.transform, "BtnCancelNewGame", "CANCEL", 23);
                    Ui.Place((RectTransform)no.transform, new Vector2(.5f, 0), new Vector2(155, 48),
                        new Vector2(280, 72));
                    no.onClick.AddListener(() => answered = true);
                    yield return new WaitUntil(() => answered);
                    Destroy(confirm.gameObject);
                    if (!overwrite)
                    {
                        Destroy(picker.gameObject);
                        panel.gameObject.SetActive(true);
                        lucas.gameObject.SetActive(true);
                        continue;
                    }
                }

                if (action == 1)
                {
                    var fresh = SaveSystem.StartNewGame(selectedSlot);
                    Respawn(fresh, BootMode.NewGame);
                }
                else
                {
                    var loaded = SaveSystem.LoadFromSlot(selectedSlot);
                    if (loaded != null) Respawn(loaded);
                }
                yield break;
            }
        }

        private IEnumerator NewGameIntroRoutine()
        {
            yield return DialogueRoutine("NARRATOR",
                "Lucas wakes beneath a sky full of glowing numbers.", SpriteLib.LucasExplorer());
            yield return DialogueRoutine("LUCAS", "Where am I? This isn't home.", SpriteLib.LucasExplorer());
            yield return DialogueRoutine("VOICE OF NUMERIA",
                "Welcome to Numeria, Lucas. The gate home has lost its power.",
                SpriteLib.One("generated/Story/digit_crystal"));
            yield return DialogueRoutine("VOICE OF NUMERIA",
                "Six Digit Crystals can wake it. Seek the Crystal Guardians.",
                SpriteLib.One("generated/Story/digit_crystal"));
            yield return DialogueRoutine("ADDMANDER",
                "Let's be brave, make Mathmon friends, and solve this together!",
                SpriteLib.MapSprite("addmander"));
            _progress.StoryIntroSeen = true;
            SaveSystem.Save(_progress);
            _voice.Say(_def.WelcomeLine);
            _busy = false;
        }

        private IEnumerator DialogueRoutine(string speaker, string line, Sprite portrait)
        {
            bool next = false;
            var shade = Ui.Img(_hudCanvasRoot, "StoryDialogue", new Color(.02f, .04f, .04f, .58f));
            Ui.Stretch(shade.rectTransform);
            var panel = Ui.Img(shade.transform, "DialoguePanel", Ui.PlateBg);
            Ui.Place(panel.rectTransform, new Vector2(.5f, 0), new Vector2(0, 42), new Vector2(1420, 300));
            Ui.AddOutline(panel.gameObject);

            var image = Ui.SpriteImg(panel.transform, "Portrait", portrait);
            image.preserveAspect = true;
            Ui.PlaceCentered(image.rectTransform, new Vector2(0, .5f), new Vector2(155, 0), new Vector2(270, 270));
            var name = Ui.DisplayLabel(panel.transform, "Speaker", speaker.ToUpperInvariant(), 39,
                Ui.Hex("#a65b28"), TextAnchor.MiddleLeft);
            Ui.Place(name.rectTransform, new Vector2(0, 1), new Vector2(310, -28), new Vector2(820, 58));
            var body = Ui.Label(panel.transform, "Line", line, 34, Ui.Ink, TextAnchor.UpperLeft);
            Ui.Place(body.rectTransform, new Vector2(0, 1), new Vector2(310, -92), new Vector2(840, 160));
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Ellipsis;

            var button = Ui.Btn(panel.transform, "Next", "NEXT", 28);
            Ui.Place((RectTransform)button.transform, new Vector2(1, 0), new Vector2(-36, 30), new Vector2(210, 66));
            button.onClick.AddListener(() => next = true);
            _voice.Say(line);
            yield return new WaitUntil(() => next);
            Destroy(shade.gameObject);
        }

        private IEnumerator BossEncounterRoutine()
        {
            _busy = true;
            Sprite guardian = SpriteLib.One(_def.GuardianSpriteResource);
            foreach (string line in _def.GuardianChallengeLines ?? Array.Empty<string>())
                yield return DialogueRoutine(_def.GuardianName, line, guardian);
            _voice.Say(_def.BossLine);
            yield return new WaitForSeconds(1.5f);
            StartBattle(_def.RollBossEncounter(_progress.ActiveGrowth.Level, _rng), true);
        }

        private void StartBattle(CombatantDef enemy, bool isBoss, MerchantDef merchant = null)
        {
            _busy = true;
            _progress.Records.BattlesStarted++;
            if (_bossMarker != null) _bossMarker.gameObject.SetActive(false);
            Music.Play(isBoss ? MusicMood.Boss : MusicMood.Battle);
            _mapRoot.SetActive(false);
            _hudRoot.SetActive(false);

            var battleGo = new GameObject("Battle");
            var battle = battleGo.AddComponent<BattleController>();
            battle.Init(enemy, _progress, _def.Tier, _def.BattleBg,
                end => StartCoroutine(AfterBattle(end, enemy, isBoss, battleGo, merchant)));
        }

        private IEnumerator AfterBattle(BattleEnd end, CombatantDef enemy, bool isBoss, GameObject battleGo,
            MerchantDef merchant)
        {
            Destroy(battleGo);
            _mapRoot.SetActive(true);
            _hudRoot.SetActive(true);
            SetupCamera();
            Music.PlayMap(_def.Id);

            int levelUps = 0;
            int xpReward = GrowthSystem.VictoryXp(enemy.BaseXp, enemy.Level,
                _progress.ActiveGrowth.Level, isBoss);
            bool openMerchantShop = false;
            switch (end)
            {
                case BattleEnd.Win:
                    _progress.Records.BattlesWon++;
                    levelUps = _progress.GainXp(xpReward);
                    int coins = EconomySystem.VictoryCoins(_def.Tier, isBoss, merchant != null, _rng);
                    _progress.AddCoins(coins);
                    _voice.Say($"You found {coins} Numeria coins!");
                    yield return MaybeDrop(enemy, isBoss);
                    if (merchant != null)
                    {
                        openMerchantShop = _progress.DefeatMerchant(merchant.Id);
                    }
                    else if (isBoss && !_def.GateCleared(_progress))
                    {
                        _progress.Records.BossesDefeated++;
                        yield return GateTrialRoutine();
                        _def.ClearGate(_progress);
                        bool newCrystal = _progress.CollectDigitCrystal(_def.Id);
                        RefreshPortalState();
                        _voice.Say(_def.GateClearLine);
                        yield return new WaitForSeconds(2.5f);
                        if (newCrystal)
                        {
                            yield return DialogueRoutine(_def.GuardianName, _def.GuardianVictoryLine,
                                SpriteLib.One(_def.GuardianSpriteResource));
                            if (_progress.DigitCrystalCount == 6)
                            {
                                yield return DialogueRoutine("DIGIT CRYSTALS",
                                    "The six Digit Crystals sing together. The gate home is awake!",
                                    SpriteLib.One("generated/Story/digit_crystal"));
                                yield return DialogueRoutine("LUCAS",
                                    "I can go home when I am ready—and Numeria will always be waiting.",
                                    SpriteLib.LucasExplorer());
                            }
                        }
                    }
                    break;
                case BattleEnd.Caught:
                    bool joined = false;
                    bool duplicate = false;
                    bool upgraded = false;
                    int catchXp = 0;
                    CatchRosterResult rosterResult = _progress.AddCaught(enemy);
                    if (rosterResult == CatchRosterResult.Added)
                    {
                        joined = true;
                        catchXp = xpReward;
                    }
                    else if (rosterResult == CatchRosterResult.Duplicate)
                    {
                        duplicate = true;
                        catchXp = CatchSystem.ConversionXp(xpReward);
                    }
                    else if (rosterResult == CatchRosterResult.UpgradeAvailable)
                    {
                        bool keepStrongerCatch = false;
                        yield return ResolveStrongerDuplicate(enemy, xpReward, choice => keepStrongerCatch = choice);
                        if (keepStrongerCatch)
                        {
                            upgraded = _progress.AdoptCaptured(enemy);
                            if (upgraded) _voice.Say("Your stronger friend is ready for adventure!");
                        }
                        else
                        {
                            duplicate = true;
                            catchXp = CatchSystem.ConversionXp(xpReward);
                        }
                    }
                    else
                    {
                        string releasedId = null;
                        bool decided = false;
                        yield return ResolveFullTeamCatch(enemy, choice =>
                        {
                            releasedId = choice;
                            decided = true;
                        });
                        if (decided && !string.IsNullOrEmpty(releasedId))
                        {
                            joined = _progress.ReplaceCaught(releasedId, enemy);
                            if (joined) _voice.Say("Your new friend joined the team!");
                        }
                        else
                        {
                            _voice.Say("The new friend returned to the wild.");
                        }
                        catchXp = xpReward;
                    }
                    _progress.Records.BattlesWon++;
                    _progress.Records.MonstersCaught++;
                    if (catchXp > 0) levelUps = _progress.GainXp(catchXp);
                    yield return MaybeDrop(enemy, false);
                    if (duplicate)
                    {
                        _voice.Say("Already best friends! Bonus experience!");
                        yield return new WaitForSeconds(1.8f);
                    }
                    else if (upgraded)
                    {
                        yield return new WaitForSeconds(1.2f);
                    }
                    break;
                case BattleEnd.Lose:
                    _progress.Records.BattlesLost++;
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
            if (openMerchantShop)
            {
                _voice.Say("Shop unlocked!");
                yield return new WaitForSeconds(1.2f);
                yield return ShopRoutine(merchant);
            }
            RefreshPortalState();
            _busy = false;
        }

        /// <summary>
        /// 15 只满员后暂停地图结算。返回 null 表示放走新伙伴；返回已有家族 id 表示用新伙伴替换它。
        /// Starter Addmander 不在可释放列表中。
        /// </summary>
        private IEnumerator ResolveStrongerDuplicate(CombatantDef newcomer, int baseXp, Action<bool> onResolved)
        {
            bool resolved = false;
            bool keepCatch = false;
            string baseId = GameData.BaseId(newcomer.Id);
            var ownedGrowth = _progress.EnsureGrowth(baseId);
            string ownedForm = _progress.CurrentFormId(baseId);
            var ownedSpecies = GameData.SpeciesById(ownedForm);
            var caughtSpecies = GameData.SpeciesById(newcomer.Id);
            var ownedCombatant = _progress.PlayerCombatant(baseId);
            int ownedAttack = ownedCombatant.AttackPower + _progress.TotalAttackBonus(baseId);
            int ownedDefense = ownedCombatant.DefensePower + _progress.TotalDefenseBonus(baseId);
            int bonusXp = CatchSystem.ConversionXp(baseXp);

            var shade = Ui.Img(_hudCanvasRoot, "StrongerCatchOverlay", new Color(.03f, .05f, .04f, .91f));
            Ui.Stretch(shade.rectTransform);
            var panel = Ui.Img(shade.transform, "StrongerCatchPanel", Ui.PlateBg);
            Ui.Place(panel.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(940, 560));
            Ui.AddOutline(panel.gameObject);
            var title = Ui.DisplayLabel(panel.transform, "Title", "A STRONGER FRIEND!", 48, Ui.Ink);
            Ui.Place(title.rectTransform, new Vector2(.5f, 1), new Vector2(0, -36), new Vector2(800, 66));
            var prompt = Ui.Label(panel.transform, "Prompt",
                $"You have {ownedSpecies?.Name ?? baseId} Lv.{ownedGrowth.Level}. " +
                $"HP {ownedCombatant.MaxHp}  ATK {ownedAttack}  DEF {ownedDefense}\n" +
                $"New {caughtSpecies?.Name ?? newcomer.Name} Lv.{newcomer.Level}: " +
                $"HP {newcomer.MaxHp}  ATK {newcomer.AttackPower}  DEF {newcomer.DefensePower}\n" +
                "What should happen?",
                28, Ui.Hex("#8b542f"));
            Ui.Place(prompt.rectTransform, new Vector2(.5f, 1), new Vector2(0, -112), new Vector2(850, 132));
            prompt.textWrappingMode = TextWrappingModes.Normal;

            var currentIcon = Ui.SpriteImg(panel.transform, "CurrentIcon", SpriteLib.MapSprite(ownedForm));
            currentIcon.preserveAspect = true;
            Ui.PlaceCentered(currentIcon.rectTransform, new Vector2(.5f, .5f), new Vector2(-205, 24),
                new Vector2(150, 150));
            var caughtIcon = Ui.SpriteImg(panel.transform, "CaughtIcon", SpriteLib.MapSprite(newcomer.Id));
            caughtIcon.preserveAspect = true;
            Ui.PlaceCentered(caughtIcon.rectTransform, new Vector2(.5f, .5f), new Vector2(205, 24),
                new Vector2(150, 150));

            var replace = Ui.Btn(panel.transform, "BtnKeepStrongerCatch", "KEEP STRONGER FRIEND", 23);
            Ui.Place((RectTransform)replace.transform, new Vector2(.5f, 0), new Vector2(-215, 38),
                new Vector2(390, 80));
            replace.onClick.AddListener(() => { keepCatch = true; resolved = true; });
            var convert = Ui.Btn(panel.transform, "BtnConvertCatchToXp", $"TURN INTO +{bonusXp} XP", 23);
            Ui.Place((RectTransform)convert.transform, new Vector2(.5f, 0), new Vector2(215, 38),
                new Vector2(390, 80));
            convert.onClick.AddListener(() => resolved = true);

            _voice.Say("You caught a stronger friend. Keep it, or turn the catch into experience.");
            yield return new WaitUntil(() => resolved);
            Destroy(shade.gameObject);
            onResolved(keepCatch);
        }

        private IEnumerator ResolveFullTeamCatch(CombatantDef newcomer, Action<string> onResolved)
        {
            string choice = null;
            bool resolved = false;
            var newcomerSpecies = GameData.SpeciesById(newcomer.Id);
            string newcomerName = newcomerSpecies?.Name ?? newcomer.Id;

            var shade = Ui.Img(_hudCanvasRoot, "FullTeamOverlay", new Color(.03f, .05f, .04f, .91f));
            Ui.Stretch(shade.rectTransform);
            var panel = Ui.Img(shade.transform, "FullTeamPanel", Ui.PlateBg);
            Ui.Place(panel.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1050, 820));
            Ui.AddOutline(panel.gameObject);

            var title = Ui.DisplayLabel(panel.transform, "Title",
                $"TEAM FULL  {_progress.TeamCount} / {Progress.TeamCapacity}", 48, Ui.Ink);
            Ui.Place(title.rectTransform, new Vector2(.5f, 1), new Vector2(0, -38), new Vector2(850, 62));
            var prompt = Ui.Label(panel.transform, "Prompt",
                $"{newcomerName} Lv.{newcomer.Level} wants to join! Pick a friend to release, or let {newcomerName} go.",
                25, Ui.Hex("#8b542f"));
            Ui.Place(prompt.rectTransform, new Vector2(.5f, 1), new Vector2(0, -105), new Vector2(900, 62));
            _voice.Say("Your team is full. Choose a friend to release, or let the new friend go.");

            var viewport = Ui.Node(panel.transform, "RosterViewport");
            Ui.PlaceCentered(viewport, new Vector2(.5f, .5f), new Vector2(0, 10), new Vector2(920, 500));
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;

            var grid = Ui.Node(viewport, "RosterGrid");
            grid.anchorMin = new Vector2(0, 1);
            grid.anchorMax = new Vector2(1, 1);
            grid.pivot = new Vector2(.5f, 1);
            grid.anchoredPosition = Vector2.zero;
            grid.sizeDelta = Vector2.zero;
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(215, 108);
            layout.spacing = new Vector2(16, 14);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 4;
            layout.childAlignment = TextAnchor.MiddleCenter;
            grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = grid;

            foreach (string existingId in _progress.CaughtIds)
            {
                string capturedId = existingId;
                var existingGrowth = _progress.EnsureGrowth(existingId);
                string formId = _progress.CurrentFormId(existingId);
                var species = GameData.SpeciesById(formId);
                string name = species?.Name ?? existingId;
                var button = Ui.Btn(grid, $"Replace-{existingId}", $"RELEASE\n{name.ToUpperInvariant()}  LV.{existingGrowth.Level}", 17);
                var icon = Ui.SpriteImg(button.transform, "Icon", SpriteLib.MapSprite(formId));
                icon.preserveAspect = true;
                Ui.Place(icon.rectTransform, new Vector2(0, .5f), new Vector2(10, 0), new Vector2(70, 70));
                var text = button.GetComponentInChildren<TMP_Text>();
                text.alignment = TextAlignmentOptions.MidlineRight;
                text.rectTransform.offsetMin = new Vector2(76, 8);
                text.rectTransform.offsetMax = new Vector2(-8, -8);
                button.onClick.AddListener(() =>
                {
                    choice = capturedId;
                    resolved = true;
                });
            }

            var letGo = Ui.Btn(panel.transform, "LetNewFriendGo", $"LET {newcomerName.ToUpperInvariant()} GO", 24);
            Ui.Place((RectTransform)letGo.transform, new Vector2(.5f, 0), new Vector2(0, 34), new Vector2(420, 70));
            letGo.onClick.AddListener(() => resolved = true);

            yield return new WaitUntil(() => resolved);
            Destroy(shade.gameObject);
            onResolved(choice);
        }

        private static MapPuzzleKind DiscoveryPuzzleKind(DiscoveryDef discovery, int tier)
        {
            string name = (discovery?.Name ?? "").ToLowerInvariant();
            if (name.Contains("count") || name.Contains("number")) return MapPuzzleKind.Counting;
            if (name.Contains("symmetry") || name.Contains("mirror")) return MapPuzzleKind.Symmetry;
            if (name.Contains("balance")) return MapPuzzleKind.Balance;
            if (name.Contains("pattern") || name.Contains("order") || name.Contains("sequence"))
                return tier >= 2 ? MapPuzzleKind.NumberSequence : MapPuzzleKind.Pattern;
            if (name.Contains("shape") || name.Contains("prism")) return MapPuzzleKind.Shape;
            if (name.Contains("difference")) return MapPuzzleKind.Formula;
            if (name.Contains("twin")) return MapPuzzleKind.ChainSum;
            return tier == 1 ? MapPuzzleKind.MakeTen : MapPuzzleKind.Formula;
        }

        /// <summary>
        /// 发光数字符文不是自动拾取：答对和地标主题对应的数学题后，才给予一次性奖励。
        /// 答错不消失，孩子可以原地重试。
        /// </summary>
        private IEnumerator DiscoveryRoutine(DiscoveryDef discovery)
        {
            _busy = true;
            _voice.Say("A number rune is glowing! Solve its math magic!");
            yield return new WaitForSeconds(1.2f);

            bool? solved = null;
            MapPuzzleKind kind = DiscoveryPuzzleKind(discovery, _def.Tier);
            yield return _puzzles.RunPuzzleKind(kind, value => solved = value, _def.Tier);
            if (solved == true && _progress.CollectDiscovery(discovery.Id))
            {
                _progress.AddCoins(discovery.Coins);
                if (discovery.BonusConsumable.HasValue && discovery.BonusAmount > 0)
                    _progress.AddConsumable(discovery.BonusConsumable.Value, discovery.BonusAmount);

                if (_discoveryRenderers.TryGetValue(discovery.Id, out var marker))
                    marker.gameObject.SetActive(false);
                Sfx.Play(SfxCue.Chest, .8f);
                _voice.Say($"You found {discovery.Coins} Numeria coins!");
                yield return new WaitForSeconds(1.2f);

                if (discovery.BonusConsumable == ConsumableType.HealthPotion)
                    _voice.Say($"You found {discovery.BonusAmount} HP Potions! Use them only in battle.");
                else if (discovery.BonusConsumable == ConsumableType.GemSnack)
                    _voice.Say($"You found {discovery.BonusAmount} Gem Snacks! Use them only in battle.");

                SaveSystem.Save(_progress);
                UpdateHud();
            }
            _busy = false;
        }

        private IEnumerator MerchantRoutine(MerchantDef merchant)
        {
            _busy = true;
            if (_progress.DefeatedMerchants.Contains(merchant.Id))
            {
                yield return ShopRoutine(merchant);
                _busy = false;
                yield break;
            }

            _voice.Say(merchant.ChallengeLine);
            yield return new WaitForSeconds(2.2f);
            StartBattle(merchant.Opponent(_progress.ActiveGrowth.Level, _def.Tier, _rng), false, merchant);
        }

        /// <summary>每位商人的库存属于该存档且不可刷新，避免重复刷取强力饰品和进化石。</summary>
        private IEnumerator ShopRoutine(MerchantDef merchant)
        {
            ShopItemDef selected = null;
            bool close = false;
            var shade = Ui.Img(_hudCanvasRoot, "ShopOverlay", new Color(.03f, .05f, .04f, .90f));
            Ui.Stretch(shade.rectTransform);
            var panel = Ui.Img(shade.transform, "ShopPanel", Ui.PlateBg);
            Ui.Place(panel.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1040, 700));
            Ui.AddOutline(panel.gameObject);

            var title = Ui.DisplayLabel(panel.transform, "Title", $"{merchant.Name.ToUpperInvariant()}'S SHOP", 52, Ui.Ink);
            Ui.Place(title.rectTransform, new Vector2(.5f, 1), new Vector2(0, -42), new Vector2(760, 70));
            var coinIcon = Ui.SpriteImg(panel.transform, "Coin", SpriteLib.One("generated/Economy/numeria_coin"));
            coinIcon.preserveAspect = true;
            Ui.Place(coinIcon.rectTransform, new Vector2(1, 1), new Vector2(-210, -46), new Vector2(46, 46));
            var coinText = Ui.DisplayLabel(panel.transform, "CoinText", "", 30, Ui.GemOrange, TextAnchor.MiddleLeft);
            Ui.Place(coinText.rectTransform, new Vector2(1, 1), new Vector2(-154, -46), new Vector2(170, 46));

            var status = Ui.Label(panel.transform, "Status", "LIMITED STOCK — CHOOSE WISELY", 24,
                Ui.Hex("#8b542f"));
            Ui.Place(status.rectTransform, new Vector2(.5f, 1), new Vector2(0, -105), new Vector2(800, 42));

            var rows = Ui.Node(panel.transform, "Stock");
            Ui.Place(rows, new Vector2(.5f, .5f), new Vector2(0, -22), new Vector2(900, 420));
            var layout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            var buttons = new System.Collections.Generic.List<Button>();
            var labels = new System.Collections.Generic.List<TMP_Text>();
            foreach (ShopItemDef stock in merchant.Stock)
            {
                ShopItemDef captured = stock;
                var button = Ui.Btn(rows, $"Buy-{stock.Id}", "", 23);
                ((RectTransform)button.transform).sizeDelta = new Vector2(900, 88);
                var label = button.GetComponentInChildren<TMP_Text>();
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.rectTransform.offsetMin = new Vector2(28, 8);
                label.rectTransform.offsetMax = new Vector2(-24, -8);
                button.onClick.AddListener(() => selected = captured);
                buttons.Add(button);
                labels.Add(label);
            }

            Action refresh = () =>
            {
                coinText.text = $"{_progress.Coins} COINS";
                for (int i = 0; i < merchant.Stock.Length; i++)
                {
                    ShopItemDef stock = merchant.Stock[i];
                    int remaining = Math.Max(0, stock.StockLimit - _progress.PurchaseCount(stock.Id));
                    labels[i].text = remaining == 0
                        ? $"{stock.Name.ToUpperInvariant()}  —  SOLD OUT\n{stock.Description}"
                        : $"{stock.Name.ToUpperInvariant()}  —  {stock.Price} COINS  —  {remaining} LEFT\n{stock.Description}";
                    buttons[i].interactable = remaining > 0;
                }
            };
            refresh();

            var done = Ui.Btn(panel.transform, "Close", "DONE", 26);
            Ui.Place((RectTransform)done.transform, new Vector2(.5f, 0), new Vector2(0, 28), new Vector2(320, 68));
            done.onClick.AddListener(() => close = true);

            while (!close)
            {
                yield return new WaitUntil(() => close || selected != null);
                if (close) break;

                PurchaseResult result = EconomySystem.Buy(_progress, selected);
                selected = null;
                if (result == PurchaseResult.Purchased)
                {
                    status.text = "PURCHASED!";
                    status.color = Ui.CellOn;
                    Sfx.Play(SfxCue.Correct, .8f);
                    _voice.Say("Great choice!");
                    SaveSystem.Save(_progress);
                    UpdateHud();
                }
                else if (result == PurchaseResult.NotEnoughCoins)
                {
                    status.text = "NOT ENOUGH COINS";
                    status.color = Ui.Hex("#c7472f");
                    _voice.Say("Not enough coins.");
                }
                else
                {
                    status.text = "SOLD OUT";
                    status.color = Ui.Hex("#c7472f");
                    _voice.Say("That item is sold out.");
                }
                refresh();
            }

            Destroy(shade.gameObject);
        }

        private IEnumerator MaybeDrop(CombatantDef enemy, bool boss)
        {
            if (!boss && _rng.Next() >= enemy.DropChance) yield break;
            ConsumableType drop = _rng.Next() < .25
                ? (enemy.PreferredDrop == ConsumableType.HealthPotion
                    ? ConsumableType.GemSnack : ConsumableType.HealthPotion)
                : enemy.PreferredDrop;
            _progress.AddConsumable(drop);
            string name = drop == ConsumableType.HealthPotion ? "HP Potion" : "Gem Snack";
            _voice.Say(drop == ConsumableType.HealthPotion
                ? "The enemy dropped an HP Potion!" : "The enemy dropped a Gem Snack!");
            yield return new WaitForSeconds(1.4f);
        }

        private IEnumerator BossLockedRoutine()
        {
            int remaining = 0;
            foreach (string id in _def.ChestIds())
                if (!_progress.OpenedChests.Contains(id)) remaining++;
            _voice.Say($"The portal is quiet. Find {remaining} more treasure chest{(remaining == 1 ? "" : "s")}!");
            yield return new WaitForSeconds(1.6f);
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
                _progress.Records.ChestsOpened++;
                if (_chestRenderers.TryGetValue((x, y), out var sr))
                {
                    sr.sprite = MapArt.Prop(_def.Theme, "treasure-opened", 0);
                    ScaleSpriteToHeight(sr, MapArt.PropHeight(_def.Theme, "treasure-opened"));
                }
                ChestRewardDef reward = null;
                _def.ChestRewards?.TryGetValue(id, out reward);
                if (reward != null && reward.Type == ChestRewardType.EvolutionStone)
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
                else if (reward != null)
                {
                    switch (reward.Type)
                    {
                        case ChestRewardType.HealthPotion:
                            _progress.AddConsumable(ConsumableType.HealthPotion, reward.Amount);
                            _voice.Say($"You found {reward.Amount} HP Potions! Use them only in battle.");
                            break;
                        case ChestRewardType.GemSnack:
                            _progress.AddConsumable(ConsumableType.GemSnack, reward.Amount);
                            _voice.Say($"You found {reward.Amount} Gem Snacks! Use them only in battle.");
                            break;
                        case ChestRewardType.DefenseCharm:
                            _progress.AddAccessory(id, reward.Name, 0, reward.Amount);
                            _voice.Say("You found a defense charm! Equip it to one Mathmon.");
                            break;
                        default:
                            _progress.AddAccessory(id, reward.Name, reward.Amount, 0);
                            _voice.Say("You found an attack charm! Equip it to one Mathmon.");
                            break;
                    }
                }
                RefreshPortalState();
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
                        if (growth.Stage == 0) yield return _puzzles.RunPattern(v => ok = v, 1);
                        else yield return _puzzles.RunSymmetry(v => ok = v, 3);
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
                        if (_rng.Next() < 0.5) yield return _puzzles.RunSymmetry(v => ok = v, growth.Stage + 2);
                        else yield return _puzzles.RunPattern(v => ok = v, growth.Stage + 2);
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
