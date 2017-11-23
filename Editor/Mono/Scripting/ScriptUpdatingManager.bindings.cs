// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditorInternal
{
    //*undocumented*
    [NativeHeader("Editor/Src/ScriptUpdatingManager.h")]
    [StaticAccessor("ScriptUpdatingManager::GetInstance()", StaticAccessorType.Dot)]
    public static class ScriptUpdatingManager
    {
        public static extern bool WaitForVCSServerConnection(bool reportTimeout);
        public static extern void ReportExpectedUpdateFailure();
        public static extern void ReportGroupedAPIUpdaterFailure(string msg);
        public static extern int numberOfTimesAsked {[NativeName("NumberOfTimesAsked")] get; }
        public static extern void ResetConsentStatus();
    }
}
