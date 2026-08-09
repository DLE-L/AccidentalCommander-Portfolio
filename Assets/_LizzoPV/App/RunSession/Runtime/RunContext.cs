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

    public readonly struct RunContext : IEquatable<RunContext>
    {
        public static RunContext Normal => new RunContext(RunMode.Normal);
        public static RunContext Tutorial => new RunContext(RunMode.Tutorial);

        public RunMode Mode { get; }
        public CommanderWeaponId CommanderWeapon { get; }
        public bool IsTutorial => Mode == RunMode.Tutorial;
        public bool IsNormal => Mode == RunMode.Normal;
        public bool HasCommanderWeapon => CommanderWeaponCatalog.IsSelectable(CommanderWeapon);

        public RunContext(RunMode mode)
            : this(mode, CommanderWeaponId.None)
        {
        }

        public RunContext(RunMode mode, CommanderWeaponId commanderWeapon)
        {
            if (mode != RunMode.Normal && mode != RunMode.Tutorial)
                throw new ArgumentOutOfRangeException(nameof(mode));
            if (commanderWeapon != CommanderWeaponId.None &&
                CommanderWeaponCatalog.IsSelectable(commanderWeapon) == false)
                throw new ArgumentOutOfRangeException(nameof(commanderWeapon));

            Mode = mode;
            CommanderWeapon = commanderWeapon;
        }

        public bool Equals(RunContext other) =>
            Mode == other.Mode && CommanderWeapon == other.CommanderWeapon;

        public override bool Equals(object obj) => obj is RunContext other && Equals(other);

        public override int GetHashCode() => HashCode.Combine((int)Mode, (int)CommanderWeapon);

        public override string ToString()
        {
            string mode = Mode == RunMode.Tutorial ? "tutorial" : "normal";
            string weapon = CommanderWeaponCatalog.ToId(CommanderWeapon);
            return string.IsNullOrEmpty(weapon) ? mode : mode + ":" + weapon;
        }
    }

    public sealed class RunLaunchState
    {
        RunContext _currentContext = RunContext.Normal;
        bool _hasPreparedRequest;

        public RunContext CurrentContext => _currentContext;
        public bool HasPreparedRequest => _hasPreparedRequest;

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
