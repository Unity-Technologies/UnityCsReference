// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Flags]
    internal enum PageCapability : uint
    {
        None                    = 0,
        RequireUserLoggedIn     = 1 << 0,
        RequireNetwork          = 1 << 1,
        SupportLocalReordering  = 1 << 2,
    }
}
