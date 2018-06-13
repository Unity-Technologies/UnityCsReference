// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditorInternal
{
    //*undocumented*
    [NativeHeader("Editor/Src/Scripting/APIUpdater/APIUpdaterManager.h")]
    [StaticAccessor("APIUpdaterManager::GetInstance()", StaticAccessorType.Dot)]
    internal static class APIUpdaterManager
    {
        public static extern bool WaitForVCSServerConnection(bool reportTimeout);
        public static extern void ReportExpectedUpdateFailure();
        public static extern void ReportGroupedAPIUpdaterFailure(string msg);
        public static extern int numberOfTimesAsked
        {
            [NativeName("NumberOfTimesAsked")] get;
        }

        public static extern void ResetNumberOfTimesAsked();

        public static extern void ResetConsentStatus();
        public static extern void ReportUpdatedFiles(string[] filePaths);

        public static extern void AcceptUpdateOffer();

        // Sets/gets a regular expression used to filter configuration sources assemblies
        // by name.
        public static extern string ConfigurationSourcesFilter { get; set; }
    }

    // Keep in sync with APIUpdaterManager.h
    internal enum APIUpdaterStatus
    {
        None,
        Offered,
        Accepted
    };
}
