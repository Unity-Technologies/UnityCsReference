// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngineInternal;

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
        Action m_NoArgumentsAction;

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

        void NoArgumentShortcutMethodProxy(ShortcutArguments arguments)
        {
            m_NoArgumentsAction();
        }

        public override ShortcutEntry CreateShortcutEntry(MethodInfo methodInfo)
        {
            var identifier = new Identifier(methodInfo, this);

            IEnumerable<KeyCombination> defaultCombination;

            KeyCombination keyCombination;
            if (KeyCombination.TryParseMenuItemBindingString(defaultKeyCombination, out keyCombination))
                defaultCombination = new[] { keyCombination };
            else
                defaultCombination = Enumerable.Empty<KeyCombination>();


            var type = this is ClutchShortcutAttribute ? ShortcutType.Clutch : ShortcutType.Action;
            var methodParams = methodInfo.GetParameters();
            Action<ShortcutArguments> action;

            // We instantiate this as the specific delegate type in advance,
            // because passing ShortcutArguments in object[] via MethodInfo.Invoke() causes boxing/allocation
            if (methodParams.Any())
                action = (Action<ShortcutArguments>)methodInfo.CreateDelegate(typeof(Action<ShortcutArguments>), null);
            else
            {
                m_NoArgumentsAction = (Action)methodInfo.CreateDelegate(typeof(Action), null);
                action = NoArgumentShortcutMethodProxy;
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
