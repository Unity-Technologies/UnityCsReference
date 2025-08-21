// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// This enum defines the sequential stages involved in the execution workflow.
    /// Instances use these stages to process groups of nodes, while Scenarios leverage
    /// them to orchestrate and coordinate stage execution across multiple Instances.
    /// </summary>
    internal enum ExecutionStage
    {
        None,
        Prepare,
        Deploy,
        Run
    }
}
