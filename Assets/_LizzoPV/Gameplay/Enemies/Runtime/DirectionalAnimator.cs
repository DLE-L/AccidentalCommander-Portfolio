using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
    public static class DirectionalAnimator
    {
        public const string DefaultDirection = "S";

        public static string ResolveDirectionName(Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0.001f)
                return DefaultDirection;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (angle < 0.0f)
                angle += 360.0f;

            if (angle >= 337.5f || angle < 22.5f)
                return "R";
            if (angle < 67.5f)
                return "NR";
            if (angle < 112.5f)
                return "N";
            if (angle < 157.5f)
                return "NW";
            if (angle < 202.5f)
                return "W";
            if (angle < 247.5f)
                return "SW";
            if (angle < 292.5f)
                return "S";

            return "SR";
        }

        public static string ResolveRunState(string directionName)
        {
            switch (directionName)
            {
                case "R":
                    return "Run_R";
                case "NR":
                    return "Run_NR";
                case "N":
                    return "Run_N";
                case "NW":
                    return "Run_NW";
                case "W":
                    return "Run_W";
                case "SW":
                    return "Run_SW";
                case "SR":
                    return "Run_SR";
                default:
                    return "Run_S";
            }
        }

        public static string ResolveAttack3State(string directionName)
        {
            switch (directionName)
            {
                case "R":
                    return "Attack3_R";
                case "NR":
                    return "Attack3_NR";
                case "N":
                    return "Attack3_N";
                case "NW":
                    return "Attack3_NW";
                case "W":
                    return "Attack3_W";
                case "SW":
                    return "Attack3_SW";
                case "SR":
                    return "Attack3_SR";
                default:
                    return "Attack3_S";
            }
        }

        public static string ResolveDieState(string directionName)
        {
            switch (directionName)
            {
                case "R":
                    return "Die_R";
                case "NR":
                    return "Die_NR";
                case "N":
                    return "Die_N";
                case "NW":
                    return "Die_NW";
                case "W":
                    return "Die_W";
                case "SW":
                    return "Die_SW";
                case "SR":
                    return "Die_SR";
                default:
                    return "Die_S";
            }
        }

        public static bool Play(Animator animator, string stateName, ref string lastStateName)
        {
            if (animator == null || string.IsNullOrEmpty(stateName))
                return false;

            if (lastStateName == stateName)
                return false;

            int stateHash = Animator.StringToHash(stateName);
            if (animator.HasState(0, stateHash))
            {
                animator.Play(stateHash, 0, 0.0f);
                lastStateName = stateName;
                return true;
            }

            int fullPathHash = Animator.StringToHash("Base Layer." + stateName);
            if (animator.HasState(0, fullPathHash) == false)
                return false;

            animator.Play(fullPathHash, 0, 0.0f);
            lastStateName = stateName;
            return true;
        }

        public static bool PlayAny(Animator animator, ref string lastStateName, params string[] stateNames)
        {
            if (animator == null || stateNames == null)
                return false;

            for (int i = 0; i < stateNames.Length; i++)
            {
                if (lastStateName == stateNames[i])
                    return false;
            }

            for (int i = 0; i < stateNames.Length; i++)
            {
                if (Play(animator, stateNames[i], ref lastStateName))
                    return true;
            }

            return false;
        }

    }
}
