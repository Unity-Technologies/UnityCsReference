// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.VersionControl
{
    [NativeHeader("Editor/Src/VersionControl/VCManager.h")]
    [StaticAccessor("GetVCManager()")]
    partial class VersionControlManager
    {
        static extern ScriptableObject GetActiveObject();
        static extern void SetActiveObject(ScriptableObject vco);
    }
}
