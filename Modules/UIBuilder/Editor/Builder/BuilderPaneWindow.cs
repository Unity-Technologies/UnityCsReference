// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderPaneWindow : EditorWindow
    {
        BuilderDocument m_Document;
        BuilderCommandHandler m_CommandHandler;

        public BuilderDocument document
        {
            get
            {
                // Find or create document.
                if (m_Document == null)
                {
                    var allDocuments = Resources.FindObjectsOfTypeAll(typeof(BuilderDocument));
                    if (allDocuments.Length > 1)
                        Debug.LogError("UIBuilder: More than one BuilderDocument was somehow created!");
                    if (allDocuments.Length == 0)
                        m_Document = BuilderDocument.CreateInstance();
                    else
                        m_Document = allDocuments[0] as BuilderDocument;
                }

                return m_Document;
            }
        }

        public BuilderCommandHandler commandHandler
        {
            get
            {
                if (m_CommandHandler == null)
                {
                    var selection = primarySelection;
                    if (selection == null)
                        return null;

                    m_CommandHandler = new BuilderCommandHandler(this, selection);
                }
                return m_CommandHandler;
            }
        }

        public BuilderSelection primarySelection
        {
            get
            {
                if (this is IBuilderViewportWindow)
                    return (this as IBuilderViewportWindow).selection;

                return document.primaryViewportWindow.selection;
            }
        }

        protected static T GetWindowAndInit<T>() where T : BuilderPaneWindow
        {
            var window = GetWindow<T>();
            window.Show();
            return window;
        }

        protected static T GetWindowWithRectAndInit<T>(Rect rect) where T : BuilderPaneWindow
        {
            var window = GetWindowWithRect<T>(rect);

            window.minSize = BuilderConstants.BuilderWindowDefaultMinSize;
            window.maxSize = BuilderConstants.BuilderWindowDefaultMaxSize;
            window.Show();
            return window;
        }

        protected void SetTitleContent(string windowTitle, string windowIconPath = null)
        {
            if (string.IsNullOrEmpty(windowTitle))
                return;

            Texture2D iconTex = null;
            if (!string.IsNullOrEmpty(windowIconPath))
            {
                if (EditorGUIUtility.isProSkin)
                {
                    var newName = "d_" + Path.GetFileName(windowIconPath);
                    var iconDirName = Path.GetDirectoryName(windowIconPath);
                    if (!string.IsNullOrEmpty(iconDirName))
                        newName = $"{iconDirName}/{newName}";

                    windowIconPath = newName;
                }

                if (EditorGUIUtility.pixelsPerPoint > 1)
                    windowIconPath = $"{windowIconPath}@2x";

                iconTex = EditorGUIUtility.Load(windowIconPath + ".png") as Texture2D;
            }

            titleContent = new GUIContent(windowTitle, iconTex);
        }

        protected virtual void OnEnable()
        {
            var root = rootVisualElement;

            // Load assets.
            var mainSS = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/Builder.uss");
            var themeSS = EditorGUIUtility.isProSkin
                ? BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/BuilderDark.uss")
                : BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/BuilderLight.uss");

            // HACK: Check for null assets.
            // See: https://fogbugz.unity3d.com/f/cases/1180330/
            if (mainSS == null || themeSS == null)
            {
                EditorApplication.delayCall += () =>
                {
                    this.m_Parent.Reload(this);
                };
                return;
            }

            // Load styles.
            root.styleSheets.Add(mainSS);
            root.styleSheets.Add(themeSS);

            // Handle viewport window.
            if (this is IBuilderViewportWindow || document.primaryViewportWindow != null)
                CreateUIInternal();

            // Register window.
            document.RegisterWindow(this);
        }

        public void SetHasUnsavedChanges(bool unsaved)
        {
            hasUnsavedChanges = unsaved;
        }

        void CreateUIInternal()
        {
            CreateUI();

            commandHandler.OnEnable();
        }

        public virtual void CreateUI()
        {
            // Nothing to do by default.
        }

        public virtual void ClearUI()
        {
            // Nothing to do by default.
        }

        protected virtual void OnDisable()
        {
            // Unregister window.
            document.UnregisterWindow(this);

            // Commands
            if (m_CommandHandler != null)
                m_CommandHandler.OnDisable();
        }

        public virtual void OnEnableAfterAllSerialization()
        {
            // Nothing to do by default.
        }

        public virtual bool LoadDocument(VisualTreeAsset asset, bool unloadAllSubdocuments = true)
        {
            return false;
        }

        public virtual bool NewDocument(bool checkForUnsavedChanges = true, bool unloadAllSubdocuments = true)
        {
            return true;
        }

        public virtual void PrimaryViewportWindowChanged()
        {
            if (this is IBuilderViewportWindow)
                return;

            ClearUI();
            rootVisualElement.Clear();

            m_CommandHandler = null;

            var viewportWindow = document.primaryViewportWindow;
            if (viewportWindow == null)
                AddMessageForNoViewportOpen();
            else
                CreateUIInternal();
        }

        void AddMessageForNoViewportOpen()
        {
            rootVisualElement.Add(new Label("No viewport window open."));
        }
    }
}
