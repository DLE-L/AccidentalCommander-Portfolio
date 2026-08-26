using System;

namespace Lizzo.PV.Flow
{
    public enum CommanderWeaponId
    {
        None,
        RapidCrossbow,
        PiercingSpear,
        BlastStaff,
    }

    public static class CommanderWeaponCatalog
    {
        public static bool IsSelectable(CommanderWeaponId weapon)
        {
            return weapon == CommanderWeaponId.RapidCrossbow ||
                   weapon == CommanderWeaponId.PiercingSpear ||
                   weapon == CommanderWeaponId.BlastStaff;
        }

        public static string ToId(CommanderWeaponId weapon)
        {
            return weapon switch
            {
                CommanderWeaponId.RapidCrossbow => "rapid_crossbow",
                CommanderWeaponId.PiercingSpear => "piercing_spear",
                CommanderWeaponId.BlastStaff => "blast_staff",
                _ => string.Empty,
            };
        }
    }

    public enum RunMode
    {
        Normal,
        Tutorial,
    }

    public enum CampaignStageId
    {
        Stage1 = 1,
        Stage2 = 2,
        Stage3 = 3,
    }

    public readonly struct RunContext : IEquatable<RunContext>
    {
        public static RunContext Normal => new RunContext(RunMode.Normal, CampaignStageId.Stage1);
        public static RunContext Tutorial => new RunContext(RunMode.Tutorial, CampaignStageId.Stage1);

        public RunMode Mode { get; }
        public CampaignStageId StageId { get; }
        public CommanderWeaponId CommanderWeapon { get; }
        public bool IsTutorial => Mode == RunMode.Tutorial;
        public bool IsNormal => Mode == RunMode.Normal;
        public int ExpeditionTicketCost => IsNormal ? 1 : 0;
        public bool HasCommanderWeapon => CommanderWeaponCatalog.IsSelectable(CommanderWeapon);

        public RunContext(RunMode mode)
            : this(mode, CampaignStageId.Stage1, CommanderWeaponId.None)
        {
        }

        public RunContext(RunMode mode, CommanderWeaponId commanderWeapon)
            : this(mode, CampaignStageId.Stage1, commanderWeapon)
        {
        }

        public RunContext(RunMode mode, CampaignStageId stageId)
            : this(mode, stageId, CommanderWeaponId.None)
        {
        }

        public RunContext(RunMode mode, CampaignStageId stageId, CommanderWeaponId commanderWeapon)
        {
            if (mode != RunMode.Normal && mode != RunMode.Tutorial)
                throw new ArgumentOutOfRangeException(nameof(mode));
            if (stageId < CampaignStageId.Stage1 || stageId > CampaignStageId.Stage3)
                throw new ArgumentOutOfRangeException(nameof(stageId));
            if (mode == RunMode.Tutorial && stageId != CampaignStageId.Stage1)
                throw new ArgumentOutOfRangeException(nameof(stageId));
            if (commanderWeapon != CommanderWeaponId.None &&
                CommanderWeaponCatalog.IsSelectable(commanderWeapon) == false)
                throw new ArgumentOutOfRangeException(nameof(commanderWeapon));

            Mode = mode;
            StageId = stageId;
            CommanderWeapon = commanderWeapon;
        }

        public bool Equals(RunContext other) =>
            Mode == other.Mode && StageId == other.StageId && CommanderWeapon == other.CommanderWeapon;

        public override bool Equals(object obj) => obj is RunContext other && Equals(other);

        public override int GetHashCode() => HashCode.Combine((int)Mode, (int)StageId, (int)CommanderWeapon);

        public override string ToString()
        {
            string mode = Mode == RunMode.Tutorial ? "tutorial" : "normal";
            if (Mode == RunMode.Normal && StageId != CampaignStageId.Stage1)
                mode += ":stage" + (int)StageId;
            string weapon = CommanderWeaponCatalog.ToId(CommanderWeapon);
            return string.IsNullOrEmpty(weapon) ? mode : mode + ":" + weapon;
        }
    }

    public sealed class RunLaunchState
    {
        RunContext _currentContext = RunContext.Normal;
        bool _hasPreparedRequest;

        public bool TryPrepare(RunContext context, CompanionUnlockProgress progress)
        {
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));
            if (context.IsNormal && progress.IsStageUnlocked(context.StageId) == false)
                return false;

            Prepare(context);
            return true;
        }

        public void Prepare(RunContext context)
        {
            _currentContext = context;
            _hasPreparedRequest = true;
        }

        public RunContext ConsumeForLaunch()
        {
            if (_hasPreparedRequest == false)
            {
                _currentContext = RunContext.Normal;
                return _currentContext;
            }

            _hasPreparedRequest = false;
            return _currentContext;
        }

        public void PrepareRetry()
        {
            Prepare(_currentContext);
        }
    }
}
