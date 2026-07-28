using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;

namespace Lizzo.PV.P0.Cards
{
    public readonly struct PassiveOfferContext
    {
        public PassiveOfferContext(float commanderHpRatio, float elapsedSeconds, bool guardActive)
        {
            CommanderHpRatio = commanderHpRatio;
            ElapsedSeconds = elapsedSeconds;
            GuardActive = guardActive;
        }

        public float CommanderHpRatio { get; }
        public float ElapsedSeconds { get; }
        public bool GuardActive { get; }

        public float GetMultiplier(string passiveId)
        {
            return passiveId switch
            {
                "passive_survival_instinct" when CommanderHpRatio <= .40f => 1.25f,
                "passive_supply_pouch" when ElapsedSeconds >= 300.0f => 1.10f,
                _ => 1.0f,
            };
        }
    }

    public readonly struct CanonicalPassiveCardCandidate
    {
        public CanonicalPassiveCardCandidate(CardKind cardKind, string passiveId, int nextLevel, float weight)
        { CardKind = cardKind; PassiveId = passiveId; NextLevel = nextLevel; Weight = weight; }
        public CardKind CardKind { get; }
        public string PassiveId { get; }
        public int NextLevel { get; }
        public float Weight { get; }
        public bool IsNew => NextLevel == 1;
    }

    public sealed class CanonicalPassiveCardService
    {
        static readonly string[] PassiveIds =
        {
            "passive_melee_training", "passive_frontline_tempo", "passive_ranged_training", "passive_projectile_speed",
            "passive_long_range", "passive_healing_prayer", "passive_swift_prayer", "passive_blue_shield_crest",
            "passive_hold_formation", "passive_battle_command", "passive_march_speed", "passive_command_radius",
            "passive_survival_instinct", "passive_old_flag", "passive_war_drum", "passive_supply_pouch",
        };

        readonly IDataProvider _data;
        readonly PartyService _party;
        readonly Func<PassiveOfferContext> _offerContext;
        public CanonicalPassiveCardService(IDataProvider data, PartyService party, PassiveRosterState roster)
            : this(data, party, roster, null)
        {
        }

        public CanonicalPassiveCardService(IDataProvider data, PartyService party, PassiveRosterState roster, Func<PassiveOfferContext> offerContext)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _party = party ?? throw new ArgumentNullException(nameof(party));
            Roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _offerContext = offerContext;
        }
        public PassiveRosterState Roster { get; }

        public static bool TryGetPassiveId(CardKind kind, out string passiveId)
        {
            int index = (int)kind - (int)CardKind.PassiveMeleeTraining;
            if (index < 0 || index >= PassiveIds.Length) { passiveId = null; return false; }
            passiveId = PassiveIds[index]; return true;
        }

        public bool TryGetCandidate(CardKind kind, out CanonicalPassiveCardCandidate candidate)
        {
            candidate = default;
            if (TryGetPassiveId(kind, out string passiveId) == false) return false;
            PassiveData data = _data.GetPassive(passiveId);
            if (Roster.CanApply(data, out PassiveRosterChangeResult change) == false) return false;
            int nextLevel = change == PassiveRosterChangeResult.New ? 1 : Roster.GetLevel(passiveId) + 1;
            float weight = ResolveWeight(data);
            if (weight <= 0.0f) return false;
            candidate = new CanonicalPassiveCardCandidate(kind, passiveId, nextLevel, weight);
            return true;
        }

        public void CollectEligibleCandidates(List<CanonicalPassiveCardCandidate> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            for (int i = 0; i < PassiveIds.Length; i++)
            {
                CardKind kind = (CardKind)((int)CardKind.PassiveMeleeTraining + i);
                if (TryGetCandidate(kind, out CanonicalPassiveCardCandidate candidate)) destination.Add(candidate);
            }
        }

        public bool TryApply(string passiveId, out PassiveRosterChangeResult result) => Roster.TryApply(_data.GetPassive(passiveId), out result);

        float ResolveWeight(PassiveData data)
        {
            if (data == null) return 0.0f;
            PassiveOfferContext context = _offerContext == null ? default : _offerContext();
            bool melee = HasFamily("melee_family") || HasFamily("sword_family");
            bool ranged = HasFamily("ranged_family");
            bool healing = HasFamily("healing_family");
            bool shield = HasFamily("shield_family");
            bool guard = context.GuardActive;
            float weight = data.Id switch
            {
                "passive_melee_training" => melee ? 1.5f : .4f,
                "passive_frontline_tempo" => melee ? 1.5f : 1.0f,
                "passive_ranged_training" or "passive_long_range" => ranged ? 1.5f : 1.0f,
                "passive_projectile_speed" => HasProjectileCompanion() ? 1.5f : 1.0f,
                "passive_healing_prayer" or "passive_swift_prayer" => healing ? 1.5f : 1.0f,
                "passive_blue_shield_crest" => (shield ? 1.5f : 1.0f) * (guard ? 1.25f : 1.0f),
                "passive_hold_formation" => guard ? 2.0f : 0.0f,
                "passive_old_flag" or "passive_war_drum" => .8f,
                _ => 1.0f,
            };
            return weight * context.GetMultiplier(data.Id);
        }

        bool HasFamily(string familyTag)
        {
            IReadOnlyList<Lizzo.PV.Legion.SquadSlotState> slots = _party.GetSquadSlotSnapshot();
            for (int i = 0; i < slots.Count; i++)
            {
                string baseUnitId = slots[i].BaseUnitId;
                if (string.IsNullOrWhiteSpace(baseUnitId)) continue;
                CompanionRosterData roster = _data.GetCompanionRoster(baseUnitId);
                if (roster != null && roster.FamilyTags.IndexOf(familyTag, StringComparison.Ordinal) >= 0) return true;
            }
            return false;
        }

        bool HasProjectileCompanion()
        {
            IReadOnlyList<Lizzo.PV.Legion.SquadSlotState> slots = _party.GetSquadSlotSnapshot();
            for (int i = 0; i < slots.Count; i++)
            {
                string baseUnitId = slots[i].BaseUnitId;
                if (string.IsNullOrWhiteSpace(baseUnitId)) continue;
                CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(baseUnitId);
                CombatEffectData effect = profile == null ? null : _data.GetCombatEffect(profile.BasicEffectId);
                if (effect != null && effect.DeliveryKind == CombatDeliveryKind.Projectile) return true;
            }
            return false;
        }
    }
}
