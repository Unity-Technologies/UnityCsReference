// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace UnityEditor
{
    internal class InspectorUtility
    {
        internal delegate void LivePropertyOverrideCallback(SerializedObject serializedObject, bool isLiveUpdate);
        internal delegate bool LivePropertyChangedCallback(Object[] unityObject);

        internal static Dictionary<Type, LivePropertyOverrideCallback> s_LivePropertyOverrideCallbacks = new Dictionary<Type, LivePropertyOverrideCallback>();
        internal static Dictionary<Type, LivePropertyChangedCallback> s_LivePropertyChangedCallbacks = new Dictionary<Type, LivePropertyChangedCallback>();

        static SerializedObject s_CachedSerializedObject = new SerializedObject(IntPtr.Zero);

        internal static void SetLivePropertyOverride(Type type, LivePropertyOverrideCallback callback)
        {
            if (s_LivePropertyOverrideCallbacks.TryGetValue(type, out var del))
            {
                del += callback;
            }
            else
            {
                s_LivePropertyOverrideCallbacks[type] = callback;
            }
        }

        internal static void RemoveLivePropertyOverride(Type type, LivePropertyOverrideCallback callback)
        {
            s_LivePropertyOverrideCallbacks[type] -= callback;
            if (s_LivePropertyOverrideCallbacks[type] == null)
                s_LivePropertyOverrideCallbacks.Remove(type);
        }

        internal static void SetLivePropertyChanged(Type type, LivePropertyChangedCallback callback)
        {
            if (s_LivePropertyChangedCallbacks.TryGetValue(type, out var del))
            {
                del += callback;
            }
            else
            {
                s_LivePropertyChangedCallbacks[type] = callback;
            }
        }

        internal static void RemoveLivePropertyChanged(Type type, LivePropertyChangedCallback callback)
        {
            s_LivePropertyChangedCallbacks[type] -= callback;
            if (s_LivePropertyChangedCallbacks[type] == null)
                s_LivePropertyChangedCallbacks.Remove(type);
        }

        static bool IsLivePropertyChanged(Editor editor)
        {
            var target = editor.target;
            if (target == null)
                return false;

            var targetType = target.GetType();

            if (s_LivePropertyChangedCallbacks.TryGetValue(targetType, out var cb))
            {
                if (cb.Invoke(editor.targets))
                    return true;
            }

            return false;
        }

        internal static void DirtyLivePropertyChanges(ActiveEditorTracker tracker)
        {
            if (!EditorApplication.isPlaying || s_LivePropertyOverrideCallbacks.Count <= 0)
                return;

            var editors = tracker.activeEditors;
            for (var i = 0; i != editors.Length;i++)
            {
                if (tracker.GetVisible(i) == 0)
                    continue;

                // Callback
                var editor = editors[i];
                if (IsLivePropertyChanged(editor))
                    editor.isInspectorDirty = true;
            }
        }

        // Check if current serialized object contains any live properties.
        // If so, enable live property feature.
        [RequiredByNativeCode]
        internal static bool Internal_HasLiveProperties(IntPtr nativeObjectPtr)
        {
            if (!EditorApplication.isPlaying || s_LivePropertyOverrideCallbacks.Count <= 0)
                return false;

            if (s_CachedSerializedObject.m_NativeObjectPtr != IntPtr.Zero)
                throw new ArgumentException("Recursive EnableLivePropertyFeature are not allowed.");

            var livePropertyFeatureEnabled = false;

            try
            {
                s_CachedSerializedObject.m_NativeObjectPtr = nativeObjectPtr;
                var target = s_CachedSerializedObject.targetObject;
                if (target == null)
                    return false;

                var targetType = target.GetType();

                if (s_LivePropertyOverrideCallbacks.ContainsKey(targetType))
                {
                    livePropertyFeatureEnabled = true;
                }
            }
            finally
            {
                // This serialized object is just a temp reference to pass around, it doesn't own the pointer
                s_CachedSerializedObject.m_NativeObjectPtr = IntPtr.Zero;
            }

            return livePropertyFeatureEnabled;
        }

        // Invoke override callback for live property.
        [RequiredByNativeCode]
        internal static void Internal_CallPropertyOverrideCallback(IntPtr nativeObjectPtr, bool isLiveUpdate)
        {
            if (s_CachedSerializedObject.m_NativeObjectPtr != IntPtr.Zero && s_CachedSerializedObject.m_NativeObjectPtr != nativeObjectPtr)
                throw new ArgumentException("Recursive SetLivePropertyOverrides are not allowed.");

            try
            {
                if (s_CachedSerializedObject.m_NativeObjectPtr == IntPtr.Zero)
                {
                    s_CachedSerializedObject.m_NativeObjectPtr = nativeObjectPtr;
                    if (s_CachedSerializedObject.targetObject == null)
                        return;
                }

                var targetType = s_CachedSerializedObject.targetObject.GetType();
                if (s_LivePropertyOverrideCallbacks.TryGetValue(targetType, out var cb))
                {
                    cb.Invoke(s_CachedSerializedObject, isLiveUpdate);
                }
            }
            finally
            {
                // This serialized object is just a temp reference to pass around, it doesn't own the pointer
                s_CachedSerializedObject.m_NativeObjectPtr = IntPtr.Zero;
            }
        }
    }
}
