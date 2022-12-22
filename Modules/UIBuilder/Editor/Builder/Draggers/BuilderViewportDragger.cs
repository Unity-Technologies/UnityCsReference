// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderViewportDragger : BuilderHierarchyDragger
    {
        public BuilderViewportDragger(
            BuilderPaneWindow paneWindow,
            VisualElement root, BuilderSelection selection,
            BuilderViewport viewport = null, BuilderParentTracker parentTracker = null)
            : base(paneWindow, root, selection, viewport, parentTracker)
        {
        }

        protected override VisualElement ExplorerGetDragPreviewFromTarget(VisualElement target, Vector2 mousePosition)
        {
            var picked = viewport.PickElement(mousePosition);
            return picked;
        }
    }
}
