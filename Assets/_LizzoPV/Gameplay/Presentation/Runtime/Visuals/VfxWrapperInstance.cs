using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using System.Globalization;
using Lizzo.PV.Gameplay.Telemetry;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Visuals
{
    [RequireComponent(typeof(RendererSortingCache))]
    public sealed class VfxWrapperInstance : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _lifetimeSeconds = 0.5f;

        private ParticleSystem[] _particleSystems;
        private ParticleSystemSimulationSpace[] _authoredSimulationSpaces;
        private ParticleSystem.MinMaxGradient[] _authoredStartColors;
        private SpriteRenderer[] _spriteRenderers;
        private Color[] _authoredSpriteColors;
        private IPrefabFactory _factory;
        private float _releaseAt;
        private Vector3 _travelStart;
        private Vector3 _travelTarget;
        private float _travelStartedAt;
        private float _travelDuration;
        private bool _isTraveling;
        private bool _lifecycleActive;

        private void Update()
        {
            if (!_lifecycleActive)
                return;

            if (_isTraveling)
            {
                float progress = Mathf.Clamp01((Time.time - _travelStartedAt) / _travelDuration);
                transform.position = Vector3.Lerp(_travelStart, _travelTarget, progress);
                if (progress >= 1.0f)
                    _isTraveling = false;
            }

            if (Time.time >= _releaseAt)
                ReleaseToPool();
        }

        public void ActivatePooled(IPrefabFactory factory, float intensityMultiplier = 1.0f, bool attached = false)
        {
            _factory = factory ?? throw new System.ArgumentNullException(nameof(factory));
            PrepareForPlayback(intensityMultiplier, attached);
        }

        public void ActivatePooledTraveling(
            IPrefabFactory factory,
            Vector3 targetPosition,
            float travelSeconds,
            float intensityMultiplier = 1.0f)
        {
            ActivatePooled(factory, intensityMultiplier);
            _travelStart = transform.position;
            _travelTarget = targetPosition;
            _travelStartedAt = Time.time;
            _travelDuration = Mathf.Min(_lifetimeSeconds, Mathf.Max(0.01f, travelSeconds));
            _isTraveling = true;
        }

        private void PrepareForPlayback(float intensityMultiplier, bool attached = false)
        {
            _isTraveling = false;
            _travelStart = default;
            _travelTarget = default;
            _travelStartedAt = 0.0f;
            _travelDuration = 0.0f;
            _releaseAt = Time.time + _lifetimeSeconds;
            CacheAuthoredColors();
            ApplyIntensity(Mathf.Max(0.0f, intensityMultiplier));
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = _particleSystems[i];
                if (particleSystem == null)
                    continue;

                particleSystem.Clear(withChildren: true);
                ParticleSystem.MainModule main = particleSystem.main;
                main.simulationSpace = attached ? ParticleSystemSimulationSpace.Local : _authoredSimulationSpaces[i];
                particleSystem.Play(withChildren: true);
            }

            _lifecycleActive = true;
            LogLifecycle("activate");
        }

        private void CacheAuthoredColors()
        {
            if (_particleSystems == null)
            {
                _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
                _authoredStartColors = new ParticleSystem.MinMaxGradient[_particleSystems.Length];
                _authoredSimulationSpaces = new ParticleSystemSimulationSpace[_particleSystems.Length];
                for (int index = 0; index < _particleSystems.Length; index += 1)
                {
                    ParticleSystem particleSystem = _particleSystems[index];
                    if (particleSystem != null)
                    {
                        _authoredStartColors[index] = Clone(particleSystem.main.startColor);
                        _authoredSimulationSpaces[index] = particleSystem.main.simulationSpace;
                    }
                }
            }

            if (_spriteRenderers == null)
            {
                _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
                _authoredSpriteColors = new Color[_spriteRenderers.Length];
                for (int index = 0; index < _spriteRenderers.Length; index += 1)
                {
                    SpriteRenderer renderer = _spriteRenderers[index];
                    if (renderer != null)
                        _authoredSpriteColors[index] = renderer.color;
                }
            }
        }

        private void ApplyIntensity(float intensityMultiplier)
        {
            for (int index = 0; index < _particleSystems.Length; index += 1)
            {
                ParticleSystem particleSystem = _particleSystems[index];
                if (particleSystem == null)
                    continue;

                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = Scale(_authoredStartColors[index], intensityMultiplier);
            }

            for (int index = 0; index < _spriteRenderers.Length; index += 1)
            {
                SpriteRenderer renderer = _spriteRenderers[index];
                if (renderer != null)
                    renderer.color = ScaleRgb(_authoredSpriteColors[index], intensityMultiplier);
            }
        }

        private static ParticleSystem.MinMaxGradient Clone(ParticleSystem.MinMaxGradient source)
        {
            return source.mode switch
            {
                ParticleSystemGradientMode.Color => new ParticleSystem.MinMaxGradient(source.color),
                ParticleSystemGradientMode.TwoColors => new ParticleSystem.MinMaxGradient(source.colorMin, source.colorMax),
                ParticleSystemGradientMode.Gradient => new ParticleSystem.MinMaxGradient(Clone(source.gradient)),
                ParticleSystemGradientMode.TwoGradients => new ParticleSystem.MinMaxGradient(
                    Clone(source.gradientMin),
                    Clone(source.gradientMax)),
                ParticleSystemGradientMode.RandomColor => CreateRandomColorGradient(Clone(source.gradient)),
                _ => new ParticleSystem.MinMaxGradient(source.color),
            };
        }

        private static ParticleSystem.MinMaxGradient Scale(
            ParticleSystem.MinMaxGradient source,
            float intensityMultiplier)
        {
            return source.mode switch
            {
                ParticleSystemGradientMode.Color => new ParticleSystem.MinMaxGradient(
                    ScaleRgb(source.color, intensityMultiplier)),
                ParticleSystemGradientMode.TwoColors => new ParticleSystem.MinMaxGradient(
                    ScaleRgb(source.colorMin, intensityMultiplier),
                    ScaleRgb(source.colorMax, intensityMultiplier)),
                ParticleSystemGradientMode.Gradient => new ParticleSystem.MinMaxGradient(
                    Scale(source.gradient, intensityMultiplier)),
                ParticleSystemGradientMode.TwoGradients => new ParticleSystem.MinMaxGradient(
                    Scale(source.gradientMin, intensityMultiplier),
                    Scale(source.gradientMax, intensityMultiplier)),
                ParticleSystemGradientMode.RandomColor => CreateRandomColorGradient(
                    Scale(source.gradient, intensityMultiplier)),
                _ => new ParticleSystem.MinMaxGradient(ScaleRgb(source.color, intensityMultiplier)),
            };
        }

        private static ParticleSystem.MinMaxGradient CreateRandomColorGradient(Gradient gradient)
        {
            ParticleSystem.MinMaxGradient result = new ParticleSystem.MinMaxGradient(gradient);
            result.mode = ParticleSystemGradientMode.RandomColor;
            return result;
        }

        private static Gradient Clone(Gradient source)
        {
            if (source == null)
                return new Gradient();

            Gradient clone = new Gradient();
            clone.SetKeys(source.colorKeys, source.alphaKeys);
            clone.mode = source.mode;
            return clone;
        }

        private static Gradient Scale(Gradient source, float intensityMultiplier)
        {
            if (source == null)
                return new Gradient();

            GradientColorKey[] colorKeys = source.colorKeys;
            for (int index = 0; index < colorKeys.Length; index += 1)
            {
                GradientColorKey key = colorKeys[index];
                key.color = ScaleRgb(key.color, intensityMultiplier);
                colorKeys[index] = key;
            }

            Gradient scaled = new Gradient();
            scaled.SetKeys(colorKeys, source.alphaKeys);
            scaled.mode = source.mode;
            return scaled;
        }

        private static Color ScaleRgb(Color color, float intensityMultiplier)
        {
            return new Color(
                color.r * intensityMultiplier,
                color.g * intensityMultiplier,
                color.b * intensityMultiplier,
                color.a);
        }

        public void ReleaseToPool()
        {
            if (!_lifecycleActive)
                return;

            _isTraveling = false;
            LogLifecycle("release");
            _lifecycleActive = false;
            IPrefabFactory factory = _factory;
            _factory = null;
            factory.Release(gameObject);
        }

        private void OnDisable()
        {
            if (!_lifecycleActive)
                return;

            LogLifecycle("external_disable");
            _lifecycleActive = false;
            _isTraveling = false;
            _factory = null;
        }

        private void LogLifecycle(string state)
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return;

            RunTelemetry.Log(
                RunTelemetry.VfxLifecycle,
                "kind=wrapper",
                $"state={state}",
                $"vfx_id={gameObject.name}",
                $"instance_id={GetInstanceID()}",
                $"pooled={(_factory != null).ToString().ToLowerInvariant()}",
                $"scaled_time={Time.time.ToString("0.###", CultureInfo.InvariantCulture)}",
                $"realtime={Time.realtimeSinceStartup.ToString("0.###", CultureInfo.InvariantCulture)}",
                $"time_scale={Time.timeScale.ToString("0.###", CultureInfo.InvariantCulture)}",
                $"release_at={_releaseAt.ToString("0.###", CultureInfo.InvariantCulture)}");
        }
    }
}
