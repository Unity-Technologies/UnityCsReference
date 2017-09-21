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
    public abstract class Request
    {
        [SerializeField]
        private bool m_ErrorFetched;

        [SerializeField]
        private Error m_Error;

        [SerializeField]
        private NativeClient.StatusCode m_Status = NativeClient.StatusCode.NotFound;

        [SerializeField]
        private long m_Id;

        private NativeClient.StatusCode NativeStatusCode
        {
            get
            {
                if (m_Status <= NativeClient.StatusCode.InProgress)
                {
                    m_Status = NativeClient.GetOperationStatus(Id);
                }

                return m_Status;
            }
        }

        protected long Id
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
                switch (NativeStatusCode)
                {
                    case NativeClient.StatusCode.InProgress:
                    case NativeClient.StatusCode.InQueue:
                        return StatusCode.InProgress;
                    case NativeClient.StatusCode.Error:
                    case NativeClient.StatusCode.NotFound:
                        return StatusCode.Failure;
                    case NativeClient.StatusCode.Done:
                        return StatusCode.Success;
                }

                throw new NotSupportedException(String.Format("Unknown native status code {0}", NativeStatusCode));
            }
        }

        /// <summary>
        /// Gets whether the operation is completed or not
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                return (Status > StatusCode.InProgress);
            }
        }

        /// <summary>
        /// Gets the error associated to this operation
        /// </summary>
        public Error Error
        {
            get
            {
                if (!m_ErrorFetched && Status == StatusCode.Failure)
                {
                    m_ErrorFetched = true;
                    m_Error = NativeClient.GetOperationError(Id);

                    if (m_Error == null)
                    {
                        if (NativeStatusCode == NativeClient.StatusCode.NotFound)
                        {
                            m_Error = new Error(ErrorCode.NotFound, "Operation not found");
                        }
                        else
                        {
                            m_Error = new Error(ErrorCode.Unknown, "Unknown error");
                        }
                    }
                }

                return m_Error;
            }
        }

        /// <summary>
        /// Constructor to support serialization.  Internal to prevent
        /// API consumers to extend the class.
        /// </summary>
        internal Request()
        {
        }

        internal Request(long operationId, NativeClient.StatusCode initialStatus)
        {
            m_Id = operationId;
            m_Status = initialStatus;
        }
    }

    /// <summary>
    /// Tracks the state of an asynchronous Upm server operation that returns a non-empty response
    /// </summary>
    public abstract class Request<T> : Request
    {
        [SerializeField]
        private bool m_ResultFetched = false;

        [SerializeField]
        private T m_Result = default(T);

        protected abstract T GetResult();

        /// <summary>
        /// Gets the result of the operation
        /// </summary>
        public T Result
        {
            get
            {
                if (!m_ResultFetched && Status == StatusCode.Success)
                {
                    m_ResultFetched = true;
                    m_Result = GetResult();
                }

                return m_Result;
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

        internal Request(long operationId, NativeClient.StatusCode initialStatus)
            : base(operationId, initialStatus)
        {
        }
    }
}

