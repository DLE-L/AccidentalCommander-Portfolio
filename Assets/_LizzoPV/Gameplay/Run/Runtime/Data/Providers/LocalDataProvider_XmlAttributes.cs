using System;
using System.Globalization;
using System.Xml.Linq;
using UnityEngine;

namespace Lizzo.PV.Data
{
    public sealed partial class LocalDataProvider
    {
        private string StringAttr(XElement element, string name, string fallback)
        {
            return element.Attribute(name)?.Value ?? fallback;
        }

        private int IntAttr(XElement element, string name, int fallback)
        {
            string value = element.Attribute(name)?.Value;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
                ? result
                : fallback;
        }

        private float FloatAttr(XElement element, string name, float fallback)
        {
            string value = element.Attribute(name)?.Value;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)
                ? result
                : fallback;
        }

        private Color ColorAttr(XElement element, string name, Color fallback)
        {
            string value = element.Attribute(name)?.Value;
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            return ColorUtility.TryParseHtmlString(value, out Color color)
                ? color
                : fallback;
        }

        private T EnumAttr<T>(XElement element, string name, T fallback) where T : struct
        {
            string value = element.Attribute(name)?.Value;
            return Enum.TryParse(value, true, out T result) ? result : fallback;
        }
    }
}
