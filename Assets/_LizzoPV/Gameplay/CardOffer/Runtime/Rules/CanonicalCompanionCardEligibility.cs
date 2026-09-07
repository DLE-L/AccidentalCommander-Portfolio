using System;
using System.Collections.Generic;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;

namespace Lizzo.PV.Gameplay.CardOffer
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
        const float DefaultWeight = 1.0f;

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
            new Definition(CardKind.RecruitSkeletonScytheThrower, "skeleton_scythe_thrower"),
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
            switch (change)
            {
                case PartyRosterChangeResult.Recruit:
                    if (activeSlots >= NewCompanionSlotLimit)
                    {
                        candidate = default;
                        return false;
                    }

                    break;
                case PartyRosterChangeResult.Reinforce:
                    break;
                case PartyRosterChangeResult.Promote:
                    break;
                default:
                    candidate = default;
                    return false;
            }

            candidate = new CanonicalCompanionCardCandidate(cardKind, baseUnitId, change, DefaultWeight, isNewlyUnlocked);
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
