using System;

namespace Lizzo.PV.Gameplay.Run
{
    public static class FormationLayout
    {
        public const int GroupCapacity = 7;
        public const int MemberCapacity = 3;
        public const int SlotCapacity = GroupCapacity * MemberCapacity;

        public static string CreateSlotId(int groupIndex, int memberIndex)
        {
            if (groupIndex < 0 || groupIndex >= GroupCapacity)
                throw new ArgumentOutOfRangeException(nameof(groupIndex));
            if (memberIndex < 1 || memberIndex > MemberCapacity)
                throw new ArgumentOutOfRangeException(nameof(memberIndex));
            return string.Concat((char)('A' + groupIndex), memberIndex.ToString());
        }

        public static RunPoint ResolveGroupCenter(int activeGroupCount, int groupIndex, float radius)
        {
            if (activeGroupCount < 1 || activeGroupCount > GroupCapacity)
                throw new ArgumentOutOfRangeException(nameof(activeGroupCount));
            if (groupIndex < 0 || groupIndex >= activeGroupCount)
                throw new ArgumentOutOfRangeException(nameof(groupIndex));
            if (radius <= 0.0f)
                throw new ArgumentOutOfRangeException(nameof(radius));

            double step = Math.PI * 2.0 / activeGroupCount;
            double angle;
            if ((activeGroupCount & 1) != 0)
            {
                if (groupIndex == 0)
                {
                    angle = -Math.PI * 0.5;
                }
                else
                {
                    int pair = (groupIndex + 1) / 2;
                    int sign = (groupIndex & 1) != 0 ? 1 : -1;
                    angle = -Math.PI * 0.5 + sign * pair * step;
                }
            }
            else
            {
                int pair = groupIndex / 2;
                int sign = (groupIndex & 1) == 0 ? -1 : 1;
                angle = -Math.PI * 0.5 + sign * (pair + 0.5) * step;
            }

            return new RunPoint(
                (float)(Math.Cos(angle) * radius),
                (float)(Math.Sin(angle) * radius));
        }

        public static RunPoint ResolveSlotOffset(
            RunPoint groupCenter,
            int memberIndex,
            float frontDepth,
            float rearDepth,
            float halfWidth)
        {
            float length = (float)Math.Sqrt(groupCenter.X * groupCenter.X + groupCenter.Y * groupCenter.Y);
            float outwardX = length <= 0.0001f ? 0.0f : groupCenter.X / length;
            float outwardY = length <= 0.0001f ? -1.0f : groupCenter.Y / length;
            float rightX = -outwardY;
            float rightY = outwardX;
            if (memberIndex == 1)
            {
                return new RunPoint(
                    groupCenter.X + outwardX * frontDepth - rightX * halfWidth,
                    groupCenter.Y + outwardY * frontDepth - rightY * halfWidth);
            }
            if (memberIndex == 2)
            {
                return new RunPoint(
                    groupCenter.X + outwardX * frontDepth + rightX * halfWidth,
                    groupCenter.Y + outwardY * frontDepth + rightY * halfWidth);
            }
            return new RunPoint(
                groupCenter.X - outwardX * rearDepth,
                groupCenter.Y - outwardY * rearDepth);
        }
    }
}
