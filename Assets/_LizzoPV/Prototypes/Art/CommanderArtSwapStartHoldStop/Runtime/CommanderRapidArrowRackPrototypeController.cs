using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Units;
using UnityEngine;

namespace Lizzo.PV.Prototypes.CommanderArtSwap
{
    [DisallowMultipleComponent]
    public sealed class CommanderRapidArrowRackPrototypeController : MonoBehaviour
    {
        private const int StagedArrowCount = 3;
        private const float AimDurationSeconds = 0.17f;
        private const float LaunchDurationSeconds = 0.65f;
        private const float LaunchDistance = 10.0f;
        private const float TargetMarkerRadius = 0.62f;
        private const float TargetMarkerWidth = 0.075f;

        private static readonly Vector3[] RestLocalPositions =
        {
            new Vector3(0.18f, 0.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(-0.18f, 0.0f, 0.0f),
        };

        private static readonly Vector3[] AimedLocalPositions =
        {
            new Vector3(0.18f, 0.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(-0.18f, 0.0f, 0.0f),
        };

        private static readonly float[] RestAngles = { 90.0f, 90.0f, 90.0f };

        private readonly SpriteRenderer[] _stagedRenderers = new SpriteRenderer[StagedArrowCount];
        private Transform _rackRoot;
        private Transform _launchedArrow;
        private Transform _targetMarker;
        private LineRenderer _targetMarkerLine;
        private Material _targetMarkerMaterial;
        private MonsterController _target;
        private Vector2 _aimDirection = Vector2.up;
        private float _targetAngleDegrees;
        private Vector3 _targetPositionSnapshot;
        private Vector3 _launchOrigin;
        private float _closestApproachDistance = float.PositiveInfinity;
        private float _aimStartedAt = float.NegativeInfinity;
        private float _launchStartedAt = float.NegativeInfinity;
        private bool _isAiming;
        private bool _hasLaunched;

        public bool TryBind(PlayerController player, SpriteRenderer commanderRenderer)
        {
            if (player == null || commanderRenderer == null)
                return false;

            if (!PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog)
                || catalog.Projectiles == null
                || !catalog.Projectiles.TryGetVisual(
                    CombatProjectilePresentationIds.CommanderRapidCrossbow,
                    out ProjectilePresentationCatalog.VisualDefinition visual)
                || visual.BodySprite == null)
            {
                Debug.LogError(
                    "[RapidArrowRackPrototype] Could not resolve commander_rapid_crossbow through the live PresentationCatalog.",
                    this);
                return false;
            }

            SpriteRenderer shellRenderer = catalog.Projectiles.StraightProjectileShell == null
                ? null
                : catalog.Projectiles.StraightProjectileShell.GetComponentInChildren<SpriteRenderer>(true);

            GameObject rootObject = new GameObject("PROTOTYPE_RapidArrowRack");
            _rackRoot = rootObject.transform;
            _rackRoot.SetParent(player.transform, false);

            for (int i = 0; i < StagedArrowCount; i++)
            {
                GameObject arrowObject = new GameObject($"StagedArrow_{i + 1}");
                Transform arrowTransform = arrowObject.transform;
                arrowTransform.SetParent(_rackRoot, false);
                arrowTransform.localScale = visual.Scale;

                SpriteRenderer renderer = arrowObject.AddComponent<SpriteRenderer>();
                renderer.sprite = visual.BodySprite;
                renderer.color = visual.Tint;
                renderer.sharedMaterial = shellRenderer == null
                    ? commanderRenderer.sharedMaterial
                    : shellRenderer.sharedMaterial;
                renderer.sortingLayerID = commanderRenderer.sortingLayerID;
                renderer.sortingOrder = commanderRenderer.sortingOrder + 2;
                renderer.maskInteraction = commanderRenderer.maskInteraction;
                _stagedRenderers[i] = renderer;
            }

            ApplyRackDirection(Vector2.up);
            ApplyStagedPose(0.0f);
            if (!CreateTargetMarker(commanderRenderer))
                return false;
            Debug.Log(
                $"[RapidArrowRackPrototype] STAGED count={CountStagedArrows()} "
                + $"presentation={CombatProjectilePresentationIds.CommanderRapidCrossbow} "
                + $"sprite={visual.BodySprite.name} scale={visual.Scale.x:F3}",
                this);
            return true;
        }

        public bool BeginAttack(MonsterController target, out Vector2 initialAimDirection)
        {
            initialAimDirection = Vector2.zero;
            if (_rackRoot == null
                || _hasLaunched
                || target == null
                || !target.IsValid()
                || target.Hp <= 0)
            {
                return false;
            }

            _target = target;
            _targetPositionSnapshot = target.transform.position;
            UpdateLiveAim();
            initialAimDirection = _aimDirection;
            ShowTargetMarker();
            _isAiming = true;
            _aimStartedAt = Time.time;
            Debug.Log(
                $"[RapidArrowRackPrototype] AIM started count={CountStagedArrows()} "
                + $"target={target.name} targetInstanceId={target.GetInstanceID()} "
                + $"spawnSequence={target.SpawnSequence} targetPosition={target.transform.position} "
                + $"direction={_aimDirection}",
                this);
            return true;
        }

        public bool PrepareNextVolley()
        {
            if (_rackRoot == null || _isAiming || _launchedArrow != null)
                return false;

            for (int i = 0; i < StagedArrowCount; i++)
            {
                SpriteRenderer renderer = _stagedRenderers[i];
                if (renderer == null)
                    return false;

                renderer.transform.SetParent(_rackRoot, false);
                renderer.gameObject.SetActive(true);
            }

            _target = null;
            _targetPositionSnapshot = Vector3.zero;
            _launchOrigin = Vector3.zero;
            _closestApproachDistance = float.PositiveInfinity;
            _aimStartedAt = float.NegativeInfinity;
            _launchStartedAt = float.NegativeInfinity;
            _hasLaunched = false;
            ApplyRackDirection(Vector2.up);
            ApplyStagedPose(0.0f);
            HideTargetMarker();
            Debug.Log($"[RapidArrowRackPrototype] RELOADED count={CountStagedArrows()}", this);
            return CountStagedArrows() == StagedArrowCount;
        }

        public bool LaunchLeadArrow()
        {
            if (_rackRoot == null || _hasLaunched || _stagedRenderers[0] == null)
                return false;

            UpdateLiveAim();
            ApplyStagedPose(1.0f);
            Transform leadArrow = _stagedRenderers[0].transform;
            _launchOrigin = leadArrow.position;
            _targetPositionSnapshot = ResolveTargetPosition();
            Vector2 launchDirection = _targetPositionSnapshot - _launchOrigin;
            if (launchDirection.sqrMagnitude <= 0.0001f)
                return false;

            _hasLaunched = true;
            _isAiming = false;
            _launchedArrow = leadArrow;
            _aimDirection = launchDirection.normalized;
            _targetAngleDegrees = Mathf.Atan2(_aimDirection.y, _aimDirection.x) * Mathf.Rad2Deg;
            _launchedArrow.rotation = Quaternion.Euler(0.0f, 0.0f, _targetAngleDegrees);
            _launchedArrow.SetParent(null, true);
            _launchStartedAt = Time.time;
            _closestApproachDistance = Vector3.Distance(_launchOrigin, _targetPositionSnapshot);

            int remaining = CountStagedArrows();
            Debug.Log(
                $"[RapidArrowRackPrototype] LAUNCH origin={_launchOrigin} targetSnapshot={_targetPositionSnapshot} "
                + $"direction={_aimDirection} countBefore=3 countAfter={remaining} "
                + "visualOnly=true collision=false damage=false centerDuplicate=false",
                this);
            return remaining == 2;
        }

        private void Update()
        {
            if (_isAiming)
            {
                UpdateLiveAim();
                float aimProgress = Mathf.Clamp01((Time.time - _aimStartedAt) / AimDurationSeconds);
                ApplyStagedPose(aimProgress);
            }

            UpdateTargetMarkerPosition();

            if (_launchedArrow == null)
                return;

            float launchProgress = Mathf.Clamp01((Time.time - _launchStartedAt) / LaunchDurationSeconds);
            float easedProgress = 1.0f - Mathf.Pow(1.0f - launchProgress, 2.0f);
            _launchedArrow.position = _launchOrigin
                + (Vector3)(_aimDirection * (LaunchDistance * easedProgress));
            float targetDistance = Vector3.Distance(_launchedArrow.position, ResolveTargetPosition());
            if (targetDistance < _closestApproachDistance)
                _closestApproachDistance = targetDistance;
            if (launchProgress >= 1.0f)
            {
                _launchedArrow.gameObject.SetActive(false);
                _launchedArrow = null;
                HideTargetMarker();
                Debug.Log(
                    $"[RapidArrowRackPrototype] LAUNCH_COMPLETE remaining={CountStagedArrows()} "
                    + $"closestApproachWorld={_closestApproachDistance:F3}",
                    this);
            }
        }

        private void OnDestroy()
        {
            if (_launchedArrow != null)
                Destroy(_launchedArrow.gameObject);
            if (_targetMarkerMaterial != null)
                Destroy(_targetMarkerMaterial);
        }

        private bool CreateTargetMarker(SpriteRenderer commanderRenderer)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("[RapidArrowRackPrototype] QA target marker requires the runtime Sprites/Default shader.", this);
                return false;
            }

            _targetMarkerMaterial = new Material(shader)
            {
                color = new Color(1.0f, 0.82f, 0.08f, 1.0f),
            };
            GameObject markerObject = new GameObject("PROTOTYPE_SelectedTargetDiamond");
            _targetMarker = markerObject.transform;
            _targetMarker.SetParent(transform, false);
            _targetMarkerLine = markerObject.AddComponent<LineRenderer>();
            _targetMarkerLine.useWorldSpace = false;
            _targetMarkerLine.loop = true;
            _targetMarkerLine.positionCount = 4;
            _targetMarkerLine.SetPosition(0, new Vector3(0.0f, TargetMarkerRadius, 0.0f));
            _targetMarkerLine.SetPosition(1, new Vector3(TargetMarkerRadius, 0.0f, 0.0f));
            _targetMarkerLine.SetPosition(2, new Vector3(0.0f, -TargetMarkerRadius, 0.0f));
            _targetMarkerLine.SetPosition(3, new Vector3(-TargetMarkerRadius, 0.0f, 0.0f));
            _targetMarkerLine.startWidth = TargetMarkerWidth;
            _targetMarkerLine.endWidth = TargetMarkerWidth;
            _targetMarkerLine.numCornerVertices = 2;
            _targetMarkerLine.numCapVertices = 2;
            _targetMarkerLine.sharedMaterial = _targetMarkerMaterial;
            _targetMarkerLine.startColor = _targetMarkerMaterial.color;
            _targetMarkerLine.endColor = _targetMarkerMaterial.color;
            _targetMarkerLine.sortingLayerID = commanderRenderer.sortingLayerID;
            _targetMarkerLine.sortingOrder = commanderRenderer.sortingOrder + 20;
            markerObject.SetActive(false);
            return true;
        }

        private void ShowTargetMarker()
        {
            if (_targetMarker == null)
                return;
            _targetMarker.gameObject.SetActive(true);
            UpdateTargetMarkerPosition();
        }

        private void HideTargetMarker()
        {
            if (_targetMarker != null)
                _targetMarker.gameObject.SetActive(false);
        }

        private void UpdateTargetMarkerPosition()
        {
            if (_targetMarker == null || !_targetMarker.gameObject.activeSelf)
                return;
            _targetMarker.position = ResolveTargetPosition();
            _targetMarker.rotation = Quaternion.identity;
        }

        private void UpdateLiveAim()
        {
            if (_stagedRenderers[0] == null)
                return;
            _targetPositionSnapshot = ResolveTargetPosition();
            Vector2 liveDirection = _targetPositionSnapshot - _stagedRenderers[0].transform.position;
            if (liveDirection.sqrMagnitude > 0.0001f)
                ApplyRackDirection(liveDirection.normalized);
        }

        private Vector3 ResolveTargetPosition()
        {
            if (_target != null && _target.IsValid() && _target.Hp > 0)
                return _target.transform.position;
            return _targetPositionSnapshot;
        }

        private void ApplyRackDirection(Vector2 targetDirection)
        {
            _aimDirection = targetDirection.sqrMagnitude > 0.0001f
                ? targetDirection.normalized
                : Vector2.up;
            _targetAngleDegrees = Mathf.Atan2(_aimDirection.y, _aimDirection.x) * Mathf.Rad2Deg;

            // Flying-sword staging reads from the Commander's screen-left shoulder/face side.
            // The rack stays upright. Only the lead arrow turns toward the target during the 170 ms aim.
            _rackRoot.localPosition = new Vector3(-0.74f, 0.66f, 0.0f);
            _rackRoot.localRotation = Quaternion.identity;
        }

        private void ApplyStagedPose(float aimProgress)
        {
            for (int i = 0; i < StagedArrowCount; i++)
            {
                SpriteRenderer renderer = _stagedRenderers[i];
                if (renderer == null || renderer.transform == _launchedArrow)
                    continue;

                renderer.transform.localPosition = Vector3.Lerp(
                    RestLocalPositions[i],
                    AimedLocalPositions[i],
                    aimProgress);
                float aimedAngle = i == 0 ? _targetAngleDegrees : RestAngles[i];
                float angle = Mathf.LerpAngle(RestAngles[i], aimedAngle, aimProgress);
                renderer.transform.localRotation = Quaternion.Euler(0.0f, 0.0f, angle);
            }
        }

        private int CountStagedArrows()
        {
            int count = 0;
            for (int i = 0; i < StagedArrowCount; i++)
            {
                SpriteRenderer renderer = _stagedRenderers[i];
                if (renderer != null
                    && renderer.gameObject.activeSelf
                    && renderer.transform.parent == _rackRoot)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
