// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.ShortcutManagement
{
    // TODO: Find better name
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    class ShortcutAttribute : Attribute
    {
        internal string identifier { get; }
        internal Type context { get; }
        internal string defaultKeyCombination { get; }

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
