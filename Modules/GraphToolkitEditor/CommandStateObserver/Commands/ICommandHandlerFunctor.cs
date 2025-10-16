// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor;

namespace Unity.GraphToolkit.CSO
{
    /// <summary>
    /// Interface to wrap a command handler, bind its parameters and invoke it.
    /// </summary>
    [UnityRestricted]
    internal interface ICommandHandlerFunctor
    {
        /// <summary>
        /// Returns the type of the command handled by this functor.
        /// </summary>
        Type CommandType { get; }

        /// <summary>
        /// Binds the parameters of the command handler.
        /// </summary>
        /// <param name="args">The parameters to bind.</param>
        /// <returns>The current functor, with the parameters bound.</returns>
        ICommandHandlerFunctor Bind(List<IStateComponent> args);

        /// <summary>
        /// Invokes the command handler.
        /// </summary>
        /// <param name="command">The command that triggered the invocation.</param>
        /// <param name="logHandler">Whether to log the invocation.</param>
        void Invoke(ICommand command, bool logHandler);
    }
}
