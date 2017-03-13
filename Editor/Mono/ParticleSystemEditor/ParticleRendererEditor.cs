// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditorInternal;

#pragma warning disable 612, 618

namespace UnityEditor
{
    [CustomEditor(typeof(ParticleRenderer))]
    [CanEditMultipleObjects]
    internal class ParticleRendererEditor : RendererEditorBase
    {
        public override void OnEnable()
        {
            base.OnEnable();

            InitializeProbeFields();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, Probes.GetFieldsStringArray());

            m_Probes.OnGUI(targets, (Renderer)target, false);

            serializedObject.ApplyModifiedProperties();
        }
    }
}

#pragma warning restore 612, 618
