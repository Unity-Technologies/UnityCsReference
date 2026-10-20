// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace Unity.Entities
{
    /// <summary>
    /// What Entities needs from the editor in editor builds. Module code can't reference UnityEditor, so UnityEditor
    /// installs the implementation as its code loads by calling <see cref="InstallHooks"/>.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule", "UnityEditor.CoreModule")]
    static partial class EditorLightShim
    {
        [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule", "UnityEditor.CoreModule")]
        internal interface IHooks
        {
            // TypeCache
            IEnumerable<Type> GetTypesDerivedFrom(Type type);
            IEnumerable<Type> GetTypesWithAttribute(Type attributeType);

            bool CurrentThreadIsMainThread();
            Stream OpenRead(string path);
            string ComponentToJson(object component);

            // GlobalObjectId
            string GetGlobalObjectId(object unityObject);
            object GlobalObjectIdToObject(string globalObjectId);
        }

        [AutoStaticsCleanupOnCodeReload] // UnityEditor installs the hooks again as code loads.
        static IHooks s_Hooks;

        [VisibleToOtherModules("UnityEditor.CoreModule")]
        internal static void InstallHooks(IHooks hooks) => s_Hooks = hooks;

        internal static IHooks Hooks
        {
            [VisibleToOtherModules("UnityEngine.EntitiesPropertiesModule")]
            get => s_Hooks;
        }
    }
}
