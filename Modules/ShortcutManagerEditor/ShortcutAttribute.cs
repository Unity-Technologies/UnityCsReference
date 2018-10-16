// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    public abstract class ShortcutBaseAttribute : Attribute
    {
        internal abstract ShortcutEntry CreateShortcutEntry(MethodInfo methodInfo);
    }

    // TODO: Find better name
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ShortcutAttribute : ShortcutBaseAttribute
    {
        internal string identifier { get; }
        internal Type context { get; }
        internal ShortcutBinding defaultBinding { get; }

        static readonly object[] k_ReusableShortcutArgs = { null };
        static readonly object[] k_EmptyReusableShortcutArgs = {};

        ShortcutAttribute(string id, Type context, ShortcutBinding defaultBinding)
        {
            this.identifier = id;
            this.context = context;
            this.defaultBinding = defaultBinding;
        }

        public ShortcutAttribute(string id, [DefaultValue("null")] Type context = null)
            : this(id, context, ShortcutBinding.empty)
        {
        }

        public ShortcutAttribute(string id, Type context, KeyCode defaultKeyCode, [DefaultValue(nameof(ShortcutModifiers.None))] ShortcutModifiers defaultShortcutModifiers = ShortcutModifiers.None)
            : this(id, context, new ShortcutBinding(new KeyCombination(defaultKeyCode, defaultShortcutModifiers)))
        {
        }

        public ShortcutAttribute(string id, KeyCode defaultKeyCode, [DefaultValue(nameof(ShortcutModifiers.None))] ShortcutModifiers defaultShortcutModifiers = ShortcutModifiers.None)
            : this(id, null, new ShortcutBinding(new KeyCombination(defaultKeyCode, defaultShortcutModifiers)))
        {
        }

        [RequiredSignature]
        static extern void ShortcutMethodWithArgs(ShortcutArguments args);
        [RequiredSignature]
        static extern void ShortcutMethodNoArgs();

        internal override ShortcutEntry CreateShortcutEntry(MethodInfo methodInfo)
        {
            var identifier = new Identifier(methodInfo, this);
            var defaultCombination = defaultBinding.keyCombinationSequence;
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
    public class ClutchShortcutAttribute : ShortcutAttribute
    {
        public ClutchShortcutAttribute(string id, [DefaultValue("null")] Type context = null)
            : base(id, context)
        {
        }

        public ClutchShortcutAttribute(string id, Type context, KeyCode defaultKeyCode, [DefaultValue(nameof(ShortcutModifiers.None))] ShortcutModifiers defaultShortcutModifiers = ShortcutModifiers.None)
            : base(id, context, defaultKeyCode, defaultShortcutModifiers)
        {
        }

        public ClutchShortcutAttribute(string id, KeyCode defaultKeyCode, [DefaultValue(nameof(ShortcutModifiers.None))] ShortcutModifiers defaultShortcutModifiers = ShortcutModifiers.None)
            : base(id, defaultKeyCode, defaultShortcutModifiers)
        {
        }

        [RequiredSignature]
        static extern void ShortcutClutchMethod(ShortcutArguments args);
    }
}
