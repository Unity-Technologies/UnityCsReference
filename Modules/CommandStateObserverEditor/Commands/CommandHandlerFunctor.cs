// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.CSO;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.CSO.Editor
{
    /// <summary>
    /// Class to wrap a <see cref="CommandHandler{TCommand}"/> and invoke it.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
    internal class CommandHandlerFunctor<TCommand> : CSO.CommandHandlerFunctor<TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandHandlerFunctor{TCommand}"/> class.
        /// </summary>
        /// <param name="callback">The delegate to wrap. If null, will try to find `TCommand.DefaultCommandHandler`.</param>
        public CommandHandlerFunctor(CommandHandler<TCommand> callback = null) : base(callback)
        {
            if (m_Callback == null)
                m_Callback = CommandHandlerUtilities.GetDefaultCommandHandler<CommandHandler<TCommand>, TCommand>();
        }
    }

    /// <summary>
    /// Class to wrap a <see cref="CommandHandler{TParam, TCommand}"/>, bind its parameter and invoke it.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
    internal class CommandHandlerFunctor<TParam, TCommand> : CSO.CommandHandlerFunctor<TParam, TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandHandlerFunctor{TParam, TCommand}"/> class.
        /// </summary>
        /// <param name="callback">The delegate to wrap. If null, will try to find `TCommand.DefaultCommandHandler`.</param>
        public CommandHandlerFunctor(CommandHandler<TParam, TCommand> callback = null) : base(callback)
        {
            if (m_Callback == null)
                m_Callback = CommandHandlerUtilities.GetDefaultCommandHandler<CommandHandler<TParam, TCommand>, TCommand>();
        }
    }

    /// <summary>
    /// Class to wrap a <see cref="CommandHandler{TParam1, TParam2, TCommand}"/>, bind its parameters and invoke it.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
    internal class CommandHandlerFunctor<TParam1, TParam2, TCommand> : CSO.CommandHandlerFunctor<TParam1, TParam2, TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandHandlerFunctor{TParam1, TParam2, TCommand}"/> class.
        /// </summary>
        /// <param name="callback">The delegate to wrap. If null, will try to find `TCommand.DefaultCommandHandler`.</param>
        public CommandHandlerFunctor(CommandHandler<TParam1, TParam2, TCommand> callback = null) : base(callback)
        {
            if (m_Callback == null)
                m_Callback = CommandHandlerUtilities.GetDefaultCommandHandler<CommandHandler<TParam1, TParam2, TCommand>, TCommand>();
        }
    }

    /// <summary>
    /// Class to wrap a <see cref="CommandHandler{TParam1, TParam2, TParam3, TCommand}"/>, bind its parameters and invoke it.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
    internal class CommandHandlerFunctor<TParam1, TParam2, TParam3, TCommand> : CSO.CommandHandlerFunctor<TParam1, TParam2, TParam3, TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandHandlerFunctor{TParam1, TParam2, TParam3, TCommand}"/> class.
        /// </summary>
        /// <param name="callback">The delegate to wrap. If null, will try to find `TCommand.DefaultCommandHandler`.</param>
        public CommandHandlerFunctor(CommandHandler<TParam1, TParam2, TParam3, TCommand> callback = null) : base(callback)
        {
            if (m_Callback == null)
                m_Callback = CommandHandlerUtilities.GetDefaultCommandHandler<CommandHandler<TParam1, TParam2, TParam3, TCommand>, TCommand>();
        }
    }

    /// <summary>
    /// Class to wrap a <see cref="CommandHandler{TParam1, TParam2, TParam3, TParam4, TCommand}"/>, bind its parameters and invoke it.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
    internal class CommandHandlerFunctor<TParam1, TParam2, TParam3, TParam4, TCommand> : CSO.CommandHandlerFunctor<TParam1, TParam2, TParam3, TParam4, TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandHandlerFunctor{TParam1, TParam2, TParam3, TParam4, TCommand}"/> class.
        /// </summary>
        /// <param name="callback">The delegate to wrap. If null, will try to find `TCommand.DefaultCommandHandler`.</param>
        public CommandHandlerFunctor(CommandHandler<TParam1, TParam2, TParam3, TParam4, TCommand> callback = null) : base(callback)
        {
            if (m_Callback == null)
                m_Callback = CommandHandlerUtilities.GetDefaultCommandHandler<CommandHandler<TParam1, TParam2, TParam3, TParam4, TCommand>, TCommand>();
        }
    }

    /// <summary>
    /// Class to wrap a <see cref="CommandHandler{TParam1, TParam2, TParam3, TParam4, TParam5, TCommand}"/>, bind its parameters and invoke it.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
    internal class CommandHandlerFunctor<TParam1, TParam2, TParam3, TParam4, TParam5, TCommand> : CSO.CommandHandlerFunctor<TParam1, TParam2, TParam3, TParam4, TParam5, TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandHandlerFunctor{TParam1, TParam2, TParam3, TParam4, TParam5, TCommand}"/> class.
        /// </summary>
        /// <param name="callback">The delegate to wrap. If null, will try to find `TCommand.DefaultCommandHandler`.</param>
        public CommandHandlerFunctor(CommandHandler<TParam1, TParam2, TParam3, TParam4, TParam5, TCommand> callback = null) : base(callback)
        {
            if (m_Callback == null)
                m_Callback = CommandHandlerUtilities.GetDefaultCommandHandler<CommandHandler<TParam1, TParam2, TParam3, TParam4, TParam5, TCommand>, TCommand>();
        }
    }
}
