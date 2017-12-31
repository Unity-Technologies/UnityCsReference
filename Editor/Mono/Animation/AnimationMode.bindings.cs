// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class AnimationModeDriver : ScriptableObject
    {
        internal delegate bool IsKeyCallback(Object target, string propertyPath);

        internal IsKeyCallback isKeyCallback;

        [UsedByNativeCode]
        internal bool InvokeIsKeyCallback_Internal(Object target, string propertyPath)
        {
            if (isKeyCallback == null)
                return false;

            return isKeyCallback(target, propertyPath);
        }
    }

    [NativeHeader("Editor/Src/Animation/AnimationMode.bindings.h")]
    [NativeHeader("Editor/Src/Animation/EditorCurveBinding.bindings.h")]
    [NativeHeader("Editor/Src/Prefabs/PropertyModification.h")]
    public class AnimationMode
    {
        static private bool s_InAnimationPlaybackMode = false;
        static private bool s_InAnimationRecordMode = false;

        static private PrefColor s_AnimatedPropertyColor = new PrefColor("Animation/Property Animated", 0.82f, 0.97f, 1.00f, 1.00f, 0.54f, 0.85f, 1.00f, 1.00f);
        static private PrefColor s_RecordedPropertyColor = new PrefColor("Animation/Property Recorded", 1.00f, 0.60f, 0.60f, 1.00f, 1.00f, 0.50f, 0.50f, 1.00f);
        static private PrefColor s_CandidatePropertyColor = new PrefColor("Animation/Property Candidate", 1.00f, 0.70f, 0.60f, 1.00f, 1.00f, 0.67f, 0.43f, 1.00f);

        static public Color animatedPropertyColor { get { return s_AnimatedPropertyColor; } }
        static public Color recordedPropertyColor { get { return s_RecordedPropertyColor; } }
        static public Color candidatePropertyColor { get { return s_CandidatePropertyColor; } }

        static private AnimationModeDriver s_DummyDriver;
        static private AnimationModeDriver DummyDriver()
        {
            if (s_DummyDriver == null)
            {
                s_DummyDriver = ScriptableObject.CreateInstance<AnimationModeDriver>();
                s_DummyDriver.name = "DummyDriver";
            }
            return s_DummyDriver;
        }

        extern public static bool IsPropertyAnimated(Object target, string propertyPath);
        extern internal static bool IsPropertyCandidate(Object target, string propertyPath);

        // Stops animation mode, as used by the animation editor.
        public static void StopAnimationMode()
        {
            StopAnimationMode(DummyDriver());
        }

        // Stops animation mode, as used by the animation editor.
        extern internal static void StopAnimationMode(Object driver);

        // Returns true if the editor is currently in animation mode.
        public static bool InAnimationMode()
        {
            return Internal_InAnimationModeNoDriver();
        }

        // Returns true if the editor is currently in animation mode.
        internal static bool InAnimationMode(Object driver)
        {
            return Internal_InAnimationMode(driver);
        }

        // Starts animation mode, as used by the animation editor.
        public static void StartAnimationMode()
        {
            StartAnimationMode(DummyDriver());
        }

        // Starts animation mode, as used by the animation editor.
        extern internal static void StartAnimationMode(Object driver);

        // Stops animation playback mode, as used by the animation editor.
        internal static void StopAnimationPlaybackMode()
        {
            s_InAnimationPlaybackMode = false;
        }

        // Returns true if the editor is currently in animation playback mode.
        internal static bool InAnimationPlaybackMode()
        {
            return s_InAnimationPlaybackMode;
        }

        // Starts animation mode, as used by the animation editor playback mode.
        internal static void StartAnimationPlaybackMode()
        {
            s_InAnimationPlaybackMode = true;
        }

        internal static void StopAnimationRecording()
        {
            s_InAnimationRecordMode = false;
        }

        internal static bool InAnimationRecording()
        {
            return s_InAnimationRecordMode;
        }

        internal static void StartAnimationRecording()
        {
            s_InAnimationRecordMode = true;
        }

        [NativeThrows]
        extern internal static void StartCandidateRecording(Object driver);

        [NativeThrows]
        extern internal static void AddCandidate(EditorCurveBinding binding, PropertyModification modification, bool keepPrefabOverride);

        extern internal static void StopCandidateRecording();

        extern internal static bool IsRecordingCandidates();

        [NativeThrows]
        extern public static void BeginSampling();

        [NativeThrows]
        extern public static void EndSampling();

        [NativeThrows]
        extern public static void SampleAnimationClip(GameObject gameObject, AnimationClip clip, float time);

        [NativeThrows]
        extern internal static void SampleCandidateClip(GameObject gameObject, AnimationClip clip, float time);

        [NativeThrows]
        extern public static void AddPropertyModification(EditorCurveBinding binding, PropertyModification modification, bool keepPrefabOverride);

        [NativeThrows]
        extern internal static void InitializePropertyModificationForGameObject(GameObject gameObject, AnimationClip clip);

        [NativeThrows]
        extern internal static void InitializePropertyModificationForObject(Object target, AnimationClip clip);

        [NativeThrows]
        extern internal static void RevertPropertyModificationsForGameObject(GameObject gameObject);

        [NativeThrows]
        extern internal static void RevertPropertyModificationsForObject(Object target);

        extern private static bool Internal_InAnimationMode(Object driver);

        extern private static bool Internal_InAnimationModeNoDriver();
    }
}
