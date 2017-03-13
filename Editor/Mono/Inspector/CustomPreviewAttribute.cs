// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    // Tells a custom [[IPreviewable]] which run-time [[Serializable]] class or [[PropertyAttribute]] it's a drawer for.
    [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CustomPreviewAttribute : Attribute
    {
        internal Type m_Type;

        // Tells a PropertyDrawer class which run-time class or attribute it's a drawer for.
        public CustomPreviewAttribute(Type type)
        {
            m_Type = type;
        }
    }
}
