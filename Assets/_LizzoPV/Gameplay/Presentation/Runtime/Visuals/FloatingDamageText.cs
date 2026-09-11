using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using TMPro;
using UnityEngine;
using Lizzo.PV.Gameplay.Visuals;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Lizzo.PV.Gameplay.Visuals
{
    public sealed partial class FloatingDamageText : MonoBehaviour
    {

        private const float LIFE_TIME = 0.9f;
        private const float SHORT_LIFE_TIME = 0.55f;
        [SerializeField, Min(0f)] private float _baseRiseDistance = 0.34f;
        [SerializeField] private float _verticalOffset = 0.62f;


        private TextMeshPro _text;
        private IPrefabFactory _ownerFactory;
        private Vector3 _startPosition;
        private Vector3 _startScale;
        private Color _baseColor;
        private float _lifeTime = LIFE_TIME;
        private float _riseDistance;
        private float _elapsed;

        private void Play(Vector3 worldPosition, string label, Color color, bool large, float lifeTime)
        {
            EnsureText();

            float jitterX = Random.Range(-0.08f, 0.08f);
            _startPosition = worldPosition + new Vector3(jitterX, _verticalOffset, 0.0f);
            _startScale = large ? new Vector3(0.43f, 0.43f, 1.0f) : new Vector3(0.26f, 0.26f, 1.0f);
            _baseColor = color;
            _lifeTime = Mathf.Max(0.1f, lifeTime);
            _riseDistance = Mathf.Max(0f, _baseRiseDistance) * (large ? 1.2f : 1f);
            _elapsed = 0.0f;

            gameObject.name = "FloatingDamageText";
            transform.position = _startPosition;
            transform.localScale = _startScale;

            _text.text = label;
            _text.color = color;
        }

        private void Update()
        {
            if (_ownerFactory == null)
                return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _lifeTime);

            transform.position = Vector3.Lerp(_startPosition, _startPosition + Vector3.up * _riseDistance, t);
            transform.localScale = Vector3.Lerp(_startScale, _startScale * 1.08f, t);

            Color color = _baseColor;
            color.a = Mathf.Lerp(1.0f, 0.0f, t);
            _text.color = color;

            if (_elapsed >= _lifeTime)
                ReleaseToPool();
        }

        private void ReleaseToPool()
        {
            IPrefabFactory owner = _ownerFactory;
            if (owner == null)
                return;

            _ownerFactory = null;
            ActiveTexts.Remove(this);
            _elapsed = 0.0f;
            owner.Release(gameObject);
        }

        private void OnDisable()
        {
            ActiveTexts.Remove(this);
            _ownerFactory = null;
            _elapsed = 0.0f;
        }


        private const int SORTING_ORDER = SortingOrder.FloatingText;

private void EnsureText()
        {
            _text ??= GetComponent<TextMeshPro>();
            if (_text == null)
                Debug.LogError("[FloatingDamageText] Authored TextMeshPro is missing.", this);
        }


    }
}
