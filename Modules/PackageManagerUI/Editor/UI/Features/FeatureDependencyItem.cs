// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class FeatureDependencyItem : VisualElement
    {
        public IPackageVersion packageVersion { get; private set; }

        public FeatureDependencyItem(IPackageVersion featureVersion, IPackageVersion featureDependencyVersion, FeatureState state = FeatureState.None)
        {
            packageVersion = featureDependencyVersion;

            m_Name = new Label { name = "name" };
            m_Name.text = featureDependencyVersion?.displayName ?? string.Empty;
            Add(m_Name);

            m_State = new VisualElement { name = "versionState" };
            if (state == FeatureState.Customized && featureVersion.isInstalled)
            {
                m_State.AddToClassList(state.ToString().ToLower());
                m_State.tooltip = L10n.Tr("This package has been manually customized");
            }

            Add(m_State);
        }

        private Label m_Name;
        private VisualElement m_State;
    }
}
