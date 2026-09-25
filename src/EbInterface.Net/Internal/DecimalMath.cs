namespace EbInterface.Internal
{
    internal static class DecimalMath
    {
        /// <summary>Anzahl der Nachkommastellen ohne abschließende Nullen (1.500 → 1).</summary>
        internal static int DecimalPlaces(decimal value)
        {
            // Division durch 1.000…0 entfernt abschließende Nullen aus der internen Darstellung.
            decimal normalized = value / 1.0000000000000000000000000000m;
            return (decimal.GetBits(normalized)[3] >> 16) & 0xFF;
        }
    }
}
