// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.Experimental.RMGUI
{
    class VisualTreeBuilder
    {
        private struct ViewState
        {
            public readonly int childIndex;

            public ViewState(int childIndex)
            {
                this.childIndex = childIndex;
            }
        }

        private Recycler m_ElementPool;

        private VisualContainer m_CurrentContainer;
        private ViewState m_CurrentViewState;

        private Stack<ViewState> m_ViewStates;

        public VisualContainer topLevelVisualContainer { get; private set; }

        public bool verbose { get; set; }
        public IMScrollView currentScrollView { get; private set; }

        public VisualTreeBuilder(Recycler r)
        {
            m_ElementPool = r;
            m_ViewStates = new Stack<ViewState>();
            m_CurrentContainer = null;
            m_CurrentViewState = new ViewState(0);
            topLevelVisualContainer = null;
            verbose = false;
            currentScrollView = null;
        }

        public void NextView<TType>(out TType view) where TType : IMContainer, new()
        {
            NextElement(out view);

            m_CurrentContainer = view;
            m_ViewStates.Push(m_CurrentViewState);
            m_CurrentViewState = new ViewState(0);

            var scrollView = m_CurrentContainer as IMScrollView;
            if (scrollView != null)
            {
                currentScrollView = scrollView;
            }
        }

        public void EndView()
        {
            if (m_ViewStates.Count < 1)
            {
                Debug.Assert(m_CurrentContainer == topLevelVisualContainer);
                if (verbose)
                    Debug.LogError("Unexpected call to EndView()");
                return;
            }

            // Mostly for debugging right now, technically this could happen if users limit their GUI calls to EventType.Repaint
            // and put them in the pool
            if (m_CurrentContainer.childrenCount > m_CurrentViewState.childIndex)
            {
                RecycleDescendants(m_CurrentContainer, m_CurrentViewState.childIndex);
            }

            // at this point we MUST have a parent. If you hit this hierarchy is manipulated outside cache
            Debug.Assert(m_CurrentContainer.parent != null);

            if (m_CurrentContainer is IMScrollView)
            {
                // If we're ending a scroll VisualContainer, we need to find the next topmost scrollview.
                var view = m_CurrentContainer.parent;
                var scrollView = view as IMScrollView;
                // While we have a VisualContainer and that VisualContainer isn't a scroll VisualContainer, move up one parent VisualContainer.
                while (view != null && scrollView == null)
                {
                    view = view.parent;
                    scrollView = view as IMScrollView;
                }
                currentScrollView = scrollView;
            }

            m_CurrentViewState = m_ViewStates.Pop();
            m_CurrentContainer = m_CurrentContainer.parent;
        }

        public void BeginGUI(VisualContainer container)
        {
            topLevelVisualContainer = container;
            m_CurrentContainer = topLevelVisualContainer;
            m_CurrentViewState = new ViewState(0);
            m_ViewStates.Clear();
        }

        public void EndGUI()
        {
            if (topLevelVisualContainer == null)
            {
                Debug.Log("topLevelVisualContainer == null");
                return;
            }

            if (m_CurrentContainer != topLevelVisualContainer)
            {
                if (verbose)
                {
                    Debug.LogWarning("Non-symmetrical VisualContainer calls to GUI");
                }

                // to recover from this, we need to trim children in excess at every sub-VisualContainer level
                while (m_CurrentContainer != topLevelVisualContainer)
                {
                    EndView();
                }
            }

            // this is also necessary if we received less top-level controls
            if (topLevelVisualContainer.childrenCount > m_CurrentViewState.childIndex)
            {
                RecycleDescendants(topLevelVisualContainer, m_CurrentViewState.childIndex);
            }

            topLevelVisualContainer = null;
            m_CurrentContainer = null;
        }

        // in all cases reuse or allocates a widget of the correct type
        public void NextElement<TType>(out TType widget) where TType : VisualElement, IOnGUIHandler, new()
        {
            var type = typeof(TType);
            widget = null;

            // If we are over existing children count for current VisualContainer
            if (m_CurrentViewState.childIndex >= m_CurrentContainer.childrenCount)
            {
                // let's see if there are objects of the required type in the object pool
                widget = m_ElementPool.TryReuse<TType>() ?? new TType();
                m_CurrentContainer.AddChild(widget);
            }
            else
            {
                var current = m_CurrentContainer.GetChildAt(m_CurrentViewState.childIndex);
                // If the type matches at the same index, it's a successful cache read
                if (current.GetType() == type)
                {
                    widget = current as TType;
                }
                else
                {
                    var imElement = current as IOnGUIHandler;
                    if (imElement != null && imElement.id == IMElement.NonInteractiveControlID && !(imElement is VisualContainer))
                    {
                        m_CurrentContainer.RemoveChildAt(m_CurrentViewState.childIndex);
                        m_ElementPool.Trash(imElement);
                        NextElement(out widget);
                        return;
                    }
                    // otherwise still try to get from the pool
                    widget = m_ElementPool.TryReuse<TType>() ?? new TType();
                    // but insert at the specified index
                    m_CurrentContainer.InsertChild(m_CurrentViewState.childIndex, widget);
                }
            }

            Debug.Assert(widget != null);
            // move to next in tree
            m_CurrentViewState = new ViewState(m_CurrentViewState.childIndex + 1);
        }

        // Removes and pools children recursively, starting at the specified index
        void RecycleDescendants(VisualContainer parent, int startAtIndex)
        {
            while (parent.childrenCount > startAtIndex)
            {
                var widget = parent.GetChildAt(startAtIndex);
                var subView = widget as VisualContainer;
                if (subView != null)
                {
                    RecycleDescendants(subView, 0);
                }
                var im = widget as IOnGUIHandler;
                if (im != null)
                {
                    m_ElementPool.Trash(im);
                    parent.RemoveChild(widget);
                }
                else
                {
                    // skip this one its RMGUI
                    startAtIndex++;
                }
            }
        }
    }
}
