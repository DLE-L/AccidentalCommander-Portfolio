using System.Collections.Generic;
using Lizzo.PV.P0.Config;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;

namespace Lizzo.PV.P0.Cards
{
    internal sealed partial class CardOfferGenerationService
    {
        private const int MAX_COMPANION_PROGRESSION = 3;

        private bool IsFirstRecruitOffer()
        {
            return _session.LevelUpCount == 1 && _canonicalCompanionEligibility != null;
        }

        private bool IsFirstRecruitCard(CardKind kind)
        {
            return _canonicalCompanionEligibility != null
                && _canonicalCompanionEligibility.IsCanonicalCompanionCard(kind);
        }

        private void KeepFirstRecruitCandidates(List<WeightedGrowthCandidate> candidates)
        {
            for (int index = candidates.Count - 1; index >= 0; index--)
                if (IsFirstRecruitCard(candidates[index].Kind) == false)
                    candidates.RemoveAt(index);
        }

        private bool CanCardAppear(CardKind kind)
        {
            if (_enforceCurrentProductCardPolicy
                && CardOfferPoolResolver.IsCurrentProductCardAvailable(kind) == false)
            {
                return false;
            }

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
                if (CardCatalogProvider.TryGetPool(out CardPoolDefinition pool)
                    && pool.IsCompanionCardAllowed(kind) == false)
                {
                    return false;
                }

                return _canonicalCompanionEligibility.TryGetCandidate(kind, out _);
            }

            if (CardCompanionKindResolver.TryResolve(kind, out CompanionKind companionKind) == false)
                return true;

            if (IsCompanionAtMaxProgression(companionKind))
                return false;

            if (RemoteConfig.FullSlotNewCompanionBlock == false)
                return true;

            if (Party.IsCompanionSlotFull == false)
                return true;

            return Party.CanRecruitWithinSlotCap(companionKind);
        }

        private bool IsCompanionAtMaxProgression(CompanionKind kind)
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

        private CardHighlight ResolveRuntimeHighlight(CardKind kind)
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

            if (CardCompanionKindResolver.TryResolve(kind, out CompanionKind companionKind))
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

        private bool IsNewCompanionCard(CardKind kind)
        {
            if (_canonicalCompanionEligibility != null
                && _canonicalCompanionEligibility.TryGetCandidate(kind, out CanonicalCompanionCardCandidate canonicalCandidate))
            {
                return canonicalCandidate.Change == PartyRosterChangeResult.Recruit;
            }

            if (CardCompanionKindResolver.TryResolve(kind, out CompanionKind companionKind) == false)
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

        private bool IsCardEnabled(CardKind kind)
        {
            return CardCatalogProvider.TryGetDefinition(kind, out CardDefinitionSet.Entry entry) == false || entry.Enabled;
        }

    }
}
