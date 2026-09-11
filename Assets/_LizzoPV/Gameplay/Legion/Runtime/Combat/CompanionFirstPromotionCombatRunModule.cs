using System;
using Lizzo.PV.Legion.RunCore;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed class CompanionFirstPromotionCombatRunModule : IDisposable, ICompanionConditionSource
    {
        private const float ShieldPushDuration = 0.16f;

        private readonly CompanionPromotionCombatContext _combatContext;
        private readonly ICombatProjectileModule _projectiles;
        private readonly ICombatImmediateHitModule _immediateHits;
        private readonly CanonicalCompanionCastStream _casts;
        private readonly CompanionFirstPromotionCombatSetup _setup;
        private readonly CompanionPromotionTriggerState _triggers;
        private readonly bool _swordUsesMemberTurn;
        private readonly CompanionSanctuaryRuntimeState _sanctuary = new CompanionSanctuaryRuntimeState();
        private readonly List<CompanionPromotionTargetCandidate> _targets = new List<CompanionPromotionTargetCandidate>(32);
        private readonly List<CompanionPromotionTargetCandidate> _shieldTargets = new List<CompanionPromotionTargetCandidate>(8);

        private readonly Func<string, CompanionPassiveCombatModifiers> _resolveModifiers;
        private readonly CompanionCombatEvents _events;
        private readonly string _shieldEffectId;
        private readonly float _shieldCloseRadius;
        private readonly Dictionary<string, string> _countedBasicEffects = new Dictionary<string, string>();
        private readonly string _falconEffectId;
        public event Action<Vector3> SwordWaveLaunched;
        public event Action<Vector3, float> SanctuaryStarted;
        public event Action SanctuaryEnded;
        private bool _sanctuaryPublished;
        private float _shieldTime;
        public event Action<string, CompanionConditionProgress> Changed;
        private bool _shieldWasActive;
        private float _nextShieldDueTime;
        private bool _disposed;

        internal CompanionFirstPromotionCombatRunModule(
            Lizzo.PV.Data.IDataProvider data,
            CompanionPromotionCombatContext combatContext,
            ICombatProjectileModule projectiles,
            ICombatImmediateHitModule immediateHits,
            CanonicalCompanionCastStream casts,
            Func<string, CompanionPassiveCombatModifiers> resolveModifiers = null,
            CompanionCombatEvents events = null)
        {
            _falconEffectId = data.GetCompanionRoster("falcon_archer").PromotionEffectRef;
            _swordUsesMemberTurn = data.GetCompanionCombatProfile("sword_soldier").PromotionOnMemberTurn;
            _resolveModifiers = resolveModifiers;
            _events = events;
            _shieldEffectId = data.GetCompanionRoster("shield_guard").PromotionEffectRef;
            _shieldCloseRadius = data.GetCombatEffect(data.GetCompanionRoster("shield_guard").EffectRef).CloseDamageRadius;
            _combatContext = combatContext ?? throw new ArgumentNullException(nameof(combatContext));
            _projectiles = projectiles ?? throw new ArgumentNullException(nameof(projectiles));
            _immediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _casts = casts ?? throw new ArgumentNullException(nameof(casts));
            if (new CompanionFirstPromotionCombatResolver(data).TryResolve(out _setup) == false)
                throw new InvalidOperationException("First promotion combat data is missing.");

            // During the lineage migration, member-turn loadouts do not also receive legacy counter attacks.
            var bindings = new List<CompanionPromotionTriggerBinding>();
            foreach (var binding in _setup.CreateTriggers())
                if (!data.GetCompanionCombatProfile(binding.BaseUnitId).PromotionOnMemberTurn) bindings.Add(binding);
            foreach (var binding in bindings)
                if (binding.ActionKind == CanonicalCompanionActionKind.BasicAttack)
                    _countedBasicEffects[binding.BaseUnitId] = data.GetCompanionCombatProfile(binding.BaseUnitId).BasicEffectId;
            _triggers = new CompanionPromotionTriggerState(bindings);
            _triggers.Changed += OnTriggerProgress;
            _casts.Completed += OnCanonicalCastCompleted;
        }

        public int PendingSwordCount => _swordUsesMemberTurn ? 0 : _triggers.GetPendingCount(_setup.Sword.SourceId);
        public int PendingLightCount => _triggers.GetPendingCount(_setup.Light.SourceId);
        public int PendingFalconCount => _triggers.GetPendingCount(_setup.Falcon.SourceId);
        public bool IsSanctuaryActive(float currentTime) => !_disposed && _sanctuary.IsActive(currentTime);
        public float GetSanctuaryAttackIntervalDivisor(Vector3 position, float currentTime)
            => _disposed ? 1.0f : _sanctuary.GetAttackIntervalDivisor(position, currentTime);

        public void Tick(float currentTime)
        {
            if (_disposed)
                return;

            if (_sanctuaryPublished && !_sanctuary.IsActive(currentTime)) EndSanctuary();
            TickShieldCaptain(currentTime);
            if (PendingSwordCount > 0 && TryResolveSwordCaptain())
                _triggers.ConsumePending(_setup.Sword.SourceId);
            if (PendingLightCount > 0 && TryResolveLightGuide(currentTime))
                _triggers.ConsumePending(_setup.Light.SourceId);
            if (PendingFalconCount > 0 && TryResolveFalconCaptain())
                _triggers.ConsumePending(_setup.Falcon.SourceId);
        }

        public void Reset()
        {
            _triggers.Reset();
            EndSanctuary();
            _targets.Clear();
            _shieldTargets.Clear();
            _shieldWasActive = false;
            _nextShieldDueTime = 0.0f;
            _shieldTime = 0f;
            NotifyShieldProgress();
            _combatContext.ReleaseProjectilesBySourceId(_setup.Sword.SourceId);
            _combatContext.ReleaseProjectilesBySourceId(_setup.Falcon.SourceId);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _casts.Completed -= OnCanonicalCastCompleted;
            Reset();
            _triggers.Changed -= OnTriggerProgress;
            Changed = null;
            SwordWaveLaunched = null;
            SanctuaryStarted = null;
            SanctuaryEnded = null;
        }

        private void OnCanonicalCastCompleted(CanonicalCompanionCastCompleted completed)
        {
            if (TryFindPromotedRepresentative(completed.BaseUnitId, out _) == false)
                return;

            // Count the authored basic action of every squad member; follow-up deliveries emit no extra cast.
            var kind = _countedBasicEffects.TryGetValue(completed.BaseUnitId, out var basicEffect) && completed.AttackId == basicEffect
                ? CanonicalCompanionActionKind.BasicAttack : completed.ActionKind;
            _triggers.Record(completed.BaseUnitId, kind);
        }

        public bool TryGetProgress(string companionId, out CompanionConditionProgress progress)
        {
            if (companionId != "shield_guard") return _triggers.TryGetProgress(companionId, out progress);
            float current = _shieldWasActive
                ? Mathf.Clamp(_setup.Shield.Cooldown - (_nextShieldDueTime - _shieldTime), 0f, _setup.Shield.Cooldown) : 0f;
            progress = new CompanionConditionProgress(current, _setup.Shield.Cooldown,
                _shieldWasActive && _shieldTime >= _nextShieldDueTime ? 1 : 0);
            return true;
        }

        private void OnTriggerProgress(string id, CompanionConditionProgress progress)
        {
            try { Changed?.Invoke(id, progress); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private void NotifyShieldProgress()
        {
            TryGetProgress("shield_guard", out var progress);
            try { Changed?.Invoke("shield_guard", progress); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private void TickShieldCaptain(float currentTime)
        {
            _shieldTime = currentTime;
            if (!TryFindPromotedRepresentative("shield_guard", out _))
            {
                if (_shieldWasActive)
                {
                    _shieldWasActive = false;
                    _nextShieldDueTime = 0f;
                    NotifyShieldProgress();
                }
                return;
            }
            if (!_shieldWasActive)
            {
                _shieldWasActive = true;
                _nextShieldDueTime = currentTime + _setup.Shield.Cooldown;
            }
            else if (currentTime >= _nextShieldDueTime && ResolveShieldCaptain())
                _nextShieldDueTime = currentTime + _setup.Shield.Cooldown;
            NotifyShieldProgress();
        }

        private bool ResolveShieldCaptain()
        {
            CommanderActor commander = _combatContext.Player;
            if (commander == null)
                return false;

            Vector3 origin = commander.transform.position;
            CollectTargets();
            _shieldTargets.Clear();
            var modifiers = _resolveModifiers?.Invoke("shield_guard") ?? CompanionPassiveCombatModifiers.Identity;
            float radius = _setup.Shield.Radius * modifiers.AreaRadiusMultiplier;
            float radiusSquared = radius * radius;
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

                _shieldTargets.Insert(insertion, candidate);
            }

            bool applied = false;
            for (int index = 0; index < _shieldTargets.Count; index++)
            {
                CompanionPromotionTargetCandidate candidate = _shieldTargets[index];
                EnemyActor target = candidate.Target;
                if (target == null || target.IsValid() == false)
                    continue;

                if (_immediateHits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                        _setup.Shield.SourceId,
                        target,
                        origin,
                        candidate.Point,
                        Mathf.Max(1, Mathf.RoundToInt(_setup.Shield.Damage * modifiers.DamageMultiplier * modifiers.PromotedDamageMultiplier
                            * ((candidate.Point - origin).sqrMagnitude <= _shieldCloseRadius * _shieldCloseRadius
                                ? modifiers.CloseDamageMultiplier : 1f))),
                        AttackVisualKind.AreaHit,
                        false)))
                {
                    applied = true;
                    if (!candidate.IsBoss)
                        target.ApplySmoothKnockback(candidate.Point - origin,
                            _setup.Shield.PushDistance * modifiers.ForcedMovementMultiplier, ShieldPushDuration);
                }
            }
            if (applied)
                _events?.PublishEffect(_shieldEffectId, string.Empty, origin, origin, Vector3.up, 0f, radius, 2);
            return applied;
        }

        private bool TryResolveSwordCaptain()
        {
            var modifiers = _resolveModifiers?.Invoke("sword_soldier") ?? CompanionPassiveCombatModifiers.Identity;
            if (TryFindPromotedRepresentative("sword_soldier", out CompanionCombatRepresentative representative) == false
                || TryFindNearestTarget(representative.Transform.position, _setup.Sword.Range * modifiers.RangeMultiplier, out CompanionPromotionTargetCandidate target) == false)
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
                Mathf.Max(1, Mathf.RoundToInt(_setup.Sword.Damage * modifiers.DamageMultiplier * modifiers.PromotedDamageMultiplier)),
                _setup.Sword.ProjectileSpeed * modifiers.ProjectileSpeedMultiplier,
                _setup.Sword.ProjectileLifetime,
                RetroVfxKind.None,
                killAttribution: new CountableKillAttribution(representative.OwnerInstanceId, _setup.Sword.SourceId, CombatKillSourceCategory.CompanionOwnedAction),
                maxDistinctTargetHits: _setup.Sword.MaxTargets,
                attackCollisionSize: _setup.Sword.Width * modifiers.AreaRadiusMultiplier,
                presentationId: CombatProjectilePresentationIds.SwordCaptainWave);
            bool spawned = _projectiles.TrySpawn(request);
            if (spawned)
            {
                try { SwordWaveLaunched?.Invoke(origin); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
            return spawned;
        }

        private bool TryResolveLightGuide(float currentTime)
        {
            if (TryFindPromotedRepresentative("cleric", out _) == false || _combatContext.Player == null)
                return false;

            EndSanctuary();
            Vector3 center = _combatContext.Player.transform.position;
            _sanctuary.Begin(center, currentTime, _setup.Light);
            _sanctuaryPublished = true;
            try { SanctuaryStarted?.Invoke(center, _setup.Light.Radius); }
            catch (Exception exception) { Debug.LogException(exception); }
            return true;
        }

        private void EndSanctuary()
        {
            _sanctuary.Reset();
            if (!_sanctuaryPublished) return;
            _sanctuaryPublished = false;
            try { SanctuaryEnded?.Invoke(); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private bool TryResolveFalconCaptain()
        {
            if (!TryFindPromotedRepresentative("falcon_archer", out var representative)) return false;
            var modifiers = _resolveModifiers?.Invoke("falcon_archer") ?? CompanionPassiveCombatModifiers.Identity;
            Vector3 origin = representative.Transform.position;
            float range = _setup.Falcon.Range * modifiers.RangeMultiplier;
            CollectTargets();
            for (int i = _targets.Count - 1; i >= 0; i--)
                if ((_targets[i].Point - origin).sqrMagnitude > range * range) _targets.RemoveAt(i);
            if (!CompanionFirstPromotionTargetSelector.TrySelectFalconDive(_targets, out var selected)
                || selected.Target == null || !selected.Target.IsValid()) return false;
            var request = CombatProjectileRequest.CreateHoming(_setup.Falcon.SourceId, null,
                origin + Vector3.up * .28f, selected.Target,
                Mathf.Max(1, Mathf.RoundToInt(_setup.Falcon.Damage * modifiers.DamageMultiplier * modifiers.PromotedDamageMultiplier)),
                _setup.Falcon.Speed, _setup.Falcon.Lifetime, .12f, AttackVisualKind.SingleHit,
                killAttribution: new CountableKillAttribution(representative.OwnerInstanceId, _setup.Falcon.SourceId, CombatKillSourceCategory.CompanionOwnedAction),
                presentationId: _falconEffectId);
            bool launched = _projectiles.TrySpawn(request);
            if (launched) _events?.PublishEffect(_falconEffectId, string.Empty, origin, selected.Point,
                selected.Point - origin, range, 0f, 2);
            return launched;
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
