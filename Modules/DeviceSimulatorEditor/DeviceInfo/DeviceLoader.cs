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
        public static string[] s_BuiltInDevices =
        {
            "Apple iPad (5th generation).device",
            "Apple iPad (7th gen).device",
            "Apple iPad 6th Gen.device",
            "Apple iPad Air (4th generation).device",
            "Apple iPad Air 2.device",
            "Apple iPad Air.device",
            "Apple iPad Mini 2.device",
            "Apple iPad Mini 4.device",
            "Apple iPad Pro 10.15.device",
            "Apple iPad Pro 11.device",
            "Apple iPad Pro 12.9 (2018).device",
            "Apple iPhone 6.device",
            "Apple iPhone 6S.device",
            "Apple iPhone 7 Plus.device",
            "Apple iPhone 7.device",
            "Apple iPhone 8 Plus.device",
            "Apple iPhone 8.device",
            "Apple iPhone 11.device",
            "Apple iPhone 12 mini.device",
            "Apple iPhone 12 Pro Max.device",
            "Apple iPhone 12 Pro.device",
            "Apple iPhone 12.device",
            "Apple iPhone SE (2nd generation).device",
            "Apple iPhone X.device",
            "Apple iPhone XR.device",
            "Apple iPhone XS Max.device",
            "Apple iPhone XS.device",
            "Apple Ipod Touch 6th Gen.device",
            "Apple iPod Touch 7th Gen.device",
            "Asus ROG Phone.device",
            "Google Nexus 4.device",
            "Google Pixel 2 XL.device",
            "Google Pixel 2.device",
            "Google Pixel 3.device",
            "Google Pixel 4.device",
            "Google Pixel 5.device",
            "Google Pixel X.device",
            "HTC 10.device",
            "HTC One M9.device",
            "Huawei P9.device",
            "Huawei P40 Pro.device",
            "Lenovo Phab2 Pro.device",
            "LG G4.device",
            "LG Nexus 5.device",
            "LGE LG G3.device",
            "Motorola Moto E.device",
            "Motorola Moto G7 Power.device",
            "Motorola Nexus 6.device",
            "Nvidia Shield Tablet.device",
            "OnePlus 6T.device",
            "OnePlus 7 Pro.device",
            "Razer Phone.device",
            "Samsung Galaxy J7 (2017).device",
            "Samsung Galaxy Note8.device",
            "Samsung Galaxy Note9.device",
            "Samsung Galaxy Note10.device",
            "Samsung Galaxy Note10+ 5G.device",
            "Samsung Galaxy Note20 Ultra 5G.device",
            "Samsung Galaxy S5 Mini.device",
            "Samsung Galaxy S5 Neo.device",
            "Samsung Galaxy S7.device",
            "Samsung Galaxy S8.device",
            "Samsung Galaxy S9.device",
            "Samsung Galaxy S10 5G.device",
            "Samsung Galaxy S10+.device",
            "Samsung Galaxy S10e.device",
            "Samsung Galaxy Z Fold2 5G.device",
            "Sony Xperia XZ Premium.device",
            "Sony Xperia Z2 Tablet.device",
            "vivo NEX 3 5G.device",
            "Xiaomi Mi 4i.device",
            "Xiaomi Mi 5.device",
            "Xiaomi Mi A3.device",
            "Xiaomi MI Max.device",
            "Xiaomi MI Note Pro.device",
            "Xiaomi Redmi 4.device",
            "Xiaomi Redmi 6 Pro.device",
            "Xiaomi Redmi Note 3.device",
            "Xiaomi Redmi Note7.device"
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
                if (deviceAsset != null && (deviceAsset.parseErrors == null || deviceAsset.parseErrors.Length == 0))
                {
                    deviceAsset.directory = assetDirectory;
                    deviceAsset.editorResource = true;
                    devices.Add(deviceAsset);
                }
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
