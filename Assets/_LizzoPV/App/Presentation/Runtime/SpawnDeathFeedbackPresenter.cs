using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lizzo.PV.Presentation
{
    public readonly struct EnemySpawnPresentation
    {
        public EnemySpawnPresentation(EnemyId enemyId, Vector3 position, int enemyInstanceId)
        {
            EnemyId = enemyId;
            Position = position;
            EnemyInstanceId = enemyInstanceId;
        }

        public EnemyId EnemyId { get; }
        public Vector3 Position { get; }
        public int EnemyInstanceId { get; }
    }

    public readonly struct EnemyDeathPresentation
    {
        public EnemyDeathPresentation(
            EnemyId enemyId,
            Vector3 position,
            int enemyInstanceId,
            bool isBoss,
            bool isElite)
        {
            EnemyId = enemyId;
            Position = position;
            EnemyInstanceId = enemyInstanceId;
            IsBoss = isBoss;
            IsElite = isElite;
        }

        public EnemyId EnemyId { get; }
        public Vector3 Position { get; }
        public int EnemyInstanceId { get; }
        public bool IsBoss { get; }
        public bool IsElite { get; }
    }

    public interface ISpawnDeathFeedbackSink
    {
        void PresentSpawn(in EnemySpawnPresentation presentation, EnemySpawnFeedbackProfileSO profile);
        void PresentDeath(in EnemyDeathPresentation presentation, EnemyDeathFeedbackProfileSO profile);
    }

    public sealed class SpawnDeathFeedbackPresenter
    {
        private readonly Dictionary<string, EnemySpawnFeedbackProfileSO> _spawnProfiles;
        private readonly Dictionary<string, EnemyDeathFeedbackProfileSO> _deathProfiles;
        private readonly ISpawnDeathFeedbackSink _sink;

        public SpawnDeathFeedbackPresenter(
            IReadOnlyList<EnemySpawnFeedbackBinding> spawnBindings,
            IReadOnlyList<EnemyDeathFeedbackBinding> deathBindings,
            ISpawnDeathFeedbackSink sink)
        {
            _spawnProfiles = BuildSpawnProfiles(spawnBindings);
            _deathProfiles = BuildDeathProfiles(deathBindings);
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public bool TryPresentSpawn(in EnemySpawnPresentation presentation)
        {
            if (presentation.EnemyId.IsNone
                || !_spawnProfiles.TryGetValue(presentation.EnemyId.Value, out EnemySpawnFeedbackProfileSO profile))
            {
                return false;
            }

            _sink.PresentSpawn(in presentation, profile);
            return true;
        }

        public bool TryPresentDeath(in EnemyDeathPresentation presentation)
        {
            if (presentation.EnemyId.IsNone
                || !_deathProfiles.TryGetValue(presentation.EnemyId.Value, out EnemyDeathFeedbackProfileSO profile))
            {
                return false;
            }

            _sink.PresentDeath(in presentation, profile);
            return true;
        }

        private static Dictionary<string, EnemySpawnFeedbackProfileSO> BuildSpawnProfiles(
            IReadOnlyList<EnemySpawnFeedbackBinding> bindings)
        {
            var profiles = new Dictionary<string, EnemySpawnFeedbackProfileSO>(StringComparer.Ordinal);
            if (bindings == null)
                return profiles;

            for (int i = 0; i < bindings.Count; i++)
            {
                EnemySpawnFeedbackBinding binding = bindings[i];
                if (!binding.EnemyId.IsNone && binding.Profile != null)
                    profiles.TryAdd(binding.EnemyId.Value, binding.Profile);
            }

            return profiles;
        }

        private static Dictionary<string, EnemyDeathFeedbackProfileSO> BuildDeathProfiles(
            IReadOnlyList<EnemyDeathFeedbackBinding> bindings)
        {
            var profiles = new Dictionary<string, EnemyDeathFeedbackProfileSO>(StringComparer.Ordinal);
            if (bindings == null)
                return profiles;

            for (int i = 0; i < bindings.Count; i++)
            {
                EnemyDeathFeedbackBinding binding = bindings[i];
                if (!binding.EnemyId.IsNone && binding.Profile != null)
                    profiles.TryAdd(binding.EnemyId.Value, binding.Profile);
            }

            return profiles;
        }
    }
}
