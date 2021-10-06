// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.Experimental;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UnityEditor.SceneTemplate
{
    internal class SceneTemplateInfo : IComparable<SceneTemplateInfo>, IEquatable<SceneTemplateInfo>
    {
        public string name;
        public string assetPath;
        public string description;
        public string thumbnailPath;
        public Texture2D thumbnail;
        public Func<bool, bool> onCreateCallback;
        public bool isPinned;
        public bool isReadonly;
        public SceneTemplateAsset sceneTemplate;

        public string ValidPath => string.IsNullOrEmpty(assetPath) ? name : assetPath;

        public bool IsInMemoryScene => string.IsNullOrEmpty(assetPath);

        public int CompareTo(SceneTemplateInfo other)
        {
            return name.CompareTo(other.name);
        }

        public bool Equals(SceneTemplateInfo other)
        {
            if (other == null)
                return false;

            return ValidPath == other.ValidPath;
        }

        public bool Equals(string assetPathToCheck)
        {
            return ValidPath == assetPathToCheck;
        }
    }

    internal class SceneTemplateDialog : EditorWindow
    {
        private List<SceneTemplateInfo> m_SceneTemplateInfos;
        private static readonly GUIContent k_WindowTitle = new GUIContent("New Scene");

        private const string k_KeyPrefix = "SceneTemplateDialog";

        internal const string k_SceneTemplateTitleLabelName = "scene-template-title-label";
        internal const string k_SceneTemplatePathSection = "scene-template-path-section";
        internal const string k_SceneTemplatePathName = "scene-template-path-label";
        internal const string k_SceneTemplateDescriptionSection = "scene-template-description-section";
        internal const string k_SceneTemplateDescriptionName = "scene-template-description-label";
        internal const string k_SceneTemplateThumbnailName = "scene-template-thumbnail-element";
        private const string k_SceneTemplateEditTemplateButtonName = "scene-template-edit-template-button";
        private const string k_SceneTemplateCreateAdditiveButtonName = "scene-template-create-additive-button";

        private const string k_LoadAdditivelyError = "Cannot load an in-memory scene additively while another in-memory scene is loaded. Save the current scene or load a project scene.";
        private const string k_LoadAdditivelyToolTip = "Load the Scene alongside the current one.";

        private SceneTemplateInfo m_LastSelectedTemplate;

        private SceneTemplatePreviewArea m_PreviewArea;
        private GridView m_GridView;
        private HelpBox m_NoUserTemplateHelpBox;
        private VisualSplitter m_Splitter;

        private const int k_ListViewRowHeight = 32;
        internal Texture2D m_DefaultListViewThumbnail;

        private static readonly Vector2 k_DefaultWindowSize = new Vector2(800, 600);
        private static readonly Vector2 k_MinWindowSize = new Vector2(775, 240);

        private delegate void OnButtonCallback(SceneTemplateInfo info);
        private class ButtonInfo
        {
            public Button button;
            public OnButtonCallback callback;
        }
        private List<ButtonInfo> m_Buttons;
        private int m_SelectedButtonIndex = -1;

        public static SceneTemplateDialog ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            SceneTemplateDialog window = GetWindow<SceneTemplateDialog>(true);
            window.titleContent = k_WindowTitle;
            window.minSize = k_MinWindowSize;
            window.Show();
            return window;
        }

        private void ValidatePosition()
        {
            const float tolerance = 0.0001f;
            if (Math.Abs(position.xMin) < tolerance && position.yMin < tolerance)
            {
                position = SceneTemplateUtils.GetMainWindowCenteredPosition(k_DefaultWindowSize);
            }
        }

        [UsedImplicitly]
        private void OnEnable()
        {
            SceneTemplateAssetInspectorWindow.sceneTemplateAssetModified -= OnSceneTemplateAssetModified;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            SceneTemplateAssetInspectorWindow.sceneTemplateAssetModified += OnSceneTemplateAssetModified;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            ValidatePosition();

            using (new EditorPerformanceTracker("SceneTemplateDialog.SetupData"))
                SetupData();

            using (new EditorPerformanceTracker("SceneTemplateDialog.BuildingUI"))
            {
                BuildUI();
            }
        }

        internal void OnEditTemplate(SceneTemplateInfo sceneTemplateInfo)
        {
            if (sceneTemplateInfo == null)
                return;
            if (sceneTemplateInfo.IsInMemoryScene)
                return;

            // Select the asset
            var templateAsset = AssetDatabase.LoadMainAssetAtPath(sceneTemplateInfo.assetPath);
            Selection.SetActiveObjectWithContext(templateAsset, null);

            // Close the dialog
            Close();
        }

        private void BuildUI()
        {
            // Keyboard events need a focusable element to trigger
            rootVisualElement.focusable = true;
            rootVisualElement.RegisterCallback<KeyUpEvent>(e =>
            {
                switch (e.keyCode)
                {
                    case KeyCode.Escape when !docked:
                        Close();
                        break;
                }
            });

            // Load stylesheets
            rootVisualElement.AddToClassList(Styles.unityThemeVariables);
            rootVisualElement.AddToClassList(Styles.sceneTemplateThemeVariables);
            rootVisualElement.AddStyleSheetPath(Styles.k_CommonStyleSheetPath);
            rootVisualElement.AddStyleSheetPath(Styles.variableStyleSheet);

            // Create a container to offset everything nicely inside the window
            {
                var offsetContainer = new VisualElement();
                offsetContainer.AddToClassList(Styles.classOffsetContainer);
                rootVisualElement.Add(offsetContainer);

                // Create a container for the scene templates and description
                {
                    var mainContainer = new VisualElement();
                    mainContainer.style.flexDirection = FlexDirection.Row;
                    mainContainer.AddToClassList(Styles.classMainContainer);
                    offsetContainer.Add(mainContainer);

                    {
                        // Create a container for the scene templates lists(left side)
                        var sceneTemplatesContainer = new VisualElement();
                        sceneTemplatesContainer.AddToClassList(Styles.classTemplatesContainer);
                        sceneTemplatesContainer.AddToClassList(Styles.sceneTemplateDialogBorder);
                        // mainContainer.Add(sceneTemplatesContainer);
                        CreateAllSceneTemplateListsUI(sceneTemplatesContainer);

                        // Create a container for the template description (right side)
                        var descriptionContainer = new VisualElement();
                        descriptionContainer.AddToClassList(Styles.classDescriptionContainer);
                        descriptionContainer.AddToClassList(Styles.classBorder);
                        // mainContainer.Add(descriptionContainer);
                        CreateTemplateDescriptionUI(descriptionContainer);

                        if (EditorPrefs.HasKey(GetKeyName(nameof(m_Splitter))))
                        {
                            var splitterPosition = EditorPrefs.GetFloat(GetKeyName(nameof(m_Splitter)));
                            sceneTemplatesContainer.style.width = splitterPosition;
                        }
                        else
                        {
                            EditorApplication.delayCall += () =>
                            {
                                sceneTemplatesContainer.style.width = position.width * 0.60f;
                            };
                        }
                        m_Splitter = new VisualSplitter(sceneTemplatesContainer, descriptionContainer, FlexDirection.Row);
                        mainContainer.Add(m_Splitter);
                    }
                }

                // Create the button row
                {
                    var buttonRow = new VisualElement();
                    buttonRow.AddToClassList(Styles.sceneTemplateDialogFooter);
                    offsetContainer.Add(buttonRow);
                    buttonRow.style.flexDirection = FlexDirection.Row;

                    var loadAdditiveToggle = new Toggle() { name = k_SceneTemplateCreateAdditiveButtonName, text = "Load additively", tooltip = k_LoadAdditivelyToolTip };
                    buttonRow.Add(loadAdditiveToggle);
                    {
                        // The buttons need to be right-aligned
                        var buttonSection = new VisualElement();
                        buttonSection.style.flexDirection = FlexDirection.RowReverse;
                        buttonSection.AddToClassList(Styles.classButtons);
                        buttonRow.Add(buttonSection);
                        var createSceneButton = new Button(() =>
                        {
                            if (m_LastSelectedTemplate == null)
                                return;
                            OnCreateNewScene(m_LastSelectedTemplate);
                        })
                        { text = "Create", tooltip = "Instantiate a new scene from a template" };
                        createSceneButton.AddToClassList(Styles.classButton);
                        var cancelButton = new Button(Close) { text = "Cancel", tooltip = "Close scene template dialog without instantiating a new scene." };
                        cancelButton.AddToClassList(Styles.classButton);
                        buttonSection.Add(cancelButton);
                        buttonSection.Add(createSceneButton);

                        m_Buttons = new List<ButtonInfo>
                        {
                            new ButtonInfo {button = createSceneButton, callback = OnCreateNewScene},
                            new ButtonInfo {button = cancelButton, callback = info => Close()}
                        };
                        m_SelectedButtonIndex = m_Buttons.FindIndex(bi => bi.button == createSceneButton);
                        UpdateSelectedButton();
                    }
                }
                SetAllElementSequentiallyFocusable(rootVisualElement, false);
            }

            if (m_LastSelectedTemplate != null)
                UpdateTemplateDescriptionUI(m_LastSelectedTemplate);
        }

        [UsedImplicitly]
        private void OnDisable()
        {
            SceneTemplateAssetInspectorWindow.sceneTemplateAssetModified -= OnSceneTemplateAssetModified;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            SceneTemplateService.newSceneTemplateInstantiating -= TemplateInstantiating;
            EditorPrefs.SetFloat(GetKeyName(nameof(m_GridView.sizeLevel)), m_GridView.sizeLevel);
            SceneTemplateAssetInspectorWindow.sceneTemplateAssetModified -= OnSceneTemplateAssetModified;
            if (m_Splitter != null)
            {
                var splitterPosition = m_Splitter.fixedPane.style.width.value.value;
                EditorPrefs.SetFloat(GetKeyName(nameof(m_Splitter)), splitterPosition);
            }
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                Debug.LogWarning("Cannot open new scene while playing.");
                Close();
            }
        }

        private void OnSceneTemplateAssetModified(SceneTemplateAsset asset)
        {
            m_SceneTemplateInfos = SceneTemplateUtils.GetSceneTemplateInfos();
            var lastSelectedTemplateIndex = m_SceneTemplateInfos.IndexOf(m_LastSelectedTemplate);
            if (lastSelectedTemplateIndex == -1)
            {
                SetLastSelectedTemplate(GetDefaultSceneTemplateInfo());
            }
            else
            {
                SetLastSelectedTemplate(m_SceneTemplateInfos[lastSelectedTemplateIndex]);
            }

            RefreshTemplateGridView();

            UpdateTemplateDescriptionUI(m_LastSelectedTemplate);
        }

        private void RefreshTemplateGridView()
        {
            m_GridView.onPinnedChanged -= OnPinnedChanged;
            m_GridView.onSelectionChanged -= OnTemplateListViewSelectionChanged;

            var items = CreateGridViewItems();
            m_GridView.SetItems(items);
            m_GridView.SetPinned(m_SceneTemplateInfos.Where(template => template.isPinned).Select(template => template.GetHashCode()));
            m_GridView.SetSelection(m_LastSelectedTemplate.GetHashCode());

            m_GridView.onPinnedChanged += OnPinnedChanged;
            m_GridView.onSelectionChanged += OnTemplateListViewSelectionChanged;
        }

        private void CreateAllSceneTemplateListsUI(VisualElement rootContainer)
        {
            if (m_SceneTemplateInfos == null)
                return;

            var templateItems = CreateGridViewItems();
            m_GridView = new GridView(templateItems, "Scene Templates in Project", k_ListViewRowHeight, 64, 256, 4f / 3f);
            m_GridView.wrapAroundKeyboardNavigation = true;
            m_GridView.sizeLevel = EditorPrefs.GetFloat(GetKeyName(nameof(m_GridView.sizeLevel)), 128);
            rootContainer.Add(m_GridView);

            m_GridView.SetPinned(m_SceneTemplateInfos.Where(template => template.isPinned).Select(template => template.GetHashCode()));

            m_GridView.onSelectionChanged += OnTemplateListViewSelectionChanged;
            m_GridView.onPinnedChanged += OnPinnedChanged;
            m_GridView.onItemsActivated += objects =>
            {
                var sceneTemplateInfo = objects.First().userData as SceneTemplateInfo;
                if (sceneTemplateInfo == null)
                    return;
                if (m_SelectedButtonIndex != -1)
                    m_Buttons[m_SelectedButtonIndex].callback(sceneTemplateInfo);
            };

            var toSelect = templateItems.FirstOrDefault(item => item.userData.Equals(m_LastSelectedTemplate));
            if (toSelect != null)
            {
                m_GridView.SetSelection(toSelect);
            }
            else
            {
                m_GridView.SetSelection(templateItems.First());
            }

            m_NoUserTemplateHelpBox = new HelpBox("To begin using a template, create a template from an existing scene in your project. Click to see Scene template documentation.", HelpBoxMessageType.Info);
            m_NoUserTemplateHelpBox.AddToClassList(Styles.sceneTemplateNoTemplateHelpBox);
            m_NoUserTemplateHelpBox.RegisterCallback<MouseDownEvent>(e =>
            {
                SceneTemplateUtils.OpenDocumentationUrl();
            });
            m_NoUserTemplateHelpBox.style.display = m_SceneTemplateInfos.All(t => t.IsInMemoryScene) ? DisplayStyle.Flex : DisplayStyle.None;
            m_GridView.Insert(2, m_NoUserTemplateHelpBox);

            EditorApplication.delayCall += () =>
            {
                m_GridView.SetFocus();
            };
        }

        internal static string GetKeyName(string name)
        {
            return $"{k_KeyPrefix}.{name}";
        }

        private IEnumerable<GridView.Item> CreateGridViewItems()
        {
            // What to do with defaults item? auto pin them?
            var defaultThumbnail = EditorGUIUtility.IconContent("d_SceneAsset Icon").image as Texture2D;
            var templateItems = m_SceneTemplateInfos.Select(info =>
            {
                var item = new GridView.Item(info.GetHashCode(), info.name, info.thumbnail ? info.thumbnail : defaultThumbnail, info);
                return item;
            });
            return templateItems;
        }

        private void CreateTemplateDescriptionUI(VisualElement rootContainer)
        {
            rootContainer.style.flexDirection = FlexDirection.Column;

            // Thumbnail container
            m_PreviewArea = new SceneTemplatePreviewArea(k_SceneTemplateThumbnailName, m_LastSelectedTemplate?.thumbnail, "No preview thumbnail available");
            var thumbnailElement = m_PreviewArea.Element;
            rootContainer.Add(thumbnailElement);

            rootContainer.RegisterCallback<GeometryChangedEvent>(evt => UpdatePreviewAreaSize());

            // Text container
            var sceneTitleLabel = new Label();
            sceneTitleLabel.name = k_SceneTemplateTitleLabelName;
            sceneTitleLabel.AddToClassList(Styles.classWrappingText);
            rootContainer.Add(sceneTitleLabel);

            var assetPathSection = new VisualElement();
            assetPathSection.name = k_SceneTemplatePathSection;
            {
                var scenePathLabel = new Label();
                scenePathLabel.name = k_SceneTemplatePathName;
                scenePathLabel.AddToClassList(Styles.classWrappingText);
                assetPathSection.Add(scenePathLabel);

                var editLocateRow = new VisualElement();
                editLocateRow.style.flexDirection = FlexDirection.Row;
                {
                    var scenePathLocate = new Label();
                    scenePathLocate.text = "Locate";
                    scenePathLocate.AddToClassList(Styles.classTextLink);
                    scenePathLocate.RegisterCallback<MouseDownEvent>(e =>
                    {
                        if (string.IsNullOrEmpty(scenePathLabel.text))
                            return;

                        var asset = AssetDatabase.LoadAssetAtPath<SceneTemplateAsset>(scenePathLabel.text);
                        if (!asset)
                            return;

                        EditorApplication.delayCall += () =>
                        {
                            EditorWindow.FocusWindowIfItsOpen(typeof(ProjectBrowser));
                            EditorApplication.delayCall += () => EditorGUIUtility.PingObject(asset);
                        };
                    });
                    editLocateRow.Add(scenePathLocate);

                    editLocateRow.Add(new Label() { text = " | " });

                    var scenePathEdit = new Label();
                    scenePathEdit.name = k_SceneTemplateEditTemplateButtonName;
                    scenePathEdit.text = "Edit";
                    scenePathEdit.AddToClassList(Styles.classTextLink);
                    scenePathEdit.RegisterCallback<MouseDownEvent>(e =>
                    {
                        OnEditTemplate(m_LastSelectedTemplate);
                    });
                    editLocateRow.Add(scenePathEdit);
                }
                assetPathSection.Add(editLocateRow);
            }
            rootContainer.Add(assetPathSection);

            var descriptionSection = new VisualElement();
            descriptionSection.name = k_SceneTemplateDescriptionSection;
            {
                var descriptionLabel = new Label();
                descriptionLabel.AddToClassList(Styles.classHeaderLabel);
                descriptionLabel.text = "Description";
                descriptionSection.Add(descriptionLabel);

                var sceneDescriptionLabel = new Label();
                sceneDescriptionLabel.AddToClassList(Styles.classWrappingText);
                sceneDescriptionLabel.name = k_SceneTemplateDescriptionName;
                descriptionSection.Add(sceneDescriptionLabel);
            }
            rootContainer.Add(descriptionSection);
        }

        private void UpdatePreviewAreaSize(SceneTemplateInfo info = null)
        {
            info = info ?? m_LastSelectedTemplate;
            if (m_PreviewArea == null)
                return;

            if (info == null || info.thumbnail == null || info.thumbnail.height > info.thumbnail.width)
            {
                m_PreviewArea.Element.style.height = Length.Percent(50);
                return;
            }

            var thumbnail = info.thumbnail;
            var aspectRatio = (float)thumbnail.height / (float)thumbnail.width;
            var width = m_PreviewArea.Element.worldBound.width;
            var newHeight = m_PreviewArea.Element.worldBound.width * aspectRatio;
            // Debug.Log($"Preview size: {info.name} width: {info.thumbnail.width} height:{info.thumbnail.height} ratio: {aspectRatio} AreaW: {width} NewHeigth: {newHeight}");
            m_PreviewArea.Element.style.height = newHeight;
        }

        private void UpdateTemplateDescriptionUI(SceneTemplateInfo newSceneTemplateInfo)
        {
            // Text info
            var sceneTemplateTitleLabel = rootVisualElement.Q<Label>(k_SceneTemplateTitleLabelName);
            if (sceneTemplateTitleLabel != null && newSceneTemplateInfo != null)
            {
                sceneTemplateTitleLabel.text = newSceneTemplateInfo.name;
            }

            var sceneTemplatePathLabel = rootVisualElement.Q<Label>(k_SceneTemplatePathName);
            var sceneTemplatePathSection = rootVisualElement.Q(k_SceneTemplatePathSection);
            if (sceneTemplatePathLabel != null && newSceneTemplateInfo != null && sceneTemplatePathSection != null)
            {
                sceneTemplatePathLabel.text = newSceneTemplateInfo.assetPath;
                sceneTemplatePathSection.visible = !string.IsNullOrEmpty(newSceneTemplateInfo.assetPath);
            }

            var sceneTemplateDescriptionLabel = rootVisualElement.Q<Label>(k_SceneTemplateDescriptionName);
            var sceneTemplateDescriptionSection = rootVisualElement.Q(k_SceneTemplateDescriptionSection);
            if (sceneTemplateDescriptionLabel != null && newSceneTemplateInfo != null && sceneTemplateDescriptionSection != null)
            {
                sceneTemplateDescriptionLabel.text = newSceneTemplateInfo.description;
                sceneTemplateDescriptionSection.visible = !string.IsNullOrEmpty(newSceneTemplateInfo.description);
            }

            // Thumbnail
            m_PreviewArea?.UpdatePreview(newSceneTemplateInfo?.thumbnail);
            UpdatePreviewAreaSize(newSceneTemplateInfo);
        }

        private void SetupData()
        {
            m_SceneTemplateInfos = SceneTemplateUtils.GetSceneTemplateInfos();
            LoadSessionPreferences();
            m_DefaultListViewThumbnail = UnityEditorInternal.InternalEditorUtility.FindIconForFile("foo.unity");

            foreach (var builtinTemplateInfo in SceneTemplateUtils.builtinTemplateInfos)
            {
                if (builtinTemplateInfo.thumbnail == null)
                {
                    builtinTemplateInfo.thumbnail = EditorResources.Load<Texture2D>(builtinTemplateInfo.thumbnailPath);
                    Assert.IsNotNull(builtinTemplateInfo.thumbnail);
                }
            }
        }

        private void LoadSessionPreferences()
        {
            var lastTemplateAssetPath = EditorPrefs.GetString(GetKeyName(nameof(m_LastSelectedTemplate)), null);
            if (!string.IsNullOrEmpty(lastTemplateAssetPath))
            {
                m_LastSelectedTemplate = m_SceneTemplateInfos.Find(info => info.Equals(lastTemplateAssetPath));
            }

            if (m_LastSelectedTemplate == null)
            {
                m_LastSelectedTemplate = GetDefaultSceneTemplateInfo();
            }
        }

        private void OnPinnedChanged(GridView.Item item, bool isPinned)
        {
            var info = (SceneTemplateInfo)item.userData;
            if (info.IsInMemoryScene || info.isReadonly)
            {
                SceneTemplateProjectSettings.Get().SetPinState(info.name, isPinned);
            }
            else
            {
                var infoObj = new SerializedObject(info.sceneTemplate);
                var prop = infoObj.FindProperty("addToDefaults");
                prop.boolValue = isPinned;
                infoObj.ApplyModifiedProperties();
                OnSceneTemplateAssetModified(info.sceneTemplate);
            }
        }

        private void OnTemplateListViewSelectionChanged(IEnumerable<GridView.Item> oldSelection, IEnumerable<GridView.Item> newSelection)
        {
            var objList = newSelection.ToList();
            if (objList.Count == 0)
                return;

            var info = objList[0].userData as SceneTemplateInfo;
            if (info == null)
                return;

            UpdateTemplateDescriptionUI(info);

            SetLastSelectedTemplate(info);

            // Enable/Disable Create Additive button
            var createAdditiveButton = rootVisualElement.Q<Button>(k_SceneTemplateCreateAdditiveButtonName);
            createAdditiveButton?.SetEnabled(true);

            if (m_SelectedButtonIndex != -1 && m_Buttons != null && !m_Buttons[m_SelectedButtonIndex].button.enabledSelf)
            {
                SelectNextEnabledButton();
                UpdateSelectedButton();
            }
        }

        private void OnCreateNewScene(SceneTemplateInfo sceneTemplateInfo)
        {
            if (sceneTemplateInfo == null)
                return;

            var loadAdditive = rootVisualElement.Q(k_SceneTemplateCreateAdditiveButtonName) as Toggle;

            SceneTemplateService.newSceneTemplateInstantiating += TemplateInstantiating;
            try
            {
                sceneTemplateInfo.onCreateCallback(loadAdditive.value);
                if (sceneTemplateInfo.IsInMemoryScene)
                    Close();
            }
            finally
            {
                SceneTemplateService.newSceneTemplateInstantiating -= TemplateInstantiating;
            }
        }

        private void TemplateInstantiating(SceneTemplateAsset sceneTemplate, string newSceneOuputPath, bool loadAdditive)
        {
            SceneTemplateService.newSceneTemplateInstantiating -= TemplateInstantiating;
            Close();
        }

        internal static bool HasMultipleScenes()
        {
            return SceneManager.sceneCount > 0;
        }

        private static void CreateCamera(Scene scene, bool isPerspective)
        {
            var cameraObj = ObjectFactory.CreateGameObject(scene, HideFlags.None, L10n.Tr("Main Camera"), new[] { typeof(Camera), typeof(AudioListener) });
            var camera = cameraObj.GetComponent<Camera>();
            camera.depth = -1;
            cameraObj.tag = "MainCamera";

            if (isPerspective)
            {
                camera.transform.position = new Vector3(0, 1, -10);
            }
            else
            {
                camera.transform.position = new Vector3(0, 0, -10);
                camera.orthographic = true;
                camera.clearFlags = CameraClearFlags.SolidColor;
            }
        }

        private void SetLastSelectedTemplate(SceneTemplateInfo info)
        {
            m_LastSelectedTemplate = info;
            EditorPrefs.SetString(GetKeyName(nameof(m_LastSelectedTemplate)), info.ValidPath);
        }

        internal SceneTemplateInfo GetDefaultSceneTemplateInfo()
        {
            if (EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D)
            {
                return SceneTemplateUtils.default2DSceneTemplateInfo;
            }
            else
            {
                return SceneTemplateUtils.default3DSceneTemplateInfo;
            }
        }

        private static void SetAllElementSequentiallyFocusable(VisualElement parent, bool focusable)
        {
            parent.tabIndex = focusable ? 0 : -1;
            foreach (var child in parent.Children())
            {
                SetAllElementSequentiallyFocusable(child, focusable);
            }
        }

        private void UpdateSelectedButton()
        {
            for (var i = 0; i < m_Buttons.Count; i++)
            {
                m_Buttons[i].button.EnableInClassList(Styles.classElementSelected, i == m_SelectedButtonIndex);
            }
        }

        private void SelectNextEnabledButton()
        {
            var nextIndex = (m_SelectedButtonIndex + 1) % m_Buttons.Count;
            while (!m_Buttons[nextIndex].button.enabledSelf)
            {
                nextIndex = (nextIndex + 1) % m_Buttons.Count;
            }
            m_SelectedButtonIndex = nextIndex;
        }
    }
}
