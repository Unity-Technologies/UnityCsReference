// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Modules;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEditor.AssetImporters;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor
{
    [CustomEditor(typeof(PluginImporter))]
    [CanEditMultipleObjects]
    internal class PluginImporterInspector : AssetImporterEditor
    {
        private delegate Compatibility ValueSwitcher(Compatibility value);
        private bool m_HasModified;
        private ReorderableList m_DefineConstraints;

        // Notes regarding Standalone target
        // Unlike other platforms, Standalone targets (OSX, Windows, Linux) are bundled as one target ("Standalone") in platform settings
        // That's why value m_CompatibleWithPlatform for <OSX, Windows, Linux> is controlled from two places:
        // * ShowGeneralOptions (when clicking on 'Standalone' toggle, it enables or disables all those targets)
        // * DesktopPluginImporterExtension (where it's possible to individually enable or disable specific Standalone target)

        internal class Styles
        {
            public static readonly GUIContent kDefineConstraints = EditorGUIUtility.TrTextContent("Define Constraints");
            public static readonly GUIContent kLoadSettings = EditorGUIUtility.TrTextContent("Plugin load settings");
            public static readonly GUIContent kPreload = EditorGUIUtility.TrTextContent("Load on startup", "Always load plugin during startup instead of on-demand");
            public static readonly GUIContent kPluginPlatforms = EditorGUIUtility.TrTextContent("Select platforms for plugin");
            public static readonly GUIContent kPlatformSettings = EditorGUIUtility.TrTextContent("Platform settings");
            public static readonly GUIContent kInformation = EditorGUIUtility.TrTextContent("Information");

            static string kCompatibleTextIndividual = L10n.Tr("Define constraint is compatible.");
            static string kIncompatibleTextIndividual = L10n.Tr("Define constraint is incompatible.");
            static string kInvalidTextIndividual = L10n.Tr("Define constraint is invalid.");

            // This is used to make everything in reorderable list elements centered vertically.
            public const int kCenterHeightOffset = 1;

            public const int kValidityIconHeight = 16;
            public const int kValidityIconWidth = 16;
            static readonly Texture2D kValidDefineConstraint = EditorGUIUtility.FindTexture("Valid");
            static readonly Texture2D kValidDefineConstraintHighDpi = EditorGUIUtility.FindTexture("Valid@2x");
            static readonly Texture2D kInvalidDefineConstraint = EditorGUIUtility.FindTexture("Invalid");
            static readonly Texture2D kInvalidDefineConstraintHighDpi = EditorGUIUtility.FindTexture("Invalid@2x");

            public static Texture2D validDefineConstraint => EditorGUIUtility.pixelsPerPoint > 1 ? kValidDefineConstraintHighDpi : kValidDefineConstraint;
            public static Texture2D invalidDefineConstraint => EditorGUIUtility.pixelsPerPoint > 1 ? kInvalidDefineConstraintHighDpi : kInvalidDefineConstraint;

            static string kCompatibleTextTitle = L10n.Tr("Define constraints are compatible.");
            static string kIncompatibleTextTitle = L10n.Tr("One or more define constraints are invalid or incompatible.");

            public static string GetTitleTooltipFromDefineConstraintCompatibility(bool compatible)
            {
                return compatible ? kCompatibleTextTitle : kIncompatibleTextTitle;
            }

            public static string GetIndividualTooltipFromDefineConstraintStatus(DefineConstraintsHelper.DefineConstraintStatus status)
            {
                switch (status)
                {
                    case DefineConstraintsHelper.DefineConstraintStatus.Compatible:
                        return kCompatibleTextIndividual;
                    case DefineConstraintsHelper.DefineConstraintStatus.Incompatible:
                        return kIncompatibleTextIndividual;
                    default:
                        return kInvalidTextIndividual;
                }
            }
        }

        internal enum Compatibility : int
        {
            Mixed = -1,
            NotCompatible = 0,
            Compatible = 1
        }

        private Compatibility m_AutoReferenced;
        private Compatibility m_ValidateReferences;
        private Compatibility m_CompatibleWithAnyPlatform;
        private Compatibility m_CompatibleWithEditor;
        private Compatibility[] m_CompatibleWithPlatform = new Compatibility[GetPlatformGroupArraySize()];
        private List<DefineConstraint> m_DefineConstraintState = new List<DefineConstraint>();

        private Compatibility m_Preload;

        internal class DefineConstraint
        {
            public string name;
            public Compatibility displayValue;
        }

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
            return BuildTargetDiscovery.StandaloneBuildTargets.Contains(buildTarget);
        }

        private Compatibility compatibleWithStandalone
        {
            get
            {
                bool compatible = false;
                foreach (var t in BuildTargetDiscovery.StandaloneBuildTargets)
                {
                    // Return mixed value if one of the values is mixed
                    if (m_CompatibleWithPlatform[(int)t] == Compatibility.Mixed)
                        return Compatibility.Mixed;

                    // Otherwise revert to default behavior
                    compatible = compatible || (m_CompatibleWithPlatform[(int)t] > 0);
                }
                return compatible ? Compatibility.Compatible : Compatibility.NotCompatible;
            }

            set
            {
                foreach (var t in BuildTargetDiscovery.StandaloneBuildTargets)
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

                // Ignore Unknown or deprecated value
                if (IgnorePlatform(platform))
                    continue;

                // Ignore platforms which don't have module extensions loaded, accept standalone targets by default, as they don't have extensions
                if (ModuleManager.IsPlatformSupported(platform) &&
                    !ModuleManager.IsPlatformSupportLoadedByBuildTarget(platform) &&
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
                BuildPlatform editorPlatform = new BuildPlatform("Editor settings", "Editor Settings", "BuildSettings.Editor", NamedBuildTarget.Unknown, BuildTarget.NoTarget, false, true);
                editorPlatform.name = BuildPipeline.GetEditorTargetName();
                filtered.Add(editorPlatform);
            }
            foreach (BuildPlatform bp in validPlatforms)
            {
                if (IgnorePlatform(bp.defaultTarget))
                    continue;

                if (bp.namedBuildTarget.ToBuildTargetGroup() == BuildTargetGroup.Standalone)
                {
                    // Dedicated Server settings are shared with Standalone
                    if (bp.namedBuildTarget == NamedBuildTarget.Server)
                        continue;

                    if (compatibleWithStandalone < Compatibility.Compatible)
                        continue;
                }
                else
                {
                    if (m_CompatibleWithPlatform[(int)bp.defaultTarget] < Compatibility.Compatible)
                        continue;

                    IPluginImporterExtension extension = ModuleManager.GetPluginImporterExtension(bp.namedBuildTarget.ToBuildTargetGroup());
                    if (extension == null)
                        continue;
                }

                filtered.Add(bp);
            }

            return filtered.ToArray();
        }

        private void ResetCompatability(ref Compatibility value, Func<PluginImporter, bool> getComptability)
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

        [Obsolete("UnityUpgradeable () -> DiscardChanges")]
        protected override void ResetValues()
        {
            DiscardChanges();
        }

        public override void DiscardChanges()
        {
            base.DiscardChanges();

            m_HasModified = false;
            m_DefineConstraintState.Clear();

            // making sure we apply any serialized changes to the targets so accessing pluginImporter.DefineConstraints will have the updated values
            serializedObject.ApplyModifiedProperties();

            var minSizeOfDefines = importers.Min(x => x.DefineConstraints.Length);
            string[] baseImporterDefineConstraints = importer.DefineConstraints;

            foreach (var pluginImporter in importers)
            {
                var importerDefineConstraints = pluginImporter.DefineConstraints.Take(minSizeOfDefines).ToList();

                for (var i = 0; i < importerDefineConstraints.Count; i++)
                {
                    var importerDefineConstraint = importerDefineConstraints[i];

                    var symbolName = importerDefineConstraint.StartsWith(DefineConstraintsHelper.Not) ? importerDefineConstraint.Substring(1) : importerDefineConstraint;
                    Compatibility mixedValue = importerDefineConstraints[i] != baseImporterDefineConstraints[i] ? Compatibility.Mixed : Compatibility.Compatible;
                    m_DefineConstraintState.Add(new DefineConstraint { name = importerDefineConstraint, displayValue = mixedValue });

                    if (!DefineConstraintsHelper.IsDefineConstraintValid(symbolName))
                    {
                        m_HasModified = true;
                        Debug.LogError($"Invalid define constraint {symbolName} in plugin {pluginImporter.assetPath}");
                    }
                }
            }

            ResetCompatability(ref m_CompatibleWithAnyPlatform, (imp => imp.GetCompatibleWithAnyPlatform()));
            ResetCompatability(ref m_CompatibleWithEditor, (imp => imp.GetCompatibleWithEditor()));
            ResetCompatability(ref m_AutoReferenced, (imp => !imp.IsExplicitlyReferenced));
            ResetCompatability(ref m_ValidateReferences, (imp => imp.ValidateReferences));
            // If Any Platform is selected, initialize m_Compatible* variables using compatability function
            // If Any Platform is unselected, initialize m_Compatible* variables using exclude function
            // This gives correct initialization in case when plugin is imported for the first time, and only "Any Platform" is selected
            if (m_CompatibleWithAnyPlatform < Compatibility.Compatible)
            {
                ResetCompatability(ref m_CompatibleWithEditor, (imp => imp.GetCompatibleWithEditor("")));

                foreach (BuildTarget platform in GetValidBuildTargets())
                {
                    ResetCompatability(ref m_CompatibleWithPlatform[(int)platform], (imp => imp.GetCompatibleWithPlatform(platform)));
                }
            }
            else
            {
                ResetCompatability(ref m_CompatibleWithEditor, (imp => !imp.GetExcludeEditorFromAnyPlatform()));

                foreach (BuildTarget platform in GetValidBuildTargets())
                {
                    ResetCompatability(ref m_CompatibleWithPlatform[(int)platform], (imp => !imp.GetExcludeFromAnyPlatform(platform)));
                }
            }

            ResetCompatability(ref m_Preload, (imp => imp.isPreloaded));

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
            serializedObject.ApplyModifiedProperties();
            var constraints = m_DefineConstraintState.Where(x => x.displayValue > Compatibility.Mixed).Select(x => x.name).ToArray();
            foreach (var imp in importers)
            {
                imp.DefineConstraints = constraints;

                if (m_CompatibleWithAnyPlatform > Compatibility.Mixed)
                    imp.SetCompatibleWithAnyPlatform(m_CompatibleWithAnyPlatform == Compatibility.Compatible);
                if (m_CompatibleWithEditor > Compatibility.Mixed)
                    imp.SetCompatibleWithEditor(m_CompatibleWithEditor == Compatibility.Compatible);

                if (m_AutoReferenced > Compatibility.Mixed)
                    imp.IsExplicitlyReferenced = m_AutoReferenced != Compatibility.Compatible;

                if (m_ValidateReferences > Compatibility.Mixed)
                    imp.ValidateReferences = m_ValidateReferences == Compatibility.Compatible;

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

                if (m_Preload > Compatibility.Mixed)
                    imp.isPreloaded = (m_Preload == Compatibility.Compatible);
            }

            if (IsEditingPlatformSettingsSupported())
            {
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

            serializedObject.Update();
            base.Apply();

            m_HasModified = false;
        }

        protected override void Awake()
        {
            m_EditorExtension = new EditorPluginImporterExtension();
            m_DesktopExtension = new DesktopPluginImporterExtension();

            base.Awake();
        }

        public override void OnEnable()
        {
            base.OnEnable();

            // this method is doing a lot of setup and it used to be called in awake for some old reasons, which is not the case anymore.
            DiscardChanges();

            m_DefineConstraints = new ReorderableList(m_DefineConstraintState, typeof(DefineConstraint), true, false, true, true);
            m_DefineConstraints.drawElementCallback = DrawDefineConstraintListElement;
            m_DefineConstraints.onRemoveCallback = RemoveDefineConstraintListElement;

            m_DefineConstraints.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_DefineConstraints.headerHeight = 3;

            if (!IsEditingPlatformSettingsSupported())
                return;


            foreach (var extension in additionalExtensions)
            {
                if (!this.importer) continue;
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

            if (this.importer)
            {
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
                            throw new Exception("Unknown managed dll type: " + importer.dllType);
                    }

                    m_PluginInformation["Assembly Info"] = info;
                }

                ResetCompatability(ref m_Preload, (imp => imp.isPreloaded));
            }
        }

        private void RemoveDefineConstraintListElement(ReorderableList list)
        {
            m_HasModified = true;
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
        }

        private void DrawDefineConstraintListElement(Rect rect, int index, bool isactive, bool isfocused)
        {
            var list = m_DefineConstraints.list;
            var defineConstraint = list[index] as DefineConstraint;

            rect.height -= EditorGUIUtility.standardVerticalSpacing;


            var textFieldRect = new Rect(rect.x, rect.y + Styles.kCenterHeightOffset, rect.width - ReorderableList.Defaults.dragHandleWidth, rect.height);

            string noValue = L10n.Tr("(Missing)");

            var label = string.IsNullOrEmpty(defineConstraint.name) ? noValue : defineConstraint.name;
            bool mixed = defineConstraint.displayValue == Compatibility.Mixed;
            EditorGUI.showMixedValue = mixed;
            var textFieldValue = EditorGUI.TextField(textFieldRect, mixed ? L10n.Tr("(Multiple Values)") : label);
            EditorGUI.showMixedValue = false;

            var defines = InternalEditorUtility.GetCompilationDefines(EditorScriptCompilationOptions.BuildingForEditor, EditorUserBuildSettings.activeBuildTarget, EditorUserBuildSettings.GetActiveSubtargetFor(EditorUserBuildSettings.activeBuildTarget));

            if (defines != null)
            {
                var status = DefineConstraintsHelper.GetDefineConstraintCompatibility(defines, defineConstraint.name);
                var image = status == DefineConstraintsHelper.DefineConstraintStatus.Compatible ? Styles.validDefineConstraint : Styles.invalidDefineConstraint;

                var content = new GUIContent(image, Styles.GetIndividualTooltipFromDefineConstraintStatus(status));

                var constraintValidityRect = new Rect(rect.width + ReorderableList.Defaults.dragHandleWidth + Styles.kValidityIconWidth / 4, rect.y + Styles.kCenterHeightOffset, Styles.kValidityIconWidth, Styles.kValidityIconHeight);
                EditorGUI.LabelField(constraintValidityRect, content);
            }

            if (!string.IsNullOrEmpty(textFieldValue) && textFieldValue != noValue && defineConstraint.name != textFieldValue)
            {
                m_HasModified = true;
                defineConstraint.name = textFieldValue;
            }
        }

        public override void OnDisable()
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

            bool newBoolValue = EditorGUILayout.Toggle(EditorGUIUtility.TrTempContent(title), value == Compatibility.Compatible);
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
                    throw new InvalidEnumArgumentException("Invalid value: " + value);
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
                GUILayout.Label(Styles.kPlatformSettings, EditorStyles.boldLabel);
                int platformIndex = EditorGUILayout.BeginPlatformGrouping(validPlatforms, null);

                if (validPlatforms[platformIndex].name == BuildPipeline.GetEditorTargetName())
                {
                    ShowEditorSettings();
                }
                else
                {
                    BuildTargetGroup targetGroup = validPlatforms[platformIndex].namedBuildTarget.ToBuildTargetGroup();
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

        private void ShowLoadSettings()
        {
            EditorGUI.BeginChangeCheck();
            m_Preload = ToggleWithMixedValue(m_Preload, Styles.kPreload.text);
            if (EditorGUI.EndChangeCheck())
                m_HasModified = true;
        }

        private void ShowReferenceOptions()
        {
            GUILayout.Label(EditorGUIUtility.TrTempContent("General"), EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.BeginChangeCheck();
            m_AutoReferenced = ToggleWithMixedValue(m_AutoReferenced, "Auto Reference");
            m_ValidateReferences = ToggleWithMixedValue(m_ValidateReferences, "Validate References");

            if (EditorGUI.EndChangeCheck())
            {
                m_HasModified = true;
            }
            EditorGUILayout.EndVertical();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(false))
            {
                var isManagedPlugin = importers.All(x => x.dllType == DllType.ManagedNET35 || x.dllType == DllType.ManagedNET40);
                if (isManagedPlugin)
                {
                    ShowReferenceOptions();
                    GUILayout.Space(10f);
                }

                GUILayout.Label(Styles.kPluginPlatforms, EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                ShowGeneralOptions();
                EditorGUILayout.EndVertical();
                GUILayout.Space(10f);

                if (IsEditingPlatformSettingsSupported())
                    ShowPlatformSettings();

                if (isManagedPlugin)
                {
                    GUILayout.Label(Styles.kDefineConstraints, EditorStyles.boldLabel);

                    if (m_DefineConstraints.list.Count > 0)
                    {
                        var defines = InternalEditorUtility.GetCompilationDefines(EditorScriptCompilationOptions.BuildingForEditor, EditorUserBuildSettings.activeBuildTarget, EditorUserBuildSettings.GetActiveSubtargetFor(EditorUserBuildSettings.activeBuildTarget));

                        var defineConstraintsCompatible = true;

                        if (defines != null)
                        {
                            for (var i = 0; i < m_DefineConstraints.list.Count && defineConstraintsCompatible; ++i)
                            {
                                var defineConstraint = ((DefineConstraint)m_DefineConstraints.list[i]).name;

                                if (DefineConstraintsHelper.GetDefineConstraintCompatibility(defines, defineConstraint) != DefineConstraintsHelper.DefineConstraintStatus.Compatible)
                                {
                                    defineConstraintsCompatible = false;
                                }
                            }

                            var constraintValidityRect = new Rect(GUILayoutUtility.GetLastRect());
                            constraintValidityRect.x = constraintValidityRect.width - Styles.kValidityIconWidth / 4;
                            var image = defineConstraintsCompatible ? Styles.validDefineConstraint : Styles.invalidDefineConstraint;
                            var tooltip = Styles.GetTitleTooltipFromDefineConstraintCompatibility(defineConstraintsCompatible);
                            var content = new GUIContent(image, tooltip);

                            constraintValidityRect.width = Styles.kValidityIconWidth;
                            constraintValidityRect.height = Styles.kValidityIconHeight;
                            EditorGUI.LabelField(constraintValidityRect, content);
                        }
                    }

                    m_DefineConstraints.DoLayoutList();
                }

                if (importers.All(imp => imp.isNativePlugin))
                {
                    GUILayout.Space(10f);
                    GUILayout.Label(Styles.kLoadSettings, EditorStyles.boldLabel);
                    ShowLoadSettings();
                }
            }

            serializedObject.ApplyModifiedProperties();

            ApplyRevertGUI();

            // Don't output additional information if we have multiple plugins selected
            if (targets.Length > 1)
                return;

            GUILayout.Label(Styles.kInformation, EditorStyles.boldLabel);

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
        }
    }
}
