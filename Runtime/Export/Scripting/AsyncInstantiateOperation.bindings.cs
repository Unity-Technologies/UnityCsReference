// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Asynchronous operation for GameObject.Instantiate
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeHeader("Runtime/GameCode/AsyncInstantiate/AsyncInstantiateOperation.h")]
    public class AsyncInstantiateOperation : AsyncOperation
    {
        internal Object[] m_Result;
      
        public Object[] Result { get { return m_Result; } }

        [NativeMethod("IsWaitingForSceneActivation")]
        public extern bool IsWaitingForSceneActivation();

        [NativeMethod("WaitForCompletion")]
        public extern void WaitForCompletion();

        [NativeMethod("Cancel")]
        public extern void Cancel();

        [StaticAccessor("GetAsyncInstantiateManager()", StaticAccessorType.Dot)]
        internal extern static float IntegrationTimeMS { get; set; }

        public AsyncInstantiateOperation() { }

        public static float GetIntegrationTimeMS()
        {
            return IntegrationTimeMS;
        }

        public static void SetIntegrationTimeMS(float integrationTimeMS)
        {
            if (integrationTimeMS <= 0)
                throw new ArgumentOutOfRangeException("integrationTimeMS", "integrationTimeMS was out of range. Must be greater than zero.");

            IntegrationTimeMS = integrationTimeMS;
        }
    }

    [ExcludeFromDocs]
    public class AsyncInstantiateOperation<T> : CustomYieldInstruction where T : UnityEngine.Object
    {
        internal AsyncInstantiateOperation m_op;

        internal AsyncInstantiateOperation(AsyncInstantiateOperation op)
        {
            m_op = op;
        }

        public override bool keepWaiting => !m_op.isDone;

        public AsyncInstantiateOperation GetOperation() => m_op;
        public static implicit operator AsyncInstantiateOperation(AsyncInstantiateOperation<T> generic) => generic.m_op;

        public bool IsWaitingForSceneActivation() => m_op.IsWaitingForSceneActivation();
        public event System.Action<AsyncOperation> completed
        {
            add => m_op.completed += value;
            remove => m_op.completed -= value;
        }
        public bool isDone
        {
            get => m_op.isDone;
        }
        public float progress
        {
            get => m_op.progress;
        }
        public bool allowSceneActivation
        {
            get => m_op.allowSceneActivation;
            set => m_op.allowSceneActivation = value;
        }

        public void WaitForCompletion() => m_op.WaitForCompletion();
        public void Cancel() => m_op.Cancel();
        public T[] Result
        {
            get
            {
                var objArr = m_op.Result;
                return UnsafeUtility.As<Object[], T[]>(ref objArr);
            }
        }
    }

    [RequiredByNativeCode]
    internal class AsyncInstantiateOperationHelper
    {
        [RequiredByNativeCode]
        public static void SetAsyncInstantiateOperationResult(AsyncInstantiateOperation op, UnityEngine.Object[] result)
        {
            op.m_Result = result;
        }
    }
}
