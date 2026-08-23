using System.Collections.Generic;
using UnityEngine;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.Gameplay.World;

namespace Lizzo.PV.Legion
{
    internal sealed class FormationService
    {

        private readonly RuntimeObjectRegistry _registry;
        private readonly PartyService _party;
        private ArenaBounds _arenaBounds;

public FormationService(RuntimeObjectRegistry registry, PartyService party)
        {
            _registry = registry ?? throw new System.ArgumentNullException(nameof(registry));
            _party = party ?? throw new System.ArgumentNullException(nameof(party));
        }
        private const float DIRECTION_CHANGE_DOT = 0.86f;
        private const float SMOOTH_RATE = 4.0f;
        private const float OVERLAP_WARNING_RADIUS = 0.42f;
        private const float ALLY_TARGET_SEPARATION_RADIUS = 0.40f;
        private const float MAX_READABILITY_TARGET_PUSH = 0.82f;
        private const float COMMANDER_HIDDEN_WARNING_SECONDS = 0.5f;
        private const float WARNING_THROTTLE_SECONDS = 1.0f;
        private readonly Dictionary<int, float> _commanderHiddenStartedByAlly = new Dictionary<int, float>();
        private Vector3 _lastForward = Vector3.up;
        private Vector3 _candidateForward = Vector3.up;
        private string _lastVectorSource = "fallback";
        private string _candidateVectorSource = string.Empty;
        private float _candidateStartedAt;
        private bool _hasStableForward;
        private float _lastVisibilityWarningAt = -999.0f;
        private float _lastOverlapWarningAt = -999.0f;

        public string LastVectorSource => _lastVectorSource;

public void ResetRunState()
        {
            _lastForward = Vector3.up;
            _candidateForward = Vector3.up;
            _lastVectorSource = "fallback";
            _candidateVectorSource = string.Empty;
            _candidateStartedAt = 0.0f;
            _hasStableForward = false;
            _commanderHiddenStartedByAlly.Clear();
            _lastVisibilityWarningAt = -999.0f;
            _lastOverlapWarningAt = -999.0f;
        }




        public Vector3 ResolveForward()
        {
            PlayerController player = _registry?.Player;
            if (player == null)
                return _lastForward;

            ResolveDesiredForward(player, out Vector3 desiredForward, out string desiredSource);
            if (_hasStableForward == false)
                return AcceptForward(desiredForward, desiredSource);

            float dot = Vector3.Dot(_lastForward, desiredForward);
            if (dot >= DIRECTION_CHANGE_DOT)
            {
                float t = Mathf.Clamp01(Time.deltaTime * SMOOTH_RATE);
                _lastForward = Vector3.Lerp(_lastForward, desiredForward, t).normalized;
                _lastVectorSource = desiredSource;
                _candidateVectorSource = string.Empty;
                return _lastForward;
            }

            if (HasNewCandidate(desiredForward, desiredSource))
            {
                _candidateForward = desiredForward;
                _candidateVectorSource = desiredSource;
                _candidateStartedAt = Time.time;
                return _lastForward;
            }

            if (Time.time - _candidateStartedAt < RemoteConfig.FormationVectorLockSeconds)
                return _lastForward;

            return AcceptForward(desiredForward, desiredSource);
        }

        public Vector3 ResolveWorldOffset(Vector3 localOffset, string slotId)
        {
            Vector3 forward = ResolveForward();
            Vector3 right = new Vector3(forward.y, -forward.x, 0.0f);
            Vector3 directionalOffset = right * localOffset.x + forward * localOffset.y;
            float spacing = RemoteConfig.FormationSpacing;

            return directionalOffset * spacing;
        }

        internal bool TryResolveFormationAnchor(string rosterSlotId, out Vector3 anchor)
        {
            anchor = default;
            if (string.IsNullOrEmpty(rosterSlotId) || _registry.Player == null)
                return false;

            IReadOnlyList<CompanionRuntime> companions = _party.ActiveCompanions;
            for (int i = 0; i < companions.Count; i++)
            {
                CompanionRuntime companion = companions[i];
                if (companion == null || companion.RosterSlotId != rosterSlotId)
                    continue;

                AllyFollower follower = companion.GetComponent<AllyFollower>();
                if (follower == null)
                    return false;

                anchor = _registry.Player.transform.position
                    + ResolveWorldOffset(follower.FormationLocalOffset, follower.SlotId);
                return true;
            }

            return false;
        }

        internal bool TryResolveSynergyAnchorAndRange(
            string rosterSlotId,
            out Vector3 anchor,
            out float attackRange)
        {
            anchor = default;
            attackRange = 0.0f;
            if (TryResolveFormationAnchor(rosterSlotId, out anchor) == false)
                return false;

            IReadOnlyList<CompanionRuntime> companions = _party.ActiveCompanions;
            for (int i = 0; i < companions.Count; i++)
            {
                CompanionRuntime companion = companions[i];
                if (companion == null || companion.RosterSlotId != rosterSlotId)
                    continue;

                AllyCombat combat = companion.Combat;
                if (combat == null)
                    return false;

                attackRange = combat.AttackRange;
                return attackRange > 0.0f;
            }

            return false;
        }

        internal void BindArenaBounds(ArenaBounds arenaBounds)
        {
            _arenaBounds = arenaBounds;
        }

        internal Vector2 ClampFriendlyActor(Vector2 desiredPosition)
        {
            return _arenaBounds == null ? desiredPosition : _arenaBounds.ClampFriendlyActor(desiredPosition);
        }

        private void ResolveDesiredForward(PlayerController player, out Vector3 forward, out string source)
        {
            if (player.MoveDirection.sqrMagnitude > 0.001f)
            {
                forward = new Vector3(player.MoveDirection.x, player.MoveDirection.y, 0.0f).normalized;
                source = "move_direction";
                return;
            }

            forward = _lastForward;
            source = "last_valid_direction";
        }

        private bool HasNewCandidate(Vector3 desiredForward, string desiredSource)
        {
            return _candidateVectorSource != desiredSource
                || Vector3.Dot(_candidateForward, desiredForward) < DIRECTION_CHANGE_DOT;
        }

        private Vector3 AcceptForward(Vector3 forward, string source)
        {
            _lastForward = forward;
            _lastVectorSource = source;
            _candidateVectorSource = string.Empty;
            _hasStableForward = true;
            return _lastForward;
        }


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
