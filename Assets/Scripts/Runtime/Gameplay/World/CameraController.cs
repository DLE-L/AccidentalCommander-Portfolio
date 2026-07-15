using System.Collections;
using System.Collections.Generic;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    RunServices _services;

public void Initialize(RunServices services)
    {
        _services = services ?? throw new System.ArgumentNullException(nameof(services));
        InitializeFraming();
    }

    const float CloseOrthographicSize = 4.0f;
    const float FirstRecruitOrthographicSize = 5.2f;
    const float PartyOrthographicSize = 6.5f;
    const float DenseOrthographicSize = 7.15f;
    const float BossOrthographicSize = 8.1f;
    const float ViewSampleIntervalSeconds = 0.25f;
    const float ZoomSmoothTimeSeconds = 0.9f;
    const float FocusZoomSmoothTimeSeconds = 0.28f;
    const int PartyExpandCompanionCount = 3;
    const int DenseEnemyCountStart = 35;
    const int DenseEnemyCountEnd = 85;

    public GameObject Target;
    Camera _camera;
    [SerializeField] CameraVisibilityZone _visibilityZone;
    float _targetOrthographicSize = CloseOrthographicSize;
    float _zoomVelocity;
    float _nextViewSampleTime;
    Vector3 _focusShotPosition;
    float _focusShotUntil;
    bool _hasFocusShot;

    public static void PlayFocusShot(Vector3 position, float seconds)
    {
        if (seconds <= 0.0f)
            return;

        Camera camera = Camera.main;
        CameraController controller = camera == null ? null : camera.GetComponent<CameraController>();
        if (controller == null)
        {
            Debug.LogError("P0 boss intro requires Main Camera with CameraController.");
            return;
        }

        controller.BeginFocusShot(position, seconds);
    }

void Start()
    {
        if (_services != null)
            InitializeFraming();
    }

void InitializeFraming()
    {
        _camera ??= GetComponent<Camera>();
        if (_camera == null || _camera.orthographic == false)
            return;

        _targetOrthographicSize = ResolveTargetOrthographicSize();
        _camera.orthographicSize = _targetOrthographicSize;
        InitializeVisibilityZone();
    }


    void LateUpdate()
    {
        if (IsFocusShotActive())
        {
            ApplyFocusShot();
            return;
        }

        if (Target == null)
            return;

        Vector3 targetPosition = Target.transform.position;
        transform.position = new Vector3(targetPosition.x, targetPosition.y, -10);
        UpdateOrthographicSize();
        if (_visibilityZone != null)
            _visibilityZone.RefreshFromCamera();
    }

    void BeginFocusShot(Vector3 position, float seconds)
    {
        _focusShotPosition = position;
        _focusShotUntil = Time.unscaledTime + seconds;
        _hasFocusShot = true;
        _nextViewSampleTime = 0.0f;
    }

    bool IsFocusShotActive()
    {
        if (_hasFocusShot == false)
            return false;

        if (Time.unscaledTime < _focusShotUntil)
            return true;

        _hasFocusShot = false;
        return false;
    }

    void ApplyFocusShot()
    {
        transform.position = new Vector3(_focusShotPosition.x, _focusShotPosition.y, -10.0f);
        if (_camera != null && _camera.orthographic)
        {
            _targetOrthographicSize = BossOrthographicSize;
            _camera.orthographicSize = Mathf.SmoothDamp(
                _camera.orthographicSize,
                _targetOrthographicSize,
                ref _zoomVelocity,
                FocusZoomSmoothTimeSeconds,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
        }

        if (_visibilityZone != null)
            _visibilityZone.RefreshFromCamera();
    }

    void UpdateOrthographicSize()
    {
        if (_camera == null || _camera.orthographic == false)
            return;

        if (Time.time >= _nextViewSampleTime)
        {
            _nextViewSampleTime = Time.time + ViewSampleIntervalSeconds;
            _targetOrthographicSize = ResolveTargetOrthographicSize();
        }

        _camera.orthographicSize = Mathf.SmoothDamp(
            _camera.orthographicSize,
            _targetOrthographicSize,
            ref _zoomVelocity,
            ZoomSmoothTimeSeconds);
    }

    float ResolveTargetOrthographicSize()
    {
        float targetSize = CloseOrthographicSize;
        PartyService party = _services?.Party;
        if (party == null)
        {
            Debug.LogError("[CameraController] RunServices must be initialized before resolving camera framing.", this);
            return targetSize;
        }

        int companionCount = party.ActiveCompanionSlotCount;
        if (companionCount > 0)
            targetSize = Mathf.Max(targetSize, FirstRecruitOrthographicSize);

        if (companionCount >= PartyExpandCompanionCount || party.IsGuardSquadActivated)
            targetSize = Mathf.Max(targetSize, PartyOrthographicSize);

        int enemyCount = _services?.Registry?.Enemies?.Count ?? 0;
        if (enemyCount >= DenseEnemyCountStart)
        {
            float densityRatio = Mathf.InverseLerp(DenseEnemyCountStart, DenseEnemyCountEnd, enemyCount);
            float densitySize = Mathf.Lerp(PartyOrthographicSize, DenseOrthographicSize, densityRatio);
            targetSize = Mathf.Max(targetSize, densitySize);
        }

        if (HasActiveBoss())
            targetSize = Mathf.Max(targetSize, BossOrthographicSize);

        return targetSize;
    }

    static bool HasActiveBoss()
    {
        HungryGiantBehaviour boss = HungryGiantBehaviour.Current;
        return boss != null && boss.isActiveAndEnabled;
    }

    void InitializeVisibilityZone()
    {
        if (_visibilityZone == null)
        {
            Debug.LogError("[CameraController] Authored CameraVisibilityZone reference is required.", this);
            return;
        }

        _visibilityZone.Setup(_camera);
    }
}
