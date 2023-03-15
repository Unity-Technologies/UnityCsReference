// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace UnityEditor.PackageManager
{
    public static partial class Client
    {
        public static ListRequest List(bool offlineMode, bool includeIndirectDependencies)
        {
            long operationId;
            var status = List(out operationId, offlineMode, includeIndirectDependencies);
            return new ListRequest(operationId, status);
        }

        public static ListRequest List(bool offlineMode)
        {
            return List(offlineMode, false);
        }

        public static ListRequest List()
        {
            return List(false, false);
        }

        public static AddRequest Add(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("Package identifier cannot be null, empty or whitespace", nameof(identifier));

            long operationId;
            var status = Add(out operationId, identifier);
            return new AddRequest(operationId, status);
        }

        public static AddAndRemoveRequest AddAndRemove(string[] packagesToAdd = null, string[] packagesToRemove = null)
        {
            packagesToAdd = packagesToAdd ?? Array.Empty<string>();
            packagesToRemove = packagesToRemove ?? Array.Empty<string>();

            if (packagesToAdd.Length == 0 && packagesToRemove.Length == 0)
            {
                throw new ArgumentException("No packages provided to add or remove");
            }
            if (packagesToAdd.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Packages to add cannot contain null, empty or whitespace values", nameof(packagesToAdd));
            }
            if (packagesToRemove.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Packages to remove cannot contain null, empty or whitespace values", nameof(packagesToRemove));
            }

            long operationId;
            var status = AddAndRemove(out operationId, packagesToAdd, packagesToRemove);
            return new AddAndRemoveRequest(operationId, status);
        }

        internal static AddScopedRegistryRequest AddScopedRegistry(string registryName, string url, string[] scopes)
        {
            if (string.IsNullOrWhiteSpace(registryName))
                throw new ArgumentException("Registry name cannot be null, empty or whitespace", nameof(registryName));

            long operationId;
            var status = AddScopedRegistry(out operationId, registryName, url, scopes);
            return new AddScopedRegistryRequest(operationId, status);
        }

        internal static ClearCacheRootRequest ClearCacheRoot()
        {
            long operationId;
            var status = ClearCacheRoot(out operationId);
            return new ClearCacheRootRequest(operationId, status);
        }

        public static EmbedRequest Embed(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
                throw new ArgumentException("Package name cannot be null, empty or whitespace", nameof(packageName));

            if (!PackageInfo.IsPackageRegistered(packageName))
                throw new InvalidOperationException($"Cannot embed package [{packageName}] because it is not registered in the Asset Database.");

            long operationId;
            var status = Embed(out operationId, packageName);
            return new EmbedRequest(operationId, status);
        }

        internal static GetRegistriesRequest GetRegistries()
        {
            long operationId;
            var status = GetRegistries(out operationId);
            return new GetRegistriesRequest(operationId, status);
        }

        internal static GetCacheRootRequest GetCacheRoot()
        {
            long operationId;
            var status = GetCacheRoot(out operationId, ConfigSource.Unknown);
            return new GetCacheRootRequest(operationId, status);
        }

        internal static GetCacheRootRequest GetDefaultCacheRoot()
        {
            long operationId;
            var status = GetCacheRoot(out operationId, ConfigSource.Default);
            return new GetCacheRootRequest(operationId, status);
        }

        internal static ListBuiltInPackagesRequest ListBuiltInPackages()
        {
            long operationId;
            var status = ListBuiltInPackages(out operationId);
            return new ListBuiltInPackagesRequest(operationId, status);
        }

        public static RemoveRequest Remove(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
                throw new ArgumentException("Package name cannot be null, empty or whitespace", nameof(packageName));

            long operationId;
            var status = Remove(out operationId, packageName);
            return new RemoveRequest(operationId, status, packageName);
        }

        internal static RemoveScopedRegistryRequest RemoveScopedRegistry(string registryId)
        {
            if (string.IsNullOrWhiteSpace(registryId))
                throw new ArgumentException("Registry ID cannot be null, empty or whitespace", nameof(registryId));

            long operationId;
            var status = RemoveScopedRegistry(out operationId, registryId);
            return new RemoveScopedRegistryRequest(operationId, status);
        }

        public static SearchRequest Search(string packageIdOrName, bool offlineMode)
        {
            if (string.IsNullOrWhiteSpace(packageIdOrName))
                throw new ArgumentException("Package id or name cannot be null, empty or whitespace", nameof(packageIdOrName));

            long operationId;
            var status = GetPackageInfo(out operationId, packageIdOrName, offlineMode);
            return new SearchRequest(operationId, status, packageIdOrName);
        }

        public static SearchRequest Search(string packageIdOrName)
        {
            return Search(packageIdOrName, false);
        }

        public static SearchRequest SearchAll(bool offlineMode)
        {
            long operationId;
            var status = GetPackageInfo(out operationId, string.Empty, offlineMode);
            return new SearchRequest(operationId, status, string.Empty);
        }

        public static SearchRequest SearchAll()
        {
            return SearchAll(false);
        }

        internal static PerformSearchRequest Search(SearchOptions options)
        {
            long operationId;
            var status = Search(out operationId, options);
            return new PerformSearchRequest(operationId, status);
        }

        internal static SetCacheRootRequest SetCacheRoot(string newPath)
        {
            long operationId;
            var status = SetCacheRoot(out operationId, newPath);
            return new SetCacheRootRequest(operationId, status);
        }

        public static ResetToEditorDefaultsRequest ResetToEditorDefaults()
        {
            long operationId;
            var status = ResetToEditorDefaults(out operationId);
            return new ResetToEditorDefaultsRequest(operationId, status);
        }

        public static PackRequest Pack(string packageFolder, string targetFolder)
        {
            if (string.IsNullOrWhiteSpace(packageFolder))
                throw new ArgumentException("Package folder cannot be null, empty or whitespace", nameof(packageFolder));

            if (string.IsNullOrWhiteSpace(targetFolder))
                throw new ArgumentException("Target folder cannot be null, empty or whitespace", nameof(targetFolder));

            long operationId;
            var status = Pack(out operationId, packageFolder, targetFolder);
            return new PackRequest(operationId, status);
        }

        internal static UpdateScopedRegistryRequest UpdateScopedRegistry(string registryId, UpdateScopedRegistryOptions options)
        {
            if (string.IsNullOrWhiteSpace(registryId))
                throw new ArgumentException("Registry ID cannot be null, empty or whitespace", nameof(registryId));

            long operationId;
            var status = UpdateScopedRegistry(out operationId, registryId, options);
            return new UpdateScopedRegistryRequest(operationId, status);
        }
    }
}
