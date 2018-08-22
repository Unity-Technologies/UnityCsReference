// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Android
{
    [NativeHeader("Runtime/Export/AndroidPermissions.bindings.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct Permission
    {
        public const string Camera = "android.permission.CAMERA";
        public const string Microphone = "android.permission.RECORD_AUDIO";
        public const string FineLocation = "android.permission.ACCESS_FINE_LOCATION";
        public const string CoarseLocation = "android.permission.ACCESS_COARSE_LOCATION";
        public const string ExternalStorageRead = "android.permission.READ_EXTERNAL_STORAGE";
        public const string ExternalStorageWrite = "android.permission.WRITE_EXTERNAL_STORAGE";

        [StaticAccessor("PermissionsBindings", StaticAccessorType.DoubleColon)]
        extern public static bool HasUserAuthorizedPermission(string permission);

        [StaticAccessor("PermissionsBindings", StaticAccessorType.DoubleColon)]
        extern public static void RequestUserPermission(string permission);
    }
}
