using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Numeria.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Numeria.Game.Tests
{
    public class VisualAssetTests
    {
        [Test]
        public void EveryFamilyThemeSkill_HasItsOwnImportedIcon()
        {
            var paths = new HashSet<string>();
            foreach (var line in GameData.Lines)
            {
                var player = GameData.PlayerMon(line.BaseId, 0);
                var theme = System.Array.Find(player.Skills, skill => skill.Type == SkillType.Formula);
                Assert.IsNotNull(theme, line.BaseId);
                paths.Add(theme.IconResource);
                Assert.IsNotNull(Resources.Load<Sprite>(theme.IconResource),
                    $"Missing imported theme icon: {theme.IconResource}");
            }
            Assert.AreEqual(16, paths.Count,
                "Launch families have 11 icons; the five expanded elements each add one visual language.");
        }

        [Test]
        public void LucasExplorer_HasAnImportedTransparentSprite()
        {
            var sprite = Resources.Load<Sprite>("generated/Heroes/lucas_explorer");
            Assert.IsNotNull(sprite);
            Assert.Greater(sprite.texture.width, 256);
            Assert.Greater(sprite.texture.height, 256);
        }

        [Test]
        public void EveryRegisteredElement_HasAReadableTeamMenuLabel()
        {
            foreach (var line in GameData.Lines)
                Assert.AreEqual(line.Element, MenuUi.TypeLabelFor(line.BaseId),
                    $"Missing TEAM type presentation for {line.BaseId} ({line.Element})");
        }

        [Test]
        public void EveryMathmonHasAnIndependentTransparentMegaSprite()
        {
            Assert.AreEqual(141, GameData.Roster.Count);
            foreach (var species in GameData.Roster)
            {
                var mega = Resources.Load<Sprite>($"generated/{species.Id}_mega_icon");
                Assert.IsNotNull(mega, $"Missing Mega sprite for {species.Id}");
                Assert.AreEqual(512, mega.texture.width, species.Id);
                Assert.AreEqual(512, mega.texture.height, species.Id);
                Assert.AreNotSame(SpriteLib.LargeIcon(species.Id), mega, species.Id);
            }
        }

        [Test]
        public void BattleUiSwapsBetweenNormalAndRedesignedMegaSprite()
        {
            var host = new GameObject("MegaBattleUiTest");
            bool voiceWasEnabled = Voice.Enabled;
            try
            {
                // EditMode 不执行 Voice.Awake 的运行时音频初始化；本测试只验证 UI 层级。
                Voice.Enabled = false;
                var controller = host.AddComponent<BattleController>();
                controller.Init(GameData.Countipillar(), new Progress(), 1,
                    "generated/NUMERIA_Unity_Battle_Assets/Backgrounds/Sunny_Meadow_2048x1152", _ => { });
                var transforms = host.GetComponentsInChildren<Transform>(true);
                Transform megaButton = transforms.Single(t => t.name == "BtnMega");
                var playerImage = transforms.Single(t => t.name == "PlayerSprite").GetComponent<Image>();
                Assert.False(megaButton.GetComponent<Button>().interactable);

                var stateField = typeof(BattleController).GetField("_state",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var render = typeof(BattleController).GetMethod("RenderAll",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                var state = (BattleState)stateField.GetValue(controller);
                var normalSprite = SpriteLib.PlayerBattleSprite(state.Player.Id);
                var megaSprite = Resources.Load<Sprite>($"generated/{state.Player.Id}_mega_icon");
                Assert.AreSame(normalSprite, playerImage.sprite);
                Assert.IsNotNull(megaSprite);
                state.Gems = 7;
                Assert.True(state.TryActivateMega(true));
                render.Invoke(controller, null);

                Assert.AreSame(megaSprite, playerImage.sprite);
                StringAssert.Contains("NOVA", megaButton.Find("Label").GetComponent<TMP_Text>().text);
                Assert.IsNotNull(transforms.SingleOrDefault(t => t.name == "SubT" &&
                    t.GetComponent<TMP_Text>()?.text == "FREE — MEGA"));
                Assert.AreEqual(Vector3.one, transforms.Single(t => t.name == "PlayerSprite").localScale);

                while (state.MegaActive) state.ConsumeMegaTurn();
                render.Invoke(controller, null);
                Assert.AreSame(normalSprite, playerImage.sprite);
                Assert.AreEqual(Vector3.one, transforms.Single(t => t.name == "PlayerSprite").localScale);
            }
            finally
            {
                Voice.Enabled = voiceWasEnabled;
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void MapAssetCatalog_HasPaintedTerrainAndInteractionMarkers()
        {
            var catalog = Resources.Load<TinySwordsCatalog>("generated/TinySwordsMapCatalog");
            Assert.IsNotNull(catalog, "Import local map packs, then run Numeria/Rebuild Map Asset Catalogs.");
            Assert.IsTrue(MapArt.PaintedReady);
            Assert.IsNotNull(MapArt.Prop("forest", "treasure", 0));
            Assert.IsNotNull(MapArt.Prop("forest", "treasure-opened", 0));
            Assert.IsNotNull(MapArt.Prop("sky", "portal-glow", 0));
            foreach (var def in Maps.All())
                Assert.IsNotNull(MapArt.Prop(def.Theme, "encounter", 7), def.Id);
        }

        [Test]
        public void EveryChapter_UsesOnlyPaintedTerrainTiles()
        {
            var catalog = Resources.Load<TinySwordsCatalog>("generated/TinySwordsMapCatalog");
            Assert.IsTrue(MapArt.PaintedReady,
                "Import Tiles and Hexes: 2D Painted Terrain Samples, then rebuild the map catalog.");

            var painted = new HashSet<Sprite>
            {
                catalog.PaintedBase, catalog.PaintedDesert, catalog.PaintedForest,
                catalog.PaintedSnowForest, catalog.PaintedJungle, catalog.PaintedMountain,
                catalog.PaintedOcean, catalog.PaintedPlains, catalog.PaintedCastle,
                catalog.PaintedVolcano,
            };
            Assert.AreEqual(10, painted.Count);
            Assert.IsFalse(painted.Contains(null));

            foreach (var def in Maps.All())
                foreach (Tile tile in System.Enum.GetValues(typeof(Tile)))
                    Assert.IsTrue(painted.Contains(MapArt.Terrain(def.Theme, tile, 7, 15)),
                        $"{def.Id}/{tile} escaped the Painted Terrain tile set.");
        }
    }
}
