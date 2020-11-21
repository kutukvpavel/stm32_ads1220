using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AdcControl
{
    public static class DictionarySaver
    {
        private static readonly char DictionaryDelimeter = '=';
        private static readonly string DictionaryMappingFormat = "{0:X}" + DictionaryDelimeter + "{1}";

        public static ConcurrentDictionary<int, T> Parse<T>(
            System.Collections.Specialized.StringCollection collection,
            Func<string, T> valueSelector)
        {
            return new ConcurrentDictionary<int, T>(collection.Cast<string>().Select(x => ParseMapping(x, valueSelector)));
        }

        public static KeyValuePair<int, T> ParseMapping<T>(string s, Func<string, T> valueSelector)
        {
            return new KeyValuePair<int, T>(
                int.Parse(s.Split(DictionaryDelimeter).First(), NumberStyles.HexNumber),
                valueSelector(s.Split(DictionaryDelimeter).Last())
                );
        }

        public static void Save<T>(
            System.Collections.Specialized.StringCollection collection,
            ConcurrentDictionary<int, T> dictionary)
        {
            collection.Clear();
            collection.AddRange(dictionary.Select(x => WriteMapping(x)).ToArray());
        }

        public static string WriteMapping<T>(KeyValuePair<int, T> pair)
        {
            return WriteMapping(pair.Key, pair.Value);
        }

        public static string WriteMapping<T>(int key, T value)
        {
            return string.Format(DictionaryMappingFormat, key, value.ToString());
        }
    }
}
