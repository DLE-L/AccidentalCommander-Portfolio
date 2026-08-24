using System;
using Lizzo.PV.Flow;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Commander.Weapons
{
    [CreateAssetMenu(
        fileName = "CommanderWeaponTestProfile",
        menuName = "Lizzo/Gameplay/Commander Weapon Test Profile")]
    public sealed class CommanderWeaponTestProfile : ScriptableObject
    {
        [Serializable]
        public struct WeaponTestValues
        {
            [SerializeField] private CommanderWeaponId _weaponId;
            [SerializeField, Min(0.01f)] private float _attackInterval;
            [SerializeField, Min(0.0f)] private float _damageCoefficient;
            [SerializeField, Min(0.0f)] private float _range;
            [SerializeField, Min(1)] private int _maxTargets;
            [SerializeField, Min(0.0f)] private float _attackCollisionSize;
            [SerializeField, Min(0.0f)] private float _explosionRadius;

            public CommanderWeaponId WeaponId => _weaponId;
            public float AttackInterval => _attackInterval;
            public float DamageCoefficient => _damageCoefficient;
            public float Range => _range;
            public int MaxTargets => _maxTargets;
            public float AttackCollisionSize => _attackCollisionSize;
            public float ExplosionRadius => _explosionRadius;
        }

        [SerializeField] private WeaponTestValues[] _weapons = Array.Empty<WeaponTestValues>();

        public bool TryGet(CommanderWeaponId weaponId, out WeaponTestValues values)
        {
            values = default;
            if (CommanderWeaponCatalog.IsSelectable(weaponId) == false || _weapons == null)
                return false;

            bool found = false;
            for (int i = 0; i < _weapons.Length; i++)
            {
                if (_weapons[i].WeaponId != weaponId)
                    continue;

                if (found)
                    throw new InvalidOperationException($"Commander weapon TEST profile contains duplicate entry '{weaponId}'.");

                values = _weapons[i];
                found = true;
            }

            return found;
        }
    }
}
