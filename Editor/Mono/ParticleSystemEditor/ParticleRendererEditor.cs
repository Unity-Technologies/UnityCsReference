// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

#pragma warning disable 612, 618

namespace UnityEditor
{
    [CustomEditor(typeof(ParticleRenderer))]
    [CanEditMultipleObjects]
    internal class ParticleRendererEditor : RendererEditorBase
    {
        private string[] m_ExcludedProperties;

        public override void OnEnable()
        {
            base.OnEnable();

            InitializeProbeFields();

            List<string> excludedProperties = new List<string>();
            if (!SupportedRenderingFeatures.active.rendererSupportsMotionVectors)
                excludedProperties.Add("m_MotionVectors");
            if (!SupportedRenderingFeatures.active.rendererSupportsReceiveShadows)
                excludedProperties.Add("m_ReceiveShadows");
            excludedProperties.AddRange(Probes.GetFieldsStringArray());

            m_ExcludedProperties = excludedProperties.ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, m_ExcludedProperties);

            m_Probes.OnGUI(targets, (Renderer)target, false);

            RenderRenderingLayer();

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#pragma warning restore 612, 618
