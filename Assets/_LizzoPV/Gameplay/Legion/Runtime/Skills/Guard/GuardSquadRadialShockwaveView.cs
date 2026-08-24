using System;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.P0.Skills.Guard
{
    public sealed class GuardSquadRadialShockwaveView : MonoBehaviour
    {
        private const string PrefabAddress = "GuardSquadRadialShockwave.prefab";

        private static readonly Color GuardCompleteLabelColor = new Color(0.35f, 1.0f, 1.0f, 1.0f);
        private static readonly Color GuardBreakthroughLabelColor = new Color(1.0f, 0.92f, 0.18f, 1.0f);
        private static int _nextCastId;

        private PartyService _party;
        private Transform _player;
        private SpriteRenderer _shockwaveRenderer;
        private GuardSquadRadialShockwaveCast _cast;
        private float _elapsed;

        public static int Activate(
            PartyService party,
            Transform player,
            string reason,
            SynergyData synergyData,
            SkillData skillData)
        {
            if (party == null)
                throw new ArgumentNullException(nameof(party));
            if (player == null)
                return 0;

            GameObject go = party.Factory.Spawn(PrefabAddress, pooled: true);
            if (go == null)
            {
                Debug.LogError($"[GuardSquadRadialShockwaveView] Authored prefab is not cached: {PrefabAddress}");
                return 0;
            }

            GuardSquadRadialShockwaveView shockwave = go.GetComponent<GuardSquadRadialShockwaveView>();
            if (shockwave == null)
            {
                Debug.LogError("[GuardSquadRadialShockwaveView] Authored component is missing.", go);
                party.Factory.Release(go);
                return 0;
            }

            shockwave.Init(party, player, reason, synergyData, skillData);
            return shockwave._cast.CastId;
        }

        public static float ResolveFirstActivationRadius(
            PartyService party,
            SynergyData synergyData,
            SkillData skillData)
        {
            return GuardSquadRadialShockwaveRules.ResolveFirstActivationRadius(
                party.GuardWallBonusMultiplier,
                synergyData,
                skillData);
        }

        private void Init(
            PartyService party,
            Transform player,
            string reason,
            SynergyData synergyData,
            SkillData skillData)
        {
            if (synergyData == null)
                throw new InvalidOperationException("P0 Guard Squad radial shockwave requires synergy data.");
            if (skillData == null)
                throw new InvalidOperationException("P0 Guard Squad radial shockwave requires skill data.");
            if (string.IsNullOrEmpty(synergyData.Id))
                throw new InvalidOperationException("P0 Guard Squad synergy data is missing id.");
            if (string.IsNullOrEmpty(skillData.Id))
                throw new InvalidOperationException("P0 Guard Squad skill data is missing id.");

            _party = party ?? throw new ArgumentNullException(nameof(party));
            _player = player;
            _elapsed = 0.0f;
            string resolvedReason = string.IsNullOrEmpty(reason) ? "manual" : reason;
            _cast = new GuardSquadRadialShockwaveCast(
                _party,
                resolvedReason,
                synergyData,
                skillData,
                ++_nextCastId);

            if (_cast.IsFirstActivationCast)
                P0PlaytestDiagnostics.LogEnemyAliveSnapshot("before_guard_first_cast");

            CreateVisuals();
            UpdateTransform();
            if (_cast.IsFirstActivationCast)
                ShowFirstActivationLabels();

            if (resolvedReason != "cooldown")
                RetroVfx.Spawn(RetroVfxKind.GuardSquadActivate, transform.position, Vector3.up, 1.0f);

            _cast.LogCast();
            _cast.Apply(transform.position, recordSkillCast: true);
        }

        private void CreateVisuals()
        {
            _shockwaveRenderer = transform.Find("ShockwaveRange")?.GetComponent<SpriteRenderer>();
            if (_shockwaveRenderer == null)
            {
                Debug.LogError("[GuardSquadRadialShockwaveView] Authored ShockwaveRange renderer is missing.", this);
                return;
            }

            _shockwaveRenderer.color = new Color(0.12f, 0.95f, 1.0f, 0.22f);
            _shockwaveRenderer.sortingOrder = SortingOrder.GroundEffect;
            _shockwaveRenderer.transform.localScale = Vector3.one * (_cast.Radius * 2.0f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            UpdateTransform();
            RefreshVisuals();
            if (_elapsed < _cast.Duration)
                return;

            _cast.LogSummary();
            _party.Factory.Release(gameObject);
        }

        private void UpdateTransform()
        {
            if (_player != null)
                transform.position = _player.position;
        }

        private void RefreshVisuals()
        {
            float progress = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, _cast.Duration));
            float pulse = 0.9f + Mathf.Sin(_elapsed * 28.0f) * 0.1f;
            if (_shockwaveRenderer != null)
                SetAlpha(_shockwaveRenderer, Mathf.Lerp(0.22f, 0.0f, progress) * pulse);
        }

        private static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            if (renderer == null)
                return;

            Color color = renderer.color;
            color.a = Mathf.Clamp01(alpha);
            renderer.color = color;
        }

        private void ShowFirstActivationLabels()
        {
            FloatingDamageText.ShowLabel(
                transform.position + Vector3.up * 0.72f,
                "근위대 결성!",
                GuardCompleteLabelColor,
                large: true,
                lifeTime: 0.7f);
            FloatingDamageText.ShowLabel(
                transform.position + Vector3.up * 0.34f,
                "방패 진형 전개!",
                GuardBreakthroughLabelColor,
                large: true,
                lifeTime: 0.7f);
        }

        private void OnDestroy()
        {
            if (Application.isPlaying && _elapsed > 0.0f)
                _cast?.LogSummary();
        }
    }
}
