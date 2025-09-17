// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;

namespace Unity.Multiplayer.PlayMode.Editor
{
    abstract class PlayModeController
    {
        protected internal virtual void SetupExecutionGraph(ExecutionGraph graph) { }

        protected internal virtual Task<Scenario.ValidationResult> ValidateForRunningAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(new Scenario.ValidationResult(true, string.Empty));
        }
    }
}
