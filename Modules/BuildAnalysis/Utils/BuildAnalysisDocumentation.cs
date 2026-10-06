// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Manual links, pinned to the running editor version so a link never lands on another version's page.
    /// </summary>
    internal static class BuildAnalysisDocumentation
    {
        // Manual topic slug (Documentation/ManualDocs/md/<slug>.md).
        private const string k_WindowReferencePage = "build-analysis-window-reference";

        /// <summary>The Build Analysis window's manual page.</summary>
        public static string WindowReferenceUrl => UrlFor(k_WindowReferencePage);

        private static string UrlFor(string page)
        {
            var version = UnityEditorInternal.InternalEditorUtility.GetUnityVersion();
            return $"https://docs.unity3d.com/{version.Major}.{version.Minor}/Documentation/Manual/{page}.html";
        }
    }
}
