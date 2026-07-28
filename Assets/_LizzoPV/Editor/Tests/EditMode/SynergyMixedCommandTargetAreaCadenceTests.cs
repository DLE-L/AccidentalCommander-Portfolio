using System.Reflection;
using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SynergyMixedCommandTargetAreaCadenceTests
    {
        [Test]
        public void DivisorAwareImpact_ChangesOnlySuccessfulTargetCadence()
        {
            TargetAreaCastState state = new TargetAreaCastState();
            state.Configure(new CompanionTargetAreaCombatSetup("test", 1, 2.30f, 5.0f, 1.0f, 1, 0.10f, 0.20f), 0.0f, 0.0f);
            state.RecordNoTarget(0.0f);
            Assert.AreEqual(0.20f, state.NextTargetDueTime, 0.0001f);
            Assert.IsTrue(state.TryBeginCast(0.20f, Vector3.right));
            Assert.IsFalse(state.TryConsumeImpact(0.29f, out _));

            MethodInfo divisorOverload = typeof(TargetAreaCastState).GetMethod("TryConsumeImpact", new[] { typeof(float), typeof(float), typeof(Vector3).MakeByRefType() });
            Assert.IsNotNull(divisorOverload, "public divisor-aware TryConsumeImpact overload");
            object[] args = { 0.30f, 1.15f, Vector3.zero };
            Assert.IsTrue((bool)divisorOverload.Invoke(state, args));
            Assert.AreEqual(2.30f, state.NextTargetDueTime, 0.0001f);

            state.Restart(0.0f, 0.0f);
            Assert.IsTrue(state.TryBeginCast(0.0f, Vector3.right));
            Assert.IsTrue(state.TryConsumeImpact(0.10f, out _));
            Assert.AreEqual(2.40f, state.NextTargetDueTime, 0.0001f);
        }
    }
}
