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
    class AdbStartProcessNode : Node
    {
        IAdbService m_AdbService;

        [SerializeReference] private NodeInput<string> m_DeviceName;
        [SerializeReference] private NodeInput<string> m_PackageName;
        [SerializeReference] private NodeInput<string> m_ActivityName;
        [SerializeReference] private NodeOutput<int> m_ProcessId;

        public NodeInput<string> DeviceName => m_DeviceName;
        public NodeInput<string> PackageName => m_PackageName;
        public NodeInput<string> ActivityName => m_ActivityName;
        public NodeOutput<int> ProcessId => m_ProcessId;

        public IAdbService GetAdbService()
        {
            m_AdbService ??= AdbService.GetInstance();
            return m_AdbService;
        }

        public AdbStartProcessNode(string name, IAdbService adbService = null) : base(name)
        {
            m_AdbService = adbService;

            m_DeviceName = new(this);
            m_PackageName = new(this);
            m_ActivityName = new(this);
            m_ProcessId = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var deviceName = GetInput(DeviceName);
            var packageName = GetInput(PackageName);
            var activityName = GetInput(ActivityName);

            var processId = await GetAdbService().StartApk(packageName, activityName, deviceName);
            SetOutput(ProcessId, processId);
        }
    }
}
