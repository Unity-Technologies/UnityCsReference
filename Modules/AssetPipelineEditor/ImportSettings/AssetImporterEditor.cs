// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditorInternal;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.AssetImporters
{
    [MovedFrom("UnityEditor.Experimental.AssetImporters")]
    public abstract partial class AssetImporterEditor : Editor
    {
        /// <summary>
        /// This class allows us to save the dirty state of the current editor targets.
        /// We save it on each ApplyModifiedProperties (at the end of the Inspector GUI loop)
        /// If the dirty count changed during the Update (at the beginning of the Inspector GUI loop)
        /// That means the target has been updated outside of the editor (by calling Reset, applying a Preset or performing any static menu action)
        /// And thus we need to re-initialize the extra instances before updating the serializedObject.
        /// </summary>
        protected sealed class ExtraDataSerializedObject : SerializedObject
        {
            List<int> m_TargetDirtyCount;
            AssetImporterEditor m_Editor;

            private ExtraDataSerializedObject(Object obj)
                : base(obj) {}

            private ExtraDataSerializedObject(Object obj, Object context)
                : base(obj, context) {}

            private ExtraDataSerializedObject(Object[] objs)
                : base(objs) {}

            private ExtraDataSerializedObject(Object[] objs, Object context)
                : base(objs, context) {}

            internal ExtraDataSerializedObject(Object[] objs, AssetImporterEditor editor)
                : base(objs)
            {
                m_Editor = editor;
            }

            void UpdateTargetDirtyCount()
            {
                if (m_Editor != null)
                {
                    if (m_TargetDirtyCount != null)
                    {
                        for (int i = 0; i < m_Editor.targets.Length; i++)
                        {
                            var newCount = EditorUtility.GetDirtyCount(m_Editor.targets[i]);
                            if (m_TargetDirtyCount[i] != newCount)
                            {
                                m_TargetDirtyCount[i] = newCount;
                                m_Editor.InitializeExtraDataInstance(targetObjects[i], i);
                            }
                        }
                    }
                }
            }

            void SaveTargetDirtyCount()
            {
                if (m_Editor != null)
                {
                    if (m_TargetDirtyCount == null)
                    {
                        m_TargetDirtyCount = new int[m_Editor.targets.Length].ToList();
                    }

                    for (int i = 0; i < m_Editor.targets.Length; i++)
                    {
                        m_TargetDirtyCount[i] = EditorUtility.GetDirtyCount(m_Editor.targets[i]);
                    }
                }
            }

            public new void Update()
            {
                UpdateTargetDirtyCount();
                base.Update();
            }

            public new void UpdateIfRequiredOrScript()
            {
                UpdateTargetDirtyCount();
                base.UpdateIfRequiredOrScript();
            }

            public new void ApplyModifiedProperties()
            {
                SaveTargetDirtyCount();
                base.ApplyModifiedProperties();
            }

            public new void SetIsDifferentCacheDirty()
            {
                SaveTargetDirtyCount();
                base.SetIsDifferentCacheDirty();
            }
        }

        static partial class Styles
        {
            public static string localizedTitleString = L10n.Tr("{0} Import Settings");

            public static string applyButton = L10n.Tr("Apply");
            public static string revertButton = L10n.Tr("Revert");
            public static string unappliedSettingSingleAsset = L10n.Tr("Unapplied import settings for \'{0}\'");
            public static string unappliedSettingMultipleAssets = L10n.Tr("Unapplied import settings for \'{0}\' files");
            public static string unableToAppliedMessage = L10n.Tr("Your changes might contain errors and cannot be applied. \nYou can either \'Revert\' the changes, or hit \'Cancel\' to go back and fix the errors.");
        }

        // Target asset values, these are the main imported object Editor and targets.
        Editor m_AssetEditor;
        protected internal Object[] assetTargets { get { return m_AssetEditor != null ? m_AssetEditor.targets : null; } }
        protected internal Object assetTarget { get { return m_AssetEditor != null ? m_AssetEditor.target : null; } }
        protected internal SerializedObject assetSerializedObject { get { return m_AssetEditor != null ? m_AssetEditor.serializedObject : null; } }

        // Importer Custom Data. Users should register a custom SerializedObject
        // if they want to modify data outside the Importer serialization in the ImporterInspector.
        // This allow support for multiple inspectors, multiple selections and assembly reload.
        // See an example usage in AssemblyDefinitionImporterInspector.
        Object[] m_ExtraDataTargets;
        protected Object[] extraDataTargets
        {
            get
            {
                if (!m_AllowMultiObjectAccess)
                    Debug.LogError("The targets array should not be used inside OnSceneGUI or OnPreviewGUI. Use the single target property instead.");
                return m_ExtraDataTargets;
            }
        }

        protected Object extraDataTarget => m_ExtraDataTargets[referenceTargetIndex];

        ExtraDataSerializedObject m_ExtraDataSerializedObject;
        protected ExtraDataSerializedObject extraDataSerializedObject
        {
            get
            {
                if (extraDataType != null)
                {
                    if (m_ExtraDataSerializedObject == null)
                    {
                        m_ExtraDataSerializedObject = new ExtraDataSerializedObject(m_ExtraDataTargets, this);
                    }
                }
                return m_ExtraDataSerializedObject;
            }
        }

        // when no asset is accessible from the AssetImporter target
        // we are applying changes directly to the target and ignore the Apply/Revert mechanism.
        bool m_InstantApply = true;
        // This allow Importers to ignore the Apply/Revert mechanism and save their changes each update like normal Editor.
        protected virtual bool needsApplyRevert => !m_InstantApply && !EditorUtility.IsHiddenInInspector(target);

        List<int> m_TargetsInstanceID;
        // Check to make sure Users implemented their Inspector correctly for the Cancel deselection mechanism.
        bool m_ApplyRevertGUICalled;
        // Adding a check on OnEnable to make sure users call the base class, as it used to do nothing.
        bool m_OnEnableCalled;

        // Called from ActiveEditorTracker.cpp to setup the target editor once created before Awake and OnEnable of the Editor.
        internal void InternalSetAssetImporterTargetEditor(Object editor)
        {
            m_AssetEditor = editor as Editor;
            m_InstantApply = m_AssetEditor == null || m_AssetEditor.target == null;
        }

        void CheckExtraDataArray()
        {
            if (extraDataType != null)
            {
                if (!typeof(ScriptableObject).IsAssignableFrom(extraDataType))
                {
                    Debug.LogError("Extra Data objects needs to be ScriptableObject to support assembly reloads and Undo/Redo");
                    m_ExtraDataTargets = null;
                }
                else
                {
                    var tempObject = ScriptableObject.CreateInstance(extraDataType);
                    if (MonoScript.FromScriptableObject(tempObject) == null)
                    {
                        Debug.LogWarning($"Unable to find a MonoScript for {extraDataType.FullName}. The inspector may not reload properly after an assembly reload. Check that the definition is in a file of the same name.");
                    }
                    DestroyImmediate(tempObject);
                    m_ExtraDataTargets = new Object[targets.Length];
                }
            }
            else
            {
                m_ExtraDataTargets = null;
            }
        }

        void InitializeUnsavedChangesCache()
        {
            var editors = Resources.FindObjectsOfTypeAll(this.GetType()).Cast<AssetImporterEditor>().ToList();

            CheckExtraDataArray();
            var loadedIds = new List<int>(targets.Length);
            for (int i = 0; i < targets.Length; ++i)
            {
                int instanceID = targets[i].GetInstanceID();
                loadedIds.Add(instanceID);
                var extraData = CreateOrReloadInspectorCopy(instanceID, this);
                if (m_ExtraDataTargets != null)
                {
                    // we got the data from another instance
                    if (extraData != null)
                        m_ExtraDataTargets[i] = extraData;
                    else
                    {
                        m_ExtraDataTargets[i] = ScriptableObject.CreateInstance(extraDataType);
                        m_ExtraDataTargets[i].hideFlags = HideFlags.DontUnloadUnusedAsset | HideFlags.DontSaveInEditor;
                        InitializeExtraDataInstance(m_ExtraDataTargets[i], i);
                        SaveUserData(instanceID, m_ExtraDataTargets[i]);
                    }
                }

                // proceed to an editor count check to make sure we have the proper number of instances saved.
                // If it is not the case, then a dispose was not done properly.
                // We are selecting all Editor instances already enabled and ourselves.
                // This is because when coming back from an assembly reload,
                // the Editors already exist but get removed from the cache in their OnDisable, so we don't count them until its their turn to be Enabled back.
                var allEditors = editors.Where(e => e == this || (e.m_OnEnableCalled && e.targets.Contains(targets[i]))).Select(e => e.GetInstanceID()).ToArray();
                var instances = GetInspectorCopyCount(instanceID);
                if (allEditors.Length != instances)
                {
                    if (!CanEditorSurviveAssemblyReload())
                    {
                        Debug.LogError(
                            $"The previous instance of {GetType()} was not un-loaded properly. The script has to be declared in a file with the same name.");
                    }
                    else
                    {
                        Debug.LogError(
                            $"The previous instance of {GetType()} has not been disposed correctly. Make sure you are calling base.OnDisable() in your AssetImporterEditor implementation.");
                    }

                    // Fix the cache count so it does not fail anymore.
                    FixCacheCount(instanceID, allEditors);
                }
            }
            m_TargetsInstanceID = loadedIds;
        }

        void FixImporterAssetbundleName(string arg1, string arg2)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                var importer = targets[i] as AssetImporter;
                if (importer != null && importer.assetPath == arg1)
                {
                    FixSavedAssetbundleSettings(importer.GetInstanceID(), new PropertyModification[]
                    {
                        new PropertyModification()
                        {
                            objectReference = importer,
                            propertyPath = "m_AssetBundleName",
                            target = null,
                            value = importer.assetBundleName
                        }, new PropertyModification()
                        {
                            objectReference = importer,
                            propertyPath = "m_AssetBundleVariant",
                            target = null,
                            value = importer.assetBundleVariant
                        }
                    });
                }
            }
        }

        // Mechanism to register a ScriptableObject type that will be check along the Importer serialization.
        // This is useful to help with the apply/revert mechanism on importers that store data outside their own serialization
        // See an example usage in AssemblyDefinitionImporterInspector.
        protected virtual Type extraDataType => null;
        protected virtual void InitializeExtraDataInstance(Object extraData, int targetIndex)
        {
            throw new NotImplementedException("InitializeExtraDataInstance must be implemented when extraDataType is overridden.");
        }

        internal override string targetTitle
        {
            get
            {
                return string.Format(Styles.localizedTitleString, m_AssetEditor == null ? string.Empty : m_AssetEditor.targetTitle);
            }
        }

        internal sealed override int referenceTargetIndex
        {
            get { return base.referenceTargetIndex; }
            set
            {
                base.referenceTargetIndex = value;
                if (m_AssetEditor != null)
                    m_AssetEditor.referenceTargetIndex = value;
            }
        }

        internal override IPreviewable preview
        {
            get
            {
                if (useAssetDrawPreview && m_AssetEditor != null)
                    return m_AssetEditor;
                // Sometimes assetEditor has gone away because of "magical" workarounds and we need to fall back to base.Preview.
                // See cases 597496 and 601174 for context.
                return base.preview;
            }
        }

        public override void DrawPreview(Rect previewArea)
        {
            // If the importer is drawing the previews,
            // respect that when passing through to object preview helper
            var previewable = useAssetDrawPreview ? preview : this;
            ObjectPreview.DrawPreview(previewable, previewArea, assetTargets);
        }

        //We usually want to redirect the DrawPreview to the assetEditor, but there are few cases we don't want that.
        //If you want to use the Importer DrawPreview, then override useAssetDrawPreview to false.
        protected virtual bool useAssetDrawPreview { get { return true; } }

        internal override void OnHeaderControlsGUI()
        {
            DrawImporterSelectionPopup();

            GUILayout.FlexibleSpace();

            if (!ShouldHideOpenButton())
            {
                var assets = assetTargets;
                ShowOpenButton(assets, assetTarget != null);
            }
        }

        // Make the Importer use the icon of the asset
        internal override void OnHeaderIconGUI(Rect iconRect)
        {
            if (m_AssetEditor != null)
                m_AssetEditor.OnHeaderIconGUI(iconRect);
            else
                base.OnHeaderIconGUI(iconRect);
        }

        // Let asset importers decide if the imported object should be shown as a separate editor or not
        public virtual bool showImportedObject { get { return true; } }

        protected virtual void Awake()
        {
        }

        public virtual void OnEnable()
        {
            AssetImporterEditorPostProcessAsset.OnAssetbundleNameChanged += FixImporterAssetbundleName;

            InitializeAvailableImporters();
            InitializeUnsavedChangesCache();
            InitializePostprocessors();

            saveChangesMessage = targets.Length == 1
                ? string.Format(Styles.unappliedSettingSingleAsset, GetAssetPaths().First())
                : string.Format(Styles.unappliedSettingMultipleAssets, targets.Length);

            m_OnEnableCalled = true;
            // Forces the inspector as dirty allows us to make sure the OnInspectorGUI has been called
            // at least once in the OnDisable in order to show the ApplyRevertGUI error.
            isInspectorDirty = true;
        }

        public virtual void OnDisable()
        {
            AssetImporterEditorPostProcessAsset.OnAssetbundleNameChanged -= FixImporterAssetbundleName;

            if (!m_OnEnableCalled)
            {
                Debug.LogError($"{this.GetType().Name}.OnEnable must call base.OnEnable to avoid unexpected behaviour.");
            }

            // do not check on m_ApplyRevertGUICalled if OnEnable was never called
            // or we are closing before OnInspectorGUI have been called (which is the case in most of our EditorTests)
            if (m_OnEnableCalled && needsApplyRevert && !isInspectorDirty && !m_ApplyRevertGUICalled)
            {
                Debug.LogError($"{this.GetType().Name}.OnInspectorGUI must call ApplyRevertGUI to avoid unexpected behaviour.");
            }

            m_OnEnableCalled = false;
            m_ApplyRevertGUICalled = false;

            foreach (var t in m_TargetsInstanceID)
            {
                ReleaseInspectorCopy(t, this);
            }

            // Let's make sure everything get forced apply in case the Editor instance is destroyed with pending changes.
            // The changes are made in the Importer instance already anyway and will be pickup at a random time otherwise.
            if (hasUnsavedChanges)
            {
                SaveChanges();
            }
        }

        bool CanEditorSurviveAssemblyReload()
        {
            using (var so = new SerializedObject(this))
            using (var prop = so.FindProperty("m_Script"))
            {
                var script = prop.objectReferenceValue as MonoScript;
                return script != null && AssetDatabase.Contains(script);
            }
        }

        public override void OnInspectorGUI()
        {
            DoDrawDefaultInspector(serializedObject);
            if (extraDataType != null)
                DoDrawDefaultInspector(extraDataSerializedObject);
            ApplyRevertGUI();
        }

        IEnumerable<string> GetAssetPaths()
        {
            return targets.OfType<AssetImporter>().Select(i => i.assetPath);
        }

        public virtual bool HasModified()
        {
            serializedObject.ApplyModifiedProperties();
            extraDataSerializedObject?.ApplyModifiedProperties();
            for (int i = 0; i < targets.Length; ++i)
                if (!IsSerializedDataEqual(targets[i]))
                    return true;
            return false;
        }

        protected virtual bool CanApply()
        {
            return true;
        }

        protected virtual void Apply()
        {
            serializedObject.ApplyModifiedProperties();
            extraDataSerializedObject?.ApplyModifiedProperties();
            for (int i = 0; i < targets.Length; ++i)
                UpdateSavedData(targets[i]);
        }

        public override void SaveChanges()
        {
            base.SaveChanges();

            Apply();
            ImportAssets(GetAssetPaths());
            // Re-import of assets may change settings dur AssetPostprocessors
            // We have to make sure we update the saved data after the import is done
            // so users see the real state of the importer once the import is finished.
            for (int i = 0; i < targets.Length; i++)
            {
                UpdateSavedData(targets[i]);
            }
        }

        [Obsolete("UnityUpgradeable () -> SaveChanges")]
        protected internal void ApplyAndImport()
        {
            SaveChanges();
        }

        public override void DiscardChanges()
        {
            base.DiscardChanges();

            serializedObject.SetIsDifferentCacheDirty();
            extraDataSerializedObject?.SetIsDifferentCacheDirty();
            for (int i = 0; i < targets.Length; ++i)
                RevertObject(targets[i]);
            extraDataSerializedObject?.Update();
            serializedObject.Update();
        }

        [Obsolete("UnityUpgradeable () -> DiscardChanges")]
        protected virtual void ResetValues()
        {
            DiscardChanges();
        }

        static void ImportAssets(IEnumerable<string> paths)
        {
            // When using the cache server we have to write all import settings to disk first.
            // Then perform the import (Otherwise the cache server will not be used for the import)
            List<string> toReimport = new List<string>();
            foreach (var path in paths)
            {
                if (AssetDatabase.WriteImportSettingsIfDirty(path))
                    toReimport.Add(path);
            }
            AssetDatabase.StartAssetEditing();
            foreach (string path in toReimport)
                AssetDatabase.ImportAsset(path);
            AssetDatabase.StopAssetEditing();
        }

        protected void RevertButton()
        {
            if (GUILayout.Button(Styles.revertButton))
            {
                GUI.FocusControl(null);
                DiscardChanges();
                if (HasModified())
                    Debug.LogError("Importer reports modified values after reset.");
            }
        }

        protected bool ApplyButton()
        {
            using (new EditorGUI.DisabledScope(!CanApply()))
            {
                if (GUILayout.Button(Styles.applyButton))
                {
                    GUI.FocusControl(null);
                    SaveChanges();
                    return true;
                }
            }
            return false;
        }

        protected virtual bool OnApplyRevertGUI()
        {
            using (new EditorGUI.DisabledScope(!hasUnsavedChanges))
            {
                RevertButton();
                return ApplyButton();
            }
        }

        protected void ApplyRevertGUI()
        {
            m_ApplyRevertGUICalled = true;

            hasUnsavedChanges = HasModified();

            if (serializedObject.hasModifiedProperties)
            {
                Debug.LogWarning("OnInspectorGUI should call serializedObject.Update() at its beginning and serializedObject.ApplyModifiedProperties() before calling ApplyRevertGUI() method.");
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }

            if (extraDataSerializedObject != null && extraDataSerializedObject.hasModifiedProperties)
            {
                Debug.LogWarning("OnInspectorGUI should call extraDataSerializedObject.Update() at its beginning and extraDataSerializedObject.ApplyModifiedProperties() before calling ApplyRevertGUI() method.");
                extraDataSerializedObject.ApplyModifiedProperties();
                extraDataSerializedObject.Update();
            }

            if (needsApplyRevert)
            {
                EditorGUILayout.Space();
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    // need to start rendering again.
                    if (OnApplyRevertGUI())
                    {
                        // If applied is pressed, let's kill the GUI execution to avoid following action obsolete asset data.
                        GUIUtility.ExitGUI();
                    }
                }
            }
            else
            {
                if (extraDataSerializedObject != null && HasModified())
                    Apply(); // user may have extra data that needs to be applied back to the target.
            }

            DrawAssetPostprocessors();
        }
    }

    [MovedFrom("UnityEditor.Experimental.AssetImporters")]
    internal class AssetImporterEditorPostProcessAsset : AssetPostprocessor
    {
        public static event Action<string, string> OnAssetbundleNameChanged;

        void OnPostprocessAssetbundleNameChanged(string assetPath, string oldName, string newName)
        {
            OnAssetbundleNameChanged?.Invoke(assetPath, newName);
        }
    }

    // Part of the class handling the AssetPostprocessor UI
    public abstract partial class AssetImporterEditor
    {
        // Support for postprocessors display
        struct PostprocessorInfo
        {
            public string Name;
            public string[] Methods;
            public bool Expanded;
        }
        List<PostprocessorInfo> m_Postprocessors;
        ReorderableList m_PostprocessorUI;
        SavedBool m_ProcessorsAreExpanded;

        private void InitializePostprocessors()
        {
            /*
             * Combine Dynamic and Static Postprocessors into one ordered list - the user is unlikely to be interested in *how* we're registering the Assets Postprocessors under the hood.
             */
            SortedSet<AssetPostprocessor.PostprocessorInfo> allAssetImportProcessors = new SortedSet<AssetPostprocessor.PostprocessorInfo>(new AssetPostprocessingInternal.CompareAssetImportPriority());
            allAssetImportProcessors.UnionWith(((AssetImporter)target).GetDynamicPostprocessors());
            allAssetImportProcessors.UnionWith(AssetImporter.GetStaticPostprocessors(target.GetType()).Where(t => t.Type.Assembly != typeof(AssetImporter).Assembly));

            m_Postprocessors = new List<PostprocessorInfo>();
            foreach (var processor in allAssetImportProcessors)
            {
                m_Postprocessors.Add(new PostprocessorInfo()
                {
                    Expanded = false,
                    Methods = processor.Methods,
                    Name = processor.Type.FullName
                });
            }

            m_ProcessorsAreExpanded = new SavedBool("AssetImporterEditor_DisplayProcessors", true);
            m_PostprocessorUI = new ReorderableList(m_Postprocessors, typeof(string), false, true, false, false)
            {
                elementHeightCallback = index => m_Postprocessors[index].Expanded ? EditorGUIUtility.singleLineHeight * (m_Postprocessors[index].Methods.Length + 1) + 3f : EditorGUIUtility.singleLineHeight + 3f,
                drawElementCallback = DrawPostprocessorElement,
                headerHeight = ReorderableList.Defaults.minHeaderHeight,
                m_IsEditable = false,
                multiSelect = false,
                footerHeight = ReorderableList.Defaults.minHeaderHeight,
            };
        }

        void DrawPostprocessorElement(Rect rect, int index, bool active, bool focused)
        {
            EditorGUI.indentLevel++;

            var processor = m_Postprocessors[index];
            rect.yMax = rect.yMin + EditorGUIUtility.singleLineHeight;
            if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                GenericMenu pm = new GenericMenu();
                pm.AddItem(new GUIContent("Copy"), false, RightClickPostprocessor, processor.Name);
                pm.ShowAsContext();
            }

            processor.Expanded = EditorGUI.Foldout(rect, processor.Expanded, m_Postprocessors[index].Name, true);
            if (processor.Expanded)
            {
                EditorGUI.indentLevel++;
                foreach (var method in processor.Methods)
                {
                    rect.y += EditorGUIUtility.singleLineHeight;
                    EditorGUI.LabelField(rect, method);
                }
                EditorGUI.indentLevel--;
            }
            m_Postprocessors[index] = processor;

            EditorGUI.indentLevel--;
        }

        static void RightClickPostprocessor(object userdata)
        {
            EditorGUIUtility.systemCopyBuffer = (string)userdata;
        }

        private void DrawAssetPostprocessors()
        {
            if (m_Postprocessors.Count > 0)
            {
                EditorGUILayout.Space();
                m_ProcessorsAreExpanded.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_ProcessorsAreExpanded.value,
                    GUIContent.Temp("Asset PostProcessors"));
                EditorGUI.EndFoldoutHeaderGroup();
                if (m_ProcessorsAreExpanded)
                {
                    m_PostprocessorUI.DoLayoutList();
                }
            }
        }
    }

    // Part of the class handling the ImporterSelection
    public abstract partial class AssetImporterEditor
    {
        static partial class Styles
        {
            public static GUIContent ImporterSelection = EditorGUIUtility.TrTextContent("Importer");
            public static string defaultImporterName = L10n.Tr("{0} (Default)");
        }

        // Support for importer overrides
        List<Type> m_AvailableImporterTypes;
        const int k_MultipleSelectedImporterTypes = -1;
        int m_SelectedImporterType = k_MultipleSelectedImporterTypes;
        string[] m_AvailableImporterTypesOptions = {};

        private void DrawImporterSelectionPopup()
        {
            if (m_AvailableImporterTypes.Count < 2)
                return;
            var mixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = m_SelectedImporterType == k_MultipleSelectedImporterTypes;
            GUILayout.Label(Styles.ImporterSelection);
            var newSelection = EditorGUILayout.Popup(m_SelectedImporterType, m_AvailableImporterTypesOptions);
            if (newSelection != m_SelectedImporterType)
            {
                // cancel any pending changes if we are switching the importer.
                // It's going to do a full import with new settings anyway.
                if (hasUnsavedChanges)
                {
                    DiscardChanges();
                }

                AssetDatabase.StartAssetEditing();
                foreach (var importer in targets.Cast<AssetImporter>())
                {
                    Undo.RegisterImporterUndo(importer.assetPath, string.Empty);
                    //When selecting an override, set it as an override, when selecting the default importer, clear the override
                    if(m_AvailableImporterTypes[newSelection] != AssetDatabase.GetDefaultImporter(importer.assetPath))
                        AssetDatabase.SetImporterOverrideInternal(importer.assetPath, m_AvailableImporterTypes[newSelection]);
                    else
                        AssetDatabase.ClearImporterOverride(importer.assetPath);
                }
                AssetDatabase.StopAssetEditing();
                GUIUtility.ExitGUI();
            }
            EditorGUI.showMixedValue = mixed;
        }

        void InitializeAvailableImporters()
        {
            m_AvailableImporterTypes = new List<Type>(1);
            if (assetTarget == null)
                return;

            var targetsPaths = targets.OfType<AssetImporter>().Select(t => t.assetPath);

            var typeLists = targetsPaths.Select(AssetDatabase.GetAvailableImporters).ToList();
            m_AvailableImporterTypes.AddRange(typeLists.Aggregate(
                new HashSet<Type>(typeLists.First()),
                (h, e) =>
                {
                    h.IntersectWith(e);
                    return h;
                }));

            var defaultImporter = targetsPaths.Select(AssetDatabase.GetDefaultImporter).First();
            m_AvailableImporterTypesOptions = m_AvailableImporterTypes.Select(a => a == defaultImporter ? string.Format(Styles.defaultImporterName, defaultImporter.FullName) : a.FullName).ToArray();


            if (m_AvailableImporterTypes.Count > 0)
            {
                var selection = targets
                    .Select(t => t.GetType())
                    .Select(t => m_AvailableImporterTypes.IndexOf(t))
                    .Distinct();
                if (selection.Count() > 1)
                {
                    m_SelectedImporterType = k_MultipleSelectedImporterTypes;
                }
                else
                {
                    m_SelectedImporterType = selection.First();
                }
            }

        }
    }

}
