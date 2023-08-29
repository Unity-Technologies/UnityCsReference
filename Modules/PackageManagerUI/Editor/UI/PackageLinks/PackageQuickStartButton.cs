// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageQuickStartButton : PackageLinkButton
    {
        public PackageQuickStartButton(IApplicationProxy application, PackageLink link): base(application, link)
        {
            m_Link = link;
            m_Application = application;
            AddToClassList(k_LinkClass);

            text = string.Empty;
            AddToClassList("quickStartButton");
            AddToClassList("rightAlign");
            Add(new VisualElement { classList = { "quickStartIcon" } });
            Add(new TextElement { text = L10n.Tr("QuickStart"), classList = { "quickStartText" } });
        }
    }
}
