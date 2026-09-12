using Numeria.Core;

namespace Numeria.Game
{
    /// <summary>A visual record of the actual puzzle; never used to calculate battle damage.</summary>
    public sealed class SpellTrace
    {
        public int A, B, Total;
        public char Operation = '+';
        public PatternToken[] Pattern;
        public string Equation => $"{A} {Operation} {B} = {Total}";
    }
}
