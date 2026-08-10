using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed class VfxWrapperInstance : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _lifetimeSeconds = 0.5f;

        private ParticleSystem[] _particleSystems;
        private IPrefabFactory _factory;
        private float _releaseAt;
        private bool _pooled;

        private void Update()
        {
            if (Time.time >= _releaseAt)
                ReleaseOrDestroy();
        }

        public void ActivatePooled(IPrefabFactory factory)
        {
            _factory = factory ?? throw new System.ArgumentNullException(nameof(factory));
            _pooled = true;
            PrepareForPlayback();
        }

        public void ActivateTransient()
        {
            _factory = null;
            _pooled = false;
            PrepareForPlayback();
        }

        private void PrepareForPlayback()
        {
            _releaseAt = Time.time + _lifetimeSeconds;
            _particleSystems ??= GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = _particleSystems[i];
                if (particleSystem == null)
                    continue;

                particleSystem.Clear(withChildren: true);
                particleSystem.Play(withChildren: true);
            }
        }

        private void ReleaseOrDestroy()
        {
            if (_pooled && _factory != null)
            {
                _factory.Release(gameObject);
                return;
            }

            Destroy(gameObject);
        }
    }
}
