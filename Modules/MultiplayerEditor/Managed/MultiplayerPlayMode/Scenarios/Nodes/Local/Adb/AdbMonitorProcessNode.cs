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
    class AdbMonitorProcessNode : Node
    {
        const int k_ProcessCheckIntervalMS = 120;

        IAdbService m_AdbService;

        [SerializeReference] NodeInput<int> m_ProcessId;
        [SerializeReference] NodeInput<string> m_DeviceName;
        [SerializeReference] NodeInput<string> m_PackageName;

        public NodeInput<int> ProcessId => m_ProcessId;
        public NodeInput<string> DeviceName => m_DeviceName;
        public NodeInput<string> PackageName => m_PackageName;

        public IAdbService GetAdbService()
        {
            m_AdbService ??= AdbService.GetInstance();
            return m_AdbService;
        }

        public AdbMonitorProcessNode(string name, IAdbService adbService = null) : base(name)
        {
            m_AdbService = adbService;

            m_ProcessId = new(this);
            m_DeviceName = new(this);
            m_PackageName = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var processId = GetInput(ProcessId);
            var deviceName = GetInput(DeviceName);
            var packageName = GetInput(PackageName);

            var apkShadowTask = GetAdbService().ProcessShadowTask(deviceName, processId, cancellationToken);

            try
            {
                while (!apkShadowTask.IsCompleted && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(k_ProcessCheckIntervalMS, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // A cancellation at this point means it was requested by the user,
                // which means the node actually completed properly. Setting progress to 1.0 will prevent its state to be set to Aborted.
                SetProgress(1f);
            }
            finally
            {
                GetAdbService().StopApk(packageName, deviceName);
                await apkShadowTask;
            }
        }
    }
}
