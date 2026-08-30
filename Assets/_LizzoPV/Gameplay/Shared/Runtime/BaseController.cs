using UnityEngine;
using static Define;

public class BaseController : MonoBehaviour
{
    public ObjectType ObjectType { get; protected set; }
    public RunServices Services { get; private set; }

    public void Initialize(RunServices services)
    {
        Services = services ?? throw new System.ArgumentNullException(nameof(services));
    }

    void Awake()
    {
        Init();
    }

    bool _init;
    public virtual bool Init()
    {
        if (_init)
            return false;

        _init = true;
        return true;
    }

    public virtual void ResetForSpawn()
    {
    }

}
