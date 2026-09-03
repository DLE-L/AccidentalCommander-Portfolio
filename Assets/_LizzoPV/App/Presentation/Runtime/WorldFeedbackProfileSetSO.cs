using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    [CreateAssetMenu(menuName = "Lizzo PV/Presentation/World/World Feedback Profile Set", fileName = "WorldFeedbackProfileSet")]
    public sealed class WorldFeedbackProfileSetSO : ScriptableObject
    {
        [SerializeField] private CommanderWorldFeedbackProfileSO _commanderProfile;
        [SerializeField] private List<CompanionLifecycleFeedbackBinding> _companionLifecycleBindings = new List<CompanionLifecycleFeedbackBinding>();
        [SerializeField] private WorldUiFeedbackProfileSO _worldUiProfile;
        [SerializeField] private List<CombatImpactFeedbackBinding> _combatImpactBindings = new List<CombatImpactFeedbackBinding>();
        [SerializeField] private List<StatusFeedbackBinding> _statusBindings = new List<StatusFeedbackBinding>();
        [SerializeField] private List<AttackFeedbackBinding> _attackBindings = new List<AttackFeedbackBinding>();
        [SerializeField] private List<EnemyAttackFeedbackBinding> _enemyAttackBindings = new List<EnemyAttackFeedbackBinding>();
        [SerializeField] private List<EnemySpawnFeedbackBinding> _enemySpawnBindings = new List<EnemySpawnFeedbackBinding>();
        [SerializeField] private List<EnemyDeathFeedbackBinding> _enemyDeathBindings = new List<EnemyDeathFeedbackBinding>();
        [SerializeField] private List<ExperienceOrbFeedbackBinding> _experienceOrbBindings = new List<ExperienceOrbFeedbackBinding>();
        [SerializeField] private RunOutcomeWorldFeedbackProfileSO _runOutcomeProfile;

        public CommanderWorldFeedbackProfileSO CommanderProfile => _commanderProfile;
        public IReadOnlyList<CompanionLifecycleFeedbackBinding> CompanionLifecycleBindings => _companionLifecycleBindings;
        public WorldUiFeedbackProfileSO WorldUiProfile => _worldUiProfile;
        public IReadOnlyList<CombatImpactFeedbackBinding> CombatImpactBindings => _combatImpactBindings;
        public IReadOnlyList<StatusFeedbackBinding> StatusBindings => _statusBindings;
        public IReadOnlyList<AttackFeedbackBinding> AttackBindings => _attackBindings;
        public IReadOnlyList<EnemyAttackFeedbackBinding> EnemyAttackBindings => _enemyAttackBindings;
        public IReadOnlyList<EnemySpawnFeedbackBinding> EnemySpawnBindings => _enemySpawnBindings;
        public IReadOnlyList<EnemyDeathFeedbackBinding> EnemyDeathBindings => _enemyDeathBindings;
        public IReadOnlyList<ExperienceOrbFeedbackBinding> ExperienceOrbBindings => _experienceOrbBindings;
        public RunOutcomeWorldFeedbackProfileSO RunOutcomeProfile => _runOutcomeProfile;

        public bool TryValidate(out string issue)
        {
            if (_commanderProfile == null || _worldUiProfile == null || _runOutcomeProfile == null)
            {
                issue = "World feedback profile set requires Commander, World UI, and Run Outcome profiles.";
                return false;
            }

            return _commanderProfile.TryValidate(out issue)
                   && _worldUiProfile.TryValidate(out issue)
                   && _runOutcomeProfile.TryValidate(out issue)
                   && ValidateCompanionBindings(out issue)
                   && ValidateCombatImpactBindings(out issue)
                   && ValidateStatusBindings(out issue)
                   && ValidateAttackBindings(out issue)
                   && ValidateEnemyAttackBindings(out issue)
                   && ValidateEnemySpawnBindings(out issue)
                   && ValidateEnemyDeathBindings(out issue)
                   && ValidateExperienceBindings(out issue);
        }

        private bool ValidateCompanionBindings(out string issue)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (!RequireBindings(_companionLifecycleBindings, nameof(CompanionLifecycleBindings), out issue))
            {
                return false;
            }

            for (int i = 0; i < _companionLifecycleBindings.Count; i++)
            {
                CompanionLifecycleFeedbackBinding binding = _companionLifecycleBindings[i];
                if (binding.CompanionId.IsNone || binding.Profile == null)
                {
                    issue = "Companion lifecycle binding requires a CompanionId and Profile.";
                    return false;
                }

                if (!keys.Add(binding.CompanionId.Value))
                {
                    issue = $"Duplicate CompanionId {binding.CompanionId.Value}.";
                    return false;
                }

                if (!binding.Profile.TryValidate(out issue))
                {
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private bool ValidateCombatImpactBindings(out string issue)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (!RequireBindings(_combatImpactBindings, nameof(CombatImpactBindings), out issue))
            {
                return false;
            }

            for (int i = 0; i < _combatImpactBindings.Count; i++)
            {
                CombatImpactFeedbackBinding binding = _combatImpactBindings[i];
                if (binding.ImpactKind.IsNone || binding.Profile == null)
                {
                    issue = "Combat impact binding requires a CombatImpactKind and Profile.";
                    return false;
                }

                if (!keys.Add(binding.ImpactKind.Value))
                {
                    issue = $"Duplicate CombatImpactKind {binding.ImpactKind.Value}.";
                    return false;
                }

                if (!binding.Profile.TryValidate(out issue))
                {
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private bool ValidateStatusBindings(out string issue)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (!RequireBindings(_statusBindings, nameof(StatusBindings), out issue))
            {
                return false;
            }

            for (int i = 0; i < _statusBindings.Count; i++)
            {
                StatusFeedbackBinding binding = _statusBindings[i];
                if (binding.StatusId.IsNone || binding.Profile == null)
                {
                    issue = "Status binding requires a StatusId and Profile.";
                    return false;
                }

                if (!keys.Add(binding.StatusId.Value))
                {
                    issue = $"Duplicate StatusId {binding.StatusId.Value}.";
                    return false;
                }

                if (!binding.Profile.TryValidate(out issue))
                {
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private bool ValidateAttackBindings(out string issue)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (!RequireBindings(_attackBindings, nameof(AttackBindings), out issue))
            {
                return false;
            }

            for (int i = 0; i < _attackBindings.Count; i++)
            {
                AttackFeedbackBinding binding = _attackBindings[i];
                if (binding.AttackId.IsNone || binding.Profile == null)
                {
                    issue = "Attack binding requires an AttackId and Profile.";
                    return false;
                }

                if (!keys.Add(binding.AttackId.Value))
                {
                    issue = $"Duplicate AttackId {binding.AttackId.Value}.";
                    return false;
                }

                if (!binding.Profile.TryValidate(out issue))
                {
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private bool ValidateEnemyAttackBindings(out string issue)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (!RequireBindings(_enemyAttackBindings, nameof(EnemyAttackBindings), out issue))
            {
                return false;
            }

            for (int i = 0; i < _enemyAttackBindings.Count; i++)
            {
                EnemyAttackFeedbackBinding binding = _enemyAttackBindings[i];
                if (binding.EnemyAttackId.IsNone || binding.Profile == null)
                {
                    issue = "Enemy attack binding requires an EnemyAttackId and Profile.";
                    return false;
                }

                if (!keys.Add(binding.EnemyAttackId.Value))
                {
                    issue = $"Duplicate EnemyAttackId {binding.EnemyAttackId.Value}.";
                    return false;
                }

                if (!binding.Profile.TryValidate(out issue))
                {
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private bool ValidateEnemySpawnBindings(out string issue)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (!RequireBindings(_enemySpawnBindings, nameof(EnemySpawnBindings), out issue))
            {
                return false;
            }

            for (int i = 0; i < _enemySpawnBindings.Count; i++)
            {
                EnemySpawnFeedbackBinding binding = _enemySpawnBindings[i];
                if (binding.EnemyId.IsNone || binding.Profile == null)
                {
                    issue = "Enemy spawn binding requires an EnemyId and Profile.";
                    return false;
                }

                if (!keys.Add(binding.EnemyId.Value))
                {
                    issue = $"Duplicate Enemy Spawn EnemyId {binding.EnemyId.Value}.";
                    return false;
                }

                if (!binding.Profile.TryValidate(out issue))
                {
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private bool ValidateEnemyDeathBindings(out string issue)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            if (!RequireBindings(_enemyDeathBindings, nameof(EnemyDeathBindings), out issue))
            {
                return false;
            }

            for (int i = 0; i < _enemyDeathBindings.Count; i++)
            {
                EnemyDeathFeedbackBinding binding = _enemyDeathBindings[i];
                if (binding.EnemyId.IsNone || binding.Profile == null)
                {
                    issue = "Enemy death binding requires an EnemyId and Profile.";
                    return false;
                }

                if (!keys.Add(binding.EnemyId.Value))
                {
                    issue = $"Duplicate Enemy Death EnemyId {binding.EnemyId.Value}.";
                    return false;
                }

                if (!binding.Profile.TryValidate(out issue))
                {
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private bool ValidateExperienceBindings(out string issue)
        {
            if (!RequireBindings(_experienceOrbBindings, nameof(ExperienceOrbBindings), out issue))
            {
                return false;
            }

            bool hasSmall = false;
            bool hasMedium = false;
            bool hasLarge = false;
            for (int i = 0; i < _experienceOrbBindings.Count; i++)
            {
                ExperienceOrbFeedbackBinding binding = _experienceOrbBindings[i];
                if (binding.Profile == null)
                {
                    issue = "Experience Orb binding requires a Profile.";
                    return false;
                }

                bool duplicate;
                switch (binding.OrbVisualTier)
                {
                    case OrbVisualTier.Small:
                        duplicate = hasSmall;
                        hasSmall = true;
                        break;
                    case OrbVisualTier.Medium:
                        duplicate = hasMedium;
                        hasMedium = true;
                        break;
                    case OrbVisualTier.Large:
                        duplicate = hasLarge;
                        hasLarge = true;
                        break;
                    default:
                        issue = $"Unsupported OrbVisualTier {binding.OrbVisualTier}.";
                        return false;
                }

                if (duplicate)
                {
                    issue = $"Duplicate OrbVisualTier {binding.OrbVisualTier}.";
                    return false;
                }

                if (!binding.Profile.TryValidate(out issue))
                {
                    return false;
                }
            }

            if (!hasSmall || !hasMedium || !hasLarge)
            {
                issue = "Experience Orb bindings require Small, Medium, and Large tiers.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static bool RequireBindings<T>(List<T> bindings, string fieldName, out string issue)
        {
            if (bindings == null || bindings.Count == 0)
            {
                issue = $"{fieldName} requires at least one binding.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

#if UNITY_EDITOR
        public void SetForEditor(
            CommanderWorldFeedbackProfileSO commanderProfile,
            IEnumerable<CompanionLifecycleFeedbackBinding> companionLifecycleBindings,
            WorldUiFeedbackProfileSO worldUiProfile,
            IEnumerable<CombatImpactFeedbackBinding> combatImpactBindings,
            IEnumerable<StatusFeedbackBinding> statusBindings,
            IEnumerable<AttackFeedbackBinding> attackBindings,
            IEnumerable<EnemyAttackFeedbackBinding> enemyAttackBindings,
            IEnumerable<EnemySpawnFeedbackBinding> enemySpawnBindings,
            IEnumerable<EnemyDeathFeedbackBinding> enemyDeathBindings,
            IEnumerable<ExperienceOrbFeedbackBinding> experienceOrbBindings,
            RunOutcomeWorldFeedbackProfileSO runOutcomeProfile)
        {
            _commanderProfile = commanderProfile;
            _companionLifecycleBindings = Copy(companionLifecycleBindings);
            _worldUiProfile = worldUiProfile;
            _combatImpactBindings = Copy(combatImpactBindings);
            _statusBindings = Copy(statusBindings);
            _attackBindings = Copy(attackBindings);
            _enemyAttackBindings = Copy(enemyAttackBindings);
            _enemySpawnBindings = Copy(enemySpawnBindings);
            _enemyDeathBindings = Copy(enemyDeathBindings);
            _experienceOrbBindings = Copy(experienceOrbBindings);
            _runOutcomeProfile = runOutcomeProfile;
        }

        private static List<T> Copy<T>(IEnumerable<T> source)
        {
            return source == null ? new List<T>() : new List<T>(source);
        }
#endif
    }
}
