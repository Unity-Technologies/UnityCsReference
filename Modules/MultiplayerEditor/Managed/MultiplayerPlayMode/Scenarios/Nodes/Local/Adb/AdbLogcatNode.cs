// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class AdbLogcatNode : ExecutionNode
    {
        const int k_ProcessCheckIntervalMS = 120;

        IAdbService m_AdbService;

        [SerializeReference] NodeInput<string> m_LogPath;
        [SerializeReference] NodeInput<string> m_DeviceName;
        [SerializeReference] NodeInput<int> m_DeviceProcessId;
        [SerializeReference] NodeOutput<int> m_ProcessId;

        public NodeInput<string> LogPath => m_LogPath;
        public NodeInput<string> DeviceName => m_DeviceName;
        public NodeInput<int> DeviceProcessId => m_DeviceProcessId;
        public NodeOutput<int> ProcessId => m_ProcessId;

        public IAdbService GetAdbService()
        {
            m_AdbService ??= AdbService.GetInstance();
            return m_AdbService;
        }

        public AdbLogcatNode()
        {
            m_LogPath = new(this);
            m_DeviceName = new(this);
            m_DeviceProcessId = new(this);
            m_ProcessId = new(this);
        }

        public AdbLogcatNode(IAdbService adbService) : this()
        {
            m_AdbService = adbService;
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var logPath = GetInput(LogPath);
            var deviceName = GetInput(DeviceName);
            var logFolder = Path.GetDirectoryName(logPath);

            if (!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }

            var logcat = GetAdbService().CreateLogcat(deviceName);
            logcat.Start(GetInput(DeviceProcessId), logPath);
            SetOutput(ProcessId, logcat.GetLogcatProcessId());

            return Task.CompletedTask;
        }
    }
}
