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
        protected class Styles
        {
            public static readonly GUIContent legacyClampBlendShapeWeightsInfo = L10n.TextContent("Note that BlendShape weight range is clamped. This can be disabled in Player Settings.", null, null, null);
            public static readonly GUIContent meshNotSupportingSkinningInfo = L10n.TextContent("The assigned mesh is missing either bone weights with bind pose, or blend shapes. This might cause the mesh not to render in the Player. If your mesh does not have either bone weights with bind pose, or blend shapes, use a Mesh Renderer instead of Skinned Mesh Renderer.", null, null, null);
            public static readonly GUIContent bounds = L10n.TextContent("Bounds", "The bounding box that encapsulates the mesh.", null, null);
            public static readonly GUIContent quality = L10n.TextContent("Quality", "The maximum number of bones to use per vertex during skinning.", null, null);
            public static readonly GUIContent updateWhenOffscreen = L10n.TextContent("Update When Offscreen", "If true, the bounding box is updated to fit the skinned pose every frame, and the skinned pose is updated even while offscreen. If false, the bounding box remains unchanged, and the skinned pose only updates while visible.", null, null);
            public static readonly GUIContent mesh = L10n.TextContent("Mesh", "The mesh used by this renderer.", null, null);
            public static readonly GUIContent rootBone = L10n.TextContent("Root Bone", "Transform with which the bounds move, and the space in which skinning is computed.", null, null);

            public static readonly GUIContent boundSettings = L10n.TextContent("Bounds", null, null, null);
            public static readonly GUIContent skinningSettings = L10n.TextContent("Skinning", null, null, null);
            public static readonly GUIContent blendShapeSettings = L10n.TextContent("Blend Shapes", null, null, null);
        }

        private SerializedProperty m_AABB;
        private SerializedProperty m_DirtyAABB;
        private SerializedProperty m_BlendShapeWeights;
        private SerializedProperty m_Quality;
        private SerializedProperty m_UpdateWhenOffscreen;
        private SerializedProperty m_Mesh;
        private SerializedProperty m_RootBone;

        private BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();

        private SavedBool m_ShowBoundSettings;
        private SavedBool m_ShowSkinningSettings;
        private SavedBool m_ShowBlendShapeSettings;

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

            m_ShowBoundSettings = new SavedBool($"{target.GetType()}.ShowBoundSettings", true);
            m_ShowSkinningSettings = new SavedBool($"{target.GetType()}.ShowSkinningSettings", true);
            m_ShowBlendShapeSettings = new SavedBool($"{target.GetType()}.ShowBlendShapeSettings", true);
        }

        public void OnDisable()
        {
            EditorApplication.update -= MeshLODUpdate;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            OnMeshUI();
            OnSkinningUI();
            OnBlendShapeUI();
            OnBoundsUI();
            DrawMaterials();
            LightingSettingsGUI(false);
            RayTracingSettingsGUI();
            OnMeshLodUI();
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

        public void OnSkinningUI()
        {
            m_ShowSkinningSettings.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowSkinningSettings.value, Styles.skinningSettings);
            using (new EditorGUI.IndentLevelScope())
            {
                if (m_ShowSkinningSettings.value)
                {
                    EditorGUILayout.PropertyField(m_Quality, Styles.quality);
                    EditorGUILayout.PropertyField(m_RootBone, Styles.rootBone);
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        public void OnBlendShapeUI()
        {
            SkinnedMeshRenderer renderer = (SkinnedMeshRenderer)target;
            int blendShapeCount = renderer.sharedMesh == null ? 0 : renderer.sharedMesh.blendShapeCount;
            if (blendShapeCount == 0)
                return;

            m_ShowBlendShapeSettings.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowBlendShapeSettings.value, Styles.blendShapeSettings);
            using (new EditorGUI.IndentLevelScope())
            {
                if (m_ShowBlendShapeSettings.value)
                {
                    if (PlayerSettings.legacyClampBlendShapeWeights)
                        EditorGUILayout.HelpBox(Styles.legacyClampBlendShapeWeightsInfo.text, MessageType.Info);

                    Mesh m = renderer.sharedMesh;
                    GUIContent content = new GUIContent();

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
                        {
                            EditorGUILayout.Slider(m_BlendShapeWeights.GetArrayElementAtIndex(i), sliderMin, sliderMax, float.MinValue, float.MaxValue, content);
                        }
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
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        public void OnBoundsUI()
        {
            m_ShowBoundSettings.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowBoundSettings.value, Styles.boundSettings);
            using (new EditorGUI.IndentLevelScope())
            {
                if (m_ShowBoundSettings.value)
                {
                    EditorGUILayout.PropertyField(m_UpdateWhenOffscreen, Styles.updateWhenOffscreen);

                    bool isDisabledButton = m_UpdateWhenOffscreen.boolValue || m_UpdateWhenOffscreen.hasMultipleDifferentValues;
                    EditorGUI.BeginDisabled(isDisabledButton);
                    EditMode.DoEditModeInspectorModeButton(
                        EditMode.SceneViewEditMode.Collider,
                        "Edit Bounds",
                        PrimitiveBoundsHandle.editModeButton,
                        this,
                        true
                    );
                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.PropertyField(m_AABB, Styles.bounds);
                    // If we set m_AABB then we need to set m_DirtyAABB to false
                    if (EditorGUI.EndChangeCheck())
                        m_DirtyAABB.boolValue = false;
                    EditorGUI.EndDisabled();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        public void OnMeshLodUI()
        {
            EditorApplication.update -= MeshLODUpdate;
            SkinnedMeshRenderer renderer = (SkinnedMeshRenderer)target;
            if (renderer != null && renderer.sharedMesh != null)
            {
                if (renderer.sharedMesh.isLodSelectionActive)
                {
                    MeshLodSettingsGUI(renderer.sharedMesh.lodCount);
                }
            }
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

            if (renderer.sharedMesh && renderer.sharedMesh.isLodSelectionActive && LODGUI.IsDrawingLabelInCurrentSceneView())
            {
                DrawMeshLODLabel(renderer, renderer.sharedMesh.lodCount);
            }
        }
    }
}
