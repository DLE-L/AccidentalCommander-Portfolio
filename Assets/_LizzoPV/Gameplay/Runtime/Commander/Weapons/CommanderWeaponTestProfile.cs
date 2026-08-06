using System;
using System.Collections.Generic;
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

        public void ValidateOrThrow()
        {
            if (_weapons == null)
                throw new InvalidOperationException("Commander weapon TEST profile entries are missing.");

            var seen = new HashSet<CommanderWeaponId>();
            for (int i = 0; i < _weapons.Length; i++)
            {
                WeaponTestValues values = _weapons[i];
                if (CommanderWeaponCatalog.IsSelectable(values.WeaponId) == false)
                    throw new InvalidOperationException($"Commander weapon TEST profile contains invalid weapon '{values.WeaponId}'.");
                if (seen.Add(values.WeaponId) == false)
                    throw new InvalidOperationException($"Commander weapon TEST profile contains duplicate entry '{values.WeaponId}'.");
                if (values.AttackInterval <= 0.0f || values.DamageCoefficient < 0.0f ||
                    values.Range < 0.0f || values.MaxTargets < 1 ||
                    values.AttackCollisionSize < 0.0f || values.ExplosionRadius < 0.0f)
                    throw new InvalidOperationException($"Commander weapon TEST profile contains invalid values for '{values.WeaponId}'.");
            }

            foreach (CommanderWeaponId weaponId in new[]
                     {
                         CommanderWeaponId.RapidCrossbow,
                         CommanderWeaponId.PiercingSpear,
                         CommanderWeaponId.BlastStaff,
                     })
            {
                if (seen.Contains(weaponId) == false)
                    throw new InvalidOperationException($"Commander weapon TEST profile is missing '{weaponId}'.");
            }
        }
    }
}
