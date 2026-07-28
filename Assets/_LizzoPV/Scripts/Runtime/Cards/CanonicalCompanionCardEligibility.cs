using System;
using System.Collections.Generic;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;

namespace Lizzo.PV.P0.Cards
{
    public readonly struct CanonicalCompanionCardCandidate
    {
        public CanonicalCompanionCardCandidate(CardKind cardKind, string baseUnitId, PartyRosterChangeResult change, float weight, bool isNewlyUnlocked)
        {
            CardKind = cardKind;
            BaseUnitId = baseUnitId;
            Change = change;
            Weight = weight;
            IsNewlyUnlocked = isNewlyUnlocked;
        }

        public CardKind CardKind { get; }
        public string BaseUnitId { get; }
        public PartyRosterChangeResult Change { get; }
        public float Weight { get; }
        public bool IsNewlyUnlocked { get; }
    }

    public sealed class CanonicalCompanionCardEligibility
    {
        const int NewCompanionSlotLimit = 7;
        const float NewUnlockBoost = 1.8f;
        const float ReinforceWeight = 1.4f;
        const float PromoteWeight = 3.0f;
        const float SlotFiveNewWeight = 0.75f;
        const float SlotSixNewWeight = 0.25f;
        const float SlotSixPromoteWeight = 1.25f;

        static readonly Definition[] Definitions =
        {
            new Definition(CardKind.AddShieldSoldier, "shield_guard"),
            new Definition(CardKind.RecruitSwordsman, "sword_soldier"),
            new Definition(CardKind.RecruitCleric, "cleric"),
            new Definition(CardKind.RecruitArcher, "falcon_archer"),
            new Definition(CardKind.RecruitFieldHerbalist, "field_herbalist"),
            new Definition(CardKind.RecruitBombardier, "bombardier"),
            new Definition(CardKind.RecruitFireMage, "fire_mage"),
            new Definition(CardKind.RecruitLightningMage, "lightning_mage"),
            new Definition(CardKind.RecruitWolfTamer, "wolf_tamer"),
            new Definition(CardKind.RecruitWraithKnight, "wraith_knight"),
            new Definition(CardKind.RecruitNecromancer, "necromancer"),
            new Definition(CardKind.RecruitSkeletonBomber, "skeleton_bomber"),
        };

        readonly ICanonicalCompanionRosterView _roster;
        readonly CompanionUnlockProgress _progress;

        public CanonicalCompanionCardEligibility(ICanonicalCompanionRosterView roster, CompanionUnlockProgress progress)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        public bool IsCanonicalCompanionCard(CardKind cardKind) => TryGetBaseUnitId(cardKind, out _);

        public bool TryGetBaseUnitId(CardKind cardKind, out string baseUnitId)
        {
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (Definitions[i].CardKind != cardKind)
                    continue;

                baseUnitId = Definitions[i].BaseUnitId;
                return true;
            }

            baseUnitId = null;
            return false;
        }

        public bool TryGetCandidate(CardKind cardKind, out CanonicalCompanionCardCandidate candidate)
        {
            if (TryGetBaseUnitId(cardKind, out string baseUnitId) == false || _progress.IsUnlocked(baseUnitId) == false)
            {
                candidate = default;
                return false;
            }

            PartyRosterChangeResult change = _roster.PreviewCanonicalRecruit(baseUnitId);
            int activeSlots = _roster.ActiveCompanionSlotCount;
            bool isNewlyUnlocked = _progress.IsNewUnlockBoostEligible(baseUnitId);
            float weight;
            switch (change)
            {
                case PartyRosterChangeResult.Recruit:
                    if (activeSlots >= NewCompanionSlotLimit)
                    {
                        candidate = default;
                        return false;
                    }

                    weight = activeSlots switch
                    {
                        5 => SlotFiveNewWeight,
                        6 => SlotSixNewWeight,
                        _ => 1.0f,
                    };
                    if (isNewlyUnlocked)
                        weight *= NewUnlockBoost;
                    break;
                case PartyRosterChangeResult.Reinforce:
                    weight = ReinforceWeight;
                    break;
                case PartyRosterChangeResult.Promote:
                    weight = PromoteWeight;
                    if (activeSlots >= 6)
                        weight *= SlotSixPromoteWeight;
                    break;
                default:
                    candidate = default;
                    return false;
            }

            candidate = new CanonicalCompanionCardCandidate(cardKind, baseUnitId, change, weight, isNewlyUnlocked);
            return true;
        }

        public void CollectEligibleCandidates(List<CanonicalCompanionCardCandidate> destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            destination.Clear();
            for (int i = 0; i < Definitions.Length; i++)
            {
                if (TryGetCandidate(Definitions[i].CardKind, out CanonicalCompanionCardCandidate candidate))
                    destination.Add(candidate);
            }
        }

        readonly struct Definition
        {
            public Definition(CardKind cardKind, string baseUnitId)
            {
                CardKind = cardKind;
                BaseUnitId = baseUnitId;
            }

            public CardKind CardKind { get; }
            public string BaseUnitId { get; }
        }
    }
}
