// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmCacheRootClient
    {
        public virtual event Action<CacheRootConfig> onGetCacheRootOperationResult = delegate {};
        public virtual event Action<UIError> onGetCacheRootOperationError = delegate {};
        public virtual event Action<CacheRootConfig> onSetCacheRootOperationResult = delegate {};
        public virtual event Action<UIError, string> onSetCacheRootOperationError = delegate {};
        public virtual event Action<CacheRootConfig> onClearCacheRootOperationResult = delegate {};
        public virtual event Action<UIError> onClearCacheRootOperationError = delegate {};

        [SerializeField]
        private UpmGetCacheRootOperation m_GetCacheRootOperation;
        private UpmGetCacheRootOperation getCacheRootOperation => CreateOperation(ref m_GetCacheRootOperation);

        [SerializeField]
        private UpmSetCacheRootOperation m_SetCacheRootOperation;
        private UpmSetCacheRootOperation setCacheRootOperation => CreateOperation(ref m_SetCacheRootOperation);

        [SerializeField]
        private UpmClearCacheRootOperation m_ClearCacheRootOperation;
        private UpmClearCacheRootOperation clearCacheRootOperation => CreateOperation(ref m_ClearCacheRootOperation);

        [NonSerialized]
        private ClientProxy m_ClientProxy;
        [NonSerialized]
        private ApplicationProxy m_ApplicationProxy;
        public void ResolveDependencies(ClientProxy clientProxy,
            ApplicationProxy applicationProxy)
        {
            m_ClientProxy = clientProxy;
            m_ApplicationProxy = applicationProxy;

            m_GetCacheRootOperation?.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            m_SetCacheRootOperation?.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            m_ClearCacheRootOperation?.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
        }

        public virtual void GetCacheRoot()
        {
            if (m_GetCacheRootOperation?.isInProgress ?? false)
                getCacheRootOperation.Cancel();

            getCacheRootOperation.GetCacheRoot();
            getCacheRootOperation.onProcessResult += req => onGetCacheRootOperationResult?.Invoke(req?.Result);
            getCacheRootOperation.onOperationError += (op, error) => onGetCacheRootOperationError?.Invoke(error);
        }

        public virtual void SetCacheRoot(string path)
        {
            if (m_SetCacheRootOperation?.isInProgress ?? true)
                setCacheRootOperation.Cancel();

            setCacheRootOperation.SetCacheRoot(path);
            setCacheRootOperation.onProcessResult += req => onSetCacheRootOperationResult?.Invoke(req?.Result);
            setCacheRootOperation.onOperationError += (op, error) => onSetCacheRootOperationError?.Invoke(error, path);
        }

        public virtual void ClearCacheRoot()
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
            operation.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            return operation;
        }
    }
}
