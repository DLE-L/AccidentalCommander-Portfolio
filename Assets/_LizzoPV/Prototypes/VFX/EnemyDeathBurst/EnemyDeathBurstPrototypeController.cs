using UnityEngine;

namespace Lizzo.PV.Prototypes.Vfx
{
    public sealed class EnemyDeathBurstPrototypeController : MonoBehaviour
    {
        [SerializeField] private GameObject _burstPrefab;
        [SerializeField] private SpriteRenderer _targetRenderer;
        [SerializeField, Min(0.0f)] private float _initialDelaySeconds = 0.8f;
        [SerializeField, Min(0.25f)] private float _repeatIntervalSeconds = 2.4f;
        [SerializeField, Min(0.0f)] private float _targetHiddenSeconds = 1.1f;
        [SerializeField, Min(0.01f)] private float _burstScale = 1.0f;
        [SerializeField] private Vector3 _burstOffset = Vector3.zero;

        private GameObject _activeBurst;
        private float _nextBurstAt;
        private float _showTargetAt;
        private int _burstCount;
        private bool _targetHidden;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;

        private void Awake()
        {
            if (_burstPrefab == null)
            {
                Debug.LogError("[EnemyDeathBurstPrototype] Burst Prefab is missing.", this);
                enabled = false;
                return;
            }

            if (_targetRenderer == null)
            {
                Debug.LogError("[EnemyDeathBurstPrototype] Target SpriteRenderer is missing.", this);
                enabled = false;
                return;
            }

            _targetRenderer.enabled = true;
            _nextBurstAt = Time.time + _initialDelaySeconds;
        }

        private void Update()
        {
            if (_targetHidden && Time.time >= _showTargetAt)
            {
                _targetRenderer.enabled = true;
                _targetHidden = false;
            }

            if (Time.time < _nextBurstAt)
                return;

            PlayBurst();
            _nextBurstAt = Time.time + _repeatIntervalSeconds;
        }

        private void OnDisable()
        {
            if (_targetRenderer != null)
                _targetRenderer.enabled = true;

            if (_activeBurst != null)
                Destroy(_activeBurst);
        }

        private void OnGUI()
        {
            EnsureGuiStyles();

            const float padding = 18.0f;
            GUI.Box(new Rect(padding, padding, 430.0f, 112.0f), GUIContent.none);
            GUI.Label(new Rect(padding + 16.0f, padding + 12.0f, 398.0f, 32.0f), "PROTOTYPE · Enemy Death Burst", _titleStyle);
            GUI.Label(
                new Rect(padding + 16.0f, padding + 48.0f, 398.0f, 52.0f),
                $"SkullBurst auto-repeat · count {_burstCount}\nProduction Gameplay is not connected.",
                _bodyStyle);
        }

        private void PlayBurst()
        {
            if (_activeBurst != null)
                Destroy(_activeBurst);

            _targetRenderer.enabled = false;
            _targetHidden = true;
            _showTargetAt = Time.time + _targetHiddenSeconds;

            Vector3 position = _targetRenderer.transform.position + _burstOffset;
            _activeBurst = Instantiate(_burstPrefab, position, Quaternion.identity);
            _activeBurst.name = "Prototype_SkullBurst_Runtime";
            _activeBurst.transform.localScale = Vector3.one * _burstScale;
            Destroy(_activeBurst, _repeatIntervalSeconds);

            _burstCount += 1;
        }

        private void EnsureGuiStyles()
        {
            if (_titleStyle != null)
                return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
            };

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                normal = { textColor = new Color(0.82f, 0.9f, 0.95f, 1.0f) },
            };
        }
    }
}
