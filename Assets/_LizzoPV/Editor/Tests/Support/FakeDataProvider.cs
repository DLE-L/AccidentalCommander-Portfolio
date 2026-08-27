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
        readonly List<PassiveData> _passives = new List<PassiveData>();
        readonly Dictionary<string, PassiveData> _passivesById = new Dictionary<string, PassiveData>();
        readonly IReadOnlyList<PassiveData> _passiveView;
        readonly List<CompanionCombatProfileData> _companionCombatProfiles = new List<CompanionCombatProfileData>();
        readonly Dictionary<string, CompanionCombatProfileData> _companionCombatProfilesByUnitId = new Dictionary<string, CompanionCombatProfileData>();
        readonly List<CombatEffectData> _combatEffects = new List<CombatEffectData>();
        readonly Dictionary<string, CombatEffectData> _combatEffectsById = new Dictionary<string, CombatEffectData>();
        readonly IReadOnlyList<CompanionCombatProfileData> _companionCombatProfileView;
        readonly IReadOnlyList<CombatEffectData> _combatEffectView;
        readonly List<CompanionSummonData> _companionSummons = new List<CompanionSummonData>();
        readonly Dictionary<string, CompanionSummonData> _companionSummonsById = new Dictionary<string, CompanionSummonData>();
        readonly IReadOnlyList<CompanionSummonData> _companionSummonView;
        readonly List<SynergyDamageData> _synergyDamages = new List<SynergyDamageData>();
        readonly Dictionary<string, SynergyDamageData> _synergyDamagesById = new Dictionary<string, SynergyDamageData>();
        readonly List<SynergyEffectData> _synergyEffects = new List<SynergyEffectData>();
        readonly Dictionary<string, SynergyEffectData> _synergyEffectsById = new Dictionary<string, SynergyEffectData>();
        readonly List<SynergySummonData> _synergySummons = new List<SynergySummonData>();
        readonly Dictionary<string, SynergySummonData> _synergySummonsById = new Dictionary<string, SynergySummonData>();
        readonly IReadOnlyList<SynergyDamageData> _synergyDamageView;
        readonly IReadOnlyList<SynergyEffectData> _synergyEffectView;
        readonly IReadOnlyList<SynergySummonData> _synergySummonView;
        readonly List<string> _missingIds = new List<string>();
        readonly RunTuningData _runTuning = new RunTuningData();
        bool _initialized;
        bool _failInitialization;

        public FakeDataProvider()
        {
            _companionRosterView = _companionRoster.AsReadOnly();
            _companionCardLocalizationView = _companionCardLocalizations.AsReadOnly();
            _passiveView = _passives.AsReadOnly();
            _companionCombatProfileView = _companionCombatProfiles.AsReadOnly();
            _combatEffectView = _combatEffects.AsReadOnly();
            _companionSummonView = _companionSummons.AsReadOnly();
            _synergyDamageView = _synergyDamages.AsReadOnly();
            _synergyEffectView = _synergyEffects.AsReadOnly();
            _synergySummonView = _synergySummons.AsReadOnly();
            AddBaseline();
            AddCompanionRosterBaseline();
        }
        public bool IsInitialized => _initialized;
        public RunTuningData RunTuning { get { EnsureInitialized(); return _runTuning; } }
        public IReadOnlyList<CompanionRosterData> CompanionRoster { get { EnsureInitialized(); return _companionRosterView; } }
        public IReadOnlyList<CompanionCardLocalizationData> CompanionCardLocalizations { get { EnsureInitialized(); return _companionCardLocalizationView; } }
        public IReadOnlyList<PassiveData> Passives { get { EnsureInitialized(); return _passiveView; } }
        public IReadOnlyList<CompanionCombatProfileData> CompanionCombatProfiles { get { EnsureInitialized(); return _companionCombatProfileView; } }
        public IReadOnlyList<CombatEffectData> CombatEffects { get { EnsureInitialized(); return _combatEffectView; } }
        public IReadOnlyList<CompanionSummonData> CompanionSummons { get { EnsureInitialized(); return _companionSummonView; } }
        public IReadOnlyList<SynergyDamageData> SynergyDamages { get { EnsureInitialized(); return _synergyDamageView; } }
        public IReadOnlyList<SynergyEffectData> SynergyEffects { get { EnsureInitialized(); return _synergyEffectView; } }
        public IReadOnlyList<SynergySummonData> SynergySummons { get { EnsureInitialized(); return _synergySummonView; } }

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

        public FakeDataProvider SetPassive(PassiveData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Id)) return this;
            if (_passivesById.TryGetValue(data.Id, out PassiveData existing)) _passives.Remove(existing);
            _passivesById[data.Id] = data;
            _passives.Add(data);
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

        public FakeDataProvider SetSynergyDamage(SynergyDamageData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Id)) return this;
            if (_synergyDamagesById.TryGetValue(data.Id, out SynergyDamageData existing))
                _synergyDamages.Remove(existing);
            _synergyDamagesById[data.Id] = data;
            _synergyDamages.Add(data);
            return this;
        }

        public FakeDataProvider SetSynergyEffect(SynergyEffectData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Id)) return this;
            if (_synergyEffectsById.TryGetValue(data.Id, out SynergyEffectData existing))
                _synergyEffects.Remove(existing);
            _synergyEffectsById[data.Id] = data;
            _synergyEffects.Add(data);
            return this;
        }

        public FakeDataProvider SetSynergySummon(SynergySummonData data)
        {
            if (data == null || string.IsNullOrEmpty(data.Id)) return this;
            if (_synergySummonsById.TryGetValue(data.Id, out SynergySummonData existing))
                _synergySummons.Remove(existing);
            _synergySummonsById[data.Id] = data;
            _synergySummons.Add(data);
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
        public PassiveData GetPassive(string passiveId) { EnsureInitialized(); return _passivesById.TryGetValue(passiveId, out PassiveData value) ? value : null; }
        public CompanionPromotionData GetCompanionPromotion(string profileId) { EnsureInitialized(); return _companionPromotionsByProfileId.TryGetValue(profileId, out CompanionPromotionData value) ? value : null; }
        public CompanionCombatProfileData GetCompanionCombatProfile(string unitId) { EnsureInitialized(); return _companionCombatProfilesByUnitId.TryGetValue(unitId, out CompanionCombatProfileData value) ? value : null; }
        public CombatEffectData GetCombatEffect(string effectId) { EnsureInitialized(); return _combatEffectsById.TryGetValue(effectId, out CombatEffectData value) ? value : null; }
        public CompanionSummonData GetCompanionSummon(string summonId) { EnsureInitialized(); return _companionSummonsById.TryGetValue(summonId, out CompanionSummonData value) ? value : null; }
        public SynergyDamageData GetSynergyDamage(string damageId) { EnsureInitialized(); return _synergyDamagesById.TryGetValue(damageId, out SynergyDamageData value) ? value : null; }
        public SynergyEffectData GetSynergyEffect(string effectId) { EnsureInitialized(); return _synergyEffectsById.TryGetValue(effectId, out SynergyEffectData value) ? value : null; }
        public SynergySummonData GetSynergySummon(string summonId) { EnsureInitialized(); return _synergySummonsById.TryGetValue(summonId, out SynergySummonData value) ? value : null; }
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
            SetUnit(new UnitData { Id = "commander_01", SkillId = "commander_basic", Hp = 100, MoveSpeed = 5.0f });
            SetUnit(new UnitData { Id = "shield_guard", SkillId = "shield_push", Hp = 80 });
            SetUnit(new UnitData { Id = "shield_captain", SkillId = "shield_captain_push", Hp = 120 });
            SetUnit(new UnitData { Id = "sword_soldier", SkillId = "sword_front_slash", Hp = 90 });
            SetUnit(new UnitData { Id = "cleric", SkillId = "cleric_heal", Hp = 70 });
            SetUnit(new UnitData { Id = "archer", SkillId = "archer_far_shot", Hp = 60 });
            foreach (string id in new[] { "commander_basic", "shield_push", "shield_captain_push", "sword_front_slash", "cleric_heal", "archer_far_shot", "guard_squad_shield" }) SetSkill(new SkillData { Id = id, Power = 10 });
            SetEnemy(new EnemyData { Id = "small_goblin", TemplateId = 1, Hp = 30, SpawnSeconds = 0.0f });
            SetEnemy(new EnemyData { Id = "hungry_wolf", TemplateId = 2, Hp = 50, SpawnSeconds = 20.0f });
            SetEnemy(new EnemyData { Id = "shield_orc", TemplateId = 3, Hp = 100, SpawnSeconds = 40.0f });
            SetEnemy(new EnemyData { Id = "elite_red_charger", TemplateId = 4, Hp = 500, SpawnSeconds = 150.0f });
            SetEnemy(new EnemyData { Id = "boss_hungry_giant", TemplateId = 5, Hp = 2500, SpawnSeconds = 300.0f });
            SetSynergy(new SynergyData { Id = "guard_squad", ShieldDurability = 80 });
            AddSynergyCombatBaseline();
            SetSynergySummon(CreateCanonicalSynergySkeleton());
            SetLevelExp(1, _runTuning.FirstLevelExp);
        }

        void AddSynergyCombatBaseline()
        {
            SetSynergyDamage(CreateSynergyDamage("DMG_SYNERGY_GUARD_01", "synergy_guard_shockwave", 18.0f, 12.0f, 0.0f, 0.0f, 0.0f, 3.5f, 120.0f, 6, 0, 0, 0, 0, 0.01f, 0.0f, 0.0f, 0.0f, false, false, false, false, false, false, false, false, "sector", "nearest", "per_cast_max_hp_cap", string.Empty, "battle_end", "rc_synergy_guard_damage"));
            SetSynergyDamage(CreateSynergyDamage("DMG_BUILD1_GUARD_READY_01", "synergy_guard_shockwave", 0.0f, 15.0f, 0.0f, 0.0f, 0.0f, 1.2f, 60.0f, 3, 0, 0, 0, 0, 0.0f, 0.5f, 0.0f, 0.0f, false, false, false, false, false, false, false, false, "cone", "frontal", "no_damage", string.Empty, "battle_end", "rc_build1_guard_ready"));
            SetSynergyDamage(CreateSynergyDamage("DMG_SYNERGY_ARCHER_01", "synergy_archer_rain", 14.0f, 8.0f, 0.0f, 0.0f, 0.0f, 2.0f, 0.0f, 8, 0, 0, 0, 0, 0.0f, 0.0f, 0.8f, 2.0f, false, false, false, false, false, false, false, false, "circle", "strongest_only", "normal", string.Empty, string.Empty, "rc_synergy_archer_damage"));
            SetSynergyDamage(CreateSynergyDamage("DMG_SYNERGY_MAGIC_01", "synergy_magic_chain", 10.0f, 0.0f, 0.0f, 0.0f, 2.0f, 0.0f, 0.0f, 5, 5, 3, 0, 0, 0.0025f, 0.0f, 0.0f, 0.0f, true, true, false, false, false, false, false, false, "projectile", "same_target_duplicates_allowed", "per_projectile_max_hp_cap", string.Empty, string.Empty, "rc_synergy_magic_damage"));
            SetSynergyDamage(CreateSynergyDamage("DMG_SYNERGY_EXPLOSION_01", "synergy_explosion_chain", 20.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.8f, 0.0f, 8, 0, 0, 8, 1, 0.008f, 0.0f, 0.0f, 0.0f, false, false, true, true, false, false, false, false, "circle", "death_origin", "per_cast_max_hp_cap", string.Empty, string.Empty, "rc_synergy_explosion_damage"));
            SetSynergyDamage(CreateSynergyDamage("DMG_BUILD1_EXPLOSIVE_READY_01", "synergy_explosion_chain", 10.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.6f, 0.0f, 6, 0, 0, 12, 0, 0.008f, 0.0f, 0.0f, 0.0f, false, false, true, false, false, false, false, false, "circle", "lethal_origin", "per_cast_max_hp_cap", string.Empty, "battle_end", "rc_build1_explosive_ready"));
            SetSynergyDamage(CreateSynergyDamage("DMG_SYNERGY_BEAST_01", "synergy_beast_hunt", 8.0f, 10.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1, 0, 0, 0, 0, 0.004f, 0.0f, 0.0f, 0.0f, false, false, false, false, true, true, true, false, "direct", "common_target", "per_caster_max_hp_cap", string.Empty, string.Empty, "rc_synergy_beast_damage"));
            SetSynergyDamage(CreateSynergyDamage("DOT_SYNERGY_BEAST_01", "synergy_beast_hunt", 3.0f, 0.0f, 1.0f, 5.0f, 0.0f, 0.0f, 0.0f, 1, 0, 0, 0, 0, 0.001f, 0.0f, 0.0f, 0.0f, false, false, false, false, false, false, false, true, "dot", "boss_target", "per_tick_max_hp_cap", "max_one_refresh_duration", string.Empty, "rc_synergy_beast_bleed"));

            SetSynergyEffect(CreateSynergyEffect("EFFECT_SYNERGY_GUARD_DR", "synergy_guard_shockwave", 0.75f, 0.25f, 0.0f, 6.0f, 0.0f, 0.0f, 0.0f, 0.60f, false, true, true, true, false, true, false, false, false, false, "on_cast", "same_source_refresh_no_numeric_stack", "rc_guard_companion_dr"));
            SetSynergyEffect(CreateSynergyEffect("EFFECT_HEALING_BOND_DR", "synergy_healing_bond", 0.8f, 0.2f, 0.0f, 3.0f, 2.5f, 0.0f, 0.0f, 0.0f, true, false, true, true, false, false, false, true, true, true, "zone_membership", "new_replaces_old_no_stack", "rc_healing_bond_dr"));
            SetSynergyEffect(CreateSynergyEffect("EFFECT_MIXED_COMMAND", "synergy_mixed_command", 0.0f, 0.0f, 15.0f, 5.0f, 0.0f, 1.15f, 1.15f, 0.0f, false, true, true, false, true, true, false, false, false, false, "all_alive_companions", "same_source_refresh_no_multiplier_stack", "rc_mixed_command_multiplier"));
            SetSynergyEffect(CreateSynergyEffect("EFFECT_BUILD1_MIXED_READY_01", "synergy_mixed_command", 0.0f, 0.0f, 18.0f, 3.0f, 0.0f, 1.0f, 1.12f, 0.0f, false, true, true, true, true, true, false, false, false, false, "all_living_companions", "same_source_refresh_no_multiplier_stack", "rc_build1_mixed_ready"));
        }

        static SynergyDamageData CreateSynergyDamage(params object[] arguments)
        {
            return CreateInternalData<SynergyDamageData>(arguments);
        }

        static SynergyEffectData CreateSynergyEffect(params object[] arguments)
        {
            return CreateInternalData<SynergyEffectData>(arguments);
        }

        static T CreateInternalData<T>(object[] arguments) where T : class
        {
            ConstructorInfo[] constructors = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
            if (constructors.Length != 1)
                throw new InvalidOperationException($"[FakeDataProvider] {typeof(T).Name} constructor is unavailable.");
            return (T)constructors[0].Invoke(arguments);
        }

        static SynergySummonData CreateCanonicalSynergySkeleton()
        {
            ConstructorInfo constructor = typeof(SynergySummonData).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(string), typeof(string), typeof(int), typeof(int), typeof(float), typeof(float), typeof(float),
                    typeof(float), typeof(string), typeof(CombatTargetRule), typeof(int), typeof(int), typeof(int),
                    typeof(string), typeof(string), typeof(string), typeof(string),
                },
                null);
            if (constructor == null)
                throw new InvalidOperationException("[FakeDataProvider] SynergySummonData constructor is unavailable.");

            return (SynergySummonData)constructor.Invoke(new object[]
            {
                "UNIT_SYNERGY_SKELETON_01", "synergy_undead_summon", 22, 5, 1.2f, 1.0f, 2.8f, 0.2f,
                "battle_end_or_hp0", CombatTargetRule.Nearest, 5, 3, 1,
                "summon_object,companion_tag=false,no_family_tag", "battle_end", "rc_synergy_skeleton_stats",
                "UNIT_PERSONAL_SKELETON_01",
            });
        }

        void AddCompanionRosterBaseline()
        {
            AddCompanionRoster("shield_guard", "shield_captain", "shield_captain");
            AddCompanionRoster("sword_soldier", "sword_captain", "sword_captain");
            AddCompanionRoster("cleric", "light_guide", "light_guide", "cleric_family,healing_family");
            AddCompanionRoster("falcon_archer", "falcon_captain", "falcon_captain");
            AddCompanionRoster("field_herbalist", "battle_apothecary", "battle_apothecary", "ranged_family,healing_family", 2.0f, 1.55f, 0.90f);
            AddCompanionRoster("bombardier", "powder_captain", "powder_captain", "ranged_family,explosive_family", 2.10f, 1.75f, 1.10f);
            AddCompanionRoster("fire_mage", "fire_sage", "fire_sage", "magic_family,explosive_family", 2.10f, 1.60f, 1.05f);
            AddCompanionRoster("lightning_mage", "storm_mage", "storm_mage", "magic_family,chain_family", 2.10f, 1.50f, 1.05f);
            AddCompanionRoster("wolf_tamer", "beast_commander", "beast_commander", "beast_family,summon_family", 2.15f, 1.65f, 1.10f);
            AddCompanionRoster("wraith_knight", "wraith_guardian", "wraith_guardian", "undead_family,defense_family", 2.20f, 1.70f, 1.05f);
            AddCompanionRoster("necromancer", "dark_ritualist", "dark_ritualist", "undead_family,magic_family", 2.20f, 1.75f, 1.05f);
            AddCompanionRoster("skeleton_bomber", "bone_artillery", "bone_artillery", "undead_family,explosive_family", 2.10f, 1.70f, 1.10f);
            AddCompanionCombatProfile("field_herbalist", 50, 2.8f, "battle_apothecary");
            AddCompanionCombatProfile("bombardier", 50, 2.7f, "powder_captain", "skill_bomb_throw", "dmg_bomb_explosion_v1");
            AddCompanionCombatProfile("skeleton_bomber", 40, 2.6f, "bone_artillery", "skill_skeleton_bomb", "dmg_skeleton_bomb_v1");
            AddCompanionCombatProfile("fire_mage", 45, 2.6f, "fire_sage", "skill_fire_field", "dot_fire_field_v1");
            AddCompanionCombatProfile("lightning_mage", 45, 2.7f, "storm_mage", "skill_chain_lightning", "dmg_chain_lightning_v1");
            AddCompanionCombatProfile("wolf_tamer", 55, 3.0f, "beast_commander", "skill_wolf_assault", "dmg_wolf_assault_v1");
            AddCompanionCombatProfile("wraith_knight", 120, 2.6f, "wraith_guardian", "skill_wraith_slash", "dmg_wraith_slash_v1", "skill_wraith_guard", "dr_wraith_guard_v1");
            AddCompanionCombatProfile("necromancer", 50, 2.5f, "dark_ritualist", "skill_curse_bolt", "dmg_curse_bolt_v1", "skill_personal_thrall", "");
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
                Id = "dmg_skeleton_bomb_v1", OwnerUnitId = "skeleton_bomber", SkillId = "skill_skeleton_bomb",
                EffectKind = CombatEffectKind.Damage, DeliveryKind = CombatDeliveryKind.ReturningProjectile,
                BaseValue = 15.0f, CastInterval = 2.4f, Duration = 1.0f, Range = 4.8f, Radius = 0.75f,
                MaxTargets = 4, TargetRule = CombatTargetRule.Targeted,
            });
            AddCombatEffect(new CombatEffectData
            {
                Id = "heal_herbal_aid_v1",
                OwnerUnitId = "field_herbalist",
                SkillId = "skill_herbal_aid",
                EffectKind = CombatEffectKind.Heal,
                DeliveryKind = CombatDeliveryKind.Projectile,
                BaseValue = 4.0f,
                CastInterval = 6.0f,
                Range = 4.0f,
                MaxTargets = 1,
                TargetRule = CombatTargetRule.LowestHealthNoRevive,
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
        }

        void AddCompanionCombatProfile(string unitId, int baseHp, float moveSpeed, string promotionProfileId, string basicSkillId = "skill_herbal_dart", string basicEffectId = "dmg_herbal_dart_v1", string secondarySkillId = "skill_herbal_aid", string secondaryEffectId = "heal_herbal_aid_v1")
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
            float intervalMultiplier = 1.0f)
        {
            CompanionRosterData roster = new CompanionRosterData
            {
                UnitId = unitId,
                FamilyTags = familyTags,
                SkillId = "test_skill",
                EffectRef = "test_effect",
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
