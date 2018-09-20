// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;
using UnityEditor.U2D;
using System.Collections.Generic;
using UnityEditor.Experimental.U2D;
using UnityEditor.U2D.Interface;
using UnityEngine.U2D.Interface;
using UnityTexture2D = UnityEngine.Texture2D;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor
{
    internal class SpriteEditorWindow : SpriteUtilityWindow, ISpriteEditor
    {
        private class SpriteEditorWindowStyles
        {
            public static readonly GUIContent editingDisableMessageLabel = EditorGUIUtility.TrTextContent("Editing is disabled during play mode");
            public static readonly GUIContent revertButtonLabel = EditorGUIUtility.TrTextContent("Revert");
            public static readonly GUIContent applyButtonLabel = EditorGUIUtility.TrTextContent("Apply");

            public static readonly GUIContent spriteEditorWindowTitle = EditorGUIUtility.TrTextContent("Sprite Editor");

            public static readonly GUIContent pendingChangesDialogContent = EditorGUIUtility.TrTextContent("The asset was modified outside of Sprite Editor Window.\nDo you want to apply pending changes?");

            public static readonly GUIContent applyRevertDialogTitle = EditorGUIUtility.TrTextContent("Unapplied import settings");
            public static readonly GUIContent applyRevertDialogContent = EditorGUIUtility.TrTextContent("Unapplied import settings for '{0}'");

            public static readonly GUIContent noSelectionWarning = EditorGUIUtility.TrTextContent("No texture or sprite selected");
            public static readonly GUIContent noModuleWarning = EditorGUIUtility.TrTextContent("No Sprite Editor module available");
            public static readonly GUIContent applyRevertModuleDialogTitle = EditorGUIUtility.TrTextContent("Unapplied module changes");
            public static readonly GUIContent applyRevertModuleDialogContent = EditorGUIUtility.TrTextContent("You have unapplied changes from the current module");

            public static readonly GUIContent loadProgressTitle = EditorGUIUtility.TrTextContent("Loading");
            public static readonly GUIContent loadContentText = EditorGUIUtility.TrTextContent("Loading Sprites {0}/{1}");
        }

        private const float k_MarginForFraming = 0.05f;
        private const float k_WarningMessageWidth = 250f;
        private const float k_WarningMessageHeight = 40f;
        private const float k_ModuleListWidth = 90f;

        public static SpriteEditorWindow s_Instance;
        public bool m_ResetOnNextRepaint;

        private List<SpriteRect> m_RectsCache;
        ISpriteEditorDataProvider m_SpriteDataProvider;

        private bool m_RequestRepaint = false;

        public static bool s_OneClickDragStarted = false;
        public string m_SelectedAssetPath;

        private IEventSystem m_EventSystem;
        private IUndoSystem m_UndoSystem;
        private IAssetDatabase m_AssetDatabase;
        private IGUIUtility m_GUIUtility;
        private UnityTexture2D m_OutlineTexture;
        private UnityTexture2D m_ReadableTexture;
        private Dictionary<Type, RequireSpriteDataProviderAttribute> m_ModuleRequireSpriteDataProvider = new Dictionary<Type, RequireSpriteDataProviderAttribute>();

        private IMGUIContainer m_ToolbarIMGUIElement;
        private IMGUIContainer m_MainViewIMGUIElement;
        private VisualElement m_ModuleViewElement;
        private VisualElement m_MainViewElement;

        [SerializeField]
        private string m_SelectedSpriteRectGUID;

        public static void GetWindow()
        {
            EditorWindow.GetWindow<SpriteEditorWindow>();
        }

        public SpriteEditorWindow()
        {
            m_EventSystem = new EventSystem();
            m_UndoSystem = new UndoSystem();
            m_AssetDatabase = new AssetDatabaseSystem();
            m_GUIUtility = new GUIUtilitySystem();
        }

        void ModifierKeysChanged()
        {
            if (EditorWindow.focusedWindow == this)
            {
                Repaint();
            }
        }

        private void OnFocus()
        {
            if (selectedProviderChanged)
                OnSelectionChange();
        }

        public void RefreshPropertiesCache()
        {
            m_SpriteDataProvider = AssetImporter.GetAtPath(m_SelectedAssetPath) as ISpriteEditorDataProvider;
            if (!IsSpriteDataProviderValid())
            {
                m_SelectedAssetPath = "";
                return;
            }


            m_SpriteDataProvider.InitSpriteEditorDataProvider();

            var textureProvider = m_SpriteDataProvider.GetDataProvider<ITextureDataProvider>();
            if (textureProvider != null)
            {
                int width = 0, height = 0;
                textureProvider.GetTextureActualWidthAndHeight(out width, out height);
                m_Texture = textureProvider.previewTexture == null ? null : new PreviewTexture2D(textureProvider.previewTexture, width, height);
            }
        }

        internal string GetSelectionAssetPath()
        {
            var selection = Selection.activeObject;

            if (Selection.activeGameObject)
            {
                var spriteRenderer = Selection.activeGameObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                    selection = spriteRenderer.sprite;
            }

            return m_AssetDatabase.GetAssetPath(selection);
        }

        public void InvalidatePropertiesCache()
        {
            m_RectsCache = null;
            m_SpriteDataProvider = null;
        }

        private Rect warningMessageRect
        {
            get
            {
                return new Rect(
                    position.width - k_WarningMessageWidth - k_InspectorWindowMargin - k_ScrollbarMargin,
                    k_InspectorWindowMargin + k_ScrollbarMargin,
                    k_WarningMessageWidth,
                    k_WarningMessageHeight);
            }
        }

        public SpriteImportMode spriteImportMode
        {
            get { return !IsSpriteDataProviderValid() ? SpriteImportMode.None : m_SpriteDataProvider.spriteImportMode; }
        }

        bool activeDataProviderSelected
        {
            get { return m_SpriteDataProvider != null; }
        }

        public bool textureIsDirty
        {
            get; set;
        }

        public bool selectedProviderChanged
        {
            get
            {
                var assetPath = GetSelectionAssetPath();
                var ai = AssetImporter.GetAtPath(assetPath);
                var dataProvider = ai as ISpriteEditorDataProvider;
                return dataProvider != null && m_SelectedAssetPath != assetPath;
            }
        }

        public bool IsEditingDisabled()
        {
            return EditorApplication.isPlayingOrWillChangePlaymode;
        }

        void OnSelectionChange()
        {
            // In case of changed of texture/sprite or selected on non texture object
            bool updateModules = false;
            if (selectedProviderChanged)
            {
                HandleApplyRevertDialog(SpriteEditorWindowStyles.applyRevertDialogTitle.text,
                    String.Format(SpriteEditorWindowStyles.applyRevertDialogContent.text, m_SelectedAssetPath));
                m_SelectedAssetPath = GetSelectionAssetPath();
                ResetWindow();
                RefreshPropertiesCache();
                RefreshRects();
                updateModules = true;
            }

            if (m_RectsCache != null)
            {
                UpdateSelectedSpriteRectFromSelection();
            }

            // We only update modules when data provider changed
            if (updateModules)
                UpdateAvailableModules();
            Repaint();
        }

        private void UpdateSelectedSpriteRectFromSelection()
        {
            if (Selection.activeObject is Sprite)
            {
                UpdateSelectedSpriteRect(Selection.activeObject as Sprite);
            }
            else if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<SpriteRenderer>())
            {
                Sprite sprite = Selection.activeGameObject.GetComponent<SpriteRenderer>().sprite;
                UpdateSelectedSpriteRect(sprite);
            }
        }

        public void ResetWindow()
        {
            InvalidatePropertiesCache();
            textureIsDirty = false;
            m_Zoom = -1;
            m_ScrollPosition = Vector2.zero;
        }

        void OnEnable()
        {
            minSize = new Vector2(360, 200);
            titleContent = SpriteEditorWindowStyles.spriteEditorWindowTitle;
            s_Instance = this;

            m_UndoSystem.RegisterUndoCallback(UndoRedoPerformed);
            EditorApplication.modifierKeysChanged += ModifierKeysChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += OnEditorApplicationQuit;

            if (selectedProviderChanged)
                m_SelectedAssetPath = GetSelectionAssetPath();

            ResetWindow();
            RefreshPropertiesCache();
            bool noSelectedSprite = string.IsNullOrEmpty(m_SelectedSpriteRectGUID);
            RefreshRects();
            if (noSelectedSprite)
                UpdateSelectedSpriteRectFromSelection();

            SetupVisualElements();
            InitModules();
        }

        private void SetupVisualElements()
        {
            m_ToolbarIMGUIElement = new IMGUIContainer(DoToolbarGUI)
            {
                name = "spriteEditorWindowToolbar",
            };
            m_MainViewIMGUIElement = new IMGUIContainer(DoTextureAndModulesGUI)
            {
                name = "mainViewIMGUIElement"
            };
            m_MainViewElement = new VisualElement()
            {
                name = "spriteEditorWindowMainView",
            };
            m_ModuleViewElement = new VisualElement()
            {
                name = "moduleViewElement",
                pickingMode = PickingMode.Ignore
            };
            m_MainViewElement.Add(m_MainViewIMGUIElement);
            m_MainViewElement.Add(m_ModuleViewElement);
            var root = this.GetRootVisualContainer();
            root.AddStyleSheetPath("StyleSheets/SpriteEditor/SpriteEditor.uss");
            root.Add(m_ToolbarIMGUIElement);
            root.Add(m_MainViewElement);
        }

        private void UndoRedoPerformed()
        {
            // Was selected texture changed by undo?
            if (selectedProviderChanged)
                OnSelectionChange();

            InitSelectedSpriteRect();

            Repaint();
        }

        private void InitSelectedSpriteRect()
        {
            SpriteRect newSpriteRect = null;
            if (m_RectsCache != null && m_RectsCache.Count > 0)
            {
                if (selectedSpriteRect != null)
                    newSpriteRect = m_RectsCache.FirstOrDefault(x => x.spriteID == selectedSpriteRect.spriteID) != null ? selectedSpriteRect : m_RectsCache[0];
                else
                    newSpriteRect = m_RectsCache[0];
            }

            selectedSpriteRect = newSpriteRect;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            HandleApplyRevertDialog(SpriteEditorWindowStyles.applyRevertDialogTitle.text,
                String.Format(SpriteEditorWindowStyles.applyRevertDialogContent.text, m_SelectedAssetPath));
            InvalidatePropertiesCache();
            EditorApplication.modifierKeysChanged -= ModifierKeysChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.quitting -= OnEditorApplicationQuit;
            s_Instance = null;

            if (m_OutlineTexture != null)
            {
                DestroyImmediate(m_OutlineTexture);
                m_OutlineTexture = null;
            }

            if (m_ReadableTexture)
            {
                DestroyImmediate(m_ReadableTexture);
                m_ReadableTexture = null;
            }

            if (m_CurrentModule != null)
                m_CurrentModule.OnModuleDeactivate();
        }

        void OnPlayModeStateChanged(PlayModeStateChange playModeState)
        {
            if (PlayModeStateChange.EnteredPlayMode == playModeState || PlayModeStateChange.EnteredEditMode == playModeState)
            {
                RebuildCache();
            }
        }

        void OnEditorApplicationQuit()
        {
            HandleApplyRevertDialog(SpriteEditorWindowStyles.applyRevertDialogTitle.text,
                String.Format(SpriteEditorWindowStyles.applyRevertDialogContent.text, m_SelectedAssetPath));
        }

        void HandleApplyRevertDialog(string dialogTitle, string dialogContent)
        {
            if (textureIsDirty && IsSpriteDataProviderValid())
            {
                if (EditorUtility.DisplayDialog(dialogTitle, dialogContent,
                    SpriteEditorWindowStyles.applyButtonLabel.text, SpriteEditorWindowStyles.revertButtonLabel.text))
                    DoApply();
                else
                    DoRevert();

                SetupModule(m_CurrentModuleIndex);
            }
        }

        bool IsSpriteDataProviderValid()
        {
            return m_SpriteDataProvider != null && !m_SpriteDataProvider.Equals(null);
        }

        void RefreshRects()
        {
            m_RectsCache = null;
            if (IsSpriteDataProviderValid())
            {
                m_RectsCache = m_SpriteDataProvider.GetSpriteRects().ToList();
            }

            InitSelectedSpriteRect();
        }

        private void Update()
        {
            if (m_ResetOnNextRepaint || selectedProviderChanged)
            {
                if (selectedProviderChanged || !IsSpriteDataProviderValid())
                    m_SelectedAssetPath = GetSelectionAssetPath();
                RebuildCache();
            }
        }

        private void RebuildCache()
        {
            m_ResetOnNextRepaint = false;
            HandleApplyRevertDialog(SpriteEditorWindowStyles.applyRevertDialogTitle.text, SpriteEditorWindowStyles.pendingChangesDialogContent.text);
            ResetWindow();
            RefreshPropertiesCache();
            RefreshRects();
            UpdateAvailableModules();
        }

        private void DoTextureAndModulesGUI()
        {
            InitStyles();
            if (!activeDataProviderSelected)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    GUILayout.Label(SpriteEditorWindowStyles.noSelectionWarning);
                }
                return;
            }
            if (m_CurrentModule == null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    GUILayout.Label(SpriteEditorWindowStyles.noModuleWarning);
                }
                return;
            }
            m_TextureViewRect = new Rect(0f, 0f, m_MainViewIMGUIElement.layout.width - k_ScrollbarMargin, m_MainViewIMGUIElement.layout.height - k_ScrollbarMargin);
            Matrix4x4 oldHandlesMatrix = Handles.matrix;
            DoTextureGUI();
            // Warning message if applicable
            DoEditingDisabledMessage();
            m_CurrentModule.DoPostGUI();
            Handles.matrix = oldHandlesMatrix;
            if (m_RequestRepaint == true)
            {
                Repaint();
                m_RequestRepaint = false;
            }
        }

        protected override void DoTextureGUIExtras()
        {
            HandleFrameSelected();

            if (m_EventSystem.current.type == EventType.Repaint)
            {
                SpriteEditorUtility.BeginLines(new Color(1f, 1f, 1f, 0.5f));
                var selectedRect = selectedSpriteRect?.spriteID;
                for (int i = 0; i < m_RectsCache.Count; i++)
                {
                    if (m_RectsCache[i].spriteID != selectedRect)
                        SpriteEditorUtility.DrawBox(m_RectsCache[i].rect);
                }
                SpriteEditorUtility.EndLines();
            }

            m_CurrentModule.DoMainGUI();
        }

        private void DoToolbarGUI()
        {
            InitStyles();

            GUIStyle toolBarStyle = EditorStyles.toolbar;

            Rect toolbarRect = new Rect(0, 0, position.width, k_ToolbarHeight);
            if (m_EventSystem.current.type == EventType.Repaint)
            {
                toolBarStyle.Draw(toolbarRect, false, false, false, false);
            }

            if (!activeDataProviderSelected || m_CurrentModule == null)
                return;
            // Top menu bar

            // only show popup if there is more than 1 module.
            if (m_RegisteredModules.Count > 1)
            {
                float moduleWidthPercentage = k_ModuleListWidth / minSize.x;
                float moduleListWidth = position.width > minSize.x ? position.width * moduleWidthPercentage : k_ModuleListWidth;
                moduleListWidth = Mathf.Min(moduleListWidth, EditorStyles.popup.CalcSize(m_RegisteredModuleNames[m_CurrentModuleIndex]).x);
                int module = EditorGUI.Popup(new Rect(0, 0, moduleListWidth, k_ToolbarHeight), m_CurrentModuleIndex, m_RegisteredModuleNames, EditorStyles.toolbarPopup);
                if (module != m_CurrentModuleIndex)
                {
                    if (textureIsDirty)
                    {
                        // Have pending module edit changes. Ask user if they want to apply or revert
                        if (EditorUtility.DisplayDialog(SpriteEditorWindowStyles.applyRevertModuleDialogTitle.text,
                            SpriteEditorWindowStyles.applyRevertModuleDialogContent.text,
                            SpriteEditorWindowStyles.applyButtonLabel.text, SpriteEditorWindowStyles.revertButtonLabel.text))
                            DoApply();
                        else
                            DoRevert();
                    }
                    m_LastUsedModuleTypeName = m_RegisteredModules[module].GetType().FullName;
                    SetupModule(module);
                }
                toolbarRect.x = moduleListWidth;
            }

            toolbarRect  = DoAlphaZoomToolbarGUI(toolbarRect);

            Rect applyRevertDrawArea = toolbarRect;
            applyRevertDrawArea.x = applyRevertDrawArea.width;

            using (new EditorGUI.DisabledScope(!textureIsDirty))
            {
                applyRevertDrawArea.width = EditorStyles.toolbarButton.CalcSize(SpriteEditorWindowStyles.applyButtonLabel).x;
                applyRevertDrawArea.x -= applyRevertDrawArea.width;
                if (GUI.Button(applyRevertDrawArea, SpriteEditorWindowStyles.applyButtonLabel, EditorStyles.toolbarButton))
                {
                    DoApply();
                    SetupModule(m_CurrentModuleIndex);
                }

                applyRevertDrawArea.width = EditorStyles.toolbarButton.CalcSize(SpriteEditorWindowStyles.revertButtonLabel).x;
                applyRevertDrawArea.x -= applyRevertDrawArea.width;
                if (GUI.Button(applyRevertDrawArea, SpriteEditorWindowStyles.revertButtonLabel, EditorStyles.toolbarButton))
                {
                    DoRevert();
                    SetupModule(m_CurrentModuleIndex);
                }
            }

            toolbarRect.width = applyRevertDrawArea.x - toolbarRect.x;
            m_CurrentModule.DoToolbarGUI(toolbarRect);
        }

        private void DoEditingDisabledMessage()
        {
            if (IsEditingDisabled())
            {
                GUILayout.BeginArea(warningMessageRect);
                EditorGUILayout.HelpBox(SpriteEditorWindowStyles.editingDisableMessageLabel.text, MessageType.Warning);
                GUILayout.EndArea();
            }
        }

        private void DoApply()
        {
            bool reimport = true;
            if (m_CurrentModule != null)
                reimport = m_CurrentModule.ApplyRevert(true);
            m_SpriteDataProvider.Apply();

            // Do this so that asset change save dialog will not show
            var originalValue = EditorPrefs.GetBool("VerifySavingAssets", false);
            EditorPrefs.SetBool("VerifySavingAssets", false);
            AssetDatabase.ForceReserializeAssets(new[] {m_SelectedAssetPath}, ForceReserializeAssetsOptions.ReserializeMetadata);
            EditorPrefs.SetBool("VerifySavingAssets", originalValue);

            if (reimport)
                DoTextureReimport(m_SelectedAssetPath);
            Repaint();

            textureIsDirty = false;
            InitSelectedSpriteRect();
        }

        private void DoRevert()
        {
            textureIsDirty = false;
            RefreshRects();
            GUI.FocusControl("");
            if (m_CurrentModule != null)
                m_CurrentModule.ApplyRevert(false);
        }

        public bool HandleSpriteSelection()
        {
            bool changed = false;

            if (m_EventSystem.current.type == EventType.MouseDown && m_EventSystem.current.button == 0 && GUIUtility.hotControl == 0 && !m_EventSystem.current.alt)
            {
                var oldSelected = selectedSpriteRect;

                var triedRect = TrySelect(m_EventSystem.current.mousePosition);
                if (triedRect != oldSelected)
                {
                    Undo.RegisterCompleteObjectUndo(this, "Sprite Selection");

                    selectedSpriteRect = triedRect;
                    changed = true;
                }

                if (selectedSpriteRect != null)
                    s_OneClickDragStarted = true;
                else
                    RequestRepaint();

                if (changed && selectedSpriteRect != null)
                {
                    m_EventSystem.current.Use();
                }
            }

            return changed;
        }

        private void HandleFrameSelected()
        {
            UnityEngine.U2D.Interface.IEvent evt = m_EventSystem.current;

            if ((evt.type == EventType.ValidateCommand || evt.type == EventType.ExecuteCommand)
                && evt.commandName == EventCommandNames.FrameSelected)
            {
                if (evt.type == EventType.ExecuteCommand)
                {
                    // Do not do frame if there is none selected
                    if (selectedSpriteRect == null)
                        return;

                    Rect rect = selectedSpriteRect.rect;

                    // Calculate the require pixel to display the frame, then get the zoom needed.
                    float targetZoom = m_Zoom;
                    if (rect.width < rect.height)
                        targetZoom = m_TextureViewRect.height / (rect.height + m_TextureViewRect.height * k_MarginForFraming);
                    else
                        targetZoom = m_TextureViewRect.width / (rect.width + m_TextureViewRect.width * k_MarginForFraming);

                    // Apply the zoom
                    m_Zoom = targetZoom;

                    // Calculate the scroll values to center the frame
                    m_ScrollPosition.x = (rect.center.x - (m_Texture.width * 0.5f)) * m_Zoom;
                    m_ScrollPosition.y = (rect.center.y - (m_Texture.height * 0.5f)) * m_Zoom * -1.0f;

                    Repaint();
                }

                evt.Use();
            }
        }

        void UpdateSelectedSpriteRect(Sprite sprite)
        {
            if (m_RectsCache == null)
                return;

            var spriteGUID = sprite.GetSpriteID();
            for (int i = 0; i < m_RectsCache.Count; i++)
            {
                if (spriteGUID == m_RectsCache[i].spriteID)
                {
                    selectedSpriteRect = m_RectsCache[i];
                    return;
                }
            }
            selectedSpriteRect = null;
        }

        private SpriteRect TrySelect(Vector2 mousePosition)
        {
            float selectedSize = float.MaxValue;
            SpriteRect currentRect = null;
            mousePosition = Handles.inverseMatrix.MultiplyPoint(mousePosition);

            for (int i = 0; i < m_RectsCache.Count; i++)
            {
                var sr = m_RectsCache[i];
                if (sr.rect.Contains(mousePosition))
                {
                    // If we clicked inside an already selected spriterect, always persist that selection
                    if (sr == selectedSpriteRect)
                        return sr;

                    float width = sr.rect.width;
                    float height = sr.rect.height;
                    float newSize = width * height;
                    if (width > 0f && height > 0f && newSize < selectedSize)
                    {
                        currentRect = sr;
                        selectedSize = newSize;
                    }
                }
            }

            return currentRect;
        }

        public void DoTextureReimport(string path)
        {
            if (m_SpriteDataProvider != null)
            {
                try
                {
                    AssetDatabase.StartAssetEditing();
                    AssetDatabase.ImportAsset(path);
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
        }

        GUIContent[] m_RegisteredModuleNames;
        List<SpriteEditorModuleBase> m_AllRegisteredModules;
        List<SpriteEditorModuleBase> m_RegisteredModules;
        SpriteEditorModuleBase m_CurrentModule = null;
        int m_CurrentModuleIndex = 0;
        [SerializeField]
        string m_LastUsedModuleTypeName;

        internal void SetupModule(int newModuleIndex)
        {
            if (s_Instance == null)
                return;

            m_ModuleViewElement.Clear();
            if (m_RegisteredModules.Count > newModuleIndex)
            {
                m_CurrentModuleIndex = newModuleIndex;
                if (m_CurrentModule != null)
                    m_CurrentModule.OnModuleDeactivate();

                m_CurrentModule = null;

                m_CurrentModule = m_RegisteredModules[newModuleIndex];

                m_CurrentModule.OnModuleActivate();
            }
            if (m_MainViewElement != null)
                m_MainViewElement.MarkDirtyRepaint();
            if (m_ModuleViewElement != null)
                m_ModuleViewElement.MarkDirtyRepaint();
        }

        void UpdateAvailableModules()
        {
            if (m_AllRegisteredModules == null)
                return;
            m_RegisteredModules = new List<SpriteEditorModuleBase>();
            foreach (var module in m_AllRegisteredModules)
            {
                if (module.CanBeActivated())
                {
                    RequireSpriteDataProviderAttribute attribute = null;
                    m_ModuleRequireSpriteDataProvider.TryGetValue(module.GetType(), out attribute);
                    if (attribute == null || attribute.ContainsAllType(m_SpriteDataProvider))
                        m_RegisteredModules.Add(module);
                }
            }

            m_RegisteredModuleNames = new GUIContent[m_RegisteredModules.Count];
            int lastUsedModuleIndex = 0;
            for (int i = 0; i < m_RegisteredModules.Count; i++)
            {
                m_RegisteredModuleNames[i] = new GUIContent(m_RegisteredModules[i].moduleName);
                if (m_RegisteredModules[i].GetType().FullName.Equals(m_LastUsedModuleTypeName))
                {
                    lastUsedModuleIndex = i;
                }
            }

            SetupModule(lastUsedModuleIndex);
        }

        void InitModules()
        {
            m_AllRegisteredModules = new List<SpriteEditorModuleBase>();
            m_ModuleRequireSpriteDataProvider.Clear();

            if (m_OutlineTexture == null)
            {
                m_OutlineTexture = new UnityTexture2D(1, 16, TextureFormat.RGBA32, false);
                m_OutlineTexture.SetPixels(new Color[]
                {
                    new Color(0.5f, 0.5f, 0.5f, 0.5f), new Color(0.5f, 0.5f, 0.5f, 0.5f), new Color(0.8f, 0.8f, 0.8f, 0.8f), new Color(0.8f, 0.8f, 0.8f, 0.8f),
                    Color.white, Color.white, Color.white, Color.white,
                    new Color(.8f, .8f, .8f, 1f), new Color(.5f, .5f, .5f, .8f), new Color(0.3f, 0.3f, 0.3f, 0.5f), new Color(0.3f, .3f, 0.3f, 0.5f),
                    new Color(0.3f, .3f, 0.3f, 0.3f), new Color(0.3f, .3f, 0.3f, 0.3f), new Color(0.1f, 0.1f, 0.1f, 0.1f), new Color(0.1f, .1f, 0.1f, 0.1f)
                });
                m_OutlineTexture.Apply();
                m_OutlineTexture.hideFlags = HideFlags.HideAndDontSave;
            }
            var outlineTexture = new UnityEngine.U2D.Interface.Texture2D(m_OutlineTexture);

            // Add your modules here
            RegisterModule(new SpriteFrameModule(this, m_EventSystem, m_UndoSystem, m_AssetDatabase));
            RegisterModule(new SpritePolygonModeModule(this, m_EventSystem, m_UndoSystem, m_AssetDatabase));
            RegisterModule(new SpriteOutlineModule(this, m_EventSystem, m_UndoSystem, m_AssetDatabase, m_GUIUtility, new ShapeEditorFactory(), outlineTexture));
            RegisterModule(new SpritePhysicsShapeModule(this, m_EventSystem, m_UndoSystem, m_AssetDatabase, m_GUIUtility, new ShapeEditorFactory(), outlineTexture));
            RegisterCustomModules();
            UpdateAvailableModules();
        }

        void RegisterModule(SpriteEditorModuleBase module)
        {
            var type = module.GetType();
            var attributes = type.GetCustomAttributes(typeof(RequireSpriteDataProviderAttribute), false);
            if (attributes.Length == 1)
                m_ModuleRequireSpriteDataProvider.Add(type, (RequireSpriteDataProviderAttribute)attributes[0]);
            m_AllRegisteredModules.Add(module);
        }

        void RegisterCustomModules()
        {
            var type = typeof(SpriteEditorModuleBase);
            foreach (var moduleClassType in EditorAssemblies.SubclassesOf(type))
            {
                if (!moduleClassType.IsAbstract)
                {
                    bool moduleFound = false;
                    foreach (var module in m_AllRegisteredModules)
                    {
                        if (module.GetType() == moduleClassType)
                        {
                            moduleFound = true;
                            break;
                        }
                    }
                    if (!moduleFound)
                    {
                        var constructorType = new Type[0];
                        // Get the public instance constructor that takes ISpriteEditorModule parameter.
                        var constructorInfoObj = moduleClassType.GetConstructor(
                            BindingFlags.Instance | BindingFlags.Public, null,
                            CallingConventions.HasThis, constructorType, null);
                        if (constructorInfoObj != null)
                        {
                            try
                            {
                                var newInstance = constructorInfoObj.Invoke(new object[0]) as SpriteEditorModuleBase;
                                if (newInstance != null)
                                {
                                    newInstance.spriteEditor = this;
                                    RegisterModule(newInstance);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning("Unable to instantiate custom module " + moduleClassType.FullName + ". Exception:" + ex);
                            }
                        }
                        else
                            Debug.LogWarning(moduleClassType.FullName + " does not have a parameterless constructor");
                    }
                }
            }
        }

        internal List<SpriteEditorModuleBase> activatedModules
        {
            get { return m_RegisteredModules; }
        }

        public List<SpriteRect> spriteRects
        {
            set { m_RectsCache = value; }
        }

        public SpriteRect selectedSpriteRect
        {
            get
            {
                // Always return null if editing is disabled to prevent all possible action to selected frame.
                if (editingDisabled || m_RectsCache == null || string.IsNullOrEmpty(m_SelectedSpriteRectGUID))
                    return null;

                var guid = new GUID(m_SelectedSpriteRectGUID);
                return m_RectsCache.FirstOrDefault(x => x.spriteID == guid);
            }
            set
            {
                if (editingDisabled)
                    return;

                var oldSelected = m_SelectedSpriteRectGUID;
                m_SelectedSpriteRectGUID = value?.spriteID.ToString();
                if (oldSelected != m_SelectedSpriteRectGUID)
                {
                    if (m_MainViewIMGUIElement != null)
                        m_MainViewIMGUIElement.MarkDirtyRepaint();
                    if (m_MainViewElement != null)
                    {
                        m_MainViewElement.MarkDirtyRepaint();
                        using (var e = SpriteSelectionChangeEvent.GetPooled())
                        {
                            e.target = m_ModuleViewElement;
                            m_MainViewElement.SendEvent(e);
                        }
                    }
                }
            }
        }

        public ISpriteEditorDataProvider spriteEditorDataProvider
        {
            get { return m_SpriteDataProvider; }
        }

        public bool enableMouseMoveEvent
        {
            set { wantsMouseMove = value; }
        }

        public void RequestRepaint()
        {
            if (focusedWindow != this)
                Repaint();
            else
                m_RequestRepaint = true;
        }

        public void SetDataModified()
        {
            textureIsDirty = true;
        }

        public Rect windowDimension
        {
            get { return m_TextureViewRect; }
        }

        public ITexture2D previewTexture
        {
            get { return m_Texture; }
        }

        public bool editingDisabled
        {
            get { return EditorApplication.isPlayingOrWillChangePlaymode; }
        }

        public void SetPreviewTexture(UnityTexture2D texture, int width, int height)
        {
            m_Texture = new PreviewTexture2D(texture, width, height);
            m_Zoom = -1;
            m_ScrollPosition = Vector2.zero;
        }

        public void ApplyOrRevertModification(bool apply)
        {
            if (apply)
                DoApply();
            else
                DoRevert();
        }

        internal class PreviewTexture2D : UnityEngine.U2D.Interface.Texture2D
        {
            private int m_ActualWidth = 0;
            private int m_ActualHeight = 0;

            public PreviewTexture2D(UnityTexture2D t, int width, int height)
                : base(t)
            {
                m_ActualWidth = width;
                m_ActualHeight = height;
            }

            public override int width
            {
                get { return m_ActualWidth; }
            }

            public override int height
            {
                get { return m_ActualHeight; }
            }
        }

        public T GetDataProvider<T>() where T : class
        {
            return m_SpriteDataProvider?.GetDataProvider<T>();
        }

        public VisualElement GetMainVisualContainer()
        {
            return m_ModuleViewElement;
        }

        static internal void OnTextureReimport(string path)
        {
            if (s_Instance != null && s_Instance.m_SelectedAssetPath == path)
            {
                s_Instance.m_ResetOnNextRepaint = true;
                s_Instance.Repaint();
            }
        }

        [MenuItem("Window/2D/Sprite Editor", false, 0)]
        static private void OpenSpriteEditorWindow()
        {
            SpriteEditorWindow.GetWindow();
        }
    }

    internal class SpriteEditorTexturePostprocessor : AssetPostprocessor
    {
        public override int GetPostprocessOrder()
        {
            return 1;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var deletedAsset in deletedAssets)
                SpriteEditorWindow.OnTextureReimport(deletedAsset);

            foreach (var importedAsset in importedAssets)
                SpriteEditorWindow.OnTextureReimport(importedAsset);
        }
    }

    internal class SpriteSelectionChangeEvent : EventBase<SpriteSelectionChangeEvent>, IPropagatableEvent
    {
    }
}
