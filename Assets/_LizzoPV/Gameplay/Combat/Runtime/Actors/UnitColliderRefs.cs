using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
    public sealed class UnitColliderRefs : MonoBehaviour
    {
        [SerializeField] private Collider2D _combatCollider;
        [SerializeField] private Collider2D _bodyCollider;

        public Collider2D CombatCollider => _combatCollider;
        public Collider2D BodyCollider => _bodyCollider;
    }
}
