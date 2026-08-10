using System;
using System.Collections.Generic;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Visuals;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed class RetroVfx : MonoBehaviour
    {

        static IAssetService _assets;
        static IPrefabFactory _factory;

        public static void Configure(IAssetService assets, IPrefabFactory factory)
        {
            _assets = assets ?? throw new System.ArgumentNullException(nameof(assets));
            _factory = factory ?? throw new System.ArgumentNullException(nameof(factory));
        }

        public static void ClearServices()
        {
            ReportedMissingEntries.Clear();
            ReportedMissingPrefabs.Clear();
            ReportedMissingCompanionEntries.Clear();
            ReportedCompanionAudioOnlyEntries.Clear();
            _reportedMissingCatalog = false;
            _assets = null;
            _factory = null;
        }


        private static readonly HashSet<RetroVfxKind> ReportedMissingEntries = new HashSet<RetroVfxKind>();
        private static readonly HashSet<RetroVfxKind> ReportedMissingPrefabs = new HashSet<RetroVfxKind>();
        private static readonly HashSet<string> ReportedMissingCompanionEntries = new HashSet<string>();
        private static readonly HashSet<string> ReportedCompanionAudioOnlyEntries = new HashSet<string>();
        private static bool _reportedMissingCatalog;

        private ParticleSystem[] _particleSystems;
        private string _poolAddress;
        private float _destroyAt;
        private bool _usePool;

        public static bool SpawnForAttackVisual(AttackVisualKind visualKind, Vector3 position, Vector3 direction, float range)
        {
            RetroVfxKind retroKind = visualKind switch
            {
                AttackVisualKind.HealingReceived => RetroVfxKind.HealingReceived,
                AttackVisualKind.BuffApplied => RetroVfxKind.BuffApplied,
                _ => RetroVfxKind.None,
            };

            return Spawn(retroKind, position, direction, Mathf.Max(1.15f, range * 0.8f));
        }

        public static bool SpawnForAttackVisualAttached(AttackVisualKind visualKind, Transform parent, Vector3 localPosition, Vector3 direction, float range)
        {
            RetroVfxKind retroKind = visualKind switch
            {
                AttackVisualKind.HealingReceived => RetroVfxKind.HealingReceived,
                AttackVisualKind.BuffApplied => RetroVfxKind.BuffApplied,
                _ => RetroVfxKind.None,
            };

            return SpawnAttached(retroKind, parent, localPosition, direction, Mathf.Max(1.0f, range * 0.65f));
        }

        private void Update()
        {
            if (Time.time >= _destroyAt)
                ReleaseOrDestroy();
        }


        private static bool TryResolveSpec(RetroVfxKind kind, out VfxSpec spec)
        {
            if (kind == RetroVfxKind.None)
            {
                spec = default;
                return false;
            }

            if (PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog) == false)
            {
                if (_reportedMissingCatalog == false)
                {
                    _reportedMissingCatalog = true;
                    Debug.LogWarning("[RetroVfx] Presentation catalog is unavailable.");
                }

                spec = default;
                return false;
            }

            FeedbackPresentationSet feedback = catalog.Feedback;
            if (feedback == null || feedback.TryGetEntry(kind, out FeedbackPresentationSet.Entry entry) == false)
            {
                if (ReportedMissingEntries.Add(kind))
                    Debug.LogWarning($"[RetroVfx] FeedbackPresentationSet is missing the unique entry for '{kind}'.");

                spec = default;
                return false;
            }

            spec = new VfxSpec(entry);
            if (entry.Prefab == null && kind != RetroVfxKind.ResultClear)
            {
                if (ReportedMissingPrefabs.Add(kind))
                    Debug.LogWarning($"[RetroVfx] Feedback slot '{entry.SlotId}' ({kind}) has no prefab assigned.", feedback);

                spec = default;
                return false;
            }

            return true;
        }

        private static bool TryResolveCompanionAttackSpec(string effectId, out VfxSpec spec)
        {
            if (string.IsNullOrEmpty(effectId))
            {
                spec = default;
                return false;
            }

            if (PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog) == false || catalog.Feedback == null)
            {
                if (_reportedMissingCatalog == false)
                {
                    _reportedMissingCatalog = true;
                    Debug.LogWarning("[RetroVfx] Presentation catalog is unavailable.");
                }

                spec = default;
                return false;
            }

            FeedbackPresentationSet feedback = catalog.Feedback;
            if (feedback.TryGetCompanionAttackEntry(effectId, out FeedbackPresentationSet.CompanionAttackEntry entry) == false)
            {
                if (ReportedMissingCompanionEntries.Add(effectId))
                    Debug.LogWarning($"[RetroVfx] FeedbackPresentationSet is missing the companion attack entry for '{effectId}'.", feedback);

                spec = default;
                return false;
            }

            spec = new VfxSpec(entry);
            if (entry.Prefab == null && ReportedCompanionAudioOnlyEntries.Add(effectId))
                Debug.LogWarning($"[RetroVfx] Companion attack '{effectId}' has no prefab; using audio-only presentation when configured.", feedback);

            return true;
        }

        private static float ResolveFinalScale(VfxSpec spec, float scaleMultiplier)
        {
            float rawScale = Mathf.Max(0.01f, spec.Scale * Mathf.Max(0.01f, scaleMultiplier));
            return Mathf.Clamp(rawScale, spec.MinScale, spec.MaxScale);
        }

        private static int ResolveSortingOrder(RetroVfxKind kind)
        {
            return kind switch
            {
                RetroVfxKind.RedChargerCharge => SortingOrder.GroundEffect,
                _ => SortingOrder.HitEffect,
            };
        }

        private readonly struct VfxSpec
        {
            public readonly string Address;
            public readonly GameObject Prefab;
            public readonly float Lifetime;
            public readonly float Scale;
            public readonly float MinScale;
            public readonly float MaxScale;
            public readonly bool IsScaleException;
            public readonly float ForwardOffset;
            public readonly float UpOffset;
            public readonly bool AlignToDirection;
            public readonly float AngleOffset;
            public readonly string SlotId;
            public readonly string SfxId;
            public readonly AudioClip SfxClip;
            public readonly float SfxVolumeScale;
            public readonly bool IsHitFeedback;
            public readonly bool HasRewardCue;

            public VfxSpec(FeedbackPresentationSet.Entry entry)
                : this(entry, false)
            {
            }

            public VfxSpec(FeedbackPresentationSet.Entry entry, bool suppressSfx)
            {
                Address = entry.Prefab != null ? entry.Prefab.name : string.Empty;
                Prefab = entry.Prefab;
                SlotId = entry.SlotId;
                Lifetime = entry.Lifetime;
                Scale = entry.Scale;
                MinScale = entry.MinScale;
                MaxScale = entry.MaxScale;
                IsScaleException = entry.IsScaleException;
                ForwardOffset = entry.ForwardOffset;
                UpOffset = entry.UpOffset;
                AlignToDirection = entry.AlignToDirection;
                AngleOffset = entry.AngleOffset;
                SfxId = suppressSfx || entry.Sfx == null ? string.Empty : entry.Sfx.name;
                SfxClip = suppressSfx ? null : entry.Sfx;
                SfxVolumeScale = suppressSfx ? 0.0f : entry.SfxVolumeScale;
                IsHitFeedback = suppressSfx ? false : entry.IsHitFeedback;
                HasRewardCue = entry.HasRewardCue;
            }

            public VfxSpec(FeedbackPresentationSet.CompanionAttackEntry entry)
            {
                Address = entry.Prefab != null ? entry.Prefab.name : string.Empty;
                Prefab = entry.Prefab;
                SlotId = entry.EffectId;
                Lifetime = entry.Lifetime;
                Scale = entry.Scale;
                MinScale = 0.01f;
                MaxScale = float.MaxValue;
                IsScaleException = false;
                ForwardOffset = entry.ForwardOffset;
                UpOffset = entry.UpOffset;
                AlignToDirection = entry.AlignToDirection;
                AngleOffset = 0.0f;
                SfxId = entry.Sfx == null ? string.Empty : entry.Sfx.name;
                SfxClip = entry.Sfx;
                SfxVolumeScale = 1.0f;
                IsHitFeedback = false;
                HasRewardCue = false;
            }
        }


        private static bool _hasPreloadedDefaults;

        public static void PreloadDefaults()
        {
            if (_hasPreloadedDefaults)
                return;

            _hasPreloadedDefaults = true;
            Array values = Enum.GetValues(typeof(RetroVfxKind));
            for (int i = 0; i < values.Length; i++)
            {
                RetroVfxKind kind = (RetroVfxKind)values.GetValue(i);
                if (kind == RetroVfxKind.None)
                    continue;

                if (TryResolveSpec(kind, out VfxSpec spec) == false)
                    continue;

                LoadPrefab(spec);
                if (spec.SfxClip == null)
                    RetroSfx.Preload(spec.SfxId);
            }

            if (PresentationCatalogProvider.TryGetCatalog(out PresentationCatalog catalog) == false || catalog.Feedback == null)
                return;

            foreach (string effectId in FeedbackPresentationSet.CanonicalCompanionAttackEffectIds)
            {
                if (catalog.Feedback.TryGetCompanionAttackEntry(effectId, out FeedbackPresentationSet.CompanionAttackEntry entry) == false)
                    continue;

                VfxSpec spec = new VfxSpec(entry);
                LoadPrefab(spec);
                if (spec.SfxClip == null)
                    RetroSfx.Preload(spec.SfxId);
            }
        }


        private static RetroVfx GetOrCreatePooledInstance(string poolKey, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (_factory == null)
            {
                Debug.LogError("[RetroVfx] Spawn requested before RunServices factory binding.");
                return null;
            }

            GameObject instance = _factory.Rent(prefab, poolKey);
            if (instance == null)
                return null;

            instance.transform.SetPositionAndRotation(position, rotation);
            RetroVfx lifetime = instance.GetComponent<RetroVfx>();
            if (lifetime == null)
                lifetime = instance.AddComponent<RetroVfx>();

            return lifetime;
        }

        private static string BuildPoolKey(RetroVfxKind kind, GameObject prefab)
        {
            return $"RetroVfx:{(int)kind}:{prefab.GetInstanceID()}";
        }

        private static string BuildCompanionAttackPoolKey(string effectId, GameObject prefab)
        {
            return $"RetroVfx:CompanionAttack:{effectId}:{prefab.GetInstanceID()}";
        }

        private void Activate(string poolAddress, float lifetimeSeconds, bool usePool)
        {
            _poolAddress = poolAddress;
            _usePool = usePool && string.IsNullOrEmpty(poolAddress) == false;
            _destroyAt = Time.time + Mathf.Max(0.01f, lifetimeSeconds);
            RestartParticleSystems();
        }

        private void ReleaseOrDestroy()
        {
            if (_usePool == false)
            {
                Destroy(gameObject);
                return;
            }

            _factory.Release(gameObject);
        }

        private void RestartParticleSystems()
        {
            _particleSystems ??= GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = _particleSystems[i];
                if (particleSystem == null)
                    continue;

                particleSystem.Clear(withChildren: true);
                particleSystem.Play(withChildren: true);
            }
        }


        private static void RecordFeedback(VfxSpec spec, float finalScale, Vector3 position)
        {
            P0PlaytestDiagnostics.RecordFxScale(spec.SlotId, spec.Address, finalScale, spec.MinScale, spec.MaxScale, spec.IsScaleException);

            bool hasSfx = PlaySfx(spec, position);

            if (spec.IsHitFeedback)
                P0PlaytestDiagnostics.RecordHitFeedback(spec.SlotId, hasFx: true, hasSfx: hasSfx, hasHitStop: false, hasRewardCue: spec.HasRewardCue);
        }

        private static GameObject LoadPrefab(VfxSpec spec)
        {
            return spec.Prefab;
        }

        private static bool PlaySfx(VfxSpec spec, Vector3 position)
        {
            bool hasSfx = spec.SfxClip != null || string.IsNullOrEmpty(spec.SfxId) == false;
            if (hasSfx)
                RetroSfx.Play(spec.SfxClip, spec.SfxId, position, spec.SfxVolumeScale);

            return hasSfx;
        }

        private static void ForceLocalSimulation(GameObject instance)
        {
            ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem.MainModule main = particleSystems[i].main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
        }


        public static bool SpawnCompanionAttack(string effectId, Vector3 position, Vector3 direction, float range)
        {
            if (_assets == null || _factory == null || TryResolveCompanionAttackSpec(effectId, out VfxSpec spec) == false)
                return false;

            GameObject prefab = LoadPrefab(spec);
            if (prefab == null)
            {
                bool hasSfx = PlaySfx(spec, position);
                return hasSfx;
            }

            Vector3 offset = direction.sqrMagnitude > 0.0001f ? direction.normalized * spec.ForwardOffset : Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            if (spec.AlignToDirection && direction.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spec.AngleOffset;
                rotation = Quaternion.Euler(0.0f, 0.0f, angle);
            }

            Vector3 spawnPosition = position + offset + Vector3.up * spec.UpOffset;
            RetroVfx lifetime = GetOrCreatePooledInstance(BuildCompanionAttackPoolKey(effectId, prefab), prefab, spawnPosition, rotation);
            if (lifetime == null)
                return false;

            float finalScale = ResolveFinalScale(spec, Mathf.Max(1.15f, range * 0.8f));
            GameObject instance = lifetime.gameObject;
            instance.name = $"RetroVfx_CompanionAttack_{effectId}";
            instance.transform.localScale = Vector3.one * finalScale;
            SortingOrder.ApplyToRenderers(instance, SortingOrder.HitEffect);
            RecordFeedback(spec, finalScale, spawnPosition);
            lifetime.Activate(BuildCompanionAttackPoolKey(effectId, prefab), spec.Lifetime, usePool: true);
            return true;
        }

        public static bool Spawn(RetroVfxKind kind, Vector3 position, Vector3 direction = default, float scaleMultiplier = 1.0f)
        {
            if (kind == RetroVfxKind.None)
                return true;

            if (_assets == null || _factory == null)
                return false;

            if (TryResolveSpec(kind, out VfxSpec spec) == false)
                return false;

            GameObject prefab = LoadPrefab(spec);
            if (prefab == null)
            {
                if (kind == RetroVfxKind.ResultClear)
                    PlaySfx(spec, position);

                return kind == RetroVfxKind.ResultClear;
            }

            Vector3 offset = direction.sqrMagnitude > 0.0001f
                ? direction.normalized * spec.ForwardOffset
                : Vector3.zero;

            Quaternion rotation = Quaternion.identity;
            if (spec.AlignToDirection && direction.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spec.AngleOffset;
                rotation = Quaternion.Euler(0.0f, 0.0f, angle);
            }

            Vector3 spawnPosition = position + offset + Vector3.up * spec.UpOffset;
            string poolKey = BuildPoolKey(kind, prefab);
            RetroVfx lifetime = GetOrCreatePooledInstance(poolKey, prefab, spawnPosition, rotation);
            if (lifetime == null)
                return false;

            GameObject instance = lifetime.gameObject;
            instance.name = $"RetroVfx_{kind}";
            float finalScale = ResolveFinalScale(spec, scaleMultiplier);
            instance.transform.localScale = Vector3.one * finalScale;
            SortingOrder.ApplyToRenderers(instance, ResolveSortingOrder(kind));
            RecordFeedback(spec, finalScale, spawnPosition);

            lifetime.Activate(poolKey, spec.Lifetime, usePool: true);
            return true;
        }

        public static bool SpawnAttached(RetroVfxKind kind, Transform parent, Vector3 localPosition = default, Vector3 direction = default, float scaleMultiplier = 1.0f)
        {
            if (kind == RetroVfxKind.None)
                return true;

            if (parent == null)
                return false;

            if (TryResolveSpec(kind, out VfxSpec spec) == false)
                return false;

            GameObject prefab = LoadPrefab(spec);
            if (prefab == null)
            {
                if (kind == RetroVfxKind.ResultClear)
                    PlaySfx(spec, parent.position + localPosition);

                return kind == RetroVfxKind.ResultClear;
            }

            Vector3 offset = direction.sqrMagnitude > 0.0001f
                ? direction.normalized * spec.ForwardOffset
                : Vector3.zero;

            Quaternion rotation = Quaternion.identity;
            if (spec.AlignToDirection && direction.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spec.AngleOffset;
                rotation = Quaternion.Euler(0.0f, 0.0f, angle);
            }

            GameObject instance = Instantiate(prefab, parent);
            instance.name = $"RetroVfx_{kind}";
            instance.transform.localPosition = localPosition + offset + Vector3.up * spec.UpOffset;
            instance.transform.localRotation = rotation;
            float finalScale = ResolveFinalScale(spec, scaleMultiplier);
            instance.transform.localScale = Vector3.one * finalScale;
            ForceLocalSimulation(instance);
            SortingOrder.ApplyToRenderers(instance, ResolveSortingOrder(kind));
            RecordFeedback(spec, finalScale, parent.position + localPosition);

            RetroVfx lifetime = instance.GetComponent<RetroVfx>();
            if (lifetime == null)
                lifetime = instance.AddComponent<RetroVfx>();

            lifetime.Activate(string.Empty, spec.Lifetime, usePool: false);
            return true;
        }
    }
}
