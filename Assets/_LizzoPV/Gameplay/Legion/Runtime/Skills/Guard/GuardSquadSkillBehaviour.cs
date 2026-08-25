using System;
using System.Collections.Generic;
using System.Text;
using Lizzo.PV.P0.Combat;
using Lizzo.PV.P0.Config;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Skills;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.P0.Skills.Guard
{
    internal sealed class GuardSquadFirstCastSchedule
    {
        const int MinimumTargets = 3;
        const float RetrySeconds = 0.25f;
        const float MaximumDelaySeconds = 4.0f;

        float _requestedAt;
        float _nextCheckAt;
        string _reason;

        internal bool Pending { get; private set; }
        internal int TargetThreshold => MinimumTargets;

        internal void Schedule(string reason, float currentTime)
        {
            Pending = true;
            _requestedAt = currentTime;
            _nextCheckAt = 0.0f;
            _reason = string.IsNullOrEmpty(reason) ? "synergy_activate" : reason;
        }

        internal bool IsMaximumDelayReached(float currentTime)
        {
            return currentTime - _requestedAt >= MaximumDelaySeconds;
        }

        internal bool TryOpenTargetCheck(float currentTime)
        {
            if (currentTime < _nextCheckAt)
                return false;

            _nextCheckAt = currentTime + RetrySeconds;
            return true;
        }

        internal string ConsumeReason()
        {
            string reason = _reason;
            Pending = false;
            _reason = string.Empty;
            return reason;
        }

        internal void Reset()
        {
            Pending = false;
            _requestedAt = 0.0f;
            _nextCheckAt = 0.0f;
            _reason = null;
        }
    }

    internal static class GuardSquadFirstCastTargetCounter
    {
        internal static int Count(
            PartyService party,
            Transform player,
            SynergyData synergyData,
            SkillData skillData)
        {
            if (party.Registry?.Enemies == null)
                return 0;

            float radius = GuardSquadRadialShockwaveView.ResolveFirstActivationRadius(
                party,
                synergyData,
                skillData);
            float radiusSqr = radius * radius;
            int count = 0;

            foreach (MonsterController monster in party.Registry.Enemies)
            {
                if (monster == null || monster.IsValid() == false)
                    continue;

                Vector3 delta = monster.transform.position - player.position;
                delta.z = 0.0f;
                if (delta.sqrMagnitude <= radiusSqr)
                    count++;
            }

            return count;
        }
    }

    internal sealed class GuardSquadProtectionState
    {
        float _protectUntil;

        internal int ActiveCastId { get; private set; }

        internal bool IsActive(float currentTime)
        {
            return currentTime < _protectUntil;
        }

        internal void Start(float currentTime, float duration, int castId)
        {
            _protectUntil = currentTime + duration;
            ActiveCastId = castId;
        }

        internal void Reset()
        {
            _protectUntil = 0.0f;
            ActiveCastId = 0;
        }
    }

    internal sealed class GuardSquadCooldownSchedule
    {
        float _nextCastAt;

        internal bool IsDue(float currentTime)
        {
            return currentTime >= _nextCastAt;
        }

        internal void Schedule(float currentTime, float cooldownSeconds)
        {
            _nextCastAt = currentTime + Mathf.Max(1.0f, cooldownSeconds);
        }

        internal void Reset()
        {
            _nextCastAt = 0.0f;
        }
    }

    internal sealed class GuardSquadRuntimeDependencies
    {
        internal PartyService Party { get; private set; }
        internal Transform Player { get; private set; }
        internal SynergyData SynergyData { get; private set; }
        internal SkillData SkillData { get; private set; }
        internal string SynergyId { get; private set; }

        internal bool IsValid => Party != null && Player != null && SynergyData != null && SkillData != null;

        internal void Bind(
            PartyService party,
            Transform player,
            SynergyData synergyData,
            SkillData skillData)
        {
            Party = party;
            Player = player;
            SynergyData = synergyData;
            SkillData = skillData;
            SynergyId = synergyData.Id;
        }

        internal void Clear()
        {
            Party = null;
            Player = null;
            SynergyData = null;
            SkillData = null;
            SynergyId = null;
        }
    }

    public sealed class GuardSquadSkillBehaviour : MonoBehaviour
    {
        private static GuardSquadSkillBehaviour _active;

        private readonly GuardSquadRuntimeDependencies _runtime = new GuardSquadRuntimeDependencies();
        private readonly GuardSquadCooldownSchedule _cooldown = new GuardSquadCooldownSchedule();
        private readonly GuardSquadProtectionState _protection = new GuardSquadProtectionState();
        private bool _isActive;
        private readonly GuardSquadFirstCastSchedule _firstCast = new GuardSquadFirstCastSchedule();
        private bool _invalidRuntimeStateReported;

        public static float CompanionDamageMultiplier
        {
            get
            {
                if (_active == null || _active._isActive == false || _active._protection.IsActive(Time.time) == false)
                    return 1.0f;

                return Mathf.Clamp01(1.0f - RemoteConfig.GuardCompanionDamageReduction);
            }
        }

        public static bool IsProtectingCompanions => CompanionDamageMultiplier < 0.999f;

        public static int ActiveCastId => _active == null ? 0 : _active._protection.ActiveCastId;

public static void EnsureActive(PartyService party, Transform player, string reason, SynergyData synergyData, SkillData skillData)
        {
            if (party == null)
            {
                Debug.LogError("[GuardSquadSkillBehaviour] PartyService is required.");
                return;
            }

            if (player == null)
                return;

            if (synergyData == null)
            {
                Debug.LogError("[GuardSquadSkillBehaviour] SynergyData is required.", player);
                return;
            }

            if (skillData == null)
            {
                Debug.LogError("[GuardSquadSkillBehaviour] SkillData is required.", player);
                return;
            }

            GuardSquadSkillBehaviour runtime = player.GetComponent<GuardSquadSkillBehaviour>();
            if (runtime == null)
            {
                Debug.LogError("[GuardSquadSkillBehaviour] Commander prefab is missing the authored behaviour.", player);
                return;
            }

            runtime.Activate(party, player, reason, synergyData, skillData);
        }

public static void StopActive()
        {
            if (_active == null)
                return;

            _active.ClearRuntimeState();
            _active = null;
        }

private void Activate(PartyService party, Transform player, string reason, SynergyData synergyData, SkillData skillData)
        {
            _active = this;
            _runtime.Bind(party, player, synergyData, skillData);
            _invalidRuntimeStateReported = false;

            if (_isActive)
                return;

            _isActive = true;
            ScheduleFirstCast(string.IsNullOrEmpty(reason) ? "synergy_activate" : reason);
        }

private void Update()
        {
            if (_isActive == false)
                return;

            if (_runtime.Player == null || P0Telemetry.IsRunEnded)
            {
                ClearRuntimeState();
                if (_active == this)
                    _active = null;
                return;
            }

            if (_runtime.IsValid == false)
            {
                if (_invalidRuntimeStateReported == false)
                {
                    _invalidRuntimeStateReported = true;
                    Debug.LogError("[GuardSquadSkillBehaviour] Active runtime is missing party or data dependencies.", this);
                }

                ClearRuntimeState();
                if (_active == this)
                    _active = null;
                return;
            }

            if (_firstCast.Pending)
            {
                if (ShouldReleaseFirstCast())
                {
                    string firstCastReason = _firstCast.ConsumeReason();
                    CastGuardEffect(firstCastReason);
                }

                return;
            }

            if (_cooldown.IsDue(Time.time) == false)
                return;

            CastGuardEffect("cooldown");
        }

        private void ScheduleFirstCast(string reason)
        {
            _firstCast.Schedule(reason, Time.time);
        }

        private bool ShouldReleaseFirstCast()
        {
            if (_firstCast.IsMaximumDelayReached(Time.time))
                return true;

            if (_firstCast.TryOpenTargetCheck(Time.time) == false)
                return false;

            return GuardSquadFirstCastTargetCounter.Count(
                _runtime.Party,
                _runtime.Player,
                _runtime.SynergyData,
                _runtime.SkillData) >= _firstCast.TargetThreshold;
        }


        private void CastGuardEffect(string reason)
        {
            if (_runtime.Player == null)
                return;

            int castId = GuardSquadRadialShockwaveView.Activate(
                _runtime.Party,
                _runtime.Player,
                reason,
                _runtime.SynergyData,
                _runtime.SkillData);
            StartCompanionProtection(reason, castId);
            _cooldown.Schedule(Time.time, RemoteConfig.GuardWallCooldown);
        }

        private void StartCompanionProtection(string reason, int castId)
        {
            float duration = Mathf.Max(0.1f, RemoteConfig.GuardCompanionDamageReductionDuration);
            _protection.Start(Time.time, duration, castId);

            if (reason != "cooldown")
            {
                P0Telemetry.Log(
                    P0Telemetry.SynergyGuardProtectStart,
                    $"combo_id={_runtime.SynergyId}",
                    $"reason={reason}",
                    $"damage_reduction_percent={Mathf.RoundToInt(RemoteConfig.GuardCompanionDamageReduction * 100.0f)}",
                    $"duration={duration:0.##}");
            }

            AttackVisual.SpawnAttached(_runtime.Player, AttackVisualKind.BuffApplied, new Vector3(0.0f, 0.36f, 0.0f));

            for (int i = 0; i < _runtime.Party.ActiveCompanions.Count; i++)
            {
                CompanionRuntime companion = _runtime.Party.ActiveCompanions[i];
                if (companion == null || companion.IsDown)
                    continue;

                AttackVisual.SpawnAttached(companion.transform, AttackVisualKind.BuffApplied, new Vector3(0.0f, 0.28f, 0.0f));
            }
        }

        private void ClearRuntimeState()
        {
            _isActive = false;
            _firstCast.Reset();
            _runtime.Clear();
            _cooldown.Reset();
            _protection.Reset();
        }

    }
}
