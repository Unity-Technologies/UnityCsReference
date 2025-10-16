// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Command dispatcher class for graph tools.
    /// </summary>
    [UnityRestricted]
    internal class CommandDispatcher : Dispatcher
    {
        internal string LastDispatchedCommandName { get; private set; }

        /// <summary>
        /// Diagnostic flags to add to every <see cref="Dispatch"/> call.
        /// </summary>
        public Diagnostics DiagnosticFlags { get; set; }

        /// <inheritdoc />
        protected override void PreDispatchCommand(ICommand command)
        {
            base.PreDispatchCommand(command);
            LastDispatchedCommandName = command.GetType().Name;
        }

        /// <inheritdoc />
        public override void Dispatch(ICommand command, Diagnostics diagnosticFlags = Diagnostics.None)
        {
            base.Dispatch(command, diagnosticFlags | DiagnosticFlags);
        }
    }
}
