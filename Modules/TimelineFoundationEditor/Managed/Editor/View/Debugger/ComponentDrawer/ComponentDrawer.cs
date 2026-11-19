// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Timeline.Foundation.ViewModel;
using UnityEditor;

namespace Unity.Timeline.Foundation.View.Debugger
{
    /// <summary>
    /// Use this class to add a custom view for a component in the debugger.
    /// </summary>
    /// <typeparam name="T">Type of the component that will be drawn.</typeparam>
    abstract class ComponentDrawer<T> : IComponentDrawer where T : Component
    {
        protected ISequenceViewModel viewModel { get; private set; }
        protected T component { get; private set; }

        public bool isShown
        {
            get => m_IsShown;
            set
            {
                if (m_IsShown != value)
                    SessionState.SetBool(GetSessionKey(), value);
                m_IsShown = value;
            }
        }

        bool m_IsShown;

        protected ComponentDrawer()
        {
            m_IsShown = SessionState.GetBool(GetSessionKey(), false);
        }

        public string GetDisplayName()
        {
            string baseName = typeof(T).Name;
            if (component == null)
                return baseName;

            Type componentType = component.GetType();
            if (componentType == typeof(T))
                return baseName;

            return $"{baseName} ({componentType.Name})";
        }

        string GetSessionKey()
        {
            return GetType().FullName;
        }

        void IComponentDrawer.SetPayload(ISequenceViewModel viewModel, Component component)
        {
            this.viewModel = viewModel;
            this.component = (T)component;
        }

        public abstract void OnGUI();
    }
}
