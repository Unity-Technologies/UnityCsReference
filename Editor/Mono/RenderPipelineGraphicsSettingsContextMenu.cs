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
        void PopulateContextMenu(IRenderPipelineGraphicsSettings setting, ref GenericDropdownMenu menu);

        int priority => 0;
    }

    // This version is specialized for T only
    public interface IRenderPipelineGraphicsSettingsContextMenu<T> : IRenderPipelineGraphicsSettingsContextMenu
        where T : class, IRenderPipelineGraphicsSettings
    {
        void PopulateContextMenu(T setting, ref GenericDropdownMenu menu);
        void IRenderPipelineGraphicsSettingsContextMenu.PopulateContextMenu(IRenderPipelineGraphicsSettings setting, ref GenericDropdownMenu menu)
            => PopulateContextMenu(setting as T, ref menu);
    }

    static class RenderPipelineGraphicsSettingsContextMenuManager
    {
        // typeof(IRenderPipelineGraphicsSettings) is used for global menu entries.
        static Lazy<Dictionary<Type, List<IRenderPipelineGraphicsSettingsContextMenu>>> s_MenuEntries = new (Initialize);
       
        static Dictionary<Type, List<IRenderPipelineGraphicsSettingsContextMenu>> Initialize()
        {
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
                Sort(list);

            return menus;
        }

        static void Sort(List<IRenderPipelineGraphicsSettingsContextMenu> listToSort)
        {
            //Sorting is done first by priority, but when priority
            //are the same, we sort by type name to have a stable result.
            listToSort.Sort((m1, m2) =>
            {
                var compResult = m1.priority.CompareTo(m2.priority);
                if (compResult == 0)
                    compResult = m1.GetType().AssemblyQualifiedName.CompareTo(m2.GetType().AssemblyQualifiedName);
                return compResult;
            });
        }

        static internal void PopulateContextMenu<T>(T graphicsSettings, ref GenericDropdownMenu menu)
            where T : class, IRenderPipelineGraphicsSettings
        {
            List<IRenderPipelineGraphicsSettingsContextMenu> menuPopupators = null;
            var defaultMenuPopulators = s_MenuEntries.Value.GetValueOrDefault(typeof(IRenderPipelineGraphicsSettings));

            if (s_MenuEntries.Value.TryGetValue(graphicsSettings.GetType(), out var additionalSpecificMenuPopulators))
            {
                menuPopupators = new(defaultMenuPopulators);
                foreach (var item in additionalSpecificMenuPopulators)
                    menuPopupators.Add(item);
                Sort(menuPopupators);
            }
            else
                menuPopupators = defaultMenuPopulators;

            foreach (var menuPopulator in menuPopupators)
                menuPopulator.PopulateContextMenu(graphicsSettings, ref menu);
        }
    }

    struct ResetImplementation : IRenderPipelineGraphicsSettingsContextMenu
    {
        const string k_Label = "Reset";
        // Keeping space in case one want to modify even the Reset
        public int priority => int.MaxValue - 1;

        public void PopulateContextMenu(IRenderPipelineGraphicsSettings setting, ref GenericDropdownMenu menu)
        {
            if (menu.items.Count > 0 && !menu.items[menu.items.Count - 1].isSeparator)
                menu.AddSeparator("");

            if (EditorApplication.isPlaying)
                menu.AddDisabledItem(k_Label, false);
            else
                menu.AddItem(k_Label, false, () => Reset(setting));
        }

        static void Reset(IRenderPipelineGraphicsSettings setting)
        {
            var alreadyOpenedWindow = EditorWindow.GetWindow<ProjectSettingsWindow>();
            var renderPipelineType = RenderPipelineEditorUtility.GetPipelineTypeFromPipelineAssetType(GraphicsSettingsInspectorUtility.GetRenderPipelineAssetTypeForSelectedTab(alreadyOpenedWindow.rootVisualElement));
            RenderPipelineGraphicsSettingsManager.ResetRenderPipelineGraphicsSettings(setting.GetType(), renderPipelineType);
        }
    }
}
