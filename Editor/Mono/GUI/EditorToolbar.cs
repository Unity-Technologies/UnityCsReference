// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    class MainToolbarAttribute : Attribute
    {
        string m_DisplayName;

        public string displayName
        {
            get { return m_DisplayName; }
        }

        public MainToolbarAttribute(string name)
        {
            m_DisplayName = name;
        }
    }

    class EditorToolbar : ScriptableObject
    {
        [SerializeField]
        internal bool m_DontSaveToLayout;

        [NonSerialized]
        internal GUIView m_Parent;

        VisualElement m_RootVisualElement;

        public VisualElement rootVisualElement
        {
            get
            {
                if (m_RootVisualElement != null)
                    return m_RootVisualElement;

                m_RootVisualElement = CreateRoot();
                return m_RootVisualElement;
            }
        }

        public Rect position
        {
            get { return Toolbar.get.GetToolbarPosition(); }
        }

        static VisualElement CreateRoot()
        {
            var name = "rootVisualContainer";
            var root = new VisualElement()
            {
                name = VisualElementUtils.GetUniqueName(name),
                pickingMode = PickingMode.Ignore, // do not eat events so IMGUI gets them
                viewDataKey = name,
                renderHints = RenderHints.ClipWithScissors
            };
            root.pseudoStates |= PseudoStates.Root;
            EditorUIService.instance.AddDefaultEditorStyleSheets(root);
            root.style.overflow = UnityEngine.UIElements.Overflow.Hidden;
            return root;
        }

        public virtual void OnGUI() {}

        public void Repaint()
        {
            if (m_Parent != null)
                m_Parent.Repaint();
        }

        // Static functions for setting the main app toolbar are in EditorToolbar for lack of any better place to
        // put them. However EditorToolbar could be used anywhere in the editor.
        public static Type mainToolbarType
        {
            get { return Toolbar.get.mainToolbar.GetType(); }
        }

        public static void SetMainToolbar<T>() where T : EditorToolbar
        {
            SetMainToolbar(typeof(T));
        }

        public static void SetMainToolbar(Type type)
        {
            if (!typeof(EditorToolbar).IsAssignableFrom(type))
                throw new ArgumentException("Type must be assignable to EditorToolbar");

            if (type == null)
                throw new ArgumentNullException("type");

            Toolbar.get.mainToolbar = Toolbar.get.GetSingleton(type);
        }
    }
}
