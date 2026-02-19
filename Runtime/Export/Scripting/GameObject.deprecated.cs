// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.SceneManagement;

namespace UnityEngine
{
    public partial class GameObject
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Property rigidbody has been deprecated. Use GetComponent<Rigidbody>() instead. (UnityUpgradable)", true)]
        public Component rigidbody
        {
            get { throw new NotSupportedException("rigidbody property has been deprecated"); }
        }
        
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("GameObject.active is obsolete. Use GameObject.SetActive(), GameObject.activeSelf or GameObject.activeInHierarchy.", true)]
        public extern bool active
        {
            [NativeMethod(Name = "IsActive")]
            get;
            [NativeMethod(Name = "SetSelfActive")]
            set;
        }
        
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("gameObject.SetActiveRecursively() is obsolete. Use GameObject.SetActive(), which is now inherited by children.", false)]
        [NativeMethod(Name = "SetActiveRecursivelyDeprecated")]
        public extern void SetActiveRecursively(bool state);
        
        [Obsolete("Obsolete. Please use GameObject.SetGameObjectsActive(NativeArray<EntityId>, bool) instead.", false)]
        public static unsafe void SetGameObjectsActive(NativeArray<int> instanceIDs, bool active)
        {
            if (!instanceIDs.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(instanceIDs));

            if (instanceIDs.Length == 0)
                return;

            SetGameObjectsActive((IntPtr)instanceIDs.GetUnsafeReadOnlyPtr(), instanceIDs.Length, active);
        }    
        
        [Obsolete("Obsolete. Please use GameObject.SetGameObjectsActive(ReadOnlySpan<EntityId>, bool) instead.", false)]
        public static unsafe void SetGameObjectsActive(ReadOnlySpan<int> instanceIDs, bool active)
        {
            if (instanceIDs.Length == 0)
                return;

            fixed (int* instanceIDsPtr = instanceIDs)
            {
                SetGameObjectsActive((IntPtr)instanceIDsPtr, instanceIDs.Length, active);
            }
        }
        
        [Obsolete("Obsolete. Please use GameObject.InstantiateGameObjects(EntityId, int, NativeArray<EntityId>, NativeArray<EntityId>, Scene) instead.", false)]
        public static unsafe void InstantiateGameObjects(int sourceInstanceID, int count, NativeArray<int> newInstanceIDs, NativeArray<int> newTransformInstanceIDs, Scene destinationScene = default)
        {
            if (!newInstanceIDs.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(newInstanceIDs));
            if (!newTransformInstanceIDs.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(newTransformInstanceIDs));
            if (count == 0)
                return;
            if ((count != newInstanceIDs.Length) || (count != newTransformInstanceIDs.Length))
                throw new ArgumentException("Size mismatch! Both arrays must already be the size of count.");

            InstantiateGameObjects(sourceInstanceID, (IntPtr)newInstanceIDs.GetUnsafeReadOnlyPtr(), (IntPtr)newTransformInstanceIDs.GetUnsafeReadOnlyPtr(), newInstanceIDs.Length, destinationScene);
        }   
        
        [Obsolete("Obsolete. Please use GameObject.GetScene(EntityId entityId) instead.", false)]
        public static Scene GetScene(int instanceID) => GetSceneInternal((EntityId)instanceID);        
    }
}
