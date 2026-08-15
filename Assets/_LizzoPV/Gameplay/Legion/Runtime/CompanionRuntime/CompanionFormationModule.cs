using System.Collections.Generic;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionFormationModule
    {
        private static readonly CompanionPoint[][] FormationAnchors =
        {
            new CompanionPoint[0],
            new[] { new CompanionPoint(0.0f, 1.25f) },
            new[]
            {
                new CompanionPoint(-0.65f, 1.20f),
                new CompanionPoint(0.65f, 1.20f)
            },
            new[]
            {
                new CompanionPoint(0.0f, 1.30f),
                new CompanionPoint(-0.75f, 0.65f),
                new CompanionPoint(0.75f, 0.65f)
            },
            new[]
            {
                new CompanionPoint(-0.65f, 1.25f),
                new CompanionPoint(0.65f, 1.25f),
                new CompanionPoint(-0.85f, 0.45f),
                new CompanionPoint(0.85f, 0.45f)
            },
            new[]
            {
                new CompanionPoint(0.0f, 1.35f),
                new CompanionPoint(-0.75f, 0.95f),
                new CompanionPoint(0.75f, 0.95f),
                new CompanionPoint(-0.95f, 0.30f),
                new CompanionPoint(0.95f, 0.30f)
            },
            new[]
            {
                new CompanionPoint(-0.55f, 1.40f),
                new CompanionPoint(0.55f, 1.40f),
                new CompanionPoint(-0.85f, 0.75f),
                new CompanionPoint(0.85f, 0.75f),
                new CompanionPoint(-1.05f, 0.15f),
                new CompanionPoint(1.05f, 0.15f)
            },
            new[]
            {
                new CompanionPoint(0.0f, 1.50f),
                new CompanionPoint(-0.65f, 1.05f),
                new CompanionPoint(0.65f, 1.05f),
                new CompanionPoint(-0.95f, 0.55f),
                new CompanionPoint(0.95f, 0.55f),
                new CompanionPoint(-1.15f, 0.0f),
                new CompanionPoint(1.15f, 0.0f)
            }
        };

        public void ReflowFormation(IReadOnlyList<CompanionSquadModule> squads)
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

            CompanionPoint[] anchors = FormationAnchors[count];
            for (int index = 0; index < count; index += 1)
            {
                squads[index].AssignFormationAnchor(anchors[index]);
            }
        }
    }
}
