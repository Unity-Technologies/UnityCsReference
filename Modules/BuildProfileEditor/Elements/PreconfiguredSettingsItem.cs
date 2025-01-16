// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using UnityEditor.Modules;
using UnityEngine;
using System.IO;

namespace UnityEditor.Build.Profile
{
    internal class PreconfiguredSettingsItem : VisualElement
    {
        const string k_Uxml = "BuildProfile/UXML/PreconfiguredSettingsItemElement.uxml";

        readonly Toggle m_Toggle;
        readonly Label m_Label;
        PreconfiguredSettingsVariant m_Variant;

        internal delegate void OnPreconfiguredSettingsChanged();
        OnPreconfiguredSettingsChanged m_Changed;

        internal PreconfiguredSettingsItem()
        {
            var uxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var stylesheet = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            styleSheets.Add(stylesheet);
            uxml.CloneTree(this);

            m_Toggle = this.Q<Toggle>("settings-list-toggle");
            m_Label = this.Q<Label>("settings-list-label");
            m_Label = this.Q<Label>("settings-list-label");

            m_Toggle.RegisterValueChangedCallback(OnValueChanged);
        }

        internal void Set(PreconfiguredSettingsVariant variant, OnPreconfiguredSettingsChanged changed, string tooltip)
        {
            m_Variant = variant;
            m_Label.text = variant.Name;
            m_Toggle.SetValueWithoutNotify(variant.Selected);
            m_Changed = changed;
        }

        internal void Select()
        {
            m_Toggle.value = true;
        }

        internal void SelectInitialState()
        {
            m_Toggle.value = m_Variant.SelectedInitially;
        }

        internal void Deselect()
        {
            m_Toggle.value = false;
        }

        void OnValueChanged(ChangeEvent<bool> evt)
        {
            if (m_Variant != null)
                m_Variant.Selected = evt.newValue;
            if (m_Changed != null)
                m_Changed.Invoke();
        }
    }
}
