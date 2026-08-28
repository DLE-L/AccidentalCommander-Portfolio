using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Lizzo.PV.Flow;
using Lizzo.PV.Legion;
using Lizzo.PV.Legion.Party.Roster;

namespace Lizzo.PV.EditorTools
{
    public static class FtueHomeTestActions
    {
        internal const string TutorialCompletionKey = "lizzo.ftue.tutorial_completed.v1";

        public const int SynergyFixtureRowCount = 2;
        public const int SynergyFixtureColumnCount = 4;

        public sealed class SynergyFixtureDefinition
        {
            public SynergyFixtureDefinition(string id, params string[] requiredBaseUnitIds)
            {
                Id = id;
                RequiredBaseUnitIds = Array.AsReadOnly(requiredBaseUnitIds ?? Array.Empty<string>());
            }

            public string Id { get; }
            public IReadOnlyList<string> RequiredBaseUnitIds { get; }
        }

        public interface ISynergyFixtureRecruiter
        {
            int FreeCompanionSlots { get; }
            bool IsBaseUnitOwned(string baseUnitId);
            bool CanRecruitBaseUnit(string baseUnitId);
            bool TryRecruitBaseUnit(string baseUnitId);
        }

        static readonly IReadOnlyList<SynergyFixtureDefinition> _synergyFixtureDefinitions =
            new[]
            {
                new SynergyFixtureDefinition("synergy_guard_shockwave", "shield_guard", "sword_soldier", "cleric"),
                new SynergyFixtureDefinition("synergy_archer_rain", "field_herbalist", "falcon_archer", "bombardier"),
                new SynergyFixtureDefinition("synergy_magic_chain", "fire_mage", "lightning_mage", "necromancer"),
                new SynergyFixtureDefinition("synergy_explosion_chain", "bombardier", "fire_mage", "skeleton_bomber"),
                new SynergyFixtureDefinition("synergy_beast_hunt", "falcon_archer", "wolf_tamer"),
                new SynergyFixtureDefinition("synergy_undead_summon", "wraith_knight", "necromancer", "skeleton_bomber"),
                new SynergyFixtureDefinition("synergy_healing_bond", "cleric", "field_herbalist", "wraith_knight"),
                new SynergyFixtureDefinition("synergy_mixed_command", "shield_guard", "sword_soldier", "cleric", "falcon_archer", "fire_mage"),
            };
        static readonly HashSet<string> _missingSynergyPresentationReports = new HashSet<string>();

        public static IReadOnlyList<SynergyFixtureDefinition> SynergyFixtureDefinitions => _synergyFixtureDefinitions;
        public static string LastSynergyFixtureStatus { get; private set; } = string.Empty;

        internal static bool IsTutorialCompleted => PlayerPrefs.GetInt(TutorialCompletionKey, 0) != 0;

        public static bool IsValidBattleRun(bool runLoaded, string scenePath)
        {
            return runLoaded && scenePath == GameFlowRoutes.GameplayScenePath;
        }

        public static string GetBattleControlDisabledReason(bool isPlaying, bool runLoaded, string scenePath)
        {
            if (!isPlaying)
                return "Disabled: enter Play Mode through Loading.";
            if (!runLoaded)
                return "Disabled: wait for Gameplay to finish loading.";
            if (scenePath == GameFlowRoutes.GameplayScenePath)
                return string.Empty;

            return "Disabled: runtime controls are available only in the loaded Gameplay scene.";
        }

        public static string ResolveSynergyFixtureDisplayName(IDataProvider data, string synergyId)
        {
            if (data != null)
            {
                SynergyData synergy = data.GetSynergy(synergyId);
                if (synergy != null && string.IsNullOrWhiteSpace(synergy.DisplayName) == false)
                    return synergy.DisplayName;

                if (_missingSynergyPresentationReports.Add(synergyId))
                    Debug.LogError($"FTUE synergy fixture presentation missing: {synergyId}");
            }

            return synergyId;
        }

        internal static void ResetMissingSynergyPresentationReportsForTests()
        {
            _missingSynergyPresentationReports.Clear();
        }

        public static bool TryApplySynergyFixture(string synergyId, ISynergyFixtureRecruiter recruiter)
        {
            SynergyFixtureDefinition definition = null;
            for (int i = 0; i < _synergyFixtureDefinitions.Count; i++)
            {
                if (_synergyFixtureDefinitions[i].Id == synergyId)
                {
                    definition = _synergyFixtureDefinitions[i];
                    break;
                }
            }

            if (definition == null || recruiter == null)
                return SetSynergyFixtureFailure("fixture failure: unknown synergy or unavailable roster");

            List<string> missing = new List<string>(definition.RequiredBaseUnitIds.Count);
            for (int i = 0; i < definition.RequiredBaseUnitIds.Count; i++)
            {
                string baseUnitId = definition.RequiredBaseUnitIds[i];
                if (recruiter.IsBaseUnitOwned(baseUnitId) == false)
                    missing.Add(baseUnitId);
            }

            if (missing.Count > recruiter.FreeCompanionSlots)
                return SetSynergyFixtureFailure($"fixture failure: insufficient free companion slots ({missing.Count} required, {recruiter.FreeCompanionSlots} free)");

            for (int i = 0; i < missing.Count; i++)
            {
                if (recruiter.CanRecruitBaseUnit(missing[i]) == false)
                    return SetSynergyFixtureFailure($"fixture failure: recruit unavailable for {missing[i]}");
            }

            for (int i = 0; i < missing.Count; i++)
            {
                string baseUnitId = missing[i];
                bool recruited;
                try
                {
                    recruited = recruiter.TryRecruitBaseUnit(baseUnitId);
                }
                catch (Exception exception)
                {
                    return SetSynergyFixtureFailure($"fixture failure: recruit exception for {baseUnitId}: {exception.Message}");
                }

                if (recruited == false || recruiter.IsBaseUnitOwned(baseUnitId) == false)
                    return SetSynergyFixtureFailure($"fixture failure: recruit failed for {baseUnitId}");
            }

            return SetSynergyFixtureSuccess($"fixture success: {synergyId} recruited {missing.Count} missing member(s)");
        }

        public static bool TryApplySynergyFixture(GameScene gameScene, string synergyId)
        {
            if (gameScene == null || gameScene.IsRunLoaded == false || gameScene.Services?.Party == null)
                return SetSynergyFixtureFailure("fixture failure: Gameplay run is not ready");

            return TryApplySynergyFixture(synergyId, new PartyServiceSynergyFixtureRecruiter(gameScene.Services.Party));
        }

        static bool SetSynergyFixtureSuccess(string status)
        {
            LastSynergyFixtureStatus = status;
            return true;
        }

        static bool SetSynergyFixtureFailure(string status)
        {
            LastSynergyFixtureStatus = status;
            return false;
        }

        sealed class PartyServiceSynergyFixtureRecruiter : ISynergyFixtureRecruiter
        {
            readonly PartyService _party;

            public PartyServiceSynergyFixtureRecruiter(PartyService party) { _party = party; }
            public int FreeCompanionSlots => _party.FreeCompanionSlots;

            public bool IsBaseUnitOwned(string baseUnitId)
            {
                return _party.TryGetCanonicalCompanionProgress(baseUnitId, out int ownedCount, out _) && ownedCount > 0;
            }

            public bool CanRecruitBaseUnit(string baseUnitId)
            {
                return _party.PreviewCanonicalRecruit(baseUnitId) == PartyRosterChangeResult.Recruit;
            }

            public bool TryRecruitBaseUnit(string baseUnitId)
            {
                switch (baseUnitId)
                {
                    case "shield_guard":
                        _party.Recruit(CompanionKind.ShieldSoldier);
                        break;
                    case "sword_soldier":
                        _party.Recruit(CompanionKind.Swordsman);
                        break;
                    case "cleric":
                        _party.Recruit(CompanionKind.Cleric);
                        break;
                    case "falcon_archer":
                        _party.Recruit(CompanionKind.Archer);
                        break;
                    default:
                        return _party.RecruitCanonical(baseUnitId);
                }

                return true;
            }
        }

        internal static void ResetFirstRunState()
        {
            ResetFirstRunState(new CompanionUnlockProgress(new EditorPlayerPrefsCompanionUnlockProgressStore()));
        }

        internal static void ResetFirstRunState(CompanionUnlockProgress progress)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));

            PlayerPrefs.DeleteKey(TutorialCompletionKey);
            TutorialCheckpointProgress.Reset();
            progress.ResetAccountProgress();
            PlayerPrefs.Save();
        }

        sealed class EditorPlayerPrefsCompanionUnlockProgressStore : ICompanionUnlockProgressStore
        {
            public int GetInt(string key, int defaultValue)
            {
                return PlayerPrefs.GetInt(key, defaultValue);
            }

            public void SetInt(string key, int value)
            {
                PlayerPrefs.SetInt(key, value);
            }

            public void Save()
            {
                PlayerPrefs.Save();
            }
        }

        internal static void SetReturningState()
        {
            PlayerPrefs.SetInt(TutorialCompletionKey, 1);
            PlayerPrefs.Save();
        }

        internal static void LaunchFromLoading()
        {
            if (EditorApplication.isPlaying)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(GameFlowRoutes.LoadingScenePath);
                return;
            }

            EditorSceneManager.OpenScene(GameFlowRoutes.LoadingScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
    }
}
