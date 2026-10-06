// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEditor.Compilation;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Scripting
{
    public sealed partial class ManagedDebugger
    {
#pragma warning disable CS0067 // Disable unused event warning for CoreCLR
        [Obsolete("ManagedDebugger.debuggerAttached is deprecated for CoreCLR. It is never invoked on CoreCLR.", false)]
        // Public static event: always cleaned up on code reload regardless of visibility, even though
        // this particular event is dead on CoreCLR (obsolete above, and its only raise/subscribe sites
        // are #if !ENABLE_CORECLR) — there is currently nothing to clean, but the attribute stays so it
        // does the right thing if a CoreCLR subscriber is ever added.
        [AutoStaticsCleanupOnCodeReload]
        public static event Action<bool> debuggerAttached;
#pragma warning restore CS0067

        [Obsolete("ManagedDebugger.isAttached is deprecated for CoreCLR. Use System.Diagnostics.Debugger.IsAttached instead.", false)]
        public static bool isAttached
        {
            get { return System.Diagnostics.Debugger.IsAttached; }
        }

        [Obsolete("ManagedDebugger.isEnabled is deprecated for CoreCLR. This property always returns true on CoreCLR.", false)]
        public static bool isEnabled
        {
            get { return true; }
        }


        [Obsolete("ManagedDebugger.Disconnect() is deprecated for CoreCLR. This method does nothing on CoreCLR.", false)]
        public static void Disconnect()
        {
        }

    }
}
