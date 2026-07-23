using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using Lizzo.PV.Data;
using Lizzo.PV.Tests.Support;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class LocalDataProviderTests
    {
        [Test]
        public void ProjectGameDataLoadsWithoutAddressables()
        {
            TestAssetService assets = new TestAssetService();
            UnityEngine.TextAsset gameData = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>("Assets/Data/Runtime/GameData.xml");
            Assert.IsNotNull(gameData);
            assets.Register("PlayerData.xml", gameData);

            LocalDataProvider provider = new LocalDataProvider(assets);
            DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();

            Assert.IsTrue(result.Succeeded);
            Assert.IsFalse(result.UsedFallback);
            Assert.AreEqual(8, provider.GetLevelExp(1));
            Assert.AreEqual(2500, provider.GetEnemy("boss_hungry_giant").Hp);
        }

        [Test]
        public void MissingLocalAssetUsesFallbackAndReportsParseError()
        {
            LogAssert.Expect(UnityEngine.LogType.Error, new Regex("\\[LocalDataProvider\\].*"));
            LocalDataProvider provider = new LocalDataProvider(new TestAssetService());
            DataLoadResult result = provider.InitializeAsync().GetAwaiter().GetResult();

            Assert.IsTrue(result.Succeeded);
            Assert.IsTrue(result.UsedFallback);
            Assert.IsNotEmpty(result.ParseError);
            Assert.AreEqual(2500, provider.GetEnemy("boss_hungry_giant").Hp);
        }
    }
}
