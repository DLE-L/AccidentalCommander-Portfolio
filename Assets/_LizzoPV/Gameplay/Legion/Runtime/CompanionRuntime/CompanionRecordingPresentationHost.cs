using System;
using System.Collections.Generic;
using Lizzo.PV.Legion.RunCore.Presentation;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.RunCore
{
    internal sealed class CompanionRecordingPresentationHost : IDisposable
    {
        private const float ReflowSharpness = 12.0f;

        private readonly ICompanionRunOutput _output;
        private readonly CompanionRuntimePresentationSet _presentationSet;
        private readonly Dictionary<int, CompanionSquadRoot> _rootsBySlot =
            new Dictionary<int, CompanionSquadRoot>();
        private readonly HashSet<int> _activeSlots = new HashSet<int>();
        private readonly List<int> _releaseSlots = new List<int>(7);
        private bool _hasCommanderWorldPosition;
        private Vector3 _lastCommanderWorldPosition;
        private bool _disposed;

        internal CompanionRecordingPresentationHost(
            ICompanionRunOutput output,
            CompanionRuntimePresentationSet presentationSet)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _presentationSet = presentationSet ?? throw new ArgumentNullException(nameof(presentationSet));
        }

        internal void Consume(Transform commander, float deltaSeconds)
        {
            if (_disposed || commander == null)
                return;

            CompanionRunOutputBatch batch = _output.Pull();
            _activeSlots.Clear();
            Vector3 commanderWorldMovement = _hasCommanderWorldPosition
                ? commander.position - _lastCommanderWorldPosition
                : Vector3.zero;
            float blend = 1.0f - Mathf.Exp(-ReflowSharpness * Mathf.Max(0.0f, deltaSeconds));
            for (int index = 0; index < batch.Snapshot.Squads.Count; index += 1)
            {
                SquadSnapshot squad = batch.Snapshot.Squads[index];
                _activeSlots.Add(squad.SlotId);
                if (!TryGetOrCreateRoot(in squad, commander, out CompanionSquadRoot root))
                    continue;

                if (root.transform.parent != commander)
                    root.transform.SetParent(commander, true);

                CompanionPoint desiredLocal = WorldToCommander(squad.FormationAnchor, commander.position);
                Vector3 currentLocal = root.transform.localPosition;
                CompanionPoint smoothLocal = new CompanionPoint(
                    Mathf.Lerp(currentLocal.x, desiredLocal.X, blend),
                    Mathf.Lerp(currentLocal.y, desiredLocal.Y, blend));
                Vector3 localReflow = new Vector3(
                    smoothLocal.X - currentLocal.x,
                    smoothLocal.Y - currentLocal.y,
                    0.0f);
                Vector3 squadWorldMovement = commanderWorldMovement + commander.TransformVector(localReflow);
                SquadSnapshot localSnapshot = ToCommanderLocal(in squad, in smoothLocal, commander.position);
                CompanionPoint presentationMovement = new CompanionPoint(
                    squadWorldMovement.x,
                    squadWorldMovement.y);
                root.TryApplySnapshot(in localSnapshot, in presentationMovement);
            }

            PlayCues(batch, commander);
            ReleaseMissingRoots();
            _lastCommanderWorldPosition = commander.position;
            _hasCommanderWorldPosition = true;
        }

        private void PlayCues(CompanionRunOutputBatch batch, Transform commander)
        {
            for (int eventIndex = 0; eventIndex < batch.Events.Count; eventIndex += 1)
            {
                CompanionRunEvent runEvent = batch.Events[eventIndex];
                if (runEvent.Kind == CompanionRunEventKind.EffectCommitted)
                {
                    PlayCommittedEffect(batch, in runEvent, commander);
                    continue;
                }

                RetroVfxKind kind = runEvent.Kind switch
                {
                    CompanionRunEventKind.SquadRecruited => RetroVfxKind.CompanionRecruit,
                    CompanionRunEventKind.SquadReinforced => RetroVfxKind.CompanionRecruit,
                    CompanionRunEventKind.SquadPromoted => RetroVfxKind.CompanionPromotion,
                    _ => RetroVfxKind.None,
                };
                if (kind == RetroVfxKind.None)
                    continue;

                for (int squadIndex = 0; squadIndex < batch.Snapshot.Squads.Count; squadIndex += 1)
                {
                    SquadSnapshot squad = batch.Snapshot.Squads[squadIndex];
                    if (!string.Equals(squad.SquadId, runEvent.SquadId, StringComparison.Ordinal)
                        || !_rootsBySlot.TryGetValue(squad.SlotId, out CompanionSquadRoot root)
                        || root == null)
                    {
                        continue;
                    }

                    RetroVfx.Spawn(kind, root.transform.position, Vector3.up, kind == RetroVfxKind.CompanionPromotion ? 1.15f : 0.90f);
                    break;
                }
            }
        }

        private void PlayCommittedEffect(
            CompanionRunOutputBatch batch,
            in CompanionRunEvent runEvent,
            Transform commander)
        {
            if (!runEvent.PresentationCue.HasValue || commander == null)
                return;

            PresentationCue cue = runEvent.PresentationCue.Value;
            for (int squadIndex = 0; squadIndex < batch.Snapshot.Squads.Count; squadIndex += 1)
            {
                SquadSnapshot squad = batch.Snapshot.Squads[squadIndex];
                if (!string.Equals(squad.SquadId, cue.SquadId, StringComparison.Ordinal)
                    || !_rootsBySlot.TryGetValue(squad.SlotId, out CompanionSquadRoot root)
                    || root == null)
                {
                    continue;
                }

                CompanionPoint targetLocal = WorldToCommander(cue.TargetPosition, commander.position);
                float holdSeconds = cue.DeliveryDelaySeconds > 0.0f
                    ? Mathf.Clamp(cue.DeliveryDelaySeconds, 0.12f, 0.32f)
                    : -1.0f;
                root.TryPlayAttack(cue.MemberOrder, in targetLocal, holdSeconds);
                break;
            }

            CompanionTravelingPayloadView.TryPlay(
                runEvent.CompanionId,
                cue.Delivery,
                cue.PresentationId,
                new Vector3(cue.SourcePosition.X, cue.SourcePosition.Y, 0.0f),
                new Vector3(cue.TargetPosition.X, cue.TargetPosition.Y, 0.0f),
                cue.DeliveryDelaySeconds,
                CompanionMemberVisualVariant.ResolveIntensity(cue.MemberOrder));
        }

        internal void Reset()
        {
            foreach (CompanionSquadRoot root in _rootsBySlot.Values)
            {
                if (root != null)
                    UnityEngine.Object.Destroy(root.gameObject);
            }

            _rootsBySlot.Clear();
            _activeSlots.Clear();
            _releaseSlots.Clear();
            _lastCommanderWorldPosition = Vector3.zero;
            _hasCommanderWorldPosition = false;
        }

        internal static void PresentRecordingVideoEffect(
            string effectId,
            string presentationCueId,
            Vector3 source,
            Vector3 target,
            Vector3 forward,
            float range,
            float radius,
            int memberOrder)
        {
            if (forward.sqrMagnitude <= 0.0001f)
                forward = Vector3.right;
            else
                forward.Normalize();
            float memberVisualIntensity = CompanionMemberVisualVariant.ResolveIntensity(memberOrder);

            if (string.Equals(
                    presentationCueId,
                    CompanionPresentationCueIds.TravelingForward,
                    StringComparison.Ordinal))
            {
                RetroVfx.SpawnCompanionTravelingAttack(
                    effectId,
                    source,
                    target,
                    forward,
                    1.85f,
                    0.20f,
                    memberVisualIntensity);
                return;
            }

            switch (effectId)
            {
                case "dmg_shield_bash_v1":
                    RetroVfx.SpawnCompanionAttack(
                        effectId,
                        source + forward * Mathf.Min(0.55f, Mathf.Max(0.25f, range * 0.35f)),
                        forward,
                        Mathf.Max(1.80f, range * 0.98f),
                        memberVisualIntensity);
                    break;
                case "dmg_sword_slash_v1":
                    RetroVfx.SpawnCompanionAttack(
                        effectId,
                        source + (forward * Mathf.Max(0.30f, range * 0.34f)),
                        forward,
                        Mathf.Max(1.65f, range * 0.95f),
                        memberVisualIntensity);
                    break;
                case "dmg_bomb_explosion_v1":
                    CompanionBurstVfxSequence.Play(
                        effectId,
                        target,
                        forward,
                        7.4f,
                        memberVisualIntensity);
                    break;
                case "dot_fire_field_v1":
                    RetroVfx.SpawnCompanionAttack(
                        "blast_staff_explosion",
                        target,
                        Vector3.up,
                        Mathf.Max(2.20f, radius * 1.10f),
                        memberVisualIntensity);
                    RetroVfx.SpawnCompanionAttack(
                        effectId,
                        target,
                        Vector3.up,
                        Mathf.Max(1.70f, radius),
                        memberVisualIntensity);
                    break;
                case "dmg_cleric_bolt_v1":
                case "heal_cleric_v1":
                    // Projectile/heal modules already own their presentation.
                    break;
                default:
                    RetroVfx.SpawnCompanionAttack(
                        effectId,
                        target,
                        forward,
                        Mathf.Max(range, radius),
                        memberVisualIntensity);
                    break;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            Reset();
            _disposed = true;
        }

        private bool TryGetOrCreateRoot(
            in SquadSnapshot squad,
            Transform commander,
            out CompanionSquadRoot root)
        {
            if (_rootsBySlot.TryGetValue(squad.SlotId, out root) && root != null)
                return true;

            if (!_presentationSet.TryGetSquadRoot(squad.CompanionId, out CompanionSquadRoot prefab))
            {
                Debug.LogError("[CompanionRecordingPresentationHost] Missing squad presentation: " + squad.CompanionId);
                root = null;
                return false;
            }

            root = UnityEngine.Object.Instantiate(prefab, commander);
            root.name = "CompanionSquad_" + squad.SlotId.ToString("00") + "_" + squad.CompanionId;
            _rootsBySlot[squad.SlotId] = root;
            return true;
        }

        private void ReleaseMissingRoots()
        {
            _releaseSlots.Clear();
            foreach (KeyValuePair<int, CompanionSquadRoot> pair in _rootsBySlot)
            {
                if (!_activeSlots.Contains(pair.Key))
                    _releaseSlots.Add(pair.Key);
            }

            for (int index = 0; index < _releaseSlots.Count; index += 1)
            {
                int slotId = _releaseSlots[index];
                CompanionSquadRoot root = _rootsBySlot[slotId];
                if (root != null)
                    UnityEngine.Object.Destroy(root.gameObject);
                _rootsBySlot.Remove(slotId);
            }
        }

        private static SquadSnapshot ToCommanderLocal(
            in SquadSnapshot source,
            in CompanionPoint localFormationAnchor,
            Vector3 commanderPosition)
        {
            CompanionPoint activePosition = WorldToCommander(source.ActiveMemberPosition, commanderPosition);
            CompanionPoint? targetPosition = source.CommittedTargetPosition.HasValue
                ? WorldToCommander(source.CommittedTargetPosition.Value, commanderPosition)
                : null;
            return new SquadSnapshot(
                source.SquadId,
                source.SlotId,
                source.CompanionId,
                source.ActionSetId,
                source.MemberCount,
                source.Promoted,
                source.CombatEligible,
                source.CooldownRemainingSeconds,
                localFormationAnchor,
                source.ActionPhase,
                source.ActiveMemberOrder,
                activePosition,
                targetPosition,
                source.Members);
        }

        private static CompanionPoint WorldToCommander(in CompanionPoint world, Vector3 commanderPosition)
        {
            return new CompanionPoint(world.X - commanderPosition.x, world.Y - commanderPosition.y);
        }
    }
}
