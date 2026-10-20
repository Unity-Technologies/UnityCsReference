// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Entities
{
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

        internal static void InvokePreFrameUpdate() => PreFrameUpdate?.Invoke();
        internal static void InvokePostFrameUpdate() => PostFrameUpdate?.Invoke();
    }
}
