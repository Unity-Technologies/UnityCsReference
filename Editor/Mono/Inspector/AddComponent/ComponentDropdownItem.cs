// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.AddComponent
{
    internal class ComponentDropdownItem : AdvancedDropdownItem
    {
        private string m_MenuPath;
        private bool m_IsLegacy;
        private string m_LocalizedName;
        private string m_SearchableNameLocalized;
        private string m_SearchableName;

        internal override string displayName
        {
            get { return m_LocalizedName; }
        }
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

        public string searchableNameLocalized
        {
            get
            {
                if (m_SearchableNameLocalized == null)
                    return m_SearchableName;
                return m_SearchableNameLocalized;
            }
            set { m_SearchableNameLocalized = value; }
        }

        public string localizedName
        {
            get { return m_LocalizedName ?? name; }
        }

        public string menuPath => m_MenuPath;

        public ComponentDropdownItem(string name) : base(name)
        {
        }

        public ComponentDropdownItem(string name, string localized) : base(name)
        {
            m_LocalizedName = localized;
            m_SearchableName = name;
            m_SearchableNameLocalized = localized;
        }

        public ComponentDropdownItem(string name, string localized, string menuPath, string command, bool isLegacy) : base(name)
        {
            m_LocalizedName = localized;
            m_MenuPath = menuPath;
            m_IsLegacy = isLegacy;

            if (command.StartsWith("SCRIPT"))
            {
                var scriptId = int.Parse(command.Substring(6));
                var obj = EditorUtility.InstanceIDToObject(scriptId);
                var icon = AssetPreview.GetMiniThumbnail(obj);
                base.name = name;
                base.icon = icon;
            }
            else
            {
                var classId = int.Parse(command);
                base.name = name;
                base.icon = AssetPreview.GetMiniTypeThumbnailFromClassID(classId);
            }
            m_SearchableName = name;
            m_SearchableNameLocalized = localized;
            if (m_IsLegacy)
            {
                m_SearchableName += " (Legacy)";
            }
        }

        public override int CompareTo(object o)
        {
            if (o is ComponentDropdownItem)
            {
                // legacy elements should always come after non legacy elements
                var componentElement = (ComponentDropdownItem)o;
                if (m_IsLegacy && !componentElement.m_IsLegacy)
                    return 1;
                if (!m_IsLegacy && componentElement.m_IsLegacy)
                    return -1;
            }
            return base.CompareTo(o);
        }

        public override string ToString()
        {
            return m_MenuPath;
        }
    }
}
