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
        public void TinySwordsMapCatalog_HasEveryRuntimeAssetFamily()
        {
            var catalog = Resources.Load<TinySwordsCatalog>("generated/TinySwordsMapCatalog");
            Assert.IsNotNull(catalog, "Import local map packs, then run Numeria/Rebuild Map Asset Catalogs.");
            Assert.IsTrue(MapArt.Ready);
            Assert.GreaterOrEqual(catalog.Terrain1.Length, 44);
            Assert.IsNotNull(catalog.Water);
            Assert.IsNotNull(catalog.Bridge);
            Assert.AreEqual(6, catalog.Landmarks.Length);
            Assert.AreEqual(6, catalog.Portals.Length);
            Assert.AreEqual(6, catalog.Treasures.Length);
            Assert.IsNotNull(catalog.TreasureOpened);
            CollectionAssert.AllItemsAreNotNull(catalog.Landmarks);
            CollectionAssert.AllItemsAreNotNull(catalog.Portals);
            CollectionAssert.AllItemsAreNotNull(catalog.Treasures);
        }

        [Test]
        public void PaintedAndCaveMapPacks_AreUsedByTheirAssignedChapters()
        {
            var catalog = Resources.Load<TinySwordsCatalog>("generated/TinySwordsMapCatalog");
            Assert.IsTrue(MapArt.PaintedReady,
                "The first four chapters require Tiles and Hexes: 2D Painted Terrain Samples.");
            Assert.IsTrue(MapArt.CaveReady,
                "Import RPG Worlds Caves, then run Numeria/Rebuild Map Asset Catalogs.");
            Assert.GreaterOrEqual(catalog.CaveFloorDark.Length, 4);
            Assert.GreaterOrEqual(catalog.CaveFloorPurple.Length, 4);
            Assert.GreaterOrEqual(catalog.CaveWalls.Length, 4);
            Assert.GreaterOrEqual(catalog.CaveCrystals.Length, 4);
            CollectionAssert.AllItemsAreNotNull(catalog.CaveFloorDark);
            CollectionAssert.AllItemsAreNotNull(catalog.CaveFloorPurple);
            CollectionAssert.AllItemsAreNotNull(catalog.CaveWalls);
            CollectionAssert.AllItemsAreNotNull(catalog.CaveCrystals);
            Assert.IsNotNull(MapArt.Terrain("forest", Tile.Grass, 3, 15));
            Assert.IsNotNull(MapArt.Terrain("dark_mines", Tile.Grass, 3, 15));
            Assert.IsNotNull(MapArt.Terrain("underground", Tile.Cliff, 3, 15));
        }
    }
}
