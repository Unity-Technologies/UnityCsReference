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
        public long timestamp { get { return m_Timestamp; } }

        [SerializeField]
        protected long m_LastSuccessTimestamp = 0;
        public long lastSuccessTimestamp { get { return m_LastSuccessTimestamp; } }

        [SerializeField]
        protected bool m_OfflineMode = false;
        public bool isOfflineMode { get { return m_OfflineMode; } }

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
        protected ClientProxy m_ClientProxy;
        [NonSerialized]
        protected ApplicationProxy m_ApplicationProxy;
        public void ResolveDependencies(ClientProxy clientProxy, ApplicationProxy applicationProxy)
        {
            m_ClientProxy = clientProxy;
            m_ApplicationProxy = applicationProxy;
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
        public override bool isInProgress { get { return m_Request != null && m_Request.Id != 0 && !m_IsCompleted; } }

        protected abstract T CreateRequest();

        protected void Start()
        {
            if (isInProgress)
            {
                Debug.LogError(L10n.Tr("[Package Manager Window] Unable to start the operation again while it's in progress. " +
                    "Please cancel the operation before re-start or wait until the operation is completed."));
                return;
            }

            if (!isOfflineMode)
                m_Timestamp = DateTime.Now.Ticks;
            // Usually the timestamp for an offline operation is the last success timestamp of its online equivalence (to indicate the freshness of the data)
            // But in the rare case where we start an offline operation before an online one, we use the start timestamp of the editor instead of 0,
            // because we consider a `0` refresh timestamp as `not initialized`/`no refreshes have been done`.
            else if (m_Timestamp == 0)
                m_Timestamp = DateTime.Now.Ticks - (long)(EditorApplication.timeSinceStartup * TimeSpan.TicksPerSecond);

            if (!m_ApplicationProxy.isUpmRunning)
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
