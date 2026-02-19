// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Multiplayer.PlayMode.Editor
{
    // TODO MTT-10157 Update Execution State to remove Active, Aborted and remove redundant states
    // when integrating the new Execution Node system.

    /// <summary>
    /// Represents the state of an execution in a graph
    /// </summary>
    enum ExecutionState
    {
        /// <summary>
        /// Execution is invalid and won't do anything until the next cycle. Will transition to Idle when the next cycle starts
        /// </summary>
        Invalid = default,

        /// <summary>
        /// Execution has failed and won't do anything until the next cycle. Will transition to Idle when the next cycle starts
        /// </summary>
        Failed,

        /// <summary>
        /// Initial state; Will transition to Running or Error when the Node
        /// </summary>
        Idle,

        /// <summary>
        /// Execution is running; building, uploading something, starting a process, etc. Will transition to Completed or Error when done
        /// </summary>
        Running,

        /// <summary>
        /// Execution has completed its tasks successfully and won't do anything until the next cycle. Will transition to Idle when the next cycle starts
        /// </summary>
        Completed,

        /// <summary>
        /// Execution has been aborted and won't do anything until the next cycle. Will transition to Idle when the next cycle starts
        /// </summary>
        Aborted,
    }
}
