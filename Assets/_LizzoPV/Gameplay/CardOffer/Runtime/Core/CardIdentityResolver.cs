namespace Lizzo.PV.P0.Cards
{
    internal sealed class CardIdentityResolver
    {
        internal void Resolve(
            CardData card,
            out string canonicalBaseUnitId,
            out string canonicalPassiveId)
        {
            canonicalBaseUnitId = card.CanonicalBaseUnitId;
            canonicalPassiveId = card.CanonicalPassiveId;
        }
    }
}
