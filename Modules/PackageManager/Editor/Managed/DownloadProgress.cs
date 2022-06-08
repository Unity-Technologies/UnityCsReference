// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager
{
    internal struct DownloadProgress
    {
        public readonly ulong currentBytes;
        public readonly ulong totalBytes;

        internal DownloadProgress(ulong current, ulong total)
        {
            currentBytes = current;
            totalBytes = total;
        }
    }
}
