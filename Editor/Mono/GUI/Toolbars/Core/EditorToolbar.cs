// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    struct ToolbarElementDefinition
    {
        public string id { get; }
        public Type elementType { get; }
        public Type[] targetContexts { get; }

        public ToolbarElementDefinition(string id, Type elementType, Type[] targetContexts)
        {
            this.id = id;
            this.elementType = elementType;
            this.targetContexts = targetContexts;
        }
    }

    sealed class EditorToolbar
    {
        struct ToolbarElement
        {
            public string id { get; }
            public VisualElement visual { get; }

            public ToolbarElement(string id, VisualElement visual)
            {
                this.id = id;
                this.visual = visual;
            }
        }

        public const string elementClassName = "unity-editor-toolbar-element";
        public const string elementIconClassName = elementClassName + "__icon";
        public const string elementLabelClassName = elementClassName + "__label";

        List<string> m_AddedElementIds = new List<string>();

        readonly List<ToolbarElement> m_LoadedElements = new List<ToolbarElement>();
        readonly object m_Context;
        VisualElement m_Root;

        public VisualElement root => m_Root;

        public EditorToolbar(object context, VisualElement root, params string[] idAddedByDefault)
        {
            m_Context = context;
            m_Root = root;

            foreach (var toAdd in idAddedByDefault)
            {
                AddElement(toAdd, false);
            }

            //Load saved toolbar elements if it should be included
            for (var i = 0; i < m_AddedElementIds.Count; ++i)
            {
                var id = m_AddedElementIds[i];
                if (!LoadElement(id))
                    m_AddedElementIds[i] = null;
            }

            //Clear all ids that failed to load
            m_AddedElementIds.RemoveAll((id) => id == null);

            EditorToolbarUtility.LoadStyleSheets("EditorToolbar", root);
        }

        public void AddElement(string id)
        {
            AddElement(id, true);
        }

        public void AddElement(string id, VisualElement ve)
        {
            if (ContainsElement(id))
                return;

            m_AddedElementIds.Add(id);

            ve.AddToClassList(elementClassName);
            var element = new ToolbarElement(id, ve);
            root.Add(ve);
            m_LoadedElements.Add(element);

            if (ve is IEditorToolbarContext visualWithContext)
            {
                visualWithContext.context = m_Context;
            }
        }

        void AddElement(string id, bool load)
        {
            if (!EditorToolbarManager.instance.Exists(id))
            {
                Debug.LogError($"Trying to add the id '{id}' to the toolbar {GetType().FullName}. " +
                    "No element with that Id was registered using the EditorToolbarElement attribute.");
                return;
            }

            if (ContainsElement(id))
                return;

            m_AddedElementIds.Add(id);

            if (load)
                LoadElement(id);
        }

        public bool RemoveElement(string id)
        {
            if (GetElementFromId(id, out ToolbarElement element, out int index))
            {
                root.Remove(element.visual);
                m_LoadedElements.RemoveAt(index);

                return true;
            }

            return false;
        }

        public bool ContainsElement(string id)
        {
            return m_AddedElementIds.Contains(id);
        }

        bool LoadElement(string id)
        {
            if (EditorToolbarManager.instance.TryCreateElementFromId(m_Context, id, out VisualElement visual))
            {
                var element = new ToolbarElement(id, visual);
                root.Add(visual);
                m_LoadedElements.Add(element);

                if (visual is IEditorToolbarContext visualWithContext)
                {
                    visualWithContext.context = m_Context;
                }
                return true;
            }

            return false;
        }

        bool GetElementFromId(string id, out ToolbarElement element, out int index)
        {
            for (var i = 0; i < m_LoadedElements.Count; ++i)
            {
                var ele = m_LoadedElements[i];
                if (ele.id == id)
                {
                    element = ele;
                    index = i;
                    return true;
                }
            }

            element = default(ToolbarElement);
            index = -1;
            return false;
        }
    }
}
