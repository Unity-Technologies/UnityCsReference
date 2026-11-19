// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace Unity.Entities
{
    [VisibleToOtherModules("UnityEngine.CoreModule")]
    internal static partial class RuntimeApplication
    {
        /// <summary>
        /// Event invoked before a frame update.
        /// </summary>
        [AutoStaticsCleanupOnCodeReload]
        public static event Action PreFrameUpdate;

        /// <summary>
        /// Event invoked after a frame update.
        /// </summary>
        [AutoStaticsCleanupOnCodeReload]
        public static event Action PostFrameUpdate;

        [VisibleToOtherModules("UnityEngine.CoreModule")]
        internal static void InvokePreFrameUpdate() => PreFrameUpdate?.Invoke();
        [VisibleToOtherModules("UnityEngine.CoreModule")]
        internal static void InvokePostFrameUpdate() => PostFrameUpdate?.Invoke();
    }
}
