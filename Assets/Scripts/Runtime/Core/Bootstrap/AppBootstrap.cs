using Lizzo.PV.Data;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class AppBootstrap : MonoBehaviour
{
    static AppBootstrap s_instance;

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

        IAssetService assets = new AddressableAssetService();
        LocalDataProvider provider = new LocalDataProvider(assets);
        Services = new AppServices(assets, provider);
        IsReady = true;
    }

    void OnDestroy()
    {
        if (s_instance != this)
            return;

        Services?.ReleaseAll();
        Services = null;
        IsReady = false;
        s_instance = null;
    }
}
