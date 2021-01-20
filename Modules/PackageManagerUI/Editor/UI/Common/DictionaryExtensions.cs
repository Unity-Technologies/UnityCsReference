// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class DictionaryExtensions
    {
        public static T Get<T>(this IDictionary<string, object> dict, string key) where T : class
        {
            object result;
            return dict.TryGetValue(key, out result) ? (T)result : null;
        }

        public static T Get<T>(this IDictionary<string, object> dict, string key, T fallbackValue = default(T)) where T : struct
        {
            object result;
            return dict.TryGetValue(key, out result) ? (T)result : fallbackValue;
        }

        public static T Get<T>(this IDictionary<string, T> dict, string key) where T : class
        {
            T result;
            return dict.TryGetValue(key, out result) ? result : null;
        }

        public static T Get<T>(this IDictionary<string, T> dict, string key, T fallbackValue = default(T)) where T : struct
        {
            T result;
            return dict.TryGetValue(key, out result) ? result : fallbackValue;
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
    }
}
