using System;
using System.Collections.Generic;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Debugging;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Commander
{
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
            if (damage <= 0 || _owner.Hp <= 0)
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
                damage = monster.ResolveCompanionOutgoingCommanderDamage(damage, Time.time);

            if (monster != null)
                P0DeathReasonTracker.RecordEnemyDamage(monster, patternId);

            RunContext context = _owner.Services == null
                ? RunContext.Normal
                : _owner.Services.Context;
            TutorialDamageResolution tutorialDamage = TutorialDefeatProtection.Resolve(
                context,
                _owner.Hp,
                _owner.MaxHp,
                damage);
            int appliedDamage = tutorialDamage.AppliedDamage;
#if UNITY_EDITOR
            if (_editorAutomationInfiniteHp)
                appliedDamage = Mathf.Min(damage, Mathf.Max(0, _owner.Hp - 1));
#endif
            if (appliedDamage <= 0)
            {
                if (tutorialDamage.PreventedDefeat)
                    _owner.Hp = Mathf.Clamp(tutorialDamage.RecoveryHp, 1, _owner.MaxHp);
                return false;
            }

            int hpBefore = _owner.Hp;
            _owner.ApplyDamageFromReceiver(monster, appliedDamage);
            int actualDamage = Mathf.Max(0, hpBefore - _owner.Hp);
#if UNITY_EDITOR
            if (_editorAutomationInfiniteHp && _owner.MaxHp > 0)
                _owner.Hp = _owner.MaxHp;
            else if (tutorialDamage.PreventedDefeat)
                _owner.Hp = Mathf.Clamp(tutorialDamage.RecoveryHp, 1, _owner.MaxHp);
#else
            if (tutorialDamage.PreventedDefeat)
                _owner.Hp = Mathf.Clamp(tutorialDamage.RecoveryHp, 1, _owner.MaxHp);
#endif

            if (actualDamage <= 0)
                return false;

            _owner.Services?.Party?.TryActivateEmergencyRally(_owner.Hp, _owner.MaxHp, Time.time);

            FloatingDamageText.ShowFriendlyDamage(_owner, _owner.transform.position, actualDamage);
            P0PlaytestDiagnostics.RecordCommanderDamage(enemyId, patternId, actualDamage, GetHpPercent());
            P0Telemetry.Log(P0Telemetry.CommanderDamage, $"damage={actualDamage}", $"enemy_id={enemyId}", $"pattern_id={patternId}", $"hp_percent={GetHpPercent()}");
            P0Telemetry.Log(P0Telemetry.DamageApply, "target=commander", $"damage={actualDamage}", $"enemy_id={enemyId}", $"pattern_id={patternId}");
            if (enemyId == CombatIds.EliteRedCharger && patternId == CombatIds.RedChargerImpactGrace)
                P0Telemetry.Log(P0Telemetry.RedChargerImpactGraceHit, $"damage={actualDamage}", $"hp_percent={GetHpPercent()}");
            else if (enemyId == CombatIds.EliteRedCharger && patternId == CombatIds.RedChargerDash)
                P0Telemetry.Log(P0Telemetry.RedChargerImpactHit, $"damage={actualDamage}", $"pattern_id={patternId}", $"hp_percent={GetHpPercent()}");
            if (CombatIds.IsBossPattern(patternId))
                P0Telemetry.Log(P0Telemetry.BossPatternHit, "target=commander", $"pattern_id={patternId}", $"damage={actualDamage}");
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
