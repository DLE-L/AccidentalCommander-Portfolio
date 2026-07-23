using System;
using Lizzo.PV.P0.Skills.Guard;

namespace Lizzo.PV.P0.Skills
{
    public static class SkillRegistry
    {
        public const string RadialShieldPush = "radial_shield_push";


        public static int Execute(SkillExecutionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.SkillData == null)
                throw new InvalidOperationException("P0 skill execution context is missing SkillData.");

            string skillKind = context.SkillData.SkillKind;
            if (string.IsNullOrEmpty(skillKind))
                throw new InvalidOperationException($"P0 skill '{context.SkillData.Id}' is missing skillKind.");

            switch (skillKind)
            {
                case RadialShieldPush:
                    return ExecuteGuardSquad(context);
                default:
                    throw new InvalidOperationException($"Unknown P0 skillKind '{skillKind}' for skill '{context.SkillData.Id}'.");
            }
        }
        private static int ExecuteGuardSquad(SkillExecutionContext context)
        {
            GuardSquadSkillBehaviour.EnsureActive(
                context.Party,
                context.Caster,
                context.Reason,
                context.SynergyData,
                context.SkillData);
            return GuardSquadSkillBehaviour.ActiveCastId;
        }

    }
}
