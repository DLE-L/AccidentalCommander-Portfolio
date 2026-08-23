using Lizzo.PV.Legion;

namespace Lizzo.PV.P0.Cards
{
    internal static class CardCompanionKindResolver
    {
        internal static bool TryResolve(CardKind kind, out CompanionKind companionKind)
        {
            if (CardCatalogProvider.TryGetDefinition(kind, out CardDefinitionSet.Entry entry)
                && entry.HasCompanionKind)
            {
                companionKind = entry.CompanionKind;
                return true;
            }

            switch (kind)
            {
                case CardKind.AddShieldSoldier:
                    companionKind = CompanionKind.ShieldSoldier;
                    return true;
                case CardKind.RecruitArcher:
                    companionKind = CompanionKind.Archer;
                    return true;
                case CardKind.RecruitSwordsman:
                    companionKind = CompanionKind.Swordsman;
                    return true;
                case CardKind.RecruitCleric:
                    companionKind = CompanionKind.Cleric;
                    return true;
                default:
                    companionKind = default;
                    return false;
            }
        }
    }
}
