// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    /// <summary>
    /// HelpURLAttribute wrapper for UGUI C# code
    /// </summary>
    class UGUIHelpURL : UIModuleHelpURL
    {
        internal UGUIHelpURL(string className)
            : base($"script-{className}") { }
    }
}
