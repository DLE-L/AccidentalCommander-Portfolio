using System;
using Lizzo.PV.Gameplay.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    public readonly struct OwnerBoundSupportPresentationData
    {
        public readonly string Id;
        public readonly string Address;
        public readonly string RunMotionCategory;
        public readonly string AttackMotionCategory;

        public OwnerBoundSupportPresentationData(string id, string address, string runMotionCategory = "Run", string attackMotionCategory = "Attack")
        {
            Id = id ?? string.Empty;
            Address = address ?? string.Empty;
            RunMotionCategory = string.IsNullOrEmpty(runMotionCategory) ? "Run" : runMotionCategory;
            AttackMotionCategory = string.IsNullOrEmpty(attackMotionCategory) ? "Attack" : attackMotionCategory;
        }

        public bool IsValid => string.IsNullOrEmpty(Id) == false && string.IsNullOrEmpty(Address) == false;
    }

    public interface IOwnerBoundSupportVisual
    {
        void SetPosition(Vector3 position);
        void SetFacing(Vector3 direction);
        void SetMotion(string category);
    }

    public interface IOwnerBoundSupportPresentationFactory
    {
        bool TryCreate(OwnerBoundSupportPresentationData data, out IOwnerBoundSupportVisual visual);
        void Release(IOwnerBoundSupportVisual visual);
    }

    public sealed class OwnerBoundSupportPresenter : IDisposable
    {
        private readonly Transform _owner;
        private readonly OwnerBoundSupportPresentationData _data;
        private readonly IOwnerBoundSupportPresentationFactory _factory;
        private IOwnerBoundSupportVisual _visual;
        private bool _creationAttempted;
        private bool _disposed;

        public OwnerBoundSupportPresenter(Transform owner, OwnerBoundSupportPresentationData data, IOwnerBoundSupportPresentationFactory factory)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _data = data;
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public bool IsPresenting => _visual != null;

        public void Observe(WolfOwnedProxyPhase phase, Vector3 proxyPosition, Vector3 direction)
        {
            if (_disposed)
            {
                Release();
                return;
            }

            if (phase == WolfOwnedProxyPhase.Inactive)
            {
                _creationAttempted = false;
                Release();
                return;
            }

            if (_visual == null)
            {
                if (_creationAttempted)
                    return;

                _creationAttempted = true;
                if (_data.IsValid == false || _factory.TryCreate(_data, out _visual) == false || _visual == null)
                    return;
            }

            _visual.SetPosition(proxyPosition);
            _visual.SetFacing(direction.sqrMagnitude > 0.0f ? direction : _owner.right);
            _visual.SetMotion(phase == WolfOwnedProxyPhase.Impact ? "Attack" : "Run");
        }

        public void Release()
        {
            if (_visual == null)
                return;

            _factory.Release(_visual);
            _visual = null;
            _creationAttempted = false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Release();
        }
    }

}
