using Lizzo.PV.P0.Config;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;

namespace Lizzo.PV.P0.Cards
{
    public static partial class FixedCardPool
    {
        private const int MAX_COMPANION_PROGRESSION = 3;

        private static bool CanCardAppear(CardKind kind)
        {
            if (IsCardEnabled(kind) == false)
                return false;

            if (_canonicalPassiveCards != null && _canonicalPassiveCards.TryGetCandidate(kind, out _))
                return true;
            if (_canonicalPassiveCards != null && CanonicalPassiveCardService.TryGetPassiveId(kind, out _))
                return false;

            // P10B canonical passive cards own run progression; legacy effect cards must not
            // bypass that state while the canonical service is bound for a run.
            if (_canonicalPassiveCards != null && CardEffectRuntime.IsPassiveCard(kind))
                return false;

            if (CardEffectRuntime.IsPassiveCard(kind)
                && CardEffectRuntime.CanAcquirePassive(kind) == false)
            {
                return false;
            }

            if (_canonicalCompanionEligibility != null
                && _canonicalCompanionEligibility.IsCanonicalCompanionCard(kind))
            {
                return _canonicalCompanionEligibility.TryGetCandidate(kind, out _);
            }

            if (TryGetCompanionKind(kind, out CompanionKind companionKind) == false)
                return true;

            if (IsCompanionAtMaxProgression(companionKind))
                return false;

            if (RemoteConfig.FullSlotNewCompanionBlock == false)
                return true;

            if (Party.IsCompanionSlotFull == false)
                return true;

            return Party.CanRecruitWithinSlotCap(companionKind);
        }

        private static bool IsCompanionAtMaxProgression(CompanionKind kind)
        {
            return kind switch
            {
                CompanionKind.ShieldSoldier => Party.ShieldCaptainCount > 0
                    || Party.ShieldSoldierCount >= MAX_COMPANION_PROGRESSION,
                CompanionKind.Swordsman => Party.SwordsmanCount >= MAX_COMPANION_PROGRESSION,
                CompanionKind.Cleric => Party.ClericCount >= MAX_COMPANION_PROGRESSION,
                CompanionKind.Archer => Party.ArcherCount >= MAX_COMPANION_PROGRESSION,
                _ => false,
            };
        }

        private static CardHighlight ResolveRuntimeHighlight(CardKind kind)
        {
            if (_canonicalCompanionEligibility != null
                && _canonicalCompanionEligibility.TryGetCandidate(kind, out CanonicalCompanionCardCandidate canonicalCandidate))
            {
                return canonicalCandidate.Change switch
                {
                    PartyRosterChangeResult.Promote => CardHighlight.PromotionReady,
                    PartyRosterChangeResult.Recruit => CardHighlight.New,
                    _ => CardHighlight.None,
                };
            }

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
            if (_canonicalCompanionEligibility != null
                && _canonicalCompanionEligibility.TryGetCandidate(kind, out CanonicalCompanionCardCandidate canonicalCandidate))
            {
                return canonicalCandidate.Change == PartyRosterChangeResult.Recruit;
            }

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
