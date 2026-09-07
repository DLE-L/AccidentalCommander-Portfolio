using Lizzo.PV.P0.Combat;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal sealed class CompanionSurvival
    {
        private readonly CompanionRuntime _owner;
        private readonly CompanionSurvivalTiming _timing = new CompanionSurvivalTiming();

        internal CompanionSurvival(CompanionRuntime owner)
        {
            _owner = owner;
        }

        internal void Initialize()
        {
            _timing.StartSpawnProtection(Time.time, CompanionSurvivalHealthMath.SpawnProtection(_owner));
            if (_timing.SpawnProtected(Time.time))
                AttackVisual.SpawnAttached(_owner.transform, AttackVisualKind.BuffApplied, new Vector3(0.0f, 0.28f, 0.0f));
        }

        internal void Tick()
        {
            _owner.Presentation.RefreshHealthBar();
            if (_owner.IsDown && _timing.RecoveryDue(Time.time))
                RecoverFromDown("auto_recover", CompanionSurvivalHealthMath.RecoverHp(_owner), "down_duration_elapsed");
        }

        internal bool ApplyHeal(int amount, string priorityReason)
        {
            if (amount <= 0)
                return false;

            if (_owner.IsDown)
            {
                RecoverFromDown("cleric_heal", Mathf.Max(amount, CompanionSurvivalHealthMath.RecoverHp(_owner)), priorityReason);
                return true;
            }

            if (_owner.Hp >= _owner.MaxHp)
                return false;

            int beforeHp = _owner.Hp;
            _owner.Hp = Mathf.Min(_owner.MaxHp, _owner.Hp + amount);
            _owner.LastAppliedHealAmount = _owner.Hp - beforeHp;
            FloatingDamageText.ShowHeal(_owner.transform.position, _owner.LastAppliedHealAmount);
            AttackVisual.SpawnAttached(_owner.transform, AttackVisualKind.HealingReceived, new Vector3(0.0f, 0.28f, 0.0f));
            _owner.Presentation.RefreshHealthBar();
            return true;
        }

        internal void TryTakeContactDamage(MonsterController monster)
        {
            if (CompanionDamageEligibility.CanReceive(_owner, monster) == false || _timing.DamageReady(Time.time) == false)
                return;

            CompanionContactDamage contact = CompanionContactDamageResolver.Resolve(monster);

            _timing.StartPostHitCooldown(Time.time, _owner.Party.Tuning.CompanionPostHitCooldown);
            if (TakeDamage(contact.Damage, contact.Source) && CombatIds.IsBossPattern(contact.PatternId))
            {
                RunTelemetry.Log(
                    RunTelemetry.BossPatternHit,
                    $"target={_owner.UnitId}",
                    $"pattern_id={contact.PatternId}",
                    $"damage={contact.Damage}");
            }
        }

        internal bool TryApplyBossPatternDamage(MonsterController monster, int damage, string patternId)
        {
            if (CompanionDamageEligibility.CanReceive(_owner, monster) == false)
                return false;

            if (_timing.DamageReady(Time.time) == false)
            {
                CompanionBossRepeatBlockTelemetry.Log(
                    _owner, monster, patternId, _timing.DamageBlockRemaining(Time.time));
                return false;
            }

            EnemyRuntimeStats stats = monster.RuntimeStats;
            string sourceId = stats?.Data?.Id ?? monster.gameObject.name;
            _timing.StartPostHitCooldown(Time.time, _owner.Party.Tuning.CompanionPostHitCooldown);
            return TakeDamage(damage, CombatIds.EnemyPatternSource(sourceId, patternId));
        }

        private bool TakeDamage(int damage, string source)
        {
            if (damage <= 0 || _owner.IsDown)
                return false;

            if (_timing.SpawnProtected(Time.time))
            {
                RunTelemetry.Log(
                    RunTelemetry.DamageBlockedInvulnerable,
                    $"target={_owner.UnitId}",
                    $"source={source}",
                    "reason=companion_spawn_protection");
                return false;
            }

            int originalDamage = damage;
            CompanionIncomingDamageResolution resolution = _owner.Party.ResolveCompanionIncomingDamage(
                _owner,
                originalDamage,
                _owner.Hp);
            damage = resolution.AppliedDamage;
            _owner.Hp = Mathf.Max(0, _owner.Hp - damage);
            FloatingDamageText.ShowFriendlyDamage(_owner, _owner.transform.position, damage);
            _owner.Presentation.RefreshHealthBar();
            CompanionDamageTelemetry.Record(_owner, damage, originalDamage, source);

            if (_owner.Hp <= 0)
            {
                EnterDownState(source);
                return true;
            }

            if (_owner.HitFlash == null)
            {
                Debug.LogError($"Companion prefab is missing required HitFlash: {_owner.gameObject.name}", _owner);
                return true;
            }

            _owner.HitFlash.Play();
            return true;
        }

        private void EnterDownState(string source)
        {
            _owner.IsDown = true;
            _timing.EnterDown(Time.time, _owner.Party.Tuning.CompanionDownDuration);
            _owner.Combat?.SetDown(true);
            _owner.Presentation.ApplyDownVisuals();

            CompanionStateTransitionTelemetry.Down(_owner, source);
            _owner.Party.NotifyCompanionDown(_owner);
        }

        private void RecoverFromDown(string source, int recoverHp, string priorityReason)
        {
            _owner.IsDown = false;
            int beforeHp = _owner.Hp;
            _owner.Hp = Mathf.Clamp(recoverHp, 1, _owner.MaxHp);
            _owner.LastAppliedHealAmount = _owner.Hp - beforeHp;
            _timing.Recover(Time.time);
            _owner.Combat?.SetDown(false);
            _owner.Presentation.RestoreVisuals();
            FloatingDamageText.ShowHeal(_owner.transform.position, _owner.LastAppliedHealAmount);
            AttackVisual.SpawnAttached(_owner.transform, AttackVisualKind.HealingReceived, new Vector3(0.0f, 0.28f, 0.0f));

            CompanionStateTransitionTelemetry.Recover(_owner, source, priorityReason);
            _owner.Party.NotifyCompanionRecovered(_owner);
        }

    }
}
