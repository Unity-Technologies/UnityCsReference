// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsAuthoringFramework.InternalEditorBridge
{
    // Copy of SaveData. Used to avoid exposing SaveData to our clients.
    [Serializable]
    class OverlaySaveData
    {
        public const int invalidIndex = -1;

        public DockPosition dockPosition = DockPosition.Bottom;
        public string containerId = string.Empty;
        public bool displayed;
        public string id;
        public int index = invalidIndex;
        public string contents;

        public bool floating;
        public bool collapsed;
        public Vector2 snapOffset;
        public Vector2 snapOffsetDelta;
        public SnapCorner snapCorner;
        public Layout layout = Layout.Panel;
        public Vector2 size;
        public bool sizeOverridden;
    }

    static class OverlayBridge
    {
        public static OverlayToolbar CreateOverlay(IEnumerable<string> toolbarElements, EditorWindow containerWindow)
        {
            return EditorToolbar.CreateOverlay(toolbarElements, containerWindow);
        }

        public static void RestoreOverlay(this OverlayCanvas overlayCanvas, Overlay overlay, OverlaySaveData overlaySaveData)
        {
            var saveData = new SaveData
            {
                dockPosition = overlaySaveData.dockPosition,
                containerId = overlaySaveData.containerId,
                displayed = overlaySaveData.displayed,
                id = overlaySaveData.id,
                index = overlaySaveData.index,
                contents = overlaySaveData.contents,

#pragma warning disable CS0612 // Type or member is obsolete
                floating = overlaySaveData.floating,
                collapsed = overlaySaveData.collapsed,
                snapOffset = overlaySaveData.snapOffset,
                snapOffsetDelta = overlaySaveData.snapOffsetDelta,
                snapCorner = overlaySaveData.snapCorner,
                layout = overlaySaveData.layout,
                size = overlaySaveData.size,
                sizeOverridden = overlaySaveData.sizeOverridden
#pragma warning restore CS0612 // Type or member is obsolete
            };

            overlayCanvas.RestoreOverlay(overlay, saveData);
        }

        public static OverlaySaveData GetOverlaySaveData(this Overlay overlay)
        {
            var saveData = GetOverlaySaveDataEditor(overlay);

            return new OverlaySaveData
            {
                dockPosition = saveData.dockPosition,
                containerId = saveData.containerId,
                displayed = saveData.displayed,
                id = saveData.id,
                index = saveData.index,
                contents = saveData.contents,

#pragma warning disable CS0612 // Type or member is obsolete
                floating = saveData.floating,
                collapsed = saveData.collapsed,
                snapOffset = saveData.snapOffset,
                snapOffsetDelta = saveData.snapOffsetDelta,
                snapCorner = saveData.snapCorner,
                layout = saveData.layout,
                size = saveData.size,
                sizeOverridden = saveData.sizeOverridden
#pragma warning restore CS0612 // Type or member is obsolete
            };
        }

        // Copy of the default part of OverlayCanvas.FindSaveData(Overlay overlay).
        public static SaveData GetDefaultSaveData(Overlay overlay)
        {
            var data = new SaveData
            {
                containerId = null,
                displayed = false,
                dockPosition = DockPosition.Bottom,
                index = int.MaxValue
            };

            var attrib = overlay.GetType().GetCustomAttribute<OverlayAttribute>();

            if (attrib != null)
            {
                data.containerId = k_DockZoneContainerIDs[(int)attrib.defaultDockZone];
                data.index = attrib.defaultDockIndex;
                data.dockPosition = attrib.defaultDockPosition;
                data.displayed = attrib.defaultDisplay;

                // also apply to obsolete SaveData fields for backwards compatibility (ie, there is no
                // SaveData.contents but we still want layout and size attribute values to be forwarded)
#pragma warning disable 612
                data.layout = attrib.defaultLayout;
                data.floating = attrib.defaultDockZone == DockZone.Floating;
#pragma warning restore 612
            }

            return data;
        }

        public static void ShowOverlayMenuAtPosition(this OverlayCanvas overlayCanvas, Vector2 position)
        {
            overlayCanvas.ShowPopup<OverlayMenu>(position);
        }

        // Copied from OverlayCanvas.
        static readonly string[] k_DockZoneContainerIDs = new string[7]
        {
            "overlay-toolbar__left",
            "overlay-toolbar__right",
            "overlay-toolbar__top",
            "overlay-toolbar__bottom",
            "overlay-container--left",
            "overlay-container--right",
            "Floating"
        };

        // The SaveData constructor.
        static SaveData GetOverlaySaveDataEditor(Overlay overlay)
        {
            if (overlay.container == null || !overlay.container.GetOverlayIndex(overlay, out OverlayContainerSection _, out int containerIndex))
                containerIndex = OverlaySaveData.invalidIndex;

            return new SaveData(overlay, containerIndex);
        }
    }
}
