// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(SkinnedMeshRenderer))]
    [CanEditMultipleObjects]
    internal class SkinnedMeshRendererEditor : RendererEditorBase
    {
        private const string kDisplayLightingKey = "SkinnedMeshRendererEditor.Lighting.ShowSettings";

        private SerializedProperty m_Materials;
        private SerializedProperty m_AABB;
        private SerializedProperty m_DirtyAABB;
        private SerializedProperty m_BlendShapeWeights;
        private LightingSettingsInspector m_Lighting;

        private BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();
        private string[] m_ExcludedProperties;

        public override void OnEnable()
        {
            base.OnEnable();

            m_Materials = serializedObject.FindProperty("m_Materials");
            m_BlendShapeWeights = serializedObject.FindProperty("m_BlendShapeWeights");
            m_AABB = serializedObject.FindProperty("m_AABB");
            m_DirtyAABB = serializedObject.FindProperty("m_DirtyAABB");

            m_BoundsHandle.SetColor(Handles.s_BoundingBoxHandleColor);

            InitializeProbeFields();
            InitializeLightingFields();

            List<string> excludedProperties = new List<string>();
            excludedProperties.AddRange(new[]
            {
                "m_CastShadows",
                "m_ReceiveShadows",
                "m_MotionVectors",
                "m_Materials",
                "m_BlendShapeWeights",
                "m_AABB",
                "m_LightmapParameters",
                "m_DynamicOccludee"
            });
            excludedProperties.AddRange(Probes.GetFieldsStringArray());
            m_ExcludedProperties = excludedProperties.ToArray();
        }

        private void InitializeLightingFields()
        {
            m_Lighting = new LightingSettingsInspector(serializedObject);

            m_Lighting.showSettings = EditorPrefs.GetBool(kDisplayLightingKey, false);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            OnBlendShapeUI();

            DrawPropertiesExcluding(serializedObject, m_ExcludedProperties);

            EditMode.DoEditModeInspectorModeButton(
                EditMode.SceneViewEditMode.Collider,
                "Edit Bounds",
                PrimitiveBoundsHandle.editModeButton,
                this
                );
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_AABB, new GUIContent("Bounds"));
            // If we set m_AABB then we need to set m_DirtyAABB to false
            if (EditorGUI.EndChangeCheck())
                m_DirtyAABB.boolValue = false;

            LightingFieldsGUI();

            EditorGUILayout.PropertyField(m_Materials, true);

            CullDynamicFieldGUI();

            serializedObject.ApplyModifiedProperties();
        }

        internal override Bounds GetWorldBoundsOfTarget(Object targetObject)
        {
            return ((SkinnedMeshRenderer)targetObject).bounds;
        }

        private void LightingFieldsGUI()
        {
            bool oldShowLighting = m_Lighting.showSettings;

            if (m_Lighting.Begin())
            {
                RenderProbeFields();
                m_Lighting.RenderMeshSettings(false);
            }
            m_Lighting.End();

            if (m_Lighting.showSettings != oldShowLighting)
                EditorPrefs.SetBool(kDisplayLightingKey, m_Lighting.showSettings);
        }

        public void OnBlendShapeUI()
        {
            SkinnedMeshRenderer renderer = (SkinnedMeshRenderer)target;
            int blendShapeCount = renderer.sharedMesh == null ? 0 : renderer.sharedMesh.blendShapeCount;
            if (blendShapeCount == 0)
                return;

            GUIContent content = new GUIContent();
            content.text = "BlendShapes";

            EditorGUILayout.PropertyField(m_BlendShapeWeights, content, false);
            if (!m_BlendShapeWeights.isExpanded)
                return;

            EditorGUI.indentLevel++;
            Mesh m = renderer.sharedMesh;

            int arraySize = m_BlendShapeWeights.arraySize;
            for (int i = 0; i < blendShapeCount; i++)
            {
                content.text = m.GetBlendShapeName(i);

                // The SkinnedMeshRenderer blendshape weights array size can be out of sync with the size defined in the mesh
                // (default values in that case are 0)
                // The desired behaviour is to resize the blendshape array on edit.

                // Default path when the blend shape array size is big enough.
                if (i < arraySize)
                    EditorGUILayout.PropertyField(m_BlendShapeWeights.GetArrayElementAtIndex(i), content);
                // Fall back to 0 based editing &
                else
                {
                    EditorGUI.BeginChangeCheck();

                    float value = EditorGUILayout.FloatField(content, 0.0F);
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_BlendShapeWeights.arraySize = blendShapeCount;
                        arraySize = blendShapeCount;
                        m_BlendShapeWeights.GetArrayElementAtIndex(i).floatValue = value;
                    }
                }
            }

            EditorGUI.indentLevel--;
        }

        public void OnSceneGUI()
        {
            SkinnedMeshRenderer renderer = (SkinnedMeshRenderer)target;

            if (renderer.updateWhenOffscreen)
            {
                Bounds bounds = renderer.bounds;
                Vector3 center = bounds.center;
                Vector3 size = bounds.size;

                Handles.DrawWireCube(center, size);
            }
            else
            {
                using (new Handles.DrawingScope(renderer.actualRootBone.localToWorldMatrix))
                {
                    Bounds bounds = renderer.localBounds;
                    m_BoundsHandle.center = bounds.center;
                    m_BoundsHandle.size = bounds.size;

                    // only display interactive handles if edit mode is active
                    m_BoundsHandle.handleColor = EditMode.editMode == EditMode.SceneViewEditMode.Collider && EditMode.IsOwner(this) ?
                        m_BoundsHandle.wireframeColor : Color.clear;

                    EditorGUI.BeginChangeCheck();
                    m_BoundsHandle.DrawHandle();
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(renderer, "Resize Bounds");
                        renderer.localBounds = new Bounds(m_BoundsHandle.center, m_BoundsHandle.size);
                    }
                }
            }
        }
    }
}
