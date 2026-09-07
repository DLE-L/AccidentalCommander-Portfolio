using NUnit.Framework;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Lizzo.PV.EditorTests
{
    public sealed class UnitAddressableContractTests
    {
        static readonly string[][] ExpectedEntries =
        {
            new[] { "438227feb6ef880419424ba3dc96a4a9", "Units/Enemies/SmallGoblin.prefab" },
            new[] { "5e2ac044b7cc4a94199d284dab3b2290", "Units/Enemies/HungryGiant.prefab" },
            new[] { "7d36c51ab95751b4b8ca05b49df8dfe7", "Units/Enemies/RedCharger.prefab" },
            new[] { "a354befacb7ce63438a5e04b5099d558", "Units/Enemies/ShieldOrc.prefab" },
            new[] { "b8bf201c1c163924589c805c7955a41e", "Units/Enemies/HungryWolf.prefab" },
            new[] { "ba5a0938b1bed7841a4024eac2b9d585", "Units/Commander/Commander.prefab" },
        };

        [Test]
        public void RuntimeUnitEntries_UseCurrentAddressesAndPreloadLabel()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            Assert.That(settings, Is.Not.Null);

            foreach (string[] expected in ExpectedEntries)
            {
                AddressableAssetEntry entry = settings.FindAssetEntry(expected[0]);
                Assert.That(entry, Is.Not.Null, expected[0]);
                Assert.That(entry.address, Is.EqualTo(expected[1]), expected[0]);
                CollectionAssert.Contains(entry.labels, "PreLoad", expected[1]);
            }
        }
    }
}
