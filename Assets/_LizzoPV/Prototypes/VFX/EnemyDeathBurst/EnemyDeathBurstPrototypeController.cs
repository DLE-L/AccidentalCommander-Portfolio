using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Prototypes.Vfx
{
    public sealed class EnemyDeathBurstPrototypeController : MonoBehaviour
    {
        [SerializeField] private GameObject _burstPrefab;
        [SerializeField] private GameObject _smokePrefab;
        [SerializeField] private GameObject _arrivalPrefab;
        [SerializeField] private SpriteRenderer _targetRenderer;
        [SerializeField] private SpriteRenderer _commanderRenderer;
        [SerializeField] private CircleCollider2D _absorbCollider;
        [SerializeField] private GameObject _soulVisual;
        [SerializeField, Min(0.0f)] private float _initialDelaySeconds = 0.8f;
        [SerializeField, Min(0.25f)] private float _repeatIntervalSeconds = 2.4f;
        [SerializeField, Min(0.0f)] private float _targetHiddenSeconds = 1.1f;
        [SerializeField, Min(0.01f)] private float _burstScale = 1.0f;
        [SerializeField, Min(0.01f)] private float _smokeScale = 0.7f;
        [SerializeField, Min(0.01f)] private float _popDurationSeconds = 0.1f;
        [SerializeField, Min(0.1f)] private float _soulTravelSeconds = 0.72f;
        [SerializeField, Range(1, 40)] private int _soulCount = 24;
        [SerializeField, Min(0.0f)] private float _soulStaggerSeconds = 0.018f;
        [SerializeField] private Vector2 _soulOriginSpread = new Vector2(1.0f, 0.62f);
        [SerializeField] private Vector3 _soulCurveOffset = new Vector3(0.55f, 0.35f, 0.0f);
        [SerializeField] private Vector3 _burstOffset = Vector3.zero;

        private GameObject _activeEffect;
        private float _nextBurstAt;
        private float _hideTargetAt;
        private float _showTargetAt;
        private int _burstCount;
        private int _activeSoulCount;
        private int _completedSoulCount;
        private Vector3 _targetBaseScale;
        private bool _targetPopping;
        private bool _targetHidden;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private readonly List<SoulFlight> _soulFlights = new List<SoulFlight>(40);

        private sealed class SoulFlight
        {
            public GameObject Visual;
            public ParticleSystem ParticleSystem;
            public Vector3 BaseScale;
            public Vector3 Start;
            public Vector3 End;
            public Vector3 Control;
            public float StartAt;
            public float Duration;
            public bool Started;
            public bool Complete;
        }

        private void Awake()
        {
            if (_burstPrefab == null)
            {
                Debug.LogError("[EnemyDeathBurstPrototype] Burst Prefab is missing.", this);
                enabled = false;
                return;
            }

            if (_smokePrefab == null)
            {
                Debug.LogError("[EnemyDeathBurstPrototype] Smoke Prefab is missing.", this);
                enabled = false;
                return;
            }

            if (_arrivalPrefab == null || _commanderRenderer == null || _absorbCollider == null || _soulVisual == null)
            {
                Debug.LogError("[EnemyDeathBurstPrototype] Commander, absorb collider, soul, or arrival authoring is missing.", this);
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
            _targetBaseScale = _targetRenderer.transform.localScale;
            if (PrepareSoulPool() == false)
            {
                enabled = false;
                return;
            }

            _nextBurstAt = Time.time + _initialDelaySeconds;
        }

        private void Update()
        {
            if (_targetPopping)
            {
                float remaining = Mathf.Max(0.0f, _hideTargetAt - Time.time);
                float normalized = Mathf.Clamp01(remaining / _popDurationSeconds);
                float scaleMultiplier = Mathf.Lerp(0.72f, 1.12f, normalized);
                _targetRenderer.transform.localScale = _targetBaseScale * scaleMultiplier;

                if (Time.time >= _hideTargetAt)
                {
                    _targetPopping = false;
                    _targetRenderer.enabled = false;
                    _targetRenderer.transform.localScale = _targetBaseScale;
                    _targetHidden = true;
                }
            }

            if (_targetHidden && Time.time >= _showTargetAt)
            {
                _targetRenderer.enabled = true;
                _targetHidden = false;
            }

            UpdateSoulTravel();

            if (Time.time < _nextBurstAt)
                return;

            PlayBurst();
            _nextBurstAt = Time.time + _repeatIntervalSeconds;
        }

        private void OnDisable()
        {
            if (_targetRenderer != null)
            {
                _targetRenderer.enabled = true;
                _targetRenderer.transform.localScale = _targetBaseScale;
            }

            if (_activeEffect != null)
                Destroy(_activeEffect);
            for (int i = 0; i < _soulFlights.Count; i++)
            {
                SoulFlight flight = _soulFlights[i];
                flight.ParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                flight.Visual.SetActive(false);
                flight.Visual.transform.localScale = flight.BaseScale;
            }
        }

        private void OnGUI()
        {
            EnsureGuiStyles();

            const float padding = 18.0f;
            GUI.Box(new Rect(padding, padding, 430.0f, 112.0f), GUIContent.none);
            GUI.Label(new Rect(padding + 16.0f, padding + 12.0f, 398.0f, 32.0f), "PROTOTYPE · Enemy Death Burst", _titleStyle);
            GUI.Label(
                new Rect(padding + 16.0f, padding + 48.0f, 398.0f, 52.0f),
                $"Death burst → {_soulCount} XP curves → Commander · loop {_burstCount}\nProduction Gameplay is not connected.",
                _bodyStyle);
        }

        private void PlayBurst()
        {
            if (_activeEffect != null)
                Destroy(_activeEffect);

            _targetRenderer.enabled = true;
            _targetRenderer.transform.localScale = _targetBaseScale * 1.12f;
            _targetPopping = true;
            _targetHidden = false;
            _hideTargetAt = Time.time + _popDurationSeconds;
            _showTargetAt = _hideTargetAt + _targetHiddenSeconds;

            Vector3 position = _targetRenderer.bounds.center + _burstOffset;
            _activeEffect = new GameObject("Prototype_EnemyDeathEffect_Runtime");
            _activeEffect.transform.position = position;

            GameObject smoke = Instantiate(_smokePrefab, position, Quaternion.identity, _activeEffect.transform);
            smoke.name = "SmokeRadialGrey";
            smoke.transform.localScale = Vector3.one * _smokeScale;

            GameObject skull = Instantiate(_burstPrefab, position, Quaternion.identity, _activeEffect.transform);
            skull.name = "SkullBurst";
            skull.transform.localScale = Vector3.one * _burstScale;
            Destroy(_activeEffect, _repeatIntervalSeconds);

            ConfigureSoulStream();

            _burstCount += 1;
        }

        private void UpdateSoulTravel()
        {
            for (int i = 0; i < _activeSoulCount; i++)
            {
                SoulFlight flight = _soulFlights[i];
                if (flight.Complete || Time.time < flight.StartAt)
                    continue;

                if (flight.Started == false)
                {
                    flight.Started = true;
                    flight.Visual.SetActive(true);
                    flight.ParticleSystem.Clear(true);
                    ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
                    {
                        position = Vector3.zero,
                        startColor = new Color(0.65f, 1.0f, 0.38f, 1.0f),
                        startLifetime = flight.Duration + 0.18f,
                        startSize = 0.48f,
                    };
                    flight.ParticleSystem.Emit(emitParams, 1);
                    flight.ParticleSystem.Play(true);
                }

                float t = Mathf.Clamp01((Time.time - flight.StartAt) / flight.Duration);
                float eased = t * t * (3.0f - 2.0f * t);
                float inverse = 1.0f - eased;
                Vector3 previousPosition = flight.Visual.transform.position;
                Vector3 nextPosition = inverse * inverse * flight.Start
                    + 2.0f * inverse * eased * flight.Control
                    + eased * eased * flight.End;
                flight.Visual.transform.position = nextPosition;
                float pulse = 1.0f + Mathf.Sin(eased * Mathf.PI) * 0.18f;
                flight.Visual.transform.localScale = flight.BaseScale * pulse;

                if (TryResolveAbsorbHit(previousPosition, nextPosition, out Vector3 absorbPosition) == false
                    && t < 1.0f)
                    continue;

                flight.Visual.transform.position = absorbPosition;
                CompleteSoulFlight(flight, i, absorbPosition);
            }

        }

        private bool PrepareSoulPool()
        {
            Vector3 baseScale = _soulVisual.transform.localScale;
            for (int i = 0; i < _soulCount; i++)
            {
                GameObject visual = i == 0 ? _soulVisual : Instantiate(_soulVisual, transform);
                visual.name = $"PrototypeSoul_ExpGem_{i:00}";
                visual.SetActive(false);
                ParticleSystem particleSystem = visual.GetComponent<ParticleSystem>();
                if (particleSystem == null)
                {
                    Debug.LogError("[EnemyDeathBurstPrototype] Soul visual requires a ParticleSystem.", visual);
                    return false;
                }

                ConfigureSoulParticle(particleSystem);
                _soulFlights.Add(new SoulFlight
                {
                    Visual = visual,
                    ParticleSystem = particleSystem,
                    BaseScale = baseScale,
                    Complete = true,
                });
            }

            return true;
        }

        private static void ConfigureSoulParticle(ParticleSystem particleSystem)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = false;
            ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
                renderer.sortingOrder = 20;
        }

        private void ConfigureSoulStream()
        {
            _activeSoulCount = Mathf.Min(_soulCount, _soulFlights.Count);
            _completedSoulCount = 0;
            Vector3 commanderPosition = _absorbCollider.bounds.center;

            for (int i = 0; i < _activeSoulCount; i++)
            {
                SoulFlight flight = _soulFlights[i];
                float xSample = Mathf.Repeat(i * 0.618034f, 1.0f);
                float ySample = Mathf.Repeat(0.23f + i * 0.381966f, 1.0f);
                Vector3 spread = new Vector3(
                    (xSample - 0.5f) * 2.0f * _soulOriginSpread.x,
                    (ySample - 0.5f) * 2.0f * _soulOriginSpread.y,
                    0.0f);
                flight.Start = _targetRenderer.bounds.center + spread;
                flight.End = commanderPosition;
                float bendDirection = (i & 1) == 0 ? -1.0f : 1.0f;
                float bend = bendDirection * Mathf.Lerp(0.12f, _soulCurveOffset.x, ySample);
                flight.Control = (flight.Start + flight.End) * 0.5f
                    + new Vector3(bend, _soulCurveOffset.y * Mathf.Lerp(0.55f, 1.0f, xSample), 0.0f);
                flight.StartAt = _hideTargetAt + i * _soulStaggerSeconds;
                flight.Duration = _soulTravelSeconds * Mathf.Lerp(0.86f, 1.14f, xSample);
                flight.Started = false;
                flight.Complete = false;
                flight.Visual.transform.position = flight.Start;
                flight.Visual.transform.localScale = flight.BaseScale * Mathf.Lerp(0.82f, 1.05f, ySample);
                flight.Visual.SetActive(false);
            }
        }

        private bool TryResolveAbsorbHit(Vector3 previousPosition, Vector3 nextPosition, out Vector3 absorbPosition)
        {
            Vector3 center = _absorbCollider.bounds.center;
            float radius = Mathf.Max(_absorbCollider.bounds.extents.x, _absorbCollider.bounds.extents.y);
            float nextDistance = Vector2.Distance(nextPosition, center);
            if (nextDistance > radius)
            {
                absorbPosition = nextPosition;
                return false;
            }

            float previousDistance = Vector2.Distance(previousPosition, center);
            float denominator = Mathf.Max(0.0001f, previousDistance - nextDistance);
            float crossing = Mathf.Clamp01((previousDistance - radius) / denominator);
            absorbPosition = Vector3.Lerp(previousPosition, nextPosition, crossing);
            return true;
        }

        private void CompleteSoulFlight(SoulFlight flight, int index, Vector3 absorbPosition)
        {
            flight.Complete = true;
            flight.ParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            flight.Visual.SetActive(false);
            flight.Visual.transform.localScale = flight.BaseScale;
            _completedSoulCount += 1;

            bool finalArrival = _completedSoulCount >= _activeSoulCount;
            if (finalArrival == false && index % 6 != 5)
                return;

            GameObject arrival = Instantiate(_arrivalPrefab, absorbPosition, Quaternion.identity, transform);
            arrival.name = "Prototype_XpArrival_Runtime";
            arrival.transform.localScale = Vector3.one * (finalArrival ? 0.42f : 0.24f);
            Destroy(arrival, 1.2f);
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
