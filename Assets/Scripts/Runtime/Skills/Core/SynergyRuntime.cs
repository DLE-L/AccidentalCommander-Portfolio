using Lizzo.PV.Legion;
using System;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.P0.Skills
{
    public static class SynergyRuntime
    {
        public static int Activate(PartyService party, string synergyId, Transform caster, string reason)
        {
            if (string.IsNullOrEmpty(synergyId))
                throw new ArgumentException("P0 synergy id is required.", nameof(synergyId));

            if (caster == null)
                throw new ArgumentNullException(nameof(caster));

            SynergyData synergyData = party.Data.GetSynergy(synergyId);
            if (synergyData == null)
                throw new InvalidOperationException($"Missing P0 synergy data: {synergyId}");

            if (string.IsNullOrEmpty(synergyData.SkillId))
                throw new InvalidOperationException($"P0 synergy '{synergyId}' is missing skillId.");

            SkillData skillData = party.Data.GetSkill(synergyData.SkillId);
            if (skillData == null)
                throw new InvalidOperationException($"Missing P0 skill data: {synergyData.SkillId}");

            SkillExecutionContext context = new SkillExecutionContext(
                party,
                synergyId,
                synergyData,
                skillData,
                caster,
                reason);
            return SkillRegistry.Execute(context);
        }
    }
}
