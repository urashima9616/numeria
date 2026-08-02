using NUnit.Framework;
using Numeria.Core;

namespace Numeria.Game.Tests
{
    public class SkyCityTests
    {
        [Test]
        public void MountainsPortal_LeadsToTierThreeSkyCity()
        {
            Assert.AreEqual("sky", Maps.Mountains().PortalTargetMap);
            var sky = Maps.Get("sky");
            Assert.AreEqual("Azure Sky City", sky.DisplayName);
            Assert.AreEqual(3, sky.Tier);
            Assert.AreEqual("sky", sky.Theme);
            Assert.AreEqual("mirrowl", sky.Wild().Id);
            Assert.AreEqual("symmetrix", sky.Boss().Id);
        }

        [Test]
        public void Mirrowl_IsCatchableAndCanBecomeActivePlayer()
        {
            Assert.True(GameData.Mirrowl().Catchable);
            Assert.False(GameData.Symmetrix().Catchable);

            var player = GameData.PlayerMon("mirrowl", false);
            Assert.AreEqual("Mirrowl", player.Name);
            Assert.AreEqual(2, player.Skills.Length);
            Assert.AreEqual("Mirror Pattern", player.Skills[1].Name);
        }

        [Test]
        public void SkyGate_UsesProgressGateCollection()
        {
            var progress = new Progress();
            var sky = Maps.Sky();
            Assert.False(sky.GateCleared(progress));
            sky.ClearGate(progress);
            sky.ClearGate(progress);
            Assert.True(sky.GateCleared(progress));
            Assert.AreEqual(1, progress.ClearedGates.FindAll(id => id == "sky").Count);
        }
    }
}
