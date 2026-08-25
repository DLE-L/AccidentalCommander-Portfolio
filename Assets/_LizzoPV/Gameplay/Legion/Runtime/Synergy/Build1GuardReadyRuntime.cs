using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Diagnostics;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    internal sealed class Build1GuardReadyRuntime
    {
        private readonly PartyService _party;
        private readonly SynergyDamageData _ready;
        private float _elapsed;

        internal Build1GuardReadyRuntime(PartyService party, SynergyDamageData ready)
        {
            _party = party ?? throw new ArgumentNullException(nameof(party));
            _ready = ready ?? throw new ArgumentNullException(nameof(ready));
        }

        internal void Tick(float deltaSeconds)
        {
            _elapsed += deltaSeconds;
            while (_elapsed >= _ready.CadenceSeconds)
            {
                _elapsed -= _ready.CadenceSeconds;
                Resolve();
            }
        }

        internal void Reset()
        {
            _elapsed = 0.0f;
        }

        private void Resolve()
        {
            if (_party.TryResolveActiveCompanionWithFamilyTag("shield_family", out CompanionRuntime shield) == false)
                return;

            AllyCombat combat = shield.Combat;
            if (combat == null)
                return;

            Vector3 forward = combat.ResolveForwardAttackDirection();
            List<MonsterController> targets = combat.CollectForwardTargets(forward);
            int knockedTargetCount = 0;
            for (int index = 0; index < targets.Count; index++)
            {
                if (combat.TryApplyKnockback(targets[index], forward))
                    knockedTargetCount++;
            }

            Build1RuntimeDiagnostics.Log("synergy_ready_effect",
                Build1RuntimeDiagnostics.Text("synergy_id", _ready.SynergyId),
                Build1RuntimeDiagnostics.Float("cadence", _ready.CadenceSeconds),
                Build1RuntimeDiagnostics.Int("found_target_count", targets.Count),
                Build1RuntimeDiagnostics.Int("knocked_target_count", knockedTargetCount),
                Build1RuntimeDiagnostics.Float("push", _ready.Push),
                Build1RuntimeDiagnostics.Bool("no_damage", _ready.BaseValue == 0.0f));
        }
    }

}
