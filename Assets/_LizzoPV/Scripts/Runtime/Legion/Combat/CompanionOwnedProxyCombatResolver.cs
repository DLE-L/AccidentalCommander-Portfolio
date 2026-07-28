using System;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public readonly struct CompanionOwnedProxyCombatSetup
    {
        public readonly int Damage;
        public readonly float Range;
        public readonly int MaxTargets;
        public readonly int TriggerCount;

        public CompanionOwnedProxyCombatSetup(int damage, float range, int maxTargets, int triggerCount)
        {
            Damage = damage;
            Range = range;
            MaxTargets = maxTargets;
            TriggerCount = triggerCount;
        }

        public CompanionOwnedProxyCombatSetup WithTriggerCount(int triggerCount)
        {
            return new CompanionOwnedProxyCombatSetup(Damage, Range, MaxTargets, triggerCount);
        }
    }

    public sealed class CompanionOwnedProxyCombatResolver
    {
        private const string FalconArcherId = "falcon_archer";

        private readonly IDataProvider _data;

        public CompanionOwnedProxyCombatResolver(IDataProvider data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public bool TryResolve(string baseUnitId, out CompanionOwnedProxyCombatSetup setup)
        {
            if (baseUnitId != FalconArcherId)
            {
                setup = default;
                return false;
            }

            CompanionCombatProfileData profile = _data.GetCompanionCombatProfile(baseUnitId)
                ?? throw new InvalidOperationException("Falcon profile missing.");
            CombatEffectData effect = _data.GetCombatEffect(profile.SecondaryEffectId)
                ?? throw new InvalidOperationException("Falcon assist missing.");

            if (effect.OwnerUnitId != baseUnitId
                || effect.SkillId != profile.SecondarySkillId
                || effect.EffectKind != CombatEffectKind.Damage
                || effect.DeliveryKind != CombatDeliveryKind.Proxy
                || effect.TargetRule != CombatTargetRule.Nearest
                || effect.BaseValue <= 0.0f
                || effect.Range <= 0.0f
                || effect.MaxTargets != 1
                || effect.TriggerCount < 1)
            {
                throw new InvalidOperationException("Falcon assist data invalid.");
            }

            setup = new CompanionOwnedProxyCombatSetup(
                Mathf.RoundToInt(effect.BaseValue),
                effect.Range,
                effect.MaxTargets,
                effect.TriggerCount);
            return true;
        }
    }
}
