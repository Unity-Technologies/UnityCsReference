// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Internal;
using UnityEngineInternal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

using System;
using System.Collections.Generic;

namespace UnityEngine
{
    [RequiredByNativeCode]
    [NativeClass("Unity::Component")]
    [NativeHeader("Runtime/Export/Component.bindings.h")]
    public partial class Component : UnityEngine.Object
    {
        public extern Transform transform
        {
            [FreeFunction("GetTransform", HasExplicitThis = true, ThrowsException = true)]
            get;
        }

        public extern GameObject gameObject
        {
            [FreeFunction("GetGameObject", HasExplicitThis = true)]
            get;
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public Component GetComponent(Type type)
        {
            return gameObject.GetComponent(type);
        }

        [FreeFunction(HasExplicitThis = true, ThrowsException = true)]
        extern internal void GetComponentFastPath(System.Type type, IntPtr oneFurtherThanResultValue);

        [System.Security.SecuritySafeCritical]
        public unsafe T GetComponent<T>()
        {
            var h = new CastHelper<T>();
            GetComponentFastPath(typeof(T), new System.IntPtr(&h.onePointerFurtherThanT));
            return h.t;
        }

        [FreeFunction(HasExplicitThis = true)]
        extern public Component GetComponent(string type);

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public Component GetComponentInChildren(Type t, bool includeInactive)
        {
            return gameObject.GetComponentInChildren(t, includeInactive);
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public Component GetComponentInChildren(Type t)
        {
            return GetComponentInChildren(t, false);
        }

        public T GetComponentInChildren<T>([DefaultValue("false")] bool includeInactive)
        {
            return (T)(object)GetComponentInChildren(typeof(T), includeInactive);
        }

        [ExcludeFromDocs]
        public T GetComponentInChildren<T>()
        {
            return (T)(object)GetComponentInChildren(typeof(T), false);
        }

        public Component[] GetComponentsInChildren(Type t, bool includeInactive)
        {
            return gameObject.GetComponentsInChildren(t, includeInactive);
        }

        [ExcludeFromDocs]
        public Component[] GetComponentsInChildren(Type t)
        {
            return gameObject.GetComponentsInChildren(t, false);
        }

        public T[] GetComponentsInChildren<T>(bool includeInactive)
        {
            return gameObject.GetComponentsInChildren<T>(includeInactive);
        }

        public void GetComponentsInChildren<T>(bool includeInactive, List<T> result)
        {
            gameObject.GetComponentsInChildren<T>(includeInactive, result);
        }

        public T[] GetComponentsInChildren<T>()
        {
            return GetComponentsInChildren<T>(false);
        }

        public void GetComponentsInChildren<T>(List<T> results)
        {
            GetComponentsInChildren<T>(false, results);
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public Component GetComponentInParent(Type t)
        {
            return gameObject.GetComponentInParent(t);
        }

        public T GetComponentInParent<T>()
        {
            return (T)(object)GetComponentInParent(typeof(T));
        }

        public Component[] GetComponentsInParent(Type t, [DefaultValue("false")] bool includeInactive)
        {
            return gameObject.GetComponentsInParent(t, includeInactive);
        }

        [ExcludeFromDocs]
        public Component[] GetComponentsInParent(Type t)
        {
            return GetComponentsInParent(t, false);
        }

        public T[] GetComponentsInParent<T>(bool includeInactive)
        {
            return gameObject.GetComponentsInParent<T>(includeInactive);
        }

        public void GetComponentsInParent<T>(bool includeInactive, List<T> results)
        {
            gameObject.GetComponentsInParent(includeInactive, results);
        }

        public T[] GetComponentsInParent<T>()
        {
            return GetComponentsInParent<T>(false);
        }

        public Component[] GetComponents(Type type)
        {
            return gameObject.GetComponents(type);
        }

        [FreeFunction(HasExplicitThis = true)]
        extern private void GetComponentsForListInternal(Type searchType, object resultList);

        public void GetComponents(Type type, List<Component> results)
        {
            GetComponentsForListInternal(type, results);
        }

        public void GetComponents<T>(List<T> results)
        {
            GetComponentsForListInternal(typeof(T), results);
        }

        public string tag
        {
            get { return gameObject.tag; }
            set { gameObject.tag = value; }
        }

        public T[] GetComponents<T>()
        {
            return gameObject.GetComponents<T>();
        }

        public bool CompareTag(string tag)
        {
            return gameObject.CompareTag(tag);
        }

        [FreeFunction(HasExplicitThis = true)]
        extern public void SendMessageUpwards(string methodName, [DefaultValue("null")] object value, [DefaultValue("SendMessageOptions.RequireReceiver")] SendMessageOptions options);

        [ExcludeFromDocs]
        public void SendMessageUpwards(string methodName, object value)
        {
            SendMessageUpwards(methodName, value, SendMessageOptions.RequireReceiver);
        }

        [ExcludeFromDocs]
        public void SendMessageUpwards(string methodName)
        {
            SendMessageUpwards(methodName, null, SendMessageOptions.RequireReceiver);
        }

        public void SendMessageUpwards(string methodName, SendMessageOptions options)
        {
            SendMessageUpwards(methodName, null, options);
        }

        public void SendMessage(string methodName, object value)
        {
            SendMessage(methodName, value, SendMessageOptions.RequireReceiver);
        }

        public void SendMessage(string methodName)
        {
            SendMessage(methodName, null, SendMessageOptions.RequireReceiver);
        }

        [FreeFunction("SendMessage", HasExplicitThis = true)]
        extern public void SendMessage(string methodName, object value, SendMessageOptions options);

        public void SendMessage(string methodName, SendMessageOptions options)
        {
            SendMessage(methodName, null, options);
        }

        [FreeFunction("BroadcastMessage", HasExplicitThis = true)]
        extern public void BroadcastMessage(string methodName, [DefaultValue("null")] object parameter, [DefaultValue("SendMessageOptions.RequireReceiver")] SendMessageOptions options);

        [ExcludeFromDocs]
        public void BroadcastMessage(string methodName, object parameter)
        {
            BroadcastMessage(methodName, parameter, SendMessageOptions.RequireReceiver);
        }

        [ExcludeFromDocs]
        public void BroadcastMessage(string methodName)
        {
            BroadcastMessage(methodName, null, SendMessageOptions.RequireReceiver);
        }

        public void BroadcastMessage(string methodName, SendMessageOptions options)
        {
            BroadcastMessage(methodName, null, options);
        }
    }
}
