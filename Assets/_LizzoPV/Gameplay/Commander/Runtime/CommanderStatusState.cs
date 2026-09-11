using System;

namespace Lizzo.PV.Gameplay.Commander
{
    [Flags]
    public enum CommanderStatusKind
    {
        None = 0,
        SanctuaryHaste = 1,
        SanctuaryGuard = 2,
        PostHitInvulnerability = 4,
    }

    public sealed class CommanderStatusState
    {
        public CommanderStatusKind Active { get; private set; }
        public event Action<CommanderStatusKind, bool> Changed;
        public bool IsActive(CommanderStatusKind kind) => kind != CommanderStatusKind.None && (Active & kind) == kind;

        public void Publish(CommanderStatusKind active)
        {
            var changed = Active ^ active;
            Active = active;
            for (int bit = 1; bit <= 4; bit <<= 1)
            {
                var kind = (CommanderStatusKind)bit;
                if ((changed & kind) != 0) Changed?.Invoke(kind, (active & kind) != 0);
            }
        }
        public void Clear() => Publish(CommanderStatusKind.None);
    }
}
