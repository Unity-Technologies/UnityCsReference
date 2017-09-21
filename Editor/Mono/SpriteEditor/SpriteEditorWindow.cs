// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEditorInternal;
using UnityEditor.U2D;
using System.Collections.Generic;
using UnityEditor.Experimental.U2D;
using UnityEditor.U2D.Interface;
using UnityEngine.U2D.Interface;
using UnityTexture2D = UnityEngine.Texture2D;

namespace UnityEditor
{
    internal class SpriteEditorWindow : SpriteUtilityWindow, ISpriteEditor
    {
        private class SpriteEditorWindowStyles
        {
            public static readonly GUIContent editingDisableMessageLabel = EditorGUIUtility.TextContent("Editing is disabled during play mode");
            public static readonly GUIContent revertButtonLabel = EditorGUIUtility.TextContent("Revert");
            public static readonly GUIContent applyButtonLabel = EditorGUIUtility.TextContent("Apply");

            public static readonly GUIContent spriteEditorWindowTitle = EditorGUIUtility.TextContent("Sprite Editor");

            public static readonly GUIContent pendingChangesDialogContent = EditorGUIUtility.TextContent("You have pending changes in the Sprite Editor Window.\nDo you want to apply these changes?");
            public static readonly GUIContent yesButtonLabel = EditorGUIUtility.TextContent("Yes");
            public static readonly GUIContent noButtonLabel = EditorGUIUtility.TextContent("No");

            public static readonly GUIContent applyRevertDialogTitle = EditorGUIUtility.TextContent("Unapplied import settings");
            public static readonly GUIContent applyRevertDialogContent = EditorGUIUtility.TextContent("Unapplied import settings for '{0}'");

            public static readonly GUIContent noSelectionWarning = EditorGUIUtility.TextContent("No texture or sprite selected");
            public static readonly GUIContent applyRevertModuleDialogTitle = EditorGUIUtility.TextContent("Unapplied module changes");
            public static readonly GUIContent applyRevertModuleDialogContent = EditorGUIUtility.TextContent("You have unapplied changes from the current module");

            public static readonly GUIContent saveProgressTitle = EditorGUIUtility.TextContent("Saving");
            public static readonly GUIContent saveContentText = EditorGUIUtility.TextContent("Saving Sprites {0}/{1}");
            public static readonly GUIContent loadProgressTitle = EditorGUIUtility.TextContent("Loading");
            public static readonly GUIContent loadContentText = EditorGUIUtility.TextContent("Loading Sprites {0}/{1}");
        }

        private const float k_MarginForFraming = 0.05f;
        private const float k_WarningMessageWidth = 250f;
        private const float k_WarningMessageHeight = 40f;
        private const float k_ModuleListWidth = 90f;

        public static SpriteEditorWindow s_Instance;
        public bool m_ResetOnNextRepaint;
        public bool m_IgnoreNextPostprocessEvent;
        public ITexture2D m_OriginalTexture;

        private SpriteRectCache m_RectsCache;
        ISpriteEditorDataProvider m_SpriteDataProvider;
        private SerializedObject m_TextureImporterSO;

        private bool m_RequestRepaint = false;

        public static bool s_OneClickDragStarted = false;
        public string m_SelectedAssetPath;

        private IEventSystem m_EventSystem;
        private IUndoSystem m_UndoSystem;
        private IAssetDatabase m_AssetDatabase;
        private IGUIUtility m_GUIUtility;
        private UnityTexture2D m_OutlineTexture;
        private UnityTexture2D m_ReadableTexture;

        [SerializeField]
        private SpriteRect m_Selected;

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
            if (selectedTextureChanged)
                OnSelectionChange();
        }

        public static void TextureImporterApply(SerializedObject so)
        {
            if (s_Instance == null)
                return;
            s_Instance.ApplyCacheSettingsToInspector(so);
        }

        void ApplyCacheSettingsToInspector(SerializedObject so)
        {
            // Apply cache settings if SpriteEditorWindow's sprite is the same as the texture importer's
            if (m_SpriteDataProvider != null && m_SpriteDataProvider.targetObject == so.targetObject)
            {
                if (so.FindProperty("m_SpriteMode").intValue == (int)m_SpriteDataProvider.spriteImportMode)
                {
                    s_Instance.m_IgnoreNextPostprocessEvent = true;
                }
                else if (textureIsDirty)
                {
                    // sprite mode is different and user have pending changes. Ask user if they want to save it
                    bool yes = EditorUtility.DisplayDialog(SpriteEditorWindowStyles.spriteEditorWindowTitle.text,
                            SpriteEditorWindowStyles.pendingChangesDialogContent.text,
                            SpriteEditorWindowStyles.yesButtonLabel.text, SpriteEditorWindowStyles.noButtonLabel.text);
                    if (yes)
                    {
                        // Save user changes into Inspector's SeralizedObject
                        DoApply(so);
                    }
                }
            }
        }

        public void RefreshPropertiesCache()
        {
            m_OriginalTexture = GetSelectedTexture2D();

            if (m_OriginalTexture == null)
                return;

            var ai = TextureImporter.GetAtPath(m_SelectedAssetPath);
            m_SpriteDataProvider = ai as ISpriteEditorDataProvider;
            if (ai is TextureImporter)
                m_SpriteDataProvider = new UnityEditor.U2D.Interface.TextureImporter((TextureImporter)ai);

            if (ai == null || m_SpriteDataProvider == null)
                return;

            m_TextureImporterSO = new SerializedObject(ai);
            m_SpriteDataProvider.InitSpriteEditorDataProvider(m_TextureImporterSO);

            int width = 0, height = 0;
            m_SpriteDataProvider.GetTextureActualWidthAndHeight(out width, out height);
            m_Texture = m_OriginalTexture == null ? null : new PreviewTexture2D(m_OriginalTexture, width, height);
        }

        public void InvalidatePropertiesCache()
        {
            if (m_RectsCache)
            {
                m_RectsCache.ClearAll();
                DestroyImmediate(m_RectsCache);
            }
            if (m_ReadableTexture)
            {
                DestroyImmediate(m_ReadableTexture);
                m_ReadableTexture = null;
            }


            m_OriginalTexture = null;
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

        private bool multipleSprites
        {
            get
            {
                if (m_SpriteDataProvider != null)
                    return m_SpriteDataProvider.spriteImportMode == SpriteImportMode.Multiple;
                return false;
            }
        }

        private bool validSprite
        {
            get
            {
                if (m_SpriteDataProvider != null)
                    return m_SpriteDataProvider.spriteImportMode != SpriteImportMode.None;
                return false;
            }
        }

        bool activeTextureSelected
        {
            get { return m_SpriteDataProvider != null && m_Texture != null && m_OriginalTexture != null; }
        }

        public bool textureIsDirty
        {
            get; set;
        }

        public bool selectedTextureChanged
        {
            get
            {
                ITexture2D newTexture = GetSelectedTexture2D();
                return newTexture != null && m_OriginalTexture != newTexture;
            }
        }

        public bool IsEditingDisabled()
        {
            return EditorApplication.isPlayingOrWillChangePlaymode;
        }

        void OnSelectionChange()
        {
            // In case of changed of texture/sprite or selected on non texture object
            if (GetSelectedTexture2D() == null || selectedTextureChanged)
            {
                HandleApplyRevertDialog();
                ResetWindow();
                RefreshPropertiesCache();
                RefreshRects();
            }

            if (m_RectsCache != null)
            {
                if (Selection.activeObject is Sprite)
                    UpdateSelectedSpriteRect(Selection.activeObject as Sprite);
                else if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<SpriteRenderer>())
                {
                    Sprite sprite = Selection.activeGameObject.GetComponent<SpriteRenderer>().sprite;
                    UpdateSelectedSpriteRect(sprite);
                }
            }

            UpdateAvailableModules();
            Repaint();
        }

        public void ResetWindow()
        {
            InvalidatePropertiesCache();

            selectedSpriteRect = null;
            textureIsDirty = false;
            m_Zoom = -1;
        }

        void OnEnable()
        {
            minSize = new Vector2(360, 200);
            titleContent = SpriteEditorWindowStyles.spriteEditorWindowTitle;
            s_Instance = this;
            m_UndoSystem.RegisterUndoCallback(UndoRedoPerformed);
            EditorApplication.modifierKeysChanged += ModifierKeysChanged;
            ResetWindow();
            RefreshPropertiesCache();
            RefreshRects();
            InitModules();
        }

        private void UndoRedoPerformed()
        {
            ITexture2D newTexture = GetSelectedTexture2D();

            // Was selected texture changed by undo?
            if (newTexture != null && m_OriginalTexture != newTexture)
                OnSelectionChange();

            InitSelectedSpriteRect();

            Repaint();
        }

        private void InitSelectedSpriteRect()
        {
            SpriteRect newSpriteRect = null;
            if (m_RectsCache != null)
            {
                if (m_RectsCache.Count > 0)
                    newSpriteRect = m_RectsCache.Contains(selectedSpriteRect) ? selectedSpriteRect : m_RectsCache.RectAt(0);
            }

            selectedSpriteRect = newSpriteRect;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            if (m_RectsCache != null)
            {
                Undo.ClearUndo(m_RectsCache);
            }
            HandleApplyRevertDialog();
            InvalidatePropertiesCache();
            EditorApplication.modifierKeysChanged -= ModifierKeysChanged;
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
        }

        void HandleApplyRevertDialog()
        {
            if (textureIsDirty && m_SpriteDataProvider != null)
            {
                if (EditorUtility.DisplayDialog(SpriteEditorWindowStyles.applyRevertDialogTitle.text,
                        String.Format(SpriteEditorWindowStyles.applyRevertDialogContent.text, m_SelectedAssetPath),
                        SpriteEditorWindowStyles.applyButtonLabel.text, SpriteEditorWindowStyles.revertButtonLabel.text))
                    DoApply();
                else
                    DoRevert();

                SetupModule(m_CurrentModuleIndex);
            }
        }

        void RefreshRects()
        {
            if (m_RectsCache)
            {
                m_RectsCache.ClearAll();
                Undo.ClearUndo(m_RectsCache);
                DestroyImmediate(m_RectsCache);
            }
            m_RectsCache = CreateInstance<SpriteRectCache>();

            if (m_SpriteDataProvider != null)
            {
                if (multipleSprites)
                {
                    for (int i = 0; i < m_SpriteDataProvider.spriteDataCount; i++)
                    {
                        SpriteRect spriteRect = new SpriteRect();
                        spriteRect.LoadFromSpriteData(m_SpriteDataProvider.GetSpriteData(i));
                        m_RectsCache.AddRect(spriteRect);
                        EditorUtility.DisplayProgressBar(SpriteEditorWindowStyles.loadProgressTitle.text, String.Format(SpriteEditorWindowStyles.loadContentText.text, i, m_SpriteDataProvider.spriteDataCount), (float)i / (float)m_SpriteDataProvider.spriteDataCount);
                    }
                }
                else if (validSprite)
                {
                    SpriteRect spriteRect = new SpriteRect();
                    spriteRect.LoadFromSpriteData(m_SpriteDataProvider.GetSpriteData(0));
                    spriteRect.rect = new Rect(0, 0, m_Texture.width, m_Texture.height);
                    spriteRect.name = m_OriginalTexture.name;

                    m_RectsCache.AddRect(spriteRect);
                }

                EditorUtility.ClearProgressBar();
            }

            InitSelectedSpriteRect();
        }

        void OnGUI()
        {
            InitStyles();

            if (m_ResetOnNextRepaint || selectedTextureChanged || m_RectsCache == null)
            {
                ResetWindow();
                RefreshPropertiesCache();
                RefreshRects();
                UpdateAvailableModules();
                SetupModule(m_CurrentModuleIndex);
                m_ResetOnNextRepaint = false;
            }
            Matrix4x4 oldHandlesMatrix = Handles.matrix;

            if (!activeTextureSelected)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    GUILayout.Label(SpriteEditorWindowStyles.noSelectionWarning);
                }
                return;
            }

            // Top menu bar
            DoToolbarGUI();
            DoTextureGUI();

            // Warning message if applicable
            DoEditingDisabledMessage();
            m_CurrentModule.OnPostGUI();
            Handles.matrix = oldHandlesMatrix;

            if (m_RequestRepaint == true)
                Repaint();
        }

        protected override void DoTextureGUIExtras()
        {
            HandleFrameSelected();

            if (m_EventSystem.current.type == EventType.Repaint)
            {
                SpriteEditorUtility.BeginLines(new Color(1f, 1f, 1f, 0.5f));
                for (int i = 0; i < m_RectsCache.Count; i++)
                {
                    if (m_RectsCache.RectAt(i) != selectedSpriteRect)
                        SpriteEditorUtility.DrawBox(m_RectsCache.RectAt(i).rect);
                }
                SpriteEditorUtility.EndLines();
            }

            m_CurrentModule.DoTextureGUI();
        }

        private void DoToolbarGUI()
        {
            GUIStyle toolBarStyle = EditorStyles.toolbar;

            Rect toolbarRect = new Rect(0, 0, position.width, k_ToolbarHeight);
            if (m_EventSystem.current.type == EventType.Repaint)
            {
                toolBarStyle.Draw(toolbarRect, false, false, false, false);
            }
            m_TextureViewRect = new Rect(0f, k_ToolbarHeight, position.width - k_ScrollbarMargin, position.height - k_ScrollbarMargin - k_ToolbarHeight);

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
            m_CurrentModule.DrawToolbarGUI(toolbarRect);
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

        private void DoApply(SerializedObject so)
        {
            if (multipleSprites)
            {
                var oldNames = new List<string>();
                var newNames = new List<string>();
                m_SpriteDataProvider.spriteDataCount = m_RectsCache.Count;
                for (int i = 0; i < m_RectsCache.Count; i++)
                {
                    SpriteRect spriteRect = m_RectsCache.RectAt(i);

                    if (string.IsNullOrEmpty(spriteRect.name))
                        spriteRect.name = "Empty";

                    if (!string.IsNullOrEmpty(spriteRect.originalName))
                    {
                        oldNames.Add(spriteRect.originalName);
                        newNames.Add(spriteRect.name);
                    }

                    var newRect = m_SpriteDataProvider.GetSpriteData(i);
                    spriteRect.ApplyToSpriteData(newRect);
                    EditorUtility.DisplayProgressBar(SpriteEditorWindowStyles.saveProgressTitle.text, String.Format(SpriteEditorWindowStyles.saveContentText.text, i, m_RectsCache.Count), (float)i / (float)m_RectsCache.Count);
                }

                if (oldNames.Count > 0)
                    PatchImportSettingRecycleID.PatchMultiple(so, 213, oldNames.ToArray(), newNames.ToArray());
            }
            else
            {
                if (m_RectsCache.Count > 0)
                {
                    SpriteRect spriteRect = m_RectsCache.RectAt(0);
                    var spriteData = m_SpriteDataProvider.GetSpriteData(0);
                    spriteRect.ApplyToSpriteData(spriteData);
                }
            }
            m_SpriteDataProvider.Apply(so);
            EditorUtility.ClearProgressBar();
        }

        private void DoApply()
        {
            m_UndoSystem.ClearUndo(m_RectsCache);
            DoApply(m_TextureImporterSO);
            m_TextureImporterSO.ApplyModifiedPropertiesWithoutUndo();
            // Usually on postprocess event, we assume things are changed so much that we need to reset. However here we are the one triggering it, so we ignore it.
            m_IgnoreNextPostprocessEvent = true;
            DoTextureReimport(m_SelectedAssetPath);
            textureIsDirty = false;
            InitSelectedSpriteRect();
        }

        private void DoRevert()
        {
            textureIsDirty = false;
            RefreshRects();
            GUI.FocusControl("");
        }

        public void HandleSpriteSelection()
        {
            if (m_EventSystem.current.type == EventType.MouseDown && m_EventSystem.current.button == 0 && GUIUtility.hotControl == 0 && !m_EventSystem.current.alt)
            {
                SpriteRect oldSelected = selectedSpriteRect;

                selectedSpriteRect = TrySelect(m_EventSystem.current.mousePosition);

                if (selectedSpriteRect != null)
                    s_OneClickDragStarted = true;
                else
                    RequestRepaint();

                if (oldSelected != selectedSpriteRect && selectedSpriteRect != null)
                {
                    m_EventSystem.current.Use();
                }
            }
        }

        private void HandleFrameSelected()
        {
            UnityEngine.U2D.Interface.IEvent evt = m_EventSystem.current;

            if ((evt.type == EventType.ValidateCommand || evt.type == EventType.ExecuteCommand)
                && evt.commandName == "FrameSelected")
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
            for (int i = 0; i < m_RectsCache.Count; i++)
            {
                if (sprite.rect == m_RectsCache.RectAt(i).rect)
                {
                    selectedSpriteRect = m_RectsCache.RectAt(i);
                    return;
                }
            }
            selectedSpriteRect = null;
        }

        private ITexture2D GetSelectedTexture2D()
        {
            UnityTexture2D texture = null;

            if (Selection.activeObject is UnityTexture2D)
            {
                texture = Selection.activeObject as UnityTexture2D;
            }
            else if (Selection.activeObject is Sprite)
            {
                texture = UnityEditor.Sprites.SpriteUtility.GetSpriteTexture(Selection.activeObject as Sprite, false);
            }
            else if (Selection.activeGameObject)
            {
                if (Selection.activeGameObject.GetComponent<SpriteRenderer>())
                {
                    if (Selection.activeGameObject.GetComponent<SpriteRenderer>().sprite)
                    {
                        texture = UnityEditor.Sprites.SpriteUtility.GetSpriteTexture(Selection.activeGameObject.GetComponent<SpriteRenderer>().sprite, false);
                    }
                }
            }

            if (texture != null)
                m_SelectedAssetPath = m_AssetDatabase.GetAssetPath(texture);

            return new UnityEngine.U2D.Interface.Texture2D(texture);
        }

        private SpriteRect TrySelect(Vector2 mousePosition)
        {
            float selectedSize = float.MaxValue;
            SpriteRect currentRect = null;
            mousePosition = Handles.inverseMatrix.MultiplyPoint(mousePosition);

            for (int i = 0; i < m_RectsCache.Count; i++)
            {
                SpriteRect sr = m_RectsCache.RectAt(i);
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
                textureIsDirty = false;
            }
        }

        GUIContent[] m_RegisteredModuleNames;
        List<ISpriteEditorModule> m_AllRegisteredModules;
        List<ISpriteEditorModule> m_RegisteredModules;
        ISpriteEditorModule m_CurrentModule = null;
        int m_CurrentModuleIndex = 0;

        void SetupModule(int newModuleIndex)
        {
            if (s_Instance == null)
                return;

            if (m_CurrentModule != null)
                m_CurrentModule.OnModuleDeactivate();

            if (m_RegisteredModules.Count > newModuleIndex)
            {
                m_CurrentModule = m_RegisteredModules[newModuleIndex];
                m_CurrentModule.OnModuleActivate();
                m_CurrentModuleIndex = newModuleIndex;
            }
        }

        void UpdateAvailableModules()
        {
            if (m_AllRegisteredModules == null)
                return;
            m_RegisteredModules = new List<ISpriteEditorModule>();
            foreach (var module in m_AllRegisteredModules)
            {
                if (module.CanBeActivated())
                {
                    m_RegisteredModules.Add(module);
                }
            }

            m_RegisteredModuleNames = new GUIContent[m_RegisteredModules.Count];
            for (int i = 0; i < m_RegisteredModules.Count; i++)
            {
                m_RegisteredModuleNames[i] = new GUIContent(m_RegisteredModules[i].moduleName);
            }

            if (!(m_RegisteredModules.Contains(m_CurrentModule)))
            {
                SetupModule(0);
            }
            else
                SetupModule(m_CurrentModuleIndex);
        }

        void InitModules()
        {
            m_AllRegisteredModules = new List<ISpriteEditorModule>();

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
            m_AllRegisteredModules.Add(new SpriteFrameModule(this, m_EventSystem, m_UndoSystem, m_AssetDatabase));
            m_AllRegisteredModules.Add(new SpritePolygonModeModule(this, m_EventSystem, m_UndoSystem, m_AssetDatabase));
            m_AllRegisteredModules.Add(new SpriteOutlineModule(this, m_EventSystem, m_UndoSystem, m_AssetDatabase, m_GUIUtility, new ShapeEditorFactory(), outlineTexture));
            m_AllRegisteredModules.Add(new SpritePhysicsShapeModule(this, m_EventSystem, m_UndoSystem, m_AssetDatabase, m_GUIUtility, new ShapeEditorFactory(), outlineTexture));

            UpdateAvailableModules();
        }

        public ISpriteRectCache spriteRects
        {
            get { return m_RectsCache; }
        }

        public SpriteRect selectedSpriteRect
        {
            get
            {
                // Always return null if editing is disabled to prevent all possible action to selected frame.
                if (editingDisabled)
                    return null;

                return m_Selected;
            }
            set { m_Selected = value; }
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
            get { return position; }
        }

        public ITexture2D selectedTexture
        {
            get { return m_OriginalTexture; }
        }

        public ITexture2D previewTexture
        {
            get { return m_Texture; }
        }

        public bool editingDisabled
        {
            get { return EditorApplication.isPlayingOrWillChangePlaymode; }
        }

        public void DisplayProgressBar(string title, string content, float progress)
        {
            EditorUtility.DisplayProgressBar(title, content, progress);
        }

        public void ClearProgressBar()
        {
            EditorUtility.ClearProgressBar();
        }

        public ITexture2D GetReadableTexture2D()
        {
            if (m_ReadableTexture == null)
            {
                int width = 0, height = 0;
                m_SpriteDataProvider.GetTextureActualWidthAndHeight(out width, out height);
                m_ReadableTexture = SpriteUtility.CreateTemporaryDuplicate(m_OriginalTexture, width, height);
                if (m_ReadableTexture != null)
                    m_ReadableTexture.filterMode = FilterMode.Point;
            }

            return new UnityEngine.U2D.Interface.Texture2D(m_ReadableTexture);
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
    }

    internal class SpriteEditorTexturePostprocessor : AssetPostprocessor
    {
        public override int GetPostprocessOrder()
        {
            return 1;
        }

        public void OnPostprocessTexture(UnityTexture2D tex)
        {
            if (SpriteEditorWindow.s_Instance != null)
            {
                if (assetPath.Equals(SpriteEditorWindow.s_Instance.m_SelectedAssetPath))
                {
                    if (!SpriteEditorWindow.s_Instance.m_IgnoreNextPostprocessEvent)
                    {
                        SpriteEditorWindow.s_Instance.m_ResetOnNextRepaint = true;
                    }
                    else
                    {
                        SpriteEditorWindow.s_Instance.m_IgnoreNextPostprocessEvent = false;
                    }
                    SpriteEditorWindow.s_Instance.Repaint();
                }
            }
        }
    }
}
