using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.RunCore;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.EditorTests
{
    public sealed class CompanionCombatHitReadabilityTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _objects.Count - 1; index >= 0; index -= 1)
                Object.DestroyImmediate(_objects[index]);
            _objects.Clear();
        }

        [Test]
        public void SwordCone_AffectsEveryValidTargetInsideItsAuthoredShape()
        {
            LocalDataProvider data = new LocalDataProvider(new TestAssetService());
            LogAssert.Expect(LogType.Error, new Regex("\\[LocalDataProvider\\] Local data asset was not available\\."));
            data.InitializeAsync().GetAwaiter().GetResult();
            CombatEffectData effect = data.GetCombatEffect("dmg_sword_slash_v1");
            Assert.That(effect.AffectsAllTargetsInShape, Is.True);

            RuntimeObjectRegistry registry = new RuntimeObjectRegistry(new NoopFactory());
            for (int index = 0; index < 5; index += 1)
            {
                GameObject enemyObject = NewObject("Enemy" + index);
                MonsterController enemy = enemyObject.AddComponent<MonsterController>();
                enemy.MaxHp = 10;
                enemy.Hp = 10;
                enemy.transform.position = new Vector3(0.45f + index * 0.1f, (index - 2) * 0.03f, 0.0f);
                registry.RegisterEnemy(enemy);
            }

            RecordingImmediateHits immediateHits = new RecordingImmediateHits();
            System.Type worldType = typeof(CompanionRecordingProductionHost).Assembly.GetType(
                "Lizzo.PV.Legion.RunCore.CompanionRecordingCombatWorld",
                true);
            object world = System.Activator.CreateInstance(
                worldType,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { data, registry, new NoopProjectiles(), immediateHits, new NoopFields() },
                null);
            EffectIntent intent = new EffectIntent(
                1L,
                "squad-0",
                "sword_soldier",
                effect.Id,
                effect.BaseValue,
                new CompanionPoint(1.0f, 0.0f),
                CombatMotion.Excursion,
                AttackDelivery.Direct,
                0,
                effect.Id,
                0.0f,
                1L,
                0,
                CompanionPoint.Zero);

            MethodInfo resolve = worldType.GetMethod("Resolve", BindingFlags.Instance | BindingFlags.Public);
            EffectResolution resolution = (EffectResolution)resolve.Invoke(world, new object[] { intent });

            Assert.That(resolution.AffectedTargetCount, Is.EqualTo(5));
            Assert.That(immediateHits.AppliedCount, Is.EqualTo(5));
        }

        [Test]
        public void StationaryAction_WithPositiveAcquisitionRange_DoesNotCommitDistantTarget()
        {
            ActionStep step = new ActionStep(
                CombatMotion.Stationary,
                AttackDelivery.Direct,
                "dmg_shield_bash_v1",
                6.0f,
                "dmg_shield_bash_v1",
                0.0f,
                0.0f,
                0.0f,
                0.0f,
                0.0f,
                1.2f);
            ActionSet set = new ActionSet("shield", 0.1f, new[] { step });
            FixedTargetWorld world = new FixedTargetWorld(new CompanionPoint(2.0f, 0.0f));
            using CompanionRunModule module = new CompanionRunModule(
                new RunCombatContext(1UL, new SingleDefinitionCatalog("shield_guard", set), world));
            Assert.That(module.Submit(new CompanionRosterCommand(
                1L,
                CompanionRosterCommandKind.Recruit,
                "shield_guard")).Accepted, Is.True);

            module.Advance(new CompanionAdvanceRequest(1L, 0.2f, CompanionPoint.Zero));

            Assert.That(world.ResolveCount, Is.Zero);
            Assert.That(module.CaptureSnapshot().Squads[0].ActionPhase, Is.EqualTo(SquadActionPhase.Idle));
        }

        [Test]
        public void HitImpact_StartsFlashAndSubtleVisualReactionTogether()
        {
            GameObject root = NewObject("Enemy");
            GameObject visual = NewObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.AddComponent<SpriteRenderer>().color = new Color(0.4f, 0.7f, 0.9f, 1.0f);
            HitFlash flash = root.AddComponent<HitFlash>();

            flash.PlayImpact();

            Assert.That(visual.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.white));
        }

        [Test]
        public void ChargeAndDashBehaviors_DoNotTintEnemySpritesOutsideHitFeedback()
        {
            string enemiesPath = Path.Combine(Application.dataPath, "_LizzoPV/Gameplay/Enemies/Runtime");
            string wolfSource = File.ReadAllText(Path.Combine(enemiesPath, "WolfDashBehaviour.cs"));
            string chargerSource = File.ReadAllText(Path.Combine(enemiesPath, "RedChargerBehaviour.cs"));
            string bossSource = File.ReadAllText(Path.Combine(enemiesPath, "HungryGiantBehaviour.Movement.cs"));

            StringAssert.DoesNotContain("DashWarningColor", wolfSource);
            StringAssert.DoesNotContain("DashColor", wolfSource);
            StringAssert.DoesNotContain("ChargeWarningColor", chargerSource);
            StringAssert.DoesNotContain("ChargeColor", chargerSource);
            StringAssert.DoesNotContain("ChargeWarningColor", bossSource);
            StringAssert.DoesNotContain("ChargeColor", bossSource);
        }

        private GameObject NewObject(string name)
        {
            GameObject value = new GameObject(name);
            _objects.Add(value);
            return value;
        }

        private sealed class RecordingImmediateHits : ICombatImmediateHitModule
        {
            public int AppliedCount { get; private set; }
            public bool TryApply(in CombatImmediateHitRequest request)
            {
                AppliedCount += 1;
                return true;
            }
        }

        private sealed class NoopProjectiles : ICombatProjectileModule
        {
            public bool TrySpawn(in CombatProjectileRequest request) => false;
        }

        private sealed class NoopFields : ICombatPersistentFieldModule
        {
            public int ActiveFieldCount => 0;
            public bool TrySpawn(in CombatPersistentFieldRequest request, float currentTime) => false;
            public void Tick(float currentTime) { }
            public void Reset() { }
            public void Dispose() { }
        }

        private sealed class NoopFactory : IPrefabFactory
        {
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false) => null;
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => null;
            public void Release(GameObject instance) { }
            public void Clear() { }
        }

        private sealed class SingleDefinitionCatalog : ICompanionDefinitionCatalog
        {
            private readonly string _id;
            private readonly CompanionDefinition _definition;

            public SingleDefinitionCatalog(string id, ActionSet set)
            {
                _id = id;
                _definition = new CompanionDefinition(id, set);
            }

            public bool TryGetDefinition(string companionId, out CompanionDefinition definition)
            {
                definition = companionId == _id ? _definition : null;
                return definition != null;
            }
        }

        private sealed class FixedTargetWorld : ICompanionCombatWorld
        {
            private readonly CompanionPoint _target;
            public FixedTargetWorld(CompanionPoint target) => _target = target;
            public int ResolveCount { get; private set; }
            public bool TrySelectTargetPosition(out CompanionPoint targetPosition)
            {
                targetPosition = _target;
                return true;
            }
            public EffectResolution Resolve(in EffectIntent intent)
            {
                ResolveCount += 1;
                return new EffectResolution(true, intent.EffectId, intent.SourceMagnitude, 1);
            }
        }
    }
}
