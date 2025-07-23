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
class VisualTreeAssetHeader : UISelectionObjectHeader
{
    [Serializable]
    public new class UxmlSerializedData : UISelectionObjectHeader.UxmlSerializedData
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
            return new VisualTreeAssetHeader();
        }

        public override void Deserialize(object obj)
        {
            base.Deserialize(obj);
            var ve = (VisualTreeAssetHeader)obj;
            if (ShouldWriteAttributeValue(VisualTreeAsset_UxmlAttributeFlags))
                ve.VisualTreeAsset = VisualTreeAsset;
        }
    }

    public static readonly BindingId VisualTreeAssetProperty = nameof(VisualTreeAsset);

    public new const string UssClass = "unity-visual-tree-asset-header";
    public const string AssetPathUssClass = UssClass + "__asset-path";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/VisualTreeAssetHeader.uxml";
    private const string k_NoAssetPath = "<none>.uxml";

    private VisualTreeAsset m_VisualTreeAsset;
    private TextField m_AssetPath;

    [UxmlAttribute, CreateProperty]
    public VisualTreeAsset VisualTreeAsset
    {
        get => m_VisualTreeAsset;
        set
        {
            if (m_VisualTreeAsset == value)
                return;
            m_VisualTreeAsset = value;
            m_AssetPath.value = m_VisualTreeAsset
                ? AssetDatabase.GetAssetPath(m_VisualTreeAsset.GetEntityId())
                : k_NoAssetPath;
            NotifyPropertyChanged(VisualTreeAssetProperty);
        }
    }

    protected override VisualTreeAsset IdentifierDetails => EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;

    public VisualTreeAssetHeader()
    {
        AddToClassList(UssClass);

        TypeIcon = UIResources.GetIconForType(typeof(TemplateContainer), UIResources.RequestSize.Px32);
        TypeName = nameof(VisualTreeAsset);

        m_AssetPath = this.Q<TextField>(className: AssetPathUssClass);
        m_AssetPath.value = k_NoAssetPath;
        m_AssetPath.isReadOnly = true;
    }
}
