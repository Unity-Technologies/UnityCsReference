// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
        internal string tag { get; }
        internal ShortcutBinding defaultBinding { get; }
        internal int priority { get; }
        public string displayName { get; set; }
        Action m_NoArgumentsAction;

        ShortcutAttribute(string id, Type context, string tag, int priority, ShortcutBinding defaultBinding)
        {
            this.identifier = id;
            this.context = context;
            this.tag = tag;
            this.defaultBinding = defaultBinding;
            this.priority = ShortcutAttributeUtility.AssignPriority(context, priority);
            displayName = identifier;
        }

        public ShortcutAttribute(string id, [DefaultValue("null")] Type context = null)
            : this(id, context, null, int.MaxValue, ShortcutBinding.empty)
        {
        }

        public ShortcutAttribute(string id, Type context, KeyCode defaultKeyCode, [DefaultValue(nameof(ShortcutModifiers.None))] ShortcutModifiers defaultShortcutModifiers = ShortcutModifiers.None)
            : this(id, context, null, int.MaxValue, new ShortcutBinding(new KeyCombination(defaultKeyCode, defaultShortcutModifiers)))
        {
        }

        public ShortcutAttribute(string id, Type context, string tag, KeyCode defaultKeyCode, [DefaultValue(nameof(ShortcutModifiers.None))] ShortcutModifiers defaultShortcutModifiers = ShortcutModifiers.None)
            : this(id, context, tag, int.MaxValue, new ShortcutBinding(new KeyCombination(defaultKeyCode, defaultShortcutModifiers)))
        {
        }

        public ShortcutAttribute(string id, Type context, string tag, KeyCode defaultKeyCode, [DefaultValue(nameof(ShortcutModifiers.None))] ShortcutModifiers defaultShortcutModifiers, int priority)
            : this(id, context, tag, priority, new ShortcutBinding(new KeyCombination(defaultKeyCode, defaultShortcutModifiers)))
        {
        }

        public ShortcutAttribute(string id, KeyCode defaultKeyCode, [DefaultValue(nameof(ShortcutModifiers.None))] ShortcutModifiers defaultShortcutModifiers = ShortcutModifiers.None)
            : this(id, null, null, int.MaxValue, new ShortcutBinding(new KeyCombination(defaultKeyCode, defaultShortcutModifiers)))
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

            var entry = new ShortcutEntry(identifier, defaultCombination, action, context, tag, type, displayName, priority);

            foreach (var attribute in methodInfo.GetCustomAttributes(typeof(ReserveModifiersAttribute), true))
                entry.m_ReservedModifier |= (attribute as ReserveModifiersAttribute).Modifiers;

            return entry;
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

        public ClutchShortcutAttribute(string id, Type context, string tag, KeyCode defaultKeyCode, [DefaultValue(nameof(ShortcutModifiers.None))] ShortcutModifiers defaultShortcutModifiers, int priority)
            : base(id, context, tag, defaultKeyCode, defaultShortcutModifiers, priority)
        {
        }

        public ClutchShortcutAttribute(string id, KeyCode defaultKeyCode, [DefaultValue(nameof(ShortcutModifiers.None))] ShortcutModifiers defaultShortcutModifiers = ShortcutModifiers.None)
            : base(id, defaultKeyCode, defaultShortcutModifiers)
        {
        }

        [RequiredSignature]
        static void ShortcutClutchMethod(ShortcutArguments args) { throw new InvalidOperationException(); }
    }

    // We want GameView shortcuts to trigger even in play mode so we declare them as menu shortcuts
    internal class GameViewShortcutAttribute : ShortcutBaseAttribute
    {
        internal string identifier { get; }
        internal ShortcutBinding defaultBinding { get; }
        public string displayName { get; set; }
        Action m_NoArgumentsAction;

        GameViewShortcutAttribute(string id, ShortcutBinding defaultBinding)
        {
            this.identifier = id;
            this.defaultBinding = defaultBinding;
            displayName = identifier;
        }

        public GameViewShortcutAttribute(string id) : this(id, ShortcutBinding.empty)
        {
        }

        public GameViewShortcutAttribute(string id, KeyCode defaultKeyCode, [DefaultValue("None")] ShortcutModifiers defaultShortcutModifiers = ShortcutModifiers.None)
            : this(id, new ShortcutBinding(new KeyCombination(defaultKeyCode, defaultShortcutModifiers)))
        {
        }

        void NoArgumentShortcutMethodProxy(ShortcutArguments arguments)
        {
            m_NoArgumentsAction();
        }

        internal override ShortcutEntry CreateShortcutEntry(MethodInfo methodInfo)
        {
            var defaultCombination = defaultBinding.keyCombinationSequence;
            var type = ShortcutType.Menu;
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

            return new ShortcutEntry(new Identifier(identifier), defaultCombination, action, typeof(GameView), null, type, displayName);
        }
    }


    class ShortcutAttributeUtility
    {
        internal const int DefaultGlobalPriority = 1_000_000;
        internal const int DefaultContextPriority = 1_000;

        internal static int AssignPriority(Type context, int suggestedPriority = int.MaxValue)
        {
            if (suggestedPriority == int.MaxValue)
            {
                if (context == null || context == ContextManager.globalContextType) return DefaultGlobalPriority;
                else return DefaultContextPriority;
            }

            return suggestedPriority;
        }
    }
}
