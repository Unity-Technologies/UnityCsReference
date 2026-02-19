// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Internal;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Represents an inspector element that displays bindings information of a selected VisualElement.
/// </summary>
/// <remarks>
/// This view only shows data source information.
/// </remarks>
[UxmlElement]
sealed class VisualElementBindingsInspectorElement : UxmlAttributesView
{
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

        [ExcludeFromDocs]
        public override object CreateInstance()
        {
            return new VisualElementBindingsInspectorElement();
        }
    }

    public new const string UssClassName = "unity-bindings-inspector";
    const string k_VisualTreeAssetPath = "UIToolkitAuthoring/Inspector/VisualElementBindingsInspectorElement.uxml";

    VisualTreeAsset m_VisualTreeAsset;
    UxmlSerializedDataPropertyView m_RootPropertyView;

    /// <summary>
    /// Constructor for the VisualElementBindingsInspectorElement.
    /// </summary>
    public VisualElementBindingsInspectorElement()
    {
        AddToClassList(UssClassName);
    }

    protected override void CreateViewContent(UxmlAttributesEditingContext context)
    {
        // If there is no UxmlSerializedData, there are no bindings to show.
        if (context.uxmlSerializedDataDescription == null)
        {
            return;
        }

        if (m_VisualTreeAsset == null)
        {
            m_VisualTreeAsset = EditorGUIUtility.LoadRequired(k_VisualTreeAssetPath) as VisualTreeAsset;
        }
        m_VisualTreeAsset.CloneTree(this);
        m_RootPropertyView = this.Q<UxmlSerializedDataPropertyView>("RootElement");
        m_RootPropertyView.context = context;
        m_RootPropertyView.bindingPath = context.serializedBasePath;
        m_RootPropertyView.Bind(context.rootSerializedObject);
    }

    protected override void ReleaseViewContent(UxmlAttributesEditingContext context)
    {
        if (m_RootPropertyView == null)
            return;
        m_RootPropertyView.Unbind();
        m_RootPropertyView.context = null;
        m_RootPropertyView = null;
    }
}
