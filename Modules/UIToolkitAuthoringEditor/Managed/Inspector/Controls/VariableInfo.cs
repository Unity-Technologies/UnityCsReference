// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal struct VariableInfo
{
    public VariableInfo(StyleVariable styleVariable, string description = null)
    {
        StyleVariable = styleVariable;
        Description = description;
        m_Name = null;
    }

    public VariableInfo(string variableName)
    {
        StyleVariable = default;
        Description = null;
        m_Name = variableName;
    }

    string m_Name;

    public StyleVariable StyleVariable { get; set; }
    public StyleSheet Sheet => StyleVariable.sheet;
    public string Name
    {
        get
        {
            if (StyleVariable.sheet)
                return StyleVariable.name;
            return m_Name;
        }
    }

    public bool IsEditorVar => StyleVariable.sheet && StyleVariable.sheet.IsUnityEditorStyleSheet();
    public string Description { get; set; }

    public bool IsValid() => Sheet || !string.IsNullOrEmpty(m_Name);
}
