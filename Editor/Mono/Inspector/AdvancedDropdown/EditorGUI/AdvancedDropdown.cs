// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    public abstract class AdvancedDropdown
    {
        private Vector2 m_MinimumSize;
        protected Vector2 minimumSize
        {
            get { return m_MinimumSize; }
            set { m_MinimumSize = value; }
        }

        internal AdvancedDropdownWindow m_WindowInstance;
        internal AdvancedDropdownState m_State;
        internal AdvancedDropdownDataSource m_DataSource;

        public AdvancedDropdown(AdvancedDropdownState state)
        {
            m_State = state;
        }

        public void Show(Rect rect)
        {
            if (m_WindowInstance != null)
            {
                m_WindowInstance.Close();
                m_WindowInstance = null;
            }

            if (m_DataSource == null)
            {
                m_DataSource = new CallbackDataSource(BuildRoot);
            }

            m_WindowInstance = ScriptableObject.CreateInstance<AdvancedDropdownWindow>();
            if (m_MinimumSize != Vector2.zero)
                m_WindowInstance.minSize = m_MinimumSize;
            m_WindowInstance.state = m_State;
            m_WindowInstance.dataSource = m_DataSource;
            m_WindowInstance.windowClosed += (w) => ItemSelected(w.GetSelectedItem());
            m_WindowInstance.Init(rect);
        }

        internal void SetFilter(string searchString)
        {
            m_WindowInstance.searchString = searchString;
        }

        protected abstract AdvancedDropdownItem BuildRoot();

        protected virtual void ItemSelected(AdvancedDropdownItem item)
        {
        }
    }
}
