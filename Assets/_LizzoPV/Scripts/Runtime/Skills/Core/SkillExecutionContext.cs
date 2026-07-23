using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.P0.Skills
{
    public sealed class SkillExecutionContext
    {
        public SkillExecutionContext(
            PartyService party,
            string synergyId,
            SynergyData synergyData,
            SkillData skillData,
            Transform caster,
            string reason)
        {
            Party = party ?? throw new System.ArgumentNullException(nameof(party));
            SynergyId = synergyId;
            SynergyData = synergyData;
            SkillData = skillData;
            Caster = caster;
            Reason = reason;
        }

        public PartyService Party { get; }
        public string SynergyId { get; }
        public SynergyData SynergyData { get; }
        public SkillData SkillData { get; }
        public Transform Caster { get; }
        public string Reason { get; }
    }
}
