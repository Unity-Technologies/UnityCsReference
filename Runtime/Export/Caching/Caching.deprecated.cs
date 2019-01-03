// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{

    public partial class Caching
    {
        [System.Obsolete("This function is obsolete. Please use ClearCache.  (UnityUpgradable) -> ClearCache()")]
        public static bool CleanCache()
        {
            return ClearCache();
        }
    }
}
