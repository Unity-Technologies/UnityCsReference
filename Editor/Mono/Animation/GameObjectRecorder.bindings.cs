// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Bindings;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Animations
{
    [NativeInclude(Header = "Runtime/Animation/EditorCurveBinding.bindings.h")]
    [NativeInclude(Header = "Runtime/Animation/AnimationClip.h")]
    [NativeInclude(Header = "Editor/Src/Animation/GameObjectRecorder.h")]
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
        extern public void BindAllProperties(GameObject target, bool recursive);
        extern public void BindTransform(GameObject target, bool recursive);
        extern public EditorCurveBinding[] GetBindings();

        // Recording.
        extern public void TakeSnapshot(float dt);
        extern public float currentTime { get; }
        extern public bool isRecording { get; }

        extern public void SaveToClip(AnimationClip clip);
        extern public void ResetRecording();
    }
}
