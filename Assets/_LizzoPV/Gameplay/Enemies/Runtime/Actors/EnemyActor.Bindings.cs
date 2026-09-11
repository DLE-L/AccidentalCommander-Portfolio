using System;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Combat;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Presentation;

namespace Lizzo.PV.Gameplay.Units
{
    public partial class EnemyActor
    {
        internal RuntimeObjectRegistry Registry { get; private set; }
        internal IDataProvider Data { get; private set; }
        internal RunGameplayTuning Tuning { get; private set; }
        internal ICombatImmediateHitModule ImmediateHits { get; private set; }
        private RunCombatTelemetry _combatTelemetry;
        private SafeKnockbackWorld _safeKnockback;
        private EnemyDeathResolver _deathResolver;

        internal void BindRuntime(RuntimeObjectRegistry registry, IDataProvider data, RunGameplayTuning tuning,
            ICombatImmediateHitModule immediateHits, RunCombatTelemetry telemetry,
            SafeKnockbackWorld safeKnockback, EnemyDeathResolver deathResolver)
        {
            Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            ImmediateHits = immediateHits ?? throw new ArgumentNullException(nameof(immediateHits));
            _deathResolver = deathResolver ?? throw new ArgumentNullException(nameof(deathResolver));
            _combatTelemetry = telemetry;
            _safeKnockback = safeKnockback;
        }
    }
}
