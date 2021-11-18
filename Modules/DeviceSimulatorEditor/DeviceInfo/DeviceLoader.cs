// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.DeviceSimulation
{
    internal static class DeviceLoader
    {
        public static string[] s_BuiltInDevices =
        {
            "Apple iPad Mini 4.device",
            "Apple iPad Pro 12.9 (2018).device",
            "Apple iPhone 12 mini.device",
            "Apple iPhone 12 Pro Max.device",
            "Apple iPhone 12.device",
            "Apple iPhone SE (2nd generation).device",
            "Google Pixel 5.device",
            "Huawei P40 Pro.device",
            "Samsung Galaxy Note20 Ultra 5G.device",
            "Samsung Galaxy S10e.device",
            "Samsung Galaxy Z Fold2 5G.device"
        };

        public static DeviceInfoAsset[] LoadDevices()
        {
            var devices = new List<DeviceInfoAsset>();

            var deviceInfoGUIDs = AssetDatabase.FindAssets("t:DeviceInfoAsset");
            foreach (var guid in deviceInfoGUIDs)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var deviceAsset = AssetDatabase.LoadAssetAtPath<DeviceInfoAsset>(assetPath);
                if (deviceAsset.parseErrors == null || deviceAsset.parseErrors.Length == 0)
                {
                    deviceAsset.directory = Path.GetDirectoryName(assetPath);
                    devices.Add(deviceAsset);
                }
            }

            foreach (var assetName in s_BuiltInDevices)
            {
                var assetDirectory = "DeviceSimulator/DeviceAssets";
                var deviceAsset = EditorGUIUtility.Load(Path.Combine(assetDirectory, assetName)) as DeviceInfoAsset;

                // Devices that are not built-in will hide built-in devices with the same name. This way devices from packages can have
                // updated or duplicate versions of the same device without causing duplicate devices to appear.
                if (deviceAsset == null || deviceAsset.parseErrors != null && deviceAsset.parseErrors.Length != 0 || devices.Any(device => device.deviceInfo.friendlyName == deviceAsset.deviceInfo.friendlyName))
                    continue;

                deviceAsset.directory = assetDirectory;
                deviceAsset.editorResource = true;
                devices.Add(deviceAsset);
            }

            devices.Sort((x, y) => string.CompareOrdinal(x.deviceInfo.friendlyName, y.deviceInfo.friendlyName));
            return devices.ToArray();
        }

        public static Texture LoadOverlay(DeviceInfoAsset device, int screenIndex)
        {
            var screen = device.deviceInfo.screens[screenIndex];
            var path = screen.presentation.overlayPath;

            if (string.IsNullOrEmpty(path))
                return null;

            if (device.editorResource)
            {
                var filePath = Path.Combine(device.directory, screen.presentation.overlayPath);
                var overlay = EditorGUIUtility.Load(filePath) as Texture;
                Debug.Assert(overlay != null, $"Failed to load built-in device {device.deviceInfo} overlay");
                return overlay;
            }

            path = path.Replace("\\", "/");
            if (!path.StartsWith("Assets/") && !path.StartsWith("Packages/"))
                path = Path.Combine(device.directory, path);

            // Custom textures need to be readable for us to accurately map touches in cutouts
            // but we can full back to the .device cutouts. We need the .meta file to be unlocked in VCS.
            var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (textureImporter != null && !textureImporter.isReadable)
            {
                if (AssetDatabase.MakeEditable(path + ".meta"))
                {
                    textureImporter.isReadable = true;
                    AssetDatabase.ImportAsset(path);
                    AssetDatabase.Refresh();
                }
                else
                {
                    Debug.LogWarning(
                        "Read/Write not enabled on simulator texture\nRead/Write is required to simulate touch over the device bezel and cutouts accurately.");
                }
            }

            return AssetDatabase.LoadAssetAtPath<Texture>(path);
        }
    }
}
