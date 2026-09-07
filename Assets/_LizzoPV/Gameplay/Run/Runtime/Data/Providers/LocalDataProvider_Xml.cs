using System.Xml.Linq;
using UnityEngine;

namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        private void LoadRunTuning(XElement element)
        {
            if (element == null)
                return;

            _runTuning.StageDurationSeconds = FloatAttr(element, "stageDurationSeconds", _runTuning.StageDurationSeconds);
            _runTuning.DemoDurationSeconds = FloatAttr(element, "demoDurationSeconds", _runTuning.DemoDurationSeconds);
            _runTuning.BossSpawnSeconds = FloatAttr(element, "bossSpawnSeconds", _runTuning.BossSpawnSeconds);
            _runTuning.TimedEliteSpawnSeconds = FloatAttr(element, "timedEliteSpawnSeconds", _runTuning.TimedEliteSpawnSeconds);
            LoadEncounter(element, "timedElite", _runTuning.TimedElite);
            LoadFinalThreat(element, "tutorialFinalThreat", _runTuning.TutorialFinalThreat);
            LoadFinalThreat(element, "stage1FinalThreat", _runTuning.Stage1FinalThreat);
            LoadFinalThreat(element, "stage2FinalThreat", _runTuning.Stage2FinalThreat);
            LoadFinalThreat(element, "stage3FinalThreat", _runTuning.Stage3FinalThreat);
            _runTuning.FirstLevelExp = IntAttr(element, "firstLevelExp", _runTuning.FirstLevelExp);
            _runTuning.MaxEnemyStage1 = IntAttr(element, "maxEnemyStage1", _runTuning.MaxEnemyStage1);
            _runTuning.NormalEnemyExperience = IntAttr(element, "normalEnemyExperience", _runTuning.NormalEnemyExperience);
            _runTuning.EliteEnemyExperience = IntAttr(element, "eliteEnemyExperience", _runTuning.EliteEnemyExperience);
            _runTuning.BossEnemyExperience = IntAttr(element, "bossEnemyExperience", _runTuning.BossEnemyExperience);
            _runTuning.TutorialExperienceMultiplierPermille = IntAttr(element, "tutorialExperienceMultiplierPermille", _runTuning.TutorialExperienceMultiplierPermille);
            _runTuning.Stage1ExperienceMultiplierPermille = IntAttr(element, "stage1ExperienceMultiplierPermille", _runTuning.Stage1ExperienceMultiplierPermille);
            _runTuning.Stage2ExperienceMultiplierPermille = IntAttr(element, "stage2ExperienceMultiplierPermille", _runTuning.Stage2ExperienceMultiplierPermille);
            _runTuning.Stage3ExperienceMultiplierPermille = IntAttr(element, "stage3ExperienceMultiplierPermille", _runTuning.Stage3ExperienceMultiplierPermille);
            _runTuning.LowFxScale = FloatAttr(element, "lowFxScale", _runTuning.LowFxScale);
        }

        private void LoadFinalThreat(XElement element, string prefix, EnemyEncounterDefinition target)
        {
            LoadEncounter(element, prefix, target);
        }

        private void LoadEncounter(XElement element, string prefix, EnemyEncounterDefinition target)
        {
            target.EnemyTemplateId = IntAttr(element, prefix + "TemplateId", target.EnemyTemplateId);
            target.EncounterRank = EnumAttr(element, prefix + "Rank", target.EncounterRank);
            target.ScaleMultiplier = FloatAttr(element, prefix + "Scale", target.ScaleMultiplier);
        }

        private void LoadLevelExp(XElement parent)
        {
            if (parent == null)
                return;

            foreach (XElement element in parent.Elements("LevelExpData"))
            {
                int level = IntAttr(element, "level", 0);
                if (level <= 0)
                    continue;

                LevelExp[level] = IntAttr(element, "requiredExp", _runTuning.FirstLevelExp);
            }
        }

        private void LoadUnits(XElement parent)
        {
            if (parent == null)
                return;

            foreach (XElement element in parent.Elements("UnitData"))
            {
                UnitData data = new UnitData
                {
                    Id = StringAttr(element, "id", string.Empty),
                    DisplayName = StringAttr(element, "displayName", string.Empty),
                    FamilyTags = StringAttr(element, "familyTags", string.Empty),
                    RoleTags = StringAttr(element, "roleTags", string.Empty),
                    SkillId = StringAttr(element, "skillId", string.Empty),
                    PromotionSource = StringAttr(element, "promotionSource", string.Empty),
                    PromotionResult = StringAttr(element, "promotionResult", string.Empty),
                    Hp = IntAttr(element, "hp", 1),
                    Attack = IntAttr(element, "attack", 0),
                    Heal = IntAttr(element, "heal", 0),
                    Cooldown = FloatAttr(element, "cooldown", 1.0f),
                    Range = FloatAttr(element, "range", 1.0f),
                    MoveSpeed = FloatAttr(element, "moveSpeed", 1.0f),
                    Knockback = FloatAttr(element, "knockback", 0.0f),
                    AbsorbRange = FloatAttr(element, "absorbRange", 1.0f),
                    Color = ColorAttr(element, "color", Color.white),
                };

                if (string.IsNullOrEmpty(data.Id))
                    continue;

                Units[data.Id] = data;
            }
        }

        private void LoadSkills(XElement parent)
        {
            if (parent == null)
                return;

            foreach (XElement element in parent.Elements("SkillData"))
            {
                SkillData data = new SkillData
                {
                    Id = StringAttr(element, "id", string.Empty),
                    DisplayName = StringAttr(element, "displayName", string.Empty),
                    SkillKind = StringAttr(element, "skillKind", string.Empty),
                    Power = IntAttr(element, "power", 0),
                    Cooldown = FloatAttr(element, "cooldown", 1.0f),
                    Range = FloatAttr(element, "range", 1.0f),
                    Knockback = FloatAttr(element, "knockback", 0.0f),
                    Angle = FloatAttr(element, "angle", 0.0f),
                    Duration = FloatAttr(element, "duration", 0.0f),
                    Width = FloatAttr(element, "width", 0.0f),
                };

                if (string.IsNullOrEmpty(data.Id))
                    continue;

                Skills[data.Id] = data;
            }
        }

        private void LoadEnemies(XElement parent)
        {
            if (parent == null)
                return;

            foreach (XElement element in parent.Elements("EnemyData"))
            {
                EnemyData data = new EnemyData
                {
                    TemplateId = IntAttr(element, "templateId", 0),
                    Id = StringAttr(element, "id", string.Empty),
                    DisplayName = StringAttr(element, "displayName", string.Empty),
                    Prefab = StringAttr(element, "prefab", string.Empty),
                    Type = StringAttr(element, "type", string.Empty),
                    Hp = IntAttr(element, "hp", 1),
                    Attack = IntAttr(element, "attack", 0),
                    ChargeAttack = IntAttr(element, "chargeAttack", IntAttr(element, "attack", 0)),
                    AttackCooldown = FloatAttr(element, "attackCooldown", 1.0f),
                    ContactRange = FloatAttr(element, "contactRange", 0.8f),
                    MoveSpeed = FloatAttr(element, "moveSpeed", 1.0f),
                    SpawnSeconds = FloatAttr(element, "spawnSeconds", 0.0f),
                    ChargeCooldown = FloatAttr(element, "chargeCooldown", 0.0f),
                    ChargeDuration = FloatAttr(element, "chargeDuration", 0.0f),
                    PatternCooldown = FloatAttr(element, "patternCooldown", 0.0f),
                    Range = FloatAttr(element, "range", 0.0f),
                    Width = FloatAttr(element, "width", 0.0f),
                    Color = ColorAttr(element, "color", Color.white),
                    SortingOrder = IntAttr(element, "sortingOrder", 200),
                };

                if (string.IsNullOrEmpty(data.Id))
                    continue;

                Enemies[data.Id] = data;
                if (data.TemplateId > 0)
                    EnemiesByTemplateId[data.TemplateId] = data;
            }
        }

        private void LoadSynergies(XElement parent)
        {
            if (parent == null)
                return;

            foreach (XElement element in parent.Elements("SynergyData"))
            {
                SynergyData data = new SynergyData
                {
                    Id = StringAttr(element, "id", string.Empty),
                    DisplayName = StringAttr(element, "displayName", string.Empty),
                    RequiredFamilyTags = StringAttr(element, "requiredFamilyTags", string.Empty),
                    SkillId = StringAttr(element, "skillId", string.Empty),
                    ShieldDurability = IntAttr(element, "shieldDurability", 80),
                    Cooldown = FloatAttr(element, "cooldown", 12.0f),
                    Width = FloatAttr(element, "width", 3.0f),
                    Duration = FloatAttr(element, "duration", 2.0f),
                };

                if (string.IsNullOrEmpty(data.Id))
                    continue;

                Synergies[data.Id] = data;
            }
        }

    }
}
