using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Combat;
using System;
using System.Collections.Generic;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Legion.Summons
{
    public readonly struct PersonalSummonTarget
    {
        public readonly ICombatImmediateHitTarget Target;
        public readonly Vector3 Position;
        public readonly int InstanceId;

        public PersonalSummonTarget(ICombatImmediateHitTarget target, Vector3 position, int instanceId)
        {
            Target = target;
            Position = position;
            InstanceId = instanceId;
        }
    }

    public interface ICompanionPersonalSummonTargetSource
    {
        void CollectTargets(List<PersonalSummonTarget> destination);
    }

    public sealed class RegistryPersonalSummonTargetSource : ICompanionPersonalSummonTargetSource
    {
        private readonly RuntimeObjectRegistry _registry;

        public RegistryPersonalSummonTargetSource(RuntimeObjectRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void CollectTargets(List<PersonalSummonTarget> destination)
        {
            foreach (EnemyActor monster in _registry.Enemies)
            {
                if (monster == null || monster.IsValid() == false)
                    continue;

                Collider2D collider = monster.CombatCollider;
                Vector3 point = collider == null || collider.enabled == false
                    ? monster.transform.position
                    : collider.ClosestPoint(monster.transform.position);
                destination.Add(new PersonalSummonTarget(monster, point, monster.GetInstanceID()));
            }
        }
    }
}
