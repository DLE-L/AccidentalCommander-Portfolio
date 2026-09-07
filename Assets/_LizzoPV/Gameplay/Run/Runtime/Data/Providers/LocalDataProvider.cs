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
        readonly bool _allowEmbeddedFallback;
        readonly Dictionary<string, UnitData> Units = new Dictionary<string, UnitData>();
        readonly Dictionary<string, SkillData> Skills = new Dictionary<string, SkillData>();
        readonly Dictionary<string, EnemyData> Enemies = new Dictionary<string, EnemyData>();
        readonly Dictionary<int, EnemyData> EnemiesByTemplateId = new Dictionary<int, EnemyData>();
        readonly Dictionary<string, SynergyData> Synergies = new Dictionary<string, SynergyData>();
        readonly Dictionary<int, int> LevelExp = new Dictionary<int, int>();
        readonly List<string> _companionCatalogValidationErrors = new List<string>();
        RunTuningData _runTuning = new RunTuningData();
        DataLoadResult _lastResult;
        bool _initialized;

        public LocalDataProvider(IAssetService assets)
            : this(assets, Debug.isDebugBuild)
        {
        }

        private LocalDataProvider(IAssetService assets, bool allowEmbeddedFallback)
        {
            _assets = assets ?? throw new ArgumentNullException(nameof(assets));
            _allowEmbeddedFallback = allowEmbeddedFallback;
            _companionRosterView = _companionRoster.AsReadOnly();
            _companionCardLocalizationView = _companionCardLocalizations.AsReadOnly();
            _companionCombatProfileView = _companionCombatProfiles.AsReadOnly();
            _combatEffectView = _combatEffects.AsReadOnly();
            _companionSummonView = _companionSummons.AsReadOnly();
        }

        public bool IsInitialized => _initialized;
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
                UsedFallback = _allowEmbeddedFallback
            };

            if (_allowEmbeddedFallback)
                LoadFallbackData();

            try
            {
                TextAsset textAsset = await _assets.LoadAsync<TextAsset>(DATA_ADDRESS, cancellationToken);
                if (textAsset == null)
                {
                    result.ParseError = "Local data asset was not available.";
                    Debug.LogError($"[LocalDataProvider] {result.ParseError} address={DATA_ADDRESS} embeddedFallback={_allowEmbeddedFallback}");
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
                        LoadCompanionCombatProfiles(root.Element("CompanionCombatProfileDatas"));
                        LoadCombatEffects(root.Element("CombatEffectDatas"));
                        LoadCompanionSummons(root.Element("CompanionSummonDatas"));
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
            string[] skills = { "shield_push", "shield_captain_push", "sword_front_slash", "cleric_heal", "archer_far_shot" };
            string[] enemies = { "small_goblin", "hungry_wolf", "shield_orc", "elite_red_charger", "boss_hungry_giant" };
            foreach (string id in units) if (!Units.ContainsKey(id)) result.MissingRequiredIds.Add($"unit:{id}");
            foreach (string id in skills) if (!Skills.ContainsKey(id)) result.MissingRequiredIds.Add($"skill:{id}");
            foreach (string id in enemies) if (!Enemies.ContainsKey(id)) result.MissingRequiredIds.Add($"enemy:{id}");
            foreach (string id in SynergyBalanceProfileIds.Required)
            {
                if (!Synergies.TryGetValue(id, out SynergyData synergy)
                    || synergy.Cooldown <= 0.0f
                    || string.IsNullOrWhiteSpace(synergy.BalanceParameters))
                {
                    result.MissingRequiredIds.Add($"synergy_balance:{id}");
                }
            }
            ValidateEncounter(_runTuning.TimedElite, "timed_elite", result);
            ValidateFinalThreat(_runTuning.TutorialFinalThreat, "tutorial", result);
            ValidateFinalThreat(_runTuning.Stage1FinalThreat, "stage1", result);
            ValidateFinalThreat(_runTuning.Stage2FinalThreat, "stage2", result);
            ValidateFinalThreat(_runTuning.Stage3FinalThreat, "stage3", result);
            if (!LevelExp.ContainsKey(1)) result.MissingRequiredIds.Add("level_exp:1");
            ValidateCompanionCatalog(result);
        }

        void ValidateFinalThreat(EnemyEncounterDefinition definition, string contextId, DataLoadResult result)
        {
            ValidateEncounter(definition, $"final_threat:{contextId}", result);
        }

        void ValidateEncounter(EnemyEncounterDefinition definition, string contextId, DataLoadResult result)
        {
            if (definition == null
                || definition.EnemyTemplateId <= 0
                || EnemiesByTemplateId.ContainsKey(definition.EnemyTemplateId) == false
                || definition.EncounterRank == EnemyEncounterRank.TemplateDefault
                || definition.ScaleMultiplier <= 0.0f)
            {
                result.MissingRequiredIds.Add(contextId);
            }
        }
    }
}
