// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngineInternal;

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
        internal string tag { get; }
        internal ShortcutBinding defaultBinding { get; }
        public string displayName { get; set; }
        Action m_NoArgumentsAction;

        ShortcutAttribute(string id, Type context, string tag, ShortcutBinding defaultBinding)
        {
            this.identifier = id;
            this.context = context;
            this.tag = tag;
            this.defaultBinding = defaultBinding;
            displayName = identifier;
        }

        public ShortcutAttribute(string id, [DefaultValue("null")] Type context = null)
            : this(id, context, null, ShortcutBinding.empty)
        {
        }

        public ShortcutAttribute(string id, Type context, KeyCode defaultKeyCode, [DefaultValue(nameof(ShortcutModifiers.None))] ShortcutModifiers defaultShortcutModifiers = ShortcutModifiers.None)
            : this(id, context, null, new ShortcutBinding(new KeyCombination(defaultKeyCode, defaultShortcutModifiers)))
        {
        }

        public ShortcutAttribute(string id, Type context, string tag, KeyCode defaultKeyCode, [DefaultValue(nameof(ShortcutModifiers.None))] ShortcutModifiers defaultShortcutModifiers = ShortcutModifiers.None)
            : this(id, context, tag, new ShortcutBinding(new KeyCombination(defaultKeyCode, defaultShortcutModifiers)))
        {
        }

        public ShortcutAttribute(string id, KeyCode defaultKeyCode, [DefaultValue(nameof(ShortcutModifiers.None))] ShortcutModifiers defaultShortcutModifiers = ShortcutModifiers.None)
            : this(id, null, null, new ShortcutBinding(new KeyCombination(defaultKeyCode, defaultShortcutModifiers)))
        {
        }

        [RequiredSignature]
        static void ShortcutMethodWithArgs(ShortcutArguments args) { throw new InvalidOperationException(); }
        [RequiredSignature]
        static void ShortcutMethodNoArgs() { throw new InvalidOperationException(); }

        void NoArgumentShortcutMethodProxy(ShortcutArguments arguments)
        {
            m_NoArgumentsAction();
        }

        internal override ShortcutEntry CreateShortcutEntry(MethodInfo methodInfo)
        {
            var identifier = new Identifier(methodInfo, this);
            var defaultCombination = defaultBinding.keyCombinationSequence;
            var type = this is ClutchShortcutAttribute ? ShortcutType.Clutch : ShortcutType.Action;
            var methodParams = methodInfo.GetParameters();
            Action<ShortcutArguments> action;

            // We instantiate this as the specific delegate type in advance,
            // because passing ShortcutArguments in object[] via MethodInfo.Invoke() causes boxing/allocation
            if (methodParams.Any())
                action = (Action<ShortcutArguments>)Delegate.CreateDelegate(typeof(Action<ShortcutArguments>), null, methodInfo);
            else
            {
                m_NoArgumentsAction = (Action)Delegate.CreateDelegate(typeof(Action), null, methodInfo);
                action = NoArgumentShortcutMethodProxy;
            }

            return new ShortcutEntry(identifier, defaultCombination, action, context, tag, type, displayName);
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
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

        public ClutchShortcutAttribute(string id, Type context, string tag, KeyCode defaultKeyCode, [DefaultValue(nameof(ShortcutModifiers.None))] ShortcutModifiers defaultShortcutModifiers = ShortcutModifiers.None)
            : base(id, context, tag, defaultKeyCode, defaultShortcutModifiers)
        {
        }

        public ClutchShortcutAttribute(string id, KeyCode defaultKeyCode, [DefaultValue(nameof(ShortcutModifiers.None))] ShortcutModifiers defaultShortcutModifiers = ShortcutModifiers.None)
            : base(id, defaultKeyCode, defaultShortcutModifiers)
        {
        }

        [RequiredSignature]
        static void ShortcutClutchMethod(ShortcutArguments args) { throw new InvalidOperationException(); }
    }
}
