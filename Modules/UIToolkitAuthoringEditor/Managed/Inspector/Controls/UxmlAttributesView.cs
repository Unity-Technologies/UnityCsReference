// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Represents a view to UXML attributes bound to a UXML attributes editing context.
/// </summary>
abstract class UxmlAttributesView : VisualElement
{
    public new class UxmlSerializedData : VisualElement.UxmlSerializedData
    {
    }

    public const string UssClassName = "unity-uxml-attributes-view";

    UxmlAttributesEditingContext m_Context;

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
                if (m_Context.element != null)
                {
                    ReleaseViewContent(m_Context);
                }
            }

            m_Context = value;

            if (m_Context != null)
            {
                m_Context.contextChanged += OnContextChanged;

                if (m_Context.element != null)
                {
                    CreateViewContent(m_Context);
                }
            }
        }
    }

    /// <summary>
    /// Constructor for the UxmlAttributesView.
    /// </summary>
    public UxmlAttributesView()
    {
        AddToClassList(UssClassName);
    }

    /// <summary>
    /// Shares the context with another UxmlAttributesView.
    /// </summary>
    /// <param name="otherView">The other view to share the editing context with</param>
    public void ShareContext(UxmlAttributesView otherView)
    {
        Context = otherView.Context;
    }

    /// <summary>
    /// Creates the content of the view.
    /// </summary>
    protected abstract void CreateViewContent(UxmlAttributesEditingContext context);

    /// <summary>
    /// Destroys the content of the view.
    /// </summary>
    protected abstract void ReleaseViewContent(UxmlAttributesEditingContext context);

    void OnContextChanged(object sender, UxmlAttributesEditingContext.ContextChangedEventArgs args)
    {
        if (childCount > 0)
        {
            ReleaseViewContent(Context);
            Clear();
        }

        if (args.newElement != null)
        {
            CreateViewContent(Context);
        }
    }
}
