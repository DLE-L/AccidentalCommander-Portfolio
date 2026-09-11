#if UNITY_EDITOR
using System;
using Cysharp.Threading.Tasks;
using Lizzo.PV.Combat;
using Lizzo.PV.Combat.Projectiles;
using Lizzo.PV.Combat.Fields;
using Lizzo.PV.Data;
using Lizzo.PV.Flow;
using Lizzo.PV.Gameplay.Run;
using Lizzo.PV.Gameplay.Units;
using Lizzo.PV.Gameplay.Visuals;
using Lizzo.PV.Gameplay.World;
using Lizzo.PV.Legion;
using Lizzo.PV.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lizzo.PV.Prototypes.Combat
{
    public enum CombatTestScenario { HungryGiant, RedCharger, HungryWolf, SwordSoldier, FalconArcher, EnemyProjectile, Bombardier, FireMage, EnemyField, LightningMage, SkeletonScytheThrower, Cleric, WolfTamer, Necromancer, FieldHerbalist, ShieldGuard, WraithKnight }
    public enum CompanionPromotionTestUnit { ShieldGuard, SwordSoldier, FalconArcher, Bombardier, FireMage, LightningMage, WraithKnight, SkeletonScytheThrower, WolfTamer }

    public enum PatternAttackTestSet { Authored, ContactOnly, ChargeOnly }
    public enum GiantAttackTestSet { Authored, ContactOnly, ChargeAndContact, AreaAndContact, AreaFirst }

    // Editor-only scene harness; all combat uses production services and pooled prefabs.
    public sealed class CombatTestScene : MonoBehaviour
    {
        [SerializeField] private AppBootstrap _app;
        [SerializeField] private Transform _poolRoot;
        [SerializeField] private CameraController _camera;
        [SerializeField] private SafeKnockbackWorld _safeKnockback;
        [SerializeField] private WorldFeedbackProfileSetSO _feedback;
        [SerializeField] private VfxCatalogSO _vfxCatalog;
        [Header("Scenario")]
        [SerializeField] private CombatTestScenario _scenario;
        [SerializeField] private EnemyAttackKind _attack = EnemyAttackKind.Charge;
        [SerializeField] private bool _manualAttacks = true;
        [SerializeField] private bool _infiniteHp = true;
        [SerializeField] private bool _keyboardMovement;
        [SerializeField] private bool _repeat;
        [SerializeField, Min(.1f)] private float _repeatInterval = 1f;
        [SerializeField] private Vector2 _targetPosition = new Vector2(0f, -2f);
        [SerializeField] private Vector2 _enemyPosition = new Vector2(0f, 2f);

        private RunServices _services;
        private CommanderActor _player;
        private EnemyActor _enemy;
        private EnemyBossController _boss;
        private EnemyChargeController _charger;
        private EnemyChargeController _wolf;
        private ArenaBounds _bounds;
        private float _nextRepeatAt;
        private bool _disposed;
        public bool IsCompanionScenario { get; private set; }
        public bool IsEnemyProjectileScenario { get; private set; }
        public bool IsEnemyFieldScenario { get; private set; }
        [SerializeField] private GiantAttackTestSet _giantAttackSet;
        [SerializeField] private PatternAttackTestSet _patternAttackSet;
        [SerializeField] private bool _clericPromoted;
        [SerializeField] private CompanionPromotionTestUnit _promotionTestUnit;
        [SerializeField] private bool _testPromotedCompanion;
        public EnemyActor PromotionRangeTarget { get; private set; }
        [SerializeField, Min(1)] private int _killTargetHp = 1;
        [SerializeField, Min(1)] private int _curseTargetHp = 100;
        [SerializeField, Tooltip("소환 재현 완료 직후 Unity를 일시정지합니다.")]
        private bool _pauseAfterSummonReplay = true;
        public bool SummonReplayRunning { get; private set; }
        public string SummonReplayStatus { get; private set; } = "소환 재현 버튼을 누르세요.";
        private int _summonReplayDeaths;
        private float _summonReplayDeadline;
        public bool IsCurseScenario => _scenario == CombatTestScenario.Necromancer;
        public bool IsSpreadScenario => _scenario == CombatTestScenario.FieldHerbalist;
        public bool IsKillScenario => _scenario == CombatTestScenario.WolfTamer || IsCurseScenario;
        private int KillTargetHp => Mathf.Max(1, IsCurseScenario ? _curseTargetHp : _killTargetHp);
        public int KillCount => _services?.State.KillCount ?? 0;
        public int PendingPackAssaultCount => _services?.ThirdPromotionCombat?.PendingBeastCount ?? 0;
        public int PendingRitualCount => _services?.ThirdPromotionCombat?.PendingRitualCount ?? 0;
        public int RitualSummonCount => _services != null
            && _services.CompanionRuntimeHost.TryGetPromotedRepresentative("necromancer", 0, out var representative)
                ? _services.PersonalSummonModule.GetActiveCount(representative.RosterSlotId, "necromancer:dark_ritualist_undead_ritual") : 0;
        private EnemyAttackLoadout _patternLoadout;
        private EnemyAttackKind[] _authoredPatternAttacks;
        private EnemyAttackLoadout _testLoadout;
        private EnemyAttackKind[] _authoredGiantAttacks;
        private readonly System.Collections.Generic.List<EnemyActor> _chainTestTargets = new System.Collections.Generic.List<EnemyActor>(4);
        public System.Collections.Generic.IReadOnlyList<EnemyActor> ChainTestTargets => _chainTestTargets;
        private readonly System.Collections.Generic.List<EnemyActor> _spreadTestTargets = new System.Collections.Generic.List<EnemyActor>(3);
        public System.Collections.Generic.IReadOnlyList<EnemyActor> SpreadTestTargets => _spreadTestTargets;
        public int ActiveFieldCount => _services?.PersistentFieldModule?.ActiveFieldCount ?? 0;
        public bool SanctuaryActive => _services?.FirstPromotionCombat?.IsSanctuaryActive(Time.time) ?? false;
        public float SanctuaryPartySpeed => _player == null ? 1f
            : _services?.FirstPromotionCombat?.GetSanctuaryAttackIntervalDivisor(_player.transform.position, Time.time) ?? 1f;
        public int EnemyProjectileShotCount { get; private set; }
        public string CompanionLabel { get; private set; } = string.Empty;
        public int CompanionCastCount => _services?.CompanionRuntimeHost?.EmittedCanonicalCastCount ?? 0;
        public bool IsReady { get; private set; }
        private float _cursePullReplayAt = -1f;
        public string CursePullReplayStatus { get; private set; } = "다중 당김 재현 버튼을 누르세요.";
        public string Status { get; private set; } = "Enter Play Mode to initialize.";
        public CommanderActor Player => _player;
        public EnemyActor Enemy => _enemy;
        public EnemyActionFrame ActionFrame => _boss != null ? _boss.EditorActionFrame
            : _charger != null ? _charger.EditorActionFrame : _wolf != null ? _wolf.EditorActionFrame : default;

        private void Start() => InitializeAsync().Forget();

        private async UniTaskVoid InitializeAsync()
        {
            try
            {
                if (_app == null || !_app.IsReady || _poolRoot == null || _camera == null
                    || _safeKnockback == null || _feedback == null)
                    throw new InvalidOperationException("CombatTestScene requires its authored service references.");
                // No GameScene, result coordinator or run outcome: tests cannot settle account rewards.
                if (FindFirstObjectByType<GameScene>() != null)
                    throw new InvalidOperationException("Open CombatTest as the only gameplay scene.");
                Status = "Loading production resources...";
                var cancellation = this.GetCancellationTokenOnDestroy();
                if (!await RunStartupResourceLoader.PrepareAsync(_app.Services, cancellation))
                    throw new InvalidOperationException("Combat test resource loading failed.");
                if (await _app.Services.Assets.LoadAsync<GameObject>("vfx/dot_fire_field_v1", cancellation) == null)
                    throw new InvalidOperationException("Combat test requires the authored fire-field prefab.");
                cancellation.ThrowIfCancellationRequested();
                var pool = new ObjectPoolService(_poolRoot);
                var factory = new PrefabFactory(_app.Services.Assets, pool);
                var registry = new RuntimeObjectRegistry(factory);
                _services = new RunServices(_app.Services, new RunState(), registry, pool, factory,
                    RunContext.Normal, _safeKnockback, _feedback);
                _services.WorldFeedback.Sink.StatusPresented += PresentStatusReaction;
                RetroVfx.Configure(_services.App.Assets, factory);
                FloatingDamageText.Configure(factory, _feedback.WorldUiProfile.ConsecutiveDamageMergeWindowSeconds);
                var map = factory.Spawn("Map_01.prefab");
                _bounds = map == null ? null : map.GetComponent<ArenaBounds>();
                if (_bounds == null) throw new InvalidOperationException("Production map requires ArenaBounds.");
                _camera.Initialize(_services);
                _camera.BindArenaBounds(_bounds);
                IsReady = true;
                ResetScenario();
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                Status = exception.Message;
                Debug.LogException(exception, this);
                RestorePatternAttackList();
            RestoreGiantAttackList();
            IsReady = false;
            }
        }

        public void StartSwordScenario()
        {
            if (!IsReady) return;
            _testPromotedCompanion = false;
            _scenario = CombatTestScenario.SwordSoldier;
            _enemyPosition = _targetPosition + Vector2.up * 2f;
            ResetScenario();
        }

        public void StartFalconScenario()
        {
            if (!IsReady) return;
            _testPromotedCompanion = false;
            _scenario = CombatTestScenario.FalconArcher;
            _enemyPosition = _targetPosition + Vector2.up * 4f;
            ResetScenario();
        }

        public void StartEnemyProjectileScenario()
        {
            if (!IsReady) return;
            _scenario = CombatTestScenario.EnemyProjectile;
            _enemyPosition = _targetPosition + Vector2.up * 4f;
            _repeat = false;
            ResetScenario();
        }

        public void StartBombardierScenario()
        {
            if (!IsReady) return;
            _testPromotedCompanion = false;
            _scenario = CombatTestScenario.Bombardier;
            _enemyPosition = _targetPosition + Vector2.up * 4f;
            ResetScenario();
        }

        public void StartFireMageScenario()
        {
            if (!IsReady) return;
            _testPromotedCompanion = false;
            _scenario = CombatTestScenario.FireMage;
            _enemyPosition = _targetPosition + Vector2.up * 4f;
            ResetScenario();
        }

        public void StartLightningMageScenario()
        {
            if (!IsReady) return;
            _testPromotedCompanion = false;
            _scenario = CombatTestScenario.LightningMage;
            _enemyPosition = _targetPosition + Vector2.up * 3f;
            ResetScenario();
        }

        public void StartSkeletonScytheScenario()
        {
            if (!IsReady) return;
            _testPromotedCompanion = false;
            _scenario = CombatTestScenario.SkeletonScytheThrower;
            _enemyPosition = _targetPosition + Vector2.up * 2.5f;
            ResetScenario();
        }

        public void StartClericScenario(bool promoted)
        {
            if (!IsReady) return;
            _scenario = CombatTestScenario.Cleric;
            _clericPromoted = promoted;
            _infiniteHp = false;
            _enemyPosition = _targetPosition + Vector2.up * 3f;
            ResetScenario();
        }

        public void StartEnemyFieldScenario()
        {
            if (!IsReady) return;
            _scenario = CombatTestScenario.EnemyField;
            _enemyPosition = _targetPosition + Vector2.up * 4f;
            _repeat = false;
            ResetScenario();
        }

        public void StartSelectedPromotionTest() => StartPromotionTest(_promotionTestUnit);

        public void StartPromotionTest(CompanionPromotionTestUnit unit)
        {
            if (!IsReady) return;
            _scenario = unit switch
            {
                CompanionPromotionTestUnit.ShieldGuard => CombatTestScenario.ShieldGuard,
                CompanionPromotionTestUnit.SwordSoldier => CombatTestScenario.SwordSoldier,
                CompanionPromotionTestUnit.FalconArcher => CombatTestScenario.FalconArcher,
                CompanionPromotionTestUnit.Bombardier => CombatTestScenario.Bombardier,
                CompanionPromotionTestUnit.FireMage => CombatTestScenario.FireMage,
                CompanionPromotionTestUnit.LightningMage => CombatTestScenario.LightningMage,
                CompanionPromotionTestUnit.WolfTamer => CombatTestScenario.WolfTamer,
                CompanionPromotionTestUnit.WraithKnight => CombatTestScenario.WraithKnight,
                CompanionPromotionTestUnit.SkeletonScytheThrower => CombatTestScenario.SkeletonScytheThrower,
                _ => throw new ArgumentOutOfRangeException(nameof(unit)),
            };
            _promotionTestUnit = unit;
            _testPromotedCompanion = true;
            // The first stationary squad anchors below the commander; keep the shield target in its reach.
            _enemyPosition = _targetPosition + (unit == CompanionPromotionTestUnit.ShieldGuard ? Vector2.down * 1.2f : Vector2.up * 2f);
            ResetScenario();
        }

        public void StartShieldGuardPassiveTest()
        {
            if (!IsReady) return;
            _scenario = CombatTestScenario.ShieldGuard;
            _promotionTestUnit = CompanionPromotionTestUnit.ShieldGuard;
            _testPromotedCompanion = true;
            _enemyPosition = _targetPosition + Vector2.up * 1.8f;
            ResetScenario();
            foreach (string id in new[] { "passive_shield_wide_strike", "passive_shield_strong_push",
                         "passive_shield_return_trail", "passive_shield_close_intercept" })
            {
                if (!Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.TryGet(id, out var entry)
                    || !_services.PassiveRoster.TryApply(Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.CreateData(in entry), out _))
                    throw new InvalidOperationException("Shield review passive could not be applied: " + id);
            }
        }

        public void StartSwordTurnTest(int memberCount, bool fullArmy = false)
        {
            if (!IsReady) return;
            _scenario = CombatTestScenario.SwordSoldier;
            _promotionTestUnit = CompanionPromotionTestUnit.SwordSoldier;
            _testPromotedCompanion = memberCount >= 3;
            _enemyPosition = _targetPosition + Vector2.up * 2f;
            ResetScenario();
            long sequence = 1000;
            if (memberCount == 2 && !_services.CompanionRuntimeHost.CardInput.SubmitCard(sequence++, "sword_soldier").Accepted)
                throw new InvalidOperationException("Sword turn reinforcement failed.");
            if (fullArmy)
            {
                foreach (var id in new[] { "shield_guard", "cleric", "falcon_archer", "fire_mage", "lightning_mage", "skeleton_scythe_thrower" })
                    for (int i = 0; i < 3; i++)
                        if (!_services.CompanionRuntimeHost.CardInput.SubmitCard(sequence++, id).Accepted)
                            throw new InvalidOperationException("Mixed army setup failed: " + id);
                for (int i = 0; i < 12; i++)
                {
                    float angle = i * Mathf.PI * 2f / 12;
                    var enemy = _services.Spawner.SpawnEnemy(_targetPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2.5f, Define.GOBLIN_ID);
                    enemy.ResetHealth(100000);
                    enemy.SetExternalMovement(true);
                }
            }
            Status = fullArmy ? "21명 혼합 편성: 검병 교대와 화면 혼잡도를 확인하세요. 다른 병종은 기존 공격입니다."
                : "검병 " + memberCount + "명 교대: 출격/복귀와 진급체 차례를 확인하세요.";
        }

        public void StartWraithKnightPassiveTest()
        {
            if (!IsReady) return;
            StartPromotionTest(CompanionPromotionTestUnit.WraithKnight);
            foreach (string id in new[] { "passive_wraith_wide_slash", "passive_wraith_deep_weakening", "passive_wraith_lingering_weakening", "passive_wraith_wide_patrol" })
            {
                if (!Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.TryGet(id, out var entry)
                    || !_services.PassiveRoster.TryApply(Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.CreateData(in entry), out _))
                    throw new InvalidOperationException("Wraith review passive could not be applied: " + id);
            }
            if (PromotionRangeTarget != null)
                PromotionRangeTarget.transform.position = _services.Registry.Player.transform.position + Vector3.up
                    * _app.Services.Data.GetCombatEffect("dmg_wraith_guardian_patrol_v1").Range * _services.PassiveEffects.Resolve("wraith_knight").OrbitRadiusMultiplier;
        }

        public void StartWolfTamerPassiveTest()
        {
            if (!IsReady) return;
            StartPromotionTest(CompanionPromotionTestUnit.WolfTamer);
            foreach (string id in new[] { "passive_wolf_fang", "passive_wolf_relentless_hunt", "passive_wolf_execution_sense", "passive_wolf_pack_ferocity" })
            {
                if (!Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.TryGet(id, out var entry)
                    || !_services.PassiveRoster.TryApply(Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.CreateData(in entry), out _))
                    throw new InvalidOperationException("Wolf review passive could not be applied: " + id);
            }
            for (int i = 0; i < 4; i++)
            {
                var target = _services.Spawner.SpawnEnemy(_enemyPosition + Vector2.right * (i * .6f), Define.GOBLIN_ID);
                target.ResetHealth(100);
                target.RestoreHealth(10);
                target.SetExternalMovement(true);
            }
        }

        public void StartLightningMagePassiveTest()
        {
            if (!IsReady) return;
            StartPromotionTest(CompanionPromotionTestUnit.LightningMage);
            foreach (string id in new[] { "passive_lightning_additional_chains", "passive_lightning_conductive_arc",
                "passive_lightning_long_shock", "passive_lightning_wide_overload" })
            {
                if (!Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.TryGet(id, out var entry)
                    || !_services.PassiveRoster.TryApply(Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.CreateData(in entry), out _))
                    throw new InvalidOperationException("Lightning review passive could not be applied: " + id);
            }
            for (int i = 1; i < 5; i++)
            {
                var target = _services.Spawner.SpawnEnemy(_enemyPosition + Vector2.right * (i * .5f), Define.GOBLIN_ID);
                target.ResetHealth(10000);
                target.SetExternalMovement(true);
            }
        }

        public void StartFireMagePassiveTest()
        {
            if (!IsReady) return;
            StartPromotionTest(CompanionPromotionTestUnit.FireMage);
            foreach (string id in new[] { "passive_fire_wide_field", "passive_fire_long_burn",
                "passive_fire_rapid_combustion", "passive_fire_additional_field" })
            {
                if (!Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.TryGet(id, out var entry)
                    || !_services.PassiveRoster.TryApply(Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.CreateData(in entry), out _))
                    throw new InvalidOperationException("Fire Mage review passive could not be applied: " + id);
            }
        }

        public void StartBombardierPassiveTest()
        {
            if (!IsReady) return;
            StartPromotionTest(CompanionPromotionTestUnit.Bombardier);
            foreach (string id in new[] { "passive_bomb_double_throw", "passive_bomb_short_fuse",
                "passive_bomb_fragments", "passive_bomb_compressed_powder" })
            {
                if (!Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.TryGet(id, out var entry)
                    || !_services.PassiveRoster.TryApply(Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.CreateData(in entry), out _))
                    throw new InvalidOperationException("Bombardier review passive could not be applied: " + id);
            }
            float ringRadius = _services.App.Data.GetCombatEffect("dmg_bomb_explosion_v1").Radius * .75f;
            for (int i = 0; i < 10; i++)
            {
                float angle = i * Mathf.PI * 2f / 10f;
                var target = _services.Spawner.SpawnEnemy(_enemyPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ringRadius, Define.GOBLIN_ID);
                target.ResetHealth(10000);
                target.SetExternalMovement(true);
            }
        }

        public void StartHerbalistPassiveTest()
        {
            if (!IsReady) return;
            StartHerbalistScenario();
            foreach (string id in new[] { "passive_herbalist_wide_flask", "passive_herbalist_concentrated_mixture",
                "passive_herbalist_long_reaction" })
            {
                if (!Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.TryGet(id, out var entry)
                    || !_services.PassiveRoster.TryApply(Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.CreateData(in entry), out _))
                    throw new InvalidOperationException("Herbalist review passive could not be applied: " + id);
            }
            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI / 3f;
                var target = _services.Spawner.SpawnEnemy(_enemyPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 2.3f, Define.GOBLIN_ID);
                target.ResetHealth(10000);
                target.SetExternalMovement(true);
                _spreadTestTargets.Add(target);
            }
        }

        public void StartFalconPassiveTest()
        {
            if (!IsReady) return;
            StartPromotionTest(CompanionPromotionTestUnit.FalconArcher);
            foreach (string id in new[] { "passive_archer_multi_shot", "passive_archer_double_volley",
                "passive_archer_piercing_arrow", "passive_archer_penetration_acceleration" })
            {
                if (!Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.TryGet(id, out var entry)
                    || !_services.PassiveRoster.TryApply(Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.CreateData(in entry), out _))
                    throw new InvalidOperationException("Falcon review passive could not be applied: " + id);
            }
            for (int i = 1; i <= 5; i++)
            {
                var target = _services.Spawner.SpawnEnemy(_enemyPosition + Vector2.up * (.5f * i), Define.GOBLIN_ID);
                target.ResetHealth(10000); target.SetExternalMovement(true);
            }
        }

        public void StartClericPassiveTest()
        {
            if (!IsReady) return;
            StartClericScenario(true);
            foreach (string id in new[] { "passive_cleric_piercing_light", "passive_cleric_split_light",
                         "passive_cleric_swift_return", "passive_cleric_full_prayer" })
            {
                if (!Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.TryGet(id, out var entry)
                    || !_services.PassiveRoster.TryApply(Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.CreateData(in entry), out _))
                    throw new InvalidOperationException("Cleric review passive could not be applied: " + id);
            }
        }

        public void StartSwordPassiveTest()
        {
            if (!IsReady) return;
            _scenario = CombatTestScenario.SwordSoldier;
            _promotionTestUnit = CompanionPromotionTestUnit.SwordSoldier;
            _testPromotedCompanion = true;
            _enemyPosition = _targetPosition + Vector2.up * .5f;
            ResetScenario();
            foreach (string id in new[] { "passive_sword_greatsword", "passive_sword_focused_strike",
                         "passive_sword_afterimage", "passive_sword_footwork" })
            {
                if (!Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.TryGet(id, out var entry)
                    || !_services.PassiveRoster.TryApply(Lizzo.PV.Gameplay.CardOffer.CompanionPassiveCatalog.CreateData(in entry), out _))
                    throw new InvalidOperationException("Sword review passive could not be applied: " + id);
            }
            for (int i = 1; i <= 6; i++)
            {
                var target = _services.Spawner.SpawnEnemy(_enemyPosition + Vector2.up * (.6f * i), Define.GOBLIN_ID);
                if (target == null) throw new InvalidOperationException("Sword piercing target spawn failed.");
                target.ResetHealth(10000);
                target.SetExternalMovement(true);
            }
        }

        public void StartWolfTamerScenario()
        {
            if (!IsReady) return;
            _scenario = CombatTestScenario.WolfTamer;
            _enemyPosition = _targetPosition + Vector2.up * 2f;
            ResetScenario();
        }

        public void StartNecromancerScenario()
        {
            if (!IsReady) return;
            _scenario = CombatTestScenario.Necromancer;
            _enemyPosition = _targetPosition + Vector2.up * 2f;
            ResetScenario();
        }

        public void StartCursePullReplay()
        {
            if (!IsReady || SummonReplayRunning) return;
            StartNecromancerScenario();
            if (_enemy == null) return;
            Vector3 center = _enemy.transform.position;
            if (!new CompanionCurseDeathPullResolver(_services.App.Data, _services.PassiveEffects.ResolveCursePullRadiusMultiplier).TryResolve(out var pullSetup)) return;
            for (int index = 0; index < 6; index++)
            {
                float angle = index * Mathf.PI / 3f;
                var target = _services.Spawner.SpawnEnemy(
                    center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * (pullSetup.Radius * .75f),
                    Define.GOBLIN_ID);
                if (target != null) target.SetExternalMovement(true);
            }
            Physics2D.SyncTransforms();
            _cursePullReplayAt = Time.unscaledTime + 1.5f;
            CursePullReplayStatus = "1.5초 후 중심 표적 처치 · Game 화면에서 원 안의 적 6명을 보세요.";
            UnityEditor.EditorApplication.isPaused = false;
            UnityEditor.EditorApplication.ExecuteMenuItem("Window/General/Game");
        }

        private void TickCursePullReplay()
        {
            if (_cursePullReplayAt < 0f || Time.unscaledTime < _cursePullReplayAt) return;
            _cursePullReplayAt = -1f;
            if (_enemy == null || _enemy.Hp <= 0)
            {
                CursePullReplayStatus = "중심 표적이 없어 재현을 취소했습니다. 다시 실행하세요.";
                return;
            }
            _enemy.ApplyCompanionStatus(CompanionEnemyStatusKind.Curse,
                new CompanionStatusSource("necromancer", _player.GetInstanceID()), 1f, 4f, Time.time);
            KillEnemy();
            int selected = 0;
            foreach (var target in _services.Registry.Enemies)
                if (target.IsForcedMovementActive) selected++;
            CursePullReplayStatus = $"재현 완료 · 당김 대상 {selected}명 · 다시 누르면 새 무작위 선택";
        }

        public void StartSummonReplay()
        {
            if (!IsReady || SummonReplayRunning) return;
            StartNecromancerScenario();
            _keyboardMovement = true;
            _summonReplayDeaths = 0;
            _summonReplayDeadline = Time.time + 15f;
            SummonReplayRunning = true;
            SummonReplayStatus = "저주탄 대기 중 · 0/3";
            UnityEditor.EditorApplication.isPaused = false;
        }

        private void TickSummonReplay()
        {
            if (!SummonReplayRunning) return;
            if (RitualSummonCount > 0)
            {
                RespawnKillTarget();
                SummonReplayRunning = false;
                SummonReplayStatus = "소환 완료 · 일시정지 해제 후 Game 화면에서 WASD/방향키로 이동하세요.";
                if (_pauseAfterSummonReplay) UnityEditor.EditorApplication.isPaused = true;
                return;
            }
            if (Time.time >= _summonReplayDeadline)
            {
                SummonReplayRunning = false;
                SummonReplayStatus = "재현 시간 초과 · 군단장과 표적의 거리 또는 Console을 확인하고 다시 실행하세요.";
                return;
            }
            if (_summonReplayDeaths >= 3) return;
            if (_enemy == null || _enemy.Hp <= 0) { RespawnKillTarget(); return; }
            if (!_enemy.EditorHasNecromancerCurse) return;
            KillEnemy();
            _summonReplayDeaths++;
            _summonReplayDeadline = Time.time + 15f;
            SummonReplayStatus = $"저주 후 처치 {_summonReplayDeaths}/3 · 소환 대기";
            if (_summonReplayDeaths < 3) RespawnKillTarget();
        }

        public void StartHerbalistScenario()
        {
            if (!IsReady) return;
            _scenario = CombatTestScenario.FieldHerbalist;
            _enemyPosition = _targetPosition + Vector2.up * 2f;
            ResetScenario();
        }

        public void KillSpreadTarget(int index)
        {
            if (!IsReady || !IsSpreadScenario || index < 0 || index >= _spreadTestTargets.Count) return;
            var target = _spreadTestTargets[index];
            if (target != null && target.Hp > 0) target.OnDamaged(_player, target.Hp);
        }

        public void RespawnKillTarget()
        {
            if (!IsReady || !IsKillScenario || (_enemy != null && _enemy.Hp > 0)) return;
            _enemy = _services.Spawner.SpawnEnemy(_enemyPosition, Define.RED_CHARGER_ID, EnemyEncounterRank.Elite, 1f);
            _charger = _enemy == null ? null : _enemy.GetComponent<EnemyChargeController>();
            if (_charger == null) throw new InvalidOperationException("Kill test target spawn failed.");
            _charger.Setup(_enemy);
            _charger.SetEditorManualActions(true);
            _enemy.ResetHealth(KillTargetHp);
        }

        public void ResetScenario()
        {
            _cursePullReplayAt = -1f;
            CursePullReplayStatus = "다중 당김 재현 버튼을 누르세요.";
            SummonReplayRunning = false;
            SummonReplayStatus = "소환 재현 버튼을 누르세요.";
            if (!IsReady) return;
            RestorePatternAttackList();
            RestoreGiantAttackList();
            _chainTestTargets.Clear();
            _spreadTestTargets.Clear();
            PromotionRangeTarget = null;
            _services.ResetRuntimeForResult();
            _services.Registry.Clear();
            _services.ResetRunState();
            _services.State.Reset(1);
            _player = _services.Spawner.SpawnPlayer(_targetPosition);
            if (_player == null) throw new InvalidOperationException("Production commander spawn failed.");
            _player.BindArenaBounds(_bounds);
            _player.SetEditorAutomationInfiniteHp(_infiniteHp);
            MoveActor(_player, _targetPosition);
            _camera.Target = _player.gameObject;
            _boss = null; _charger = null; _wolf = null;
            IsEnemyProjectileScenario = _scenario == CombatTestScenario.EnemyProjectile;
            IsEnemyFieldScenario = _scenario == CombatTestScenario.EnemyField;
            EnemyProjectileShotCount = 0;
            IsCompanionScenario = _scenario == CombatTestScenario.SwordSoldier || _scenario == CombatTestScenario.FalconArcher
                || _scenario == CombatTestScenario.Bombardier || _scenario == CombatTestScenario.FireMage || _scenario == CombatTestScenario.LightningMage || _scenario == CombatTestScenario.SkeletonScytheThrower || _scenario == CombatTestScenario.Cleric || IsKillScenario || IsSpreadScenario || _scenario == CombatTestScenario.ShieldGuard || _scenario == CombatTestScenario.WraithKnight;
            CompanionLabel = _scenario == CombatTestScenario.SwordSoldier ? "검병"
                : _scenario == CombatTestScenario.FalconArcher ? "매 궁수"
                : _scenario == CombatTestScenario.Bombardier ? "폭탄병"
                : _scenario == CombatTestScenario.FireMage ? "화염 마법사"
                : _scenario == CombatTestScenario.LightningMage ? "번개 마법사"
                : _scenario == CombatTestScenario.SkeletonScytheThrower ? "해골 낫 투척병"
                : _scenario == CombatTestScenario.Cleric ? (_clericPromoted ? "성직자 진급 분대" : "성직자")
                : IsCurseScenario ? "사령술사 진급 분대" : IsKillScenario ? "늑대 조련사 진급 분대"
                : IsSpreadScenario ? "전투 약초사 진급 분대"
                : _scenario == CombatTestScenario.ShieldGuard ? "방패병"
                : _scenario == CombatTestScenario.WraithKnight ? "망령 기사" : string.Empty;
            if (_testPromotedCompanion && IsCompanionScenario && !IsKillScenario && !IsSpreadScenario && _scenario != CombatTestScenario.Cleric)
                CompanionLabel += " 진급 분대";
            switch (_scenario)
            {
                case CombatTestScenario.HungryGiant:
                    _enemy = _services.Spawner.SpawnEnemy(_enemyPosition, Define.BOSS_ID, EnemyEncounterRank.Boss, 1f);
                    _boss = _enemy == null ? null : _enemy.GetComponent<EnemyBossController>();
                    if (_boss == null) throw new InvalidOperationException("Production Hungry Giant spawn failed.");
                    _testLoadout = _boss.GetComponent<EnemyAttackLoadout>();
                    if (_testLoadout == null) throw new InvalidOperationException("Giant test requires its authored attack loadout.");
                    _authoredGiantAttacks = _testLoadout.CopyEditorAttacks();
                    switch (_giantAttackSet)
                    {
                        case GiantAttackTestSet.ContactOnly: _testLoadout.SetEditorAttacks(EnemyAttackKind.Contact); break;
                        case GiantAttackTestSet.ChargeAndContact: _testLoadout.SetEditorAttacks(EnemyAttackKind.Charge, EnemyAttackKind.Contact); break;
                        case GiantAttackTestSet.AreaAndContact: _testLoadout.SetEditorAttacks(EnemyAttackKind.Area, EnemyAttackKind.Contact); break;
                        case GiantAttackTestSet.AreaFirst: _testLoadout.SetEditorAttacks(EnemyAttackKind.Area, EnemyAttackKind.Charge, EnemyAttackKind.Contact); break;
                    }
                    _boss.Setup(_enemy);
                    _boss.SetEditorManualActions(_manualAttacks);
                    break;
                case CombatTestScenario.RedCharger:
                case CombatTestScenario.EnemyProjectile:
                case CombatTestScenario.EnemyField:
                    _enemy = _services.Spawner.SpawnEnemy(_enemyPosition, Define.RED_CHARGER_ID, EnemyEncounterRank.Elite, 1f);
                    _charger = _enemy == null ? null : _enemy.GetComponent<EnemyChargeController>();
                    if (_charger == null) throw new InvalidOperationException("Production Red Charger spawn failed.");
                    if (_scenario == CombatTestScenario.RedCharger) ConfigurePatternAttackList(_enemy);
                    _charger.Setup(_enemy);
                    _charger.SetEditorManualActions(IsEnemyProjectileScenario || IsEnemyFieldScenario || _manualAttacks);
                    break;
                case CombatTestScenario.HungryWolf:
                    _enemy = _services.Spawner.SpawnEnemy(_enemyPosition, Define.SNAKE_ID, EnemyEncounterRank.Normal, 1f);
                    _wolf = _enemy == null ? null : _enemy.GetComponent<EnemyChargeController>();
                    if (_wolf == null) throw new InvalidOperationException("Production Hungry Wolf spawn failed.");
                    ConfigurePatternAttackList(_enemy);
                    _wolf.Setup(_enemy);
                    _wolf.SetEditorManualActions(_manualAttacks);
                    break;
                case CombatTestScenario.SwordSoldier:
                case CombatTestScenario.FalconArcher:
                case CombatTestScenario.Bombardier:
                case CombatTestScenario.FireMage:
                case CombatTestScenario.LightningMage:
                case CombatTestScenario.SkeletonScytheThrower:
                case CombatTestScenario.Cleric:
                case CombatTestScenario.WolfTamer:
                case CombatTestScenario.Necromancer:
                case CombatTestScenario.FieldHerbalist:
                case CombatTestScenario.ShieldGuard:
                case CombatTestScenario.WraithKnight:
                    // A real, stationary enemy receives production hits; no fake attack or damage path.
                    _enemy = _services.Spawner.SpawnEnemy(_enemyPosition, Define.RED_CHARGER_ID, EnemyEncounterRank.Elite, 1f);
                    _charger = _enemy == null ? null : _enemy.GetComponent<EnemyChargeController>();
                    if (_charger == null) throw new InvalidOperationException("Companion test target spawn failed.");
                    _charger.Setup(_enemy);
                    _charger.SetEditorManualActions(true);
                    _enemy.ResetHealth(IsKillScenario ? KillTargetHp : 10000);
                    if (_scenario == CombatTestScenario.LightningMage)
                    {
                        _chainTestTargets.Add(_enemy);
                        for (int i = 1; i <= 3; i++)
                        {
                            Vector2 offset = i == 1 ? Vector2.right * 1.4f
                                : i == 2 ? new Vector2(1.4f, 1.4f) : Vector2.left * 2.2f;
                            Vector2 position = _enemyPosition + offset;
                            var target = _services.Spawner.SpawnEnemy(position, Define.RED_CHARGER_ID, EnemyEncounterRank.Elite, 1f);
                            var ai = target == null ? null : target.GetComponent<EnemyChargeController>();
                            if (ai == null) throw new InvalidOperationException("Chain test target spawn failed.");
                            ai.Setup(target);
                            ai.SetEditorManualActions(true);
                            target.ResetHealth(10000);
                            _chainTestTargets.Add(target);
                        }
                    }
                    if (IsSpreadScenario)
                    {
                        _spreadTestTargets.Add(_enemy);
                        for (int i = 1; i <= 2; i++)
                        {
                            var target = _services.Spawner.SpawnEnemy(_enemyPosition + Vector2.up * (2f * i), Define.RED_CHARGER_ID, EnemyEncounterRank.Elite, 1f);
                            var ai = target == null ? null : target.GetComponent<EnemyChargeController>();
                            if (ai == null) throw new InvalidOperationException("Vulnerability spread target spawn failed.");
                            ai.Setup(target);
                            ai.SetEditorManualActions(true);
                            target.ResetHealth(10000);
                            _spreadTestTargets.Add(target);
                        }
                    }
                    if (_testPromotedCompanion && (_scenario == CombatTestScenario.WraithKnight || _scenario == CombatTestScenario.SkeletonScytheThrower))
                    {
                        float orbitRadius = _scenario == CombatTestScenario.WraithKnight ? _app.Services.Data.GetCombatEffect("dmg_wraith_guardian_patrol_v1").Range : _app.Services.Data.GetCombatEffect("dmg_skeleton_reaper_orbit_v1").Range;
                        PromotionRangeTarget = _services.Spawner.SpawnEnemy(_targetPosition + Vector2.up * orbitRadius, Define.RED_CHARGER_ID, EnemyEncounterRank.Elite, 1f);
                        var ai = PromotionRangeTarget == null ? null : PromotionRangeTarget.GetComponent<EnemyChargeController>();
                        if (ai == null) throw new InvalidOperationException("Promotion orbit target spawn failed.");
                        ai.Setup(PromotionRangeTarget);
                        ai.SetEditorManualActions(true);
                        PromotionRangeTarget.ResetHealth(10000);
                    }
                    string companionId = _scenario == CombatTestScenario.SwordSoldier ? "sword_soldier"
                        : _scenario == CombatTestScenario.FalconArcher ? "falcon_archer"
                        : _scenario == CombatTestScenario.Bombardier ? "bombardier"
                        : _scenario == CombatTestScenario.LightningMage ? "lightning_mage"
                        : _scenario == CombatTestScenario.SkeletonScytheThrower ? "skeleton_scythe_thrower"
                        : _scenario == CombatTestScenario.Cleric ? "cleric" : IsCurseScenario ? "necromancer" : IsKillScenario ? "wolf_tamer" : IsSpreadScenario ? "field_herbalist"
                        : _scenario == CombatTestScenario.ShieldGuard ? "shield_guard" : _scenario == CombatTestScenario.WraithKnight ? "wraith_knight" : "fire_mage";
                    var recruit = _services.CompanionRuntimeHost.CardInput.SubmitCard(1, companionId);
                    if (!recruit.Accepted) throw new InvalidOperationException("Companion test recruitment rejected: " + recruit.Rejection);
                    if (IsKillScenario || IsSpreadScenario || (_testPromotedCompanion && _scenario != CombatTestScenario.Cleric))
                        for (int count = 2; count <= 3; count++)
                            if (!_services.CompanionRuntimeHost.CardInput.SubmitCard(count, companionId).Accepted)
                                throw new InvalidOperationException("Kill-condition test promotion failed: " + companionId);
                    if (_scenario == CombatTestScenario.Cleric)
                    {
                        _player.RestoreHealth(Mathf.Max(1, _player.MaxHp / 2));
                        if (_clericPromoted)
                        {
                            for (int count = 2; count <= 3; count++)
                                if (!_services.CompanionRuntimeHost.CardInput.SubmitCard(count, companionId).Accepted)
                                    throw new InvalidOperationException("Cleric test promotion failed.");
                        }
                    }
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(_scenario));
            }
            // Match production's post-setup activation so real deaths can report their attribution.
            _services.State.MarkLoaded();
            _nextRepeatAt = Time.time + _repeatInterval;
            Status = IsCompanionScenario
                ? $"{CompanionLabel} 구성 완료. 자동 공격합니다. 적 HP와 공격 횟수를 확인하세요."
                : IsEnemyFieldScenario ? "적 장판 준비 완료. 생성 즉시 피해, 이후 1초 주기·3초 지속입니다. Infinite Hp 해제 시 HP 감소를 확인할 수 있습니다."
                : IsEnemyProjectileScenario ? "적 투사체 준비 완료. 발사 버튼 또는 Repeat를 사용하세요. Infinite Hp를 끄면 HP 감소를 확인할 수 있습니다."
                : "Ready. Select an attack and request it, or enable automatic AI.";
        }

        public bool RequestAttack()
        {
            if (IsCompanionScenario) return false;
            if (IsEnemyFieldScenario)
            {
                if (!IsReady || _enemy == null || _enemy.Hp <= 0 || _player == null || _player.Hp <= 0) return false;
                Vector3 center = _player.CombatCollider.bounds.center;
                bool created = _services.PersistentFieldModule.TrySpawn(CombatPersistentFieldRequest.CreateEnemyDamage(
                    "combat_test_enemy_field", "dot_fire_field_v1", _enemy, center, 5, 1.6f, 1f, 3f), Time.time);
                if (created && !RetroVfx.SpawnCompanionAttack("dot_fire_field_v1", center, Vector3.up, 2.2f))
                    Debug.LogError("Enemy field test requires its existing fire-field visual.", this);
                _nextRepeatAt = Time.time + Mathf.Max(.1f, _repeatInterval);
                Status = created ? "적 장판 생성: 범위 밖으로 이동해 반복 피해를 피하세요." : "적 장판 생성 실패.";
                return created;
            }
            if (IsEnemyProjectileScenario)
            {
                if (!IsReady || _enemy == null || _enemy.Hp <= 0 || _player == null || _player.Hp <= 0) return false;
                Vector3 origin = _enemy.CombatCollider.bounds.center;
                Vector3 direction = _player.CombatCollider.bounds.center - origin;
                bool fired = _services.ProjectileModule.TrySpawn(CombatProjectileRequest.CreateStraight(
                    "combat_test_enemy_arrow", _enemy, origin, direction.normalized, 9, 7f, 1.2f,
                    RetroVfxKind.PlayerDamaged, CombatProjectileFaction.Enemy, presentationId: "dmg_falcon_arrow_v1"));
                if (fired) EnemyProjectileShotCount++;
                _nextRepeatAt = Time.time + Mathf.Max(.1f, _repeatInterval);
                Status = fired ? "적 화살 발사: 비행 경로에서 벗어나 회피하세요." : "적 화살 발사 실패.";
                return fired;
            }
            bool accepted = IsReady && (_boss != null ? _boss.RequestEditorAttack(_attack)
                : _charger != null ? _charger.RequestEditorAttack(_attack)
                : _wolf != null && _wolf.RequestEditorAttack(_attack));
            Status = accepted ? "Attack requested through the production action runner."
                : "Not ready: check attack range, cooldown and Idle state. Contact requires touching; AREA is boss-only.";
            _nextRepeatAt = Time.time + Mathf.Max(.1f, _repeatInterval);
            return accepted;
        }

        public void PlaceTarget() { if (_player != null) MoveActor(_player, _targetPosition); }
        public void PlaceEnemy() { if (_enemy != null) MoveActor(_enemy, _enemyPosition); }
        public void PlaceTargetAtContact()
        {
            if (_enemy == null || _player == null) return;
            MoveActor(_player, (Vector2)_enemy.transform.position + Vector2.down * .4f);
        }
        public void KillEnemy()
        {
            if (_enemy != null && _enemy.Hp > 0) _enemy.OnDamaged(_player, _enemy.Hp);
        }

        private static void MoveActor(Component actor, Vector2 position)
        {
            var body = actor.GetComponent<Rigidbody2D>();
            if (body != null) { body.position = position; body.linearVelocity = Vector2.zero; }
            else actor.transform.position = position;
            Physics2D.SyncTransforms();
        }

        private void Update()
        {
            if (!IsReady || _player == null) return;
            _player.SetEditorAutomationInfiniteHp(_infiniteHp);
            if (_boss != null) _boss.SetEditorManualActions(_manualAttacks);
            if (_charger != null) _charger.SetEditorManualActions(IsCompanionScenario || IsEnemyProjectileScenario || IsEnemyFieldScenario || _manualAttacks);
            if (_wolf != null) _wolf.SetEditorManualActions(_manualAttacks);
            Vector2 direction = Vector2.zero;
            var keyboard = Keyboard.current;
            if (_keyboardMovement && keyboard != null)
            {
                direction.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                direction.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            }
            _player.SetMoveDirection(direction.normalized);
            _services.TickRuntime(Time.deltaTime, Time.time, Time.frameCount, false, HitStop.IsActive);
            TickSummonReplay();
            TickCursePullReplay();
            if (!IsCompanionScenario && (IsEnemyProjectileScenario || IsEnemyFieldScenario || _manualAttacks) && _repeat && _enemy != null && _enemy.Hp > 0
                && ActionFrame.Phase == EnemyActionPhase.Idle && Time.time >= _nextRepeatAt) RequestAttack();
        }

        public void StartGiantAttackListScenario()
        {
            if (!IsReady) return;
            _scenario = CombatTestScenario.HungryGiant;
            _manualAttacks = false;
            _repeat = false;
            _enemyPosition = _targetPosition + Vector2.up * 2f;
            ResetScenario();
        }

        public void StartPatternAttackListScenario(bool wolf)
        {
            if (!IsReady) return;
            _scenario = wolf ? CombatTestScenario.HungryWolf : CombatTestScenario.RedCharger;
            _manualAttacks = false;
            _repeat = false;
            _enemyPosition = _targetPosition + Vector2.up * (wolf ? 1.3f : 3.5f);
            ResetScenario();
        }

        private void ConfigurePatternAttackList(EnemyActor enemy)
        {
            _patternLoadout = enemy.GetComponent<EnemyAttackLoadout>();
            if (_patternLoadout == null) throw new InvalidOperationException("Pattern test requires its authored attack loadout.");
            _authoredPatternAttacks = _patternLoadout.CopyEditorAttacks();
            if (_patternAttackSet == PatternAttackTestSet.ContactOnly)
                _patternLoadout.SetEditorAttacks(EnemyAttackKind.Contact);
            else if (_patternAttackSet == PatternAttackTestSet.ChargeOnly)
                _patternLoadout.SetEditorAttacks(EnemyAttackKind.Charge);
        }

        private void RestorePatternAttackList()
        {
            if (_patternLoadout != null && _authoredPatternAttacks != null)
                _patternLoadout.SetEditorAttacks(_authoredPatternAttacks);
            _patternLoadout = null;
            _authoredPatternAttacks = null;
        }

        private void RestoreGiantAttackList()
        {
            if (_testLoadout != null && _authoredGiantAttacks != null)
                _testLoadout.SetEditorAttacks(_authoredGiantAttacks);
            _testLoadout = null;
            _authoredGiantAttacks = null;
        }

        private void PresentStatusReaction(StatusFeedbackPresentation presentation, StatusFeedbackProfileSO profile)
        {
            if (_vfxCatalog == null || presentation.EventKind != StatusFeedbackEventKind.Reaction) return;
            try
            {
                foreach (var reaction in profile.Reactions)
                {
                    if (reaction.ReactionKind != presentation.ReactionKind) continue;
                    foreach (var entry in _vfxCatalog.Entries)
                    {
                        if (entry.Id.Value != reaction.ReactionVfxId.Value || entry.Asset == null) continue;
                        var instance = _services.Factory.Rent(entry.Asset, $"CombatTestStatus:{entry.Id.Value}", _poolRoot);
                        if (instance == null) return;
                        instance.transform.SetPositionAndRotation(presentation.Position, Quaternion.identity);
                        instance.transform.localScale = Vector3.one * presentation.VisualScale;
                        var wrapper = instance.GetComponent<VfxWrapperInstance>();
                        if (wrapper != null) wrapper.ActivatePooled(_services.Factory);
                        else _services.Factory.Release(instance);
                        return;
                    }
                }
            }
            catch (Exception exception) { Debug.LogException(exception, this); }
        }

        private void OnDestroy()
        {
            RestorePatternAttackList();
            RestoreGiantAttackList();
            IsReady = false;
            if (_disposed || _services == null) return;
            _disposed = true;
            _services.WorldFeedback.Sink.StatusPresented -= PresentStatusReaction;
            _services.Dispose();
            FloatingDamageText.ClearServices();
            RetroVfx.ClearServices();
            RetroSfx.StopAndReset();
            Time.timeScale = 1f;
        }
    }
}
#endif
