// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class RegistryItem : VisualElement
    {
        private static readonly string k_AddNewScopedRegistryText = L10n.Tr("New Scoped Registry");
        private const string k_SelectedRegistryClass = "selectedRegistry";
        private const string k_ErrorIconClasses = "unity__icon unity__icon--error";
        private readonly string k_NonCompliantRegistry = L10n.Tr("Restricted scoped registry");

        private readonly Label m_Label;
        private readonly VisualElement m_NonCompliantErrorIcon;

        private readonly RegistryInfo m_RegistryInfo;

        public RegistryItem(RegistryInfo registryInfo)
        {
            m_RegistryInfo = registryInfo;

            m_Label = new Label();
            m_Label.text = registryInfo?.name ?? k_AddNewScopedRegistryText;
            Add(m_Label);

            m_NonCompliantErrorIcon = new VisualElement();
            m_NonCompliantErrorIcon.AddClasses(k_ErrorIconClasses);
            m_NonCompliantErrorIcon.tooltip = k_NonCompliantRegistry;
            Add(m_NonCompliantErrorIcon);
            UIUtils.SetElementDisplay(m_NonCompliantErrorIcon, registryInfo?.compliance.status == RegistryComplianceStatus.NonCompliant);
        }

        public RegistryItem RefreshDraft(RegistryInfoDraft draft)
        {
            if (draft is null || (draft.original?.id ?? string.Empty) != (m_RegistryInfo?.id ?? string.Empty))
                return this;

            m_Label.text = draft.hasUnsavedChanges ? $"* {draft.name}" : m_RegistryInfo?.name ?? k_AddNewScopedRegistryText;
            return this;
        }

        public RegistryItem SetLabelClick(Action action)
        {
            m_Label.OnLeftClick(action);
            return this;
        }

        public RegistryItem SetSelected(bool selected)
        {
            EnableInClassList(k_SelectedRegistryClass, selected);
            return this;
        }
    }
}
