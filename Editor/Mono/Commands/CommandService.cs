// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Internal;

namespace UnityEditor
{
    [ExcludeFromDocs]
    public delegate void CommandHandler(CommandExecuteContext context);

    [ExcludeFromDocs, AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class CommandHandlerAttribute : Attribute
    {
        public string id { get; }
        public string label { get; }
        public CommandHint hint { get; }

        public CommandHandlerAttribute(string id, string label, CommandHint hint)
        {
            this.id = id;
            this.label = label;
            this.hint = hint;
        }

        public CommandHandlerAttribute(string id) : this(id, id, CommandHint.Any) {}
        public CommandHandlerAttribute(string id, CommandHint hint) : this(id, id, hint) {}
        public CommandHandlerAttribute(string id, string label) : this(id, label, CommandHint.Any) {}

        [UsedImplicitly, RequiredSignature] static void RequiredSignature(CommandExecuteContext context) {}
    }

    [ExcludeFromDocs, Flags]
    public enum CommandHint : long
    {
        Undefined   = -1,
        None        = 0,
        Event       = 1 << 0,
        Menu        = 1 << 1,
        Shortcut    = 1 << 2,
        Shelf       = 1 << 3,

        UI          = 1 << 20,
        OnGUI       = UI | 1 << 21,
        UIElements  = UI | 1 << 22,

        Validate    = 1 << 30,
        UserDefined = 1 << 31,

        Any = ~0L
    }

    [ExcludeFromDocs]
    public class CommandExecuteContext
    {
        public object[] args;
        public object result;
        public CommandHint hint;

        // args[0] should at least be null and args be size of 1 or more.
        public object data => args[0];

        public T GetArgument<T>(int index, T defaultValue = default(T))
        {
            if (0 < index || index >= args.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return (T)args[index];
        }
    }

    [ExcludeFromDocs]
    public static class CommandService
    {
        private struct Command
        {
            public string id;
            public string label;
            public CommandHint hint;
            public CommandHandler handler;
            public bool managed;
        }

        private static Dictionary<string, Command> s_Commands;
        private static readonly object[] k_DefaultArgs = { null };

        static CommandService()
        {
            s_Commands = ScanAttributes().ToDictionary(c => c.id, c => c);
        }

        public static string GetCommandLabel(string commandId)
        {
            if (!Exists(commandId))
                throw new ArgumentException($"No command with id {commandId} exists", nameof(commandId));

            return s_Commands[commandId].label;
        }

        public static void RegisterCommand(string id, string label, CommandHandler handler, CommandHint hint = CommandHint.Any)
        {
            if (Exists(id))
                throw new ArgumentException($"A command with id {id} already exists", nameof(id));

            s_Commands[id] = new Command {id = id, label = label ?? id, hint = hint, handler = handler, managed = false};
        }

        public static void RegisterCommand(string id, CommandHandler handler, CommandHint hint = CommandHint.Any)
        {
            RegisterCommand(id, id, handler, hint);
        }

        public static bool UnregisterCommand(string id)
        {
            if (!Exists(id))
                return false;

            if (s_Commands[id].managed)
                Debug.LogWarning($"The command {id} was not explicitly registered by the user and should not be unregistered.");

            return s_Commands.Remove(id);
        }

        public static bool Exists(string id)
        {
            return s_Commands.ContainsKey(id);
        }

        public static object Execute(string id)
        {
            return ExecuteCommand(id, CommandHint.Any, k_DefaultArgs);
        }

        public static object Execute(string id, CommandHint hint)
        {
            return ExecuteCommand(id, hint, k_DefaultArgs);
        }

        public static object Execute(string id, CommandHint hint, params object[] args)
        {
            var contextArgs = args != null && args.Length > 0 ? args : k_DefaultArgs;
            return ExecuteCommand(id, hint, contextArgs);
        }

        private static IEnumerable<Command> ScanAttributes()
        {
            var commands = new List<Command>();
            var commandAttributes = AttributeHelper.GetMethodsWithAttribute<CommandHandlerAttribute>();
            foreach (var method in commandAttributes.methodsWithAttributes)
            {
                var callback = Delegate.CreateDelegate(typeof(CommandHandler), method.info) as CommandHandler;
                if (callback == null)
                    continue;
                var attr = (CommandHandlerAttribute)method.attribute;

                if (commands.Any(c => c.id == attr.id))
                {
                    Debug.LogWarning($"There is already a command with the ID {attr.id}. " +
                        "Commands need to have a unique ID, i.e. \"Unity/Category/Command_42\".");
                    continue;
                }

                commands.Add(new Command {id = attr.id, label = attr.label ?? attr.id, hint = attr.hint, handler = callback, managed = true});
            }

            return commands;
        }

        private static object ExecuteCommand(string id, CommandHint hint, object[] args)
        {
            if (!Exists(id))
                throw new ArgumentException($"Command {id} does not exist", nameof(id));

            var command = s_Commands[id];

            if ((command.hint & hint) == 0)
                throw new ArgumentException($"Command ({id}, {command.hint}) does not match the hinting {hint}", nameof(id));

            var context = new CommandExecuteContext { hint = hint, args = args, result = null };
            command.handler(context);
            return context.result;
        }
    }
}
