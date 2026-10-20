// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Entities;
using Unity.Scripting.LifecycleManagement;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    // Installs what the Entities module needs from the editor; module code can't reference UnityEditor.
    static partial class EditorLightShimHooks
    {
        [OnCodeLoaded]
        static void Install() => EditorLightShim.InstallHooks(new Hooks());

        sealed class Hooks : EditorLightShim.IHooks
        {
            public IEnumerable<Type> GetTypesDerivedFrom(Type type) => TypeCache.GetTypesDerivedFrom(type);

            public IEnumerable<Type> GetTypesWithAttribute(Type attributeType) => TypeCache.GetTypesWithAttribute(attributeType);

            public bool CurrentThreadIsMainThread() => InternalEditorUtility.CurrentThreadIsMainThread();

            public Stream OpenRead(string path) => FileUtil.OpenRead(path);

            public string ComponentToJson(object component) => JsonUtility.ToJson(component, true);

            public string GetGlobalObjectId(object unityObject) =>
                GlobalObjectId.GetGlobalObjectIdSlow((UnityEngine.Object)unityObject).ToString();

            public object GlobalObjectIdToObject(string globalObjectId) =>
                GlobalObjectId.TryParse(globalObjectId, out var id) ? GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) : null;
        }
    }
}
