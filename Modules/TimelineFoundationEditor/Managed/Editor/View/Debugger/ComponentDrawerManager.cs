// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Timeline.Foundation.ViewModel;

namespace Unity.Timeline.Foundation.View.Debugger
{
    interface IComponentDrawer
    {
        bool isShown { get; set; }

        string GetDisplayName();
        void SetPayload(ISequenceViewModel viewModel, Component component);
        void OnGUI();
    }

    class ComponentDrawerManager
    {
        Dictionary<Type, IComponentDrawer> m_Drawers = new Dictionary<Type, IComponentDrawer>();

        public void AddDrawer<TComponent>(ComponentDrawer<TComponent> drawer) where TComponent : Component
        {
            m_Drawers[typeof(TComponent)] = drawer;
        }

        public void RemoveDrawerFor<TComponent>() where TComponent : Component
        {
            m_Drawers.Remove(typeof(TComponent));
        }

        public IComponentDrawer GetDrawerFor(Component requestedComponent)
        {
            if (m_Drawers.TryGetValue(requestedComponent.GetType(), out IComponentDrawer drawer))
            {
                return drawer;
            }

            //find a drawer that matches a component subclass
            foreach ((Type componentType, IComponentDrawer componentDrawer) in m_Drawers)
            {
                if (componentType.IsInstanceOfType(requestedComponent))
                    return componentDrawer;
            }

            return null;
        }
    }
}
