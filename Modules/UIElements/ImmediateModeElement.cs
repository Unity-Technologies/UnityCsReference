// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
}
