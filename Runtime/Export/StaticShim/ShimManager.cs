// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    internal class ShimManager
    {
        private static ShimBase s_ActiveShim;
        private static readonly ShimBase s_DefaultShim = new ShimBase();

        internal static void UseShim(ShimBase shim)
        {
            s_ActiveShim = shim;
        }

        internal static void RemoveShim(ShimBase shim)
        {
            if (s_ActiveShim == shim)
                s_ActiveShim = null;
        }

        internal static bool IsShimActive(ShimBase shim)
        {
            return s_ActiveShim == shim;
        }

        internal static ShimBase GetActiveShim()
        {
            return s_ActiveShim ?? s_DefaultShim;
        }
    }
}
