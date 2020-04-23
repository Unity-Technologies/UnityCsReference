using System;

namespace UnityEngine.UIElements
{
    public abstract class ImmediateModeElement : VisualElement
    {
        // If true, skip callback when outside the viewport
        private bool m_CullingEnabled = false;
        public bool cullingEnabled
        {
            get { return m_CullingEnabled; }
            set { m_CullingEnabled = value; IncrementVersion(VersionChangeType.Repaint); }
        }

        public ImmediateModeElement()
        {
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            mgc.painter.DrawImmediate(ImmediateRepaint, cullingEnabled);
        }

        protected abstract void ImmediateRepaint();
    }

    // Used to wrap the exception thrown by the immediate callback
    class ImmediateModeException : Exception
    {
        public ImmediateModeException(Exception inner)
            : base("", inner)
        {
        }
    }
}
