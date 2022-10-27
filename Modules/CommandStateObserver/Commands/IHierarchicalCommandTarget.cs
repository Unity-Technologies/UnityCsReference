// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// A command target that also send the command to its parent.
    /// </summary>
    interface IHierarchicalCommandTarget : ICommandTarget
    {
        /// <summary>
        /// The parent target.
        /// </summary>
        IHierarchicalCommandTarget ParentTarget { get; }

        /// <summary>
        /// Dispatches a command to this target, without sending it to its parent.
        /// </summary>
        /// <param name="command">The command to dispatch.</param>
        /// <param name="diagnosticsFlags">Diagnostic flags for the dispatch process.</param>
        void DispatchToSelf(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None);
    }
}
