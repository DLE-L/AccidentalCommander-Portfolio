using UnityEngine;

namespace Lizzo.PV.P0.Cards
{
    public sealed class CardCatalog : ScriptableObject
    {
        [SerializeField]
        private CardDefinitionSet _definitions;

        [SerializeField]
        private CardPoolDefinition _pool;

        public CardDefinitionSet Definitions => _definitions;
        public CardPoolDefinition Pool => _pool;

#if UNITY_EDITOR
        public void SetForEditor(CardDefinitionSet definitions, CardPoolDefinition pool)
        {
            _definitions = definitions;
            _pool = pool;
        }
#endif
    }
}
