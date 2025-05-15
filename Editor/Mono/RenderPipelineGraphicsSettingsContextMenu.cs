// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Inspector.GraphicsSettingsInspectors;
using UnityEditor.Rendering.Settings;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    // We need a base non generic to fetch with TypeCache. But User should use the
    // generic type as the generic is used to retrieve the IRenderPipelineGraphicsSettings
    // used.

    #region Deprecated interface definition
    // This version indicates that it will be applied to any IRenderPipelineGraphicsSettings
    // UUM-99684: This interface is deprecated because the PropertyDrawer is cached internally by unity editor and will be null after domain reload
    [Obsolete("IRenderPipelineGraphicsSettingsContextMenu is deprecated and will be removed in a future release. Use IRenderPipelineGraphicsSettingsContextMenu2 instead. #from(6000.0)")]
    public interface IRenderPipelineGraphicsSettingsContextMenu
    {
        void PopulateContextMenu(IRenderPipelineGraphicsSettings setting, PropertyDrawer drawer, ref GenericMenu menu);

        int priority => 0;
    }

    // This version is specialized for T only
    // UUM-99684: This interface is deprecated because the PropertyDrawer is cached internally by unity editor and will be null after domain reload
    [Obsolete("IRenderPipelineGraphicsSettingsContextMenu<T> is deprecated and will be removed in a future release. Use IRenderPipelineGraphicsSettingsContextMenu2<T> instead. #from(6000.0)")]
    public interface IRenderPipelineGraphicsSettingsContextMenu<T> : IRenderPipelineGraphicsSettingsContextMenu
        where T : class, IRenderPipelineGraphicsSettings
    {
        void PopulateContextMenu(T setting, PropertyDrawer drawer, ref GenericMenu menu);
        void IRenderPipelineGraphicsSettingsContextMenu.PopulateContextMenu(IRenderPipelineGraphicsSettings setting, PropertyDrawer drawer, ref GenericMenu menu)
            => PopulateContextMenu(setting as T, drawer, ref menu);
    }

    [Obsolete]
    struct OldRenderPipelineGraphicsSettingsContextMenuComparer : IComparer<IRenderPipelineGraphicsSettingsContextMenu>, IComparer<(IRenderPipelineGraphicsSettingsContextMenu, IEnumerable<(IRenderPipelineGraphicsSettings, SerializedProperty)>)>
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

        public int Compare((IRenderPipelineGraphicsSettingsContextMenu, IEnumerable<(IRenderPipelineGraphicsSettings, SerializedProperty)>) m1, (IRenderPipelineGraphicsSettingsContextMenu, IEnumerable<(IRenderPipelineGraphicsSettings, SerializedProperty)>) m2)
            => Compare(m1.Item1, m2.Item1);
    }
    #endregion

    // This version indicates that it will be applied to any IRenderPipelineGraphicsSettings
    public interface IRenderPipelineGraphicsSettingsContextMenu2
    {
        void PopulateContextMenu(IRenderPipelineGraphicsSettings setting, SerializedProperty property, ref GenericMenu menu);

        int priority => 0;
    }
    
    // This version is specialized for T only
    public interface IRenderPipelineGraphicsSettingsContextMenu2<T> : IRenderPipelineGraphicsSettingsContextMenu2
        where T : class, IRenderPipelineGraphicsSettings
    {
        void PopulateContextMenu(T setting, SerializedProperty property, ref GenericMenu menu);
        void IRenderPipelineGraphicsSettingsContextMenu2.PopulateContextMenu(IRenderPipelineGraphicsSettings setting, SerializedProperty property, ref GenericMenu menu)
            => PopulateContextMenu(setting as T, property, ref menu);
    }

    struct RenderPipelineGraphicsSettingsContextMenuComparer : IComparer<IRenderPipelineGraphicsSettingsContextMenu2>, IComparer<(IRenderPipelineGraphicsSettingsContextMenu2, IEnumerable<(IRenderPipelineGraphicsSettings, SerializedProperty)>)>
    {
        //Sorting is done first by priority, but when priority
        //are the same, we sort by type name to have a stable result.
        public int Compare(IRenderPipelineGraphicsSettingsContextMenu2 m1, IRenderPipelineGraphicsSettingsContextMenu2 m2)
        {
            var compResult = m1.priority.CompareTo(m2.priority);
            if (compResult == 0)
                compResult = m1.GetType().FullName.CompareTo(m2.GetType().FullName);
            return compResult;
        }

        public int Compare((IRenderPipelineGraphicsSettingsContextMenu2, IEnumerable<(IRenderPipelineGraphicsSettings, SerializedProperty)>) m1, (IRenderPipelineGraphicsSettingsContextMenu2, IEnumerable<(IRenderPipelineGraphicsSettings, SerializedProperty)>) m2)
            => Compare(m1.Item1, m2.Item1);
    }

    static class RenderPipelineGraphicsSettingsContextMenuManager
    {
        // typeof(IRenderPipelineGraphicsSettings2) is used for global menu entries.
        // typeof(IRenderPipelineGraphicsSettings) is used for global menu entries.

        #region Deprecated interface list initialization
        // still supporting old IRenderPipelineGraphicsSettings, though it is sorted out as to be inserted after.
        // all obsolete handling (caching and sorting before call) is below under pragma
#pragma warning disable CS0618 // Type or member is obsolete
        static Lazy<Dictionary<Type, List<IRenderPipelineGraphicsSettingsContextMenu>>> s_OldMenuEntries = new(OldInitialize);

        static Dictionary<Type, List<IRenderPipelineGraphicsSettingsContextMenu>> OldInitialize()
        {
#pragma warning disable CS0612 // Type or member is obsolete
            OldRenderPipelineGraphicsSettingsContextMenuComparer comparer = new();
#pragma warning restore CS0612 // Type or member is obsolete
            Dictionary<Type, List<IRenderPipelineGraphicsSettingsContextMenu>> contextMenus = new();

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
                if (menuType.IsAbstract || !typeof(IRenderPipelineGraphicsSettingsContextMenu).IsAssignableFrom(menuType))
                    return;

                Type rpgsType = GetTargetGraphicsSettingsType(menuType);
                if (!contextMenus.ContainsKey(rpgsType))
                    contextMenus[rpgsType] = new();

                var instance = Activator.CreateInstance(menuType, true) as IRenderPipelineGraphicsSettingsContextMenu;
                contextMenus[rpgsType].Add(instance);
            }

            foreach (Type menuType in TypeCache.GetTypesDerivedFrom<IRenderPipelineGraphicsSettingsContextMenu>())
                AddToList(menuType);

            foreach (var list in contextMenus.Values)
                list.Sort(comparer);

            return contextMenus;
        }
#pragma warning restore CS0618 // Type or member is obsolete
        #endregion

        static Lazy<Dictionary<Type, List<IRenderPipelineGraphicsSettingsContextMenu2>>> s_MenuEntries = new(Initialize);

        static Dictionary<Type, List<IRenderPipelineGraphicsSettingsContextMenu2>> Initialize()
        {
            RenderPipelineGraphicsSettingsContextMenuComparer comparer = new();
            Dictionary<Type, List<IRenderPipelineGraphicsSettingsContextMenu2>> contextMenus = new();

            Type GetTargetGraphicsSettingsType(Type menuType)
            {
                var interfaces = menuType.GetInterfaces();
                foreach (var @interface in interfaces)
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IRenderPipelineGraphicsSettingsContextMenu2<>))
                        return @interface.GetGenericArguments()[0];
                return typeof(IRenderPipelineGraphicsSettings);
            }

            void AddToList(Type menuType)
            {
                if (menuType.IsAbstract || !typeof(IRenderPipelineGraphicsSettingsContextMenu2).IsAssignableFrom(menuType))
                    return;

                Type rpgsType = GetTargetGraphicsSettingsType(menuType);
                if (!contextMenus.ContainsKey(rpgsType))
                    contextMenus[rpgsType] = new();

                var instance = Activator.CreateInstance(menuType, true) as IRenderPipelineGraphicsSettingsContextMenu2;
                contextMenus[rpgsType].Add(instance);
            }

            foreach (Type menuType in TypeCache.GetTypesDerivedFrom<IRenderPipelineGraphicsSettingsContextMenu2>())
                AddToList(menuType);

            foreach (var list in contextMenus.Values)
                list.Sort(comparer);

            return contextMenus;
        }

        delegate void Populator(ref GenericMenu menu);

        static IEnumerable<Populator> FilteredAndOrderedPopulatorsFor(IEnumerable<(IRenderPipelineGraphicsSettings target, SerializedProperty property)> graphicsSettings)
        {
            var defaultMenuPopulators = s_MenuEntries.Value.GetValueOrDefault(typeof(IRenderPipelineGraphicsSettings));
            List<(IRenderPipelineGraphicsSettingsContextMenu2 populator, IEnumerable<(IRenderPipelineGraphicsSettings target, SerializedProperty property)> data)> contextMenus = new();
            foreach(var defaultMenuPopulator in defaultMenuPopulators)
                contextMenus.Add((defaultMenuPopulator, graphicsSettings));
            foreach(var settings in graphicsSettings)
                if (s_MenuEntries.Value.TryGetValue(settings.target.GetType(), out var additionalSpecificMenuPopulators))
                    foreach (var populator in additionalSpecificMenuPopulators)
                        contextMenus.Add((populator, new (IRenderPipelineGraphicsSettings target, SerializedProperty property)[] { settings }));

            RenderPipelineGraphicsSettingsContextMenuComparer comparer = new();
            contextMenus.Sort(comparer);
            foreach (var contextMenu in contextMenus)
                foreach (var target in contextMenu.data)
                    yield return (ref GenericMenu menu) => contextMenu.populator.PopulateContextMenu(target.target, target.property, ref menu);

            
            #region Deprecated interface old call logic recreated
            //Following can be removed once we remove the obsolete IRenderPipelineGraphicsSettingsContextMenu
#pragma warning disable CS0618 // Type or member is obsolete
            var oldDefaultMenuPopulators = s_OldMenuEntries.Value.GetValueOrDefault(typeof(IRenderPipelineGraphicsSettings));
            List<(IRenderPipelineGraphicsSettingsContextMenu populator, IEnumerable<(IRenderPipelineGraphicsSettings target, SerializedProperty property)> data)> oldContextMenus = new();
            if (oldDefaultMenuPopulators != null) //can happens, Reset is using the new version
            {
                foreach(var defaultMenuPopulator in oldDefaultMenuPopulators)
                    oldContextMenus.Add((defaultMenuPopulator, graphicsSettings));
            }
            foreach(var settings in graphicsSettings)
                if (s_OldMenuEntries.Value.TryGetValue(settings.target.GetType(), out var additionalSpecificMenuPopulators))
                    foreach (var populator in additionalSpecificMenuPopulators)
                        oldContextMenus.Add((populator, new (IRenderPipelineGraphicsSettings target, SerializedProperty property)[] { settings }));

#pragma warning disable CS0612 // Type or member is obsolete
            OldRenderPipelineGraphicsSettingsContextMenuComparer oldComparer = new();
#pragma warning restore CS0612 // Type or member is obsolete
            oldContextMenus.Sort(oldComparer);
            foreach (var contextMenu in oldContextMenus)
                foreach (var target in contextMenu.data)
                    yield return (ref GenericMenu menu) => contextMenu.populator.PopulateContextMenu(target.target, ScriptAttributeUtility.GetHandler(target.property).propertyDrawer, ref menu);
#pragma warning restore CS0618 // Type or member is obsolete
            #endregion
        }

        //Note: Once IRenderPipelineGraphicsSettingsContextMenu can be fully removed, SerializedProperty property can be removed too
        static internal void PopulateContextMenu(IEnumerable<(IRenderPipelineGraphicsSettings target, SerializedProperty property)> graphicsSettings, ref GenericMenu menu)
        {
            foreach (var populator in FilteredAndOrderedPopulatorsFor(graphicsSettings))
                populator.Invoke(ref menu);
        }
    }

    struct ResetImplementation : IRenderPipelineGraphicsSettingsContextMenu2
    {
        const string k_Label = "Reset";
        // Keeping space in case one want to modify after the Reset
        public int priority => int.MaxValue - 1;

        List<IRenderPipelineGraphicsSettings> targets;

        public void PopulateContextMenu(IRenderPipelineGraphicsSettings setting, SerializedProperty _, ref GenericMenu menu)
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
