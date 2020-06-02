// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
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


        [SerializeField]
        List<EditorToolbar> m_LoadedToolbars;

        internal EditorToolbar GetSingleton(Type type)
        {
            if (m_LoadedToolbars == null)
                m_LoadedToolbars = new List<EditorToolbar>();
            m_LoadedToolbars = m_LoadedToolbars.Where(x => x != null).ToList();
            var res = m_LoadedToolbars.FirstOrDefault(x => x.GetType() == type);
            if (res != null)
                return res;
            res = (EditorToolbar)CreateInstance(type);
            m_LoadedToolbars.Add(res);
            return res;
        }

        [SerializeField]
        EditorToolbar m_MainToolbar;

        internal EditorToolbar mainToolbar
        {
            get
            {
                if (m_MainToolbar == null)
                    m_MainToolbar = GetSingleton(EditorUIService.instance.GetDefaultToolbarType());
                return m_MainToolbar;
            }

            set
            {
                if (value == mainToolbar)
                    return;

                if (m_MainToolbar != null)
                {
                    m_MainToolbar.m_Parent = null;
                    if (m_MainToolbar.rootVisualElement != null)
                        m_MainToolbar.rootVisualElement.RemoveFromHierarchy();
                    DestroyImmediate(m_MainToolbar);
                }

                m_MainToolbar = value == null ? GetSingleton(EditorUIService.instance.GetDefaultToolbarType()) : value;
                m_MainToolbar.m_Parent = this;

                PositionChanged(this);

                if (m_MainToolbar.rootVisualElement != null)
                {
                    ValidateWindowBackendForCurrentView();

                    var visualTree = windowBackend.visualTree as UnityEngine.UIElements.VisualElement;

                    visualTree?.Add(m_MainToolbar.rootVisualElement);
                }

                RepaintToolbar();
            }
        }

        [SerializeField]
        string m_LastLoadedLayoutName;

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
            positionChanged += PositionChanged;

            get = this;

            if (m_MainToolbar == null)
                m_MainToolbar = (EditorToolbar)CreateInstance(EditorUIService.instance.GetDefaultToolbarType());

            if (m_MainToolbar.rootVisualElement != null)
            {
                var visualTree = windowBackend.visualTree as UnityEngine.UIElements.VisualElement;
                visualTree?.Add(m_MainToolbar.rootVisualElement);
            }

            PositionChanged(this);

            m_EventInterests.wantsLessLayoutEvents = true;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            positionChanged -= PositionChanged;
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

            BeginOffsetArea(GetToolbarPosition(), GUIContent.none, GUIStyle.none);
            mainToolbar.OnGUI();
            EndOffsetArea();

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

        void PositionChanged(GUIView view)
        {
            if (m_MainToolbar.rootVisualElement != null)
                m_MainToolbar.rootVisualElement.SetSize(GetToolbarPosition().size);
        }

        public Rect GetToolbarPosition()
        {
            return position;
        }

        // @todo Remove when collab updates
        internal static void AddSubToolbar(SubToolbar subToolbar)
        {
            EditorUIService.instance.AddSubToolbar(subToolbar);
        }

        // Repaints all views, called from C++ when playmode entering is aborted
        // and when the user clicks on the playmode button.
        static void InternalWillTogglePlaymode()
        {
            InternalEditorUtility.RepaintAllViews();
        }
    }
}
