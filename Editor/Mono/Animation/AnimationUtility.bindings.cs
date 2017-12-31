// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using UnityEngine.Internal;

using Object = UnityEngine.Object;

namespace UnityEditor
{
    public struct ObjectReferenceKeyframe
    {
        public float time;
        public UnityEngine.Object value;
    }

    // An AnimationClipCurveData object contains all the information needed to identify a specific curve in an AnimationClip. The curve animates a specific property of a component / material attached to a game object / animated bone.
    public class AnimationClipCurveData
    {
        // The path of the game object / bone being animated.
        public string path;
        // The type of the component / material being animated.
        public Type   type;
        // The name of the property being animated.
        public string propertyName;
        // The actual animation curve.
        public AnimationCurve curve;

        // This is only used internally for deleting curves
        internal int  classID;
        internal int  scriptInstanceID;

        public AnimationClipCurveData()
        {
        }

        public AnimationClipCurveData(EditorCurveBinding binding)
        {
            path = binding.path;
            type = binding.type;
            propertyName = binding.propertyName;
            curve = null;
            classID = binding.m_ClassID;
            scriptInstanceID = binding.m_ScriptInstanceID;
        }
    }

    [NativeHeader("Editor/Src/Animation/AnimationUtility.bindings.h")]
    public partial class AnimationUtility
    {
        public enum CurveModifiedType
        {
            CurveDeleted = 0,
            CurveModified = 1,
            ClipModified = 2
        }

        public enum TangentMode
        {
            Free = 0,
            Auto = 1,
            Linear = 2,
            Constant = 3,
            ClampedAuto = 4
        }

        internal enum PolynomialValid
        {
            Valid = 0,
            InvalidPreWrapMode = 1,
            InvalidPostWrapMode = 2,
            TooManySegments = 3
        }

        public delegate void OnCurveWasModified(AnimationClip clip, EditorCurveBinding binding, CurveModifiedType type);
        public static OnCurveWasModified onCurveWasModified;

        [RequiredByNativeCode]
        private static void Internal_CallOnCurveWasModified(AnimationClip clip, EditorCurveBinding binding, CurveModifiedType type)
        {
            if (onCurveWasModified != null)
                onCurveWasModified(clip, binding, type);
        }

        [RequiredByNativeCode]
        private static void Internal_CallAnimationClipAwake(AnimationClip clip)
        {
            if (onCurveWasModified != null)
                onCurveWasModified(clip, new EditorCurveBinding(), CurveModifiedType.ClipModified);
        }

        [Obsolete("GetAnimationClips(Animation) is deprecated. Use GetAnimationClips(GameObject) instead.")]
        public static AnimationClip[] GetAnimationClips(Animation component)
        {
            return GetAnimationClips(component.gameObject);
        }

        // Returns the array of AnimationClips that are referenced in the Animation component
        extern public static AnimationClip[] GetAnimationClips([NotNull] GameObject gameObject);


        // Sets the array of AnimationClips to be referenced in the Animation component
        extern public static void SetAnimationClips([NotNull] Animation animation, AnimationClip[] clips);

        public static EditorCurveBinding[] GetAnimatableBindings(GameObject targetObject, GameObject root)
        {
            return Internal_GetGameObjectAnimatableBindings(targetObject, root);
        }

        internal static EditorCurveBinding[] GetAnimatableBindings(ScriptableObject scriptableObject)
        {
            return Internal_GetScriptableObjectAnimatableBindings(scriptableObject);
        }

        extern private static EditorCurveBinding[] Internal_GetGameObjectAnimatableBindings([NotNull] GameObject targetObject, [NotNull] GameObject root);
        extern private static EditorCurveBinding[] Internal_GetScriptableObjectAnimatableBindings([NotNull] ScriptableObject scriptableObject);

        // Binds the property and returns the type of the bound value (Can be used to display special UI for it and to enforce correct drag and drop)
        // null if it can't be bound.
        public static System.Type GetEditorCurveValueType(GameObject root, EditorCurveBinding binding)
        {
            return Internal_GetGameObjectEditorCurveValueType(root, binding);
        }

        internal static System.Type GetEditorCurveValueType(ScriptableObject scriptableObject, EditorCurveBinding binding)
        {
            return Internal_GetScriptableObjectEditorCurveValueType(scriptableObject, binding);
        }

        extern private static System.Type Internal_GetGameObjectEditorCurveValueType([NotNull] GameObject root, EditorCurveBinding binding);
        extern private static System.Type Internal_GetScriptableObjectEditorCurveValueType([NotNull] ScriptableObject scriptableObject, EditorCurveBinding binding);

        extern public static bool GetFloatValue([NotNull] GameObject root, EditorCurveBinding binding, out float data);
        //extern public static bool GetObjectReferenceValue([NotNull]GameObject root, [NotNull]EditorCurveBinding binding, out Object data);

        extern public static Object GetAnimatedObject([NotNull] GameObject root, EditorCurveBinding binding);

        //extern public static Type PropertyModificationToEditorCurveBinding(PropertyModification modification, [NotNull]GameObject gameObject, out EditorCurveBinding binding);
        extern internal static PropertyModification EditorCurveBindingToPropertyModification(EditorCurveBinding binding, [NotNull] GameObject gameObject);

        extern public static EditorCurveBinding[] GetCurveBindings([NotNull] AnimationClip clip);
        extern public static EditorCurveBinding[] GetObjectReferenceCurveBindings([NotNull] AnimationClip clip);

        extern public static ObjectReferenceKeyframe[] GetObjectReferenceCurve([NotNull] AnimationClip clip, EditorCurveBinding binding);
        extern public static void SetObjectReferenceCurve([NotNull] AnimationClip clip, EditorCurveBinding binding, ObjectReferenceKeyframe[] keyframes);

        extern public static AnimationCurve GetEditorCurve([NotNull] AnimationClip clip, EditorCurveBinding binding);
        extern public static void SetEditorCurve([NotNull] AnimationClip clip, EditorCurveBinding binding, AnimationCurve curve);

        // Todo. Should be converted to c++.
        internal static void SetEditorCurves(AnimationClip clip, EditorCurveBinding[] bindings, AnimationCurve[] curves)
        {
            if (clip == null)
                throw new ArgumentNullException("clip");
            if (curves == null)
                throw new ArgumentNullException("curves");
            if (bindings == null)
                throw new ArgumentNullException("bindings");

            if (bindings.Length != curves.Length)
                throw new ArgumentException("bindings and curves array sizes do not match");

            for (int i = 0; i < bindings.Length; ++i)
            {
                Internal_SetEditorCurve(clip, bindings[i], curves[i], false);

                if (onCurveWasModified != null)
                    onCurveWasModified(clip, bindings[i], curves[i] != null ? CurveModifiedType.CurveModified : CurveModifiedType.CurveDeleted);
            }

            Internal_SyncEditorCurves(clip);
        }

        extern private static void Internal_SetEditorCurve(AnimationClip clip, EditorCurveBinding binding, AnimationCurve curve, bool syncEditorCurve);
        extern private static void Internal_SyncEditorCurves(AnimationClip clip);

        [NativeThrows]
        extern internal static void UpdateTangentsFromModeSurrounding([NotNull] AnimationCurve curve, int index);

        extern internal static void UpdateTangentsFromMode([NotNull] AnimationCurve curve);

        [NativeThrows]
        extern public static TangentMode GetKeyLeftTangentMode([NotNull] AnimationCurve curve, int index);

        [NativeThrows]
        extern public static TangentMode GetKeyRightTangentMode([NotNull] AnimationCurve curve, int index);

        [NativeThrows]
        extern public static bool GetKeyBroken([NotNull] AnimationCurve curve, int index);

        [NativeThrows]
        extern public static void SetKeyLeftTangentMode([NotNull] AnimationCurve curve, int index, TangentMode tangentMode);

        [NativeThrows]
        extern public static void SetKeyRightTangentMode([NotNull] AnimationCurve curve, int index, TangentMode tangentMode);

        [NativeThrows]
        extern public static void SetKeyBroken([NotNull] AnimationCurve curve, int index, bool broken);

        internal static TangentMode GetKeyLeftTangentMode(Keyframe key)
        {
            return Internal_GetKeyLeftTangentMode(key);
        }

        internal static TangentMode GetKeyRightTangentMode(Keyframe key)
        {
            return Internal_GetKeyRightTangentMode(key);
        }

        internal static bool GetKeyBroken(Keyframe key)
        {
            return Internal_GetKeyBroken(key);
        }

        extern private static TangentMode Internal_GetKeyLeftTangentMode(Keyframe key);
        extern private static TangentMode Internal_GetKeyRightTangentMode(Keyframe key);
        extern private static bool Internal_GetKeyBroken(Keyframe key);

        internal static void SetKeyLeftTangentMode(ref Keyframe key, TangentMode tangentMode)
        {
            Internal_SetKeyLeftTangentMode(ref key, tangentMode);
        }

        internal static void SetKeyRightTangentMode(ref Keyframe key, TangentMode tangentMode)
        {
            Internal_SetKeyRightTangentMode(ref key, tangentMode);
        }

        internal static void SetKeyBroken(ref Keyframe key, bool broken)
        {
            Internal_SetKeyBroken(ref key, broken);
        }

        extern private static void Internal_SetKeyLeftTangentMode(ref Keyframe key, TangentMode tangentMode);
        extern private static void Internal_SetKeyRightTangentMode(ref Keyframe key, TangentMode tangentMode);
        extern private static void Internal_SetKeyBroken(ref Keyframe key, bool broken);

        extern internal static int AddInbetweenKey(AnimationCurve curve, float time);

        [Obsolete("GetAllCurves is deprecated. Use GetCurveBindings and GetObjectReferenceCurveBindings instead.")]
        public static AnimationClipCurveData[] GetAllCurves(AnimationClip clip)
        {
            bool includeCurveData = true;
            return GetAllCurves(clip, includeCurveData);
        }

        [Obsolete("GetAllCurves is deprecated. Use GetCurveBindings and GetObjectReferenceCurveBindings instead.")]
        public static AnimationClipCurveData[] GetAllCurves(AnimationClip clip, [DefaultValue("true")] bool includeCurveData)
        {
            EditorCurveBinding[] bindings = GetCurveBindings(clip);

            AnimationClipCurveData[] curves = new AnimationClipCurveData[bindings.Length];
            for (int i = 0; i < curves.Length; i++)
            {
                curves[i] = new AnimationClipCurveData(bindings[i]);

                if (includeCurveData)
                    curves[i].curve = GetEditorCurve(clip, bindings[i]);
            }

            return curves;
        }

        [Obsolete("This overload is deprecated. Use the one with EditorCurveBinding instead.")]
        public static bool GetFloatValue(GameObject root, string relativePath, Type type, string propertyName, out float data)
        {
            return GetFloatValue(root, EditorCurveBinding.FloatCurve(relativePath, type, propertyName), out data);
        }

        [Obsolete("This overload is deprecated. Use the one with EditorCurveBinding instead.")]
        public static void SetEditorCurve(AnimationClip clip, string relativePath, Type type, string propertyName, AnimationCurve curve)
        {
            SetEditorCurve(clip, EditorCurveBinding.FloatCurve(relativePath, type, propertyName), curve);
        }

        [Obsolete("This overload is deprecated. Use the one with EditorCurveBinding instead.")]
        public static AnimationCurve GetEditorCurve(AnimationClip clip, string relativePath, Type type, string propertyName)
        {
            return GetEditorCurve(clip, EditorCurveBinding.FloatCurve(relativePath, type, propertyName));
        }

        extern public static AnimationEvent[] GetAnimationEvents([NotNull] AnimationClip clip);
        extern public static void SetAnimationEvents([NotNull] AnimationClip clip, [NotNull] AnimationEvent[] events);

        extern public static string CalculateTransformPath([NotNull] Transform targetTransform, Transform root);

        extern public static AnimationClipSettings GetAnimationClipSettings([NotNull] AnimationClip clip);
        extern public static void SetAnimationClipSettings([NotNull] AnimationClip clip, AnimationClipSettings srcClipInfo);
        extern internal static void SetAnimationClipSettingsNoDirty([NotNull] AnimationClip clip, AnimationClipSettings srcClipInfo);

        extern public static void SetAdditiveReferencePose(AnimationClip clip, AnimationClip referenceClip, float time);

        extern internal static bool IsValidOptimizedPolynomialCurve(AnimationCurve curve);
        extern public static void ConstrainToPolynomialCurve(AnimationCurve curve);
        extern internal static int GetMaxNumPolynomialSegmentsSupported();
        extern internal static PolynomialValid IsValidPolynomialCurve(AnimationCurve curve);

        extern internal static AnimationClipStats GetAnimationClipStats(AnimationClip clip);

        extern public static bool GetGenerateMotionCurves(AnimationClip clip);
        extern public static void SetGenerateMotionCurves(AnimationClip clip, bool value);
        extern internal static bool HasGenericRootTransform(AnimationClip clip);
        extern internal static bool HasMotionFloatCurves(AnimationClip clip);
        extern internal static bool HasMotionCurves(AnimationClip clip);
        extern internal static bool HasRootCurves(AnimationClip clip);

        extern internal static bool AmbiguousBinding(string path, int classID, Transform root);

        extern internal static Vector3 GetClosestEuler(Quaternion q, Vector3 eulerHint, RotationOrder rotationOrder);

        [Obsolete("Use AnimationMode.InAnimationMode instead.")]
        static public bool InAnimationMode()
        {
            return AnimationMode.InAnimationMode();
        }

        [Obsolete("Use AnimationMode.StartAnimationmode instead.")]
        public static void StartAnimationMode(Object[] objects)
        {
            Debug.LogWarning("AnimationUtility.StartAnimationMode is deprecated. Use AnimationMode.StartAnimationMode with the new APIs. The objects passed to this function will no longer be reverted automatically. See AnimationMode.AddPropertyModification");
            AnimationMode.StartAnimationMode();
        }

        [Obsolete("Use AnimationMode.StopAnimationMode instead.")]
        public static void StopAnimationMode()
        {
            AnimationMode.StopAnimationMode();
        }

        [Obsolete("SetAnimationType is no longer supported.")]
        public static void SetAnimationType(AnimationClip clip, ModelImporterAnimationType type) {}
    }
}
