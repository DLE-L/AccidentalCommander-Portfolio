using System;
using System.Collections.Generic;
using Lizzo.PV.Combat;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Run
{
    internal enum SynergyEffectStatus
    {
        None,
        Weakening,
        Vulnerable,
        Shock,
    }

    internal enum SynergyEffectMovement
    {
        None,
        PullToPoint,
        PushAlongDirection,
    }

    internal readonly struct SynergyEffectCommand
    {
        internal RunPoint Point { get; }
        internal RunPoint Direction { get; }
        internal float Magnitude { get; }
        internal float Radius { get; }
        internal float DurationSeconds { get; }
        internal float Distance { get; }
        internal float StatusMagnitude { get; }
        internal int TargetLimit { get; }
        internal bool HealsCommander { get; }
        internal SynergyEffectStatus Status { get; }
        internal SynergyEffectMovement Movement { get; }

        private SynergyEffectCommand(
            RunPoint point,
            RunPoint direction,
            float magnitude,
            float radius,
            float durationSeconds,
            float distance,
            float statusMagnitude,
            int targetLimit,
            bool healsCommander,
            SynergyEffectStatus status,
            SynergyEffectMovement movement)
        {
            Point = point;
            Direction = direction;
            Magnitude = magnitude;
            Radius = radius;
            DurationSeconds = durationSeconds;
            Distance = distance;
            StatusMagnitude = statusMagnitude;
            TargetLimit = targetLimit;
            HealsCommander = healsCommander;
            Status = status;
            Movement = movement;
        }

        internal static SynergyEffectCommand From(PairSynergyEffectStep step)
        {
            return new SynergyEffectCommand(
                step.Point,
                default,
                step.Magnitude,
                step.Radius,
                step.DurationSeconds,
                0.0f,
                0.0f,
                step.TargetLimit,
                step.Kind == PairSynergyEffectKind.SoulReturnHeal,
                step.Kind == PairSynergyEffectKind.ApplyWeaken
                    ? SynergyEffectStatus.Weakening
                    : SynergyEffectStatus.None,
                step.Kind == PairSynergyEffectKind.PullToCenter
                    ? SynergyEffectMovement.PullToPoint
                    : SynergyEffectMovement.None);
        }

        internal static SynergyEffectCommand From(TrioSynergyEffectStep step)
        {
            SynergyEffectStatus status = SynergyEffectStatus.None;
            if (step.Kind == TrioSynergyEffectKind.WraithMarchDamageWeaken)
                status = SynergyEffectStatus.Weakening;
            else if (step.Kind == TrioSynergyEffectKind.ApplySynergyVulnerability)
                status = SynergyEffectStatus.Vulnerable;
            else if (step.Kind == TrioSynergyEffectKind.WraithBind)
                status = SynergyEffectStatus.Shock;

            SynergyEffectMovement movement = SynergyEffectMovement.None;
            if (step.Kind == TrioSynergyEffectKind.RitualPull)
                movement = SynergyEffectMovement.PullToPoint;
            else if (step.Distance > 0.0f)
                movement = SynergyEffectMovement.PushAlongDirection;

            return new SynergyEffectCommand(
                step.Point,
                step.Direction,
                step.Magnitude,
                step.Radius,
                step.DurationSeconds,
                step.Distance,
                step.StatusMagnitude,
                step.TargetCount,
                step.Kind == TrioSynergyEffectKind.HolyReturnHeal,
                status,
                movement);
        }
    }

    internal sealed class SynergyEffectExecutor
    {
        private readonly RuntimeObjectRegistry _registry;
        private readonly CombatImmediateHitModule _hits;
        private readonly List<MonsterController> _targets = new List<MonsterController>(32);

        internal SynergyEffectExecutor(
            RuntimeObjectRegistry registry,
            CombatImmediateHitModule hits)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _hits = hits ?? throw new ArgumentNullException(nameof(hits));
        }

        internal void Execute(string sourceId, in SynergyEffectCommand command)
        {
            PlayerController commander = _registry.Player;
            Vector3 point = ToVector(command.Point);
            if (point == Vector3.zero && commander != null)
                point = commander.transform.position;

            if (command.HealsCommander)
            {
                HealCommander(commander, command.Magnitude);
                return;
            }

            float resolvedRadius = command.Radius > 0.0f ? command.Radius : 1.25f;
            CollectTargets(point, resolvedRadius, command.TargetLimit);
            Vector3 direction = ToVector(command.Direction);
            Vector3 pushDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.zero;
            int damage = Mathf.Max(0, Mathf.RoundToInt(command.Magnitude));
            int ownerId = StableOwnerId(sourceId);
            for (int index = 0; index < _targets.Count; index++)
            {
                MonsterController target = _targets[index];
                if (damage > 0)
                {
                    _hits.TryApply(CombatImmediateHitRequest.CreateAllyDirectTarget(
                        sourceId,
                        target,
                        point,
                        target.transform.position,
                        damage,
                        AttackVisualKind.SingleHit,
                        false,
                        new CountableKillAttribution(
                            ownerId,
                            sourceId,
                            CombatKillSourceCategory.SynergyAction)));
                }

                ApplyStatus(target, sourceId, ownerId, in command);
                ApplyMovement(target, point, pushDirection, in command);
            }

            AttackVisual.Spawn(
                point,
                resolvedRadius > 1.5f
                    ? AttackVisualKind.AreaHit
                    : AttackVisualKind.SingleHit);
        }

        internal void Reset()
        {
            _targets.Clear();
        }

        private static void HealCommander(PlayerController commander, float magnitude)
        {
            if (commander == null || magnitude <= 0.0f)
                return;

            int amount = Mathf.Max(1, Mathf.RoundToInt(magnitude));
            commander.Hp = Mathf.Min(commander.MaxHp, commander.Hp + amount);
            FloatingDamageText.ShowHeal(commander.transform.position, amount);
        }

        private void CollectTargets(Vector3 center, float radius, int limit)
        {
            _targets.Clear();
            float radiusSquared = radius * radius;
            foreach (MonsterController target in _registry.Enemies)
            {
                if (target == null
                    || target.IsValid() == false
                    || (target.transform.position - center).sqrMagnitude > radiusSquared)
                {
                    continue;
                }

                _targets.Add(target);
            }

            _targets.Sort((left, right) =>
                ((left.transform.position - center).sqrMagnitude)
                    .CompareTo((right.transform.position - center).sqrMagnitude));
            int cap = limit > 0 ? limit : _targets.Count;
            if (_targets.Count > cap)
                _targets.RemoveRange(cap, _targets.Count - cap);
        }

        private static void ApplyStatus(
            MonsterController target,
            string sourceId,
            int ownerId,
            in SynergyEffectCommand command)
        {
            if (command.Status == SynergyEffectStatus.None)
                return;

            CompanionEnemyStatusKind statusKind;
            float magnitude;
            switch (command.Status)
            {
                case SynergyEffectStatus.Weakening:
                    statusKind = CompanionEnemyStatusKind.Weakening;
                    magnitude = command.StatusMagnitude > 0.0f
                        ? Mathf.Clamp01(1.0f - command.StatusMagnitude)
                        : 0.8f;
                    break;
                case SynergyEffectStatus.Vulnerable:
                    statusKind = CompanionEnemyStatusKind.Vulnerable;
                    magnitude = 1.0f + Mathf.Max(0.1f, command.Magnitude);
                    break;
                case SynergyEffectStatus.Shock:
                    statusKind = CompanionEnemyStatusKind.Shock;
                    magnitude = 0.5f;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            target.ApplyCompanionStatus(
                statusKind,
                new CompanionStatusSource(sourceId, ownerId),
                magnitude,
                Mathf.Max(0.1f, command.DurationSeconds),
                Time.time);
        }

        private static void ApplyMovement(
            MonsterController target,
            Vector3 point,
            Vector3 pushDirection,
            in SynergyEffectCommand command)
        {
            if (target.IsBoss || command.Movement == SynergyEffectMovement.None)
                return;

            Vector3 direction = command.Movement == SynergyEffectMovement.PullToPoint
                ? point - target.transform.position
                : pushDirection;
            if (direction.sqrMagnitude <= 0.0001f)
                return;

            target.ApplySmoothKnockback(
                direction,
                Mathf.Max(command.Distance, command.Magnitude),
                0.25f);
        }

        private static int StableOwnerId(string sourceId)
        {
            unchecked
            {
                int hash = 17;
                for (int index = 0; index < sourceId.Length; index++)
                    hash = hash * 31 + sourceId[index];
                return hash == 0 ? 1 : hash;
            }
        }

        private static Vector3 ToVector(RunPoint value) =>
            new Vector3(value.X, value.Y, 0.0f);
    }
}
