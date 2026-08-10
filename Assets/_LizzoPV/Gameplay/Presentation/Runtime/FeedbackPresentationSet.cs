using System;
using System.Collections.Generic;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
    public enum EventSfxPolicy
    {
        Optional,
        Required,
        None,
    }

    public enum ActorFeedbackProfile
    {
        None,
        Flash,
        FlashAndShake,
        Shake,
    }

    public sealed class FeedbackPresentationSet : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField]
            private string _slotId;

            [SerializeField]
            private RetroVfxKind _kind;

            [SerializeField]
            private GameObject _prefab;

            [SerializeField]
            private AudioClip _sfx;

            [SerializeField]
            private EventSfxPolicy _sfxPolicy = EventSfxPolicy.Optional;

            [SerializeField]
            private ActorFeedbackProfile _actorFeedback = ActorFeedbackProfile.None;

            [SerializeField]
            private float _lifetime = 0.5f;

            [SerializeField]
            private float _scale = 0.7f;

            [SerializeField]
            private float _minScale = 0.5f;

            [SerializeField]
            private float _maxScale = 0.9f;

            [SerializeField]
            private bool _isScaleException;

            [SerializeField]
            private float _forwardOffset;

            [SerializeField]
            private float _upOffset;

            [SerializeField]
            private bool _alignToDirection;

            [SerializeField]
            private Vector3 _rotationEuler;

            [SerializeField]
            private float _angleOffset;

            [SerializeField]
            private float _sfxVolumeScale = 1.0f;

            [SerializeField]
            private bool _isHitFeedback;

            [SerializeField]
            private bool _hasRewardCue;

            public Entry(
                RetroVfxKind kind,
                string slotId,
                GameObject prefab,
                AudioClip sfx,
                float lifetime,
                float scale,
                float minScale,
                float maxScale,
                bool isScaleException,
                float forwardOffset,
                float upOffset,
                bool alignToDirection,
                float angleOffset,
                float sfxVolumeScale,
                bool isHitFeedback,
                bool hasRewardCue,
                EventSfxPolicy sfxPolicy = EventSfxPolicy.Optional,
                ActorFeedbackProfile actorFeedback = ActorFeedbackProfile.None,
                Vector3 rotationEuler = default)
            {
                _kind = kind;
                _slotId = slotId;
                _prefab = prefab;
                _sfx = sfx;
                _sfxPolicy = sfxPolicy;
                _actorFeedback = actorFeedback;
                _lifetime = lifetime;
                _scale = scale;
                _minScale = minScale;
                _maxScale = maxScale;
                _isScaleException = isScaleException;
                _forwardOffset = forwardOffset;
                _upOffset = upOffset;
                _alignToDirection = alignToDirection;
                _rotationEuler = rotationEuler;
                _angleOffset = angleOffset;
                _sfxVolumeScale = sfxVolumeScale;
                _isHitFeedback = isHitFeedback;
                _hasRewardCue = hasRewardCue;
            }

            public RetroVfxKind Kind => _kind;
            public string SlotId => _slotId;
            public GameObject Prefab => _prefab;
            public AudioClip Sfx => _sfx;
            public EventSfxPolicy SfxPolicy => _sfxPolicy;
            public ActorFeedbackProfile ActorFeedback => _actorFeedback;
            public float Lifetime => _lifetime;
            public float Scale => _scale;
            public float MinScale => _minScale;
            public float MaxScale => _maxScale;
            public bool IsScaleException => _isScaleException;
            public float ForwardOffset => _forwardOffset;
            public float UpOffset => _upOffset;
            public bool AlignToDirection => _alignToDirection;
            public Vector3 RotationEuler => _rotationEuler;
            public float AngleOffset => _angleOffset;
            public float SfxVolumeScale => _sfxVolumeScale;
            public bool IsHitFeedback => _isHitFeedback;
            public bool HasRewardCue => _hasRewardCue;
        }

        [SerializeField]
        private Entry[] _entries = Array.Empty<Entry>();

        [Serializable]
        public sealed class CompanionAttackEntry
        {
            [SerializeField] private string _effectId;
            [SerializeField] private GameObject _prefab;
            [SerializeField] private AudioClip _sfx;
            [SerializeField] private EventSfxPolicy _sfxPolicy = EventSfxPolicy.Optional;
            [SerializeField] private ActorFeedbackProfile _actorFeedback = ActorFeedbackProfile.None;
            [SerializeField] private float _scale = 0.7f;
            [SerializeField] private float _lifetime = 0.5f;
            [SerializeField] private float _forwardOffset;
            [SerializeField] private float _upOffset;
            [SerializeField] private bool _alignToDirection;
            [SerializeField] private Vector3 _rotationEuler;

            public CompanionAttackEntry(
                string effectId,
                GameObject prefab,
                AudioClip sfx,
                float scale,
                float lifetime,
                float forwardOffset,
                float upOffset,
                bool alignToDirection,
                EventSfxPolicy sfxPolicy = EventSfxPolicy.Optional,
                ActorFeedbackProfile actorFeedback = ActorFeedbackProfile.None,
                Vector3 rotationEuler = default)
            {
                _effectId = effectId;
                _prefab = prefab;
                _sfx = sfx;
                _sfxPolicy = sfxPolicy;
                _actorFeedback = actorFeedback;
                _scale = scale;
                _lifetime = lifetime;
                _forwardOffset = forwardOffset;
                _upOffset = upOffset;
                _alignToDirection = alignToDirection;
                _rotationEuler = rotationEuler;
            }

            public string EffectId => _effectId;
            public GameObject Prefab => _prefab;
            public AudioClip Sfx => _sfx;
            public EventSfxPolicy SfxPolicy => _sfxPolicy;
            public ActorFeedbackProfile ActorFeedback => _actorFeedback;
            public float Scale => _scale;
            public float Lifetime => _lifetime;
            public float ForwardOffset => _forwardOffset;
            public float UpOffset => _upOffset;
            public Vector3 RotationEuler => _rotationEuler;
        }

        private static readonly string[] CompanionAttackEffectIds =
        {
            "dmg_shield_bash_v1", "dmg_sword_slash_v1", "dmg_cleric_bolt_v1", "dmg_falcon_arrow_v1",
            "dmg_herbal_dart_v1", "dmg_bomb_explosion_v1", "dot_fire_field_v1", "dmg_chain_lightning_v1",
            "dmg_wolf_assault_v1", "dmg_wraith_slash_v1", "dmg_curse_bolt_v1", "dmg_skeleton_bomb_v1",
        };

        public static IReadOnlyList<string> CanonicalCompanionAttackEffectIds => CompanionAttackEffectIds;

        [SerializeField]
        private CompanionAttackEntry[] _companionAttacks = Array.Empty<CompanionAttackEntry>();

        [Serializable]
        public sealed class ProjectileVisualEntry
        {
            [SerializeField] private string _presentationId;
            [SerializeField] private Sprite _bodySprite;
            [SerializeField] private Color _tint = Color.white;
            [SerializeField] private Vector3 _scale = Vector3.one;
            [SerializeField] private Vector3 _rotationEuler;

            public ProjectileVisualEntry(
                string presentationId,
                Sprite bodySprite,
                Color tint,
                Vector3 scale,
                Vector3 rotationEuler)
            {
                _presentationId = presentationId;
                _bodySprite = bodySprite;
                _tint = tint;
                _scale = scale;
                _rotationEuler = rotationEuler;
            }

            public string PresentationId => _presentationId;
            public Sprite BodySprite => _bodySprite;
            public Color Tint => _tint;
            public Vector3 Scale => _scale;
            public Vector3 RotationEuler => _rotationEuler;
        }

        private static readonly string[] ProjectilePresentationIds =
        {
            "commander_basic",
            "commander_rapid_crossbow",
            "commander_piercing_spear",
            "commander_blast_staff",
            "dmg_cleric_bolt_v1",
            "dmg_falcon_arrow_v1",
            "dmg_herbal_dart_v1",
            "dmg_curse_bolt_v1",
            "synergy_magic_chain",
        };

        public static IReadOnlyList<string> CanonicalProjectilePresentationIds => ProjectilePresentationIds;

        [SerializeField]
        private GameObject _straightProjectileShell;

        [SerializeField]
        private GameObject _homingProjectileShell;

        [SerializeField]
        private ProjectileVisualEntry[] _projectileVisuals = Array.Empty<ProjectileVisualEntry>();

        public GameObject StraightProjectileShell => _straightProjectileShell;
        public GameObject HomingProjectileShell => _homingProjectileShell;

        [NonSerialized]
        private bool _validationReported;

        private void OnEnable()
        {
            _validationReported = false;
        }

        public bool TryGetEntry(RetroVfxKind kind, out Entry entry)
        {
            ReportValidationOnce();

            Entry match = null;
            if (_entries != null)
            {
                for (int i = 0; i < _entries.Length; i++)
                {
                    Entry candidate = _entries[i];
                    if (candidate != null && candidate.Kind == kind)
                    {
                        if (match != null)
                        {
                            entry = null;
                            return false;
                        }

                        match = candidate;
                    }
                }
            }

            entry = match;
            return match != null;
        }

        public bool TryGetCompanionAttackEntry(string effectId, out CompanionAttackEntry entry)
        {
            ReportValidationOnce();
            entry = null;
            if (string.IsNullOrEmpty(effectId) || _companionAttacks == null)
                return false;

            for (int i = 0; i < _companionAttacks.Length; i++)
            {
                CompanionAttackEntry candidate = _companionAttacks[i];
                if (candidate == null || string.Equals(candidate.EffectId, effectId, StringComparison.Ordinal) == false)
                    continue;

                if (entry != null)
                {
                    entry = null;
                    return false;
                }

                entry = candidate;
            }

            return entry != null;
        }

        public bool TryGetEntry(string slotId, out Entry entry)
        {
            ReportValidationOnce();
            entry = null;
            if (string.IsNullOrEmpty(slotId) || _entries == null)
                return false;

            for (int i = 0; i < _entries.Length; i++)
            {
                Entry candidate = _entries[i];
                if (candidate == null || string.Equals(candidate.SlotId, slotId, StringComparison.Ordinal) == false)
                    continue;

                if (entry != null)
                {
                    entry = null;
                    return false;
                }

                entry = candidate;
            }

            return entry != null;
        }

        public bool TryGetProjectileVisual(string presentationId, out ProjectileVisualEntry entry)
        {
            ReportValidationOnce();
            entry = null;
            if (string.IsNullOrEmpty(presentationId) || _projectileVisuals == null)
                return false;

            for (int i = 0; i < _projectileVisuals.Length; i++)
            {
                ProjectileVisualEntry candidate = _projectileVisuals[i];
                if (candidate == null || string.Equals(candidate.PresentationId, presentationId, StringComparison.Ordinal) == false)
                    continue;

                if (entry != null)
                {
                    entry = null;
                    return false;
                }

                entry = candidate;
            }

            return entry != null;
        }

        private void ReportValidationOnce()
        {
            if (_validationReported)
                return;

            _validationReported = true;
            if (TryValidate(out string issue) == false)
                Debug.LogError($"[{nameof(FeedbackPresentationSet)}] {issue}", this);
        }

        public bool TryValidate(out string issue)
        {
            Array values = Enum.GetValues(typeof(RetroVfxKind));
            int expectedCount = 0;
            for (int i = 0; i < values.Length; i++)
            {
                if (IsAuthorableKind((RetroVfxKind)values.GetValue(i)))
                    expectedCount++;
            }
            int actualCount = _entries == null ? 0 : _entries.Length;
            if (actualCount != expectedCount)
            {
                issue = $"Expected exactly {expectedCount} entries, but found {actualCount}.";
                return false;
            }

            int companionCount = _companionAttacks == null ? 0 : _companionAttacks.Length;
            if (companionCount != CompanionAttackEffectIds.Length)
            {
                issue = $"Expected exactly {CompanionAttackEffectIds.Length} companion attack entries, but found {companionCount}.";
                return false;
            }

            HashSet<string> companionEffectIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _companionAttacks.Length; i++)
            {
                CompanionAttackEntry companionEntry = _companionAttacks[i];
                if (companionEntry == null || string.IsNullOrEmpty(companionEntry.EffectId))
                {
                    issue = $"Companion attack entry {i} is null or has an empty effect ID.";
                    return false;
                }

                if (Array.IndexOf(CompanionAttackEffectIds, companionEntry.EffectId) < 0)
                {
                    issue = $"Companion attack entry {i} has unknown effect ID '{companionEntry.EffectId}'.";
                    return false;
                }

                if (companionEffectIds.Add(companionEntry.EffectId) == false)
                {
                    issue = $"Duplicate companion attack effect ID '{companionEntry.EffectId}'.";
                    return false;
                }

                if (ValidateEventChannels(
                        companionEntry.EffectId,
                        companionEntry.SfxPolicy,
                        companionEntry.Sfx,
                        companionEntry.ActorFeedback,
                        out issue) == false)
                    return false;
            }

            for (int i = 0; i < CompanionAttackEffectIds.Length; i++)
            {
                if (companionEffectIds.Contains(CompanionAttackEffectIds[i]) == false)
                {
                    issue = $"Missing companion attack effect ID '{CompanionAttackEffectIds[i]}'.";
                    return false;
                }
            }

            if (ValidateProjectileVisuals(out issue) == false)
                return false;

            HashSet<RetroVfxKind> seen = new HashSet<RetroVfxKind>();
            for (int i = 0; i < _entries.Length; i++)
            {
                Entry entry = _entries[i];
                if (entry == null)
                {
                    issue = $"Entry {i} is null.";
                    return false;
                }

                if (Enum.IsDefined(typeof(RetroVfxKind), entry.Kind) == false || IsAuthorableKind(entry.Kind) == false)
                {
                    issue = $"Entry {i} has undefined kind value {(int)entry.Kind}.";
                    return false;
                }

                if (seen.Add(entry.Kind) == false)
                {
                    issue = $"Duplicate VFX kind '{entry.Kind}'.";
                    return false;
                }

                string expectedSlotId = GetExpectedSlotId(entry.Kind);
                if (string.Equals(entry.SlotId, expectedSlotId, StringComparison.Ordinal) == false)
                {
                    issue = $"Kind '{entry.Kind}' must keep slot ID '{expectedSlotId}', but found '{entry.SlotId}'.";
                    return false;
                }

                if (ValidateEventChannels(entry.SlotId, entry.SfxPolicy, entry.Sfx, entry.ActorFeedback, out issue) == false)
                    return false;

            }

            for (int i = 0; i < values.Length; i++)
            {
                RetroVfxKind kind = (RetroVfxKind)values.GetValue(i);
                if (IsAuthorableKind(kind) == false)
                    continue;

                if (seen.Contains(kind) == false)
                {
                    issue = $"Missing VFX kind '{kind}'.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private bool ValidateProjectileVisuals(out string issue)
        {
            if (_straightProjectileShell == null || _homingProjectileShell == null)
            {
                issue = "Straight and homing projectile shell prefabs are required.";
                return false;
            }

            int actualCount = _projectileVisuals == null ? 0 : _projectileVisuals.Length;
            if (actualCount != ProjectilePresentationIds.Length)
            {
                issue = $"Expected exactly {ProjectilePresentationIds.Length} projectile visual entries, but found {actualCount}.";
                return false;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < _projectileVisuals.Length; i++)
            {
                ProjectileVisualEntry entry = _projectileVisuals[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.PresentationId))
                {
                    issue = $"Projectile visual entry {i} is null or has an empty presentation ID.";
                    return false;
                }

                if (Array.IndexOf(ProjectilePresentationIds, entry.PresentationId) < 0 || seen.Add(entry.PresentationId) == false)
                {
                    issue = $"Projectile visual entry {i} has an unknown or duplicate presentation ID '{entry.PresentationId}'.";
                    return false;
                }

                Vector3 scale = entry.Scale;
                if (scale.x <= 0.0f || scale.y <= 0.0f || scale.z <= 0.0f)
                {
                    issue = $"Projectile visual '{entry.PresentationId}' must use positive scale values.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private static bool ValidateEventChannels(
            string id,
            EventSfxPolicy policy,
            AudioClip sfx,
            ActorFeedbackProfile actorFeedback,
            out string issue)
        {
            if (Enum.IsDefined(typeof(EventSfxPolicy), policy) == false
                || Enum.IsDefined(typeof(ActorFeedbackProfile), actorFeedback) == false)
            {
                issue = $"Event cue '{id}' has an undefined channel policy.";
                return false;
            }

            if (policy == EventSfxPolicy.Required && sfx == null)
            {
                issue = $"Event cue '{id}' requires SFX but has no clip.";
                return false;
            }

            if (policy == EventSfxPolicy.None && sfx != null)
            {
                issue = $"Event cue '{id}' uses SFX policy None but still has a clip.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        public static string GetExpectedSlotId(RetroVfxKind kind)
        {
            return kind switch
            {
                RetroVfxKind.HealingReceived => "healing_received",
                RetroVfxKind.BuffApplied => "buff_applied",
                RetroVfxKind.PlayerDamaged => "player_damaged",
                RetroVfxKind.ShieldOrcCrack => "shield_orc_crack",
                RetroVfxKind.RedChargerCharge => "red_charger_charge",
                RetroVfxKind.BossAoeImpact => "boss_aoe_impact",
                RetroVfxKind.GuardSquadActivate => "guard_squad_activate",
                RetroVfxKind.GuardShockwave => "guard_shockwave",
                RetroVfxKind.GuardRadialShield => "guard_radial_shield",
                RetroVfxKind.LevelUp => "level_up",
                RetroVfxKind.CardSelect => "card_select",
                RetroVfxKind.ResultClear => "result_clear",
                RetroVfxKind.XpAbsorb => "exp_absorb",
                RetroVfxKind.CompanionRecruit => "companion_recruit",
                RetroVfxKind.CompanionPromotion => "companion_promotion",
                RetroVfxKind.PromotionShoutActivate => "promotion_shout_activate",
                RetroVfxKind.SynergyReady => "synergy_ready",
                RetroVfxKind.SynergyComplete => "synergy_complete",
                RetroVfxKind.RapidCrossbowCast => "rapid_crossbow_cast",
                RetroVfxKind.PiercingSpearCast => "piercing_spear_cast",
                RetroVfxKind.BlastStaffCast => "blast_staff_cast",
                RetroVfxKind.BlastStaffExplosion => "blast_staff_explosion",
                RetroVfxKind.BossSpawn => "boss_spawn",
                _ => string.Empty,
            };
        }

        public static bool IsAuthorableKind(RetroVfxKind kind)
        {
            return kind != RetroVfxKind.None;
        }

#if UNITY_EDITOR
        public void SetEntriesForEditor(Entry[] entries)
        {
            _entries = entries ?? Array.Empty<Entry>();
            _validationReported = false;
        }

        public void SetProjectileVisualsForEditor(
            GameObject straightShell,
            GameObject homingShell,
            ProjectileVisualEntry[] entries)
        {
            _straightProjectileShell = straightShell;
            _homingProjectileShell = homingShell;
            _projectileVisuals = entries ?? Array.Empty<ProjectileVisualEntry>();
            _validationReported = false;
        }
#endif
    }
}
