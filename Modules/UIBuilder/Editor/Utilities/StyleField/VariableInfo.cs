// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    struct VariableInfo
    {
        public VariableInfo(StyleVariable styleVariable, string description = null)
        {
            this.styleVariable = styleVariable;
            this.description = description;
            this.m_Name = null;
        }

        public VariableInfo(string variableName)
        {
            this.styleVariable = default;
            this.description = null;
            this.m_Name = variableName;
        }

        string m_Name;

        public StyleVariable styleVariable { get; set; }
        public StyleSheet sheet => styleVariable.sheet;
        public string name
        {
            get
            {
                if (styleVariable.sheet)
                    return styleVariable.name;
                return m_Name;
            }
        }

        public bool isEditorVar => styleVariable.sheet && styleVariable.sheet.IsUnityEditorStyleSheet();
        public string description { get; set; }

        public bool IsValid() => sheet || !string.IsNullOrEmpty(m_Name);
    }
}
