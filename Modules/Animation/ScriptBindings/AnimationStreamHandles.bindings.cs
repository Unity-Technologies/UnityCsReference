// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Animations
{
    // This enum is mapped to UnityEngine::Animation::BindType
    internal enum BindType
    {
        Unbound                        = 0,  // UnityEngine::Animation::kUnbound
        Float                          = 5,  // UnityEngine::Animation::kBindFloat
        Bool                           = 6,  // UnityEngine::Animation::kBindFloatToBool
        GameObjectActive               = 7,  // UnityEngine::Animation::kBindGameObjectActive
        ObjectReference                = 9,  // UnityEngine::Animation::kBindScriptObjectReference;
        Int                            = 10, // UnityEngine::Animation::kBindFloatToInt
        DiscreetInt                    = 11, // UnityEngine::Animation::kBindDiscreteInt
    }

    [MovedFrom("UnityEngine.Experimental.Animations")]
    [NativeHeader("Modules/Animation/ScriptBindings/AnimationStreamHandles.bindings.h")]
    [NativeHeader("Modules/Animation/Director/AnimationStreamHandles.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct TransformStreamHandle
    {
        private UInt32 m_AnimatorBindingsVersion;
        private int handleIndex;
        private int skeletonIndex;

        public bool IsValid(AnimationStream stream)
        {
            return IsValidInternal(ref stream);
        }

        private bool IsValidInternal(ref AnimationStream stream)
        {
            return stream.isValid && createdByNative && hasHandleIndex;
        }

        private bool createdByNative
        {
            get { return animatorBindingsVersion != (UInt32)AnimatorBindingsVersion.kInvalidNotNative; }
        }

        private bool IsSameVersionAsStream(ref AnimationStream stream)
        {
            return animatorBindingsVersion == stream.animatorBindingsVersion;
        }

        private bool hasHandleIndex
        {
            get { return handleIndex != AnimationStream.InvalidIndex; }
        }

        private bool hasSkeletonIndex
        {
            get { return skeletonIndex != AnimationStream.InvalidIndex; }
        }

        // internal for EditorTests
        internal UInt32 animatorBindingsVersion
        {
            private set { m_AnimatorBindingsVersion = value; }
            get { return m_AnimatorBindingsVersion; }
        }

        public void Resolve(AnimationStream stream)
        {
            CheckIsValidAndResolve(ref stream);
        }

        public bool IsResolved(AnimationStream stream)
        {
            return IsResolvedInternal(ref stream);
        }

        private bool IsResolvedInternal(ref AnimationStream stream)
        {
            return IsValidInternal(ref stream) &&
                IsSameVersionAsStream(ref stream) &&
                hasSkeletonIndex;
        }

        private void CheckIsValidAndResolve(ref AnimationStream stream)
        {
            // Verify stream.
            stream.CheckIsValid();

            if (IsResolvedInternal(ref stream))
                return;

            // Handle create directly by user are never valid
            if (!createdByNative || !hasHandleIndex)
                throw new InvalidOperationException("The TransformStreamHandle is invalid. Please use proper function to create the handle.");

            if (!IsSameVersionAsStream(ref stream) || (hasHandleIndex && !hasSkeletonIndex))
            {
                ResolveInternal(ref stream);
            }

            if (hasHandleIndex && !hasSkeletonIndex)
                throw new InvalidOperationException("The TransformStreamHandle cannot be resolved.");
        }

        public Vector3 GetPosition(AnimationStream stream) { CheckIsValidAndResolve(ref stream); return GetPositionInternal(ref stream); }
        public void SetPosition(AnimationStream stream, Vector3 position) { CheckIsValidAndResolve(ref stream); SetPositionInternal(ref stream, position); }

        public Quaternion GetRotation(AnimationStream stream) { CheckIsValidAndResolve(ref stream); return GetRotationInternal(ref stream); }
        public void SetRotation(AnimationStream stream, Quaternion rotation) { CheckIsValidAndResolve(ref stream); SetRotationInternal(ref stream, rotation); }

        public Vector3 GetLocalPosition(AnimationStream stream) { CheckIsValidAndResolve(ref stream); return GetLocalPositionInternal(ref stream); }
        public void SetLocalPosition(AnimationStream stream, Vector3 position) { CheckIsValidAndResolve(ref stream); SetLocalPositionInternal(ref stream, position); }

        public Quaternion GetLocalRotation(AnimationStream stream) { CheckIsValidAndResolve(ref stream); return GetLocalRotationInternal(ref stream); }
        public void SetLocalRotation(AnimationStream stream, Quaternion rotation) { CheckIsValidAndResolve(ref stream); SetLocalRotationInternal(ref stream, rotation); }

        public Vector3 GetLocalScale(AnimationStream stream) { CheckIsValidAndResolve(ref stream); return GetLocalScaleInternal(ref stream); }
        public void SetLocalScale(AnimationStream stream, Vector3 scale) { CheckIsValidAndResolve(ref stream); SetLocalScaleInternal(ref stream, scale); }

        public Matrix4x4 GetLocalToParentMatrix(AnimationStream stream)
        {
            CheckIsValidAndResolve(ref stream);
            return GetLocalToParentMatrixInternal(ref stream);
        }

        public bool GetPositionReadMask(AnimationStream stream) { CheckIsValidAndResolve(ref stream); return GetPositionReadMaskInternal(ref stream); }
        public bool GetRotationReadMask(AnimationStream stream) { CheckIsValidAndResolve(ref stream); return GetRotationReadMaskInternal(ref stream); }
        public bool GetScaleReadMask(AnimationStream stream) { CheckIsValidAndResolve(ref stream); return GetScaleReadMaskInternal(ref stream); }

        public void GetLocalTRS(AnimationStream stream, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            CheckIsValidAndResolve(ref stream);
            GetLocalTRSInternal(ref stream, out position, out rotation, out scale);
        }

        public void SetLocalTRS(AnimationStream stream, Vector3 position, Quaternion rotation, Vector3 scale, bool useMask)
        {
            CheckIsValidAndResolve(ref stream);
            SetLocalTRSInternal(ref stream, position, rotation, scale, useMask);
        }

        public void GetGlobalTR(AnimationStream stream, out Vector3 position, out Quaternion rotation)
        {
            CheckIsValidAndResolve(ref stream);
            GetGlobalTRInternal(ref stream, out position, out rotation);
        }

        public Matrix4x4 GetLocalToWorldMatrix(AnimationStream stream)
        {
            CheckIsValidAndResolve(ref stream);
            return GetLocalToWorldMatrixInternal(ref stream);
        }

        public void SetGlobalTR(AnimationStream stream, Vector3 position, Quaternion rotation, bool useMask)
        {
            CheckIsValidAndResolve(ref stream);
            SetGlobalTRInternal(ref stream, position, rotation, useMask);
        }

        [NativeMethod(Name = "Resolve", IsThreadSafe = true)]
        private extern void ResolveInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformStreamHandleBindings::GetPositionInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern Vector3 GetPositionInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformStreamHandleBindings::SetPositionInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern void SetPositionInternal(ref AnimationStream stream, Vector3 position);

        [NativeMethod(Name = "TransformStreamHandleBindings::GetRotationInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern Quaternion GetRotationInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformStreamHandleBindings::SetRotationInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern void SetRotationInternal(ref AnimationStream stream, Quaternion rotation);

        [NativeMethod(Name = "TransformStreamHandleBindings::GetLocalPositionInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern Vector3 GetLocalPositionInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformStreamHandleBindings::SetLocalPositionInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern void SetLocalPositionInternal(ref AnimationStream stream, Vector3 position);

        [NativeMethod(Name = "TransformStreamHandleBindings::GetLocalRotationInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern Quaternion GetLocalRotationInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformStreamHandleBindings::SetLocalRotationInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern void SetLocalRotationInternal(ref AnimationStream stream, Quaternion rotation);

        [NativeMethod(Name = "TransformStreamHandleBindings::GetLocalScaleInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern Vector3 GetLocalScaleInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformStreamHandleBindings::SetLocalScaleInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern void SetLocalScaleInternal(ref AnimationStream stream, Vector3 scale);

        [NativeMethod(Name = "TransformStreamHandleBindings::GetLocalToParentMatrixInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern Matrix4x4 GetLocalToParentMatrixInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformStreamHandleBindings::GetPositionReadMaskInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern bool GetPositionReadMaskInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformStreamHandleBindings::GetRotationReadMaskInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern bool GetRotationReadMaskInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformStreamHandleBindings::GetScaleReadMaskInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern bool GetScaleReadMaskInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformStreamHandleBindings::GetLocalTRSInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern void GetLocalTRSInternal(ref AnimationStream stream, out Vector3 position, out Quaternion rotation, out Vector3 scale);

        [NativeMethod(Name = "TransformStreamHandleBindings::SetLocalTRSInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern void SetLocalTRSInternal(ref AnimationStream stream, Vector3 position, Quaternion rotation, Vector3 scale, bool useMask);

        [NativeMethod(Name = "TransformStreamHandleBindings::GetGlobalTRInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern void GetGlobalTRInternal(ref AnimationStream stream, out Vector3 position, out Quaternion rotation);

        [NativeMethod(Name = "TransformStreamHandleBindings::GetLocalToWorldMatrixInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern Matrix4x4 GetLocalToWorldMatrixInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformStreamHandleBindings::SetGlobalTRInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern void SetGlobalTRInternal(ref AnimationStream stream, Vector3 position, Quaternion rotation, bool useMask);
    }

    [MovedFrom("UnityEngine.Experimental.Animations")]
    [NativeHeader("Modules/Animation/Director/AnimationStreamHandles.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct PropertyStreamHandle
    {
        private UInt32 m_AnimatorBindingsVersion;
        private int handleIndex;
        private int valueArrayIndex;
        private int bindType;

        public bool IsValid(AnimationStream stream)
        {
            return IsValidInternal(ref stream);
        }

        private bool IsValidInternal(ref AnimationStream stream)
        {
            return stream.isValid && createdByNative && hasHandleIndex && hasBindType;
        }

        private bool createdByNative
        {
            get { return animatorBindingsVersion != (UInt32)AnimatorBindingsVersion.kInvalidNotNative; }
        }

        private bool IsSameVersionAsStream(ref AnimationStream stream)
        {
            return animatorBindingsVersion == stream.animatorBindingsVersion;
        }

        private bool hasHandleIndex
        {
            get { return handleIndex != AnimationStream.InvalidIndex; }
        }

        private bool hasValueArrayIndex
        {
            get { return valueArrayIndex != AnimationStream.InvalidIndex; }
        }

        private bool hasBindType
        {
            get { return bindType != (int)BindType.Unbound; }
        }

        // internal for EditorTests
        internal UInt32 animatorBindingsVersion
        {
            private set { m_AnimatorBindingsVersion = value; }
            get { return m_AnimatorBindingsVersion; }
        }

        public void Resolve(AnimationStream stream)
        {
            CheckIsValidAndResolve(ref stream);
        }

        public bool IsResolved(AnimationStream stream)
        {
            return IsResolvedInternal(ref stream);
        }

        private bool IsResolvedInternal(ref AnimationStream stream)
        {
            return IsValidInternal(ref stream) &&
                IsSameVersionAsStream(ref stream) &&
                hasValueArrayIndex;
        }

        private void CheckIsValidAndResolve(ref AnimationStream stream)
        {
            // Verify stream.
            stream.CheckIsValid();

            if (IsResolvedInternal(ref stream))
                return;

            // Handle create directly by user are never valid
            if (!createdByNative || !hasHandleIndex || !hasBindType)
                throw new InvalidOperationException("The PropertyStreamHandle is invalid. Please use proper function to create the handle.");

            if (!IsSameVersionAsStream(ref stream) || (hasHandleIndex && !hasValueArrayIndex))
            {
                ResolveInternal(ref stream);
            }

            if (hasHandleIndex && !hasValueArrayIndex)
                throw new InvalidOperationException("The PropertyStreamHandle cannot be resolved.");
        }

        public float GetFloat(AnimationStream stream)
        {
            CheckIsValidAndResolve(ref stream);
            if (bindType != (int)BindType.Float)
                throw new InvalidOperationException("GetValue type doesn't match PropertyStreamHandle bound type.");
            return GetFloatInternal(ref stream);
        }

        public void SetFloat(AnimationStream stream, float value)
        {
            CheckIsValidAndResolve(ref stream);
            if (bindType != (int)BindType.Float)
                throw new InvalidOperationException("SetValue type doesn't match PropertyStreamHandle bound type.");
            SetFloatInternal(ref stream, value);
        }

        public int GetInt(AnimationStream stream)
        {
            CheckIsValidAndResolve(ref stream);
            if (bindType == (int)BindType.ObjectReference)
            {
                Debug.LogWarning("Please Use GetEntityId directly to get the value of an ObjectReference PropertyStreamHandle.");
                return GetEntityId(stream);
            }
            if (bindType != (int)BindType.Int && bindType != (int)BindType.DiscreetInt)
                throw new InvalidOperationException("GetValue type doesn't match PropertyStreamHandle bound type.");
            return GetIntInternal(ref stream);
        }

        public void SetInt(AnimationStream stream, int value)
        {
            CheckIsValidAndResolve(ref stream);
            if (bindType == (int)BindType.ObjectReference)
            {
                Debug.LogWarning("Please Use SetEntityId directly to set the value of an ObjectReference PropertyStreamHandle.");
                SetEntityId(stream, value);
                return;
            }

            if (bindType != (int)BindType.Int && bindType != (int)BindType.DiscreetInt)
                throw new InvalidOperationException("SetValue type doesn't match PropertyStreamHandle bound type.");
            SetIntInternal(ref stream, value);
        }

        public EntityId GetEntityId(AnimationStream stream)
        {
            CheckIsValidAndResolve(ref stream);
            if (bindType != (int)BindType.ObjectReference)
                throw new InvalidOperationException("GetValue type doesn't match PropertyStreamHandle bound type.");
            return GetEntityIdInternal(ref stream);
        }

        public void SetEntityId(AnimationStream stream, EntityId value)
        {
            CheckIsValidAndResolve(ref stream);
            if (bindType != (int)BindType.ObjectReference)
                throw new InvalidOperationException("SetValue type doesn't match PropertyStreamHandle bound type.");
            SetEntityIdInternal(ref stream, value);
        }

        public bool GetBool(AnimationStream stream)
        {
            CheckIsValidAndResolve(ref stream);
            if (bindType != (int)BindType.Bool && bindType != (int)BindType.GameObjectActive)
                throw new InvalidOperationException("GetValue type doesn't match PropertyStreamHandle bound type.");
            return GetBoolInternal(ref stream);
        }

        public void SetBool(AnimationStream stream, bool value)
        {
            CheckIsValidAndResolve(ref stream);
            if (bindType != (int)BindType.Bool && bindType != (int)BindType.GameObjectActive)
                throw new InvalidOperationException("SetValue type doesn't match PropertyStreamHandle bound type.");
            SetBoolInternal(ref stream, value);
        }

        public bool GetReadMask(AnimationStream stream)
        {
            CheckIsValidAndResolve(ref stream);
            return GetReadMaskInternal(ref stream);
        }

        [NativeMethod(Name = "Resolve", IsThreadSafe = true)]
        private extern void ResolveInternal(ref AnimationStream stream);

        [NativeMethod(Name = "GetFloat", IsThreadSafe = true)]
        private extern float GetFloatInternal(ref AnimationStream stream);

        [NativeMethod(Name = "SetFloat", IsThreadSafe = true)]
        private extern void SetFloatInternal(ref AnimationStream stream, float value);

        [NativeMethod(Name = "GetInt", IsThreadSafe = true)]
        private extern int GetIntInternal(ref AnimationStream stream);

        [NativeMethod(Name = "SetInt", IsThreadSafe = true)]
        private extern void SetIntInternal(ref AnimationStream stream, int value);

        [NativeMethod(Name = "GetEntityId", IsThreadSafe = true)]
        private extern EntityId GetEntityIdInternal(ref AnimationStream stream);

        [NativeMethod(Name = "SetEntityId", IsThreadSafe = true)]
        private extern void SetEntityIdInternal(ref AnimationStream stream, EntityId value);

        [NativeMethod(Name = "GetBool", IsThreadSafe = true)]
        private extern bool GetBoolInternal(ref AnimationStream stream);

        [NativeMethod(Name = "SetBool", IsThreadSafe = true)]
        private extern void SetBoolInternal(ref AnimationStream stream, bool value);

        [NativeMethod(Name = "GetReadMask", IsThreadSafe = true)]
        private extern bool GetReadMaskInternal(ref AnimationStream stream);
    }

    [MovedFrom("UnityEngine.Experimental.Animations")]
    [NativeHeader("Modules/Animation/ScriptBindings/AnimationStreamHandles.bindings.h")]
    [NativeHeader("Modules/Animation/Director/AnimationSceneHandles.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct TransformSceneHandle
    {
        private UInt32 valid;
        private int transformSceneHandleDefinitionIndex;

        public bool IsValid(AnimationStream stream)
        {
            // [case 1032369] Cannot call native code before validating that handle was created in native and has a valid handle index
            return stream.isValid &&
                createdByNative &&
                hasTransformSceneHandleDefinitionIndex &&
                HasValidTransform(ref stream);
        }

        private bool createdByNative
        {
            get { return valid != 0; }
        }

        private bool hasTransformSceneHandleDefinitionIndex
        {
            get { return transformSceneHandleDefinitionIndex != AnimationStream.InvalidIndex; }
        }

        private void CheckIsValid(ref AnimationStream stream)
        {
            // Verify stream.
            stream.CheckIsValid();

            // Handle create directly by user are never valid
            if (!createdByNative || !hasTransformSceneHandleDefinitionIndex)
                throw new InvalidOperationException("The TransformSceneHandle is invalid. Please use proper function to create the handle.");

            // [case 1032369] Cannot call native code before validating that handle was created in native and has a valid handle index
            if (!HasValidTransform(ref stream))
                throw new NullReferenceException("The transform is invalid.");
        }

        public Vector3 GetPosition(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetPositionInternal(ref stream);
        }

        [Obsolete("SceneHandle is now read-only; it was problematic with the engine multithreading and determinism", true)]
        public void SetPosition(AnimationStream stream, Vector3 position) {}

        public Vector3 GetLocalPosition(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetLocalPositionInternal(ref stream);
        }

        [Obsolete("SceneHandle is now read-only; it was problematic with the engine multithreading and determinism", true)]
        public void SetLocalPosition(AnimationStream stream, Vector3 position) {}

        public Quaternion GetRotation(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetRotationInternal(ref stream);
        }

        [Obsolete("SceneHandle is now read-only; it was problematic with the engine multithreading and determinism", true)]
        public void SetRotation(AnimationStream stream, Quaternion rotation) {}

        public Quaternion GetLocalRotation(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetLocalRotationInternal(ref stream);
        }

        [Obsolete("SceneHandle is now read-only; it was problematic with the engine multithreading and determinism", true)]
        public void SetLocalRotation(AnimationStream stream, Quaternion rotation) {}

        public Vector3 GetLocalScale(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetLocalScaleInternal(ref stream);
        }

        public void GetLocalTRS(AnimationStream stream, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            CheckIsValid(ref stream);
            GetLocalTRSInternal(ref stream, out position, out rotation, out scale);
        }

        public Matrix4x4 GetLocalToParentMatrix(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetLocalToParentMatrixInternal(ref stream);
        }

        public void GetGlobalTR(AnimationStream stream, out Vector3 position, out Quaternion rotation)
        {
            CheckIsValid(ref stream);
            GetGlobalTRInternal(ref stream, out position, out rotation);
        }

        public Matrix4x4 GetLocalToWorldMatrix(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetLocalToWorldMatrixInternal(ref stream);
        }

        [Obsolete("SceneHandle is now read-only; it was problematic with the engine multithreading and determinism", true)]
        public void SetLocalScale(AnimationStream stream, Vector3 scale) {}

        [ThreadSafe]
        private extern bool HasValidTransform(ref AnimationStream stream);

        [NativeMethod(Name = "TransformSceneHandleBindings::GetPositionInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 GetPositionInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformSceneHandleBindings::GetLocalPositionInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 GetLocalPositionInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformSceneHandleBindings::GetRotationInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Quaternion GetRotationInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformSceneHandleBindings::GetLocalRotationInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Quaternion GetLocalRotationInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformSceneHandleBindings::GetLocalScaleInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 GetLocalScaleInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformSceneHandleBindings::GetLocalTRSInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void GetLocalTRSInternal(ref AnimationStream stream, out Vector3 position, out Quaternion rotation, out Vector3 scale);

        [NativeMethod(Name = "TransformSceneHandleBindings::GetLocalToParentMatrixInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern Matrix4x4 GetLocalToParentMatrixInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformSceneHandleBindings::GetGlobalTRInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void GetGlobalTRInternal(ref AnimationStream stream, out Vector3 position, out Quaternion rotation);

        [NativeMethod(Name = "TransformSceneHandleBindings::GetLocalToWorldMatrixInternal", IsFreeFunction = true, HasExplicitThis = true, IsThreadSafe = true)]
        private extern Matrix4x4 GetLocalToWorldMatrixInternal(ref AnimationStream stream);
    }

    [MovedFrom("UnityEngine.Experimental.Animations")]
    [NativeHeader("Modules/Animation/Director/AnimationSceneHandles.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct PropertySceneHandle
    {
        private UInt32 valid;
        private int handleIndex;

        public bool IsValid(AnimationStream stream)
        {
            return IsValidInternal(ref stream);
        }

        private bool IsValidInternal(ref AnimationStream stream)
        {
            // [case 1032369] Cannot call native code before validating that handle was created in native and has a valid handle index
            return stream.isValid &&
                createdByNative &&
                hasHandleIndex &&
                HasValidTransform(ref stream);
        }

        private bool createdByNative
        {
            get { return valid != 0; }
        }

        private bool hasHandleIndex
        {
            get { return handleIndex != AnimationStream.InvalidIndex; }
        }

        public void Resolve(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            ResolveInternal(ref stream);
        }

        public bool IsResolved(AnimationStream stream)
        {
            return IsValidInternal(ref stream) && IsBound(ref stream);
        }

        private void CheckIsValid(ref AnimationStream stream)
        {
            // Verify stream.
            stream.CheckIsValid();

            // Handle create directly by user are never valid
            if (!createdByNative || !hasHandleIndex)
                throw new InvalidOperationException("The PropertySceneHandle is invalid. Please use proper function to create the handle.");

            // [case 1032369] Cannot call native code before validating that handle was created in native and has a valid handle index
            if (!HasValidTransform(ref stream))
                throw new NullReferenceException("The transform is invalid.");
        }

        public float GetFloat(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetFloatInternal(ref stream);
        }

        [Obsolete("SceneHandle is now read-only; it was problematic with the engine multithreading and determinism", true)]
        public void SetFloat(AnimationStream stream, float value) {}

        public int GetInt(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetIntInternal(ref stream);
        }

        public EntityId GetEntityId(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetEntityIdInternal(ref stream);
        }

        [Obsolete("SceneHandle is now read-only; it was problematic with the engine multithreading and determinism", true)]
        public void SetInt(AnimationStream stream, int value) {}

        public bool GetBool(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetBoolInternal(ref stream);
        }

        [Obsolete("SceneHandle is now read-only; it was problematic with the engine multithreading and determinism", true)]
        public void SetBool(AnimationStream stream, bool value) {}

        [ThreadSafe]
        private extern bool HasValidTransform(ref AnimationStream stream);

        [ThreadSafe]
        private extern bool IsBound(ref AnimationStream stream);

        [NativeMethod(Name = "Resolve", IsThreadSafe = true)]
        private extern void ResolveInternal(ref AnimationStream stream);

        [NativeMethod(Name = "GetFloat", IsThreadSafe = true)]
        private extern float GetFloatInternal(ref AnimationStream stream);

        [NativeMethod(Name = "GetInt", IsThreadSafe = true)]
        private extern int GetIntInternal(ref AnimationStream stream);

        [NativeMethod(Name = "GetEntityId", IsThreadSafe = true)]
        private extern EntityId GetEntityIdInternal(ref AnimationStream stream);

        [NativeMethod(Name = "GetBool", IsThreadSafe = true)]
        private extern bool GetBoolInternal(ref AnimationStream stream);
    }

    [MovedFrom("UnityEngine.Experimental.Animations")]
    [NativeHeader("Modules/Animation/ScriptBindings/AnimationStreamHandles.bindings.h")]
    unsafe public static class AnimationSceneHandleUtility
    {
        public static void ReadInts(AnimationStream stream, NativeArray<PropertySceneHandle> handles, NativeArray<int> buffer)
        {
            int count = ValidateAndGetArrayCount(ref stream, handles, buffer);
            if (count == 0)
                return;

            ReadSceneIntsInternal(ref stream, handles.GetUnsafePtr(), buffer.GetUnsafePtr(), count);
        }

        public static void ReadFloats(AnimationStream stream, NativeArray<PropertySceneHandle> handles, NativeArray<float> buffer)
        {
            int count = ValidateAndGetArrayCount(ref stream, handles, buffer);
            if (count == 0)
                return;

            ReadSceneFloatsInternal(ref stream, handles.GetUnsafePtr(), buffer.GetUnsafePtr(), count);
        }

        public static void ReadEntityIds(AnimationStream stream, NativeArray<PropertySceneHandle> handles, NativeArray<EntityId> buffer)
        {
            int count = ValidateAndGetArrayCount(ref stream, handles, buffer);
            if (count == 0)
                return;

            ReadSceneEntityIdsInternal(ref stream, handles.GetUnsafePtr(), buffer.GetUnsafePtr(), count);
        }

        internal static int ValidateAndGetArrayCount<T0, T1>(ref AnimationStream stream, NativeArray<T0> handles, NativeArray<T1> buffer)
            where T0 : struct
            where T1 : struct
        {
            stream.CheckIsValid();

            if (!handles.IsCreated)
                throw new NullReferenceException("Handle array is invalid.");
            if (!buffer.IsCreated)
                throw new NullReferenceException("Data buffer is invalid.");
            if (buffer.Length < handles.Length)
                throw new InvalidOperationException("Data buffer array is smaller than handles array.");

            return handles.Length;
        }

        // PropertySceneHandle
        [NativeMethod(Name = "AnimationHandleUtilityBindings::ReadSceneIntsInternal", IsFreeFunction = true, HasExplicitThis = false, IsThreadSafe = true)]
        static private extern void ReadSceneIntsInternal(ref AnimationStream stream, void* propertySceneHandles, void* intBuffer, int count);

        [NativeMethod(Name = "AnimationHandleUtilityBindings::ReadSceneFloatsInternal", IsFreeFunction = true, HasExplicitThis = false, IsThreadSafe = true)]
        static private extern void ReadSceneFloatsInternal(ref AnimationStream stream, void* propertySceneHandles, void* floatBuffer, int count);

        [NativeMethod(Name = "AnimationHandleUtilityBindings::ReadSceneEntityIdsInternal", IsFreeFunction = true, HasExplicitThis = false, IsThreadSafe = true)]
        static private extern void ReadSceneEntityIdsInternal(ref AnimationStream stream, void* propertySceneHandles, void* instanceIDBuffer, int count);
    }

    [MovedFrom("UnityEngine.Experimental.Animations")]
    [NativeHeader("Modules/Animation/ScriptBindings/AnimationStreamHandles.bindings.h")]
    unsafe public static class AnimationStreamHandleUtility
    {
        public static void WriteInts(AnimationStream stream, NativeArray<PropertyStreamHandle> handles, NativeArray<int> buffer, bool useMask)
        {
            stream.CheckIsValid();
            int count = AnimationSceneHandleUtility.ValidateAndGetArrayCount(ref stream, handles, buffer);
            if (count == 0)
                return;

            WriteStreamIntsInternal(ref stream, handles.GetUnsafePtr(), buffer.GetUnsafePtr(), count, useMask);
        }

        public static void WriteFloats(AnimationStream stream, NativeArray<PropertyStreamHandle> handles, NativeArray<float> buffer, bool useMask)
        {
            stream.CheckIsValid();
            int count = AnimationSceneHandleUtility.ValidateAndGetArrayCount(ref stream, handles, buffer);
            if (count == 0)
                return;

            WriteStreamFloatsInternal(ref stream, handles.GetUnsafePtr(), buffer.GetUnsafePtr(), count, useMask);
        }

        public static void WriteEntityIds(AnimationStream stream, NativeArray<PropertyStreamHandle> handles, NativeArray<EntityId> buffer, bool useMask)
        {
            stream.CheckIsValid();
            int count = AnimationSceneHandleUtility.ValidateAndGetArrayCount(ref stream, handles, buffer);
            if (count == 0)
                return;

            WriteStreamEntityIdsInternal(ref stream, handles.GetUnsafePtr(), buffer.GetUnsafePtr(), count, useMask);
        }

        public static void ReadInts(AnimationStream stream, NativeArray<PropertyStreamHandle> handles, NativeArray<int> buffer)
        {
            stream.CheckIsValid();
            int count = AnimationSceneHandleUtility.ValidateAndGetArrayCount(ref stream, handles, buffer);
            if (count == 0)
                return;

            ReadStreamIntsInternal(ref stream, handles.GetUnsafePtr(), buffer.GetUnsafePtr(), count);
        }

        public static void ReadFloats(AnimationStream stream, NativeArray<PropertyStreamHandle> handles, NativeArray<float> buffer)
        {
            stream.CheckIsValid();
            int count = AnimationSceneHandleUtility.ValidateAndGetArrayCount(ref stream, handles, buffer);
            if (count == 0)
                return;

            ReadStreamFloatsInternal(ref stream, handles.GetUnsafePtr(), buffer.GetUnsafePtr(), count);
        }

        public static void ReadEntityIds(AnimationStream stream, NativeArray<PropertyStreamHandle> handles, NativeArray<EntityId> buffer)
        {
            stream.CheckIsValid();
            int count = AnimationSceneHandleUtility.ValidateAndGetArrayCount(ref stream, handles, buffer);
            if (count == 0)
                return;

            ReadStreamEntityIdsInternal(ref stream, handles.GetUnsafePtr(), buffer.GetUnsafePtr(), count);
        }

        // PropertyStreamHandle
        [NativeMethod(Name = "AnimationHandleUtilityBindings::ReadStreamIntsInternal", IsFreeFunction = true, HasExplicitThis = false, IsThreadSafe = true)]
        static private extern void ReadStreamIntsInternal(ref AnimationStream stream, void* propertyStreamHandles, void* intBuffer, int count);

        [NativeMethod(Name = "AnimationHandleUtilityBindings::ReadStreamFloatsInternal", IsFreeFunction = true, HasExplicitThis = false, IsThreadSafe = true)]
        static private extern void ReadStreamFloatsInternal(ref AnimationStream stream, void* propertyStreamHandles, void* floatBuffer, int count);

        [NativeMethod(Name = "AnimationHandleUtilityBindings::ReadStreamEntityIdsInternal", IsFreeFunction = true, HasExplicitThis = false, IsThreadSafe = true)]
        static private extern void ReadStreamEntityIdsInternal(ref AnimationStream stream, void* propertyStreamHandles, void* instanceIDBuffer, int count);

        [NativeMethod(Name = "AnimationHandleUtilityBindings::WriteStreamIntsInternal", IsFreeFunction = true, HasExplicitThis = false, IsThreadSafe = true)]
        static private extern void WriteStreamIntsInternal(ref AnimationStream stream, void* propertyStreamHandles, void* intBuffer, int count, bool useMask);

        [NativeMethod(Name = "AnimationHandleUtilityBindings::WriteStreamFloatsInternal", IsFreeFunction = true, HasExplicitThis = false, IsThreadSafe = true)]
        static private extern void WriteStreamFloatsInternal(ref AnimationStream stream, void* propertyStreamHandles, void* floatBuffer, int count, bool useMask);

        [NativeMethod(Name = "AnimationHandleUtilityBindings::WriteStreamEntityIdsInternal", IsFreeFunction = true, HasExplicitThis = false, IsThreadSafe = true)]
        static private extern void WriteStreamEntityIdsInternal(ref AnimationStream stream, void* propertyStreamHandles, void* instanceIDBuffer, int count, bool useMask);
    }
}

