// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    sealed class EditorToolbar
    {
        public const string elementClassName = "unity-editor-toolbar-element";
        public const string elementIconClassName = elementClassName + "__icon";
        public const string elementLabelClassName = elementClassName + "__label";

        string[] m_ToolbarElements;
        readonly EditorWindow m_Context;
        VisualElement m_RootVisualElement;

        public VisualElement rootVisualElement
        {
            get
            {
                if (m_RootVisualElement != null)
                    return m_RootVisualElement;
                return m_RootVisualElement = CreateOverlay(m_ToolbarElements, m_Context);
            }
        }

        public EditorToolbar(IEnumerable<string> toolbarElementIds, EditorWindow context = null)
        {
            m_Context = context;
            m_ToolbarElements = toolbarElementIds.ToArray();
        }

        public static OverlayToolbar CreateOverlay(IEnumerable<string> toolbarElementIds, EditorWindow context = null)
        {
            var root = new OverlayToolbar();

            foreach (var id in toolbarElementIds)
            {
                if (TryCreateElement(id, context, out var ve))
                    root.Add(ve);
            }

            return root;
        }

        // Used by MainToolbar, as it doesn't use the same Overlay styling
        internal void LoadToolbarElements(VisualElement root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            EditorToolbarUtility.LoadStyleSheets("EditorToolbar", root);

            foreach (var id in m_ToolbarElements)
            {
                if(TryCreateElement(id, m_Context, out var ve))
                    root.Add(ve);
            }
        }

        static bool TryCreateElement(string id, EditorWindow ctx, out VisualElement ve)
        {
            if (EditorToolbarManager.instance.TryCreateElementFromId(ctx, id, out ve))
            {
                if (ve is IAccessContainerWindow visualWithContext)
                    visualWithContext.containerWindow = ctx;
                ve.AddToClassList(elementClassName);
                return true;
            }

            return false;
        }
    }
}
