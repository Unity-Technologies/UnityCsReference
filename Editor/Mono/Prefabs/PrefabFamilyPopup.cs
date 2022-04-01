// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor.SceneManagement;
using System.Linq;
using System;
using UnityEngine.Assertions;

namespace UnityEditor
{
    class PrefabFamilyPopup : PopupWindowContent
    {
        GameObject m_Target;
        string m_TargetPath;
        string m_TargetGUID;

        struct AncestorItem
        {
            public string assetPath;
            public int overrideCount;
        }
        AncestorItem[] m_AncestorItems;
        OverridesCounterForPrefabAssets m_OverridesCounter;
        int numRows => m_AncestorItems.Length;

        bool m_DisplayChildren = false;
        ObjectListArea m_ListArea;
        ObjectListAreaState m_ListAreaState = new ObjectListAreaState() { m_GridSize = 56 };
        SavedInt m_SavedGridSize = new SavedInt("PrefabFamilyPopup.GridSize", 56);
        int[] m_Results = null;
        Delayer m_Debounce;
        SearchFilter m_SearchFilter;
        string m_SearchFilterString = "";
        Vector2 m_Scroll = Vector2.zero;
        IEnumerator<HierarchyProperty> m_Enumerator = null;
        const int k_MinIconSize = 20;

        static bool s_Open = false;
        bool m_Debug = false;

        float m_WindowWidth, m_NamesWidth, m_MaxNameWidth, m_NoResultsX, m_OverridesX;

        const int k_MaxSearchIterationPerFrame = 500;
        const int k_MaxTableRows = 10;
        const float k_MinWindowWidth = 300f, k_MaxWindowWidth = 700f;
        const float k_HeaderHeight = 28f;
        const float k_EntryHeight = 20f;
        const float k_SliderWidth = 55f;
        const float k_SearchHeight = 150f;
        const float k_Padding = 3f;
        const float k_OffsetX = 6f;
        const float k_SplitWidth = 1f;
        readonly float k_MinNameWidth;
        readonly float k_TitleWidth = k_OffsetX + 50f;
        readonly float k_OverridesWidth = k_SplitWidth + EditorStyles.miniLabel.CalcSize(Styles.overridesLabel).x + 2 * k_Padding;
        readonly float k_ScrollbarWidth = GUI.skin.verticalScrollbar.fixedWidth + GUI.skin.verticalScrollbar.margin.left;
        readonly float k_ScrollbarHeight = GUI.skin.horizontalScrollbar.fixedHeight + GUI.skin.horizontalScrollbar.margin.top;


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
            public static readonly GUIContent rootLabel = EditorGUIUtility.TrTextContent("Root", "The root of the Prefab Variant hierarchy");
            public static readonly GUIContent selectedLabel = EditorGUIUtility.TrTextContent("Current", "The currently selected Prefab");
            public static readonly GUIContent titlePrefixLabel = EditorGUIUtility.TrTextContent("Variant Family of");
            public static readonly GUIContent ancestorLabel = EditorGUIUtility.TrTextContent("Ancestors");
            public static readonly GUIContent overridesLabel = EditorGUIUtility.TrTextContent("Overrides");
            public static readonly GUIContent childrenLabel = EditorGUIUtility.TrTextContent("Children");
            public static readonly GUIContent noResultsLabel = EditorGUIUtility.TrTextContent("No results");
            public static readonly GUIContent noChildrenLabel = EditorGUIUtility.TrTextContent("This Prefab doesn't have any children.\nPrefab Variants created from this Prefab\nwill be listed here.");
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

        internal PrefabFamilyPopup(GameObject target)
        {
            if (isOpen)
                throw new InvalidOperationException("PrefabFamilyPopup is already open");

            this.m_Target = target;
            m_TargetPath = AssetDatabase.GetAssetPath(target);
            m_TargetGUID = AssetDatabase.AssetPathToGUID(m_TargetPath);

            k_MinNameWidth = k_MinWindowWidth - (k_TitleWidth + k_SplitWidth + k_OverridesWidth);

            m_SearchFilter = new SearchFilter()
            {
                classNames = new string[] { "Prefab" },
                searchArea = SearchFilter.SearchArea.AllAssets
            };
            m_Debounce = Delayer.Debounce(_ =>
            {
                SearchFilterChanged();
                editorWindow.Repaint();
            });

            Init();
        }

        public override void OnOpen()
        {
            base.OnOpen();
            s_Open = true;

            if (m_Debug)
                Debug.Log("[PrefabFamilyPopup] Open");
        }

        public override void OnClose()
        {
            base.OnClose();
            s_Open = false;

            EditorApplication.update -= CalculateOverrideCountsTimeSliced;
            if (m_ListArea != null)
                m_ListArea.OnDestroy();

            if (m_Debug)
                Debug.Log("[PrefabFamilyPopup] Close");
        }

        bool isVariant { get { return PrefabUtility.GetCorrespondingObjectFromSource(m_Target) != null; } }

        public static bool isOpen => s_Open;

        public static string ObjectToGUID(Object asset)
        {
            Assert.IsNotNull(asset);

            string guid;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out guid, out long _);
            return guid;

        }

        AncestorItem[] GetAncestorItems()
        {
            if (!isVariant)
                throw new InvalidOperationException("GetAncestorItems() should only be called for prefab variants");

            var items = new List<AncestorItem>();

            var currentGUID = ObjectToGUID(m_Target);
            while (!string.IsNullOrEmpty(currentGUID))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(currentGUID);
                Assert.IsNotNull(assetPath);

                items.Add(new AncestorItem { assetPath = assetPath, overrideCount = -1 });

                var instanceID = AssetDatabase.GetMainAssetInstanceID(assetPath);
                currentGUID = PrefabUtility.GetVariantParentGUID(instanceID);
            }

            return items.ToArray();
        }


        void Init()
        {
            if (isVariant)
            {
                m_NamesWidth = k_MinNameWidth;
                m_AncestorItems = GetAncestorItems();
                foreach (var item in m_AncestorItems)
                {
                    var prefabName = System.IO.Path.GetFileNameWithoutExtension(item.assetPath);
                    m_NamesWidth = Mathf.Max(GUI.skin.label.CalcSize(EditorGUIUtility.TempContent(prefabName)).x + 40, m_NamesWidth);
                }

                m_OverridesCounter = new OverridesCounterForPrefabAssets(m_AncestorItems.Select(x => AssetDatabase.LoadAssetAtPath<GameObject>(x.assetPath)).ToList());
                EditorApplication.update += CalculateOverrideCountsTimeSliced;

                float scrollBarWidthOffset = numRows >= k_MaxTableRows ? k_ScrollbarWidth : 0;
                m_MaxNameWidth = k_MaxWindowWidth - (k_TitleWidth + k_SplitWidth + k_OverridesWidth) - scrollBarWidthOffset;

                float prevWidth = m_WindowWidth;
                if (m_NamesWidth <= k_MinNameWidth)
                    m_WindowWidth = k_MinWindowWidth;
                else if (m_NamesWidth >= m_MaxNameWidth)
                    m_WindowWidth = k_MaxWindowWidth;
                else
                    m_WindowWidth = k_TitleWidth + m_NamesWidth + k_SplitWidth + k_OverridesWidth;

                m_OverridesX = m_WindowWidth - k_OverridesWidth - scrollBarWidthOffset;
            }
            else
            {
                // Just showing children area
                var titleWidth = GUI.skin.label.CalcSize(EditorGUIUtility.TempContent(m_Target.name)).x + GUI.skin.label.CalcSize(Styles.titlePrefixLabel).x + 60;
                m_WindowWidth = Mathf.Min(k_MaxWindowWidth, Mathf.Max(titleWidth, k_MinWindowWidth));
            }

            m_NoResultsX = (m_WindowWidth - EditorStyles.label.CalcSize(Styles.noResultsLabel).x) * 0.5f;
        }

        void CalculateOverrideCountsTimeSliced()
        {
            if (m_OverridesCounter.MoveNext())
            {
                if (m_OverridesCounter.changedCount)
                {
                    for (int i = 0; i < m_AncestorItems.Count(); ++i)
                    {
                        m_AncestorItems[i].overrideCount = m_OverridesCounter.GetCurrentOverrideCount(i);
                    }

                    editorWindow.Repaint();
                }
            }
            else
            {
                EditorApplication.update -= CalculateOverrideCountsTimeSliced;
                if (m_Debug)
                    Debug.Log("[PrefabFamilyPopup] Timesliced overrides calculation done");
            }
        }

        public override Vector2 GetWindowSize()
        {
            if (m_Target == null)
            {
                editorWindow.Close();
                return Vector2.one;
            }

            var height = k_HeaderHeight;

            if (isVariant)
            {
                // Horizontal scrollbar
                if (m_NamesWidth > m_MaxNameWidth)
                    height += k_ScrollbarHeight;

                // Ancestors table
                height += Mathf.Min(numRows, k_MaxTableRows) * k_EntryHeight + k_EntryHeight;
            }

            // Children list
            height += k_EntryHeight;
            if (!isVariant || m_DisplayChildren)
                height += k_SearchHeight;

            return new Vector2(m_WindowWidth, height);
        }

        public override void OnGUI(Rect rect)
        {
            if (m_Target == null)
            {
                editorWindow.Close();
                return;
            }

            // Escape closes the window
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                editorWindow.Close();
                GUIUtility.ExitGUI();
            }

            DrawHeader();

            float height = k_HeaderHeight;
            if (isVariant)
                height += DrawVariantHierarchy();
            height += DrawChildrenLabel(height);

            if (!isVariant || m_DisplayChildren)
                DrawChildren(height);
        }

        void DrawHeader()
        {
            Rect headerRect = GUILayoutUtility.GetRect(20, m_WindowWidth, k_HeaderHeight, k_HeaderHeight);
            EditorGUI.DrawRect(headerRect, Colors.headerBackground);

            float labelSize = Styles.boldRightAligned.CalcSize(Styles.titlePrefixLabel).x;

            Rect labelRect = new Rect(k_OffsetX, headerRect.y + k_Padding, labelSize, EditorGUIUtility.singleLineHeight);
            Rect contentRect = new Rect(labelRect.x + labelRect.width + k_Padding, labelRect.y, m_WindowWidth, labelRect.height);

            GUI.Label(labelRect, Styles.titlePrefixLabel, Styles.boldRightAligned);
            DoObjectLabel(contentRect, AssetDatabase.GetAssetPath(m_Target), EditorStyles.boldLabel);

            labelRect.y = labelRect.height + 2 * k_Padding;
        }

        float DrawVariantHierarchy()
        {
            // Draw table header
            Rect entryRect = new Rect(0, k_HeaderHeight, m_WindowWidth, k_EntryHeight);
            EditorGUI.DrawRect(entryRect, Colors.rowBackground(0));

            var labelRect = entryRect;
            labelRect.x = k_TitleWidth;
            GUI.Label(labelRect, Styles.ancestorLabel, EditorStyles.miniLabel);

            labelRect.x = m_OverridesX + k_Padding;
            GUI.Label(labelRect, Styles.overridesLabel, EditorStyles.miniLabel);

            float tableHeight = Mathf.Min(numRows, k_MaxTableRows) * k_EntryHeight + (m_NamesWidth > m_MaxNameWidth ? k_ScrollbarHeight : 0);
            float realHeight = numRows * k_EntryHeight + (m_NamesWidth > m_MaxNameWidth ? k_ScrollbarHeight : 0);
            var tableRect = new Rect(0, k_HeaderHeight + k_EntryHeight, m_WindowWidth - 1, tableHeight);

            m_Scroll.y = GUI.BeginScrollView(tableRect, m_Scroll, new Rect(tableRect) { width = tableRect.width - k_ScrollbarWidth, height = realHeight }).y;
            {
                // Draw overrides and locks table
                for (int i = 0; i < m_AncestorItems.Length; i++)
                {
                    entryRect.y = k_HeaderHeight + (i+1) * k_EntryHeight;
                    EditorGUI.DrawRect(entryRect, Colors.rowBackground(i+1));

                    DisplayOverridesAndLocks(entryRect, m_AncestorItems[m_AncestorItems.Length - 1 - i]);
                }

                var scrollRect = new Rect(k_TitleWidth, k_HeaderHeight + k_EntryHeight, Mathf.Min(m_NamesWidth, m_MaxNameWidth), numRows * k_EntryHeight);
                m_Scroll.x = GUI.BeginScrollView(new Rect(scrollRect) { height = scrollRect.height + k_ScrollbarHeight }, m_Scroll, new Rect(scrollRect) { width = m_NamesWidth }).x;
                {
                    // Draw scrollable table
                    entryRect.x = k_TitleWidth;
                    for (int i = 0; i < m_AncestorItems.Length; i++)
                    {
                        entryRect.y = k_HeaderHeight + (i + 1) * k_EntryHeight;
                        var assetPath = m_AncestorItems[m_AncestorItems.Length - 1 - i].assetPath;

                        DoObjectLabel(entryRect, assetPath);
                    }
                }
                GUI.EndScrollView();


                // Draw selected label
                labelRect.x = k_OffsetX;
                labelRect.y = k_HeaderHeight + numRows * k_EntryHeight;
                labelRect.width = k_TitleWidth - labelRect.x;
                GUI.Label(labelRect, Styles.selectedLabel);

                // Draw root label
                if (labelRect.y != k_HeaderHeight + k_EntryHeight)
                {
                    labelRect.y = k_HeaderHeight + k_EntryHeight;
                    GUI.Label(labelRect, Styles.rootLabel);
                }

                // Draw vertical splits
                Rect splitBar = new Rect(m_OverridesX - k_SplitWidth, k_HeaderHeight, k_SplitWidth, (numRows + 1) * k_EntryHeight);
                EditorGUI.DrawRect(splitBar, Colors.headerBackground);
            }
            GUI.EndScrollView();

            float height = tableHeight + k_EntryHeight;
            return height;
        }

        float DrawChildrenLabel(float yMin)
        {
            var labelRect = new Rect(k_OffsetX, yMin, 100, k_EntryHeight);
            if (isVariant)
                m_DisplayChildren = EditorGUI.Foldout(labelRect, m_DisplayChildren, Styles.childrenLabel, true);
            else
                EditorGUI.LabelField(labelRect, Styles.childrenLabel);

            if (!isVariant || m_DisplayChildren)
            {
                if (m_ListArea == null)
                    InitListArea();

                labelRect = new Rect(labelRect.x + 70, labelRect.y + 2, k_SliderWidth, EditorGUI.kSingleLineHeight);
                if (m_Results.Length != 0)
                    EditorGUI.LabelField(labelRect, m_Results.Length.ToString(), Styles.boldNumber);

                EditorGUI.BeginChangeCheck();
                labelRect.x = m_WindowWidth - k_OffsetX - k_SliderWidth;
                var newGridSize = (int)GUI.HorizontalSlider(labelRect, m_ListArea.gridSize, m_ListArea.minGridSize, m_ListArea.maxGridSize);
                if (EditorGUI.EndChangeCheck())
                {
                    m_ListArea.gridSize = m_SavedGridSize.value = newGridSize;
                }
            }

            return k_EntryHeight;
        }

        void DrawChildren(float yMin)
        {
            int listKeyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);

            var backgroundRect = new Rect(0, yMin, m_WindowWidth, k_SearchHeight);
            GUI.Label(backgroundRect, GUIContent.none, Styles.searchBackground);

            EditorGUI.BeginChangeCheck();
            var searchRect = new Rect(k_OffsetX + k_Padding, backgroundRect.y + k_Padding, m_WindowWidth - 2 * k_OffsetX - k_Padding, Styles.searchFieldStyle.fixedHeight);
            m_SearchFilterString = EditorGUI.ToolbarSearchField(searchRect, m_SearchFilterString, false);
            if (EditorGUI.EndChangeCheck())
                m_Debounce.Execute();

            if (m_Enumerator != null)
                Search();

            yMin = searchRect.height + (m_ListArea.gridSize < k_MinIconSize ? 11f : 0f);
            var listRect = new Rect(k_Padding, searchRect.y + yMin, m_WindowWidth - 2 * k_Padding, k_SearchHeight - yMin - k_Padding);

            m_ListArea.OnGUI(listRect, listKeyboardControlID);

            if (m_Enumerator == null && m_Results.Length == 0)
            {
                var labelRect = new Rect(m_NoResultsX, backgroundRect.y + 69f, m_WindowWidth, EditorGUI.kSingleLineHeight);
                EditorGUI.LabelField(backgroundRect, m_SearchFilter.nameFilter.Length == 0 ? Styles.noChildrenLabel : Styles.noResultsLabel, Styles.centered);
            }
        }

        void InitListArea()
        {
            m_ListAreaState.m_GridSize = m_SavedGridSize;
            m_ListArea = new ObjectListArea(m_ListAreaState, editorWindow, false)
            {
                allowDeselection = true,
                allowMultiSelect = false,
                allowRenaming = false,
                allowBuiltinResources = true,
            };

            m_ListArea.itemSelectedCallback += (bool doubleClicked) =>
            {
                if (m_ListArea.GetSelection().Length == 0)
                    return;
                var selection = m_ListArea.GetSelection()[0];
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
            m_SearchFilter.nameFilter = m_SearchFilterString;

            var size = GetWindowSize();
            var rect = new Rect(0, size.y - k_SearchHeight, size.x, k_SearchHeight);

            m_ListArea.Init(rect, HierarchyType.Assets, new SearchFilter(), true, SearchService.SearchSessionOptions.Default);
            m_Enumerator = FindInAllAssets(m_SearchFilter);
            m_Results = new int[0];
        }

        void Search()
        {
            var newResults = new List<int>();

            var maxAddCount = k_MaxSearchIterationPerFrame;
            while (--maxAddCount >= 0)
            {
                if (!m_Enumerator.MoveNext())
                {
                    m_Enumerator = null;
                    break;
                }

                var currentInstanceID = m_Enumerator.Current.instanceID;
                var variantParentGUID = PrefabUtility.GetVariantParentGUID(currentInstanceID);
                if (variantParentGUID == m_TargetGUID)
                {
                    newResults.Add(currentInstanceID);
                }
            }

            int newCount = newResults.Count;
            int i = m_Results.Length;
            Array.Resize(ref m_Results, m_Results.Length + newCount);
            for (var j = 0; j < newCount && i < m_Results.Length; ++j, ++i)
                m_Results[i] = newResults[j];

            m_ListArea.ShowObjectsInList(m_Results);
        }

        void DisplayOverridesAndLocks(Rect rect, AncestorItem item)
        {
            rect.x = m_OverridesX;
            rect.width = k_OverridesWidth;
            string text;
            if (item.overrideCount == -1)
                text = string.Empty;
            else if (item.overrideCount == 0)
                text = "-";
            else if (item.overrideCount >= 10000)
                text = "+9999";
            else
                text = item.overrideCount.ToString();
            GUI.Label(rect, GUIContent.Temp(text, item.overrideCount.ToString()), Styles.centered);
        }

        void DoObjectLabel(Rect rect, string assetPath)
        {
            DoObjectLabel(rect, assetPath, GUI.skin.label);
        }

        void DoObjectLabel(Rect rect, string assetPath, GUIStyle style)
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                // One click shows where the referenced object is
                if (Event.current.clickCount == 1)
                {
                    GUIUtility.keyboardControl = GUIUtility.GetControlID(FocusType.Keyboard);

                    var instanceID = AssetDatabase.GetMainAssetInstanceID(assetPath);
                    EditorGUIUtility.PingObject(instanceID);
                    Event.current.Use();
                }
                // Double click changes selection to referenced object
                else if (Event.current.clickCount == 2)
                {
                    var instanceID = AssetDatabase.GetMainAssetInstanceID(assetPath);
                    Selection.activeInstanceID = instanceID;
                    Event.current.Use();
                    editorWindow.Close();
                    GUIUtility.ExitGUI();
                }
            }

            var icon = AssetDatabase.GetCachedIcon(assetPath);
            var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            GUI.Label(rect, EditorGUIUtility.TempContent(name, icon), style);
        }
    }

    [Serializable]
    class PrefabAssetInfo
    {
        [SerializeField]
        public Hash128 prefabFileHash;

        [SerializeField]
        public int overrideCount;
    }

    static class PrefabAssetStateCache
    {
        static StateCache<PrefabAssetInfo> s_StateCache = new StateCache<PrefabAssetInfo>("Library/StateCache/PrefabAssetInfo/");

        public static void SetState(string guid, PrefabAssetInfo obj)
        {
            var key = GetPrefabAssetIdentifier(guid);
            s_StateCache.SetState(key, obj);
        }

        public static PrefabAssetInfo GetState(string guid)
        {
            var id = GetPrefabAssetIdentifier(guid);
            var cachedPrefabAssetInfo = s_StateCache.GetState(id);
            if (cachedPrefabAssetInfo == null)
                return null; // not cached yet

            var currentPrefabSourceFileHash = AssetDatabase.GetSourceAssetFileHash(guid);
            if (currentPrefabSourceFileHash != cachedPrefabAssetInfo.prefabFileHash)
                return null; // cache is out of sync

            return cachedPrefabAssetInfo;
        }
        public static Hash128 GetPrefabAssetIdentifier(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                throw new ArgumentNullException(nameof(guid));

            return Hash128.Compute(guid);
        }
    }

    class OverridesCounterForPrefabAssets
    {
        List<GameObject> m_PrefabAssetRoots;
        List<int> m_OverridesCount;
        int m_CurrentAssetIndex;
        int m_CurrentStep;
        bool m_ChangedCount;

        bool m_Debug = false;

        public OverridesCounterForPrefabAssets(List<GameObject> prefabInstanceRoots)
        {
            m_PrefabAssetRoots = prefabInstanceRoots;
            m_OverridesCount = new List<int>();
            m_OverridesCount.AddRange(new int[prefabInstanceRoots.Count]);
        }

        public bool changedCount => m_ChangedCount;

        public int GetCurrentOverrideCount(int assetIndex)
        {
            return m_OverridesCount[assetIndex];
        }

        bool IsDone()
        {
            return m_CurrentAssetIndex >= m_PrefabAssetRoots.Count -1; // No more prefabs to process (last prefab is the root prefab which does not have overrides)
        }

        // Returns true if overrides count changed during the update
        public bool MoveNext()
        {
            if (IsDone())
                return false;

            var assetRoot = m_PrefabAssetRoots[m_CurrentAssetIndex];
            var startCount = m_OverridesCount[m_CurrentAssetIndex];

            switch (m_CurrentStep)
            {
                case 0:
                    {
                        // First check if we have cached overides count for the prefab
                        var path = AssetDatabase.GetAssetPath(assetRoot);
                        var guid = AssetDatabase.AssetPathToGUID(path);
                        var cachedPrefabAssetInfo = PrefabAssetStateCache.GetState(guid);
                        if (cachedPrefabAssetInfo != null)
                        {
                            m_OverridesCount[m_CurrentAssetIndex] = cachedPrefabAssetInfo.overrideCount;
                            m_ChangedCount = true;
                            m_CurrentAssetIndex++;
                            if (m_Debug)
                                Debug.Log($"[OverridesCounterForPrefabAssets] Using cached overridecount {cachedPrefabAssetInfo.overrideCount} ({path})");
                            return true;
                        }
                    }
                    break;
                case 1:
                    PropertyModification[] mods = PrefabUtility.GetPropertyModifications(assetRoot);
                    foreach (PropertyModification mod in mods)
                    {
                        if (mod.target == null)
                            continue;

                        if (!PrefabUtility.IsDefaultOverride(mod))
                            m_OverridesCount[m_CurrentAssetIndex]++;
                    }
                    break;
                case 2:
                    m_OverridesCount[m_CurrentAssetIndex] += PrefabOverridesUtility.GetAddedComponents(assetRoot).Count;
                    break;
                case 3:
                    m_OverridesCount[m_CurrentAssetIndex] += PrefabOverridesUtility.GetRemovedComponents(assetRoot).Count;
                    break;
                case 4:
                    m_OverridesCount[m_CurrentAssetIndex] += PrefabOverridesUtility.GetAddedGameObjects(assetRoot).Count;
                    break;
                default:
                    {
                        // Cache result
                        var path = AssetDatabase.GetAssetPath(assetRoot);
                        var guid = AssetDatabase.AssetPathToGUID(path);
                        var prefabAssetInfo = new PrefabAssetInfo();
                        prefabAssetInfo.overrideCount = m_OverridesCount[m_CurrentAssetIndex];
                        prefabAssetInfo.prefabFileHash = AssetDatabase.GetSourceAssetFileHash(guid);
                        PrefabAssetStateCache.SetState(guid, prefabAssetInfo);
                        if (m_Debug)
                            Debug.Log($"[OverridesCounterForPrefabAssets] Set cached overridecount {prefabAssetInfo.overrideCount} for {path}");

                        // Move to next asset
                        m_CurrentAssetIndex++;
                        m_CurrentStep = 0;
                    }
                    break;
            }

            if (m_Debug)
                Debug.Log($"[OverridesCounterForPrefabAssets] Current asset index: {m_CurrentAssetIndex}, current step: {m_CurrentStep}");

            m_CurrentStep++;

            // Simulate heavy calculation
            if (m_Debug)
                System.Threading.Thread.Sleep(500);

            m_ChangedCount = m_OverridesCount[m_CurrentAssetIndex] != startCount;

            return true;
        }
    }
}
