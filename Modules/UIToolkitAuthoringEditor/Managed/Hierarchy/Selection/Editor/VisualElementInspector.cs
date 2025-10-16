// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class VisualElementInspector : VisualElement
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
            UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
        }

        public override object CreateInstance()
        {
            return new VisualElementInspector();
        }
    }

    public static readonly BindingId ElementProperty = nameof(Element);

    public const string UssClass = "unity-visual-element-inspector";
    public const string HeaderUssClass = UssClass + "__header";
    public const string OpenInBuilderUssClass = UssClass + "__open-in-builder-button";
    public const string StyleInspectorClass = UssClass + "__style-inspector";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/VisualElementInspector.uxml";
    private const string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorDark.uss";
    private const string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/UIToolkitAuthoringInspectorLight.uss";

    private VisualElement m_Element;

    private readonly VisualElementHeader m_Header;
    private readonly OpenInBuilderElement m_OpenInBuilder;
    private readonly StyleInspectorElement m_StyleInspector;

    [CreateProperty]
    public VisualElement Element
    {
        get => m_Element;
        set
        {
            if (m_Element == value)
                return;
            m_Element = value;

            m_Header.Element = m_Element;

            if (m_Element == null)
            {
                m_OpenInBuilder.VisualTreeAsset = null;
                m_StyleInspector.Element = null;
            }
            else
            {
                var visualTreeAsset = m_Element.visualTreeAssetSource
                    ? m_Element.visualTreeAssetSource
                    : m_Element.GetFirstAncestorWhere(ve => ve.visualTreeAssetSource)?.visualTreeAssetSource;
                m_OpenInBuilder.VisualTreeAsset = visualTreeAsset;
                m_StyleInspector.Element = m_Element;
            }
            NotifyPropertyChanged(ElementProperty);
        }
    }

    public VisualElementInspector()
    {
        AddToClassList(UssClass);

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        styleSheets.Add(styleSheet);

        m_Header = this.Q<VisualElementHeader>(className:HeaderUssClass);
        m_Header.SetEnabled(false);
        m_OpenInBuilder = this.Q<OpenInBuilderElement>(className:OpenInBuilderUssClass);
        m_StyleInspector = this.Q<StyleInspectorElement>(className:StyleInspectorClass);
    }
}
