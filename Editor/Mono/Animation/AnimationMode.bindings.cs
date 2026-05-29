// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Playables;
using UnityEngine;

using Object = UnityEngine.Object;

namespace UnityEditor
{
    public class AnimationModeDriver : ScriptableObject
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

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        static internal event Action onAnimationRecordingStart;
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        static internal event Action onAnimationRecordingStop;
        static internal event Action onAnimationPlaybackStart;
        static internal event Action onAnimationPlaybackStop;
        static internal event Action onAnimationSampleEnd;
        static internal event Action onAnimationModeStop;

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
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        extern internal static bool IsPropertyCandidate(Object target, string propertyPath);


        // Stops animation mode, as used by the animation editor.
        public static void StopAnimationMode()
        {
            Internal_StopAnimationMode(DummyDriver());

            onAnimationModeStop?.Invoke();
        }

        // Stops animation mode, as used by the animation editor.
        public static void StopAnimationMode(AnimationModeDriver driver)
        {
            Internal_StopAnimationMode(driver);

            onAnimationModeStop?.Invoke();
        }

        // Returns true if the editor is currently in animation mode.
        public static bool InAnimationMode()
        {
            return Internal_InAnimationModeNoDriver();
        }

        // Returns true if the editor is currently in animation mode.
        public static bool InAnimationMode(AnimationModeDriver driver)
        {
            return Internal_InAnimationMode(driver);
        }

        // Starts animation mode, as used by the animation editor.
        public static void StartAnimationMode()
        {
            Internal_StartAnimationMode(DummyDriver());
        }

        // Starts animation mode, as used by the animation editor.
        public static void StartAnimationMode(AnimationModeDriver driver)
        {
            Internal_StartAnimationMode(driver);
        }

        // Stops animation playback mode, as used by the animation editor.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static void StopAnimationPlaybackMode()
        {
            s_InAnimationPlaybackMode = false;

            onAnimationPlaybackStop?.Invoke();
        }

        // Returns true if the editor is currently in animation playback mode.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static bool InAnimationPlaybackMode()
        {
            return s_InAnimationPlaybackMode;
        }

        // Starts animation mode, as used by the animation editor playback mode.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static void StartAnimationPlaybackMode()
        {
            s_InAnimationPlaybackMode = true;
            onAnimationPlaybackStart?.Invoke();
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static void StopAnimationRecording()
        {
            s_InAnimationRecordMode = false;

            onAnimationRecordingStop?.Invoke();
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static bool InAnimationRecording()
        {
            return s_InAnimationRecordMode;
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static void StartAnimationRecording()
        {
            s_InAnimationRecordMode = true;

            onAnimationRecordingStart?.Invoke();
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static void StartCandidateRecording(AnimationModeDriver driver)
        {
            Internal_StartCandidateRecording(driver);
        }

        public static void BeginSampling()
        {
            Internal_BeginSampling();
        }

        public static void EndSampling()
        {
            Internal_EndSampling();

            onAnimationSampleEnd?.Invoke();
        }

        [NativeMethod(ThrowsException = true)]
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        extern internal static void AddCandidate(EditorCurveBinding binding, PropertyModification modification, bool keepPrefabOverride);

        [NativeMethod(ThrowsException = true)]
        extern internal static void AddCandidates([NotNull] GameObject gameObject, [NotNull] AnimationClip clip);

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        extern internal static void StopCandidateRecording();

        extern internal static bool IsRecordingCandidates();

        [NativeMethod(ThrowsException = true)]
        extern public static void SampleAnimationClip([NotNull] GameObject gameObject, [NotNull] AnimationClip clip, float time);

        [NativeMethod(ThrowsException = true)]
        extern internal static void SampleCandidateClip([NotNull] GameObject gameObject, [NotNull] AnimationClip clip, float time);

        [NativeMethod(ThrowsException = true)]
        extern public static void SamplePlayableGraph(PlayableGraph graph, int index, float time);

        [NativeMethod(ThrowsException = true)]
        extern public static void AddPropertyModification(EditorCurveBinding binding, PropertyModification modification, bool keepPrefabOverride);

        [NativeMethod(ThrowsException = true)]
        extern public static void AddEditorCurveBinding([NotNull] GameObject gameObject, EditorCurveBinding binding);

        [NativeMethod(ThrowsException = true)]
        extern internal static void AddTransformTR([NotNull] GameObject root, string path);

        [NativeMethod(ThrowsException = true)]
        extern internal static void AddTransformTRS([NotNull] GameObject root, string path);

        [NativeMethod(ThrowsException = true)]
        extern internal static void InitializePropertyModificationForGameObject([NotNull] GameObject gameObject, [NotNull] AnimationClip clip);

        [NativeMethod(ThrowsException = true)]
        extern internal static void InitializePropertyModificationForObject([NotNull] Object target, [NotNull] AnimationClip clip);

        [NativeMethod(ThrowsException = true)]
        extern internal static void RevertPropertyModificationsForGameObject([NotNull] GameObject gameObject);

        [NativeMethod(ThrowsException = true)]
        extern internal static void RevertPropertyModificationsForObject([NotNull] Object target);

        // Returns editor curve bindings for animation clip and animator hierarchy that need to be snapshot for animation mode.
        extern internal static EditorCurveBinding[] GetAllBindings([NotNull] GameObject root, [NotNull] AnimationClip clip);

        // Returns editor curve bindings for animation clip that need to be snapshot for animation mode.
        extern internal static EditorCurveBinding[] GetCurveBindings([NotNull] AnimationClip clip);

        // Return editor curve bindings for animator hierarhcy that need to be snapshot for animation mode.
        extern internal static EditorCurveBinding[] GetAnimatorBindings([NotNull] GameObject root);

        [NativeMethod(ThrowsException = true)]
        extern private static void Internal_BeginSampling();

        [NativeMethod(ThrowsException = true)]
        extern private static void Internal_EndSampling();

        extern private static void Internal_StartAnimationMode(Object driver);

        extern private static void Internal_StopAnimationMode(Object driver);

        extern private static bool Internal_InAnimationMode(Object driver);

        extern private static bool Internal_InAnimationModeNoDriver();

        [NativeMethod(ThrowsException = true)]
        extern private static void Internal_StartCandidateRecording(Object driver);
    }
}
