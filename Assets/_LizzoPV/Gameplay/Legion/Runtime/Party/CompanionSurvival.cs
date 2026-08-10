using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Skills.Guard;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal sealed class CompanionSurvival
    {
        private const float CONTACT_DAMAGE_GRACE_TIME = 0.6f;

        private readonly CompanionRuntime _owner;
        private float _recoverAt;
        private float _nextContactDamageTime;
        private float _spawnProtectedUntil;

        internal CompanionSurvival(CompanionRuntime owner)
        {
            _owner = owner;
        }

        internal void Initialize()
        {
            _spawnProtectedUntil = Time.time + ResolveSpawnProtectionSeconds();
            if (_spawnProtectedUntil > Time.time)
                AttackVisual.SpawnAttached(_owner.transform, AttackVisualKind.BuffApplied, new Vector3(0.0f, 0.28f, 0.0f));
        }

        internal void Tick()
        {
            _owner.Presentation.RefreshHealthBar();
            if (_owner.IsDown && Time.time >= _recoverAt)
                RecoverFromDown("auto_recover", GetRecoverHp(), "down_duration_elapsed");
        }

        internal bool ApplyHeal(int amount, string priorityReason)
        {
            if (amount <= 0)
                return false;

            if (_owner.IsDown)
            {
                RecoverFromDown("cleric_heal", Mathf.Max(amount, GetRecoverHp()), priorityReason);
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
            if (monster == null || _owner.IsDown || monster.IsValid() == false || Time.time < _nextContactDamageTime)
                return;

            EnemyRuntimeStats stats = monster.RuntimeStats;
            int damage = stats == null ? 2 : stats.AttackDamage;
            string sourceId = stats?.Data?.Id ?? monster.gameObject.name;
            string patternId = monster.GetDamagePatternId();
            string source = CombatIds.EnemyPatternSource(sourceId, patternId);
            if (monster.IsBoss && patternId != CombatIds.ContactAttack)
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * 0.8f));

            _nextContactDamageTime = Time.time + RemoteConfig.CompanionPostHitCooldown;
            if (TakeDamage(damage, source) && CombatIds.IsBossPattern(patternId))
            {
                P0Telemetry.Log(
                    P0Telemetry.BossPatternHit,
                    $"target={_owner.UnitId}",
                    $"pattern_id={patternId}",
                    $"damage={damage}");
            }
        }

        internal bool TryApplyBossPatternDamage(MonsterController monster, int damage, string patternId)
        {
            if (monster == null || _owner.IsDown || monster.IsValid() == false)
                return false;

            if (Time.time < _nextContactDamageTime)
            {
                EnemyRuntimeStats blockedStats = monster.RuntimeStats;
                string blockedSourceId = blockedStats?.Data?.Id ?? monster.gameObject.name;
                P0Telemetry.Log(
                    P0Telemetry.BossPatternRepeatBlock,
                    $"target={_owner.UnitId}",
                    $"enemy_id={blockedSourceId}",
                    $"pattern_id={patternId}",
                    $"remaining={_nextContactDamageTime - Time.time:0.##}");
                return false;
            }

            EnemyRuntimeStats stats = monster.RuntimeStats;
            string sourceId = stats?.Data?.Id ?? monster.gameObject.name;
            _nextContactDamageTime = Time.time + RemoteConfig.CompanionPostHitCooldown;
            return TakeDamage(damage, CombatIds.EnemyPatternSource(sourceId, patternId));
        }

        private bool TakeDamage(int damage, string source)
        {
            if (damage <= 0 || _owner.IsDown)
                return false;

            if (Time.time < _spawnProtectedUntil)
            {
                P0Telemetry.Log(
                    P0Telemetry.DamageBlockedInvulnerable,
                    $"target={_owner.UnitId}",
                    $"source={source}",
                    "reason=companion_spawn_protection");
                return false;
            }

            int originalDamage = damage;
            CompanionIncomingDamageResolution resolution = _owner.Party.ResolveCompanionIncomingDamage(
                _owner,
                originalDamage,
                _owner.Hp,
                Time.time);
            damage = resolution.AppliedDamage;
            _owner.Party.RecordCompanionDamagePrevention(in resolution);
            _owner.Hp = Mathf.Max(0, _owner.Hp - damage);
            FloatingDamageText.ShowFriendlyDamage(_owner.transform.position, damage);
            _owner.Presentation.RefreshHealthBar();
            P0Telemetry.Log(P0Telemetry.HurtboxContact, $"target={_owner.UnitId}", $"source={source}");
            P0Telemetry.Log(
                P0Telemetry.DamageApply,
                $"target={_owner.UnitId}",
                $"damage={damage}",
                $"source={source}",
                $"original_damage={originalDamage}");
            P0Telemetry.Log(
                P0Telemetry.CompanionDamage,
                $"unit_id={_owner.UnitId}",
                $"damage={damage}",
                $"hp_percent={GetHpPercent()}",
                $"source={source}");
            P0PlaytestDiagnostics.RecordCompanionDamage(_owner.UnitId, damage, GetHpPercent(), source);
            P0PlaytestDiagnostics.RecordEnemyContactDamage(source);

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
            _recoverAt = Time.time + RemoteConfig.CompanionDownDuration;
            _nextContactDamageTime = _recoverAt + CONTACT_DAMAGE_GRACE_TIME;
            _owner.Combat?.SetDown(true);
            _owner.Presentation.ApplyDownVisuals();

            P0Telemetry.Log(
                P0Telemetry.CompanionDown,
                $"unit_id={_owner.UnitId}",
                $"family_tags_snapshot={_owner.FamilyTags}",
                $"promoted_state={_owner.Promoted}",
                $"down_duration={RemoteConfig.CompanionDownDuration:0.##}",
                $"source={source}",
                $"slot_id={_owner.SlotId}");
            P0PlaytestDiagnostics.RecordCompanionDown(_owner.UnitId, source);
            _owner.Party.NotifyCompanionDown(_owner);
        }

        private void RecoverFromDown(string source, int recoverHp, string priorityReason)
        {
            _owner.IsDown = false;
            int beforeHp = _owner.Hp;
            _owner.Hp = Mathf.Clamp(recoverHp, 1, _owner.MaxHp);
            _owner.LastAppliedHealAmount = _owner.Hp - beforeHp;
            _nextContactDamageTime = Time.time + CONTACT_DAMAGE_GRACE_TIME;
            _owner.Combat?.SetDown(false);
            _owner.Presentation.RestoreVisuals();
            FloatingDamageText.ShowHeal(_owner.transform.position, _owner.LastAppliedHealAmount);
            AttackVisual.SpawnAttached(_owner.transform, AttackVisualKind.HealingReceived, new Vector3(0.0f, 0.28f, 0.0f));

            P0Telemetry.Log(
                P0Telemetry.CompanionRecover,
                $"unit_id={_owner.UnitId}",
                $"family_tags_snapshot={_owner.FamilyTags}",
                $"promoted_state={_owner.Promoted}",
                $"hp_percent={GetHpPercent()}",
                $"source={source}",
                $"priority_reason={priorityReason}",
                $"slot_id={_owner.SlotId}");
            _owner.Party.NotifyCompanionRecovered(_owner);
        }

        private static int ApplyGuardDamageReduction(int damage, string source, string unitId)
        {
            float multiplier = GuardSquadSkillBehaviour.CompanionDamageMultiplier;
            if (multiplier >= 0.999f)
                return damage;

            int reducedDamage = Mathf.Max(1, Mathf.CeilToInt(damage * multiplier));
            P0Telemetry.Log(
                P0Telemetry.GuardWallBlockContact,
                $"combo_id={CombatIds.GuardSquad}",
                $"cast_id={GuardSquadSkillBehaviour.ActiveCastId}",
                $"target={unitId}",
                $"source={source}",
                $"original_damage={damage}",
                $"reduced_damage={reducedDamage}",
                $"blocked_damage={Mathf.Max(0, damage - reducedDamage)}");
            return reducedDamage;
        }

        private int GetRecoverHp()
        {
            return Mathf.Max(1, Mathf.RoundToInt(_owner.MaxHp * RemoteConfig.CompanionRecoverHpRatio));
        }

        private float ResolveSpawnProtectionSeconds()
        {
            return _owner.UnitId == "archer"
                ? Mathf.Max(RemoteConfig.CompanionSpawnProtection, RemoteConfig.ArcherSpawnProtection)
                : RemoteConfig.CompanionSpawnProtection;
        }

        private int GetHpPercent()
        {
            if (_owner.MaxHp <= 0)
                return 0;

            return Mathf.Clamp(Mathf.RoundToInt((float)_owner.Hp / _owner.MaxHp * 100.0f), 0, 100);
        }
    }
}
