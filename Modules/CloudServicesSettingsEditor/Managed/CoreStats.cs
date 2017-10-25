// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEditor.Analytics
{
    internal static class CoreStats
    {
        public delegate bool RequireInBuildDelegate();
        public static event RequireInBuildDelegate OnRequireInBuildHandler = null;

        [RequiredByNativeCode]
        public static bool RequiresCoreStatsInBuild()
        {
            if (OnRequireInBuildHandler != null)
            {
                Delegate[] invokeList = OnRequireInBuildHandler.GetInvocationList();
                for (int i = 0; i < invokeList.Length; ++i)
                {
                    RequireInBuildDelegate func = (RequireInBuildDelegate)invokeList[i];
                    if (func())
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
