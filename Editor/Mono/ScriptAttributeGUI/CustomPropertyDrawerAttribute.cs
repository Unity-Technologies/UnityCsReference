// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    // Tells a custom [[PropertyDrawer]] which run-time [[Serializable]] class or [[PropertyAttribute]] it's a drawer for.
    [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class CustomPropertyDrawer : Attribute
    {
        internal Type m_Type;
        internal bool m_UseForChildren;

        // Tells a PropertyDrawer class which run-time class or attribute it's a drawer for.
        public CustomPropertyDrawer(Type type)
        {
            m_Type = type;
        }

        // Tells a PropertyDrawer class which run-time class or attribute it's a drawer for.
        public CustomPropertyDrawer(Type type, bool useForChildren)
        {
            m_Type = type;
            m_UseForChildren = useForChildren;
        }
    }
}
