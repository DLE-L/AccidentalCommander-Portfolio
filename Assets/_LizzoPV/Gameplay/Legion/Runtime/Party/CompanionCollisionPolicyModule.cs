using UnityEngine;

namespace Lizzo.PV.Legion
{
    internal static class CompanionCollisionPolicyModule
    {
        internal static void ApplyCollisionPolicyToCompanion(this PartyService party, CompanionRuntime companion)
        {
            Collider2D companionBody = companion == null ? null : companion.BodyCollider;
            if (companionBody == null)
                return;

            PlayerController player = party.Registry?.Player;
            IgnoreCollision(companionBody, player == null ? null : player.BodyCollider);

            for (int i = 0; i < party.Companions.Count; i++)
            {
                CompanionRuntime other = party.Companions[i];
                if (other == null || other == companion)
                    continue;

                IgnoreCollision(companionBody, other.BodyCollider);
            }

            if (party.Registry?.Enemies == null)
                return;

            foreach (MonsterController monster in party.Registry.Enemies)
                IgnoreCollision(companionBody, monster == null ? null : monster.BodyCollider);
        }

        internal static void ApplyCollisionPolicyToEnemy(this PartyService party, MonsterController monster)
        {
            Collider2D enemyBody = monster == null ? null : monster.BodyCollider;
            if (enemyBody == null)
                return;

            PlayerController player = party.Registry?.Player;
            IgnoreCollision(player == null ? null : player.BodyCollider, enemyBody);

            for (int i = 0; i < party.Companions.Count; i++)
            {
                CompanionRuntime companion = party.Companions[i];
                IgnoreCollision(companion == null ? null : companion.BodyCollider, enemyBody);
            }
        }

        private static void IgnoreCollision(Collider2D first, Collider2D second)
        {
            if (first == null || second == null || first == second)
                return;

            Physics2D.IgnoreCollision(first, second, true);
        }
    }
}
