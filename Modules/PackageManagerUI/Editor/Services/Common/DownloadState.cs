// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Flags]
    internal enum DownloadState
    {
        None                    = 0,
        Connecting              = 1 << 0,
        DownloadRequested       = 1 << 1,
        Downloading             = 1 << 2,
        Pausing                 = 1 << 3,
        Paused                  = 1 << 4,
        ResumeRequested         = 1 << 5,
        Completed               = 1 << 6,
        Decrypting              = 1 << 7,
        Aborted                 = 1 << 8,
        AbortRequsted           = 1 << 9,
        Error                   = 1 << 10,


        InProgress              = Connecting | DownloadRequested | Downloading | ResumeRequested | Decrypting,
        InPause                 = Pausing | Paused
    }
}
