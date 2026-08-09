using System;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    [DisallowMultipleComponent]
    public sealed class OwnerBoundSupportPresenterBehaviour : MonoBehaviour
    {
        [SerializeField] private AllyCombat _combat;
        [SerializeField] private Transform _owner;
        [SerializeField] private string _supportId;

        private OwnerBoundSupportPresenter _presenter;

        public string SupportId => _supportId;
        public bool IsConfigured => _presenter != null;
        public bool IsPresenting => _presenter != null && _presenter.IsPresenting;

        public void Configure(AllyCombat combat, OwnerBoundSupportPresentationData data, IPrefabFactory factory)
        {
            if (_combat == null || _owner == null)
                throw new InvalidOperationException("Owner-bound support presenter requires authored owner and combat references.");
            if (combat != _combat)
                throw new InvalidOperationException("Owner-bound support presenter must be configured with its authored AllyCombat.");
            if (data.Id != _supportId)
                throw new InvalidOperationException("Owner-bound support presenter support ID does not match authored data.");

            _presenter?.Dispose();
            _presenter = new OwnerBoundSupportPresenter(_owner, data, new PrefabFactoryAdapter(factory, data));
        }

        private void LateUpdate() => _presenter?.Observe(_combat.WolfPresentationPhase, _combat.WolfPresentationPosition, _combat.WolfPresentationDirection);

        private void OnDisable() => _presenter?.Release();

        private void OnDestroy()
        {
            _presenter?.Dispose();
            _presenter = null;
        }

        private sealed class PrefabFactoryAdapter : IOwnerBoundSupportPresentationFactory
        {
            private readonly IPrefabFactory _factory;
            private readonly OwnerBoundSupportPresentationData _data;

            public PrefabFactoryAdapter(IPrefabFactory factory, OwnerBoundSupportPresentationData data)
            {
                _factory = factory ?? throw new ArgumentNullException(nameof(factory));
                _data = data;
            }

            public bool TryCreate(OwnerBoundSupportPresentationData data, out IOwnerBoundSupportVisual visual)
            {
                GameObject instance = _factory.Spawn(data.Address);
                if (instance == null)
                {
                    visual = null;
                    return false;
                }

                UnitVisualDriver driver = instance.GetComponentInChildren<UnitVisualDriver>(true);
                if (driver == null)
                {
                    _factory.Release(instance);
                    visual = null;
                    return false;
                }

                visual = new UnitVisualAdapter(instance, driver, _data);
                return true;
            }

            public void Release(IOwnerBoundSupportVisual visual)
            {
                if (visual is UnitVisualAdapter adapter)
                    _factory.Release(adapter.Instance);
            }
        }

        private sealed class UnitVisualAdapter : IOwnerBoundSupportVisual
        {
            private readonly UnitVisualDriver _driver;
            private readonly OwnerBoundSupportPresentationData _data;
            private Vector3 _direction = Vector3.right;

            public UnitVisualAdapter(GameObject instance, UnitVisualDriver driver, OwnerBoundSupportPresentationData data)
            {
                Instance = instance;
                _driver = driver;
                _data = data;
            }

            public GameObject Instance { get; }

            public void SetPosition(Vector3 position) => Instance.transform.position = position;

            public void SetFacing(Vector3 direction)
            {
                if (direction.sqrMagnitude > 0.0f)
                    _direction = direction;
                _driver.FaceDirection(_direction);
            }

            public void SetMotion(string category)
            {
                if (category == _data.AttackMotionCategory)
                {
                    _driver.PlayAttack(_direction);
                    return;
                }

                _driver.SetMoving(category == _data.RunMotionCategory);
            }
        }
    }
}
