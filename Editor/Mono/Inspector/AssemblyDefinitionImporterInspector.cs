// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Scripting;
using UnityEditorInternal;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.Compilation;
using System;

namespace UnityEditor
{
    [CustomEditor(typeof(UnityEditorInternal.AssemblyDefinitionImporter))]
    [CanEditMultipleObjects]
    internal class AssemblyDefinitionImporterInspector : AssetImporterEditor
    {
        internal enum MixedBool : int
        {
            Mixed = -1,
            True = 0,
            False = 1
        }

        internal class AssemblyDefinitionReference
        {
            public string path
            {
                get { return AssetDatabase.GetAssetPath(asset); }
            }
            public AssemblyDefinitionAsset asset;
            public CustomScriptAssemblyData data;
            public MixedBool displayValue;
        }

        internal class AssemblyDefintionState
        {
            public string path
            {
                get { return AssetDatabase.GetAssetPath(asset); }
            }
            public AssemblyDefinitionAsset asset;
            public string name;
            public List<AssemblyDefinitionReference> references;
            public MixedBool compatibleWithAnyPlatform;
            public MixedBool[] platformCompatibility;
            public bool modified;
        }

        AssemblyDefintionState[] m_TargetStates;

        AssemblyDefintionState m_State;
        ReorderableList m_ReferencesList;
        GUIStyle m_TextStyle;

        public override bool showImportedObject { get { return false; } }

        public override void OnInspectorGUI()
        {
            if (m_State == null)
            {
                try
                {
                    LoadAssemblyDefinitionFiles();
                }
                catch (Exception e)
                {
                    ShowLoadErrorExceptionGUI(e);
                    return;
                }
            }

            var platforms = Compilation.CompilationPipeline.GetAssemblyDefinitionPlatforms();

            using (new EditorGUI.DisabledScope(false))
            {
                EditorGUI.BeginChangeCheck();

                if (targets.Length > 1)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        var value = string.Join(", ", m_TargetStates.Select(t => t.name).ToArray());
                        EditorGUILayout.TextField("Name", value, EditorStyles.textField);
                    }
                }
                else
                {
                    m_State.name = EditorGUILayout.TextField("Name", m_State.name, EditorStyles.textField);
                }

                GUILayout.Label("References", EditorStyles.boldLabel);
                m_ReferencesList.DoLayoutList();

                GUILayout.Label("Platforms", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(GUI.skin.box);

                var compatibleWithAnyPlatform = m_State.compatibleWithAnyPlatform;
                m_State.compatibleWithAnyPlatform = ToggleWithMixedValue("Any Platform", m_State.compatibleWithAnyPlatform);

                if (compatibleWithAnyPlatform == MixedBool.Mixed && m_State.compatibleWithAnyPlatform != MixedBool.Mixed)
                {
                    // Switching from mixed state to non-mixed state.
                    // Invert state include/exclude compatibility of states that have the opposite compatibility,
                    // so all states are either include or exclude.
                    UpdatePlatformCompatibility(m_State.compatibleWithAnyPlatform, m_TargetStates);

                    // Now that we have potentially update the compatibility states, we now also
                    // need to update the combined state to reflect the changes.
                    UpdateCombinedCompatibility();
                }
                else if (m_State.compatibleWithAnyPlatform != compatibleWithAnyPlatform)
                {
                    InversePlatformCompatibility(m_State);
                }

                if (m_State.compatibleWithAnyPlatform != MixedBool.Mixed)
                {
                    GUILayout.Label(m_State.compatibleWithAnyPlatform == MixedBool.False ? "Exclude Platforms" : "Include Platforms", EditorStyles.boldLabel);

                    for (int i = 0; i < platforms.Length; ++i)
                    {
                        m_State.platformCompatibility[i] = ToggleWithMixedValue(platforms[i].DisplayName, m_State.platformCompatibility[i]);
                    }
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(10f);

                if (EditorGUI.EndChangeCheck())
                    m_State.modified = true;
            }

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(!m_State.modified))
            {
                if (GUILayout.Button("Revert"))
                {
                    LoadAssemblyDefinitionFiles();
                }

                if (GUILayout.Button("Apply"))
                {
                    SaveAndUpdateAssemblyDefinitionStates(m_State, m_TargetStates);
                }
            }

            GUILayout.EndHorizontal();
        }

        public override void OnDisable()
        {
            if (m_State != null && m_State.modified)
            {
                AssetImporter importer = target as AssetImporter;

                string dialogText = "Unapplied import settings for \'" + importer.assetPath + "\'";

                if (targets.Length > 1)
                    dialogText = "Unapplied import settings for \'" + targets.Length + "\' files";

                if (EditorUtility.DisplayDialog("Unapplied import settings", dialogText, "Apply", "Revert"))
                {
                    SaveAndUpdateAssemblyDefinitionStates(m_State, m_TargetStates);
                }
            }
        }

        static void UpdatePlatformCompatibility(MixedBool compatibleWithAnyPlatform, AssemblyDefintionState[] states)
        {
            if (compatibleWithAnyPlatform == MixedBool.Mixed)
                throw new ArgumentOutOfRangeException("compatibleWithAnyPlatform");

            foreach (var state in states)
            {
                // Same include/exclude compatibility
                if (state.compatibleWithAnyPlatform == compatibleWithAnyPlatform)
                    continue;

                // Opposite compatibility, invert.
                state.compatibleWithAnyPlatform = compatibleWithAnyPlatform;
                InversePlatformCompatibility(state);
            }
        }

        static MixedBool ToggleWithMixedValue(string title, MixedBool value)
        {
            EditorGUI.showMixedValue = value == MixedBool.Mixed;

            EditorGUI.BeginChangeCheck();

            bool newBoolValue = EditorGUILayout.Toggle(title, value == MixedBool.False);
            if (EditorGUI.EndChangeCheck())
                return newBoolValue ? MixedBool.False : MixedBool.True;

            EditorGUI.showMixedValue = false;
            return value;
        }

        static void InversePlatformCompatibility(AssemblyDefintionState state)
        {
            var platforms = Compilation.CompilationPipeline.GetAssemblyDefinitionPlatforms();

            for (int i = 0; i < platforms.Length; ++i)
                state.platformCompatibility[i] = InverseCompability(state.platformCompatibility[i]);
        }

        static MixedBool InverseCompability(MixedBool compatibility)
        {
            if (compatibility == MixedBool.False)
                return MixedBool.True;

            if (compatibility == MixedBool.True)
                return MixedBool.False;

            return MixedBool.Mixed;
        }

        void ShowLoadErrorExceptionGUI(Exception e)
        {
            if (m_TextStyle == null)
                m_TextStyle = "ScriptText";

            GUILayout.Label("Load Error", EditorStyles.boldLabel);
            Rect rect = GUILayoutUtility.GetRect(EditorGUIUtility.TempContent(e.Message), m_TextStyle);
            EditorGUI.HelpBox(rect, e.Message, MessageType.Error);
        }

        void LoadAssemblyDefinitionFiles()
        {
            m_TargetStates = new AssemblyDefintionState[targets.Length];

            for (int i = 0; i < targets.Length; ++i)
            {
                var importer = targets[i] as AssetImporter;

                if (importer == null)
                    continue;

                m_TargetStates[i] = LoadAssemblyDefintionState(importer.assetPath);
            }

            // Show as many references as the shortest list of references.
            int minReferencesCount = m_TargetStates.Min(t => t.references.Count());

            m_State = new AssemblyDefintionState();
            m_State.name = m_TargetStates[0].name;
            m_State.references = new List<AssemblyDefinitionReference>();
            m_State.modified = m_TargetStates[0].modified;

            for (int i = 0; i < minReferencesCount; ++i)
                m_State.references.Add(m_TargetStates[0].references[i]);

            for (int i = 1; i < m_TargetStates.Length; ++i)
            {
                var targetState = m_TargetStates[i];

                for (int r = 0; r < minReferencesCount; ++r)
                {
                    // If already set to mixed, continue.
                    if (m_State.references[r].displayValue == MixedBool.Mixed)
                        continue;

                    // If different from existing value, set to mixed.
                    if (m_State.references[r].path != targetState.references[r].path)
                        m_State.references[r].displayValue = MixedBool.Mixed;
                }

                m_State.modified |= targetState.modified;
            }

            UpdateCombinedCompatibility();

            m_ReferencesList = new ReorderableList(m_State.references, typeof(AssemblyDefinitionReference), false, false, true, true);
            m_ReferencesList.drawElementCallback = DrawReferenceListElement;
            m_ReferencesList.onAddCallback = AddReferenceListElement;
            m_ReferencesList.onRemoveCallback = RemoveReferenceListElement;


            m_ReferencesList.elementHeight = EditorGUIUtility.singleLineHeight + 2;
            m_ReferencesList.headerHeight = 3;
        }

        void UpdateCombinedCompatibility()
        {
            // Merge platform compability for all targets
            m_State.compatibleWithAnyPlatform = m_TargetStates[0].compatibleWithAnyPlatform;

            var platforms = Compilation.CompilationPipeline.GetAssemblyDefinitionPlatforms();
            m_State.platformCompatibility = new MixedBool[platforms.Length];

            Array.Copy(m_TargetStates[0].platformCompatibility, m_State.platformCompatibility, platforms.Length);

            for (int i = 1; i < m_TargetStates.Length; ++i)
            {
                var targetState = m_TargetStates[i];

                if (m_State.compatibleWithAnyPlatform != MixedBool.Mixed)
                {
                    if (m_State.compatibleWithAnyPlatform != targetState.compatibleWithAnyPlatform)
                        m_State.compatibleWithAnyPlatform = MixedBool.Mixed;
                }

                for (int p = 0; p < platforms.Length; ++p)
                {
                    // If already set to mixed, continue.
                    if (m_State.platformCompatibility[p] == MixedBool.Mixed)
                        continue;

                    // If different from existing value, set to mixed.
                    if (m_State.platformCompatibility[p] != targetState.platformCompatibility[p])
                        m_State.platformCompatibility[p] = MixedBool.Mixed;
                }
            }
        }

        static AssemblyDefintionState LoadAssemblyDefintionState(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);

            if (asset == null)
                return null;

            var data = CustomScriptAssemblyData.FromJson(asset.text);

            if (data == null)
                return null;

            var state = new AssemblyDefintionState();

            state.asset = asset;
            state.name = data.name;
            state.references = new List<AssemblyDefinitionReference>();

            if (data.references != null)
            {
                foreach (var reference in data.references)
                {
                    try
                    {
                        var assemblyDefinitionFile = new AssemblyDefinitionReference();
                        var referencePath = Compilation.CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(reference);

                        if (string.IsNullOrEmpty(referencePath))
                            throw new AssemblyDefinitionException(string.Format("Could not find assembly reference '{0}'", reference), path);

                        assemblyDefinitionFile.asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(referencePath);

                        if (assemblyDefinitionFile.asset == null)
                            throw new AssemblyDefinitionException(string.Format("Reference assembly definition file '{0}' not found", referencePath), path);

                        assemblyDefinitionFile.data = CustomScriptAssemblyData.FromJson(assemblyDefinitionFile.asset.text);
                        assemblyDefinitionFile.displayValue = MixedBool.True;
                        state.references.Add(assemblyDefinitionFile);
                    }
                    catch (AssemblyDefinitionException e)
                    {
                        UnityEngine.Debug.LogException(e, asset);
                        state.references.Add(new AssemblyDefinitionReference());
                        state.modified = true;
                    }
                }
            }

            var platforms = Compilation.CompilationPipeline.GetAssemblyDefinitionPlatforms();
            state.platformCompatibility = new MixedBool[platforms.Length];

            state.compatibleWithAnyPlatform = MixedBool.False;
            string[] dataPlatforms = null;

            if (data.includePlatforms != null && data.includePlatforms.Length > 0)
            {
                state.compatibleWithAnyPlatform = MixedBool.True;
                dataPlatforms = data.includePlatforms;
            }
            else if (data.excludePlatforms != null && data.excludePlatforms.Length > 0)
            {
                state.compatibleWithAnyPlatform = MixedBool.False;
                dataPlatforms = data.excludePlatforms;
            }

            if (dataPlatforms != null)
                foreach (var platform in dataPlatforms)
                {
                    var platformIndex = GetPlatformIndex(platforms, platform);
                    state.platformCompatibility[platformIndex] = MixedBool.False;
                }

            return state;
        }

        static AssemblyDefinitionReference CreateAssemblyDefinitionReference(string assemblyName)
        {
            var assemblyDefinitionFile = new AssemblyDefinitionReference();
            var path = Compilation.CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(assemblyName);

            if (string.IsNullOrEmpty(path))
                throw new System.Exception(string.Format("Could not get assembly definition filename for assembly '{0}'", assemblyName));

            assemblyDefinitionFile.asset = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);

            if (assemblyDefinitionFile.asset == null)
                throw new FileNotFoundException(string.Format("Assembly definition file '{0}' not found", assemblyDefinitionFile.path), assemblyDefinitionFile.path);

            assemblyDefinitionFile.data = CustomScriptAssemblyData.FromJson(assemblyDefinitionFile.asset.text);

            return assemblyDefinitionFile;
        }

        static void SaveAndUpdateAssemblyDefinitionStates(AssemblyDefintionState combinedState, AssemblyDefintionState[] states)
        {
            int combinedReferenceCount = combinedState.references.Count();

            // Update the name if there is only one file selected.
            if (states.Length == 1)
                states[0].name = combinedState.name;

            foreach (var state in states)
            {
                for (int i = 0; i < combinedReferenceCount; ++i)
                {
                    if (combinedState.references[i].displayValue != MixedBool.Mixed)
                        state.references[i] = combinedState.references[i];
                }

                if (combinedState.compatibleWithAnyPlatform != MixedBool.Mixed)
                    state.compatibleWithAnyPlatform = combinedState.compatibleWithAnyPlatform;

                for (int i = 0; i < combinedState.platformCompatibility.Length; ++i)
                {
                    if (combinedState.platformCompatibility[i] != MixedBool.Mixed)
                        state.platformCompatibility[i] = combinedState.platformCompatibility[i];
                }

                SaveAssemblyDefinitionState(state);
            }

            combinedState.modified = false;
        }

        static void SaveAssemblyDefinitionState(AssemblyDefintionState state)
        {
            var references = state.references.Where(r => r.asset != null);
            var platforms = Compilation.CompilationPipeline.GetAssemblyDefinitionPlatforms();

            CustomScriptAssemblyData data = new CustomScriptAssemblyData();

            data.name = state.name;

            data.references = references.Select(r => r.data.name).ToArray();

            List<string> dataPlatforms = new List<string>();

            for (int i = 0; i < platforms.Length; ++i)
            {
                if (state.platformCompatibility[i] == MixedBool.False)
                    dataPlatforms.Add(platforms[i].Name);
            }

            if (dataPlatforms.Any())
            {
                if (state.compatibleWithAnyPlatform == MixedBool.False)
                    data.excludePlatforms = dataPlatforms.ToArray();
                else
                    data.includePlatforms = dataPlatforms.ToArray();
            }

            var json = CustomScriptAssemblyData.ToJson(data);
            File.WriteAllText(state.path, json);
            state.modified = false;

            AssetDatabase.ImportAsset(state.path);
        }

        static int GetPlatformIndex(AssemblyDefinitionPlatform[] platforms, string name)
        {
            for (int i = 0; i < platforms.Length; ++i)
            {
                if (string.Equals(platforms[i].Name, name, System.StringComparison.InvariantCultureIgnoreCase))
                    return i;
            }

            throw new System.ArgumentException(string.Format("Unknown platform '{0}'", name) , name);
        }

        void DrawReferenceListElement(Rect rect, int index, bool selected, bool focused)
        {
            var list = m_ReferencesList.list;
            var assemblyDefinitionFile = list[index] as AssemblyDefinitionReference;

            rect.height -= 2;
            var label = assemblyDefinitionFile.data != null ? assemblyDefinitionFile.data.name : "(Missing Reference)";
            var asset = assemblyDefinitionFile.asset;

            bool mixed = assemblyDefinitionFile.displayValue == MixedBool.Mixed;
            EditorGUI.showMixedValue = mixed;
            assemblyDefinitionFile.asset = EditorGUI.ObjectField(rect, mixed ? "(Multiple Values)" : label, asset, typeof(AssemblyDefinitionAsset), false) as AssemblyDefinitionAsset;
            EditorGUI.showMixedValue = false;

            if (asset != assemblyDefinitionFile.asset && assemblyDefinitionFile.asset != null)
            {
                assemblyDefinitionFile.data = CustomScriptAssemblyData.FromJson(assemblyDefinitionFile.asset.text);

                foreach (var state in m_TargetStates)
                    state.references[index] = assemblyDefinitionFile;
            }
        }

        void AddReferenceListElement(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoAddButton(list);

            foreach (var state in m_TargetStates)
            {
                // Only add references to lists that are smaller or equal to the combined references list size.
                if (state.references.Count <= list.count)
                {
                    int index = Math.Min(list.index, state.references.Count());
                    state.references.Insert(index, list.list[list.index] as AssemblyDefinitionReference);
                }
            }
        }

        void RemoveReferenceListElement(ReorderableList list)
        {
            foreach (var state in m_TargetStates)
                state.references.RemoveAt(list.index);

            ReorderableList.defaultBehaviours.DoRemoveButton(list);
        }
    }
}
