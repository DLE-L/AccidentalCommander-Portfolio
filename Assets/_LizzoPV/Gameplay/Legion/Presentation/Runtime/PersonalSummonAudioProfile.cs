using Lizzo.PV.Legion.Summons;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo/Presentation/Personal Summon Audio")]
    public sealed class PersonalSummonAudioProfile : ScriptableObject
    {
        [SerializeField] private AudioClip _spawn;
        [SerializeField] private AudioClip _attack;
        [SerializeField] private AudioClip _expire;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.6f;
        [SerializeField, Min(0f)] private float _minimumInterval = 0.08f;
        public float Volume => _volume;
        public float MinimumInterval => _minimumInterval;
        public AudioClip GetClip(PersonalSummonEventKind kind) => kind switch
        {
            PersonalSummonEventKind.Spawned => _spawn,
            PersonalSummonEventKind.Attacked => _attack,
            PersonalSummonEventKind.Expired => _expire,
            _ => null,
        };
    }
}
