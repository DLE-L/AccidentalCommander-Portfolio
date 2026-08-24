using System.Collections.Generic;
using Lizzo.PV.P0.Config;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;
using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    internal sealed partial class CardOfferGenerationService
    {
        private const int MAX_COMPANION_PROGRESSION = 3;

        private bool TryAddCanonicalCompanionCard(List<CardKind> selectedKinds, CardKind[] excludedKinds, ref bool filtered)
        {
            if (_canonicalCompanionEligibility == null || selectedKinds.Count >= CardOfferPoolResolver.CardOptionCount)
                return false;

            _canonicalCompanionEligibility.CollectEligibleCandidates(_canonicalCompanionCandidates);
            float totalWeight = 0.0f;
            for (int i = 0; i < _canonicalCompanionCandidates.Count; i++)
            {
                CanonicalCompanionCardCandidate candidate = _canonicalCompanionCandidates[i];
                if (selectedKinds.Contains(candidate.CardKind) || ContainsKind(excludedKinds, candidate.CardKind))
                {
                    if (ContainsKind(excludedKinds, candidate.CardKind))
                        filtered = true;
                    continue;
                }

                totalWeight += candidate.Weight;
            }

            if (totalWeight <= 0.0f)
                return false;

            float roll = Random.value * totalWeight;
            for (int i = 0; i < _canonicalCompanionCandidates.Count; i++)
            {
                CanonicalCompanionCardCandidate candidate = _canonicalCompanionCandidates[i];
                if (selectedKinds.Contains(candidate.CardKind) || ContainsKind(excludedKinds, candidate.CardKind))
                    continue;

                roll -= candidate.Weight;
                if (roll > 0.0f)
                    continue;

                return TryAddCardKind(selectedKinds, candidate.CardKind, null, ref filtered);
            }

            return false;
        }

        private bool TryAddCanonicalPassiveCard(List<CardKind> selectedKinds, CardKind[] excludedKinds, ref bool filtered)
        {
            if (_canonicalPassiveCards == null || selectedKinds.Count >= CardOfferPoolResolver.CardOptionCount) return false;
            _canonicalPassiveCards.CollectEligibleCandidates(_canonicalPassiveCandidates);
            float totalWeight = 0.0f;
            for (int i = 0; i < _canonicalPassiveCandidates.Count; i++)
            {
                CanonicalPassiveCardCandidate candidate = _canonicalPassiveCandidates[i];
                if (selectedKinds.Contains(candidate.CardKind) || ContainsKind(excludedKinds, candidate.CardKind)) { if (ContainsKind(excludedKinds, candidate.CardKind)) filtered = true; continue; }
                totalWeight += candidate.Weight;
            }
            if (totalWeight <= 0.0f) return false;
            float roll = Random.value * totalWeight;
            for (int i = 0; i < _canonicalPassiveCandidates.Count; i++)
            {
                CanonicalPassiveCardCandidate candidate = _canonicalPassiveCandidates[i];
                if (selectedKinds.Contains(candidate.CardKind) || ContainsKind(excludedKinds, candidate.CardKind)) continue;
                roll -= candidate.Weight;
                if (roll > 0.0f) continue;
                return TryAddCardKind(selectedKinds, candidate.CardKind, null, ref filtered);
            }
            return false;
        }

        private CardKind[] ResolvePassiveBucket()
        {
            bool hasShield = Party.ShieldSoldierCount > 0
                || Party.ShieldCaptainCount > 0
                || Party.IsGuardSquadActivated;
            return hasShield ? CardOfferPoolResolver.PassiveBucketAfterShield : CardOfferPoolResolver.PassiveBucketDefault;
        }

        private bool CanCardAppear(CardKind kind)
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

        private void AddNonCompanionPressureCards(List<CardKind> pool)
        {
            CardKind[] fallbackKinds = CardOfferPoolResolver.FallbackKinds;
            for (int i = 0; i < fallbackKinds.Length; i++)
            {
                if (CanCardAppear(fallbackKinds[i]))
                    pool.Add(fallbackKinds[i]);
            }
        }

        private void AddPromotionPressureCards(List<CardKind> pool)
        {
            if (Party.PromotionReadyCount <= 0)
                return;

            int repeatCount = Mathf.Max(1, Mathf.RoundToInt(RemoteConfig.FullSlotPromotionWeight));
            for (int i = 0; i < repeatCount; i++)
            {
                if (CanCardAppear(CardKind.AddShieldSoldier))
                    pool.Add(CardKind.AddShieldSoldier);
            }
        }

        private void AddSynergyCompletionCards(List<CardKind> pool)
        {
            CardKind[] squadBucket = CardOfferPoolResolver.SquadBucket;
            for (int i = 0; i < squadBucket.Length; i++)
                AddSynergyCompletionCard(pool, squadBucket[i]);
        }

        private void AddSynergyCompletionCard(List<CardKind> pool, CardKind kind)
        {
            if (CardCompanionKindResolver.TryResolve(kind, out CompanionKind companionKind) == false)
                return;

            if (Party.WouldRecruitCompleteGuardSquad(companionKind) && CanCardAppear(kind))
                pool.Add(kind);
        }
    }
}
