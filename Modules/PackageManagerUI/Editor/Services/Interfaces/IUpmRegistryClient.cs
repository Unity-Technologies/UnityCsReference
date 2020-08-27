// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface IUpmRegistryClient
    {
        event Action<int> onRegistriesAdded;
        event Action onRegistriesModified;
        event Action<string, UIError> onRegistryOperationError;

        void AddRegistry(string name, string url, string[] scopes);
        void UpdateRegistry(string oldName, string newName, string url, string[] scopes);
        void RemoveRegistry(string name);
        void CheckRegistriesChanged();
    }
}
