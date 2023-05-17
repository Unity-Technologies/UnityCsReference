// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor
{
    abstract class MainToolbarVisual
    {
        VisualElement m_Root;

        public VisualElement root
        {
            get
            {
                if (m_Root == null)
                    m_Root = CreateRoot();

                return m_Root;
            }
        }

        protected abstract VisualElement CreateRoot();
    }

    // The main toolbar
    class Toolbar : GUIView
    {
        const float k_ToolbarHeight = 30f;

        static class Styles
        {
            public static readonly GUIStyle appToolbar = "AppToolbar";

            public static readonly GUIStyle paneOptions = new GUIStyle("PaneOptions")
            {
                fixedHeight = k_ToolbarHeight
            };
        }

        public static Toolbar get;

        public static bool isLastShowRequestPartial = true;

        MainToolbarVisual m_MainToolbarVisual;

        [SerializeField]
        string m_LastLoadedLayoutName;

        VisualElement m_Root;

        internal static string lastLoadedLayoutName
        {
            get
            {
                if (get == null)
                {
                    return "Layout";
                }
                return string.IsNullOrEmpty(get.m_LastLoadedLayoutName) ? "Layout" : get.m_LastLoadedLayoutName;
            }
            set
            {
                if (!get)
                    return;
                get.m_LastLoadedLayoutName = value;
                get.Repaint();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EditorApplication.modifierKeysChanged += Repaint;
            get = this;
            m_EventInterests.wantsLessLayoutEvents = true;
            CreateContents();
        }

        void CreateContents()
        {
            m_MainToolbarVisual = (MainToolbarVisual)Activator.CreateInstance(typeof(DefaultMainToolbar));
            m_Root?.RemoveFromHierarchy();
            m_Root = CreateRoot();

            if (windowBackend?.visualTree is VisualElement visualTree)
            {
                visualTree.Add(m_Root);
                m_Root.Add(m_MainToolbarVisual.root);
            }

            RepaintToolbar();
        }

        protected override void OnDisable()
        {
            m_Root?.RemoveFromHierarchy();
            base.OnDisable();
            EditorApplication.modifierKeysChanged -= Repaint;
        }

        protected override bool OnFocus()
        {
            return false;
        }

        protected override void OldOnGUI()
        {
            if (Event.current.type == EventType.Repaint)
                Styles.appToolbar.Draw(new Rect(0, 0, position.width, position.height), false, false, false, false);

            //BeginOffsetArea(GetToolbarPosition(), GUIContent.none, GUIStyle.none);
            //EndOffsetArea();
        }

        static VisualElement CreateRoot()
        {
            var name = VisualElement.k_RootVisualContainerName;
            var root = new VisualElement()
            {
                name = VisualElementUtils.GetUniqueName(name),
                pickingMode = PickingMode.Ignore, // do not eat events so IMGUI gets them
                viewDataKey = name,
                renderHints = RenderHints.ClipWithScissors
            };
            root.pseudoStates |= PseudoStates.Root;
            UIElementsEditorUtility.AddDefaultEditorStyleSheets(root);
            root.style.overflow = Overflow.Hidden;
            return root;
        }

        protected override void OnBackingScaleFactorChanged()
        {
            CreateContents();
        }

        internal static void RepaintToolbar()
        {
            if (get != null)
                get.Repaint();
        }

        public float CalcHeight()
        {
            return k_ToolbarHeight;
        }

        public Rect GetToolbarPosition()
        {
            return position;
        }

        // @todo Remove when collab updates
        internal static void AddSubToolbar(SubToolbar subToolbar)
        {
            MainToolbarImguiContainer.AddDeprecatedSubToolbar(subToolbar);
        }

        // Repaints all views, called from C++ when playmode entering is aborted
        // and when the user clicks on the playmode button.
        static void InternalWillTogglePlaymode()
        {
            InternalEditorUtility.RepaintAllViews();
        }
    }
}
