// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngineInternal;
using UnityEngine.SceneManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using uei = UnityEngine.Internal;


namespace UnityEngine
{
    [ExcludeFromPreset]
    [UsedByNativeCode]
    [NativeHeader("Runtime/Export/GameObject.bindings.h")]
    public sealed partial class GameObject : Object
    {
        [FreeFunction("GameObjectBindings::CreatePrimitive")]
        public extern static GameObject CreatePrimitive(PrimitiveType type);

        [System.Security.SecuritySafeCritical]
        public unsafe T GetComponent<T>()
        {
            var h = new CastHelper<T>();
            GetComponentFastPath(typeof(T), new System.IntPtr(&h.onePointerFurtherThanT));
            return h.t;
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [FreeFunction(Name = "GameObjectBindings::GetComponentFromType", HasExplicitThis = true, ThrowsException = true)]
        public extern Component GetComponent(Type type);

        [FreeFunction(Name = "GameObjectBindings::GetComponentFastPath", HasExplicitThis = true, ThrowsException = true)]
        [NativeWritableSelf]
        internal extern void GetComponentFastPath(Type type, IntPtr oneFurtherThanResultValue);

        [FreeFunction(Name = "Scripting::GetScriptingWrapperOfComponentOfGameObject", HasExplicitThis = true)]
        internal extern Component GetComponentByName(string type);

        public Component GetComponent(string type)
        {
            return GetComponentByName(type);
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [FreeFunction(Name = "GameObjectBindings::GetComponentInChildren", HasExplicitThis = true, ThrowsException = true)]
        public extern Component GetComponentInChildren(Type type, bool includeInactive);

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public Component GetComponentInChildren(Type type)
        {
            return GetComponentInChildren(type, false);
        }

        [uei.ExcludeFromDocs]
        public T GetComponentInChildren<T>()
        {
            bool includeInactive = false;
            return GetComponentInChildren<T>(includeInactive);
        }

        public T GetComponentInChildren<T>([uei.DefaultValue("false")] bool includeInactive)
        {
            return (T)(object)GetComponentInChildren(typeof(T), includeInactive);
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        [FreeFunction(Name = "GameObjectBindings::GetComponentInParent", HasExplicitThis = true, ThrowsException = true)]
        public extern Component GetComponentInParent(Type type);

        public T GetComponentInParent<T>()
        {
            return (T)(object)GetComponentInParent(typeof(T));
        }

        [FreeFunction(Name = "GameObjectBindings::GetComponentsInternal", HasExplicitThis = true, ThrowsException = true)]
        private extern System.Array GetComponentsInternal(Type type, bool useSearchTypeAsArrayReturnType, bool recursive, bool includeInactive, bool reverse, object resultList);

        public Component[] GetComponents(Type type)
        {
            return (Component[])GetComponentsInternal(type, false, false, true, false, null);
        }

        public T[] GetComponents<T>()
        {
            return (T[])GetComponentsInternal(typeof(T), true, false, true, false, null);
        }

        public void GetComponents(Type type, List<Component> results)
        {
            GetComponentsInternal(type, false, false, true, false, results);
        }

        public void GetComponents<T>(List<T> results)
        {
            GetComponentsInternal(typeof(T), false, false, true, false, results);
        }

        [uei.ExcludeFromDocs]
        public Component[] GetComponentsInChildren(Type type)
        {
            bool includeInactive = false;
            return GetComponentsInChildren(type, includeInactive);
        }

        public Component[] GetComponentsInChildren(Type type, [uei.DefaultValue("false")]  bool includeInactive)
        {
            return (Component[])GetComponentsInternal(type, false, true, includeInactive, false, null);
        }

        public T[] GetComponentsInChildren<T>(bool includeInactive)
        {
            return (T[])GetComponentsInternal(typeof(T), true, true, includeInactive, false, null);
        }

        public void GetComponentsInChildren<T>(bool includeInactive, List<T> results)
        {
            GetComponentsInternal(typeof(T), true, true, includeInactive, false, results);
        }

        public T[] GetComponentsInChildren<T>()
        {
            return GetComponentsInChildren<T>(false);
        }

        public void GetComponentsInChildren<T>(List<T> results)
        {
            GetComponentsInChildren<T>(false, results);
        }

        [uei.ExcludeFromDocs]
        public Component[] GetComponentsInParent(Type type)
        {
            bool includeInactive = false;
            return GetComponentsInParent(type, includeInactive);
        }

        public Component[] GetComponentsInParent(Type type, [uei.DefaultValue("false")]  bool includeInactive)
        {
            return (Component[])GetComponentsInternal(type, false, true, includeInactive, true, null);
        }

        public void GetComponentsInParent<T>(bool includeInactive, List<T> results)
        {
            GetComponentsInternal(typeof(T), true, true, includeInactive, true, results);
        }

        public T[] GetComponentsInParent<T>(bool includeInactive)
        {
            return (T[])GetComponentsInternal(typeof(T), true, true, includeInactive, true, null);
        }

        public T[] GetComponentsInParent<T>()
        {
            return GetComponentsInParent<T>(false);
        }

        public static GameObject FindWithTag(string tag)
        {
            return FindGameObjectWithTag(tag);
        }

        public void SendMessageUpwards(string methodName, SendMessageOptions options)
        {
            SendMessageUpwards(methodName, null, options);
        }

        public void SendMessage(string methodName, SendMessageOptions options)
        {
            SendMessage(methodName, null, options);
        }

        public void BroadcastMessage(string methodName, SendMessageOptions options)
        {
            BroadcastMessage(methodName, null, options);
        }

        [FreeFunction(Name = "MonoAddComponent", HasExplicitThis = true)]
        internal extern Component AddComponentInternal(string className);

        [FreeFunction(Name = "MonoAddComponentWithType", HasExplicitThis = true)]
        private extern Component Internal_AddComponentWithType(Type componentType);

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public Component AddComponent(Type componentType)
        {
            return Internal_AddComponentWithType(componentType);
        }

        public T AddComponent<T>() where T : Component
        {
            return AddComponent(typeof(T)) as T;
        }

        public extern Transform transform
        {
            [FreeFunction("GameObjectBindings::GetTransform", HasExplicitThis = true)]
            get;
        }

        public extern int layer { get; set; }

        [Obsolete("GameObject.active is obsolete. Use GameObject.SetActive(), GameObject.activeSelf or GameObject.activeInHierarchy.")]
        public extern bool active
        {
            [NativeMethod(Name = "IsActive")]
            get;
            [NativeMethod(Name = "SetSelfActive")]
            set;
        }

        [NativeMethod(Name = "SetSelfActive")]
        public extern void SetActive(bool value);

        public extern bool activeSelf
        {
            [NativeMethod(Name = "IsSelfActive")]
            get;
        }

        public extern bool activeInHierarchy
        {
            [NativeMethod(Name = "IsActive")]
            get;
        }

        [Obsolete("gameObject.SetActiveRecursively() is obsolete. Use GameObject.SetActive(), which is now inherited by children.")]
        [NativeMethod(Name = "SetActiveRecursivelyDeprecated")]
        public extern void SetActiveRecursively(bool state);

        public extern bool isStatic
        {
            [NativeMethod(Name = "GetIsStaticDeprecated")]
            get;
            [NativeMethod(Name = "SetIsStaticDeprecated")]
            set;
        }


        internal extern bool isStaticBatchable
        {
            [NativeMethod(Name = "IsStaticBatchable")]
            get;
        }

        public extern string tag
        {
            [FreeFunction("GameObjectBindings::GetTag", HasExplicitThis = true, ThrowsException = true)]
            get;
            [FreeFunction("GameObjectBindings::SetTag", HasExplicitThis = true)]
            set;
        }

        [FreeFunction(Name = "GameObjectBindings::CompareTag", HasExplicitThis = true)]
        public extern bool CompareTag(string tag);

        [FreeFunction(Name = "GameObjectBindings::FindGameObjectWithTag", ThrowsException = true)]
        public static extern GameObject FindGameObjectWithTag(string tag);

        [FreeFunction(Name = "GameObjectBindings::FindGameObjectsWithTag", ThrowsException = true)]
        public static extern GameObject[] FindGameObjectsWithTag(string tag);

        [FreeFunction(Name = "Scripting::SendScriptingMessageUpwards", HasExplicitThis = true)]
        extern public void SendMessageUpwards(string methodName, [uei.DefaultValue("null")]  object value , [uei.DefaultValue("SendMessageOptions.RequireReceiver")]  SendMessageOptions options);

        [uei.ExcludeFromDocs]
        public void SendMessageUpwards(string methodName, object value)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            SendMessageUpwards(methodName, value, options);
        }

        [uei.ExcludeFromDocs]
        public void SendMessageUpwards(string methodName)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            object value = null;
            SendMessageUpwards(methodName, value, options);
        }

        [FreeFunction(Name = "Scripting::SendScriptingMessage", HasExplicitThis = true)]
        extern public void SendMessage(string methodName, [uei.DefaultValue("null")]  object value , [uei.DefaultValue("SendMessageOptions.RequireReceiver")]  SendMessageOptions options);

        [uei.ExcludeFromDocs]
        public void SendMessage(string methodName, object value)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            SendMessage(methodName, value, options);
        }

        [uei.ExcludeFromDocs]
        public void SendMessage(string methodName)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            object value = null;
            SendMessage(methodName, value, options);
        }

        [FreeFunction(Name = "Scripting::BroadcastScriptingMessage", HasExplicitThis = true)]
        extern public void BroadcastMessage(string methodName, [uei.DefaultValue("null")]  object parameter , [uei.DefaultValue("SendMessageOptions.RequireReceiver")]  SendMessageOptions options);

        [uei.ExcludeFromDocs]
        public void BroadcastMessage(string methodName, object parameter)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            BroadcastMessage(methodName, parameter, options);
        }

        [uei.ExcludeFromDocs]
        public void BroadcastMessage(string methodName)
        {
            SendMessageOptions options = SendMessageOptions.RequireReceiver;
            object parameter = null;
            BroadcastMessage(methodName, parameter, options);
        }

        public GameObject(string name)
        {
            Internal_CreateGameObject(this, name);
        }

        public GameObject()
        {
            Internal_CreateGameObject(this, null);
        }

        public GameObject(string name, params Type[] components)
        {
            Internal_CreateGameObject(this, name);
            foreach (Type t in components)
                AddComponent(t);
        }

        [FreeFunction(Name = "GameObjectBindings::Internal_CreateGameObject")]
        static extern void Internal_CreateGameObject([Writable] GameObject self, string name);

        [FreeFunction(Name = "GameObjectBindings::Find")]
        public static extern GameObject Find(string name);

        public extern Scene scene
        {
            [FreeFunction("GameObjectBindings::GetScene", HasExplicitThis = true)]
            get;
        }

        [FreeFunction(Name = "GameObjectBindings::CalculateBounds", HasExplicitThis = true)]
        internal extern Bounds CalculateBounds();

        public GameObject gameObject { get { return this; } }
    }
}
