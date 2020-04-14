// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI
{
    internal enum PackageProgress
    {
        None = 0,
        Refreshing,
        Downloading,
        Pausing,
        Resuming,
        Installing,
        Removing
    }
}
