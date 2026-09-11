using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    public sealed class UnitStatusVisualInstance : MonoBehaviour
    {
        private ParticleSystem[] _particles;
        private SpriteRenderer[] _sprites;
        private Renderer[] _renderers;
        private Color[] _colors;
        private ParticleSystem.MinMaxGradient[] _particleColors;

        private void Awake()
        {
            _particles = GetComponentsInChildren<ParticleSystem>(true);
            _sprites = GetComponentsInChildren<SpriteRenderer>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colors = new Color[_sprites.Length];
            _particleColors = new ParticleSystem.MinMaxGradient[_particles.Length];
            for (int i = 0; i < _sprites.Length; i++) _colors[i] = _sprites[i].color;
            for (int i = 0; i < _particles.Length; i++) _particleColors[i] = _particles[i].main.startColor;
        }

        public void Play(float opacity)
        {
            for (int i = 0; i < _sprites.Length; i++)
            {
                Color color = _colors[i]; color.a *= opacity; _sprites[i].color = color;
            }
            for (int i = 0; i < _particles.Length; i++)
            {
                var main = _particles[i].main;
                var color = _particleColors[i];
                switch (color.mode)
                {
                    case ParticleSystemGradientMode.Color:
                        var value = color.color; value.a *= opacity; color.color = value; break;
                    case ParticleSystemGradientMode.TwoColors:
                        var min = color.colorMin; min.a *= opacity; color.colorMin = min;
                        var max = color.colorMax; max.a *= opacity; color.colorMax = max; break;
                    default:
                        color.gradientMin = WithOpacity(color.gradientMin, opacity);
                        color.gradientMax = WithOpacity(color.gradientMax, opacity); break;
                }
                main.startColor = color;
                _particles[i].Play(false);
            }
        }

        private static Gradient WithOpacity(Gradient source, float opacity)
        {
            if (source == null) return null;
            var alpha = source.alphaKeys;
            for (int i = 0; i < alpha.Length; i++) alpha[i].alpha *= opacity;
            var copy = new Gradient { mode = source.mode };
            copy.SetKeys(source.colorKeys, alpha);
            return copy;
        }

        public void SetOrder(int layer, int order, bool visible)
        {
            foreach (var renderer in _renderers)
            { renderer.sortingLayerID = layer; renderer.sortingOrder = order; renderer.enabled = visible; }
        }

        private void OnDisable()
        {
            if (_particles == null) return;
            foreach (var particle in _particles)
                particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
