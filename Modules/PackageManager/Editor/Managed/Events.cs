// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    public static class Events
    {
        public static event Action<PackageRegistrationEventArgs> registeringPackages;
        public static event Action<PackageRegistrationEventArgs> registeredPackages;

        [RequiredByNativeCode]
        internal static void InvokeRegisteringPackages(PackageRegistrationEventArgs eventArgs)
        {
            registeringPackages?.Invoke(eventArgs);
        }

        [RequiredByNativeCode]
        internal static void InvokeRegisteredPackages(PackageRegistrationEventArgs eventArgs)
        {
            registeredPackages?.Invoke(eventArgs);
        }
    }
}
