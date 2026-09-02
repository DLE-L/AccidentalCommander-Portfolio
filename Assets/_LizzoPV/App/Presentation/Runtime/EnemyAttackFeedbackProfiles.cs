using System;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [Serializable]
    public struct WindupFeedback
    {
        [SerializeField] private MotionAssetId _windupMotionId;
        [SerializeField] private VfxAssetId _windupVfxId;
        [SerializeField] private AudioAssetId _windupSfxId;

        public WindupFeedback(
            MotionAssetId windupMotionId,
            VfxAssetId windupVfxId,
            AudioAssetId windupSfxId)
        {
            _windupMotionId = windupMotionId;
            _windupVfxId = windupVfxId;
            _windupSfxId = windupSfxId;
        }

        public MotionAssetId WindupMotionId => _windupMotionId;
        public VfxAssetId WindupVfxId => _windupVfxId;
        public AudioAssetId WindupSfxId => _windupSfxId;
        public bool IsConfigured => !_windupMotionId.IsNone || !_windupVfxId.IsNone || !_windupSfxId.IsNone;

        public bool TryValidate(out string issue)
        {
            if (!IsConfigured)
            {
                issue = string.Empty;
                return true;
            }

            return WorldFeedbackCoreValidation.Require(_windupMotionId, nameof(WindupMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_windupVfxId, nameof(WindupVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_windupSfxId, nameof(WindupSfxId), out issue);
        }
    }

    [Serializable]
    public struct TelegraphFeedback
    {
        [SerializeField] private VfxAssetId _telegraphVfxId;
        [SerializeField] private MotionAssetId _enterMotionId;
        [SerializeField] private MotionAssetId _loopMotionId;
        [SerializeField] private MotionAssetId _activateMotionId;
        [SerializeField] private MotionAssetId _cancelMotionId;
        [SerializeField] private AudioAssetId _loopSfxId;
        [SerializeField] private AudioAssetId _activateSfxId;

        public TelegraphFeedback(
            VfxAssetId telegraphVfxId,
            MotionAssetId enterMotionId,
            MotionAssetId loopMotionId,
            MotionAssetId activateMotionId,
            MotionAssetId cancelMotionId,
            AudioAssetId loopSfxId,
            AudioAssetId activateSfxId)
        {
            _telegraphVfxId = telegraphVfxId;
            _enterMotionId = enterMotionId;
            _loopMotionId = loopMotionId;
            _activateMotionId = activateMotionId;
            _cancelMotionId = cancelMotionId;
            _loopSfxId = loopSfxId;
            _activateSfxId = activateSfxId;
        }

        public VfxAssetId TelegraphVfxId => _telegraphVfxId;
        public MotionAssetId EnterMotionId => _enterMotionId;
        public MotionAssetId LoopMotionId => _loopMotionId;
        public MotionAssetId ActivateMotionId => _activateMotionId;
        public MotionAssetId CancelMotionId => _cancelMotionId;
        public AudioAssetId LoopSfxId => _loopSfxId;
        public AudioAssetId ActivateSfxId => _activateSfxId;
        public bool IsConfigured => !_telegraphVfxId.IsNone || !_enterMotionId.IsNone || !_loopMotionId.IsNone
                                    || !_activateMotionId.IsNone || !_cancelMotionId.IsNone
                                    || !_loopSfxId.IsNone || !_activateSfxId.IsNone;

        public bool TryValidate(out string issue)
        {
            if (!IsConfigured)
            {
                issue = string.Empty;
                return true;
            }

            return WorldFeedbackCoreValidation.Require(_telegraphVfxId, nameof(TelegraphVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_enterMotionId, nameof(EnterMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_loopMotionId, nameof(LoopMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_activateMotionId, nameof(ActivateMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_cancelMotionId, nameof(CancelMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_loopSfxId, nameof(LoopSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_activateSfxId, nameof(ActivateSfxId), out issue);
        }
    }

    [Serializable]
    public struct EnemyProjectileFeedback
    {
        [SerializeField] private SpriteAssetId _projectileSpriteId;
        [SerializeField] private VfxAssetId _spawnVfxId;
        [SerializeField] private VfxAssetId _trailVfxId;
        [SerializeField] private AudioAssetId _launchSfxId;
        [SerializeField] private AudioAssetId _travelLoopSfxId;
        [SerializeField] private VfxAssetId _lifetimeEndVfxId;
        [SerializeField] private AudioAssetId _lifetimeEndSfxId;

        public EnemyProjectileFeedback(
            SpriteAssetId projectileSpriteId,
            VfxAssetId spawnVfxId,
            VfxAssetId trailVfxId,
            AudioAssetId launchSfxId,
            AudioAssetId travelLoopSfxId,
            VfxAssetId lifetimeEndVfxId,
            AudioAssetId lifetimeEndSfxId)
        {
            _projectileSpriteId = projectileSpriteId;
            _spawnVfxId = spawnVfxId;
            _trailVfxId = trailVfxId;
            _launchSfxId = launchSfxId;
            _travelLoopSfxId = travelLoopSfxId;
            _lifetimeEndVfxId = lifetimeEndVfxId;
            _lifetimeEndSfxId = lifetimeEndSfxId;
        }

        public SpriteAssetId ProjectileSpriteId => _projectileSpriteId;
        public VfxAssetId SpawnVfxId => _spawnVfxId;
        public VfxAssetId TrailVfxId => _trailVfxId;
        public AudioAssetId LaunchSfxId => _launchSfxId;
        public AudioAssetId TravelLoopSfxId => _travelLoopSfxId;
        public VfxAssetId LifetimeEndVfxId => _lifetimeEndVfxId;
        public AudioAssetId LifetimeEndSfxId => _lifetimeEndSfxId;
        public bool IsConfigured => !_projectileSpriteId.IsNone || !_spawnVfxId.IsNone || !_trailVfxId.IsNone
                                    || !_launchSfxId.IsNone || !_travelLoopSfxId.IsNone
                                    || !_lifetimeEndVfxId.IsNone || !_lifetimeEndSfxId.IsNone;

        public bool TryValidate(out string issue)
        {
            if (!IsConfigured)
            {
                issue = string.Empty;
                return true;
            }

            return WorldFeedbackCoreValidation.Require(_projectileSpriteId, nameof(ProjectileSpriteId), out issue)
                   && WorldFeedbackCoreValidation.Require(_spawnVfxId, nameof(SpawnVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_trailVfxId, nameof(TrailVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_launchSfxId, nameof(LaunchSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_travelLoopSfxId, nameof(TravelLoopSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_lifetimeEndVfxId, nameof(LifetimeEndVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_lifetimeEndSfxId, nameof(LifetimeEndSfxId), out issue);
        }
    }

    [Serializable]
    public struct EnemyImpactFeedback
    {
        [SerializeField] private VfxAssetId _impactVfxId;
        [SerializeField] private CombatImpactKind _globalImpactKind;

        public EnemyImpactFeedback(VfxAssetId impactVfxId, CombatImpactKind globalImpactKind)
        {
            _impactVfxId = impactVfxId;
            _globalImpactKind = globalImpactKind;
        }

        public VfxAssetId ImpactVfxId => _impactVfxId;
        public CombatImpactKind GlobalImpactKind => _globalImpactKind;
        public bool IsConfigured => !_impactVfxId.IsNone || !_globalImpactKind.IsNone;

        public bool TryValidate(out string issue)
        {
            if (!IsConfigured)
            {
                issue = string.Empty;
                return true;
            }

            if (!WorldFeedbackCoreValidation.Require(_impactVfxId, nameof(ImpactVfxId), out issue))
            {
                return false;
            }

            if (_globalImpactKind.IsNone)
            {
                issue = $"Required {nameof(GlobalImpactKind)} cannot be empty.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }

    [Serializable]
    public struct PersistentHazardFeedback
    {
        [SerializeField] private VfxAssetId _spawnVfxId;
        [SerializeField] private VfxAssetId _boundaryVfxId;
        [SerializeField] private VfxAssetId _loopVfxId;
        [SerializeField] private VfxAssetId _tickVfxId;
        [SerializeField] private VfxAssetId _endVfxId;
        [SerializeField] private AudioAssetId _spawnSfxId;
        [SerializeField] private AudioAssetId _loopSfxId;
        [SerializeField] private AudioAssetId _tickSfxId;
        [SerializeField] private AudioAssetId _endSfxId;

        public PersistentHazardFeedback(
            VfxAssetId spawnVfxId,
            VfxAssetId boundaryVfxId,
            VfxAssetId loopVfxId,
            VfxAssetId tickVfxId,
            VfxAssetId endVfxId,
            AudioAssetId spawnSfxId,
            AudioAssetId loopSfxId,
            AudioAssetId tickSfxId,
            AudioAssetId endSfxId)
        {
            _spawnVfxId = spawnVfxId;
            _boundaryVfxId = boundaryVfxId;
            _loopVfxId = loopVfxId;
            _tickVfxId = tickVfxId;
            _endVfxId = endVfxId;
            _spawnSfxId = spawnSfxId;
            _loopSfxId = loopSfxId;
            _tickSfxId = tickSfxId;
            _endSfxId = endSfxId;
        }

        public VfxAssetId SpawnVfxId => _spawnVfxId;
        public VfxAssetId BoundaryVfxId => _boundaryVfxId;
        public VfxAssetId LoopVfxId => _loopVfxId;
        public VfxAssetId TickVfxId => _tickVfxId;
        public VfxAssetId EndVfxId => _endVfxId;
        public AudioAssetId SpawnSfxId => _spawnSfxId;
        public AudioAssetId LoopSfxId => _loopSfxId;
        public AudioAssetId TickSfxId => _tickSfxId;
        public AudioAssetId EndSfxId => _endSfxId;
        public bool IsConfigured => !_spawnVfxId.IsNone || !_boundaryVfxId.IsNone || !_loopVfxId.IsNone
                                    || !_tickVfxId.IsNone || !_endVfxId.IsNone || !_spawnSfxId.IsNone
                                    || !_loopSfxId.IsNone || !_tickSfxId.IsNone || !_endSfxId.IsNone;

        public bool TryValidate(out string issue)
        {
            if (!IsConfigured)
            {
                issue = string.Empty;
                return true;
            }

            return WorldFeedbackCoreValidation.Require(_spawnVfxId, nameof(SpawnVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_boundaryVfxId, nameof(BoundaryVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_loopVfxId, nameof(LoopVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_tickVfxId, nameof(TickVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_endVfxId, nameof(EndVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_spawnSfxId, nameof(SpawnSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_loopSfxId, nameof(LoopSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_tickSfxId, nameof(TickSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_endSfxId, nameof(EndSfxId), out issue);
        }
    }

    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/Enemy Attack Feedback Profile", fileName = "EnemyAttackFeedbackProfile")]
    public sealed class EnemyAttackFeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private WindupFeedback _windupFeedback;
        [SerializeField] private TelegraphFeedback _telegraphFeedback;
        [SerializeField] private EnemyProjectileFeedback _enemyProjectileFeedback;
        [SerializeField] private EnemyImpactFeedback _enemyImpactFeedback;
        [SerializeField] private PersistentHazardFeedback _persistentHazardFeedback;

        public WindupFeedback WindupFeedback => _windupFeedback;
        public TelegraphFeedback TelegraphFeedback => _telegraphFeedback;
        public EnemyProjectileFeedback EnemyProjectileFeedback => _enemyProjectileFeedback;
        public EnemyImpactFeedback EnemyImpactFeedback => _enemyImpactFeedback;
        public PersistentHazardFeedback PersistentHazardFeedback => _persistentHazardFeedback;

        public bool TryValidate(out string issue)
        {
            bool hasFeedback = _windupFeedback.IsConfigured
                               || _telegraphFeedback.IsConfigured
                               || _enemyProjectileFeedback.IsConfigured
                               || _enemyImpactFeedback.IsConfigured
                               || _persistentHazardFeedback.IsConfigured;
            if (!hasFeedback)
            {
                issue = "Enemy attack feedback profile must configure at least one delivery group.";
                return false;
            }

            return _windupFeedback.TryValidate(out issue)
                   && _telegraphFeedback.TryValidate(out issue)
                   && _enemyProjectileFeedback.TryValidate(out issue)
                   && _enemyImpactFeedback.TryValidate(out issue)
                   && _persistentHazardFeedback.TryValidate(out issue);
        }

#if UNITY_EDITOR
        public void SetForEditor(
            WindupFeedback windupFeedback,
            TelegraphFeedback telegraphFeedback,
            EnemyProjectileFeedback enemyProjectileFeedback,
            EnemyImpactFeedback enemyImpactFeedback,
            PersistentHazardFeedback persistentHazardFeedback)
        {
            _windupFeedback = windupFeedback;
            _telegraphFeedback = telegraphFeedback;
            _enemyProjectileFeedback = enemyProjectileFeedback;
            _enemyImpactFeedback = enemyImpactFeedback;
            _persistentHazardFeedback = persistentHazardFeedback;
        }
#endif
    }

    [Serializable]
    public struct EnemyAttackFeedbackBinding
    {
        [SerializeField] private EnemyAttackId _enemyAttackId;
        [SerializeField] private EnemyAttackFeedbackProfileSO _profile;

        public EnemyAttackFeedbackBinding(EnemyAttackId enemyAttackId, EnemyAttackFeedbackProfileSO profile)
        {
            _enemyAttackId = enemyAttackId;
            _profile = profile;
        }

        public EnemyAttackId EnemyAttackId => _enemyAttackId;
        public EnemyAttackFeedbackProfileSO Profile => _profile;
    }
}
