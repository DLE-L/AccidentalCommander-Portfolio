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

    public enum RunStartMode
    {
        Fresh,
        Resume,
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

    public sealed class RunStartRequest
    {
        public string RequestId { get; }
        public RunContext Context { get; }
        public RunStartMode StartMode { get; }
        public RunSnapshot Snapshot { get; }
        public RunDefinition Definition { get; }
        public bool IsResolved => Definition != null;

        RunStartRequest(
            string requestId,
            RunContext context,
            RunStartMode startMode,
            RunSnapshot snapshot,
            RunDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("Run request id is required.", nameof(requestId));
            if (startMode != RunStartMode.Fresh && startMode != RunStartMode.Resume)
                throw new ArgumentOutOfRangeException(nameof(startMode));
            if (startMode == RunStartMode.Fresh && snapshot != null)
                throw new ArgumentException("Fresh runs cannot include a snapshot.", nameof(snapshot));
            if (startMode == RunStartMode.Resume && snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            RequestId = requestId;
            Context = context;
            StartMode = startMode;
            Snapshot = snapshot;
            Definition = definition;
        }

        public static RunStartRequest Fresh(RunContext context, string requestId = null)
        {
            return new RunStartRequest(
                ResolveRequestId(requestId),
                context,
                RunStartMode.Fresh,
                null,
                null);
        }

        public static RunStartRequest Fresh(
            RunContext context,
            RunDefinition definition,
            string requestId = null)
        {
            return new RunStartRequest(
                ResolveRequestId(requestId),
                context,
                RunStartMode.Fresh,
                null,
                definition ?? throw new ArgumentNullException(nameof(definition)));
        }

        public static RunStartRequest Resume(
            RunContext context,
            RunSnapshot snapshot,
            string requestId = null)
        {
            return new RunStartRequest(
                ResolveRequestId(requestId),
                context,
                RunStartMode.Resume,
                snapshot,
                null);
        }

        public static RunStartRequest Resume(
            RunContext context,
            RunSnapshot snapshot,
            RunDefinition definition,
            string requestId = null)
        {
            return new RunStartRequest(
                ResolveRequestId(requestId),
                context,
                RunStartMode.Resume,
                snapshot,
                definition ?? throw new ArgumentNullException(nameof(definition)));
        }

        public RunStartRequest Resolve(Lizzo.PV.Data.IDataProvider data)
        {
            if (Definition != null)
                return this;

            return new RunStartRequest(
                RequestId,
                Context,
                StartMode,
                Snapshot,
                RunDefinitionResolver.Resolve(Context, data));
        }

        static string ResolveRequestId(string requestId)
        {
            return string.IsNullOrWhiteSpace(requestId)
                ? Guid.NewGuid().ToString("N")
                : requestId.Trim();
        }
    }

    public sealed class RunLaunchState
    {
        RunStartRequest _currentRequest = RunStartRequest.Fresh(RunContext.Normal);
        bool _hasPreparedRequest;

        public RunContext CurrentContext => _currentRequest.Context;

        public bool TryPrepare(RunStartRequest request, CompanionUnlockProgress progress)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (progress == null)
                throw new ArgumentNullException(nameof(progress));
            if (request.Context.IsNormal && progress.IsStageUnlocked(request.Context.StageId) == false)
                return false;

            Prepare(request);
            return true;
        }

        public void Prepare(RunStartRequest request)
        {
            _currentRequest = request ?? throw new ArgumentNullException(nameof(request));
            _hasPreparedRequest = true;
        }

        public RunStartRequest ConsumeForLaunch()
        {
            if (_hasPreparedRequest == false)
            {
                _currentRequest = RunStartRequest.Fresh(RunContext.Normal);
                return _currentRequest;
            }

            _hasPreparedRequest = false;
            return _currentRequest;
        }

        public void PrepareRetry(RunSnapshot snapshot = null)
        {
            Prepare(snapshot == null
                ? RunStartRequest.Fresh(_currentRequest.Context)
                : RunStartRequest.Resume(_currentRequest.Context, snapshot));
        }
    }
}
