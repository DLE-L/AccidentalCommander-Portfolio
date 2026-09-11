using System;
using System.Collections.Generic;
using System.Globalization;
using Lizzo.PV.Data;

namespace Lizzo.PV.Gameplay.Run
{
    internal readonly struct CompanionSynergyDefinitionSets
    {
        internal CompanionSynergyDefinitionSets(
            PairSynergyDefinitionSet pairs,
            TrioSynergyDefinitionSet trios,
            CompanionSynergyTriggerBalance triggerBalance)
        {
            Pairs = pairs;
            Trios = trios;
            TriggerBalance = triggerBalance;
        }

        internal PairSynergyDefinitionSet Pairs { get; }
        internal TrioSynergyDefinitionSet Trios { get; }
        internal CompanionSynergyTriggerBalance TriggerBalance { get; }
    }

    internal readonly struct CompanionSynergyTriggerBalance
    {
        internal CompanionSynergyTriggerBalance(
            float periodicTriggerSeconds,
            int counterThreshold,
            float periodicTargetRadius)
        {
            PeriodicTriggerSeconds = periodicTriggerSeconds;
            CounterThreshold = counterThreshold;
            PeriodicTargetRadius = periodicTargetRadius;
        }

        internal float PeriodicTriggerSeconds { get; }
        internal int CounterThreshold { get; }
        internal float PeriodicTargetRadius { get; }
    }

    internal static class CompanionSynergyDefinitionFactory
    {
        internal static CompanionSynergyDefinitionSets Create(IDataProvider data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            SynergyBalanceParameters pairFirst = Load(data, SynergyBalanceProfileIds.PairFirst);
            SynergyBalanceParameters pairSecond = Load(data, SynergyBalanceProfileIds.PairSecond);
            SynergyBalanceParameters trioFirst = Load(data, SynergyBalanceProfileIds.TrioFirst);
            SynergyBalanceParameters trioSecond = Load(data, SynergyBalanceProfileIds.TrioSecond);
            SynergyBalanceParameters trigger = Load(data, SynergyBalanceProfileIds.Trigger);

            PairSynergyDefinitionSet pairs = PairSynergyDefinitionSet.Combine(
                PairSynergyCatalog.CreateFirstSet(CreatePairFirst(pairFirst)),
                PairSynergyCatalog.CreateSecondSet(CreatePairSecond(pairSecond)));
            TrioSynergyDefinitionSet trios = TrioSynergyDefinitionSet.Combine(
                TrioSynergyCatalog.CreateFirstSet(CreateTrioFirst(trioFirst)),
                TrioSynergyCatalog.CreateSecondSet(CreateTrioSecond(trioSecond)));
            return new CompanionSynergyDefinitionSets(
                pairs,
                trios,
                new CompanionSynergyTriggerBalance(
                    trigger.Cooldown,
                    trigger.Int("counterThreshold"),
                    trigger.Float("periodicTargetRadius")));
        }

        private static SynergyBalanceParameters Load(IDataProvider data, string profileId)
        {
            SynergyData profile = data.GetSynergy(profileId)
                ?? throw new InvalidOperationException("Synergy balance profile is missing: " + profileId);
            if (profile.Cooldown <= 0.0f)
                throw new InvalidOperationException("Synergy balance cooldown is invalid: " + profileId);
            return new SynergyBalanceParameters(profileId, profile.Cooldown, profile.BalanceParameters);
        }

        private static PairSynergyBalance CreatePairFirst(SynergyBalanceParameters values) =>
            new PairSynergyBalance(
                values.Float("crossSlashDamage"), values.Float("crossSlashRadius"),
                values.Float("vulnerableCutDamage"), values.Float("vulnerableCutRadius"),
                values.Float("vulnerableCutDelay"), values.Float("mistRadius"),
                values.Int("mistTargetLimit"), values.Float("weakenMagnitude"),
                values.Float("weakenDuration"), values.Float("soulHealing"),
                values.Float("cleansingDamage"), values.Float("cleansingWidth"),
                values.Float("cremationPullRadius"), values.Float("cremationPullDistance"),
                values.Float("cremationDamage"), values.Float("cremationRadius"), values.Cooldown);

        private static PairSynergySecondBalance CreatePairSecond(SynergyBalanceParameters values) =>
            new PairSynergySecondBalance(
                values.Float("thunderPullRadius"), values.Float("thunderPullDistance"),
                values.Float("thunderDamage"), values.Int("thunderChainLimit"),
                values.Float("conductiveDamage"), values.Int("conductiveHitLimit"),
                values.Float("huntingBiteDamage"), values.Float("huntingRange"),
                values.Float("arrowRainDamage"), values.Float("arrowRainRadius"),
                values.Float("arrowRainDelay"), values.Float("precisionBombDamage"),
                values.Float("precisionBombRadius"), values.Float("precisionBombDelay"),
                values.Float("coverBombDamage"), values.Float("coverBombRadius"),
                values.Float("coverBombDelay"), values.Cooldown);

        private static TrioSynergyBalance CreateTrioFirst(SynergyBalanceParameters values) =>
            new TrioSynergyBalance(
                values.Float("guardWaveDamage"), values.Float("guardWaveRadius"),
                values.Float("guardPushDistance"), values.Float("guardSwordDamage"),
                values.Float("guardHealing"), values.Float("barrageArrowDamage"),
                values.Float("barrageScytheDamage"), values.Float("barrageBombDamage"),
                values.Float("barrageWidth"), values.Float("ritualPullDistance"),
                values.Float("ritualRadius"), values.Float("ritualFireDamage"),
                values.Float("ritualLightningDamage"), values.Float("ritualExplosionDamage"),
                values.Float("lureVulnerability"), values.Float("lureDuration"),
                values.Float("huntArrowDamage"), values.Float("huntBiteDamage"),
                values.Float("huntRadius"), values.Cooldown);

        private static TrioSynergySecondBalance CreateTrioSecond(SynergyBalanceParameters values) =>
            new TrioSynergySecondBalance(
                values.Float("undeadMarchDamage"), values.Float("undeadWeaken"),
                values.Float("undeadWeakenDuration"), values.Float("undeadScytheDamage"),
                values.Float("alchemyVulnerability"), values.Float("alchemyVulnerabilityDuration"),
                values.Float("alchemyBombDamage"), values.Float("alchemyExplosionDamage"),
                values.Float("alchemyFieldDamage"), values.Float("alchemyRadius"),
                values.Float("assaultShieldDamage"), values.Float("assaultSwordDamage"),
                values.Float("assaultBiteDamage"), values.Float("assaultPathWidth"),
                values.Float("sanctuaryBindDuration"), values.Float("sanctuaryDamage"),
                values.Float("sanctuaryRadius"), values.Cooldown);
    }

    internal sealed class SynergyBalanceParameters
    {
        private readonly string _profileId;
        private readonly Dictionary<string, float> _values =
            new Dictionary<string, float>(StringComparer.Ordinal);

        internal SynergyBalanceParameters(string profileId, float cooldown, string parameters)
        {
            _profileId = profileId;
            Cooldown = cooldown;
            string[] entries = (parameters ?? string.Empty).Split(';');
            for (int index = 0; index < entries.Length; index++)
            {
                string entry = entries[index].Trim();
                if (entry.Length == 0)
                    continue;
                int separator = entry.IndexOf('=');
                if (separator <= 0
                    || !float.TryParse(
                        entry.Substring(separator + 1),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out float value)
                    || value <= 0.0f
                    || !_values.TryAdd(entry.Substring(0, separator), value))
                {
                    throw new InvalidOperationException(
                        "Synergy balance parameter is invalid: " + profileId + ":" + entry);
                }
            }
        }

        internal float Cooldown { get; }

        internal float Float(string key)
        {
            if (_values.TryGetValue(key, out float value))
                return value;
            throw new InvalidOperationException(
                "Synergy balance parameter is missing: " + _profileId + ":" + key);
        }

        internal int Int(string key)
        {
            float value = Float(key);
            int converted = (int)value;
            if (Math.Abs(value - converted) <= 0.0001f)
                return converted;
            throw new InvalidOperationException(
                "Synergy balance integer parameter is invalid: " + _profileId + ":" + key);
        }
    }
}
