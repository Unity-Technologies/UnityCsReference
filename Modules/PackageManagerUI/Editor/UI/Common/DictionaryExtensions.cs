// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class DictionaryExtensions
    {
        public static T Get<T>(this IDictionary<string, object> dict, string key) where T : class
        {
            var result = dict.TryGetValue(key, out var value);
            try
            {
                return result ? (T)value : null;
            }
            catch (InvalidCastException)
            {
                throw new IncorrectFieldTypeException(key, typeof(T), value.GetType());
            }
        }

        public static T Get<T>(this IDictionary<string, object> dict, string key, T fallbackValue = default(T)) where T : struct
        {
            var result = dict.TryGetValue(key, out var value);
            try
            {
                return result ? (T)value: fallbackValue;
            }
            catch (InvalidCastException)
            {
                throw new IncorrectFieldTypeException(key, typeof(T), value.GetType());
            }
        }

        public static T Get<T>(this IDictionary<string, T> dict, string key) where T : class
        {
            return dict.TryGetValue(key, out var result) ? result : null;
        }

        public static T Get<T>(this IDictionary<string, T> dict, string key, T fallbackValue = default(T)) where T : struct
        {
            return dict.TryGetValue(key, out var result) ? result : fallbackValue;
        }

        public static IDictionary<string, object> GetDictionary(this IDictionary<string, object> dict, string key)
        {
            return Get<IDictionary<string, object>>(dict, key);
        }

        public static IEnumerable<T> GetList<T>(this IDictionary<string, object> dict, string key)
        {
            return Get<IList>(dict, key)?.OfType<T>();
        }

        public static string GetString(this IDictionary<string, object> dict, string key)
        {
            return Get<string>(dict, key);
        }

        public static long GetStringAsLong(this IDictionary<string, object> dict, string key, long fallbackValue = default(long))
        {
            var stringValue = Get<string>(dict, key);
            return long.TryParse(stringValue, out var result) ? result : fallbackValue;
        }
    }
}
