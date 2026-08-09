using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider : IDataProvider
    {
        const string DATA_ADDRESS = "PlayerData.xml";
        readonly IAssetService _assets;
        readonly Dictionary<string, UnitData> Units = new Dictionary<string, UnitData>();
        readonly Dictionary<string, SkillData> Skills = new Dictionary<string, SkillData>();
        readonly Dictionary<string, EnemyData> Enemies = new Dictionary<string, EnemyData>();
        readonly Dictionary<int, EnemyData> EnemiesByTemplateId = new Dictionary<int, EnemyData>();
        readonly Dictionary<string, SynergyData> Synergies = new Dictionary<string, SynergyData>();
        readonly Dictionary<int, int> LevelExp = new Dictionary<int, int>();
        readonly List<CompanionRosterData> _companionRoster = new List<CompanionRosterData>();
        readonly Dictionary<string, CompanionRosterData> _companionRosterByUnitId = new Dictionary<string, CompanionRosterData>();
        readonly Dictionary<string, CompanionPromotionData> _companionPromotionsByProfileId = new Dictionary<string, CompanionPromotionData>();
        readonly List<CompanionCardLocalizationData> _companionCardLocalizations = new List<CompanionCardLocalizationData>();
        readonly Dictionary<string, CompanionCardLocalizationData> _companionCardLocalizationsByUnitId = new Dictionary<string, CompanionCardLocalizationData>();
        readonly List<PassiveData> _passives = new List<PassiveData>();
        readonly Dictionary<string, PassiveData> _passivesById = new Dictionary<string, PassiveData>();
        readonly List<CompanionCombatProfileData> _companionCombatProfiles = new List<CompanionCombatProfileData>();
        readonly Dictionary<string, CompanionCombatProfileData> _companionCombatProfilesByUnitId = new Dictionary<string, CompanionCombatProfileData>();
        readonly List<CombatEffectData> _combatEffects = new List<CombatEffectData>();
        readonly Dictionary<string, CombatEffectData> _combatEffectsById = new Dictionary<string, CombatEffectData>();
        readonly List<CompanionSummonData> _companionSummons = new List<CompanionSummonData>();
        readonly Dictionary<string, CompanionSummonData> _companionSummonsById = new Dictionary<string, CompanionSummonData>();
        readonly List<SynergyDamageData> _synergyDamages = new List<SynergyDamageData>();
        readonly Dictionary<string, SynergyDamageData> _synergyDamagesById = new Dictionary<string, SynergyDamageData>();
        readonly List<SynergyEffectData> _synergyEffects = new List<SynergyEffectData>();
        readonly Dictionary<string, SynergyEffectData> _synergyEffectsById = new Dictionary<string, SynergyEffectData>();
        readonly List<SynergySummonData> _synergySummons = new List<SynergySummonData>();
        readonly Dictionary<string, SynergySummonData> _synergySummonsById = new Dictionary<string, SynergySummonData>();
        readonly List<string> _companionCatalogValidationErrors = new List<string>();
        readonly IReadOnlyList<CompanionRosterData> _companionRosterView;
        readonly IReadOnlyList<CompanionCardLocalizationData> _companionCardLocalizationView;
        readonly IReadOnlyList<PassiveData> _passiveView;
        readonly IReadOnlyList<CompanionCombatProfileData> _companionCombatProfileView;
        readonly IReadOnlyList<CombatEffectData> _combatEffectView;
        readonly IReadOnlyList<CompanionSummonData> _companionSummonView;
        readonly IReadOnlyList<SynergyDamageData> _synergyDamageView;
        readonly IReadOnlyList<SynergyEffectData> _synergyEffectView;
        readonly IReadOnlyList<SynergySummonData> _synergySummonView;
        RunTuningData _runTuning = new RunTuningData();
        DataLoadResult _lastResult;
        bool _initialized;

        public LocalDataProvider(IAssetService assets)
        {
            _assets = assets ?? throw new ArgumentNullException(nameof(assets));
            _companionRosterView = _companionRoster.AsReadOnly();
            _companionCardLocalizationView = _companionCardLocalizations.AsReadOnly();
            _passiveView = _passives.AsReadOnly();
            _companionCombatProfileView = _companionCombatProfiles.AsReadOnly();
            _combatEffectView = _combatEffects.AsReadOnly();
            _companionSummonView = _companionSummons.AsReadOnly();
            _synergyDamageView = _synergyDamages.AsReadOnly();
            _synergyEffectView = _synergyEffects.AsReadOnly();
            _synergySummonView = _synergySummons.AsReadOnly();
        }

        public bool IsInitialized => _initialized;
        public IReadOnlyList<CompanionRosterData> CompanionRoster
        {
            get
            {
                EnsureInitialized();
                return _companionRosterView;
            }
        }

        public IReadOnlyList<CompanionCardLocalizationData> CompanionCardLocalizations
        {
            get
            {
                EnsureInitialized();
                return _companionCardLocalizationView;
            }
        }

        public IReadOnlyList<PassiveData> Passives
        {
            get { EnsureInitialized(); return _passiveView; }
        }

        public IReadOnlyList<CompanionCombatProfileData> CompanionCombatProfiles
        {
            get
            {
                EnsureInitialized();
                return _companionCombatProfileView;
            }
        }

        public IReadOnlyList<CombatEffectData> CombatEffects
        {
            get
            {
                EnsureInitialized();
                return _combatEffectView;
            }
        }

        public IReadOnlyList<CompanionSummonData> CompanionSummons
        {
            get
            {
                EnsureInitialized();
                return _companionSummonView;
            }
        }

        public IReadOnlyList<SynergyDamageData> SynergyDamages
        {
            get { EnsureInitialized(); return _synergyDamageView; }
        }

        public IReadOnlyList<SynergyEffectData> SynergyEffects
        {
            get { EnsureInitialized(); return _synergyEffectView; }
        }

        public IReadOnlyList<SynergySummonData> SynergySummons
        {
            get { EnsureInitialized(); return _synergySummonView; }
        }

        public RunTuningData RunTuning
        {
            get
            {
                EnsureInitialized();
                return _runTuning;
            }
        }

        public async UniTask<DataLoadResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
                return _lastResult;

            var result = new DataLoadResult
            {
                Source = "local_xml",
                UsedFallback = true
            };

            LoadFallbackData();

            try
            {
                TextAsset textAsset = await _assets.LoadAsync<TextAsset>(DATA_ADDRESS, cancellationToken);
                if (textAsset == null)
                {
                    result.ParseError = "Local data asset was not available.";
                    Debug.LogError($"[LocalDataProvider] {result.ParseError} address={DATA_ADDRESS}");
                }
                else
                {
                    XDocument document = XDocument.Parse(textAsset.text);
                    XElement root = document.Root;
                    if (root == null)
                    {
                        result.ParseError = "Local data XML has no root element.";
                        Debug.LogError($"[LocalDataProvider] {result.ParseError}");
                    }
                    else
                    {
                        LoadRunTuning(root.Element("RunTuning"));
                        LoadLevelExp(root.Element("LevelExpDatas"));
                        LoadUnits(root.Element("UnitDatas"));
                        LoadSkills(root.Element("SkillDatas"));
                        LoadEnemies(root.Element("EnemyDatas"));
                        LoadSynergies(root.Element("SynergyDatas"));
                        ResetCompanionCatalog();
                        LoadCompanionRoster(root.Element("CompanionRosterDatas"));
                        LoadCompanionPromotions(root.Element("CompanionPromotionDatas"));
                        LoadCompanionCardLocalizations(root.Element("CompanionCardLocalizationDatas"));
                        LoadPassives(root.Element("PassiveDatas"));
                        LoadCompanionCombatProfiles(root.Element("CompanionCombatProfileDatas"));
                        LoadCombatEffects(root.Element("CombatEffectDatas"));
                        LoadCompanionSummons(root.Element("CompanionSummonDatas"));
                        LoadSynergyCombatCatalog(root);
                        result.UsedFallback = false;
                    }
                }
            }
            catch (Exception exception)
            {
                result.ParseError = exception.Message;
                Debug.LogError($"[LocalDataProvider] XML parse failed: {exception}");
            }

            ValidateRequiredData(result);
            result.Succeeded = result.MissingRequiredIds.Count == 0;
            _lastResult = result;
            _initialized = true;

            if (!result.Succeeded)
                Debug.LogError($"[LocalDataProvider] Required data missing: {string.Join(", ", result.MissingRequiredIds)}");

            return result;
        }

        public UnitData GetUnit(string id)
        {
            EnsureInitialized();
            return Units.TryGetValue(id, out UnitData data) ? data : null;
        }

        public CompanionRosterData GetCompanionRoster(string unitId)
        {
            EnsureInitialized();
            return _companionRosterByUnitId.TryGetValue(unitId, out CompanionRosterData data) ? data : null;
        }

        public CompanionPromotionData GetCompanionPromotion(string profileId)
        {
            EnsureInitialized();
            return _companionPromotionsByProfileId.TryGetValue(profileId, out CompanionPromotionData data) ? data : null;
        }

        public CompanionCardLocalizationData GetCompanionCardLocalization(string unitId)
        {
            EnsureInitialized();
            return _companionCardLocalizationsByUnitId.TryGetValue(unitId, out CompanionCardLocalizationData data) ? data : null;
        }

        public PassiveData GetPassive(string passiveId)
        {
            EnsureInitialized();
            return _passivesById.TryGetValue(passiveId, out PassiveData data) ? data : null;
        }

        public CompanionCombatProfileData GetCompanionCombatProfile(string unitId)
        {
            EnsureInitialized();
            return _companionCombatProfilesByUnitId.TryGetValue(unitId, out CompanionCombatProfileData data) ? data : null;
        }

        public CombatEffectData GetCombatEffect(string effectId)
        {
            EnsureInitialized();
            return _combatEffectsById.TryGetValue(effectId, out CombatEffectData data) ? data : null;
        }

        public CompanionSummonData GetCompanionSummon(string summonId)
        {
            EnsureInitialized();
            return _companionSummonsById.TryGetValue(summonId, out CompanionSummonData data) ? data : null;
        }

        public SynergyDamageData GetSynergyDamage(string damageId)
        {
            EnsureInitialized();
            return _synergyDamagesById.TryGetValue(damageId, out SynergyDamageData data) ? data : null;
        }

        public SynergyEffectData GetSynergyEffect(string effectId)
        {
            EnsureInitialized();
            return _synergyEffectsById.TryGetValue(effectId, out SynergyEffectData data) ? data : null;
        }

        public SynergySummonData GetSynergySummon(string summonId)
        {
            EnsureInitialized();
            return _synergySummonsById.TryGetValue(summonId, out SynergySummonData data) ? data : null;
        }

        public SkillData GetSkill(string id)
        {
            EnsureInitialized();
            return Skills.TryGetValue(id, out SkillData data) ? data : null;
        }

        public EnemyData GetEnemy(string id)
        {
            EnsureInitialized();
            return Enemies.TryGetValue(id, out EnemyData data) ? data : null;
        }

        public EnemyData GetEnemyByTemplateId(int templateId)
        {
            EnsureInitialized();
            return EnemiesByTemplateId.TryGetValue(templateId, out EnemyData data) ? data : null;
        }

        public SynergyData GetSynergy(string id)
        {
            EnsureInitialized();
            return Synergies.TryGetValue(id, out SynergyData data) ? data : null;
        }

        public int GetLevelExp(int level)
        {
            EnsureInitialized();
            if (LevelExp.TryGetValue(level, out int exp))
                return exp;
            return level <= 1 ? _runTuning.FirstLevelExp : _runTuning.FirstLevelExp + (level - 1) * 14;
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

        void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException("[LocalDataProvider] Provider is not initialized.");
        }

        void ValidateRequiredData(DataLoadResult result)
        {
            string[] units = { "commander_01", "shield_guard", "shield_captain", "sword_soldier", "cleric", "archer" };
            string[] skills = { "commander_basic", "shield_push", "shield_captain_push", "sword_front_slash", "cleric_heal", "archer_far_shot", "guard_squad_shield" };
            string[] enemies = { "small_goblin", "hungry_wolf", "shield_orc", "elite_red_charger", "boss_hungry_giant" };
            foreach (string id in units) if (!Units.ContainsKey(id)) result.MissingRequiredIds.Add($"unit:{id}");
            foreach (string id in skills) if (!Skills.ContainsKey(id)) result.MissingRequiredIds.Add($"skill:{id}");
            foreach (string id in enemies) if (!Enemies.ContainsKey(id)) result.MissingRequiredIds.Add($"enemy:{id}");
            string[] requiredSynergyPresentationIds =
            {
                "guard_squad",
                "synergy_guard_shockwave",
                "synergy_archer_rain",
                "synergy_magic_chain",
                "synergy_explosion_chain",
                "synergy_beast_hunt",
                "synergy_undead_summon",
                "synergy_healing_bond",
                "synergy_mixed_command",
            };
            foreach (string synergyId in requiredSynergyPresentationIds)
            {
                if (Synergies.TryGetValue(synergyId, out SynergyData synergy) == false
                    || string.IsNullOrWhiteSpace(synergy.DisplayName))
                    result.MissingRequiredIds.Add($"synergy:{synergyId}");
            }
            if (!LevelExp.ContainsKey(1)) result.MissingRequiredIds.Add("level_exp:1");
            if (_runTuning.FuseLinkFuseSeconds != 3.0f
                || _runTuning.FuseLinkSecondaryDamageRatio != 0.60f
                || _runTuning.FuseLinkSecondaryRadius != 1.5f
                || _runTuning.FuseLinkSecondaryMaxTargets != 6
                || _runTuning.FuseLinkPrimaryEffectIds != "dmg_bomb_explosion_v1,dot_fire_field_v1,dmg_skeleton_bomb_v1,DMG_SYNERGY_EXPLOSION_01")
            {
                result.MissingRequiredIds.Add("run_tuning:invalid_fuse_link");
            }
            ValidateCompanionCatalog(result);
            ValidatePassives(result);
            ValidateSynergyCombatCatalog(result);
        }
    }
}
