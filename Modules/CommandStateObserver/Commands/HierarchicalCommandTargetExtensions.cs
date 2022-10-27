// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.CommandStateObserver
{
    /// <summary>
    /// Extension methods for <see cref="IHierarchicalCommandTarget"/>.
    /// </summary>
    static class HierarchicalCommandTargetExtensions
    {
        /// <summary>
        /// Dispatches a command to a command target and its ancestors.
        /// </summary>
        /// <param name="self">The command target to dispatch the command to.</param>
        /// <param name="command">The command to dispatch.</param>
        /// <param name="diagnosticsFlags">Diagnostic flags for the dispatch process.</param>
        public static void DispatchToHierarchy(this IHierarchicalCommandTarget self, ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
        {
            while (self != null)
            {
                self.DispatchToSelf(command, diagnosticsFlags);
                self = self.ParentTarget;
            }
        }
    }
}
