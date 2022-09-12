// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Android
{
    public class PermissionCallbacks : AndroidJavaProxy
    {
        public event Action<string> PermissionGranted;
        public event Action<string> PermissionDenied;
        public event Action<string> PermissionDeniedAndDontAskAgain;

        public PermissionCallbacks()
            : base("com.unity3d.player.IPermissionRequestCallbacks")
        {}

        // Preserve is needed on Android, since AndroidJavaProxy accesses these methods via reflection.
        // Thus they will get stripped, since they're not referenced directlry
        // On other hand, this has a bad side effect on platforms like WebGL, where stripper would keep these methods
        // And what's worse will include the whole module because of this
        private void onPermissionGranted(string permissionName)
        {
            PermissionGranted?.Invoke(permissionName);
        }

        private void onPermissionDenied(string permissionName)
        {
            PermissionDenied?.Invoke(permissionName);
        }

        private void onPermissionDeniedAndDontAskAgain(string permissionName)
        {
            if (PermissionDeniedAndDontAskAgain != null)
            {
                PermissionDeniedAndDontAskAgain(permissionName);
            }
            else
            {
                // Fall back to OnPermissionDeniedAction
                PermissionDenied?.Invoke(permissionName);
            }
        }
    }

    public struct Permission
    {
        public const string Camera = "android.permission.CAMERA";
        public const string Microphone = "android.permission.RECORD_AUDIO";
        public const string FineLocation = "android.permission.ACCESS_FINE_LOCATION";
        public const string CoarseLocation = "android.permission.ACCESS_COARSE_LOCATION";
        public const string ExternalStorageRead = "android.permission.READ_EXTERNAL_STORAGE";
        public const string ExternalStorageWrite = "android.permission.WRITE_EXTERNAL_STORAGE";

        private static AndroidJavaObject m_UnityPermissions;

        private static AndroidJavaObject GetUnityPermissions()
        {
            if (m_UnityPermissions != null)
                return m_UnityPermissions;
            m_UnityPermissions = new AndroidJavaClass("com.unity3d.player.UnityPermissions");
            return m_UnityPermissions;
        }

        public static bool HasUserAuthorizedPermission(string permission)
        {
            if (permission == null)
                return false;
            return true;
        }

        public static void RequestUserPermission(string permission)
        {
            if (permission == null)
                return;
            RequestUserPermissions(new[] { permission }, null);
        }

        public static void RequestUserPermissions(string[] permissions)
        {
            if (permissions == null || permissions.Length == 0)
                return;
            RequestUserPermissions(permissions, null);
        }

        public static void RequestUserPermission(string permission, PermissionCallbacks callbacks)
        {
            if (permission == null)
                return;
            RequestUserPermissions(new[] { permission }, callbacks);
        }

        public static void RequestUserPermissions(string[] permissions, PermissionCallbacks callbacks)
        {
            if (permissions == null || permissions.Length == 0)
                return;
        }
    }
}
