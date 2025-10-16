// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.ProjectAuditor.Editor
{
    static class Documentation
    {
        internal const string baseURL = "https://docs.unity3d.com/Packages/com.unity.project-auditor@";
        internal const string subURL = "/manual/";
        internal const string endURL = ".html";

        internal static string GetPageUrl(string pageName)
        {
            return baseURL + "1.0" + subURL + pageName + endURL; // TODO - link to main manual instead
        }
    }
}
