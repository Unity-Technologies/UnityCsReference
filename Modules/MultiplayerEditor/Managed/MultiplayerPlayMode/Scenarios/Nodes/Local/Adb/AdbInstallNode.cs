// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: HeadlessRuntime not yet converted
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class AdbInstallNode : ExecutionNode
    {
        IAdbService m_AdbService;

        [SerializeReference] private NodeInput<string> m_ApkPath;
        [SerializeReference] private NodeInput<string> m_DeviceName;

        public NodeInput<string> ApkPath => m_ApkPath;
        public NodeInput<string> DeviceName => m_DeviceName;

        public IAdbService GetAdbService()
        {
            m_AdbService ??= AdbService.GetInstance();
            return m_AdbService;
        }

        public AdbInstallNode()
        {
            m_ApkPath = new(this);
            m_DeviceName = new(this);
        }

        public AdbInstallNode(IAdbService adbService) : this()
        {
            m_AdbService = adbService;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var apkPath = GetInput(ApkPath);
            var deviceName = GetInput(DeviceName);
            var service = GetAdbService();

            var installTask = Task.Run(() => service.InstallApk(apkPath, deviceName));
            await installTask;
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
