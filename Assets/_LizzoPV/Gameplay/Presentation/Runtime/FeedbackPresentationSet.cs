using System;
using System.Collections.Generic;
using Lizzo.PV.Legion;
using UnityEngine;

namespace Lizzo.PV.P0.Presentation
{
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
                bool hasRewardCue)
            {
                _kind = kind;
                _slotId = slotId;
                _prefab = prefab;
                _sfx = sfx;
                _lifetime = lifetime;
                _scale = scale;
                _minScale = minScale;
                _maxScale = maxScale;
                _isScaleException = isScaleException;
                _forwardOffset = forwardOffset;
                _upOffset = upOffset;
                _alignToDirection = alignToDirection;
                _angleOffset = angleOffset;
                _sfxVolumeScale = sfxVolumeScale;
                _isHitFeedback = isHitFeedback;
                _hasRewardCue = hasRewardCue;
            }

            public RetroVfxKind Kind => _kind;
            public string SlotId => _slotId;
            public GameObject Prefab => _prefab;
            public AudioClip Sfx => _sfx;
            public float Lifetime => _lifetime;
            public float Scale => _scale;
            public float MinScale => _minScale;
            public float MaxScale => _maxScale;
            public bool IsScaleException => _isScaleException;
            public float ForwardOffset => _forwardOffset;
            public float UpOffset => _upOffset;
            public bool AlignToDirection => _alignToDirection;
            public float AngleOffset => _angleOffset;
            public float SfxVolumeScale => _sfxVolumeScale;
            public bool IsHitFeedback => _isHitFeedback;
            public bool HasRewardCue => _hasRewardCue;
        }

        [SerializeField]
        private Entry[] _entries = Array.Empty<Entry>();

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
            int expectedCount = values.Length;
            int actualCount = _entries == null ? 0 : _entries.Length;
            if (actualCount != expectedCount)
            {
                issue = $"Expected exactly {expectedCount} entries, but found {actualCount}.";
                return false;
            }

            HashSet<RetroVfxKind> seen = new HashSet<RetroVfxKind>();
            for (int i = 0; i < _entries.Length; i++)
            {
                Entry entry = _entries[i];
                if (entry == null)
                {
                    issue = $"Entry {i} is null.";
                    return false;
                }

                if (Enum.IsDefined(typeof(RetroVfxKind), entry.Kind) == false)
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

                if (entry.Prefab == null)
                {
                    issue = $"Slot '{entry.SlotId}' ({entry.Kind}) has no prefab assigned.";
                    return false;
                }
            }

            for (int i = 0; i < values.Length; i++)
            {
                RetroVfxKind kind = (RetroVfxKind)values.GetValue(i);
                if (seen.Contains(kind) == false)
                {
                    issue = $"Missing VFX kind '{kind}'.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        public static string GetExpectedSlotId(RetroVfxKind kind)
        {
            return kind switch
            {
                RetroVfxKind.CommanderMuzzle => "commander_attack_start",
                RetroVfxKind.ProjectileHit => "commander_projectile_hit",
                RetroVfxKind.SingleHit => "ally_hit",
                RetroVfxKind.AreaHit => "area_hit",
                RetroVfxKind.HealPulse => "cleric_heal",
                RetroVfxKind.BuffPulse => "guard_protect_aura",
                RetroVfxKind.ShieldPush => "shield_push",
                RetroVfxKind.ForwardSlash => "swordsman_slash",
                RetroVfxKind.EnemyContactHit => "normal_enemy_hit",
                RetroVfxKind.EnemyDeath => "normal_enemy_death",
                RetroVfxKind.ShieldOrcHit => "shield_orc_hit",
                RetroVfxKind.ShieldOrcCrack => "shield_orc_crack",
                RetroVfxKind.ShieldOrcDeath => "shield_orc_death",
                RetroVfxKind.RedChargerWarning => "red_charger_warning",
                RetroVfxKind.RedChargerCharge => "red_charger_charge",
                RetroVfxKind.RedChargerDeath => "red_charger_death",
                RetroVfxKind.BossWarning => "boss_warning",
                RetroVfxKind.BossAttackHit => "boss_attack_hit",
                RetroVfxKind.BossDeath => "boss_death",
                RetroVfxKind.SynergyActivate => "guard_squad_complete",
                RetroVfxKind.GuardShockwaveHit => "guard_shield_push_hit",
                RetroVfxKind.GuardRadialShield => "guard_radial_shield",
                RetroVfxKind.ArcherHit => "archer_hit",
                RetroVfxKind.LevelUp => "level_up",
                RetroVfxKind.CardSelect => "card_select",
                RetroVfxKind.ResultClear => "result_clear",
                RetroVfxKind.XpAbsorb => "exp_absorb",
                _ => string.Empty,
            };
        }

#if UNITY_EDITOR
        public void SetEntriesForEditor(Entry[] entries)
        {
            _entries = entries ?? Array.Empty<Entry>();
            _validationReported = false;
        }
#endif
    }
}
