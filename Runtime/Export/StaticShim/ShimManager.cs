// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    internal class ShimManager
    {
        internal static Action ActiveShimChanged;

        private static ScreenShimBase s_ActiveScreenShim;
        private static readonly ScreenShimBase s_DefaultScreenShim = new ScreenShimBase();

        private static SystemInfoShimBase s_ActiveSystemInfoShim;
        private static readonly SystemInfoShimBase s_DefaultSystemInfoShim = new SystemInfoShimBase();

        internal static ScreenShimBase ScreenShim => s_ActiveScreenShim ?? s_DefaultScreenShim;
        internal static SystemInfoShimBase SystemInfoShim => s_ActiveSystemInfoShim ?? s_DefaultSystemInfoShim;

        internal static void UseShim(ScreenShimBase shim)
        {
            s_ActiveScreenShim = shim;
            ActiveShimChanged?.Invoke();
        }

        internal static void UseShim(SystemInfoShimBase shim)
        {
            s_ActiveSystemInfoShim = shim;
            ActiveShimChanged?.Invoke();
        }

        internal static void RemoveShim(ScreenShimBase shim)
        {
            if (s_ActiveScreenShim == shim)
            {
                s_ActiveScreenShim = null;
                ActiveShimChanged?.Invoke();
            }
        }

        internal static void RemoveShim(SystemInfoShimBase shim)
        {
            if (s_ActiveSystemInfoShim == shim)
            {
                s_ActiveSystemInfoShim = null;
                ActiveShimChanged?.Invoke();
            }
        }

        internal static bool IsShimActive(ScreenShimBase shim)
        {
            return s_ActiveScreenShim == shim;
        }

        internal static bool IsShimActive(SystemInfoShimBase shim)
        {
            return s_ActiveSystemInfoShim == shim;
        }

        internal bool IsScreenShimActive()
        {
            return s_ActiveScreenShim != null;
        }

        internal bool IsSystemInfoShimActive()
        {
            return s_ActiveSystemInfoShim != null;
        }
    }
}
