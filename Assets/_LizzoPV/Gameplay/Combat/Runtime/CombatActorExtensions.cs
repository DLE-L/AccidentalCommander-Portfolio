using UnityEngine;
namespace Lizzo.PV.Gameplay.Units
{
    public static class CombatActorExtensions
    {
        public static bool IsValid(this Behaviour actor) => actor != null && actor.isActiveAndEnabled;
    }
}
