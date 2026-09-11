using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Lizzo.PV.Gameplay.Visuals
{
    [MovedFrom(true, "Lizzo.PV.P0.Visuals")]
    public sealed class RendererSortingCache : MonoBehaviour
    {
        Renderer[] _renderers;
        int[] _authoredOrders;
        int _minimumAuthoredOrder;

        public void ApplyRelative(int baseOrder)
        {
            if (_authoredOrders == null)
            {
                _renderers ??= GetComponentsInChildren<Renderer>(true);
                _authoredOrders = new int[_renderers.Length];
                _minimumAuthoredOrder = int.MaxValue;
                for (int i = 0; i < _renderers.Length; i++)
                {
                    if (_renderers[i] == null) continue;
                    _authoredOrders[i] = _renderers[i].sortingOrder;
                    _minimumAuthoredOrder = Mathf.Min(_minimumAuthoredOrder, _authoredOrders[i]);
                }
            }
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].sortingOrder = baseOrder + _authoredOrders[i] - _minimumAuthoredOrder;
            }
        }

        public void Apply(int sortingOrder)
        {
            _renderers ??= GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                    _renderers[i].sortingOrder = sortingOrder;
            }
        }
    }
}
