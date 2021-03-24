// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class BaseDropdownItem
    {
        public event Action onVisibleChanged;
        public bool needRefresh { get; set; }

        private bool m_Visible;
        public bool visible
        {
            get => m_Visible;
            set
            {
                if (m_Visible == value)
                    return;
                m_Visible = value;
                needRefresh = true;
                onVisibleChanged?.Invoke();
            }
        }

        private bool m_Enabled;
        public bool enabled
        {
            get => m_Enabled;
            set
            {
                if (m_Enabled == value)
                    return;
                m_Enabled = value;
                needRefresh = true;
            }
        }

        private bool m_InsertSeparatorBefore;
        public bool insertSeparatorBefore
        {
            get => m_InsertSeparatorBefore;
            set
            {
                if (m_InsertSeparatorBefore == value)
                    return;
                m_InsertSeparatorBefore = value;
                needRefresh = true;
            }
        }

        public Func<DropdownMenuAction, DropdownMenuAction.Status> statusCallback
        {
            get
            {
                if (!visible)
                    return e => DropdownMenuAction.Status.Hidden;
                if (!enabled)
                    return DropdownMenuAction.AlwaysDisabled;
                if (isChecked)
                    return e => DropdownMenuAction.Status.Checked;
                return DropdownMenuAction.AlwaysEnabled;
            }
        }

        private string m_Text;
        public string text
        {
            get => m_Text;
            set
            {
                if (m_Text == value)
                    return;
                m_Text = value;
                needRefresh = true;
            }
        }

        private int m_Priority;
        public int priority
        {
            get => m_Priority;
            set
            {
                if (m_Priority == value)
                    return;
                m_Priority = value;
                needRefresh = true;
            }
        }

        private bool m_IsChecked;
        public bool isChecked
        {
            get => m_IsChecked;
            set
            {
                if (m_IsChecked == value)
                    return;
                m_IsChecked = value;
                needRefresh = true;
            }
        }

        public BaseDropdownItem()
        {
            m_Enabled = true;
            m_Visible = true;
            needRefresh = true;
        }
    }
}
