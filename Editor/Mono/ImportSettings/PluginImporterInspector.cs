// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditorInternal;
using UnityEditor.Modules;
using UnityEditor.Build;

using Mono.Cecil;
namespace UnityEditor
{
    [CustomEditor(typeof(PluginImporter))]
    [CanEditMultipleObjects]
    internal class PluginImporterInspector : Experimental.AssetImporters.AssetImporterEditor
    {
        private delegate Compatibility ValueSwitcher(Compatibility value);
        private bool m_HasModified;
        private string m_ReferencesUnityEngineModule;

        // Notes regarding Standalone target
        // Unlike other platforms, Standalone targets (OSX, Windows, Linux) are bundled as one target ("Standalone") in platform settings
        // That's why value m_CompatibleWithPlatform for <OSX, Windows, Linux> is controlled from two places:
        // * ShowGeneralOptions (when clicking on 'Standalone' toggle, it enables or disables all those targets)
        // * DesktopPluginImporterExtension (where it's possible to individually enable or disable specific Standalone target)

        internal enum Compatibility : int
        {
            Mixed = -1,
            NotCompatible = 0,
            Compatible = 1
        }

        private Compatibility m_CompatibleWithAnyPlatform;
        private Compatibility m_CompatibleWithEditor;
        private Compatibility[] m_CompatibleWithPlatform = new Compatibility[GetPlatformGroupArraySize()];

        private readonly static BuildTarget[] m_StandaloneTargets = new BuildTarget[]
        {
            BuildTarget.StandaloneOSX,
            BuildTarget.StandaloneWindows,
            BuildTarget.StandaloneWindows64,
            BuildTarget.StandaloneLinux,
            BuildTarget.StandaloneLinux64,
            BuildTarget.StandaloneLinuxUniversal
        };

        private Vector2 m_InformationScrollPosition = Vector2.zero;
        private Dictionary<string, string> m_PluginInformation;
        private EditorPluginImporterExtension m_EditorExtension = null;
        private DesktopPluginImporterExtension m_DesktopExtension = null;

        public override bool showImportedObject { get { return false; } }

        internal EditorPluginImporterExtension editorExtension
        {
            get
            {
                if (m_EditorExtension == null)
                    m_EditorExtension = new EditorPluginImporterExtension();
                return m_EditorExtension;
            }
        }

        internal DesktopPluginImporterExtension desktopExtension
        {
            get
            {
                if (m_DesktopExtension == null)
                    m_DesktopExtension = new DesktopPluginImporterExtension();
                return m_DesktopExtension;
            }
        }

        internal IPluginImporterExtension[] additionalExtensions
        {
            get
            {
                return new IPluginImporterExtension[]
                {
                    editorExtension,
                    desktopExtension
                };
            }
        }

        internal PluginImporter importer
        {
            get { return target as PluginImporter; }
        }

        internal PluginImporter[] importers
        {
            get { return targets.Cast<PluginImporter>().ToArray(); }
        }

        private static bool IgnorePlatform(BuildTarget platform)
        {
            return false;
        }

        private bool IsEditingPlatformSettingsSupported()
        {
            // We don't support editing platform settings when multiple objects are selected
            return targets.Length == 1;
        }

        private static int GetPlatformGroupArraySize()
        {
            int max = 0;
            foreach (BuildTarget platform in typeof(BuildTarget).EnumGetNonObsoleteValues())
                if (max < (int)platform + 1) max = (int)platform + 1;
            return max;
        }

        private static bool IsStandaloneTarget(BuildTarget buildTarget)
        {
            return m_StandaloneTargets.Contains(buildTarget);
        }

        private Compatibility compatibleWithStandalone
        {
            get
            {
                bool compatible = false;
                foreach (var t in m_StandaloneTargets)
                {
                    // Return mixed value if one of the values is mixed
                    if (m_CompatibleWithPlatform[(int)t] == Compatibility.Mixed)
                        return Compatibility.Mixed;

                    // Otherwise revert to default behavior
                    compatible |= m_CompatibleWithPlatform[(int)t] > 0;
                }
                return compatible ? Compatibility.Compatible : Compatibility.NotCompatible;
            }

            set
            {
                foreach (var t in m_StandaloneTargets)
                    m_CompatibleWithPlatform[(int)t] = value;
            }
        }

        internal static bool IsValidBuildTarget(BuildTarget buildTarget)
        {
            return buildTarget > 0;
        }

        // Used by extensions, for ex., Standalone where we have options for enabling/disabling platform in platform specific extensions
        internal Compatibility GetPlatformCompatibility(string platformName)
        {
            var buildTarget = BuildPipeline.GetBuildTargetByName(platformName);
            if (!IsValidBuildTarget(buildTarget))
                return Compatibility.NotCompatible;

            return m_CompatibleWithPlatform[(int)buildTarget];
        }

        internal void SetPlatformCompatibility(string platformName, bool compatible)
        {
            SetPlatformCompatibility(platformName, compatible ? Compatibility.Compatible : Compatibility.NotCompatible);
        }

        internal void SetPlatformCompatibility(string platformName, Compatibility compatibility)
        {
            if (compatibility == Compatibility.Mixed)
                throw new ArgumentException("compatibility value cannot be Mixed");

            var buildTarget = BuildPipeline.GetBuildTargetByName(platformName);
            if (!IsValidBuildTarget(buildTarget) || m_CompatibleWithPlatform[(int)buildTarget] == compatibility)
                return;

            m_CompatibleWithPlatform[(int)buildTarget] = compatibility;
            m_HasModified = true;
        }

        private static List<BuildTarget> GetValidBuildTargets()
        {
            List<BuildTarget> validBuildTargets = new List<BuildTarget>();
            foreach (BuildTarget platform in typeof(BuildTarget).EnumGetNonObsoleteValues())
            {
                // We have some special enums with negative values which are not actual targets, ignore those
                if (!IsValidBuildTarget(platform))
                    continue;

                // Ignore Unknown or deprectated value
                if (IgnorePlatform(platform))
                    continue;

                // Ignore platforms which don't have module extensions loaded, accept standalone targets by default, as they don't have extensions
                if (ModuleManager.IsPlatformSupported(platform) &&
                    !ModuleManager.IsPlatformSupportLoaded(ModuleManager.GetTargetStringFromBuildTarget(platform)) &&
                    !IsStandaloneTarget(platform))
                    continue;

                validBuildTargets.Add(platform);
            }
            return validBuildTargets;
        }

        private BuildPlatform[] GetBuildPlayerValidPlatforms()
        {
            List<BuildPlatform> validPlatforms = BuildPlatforms.instance.GetValidPlatforms();
            List<BuildPlatform> filtered = new List<BuildPlatform>();

            if (m_CompatibleWithEditor > Compatibility.NotCompatible)
            {
                BuildPlatform editorPlatform = new BuildPlatform("Editor settings", "Editor Settings", "BuildSettings.Editor", BuildTargetGroup.Unknown, true);
                editorPlatform.name = BuildPipeline.GetEditorTargetName();
                filtered.Add(editorPlatform);
            }
            foreach (BuildPlatform bp in validPlatforms)
            {
                if (IgnorePlatform(bp.defaultTarget))
                    continue;

                if (bp.targetGroup == BuildTargetGroup.Standalone)
                {
                    if (compatibleWithStandalone < Compatibility.Compatible)
                        continue;
                }
                else
                {
                    if (m_CompatibleWithPlatform[(int)bp.defaultTarget] < Compatibility.Compatible)
                        continue;

                    IPluginImporterExtension extension = ModuleManager.GetPluginImporterExtension(bp.targetGroup);
                    if (extension == null)
                        continue;
                }

                filtered.Add(bp);
            }

            return filtered.ToArray();
        }

        private delegate bool GetComptability(PluginImporter imp);
        private void ResetCompatability(ref Compatibility value, GetComptability getComptability)
        {
            value = getComptability(importer) ? Compatibility.Compatible : Compatibility.NotCompatible;
            foreach (var imp in importers)
            {
                if (value != (getComptability(imp) ? Compatibility.Compatible : Compatibility.NotCompatible))
                {
                    value = Compatibility.Mixed;
                    break;
                }
            }
        }

        protected override void ResetValues()
        {
            base.ResetValues();

            m_HasModified = false;

            ResetCompatability(ref m_CompatibleWithAnyPlatform, (imp => { return imp.GetCompatibleWithAnyPlatform(); }));
            ResetCompatability(ref m_CompatibleWithEditor, (imp => { return imp.GetCompatibleWithEditor(); }));
            // If Any Platform is selected, initialize m_Compatible* variables using compatability function
            // If Any Platform is unselected, initialize m_Compatible* variables using exclude function
            // This gives correct initialization in case when plugin is imported for the first time, and only "Any Platform" is selected
            if (m_CompatibleWithAnyPlatform < Compatibility.Compatible)
            {
                ResetCompatability(ref m_CompatibleWithEditor, (imp => { return imp.GetCompatibleWithEditor("", ""); }));

                foreach (BuildTarget platform in GetValidBuildTargets())
                {
                    ResetCompatability(ref m_CompatibleWithPlatform[(int)platform], (imp => { return imp.GetCompatibleWithPlatform(platform); }));
                }
            }
            else
            {
                ResetCompatability(ref m_CompatibleWithEditor, (imp => { return !imp.GetExcludeEditorFromAnyPlatform(); }));

                foreach (BuildTarget platform in GetValidBuildTargets())
                {
                    ResetCompatability(ref m_CompatibleWithPlatform[(int)platform], (imp => { return !imp.GetExcludeFromAnyPlatform(platform); }));
                }
            }

            if (!IsEditingPlatformSettingsSupported())
                return;

            foreach (var extension in additionalExtensions)
            {
                extension.ResetValues(this);
            }

            foreach (BuildTarget platform in GetValidBuildTargets())
            {
                IPluginImporterExtension extension = ModuleManager.GetPluginImporterExtension(platform);
                if (extension != null)
                    extension.ResetValues(this);
            }
        }

        public override bool HasModified()
        {
            bool modified = m_HasModified || base.HasModified();

            if (!IsEditingPlatformSettingsSupported())
                return modified;

            foreach (var extension in additionalExtensions)
            {
                modified |= extension.HasModified(this);
            }

            foreach (BuildTarget platform in GetValidBuildTargets())
            {
                IPluginImporterExtension extension = ModuleManager.GetPluginImporterExtension(platform);
                if (extension != null) modified |= extension.HasModified(this);
            }

            return modified;
        }

        protected override void Apply()
        {
            base.Apply();
            foreach (var imp in importers)
            {
                if (m_CompatibleWithAnyPlatform > Compatibility.Mixed)
                    imp.SetCompatibleWithAnyPlatform(m_CompatibleWithAnyPlatform == Compatibility.Compatible);
                if (m_CompatibleWithEditor > Compatibility.Mixed)
                    imp.SetCompatibleWithEditor(m_CompatibleWithEditor == Compatibility.Compatible);

                foreach (BuildTarget platform in GetValidBuildTargets())
                {
                    if (m_CompatibleWithPlatform[(int)platform] > Compatibility.Mixed)
                        imp.SetCompatibleWithPlatform(platform, m_CompatibleWithPlatform[(int)platform] == Compatibility.Compatible);
                }

                if (m_CompatibleWithEditor > Compatibility.Mixed)
                    imp.SetExcludeEditorFromAnyPlatform(m_CompatibleWithEditor == Compatibility.NotCompatible);

                foreach (BuildTarget platform in GetValidBuildTargets())
                {
                    if (m_CompatibleWithPlatform[(int)platform] > Compatibility.Mixed)
                        imp.SetExcludeFromAnyPlatform(platform, m_CompatibleWithPlatform[(int)platform] == Compatibility.NotCompatible);
                }
            }

            if (!IsEditingPlatformSettingsSupported())
                return;

            foreach (var extension in additionalExtensions)
            {
                extension.Apply(this);
            }

            foreach (BuildTarget platform in GetValidBuildTargets())
            {
                IPluginImporterExtension extension = ModuleManager.GetPluginImporterExtension(platform);
                if (extension != null) extension.Apply(this);
            }
        }

        protected override void Awake()
        {
            m_EditorExtension = new EditorPluginImporterExtension();
            m_DesktopExtension = new DesktopPluginImporterExtension();

            base.Awake();
        }

        public override void OnEnable()
        {
            if (!IsEditingPlatformSettingsSupported())
                return;

            foreach (var extension in additionalExtensions)
            {
                extension.OnEnable(this);
            }

            foreach (BuildTarget platform in GetValidBuildTargets())
            {
                IPluginImporterExtension extension = ModuleManager.GetPluginImporterExtension(platform);
                if (extension != null)
                {
                    extension.OnEnable(this);
                    extension.ResetValues(this);
                }
            }

            m_PluginInformation = new Dictionary<string, string>();
            m_PluginInformation["Path"] = importer.assetPath;
            m_PluginInformation["Type"] = importer.isNativePlugin ? "Native" : "Managed";
            if (!importer.isNativePlugin)
            {
                string info;
                switch (importer.dllType)
                {
                    case DllType.ManagedNET35: info = "Targets .NET 3.5"; break;
                    case DllType.ManagedNET40: info = "Targets .NET 4.x"; break;
                    case DllType.UnknownManaged: info = "Targets Unknown .NET"; break;
                    case DllType.WinMDNET40: info = "Managed WinMD"; break;
                    case DllType.WinMDNative: info = "Native WinMD"; break;
                    default:
                        throw new Exception("Unknown managed dll type: " + importer.dllType.ToString());
                }

                m_PluginInformation["Assembly Info"] = info;
            }

            m_ReferencesUnityEngineModule = importer.HasDiscouragedReferences();
        }

        new void OnDisable()
        {
            base.OnDisable();

            if (!IsEditingPlatformSettingsSupported())
                return;

            foreach (var extension in additionalExtensions)
            {
                extension.OnDisable(this);
            }

            foreach (BuildTarget platform in GetValidBuildTargets())
            {
                IPluginImporterExtension extension = ModuleManager.GetPluginImporterExtension(platform);
                if (extension != null) extension.OnDisable(this);
            }
        }

        private Compatibility ToggleWithMixedValue(Compatibility value, string title)
        {
            EditorGUI.showMixedValue = value == Compatibility.Mixed;

            EditorGUI.BeginChangeCheck();

            bool newBoolValue = EditorGUILayout.Toggle(title, value == Compatibility.Compatible);
            if (EditorGUI.EndChangeCheck())
                return newBoolValue ? Compatibility.Compatible : Compatibility.NotCompatible;

            EditorGUI.showMixedValue = false;
            return value;
        }

        private void ShowPlatforms(ValueSwitcher switcher)
        {
            // Note: We use m_CompatibleWithEditor & m_CompatibleWithPlatform for displaying both Include & Exclude platforms
            m_CompatibleWithEditor = switcher(ToggleWithMixedValue(switcher(m_CompatibleWithEditor), "Editor"));
            EditorGUI.BeginChangeCheck();
            Compatibility value = ToggleWithMixedValue(switcher(compatibleWithStandalone), "Standalone");
            // We only want to change compatibleWithStandalone value, if user actually clicks on it
            if (EditorGUI.EndChangeCheck())
            {
                compatibleWithStandalone = switcher(value);
                if (compatibleWithStandalone != Compatibility.Mixed)
                    desktopExtension.ValidateSingleCPUTargets(this);
            }

            foreach (BuildTarget platform in GetValidBuildTargets())
            {
                // Ignore Standalone targets, we're displaying it as one item
                if (IsStandaloneTarget(platform))
                    continue;

                m_CompatibleWithPlatform[(int)platform] = switcher(ToggleWithMixedValue(switcher(m_CompatibleWithPlatform[(int)platform]), platform.ToString()));
            }
        }

        private Compatibility SwitchToInclude(Compatibility value)
        {
            return value;
        }

        private Compatibility SwitchToExclude(Compatibility value)
        {
            switch (value)
            {
                case Compatibility.Mixed: return Compatibility.Mixed;
                case Compatibility.Compatible: return Compatibility.NotCompatible;
                case Compatibility.NotCompatible: return Compatibility.Compatible;
                default:
                    throw new InvalidEnumArgumentException("Invalid value: " + value.ToString());
            }
        }

        private void ShowGeneralOptions()
        {
            EditorGUI.BeginChangeCheck();
            m_CompatibleWithAnyPlatform = ToggleWithMixedValue(m_CompatibleWithAnyPlatform, "Any Platform");

            if (m_CompatibleWithAnyPlatform == Compatibility.Compatible)
            {
                GUILayout.Label("Exclude Platforms", EditorStyles.boldLabel);
                ShowPlatforms(SwitchToExclude);
            }
            else if (m_CompatibleWithAnyPlatform == Compatibility.NotCompatible)
            {
                GUILayout.Label("Include Platforms", EditorStyles.boldLabel);
                ShowPlatforms(SwitchToInclude);
            }

            if (EditorGUI.EndChangeCheck())
                m_HasModified = true;
        }

        private void ShowEditorSettings()
        {
            editorExtension.OnPlatformSettingsGUI(this);
        }

        private void ShowPlatformSettings()
        {
            BuildPlatform[] validPlatforms = GetBuildPlayerValidPlatforms();
            if (validPlatforms.Length > 0)
            {
                GUILayout.Label("Platform settings", EditorStyles.boldLabel);
                int platformIndex = EditorGUILayout.BeginPlatformGrouping(validPlatforms, null);

                if (validPlatforms[platformIndex].name == BuildPipeline.GetEditorTargetName())
                {
                    ShowEditorSettings();
                }
                else
                {
                    BuildTargetGroup targetGroup = validPlatforms[platformIndex].targetGroup;
                    if (targetGroup == BuildTargetGroup.Standalone)
                    {
                        desktopExtension.OnPlatformSettingsGUI(this);
                    }
                    else
                    {
                        IPluginImporterExtension extension = ModuleManager.GetPluginImporterExtension(targetGroup);
                        if (extension != null) extension.OnPlatformSettingsGUI(this);
                    }
                }
                EditorGUILayout.EndPlatformGrouping();
            }
        }

        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(false))
            {
                GUILayout.Label("Select platforms for plugin", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                ShowGeneralOptions();
                EditorGUILayout.EndVertical();
                GUILayout.Space(10f);

                if (IsEditingPlatformSettingsSupported())
                    ShowPlatformSettings();
            }
            ApplyRevertGUI();

            // Don't output additional information if we have multiple plugins selected
            if (targets.Length > 1)
                return;

            GUILayout.Label("Information", EditorStyles.boldLabel);

            m_InformationScrollPosition = EditorGUILayout.BeginVerticalScrollView(m_InformationScrollPosition);

            foreach (var prop in m_PluginInformation)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(prop.Key, GUILayout.Width(85));
                EditorGUILayout.SelectableLabel(prop.Value, GUILayout.Height(EditorGUI.kSingleLineHeight));
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            GUILayout.FlexibleSpace();

            // Warning for Case 648027
            // Once Mono loads a native plugin, it never releases a handle, thus plugin is never unloaded.
            if (importer.isNativePlugin)
                EditorGUILayout.HelpBox("Once a native plugin is loaded from script, it's never unloaded. If you deselect a native plugin and it's already loaded, please restart Unity.", MessageType.Warning);
            if (EditorApplication.scriptingRuntimeVersion == ScriptingRuntimeVersion.Legacy && importer.dllType == DllType.ManagedNET40 && m_CompatibleWithEditor == Compatibility.Compatible)
                EditorGUILayout.HelpBox("Plugin targets .NET 4.x and is marked as compatible with Editor, Editor can only use assemblies targeting .NET 3.5 or lower, please unselect Editor as compatible platform.", MessageType.Error);
            if (m_ReferencesUnityEngineModule != null)
                EditorGUILayout.HelpBox("This plugin references at least one UnityEngine module assemblies directly (" + m_ReferencesUnityEngineModule + ".dll). To assure forward compatibility, only reference UnityEngine.dll, which contains type forwarders for all the module dlls.", MessageType.Warning);
        }
    }
}
