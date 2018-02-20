// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Modules/JSONSerializeEditor/EditorJsonUtility.bindings.h")]
    public static class EditorJsonUtility
    {
        [FreeFunction("ToEditorJsonInternal")]
        private static extern string ToJsonInternal([NotNull] object obj, bool prettyPrint);

        public static string ToJson(object obj) { return ToJson(obj, false); }

        public static string ToJson(object obj, bool prettyPrint)
        {
            if (obj == null)
                return "";

            return ToJsonInternal(obj, prettyPrint);
        }

        [FreeFunction("FromEditorJsonOverwriteInternal", ThrowsException = true)]
        private static extern void FromJsonOverwriteInternal([NotNull] string json, [NotNull] object objectToOverwrite);

        public static void FromJsonOverwrite(string json, object objectToOverwrite)
        {
            if (string.IsNullOrEmpty(json))
                return;

            if (objectToOverwrite == null || (objectToOverwrite is UnityEngine.Object && !((UnityEngine.Object)objectToOverwrite)))
                throw new ArgumentNullException("objectToOverwrite");

            FromJsonOverwriteInternal(json, objectToOverwrite);
        }
    }
}
