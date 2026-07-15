using System.Collections.Generic;
using Lizzo.PV.P0.Visuals;
using UnityEngine;
using System;

using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Telemetry;
using UnityEngine.AddressableAssets;

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
            PrefabCache.Clear();
            FailedAddresses.Clear();
            _assets = null;
            _factory = null;
        }


        private static readonly Dictionary<string, GameObject> PrefabCache = new Dictionary<string, GameObject>();
        private static readonly HashSet<string> FailedAddresses = new HashSet<string>();

        private ParticleSystem[] _particleSystems;
        private string _poolAddress;
        private float _destroyAt;
        private bool _usePool;

        public static bool SpawnForAttackVisual(AttackVisualKind visualKind, Vector3 position, Vector3 direction, float range)
        {
            RetroVfxKind retroKind = visualKind switch
            {
                AttackVisualKind.AreaHit => RetroVfxKind.AreaHit,
                AttackVisualKind.HealPulse => RetroVfxKind.HealPulse,
                AttackVisualKind.BuffPulse => RetroVfxKind.BuffPulse,
                AttackVisualKind.ShieldPush => RetroVfxKind.ShieldPush,
                AttackVisualKind.ForwardSlash => RetroVfxKind.ForwardSlash,
                AttackVisualKind.ArcherHit => RetroVfxKind.ArcherHit,
                _ => RetroVfxKind.SingleHit,
            };

            return Spawn(retroKind, position, direction, Mathf.Max(1.15f, range * 0.8f));
        }

        public static bool SpawnForAttackVisualAttached(AttackVisualKind visualKind, Transform parent, Vector3 localPosition, Vector3 direction, float range)
        {
            RetroVfxKind retroKind = visualKind switch
            {
                AttackVisualKind.HealPulse => RetroVfxKind.HealPulse,
                AttackVisualKind.BuffPulse => RetroVfxKind.BuffPulse,
                _ => RetroVfxKind.SingleHit,
            };

            return SpawnAttached(retroKind, parent, localPosition, direction, Mathf.Max(1.0f, range * 0.65f));
        }

        private void Update()
        {
            if (Time.time >= _destroyAt)
                ReleaseOrDestroy();
        }


        private static VfxSpec ResolveSpec(RetroVfxKind kind)
        {
            if (PresentationCatalogProvider.TryGetFeedback(kind, out FeedbackPresentationSet.Entry entry))
                return new VfxSpec(entry, kind == RetroVfxKind.ShieldOrcHit || kind == RetroVfxKind.ShieldOrcDeath);

            return kind switch
            {
                RetroVfxKind.CommanderMuzzle => new VfxSpec("Retro73_CommanderMuzzle.prefab", "commander_attack_start", 0.2f, 0.6f, 0.5f, 0.7f, false, 0.08f, 0.08f, true, 0.0f, "retro_shoot_magic", 0.9f, true, false),
                RetroVfxKind.ProjectileHit => new VfxSpec("Retro73_CommanderProjectileHit.prefab", "commander_projectile_hit", 0.25f, 0.58f, 0.5f, 0.7f, false, 0.0f, 0.0f, true, 0.0f, "retro_smack", 0.85f, true, false),
                RetroVfxKind.SingleHit => new VfxSpec("Retro73_AllyHit.prefab", "ally_hit", 0.18f, 0.58f, 0.5f, 0.7f, false, 0.0f, 0.0f, false, 0.0f, "retro_smack", 0.8f, true, false),
                RetroVfxKind.AreaHit => new VfxSpec("Retro73_AreaHit.prefab", "area_hit", 0.4f, 0.68f, 0.5f, 0.7f, false, 0.0f, 0.0f, false, 0.0f, "retro_explosion_shockwave", 0.85f, true, false),
                RetroVfxKind.HealPulse => new VfxSpec("Retro73_HealPulse.prefab", "cleric_heal", 0.7f, 0.62f, 0.5f, 0.7f, false, 0.0f, 0.0f, false, 0.0f, "retro_bling", 0.75f, true, false),
                RetroVfxKind.BuffPulse => new VfxSpec("Retro73_BuffPulse.prefab", "guard_protect_aura", 0.7f, 0.68f, 0.5f, 0.7f, false, 0.0f, 0.0f, false, 0.0f, "retro_bling", 0.55f, true, false),
                RetroVfxKind.ShieldPush => new VfxSpec("Retro73_BlockAttack.prefab", "shield_push", 0.28f, 0.58f, 0.5f, 0.7f, false, 0.0f, 0.0f, true, 0.0f, "retro_block", 0.8f, true, false),
                RetroVfxKind.ForwardSlash => new VfxSpec("Retro73_SwordsmanSlash.prefab", "swordsman_slash", 0.25f, 0.62f, 0.5f, 0.7f, false, 0.1f, 0.0f, true, 0.0f, "retro_sword_slash", 0.85f, true, false),
                RetroVfxKind.EnemyContactHit => new VfxSpec("Retro73_EnemyHitRed.prefab", "normal_enemy_hit", 0.15f, 0.52f, 0.5f, 0.7f, false, 0.0f, 0.0f, false, 0.0f, "retro_smack", 0.75f, true, false),
                RetroVfxKind.EnemyDeath => new VfxSpec("Retro73_NormalEnemyDeath.prefab", "normal_enemy_death", 0.3f, 0.58f, 0.5f, 0.7f, false, 0.0f, 0.0f, false, 0.0f, "retro_explosion_small", 0.8f, true, true),
                RetroVfxKind.ShieldOrcHit => new VfxSpec("Retro73_ShieldOrcHit.prefab", "shield_orc_hit", 0.35f, 0.64f, 0.5f, 0.7f, false, 0.0f, 0.0f, true, 0.0f, string.Empty, 0.0f, false, false),
                RetroVfxKind.ShieldOrcCrack => new VfxSpec("Retro73_ShieldOrcCrack.prefab", "shield_orc_crack", 0.45f, 0.64f, 0.5f, 0.7f, false, 0.0f, 0.42f, false, 0.0f, "retro_crack", 0.9f, true, false),
                RetroVfxKind.ShieldOrcDeath => new VfxSpec("Retro73_ShieldOrcDeath.prefab", "shield_orc_death", 0.55f, 0.7f, 0.5f, 0.7f, false, 0.0f, 0.0f, false, 0.0f, string.Empty, 0.0f, false, true),
                RetroVfxKind.RedChargerWarning => new VfxSpec("Retro73_RedChargerWarning.prefab", "red_charger_warning", 0.8f, 0.78f, 0.7f, 0.9f, true, 0.0f, 0.0f, false, 0.0f, "retro_crack", 0.65f, false, false),
                RetroVfxKind.RedChargerCharge => new VfxSpec("Retro73_RedChargerCharge.prefab", "red_charger_charge", 0.5f, 0.78f, 0.7f, 0.9f, true, 0.0f, 0.0f, true, 0.0f, "retro_shoot01", 0.7f, false, false),
                RetroVfxKind.RedChargerDeath => new VfxSpec("Retro73_RedChargerDeath.prefab", "red_charger_death", 0.7f, 0.82f, 0.7f, 0.9f, true, 0.0f, 0.0f, false, 0.0f, "retro_explosion_medium", 0.9f, true, true),
                RetroVfxKind.BossWarning => new VfxSpec("Retro73_BossWarning.prefab", "boss_warning", 1.2f, 0.9f, 0.7f, 0.9f, true, 0.0f, 0.0f, false, 0.0f, "retro_epic", 0.75f, false, false),
                RetroVfxKind.BossAttackHit => new VfxSpec("Retro73_BossAttackHit.prefab", "boss_attack_hit", 0.55f, 0.86f, 0.7f, 0.9f, true, 0.0f, 0.0f, false, 0.0f, "retro_groundslam", 0.9f, true, false),
                RetroVfxKind.BossDeath => new VfxSpec("Retro73_BossDeath.prefab", "boss_death", 1.5f, 0.9f, 0.7f, 0.9f, true, 0.0f, 0.2f, false, 0.0f, "retro_explosion_medium", 0.9f, true, true),
                RetroVfxKind.SynergyActivate => new VfxSpec("Retro73_GuardSquadComplete.prefab", "guard_squad_complete", 1.2f, 0.78f, 0.7f, 0.9f, true, 0.0f, 0.0f, false, 0.0f, "retro_epic", 0.72f, true, false),
                RetroVfxKind.GuardShockwaveHit => new VfxSpec("Retro73_BlockAttack.prefab", "guard_shield_push_hit", 0.32f, 0.74f, 0.7f, 0.9f, true, 0.0f, 0.0f, true, 0.0f, "retro_block", 0.85f, true, false),
                RetroVfxKind.GuardRadialShield => new VfxSpec("Retro_ShieldPush.prefab", "guard_radial_shield", 0.5f, 0.9f, 0.7f, 1.2f, true, 0.0f, 0.0f, false, 0.0f, "retro_block", 0.9f, true, false),
                RetroVfxKind.ArcherHit => new VfxSpec("Retro73_ArcherHit.prefab", "archer_hit", 0.22f, 0.58f, 0.5f, 0.7f, false, 0.0f, 0.0f, false, 0.0f, "retro_smack", 0.75f, true, false),
                RetroVfxKind.LevelUp => new VfxSpec("Retro73_LevelUp.prefab", "level_up", 0.8f, 0.82f, 0.7f, 0.9f, true, 0.0f, 0.0f, false, 0.0f, "retro_treasure_crystal", 0.85f, false, true),
                RetroVfxKind.CardSelect => new VfxSpec("Retro73_CardSelect.prefab", "card_select", 0.45f, 0.62f, 0.5f, 0.7f, false, 0.0f, 0.0f, false, 0.0f, "retro_bling", 0.75f, false, false),
                RetroVfxKind.ResultClear => new VfxSpec("Retro73_ResultClear.prefab", "result_clear", 1.5f, 0.9f, 0.7f, 0.9f, true, 0.0f, 0.0f, false, 0.0f, "retro_confetti_shoot", 0.85f, false, true),
                RetroVfxKind.XpAbsorb => new VfxSpec("Retro73_XpAbsorb.prefab", "exp_absorb", 0.4f, 0.58f, 0.5f, 0.7f, false, 0.0f, 0.0f, false, 0.0f, "retro_bling", 0.6f, true, true),
                _ => new VfxSpec(string.Empty, "unknown", 1.0f, 0.6f, 0.5f, 0.7f, false, 0.0f, 0.0f, false, 0.0f, string.Empty, 1.0f, false, false),
            };
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
                RetroVfxKind.RedChargerWarning => SortingOrder.GroundEffect,
                RetroVfxKind.RedChargerCharge => SortingOrder.GroundEffect,
                RetroVfxKind.BossWarning => SortingOrder.GroundEffect,
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

            public VfxSpec(
                string address,
                string slotId,
                float lifetime,
                float scale,
                float minScale,
                float maxScale,
                bool isScaleException,
                float forwardOffset,
                float upOffset,
                bool alignToDirection,
                float angleOffset,
                string sfxId,
                float sfxVolumeScale,
                bool isHitFeedback,
                bool hasRewardCue)
            {
                Address = address;
                Prefab = null;
                SlotId = slotId;
                Lifetime = lifetime;
                Scale = scale;
                MinScale = minScale;
                MaxScale = maxScale;
                IsScaleException = isScaleException;
                ForwardOffset = forwardOffset;
                UpOffset = upOffset;
                AlignToDirection = alignToDirection;
                AngleOffset = angleOffset;
                SfxId = sfxId;
                SfxClip = null;
                SfxVolumeScale = sfxVolumeScale;
                IsHitFeedback = isHitFeedback;
                HasRewardCue = hasRewardCue;
            }

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
                VfxSpec spec = ResolveSpec(kind);
                LoadPrefab(spec);
                if (spec.SfxClip == null)
                    RetroSfx.Preload(spec.SfxId);
            }
        }


private static RetroVfx GetOrCreatePooledInstance(string address, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (_factory == null)
            {
                Debug.LogError("[RetroVfx] Spawn requested before RunServices factory binding.");
                return null;
            }

            GameObject instance = _factory.Rent(prefab, address);
            if (instance == null)
                return null;

            instance.transform.SetPositionAndRotation(position, rotation);
            RetroVfx lifetime = instance.GetComponent<RetroVfx>();
            if (lifetime != null)
                return lifetime;

            Debug.LogError($"[RetroVfx] Prefab '{address}' is missing required RetroVfx component.", instance);
            _factory.Release(instance);
            return null;
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

            bool hasSfx = spec.SfxClip != null || string.IsNullOrEmpty(spec.SfxId) == false;
            if (hasSfx)
                RetroSfx.Play(spec.SfxClip, spec.SfxId, position, spec.SfxVolumeScale);

            if (spec.IsHitFeedback)
                P0PlaytestDiagnostics.RecordHitFeedback(spec.SlotId, hasFx: true, hasSfx: hasSfx, hasHitStop: false, hasRewardCue: spec.HasRewardCue);
        }

        private static GameObject LoadPrefab(VfxSpec spec)
        {
            if (spec.Prefab != null)
                return spec.Prefab;

            return LoadPrefab(spec.Address);
        }

        private static GameObject LoadPrefab(string address)
        {
            if (string.IsNullOrEmpty(address) || FailedAddresses.Contains(address))
                return null;

            if (PrefabCache.TryGetValue(address, out GameObject cachedPrefab))
                return cachedPrefab;

            GameObject prefab = _assets.GetCached<GameObject>(address);
            if (prefab == null)
            {
                FailedAddresses.Add(address);
                Debug.LogWarning($"P0 Retro VFX address not found: {address}");
                return null;
            }

            PrefabCache[address] = prefab;
            return prefab;
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


        public static bool Spawn(RetroVfxKind kind, Vector3 position, Vector3 direction = default, float scaleMultiplier = 1.0f)
        {
            VfxSpec spec = ResolveSpec(kind);
            GameObject prefab = LoadPrefab(spec);
            if (prefab == null)
                return false;

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
            RetroVfx lifetime = GetOrCreatePooledInstance(spec.Address, prefab, spawnPosition, rotation);
            if (lifetime == null)
                return false;

            GameObject instance = lifetime.gameObject;
            instance.name = $"RetroVfx_{kind}";
            float finalScale = ResolveFinalScale(spec, scaleMultiplier);
            instance.transform.localScale = Vector3.one * finalScale;
            SortingOrder.ApplyToRenderers(instance, ResolveSortingOrder(kind));
            RecordFeedback(spec, finalScale, spawnPosition);

            lifetime.Activate(spec.Address, spec.Lifetime, usePool: true);
            return true;
        }

public static bool SpawnAttached(RetroVfxKind kind, Transform parent, Vector3 localPosition = default, Vector3 direction = default, float scaleMultiplier = 1.0f)
        {
            if (parent == null)
                return false;

            VfxSpec spec = ResolveSpec(kind);
            GameObject prefab = LoadPrefab(spec);
            if (prefab == null)
                return false;

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
            {
                Debug.LogError($"[RetroVfx] Prefab '{spec.Address}' is missing required RetroVfx component.", instance);
                Destroy(instance);
                return false;
            }

            lifetime.Activate(string.Empty, spec.Lifetime, usePool: false);
            return true;
        }
    }
}
