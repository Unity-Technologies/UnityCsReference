// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Represents a view to UXML attributes bound to a UXML attributes editing context.
/// </summary>
[UxmlElement]
partial class UxmlAttributesView : VisualElement
{
    public const string UssClassName = "unity-uxml-attributes-view";

    UxmlAttributesEditingContext m_Context;
    readonly UxmlSerializedDataPropertyView m_RootPropertyView;

    public override VisualElement contentContainer => (VisualElement)m_RootPropertyView ?? this;

    /// <summary>
    /// The editing context used to edit UXML attributes of the selected VisualElement.
    /// </summary>
    internal protected UxmlAttributesEditingContext Context {
        get => m_Context;
        set
        {
            if (m_Context == value)
                return;

            if (m_Context != null)
            {
                m_Context.contextChanged -= OnContextChanged;
            }

            m_Context = value;
            m_RootPropertyView.context = m_Context;

            if (m_Context != null)
            {
                m_Context.contextChanged += OnContextChanged;
            }
            Rebind();
            UpdateEnabledState();
        }
    }

    /// <summary>
    /// Event sent when the context changes.
    /// </summary>
    public event EventHandler<UxmlAttributesEditingContext.ContextChangedEventArgs> ContextChanged;

    /// <summary>
    /// Constructor for the UxmlAttributesView.
    /// </summary>
    public UxmlAttributesView()
    {
        AddToClassList(UssClassName);

        m_RootPropertyView = new UxmlSerializedDataPropertyView();
        hierarchy.Add(m_RootPropertyView);
        Context = new UxmlAttributesEditingContext(new UxmlAttributesEditingController());
    }

    /// <summary>
    /// Shares the context with another UxmlAttributesView.
    /// </summary>
    /// <param name="otherView">The other view to share the editing context with</param>
    public void ShareContext(UxmlAttributesView otherView)
    {
        Context = otherView.Context;
    }

    void UpdateEnabledState()
    {
        SetEnabled(Context is {isReadOnly: false});
    }

    public void Rebind()
    {
        m_RootPropertyView.Unbind();

        if (Context != null && Context.rootSerializedObject != null)
        {
            m_RootPropertyView.bindingPath = m_Context.serializedBasePath;
            m_RootPropertyView.Bind(m_Context.rootSerializedObject);
        }
    }

    void OnContextChanged(object sender, UxmlAttributesEditingContext.ContextChangedEventArgs args)
    {
        UpdateEnabledState();
        NotifyContextChanged(args);
        Rebind();
    }

    void NotifyContextChanged(UxmlAttributesEditingContext.ContextChangedEventArgs args)
    {
        ContextChanged?.Invoke(this, args);
    }
}
