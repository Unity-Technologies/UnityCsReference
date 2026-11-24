// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageActionDropdownItem : BaseDropdownItem, IPackageActionDropdownItem
    {
        private Action<PackageSelectionArgs> m_Action;
        public Action<PackageSelectionArgs> action
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
