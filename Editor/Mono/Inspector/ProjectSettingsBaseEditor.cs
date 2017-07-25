// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace UnityEditorInternal
{
    [CustomEditor(typeof(ProjectSettingsBase), true)]
    internal class ProjectSettingsBaseEditor : Editor
    {
        protected override bool ShouldHideOpenButton()
        {
            return true;
        }
    }
}
