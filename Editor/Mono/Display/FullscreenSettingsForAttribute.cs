// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [VisibleToOtherModules]
    internal class FullscreenSettingsForAttribute : Attribute
    {
        public FullscreenSettingsForAttribute(Type assignedType)
        {
            AssignedType = assignedType;
        }

        public Type AssignedType { get; set; }
    }
}
