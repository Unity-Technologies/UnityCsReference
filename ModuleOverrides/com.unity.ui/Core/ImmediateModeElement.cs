// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// VisualElement that can implement custom immediate mode rendering.
    /// </summary>
    /// <remarks>
    /// To use this element, create a new element inheriting from this type and override the ImmediateRepaint method.
    /// </remarks>
    public abstract class ImmediateModeElement : VisualElement
    {
        static readonly Dictionary<Type, ProfilerMarker> s_Markers = new Dictionary<Type, ProfilerMarker>();
        readonly ProfilerMarker m_ImmediateRepaintMarker;

        // If true, skip callback when outside the viewport
        private bool m_CullingEnabled = false;
        /// <summary>
        /// When this property is set to true, the Element does not repaint itself when it is outside the viewport.
        /// </summary>
        public bool cullingEnabled
        {
            get { return m_CullingEnabled; }
            set { m_CullingEnabled = value; IncrementVersion(VersionChangeType.Repaint); }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ImmediateModeElement()
        {
            generateVisualContent += OnGenerateVisualContent;
            var type = GetType();
            if (!s_Markers.TryGetValue(type, out m_ImmediateRepaintMarker))
            {
                m_ImmediateRepaintMarker = new ProfilerMarker($"{typeName}.{nameof(ImmediateRepaint)}");
                s_Markers[type] = m_ImmediateRepaintMarker;
            }
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            mgc.entryRecorder.DrawImmediate(CallImmediateRepaint, cullingEnabled);
        }

        void CallImmediateRepaint()
        {
            using (m_ImmediateRepaintMarker.Auto())
            {
                ImmediateRepaint();
            }
        }

        /// <summary>
        /// Invoked during the repaint phase.
        /// </summary>
        /// <remarks>
        /// Here it is safe to use any rendering calls using the immediate Graphics api,
        /// eg: Graphics.DrawTexture(contentRect, image); Graphics.DrawMesh, etc
        /// The current transform matrix is set up so (0,0) correspond to the top-left corner of the element.
        /// For IMGUI usage, please use the <see cref="IMGUIContainer"/> element.
        /// </remarks>
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
