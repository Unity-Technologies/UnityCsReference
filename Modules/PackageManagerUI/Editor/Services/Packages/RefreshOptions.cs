// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Flags]
    internal enum RefreshOptions : uint
    {
        None             = 0,

        UpmListOffline   = 1 << 0,
        UpmList          = 1 << 1,
        UpmSearchOffline = 1 << 2,
        UpmSearch        = 1 << 3,
        Purchased        = 1 << 4,
        PurchasedOffline = 1 << 5,

        UpmAny = UpmList | UpmListOffline | UpmSearch | UpmSearchOffline
    }
}
