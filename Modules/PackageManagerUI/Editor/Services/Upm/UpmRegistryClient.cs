// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IUpmRegistryClient : IService
    {
        event Action<int> onRegistriesAdded;
        event Action onRegistriesModified;
        event Action<string, UIError> onRegistryOperationError;

        void AddRegistry(string name, string url, string[] scopes);
        void UpdateRegistry(string oldName, string newName, string url, string[] scopes);
        void RemoveRegistry(string name);

        void AddRegistryDryRun(string name, string url, string[] scopes, Action<RegistryInfo> successCallback = null, Action<UIError> errorCallback = null);
        void UpdateRegistryDryRun(string oldName, string newName, string url, string[] scopes, Action<RegistryInfo> successCallback = null, Action<UIError> errorCallback = null);

        void CheckRegistriesChanged();
    }

    [Serializable]
    internal class UpmRegistryClient : BaseService<IUpmRegistryClient>, IUpmRegistryClient, ISerializationCallbackReceiver
    {
        public event Action<int> onRegistriesAdded = delegate {};
        public event Action onRegistriesModified = delegate {};
        public event Action<string, UIError> onRegistryOperationError = delegate {};

        [SerializeField]
        private UpmGetRegistriesOperation m_GetRegistriesOperation;
        private UpmGetRegistriesOperation getRegistriesOperation => CreateOperation(ref m_GetRegistriesOperation);

        [SerializeField]
        private UpmAddRegistryOperation m_AddRegistryOperation;
        private UpmAddRegistryOperation addRegistryOperation => CreateOperation(ref m_AddRegistryOperation);

        [SerializeField]
        private UpmUpdateRegistryOperation m_UpdateRegistryOperation;
        private UpmUpdateRegistryOperation updateRegistryOperation => CreateOperation(ref m_UpdateRegistryOperation);

        [SerializeField]
        private UpmRemoveRegistryOperation m_RemoveRegistryOperation;
        private UpmRemoveRegistryOperation removeRegistryOperation => CreateOperation(ref m_RemoveRegistryOperation);

        private readonly IProjectSettingsProxy m_SettingsProxy;
        private readonly IClientProxy m_ClientProxy;
        private readonly IUpmCache m_UpmCache;
        private readonly IApplicationProxy m_ApplicationProxy;
        public UpmRegistryClient(IProjectSettingsProxy settingsProxy,
            IClientProxy clientProxy,
            IUpmCache upmCache,
            IApplicationProxy applicationProxy)
        {
            m_SettingsProxy = RegisterDependency(settingsProxy);
            m_ClientProxy = RegisterDependency(clientProxy);
            m_UpmCache = RegisterDependency(upmCache);
            m_ApplicationProxy = RegisterDependency(applicationProxy);
        }

        public override void OnEnable()
        {
            m_UpmCache.onScopedRegistriesPotentiallyChanged += CheckRegistriesChanged;
        }

        public override void OnDisable()
        {
            m_UpmCache.onScopedRegistriesPotentiallyChanged -= CheckRegistriesChanged;
        }

        public void AddRegistry(string name, string url, string[] scopes)
        {
            addRegistryOperation.Add(name, url, scopes);
            addRegistryOperation.onProcessResult += OnProcessAddRegistryResult;
            addRegistryOperation.onOperationError += (op, error) => onRegistryOperationError?.Invoke(name, error);
        }

        private void OnProcessAddRegistryResult(AddScopedRegistryRequest request)
        {
            var result = request.Result;
            if (m_SettingsProxy.AddRegistry(result))
            {
                m_SettingsProxy.Save();
                onRegistriesModified?.Invoke();
                m_ClientProxy.Resolve();
            }
        }

        public void UpdateRegistry(string oldName, string newName, string url, string[] scopes)
        {
            updateRegistryOperation.Update(oldName, newName, url, scopes);
            updateRegistryOperation.onProcessResult += OnProcessUpdateRegistryResult;
            updateRegistryOperation.onOperationError += (op, error) => onRegistryOperationError?.Invoke(oldName, error);
        }

        private void OnProcessUpdateRegistryResult(UpdateScopedRegistryRequest request)
        {
            var result = request.Result;
            if (m_SettingsProxy.UpdateRegistry(updateRegistryOperation.registryName, result))
            {
                m_SettingsProxy.Save();
                onRegistriesModified?.Invoke();
                m_ClientProxy.Resolve();
            }
        }

        public void RemoveRegistry(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            removeRegistryOperation.Remove(name);
            removeRegistryOperation.onProcessResult += OnProcessRemoveRegistryResult;
            removeRegistryOperation.onOperationError += (op, error) => onRegistryOperationError?.Invoke(name, error);
        }

        public void AddRegistryDryRun(string name, string url, string[] scopes, Action<RegistryInfo> successCallback = null, Action<UIError> errorCallback = null)
        {
            var operation = new UpmAddRegistryOperation();
            operation.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            operation.Add(name, url, scopes, true);
            if (successCallback != null)
                operation.onProcessResult += request => successCallback.Invoke(request.Result);
            if (errorCallback != null)
                operation.onOperationError += (_, error) => errorCallback.Invoke(error);
        }

        public void UpdateRegistryDryRun(string oldName, string newName, string url, string[] scopes, Action<RegistryInfo> successCallback = null, Action<UIError> errorCallback = null)
        {
            var operation = new UpmUpdateRegistryOperation();
            operation.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            operation.Update(oldName, newName, url, scopes, true);
            if (successCallback != null)
                operation.onProcessResult += request => successCallback.Invoke(request.Result);
            if (errorCallback != null)
                operation.onOperationError += (_, error) => errorCallback.Invoke(error);
        }

        private void OnProcessRemoveRegistryResult(RemoveScopedRegistryRequest request)
        {
            if (m_SettingsProxy.RemoveRegistry(removeRegistryOperation.registryName))
            {
                m_SettingsProxy.Save();
                onRegistriesModified?.Invoke();
                m_ClientProxy.Resolve();
            }
        }

        public void CheckRegistriesChanged()
        {
            if (Unsupported.IsRegistryValidationDisabled)
                return;

            if (getRegistriesOperation.isInProgress)
                getRegistriesOperation.Cancel();
            getRegistriesOperation.GetRegistries();
            getRegistriesOperation.onProcessResult += OnProcessGetRegistriesResult;
            getRegistriesOperation.logErrorInConsole = true;
        }

        private void OnProcessGetRegistriesResult(GetRegistriesRequest request)
        {
            var registriesListResult = request.Result ?? new RegistryInfo[0];
            var registriesCount = registriesListResult.Length;

            if (m_SettingsProxy.registries.Any() && m_SettingsProxy.registries.Count < registriesCount)
                onRegistriesAdded?.Invoke(registriesCount - m_SettingsProxy.registries.Count);

            if (!registriesListResult.IsEquivalentTo(m_SettingsProxy.registries))
            {
                var name = registriesListResult.FirstOrDefault(r => !m_SettingsProxy.registries.Any(r.IsEquivalentTo))?.name;
                if (!string.IsNullOrEmpty(name))
                    m_SettingsProxy.SelectRegistry(name);

                m_SettingsProxy.SetRegistries(registriesListResult);
                m_SettingsProxy.Save();
                onRegistriesModified?.Invoke();
            }
        }

        private T CreateOperation<T>(ref T operation) where T : UpmBaseOperation, new()
        {
            if (operation != null)
                return operation;

            operation = new T();
            operation.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            return operation;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            m_GetRegistriesOperation?.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            m_AddRegistryOperation?.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            m_UpdateRegistryOperation?.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
            m_RemoveRegistryOperation?.ResolveDependencies(m_ClientProxy, m_ApplicationProxy);
        }
    }
}
