using System;

namespace Lizzo.PV.Legion.RunCore
{
    internal static class CompanionPointMath
    {
        internal static CompanionPoint Add(CompanionPoint left, CompanionPoint right)
        {
            return new CompanionPoint(left.X + right.X, left.Y + right.Y);
        }

        internal static float Distance(CompanionPoint first, CompanionPoint second)
        {
            float dx = first.X - second.X;
            float dy = first.Y - second.Y;
            return MathF.Sqrt((dx * dx) + (dy * dy));
        }

        internal static CompanionPoint MoveTowards(
            CompanionPoint current,
            CompanionPoint target,
            float maxDistance)
        {
            float distance = Distance(current, target);
            if (distance <= 0.0f || maxDistance <= 0.0f)
            {
                return current;
            }

            if (maxDistance >= distance)
            {
                return target;
            }

            float ratio = maxDistance / distance;
            return new CompanionPoint(
                current.X + ((target.X - current.X) * ratio),
                current.Y + ((target.Y - current.Y) * ratio));
        }
    }

    internal sealed class CompanionCooldownClock
    {
        internal CompanionCooldownClock(float durationSeconds)
        {
            Restart(durationSeconds);
        }

        internal float RemainingSeconds { get; private set; }

        internal bool Advance(ref float remainingDelta)
        {
            if (RemainingSeconds <= 0.0f)
            {
                return true;
            }

            float consumed = MathF.Min(RemainingSeconds, remainingDelta);
            RemainingSeconds -= consumed;
            remainingDelta -= consumed;
            return RemainingSeconds <= 0.0f;
        }

        internal void Restart(float durationSeconds)
        {
            RemainingSeconds = durationSeconds;
        }
    }

    internal static class CompanionTargetAcquisitionResolver
    {
        internal static bool TryCommit(
            ActionStep firstStep,
            ICompanionCombatWorld combatWorld,
            CompanionPoint origin,
            float rangeMultiplier,
            out CompanionPoint targetPosition)
        {
            float maxRange = firstStep.TargetAcquisitionRange * MathF.Max(0.01f, rangeMultiplier);
            if (maxRange > 0.0f && combatWorld is IRangedCompanionTargetWorld rangedWorld)
            {
                return rangedWorld.TrySelectTargetPosition(origin, maxRange, out targetPosition);
            }

            if (!combatWorld.TrySelectTargetPosition(out targetPosition))
            {
                return false;
            }

            return maxRange <= 0.0f || CompanionPointMath.Distance(origin, targetPosition) <= maxRange;
        }
    }

    internal static class CompanionExcursionPath
    {
        internal static CompanionPoint ResolveDestination(
            CompanionPoint target,
            CompanionPoint formationAnchor,
            ActionStep actionStep,
            int memberOrder)
        {
            float standOff = actionStep.ExcursionStandOffDistance;
            float lateral = actionStep.ExcursionLateralOffset;
            if (standOff <= 0.0f && lateral <= 0.0f)
            {
                return target;
            }

            float forwardX = target.X - formationAnchor.X;
            float forwardY = target.Y - formationAnchor.Y;
            float length = MathF.Sqrt((forwardX * forwardX) + (forwardY * forwardY));
            if (length <= 0.0001f)
            {
                forwardX = 1.0f;
                forwardY = 0.0f;
            }
            else
            {
                forwardX /= length;
                forwardY /= length;
            }

            float lateralSign = memberOrder == 0
                ? -1.0f
                : memberOrder == 1
                    ? 1.0f
                    : 0.0f;
            float sideX = -forwardY;
            float sideY = forwardX;
            return new CompanionPoint(
                target.X - (forwardX * standOff) + (sideX * lateral * lateralSign),
                target.Y - (forwardY * standOff) + (sideY * lateral * lateralSign));
        }

        internal static bool Advance(
            ref CompanionPoint current,
            CompanionPoint target,
            float speed,
            ref float remainingDelta)
        {
            float distance = CompanionPointMath.Distance(current, target);
            if (distance <= 0.0f)
            {
                return true;
            }

            float moveDistance = speed * remainingDelta;
            if (moveDistance >= distance)
            {
                if (distance > 0.0f)
                {
                    remainingDelta -= distance / speed;
                }

                current = target;
                return true;
            }

            current = CompanionPointMath.MoveTowards(current, target, moveDistance);
            remainingDelta = 0.0f;
            return false;
        }
    }

}
