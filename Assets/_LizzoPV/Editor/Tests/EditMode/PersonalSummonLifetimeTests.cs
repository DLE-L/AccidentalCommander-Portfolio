using Lizzo.PV.Combat;
using Lizzo.PV.Legion.Summons;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class PersonalSummonLifetimeTests
    {
        [Test]
        public void AuthoredSummon_HasNoEnabledSolidCollider()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_LizzoPV/Gameplay/Legion/Prefabs/Characters/Supports/PersonalSkeletonSummon.prefab");
            Assert.That(prefab, Is.Not.Null);
            foreach (var collider in prefab.GetComponentsInChildren<Collider2D>(true))
                Assert.That(collider.enabled && !collider.isTrigger, Is.False,
                    "Personal summons must not push enemies: " + collider.name);
        }

        [TestCase(0f, false)]
        [TestCase(-1f, false)]
        [TestCase(float.NaN, false)]
        [TestCase(float.PositiveInfinity, false)]
        [TestCase(6f, true)]
        public void SpawnRequest_RequiresPositiveFiniteLifetime(float seconds, bool expected)
        {
            var owner = new GameObject("summon-owner");
            try
            {
                var setup = new CompanionPersonalSummonSetup(new CompanionSummonData
                {
                    Id = "test-summon", Damage = 4, AttackInterval = 1.3f, Range = 1f,
                    MoveSpeed = 2.7f, AiScanInterval = .2f, LifetimeRuleId = "timed_group",
                });
                var request = new PersonalSummonSpawnRequest("owner", "summon", owner.transform,
                    Vector3.zero, "test-address", setup, 3, seconds, Vector3.zero, 5f);
                Assert.That(request.IsValid, Is.EqualTo(expected));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void PersonalSummon_HasNoDamageTargetOrHealthContract()
        {
            Assert.That(typeof(ICombatImmediateHitTarget).IsAssignableFrom(typeof(PersonalSummonRuntime)), Is.False);
            Assert.That(typeof(PersonalSummonRuntime).GetProperty("Hp"), Is.Null);
            Assert.That(typeof(PersonalSummonRuntime).GetProperty("MaxHp"), Is.Null);
            Assert.That(typeof(CompanionSummonData).GetField("Hp"), Is.Null);
            Assert.That(typeof(CompanionPersonalSummonSetup).GetField("Hp"), Is.Null);
        }
    }
}
