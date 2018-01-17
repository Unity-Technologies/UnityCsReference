// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements;

namespace UnityEngine.Internal.Experimental.UIElements
{
    /// <summary>
    /// <c>Panel</c> being internal, this wrapper is used in Graphics Tests to perform basic operations that would
    /// otherwise only be possible with reflection. We prefer to avoid using reflection because it is cumbersome and
    /// only detected at runtime which would add latency to the development process.
    /// </summary>
    public class PanelWrapper : ScriptableObject
    {
        private Panel m_Panel;

        private void OnEnable()
        {
            m_Panel = UIElementsUtility.FindOrCreatePanel(this);
        }

        private void OnDisable()
        {
            m_Panel = null;
        }

        public VisualElement visualTree
        {
            get
            {
                return m_Panel.visualTree;
            }
        }

        public void Repaint(Event e)
        {
            m_Panel.Repaint(e);
        }
    }
}
