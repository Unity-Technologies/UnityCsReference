// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Animations
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

    [NativeHeader("Runtime/Animation/ScriptBindings/AnimationStreamHandles.bindings.h")]
    [NativeHeader("Runtime/Animation/Director/AnimationStreamHandles.h")]
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
    }

    [NativeHeader("Runtime/Animation/Director/AnimationStreamHandles.h")]
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
            if (bindType != (int)BindType.Int && bindType != (int)BindType.DiscreetInt && bindType != (int)BindType.ObjectReference)
                throw new InvalidOperationException("GetValue type doesn't match PropertyStreamHandle bound type.");
            return GetIntInternal(ref stream);
        }

        public void SetInt(AnimationStream stream, int value)
        {
            CheckIsValidAndResolve(ref stream);
            if (bindType != (int)BindType.Int && bindType != (int)BindType.DiscreetInt && bindType != (int)BindType.ObjectReference)
                throw new InvalidOperationException("SetValue type doesn't match PropertyStreamHandle bound type.");
            SetIntInternal(ref stream, value);
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

        [NativeMethod(Name = "GetBool", IsThreadSafe = true)]
        private extern bool GetBoolInternal(ref AnimationStream stream);

        [NativeMethod(Name = "SetBool", IsThreadSafe = true)]
        private extern void SetBoolInternal(ref AnimationStream stream, bool value);
    }

    [NativeHeader("Runtime/Animation/ScriptBindings/AnimationStreamHandles.bindings.h")]
    [NativeHeader("Runtime/Animation/Director/AnimationSceneHandles.h")]
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

        public void SetPosition(AnimationStream stream, Vector3 position)
        {
            CheckIsValid(ref stream);
            SetPositionInternal(ref stream, position);
        }

        public Vector3 GetLocalPosition(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetLocalPositionInternal(ref stream);
        }

        public void SetLocalPosition(AnimationStream stream, Vector3 position)
        {
            CheckIsValid(ref stream);
            SetLocalPositionInternal(ref stream, position);
        }

        public Quaternion GetRotation(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetRotationInternal(ref stream);
        }

        public void SetRotation(AnimationStream stream, Quaternion rotation)
        {
            CheckIsValid(ref stream);
            SetRotationInternal(ref stream, rotation);
        }

        public Quaternion GetLocalRotation(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetLocalRotationInternal(ref stream);
        }

        public void SetLocalRotation(AnimationStream stream, Quaternion rotation)
        {
            CheckIsValid(ref stream);
            SetLocalRotationInternal(ref stream, rotation);
        }

        public Vector3 GetLocalScale(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetLocalScaleInternal(ref stream);
        }

        public void SetLocalScale(AnimationStream stream, Vector3 scale)
        {
            CheckIsValid(ref stream);
            SetLocalScaleInternal(ref stream, scale);
        }

        [ThreadSafe]
        private extern bool HasValidTransform(ref AnimationStream stream);

        [NativeMethod(Name = "TransformSceneHandleBindings::GetPositionInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 GetPositionInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformSceneHandleBindings::SetPositionInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void SetPositionInternal(ref AnimationStream stream, Vector3 position);

        [NativeMethod(Name = "TransformSceneHandleBindings::GetLocalPositionInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 GetLocalPositionInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformSceneHandleBindings::SetLocalPositionInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void SetLocalPositionInternal(ref AnimationStream stream, Vector3 position);

        [NativeMethod(Name = "TransformSceneHandleBindings::GetRotationInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Quaternion GetRotationInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformSceneHandleBindings::SetRotationInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void SetRotationInternal(ref AnimationStream stream, Quaternion rotation);

        [NativeMethod(Name = "TransformSceneHandleBindings::GetLocalRotationInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Quaternion GetLocalRotationInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformSceneHandleBindings::SetLocalRotationInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void SetLocalRotationInternal(ref AnimationStream stream, Quaternion rotation);

        [NativeMethod(Name = "TransformSceneHandleBindings::GetLocalScaleInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 GetLocalScaleInternal(ref AnimationStream stream);

        [NativeMethod(Name = "TransformSceneHandleBindings::SetLocalScaleInternal", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void SetLocalScaleInternal(ref AnimationStream stream, Vector3 scale);
    }

    [NativeHeader("Runtime/Animation/Director/AnimationSceneHandles.h")]
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

        public void SetFloat(AnimationStream stream, float value)
        {
            CheckIsValid(ref stream);
            SetFloatInternal(ref stream, value);
        }

        public int GetInt(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetIntInternal(ref stream);
        }

        public void SetInt(AnimationStream stream, int value)
        {
            CheckIsValid(ref stream);
            SetIntInternal(ref stream, value);
        }

        public bool GetBool(AnimationStream stream)
        {
            CheckIsValid(ref stream);
            return GetBoolInternal(ref stream);
        }

        public void SetBool(AnimationStream stream, bool value)
        {
            CheckIsValid(ref stream);
            SetBoolInternal(ref stream, value);
        }

        [ThreadSafe]
        private extern bool HasValidTransform(ref AnimationStream stream);

        [ThreadSafe]
        private extern bool IsBound(ref AnimationStream stream);

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

        [NativeMethod(Name = "GetBool", IsThreadSafe = true)]
        private extern bool GetBoolInternal(ref AnimationStream stream);

        [NativeMethod(Name = "SetBool", IsThreadSafe = true)]
        private extern void SetBoolInternal(ref AnimationStream stream, bool value);
    }
}

