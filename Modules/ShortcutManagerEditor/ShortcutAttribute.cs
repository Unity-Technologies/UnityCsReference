// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    abstract class ShortcutBaseAttribute : Attribute
    {
        public abstract ShortcutEntry CreateShortcutEntry(MethodInfo methodInfo);
    }

    // TODO: Find better name
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    class ShortcutAttribute : ShortcutBaseAttribute
    {
        internal string identifier { get; }
        internal Type context { get; }
        internal string defaultKeyCombination { get; }


        static readonly object[] k_ReusableShortcutArgs = { null };
        static readonly object[] k_EmptyReusableShortcutArgs = {};

        public ShortcutAttribute(string identifier, Type context = null, string defaultKeyCombination = null)
        {
            this.identifier = identifier;
            this.context = context;
            this.defaultKeyCombination = defaultKeyCombination;
        }

        [RequiredSignature]
        static extern void ShortcutMethodWithArgs(ShortcutArguments args);
        [RequiredSignature]
        static extern void ShortcutMethodNoArgs();

        public override ShortcutEntry CreateShortcutEntry(MethodInfo methodInfo)
        {
            var identifier = new Identifier(methodInfo, this);

            IEnumerable<KeyCombination> defaultCombination;
            if (defaultKeyCombination == null)
                defaultCombination = Enumerable.Empty<KeyCombination>();
            else
                defaultCombination = new[] { KeyCombination.ParseLegacyBindingString(defaultKeyCombination) };

            var type = this is ClutchShortcutAttribute ? ShortcutType.Clutch : ShortcutType.Action;
            var methodParams = methodInfo.GetParameters();
            Action<ShortcutArguments> action;
            if (methodParams.Length == 0)
            {
                action = shortcutArgs =>
                {
                    methodInfo.Invoke(null, k_EmptyReusableShortcutArgs);
                };
            }
            else
            {
                action = shortcutArgs =>
                {
                    k_ReusableShortcutArgs[0] = shortcutArgs;
                    methodInfo.Invoke(null, k_ReusableShortcutArgs);
                };
            }

            return new ShortcutEntry(identifier, defaultCombination, action, context, type);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    // TODO: Find better name
    class ClutchShortcutAttribute : ShortcutAttribute
    {
        public ClutchShortcutAttribute(string identifier, Type context = null, string defaultKeyCombination = null)
            : base(identifier, context, defaultKeyCombination)
        {
        }

        [RequiredSignature]
        static extern void ShortcutClutchMethod(ShortcutArguments args);
    }
}
