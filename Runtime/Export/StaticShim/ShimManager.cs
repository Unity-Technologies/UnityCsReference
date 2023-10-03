// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    internal class ShimManager
    {
        internal static Action ActiveShimChanged;

        private static List<ScreenShimBase> s_ActiveScreenShim = new List<ScreenShimBase>(new [] { new ScreenShimBase() } );
        private static List<SystemInfoShimBase> s_ActiveSystemInfoShim = new List<SystemInfoShimBase>(new [] { new SystemInfoShimBase() } );
        private static List<ApplicationShimBase> s_ActiveApplicationShim = new List<ApplicationShimBase>(new [] { new ApplicationShimBase() } );

        internal static ScreenShimBase screenShim => s_ActiveScreenShim.Last();
        internal static SystemInfoShimBase systemInfoShim => s_ActiveSystemInfoShim.Last();
        internal static ApplicationShimBase applicationShim => s_ActiveApplicationShim.Last();

        internal static void UseShim(ScreenShimBase shim)
        {
            if (s_ActiveScreenShim.Last() == shim)
            {
                return;
            }

            RemoveShim(shim);
            s_ActiveScreenShim.Add(shim);
            ActiveShimChanged?.Invoke();
        }

        internal static void UseShim(SystemInfoShimBase shim)
        {
            if (s_ActiveSystemInfoShim.Last() == shim)
            {
                return;
            }

            RemoveShim(shim);
            s_ActiveSystemInfoShim.Add(shim);
            ActiveShimChanged?.Invoke();
        }

        internal static void UseShim(ApplicationShimBase shim)
        {
            if (s_ActiveApplicationShim.Last() == shim)
            {
                return;
            }

            RemoveShim(shim);
            s_ActiveApplicationShim.Add(shim);
            ActiveShimChanged?.Invoke();
        }

        internal static void RemoveShim(ScreenShimBase shim)
        {
            if (s_ActiveScreenShim.Contains(shim))
            {
                s_ActiveScreenShim.Remove(shim);
                ActiveShimChanged?.Invoke();
            }
        }

        internal static void RemoveShim(SystemInfoShimBase shim)
        {
            if (s_ActiveSystemInfoShim.Contains(shim))
            {
                s_ActiveSystemInfoShim.Remove(shim);
                ActiveShimChanged?.Invoke();
            }
        }

        internal static void RemoveShim(ApplicationShimBase shim)
        {
            if (s_ActiveApplicationShim.Contains(shim))
            {
                s_ActiveApplicationShim.Remove(shim);
                ActiveShimChanged?.Invoke();
            }
        }

        internal static bool IsShimActive(ScreenShimBase shim)
        {
            return s_ActiveScreenShim.Last() == shim;
        }

        internal static bool IsShimActive(SystemInfoShimBase shim)
        {
            return s_ActiveSystemInfoShim.Last() == shim;
        }

        internal static bool IsShimActive(ApplicationShimBase shim)
        {
            return s_ActiveApplicationShim.Last() == shim;
        }

        // For the following functions, only return true if shims besides the default are in the collection
        internal static bool IsScreenShimActive()
        {
            return s_ActiveScreenShim.Count > 1;
        }

        internal static bool IsSystemInfoShimActive()
        {
            return s_ActiveSystemInfoShim.Count > 1;
        }

        internal static bool IsApplicationShimActive()
        {
            return s_ActiveApplicationShim.Count > 1;
        }
    }
}
