// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.DeviceSimulation
{
    internal enum DevicePackageStatus { Available, Unavailable, Outdated, Adding, Updating, Unknown, Error }

    internal static class DevicePackage
    {
        private static bool s_Initialized;
        private static DevicePackageStatus s_CurrentStatus = DevicePackageStatus.Unknown;

        private static ListRequest s_ListRequest;
        private static AddRequest s_AddRequest;

        private static Action<DevicePackageStatus> m_OnPackageStatus;
        public static event Action<DevicePackageStatus> OnPackageStatus
        {
            add
            {
                m_OnPackageStatus += value;

                if (!s_Initialized)
                {
                    s_Initialized = true;
                    PackageManager.Events.registeredPackages += OnPackageRegistration;
                }

                if (s_CurrentStatus != DevicePackageStatus.Unknown)
                {
                    m_OnPackageStatus?.Invoke(s_CurrentStatus);
                }
                else if (s_ListRequest == null && s_AddRequest == null)
                {
                    s_ListRequest = Client.List();
                    EditorApplication.update += HandleList;
                }
            }
            remove => m_OnPackageStatus -= value;
        }

        public static void Add()
        {
            s_AddRequest = Client.Add("com.unity.device-simulator.devices");
            EditorApplication.update += HandleAdd;

            SetStatus(DevicePackageStatus.Adding);
        }

        private static void OnPackageRegistration(PackageRegistrationEventArgs args)
        {
            if (args.removed.Any(package => package.name == "com.unity.device-simulator.devices"))
                SetStatus(DevicePackageStatus.Unavailable);

            var package = args.added.Concat(args.changedTo).FirstOrDefault(package => package.name == "com.unity.device-simulator.devices");
            if (package != null)
                SetStatus(GetDevicePackageStatus(package));
        }

        private static void HandleList()
        {
            if (!s_ListRequest.IsCompleted)
                return;

            EditorApplication.update -= HandleList;

            if (s_ListRequest.Status == StatusCode.Success)
            {
                var package = s_ListRequest.Result.FirstOrDefault(package => package.name == "com.unity.device-simulator.devices");
                SetStatus(GetDevicePackageStatus(package));
            }
            else if (s_ListRequest.Status == StatusCode.Failure)
            {
                SetStatus(DevicePackageStatus.Error);
            }

            s_ListRequest = null;
        }

        private static void HandleAdd()
        {
            if (!s_AddRequest.IsCompleted)
                return;

            EditorApplication.update -= HandleAdd;

            if (s_AddRequest.Status == StatusCode.Success)
                SetStatus(DevicePackageStatus.Available);
            else if (s_AddRequest.Status == StatusCode.Failure)
            {
                Debug.LogError("Failed installing Device Simulator Devices (com.unity.device-simulator.devices) package. Try installing it from the Package Manager window.");
                SetStatus(DevicePackageStatus.Error);
            }

            s_AddRequest = null;
        }

        private static void SetStatus(DevicePackageStatus newStatus)
        {
            s_CurrentStatus = newStatus;
            m_OnPackageStatus?.Invoke(s_CurrentStatus);
        }

        private static DevicePackageStatus GetDevicePackageStatus(PackageManager.PackageInfo packageInfo)
        {
            if (packageInfo == null)
                return DevicePackageStatus.Unavailable;
            return packageInfo.version == packageInfo.versions.latestCompatible ? DevicePackageStatus.Available : DevicePackageStatus.Outdated;
        }

    }
}
