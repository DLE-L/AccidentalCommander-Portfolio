using System.Collections.Generic;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.Legion
{
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
        internal static int RecoverHp(CompanionRuntime owner) => Mathf.Max(1, Mathf.RoundToInt(owner.MaxHp * owner.Party.Tuning.CompanionRecoverHpRatio));
        internal static float SpawnProtection(CompanionRuntime owner) => owner.UnitId == "archer"
            ? Mathf.Max(owner.Party.Tuning.CompanionSpawnProtection, owner.Party.Tuning.ArcherSpawnProtection)
            : owner.Party.Tuning.CompanionSpawnProtection;
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
    internal static class CompanionDamageEligibility
    {
        internal static bool CanReceive(CompanionRuntime owner, MonsterController monster) => false;
    }
}
