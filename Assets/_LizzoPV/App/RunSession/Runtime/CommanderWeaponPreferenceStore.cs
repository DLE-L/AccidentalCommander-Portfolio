using System;
using UnityEngine;

namespace Lizzo.PV.Flow
{
    public static class CommanderWeaponPreferenceStore
    {
        const string PreferenceKey = "lizzo_pv.commander_weapon.v1";

        public static bool TryLoad(out CommanderWeaponId weapon)
        {
            string weaponId = PlayerPrefs.GetString(PreferenceKey, string.Empty);
            weapon = weaponId switch
            {
                "rapid_crossbow" => CommanderWeaponId.RapidCrossbow,
                "piercing_spear" => CommanderWeaponId.PiercingSpear,
                "blast_staff" => CommanderWeaponId.BlastStaff,
                _ => CommanderWeaponId.None,
            };

            return CommanderWeaponCatalog.IsSelectable(weapon);
        }

        public static void Save(CommanderWeaponId weapon)
        {
            if (CommanderWeaponCatalog.IsSelectable(weapon) == false)
                throw new ArgumentOutOfRangeException(nameof(weapon), weapon, "A selectable commander weapon is required.");

            PlayerPrefs.SetString(PreferenceKey, CommanderWeaponCatalog.ToId(weapon));
            PlayerPrefs.Save();
        }
    }
}
