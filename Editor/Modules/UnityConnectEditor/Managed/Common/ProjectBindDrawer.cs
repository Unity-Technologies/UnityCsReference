// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Connect
{
    /// <summary>
    /// A container that fetches the UIElements that draw the Project Binding UI, and subscribes to its events.
    /// </summary>
    public class ProjectBindDrawer : IProjectEditorDrawer, IDisposable
    {
        VisualElement m_VisualElement;
        ProjectBindManager m_ProjectBindManager;

        /// <summary>
        /// The default constructor for the Drawer that subscribes to the callback events
        /// for the Link and Create buttons.
        /// </summary>
        public ProjectBindDrawer()
        {
            m_VisualElement = new VisualElement();
            m_ProjectBindManager = new ProjectBindManager(m_VisualElement)
            {
                linkButtonCallback = _ =>
                {
                    OnProjectStateChanged();
                },
                createButtonCallback = _ =>
                {
                    OnProjectStateChanged();
                },
                exceptionCallback = OnExceptionCallback
            };
        }

        /// <summary>
        /// An event that fires when any button within the Project Bind UI causes a state change.
        /// </summary>
        public event Action stateChangeButtonFired;

        /// <summary>
        /// An event that fires when an exception is thrown within the Project Bind UI.
        /// </summary>
        public event Action<Exception> exceptionCallback;

        /// <summary>
        /// Retrieves the ProjectBind UI.
        /// </summary>
        /// <returns>Returns the visual element containing the ProjectBind UI.</returns>
        public VisualElement GetVisualElement()
        {
            return m_VisualElement;
        }

        void OnProjectStateChanged()
        {
            stateChangeButtonFired?.Invoke();
        }

        void OnExceptionCallback(Exception exception)
        {
            if (exceptionCallback != null)
            {
                exceptionCallback.Invoke(exception);
            }
            else
            {
                Debug.LogError(exception);
            }
        }

        /// <summary>
        /// Disposes of the UI
        /// </summary>
        public void Dispose()
        {
            m_VisualElement = null;
            m_ProjectBindManager?.Dispose();
            stateChangeButtonFired = null;
            exceptionCallback = null;
        }
    }
}
