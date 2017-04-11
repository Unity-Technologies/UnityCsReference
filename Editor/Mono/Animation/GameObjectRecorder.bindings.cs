// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEditor;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Animations
{
    [NativeHeader("Runtime/Animation/EditorCurveBinding.bindings.h")]
    [NativeHeader("Runtime/Animation/AnimationClip.h")]
    [NativeHeader("Editor/Src/Animation/GameObjectRecorder.h")]
    [NativeType]
    public class GameObjectRecorder : Object
    {
        public GameObjectRecorder()
        {
            Internal_Create(this);
        }

        extern private static void Internal_Create([Writable] GameObjectRecorder notSelf);

        // Root.
        extern public GameObject root { set; get; }

        // Bindings.
        extern public void Bind(EditorCurveBinding binding);
        extern public void BindAll(GameObject target, bool recursive);
        extern public void BindComponent(GameObject target, Type componentType, bool recursive);

        public void BindComponent<T>(GameObject target, bool recursive) where T : Component
        {
            BindComponent(target, typeof(T), recursive);
        }

        extern public EditorCurveBinding[] GetBindings();

        // Recording.
        extern public void TakeSnapshot(float dt);
        extern public float currentTime { get; }
        extern public bool isRecording { get; }

        extern public void SaveToClip(AnimationClip clip);
        extern public void ResetRecording();
    }
}
