// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace UnityEditor.DeviceSimulation
{
    [ScriptedImporter(1, "device")]
    class DeviceInfoImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            SimulatorWindow.MarkAllDeviceListsDirty();

            var asset = ScriptableObject.CreateInstance<DeviceInfoAsset>();

            var deviceJson = File.ReadAllText(ctx.assetPath);
            asset.deviceInfo = ParseDeviceInfo(deviceJson, out var parseErrors);

            if (asset.deviceInfo == null)
            {
                asset.parseErrors = parseErrors;
            }
            else
            {
                AddOptionalFields(asset.deviceInfo);

                // Saving asset path in order to find overlay relatively to it
                asset.deviceInfo.directory = Path.GetDirectoryName(ctx.assetPath);
                ctx.DependsOnSourceAsset(ctx.assetPath);
            }

            ctx.AddObjectToAsset("main obj", asset);
            ctx.SetMainObject(asset);
        }

        internal static DeviceInfo ParseDeviceInfo(string deviceJson, out string[] errors)
        {
            var errorList = new List<string>();

            DeviceInfo deviceInfo;
            try
            {
                deviceInfo = JsonUtility.FromJson<DeviceInfo>(deviceJson);
            }
            catch (Exception e)
            {
                errorList.Add(e.Message);
                errors = errorList.ToArray();
                return null;
            }

            if (string.IsNullOrEmpty(deviceInfo.friendlyName))
                errorList.Add("Mandatory field [friendlyName] is omitted or empty.");
            if (deviceInfo.version != 1)
                errorList.Add("Mandatory field [version] is omitted or set to an unknown value. Newest device file version is 1.");
            if (deviceInfo.screens == null || deviceInfo.screens.Length == 0)
                errorList.Add("No screen information found, mandatory field [screens] must contain at least one screen.");
            else
            {
                for (var i = 0; i < deviceInfo.screens.Length; i++)
                {
                    var screen = deviceInfo.screens[i];
                    if (screen.width < 4 || screen.width > 8192)
                    {
                        errorList.Add($"screens[{i}] -> width field is omitted or set to an incorrect value. Screen width must be larger than 4 and smaller than 8192.");
                    }

                    if (screen.height < 4 || screen.height > 8192)
                    {
                        errorList.Add($"screens[{i}] -> height field is omitted or set to an incorrect value. Screen height must be larger than 4 and smaller than 8192.");
                    }

                    if (screen.dpi < 0.0001f || screen.dpi > 10000f)
                    {
                        errorList.Add($"screens[{i}] -> dpi field is omitted or set to an incorrect value. Screen dpi must be larger than 0 and smaller than 10000.");
                    }
                }
            }
            if (deviceInfo.systemInfo == null || string.IsNullOrEmpty(deviceInfo.systemInfo.operatingSystem))
                errorList.Add("Mandatory field [systemInfo -> operatingSystem] is omitted or empty.");
            else
            {
                var os = deviceInfo.systemInfo.operatingSystem.ToLower();
                if (!os.Contains("ios") && !os.Contains("android"))
                    errorList.Add("[systemInfo -> operatingSystem] field does not contain the name of the operating system. Currently supported names are iOS or Android.");
            }

            errors = errorList.ToArray();
            return errors.Length == 0 ? deviceInfo : null;
        }

        internal static void AddOptionalFields(DeviceInfo deviceInfo)
        {
            foreach (var screen in deviceInfo.screens)
            {
                if (screen.orientations == null || screen.orientations.Length == 0)
                {
                    screen.orientations = new[]
                    {
                        new OrientationData {orientation = ScreenOrientation.Portrait},
                        new OrientationData {orientation = ScreenOrientation.PortraitUpsideDown},
                        new OrientationData {orientation = ScreenOrientation.LandscapeLeft},
                        new OrientationData {orientation = ScreenOrientation.LandscapeRight}
                    };
                }
                foreach (var orientation in screen.orientations)
                {
                    if (orientation.safeArea == Rect.zero)
                        orientation.safeArea = SimulatorUtilities.IsLandscape(orientation.orientation) ? new Rect(0, 0, screen.height, screen.width) : new Rect(0, 0, screen.width, screen.height);
                }
            }
        }
    }
}
