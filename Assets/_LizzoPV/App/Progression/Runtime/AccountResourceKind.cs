using System;

namespace Lizzo.PV.Flow
{
    [Flags]
    public enum AccountResourceKind
    {
        None = 0,
        Gold = 1 << 0,
        LegionScroll = 1 << 1,
        LegionPiece = 1 << 2,
        ExpeditionTicket = 1 << 3,
        Seal = 1 << 4,
    }
}
