using System;
using System.Collections.Generic;
using System.Globalization;

namespace CapCutDataParser
{
    internal static class CapCutJsonUtilities
    {
        public static Dictionary<string, object> GetObject(Dictionary<string, object> source, string key)
        {
            if (source != null && source.TryGetValue(key, out var value))
            {
                return value as Dictionary<string, object>;
            }

            return null;
        }

        public static List<object> GetList(Dictionary<string, object> source, string key)
        {
            if (source != null && source.TryGetValue(key, out var value))
            {
                return value as List<object>;
            }

            return null;
        }

        public static IEnumerable<Dictionary<string, object>> EnumerateObjects(List<object> items)
        {
            if (items == null)
            {
                yield break;
            }

            foreach (var item in items)
            {
                if (item is Dictionary<string, object> dict)
                {
                    yield return dict;
                }
            }
        }

        public static string GetString(Dictionary<string, object> obj, string key)
        {
            if (obj == null || !obj.TryGetValue(key, out var value))
            {
                return null;
            }

            return ConvertToString(value);
        }

        public static long GetLong(Dictionary<string, object> obj, string key)
        {
            if (obj == null || !obj.TryGetValue(key, out var value) || value == null)
            {
                return 0;
            }

            if (value is double d)
            {
                return (long)Math.Round(d);
            }

            if (value is long l)
            {
                return l;
            }

            if (value is int i)
            {
                return i;
            }

            if (value is string s && long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return 0;
        }

        private static string ConvertToString(object value)
        {
            if (value == null)
            {
                return null;
            }

            switch (value)
            {
                case string s:
                    return s;

                case double d:
                    return d.ToString(CultureInfo.InvariantCulture);

                case long l:
                    return l.ToString(CultureInfo.InvariantCulture);

                case int i:
                    return i.ToString(CultureInfo.InvariantCulture);

                case bool b:
                    return b ? "true" : "false";

                default:
                    return value.ToString();
            }
        }
    }
}