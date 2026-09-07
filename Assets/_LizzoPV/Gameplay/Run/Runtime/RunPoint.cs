using System;

namespace Lizzo.PV.Gameplay.Run
{
    public readonly struct RunPoint : IEquatable<RunPoint>
    {
        public static RunPoint Zero => default;

        public float X { get; }
        public float Y { get; }

        public RunPoint(float x, float y)
        {
            if (float.IsNaN(x) || float.IsInfinity(x))
                throw new ArgumentOutOfRangeException(nameof(x));
            if (float.IsNaN(y) || float.IsInfinity(y))
                throw new ArgumentOutOfRangeException(nameof(y));

            X = x;
            Y = y;
        }

        public bool Equals(RunPoint other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is RunPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public static bool operator ==(RunPoint left, RunPoint right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RunPoint left, RunPoint right)
        {
            return !left.Equals(right);
        }

        internal static float DistanceSquared(RunPoint left, RunPoint right)
        {
            float x = left.X - right.X;
            float y = left.Y - right.Y;
            return x * x + y * y;
        }
    }
}
