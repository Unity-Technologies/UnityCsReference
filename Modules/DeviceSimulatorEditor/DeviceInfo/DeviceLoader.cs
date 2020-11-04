// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.DeviceSimulation
{
    internal static class DeviceLoader
    {
        private static string[] s_BuiltInDevices =
        {
            "Apple iPhone XR.device",
            "Samsung Galaxy S10e.device"
        };

        public static DeviceInfo[] LoadDevices()
        {
            var devices = new List<DeviceInfo>();

            var deviceInfoGUIDs = AssetDatabase.FindAssets("t:DeviceInfoAsset");
            foreach (var guid in deviceInfoGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var deviceAsset = AssetDatabase.LoadAssetAtPath<DeviceInfoAsset>(assetPath);
                if (deviceAsset.parseErrors == null || deviceAsset.parseErrors.Length == 0)
                {
                    deviceAsset.deviceInfo.directory = Path.GetDirectoryName(assetPath);
                    devices.Add(deviceAsset.deviceInfo);
                }
            }

            foreach (var assetName in s_BuiltInDevices)
            {
                var assetDirectory = "DeviceSimulator/DeviceAssets";
                var deviceAsset = EditorGUIUtility.Load(Path.Combine(assetDirectory, assetName)) as DeviceInfoAsset;
                if (deviceAsset != null && (deviceAsset.parseErrors == null || deviceAsset.parseErrors.Length == 0))
                {
                    deviceAsset.deviceInfo.directory = assetDirectory;
                    deviceAsset.deviceInfo.editorResource = true;
                    devices.Add(deviceAsset.deviceInfo);
                }
            }

            devices.Sort((x, y) => string.CompareOrdinal(x.friendlyName, y.friendlyName));
            return devices.ToArray();
        }

        public static bool LoadOverlay(DeviceInfo deviceInfo, int screenIndex)
        {
            var screen = deviceInfo.screens[screenIndex];
            var path = screen.presentation.overlayPath;

            if (string.IsNullOrEmpty(path))
                return false;

            if (deviceInfo.editorResource)
            {
                var filePath = Path.Combine(deviceInfo.directory, screen.presentation.overlayPath);
                var overlay = EditorGUIUtility.Load(filePath) as Texture;
                Debug.Assert(overlay != null, $"Failed to load built-in device {deviceInfo} overlay");
                screen.presentation.overlay = overlay;
            }
            else
            {
                if (!path.StartsWith("Assets/") && !path.StartsWith("Packages/"))
                    path = Path.Combine(deviceInfo.directory, path);
                screen.presentation.overlay = AssetDatabase.LoadAssetAtPath<Texture>(path);
            }

            return screen.presentation.overlay != null;
        }

        public static void UnloadOverlays(DeviceInfo deviceInfo)
        {
            foreach (var screen in deviceInfo.screens)
            {
                screen.presentation.overlay = null;
            }
        }
    }
}
