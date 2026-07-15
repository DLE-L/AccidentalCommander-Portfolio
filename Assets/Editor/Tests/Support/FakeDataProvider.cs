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
        readonly List<string> _missingIds = new List<string>();
        readonly RunTuningData _runTuning = new RunTuningData();
        bool _initialized;
        bool _failInitialization;

        public FakeDataProvider() => AddBaseline();
        public bool IsInitialized => _initialized;
        public RunTuningData RunTuning { get { EnsureInitialized(); return _runTuning; } }

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
            SetLevelExp(1, _runTuning.FirstLevelExp);
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
