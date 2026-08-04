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
    }
}
