using System;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Units
{
    // Authored selection and priority only. Definitions and execution remain owned by attack modules.
    [DisallowMultipleComponent]
    public sealed class EnemyAttackLoadout : MonoBehaviour
    {
        [SerializeField, Tooltip("Priority order. Applied when the unit is initialized. Each supported attack appears at most once.")]
        private EnemyAttackKind[] _attacks = Array.Empty<EnemyAttackKind>();

        // Red/Wolf contact keeps its existing immediate/preemptive lifecycle outside the runner.
        public EnemyAttackDefinition[] Compose(EnemyAttackDefinition[] available, bool externalContact = false)
        {
            if (available == null) throw new ArgumentNullException(nameof(available));
            if (_attacks == null || _attacks.Length == 0)
                throw new InvalidOperationException("Enemy attack loadout requires at least one authored attack.");
            var result = new EnemyAttackDefinition[_attacks.Length];
            int count = 0;
            for (int i = 0; i < _attacks.Length; i++)
            {
                for (int j = 0; j < i; j++)
                    if (_attacks[j] == _attacks[i])
                        throw new InvalidOperationException("Duplicate enemy attack in loadout: " + _attacks[i]);
                if (externalContact && _attacks[i] == EnemyAttackKind.Contact) continue;
                int match = -1;
                for (int j = 0; j < available.Length; j++)
                {
                    if (available[j].Kind != _attacks[i]) continue;
                    if (match >= 0) throw new InvalidOperationException("Duplicate available enemy attack: " + _attacks[i]);
                    match = j;
                }
                if (match < 0) throw new InvalidOperationException("Unsupported enemy attack in loadout: " + _attacks[i]);
                result[count++] = available[match];
            }
            if (count != result.Length) Array.Resize(ref result, count);
            return result;
        }

        public bool Contains(EnemyAttackKind kind) => Array.IndexOf(_attacks, kind) >= 0;

#if UNITY_EDITOR
        public EnemyAttackKind[] CopyEditorAttacks() => (EnemyAttackKind[])_attacks.Clone();
        public void SetEditorAttacks(params EnemyAttackKind[] attacks)
            => _attacks = attacks == null ? Array.Empty<EnemyAttackKind>() : (EnemyAttackKind[])attacks.Clone();
#endif
    }
}
