// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(SkinnedMeshRenderer))]
    [CanEditMultipleObjects]
    internal class SkinnedMeshRendererEditor : RendererEditorBase
    {
        class Styles
        {
            public static readonly GUIContent legacyClampBlendShapeWeightsInfo = EditorGUIUtility.TrTextContent("Note that BlendShape weight range is clamped. This can be disabled in Player Settings.");
            public static readonly GUIContent meshNotSupportingSkinningInfo = EditorGUIUtility.TrTextContent("The assigned mesh is missing either bone weights with bind pose, or blend shapes. This might cause the mesh not to render in the Player. If your mesh does not have either bone weights with bind pose, or blend shapes, use a Mesh Renderer instead of Skinned Mesh Renderer.");
            public static readonly GUIContent bounds = EditorGUIUtility.TrTextContent("Bounds");
            public static readonly GUIContent quality = EditorGUIUtility.TrTextContent("Quality", "Number of bones to use per vertex during skinning.");
            public static readonly GUIContent updateWhenOffscreen = EditorGUIUtility.TrTextContent("Update When Offscreen", "If an accurate bounding volume representation should be calculated every frame. ");
            public static readonly GUIContent mesh = EditorGUIUtility.TrTextContent("Mesh");
            public static readonly GUIContent rootBone = EditorGUIUtility.TrTextContent("Root Bone");
        }

        private SerializedProperty m_AABB;
        private SerializedProperty m_DirtyAABB;
        private SerializedProperty m_BlendShapeWeights;
        private SerializedProperty m_Quality;
        private SerializedProperty m_UpdateWhenOffscreen;
        private SerializedProperty m_Mesh;
        private SerializedProperty m_RootBone;

        private BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();

        public override void OnEnable()
        {
            base.OnEnable();

            m_AABB = serializedObject.FindProperty("m_AABB");
            m_DirtyAABB = serializedObject.FindProperty("m_DirtyAABB");
            m_BlendShapeWeights = serializedObject.FindProperty("m_BlendShapeWeights");
            m_Quality = serializedObject.FindProperty("m_Quality");
            m_UpdateWhenOffscreen = serializedObject.FindProperty("m_UpdateWhenOffscreen");
            m_Mesh = serializedObject.FindProperty("m_Mesh");
            m_RootBone = serializedObject.FindProperty("m_RootBone");

            m_BoundsHandle.SetColor(Handles.s_BoundingBoxHandleColor);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditMode.DoEditModeInspectorModeButton(
                EditMode.SceneViewEditMode.Collider,
                "Edit Bounds",
                PrimitiveBoundsHandle.editModeButton,
                this
            );
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_AABB, Styles.bounds);
            // If we set m_AABB then we need to set m_DirtyAABB to false
            if (EditorGUI.EndChangeCheck())
                m_DirtyAABB.boolValue = false;

            OnBlendShapeUI();

            EditorGUILayout.PropertyField(m_Quality, Styles.quality);
            EditorGUILayout.PropertyField(m_UpdateWhenOffscreen, Styles.updateWhenOffscreen);

            OnMeshUI();

            EditorGUILayout.PropertyField(m_RootBone, Styles.rootBone);

            DrawMaterials();
            LightingSettingsGUI(false);
            RayTracingSettingsGUI();
            OtherSettingsGUI(false, true);

            serializedObject.ApplyModifiedProperties();
        }

        internal override Bounds GetWorldBoundsOfTarget(Object targetObject)
        {
            return ((SkinnedMeshRenderer)targetObject).bounds;
        }

        public void OnMeshUI()
        {
            SkinnedMeshRenderer renderer = (SkinnedMeshRenderer)target;

            EditorGUILayout.PropertyField(m_Mesh, Styles.mesh);

            if (renderer.sharedMesh != null)
            {
                bool haveClothComponent = renderer.gameObject.GetComponent<Cloth>() != null;

                if (!haveClothComponent && renderer.sharedMesh.blendShapeCount == 0 && (renderer.sharedMesh.boneWeights.Length == 0 || renderer.sharedMesh.bindposes.Length == 0))
                {
                    EditorGUILayout.HelpBox(Styles.meshNotSupportingSkinningInfo.text, MessageType.Error);
                }
            }
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

            if (PlayerSettings.legacyClampBlendShapeWeights)
                EditorGUILayout.HelpBox(Styles.legacyClampBlendShapeWeightsInfo.text, MessageType.Info);

            Mesh m = renderer.sharedMesh;

            int arraySize = m_BlendShapeWeights.arraySize;
            for (int i = 0; i < blendShapeCount; i++)
            {
                content.text = m.GetBlendShapeName(i);

                // Calculate the min and max values for the slider from the frame blendshape weights
                float sliderMin = 0f, sliderMax = 0f;

                int frameCount = m.GetBlendShapeFrameCount(i);
                for (int j = 0; j < frameCount; j++)
                {
                    float frameWeight = m.GetBlendShapeFrameWeight(i, j);
                    sliderMin = Mathf.Min(frameWeight, sliderMin);
                    sliderMax = Mathf.Max(frameWeight, sliderMax);
                }

                // The SkinnedMeshRenderer blendshape weights array size can be out of sync with the size defined in the mesh
                // (default values in that case are 0)
                // The desired behaviour is to resize the blendshape array on edit.

                // Default path when the blend shape array size is big enough.
                if (i < arraySize)
                    EditorGUILayout.Slider(m_BlendShapeWeights.GetArrayElementAtIndex(i), sliderMin, sliderMax, float.MinValue, float.MaxValue, content);
                // Fall back to 0 based editing &
                else
                {
                    EditorGUI.BeginChangeCheck();

                    float value = EditorGUILayout.Slider(content, 0f, sliderMin, sliderMax, float.MinValue, float.MaxValue);
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
            if (!target)
                return;
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
