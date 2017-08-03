// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using UnityEngine.Bindings;



using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;

namespace UnityEditor
{


[StructLayout(LayoutKind.Sequential)]
public sealed partial class AnimationClipCurveData
{
    public string path;
    public Type   type;
    public string propertyName;
    public AnimationCurve curve;
    
    
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

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct ObjectReferenceKeyframe
{
    public float              time;
    public UnityEngine.Object value;
}

[NativeType(CodegenOptions.Custom, "MonoEditorCurveBinding")]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct EditorCurveBinding
{
    public string path;
    
    
    private Type   m_type;
    
    
    public string propertyName;
    
    
    private int   m_isPPtrCurve;
    
    
    private int   m_isDiscreteCurve;
    
    
    private int   m_isPhantom;
    
    
    internal int  m_ClassID;
    internal int  m_ScriptInstanceID;
    
    
    public bool  isPPtrCurve { get { return m_isPPtrCurve != 0; } }
    public bool  isDiscreteCurve { get { return m_isDiscreteCurve != 0; } }
    internal bool  isPhantom { get { return m_isPhantom != 0; } set { m_isPhantom = value == true ? 1 : 0; } }
    
            public static bool operator==(EditorCurveBinding lhs, EditorCurveBinding rhs)
        {
            if (lhs.m_ClassID != 0 && rhs.m_ClassID != 0)
            {
                if (lhs.m_ClassID != rhs.m_ClassID || lhs.m_ScriptInstanceID != rhs.m_ScriptInstanceID)
                    return false;
            }

            return lhs.m_isPPtrCurve == rhs.m_isPPtrCurve && lhs.m_isDiscreteCurve == rhs.m_isDiscreteCurve && lhs.path == rhs.path && lhs.type == rhs.type && lhs.propertyName == rhs.propertyName;
        }
    
            public static bool operator!=(EditorCurveBinding lhs, EditorCurveBinding rhs)
        {
            return !(lhs == rhs);
        }
    
    public override int GetHashCode()
        {
            return String.Format("{0}:{1}:{2}", path, type.Name, propertyName).GetHashCode();
        }
    
    public override bool Equals(object other)
        {
            if (!(other is EditorCurveBinding)) return false;

            EditorCurveBinding rhs = (EditorCurveBinding)other;
            return this == rhs;
        }
    
    
    public Type type
        {
            get { return m_type; }
            set { m_type = value; m_ClassID = 0; m_ScriptInstanceID = 0; }
        }
    
    
    
    static public EditorCurveBinding FloatCurve(string inPath, System.Type inType, string inPropertyName)
        {
            EditorCurveBinding binding = new EditorCurveBinding();

            binding.path = inPath;
            binding.type = inType;
            binding.propertyName = inPropertyName;
            binding.m_isPPtrCurve = 0;
            binding.m_isDiscreteCurve = 0;
            binding.m_isPhantom = 0;


            return binding;
        }
    
    
    static public EditorCurveBinding PPtrCurve(string inPath, System.Type inType, string inPropertyName)
        {
            EditorCurveBinding binding = new EditorCurveBinding();

            binding.path = inPath;
            binding.type = inType;
            binding.propertyName = inPropertyName;
            binding.m_isPPtrCurve = 1;
            binding.m_isDiscreteCurve = 1;
            binding.m_isPhantom = 0;
            return binding;

        }
    
    
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
internal partial struct AnimationClipStats
{
    public int size;
    public int positionCurves;
    public int quaternionCurves;
    public int eulerCurves;
    public int scaleCurves;
    public int muscleCurves;
    public int genericCurves;
    public int pptrCurves;
    public int totalCurves;
    public int constantCurves;
    public int denseCurves;
    public int streamCurves;
    
    
    public void Reset()
        {
            size = 0;
            positionCurves = 0;
            quaternionCurves = 0;
            eulerCurves = 0;
            scaleCurves = 0;
            muscleCurves = 0;
            genericCurves = 0;
            pptrCurves = 0;
            totalCurves = 0;
            constantCurves = 0;
            denseCurves = 0;
            streamCurves = 0;
        }
    
    
    public void Combine(AnimationClipStats other)
        {
            size += other.size;
            positionCurves += other.positionCurves;
            quaternionCurves += other.quaternionCurves;
            eulerCurves += other.eulerCurves;
            scaleCurves += other.scaleCurves;
            muscleCurves += other.muscleCurves;
            genericCurves += other.genericCurves;
            pptrCurves += other.pptrCurves;
            totalCurves += other.totalCurves;
            constantCurves += other.constantCurves;
            denseCurves += other.denseCurves;
            streamCurves += other.streamCurves;
        }
    
    
}

[RequiredByNativeCode]
public sealed partial class AnimationClipSettings
{
    public AnimationClip additiveReferencePoseClip;
    public float additiveReferencePoseTime;
    public float startTime;
    public float stopTime;
    public float orientationOffsetY;
    public float level;
    public float cycleOffset;
    public bool hasAdditiveReferencePose;
    public bool loopTime;
    public bool loopBlend;
    public bool loopBlendOrientation;
    public bool loopBlendPositionY;
    public bool loopBlendPositionXZ;
    public bool keepOriginalOrientation;
    public bool keepOriginalPositionY;
    public bool keepOriginalPositionXZ;
    public bool heightFromFeet;
    public bool mirror;
}

public sealed partial class AnimationUtility
{
    [System.Obsolete ("GetAnimationClips(Animation) is deprecated. Use GetAnimationClips(GameObject) instead.")]
public static AnimationClip[] GetAnimationClips(Animation component)
        {
            return GetAnimationClips(component.gameObject);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  AnimationClip[] GetAnimationClips (GameObject gameObject) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetAnimationClips (Animation animation, AnimationClip[] clips) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  EditorCurveBinding[] GetAnimatableBindings (GameObject targetObject, GameObject root) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  EditorCurveBinding[] GetScriptableObjectAnimatableBindings (ScriptableObject scriptableObject) ;

    public static bool GetFloatValue (GameObject root, EditorCurveBinding binding, out float data) {
        return INTERNAL_CALL_GetFloatValue ( root, ref binding, out data );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetFloatValue (GameObject root, ref EditorCurveBinding binding, out float data);
    public static System.Type GetEditorCurveValueType (GameObject root, EditorCurveBinding binding) {
        return INTERNAL_CALL_GetEditorCurveValueType ( root, ref binding );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static System.Type INTERNAL_CALL_GetEditorCurveValueType (GameObject root, ref EditorCurveBinding binding);
    internal static System.Type GetScriptableObjectEditorCurveValueType (ScriptableObject scriptableObject, EditorCurveBinding binding) {
        return INTERNAL_CALL_GetScriptableObjectEditorCurveValueType ( scriptableObject, ref binding );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static System.Type INTERNAL_CALL_GetScriptableObjectEditorCurveValueType (ScriptableObject scriptableObject, ref EditorCurveBinding binding);
    public static bool GetObjectReferenceValue (GameObject root, EditorCurveBinding binding, out Object targetObject) {
        return INTERNAL_CALL_GetObjectReferenceValue ( root, ref binding, out targetObject );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetObjectReferenceValue (GameObject root, ref EditorCurveBinding binding, out Object targetObject);
    public static Object GetAnimatedObject (GameObject root, EditorCurveBinding binding) {
        return INTERNAL_CALL_GetAnimatedObject ( root, ref binding );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static Object INTERNAL_CALL_GetAnimatedObject (GameObject root, ref EditorCurveBinding binding);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  Type PropertyModificationToEditorCurveBinding (PropertyModification modification, GameObject gameObject, out EditorCurveBinding binding) ;

    internal static PropertyModification EditorCurveBindingToPropertyModification (EditorCurveBinding binding, GameObject gameObject) {
        return INTERNAL_CALL_EditorCurveBindingToPropertyModification ( ref binding, gameObject );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static PropertyModification INTERNAL_CALL_EditorCurveBindingToPropertyModification (ref EditorCurveBinding binding, GameObject gameObject);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  EditorCurveBinding[] GetCurveBindings (AnimationClip clip) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  EditorCurveBinding[] GetObjectReferenceCurveBindings (AnimationClip clip) ;

    public static ObjectReferenceKeyframe[] GetObjectReferenceCurve (AnimationClip clip, EditorCurveBinding binding) {
        return INTERNAL_CALL_GetObjectReferenceCurve ( clip, ref binding );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static ObjectReferenceKeyframe[] INTERNAL_CALL_GetObjectReferenceCurve (AnimationClip clip, ref EditorCurveBinding binding);
    private static void Internal_SetObjectReferenceCurve (AnimationClip clip, EditorCurveBinding binding, ObjectReferenceKeyframe[] keyframes) {
        INTERNAL_CALL_Internal_SetObjectReferenceCurve ( clip, ref binding, keyframes );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_SetObjectReferenceCurve (AnimationClip clip, ref EditorCurveBinding binding, ObjectReferenceKeyframe[] keyframes);
    public static AnimationCurve GetEditorCurve (AnimationClip clip, EditorCurveBinding binding) {
        return INTERNAL_CALL_GetEditorCurve ( clip, ref binding );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static AnimationCurve INTERNAL_CALL_GetEditorCurve (AnimationClip clip, ref EditorCurveBinding binding);
    private static void Internal_SetEditorCurve (AnimationClip clip, EditorCurveBinding binding, AnimationCurve curve, bool syncEditorCurve) {
        INTERNAL_CALL_Internal_SetEditorCurve ( clip, ref binding, curve, syncEditorCurve );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_SetEditorCurve (AnimationClip clip, ref EditorCurveBinding binding, AnimationCurve curve, bool syncEditorCurve);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SyncEditorCurves (AnimationClip clip) ;

public enum CurveModifiedType    
    {
        CurveDeleted = 0,
        CurveModified = 1,
        ClipModified = 2
    }

    public delegate void OnCurveWasModified(AnimationClip clip, EditorCurveBinding binding, CurveModifiedType deleted);
    public static OnCurveWasModified onCurveWasModified;
    
    
    [RequiredByNativeCode]
    private static void Internal_CallAnimationClipAwake(AnimationClip clip)
        {
            if (onCurveWasModified != null)
                onCurveWasModified(clip, new EditorCurveBinding(), CurveModifiedType.ClipModified);
        }
    
    
    public static void SetEditorCurve(AnimationClip clip, EditorCurveBinding binding, AnimationCurve curve)
        {
            Internal_SetEditorCurve(clip, binding, curve, true);

            if (onCurveWasModified != null)
                onCurveWasModified(clip, binding, curve != null ? CurveModifiedType.CurveModified : CurveModifiedType.CurveDeleted);
        }
    
    
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
    
    
    public static void SetObjectReferenceCurve(AnimationClip clip, EditorCurveBinding binding, ObjectReferenceKeyframe[] keyframes)
        {
            Internal_SetObjectReferenceCurve(clip, binding, keyframes);

            if (onCurveWasModified != null)
                onCurveWasModified(clip, binding, keyframes != null ? CurveModifiedType.CurveModified : CurveModifiedType.CurveDeleted);
        }
    
    
    public enum TangentMode    
    {
        Free = 0,
        Auto = 1,
        Linear = 2,
        Constant = 3,
        ClampedAuto = 4,
    }

    private static void VerifyCurve(AnimationCurve curve)
        {
            if (curve == null)
            {
                throw new ArgumentNullException("curve");
            }
        }
    
    
    private static void VerifyCurveAndKeyframeIndex(AnimationCurve curve, int index)
        {
            VerifyCurve(curve);

            if (index < 0 || index >= curve.length)
            {
                string message = String.Format("index {0} must be in the range of 0 to {1}.",
                        index, curve.length - 1);
                throw new ArgumentOutOfRangeException("index", message);
            }
        }
    
    
    internal static void UpdateTangentsFromModeSurrounding(AnimationCurve curve, int index)
        {
            VerifyCurveAndKeyframeIndex(curve, index);

            UpdateTangentsFromModeSurroundingInternal(curve, index);
        }
    
    
    internal static void UpdateTangentsFromMode(AnimationCurve curve)
        {
            VerifyCurve(curve);

            UpdateTangentsFromModeInternal(curve);
        }
    
    
    public static TangentMode GetKeyLeftTangentMode(AnimationCurve curve, int index)
        {
            VerifyCurveAndKeyframeIndex(curve, index);

            return GetKeyLeftTangentModeInternal(curve, index);
        }
    
    
    public static TangentMode GetKeyRightTangentMode(AnimationCurve curve, int index)
        {
            VerifyCurveAndKeyframeIndex(curve, index);

            return GetKeyRightTangentModeInternal(curve, index);
        }
    
    
    internal static bool GetKeyBroken (Keyframe key) {
        return INTERNAL_CALL_GetKeyBroken ( ref key );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_GetKeyBroken (ref Keyframe key);
    internal static void SetKeyLeftTangentMode (ref Keyframe key, TangentMode tangentMode) {
        INTERNAL_CALL_SetKeyLeftTangentMode ( ref key, tangentMode );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetKeyLeftTangentMode (ref Keyframe key, TangentMode tangentMode);
    internal static void SetKeyRightTangentMode (ref Keyframe key, TangentMode tangentMode) {
        INTERNAL_CALL_SetKeyRightTangentMode ( ref key, tangentMode );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetKeyRightTangentMode (ref Keyframe key, TangentMode tangentMode);
    internal static void SetKeyBroken (ref Keyframe key, bool broken) {
        INTERNAL_CALL_SetKeyBroken ( ref key, broken );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_SetKeyBroken (ref Keyframe key, bool broken);
    public static void SetKeyBroken(AnimationCurve curve, int index, bool broken)
        {
            VerifyCurveAndKeyframeIndex(curve, index);

            SetKeyBrokenInternal(curve, index, broken);
        }
    
    
    public static void SetKeyLeftTangentMode(AnimationCurve curve, int index, TangentMode tangentMode)
        {
            VerifyCurveAndKeyframeIndex(curve, index);

            SetKeyLeftTangentModeInternal(curve, index, tangentMode);
        }
    
    
    public static void SetKeyRightTangentMode(AnimationCurve curve, int index, TangentMode tangentMode)
        {
            VerifyCurveAndKeyframeIndex(curve, index);

            SetKeyRightTangentModeInternal(curve, index, tangentMode);
        }
    
    
    public static bool GetKeyBroken(AnimationCurve curve, int index)
        {
            VerifyCurveAndKeyframeIndex(curve, index);

            return GetKeyBrokenInternal(curve, index);
        }
    
    
    internal static TangentMode GetKeyLeftTangentMode (Keyframe key) {
        return INTERNAL_CALL_GetKeyLeftTangentMode ( ref key );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static TangentMode INTERNAL_CALL_GetKeyLeftTangentMode (ref Keyframe key);
    internal static TangentMode GetKeyRightTangentMode (Keyframe key) {
        return INTERNAL_CALL_GetKeyRightTangentMode ( ref key );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static TangentMode INTERNAL_CALL_GetKeyRightTangentMode (ref Keyframe key);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void UpdateTangentsFromModeSurroundingInternal (AnimationCurve curve, int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void UpdateTangentsFromModeInternal (AnimationCurve curve) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  TangentMode GetKeyLeftTangentModeInternal (AnimationCurve curve, int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  TangentMode GetKeyRightTangentModeInternal (AnimationCurve curve, int index) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void SetKeyBrokenInternal (AnimationCurve curve, int index, bool broken) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void SetKeyLeftTangentModeInternal (AnimationCurve curve, int index, TangentMode tangentMode) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void SetKeyRightTangentModeInternal (AnimationCurve curve, int index, TangentMode tangentMode) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool GetKeyBrokenInternal (AnimationCurve curve, int index) ;

    [System.Obsolete ("GetAllCurves is deprecated. Use GetCurveBindings and GetObjectReferenceCurveBindings instead.")]
[uei.ExcludeFromDocs]
public static AnimationClipCurveData[] GetAllCurves (AnimationClip clip) {
    bool includeCurveData = true;
    return GetAllCurves ( clip, includeCurveData );
}

[System.Obsolete ("GetAllCurves is deprecated. Use GetCurveBindings and GetObjectReferenceCurveBindings instead.")]
public static AnimationClipCurveData[] GetAllCurves(AnimationClip clip, [uei.DefaultValue("true")]  bool includeCurveData )
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

    
    
    [System.Obsolete ("This overload is deprecated. Use the one with EditorCurveBinding instead.")]
public static bool GetFloatValue(GameObject root, string relativePath, Type type, string propertyName, out float data)
        {
            return GetFloatValue(root, EditorCurveBinding.FloatCurve(relativePath, type, propertyName), out data);
        }
    
    
    [System.Obsolete ("This overload is deprecated. Use the one with EditorCurveBinding instead.")]
public static void SetEditorCurve(AnimationClip clip, string relativePath, Type type, string propertyName, AnimationCurve curve)
        {
            SetEditorCurve(clip, EditorCurveBinding.FloatCurve(relativePath, type, propertyName), curve);
        }
    
    
    [System.Obsolete ("This overload is deprecated. Use the one with EditorCurveBinding instead.")]
public static AnimationCurve GetEditorCurve(AnimationClip clip, string relativePath, Type type, string propertyName)
        {
            return GetEditorCurve(clip, EditorCurveBinding.FloatCurve(relativePath, type, propertyName));
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  AnimationEvent[] GetAnimationEvents (AnimationClip clip) ;

    public static void SetAnimationEvents(AnimationClip clip, AnimationEvent[] events)
        {
            if (clip == null)
                throw new ArgumentNullException("clip");
            if (events == null)
                throw new ArgumentNullException("events");

            Internal_SetAnimationEvents(clip, events);

            if (onCurveWasModified != null)
                onCurveWasModified(clip, new EditorCurveBinding(), CurveModifiedType.ClipModified);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetAnimationEvents (AnimationClip clip, AnimationEvent[] events) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string CalculateTransformPath (Transform targetTransform, Transform root) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  AnimationClipSettings GetAnimationClipSettings (AnimationClip clip) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetAnimationClipSettings (AnimationClip clip, AnimationClipSettings srcClipInfo) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetAnimationClipSettingsNoDirty (AnimationClip clip, AnimationClipSettings srcClipInfo) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetAdditiveReferencePose (AnimationClip clip, AnimationClip referenceClip, float time) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsValidOptimizedPolynomialCurve (AnimationCurve curve) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void ConstrainToPolynomialCurve (AnimationCurve curve) ;

internal enum PolynomialValid    
    {
        Valid = 0,
        InvalidPreWrapMode = 1,
        InvalidPostWrapMode = 2,
        TooManySegments = 3
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetMaxNumPolynomialSegmentsSupported () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  PolynomialValid IsValidPolynomialCurve (AnimationCurve curve) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  AnimationClipStats GetAnimationClipStats (AnimationClip clip) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool GetGenerateMotionCurves (AnimationClip clip) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetGenerateMotionCurves (AnimationClip clip, bool value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool HasGenericRootTransform (AnimationClip clip) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool HasMotionFloatCurves (AnimationClip clip) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool HasMotionCurves (AnimationClip clip) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool HasRootCurves (AnimationClip clip) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool AmbiguousBinding (string path, int classID, Transform root) ;

    internal static Vector3 GetClosestEuler (Quaternion q, Vector3 eulerHint, RotationOrder rotationOrder) {
        Vector3 result;
        INTERNAL_CALL_GetClosestEuler ( ref q, ref eulerHint, rotationOrder, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetClosestEuler (ref Quaternion q, ref Vector3 eulerHint, RotationOrder rotationOrder, out Vector3 value);
    [System.Obsolete ("Use AnimationMode.InAnimationMode instead")]
static public bool InAnimationMode()
        {
            return AnimationMode.InAnimationMode();
        }
    
    
    [System.Obsolete ("Use AnimationMode.StartAnimationmode instead")]
public static void StartAnimationMode(Object[] objects)
        {
            Debug.LogWarning("AnimationUtility.StartAnimationMode is deprecated. Use AnimationMode.StartAnimationMode with the new APIs. The objects passed to this function will no longer be reverted automatically. See AnimationMode.AddPropertyModification");
            AnimationMode.StartAnimationMode();
        }
    
    
    [System.Obsolete ("Use AnimationMode.StopAnimationMode instead")]
public static void StopAnimationMode()
        {
            AnimationMode.StopAnimationMode();
        }
    
    
    [System.Obsolete ("SetAnimationType is no longer supported", true)]
public static void SetAnimationType(AnimationClip clip, ModelImporterAnimationType type) {}
    
    
}

internal sealed partial class AnimationModeDriver : ScriptableObject
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

public sealed partial class AnimationMode
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
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool IsPropertyAnimated (Object target, string propertyPath) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsPropertyCandidate (Object target, string propertyPath) ;

    public static void StopAnimationMode()
        {
            StopAnimationMode(DummyDriver());
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void StopAnimationMode (Object driver) ;

    public static bool InAnimationMode()
        {
            return Internal_InAnimationModeNoDriver();
        }
    
    
    internal static bool InAnimationMode(Object driver)
        {
            return Internal_InAnimationMode(driver);
        }
    
    
    public static void StartAnimationMode()
        {
            StartAnimationMode(DummyDriver());
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void StartAnimationMode (Object driver) ;

    static internal void StopAnimationPlaybackMode()
        {
            s_InAnimationPlaybackMode = false;
        }
    
    
    static internal bool InAnimationPlaybackMode()
        {
            return s_InAnimationPlaybackMode;
        }
    
    
    static internal void StartAnimationPlaybackMode()
        {
            s_InAnimationPlaybackMode = true;
        }
    
    
    static internal void StopAnimationRecording()
        {
            s_InAnimationRecordMode = false;
        }
    
    
    static internal bool InAnimationRecording()
        {
            return s_InAnimationRecordMode;
        }
    
    
    static internal void StartAnimationRecording()
        {
            s_InAnimationRecordMode = true;
        }
    
    
    internal static void StartCandidateRecording(Object driver)
        {
            if (!InAnimationMode())
                throw new InvalidOperationException("AnimationMode.StartCandidateRecording may only be called in animation mode.  See AnimationMode.StartAnimationMode.");

            Internal_StartCandidateRecording(driver);
        }
    
    
    internal static void AddCandidate(EditorCurveBinding binding, PropertyModification modification, bool keepPrefabOverride)
        {
            if (!IsRecordingCandidates())
                throw new InvalidOperationException("AnimationMode.AddCandidate may only be called when recording candidates.  See AnimationMode.StartCandidateRecording.");

            Internal_AddCandidate(binding, modification, keepPrefabOverride);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void StopCandidateRecording () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsRecordingCandidates () ;

    public static void BeginSampling()
        {
            if (!InAnimationMode())
                throw new InvalidOperationException("AnimationMode.BeginSampling may only be called in animation mode.  See AnimationMode.StartAnimationMode.");

            Internal_BeginSampling();
        }
    
    
    public static void EndSampling()
        {
            if (!InAnimationMode())
                throw new InvalidOperationException("AnimationMode.EndSampling may only be called in animation mode.  See AnimationMode.StartAnimationMode.");

            Internal_EndSampling();
        }
    
    
    public static void SampleAnimationClip(GameObject gameObject, AnimationClip clip, float time)
        {
            if (!InAnimationMode())
                throw new InvalidOperationException("AnimationMode.SampleAnimationClip may only be called in animation mode.  See AnimationMode.StartAnimationMode.");

            Internal_SampleAnimationClip(gameObject, clip, time);
        }
    
    
    internal static void SampleCandidateClip(GameObject gameObject, AnimationClip clip, float time)
        {
            if (!IsRecordingCandidates())
                throw new InvalidOperationException("AnimationMode.SampleCandidateClip may only be called when recording candidates.  See AnimationMode.StartAnimationMode.");

            Internal_SampleCandidateClip(gameObject, clip, time);
        }
    
    
    public static void AddPropertyModification(EditorCurveBinding binding, PropertyModification modification, bool keepPrefabOverride)
        {
            if (!InAnimationMode())
                throw new InvalidOperationException("AnimationMode.AddPropertyModification may only be called in animation mode.  See AnimationMode.StartAnimationMode.");

            Internal_AddPropertyModification(binding, modification, keepPrefabOverride);
        }
    
    
    internal static void InitializePropertyModificationForGameObject(GameObject gameObject, AnimationClip clip)
        {
            if (!InAnimationMode())
                throw new InvalidOperationException("AnimationMode.InitializePropertyModificationForGameObject may only be called in animation mode.  See AnimationMode.StartAnimationMode.");

            Internal_InitializePropertyModificationForGameObject(gameObject, clip);
        }
    
    
    internal static void InitializePropertyModificationForObject(Object target, AnimationClip clip)
        {
            if (!InAnimationMode())
                throw new InvalidOperationException("AnimationMode.InitializePropertyModificationForObject may only be called in animation mode.  See AnimationMode.StartAnimationMode.");

            Internal_InitializePropertyModificationForObject(target, clip);
        }
    
    
    internal static void RevertPropertyModificationsForGameObject(GameObject gameObject)
        {
            if (!InAnimationMode())
                throw new InvalidOperationException("AnimationMode.RevertPropertyModificationsForGameObject may only be called in animation mode.  See AnimationMode.StartAnimationMode.");

            Internal_RevertPropertyModificationsForGameObject(gameObject);
        }
    
    
    internal static void RevertPropertyModificationsForObject(Object target)
        {
            if (!InAnimationMode())
                throw new InvalidOperationException("AnimationMode.RevertPropertyModificationsForObject may only be called in animation mode.  See AnimationMode.StartAnimationMode.");

            Internal_RevertPropertyModificationsForObject(target);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool Internal_InAnimationMode (Object driver) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool Internal_InAnimationModeNoDriver () ;

    private static void Internal_AddCandidate (EditorCurveBinding binding, PropertyModification modification, bool keepPrefabOverride) {
        INTERNAL_CALL_Internal_AddCandidate ( ref binding, modification, keepPrefabOverride );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_AddCandidate (ref EditorCurveBinding binding, PropertyModification modification, bool keepPrefabOverride);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_StartCandidateRecording (Object driver) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_BeginSampling () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_EndSampling () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SampleAnimationClip (GameObject gameObject, AnimationClip clip, float time) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SampleCandidateClip (GameObject gameObject, AnimationClip clip, float time) ;

    private static void Internal_AddPropertyModification (EditorCurveBinding binding, PropertyModification modification, bool keepPrefabOverride) {
        INTERNAL_CALL_Internal_AddPropertyModification ( ref binding, modification, keepPrefabOverride );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_AddPropertyModification (ref EditorCurveBinding binding, PropertyModification modification, bool keepPrefabOverride);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_InitializePropertyModificationForGameObject (GameObject gameObject, AnimationClip clip) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_InitializePropertyModificationForObject (Object target, AnimationClip clip) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_RevertPropertyModificationsForGameObject (GameObject gameObject) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_RevertPropertyModificationsForObject (Object target) ;

}

}
