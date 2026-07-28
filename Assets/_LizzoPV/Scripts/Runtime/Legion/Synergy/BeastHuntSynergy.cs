using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    /// <summary>Run-owned P10D Beast Hunt executor. P10C2 remains the sole cadence owner.</summary>
    public sealed class BeastHuntSynergy : IDisposable
    {
        const string DamageId = "DMG_SYNERGY_BEAST_01";
        const string BleedId = "DOT_SYNERGY_BEAST_01";
        const float DashCap = 1.0f;
        const float DashAndReturnDuration = 0.6f;

        readonly SynergyActivationState _activations;
        readonly SynergyTriggerState _triggers;
        readonly PartyService _party;
        readonly RuntimeObjectRegistry _registry;
        readonly ICombatImmediateHitModule _hits;
        readonly SafeKnockbackWorld _safeWorld;
        readonly SynergyDamageData _damage;
        readonly SynergyDamageData _bleed;
        readonly List<CompanionRuntime> _representatives = new(7);
        readonly List<BeastDash> _pending = new(7);
        readonly List<BeastBleed> _bleeds = new(4);
        bool _disposed;

        public BeastHuntSynergy(IDataProvider data, SynergyActivationState activations, SynergyTriggerState triggers, PartyService party, RuntimeObjectRegistry registry, ICombatImmediateHitModule hits, SafeKnockbackWorld safeWorld)
        {
            _activations = activations ?? throw new ArgumentNullException(nameof(activations));
            _triggers = triggers ?? throw new ArgumentNullException(nameof(triggers));
            _party = party ?? throw new ArgumentNullException(nameof(party));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _hits = hits ?? throw new ArgumentNullException(nameof(hits));
            _safeWorld = safeWorld;
            _damage = data?.GetSynergyDamage(DamageId) ?? throw new InvalidOperationException("Canonical Beast Hunt damage data is missing.");
            _bleed = data.GetSynergyDamage(BleedId) ?? throw new InvalidOperationException("Canonical Beast Hunt bleed data is missing.");
        }

        public int PendingHitCount => _pending.Count;
        public int ActiveBleedCount => _bleeds.Count;

        public bool TryResolvePending(float now)
        {
            if (_triggers.TryConsumePending(SynergyActivationIds.BeastHunt, out _) == false)
                return false;

            _party.CollectLivingBeastRepresentatives(_representatives);
            if (_representatives.Count < 2 || TrySelectCommonTarget(out MonsterController target) == false)
                return true;

            for (int index = 0; index < _representatives.Count; index++)
                BeginDash(_representatives[index], target, now);
            return true;
        }

        public void Tick(float now)
        {
            for (int index = _pending.Count - 1; index >= 0; index--)
            {
                BeastDash dash = _pending[index];
                if (dash.Advance(now, _hits, _damage, out MonsterController bleedTarget))
                    _pending.RemoveAt(index);
                if (bleedTarget != null) ApplyBleed(bleedTarget, now);
            }

            for (int index = _bleeds.Count - 1; index >= 0; index--)
            {
                BeastBleed bleed = _bleeds[index];
                if (bleed.Advance(now, _hits, _bleed, _damage.SynergyId))
                    _bleeds.RemoveAt(index);
            }
        }

        public void Reset()
        {
            for (int index = 0; index < _pending.Count; index++) _pending[index].Cancel();
            _pending.Clear();
            _bleeds.Clear();
            _representatives.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Reset();
        }

        bool TrySelectCommonTarget(out MonsterController selected)
        {
            selected = null;
            float bestCommanderDistance = float.MaxValue;
            long bestSequence = long.MaxValue;
            int bestClass = int.MaxValue;
            Vector3 commander = _registry.Player == null ? Vector3.zero : _registry.Player.transform.position;
            foreach (MonsterController candidate in _registry.Enemies)
            {
                if (candidate == null || candidate.IsValid() == false || candidate.Hp <= 0 || candidate.SpawnSequence <= 0L)
                    continue;
                if (IsInEveryRepresentativeRange(candidate.transform.position) == false)
                    continue;
                int targetClass = candidate.IsBoss ? 0 : candidate.IsElite ? 1 : 2;
                float commanderDistance = (candidate.transform.position - commander).sqrMagnitude;
                if (targetClass < bestClass || (targetClass == bestClass &&
                    (commanderDistance < bestCommanderDistance || (Mathf.Approximately(commanderDistance, bestCommanderDistance) && candidate.SpawnSequence < bestSequence))))
                {
                    selected = candidate;
                    bestClass = targetClass;
                    bestCommanderDistance = commanderDistance;
                    bestSequence = candidate.SpawnSequence;
                }
            }
            return selected != null;
        }

        bool IsInEveryRepresentativeRange(Vector3 point)
        {
            for (int index = 0; index < _representatives.Count; index++)
            {
                CompanionRuntime representative = _representatives[index];
                AllyCombat combat = representative == null ? null : representative.GetComponent<AllyCombat>();
                if (representative == null || representative.IsDown || combat == null || (representative.transform.position - point).sqrMagnitude > combat.AttackRange * combat.AttackRange)
                    return false;
            }
            return true;
        }

        void BeginDash(CompanionRuntime representative, MonsterController target, float now)
        {
            AllyFollower follower = representative == null ? null : representative.GetComponent<AllyFollower>();
            if (follower == null || representative.BodyCollider == null) return;
            Vector3 origin = representative.transform.position;
            Vector3 towardTarget = target.transform.position - origin;
            float distance = towardTarget.magnitude;
            Vector2 requested = distance <= 0.0001f ? Vector2.zero : (Vector2)(towardTarget / distance * Mathf.Min(DashCap, distance));
            Vector3 dashPoint = origin + (Vector3)(_safeWorld == null ? requested : _safeWorld.ResolveDisplacement(representative.BodyCollider, requested));
            follower.SetSynergyExternalMovement(true);
            _pending.Add(new BeastDash(representative, follower, target, origin, dashPoint, now, DashAndReturnDuration));
        }

        void ApplyBleed(MonsterController target, float now)
        {
            if (target == null || target.IsBleedImmune) return;
            for (int index = 0; index < _bleeds.Count; index++)
            {
                if (_bleeds[index].Target == target) { _bleeds[index].Refresh(now, _bleed.DurationSeconds); return; }
            }
            _bleeds.Add(new BeastBleed(target, now, _bleed.DurationSeconds));
        }

        sealed class BeastDash
        {
            readonly CompanionRuntime _representative; readonly AllyFollower _follower; readonly MonsterController _target;
            readonly Vector3 _origin; readonly Vector3 _dashPoint; readonly float _startedAt; readonly float _halfDuration;
            bool _hitApplied;
            public BeastDash(CompanionRuntime representative, AllyFollower follower, MonsterController target, Vector3 origin, Vector3 dashPoint, float startedAt, float totalDuration)
            { _representative=representative; _follower=follower; _target=target; _origin=origin; _dashPoint=dashPoint; _startedAt=startedAt; _halfDuration=totalDuration*0.5f; }
            public bool Advance(float now, ICombatImmediateHitModule hits, SynergyDamageData data, out MonsterController bleedTarget)
            {
                bleedTarget = null;
                if (_representative == null || _follower == null) return true;
                if (_representative.IsDown) { Cancel(); return true; }
                float elapsed=now-_startedAt;
                if (elapsed < _halfDuration) { _follower.TryMoveSynergyExternal(Vector2.Lerp(_origin,_dashPoint,Mathf.Clamp01(elapsed/_halfDuration))); return false; }
                if (_hitApplied == false)
                {
                    _hitApplied=true;
                    if (_target == null || _target.IsValid()==false || _target.Hp<=0) { Cancel(); return true; }
                    int damage=Mathf.RoundToInt(data.BaseValue);
                    if (_target.IsBoss) damage=Mathf.Max(1,Mathf.Min(damage,Mathf.FloorToInt(_target.MaxHp*data.BossMaxHpPercent)));
                    if (hits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(data.SynergyId,_target,_representative.transform.position,_target.transform.position,damage,AttackVisualKind.SingleHit,false,new CountableKillAttribution(0,data.SynergyId,CombatKillSourceCategory.SynergyAction))))
                        bleedTarget = _target.IsBoss ? _target : null;
                }
                float returnT=Mathf.Clamp01((elapsed-_halfDuration)/_halfDuration);
                _follower.TryMoveSynergyExternal(Vector2.Lerp(_dashPoint,_origin,returnT));
                if (returnT < 1.0f) return false;
                Cancel(); return true;
            }
            public void Cancel() { if(_follower!=null) _follower.SetSynergyExternalMovement(false); }
        }

        sealed class BeastBleed
        {
            public MonsterController Target { get; }
            float _until; float _nextTick;
            public BeastBleed(MonsterController target,float now,float duration){Target=target;Refresh(now,duration);}
            public void Refresh(float now,float duration){_until=now+duration;_nextTick=now+1.0f;}
            public bool Advance(float now,ICombatImmediateHitModule hits,SynergyDamageData data,string sourceId)
            {
                if(Target==null||Target.IsValid()==false||Target.Hp<=0) return true;
                float dueThrough=Mathf.Min(now,_until);
                float interval=Mathf.Max(0.01f,data.TickIntervalSeconds);
                while(_nextTick<=dueThrough)
                {
                    if(Target==null||Target.IsValid()==false||Target.Hp<=0) return true;
                    int damage=Mathf.RoundToInt(data.BaseValue); if(Target.IsBoss) damage=Mathf.Max(1,Mathf.Min(damage,Mathf.FloorToInt(Target.MaxHp*data.BossMaxHpPercent)));
                    hits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(sourceId,Target,Target.transform.position,Target.transform.position,damage,AttackVisualKind.SingleHit,false,new CountableKillAttribution(0,sourceId,CombatKillSourceCategory.SynergyAction)));
                    _nextTick+=interval;
                }
                return now>=_until&&_nextTick>_until;
            }
        }
    }
}
