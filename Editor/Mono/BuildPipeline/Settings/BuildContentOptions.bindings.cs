// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    [Flags]
    // UCBP-Backport - temporary internal
    internal enum BuildContentOptions
    {
        None = 0,
        UseArchive = 1 << 0,
        GranularBuild = 1 << 1,
        UdmBuild = 1 << 2,
        DisableWriteTypeTree = 1 << 3,
        CleanBuildCache = 1 << 5,
        FailBuildWhenErrorsLogged = 1 << 9,
        DryRunBuild = 1 << 10,
        SerializeUnityVersion = 1 << 15,
        DetailedBuildReport = 1 << 29
    }
}

