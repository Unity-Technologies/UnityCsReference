// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using Unity.Multiplayer.Internal;

namespace Unity.Multiplayer.Editor
{
    internal class ProjectSettingsProvider : SettingsProvider
    {
        internal const string k_SettingsGroupPath = "Project/Multiplayer/";
        private static readonly string[] k_Keywords = new string[]
        {
            "Multiplayer",
            "Server",
        };

        [SettingsProviderGroup]
        public static SettingsProvider[] CreateDedicatedServerSettingsProvider()
        {
            if(!DedicatedServerMigrationUtility.ShouldEnableDedicatedServer())
            {
                return Array.Empty<SettingsProvider>();
            }
            var paths = new HashSet<string>();
            foreach(var t in TypeCache.GetTypesWithAttribute<ProjectSettingsSectionAttribute>())
            {
                ProjectSettingsSectionAttribute attr = (ProjectSettingsSectionAttribute)(t.GetCustomAttributes(typeof(ProjectSettingsSectionAttribute), true)[0]);
                paths.Add(attr.SettingsPath);
            }

            var settingsProviders = new List<SettingsProvider>();

            foreach (var path in paths)
            {
                settingsProviders.Add(new ProjectSettingsProvider(path));
            }

            return settingsProviders.ToArray();
        }

        public ProjectSettingsProvider(string path) : base(path, SettingsScope.Project)
        {
            keywords = new HashSet<string>(k_Keywords);
            activateHandler = ActivateHandler;
        }

        private void ActivateHandler(string searchContext, VisualElement root)
        {
            var container = new ScrollView();
            container.AddToClassList("dedicated-server-settings-container");
            container.styleSheets.Add(EditorGUIUtility.LoadRequired("Multiplayer/UI/DedicatedServerSettings.uss") as StyleSheet);

            var header = new Label(Path.GetFileName(this.settingsPath));
            header.AddToClassList("dedicated-server-settings-header");

            container.Add(header);

            var types = GetAllSectionTypes();
            foreach (var type in types)
            {
                Assert.IsTrue(type.IsSubclassOf(typeof(VisualElement)));

                var sectionAttribute = (ProjectSettingsSectionAttribute)(type.GetCustomAttributes(typeof(ProjectSettingsSectionAttribute), true)[0]);

                var constructor = type.GetConstructor(Array.Empty<Type>());
                Assert.IsNotNull(constructor, $"The type {type} must have a parameterless constructor");

                if (sectionAttribute.Label != null)
                {
                    var title = new Label(sectionAttribute.Label);
                    title.AddToClassList("dedicated-server-settings-header2");
                    container.Add(title);
                }

                var section = (VisualElement)constructor.Invoke(null);
                container.Add(section);
            }

            root.Add(container);
        }

        private IEnumerable<Type> GetAllSectionTypes()
        {
            var types = new HashSet<Type>();
            foreach(var t in TypeCache.GetTypesWithAttribute<ProjectSettingsSectionAttribute>())
            {
                ProjectSettingsSectionAttribute attr = (ProjectSettingsSectionAttribute)(t.GetCustomAttributes(typeof(ProjectSettingsSectionAttribute), true)[0]);
                if(attr.SettingsPath == settingsPath)
                {
                    types.Add(t);
                }
            }
            return types;
        }
    }
}
