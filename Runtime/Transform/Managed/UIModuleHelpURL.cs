// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    /// <summary>
    /// HelpURLAttribute wrapper for UGUI native code
    /// </summary>
    [VisibleToOtherModules("UnityEngine.UIModule")]
    class UIModuleHelpURL : HelpURLAttribute
    {
        static string version
        {
            get
            {
                return "2.5";
            }
        }

        static string GetScriptURL(string urlBody)
        {
            if (string.IsNullOrEmpty(urlBody))
                return "";
            return $"https://docs.unity3d.com/Packages/com.unity.ugui@{version}/manual/{urlBody}.html";
        }

        [VisibleToOtherModules("UnityEngine.UIModule")]
        internal UIModuleHelpURL(string urlBody)
            : base(GetScriptURL(urlBody)) { }
    }
}
