using System;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [Serializable]
    public struct CastFeedback
    {
        [SerializeField] private MotionAssetId _castMotionId;
        [SerializeField] private VfxAssetId _castVfxId;
        [SerializeField] private AudioAssetId _castSfxId;
        [SerializeField] private MotionAssetId _releaseMotionId;
        [SerializeField] private VfxAssetId _releaseVfxId;
        [SerializeField] private AudioAssetId _releaseSfxId;

        public CastFeedback(
            MotionAssetId castMotionId,
            VfxAssetId castVfxId,
            AudioAssetId castSfxId,
            MotionAssetId releaseMotionId,
            VfxAssetId releaseVfxId,
            AudioAssetId releaseSfxId)
        {
            _castMotionId = castMotionId;
            _castVfxId = castVfxId;
            _castSfxId = castSfxId;
            _releaseMotionId = releaseMotionId;
            _releaseVfxId = releaseVfxId;
            _releaseSfxId = releaseSfxId;
        }

        public MotionAssetId CastMotionId => _castMotionId;
        public VfxAssetId CastVfxId => _castVfxId;
        public AudioAssetId CastSfxId => _castSfxId;
        public MotionAssetId ReleaseMotionId => _releaseMotionId;
        public VfxAssetId ReleaseVfxId => _releaseVfxId;
        public AudioAssetId ReleaseSfxId => _releaseSfxId;
        public bool IsConfigured => !_castMotionId.IsNone || !_castVfxId.IsNone || !_castSfxId.IsNone
                                    || !_releaseMotionId.IsNone || !_releaseVfxId.IsNone || !_releaseSfxId.IsNone;

        public bool TryValidate(out string issue)
        {
            if (!IsConfigured)
            {
                issue = string.Empty;
                return true;
            }

            return WorldFeedbackCoreValidation.Require(_castMotionId, nameof(CastMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_castVfxId, nameof(CastVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_castSfxId, nameof(CastSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_releaseMotionId, nameof(ReleaseMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_releaseVfxId, nameof(ReleaseVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_releaseSfxId, nameof(ReleaseSfxId), out issue);
        }
    }

    [Serializable]
    public struct ProjectileFeedback
    {
        [SerializeField] private SpriteAssetId _projectileSpriteId;
        [SerializeField] private VfxAssetId _spawnVfxId;
        [SerializeField] private VfxAssetId _trailVfxId;
        [SerializeField] private AudioAssetId _launchSfxId;
        [SerializeField] private AudioAssetId _travelLoopSfxId;
        [SerializeField] private VfxAssetId _lifetimeEndVfxId;
        [SerializeField] private AudioAssetId _lifetimeEndSfxId;

        public ProjectileFeedback(
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
    public struct AreaFeedback
    {
        [SerializeField] private VfxAssetId _indicatorVfxId;
        [SerializeField] private MotionAssetId _indicatorEnterMotionId;
        [SerializeField] private MotionAssetId _indicatorExitMotionId;
        [SerializeField] private VfxAssetId _activationVfxId;
        [SerializeField] private AudioAssetId _activationSfxId;

        public AreaFeedback(
            VfxAssetId indicatorVfxId,
            MotionAssetId indicatorEnterMotionId,
            MotionAssetId indicatorExitMotionId,
            VfxAssetId activationVfxId,
            AudioAssetId activationSfxId)
        {
            _indicatorVfxId = indicatorVfxId;
            _indicatorEnterMotionId = indicatorEnterMotionId;
            _indicatorExitMotionId = indicatorExitMotionId;
            _activationVfxId = activationVfxId;
            _activationSfxId = activationSfxId;
        }

        public VfxAssetId IndicatorVfxId => _indicatorVfxId;
        public MotionAssetId IndicatorEnterMotionId => _indicatorEnterMotionId;
        public MotionAssetId IndicatorExitMotionId => _indicatorExitMotionId;
        public VfxAssetId ActivationVfxId => _activationVfxId;
        public AudioAssetId ActivationSfxId => _activationSfxId;
        public bool IsConfigured => !_indicatorVfxId.IsNone || !_indicatorEnterMotionId.IsNone
                                    || !_indicatorExitMotionId.IsNone || !_activationVfxId.IsNone
                                    || !_activationSfxId.IsNone;

        public bool TryValidate(out string issue)
        {
            if (!IsConfigured)
            {
                issue = string.Empty;
                return true;
            }

            return WorldFeedbackCoreValidation.Require(_indicatorVfxId, nameof(IndicatorVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_indicatorEnterMotionId, nameof(IndicatorEnterMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_indicatorExitMotionId, nameof(IndicatorExitMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_activationVfxId, nameof(ActivationVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_activationSfxId, nameof(ActivationSfxId), out issue);
        }
    }

    [Serializable]
    public struct FieldFeedback
    {
        [SerializeField] private VfxAssetId _spawnVfxId;
        [SerializeField] private VfxAssetId _groundBoundaryVfxId;
        [SerializeField] private VfxAssetId _loopVfxId;
        [SerializeField] private VfxAssetId _tickVfxId;
        [SerializeField] private VfxAssetId _endVfxId;
        [SerializeField] private AudioAssetId _spawnSfxId;
        [SerializeField] private AudioAssetId _loopSfxId;
        [SerializeField] private AudioAssetId _tickSfxId;
        [SerializeField] private AudioAssetId _endSfxId;

        public FieldFeedback(
            VfxAssetId spawnVfxId,
            VfxAssetId groundBoundaryVfxId,
            VfxAssetId loopVfxId,
            VfxAssetId tickVfxId,
            VfxAssetId endVfxId,
            AudioAssetId spawnSfxId,
            AudioAssetId loopSfxId,
            AudioAssetId tickSfxId,
            AudioAssetId endSfxId)
        {
            _spawnVfxId = spawnVfxId;
            _groundBoundaryVfxId = groundBoundaryVfxId;
            _loopVfxId = loopVfxId;
            _tickVfxId = tickVfxId;
            _endVfxId = endVfxId;
            _spawnSfxId = spawnSfxId;
            _loopSfxId = loopSfxId;
            _tickSfxId = tickSfxId;
            _endSfxId = endSfxId;
        }

        public VfxAssetId SpawnVfxId => _spawnVfxId;
        public VfxAssetId GroundBoundaryVfxId => _groundBoundaryVfxId;
        public VfxAssetId LoopVfxId => _loopVfxId;
        public VfxAssetId TickVfxId => _tickVfxId;
        public VfxAssetId EndVfxId => _endVfxId;
        public AudioAssetId SpawnSfxId => _spawnSfxId;
        public AudioAssetId LoopSfxId => _loopSfxId;
        public AudioAssetId TickSfxId => _tickSfxId;
        public AudioAssetId EndSfxId => _endSfxId;
        public bool IsConfigured => !_spawnVfxId.IsNone || !_groundBoundaryVfxId.IsNone || !_loopVfxId.IsNone
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
                   && WorldFeedbackCoreValidation.Require(_groundBoundaryVfxId, nameof(GroundBoundaryVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_loopVfxId, nameof(LoopVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_tickVfxId, nameof(TickVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_endVfxId, nameof(EndVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_spawnSfxId, nameof(SpawnSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_loopSfxId, nameof(LoopSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_tickSfxId, nameof(TickSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_endSfxId, nameof(EndSfxId), out issue);
        }
    }

    [Serializable]
    public struct ChainFeedback
    {
        [SerializeField] private VfxAssetId _startVfxId;
        [SerializeField] private VfxAssetId _linkVfxId;
        [SerializeField] private MotionAssetId _linkMotionId;
        [SerializeField] private AudioAssetId _linkSfxId;
        [SerializeField] private VfxAssetId _endVfxId;

        public ChainFeedback(
            VfxAssetId startVfxId,
            VfxAssetId linkVfxId,
            MotionAssetId linkMotionId,
            AudioAssetId linkSfxId,
            VfxAssetId endVfxId)
        {
            _startVfxId = startVfxId;
            _linkVfxId = linkVfxId;
            _linkMotionId = linkMotionId;
            _linkSfxId = linkSfxId;
            _endVfxId = endVfxId;
        }

        public VfxAssetId StartVfxId => _startVfxId;
        public VfxAssetId LinkVfxId => _linkVfxId;
        public MotionAssetId LinkMotionId => _linkMotionId;
        public AudioAssetId LinkSfxId => _linkSfxId;
        public VfxAssetId EndVfxId => _endVfxId;
        public bool IsConfigured => !_startVfxId.IsNone || !_linkVfxId.IsNone || !_linkMotionId.IsNone
                                    || !_linkSfxId.IsNone || !_endVfxId.IsNone;

        public bool TryValidate(out string issue)
        {
            if (!IsConfigured)
            {
                issue = string.Empty;
                return true;
            }

            return WorldFeedbackCoreValidation.Require(_startVfxId, nameof(StartVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_linkVfxId, nameof(LinkVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_linkMotionId, nameof(LinkMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_linkSfxId, nameof(LinkSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_endVfxId, nameof(EndVfxId), out issue);
        }
    }

    [Serializable]
    public struct ProxyFeedback
    {
        [SerializeField] private SpriteAssetId _proxySpriteId;
        [SerializeField] private MotionAssetId _spawnMotionId;
        [SerializeField] private VfxAssetId _spawnVfxId;
        [SerializeField] private AudioAssetId _spawnSfxId;
        [SerializeField] private MotionAssetId _idleMotionId;
        [SerializeField] private MotionAssetId _moveLoopMotionId;
        [SerializeField] private MotionAssetId _exitMotionId;
        [SerializeField] private VfxAssetId _exitVfxId;
        [SerializeField] private AudioAssetId _exitSfxId;

        public ProxyFeedback(
            SpriteAssetId proxySpriteId,
            MotionAssetId spawnMotionId,
            VfxAssetId spawnVfxId,
            AudioAssetId spawnSfxId,
            MotionAssetId idleMotionId,
            MotionAssetId moveLoopMotionId,
            MotionAssetId exitMotionId,
            VfxAssetId exitVfxId,
            AudioAssetId exitSfxId)
        {
            _proxySpriteId = proxySpriteId;
            _spawnMotionId = spawnMotionId;
            _spawnVfxId = spawnVfxId;
            _spawnSfxId = spawnSfxId;
            _idleMotionId = idleMotionId;
            _moveLoopMotionId = moveLoopMotionId;
            _exitMotionId = exitMotionId;
            _exitVfxId = exitVfxId;
            _exitSfxId = exitSfxId;
        }

        public SpriteAssetId ProxySpriteId => _proxySpriteId;
        public MotionAssetId SpawnMotionId => _spawnMotionId;
        public VfxAssetId SpawnVfxId => _spawnVfxId;
        public AudioAssetId SpawnSfxId => _spawnSfxId;
        public MotionAssetId IdleMotionId => _idleMotionId;
        public MotionAssetId MoveLoopMotionId => _moveLoopMotionId;
        public MotionAssetId ExitMotionId => _exitMotionId;
        public VfxAssetId ExitVfxId => _exitVfxId;
        public AudioAssetId ExitSfxId => _exitSfxId;
        public bool IsConfigured => !_proxySpriteId.IsNone || !_spawnMotionId.IsNone || !_spawnVfxId.IsNone
                                    || !_spawnSfxId.IsNone || !_idleMotionId.IsNone || !_moveLoopMotionId.IsNone
                                    || !_exitMotionId.IsNone || !_exitVfxId.IsNone || !_exitSfxId.IsNone;

        public bool TryValidate(out string issue)
        {
            if (!IsConfigured)
            {
                issue = string.Empty;
                return true;
            }

            return WorldFeedbackCoreValidation.Require(_proxySpriteId, nameof(ProxySpriteId), out issue)
                   && WorldFeedbackCoreValidation.Require(_spawnMotionId, nameof(SpawnMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_spawnVfxId, nameof(SpawnVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_spawnSfxId, nameof(SpawnSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_idleMotionId, nameof(IdleMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_moveLoopMotionId, nameof(MoveLoopMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_exitMotionId, nameof(ExitMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_exitVfxId, nameof(ExitVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_exitSfxId, nameof(ExitSfxId), out issue);
        }
    }

    [Serializable]
    public struct ReturningProjectileFeedback
    {
        [SerializeField] private MotionAssetId _turnMotionId;
        [SerializeField] private VfxAssetId _turnVfxId;
        [SerializeField] private AudioAssetId _turnSfxId;
        [SerializeField] private VfxAssetId _returnTrailVfxId;
        [SerializeField] private MotionAssetId _returnMotionId;
        [SerializeField] private VfxAssetId _catchVfxId;
        [SerializeField] private AudioAssetId _catchSfxId;

        public ReturningProjectileFeedback(
            MotionAssetId turnMotionId,
            VfxAssetId turnVfxId,
            AudioAssetId turnSfxId,
            VfxAssetId returnTrailVfxId,
            MotionAssetId returnMotionId,
            VfxAssetId catchVfxId,
            AudioAssetId catchSfxId)
        {
            _turnMotionId = turnMotionId;
            _turnVfxId = turnVfxId;
            _turnSfxId = turnSfxId;
            _returnTrailVfxId = returnTrailVfxId;
            _returnMotionId = returnMotionId;
            _catchVfxId = catchVfxId;
            _catchSfxId = catchSfxId;
        }

        public MotionAssetId TurnMotionId => _turnMotionId;
        public VfxAssetId TurnVfxId => _turnVfxId;
        public AudioAssetId TurnSfxId => _turnSfxId;
        public VfxAssetId ReturnTrailVfxId => _returnTrailVfxId;
        public MotionAssetId ReturnMotionId => _returnMotionId;
        public VfxAssetId CatchVfxId => _catchVfxId;
        public AudioAssetId CatchSfxId => _catchSfxId;
        public bool IsConfigured => !_turnMotionId.IsNone || !_turnVfxId.IsNone || !_turnSfxId.IsNone
                                    || !_returnTrailVfxId.IsNone || !_returnMotionId.IsNone
                                    || !_catchVfxId.IsNone || !_catchSfxId.IsNone;

        public bool TryValidate(out string issue)
        {
            if (!IsConfigured)
            {
                issue = string.Empty;
                return true;
            }

            return WorldFeedbackCoreValidation.Require(_turnMotionId, nameof(TurnMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_turnVfxId, nameof(TurnVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_turnSfxId, nameof(TurnSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_returnTrailVfxId, nameof(ReturnTrailVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_returnMotionId, nameof(ReturnMotionId), out issue)
                   && WorldFeedbackCoreValidation.Require(_catchVfxId, nameof(CatchVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_catchSfxId, nameof(CatchSfxId), out issue);
        }
    }

    [Serializable]
    public struct SelfFeedback
    {
        [SerializeField] private VfxAssetId _activationVfxId;
        [SerializeField] private VfxAssetId _auraLoopVfxId;
        [SerializeField] private VfxAssetId _endVfxId;
        [SerializeField] private AudioAssetId _activationSfxId;
        [SerializeField] private AudioAssetId _loopSfxId;
        [SerializeField] private AudioAssetId _endSfxId;

        public SelfFeedback(
            VfxAssetId activationVfxId,
            VfxAssetId auraLoopVfxId,
            VfxAssetId endVfxId,
            AudioAssetId activationSfxId,
            AudioAssetId loopSfxId,
            AudioAssetId endSfxId)
        {
            _activationVfxId = activationVfxId;
            _auraLoopVfxId = auraLoopVfxId;
            _endVfxId = endVfxId;
            _activationSfxId = activationSfxId;
            _loopSfxId = loopSfxId;
            _endSfxId = endSfxId;
        }

        public VfxAssetId ActivationVfxId => _activationVfxId;
        public VfxAssetId AuraLoopVfxId => _auraLoopVfxId;
        public VfxAssetId EndVfxId => _endVfxId;
        public AudioAssetId ActivationSfxId => _activationSfxId;
        public AudioAssetId LoopSfxId => _loopSfxId;
        public AudioAssetId EndSfxId => _endSfxId;
        public bool IsConfigured => !_activationVfxId.IsNone || !_auraLoopVfxId.IsNone || !_endVfxId.IsNone
                                    || !_activationSfxId.IsNone || !_loopSfxId.IsNone || !_endSfxId.IsNone;

        public bool TryValidate(out string issue)
        {
            if (!IsConfigured)
            {
                issue = string.Empty;
                return true;
            }

            return WorldFeedbackCoreValidation.Require(_activationVfxId, nameof(ActivationVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_auraLoopVfxId, nameof(AuraLoopVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_endVfxId, nameof(EndVfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_activationSfxId, nameof(ActivationSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_loopSfxId, nameof(LoopSfxId), out issue)
                   && WorldFeedbackCoreValidation.Require(_endSfxId, nameof(EndSfxId), out issue);
        }
    }

    [Serializable]
    public struct ImpactFeedback
    {
        [SerializeField] private VfxAssetId _impactVfxId;
        [SerializeField] private CombatImpactKind _globalImpactKind;

        public ImpactFeedback(VfxAssetId impactVfxId, CombatImpactKind globalImpactKind)
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
    public struct AttackFeedbackBinding
    {
        [SerializeField] private AttackId _attackId;
        [SerializeField] private AttackFeedbackProfileSO _profile;

        public AttackFeedbackBinding(AttackId attackId, AttackFeedbackProfileSO profile)
        {
            _attackId = attackId;
            _profile = profile;
        }

        public AttackId AttackId => _attackId;
        public AttackFeedbackProfileSO Profile => _profile;
    }
}
