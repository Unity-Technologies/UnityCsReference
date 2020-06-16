// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.SceneTemplate
{
    [UsedImplicitly, CustomEditor(typeof(SceneTemplateAsset))]
    internal class SceneTemplateAssetInspectorWindow : Editor
    {
        public delegate void SceneTemplateAssetModified(SceneTemplateAsset asset);
        public static event SceneTemplateAssetModified sceneTemplateAssetModified;

        private const string k_TemplateScenePropertyName = nameof(SceneTemplateAsset.templateScene);
        private const string k_TemplatePipelineName = nameof(SceneTemplateAsset.templatePipeline);
        private const string k_TemplateTitlePropertyName = nameof(SceneTemplateAsset.templateName);
        private const string k_TemplateDescriptionPropertyName = nameof(SceneTemplateAsset.description);
        private const string k_TemplateAddToDefaultsPropertyName = nameof(SceneTemplateAsset.addToDefaults);
        private const string k_TemplateThumbnailPropertyName = nameof(SceneTemplateAsset.preview);
        private const string k_DependenciesPropertyName = nameof(SceneTemplateAsset.dependencies);
        private const string k_DependencyPropertyName = nameof(DependencyInfo.dependency);
        private const string k_InstantiationModePropertyName = nameof(DependencyInfo.instantiationMode);

        private const string k_DescriptionTextFieldName = "scene-template-asset-inspector-description-field";
        private const string k_DependencyRowElementName = "scene-template-asset-inspector-dependency-row";
        private const string k_DependenciesLabelName = "scene-template-asset-inspector-dependency-label";
        private const string k_ThumbnailAreaName = "scene-template-asset-inspector-thumbnail-area";
        private const string k_DependencyListView = "scene-template-asset-inspector-list-view";
        private const string k_SnapshotRowName = "scene-template-asset-inspector-snapshot-row";
        private const string k_SceneTemplatePipelineName = "scene-template-pipeline-field";

        private const string k_DependencyInfo = @"This section lists dependencies of the Template Scene assigned above. Enable the Clone option for any dependency that you want to clone when you create a new scene from this template. Unity duplicates cloned assets into a folder with the same name as the new scene. The new scene references any dependencies that are not cloned.";

        const string k_SceneTemplateInfo = @"Scene Template Pipeline must be a Monoscript whose main class derives from ISceneTemplatePipeline or SceneTemplatePipelineAdapter. The main class and the script must have the same name.";

        private const int k_ItemSize = 16;

        private SceneTemplatePreviewArea m_PreviewArea;

        private const string k_SnapshotTooltip = "Take a snapshot based on the selected target then assign it as thumbnail.";
        private const string k_SnapshotButtonLabel = "Take Snapshot";
        private List<SerializedProperty> m_DependenciesProperty = new List<SerializedProperty>();

        private VisualElement m_DependencyHelpBox;
        private ZebraList m_ZebraList;
        private Toggle m_CloneHeaderToggle;

        VisualElement Root { get; set; }

        private class SnapshotTargetInfo
        {
            public string Name { get; set; }
            public Action<SnapshotTargetInfo, SerializedProperty> OnSnapshotAction { get; set; }
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            var sceneTemplateAsset = target as SceneTemplateAsset;

            // Return null to use the default texture
            if (!sceneTemplateAsset || !sceneTemplateAsset.preview)
                return null;

            // We have to decompress compressed textures since saving the template
            // causes a call to RenderStaticPreview and tries to save the main asset thumbnail but fails when
            // the texture is compressed.
            if (GraphicsFormatUtility.IsCompressedFormat(sceneTemplateAsset.preview.graphicsFormat))
            {
                return Decompress(sceneTemplateAsset.preview);
            }

            var previewTex = new Texture2D(sceneTemplateAsset.preview.width, sceneTemplateAsset.preview.height);
            EditorUtility.CopySerialized(sceneTemplateAsset.preview, previewTex);
            return previewTex;
        }

        public override bool UseDefaultMargins()
        {
            return false;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = Root = new VisualElement();
            root.AddToClassList("scene-template-asset-inspector");
            root.AddToClassList(Styles.unityThemeVariables);
            root.AddToClassList(Styles.sceneTemplateThemeVariables);
            root.AddStyleSheetPath(Styles.k_CommonStyleSheetPath);
            root.AddStyleSheetPath(Styles.variableStyleSheet);
            root.style.flexDirection = FlexDirection.Column;

            var detailElement = new VisualElement();

            // Template scene
            var templateSceneProperty = serializedObject.FindProperty(k_TemplateScenePropertyName);
            var templatePropertyField = new PropertyField(templateSceneProperty, "Template Scene");
            templatePropertyField.RegisterCallback<ChangeEvent<Object>>(e =>
            {
                RebuildDependencies(root);
                TriggerSceneTemplateModified();
            });
            detailElement.Add(templatePropertyField);

            // Scene title
            var templateTitleProperty = serializedObject.FindProperty(k_TemplateTitlePropertyName);
            var titlePropertyField = new PropertyField(templateTitleProperty, "Title");
            titlePropertyField.RegisterCallback<ChangeEvent<string>>(e => TriggerSceneTemplateModified());
            detailElement.Add(titlePropertyField);

            // Scene description
            var templateDescriptionProperty = serializedObject.FindProperty(k_TemplateDescriptionPropertyName);
            var descriptionTextField = new TextField("Description", -1, true, false, '*')
            {
                name = k_DescriptionTextFieldName,
                value = templateDescriptionProperty.stringValue
            };
            descriptionTextField.AddToClassList(Styles.classWrappingText);
            descriptionTextField.RegisterValueChangedCallback(e =>
            {
                serializedObject.UpdateIfRequiredOrScript();
                templateDescriptionProperty.stringValue = e.newValue;
                serializedObject.ApplyModifiedProperties();
                TriggerSceneTemplateModified();
            });
            detailElement.Add(descriptionTextField);

            var templateAddToDefaultsProperty = serializedObject.FindProperty(k_TemplateAddToDefaultsPropertyName);
            var defaultTemplateField = new VisualElement();
            defaultTemplateField.style.flexDirection = FlexDirection.Row;
            var addToDefaultsPropertyField = new PropertyField(templateAddToDefaultsProperty, " ");
            addToDefaultsPropertyField.RegisterCallback<ChangeEvent<bool>>(e => TriggerSceneTemplateModified());
            addToDefaultsPropertyField.style.flexShrink = 0;
            defaultTemplateField.Add(addToDefaultsPropertyField);
            var label = new Label("Pin in New Scene Dialog");
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            label.style.flexShrink = 1;
            defaultTemplateField.Add(label);
            detailElement.Add(defaultTemplateField);
            root.Add(CreateFoldoutInspector(detailElement, "Details", "SceneTemplateInspectorDetailsFoldout"));

            // Template thumbnail
            var templateThumbnailProperty = serializedObject.FindProperty(k_TemplateThumbnailPropertyName);
            var thumbnailField = MakeThumbnailField(templateThumbnailProperty, "Texture");
            root.Add(CreateFoldoutInspector(thumbnailField, "Thumbnail", "SceneTemplateInspectorThumbnailFoldout"));

            // SceneTemplatePipeline
            var sceneTemplatePipeline = new VisualElement();
            sceneTemplatePipeline.Add(new HelpBox(k_SceneTemplateInfo, HelpBoxMessageType.Info));

            var pipelineProperty = serializedObject.FindProperty(k_TemplatePipelineName);
            var pipelineField = new PropertyField(pipelineProperty, "Scene Template Pipeline") { name = k_SceneTemplatePipelineName };
            pipelineField.RegisterCallback<ChangeEvent<Object>>(e =>
            {
                if (e.newValue != null && !SceneTemplateAsset.IsValidPipeline(e.newValue as MonoScript))
                {
                    Debug.LogWarning(k_SceneTemplateInfo);
                    pipelineProperty.objectReferenceValue = null;
                    serializedObject.ApplyModifiedProperties();
                }
            });
            sceneTemplatePipeline.Add(pipelineField);
            root.Add(CreateFoldoutInspector(sceneTemplatePipeline, "Scene Template Pipeline", "SceneTemplatePipelineFoldout"));

            // Dependencies
            root.Add(CreateFoldoutInspector(BuildDependencyRows(), "Dependencies", "SceneTemplateDependenciesFoldout"));

            root.Bind(serializedObject);
            return root;
        }

        [UsedImplicitly]
        private void OnEnable()
        {
            UpdateSceneTemplateAsset();
        }

        private static Foldout CreateFoldoutInspector(VisualElement element, string title, string foldoutEditorPref)
        {
            var foldout = new Foldout();
            foldout.value = EditorPrefs.GetBool(foldoutEditorPref, true);
            var toggle = foldout.Q<Toggle>();
            toggle.AddToClassList(Styles.classInspectorFoldoutHeader);
            var titleElement = new Label(title);
            titleElement.AddToClassList(Styles.classInspectorFoldoutHeaderText);
            toggle.Children().First().Add(titleElement);
            foldout.Add(element);

            foldout.RegisterValueChangedCallback(e =>
            {
                EditorPrefs.SetBool(foldoutEditorPref, e.newValue);
            });

            return foldout;
        }

        private VisualElement BuildDependencyRows()
        {
            var dependencies = new VisualElement();
            var dependenciesProperty = serializedObject.FindProperty(k_DependenciesPropertyName);
            m_DependencyHelpBox = new HelpBox(k_DependencyInfo, HelpBoxMessageType.Info);// AddHelpBox(k_DependencyInfo, MessageType.Info, "scene-template-asset-inspector-help-box-dependency");
            dependencies.Add(m_DependencyHelpBox);

            m_DependenciesProperty = new List<SerializedProperty>();
            PopulateDependencyProperties(dependenciesProperty, m_DependenciesProperty);

            m_ZebraList = new ZebraList(m_DependenciesProperty, k_ItemSize, () =>
            {
                var changeAllRowElement = new VisualElement();
                changeAllRowElement.style.flexDirection = FlexDirection.Row;
                dependencies.Add(changeAllRowElement);

                var dependenciesLabelField = new Label("Dependencies") { name = k_DependenciesLabelName };
                dependenciesLabelField.AddToClassList(Styles.classHeaderLabel);
                dependenciesLabelField.AddToClassList(Styles.classUnityBaseField);
                dependenciesLabelField.AddToClassList("scene-template-asset-inspector-dependency-header");
                changeAllRowElement.Add(dependenciesLabelField);

                var cloneLabel = new Label("Clone");
                changeAllRowElement.Add(cloneLabel);
                m_CloneHeaderToggle = new Toggle();
                m_CloneHeaderToggle.SetValueWithoutNotify(AreAllDependenciesCloned());
                m_CloneHeaderToggle.RegisterValueChangedCallback<bool>(evt =>
                {
                    var listContent = m_ZebraList.listView.Q<VisualElement>("unity-content-container");
                    foreach (var row in listContent.Children())
                    {
                        var prop = (SerializedProperty)row.userData;
                        var mode = evt.newValue ? TemplateInstantiationMode.Clone : TemplateInstantiationMode.Reference;
                        SetDependencyInstantiationMode(row, mode);
                    }
                    serializedObject.ApplyModifiedProperties();
                });
                m_CloneHeaderToggle.AddToClassList("scene-template-asset-inspector-dependency-header-clone-column");
                changeAllRowElement.Add(m_CloneHeaderToggle);

                return changeAllRowElement;
            }, CreateDependencyElement, BindDependencyElement);
            m_ZebraList.AddToClassList(Styles.classUnityBaseField);
            m_ZebraList.listView.selectionType = SelectionType.Multiple;
            m_ZebraList.listView.onItemsChosen += OnDoubleClick;
            m_ZebraList.listView.RegisterCallback<KeyUpEvent>(OnKeyUpEvent);
            dependencies.Add(m_ZebraList);
            m_ZebraList.name = k_DependencyListView;
            UpdateDependencyListVisibility();
            return dependencies;
        }

        private bool AreAllDependenciesCloned()
        {
            var sceneTemplateAsset = (SceneTemplateAsset)serializedObject.targetObject;
            return sceneTemplateAsset.dependencies
                .Where(dep => SceneTemplateProjectSettings.Get().GetDependencyInfo(dep.dependency).supportsModification)
                .All(dep => dep.instantiationMode == TemplateInstantiationMode.Clone);
        }

        private static void OnDoubleClick(IEnumerable<object> objs)
        {
            var obj = objs.FirstOrDefault();
            var property = obj as SerializedProperty;
            if (property == null)
                return;

            var depProperty = property.FindPropertyRelative(k_DependencyPropertyName);
            if (depProperty.objectReferenceValue != null)
            {
                EditorGUIUtility.PingObject(depProperty.objectReferenceValue);
            }
        }

        private void OnKeyUpEvent(KeyUpEvent e)
        {
            if (e.keyCode == KeyCode.Space)
            {
                if (m_ZebraList.listView.selectedIndex == -1)
                    return;

                // If there is any value that is not set to Clone, set everything to Clone. Otherwise,
                // set everything to Reference.
                var selectedItems = GetSelectedDependencies();
                var allClone = selectedItems.Select(item => item.FindPropertyRelative(k_InstantiationModePropertyName))
                    .All(instantiationModeProperty => instantiationModeProperty.enumValueIndex == (int)TemplateInstantiationMode.Clone);

                var newEnumValue = allClone ? TemplateInstantiationMode.Reference : TemplateInstantiationMode.Clone;
                SyncListSelectionToValue(newEnumValue);

                e.StopPropagation();
            }
        }

        private static VisualElement CreateDependencyElement()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.name = k_DependencyRowElementName;

            var icon = new Image(){ scaleMode = ScaleMode.ScaleAndCrop, pickingMode = PickingMode.Ignore };
            icon.AddToClassList("scene-template-asset-inspector-dependency-row-icon");
            row.Add(icon);

            var label = new Label();
            label.AddToClassList("scene-template-asset-inspector-dependency-row-label");
            row.Add(label);

            var cloneToggle = new Toggle();
            cloneToggle.AddToClassList("scene-template-asset-inspector-dependency-row-clone-toggle");
            row.Add(cloneToggle);

            return row;
        }

        void BindDependencyElement(VisualElement el, int modelIndex)
        {
            var property = m_DependenciesProperty[modelIndex];
            var icon = (Image)el.ElementAt(0);
            var label = (Label)el.ElementAt(1);
            var depProperty = property.FindPropertyRelative(k_DependencyPropertyName);
            var content = EditorGUIUtility.ObjectContent(depProperty.objectReferenceValue, depProperty.objectReferenceValue.GetType());
            icon.image = content.image;
            label.text = content.text;
            el.userData = property;

            var instantiationModeProperty = property.FindPropertyRelative(k_InstantiationModePropertyName);
            var cloneToggle = (Toggle)el.ElementAt(2);
            cloneToggle.value = IsCloning(instantiationModeProperty);
            cloneToggle.SetEnabled(SceneTemplateProjectSettings.Get().GetDependencyInfo(depProperty.objectReferenceValue).supportsModification);
            cloneToggle.RegisterValueChangedCallback<bool>(evt =>
            {
                if (evt.newValue == IsCloning(instantiationModeProperty))
                    return;
                var newInstantiationType = (evt.newValue ? TemplateInstantiationMode.Clone : TemplateInstantiationMode.Reference);
                instantiationModeProperty.enumValueIndex = (int)newInstantiationType;

                // Sync Selection if the dependency is part of it:
                if (m_ZebraList.listView.selectedIndices.Contains(modelIndex))
                    SyncListSelectionToValue(newInstantiationType);
                serializedObject.ApplyModifiedProperties();

                m_CloneHeaderToggle.SetValueWithoutNotify(AreAllDependenciesCloned());
            });
        }

        private void SyncListSelectionToValue(TemplateInstantiationMode mode)
        {
            if (m_ZebraList.listView.selectedIndex != -1)
            {
                var listContent = m_ZebraList.listView.Q<VisualElement>("unity-content-container");
                foreach (var row in listContent.Children())
                {
                    var prop = (SerializedProperty)row.userData;
                    if (row.ClassListContains("unity-list-view__item--selected"))
                    {
                        SetDependencyInstantiationMode(row, mode);
                    }
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void SetDependencyInstantiationMode(VisualElement row, TemplateInstantiationMode mode)
        {
            var prop = (SerializedProperty)row.userData;
            var toggle = (Toggle)row.ElementAt(2);
            if (!toggle.enabledSelf)
                return;

            toggle.SetValueWithoutNotify(mode == TemplateInstantiationMode.Clone);

            var depProp = prop.FindPropertyRelative(k_InstantiationModePropertyName);
            if (depProp.enumValueIndex != (int)mode)
            {
                depProp.enumValueIndex = (int)mode;
            }
        }

        private IEnumerable<SerializedProperty> GetSelectedDependencies()
        {
            var selectedItems = new List<SerializedProperty>();
            if (m_ZebraList.listView.selectedIndex != -1)
            {
                var listContent = m_ZebraList.listView.Q<VisualElement>("unity-content-container");
                foreach (var row in listContent.Children())
                {
                    var prop = (SerializedProperty)row.userData;
                    if (row.ClassListContains("unity-list-view__item--selected"))
                    {
                        selectedItems.Add(prop);
                    }
                }
            }
            return selectedItems;
        }

        static bool IsCloning(SerializedProperty prop)
        {
            var instantiationMode = (TemplateInstantiationMode)prop.enumValueIndex;
            return instantiationMode == TemplateInstantiationMode.Clone;
        }

        private void RebuildDependencies(VisualElement root)
        {
            var sceneTemplateAsset = (SceneTemplateAsset)serializedObject.targetObject;
            sceneTemplateAsset.BindScene(sceneTemplateAsset.templateScene);
            serializedObject.Update();
            var dependenciesProperty = serializedObject.FindProperty(k_DependenciesPropertyName);
            m_DependenciesProperty.Clear();
            PopulateDependencyProperties(dependenciesProperty, m_DependenciesProperty);
            m_ZebraList.listView.Refresh();
            UpdateDependencyListVisibility();
        }

        private void UpdateDependencyListVisibility()
        {
            m_ZebraList.style.height = (m_DependenciesProperty.Count + 1) * k_ItemSize + 2;
            m_DependencyHelpBox.visible = m_DependenciesProperty.Count > 0;
            m_ZebraList.visible = m_DependenciesProperty.Count > 0;
        }

        private static void PopulateDependencyProperties(SerializedProperty rootProperty, List<SerializedProperty> model)
        {
            var numDependency = rootProperty.arraySize;
            for (var i = 0; i < numDependency; ++i)
            {
                var depInfoProperty = rootProperty.GetArrayElementAtIndex(i);
                var depProperty = depInfoProperty.FindPropertyRelative(k_DependencyPropertyName);
                if (!SceneTemplateProjectSettings.Get().GetDependencyInfo(depProperty.objectReferenceValue).ignore)
                {
                    model.Add(depInfoProperty);
                }
            }
        }

        private void UpdateSceneTemplateAsset()
        {
            var sceneTemplateAsset = (SceneTemplateAsset)serializedObject.targetObject;
            sceneTemplateAsset.UpdateDependencies();
            serializedObject.Update();
        }

        private VisualElement MakeThumbnailField(SerializedProperty thumbnailProperty, string label)
        {
            var propertyElement = new VisualElement();
            var thumbnailObjectField = new PropertyField(thumbnailProperty, label);
            propertyElement.Add(thumbnailObjectField);

            m_PreviewArea = new SceneTemplatePreviewArea(k_ThumbnailAreaName, thumbnailProperty.objectReferenceValue as Texture2D, "No preview available");
            var previewAreaElement = m_PreviewArea.Element;
            previewAreaElement.AddToClassList(Styles.classUnityBaseField);
            propertyElement.Add(previewAreaElement);

            thumbnailObjectField.RegisterCallback<ChangeEvent<Object>>(e =>
            {
                m_PreviewArea.UpdatePreview(e.newValue as Texture2D);
                TriggerSceneTemplateModified();
            });

            // Snapshot header row
            var snapshotHeaderRowElement = new VisualElement();
            snapshotHeaderRowElement.AddToClassList(Styles.classUnityBaseField);
            propertyElement.Add(snapshotHeaderRowElement);
            var snapshotHeaderLabel = new Label("Snapshot");
            snapshotHeaderLabel.AddToClassList(Styles.classUnityLabel);
            snapshotHeaderRowElement.Add(snapshotHeaderLabel);

            // Snapshot button with dropdown
            var cameraNames = Camera.allCameras.Select(c => new SnapshotTargetInfo { Name = c.name, OnSnapshotAction = TakeSnapshotFromCamera }).ToList();
            cameraNames.Add(new SnapshotTargetInfo()); // Separator
            cameraNames.Add(new SnapshotTargetInfo { Name = "Game View", OnSnapshotAction = (info, property) => TakeSnapshotFromGameView(property) });
            var snapshotTargetPopup = new PopupField<SnapshotTargetInfo>("View", cameraNames, Camera.allCameras.Length == 0 ? 1 : 0);
            snapshotTargetPopup.formatListItemCallback = info => info.Name;
            snapshotTargetPopup.formatSelectedValueCallback = info => info.Name;
            propertyElement.Add(snapshotTargetPopup);

            var snapshotSecondRowElement = new VisualElement() { name = k_SnapshotRowName };
            snapshotSecondRowElement.AddToClassList(Styles.classUnityBaseField);
            propertyElement.Add(snapshotSecondRowElement);
            var emptyLabel = new VisualElement();
            emptyLabel.AddToClassList(Styles.classUnityBaseFieldLabel);
            snapshotSecondRowElement.Add(emptyLabel);
            var snapshotButton = new Button(() =>
            {
                var targetInfo = snapshotTargetPopup.value;
                if (targetInfo.OnSnapshotAction == null)
                    return;
                targetInfo.OnSnapshotAction(targetInfo, thumbnailProperty);
            });
            snapshotButton.tooltip = k_SnapshotTooltip;
            snapshotButton.text = k_SnapshotButtonLabel;
            snapshotButton.AddToClassList(Styles.classUnityBaseFieldInput);
            snapshotSecondRowElement.Add(snapshotButton);

            return propertyElement;
        }

        private void TakeSnapshotFromCamera(SnapshotTargetInfo targetInfo, SerializedProperty thumbnailProperty)
        {
            var sceneTemplateAsset = serializedObject.targetObject as SceneTemplateAsset;
            if (!sceneTemplateAsset)
                return;

            var camera = Camera.allCameras.FirstOrDefault(c => c.name == targetInfo.Name);

            if (!camera)
            {
                Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, $"There is no camera named {targetInfo.Name} in the current scene.");
                return;
            }

            var snapshotTexture = SnapshotUtils.TakeCameraSnapshot(camera);
            snapshotTexture.name = sceneTemplateAsset.name;

            // Put the snapshot into the scene template asset
            // This needs to be done before we set it into the property
            // and apply modifications.
            sceneTemplateAsset.AddThumbnailToAsset(snapshotTexture);

            thumbnailProperty.objectReferenceValue = snapshotTexture;
            serializedObject.ApplyModifiedProperties();
        }

        private void TakeSnapshotFromSceneCamera(SerializedProperty thumbnailProperty)
        {
            var sceneTemplateAsset = serializedObject.targetObject as SceneTemplateAsset;
            if (!sceneTemplateAsset)
                return;

            var sceneView = EditorWindow.GetWindow<SceneView>();
            if (sceneView == null)
            {
                Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, "Unable to retrieve the Scene view.");
                return;
            }

            SnapshotUtils.TakeSceneViewSnapshot(sceneView, snapshotTexture =>
            {
                snapshotTexture.name = sceneTemplateAsset.name;

                // Put the snapshot into the scene template asset
                // This needs to be done before we set it into the property
                // and apply modifications.
                sceneTemplateAsset.AddThumbnailToAsset(snapshotTexture);

                thumbnailProperty.objectReferenceValue = snapshotTexture;
                serializedObject.ApplyModifiedProperties();
            });
        }

        private static Texture2D Decompress(Texture2D source)
        {
            var uncompressedTexture = new Texture2D(source.width, source.height);
            if (source.isReadable)
            {
                // This supposes that the source is readable
                var pixels = source.GetPixels32();
                uncompressedTexture.SetPixels32(pixels);
                uncompressedTexture.Apply();
            }
            else
            {
                var savedRT = RenderTexture.active;
                var savedViewport = ShaderUtil.rawViewportRect;

                var tmp = RenderTexture.GetTemporary(
                    source.width, source.height,
                    0,
                    SystemInfo.GetGraphicsFormat(DefaultFormat.LDR));
                var mat = EditorGUI.GetMaterialForSpecialTexture(source, null, QualitySettings.activeColorSpace == ColorSpace.Linear);
                if (mat != null)
                    Graphics.Blit(source, tmp, mat);
                else Graphics.Blit(source, tmp);

                RenderTexture.active = tmp;
                uncompressedTexture = new Texture2D(source.width, source.height, source.alphaIsTransparency ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
                uncompressedTexture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                uncompressedTexture.Apply();
                RenderTexture.ReleaseTemporary(tmp);

                EditorGUIUtility.SetRenderTextureNoViewport(savedRT);
                ShaderUtil.rawViewportRect = savedViewport;
            }

            return uncompressedTexture;
        }

        private void TakeSnapshotFromGameView(SerializedProperty thumbnailProperty)
        {
            var sceneTemplateAsset = serializedObject.targetObject as SceneTemplateAsset;
            if (!sceneTemplateAsset)
                return;

            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            var gameView = EditorWindow.GetWindow(gameViewType);
            if (gameView == null)
            {
                Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, "Unable to retrieve the Game view.");
                return;
            }

            SnapshotUtils.TakeGameViewSnapshot(gameView, textureCopy =>
            {
                textureCopy.name = sceneTemplateAsset.name;

                // Put the snapshot into the scene template asset
                // This needs to be done before we set it into the property
                // and apply modifications.
                sceneTemplateAsset.AddThumbnailToAsset(textureCopy);
                thumbnailProperty.objectReferenceValue = textureCopy;
                serializedObject.ApplyModifiedProperties();
            });
        }

        private void TriggerSceneTemplateModified()
        {
            sceneTemplateAssetModified?.Invoke(serializedObject.targetObject as SceneTemplateAsset);
        }
    }
}
