// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// View that displays details information about the selected or hovered converter group in a converter group field completer
    /// </summary>
    class BindingConverterGroupDetailsView : VisualElement
    {
        static readonly string s_UssClassName = "binding-converter-group-details-view";
        private static readonly string s_IncompatibleUssClassName = s_UssClassName + "--incompatible";
        private static readonly string s_CompatibleUssClassName = s_UssClassName + "--compatible";
        private static readonly string s_UnknownCompabilityUssClassName = s_UssClassName + "--unknown-compatibility";
        private static readonly string s_CurrentEntryUssClassName = s_UssClassName + "--current-entry";
        private static readonly string s_NoNameUssClassName = s_UssClassName + "--noname";
        private static readonly string s_NoDescriptionUssClassName = s_UssClassName + "--nodesc";
        private static readonly string s_UxmlFilename = BuilderConstants.UIBuilderPackagePath + "/Inspector/ConverterGroupDetailsView.uxml";

        private Label m_IdLabel;
        private Label m_NameLabel;
        private Label m_DescriptionLabel;
        private Label m_CompatibilityLabel;

        /// <summary>
        /// Constructs a view.
        /// </summary>
        public BindingConverterGroupDetailsView()
        {
            AddToClassList(s_UssClassName);

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_UxmlFilename);
            template.CloneTree(this);

            m_IdLabel = this.Q<Label>("id-label");
            m_NameLabel = this.Q<Label>("name-label");
            m_DescriptionLabel = this.Q<Label>("description-label");
            m_CompatibilityLabel = this.Q<Label>("compatibility-label");
        }

        /// <summary>
        /// Sets the group
        /// </summary>
        /// <param name="group">The group</param>
        public void SetGroup(ConverterGroup group, BindingCompatibilityStatus compatibility)
        {
            var groupId = string.Empty;
            var description = string.Empty;
            bool isCurrentEntryItem = false;

            if (group.id == BuilderConstants.CompleterCurrentEntryItemId)
            {
                groupId = string.Empty;
                description = BuilderConstants.BindingWindowConverterCompleter_SelectEditedText;
                isCurrentEntryItem = true;
            }
            else
            {
                groupId = group.id;
                description = group.description;
            }

            m_IdLabel.text = groupId;

            m_NameLabel.text = group.displayName;
            EnableInClassList(s_NoNameUssClassName, string.IsNullOrEmpty(group.displayName));

            m_DescriptionLabel.text = description;
            EnableInClassList(s_NoDescriptionUssClassName, string.IsNullOrEmpty(description));

            EnableInClassList(s_CurrentEntryUssClassName, isCurrentEntryItem);

            m_CompatibilityLabel.text = compatibility == BindingCompatibilityStatus.Incompatible ? BuilderConstants.BindingWindowConverterCompleter_IncompatibleMessage
                : compatibility == BindingCompatibilityStatus.Compatible ? BuilderConstants.BindingWindowConverterCompleter_CompatibleMessage
                : BuilderConstants.BindingWindowConverterCompleter_UnknownCompatibilityMessage;

            EnableInClassList(s_IncompatibleUssClassName, compatibility == BindingCompatibilityStatus.Incompatible);
            EnableInClassList(s_CompatibleUssClassName, compatibility == BindingCompatibilityStatus.Compatible);
            EnableInClassList(s_UnknownCompabilityUssClassName, compatibility == BindingCompatibilityStatus.Unknown);
        }
    }
}
