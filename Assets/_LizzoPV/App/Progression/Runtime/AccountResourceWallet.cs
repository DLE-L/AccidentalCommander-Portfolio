using System;

namespace Lizzo.PV.Flow
{
    public interface IAccountResourceWalletStore
    {
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
        void Save();
    }

    public sealed class AccountResourceWallet
    {
        const string KeyPrefix = "lizzo.account_wallet.v1.";

        readonly IAccountResourceWalletStore _store;

        public AccountResourceWallet(IAccountResourceWalletStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public int GetBalance(AccountResourceKind kind)
        {
            return Math.Max(0, _store.GetInt(ResolveKey(kind), 0));
        }

        public int Credit(AccountResourceKind kind, int amount)
        {
            ValidateAmount(amount);

            string key = ResolveKey(kind);
            int current = Math.Max(0, _store.GetInt(key, 0));
            int next = current > int.MaxValue - amount
                ? int.MaxValue
                : current + amount;
            if (next == current)
                return current;

            _store.SetInt(key, next);
            _store.Save();
            return next;
        }

        public bool TryDebit(AccountResourceKind kind, int amount)
        {
            ValidateAmount(amount);

            string key = ResolveKey(kind);
            int current = Math.Max(0, _store.GetInt(key, 0));
            if (current < amount)
                return false;

            _store.SetInt(key, current - amount);
            _store.Save();
            return true;
        }

        static string ResolveKey(AccountResourceKind kind)
        {
            switch (kind)
            {
                case AccountResourceKind.Gold:
                    return KeyPrefix + "gold";
                case AccountResourceKind.LegionScroll:
                    return KeyPrefix + "legion_scroll";
                case AccountResourceKind.ExpeditionTicket:
                    return KeyPrefix + "expedition_ticket";
                case AccountResourceKind.Seal:
                    return KeyPrefix + "seal";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(kind),
                        kind,
                        "Resource is not eligible for the account-wide wallet.");
            }
        }

        static void ValidateAmount(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
        }
    }
}
