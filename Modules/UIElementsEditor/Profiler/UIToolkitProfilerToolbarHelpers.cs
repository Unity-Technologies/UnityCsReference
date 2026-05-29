// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
                title.tooltip = tooltipText;
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

            headerContent.parent.tooltip = tooltipText;
        }

        /// <summary>
        /// Resolves a Unity object name from frame metadata, falling back to live editor lookup and
        /// finally to the raw EntityId. Used by both UI Toolkit profiler details views.
        /// </summary>
        public static string GetPanelDisplayName(RawFrameDataView frameData, EntityId entityId)
        {
            if (entityId == EntityId.None)
                return L10n.Tr("(Unknown)");
            if (frameData != null && frameData.GetUnityObjectInfo(entityId, out var info))
            {
                if (!string.IsNullOrEmpty(info.name))
                    return info.name;
                if (info.nativeTypeIndex >= 0 && info.nativeTypeIndex < frameData.GetUnityObjectNativeTypeInfoCount() &&
                    frameData.GetUnityObjectNativeTypeInfo(info.nativeTypeIndex, out var typeInfo) && !string.IsNullOrEmpty(typeInfo.name))
                    return typeInfo.name;
            }
            // Fallback to EditorUtility for editor-only objects that don't show up in frame data.
            var obj = InternalEditorUtility.GetObjectFromEntityId(entityId);
            if (obj != null)
                return string.IsNullOrEmpty(obj.name) ? obj.GetType().Name : obj.name;
            return EntityId.ToULong(entityId).ToString();
        }

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
