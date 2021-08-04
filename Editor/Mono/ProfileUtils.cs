// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Mono
{
    internal static class ProfileUtils
    {
        public static bool IsLatestApiCompatibility(ApiCompatibilityLevel level)
        {
            return level == ApiCompatibilityLevel.NET_4_6 || level == ApiCompatibilityLevel.NET_Standard_2_0 ||
                level == ApiCompatibilityLevel.NET_Unity_4_8 || level == ApiCompatibilityLevel.NET_Standard;
        }
    }
}
