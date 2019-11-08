// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEditor;
using Object = UnityEngine.Object;
using uei = UnityEngine.Internal;

namespace UnityEditor.Animations
{
    public struct CurveFilterOptions
    {
        public float positionError;
        public float rotationError;
        public float scaleError;
        public float floatError;
        public bool keyframeReduction;
        public bool unrollRotation;
    }

    [NativeHeader("Editor/Src/Animation/EditorCurveBinding.bindings.h")]
    [NativeHeader("Editor/Src/Animation/GameObjectRecorder.h")]
    [NativeHeader("Modules/Animation/AnimationClip.h")]
    [NativeType]
    public class GameObjectRecorder : Object
    {
        readonly static CurveFilterOptions k_DefaultCurveFilterOptions = new CurveFilterOptions()
        {
            unrollRotation = true,
            keyframeReduction = true,
            positionError = 0.5f,
            rotationError = 0.5f,
            scaleError = 0.5f,
            floatError = 0.5f
        };

        public GameObjectRecorder(GameObject root)
        {
            Internal_Create(this, root);
        }

        public void BindComponentsOfType<T>(GameObject target, bool recursive)
            where T : Component
        {
            BindComponentsOfType(target, typeof(T), recursive);
        }

        public void BindComponentsOfType(GameObject target, Type componentType, bool recursive)
        {
            Component[] components;
            if (recursive)
                components = target.GetComponentsInChildren(componentType, true);
            else
                components = target.GetComponents(componentType);

            for (int i = 0; i < components.Length; ++i)
                BindComponent(components[i]);
        }

        extern private static void Internal_Create([Writable] GameObjectRecorder self, [NotNull] GameObject root);

        // Root.
        extern public GameObject root { get; }

        // Bindings.
        public void Bind(EditorCurveBinding binding)
        {
            if (!binding.type.IsSubclassOf(typeof(UnityEngine.Object)))
                throw new InvalidCastException("Binding type should derive from Unity type.");

            BindInternal(binding);
        }

        [NativeMethod("Bind")]
        extern private void BindInternal(EditorCurveBinding binding);

        extern public void BindAll(GameObject target, bool recursive);
        extern public void BindComponent([NotNull] Component component);

        extern public EditorCurveBinding[] GetBindings();

        // Recording.
        extern public void TakeSnapshot(float dt);
        extern public float currentTime { get; }
        extern public bool isRecording { get; }

        public void SaveToClip(AnimationClip clip)
        {
            SaveToClip(clip, 60.0f);
        }

        public void SaveToClip(AnimationClip clip, float fps)
        {
            if (fps <= Mathf.Epsilon)
                throw new ArgumentException("FPS can't be 0.0 or less");

            if (!isRecording)
                throw new InvalidOperationException("Cannot save to clip as there is nothing to save. The method TakeSnapshot() has not been called.");

            SaveToClipInternal(clip, fps, k_DefaultCurveFilterOptions);

            AnimationUtility.onCurveWasModified?.Invoke(clip, new EditorCurveBinding(), AnimationUtility.CurveModifiedType.ClipModified);
        }

        public void SaveToClip(AnimationClip clip, float fps, CurveFilterOptions filterOptions)
        {
            if (fps <= Mathf.Epsilon)
                throw new ArgumentException("FPS can't be 0.0 or less");

            if (filterOptions.keyframeReduction)
            {
                if (filterOptions.positionError < 0 || filterOptions.rotationError < 0 || filterOptions.scaleError < 0 || filterOptions.floatError < 0)
                    throw new ArgumentException("Allowed errors for keyframe reduction cannot be negative.");
            }

            if (!isRecording)
                throw new InvalidOperationException("Cannot save to clip as there is nothing to save. The method TakeSnapshot() has not been called.");

            SaveToClipInternal(clip, fps, filterOptions);

            AnimationUtility.onCurveWasModified?.Invoke(clip, new EditorCurveBinding(), AnimationUtility.CurveModifiedType.ClipModified);
        }

        [NativeMethod("SaveToClip")]
        extern void SaveToClipInternal(AnimationClip clip, float fps, CurveFilterOptions filterOptions);

        extern public void ResetRecording();

        // Obsolete
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("The GameObjectRecorder constructor now takes a root GameObject", true)]
        public GameObjectRecorder() {}

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("BindComponent() using a System::Type is obsolete, use BindComponentsOfType() instead (UnityUpgradable) -> BindComponentsOfType(*)", true)]
        public void BindComponent(GameObject target, Type componentType, bool recursive) {}

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("\"BindComponent<T>() where T : Component\" is obsolete, use BindComponentsOfType<T>() instead (UnityUpgradable) -> BindComponentsOfType<T>(*)", true)]
        public void BindComponent<T>(GameObject target, bool recursive) where T : Component {}
    }
}
