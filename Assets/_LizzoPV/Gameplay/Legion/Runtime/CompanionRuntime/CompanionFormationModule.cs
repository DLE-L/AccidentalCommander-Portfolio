using System.Collections.Generic;
using Lizzo.PV.Gameplay.Run;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionFormationModule
    {
        private const float GroupRadius = 0.85f;

        public void ReflowFormation(
            IReadOnlyList<CompanionSquadModule> squads,
            CompanionPoint commanderWorldPosition)
        {
            if (squads == null)
            {
                return;
            }

            int count = squads.Count;
            if (count <= 0 || count > FormationLayout.GroupCapacity)
            {
                return;
            }

            for (int index = 0; index < count; index += 1)
            {
                if (TryResolveAnchor(count, index, out CompanionPoint anchor))
                {
                    squads[index].AssignFormationAnchor(Add(commanderWorldPosition, anchor));
                    squads[index].AssignFormationDirection(anchor);
                }
            }
        }

        private static CompanionPoint Add(CompanionPoint left, CompanionPoint right)
        {
            return new CompanionPoint(left.X + right.X, left.Y + right.Y);
        }

        internal static bool TryResolveAnchor(int activeSquadCount, int activeSquadOrder, out CompanionPoint anchor)
        {
            anchor = CompanionPoint.Zero;
            if (activeSquadCount <= 0
                || activeSquadCount > FormationLayout.GroupCapacity
                || activeSquadOrder < 0
                || activeSquadOrder >= activeSquadCount)
            {
                return false;
            }

            RunPoint point = FormationLayout.ResolveGroupCenter(
                activeSquadCount,
                activeSquadOrder,
                GroupRadius);
            anchor = new CompanionPoint(point.X, point.Y);
            return true;
        }
    }
}
