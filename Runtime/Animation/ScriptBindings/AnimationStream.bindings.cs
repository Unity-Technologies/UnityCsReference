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
    internal enum AnimatorBindingsVersion
    {
        // Invalid.
        kInvalidNotNative = 0,       // Created in C# (with new)
        kInvalidUnresolved = 1,      // Created in C++, but still unresolved

        // Valid.
        kValidMinVersion = 2         // Minimum valid version
    }

    [NativeHeader("Runtime/Animation/ScriptBindings/AnimationStream.bindings.h")]
    [NativeHeader("Runtime/Animation/Director/AnimationStream.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct AnimationStream
    {
        private UInt32 m_AnimatorBindingsVersion;

        private System.IntPtr constant;
        private System.IntPtr input;
        private System.IntPtr output;
        private System.IntPtr workspace;
        private System.IntPtr inputStreamAccessor;
        private System.IntPtr animationHandleBinder;

        internal const int InvalidIndex = ~0;

        internal UInt32 animatorBindingsVersion
        {
            get { return m_AnimatorBindingsVersion; }
        }

        public bool isValid
        {
            get
            {
                return m_AnimatorBindingsVersion >= (UInt32)AnimatorBindingsVersion.kValidMinVersion &&
                    constant != System.IntPtr.Zero &&
                    input != System.IntPtr.Zero &&
                    output != System.IntPtr.Zero &&
                    workspace != System.IntPtr.Zero &&
                    animationHandleBinder != System.IntPtr.Zero;
            }
        }

        internal void CheckIsValid()
        {
            if (!isValid)
                throw new InvalidOperationException("The AnimationStream is invalid.");
        }

        public float deltaTime
        {
            get { CheckIsValid(); return GetDeltaTime(); }
        }

        public Vector3 velocity
        {
            get { CheckIsValid(); return GetVelocity(); }
            set { CheckIsValid(); SetVelocity(value); }
        }

        public Vector3 angularVelocity
        {
            get { CheckIsValid(); return GetAngularVelocity(); }
            set { CheckIsValid(); SetAngularVelocity(value); }
        }

        public Vector3 rootMotionPosition
        {
            get { CheckIsValid(); return GetRootMotionPosition(); }
        }

        public Quaternion rootMotionRotation
        {
            get { CheckIsValid(); return GetRootMotionRotation(); }
        }

        public bool isHumanStream
        {
            get { CheckIsValid(); return GetIsHumanStream(); }
        }

        public AnimationHumanStream AsHuman()
        {
            CheckIsValid();
            if (!GetIsHumanStream())
                throw new InvalidOperationException("Cannot create an AnimationHumanStream for a generic rig.");

            return GetHumanStream();
        }

        public int inputStreamCount
        {
            get { CheckIsValid(); return GetInputStreamCount(); }
        }

        public AnimationStream GetInputStream(int index)
        {
            CheckIsValid();
            return InternalGetInputStream(index);
        }

        private void ReadSceneTransforms() { CheckIsValid(); InternalReadSceneTransforms(); }
        private void WriteSceneTransforms() { CheckIsValid(); InternalWriteSceneTransforms(); }


        [NativeMethod(IsThreadSafe = true)]
        private extern float GetDeltaTime();

        [NativeMethod(IsThreadSafe = true)]
        private extern bool GetIsHumanStream();

        [NativeMethod(Name = "AnimationStreamBindings::GetVelocity", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 GetVelocity();

        [NativeMethod(Name = "AnimationStreamBindings::SetVelocity", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void SetVelocity(Vector3 velocity);

        [NativeMethod(Name = "AnimationStreamBindings::GetAngularVelocity", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 GetAngularVelocity();

        [NativeMethod(Name = "AnimationStreamBindings::SetAngularVelocity", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern void SetAngularVelocity(Vector3 velocity);

        [NativeMethod(Name = "AnimationStreamBindings::GetRootMotionPosition", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Vector3 GetRootMotionPosition();

        [NativeMethod(Name = "AnimationStreamBindings::GetRootMotionRotation", IsFreeFunction = true, IsThreadSafe = true, HasExplicitThis = true)]
        private extern Quaternion GetRootMotionRotation();

        [NativeMethod(IsThreadSafe = true)]
        private extern int GetInputStreamCount();

        [NativeMethod(Name = "GetInputStream", IsThreadSafe = true)]
        private extern AnimationStream InternalGetInputStream(int index);

        [NativeMethod(IsThreadSafe = true)]
        private extern AnimationHumanStream GetHumanStream();

        [NativeMethod(Name = "ReadSceneTransforms", IsThreadSafe = true)]
        private extern void InternalReadSceneTransforms();

        [NativeMethod(Name = "WriteSceneTransforms", IsThreadSafe = true)]
        private extern void InternalWriteSceneTransforms();
    }
}
