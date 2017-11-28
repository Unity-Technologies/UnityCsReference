// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using System.Text;
using UnityEngine;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace UnityEditor.Modules
{
    internal class DefaultPluginImporterExtension : IPluginImporterExtension
    {
        protected bool hasModified = false;
        protected Property[] properties = null;

        internal class Property
        {
            internal GUIContent name { get; set; }
            internal string key { get; set; }
            internal object defaultValue { get; set; }
            internal Type type { get; set; }
            internal string platformName { get; set; }
            internal object value { get; set; }

            internal Property(string name, string key, object defaultValue, string platformName)
                : this(new GUIContent(name), key, defaultValue, platformName)
            {
            }

            internal Property(GUIContent name, string key, object defaultValue, string platformName)
            {
                this.name = name;
                this.key = key;
                this.defaultValue = defaultValue;
                this.type = defaultValue.GetType();
                this.platformName = platformName;
            }

            internal virtual void Reset(PluginImporterInspector inspector)
            {
                string valueString = inspector.importer.GetPlatformData(platformName, key);
                ParseStringValue(inspector, valueString);
            }

            protected void ParseStringValue(PluginImporterInspector inspector, string valueString, bool muteWarnings = false)
            {
                try
                {
                    value = TypeDescriptor.GetConverter(type).ConvertFromString(valueString);
                }
                catch
                {
                    value = defaultValue;

                    if (!muteWarnings && !string.IsNullOrEmpty(valueString))
                    {
                        // We mute warnings for properties that are on disabled platforms to avoid unnecessary spam for deprecated values, case 909247
                        if (inspector.importer.GetCompatibleWithPlatform(platformName))
                            Debug.LogWarning("Failed to parse value ('" + valueString + "') for " + key + ", platform: " + platformName + ", type: " + type + ". Default value will be set '" + defaultValue + "'");
                    }
                }
            }

            internal virtual void Apply(PluginImporterInspector inspector)
            {
                inspector.importer.SetPlatformData(platformName, key, value.ToString());
            }

            internal virtual void OnGUI(PluginImporterInspector inspector)
            {
                if (type == typeof(bool)) value = EditorGUILayout.Toggle(name, (bool)value);
                else if (type.IsEnum) value = EditorGUILayout.EnumPopup(name, (Enum)value);
                else if (type == typeof(string)) value = EditorGUILayout.TextField(name, (string)value);
                else throw new NotImplementedException("Don't know how to display value.");
            }
        }

        internal bool propertiesRefreshed = false;

        public DefaultPluginImporterExtension(Property[] properties)
        {
            this.properties = properties;
        }

        public virtual void ResetValues(PluginImporterInspector inspector)
        {
            hasModified = false;
            RefreshProperties(inspector);
        }

        public virtual bool HasModified(PluginImporterInspector inspector)
        {
            return hasModified;
        }

        public virtual void Apply(PluginImporterInspector inspector)
        {
            if (!propertiesRefreshed) return;

            foreach (var p in properties)
            {
                p.Apply(inspector);
            }
        }

        public virtual void OnEnable(PluginImporterInspector inspector)
        {
            RefreshProperties(inspector);
        }

        public virtual void OnDisable(PluginImporterInspector inspector)
        {
        }

        public virtual void OnPlatformSettingsGUI(PluginImporterInspector inspector)
        {
            if (!propertiesRefreshed) RefreshProperties(inspector);

            EditorGUI.BeginChangeCheck();
            foreach (var p in properties)
            {
                p.OnGUI(inspector);
            }
            if (EditorGUI.EndChangeCheck()) hasModified = true;
        }

        protected virtual void RefreshProperties(PluginImporterInspector inspector)
        {
            foreach (var p in properties)
            {
                p.Reset(inspector);
            }
            propertiesRefreshed = true;
        }

        public virtual string CalculateFinalPluginPath(string platformName, PluginImporter imp)
        {
            string cpu = imp.GetPlatformData(platformName, "CPU");

            if (!string.IsNullOrEmpty(cpu) && (string.Compare(cpu, "AnyCPU", true) != 0) && (string.Compare(cpu, "None", true) != 0))
                return Path.Combine(cpu, Path.GetFileName(imp.assetPath));

            return Path.GetFileName(imp.assetPath);
        }

        protected Dictionary<string, List<PluginImporter>> GetCompatiblePlugins(string buildTargetName)
        {
            IEnumerable<PluginImporter> plugins = PluginImporter.GetAllImporters().Where(imp => imp.GetCompatibleWithPlatformOrAnyPlatformBuildTarget(buildTargetName));
            Dictionary<string, List<PluginImporter>> matchingPlugins = new Dictionary<string, List<PluginImporter>>();

            foreach (var plugin in plugins)
            {
                if (string.IsNullOrEmpty(plugin.assetPath))
                    continue;
                string finalPluginPath = CalculateFinalPluginPath(buildTargetName, plugin);
                if (string.IsNullOrEmpty(finalPluginPath))
                    continue;

                List<PluginImporter> temp = null;
                if (matchingPlugins.TryGetValue(finalPluginPath, out temp) == false)
                {
                    temp = new List<PluginImporter>();
                    matchingPlugins[finalPluginPath] = temp;
                }
                temp.Add(plugin);
            }
            return matchingPlugins;
        }

        public virtual bool CheckFileCollisions(string buildTargetName)
        {
            Dictionary<string, List<PluginImporter>> matchingPlugins = GetCompatiblePlugins(buildTargetName);

            bool foundCollisions = false;

            StringBuilder errorMessage = new StringBuilder();
            foreach (KeyValuePair<string, List<PluginImporter>> pair in matchingPlugins)
            {
                List<PluginImporter> plugins = pair.Value;
                // If we have only one plugin with specified final path, that means everything is ok, and no overwrite will occur
                if (plugins.Count == 1) continue;

                // Project plugins are those found inside of the User's project folder.
                int projectPluginCount = 0;
                foreach (PluginImporter importer in plugins)
                {
                    if (!importer.GetIsOverridable())
                    {
                        projectPluginCount++;
                    }
                }

                // If we have a single user project plugin, it will take precedence and overwrite the others.
                // Anything else should throw errors if there are multiple.
                if (projectPluginCount == 1)
                    continue;

                foundCollisions = true;
                // Two or more plugins are being copied to the same path, create an error message
                errorMessage.AppendLine(string.Format("Plugin '{0}' is used from several locations:", Path.GetFileName(pair.Key)));
                foreach (PluginImporter importer in plugins)
                {
                    errorMessage.AppendLine(" " + importer.assetPath + " would be copied to <PluginPath>/" + pair.Key.Replace("\\", "/"));
                }
            }
            if (foundCollisions)
            {
                errorMessage.AppendLine("Please fix plugin settings and try again.");
                Debug.LogError(errorMessage.ToString());
            }

            return foundCollisions;
        }
    }
}
