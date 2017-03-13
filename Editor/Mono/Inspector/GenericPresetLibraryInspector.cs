// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;

namespace UnityEditor
{
    class GenericPresetLibraryInspector<T> where T : ScriptableObject
    {
        readonly ScriptableObjectSaveLoadHelper<T> m_SaveLoadHelper;
        readonly UnityEngine.Object m_Target;
        readonly string m_Header;
        readonly VerticalGrid m_Grid;
        readonly Action<string> m_EditButtonClickedCallback;
        private static GUIStyle s_EditButtonStyle;
        private float m_LastRepaintedWidth = -1f;

        // Configure
        public int maxShowNumPresets { get; set; }
        public Vector2 presetSize { get; set; }
        public float lineSpacing { get; set; }
        public string extension { get { return m_SaveLoadHelper.fileExtensionWithoutDot; } }
        public bool useOnePixelOverlappedGrid { get; set; }
        public RectOffset marginsForList { get; set; }
        public RectOffset marginsForGrid { get; set; }
        public PresetLibraryEditorState.ItemViewMode itemViewMode { get; set; }

        public GenericPresetLibraryInspector(UnityEngine.Object target, string header, Action<string> editButtonClicked)
        {
            m_Target = target;
            m_Header = header;
            m_EditButtonClickedCallback = editButtonClicked;

            string assetPath = AssetDatabase.GetAssetPath(m_Target.GetInstanceID());
            string extension = Path.GetExtension(assetPath);
            if (!string.IsNullOrEmpty(extension))
                extension = extension.TrimStart('.');
            m_SaveLoadHelper = new ScriptableObjectSaveLoadHelper<T>(extension, SaveType.Text);
            m_Grid = new VerticalGrid();

            // Default configuration
            maxShowNumPresets = 49; // We clear some preview caches when they reach 50 (See AnimationCurvePreviewCache and GradientPreviewCache)
            presetSize = new Vector2(14, 14);
            lineSpacing = 1f;
            useOnePixelOverlappedGrid = false;
            marginsForList = new RectOffset(10, 10, 5, 5);
            marginsForGrid = new RectOffset(10, 10, 5, 5);
            itemViewMode = PresetLibraryEditorState.ItemViewMode.List;
        }

        public void OnDestroy()
        {
            PresetLibraryManager.instance.UnloadAllLibrariesFor(m_SaveLoadHelper);
        }

        public void OnInspectorGUI()
        {
            if (s_EditButtonStyle == null)
            {
                s_EditButtonStyle = new GUIStyle(EditorStyles.miniButton);
                s_EditButtonStyle.margin.top = 7;
            }

            string assetPath = AssetDatabase.GetAssetPath(m_Target.GetInstanceID());
            string libraryPath = Path.ChangeExtension(assetPath, null);
            bool isInAnEditorFolder = libraryPath.Contains("/Editor/");

            GUILayout.BeginHorizontal();
            GUILayout.Label(m_Header, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (isInAnEditorFolder && m_EditButtonClickedCallback != null && GUILayout.Button("Edit...", s_EditButtonStyle))
            {
                if (m_EditButtonClickedCallback != null)
                    m_EditButtonClickedCallback(libraryPath);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            if (!isInAnEditorFolder)
            {
                GUIContent c = new GUIContent("Preset libraries should be placed in an 'Editor' folder.", EditorGUIUtility.warningIcon);
                GUILayout.Label(c, EditorStyles.helpBox);
            }

            DrawPresets(libraryPath);
        }

        private void DrawPresets(string libraryPath)
        {
            if (GUIClip.visibleRect.width > 0)
                m_LastRepaintedWidth = GUIClip.visibleRect.width;

            if (m_LastRepaintedWidth < 0)
            {
                GUILayoutUtility.GetRect(1, 1); // Ensure consistent call
                HandleUtility.Repaint(); // Wait until we have a proper width
                return;
            }

            PresetLibrary lib = PresetLibraryManager.instance.GetLibrary(m_SaveLoadHelper, libraryPath) as PresetLibrary;
            if (lib == null)
            {
                Debug.Log("Could not load preset library '" + libraryPath + "'");
                return;
            }

            SetupGrid(m_LastRepaintedWidth, lib.Count(), itemViewMode);


            int showNumPresets = Mathf.Min(lib.Count(), maxShowNumPresets);
            int hiddenNumPresets = lib.Count() - showNumPresets;
            float contentHeight = m_Grid.CalcRect(showNumPresets - 1, 0f).yMax + (hiddenNumPresets > 0 ? 20f : 0f);

            Rect reservedRect = GUILayoutUtility.GetRect(1, contentHeight);

            float spaceBetweenPresetAndText = presetSize.x + 6f;
            for (int index = 0; index < showNumPresets; ++index)
            {
                Rect r = m_Grid.CalcRect(index, reservedRect.y);
                Rect presetRect = new Rect(r.x, r.y, presetSize.x, presetSize.y);
                lib.Draw(presetRect, index);
                if (itemViewMode == PresetLibraryEditorState.ItemViewMode.List)
                {
                    Rect nameRect = new Rect(r.x + spaceBetweenPresetAndText, r.y, r.width - spaceBetweenPresetAndText, r.height);
                    string name = lib.GetName(index);
                    GUI.Label(nameRect, name);
                }
            }
            if (hiddenNumPresets > 0)
            {
                Rect textRect = new Rect(m_Grid.CalcRect(0, 0).x, reservedRect.y + contentHeight - 20f, reservedRect.width, 20f);
                GUI.Label(textRect, string.Format("+ {0} more...", hiddenNumPresets));
            }
        }

        void SetupGrid(float availableWidth, int itemCount, PresetLibraryEditorState.ItemViewMode presetsViewMode)
        {
            m_Grid.useFixedHorizontalSpacing = useOnePixelOverlappedGrid;
            m_Grid.fixedHorizontalSpacing = useOnePixelOverlappedGrid ? -1 : 0;

            switch (presetsViewMode)
            {
                case PresetLibraryEditorState.ItemViewMode.Grid:
                    m_Grid.fixedWidth = availableWidth;
                    m_Grid.topMargin = marginsForGrid.top;
                    m_Grid.bottomMargin = marginsForGrid.bottom;
                    m_Grid.leftMargin = marginsForGrid.left;
                    m_Grid.rightMargin = marginsForGrid.right;
                    m_Grid.verticalSpacing = useOnePixelOverlappedGrid ? -1 : lineSpacing;
                    m_Grid.minHorizontalSpacing = 8f;
                    m_Grid.itemSize = presetSize; // no text
                    m_Grid.InitNumRowsAndColumns(itemCount, int.MaxValue);
                    break;
                case PresetLibraryEditorState.ItemViewMode.List:
                    m_Grid.fixedWidth = availableWidth;
                    m_Grid.topMargin = marginsForList.top;
                    m_Grid.bottomMargin = marginsForList.bottom;
                    m_Grid.leftMargin = marginsForList.left;
                    m_Grid.rightMargin = marginsForList.right;
                    m_Grid.verticalSpacing = lineSpacing;
                    m_Grid.minHorizontalSpacing = 0f;
                    m_Grid.itemSize = new Vector2(availableWidth - m_Grid.leftMargin, presetSize.y);
                    m_Grid.InitNumRowsAndColumns(itemCount, int.MaxValue);
                    break;
            }
        }
    }
}
