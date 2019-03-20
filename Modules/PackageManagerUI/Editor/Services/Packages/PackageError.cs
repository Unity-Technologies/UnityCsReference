// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PackageError
    {
        public string PackageName;
        public Error Error;

        public PackageError(string packageName, Error error)
        {
            PackageName = packageName;
            Error = error;
        }
    }
}
