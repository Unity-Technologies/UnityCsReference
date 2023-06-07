// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Represents a visual element that displays information on a data binding group.
    /// </summary>
    class BindingConverterGroupViewItem : VisualElement
    {
        private static readonly string s_UssClassName = "binding-converter-group-item";
        private static readonly string s_IncompatibleUssClassName = s_UssClassName + "--incompatible";
        private static readonly string s_CompatibleUssClassName = s_UssClassName + "--compatible";
        private static readonly string s_UnknownCompabilityUssClassName = s_UssClassName + "--unknown-compatibility";
        private static readonly string s_CurrentEntryUssClassName = s_UssClassName + "--current-entry";
        public static readonly string s_CheckUssClassName = s_UssClassName + "--checked";
        public static readonly string s_IconElementName = "icon-element";
        public static readonly string s_LabelElementName = "label-element";
        public static readonly string s_DeleteButtonName = "delete-button";

        private Label m_Label;
        private Button m_DeleteButton;

        private ConverterGroup m_Group;
        private BindingCompatibilityStatus m_Compatibility;

        public Func<ConverterGroup, string> getTextFromDataCallback { get; set; }

        /// <summary>
        /// The group displayed by this view
        /// </summary>
        public ConverterGroup group => m_Group;

        /// <summary>
        /// Sets the group to display
        /// </summary>
        /// <param name="group">The group to display</param>
        /// <param name="compatibility">The compatibility of the group</param>
        public void SetGroup(ConverterGroup group, BindingCompatibilityStatus compatibility)
        {
            m_Group = group;
            m_Compatibility = compatibility;
            UpdateFromGroup();
        }

        /// <summary>
        /// Callback invoked when clicking on the Delete button
        /// </summary>
        public Action onDeleteButtonClicked
        {
            set
            {
                if (value != null && m_DeleteButton == null)
                {
                    m_DeleteButton = new Button() { name = s_DeleteButtonName };
                    Add(m_DeleteButton);
                }
                else if (value == null && m_DeleteButton != null)
                {
                    Remove(m_DeleteButton);
                    m_DeleteButton = null;
                }

                if (m_DeleteButton != null)
                    m_DeleteButton.clickable = new Clickable(value);
            }
        }

        /// <summary>
        /// Constructs a converter group view item
        /// </summary>
        public BindingConverterGroupViewItem()
        {
            AddToClassList(s_UssClassName);
            Add(new VisualElement() { name = s_IconElementName });
            Add(m_Label = new Label() { name = s_LabelElementName });
        }

        private void UpdateFromGroup()
        {
            m_Label.text = getTextFromDataCallback?.Invoke(m_Group) ?? m_Group.id;

            var tooltipStr = string.Empty;

            if (!string.IsNullOrEmpty(m_Group.displayName))
                tooltipStr = $"Converter Group Name: {m_Group.displayName}\n";
            tooltipStr += $"Converter Group ID: {m_Group.id}\n";
            if (!string.IsNullOrEmpty(m_Group.description))
                tooltipStr += $"Description: {m_Group.description}";

            tooltip = tooltipStr;

            EnableInClassList(s_CurrentEntryUssClassName, m_Group.id == BuilderConstants.CompleterCurrentEntryItemId);
            EnableInClassList(s_IncompatibleUssClassName, m_Compatibility == BindingCompatibilityStatus.Incompatible);
            EnableInClassList(s_CompatibleUssClassName, m_Compatibility == BindingCompatibilityStatus.Compatible);
            EnableInClassList(s_UnknownCompabilityUssClassName, m_Compatibility == BindingCompatibilityStatus.Unknown);
        }
    }
}
