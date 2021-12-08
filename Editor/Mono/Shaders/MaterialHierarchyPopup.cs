// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    class MaterialHierarchyPopup : PopupWindowContent
    {
        const int k_MaxSearchIterationPerFrame = 500;

        const float k_MinWindowWidth = 300f, k_MaxWindowWidth = 500f;

        const float k_HeaderHeight = 49f;
        const float k_EntryHeight = 20f;
        const float k_SliderWidth = 55f;
        const float k_SearchHeight = 150f;
        const float k_ConvertLabelWidth = 150f;

        const float k_Padding = 3f;
        const float k_OffsetX = 6f;
        const float k_SplitWidth = 1f;

        readonly float k_MinNameWidth, k_MaxNameWidth;
        readonly float k_TitleWidth = k_OffsetX + 50f;
        readonly float k_LocksWidth = EditorStyles.miniLabel.CalcSize(Styles.locksLabel).x + 2 * k_Padding;
        readonly float k_OverridesWidth = k_SplitWidth + EditorStyles.miniLabel.CalcSize(Styles.overridesLabel).x + 2 * k_Padding;

        readonly float k_ScrollbarHeight = GUI.skin.horizontalScrollbar.fixedHeight + GUI.skin.horizontalScrollbar.margin.top;

        Object[] targets;
        Material target;
        GUID targetGUID;
        int numRows;
        float windowWidth, namesWidth, noResultsX, locksX, overridesX;

        enum ConvertAction { None, Flatten, Convert }
        ConvertAction convertState;

        // Children list
        bool displayChildren;
        ObjectListArea listArea;
        int[] results = null;
        Delayer debounce;
        SearchFilter searchFilter;
        string searchFilterString = "";
        Vector2 scroll = Vector2.zero;
        IEnumerator<HierarchyProperty> enumerator = null;


        const int k_MinIconSize = 20;
        static ObjectListAreaState s_ListAreaState = new ObjectListAreaState() { m_GridSize = 56 };

        public static class Colors
        {
            static Color header_l = new Color32(0xDF, 0xDF, 0xDF, 0xFF);
            static Color header_d = new Color(0.5f, 0.5f, 0.5f, 0.2f);

            static Color[] rows_l = new Color[2]
            {
                new Color32(0xC8, 0xC8, 0xC8, 0xFF),
                new Color32(0xCE, 0xCE, 0xCE, 0xFF)
            };

            static Color[] rows_d = new Color[2]
            {
                new Color32(0x38, 0x38, 0x38, 0xFF),
                new Color32(0x3E, 0x3E, 0x3E, 0xFF)
            };

            public static Color headerBackground { get { return EditorGUIUtility.isProSkin ? Colors.header_d : Colors.header_l; } }
            public static Color rowBackground(int i) => EditorGUIUtility.isProSkin ? Colors.rows_d[i % 2] : Colors.rows_l[i % 2];
        }

        public static class Styles
        {
            public const string materialVariantHierarchyText = "Material Variant Hierarchy";

            public static readonly GUIContent parentLabel = EditorGUIUtility.TrTextContent("Parent", "The direct parent of the Material.");
            public static readonly GUIContent rootLabel = EditorGUIUtility.TrTextContent("Root", "The root of the hierarchy.");
            public static readonly GUIContent selectedLabel = EditorGUIUtility.TrTextContent("Current", "The currently selected Material.");

            public static readonly GUIContent instanceLabel = EditorGUIUtility.TrTextContent("Hierarchy of");
            public static readonly GUIContent ancestorLabel = EditorGUIUtility.TrTextContent("Ancestor");
            public static readonly GUIContent overridesLabel = EditorGUIUtility.TrTextContent("Overrides");
            public static readonly GUIContent locksLabel = EditorGUIUtility.TrTextContent("Locks");
            public static readonly GUIContent childrenLabel = EditorGUIUtility.TrTextContent("Children");
            public static readonly GUIContent noResultsLabel = EditorGUIUtility.TrTextContent("No results");
            public static readonly GUIContent noChildrenLabel = EditorGUIUtility.TrTextContent("This Material doesn't have any children.\nMaterial Variants created from this Material\nwill be listed here.");

            public static readonly string[] headerPopupOptions = new string[] { "Material", "Material Variant" };
            public static readonly GUIContent convertingLabel = EditorGUIUtility.TrTextContent("Converting to Material Variant");
            public static readonly GUIContent conversionHelpLabel = EditorGUIUtility.TrTextContent("To convert, select a Parent Material");

            public static readonly GUIStyle searchBackground = new GUIStyle("ProjectBrowserIconAreaBg");
            public static readonly GUIStyle centered = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            public static readonly GUIStyle boldRightAligned = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = (int)(1.1f * EditorStyles.boldLabel.fontSize)
            };
            public static readonly GUIStyle boldNumber = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = (int)(0.9f * EditorStyles.boldLabel.fontSize)
            };

            public static readonly GUIStyle searchFieldStyle = new GUIStyle(EditorStyles.toolbarSearchField)
            {
                margin = new RectOffset(5, 4, 4, 5)
            };
        }

        internal MaterialHierarchyPopup(Object[] targets)
        {
            this.targets = targets;
            target = targets[0] as Material;
            targetGUID = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(target));

            k_MinNameWidth = k_MinWindowWidth - (k_TitleWidth + k_SplitWidth + k_OverridesWidth + k_LocksWidth);
            k_MaxNameWidth = k_MaxWindowWidth - (k_TitleWidth + k_SplitWidth + k_OverridesWidth + k_LocksWidth);

            convertState = ConvertAction.None;
            searchFilter = new SearchFilter()
            {
                classNames = new string[] { "Material" },
                searchArea = SearchFilter.SearchArea.AllAssets
            };
            debounce = Delayer.Debounce(_ =>
            {
                SearchFilterChanged();
                editorWindow.Repaint();
            });

            Init();
        }

        void Init()
        {
            displayChildren = !target.isVariant;

            numRows = 0;
            namesWidth = k_MinNameWidth;
            if (target.isVariant)
            {
                Material current = target;
                while (current != null)
                {
                    numRows++;
                    namesWidth = Mathf.Max(GUI.skin.label.CalcSize(EditorGUIUtility.TempContent(current.name)).x + 23, namesWidth);
                    current = current.parent;
                }
                numRows = Mathf.Max(numRows, 2); // at least this and his parent
            }

            float prevWidth = windowWidth;
            if (namesWidth <= k_MinNameWidth)
                windowWidth = k_MinWindowWidth;
            else if (namesWidth >= k_MaxNameWidth)
                windowWidth = k_MaxWindowWidth;
            else
                windowWidth = k_TitleWidth + namesWidth + k_SplitWidth + k_OverridesWidth + k_LocksWidth;

            // Prevent window size from getting smaller when changing options
            windowWidth = Mathf.Max(windowWidth, prevWidth);

            locksX = windowWidth - k_LocksWidth;
            overridesX = locksX - k_OverridesWidth;
            noResultsX = (windowWidth - EditorStyles.label.CalcSize(Styles.noResultsLabel).x) * 0.5f;
        }

        public override void OnClose()
        {
            if (listArea != null)
                listArea.OnDestroy();
        }

        public override Vector2 GetWindowSize()
        {
            var height = k_HeaderHeight;

            if (target.isVariant)
            {
                // Horizontal scrollbar
                if (namesWidth > k_MaxNameWidth)
                    height += k_ScrollbarHeight;

                // Ancestors table
                height += numRows * k_EntryHeight + k_EntryHeight;
            }

            if (convertState != ConvertAction.Convert)
            {
                // Children list
                height += k_EntryHeight;
                if (displayChildren)
                    height += k_SearchHeight;
            }
            else
            {
                // Conversion panel
                height += 2 * k_EntryHeight + k_Padding;
            }

            return new Vector2(windowWidth, height);
        }

        public override void OnGUI(Rect rect)
        {
            // Escape closes the window
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }

            if (!DrawHeader())
                return;

            float height = k_HeaderHeight;
            if (target.isVariant)
                height += DrawVariantHierarchy();

            height += DrawChildrenLabel(height);

            if (displayChildren)
                DrawChildren(height);

            if (convertState == ConvertAction.Flatten)
            {
                convertState = ConvertAction.None;
                Undo.RecordObject(target, "Flatten Material Variant");
                target.parent = null;
                Init();
            }
        }

        bool DrawHeader()
        {
            Rect headerRect = GUILayoutUtility.GetRect(20, windowWidth, k_HeaderHeight, k_HeaderHeight);
            EditorGUI.DrawRect(headerRect, Colors.headerBackground);

            float labelSize = Styles.boldRightAligned.CalcSize(Styles.instanceLabel).x;

            Rect labelRect = new Rect(k_OffsetX, headerRect.y + k_Padding, labelSize, EditorGUIUtility.singleLineHeight);
            Rect contentRect = new Rect(labelRect.x + labelRect.width + k_Padding, labelRect.y, windowWidth, labelRect.height);

            GUI.Label(labelRect, Styles.instanceLabel, Styles.boldRightAligned);
            DoObjectLabel(contentRect, target, EditorStyles.boldLabel);

            labelRect.y = labelRect.height + 2 * k_Padding;
            if (convertState == ConvertAction.None)
            {
                labelRect.width = k_ConvertLabelWidth;
                int result = EditorGUI.Popup(labelRect, target.isVariant ? 1 : 0, Styles.headerPopupOptions);
                if (result == 0 && target.isVariant)
                    convertState = ConvertAction.Flatten;
                if (result == 1 && !target.isVariant)
                    convertState = ConvertAction.Convert;
            }
            else if (convertState == ConvertAction.Convert)
            {
                GUI.enabled = false;
                labelRect.width = 200f;
                EditorGUI.Button(labelRect, Styles.convertingLabel);
                GUI.enabled = true;

                // Conversion helper
                labelRect.y = k_HeaderHeight;
                labelRect.width = windowWidth;
                EditorGUI.LabelField(labelRect, Styles.conversionHelpLabel);

                labelRect.x = windowWidth - 14;
                if (GUI.Button(labelRect, GUIContent.none, EditorStyles.toolbarSearchFieldCancelButton))
                    convertState = ConvertAction.None;

                labelRect.x = k_OffsetX;
                var oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 70;
                EditorGUI.BeginChangeCheck();
                var parent = target.parent;
                MaterialEditor.ParentField(new Rect(k_OffsetX, labelRect.yMax + k_Padding, windowWidth - 2 * k_OffsetX, k_EntryHeight), targets);
                if (EditorGUI.EndChangeCheck() && parent != target.parent)
                {
                    convertState = ConvertAction.None;
                    Init();
                }
                EditorGUIUtility.labelWidth = oldLabelWidth;
                return false;
            }

            return true;
        }

        float DrawVariantHierarchy()
        {
            // Draw table header
            Rect entryRect = new Rect(0, k_HeaderHeight, windowWidth, k_EntryHeight);
            EditorGUI.DrawRect(entryRect, Colors.rowBackground(0));

            var labelRect = entryRect;
            labelRect.x = k_TitleWidth;
            GUI.Label(labelRect, Styles.ancestorLabel, EditorStyles.miniLabel);

            labelRect.x = overridesX + k_Padding;
            GUI.Label(labelRect, Styles.overridesLabel, EditorStyles.miniLabel);

            labelRect.x = locksX + k_Padding;
            GUI.Label(labelRect, Styles.locksLabel, EditorStyles.miniLabel);

            // Draw overrides and locks table
            int i = numRows;
            Material current = target;
            while (current != null)
            {
                entryRect.y = k_HeaderHeight + i * k_EntryHeight;
                EditorGUI.DrawRect(entryRect, Colors.rowBackground(i--));

                DisplayOverridesAndLocks(entryRect, current);
                current = current.parent;
            }

            var scrollRect = new Rect(k_TitleWidth, k_HeaderHeight + k_EntryHeight, Mathf.Min(namesWidth, k_MaxNameWidth), numRows * k_EntryHeight);
            scroll = GUI.BeginScrollView(new Rect(scrollRect) { height = scrollRect.height + k_ScrollbarHeight }, scroll, new Rect(scrollRect) { width = namesWidth });

            // Draw scrollable table
            i = numRows;
            current = target;
            entryRect.x = k_TitleWidth;
            while (i != 0)
            {
                entryRect.y = k_HeaderHeight + i-- * k_EntryHeight;

                if (current == null)
                {
                    GUI.Label(entryRect, EditorGUIUtility.TempContent("Missing (Material)"));
                    break;
                }
                DoObjectLabel(entryRect, current);
                current = current.parent;
            }

            GUI.EndScrollView();

            float height = (numRows + 1) * k_EntryHeight;
            if (namesWidth > k_MaxNameWidth)
                height += k_ScrollbarHeight;

            // Draw selected label
            labelRect.x = k_OffsetX;
            labelRect.y = k_HeaderHeight + numRows * k_EntryHeight;
            labelRect.width = k_TitleWidth - labelRect.x;
            GUI.Label(labelRect, Styles.selectedLabel);

            // Draw parent label
            labelRect.y = k_HeaderHeight + (numRows - 1) * k_EntryHeight;
            GUI.Label(labelRect, Styles.parentLabel);

            // Draw root label
            if (labelRect.y != k_HeaderHeight + k_EntryHeight)
            {
                labelRect.y = k_HeaderHeight + k_EntryHeight;
                GUI.Label(labelRect, Styles.rootLabel);
            }

            // Draw vertical splits
            Rect splitBar = new Rect(overridesX - k_SplitWidth, k_HeaderHeight, k_SplitWidth, (numRows + 1) * k_EntryHeight);
            EditorGUI.DrawRect(splitBar, Colors.headerBackground);
            splitBar.x = locksX - k_SplitWidth;
            EditorGUI.DrawRect(splitBar, Colors.headerBackground);

            return height;
        }

        float DrawChildrenLabel(float yMin)
        {
            var labelRect = new Rect(k_OffsetX, yMin, 100, k_EntryHeight);
            if (target.isVariant)
                displayChildren = EditorGUI.Foldout(labelRect, displayChildren, Styles.childrenLabel, true);
            else
                EditorGUI.LabelField(labelRect, Styles.childrenLabel);

            if (displayChildren)
            {
                if (listArea == null)
                    InitListArea();

                labelRect = new Rect(labelRect.x + 58 + (target.isVariant ? 12 : 0), labelRect.y + 2, k_SliderWidth, EditorGUI.kSingleLineHeight);
                if (results.Length != 0)
                    EditorGUI.LabelField(labelRect, results.Length.ToString(), Styles.boldNumber);

                EditorGUI.BeginChangeCheck();
                labelRect.x = windowWidth - k_OffsetX - k_SliderWidth;
                var newGridSize = (int)GUI.HorizontalSlider(labelRect, listArea.gridSize, listArea.minGridSize, listArea.maxGridSize);
                if (EditorGUI.EndChangeCheck())
                    listArea.gridSize = newGridSize;
            }

            return k_EntryHeight;
        }

        void DrawChildren(float yMin)
        {
            var backgroundRect = new Rect(0, yMin, windowWidth, k_SearchHeight);
            GUI.Label(backgroundRect, GUIContent.none, Styles.searchBackground);

            EditorGUI.BeginChangeCheck();
            var searchRect = new Rect(k_OffsetX + k_Padding, backgroundRect.y + k_Padding, windowWidth - 2 * k_OffsetX - k_Padding, Styles.searchFieldStyle.fixedHeight);
            searchFilterString = EditorGUI.ToolbarSearchField(searchRect, searchFilterString, false);
            if (EditorGUI.EndChangeCheck())
                debounce.Execute();

            if (enumerator != null)
                Search();

            yMin = searchRect.height + (listArea.gridSize < k_MinIconSize ? 11f : 0f);
            var listRect = new Rect(k_Padding, searchRect.y + yMin, windowWidth - 2 * k_Padding, k_SearchHeight - yMin - k_Padding);

            int listKeyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);
            listArea.OnGUI(listRect, listKeyboardControlID);

            if (enumerator == null && results.Length == 0)
            {
                var labelRect = new Rect(noResultsX, backgroundRect.y + 69f, windowWidth, EditorGUI.kSingleLineHeight);
                EditorGUI.LabelField(backgroundRect, searchFilter.nameFilter.Length == 0 ? Styles.noChildrenLabel : Styles.noResultsLabel, Styles.centered);
            }
        }

        void InitListArea()
        {
            listArea = new ObjectListArea(s_ListAreaState, editorWindow, false)
            {
                allowDeselection = true,
                allowMultiSelect = false,
                allowRenaming = false,
                allowBuiltinResources = true,
            };

            listArea.itemSelectedCallback += (bool doubleClicked) =>
            {
                if (listArea.GetSelection().Length == 0)
                    return;
                var selection = listArea.GetSelection()[0];
                GUIUtility.keyboardControl = GUIUtility.GetControlID(FocusType.Keyboard);
                if (doubleClicked)
                {
                    Selection.SetActiveObjectWithContext(EditorUtility.InstanceIDToObject(selection), null);
                    Event.current.Use();
                    editorWindow.Close();
                    GUIUtility.ExitGUI();
                }
                else
                {
                    EditorGUIUtility.PingObject(selection);
                    Event.current.Use();
                }
            };

            SearchFilterChanged();
        }

        static IEnumerator<HierarchyProperty> FindInAllAssets(SearchFilter searchFilter)
        {
            var rootPaths = new List<string>();
            rootPaths.Add("Assets");
            foreach (var package in PackageManagerUtilityInternal.GetAllVisiblePackages(false))
            {
                if (package.source == PackageManager.PackageSource.Local)
                    rootPaths.Add(package.assetPath);
            }

            foreach (var rootPath in rootPaths)
            {
                var property = new HierarchyProperty(rootPath, false);
                property.SetSearchFilter(searchFilter);
                while (property.Next(null))
                    yield return property;
            }
        }

        void SearchFilterChanged()
        {
            searchFilter.nameFilter = searchFilterString;

            var size = GetWindowSize();
            var rect = new Rect(0, size.y - k_SearchHeight, size.x, k_SearchHeight);

            listArea.Init(rect, HierarchyType.Assets, new SearchFilter(), true, SearchService.SearchSessionOptions.Default);
            enumerator = FindInAllAssets(searchFilter);
            results = new int[0];
        }

        void Search()
        {
            var newResults = new List<int>();

            var maxAddCount = k_MaxSearchIterationPerFrame;
            while (--maxAddCount >= 0)
            {
                if (!enumerator.MoveNext())
                {
                    enumerator = null;
                    break;
                }
                var child = InternalEditorUtility.GetLoadedObjectFromInstanceID(enumerator.Current.GetInstanceIDIfImported()) as Material;
                if (!child)
                {
                    // First check guid from file to avoid loading material in memory
                    string path = AssetDatabase.GUIDToAssetPath(enumerator.Current.guid);
                    if (EditorMaterialUtility.GetMaterialParentFromFile(path) != targetGUID)
                        continue;
                    child = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
                if (child != null && child.parent == target)
                    newResults.Add(child.GetInstanceID());
            }

            int newElements = newResults.Count;
            int i = results.Length;
            System.Array.Resize(ref results, results.Length + newElements);
            for (var j = 0; j < newElements && i < results.Length; ++j, ++i)
                results[i] = newResults[j];

            listArea.ShowObjectsInList(results);
        }

        void DisplayOverridesAndLocks(Rect rect, Material entry)
        {
            rect.x = overridesX;
            rect.width = k_OverridesWidth;
            int overrideCount = entry.overrideCount;
            GUI.Label(rect, overrideCount == 0 ? "-" : overrideCount.ToString(), Styles.centered);

            rect.x = locksX;
            rect.width = k_LocksWidth;
            int lockCount = entry.lockCount;
            GUI.Label(rect, lockCount == 0 ? "-" : lockCount.ToString(), Styles.centered);
        }

        void DoObjectLabel(Rect rect, Object entry)
        {
            DoObjectLabel(rect, entry, GUI.skin.label);
        }

        void DoObjectLabel(Rect rect, Object entry, GUIStyle style)
        {
            GUI.Label(rect, AssetPreview.GetMiniThumbnail(entry));

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                // One click shows where the referenced object is
                if (Event.current.clickCount == 1)
                {
                    GUIUtility.keyboardControl = GUIUtility.GetControlID(FocusType.Keyboard);

                    EditorGUIUtility.PingObject(entry);
                    Event.current.Use();
                }
                // Double click changes selection to referenced object
                else if (Event.current.clickCount == 2)
                {
                    if (entry)
                    {
                        Selection.SetActiveObjectWithContext(entry, null);
                        Event.current.Use();
                        editorWindow.Close();
                        GUIUtility.ExitGUI();
                    }
                }
            }

            rect.x += rect.height;
            rect.width -= rect.height;
            GUI.Label(rect, EditorGUIUtility.TempContent(entry.name, entry.name), style);
        }
    }
}
