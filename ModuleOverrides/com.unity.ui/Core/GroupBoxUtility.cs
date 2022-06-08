// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal static class GroupBoxUtility
    {
        static Dictionary<IGroupBox, IGroupManager> s_GroupManagers = new Dictionary<IGroupBox, IGroupManager>();
        static Dictionary<IGroupBoxOption, IGroupManager> s_GroupOptionManagerCache = new Dictionary<IGroupBoxOption, IGroupManager>();
        static readonly Type k_GenericGroupBoxType = typeof(IGroupBox<>);

        /// <summary>
        /// Registers this group option panel callbacks.
        /// </summary>
        public static void RegisterGroupBoxOption<T>(this T option) where T : VisualElement, IGroupBoxOption
        {
            var element = option as VisualElement;

            IGroupBox groupInHierarchy = null;
            var hierarchyParent = element.hierarchy.parent;
            while (hierarchyParent != null)
            {
                if (hierarchyParent is IGroupBox group)
                {
                    groupInHierarchy = group;
                    break;
                }

                hierarchyParent = hierarchyParent.hierarchy.parent;
            }

            var groupBox = groupInHierarchy ?? element.elementPanel;
            var groupManager = FindOrCreateGroupManager(groupBox);
            groupManager.RegisterOption(option);
            s_GroupOptionManagerCache[option] = groupManager;
        }

        public static void UnregisterGroupBoxOption<T>(this T option) where T : VisualElement, IGroupBoxOption
        {
            if (!s_GroupOptionManagerCache.ContainsKey(option))
                return;

            s_GroupOptionManagerCache[option].UnregisterOption(option);
            s_GroupOptionManagerCache.Remove(option);
        }

        /// <summary>
        /// Updates selection on all group options linked to this one.
        /// </summary>
        public static void OnOptionSelected<T>(this T selectedOption) where T : VisualElement, IGroupBoxOption
        {
            if (!s_GroupOptionManagerCache.ContainsKey(selectedOption))
                return;

            s_GroupOptionManagerCache[selectedOption].OnOptionSelectionChanged(selectedOption);
        }

        /// <summary>
        /// Retrieve the selected option in this group
        /// </summary>
        public static IGroupBoxOption GetSelectedOption(this IGroupBox groupBox)
        {
            return !s_GroupManagers.ContainsKey(groupBox) ? null : s_GroupManagers[groupBox].GetSelectedOption();
        }

        /// <summary>
        /// Accessor for the group manager of a specified group box implementation.
        /// Note: A group manager is only created once an option is added under its group box. It has no use otherwise.
        /// </summary>
        /// <param name="groupBox">The group box to search against.</param>
        /// <returns>The group manager associated to this group box.</returns>
        public static IGroupManager GetGroupManager(this IGroupBox groupBox)
        {
            return s_GroupManagers.ContainsKey(groupBox) ? s_GroupManagers[groupBox] : null;
        }

        static IGroupManager FindOrCreateGroupManager(IGroupBox groupBox)
        {
            if (s_GroupManagers.ContainsKey(groupBox))
            {
                return s_GroupManagers[groupBox];
            }

            Type genericType = null;
            foreach (var interfaceType in groupBox.GetType().GetInterfaces())
            {
                if (interfaceType.IsGenericType && k_GenericGroupBoxType.IsAssignableFrom(interfaceType.GetGenericTypeDefinition()))
                {
                    genericType = interfaceType.GetGenericArguments()[0];
                    break;
                }
            }

            var groupManager = genericType != null ? (IGroupManager)Activator.CreateInstance(genericType) : new DefaultGroupManager();
            groupManager.Init(groupBox);

            if (groupBox is BaseVisualElementPanel panel)
            {
                panel.panelDisposed += OnPanelDestroyed;
            }
            else if (groupBox is VisualElement visualElement)
            {
                visualElement.RegisterCallback<DetachFromPanelEvent>(OnGroupBoxDetachedFromPanel);
            }

            s_GroupManagers[groupBox] = groupManager;
            return groupManager;
        }

        static void OnGroupBoxDetachedFromPanel(DetachFromPanelEvent evt)
        {
            s_GroupManagers.Remove(evt.currentTarget as IGroupBox);
        }

        static void OnPanelDestroyed(BaseVisualElementPanel panel)
        {
            s_GroupManagers.Remove(panel);
            panel.panelDisposed -= OnPanelDestroyed;
        }
    }
}
