// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;

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
        LocalInfo        = 1 << 5,
        ImportedAssets   = 1 << 6
    }

    internal static class RefreshOptionsExtension
    {
        public static RefreshOptions[] Split(this RefreshOptions value)
        {
            return Enum.GetValues(typeof(RefreshOptions)).Cast<RefreshOptions>()
                .Where(r => (value & r) != 0).ToArray();
        }

        public static bool Contains(this RefreshOptions value, RefreshOptions flag) => (value & flag) == flag;
    }
}
