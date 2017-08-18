// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditorInternal;
using UnityEditor.Audio;
using UnityEditor.IMGUI.Controls;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Audio Mixer", icon = "Audio Mixer")]
    internal class AudioMixerWindow : EditorWindow, IHasCustomMenu
    {
        static AudioMixerWindow s_Instance;

        static string kAudioMixerUseRMSMetering = "AudioMixerUseRMSMetering";

        enum SectionType
        {
            MixerTree,
            GroupTree,
            ViewList,
            SnapshotList
        };

        public enum LayoutMode
        {
            Horizontal,
            Vertical
        }

        [Serializable]
        class Layout
        {
            [SerializeField]
            public SplitterState m_VerticalSplitter;
            [SerializeField]
            public SplitterState m_HorizontalSplitter;
        }

        [NonSerialized]
        bool m_Initialized;

        AudioMixerController m_Controller;
        List<AudioMixerController> m_AllControllers;

        AudioMixerChannelStripView.State m_ChannelStripViewState;
        AudioMixerChannelStripView m_ChannelStripView;
        TreeViewState m_AudioGroupTreeState;
        AudioMixerGroupTreeView m_GroupTree;
        [SerializeField] TreeViewStateWithAssetUtility m_MixersTreeState; // Use SerializeField so it is stored in the layout file (persistant)
        AudioMixersTreeView m_MixersTree;
        ReorderableListWithRenameAndScrollView.State m_ViewsState;
        AudioMixerGroupViewList m_GroupViews;
        ReorderableListWithRenameAndScrollView.State m_SnapshotState;
        AudioMixerSnapshotListView m_SnapshotListView;

        [SerializeField]
        Layout m_LayoutStripsOnTop;
        [SerializeField]
        Layout m_LayoutStripsOnRight;
        [SerializeField]
        SectionType[] m_SectionOrder = { SectionType.MixerTree, SectionType.SnapshotList, SectionType.GroupTree, SectionType.ViewList }; // default order
        [SerializeField]
        LayoutMode m_LayoutMode = LayoutMode.Vertical;  // We use vertical layout as default as it is the most compact layout
        [SerializeField]
        bool m_SortGroupsAlphabetically = false;
        [SerializeField]
        bool m_ShowReferencedBuses = true;
        [SerializeField]
        bool m_ShowBusConnections = false;
        [SerializeField]
        bool m_ShowBusConnectionsOfSelection = false;

        Vector2 m_SectionsScrollPosition = Vector2.zero;
        int m_RepaintCounter = 2;
        Vector2 m_LastSize;
        bool m_GroupsRenderedAboveSections = true;

        [NonSerialized]
        bool m_ShowDeveloperOverlays = false;

        readonly TickTimerHelper m_Ticker = new TickTimerHelper(1.0 / 20.0);

        public AudioMixerController controller { get { return m_Controller; } }

        LayoutMode layoutMode
        {
            get
            {
                return m_LayoutMode;
            }
            set
            {
                m_LayoutMode = value;
                m_RepaintCounter = 2;
            }
        }

        class GUIContents
        {
            public GUIContent rms;
            public GUIContent editSnapShots;
            public GUIContent infoText;
            public GUIContent selectAudioMixer;
            public GUIContent output;
            public GUIStyle toolbarObjectField = new GUIStyle("ShurikenObjectField");
            public GUIStyle toolbarLabel = new GUIStyle(EditorStyles.miniLabel);
            public GUIStyle mixerHeader = new GUIStyle(EditorStyles.largeLabel);

            public GUIContents()
            {
                rms = new GUIContent("RMS", "Switches between RMS (Root Mean Square) metering and peak metering. RMS is closer to the energy level and perceived loudness of the sound (hence lower than the peak meter), while peak-metering is useful for monitoring spikes in the signal that can cause clipping.");
                editSnapShots = new GUIContent("Edit in Play Mode", EditorGUIUtility.IconContent("Animation.Record", "|Are scene and inspector changes recorded into the animation curves?").image, "Edit in playmode and your changes are automatically saved. Note when editting is disabled then live values are shown.");
                infoText = new GUIContent("Create an AudioMixer asset from the Project Browser to get started");
                selectAudioMixer = new GUIContent("", "Select an Audio Mixer");
                output = new GUIContent("Output", "Select an Audio Mixer Group from another Audio Mixer to output to. If 'None' is selected then output is routed directly to the Audio Listener.");
                toolbarLabel.alignment = TextAnchor.MiddleLeft;
                toolbarObjectField.normal.textColor = toolbarLabel.normal.textColor;

                mixerHeader.fontStyle = FontStyle.Bold;
                mixerHeader.fontSize = 17;
                mixerHeader.margin = new RectOffset();
                mixerHeader.padding = new RectOffset();
                mixerHeader.alignment = TextAnchor.MiddleLeft;
                if (!EditorGUIUtility.isProSkin)
                    mixerHeader.normal.textColor = new Color(0.4f, 0.4f, 0.4f, 1.0f);
                else
                    mixerHeader.normal.textColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);
            }
        }
        private static GUIContents s_GuiContents;

        class AudioMixerPostprocessor : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
            {
                if (s_Instance != null)
                {
                    bool anyMixers = importedAssets.Any(val => val.EndsWith(".mixer"));
                    anyMixers |= deletedAssets.Any(val => val.EndsWith(".mixer"));
                    anyMixers |= movedAssets.Any(val => val.EndsWith(".mixer"));
                    anyMixers |= movedFromPath.Any(val => val.EndsWith(".mixer"));

                    if (anyMixers)
                        s_Instance.UpdateAfterAssetChange();
                }
            }
        }

        void UpdateAfterAssetChange()
        {
            if (m_Controller == null)
                return;

            m_AllControllers = FindAllAudioMixerControllers();

            m_Controller.SanitizeGroupViews();
            m_Controller.OnUnitySelectionChanged();

            if (m_GroupTree != null)
                m_GroupTree.ReloadTreeData();

            if (m_GroupViews != null)
                m_GroupViews.RecreateListControl();

            if (m_SnapshotListView != null)
                m_SnapshotListView.LoadFromBackend();

            if (m_MixersTree != null)
                m_MixersTree.ReloadTree();

            AudioMixerUtility.RepaintAudioMixerAndInspectors();
        }

        [RequiredByNativeCode]
        public static void CreateAudioMixerWindow()
        {
            var win = GetWindow<AudioMixerWindow>(typeof(ProjectBrowser));  // From usability tests we decided to auto dock together with project browser to prevent the mixer window keep going behind the main window on OSX

            if (win.m_Pos.width < 400f)
                win.m_Pos = new Rect(win.m_Pos.x, win.m_Pos.y, 800, 450f);  // Set default size if window is small
        }

        public static void RepaintAudioMixerWindow()
        {
            if (s_Instance != null)
                s_Instance.Repaint();
        }

        void Init()
        {
            if (m_Initialized)
                return;

            if (m_LayoutStripsOnTop == null)
                m_LayoutStripsOnTop = new Layout();

            if (m_LayoutStripsOnTop.m_VerticalSplitter == null || m_LayoutStripsOnTop.m_VerticalSplitter.realSizes.Length != 2)
            {
                m_LayoutStripsOnTop.m_VerticalSplitter = new SplitterState(new int[] { 65, 35 }, new int[] { 85, 105 }, null);
            }

            if (m_LayoutStripsOnTop.m_HorizontalSplitter == null || m_LayoutStripsOnTop.m_HorizontalSplitter.realSizes.Length != 4)
                m_LayoutStripsOnTop.m_HorizontalSplitter = new SplitterState(new int[] { 60, 60, 60, 60 }, new int[] { 85, 85, 85, 85 }, null);

            if (m_LayoutStripsOnRight == null)
                m_LayoutStripsOnRight = new Layout();

            if (m_LayoutStripsOnRight.m_HorizontalSplitter == null || m_LayoutStripsOnRight.m_HorizontalSplitter.realSizes.Length != 2)
                m_LayoutStripsOnRight.m_HorizontalSplitter = new SplitterState(new int[] { 30, 70 }, new int[] { 160, 160 }, null);

            if (m_LayoutStripsOnRight.m_VerticalSplitter == null || m_LayoutStripsOnRight.m_VerticalSplitter.realSizes.Length != 4)
                m_LayoutStripsOnRight.m_VerticalSplitter = new SplitterState(new int[] { 60, 60, 60, 60 }, new int[] { 100, 85, 85, 85 }, null);

            if (m_AudioGroupTreeState == null)
                m_AudioGroupTreeState = new TreeViewState();
            m_GroupTree = new AudioMixerGroupTreeView(this, m_AudioGroupTreeState);

            if (m_MixersTreeState == null)
                m_MixersTreeState = new TreeViewStateWithAssetUtility();
            m_MixersTree = new AudioMixersTreeView(this, m_MixersTreeState, GetAllControllers);

            if (m_ViewsState == null)
                m_ViewsState = new ReorderableListWithRenameAndScrollView.State();
            m_GroupViews = new AudioMixerGroupViewList(m_ViewsState);

            if (m_SnapshotState == null)
                m_SnapshotState = new ReorderableListWithRenameAndScrollView.State();
            m_SnapshotListView = new AudioMixerSnapshotListView(m_SnapshotState);

            if (m_ChannelStripViewState == null)
                m_ChannelStripViewState = new AudioMixerChannelStripView.State();
            m_ChannelStripView = new AudioMixerChannelStripView(m_ChannelStripViewState);

            OnMixerControllerChanged();

            m_Initialized = true;
        }

        List<AudioMixerController> GetAllControllers()
        {
            return m_AllControllers;
        }

        static List<AudioMixerController> FindAllAudioMixerControllers()
        {
            var result = new List<AudioMixerController>();
            var prop = new HierarchyProperty(HierarchyType.Assets);
            prop.SetSearchFilter(new SearchFilter() { classNames = new[] { "AudioMixerController" } });
            while (prop.Next(null))
            {
                var controller = prop.pptrValue as AudioMixerController;
                if (controller)
                    result.Add(controller);
            }
            return result;
        }

        public void Awake()
        {
            m_AllControllers = FindAllAudioMixerControllers();

            if (m_MixersTreeState != null)
            {
                // Clear state that are serialized in the layout file that should not survive closing/starting Unity
                m_MixersTreeState.OnAwake();
                m_MixersTreeState.selectedIDs = new List<int>();
            }
        }

        public void OnEnable()
        {
            titleContent = GetLocalizedTitleContent();

            s_Instance = this;

            Undo.undoRedoPerformed += UndoRedoPerformed;
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.projectWindowChanged += OnProjectChanged;
        }

        public void OnDisable()
        {
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            EditorApplication.projectWindowChanged -= OnProjectChanged;
        }

        void OnPauseStateChanged(PauseState state)
        {
            OnPauseOrPlayModeStateChanged();
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            OnPauseOrPlayModeStateChanged();
        }

        void OnPauseOrPlayModeStateChanged()
        {
            m_Ticker.Reset();  // ensures immediate tick on play mode change
            if (m_Controller != null)
            {
                Repaint();
            }

            EndRenaming();
        }

        void OnLostFocus()
        {
            EndRenaming();
        }

        void EndRenaming()
        {
            if (m_GroupTree != null)
                m_GroupTree.EndRenaming();

            if (m_MixersTree != null)
                m_MixersTree.EndRenaming();
        }

        public void UndoRedoPerformed()
        {
            if (m_Controller == null)
                return;

            // Undo may have deleted one of the selected groups
            m_Controller.SanitizeGroupViews();
            m_Controller.OnUnitySelectionChanged();
            m_Controller.OnSubAssetChanged();

            if (m_GroupTree != null)
                m_GroupTree.OnUndoRedoPerformed();

            if (m_GroupViews != null)
                m_GroupViews.OnUndoRedoPerformed();

            if (m_SnapshotListView != null)
                m_SnapshotListView.OnUndoRedoPerformed();

            if (m_MixersTree != null)
                m_MixersTree.OnUndoRedoPerformed();

            AudioMixerUtility.RepaintAudioMixerAndInspectors();
        }

        void OnMixerControllerChanged()
        {
            if (m_Controller)
                m_Controller.ClearEventHandlers();

            m_MixersTree.OnMixerControllerChanged(m_Controller);
            m_GroupTree.OnMixerControllerChanged(m_Controller);
            m_GroupViews.OnMixerControllerChanged(m_Controller);
            m_ChannelStripView.OnMixerControllerChanged(m_Controller);
            m_SnapshotListView.OnMixerControllerChanged(m_Controller);

            if (m_Controller)
                m_Controller.ForceSetView(m_Controller.currentViewIndex);
        }

        void OnProjectChanged()
        {
            if (m_MixersTree == null)
                Init();

            m_AllControllers = FindAllAudioMixerControllers();
            m_MixersTree.ReloadTree();
        }

        // Called from C++
        public void Update()
        {
            if (m_Ticker.DoTick())
            {
                if (EditorApplication.isPlaying || (m_ChannelStripView != null && m_ChannelStripView.requiresRepaint))
                    Repaint();
            }
        }

        void DetectControllerChange()
        {
            AudioMixerController oldController = m_Controller;
            if (Selection.activeObject is AudioMixerController)
                m_Controller = Selection.activeObject as AudioMixerController;
            if (m_Controller != oldController)
            {
                OnMixerControllerChanged();
            }
        }

        // Called from C++
        void OnSelectionChange()
        {
            if (m_Controller != null)
                m_Controller.OnUnitySelectionChanged();

            if (m_GroupTree != null)
                m_GroupTree.InitSelection(true);

            Repaint();
        }

        Dictionary<AudioMixerEffectController, AudioMixerGroupController> GetEffectMap(List<AudioMixerGroupController> allGroups)
        {
            var effectMap = new Dictionary<AudioMixerEffectController, AudioMixerGroupController>();
            foreach (var g in allGroups)
                foreach (var e in g.effects)
                    effectMap[e] = g;
            return effectMap;
        }

        void DoToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(EditorGUI.kWindowToolbarHeight));
            GUILayout.FlexibleSpace();
            if (m_Controller != null)
            {
                if (Application.isPlaying)
                {
                    Color orgColor = GUI.backgroundColor;
                    if (AudioSettings.editingInPlaymode)
                        GUI.backgroundColor = AnimationMode.animatedPropertyColor;

                    EditorGUI.BeginChangeCheck();
                    AudioSettings.editingInPlaymode = GUILayout.Toggle(AudioSettings.editingInPlaymode, s_GuiContents.editSnapShots, EditorStyles.toolbarButton);
                    if (EditorGUI.EndChangeCheck())
                        InspectorWindow.RepaintAllInspectors();

                    GUI.backgroundColor = orgColor;
                }
                GUILayout.FlexibleSpace();

                AudioMixerExposedParametersPopup.Popup(m_Controller, EditorStyles.toolbarPopup);
            }
            EditorGUILayout.EndHorizontal();
        }

        void RepaintIfNeeded()
        {
            if (m_RepaintCounter > 0)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    m_RepaintCounter--;
                    //Debug.Log ("Repainting (left: " + m_RepaintCounter + ")");
                }
                Repaint();
            }
        }

        public void OnGUI()
        {
            Init();

            if (s_GuiContents == null)
                s_GuiContents = new GUIContents();
            AudioMixerDrawUtils.InitStyles();

            DetectControllerChange();

            m_GroupViews.OnEvent();
            m_SnapshotListView.OnEvent();

            DoToolbar();

            List<AudioMixerGroupController> allGroups;
            if (m_Controller != null)
                allGroups = m_Controller.GetAllAudioGroupsSlow();
            else
                allGroups = new List<AudioMixerGroupController>();

            var effectMap = GetEffectMap(allGroups);

            m_GroupTree.UseScrollView(m_LayoutMode == LayoutMode.Horizontal);

            if (m_LayoutMode == LayoutMode.Horizontal)
                LayoutWithStripsOnTop(allGroups, effectMap);
            else
                LayoutWithStripsOnRightSideOneScrollBar(allGroups, effectMap);

            // Ensure valid layout after maximizing window
            if (m_LastSize.x != position.width || m_LastSize.y != position.height)
            {
                m_RepaintCounter = 2;
                m_LastSize = new Vector2(position.width, position.height);
            }

            RepaintIfNeeded();
        }

        void LayoutWithStripsOnRightSideOneScrollBar(List<AudioMixerGroupController> allGroups, Dictionary<AudioMixerEffectController, AudioMixerGroupController> effectMap)
        {
            // Do layouting
            SplitterState horizontalState = m_LayoutStripsOnRight.m_HorizontalSplitter;
            SplitterGUILayout.BeginHorizontalSplit(horizontalState, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            SplitterGUILayout.EndHorizontalSplit();

            float column1Width = horizontalState.realSizes[0];
            float column2Width = position.width - column1Width;
            Rect column1Rect = new Rect(0, EditorGUI.kWindowToolbarHeight, column1Width, position.height - EditorGUI.kWindowToolbarHeight);
            Rect column2Rect = new Rect(column1Width, EditorGUI.kWindowToolbarHeight, column2Width, column1Rect.height);

            // Column1

            // Background color for mixertree and views (if needed)
            if (EditorGUIUtility.isProSkin)
                EditorGUI.DrawRect(column1Rect, EditorGUIUtility.isProSkin ? new Color(0.19f, 0.19f, 0.19f) : new Color(0.6f, 0.6f, 0.6f, 0f));

            // Rects for sections
            float spacing = AudioMixerDrawUtils.kSpaceBetweenSections;
            Rect[] sectionRects = new Rect[m_SectionOrder.Length];
            const float xPos = 0f;
            float yPos = 0f;
            for (int i = 0; i < m_SectionOrder.Length; i++)
            {
                yPos += spacing;
                if (i > 0)
                    yPos += sectionRects[i - 1].height;
                sectionRects[i] = new Rect(xPos, yPos, column1Rect.width, GetHeightOfSection(m_SectionOrder[i]));

                // Adjust for left and right margins
                const float margin = 4f;
                sectionRects[i].x += margin;
                sectionRects[i].width -= margin * 2;
            }
            Rect contentRect = new Rect(0, 0, 1, sectionRects.Last().yMax);

            // Adjust for scrollbar
            if (contentRect.height > column1Rect.height)
            {
                for (int i = 0; i < sectionRects.Length; i++)
                    sectionRects[i].width -= 14;
            }

            // Scroll view
            m_SectionsScrollPosition = GUI.BeginScrollView(column1Rect, m_SectionsScrollPosition, contentRect);
            DoSections(column1Rect, sectionRects, m_SectionOrder);
            GUI.EndScrollView();

            // Column2
            m_ChannelStripView.OnGUI(column2Rect, m_ShowReferencedBuses, m_ShowBusConnections, m_ShowBusConnectionsOfSelection, allGroups, effectMap, m_SortGroupsAlphabetically, m_ShowDeveloperOverlays, m_GroupTree.ScrollToItem);

            // Vertical line (split)
            EditorGUI.DrawRect(new Rect(column1Rect.xMax - 1, EditorGUI.kWindowToolbarHeight, 1, position.height - EditorGUI.kWindowToolbarHeight), EditorGUIUtility.isProSkin ? new Color(0.15f, 0.15f, 0.15f) : new Color(0.6f, 0.6f, 0.6f));
        }

        void LayoutWithStripsOnTop(List<AudioMixerGroupController> allGroups, Dictionary<AudioMixerEffectController, AudioMixerGroupController> effectMap)
        {
            // Do layouting
            SplitterState horizontalState = m_LayoutStripsOnTop.m_HorizontalSplitter;
            SplitterState verticalState = m_LayoutStripsOnTop.m_VerticalSplitter;

            SplitterGUILayout.BeginVerticalSplit(verticalState, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (m_GroupsRenderedAboveSections)
            {
                GUILayout.BeginVertical();
                GUILayout.EndVertical();
            }
            SplitterGUILayout.BeginHorizontalSplit(horizontalState, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (!m_GroupsRenderedAboveSections)
            {
                GUILayout.BeginVertical();
                GUILayout.EndVertical();
            }
            SplitterGUILayout.EndHorizontalSplit();
            SplitterGUILayout.EndVerticalSplit();

            float channelStripYPos = m_GroupsRenderedAboveSections ? EditorGUI.kWindowToolbarHeight : EditorGUI.kWindowToolbarHeight + verticalState.realSizes[0];
            float channelStripHeight = m_GroupsRenderedAboveSections ? verticalState.realSizes[0] : verticalState.realSizes[1];

            float sectionsYPos = !m_GroupsRenderedAboveSections ? EditorGUI.kWindowToolbarHeight : EditorGUI.kWindowToolbarHeight + verticalState.realSizes[0];
            float sectionsHeight = !m_GroupsRenderedAboveSections ? verticalState.realSizes[0] : verticalState.realSizes[1];

            Rect channelStripViewRect = new Rect(0, channelStripYPos, position.width, channelStripHeight);
            Rect totalRectOfSections = new Rect(0, channelStripViewRect.yMax, position.width, position.height - channelStripViewRect.height);

            // Rects for sections
            Rect[] sectionRects = new Rect[m_SectionOrder.Length];
            const float spaceFromBottom = 12f;
            for (int i = 0; i < sectionRects.Length; i++)
            {
                float xPos = (i > 0) ? sectionRects[i - 1].xMax : 0f;
                sectionRects[i] = new Rect(xPos, sectionsYPos, horizontalState.realSizes[i], sectionsHeight - spaceFromBottom);
            }

            // Spacing between lists
            const float halfSpaceBetween = 4f;
            sectionRects[0].x += 2 * halfSpaceBetween;
            sectionRects[0].width -= 3 * halfSpaceBetween;
            sectionRects[sectionRects.Length - 1].x += halfSpaceBetween;
            sectionRects[sectionRects.Length - 1].width -= 3 * halfSpaceBetween;
            for (int i = 1; i < sectionRects.Length - 1; i++)
            {
                sectionRects[i].x += halfSpaceBetween;
                sectionRects[i].width -= halfSpaceBetween * 2;
            }

            // Do content
            DoSections(totalRectOfSections, sectionRects, m_SectionOrder);
            m_ChannelStripView.OnGUI(channelStripViewRect, m_ShowReferencedBuses, m_ShowBusConnections, m_ShowBusConnectionsOfSelection, allGroups, effectMap, m_SortGroupsAlphabetically, m_ShowDeveloperOverlays, m_GroupTree.ScrollToItem);

            // Horizontal line (split)
            EditorGUI.DrawRect(new Rect(0, EditorGUI.kWindowToolbarHeight + verticalState.realSizes[0] - 1, position.width, 1), new Color(0f, 0f, 0f, 0.4f));
        }

        float GetHeightOfSection(SectionType sectionType)
        {
            switch (sectionType)
            {
                case SectionType.MixerTree: return m_MixersTree.GetTotalHeight();
                case SectionType.SnapshotList: return m_SnapshotListView.GetTotalHeight();
                case SectionType.GroupTree: return m_GroupTree.GetTotalHeight();
                case SectionType.ViewList: return m_GroupViews.GetTotalHeight();
                default:
                    Debug.LogError("Unhandled enum value");
                    break;
            }
            return 0f;
        }

        void DoSections(Rect totalRectOfSections, Rect[] sectionRects, SectionType[] sectionOrder)
        {
            Event evt = Event.current;
            bool enabledGUI = m_Controller == null || AudioMixerController.EditingTargetSnapshot();

            for (int i = 0; i < sectionOrder.Length; ++i)
            {
                Rect sectionRect = sectionRects[i];
                if (sectionRect.height <= 0.0f)
                    continue;

                switch (sectionOrder[i])
                {
                    case SectionType.MixerTree:
                        m_MixersTree.OnGUI(sectionRect);
                        break;
                    case SectionType.SnapshotList:
                        using (new EditorGUI.DisabledScope(!enabledGUI))
                        {
                            m_SnapshotListView.OnGUI(sectionRect);
                        }
                        break;
                    case SectionType.GroupTree:
                        m_GroupTree.OnGUI(sectionRect);
                        break;
                    case SectionType.ViewList:
                        m_GroupViews.OnGUI(sectionRect);
                        break;
                    default:
                        Debug.LogError("Unhandled enum value");
                        break;
                }

                if (evt.type == EventType.ContextClick)
                {
                    Rect sectionHeaderRect = new Rect(sectionRect.x, sectionRect.y, sectionRect.width - 15f, AudioMixerDrawUtils.kSectionHeaderHeight);
                    if (sectionHeaderRect.Contains(evt.mousePosition))
                    {
                        ReorderContextMenu(sectionHeaderRect, i);
                        evt.Use();
                    }
                }
            }
        }

        void ReorderContextMenu(Rect rect, int sectionIndex)
        {
            Event evt = Event.current;
            if (Event.current.type == EventType.ContextClick && rect.Contains(evt.mousePosition))
            {
                GUIContent moveUp = new GUIContent(m_LayoutMode == LayoutMode.Horizontal ? "Move Left" : "Move Up");
                GUIContent moveDown = new GUIContent(m_LayoutMode == LayoutMode.Horizontal ? "Move Right" : "Move Down");

                GenericMenu menu = new GenericMenu();
                if (sectionIndex > 1)
                    menu.AddItem(moveUp, false, ChangeSectionOrder, new Vector2(sectionIndex, -1));
                else
                    menu.AddDisabledItem(moveUp);
                if (sectionIndex > 0 && sectionIndex < m_SectionOrder.Length - 1)
                    menu.AddItem(moveDown, false, ChangeSectionOrder, new Vector2(sectionIndex, 1));
                else
                    menu.AddDisabledItem(moveDown);
                menu.ShowAsContext();
            }
        }

        void ChangeSectionOrder(object userData)
        {
            Vector2 sectionIndexAndDirection = (Vector2)userData;
            int sectionIndex = (int)sectionIndexAndDirection.x;
            int direction = (int)sectionIndexAndDirection.y;
            int newSectionIndex = Mathf.Clamp(sectionIndex + direction, 0, m_SectionOrder.Length - 1);
            if (newSectionIndex != sectionIndex)
            {
                SectionType tmp = m_SectionOrder[sectionIndex];
                m_SectionOrder[sectionIndex] = m_SectionOrder[newSectionIndex];
                m_SectionOrder[newSectionIndex] = tmp;
            }
        }

        public MixerParameterDefinition ParamDef(string name, string desc, string units, float displayScale, float minRange, float maxRange, float defaultValue)
        {
            MixerParameterDefinition paramDef = new MixerParameterDefinition();
            paramDef.name = name;
            paramDef.description = desc;
            paramDef.units = units;
            paramDef.displayScale = displayScale;
            paramDef.minRange = minRange;
            paramDef.maxRange = maxRange;
            paramDef.defaultValue = defaultValue;
            return paramDef;
        }

        // Add items to the context menu for the AudioMixerWindow (the tree horizontal lines, upper right corner)
        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Sort groups alphabetically"), m_SortGroupsAlphabetically, delegate { m_SortGroupsAlphabetically = !m_SortGroupsAlphabetically; });
            menu.AddItem(new GUIContent("Show referenced groups"), m_ShowReferencedBuses, delegate { m_ShowReferencedBuses = !m_ShowReferencedBuses; });
            menu.AddItem(new GUIContent("Show group connections"), m_ShowBusConnections, delegate { m_ShowBusConnections = !m_ShowBusConnections; });
            if (m_ShowBusConnections)
                menu.AddItem(new GUIContent("Only highlight selected group connections"), m_ShowBusConnectionsOfSelection, delegate { m_ShowBusConnectionsOfSelection = !m_ShowBusConnectionsOfSelection; });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Vertical layout"), layoutMode == LayoutMode.Vertical, delegate { layoutMode = LayoutMode.Vertical; });
            menu.AddItem(new GUIContent("Horizontal layout"), layoutMode == LayoutMode.Horizontal, delegate { layoutMode = LayoutMode.Horizontal; });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Use RMS metering for display"), EditorPrefs.GetBool(kAudioMixerUseRMSMetering, true), delegate { EditorPrefs.SetBool(kAudioMixerUseRMSMetering, true); });
            menu.AddItem(new GUIContent("Use peak metering for display"), !EditorPrefs.GetBool(kAudioMixerUseRMSMetering, true), delegate { EditorPrefs.SetBool(kAudioMixerUseRMSMetering, false); });
            if (Unsupported.IsDeveloperBuild())
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("DEVELOPER/Groups Rendered Above"), m_GroupsRenderedAboveSections, delegate { m_GroupsRenderedAboveSections = !m_GroupsRenderedAboveSections; });
                menu.AddItem(new GUIContent("DEVELOPER/Build 10 groups"), false, delegate { m_Controller.BuildTestSetup(0, 7, 10); });
                menu.AddItem(new GUIContent("DEVELOPER/Build 20 groups"), false, delegate { m_Controller.BuildTestSetup(0, 7, 20); });
                menu.AddItem(new GUIContent("DEVELOPER/Build 40 groups"), false, delegate { m_Controller.BuildTestSetup(0, 7, 40); });
                menu.AddItem(new GUIContent("DEVELOPER/Build 80 groups"), false, delegate { m_Controller.BuildTestSetup(0, 7, 80); });
                menu.AddItem(new GUIContent("DEVELOPER/Build 160 groups"), false, delegate { m_Controller.BuildTestSetup(0, 7, 160); });
                menu.AddItem(new GUIContent("DEVELOPER/Build chain of 10 groups"), false, delegate { m_Controller.BuildTestSetup(1, 1, 10); });
                menu.AddItem(new GUIContent("DEVELOPER/Build chain of 20 groups "), false, delegate { m_Controller.BuildTestSetup(1, 1, 20); });
                menu.AddItem(new GUIContent("DEVELOPER/Build chain of 40 groups"), false, delegate { m_Controller.BuildTestSetup(1, 1, 40); });
                menu.AddItem(new GUIContent("DEVELOPER/Build chain of 80 groups"), false, delegate { m_Controller.BuildTestSetup(1, 1, 80); });
                menu.AddItem(new GUIContent("DEVELOPER/Show overlays"), m_ShowDeveloperOverlays, delegate { m_ShowDeveloperOverlays = !m_ShowDeveloperOverlays; });
            }
        }
    }

    internal class AssetSelectionPopupMenu
    {
        static public void Show(Rect buttonRect, string[] classNames, int initialSelectedInstanceID)
        {
            var menu = new GenericMenu();
            var objs = FindAssetsOfType(classNames);

            if (objs.Any())
            {
                objs.Sort((result1, result2) => EditorUtility.NaturalCompare(result1.name, result2.name));
                foreach (var obj in objs)
                {
                    var assetName = new GUIContent(obj.name);
                    bool selected = obj.GetInstanceID() == initialSelectedInstanceID;
                    menu.AddItem(assetName, selected, SelectCallback, obj);
                }
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("No Audio Mixers found in this project"));
            }
            menu.DropDown(buttonRect);
        }

        static void SelectCallback(object userData)
        {
            UnityEngine.Object obj = userData as UnityEngine.Object;
            if (obj != null)
                Selection.activeInstanceID = obj.GetInstanceID();
        }

        static List<UnityEngine.Object> FindAssetsOfType(string[] classNames)
        {
            var prop = new HierarchyProperty(HierarchyType.Assets);
            prop.SetSearchFilter(new SearchFilter() { classNames = classNames });
            var objs = new List<UnityEngine.Object>();
            while (prop.Next(null))
            {
                objs.Add(prop.pptrValue);
            }
            return objs;
        }
    }
}
