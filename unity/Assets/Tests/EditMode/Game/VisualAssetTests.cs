using System.Collections.Generic;
using NUnit.Framework;
using Numeria.Core;
using UnityEngine;

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
