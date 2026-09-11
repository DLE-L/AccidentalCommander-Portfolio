using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Legion.Presentation
{
    public sealed class CompanionConditionView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _segmentPrefab;
        [SerializeField] private Transform _segmentsRoot;
        [SerializeField] private Transform _gaugeFill;
        [SerializeField] private SpriteRenderer _gaugeRenderer;
        [SerializeField] private float _spacing = 0.18f;
        [SerializeField] private Color _empty = new Color(0.16f, 0.12f, 0.2f, 0.8f);
        [SerializeField] private Color _filled = new Color(0.72f, 0.4f, 1f, 1f);
        [SerializeField] private Color _ready = new Color(1f, 0.82f, 0.3f, 1f);
        private readonly List<SpriteRenderer> _segments = new List<SpriteRenderer>();
        public int SegmentCount { get; private set; }
        public CompanionConditionProgress Displayed { get; private set; }

        public void Show(CompanionConditionProgress progress, CompanionConditionVisualKind kind)
        {
            Displayed = progress;
            bool ready = progress.Pending > 0;
            if (kind == CompanionConditionVisualKind.Count)
            {
                if (_segmentPrefab == null || _segmentsRoot == null)
                    throw new System.InvalidOperationException("Condition count prefab requires its authored segment and root.");
                int count = Mathf.Max(1, Mathf.CeilToInt(progress.Required));
                // The actual condition threshold determines the number of instances; reuse on pool rent.
                while (_segments.Count < count)
                    _segments.Add(Instantiate(_segmentPrefab, _segmentsRoot));
                SegmentCount = count;
                for (int i = 0; i < _segments.Count; i++)
                {
                    SpriteRenderer segment = _segments[i];
                    segment.gameObject.SetActive(i < count);
                    segment.transform.localPosition = new Vector3((i - (count - 1) * 0.5f) * _spacing, 0f, 0f);
                    segment.color = ready ? _ready : i < progress.Current ? _filled : _empty;
                }
            }
            else if (kind == CompanionConditionVisualKind.Gauge)
            {
                if (_gaugeFill == null || _gaugeRenderer == null)
                    throw new System.InvalidOperationException("Condition gauge prefab requires its authored fill.");
                float ratio = ready ? 1f : progress.Required > 0f ? Mathf.Clamp01(progress.Current / progress.Required) : 0f;
                _gaugeFill.localScale = new Vector3(ratio, 1f, 1f);
                _gaugeRenderer.color = ready ? _ready : _filled;
            }
        }
    }
}
