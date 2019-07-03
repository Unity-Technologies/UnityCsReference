// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager.UI
{
    internal abstract class UpmBaseOperation : IOperation
    {
        public abstract event Action<Error> onOperationError;
        public abstract event Action onOperationSuccess;
        public abstract event Action onOperationFinalized;

        protected string m_PackageName = string.Empty;
        public string packageName
        {
            get
            {
                if (!string.IsNullOrEmpty(m_PackageName))
                    return m_PackageName;
                if (!string.IsNullOrEmpty(m_PackageId))
                    return m_PackageId.Split(new[] { '@' }, 2)[0];
                return string.Empty;
            }
        }

        protected string m_PackageId = string.Empty;
        public string packageId { get { return m_PackageId; } }

        public string packageUniqueId { get { return packageName; } }
        public string versionUniqueId { get { return packageId; } }

        public virtual string specialUniqueId { get { return string.Empty; } }

        // a timestamp is added to keep track of how `refresh` the result it
        // in the case of an online operation, it is the time when the operation starts
        // in the case of an offline operation, it is set to the timestamp of the last online operation
        protected long m_Timestamp = 0;
        public long timestamp { get { return m_Timestamp; } }

        protected long m_LastSuccessTimestamp = 0;
        public long lastSuccessTimestamp { get { return m_LastSuccessTimestamp; } }

        protected bool m_OfflineMode = false;
        public bool isOfflineMode { get { return m_OfflineMode; } }

        public abstract bool isInProgress { get; }

        public Error error { get; protected set; }        // Keep last error
    }

    internal abstract class UpmBaseOperation<T> : UpmBaseOperation where T : Request
    {
        public override event Action<Error> onOperationError = delegate {};
        public override event Action onOperationFinalized = delegate {};
        public override event Action onOperationSuccess = delegate {};
        public Action<T> onProcessResult = delegate {};

        protected T m_Request;

        public override bool isInProgress { get { return m_Request != null && m_Request.Id != 0 && !m_Request.IsCompleted; } }

        protected abstract T CreateRequest();

        protected void Start()
        {
            if (isInProgress)
            {
                Debug.LogError("Unable to start the operation again while it's in progress. " +
                    "Please cancel the operation before re-start or wait until the operation is completed.");
                return;
            }

            if (!isOfflineMode)
                m_Timestamp = DateTime.Now.Ticks;
            m_Request = CreateRequest();
            error = null;
            EditorApplication.update += Progress;
        }

        protected void CancelInternal()
        {
            m_Request = null;
            onOperationError = delegate {};
            onOperationFinalized = delegate {};
            onProcessResult = delegate {};
            EditorApplication.update -= Progress;
        }

        // Common progress code for all classes
        protected void Progress()
        {
            if (m_Request.IsCompleted)
            {
                if (m_Request.Status == StatusCode.Success)
                    OnSuccess();
                else if (m_Request.Status >= StatusCode.Failure)
                    OnError(m_Request.Error);
                else
                    Debug.LogError("Unsupported progress state " + m_Request.Status);
                OnFinalize();
            }
        }

        private void OnError(Error error)
        {
            try
            {
                this.error = error;
                var message = "Cannot perform upm operation";
                message += error == null ? "." : $": {error.message} [{error.errorCode}]";

                Debug.LogError(message);
                onOperationError(error);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Package Manager Window had an error while reporting an error in an operation: {exception}");
            }
        }

        private void OnSuccess()
        {
            try
            {
                onProcessResult(m_Request);
                m_LastSuccessTimestamp = m_Timestamp;
                onOperationSuccess();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Package Manager Window had an error while completing an operation: {exception}");
            }
        }

        private void OnFinalize()
        {
            EditorApplication.update -= Progress;
            onOperationFinalized();

            onOperationError = delegate {};
            onOperationFinalized = delegate {};
            onOperationSuccess = delegate {};
            onProcessResult = delegate {};
        }
    }
}
