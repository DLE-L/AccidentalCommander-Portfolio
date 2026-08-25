using System.Collections.Generic;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal sealed partial class FormationService
    {
        private const float OVERLAP_WARNING_RADIUS = 0.42f;
        private const float ALLY_TARGET_SEPARATION_RADIUS = 0.40f;
        private const float MAX_READABILITY_TARGET_PUSH = 0.82f;
        private const float COMMANDER_HIDDEN_WARNING_SECONDS = 0.5f;
        private const float WARNING_THROTTLE_SECONDS = 1.0f;
        private readonly Dictionary<int, float> _commanderHiddenStartedByAlly = new Dictionary<int, float>();
        private float _lastVisibilityWarningAt = -999.0f;
        private float _lastOverlapWarningAt = -999.0f;

        public Vector3 ApplyReadabilityGuards(AllyFollower follower, Vector3 desiredPosition)
        {
            PlayerController player = _registry?.Player;
            if (player == null || follower == null)
                return desiredPosition;

            Vector3 commanderPosition = player.transform.position;
            Vector3 adjustedPosition = ApplyCommanderSeparation(follower, desiredPosition, commanderPosition);
            adjustedPosition = ApplyAllySeparation(follower, adjustedPosition);

            CheckCommanderVisibility(follower, commanderPosition);
            CheckAllyOverlap(follower, adjustedPosition);
            return adjustedPosition;
        }

        private Vector3 ApplyCommanderSeparation(AllyFollower follower, Vector3 desiredPosition, Vector3 commanderPosition)
        {
            float radius = RemoteConfig.CommanderVisibilityPushRadius;
            if (radius <= 0.0f)
                return desiredPosition;

            Vector3 delta = desiredPosition - commanderPosition;
            delta.z = 0.0f;
            float distance = delta.magnitude;
            if (distance >= radius)
                return desiredPosition;

            Vector3 direction = distance > 0.0001f ? delta / distance : ResolveStableSeparationDirection(follower);
            return commanderPosition + direction * radius;
        }

        private Vector3 ApplyAllySeparation(AllyFollower follower, Vector3 desiredPosition)
        {
            IReadOnlyList<AllyFollower> allies = _party.ActiveAllies;
            Vector3 push = Vector3.zero;

            for (int i = 0; i < allies.Count; i++)
            {
                AllyFollower other = allies[i];
                if (other == null || other == follower)
                    continue;

                Vector3 delta = desiredPosition - other.transform.position;
                delta.z = 0.0f;
                float distance = delta.magnitude;
                if (distance >= ALLY_TARGET_SEPARATION_RADIUS)
                    continue;

                Vector3 direction = distance > 0.0001f ? delta / distance : ResolveStableSeparationDirection(follower);
                float strength = ALLY_TARGET_SEPARATION_RADIUS - distance;
                push += direction * strength;
            }

            if (push.sqrMagnitude <= 0.0001f)
                return desiredPosition;

            return desiredPosition + Vector3.ClampMagnitude(push, MAX_READABILITY_TARGET_PUSH);
        }

        private Vector3 ResolveStableSeparationDirection(AllyFollower follower)
        {
            int hash = follower == null || string.IsNullOrEmpty(follower.SlotId)
                ? 0
                : follower.SlotId.GetHashCode();
            float angle = Mathf.Abs(hash % 360) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
        }

        private void CheckCommanderVisibility(AllyFollower follower, Vector3 commanderPosition)
        {
            float radius = RemoteConfig.CommanderVisibilityPushRadius;
            if (radius <= 0.0f)
                return;

            Vector3 currentDelta = follower.transform.position - commanderPosition;
            float radiusSqr = radius * radius;
            int allyId = follower.GetInstanceID();
            if (currentDelta.sqrMagnitude < radiusSqr)
            {
                if (_commanderHiddenStartedByAlly.TryGetValue(allyId, out float hiddenStartedAt) == false)
                {
                    hiddenStartedAt = Time.time;
                    _commanderHiddenStartedByAlly[allyId] = hiddenStartedAt;
                }

                if (Time.time - hiddenStartedAt >= COMMANDER_HIDDEN_WARNING_SECONDS
                    && Time.time - _lastVisibilityWarningAt >= WARNING_THROTTLE_SECONDS)
                {
                    _lastVisibilityWarningAt = Time.time;
                    P0Telemetry.Log(
                        P0Telemetry.FormationVisibilityWarning,
                        $"hidden_duration={Time.time - hiddenStartedAt:0.00}",
                        $"blocker_unit_ids={follower.SlotId}",
                        $"ally_count={_party.ActiveAllyCount}",
                        $"enemy_count={GetEnemyCount()}");
                }
            }
            else
            {
                _commanderHiddenStartedByAlly.Remove(allyId);
            }
        }

        private void CheckAllyOverlap(AllyFollower follower, Vector3 desiredPosition)
        {
            IReadOnlyList<AllyFollower> allies = _party.ActiveAllies;
            int overlapCount = 0;

            for (int i = 0; i < allies.Count; i++)
            {
                AllyFollower other = allies[i];
                if (other == null || other == follower)
                    continue;

                Vector3 delta = desiredPosition - other.transform.position;
                float distance = delta.magnitude;
                if (distance <= 0.0001f || distance >= OVERLAP_WARNING_RADIUS)
                    continue;

                overlapCount++;
            }

            if (overlapCount > 0 && Time.time - _lastOverlapWarningAt >= WARNING_THROTTLE_SECONDS)
            {
                _lastOverlapWarningAt = Time.time;
                P0Telemetry.Log(
                    P0Telemetry.FormationOverlapWarning,
                    $"overlap_count={overlapCount}",
                    $"max_overlap_duration=>=frame",
                    $"ally_count={_party.ActiveAllyCount}",
                    "device_model=editor_or_unknown");
            }
        }

private int GetEnemyCount()
        {
            return _registry?.Enemies?.Count ?? 0;
        }

    }
}
