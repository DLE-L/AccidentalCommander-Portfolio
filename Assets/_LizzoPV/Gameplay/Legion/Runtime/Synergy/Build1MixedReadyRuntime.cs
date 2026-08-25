using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.Gameplay.Diagnostics;
using UnityEngine;

namespace Lizzo.PV.Legion.Synergy
{
    internal sealed class Build1MixedReadyRuntime
    {
        private readonly PartyService _party;
        private readonly SynergyEffectData _ready;
        private float _elapsed;
        private float _moveRemaining;
        private bool _effectActive;

        internal Build1MixedReadyRuntime(PartyService party, SynergyEffectData ready)
        {
            _party = party ?? throw new ArgumentNullException(nameof(party));
            _ready = ready ?? throw new ArgumentNullException(nameof(ready));
        }

        internal void Tick(float deltaSeconds)
        {
            _moveRemaining = Mathf.Max(0.0f, _moveRemaining - deltaSeconds);
            if (_effectActive && _moveRemaining <= 0.0f)
            {
                _effectActive = false;
                Build1RuntimeDiagnostics.Log("synergy_ready_effect",
                    Build1RuntimeDiagnostics.Text("synergy_id", _ready.SynergyId),
                    Build1RuntimeDiagnostics.Text("phase", "expired"));
            }

            _elapsed += deltaSeconds;
            while (_elapsed >= _ready.CadenceSeconds)
            {
                _elapsed -= _ready.CadenceSeconds;
                _moveRemaining = _ready.DurationSeconds;
                _effectActive = true;
                Build1RuntimeDiagnostics.Log("synergy_ready_effect",
                    Build1RuntimeDiagnostics.Text("synergy_id", _ready.SynergyId),
                    Build1RuntimeDiagnostics.Float("cadence", _ready.CadenceSeconds),
                    Build1RuntimeDiagnostics.Int("target_living_count", CountLivingCompanions()),
                    Build1RuntimeDiagnostics.Float("move_multiplier", _ready.MoveSpeedMultiplier),
                    Build1RuntimeDiagnostics.Float("duration", _ready.DurationSeconds));
            }
        }

        internal float GetMoveSpeedMultiplier(CompanionRuntime companion)
        {
            return _moveRemaining > 0.0f
                && companion != null
                && companion.IsDown == false
                ? _ready.MoveSpeedMultiplier
                : 1.0f;
        }

        internal void Reset()
        {
            _elapsed = 0.0f;
            _moveRemaining = 0.0f;
            _effectActive = false;
        }

        private int CountLivingCompanions()
        {
            IReadOnlyList<CompanionRuntime> companions = _party.ActiveCompanions;
            int count = 0;
            for (int index = 0; index < companions.Count; index++)
            {
                CompanionRuntime companion = companions[index];
                if (companion != null && companion.IsDown == false && companion.Hp > 0)
                    count++;
            }
            return count;
        }
    }

}
