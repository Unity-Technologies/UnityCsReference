// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/Utility/ChangeTracker.h")]
    [RequiredByNativeCode]
    internal class SerializedObjectChangeTracker: IDisposable
    {
        IntPtr m_Handle;

        // This is so the garbage collector won't clean up SerializedObject behind the scenes.
        internal SerializedObject m_SerializedObject;

        private SerializedProperty[] m_ModifiedTrackedProperties;
        private static readonly SerializedProperty[] s_EmptyPropertyArray = new SerializedProperty[] { };

        public SerializedObjectChangeTracker(SerializedObject obj)
        {
            m_SerializedObject= obj;
            m_Handle = Internal_AcquireTracker(obj);
            m_ModifiedTrackedProperties = s_EmptyPropertyArray;
        }

        ~SerializedObjectChangeTracker() { Dispose(); }

        [ThreadAndSerializationSafe()]
        public void Dispose()
        {
            if (m_Handle != IntPtr.Zero)
            {
                ClearModifiedTrackedProperties();
                Internal_ReleaseTracker(m_Handle);
                m_Handle = IntPtr.Zero;
                m_SerializedObject = null;
            }
        }


        public SerializedProperty[] GetModifiedTrackedProperties()
        {
            return m_ModifiedTrackedProperties;
        }

        public void ClearModifiedTrackedProperties()
        {
            if (m_ModifiedTrackedProperties.Length > 0)
            {
                for (int i = 0; i < m_ModifiedTrackedProperties.Length; ++i)
                {
                    m_ModifiedTrackedProperties[i].Dispose();
                }

                m_ModifiedTrackedProperties = s_EmptyPropertyArray;
            }
        }

        public bool HasModifiedTrackedProperties()
        {
            return m_ModifiedTrackedProperties.Length > 0;
        }

        [NativeName("GetSyncedTrackedProperties")]
        public extern int[] GetSyncedTrackedProperties();

        [NativeName("HasModifiedTrackedProperties")]
        internal extern bool HasModifiedTrackedPropertiesInternal();

        [NativeName("GetModifiedTrackedProperties")]
        private extern SerializedProperty[] GetModifiedTrackedPropertiesInternal();

        [NativeName("ClearModifiedTrackedProperties")]
        private extern void ClearModifiedTrackedPropertiesInternal();

        [FreeFunction("SerializedObjectChangeTracker::AcquireTracker")]
        private static extern IntPtr Internal_AcquireTracker(UnityEditor.SerializedObject o);

        [FreeFunction("SerializedObjectChangeTracker::ReleaseTracker", IsThreadSafe = true)]
        private static extern void Internal_ReleaseTracker(IntPtr handle);

        // returns true if object changed since last poll
        internal bool PollForChanges(bool updateSerializedObject)
        {
            if (m_Handle == IntPtr.Zero)
                throw new ArgumentNullException("Not a valid handle, has it been released already?");
            bool result = Internal_PollChanges(updateSerializedObject);

            if (HasModifiedTrackedPropertiesInternal())
            {
                m_ModifiedTrackedProperties = GetModifiedTrackedPropertiesInternal();
                for (int i = 0; i < m_ModifiedTrackedProperties.Length; ++i)
                {
                    m_ModifiedTrackedProperties[i].m_SerializedObject = m_SerializedObject;
                }

                ClearModifiedTrackedPropertiesInternal();
            }
            else
            {
                m_ModifiedTrackedProperties = s_EmptyPropertyArray;
            }

            return result;
        }

        [NativeName("PollChanges")]
        private extern bool Internal_PollChanges(bool updateSerializedObject);

        [NativeName("TrackPropertyValue")]
        internal extern void AddPropertyTracking(SerializedProperty property);

        internal void RemovePropertyTracking(SerializedProperty property)
        {
            RemovePropertyTracking(property.hashCodeForPropertyPath);
        }

        [NativeName("UntrackPropertyValue")]
        internal extern void RemovePropertyTracking(int propertyPathHash);
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(UnityEditor.SerializedObjectChangeTracker obj) => obj.m_Handle;
        }

    }
}
