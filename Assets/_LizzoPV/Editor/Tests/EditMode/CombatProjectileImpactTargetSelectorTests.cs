using System.Collections.Generic;
using Lizzo.PV.Combat.Projectiles;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CombatProjectileImpactTargetSelectorTests
    {
        [Test]
        public void SelectsInclusiveValidUniqueTargets_ByDistanceThenSpawnSequence_CappedAtEight()
        {
            var objects = new List<GameObject>();
            try
            {
                var selector = new CombatProjectileImpactTargetSelector(8);
                Vector3 impactPoint = Vector3.zero;
                MonsterController firstTie = CreateTarget(objects, "FirstTie");
                MonsterController secondTie = CreateTarget(objects, "SecondTie");
                MonsterController boundary = CreateTarget(objects, "Boundary");

                selector.Begin(impactPoint, 1.8f, 8);
                selector.Consider(new CombatProjectileImpactTargetCandidate(boundary, new Vector3(1.8f, 0.0f), 30, true));
                Assert.That(selector.Contains(boundary), Is.True, "The radius boundary must be inclusive.");

                selector.Begin(impactPoint, 1.8f, 8);
                selector.Consider(new CombatProjectileImpactTargetCandidate(secondTie, new Vector3(1.0f, 0.0f), 20, true));
                selector.Consider(new CombatProjectileImpactTargetCandidate(firstTie, new Vector3(-1.0f, 0.0f), 10, true));
                selector.Consider(new CombatProjectileImpactTargetCandidate(boundary, new Vector3(1.8f, 0.0f), 30, true));
                selector.Consider(new CombatProjectileImpactTargetCandidate(boundary, new Vector3(0.2f, 0.0f), 30, true));
                selector.Consider(new CombatProjectileImpactTargetCandidate(CreateTarget(objects, "Invalid"), Vector3.zero, 1, false));
                selector.Consider(new CombatProjectileImpactTargetCandidate(CreateTarget(objects, "Outside"), new Vector3(1.81f, 0.0f), 2, true));
                for (int i = 0; i < 7; i++)
                {
                    selector.Consider(new CombatProjectileImpactTargetCandidate(
                        CreateTarget(objects, $"Fill{i}"),
                        new Vector3(1.1f + i * 0.05f, 0.0f),
                        40 + i,
                        true));
                }

                Assert.That(selector.Count, Is.EqualTo(8));
                Assert.That(selector.GetTarget(0), Is.SameAs(firstTie));
                Assert.That(selector.GetTarget(1), Is.SameAs(secondTie));
                Assert.That(selector.Contains(boundary), Is.False, "Boundary is inclusive but loses the deterministic top-eight cap.");
            }
            finally
            {
                for (int i = objects.Count - 1; i >= 0; i--)
                    Object.DestroyImmediate(objects[i]);
            }
        }

        private static MonsterController CreateTarget(ICollection<GameObject> objects, string name)
        {
            var gameObject = new GameObject(name);
            objects.Add(gameObject);
            return gameObject.AddComponent<MonsterController>();
        }
    }
}
