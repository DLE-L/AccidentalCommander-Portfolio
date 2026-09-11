using Lizzo.PV.Combat;
using Lizzo.PV.Gameplay.Visuals;
using System.Reflection;
using Lizzo.PV.Legion;
using Lizzo.PV.Gameplay.Telemetry;
using Lizzo.PV.Gameplay.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class BossWorldFeedbackRegressionTests
    {
        private const string HungryGiantPrefabPath =
            "Assets/_LizzoPV/Gameplay/Enemies/Prefabs/Units/HungryGiant.prefab";

        [Test]
        public void StaggerDamage_BelongsToEachBossRegardlessOfSetupOrder()
        {
            using var first = new BossPhysicsFixture();
            using var second = new BossPhysicsFixture();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var window = first.Monster.GetComponent<BossVulnerabilityWindow>();
            typeof(BossVulnerabilityWindow).GetField("_staggerRemaining", flags).SetValue(window, 1f);
            var resolve = typeof(EnemyActor).GetMethod("ResolveBossStaggerDamage", flags);
            Assert.That(resolve.Invoke(first.Monster, new object[] { 100 }), Is.EqualTo(118));
            Assert.That(resolve.Invoke(second.Monster, new object[] { 100 }), Is.EqualTo(100));
            int hpBefore = first.Monster.Hp;
            first.Monster.OnDamagedFromPosition(Vector3.zero, 100);
            Assert.That(hpBefore - first.Monster.Hp, Is.EqualTo(118));
            second.Monster.gameObject.SetActive(false);
            Assert.That(resolve.Invoke(first.Monster, new object[] { 100 }), Is.EqualTo(118));
        }

        [Test]
        public void HitFlash_RepeatedHitPreservesOriginalTint()
        {
            RunTelemetry.BeginRun();
            GameObject root = new GameObject("HitFlashRegression");
            try
            {
                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                Color original = new Color(0.45f, 0.08f, 0.08f, 1f);
                renderer.color = original;
                HitFlash flash = root.AddComponent<HitFlash>();

                flash.Play();
                flash.Play();

                Assert.That(RunTelemetry.TryGetEventSnapshot(RunTelemetry.VfxLifecycle, out RunTelemetry.EventSnapshot active), Is.True);
                StringAssert.Contains("kind=hit_flash", active.LastParametersText);
                StringAssert.Contains("state=activate", active.LastParametersText);
                StringAssert.Contains("activation_sequence=2", active.LastParametersText);

                Color stored = (Color)typeof(HitFlash)
                    .GetField("_baseColor", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(flash);
                Assert.That(stored, Is.EqualTo(original));

                root.SetActive(false);
                MethodInfo onDisable = typeof(HitFlash).GetMethod(
                    "OnDisable",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(onDisable, Is.Not.Null);
                onDisable.Invoke(flash, null);
                Assert.That(RunTelemetry.GetCount(RunTelemetry.VfxLifecycle), Is.EqualTo(3));
                Assert.That(RunTelemetry.TryGetEventSnapshot(RunTelemetry.VfxLifecycle, out RunTelemetry.EventSnapshot disabled), Is.True);
                StringAssert.Contains("state=disable_cleanup", disabled.LastParametersText);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HungryGiantPrefab_HasVisibleAuthoredAttackTelegraphs()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HungryGiantPrefabPath);
            try
            {
                EnemyBossController boss = root.GetComponent<EnemyBossController>();
                Assert.That(boss, Is.Not.Null);
                SerializedObject serialized = new SerializedObject(boss);
                SpriteRenderer charge = serialized.FindProperty("_chargePathRenderer").objectReferenceValue
                    as SpriteRenderer;
                var areaAttack = root.GetComponent<EnemyAreaAttack>();
                SpriteRenderer aoe = new SerializedObject(areaAttack).FindProperty("_aoeWarningRenderer").objectReferenceValue
                    as SpriteRenderer;

                Assert.That(charge, Is.Not.Null);
                Assert.That(aoe, Is.Not.Null);
                Assert.That(charge.sprite, Is.Not.Null, "Charge telegraph needs a visible temporary sprite.");
                Assert.That(aoe.sprite, Is.Not.Null, "AOE telegraph needs a visible temporary sprite.");

                var constructorFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                using var areaPresentation = (System.IDisposable)System.Activator.CreateInstance(
                    typeof(EnemyAreaAttack).Assembly.GetType("Lizzo.PV.Gameplay.Visuals.EnemyAreaPresentation"),
                    constructorFlags, null, new object[] { areaAttack, aoe, 1.65f }, null);
                object pathPresentation = typeof(EnemyBossController).GetField("_chargePathWarning", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(boss);
                using var bossPresentation = (System.IDisposable)System.Activator.CreateInstance(
                    typeof(EnemyBossController).Assembly.GetType("Lizzo.PV.Gameplay.Visuals.BossChargePresentation"),
                    constructorFlags, null, new object[] { boss, pathPresentation, null, Color.white }, null);
                aoe.gameObject.SetActive(false);
                typeof(EnemyAreaAttack)
                    .GetMethod("BeginBossAoeWarning", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(areaAttack, new object[] { Vector2.zero });
                Assert.That(aoe.gameObject.activeSelf, Is.True);
                Assert.That(aoe.enabled, Is.True);
                Assert.That(aoe.bounds.size.x, Is.EqualTo(1.65f * 1.64f).Within(0.03f));
                Assert.That(aoe.bounds.size.y, Is.EqualTo(1.65f * 1.64f).Within(0.03f));

                typeof(EnemyAreaAttack)
                    .GetMethod("HideBossAoeWarning", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(areaAttack, null);
                object chargeWarning = typeof(EnemyBossController)
                    .GetField("_chargePathWarning", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(boss);
                chargeWarning.GetType()
                    .GetMethod("Bind", BindingFlags.Instance | BindingFlags.Public)
                    .Invoke(chargeWarning, new object[] { charge });
                typeof(EnemyBossController)
                    .GetMethod("BeginBossChargeWarning", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(boss, new object[] { Vector2.right });
                typeof(EnemyBossController)
                    .GetMethod("ShowBossChargePath", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(boss, new object[] { 0.5f });
                Assert.That(charge.gameObject.activeSelf, Is.True);
                Assert.That(charge.enabled, Is.True);
                Assert.That(charge.bounds.size.y, Is.EqualTo(1.25f).Within(0.03f));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void BossAoeDamage_UsesImmediateHitPipelineForCommanderFeedback()
        {
            using var fixture = new BossPhysicsFixture();
            int applied = 0;
            int hpBefore = fixture.Player.Hp;
            var module = (Lizzo.PV.Combat.CombatImmediateHitModule)fixture.ImmediateHits;
            module.Applied += request =>
            {
                Assert.That(request.Target, Is.SameAs(fixture.Player));
                Assert.That(request.Faction, Is.EqualTo(Lizzo.PV.Combat.CombatImmediateHitFaction.Enemy));
                applied++;
            };
            fixture.BeginWarning(true);
            for (int i = 0; i < 100 && fixture.Runner.Phase == EnemyActionPhase.Warning; i++) fixture.Step();
            for (int i = 0; i < 5; i++) fixture.Step();
            Assert.That(applied, Is.EqualTo(1));
            Assert.That(fixture.Player.Hp, Is.LessThan(hpBefore));
        }

        [Test]
        public void HungryGiantContactWindup_IsAuthored()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HungryGiantPrefabPath);
            try
            {
                EnemyBossController boss = root.GetComponent<EnemyBossController>();
                SerializedProperty windup = new SerializedObject(boss)
                    .FindProperty("_contactAttackWindupSeconds");
                Assert.That(windup, Is.Not.Null);
                Assert.That(windup.floatValue, Is.GreaterThan(0.0f));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

        }

        [Test]
        public void ExternalAttackPreparationLock_FreezesAndRestoresRigidbodyMotion()
        {
            GameObject root = new GameObject("ExternalAttackPreparationLock");
            try
            {
                Rigidbody2D body = root.AddComponent<Rigidbody2D>();
                body.gravityScale = 0.0f;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.linearVelocity = new Vector2(3.0f, -2.0f);
                body.angularVelocity = 4.0f;
                EnemyActor monster = root.AddComponent<EnemyActor>();
                MethodInfo setLock = typeof(EnemyActor).GetMethod(
                    "SetExternalAttackPreparationLocked",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                Assert.That(setLock, Is.Not.Null,
                    "External movers need a reusable attack-preparation physics lock.");
                setLock.Invoke(monster, new object[] { true });

                Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
                Assert.That(body.angularVelocity, Is.EqualTo(0.0f));
                Assert.That((body.constraints & RigidbodyConstraints2D.FreezePositionX) != 0, Is.True);
                Assert.That((body.constraints & RigidbodyConstraints2D.FreezePositionY) != 0, Is.True);

                setLock.Invoke(monster, new object[] { false });

                Assert.That(body.constraints, Is.EqualTo(RigidbodyConstraints2D.FreezeRotation));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HitFlash_RepeatedShakeRestoresOriginAndRecapturesAfterReuse()
        {
            GameObject root = new GameObject("RepeatedShakeRegression");
            try
            {
                Transform visual = new GameObject("Visual").transform;
                visual.SetParent(root.transform, false);
                HitFlash flash = root.AddComponent<HitFlash>();
                var onDisable = typeof(HitFlash).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);
                for (int cycle = 0; cycle < 2; cycle++)
                {
                    Vector3 origin = new Vector3(0.3f + cycle, 0.6f, 0f);
                    visual.localPosition = origin;
                    flash.PlayShake();
                    for (int hit = 0; hit < 3; hit++)
                    {
                        // Pin an in-flight shake offset independently of frame timing.
                        visual.localPosition = origin + Vector3.right * 0.04f;
                        flash.PlayShake();
                    }
                    onDisable.Invoke(flash, null);
                    Assert.That(Vector3.Distance(visual.localPosition, origin), Is.LessThan(0.00001f),
                        $"Repeated hits must restore the pre-shake position (cycle {cycle}).");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void HungryGiantPrefab_InterpolatesPhysicalMovement()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HungryGiantPrefabPath);
            try
            {
                Rigidbody2D body = root.GetComponent<Rigidbody2D>();
                Assert.That(body, Is.Not.Null);
                Assert.That(body.interpolation, Is.EqualTo(RigidbodyInterpolation2D.Interpolate));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void PhysicsPressureControl_UnlockedBossActuallyMovesUnderCollision()
        {
            using var fixture = new BossPhysicsFixture();
            Vector2 start = fixture.Body.position;
            for (int step = 0; step < 20; step++) fixture.SimulatePressureOnly();
            Assert.That(Vector2.Distance(start, fixture.Body.position), Is.GreaterThan(0.01f),
                "The locked-position tests require a real collision that moves the unlocked body.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RepeatedSpecialWarnings_HoldAgainstCollisionAndReleaseForMovement(bool aoe)
        {
            using var fixture = new BossPhysicsFixture();
            for (int cycle = 0; cycle < 5; cycle++)
            {
                fixture.ResetPositions();
                fixture.BeginWarning(aoe);
                Vector2 start = fixture.Body.position;
                int hpBefore = fixture.Player.Hp;
                for (int step = 0; step < 20; step++)
                {
                    fixture.PushAgainstBoss();
                    fixture.Step();
                }
                Assert.That(Vector2.Distance(start, fixture.Body.position), Is.LessThan(0.005f),
                    $"cycle={cycle}, aoe={aoe}, preparation drift");
                Assert.That(fixture.Player.Hp, Is.EqualTo(hpBefore), "Warning must not deal special damage.");
                for (int step = 0; step < 140; step++) fixture.Step();
                Assert.That(fixture.Body.constraints, Is.EqualTo(fixture.BaseConstraints),
                    $"cycle={cycle}, aoe={aoe}, attack left movement locked");
                Assert.That(Vector2.Distance(start, fixture.Body.position), Is.GreaterThan(0.01f),
                    $"cycle={cycle}, aoe={aoe}, movement did not resume");
                if (aoe) Assert.That(fixture.Player.Hp, Is.LessThan(hpBefore), "AOE must resolve after its warning.");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SpecialWarning_TargetAtBossOriginStillCompletes(bool aoe)
        {
            using var fixture = new BossPhysicsFixture();
            fixture.BeginWarning(aoe);
            fixture.Player.transform.position = fixture.Body.position;
            for (int step = 0; step < 80; step++) fixture.Step();
            Assert.That(fixture.Body.constraints, Is.EqualTo(fixture.BaseConstraints),
                "A zero target direction must not suspend an already committed warning and its physics lock.");
        }

        [TestCase("HungryGiant")]
        [TestCase("RedCharger")]
        [TestCase("HungryWolf")]
        public void ContactPreparation_HoldsUntilReadyThenRecoversOrReleasesOnSeparation(string prefab)
        {
            using var fixture = new BossPhysicsFixture(prefab);
            fixture.Player.transform.position = fixture.Monster.CombatCollider.bounds.center;
            Physics2D.SyncTransforms();
            Assert.That(fixture.Monster.TryEnterContactAttack(0.28f), Is.True);
            int hp = fixture.Player.Hp;
            Vector2 start = fixture.Body.position;
            for (int step = 0; step < 10; step++) fixture.Step();
            Assert.That(fixture.Player.Hp, Is.EqualTo(hp));
            Assert.That(Vector2.Distance(start, fixture.Body.position), Is.LessThan(0.005f));
            // Cross the clock boundary explicitly; physics simulation does not advance Time.time in EditMode.
            SetFixtureField(GetFixtureContactAttack(fixture.Monster), "_contactAttackReadyAt", Time.time - 1f);
            fixture.Monster.UpdateController();
            Assert.That(fixture.Player.Hp, Is.LessThan(hp));
            Assert.That(fixture.Body.constraints, Is.Not.EqualTo(fixture.BaseConstraints));
            Assert.That(fixture.Monster.TryEnterContactAttack(), Is.False, "Recovery must block the next attack.");
            for (int step = 0; step < 12; step++) fixture.Step();
            Assert.That(fixture.Body.constraints, Is.EqualTo(fixture.BaseConstraints));
            SetFixtureField(GetFixtureContactAttack(fixture.Monster), "_nextAttackTime", 0f);
            Assert.That(fixture.Monster.TryEnterContactAttack(0.28f), Is.True);
            fixture.Player.transform.position = new Vector3(20, 0, 0);
            Physics2D.SyncTransforms();
            fixture.Monster.UpdateController();
            Assert.That(fixture.Body.constraints, Is.EqualTo(fixture.BaseConstraints));
            Assert.That(fixture.Monster.CreatureState, Is.EqualTo(Define.CreatureState.Moving));
        }

        [TestCase("HungryGiant", false)]
        [TestCase("HungryGiant", true)]
        [TestCase("RedCharger", false)]
        [TestCase("HungryWolf", false)]
        public void SpecialAttackCompletion_HoldsForRecoveryThenResumes(string prefab, bool aoe)
        {
            using var fixture = new BossPhysicsFixture(prefab);
            fixture.FinishAttack(aoe);
            Vector2 end = fixture.Body.position;
            for (int step = 0; step < 5; step++) fixture.Step();
            Assert.That(Vector2.Distance(end, fixture.Body.position), Is.LessThan(0.005f),
                "Attack must have a short stationary recovery instead of immediately chasing again.");
            Assert.That(fixture.Body.constraints, Is.Not.EqualTo(fixture.BaseConstraints));
            for (int step = 0; step < 10; step++) fixture.Step();
            Assert.That(fixture.Body.constraints, Is.EqualTo(fixture.BaseConstraints));
            Assert.That(Vector2.Distance(end, fixture.Body.position), Is.GreaterThan(0.01f));
        }

        [TestCase("HungryGiant")]
        [TestCase("RedCharger")]
        [TestCase("HungryWolf")]
        public void Recovery_ExternalMovementReleaseAndSpawnResetRestoreConstraints(string prefab)
        {
            using var fixture = new BossPhysicsFixture(prefab);
            fixture.FinishAttack(false);
            fixture.Monster.SetExternalMovement(false);
            Assert.That(fixture.Body.constraints, Is.EqualTo(fixture.BaseConstraints));
            fixture.Monster.ResetForSpawn();
            Assert.That(fixture.Body.constraints, Is.EqualTo(fixture.BaseConstraints));
        }

        [TestCase("RedCharger")]
        [TestCase("HungryWolf")]
        public void Recovery_NonBossForcedMovementCanInterruptTheHold(string prefab)
        {
            using var fixture = new BossPhysicsFixture(prefab);
            fixture.FinishAttack(false);
            fixture.Monster.ApplySmoothKnockback(Vector3.left, 1f);
            Assert.That(fixture.Monster.IsForcedMovementActive, Is.True);
            Assert.That(fixture.Body.constraints, Is.EqualTo(fixture.BaseConstraints));
        }

        private static object GetFixtureContactAttack(EnemyActor actor) => typeof(EnemyActor).GetField("_contactAttack", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(actor);
        private static void SetFixtureField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        [Test]
        public void NewBossActionAdapter_ContactWaitsThenDamagesWithoutEnteringLegacySkill()
        {
            using var fixture = new BossPhysicsFixture();
            fixture.Player.transform.position = new Vector3(0.4f, 0, 0);
            Physics2D.SyncTransforms();
            int hp = fixture.Player.Hp;
            fixture.Step();
            Assert.That(fixture.Runner.Phase, Is.EqualTo(EnemyActionPhase.Warning));
            Assert.That(fixture.Monster.CreatureState, Is.EqualTo(Define.CreatureState.Moving));
            for (int i = 0; i < 10; i++) fixture.Step();
            Assert.That(fixture.Player.Hp, Is.EqualTo(hp));
            for (int i = 0; i < 6; i++) fixture.Step();
            Assert.That(fixture.Player.Hp, Is.LessThan(hp));
            Assert.That(fixture.Runner.Phase, Is.EqualTo(EnemyActionPhase.Recovery));
            Assert.That(fixture.Body.constraints, Is.Not.EqualTo(fixture.BaseConstraints));
        }

        [TestCase("HungryGiant")]
        [TestCase("RedCharger")]
        [TestCase("HungryWolf")]
        public void NewMovementOwner_ConsumesDisplacementOnceAndCanReleaseOwnership(string prefab)
        {
            using var fixture = new BossPhysicsFixture(prefab);
            fixture.Monster.ConfigureEncounterRank(Lizzo.PV.Data.EnemyEncounterRank.Elite, 1f);
            fixture.Monster.ApplySmoothKnockback(Vector3.left, 1f, .4f);
            fixture.StepLegacyMovement();
            Assert.That(fixture.Body.position.x, Is.EqualTo(0f).Within(.001f), "Legacy movement must yield to the new motor.");
            fixture.Step();
            float moved = fixture.Body.position.x;
            Assert.That(moved, Is.LessThan(-.005f), "The new motor must consume accepted forced movement.");
            fixture.Monster.SetExternalMovement(false);
            fixture.StepLegacyMovement();
            Assert.That(fixture.Body.position.x, Is.LessThan(moved - .005f), "Releasing ownership must restore the legacy movement route.");
        }

        [Test]
        public void ConsecutiveAreaAttacks_PreviousImpactCannotHideNextWarning()
        {
            using var fixture = new BossPhysicsFixture();
            var warning = fixture.Monster.transform.Find("BossAoeWarning").GetComponent<SpriteRenderer>();
            for (int attack = 0; attack < 3; attack++)
            {
                fixture.BeginWarning(true);
                int steps = 0;
                while (fixture.Runner.Phase == EnemyActionPhase.Warning && steps++ < 100)
                {
                    Assert.That(warning.enabled, Is.True, $"Attack {attack + 1}, warning frame {steps}");
                    fixture.Step();
                }
                Assert.That(fixture.Runner.Phase, Is.EqualTo(EnemyActionPhase.Recovery));
                steps = 0;
                while (fixture.Runner.Phase == EnemyActionPhase.Recovery && steps++ < 30) fixture.Step();
                Assert.That(fixture.Runner.Phase, Is.EqualTo(EnemyActionPhase.Idle));
            }
            for (int step = 0; step < 20; step++) fixture.Step();
            Assert.That(warning.enabled, Is.False, "The final impact must still expire normally.");
        }

        [Test]
        public void RedCharger_NewRunnerPreservesGraceAndChargeCancellationStun()
        {
            using var fixture = new BossPhysicsFixture("RedCharger");
            var charger = fixture.Monster.GetComponent<EnemyChargeController>();
            fixture.BeginWarning(false);
            for (int i = 0; i < 200 && fixture.Runner.Phase != EnemyActionPhase.ImpactGrace; i++) fixture.Step();
            Assert.That(charger.IsImpactGrace, Is.True);
            Assert.That(charger.IsChargeCancelable, Is.True);
            var result = charger.CancelChargeAndApplyStun(.4f);
            Assert.That(result.ChargeCancelled, Is.True);
            Assert.That(result.StunApplied, Is.True);
            Assert.That(fixture.Runner.Phase, Is.EqualTo(EnemyActionPhase.Idle));
            Assert.That(charger.IsChargeCancelable, Is.False);
            Vector2 position = fixture.Body.position;
            for (int i = 0; i < 10; i++) fixture.Step();
            Assert.That(Vector2.Distance(position, fixture.Body.position), Is.LessThan(.005f));
            for (int i = 0; i < 20; i++) fixture.Step();
            Assert.That(Vector2.Distance(position, fixture.Body.position), Is.GreaterThan(.01f));
        }

        [Test]
        public void WolfContact_PreemptsNewDashWithoutLeavingRecoveryLocked()
        {
            using var fixture = new BossPhysicsFixture("HungryWolf");
            fixture.BeginWarning(false);
            fixture.Player.transform.position = fixture.Monster.CombatCollider.bounds.center;
            Physics2D.SyncTransforms();
            fixture.Step();
            Assert.That(fixture.Runner.Phase, Is.EqualTo(EnemyActionPhase.Idle));
            Assert.That(fixture.Monster.CreatureState, Is.EqualTo(Define.CreatureState.Skill));
            Assert.That(fixture.Monster.GetComponent<EnemyChargeController>().IsCharging, Is.False);
        }

        [TestCase("RedCharger")]
        [TestCase("HungryWolf")]
        public void PatternWarning_KnockbackDoesNotCancelCommittedAttack(string prefab)
        {
            using var fixture = new BossPhysicsFixture(prefab);
            fixture.BeginWarning(false);
            fixture.Monster.ApplySmoothKnockback(Vector3.left, 1f, .4f);
            fixture.Step();
            Assert.That(fixture.Runner.Phase, Is.EqualTo(EnemyActionPhase.Warning));
            Assert.That(fixture.Body.position.x, Is.LessThan(-.005f));
        }

        [TestCase("HungryGiant", 2.905f)]
        [TestCase("RedCharger", 4.055f)]
        [TestCase("HungryWolf", 1.38f)]
        public void ChargeSelection_ChasesOutsideRangeAndStartsInside(string prefab, float expectedMaximum)
        {
            using var fixture = new BossPhysicsFixture(prefab);
            var definitions = (EnemyAttackDefinition[])typeof(EnemyActionRunner)
                .GetField("_attacks", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fixture.Runner);
            Assert.That(definitions[0].Kind, Is.EqualTo(EnemyAttackKind.Charge));
            float maximum = definitions[0].MaximumRange;
            Assert.That(maximum, Is.EqualTo(expectedMaximum).Within(.001f));
            Assert.That(maximum, Is.LessThan(10f));
            Assert.That(maximum, Is.GreaterThanOrEqualTo(definitions[0].MinimumRange));
            var cooldowns = (float[])typeof(EnemyActionRunner).GetField("_cooldowns", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fixture.Runner);
            cooldowns[0] = 0f;
            var outside = new EnemyActionInput(Vector2.zero, Vector2.right * (maximum + .01f), true, false, false);
            var frame = fixture.Runner.Advance(.02f, outside);
            Assert.That(frame.Phase, Is.EqualTo(EnemyActionPhase.Idle));
            Assert.That(frame.Velocity.x, Is.GreaterThan(0f));
            Assert.That(fixture.Runner.TryStartAttack(EnemyAttackKind.Charge, outside, out _), Is.False);
            var inside = new EnemyActionInput(Vector2.zero, Vector2.right * (maximum - .01f), true, false, false);
            frame = fixture.Runner.Advance(.02f, inside);
            Assert.That(frame.Kind, Is.EqualTo(EnemyAttackKind.Charge));
            Assert.That(frame.Phase, Is.EqualTo(EnemyActionPhase.Warning));
        }

        [Test]
        public void HungryGiant_ReadyChargeTakesPriorityOverReadyArea()
        {
            using var fixture = new BossPhysicsFixture();
            var cooldowns = (float[])typeof(EnemyActionRunner).GetField("_cooldowns", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(fixture.Runner);
            cooldowns[0] = cooldowns[1] = 0f;
            var input = new EnemyActionInput(Vector2.zero, Vector2.right * 2f, true, false, false);
            var frame = fixture.Runner.Advance(.02f, input);
            Assert.That(frame.Kind, Is.EqualTo(EnemyAttackKind.Charge));
            Assert.That(frame.Phase, Is.EqualTo(EnemyActionPhase.Warning));
            fixture.Runner.CancelActive();
            frame = fixture.Runner.Advance(.02f, input);
            Assert.That(frame.Kind, Is.EqualTo(EnemyAttackKind.Area), "Charge cooldown leaves room for other attacks.");
        }

        [TestCase("HungryGiant")]
        [TestCase("RedCharger")]
        [TestCase("HungryWolf")]
        public void SharedChargeWarning_IsNestedAndTracksRepeatedPreparation(string prefab)
        {
            const string shared = "Assets/_LizzoPV/Gameplay/Enemies/Prefabs/Common/ChargePathWarning.prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LizzoPV/Gameplay/Enemies/Prefabs/Units/" + prefab + ".prefab");
            var authoredWarning = asset.transform.Find("ChargePathWarning");
            Assert.That(authoredWarning, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(authoredWarning.gameObject)), Is.EqualTo(shared));
            using var fixture = new BossPhysicsFixture(prefab);
            var warning = fixture.Monster.transform.Find("ChargePathWarning").GetComponent<SpriteRenderer>();
            for (int cycle = 0; cycle < 3; cycle++)
            {
                fixture.BeginWarning(false);
                fixture.Step();
                Assert.That(warning.enabled && warning.gameObject.activeInHierarchy, Is.True, $"{prefab} cycle {cycle}");
                Assert.That(warning.bounds.size.x, Is.GreaterThan(0f));
                Assert.That(warning.bounds.size.y, Is.GreaterThan(0f));
                for (int step = 0; step < 100 && fixture.Runner.Phase == EnemyActionPhase.Warning; step++) fixture.Step();
                Assert.That(warning.enabled, Is.False, "Warning ends when execution starts.");
                for (int step = 0; step < 200 && fixture.Runner.Phase != EnemyActionPhase.Idle; step++) fixture.Step();
                Assert.That(fixture.Runner.Phase, Is.EqualTo(EnemyActionPhase.Idle));
            }
            fixture.BeginWarning(false);
            fixture.Step();
            fixture.Monster.RestoreHealth(0);
            fixture.Step();
            Assert.That(warning.enabled, Is.False, "Death must clear the warning.");
        }

        // Runs the production prefab and physics in an additive local scene, never the open Gameplay scene.
        // Reflection drives Unity's private callback and selects a pattern; assertions observe position/HP/constraints.
        private sealed class BossPhysicsFixture : System.IDisposable
        {
            private readonly Lizzo.PV.Tests.Support.ServiceTestFixture _services = new();
            private readonly UnityEngine.SceneManagement.Scene _scene;
            private readonly PhysicsScene2D _physics;
            private readonly EnemyBossController _boss;
            private readonly MonoBehaviour _behaviour;
            private readonly Rigidbody2D _pusher;
            private readonly MethodInfo _fixedUpdate;
            public EnemyActor Monster { get; }
            public Lizzo.PV.Combat.ICombatImmediateHitModule ImmediateHits => _services.Run.ImmediateHitModule;
            public Rigidbody2D Body { get; }
            public CommanderActor Player { get; }
            public RigidbodyConstraints2D BaseConstraints { get; }
            private object Adapter => _behaviour;
            public EnemyActionRunner Runner => _boss != null
                ? _boss.EditorRunner
                : ((EnemyChargeController)_behaviour).EditorRunner;

            public BossPhysicsFixture(string prefab = "HungryGiant")
            {
                RunTelemetry.BeginRun();
                _scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
                _physics = _scene.GetPhysicsScene2D();
                Assert.That(_physics.Equals(Physics2D.defaultPhysicsScene), Is.False);
                FloatingDamageText.Configure(new PhysicsFeedbackFactory(_scene));
                var data = _services.Data.GetEnemy(prefab == "HungryGiant" ? "boss_hungry_giant" : prefab == "RedCharger" ? "red_charger" : "hungry_wolf");
                data.Attack = 10;
                data.MoveSpeed = 1.2f;
                data.ChargeCooldown = 10000f;
                data.ChargeDuration = .9f;
                data.ContactRange = prefab == "HungryWolf" ? .8f : prefab == "RedCharger" ? 1f : 2.2f;
                GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_LizzoPV/Gameplay/Enemies/Prefabs/Units/" + prefab + ".prefab"));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, _scene);
                Monster = root.GetComponent<EnemyActor>();
                _services.Run.BindEnemy(Monster);
                Monster.Init();
                Body = root.GetComponent<Rigidbody2D>();
                BaseConstraints = Body.constraints;
                _boss = root.GetComponent<EnemyBossController>();
                GameObject playerRoot = new("BossPhysicsPlayer");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(playerRoot, _scene);
                playerRoot.AddComponent<SpriteRenderer>();
                playerRoot.AddComponent<HitFlash>();
                CircleCollider2D hurtbox = playerRoot.AddComponent<CircleCollider2D>();
                hurtbox.isTrigger = true;
                hurtbox.radius = 0.2f;
                Player = playerRoot.AddComponent<CommanderActor>();
                _services.Run.BindCommander(Player);
                Player.ResetHealth(100000);
                SetFixtureField(Player, "_combatCollider", hurtbox);
                _services.Run.Registry.RegisterPlayer(Player);
                if (_boss != null) { _boss.Setup(Monster); _behaviour = _boss; }
                else { var charge = root.GetComponent<EnemyChargeController>(); charge.Setup(Monster); _behaviour = charge; }
                _fixedUpdate = _behaviour.GetType().GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
                GameObject pushRoot = new("BossPhysicsPusher");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(pushRoot, _scene);
                _pusher = pushRoot.AddComponent<Rigidbody2D>();
                pushRoot.layer = Monster.BodyCollider.gameObject.layer;
                _pusher.gravityScale = 0f;
                _pusher.mass = 10f;
                pushRoot.AddComponent<CircleCollider2D>().radius = 0.5f;
                ResetPositions();
            }

            public void ResetPositions()
            {
                Body.position = Vector2.zero;
                Body.linearVelocity = Vector2.zero;
                Player.transform.position = new Vector3(10, 0, 0);
                _pusher.position = new Vector2(-20, 0);
                _pusher.linearVelocity = Vector2.zero;
                Monster.CreatureState = Define.CreatureState.Moving;
                Runner.Reset();
                // Isolate one requested action from a different already-ready attack.
                var cooldowns = (float[])typeof(EnemyActionRunner).GetField("_cooldowns", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Runner);
                cooldowns[0] = 10000f;
                if (_boss != null) cooldowns[1] = 10000f;
                // Each repetition represents a fresh damage window, independent of wall-clock test speed.
                SetFixtureField(Player, "_damageReceiver", null);
                Physics2D.SyncTransforms();
            }

            public void BeginWarning(bool aoe)
            {
                var input = new EnemyActionInput(Body.position, Player.transform.position, true, false, false);
                var frame = Runner.StartAttack(aoe ? EnemyAttackKind.Area : EnemyAttackKind.Charge, input);
                if (_boss != null)
                    Adapter.GetType().GetMethod("ApplyFrame", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Adapter, new object[] { frame, 0f });
                else Adapter.GetType().GetMethod("Present", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Adapter, new object[] { frame });
            }

            public void PushAgainstBoss()
            {
                Bounds bounds = Monster.BodyCollider.bounds;
                _pusher.position = new Vector2(bounds.min.x - 0.4f, bounds.center.y);
                _pusher.linearVelocity = Vector2.right * 4f;
            }

            public void Step()
            {
                _fixedUpdate.Invoke(_behaviour, null);
                _physics.Simulate(Time.fixedDeltaTime);
            }

            public void StepLegacyMovement()
            {
                typeof(EnemyActor).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(Monster, null);
                _physics.Simulate(Time.fixedDeltaTime);
            }

            public void FinishAttack(bool aoe)
            {
                BeginWarning(aoe);
                for (int i=0;i<300 && Runner.Phase != EnemyActionPhase.Recovery;i++) Step();
                Assert.That(Runner.Phase, Is.EqualTo(EnemyActionPhase.Recovery));
            }

            public void SimulatePressureOnly()
            {
                PushAgainstBoss();
                _physics.Simulate(Time.fixedDeltaTime);
            }

            public void Dispose()
            {
                FloatingDamageText.ClearServices();
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(_scene);
                _services.Dispose();
            }
        }

        private sealed class PhysicsFeedbackFactory : IPrefabFactory
        {
            private readonly UnityEngine.SceneManagement.Scene _scene;
            public PhysicsFeedbackFactory(UnityEngine.SceneManagement.Scene scene) { _scene = scene; }
            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                GameObject instance = new(address);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, _scene);
                instance.AddComponent<TMPro.TextMeshPro>();
                instance.AddComponent<FloatingDamageText>();
                return instance;
            }
            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null) => Spawn(poolKey, parent, true);
            public void Release(GameObject instance) { if (instance != null) instance.SetActive(false); }
            public void Clear() { }
        }

        private static int CountOccurrences(string source, string value)
        {
            int count = 0;
            int index = 0;
            while ((index = source.IndexOf(value, index, System.StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }
            return count;
        }
    }
}
