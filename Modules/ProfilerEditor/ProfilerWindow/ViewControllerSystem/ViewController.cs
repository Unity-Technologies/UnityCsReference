// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    // Base view controller class. A view controller is responsible for managing a single, logical unit of UI, known as a 'view'. View controllers may embed the views of other view controllers to form a modular hierarchy.
    abstract class ViewController : IDisposable
    {
        // The view owned by this view controller.
        VisualElement m_View;

        // The view controller's child view controllers.
        readonly List<ViewController> m_Children = new();

        // The view controller's view. If the view does not exist when this method is called, it will be created.
        public VisualElement View
        {
            get
            {
                if (m_View == null)
                {
                    m_View = LoadView();
                    if (m_View == null)
                        throw new InvalidOperationException($"View controller did not create a view. Ensure your view controller's LoadView method returns a non-null VisualElement.");

                    ViewLoaded();
                }

                return m_View;
            }
        }

        // Has the view controller's view been loaded?
        public bool IsViewLoaded => m_View != null;

        // Has this view controller been disposed?
        public bool IsDisposed { get; private set; }

        // Dispose this view controller. This calls Dispose on all children before removing its view from the view hierarchy.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // LoadView is called the first time the view controller's view is requested for display. Override this method to create the view controller's view.
        protected abstract VisualElement LoadView();

        // ViewLoaded is called immediately after the view controller's view has been created. Override this method to perform one-time view setup.
        protected virtual void ViewLoaded() { }

        // Dispose is called when the view controller is being disposed from memory. Override this method to perform one-time view clean-up. You must call the base class implementation after yours, as is standard in the C# Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                foreach (var child in m_Children)
                    child.Dispose();

                m_View?.RemoveFromHierarchy();
                m_View = null;
            }

            IsDisposed = true;
        }

        // Add viewController as a child of this view controller.
        protected void AddChild(ViewController viewController)
        {
            m_Children.Add(viewController);
        }

        // Remove viewController from the children of this view controller.
        protected void RemoveChild(ViewController viewController)
        {
            m_Children.Remove(viewController);
        }
    }
}
