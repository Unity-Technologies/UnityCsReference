// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Android
{
    public class PermissionCallbacks : AndroidJavaProxy
    {
        enum Result
        {
            Dismissed = 0,
            Granted = 1,
            Denied = 2,
            DeniedDontAskAgain = 3,
        }

        public event Action<string> PermissionGranted;
        public event Action<string> PermissionDenied;
        public event Action<string> PermissionDeniedAndDontAskAgain;
        public event Action<string> PermissionRequestDismissed;

        public PermissionCallbacks()
            : base("com.unity3d.player.IPermissionRequestCallbacks")
        {}

        // override Invoke so we don't pay for C# reflection
        public override IntPtr Invoke(string methodName, IntPtr javaArgs)
        {
            switch (methodName)
            {
                case nameof(onPermissionResult):
                    onPermissionResult(javaArgs);
                    return IntPtr.Zero;
                default:
                    return base.Invoke(methodName, javaArgs);
            }
        }

        private void onPermissionResult(IntPtr javaArgs)
        {
            var names = AndroidJNISafe.GetObjectArrayElement(javaArgs, 0);
            var grantResults = AndroidJNISafe.FromIntArray(AndroidJNISafe.GetObjectArrayElement(javaArgs, 1));
            for (int i = 0; i < grantResults.Length; ++i)
            {
                string permission = AndroidJNISafe.GetStringChars(AndroidJNISafe.GetObjectArrayElement(names, i));
                switch ((Result)grantResults[i])
                {
                    case Result.Dismissed:
                        if (PermissionRequestDismissed == null)
                            goto case Result.Denied;
                        PermissionRequestDismissed.Invoke(permission);
                        break;
                    case Result.Granted:
                        PermissionGranted?.Invoke(permission);
                        break;
                    case Result.DeniedDontAskAgain:
                        if (PermissionDeniedAndDontAskAgain == null)
                            goto case Result.Denied;
                        PermissionDeniedAndDontAskAgain.Invoke(permission);
                        break;
                    case Result.Denied:
                        PermissionDenied?.Invoke(permission);
                        break;
                }
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
