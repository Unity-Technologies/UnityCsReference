// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager
{
    public static class Client
    {
        public static ListRequest List(bool offlineMode, bool includeIndirectDependencies)
        {
            long operationId;
            var status = NativeClient.List(out operationId, offlineMode, includeIndirectDependencies);
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
            long operationId;
            var status = NativeClient.Add(out operationId, identifier);
            return new AddRequest(operationId, status);
        }

        public static RemoveRequest Remove(string packageName)
        {
            long operationId;
            var status = NativeClient.Remove(out operationId, packageName);
            return new RemoveRequest(operationId, status, packageName);
        }

        public static SearchRequest Search(string packageIdOrName, bool offlineMode)
        {
            if (string.IsNullOrEmpty(packageIdOrName?.Trim()))
                throw new ArgumentNullException(nameof(packageIdOrName));

            long operationId;
            var status = NativeClient.Search(out operationId, packageIdOrName, offlineMode);
            return new SearchRequest(operationId, status, packageIdOrName);
        }

        public static SearchRequest Search(string packageIdOrName)
        {
            return Search(packageIdOrName, false);
        }

        public static SearchRequest SearchAll(bool offlineMode)
        {
            long operationId;
            var status = NativeClient.SearchAll(out operationId, offlineMode);
            return new SearchRequest(operationId, status, string.Empty);
        }

        public static SearchRequest SearchAll()
        {
            return SearchAll(false);
        }

        public static ResetToEditorDefaultsRequest ResetToEditorDefaults()
        {
            long operationId;
            var status = NativeClient.ResetToEditorDefaults(out operationId);
            return new ResetToEditorDefaultsRequest(operationId, status);
        }
    }
}
