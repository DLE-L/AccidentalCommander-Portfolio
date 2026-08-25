using UnityEngine;

namespace Lizzo.PV.Gameplay.CardOffer
{
    public readonly struct GameplayCardOfferItemPresentation
    {
        public GameplayCardOfferItemPresentation(
            string cardId,
            string title,
            string description,
            string value,
            string status,
            Sprite portrait,
            string relation,
            bool showProgress,
            int progressCount,
            bool recommended)
        {
            CardId = cardId ?? string.Empty;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Value = value ?? string.Empty;
            Status = status ?? string.Empty;
            Portrait = portrait;
            Relation = relation ?? string.Empty;
            ShowProgress = showProgress;
            ProgressCount = progressCount;
            Recommended = recommended;
        }

        public string CardId { get; }
        public string Title { get; }
        public string Description { get; }
        public string Value { get; }
        public string Status { get; }
        public Sprite Portrait { get; }
        public string Relation { get; }
        public bool ShowProgress { get; }
        public int ProgressCount { get; }
        public bool Recommended { get; }
    }

}
