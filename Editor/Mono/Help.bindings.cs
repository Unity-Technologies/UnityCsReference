// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    // Helper class to access Unity documentation.
    [NativeHeader("Editor/Src/Panels/HelpPanel.h")]
    [NativeHeader("Editor/Platform/Interface/EditorUtility.h")]
    [NativeHeader("Editor/Src/Utility/DocUtilities.h")]
    public partial class Help
    {
        [FreeFunction]
        internal static extern void SendHelpRequestedUsabilityEvent(long start, long duration, UnityEngine.Object contextObject, string url);

        [FreeFunction("DocUtilities::GetDocumentationAbsolutePath")]
        private static extern string GetDocumentationAbsolutePath_Internal();

        [UnityEngine.Scripting.RequiredByNativeCode]
        internal static void ShowNamedHelp(string topic)
        {
            ShowHelpPage(topic);
        }
    }
}
