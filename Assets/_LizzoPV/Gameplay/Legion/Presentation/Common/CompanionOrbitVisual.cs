using System;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    internal sealed class CompanionOrbitVisual : IDisposable
    {
        private readonly CompanionOrbitAttack _system;
        private readonly IPrefabFactory _factory;
        private readonly GameObject _bladePrefab, _boundaryPrefab;
        private GameObject _blade, _boundary;
        private LineRenderer _boundaryLine;
        private Vector3[] _boundaryUnitPoints, _boundaryPoints;
        internal CompanionOrbitVisual(CompanionOrbitAttack system, IPrefabFactory factory, GameObject blade, GameObject boundary)
        {
            _system = system; _factory = factory; _bladePrefab = blade; _boundaryPrefab = boundary;
            var authoredLine = boundary == null ? null : boundary.GetComponent<LineRenderer>();
            if (authoredLine != null)
            {
                _boundaryUnitPoints = new Vector3[authoredLine.positionCount];
                _boundaryPoints = new Vector3[authoredLine.positionCount];
                authoredLine.GetPositions(_boundaryUnitPoints);
            }
            system.Changed += Apply;
        }
        private void Apply(CompanionOrbitSnapshot state)
        {
            if (!state.Active) { Clear(); return; }
            if (_blade == null && _bladePrefab != null) _blade = _factory.Rent(_bladePrefab, _bladePrefab.name);
            if (_boundary == null && _boundaryPrefab != null)
            {
                _boundary = _factory.Rent(_boundaryPrefab, _boundaryPrefab.name);
                if (_boundary != null)
                {
                    _boundaryLine = _boundary.GetComponent<LineRenderer>();

                }
            }
            if (_blade != null)
            {
                var delta = state.Position - state.Center;
                _blade.transform.SetPositionAndRotation(state.Position, Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg + 90f));
                _blade.transform.localScale = _bladePrefab.transform.localScale;
            }
            if (_boundary != null)
            {
                _boundary.transform.position = state.Center;
                _boundary.transform.localScale = Vector3.one;
                if (_boundaryLine != null && _boundaryPoints != null)
                {
                    for (int i = 0; i < _boundaryPoints.Length; i++) _boundaryPoints[i] = _boundaryUnitPoints[i] * state.Radius;
                    _boundaryLine.SetPositions(_boundaryPoints);
                    _boundaryLine.widthMultiplier = state.Width * 2f;
                }
            }
        }
        private void Clear()
        { if (_blade != null) _factory.Release(_blade); if (_boundary != null) _factory.Release(_boundary); _blade = null; _boundary = null; _boundaryLine = null; }
        public void Dispose() { _system.Changed -= Apply; Clear(); }
    }
}
