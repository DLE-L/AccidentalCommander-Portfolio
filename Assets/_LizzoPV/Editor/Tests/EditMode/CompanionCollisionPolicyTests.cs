using System;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.P0.Units;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class CompanionCollisionPolicyTests
    {
        readonly List<GameObject> _objects = new();
        PartyService _party;
        RuntimeObjectRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            TestAssetService assets = new();
            assets.Register(
                "PlayerData.xml",
                AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml"));
            LocalDataProvider data = new(assets);
            Assert.IsTrue(data.InitializeAsync().GetAwaiter().GetResult().Succeeded);

            TestFactory factory = new();
            _registry = new RuntimeObjectRegistry(factory);
            _party = new PartyService(
                data,
                _registry,
                factory,
                new NoProjectile(),
                new NoImmediateHit(),
                new NoPersistentField());
        }

        [TearDown]
        public void TearDown()
        {
            _party?.Dispose();
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                    UnityEngine.Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
            _party = null;
            _registry = null;
        }

        [Test]
        public void ApplyToCompanion_IgnoresCommanderOtherCompanionAndExistingEnemyBodies()
        {
            PlayerController player = CreatePlayer("Player", out CircleCollider2D playerBody);
            _registry.RegisterPlayer(player);
            CompanionRuntime existing = CreateCompanion("Existing", out CircleCollider2D existingBody);
            CompanionRuntime candidate = CreateCompanion("Candidate", out CircleCollider2D candidateBody);
            GetCompanions(_party).Add(existing);
            GetCompanions(_party).Add(candidate);
            MonsterController enemy = CreateEnemy("Enemy", out CircleCollider2D enemyBody);
            _registry.RegisterEnemy(enemy);

            ApplyCollisionPolicyToCompanion(_party, candidate);

            Assert.IsTrue(Physics2D.GetIgnoreCollision(candidateBody, playerBody));
            Assert.IsTrue(Physics2D.GetIgnoreCollision(candidateBody, existingBody));
            Assert.IsTrue(Physics2D.GetIgnoreCollision(candidateBody, enemyBody));
        }

        [Test]
        public void PublicEnemyFacade_IgnoresCommanderAndAllCompanionBodies()
        {
            PlayerController player = CreatePlayer("Player", out CircleCollider2D playerBody);
            _registry.RegisterPlayer(player);
            CompanionRuntime first = CreateCompanion("First", out CircleCollider2D firstBody);
            CompanionRuntime second = CreateCompanion("Second", out CircleCollider2D secondBody);
            GetCompanions(_party).Add(first);
            GetCompanions(_party).Add(second);
            MonsterController enemy = CreateEnemy("Enemy", out CircleCollider2D enemyBody);

            _party.IgnoreFriendlyBodyCollisionsWithEnemy(enemy);

            Assert.IsTrue(Physics2D.GetIgnoreCollision(playerBody, enemyBody));
            Assert.IsTrue(Physics2D.GetIgnoreCollision(firstBody, enemyBody));
            Assert.IsTrue(Physics2D.GetIgnoreCollision(secondBody, enemyBody));
        }

        PlayerController CreatePlayer(string name, out CircleCollider2D body)
        {
            GameObject owner = CreateObject(name);
            body = owner.AddComponent<CircleCollider2D>();
            PlayerController player = owner.AddComponent<PlayerController>();
            SetField(player, "_bodyCollider", body);
            return player;
        }

        CompanionRuntime CreateCompanion(string name, out CircleCollider2D body)
        {
            GameObject owner = CreateObject(name);
            body = owner.AddComponent<CircleCollider2D>();
            CompanionRuntime companion = owner.AddComponent<CompanionRuntime>();
            SetField(companion, "_bodyCollider", body);
            return companion;
        }

        MonsterController CreateEnemy(string name, out CircleCollider2D body)
        {
            GameObject owner = CreateObject(name);
            body = owner.AddComponent<CircleCollider2D>();
            UnitColliderRefs colliders = owner.AddComponent<UnitColliderRefs>();
            SetField(colliders, "_bodyCollider", body);
            return owner.AddComponent<MonsterController>();
        }

        GameObject CreateObject(string name)
        {
            GameObject owner = new(name);
            _objects.Add(owner);
            return owner;
        }

        static void SetField(object owner, string fieldName, object value)
        {
            FieldInfo field = owner.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing test field: {owner.GetType().Name}.{fieldName}");
            field.SetValue(owner, value);
        }

        static List<CompanionRuntime> GetCompanions(PartyService party)
        {
            FieldInfo field = typeof(PartyService).GetField("Companions", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing PartyService.Companions test field.");
            return (List<CompanionRuntime>)field.GetValue(party);
        }

        static void ApplyCollisionPolicyToCompanion(PartyService party, CompanionRuntime companion)
        {
            Type module = typeof(PartyService).Assembly.GetType("Lizzo.PV.Legion.CompanionCollisionPolicyModule");
            Assert.IsNotNull(module, "Missing CompanionCollisionPolicyModule test type.");
            MethodInfo method = module.GetMethod(
                "ApplyCollisionPolicyToCompanion",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Missing companion collision policy test method.");
            method.Invoke(null, new object[] { party, companion });
        }

        sealed class TestFactory : IPrefabFactory
        {
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false) => null;
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) { }
            public void Clear() { }
        }

        sealed class NoProjectile : ICombatProjectileModule
        {
            public bool TrySpawn(in CombatProjectileRequest request) => false;
        }

        sealed class NoImmediateHit : ICombatImmediateHitModule
        {
            public bool TryApply(in CombatImmediateHitRequest request) => false;
        }

        sealed class NoPersistentField : ICombatPersistentFieldModule
        {
            public int ActiveFieldCount => 0;
            public bool TrySpawn(in CombatPersistentFieldRequest request, float currentTime) => false;
            public bool TryIgnite(in CombatPersistentFieldIgnitionRequest request, float currentTime, out int ignitedFieldCount)
            {
                ignitedFieldCount = 0;
                return false;
            }
            public void Tick(float currentTime) { }
            public void Reset() { }
            public void Dispose() { }
        }
    }
}
