#if false // Superseded by RunResultViewDataResolverContractTests.
using System;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Tests.Support;
using Lizzo.PV.UI;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class RunResultViewDataResolverTests
    {
        [Test]
        public void Resolve_PreservesClearAndFailureViewContracts()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();

            RunResultViewData clear = Resolve(
                new RunResult(RunOutcome.Clear, 0, 123.5f, 17),
                fixture.Run);
            Assert.IsTrue(clear.IsClear);
            Assert.AreEqual("승리", clear.Title);
            Assert.AreEqual("1-1", clear.StageLabel);
            Assert.AreEqual("다시 출정", clear.PrimaryButtonLabel);
            Assert.IsFalse(clear.OptionalButtonVisible);
            Assert.AreEqual(123.5f, clear.ElapsedSeconds);
            Assert.AreEqual(17, clear.KillCount);
            Assert.AreEqual(fixture.Run.State.Level, clear.Level);
            Assert.AreEqual("이번 클리어 우수 시너지", clear.SynergySectionLabel);
            Assert.AreEqual("우수 시너지 없음", clear.SynergyName);
            Assert.AreEqual(7, clear.SquadSlots.Count);
            Assert.AreEqual(7, clear.CompanionPresentations.Count);
            Assert.IsEmpty(clear.FinalLegion);
            Assert.IsEmpty(clear.CompletedSynergies);
            Assert.IsEmpty(clear.SelectedTraits);
            Assert.IsNull(clear.BestActiveSynergy);

            RunResultViewData failure = Resolve(
                new RunResult(RunOutcome.Failure, 42, 64.0f, 9),
                fixture.Run);
            Assert.IsFalse(failure.IsClear);
            Assert.AreEqual("쓰러졌습니다", failure.Title);
            Assert.AreEqual("이번 전투 기록", failure.Headline);
            Assert.AreEqual("다시 전장에 들어가 준비를 이어가세요.", failure.Body);
            Assert.AreEqual("다시 도전", failure.PrimaryButtonLabel);
            Assert.IsFalse(failure.OptionalButtonVisible);
            Assert.IsEmpty(failure.OptionalButtonLabel);
            Assert.AreEqual("사령관이 전투 중 쓰러졌습니다.", failure.FailureCause);
            Assert.AreEqual("동료를 모아 강화하세요.", failure.Recommendation);
            Assert.AreEqual("이번 런에서 완성한 시너지", failure.SynergySectionLabel);
            Assert.AreEqual("활성 시너지 없음", failure.SynergyName);
            Assert.IsNull(failure.BestActiveSynergy);
        }

        [Test]
        public void Resolve_TutorialClearUsesDedicatedCompletionCopy()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture(RunContext.Tutorial);

            RunResultViewData clear = Resolve(
                new RunResult(RunOutcome.Clear, 0, 180.0f, 20),
                fixture.Run);

            Assert.IsTrue(clear.IsClear);
            Assert.AreEqual("튜토리얼 완료", clear.Title);
            Assert.AreEqual("튜토리얼", clear.StageLabel);
            Assert.AreEqual("로비로", clear.PrimaryButtonLabel);
            Assert.IsFalse(clear.OptionalButtonVisible);
            Assert.AreEqual(7, clear.SquadSlots.Count);
        }

        [Test]
        public void Resolve_AbandonedUsesDistinctMinimalRewardCopy()
        {
            using ServiceTestFixture fixture = new ServiceTestFixture();

            RunResultViewData abandoned = Resolve(
                new RunResult(RunOutcome.Abandoned, -1, 45.0f, 6),
                fixture.Run);

            Assert.IsFalse(abandoned.IsClear);
            Assert.AreEqual("전투 종료", abandoned.Title);
            Assert.AreEqual("획득 보상", abandoned.Headline);
            Assert.AreEqual("메인으로", abandoned.PrimaryButtonLabel);
            Assert.IsEmpty(abandoned.FailureCause);
            Assert.IsEmpty(abandoned.Recommendation);
        }

        private static RunResultViewData Resolve(RunResult result, RunServices services)
        {
            Type resolver = typeof(RunResultViewData).Assembly.GetType("Lizzo.PV.UI.RunResultViewDataResolver");
            Assert.IsNotNull(resolver, "Missing RunResultViewDataResolver test type.");
            MethodInfo method = resolver.GetMethod("Resolve", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Missing result view-data resolver method.");
            return (RunResultViewData)method.Invoke(null, new object[] { result, services, null, null });
        }
    }
}
#endif
