// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Platform/Interface/AppInfo.h")]
    [NativeHeader("Runtime/Utilities/FileUtilities.h")]
    internal static class OSUtil
    {
        [StaticAccessor("AppInfo", StaticAccessorType.DoubleColon)]
        public static extern string[] GetDefaultApps(string fileType);

        [StaticAccessor("AppInfo", StaticAccessorType.DoubleColon)]
        public static extern string GetAppFriendlyName(string app);

        [StaticAccessor("AppInfo", StaticAccessorType.DoubleColon)]
        public static extern string GetDefaultAppPath(string fileType);

        [FreeFunction("GetUserAppCacheFolder")]
        public static extern string GetDefaultCachePath();
    }
}
