using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/World UI Feedback Profile", fileName = "WorldUiFeedbackProfile")]
    public sealed class WorldUiFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private SpriteAssetId _enemyHealthBarBackSpriteId; [SerializeField] private SpriteAssetId _enemyHealthBarFillSpriteId;
        [SerializeField] private MotionAssetId _healthBarEnterMotionId; [SerializeField] private MotionAssetId _healthBarExitMotionId;
        [SerializeField] private MotionAssetId _damageNumberMotionId; [SerializeField] private MotionAssetId _healNumberMotionId;
        [SerializeField] private ColorRole _enemyDamageColorRole; [SerializeField] private ColorRole _commanderDamageColorRole; [SerializeField] private ColorRole _healColorRole;
        [SerializeField, Min(0f)] private float _consecutiveDamageMergeWindowSeconds; [SerializeField, Min(0f)] private float _enemyHealthBarVisibleSeconds;
        public SpriteAssetId EnemyHealthBarBackSpriteId=>_enemyHealthBarBackSpriteId; public SpriteAssetId EnemyHealthBarFillSpriteId=>_enemyHealthBarFillSpriteId;
        public MotionAssetId HealthBarEnterMotionId=>_healthBarEnterMotionId; public MotionAssetId HealthBarExitMotionId=>_healthBarExitMotionId;
        public MotionAssetId DamageNumberMotionId=>_damageNumberMotionId; public MotionAssetId HealNumberMotionId=>_healNumberMotionId;
        public ColorRole EnemyDamageColorRole=>_enemyDamageColorRole; public ColorRole CommanderDamageColorRole=>_commanderDamageColorRole; public ColorRole HealColorRole=>_healColorRole;
        public float ConsecutiveDamageMergeWindowSeconds=>_consecutiveDamageMergeWindowSeconds; public float EnemyHealthBarVisibleSeconds=>_enemyHealthBarVisibleSeconds;
        public bool TryValidate(out string issue)
        {
            if(!WorldFeedbackCoreValidation.Require(_enemyHealthBarBackSpriteId,nameof(EnemyHealthBarBackSpriteId),out issue)||!WorldFeedbackCoreValidation.Require(_enemyHealthBarFillSpriteId,nameof(EnemyHealthBarFillSpriteId),out issue)
              ||!WorldFeedbackCoreValidation.Require(_healthBarEnterMotionId,nameof(HealthBarEnterMotionId),out issue)||!WorldFeedbackCoreValidation.Require(_healthBarExitMotionId,nameof(HealthBarExitMotionId),out issue)
              ||!WorldFeedbackCoreValidation.Require(_damageNumberMotionId,nameof(DamageNumberMotionId),out issue)||!WorldFeedbackCoreValidation.Require(_healNumberMotionId,nameof(HealNumberMotionId),out issue)
              ||!WorldFeedbackCoreValidation.Require(_enemyDamageColorRole,nameof(EnemyDamageColorRole),out issue)||!WorldFeedbackCoreValidation.Require(_commanderDamageColorRole,nameof(CommanderDamageColorRole),out issue)
              ||!WorldFeedbackCoreValidation.Require(_healColorRole,nameof(HealColorRole),out issue)) return false;
            if(_consecutiveDamageMergeWindowSeconds<=0f||_enemyHealthBarVisibleSeconds<=0f){issue="World UI feedback timing values must be greater than zero.";return false;} issue=string.Empty;return true;
        }
#if UNITY_EDITOR
        public void SetForEditor(SpriteAssetId back, SpriteAssetId fill, MotionAssetId enter, MotionAssetId exit, MotionAssetId damage, MotionAssetId heal,
            ColorRole enemyDamage, ColorRole commanderDamage, ColorRole healColor, float mergeWindow, float healthVisible)
        { _enemyHealthBarBackSpriteId=back;_enemyHealthBarFillSpriteId=fill;_healthBarEnterMotionId=enter;_healthBarExitMotionId=exit;_damageNumberMotionId=damage;_healNumberMotionId=heal;
          _enemyDamageColorRole=enemyDamage;_commanderDamageColorRole=commanderDamage;_healColorRole=healColor;_consecutiveDamageMergeWindowSeconds=mergeWindow;_enemyHealthBarVisibleSeconds=healthVisible; }
#endif
    }
}
