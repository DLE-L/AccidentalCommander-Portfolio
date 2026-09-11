using UnityEngine;

namespace Lizzo.PV.Legion.Summons
{
    public enum PersonalSummonEventKind { Spawned, Attacked, Expired }
    public readonly struct PersonalSummonEvent
    {
        public PersonalSummonEvent(string summonId, PersonalSummonEventKind kind, Vector3 position)
        { SummonId = summonId; Kind = kind; Position = position; }
        public string SummonId { get; }
        public PersonalSummonEventKind Kind { get; }
        public Vector3 Position { get; }
    }
}
