// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Presets;

namespace UnityEditor
{
    [CustomEditor(typeof(LODGroup))]
    [CanEditMultipleObjects]
    internal class LODGroupEditor : Editor
    {
        private int m_SelectedLODSlider = -1;
        private int m_SelectedLOD = -1;
        private int m_NumberOfLODs;

        private LODGroup m_LODGroup;
        private bool m_IsPrefab;
        private bool m_IsPreset;

        private int m_SelectedLODGroupCount;
        private int m_MaxLODCountForMultiselection;

        private SerializedProperty m_FadeMode;
        private SerializedProperty m_AnimateCrossFading;
        private SerializedProperty m_LastLODIsBillboard;
        private SerializedProperty m_LODs;
        private SerializedProperty m_LODSize;

        private AnimBool m_ShowAnimateCrossFading = new AnimBool();
        private AnimBool m_ShowFadeTransitionWidth = new AnimBool();

        private Mesh m_BillboardPreviewMesh = null;
        private MaterialPropertyBlock m_BillboardPreviewProperties = null;

        private int[][] m_PrimitiveCounts;
        private int[] m_SubmeshCounts;
        private Texture2D[] m_Textures;

        private GUIContent m_PrimitiveCountLabel;
        private SavedBool[] m_LODGroupFoldoutHeaderValues = null;

        private ReorderableList[] m_RendererMeshLists;
        private int[] m_ReoderableMeshListCounts;
        private int m_ReorderableListIndex = 0;

        private Transform m_TargetTransform;

        void InitAndSetFoldoutLabelTextures()
        {
            m_Textures = new Texture2D[m_LODs.arraySize];
            for (int i = 0; i < m_Textures.Length; i++)
            {
                m_Textures[i] = new Texture2D(1, 1);
                m_Textures[i].SetPixel(0, 0, LODGroupGUI.kLODColors[i]);
            }
        }

        void OnUndoRedoPerformed(in UndoRedoInfo info)
        {
            if (target == null || serializedObject == null)
                return;

            serializedObject.Update();
            m_LODs = serializedObject.FindProperty("m_LODs");
            m_LODSize = m_LODs.serializedObject.FindProperty("m_Size");
            m_LODGroup = (LODGroup)target;

            ResetValuesAfterLODObjectIsModified();
            InitAndSetFoldoutLabelTextures();
            UpdateRendererMeshListCounts();
        }

        void OnEnable()
        {
            m_FadeMode = serializedObject.FindProperty("m_FadeMode");
            m_AnimateCrossFading = serializedObject.FindProperty("m_AnimateCrossFading");
            m_LastLODIsBillboard = serializedObject.FindProperty("m_LastLODIsBillboard");
            m_LODs = serializedObject.FindProperty("m_LODs");
            m_LODSize = m_LODs.serializedObject.FindProperty("m_Size");

            m_ShowAnimateCrossFading.value = m_FadeMode.intValue != (int)LODFadeMode.None;
            m_ShowAnimateCrossFading.valueChanged.AddListener(Repaint);
            m_ShowFadeTransitionWidth.value = false;
            m_ShowFadeTransitionWidth.valueChanged.AddListener(Repaint);

            EditorApplication.update += Update;

            m_LODGroup = (LODGroup)target;
            m_SelectedLODGroupCount = targets.Length;
            m_MaxLODCountForMultiselection = GetMaxLODCountForMultiSelection();

            // Calculate if the newly selected LOD group is a prefab... they require special handling
            m_IsPrefab = PrefabUtility.IsPartOfPrefabAsset(m_LODGroup.gameObject);

            // this is used to disable the Renderers section if we are viewing a Preset inspector
            m_IsPreset = Preset.IsEditorTargetAPreset(serializedObject.targetObject);

            CalculatePrimitiveCountForRenderers();
            m_PrimitiveCountLabel = LODGroupGUI.GUIStyles.m_TriangleCountLabel;
            ResetFoldoutLists();
            UpdateRendererMeshListCounts();

            m_TargetTransform = (target as LODGroup)?.gameObject?.transform;

            Undo.undoRedoEvent += OnUndoRedoPerformed;
            Repaint();
        }

        void UpdateRendererMeshListCounts()
        {
            m_ReoderableMeshListCounts = m_RendererMeshLists.Select(i => i.count).ToArray();
        }

        protected virtual void DrawLODRendererMeshListItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            Rect objectFieldRect = new Rect(rect.x, rect.y, rect.width * 0.6f, EditorGUI.kSingleLineHeight);
            rect.height = EditorGUI.kSingleLineHeight;

            if (m_RendererMeshLists.Length != m_ReoderableMeshListCounts.Length || m_RendererMeshLists[m_ReorderableListIndex].count != m_ReoderableMeshListCounts[m_ReorderableListIndex])
            {
                CalculatePrimitiveCountForRenderers();
                UpdateRendererMeshListCounts();
            }

            var prop = m_RendererMeshLists[m_ReorderableListIndex].serializedProperty.GetArrayElementAtIndex(index).FindPropertyRelative("renderer");

            EditorGUI.ObjectField(objectFieldRect, prop, typeof(Renderer), GUIContent.none);

            string labelText = $"{m_PrimitiveCounts[m_ReorderableListIndex][index]} Tris.";
            var size = EditorStyles.label.CalcSize(new GUIContent(labelText));
            Rect triCountLabelRect = new Rect(rect.x + objectFieldRect.width + 10, rect.y, size.x, EditorGUI.kSingleLineHeight);
            GUI.Label(triCountLabelRect, labelText);

            var subMeshCount = "0";
            var renderer = prop.objectReferenceValue as Renderer;
            if (renderer != null)
            {
                MeshFilter meshFilter;
                if (renderer.TryGetComponent(out meshFilter) && meshFilter.sharedMesh != null)
                    subMeshCount = meshFilter.sharedMesh.subMeshCount.ToString();
            }

            labelText = $"{subMeshCount} Sub Mesh(es).";
            size = EditorStyles.label.CalcSize(new GUIContent(labelText));
            triCountLabelRect.x += triCountLabelRect.width;
            triCountLabelRect.width = size.x;
            GUI.Label(triCountLabelRect, labelText);
        }

        protected virtual void DrawLODRendererMeshListHeader(Rect rect)
        {
            rect.height = EditorGUI.kSingleLineHeight;
            GUI.Label(rect, LODGroupGUI.Styles.m_RendersTitle);
        }

        protected virtual void RemoveLODRendererMeshFromList(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            CalculatePrimitiveCountForRenderers();
        }

        protected virtual void AddLODRendererMeshToList(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoAddButton(list);
            CalculatePrimitiveCountForRenderers();
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;
            Undo.undoRedoEvent -= OnUndoRedoPerformed;

            m_ShowAnimateCrossFading.valueChanged.RemoveListener(Repaint);
            m_ShowFadeTransitionWidth.valueChanged.RemoveListener(Repaint);
            if (m_PreviewUtility != null)
                m_PreviewUtility.Cleanup();

            DestroyImmediate(m_BillboardPreviewMesh, true);
            m_BillboardPreviewProperties = null;
        }

        // Find the given sceen space rectangular bounds from a list of vector 3 points.
        private static Rect CalculateScreenRect(IEnumerable<Vector3> points)
        {
            var points2 = points.Select(p => HandleUtility.WorldToGUIPoint(p)).ToList();

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);

            foreach (var point in points2)
            {
                min.x = (point.x < min.x) ? point.x : min.x;
                max.x = (point.x > max.x) ? point.x : max.x;

                min.y = (point.y < min.y) ? point.y : min.y;
                max.y = (point.y > max.y) ? point.y : max.y;
            }

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }

        public static bool IsSceneGUIEnabled()
        {
            if (Event.current.type != EventType.Repaint
                || Camera.current == null
                || SceneView.lastActiveSceneView != SceneView.currentDrawingSceneView)
            {
                return false;
            }

            return true;
        }

        public void OnSceneGUI()
        {
            if (!target)
                return;

            if (m_SelectedLODGroupCount > 1)
                return;

            LODGroup lodGroup = (LODGroup)target;
            Camera camera = SceneView.lastActiveSceneView.camera;
            var worldReferencePoint = LODUtility.CalculateWorldReferencePoint(lodGroup);

            if (Vector3.Dot(camera.transform.forward,
                (camera.transform.position - worldReferencePoint).normalized) > 0)
                return;

            var info = LODUtility.CalculateVisualizationData(camera, lodGroup, -1);
            float size = info.worldSpaceSize;

            // Draw cap around LOD to visualize it's size
            Handles.color = info.activeLODLevel != -1 ? LODGroupGUI.kLODColors[info.activeLODLevel] : LODGroupGUI.kCulledLODColor;

            Handles.SelectionFrame(0, worldReferencePoint, camera.transform.rotation, size / 2);

            // Calculate a screen rect for the on scene title
            Vector3 sideways = camera.transform.right * size / 2.0f;
            Vector3 up = camera.transform.up * size / 2.0f;
            var rect = CalculateScreenRect(
                new[]
                {
                    worldReferencePoint - sideways + up,
                    worldReferencePoint - sideways - up,
                    worldReferencePoint + sideways + up,
                    worldReferencePoint + sideways - up
                });

            // Place the screen space lable directaly under the
            var midPoint = rect.x + rect.width / 2.0f;
            rect = new Rect(midPoint - LODGroupGUI.kSceneLabelHalfWidth, rect.yMax, LODGroupGUI.kSceneLabelHalfWidth * 2, LODGroupGUI.kSceneLabelHeight);

            if (rect.yMax > Screen.height - LODGroupGUI.kSceneLabelHeight)
                rect.y = Screen.height - LODGroupGUI.kSceneLabelHeight - LODGroupGUI.kSceneHeaderOffset;

            Handles.BeginGUI();
            GUI.Label(rect, GUIContent.none, EditorStyles.notificationBackground);
            EditorGUI.DoDropShadowLabel(rect, GUIContent.Temp(info.activeLODLevel >= 0 ? "LOD " + info.activeLODLevel : "Culled"), LODGroupGUI.Styles.m_LODLevelNotifyText, 0.3f);
            Handles.EndGUI();
        }

        private Vector3 m_LastCameraPos = Vector3.zero;
        public void Update()
        {
            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null)
            {
                return;
            }

            // Update the last camera positon and repaint if the camera has moved
            if (SceneView.lastActiveSceneView.camera.transform.position != m_LastCameraPos)
            {
                m_LastCameraPos = SceneView.lastActiveSceneView.camera.transform.position;
                Repaint();
            }
        }

        private const string kLODDataPath = "m_LODs.Array.data[{0}]";
        private const string kPixelHeightDataPath = "m_LODs.Array.data[{0}].screenRelativeHeight";
        private const string kRenderRootPath = "m_LODs.Array.data[{0}].renderers";
        private const string kFadeTransitionWidthDataPath = "m_LODs.Array.data[{0}].fadeTransitionWidth";

        private int activeLOD
        {
            get {return m_SelectedLOD; }
        }

        private ModelImporter GetImporter()
        {
            return AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(target))) as ModelImporter;
        }

        private bool IsLODUsingCrossFadeWidth(int lod)
        {
            if (m_FadeMode.intValue == (int)LODFadeMode.None || m_AnimateCrossFading.boolValue)
                return false;
            if (m_FadeMode.intValue == (int)LODFadeMode.CrossFade)
                return true;
            // SpeedTree: only last mesh LOD and billboard LOD do crossfade
            if (m_NumberOfLODs > 0 && m_SelectedLOD == m_NumberOfLODs - 1)
                return true;
            if (m_NumberOfLODs > 1 && m_SelectedLOD == m_NumberOfLODs - 2)
            {
                // the second last LOD uses cross-fade if the last LOD is a billboard
                var renderers = serializedObject.FindProperty(String.Format(kRenderRootPath, m_NumberOfLODs - 1));
                if (renderers.arraySize != 1)
                    return false;
                var renderer = renderers.GetArrayElementAtIndex(0).FindPropertyRelative("renderer").objectReferenceValue;
                if (renderer is BillboardRenderer || (renderer is MeshRenderer && m_LastLODIsBillboard.boolValue))
                    return true;
            }
            return false;
        }

        int GetMaxLODCountForMultiSelection()
        {
            var maxLODIndex = m_LODs.arraySize;

            foreach (UnityEngine.Object targetObject in serializedObject.targetObjects)
            {
                SerializedObject targetObjectSerialized = new SerializedObject(targetObject);
                SerializedProperty property = targetObjectSerialized.FindProperty(m_LODs.propertyPath);

                maxLODIndex = Mathf.Min(property.arraySize, maxLODIndex);
            }
            return maxLODIndex;
        }

        void DrawLODGroupFoldouts()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(m_LODSize, LODGroupGUI.Styles.m_LODObjectSizeLabel);

            if (GUILayout.Button(LODGroupGUI.Styles.m_ResetObjectSizeLabel))
            {
                float originalSize = m_LODSize.floatValue;
                m_LODSize.floatValue = 1f;
                for (int i = 0; i < m_LODs.arraySize; i++)
                {
                    var heightPercentageProperty = serializedObject.FindProperty(string.Format(kPixelHeightDataPath, i));
                    heightPercentageProperty.floatValue = heightPercentageProperty.floatValue / originalSize;
                }
            }
            EditorGUILayout.EndHorizontal();

            Camera camera = null;
            if (SceneView.lastActiveSceneView && SceneView.lastActiveSceneView.camera)
                camera = SceneView.lastActiveSceneView.camera;

            if (camera == null)
                return;

            for (int i = 0; i < m_MaxLODCountForMultiselection; i++)
            {
                if (targets.Length == 1)
                    DrawLODGroupFoldout(camera, i, ref m_LODGroupFoldoutHeaderValues[i]);
                else
                    DrawLODTransitionPropertyField(camera, i, m_MaxLODCountForMultiselection);
            }
        }

        void DrawLODGroupFoldout(Camera camera, int lodGroupIndex, ref SavedBool foldoutState)
        {
            var totalTriCount = m_PrimitiveCounts.Length > 0 ? m_PrimitiveCounts[lodGroupIndex].Sum() : 0;
            var lod0TriCount = m_PrimitiveCounts[0].Sum();
            var triCountChange = lod0TriCount != 0 ? (float)totalTriCount / lod0TriCount * 100 : 0;
            var triangleChangeLabel = lodGroupIndex > 0 && lod0TriCount != 0 ? $"({triCountChange.ToString("f2")}% LOD0)" : "";

            var wideInspector = Screen.width >= 350;
            triangleChangeLabel = wideInspector ? triangleChangeLabel : "";
            var submeshCountLabel = wideInspector ? $"- {m_SubmeshCounts[lodGroupIndex]} Sub Mesh(es)" : "";

            if (activeLOD == lodGroupIndex)
                LODGroupGUI.DrawRoundedBoxAroundLODDFoldout(lodGroupIndex, activeLOD);
            else
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            foldoutState.value = LODGroupGUI.FoldoutHeaderGroupInternal(GUILayoutUtility.GetRect(GUIContent.none, LODGroupGUI.GUIStyles.m_InspectorTitlebarFlat), foldoutState.value, $"LOD {lodGroupIndex}", m_Textures[lodGroupIndex],
                LODGroupGUI.kLODColors[lodGroupIndex] * 0.6f, $"{totalTriCount} {m_PrimitiveCountLabel.text} {triangleChangeLabel} {submeshCountLabel}");

            if (foldoutState.value)
            {
                DrawLODTransitionPropertyField(camera, lodGroupIndex, m_LODs.arraySize);

                EditorGUILayout.BeginHorizontal();

                m_ShowFadeTransitionWidth.target = IsLODUsingCrossFadeWidth(lodGroupIndex);
                if (EditorGUILayout.BeginFadeGroup(m_ShowFadeTransitionWidth.faded))
                {
                    EditorGUILayout.Slider(serializedObject.FindProperty(string.Format(kFadeTransitionWidthDataPath, lodGroupIndex)), 0, 1);
                }
                EditorGUILayout.EndFadeGroup();
                EditorGUILayout.EndHorizontal();

                m_ReorderableListIndex = lodGroupIndex;

                EditorGUI.BeginChangeCheck();

                m_RendererMeshLists[lodGroupIndex].DoLayoutList();

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    ResetValuesAfterLODObjectIsModified();
                }
            }

            if (activeLOD == lodGroupIndex)
                GUILayoutUtility.EndLayoutGroup();
            else
                EditorGUILayout.EndVertical();
        }

        void DrawLODTransitionPropertyField(Camera camera, int lodGroupIndex, int maxLOD)
        {
            EditorGUILayout.BeginHorizontal();

            var heightPercentageProperty = serializedObject.FindProperty(string.Format(kPixelHeightDataPath, lodGroupIndex));

            EditorGUI.showMixedValue = heightPercentageProperty.hasMultipleDifferentValues;

            EditorGUI.BeginChangeCheck();
            float newVal = 0;
            if (targets.Length == 1)
                newVal = EditorGUILayout.FloatField(LODGroupGUI.Styles.m_LODTransitionPercentageLabel, heightPercentageProperty.floatValue * 100);
            else
            {
                GUIContent label = new GUIContent($"LOD {lodGroupIndex} Transition", LODGroupGUI.Styles.m_LODTransitionPercentageLabel.tooltip);
                newVal = EditorGUILayout.FloatField(label, heightPercentageProperty.floatValue * 100);
            }

            if (EditorGUI.EndChangeCheck())
            {
                float minVal = 0, maxVal = 100f;
                if (lodGroupIndex < maxLOD - 1)
                {
                    var nextTransitionProperty = serializedObject.FindProperty(string.Format(kPixelHeightDataPath, lodGroupIndex + 1));
                    minVal = nextTransitionProperty.floatValue * 100;
                }
                if (lodGroupIndex > 0)
                {
                    var previousTransitionProperty = serializedObject.FindProperty(string.Format(kPixelHeightDataPath, lodGroupIndex - 1));
                    maxVal = previousTransitionProperty.floatValue * 100;
                }

                float addToMinVal = lodGroupIndex == maxLOD - 1 && targets.Length == 1 ? 0f : 0.1f;

                if (newVal > maxVal)
                    newVal = maxVal - 0.1f;
                else if (newVal < minVal)
                    newVal = minVal + addToMinVal;

                heightPercentageProperty.floatValue = newVal / 100;
            }

            EditorGUI.showMixedValue = false;

            EditorGUI.BeginDisabledGroup(!IsObjectVisibleToCamera(camera));
            if (GUILayout.Button(LODGroupGUI.Styles.m_LODSetToCameraLabel, GUILayout.Width(95)))
            {
                heightPercentageProperty.floatValue = m_LODGroup.GetRelativeHeight(camera);
            }
            EditorGUI.EndDisabledGroup();

            if (Screen.width > 380)
            {
                var distanceLabel = Selection.count == 1 && newVal > 0 && camera != null ? LODGroupExtensions.RelativeHeightToDistance(camera, heightPercentageProperty.floatValue, m_LODSize.floatValue).ToString("F2") + " m" : "-";
                LODGroupGUI.GUIStyles.m_DistanceInMetersLabel.text = distanceLabel;
                GUILayout.Label(LODGroupGUI.GUIStyles.m_DistanceInMetersLabel, GUILayout.Width(60));
            }
            EditorGUILayout.EndHorizontal();
        }

        bool IsObjectVisibleToCamera(Camera camera)
        {
            if (camera == null)
                return false;

            if (targets.Length == 1 && m_TargetTransform != null)
            {
                Vector3 pointOnScreen = camera.WorldToViewportPoint(m_TargetTransform.position);
                bool isVisible = pointOnScreen.z > 0 && pointOnScreen.x > 0 && pointOnScreen.x < 1 && pointOnScreen.y > 0 && pointOnScreen.y < 1;
                return isVisible;
            }
            return false;
        }

        public static Mesh GetMeshFromRendererIfAvailable(Renderer renderer)
        {
            if (renderer == null)
                return null;

            Mesh rendererMesh = null;
            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter != null)
                rendererMesh = meshFilter.sharedMesh;
            else
            {
                var skinnedRenderer = renderer as SkinnedMeshRenderer;
                if (skinnedRenderer != null)
                    rendererMesh = skinnedRenderer.sharedMesh;
            }

            return rendererMesh;
        }

        void CalculatePrimitiveCountForRenderers()
        {
            var lods = m_LODGroup.GetLODs();

            m_PrimitiveCountLabel = LODGroupGUI.GUIStyles.m_TriangleCountLabel;
            m_PrimitiveCounts = new int[lods.Length][];
            m_SubmeshCounts = new int[lods.Length];

            for (int i = 0; i < lods.Length; i++)
            {
                var renderers = lods[i].renderers;
                m_PrimitiveCounts[i] = new int[renderers.Length];

                for (int j = 0; j < renderers.Length; j++)
                {
                    var hasMismatchingSubMeshTopologyTypes = CheckIfMeshesHaveMatchingTopologyTypes(renderers);

                    var rendererMesh = GetMeshFromRendererIfAvailable(renderers[j]);
                    if (rendererMesh == null)
                        continue;

                    m_SubmeshCounts[i] += rendererMesh.subMeshCount;

                    if (hasMismatchingSubMeshTopologyTypes)
                    {
                        m_PrimitiveCounts[i][j] = rendererMesh.vertexCount;
                        m_PrimitiveCountLabel = LODGroupGUI.GUIStyles.m_VertexCountLabel;
                    }
                    else
                    {
                        for (int subMeshIndex = 0; subMeshIndex < rendererMesh.subMeshCount; subMeshIndex++)
                        {
                            m_PrimitiveCounts[i][j] += (int)rendererMesh.GetIndexCount(subMeshIndex) / 3;
                        }
                    }
                }
            }
        }

        void ResetMembersForEachOpenInspector()
        {
            serializedObject.Update();
            m_LODs = serializedObject.FindProperty("m_LODs");
            m_MaxLODCountForMultiselection = GetMaxLODCountForMultiSelection();

            CalculatePrimitiveCountForRenderers();
            ResetFoldoutLists();
        }

        void ResetValuesAfterLODObjectIsModified()
        {
            // A safeguard for when several inspectors of the same type are open for the same object
            UnityEngine.Object[] openLODInspectors = Resources.FindObjectsOfTypeAll(typeof(LODGroupEditor));
            Array.ForEach(openLODInspectors, el => (el as LODGroupEditor).ResetMembersForEachOpenInspector());
        }

        void ExpandSelectedHeaderAndCloseRemaining(int index)
        {
            // need this to safeguard against drag & drop on Culled section
            // as that sets the LOD index to 8 which is outside of the total
            // allowed LOD range
            if (index >= m_LODs.arraySize)
                return;

            Array.ForEach(m_LODGroupFoldoutHeaderValues, el => el.value = false);
            m_LODGroupFoldoutHeaderValues[index].value = true;
        }

        void ResetFoldoutLists()
        {
            m_RendererMeshLists = new ReorderableList[m_LODs.arraySize];
            m_LODGroupFoldoutHeaderValues = new SavedBool[m_LODs.arraySize];

            for (int i = 0; i < m_RendererMeshLists.Length; i++)
            {
                m_LODGroupFoldoutHeaderValues[i] = new SavedBool($"{target.GetType()}.lodFoldout{i}", false);

                var renderersProperty = serializedObject.FindProperty(string.Format(kRenderRootPath, i));
                m_RendererMeshLists[i] = new ReorderableList(serializedObject, renderersProperty);
                m_RendererMeshLists[i].drawElementCallback = DrawLODRendererMeshListItems;
                m_RendererMeshLists[i].drawHeaderCallback = DrawLODRendererMeshListHeader;
                m_RendererMeshLists[i].onRemoveCallback = RemoveLODRendererMeshFromList;
                m_RendererMeshLists[i].onAddCallback = AddLODRendererMeshToList;
                m_RendererMeshLists[i].draggable = false;
            }

            InitAndSetFoldoutLabelTextures();
        }

        public static bool CheckIfSubmeshesHaveMatchingTopologyTypes(Mesh mesh)
        {
            var meshTopology = mesh.GetTopology(0);

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                var newTopology = mesh.GetTopology(i);
                if (meshTopology != newTopology)
                    return true;
            }

            return false;
        }

        public static bool CheckIfMeshesHaveMatchingTopologyTypes(Renderer[] renderers)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Mesh mesh = GetMeshFromRendererIfAvailable(renderers[i]);

                if (mesh == null)
                    return false;

                if (CheckIfSubmeshesHaveMatchingTopologyTypes(mesh))
                    return true;
            }

            return false;
        }

        public override void OnInspectorGUI()
        {
            var initiallyEnabled = GUI.enabled;

            // Grab the latest data from the object
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_FadeMode);

            m_ShowAnimateCrossFading.target = m_FadeMode.intValue != (int)LODFadeMode.None;
            if (EditorGUILayout.BeginFadeGroup(m_ShowAnimateCrossFading.faded))
                EditorGUILayout.PropertyField(m_AnimateCrossFading);
            EditorGUILayout.EndFadeGroup();

            m_NumberOfLODs = m_LODs.arraySize;

            // This could happen when you select a newly inserted LOD level and then undo the insertion.
            // It's valid for m_SelectedLOD to become -1, which means nothing is selected.
            if (m_SelectedLOD >= m_NumberOfLODs)
            {
                m_SelectedLOD = m_NumberOfLODs - 1;
            }

            if (targets.Length > 1)
            {
                DrawLODGroupFoldouts();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // Add some space at the top..
            GUILayout.Space(LODGroupGUI.kSliderBarTopMargin);

            // Precalculate and cache the slider bar position for this update
            var sliderBarPosition = GUILayoutUtility.GetRect(0, LODGroupGUI.kSliderBarHeight, GUILayout.ExpandWidth(true));

            // Precalculate the lod info (button locations / ranges ect)
            var lods = LODGroupGUI.CreateLODInfos(m_NumberOfLODs, sliderBarPosition,
                i => String.Format("LOD {0}", i),
                i => serializedObject.FindProperty(string.Format(kPixelHeightDataPath, i)).floatValue);

            DrawLODLevelSlider(sliderBarPosition, lods);
            GUILayout.Space(LODGroupGUI.kSliderBarBottomMargin);

            if (QualitySettings.lodBias != 1.0f)
                EditorGUILayout.HelpBox(string.Format("Active LOD bias is {0:0.0#}. Distances are adjusted accordingly.", QualitySettings.lodBias), MessageType.Warning);

            // Draw the info for the selected LOD
            if (m_NumberOfLODs > 0 && activeLOD >= 0 && activeLOD < m_NumberOfLODs && !m_IsPreset)
            {
                DrawRenderersInfo(EditorGUIUtility.contextWidth);
            }

            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            bool needUpdateBounds = LODUtility.NeedUpdateLODGroupBoundingBox(m_LODGroup);
            using (new EditorGUI.DisabledScope(!needUpdateBounds))
            {
                if (GUILayout.Button(needUpdateBounds ? LODGroupGUI.Styles.m_RecalculateBounds : LODGroupGUI.Styles.m_RecalculateBoundsDisabled, GUILayout.ExpandWidth(false)))
                {
                    Undo.RecordObject(m_LODGroup, "Recalculate LODGroup Bounds");
                    m_LODGroup.RecalculateBounds();
                }
            }

            if (GUILayout.Button(LODGroupGUI.Styles.m_LightmapScale, GUILayout.ExpandWidth(false)))
                SendPercentagesToLightmapScale();

            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            DrawLODGroupFoldouts();

            var importer = PrefabUtility.IsPartOfModelPrefab(target) ? GetImporter() : null;
            if (importer != null)
            {
                using (var importerRef = new SerializedObject(importer))
                {
                    var importerLODLevels = importerRef.FindProperty("m_LODScreenPercentages");
                    var lodNumberOnImporterMatches = importerLODLevels.isArray && importerLODLevels.arraySize == lods.Count;

                    var guiState = GUI.enabled;
                    if (!lodNumberOnImporterMatches)
                        GUI.enabled = false;

                    if (GUILayout.Button(lodNumberOnImporterMatches ? LODGroupGUI.Styles.m_UploadToImporter : LODGroupGUI.Styles.m_UploadToImporterDisabled))
                    {
                        // Number of imported LOD's is the same as in the imported model
                        for (var i = 0; i < importerLODLevels.arraySize; i++)
                            importerLODLevels.GetArrayElementAtIndex(i).floatValue = lods[i].RawScreenPercent;

                        importerRef.ApplyModifiedProperties();

                        AssetDatabase.ImportAsset(importer.assetPath);
                    }
                    GUI.enabled = guiState;
                }
            }

            if (targets.Length == 1)
                SpeedTreeMaterialFixer.DoFixerUI((target as LODGroup).gameObject);

            // Apply the property, handle undo
            serializedObject.ApplyModifiedProperties();

            GUI.enabled = initiallyEnabled;
        }

        // Draw the renderers for the current LOD group
        // Arrange in a grid
        private void DrawRenderersInfo(float availableWidth)
        {
            var horizontalCount = Mathf.Max(Mathf.FloorToInt(availableWidth / LODGroupGUI.kRenderersButtonHeight), 1);
            var titleArea = GUILayoutUtility.GetRect(LODGroupGUI.Styles.m_RendersTitle, LODGroupGUI.Styles.m_LODSliderTextSelected);
            if (Event.current.type == EventType.Repaint)
                EditorStyles.label.Draw(titleArea, LODGroupGUI.Styles.m_RendersTitle, false, false, false, false);

            // Draw renderer info
            var renderersProperty = serializedObject.FindProperty(string.Format(kRenderRootPath, activeLOD));

            var numberOfButtons = renderersProperty.arraySize + 1;
            var numberOfRows = Mathf.CeilToInt(numberOfButtons / (float)horizontalCount);

            var drawArea = GUILayoutUtility.GetRect(0, numberOfRows * LODGroupGUI.kRenderersButtonHeight, GUILayout.ExpandWidth(true));
            var rendererArea = drawArea;
            GUI.Box(drawArea, GUIContent.none);
            rendererArea.width -= 2 * LODGroupGUI.kRenderAreaForegroundPadding;
            rendererArea.x += LODGroupGUI.kRenderAreaForegroundPadding;

            var buttonWidth = rendererArea.width / horizontalCount;

            var buttons = new List<Rect>();

            for (int i = 0; i < numberOfRows; i++)
            {
                for (int k = 0; k < horizontalCount && (i * horizontalCount + k) < renderersProperty.arraySize; k++)
                {
                    var drawPos = new Rect(
                        LODGroupGUI.kButtonPadding + rendererArea.x + k * buttonWidth,
                        LODGroupGUI.kButtonPadding + rendererArea.y + i * LODGroupGUI.kRenderersButtonHeight,
                        buttonWidth - LODGroupGUI.kButtonPadding * 2,
                        LODGroupGUI.kRenderersButtonHeight - LODGroupGUI.kButtonPadding * 2);
                    buttons.Add(drawPos);
                    DrawRendererButton(drawPos, i * horizontalCount + k);
                }
            }

            if (m_IsPrefab)
                return;

            //+ button
            int horizontalPos = (numberOfButtons - 1) % horizontalCount;
            int verticalPos = numberOfRows - 1;
            HandleAddRenderer(new Rect(
                LODGroupGUI.kButtonPadding + rendererArea.x + horizontalPos * buttonWidth,
                LODGroupGUI.kButtonPadding + rendererArea.y + verticalPos * LODGroupGUI.kRenderersButtonHeight,
                buttonWidth - LODGroupGUI.kButtonPadding * 2,
                LODGroupGUI.kRenderersButtonHeight - LODGroupGUI.kButtonPadding * 2), buttons, drawArea);
        }

        private void HandleAddRenderer(Rect position, IEnumerable<Rect> alreadyDrawn, Rect drawArea)
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.Repaint:
                {
                    bool isHovered = position.Contains(evt.mousePosition);
                    LODGroupGUI.Styles.m_LODStandardButton.Draw(position, GUIContent.none, isHovered, false, false, false);
                    LODGroupGUI.Styles.m_LODRendererAddButton.Draw(new Rect(position.x - LODGroupGUI.kButtonPadding, position.y, position.width, position.height), "Add", false, false, false, false);
                    break;
                }
                case EventType.DragUpdated:
                case EventType.DragPerform:
                {
                    bool dragArea = false;
                    if (drawArea.Contains(evt.mousePosition))
                    {
                        if (alreadyDrawn.All(x => !x.Contains(evt.mousePosition)))
                            dragArea = true;
                    }

                    if (!dragArea)
                        break;

                    // If we are over a valid range, make sure we have a game object...
                    if (DragAndDrop.objectReferences.Length > 0)
                    {
                        DragAndDrop.visualMode = m_IsPrefab ? DragAndDropVisualMode.None : DragAndDropVisualMode.Copy;

                        if (evt.type == EventType.DragPerform)
                        {
                            // First try gameobjects...
                            var selectedGameObjects =
                                from go in DragAndDrop.objectReferences
                                where go as GameObject != null
                                select go as GameObject;

                            var renderers = GetRenderers(selectedGameObjects, true);
                            AddGameObjectRenderers(renderers, true);
                            ResetValuesAfterLODObjectIsModified();
                            DragAndDrop.AcceptDrag();

                            evt.Use();
                            break;
                        }
                    }
                    evt.Use();
                    break;
                }
                case EventType.MouseDown:
                {
                    if (position.Contains(evt.mousePosition))
                    {
                        evt.Use();
                        int id = "LODGroupSelector".GetHashCode();
                        ObjectSelector.get.Show(null, typeof(Renderer), null, true);
                        ObjectSelector.get.objectSelectorID = id;
                        GUIUtility.ExitGUI();
                    }
                    break;
                }
                case EventType.ExecuteCommand:
                {
                    string commandName = evt.commandName;
                    if (commandName == ObjectSelector.ObjectSelectorClosedCommand && ObjectSelector.get.objectSelectorID == "LODGroupSelector".GetHashCode())
                    {
                        var selectedObject = ObjectSelector.GetCurrentObject() as GameObject;
                        if (selectedObject != null)
                        {
                            AddGameObjectRenderers(GetRenderers(new List<GameObject> { selectedObject }, true), true);
                            CalculatePrimitiveCountForRenderers();
                        }
                        evt.Use();
                        GUIUtility.ExitGUI();
                    }
                    break;
                }
            }
        }

        private void DrawRendererButton(Rect position, int rendererIndex)
        {
            var renderersProperty = serializedObject.FindProperty(string.Format(kRenderRootPath, activeLOD));
            var rendererRef = renderersProperty.GetArrayElementAtIndex(rendererIndex).FindPropertyRelative("renderer");
            var renderer = rendererRef.objectReferenceValue as Renderer;

            var deleteButton = new Rect(position.xMax - LODGroupGUI.kDeleteButtonSize, position.yMax - LODGroupGUI.kDeleteButtonSize, LODGroupGUI.kDeleteButtonSize, LODGroupGUI.kDeleteButtonSize);

            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.Repaint:
                {
                    if (renderer != null)
                    {
                        GUIContent content;

                        var filter = renderer.GetComponent<MeshFilter>();
                        if (filter != null && filter.sharedMesh != null)
                            content = new GUIContent(AssetPreview.GetAssetPreview(filter.sharedMesh), renderer.gameObject.name);
                        else if (renderer is SkinnedMeshRenderer)
                            content = new GUIContent(AssetPreview.GetAssetPreview((renderer as SkinnedMeshRenderer).sharedMesh), renderer.gameObject.name);
                        else if (renderer is BillboardRenderer)
                            content = new GUIContent(AssetPreview.GetAssetPreview((renderer as BillboardRenderer).billboard), renderer.gameObject.name);
                        else
                            content = new GUIContent(ObjectNames.NicifyVariableName(renderer.GetType().Name), renderer.gameObject.name);

                        LODGroupGUI.Styles.m_LODBlackBox.Draw(position, GUIContent.none, false, false, false, false);

                        LODGroupGUI.Styles.m_LODRendererButton.Draw(
                            new Rect(
                                position.x + LODGroupGUI.kButtonPadding,
                                position.y + LODGroupGUI.kButtonPadding,
                                position.width - 2 * LODGroupGUI.kButtonPadding, position.height - 2 * LODGroupGUI.kButtonPadding),
                            content, false, false, false, false);
                    }
                    else
                    {
                        LODGroupGUI.Styles.m_LODBlackBox.Draw(position, GUIContent.none, false, false, false, false);
                        LODGroupGUI.Styles.m_LODRendererAddButton.Draw(position, "Empty", false, false, false, false);
                    }

                    if (!m_IsPrefab)
                    {
                        LODGroupGUI.Styles.m_LODBlackBox.Draw(deleteButton, GUIContent.none, false, false, false, false);
                        LODGroupGUI.Styles.m_LODRendererRemove.Draw(deleteButton, LODGroupGUI.Styles.m_IconRendererMinus, false, false, false, false);
                    }
                    break;
                }
                case EventType.MouseDown:
                {
                    if (!m_IsPrefab && deleteButton.Contains(evt.mousePosition))
                    {
                        renderersProperty.DeleteArrayElementAtIndex(rendererIndex);
                        evt.Use();
                        serializedObject.ApplyModifiedProperties();
                        CalculatePrimitiveCountForRenderers();
                        m_LODGroup.RecalculateBounds();
                    }
                    else if (position.Contains(evt.mousePosition))
                    {
                        EditorGUIUtility.PingObject(renderer);
                        evt.Use();
                    }
                    break;
                }
            }
        }

        // Get all the renderers that are attached to this game object
        private IEnumerable<Renderer> GetRenderers(IEnumerable<GameObject> selectedGameObjects, bool searchChildren)
        {
            // Only allow renderers that are parented to this LODGroup
            if (EditorUtility.IsPersistent(m_LODGroup))
                return new List<Renderer>();

            var validSearchObjects = from go in selectedGameObjects
                where go.transform.IsChildOf(m_LODGroup.transform)
                select go;

            var nonChildObjects = from go in selectedGameObjects
                where !go.transform.IsChildOf(m_LODGroup.transform)
                select go;

            // Handle reparenting
            var validChildren = new List<GameObject>();
            if (nonChildObjects.Count() > 0)
            {
                const string kReparent = "Some objects are not children of the LODGroup GameObject. Do you want to reparent them and add them to the LODGroup?";
                if (EditorUtility.DisplayDialog(
                    "Reparent GameObjects",
                    kReparent,
                    "Yes, Reparent",
                    "No, Use Only Existing Children"))
                {
                    foreach (var go in nonChildObjects)
                    {
                        if (EditorUtility.IsPersistent(go))
                        {
                            var newGo = Instantiate(go);
                            if (newGo != null)
                            {
                                newGo.transform.parent = m_LODGroup.transform;
                                newGo.transform.localPosition = Vector3.zero;
                                newGo.transform.localRotation = Quaternion.identity;
                                validChildren.Add(newGo);
                            }
                        }
                        else
                        {
                            go.transform.parent = m_LODGroup.transform;
                            validChildren.Add(go);
                        }
                    }
                    validSearchObjects = validSearchObjects.Union(validChildren);
                }
            }

            //Get all the renderers
            var renderers = new List<Renderer>();
            foreach (var go in validSearchObjects)
            {
                if (searchChildren)
                    renderers.AddRange(go.GetComponentsInChildren<Renderer>());
                else
                    renderers.Add(go.GetComponent<Renderer>());
            }

            // Then try renderers
            var selectedRenderers = from go in DragAndDrop.objectReferences
                where go as Renderer != null
                select go as Renderer;

            renderers.AddRange(selectedRenderers);
            return renderers;
        }

        // Add the given renderers to the current LOD group
        private void AddGameObjectRenderers(IEnumerable<Renderer> toAdd, bool add)
        {
            var renderersProperty = serializedObject.FindProperty(string.Format(kRenderRootPath, activeLOD));

            if (!add)
                renderersProperty.ClearArray();

            // On add make a list of the old renderers (to check for dupes)
            var oldRenderers = new List<Renderer>();
            for (var i = 0; i < renderersProperty.arraySize; i++)
            {
                var lodRenderRef = renderersProperty.GetArrayElementAtIndex(i).FindPropertyRelative("renderer");
                var renderer = lodRenderRef.objectReferenceValue as Renderer;

                if (renderer == null)
                    continue;

                oldRenderers.Add(renderer);
            }

            foreach (var renderer in toAdd)
            {
                // Ensure that we don't add the renderer if it already exists
                if (oldRenderers.Contains(renderer))
                    continue;

                renderersProperty.arraySize += 1;
                renderersProperty.
                    GetArrayElementAtIndex(renderersProperty.arraySize - 1).
                    FindPropertyRelative("renderer").objectReferenceValue = renderer;

                // Stop readd
                oldRenderers.Add(renderer);
            }
            serializedObject.ApplyModifiedProperties();
            m_LODGroup.RecalculateBounds();
            ResetValuesAfterLODObjectIsModified();
            ExpandSelectedHeaderAndCloseRemaining(activeLOD);
        }

        // Callback action for mouse context clicks on the LOD slider(right click ect)
        private class LODAction
        {
            private readonly float m_Percentage;
            private readonly List<LODGroupGUI.LODInfo> m_LODs;
            private readonly Vector2 m_ClickedPosition;
            private readonly SerializedObject m_ObjectRef;
            private readonly SerializedProperty m_LODsProperty;

            public delegate void Callback();
            private readonly Callback m_Callback;

            public LODAction(List<LODGroupGUI.LODInfo> lods, float percentage, Vector2 clickedPosition, SerializedProperty propLODs, Callback callback)
            {
                m_LODs = lods;
                m_Percentage = percentage;
                m_ClickedPosition = clickedPosition;
                m_LODsProperty = propLODs;
                m_ObjectRef = propLODs.serializedObject;
                m_Callback = callback;
            }

            public void InsertLOD()
            {
                if (!m_LODsProperty.isArray)
                    return;

                // Find where to insert
                int insertIndex = -1;
                foreach (var lod in m_LODs)
                {
                    if (m_Percentage > lod.RawScreenPercent)
                    {
                        insertIndex = lod.LODLevel;
                        break;
                    }
                }

                // Clicked in the culled area... duplicate last
                if (insertIndex < 0)
                {
                    m_LODsProperty.InsertArrayElementAtIndex(m_LODs.Count);
                    insertIndex = m_LODs.Count;
                }
                else
                {
                    m_LODsProperty.InsertArrayElementAtIndex(insertIndex);
                }

                // Null out the copied renderers (we want the list to be empty)
                var renderers = m_ObjectRef.FindProperty(string.Format(kRenderRootPath, insertIndex));
                renderers.arraySize = 0;

                var newLOD = m_LODsProperty.GetArrayElementAtIndex(insertIndex);
                newLOD.FindPropertyRelative("screenRelativeHeight").floatValue = m_Percentage;

                m_ObjectRef.ApplyModifiedProperties();
                if (m_Callback != null)
                    m_Callback();
            }

            public void DeleteLOD()
            {
                if (m_LODs.Count <= 0)
                    return;

                // Check for range click
                foreach (var lod in m_LODs)
                {
                    var numberOfRenderers = m_ObjectRef.FindProperty(string.Format(kRenderRootPath, lod.LODLevel)).arraySize;
                    if (lod.m_RangePosition.Contains(m_ClickedPosition) && (numberOfRenderers == 0
                                                                            || EditorUtility.DisplayDialog("Delete LOD",
                                                                                "Are you sure you wish to delete this LOD?",
                                                                                "Yes",
                                                                                "No")))
                    {
                        var lodData = m_ObjectRef.FindProperty(string.Format(kLODDataPath, lod.LODLevel));
                        lodData.DeleteCommand();

                        m_ObjectRef.ApplyModifiedProperties();
                        if (m_Callback != null)
                            m_Callback();
                        break;
                    }
                }
            }
        }

        private void DeletedLOD()
        {
            m_SelectedLOD--;

            ResetValuesAfterLODObjectIsModified();
        }

        // Set the camera distance so that the current LOD group covers the desired percentage of the screen
        private static void UpdateCamera(float desiredPercentage, LODGroup group)
        {
            var worldReferencePoint = LODUtility.CalculateWorldReferencePoint(group);
            var percentage = Mathf.Max(desiredPercentage / QualitySettings.lodBias, 0.000001f);

            var sceneView = SceneView.lastActiveSceneView;
            var sceneCamera = sceneView.camera;

            // Figure out a distance based on the percentage
            var distance = LODUtility.CalculateDistance(sceneCamera, percentage, group);

            // We need to do inverse of SceneView.cameraDistance:
            // given the distance, need to figure out "size" to focus the scene view on.
            float size;
            if (sceneCamera.orthographic)
            {
                size = distance;
                if (sceneCamera.aspect < 1.0)
                    size *= sceneCamera.aspect;
            }
            else
            {
                var fov = sceneCamera.fieldOfView;
                size = distance * Mathf.Sin(fov * 0.5f * Mathf.Deg2Rad);
            }

            SceneView.lastActiveSceneView.LookAtDirect(worldReferencePoint, sceneCamera.transform.rotation, size);
        }

        private void UpdateSelectedLODFromCamera(IEnumerable<LODGroupGUI.LODInfo> lods, float cameraPercent)
        {
            foreach (var lod in lods)
            {
                if (cameraPercent > lod.RawScreenPercent)
                {
                    m_SelectedLOD = lod.LODLevel;
                    break;
                }
            }
        }

        private readonly int m_LODSliderId = "LODSliderIDHash".GetHashCode();
        private readonly int m_CameraSliderId = "LODCameraIDHash".GetHashCode();
        private void DrawLODLevelSlider(Rect sliderPosition, List<LODGroupGUI.LODInfo> lods)
        {
            int sliderId = GUIUtility.GetControlID(m_LODSliderId, FocusType.Passive);
            int camerId = GUIUtility.GetControlID(m_CameraSliderId, FocusType.Passive);
            Event evt = Event.current;

            switch (evt.GetTypeForControl(sliderId))
            {
                case EventType.Repaint:
                {
                    LODGroupGUI.DrawLODSlider(sliderPosition, lods, activeLOD);
                    break;
                }
                case EventType.MouseDown:
                {
                    // Handle right click first
                    if (evt.button == 1 && sliderPosition.Contains(evt.mousePosition))
                    {
                        var cameraPercent = LODGroupGUI.GetCameraPercent(evt.mousePosition, sliderPosition);
                        var pm = new GenericMenu();
                        if (lods.Count >= 8)
                        {
                            pm.AddDisabledItem(EditorGUIUtility.TrTextContent("Insert Before"));
                        }
                        else
                        {
                            pm.AddItem(EditorGUIUtility.TrTextContent("Insert Before"), false,
                                new LODAction(lods, cameraPercent, evt.mousePosition, m_LODs, ResetValuesAfterLODObjectIsModified).
                                InsertLOD);
                        }

                        // Figure out if we clicked in the culled region
                        var disabledRegion = true;
                        if (lods.Count > 0 && lods[lods.Count - 1].RawScreenPercent < cameraPercent)
                            disabledRegion = false;

                        if (disabledRegion)
                            pm.AddDisabledItem(EditorGUIUtility.TrTextContent("Delete"));
                        else
                            pm.AddItem(EditorGUIUtility.TrTextContent("Delete"), false,
                                new LODAction(lods, cameraPercent, evt.mousePosition, m_LODs, DeletedLOD).
                                DeleteLOD);
                        pm.ShowAsContext();

                        // Do selection
                        bool selected = false;
                        foreach (var lod in lods)
                        {
                            if (lod.m_RangePosition.Contains(evt.mousePosition))
                            {
                                m_SelectedLOD = lod.LODLevel;
                                selected = true;
                                break;
                            }
                        }

                        if (!selected)
                            m_SelectedLOD = -1;

                        evt.Use();

                        break;
                    }

                    // Slightly grow position on the x because edge buttons overflow by 5 pixels
                    var barPosition = sliderPosition;
                    barPosition.x -= 5;
                    barPosition.width += 10;

                    if (barPosition.Contains(evt.mousePosition))
                    {
                        evt.Use();
                        GUIUtility.hotControl = sliderId;

                        // Check for button click
                        var clickedButton = false;

                        // case:464019 have to re-sort the LOD array for these buttons to get the overlaps in the right order...
                        var lodsLeft = lods.Where(lod => lod.ScreenPercent > 0.5f).OrderByDescending(x => x.LODLevel);
                        var lodsRight = lods.Where(lod => lod.ScreenPercent <= 0.5f).OrderBy(x => x.LODLevel);

                        var lodButtonOrder = new List<LODGroupGUI.LODInfo>();
                        lodButtonOrder.AddRange(lodsLeft);
                        lodButtonOrder.AddRange(lodsRight);

                        foreach (var lod in lodButtonOrder)
                        {
                            if (lod.m_ButtonPosition.Contains(evt.mousePosition))
                            {
                                m_SelectedLODSlider = lod.LODLevel;
                                clickedButton = true;
                                // Bias by 0.1% so that there is no skipping when sliding
                                BeginLODDrag(lod.RawScreenPercent + 0.001f, m_LODGroup);
                                break;
                            }
                        }

                        if (!clickedButton)
                        {
                            // Check for range click
                            foreach (var lod in lodButtonOrder)
                            {
                                if (lod.m_RangePosition.Contains(evt.mousePosition))
                                {
                                    m_SelectedLODSlider = -1;
                                    m_SelectedLOD = lod.LODLevel;
                                    ExpandSelectedHeaderAndCloseRemaining(m_SelectedLOD);
                                    break;
                                }
                            }
                        }
                    }
                    break;
                }

                case EventType.MouseDrag:
                {
                    if (GUIUtility.hotControl == sliderId && m_SelectedLODSlider >= 0 && lods[m_SelectedLODSlider] != null)
                    {
                        evt.Use();

                        var cameraPercent = LODGroupGUI.GetCameraPercent(evt.mousePosition, sliderPosition);
                        // Bias by 0.1% so that there is no skipping when sliding
                        LODGroupGUI.SetSelectedLODLevelPercentage(cameraPercent - 0.001f, m_SelectedLODSlider, lods);
                        var percentageProperty = serializedObject.FindProperty(string.Format(kPixelHeightDataPath, lods[m_SelectedLODSlider].LODLevel));
                        percentageProperty.floatValue = lods[m_SelectedLODSlider].RawScreenPercent;

                        UpdateLODDrag(cameraPercent, m_LODGroup);
                    }
                    break;
                }

                case EventType.MouseUp:
                {
                    if (GUIUtility.hotControl == sliderId)
                    {
                        GUIUtility.hotControl = 0;
                        m_SelectedLODSlider = -1;
                        EndLODDrag();
                        evt.Use();
                    }
                    break;
                }

                case EventType.DragUpdated:
                case EventType.DragPerform:
                {
                    // -2 = invalid region
                    // -1 = culledregion
                    // rest = LOD level
                    var lodLevel = -2;
                    // Is the mouse over a valid LOD level range?
                    foreach (var lod in lods)
                    {
                        if (lod.m_RangePosition.Contains(evt.mousePosition))
                        {
                            lodLevel = lod.LODLevel;
                            break;
                        }
                    }

                    if (lodLevel == -2)
                    {
                        var culledRange = LODGroupGUI.GetCulledBox(sliderPosition, lods.Count > 0 ? lods[lods.Count - 1].ScreenPercent : 1.0f);
                        if (culledRange.Contains(evt.mousePosition))
                        {
                            lodLevel = -1;
                        }
                    }

                    if (lodLevel >= -1)
                    {
                        // Actually set LOD level now
                        m_SelectedLOD = lodLevel;

                        if (DragAndDrop.objectReferences.Length > 0)
                        {
                            DragAndDrop.visualMode = m_IsPrefab ? DragAndDropVisualMode.None : DragAndDropVisualMode.Copy;

                            if (evt.type == EventType.DragPerform)
                            {
                                // First try gameobjects...
                                var selectedGameObjects = from go in DragAndDrop.objectReferences
                                    where go as GameObject != null
                                    select go as GameObject;
                                var renderers = GetRenderers(selectedGameObjects, true);

                                if (lodLevel == -1)
                                {
                                    m_LODs.arraySize++;
                                    var pixelHeightNew = serializedObject.FindProperty(string.Format(kPixelHeightDataPath, lods.Count));

                                    if (lods.Count == 0)
                                        pixelHeightNew.floatValue = 0.5f;
                                    else
                                    {
                                        var pixelHeightPrevious = serializedObject.FindProperty(string.Format(kPixelHeightDataPath, lods.Count - 1));
                                        pixelHeightNew.floatValue = pixelHeightPrevious.floatValue / 2.0f;
                                    }

                                    m_SelectedLOD = lods.Count;
                                    AddGameObjectRenderers(renderers, false);
                                }
                                else
                                {
                                    AddGameObjectRenderers(renderers, true);
                                }
                                DragAndDrop.AcceptDrag();
                            }
                        }
                        evt.Use();
                    }

                    break;
                }
                case EventType.DragExited:
                {
                    evt.Use();
                    break;
                }
            }
            if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null && !m_IsPrefab)
            {
                var camera = SceneView.lastActiveSceneView.camera;

                var info = LODUtility.CalculateVisualizationData(camera, m_LODGroup, -1);
                var linearHeight = info.activeRelativeScreenSize;
                var relativeHeight = LODGroupGUI.DelinearizeScreenPercentage(linearHeight);

                var worldReferencePoint = LODUtility.CalculateWorldReferencePoint(m_LODGroup);
                var vectorFromObjectToCamera = (SceneView.lastActiveSceneView.camera.transform.position - worldReferencePoint).normalized;
                if (Vector3.Dot(camera.transform.forward, vectorFromObjectToCamera) > 0f)
                    relativeHeight = 1.0f;

                var cameraRect = LODGroupGUI.CalcLODButton(sliderPosition, Mathf.Clamp01(relativeHeight));
                var cameraIconRect = new Rect(cameraRect.center.x - 15, cameraRect.y - 25, 32, 32);
                var cameraLineRect = new Rect(cameraRect.center.x - 1, cameraRect.y, 2, cameraRect.height);
                var cameraPercentRect = new Rect(cameraIconRect.center.x - 5, cameraLineRect.yMax, 35, 20);

                switch (evt.GetTypeForControl(camerId))
                {
                    case EventType.Repaint:
                    {
                        // Draw a marker to indicate the current scene camera distance
                        var colorCache = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(colorCache.r, colorCache.g, colorCache.b, 0.8f);
                        LODGroupGUI.Styles.m_LODCameraLine.Draw(cameraLineRect, false, false, false, false);
                        GUI.backgroundColor = colorCache;
                        GUI.Label(cameraIconRect, LODGroupGUI.Styles.m_CameraIcon, GUIStyle.none);
                        LODGroupGUI.Styles.m_LODSliderText.Draw(cameraPercentRect, String.Format("{0:0}%", Mathf.Clamp01(linearHeight) * 100.0f), false, false, false, false);
                        break;
                    }
                    case EventType.MouseDown:
                    {
                        if (cameraIconRect.Contains(evt.mousePosition))
                        {
                            evt.Use();
                            var cameraPercent = LODGroupGUI.GetCameraPercent(evt.mousePosition, sliderPosition);

                            // Update the selected LOD to be where the camera is if we click the camera
                            UpdateSelectedLODFromCamera(lods, cameraPercent);
                            GUIUtility.hotControl = camerId;

                            BeginLODDrag(cameraPercent, m_LODGroup);
                        }
                        break;
                    }
                    case EventType.MouseDrag:
                    {
                        if (GUIUtility.hotControl == camerId)
                        {
                            evt.Use();
                            var cameraPercent = LODGroupGUI.GetCameraPercent(evt.mousePosition, sliderPosition);

                            // Change the active LOD level if the camera moves into a new LOD level
                            UpdateSelectedLODFromCamera(lods, cameraPercent);
                            UpdateLODDrag(cameraPercent, m_LODGroup);
                        }
                        break;
                    }
                    case EventType.MouseUp:
                    {
                        if (GUIUtility.hotControl == camerId)
                        {
                            EndLODDrag();
                            GUIUtility.hotControl = 0;
                            evt.Use();
                        }
                        break;
                    }
                }
            }
        }

        private void BeginLODDrag(float desiredPercentage, LODGroup group)
        {
            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null || m_IsPrefab)
                return;

            UpdateCamera(desiredPercentage, group);
            SceneView.lastActiveSceneView.ClearSearchFilter();
            SceneView.lastActiveSceneView.SetSceneViewFilteringForLODGroups(true);
            HierarchyProperty.FilterSingleSceneObject(group.gameObject.GetInstanceID(), false);
            SceneView.RepaintAll();
        }

        private void UpdateLODDrag(float desiredPercentage, LODGroup group)
        {
            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null || m_IsPrefab)
                return;

            UpdateCamera(desiredPercentage, group);
            SceneView.RepaintAll();
        }

        private void EndLODDrag()
        {
            if (SceneView.lastActiveSceneView == null || SceneView.lastActiveSceneView.camera == null || m_IsPrefab)
                return;

            SceneView.lastActiveSceneView.SetSceneViewFilteringForLODGroups(false);
            SceneView.lastActiveSceneView.ClearSearchFilter();
            // Clearing the search filter of a SceneView will not actually reset the visibility values
            // of the GameObjects in the scene so we have to explicitly do that  (case 770915).
            HierarchyProperty.ClearSceneObjectsFilter();
        }

        //Code to be able to send percentages to this gameobjects lightmap scale
        private class LODLightmapScale
        {
            public readonly float m_Scale;
            public readonly List<SerializedProperty> m_Renderers;

            public LODLightmapScale(float scale, List<SerializedProperty> renderers)
            {
                m_Scale = scale;
                m_Renderers = renderers;
            }
        }

        private void SendPercentagesToLightmapScale()
        {
            //List of renderers per LOD
            var lodRenderers = new List<LODLightmapScale>();

            for (var i = 0; i < m_NumberOfLODs; i++)
            {
                var renderersProperty = serializedObject.FindProperty(string.Format(kRenderRootPath, i));
                var renderersAtLOD = new List<SerializedProperty>();

                for (var k = 0; k < renderersProperty.arraySize; k++)
                {
                    var rendererRef = renderersProperty.GetArrayElementAtIndex(k).FindPropertyRelative("renderer");

                    if (rendererRef != null)
                        renderersAtLOD.Add(rendererRef);
                }
                var pixelHeight = i == 0 ? 1.0f : serializedObject.FindProperty(string.Format(kPixelHeightDataPath, i - 1)).floatValue;
                lodRenderers.Add(new LODLightmapScale(pixelHeight, renderersAtLOD));
            }

            // set from least detailed to most detailed, as renderers can be in multiple layers
            for (var i = m_NumberOfLODs - 1; i >= 0; i--)
            {
                SetLODLightmapScale(lodRenderers[i]);
            }
        }

        private static void SetLODLightmapScale(LODLightmapScale lodRenderer)
        {
            foreach (var renderer in lodRenderer.m_Renderers)
            {
                if (renderer.objectReferenceValue == null)
                    continue;

                using (var so = new SerializedObject(renderer.objectReferenceValue))
                {
                    var lightmapScaleProp = so.FindProperty("m_ScaleInLightmap");
                    lightmapScaleProp.floatValue = Mathf.Max(0.0f, lodRenderer.m_Scale * (1.0f / LightmapVisualization.GetLightmapLODLevelScale((Renderer)renderer.objectReferenceValue)));
                    so.ApplyModifiedProperties();
                }
            }
        }

        // / PREVIEW GUI CODE BELOW
        public override bool HasPreviewGUI()
        {
            return (target != null);
        }

        private PreviewRenderUtility m_PreviewUtility;
        static private readonly GUIContent[] kSLightIcons = {null, null};
        private Vector2 m_PreviewDir = new Vector2(0, -20);

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "LOD preview \nnot available");
                return;
            }

            InitPreview();
            m_PreviewDir = PreviewGUI.Drag2D(m_PreviewDir, r);
            m_PreviewDir.y = Mathf.Clamp(m_PreviewDir.y, -89.0f, 89.0f);

            if (Event.current.type != EventType.Repaint)
                return;

            m_PreviewUtility.BeginPreview(r, background);

            DoRenderPreview();

            m_PreviewUtility.EndAndDrawPreview(r);
        }

        void InitPreview()
        {
            if (m_PreviewUtility == null)
                m_PreviewUtility = new PreviewRenderUtility();

            if (kSLightIcons[0] == null)
            {
                kSLightIcons[0] = EditorGUIUtility.IconContent("PreMatLight0");
                kSLightIcons[1] = EditorGUIUtility.IconContent("PreMatLight1");
            }
        }

        protected void DoRenderPreview()
        {
            bool LODSelected = activeLOD >= 0;
            if (m_PreviewUtility.renderTexture.width <= 0
                || m_PreviewUtility.renderTexture.height <= 0
                || m_NumberOfLODs <= 0)
                return;

            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool boundsSet = false;

            Renderer[] renderers = null;
            if (target is LODGroup targetLODGroup)
            {
                LOD[] lodArray = targetLODGroup.GetLODs();
                int lodIndex = LODSelected ? activeLOD : 0; // display LOD0 if no LOD is selected
                if (lodIndex >= 0 && lodIndex < lodArray.Length)
                {
                    renderers = lodArray[lodIndex].renderers; 
                }
            }
            if (renderers == null)
                return;

            var meshsToRender = new List<MeshFilter>();
            var billboards = new List<BillboardRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null)
                    continue;

                var meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null && meshFilter.sharedMesh.subMeshCount > 0)
                {
                    meshsToRender.Add(meshFilter);
                }

                var billboard = renderer.GetComponent<BillboardRenderer>();
                if (billboard != null && billboard.billboard != null && billboard.sharedMaterial != null)
                {
                    billboards.Add(billboard);
                }

                if (!boundsSet)
                {
                    bounds = renderer.bounds;
                    boundsSet = true;
                }
                else
                    bounds.Encapsulate(renderer.bounds);
            }

            if (!boundsSet)
                return;

            var halfSize = bounds.extents.magnitude;
            var distance = halfSize * 10.0f;

            var viewDir = -(m_PreviewDir / 100.0f);

            m_PreviewUtility.camera.transform.position = bounds.center + (new Vector3(Mathf.Sin(viewDir.x) * Mathf.Cos(viewDir.y), Mathf.Sin(viewDir.y), Mathf.Cos(viewDir.x) * Mathf.Cos(viewDir.y)) * distance);

            m_PreviewUtility.camera.transform.LookAt(bounds.center);
            m_PreviewUtility.camera.nearClipPlane = 0.05f;
            m_PreviewUtility.camera.farClipPlane = 1000.0f;

            m_PreviewUtility.lights[0].intensity = 1.0f;
            m_PreviewUtility.lights[0].transform.rotation = Quaternion.Euler(50f, 50f, 0);
            m_PreviewUtility.lights[1].intensity = 1.0f;
            m_PreviewUtility.ambientColor = new Color(.2f, .2f, .2f, 0);

            foreach (var meshFilter in meshsToRender)
            {
                for (int k = 0; k < meshFilter.sharedMesh.subMeshCount; k++)
                {
                    if (k < meshFilter.GetComponent<Renderer>().sharedMaterials.Length)
                    {
                        var matrix = Matrix4x4.TRS(meshFilter.transform.position, meshFilter.transform.rotation, meshFilter.transform.localScale);
                        m_PreviewUtility.DrawMesh(
                            meshFilter.sharedMesh,
                            matrix,
                            meshFilter.GetComponent<Renderer>().sharedMaterials[k],
                            k);
                    }
                }
            }

            foreach (var billboard in billboards)
            {
                if (m_BillboardPreviewMesh == null)
                {
                    m_BillboardPreviewMesh = new Mesh();
                    m_BillboardPreviewMesh.hideFlags = HideFlags.HideAndDontSave;
                    m_BillboardPreviewMesh.MarkDynamic();
                }
                if (m_BillboardPreviewProperties == null)
                    m_BillboardPreviewProperties = new MaterialPropertyBlock();
                BillboardAssetInspector.MakeRenderMesh(m_BillboardPreviewMesh, billboard.billboard);
                billboard.billboard.MakeMaterialProperties(m_BillboardPreviewProperties, m_PreviewUtility.camera);
                var matrix = Matrix4x4.TRS(billboard.transform.position, billboard.transform.rotation, billboard.transform.localScale);
                m_PreviewUtility.DrawMesh(
                    m_BillboardPreviewMesh,
                    matrix,
                    billboard.sharedMaterial,
                    0, m_BillboardPreviewProperties);
            }

            m_PreviewUtility.Render(Unsupported.useScriptableRenderPipeline);
        }

        override public string GetInfoString()
        {
            if (SceneView.lastActiveSceneView == null
                || SceneView.lastActiveSceneView.camera == null
                || m_NumberOfLODs <= 0
                || activeLOD < 0)
                return "";

            var materials = new List<Material>();
            var renderers = serializedObject.FindProperty(string.Format(kRenderRootPath, activeLOD));
            for (int i = 0; i < renderers.arraySize; i++)
            {
                var renderRef = renderers.GetArrayElementAtIndex(i).FindPropertyRelative("renderer");
                var renderer = renderRef.objectReferenceValue as Renderer;

                if (renderer != null)
                    materials.AddRange(renderer.sharedMaterials);
            }

            var camera = SceneView.lastActiveSceneView.camera;

            var info = LODUtility.CalculateVisualizationData(camera, m_LODGroup, activeLOD);
            return activeLOD != -1 ? string.Format("{0} Renderer(s)\n{1} Triangle(s)\n{2} Material(s)", renderers.arraySize, info.triangleCount, materials.Distinct().Count()) : "LOD: culled";
        }
    }
}
