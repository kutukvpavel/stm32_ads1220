using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AdcControl
{
    public static class DictionarySerializer
    {
        private static readonly char DictionaryDelimeter = '=';
        private static readonly string DictionaryMappingFormat = "{0:X}" + DictionaryDelimeter + "{1}";

        public static ConcurrentDictionary<int, T> Parse<T>(
            System.Collections.Specialized.StringCollection collection,
            Func<string, T> valueSelector, bool substituteNull = true)
        {
            return new ConcurrentDictionary<int, T>(collection.Cast<string>()
                .Select(x => ParseMapping(x, valueSelector, substituteNull)));
        }

        public static KeyValuePair<int, T> ParseMapping<T>(string s, Func<string, T> valueSelector, bool substituteNull = true)
        {
            int delimeterIndex = s.IndexOf(DictionaryDelimeter);
            if (delimeterIndex == 0) throw new ArgumentException("Channel index can't be null.");
            string key = s.Substring(0, delimeterIndex > -1 ? delimeterIndex : s.Length);
            string value;
            if (!substituteNull && delimeterIndex < 0)
            {
                value = null;
            }
            else
            {
                if (++delimeterIndex == s.Length)
                {
                    if (substituteNull)
                    {
                        value = key;
                    }
                    else
                    {
                        value = null;
                    }
                }
                else
                {
                    value = s.Substring(delimeterIndex > 0 ? delimeterIndex : 0);
                }
            }
            return new KeyValuePair<int, T>(
                int.Parse(key, NumberStyles.HexNumber),
                valueSelector(value)
                );
        }

        public static void Save<T>(
            System.Collections.Specialized.StringCollection collection,
            ConcurrentDictionary<int, T> dictionary, Func<T, string> valueSelector)
        {
            collection.Clear();
            collection.AddRange(dictionary.Select(x => WriteMapping(x, valueSelector)).ToArray());
        }

        public static string WriteMapping<T>(KeyValuePair<int, T> pair, Func<T, string> valueSelector)
        {
            return WriteMapping(pair.Key, pair.Value, valueSelector);
        }

        public static string WriteMapping<T>(int key, T value, Func<T, string> valueSelector)
        {
            return string.Format(DictionaryMappingFormat, key, valueSelector(value));
        }
    }
}
