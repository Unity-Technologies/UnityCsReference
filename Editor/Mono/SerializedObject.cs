// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    public partial class SerializedObject
    {
        // Create SerializedObject for inspected object.
        public SerializedObject(Object obj)
        {
            InternalCreate(new Object[] {obj}, null);
        }

        public SerializedObject(Object obj, Object context)
        {
            InternalCreate(new Object[] {obj}, context);
        }

        // Create SerializedObject for inspected object.
        public SerializedObject(Object[] objs)
        {
            InternalCreate(objs, null);
        }

        public SerializedObject(Object[] objs, Object context)
        {
            InternalCreate(objs, context);
        }

        ~SerializedObject() { Dispose(); }

        // Get the first serialized property.
        public SerializedProperty GetIterator()
        {
            SerializedProperty i = GetIterator_Internal();
            // This is so the garbage collector won't clean up SerializedObject behind the scenes,
            // when we are still iterating properties
            i.m_SerializedObject = this;
            return i;
        }

        // Find serialized property by name.
        public SerializedProperty FindProperty(string propertyPath)
        {
            SerializedProperty i = GetIterator_Internal();
            // This is so the garbage collector won't clean up SerializedObject behind the scenes,
            // when we are still iterating properties
            i.m_SerializedObject = this;
            if (i.FindPropertyInternal(propertyPath))
                return i;
            else
                return null;
        }
    }
}
