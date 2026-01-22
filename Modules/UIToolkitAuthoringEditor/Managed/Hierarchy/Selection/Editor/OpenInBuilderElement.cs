// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
class OpenInBuilderElement : VisualElement
{
    [Serializable]
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
        /// <summary>
        /// This is used by the code generator when a custom control is using the <see cref="UxmlElementAttribute"/>. You should not need to call it.
        /// </summary>
        [Conditional("UNITY_EDITOR"), RegisterUxmlCache]
        public new static void Register()
        {
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new(nameof(VisualTreeAsset), "visual-tree-asset"),
                }
                , true);
        }

#pragma warning disable 649
        [SerializeField] private VisualTreeAsset VisualTreeAsset;

        [SerializeField, UxmlIgnore, HideInInspector] private UxmlAttributeFlags VisualTreeAsset_UxmlAttributeFlags;
#pragma warning restore 649

        public override object CreateInstance()
        {
            return new OpenInBuilderElement();
        }

        public override void Deserialize(object obj)
        {
            base.Deserialize(obj);
            var ve = (OpenInBuilderElement)obj;
            if (ShouldWriteAttributeValue(VisualTreeAsset_UxmlAttributeFlags))
                ve.VisualTreeAsset = VisualTreeAsset;
        }
    }

    public const string UssClass = "unity-open-in-builder-element";
    public const string HelpBoxUssClass = UssClass + "__help-box";
    public const string OpenVtaButtonUssClass = UssClass + "__button";
    public const string HiddenOpenVtaButtonUssClass = OpenVtaButtonUssClass + "--hidden";

    public static readonly BindingId VisualTreeAssetProperty = nameof(VisualTreeAsset);

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/OpenInBuilderElement.uxml";

    private readonly Button m_OpenInBuilderButton;

    private VisualTreeAsset m_VisualTreeAsset;

    [UxmlAttribute, CreateProperty]
    public VisualTreeAsset VisualTreeAsset
    {
        get => m_VisualTreeAsset;
        set
        {
            if (m_VisualTreeAsset == value)
                return;

            m_VisualTreeAsset = value;
            if (m_VisualTreeAsset)
            {
                var path = AssetDatabase.GetAssetPath(m_VisualTreeAsset.GetEntityId());
                var isValidPath = !string.IsNullOrEmpty(path);
                m_OpenInBuilderButton.EnableInClassList(HiddenOpenVtaButtonUssClass, !isValidPath);
                m_OpenInBuilderButton.text = isValidPath
                    ? $"Open '{path}' in the UI Builder."
                    : null;
            }
            else
            {
                m_OpenInBuilderButton.AddToClassList(HiddenOpenVtaButtonUssClass);
            }
            NotifyPropertyChanged(VisualTreeAssetProperty);
        }
    }

    public OpenInBuilderElement()
    {
        AddToClassList(UssClass);

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        m_OpenInBuilderButton = this.Q<Button>(className:OpenVtaButtonUssClass);
        m_OpenInBuilderButton.AddToClassList(HiddenOpenVtaButtonUssClass);
        m_OpenInBuilderButton.clicked += OpenInUIBuilder;
    }

    private void OpenInUIBuilder()
    {
        if (!m_VisualTreeAsset)
            return;

        AssetDatabase.OpenAsset(m_VisualTreeAsset.GetEntityId());
    }
}
