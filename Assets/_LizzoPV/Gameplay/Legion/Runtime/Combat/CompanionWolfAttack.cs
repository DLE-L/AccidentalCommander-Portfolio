using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal sealed class CompanionWolfAttack
    {
        private readonly RuntimeObjectRegistry _registry;
        private readonly ICombatProjectileModule _projectiles;
        private readonly ICombatImmediateHitModule _hits;
        private readonly List<PackBite> _pack = new List<PackBite>(6);
        private int _generation;
        private long _nextId;
        private float _time;
        internal event Action<long, Vector3, Vector3> PackShown;
        internal event Action<long> PackHidden;
        internal event Action<Vector3> Launched;

        internal CompanionWolfAttack(RuntimeObjectRegistry registry, ICombatProjectileModule projectiles, ICombatImmediateHitModule hits)
        { _registry = registry; _projectiles = projectiles; _hits = hits; }

        internal bool Launch(CombatEffectData effect, Vector3 origin, int damage, CountableKillAttribution attribution, CompanionPassiveCombatModifiers modifiers)
        {
            var target = Select(origin, effect.Range * modifiers.RangeMultiplier, null);
            if (target == null) return false;
            var flight = new Flight(this, effect, damage, attribution, modifiers,
                Mathf.Max(1, effect.TriggerCount + modifiers.ChainTargetBonus), new HashSet<int>(), target);
            return Spawn(flight, origin);
        }

        private bool Spawn(Flight flight, Vector3 origin)
        {
            bool spawned = _projectiles.TrySpawn(CombatProjectileRequest.CreateHoming(
                "wolf_tamer", null, origin, flight.Target, flight.Damage,
                flight.Effect.MotionSpeed * flight.Modifiers.ProjectileSpeedMultiplier,
                flight.Effect.ProjectileLifetime, .08f, AttackVisualKind.SingleHit,
                killAttribution: flight.Attribution, presentationId: flight.Effect.Id, homingPayload: flight));
            if (spawned) { try { Launched?.Invoke(origin); } catch (Exception e) { Debug.LogException(e); } }
            return spawned;
        }

        private EnemyActor Select(Vector3 origin, float range, HashSet<int> excluded)
        {
            EnemyActor selected = null;
            foreach (var enemy in _registry.Enemies)
            {
                if (enemy == null || !enemy.IsValid() || (excluded != null && excluded.Contains(enemy.GetInstanceID()))
                    || (enemy.transform.position - origin).sqrMagnitude > range * range) continue;
                if (selected == null || enemy.Hp < selected.Hp || (enemy.Hp == selected.Hp && enemy.GetInstanceID() < selected.GetInstanceID())) selected = enemy;
            }
            return selected;
        }

        internal bool QueuePack(CombatEffectData effect, EnemyActor target, int damage, CountableKillAttribution attribution)
        {
            if (target == null || !target.IsValid()) return false;
            int version = _generation;
            for (int i = 0; i < effect.MaxActiveCount; i++)
            {
                float angle = i * Mathf.PI * 2f / effect.MaxActiveCount;
                Vector3 position = target.transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * effect.Radius;
                var bite = new PackBite(++_nextId, target, position, _time + effect.Duration + i * effect.RepeatInterval, damage, attribution, effect.Id);
                _pack.Add(bite);
                try { PackShown?.Invoke(bite.Id, position, target.transform.position); } catch (Exception e) { Debug.LogException(e); }
                if (version != _generation) break;
            }
            return true;
        }

        internal void Advance(float delta)
        {
            _time += Mathf.Max(0f, delta);
            for (int i = 0; i < _pack.Count;)
            {
                var bite = _pack[i];
                if (bite.At > _time) { i++; continue; }
                _pack.RemoveAt(i);
                int version = _generation;
                if (bite.Target != null && bite.Target.IsValid() && bite.Target.SpawnSequence == bite.SpawnSequence)
                    _hits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget("beast_commander_pack_assault", bite.Target,
                        bite.Position, bite.Target.transform.position, bite.Damage, AttackVisualKind.SingleHit, false, bite.Attribution, bite.EffectId));
                Hide(bite.Id);
                if (version != _generation) return;
            }
        }

        internal void Reset()
        {
            _generation++;
            for (int i = _pack.Count - 1; i >= 0; i--) Hide(_pack[i].Id);
            _pack.Clear();
            _time = 0;
        }

        private void Hide(long id) { try { PackHidden?.Invoke(id); } catch (Exception e) { Debug.LogException(e); } }

        private readonly struct PackBite
        {
            internal readonly long Id, SpawnSequence;
            internal readonly EnemyActor Target;
            internal readonly Vector3 Position;
            internal readonly float At;
            internal readonly int Damage;
            internal readonly CountableKillAttribution Attribution;
            internal readonly string EffectId;
            internal PackBite(long id, EnemyActor target, Vector3 position, float at, int damage, CountableKillAttribution attribution, string effectId)
            { Id = id; Target = target; SpawnSequence = target.SpawnSequence; Position = position; At = at; Damage = damage; Attribution = attribution; EffectId = effectId; }
        }

        private sealed class Flight : ICombatHomingPayload
        {
            private readonly CompanionWolfAttack _owner;
            private readonly int _generation, _remaining;
            private readonly HashSet<int> _visited;
            private long _targetSpawn;
            internal EnemyActor Target;
            internal readonly CombatEffectData Effect;
            internal readonly int Damage;
            internal readonly CountableKillAttribution Attribution;
            internal readonly CompanionPassiveCombatModifiers Modifiers;
            internal Flight(CompanionWolfAttack owner, CombatEffectData effect, int damage, CountableKillAttribution attribution,
                CompanionPassiveCombatModifiers modifiers, int remaining, HashSet<int> visited, EnemyActor target)
            {
                _owner = owner; _generation = owner._generation; Effect = effect; Damage = damage;
                Attribution = attribution; Modifiers = modifiers; _remaining = remaining; _visited = visited;
                Target = target; _targetSpawn = target.SpawnSequence;
            }
            public EnemyActor ResolveTarget(Vector3 position)
            {
                if (_generation != _owner._generation) return null;
                if (Target != null && Target.IsValid() && Target.SpawnSequence == _targetSpawn) return Target;
                Target = _owner.Select(position, Effect.Radius, _visited);
                _targetSpawn = Target == null ? 0 : Target.SpawnSequence;
                return Target;
            }
            public void ApplyHit(EnemyActor target, Vector3 position)
            {
                if (_generation != _owner._generation || target == null || !target.IsValid()) return;
                _visited.Add(target.GetInstanceID());
                _owner._hits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget("wolf_tamer", target, position, position,
                    Damage, AttackVisualKind.SingleHit, false, Attribution, Effect.Id, executionThreshold: Modifiers.ExecutionThreshold));
                if (_generation != _owner._generation || _remaining <= 1 || (target.IsValid() && target.Hp > 0)) return;
                var next = _owner.Select(position, Effect.Radius, _visited);
                if (next != null) _owner.Spawn(new Flight(_owner, Effect, Damage, Attribution, Modifiers, _remaining - 1, _visited, next), position);
            }
        }
    }
}
