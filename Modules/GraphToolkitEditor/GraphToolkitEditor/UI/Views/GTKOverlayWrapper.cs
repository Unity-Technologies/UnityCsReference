// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor;

abstract class GTKOverlayWrapper : VisualElement
{
    public abstract void DisposeRoot();
    public abstract VisualElement RootView { get; set; }
}

class GTKOverlayWrapper<T> : GTKOverlayWrapper where T : RootView
{
    public GTKOverlayWrapper()
    {
        this.style.flexGrow = 1;
    }

    VisualElement m_RootView;

    public override VisualElement RootView
    {
        get => m_RootView;
        set
        {
            if (m_RootView != null && m_RootView.parent == this)
            {
                Remove(m_RootView);
            }

            m_RootView = value;

            if (m_RootView != null)
            {
                Add(m_RootView);
            }
        }
    }

    public override void DisposeRoot()
    {
        if (RootView is T rootView)
        {
            rootView.Dispose();
            RootView = null;
        }
    }
}
