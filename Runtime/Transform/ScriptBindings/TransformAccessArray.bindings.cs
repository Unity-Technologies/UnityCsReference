// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;

namespace UnityEngine.Jobs
{
    //@TODO: Static code analysis needs to prevent creation of TransformAccess
    //       except through what is passed into the job function.
    //       Code below assumes this to be true since it doesn't check if TransformAccess is valid
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Transform/ScriptBindings/TransformAccess.bindings.h")]
    public partial struct TransformAccess
    {
        private IntPtr hierarchy;
        private int    index;

        public Vector3 position         { get { Vector3 p; GetPosition(ref this, out p); return p; }         set { SetPosition(ref this, ref value); } }
        public Quaternion rotation      { get { Quaternion r; GetRotation(ref this, out r); return r; }      set { SetRotation(ref this, ref value); } }

        public Vector3 localPosition    { get { Vector3 p; GetLocalPosition(ref this, out p); return p; }    set { SetLocalPosition(ref this, ref value); } }
        public Quaternion localRotation { get { Quaternion r; GetLocalRotation(ref this, out r); return r; } set { SetLocalRotation(ref this, ref value); } }
        public Vector3 localScale       { get { Vector3 s; GetLocalScale(ref this, out s); return s; }       set { SetLocalScale(ref this, ref value); } }

        //@TODO: Static code analysis needs to prevent creation of TransformAccess except through TransformAccessArray accessor.
        // Code below assumes this to be true since it doesn't check if TransformAccess is valid

        [NativeMethod(Name = "TransformAccessBindings::GetPosition", IsThreadSafe = true, IsFreeFunction = true)]
        private static extern void GetPosition(ref TransformAccess access, out Vector3 p);

        [NativeMethod(Name = "TransformAccessBindings::SetPosition", IsThreadSafe = true, IsFreeFunction = true)]
        private static extern void SetPosition(ref TransformAccess access, ref Vector3 p);

        [NativeMethod(Name = "TransformAccessBindings::GetRotation", IsThreadSafe = true, IsFreeFunction = true)]
        private static extern void GetRotation(ref TransformAccess access, out Quaternion r);

        [NativeMethod(Name = "TransformAccessBindings::SetRotation", IsThreadSafe = true, IsFreeFunction = true)]
        private static extern void SetRotation(ref TransformAccess access, ref Quaternion r);


        [NativeMethod(Name = "TransformAccessBindings::GetLocalPosition", IsThreadSafe = true, IsFreeFunction = true)]
        private static extern void GetLocalPosition(ref TransformAccess access, out Vector3 p);

        [NativeMethod(Name = "TransformAccessBindings::SetLocalPosition", IsThreadSafe = true, IsFreeFunction = true)]
        private static extern void SetLocalPosition(ref TransformAccess access, ref Vector3 p);

        [NativeMethod(Name = "TransformAccessBindings::GetLocalRotation", IsThreadSafe = true, IsFreeFunction = true)]
        private static extern void GetLocalRotation(ref TransformAccess access, out Quaternion r);

        [NativeMethod(Name = "TransformAccessBindings::SetLocalRotation", IsThreadSafe = true, IsFreeFunction = true)]
        private static extern void SetLocalRotation(ref TransformAccess access, ref Quaternion r);


        [NativeMethod(Name = "TransformAccessBindings::GetLocalScale", IsThreadSafe = true, IsFreeFunction = true)]
        private static extern void GetLocalScale(ref TransformAccess access, out Vector3 r);

        [NativeMethod(Name = "TransformAccessBindings::SetLocalScale", IsThreadSafe = true, IsFreeFunction = true)]
        private static extern void SetLocalScale(ref TransformAccess access, ref Vector3 r);

        //@TODO: API incomplete...
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeType(Header = "Runtime/Transform/ScriptBindings/TransformAccess.bindings.h", CodegenOptions = CodegenOptions.Custom)]
    public struct TransformAccessArray : IDisposable
    {
        IntPtr              m_TransformArray;


        public TransformAccessArray(Transform[] transforms, int desiredJobCount = -1)
        {
            Allocate(transforms.Length, desiredJobCount, out this);
            SetTransforms(m_TransformArray, transforms);
        }

        public TransformAccessArray(int capacity, int desiredJobCount = -1)
        {
            Allocate(capacity, desiredJobCount, out this);
        }

        public static void Allocate(int capacity, int desiredJobCount, out TransformAccessArray array)
        {
            array.m_TransformArray = Create(capacity, desiredJobCount);

        }

        public bool IsCreated
        {
            get { return m_TransformArray != IntPtr.Zero; }
        }

        public void Dispose()
        {

            DestroyTransformAccessArray(m_TransformArray);
            m_TransformArray = IntPtr.Zero;
        }

        internal IntPtr GetTransformAccessArrayForSchedule()
        {

            return m_TransformArray;
        }

        public Transform this[int index]
        {
            get
            {

                return GetTransform(m_TransformArray, index);
            }
            set
            {

                SetTransform(m_TransformArray, index, value);
            }
        }

        public int Capacity
        {
            get
            {

                return GetCapacity(m_TransformArray);
            }
            set
            {

                SetCapacity(m_TransformArray, value);
            }
        }

        public int Length
        {
            get
            {

                return GetLength(m_TransformArray);
            }
        }

        public void Add(Transform transform)
        {

            Add(m_TransformArray, transform);
        }

        public void RemoveAtSwapBack(int index)
        {

            RemoveAtSwapBack(m_TransformArray, index);
        }

        public void SetTransforms(Transform[] transforms)
        {

            SetTransforms(m_TransformArray, transforms);
        }

        [NativeMethod(Name = "TransformAccessArrayBindings::Create", IsFreeFunction = true)]
        private static extern IntPtr Create(int capacity, int desiredJobCount);

        [NativeMethod(Name = "DestroyTransformAccessArray", IsFreeFunction = true)]
        private static extern void DestroyTransformAccessArray(IntPtr transformArray);

        [NativeMethod(Name = "TransformAccessArrayBindings::SetTransforms", IsFreeFunction = true)]
        private static extern void SetTransforms(IntPtr transformArrayIntPtr, Transform[] transforms);

        [NativeMethod(Name = "TransformAccessArrayBindings::AddTransform", IsFreeFunction = true)]
        private static extern void Add(IntPtr transformArrayIntPtr, Transform transform);

        [NativeMethod(Name = "TransformAccessArrayBindings::RemoveAtSwapBack", IsFreeFunction = true)]
        private static extern void RemoveAtSwapBack(IntPtr transformArrayIntPtr, int index);

        [NativeMethod(Name = "TransformAccessArrayBindings::GetSortedTransformAccess", IsThreadSafe = true, IsFreeFunction = true)]
        internal static extern IntPtr GetSortedTransformAccess(IntPtr transformArrayIntPtr);

        [NativeMethod(Name = "TransformAccessArrayBindings::GetSortedToUserIndex", IsThreadSafe = true, IsFreeFunction = true)]
        internal static extern IntPtr GetSortedToUserIndex(IntPtr transformArrayIntPtr);

        [NativeMethod(Name = "TransformAccessArrayBindings::GetLength", IsFreeFunction = true)]
        internal static extern int GetLength(IntPtr transformArrayIntPtr);

        [NativeMethod(Name = "TransformAccessArrayBindings::GetCapacity", IsFreeFunction = true)]
        internal static extern int GetCapacity(IntPtr transformArrayIntPtr);

        [NativeMethod(Name = "TransformAccessArrayBindings::SetCapacity", IsFreeFunction = true)]
        internal static extern void SetCapacity(IntPtr transformArrayIntPtr, int capacity);

        [NativeMethod(Name = "TransformAccessArrayBindings::GetTransform", IsFreeFunction = true)]
        internal static extern Transform GetTransform(IntPtr transformArrayIntPtr, int index);

        [NativeMethod(Name = "TransformAccessArrayBindings::SetTransform", IsFreeFunction = true)]
        internal static extern void SetTransform(IntPtr transformArrayIntPtr, int index, Transform transform);
    }
}
