// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    [CustomEditor(typeof(BillboardRenderer))]
    [CanEditMultipleObjects]
    internal class BillboardRendererInspector : RendererEditorBase
    {
        private string[] m_ExcludedProperties;

        public override void OnEnable()
        {
            base.OnEnable();
            InitializeProbeFields();

            List<string> excludedProperties = new List<string>();
            excludedProperties.AddRange(new[]
            {
                "m_Materials",
                "m_LightmapParameters"
            });
            excludedProperties.AddRange(Probes.GetFieldsStringArray());
            m_ExcludedProperties = excludedProperties.ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, m_ExcludedProperties);

            m_Probes.OnGUI(targets, (Renderer)target, false);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
