// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    [CustomEditor(typeof(ProjectSettingsBase), true)]
    internal class ProjectSettingsBaseEditor : Editor
    {
        protected override bool ShouldHideOpenButton()
        {
            return true;
        }

        private string m_LocalizedTargetName;

        internal override string targetTitle
        {
            get
            {
                if (m_LocalizedTargetName == null)
                    m_LocalizedTargetName = L10n.Tr(target.name);
                return m_LocalizedTargetName;
            }
        }
    }
}
