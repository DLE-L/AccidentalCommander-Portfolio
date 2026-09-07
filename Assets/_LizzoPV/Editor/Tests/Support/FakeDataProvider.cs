using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;using Lizzo.PV.Data;


namespace Lizzo.PV.Tests.Support
{
    internal sealed class FakeDataProvider : IDataProvider
    {
        readonly Dictionary<string, UnitData> _units = new Dictionary<string, UnitData>();
        readonly Dictionary<string, SkillData> _skills = new Dictionary<string, SkillData>();
        readonly Dictionary<string, EnemyData> _enemies = new Dictionary<string, EnemyData>();
        readonly Dictionary<int, EnemyData> _enemiesByTemplate = new Dictionary<int, EnemyData>();
        readonly Dictionary<string, SynergyData> _synergies = new Dictionary<string, SynergyData>();
        readonly Dictionary<int, int> _levelExp = new Dictionary<int, int>();
        readonly List<CompanionRosterData> _companionRoster = new List<CompanionRosterData>();
        readonly Dictionary<string, CompanionRosterData> _companionRosterByUnitId = new Dictionary<string, CompanionRosterData>();
        readonly Dictionary<string, CompanionPromotionData> _companionPromotionsByProfileId = new Dictionary<string, CompanionPromotionData>();
        readonly IReadOnlyList<CompanionRosterData> _companionRosterView;
        readonly List<CompanionCardLocalizationData> _companionCardLocalizations = new List<CompanionCardLocalizationData>();
        readonly Dictionary<string, CompanionCardLocalizationData> _companionCardLocalizationsByUnitId = new Dictionary<string, CompanionCardLocalizationData>();
        readonly IReadOnlyList<CompanionCardLocalizationData> _companionCardLocalizationView;
        readonly List<CompanionCombatProfileData> _companionCombatProfiles = new List<CompanionCombatProfileData>();
        readonly Dictionary<string, CompanionCombatProfileData> _companionCombatProfilesByUnitId = new Dictionary<string, CompanionCombatProfileData>();
        readonly List<CombatEffectData> _combatEffects = new List<CombatEffectData>();
        readonly Dictionary<string, CombatEffectData> _combatEffectsById = new Dictionary<string, CombatEffectData>();
        readonly IReadOnlyList<CompanionCombatProfileData> _companionCombatProfileView;
        readonly IReadOnlyList<CombatEffectData> _combatEffectView;
        readonly List<CompanionSummonData> _companionSummons = new List<CompanionSummonData>();
        readonly Dictionary<string, CompanionSummonData> _companionSummonsById = new Dictionary<string, CompanionSummonData>();
        readonly IReadOnlyList<CompanionSummonData> _companionSummonView;
        readonly List<string> _missingIds = new List<string>();
        readonly RunTuningData _runTuning = new RunTuningData();
        bool _initialized;
        bool _failInitialization;

        public FakeDataProvider()
        {
            _companionRosterView = _companionRoster.AsReadOnly();
            _companionCardLocalizationView = _companionCardLocalizations.AsReadOnly();
            _companionCombatProfileView = _companionCombatProfiles.AsReadOnly();
            _combatEffectView = _combatEffects.AsReadOnly();
            _companionSummonView = _companionSummons.AsReadOnly();
            AddBaseline();
            AddCompanionRosterBaseline();
        }
        public bool IsInitialized => _initialized;
        public RunTuningData RunTuning { get { EnsureInitialized(); return _runTuning; } }
        public IReadOnlyList<CompanionRosterData> CompanionRoster { get { EnsureInitialized(); return _companionRosterView; } }
        public IReadOnlyList<CompanionCardLocalizationData> CompanionCardLocalizations { get { EnsureInitialized(); return _companionCardLocalizationView; } }
        public IReadOnlyList<CompanionCombatProfileData> CompanionCombatProfiles { get { EnsureInitialized(); return _companionCombatProfileView; } }
        public IReadOnlyList<CombatEffectData> CombatEffects { get { EnsureInitialized(); return _combatEffectView; } }
        public IReadOnlyList<CompanionSummonData> CompanionSummons { get { EnsureInitialized(); return _companionSummonView; } }

        public FakeDataProvider SetInitializationFailure(params string[] missingIds)
        {
            _failInitialization = true;
            _missingIds.Clear();
            if (missingIds != null) _missingIds.AddRange(missingIds);
            return this;
        }

        public FakeDataProvider SetRunTuning(Action<RunTuningData> configure) { configure?.Invoke(_runTuning); return this; }
        public FakeDataProvider SetLevelExp(int level, int exp) { _levelExp[level] = exp; return this; }
        public FakeDataProvider SetUnit(UnitData data) { if (data != null && !string.IsNullOrEmpty(data.Id)) _units[data.Id] = data; return this; }
        public FakeDataProvider SetSkill(SkillData data) { if (data != null && !string.IsNullOrEmpty(data.Id)) _skills[data.Id] = data; return this; }
        public FakeDataProvider SetSynergy(SynergyData data) { if (data != null && !string.IsNullOrEmpty(data.Id)) _synergies[data.Id] = data; return this; }

        public FakeDataProvider SetCompanionCombatProfile(CompanionCombatProfileData data)
        {
            if (data == null || string.IsNullOrEmpty(data.UnitId)) return this;
            if (_companionCombatProfilesByUnitId.TryGetValue(data.UnitId, out CompanionCombatProfileData existing))
                _companionCombatProfiles.Remove(existing);
            _companionCombatProfilesByUnitId[data.UnitId] = data;
            _companionCombatProfiles.Add(data);
            return this;
        }

        public FakeDataProvider SetCompanionSummon(CompanionSummonData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Id)) return this;
            if (_companionSummonsById.TryGetValue(data.Id, out CompanionSummonData existing))
                _companionSummons.Remove(existing);
            _companionSummonsById[data.Id] = data;
            _companionSummons.Add(data);
            return this;
        }

        public FakeDataProvider SetEnemy(EnemyData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Id)) return this;
            _enemies[data.Id] = data;
            _enemiesByTemplate[data.TemplateId] = data;
            return this;
        }

        public UniTask<DataLoadResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _initialized = true;
            return UniTask.FromResult(CreateResult(!_failInitialization, _failInitialization ? "fake_failure" : "fake"));
        }

        public UnitData GetUnit(string id) { EnsureInitialized(); return Find(_units, id); }
        public CompanionRosterData GetCompanionRoster(string unitId) { EnsureInitialized(); return _companionRosterByUnitId.TryGetValue(unitId, out CompanionRosterData value) ? value : null; }
        public CompanionCardLocalizationData GetCompanionCardLocalization(string unitId) { EnsureInitialized(); return _companionCardLocalizationsByUnitId.TryGetValue(unitId, out CompanionCardLocalizationData value) ? value : null; }
        public CompanionPromotionData GetCompanionPromotion(string profileId) { EnsureInitialized(); return _companionPromotionsByProfileId.TryGetValue(profileId, out CompanionPromotionData value) ? value : null; }
        public CompanionCombatProfileData GetCompanionCombatProfile(string unitId) { EnsureInitialized(); return _companionCombatProfilesByUnitId.TryGetValue(unitId, out CompanionCombatProfileData value) ? value : null; }
        public CombatEffectData GetCombatEffect(string effectId) { EnsureInitialized(); return _combatEffectsById.TryGetValue(effectId, out CombatEffectData value) ? value : null; }
        public CompanionSummonData GetCompanionSummon(string summonId) { EnsureInitialized(); return _companionSummonsById.TryGetValue(summonId, out CompanionSummonData value) ? value : null; }
        public SkillData GetSkill(string id) { EnsureInitialized(); return Find(_skills, id); }
        public EnemyData GetEnemy(string id) { EnsureInitialized(); return Find(_enemies, id); }
        public EnemyData GetEnemyByTemplateId(int id) { EnsureInitialized(); return _enemiesByTemplate.TryGetValue(id, out EnemyData value) ? value : null; }
        public SynergyData GetSynergy(string id) { EnsureInitialized(); return Find(_synergies, id); }

        public int GetLevelExp(int level)
        {
            EnsureInitialized();
            return _levelExp.TryGetValue(level, out int value) ? value : _runTuning.FirstLevelExp;
        }

        public float GetEffectiveSpawnSeconds(EnemyData enemyData)
        {
            EnsureInitialized();
            return enemyData == null ? 0.0f : enemyData.SpawnSeconds * _runTuning.TimelineScale;
        }

        public float GetStage1SpawnBudget(float elapsedSeconds)
        {
            EnsureInitialized();
            float scale = _runTuning.TimelineScale;
            if (elapsedSeconds < 25.0f * scale) return 1.6f;
            if (elapsedSeconds < 60.0f * scale) return 2.2f;
            if (elapsedSeconds < 150.0f * scale) return 2.8f;
            return 3.2f;
        }

		void AddBaseline()
		{
			ConfigureEncounter(_runTuning.TimedElite, 5, EnemyEncounterRank.Elite, 1.0f);
			ConfigureFinalThreat(_runTuning.TutorialFinalThreat, 5, EnemyEncounterRank.Elite, 1.0f);
			ConfigureFinalThreat(_runTuning.Stage1FinalThreat, 3, EnemyEncounterRank.Boss, 1.0f);
			ConfigureFinalThreat(_runTuning.Stage2FinalThreat, 3, EnemyEncounterRank.Boss, 1.0f);
			ConfigureFinalThreat(_runTuning.Stage3FinalThreat, 3, EnemyEncounterRank.Boss, 1.0f);
            SetUnit(new UnitData { Id = "commander_01", SkillId = string.Empty, Hp = 100, MoveSpeed = 5.0f });
            SetUnit(new UnitData { Id = "shield_guard", SkillId = "shield_push", Hp = 80 });
            SetUnit(new UnitData { Id = "shield_captain", SkillId = "shield_captain_push", Hp = 120 });
            SetUnit(new UnitData { Id = "sword_soldier", SkillId = "sword_front_slash", Hp = 90 });
            SetUnit(new UnitData { Id = "cleric", SkillId = "cleric_heal", Hp = 70 });
            SetUnit(new UnitData { Id = "archer", SkillId = "archer_far_shot", Hp = 60 });
            foreach (string id in new[] { "shield_push", "shield_captain_push", "sword_front_slash", "cleric_heal", "archer_far_shot" }) SetSkill(new SkillData { Id = id, Power = 10 });
            SetEnemy(new EnemyData { Id = "small_goblin", TemplateId = 1, Type = "normal", Hp = 30, SpawnSeconds = 0.0f });
            SetEnemy(new EnemyData { Id = "hungry_wolf", TemplateId = 2, Type = "normal", Hp = 50, SpawnSeconds = 20.0f });
            SetEnemy(new EnemyData { Id = "shield_orc", TemplateId = 4, Type = "normal", Hp = 100, SpawnSeconds = 40.0f });
            SetEnemy(new EnemyData { Id = "elite_red_charger", TemplateId = 5, Type = "elite", Hp = 500, Attack = 16, ChargeAttack = 24, SpawnSeconds = 150.0f });
            SetEnemy(new EnemyData { Id = "boss_hungry_giant", TemplateId = 3, Type = "boss", Hp = 2500, SpawnSeconds = 300.0f });
            SetLevelExp(1, _runTuning.FirstLevelExp);
        }

		static void ConfigureFinalThreat(
			EnemyEncounterDefinition target,
			int enemyTemplateId,
			EnemyEncounterRank encounterRank,
			float scaleMultiplier)
		{
			target.EnemyTemplateId = enemyTemplateId;
			target.EncounterRank = encounterRank;
			target.ScaleMultiplier = scaleMultiplier;
		}

		static void ConfigureEncounter(
			EnemyEncounterDefinition target,
			int enemyTemplateId,
			EnemyEncounterRank encounterRank,
			float scaleMultiplier)
		{
			ConfigureFinalThreat(target, enemyTemplateId, encounterRank, scaleMultiplier);
		}

        void AddCompanionRosterBaseline()
        {
            AddCompanionRoster("shield_guard", "shield_captain", "shield_captain", promotionEffectRef: "dmg_shield_captain_shockwave_v1");
            AddCompanionRoster("sword_soldier", "sword_captain", "sword_captain", promotionEffectRef: "dmg_sword_captain_crescent_v1");
            AddCompanionRoster("cleric", "light_guide", "light_guide", "cleric_family,healing_family", promotionEffectRef: "buff_light_guide_sanctuary_v1");
            AddCompanionRoster("falcon_archer", "falcon_captain", "falcon_captain", promotionEffectRef: "dmg_falcon_captain_dive_v1");
            AddCompanionRoster("field_herbalist", "battle_apothecary", "battle_apothecary", "ranged_family,healing_family", 2.0f, 1.55f, 0.90f, "status_battle_apothecary_spread_v1");
            AddCompanionRoster("bombardier", "powder_captain", "powder_captain", "ranged_family,explosive_family", 2.10f, 1.75f, 1.10f, "dmg_powder_captain_cluster_v1");
            AddCompanionRoster("fire_mage", "fire_sage", "fire_sage", "magic_family,explosive_family", 2.10f, 1.60f, 1.05f, "dmg_fire_sage_ignition_v1");
            AddCompanionRoster("lightning_mage", "storm_mage", "storm_mage", "magic_family,chain_family", 2.10f, 1.50f, 1.05f, "dmg_storm_mage_overload_v1");
            AddCompanionRoster("wolf_tamer", "beast_commander", "beast_commander", "beast_family,summon_family", 2.15f, 1.65f, 1.10f, "dmg_beast_commander_pack_assault_v1");
            AddCompanionRoster("wraith_knight", "wraith_guardian", "wraith_guardian", "undead_family,defense_family", 2.20f, 1.70f, 1.05f, "dmg_wraith_guardian_patrol_v1");
            AddCompanionRoster("necromancer", "dark_ritualist", "dark_ritualist", "undead_family,magic_family", 2.20f, 1.75f, 1.05f, "summon_dark_ritualist_group_v1");
            AddCompanionRoster("skeleton_scythe_thrower", "skeleton_reaper", "skeleton_reaper", "undead_family,explosive_family", 2.10f, 1.70f, 1.10f, "dmg_skeleton_reaper_orbit_v1");
            AddCompanionCombatProfile("shield_guard", 80, 2.8f, "shield_captain", "skill_shield_bash", "dmg_shield_bash_v1");
            AddCompanionCombatProfile("sword_soldier", 65, 3.0f, "sword_captain", "skill_sword_slash", "dmg_sword_slash_v1");
            AddCompanionCombatProfile("cleric", 55, 2.7f, "light_guide", "skill_cleric_bolt", "dmg_cleric_bolt_v1", "skill_cleric_heal", "heal_cleric_v1");
            AddCompanionCombatProfile("falcon_archer", 45, 2.9f, "falcon_captain", "skill_falcon_arrow", "dmg_falcon_arrow_v1", "skill_falcon_assist", "dmg_falcon_assist_v1");
            AddCompanionCombatProfile("field_herbalist", 50, 2.8f, "battle_apothecary");
            AddCompanionCombatProfile("bombardier", 50, 2.7f, "powder_captain", "skill_bomb_throw", "dmg_bomb_explosion_v1");
            AddCompanionCombatProfile("skeleton_scythe_thrower", 40, 2.6f, "skeleton_reaper", "skill_skeleton_scythe_throw", "dmg_skeleton_scythe_throw_v1");
            AddCompanionCombatProfile("fire_mage", 45, 2.6f, "fire_sage", "skill_fire_field", "dot_fire_field_v1");
            AddCompanionCombatProfile("lightning_mage", 45, 2.7f, "storm_mage", "skill_chain_lightning", "dmg_chain_lightning_v1");
            AddCompanionCombatProfile("wolf_tamer", 55, 3.0f, "beast_commander", "skill_wolf_assault", "dmg_wolf_assault_v1");
            AddCompanionCombatProfile("wraith_knight", 120, 2.6f, "wraith_guardian", "skill_wraith_slash", "dmg_wraith_slash_v1", "skill_wraith_guard", "dr_wraith_guard_v1");
            AddCompanionCombatProfile("necromancer", 50, 2.5f, "dark_ritualist", "skill_curse_bolt", "dmg_curse_bolt_v1", "", "");
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_shield_bash_v1",
                OwnerUnitId = "shield_guard",
                SkillId = "skill_shield_bash",
                EffectKind = CombatEffectKind.Damage,
                DeliveryKind = CombatDeliveryKind.Cone,
                BaseValue = 6.0f,
                CastInterval = 1.4f,
                Range = 1.2f,
                Angle = 60.0f,
                MaxTargets = 3,
                Push = 0.5f,
                TargetRule = CombatTargetRule.CommanderThreat,
                RuleId = "shield_bash",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_sword_slash_v1", OwnerUnitId = "sword_soldier", SkillId = "skill_sword_slash",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Cone,
                BaseValue = 12.0f, CastInterval = 1.0f, Range = 1.6f, Angle = 60.0f,
                MaxTargets = 5, TargetRule = CombatTargetRule.DensestCluster, RuleId = "sword_slash",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_cleric_bolt_v1", OwnerUnitId = "cleric", SkillId = "skill_cleric_bolt",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Projectile,
                BaseValue = 5.0f, CastInterval = 1.6f, Range = 4.5f,
                MaxTargets = 1, TargetRule = CombatTargetRule.Targeted, RuleId = "cleric_bolt",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "heal_cleric_v1", OwnerUnitId = "cleric", SkillId = "skill_cleric_heal",
                EffectKind = CombatEffectKind.Heal, DeliveryKind = CombatDeliveryKind.Projectile,
                BaseValue = 8.0f, CastInterval = 4.0f, Range = 4.0f,
                MaxTargets = 1, TargetRule = CombatTargetRule.Self, RuleId = "returning_light_commander_heal",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_falcon_arrow_v1", OwnerUnitId = "falcon_archer", SkillId = "skill_falcon_arrow",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Projectile,
                BaseValue = 9.0f, CastInterval = 0.9f, ProjectileLifetime = 0.8f, Range = 5.5f,
                MaxTargets = 3, TargetRule = CombatTargetRule.Nearest, RuleId = "piercing_arrow",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_falcon_assist_v1", OwnerUnitId = "falcon_archer", SkillId = "skill_falcon_assist",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Proxy,
                BaseValue = 6.0f, Range = 5.5f, MaxTargets = 1, TriggerCount = 4,
                TargetRule = CombatTargetRule.Nearest, RuleId = "falcon_visual_proxy_non_squad",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_herbal_dart_v1",
                OwnerUnitId = "field_herbalist",
                SkillId = "skill_herbal_dart",
                EffectKind = CombatEffectKind.Damage,
                DeliveryKind = CombatDeliveryKind.Circle,
                BaseValue = 8.0f,
                CastInterval = 1.4f,
                Range = 5.0f,
                Radius = 1.2f,
                MaxTargets = 4,
                CastDelay = 0.25f,
                TargetRule = CombatTargetRule.Targeted,
                StatusKind = CompanionEnemyStatusKind.Vulnerable,
                StatusMagnitude = 1.2f,
                StatusDuration = 3.0f,
            });
            AddCombatEffect(new CombatEffectData
            {
                Id="dot_fire_field_v1", OwnerUnitId="fire_mage", SkillId="skill_fire_field", EffectKind=CombatEffectKind.DamageOverTime, DeliveryKind=CombatDeliveryKind.Field,
                BaseValue=5, CastInterval=3.2f, Range=4.8f, Radius=1.6f, TickInterval=1.0f, Duration=3.0f, MaxTargets=8, MaxActiveCount=2, TargetRule=CombatTargetRule.Targeted,
            });
            AddCombatEffect(new CombatEffectData
            {
                Id="dmg_chain_lightning_v1", OwnerUnitId="lightning_mage", SkillId="skill_chain_lightning", EffectKind=CombatEffectKind.Damage, DeliveryKind=CombatDeliveryKind.Chain,
                BaseValue=12, CastInterval=2.6f, Range=5.0f, ChainDistance=1.8f, MaxTargets=3, TargetRule=CombatTargetRule.Targeted,
                StatusKind=CompanionEnemyStatusKind.Shock, StatusMagnitude=0.75f, StatusDuration=2.0f,
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_bomb_explosion_v1", OwnerUnitId = "bombardier", SkillId = "skill_bomb_throw",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Circle,
                BaseValue = 16.0f, CastInterval = 2.2f, Range = 5.0f, Radius = 1.6f,
                MaxTargets = 6, CastDelay = 0.5f, TargetRule = CombatTargetRule.DensestCluster,
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_skeleton_scythe_throw_v1", OwnerUnitId = "skeleton_scythe_thrower", SkillId = "skill_skeleton_scythe_throw",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.ReturningProjectile,
                BaseValue = 15.0f, CastInterval = 2.4f, Duration = 1.0f, Range = 4.8f, Radius = 0.75f,
                MaxTargets = 4, TargetRule = CombatTargetRule.Targeted,
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_wolf_assault_v1", OwnerUnitId = "wolf_tamer", SkillId = "skill_wolf_assault",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Proxy,
                BaseValue = 10.0f, CastInterval = 4.0f, Range = 4.0f, Radius = 2.0f, Duration = 0.8f,
                MaxTargets = 1, TriggerCount = 3, MaxActiveCount = 1, TargetRule = CombatTargetRule.LowestHealth,
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_wraith_slash_v1", OwnerUnitId = "wraith_knight", SkillId = "skill_wraith_slash",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Cone,
                BaseValue = 14.0f, CastInterval = 1.4f, Range = 1.2f, Angle = 60.0f,
                MaxTargets = 3, TargetRule = CombatTargetRule.CommanderThreat,
                StatusKind = CompanionEnemyStatusKind.Weakening, StatusMagnitude = 0.70f, StatusDuration = 3.0f,
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dr_wraith_guard_v1", OwnerUnitId = "wraith_knight", SkillId = "skill_wraith_guard",
                EffectKind = CombatEffectKind.DamageReduction, DeliveryKind = CombatDeliveryKind.Self,
                BaseValue = 0.60f, CastInterval = 5.0f, Duration = 1.2f,
                MaxTargets = 1, TargetRule = CombatTargetRule.Self,
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_curse_bolt_v1", OwnerUnitId = "necromancer", SkillId = "skill_curse_bolt",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Projectile,
                BaseValue = 8.0f, CastInterval = 3.0f, Range = 5.0f, Radius = 2.0f, Push = 0.8f,
                MaxTargets = 1, TargetRule = CombatTargetRule.Nearest,
                StatusKind = CompanionEnemyStatusKind.Curse, StatusMagnitude = 1.0f, StatusDuration = 4.0f,
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_shield_captain_shockwave_v1", OwnerUnitId = "shield_guard", SkillId = "skill_shield_captain_shockwave",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Circle,
                BaseValue = 1.0f, CastInterval = 6.0f, Radius = 2.5f, MaxTargets = 8, Push = 0.8f,
                TargetRule = CombatTargetRule.CommanderThreat, RuleId = "shield_captain_shockwave",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_sword_captain_crescent_v1", OwnerUnitId = "sword_soldier", SkillId = "skill_sword_captain_crescent",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Projectile,
                BaseValue = 1.0f, ProjectileLifetime = 0.85f, Range = 5.0f, Radius = 0.75f,
                MaxTargets = 4, TriggerCount = 3, TargetRule = CombatTargetRule.Targeted, RuleId = "sword_captain_crescent",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "buff_light_guide_sanctuary_v1", OwnerUnitId = "cleric", SkillId = "skill_light_guide_sanctuary",
                EffectKind = CombatEffectKind.AttackSpeed, DeliveryKind = CombatDeliveryKind.Field,
                BaseValue = 1.25f, Duration = 4.0f, Radius = 2.5f, MaxTargets = 8, TriggerCount = 3,
                TargetRule = CombatTargetRule.Self, RuleId = "light_guide_sanctuary",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_falcon_captain_dive_v1", OwnerUnitId = "falcon_archer", SkillId = "skill_falcon_captain_dive",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Proxy,
                BaseValue = 1.0f, MaxTargets = 1, TriggerCount = 3,
                TargetRule = CombatTargetRule.BossEliteHighestHealth, RuleId = "falcon_captain_dive",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "status_battle_apothecary_spread_v1", OwnerUnitId = "field_herbalist", SkillId = "skill_battle_apothecary_spread",
                EffectKind = CombatEffectKind.Status, DeliveryKind = CombatDeliveryKind.Circle,
                BaseValue = 1.2f, Radius = 2.0f, MaxTargets = 4, TriggerCount = 1, MaxActiveCount = 1,
                TargetRule = CombatTargetRule.Targeted, StatusKind = CompanionEnemyStatusKind.Vulnerable,
                StatusMagnitude = 1.2f, StatusDuration = 3.0f, RuleId = "battle_apothecary_vulnerability_spread",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_powder_captain_cluster_v1", OwnerUnitId = "bombardier", SkillId = "skill_powder_captain_cluster",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Circle,
                BaseValue = 1.0f, Range = 5.0f, Radius = 1.8f, ChainDistance = 1.2f,
                MaxTargets = 6, TriggerCount = 3, MaxActiveCount = 4,
                TargetRule = CombatTargetRule.DensestCluster, RuleId = "powder_captain_cluster_bomb",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_fire_sage_ignition_v1", OwnerUnitId = "fire_mage", SkillId = "skill_fire_sage_ignition",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Field,
                BaseValue = 1.0f, Duration = 1.5f, Range = 4.8f, MaxTargets = 8,
                TriggerCount = 3, MaxActiveCount = 2,
                TargetRule = CombatTargetRule.Targeted, RuleId = "fire_sage_active_field_ignition",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_storm_mage_overload_v1", OwnerUnitId = "lightning_mage", SkillId = "skill_storm_mage_overload",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Circle,
                BaseValue = 1.0f, Range = 5.0f, Radius = 1.2f, MaxTargets = 3, TriggerCount = 3,
                TargetRule = CombatTargetRule.Targeted, StatusKind = CompanionEnemyStatusKind.Shock,
                StatusMagnitude = 0.75f, StatusDuration = 2.0f, RuleId = "storm_mage_shock_overload",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_beast_commander_pack_assault_v1", OwnerUnitId = "wolf_tamer", SkillId = "skill_beast_commander_pack_assault",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Proxy, BaseValue = 1.0f,
                Range = 4.0f, MaxTargets = 1, TriggerCount = 3, MaxActiveCount = 3,
                TargetRule = CombatTargetRule.HighestHealth, RuleId = "beast_commander_pack_assault",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_wraith_guardian_patrol_v1", OwnerUnitId = "wraith_knight", SkillId = "skill_wraith_guardian_patrol",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Circle, BaseValue = 1.0f,
                Range = 3.0f, Radius = 0.6f, MaxTargets = 6, TriggerCount = 3, TargetRule = CombatTargetRule.Self,
                StatusKind = CompanionEnemyStatusKind.Weakening, StatusMagnitude = 0.7f, StatusDuration = 3.0f,
                RuleId = "wraith_guardian_orbit_patrol",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "summon_dark_ritualist_group_v1", OwnerUnitId = "necromancer", SkillId = "skill_dark_ritualist_ritual",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Proxy, BaseValue = 1.0f,
                Duration = 6.0f, Range = 5.0f, MaxTargets = 3, TriggerCount = 3, MaxActiveCount = 1,
                TargetRule = CombatTargetRule.Self, RuleId = "dark_ritualist_undead_ritual",
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "dmg_skeleton_reaper_orbit_v1", OwnerUnitId = "skeleton_scythe_thrower", SkillId = "skill_skeleton_reaper_orbit",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.Circle, BaseValue = 1.0f,
                Range = 4.0f, Radius = 0.75f, MaxTargets = 8, TriggerCount = 3,
                TargetRule = CombatTargetRule.Self, RuleId = "skeleton_reaper_orbit_scythe",
            });
        }

        void AddCompanionCombatProfile(string unitId, int baseHp, float moveSpeed, string promotionProfileId, string basicSkillId = "skill_herbal_dart", string basicEffectId = "dmg_herbal_dart_v1", string secondarySkillId = "", string secondaryEffectId = "")
        {
            CompanionCombatProfileData profile = new CompanionCombatProfileData
            {
                UnitId = unitId,
                BaseHp = baseHp,
                MoveSpeed = moveSpeed,
                BasicSkillId = basicSkillId,
                BasicEffectId = basicEffectId,
                SecondarySkillId = secondarySkillId,
                SecondaryEffectId = secondaryEffectId,
                PromotionProfileId = promotionProfileId,
                NoTargetRetrySeconds = 0.15f,
            };
            _companionCombatProfiles.Add(profile);
            _companionCombatProfilesByUnitId.Add(unitId, profile);
        }

        void AddCombatEffect(CombatEffectData data)
        {
            _combatEffects.Add(data);
            _combatEffectsById.Add(data.Id, data);
        }

        void AddCompanionRoster(
            string unitId,
            string profileId,
            string promotedUnitId,
            string familyTags = "test_family",
            float hpMultiplier = 2.0f,
            float effectMultiplier = 2.0f,
            float intervalMultiplier = 1.0f,
            string promotionEffectRef = "")
        {
            CompanionRosterData roster = new CompanionRosterData
            {
                UnitId = unitId,
                FamilyTags = familyTags,
                SkillId = "test_skill",
                EffectRef = "test_effect",
                PromotionEffectRef = promotionEffectRef,
                PromotionContractStage = string.IsNullOrEmpty(promotionEffectRef)
                    ? CompanionCombatContractStage.Skeleton
                    : CompanionCombatContractStage.RuntimeConnected,
                PromotionProfileId = profileId,
                RecruitTitleKey = "test.title",
                RecruitDescKey = "test.desc",
            };
            _companionRoster.Add(roster);
            _companionRosterByUnitId.Add(unitId, roster);
            _companionPromotionsByProfileId.Add(profileId, new CompanionPromotionData
            {
                ProfileId = profileId,
                BaseUnitId = unitId,
                PromotedUnitId = promotedUnitId,
                DisplayName = promotedUnitId,
                HpMultiplier = hpMultiplier,
                EffectMultiplier = effectMultiplier,
                IntervalMultiplier = intervalMultiplier,
                PrefabId = "test_prefab",
                CardKey = "test.card",
                RequiredUnitCount = 3,
                VisualUnitCount = 3,
            });
        }

        void EnsureInitialized()
        {
            if (!_initialized) throw new InvalidOperationException("[FakeDataProvider] Provider is not initialized.");
        }

        static T Find<T>(Dictionary<string, T> values, string id) { return values.TryGetValue(id, out T value) ? value : default; }

        DataLoadResult CreateResult(bool succeeded, string source)
        {
            DataLoadResult result = new DataLoadResult();
            SetInternal(result, "Succeeded", succeeded);
            SetInternal(result, "Source", source);
            foreach (string id in _missingIds) result.MissingRequiredIds.Add(id);
            return result;
        }

        static void SetInternal(DataLoadResult result, string propertyName, object value)
        {
            PropertyInfo property = typeof(DataLoadResult).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            property?.GetSetMethod(true)?.Invoke(result, new[] { value });
        }
    }
}
