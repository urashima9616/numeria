namespace Numeria.Core
{
    /// <summary>
    /// 确定性 LCG,与 Web 原型 puzzles.js 的 makeRng 完全一致,
    /// 保证谜题生成可测试、可回放。
    /// </summary>
    public class Rng
    {
        private uint _s;

        public Rng(uint seed) => _s = seed;

        /// <summary>返回 [0, 1) 的伪随机数。</summary>
        public double Next()
        {
            unchecked { _s = _s * 1664525u + 1013904223u; }
            return _s / 4294967296.0;
        }

        /// <summary>闭区间 [lo, hi] 随机整数。</summary>
        public int Pick(int lo, int hi) => lo + (int)(Next() * (hi - lo + 1));
    }
}
