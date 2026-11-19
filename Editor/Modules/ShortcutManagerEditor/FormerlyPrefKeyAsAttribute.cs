// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.ShortcutManagement
{
    [AttributeUsage(AttributeTargets.Method)]
    internal class FormerlyPrefKeyAsAttribute : Attribute
    {
        public readonly string name;
        public readonly string defaultValue;

        public FormerlyPrefKeyAsAttribute(string name, string defaultValue)
        {
            this.name = name;
            this.defaultValue = defaultValue;
        }
    }
}
