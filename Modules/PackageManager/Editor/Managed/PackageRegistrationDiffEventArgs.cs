// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.ObjectModel;
using UnityEngine.Scripting;

namespace UnityEditor.PackageManager
{
    public class PackageRegistrationEventArgs
    {
        public ReadOnlyCollection<PackageInfo> added { get; private set; }
        public ReadOnlyCollection<PackageInfo> removed { get; private set; }
        public ReadOnlyCollection<PackageInfo> changedFrom { get; private set; }
        public ReadOnlyCollection<PackageInfo> changedTo { get; private set; }

        private static extern PackageInfo[] Internal_GetAddedPackages(IntPtr nativeHandle);
        private static extern PackageInfo[] Internal_GetRemovedPackages(IntPtr nativeHandle);
        private static extern PackageInfo[] Internal_GetChangedFromPackages(IntPtr nativeHandle);
        private static extern PackageInfo[] Internal_GetChangedToPackages(IntPtr nativeHandle);

        [RequiredByNativeCode]
        private static PackageRegistrationEventArgs InstantiateFromNative(IntPtr nativeHandle)
        {
            return new PackageRegistrationEventArgs(nativeHandle);
        }

        private PackageRegistrationEventArgs(IntPtr nativeHandle)
        {
            PopulateFromNative(nativeHandle);
        }

        private void PopulateFromNative(IntPtr nativeHandle)
        {
            added = Array.AsReadOnly(Internal_GetAddedPackages(nativeHandle));
            removed = Array.AsReadOnly(Internal_GetRemovedPackages(nativeHandle));
            changedTo = Array.AsReadOnly(Internal_GetChangedToPackages(nativeHandle));
            changedFrom = Array.AsReadOnly(Internal_GetChangedFromPackages(nativeHandle));
        }
    }
}
