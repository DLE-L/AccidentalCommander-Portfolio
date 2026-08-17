using System.Collections.Generic;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionFormationModule
    {
        private static readonly CompanionPoint[][] FormationAnchors =
        {
            new CompanionPoint[0],
            new[] { new CompanionPoint(0.0f, 0.55f) },
            new[]
            {
                new CompanionPoint(0.0f, 0.55f),
                new CompanionPoint(0.0f, -0.55f)
            },
            new[]
            {
                new CompanionPoint(0.0f, 0.58f),
                new CompanionPoint(-0.92f, 0.0f),
                new CompanionPoint(0.92f, 0.0f)
            },
            new[]
            {
                new CompanionPoint(0.0f, 0.62f),
                new CompanionPoint(-0.95f, 0.0f),
                new CompanionPoint(0.95f, 0.0f),
                new CompanionPoint(0.0f, -0.62f)
            },
            new[]
            {
                new CompanionPoint(0.0f, 0.62f),
                new CompanionPoint(-0.98f, 0.0f),
                new CompanionPoint(0.98f, 0.0f),
                new CompanionPoint(-0.62f, -0.62f),
                new CompanionPoint(0.62f, -0.62f)
            },
            new[]
            {
                new CompanionPoint(-0.65f, 0.52f),
                new CompanionPoint(0.65f, 0.52f),
                new CompanionPoint(-1.02f, 0.0f),
                new CompanionPoint(1.02f, 0.0f),
                new CompanionPoint(-0.65f, -0.65f),
                new CompanionPoint(0.65f, -0.65f)
            },
            new[]
            {
                new CompanionPoint(0.0f, 0.68f),
                new CompanionPoint(-0.78f, 0.34f),
                new CompanionPoint(0.78f, 0.34f),
                new CompanionPoint(-1.08f, -0.08f),
                new CompanionPoint(1.08f, -0.08f),
                new CompanionPoint(-0.68f, -0.68f),
                new CompanionPoint(0.68f, -0.68f)
            }
        };

        public void ReflowFormation(
            IReadOnlyList<CompanionSquadModule> squads,
            CompanionPoint commanderWorldPosition)
        {
            if (squads == null)
            {
                return;
            }

            int count = squads.Count;
            if (count <= 0 || count >= FormationAnchors.Length)
            {
                return;
            }

            for (int index = 0; index < count; index += 1)
            {
                if (TryResolveAnchor(count, index, out CompanionPoint anchor))
                {
                    squads[index].AssignFormationAnchor(Add(commanderWorldPosition, anchor));
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
                || activeSquadCount >= FormationAnchors.Length
                || activeSquadOrder < 0
                || activeSquadOrder >= activeSquadCount)
            {
                return false;
            }

            anchor = FormationAnchors[activeSquadCount][activeSquadOrder];
            return true;
        }
    }
}
