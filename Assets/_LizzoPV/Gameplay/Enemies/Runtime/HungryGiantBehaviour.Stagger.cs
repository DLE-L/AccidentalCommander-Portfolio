using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    public sealed partial class HungryGiantBehaviour
    {
        public static int ResolveIncomingDamageForBossTarget(MonsterController target, int damage)
        {
            if (damage <= 0 || target == null || Current == null || Current._monster != target)
                return damage;

            return Current.ResolveStaggerIncomingDamage(damage);
        }

        internal int ResolveStaggerIncomingDamage(int damage)
        {
            if (damage <= 0 || IsStaggered == false)
                return damage;

            return Mathf.Max(damage + 1, Mathf.RoundToInt(damage * BOSS_STAGGER_DAMAGE_MULTIPLIER));
        }

        private void BeginBossStagger(string patternId)
        {
            if (_monster == null || _monster.Hp <= 0)
                return;

            _staggerRemaining = BOSS_STAGGER_SECONDS;
            _staggerPatternId = string.IsNullOrEmpty(patternId) ? "unknown" : patternId;

            if (_spriteRenderer != null)
                _spriteRenderer.color = BossStaggerColor;

            HitStop.Request(0.08f, "boss_stagger_start");
            FloatingDamageText.ShowLabel(
                transform.position + Vector3.up * 2.9f,
                "지금 공격!",
                BossStaggerLabelColor,
                large: true,
                lifeTime: 2.0f);
            FloatingDamageText.ShowLabel(
                transform.position + Vector3.up * 2.15f,
                "거인이 흔들립니다!",
                BossStaggerLabelColor,
                large: true,
                lifeTime: 1.7f);

            RunTelemetry.Log(
                RunTelemetry.BossStaggerStart,
                RunTelemetry.RunTimeSecondsParameter,
                $"pattern_id={_staggerPatternId}",
                $"duration={BOSS_STAGGER_SECONDS:0.##}",
                $"damage_multiplier={BOSS_STAGGER_DAMAGE_MULTIPLIER:0.##}",
                $"boss_hp_percent={GetCurrentHpPercent()}");
        }

        private void UpdateBossStagger()
        {
            if (_staggerRemaining <= 0.0f)
                return;

            _staggerRemaining -= Time.fixedDeltaTime;
            if (_staggerRemaining > 0.0f)
                return;

            _staggerRemaining = 0.0f;
            _staggerPatternId = string.Empty;

            if (_spriteRenderer != null)
                _spriteRenderer.color = _baseColor;
        }

        private void ClearBossStagger()
        {
            _staggerRemaining = 0.0f;
            _staggerPatternId = string.Empty;
        }
    }
}
