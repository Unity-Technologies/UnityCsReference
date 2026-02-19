// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    interface IAdbService
    {
        void InstallApk(string buildPath, string deviceName);
        Task<int> StartApk(string packageName, string activityName, string deviceName);
        void StopApk(string packageName, string deviceName);
        Task ProcessShadowTask(string deviceName, int processId, CancellationToken cancellationToken);
        AdbLogcatBase CreateLogcat(string deviceName);
    }

    class AdbService : IAdbService
    {
        static AdbService s_Instance;
        public static AdbService GetInstance()
        {
            if (s_Instance == null)
            {
                s_Instance = new AdbService();
            }
            return s_Instance;
        }

        AdbBridgeHelper.ADB m_AdbInstance;
        readonly Dictionary<string, Task> m_ActiveProcessShadows = new();

        AdbService()
        {
            m_AdbInstance = AdbBridgeHelper.ADB.GetInstance();
            m_ActiveProcessShadows = new();
        }

        public void InstallApk(string buildPath, string deviceName)
        {
            m_AdbInstance.Run(new[] { "-s", deviceName, "install", "-r", buildPath }, "error installing to device");
        }

        public async Task<int> StartApk(string packageName, string activityName, string deviceName)
        {
            const int k_MaxRetries = 5;
            const int k_RetryDelayMS = 500;

            m_AdbInstance.Run(new[] { "-s", deviceName, "shell", "am", "start", "-n", packageName + "/" + activityName, "-e", "unity", "-systemallocator" }, "Error running apk");

            for (int i = 0; i < k_MaxRetries; i++)
            {
                try
                {
                    var result = m_AdbInstance.Run(new[] { "-s", deviceName, "shell", "pidof", packageName }, "Error getting PID");
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

        public void StopApk(string packageName, string deviceName)
        {
            m_AdbInstance.Run(new[] { "-s", deviceName, "shell", "pm ", "clear", packageName }, "Error killing apk process");
        }

        public Task ProcessShadowTask(string deviceName, int processId, CancellationToken cancellationToken)
        {
            if (m_ActiveProcessShadows.TryGetValue(deviceName, out var shadowTask))
            {
                return shadowTask;
            }

            shadowTask = Task.Run(async () =>
            {
                const int k_PingIntervalMS = 1_000;
                Thread.CurrentThread.Name = $"Android Process ({processId}) Monitor";
                while (GetAndroidProcessRunning(deviceName, processId)
                    && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(k_PingIntervalMS);
                }
                shadowTask = null;
            });

            m_ActiveProcessShadows[deviceName] = shadowTask;
            return shadowTask;
        }

        static bool GetAndroidProcessRunning(string deviceName, int pid)
        {
            try
            {
                var result = int.Parse(GetInstance().m_AdbInstance.Run(new[] { "-s", deviceName, "shell", $"[ -d /proc/{pid} ] && echo '1' || echo '0'" }, "Error checking process"));
                return result == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public AdbLogcatBase CreateLogcat(string deviceName)
        {
            // return new AdbLogcat(m_AdbInstance, "threadtime", deviceName, Debug.Log);
            return new AdbLogcat(m_AdbInstance, "raw", deviceName, Debug.Log);
        }
    }
}
