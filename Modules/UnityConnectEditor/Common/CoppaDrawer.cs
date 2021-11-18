// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Connect
{
    /// <summary>
    /// A container that fetches the UIElements that draw the COPPA compliance UI and subscribes to its events.
    /// </summary>
    public class CoppaDrawer : IProjectEditorDrawer, IDisposable
    {
        VisualElement m_VisualElement;

        /// <summary>
        /// The default constructor for the COPPA UI drawer that subscribes to the callback events
        /// for the Cancel and Save buttons.
        /// </summary>
        public CoppaDrawer()
        {
            m_VisualElement = new VisualElement();
            var coppaManager = new CoppaManager(m_VisualElement)
            {
                cancelButtonCallback = _ =>
                {
                    OnProjectStateChanged();
                },
                saveButtonCallback = _ =>
                {
                    OnProjectStateChanged();
                },
                exceptionCallback = OnExceptionCallback
            };
        }

        /// <summary>
        /// An event that fires when any button causes a state change within the COPPA compliance UI.
        /// </summary>
        public event Action stateChangeButtonFired;

        /// <summary>
        /// An event that fires when an exception is thrown within the COPPA Compliance UI.
        /// The COPPA value is the original value prior to the exception being thrown.
        /// </summary>
        public event Action<CoppaCompliance, Exception> exceptionCallback;

        /// <summary>
        /// A method that retrieves the COPPA Compliance UI.
        /// </summary>
        /// <returns>Returns the visual element containing the COPPA Compliance UI.</returns>
        public VisualElement GetVisualElement()
        {
            return m_VisualElement;
        }

        void OnProjectStateChanged()
        {
            stateChangeButtonFired?.Invoke();
        }

        void OnExceptionCallback(CoppaCompliance coppaCompliance, Exception exception)
        {
            if (exceptionCallback != null)
            {
                exceptionCallback.Invoke(coppaCompliance, exception);
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
            stateChangeButtonFired = null;
            exceptionCallback = null;
        }
    }
}
