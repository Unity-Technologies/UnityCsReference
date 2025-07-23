// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal sealed class VisualTreeAssetInspector : VisualElement
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
            return new VisualTreeAssetInspector();
        }
    }

    public static readonly BindingId VisualTreeAssetProperty = nameof(VisualTreeAsset);

    public const string UssClass = "unity-visual-tree-asset-inspector";
    public const string HeaderUssClass = UssClass + "__header";
    public const string OpenInBuilderUssClass = UssClass + "__open-in-builder-button";

    private const string k_VisualTreeAsset = "UIToolkitAuthoring/Inspector/VisualTreeAssetInspector.uxml";

    private VisualTreeAsset m_VisualTreeAsset;

    private readonly VisualTreeAssetHeader m_Header;
    private readonly OpenInBuilderElement m_OpenInBuilder;

    [CreateProperty]
    public VisualTreeAsset VisualTreeAsset
    {
        get => m_VisualTreeAsset;
        set
        {
            if (m_VisualTreeAsset == value)
                return;
            m_VisualTreeAsset = value;

            m_Header.VisualTreeAsset = m_VisualTreeAsset;
            m_OpenInBuilder.VisualTreeAsset = m_VisualTreeAsset;
            NotifyPropertyChanged(VisualTreeAssetProperty);
        }
    }

    public VisualTreeAssetInspector()
    {
        AddToClassList(UssClass);

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta.CloneTree(this);

        m_Header = this.Q<VisualTreeAssetHeader>(className:HeaderUssClass);
        m_Header.SetEnabled(false);
        m_OpenInBuilder = this.Q<OpenInBuilderElement>(className:OpenInBuilderUssClass);
    }
}
