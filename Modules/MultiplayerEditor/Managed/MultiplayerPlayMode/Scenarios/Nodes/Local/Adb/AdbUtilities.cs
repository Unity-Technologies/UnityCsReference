// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor;
using UnityEditor.Build;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Application = UnityEngine.Application;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal class AdbUtilities
    {
        internal static string m_PackageName = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        internal static string m_ActivityName = GetActivityName();

        public static async Task<int> StartApk(string buildPath, string deviceName)
        {
            const int k_MaxRetries = 5;
            const int k_RetryDelayMS = 500;

            var adb = AdbBridgeHelper.ADB.GetInstance();
            adb.Run(new[] { "-s", deviceName, "install", buildPath }, "error installing to device");
            adb.Run(new[] { "-s", deviceName, "shell", "am", "start", "-n", m_PackageName + "/" + m_ActivityName, "-e", "unity", "-systemallocator" }, "Error running apk");

            for (int i = 0; i < k_MaxRetries; i++)
            {
                try
                {
                    var result = adb.Run(new[] { "-s", deviceName, "shell", "pidof", m_PackageName }, "Error getting PID");
                    if (int.TryParse(result, out var pid))
                    {
                        return pid;
                    }
                }
                catch (Exception)
                {
                    await Task.Delay(k_RetryDelayMS);
                }
            }

            throw new Exception("Failed to get PID of the running process in the device.");
        }

        public static void StopApk(string deviceName)
        {
            AdbBridgeHelper.ADB.GetInstance().Run(new []{"-s", deviceName, "shell", "pm ", "clear",m_PackageName},"Error killing apk process");
        }

        public static bool GetAndroidProcessRunning(string deviceName, int pid)
        {
            try
            {
                var result = int.Parse(AdbBridgeHelper.ADB.GetInstance().Run(new[] { "-s", deviceName, "shell", $"[ -d /proc/{pid} ] && echo '1' || echo '0'" }, "Error getting device name"));
                return result == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static AdbBridgeHelper.ADB TryGetADBInstance()
        {
            try
            {
                return AdbBridgeHelper.ADB.GetInstance();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool IsAdbAvailable()
        {
            return TryGetADBInstance() != null;
        }

        public static string GetADBDevices()
        {
            var instance = TryGetADBInstance();
            if (instance == null)
                return string.Empty;

            return instance.Run(["devices"], "No devices");
        }

        public static List<string> GetADBDevicesDetailed()
        {
            if (AdbBridgeHelper.AndroidExtensionsInstalled)
            {
                var instance = TryGetADBInstance();
                if (instance == null)
                    return new List<string>();

                return FormatDeviceInfo(new List<string>(instance.Run(new[] { "devices", "-l" }, "No devices").Split("\n")));
            }

            return new List<string>();
        }

        public static List<string> FormatDeviceInfo(List<string> deviceList)
        {
            string pattern = @"(\S+)\s+device\s.*model:(\S+)";
            Regex regex = new Regex(pattern);

            List<string> formattedDeviceList = new List<string>();
            foreach (string device in deviceList)
            {
                Match match = regex.Match(device);
                if (match.Success)
                {
                    string deviceId = match.Groups[1].Value;
                    string model = match.Groups[2].Value;
                    model = model.Replace("_", " ");
                    formattedDeviceList.Add($"{model} ({deviceId})");
                }
            }

            return formattedDeviceList;
        }



        internal static string GetActivityName()
        {
            if (GetCustomActivityName() != String.Empty)
                return GetCustomActivityName();

            return GetDefaultActivityName();
        }

        internal static string GetCustomActivityName()
        {
            string manifestPath = Path.Combine(Application.dataPath, "Plugins/Android/AndroidManifest.xml");
            if (File.Exists(manifestPath))
            {
                string manifestContent = File.ReadAllText(manifestPath);

                if (manifestContent.Contains("android:name"))
                {
                    int startIndex = manifestContent.IndexOf("android:name") + 14;
                    int endIndex = manifestContent.IndexOf('"', startIndex);
                    string activityName = manifestContent.Substring(startIndex, endIndex - startIndex);
                    return activityName;
                }
            }
            return String.Empty;
        }

        internal static string GetDefaultActivityName()
        {
            var applicationEntry = PlayerSettings.Android.applicationEntry;
            if (applicationEntry == AndroidApplicationEntry.GameActivity)
                return "com.unity3d.player.UnityPlayerGameActivity";

            return "com.unity3d.player.UnityPlayerActivity";
        }

    }
}
