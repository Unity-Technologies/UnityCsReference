// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IUpmCacheRootClient : IService
    {
        event Action<CacheRootConfig> onGetCacheRootOperationResult;
        event Action<UIError> onGetCacheRootOperationError;
        event Action<CacheRootConfig> onSetCacheRootOperationResult;
        event Action<UIError, string> onSetCacheRootOperationError;
        event Action<CacheRootConfig> onClearCacheRootOperationResult;
        event Action<UIError> onClearCacheRootOperationError;

        void GetCacheRoot();
        void SetCacheRoot(string path);
        void ClearCacheRoot();
    }

    [Serializable]
    internal class UpmCacheRootClient : BaseService<IUpmCacheRootClient>, IUpmCacheRootClient, ISerializationCallbackReceiver
    {
        public event Action<CacheRootConfig> onGetCacheRootOperationResult = delegate {};
        public event Action<UIError> onGetCacheRootOperationError = delegate {};
        public event Action<CacheRootConfig> onSetCacheRootOperationResult = delegate {};
        public event Action<UIError, string> onSetCacheRootOperationError = delegate {};
        public event Action<CacheRootConfig> onClearCacheRootOperationResult = delegate {};
        public event Action<UIError> onClearCacheRootOperationError = delegate {};

        [SerializeField]
        private UpmGetCacheRootOperation m_GetCacheRootOperation;
        private UpmGetCacheRootOperation getCacheRootOperation => CreateOperation(ref m_GetCacheRootOperation);

        [SerializeField]
        private UpmSetCacheRootOperation m_SetCacheRootOperation;
        private UpmSetCacheRootOperation setCacheRootOperation => CreateOperation(ref m_SetCacheRootOperation);

        [SerializeField]
        private UpmClearCacheRootOperation m_ClearCacheRootOperation;
        private UpmClearCacheRootOperation clearCacheRootOperation => CreateOperation(ref m_ClearCacheRootOperation);

        private readonly IClientProxy m_ClientProxy;
        private readonly IApplicationProxy m_Application;
        public UpmCacheRootClient(IClientProxy clientProxy, IApplicationProxy applicationProxy)
        {
            m_ClientProxy = RegisterDependency(clientProxy);
            m_Application = RegisterDependency(applicationProxy);
        }

        public void GetCacheRoot()
        {
            if (m_GetCacheRootOperation?.isInProgress ?? false)
                getCacheRootOperation.Cancel();

            getCacheRootOperation.GetCacheRoot();
            getCacheRootOperation.onProcessResult += req => onGetCacheRootOperationResult?.Invoke(req?.Result);
            getCacheRootOperation.onOperationError += (op, error) => onGetCacheRootOperationError?.Invoke(error);
        }

        public void SetCacheRoot(string path)
        {
            if (m_SetCacheRootOperation?.isInProgress ?? true)
                setCacheRootOperation.Cancel();

            setCacheRootOperation.SetCacheRoot(path);
            setCacheRootOperation.onProcessResult += req => onSetCacheRootOperationResult?.Invoke(req?.Result);
            setCacheRootOperation.onOperationError += (op, error) => onSetCacheRootOperationError?.Invoke(error, path);
        }

        public void ClearCacheRoot()
        {
            if (m_ClearCacheRootOperation?.isInProgress ?? false)
                clearCacheRootOperation.Cancel();

            clearCacheRootOperation.ClearCacheRoot();
            clearCacheRootOperation.onProcessResult += req => onClearCacheRootOperationResult?.Invoke(req?.Result);
            clearCacheRootOperation.onOperationError += (op, error) => onClearCacheRootOperationError?.Invoke(error);
        }

        private T CreateOperation<T>(ref T operation) where T : UpmBaseOperation, new()
        {
            if (operation != null)
                return operation;

            operation = new T();
            operation.ResolveDependencies(m_ClientProxy, m_Application);
            return operation;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            m_GetCacheRootOperation?.ResolveDependencies(m_ClientProxy, m_Application);
            m_SetCacheRootOperation?.ResolveDependencies(m_ClientProxy, m_Application);
            m_ClearCacheRootOperation?.ResolveDependencies(m_ClientProxy, m_Application);
        }
    }
}
