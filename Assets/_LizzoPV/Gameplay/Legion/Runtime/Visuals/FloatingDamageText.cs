using TMPro;
using UnityEngine;
using Lizzo.PV.P0.Visuals;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Lizzo.PV.Legion
{
    public sealed partial class FloatingDamageText : MonoBehaviour
    {

        private const float LIFE_TIME = 0.9f;
        private const float SHORT_LIFE_TIME = 0.55f;
        private const float RISE_DISTANCE = 0.34f;


        private TextMeshPro _text;
        private Vector3 _startPosition;
        private Vector3 _startScale;
        private Color _baseColor;
        private float _lifeTime = LIFE_TIME;
        private float _riseDistance = RISE_DISTANCE;
        private float _elapsed;

        private void Play(Vector3 worldPosition, string label, Color color, bool large, float lifeTime)
        {
            EnsureText();

            float jitterX = Random.Range(-0.08f, 0.08f);
            _startPosition = worldPosition + new Vector3(jitterX, 0.62f, 0.0f);
            _startScale = large ? new Vector3(0.43f, 0.43f, 1.0f) : new Vector3(0.26f, 0.26f, 1.0f);
            _baseColor = color;
            _lifeTime = Mathf.Max(0.1f, lifeTime);
            _riseDistance = large ? RISE_DISTANCE * 1.2f : RISE_DISTANCE;
            _elapsed = 0.0f;

            gameObject.name = "FloatingDamageText";
            transform.position = _startPosition;
            transform.localScale = _startScale;

            _text.text = label;
            _text.color = color;
        }

        private void Update()
        {
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
            _elapsed = 0.0f;
            _factory.Release(gameObject);
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
