using Lizzo.PV.P0.Config;
using Lizzo.PV.Legion;

namespace Lizzo.PV.P0.Cards
{
    public static partial class FixedCardPool
    {
        private static bool CanCardAppear(CardKind kind)
        {
            if (IsCardEnabled(kind) == false)
                return false;

            if (TryGetCompanionKind(kind, out CompanionKind companionKind) == false)
                return true;

            if (RemoteConfig.FullSlotNewCompanionBlock == false)
                return true;

            if (Party.IsCompanionSlotFull == false)
                return true;

            return Party.WouldRecruitCompressSlot(companionKind);
        }

        private static CardHighlight ResolveRuntimeHighlight(CardKind kind)
        {
            if (TryGetCompanionKind(kind, out CompanionKind companionKind))
            {
                if (Party.WouldRecruitCompressSlot(companionKind))
                    return CardHighlight.PromotionReady;

                if (Party.WouldRecruitCompleteGuardSquad(companionKind))
                    return CardHighlight.SynergyOneMore;

                if (IsNewCompanionCard(kind))
                    return CardHighlight.New;
            }

            return CardHighlight.None;
        }

        private static bool IsNewCompanionCard(CardKind kind)
        {
            if (TryGetCompanionKind(kind, out CompanionKind companionKind) == false)
                return false;

            return companionKind switch
            {
                CompanionKind.ShieldSoldier => Party.ShieldSoldierCount <= 0 && Party.ShieldCaptainCount <= 0,
                CompanionKind.Swordsman => Party.SwordsmanCount <= 0,
                CompanionKind.Cleric => Party.ClericCount <= 0,
                CompanionKind.Archer => Party.ArcherCount <= 0,
                _ => false,
            };
        }

        private static bool TryGetCompanionKind(CardKind kind, out CompanionKind companionKind)
        {
            if (CardCatalogProvider.TryGetDefinition(kind, out CardDefinitionSet.Entry entry) && entry.HasCompanionKind)
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

        private static bool IsCardEnabled(CardKind kind)
        {
            return CardCatalogProvider.TryGetDefinition(kind, out CardDefinitionSet.Entry entry) == false || entry.Enabled;
        }
    }
}
