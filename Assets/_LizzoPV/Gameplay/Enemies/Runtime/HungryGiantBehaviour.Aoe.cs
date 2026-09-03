using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Config;
using Lizzo.PV.Gameplay.Diagnostics;
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

            if (_aoeWarningRenderer.gameObject.activeSelf == false)
                _aoeWarningRenderer.gameObject.SetActive(true);

            _aoeWarningRenderer.transform.position = new Vector3(_aoeCenter.x, _aoeCenter.y, transform.position.z);
            SetBossAoeWarningDiameter(BOSS_AOE_RADIUS * 1.64f);
            _aoeWarningRenderer.color = new Color(1.0f, 0.18f, 0.05f, 0.52f);
            _aoeWarningRenderer.enabled = true;
            Build1RuntimeDiagnostics.Log(
                "boss_telegraph",
                Build1RuntimeDiagnostics.Text("boss_id", CombatIds.BossHungryGiant),
                Build1RuntimeDiagnostics.Text("attack_type", "aoe"),
                Build1RuntimeDiagnostics.Float("warning_seconds", _aoeWarningDuration),
                Build1RuntimeDiagnostics.Text("geometry", "circle"),
                Build1RuntimeDiagnostics.Float("range", BOSS_AOE_RADIUS),
                Build1RuntimeDiagnostics.Int("damage", RemoteConfig.Boss1Atk));
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
                SetBossAoeWarningDiameter(diameter);
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
            RetroVfx.Spawn(RetroVfxKind.BossAoeImpact, new Vector3(_aoeCenter.x, _aoeCenter.y, transform.position.z), Vector3.zero, 1.0f);

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

            _isAoeDamageFrame = false;
            ShowBossAoeImpact();
            BeginBossStagger(BossAoePatternId);
            Build1RuntimeDiagnostics.Log(
                "boss_attack_resolved",
                Build1RuntimeDiagnostics.Text("boss_id", CombatIds.BossHungryGiant),
                Build1RuntimeDiagnostics.Text("attack_type", "aoe"),
                Build1RuntimeDiagnostics.Bool("resolved", true),
                Build1RuntimeDiagnostics.Text("affected_count", "unavailable"));
        }

        private void ShowBossAoeImpact()
        {
            if (_aoeWarningRenderer == null)
                return;

            _aoeImpactRemaining = BOSS_AOE_IMPACT_LINGER_SECONDS;
            _aoeWarningRenderer.transform.position = new Vector3(_aoeCenter.x, _aoeCenter.y, transform.position.z);
            SetBossAoeWarningDiameter(BOSS_AOE_RADIUS * 2.16f);
            _aoeWarningRenderer.color = new Color(1.0f, 0.78f, 0.12f, 0.62f);
            _aoeWarningRenderer.enabled = true;
        }

        private void UpdateBossAoeImpact()
        {
            if (_aoeImpactRemaining <= 0.0f || _aoeWarningRenderer == null)
                return;

            _aoeImpactRemaining -= Time.fixedDeltaTime;
            float t = 1.0f - Mathf.Clamp01(_aoeImpactRemaining / BOSS_AOE_IMPACT_LINGER_SECONDS);
            SetBossAoeWarningDiameter(Mathf.Lerp(BOSS_AOE_RADIUS * 2.16f, BOSS_AOE_RADIUS * 2.36f, t));
            _aoeWarningRenderer.color = new Color(1.0f, 0.78f, 0.12f, Mathf.Lerp(0.62f, 0.0f, t));

            if (_aoeImpactRemaining <= 0.0f)
                HideBossAoeWarning();
        }

        private void SetBossAoeWarningDiameter(float diameter)
        {
            if (_aoeWarningRenderer == null)
                return;

            Vector2 spriteSize = _aoeWarningRenderer.sprite == null
                ? Vector2.one
                : _aoeWarningRenderer.sprite.bounds.size;
            float nativeDiameter = Mathf.Max(0.0001f, Mathf.Max(spriteSize.x, spriteSize.y));
            _aoeWarningRenderer.transform.localScale = Vector3.one * (diameter / nativeDiameter);
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
