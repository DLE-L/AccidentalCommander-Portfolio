using System;

namespace Lizzo.PV.Flow
{
    public interface ILegionPieceStore
    {
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
        void Save();
    }

    public sealed class LegionPieceLedger
    {
        const string KeyPrefix = "lizzo.legion_piece.v1.";

        readonly ILegionPieceStore _store;

        public LegionPieceLedger(ILegionPieceStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public int GetBalance(string baseUnitId)
        {
            return Math.Max(0, _store.GetInt(ResolveKey(baseUnitId), 0));
        }

        public int Credit(string baseUnitId, int amount)
        {
            ValidateAmount(amount);

            string key = ResolveKey(baseUnitId);
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

        public bool TryDebit(string baseUnitId, int amount)
        {
            ValidateAmount(amount);

            string key = ResolveKey(baseUnitId);
            int current = Math.Max(0, _store.GetInt(key, 0));
            if (current < amount)
                return false;

            _store.SetInt(key, current - amount);
            _store.Save();
            return true;
        }

        static string ResolveKey(string baseUnitId)
        {
            if (string.IsNullOrWhiteSpace(baseUnitId))
                throw new ArgumentException("Canonical base unit id is required.", nameof(baseUnitId));

            return KeyPrefix + baseUnitId;
        }

        static void ValidateAmount(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
        }
    }
}
