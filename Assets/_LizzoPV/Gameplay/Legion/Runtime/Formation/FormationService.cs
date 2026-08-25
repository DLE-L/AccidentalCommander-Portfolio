using System.Collections.Generic;
using UnityEngine;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.Gameplay.World;

namespace Lizzo.PV.Legion
{
    internal sealed partial class FormationService
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
        private Vector3 _lastForward = Vector3.up;
        private Vector3 _candidateForward = Vector3.up;
        private string _lastVectorSource = "fallback";
        private string _candidateVectorSource = string.Empty;
        private float _candidateStartedAt;
        private bool _hasStableForward;

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


    }
}
