// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Reporting
{
    /*
     * The BuildReport used to expose a REST API over an HTTP endpoint, used only by the tests. The HTTP endpoint
     * has since been removed, but in order to do that without needing to rewrite all the tests, we introduced this API.
     * It returns the same JSON strings as the HTTP endpoint used to return, but just as pure C# method calls instead
     * of going via an HTTP transport. The tests could someday be updated to work with the BuildReport API more
     * directly instead of using JSON, at which point the methods here could be deleted completely.
     */
    [NativeHeader("Modules/BuildReportingEditor/BuildReportRestService.h")]
    internal sealed class BuildReportRestAPI
    {
        [FreeFunction("BuildReporting::GetSummaryResponse")]
        internal static extern string GetSummaryResponse(BuildReport report);

        [FreeFunction("BuildReporting::GetAssetsResponse")]
        internal static extern string GetAssetsResponse(BuildReport report, string rootPath, int depth);

        [FreeFunction("BuildReporting::GetFilesResponse")]
        internal static extern string GetFilesResponse(BuildReport report, string rootPath, int depth);

        [FreeFunction("BuildReporting::GetStepsResponse")]
        internal static extern string GetStepsResponse(BuildReport report);

        [FreeFunction("BuildReporting::GetAppendicesResponse")]
        internal static extern string GetAppendicesResponse(BuildReport report, string type);

        [FreeFunction("BuildReporting::GetAppendicesResponseWithIndex")]
        internal static extern string GetAppendicesResponseWithIndex(BuildReport report, string type, int appendixID);

        [FreeFunction("BuildReporting::GetAppendicesResponseWithMethod")]
        internal static extern string GetAppendicesResponseWithMethod(BuildReport report, string type, int appendixID, string method, string payloadJson);
    }
}
