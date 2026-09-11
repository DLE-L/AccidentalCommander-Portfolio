using System;
using Lizzo.PV.Gameplay.Units;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    internal sealed class EnemyAreaPresentation : IDisposable
    {
        private readonly EnemyAreaAttack _source;
        private readonly SpriteRenderer _renderer;
        private readonly float _radius;
        private const float ImpactSeconds = .24f;
        private float _remaining;

        internal EnemyAreaPresentation(EnemyAreaAttack source, SpriteRenderer renderer, float radius)
        {
            _source = source; _renderer = renderer; _radius = radius;
            source.PresentationChanged += OnChanged;
        }

        private void OnChanged(EnemyAreaSignal signal, Vector2 center, float value)
        {
            if (signal == EnemyAreaSignal.Impact)
            {
                try { RetroVfx.Spawn(RetroVfxKind.BossAoeImpact, new Vector3(center.x, center.y, _source.transform.position.z)); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
            try
            {
                if (signal == EnemyAreaSignal.WarningStarted) _remaining = 0f;
                if (_renderer == null) return;
                switch (signal)
                {
                    case EnemyAreaSignal.WarningStarted:
                        _renderer.gameObject.SetActive(true);
                        _renderer.transform.position = new Vector3(center.x, center.y, _source.transform.position.z);
                        SetDiameter(_radius * 1.64f);
                        _renderer.color = new Color(1f, .18f, .05f, .52f);
                        _renderer.enabled = true;
                        break;
                    case EnemyAreaSignal.WarningProgress:
                        _renderer.transform.position = new Vector3(center.x, center.y, _source.transform.position.z);
                        SetDiameter(Mathf.Lerp(_radius * 1.64f, _radius * 2f, value));
                        _renderer.color = new Color(1f, .18f, .05f, Mathf.Lerp(.36f, .66f, Mathf.PingPong(Time.time * 5.5f, 1f)));
                        break;
                    case EnemyAreaSignal.Impact:
                        _remaining = ImpactSeconds;
                        _renderer.transform.position = new Vector3(center.x, center.y, _source.transform.position.z);
                        SetDiameter(_radius * 2.16f);
                        _renderer.color = new Color(1f, .78f, .12f, .62f);
                        _renderer.enabled = true;
                        break;
                    case EnemyAreaSignal.Tick:
                        if (_remaining <= 0f) return;
                        _remaining -= value;
                        float t = 1f - Mathf.Clamp01(_remaining / ImpactSeconds);
                        SetDiameter(Mathf.Lerp(_radius * 2.16f, _radius * 2.36f, t));
                        _renderer.color = new Color(1f, .78f, .12f, Mathf.Lerp(.62f, 0f, t));
                        if (_remaining <= 0f) _renderer.enabled = false;
                        break;
                    case EnemyAreaSignal.Hidden:
                        _renderer.enabled = false;
                        break;
                }
            }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private void SetDiameter(float diameter)
        {
            Vector2 size = _renderer.sprite == null ? Vector2.one : _renderer.sprite.bounds.size;
            _renderer.transform.localScale = Vector3.one * (diameter / Mathf.Max(.0001f, Mathf.Max(size.x, size.y)));
        }

        public void Dispose()
        {
            _source.PresentationChanged -= OnChanged;
            if (_renderer != null) _renderer.enabled = false;
        }
    }
}
