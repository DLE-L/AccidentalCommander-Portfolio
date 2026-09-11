using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
    public enum EnemyAreaSignal { WarningStarted, WarningProgress, Impact, Tick, Hidden }
    public sealed class EnemyAreaAttack : MonoBehaviour
    {
        private const float BOSS_AOE_RADIUS = 1.65f;
        private const float BOSS_AOE_WARNING_BONUS_SECONDS = .35f;
        public const string BossAoePatternId = CombatIds.BossAoeSlam;
        [SerializeField] private SpriteRenderer _aoeWarningRenderer;
        private EnemyActor _actor;
        private Vector2 _aoeCenter;
        private float _bossWarningTime;
        private int _bossAttack;
        private EnemyAreaPresentation _presentation;
        public event System.Action<EnemyAreaSignal, Vector2, float> PresentationChanged;
        private bool _isAoeDamageFrame;
        public bool IsDamageFrame => _isAoeDamageFrame;
        public void Setup(EnemyActor actor)
        {
            _actor = actor;
            _areaAttack = new CombatAreaAttack(actor.Registry, actor.ImmediateHits);
            _bossWarningTime = actor.Tuning.Boss1WarningTime;
            _bossAttack = actor.Tuning.Boss1Atk;
            _presentation?.Dispose();
            _presentation = new EnemyAreaPresentation(this, _aoeWarningRenderer, BOSS_AOE_RADIUS);
            ResetAttack();
        }
        public void ResetAttack() { _isAoeDamageFrame = false; _areaAttack?.Reset(); HideBossAoeWarning(); }
        private void OnDisable() { ResetAttack(); _presentation?.Dispose(); _presentation = null; _actor = null; _areaAttack = null; }
        private CombatAreaAttack _areaAttack;
        internal void BeginBossAoeWarning(Vector2 center)
        {
            // The shared renderer now belongs to this warning, not the previous impact.
			_actor?.SetExternalAttackPreparationLocked(true);
            _aoeCenter = center;
            float warningDuration = Mathf.Clamp(_bossWarningTime + BOSS_AOE_WARNING_BONUS_SECONDS, 1.25f, 1.45f);


            PresentationChanged?.Invoke(EnemyAreaSignal.WarningStarted, center, 0f);
            CombatRuntimeDiagnostics.Log(
                "boss_telegraph",
                CombatRuntimeDiagnostics.Text("boss_id", CombatIds.BossHungryGiant),
                CombatRuntimeDiagnostics.Text("attack_type", "aoe"),
                CombatRuntimeDiagnostics.Float("warning_seconds", warningDuration),
                CombatRuntimeDiagnostics.Text("geometry", "circle"),
                CombatRuntimeDiagnostics.Float("range", BOSS_AOE_RADIUS),
                CombatRuntimeDiagnostics.Int("damage", _bossAttack));
            RunTelemetry.Log(
                RunTelemetry.BossPatternWarningShow,
                $"source_id={CombatIds.BossHungryGiant}",
                $"pattern_id={BossAoePatternId}",
                $"warning={warningDuration:0.##}",
                $"radius={BOSS_AOE_RADIUS:0.##}");
        }

        internal void PresentBossAoeWarning(float progress) => PresentationChanged?.Invoke(EnemyAreaSignal.WarningProgress, _aoeCenter, progress);

        internal void ApplyBossAoeDamageFrame()
        {
            _isAoeDamageFrame = true;
            int damage = _bossAttack;

            var targets = _areaAttack.SelectTargets(CombatImmediateHitFaction.Enemy, _aoeCenter, BOSS_AOE_RADIUS);
            if (targets.Count > 0)
            {
                CommanderActor player = (CommanderActor)targets[0];
                Vector2 direction = ((Vector2)player.transform.position - _aoeCenter).normalized;
                CombatImmediateHitRequest request = CombatImmediateHitRequest.CreateEnemyContact(
                    _actor.GetDamageEnemyId(),
                    player,
                    _aoeCenter,
                    direction,
                    damage,
                    BossAoePatternId,
                    RetroVfxKind.PlayerDamaged, source: _actor);
                _areaAttack.TryApply(0, in request);
            }

            _isAoeDamageFrame = false;
            ShowBossAoeImpact();

            CombatRuntimeDiagnostics.Log(
                "boss_attack_resolved",
                CombatRuntimeDiagnostics.Text("boss_id", CombatIds.BossHungryGiant),
                CombatRuntimeDiagnostics.Text("attack_type", "aoe"),
                CombatRuntimeDiagnostics.Bool("resolved", true),
                CombatRuntimeDiagnostics.Text("affected_count", "unavailable"));
        }

        private void ShowBossAoeImpact() => PresentationChanged?.Invoke(EnemyAreaSignal.Impact, _aoeCenter, 0f);
        internal void UpdateBossAoeImpact() => PresentationChanged?.Invoke(EnemyAreaSignal.Tick, _aoeCenter, Time.fixedDeltaTime);
        internal void HideBossAoeWarning() => PresentationChanged?.Invoke(EnemyAreaSignal.Hidden, _aoeCenter, 0f);
    }
}