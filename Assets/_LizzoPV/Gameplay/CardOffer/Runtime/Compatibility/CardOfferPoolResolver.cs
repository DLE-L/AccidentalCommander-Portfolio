namespace Lizzo.PV.P0.Cards
{
    internal static class CardOfferPoolResolver
    {
        internal const int CardOptionCount = 3;

        internal static bool IsCurrentProductCardAvailable(CardKind kind)
        {
            int serializedKind = (int)kind;
            return serializedKind != 1
                && serializedKind != 2
                && serializedKind != 5
                && serializedKind != 7
                && serializedKind != 9
                && serializedKind != 34;
        }
    }
}
