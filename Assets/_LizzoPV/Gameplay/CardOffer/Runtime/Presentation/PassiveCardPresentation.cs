using System.Globalization;
using Lizzo.PV.Data;

namespace Lizzo.PV.P0.Cards
{
    public static class PassiveCardPresentation
    {
        public static string FormatCurrentToNext(PassiveData data, int currentLevel)
        {
            if (data == null) return string.Empty;
            float next = currentLevel switch { 0 => data.Level1Value, 1 => data.Level2Value, _ => data.Level3Value };
            string value = data.ValueType.IndexOf("multiplier", System.StringComparison.Ordinal) >= 0
                ? ((next - 1.0f) * 100.0f).ToString("0.##", CultureInfo.InvariantCulture)
                : next.ToString("0.##", CultureInfo.InvariantCulture);
            string token = data.ValueType.IndexOf("multiplier", System.StringComparison.Ordinal) >= 0 ? "{percent}" : "{value}";
            return data.DescriptionTemplateKo.Replace(token, value);
        }
    }
}
