using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Combat.Attacks;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
namespace Lizzo.PV.Tests.EditMode
{
    public sealed class ClericHealAttackTests
    {
        private ClericHealTestVisualFactory _visualFactory;

[SetUp]
        public void IgnoreAuthoredVisualLogNoise()
        {
            _visualFactory = new ClericHealTestVisualFactory();
            LogAssert.ignoreFailingMessages = true;
        }

[TearDown]
        public void ClearVisualServices()
        {
            LogAssert.ignoreFailingMessages = false;
            AttackVisual.ClearServices();
            FloatingDamageText.ClearServices();
            RetroVfx.ClearServices();
            _visualFactory.Clear();
            _visualFactory = null;
        }

        [Test]
        public void CommanderAt31To99Percent_IsHealedWhenNoCompanionIsDamaged()
        {
            using ServiceTestFixture fixture = CreateFixture();
            PlayerController player = CreatePlayer(fixture, 50);

            bool resolved = ClericHealAttack.TryResolve(fixture.Run.Party, 10);

            Assert.IsTrue(resolved);
            Assert.AreEqual(60, player.Hp);
        }

        [Test]
        public void LowestDamagedTarget_IsHealedOnEachCastUntilPartyIsFull()
        {
            using ServiceTestFixture fixture = CreateFixture();
            PlayerController player = CreatePlayer(fixture, 80);
            CompanionRuntime companion = AddCompanion(fixture, "shield_guard", 40);

            int castCount = 0;
            while (ClericHealAttack.TryResolve(fixture.Run.Party, 10))
                castCount++;

            Assert.AreEqual(8, castCount);
            Assert.AreEqual(player.MaxHp, player.Hp);
            Assert.AreEqual(companion.MaxHp, companion.Hp);
        }

[Test]
        public void FullParty_DoesNotResolveAHeal()
        {
            using ServiceTestFixture fixture = CreateFixture();
            PlayerController player = CreatePlayer(fixture, 100);
            CompanionRuntime companion = AddCompanion(fixture, "shield_guard", 80);
            SetProperty(companion, "Hp", companion.MaxHp);

            Assert.IsFalse(ClericHealAttack.TryResolve(fixture.Run.Party, 10));
            Assert.AreEqual(player.MaxHp, player.Hp);
            Assert.AreEqual(companion.MaxHp, companion.Hp);
        }

        [Test]
        public void DownedCompanion_IsRecoveredBeforeLowestNormalTarget()
        {
            using ServiceTestFixture fixture = CreateFixture();
            PlayerController player = CreatePlayer(fixture, 50);
            CompanionRuntime downed = AddCompanion(fixture, "shield_guard", 0, true);
            CompanionRuntime damaged = AddCompanion(fixture, "sword_soldier", 20);

            bool resolved = ClericHealAttack.TryResolve(fixture.Run.Party, 10);

            Assert.IsTrue(resolved);
            Assert.IsFalse(downed.IsDown);
            Assert.Greater(downed.Hp, 0);
            Assert.AreEqual(50, player.Hp);
            Assert.AreEqual(20, damaged.Hp);
        }

private ServiceTestFixture CreateFixture()
        {
            ServiceTestFixture fixture = new ServiceTestFixture();
            fixture.Data.InitializeAsync().GetAwaiter().GetResult();
            AttackVisual.Configure(_visualFactory);
            FloatingDamageText.Configure(_visualFactory);
            RetroVfx.Configure(fixture.App.Assets, fixture.Run.Factory);
            return fixture;
        }

        private static PlayerController CreatePlayer(ServiceTestFixture fixture, int hp)
        {
            GameObject playerObject = new GameObject("TestCommander");
            PlayerController player = playerObject.AddComponent<PlayerController>();
            player.MaxHp = 100;
            player.Hp = hp;
            fixture.Run.Registry.RegisterPlayer(player);
            return player;
        }

        private static CompanionRuntime AddCompanion(
            ServiceTestFixture fixture,
            string unitId,
            int hp,
            bool isDown = false)
        {
            GameObject companionObject = new GameObject(unitId);
            companionObject.AddComponent<Rigidbody2D>();
            CircleCollider2D bodyCollider = companionObject.AddComponent<CircleCollider2D>();
            CircleCollider2D combatCollider = companionObject.AddComponent<CircleCollider2D>();
            combatCollider.isTrigger = true;
            companionObject.AddComponent<AllyCombat>();
            companionObject.AddComponent<HitFlash>();
            companionObject.AddComponent<CompanionHealthBar>();
            CreateCompanionUi(companionObject);


            CompanionRuntime companion = companionObject.AddComponent<CompanionRuntime>();
            SetPrivateField(companion, "_bodyCollider", bodyCollider);
            SetPrivateField(companion, "_combatCollider", combatCollider);
            companion.Configure(fixture.Run.Party, fixture.Data.GetUnit(unitId), unitId, false);
            SetProperty(companion, "Hp", hp);
            SetProperty(companion, "IsDown", isDown);

            List<CompanionRuntime> companions = (List<CompanionRuntime>)typeof(PartyService)
                .GetField("Companions", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(fixture.Run.Party);
            companions.Add(companion);
            return companion;
        }

        
        private static void CreateCompanionUi(GameObject companionObject)
        {
            Transform ui = new GameObject("UI").transform;
            ui.SetParent(companionObject.transform, false);

            Transform hpBarAnchor = new GameObject("HpBarAnchor").transform;
            hpBarAnchor.SetParent(ui, false);

            GameObject downMarker = new GameObject("P0_DownMarker");
            downMarker.transform.SetParent(hpBarAnchor, false);
            downMarker.AddComponent<TextMeshPro>();

            GameObject healthBar = new GameObject("P0_CompanionHPBar");
            healthBar.transform.SetParent(hpBarAnchor, false);
            CreateBarSprite(healthBar.transform, "Back");
            CreateBarSprite(healthBar.transform, "Fill");
            GameObject hpText = new GameObject("Text");
            hpText.transform.SetParent(healthBar.transform, false);
            hpText.AddComponent<TextMeshPro>();
        }

        private static void CreateBarSprite(Transform parent, string name)
        {
            GameObject part = new GameObject(name);
            part.transform.SetParent(parent, false);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0.0f, 0.0f, 1.0f, 1.0f), new Vector2(0.5f, 0.5f));
        }

private static void SetPrivateField(object target, string fieldName, object value)
        {
            typeof(CompanionRuntime)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            property.GetSetMethod(true).Invoke(target, new[] { value });
        }
    }
}
