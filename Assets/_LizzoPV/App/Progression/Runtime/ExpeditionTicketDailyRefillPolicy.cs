using System;

namespace Lizzo.PV.Flow
{
    public static class ExpeditionTicketDailyRefillPolicy
    {
        public static int ResolveBalance(int currentBalance, int dailyBaseline)
        {
            if (currentBalance < 0)
                throw new ArgumentOutOfRangeException(nameof(currentBalance));
            if (dailyBaseline < 0)
                throw new ArgumentOutOfRangeException(nameof(dailyBaseline));

            return Math.Max(currentBalance, dailyBaseline);
        }
    }
}
