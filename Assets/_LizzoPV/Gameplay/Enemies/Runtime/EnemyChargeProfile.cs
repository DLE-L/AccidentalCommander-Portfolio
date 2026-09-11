using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
    // Authored differences between charge enemies; execution belongs to EnemyChargeController.
    public sealed class EnemyChargeProfile : ScriptableObject
    {
        [SerializeField] private string _enemyDataId;
        [SerializeField] private string _patternId;
        [SerializeField] private string _impactGracePatternId;
        [SerializeField] private bool _contactInterruptsCharge;
        [SerializeField] private bool _allowCancellation;
        [SerializeField] private bool _useEnemyChargeTiming;
        [SerializeField] private bool _initialCooldown;
        [SerializeField] private bool _growWarning;
        [SerializeField] private bool _chargeFeedback;
        [SerializeField] private float _speed;
        [SerializeField] private float _warningSeconds;
        [SerializeField] private float _durationSeconds;
        [SerializeField] private float _cooldownSeconds;
        [SerializeField] private float _minimumRange;
        [SerializeField] private float _hitRadius;
        [SerializeField] private float _impactGraceSeconds;
        [SerializeField] private Color _warningColor;
        [SerializeField] private Color _chargeColor;
        [SerializeField] private Color _pathColor;
        public string EnemyDataId => _enemyDataId;
        public string PatternId => _patternId;
        public string ImpactGracePatternId => _impactGracePatternId;
        public bool ContactInterruptsCharge => _contactInterruptsCharge;
        public bool AllowCancellation => _allowCancellation;
        public bool UseEnemyChargeTiming => _useEnemyChargeTiming;
        public bool InitialCooldown => _initialCooldown;
        public bool GrowWarning => _growWarning;
        public bool ChargeFeedback => _chargeFeedback;
        public float Speed => _speed;
        public float WarningSeconds => _warningSeconds;
        public float DurationSeconds => _durationSeconds;
        public float CooldownSeconds => _cooldownSeconds;
        public float MinimumRange => _minimumRange;
        public float HitRadius => _hitRadius;
        public float ImpactGraceSeconds => _impactGraceSeconds;
        public Color WarningColor => _warningColor;
        public Color ChargeColor => _chargeColor;
        public Color PathColor => _pathColor;
    }
}
