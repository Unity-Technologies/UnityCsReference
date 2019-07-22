// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.IMGUI.Controls;

namespace UnityEditor.Presets
{
    class PresetTypeDropdownItem : AdvancedDropdownItem
    {
        string m_SearchableName;

        public string searchableName
        {
            get
            {
                if (m_SearchableName == null)
                    return name;
                return m_SearchableName;
            }
            set { m_SearchableName = value; }
        }

        public PresetType presetType { get; }

        public PresetTypeDropdownItem(string name) : base(name)
        {
        }

        public PresetTypeDropdownItem(string name, string command) : base(name)
        {
            //Re-used method from ComponentDropdownItem
            if (command.StartsWith("SCRIPT"))
            {
                var scriptId = int.Parse(command.Substring(6));
                var obj = EditorUtility.InstanceIDToObject(scriptId) as MonoScript;
                presetType = new PresetType(obj.GetClass());
            }
            else
            {
                var classId = int.Parse(command);
                presetType = new PresetType(classId);
            }
            icon = presetType.GetIcon();
            m_SearchableName = name;
        }

        public PresetTypeDropdownItem(string name, PresetType type) : base(name)
        {
            presetType = type;
            icon = type.GetIcon();
            m_SearchableName = name;
        }

        public override string ToString()
        {
            return presetType.GetManagedTypeName();
        }
    }
}
