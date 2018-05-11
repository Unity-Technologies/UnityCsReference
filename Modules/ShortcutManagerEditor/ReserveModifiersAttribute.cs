// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.ShortcutManagement
{
    [AttributeUsage(AttributeTargets.Class)]
    class ReserveModifiersAttribute : Attribute
    {
        public ShortcutModifiers modifier { get; }

        public ReserveModifiersAttribute(ShortcutModifiers modifier)
        {
            this.modifier = modifier;
        }
    }
}
