// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.ShortcutManagement
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ReserveModifiersAttribute : Attribute
    {
        public ShortcutModifiers Modifiers { get; }

        public ReserveModifiersAttribute(ShortcutModifiers modifiers)
        {
            this.Modifiers = modifiers;
        }
    }
}
