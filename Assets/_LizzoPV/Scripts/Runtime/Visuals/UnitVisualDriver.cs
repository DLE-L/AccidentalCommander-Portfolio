using UnityEngine;

namespace Lizzo.PV.P0.Visuals
{
    [DefaultExecutionOrder(30)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Animator))]
    public sealed class UnitVisualDriver : MonoBehaviour
    {
        private const float FLIP_DELTA_THRESHOLD = 0.006f;
        private const string STATE_PATH_PREFIX = "Base Layer.";
        private const string IDLE_STATE = "Idle";
        private const string RUN_STATE = "Run";
        private const string ATTACK_STATE = "Attack";
        private const string DEATH_STATE = "Death";

        [SerializeField]
        private SpriteRenderer _spriteRenderer;
        [SerializeField]
        private Animator _animator;
        [SerializeField]
        private float _attackHoldSeconds = 0.28f;
        [SerializeField]
        private float _attackClipLength = 0.33f;

        private int _idleHash;
        private int _runHash;
        private int _attackHash;
        private int _deathHash;
        private int _currentStateHash;
        private bool _isMoving;
        private bool _isDead;
        private float _attackUntil;
        private float _attackDuration;

        public SpriteRenderer SpriteRenderer => _spriteRenderer;

        private void Awake()
        {
            ResolveReferences();
            InitializeStateHashes();
            ResetState();
        }

        private void OnEnable()
        {
            ResolveReferences();
            InitializeStateHashes();
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
        }

        public void SetMoving(bool isMoving)
        {
            _isMoving = isMoving;
        }

        public void SetDead(bool isDead)
        {
            if (_isDead == isDead)
                return;

            _isDead = isDead;
            _currentStateHash = 0;
        }

        public void FaceDirection(Vector3 worldDirection)
        {
            if (_spriteRenderer == null || Mathf.Abs(worldDirection.x) <= FLIP_DELTA_THRESHOLD)
                return;

            _spriteRenderer.flipX = worldDirection.x < 0.0f;
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
        }

        private void ResolveReferences()
        {
            _spriteRenderer ??= GetComponent<SpriteRenderer>();
            _animator ??= GetComponent<Animator>();

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

private void InitializeStateHashes()
        {
            _idleHash = Animator.StringToHash(STATE_PATH_PREFIX + IDLE_STATE);
            _runHash = Animator.StringToHash(STATE_PATH_PREFIX + RUN_STATE);
            _attackHash = Animator.StringToHash(STATE_PATH_PREFIX + ATTACK_STATE);
            _deathHash = Animator.StringToHash(STATE_PATH_PREFIX + DEATH_STATE);
        }

        private void ResetState()
        {
            _currentStateHash = 0;
            _isMoving = false;
            _isDead = false;
            _attackUntil = 0.0f;
            _attackDuration = Mathf.Max(0.05f, _attackHoldSeconds);
            if (_animator != null)
                _animator.speed = 1.0f;
        }

        private int ResolveStateHash()
        {
            if (_isDead)
                return _deathHash;

            if (Time.time < _attackUntil)
                return _attackHash;

            return _isMoving ? _runHash : _idleHash;
        }

        private bool HasRequiredState(int stateHash)
        {
            return _animator != null
                && _animator.runtimeAnimatorController != null
                && _animator.HasState(0, stateHash);
        }

        private string ResolveStateName(int stateHash)
        {
            if (stateHash == _idleHash) return IDLE_STATE;
            if (stateHash == _runHash) return RUN_STATE;
            if (stateHash == _attackHash) return ATTACK_STATE;
            if (stateHash == _deathHash) return DEATH_STATE;
            return "<unknown>";
        }
    }
}