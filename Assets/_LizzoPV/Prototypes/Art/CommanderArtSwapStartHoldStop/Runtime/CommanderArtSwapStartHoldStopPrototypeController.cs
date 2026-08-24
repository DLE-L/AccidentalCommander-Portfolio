using System;
using System.Reflection;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Prototypes.CommanderArtSwap
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class CommanderArtSwapStartHoldStopPrototypeController : MonoBehaviour
    {
        private const float MOTION_FPS = 60.0f;
        private const float MOVE_LOOP_FRAMES = 28.0f;
        private const float ATTACK_EVENT_FRAME = 7.0f;
        private const float ATTACK_TOTAL_FRAMES = 22.0f;
        private const float MOVE_THRESHOLD_SQR = 0.001f;

        private static readonly FieldInfo NextAttackTimeField = typeof(CommanderAttack).GetField(
            "_nextAttackTime",
            BindingFlags.Instance | BindingFlags.NonPublic);

        [Header("Static Commander sprite")]
        [SerializeField]
        private Sprite _staticSprite;
        [SerializeField, Min(0.001f)]
        private float _staticLocalScale = 0.1f;
        [Header("Motion preview")]
        [SerializeField]
        private bool _enableAttackMotion;

        private UnitVisualDriver[] _drivers = Array.Empty<UnitVisualDriver>();
        private bool[] _driverEnabledStates = Array.Empty<bool>();
        private Animator[] _animators = Array.Empty<Animator>();
        private bool[] _animatorEnabledStates = Array.Empty<bool>();
        private SpriteRenderer[] _renderers = Array.Empty<SpriteRenderer>();
        private bool[] _rendererEnabledStates = Array.Empty<bool>();
        private PlayerController _player;
        private CommanderAttack _commanderAttack;
        private SpriteRenderer _primaryRenderer;
        private Sprite _originalSprite;
        private Vector3 _originalLocalPosition;
        private Quaternion _originalLocalRotation;
        private Vector3 _originalLocalScale;
        private float _moveStartedAt;
        private float _attackStartedAt;
        private float _predictedAttackEventAt;
        private float _attackFacingSign = 1.0f;
        private int _observedFireCount;
        private bool _wasMoving;
        private bool _isAttacking;
        private bool _isBound;

        private void Update()
        {
            if (!_isBound)
            {
                TryBindCommander();
                return;
            }

            if (_enableAttackMotion)
            {
                UpdateAttackSynchronization();
            }
            else
            {
                _isAttacking = false;
                _observedFireCount = CommanderAttack.DebugFireCount;
            }

            UpdateMovementState();
        }

        private void LateUpdate()
        {
            if (!_isBound || _primaryRenderer == null)
                return;

            _primaryRenderer.enabled = true;
            _primaryRenderer.sprite = _staticSprite;

            if (_isAttacking)
            {
                ApplyAttackPose((Time.time - _attackStartedAt) * MOTION_FPS);
                return;
            }

            if (_player != null && _player.MoveDirection.sqrMagnitude > MOVE_THRESHOLD_SQR)
            {
                ApplyMovePose(Mathf.Repeat((Time.time - _moveStartedAt) * MOTION_FPS, MOVE_LOOP_FRAMES));
                return;
            }

            ApplyVisualPose(0.0f, 0.0f, 0.0f, 1.0f, 1.0f);
        }

        private void OnDisable()
        {
            RestoreCommanderVisual();
        }

        private void OnDestroy()
        {
            RestoreCommanderVisual();
        }

        private void TryBindCommander()
        {
            if (_staticSprite == null)
            {
                Debug.LogError("[CommanderMotionPolishPrototype] Static sprite is not assigned.", this);
                enabled = false;
                return;
            }

            CommanderAllyVisual visual = FindFirstObjectByType<CommanderAllyVisual>();
            UnitVisualDriver primaryDriver = visual == null
                ? null
                : visual.GetComponentInChildren<UnitVisualDriver>(true);
            _primaryRenderer = primaryDriver == null ? null : primaryDriver.SpriteRenderer;
            _player = visual == null ? null : visual.GetComponentInParent<PlayerController>();
            _commanderAttack = _player == null ? null : _player.GetComponent<CommanderAttack>();
            if (visual == null
                || primaryDriver == null
                || _primaryRenderer == null
                || _player == null
                || _commanderAttack == null)
            {
                return;
            }

            if (NextAttackTimeField == null)
            {
                Debug.LogError(
                    "[CommanderMotionPolishPrototype] Could not read the existing Commander attack timing seam.",
                    this);
                enabled = false;
                return;
            }

            _drivers = visual.GetComponentsInChildren<UnitVisualDriver>(true);
            _driverEnabledStates = new bool[_drivers.Length];
            _renderers = new SpriteRenderer[_drivers.Length];
            _rendererEnabledStates = new bool[_drivers.Length];

            for (int i = 0; i < _drivers.Length; i++)
            {
                UnitVisualDriver driver = _drivers[i];
                if (driver == null)
                    continue;

                _driverEnabledStates[i] = driver.enabled;
                driver.enabled = false;

                SpriteRenderer renderer = driver.SpriteRenderer;
                _renderers[i] = renderer;
                _rendererEnabledStates[i] = renderer != null && renderer.enabled;
                if (renderer != null && renderer != _primaryRenderer)
                    renderer.enabled = false;
            }

            _animators = visual.GetComponentsInChildren<Animator>(true);
            _animatorEnabledStates = new bool[_animators.Length];
            for (int i = 0; i < _animators.Length; i++)
            {
                Animator animator = _animators[i];
                if (animator == null)
                    continue;

                _animatorEnabledStates[i] = animator.enabled;
                animator.enabled = false;
            }

            _originalSprite = _primaryRenderer.sprite;
            _originalLocalPosition = _primaryRenderer.transform.localPosition;
            _originalLocalRotation = _primaryRenderer.transform.localRotation;
            _originalLocalScale = _primaryRenderer.transform.localScale;
            _primaryRenderer.enabled = true;
            _primaryRenderer.sprite = _staticSprite;
            _primaryRenderer.transform.localScale = Vector3.one * _staticLocalScale;
            _observedFireCount = CommanderAttack.DebugFireCount;
            _moveStartedAt = Time.time;
            _wasMoving = _player.MoveDirection.sqrMagnitude > MOVE_THRESHOLD_SQR;
            _isBound = true;

            Debug.Log(
                $"[CommanderMotionPolishPrototype] BOUND sprite={_staticSprite.name} "
                + $"moveLoopFrames={MOVE_LOOP_FRAMES:F0} attackFrames={ATTACK_TOTAL_FRAMES:F0} "
                + $"attackEventFrame={ATTACK_EVENT_FRAME:F0}",
                this);
        }

        private void UpdateAttackSynchronization()
        {
            int currentFireCount = CommanderAttack.DebugFireCount;
            if (currentFireCount != _observedFireCount)
            {
                _observedFireCount = currentFireCount;
                _attackStartedAt = CommanderAttack.DebugLastFireTime - ATTACK_EVENT_FRAME / MOTION_FPS;
                _attackFacingSign = _primaryRenderer != null && _primaryRenderer.flipX ? -1.0f : 1.0f;
                _isAttacking = true;
                _predictedAttackEventAt = CommanderAttack.DebugLastFireTime;
            }

            if (_isAttacking)
            {
                float elapsedFrames = (Time.time - _attackStartedAt) * MOTION_FPS;
                if (elapsedFrames >= ATTACK_TOTAL_FRAMES)
                    _isAttacking = false;
                return;
            }

            if (!CommanderAttack.DebugAttackEnabled)
                return;

            float nextAttackTime = ReadNextAttackTime();
            float anticipationLead = ATTACK_EVENT_FRAME / MOTION_FPS;
            if (nextAttackTime <= 0.0f || Time.time < nextAttackTime - anticipationLead)
                return;

            if (Time.time > nextAttackTime + 0.05f)
                return;

            _attackStartedAt = nextAttackTime - anticipationLead;
            _predictedAttackEventAt = nextAttackTime;
            _attackFacingSign = _primaryRenderer != null && _primaryRenderer.flipX ? -1.0f : 1.0f;
            _isAttacking = true;
        }

        private void UpdateMovementState()
        {
            bool isMoving = _player != null && _player.MoveDirection.sqrMagnitude > MOVE_THRESHOLD_SQR;
            if (isMoving && !_wasMoving)
                _moveStartedAt = Time.time;
            _wasMoving = isMoving;

            if (_isAttacking && Time.time > _predictedAttackEventAt + 0.05f)
            {
                bool eventObserved = Mathf.Abs(CommanderAttack.DebugLastFireTime - _predictedAttackEventAt) <= 0.05f;
                if (!eventObserved && ReadNextAttackTime() > _predictedAttackEventAt + 0.05f)
                    _isAttacking = false;
            }
        }

        private float ReadNextAttackTime()
        {
            object value = NextAttackTimeField.GetValue(_commanderAttack);
            return value is float nextAttackTime ? nextAttackTime : -1.0f;
        }

        private void ApplyMovePose(float frame)
        {
            float y;
            float rotation;
            float scaleX;
            float scaleY;

            if (frame < 6.0f)
            {
                float t = SmoothStep01(frame / 6.0f);
                y = Mathf.Lerp(0.0f, 4.0f, t);
                rotation = Mathf.Lerp(0.0f, 2.0f, t);
                scaleX = Mathf.Lerp(1.0f, 0.98f, t);
                scaleY = Mathf.Lerp(1.0f, 1.03f, t);
            }
            else if (frame < 13.0f)
            {
                float t = SmoothStep01((frame - 6.0f) / 7.0f);
                y = Mathf.Lerp(4.0f, 0.0f, t);
                rotation = Mathf.Lerp(2.0f, -2.0f, t);
                scaleX = Mathf.Lerp(0.98f, 1.02f, t);
                scaleY = Mathf.Lerp(1.03f, 0.98f, t);
            }
            else if (frame < 20.0f)
            {
                float t = SmoothStep01((frame - 13.0f) / 7.0f);
                y = Mathf.Lerp(0.0f, -2.0f, t);
                rotation = Mathf.Lerp(-2.0f, 1.0f, t);
                scaleX = Mathf.Lerp(1.02f, 1.0f, t);
                scaleY = Mathf.Lerp(0.98f, 1.0f, t);
            }
            else
            {
                float t = SmoothStep01((frame - 20.0f) / 8.0f);
                y = Mathf.Lerp(-2.0f, 0.0f, t);
                rotation = Mathf.Lerp(1.0f, 0.0f, t);
                scaleX = 1.0f;
                scaleY = 1.0f;
            }

            ApplyVisualPose(0.0f, y, rotation, scaleX, scaleY);
        }

        private void ApplyAttackPose(float frame)
        {
            float x;
            float rotation;
            float scaleX;
            float scaleY;

            if (frame <= 5.0f)
            {
                float t = SmoothStep01(frame / 5.0f);
                x = Mathf.Lerp(0.0f, -5.0f, t);
                rotation = Mathf.Lerp(0.0f, -4.0f, t);
                scaleX = Mathf.Lerp(1.0f, 1.04f, t);
                scaleY = Mathf.Lerp(1.0f, 0.96f, t);
            }
            else if (frame <= 9.0f)
            {
                float t = EaseOutCubic((frame - 6.0f) / 3.0f);
                x = Mathf.Lerp(-5.0f, 0.0f, t);
                rotation = Mathf.Lerp(-4.0f, 8.0f, t);
                scaleX = Mathf.Lerp(1.04f, 0.96f, t);
                scaleY = Mathf.Lerp(0.96f, 1.05f, t);
            }
            else if (frame <= 10.0f)
            {
                x = 0.0f;
                rotation = 8.0f;
                scaleX = 0.96f;
                scaleY = 1.05f;
            }
            else if (frame <= 12.0f)
            {
                float t = SmoothStep01(frame - 11.0f);
                x = 0.0f;
                rotation = Mathf.Lerp(8.0f, 11.0f, t);
                scaleX = 0.96f;
                scaleY = 1.05f;
            }
            else if (frame <= 16.0f)
            {
                float t = EaseOutCubic((frame - 13.0f) / 3.0f);
                x = 0.0f;
                rotation = Mathf.Lerp(11.0f, 6.0f, t);
                scaleX = Mathf.Lerp(0.96f, 0.98f, t);
                scaleY = Mathf.Lerp(1.05f, 1.02f, t);
            }
            else
            {
                float t = EaseOutBack((frame - 16.0f) / 5.0f);
                x = 0.0f;
                rotation = Mathf.LerpUnclamped(6.0f, 0.0f, t);
                scaleX = Mathf.LerpUnclamped(0.98f, 1.0f, t);
                scaleY = Mathf.LerpUnclamped(1.02f, 1.0f, t);
            }

            ApplyVisualPose(
                x * _attackFacingSign,
                0.0f,
                rotation * _attackFacingSign,
                scaleX,
                scaleY);
        }

        private void ApplyVisualPose(
            float pixelX,
            float pixelY,
            float rotationDegrees,
            float scaleX,
            float scaleY)
        {
            Transform visual = _primaryRenderer.transform;
            visual.localPosition = _originalLocalPosition + ResolveLocalPixelOffset(pixelX, pixelY);
            visual.localRotation = _originalLocalRotation * Quaternion.Euler(0.0f, 0.0f, rotationDegrees);
            visual.localScale = Vector3.Scale(
                Vector3.one * _staticLocalScale,
                new Vector3(scaleX, scaleY, 1.0f));
        }

        private Vector3 ResolveLocalPixelOffset(float pixelX, float pixelY)
        {
            Camera camera = Camera.main;
            if (camera == null || Screen.height <= 0)
                return Vector3.zero;

            float worldUnitsPerPixel = camera.orthographic
                ? camera.orthographicSize * 2.0f / Screen.height
                : Mathf.Max(0.0001f, _staticLocalScale / _staticSprite.pixelsPerUnit);
            Vector3 worldOffset = camera.transform.right * (pixelX * worldUnitsPerPixel)
                + camera.transform.up * (pixelY * worldUnitsPerPixel);
            Transform parent = _primaryRenderer.transform.parent;
            Vector3 localOffset = parent == null ? worldOffset : parent.InverseTransformVector(worldOffset);
            localOffset.z = 0.0f;
            return localOffset;
        }

        private static float SmoothStep01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3.0f - 2.0f * t);
        }

        private static float EaseOutCubic(float value)
        {
            float t = Mathf.Clamp01(value);
            float inverse = 1.0f - t;
            return 1.0f - inverse * inverse * inverse;
        }

        private static float EaseOutBack(float value)
        {
            float t = Mathf.Clamp01(value);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1.0f;
            float shifted = t - 1.0f;
            return 1.0f + c3 * shifted * shifted * shifted + c1 * shifted * shifted;
        }

        private void RestoreCommanderVisual()
        {
            if (!_isBound)
                return;

            if (_primaryRenderer != null)
            {
                _primaryRenderer.sprite = _originalSprite;
                _primaryRenderer.transform.localPosition = _originalLocalPosition;
                _primaryRenderer.transform.localRotation = _originalLocalRotation;
                _primaryRenderer.transform.localScale = _originalLocalScale;
            }

            for (int i = 0; i < _renderers.Length && i < _rendererEnabledStates.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].enabled = _rendererEnabledStates[i];
            }

            for (int i = 0; i < _animators.Length && i < _animatorEnabledStates.Length; i++)
            {
                if (_animators[i] != null)
                    _animators[i].enabled = _animatorEnabledStates[i];
            }

            for (int i = 0; i < _drivers.Length && i < _driverEnabledStates.Length; i++)
            {
                if (_drivers[i] != null)
                    _drivers[i].enabled = _driverEnabledStates[i];
            }

            _isBound = false;
        }
    }
}
