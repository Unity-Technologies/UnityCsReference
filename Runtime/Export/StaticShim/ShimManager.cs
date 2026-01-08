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

#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        internal static ScreenShimBase screenShim => s_ActiveScreenShim.Last();
        internal static SystemInfoShimBase systemInfoShim => s_ActiveSystemInfoShim.Last();
        internal static ApplicationShimBase applicationShim => s_ActiveApplicationShim.Last();
#pragma warning restore RS0030

        internal static void UseShim(ScreenShimBase shim)
        {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (s_ActiveScreenShim.Last() == shim)
#pragma warning restore RS0030
            {
                return;
            }

            RemoveShim(shim);
            s_ActiveScreenShim.Add(shim);
            ActiveShimChanged?.Invoke();
        }

        internal static void UseShim(SystemInfoShimBase shim)
        {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (s_ActiveSystemInfoShim.Last() == shim)
#pragma warning restore RS0030
            {
                return;
            }

            RemoveShim(shim);
            s_ActiveSystemInfoShim.Add(shim);
            ActiveShimChanged?.Invoke();
        }

        internal static void UseShim(ApplicationShimBase shim)
        {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (s_ActiveApplicationShim.Last() == shim)
#pragma warning restore RS0030
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
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return s_ActiveScreenShim.Last() == shim;
#pragma warning restore RS0030
        }

        internal static bool IsShimActive(SystemInfoShimBase shim)
        {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return s_ActiveSystemInfoShim.Last() == shim;
#pragma warning restore RS0030
        }

        internal static bool IsShimActive(ApplicationShimBase shim)
        {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return s_ActiveApplicationShim.Last() == shim;
#pragma warning restore RS0030
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
