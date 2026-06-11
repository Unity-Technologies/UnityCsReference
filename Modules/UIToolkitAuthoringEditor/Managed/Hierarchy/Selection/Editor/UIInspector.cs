// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal abstract class UIInspector : VisualElement, IDisposable
{
    class PostUpdateBinding : CustomBinding
    {
        UIInspector m_Inspector;

        public PostUpdateBinding(UIInspector inspector)
        {
            m_Inspector = inspector;
            updateTrigger = BindingUpdateTrigger.WhenDirty;
        }

        protected internal override BindingResult Update(in BindingContext context)
        {
            m_Inspector.SearchField.ApplyCurrentFilter();
            return base.Update(in context);
        }
    }

    static readonly BindingId k_PostUpdateBindingId = "search__post-update";
    const string k_PostUpdateElementName = "PostUpdate";

    protected InspectorSearchField SearchField { get; private set; }

    PostUpdateBinding m_PostUpdater;

    internal void InitializeSearchField(InspectorSearchField searchField)
    {
        SearchField = searchField;
        SearchField.SearchContainer = this;
        focusable = true;
        m_PostUpdater = new PostUpdateBinding(this);
        this.Q(k_PostUpdateElementName).SetBinding(k_PostUpdateBindingId, m_PostUpdater);
    }

    protected void MarkSearchDirty()
    {
        m_PostUpdater?.MarkDirty();
    }

    [EventInterest(typeof(FocusInEvent), typeof(BlurEvent))]
    protected override void HandleEventBubbleUp(EventBase evt)
    {
        if (SearchField != null)
        {
            switch (evt)
            {
                case FocusInEvent:
                {
                    SearchField.active = true;
                    break;
                }
                case BlurEvent:
                {
                    if (!Contains(focusController.focusedElement as VisualElement))
                        SearchField.active = false;
                    break;
                }
            }
        }

        base.HandleEventBubbleUp(evt);
    }

    internal void ResetSearch() => SearchField?.ResetSearch();

    public virtual void Dispose()
    {
        if (SearchField != null)
        {
            SearchField.ClearSearch();
            SearchField.active = false;
        }
    }
}
