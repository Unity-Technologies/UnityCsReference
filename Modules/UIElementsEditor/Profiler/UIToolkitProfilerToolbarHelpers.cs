// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Profiling;
using UnityEditor.UIElements.Debugger;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Internal;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Shared toolbar pieces used by both UI Toolkit profiler details views
    /// (<see cref="UIToolkitProfilerModuleDetailsView"/> and
    /// <see cref="UIToolkitDetailsProfilerModuleDetailsView"/>). Keeps the Frame Debugger / UI Toolkit
    /// Debugger / Documentation actions in one place.
    /// </summary>
    internal static class UIToolkitProfilerToolbarHelpers
    {
        // Sentinel rendered in cells (and totals) when a column has no meaningful value for the row
        // (panel-only column on a batch row, missing PANEL_METRICS chunk, sums that don't compose).
        public const string NoDataCell = "—";

        public static readonly string OpenFrameDebuggerLabel = L10n.Tr("Open Frame Debugger");
        public static readonly string OpenFrameDebuggerTooltip = L10n.Tr("Opens the Frame Debugger window to inspect draw calls.");
        public static readonly string OpenUiToolkitDebuggerLabel = L10n.Tr("Open UI Toolkit Debugger");
        public static readonly string OpenUiToolkitDebuggerTooltip = L10n.Tr("Opens the UI Toolkit Debugger window.");
        public static readonly string DocumentationTooltip = L10n.Tr("Opens the manual page for UI Toolkit profiler markers (Documentation/Manual/UIE-profiler-markers.html).");
        public static readonly string PingTooltip = L10n.Tr("Ping");

        public static ToolbarButton CreateOpenFrameDebuggerButton()
            => new ToolbarButton(OpenFrameDebugger) { text = OpenFrameDebuggerLabel, tooltip = OpenFrameDebuggerTooltip };

        public static ToolbarButton CreateOpenUIToolkitDebuggerButton()
            => new ToolbarButton(OpenUIToolkitDebugger) { text = OpenUiToolkitDebuggerLabel, tooltip = OpenUiToolkitDebuggerTooltip };

        public static ToolbarButton CreateDocumentationButton()
            => new ToolbarButton(OpenUiToolkitProfilerMarkersDocumentation)
            {
                iconImage = Background.FromTexture2D(EditorGUIUtility.LoadIcon("_Help")),
                text = string.Empty,
                tooltip = DocumentationTooltip,
            };

        /// <summary>
        /// Appends the standard Frame Debugger / UI Toolkit Debugger / Documentation buttons to
        /// <paramref name="toolbar"/>. Caller is responsible for adding any leading spacer and any
        /// trailing view-specific elements.
        /// </summary>
        public static void AddCommonButtons(Toolbar toolbar)
        {
            toolbar.Add(CreateOpenFrameDebuggerButton());
            toolbar.Add(CreateOpenUIToolkitDebuggerButton());
            toolbar.Add(CreateDocumentationButton());
        }

        public static void OpenFrameDebugger() => FrameDebuggerWindow.OpenWindow();
        public static void OpenUIToolkitDebugger() => UIElementsDebugger.OpenAndInspectWindow(null);

        public static void OpenUiToolkitProfilerMarkersDocumentation()
        {
            const string kManualRelativePath = "UIE-profiler-markers";
            var url = Help.FindHelpNamed(kManualRelativePath);
            if (!string.IsNullOrEmpty(url))
                Help.BrowseURL(url);
        }

        /// <summary>
        /// Same structure as <see cref="MultiColumnHeaderColumn"/>.CreateDefaultHeaderContent.
        /// Required when using a custom <see cref="Column.bindHeader"/>; otherwise <c>makeHeader == null</c>
        /// causes the default implementation to replace <c>bindHeader</c> with DefaultBindHeaderContent.
        /// </summary>
        public static VisualElement CreateDefaultColumnHeaderContent()
        {
            var defContent = new VisualElement() { pickingMode = PickingMode.Ignore };
            defContent.AddToClassList(MultiColumnHeaderColumn.defaultContentUssClassName);

            var icon = new Image() { name = MultiColumnHeaderColumn.iconElementName, pickingMode = PickingMode.Ignore };

            var title = new Label() { name = MultiColumnHeaderColumn.titleElementName, pickingMode = PickingMode.Ignore };
            title.AddToClassList(MultiColumnHeaderColumn.titleUssClassName);

            defContent.Add(icon);
            defContent.Add(title);

            return defContent;
        }

        /// <summary>
        /// Mirrors default multi-column header binding (<see cref="MultiColumnHeaderColumn"/>) and adds tooltips.
        /// </summary>
        public static void BindColumnHeaderWithTooltip(VisualElement headerContent, Column column, string tooltipText)
        {
            var title = headerContent.Q<Label>(MultiColumnHeaderColumn.titleElementName);
            var icon = headerContent.Q<Image>(MultiColumnHeaderColumn.iconElementName);

            headerContent.RemoveFromClassList(MultiColumnHeaderColumn.hasTitleUssClassName);

            if (title != null)
            {
                title.text = column.title;
            }

            if (!string.IsNullOrEmpty(column.title))
                headerContent.AddToClassList(MultiColumnHeaderColumn.hasTitleUssClassName);

            if (icon != null)
            {
                if (column.icon.texture != null || column.icon.sprite != null || column.icon.vectorImage != null)
                {
                    icon.image = column.icon.texture;
                    icon.sprite = column.icon.sprite;
                    icon.vectorImage = column.icon.vectorImage;
                }
                else
                {
                    icon.image = null;
                    icon.sprite = null;
                    icon.vectorImage = null;
                }
            }

            // Hang the tooltip on the framework column header so it covers the title row AND the
            // totals cell (when wrapped by MultiColumnTreeViewWithTotal). Walking ancestors instead
            // of a fixed parent-hop keeps this wrapper-agnostic.
            var headerColumn = headerContent.GetFirstAncestorOfType<MultiColumnHeaderColumn>();
            if (headerColumn != null)
                headerColumn.tooltip = tooltipText;
        }

        /// <summary>
        /// Wraps <paramref name="content"/> in a stack-positioned host that overlays a centered
        /// "no data" <see cref="Label"/> on top. Callers toggle <paramref name="overlay"/>'s
        /// <see cref="IStyle.display"/> to show/hide the empty-state message. Layered as an overlay
        /// because <see cref="BaseTreeView"/> has no <c>makeNoneElement</c>.
        /// </summary>
        public static VisualElement WrapWithEmptyOverlay(VisualElement content, string hostName, string emptyMessage, out Label overlay)
        {
            var host = new VisualElement { name = hostName };
            host.style.flexGrow = 1;
            host.Add(content);

            overlay = new Label(emptyMessage)
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                    whiteSpace = WhiteSpace.Normal,
                },
            };
            overlay.StretchToParentSize();
            host.Add(overlay);
            return host;
        }

        /// <summary>
        /// Resolves a Unity object name from frame metadata, falling back to live editor lookup and
        /// finally to the raw EntityId. Used by both UI Toolkit profiler details views.
        /// </summary>
        public static string GetPanelDisplayName(RawFrameDataView frameData, EntityId entityId)
            => GetPanelDisplayName(frameData, entityId, out _);

        /// <summary>
        /// Tallies the single per-frame PANEL_EVENTS chunk by panel EntityId into <paramref name="result"/>
        /// (cleared first). Panels with no events stay absent so callers fall through to 0 via TryGetValue.
        /// Shared by both UI Toolkit profiler details views.
        /// </summary>
        public static void CollectEventCountsByPanel(RawFrameDataView frameData, Dictionary<EntityId, int> result)
        {
            result.Clear();
            var guid = ProfilerUIToolkit.kProfilerMetadataGuid;
            var tag = ProfilerUIToolkit.kProfilerUIToolkitMetadataTagPanelEvents;
            var chunkCount = frameData.GetFrameMetaDataCount(guid, tag);
            for (var ci = 0; ci < chunkCount; ci++)
            {
                using (var events = frameData.GetFrameMetaData<UIToolkitPanelEventInfo>(guid, tag, ci))
                {
                    for (var i = 0; i < events.Length; i++)
                    {
                        var panelId = events[i].panelEntityId;
                        result.TryGetValue(panelId, out var existing);
                        result[panelId] = existing + 1;
                    }
                }
            }
        }

        /// <summary>
        /// Same as <see cref="GetPanelDisplayName(RawFrameDataView, EntityId)"/> but also resolves the
        /// object's managed <see cref="Type"/> in the same pass, for callers that need a type icon.
        /// The type comes from the live object, and only for current-session frames
        /// (<see cref="IsCurrentEditorSessionFrame"/>): captured / cross-session frames don't record a
        /// usable native type for IPanelComponent owners (the recorded native type id is 0), so type
        /// is left null and the caller is expected to fall back.
        /// </summary>
        public static string GetPanelDisplayName(RawFrameDataView frameData, EntityId entityId, out Type type)
        {
            type = null;
            if (entityId == EntityId.None)
                return L10n.Tr("(Unknown)");

            // The icon type comes from the live object, resolved only for current-session frames:
            // captured or cross-session frames would map the EntityId to an unrelated object in this
            // editor (and don't record a usable native type), so type stays null and bind falls back.
            var obj = IsCurrentEditorSessionFrame ? InternalEditorUtility.GetObjectFromEntityId(entityId) : null;
            if (obj != null)
                type = obj.GetType();

            if (frameData != null && frameData.GetUnityObjectInfo(entityId, out var info))
            {
                if (!string.IsNullOrEmpty(info.name))
                    return info.name;
                if (info.nativeTypeIndex >= 0 && info.nativeTypeIndex < frameData.GetUnityObjectNativeTypeInfoCount() &&
                    frameData.GetUnityObjectNativeTypeInfo(info.nativeTypeIndex, out var typeInfo) && !string.IsNullOrEmpty(typeInfo.name))
                    return typeInfo.name;
            }
            // Fallback to EditorUtility for editor-only objects that don't show up in frame data.
            if (obj != null)
                return string.IsNullOrEmpty(obj.name) ? obj.GetType().Name : obj.name;
            return EntityId.ToULong(entityId).ToString();
        }

        /// <summary>
        /// Whether the profiler frame currently loaded into the UI Toolkit details views belongs to
        /// this editor session. The views share one selected frame (loaded through
        /// PanelComponentsPaneController.LoadFrameMetadata, which calls
        /// <see cref="UpdateCurrentEditorSession"/>), so name/icon resolution and ping logic gate
        /// live-object lookups on this single flag. EntityIds from captured or remote-player frames
        /// resolve against a different session and must not be matched to live editor objects.
        /// </summary>
        public static bool IsCurrentEditorSessionFrame { get; private set; }

        /// <summary>
        /// Refreshes <see cref="IsCurrentEditorSessionFrame"/> for the frame being loaded. Pass null
        /// or an invalid view to mark "no current-session frame".
        /// </summary>
        public static void UpdateCurrentEditorSession(RawFrameDataView frameData)
            => IsCurrentEditorSessionFrame =
                frameData != null && frameData.valid && ProfilerDriver.FrameDataBelongsToCurrentEditorSession(frameData);

        /// <summary>Standard ping behavior: focuses the EditorWindow when applicable, otherwise selects+pings.</summary>
        public static void PingEntity(EntityId id)
        {
            var obj = InternalEditorUtility.GetObjectFromEntityId(id);
            if (obj == null)
                return;

            if (obj is DockArea dockArea)
                dockArea.actualView?.Focus();
            else if (obj is EditorWindow window)
                window.Focus();
            else
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
        }
    }
}
