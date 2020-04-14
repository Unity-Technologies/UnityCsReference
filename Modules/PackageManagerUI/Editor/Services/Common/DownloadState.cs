// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    internal enum DownloadState
    {
        None,
        Connecting,
        DownloadRequested,
        Downloading,
        Pausing,
        Paused,
        ResumeRequested,
        Completed,
        Decrypting,
        Aborted,
        Error
    }
}
