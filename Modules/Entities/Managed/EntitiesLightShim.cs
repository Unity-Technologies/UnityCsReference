// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.IO.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace Unity.Entities
{
    [VisibleToOtherModules("UnityEngine.CoreModule")]
    static partial class EntitiesLightShim
    {
        [VisibleToOtherModules("UnityEngine.CoreModule")]
        internal interface IHooks
        {
            void RemoveWorldsFromCurrentPlayerLoop(IEnumerable<World> worlds);
            void RemoveWorldFromCurrentPlayerLoop(World world);
            void AppendWorldToCurrentPlayerLoop(World world);
            bool IsWorldInCurrentPlayerLoop(World world);
            void UnregisterFrameUpdateToCurrentPlayerLoop();
            void RegisterFrameUpdateToCurrentPlayerLoop();

            void ShutdownEntitiesProfilerReporting();

            void InitializeEntitiesProfilerReporting();

            bool IsAssetImportWorkerProcess();
        }

        [AutoStaticsCleanupOnCodeReload] // The com.unity.entities package installs the hooks again as its code loads.
        static IHooks s_Hooks;

        [VisibleToOtherModules("UnityEngine.CoreModule")]
        internal static void InstallHooks(IHooks hooks) => s_Hooks = hooks;

        internal static bool IsInstalled => s_Hooks != null;

        internal static IHooks Hooks => s_Hooks ?? throw new InvalidOperationException(
            "Entities needs the com.unity.entities package, but its hooks are not installed. The package installs them when its code loads.");
    }
}
