using NUnit.Framework;
using Numeria.Core;

namespace Numeria.Core.Tests
{
    public class GridMapTests
    {
        private static readonly string[] Rows =
        {
            "TTTTT",
            "TS.bT",
            "T.TCT",
            "T..PT",
            "TTTTT",
        };

        [Test]
        public void Parse_DimensionsSpawnAndTiles()
        {
            var m = GridMap.Parse(Rows);
            Assert.AreEqual(5, m.Width);
            Assert.AreEqual(5, m.Height);
            Assert.AreEqual((1, 1), m.Spawn);
            Assert.AreEqual(Tile.Grass, m.At(1, 1)); // S 是草地
            Assert.AreEqual(Tile.Bush, m.At(3, 1));
            Assert.AreEqual(Tile.Chest, m.At(3, 2));
            Assert.AreEqual(Tile.Portal, m.At(3, 3));
            Assert.AreEqual(Tile.Tree, m.At(0, 0));
        }

        [Test]
        public void Walkable_TreesAndOutOfBoundsBlocked()
        {
            var m = GridMap.Parse(Rows);
            Assert.True(m.Walkable(1, 1));
            Assert.True(m.Walkable(3, 3));  // 传送门可走
            Assert.False(m.Walkable(0, 0)); // 树
            Assert.False(m.Walkable(2, 2)); // 中间的树
            Assert.False(m.Walkable(-1, 0));
            Assert.False(m.Walkable(5, 5));
        }

        [Test]
        public void FindPath_RoutesAroundObstacles()
        {
            var m = GridMap.Parse(Rows);
            var path = m.FindPath((1, 1), (3, 3));
            Assert.That(path.Count, Is.GreaterThanOrEqualTo(4)); // 绕过 (2,2) 的树
            Assert.AreEqual((3, 3), path[path.Count - 1]);
            // 路径连续且每步可走
            var prev = (1, 1);
            foreach (var step in path)
            {
                Assert.True(m.Walkable(step.x, step.y));
                int manhattan = System.Math.Abs(step.x - prev.Item1) + System.Math.Abs(step.y - prev.Item2);
                Assert.AreEqual(1, manhattan);
                prev = step;
            }
        }

        [Test]
        public void FindPath_UnreachableOrSelf_ReturnsEmpty()
        {
            var m = GridMap.Parse(Rows);
            Assert.IsEmpty(m.FindPath((1, 1), (1, 1)));
            Assert.IsEmpty(m.FindPath((1, 1), (0, 0))); // 目标是树
        }
    }
}
