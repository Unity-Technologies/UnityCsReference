// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEditor.PackageManager.Requests;

namespace UnityEditor.PackageManager
{
    /// <summary>
    /// Entry point for all Unity Package Manager operations
    /// </summary>
    public static class Client
    {
        /// <summary>
        /// Lists the packages the project depends on.
        /// </summary>
        /// <returns>A ListRequest instance</returns>
        public static ListRequest List()
        {
            long operationId;
            var status = NativeClient.List(out operationId);
            return new ListRequest(operationId, status);
        }

        /// <summary>
        /// Adds a package dependency to the project.
        /// </summary>
        /// <param name="packageIdOrName">Id or name of the package to add</param>
        /// <returns>An AddRequest instance</returns>
        public static AddRequest Add(string packageIdOrName)
        {
            long operationId;
            var status = NativeClient.Add(out operationId, packageIdOrName);
            return new AddRequest(operationId, status);
        }

        /// <summary>
        /// Removes a previously added package from the project.
        /// </summary>
        /// <param name="packageIdOrName">Id or name of the package to remove</param>
        /// <returns>A RemoveRequest instance</returns>
        public static RemoveRequest Remove(string packageIdOrName)
        {
            long operationId;
            var status = NativeClient.Remove(out operationId, packageIdOrName);
            return new RemoveRequest(operationId, status, packageIdOrName);
        }

        /// <summary>
        /// Searches the registry for the given package.
        /// </summary>
        /// <param name="packageIdOrName">Id or name of the package to search for</param>
        /// <returns>A SearchRequest instance</returns>
        public static SearchRequest Search(string packageIdOrName)
        {
            long operationId;
            var status = NativeClient.Search(out operationId, packageIdOrName);
            return new SearchRequest(operationId, status, packageIdOrName);
        }

        /// <summary>
        /// Resets the list of packages installed for this project to the editor's default configuration.
        /// This operation will clear all packages added to the project and keep only the packages set for the current editor default configuration.
        /// </summary>
        /// <returns>A ResetToEditorDefaultsRequest instance</returns>
        public static ResetToEditorDefaultsRequest ResetToEditorDefaults()
        {
            long operationId;
            var status = NativeClient.ResetToEditorDefaults(out operationId);
            return new ResetToEditorDefaultsRequest(operationId, status);
        }
    }
}

