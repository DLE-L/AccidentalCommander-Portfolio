using System.Collections.Generic;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Config;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.P0.Units
{
    public sealed partial class HungryGiantBehaviour
    {
        private void BeginBossAoeWarning(Vector2 center)
        {
            _aoeCenter = center;
            _aoeWarningDuration = Mathf.Clamp(RemoteConfig.Boss1WarningTime + BOSS_AOE_WARNING_BONUS_SECONDS, 1.25f, 1.45f);
            _aoeWarningRemaining = _aoeWarningDuration;
            PlayBossAttackMotion(_aoeCenter - new Vector2(transform.position.x, transform.position.y), _aoeWarningRemaining);
            ResolveBossAoeWarningRenderer();

            if (_aoeWarningRenderer == null)
                return;

            _aoeWarningRenderer.transform.position = new Vector3(_aoeCenter.x, _aoeCenter.y, transform.position.z);
            _aoeWarningRenderer.transform.localScale = Vector3.one * (BOSS_AOE_RADIUS * 1.64f);
            _aoeWarningRenderer.color = new Color(1.0f, 0.18f, 0.05f, 0.52f);
            _aoeWarningRenderer.enabled = true;
            RetroVfx.Spawn(RetroVfxKind.BossWarning, new Vector3(_aoeCenter.x, _aoeCenter.y, transform.position.z), Vector3.zero, 1.0f);
            P0Telemetry.Log(
                P0Telemetry.BossPatternWarningShow,
                $"source_id={CombatIds.BossHungryGiant}",
                $"pattern_id={BossAoePatternId}",
                $"warning={_aoeWarningRemaining:0.##}",
                $"radius={BOSS_AOE_RADIUS:0.##}");
        }

        private void UpdateBossAoeWarning()
        {
            _aoeWarningRemaining -= Time.fixedDeltaTime;

            if (_aoeWarningRenderer != null)
            {
                float progress = _aoeWarningDuration <= 0.0f
                    ? 1.0f
                    : 1.0f - Mathf.Clamp01(_aoeWarningRemaining / _aoeWarningDuration);
                float pulse = Mathf.PingPong(Time.time * 5.5f, 1.0f);
                float alpha = Mathf.Lerp(0.36f, 0.66f, pulse);
                float diameter = Mathf.Lerp(BOSS_AOE_RADIUS * 1.64f, BOSS_AOE_RADIUS * 2.0f, progress);
                _aoeWarningRenderer.transform.position = new Vector3(_aoeCenter.x, _aoeCenter.y, transform.position.z);
                _aoeWarningRenderer.transform.localScale = Vector3.one * diameter;
                _aoeWarningRenderer.color = new Color(1.0f, 0.18f, 0.05f, alpha);
            }

            if (_aoeWarningRemaining > 0.0f)
                return;

            ApplyBossAoeDamageFrame();
            _aoeCooldownRemaining = BOSS_AOE_COOLDOWN_SECONDS;
        }

        private void ApplyBossAoeDamageFrame()
        {
            _isAoeDamageFrame = true;
            int damage = RemoteConfig.Boss1Atk;
            PlayBossAttackMotion(_aoeCenter - new Vector2(transform.position.x, transform.position.y), 0.35f);
            RetroVfx.Spawn(RetroVfxKind.BossAttackHit, new Vector3(_aoeCenter.x, _aoeCenter.y, transform.position.z), Vector3.zero, 1.0f);

            PlayerController player = _monster.Services.Registry?.Player;
            if (player != null && player.Hp > 0 && player.IsHurtboxOverlappingCircle(_aoeCenter, BOSS_AOE_RADIUS))
            {
                if (player.TryApplyBossPatternDamage(_monster, damage))
                {
                    P0Telemetry.Log(
                        P0Telemetry.BossPatternHit,
                        "target=commander",
                        $"pattern_id={BossAoePatternId}",
                        $"damage={damage}");
                }
            }

            IReadOnlyList<CompanionRuntime> companions = _monster.Services.Party.ActiveCompanions;
            int companionDamage = Mathf.Max(1, Mathf.RoundToInt(damage * BOSS_COMPANION_PATTERN_DAMAGE_SCALE));
            for (int i = 0; i < companions.Count; i++)
            {
                CompanionRuntime companion = companions[i];
                if (companion == null || companion.IsHurtboxOverlappingCircle(_aoeCenter, BOSS_AOE_RADIUS) == false)
                    continue;

                if (companion.TryApplyBossPatternDamage(_monster, companionDamage, BossAoePatternId))
                {
                    P0Telemetry.Log(
                        P0Telemetry.BossPatternHit,
                        $"target={companion.UnitId}",
                        $"pattern_id={BossAoePatternId}",
                        $"damage={companionDamage}");
                }
            }

            _isAoeDamageFrame = false;
            ShowBossAoeImpact();
            BeginBossStagger(BossAoePatternId);
        }

        private void ShowBossAoeImpact()
        {
            if (_aoeWarningRenderer == null)
                return;

            _aoeImpactRemaining = BOSS_AOE_IMPACT_LINGER_SECONDS;
            _aoeWarningRenderer.transform.position = new Vector3(_aoeCenter.x, _aoeCenter.y, transform.position.z);
            _aoeWarningRenderer.transform.localScale = Vector3.one * (BOSS_AOE_RADIUS * 2.16f);
            _aoeWarningRenderer.color = new Color(1.0f, 0.78f, 0.12f, 0.62f);
            _aoeWarningRenderer.enabled = true;
        }

        private void UpdateBossAoeImpact()
        {
            if (_aoeImpactRemaining <= 0.0f || _aoeWarningRenderer == null)
                return;

            _aoeImpactRemaining -= Time.fixedDeltaTime;
            float t = 1.0f - Mathf.Clamp01(_aoeImpactRemaining / BOSS_AOE_IMPACT_LINGER_SECONDS);
            _aoeWarningRenderer.transform.localScale = Vector3.one * Mathf.Lerp(BOSS_AOE_RADIUS * 2.16f, BOSS_AOE_RADIUS * 2.36f, t);
            _aoeWarningRenderer.color = new Color(1.0f, 0.78f, 0.12f, Mathf.Lerp(0.62f, 0.0f, t));

            if (_aoeImpactRemaining <= 0.0f)
                HideBossAoeWarning();
        }

private void ResolveBossAoeWarningRenderer()
{
    if (_aoeWarningRenderer == null)
        _aoeWarningRenderer = transform.Find("BossAoeWarning")?.GetComponent<SpriteRenderer>();

    if (_aoeWarningRenderer == null)
        Debug.LogError($"[HungryGiantBehaviour] Missing authored BossAoeWarning SpriteRenderer on '{name}'.", this);
}

        private void HideBossAoeWarning()
        {
            if (_aoeWarningRenderer != null)
                _aoeWarningRenderer.enabled = false;
        }
    }
}
