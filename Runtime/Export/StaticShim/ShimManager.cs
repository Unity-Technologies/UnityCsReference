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

        private static ApplicationShimBase s_ActiveApplicationShim;
        private static readonly ApplicationShimBase s_DefaultApplicationShim = new ApplicationShimBase();

        internal static ScreenShimBase screenShim => s_ActiveScreenShim ?? s_DefaultScreenShim;
        internal static SystemInfoShimBase systemInfoShim => s_ActiveSystemInfoShim ?? s_DefaultSystemInfoShim;
        internal static ApplicationShimBase applicationShim => s_ActiveApplicationShim ?? s_DefaultApplicationShim;

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

        internal static void UseShim(ApplicationShimBase shim)
        {
            s_ActiveApplicationShim = shim;
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

        internal static void RemoveShim(ApplicationShimBase shim)
        {
            if (s_ActiveApplicationShim == shim)
            {
                s_ActiveApplicationShim = null;
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

        internal static bool IsShimActive(ApplicationShimBase shim)
        {
            return s_ActiveApplicationShim == shim;
        }

        internal bool IsScreenShimActive()
        {
            return s_ActiveScreenShim != null;
        }

        internal bool IsSystemInfoShimActive()
        {
            return s_ActiveSystemInfoShim != null;
        }

        internal bool IsApplicationShimActive()
        {
            return s_ActiveApplicationShim != null;
        }
    }
}
