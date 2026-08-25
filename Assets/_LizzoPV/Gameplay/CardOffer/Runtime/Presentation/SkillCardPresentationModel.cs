using UnityEngine;

namespace Lizzo.PV.UI
{
    internal readonly struct SkillCardPresentationModel
    {
        public SkillCardPresentationModel(
            string title,
            string description,
            string badge,
            string roleBadge,
            string synergyHint,
            Sprite portrait,
            bool isCompanion,
            int ownedCompanionCount,
            int previewCompanionIndex,
            bool isPassive,
            int ownedPassiveCount,
            int previewPassiveIndex,
            bool hasStatus,
            string statusText,
            bool highlightFrame,
            bool recommended)
        {
            Title = title;
            Description = description;
            Badge = badge;
            RoleBadge = roleBadge;
            SynergyHint = synergyHint;
            Portrait = portrait;
            IsCompanion = isCompanion;
            OwnedCompanionCount = ownedCompanionCount;
            PreviewCompanionIndex = previewCompanionIndex;
            IsPassive = isPassive;
            OwnedPassiveCount = ownedPassiveCount;
            PreviewPassiveIndex = previewPassiveIndex;
            HasStatus = hasStatus;
            StatusText = statusText;
            HighlightFrame = highlightFrame;
            Recommended = recommended;
        }

        public string Title { get; }
        public string Description { get; }
        public string Badge { get; }
        public string RoleBadge { get; }
        public string SynergyHint { get; }
        public Sprite Portrait { get; }
        public bool IsCompanion { get; }
        public int OwnedCompanionCount { get; }
        public int PreviewCompanionIndex { get; }
        public bool IsPassive { get; }
        public int OwnedPassiveCount { get; }
        public int PreviewPassiveIndex { get; }
        public bool HasStatus { get; }
        public string StatusText { get; }
        public bool HighlightFrame { get; }
        public bool Recommended { get; }
    }

}
