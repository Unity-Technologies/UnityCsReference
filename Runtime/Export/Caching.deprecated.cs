// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{

    public partial class Caching
    {
        [System.Obsolete("This WebPlayer function is not used any more.", true)]
        public static bool Authorize(string name, string domain, long size, string signature)
        {
            return true;
        }

        [System.Obsolete("This WebPlayer function is not used any more.", true)]
        public static bool Authorize(string name, string domain, long size, int expiration, string signature)
        {
            return true;
        }

        [System.Obsolete("This WebPlayer function is not used any more.", true)]
        public static bool Authorize(string name, string domain, int size, int expiration, string signature)
        {
            return true;
        }

        [System.Obsolete("This WebPlayer function is not used any more.", true)]
        public static bool Authorize(string name, string domain, int size, string signature)
        {
            return true;
        }

        [System.Obsolete("This function is obsolete. Please use ClearCache.  (UnityUpgradable) -> ClearCache()")]
        public static bool CleanCache()
        {
            return ClearCache();
        }

        [System.Obsolete("This API is not for public use.")]
        public static bool CleanNamedCache(string name)
        {
            return false;
        }

        [System.Obsolete("This function is obsolete and has no effect.")]
        public static bool DeleteFromCache(string url)
        {
            return false;
        }

        [System.Obsolete("This property is only used by web player which is not used any more.", true)]
        public static bool enabled
        {
            get { return true; }
        }
    }
}
