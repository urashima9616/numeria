using System;

namespace Numeria.Core
{
    /// <summary>
    /// 1–99 级统一成长与经验公式。所有公式只使用整数或最后一步取整，
    /// 保证同一存档在不同平台得到完全相同的结果。
    /// </summary>
    public static class GrowthSystem
    {
        public const int MaxLevel = 99;

        public static int ClampLevel(int level) => Math.Max(1, Math.Min(MaxLevel, level));

        /// <summary>每十级成长值以整数保存，例如 8 表示平均每级 +0.8。</summary>
        public static int StatAtLevel(int baseValue, int growthPerTenLevels, int level)
        {
            int steps = ClampLevel(level) - 1;
            return Math.Max(1, baseValue + (steps * growthPerTenLevels + 5) / 10);
        }

        /// <summary>
        /// 每级约需 8–12 场同级普通战；低等级升级更快，Lv.99 不再需要经验。
        /// </summary>
        public static int XpToNext(int level)
        {
            int clamped = ClampLevel(level);
            return clamped >= MaxLevel ? 0 : 10 + clamped * 6;
        }

        /// <summary>
        /// 经验 = 物种基础经验 × 等级系数 × 等级差系数 × Boss 系数。
        /// 等级差奖励限制在 0.35–2.2 倍，防止越级刷怪或回低级区刷经验失衡。
        /// </summary>
        public static int VictoryXp(int baseXp, int enemyLevel, int playerLevel, bool boss)
        {
            int foe = ClampLevel(enemyLevel);
            int hero = ClampLevel(playerLevel);
            double levelFactor = 1d + (foe - 1) * 0.055d;
            double differenceFactor = Math.Max(0.35d, Math.Min(2.2d, 1d + (foe - hero) * 0.12d));
            double bossFactor = boss ? 2.5d : 1d;
            return Math.Max(1, (int)Math.Round(baseXp * levelFactor * differenceFactor * bossFactor));
        }
    }
}
