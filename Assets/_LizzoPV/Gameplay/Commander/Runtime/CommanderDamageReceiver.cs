using System;
using System.Collections.Generic;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Debugging;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Commander
{
    internal sealed class CommanderHurtbox
    {
        private readonly PlayerController _owner;

        internal CommanderHurtbox(PlayerController owner, CircleCollider2D collider)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Collider = collider;
        }

        internal CircleCollider2D Collider { get; }

        internal void ValidateRequired()
        {
            if (Collider == null)
            {
                Debug.LogError("Commander prefab is missing required CombatCollider reference.", _owner);
                return;
            }

            if (Collider.isTrigger == false)
                Debug.LogError("Commander CombatCollider must be trigger.", _owner);
        }

        internal void ValidateEnabled()
        {
            if (Collider == null)
            {
                ValidateRequired();
                return;
            }

            if (Collider.enabled == false)
                Debug.LogError("Commander CombatCollider is disabled.", _owner);
        }

        internal bool OverlapsCircle(Vector2 circleCenter, float circleRadius)
        {
            if (Collider == null)
            {
                ValidateRequired();
                return false;
            }
            if (Collider.enabled == false)
                return false;

            Vector2 hurtboxCenter = ResolveCenter();
            float overlapDistance = Mathf.Max(0.0f, circleRadius) + ResolveRadius();
            return (hurtboxCenter - circleCenter).sqrMagnitude <= overlapDistance * overlapDistance;
        }

        internal bool OverlapsCapsule(Vector2 segmentStart, Vector2 segmentEnd, float radius)
        {
            if (Collider == null)
            {
                ValidateRequired();
                return false;
            }
            if (Collider.enabled == false)
                return false;

            Vector2 hurtboxCenter = ResolveCenter();
            float overlapDistance = Mathf.Max(0.0f, radius) + ResolveRadius();
            Vector2 closestPoint = GetClosestPointOnSegment(segmentStart, segmentEnd, hurtboxCenter);
            return (hurtboxCenter - closestPoint).sqrMagnitude <= overlapDistance * overlapDistance;
        }

        private Vector2 ResolveCenter()
        {
            return Collider.transform.TransformPoint(Collider.offset);
        }

        private float ResolveRadius()
        {
            float maxScale = Mathf.Max(
                Mathf.Abs(Collider.transform.lossyScale.x),
                Mathf.Abs(Collider.transform.lossyScale.y));
            return Collider.radius * maxScale;
        }

        private static Vector2 GetClosestPointOnSegment(Vector2 segmentStart, Vector2 segmentEnd, Vector2 point)
        {
            Vector2 segment = segmentEnd - segmentStart;
            float lengthSqr = segment.sqrMagnitude;
            if (lengthSqr <= 0.000001f)
                return segmentStart;

            float t = Vector2.Dot(point - segmentStart, segment) / lengthSqr;
            return segmentStart + segment * Mathf.Clamp01(t);
        }
    }

    public sealed class CommanderDamageReceiver
    {
        readonly PlayerController _owner;
        readonly HitFlash _hitFlash;
        readonly Dictionary<string, float> _nextDamageTimeBySource = new Dictionary<string, float>();

        bool _loggedLowHp30;
        bool _loggedLowHp10;
        float _invulnerableUntil;
#if UNITY_EDITOR
        bool _editorAutomationInfiniteHp;
#endif

        public CommanderDamageReceiver(PlayerController owner, HitFlash hitFlash)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _hitFlash = hitFlash;
        }

        public bool TryApply(MonsterController monster, int damage, string overridePatternId = null)
        {
            if (damage <= 0)
                return false;

            string enemyId = monster == null ? CombatIds.Unknown : monster.GetDamageEnemyId();
            string patternId = string.IsNullOrEmpty(overridePatternId)
                ? monster == null ? CombatIds.Unknown : monster.GetDamagePatternId()
                : overridePatternId;
            string sourceKey = ResolveDamageSourceKey(monster, patternId);

            if (monster != null)
            {
                P0PlaytestDiagnostics.RecordCommanderHurtboxContact(enemyId, patternId);
                P0Telemetry.Log(P0Telemetry.HurtboxContact, "target=commander", $"enemy_id={enemyId}", $"pattern_id={patternId}");
            }

            if (_nextDamageTimeBySource.TryGetValue(sourceKey, out float nextSourceDamageTime)
                && Time.time < nextSourceDamageTime)
            {
                if (CombatIds.IsBossPattern(patternId))
                {
                    P0Telemetry.Log(
                        P0Telemetry.BossPatternRepeatBlock,
                        "target=commander",
                        $"enemy_id={enemyId}",
                        $"pattern_id={patternId}",
                        $"remaining={nextSourceDamageTime - Time.time:0.##}");
                }

                P0Telemetry.Log(
                    P0Telemetry.DamageBlockedCooldown,
                    "target=commander",
                    $"enemy_id={enemyId}",
                    $"pattern_id={patternId}",
                    $"remaining={nextSourceDamageTime - Time.time:0.##}");
                return false;
            }

            if (Time.time < _invulnerableUntil)
            {
                P0Telemetry.Log(
                    P0Telemetry.DamageBlockedInvulnerable,
                    "target=commander",
                    $"enemy_id={enemyId}",
                    $"pattern_id={patternId}",
                    $"remaining={_invulnerableUntil - Time.time:0.##}");
                return false;
            }

            if (monster != null)
                P0DeathReasonTracker.RecordEnemyDamage(monster, patternId);

#if UNITY_EDITOR
            int appliedDamage = _editorAutomationInfiniteHp
                ? Mathf.Min(damage, Mathf.Max(0, _owner.Hp - 1))
                : damage;
            _owner.ApplyDamageFromReceiver(monster, appliedDamage);
            if (_editorAutomationInfiniteHp && _owner.MaxHp > 0)
                _owner.Hp = _owner.MaxHp;
#else
            _owner.ApplyDamageFromReceiver(monster, damage);
#endif

            _owner.Services?.Party?.TryActivateEmergencyRally(_owner.Hp, _owner.MaxHp, Time.time);

            FloatingDamageText.ShowFriendlyDamage(_owner.transform.position, damage);
            P0PlaytestDiagnostics.RecordCommanderDamage(enemyId, patternId, damage, GetHpPercent());
            P0Telemetry.Log(P0Telemetry.CommanderDamage, $"damage={damage}", $"enemy_id={enemyId}", $"pattern_id={patternId}", $"hp_percent={GetHpPercent()}");
            P0Telemetry.Log(P0Telemetry.DamageApply, "target=commander", $"damage={damage}", $"enemy_id={enemyId}", $"pattern_id={patternId}");
            if (enemyId == CombatIds.EliteRedCharger && patternId == CombatIds.RedChargerImpactGrace)
                P0Telemetry.Log(P0Telemetry.RedChargerImpactGraceHit, $"damage={damage}", $"hp_percent={GetHpPercent()}");
            else if (enemyId == CombatIds.EliteRedCharger && patternId == CombatIds.RedChargerDash)
                P0Telemetry.Log(P0Telemetry.RedChargerImpactHit, $"damage={damage}", $"pattern_id={patternId}", $"hp_percent={GetHpPercent()}");
            if (CombatIds.IsBossPattern(patternId))
                P0Telemetry.Log(P0Telemetry.BossPatternHit, "target=commander", $"pattern_id={patternId}", $"damage={damage}");
            if (monster != null)
                P0PlaytestDiagnostics.RecordEnemyContactDamage(monster);

            if (_hitFlash == null)
            {
                Debug.LogError("Commander prefab is missing required HitFlash.", _owner);
                return true;
            }

            _hitFlash.Play();

            if (monster != null)
            {
                _invulnerableUntil = Time.time + RemoteConfig.CommanderPostHitInvuln;
                _nextDamageTimeBySource[sourceKey] = Time.time + RemoteConfig.ContactDamageSourceCooldown;
                P0Telemetry.Log(P0Telemetry.CommanderInvulnStart, $"duration={RemoteConfig.CommanderPostHitInvuln:0.##}");
            }

            LogCommanderLowHp();
            return true;
        }

        public void ResetForSpawn()
        {
            ResetLowHpWarnings();
            _invulnerableUntil = 0.0f;
            _nextDamageTimeBySource.Clear();
#if UNITY_EDITOR
            _editorAutomationInfiniteHp = false;
#endif
        }

        public void ResetLowHpWarnings()
        {
            _loggedLowHp30 = false;
            _loggedLowHp10 = false;
        }

#if UNITY_EDITOR
        public void SetEditorAutomationInfiniteHp(bool enabled)
        {
            _editorAutomationInfiniteHp = enabled;
            if (enabled && _owner.MaxHp > 0)
                _owner.Hp = _owner.MaxHp;
        }

        public bool EditorAutomationInfiniteHpEnabled => _editorAutomationInfiniteHp;
#endif

        string ResolveDamageSourceKey(MonsterController monster, string patternId)
        {
            if (monster == null)
                return CombatIds.Unknown;

            if (string.IsNullOrEmpty(patternId))
                return monster.GetDamageSourceKey();

            return CombatIds.DamageCooldownKey(monster.GetDamageEnemyId(), monster.GetInstanceID(), patternId);
        }

        int GetHpPercent()
        {
            if (_owner.MaxHp <= 0)
                return 0;

            return Mathf.Clamp(Mathf.RoundToInt((float)_owner.Hp / _owner.MaxHp * 100.0f), 0, 100);
        }

        void LogCommanderLowHp()
        {
            if (_owner.MaxHp <= 0 || _owner.Hp <= 0)
                return;

            int hpPercent = Mathf.CeilToInt((float)_owner.Hp / _owner.MaxHp * 100.0f);
            if (_loggedLowHp30 == false && hpPercent <= 30)
            {
                _loggedLowHp30 = true;
                P0Telemetry.Log(P0Telemetry.CommanderLowHp, "threshold=30", $"hp_percent={hpPercent}");
            }

            if (_loggedLowHp10 == false && hpPercent <= 10)
            {
                _loggedLowHp10 = true;
                P0Telemetry.Log(P0Telemetry.CommanderLowHp, "threshold=10", $"hp_percent={hpPercent}");
            }
        }
    }
}
