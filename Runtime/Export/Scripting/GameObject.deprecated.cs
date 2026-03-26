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
        [Obsolete("GameObject.active is obsolete. Use GameObject.SetActive(), GameObject.activeSelf or GameObject.activeInHierarchy.", true)]
        public extern bool active
        {
            [NativeMethod(Name = "IsActive")]
            get;
            [NativeMethod(Name = "SetSelfActive")]
            set;
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use SetActive() instead. Note: SetActive() respects explicitly inactive children, while SetActiveRecursively forced all descendants active.", true)]
        [NativeMethod(Name = "SetActiveRecursivelyDeprecated")]
        public extern void SetActiveRecursively(bool state);

        [Obsolete("Obsolete. Please use GameObject.GetScene(EntityId entityId) instead.", true)]
        public static Scene GetScene(int instanceID) => GetSceneInternal((EntityId)instanceID);
    }
}
