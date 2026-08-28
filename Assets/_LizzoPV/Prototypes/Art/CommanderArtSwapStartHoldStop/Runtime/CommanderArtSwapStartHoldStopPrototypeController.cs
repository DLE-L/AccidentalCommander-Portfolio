using System;
using System.IO;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Prototypes.CommanderArtSwap
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class CommanderArtSwapStartHoldStopPrototypeController : MonoBehaviour
    {
        private const float MOVE_THRESHOLD_SQR = 0.001f;
        private const float START_DURATION_SECONDS = 0.24f;
        private const float STOP_DURATION_SECONDS = 0.24f;
        private const float IDLE_FRAME_SECONDS = 0.15f;
        private const float FIXTURE_MOVE_STARTED_AT_SECONDS = 1.0f;
        private const float FIXTURE_MOVE_STOPPED_AT_SECONDS = 2.5f;
        private const float FIXTURE_FIRST_ATTACK_AT_SECONDS = 3.2f;
        private const float FIXTURE_PROJECTILE_CUE_DELAY_SECONDS = 0.17f;
        private const float FIXTURE_INTER_ATTACK_GAP_SECONDS = 0.55f;
        private const float RAPID_CROSSBOW_TARGET_RANGE = 7.5f;
        private const int FIXTURE_REPEATED_ATTACK_EVIDENCE_COUNT = 4;

        private static readonly float[] AttackFrameDurations =
        {
            0.08f,
            0.09f,
            0.10f,
            0.14f,
            0.11f,
            0.08f,
        };

        [Header("Imported prototype sprites")]
        [SerializeField]
        private Sprite[] _idleSprites = Array.Empty<Sprite>();
        [SerializeField]
        private Sprite[] _attackSprites = Array.Empty<Sprite>();
        [SerializeField]
        private Sprite _moveIdleSprite;
        [SerializeField]
        private Sprite[] _moveStartSprites = Array.Empty<Sprite>();
        [SerializeField]
        private Sprite _moveHoldSprite;
        [SerializeField]
        private Sprite[] _moveStopSprites = Array.Empty<Sprite>();
        [SerializeField]
        private Sprite _moveSettledSprite;

        [Header("Measured source visual")]
        [SerializeField]
        private float _referenceVisibleHeightLocal;
        [SerializeField]
        private float _referenceVisibleBottomLocal;
        [SerializeField]
        private float _prototypeVisibleHeightLocal;
        [SerializeField]
        private float _prototypeVisibleBottomLocal;

        [Header("Human Gate fixture")]
        [SerializeField]
        private bool _runAutomaticFixture = true;

        private PlayerController _player;
        private UnitVisualDriver _sourceDriver;
        private SpriteRenderer _sourceRenderer;
        private SpriteRenderer _overrideRenderer;
        private Animator _sourceAnimator;
        private SpriteRenderer[] _hiddenRenderers = Array.Empty<SpriteRenderer>();
        private bool[] _hiddenRendererStates = Array.Empty<bool>();
        private MotionPhase _motionPhase;
        private float _motionPhaseStartedAt;
        private float _idleStartedAt;
        private bool _wasMoving;
        private bool _wasSourceAttack;
        private float _attackStartedAt = float.NegativeInfinity;
        private bool _fixtureAttackFlipX;
        private float _boundAt;
        private CommanderAttack _commanderAttack;
        private CommanderRapidArrowRackPrototypeController _rapidArrowRack;
        private MonsterController _fixtureTarget;
        private bool _previousDebugAttackEnabled;
        private bool _ownsDebugAttackSuppression;
        private bool _fixtureAttackActive;
        private bool _fixtureProjectileCueTriggered;
        private int _fixtureAttackCount;
        private float _fixtureNextAttackAtSeconds = FIXTURE_FIRST_ATTACK_AT_SECONDS;
        private bool _fixtureRepeatedAttackEvidenceLogged;
        private bool _fixtureFinished;
        private int _nextCaptureIndex;
        private string _captureDirectory;
        private readonly float[] _captureTimes = { 0.60f, 1.12f, 1.75f, 2.62f, 3.38f, 4.53f, 6.83f };
        private readonly string[] _captureNames =
        {
            "01-idle",
            "02-move-start",
            "03-move-hold",
            "04-move-stop-idle",
            "05-repeat-attack-1",
            "06-repeat-attack-2",
            "07-repeat-attack-4",
        };

        private enum MotionPhase
        {
            Idle,
            Starting,
            Holding,
            Stopping,
        }

        private void Update()
        {
            if (_player == null)
            {
                TryBindCommander();
                return;
            }

            if (_runAutomaticFixture)
                UpdateAutomaticFixture();

            bool isMoving = _player.MoveDirection.sqrMagnitude > MOVE_THRESHOLD_SQR;
            UpdateMovementState(isMoving);
        }

        private void LateUpdate()
        {
            if (_overrideRenderer == null || _sourceRenderer == null)
                return;

            _overrideRenderer.flipX = _sourceRenderer.flipX;
            UpdateAttackState();

            float attackElapsed = Time.time - _attackStartedAt;
            if (_fixtureAttackActive && attackElapsed < ResolveAttackDuration())
                _overrideRenderer.flipX = _fixtureAttackFlipX;
            if (attackElapsed < ResolveAttackDuration())
                ApplyAttackFrame(attackElapsed);
            else
                ApplyMotionFrame();

            if (_runAutomaticFixture)
                CaptureScheduledEvidence();
        }

        private void OnDestroy()
        {
            RestoreDebugAttackState();

            for (int i = 0; i < _hiddenRenderers.Length && i < _hiddenRendererStates.Length; i++)
            {
                if (_hiddenRenderers[i] != null)
                    _hiddenRenderers[i].enabled = _hiddenRendererStates[i];
            }
        }

        private void TryBindCommander()
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player == null)
                return;

            CommanderAllyVisual visual = player.GetComponentInChildren<CommanderAllyVisual>(true);
            UnitVisualDriver driver = visual == null
                ? player.GetComponentInChildren<UnitVisualDriver>(true)
                : visual.GetComponentInChildren<UnitVisualDriver>(true);
            SpriteRenderer sourceRenderer = driver == null ? null : driver.SpriteRenderer;
            if (driver == null || sourceRenderer == null || sourceRenderer.sprite == null)
            {
                Debug.LogError("[CommanderArtSwapPrototype] Could not resolve the live Commander UnitVisualDriver/SpriteRenderer.", this);
                enabled = false;
                return;
            }

            if (!ValidatePrototypeInputs())
            {
                enabled = false;
                return;
            }

            _player = player;
            _commanderAttack = player.GetComponent<CommanderAttack>();
            if (_runAutomaticFixture && _commanderAttack == null)
            {
                Debug.LogError("[CommanderArtSwapPrototype] Targeting fixture requires the live CommanderAttack component.", this);
                enabled = false;
                return;
            }
            if (_runAutomaticFixture)
            {
                _previousDebugAttackEnabled = CommanderAttack.DebugAttackEnabled;
                CommanderAttack.DebugAttackEnabled = false;
                _ownsDebugAttackSuppression = true;
            }

            _sourceDriver = driver;
            _sourceRenderer = sourceRenderer;
            _sourceAnimator = driver.GetComponent<Animator>();
            CreateOverrideRenderer();
            HideAuthoredDriverRenderers(player);

            _rapidArrowRack = GetComponent<CommanderRapidArrowRackPrototypeController>();
            if (_rapidArrowRack == null)
                _rapidArrowRack = gameObject.AddComponent<CommanderRapidArrowRackPrototypeController>();
            if (!_rapidArrowRack.TryBind(player, _sourceRenderer))
            {
                RestoreDebugAttackState();
                enabled = false;
                return;
            }

            _wasMoving = player.MoveDirection.sqrMagnitude > MOVE_THRESHOLD_SQR;
            _motionPhase = _wasMoving ? MotionPhase.Holding : MotionPhase.Idle;
            _motionPhaseStartedAt = Time.time;
            _idleStartedAt = Time.time;
            _boundAt = Time.time;
            _captureDirectory = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "Artifacts",
                "PrototypeCaptures",
                "CommanderArtSwapStartHoldStop-v001"));
            Directory.CreateDirectory(_captureDirectory);

            float targetWorldHeight = _referenceVisibleHeightLocal * Mathf.Abs(_sourceRenderer.transform.lossyScale.y);
            float actualWorldHeight = _prototypeVisibleHeightLocal
                * Mathf.Abs(_overrideRenderer.transform.lossyScale.y);
            Debug.Log(
                $"[CommanderArtSwapPrototype] BOUND player={player.name} source={sourceRenderer.sprite.name} "
                + $"targetVisibleWorldHeight={targetWorldHeight:F4} overrideVisibleWorldHeight={actualWorldHeight:F4} "
                + $"scale={_overrideRenderer.transform.localScale.x:F5} capture={_captureDirectory}",
                this);
        }

        private bool ValidatePrototypeInputs()
        {
            bool valid = _idleSprites != null && _idleSprites.Length == 12
                && _attackSprites != null && _attackSprites.Length == 6
                && _moveStartSprites != null && _moveStartSprites.Length == 2
                && _moveStopSprites != null && _moveStopSprites.Length == 2
                && _moveIdleSprite != null
                && _moveHoldSprite != null
                && _moveSettledSprite != null
                && _referenceVisibleHeightLocal > 0.0f
                && _prototypeVisibleHeightLocal > 0.0f;
            if (!valid)
                Debug.LogError("[CommanderArtSwapPrototype] Prototype sprite or measurement contract is incomplete.", this);
            return valid;
        }

        private void CreateOverrideRenderer()
        {
            GameObject overrideObject = new GameObject("PROTOTYPE_CommanderArtOverride");
            overrideObject.transform.SetParent(_sourceRenderer.transform, false);
            float scale = _referenceVisibleHeightLocal / _prototypeVisibleHeightLocal;
            overrideObject.transform.localScale = Vector3.one * scale;
            overrideObject.transform.localPosition = new Vector3(
                0.0f,
                _referenceVisibleBottomLocal - scale * _prototypeVisibleBottomLocal,
                0.0f);

            _overrideRenderer = overrideObject.AddComponent<SpriteRenderer>();
            _overrideRenderer.sprite = _moveIdleSprite;
            _overrideRenderer.sharedMaterial = _sourceRenderer.sharedMaterial;
            _overrideRenderer.color = _sourceRenderer.color;
            _overrideRenderer.flipX = _sourceRenderer.flipX;
            _overrideRenderer.flipY = _sourceRenderer.flipY;
            _overrideRenderer.sortingLayerID = _sourceRenderer.sortingLayerID;
            _overrideRenderer.sortingOrder = _sourceRenderer.sortingOrder;
            _overrideRenderer.maskInteraction = _sourceRenderer.maskInteraction;
        }

        private void HideAuthoredDriverRenderers(PlayerController player)
        {
            UnitVisualDriver[] drivers = player.GetComponentsInChildren<UnitVisualDriver>(true);
            _hiddenRenderers = new SpriteRenderer[drivers.Length];
            _hiddenRendererStates = new bool[drivers.Length];
            for (int i = 0; i < drivers.Length; i++)
            {
                SpriteRenderer renderer = drivers[i] == null ? null : drivers[i].SpriteRenderer;
                _hiddenRenderers[i] = renderer;
                _hiddenRendererStates[i] = renderer != null && renderer.enabled;
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        private void UpdateAutomaticFixture()
        {
            if (_fixtureFinished)
            {
                _player.SetMoveDirection(Vector2.zero);
                return;
            }

            float elapsed = Time.time - _boundAt;
            bool entryMoving = elapsed >= FIXTURE_MOVE_STARTED_AT_SECONDS
                && elapsed < FIXTURE_MOVE_STOPPED_AT_SECONDS;
            _player.SetMoveDirection(entryMoving ? Vector2.right : Vector2.zero);

            if (_fixtureAttackActive)
            {
                float attackElapsed = Time.time - _attackStartedAt;
                if (!_fixtureProjectileCueTriggered
                    && attackElapsed >= FIXTURE_PROJECTILE_CUE_DELAY_SECONDS)
                {
                    _fixtureProjectileCueTriggered = true;
                    bool fired = _rapidArrowRack.LaunchLeadArrow();
                    if (!fired)
                    {
                        Debug.LogError(
                            $"[CommanderArtSwapPrototype] FIXTURE projectile-cue failed shot={_fixtureAttackCount} "
                            + "at attackElapsedMs=170.",
                            this);
                        return;
                    }

                    Debug.Log(
                        $"[CommanderArtSwapPrototype] FIXTURE projectile-cue fired=true "
                        + $"shot={_fixtureAttackCount} frame=2 attackElapsedMs=170 "
                        + $"target={_fixtureTarget.name} targetInstanceId={_fixtureTarget.GetInstanceID()} visualOnly=true",
                        this);
                }

                if (attackElapsed >= ResolveAttackDuration())
                {
                    _fixtureAttackActive = false;
                    _fixtureNextAttackAtSeconds = elapsed + FIXTURE_INTER_ATTACK_GAP_SECONDS;
                    Debug.Log(
                        $"[CommanderArtSwapPrototype] FIXTURE attack-end shot={_fixtureAttackCount} "
                        + "idle-return=true attackElapsedMs=600",
                        this);
                    if (!_fixtureRepeatedAttackEvidenceLogged
                        && _fixtureAttackCount >= FIXTURE_REPEATED_ATTACK_EVIDENCE_COUNT)
                    {
                        _fixtureRepeatedAttackEvidenceLogged = true;
                        Debug.Log(
                            $"[CommanderArtSwapPrototype] FIXTURE REPEATED_ATTACK_EVIDENCE_PASS "
                            + $"entryMotion=true repeatedTargetedAttacks={_fixtureAttackCount} continuesUntilPlayStop=true",
                            this);
                    }
                }
                return;
            }

            if (elapsed < _fixtureNextAttackAtSeconds)
                return;

            if (_fixtureAttackCount == 0)
            {
                if (!TryStageVisibleTargetFixture())
                    return;
            }
            else if (!_rapidArrowRack.PrepareNextVolley())
            {
                return;
            }

            if (!TryResolveNearestRapidCrossbowTarget(out MonsterController target))
                return;
            if (!_rapidArrowRack.BeginAttack(target, out Vector2 targetDirection))
            {
                Debug.LogError("[CommanderArtSwapPrototype] FIXTURE could not begin live-target aim.", this);
                return;
            }

            _fixtureTarget = target;
            _fixtureAttackActive = true;
            _fixtureProjectileCueTriggered = false;
            _fixtureAttackCount++;
            _attackStartedAt = Time.time;
            _fixtureAttackFlipX = targetDirection.x < 0.0f;
            _player.PlayAttackPose(targetDirection, ResolveAttackDuration());
            Debug.Log(
                $"[CommanderArtSwapPrototype] FIXTURE target-acquired shot={_fixtureAttackCount} "
                + $"name={target.name} instanceId={target.GetInstanceID()} spawnSequence={target.SpawnSequence} "
                + $"commanderPosition={_player.transform.position} targetPosition={target.transform.position} "
                + $"aimDirection={targetDirection} range={RAPID_CROSSBOW_TARGET_RANGE:F1}",
                this);
            Debug.Log(
                $"[CommanderArtSwapPrototype] FIXTURE attack-start shot={_fixtureAttackCount} "
                + "transition=Idle->Aim->Attack durationMs=600 targetAware=true entryMotionComplete=true",
                this);
        }

        private bool TryResolveNearestRapidCrossbowTarget(out MonsterController target)
        {
            target = null;
            RuntimeObjectRegistry registry = _player?.Services?.Registry;
            if (registry?.Enemies == null)
                return false;

            float nearestSqrDistance = RAPID_CROSSBOW_TARGET_RANGE * RAPID_CROSSBOW_TARGET_RANGE;
            Vector3 commanderPosition = _player.transform.position;
            foreach (MonsterController monster in registry.Enemies)
            {
                if (monster == null || !monster.IsValid() || monster.Hp <= 0)
                    continue;

                float sqrDistance = (monster.transform.position - commanderPosition).sqrMagnitude;
                if (sqrDistance > nearestSqrDistance
                    || (target != null && sqrDistance >= nearestSqrDistance))
                {
                    continue;
                }

                nearestSqrDistance = sqrDistance;
                target = monster;
            }

            return target != null;
        }

        private bool TryStageVisibleTargetFixture()
        {
            if (!TryResolveNearestRapidCrossbowTarget(out MonsterController candidate))
                return false;

            RuntimeObjectRegistry registry = _player.Services.Registry;
            Vector3 commanderPosition = _player.transform.position;
            Vector3 stagedPosition = commanderPosition + new Vector3(-1.65f, 0.95f, 0.0f);
            candidate.transform.position = stagedPosition;

            float candidateSqrDistance = (stagedPosition - commanderPosition).sqrMagnitude;
            int displacedIndex = 0;
            foreach (MonsterController monster in registry.Enemies)
            {
                if (monster == null || monster == candidate || !monster.IsValid() || monster.Hp <= 0)
                    continue;

                float sqrDistance = (monster.transform.position - commanderPosition).sqrMagnitude;
                if (sqrDistance > candidateSqrDistance)
                    continue;

                monster.transform.position = commanderPosition
                    + new Vector3(3.4f + displacedIndex * 0.35f, -2.4f - displacedIndex * 0.2f, 0.0f);
                displacedIndex++;
            }

            Debug.Log(
                $"[CommanderArtSwapPrototype] FIXTURE target-staged name={candidate.name} "
                + $"instanceId={candidate.GetInstanceID()} position={stagedPosition} "
                + $"displacedCloserEnemies={displacedIndex} evidenceOnly=true",
                this);
            return true;
        }

        private void RestoreDebugAttackState()
        {
            if (!_ownsDebugAttackSuppression)
                return;

            CommanderAttack.DebugAttackEnabled = _previousDebugAttackEnabled;
            _ownsDebugAttackSuppression = false;
        }

        private void UpdateMovementState(bool isMoving)
        {
            if (isMoving != _wasMoving)
            {
                _wasMoving = isMoving;
                _motionPhase = isMoving ? MotionPhase.Starting : MotionPhase.Stopping;
                _motionPhaseStartedAt = Time.time;
                Debug.Log($"[CommanderArtSwapPrototype] MOTION transition={_motionPhase}", this);
            }

            float elapsed = Time.time - _motionPhaseStartedAt;
            if (_motionPhase == MotionPhase.Starting && elapsed >= START_DURATION_SECONDS)
            {
                _motionPhase = MotionPhase.Holding;
                _motionPhaseStartedAt = Time.time;
                Debug.Log("[CommanderArtSwapPrototype] MOTION transition=Holding static=true", this);
            }
            else if (_motionPhase == MotionPhase.Stopping && elapsed >= STOP_DURATION_SECONDS)
            {
                _motionPhase = MotionPhase.Idle;
                _motionPhaseStartedAt = Time.time;
                _idleStartedAt = Time.time;
                Debug.Log("[CommanderArtSwapPrototype] MOTION transition=Idle recovered=true", this);
            }
        }

        private void UpdateAttackState()
        {
            // The live combat loop auto-attacks continuously. During the short evidence fixture,
            // only the explicit PlayAttackPose call above is allowed to drive the prototype art so
            // Idle/Start/Hold/Stop remain inspectable without changing gameplay or hit logic.
            if (_runAutomaticFixture && !_fixtureFinished)
                return;

            bool sourceAttack = false;
            if (_sourceAnimator != null && _sourceAnimator.isActiveAndEnabled)
            {
                AnimatorStateInfo state = _sourceAnimator.GetCurrentAnimatorStateInfo(0);
                sourceAttack = state.IsName("Base Layer.Attack") || state.IsName("Attack");
            }

            if (sourceAttack && !_wasSourceAttack)
            {
                _attackStartedAt = Time.time;
                Debug.Log("[CommanderArtSwapPrototype] ATTACK command-gesture started", this);
            }

            _wasSourceAttack = sourceAttack;
        }

        private void ApplyMotionFrame()
        {
            switch (_motionPhase)
            {
                case MotionPhase.Starting:
                    _overrideRenderer.sprite = ResolveTimedFrame(_moveStartSprites, Time.time - _motionPhaseStartedAt, START_DURATION_SECONDS);
                    break;
                case MotionPhase.Holding:
                    _overrideRenderer.sprite = _moveHoldSprite;
                    break;
                case MotionPhase.Stopping:
                    _overrideRenderer.sprite = ResolveTimedFrame(_moveStopSprites, Time.time - _motionPhaseStartedAt, STOP_DURATION_SECONDS);
                    break;
                default:
                    int idleFrame = Mathf.FloorToInt((Time.time - _idleStartedAt) / IDLE_FRAME_SECONDS) % _idleSprites.Length;
                    _overrideRenderer.sprite = _idleSprites[idleFrame];
                    break;
            }
        }

        private void ApplyAttackFrame(float elapsed)
        {
            float cursor = 0.0f;
            for (int i = 0; i < AttackFrameDurations.Length; i++)
            {
                cursor += AttackFrameDurations[i];
                if (elapsed < cursor)
                {
                    _overrideRenderer.sprite = _attackSprites[i];
                    return;
                }
            }

            _overrideRenderer.sprite = _attackSprites[_attackSprites.Length - 1];
        }

        private void CaptureScheduledEvidence()
        {
            if (_nextCaptureIndex >= _captureTimes.Length || string.IsNullOrEmpty(_captureDirectory))
                return;

            float elapsed = Time.time - _boundAt;
            if (elapsed < _captureTimes[_nextCaptureIndex])
                return;

            string captureName = _captureNames[_nextCaptureIndex];
            string path = Path.Combine(_captureDirectory, captureName + ".png");
            ScreenCapture.CaptureScreenshot(path, 1);
            Debug.Log(
                $"[CommanderArtSwapPrototype] CAPTURE name={captureName} state={ResolveVisualStateName()} "
                + $"position={_player.transform.position} sprite={_overrideRenderer.sprite.name} path={path}",
                this);
            _nextCaptureIndex++;
        }

        private string ResolveVisualStateName()
        {
            if (Time.time - _attackStartedAt < ResolveAttackDuration())
                return "Attack";
            return _motionPhase.ToString();
        }

        private static Sprite ResolveTimedFrame(Sprite[] sprites, float elapsed, float totalDuration)
        {
            if (sprites == null || sprites.Length == 0)
                return null;
            int frame = Mathf.Clamp(Mathf.FloorToInt(elapsed / totalDuration * sprites.Length), 0, sprites.Length - 1);
            return sprites[frame];
        }

        private static float ResolveAttackDuration()
        {
            float total = 0.0f;
            for (int i = 0; i < AttackFrameDurations.Length; i++)
                total += AttackFrameDurations[i];
            return total;
        }
    }
}
