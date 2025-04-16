// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
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
        internal static CancellationTokenSource s_GlobalCancellation = new();

        internal Object[] m_Result;
        private CancellationToken m_CancellationToken;

        public Object[] Result { get { return m_Result; } }

        [NativeMethod("IsWaitingForSceneActivation")]
        public extern bool IsWaitingForSceneActivation();

        [NativeMethod("WaitForCompletion")]
        public extern void WaitForCompletion();

        [NativeMethod("Cancel")]
        public extern void Cancel();

        [StaticAccessor("GetAsyncInstantiateManager()", StaticAccessorType.Dot)]
        internal extern static float IntegrationTimeMS { get; set; }

        public AsyncInstantiateOperation() : this(IntPtr.Zero, default)
        {
        }

        protected AsyncInstantiateOperation(IntPtr ptr, CancellationToken cancellationToken) : base(ptr)
        {
            m_CancellationToken = CancellationTokenSource.CreateLinkedTokenSource(s_GlobalCancellation.Token, cancellationToken).Token;
        }

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

        internal static new class BindingsMarshaller
        {
            public static AsyncInstantiateOperation ConvertToManaged(IntPtr ptr) => new AsyncInstantiateOperation(ptr, CancellationToken.None);
            public static IntPtr ConvertToNative(AsyncInstantiateOperation obj) => obj.m_Ptr;
        }

        [RequiredByNativeCode(GenerateProxy =true)]
        private bool IsCancellationRequested()
        {
            return m_CancellationToken.IsCancellationRequested;
        }

        internal virtual Object[] CreateResultArray(int size)
        {
            m_Result = new Object[size];
            return m_Result;
        }
    }

    public class AsyncInstantiateOperation<T> : AsyncInstantiateOperation
    {
        internal AsyncInstantiateOperation(IntPtr ptr, CancellationToken cancellationToken) : base(ptr, cancellationToken)
        {
        }

        public new T[] Result
        {
            get
            {
                return (T[])(object)m_Result;
            }
        }

        internal override Object[] CreateResultArray(int size)
        {
            m_Result = (Object[])(object)new T[size];
            return m_Result;
        }

        internal static new class BindingsMarshaller
        {
            public static AsyncInstantiateOperation<T> ConvertToManaged(IntPtr ptr) => new AsyncInstantiateOperation<T>(ptr, CancellationToken.None);
            public static IntPtr ConvertToNative(AsyncInstantiateOperation<T> obj) => obj.m_Ptr;
        }


        [ExcludeFromDocs]
        public Awaiter GetAwaiter()
        {
            return new Awaiter(this);
        }
        [ExcludeFromDocs]
        public struct Awaiter : INotifyCompletion
        {
            private readonly Awaitable _awaitable;
            private readonly AsyncInstantiateOperation<T> _op;

            public Awaiter(AsyncInstantiateOperation<T> op)
            {
                _awaitable = Awaitable.FromAsyncOperation(op);
                _op = op;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                _awaitable.SetContinuation(continuation);
            }

            public bool IsCompleted => _awaitable.IsCompleted;
            public T[] GetResult()
            {
                _awaitable.GetAwaiter().GetResult();
                return _op.Result;
            }
        }
    }

    [RequiredByNativeCode]
    internal class AsyncInstantiateOperationHelper
    {
        [RequiredByNativeCode]
        public static Object[] CreateAsyncInstantiateOperationResultArray(AsyncInstantiateOperation op, int size)
        {
            return op.CreateResultArray(size);
        }
    }
}
