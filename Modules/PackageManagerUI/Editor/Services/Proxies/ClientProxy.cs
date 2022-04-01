// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics.CodeAnalysis;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager.UI.Internal
{
    [ExcludeFromCodeCoverage]
    internal class ClientProxy
    {
        public virtual void Resolve(bool force = true)
        {
            Client.Resolve(force);
        }

        public virtual AddRequest Add(string identifier)
        {
            return Client.Add(identifier);
        }

        public virtual AddAndRemoveRequest AddAndRemove(string[] packagesToAdd = null, string[] packagesToRemove = null)
        {
            return Client.AddAndRemove(packagesToAdd, packagesToRemove);
        }

        public virtual AddScopedRegistryRequest AddScopedRegistry(string registryName, string url, string[] scopes)
        {
            return Client.AddScopedRegistry(registryName, url, scopes);
        }

        public virtual EmbedRequest Embed(string packageName)
        {
            return Client.Embed(packageName);
        }

        public virtual GetRegistriesRequest GetRegistries()
        {
            return Client.GetRegistries();
        }

        public virtual ListRequest List(bool offlineMode, bool includeIndirectDependencies)
        {
            return Client.List(offlineMode, includeIndirectDependencies);
        }

        public virtual RemoveRequest Remove(string packageName)
        {
            return Client.Remove(packageName);
        }

        public virtual RemoveScopedRegistryRequest RemoveScopedRegistry(string registryName)
        {
            return Client.RemoveScopedRegistry(registryName);
        }

        public virtual SearchRequest Search(string packageIdOrName, bool offlineMode)
        {
            return Client.Search(packageIdOrName, offlineMode);
        }

        public virtual SearchRequest SearchAll(bool offlineMode)
        {
            return Client.SearchAll(offlineMode);
        }

        public virtual UpdateScopedRegistryRequest UpdateScopedRegistry(string registryName, UpdateScopedRegistryOptions options)
        {
            return Client.UpdateScopedRegistry(registryName, options);
        }

        public virtual GetCacheRootRequest GetCacheRoot()
        {
            return Client.GetCacheRoot();
        }

        public virtual SetCacheRootRequest SetCacheRoot(string path)
        {
            return Client.SetCacheRoot(path);
        }

        public virtual ClearCacheRootRequest ClearCacheRoot()
        {
            return Client.ClearCacheRoot();
        }
    }
}
