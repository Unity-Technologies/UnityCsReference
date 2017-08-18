// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor
{
    [CustomEditor(typeof(LODGroup))]
    internal class LODGroupEditor : Editor
    {
        private int m_SelectedLODSlider = -1;
        private int m_SelectedLOD = -1;
        private int m_NumberOfLODs;

        private LODGroup m_LODGroup;
        private bool m_IsPrefab;

        private SerializedProperty m_FadeMode;
        private SerializedProperty m_AnimateCrossFading;
        private SerializedProperty m_LODs;

        private AnimBool m_ShowAnimateCrossFading = new AnimBool();
        private AnimBool m_ShowFadeTransitionWidth = new AnimBool();

        void OnEnable()
        {
            // TODO: support multi-editing?
            m_FadeMode = serializedObject.FindProperty("m_FadeMode");
            m_AnimateCrossFading = serializedObject.FindProperty("m_AnimateCrossFading");
            m_LODs = serializedObject.FindProperty("m_LODs");

            m_ShowAnimateCrossFading.value = m_FadeMode.intValue != (int)LODFadeMode.None;
            m_ShowAnimateCrossFading.valueChanged.AddListener(Repaint);
            m_ShowFadeTransitionWidth.value = false;
            m_ShowFadeTransitionWidth.valueChanged.AddListener(Repaint);

            EditorApplication.update += Update;

            m_LODGroup = (LODGroup)target;

            // Calculate if the newly selected LOD group is a prefab... they require special handling
            var type = PrefabUtility.GetPrefabType(m_LODGroup.gameObject);
            m_IsPrefab = type == PrefabType.Prefab || type == PrefabType.ModelPrefab;

            Repaint();
        }

        void OnDisable()
        {
            EditorApplication.update -= Update;

            m_ShowAnimateCrossFading.valueChanged.RemoveListener(Repaint);
            m_ShowFadeTransitionWidth.valueChanged.RemoveListener(Repaint);
            if (m_PreviewUtility != null)
                m_PreviewUtility.Cleanup();
        }

        // Find the given sceen space recangular bounds from a list of vector 3 points.
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

        public void OnSceneGUI()
        {
            if (Event.current.type != EventType.Repaint
                || Camera.current == null
                || SceneView.lastActiveSceneView != SceneView.currentDrawingSceneView)
                return;

            Camera camera = SceneView.lastActiveSceneView.camera;
            var worldReferencePoint = LODUtility.CalculateWorldReferencePoint(m_LODGroup);

            if (Vector3.Dot(camera.transform.forward,
                    (camera.transform.position - worldReferencePoint).normalized) > 0)
                return;

            var info = LODUtility.CalculateVisualizationData(camera, m_LODGroup, -1);
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
            return AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(PrefabUtility.GetPrefabParent(target))) as ModelImporter;
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
                if (renderers.arraySize == 1 && renderers.GetArrayElementAtIndex(0).FindPropertyRelative("renderer").objectReferenceValue is BillboardRenderer)
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

            // Prepass to remove all empty renderers
            if (m_NumberOfLODs > 0 && activeLOD >= 0)
            {
                var renderersProperty = serializedObject.FindProperty(string.Format(kRenderRootPath, activeLOD));
                for (var i = renderersProperty.arraySize - 1; i >= 0; i--)
                {
                    var rendererRef = renderersProperty.GetArrayElementAtIndex(i).FindPropertyRelative("renderer");
                    var renderer = rendererRef.objectReferenceValue as Renderer;

                    if (renderer == null)
                        renderersProperty.DeleteArrayElementAtIndex(i);
                }
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
            if (m_NumberOfLODs > 0 && activeLOD >= 0 && activeLOD < m_NumberOfLODs)
            {
                m_ShowFadeTransitionWidth.target = IsLODUsingCrossFadeWidth(activeLOD);
                if (EditorGUILayout.BeginFadeGroup(m_ShowFadeTransitionWidth.faded))
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(string.Format(kFadeTransitionWidthDataPath, activeLOD)));
                EditorGUILayout.EndFadeGroup();
                DrawRenderersInfo(EditorGUIUtility.currentViewWidth);
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

            var importer = PrefabUtility.GetPrefabType(target) == PrefabType.ModelPrefabInstance ? GetImporter() : null;
            if (importer != null)
            {
                var importerRef = new SerializedObject(importer);
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

            // Apply the property, handle undo
            serializedObject.ApplyModifiedProperties();

            GUI.enabled = initiallyEnabled;
        }

        // Draw the renderers for the current LOD group
        // Arrange in a grid
        private void DrawRenderersInfo(float availableWidth)
        {
            var horizontalCount = Mathf.FloorToInt(availableWidth / LODGroupGUI.kRenderersButtonHeight);
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
                    LODGroupGUI.Styles.m_LODStandardButton.Draw(position, GUIContent.none, false, false, false, false);
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
                    if (DragAndDrop.objectReferences.Count() > 0)
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
                    if (commandName == "ObjectSelectorClosed" && ObjectSelector.get.objectSelectorID == "LODGroupSelector".GetHashCode())
                    {
                        var selectedObject = ObjectSelector.GetCurrentObject() as GameObject;
                        if (selectedObject != null)
                            AddGameObjectRenderers(GetRenderers(new List<GameObject> { selectedObject }, true), true);
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
                        LODGroupGUI.Styles.m_LODRendererButton.Draw(position, "<Empty>", false, false, false, false);
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
                            var newGo = Instantiate(go) as GameObject;
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
        }

        // Callabck action for mouse context clicks on the LOD slider(right click ect)
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
                if (m_Callback != null)
                    m_Callback();

                m_ObjectRef.ApplyModifiedProperties();
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
        }

        // Set the camera distance so that the current LOD group covers the desired percentage of the screen
        private static void UpdateCamera(float desiredPercentage, LODGroup group)
        {
            var worldReferencePoint = LODUtility.CalculateWorldReferencePoint(group);
            var percentage = Mathf.Max(desiredPercentage / QualitySettings.lodBias, 0.000001f);

            // Figure out a distance based on the percentage
            var distance = LODUtility.CalculateDistance(SceneView.lastActiveSceneView.camera, percentage, group);

            if (SceneView.lastActiveSceneView.camera.orthographic)
                distance *= Mathf.Sqrt(2 * SceneView.lastActiveSceneView.camera.aspect);

            SceneView.lastActiveSceneView.LookAtDirect(worldReferencePoint, SceneView.lastActiveSceneView.camera.transform.rotation, distance);
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
                            pm.AddDisabledItem(EditorGUIUtility.TextContent("Insert Before"));
                        }
                        else
                        {
                            pm.AddItem(EditorGUIUtility.TextContent("Insert Before"), false,
                                new LODAction(lods, cameraPercent, evt.mousePosition, m_LODs, null).
                                InsertLOD);
                        }

                        // Figure out if we clicked in the culled region
                        var disabledRegion = true;
                        if (lods.Count > 0 && lods[lods.Count - 1].RawScreenPercent < cameraPercent)
                            disabledRegion = false;

                        if (disabledRegion)
                            pm.AddDisabledItem(EditorGUIUtility.TextContent("Delete"));
                        else
                            pm.AddItem(EditorGUIUtility.TextContent("Delete"), false,
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

                        if (DragAndDrop.objectReferences.Count() > 0)
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
                        break;
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
            SceneView.lastActiveSceneView.SetSceneViewFiltering(true);
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

            SceneView.lastActiveSceneView.SetSceneViewFiltering(false);
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

            for (var i = 0; i < m_NumberOfLODs; i++)
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
                var so = new SerializedObject(renderer.objectReferenceValue);
                var lightmapScaleProp = so.FindProperty("m_ScaleInLightmap");
                lightmapScaleProp.floatValue = Mathf.Max(0.0f, lodRenderer.m_Scale * (1.0f / LightmapVisualization.GetLightmapLODLevelScale((Renderer)renderer.objectReferenceValue)));
                so.ApplyModifiedProperties();
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
            if (m_PreviewUtility.renderTexture.width <= 0
                || m_PreviewUtility.renderTexture.height <= 0
                || m_NumberOfLODs <= 0
                || activeLOD < 0)
                return;

            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            bool boundsSet = false;

            var meshsToRender = new List<MeshFilter>();
            var renderers = serializedObject.FindProperty(string.Format(kRenderRootPath, activeLOD));
            for (int i = 0; i < renderers.arraySize; i++)
            {
                var lodRenderRef = renderers.GetArrayElementAtIndex(i).FindPropertyRelative("renderer");
                var renderer = lodRenderRef.objectReferenceValue as Renderer;

                if (renderer == null)
                    continue;

                var meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null && meshFilter.sharedMesh.subMeshCount > 0)
                {
                    meshsToRender.Add(meshFilter);
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

            m_PreviewUtility.Render();
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
