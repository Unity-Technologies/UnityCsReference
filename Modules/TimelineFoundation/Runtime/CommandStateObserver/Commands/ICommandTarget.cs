// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Timeline.Foundation.CSO
{
    /// <summary>
    /// Interface that defines the target of a command.
    /// </summary>
    interface ICommandTarget
    {
        /// <summary>
        /// Dispatches a command to this target and its parent, recursively.
        /// </summary>
        /// <param name="command">The command to dispatch.</param>
        /// <param name="diagnosticsFlags">Diagnostic flags for the dispatch process.</param>
        void Dispatch(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None);
    }
}
