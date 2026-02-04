// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.AdaptivePerformance
{
    internal static class APLog
    {
       public static bool enabled = false;

       public static void Debug(string format, params object[] args)
        {
            if (enabled && UnityEngine.Debug.isDebugBuild)
                UnityEngine.Debug.Log(System.String.Format("[Adaptive Performance] " + format, args));
        }
    }
}
