using Lizzo.PV.Gameplay.Units;
using NUnit.Framework;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CombatHealthStateTests
    {

        private HealthCharacterizationProbe _actor;

        [Test]
        public void HealingAndReset_KeepTheExplicitHealthContract()
        {
            var health = new Lizzo.PV.Combat.CombatHealthState();
            health.Restore(30, 100);
            Assert.AreEqual(70, health.Heal(90));
            Assert.AreEqual(100, health.Current);
            Assert.AreEqual(0, health.Heal(1));
            health.Reset(45);
            Assert.AreEqual(45, health.Current);
            Assert.AreEqual(45, health.Maximum);
            health.Restore(-5, 20);
            Assert.AreEqual(-5, health.Current, "Snapshot restoration preserves authored/diagnostic values.");
        }

        [SetUp]
        public void SetUp()
        {

            _actor = new HealthCharacterizationProbe();
        }

        [Test]
        public void Damage_ClampsLethalHitAndNotifiesOnceEvenDuringReentry()
        {
            Assert.That(_actor.Hp, Is.EqualTo(100));
            Assert.That(_actor.MaxHp, Is.EqualTo(100));
            _actor.OnDamaged(null, 30);
            Assert.That(_actor.Hp, Is.EqualTo(70));
            Assert.That(_actor.Deaths, Is.Zero);
            _actor.OnDamaged(null, 200);
            Assert.That(_actor.Hp, Is.Zero);
            Assert.That(_actor.HpObservedOnDeath, Is.Zero);
            Assert.That(_actor.Deaths, Is.EqualTo(1));
            _actor.OnDamaged(null, 10);
            Assert.That(_actor.Deaths, Is.EqualTo(1));
        }

        [Test]
        public void DirectAssignments_DoNotClampOrEmitDeathAndAllowRespawn()
        {
            _actor.MaxHp = 20;
            Assert.That(_actor.Hp, Is.EqualTo(100));
            _actor.Hp = 0;
            Assert.That(_actor.Deaths, Is.Zero);
            _actor.OnDamaged(null, -20);
            Assert.That(_actor.Hp, Is.Zero);
            _actor.Hp = 15;
            _actor.OnDamaged(null, 15);
            Assert.That(_actor.Deaths, Is.EqualTo(1));
            _actor.MaxHp = _actor.Hp = 50;
            _actor.OnDamaged(null, 50);
            Assert.That(_actor.Deaths, Is.EqualTo(2));
        }

        [Test]
        public void LegacySignedDamage_PreservesExistingArithmetic()
        {
            _actor.OnDamaged(null, 0);
            Assert.That(_actor.Hp, Is.EqualTo(100));
            _actor.OnDamaged(null, -20);
            Assert.That(_actor.Hp, Is.EqualTo(120));
            Assert.That(_actor.MaxHp, Is.EqualTo(100));
            Assert.That(_actor.Deaths, Is.Zero);
            _actor.Hp = -5;
            _actor.OnDamaged(null, 1);
            Assert.That(_actor.Hp, Is.EqualTo(-5));
            Assert.That(_actor.Deaths, Is.Zero);
        }
    }

    public sealed class HealthCharacterizationProbe
    {        private readonly Lizzo.PV.Combat.CombatHealthState _health = new();
        public int Hp { get => _health.Current; set => _health.Restore(value, MaxHp); }
        public int MaxHp { get => _health.Maximum; set => _health.Restore(Hp, value); }
        public void OnDamaged(UnityEngine.Component attacker, int damage)
        {
            if (_health.ApplyDamageAndCheckDeath(damage)) OnDead();
        }
        public int Deaths { get; private set; }
        public int HpObservedOnDeath { get; private set; }
        private void OnDead()
        {
            Deaths++;
            HpObservedOnDeath = Hp;
            OnDamaged(null, 1);
        }
    }
}
