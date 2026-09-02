using System;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/Commander Feedback Profile", fileName = "CommanderWorldFeedbackProfile")]
    public sealed class CommanderWorldFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private VfxAssetId _healVfxId;
        [SerializeField] private AudioAssetId _healSfxId;
        [SerializeField] private SpriteAssetId _lowHealthOverlaySpriteId;
        [SerializeField] private MotionAssetId _lowHealthEnterMotionId;
        [SerializeField] private MotionAssetId _lowHealthLoopMotionId;
        [SerializeField] private MotionAssetId _lowHealthExitMotionId;
        [SerializeField] private AudioAssetId _lowHealthEnterSfxId;
        [SerializeField] private AudioAssetId _lowHealthLoopSfxId;
        [SerializeField] private AudioAssetId _lowHealthExitSfxId;

        public VfxAssetId HealVfxId => _healVfxId;
        public AudioAssetId HealSfxId => _healSfxId;
        public SpriteAssetId LowHealthOverlaySpriteId => _lowHealthOverlaySpriteId;
        public MotionAssetId LowHealthEnterMotionId => _lowHealthEnterMotionId;
        public MotionAssetId LowHealthLoopMotionId => _lowHealthLoopMotionId;
        public MotionAssetId LowHealthExitMotionId => _lowHealthExitMotionId;
        public AudioAssetId LowHealthEnterSfxId => _lowHealthEnterSfxId;
        public AudioAssetId LowHealthLoopSfxId => _lowHealthLoopSfxId;
        public AudioAssetId LowHealthExitSfxId => _lowHealthExitSfxId;

        public bool TryValidate(out string issue)
        {
            return WorldFeedbackCoreValidation.Require(_healVfxId, nameof(HealVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_healSfxId, nameof(HealSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_lowHealthEnterMotionId, nameof(LowHealthEnterMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_lowHealthLoopMotionId, nameof(LowHealthLoopMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_lowHealthExitMotionId, nameof(LowHealthExitMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_lowHealthEnterSfxId, nameof(LowHealthEnterSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_lowHealthLoopSfxId, nameof(LowHealthLoopSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_lowHealthExitSfxId, nameof(LowHealthExitSfxId), out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            VfxAssetId healVfxId,
            AudioAssetId healSfxId,
            SpriteAssetId lowHealthOverlaySpriteId,
            MotionAssetId lowHealthEnterMotionId,
            MotionAssetId lowHealthLoopMotionId,
            MotionAssetId lowHealthExitMotionId,
            AudioAssetId lowHealthEnterSfxId,
            AudioAssetId lowHealthLoopSfxId,
            AudioAssetId lowHealthExitSfxId)
        {
            _healVfxId = healVfxId;
            _healSfxId = healSfxId;
            _lowHealthOverlaySpriteId = lowHealthOverlaySpriteId;
            _lowHealthEnterMotionId = lowHealthEnterMotionId;
            _lowHealthLoopMotionId = lowHealthLoopMotionId;
            _lowHealthExitMotionId = lowHealthExitMotionId;
            _lowHealthEnterSfxId = lowHealthEnterSfxId;
            _lowHealthLoopSfxId = lowHealthLoopSfxId;
            _lowHealthExitSfxId = lowHealthExitSfxId;
        }
#endif
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/Companion Lifecycle Feedback Profile", fileName = "CompanionLifecycleFeedbackProfile")]
    public sealed class CompanionLifecycleFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private MotionAssetId _joinMotionId;
        [SerializeField] private VfxAssetId _joinVfxId;
        [SerializeField] private AudioAssetId _joinSfxId;
        [SerializeField] private MotionAssetId _promoteMotionId;
        [SerializeField] private VfxAssetId _promoteVfxId;
        [SerializeField] private AudioAssetId _promoteSfxId;

        public MotionAssetId JoinMotionId => _joinMotionId;
        public VfxAssetId JoinVfxId => _joinVfxId;
        public AudioAssetId JoinSfxId => _joinSfxId;
        public MotionAssetId PromoteMotionId => _promoteMotionId;
        public VfxAssetId PromoteVfxId => _promoteVfxId;
        public AudioAssetId PromoteSfxId => _promoteSfxId;

        public bool TryValidate(out string issue)
        {
            return WorldFeedbackCoreValidation.Require(_joinMotionId, nameof(JoinMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_joinVfxId, nameof(JoinVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_joinSfxId, nameof(JoinSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_promoteMotionId, nameof(PromoteMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_promoteVfxId, nameof(PromoteVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_promoteSfxId, nameof(PromoteSfxId), out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            MotionAssetId joinMotionId,
            VfxAssetId joinVfxId,
            AudioAssetId joinSfxId,
            MotionAssetId promoteMotionId,
            VfxAssetId promoteVfxId,
            AudioAssetId promoteSfxId)
        {
            _joinMotionId = joinMotionId;
            _joinVfxId = joinVfxId;
            _joinSfxId = joinSfxId;
            _promoteMotionId = promoteMotionId;
            _promoteVfxId = promoteVfxId;
            _promoteSfxId = promoteSfxId;
        }
#endif
    }

    [Serializable]
    public struct CompanionLifecycleFeedbackBinding
    {
        [SerializeField] private CompanionId _companionId;
        [SerializeField] private CompanionLifecycleFeedbackProfileSO _profile;

        public CompanionLifecycleFeedbackBinding(CompanionId companionId, CompanionLifecycleFeedbackProfileSO profile)
        {
            _companionId = companionId;
            _profile = profile;
        }

        public CompanionId CompanionId => _companionId;
        public CompanionLifecycleFeedbackProfileSO Profile => _profile;
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/World UI Feedback Profile", fileName = "WorldUiFeedbackProfile")]
    public sealed class WorldUiFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _enemyHealthBarBackSpriteId;
        [SerializeField] private SpriteAssetId _enemyHealthBarFillSpriteId;
        [SerializeField] private MotionAssetId _healthBarEnterMotionId;
        [SerializeField] private MotionAssetId _healthBarExitMotionId;
        [SerializeField] private MotionAssetId _damageNumberMotionId;
        [SerializeField] private MotionAssetId _healNumberMotionId;
        [SerializeField] private ColorRole _enemyDamageColorRole;
        [SerializeField] private ColorRole _commanderDamageColorRole;
        [SerializeField] private ColorRole _healColorRole;
        [SerializeField, Min(0f)] private float _consecutiveDamageMergeWindowSeconds;
        [SerializeField, Min(0f)] private float _enemyHealthBarVisibleSeconds;

        public SpriteAssetId EnemyHealthBarBackSpriteId => _enemyHealthBarBackSpriteId;
        public SpriteAssetId EnemyHealthBarFillSpriteId => _enemyHealthBarFillSpriteId;
        public MotionAssetId HealthBarEnterMotionId => _healthBarEnterMotionId;
        public MotionAssetId HealthBarExitMotionId => _healthBarExitMotionId;
        public MotionAssetId DamageNumberMotionId => _damageNumberMotionId;
        public MotionAssetId HealNumberMotionId => _healNumberMotionId;
        public ColorRole EnemyDamageColorRole => _enemyDamageColorRole;
        public ColorRole CommanderDamageColorRole => _commanderDamageColorRole;
        public ColorRole HealColorRole => _healColorRole;
        public float ConsecutiveDamageMergeWindowSeconds => _consecutiveDamageMergeWindowSeconds;
        public float EnemyHealthBarVisibleSeconds => _enemyHealthBarVisibleSeconds;

        public bool TryValidate(out string issue)
        {
            if (!WorldFeedbackCoreValidation.Require(_enemyHealthBarBackSpriteId, nameof(EnemyHealthBarBackSpriteId), out issue)
                || !WorldFeedbackCoreValidation.Require(_enemyHealthBarFillSpriteId, nameof(EnemyHealthBarFillSpriteId), out issue)
                || !WorldFeedbackCoreValidation.Require(_healthBarEnterMotionId, nameof(HealthBarEnterMotionId), out issue)
                || !WorldFeedbackCoreValidation.Require(_healthBarExitMotionId, nameof(HealthBarExitMotionId), out issue)
                || !WorldFeedbackCoreValidation.Require(_damageNumberMotionId, nameof(DamageNumberMotionId), out issue)
                || !WorldFeedbackCoreValidation.Require(_healNumberMotionId, nameof(HealNumberMotionId), out issue)
                || !WorldFeedbackCoreValidation.Require(_enemyDamageColorRole, nameof(EnemyDamageColorRole), out issue)
                || !WorldFeedbackCoreValidation.Require(_commanderDamageColorRole, nameof(CommanderDamageColorRole), out issue)
                || !WorldFeedbackCoreValidation.Require(_healColorRole, nameof(HealColorRole), out issue))
            {
                return false;
            }

            if (_consecutiveDamageMergeWindowSeconds <= 0f || _enemyHealthBarVisibleSeconds <= 0f)
            {
                issue = "World UI feedback timing values must be greater than zero.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(
            SpriteAssetId enemyHealthBarBackSpriteId,
            SpriteAssetId enemyHealthBarFillSpriteId,
            MotionAssetId healthBarEnterMotionId,
            MotionAssetId healthBarExitMotionId,
            MotionAssetId damageNumberMotionId,
            MotionAssetId healNumberMotionId,
            ColorRole enemyDamageColorRole,
            ColorRole commanderDamageColorRole,
            ColorRole healColorRole,
            float consecutiveDamageMergeWindowSeconds,
            float enemyHealthBarVisibleSeconds)
        {
            _enemyHealthBarBackSpriteId = enemyHealthBarBackSpriteId;
            _enemyHealthBarFillSpriteId = enemyHealthBarFillSpriteId;
            _healthBarEnterMotionId = healthBarEnterMotionId;
            _healthBarExitMotionId = healthBarExitMotionId;
            _damageNumberMotionId = damageNumberMotionId;
            _healNumberMotionId = healNumberMotionId;
            _enemyDamageColorRole = enemyDamageColorRole;
            _commanderDamageColorRole = commanderDamageColorRole;
            _healColorRole = healColorRole;
            _consecutiveDamageMergeWindowSeconds = consecutiveDamageMergeWindowSeconds;
            _enemyHealthBarVisibleSeconds = enemyHealthBarVisibleSeconds;
        }
#endif
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/Combat Impact Feedback Profile", fileName = "CombatImpactFeedbackProfile")]
    public sealed class CombatImpactFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private HitStopGrade _hitStopGrade;
        [SerializeField] private MotionAssetId _cameraMotionId;
        [SerializeField] private MotionAssetId _screenFeedbackMotionId;
        [SerializeField] private SpriteAssetId _screenOverlaySpriteId;
        [SerializeField] private AudioAssetId _impactSfxId;

        public HitStopGrade HitStopGrade => _hitStopGrade;
        public MotionAssetId CameraMotionId => _cameraMotionId;
        public MotionAssetId ScreenFeedbackMotionId => _screenFeedbackMotionId;
        public SpriteAssetId ScreenOverlaySpriteId => _screenOverlaySpriteId;
        public AudioAssetId ImpactSfxId => _impactSfxId;

        public bool TryValidate(out string issue)
        {
            return WorldFeedbackCoreValidation.Require(_impactSfxId, nameof(ImpactSfxId), out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            HitStopGrade hitStopGrade,
            MotionAssetId cameraMotionId,
            MotionAssetId screenFeedbackMotionId,
            SpriteAssetId screenOverlaySpriteId,
            AudioAssetId impactSfxId)
        {
            _hitStopGrade = hitStopGrade;
            _cameraMotionId = cameraMotionId;
            _screenFeedbackMotionId = screenFeedbackMotionId;
            _screenOverlaySpriteId = screenOverlaySpriteId;
            _impactSfxId = impactSfxId;
        }
#endif
    }

    [Serializable]
    public struct CombatImpactFeedbackBinding
    {
        [SerializeField] private CombatImpactKind _impactKind;
        [SerializeField] private CombatImpactFeedbackProfileSO _profile;

        public CombatImpactFeedbackBinding(CombatImpactKind impactKind, CombatImpactFeedbackProfileSO profile)
        {
            _impactKind = impactKind;
            _profile = profile;
        }

        public CombatImpactKind ImpactKind => _impactKind;
        public CombatImpactFeedbackProfileSO Profile => _profile;
    }

    internal static class WorldFeedbackCoreValidation
    {
        public static bool Require(SpriteAssetId id, string fieldName, out string issue) =>
            Require(id.Value, "Sprite", fieldName, out issue);

        public static bool Require(AudioAssetId id, string fieldName, out string issue) =>
            Require(id.Value, "Audio", fieldName, out issue);

        public static bool Require(VfxAssetId id, string fieldName, out string issue) =>
            Require(id.Value, "VFX", fieldName, out issue);

        public static bool Require(MotionAssetId id, string fieldName, out string issue) =>
            Require(id.Value, "Motion", fieldName, out issue);

        public static bool Require(ColorRole role, string fieldName, out string issue)
        {
            if (role.IsNone)
            {
                issue = $"Required ColorRole {fieldName} cannot be empty.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static bool Require(int value, string assetKind, string fieldName, out string issue)
        {
            if (value <= 0)
            {
                issue = $"Required {assetKind} Asset ID {fieldName} must be positive.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}
