// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement]
partial class VisualElementHeader : UISelectionObjectHeader
{
    public static readonly BindingId ElementProperty = nameof(Element);

    public new const string UssClass = "unity-visual-element-header";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/VisualElementHeader.uxml";

    private VisualElement m_Element;
    UxmlAttributesView m_AttributesView;

    public UxmlAttributesView AttributesView => m_AttributesView;

    protected override VisualTreeAsset IdentifierDetails => EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;

    [CreateProperty]
    public VisualElement Element
    {
        get => m_Element;
        set
        {
            if (m_Element == value)
                return;
            m_Element = value;
            if (m_Element == null)
            {
                TypeIcon = UIResources.GetIconForType(typeof(VisualElement), UIResources.RequestSize.Px32);
                TypeName = nameof(VisualElement);
            }
            else
            {
                TypeIcon = UIResources.GetIconForElement(m_Element, UIResources.RequestSize.Px32);
                TypeName = TypeUtility.GetTypeDisplayName(m_Element.GetType());
            }
            NotifyPropertyChanged(ElementProperty);
        }
    }

    public VisualElementHeader()
    {
        AddToClassList(UssClass);

        TypeIcon = UIResources.GetIconForType(typeof(VisualElement), UIResources.RequestSize.Px32);
        TypeName = nameof(VisualElement);

        m_AttributesView = this.Q<UxmlAttributesView>();
    }
}
