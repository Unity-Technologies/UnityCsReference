// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager
{
    public static class Client
    {
        public static ListRequest List(bool offlineMode = false)
        {
            long operationId;
            var status = NativeClient.List(out operationId, offlineMode);
            return new ListRequest(operationId, status);
        }

        public static AddRequest Add(string packageIdOrName)
        {
            long operationId;
            var status = NativeClient.Add(out operationId, packageIdOrName);
            return new AddRequest(operationId, status);
        }

        public static RemoveRequest Remove(string packageIdOrName)
        {
            long operationId;
            var status = NativeClient.Remove(out operationId, packageIdOrName);
            return new RemoveRequest(operationId, status, packageIdOrName);
        }

        public static SearchRequest Search(string packageIdOrName)
        {
            long operationId;
            var status = NativeClient.Search(out operationId, packageIdOrName);
            return new SearchRequest(operationId, status, packageIdOrName);
        }

        public static SearchRequest SearchAll()
        {
            long operationId;
            var status = NativeClient.SearchAll(out operationId);
            return new SearchRequest(operationId, status, string.Empty);
        }

        public static ResetToEditorDefaultsRequest ResetToEditorDefaults()
        {
            long operationId;
            var status = NativeClient.ResetToEditorDefaults(out operationId);
            return new ResetToEditorDefaultsRequest(operationId, status);
        }
    }
}

