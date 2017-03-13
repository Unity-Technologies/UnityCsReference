// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(RenderSettings))]
    internal class RenderSettingsInspector : Editor
    {
        Editor m_LightingEditor;
        Editor lightingEditor
        {
            get { return m_LightingEditor ?? (m_LightingEditor = Editor.CreateEditor(target, typeof(LightingEditor))); }
        }


        Editor m_FogEditor;
        Editor fogEditor
        {
            get { return m_FogEditor ?? (m_FogEditor = Editor.CreateEditor(target, typeof(FogEditor))); }
        }

        Editor m_OtherRenderingEditor;
        Editor otherRenderingEditor
        {
            get { return m_OtherRenderingEditor ?? (m_OtherRenderingEditor = Editor.CreateEditor(target, typeof(OtherRenderingEditor))); }
        }

        public virtual void OnEnable()
        {
            m_LightingEditor = null;
            m_FogEditor = null;
            m_OtherRenderingEditor = null;
        }

        public override void OnInspectorGUI()
        {
            lightingEditor.OnInspectorGUI();
            fogEditor.OnInspectorGUI();
            otherRenderingEditor.OnInspectorGUI();
        }
    }
}
