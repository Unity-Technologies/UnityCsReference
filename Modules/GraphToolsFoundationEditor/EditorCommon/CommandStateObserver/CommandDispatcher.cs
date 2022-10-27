// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Command dispatcher class for graph tools.
    /// </summary>
    class CommandDispatcher : Dispatcher
    {
        internal string LastDispatchedCommandName_Internal { get; private set; }

        /// <summary>
        /// Diagnostic flags to add to every <see cref="Dispatch"/> call.
        /// </summary>
        public Diagnostics DiagnosticFlags { get; set; }

        /// <inheritdoc />
        protected override void PreDispatchCommand(ICommand command)
        {
            base.PreDispatchCommand(command);
            LastDispatchedCommandName_Internal = command.GetType().Name;
        }

        /// <inheritdoc />
        public override void Dispatch(ICommand command, Diagnostics diagnosticFlags = Diagnostics.None)
        {
            base.Dispatch(command, diagnosticFlags | DiagnosticFlags);
        }
    }
}
