// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.Compilation;
using System;
using System.IO;
using AssemblyFlags = UnityEditor.Scripting.ScriptCompilation.AssemblyFlags;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(AssemblyDefinitionImporter))]
    [CanEditMultipleObjects]
    internal class AssemblyDefinitionImporterInspector : AssetImporterEditor
    {
        internal class Styles
        {
            public static readonly GUIContent name = EditorGUIUtility.TrTextContent("Name", "The assembly name is used to generate a <name>.dll file on you disk.");
            public static readonly GUIContent unityReferences = EditorGUIUtility.TrTextContent("Unity References");
            public static readonly GUIContent defineConstraints = EditorGUIUtility.TrTextContent("Define Constraints", "Specify a constraint in the assembly definition. The assembly definition only builds if this constraint returns True.");
            public static readonly GUIContent versionDefines = EditorGUIUtility.TrTextContent("Version Defines", "Specify which versions of a packages and modules to include in compilations.");
            public static readonly GUIContent references = EditorGUIUtility.TrTextContent("Assembly Definition References", "The list of assembly files that this assembly definition should reference.");
            public static readonly GUIContent precompiledReferences = EditorGUIUtility.TrTextContent("Assembly References", "The list of Precompiled assemblies that this assembly definition should reference.");
            public static readonly GUIContent generalOptions = EditorGUIUtility.TrTextContent("General");
            public static readonly GUIContent allowUnsafeCode = EditorGUIUtility.TrTextContent("Allow 'unsafe' Code", "When enabled, the C# compiler for this assembly includes types or members that have the `unsafe` keyword.");
            public static readonly GUIContent overrideReferences = EditorGUIUtility.TrTextContent("Override References", "When enabled, you can select which specific precompiled assemblies to refer to via a drop-down list that appears. When not enabled, this assembly definition refers to all auto-referenced precompiled assemblies.");
            public static readonly GUIContent autoReferenced = EditorGUIUtility.TrTextContent("Auto Referenced", "When enabled, this assembly definition is automatically referenced in predefined assemblies.");
            public static readonly GUIContent useGUIDs = EditorGUIUtility.TrTextContent("Use GUIDs", "Use GUIDs instead of assembly names for Assembly Definition References. Allows referenced assemblies to be renamed without having to update references.");
            public static readonly GUIContent platforms = EditorGUIUtility.TrTextContent("Platforms", "Select which platforms include or exclude in the build that this assembly definition file is for.");
            public static readonly GUIContent anyPlatform = EditorGUIUtility.TrTextContent("Any Platform");
            public static readonly GUIContent includePlatforms = EditorGUIUtility.TrTextContent("Include Platforms");
            public static readonly GUIContent excludePlatforms = EditorGUIUtility.TrTextContent("Exclude Platforms");
            public static readonly GUIContent selectAll = EditorGUIUtility.TrTextContent("Select all");
            public static readonly GUIContent deselectAll = EditorGUIUtility.TrTextContent("Deselect all");
            public static readonly GUIContent loadError = EditorGUIUtility.TrTextContent("Load error");
            public static readonly GUIContent expressionOutcome = EditorGUIUtility.TrTextContent("Expression outcome", "Shows the mathematical equation that your Expression represents.");
        }

        GUIStyle m_TextStyle;

        [Serializable]
        internal class DefineConstraint
        {
            public string name;
        }

        [Serializable]
        internal class VersionDefine
        {
            public string name;
            public string expression;
            public string define;
        }

        [Serializable]
        internal class AssemblyDefinitionReference
        {
            public string name;
            public string serializedReference;
            public AssemblyDefinitionAsset asset;

            public void Load(string reference, bool useGUID)
            {
                var referencePath = CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyReference(reference);
                if (!string.IsNullOrEmpty(referencePath))
                {
                    asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(referencePath);

                    if (useGUID)
                    {
                        var fileData = CustomScriptAssemblyData.FromJson(asset.text);
                        name = fileData.name;
                    }
                }
            }
        }

        [Serializable]
        internal class PrecompiledReference
        {
            public string path = "";
            public string fileName = "";
            public string name = "";
        }

        class AssemblyDefinitionState : ScriptableObject
        {
            public string path
            {
                get { return AssetDatabase.GetAssetPath(asset); }
            }

            public string assemblyName;
            public AssemblyDefinitionAsset asset;
            public List<AssemblyDefinitionReference> references;
            public List<PrecompiledReference> precompiledReferences;
            public List<DefineConstraint> defineConstraints;
            public List<VersionDefine> versionDefines;
            public bool[] optionalUnityReferences;
            public bool allowUnsafeCode;
            public bool overrideReferences;
            public bool useGUIDs;
            public bool autoReferenced;
            public bool compatibleWithAnyPlatform;
            public bool[] platformCompatibility;
        }

        SemVersionRangesFactory m_SemVersionRanges;
        ReorderableList m_ReferencesList;
        ReorderableList m_PrecompiledReferencesList;
        ReorderableList m_VersionDefineList;
        ReorderableList m_DefineConstraints;

        SerializedProperty m_AssemblyName;
        SerializedProperty m_AllowUnsafeCode;
        SerializedProperty m_UseGUIDs;
        SerializedProperty m_AutoReferenced;
        SerializedProperty m_OverrideReferences;
        SerializedProperty m_OptionalUnityReferences;
        SerializedProperty m_CompatibleWithAnyPlatform;
        SerializedProperty m_PlatformCompatibility;

        Exception initializeException;

        public override bool showImportedObject { get { return false; } }

        public override void OnEnable()
        {
            base.OnEnable();
            InitializeReorderableLists();
            m_SemVersionRanges = new SemVersionRangesFactory();
            m_AssemblyName = extraDataSerializedObject.FindProperty("assemblyName");
            m_AllowUnsafeCode = extraDataSerializedObject.FindProperty("allowUnsafeCode");
            m_UseGUIDs = extraDataSerializedObject.FindProperty("useGUIDs");
            m_AutoReferenced = extraDataSerializedObject.FindProperty("autoReferenced");
            m_OverrideReferences = extraDataSerializedObject.FindProperty("overrideReferences");
            m_OptionalUnityReferences = extraDataSerializedObject.FindProperty("optionalUnityReferences");
            m_CompatibleWithAnyPlatform = extraDataSerializedObject.FindProperty("compatibleWithAnyPlatform");
            m_PlatformCompatibility = extraDataSerializedObject.FindProperty("platformCompatibility");
        }

        public override void OnInspectorGUI()
        {
            if (initializeException != null)
            {
                ShowLoadErrorExceptionGUI(initializeException);
                ApplyRevertGUI();
                return;
            }

            extraDataSerializedObject.Update();

            var platforms = Compilation.CompilationPipeline.GetAssemblyDefinitionPlatforms();
            var optionalUnityReferences = CustomScriptAssembly.OptinalUnityAssemblies;

            using (new EditorGUI.DisabledScope(false))
            {
                if (targets.Length > 1)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        var value = string.Join(", ", extraDataTargets.Select(t => t.name).ToArray());
                        EditorGUILayout.TextField(Styles.name, value, EditorStyles.textField);
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(m_AssemblyName, Styles.name);
                }

                GUILayout.Label(Styles.generalOptions, EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.PropertyField(m_AllowUnsafeCode, Styles.allowUnsafeCode);
                EditorGUILayout.PropertyField(m_AutoReferenced, Styles.autoReferenced);
                EditorGUILayout.PropertyField(m_OverrideReferences, Styles.overrideReferences);

                EditorGUILayout.EndVertical();
                GUILayout.Space(10f);

                GUILayout.Label(Styles.defineConstraints, EditorStyles.boldLabel);
                m_DefineConstraints.DoLayoutList();

                GUILayout.Label(Styles.references, EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.PropertyField(m_UseGUIDs, Styles.useGUIDs);
                EditorGUILayout.EndVertical();

                if (extraDataTargets.Any(data => ((AssemblyDefinitionState)data).references != null && ((AssemblyDefinitionState)data).references.Any(x => x.asset == null)))
                {
                    EditorGUILayout.HelpBox("The grayed out assembly references are missing and will not be referenced during compilation.", MessageType.Info);
                }

                m_ReferencesList.DoLayoutList();

                if (m_OverrideReferences.boolValue && !m_OverrideReferences.hasMultipleDifferentValues)
                {
                    GUILayout.Label(Styles.precompiledReferences, EditorStyles.boldLabel);

                    if (extraDataTargets.Any(data => ((AssemblyDefinitionState)data).precompiledReferences.Any(x => string.IsNullOrEmpty(x.path) && !string.IsNullOrEmpty(x.name))))
                    {
                        EditorGUILayout.HelpBox("The grayed out assembly references are missing and will not be referenced during compilation.", MessageType.Info);
                    }

                    UpdatePrecompiledReferenceListEntry();
                    m_PrecompiledReferencesList.DoLayoutList();
                }

                GUILayout.Label(Styles.unityReferences, EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                for (int i = 0; i < optionalUnityReferences.Length; ++i)
                {
                    SerializedProperty property;
                    if (i >= m_OptionalUnityReferences.arraySize)
                    {
                        m_OptionalUnityReferences.arraySize++;
                        property = m_OptionalUnityReferences.GetArrayElementAtIndex(i);
                        property.boolValue = false;
                    }
                    else
                    {
                        property = m_OptionalUnityReferences.GetArrayElementAtIndex(i);
                    }
                    EditorGUILayout.PropertyField(property, new GUIContent(optionalUnityReferences[i].DisplayName, optionalUnityReferences[i].Tooltip));

                    if (!property.hasMultipleDifferentValues && property.boolValue)
                    {
                        EditorGUILayout.HelpBox(optionalUnityReferences[i].AdditinalInformationWhenEnabled, MessageType.Info);
                    }
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(10f);

                GUILayout.Label(Styles.platforms, EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(GUI.skin.box);

                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUILayout.PropertyField(m_CompatibleWithAnyPlatform, Styles.anyPlatform);
                    if (change.changed)
                    {
                        // Invert state include/exclude compatibility of states that have the opposite compatibility,
                        // so all states are either include or exclude.
                        var compatibleWithAny = m_CompatibleWithAnyPlatform.boolValue;
                        var needToSwap = extraDataTargets.Cast<AssemblyDefinitionState>().Where(p => p.compatibleWithAnyPlatform != compatibleWithAny).ToList();
                        extraDataSerializedObject.ApplyModifiedProperties();
                        foreach (var state in needToSwap)
                        {
                            InversePlatformCompatibility(state);
                        }
                        extraDataSerializedObject.Update();
                    }
                }

                if (!m_CompatibleWithAnyPlatform.hasMultipleDifferentValues)
                {
                    GUILayout.Label(m_CompatibleWithAnyPlatform.boolValue ? Styles.excludePlatforms : Styles.includePlatforms, EditorStyles.boldLabel);

                    for (int i = 0; i < platforms.Length; ++i)
                    {
                        SerializedProperty property;
                        if (i >= m_PlatformCompatibility.arraySize)
                        {
                            m_PlatformCompatibility.arraySize++;
                            property = m_PlatformCompatibility.GetArrayElementAtIndex(i);
                            property.boolValue = false;
                        }
                        else
                        {
                            property = m_PlatformCompatibility.GetArrayElementAtIndex(i);
                        }
                        EditorGUILayout.PropertyField(property, new GUIContent(platforms[i].DisplayName));
                    }

                    EditorGUILayout.Space();

                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button(Styles.selectAll))
                    {
                        var prop = m_PlatformCompatibility.GetArrayElementAtIndex(0);
                        var end = m_PlatformCompatibility.GetEndProperty();
                        do
                        {
                            prop.boolValue = true;
                        }
                        while (prop.Next(false) && !SerializedProperty.EqualContents(prop, end));
                    }

                    if (GUILayout.Button(Styles.deselectAll))
                    {
                        var prop = m_PlatformCompatibility.GetArrayElementAtIndex(0);
                        var end = m_PlatformCompatibility.GetEndProperty();
                        do
                        {
                            prop.boolValue = false;
                        }
                        while (prop.Next(false) && !SerializedProperty.EqualContents(prop, end));
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(10f);

                EditorGUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(Styles.versionDefines, EditorStyles.boldLabel);
                m_VersionDefineList.DoLayoutList();
                EditorGUILayout.EndVertical();
            }

            extraDataSerializedObject.ApplyModifiedProperties();

            ApplyRevertGUI();
        }

        protected override void Apply()
        {
            base.Apply();
            SaveAndUpdateAssemblyDefinitionStates(extraDataTargets.Cast<AssemblyDefinitionState>().ToArray());
        }

        static void InversePlatformCompatibility(AssemblyDefinitionState state)
        {
            var platforms = Compilation.CompilationPipeline.GetAssemblyDefinitionPlatforms();
            for (int i = 0; i < platforms.Length; ++i)
                state.platformCompatibility[i] = !state.platformCompatibility[i];
        }

        void ShowLoadErrorExceptionGUI(Exception e)
        {
            if (m_TextStyle == null)
                m_TextStyle = "ScriptText";

            GUILayout.Label(Styles.loadError, EditorStyles.boldLabel);
            Rect rect = GUILayoutUtility.GetRect(EditorGUIUtility.TempContent(e.Message), m_TextStyle);
            EditorGUI.HelpBox(rect, e.Message, MessageType.Error);
        }

        protected override Type extraDataType => typeof(AssemblyDefinitionState);

        protected override void InitializeExtraDataInstance(Object extraTarget, int targetIndex)
        {
            try
            {
                LoadAssemblyDefintionState((AssemblyDefinitionState)extraTarget, ((AssetImporter)targets[targetIndex]).assetPath);
                initializeException = null;
            }
            catch (Exception e)
            {
                initializeException = e;
            }
        }

        void InitializeReorderableLists()
        {
            // Disable reordering for multi-editing for asmdefs
            bool enableReordering = targets.Length == 1;

            m_ReferencesList = new ReorderableList(extraDataSerializedObject, extraDataSerializedObject.FindProperty("references"), enableReordering, false, true, true);
            m_ReferencesList.drawElementCallback = DrawReferenceListElement;
            m_ReferencesList.onAddCallback += AddReferenceListElement;

            m_ReferencesList.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_ReferencesList.headerHeight = 3;

            m_DefineConstraints = new ReorderableList(extraDataSerializedObject, extraDataSerializedObject.FindProperty("defineConstraints"), enableReordering, false, true, true);
            m_DefineConstraints.drawElementCallback = DrawDefineConstraintListElement;

            m_DefineConstraints.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_DefineConstraints.headerHeight = 3;

            m_PrecompiledReferencesList = new ReorderableList(extraDataSerializedObject, extraDataSerializedObject.FindProperty("precompiledReferences"), enableReordering, false, true, true);
            m_PrecompiledReferencesList.drawElementCallback = DrawPrecompiledReferenceListElement;
            m_PrecompiledReferencesList.onAddCallback = AddPrecompiledReferenceListElement;
            m_PrecompiledReferencesList.elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            m_PrecompiledReferencesList.headerHeight = 3;

            m_VersionDefineList = new ReorderableList(extraDataSerializedObject, extraDataSerializedObject.FindProperty("versionDefines"), enableReordering, false, true, true);
            m_VersionDefineList.drawElementCallback = DrawVersionDefineListElement;
            m_VersionDefineList.elementHeight = EditorGUIUtility.singleLineHeight * 4 + EditorGUIUtility.standardVerticalSpacing;
            m_VersionDefineList.headerHeight = 3;
        }

        private void DrawDefineConstraintListElement(Rect rect, int index, bool isactive, bool isfocused)
        {
            var list = m_DefineConstraints.serializedProperty;
            var defineConstraint = list.GetArrayElementAtIndex(index).FindPropertyRelative("name");

            rect.height -= EditorGUIUtility.standardVerticalSpacing;

            string noValue = L10n.Tr("(Missing)");

            var label = string.IsNullOrEmpty(defineConstraint.stringValue) ? noValue : defineConstraint.stringValue;

            bool mixed = defineConstraint.hasMultipleDifferentValues;
            EditorGUI.showMixedValue = mixed;
            var textFieldValue = EditorGUI.TextField(rect, mixed ? L10n.Tr("(Multiple Values)") : label);
            EditorGUI.showMixedValue = false;

            if (!string.IsNullOrEmpty(textFieldValue) && textFieldValue != noValue)
            {
                defineConstraint.stringValue = textFieldValue;
            }
        }

        private void DrawVersionDefineListElement(Rect rect, int index, bool isactive, bool isfocused)
        {
            var list = m_VersionDefineList.serializedProperty;
            var versionDefineProp = list.GetArrayElementAtIndex(index);
            var nameProp = versionDefineProp.FindPropertyRelative("name");
            var expressionProp = versionDefineProp.FindPropertyRelative("expression");
            var defineProp = versionDefineProp.FindPropertyRelative("define");

            rect.height -= EditorGUIUtility.standardVerticalSpacing;

            var assetPathsMetaData = EditorCompilationInterface.Instance.GetAssetPathsMetaData().SelectMany(x => x.VersionMetaDatas.Select(y => y.Name)).ToList();

            if (!string.IsNullOrEmpty(nameProp.stringValue) && !assetPathsMetaData.Contains(nameProp.stringValue))
            {
                assetPathsMetaData.Add(nameProp.stringValue);
            }

            assetPathsMetaData.Insert(0, "Select...");
            int indexOfSelected = 0;
            if (!string.IsNullOrEmpty(nameProp.stringValue))
            {
                indexOfSelected = assetPathsMetaData.IndexOf(nameProp.stringValue);
            }

            bool mixed = versionDefineProp.hasMultipleDifferentValues;
            EditorGUI.showMixedValue = mixed;

            var elementRect = new Rect(rect);
            elementRect.height = EditorGUIUtility.singleLineHeight;
            int popupIndex = EditorGUI.Popup(elementRect, GUIContent.Temp("Resource", "Select the package or module that you want to set a define for."), indexOfSelected, assetPathsMetaData.ToArray());
            nameProp.stringValue = assetPathsMetaData[popupIndex];

            elementRect.y += EditorGUIUtility.singleLineHeight;
            defineProp.stringValue = EditorGUI.TextField(elementRect,  GUIContent.Temp("Define", "Specify the name you want this define to have. This define is only set if the expression below returns true."), defineProp.stringValue);

            elementRect.y += EditorGUIUtility.singleLineHeight;
            expressionProp.stringValue = EditorGUI.TextField(elementRect, GUIContent.Temp("Expression", "Specify the semantic version of your chosen module or package. You must use mathematical interval notation."), expressionProp.stringValue);

            string expressionOutcome = null;
            if (!string.IsNullOrEmpty(expressionProp.stringValue))
            {
                try
                {
                    var expression = m_SemVersionRanges.GetExpression(expressionProp.stringValue);
                    expressionOutcome = expression.AppliedRule;
                }
                catch (Exception)
                {
                    expressionOutcome = "Invalid";
                }
            }

            elementRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(elementRect, Styles.expressionOutcome, GUIContent.Temp(expressionOutcome));

            EditorGUI.showMixedValue = false;
        }

        List<string> m_PrecompileReferenceListEntry;

        private void UpdatePrecompiledReferenceListEntry()
        {
            var precompiledAssemblyNames = CompilationPipeline.GetPrecompiledAssemblyNames().ToList();
            var currentReferencesProp = extraDataSerializedObject.FindProperty("precompiledReferences");
            if (currentReferencesProp.arraySize > 0)
            {
                var prop = currentReferencesProp.GetArrayElementAtIndex(0);
                var end = currentReferencesProp.GetEndProperty();
                do
                {
                    var fileName = prop.FindPropertyRelative("fileName").stringValue;
                    if (!string.IsNullOrEmpty(fileName))
                        precompiledAssemblyNames.Remove(fileName);
                }
                while (prop.Next(false) && !SerializedProperty.EqualContents(prop, end));
            }

            m_PrecompileReferenceListEntry = precompiledAssemblyNames
                .OrderBy(x => x).ToList();
        }

        private void DrawPrecompiledReferenceListElement(Rect rect, int index, bool isactive, bool isfocused)
        {
            var precompiledReference = m_PrecompiledReferencesList.serializedProperty.GetArrayElementAtIndex(index);
            var nameProp = precompiledReference.FindPropertyRelative("name");
            var pathProp = precompiledReference.FindPropertyRelative("path");
            var fileNameProp = precompiledReference.FindPropertyRelative("fileName");

            rect.height -= EditorGUIUtility.standardVerticalSpacing;
            GUIContent label = GUIContent.Temp(nameProp.stringValue);

            bool mixed = nameProp.hasMultipleDifferentValues;
            EditorGUI.showMixedValue = mixed;

            bool hasValue = !string.IsNullOrEmpty(pathProp.stringValue);
            if (!hasValue)
            {
                m_PrecompileReferenceListEntry.Insert(0, L10n.Tr("None"));

                if (m_PrecompileReferenceListEntry.Count == 1)
                {
                    label = EditorGUIUtility.TrTempContent("No possible references");
                }
                else
                {
                    label = EditorGUIUtility.TrTempContent("None");
                }
            }
            else
            {
                m_PrecompileReferenceListEntry.Insert(0, fileNameProp.stringValue);
            }

            int currentlySelectedIndex = 0;
            EditorGUI.BeginDisabled(!hasValue && !string.IsNullOrEmpty(nameProp.stringValue));
            int selectedIndex = EditorGUI.Popup(rect, label, currentlySelectedIndex, m_PrecompileReferenceListEntry.ToArray());
            EditorGUI.EndDisabled();

            if (selectedIndex > 0)
            {
                var selectedAssemblyName = m_PrecompileReferenceListEntry[selectedIndex];
                var assembly = EditorCompilationInterface.Instance.GetAllPrecompiledAssemblies()
                    .Single(x => AssetPath.GetFileName(x.Path) == selectedAssemblyName);
                nameProp.stringValue = selectedAssemblyName;
                pathProp.stringValue = assembly.Path;
                fileNameProp.stringValue = AssetPath.GetFileName(assembly.Path);
            }

            m_PrecompileReferenceListEntry.RemoveAt(0);

            EditorGUI.showMixedValue = false;
        }

        static void AddPrecompiledReferenceListElement(ReorderableList list)
        {
            list.serializedProperty.arraySize += 1;
            var newProp = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
            newProp.FindPropertyRelative("name").stringValue = string.Empty;
            newProp.FindPropertyRelative("path").stringValue = string.Empty;
            newProp.FindPropertyRelative("fileName").stringValue = string.Empty;
        }

        static void LoadAssemblyDefintionState(AssemblyDefinitionState state, string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
            if (asset == null)
                return;

            var data = CustomScriptAssemblyData.FromJson(asset.text);
            if (data == null)
                return;

            state.asset = asset;
            state.assemblyName = data.name;
            state.references = new List<AssemblyDefinitionReference>();
            state.precompiledReferences = new List<PrecompiledReference>();
            state.defineConstraints = new List<DefineConstraint>();
            state.versionDefines = new List<VersionDefine>();
            state.autoReferenced = data.autoReferenced;
            state.allowUnsafeCode = data.allowUnsafeCode;
            state.overrideReferences = data.overrideReferences;

            // If the .asmdef has no references (true for newly created .asmdef), then use GUIDs.
            // Otherwise do not use GUIDs. This value might be changed below if any reference is a GUID.
            state.useGUIDs = (data.references != null && data.references.Length > 0);

            if (data.versionDefines != null)
            {
                foreach (var versionDefine in data.versionDefines)
                {
                    if (!SymbolNameRestrictions.IsValid(versionDefine.define))
                    {
                        var exception = new AssemblyDefinitionException($"Invalid version define {versionDefine.define}", path);
                        Debug.LogException(exception, asset);
                    }
                    else
                    {
                        state.versionDefines.Add(new VersionDefine
                        {
                            name = versionDefine.name,
                            expression = versionDefine.expression,
                            define = versionDefine.define,
                        });
                    }
                }
            }

            if (data.defineConstraints != null)
            {
                foreach (var defineConstaint in data.defineConstraints)
                {
                    var symbolName = defineConstaint.StartsWith(DefineConstraintsHelper.Not) ? defineConstaint.Substring(1) : defineConstaint;
                    if (!SymbolNameRestrictions.IsValid(symbolName))
                    {
                        var exception = new AssemblyDefinitionException($"Invalid define constraint {symbolName}", path);
                        Debug.LogException(exception, asset);
                    }
                    else
                    {
                        state.defineConstraints.Add(new DefineConstraint
                        {
                            name = defineConstaint,
                        });
                    }
                }
            }

            if (data.references != null)
            {
                foreach (var reference in data.references)
                {
                    try
                    {
                        var assemblyDefinitionFile = new AssemblyDefinitionReference
                        {
                            name = reference,
                            serializedReference = reference
                        };

                        // If any references is a GUID, use GUIDs.
                        var isGuid = CompilationPipeline.GetAssemblyDefinitionReferenceType(reference) == AssemblyDefinitionReferenceType.Guid;
                        if (isGuid)
                        {
                            state.useGUIDs = true;
                        }

                        assemblyDefinitionFile.Load(reference, isGuid);
                        state.references.Add(assemblyDefinitionFile);
                    }
                    catch (AssemblyDefinitionException e)
                    {
                        UnityEngine.Debug.LogException(e, asset);
                        state.references.Add(new AssemblyDefinitionReference());
                    }
                }
            }

            var nameToPrecompiledReference = EditorCompilationInterface.Instance.GetAllPrecompiledAssemblies()
                .Where(x => (x.Flags & AssemblyFlags.UserAssembly) == AssemblyFlags.UserAssembly)
                .ToDictionary(x => AssetPath.GetFileName(x.Path), x => x);
            foreach (var precompiledReferenceName in data.precompiledReferences ?? Enumerable.Empty<String>())
            {
                try
                {
                    var precompiledReference = new PrecompiledReference
                    {
                        name = precompiledReferenceName,
                    };

                    PrecompiledAssembly assembly;
                    if (nameToPrecompiledReference.TryGetValue(precompiledReferenceName, out assembly))
                    {
                        precompiledReference.path = assembly.Path;
                        precompiledReference.fileName = AssetPath.GetFileName(assembly.Path);
                    }
                    state.precompiledReferences.Add(precompiledReference);
                }
                catch (AssemblyDefinitionException e)
                {
                    Debug.LogException(e, asset);
                    state.precompiledReferences.Add(new PrecompiledReference());
                }
            }

            var platforms = CompilationPipeline.GetAssemblyDefinitionPlatforms();
            state.platformCompatibility = new bool[platforms.Length];

            var OptinalUnityAssemblies = CustomScriptAssembly.OptinalUnityAssemblies;
            state.optionalUnityReferences = new bool[OptinalUnityAssemblies.Length];

            if (data.optionalUnityReferences != null)
            {
                for (int i = 0; i < OptinalUnityAssemblies.Length; i++)
                {
                    var optionalUnityReferences = OptinalUnityAssemblies[i].OptionalUnityReferences.ToString();
                    var any = data.optionalUnityReferences.Any(x => x == optionalUnityReferences);
                    if (any)
                    {
                        state.optionalUnityReferences[i] = true;
                    }
                }
            }

            state.compatibleWithAnyPlatform = true;
            string[] dataPlatforms = null;

            if (data.includePlatforms != null && data.includePlatforms.Length > 0)
            {
                state.compatibleWithAnyPlatform = false;
                dataPlatforms = data.includePlatforms;
            }
            else if (data.excludePlatforms != null && data.excludePlatforms.Length > 0)
            {
                state.compatibleWithAnyPlatform = true;
                dataPlatforms = data.excludePlatforms;
            }

            if (dataPlatforms != null)
                foreach (var platform in dataPlatforms)
                {
                    var platformIndex = GetPlatformIndex(platforms, platform);
                    state.platformCompatibility[platformIndex] = true;
                }
        }

        static void SaveAndUpdateAssemblyDefinitionStates(AssemblyDefinitionState[] states)
        {
            foreach (var state in states)
            {
                SaveAssemblyDefinitionState(state);
            }
        }

        static void SaveAssemblyDefinitionState(AssemblyDefinitionState state)
        {
            var references = state.references;
            var platforms = CompilationPipeline.GetAssemblyDefinitionPlatforms();
            var OptinalUnityAssemblies = CustomScriptAssembly.OptinalUnityAssemblies;

            CustomScriptAssemblyData data = new CustomScriptAssemblyData();

            data.name = state.assemblyName;

            if (state.useGUIDs)
            {
                data.references = references.Select(r =>
                {
                    var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(r.asset));

                    if (string.IsNullOrEmpty(guid))
                        return r.serializedReference;

                    return CompilationPipeline.GUIDToAssemblyDefinitionReferenceGUID(guid);
                }).ToArray();
            }
            else
            {
                data.references = references.Select(r => r.name).ToArray();
            }

            data.defineConstraints = state.defineConstraints
                .Where(x => !string.IsNullOrEmpty(x.name))
                .Select(r => r.name)
                .ToArray();

            data.versionDefines = state.versionDefines.Select(x => new UnityEditor.Scripting.ScriptCompilation.VersionDefine
            {
                name = x.name,
                expression = x.expression,
                define = x.define,
            }).ToArray();

            data.autoReferenced = state.autoReferenced;
            data.overrideReferences = state.overrideReferences;

            data.precompiledReferences = state.precompiledReferences
                .Select(r => r.name).ToArray();

            List<string> optionalUnityReferences = new List<string>();

            for (int i = 0; i < OptinalUnityAssemblies.Length; i++)
            {
                if (state.optionalUnityReferences[i])
                    optionalUnityReferences.Add(OptinalUnityAssemblies[i].OptionalUnityReferences.ToString());
            }

            data.optionalUnityReferences = optionalUnityReferences.ToArray();

            data.allowUnsafeCode = state.allowUnsafeCode;

            List<string> dataPlatforms = new List<string>();

            for (int i = 0; i < platforms.Length; ++i)
            {
                if (state.platformCompatibility[i])
                    dataPlatforms.Add(platforms[i].Name);
            }

            if (dataPlatforms.Any())
            {
                if (state.compatibleWithAnyPlatform)
                    data.excludePlatforms = dataPlatforms.ToArray();
                else
                    data.includePlatforms = dataPlatforms.ToArray();
            }

            var json = CustomScriptAssemblyData.ToJson(data);
            File.WriteAllText(state.path, json);

            AssetDatabase.ImportAsset(state.path);
        }

        static int GetPlatformIndex(AssemblyDefinitionPlatform[] platforms, string name)
        {
            for (int i = 0; i < platforms.Length; ++i)
            {
                if (string.Equals(platforms[i].Name, name, System.StringComparison.InvariantCultureIgnoreCase))
                    return i;
            }

            throw new System.ArgumentException(string.Format("Unknown platform '{0}'", name), name);
        }

        void DrawReferenceListElement(Rect rect, int index, bool selected, bool focused)
        {
            var assemblyDefinitionFile = m_ReferencesList.serializedProperty.GetArrayElementAtIndex(index);
            var nameProp = assemblyDefinitionFile.FindPropertyRelative("name");
            var assetProp = assemblyDefinitionFile.FindPropertyRelative("asset");

            rect.height -= EditorGUIUtility.standardVerticalSpacing;
            var label = string.IsNullOrEmpty(nameProp.stringValue) ? L10n.Tr("(Missing Reference)") : nameProp.stringValue;
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.showMixedValue = assetProp.hasMultipleDifferentValues;
                EditorGUI.BeginDisabled(!string.IsNullOrEmpty(nameProp.stringValue) && assetProp.objectReferenceValue == null);
                var obj = EditorGUI.ObjectField(rect, EditorGUI.showMixedValue ? GUIContent.Temp("(Multiple Values)") : GUIContent.Temp(label), assetProp.objectReferenceValue, typeof(AssemblyDefinitionAsset), false);
                EditorGUI.showMixedValue = false;
                EditorGUI.EndDisabled();

                if (change.changed && obj != null)
                {
                    assetProp.objectReferenceValue = obj;
                    var data = CustomScriptAssemblyData.FromJson(((AssemblyDefinitionAsset)assetProp.objectReferenceValue).text);
                    nameProp.stringValue = data.name;
                }
            }
        }

        void AddReferenceListElement(ReorderableList list)
        {
            list.serializedProperty.arraySize += 1;
            var newProp = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
            newProp.FindPropertyRelative("name").stringValue = string.Empty;
            newProp.FindPropertyRelative("asset").objectReferenceValue = null;
        }
    }
}
