// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Internal;
using UnityEngineInternal;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Entities;

using System;
using System.Collections.Generic;

namespace UnityEngine
{
    [RequiredByNativeCode]
    [NativeClass("Unity::Component", PersistentTypeId = 2)]
    [NativeHeader("Runtime/Export/Scripting/Component.bindings.h")]
    public partial class Component : UnityEngine.Object
    {
        public Component() {}
        protected internal Component(global::UnityEngine.EntityId id) : base(id) {}
        public Transform transform
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (IsDataComponentWrapper())
                    return GetDataComponentOwner().transform;
                return GetTransform();
            }
        }

        [FreeFunction("GetTransform", HasExplicitThis = true, ThrowsException = true)]
        extern Transform GetTransform();

        public TransformHandle transformHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (IsDataComponentWrapper())
                    return GetDataComponentOwner().transformHandle;
                return GetTransformHandle();
            }
        }

        [FreeFunction("GetTransformHandle", HasExplicitThis = true, ThrowsException = true)]
        extern TransformHandle GetTransformHandle();

        public GameObject gameObject
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (IsDataComponentWrapper())
                    return GetDataComponentOwner();
                return GetGameObject();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        GameObject GetDataComponentOwner()
        {
            ThrowIfDataComponentMissing();
            return Resources.EntityIdToObject(DataComponentWrapperStore.GetOwnerId(GetEntityId())) as GameObject;
        }

        [FreeFunction("GetGameObject", HasExplicitThis = true)]
        extern GameObject GetGameObject();

        /// <summary>
        /// Whether this Component refers to a data component instead of a native component.
        /// </summary>
        /// <remarks>
        /// Passing a data component to a method that does not support it, such as <c>EditorUtility.SetDirty</c>, throws <see cref="System.NotSupportedException"/>.
        /// A data component can only be used from the main thread. On other threads it behaves as if it no longer exists.
        /// </remarks>
        public bool isDataComponent => IsDataComponentWrapper();

        /// <summary>
        /// Reads the value of the IComponentData that this Component refers to.
        /// </summary>
        /// <typeparam name="T">The type of the data component.</typeparam>
        /// <returns>A copy of the data component's value, or the default value for a component with no data.</returns>
        /// <exception cref="System.InvalidOperationException">This Component is not a data component.</exception>
        /// <exception cref="System.ArgumentException"><typeparamref name="T"/> is not the type of this data component.</exception>
        /// <exception cref="MissingReferenceException">The data component no longer exists.</exception>
        public T GetComponentData<T>() where T : unmanaged, IComponentData
        {
            Entity owner = GetExistingDataComponentOwner<T>(GetDataComponentIdOfType<T>());
            if (TypeManager.IsZeroSized(TypeManager.GetTypeIndex<T>()))
                return default;
            return GlobalEntityManager.GetComponentData<T>(owner);
        }

        /// <summary>
        /// Writes the value of the IComponentData that this Component refers to.
        /// </summary>
        /// <typeparam name="T">The type of the data component.</typeparam>
        /// <param name="value">The value to write. A component with no data ignores it.</param>
        /// <exception cref="System.InvalidOperationException">This Component is not a data component, or the World of the data component is in an exclusive entity transaction.</exception>
        /// <exception cref="System.ArgumentException"><typeparamref name="T"/> is not the type of this data component.</exception>
        /// <exception cref="MissingReferenceException">The data component no longer exists.</exception>
        public void SetComponentData<T>(T value) where T : unmanaged, IComponentData
        {
            var id = GetDataComponentIdOfType<T>();
            ThrowIfDataComponentWorldIsHeld(id);
            Entity owner = GetExistingDataComponentOwner<T>(id);
            if (!TypeManager.IsZeroSized(TypeManager.GetTypeIndex<T>()))
                GlobalEntityManager.SetComponentData(owner, value);
        }

        EntityId GetDataComponentIdOfType<T>() where T : unmanaged, IComponentData
        {
            var id = GetEntityId();
            if (!DataComponentWrapperStore.IsDataComponentId(id))
                throw new InvalidOperationException($"{GetType().FullName} is not a data component.");

            int typeId = TypeManagerV2.GetComponentTypeId(typeof(T).TypeHandle, false);
            if ((uint)typeId != id.TypeId)
                throw new ArgumentException($"The data component with EntityId {id} is not of type {typeof(T).FullName}.");
            return id;
        }

        static Entity GetExistingDataComponentOwner<T>(EntityId id) where T : unmanaged, IComponentData
        {
            if (!DoesDataComponentExist(id))
                throw new MissingReferenceException($"The data component {typeof(T).FullName} with EntityId {id} does not exist.");
            return DataComponentWrapperStore.GetOwnerId(id);
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public Component GetComponent(Type type)
        {
            return gameObject.GetComponent(type);
        }

        [FreeFunction(HasExplicitThis = true, ThrowsException = true)]
        internal extern Component GetComponentFastPath(System.Type type);

        [System.Security.SecuritySafeCritical]
        public unsafe T GetComponent<T>()
        {
            if (IsDataComponentWrapper())
                return gameObject.GetComponent<T>();
            var component = GetComponentFastPath(typeof(T));
            // Because there is no constraint on T, a user could pass a value type that is larger than a reference
            // If so we need to ensure that the ref we pass to UnsafeUtility.As is larger enough so we don't read
            // random stack data. In that case we can assume that component will null, so we will return a zero'd T
            var tuple = (component, default(T));
            return UnsafeUtility.As<Component, T>(ref tuple.component);
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public bool TryGetComponent(Type type, out Component component)
        {
            return gameObject.TryGetComponent(type, out component);
        }

        [System.Security.SecuritySafeCritical]
        public unsafe bool TryGetComponent<T>(out T component)
        {
            return gameObject.TryGetComponent(out component);
        }

        public Component GetComponent(string type)
        {
            if (IsDataComponentWrapper())
                return gameObject.GetComponent(type);
            return GetComponentByName(type);
        }

        [FreeFunction("GetComponent", HasExplicitThis = true)]
        extern Component GetComponentByName(string type);

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
        public Component GetComponentInParent(Type t, bool includeInactive)
        {
            return gameObject.GetComponentInParent(t, includeInactive);
        }

        [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
        public Component GetComponentInParent(Type t)
        {
            return gameObject.GetComponentInParent(t, false);
        }

        public T GetComponentInParent<T>([DefaultValue("false")] bool includeInactive)
        {
            return (T)(object)GetComponentInParent(typeof(T), includeInactive);
        }

        public T GetComponentInParent<T>()
        {
            return (T)(object)GetComponentInParent(typeof(T), false);
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

        // T may be an interface; native code fills the list as a List<Component>.
        [FreeFunction(HasExplicitThis = true, ThrowsException = true)]
        private extern void GetComponentsForListInternal<T>(Type searchType,
            [Out][UnityMarshalAs(NativeType.MarshalAsType, MarshalAsType = typeof(List<Component>))] List<T> resultList);

        public void GetComponents(Type type, List<Component> results)
        {
            if (IsDataComponentWrapper())
                gameObject.GetComponents(type, results);
            else
                GetComponentsForListInternal<Component>(type, results);
        }

        public void GetComponents<T>(List<T> results)
        {
            if (IsDataComponentWrapper())
                gameObject.GetComponents(results);
            else
                GetComponentsForListInternal(typeof(T), results);
        }

        public string tag
        {
            get => gameObject.tag;
            set => gameObject.tag = value;
        }

        public T[] GetComponents<T>()
        {
            return gameObject.GetComponents<T>();
        }

        public extern int GetComponentIndex();

        public bool CompareTag(string tag)
        {
            return gameObject.CompareTag(tag);
        }

        public bool CompareTag(TagHandle tag)
        {
            return gameObject.CompareTag(tag);
        }

        [FreeFunction(HasExplicitThis = true)]
        internal extern Component GetCoupledComponent();

        [FreeFunction(HasExplicitThis = true)]
        internal extern bool IsCoupledComponent();

        public void SendMessageUpwards(string methodName, [DefaultValue("null")] object value, [DefaultValue("SendMessageOptions.RequireReceiver")] SendMessageOptions options)
        {
            if (IsDataComponentWrapper())
                gameObject.SendMessageUpwards(methodName, value, options);
            else
                SendMessageUpwardsInternal(methodName, value, options);
        }

        [FreeFunction("SendMessageUpwards", HasExplicitThis = true)]
        extern void SendMessageUpwardsInternal(string methodName, object value, SendMessageOptions options);

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

        public void SendMessage(string methodName, object value, SendMessageOptions options)
        {
            if (IsDataComponentWrapper())
                gameObject.SendMessage(methodName, value, options);
            else
                SendMessageInternal(methodName, value, options);
        }

        [FreeFunction("SendMessage", HasExplicitThis = true)]
        extern void SendMessageInternal(string methodName, object value, SendMessageOptions options);

        public void SendMessage(string methodName, SendMessageOptions options)
        {
            SendMessage(methodName, null, options);
        }

        public void BroadcastMessage(string methodName, [DefaultValue("null")] object parameter, [DefaultValue("SendMessageOptions.RequireReceiver")] SendMessageOptions options)
        {
            if (IsDataComponentWrapper())
                gameObject.BroadcastMessage(methodName, parameter, options);
            else
                BroadcastMessageInternal(methodName, parameter, options);
        }

        [FreeFunction("BroadcastMessage", HasExplicitThis = true)]
        extern void BroadcastMessageInternal(string methodName, object parameter, SendMessageOptions options);

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
