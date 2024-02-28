// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Inspector.GraphicsSettingsInspectors;
using UnityEditor.Rendering.Settings;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    // We need a base non generic to fetch with TypeCache. But User should use the
    // generic type as the generic is used to retrieve the IRenderPipelineGraphicsSettings
    // used.
    // This version indicates that it will be applied to any IRenderPipelineGraphicsSettings
    public interface IRenderPipelineGraphicsSettingsContextMenu
    {
        void PopulateContextMenu(IRenderPipelineGraphicsSettings setting, PropertyDrawer drawer, ref GenericMenu menu);

        int priority => 0;
    }

    // This version is specialized for T only
    public interface IRenderPipelineGraphicsSettingsContextMenu<T> : IRenderPipelineGraphicsSettingsContextMenu
        where T : class, IRenderPipelineGraphicsSettings
    {
        void PopulateContextMenu(T setting, PropertyDrawer drawer, ref GenericMenu menu);
        void IRenderPipelineGraphicsSettingsContextMenu.PopulateContextMenu(IRenderPipelineGraphicsSettings setting, PropertyDrawer drawer, ref GenericMenu menu)
            => PopulateContextMenu(setting as T, drawer, ref menu);
    }

    struct RenderPipelineGraphicsSettingsContextMenuComparer : IComparer<IRenderPipelineGraphicsSettingsContextMenu>, IComparer<(IRenderPipelineGraphicsSettingsContextMenu, IEnumerable<IRenderPipelineGraphicsSettings>)>
    {
        //Sorting is done first by priority, but when priority
        //are the same, we sort by type name to have a stable result.
        public int Compare(IRenderPipelineGraphicsSettingsContextMenu m1, IRenderPipelineGraphicsSettingsContextMenu m2)
        {
            var compResult = m1.priority.CompareTo(m2.priority);
            if (compResult == 0)
                compResult = m1.GetType().FullName.CompareTo(m2.GetType().FullName);
            return compResult;
        }

        public int Compare((IRenderPipelineGraphicsSettingsContextMenu, IEnumerable<IRenderPipelineGraphicsSettings>) m1, (IRenderPipelineGraphicsSettingsContextMenu, IEnumerable<IRenderPipelineGraphicsSettings>) m2)
            => Compare(m1.Item1, m2.Item1);
    }

    static class RenderPipelineGraphicsSettingsContextMenuManager
    {
        // typeof(IRenderPipelineGraphicsSettings) is used for global menu entries.
        static Lazy<Dictionary<Type, List<IRenderPipelineGraphicsSettingsContextMenu>>> s_MenuEntries = new (Initialize);
       
        static Dictionary<Type, List<IRenderPipelineGraphicsSettingsContextMenu>> Initialize()
        {
            RenderPipelineGraphicsSettingsContextMenuComparer comparer = new();
            Dictionary<Type, List<IRenderPipelineGraphicsSettingsContextMenu>> menus = new();

            Type GetTargetGraphicsSettingsType(Type menuType)
            {
                var interfaces = menuType.GetInterfaces();
                foreach (var @interface in interfaces)
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IRenderPipelineGraphicsSettingsContextMenu<>))
                        return @interface.GetGenericArguments()[0];
                return typeof(IRenderPipelineGraphicsSettings);
            }

            void AddToList(Type menuType)
            {
                var instance = Activator.CreateInstance(menuType, true) as IRenderPipelineGraphicsSettingsContextMenu;
                
                Type rpgsType = GetTargetGraphicsSettingsType(menuType);
                if (!menus.ContainsKey(rpgsType))
                    menus[rpgsType] = new();
                menus[rpgsType].Add(instance);
            }

            var menuTypes = TypeCache.GetTypesDerivedFrom<IRenderPipelineGraphicsSettingsContextMenu>();
            foreach (Type menuType in menuTypes)
            {
                if (menuType.IsAbstract)
                    continue;

                AddToList(menuType);
            }

            foreach (var list in menus.Values)
                list.Sort(comparer);

            return menus;
        }

        static internal void PopulateContextMenu(IEnumerable<IRenderPipelineGraphicsSettings> graphicsSettings, SerializedProperty property, ref GenericMenu menu)
        {
            RenderPipelineGraphicsSettingsContextMenuComparer comparer = new();
            var drawer = ScriptAttributeUtility.GetHandler(property).propertyDrawer;
            var defaultMenuPopulators = s_MenuEntries.Value.GetValueOrDefault(typeof(IRenderPipelineGraphicsSettings));
            List<(IRenderPipelineGraphicsSettingsContextMenu populator, IEnumerable<IRenderPipelineGraphicsSettings> data)> menuPopupators = new();
            foreach(var defaultMenuPopulator in defaultMenuPopulators)
                menuPopupators.Add((defaultMenuPopulator, graphicsSettings));

            foreach(var settings in graphicsSettings)
            {
                if (s_MenuEntries.Value.TryGetValue(settings.GetType(), out var additionalSpecificMenuPopulators))
                {
                    foreach (var item in additionalSpecificMenuPopulators)
                        menuPopupators.Add((item, new IRenderPipelineGraphicsSettings[] { settings }));
                }
            }

            menuPopupators.Sort(comparer);
            foreach (var menuPopulator in menuPopupators)
                foreach(var settings in menuPopulator.data)
                    menuPopulator.populator.PopulateContextMenu(settings, drawer, ref menu);
        }
    }

    struct ResetImplementation : IRenderPipelineGraphicsSettingsContextMenu
    {
        const string k_Label = "Reset";
        // Keeping space in case one want to modify after the Reset
        public int priority => int.MaxValue - 1;

        List<IRenderPipelineGraphicsSettings> targets;

        public void PopulateContextMenu(IRenderPipelineGraphicsSettings setting, PropertyDrawer _, ref GenericMenu menu)
        {
            if (menu.menuItems.Count > 0)
            {
                if (menu.menuItems[menu.menuItems.Count - 1].userData is ResetImplementation implementation)
                {
                    implementation.targets.Add(setting);
                    return;
                }
                else if (!menu.menuItems[menu.menuItems.Count - 1].separator)
                    menu.AddSeparator("");
            }

            if (EditorApplication.isPlaying)
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent(k_Label), false);
            else
            {
                targets = new() { setting };
                menu.AddItem(EditorGUIUtility.TrTextContent(k_Label), false, (implementation) => Reset((ResetImplementation)implementation), this);
            }
        }

        static void Reset(ResetImplementation implementation)
        {
            var alreadyOpenedWindow = EditorWindow.GetWindow<ProjectSettingsWindow>();
            var renderPipelineType = RenderPipelineEditorUtility.GetPipelineTypeFromPipelineAssetType(GraphicsSettingsInspectorUtility.GetRenderPipelineAssetTypeForSelectedTab(alreadyOpenedWindow.rootVisualElement));
            foreach (var target in implementation.targets)
                RenderPipelineGraphicsSettingsManager.ResetRenderPipelineGraphicsSettings(target.GetType(), renderPipelineType);
        }
    }
}
