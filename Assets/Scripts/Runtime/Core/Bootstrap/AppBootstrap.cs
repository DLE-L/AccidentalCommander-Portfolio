using Lizzo.PV.Data;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class AppBootstrap : MonoBehaviour
{
    static AppBootstrap s_instance;

    bool _hasFocus = true;
    bool _isPaused;
    bool _isQuitting;

    public static AppBootstrap Instance => s_instance;
    public AppServices Services { get; private set; }
    public bool IsReady { get; private set; }

    void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        ApplyScreenAwakeState();

        IAssetService assets = new AddressableAssetService();
        LocalDataProvider provider = new LocalDataProvider(assets);
        Services = new AppServices(assets, provider);
        IsReady = true;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        _hasFocus = hasFocus;
        ApplyScreenAwakeState();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        _isPaused = pauseStatus;
        ApplyScreenAwakeState();
    }

    void OnApplicationQuit()
    {
        _isQuitting = true;
        ApplyScreenAwakeState();
    }

    void HandleActiveSceneChanged(UnityEngine.SceneManagement.Scene previousScene, UnityEngine.SceneManagement.Scene nextScene)
    {
        ApplyScreenAwakeState();
    }

    void ApplyScreenAwakeState()
    {
        bool shouldKeepAwake = Lizzo.PV.Flow.ForegroundScreenAwakePolicy.ShouldKeepAwake(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
            _hasFocus,
            _isPaused,
            _isQuitting);
        Screen.sleepTimeout = shouldKeepAwake
            ? SleepTimeout.NeverSleep
            : SleepTimeout.SystemSetting;
    }

    void OnDestroy()
    {
        if (s_instance != this)
            return;

        UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        _isQuitting = true;
        ApplyScreenAwakeState();
        Services?.ReleaseAll();
        Services = null;
        IsReady = false;
        s_instance = null;
    }
}
