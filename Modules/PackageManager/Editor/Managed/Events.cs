// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.PackageManager
{
    public static partial class Events
    {
        [AutoStaticsCleanupOnCodeReload]
        public static event Action<PackageRegistrationEventArgs> registeringPackages;
        [AutoStaticsCleanupOnCodeReload]
        // Subscribers attach through their own lifecycle - ModeService re-subscribes from its [OnCodeLoaded]
        // initializer, and the windows and UI elements re-subscribe on enable/attach - so the cleared
        // invocation list refills itself.
        [IgnoreForUAL0015("Event whose subscribers re-register through their own lifecycle after a code reload")]
        public static event Action<PackageRegistrationEventArgs> registeredPackages;

        [RequiredByNativeCode]
        internal static void InvokeRegisteringPackages(IntPtr nativePackageDiffHandle)
        {
            if (registeringPackages != null)
                registeringPackages?.Invoke(new PackageRegistrationEventArgs(nativePackageDiffHandle));
        }

        [RequiredByNativeCode]
        internal static void InvokeRegisteredPackages(IntPtr nativePackageDiffHandle)
        {
            if (registeredPackages != null)
                registeredPackages?.Invoke(new PackageRegistrationEventArgs(nativePackageDiffHandle));
        }
    }
}
