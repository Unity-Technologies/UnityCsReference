// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class DictionaryExtensions
    {
        public static T Get<T>(this IDictionary<string, object> dict, string key) where T : class
        {
            if (key == null)
                return null;
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
            if (key == null)
                return fallbackValue;
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

        public static T Get<T>(this IDictionary<long, T> dict, long key) where T : class
        {
            return dict.TryGetValue(key, out var result) ? result : null;
        }

        public static T Get<T>(this IDictionary<string, T> dict, string key) where T : class
        {
            return key != null && dict.TryGetValue(key, out var result) ? result : null;
        }

        public static T Get<T>(this IDictionary<string, T> dict, string key, T fallbackValue = default(T)) where T : struct
        {
            return key != null && dict.TryGetValue(key, out var result) ? result : fallbackValue;
        }

        public static IDictionary<string, object> GetDictionary(this IDictionary<string, object> dict, string key)
        {
            return Get<IDictionary<string, object>>(dict, key);
        }

        public static T[] GetNewArray<T>(this IDictionary<string, object> dict, string key) where T : class
        {
            var list = Get<IList>(dict, key);
            if (list == null)
                return null;
            var outputArray = new T[list.Count];
            for (var i = 0; i < list.Count; i++)
                outputArray[i] = list[i] as T;
            return outputArray;
        }

        public static IEnumerable<T> GetEnumerable<T>(this IDictionary<string, object> dict, string key) where T : class
        {
            return Get<IList>(dict, key)?.FilterByType<T>();
        }

        public static string GetString(this IDictionary<string, object> dict, string key)
        {
            return Get<string>(dict, key);
        }

        public static long GetStringAsLong(this IDictionary<string, object> dict, string key, long fallbackValue = 0)
        {
            var stringValue = Get<string>(dict, key);
            return long.TryParse(stringValue, out var result) ? result : fallbackValue;
        }
    }
}
