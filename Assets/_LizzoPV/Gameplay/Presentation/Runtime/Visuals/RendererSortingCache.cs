using UnityEngine;

namespace Lizzo.PV.P0.Visuals
{
    public sealed class RendererSortingCache : MonoBehaviour
    {
        Renderer[] _renderers;

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