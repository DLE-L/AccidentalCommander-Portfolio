using UnityEngine;
using Lizzo.PV.Gameplay.World;

public class CameraController : MonoBehaviour
{
    public const float InitialOrthographicSize = 6.0f;
    public const float FinalOrthographicSize = 8.5f;
    public const float TimedZoomDurationSeconds = 300.0f;

    RunServices _services;
    Camera _camera;
    ArenaBounds _arenaBounds;
    [SerializeField] CameraVisibilityZone _visibilityZone;
    float _currentOrthographicSize = InitialOrthographicSize;
    float _scaledGameplaySeconds;
    Vector3 _focusShotPosition;
    float _focusShotUntil;
    bool _hasFocusShot;

    public GameObject Target;
    public IWorldVisibilityQuery VisibilityQuery => _visibilityZone;

    public void BindArenaBounds(ArenaBounds arenaBounds)
    {
        _arenaBounds = arenaBounds ?? throw new System.ArgumentNullException(nameof(arenaBounds));
    }

    public void Initialize(RunServices services)
    {
        _services = services ?? throw new System.ArgumentNullException(nameof(services));
        _scaledGameplaySeconds = 0.0f;
        _currentOrthographicSize = InitialOrthographicSize;
        InitializeFraming();
    }

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

        ApplyCurrentOrthographicSize();
        InitializeVisibilityZone();
    }


    void LateUpdate()
    {
        UpdateTimedOrthographicSize();

        if (IsFocusShotActive())
        {
            ApplyFocusShot();
            return;
        }

        if (Target == null)
            return;

        Vector3 targetPosition = Target.transform.position;
        ApplyCurrentOrthographicSize();
        Vector3 desiredPosition = new Vector3(targetPosition.x, targetPosition.y, -10.0f);
        transform.position = _arenaBounds == null ? desiredPosition : _arenaBounds.ClampFollowCameraCenter(desiredPosition, _camera);
        if (_visibilityZone != null)
            _visibilityZone.RefreshFromCamera();
    }

    void BeginFocusShot(Vector3 position, float seconds)
    {
        _focusShotPosition = position;
        _focusShotUntil = Time.unscaledTime + seconds;
        _hasFocusShot = true;
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
        ApplyCurrentOrthographicSize();
        Vector3 desiredPosition = new Vector3(_focusShotPosition.x, _focusShotPosition.y, -10.0f);
        transform.position = _arenaBounds == null ? desiredPosition : _arenaBounds.ClampRenderedCameraCenter(desiredPosition, _camera);

        if (_visibilityZone != null)
            _visibilityZone.RefreshFromCamera();
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

    void UpdateTimedOrthographicSize()
    {
        _scaledGameplaySeconds = Mathf.Min(
            TimedZoomDurationSeconds,
            _scaledGameplaySeconds + Time.deltaTime);
        float progress = _scaledGameplaySeconds / TimedZoomDurationSeconds;
        _currentOrthographicSize = Mathf.Lerp(InitialOrthographicSize, FinalOrthographicSize, progress);
        ApplyCurrentOrthographicSize();
    }

    void ApplyCurrentOrthographicSize()
    {
        if (_camera != null && _camera.orthographic)
            _camera.orthographicSize = _currentOrthographicSize;
    }
}
