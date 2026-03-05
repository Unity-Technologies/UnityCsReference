// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

        public AdbInstallNode(string name, IAdbService adbService = null) : base(name)
        {
            m_AdbService = adbService;

            m_ApkPath = new(this);
            m_DeviceName = new(this);
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
