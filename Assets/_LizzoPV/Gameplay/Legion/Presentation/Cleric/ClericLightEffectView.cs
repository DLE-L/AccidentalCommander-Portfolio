using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    // Authored sprite/VFX only. Flight time and hit decisions remain in the combat system.
    public sealed class ClericLightEffectView : MonoBehaviour, ICompanionPooledEffect
    {
        private ParticleSystem[] _particles;
        private void Awake() => _particles = GetComponentsInChildren<ParticleSystem>(true);
        private CompanionEffectPool _pool;
        private ReturningAttackFlight _flight;
        private bool _outbound;

        internal static void Play(CompanionEffectPool pool, ClericLightEffectView prefab,
            Vector3 position, ReturningAttackFlight flight, bool outbound = false)
        {
            if (prefab == null || !pool.CanRent) return;
            var view = pool.Rent(prefab);
            view._pool = pool;
            view._flight = flight;
            view._outbound = outbound;
            view.transform.SetPositionAndRotation(position, prefab.transform.rotation);
            view.transform.localScale = prefab.transform.localScale;
            foreach (var particle in view._particles)
            { particle.Clear(false); particle.Play(false); }
        }

        private void LateUpdate()
        {
            if (_pool == null) return;
            if (_flight == null || _flight.IsComplete || (_outbound && _flight.IsReturning))
            { Release(); return; }
            transform.position = _flight.Position;
        }

        private void Release()
        {
            var pool = _pool;
            pool?.Release(this);
        }
        public void ResetForPool()
        {
            if (_particles != null)
                foreach (var particle in _particles) particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            _pool = null; _flight = null; _outbound = false;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
