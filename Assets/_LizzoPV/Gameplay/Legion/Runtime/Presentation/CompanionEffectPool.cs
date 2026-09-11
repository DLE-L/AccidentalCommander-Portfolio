using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public interface ICompanionPooledEffect
    {
        void ResetForPool();
    }

    // Owns this run's transient companion effects; storage and reuse remain in the injected factory.
    public sealed class CompanionEffectPool : IDisposable
    {
        private readonly IPrefabFactory _factory;
        private readonly Dictionary<GameObject, ICompanionPooledEffect> _active = new Dictionary<GameObject, ICompanionPooledEffect>();
        private bool _disposed;
        private bool _stopped;

        public CompanionEffectPool(IPrefabFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public int ActiveCount => _active.Count;
        public bool CanRent => !_disposed && !_stopped;

        public T Rent<T>(T prefab) where T : Component, ICompanionPooledEffect
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CompanionEffectPool));
            if (_stopped) throw new InvalidOperationException("Companion presentation is stopped.");
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            GameObject instance = _factory.Rent(prefab.gameObject, "CompanionEffect:" + prefab.gameObject.GetInstanceID());
            if (instance == null) throw new InvalidOperationException("Companion effect factory returned no instance.");
            T component = instance.GetComponent<T>();
            if (component == null)
            {
                _factory.Release(instance);
                throw new InvalidOperationException("Companion effect prefab component is missing.");
            }
            _active.Add(instance, component);
            return component;
        }

        public void Release(Component effect)
        {
            if (effect == null || !_active.TryGetValue(effect.gameObject, out ICompanionPooledEffect pooled)) return;
            _active.Remove(effect.gameObject);
            pooled.ResetForPool();
            _factory.Release(effect.gameObject);
        }

        public void Reset()
        {
            ReleaseAll();
            _stopped = false;
        }

        public void Stop()
        {
            _stopped = true;
            ReleaseAll();
        }

        private void ReleaseAll()
        {
            // Reset is explicit: correctness does not depend on Unity callback timing.
            foreach (var pair in _active)
                if (pair.Key != null)
                {
                    pair.Value.ResetForPool();
                    _factory.Release(pair.Key);
                }
            _active.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            ReleaseAll();
        }
    }
}
