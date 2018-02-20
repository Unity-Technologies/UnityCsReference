// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Modules/JSONSerialize/Public/JsonUtility.bindings.h")]
    public static class JsonUtility
    {
        [FreeFunction("ToJsonInternal", true)]
        [ThreadSafe]
        private static extern string ToJsonInternal([NotNull] object obj, bool prettyPrint);

        [FreeFunction("FromJsonInternal", true, ThrowsException = true)]
        [ThreadSafe]
        private static extern object FromJsonInternal(string json, object objectToOverwrite, Type type);

        public static string ToJson(object obj) { return ToJson(obj, false); }

        public static string ToJson(object obj, bool prettyPrint)
        {
            if (obj == null)
                return "";

            if (obj is UnityEngine.Object && !(obj is MonoBehaviour || obj is ScriptableObject))
                throw new ArgumentException("JsonUtility.ToJson does not support engine types.");

            return ToJsonInternal(obj, prettyPrint);
        }

        public static T FromJson<T>(string json) { return (T)FromJson(json, typeof(T)); }

        public static object FromJson(string json, Type type)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            if (type == null)
                throw new ArgumentNullException("type");

            if (type.IsAbstract || type.IsSubclassOf(typeof(UnityEngine.Object)))
                throw new ArgumentException("Cannot deserialize JSON to new instances of type '" + type.Name + ".'");

            return FromJsonInternal(json, null, type);
        }

        public static void FromJsonOverwrite(string json, object objectToOverwrite)
        {
            if (string.IsNullOrEmpty(json))
                return;

            if (objectToOverwrite == null)
                throw new ArgumentNullException("objectToOverwrite");

            if (objectToOverwrite is UnityEngine.Object && !(objectToOverwrite is MonoBehaviour || objectToOverwrite is ScriptableObject))
                throw new ArgumentException("Engine types cannot be overwritten from JSON outside of the Editor.");

            FromJsonInternal(json, objectToOverwrite, objectToOverwrite.GetType());
        }
    }
}
