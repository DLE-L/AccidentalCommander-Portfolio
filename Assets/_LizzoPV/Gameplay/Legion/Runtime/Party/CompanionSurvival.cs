using System.Collections.Generic;
using Lizzo.PV.Legion.Combat.Attacks;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal static class EmergencyRallyRosterSlots
    {
        internal static List<string> Collect(IReadOnlyList<CompanionRuntime> companions)
        {
            var slots = new List<string>(companions.Count);
            for (int index = 0; index < companions.Count; index++)
            {
                CompanionRuntime companion = companions[index];
                if (companion != null && companion.IsDown == false && string.IsNullOrEmpty(companion.RosterSlotId) == false)
                    slots.Add(companion.RosterSlotId);
            }
            return slots;
        }

        internal static bool HasOtherActive(IReadOnlyList<CompanionRuntime> companions, CompanionRuntime released)
        {
            for (int index = 0; index < companions.Count; index++)
            {
                CompanionRuntime other = companions[index];
                if (other != null && other != released && other.IsDown == false && other.RosterSlotId == released.RosterSlotId)
                    return true;
            }
            return false;
        }
    }

    internal static class CompanionPartyHealCounter
    {
        internal static int Apply(IReadOnlyList<CompanionRuntime> companions, int amount)
        {
            int healed = 0;
            for (int index = 0; index < companions.Count; index++)
                if (companions[index] != null && companions[index].ApplyHeal(amount, "small_heal_card"))
                    healed++;
            return healed;
        }
    }

    internal sealed class CompanionSurvivalTiming
    {
        const float ContactDamageGraceTime = 0.6f;
        float _recoverAt;
        float _nextDamageAt;
        float _spawnProtectedUntil;

        internal bool RecoveryDue(float now) => now >= _recoverAt;
        internal bool DamageReady(float now) => now >= _nextDamageAt;
        internal bool SpawnProtected(float now) => now < _spawnProtectedUntil;
        internal float DamageBlockRemaining(float now) => _nextDamageAt - now;
        internal void StartSpawnProtection(float now, float seconds) => _spawnProtectedUntil = now + seconds;
        internal void StartPostHitCooldown(float now, float seconds) => _nextDamageAt = now + seconds;
        internal void EnterDown(float now, float duration)
        {
            _recoverAt = now + duration;
            _nextDamageAt = _recoverAt + ContactDamageGraceTime;
        }
        internal void Recover(float now) => _nextDamageAt = now + ContactDamageGraceTime;
    }

    internal static class CompanionSurvivalHealthMath
    {
        internal static int RecoverHp(CompanionRuntime owner) => Mathf.Max(1, Mathf.RoundToInt(owner.MaxHp * RemoteConfig.CompanionRecoverHpRatio));
        internal static float SpawnProtection(CompanionRuntime owner) => owner.UnitId == "archer"
            ? Mathf.Max(RemoteConfig.CompanionSpawnProtection, RemoteConfig.ArcherSpawnProtection)
            : RemoteConfig.CompanionSpawnProtection;
        internal static int HpPercent(CompanionRuntime owner) => owner.MaxHp <= 0
            ? 0
            : Mathf.Clamp(Mathf.RoundToInt((float)owner.Hp / owner.MaxHp * 100.0f), 0, 100);
    }
    internal readonly struct CompanionContactDamage
    {
        internal CompanionContactDamage(int damage, string source, string patternId) { Damage = damage; Source = source; PatternId = patternId; }
        internal int Damage { get; }
        internal string Source { get; }
        internal string PatternId { get; }
    }
    internal static class CompanionContactDamageResolver
    {
        internal static CompanionContactDamage Resolve(MonsterController monster)
        {
            EnemyRuntimeStats stats = monster.RuntimeStats;
            int damage = stats == null ? 2 : stats.AttackDamage;
            string sourceId = stats?.Data?.Id ?? monster.gameObject.name;
            string patternId = monster.GetDamagePatternId();
            if (monster.IsBoss && patternId != CombatIds.ContactAttack)
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * 0.8f));
            return new CompanionContactDamage(damage, CombatIds.EnemyPatternSource(sourceId, patternId), patternId);
        }
    }

    public sealed partial class PartyService
    {
        public void NotifyCompanionDown(CompanionRuntime companion)
        {
            if (companion == null)
                return;

            _incomingDamage.RemoveGuardShockwaveProtection(companion);
            _runTraitEffects?.NotifyEmergencyRallyRecipientDown(companion.RosterSlotId);

            if (companion.IsFamily(SHIELD_FAMILY_TAG))
            {
                P0Telemetry.Log(
                    P0Telemetry.FrontLinePressure,
                    "reason=shield_family_down",
                    $"unit_id={companion.UnitId}",
                    $"slot_id={companion.SlotId}");
            }

            if (GuardSquadActivatedState)
            {
                P0Telemetry.Log(
                    P0Telemetry.SynergyKeep,
                    "reason=companion_down",
                    "combo_id=guard_squad",
                    this.GetFamilyTagsSnapshotParameter(),
                    this.GetPromotedStateParameter());
            }
        }

        public void NotifyCompanionRecovered(CompanionRuntime companion)
        {
            if (companion == null)
                return;

            if (companion.IsFamily(SHIELD_FAMILY_TAG))
            {
                P0Telemetry.Log(
                    P0Telemetry.FrontLinePressure,
                    "reason=shield_family_recovered",
                    $"unit_id={companion.UnitId}",
                    $"slot_id={companion.SlotId}");
            }

            if (GuardSquadActivatedState)
            {
                P0Telemetry.Log(
                    P0Telemetry.SynergyKeep,
                    "reason=companion_recover",
                    "combo_id=guard_squad",
                    this.GetFamilyTagsSnapshotParameter(),
                    this.GetPromotedStateParameter());
            }
        }

        internal bool TryActivateEmergencyRally(int commanderHp, int commanderMaxHp, float currentTime)
        {
            if (_runTraitEffects == null || _registry.Player == null)
                return false;

            List<string> rosterSlotIds = EmergencyRallyRosterSlots.Collect(Companions);

            if (_runTraitEffects.TryActivateEmergencyRally(commanderHp, commanderMaxHp, rosterSlotIds, currentTime) == false)
                return false;

            this.RefreshFormationForCurrentRoster(_registry.Player.transform, "emergency_rally");
            return true;
        }

        internal void NotifyEmergencyRallyCompanionReleased(CompanionRuntime companion)
        {
            if (companion == null || string.IsNullOrEmpty(companion.RosterSlotId))
                return;

            if (EmergencyRallyRosterSlots.HasOtherActive(Companions, companion))
                return;

            _runTraitEffects?.NotifyEmergencyRallyRecipientDown(companion.RosterSlotId);
        }

        public int ApplySmallHealToCompanions(int amount)
        {
            return CompanionPartyHealCounter.Apply(Companions, amount);
        }

        public bool TryResolveClericHeal(int healAmount, Vector3 casterPosition)
        {
            return ClericHealAttack.TryResolve(this, healAmount);
        }
    }

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
            if (monster == null || _owner.IsDown || monster.IsValid() == false || _timing.DamageReady(Time.time) == false)
                return;

            CompanionContactDamage contact = CompanionContactDamageResolver.Resolve(monster);

            _timing.StartPostHitCooldown(Time.time, RemoteConfig.CompanionPostHitCooldown);
            if (TakeDamage(contact.Damage, contact.Source) && CombatIds.IsBossPattern(contact.PatternId))
            {
                P0Telemetry.Log(
                    P0Telemetry.BossPatternHit,
                    $"target={_owner.UnitId}",
                    $"pattern_id={contact.PatternId}",
                    $"damage={contact.Damage}");
            }
        }

        internal bool TryApplyBossPatternDamage(MonsterController monster, int damage, string patternId)
        {
            if (monster == null || _owner.IsDown || monster.IsValid() == false)
                return false;

            if (_timing.DamageReady(Time.time) == false)
            {
                EnemyRuntimeStats blockedStats = monster.RuntimeStats;
                string blockedSourceId = blockedStats?.Data?.Id ?? monster.gameObject.name;
                P0Telemetry.Log(
                    P0Telemetry.BossPatternRepeatBlock,
                    $"target={_owner.UnitId}",
                    $"enemy_id={blockedSourceId}",
                    $"pattern_id={patternId}",
                    $"remaining={_timing.DamageBlockRemaining(Time.time):0.##}");
                return false;
            }

            EnemyRuntimeStats stats = monster.RuntimeStats;
            string sourceId = stats?.Data?.Id ?? monster.gameObject.name;
            _timing.StartPostHitCooldown(Time.time, RemoteConfig.CompanionPostHitCooldown);
            return TakeDamage(damage, CombatIds.EnemyPatternSource(sourceId, patternId));
        }

        private bool TakeDamage(int damage, string source)
        {
            if (damage <= 0 || _owner.IsDown)
                return false;

            if (_timing.SpawnProtected(Time.time))
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
                $"hp_percent={CompanionSurvivalHealthMath.HpPercent(_owner)}",
                $"source={source}");
            P0PlaytestDiagnostics.RecordCompanionDamage(_owner.UnitId, damage, CompanionSurvivalHealthMath.HpPercent(_owner), source);
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
            _timing.EnterDown(Time.time, RemoteConfig.CompanionDownDuration);
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
            _timing.Recover(Time.time);
            _owner.Combat?.SetDown(false);
            _owner.Presentation.RestoreVisuals();
            FloatingDamageText.ShowHeal(_owner.transform.position, _owner.LastAppliedHealAmount);
            AttackVisual.SpawnAttached(_owner.transform, AttackVisualKind.HealingReceived, new Vector3(0.0f, 0.28f, 0.0f));

            P0Telemetry.Log(
                P0Telemetry.CompanionRecover,
                $"unit_id={_owner.UnitId}",
                $"family_tags_snapshot={_owner.FamilyTags}",
                $"promoted_state={_owner.Promoted}",
                $"hp_percent={CompanionSurvivalHealthMath.HpPercent(_owner)}",
                $"source={source}",
                $"priority_reason={priorityReason}",
                $"slot_id={_owner.SlotId}");
            _owner.Party.NotifyCompanionRecovered(_owner);
        }

    }
}
