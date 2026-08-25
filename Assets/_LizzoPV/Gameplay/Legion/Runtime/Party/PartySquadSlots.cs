using System.Collections.Generic;
using Lizzo.PV.Legion.Party.Roster;
using UnityEngine;

namespace Lizzo.PV.Legion
{
    public sealed partial class PartyService
    {
        public int ActiveCompanionSlotCount => _rosterView.ActiveCompanionSlotCount;
        public int ActiveCompanionSlotCap => _rosterView.ActiveCompanionSlotCap;
        public int FreeCompanionSlots => Mathf.Max(0, ActiveCompanionSlotCap - ActiveCompanionSlotCount);
        public bool IsCompanionSlotFull => ActiveCompanionSlotCount >= ActiveCompanionSlotCap;
        public int ActiveCompanionCount => _rosterView.ActiveCompanionCount;
        public int PromotionReadyCount => _rosterView.PromotionReadyCount;
        public int SynergyReadyCount => IsGuardSquadActivated ? 0 : this.HasExactlyTwoGuardSquadFamilies() ? 1 : 0;
        public int ActiveSquadFamilySlotCount => ActiveCompanionSlotCount;

        public void Recruit(CompanionKind kind) =>
            PartyRecruitmentModule.Recruit(this, kind, playCardSummonFeedback: false);

        public void RecruitFromCard(CompanionKind kind) =>
            PartyRecruitmentModule.Recruit(this, kind, playCardSummonFeedback: true);

        public PartyRosterChangeResult PreviewCanonicalRecruit(string baseUnitId) =>
            _rosterView.PreviewCanonicalRecruit(baseUnitId);

        public bool RecruitCanonical(string baseUnitId) =>
            PartyRecruitmentModule.RecruitCanonical(
                this,
                baseUnitId,
                playCardSummonFeedback: false);

        public bool RecruitCanonicalFromCard(string baseUnitId) =>
            PartyRecruitmentModule.RecruitCanonical(
                this,
                baseUnitId,
                playCardSummonFeedback: true);

        public bool CanRecruitWithinSlotCap(CompanionKind kind)
        {
            PartyRosterChangeResult preview = PreviewRosterRecruit(kind);
            return preview == PartyRosterChangeResult.Recruit
                || preview == PartyRosterChangeResult.Reinforce
                || preview == PartyRosterChangeResult.Promote;
        }

        public bool WouldRecruitCompressSlot(CompanionKind kind)
        {
            return PreviewRosterRecruit(kind) == PartyRosterChangeResult.Promote;
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

        public IReadOnlyList<SquadSlotState> GetSquadSlotSnapshot() =>
            _rosterView.GetSquadSlotSnapshot();

        public bool TryGetSquadSlotForCompanion(CompanionKind kind, out SquadSlotState state)
        {
            if (TryResolveRosterBaseUnitId(kind, out string baseUnitId))
                return _rosterView.TryGetSlot(baseUnitId, out state);

            state = default;
            return false;
        }

        public bool TryGetCompanionProgress(CompanionKind kind, out int ownedCount, out int previewCount)
        {
            ownedCount = 0;
            previewCount = 0;
            if (kind == CompanionKind.ShieldCaptain || TryResolveRosterBaseUnitId(kind, out string baseUnitId) == false)
                return false;

            return _rosterView.TryGetCanonicalCompanionProgress(
                baseUnitId,
                out ownedCount,
                out previewCount);
        }

        public bool TryGetCanonicalCompanionProgress(string baseUnitId, out int ownedCount, out int previewCount) =>
            _rosterView.TryGetCanonicalCompanionProgress(baseUnitId, out ownedCount, out previewCount);

        public PartyRosterChangeResult PreviewRosterRecruit(CompanionKind kind)
        {
            if (kind == CompanionKind.ShieldCaptain || TryResolveRosterBaseUnitId(kind, out string baseUnitId) == false)
                return PartyRosterChangeResult.RejectedUnknown;

            return _rosterView.PreviewCanonicalRecruit(baseUnitId);
        }

        internal bool TryResolveRosterBaseUnitId(CompanionKind kind, out string baseUnitId)
        {
            baseUnitId = kind switch
            {
                CompanionKind.ShieldSoldier => "shield_guard",
                CompanionKind.ShieldCaptain => "shield_guard",
                CompanionKind.Swordsman => "sword_soldier",
                CompanionKind.Cleric => "cleric",
                CompanionKind.Archer => "falcon_archer",
                _ => string.Empty,
            };
            return string.IsNullOrEmpty(baseUnitId) == false;
        }
    }

}
