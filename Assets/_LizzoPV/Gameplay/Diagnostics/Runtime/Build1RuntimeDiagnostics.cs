using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Lizzo.PV.Gameplay.Diagnostics
{
    public static class Build1RuntimeDiagnostics
    {
        public readonly struct Field
        {
            internal Field(string key, object value)
            {
                Key = key;
                Value = value;
            }

            internal string Key { get; }
            internal object Value { get; }
        }

        static long _sequence;

        public static Field Text(string key, string value) => new Field(key, value);
        public static Field Int(string key, int value) => new Field(key, value);
        public static Field Long(string key, long value) => new Field(key, value);
        public static Field Float(string key, float value) => new Field(key, value);
        public static Field Bool(string key, bool value) => new Field(key, value);

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string eventName, params Field[] fields)
        {
            StringBuilder line = new StringBuilder(160);
            line.Append("[BUILD1_DIAG] seq=");
            line.Append(++_sequence);
            line.Append(" time=");
            line.Append(Time.time.ToString("0.###", CultureInfo.InvariantCulture));
            line.Append(" event=");
            AppendSanitized(line, eventName);

            for (int index = 0; fields != null && index < fields.Length; index++)
            {
                line.Append(' ');
                AppendSanitized(line, fields[index].Key);
                line.Append('=');
                AppendValue(line, fields[index].Value);
            }

            UnityEngine.Debug.Log(line.ToString());
        }

        static void AppendValue(StringBuilder line, object value)
        {
            switch (value)
            {
                case null:
                    line.Append("null");
                    return;
                case float floatValue:
                    line.Append(floatValue.ToString("0.###", CultureInfo.InvariantCulture));
                    return;
                case double doubleValue:
                    line.Append(doubleValue.ToString("0.###", CultureInfo.InvariantCulture));
                    return;
                case bool boolValue:
                    line.Append(boolValue ? "true" : "false");
                    return;
                case IFormattable formattable:
                    line.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                    return;
                default:
                    AppendSanitized(line, value.ToString());
                    return;
            }
        }

        static void AppendSanitized(StringBuilder line, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                line.Append("none");
                return;
            }

            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                line.Append(char.IsWhiteSpace(character) || character == '=' ? '_' : character);
            }
        }
    }
}
