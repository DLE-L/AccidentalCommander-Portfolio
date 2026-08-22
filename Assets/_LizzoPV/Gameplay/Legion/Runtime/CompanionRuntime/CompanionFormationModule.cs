using System.Collections.Generic;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionFormationModule
    {
        private const float FormationSpacingMultiplier = 1.25f;

        private static readonly CompanionPoint[][] FormationAnchors =
        {
            new CompanionPoint[0],
            new[] { Point(0.0f, 0.55f) },
            new[]
            {
                Point(0.0f, 0.55f),
                Point(0.0f, -0.55f)
            },
            new[]
            {
                Point(0.0f, 0.58f),
                Point(-0.92f, 0.0f),
                Point(0.92f, 0.0f)
            },
            new[]
            {
                Point(0.0f, 0.62f),
                Point(-0.95f, 0.0f),
                Point(0.95f, 0.0f),
                Point(0.0f, -0.62f)
            },
            new[]
            {
                Point(0.0f, 0.62f),
                Point(-0.98f, 0.0f),
                Point(0.98f, 0.0f),
                Point(-0.62f, -0.62f),
                Point(0.62f, -0.62f)
            },
            new[]
            {
                Point(-0.65f, 0.52f),
                Point(0.65f, 0.52f),
                Point(-1.02f, 0.0f),
                Point(1.02f, 0.0f),
                Point(-0.65f, -0.65f),
                Point(0.65f, -0.65f)
            },
            new[]
            {
                Point(0.0f, 0.68f),
                Point(-0.78f, 0.34f),
                Point(0.78f, 0.34f),
                Point(-1.08f, -0.08f),
                Point(1.08f, -0.08f),
                Point(-0.68f, -0.68f),
                Point(0.68f, -0.68f)
            }
        };

        private static CompanionPoint Point(float x, float y)
        {
            return new CompanionPoint(
                x * FormationSpacingMultiplier,
                y * FormationSpacingMultiplier);
        }

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
