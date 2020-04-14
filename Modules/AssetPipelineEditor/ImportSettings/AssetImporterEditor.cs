// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.AssetImporters
{
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
                    // When coming back from a target reload
                    // this is the very first place we can catch mismatching saved data and fix them.
                    if (m_Editor.m_TargetsReloaded)
                    {
                        SaveTargetDirtyCount();
                        m_Editor.m_TargetsReloaded = false;
                        for (int i = 0; i < m_Editor.targets.Length; i++)
                            UpdateSavedData(m_Editor.targets[i]);
                        return;
                    }

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

        static class Styles
        {
            public static string localizedTitleString = L10n.Tr("{0} Import Settings");

            public static string unappliedSettingTitle = L10n.Tr("Unapplied import settings");
            public static string applyButton = L10n.Tr("Apply");
            public static string cancelButton = L10n.Tr("Cancel");
            public static string revertButton = L10n.Tr("Revert");
            public static string unappliedSettingSingleAsset = L10n.Tr("Unapplied import settings for \'{0}\'");
            public static string unappliedSettingMultipleAssets = L10n.Tr("Unapplied import settings for \'{0}\' files");
            public static string unableToAppliedMessage = L10n.Tr("Your changes might contain errors and cannot be applied. \nYou can either \'Revert\' the changes, or hit \'Cancel\' to go back and fix the errors.");

            public static GUIContent ImporterSelection = EditorGUIUtility.TrTextContent("Importer");
            public static string defaultImporterName = L10n.Tr("{0} (Default)");
        }

        // list of asset hashes. We need to force reload the inspector in case the asset changed on disk.
        Hash128[] m_AssetHashes;

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
        protected virtual bool needsApplyRevert => !m_InstantApply;

        // we need to keep a list of unreleased instances in case the user cancel the de-selection
        // we are using these instances to keep the same apply/revert status with the forced re-selection
        static List<int> s_UnreleasedInstances;
        List<int> m_TargetsInstanceID;
        // Check to make sure Users implemented their Inspector correctly for the Cancel deselection mechanism.
        bool m_ApplyRevertGUICalled;
        // Adding a check on OnEnable to make sure users call the base class, as it used to do nothing.
        bool m_OnEnableCalled;

        bool m_CopySaved = false;
        // Check on the Inspector status to identify the reason the AssetImporterEditor is being Disabled.
        // If the Inspector is null, it has been closed and need to be re-created if Cancel is pressed.
        // If the Inspector is not null but was Locked, then just force the list of locked Object back.
        // If the Inspector is not null and not Locked, then change Selection.Object list back.
        InspectorWindow m_Inspector;
        bool m_HasInspectorBeenSeenLocked = false;
        bool m_TargetsReloaded = false;

        // Support for importer overrides
        List<Type> m_AvailableImporterTypes;
        const int k_MultipleSelectedImporterTypes = -1;
        int m_SelectedImporterType = k_MultipleSelectedImporterTypes;
        string[] m_AvailableImporterTypesOptions;
        List<string> m_SelectedAssetsPath = new List<string>();

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

        // Called from a various number of places, like after an assembly reload or when the Editor gets created.
        internal sealed override void InternalSetTargets(Object[] t)
        {
            base.InternalSetTargets(t);

            if (m_CopySaved) // coming back from an assembly reload or asset re-import
            {
                if (extraDataType != null && m_ExtraDataTargets != null) // we need to recreate the user custom array
                {
                    // just get back the data from customSerializedData array, it gets serialized and reconstructed properly
                    m_ExtraDataTargets = extraDataSerializedObject.targetObjects;
                }
                ReloadTargets(AssetWasUpdated());
            }
            else // newly created editor
            {
                CheckExtraDataArray();
                var loadedIds = new List<int>(t.Length);
                for (int i = 0; i < t.Length; ++i)
                {
                    int instanceID = t[i].GetInstanceID();
                    loadedIds.Add(instanceID);
                    var extraData = CreateOrReloadInspectorCopy(instanceID);
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
                    var editors = Resources.FindObjectsOfTypeAll(this.GetType()).Cast<AssetImporterEditor>();
                    int count = editors.Count(e => e.targets.Contains(t[i]));
                    if (s_UnreleasedInstances != null)
                    {
                        count += s_UnreleasedInstances.Count(id => id == instanceID);
                    }
                    var instances = GetInspectorCopyCount(instanceID);
                    if (count != instances)
                    {
                        if (!CanEditorSurviveAssemblyReload())
                        {
                            Debug.LogError($"The previous instance of {GetType()} was not un-loaded properly. The script has to be declared in a file with the same name.");
                        }
                        else
                        {
                            Debug.LogError($"The previous instance of {GetType()} has not been disposed correctly. Make sure you are calling base.OnDisable() in your AssetImporterEditor implementation.");
                        }

                        // Fix the cache count so it does not fail anymore.
                        FixCacheCount(instanceID, count);
                    }
                }

                // Clean-up previous instances now that we reloaded the copies
                if (s_UnreleasedInstances != null)
                {
                    for (var index = s_UnreleasedInstances.Count - 1; index >= 0; index--)
                    {
                        var copy = s_UnreleasedInstances[index];
                        if (loadedIds.Contains(copy))
                        {
                            ReleaseInspectorCopy(copy);
                            s_UnreleasedInstances.RemoveAt(index);
                        }
                    }
                }

                m_TargetsInstanceID = loadedIds;
                m_CopySaved = true;
            }
        }

        void FixInspectorCache()
        {
            bool instanceNull = false;
            // make sure underlying data still match saved data
            for (int i = 0; i < targets.Length; i++)
            {
                // targets[i] may be null if the importer/asset was destroyed during an assembly reload
                if (targets[i] != null)
                    CheckForInspectorCopyBackingData(targets[i]);
                else
                    instanceNull = true;
            }

            // case 1153082 - Do not Update the serializedObject if at least one instance is null.
            // The editor will be destroyed and rebuilt anyway so it's fine to ignore it.
            if (!instanceNull)
            {
                extraDataSerializedObject?.Update();
                serializedObject.Update();
            }
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

        // prevent application quit if user cancel the setting changes apply/revert
        bool ApplicationWantsToQuit()
        {
            return CheckForApplyOnClose();
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

        //We usually want to redirect the DrawPreview to the assetEditor, but there are few cases we don't want that.
        //If you want to use the Importer DrawPreview, then override useAssetDrawPreview to false.
        protected virtual bool useAssetDrawPreview { get { return true; } }

        private void DrawImporterSelectionPopup()
        {
            if (m_AvailableImporterTypesOptions.Length < 2)
                return;
            var mixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = m_SelectedImporterType == k_MultipleSelectedImporterTypes;
            GUILayout.Label(Styles.ImporterSelection);
            var newSelection = EditorGUILayout.Popup(m_SelectedImporterType, m_AvailableImporterTypesOptions);
            if (newSelection != m_SelectedImporterType)
            {
                if (CheckForApplyOnClose(false))
                {
                    // TODO : this should probably be handled by the AssetDatabase code when restoring selection?
                    m_SelectedAssetsPath = assetTargets.Select(AssetDatabase.GetAssetPath).ToList();
                    EditorApplication.delayCall += () =>
                    {
                        var loaded = m_SelectedAssetsPath.Select(AssetDatabase.LoadMainAssetAtPath);
                        if (m_HasInspectorBeenSeenLocked)
                            m_Inspector.SetObjectsLocked(loaded.ToList());
                        else
                            Selection.objects = loaded.ToArray();
                    };
                    AssetDatabase.StartAssetEditing();
                    foreach (var importer in targets.Cast<AssetImporter>())
                    {
                        Undo.RegisterImporterUndo(importer.assetPath, string.Empty);
                        AssetDatabaseExperimental.SetImporterOverrideInternal(importer.assetPath, m_AvailableImporterTypes[newSelection]);
                    }
                    AssetDatabase.StopAssetEditing();
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUI.showMixedValue = mixed;
        }

        void InitializeAvailableImporters()
        {
            if (assetTarget == null)
            {
                m_AvailableImporterTypes = new List<Type>(0);
                m_AvailableImporterTypesOptions = new string[0];
                return;
            }

            var typeLists = targets.OfType<AssetImporter>()
                .Select(t => t.assetPath)
                .Select(AssetDatabaseExperimental.GetAvailableImporterTypes);
            m_AvailableImporterTypes = typeLists
                .Aggregate(new HashSet<Type>(typeLists.First()),
                (h, e) =>
                {
                    h.IntersectWith(e);
                    return h;
                })
                .Where(t => !t.IsAbstract && t != typeof(AssetImporter))
                .ToList();
            var assetExtension = Path.GetExtension(((AssetImporter)target).assetPath).Substring(1);
            m_AvailableImporterTypesOptions = m_AvailableImporterTypes.Select(a =>
            {
                if (!a.IsSubclassOf(typeof(ScriptedImporter)))
                {
                    return string.Format(Styles.defaultImporterName, a.FullName);
                }

                var attribute = a.GetCustomAttributes(typeof(ScriptedImporterAttribute), false).Cast<ScriptedImporterAttribute>().First();
#pragma warning disable 618
                // we have to check on AutoSelect value until this is Obsolete with error to keep the same behaviour with non upgraded user scripts.
                if (attribute.fileExtensions != null && attribute.AutoSelect && attribute.fileExtensions.Contains(assetExtension))
                    return string.Format(Styles.defaultImporterName, a.FullName);
#pragma warning restore 618
                return a.FullName;
            }).ToArray();
        }

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

        public virtual void OnEnable()
        {
            EditorApplication.wantsToQuit += ApplicationWantsToQuit;
            AssemblyReloadEvents.afterAssemblyReload += FixInspectorCache;
            AssetImporterEditorPostProcessAsset.OnAssetbundleNameChanged += FixImporterAssetbundleName;

            InitializeAvailableImporters();
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

            m_OnEnableCalled = true;
            // Forces the inspector as dirty allows us to make sure the OnInspectorGUI has been called
            // at least once in the OnDisable in order to show the ApplyRevertGUI error.
            isInspectorDirty = true;
        }

        public virtual void OnDisable()
        {
            EditorApplication.wantsToQuit -= ApplicationWantsToQuit;
            AssemblyReloadEvents.afterAssemblyReload -= FixInspectorCache;
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

            // When destroying the inspector check if we have any unapplied modifications
            // and apply them.
            if (Unsupported.IsDestroyScriptableObject(this))
            {
                if (!CheckForApplyOnClose())
                {
                    // we need to force back the current tracker to our assetTargets
                    if (!IsClosingInspector())
                    {
                        if (m_HasInspectorBeenSeenLocked)
                            m_Inspector.SetObjectsLocked(new List<Object>(assetTargets));
                        else
                            Selection.objects = assetTargets;
                    }
                    else
                    {
                        var inspector = ScriptableObject.CreateInstance<InspectorWindow>();
                        if (m_HasInspectorBeenSeenLocked)
                            inspector.SetObjectsLocked(new List<Object>(assetTargets));
                        else
                            Selection.objects = assetTargets;
                        inspector.Show(true);
                    }

                    if (s_UnreleasedInstances == null)
                    {
                        s_UnreleasedInstances = new List<int>();
                    }
                    foreach (var t in m_TargetsInstanceID)
                    {
                        s_UnreleasedInstances.Add(t);
                    }
                }
                else
                {
                    foreach (var t in m_TargetsInstanceID)
                    {
                        ReleaseInspectorCopy(t);
                    }
                }
            }

            m_OnEnableCalled = false;
            m_ApplyRevertGUICalled = false;
        }

        bool CanEditorSurviveAssemblyReload()
        {
            var script = new SerializedObject(this).FindProperty("m_Script").objectReferenceValue as MonoScript;
            return script != null && AssetDatabase.Contains(script);
        }

        bool IsClosingInspector()
        {
            return m_ApplyRevertGUICalled && (m_Inspector == null || !InspectorWindow.GetInspectors().Contains(m_Inspector));
        }

        bool CheckForApplyOnClose(bool isQuitting = false)
        {
            if (!needsApplyRevert)
                return true;

            // I've tried to do a smart system that only apply/re-import changed files
            // But because users can override HasModified and have custom changes to any selected files
            // We cannot make sure only some files changed and not all...
            // So we are selecting the list of files we may have to re-import (last inspector and hash not changed)
            // and ask the user to save/discard only those ones.
            var diskModifiedAssets = AssetWasUpdated().Reverse().ToList();
            List<string> assetPaths = new List<string>(targets.Length);
            for (int i = 0; i < targets.Length; i++)
            {
                // skip any asset that is part of the disk modified ones
                if (diskModifiedAssets.Count > 0 && i == diskModifiedAssets[diskModifiedAssets.Count - 1])
                {
                    // make sure we are cancelling the changes here so that asset will re-import with previous values.
                    ResetHash(i);
                    ReloadAssetData(i);
                    diskModifiedAssets.RemoveAt(diskModifiedAssets.Count - 1);
                    continue;
                }
                var importer = targets[i] as AssetImporter;
                // Importer may be null if the selected asset was destroyed
                if (importer != null)
                {
                    int copyCount = GetInspectorCopyCount(importer.GetInstanceID());
                    if (isQuitting || copyCount == 1)
                    {
                        assetPaths.Add(importer.assetPath);
                    }
                }
            }

            if (assetPaths.Count > 0 && HasModified())
            {
                // Forces the Reset button action when in batchmode instead of cancel, or the application may not leave when running tests...
                if (Application.isBatchMode || !Application.isHumanControllingUs)
                {
                    ResetValues();
                    return true;
                }

                return ShowUnappliedAssetsPopup(assetPaths);
            }
            return true;
        }

        bool ShowUnappliedAssetsPopup(List<string> assetPaths)
        {
            var dialogText = assetPaths.Count == 1
                ? string.Format(Styles.unappliedSettingSingleAsset, assetPaths[0])
                : string.Format(Styles.unappliedSettingMultipleAssets, assetPaths.Count);

            if (CanApply())
            {
                var userChoice = EditorUtility.DisplayDialogComplex(Styles.unappliedSettingTitle, dialogText, Styles.applyButton, Styles.cancelButton, Styles.revertButton);
                switch (userChoice)
                {
                    case 0:
                        Apply(); // we need to call Apply before re-importing in case the user overridden it.
                        ImportAssets(assetPaths.ToArray());
                        break;
                    case 1:
                        return false;
                    case 2:
                        ResetValues();
                        break;
                }
            }
            else
            {
                dialogText = dialogText + "\n" + Styles.unableToAppliedMessage;
                if (EditorUtility.DisplayDialog(Styles.unappliedSettingTitle, dialogText, Styles.revertButton, Styles.cancelButton))
                {
                    ResetValues();
                    return true;
                }
                return false;
            }
            return true;
        }

        protected virtual void Awake()
        {
            ResetHash();
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

        private void ReloadAssetData(int index)
        {
            if (extraDataSerializedObject != null)
            {
                extraDataSerializedObject.SetIsDifferentCacheDirty();
                InitializeExtraDataInstance(m_ExtraDataTargets[index], index);
                extraDataSerializedObject.Update();
            }
            UpdateSavedData(targets[index]);
        }

        protected virtual void ResetValues()
        {
            serializedObject.SetIsDifferentCacheDirty();
            extraDataSerializedObject?.SetIsDifferentCacheDirty();
            for (int i = 0; i < targets.Length; ++i)
                RevertObject(targets[i]);
            extraDataSerializedObject?.Update();
            serializedObject.Update();
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

        IEnumerable<int> AssetWasUpdated()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                var importer = targets[i] as AssetImporter;
                // check for AssetImporter being null as it may have been destroyed when closing...
                if (importer != null && m_AssetHashes[i] != AssetDatabase.GetAssetDependencyHash(importer.assetPath))
                    yield return i;
            }
        }

        private void ResetHash()
        {
            m_AssetHashes = new Hash128[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                ResetHash(i);
            }
        }

        private void ResetHash(int index)
        {
            m_AssetHashes[index] = AssetDatabase.GetAssetDependencyHash(((AssetImporter)targets[index]).assetPath);
        }

        protected internal void ApplyAndImport()
        {
            Apply();
            ImportAssets(GetAssetPaths());
        }

        static void ImportAssets(IEnumerable<string> paths)
        {
            // When using the cache server we have to write all import settings to disk first.
            // Then perform the import (Otherwise the cache server will not be used for the import)
            foreach (var path in paths)
            {
                AssetDatabase.WriteImportSettingsIfDirty(path);
            }
            AssetDatabase.StartAssetEditing();
            foreach (string path in paths)
                AssetDatabase.ImportAsset(path);
            AssetDatabase.StopAssetEditing();
        }

        protected void RevertButton()
        {
            if (GUILayout.Button(Styles.revertButton))
            {
                GUI.FocusControl(null);
                ResetHash();
                ResetValues();
                if (HasModified())
                    Debug.LogError("Importer reports modified values after reset.");
            }
        }

        protected bool ApplyButton()
        {
            if (GUILayout.Button(Styles.applyButton))
            {
                GUI.FocusControl(null);
                ApplyAndImport();
                return true;
            }
            return false;
        }

        protected virtual bool OnApplyRevertGUI()
        {
            using (new EditorGUI.DisabledScope(!HasModified()))
            {
                RevertButton();
                return ApplyButton();
            }
        }

        protected void ApplyRevertGUI()
        {
            m_ApplyRevertGUICalled = true;

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

            if (!needsApplyRevert)
            {
                if (extraDataSerializedObject != null && HasModified())
                    Apply(); // user may have extra data that needs to be applied back to the target.
                return;
            }

            // we cannot do this in OnEnable because it is sometimes not setup properly right after an assembly reload...
            if (m_Inspector == null)
                m_Inspector = InspectorWindow.GetInspectors().Find(i => i.tracker.activeEditors.Contains(this));

            if (m_Inspector != null)
                m_HasInspectorBeenSeenLocked = m_Inspector.isLocked;

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                var applied = OnApplyRevertGUI();

                // If the .meta file was modified on disk, reload UI
                var updatedAssets = AssetWasUpdated();
                if (updatedAssets.Any() && Event.current.type != EventType.Layout)
                {
                    ReloadTargets(updatedAssets);
                    applied = false;
                }

                // asset has changed...
                // need to start rendering again.
                if (applied)
                    Repaint();
            }
        }

        void ReloadTargets(IEnumerable<int> targetIndices)
        {
            if (targetIndices.Any())
            {
                if (preview != null)
                {
                    ReloadPreviewInstances();
                    Repaint();
                }

                foreach (var index in targetIndices)
                {
                    ResetHash(index);
                    ReloadAssetData(index);
                }
                m_TargetsReloaded = true;
            }
        }
    }

    internal class AssetImporterEditorPostProcessAsset : AssetPostprocessor
    {
        public static event Action<string, string> OnAssetbundleNameChanged;

        void OnPostprocessAssetbundleNameChanged(string assetPath, string oldName, string newName)
        {
            OnAssetbundleNameChanged?.Invoke(assetPath, newName);
        }
    }
}
