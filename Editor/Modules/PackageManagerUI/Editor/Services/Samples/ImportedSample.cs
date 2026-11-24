// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class ImportedSample
    {
        public string sanitizedDisplayName;
        public List<string> versions;

        public bool IsEquivalent(ImportedSample other)
        {
            return sanitizedDisplayName == other.sanitizedDisplayName
                   && versions.IsSequenceEqual(other.versions);
        }
    }
}
