using System.Reflection;
using Lizzo.PV.Legion;
using NUnit.Framework;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyMixedCommandAbilityScheduleTests
    {
        [Test]
        public void DivisorAwareResolution_ScalesOnlySuccessfulPeriodAndPreservesNeutralCompatibility()
        {
            CombatAbilitySchedule schedule = new CombatAbilitySchedule();
            schedule.Configure(2.30f, 0.20f, 0.0f, 0.0f);

            MethodInfo recordWithDivisor = typeof(CombatAbilitySchedule).GetMethod(
                "RecordResolution",
                new[] { typeof(float), typeof(bool), typeof(float) });
            Assert.IsNotNull(recordWithDivisor, "public divisor-aware RecordResolution overload");

            recordWithDivisor.Invoke(schedule, new object[] { 10.0f, true, 1.15f });
            Assert.AreEqual(12.00f, schedule.NextDueTime, 0.0001f);

            recordWithDivisor.Invoke(schedule, new object[] { 10.0f, false, 1.15f });
            Assert.AreEqual(10.20f, schedule.NextDueTime, 0.0001f);

            recordWithDivisor.Invoke(schedule, new object[] { 10.0f, true, 0.0f });
            Assert.AreEqual(12.30f, schedule.NextDueTime, 0.0001f);

            schedule.RecordResolution(10.0f, true);
            Assert.AreEqual(12.30f, schedule.NextDueTime, 0.0001f);
        }
    }
}
