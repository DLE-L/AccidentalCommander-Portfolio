using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Lizzo.PV.Flow;
using Lizzo.PV.Data;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Combat;
using Lizzo.PV.P0.Presentation;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Tests.Support;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lizzo.PV.Tests.EditMode
{
    public sealed class SwordSoldierMeleeExcursionTests
    {
        private const float TrackingDistance = 2.1f;
        private const float ReturnTolerance = 0.1f;
        private const int FixedStepBudget = 120;

        [UnitySetUp]
        public IEnumerator EnterPlayModeForRuntimeMovement()
        {
            yield return new EnterPlayMode();
        }

        [UnityTearDown]
        public IEnumerator ExitPlayModeAfterRuntimeMovement()
        {
            Time.timeScale = 1.0f;
            if (EditorApplication.isPlaying)
                yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator BaseSwordSoldier_ExcursionsReturnWithoutDriftAndMovementAuthorityIsExclusive()
        {
            using RuntimeFixture fixture = new RuntimeFixture();
            fixture.SetTimeScale(10.0f);

            int castCount = 0;
            int[] castTargetIds = new int[2];
            long[] castIds = new long[2];
            int[] targetHpAfterCallbacks = new int[2];
            float[] attackDistances = new float[2];
            int swordCombatComponentCount = fixture.SwordRuntime.GetComponents<AllyCombat>().Length;
            Action<CanonicalCompanionCastCompleted> onCast = cast =>
            {
                if (cast.OwnerInstanceId != fixture.SwordRuntime.GetInstanceID()
                    || cast.ActionKind != CanonicalCompanionActionKind.BasicAttack)
                {
                    return;
                }

                int castIndex = castCount;
                castCount++;
                if (castIndex >= castTargetIds.Length)
                    return;

                castIds[castIndex] = cast.CastId;
                targetHpAfterCallbacks[castIndex] = fixture.Target.Hp;
                castTargetIds[castIndex] = fixture.LockedTargetInstanceId;
                attackDistances[castIndex] = Vector2.Distance(
                    fixture.SwordBody.position,
                    fixture.PreferredAnchor);
                fixture.DelayNextAttack();

                // Return must resolve against the latest anchor, not the excursion's starting anchor.
                fixture.MoveCommander(castIndex == 0 ? Vector2.up * 0.4f : Vector2.down * 0.3f);
            };
            fixture.CastStream.Completed += onCast;

            try
            {
                int firstTargetHp = fixture.Target.Hp;
                int firstLockId = fixture.BeginExcursion();
                Assert.That(firstLockId, Is.EqualTo(fixture.Target.GetInstanceID()), "The first excursion must lock the eligible target once.");
                Assert.That(fixture.PhaseName, Is.EqualTo("Approaching"));
                Assert.That(fixture.AuthorityName, Is.EqualTo("SwordSoldierMeleeExcursion"));
                Assert.That(fixture.TargetDistanceFromPreferredAnchor, Is.EqualTo(TrackingDistance).Within(0.001f));
                Assert.That(fixture.TryAcquireBeastHuntAuthority(), Is.False, "BeastHunt cannot acquire movement while the sword excursion owns it.");
                Assert.That(fixture.AuthorityName, Is.EqualTo("SwordSoldierMeleeExcursion"));

                bool firstLockStayedStable = true;
                yield return WaitForReturn(
                    fixture,
                    () => (castCount == 1, castCount),
                    () => $"castIds=[{castIds[0]},{castIds[1]}]; "
                        + $"targetHpAfterCallbacks=[{targetHpAfterCallbacks[0]},{targetHpAfterCallbacks[1]}]; "
                        + $"combatComponentCount={swordCombatComponentCount}",
                    firstLockId,
                    stable => firstLockStayedStable &= stable);

                float firstReturnDistance = fixture.DistanceFromPreferredAnchor;
                Assert.That(firstLockStayedStable, Is.True, "The first excursion must not retarget.");
                Assert.That(castCount, Is.EqualTo(1), "The existing ForwardSlash must resolve exactly once in the first excursion.");
                Assert.That(castTargetIds[0], Is.EqualTo(firstLockId));
                Assert.That(firstTargetHp - fixture.Target.Hp, Is.EqualTo(12));
                Assert.That(attackDistances[0], Is.GreaterThan(0.01f), "The sword soldier must approach before resolving ForwardSlash.");
                Assert.That(firstReturnDistance, Is.LessThanOrEqualTo(ReturnTolerance), "The first return must use the latest preferred anchor.");

                fixture.ResetTarget();
                int secondTargetHp = fixture.Target.Hp;
                int secondLockId = fixture.BeginExcursion();
                Assert.That(secondLockId, Is.EqualTo(fixture.Target.GetInstanceID()), "The second excursion must lock the fixed target once.");

                bool secondLockStayedStable = true;
                yield return WaitForReturn(
                    fixture,
                    () => (castCount == 2, castCount),
                    () => $"castIds=[{castIds[0]},{castIds[1]}]; "
                        + $"targetHpAfterCallbacks=[{targetHpAfterCallbacks[0]},{targetHpAfterCallbacks[1]}]; "
                        + $"combatComponentCount={swordCombatComponentCount}",
                    secondLockId,
                    stable => secondLockStayedStable &= stable);

                float secondReturnDistance = fixture.DistanceFromPreferredAnchor;
                Assert.That(secondLockStayedStable, Is.True, "The second excursion must not retarget.");
                Assert.That(castCount, Is.EqualTo(2), "Each completed excursion must emit one and only one ForwardSlash cast.");
                Assert.That(castTargetIds[1], Is.EqualTo(secondLockId));
                Assert.That(secondTargetHp - fixture.Target.Hp, Is.EqualTo(12));
                Assert.That(attackDistances[1], Is.GreaterThan(0.01f));
                Assert.That(secondReturnDistance, Is.LessThanOrEqualTo(ReturnTolerance), "Two excursions must not accumulate positional drift.");

                Assert.That(fixture.TryAcquireBeastHuntAuthority(), Is.True);
                Assert.That(fixture.TryAcquireSwordExcursionAuthority(), Is.False, "Sword movement cannot acquire while BeastHunt owns movement.");
                Assert.That(fixture.AuthorityName, Is.EqualTo("BeastHunt"));
                fixture.ReleaseBeastHuntAuthority();
                Assert.That(fixture.AuthorityName, Is.EqualTo("Formation"));

                fixture.ResetTarget();
                fixture.BeginExcursion();
                fixture.DisplaceSword(Vector2.left * 0.35f);
                fixture.Target.Hp = 0;
                fixture.AdvanceCombat();
                Assert.That(fixture.PhaseName, Is.EqualTo("Returning"), "An invalid locked target must cancel into return.");
                Assert.That(fixture.AuthorityName, Is.EqualTo("SwordSoldierMeleeExcursion"));
                yield return WaitForFormationReturn(fixture);
                Assert.That(fixture.DistanceFromPreferredAnchor, Is.LessThanOrEqualTo(ReturnTolerance));
                Assert.That(castCount, Is.EqualTo(2), "Invalid-target cancellation must not attack.");

                fixture.ResetTarget();
                fixture.BeginExcursion();
                fixture.DisplaceSword(Vector2.left * 0.3f);
                fixture.Pause();
                fixture.AdvanceCombat();
                Assert.That(fixture.AuthorityName, Is.EqualTo("Formation"), "Pause cancellation must restore formation ownership immediately.");
                Assert.That(fixture.PhaseName, Is.EqualTo("Idle"));
                fixture.DelayNextAttack();
                fixture.Target.Hp = 0;
                fixture.Resume(10.0f);
                yield return WaitForFormationReturn(fixture);
                Assert.That(fixture.DistanceFromPreferredAnchor, Is.LessThanOrEqualTo(ReturnTolerance));
                Assert.That(castCount, Is.EqualTo(2), "Pause cancellation must not attack.");
            }
            finally
            {
                fixture.CastStream.Completed -= onCast;
            }
        }

        private static IEnumerator WaitForReturn(
            RuntimeFixture fixture,
            Func<(bool Observed, int Count)> observeCast,
            Func<string> describeCastDiagnostics,
            int expectedLockId,
            Action<bool> recordLockStability)
        {
            int finalStep = -1;
            int returnFirstStep = -1;
            float returnStartAnchorDistance = float.NaN;
            Vector2 priorSwordPosition = fixture.SwordBody.position;
            float priorAnchorDistance = Vector2.Distance(priorSwordPosition, fixture.PreferredAnchor);
            for (int step = 0; step < FixedStepBudget; step++)
            {
                finalStep = step;
                priorSwordPosition = fixture.SwordBody.position;
                priorAnchorDistance = Vector2.Distance(priorSwordPosition, fixture.PreferredAnchor);
                if (returnFirstStep < 0 && fixture.PhaseName == "Returning")
                {
                    returnFirstStep = step;
                    returnStartAnchorDistance = priorAnchorDistance;
                }

                if (fixture.AuthorityName == "SwordSoldierMeleeExcursion"
                    && fixture.PhaseName == "Approaching"
                    && fixture.LockedTargetInstanceId != 0)
                {
                    recordLockStability(fixture.LockedTargetInstanceId == expectedLockId);
                }

                (bool Observed, int Count) castObservation = observeCast();
                if (castObservation.Observed
                    && fixture.AuthorityName == "Formation"
                    && fixture.PhaseName == "Idle")
                {
                    yield return new WaitForFixedUpdate();
                    yield break;
                }

                yield return new WaitForFixedUpdate();
            }

            Vector2 swordPosition = fixture.SwordBody.position;
            Vector2 preferredAnchor = fixture.PreferredAnchor;
            float anchorDistance = Vector2.Distance(swordPosition, preferredAnchor);
            Collider2D targetCollider = fixture.Target.CombatCollider;
            Vector2 resolvedTargetPoint = targetCollider != null && targetCollider.enabled
                ? targetCollider.ClosestPoint(swordPosition)
                : (Vector2)fixture.Target.transform.position;
            int castCount = observeCast().Count;
            string nonTriggerContactColliderInstanceIds = ResolveNonTriggerContactColliderInstanceIds(fixture.SwordBody);
            Assert.Fail(
                $"Sword excursion did not return within the fixed-step budget. "
                + $"finalStep={finalStep}; castCount={castCount}; phase={fixture.PhaseName}; "
                + $"authority={fixture.AuthorityName}; lockedTargetInstanceId={fixture.LockedTargetInstanceId}; "
                + $"{describeCastDiagnostics()}; "
                + $"returnFirstStep={returnFirstStep}; returnStartAnchorDistance={returnStartAnchorDistance:F6}; "
                + $"priorSwordPosition={priorSwordPosition.ToString("F6")}; priorAnchorDistance={priorAnchorDistance:F6}; "
                + $"swordPosition={swordPosition.ToString("F6")}; preferredAnchor={preferredAnchor.ToString("F6")}; "
                + $"resolvedTargetPointDistance={Vector2.Distance(swordPosition, resolvedTargetPoint):F6}; "
                + $"anchorDistance={anchorDistance:F6}; linearVelocity={fixture.SwordBody.linearVelocity.ToString("F6")}; "
                + $"nonTriggerContactColliderInstanceIds={nonTriggerContactColliderInstanceIds}.");
        }

        private static string ResolveNonTriggerContactColliderInstanceIds(Rigidbody2D body)
        {
            if (body == null)
                return "[]";

            List<ContactPoint2D> contacts = new List<ContactPoint2D>();
            body.GetContacts(contacts);
            List<int> colliderInstanceIds = new List<int>();
            for (int i = 0; i < contacts.Count; i++)
            {
                Collider2D otherCollider = contacts[i].otherCollider;
                if (otherCollider == null || otherCollider.isTrigger)
                    continue;

                int instanceId = otherCollider.GetInstanceID();
                if (colliderInstanceIds.Contains(instanceId) == false)
                    colliderInstanceIds.Add(instanceId);
            }

            return colliderInstanceIds.Count == 0
                ? "[]"
                : $"[{string.Join(",", colliderInstanceIds)}]";
        }

        private static IEnumerator WaitForFormationReturn(RuntimeFixture fixture)
        {
            for (int step = 0; step < FixedStepBudget; step++)
            {
                if (fixture.AuthorityName == "Formation"
                    && fixture.PhaseName == "Idle"
                    && fixture.DistanceFromPreferredAnchor <= ReturnTolerance)
                {
                    yield break;
                }

                yield return new WaitForFixedUpdate();
            }

            Assert.Fail("Formation ownership was restored, but the sword soldier did not return within the fixed-step budget.");
        }

        private sealed class RuntimeFixture : IDisposable
        {
            private const string CommanderControllerPath = "Assets/_LizzoPV/Animations/Units/Commander.controller";
            private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            private static readonly FieldInfo ActiveProvider = typeof(PresentationCatalogProvider).GetField("_active", BindingFlags.Static | BindingFlags.NonPublic);

            private readonly GameObject _root = new GameObject("SwordSoldierMeleeExcursionFixture");
            private readonly PresentationCatalogProvider _previousProvider;
            private readonly PresentationCatalog _catalog;
            private readonly RuntimeAnimatorController _visualController;
            private readonly GameObject _providerRoot;
            private readonly TestFactory _factory = new TestFactory();
            private readonly RunServices _run;
            private readonly RunPauseController _pause;
            private readonly FieldInfo _nextAttackTime;
            private readonly FieldInfo _excursion;
            private readonly FieldInfo _movementAuthority;
            private readonly MethodInfo _getPreferredAnchor;
            private readonly MethodInfo _tryAcquireAuthority;
            private readonly MethodInfo _releaseAuthority;
            private readonly object _beastHuntAuthority;
            private readonly object _swordExcursionAuthority;

            public RuntimeFixture()
            {
                _root.SetActive(false);
                _previousProvider = ActiveProvider.GetValue(null) as PresentationCatalogProvider;
                _visualController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CommanderControllerPath);
                Assert.That(_visualController, Is.Not.Null);
                UnitPresentationSet units = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Gameplay/Presentation/Data/UnitPresentationSet.asset");
                _catalog = ScriptableObject.CreateInstance<PresentationCatalog>();
                _catalog.SetPresentationSetsForEditor(null, units);
                _providerRoot = new GameObject("SwordExcursionCatalog");
                _providerRoot.SetActive(false);
                PresentationCatalogProvider provider = _providerRoot.AddComponent<PresentationCatalogProvider>();
                SerializedObject serializedProvider = new SerializedObject(provider);
                serializedProvider.FindProperty("_catalog").objectReferenceValue = _catalog;
                serializedProvider.ApplyModifiedPropertiesWithoutUndo();
                ActiveProvider.SetValue(null, provider);

                TestAssetService assets = new TestAssetService();
                assets.Register("PlayerData.xml", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/_LizzoPV/Gameplay/Run/Data/GameData.xml"));
                LocalDataProvider data = new LocalDataProvider(assets);
                Assert.That(data.InitializeAsync().GetAwaiter().GetResult().Succeeded, Is.True);
                AppServices app = new AppServices(assets, data);
                RuntimeObjectRegistry registry = new RuntimeObjectRegistry(_factory);
                Transform poolRoot = new GameObject("SwordExcursionPool").transform;
                poolRoot.SetParent(_root.transform, false);
                _run = new RunServices(app, new Lizzo.PV.Flow.RunState(), registry, new ObjectPoolService(poolRoot), _factory);
                RetroSfx.Configure(assets);
                RetroVfx.Configure(assets, _factory);
                AttackVisual.Configure(_factory);
                FloatingDamageText.Configure(_factory);

                Rigidbody2D playerBody = _root.AddComponent<Rigidbody2D>();
                playerBody.gravityScale = 0.0f;
                CircleCollider2D playerBodyCollider = _root.AddComponent<CircleCollider2D>();
                CircleCollider2D playerCombatCollider = _root.AddComponent<CircleCollider2D>();
                playerCombatCollider.isTrigger = true;
                GameObject playerVisual = CreateChild(_root.transform, "Visual").gameObject;
                SpriteRenderer playerRenderer = playerVisual.AddComponent<SpriteRenderer>();
                Animator playerAnimator = playerVisual.AddComponent<Animator>();
                playerAnimator.runtimeAnimatorController = _visualController;
                UnitVisualDriver playerDriver = playerVisual.AddComponent<UnitVisualDriver>();
                typeof(UnitVisualDriver).GetField("_spriteRenderer", Flags).SetValue(playerDriver, playerRenderer);
                typeof(UnitVisualDriver).GetField("_animator", Flags).SetValue(playerDriver, playerAnimator);
                _root.AddComponent<HitFlash>();
                _root.AddComponent<CommanderAllyVisual>();
                _root.AddComponent<CommanderHealthBar>();
                Transform commanderHealthBar = CreateChild(_root.transform, "P0_CommanderHPBar");
                CreateChild(commanderHealthBar, "Background").gameObject.AddComponent<SpriteRenderer>();
                CreateChild(commanderHealthBar, "Fill").gameObject.AddComponent<SpriteRenderer>();
                CreateChild(commanderHealthBar, "Text").gameObject.AddComponent<TextMeshPro>();
                Transform indicator = CreateChild(_root.transform, "@Indicator");
                Transform fireSocket = CreateChild(_root.transform, "@FireSocket");
                Player = _root.AddComponent<PlayerController>();
                typeof(PlayerController).GetField("_bodyCollider", Flags).SetValue(Player, playerBodyCollider);
                typeof(PlayerController).GetField("_combatCollider", Flags).SetValue(Player, playerCombatCollider);
                typeof(PlayerController).GetField("_indicator", Flags).SetValue(Player, indicator);
                typeof(PlayerController).GetField("_fireSocket", Flags).SetValue(Player, fireSocket);
                Player.Initialize(_run);
                Player.MaxHp = 100;
                Player.Hp = 100;
                registry.RegisterPlayer(Player);
                _pause = _root.AddComponent<RunPauseController>();
                _pause.Initialize();
                _root.SetActive(true);

                _run.Party.Recruit(CompanionKind.Swordsman);
                foreach (GameObject instance in _factory.Live)
                {
                    CompanionRuntime runtime = instance == null ? null : instance.GetComponent<CompanionRuntime>();
                    if (runtime != null && runtime.BaseUnitId == "sword_soldier" && runtime.IsPromoted == false)
                    {
                        SwordRuntime = runtime;
                        break;
                    }
                }

                Assert.That(SwordRuntime, Is.Not.Null);
                Follower = SwordRuntime.GetComponent<AllyFollower>();
                Combat = SwordRuntime.GetComponent<AllyCombat>();
                SwordBody = SwordRuntime.GetComponent<Rigidbody2D>();
                Assert.That(Follower, Is.Not.Null);
                Assert.That(Combat, Is.Not.Null);
                Assert.That(SwordBody, Is.Not.Null);
                Assert.That(Combat.AttackStyle, Is.EqualTo(AllyAttackStyle.ForwardSlash));
                Assert.That(Combat.Damage, Is.EqualTo(12));
                Assert.That(Combat.AttackPeriod, Is.EqualTo(1.0f));
                Assert.That(Combat.AttackRange, Is.EqualTo(1.1f));
                Assert.That(Combat.AttackAngle, Is.EqualTo(60.0f));
                Assert.That(Combat.MaxForwardTargetCount, Is.EqualTo(3));
                Assert.That(Combat.CombatSourceId, Is.EqualTo("sword_soldier"));

                _nextAttackTime = typeof(AllyCombat).GetField("_nextAttackTime", Flags);
                _excursion = typeof(AllyCombat).GetField("_swordSoldierMeleeExcursion", Flags);
                _movementAuthority = typeof(AllyFollower).GetField("_movementAuthority", Flags);
                _getPreferredAnchor = typeof(AllyFollower).GetMethod("TryGetPreferredFormationAnchor", Flags);
                _tryAcquireAuthority = typeof(AllyFollower).GetMethod("TryAcquireMovementAuthority", Flags);
                _releaseAuthority = typeof(AllyFollower).GetMethod("ReleaseMovementAuthority", Flags);
                Assert.That(_nextAttackTime, Is.Not.Null, "RED seam: pre-slice combat had no sword excursion schedule owner.");
                Assert.That(_excursion, Is.Not.Null, "RED seam: pre-slice combat had no SwordSoldierMeleeExcursion state.");
                Assert.That(_movementAuthority, Is.Not.Null, "RED seam: pre-slice follower had no exclusive movement authority.");
                Assert.That(_getPreferredAnchor, Is.Not.Null);
                Assert.That(_tryAcquireAuthority, Is.Not.Null);
                Assert.That(_releaseAuthority, Is.Not.Null);
                _beastHuntAuthority = Enum.Parse(_movementAuthority.FieldType, "BeastHunt");
                _swordExcursionAuthority = Enum.Parse(_movementAuthority.FieldType, "SwordSoldierMeleeExcursion");

                SnapSword(PreferredAnchor);
                Target = CreateTarget();
                ResetTarget();
            }

            public PlayerController Player { get; }
            public CompanionRuntime SwordRuntime { get; }
            public AllyFollower Follower { get; }
            public AllyCombat Combat { get; }
            public Rigidbody2D SwordBody { get; }
            public MonsterController Target { get; }
            public CanonicalCompanionCastStream CastStream => _run.CanonicalCompanionCasts;

            public Vector2 PreferredAnchor
            {
                get
                {
                    object[] arguments = { Vector2.zero };
                    Assert.That((bool)_getPreferredAnchor.Invoke(Follower, arguments), Is.True);
                    return (Vector2)arguments[0];
                }
            }

            public float DistanceFromPreferredAnchor => Vector2.Distance(SwordBody.position, PreferredAnchor);
            public float TargetDistanceFromPreferredAnchor => Vector2.Distance(Target.transform.position, PreferredAnchor);
            public string AuthorityName => _movementAuthority.GetValue(Follower).ToString();

            public string PhaseName
            {
                get
                {
                    object state = _excursion.GetValue(Combat);
                    if (state == null)
                        return "Missing";

                    FieldInfo phase = state.GetType().GetField("_phase", Flags);
                    return phase == null ? "Missing" : phase.GetValue(state).ToString();
                }
            }

            public int LockedTargetInstanceId
            {
                get
                {
                    object state = _excursion.GetValue(Combat);
                    if (state == null)
                        return 0;

                    FieldInfo lockedTarget = state.GetType().GetField("_lockedTarget", Flags);
                    MonsterController monster = lockedTarget?.GetValue(state) as MonsterController;
                    return monster == null ? 0 : monster.GetInstanceID();
                }
            }

            public int BeginExcursion()
            {
                DelayNextAttack(0.0f);
                AdvanceCombat();
                return LockedTargetInstanceId;
            }

            public void AdvanceCombat()
            {
                Combat.TryAdvanceCanonicalCastForTests(Time.time);
            }

            public void DelayNextAttack(float delay = 1000.0f)
            {
                _nextAttackTime.SetValue(Combat, Time.time + delay);
            }

            public bool TryAcquireBeastHuntAuthority()
            {
                return (bool)_tryAcquireAuthority.Invoke(Follower, new[] { _beastHuntAuthority });
            }

            public bool TryAcquireSwordExcursionAuthority()
            {
                return (bool)_tryAcquireAuthority.Invoke(Follower, new[] { _swordExcursionAuthority });
            }

            public void ReleaseBeastHuntAuthority()
            {
                _releaseAuthority.Invoke(Follower, new[] { _beastHuntAuthority });
            }

            public void MoveCommander(Vector2 delta)
            {
                Player.transform.position += (Vector3)delta;
                Physics2D.SyncTransforms();
            }

            public void DisplaceSword(Vector2 delta)
            {
                SnapSword(PreferredAnchor + delta);
            }

            public void ResetTarget()
            {
                Target.MaxHp = 100;
                Target.Hp = 100;
                Target.transform.position = PreferredAnchor + Vector2.right * TrackingDistance;
                Physics2D.SyncTransforms();
            }

            public void SetTimeScale(float scale)
            {
                _pause.SetModalOpen(false);
                Time.timeScale = scale;
            }

            public void Pause()
            {
                _pause.SetModalOpen(true);
            }

            public void Resume(float scale)
            {
                _pause.SetModalOpen(false);
                Time.timeScale = scale;
            }

            public void Dispose()
            {
                Time.timeScale = 1.0f;
                _run.Dispose();
                FloatingDamageText.ClearServices();
                AttackVisual.ClearServices();
                RetroVfx.ClearServices();
                RetroSfx.ClearServices();
                if (_root != null)
                    UnityEngine.Object.DestroyImmediate(_root);
                if (_providerRoot != null)
                    UnityEngine.Object.DestroyImmediate(_providerRoot);
                if (_catalog != null)
                    UnityEngine.Object.DestroyImmediate(_catalog);
                ActiveProvider.SetValue(null, _previousProvider);
            }

            private MonsterController CreateTarget()
            {
                GameObject targetObject = new GameObject("SwordExcursionTarget");
                targetObject.SetActive(false);
                targetObject.transform.SetParent(_root.transform, false);
                Rigidbody2D body = targetObject.AddComponent<Rigidbody2D>();
                body.gravityScale = 0.0f;
                body.constraints = RigidbodyConstraints2D.FreezeAll;
                CircleCollider2D bodyCollider = targetObject.AddComponent<CircleCollider2D>();
                bodyCollider.isTrigger = false;
                bodyCollider.radius = 0.16f;
                bodyCollider.offset = new Vector2(0.0f, 0.16f);
                CircleCollider2D combatCollider = targetObject.AddComponent<CircleCollider2D>();
                combatCollider.isTrigger = true;
                combatCollider.radius = 0.16f;
                combatCollider.offset = new Vector2(0.0f, 0.16f);
                UnitColliderRefs colliderRefs = targetObject.AddComponent<UnitColliderRefs>();
                typeof(UnitColliderRefs).GetField("_bodyCollider", Flags).SetValue(colliderRefs, bodyCollider);
                typeof(UnitColliderRefs).GetField("_combatCollider", Flags).SetValue(colliderRefs, combatCollider);
                EnemyHealthBar healthBar = targetObject.AddComponent<EnemyHealthBar>();
                HitFlash hitFlash = targetObject.AddComponent<HitFlash>();
                SpriteRenderer targetRenderer = targetObject.AddComponent<SpriteRenderer>();
                Animator targetAnimator = targetObject.AddComponent<Animator>();
                targetAnimator.runtimeAnimatorController = _visualController;
                UnitVisualDriver targetDriver = targetObject.AddComponent<UnitVisualDriver>();
                typeof(UnitVisualDriver).GetField("_spriteRenderer", Flags).SetValue(targetDriver, targetRenderer);
                typeof(UnitVisualDriver).GetField("_animator", Flags).SetValue(targetDriver, targetAnimator);
                targetObject.AddComponent<PatternEnemyVisual>();
                MonsterController monster = targetObject.AddComponent<MonsterController>();
                typeof(MonsterController).GetField("_healthBar", Flags).SetValue(monster, healthBar);
                typeof(MonsterController).GetField("_hitFlash", Flags).SetValue(monster, hitFlash);
                targetObject.SetActive(true);
                monster.Initialize(_run);
                monster.ResetForSpawn();
                monster.SetExternalMovement(true);
                _run.Registry.RegisterEnemy(monster);
                return monster;
            }

            private static Transform CreateChild(Transform parent, string name)
            {
                GameObject child = new GameObject(name);
                child.transform.SetParent(parent, false);
                return child.transform;
            }

            private void SnapSword(Vector2 position)
            {
                SwordBody.position = position;
                SwordBody.linearVelocity = Vector2.zero;
                SwordBody.angularVelocity = 0.0f;
                SwordRuntime.transform.position = position;
                Physics2D.SyncTransforms();
            }
        }

        private sealed class TestFactory : IPrefabFactory
        {
            public readonly List<GameObject> Live = new List<GameObject>();

            public GameObject Spawn(string address, Transform parent = null, bool pooled = false)
            {
                if (address == "FloatingDamageText.prefab")
                {
                    GameObject floatingText = new GameObject("FloatingDamageText");
                    floatingText.AddComponent<TextMeshPro>();
                    floatingText.AddComponent<FloatingDamageText>();
                    Live.Add(floatingText);
                    return floatingText;
                }

                UnitPresentationSet set = AssetDatabase.LoadAssetAtPath<UnitPresentationSet>("Assets/_LizzoPV/Gameplay/Presentation/Data/UnitPresentationSet.asset");
                string id = address.Substring(address.LastIndexOf('/') + 1);
                if (set == null || set.TryGetEntry(id, out UnitPresentationSet.Entry entry) == false)
                    return null;

                GameObject instance = UnityEngine.Object.Instantiate(entry.Prefab, parent);
                Live.Add(instance);
                return instance;
            }

            public GameObject Rent(GameObject prefab, string poolKey, Transform parent = null)
            {
                return null;
            }

            public void Release(GameObject instance)
            {
                Live.Remove(instance);
                if (instance != null)
                    UnityEngine.Object.DestroyImmediate(instance);
            }

            public void Clear()
            {
                for (int index = Live.Count - 1; index >= 0; index--)
                    Release(Live[index]);
            }
        }
    }
}
