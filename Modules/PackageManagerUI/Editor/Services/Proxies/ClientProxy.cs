// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics.CodeAnalysis;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IClientProxy : IService
    {
        void Resolve(bool force = true);
        AddRequest Add(string identifier);
        AddAndRemoveRequest AddAndRemove(string[] packagesToAdd = null, string[] packagesToRemove = null);
        AddScopedRegistryRequest AddScopedRegistry(string registryName, string url, string[] scopes);
        EmbedRequest Embed(string packageName);
        GetRegistriesRequest GetRegistries();
        ListRequest List(bool offlineMode, bool includeIndirectDependencies);
        RemoveRequest Remove(string packageName);
        RemoveScopedRegistryRequest RemoveScopedRegistry(string registryName);
        SearchRequest Search(string packageIdOrName, bool offlineMode);
        SearchRequest SearchAll(bool offlineMode);
        UpdateScopedRegistryRequest UpdateScopedRegistry(string registryName, UpdateScopedRegistryOptions options);
        GetCacheRootRequest GetCacheRoot();
        SetCacheRootRequest SetCacheRoot(string path);
        ClearCacheRootRequest ClearCacheRoot();
    }

    [ExcludeFromCodeCoverage]
    internal class ClientProxy : BaseService<IClientProxy>, IClientProxy
    {
        public void Resolve(bool force = true)
        {
            Client.Resolve(force);
        }

        public AddRequest Add(string identifier)
        {
            return Client.Add(identifier);
        }

        public AddAndRemoveRequest AddAndRemove(string[] packagesToAdd = null, string[] packagesToRemove = null)
        {
            return Client.AddAndRemove(packagesToAdd, packagesToRemove);
        }

        public AddScopedRegistryRequest AddScopedRegistry(string registryName, string url, string[] scopes)
        {
            return Client.AddScopedRegistry(registryName, url, scopes);
        }

        public EmbedRequest Embed(string packageName)
        {
            return Client.Embed(packageName);
        }

        public GetRegistriesRequest GetRegistries()
        {
            return Client.GetRegistries();
        }

        public ListRequest List(bool offlineMode, bool includeIndirectDependencies)
        {
            return Client.List(offlineMode, includeIndirectDependencies);
        }

        public RemoveRequest Remove(string packageName)
        {
            return Client.Remove(packageName);
        }

        public RemoveScopedRegistryRequest RemoveScopedRegistry(string registryName)
        {
            return Client.RemoveScopedRegistry(registryName);
        }

        public SearchRequest Search(string packageIdOrName, bool offlineMode)
        {
            return Client.Search(packageIdOrName, offlineMode);
        }

        public SearchRequest SearchAll(bool offlineMode)
        {
            return Client.SearchAll(offlineMode);
        }

        public UpdateScopedRegistryRequest UpdateScopedRegistry(string registryName, UpdateScopedRegistryOptions options)
        {
            return Client.UpdateScopedRegistry(registryName, options);
        }

        public GetCacheRootRequest GetCacheRoot()
        {
            return Client.GetCacheRoot();
        }

        public SetCacheRootRequest SetCacheRoot(string path)
        {
            return Client.SetCacheRoot(path);
        }

        public ClearCacheRootRequest ClearCacheRoot()
        {
            return Client.ClearCacheRoot();
        }
    }
}
