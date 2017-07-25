// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Collaboration;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System;
using UnityEditor.Web;

namespace UnityEditor
{
    // Displays the Softlocks UI in the various areas of the Editor.
    internal class SoftlockViewController
    {
        private static SoftlockViewController s_Instance;
        public GUIStyle k_Style = null;
        public GUIStyle k_StyleEmpty = new GUIStyle(); // For tooltips only.
        public GUIContent k_Content = null;

        // Stores UI strings for reuse and Editor (inspector) references to trigger
        // a repaint when softlock data changes.
        private SoftlockViewController.Cache m_Cache = null;

        private const string k_TooltipHeader = "Unpublished changes by:";
        private const string k_TooltipPrefabHeader = "Unpublished Prefab changes by:";
        private const string k_TooltipNamePrefix = " \n \u2022  "; // u2022 displays a • (bullet point)

        private SoftlockViewController() {}
        ~SoftlockViewController() {}

        [SerializeField]
        private SoftLockFilters m_SoftLockFilters = new SoftLockFilters();

        public SoftLockFilters softLockFilters { get { return m_SoftLockFilters; } }

        public static SoftlockViewController Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new SoftlockViewController();
                    s_Instance.m_Cache = new Cache();
                }
                return s_Instance;
            }
        }

        // Initialises dependencies.
        public void TurnOn()
        {
            RegisterDataDelegate();
            RegisterDrawDelegates();
            Repaint();
        }

        public void TurnOff()
        {
            UnregisterDataDelegate();
            UnregisterDrawDelegates();
        }

        private void UnregisterDataDelegate()
        {
            SoftLockData.SoftlockSubscriber -= Instance.OnSoftlockUpdate;
        }

        private void RegisterDataDelegate()
        {
            UnregisterDataDelegate();
            SoftLockData.SoftlockSubscriber += Instance.OnSoftlockUpdate;
        }

        private void UnregisterDrawDelegates()
        {
            ObjectListArea.postAssetIconDrawCallback -= Instance.DrawProjectBrowserGridUI;
            ObjectListArea.postAssetLabelDrawCallback -= Instance.DrawProjectBrowserListUI;
            Editor.OnPostIconGUI -= Instance.DrawInspectorUI;
            GameObjectTreeViewGUI.OnPostHeaderGUI -= Instance.DrawSceneUI;
        }

        // Connects to the areas of the Editor that display softlocks.
        private void RegisterDrawDelegates()
        {
            UnregisterDrawDelegates();
            ObjectListArea.postAssetIconDrawCallback += Instance.DrawProjectBrowserGridUI;
            ObjectListArea.postAssetLabelDrawCallback += Instance.DrawProjectBrowserListUI;
            AssetsTreeViewGUI.postAssetLabelDrawCallback += Instance.DrawSingleColumnProjectBrowserUI;
            Editor.OnPostIconGUI += Instance.DrawInspectorUI;
            GameObjectTreeViewGUI.OnPostHeaderGUI += Instance.DrawSceneUI;
        }

        // Returns true when the 'editor' supports Softlock UI and the
        // user has Collaborate permissions.
        private bool HasSoftlockSupport(Editor editor)
        {
            if (!Collab.instance.IsCollabEnabledForCurrentProject() || editor == null || editor.targets.Length > 1)
            {
                return false;
            }

            if (editor.target == null || !SoftLockData.AllowsSoftLocks(editor.target))
            {
                return false;
            }

            // Support Scene and Game object Inspector headers, not others like MaterialEditor.
            bool hasSupport = true;
            Type editorType = editor.GetType();

            if (editorType != typeof(GameObjectInspector) && editorType != typeof(GenericInspector))
            {
                hasSupport = false;
            }

            return hasSupport;
        }

        private bool HasSoftlocks(string assetGUID)
        {
            if (!Collab.instance.IsCollabEnabledForCurrentProject())
            {
                return false;
            }

            bool hasSoftLocks;
            bool isValid = (SoftLockData.TryHasSoftLocks(assetGUID, out hasSoftLocks) && hasSoftLocks);
            return isValid;
        }

        // Redraws softlock UI associated with the given list of 'assetGUIDs'.
        public void OnSoftlockUpdate(string[] assetGUIDs)
        {
            // Remove cached UI for the assetGUIDs before triggered a redraw.
            m_Cache.InvalidateAssetGUIDs(assetGUIDs);
            Repaint();
        }

        // Repaints all the areas where softlocks are displayed.
        public void Repaint()
        {
            RepaintInspectors();
            RepaintSceneHierarchy();
            RepaintProjectBrowsers();
        }

        private void RepaintSceneHierarchy()
        {
            List<SceneHierarchyWindow> sceneUIs = SceneHierarchyWindow.GetAllSceneHierarchyWindows();
            foreach (SceneHierarchyWindow sceneUI in sceneUIs)
            {
                sceneUI.Repaint();
            }
        }

        private void RepaintInspectors()
        {
            foreach (Editor editor in m_Cache.GetEditors())
            {
                // Does not repaint when editor is not visible, but the editor's
                // "DockArea" tab will redraw either way.
                editor.Repaint();
            }
        }

        private void RepaintProjectBrowsers()
        {
            foreach (ProjectBrowser pb in ProjectBrowser.GetAllProjectBrowsers())
            {
                pb.RefreshSearchIfFilterContains("s:");
                pb.Repaint();
            }
        }

        // Draws in the Hierarchy header, left of the context menu.
        public void DrawSceneUI(Rect availableRect, string scenePath)
        {
            string assetGUID = AssetDatabase.AssetPathToGUID(scenePath);
            if (!HasSoftlocks(assetGUID))
            {
                return;
            }

            int lockCount;
            SoftLockData.TryGetSoftlockCount(assetGUID, out lockCount);

            GUIContent content = GetGUIContent();
            content.image = SoftLockUIData.GetIconForSection(SoftLockUIData.SectionEnum.Scene);
            content.text = GetDisplayCount(lockCount);
            content.tooltip = Instance.GetTooltip(assetGUID);

            Vector2 contentSize = GetStyle().CalcSize(content);
            Rect drawRect = new Rect(availableRect.position, contentSize);
            const int kRightMargin = 4;
            drawRect.x = (availableRect.width - drawRect.width) - kRightMargin;
            EditorGUI.LabelField(drawRect, content);
        }

        // Assigned as a callback to Editor.OnPostHeaderGUI
        // Draws the Scene Inspector (Editor.cs) as well as the Game Object Inspector (GameObjectInspector.cs)
        private void DrawInspectorUI(Editor editor, Rect drawRect)
        {
            if (!HasSoftlockSupport(editor))
            {
                return;
            }

            m_Cache.StoreEditor(editor);
            string assetGUID = null;
            AssetAccess.TryGetAssetGUIDFromObject(editor.target, out assetGUID);

            if (!HasSoftlocks(assetGUID))
            {
                return;
            }

            Texture icon = SoftLockUIData.GetIconForSection(SoftLockUIData.SectionEnum.ProjectBrowser);
            if (icon != null)
            {
                DrawIconWithTooltips(drawRect, icon, assetGUID);
            }
        }

        // Assigned callback to ObjectListArea.OnPostAssetDrawDelegate.
        // Draws either overtop of the project browser asset (when in grid view).
        private void DrawProjectBrowserGridUI(Rect iconRect, string assetGUID, bool isListMode)
        {
            if (isListMode || !HasSoftlocks(assetGUID))
            {
                return;
            }

            Rect drawRect = Rect.zero;
            Texture icon = SoftLockUIData.GetIconForSection(SoftLockUIData.SectionEnum.ProjectBrowser);
            if (icon != null)
            {
                drawRect = Overlay.GetRectForBottomRight(iconRect, Overlay.k_OverlaySizeOnLargeIcon);
                DrawIconWithTooltips(drawRect, icon, assetGUID);
            }
        }

        // Should draw only in listMode and expects 'drawRect' to be the designed space for the icon,
        // and not the entire row.
        private bool DrawProjectBrowserListUI(Rect drawRect, string assetGUID, bool isListMode)
        {
            if (!isListMode || !HasSoftlocks(assetGUID))
            {
                return false;
            }

            // center icon.
            Rect iconRect = drawRect;
            iconRect.width = drawRect.height;
            iconRect.x = (float)Math.Round(drawRect.center.x - (iconRect.width / 2F));
            return DrawInProjectBrowserListMode(iconRect, assetGUID);
        }

        // Expects 'drawRect' to be the available width of the row.
        private bool DrawSingleColumnProjectBrowserUI(Rect drawRect, string assetGUID)
        {
            if (ProjectBrowser.s_LastInteractedProjectBrowser.IsTwoColumns() || !HasSoftlocks(assetGUID))
            {
                return false;
            }

            Rect iconRect = drawRect;
            iconRect.width = drawRect.height;
            float spacingFromEnd = (iconRect.width / 2F);
            iconRect.x = (float)Math.Round(drawRect.xMax - iconRect.width - spacingFromEnd);
            return DrawInProjectBrowserListMode(iconRect, assetGUID);
        }

        private bool DrawInProjectBrowserListMode(Rect iconRect, string assetGUID)
        {
            Texture icon = SoftLockUIData.GetIconForSection(SoftLockUIData.SectionEnum.ProjectBrowser);
            bool didDraw = false;
            if (icon != null)
            {
                DrawIconWithTooltips(iconRect, icon, assetGUID);
                didDraw = true;
            }
            return didDraw;
        }

        private void DrawIconWithTooltips(Rect iconRect, Texture icon, string assetGUID)
        {
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            DrawTooltip(iconRect, GetTooltip(assetGUID));
        }

        private void DrawTooltip(Rect frame, string tooltip)
        {
            GUIContent content = GetGUIContent();
            content.tooltip = tooltip;
            GUI.Label(frame, content, k_StyleEmpty);
        }

        #region String Helpers

        // Returns a string formatted as a vertical list of names with a heading.
        private string GetTooltip(string assetGUID)
        {
            string formattedText;
            if (!m_Cache.TryGetTooltipForGUID(assetGUID, out formattedText))
            {
                List<string> softLockNames = SoftLockUIData.GetLocksNamesOnAsset(assetGUID);
                string tooltipHeaderText = (SoftLockData.IsPrefab(assetGUID) ? k_TooltipPrefabHeader : k_TooltipHeader);
                formattedText = tooltipHeaderText;

                foreach (string name in softLockNames)
                {
                    formattedText += k_TooltipNamePrefix + name + " ";
                }
                m_Cache.StoreTooltipForGUID(assetGUID, formattedText);
            }
            return formattedText;
        }

        // Retrieves a previously generated string from cache
        // or creates a string displaying the given 'count' surrounded by brackets.
        // e.g. "(0)"
        private static string GetDisplayCount(int count)
        {
            string totalLocksText;
            if (!Instance.m_Cache.TryGetDisplayCount(count, out totalLocksText))
            {
                totalLocksText = count.ToString();
                Instance.m_Cache.StoreDisplayCount(count, totalLocksText);
            }
            return totalLocksText;
        }

        // When the given 'text' exceeds the given 'width', out-of-bound characters
        // are removed as well as a few more to display a trailing ellipsis.
        // If 'text' does not exceed width, text is returned.
        private string FitTextToWidth(string text, float width, GUIStyle style)
        {
            int characterCountVisible = style.GetNumCharactersThatFitWithinWidth(text, width);
            if (characterCountVisible > 1 && characterCountVisible != text.Length)
            {
                string ellipsedText;
                int characterLength = (characterCountVisible - 1);
                if (!Instance.m_Cache.TryGetEllipsedNames(text, characterLength, out ellipsedText))
                {
                    ellipsedText = text.Substring(0, characterLength) + (" \u2026");    // 'horizontal ellipsis' (U+2026) is: ...
                    Instance.m_Cache.StoreEllipsedNames(text, ellipsedText, characterLength);
                }
                return ellipsedText;
            }
            return text;
        }

        #endregion
        #region GUI Content

        public GUIContent GetGUIContent()
        {
            if (k_Content == null)
            {
                k_Content = new GUIContent();
            }

            k_Content.tooltip = string.Empty;
            k_Content.text = null;
            k_Content.image = null;

            return k_Content;
        }

        public GUIStyle GetStyle()
        {
            if (k_Style == null)
            {
                k_Style = new GUIStyle(EditorStyles.label);
                k_Style.normal.background = null;
            }
            return k_Style;
        }

        #endregion

        // Stores UI strings for reuse and Editors as WeakReferences.
        private class Cache
        {
            private List<WeakReference> m_EditorReferences = new List<WeakReference>();
            private List<WeakReference> m_CachedWeakReferences = new List<WeakReference>();
            private static Dictionary<int, string> s_CachedStringCount = new Dictionary<int, string>();
            private Dictionary<string, string> m_AssetGUIDToTooltip = new Dictionary<string, string>();
            private Dictionary<string, Dictionary<int, string>> m_NamesListToEllipsedNames = new Dictionary<string, Dictionary<int, string>>();

            public Cache() {}

            // Removes cached strings references by the given 'assetGUIDs'.
            public void InvalidateAssetGUIDs(string[] assetGUIDs)
            {
                for (int index = 0; index < assetGUIDs.Length; index++)
                {
                    string assetGUID = assetGUIDs[index];
                    m_AssetGUIDToTooltip.Remove(assetGUID);
                }
            }

            // Failure: assigns empty string ("") to 'ellipsedNames', returns false.
            // Success: assigns the cached string to 'ellipsedNames', returns true.
            public bool TryGetEllipsedNames(string allNames, int characterLength, out string ellipsedNames)
            {
                Dictionary<int, string> ellipsedVersions;
                if (m_NamesListToEllipsedNames.TryGetValue(allNames, out ellipsedVersions))
                {
                    return ellipsedVersions.TryGetValue(characterLength, out ellipsedNames);
                }
                ellipsedNames = "";
                return false;
            }

            // 'allNames' and 'characterLength' will be the keys to access the cached 'ellipsedNames'
            // see TryGetEllipsedNames() for retrieval.
            public void StoreEllipsedNames(string allNames, string ellipsedNames, int characterLength)
            {
                Dictionary<int, string> ellipsedVersions;
                if (!m_NamesListToEllipsedNames.TryGetValue(allNames, out ellipsedVersions))
                {
                    ellipsedVersions = new Dictionary<int, string>();
                }
                ellipsedVersions[characterLength] = ellipsedNames;
                m_NamesListToEllipsedNames[allNames] = ellipsedVersions;
            }

            // Failure: assigns empty string ("") to 'tooltipText', returns false.
            // Success: assigns the cached string to 'tooltipText', returns true.
            public bool TryGetTooltipForGUID(string assetGUID, out string tooltipText)
            {
                return m_AssetGUIDToTooltip.TryGetValue(assetGUID, out tooltipText);
            }

            // 'assetGUID' will be the key to access the cached 'tooltipText'
            // see TryGetTooltipForGUID() for retrieval.
            public void StoreTooltipForGUID(string assetGUID, string tooltipText)
            {
                m_AssetGUIDToTooltip[assetGUID] = tooltipText;
            }

            // Failure: assigns empty string ("") to 'displayText', returns false.
            // Success: assigns the cached string to 'displayText', returns true.
            public bool TryGetDisplayCount(int count, out string displayText)
            {
                return s_CachedStringCount.TryGetValue(count, out displayText);
            }

            // 'count' will be the key to access the cached 'displayText'
            // see TryGetDisplayCount() for retrieval.
            public void StoreDisplayCount(int count, string displayText)
            {
                s_CachedStringCount.Add(count, displayText);
            }

            // Contains at most the list of all previously given Editors
            // via StoreEditor(). Garbage collected Editor(s) will be missing.
            public List<Editor> GetEditors()
            {
                List<Editor> editors = new List<Editor>();

                for (int index = 0; index < m_EditorReferences.Count; index++)
                {
                    WeakReference reference = m_EditorReferences[index];
                    Editor editor = reference.Target as Editor;

                    if (editor == null)
                    {
                        m_EditorReferences.RemoveAt(index);
                        m_CachedWeakReferences.Add(reference);
                        index--;
                    }
                    else
                    {
                        editors.Add(editor);
                    }
                }
                return editors;
            }

            // Stores the Editor in a WeakReference.
            public void StoreEditor(Editor editor)
            {
                bool canAdd = true;

                // Check for duplicates and purge any null targets.
                for (int index = 0; canAdd && (index < m_EditorReferences.Count); index++)
                {
                    WeakReference reference = m_EditorReferences[index];
                    Editor storedEditor = reference.Target as Editor;

                    if (storedEditor == null)
                    {
                        m_EditorReferences.RemoveAt(index);
                        m_CachedWeakReferences.Add(reference);
                        index--;
                    }
                    else if (storedEditor == editor)
                    {
                        canAdd = false;
                        break;
                    }
                }

                if (canAdd)
                {
                    WeakReference editorReference;

                    // Reuse any old WeakReference if available.
                    if (m_CachedWeakReferences.Count > 0)
                    {
                        editorReference = m_CachedWeakReferences[0];
                        m_CachedWeakReferences.RemoveAt(0);
                    }
                    else
                    {
                        editorReference = new WeakReference(null);
                    }
                    editorReference.Target = editor;
                    m_EditorReferences.Add(editorReference);
                }
            }
        }
    }
}

