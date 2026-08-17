using System.Collections.Generic;
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
            _monster?.UpdateExternalMoveFacing(_aoeCenter - new Vector2(transform.position.x, transform.position.y));
            ResolveBossAoeWarningRenderer();

            if (_aoeWarningRenderer == null)
                return;

            ShowBossAoeCircle(
                BOSS_AOE_RADIUS * 0.82f,
                new Color(1.0f, 0.18f, 0.05f, 0.52f),
                0.08f);
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
                float radius = Mathf.Lerp(BOSS_AOE_RADIUS * 0.82f, BOSS_AOE_RADIUS, progress);
                ShowBossAoeCircle(radius, new Color(1.0f, 0.18f, 0.05f, alpha), 0.08f);
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
            ShowBossAoeCircle(
                BOSS_AOE_RADIUS * 1.08f,
                new Color(1.0f, 0.78f, 0.12f, 0.62f),
                0.14f);
        }

        private void UpdateBossAoeImpact()
        {
            if (_aoeImpactRemaining <= 0.0f || _aoeWarningRenderer == null)
                return;

            _aoeImpactRemaining -= Time.fixedDeltaTime;
            float t = 1.0f - Mathf.Clamp01(_aoeImpactRemaining / BOSS_AOE_IMPACT_LINGER_SECONDS);
            ShowBossAoeCircle(
                Mathf.Lerp(BOSS_AOE_RADIUS * 1.08f, BOSS_AOE_RADIUS * 1.18f, t),
                new Color(1.0f, 0.78f, 0.12f, Mathf.Lerp(0.62f, 0.0f, t)),
                Mathf.Lerp(0.14f, 0.08f, t));

            if (_aoeImpactRemaining <= 0.0f)
                HideBossAoeWarning();
        }

        private void ResolveBossAoeWarningRenderer()
        {
            if (_aoeWarningRenderer == null)
                _aoeWarningRenderer = transform.Find("BossAoeWarning")?.GetComponent<SpriteRenderer>();

            if (_aoeWarningRenderer == null)
            {
                Debug.LogError($"[HungryGiantBehaviour] Missing authored BossAoeWarning SpriteRenderer on '{name}'.", this);
                return;
            }

            GameObject warningRoot = _aoeWarningRenderer.gameObject;
            if (warningRoot.activeSelf == false)
                warningRoot.SetActive(true);

            _aoeWarningRenderer.enabled = false;
            _aoeWarningLineRenderer ??= warningRoot.GetComponent<LineRenderer>();
            if (_aoeWarningLineRenderer == null)
                _aoeWarningLineRenderer = warningRoot.AddComponent<LineRenderer>();

            _aoeWarningLineRenderer.useWorldSpace = true;
            _aoeWarningLineRenderer.loop = true;
            _aoeWarningLineRenderer.positionCount = 48;
            _aoeWarningLineRenderer.numCornerVertices = 2;
            _aoeWarningLineRenderer.numCapVertices = 2;
            _aoeWarningLineRenderer.sharedMaterial = _aoeWarningRenderer.sharedMaterial;
            _aoeWarningLineRenderer.sortingLayerID = _aoeWarningRenderer.sortingLayerID;
            _aoeWarningLineRenderer.sortingOrder = BOSS_AOE_SORTING_ORDER;
        }

        private void ShowBossAoeCircle(float radius, Color color, float width)
        {
            ResolveBossAoeWarningRenderer();
            if (_aoeWarningLineRenderer == null)
                return;

            int pointCount = _aoeWarningLineRenderer.positionCount;
            float z = transform.position.z;
            for (int i = 0; i < pointCount; i++)
            {
                float angle = (Mathf.PI * 2.0f * i) / pointCount;
                _aoeWarningLineRenderer.SetPosition(
                    i,
                    new Vector3(
                        _aoeCenter.x + Mathf.Cos(angle) * radius,
                        _aoeCenter.y + Mathf.Sin(angle) * radius,
                        z));
            }

            _aoeWarningLineRenderer.startColor = color;
            _aoeWarningLineRenderer.endColor = color;
            _aoeWarningLineRenderer.widthMultiplier = width;
            _aoeWarningLineRenderer.enabled = true;
        }

        private void HideBossAoeWarning()
        {
            if (_aoeWarningLineRenderer != null)
                _aoeWarningLineRenderer.enabled = false;

            if (_aoeWarningRenderer != null)
            {
                _aoeWarningRenderer.enabled = false;
                if (_aoeWarningRenderer.gameObject.activeSelf)
                    _aoeWarningRenderer.gameObject.SetActive(false);
            }
        }
    }
}
