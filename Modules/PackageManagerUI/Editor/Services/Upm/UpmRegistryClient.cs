// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class UpmRegistryClient
    {
        public virtual event Action<int> onRegistriesAdded = delegate {};
        public virtual event Action onRegistriesModified = delegate {};
        public virtual event Action<string, UIError> onRegistryOperationError = delegate {};

        [SerializeField]
        private UpmGetRegistriesOperation m_GetRegistriesOperation;
        private UpmGetRegistriesOperation getRegistriesOperation => m_GetRegistriesOperation ?? (m_GetRegistriesOperation = new UpmGetRegistriesOperation());
        [SerializeField]
        private UpmAddRegistryOperation m_AddRegistryOperation;
        private UpmAddRegistryOperation addRegistryOperation => m_AddRegistryOperation ?? (m_AddRegistryOperation = new UpmAddRegistryOperation());
        [SerializeField]
        private UpmUpdateRegistryOperation m_UpdateRegistryOperation;
        private UpmUpdateRegistryOperation updateRegistryOperation => m_UpdateRegistryOperation ?? (m_UpdateRegistryOperation = new UpmUpdateRegistryOperation());
        [SerializeField]
        private UpmRemoveRegistryOperation m_RemoveRegistryOperation;
        private UpmRemoveRegistryOperation removeRegistryOperation => m_RemoveRegistryOperation ?? (m_RemoveRegistryOperation = new UpmRemoveRegistryOperation());

        [NonSerialized]
        private UpmCache m_UpmCache;
        [NonSerialized]
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        public void ResolveDependencies(UpmCache upmCache,
            PackageManagerProjectSettingsProxy settingsProxy)
        {
            m_UpmCache = upmCache;
            m_SettingsProxy = settingsProxy;
        }

        public virtual void AddRegistry(string name, string url, string[] scopes)
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
                Client.Resolve();
            }
        }

        public virtual void UpdateRegistry(string oldName, string newName, string url, string[] scopes)
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
                Client.Resolve();
            }
        }

        public virtual void RemoveRegistry(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            var installedPackageInfoOnRegistry = m_UpmCache.installedPackageInfos.Where(p => p.registry?.name == name);
            if (installedPackageInfoOnRegistry.Any())
            {
                Debug.LogError(string.Format(L10n.Tr("[Package Manager Window] There are packages installed from the registry {0}. Please remove the packages before removing the registry."), name));
                return;
            }
            removeRegistryOperation.Remove(name);
            removeRegistryOperation.onProcessResult += OnProcessRemoveRegistryResult;
            removeRegistryOperation.onOperationError += (op, error) => onRegistryOperationError?.Invoke(name, error);
        }

        private void OnProcessRemoveRegistryResult(RemoveScopedRegistryRequest request)
        {
            if (m_SettingsProxy.RemoveRegistry(removeRegistryOperation.registryName))
            {
                m_SettingsProxy.Save();
                onRegistriesModified?.Invoke();
                Client.Resolve();
            }
        }

        public void CheckRegistriesChanged()
        {
            if (getRegistriesOperation.isInProgress)
                getRegistriesOperation.Cancel();
            getRegistriesOperation.GetRegistries();
            getRegistriesOperation.onProcessResult += OnProcessGetRegistriesResult;
            getRegistriesOperation.onOperationError += (op, error) => Debug.LogError(error);
        }

        private void OnProcessGetRegistriesResult(GetRegistriesRequest request)
        {
            var registriesListResult = request.Result ?? new RegistryInfo[0];
            var registriesCount = registriesListResult.Length;

            if (m_SettingsProxy.registries.Any() && m_SettingsProxy.registries.Count < registriesCount)
                onRegistriesAdded?.Invoke(registriesCount - m_SettingsProxy.registries.Count);

            if (!registriesListResult.SequenceEqual(m_SettingsProxy.registries, new RegistryInfoComparer()))
            {
                m_SettingsProxy.SetRegistries(registriesListResult);
                m_SettingsProxy.Save();
                onRegistriesModified?.Invoke();
            }
        }

        internal class RegistryInfoComparer : IEqualityComparer<RegistryInfo>
        {
            public bool Equals(RegistryInfo x, RegistryInfo y)
            {
                if (x == y)
                    return true;

                if (x == null || y == null)
                    return false;

                var equals = (x.id ?? string.Empty) == (y.id ?? string.Empty) &&
                    (x.name ?? string.Empty) == (y.name ?? string.Empty) &&
                    (x.url ?? string.Empty) == (y.url ?? string.Empty) &&
                    x.isDefault == y.isDefault;

                if (!equals)
                    return false;

                var xScopes = x.scopes ?? new string[0];
                var yScopes = y.scopes ?? new string[0];

                return xScopes.Where(s => !string.IsNullOrEmpty(s)).SequenceEqual(yScopes.Where(s => !string.IsNullOrEmpty(s)));
            }

            public int GetHashCode(RegistryInfo obj)
            {
                var hashCode = (obj.id != null ? obj.id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.name != null ? obj.name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.url != null ? obj.url.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (obj.scopes != null ? obj.scopes.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ obj.isDefault.GetHashCode();
                return hashCode;
            }
        }
    }
}
