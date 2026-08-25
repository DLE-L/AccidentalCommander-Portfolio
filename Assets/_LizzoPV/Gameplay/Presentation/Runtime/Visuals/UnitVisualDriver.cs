using System;
using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Lizzo.PV.P0.Visuals
{
    [DefaultExecutionOrder(30)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Animator))]
    public sealed partial class UnitVisualDriver : MonoBehaviour
    {
        private const float FLIP_DELTA_THRESHOLD = 0.006f;
        private const string STATE_PATH_PREFIX = "Base Layer.";
        private const string IDLE_STATE = "Idle";
        private const string RUN_STATE = "Run";
        private const string ATTACK_STATE = "Attack";
        private const string DEATH_STATE = "Death";
        private static readonly string[] SpriteLabels = { "0", "1", "2", "3", "4", "5", "6", "7", "8" };

        [SerializeField]
        private SpriteRenderer _spriteRenderer;
        [SerializeField]
        private Animator _animator;
        [SerializeField]
        private SpriteResolver _spriteResolver;
        [SerializeField]
        private float _attackHoldSeconds = 0.28f;
        [SerializeField]
        private float _attackClipLength = 0.33f;
        [SerializeField]
        private UnitVisualDriver[] _linkedDrivers = Array.Empty<UnitVisualDriver>();
        [SerializeField]
        private string _idleCategory = IDLE_STATE;
        [SerializeField]
        private string _runCategory = RUN_STATE;
        [SerializeField]
        private string _attackCategory = ATTACK_STATE;
        [SerializeField]
        private string _deathCategory = DEATH_STATE;
        [SerializeField, Range(1, 9)]
        private int _idleFrameCount = 2;
        [SerializeField, Range(1, 9)]
        private int _runFrameCount = 4;
        [SerializeField, Range(1, 9)]
        private int _attackFrameCount = 4;
        [SerializeField, Range(1, 9)]
        private int _deathFrameCount = 3;

        private int _idleHash;
        private int _runHash;
        private int _attackHash;
        private int _deathHash;
        private int _currentStateHash;
        private bool _isMoving;
        private bool _isDead;
        private float _attackUntil;
        private float _attackDuration;
        private string _lastSpriteCategory;
        private int _lastSpriteFrame = -1;

        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        public int IdleFrameCount => ClampFrameCount(_idleFrameCount);
        public int RunFrameCount => ClampFrameCount(_runFrameCount);
        public int AttackFrameCount => ClampFrameCount(_attackFrameCount);
        public int DeathFrameCount => ClampFrameCount(_deathFrameCount);

        private void Awake()
        {
            ResolveReferences();
            InitializeStateHashes();
            ValidateFrameCounts();
            ResetState();
        }

        private void OnEnable()
        {
            ResolveReferences();
            InitializeStateHashes();
            ValidateFrameCounts();
            ResetState();
        }

        private void LateUpdate()
        {
            if (_animator == null)
                return;

            int stateHash = ResolveStateHash();
            if (stateHash == 0)
                return;

            if (_currentStateHash != stateHash)
            {
                if (HasRequiredState(stateHash) == false)
                {
                    Debug.LogError($"UnitVisualDriver is missing required Animator state on '{gameObject.name}': {ResolveStateName(stateHash)}", this);
                    return;
                }

                _animator.Play(stateHash, 0, 0.0f);
                _currentStateHash = stateHash;
            }

            _animator.speed = stateHash == _attackHash
                ? Mathf.Max(0.01f, _attackClipLength / Mathf.Max(0.05f, _attackDuration))
                : 1.0f;
            UpdateSpriteResolver(stateHash);
        }

        public void SetMoving(bool isMoving)
        {
            _isMoving = isMoving;
            ForwardMoving(isMoving);
        }

        public void SetDead(bool isDead)
        {
            if (_isDead == isDead)
                return;

            _isDead = isDead;
            _currentStateHash = 0;
            ForwardDead(isDead);
        }

        public void FaceDirection(Vector3 worldDirection)
        {
            if (_spriteRenderer == null || Mathf.Abs(worldDirection.x) <= FLIP_DELTA_THRESHOLD)
                return;

            _spriteRenderer.flipX = worldDirection.x < 0.0f;
            ForwardFacing(worldDirection);
        }

        public void PlayAttack(Vector3 worldDirection, float holdSeconds = -1.0f)
        {
            if (_animator == null)
                return;

            float duration = Mathf.Max(0.05f, holdSeconds > 0.0f ? holdSeconds : _attackHoldSeconds);
            FaceDirection(worldDirection);
            _attackDuration = duration;
            _attackUntil = Time.time + duration;
            _currentStateHash = 0;
            ForwardAttack(worldDirection, holdSeconds);
        }

        private void ResolveReferences()
        {
            _spriteRenderer ??= GetComponent<SpriteRenderer>();
            _animator ??= GetComponent<Animator>();
            _spriteResolver ??= GetComponent<SpriteResolver>();

            if (_spriteRenderer == null)
                Debug.LogError($"UnitVisualDriver is missing required SpriteRenderer: {gameObject.name}", this);

            if (_animator == null)
            {
                Debug.LogError($"UnitVisualDriver is missing required Animator: {gameObject.name}", this);
                return;
            }

            if (_animator.runtimeAnimatorController == null)
                Debug.LogError($"UnitVisualDriver is missing AnimatorController: {gameObject.name}", this);
        }

    }
}
