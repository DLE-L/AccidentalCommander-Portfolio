using System;
using UnityEngine;
using UnityEngine.U2D.Animation;

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
        public int LinkedDriverCount => _linkedDrivers == null ? 0 : _linkedDrivers.Length;

        public void SetLinkedDriversForPresentation(UnitVisualDriver[] linkedDrivers)
        {
            _linkedDrivers = linkedDrivers ?? Array.Empty<UnitVisualDriver>();
        }

        public void SetMotionCategoriesForPresentation(string idle, string run, string attack, string death)
        {
            _idleCategory = string.IsNullOrEmpty(idle) ? IDLE_STATE : idle;
            _runCategory = string.IsNullOrEmpty(run) ? RUN_STATE : run;
            _attackCategory = string.IsNullOrEmpty(attack) ? ATTACK_STATE : attack;
            _deathCategory = string.IsNullOrEmpty(death) ? DEATH_STATE : death;
        }

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

        private bool _isForwardingLinkedState;

        private void ForwardMoving(bool isMoving)
        {
            if (_isForwardingLinkedState || _linkedDrivers == null)
                return;

            _isForwardingLinkedState = true;
            try
            {
                for (int i = 0; i < _linkedDrivers.Length; i++)
                {
                    UnitVisualDriver linked = _linkedDrivers[i];
                    if (linked != null && linked != this)
                        linked.SetMoving(isMoving);
                }
            }
            finally
            {
                _isForwardingLinkedState = false;
            }
        }

        private void ForwardDead(bool isDead)
        {
            if (_isForwardingLinkedState || _linkedDrivers == null)
                return;

            _isForwardingLinkedState = true;
            try
            {
                for (int i = 0; i < _linkedDrivers.Length; i++)
                {
                    UnitVisualDriver linked = _linkedDrivers[i];
                    if (linked != null && linked != this)
                        linked.SetDead(isDead);
                }
            }
            finally
            {
                _isForwardingLinkedState = false;
            }
        }

        private void ForwardFacing(Vector3 worldDirection)
        {
            if (_isForwardingLinkedState || _linkedDrivers == null)
                return;

            _isForwardingLinkedState = true;
            try
            {
                for (int i = 0; i < _linkedDrivers.Length; i++)
                {
                    UnitVisualDriver linked = _linkedDrivers[i];
                    if (linked != null && linked != this)
                        linked.FaceDirection(worldDirection);
                }
            }
            finally
            {
                _isForwardingLinkedState = false;
            }
        }

        private void ForwardAttack(Vector3 worldDirection, float holdSeconds)
        {
            if (_isForwardingLinkedState || _linkedDrivers == null)
                return;

            _isForwardingLinkedState = true;
            try
            {
                for (int i = 0; i < _linkedDrivers.Length; i++)
                {
                    UnitVisualDriver linked = _linkedDrivers[i];
                    if (linked != null && linked != this)
                        linked.PlayAttack(worldDirection, holdSeconds);
                }
            }
            finally
            {
                _isForwardingLinkedState = false;
            }
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
            _lastSpriteCategory = null;
            _lastSpriteFrame = -1;
            if (_animator != null)
                _animator.speed = 1.0f;
        }

        private void UpdateSpriteResolver(int stateHash)
        {
            if (_spriteResolver == null || _animator == null)
                return;

            string category = ResolveSpriteCategory(stateHash);
            if (category == null)
                return;

            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            float normalizedTime = stateHash == _deathHash
                ? Mathf.Clamp01(state.normalizedTime)
                : state.normalizedTime - Mathf.Floor(state.normalizedTime);
            int frame = Mathf.Clamp(Mathf.FloorToInt(normalizedTime * SpriteLabels.Length), 0, SpriteLabels.Length - 1);
            if (_lastSpriteCategory == category && _lastSpriteFrame == frame)
                return;

            _spriteResolver.SetCategoryAndLabel(category, SpriteLabels[frame]);
            _lastSpriteCategory = category;
            _lastSpriteFrame = frame;
        }

        private string ResolveSpriteCategory(int stateHash)
        {
            if (stateHash == _idleHash) return _idleCategory;
            if (stateHash == _runHash) return _runCategory;
            if (stateHash == _attackHash) return _attackCategory;
            if (stateHash == _deathHash) return _deathCategory;
            return null;
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
