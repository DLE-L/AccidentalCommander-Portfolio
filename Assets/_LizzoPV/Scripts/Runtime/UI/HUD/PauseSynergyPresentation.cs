namespace Lizzo.PV.UI
{
    public readonly struct PauseSynergyPresentation
    {
        public string Id { get; }
        public string DisplayName { get; }

        public PauseSynergyPresentation(string id, string displayName)
        {
            Id = id ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
        }
    }
}
