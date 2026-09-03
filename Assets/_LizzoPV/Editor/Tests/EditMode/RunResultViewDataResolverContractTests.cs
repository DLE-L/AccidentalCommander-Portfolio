using System;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay;
using Lizzo.PV.Presentation;
using Lizzo.PV.Tests.Support;
using Lizzo.PV.UI;
using NUnit.Framework;
using UnityEditor;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunResultViewDataResolverContractTests
    {
        private GameplayContentSpriteProfileSO _contentProfile;
        private AssetCatalogBundleLease _sharedLease;
        private AssetCatalogBundleLease _gameplayLease;

        [SetUp]
        public void SetUp()
        {
            GameplayPresentationSetSO set = AssetDatabase.LoadAssetAtPath<GameplayPresentationSetSO>(
                "Assets/_LizzoPV/Gameplay/UI/Presentation/Data/Profiles/GameplayPresentationSet.asset");
            Assert.That(set, Is.Not.Null);
            var catalogs = new AssetCatalogBundleRuntime();
            Assert.That(catalogs.Acquire(set.SharedCatalogBundle, out _sharedLease, out string sharedIssue), Is.True, sharedIssue);
            Assert.That(catalogs.Acquire(set.GameplayCatalogBundle, out _gameplayLease, out string gameplayIssue), Is.True, gameplayIssue);
            _contentProfile = set.ContentSpriteProfile;
            Assert.That(GameplayContentSpriteProvider.Configure(_contentProfile, catalogs, out string contentIssue), Is.True, contentIssue);
        }

        [TearDown]
        public void TearDown()
        {
            GameplayContentSpriteProvider.Clear(_contentProfile);
            _gameplayLease?.Dispose();
            _sharedLease?.Dispose();
        }

        [TestCase(RunOutcome.Clear, "승리", true)]
        [TestCase(RunOutcome.Failure, "패배", false)]
        [TestCase(RunOutcome.Abandoned, "전투 종료", false)]
        public void Resolve_UsesCommonCopyAndActualSettlement(
            RunOutcome outcome,
            string expectedTitle,
            bool expectedClear)
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();
            RunResult result = new RunResult(outcome, outcome == RunOutcome.Clear ? 0 : 42, 64.0f, 9);
            Assert.That(
                fixture.Run.ResultRewards.TrySettle(
                    result,
                    fixture.Run.Context.StageId,
                    out RunRewardSettlement settlement,
                    out string issue),
                Is.True,
                issue);

            RunResultViewData view = Resolve(result, fixture.Run, settlement);

            Assert.That(view.IsClear, Is.EqualTo(expectedClear));
            Assert.That(view.Title, Is.EqualTo(expectedTitle));
            Assert.That(view.StageGroupLabel, Is.EqualTo("챕터 1"));
            Assert.That(view.StageNameLabel, Is.EqualTo("경계선의 망꾼"));
            Assert.That(view.RewardPresentations.Count, Is.EqualTo(2));
            Assert.That(view.RewardPresentations[0].DisplayName, Is.EqualTo("골드"));
            Assert.That(view.RewardPresentations[0].Amount, Is.EqualTo(100));
            Assert.That(view.RewardPresentations[0].Icon, Is.Not.Null);
            Assert.That(view.RewardPresentations[1].DisplayName, Is.EqualTo("군단 스크롤"));
            Assert.That(view.RewardPresentations[1].Amount, Is.EqualTo(1));
            Assert.That(view.RewardPresentations[1].Icon, Is.Not.Null);
        }

        [Test]
        public void Resolve_TutorialClearUsesTutorialHeader()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture(RunContext.Tutorial);
            RunResult result = new RunResult(RunOutcome.Clear, 0, 180.0f, 20);
            Assert.That(
                fixture.Run.ResultRewards.TrySettle(
                    result,
                    fixture.Run.Context.StageId,
                    out RunRewardSettlement settlement,
                    out string issue),
                Is.True,
                issue);

            RunResultViewData view = Resolve(result, fixture.Run, settlement);

            Assert.That(view.Title, Is.EqualTo("튜토리얼 완료"));
            Assert.That(view.StageGroupLabel, Is.EqualTo("튜토리얼"));
        }

        private static RunResultViewData Resolve(
            RunResult result,
            RunServices services,
            RunRewardSettlement settlement)
        {
            Type resolver = typeof(RunResultViewData).Assembly.GetType("Lizzo.PV.UI.RunResultViewDataResolver");
            Assert.That(resolver, Is.Not.Null);
            MethodInfo method = resolver.GetMethod("Resolve", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (RunResultViewData)method.Invoke(null, new object[] { result, services, settlement });
        }
    }
}
