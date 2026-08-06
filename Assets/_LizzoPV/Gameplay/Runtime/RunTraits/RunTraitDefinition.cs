using System;

namespace Lizzo.PV.Gameplay.RunTraits
{
    public static class RunTraitCategories
    {
        public const string BuildRelated = "build-related";
        public const string General = "general";
        public const string Variant = "variant";
    }

    public sealed class RunTraitDefinition
    {
        public RunTraitDefinition(string id, string displayName, string category)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A run trait ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("A run trait display name is required.", nameof(displayName));
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("A run trait category is required.", nameof(category));

            Id = id;
            DisplayName = displayName;
            Category = category;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Category { get; }
    }
}
