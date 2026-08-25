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

}
