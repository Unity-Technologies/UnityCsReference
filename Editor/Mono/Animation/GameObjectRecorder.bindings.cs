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
    [NativeHeader("Editor/Src/Animation/EditorCurveBinding.bindings.h")]
    [NativeHeader("Editor/Src/Animation/GameObjectRecorder.h")]
    [NativeHeader("Runtime/Animation/AnimationClip.h")]
    [NativeType]
    public class GameObjectRecorder : Object
    {
        [Obsolete("The GameObjectRecorder constructor now takes a root GameObject", true)]
        public GameObjectRecorder()
        {
        }

        public GameObjectRecorder(GameObject root)
        {
            Internal_Create(this, root);
        }

        extern private static void Internal_Create([Writable] GameObjectRecorder notSelf, [NotNull] GameObject root);

        // Root.
        extern public GameObject root { get; }

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
