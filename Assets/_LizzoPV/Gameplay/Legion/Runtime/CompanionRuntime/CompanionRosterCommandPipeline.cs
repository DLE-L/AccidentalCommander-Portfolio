using System;

namespace Lizzo.PV.Legion.RunCore
{
    internal static class CompanionRosterCommandValidator
    {
        internal static bool TryValidate(
            in CompanionRosterCommand command,
            long lastAcceptedSequence,
            out string normalizedCompanionId,
            out CompanionRosterRejection rejection)
        {
            normalizedCompanionId = null;
            if (command.Sequence <= lastAcceptedSequence)
            {
                rejection = CompanionRosterRejection.InvalidSequence;
                return false;
            }

            normalizedCompanionId = command.CompanionId?.Trim();
            if (string.IsNullOrEmpty(normalizedCompanionId))
            {
                rejection = CompanionRosterRejection.InvalidCompanionId;
                return false;
            }

            if (!Enum.IsDefined(typeof(CompanionRosterCommandKind), command.Kind))
            {
                rejection = CompanionRosterRejection.UnsupportedCommand;
                return false;
            }

            rejection = CompanionRosterRejection.None;
            return true;
        }
    }

    internal static class CompanionRecruitCommandExecutor
    {
        internal static bool TryExecute(
            RunCombatContext context,
            CompanionRosterModule roster,
            CompanionFormationModule formation,
            CompanionPoint commanderPosition,
            string companionId,
            out CompanionSquadModule squad,
            out CompanionRosterRejection rejection)
        {
            squad = null;
            if (!context.DefinitionCatalog.TryGetDefinition(companionId, out CompanionDefinition definition))
            {
                rejection = CompanionRosterRejection.DefinitionMissing;
                return false;
            }
            if (roster.ContainsCompanion(companionId))
            {
                rejection = CompanionRosterRejection.SquadAlreadyExists;
                return false;
            }
            if (roster.IsAtCapacity())
            {
                rejection = CompanionRosterRejection.CapacityReached;
                return false;
            }
            if (!CompanionSquadModule.TryCreate(companionId, definition, out squad))
            {
                rejection = CompanionRosterRejection.UnsupportedDefinition;
                return false;
            }

            roster.AddSquad(squad);
            formation.ReflowFormation(roster.Squads, commanderPosition);
            rejection = CompanionRosterRejection.None;
            return true;
        }
    }

    internal static class CompanionExistingSquadResolver
    {
        internal static bool TryResolve(
            CompanionRosterModule roster,
            string companionId,
            out CompanionSquadModule squad,
            out CompanionRosterRejection rejection)
        {
            if (!roster.TryGetSquadByCompanionId(companionId, out squad))
            {
                rejection = CompanionRosterRejection.SquadMissing;
                return false;
            }

            rejection = CompanionRosterRejection.None;
            return true;
        }
    }

    internal static class CompanionSquadMutationExecutor
    {
        internal static bool TryApply(
            CompanionRosterCommandKind kind,
            CompanionSquadModule squad,
            out CompanionRosterRejection rejection)
        {
            bool applied = kind switch
            {
                CompanionRosterCommandKind.Reinforce => squad.TryReinforce(),
                CompanionRosterCommandKind.Promote => squad.TryPromote(),
                _ => false,
            };
            rejection = applied
                ? CompanionRosterRejection.None
                : kind == CompanionRosterCommandKind.Reinforce || kind == CompanionRosterCommandKind.Promote
                    ? CompanionRosterRejection.InvalidRosterState
                    : CompanionRosterRejection.UnsupportedCommand;
            return applied;
        }
    }
}
