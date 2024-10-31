// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal abstract class UpmBaseOperation : IOperation
    {
        public abstract event Action<IOperation, UIError> onOperationError;
        public abstract event Action<IOperation> onOperationSuccess;
        public abstract event Action<IOperation> onOperationFinalized;
        public abstract event Action<IOperation> onOperationProgress;

        public virtual string packageName
        {
            get
            {
                if (!string.IsNullOrEmpty(m_PackageIdOrName))
                    return m_PackageIdOrName.Split(new[] { '@' }, 2)[0];
                return string.Empty;
            }
        }

        [SerializeField]
        protected string m_PackageIdOrName = string.Empty;
        public virtual string packageIdOrName => m_PackageIdOrName;

        public virtual long productId => 0;
        public string packageUniqueId => packageName;

        [SerializeField]
        protected long m_Timestamp = 0;
        public long timestamp => m_Timestamp;

        // a data timestamp is added to keep track of how `fresh` the result is.
        // for online operations, it is the same as the operation timestamp
        // for offline operations, it is set to the timestamp of the last online operation
        [SerializeField]
        protected long m_OfflineDataTimestamp;
        public long dataTimestamp => m_OfflineMode ? m_OfflineDataTimestamp : m_Timestamp;

        [SerializeField]
        protected long m_LastSuccessTimestamp = 0;
        public long lastSuccessTimestamp => m_LastSuccessTimestamp;

        [SerializeField]
        protected bool m_OfflineMode = false;
        public bool isOfflineMode => m_OfflineMode;

        [SerializeField]
        protected bool m_LogErrorInConsole = false;
        public bool logErrorInConsole { get => m_LogErrorInConsole; set => m_LogErrorInConsole = value; }

        public abstract bool isInProgress { get; }

        public bool isInPause => false;

        public bool isProgressVisible => false;

        public bool isProgressTrackable => false;

        public float progressPercentage => 0;

        // Each type of operation can have an operation specific human readable error message.
        // This message should be user friendly and not as technical as the error we receive from the UPM Client.
        protected virtual string operationErrorMessage => string.Empty;

        public abstract RefreshOptions refreshOptions { get; }

        [NonSerialized]
        protected IClientProxy m_ClientProxy;
        [NonSerialized]
        protected IApplicationProxy m_Application;
        public void ResolveDependencies(IClientProxy clientProxy, IApplicationProxy applicationProxy)
        {
            m_ClientProxy = clientProxy;
            m_Application = applicationProxy;
        }
    }

    [Serializable]
    internal abstract class UpmBaseOperation<T> : UpmBaseOperation where T : Request
    {
        public override event Action<IOperation, UIError> onOperationError = delegate {};
        public override event Action<IOperation> onOperationFinalized = delegate {};
        public override event Action<IOperation> onOperationSuccess = delegate {};
        public override event Action<IOperation> onOperationProgress = delegate {};
        public virtual event Action<T> onProcessResult = delegate {};

        [SerializeField]
        protected T m_Request;
        [SerializeField]
        protected bool m_IsCompleted;
        public override bool isInProgress => m_Request != null && m_Request.Id != 0 && !m_IsCompleted;

        protected abstract T CreateRequest();

        protected void Start()
        {
            if (isInProgress)
            {
                Debug.LogError(L10n.Tr("[Package Manager Window] Unable to start the operation again while it's in progress. " +
                    "Please cancel the operation before re-start or wait until the operation is completed."));
                return;
            }

            m_Timestamp = DateTime.Now.Ticks;
            if (!m_Application.isUpmRunning)
            {
                EditorApplication.delayCall += () =>
                {
                    OnError(new UIError(UIErrorCode.UpmError_ServerNotRunning, L10n.Tr("UPM server is not running")));
                    Cancel();
                };
                return;
            }

            try
            {
                m_Request = CreateRequest();
            }
            catch (ArgumentException e)
            {
                OnError(new UIError(UIErrorCode.UpmError_ServerNotRunning, e.Message));
                return;
            }
            m_IsCompleted = false;
            EditorApplication.update += Progress;
        }

        public void Cancel()
        {
            OnFinalize();
            m_Request = null;
        }

        // Common progress code for all classes
        protected void Progress()
        {
            m_IsCompleted = m_Request.IsCompleted;
            if (m_IsCompleted)
            {
                if (m_Request.Status == StatusCode.Success)
                    OnSuccess();
                else if (m_Request.Status >= StatusCode.Failure)
                    OnError(new UIError(m_Request.Error));
                else
                    Debug.LogError(string.Format(L10n.Tr("[Package Manager Window] Unsupported progress state {0}."), m_Request.Status));
                OnFinalize();
            }
        }

        public void RestoreProgress()
        {
            if (isInProgress)
                EditorApplication.update += Progress;
        }

        private void OnError(UIError error)
        {
            if (logErrorInConsole && !error.HasAttribute(UIError.Attribute.DetailInConsole))
            {
                var consoleErrorMessage = operationErrorMessage ?? string.Empty;
                if (error.operationErrorCode >= 500 && error.operationErrorCode < 600)
                {
                    if (!string.IsNullOrEmpty(consoleErrorMessage))
                        consoleErrorMessage += " ";
                    consoleErrorMessage += L10n.Tr("An error occurred, likely on the server. Please try again later.");
                }
                if (!string.IsNullOrEmpty(error.message))
                    consoleErrorMessage += !string.IsNullOrEmpty(consoleErrorMessage) ? $"\n{error.message}" : error.message;
                Debug.LogError(string.Format(L10n.Tr("[Package Manager Window] {0}"), consoleErrorMessage));
                error.attribute |= UIError.Attribute.DetailInConsole;
            }
            onOperationError?.Invoke(this, error);

            PackageManagerOperationErrorAnalytics.SendEvent(GetType().Name, error);
        }

        private void OnSuccess()
        {
            onProcessResult?.Invoke(m_Request);
            m_LastSuccessTimestamp = m_Timestamp;
            onOperationSuccess?.Invoke(this);
        }

        private void OnFinalize()
        {
            EditorApplication.update -= Progress;
            onOperationFinalized?.Invoke(this);

            onOperationError = delegate {};
            onOperationFinalized = delegate {};
            onOperationSuccess = delegate {};
            onOperationProgress = delegate {};
            onProcessResult = delegate {};
        }
    }
}
