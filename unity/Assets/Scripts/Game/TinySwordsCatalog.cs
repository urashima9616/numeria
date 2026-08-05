using UnityEngine;

namespace Numeria.Game
{
    /// <summary>由编辑器从本地地图素材包生成；运行时不依赖 AssetDatabase。</summary>
    public sealed class TinySwordsCatalog : ScriptableObject
    {
        public Sprite[] Terrain1;
        public Sprite[] Terrain2;
        public Sprite[] Terrain3;
        public Sprite[] Terrain4;
        public Sprite[] Terrain5;
        public Sprite Water;
        public Sprite WaterFoam;
        public Sprite Shadow;
        public Sprite Bridge;
        public Sprite[] Trees;
        public Sprite[] Bushes;
        public Sprite[] Rocks;
        public Sprite[] Clouds;
        public Sprite[] Landmarks;
        public Sprite[] Treasures;
        public Sprite TreasureOpened;
        public Sprite[] Portals;

        public Sprite[] CaveFloorDark;
        public Sprite[] CaveFloorPurple;
        public Sprite[] CavePaths;
        public Sprite[] CaveWalls;
        public Sprite[] CaveVoids;
        public Sprite[] CaveCrystals;
        public Sprite[] CaveRocks;
        public Sprite CaveBridge;
        public Sprite CaveLandmark;
        public Sprite CavePortal;
        public Sprite CaveGlow;

        // Tiles and Hexes: 2D Painted Terrain Samples. These are individual,
        // bottom-anchored square tiles; the source pack stays local while this
        // catalog serializes the references needed by runtime builds.
        public Sprite PaintedBase;
        public Sprite PaintedBelowDirt;
        public Sprite PaintedBelowWater;
        public Sprite PaintedDesert;
        public Sprite PaintedForest;
        public Sprite PaintedSnowForest;
        public Sprite PaintedJungle;
        public Sprite PaintedMountain;
        public Sprite PaintedOcean;
        public Sprite PaintedPlains;
        public Sprite PaintedCastle;
        public Sprite PaintedVolcano;
    }
}
