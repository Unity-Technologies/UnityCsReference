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
    class AdbStopProcessNode : Node
    {
        IAdbService m_AdbService;

        [SerializeReference] private NodeInput<string> m_PackageName;
        [SerializeReference] private NodeInput<string> m_DeviceName;

        public NodeInput<string> PackageName => m_PackageName;
        public NodeInput<string> DeviceName => m_DeviceName;

        public IAdbService GetAdbService()
        {
            m_AdbService ??= AdbService.GetInstance();
            return m_AdbService;
        }

        public AdbStopProcessNode(string name, IAdbService adbService = null) : base(name)
        {
            m_AdbService = adbService;

            m_PackageName = new(this);
            m_DeviceName = new(this);
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var packageName = GetInput(PackageName);
            var deviceName = GetInput(DeviceName);

            GetAdbService().StopApk(packageName, deviceName);
            return Task.CompletedTask;
        }
    }
}
