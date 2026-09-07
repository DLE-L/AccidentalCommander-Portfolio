using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed class CompanionFirstPromotionCombatRunModule : IDisposable
    {
        private const float ShieldPushDuration = 0.16f;

        private readonly CompanionPromotionCombatContext _combatContext;
        private readonly ICombatProjectileModule _projectiles;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly CanonicalCompanionCastStream _casts;
        private readonly CompanionFirstPromotionCombatSetup _setup;
        private readonly CompanionFirstPromotionTriggerState _triggers;
        private readonly CompanionSanctuaryRuntimeState _sanctuary = new CompanionSanctuaryRuntimeState();
        private readonly List<CompanionPromotionTargetCandidate> _targets = new List<CompanionPromotionTargetCandidate>(32);
        private readonly List<CompanionPromotionTargetCandidate> _shieldTargets = new List<CompanionPromotionTargetCandidate>(8);

        private bool _shieldWasActive;
        private float _nextShieldDueTime;
        private int _pendingSword;
        private int _pendingLight;
        private int _pendingFalcon;
        private bool _disposed;

        internal CompanionFirstPromotionCombatRunModule(
            Lizzo.PV.Data.IDataProvider data,
            CompanionPromotionCombatContext combatContext,
            ICombatProjectileModule projectiles,
            ICombatImmediateHitModule immediateHits,
            CanonicalCompanionCastStream casts)
        {
            _combatContext = combatContext ?? throw new ArgumentNullException(nameof(combatContext));
            _projectiles = projectiles ?? throw new ArgumentNullException(nameof(projectiles));
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _casts = casts ?? throw new ArgumentNullException(nameof(casts));
            if (new CompanionFirstPromotionCombatResolver(data).TryResolve(out _setup) == false)
                throw new InvalidOperationException("First promotion combat data is missing.");

            _triggers = new CompanionFirstPromotionTriggerState(
                _setup.Sword.TriggerCount,
                _setup.Light.TriggerCount,
                _setup.Falcon.TriggerCount);
            _casts.Completed += OnCanonicalCastCompleted;
        }

        public int PendingSwordCount => _pendingSword;
        public int PendingLightCount => _pendingLight;
        public int PendingFalconCount => _pendingFalcon;

        public void Tick(float currentTime)
        {
            if (_disposed)
                return;

            TickShieldCaptain(currentTime);
            if (_pendingSword > 0 && TryResolveSwordCaptain())
                _pendingSword--;
            if (_pendingLight > 0 && TryResolveLightGuide(currentTime))
                _pendingLight--;
            if (_pendingFalcon > 0 && TryResolveFalconCaptain())
                _pendingFalcon--;
        }

        public void Reset()
        {
            _triggers.Reset();
            _sanctuary.Reset();
            _targets.Clear();
            _shieldTargets.Clear();
            _shieldWasActive = false;
            _nextShieldDueTime = 0.0f;
            _pendingSword = 0;
            _pendingLight = 0;
            _pendingFalcon = 0;
            _combatContext.ReleaseProjectilesBySourceId(_setup.Sword.SourceId);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _casts.Completed -= OnCanonicalCastCompleted;
            Reset();
        }

        private void OnCanonicalCastCompleted(CanonicalCompanionCastCompleted completed)
        {
            if (TryFindPromotedRepresentative(completed.BaseUnitId, out _) == false)
                return;

            int triggered = _triggers.Record(completed.BaseUnitId, completed.ActionKind);
            if (triggered <= 0)
                return;

            if (completed.BaseUnitId == "sword_soldier")
                _pendingSword += triggered;
            else if (completed.BaseUnitId == "cleric")
                _pendingLight += triggered;
            else if (completed.BaseUnitId == "falcon_archer")
                _pendingFalcon += triggered;
        }

        private void TickShieldCaptain(float currentTime)
        {
            if (TryFindPromotedRepresentative("shield_guard", out _) == false)
            {
                _shieldWasActive = false;
                _nextShieldDueTime = 0.0f;
                return;
            }

            if (_shieldWasActive == false)
            {
                _shieldWasActive = true;
                _nextShieldDueTime = currentTime + _setup.Shield.Cooldown;
                return;
            }

            if (currentTime < _nextShieldDueTime)
                return;

            ResolveShieldCaptain();
            _nextShieldDueTime = currentTime + _setup.Shield.Cooldown;
        }

        private void ResolveShieldCaptain()
        {
            PlayerController commander = _combatContext.Player;
            if (commander == null)
                return;

            Vector3 origin = commander.transform.position;
            CollectTargets();
            _shieldTargets.Clear();
            float radiusSquared = _setup.Shield.Radius * _setup.Shield.Radius;
            for (int index = 0; index < _targets.Count; index++)
            {
                CompanionPromotionTargetCandidate candidate = _targets[index];
                float distanceSquared = (candidate.Point - origin).sqrMagnitude;
                if (distanceSquared > radiusSquared)
                    continue;

                int insertion = 0;
                while (insertion < _shieldTargets.Count)
                {
                    CompanionPromotionTargetCandidate current = _shieldTargets[insertion];
                    float currentDistance = (current.Point - origin).sqrMagnitude;
                    if (distanceSquared < currentDistance
                        || (Mathf.Approximately(distanceSquared, currentDistance) && candidate.InstanceId < current.InstanceId))
                        break;
                    insertion++;
                }

                if (insertion >= _setup.Shield.MaxTargets)
                    continue;
                _shieldTargets.Insert(insertion, candidate);
                if (_shieldTargets.Count > _setup.Shield.MaxTargets)
                    _shieldTargets.RemoveAt(_setup.Shield.MaxTargets);
            }

            for (int index = 0; index < _shieldTargets.Count; index++)
            {
                CompanionPromotionTargetCandidate candidate = _shieldTargets[index];
                MonsterController target = candidate.Target;
                if (target == null || target.IsValid() == false)
                    continue;

                if (_immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                        _setup.Shield.SourceId,
                        target,
                        origin,
                        candidate.Point,
                        _setup.Shield.Damage,
                        AttackVisualKind.AreaHit,
                        false))
                    && candidate.IsBoss == false
                    && candidate.IsElite == false)
                {
                    target.ApplySmoothKnockback(candidate.Point - origin, _setup.Shield.PushDistance, ShieldPushDuration);
                }
            }
        }

        private bool TryResolveSwordCaptain()
        {
            if (TryFindPromotedRepresentative("sword_soldier", out CompanionCombatRepresentative representative) == false
                || TryFindNearestTarget(representative.Transform.position, _setup.Sword.Range, out CompanionPromotionTargetCandidate target) == false)
                return false;

            Vector3 origin = representative.Transform.position + Vector3.up * 0.28f;
            Vector3 direction = target.Point - origin;
            if (direction.sqrMagnitude <= 0.0001f)
                return false;

            CombatProjectileRequest request = CombatProjectileRequest.CreateStraight(
                _setup.Sword.SourceId,
                null,
                origin,
                direction.normalized,
                _setup.Sword.Damage,
                _setup.Sword.ProjectileSpeed,
                _setup.Sword.ProjectileLifetime,
                RetroVfxKind.None,
                killAttribution: new CountableKillAttribution(representative.OwnerInstanceId, _setup.Sword.SourceId, CombatKillSourceCategory.CompanionOwnedAction),
                maxDistinctTargetHits: _setup.Sword.MaxTargets,
                attackCollisionSize: _setup.Sword.Width,
                presentationId: CombatProjectilePresentationIds.SwordCaptainWave);
            return _projectiles.TrySpawn(request);
        }

        private bool TryResolveLightGuide(float currentTime)
        {
            if (TryFindPromotedRepresentative("cleric", out _) == false || _combatContext.Player == null)
                return false;

            _sanctuary.Begin(_combatContext.Player.transform.position, currentTime, _setup.Light);
            return true;
        }

        private bool TryResolveFalconCaptain()
        {
            if (TryFindPromotedRepresentative("falcon_archer", out CompanionCombatRepresentative representative) == false)
                return false;

            CollectTargets();
            if (CompanionFirstPromotionTargetSelector.TrySelectFalconDive(_targets, out CompanionPromotionTargetCandidate selected) == false
                || selected.Target == null
                || selected.Target.IsValid() == false)
                return false;

            return _immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                _setup.Falcon.SourceId,
                selected.Target,
                representative.Transform.position,
                selected.Point,
                _setup.Falcon.Damage,
                AttackVisualKind.SingleHit,
                false,
                new CountableKillAttribution(representative.OwnerInstanceId, _setup.Falcon.SourceId, CombatKillSourceCategory.CompanionOwnedAction)));
        }

        private void CollectTargets()
        {
            _combatContext.CollectPromotionTargets(_targets);
        }

        private bool TryFindNearestTarget(Vector3 origin, float range, out CompanionPromotionTargetCandidate selected)
        {
            CollectTargets();
            selected = default;
            bool found = false;
            float bestDistance = range * range;
            for (int index = 0; index < _targets.Count; index++)
            {
                CompanionPromotionTargetCandidate candidate = _targets[index];
                float distance = (candidate.Point - origin).sqrMagnitude;
                if (distance > bestDistance
                    || (found && Mathf.Approximately(distance, bestDistance) && candidate.InstanceId >= selected.InstanceId))
                    continue;
                selected = candidate;
                bestDistance = distance;
                found = true;
            }
            return found;
        }

        private bool TryFindPromotedRepresentative(string baseUnitId, out CompanionCombatRepresentative result)
        {
            return _combatContext.TryGetPromotedRepresentative(baseUnitId, 0, out result);
        }
    }
}
