// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Reflection;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;
using System;

namespace UnityEditor.Overlays
{
    sealed class MainToolbarOverlay : Overlay, ICreateHorizontalToolbar
    {
        internal event Action<VisualElement> afterContentRebuilt;

        const string k_ContentEmptyTag = "main-toolbar-overlay-empty";
        const string k_MainToolbarOverlayUxmlPath = "UXML/Overlays/main-toolbar-overlay.uxml";
        const string k_Tooltip = "\n\nHold {0} to drag and move";
        [NoAutoStaticsCleanup]
        static VisualTreeAsset s_MainToolbarOverlayTreeAsset = null;

        internal MethodInfo createElementMethod { get; set; }

        public OverlayToolbar CreateHorizontalToolbarContent()
        {
            OverlayToolbar toolbar = new OverlayToolbar();
            var result = createElementMethod.Invoke(null, null);
            if (result is MainToolbarElement single)
            {
                var ve = single.Rebuild();
                AddContextMenu(single, ve);
                toolbar.Add(ve);
            }
            else if (result is IEnumerable multiple)
            {
                foreach (var element in multiple)
                    if (element is MainToolbarElement data)
                    {
                        var ve = data.Rebuild();
                        AddContextMenu(data, ve);
                        toolbar.Add(ve);
                    }
                toolbar.SetupChildrenAsButtonStrip();
            }

            toolbar.AddManipulator(new ContextualMenuManipulator((evt) => ContextClickHandler(evt, null)));
            toolbar.RegisterCallback<GeometryChangedEvent>(OnContentGeometryChanged);

            afterContentRebuilt?.Invoke(toolbar);

            return toolbar;
        }

        void OnContentGeometryChanged(GeometryChangedEvent evt)
        {
            var isContentEmpty = Mathf.Approximately(evt.newRect.width, 0) && Mathf.Approximately(evt.newRect.height, 0);
            rootVisualElement.EnableInClassList(k_ContentEmptyTag, isContentEmpty);
        }

        void AddContextMenu(MainToolbarElement element, VisualElement target)
        {
            if (element.populateContextMenu != null)
            {
                target.AddManipulator(new ContextualMenuManipulator((evt) => ContextClickHandler(evt, element)));
            }
        }

        public override VisualElement CreatePanelContent()
        {
            return CreateHorizontalToolbarContent();
        }

        void ContextClickHandler(ContextualMenuPopulateEvent evt, MainToolbarElement element)
        {
            PopulateContextMenu(element, evt.menu);
        }

        internal void PopulateContextMenu(MainToolbarElement element, DropdownMenu menu)
        {
            if (element != null && element.populateContextMenu != null)
            {
                element.populateContextMenu.Invoke(menu);
                menu.AppendSeparator();
            }

            menu.AppendAction(L10n.Tr("Hide"), (action) => displayed = false);

        }

        internal override void PopulateRoot(VisualElement root)
        {
            if (!s_MainToolbarOverlayTreeAsset)
                s_MainToolbarOverlayTreeAsset = (VisualTreeAsset)EditorGUIUtility.Load(k_MainToolbarOverlayUxmlPath);

            s_MainToolbarOverlayTreeAsset.CloneTree(root);

            root.name = ussName;
            root.usageHints = UsageHints.DynamicTransform;
            root.AddToClassList(ussClassName);

            var dragger = new OverlayDragger(this);
            root.AddManipulator(dragger);

            var dockArea = new VisualElement() { name = "OverlayDockArea" };
            dockArea.pickingMode = PickingMode.Ignore;
            dockArea.StretchToParentSize();
            root.Add(dockArea);

            dockArea.Add(m_BeforeDropZone = new OverlayDropZone(this, OverlayDropZone.Placement.Before));
            dockArea.Add(m_AfterDropZone = new OverlayDropZone(this, OverlayDropZone.Placement.After));

            string key = "Ctrl";
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
                key = "Command";

            root.tooltip = L10n.Tr(displayName) + L10n.Tr(string.Format(k_Tooltip, key));

            CreateResizeTarget();
        }
    }
}
