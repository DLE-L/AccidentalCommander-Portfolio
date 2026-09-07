namespace Lizzo.PV.Gameplay.Combat
{
    public static class CombatIds
    {
        public const string Unknown = "unknown";

        public const string Commander = "commander";
        public const string Projectile = "projectile";
        public const string ContactAttack = "contact_attack";
        public const string RedChargerDash = "red_charger_dash";
        public const string HungryWolfDash = "hungry_wolf_dash";
        public const string RedChargerImpactGrace = "red_charger_impact_grace";
        public const string BossSlowCharge = "boss_slow_charge";
        public const string BossAoeSlam = "boss_aoe_slam";

        public const string SmallGoblin = "small_goblin";
        public const string HungryWolf = "hungry_wolf";
        public const string ShieldOrc = "shield_orc";
        public const string EliteRedCharger = "elite_red_charger";
        public const string BossHungryGiant = "boss_hungry_giant";

        public static string Normalize(string id)
        {
            return string.IsNullOrEmpty(id) ? Unknown : id;
        }

        public static string EnemyPatternSource(string enemyId, string patternId)
        {
            enemyId = Normalize(enemyId);
            patternId = Normalize(patternId);
            return patternId == ContactAttack ? enemyId : $"{enemyId}:{patternId}";
        }

        public static string DamageCooldownKey(string enemyId, int instanceId, string patternId)
        {
            return $"{Normalize(enemyId)}:{instanceId}:{Normalize(patternId)}";
        }

        public static bool IsBossPattern(string patternId)
        {
            return string.IsNullOrEmpty(patternId) == false && patternId.StartsWith("boss_");
        }
    }
}
