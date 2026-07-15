using System;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;

public sealed class RunServices
{
    public AppServices App { get; }
    public RunState State { get; }
    public RuntimeObjectRegistry Registry { get; }
    public RuntimeObjectSpawner Spawner { get; }
    public ObjectPoolService Pool { get; }
    public IPrefabFactory Factory { get; }
    public PartyService Party { get; }

    bool _disposed;

    public RunServices(AppServices app, RunState state, RuntimeObjectRegistry registry, ObjectPoolService pool, IPrefabFactory factory)
    {
        App = app ?? throw new ArgumentNullException(nameof(app));
        State = state ?? throw new ArgumentNullException(nameof(state));
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        Pool = pool ?? throw new ArgumentNullException(nameof(pool));
        Factory = factory ?? throw new ArgumentNullException(nameof(factory));
        Party = new PartyService(App.Data, Registry, Factory);
        Spawner = new RuntimeObjectSpawner(this);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Party.Dispose();
        Registry.Clear();
        Factory.Clear();
        State.Dispose();
    }
}
