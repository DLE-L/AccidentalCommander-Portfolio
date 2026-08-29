using System;
using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    public sealed class CardCatalog : ScriptableObject
    {
        [SerializeField]
        private CardDefinitionSet _definitions;

        [SerializeField]
        private CardPoolDefinition _pool;

        [SerializeField]
        private CardPoolDefinition[] _additionalPools = Array.Empty<CardPoolDefinition>();

        public CardDefinitionSet Definitions => _definitions;
        public CardPoolDefinition Pool => _pool;

        public bool TryGetPool(string profileId, out CardPoolDefinition pool)
        {
            if (MatchesProfile(_pool, profileId))
            {
                pool = _pool;
                return true;
            }

            if (_additionalPools != null)
            {
                for (int index = 0; index < _additionalPools.Length; index++)
                {
                    CardPoolDefinition candidate = _additionalPools[index];
                    if (MatchesProfile(candidate, profileId))
                    {
                        pool = candidate;
                        return true;
                    }
                }
            }

            pool = null;
            return false;
        }

        private static bool MatchesProfile(CardPoolDefinition pool, string profileId)
        {
            return pool != null
                && !string.IsNullOrWhiteSpace(profileId)
                && string.Equals(pool.ProfileId, profileId.Trim(), StringComparison.Ordinal);
        }

#if UNITY_EDITOR
        public void SetForEditor(CardDefinitionSet definitions, CardPoolDefinition pool)
        {
            _definitions = definitions;
            _pool = pool;
            _additionalPools = Array.Empty<CardPoolDefinition>();
        }

        public void SetForEditor(
            CardDefinitionSet definitions,
            CardPoolDefinition pool,
            CardPoolDefinition[] additionalPools)
        {
            _definitions = definitions;
            _pool = pool;
            _additionalPools = additionalPools ?? Array.Empty<CardPoolDefinition>();
        }
#endif
    }
}
