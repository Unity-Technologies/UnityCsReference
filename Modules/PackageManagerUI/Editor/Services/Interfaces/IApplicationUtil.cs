// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    internal interface IApplicationUtil
    {
        bool isPreReleaseVersion { get; }

        string shortUnityVersion { get; }

        bool isInternetReachable { get; }

        bool isCompiling { get; }

        event Action onFinishCompiling;
    }
}
