// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class MenuDropdownItem : BaseDropdownItem, IMenuDropdownItem
    {
        public object userData { get; set; }

        private Action m_Action;
        public Action action
        {
            get => m_Action;
            set
            {
                m_Action = value;
                needRefresh = true;
            }
        }
    }
}
