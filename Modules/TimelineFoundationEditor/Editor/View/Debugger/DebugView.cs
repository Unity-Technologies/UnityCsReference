// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Timeline.Foundation.ViewModel;
using UnityEngine;
using Unity.Timeline.Foundation.CSO;
using UnityEngine.UIElements;
using Component = Unity.Timeline.Foundation.ViewModel.Component;
using UnityEditor;
using System.Linq;

namespace Unity.Timeline.Foundation.View.Debugger
{
    class DebugView : VisualElement
    {
        SequenceViewModel m_ShownVm;
        IMGUIContainer m_DebuggersView;
        ComponentDrawerManager m_Manager = new ();
        DebugLogDrawer m_ChangelogDrawer = new (true);
        List<EventLog> m_Changelog = new ();
        List<Type> m_ComponentPriority = new ();

        bool m_IsChangelogOpened = true;
        bool m_ShouldLogCommands = true;
        bool m_ShouldLogComponentChanges = false;
        static GUIStyle s_CustomFoldoutStyle;

        public DebugView()
        {
            style.paddingLeft = style.paddingTop = 10f;
            var scrollView = new ScrollView();
            Add(scrollView);

            m_DebuggersView = new IMGUIContainer();
            m_DebuggersView.onGUIHandler += OnGUI;

            scrollView.contentContainer.Add(m_DebuggersView);

            RegisterDefaultDrawers();
            RegisterDefaultPriority();
        }

        public void AddDrawer<TComponent>(ComponentDrawer<TComponent> drawer) where TComponent : Component
        {
            m_Manager.AddDrawer(drawer);
        }

        public void AddDrawer<TComponent, TDrawer>()
            where TComponent : Component
            where TDrawer : ComponentDrawer<TComponent>, new()
        {
            AddDrawer(new TDrawer());
        }

        public void RemoveDrawerFor<TComponent>() where TComponent : Component
        {
            m_Manager.RemoveDrawerFor<TComponent>();
        }

        public void AddComponentPriority<T>() where T : Component
        {
            m_ComponentPriority.Add(typeof(T));
        }

        public void ClearComponentPriority()
        {
            m_ComponentPriority.Clear();
        }

        public void Attach(SequenceViewModel vm)
        {
            Detach();

            if (vm != null)
            {
                m_ShownVm = vm;
                m_ShownVm.StateChanged += OnViewModelStateChanged;
                m_ShownVm.RegisterCommandObserver(OnCommandDispatched);
                m_ShownVm.ComponentChanged += OnComponentChanged;
            }
        }

        public void Detach()
        {
            if (m_ShownVm != null)
            {
                m_ShownVm.StateChanged -= OnViewModelStateChanged;
                m_ShownVm.ComponentChanged -= OnComponentChanged;
                m_ShownVm.RemoveCommandObserver(OnCommandDispatched);
                m_ShownVm = null;
            }
        }

        void RegisterDefaultDrawers()
        {
            AddDrawer<PlayerComponent, PlayerComponentDrawer>();
            AddDrawer<ViewComponent, ViewComponentDrawer>();
            AddDrawer<SequenceSourceComponent, SequenceComponentDrawer>();
            AddDrawer<SelectionComponent, SelectionComponentDrawer>();
            AddDrawer<TimeComponent, TimeComponentDrawer>();
            AddDrawer<ManipulationComponent, ManipulationComponentDrawer>();
        }

        void RegisterDefaultPriority()
        {
            AddComponentPriority<SequenceSourceComponent>();
            AddComponentPriority<PlayerComponent>();
            AddComponentPriority<ViewComponent>();
        }

        void DrawChangelog()
        {
            m_IsChangelogOpened = DrawFoldout("Changelog", m_IsChangelogOpened);

            if (m_IsChangelogOpened)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    m_ShouldLogCommands = GUILayout.Toggle(m_ShouldLogCommands, "Log Commands");
                    m_ShouldLogComponentChanges = GUILayout.Toggle(m_ShouldLogComponentChanges, "Log Component changes");
                    if (GUILayout.Button("Clear logs"))
                        m_Changelog.Clear();
                    GUILayout.FlexibleSpace();
                }
                m_ChangelogDrawer.DrawLog(m_Changelog);
            }
        }

        void OnGUI()
        {
            DrawComponents();
            DrawChangelog();
        }

        void DrawComponents()
        {
            if (m_ShownVm == null)
                return;

            var prioritizedComponents = new List<Component>(m_ComponentPriority.Count);

            foreach (Type componentType in m_ComponentPriority)
            {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                Component component = m_ShownVm.GetAllComponents().FirstOrDefault(componentType.IsInstanceOfType);
#pragma warning restore UA2001
                if (component != null)
                {
                    DrawComponent(m_Manager, m_ShownVm, component);
                    prioritizedComponents.Add(component);
                }
            }

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (Component component in m_ShownVm.GetAllComponents().Except(prioritizedComponents))
#pragma warning restore UA2001
            {
                DrawComponent(m_Manager, m_ShownVm, component);
            }
        }

        static void DrawComponent(ComponentDrawerManager manager, ISequenceViewModel vm, Component component)
        {
            IComponentDrawer drawer = manager.GetDrawerFor(component);
            if (drawer != null)
            {
                drawer.SetPayload(vm, component);
                drawer.isShown = DrawFoldout(drawer.GetDisplayName(), drawer.isShown);
                if (drawer.isShown)
                    drawer.OnGUI();
            }
            else
            {
                DrawEmptyComponent(component.GetType().Name);
            }
            DrawHorizontalLine(new Color(0.5f, 0.5f, 0.5f), 2);
        }

        void OnCommandDispatched(ICommand command)
        {
            if (m_ShouldLogCommands)
                m_Changelog.Add(new EventLog($"Command - {command.GetType().Name}"));
        }

        void OnComponentChanged(IStateComponent component)
        {
            if (m_ShouldLogComponentChanges)
                m_Changelog.Add(new EventLog($"Component changed - {component.GetType().Name}"));
        }

        void OnViewModelStateChanged()
        {
            m_DebuggersView.MarkDirtyRepaint();
        }

        static bool DrawFoldout(string name, bool isShown)
        {
            if (s_CustomFoldoutStyle == null)
                s_CustomFoldoutStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };

            return EditorGUILayout.Foldout(isShown, name, true, s_CustomFoldoutStyle);
        }

        static void DrawEmptyComponent(string name)
        {
            var formatted = $"{name} (no drawer assigned)";
            DrawFoldout(formatted, false);
        }

        static void DrawHorizontalLine(Color color, float height)
        {
            EditorGUILayout.Separator();
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, height), color);
            EditorGUILayout.Separator();
        }
    }
}
