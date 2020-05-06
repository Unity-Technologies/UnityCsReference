// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.Requests
{
    /// <summary>
    /// Tracks the state of an asynchronous Upm server operation
    /// </summary>
    public abstract partial class Request : ISerializationCallbackReceiver
    {
        internal const string ShimPackageType = "shim";

        /// <summary>
        /// Note: This property is there to workaround the serializer
        /// that does not know how to handle null values
        /// </summary>
        [SerializeField]
        private bool m_ErrorFetched;

        [SerializeField]
        private Error m_Error;

        [SerializeField]
        private NativeStatusCode m_Status = NativeStatusCode.NotFound;

        [SerializeField]
        private long m_Id;

        internal NativeStatusCode NativeStatus
        {
            get
            {
                if (!m_Status.IsCompleted())
                {
                    m_Status = GetOperationStatus(Id);
                }

                return m_Status;
            }
        }

        internal long Id
        {
            get
            {
                return m_Id;
            }
        }

        /// <summary>
        /// Gets the status of the operation
        /// </summary>
        public StatusCode Status
        {
            get
            {
                FetchNativeData();
                return NativeStatus.ConvertToManaged();
            }
        }

        /// <summary>
        /// Gets whether the operation is completed or not
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                FetchNativeData();
                return NativeStatus.IsCompleted();
            }
        }

        /// <summary>
        /// Gets the error associated to this operation
        /// </summary>
        public Error Error
        {
            get
            {
                FetchNativeData();
                return m_ErrorFetched ? m_Error : null;
            }
        }

        private void FetchError()
        {
            // We assume the request is empty when the Id is 0 and skip error fetching.
            // This happens during the serialization process as empty instances are created
            // in place of null references.
            if (Id == 0 || m_ErrorFetched || NativeStatus.ConvertToManaged() != StatusCode.Failure)
            {
                return;
            }

            m_ErrorFetched = true;
            m_Error = GetOperationError(Id);

            if (m_Error == null)
            {
                if (NativeStatus == NativeStatusCode.NotFound)
                {
                    m_Error = new Error(NativeErrorCode.NotFound, "Operation not found");
                }
                else
                {
                    m_Error = new Error(NativeErrorCode.Unknown, "Unknown error");
                }
            }
        }

        protected virtual void FetchNativeData()
        {
            FetchError();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            FetchNativeData();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }

        ~Request()
        {
            // Do our best to release the native request if it has not been already.
            // The only limitation left is an in-progress request that has not been
            // serialized during domain unload.
            ReleaseCompletedOperation(Id);
        }

        /// <summary>
        /// Constructor to support serialization.  Internal to prevent
        /// API consumers to extend the class.
        /// </summary>
        internal Request()
        {
        }

        internal Request(long operationId, NativeStatusCode initialStatus)
        {
            m_Id = operationId;
            m_Status = initialStatus;
            Debug.AssertFormat(m_Status == NativeStatusCode.Error ? m_Id == 0 : m_Id > 0, "Invalid operation id [status: {0}, id: {1}]", m_Status, m_Id);
        }
    }

    /// <summary>
    /// Tracks the state of an asynchronous Upm server operation that returns a non-empty response
    /// </summary>
    public abstract class Request<T> : Request
    {
        /// <summary>
        /// Note: This property is there to workaround the serializer
        /// that does not know how to handle null values
        /// </summary>
        [SerializeField]
        private bool m_ResultFetched = false;

        [SerializeField]
        private T m_Result = default(T);

        protected abstract T GetResult();

        private void FetchResult()
        {
            if (m_ResultFetched || NativeStatus.ConvertToManaged() != StatusCode.Success)
            {
                return;
            }

            m_ResultFetched = true;
            m_Result = GetResult();
        }

        protected sealed override void FetchNativeData()
        {
            FetchResult();
            base.FetchNativeData();
        }

        /// <summary>
        /// Gets the result of the operation
        /// </summary>
        public T Result
        {
            get
            {
                FetchNativeData();
                return m_ResultFetched ? m_Result : default(T);
            }
        }

        /// <summary>
        /// Constructor to support serialization.  Internal to prevent
        /// API consumers to extend the class.
        /// </summary>
        internal Request()
            : base()
        {
        }

        internal Request(long operationId, NativeStatusCode initialStatus)
            : base(operationId, initialStatus)
        {
        }
    }
}
