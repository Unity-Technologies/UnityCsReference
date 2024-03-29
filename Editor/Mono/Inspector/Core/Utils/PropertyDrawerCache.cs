// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor
{
    internal class PropertyHandlerCache : IDisposable
    {
        internal readonly protected Dictionary<int, PropertyHandler> m_PropertyHandlers = new Dictionary<int, PropertyHandler>();

        internal PropertyHandler GetHandler(SerializedProperty property)
        {
            PropertyHandler handler;
            int key = GetPropertyHash(property);
            if (m_PropertyHandlers.TryGetValue(key, out handler))
                return handler;

            return null;
        }

        internal void SetHandler(SerializedProperty property, PropertyHandler handler)
        {
            int key = GetPropertyHash(property);
            m_PropertyHandlers[key] = handler;
        }

        internal static bool CanUseSameHandler(SerializedProperty p1, SerializedProperty p2)
        {
            return GetPropertyHash(p1) == GetPropertyHash(p2);
        }

        private static int GetPropertyHash(SerializedProperty property)
        {
            if (property.serializedObject.targetObject == null)
                return 0;

            // For efficiency, ignore indices inside brackets [] in order to make array elements share handlers.
            int key = property.serializedObject.targetObject.GetInstanceID() ^ property.hashCodeForPropertyPathWithoutArrayIndex;
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                key ^= property.objectReferenceInstanceIDValue;
            }
            return key;
        }

        public void Clear()
        {
            if (m_PropertyHandlers.Count > 0)
            {
                foreach (var handler in m_PropertyHandlers.Values)
                {
                    handler.Dispose();
                }

                m_PropertyHandlers.Clear();
            }
        }

        public void Dispose() => Clear();
    }
}
