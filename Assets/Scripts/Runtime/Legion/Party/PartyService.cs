using System;
using System.Collections.Generic;
using Lizzo.PV.Data;
using Lizzo.PV.P0.Config;
using Lizzo.PV.P0.Skills.Guard;
using Lizzo.PV.P0.Telemetry;
using Lizzo.PV.P0.Units;
using Lizzo.PV.P0.Visuals;
using Lizzo.PV.Legion.Combat.Attacks;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public enum CompanionKind
    {
        ShieldSoldier,
        ShieldCaptain,
        Swordsman,
        Cleric,
        Archer,
    }

    public sealed class PartyService : IDisposable
    {
        internal const string SHIELD_FAMILY_TAG = "shield_family";
        internal const string SWORD_FAMILY_TAG = "sword_family";
        internal const string CLERIC_FAMILY_TAG = "cleric_family";
        internal const string RANGED_FAMILY_TAG = "ranged_family";
        internal const string SHIELD_SOLDIER_PREFAB_KEY = "P0/Units/Companions/ShieldSoldier.prefab";
        internal const string SHIELD_CAPTAIN_PREFAB_KEY = "P0/Units/Companions/ShieldCaptain.prefab";
        internal const string SWORDSMAN_PREFAB_KEY = "P0/Units/Companions/Swordsman.prefab";
        internal const string CLERIC_PREFAB_KEY = "P0/Units/Companions/Cleric.prefab";
        internal const string ARCHER_PREFAB_KEY = "P0/Units/Companions/Archer.prefab";

        private readonly IDataProvider _data;
        private readonly RuntimeObjectRegistry _registry;
        private readonly IPrefabFactory _factory;
        private readonly FormationService _formation;

        internal readonly List<AllyFollower> Allies = new List<AllyFollower>();
        internal readonly List<AllyFollower> ShieldSoldiers = new List<AllyFollower>();
        internal readonly List<CompanionRuntime> Companions = new List<CompanionRuntime>();
        internal readonly SquadSlotState[] SquadSlotSnapshot = new SquadSlotState[7];

        internal int ShieldSoldierCountState;
        internal int ShieldCaptainCountState;
        internal int SwordsmanCountState;
        internal int ClericCountState;
        internal int ArcherCountState;
        internal bool GuardSquadActivatedState;
        internal bool WasSlotFullState;
        internal float AllyAttackMultiplierState = 1.0f;
        internal float GuardWallBonusMultiplierState = 1.0f;

        public PartyService(IDataProvider data, RuntimeObjectRegistry registry, IPrefabFactory factory)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _formation = new FormationService(_registry, this);
        }

        internal RuntimeObjectRegistry Registry => _registry;
        internal IDataProvider Data => _data;
        internal IPrefabFactory Factory => _factory;
        internal FormationService Formation => _formation;

        public int ShieldSoldierCount => ShieldSoldierCountState;
        public int ShieldCaptainCount => ShieldCaptainCountState;
        public int SwordsmanCount => SwordsmanCountState;
        public int ClericCount => ClericCountState;
        public int ArcherCount => ArcherCountState;
        public bool IsGuardSquadActivated => GuardSquadActivatedState;
        public int ActiveCompanionSlotCount => Allies.Count;
        public int ActiveCompanionSlotCap => RemoteConfig.ActiveCompanionSlotCap;
        public int FreeCompanionSlots => Mathf.Max(0, ActiveCompanionSlotCap - ActiveCompanionSlotCount);
        public bool IsCompanionSlotFull => ActiveCompanionSlotCount >= ActiveCompanionSlotCap;
        public int ActiveCompanionCount => Companions.Count;
        public float AllyAttackMultiplier => AllyAttackMultiplierState;
        public float GuardWallBonusMultiplier => GuardWallBonusMultiplierState;
        public int PromotionReadyCount => ShieldSoldierCountState == 2 ? 1 : 0;
        public int SynergyReadyCount => IsGuardSquadActivated ? 0 : this.HasExactlyTwoGuardSquadFamilies() ? 1 : 0;
        public int SquadFamilySlotCap => PartySquadSlots.SQUAD_FAMILY_SLOT_CAP;
        public int ActiveSquadFamilySlotCount => this.GetActiveSquadFamilySlotCount();

        internal IReadOnlyList<AllyFollower> ActiveAllies => Allies;
        internal IReadOnlyList<CompanionRuntime> ActiveCompanions => Companions;
        internal int ActiveAllyCount => Allies.Count;

        public void Dispose()
        {
            this.ResetRunState();
        }

        public void NotifyCompanionDown(CompanionRuntime companion)
        {
            if (companion == null)
                return;

            if (companion.IsFamily(SHIELD_FAMILY_TAG))
            {
                P0Telemetry.Log(
                    P0Telemetry.FrontLinePressure,
                    "reason=shield_family_down",
                    $"unit_id={companion.UnitId}",
                    $"slot_id={companion.SlotId}");
            }

            if (GuardSquadActivatedState)
            {
                P0Telemetry.Log(
                    P0Telemetry.SynergyKeep,
                    "reason=companion_down",
                    "combo_id=guard_squad",
                    this.GetFamilyTagsSnapshotParameter(),
                    this.GetPromotedStateParameter());
            }
        }

        public void NotifyCompanionRecovered(CompanionRuntime companion)
        {
            if (companion == null)
                return;

            if (companion.IsFamily(SHIELD_FAMILY_TAG))
            {
                P0Telemetry.Log(
                    P0Telemetry.FrontLinePressure,
                    "reason=shield_family_recovered",
                    $"unit_id={companion.UnitId}",
                    $"slot_id={companion.SlotId}");
            }

            if (GuardSquadActivatedState)
            {
                P0Telemetry.Log(
                    P0Telemetry.SynergyKeep,
                    "reason=companion_recover",
                    "combo_id=guard_squad",
                    this.GetFamilyTagsSnapshotParameter(),
                    this.GetPromotedStateParameter());
            }
        }

        public void RefreshShieldSoldierAreaPushTest() => PartyCompanionFactory.RefreshShieldSoldierAreaPushTest(this);

        public void IgnoreFriendlyBodyCollisionsWithEnemy(MonsterController monster) => PartyFormationRuntime.IgnoreFriendlyBodyCollisionsWithEnemy(this, monster);

        public int ApplySmallHealToCompanions(int amount)
        {
            int healedCount = 0;

            for (int i = 0; i < Companions.Count; i++)
            {
                CompanionRuntime companion = Companions[i];
                if (companion != null && companion.ApplyHeal(amount, "small_heal_card"))
                    healedCount++;
            }

            return healedCount;
        }

        public bool TryResolveClericHeal(int healAmount, Vector3 casterPosition)
        {
            return ClericHealAttack.TryResolve(this, healAmount);
        }

        public void Recruit(CompanionKind kind) => Recruit(kind, playCardSummonFeedback: false);

        public void RecruitFromCard(CompanionKind kind) => Recruit(kind, playCardSummonFeedback: true);

        public bool CanRecruitWithinSlotCap(CompanionKind kind)
        {
            if (ActiveCompanionSlotCount < ActiveCompanionSlotCap)
                return true;

            return WouldRecruitCompressSlot(kind);
        }

        public bool WouldRecruitCompressSlot(CompanionKind kind)
        {
            return kind == CompanionKind.ShieldSoldier && ShieldSoldierCountState == 2;
        }

        public bool WouldRecruitCompleteGuardSquad(CompanionKind kind)
        {
            if (GuardSquadActivatedState)
                return false;

            bool hasShield = this.HasShieldFamily();
            bool hasSword = this.HasSwordFamily();
            bool hasCleric = this.HasClericFamily();

            switch (kind)
            {
                case CompanionKind.ShieldSoldier:
                case CompanionKind.ShieldCaptain:
                    hasShield = true;
                    break;
                case CompanionKind.Swordsman:
                    hasSword = true;
                    break;
                case CompanionKind.Cleric:
                    hasCleric = true;
                    break;
            }

            return hasShield && hasSword && hasCleric;
        }

        public void LogActiveSlotState(string reason)
        {
            P0Telemetry.Log(P0Telemetry.ActiveSlotStateUpdate, BuildSlotStateParameters(reason));

            bool isFull = IsCompanionSlotFull;
            if (isFull && WasSlotFullState == false)
                P0Telemetry.Log(P0Telemetry.CompanionSlotFull, BuildSlotStateParameters(reason));

            WasSlotFullState = isFull;
        }

        public string[] BuildSlotStateParameters(string reason)
        {
            return new[]
            {
                $"reason={reason}",
                $"slot_used={ActiveCompanionSlotCount}",
                $"slot_cap={ActiveCompanionSlotCap}",
                $"free_slots={FreeCompanionSlots}",
                $"promotion_ready_count={PromotionReadyCount}",
                $"synergy_ready_count={SynergyReadyCount}",
            };
        }

        public void ResetRunState()
        {
            for (int i = Allies.Count - 1; i >= 0; i--)
            {
                if (Allies[i] != null)
                    _factory.Release(Allies[i].gameObject);
            }

            Allies.Clear();
            ShieldSoldiers.Clear();
            Companions.Clear();
            ShieldSoldierCountState = 0;
            ShieldCaptainCountState = 0;
            SwordsmanCountState = 0;
            ClericCountState = 0;
            ArcherCountState = 0;
            GuardSquadActivatedState = false;
            WasSlotFullState = false;
            ResetCardModifiers();
            Formation.ResetRunState();
            GuardSquadSkillBehaviour.StopActive();
        }

        public IReadOnlyList<SquadSlotState> GetSquadSlotSnapshot() => PartySquadSlots.GetSquadSlotSnapshot(this);

        public bool TryGetSquadSlotForCompanion(CompanionKind kind, out SquadSlotState state) => PartySquadSlots.TryGetSquadSlotForCompanion(this, kind, out state);

        public int PreviewSquadSlotCountAfterRecruit(CompanionKind kind) => PartySquadSlots.PreviewSquadSlotCountAfterRecruit(this, kind);

        public void LogActiveSquadSlotState(string reason) => PartySquadSlots.LogActiveSquadSlotState(this, reason);

        public string[] BuildSquadSlotStateParameters(string reason) => PartySquadSlots.BuildSquadSlotStateParameters(this, reason);

        public string BuildLegionSummary()
        {
            string summary = "군단";
            summary = AppendUnitSummary(summary, "방패대장", ShieldCaptainCountState);
            summary = AppendUnitSummary(summary, "방패병", ShieldSoldierCountState);
            summary = AppendUnitSummary(summary, "검병", SwordsmanCountState);
            summary = AppendUnitSummary(summary, "성직자", ClericCountState);
            summary = AppendUnitSummary(summary, "궁수", ArcherCountState);
            return summary;
        }

        public string GetCompletedSynergySummary() => GuardSquadActivatedState ? "근위대" : "없음";

        public string GetMvpCompanionSummary()
        {
            if (ShieldCaptainCountState > 0)
                return "방패대장";
            if (ClericCountState > 0)
                return "성직자";
            if (SwordsmanCountState > 0)
                return "검병";
            if (ShieldSoldierCountState > 0)
                return "방패병";
            if (ArcherCountState > 0)
                return "궁수";

            return "군단장";
        }

        private void Recruit(CompanionKind kind, bool playCardSummonFeedback)
        {
            PlayerController player = Registry?.Player;
            if (player == null)
            {
                Debug.LogWarning($"P0 recruit skipped. Player not ready: {kind}");
                return;
            }

            if (CanRecruitWithinSlotCap(kind) == false)
            {
                Debug.LogWarning($"P0 recruit blocked by companion slot cap: {kind} {ActiveCompanionSlotCount}/{ActiveCompanionSlotCap}");
                LogActiveSlotState($"recruit_blocked_{kind}");
                return;
            }

            AllyFollower recruitedFollower = null;
            CompanionKind feedbackKind = kind;
            switch (kind)
            {
                case CompanionKind.ShieldSoldier:
                    ShieldSoldierCountState++;
                    recruitedFollower = this.CreateShieldSoldier(player.transform, ShieldSoldierCountState);
                    if (ShieldSoldierCountState >= 3)
                    {
                        recruitedFollower = this.PromoteShieldCaptain(player.transform);
                        feedbackKind = CompanionKind.ShieldCaptain;
                    }
                    break;
                case CompanionKind.Swordsman:
                    SwordsmanCountState++;
                    recruitedFollower = this.CreateCombatAlly(
                        player.transform,
                        $"Swordsman_{SwordsmanCountState}",
                        Data.GetUnit("sword_soldier"),
                        SwordsmanCountState,
                        SortingOrder.Unit,
                        AllyAttackStyle.ForwardSlash);
                    break;
                case CompanionKind.Cleric:
                    ClericCountState++;
                    recruitedFollower = this.CreateCombatAlly(
                        player.transform,
                        $"Cleric_{ClericCountState}",
                        Data.GetUnit("cleric"),
                        ClericCountState,
                        SortingOrder.Unit,
                        AllyAttackStyle.HealCommander);
                    break;
                case CompanionKind.Archer:
                    ArcherCountState++;
                    recruitedFollower = this.CreateCombatAlly(
                        player.transform,
                        $"Archer_{ArcherCountState}",
                        Data.GetUnit("archer"),
                        ArcherCountState,
                        SortingOrder.Unit,
                        AllyAttackStyle.FarthestTarget);
                    break;
            }

            this.RefreshFormationForCurrentRoster(player.transform, $"companion_recruit_{kind}");
            if (playCardSummonFeedback)
                PlayCardSummonFeedback(feedbackKind, recruitedFollower);

            P0Telemetry.Log(P0Telemetry.CompanionRecruit, $"companion={kind}");
            P0Telemetry.LogOnce(P0Telemetry.FirstRecruit, $"companion={kind}");
            this.TryActivateGuardSquad(player.transform);
            this.LogGuardMaterialQaCheck($"companion_recruit_{kind}");
            LogActiveSlotState("companion_recruit");
            LogActiveSquadSlotState("companion_recruit");
        }

        private void PlayCardSummonFeedback(CompanionKind kind, AllyFollower follower)
        {
            if (follower == null)
                return;

            Transform target = follower.transform;
            Vector3 position = target.position;
            RetroVfx.Spawn(RetroVfxKind.LevelUp, position, Vector3.up, 0.9f);
            RetroVfx.SpawnAttached(RetroVfxKind.BuffPulse, target, new Vector3(0.0f, 0.32f, 0.0f), Vector3.zero, 1.15f);
            RetroSfx.Play("retro_confetti_shoot", position, 0.72f);
            FloatingDamageText.ShowLabel(
                position + new Vector3(0.0f, 0.25f, 0.0f),
                ResolveCardSummonLabel(kind),
                new Color(0.82f, 1.0f, 0.42f, 1.0f),
                large: true,
                lifeTime: 0.85f);
        }

        private static string ResolveCardSummonLabel(CompanionKind kind)
        {
            return kind switch
            {
                CompanionKind.ShieldSoldier => "방패병 합류!",
                CompanionKind.ShieldCaptain => "방패대장 합류!",
                CompanionKind.Swordsman => "검병 합류!",
                CompanionKind.Cleric => "성직자 합류!",
                CompanionKind.Archer => "궁수 합류!",
                _ => "동료 합류!",
            };
        }

        private static string AppendUnitSummary(string summary, string label, int count)
        {
            if (count <= 0)
                return summary;

            return $"{summary} / {label} x{count}";
        }

        public float AddAllyAttackBonus(float bonusRatio)
        {
            AllyAttackMultiplierState = Mathf.Max(1.0f, AllyAttackMultiplierState + Mathf.Max(0.0f, bonusRatio));
            RefreshAllCompanionCombat();
            return AllyAttackMultiplierState;
        }

        public float AddGuardWallBonus(float bonusRatio)
        {
            GuardWallBonusMultiplierState = Mathf.Max(1.0f, GuardWallBonusMultiplierState + Mathf.Max(0.0f, bonusRatio));
            return GuardWallBonusMultiplierState;
        }

        internal T RequireComponent<T>(GameObject owner) where T : Component
        {
            T component = owner == null ? null : owner.GetComponent<T>();
            if (component == null)
                throw new InvalidOperationException($"Companion prefab is missing required component: {typeof(T).Name}");

            return component;
        }

        internal void ResetCardModifiers()
        {
            AllyAttackMultiplierState = 1.0f;
            GuardWallBonusMultiplierState = 1.0f;
        }

        internal void RefreshAllCompanionCombat()
        {
            for (int i = 0; i < Companions.Count; i++)
            {
                CompanionRuntime companion = Companions[i];
                if (companion == null)
                    continue;

                UnitData unitData = _data.GetUnit(companion.UnitId);
                if (unitData == null)
                    continue;

                AllyCombat combat = companion.GetComponent<AllyCombat>();
                this.ApplyCombatFromData(combat, unitData, AllyAttackStyle.SingleTarget);
            }
        }

        internal readonly struct FormationSlot
        {
            public readonly string Id;
            public readonly Vector3 Offset;

            public FormationSlot(string id, Vector3 offset)
            {
                Id = id;
                Offset = offset;
            }
        }
    }
}
