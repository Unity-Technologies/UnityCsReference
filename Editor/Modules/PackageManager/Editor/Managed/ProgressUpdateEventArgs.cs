// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.ObjectModel;

namespace UnityEditor.PackageManager
{
    internal class ProgressUpdateEventArgs
    {
        public ReadOnlyCollection<PackageProgress> entries;

        internal ProgressUpdateEventArgs(PackageProgress[] progressUpdates)
        {
            entries = Array.AsReadOnly(progressUpdates);
        }
    }
}
