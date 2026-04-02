// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class SampleItem : BaseListItem
    {
        private readonly Label m_ParentPackageDisplayName;
        private readonly Label m_SampleDisplayName;
        private readonly VisualElement m_StateIcon;

        private readonly IPackageDatabase m_PackageDatabase;
        public SampleItem(IPageManager pageManager, IPackageDatabase packageDatabase) : base(pageManager)
        {
            m_PackageDatabase = packageDatabase;

            var leftContainer = new VisualElement();
            leftContainer.AddToClassList("left");
            Add(leftContainer);

            m_SampleDisplayName = new Label{ name = "sampleDisplayName" };
            leftContainer.Add(m_SampleDisplayName);

            m_ParentPackageDisplayName = new Label{ name = "parentPackageDisplayName" };
            leftContainer.Add(m_ParentPackageDisplayName);

            var rightContainer = new VisualElement();
            rightContainer.AddToClassList("right");
            Add(rightContainer);

            m_StateIcon = new VisualElement { name = "stateIcon" };
            rightContainer.Add(m_StateIcon);
        }

        public override void BindVisualState(VisualState newVisualState)
        {
            base.BindVisualState(newVisualState);

            var sample = m_PackageDatabase.GetSample(visualState?.itemUniqueId);
            if (sample.package == null)
                return;

            m_ParentPackageDisplayName.text = sample.package.displayName ?? string.Empty;
            m_SampleDisplayName.text = sample.displayName ?? string.Empty;

            m_StateIcon.ClearClassList();
            if (sample.isImported)
            {
                m_StateIcon.AddToClassList(Icon.Installed.ClassName(), nameof(SampleState.Installed).ToLower());
                m_StateIcon.tooltip = L10n.Tr("This sample is imported");
                UIUtils.SetElementDisplay(m_StateIcon, true);
            }
            else if (sample.previousImportPaths?.Count > 0)
            {
                m_StateIcon.AddToClassList(Icon.UpdateAvailable.ClassName(), nameof(SampleState.UpdateAvailable).ToLower());
                m_StateIcon.tooltip = L10n.Tr("A newer version of this sample is available.");
                UIUtils.SetElementDisplay(m_StateIcon, true);
            }
            else
            {
                UIUtils.SetElementDisplay(m_StateIcon, false);
            }
        }
    }
}
