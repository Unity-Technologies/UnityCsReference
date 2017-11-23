// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class ComponentDropdownElement : DropdownElement
    {
        private string m_MenuPath;
        private string m_Command;
        private bool m_IsLegacy;

        protected override bool isSearchable
        {
            get { return true; }
        }

        public ComponentDropdownElement(string name, string menuPath, string command) : base(name)
        {
            m_MenuPath = menuPath;
            m_Command = command;
            m_IsLegacy = menuPath.Contains("Legacy");

            if (command.StartsWith("SCRIPT"))
            {
                var scriptId = int.Parse(command.Substring(6));
                var obj = EditorUtility.InstanceIDToObject(scriptId);
                var icon = AssetPreview.GetMiniThumbnail(obj);
                m_Content = new GUIContent(name, icon);
            }
            else
            {
                var classId = int.Parse(command);
                m_Content = new GUIContent(name, AssetPreview.GetMiniTypeThumbnailFromClassID(classId));
            }
            m_ContentWhenSearching = new GUIContent(m_Content);
            if (m_IsLegacy)
            {
                m_ContentWhenSearching.text += " (Legacy)";
            }
        }

        public override int CompareTo(object o)
        {
            if (o is ComponentDropdownElement)
            {
                // legacy elements should always come after non legacy elements
                var componentElement = (ComponentDropdownElement)o;
                if (m_IsLegacy && !componentElement.m_IsLegacy)
                    return 1;
                if (!m_IsLegacy && componentElement.m_IsLegacy)
                    return -1;
            }
            return base.CompareTo(o);
        }

        public override bool OnAction()
        {
            AddComponentWindow.SendUsabilityAnalyticsEvent(new AddComponentWindow.AnalyticsEventData
            {
                name = name,
                filter = AddComponentWindow.s_AddComponentWindow.searchString,
                isNewScript = false
            });

            var gos = AddComponentWindow.s_AddComponentWindow.m_GameObjects;
            EditorApplication.ExecuteMenuItemOnGameObjects(m_MenuPath, gos);
            return true;
        }

        public override string ToString()
        {
            return m_MenuPath + m_Command;
        }
    }
}
