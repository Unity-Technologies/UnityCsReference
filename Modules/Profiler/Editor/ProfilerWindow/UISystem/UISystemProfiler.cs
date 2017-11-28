// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    internal class UISystemProfiler
    {
        private readonly SplitterState m_TreePreviewHorizontalSplitState = new SplitterState(new[] {70f, 30f}, new[] {100, 100}, null);

        private Material m_CompositeOverdrawMaterial;
        private MultiColumnHeaderState m_MulticolumnHeaderState;
        private UISystemProfilerRenderService m_RenderService;
        private UISystemProfilerTreeView m_TreeViewControl;
        private UISystemProfilerTreeView.State m_UGUIProfilerTreeViewState;
        private ZoomableArea m_ZoomablePreview;

        private UISystemPreviewWindow m_DetachedPreview;

        private int currentFrame = 0;

        internal void DrawUIPane(ProfilerWindow win, ProfilerArea profilerArea, UISystemProfilerChart detailsChart)
        {
            InitIfNeeded(win);

            EditorGUILayout.BeginVertical();

            if (m_DetachedPreview != null && !m_DetachedPreview)
                m_DetachedPreview = null;
            bool detachPreview = m_DetachedPreview;

            if (!detachPreview)
            {
                GUILayout.BeginHorizontal(); // Horizontal render
                SplitterGUILayout.BeginHorizontalSplit(m_TreePreviewHorizontalSplitState);
            }

            var treeRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            treeRect.yMin -= EditorGUIUtility.standardVerticalSpacing;
            m_TreeViewControl.property = win.CreateProperty(ProfilerColumn.DontSort);

            if (!m_TreeViewControl.property.frameDataReady)
            {
                m_TreeViewControl.property.Cleanup();
                m_TreeViewControl.property = null;
                GUI.Label(treeRect, Styles.noData);
            }
            else
            {
                var newVisibleFrame = win.GetActiveVisibleFrameIndex();
                if (m_UGUIProfilerTreeViewState != null && m_UGUIProfilerTreeViewState.lastFrame != newVisibleFrame)
                {
                    currentFrame = ProfilerDriver.lastFrameIndex - newVisibleFrame;

                    m_TreeViewControl.Reload();
                }
                m_TreeViewControl.OnGUI(treeRect);
                m_TreeViewControl.property.Cleanup();
            }

            if (!detachPreview)
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.ExpandWidth(true)))
                    {
                        detachPreview = GUILayout.Button(Styles.contentDetachRender, EditorStyles.toolbarButton, GUILayout.Width(75));
                        if (detachPreview)
                        {
                            m_DetachedPreview = EditorWindow.GetWindow<UISystemPreviewWindow>();
                            m_DetachedPreview.profiler = this;
                            m_DetachedPreview.Show();
                        }
                        DrawPreviewToolbarButtons();
                    }
                    DrawRenderUI();
                }

                GUILayout.EndHorizontal(); // Horizontal render
                SplitterGUILayout.EndHorizontalSplit(); // m_TreePreviewHorizontalSplitState

                // Draw separator
                EditorGUI.DrawRect(
                    new Rect(m_TreePreviewHorizontalSplitState.realSizes[0] + treeRect.xMin, treeRect.y, 1, treeRect.height),
                    Styles.separatorColor);
            }

            EditorGUILayout.EndVertical();

            if (m_DetachedPreview)
                m_DetachedPreview.Repaint();
        }

        internal static void DrawPreviewToolbarButtons()
        {
            PreviewBackground = (Styles.PreviewBackgroundType)EditorGUILayout.IntPopup(GUIContent.none, (int)PreviewBackground, Styles.backgroundOptions,
                    Styles.backgroundValues, EditorStyles.toolbarDropDown, GUILayout.MaxWidth(100));

            PreviewRenderMode = (Styles.RenderMode)EditorGUILayout.IntPopup(GUIContent.none, (int)PreviewRenderMode, Styles.rendermodeOptions,
                    Styles.rendermodeValues, EditorStyles.toolbarDropDown, GUILayout.MaxWidth(100));
        }

        private static Styles.RenderMode PreviewRenderMode
        {
            get { return (Styles.RenderMode)EditorPrefs.GetInt(Styles.PrefOverdraw, 0); }
            set { EditorPrefs.SetInt(Styles.PrefOverdraw, (int)value); }
        }

        private static Styles.PreviewBackgroundType PreviewBackground
        {
            get { return (Styles.PreviewBackgroundType)EditorPrefs.GetInt(Styles.PrefCheckerBoard, 0); }
            set { EditorPrefs.SetInt(Styles.PrefCheckerBoard, (int)value); }
        }

        internal void DrawRenderUI()
        {
            var previewRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            GUI.Box(previewRect, GUIContent.none);
            m_ZoomablePreview.BeginViewGUI();
            bool first = true;
            if (m_UGUIProfilerTreeViewState != null && Event.current.type == EventType.Repaint)
            {
                IList<int> selection = m_TreeViewControl.GetSelection();
                if (selection.Count > 0)
                {
                    IList<TreeViewItem> selectedRows = m_TreeViewControl.GetRowsFromIDs(selection);
                    foreach (TreeViewItem row in selectedRows)
                    {
                        Texture2D image = null;
                        var batch = row as UISystemProfilerTreeView.BatchTreeViewItem;
                        var previewRenderMode = PreviewRenderMode;
                        if (m_RenderService == null)
                            m_RenderService = new UISystemProfilerRenderService();

                        if (batch != null)
                        {
                            image = m_RenderService.GetThumbnail(currentFrame, batch.renderDataIndex, 1, previewRenderMode != Styles.RenderMode.Standard);
                        }
                        var canvas = row as UISystemProfilerTreeView.CanvasTreeViewItem;
                        if (canvas != null)
                        {
                            image = m_RenderService.GetThumbnail(currentFrame, canvas.info.renderDataIndex, canvas.info.renderDataCount,
                                    previewRenderMode != Styles.RenderMode.Standard);
                        }

                        if (previewRenderMode == Styles.RenderMode.CompositeOverdraw)
                        {
                            if (m_CompositeOverdrawMaterial == null)
                            {
                                Shader shader = Shader.Find("Hidden/UI/CompositeOverdraw");
                                if (shader)
                                    m_CompositeOverdrawMaterial = new Material(shader);
                            }
                        }

                        if (image)
                        {
                            float w = image.width;
                            float h = image.height;
                            float scaleFactor = Math.Min(previewRect.width / w, previewRect.height / h);
                            w *= scaleFactor;
                            h *= scaleFactor;
                            var imageRect = new Rect(previewRect.x + (previewRect.width - w) / 2, previewRect.y + (previewRect.height - h) / 2,
                                    w,
                                    h);

                            if (first)
                            {
                                first = false;
                                m_ZoomablePreview.rect = imageRect;
                                var previewBackground = PreviewBackground;
                                if (previewBackground == Styles.PreviewBackgroundType.Checkerboard)
                                    EditorGUI.DrawTransparencyCheckerTexture(m_ZoomablePreview.drawRect, ScaleMode.ScaleAndCrop, 0f);
                                else
                                    EditorGUI.DrawRect(m_ZoomablePreview.drawRect,
                                        previewBackground == Styles.PreviewBackgroundType.Black ? Color.black : Color.white);
                            }
                            Graphics.DrawTexture(m_ZoomablePreview.drawRect, image, m_ZoomablePreview.shownArea, 0, 0, 0, 0,
                                previewRenderMode == Styles.RenderMode.CompositeOverdraw ? m_CompositeOverdrawMaterial : EditorGUI.transparentMaterial);
                        }
                        if (previewRenderMode != Styles.RenderMode.Standard)
                            break;
                    }
                }
            }
            if (first && Event.current.type == EventType.Repaint)
                m_ZoomablePreview.rect = previewRect;
            m_ZoomablePreview.EndViewGUI();
        }

        private void InitIfNeeded(ProfilerWindow win)
        {
            if (m_ZoomablePreview != null)
                return;

            m_ZoomablePreview = new ZoomableArea(true, false)
            {
                hRangeMin = 0.0f,
                vRangeMin = 0.0f,
                hRangeMax = 1.0f,
                vRangeMax = 1.0f
            };

            m_ZoomablePreview.SetShownHRange(0, 1);
            m_ZoomablePreview.SetShownVRange(0, 1);

            m_ZoomablePreview.uniformScale = true;
            m_ZoomablePreview.scaleWithWindow = true;

            var initwidth = 100;
            var maxWidth = 200;
            m_MulticolumnHeaderState = new MultiColumnHeaderState(new[]
            {
                new MultiColumnHeaderState.Column {headerContent = EditorGUIUtility.TextContent("Object"), width = 220, maxWidth = 400, canSort = true},
                new MultiColumnHeaderState.Column {headerContent = EditorGUIUtility.TextContent("Self Batch Count"), width = initwidth, maxWidth = maxWidth},
                new MultiColumnHeaderState.Column {headerContent = EditorGUIUtility.TextContent("Cumulative Batch Count"), width = initwidth, maxWidth = maxWidth},
                new MultiColumnHeaderState.Column {headerContent = EditorGUIUtility.TextContent("Self Vertex Count"), width = initwidth, maxWidth = maxWidth},
                new MultiColumnHeaderState.Column {headerContent = EditorGUIUtility.TextContent("Cumulative Vertex Count"), width = initwidth, maxWidth = maxWidth},
                new MultiColumnHeaderState.Column
                {
                    headerContent = EditorGUIUtility.TextContent("Batch Breaking Reason"),
                    width = 220,
                    maxWidth = 400,
                    canSort = false
                },
                new MultiColumnHeaderState.Column {headerContent = EditorGUIUtility.TextContent("GameObject Count"), width = initwidth, maxWidth = 400},
                new MultiColumnHeaderState.Column {headerContent = EditorGUIUtility.TextContent("GameObjects"), width = 150, maxWidth = 400, canSort = false},
            });
            foreach (var column in m_MulticolumnHeaderState.columns)
            {
                column.sortingArrowAlignment = TextAlignment.Right;
            }

            m_UGUIProfilerTreeViewState = new UISystemProfilerTreeView.State { profilerWindow = win };
            var multiColumnHeader = new Headers(m_MulticolumnHeaderState) { canSort = true, height = 21 };
            multiColumnHeader.sortingChanged += header => { m_TreeViewControl.Reload(); };
            m_TreeViewControl = new UISystemProfilerTreeView(m_UGUIProfilerTreeViewState, multiColumnHeader);
            m_TreeViewControl.Reload();
        }

        internal class Headers : MultiColumnHeader
        {
            public Headers(MultiColumnHeaderState state) : base(state)
            {
            }

            protected override void ColumnHeaderGUI(MultiColumnHeaderState.Column column, Rect headerRect, int columnIndex)
            {
                GUIStyle style = GetStyleWrapped(column.headerTextAlignment);

                float labelHeight = style.CalcHeight(column.headerContent, headerRect.width);
                Rect labelRect = headerRect;
                labelRect.yMin += labelRect.height - labelHeight - 1;
                GUI.Label(labelRect, column.headerContent, style);
                if (canSort && column.canSort)
                {
                    SortingButton(column, headerRect, columnIndex);
                }
            }

            internal override void DrawDivider(Rect dividerRect, MultiColumnHeaderState.Column column)
            {
                // do NOT draw the divider
                //base.DrawDivider(dividerRect, column);
            }

            internal override Rect GetArrowRect(MultiColumnHeaderState.Column column, Rect headerRect)
            {
                return new Rect(headerRect.xMax - DefaultStyles.arrowStyle.fixedWidth, headerRect.y + 5, DefaultStyles.arrowStyle.fixedWidth, headerRect.height - 10);
            }

            private GUIStyle GetStyleWrapped(TextAlignment alignment)
            {
                switch (alignment)
                {
                    case TextAlignment.Left: return Styles.columnHeader;
                    case TextAlignment.Center: return Styles.columnHeaderCenterAligned;
                    case TextAlignment.Right: return Styles.columnHeaderRightAligned;
                    default: return Styles.columnHeader;
                }
            }
        }

        internal static class Styles
        {
            internal const string PrefCheckerBoard = "UGUIProfiler.CheckerBoard";
            internal const string PrefOverdraw = "UGUIProfiler.Overdraw";

            public static readonly GUIStyle columnHeader;
            public static readonly GUIStyle columnHeaderCenterAligned;
            public static readonly GUIStyle columnHeaderRightAligned;

            public static readonly GUIStyle background;
            public static readonly GUIStyle entryEven;
            public static readonly GUIStyle entryOdd;
            public static readonly GUIStyle header;
            public static readonly GUIContent noData;
            public static readonly GUIStyle rightHeader;
            public static GUIContent[] backgroundOptions;
            public static int[] backgroundValues;
            public static GUIContent contentDetachRender;

            public static GUIContent[] rendermodeOptions;
            public static int[] rendermodeValues;
            private static readonly Color m_SeparatorColorPro;
            private static readonly Color m_SeparatorColorNonPro;

            public static Color separatorColor
            {
                get
                {
                    return EditorGUIUtility.isProSkin ? m_SeparatorColorPro : m_SeparatorColorNonPro;
                }
            }

            static Styles()
            {
                entryOdd = "OL EntryBackOdd";
                entryEven = "OL EntryBackEven";
                rightHeader = "OL title TextRight";

                columnHeader = "OL title";
                columnHeaderCenterAligned = new GUIStyle(columnHeader)
                {
                    alignment = TextAnchor.MiddleCenter
                };
                columnHeaderRightAligned = new GUIStyle(columnHeader)
                {
                    alignment = TextAnchor.MiddleRight
                };

                background = "OL Box";
                header = "OL title";
                header.alignment = TextAnchor.MiddleLeft;

                noData = EditorGUIUtility.TextContent("No frame data available - UI profiling is only available when profiling in the editor");

                contentDetachRender = new GUIContent("Detach");

                backgroundOptions = new[] {new GUIContent("Checkerboard"), new GUIContent("Black"), new GUIContent("White")};
                backgroundValues = new[]
                {
                    (int)PreviewBackgroundType.Checkerboard,
                    (int)PreviewBackgroundType.Black,
                    (int)PreviewBackgroundType.White
                };

                rendermodeOptions = new[]
                {
                    new GUIContent("Standard"), new GUIContent("Overdraw"), new GUIContent("Composite overdraw")
                };
                rendermodeValues = new[]
                {
                    (int)RenderMode.Standard, (int)RenderMode.Overdraw, (int)RenderMode.CompositeOverdraw
                };
                m_SeparatorColorPro = new Color(0.15f, 0.15f, 0.15f);
                m_SeparatorColorNonPro = new Color(0.6f, 0.6f, 0.6f);
            }

            internal enum RenderMode
            {
                Standard,
                Overdraw,
                CompositeOverdraw
            }

            internal enum PreviewBackgroundType
            {
                Checkerboard,
                Black,
                White
            }
        }

        public void CurrentAreaChanged(ProfilerArea profilerArea)
        {
            if (profilerArea != ProfilerArea.UI && profilerArea != ProfilerArea.UIDetails)
            {
                if (m_DetachedPreview)
                {
                    m_DetachedPreview.Close();
                    m_DetachedPreview = null;
                }
                if (m_RenderService != null)
                {
                    m_RenderService.Dispose();
                    m_RenderService = null;
                }
            }
        }
    }

    internal class UISystemPreviewWindow : EditorWindow
    {
        public UISystemProfiler profiler;

        public void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            UISystemProfiler.DrawPreviewToolbarButtons();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            if (profiler == null)
                Close();
            else
                profiler.DrawRenderUI();
        }
    }
}
