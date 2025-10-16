// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    abstract class PlayModeController
    {
        protected internal virtual void SetupExecutionGraph(ExecutionGraph graph) { }

        protected internal virtual Task<Scenario.ValidationResult> ValidateForRunningAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new Scenario.ValidationResult(true, string.Empty));
        }

        protected internal virtual VisualElement CreateControllerUI() => null;
    }
}
