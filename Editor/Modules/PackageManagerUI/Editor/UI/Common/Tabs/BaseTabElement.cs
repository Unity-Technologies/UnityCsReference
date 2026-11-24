// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class BaseTabElement : VisualElement, ITabElement
    {
        protected string m_DisplayName;
        public virtual string displayName => m_DisplayName;

        protected string m_Id;
        public virtual string id => m_Id;
    }
}
