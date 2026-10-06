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
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Unity.Collections;
using Unity.Scripting.LifecycleManagement;

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
                        #pragma warning disable UAC2001 // Avoid Linq
                        m_TargetDirtyCount = new int[m_Editor.targets.Length].ToList();
#pragma warning restore UAC2001
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
            public static readonly string localizedTitleString = L10n.Tr("{0} Import Settings", null);

            public static readonly string applyButton = L10n.Tr("Apply", null);
            public static readonly string revertButton = L10n.Tr("Revert", null);
            public static readonly string unappliedSettingSingleAsset = L10n.Tr("Unapplied import settings for \'{0}\'", null);
            public static readonly string unappliedSettingMultipleAssets = L10n.Tr("Unapplied import settings for \'{0}\' files", null);
            public static readonly string unableToAppliedMessage = L10n.Tr("Your changes might contain errors and cannot be applied. \nYou can either \'Revert\' the changes, or hit \'Cancel\' to go back and fix the errors.", null);
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

        List<EntityId> m_TargetsEntityId;
        // Check to make sure Users implemented their Inspector correctly for the Cancel deselection mechanism.
        bool m_ApplyRevertGUICalled;
        // Adding a check on OnEnable to make sure users call the base class, as it used to do nothing.
        bool m_OnEnableCalled;
        bool? m_AssetHasIssues;

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
            var editors = Resources.FindObjectsOfTypeAll(typeof(AssetImporterEditor));

            CheckExtraDataArray();
            var loadedIds = new List<EntityId>(targets.Length);
            for (int i = 0; i < targets.Length; ++i)
            {
                EntityId entityId = targets[i].GetEntityId();
                loadedIds.Add(entityId);
                var extraData = CreateOrReloadInspectorCopy(entityId, this);
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
                        SaveUserData(entityId, m_ExtraDataTargets[i]);
                    }
                }

                // proceed to an editor count check to make sure we have the proper number of instances saved.
                // If it is not the case, then a dispose was not done properly.
                // We are selecting all Editor instances already enabled and ourselves.
                // This is because when coming back from an assembly reload,
                // the Editors already exist but get removed from the cache in their OnDisable, so we don't count them until its their turn to be Enabled back.
                #pragma warning disable UAC2001 // Avoid Linq
                var allEditors = editors.Cast<AssetImporterEditor>().Where(e => e == this || (e.m_OnEnableCalled && e.targets.Contains(targets[i]))).Select(e => e.GetEntityId()).ToArray();
#pragma warning restore UAC2001
                var instances = GetInspectorCopyCount(entityId);
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
                    FixCacheCount(entityId, allEditors);
                }
            }
            m_TargetsEntityId = loadedIds;
        }

        void FixImporterAssetbundleName(string arg1, string arg2)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                var importer = targets[i] as AssetImporter;
                if (importer != null && importer.assetPath == arg1)
                {
                    FixSavedAssetbundleSettings(importer.GetEntityId(), new PropertyModification[]
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
            else
            {
                // Ensure we take up the same amount of height when the Open button is hidden
                GUILayoutUtility.GetRect(10, 10, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight, EditorStyles.miniButton);
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
            finishedDefaultHeaderGUI += DrawAssetHasIssuesNotification;
            AssetImporterEditorPostProcessAsset.OnAssetbundleNameChanged += FixImporterAssetbundleName;

            // A target importer's native object can be destroyed while this managed editor is still
            // alive: the asset was deleted or its .meta guid was rewritten on disk, then a domain
            // reload re-awakes this editor from backup (MonoBehaviour.DidReloadDomain -> OnEnable).
            // Target-dependent initialization below would either dereference a dangling EntityId
            // (native crash in CreateOrReloadInspectorCopy/WriteObjectToVector) or throw
            // (GetAssetPaths().First() on an empty sequence). A destroyed target is a valid, inert
            // state for an editor: subscribe/unsubscribe symmetrically, mark it enabled, and stop.
            if (!AreImporterTargetsValid())
            {
                Debug.Log("AssetImporterEditor: one or more inspected importer targets no longer exist (asset deleted or its GUID changed); skipping inspector setup.");
                m_Postprocessors = new List<PostprocessorInfo>();
                m_TargetsEntityId = new List<EntityId>();
                m_OnEnableCalled = true;
                isInspectorDirty = true;
                return;
            }

            InitializeAvailableImporters();
            InitializeUnsavedChangesCache();
            InitializePostprocessors();

            saveChangesMessage = targets.Length == 1
                #pragma warning disable UAC2010 // Avoid Linq
                ? string.Format(Styles.unappliedSettingSingleAsset, GetAssetPaths().First())
#pragma warning restore UAC2010
                : string.Format(Styles.unappliedSettingMultipleAssets, targets.Length);

            m_OnEnableCalled = true;
            // Forces the inspector as dirty allows us to make sure the OnInspectorGUI has been called
            // at least once in the OnDisable in order to show the ApplyRevertGUI error.
            isInspectorDirty = true;
        }

        // True only when every target importer's native object still exists. A target can be
        // destroyed (asset deleted, or its .meta GUID rewritten on disk) while this managed editor
        // is still alive and later re-enabled by a domain reload. Subclasses must call this before
        // accessing serializedObject or other target-dependent state; otherwise serializedObject
        // throws SerializedObjectNotCreatableException and native code dereferences a stale EntityId.
        protected bool AreImporterTargetsValid()
        {
            foreach (var t in targets)
            {
                var importer = t as AssetImporter;
                if (importer == null) // UnityEngine.Object overloaded ==: true for null or destroyed
                    return false;
            }
            return true;
        }

        public virtual void OnDisable()
        {
            finishedDefaultHeaderGUI -= DrawAssetHasIssuesNotification;
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

            foreach (var t in m_TargetsEntityId)
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

        void DrawAssetHasIssuesNotification(Editor editor)
        {
            if (editor != this)
                return;

            AssetImporterEditor assetImporterEditor = (AssetImporterEditor) editor;
            if (!assetImporterEditor.AssetHasIssues())
                return;

            if (assetImporterEditor.targets == null || assetImporterEditor.targets.Length == 0)
                return;

            int nbErrors = 0, nbWarnings = 0;
            var guids = new List<GUID>();
            #pragma warning disable UAC2001 // Avoid Linq
            foreach (var importer in assetImporterEditor.targets.OfType<AssetImporter>())
#pragma warning restore UAC2001
            {
                var guid = AssetDatabase.GUIDFromAssetPath(importer.assetPath);
                AssetImporter.GetImportLogEntriesCount(guid, out int nbE, out int nbW);

                if (nbE > 0 || nbW > 0)
                    guids.Add(guid);

                nbErrors += nbE;
                nbWarnings += nbW;
            }

            if (nbErrors + nbWarnings <= 0)
                return;

            string text = assetImporterEditor.targets.Length == 1 ? "Last import generated " : $"{assetImporterEditor.targets.Length} selected assets with ";
            if (nbErrors > 0 && nbWarnings > 0)
                text += $"{nbErrors} errors / {nbWarnings} warnings.";
            else if (nbErrors > 0)
                text += $"{nbErrors} errors";
            else
                text += $"{nbWarnings} warnings";

            var btnText = $"Print to console";
            Action onBtnClick = () =>
            {
                foreach (var guid in guids)
                {
                    ImportLog importLog = AssetImporter.GetImportLog(guid);
                    if (importLog != null)
                        importLog.PrintToConsole();
                }
            };
            Texture image = EditorGUIUtility.GetHelpIcon(nbErrors > 0 ? MessageType.Error : MessageType.Warning);
            DrawNotification(image, text, btnText, onBtnClick);
        }

        internal bool AssetHasIssues()
        {
            if (!m_AssetHasIssues.HasValue)
            {
                m_AssetHasIssues = targets != null && targets.Length > 0 && Array.Exists(targets, t =>
                    t is AssetImporter importer
                    && AssetImporter.GetImportLogEntriesCount(AssetDatabase.GUIDFromAssetPath(importer.assetPath), out int nbErrors, out int nbWarnings)
                    && (nbErrors > 0 || nbWarnings > 0));
            }

            return m_AssetHasIssues.Value;
        }

        IEnumerable<string> GetAssetPaths()
        {
            #pragma warning disable UAC2001 // Avoid Linq
            return targets.OfType<AssetImporter>().Select(i => i.assetPath);
#pragma warning restore UAC2001
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
            // Custom importer editors that don't have apply/revert buttons are handling
            // their files in a particular way and should not reimport their assets when the inspector is closed.
            if (needsApplyRevert)
                ImportAssets(GetAssetPaths());
            // Re-import of assets may change settings dur AssetPostprocessors
            // We have to make sure we update the saved data after the import is done
            // so users see the real state of the importer once the import is finished.
            for (int i = 0; i < targets.Length; i++)
            {
                UpdateSavedData(targets[i]);
            }
        }

        [Obsolete("Please use SaveChanges.")]
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

        [Obsolete("Please use DiscardChanges.")]
        protected virtual void ResetValues()
        {
            DiscardChanges();
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
            {
                if(File.Exists(path))
                    AssetDatabase.ImportAsset(path);
            }
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

        public class ApplyRevertButtonContainer : VisualElement
        {
            public ApplyRevertButtonContainer(AssetImporterEditor editor)
            {
                style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.RowReverse);
                var applyButton = new UnityEngine.UIElements.Button();
                applyButton.text = "Apply";
                applyButton.style.maxWidth = 50;
                applyButton.name = "applyButton";
                applyButton.clicked += editor.SaveChanges;

                var revertButton = new UnityEngine.UIElements.Button();
                revertButton.text = "Revert";
                revertButton.style.maxWidth = 50;
                revertButton.name = "revertButton";
                revertButton.clicked += () =>
                {
                    #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
                    editor.DiscardChanges();
                    #pragma warning restore UAL0015
                    if (editor.HasModified())
                    {
                        Debug.LogError("Importer reports modified values after reset.");
                    }
                };

                Add(applyButton);
                Add(revertButton);
                enabledSelf = editor.hasUnsavedChanges;

                BindingExtensions.TrackSerializedObjectValue(this, editor.serializedObject,
                    (e) => {
                        enabledSelf = editor.HasModified();
                    }
                );
                RegisterCallback<AttachToPanelEvent>(
                    (e) =>
                    {
                        editor.m_ApplyRevertGUICalled = true;
                    }
                );
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

            // On the Blocks tab the pipeline view already lists AssetPostprocessors (Early/Late boxes around the
            // blocks), so the standalone foldout is suppressed there to avoid showing them twice. With blocks
            // disabled there is no pipeline view, so the foldout always draws regardless of the active tab.
            if (!UnityEditor.Experimental.AssetImporters.ImportBlocks.ImportBlocksToggle.IsEnabled || showAssetPostprocessorsFoldout)
                DrawAssetPostprocessors();
        }
    }

    [MovedFrom("UnityEditor.Experimental.AssetImporters")]
    internal partial class AssetImporterEditorPostProcessAsset : AssetPostprocessor
    {
        [AutoStaticsCleanupOnCodeReload]
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
            // GetPostprocessOrder() of the postprocessor, used to split it into the Early or Late phase
            // (relative to Import Blocks) in the pipeline inspector.
            public int Priority;
            // The postprocessor type, used by the pipeline inspector to resolve its MonoScript.
            public Type PostprocessorType;
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
            #pragma warning disable UAC2001 // Avoid Linq
            allAssetImportProcessors.UnionWith(AssetImporter.GetStaticPostprocessors(target.GetType()).Where(t => t.Type.Assembly != typeof(AssetImporter).Assembly));
#pragma warning restore UAC2001

            m_Postprocessors = new List<PostprocessorInfo>();
            foreach (var processor in allAssetImportProcessors)
            {
                m_Postprocessors.Add(new PostprocessorInfo()
                {
                    Expanded = false,
                    Methods = processor.Methods,
                    Name = processor.Type.FullName,
                    Priority = processor.Priority,
                    PostprocessorType = processor.Type,
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

            Rect clipRect = rect;
            rect.y += 3;
            rect.yMax = rect.yMin + EditorGUIUtility.singleLineHeight;
            if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                GenericMenu pm = new GenericMenu();
                pm.AddItem(new GUIContent("Copy"), false, RightClickPostprocessor, processor.Name);
                pm.ShowAsContext();
            }

            GUI.BeginClip(clipRect);
            Rect localRect = new Rect(0, 3, clipRect.width, EditorGUIUtility.singleLineHeight);

            processor.Expanded = EditorGUI.Foldout(localRect, processor.Expanded, GUIContent.Temp(processor.Name, processor.Name), true);
            if (processor.Expanded)
            {
                EditorGUI.indentLevel++;
                foreach (var method in processor.Methods)
                {
                    localRect.y += EditorGUIUtility.singleLineHeight;
                    EditorGUI.LabelField(localRect, GUIContent.Temp(method, method));
                }
                EditorGUI.indentLevel--;
            }

            GUI.EndClip();

            m_Postprocessors[index] = processor;

            EditorGUI.indentLevel--;
        }

        static void RightClickPostprocessor(object userdata)
        {
            EditorGUIUtility.systemCopyBuffer = (string)userdata;
        }

        // Whether to draw the standalone "Asset PostProcessors" foldout. Importer editors that surface the
        // postprocessors inline in the Blocks-tab pipeline view override this to hide it on that tab.
        private protected virtual bool showAssetPostprocessorsFoldout => true;

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

        SavedInt m_CombinedViewMode;
        SavedBool m_ShowPostprocessorsInBlockList;
        VisualElement m_EarlyContainer;
        VisualElement m_LateContainer;

        VisualElement CreateImportBlocksSection()
        {
            var blocksProp = serializedObject.FindProperty("m_BlockCollection");
            if (blocksProp == null)
                return null;

            var container = new VisualElement();
            container.style.marginTop = 8;
            container.AddToClassList(UnityEditor.Experimental.AssetImporters.ImportBlocks.UiHelpers.k_ExpandFoldoutClassName);

            var blocksField = new UnityEditor.UIElements.PropertyField(blocksProp);
            blocksField.BindProperty(blocksProp);
            container.Add(blocksField);

            return container;
        }

        internal VisualElement CreateCombinedImportCustomizationSection()
        {
            var blocksUI = CreateImportBlocksSection();
            if (blocksUI == null)
                return null;

            if (m_CombinedViewMode == null)
                m_CombinedViewMode = new SavedInt("AssetImporterEditor_CombinedViewMode", 0);
            if (m_ShowPostprocessorsInBlockList == null)
                m_ShowPostprocessorsInBlockList = new SavedBool("AssetImporterEditor_ShowPostprocessorsInBlockList", true);

            var container = new VisualElement();
            container.AddToClassList("pipeline-root");
            var blockItemStyleSheet = UnityEditor.Experimental.AssetImporters.ImportBlocks.UiHelpers.GetBlockItemStyleSheet();
            if (blockItemStyleSheet != null)
                container.styleSheets.Add(blockItemStyleSheet);
            var pipelineStyleSheet = UnityEditor.Experimental.AssetImporters.ImportBlocks.UiHelpers.GetPipelineInspectorStyleSheet();
            if (pipelineStyleSheet != null)
                container.styleSheets.Add(pipelineStyleSheet);

            var pipelineView = new VisualElement();
            var callbacksView = new VisualElement();

            int initialMode = m_CombinedViewMode.value;

            var toolbar = new VisualElement();
            toolbar.AddToClassList("pipeline-toolbar");

            var modeGroup = new ToggleButtonGroup
            {
                isMultipleSelection = false,
                allowEmptySelection = false
            };
            modeGroup.Add(new UnityEngine.UIElements.Button { text = "Pipeline", tooltip = "Import blocks and AssetPostprocessors assigned to this asset." });
            modeGroup.Add(new UnityEngine.UIElements.Button { text = "Execution", tooltip = "The actual invocation order for each callback: Early AssetPostprocessors, then blocks, then Late AssetPostprocessors." });

            modeGroup.SetValueWithoutNotify(new ToggleButtonGroupState(1UL << initialMode, 2));
            toolbar.Add(modeGroup);

            var spacer = new VisualElement();
            spacer.AddToClassList("pipeline-toolbar-spacer");
            toolbar.Add(spacer);

            var showAppToggle = new Toggle("Show asset postprocessors")
            {
                value = m_ShowPostprocessorsInBlockList.value,
                tooltip = "Toggle the visibility of asset postprocessors in the blocks list."
            };
            toolbar.Add(showAppToggle);

            container.Add(toolbar);

            // Rebuilt on every entry: the pipeline view edits the collection, so the entries (and a
            // circular-reference error) go stale between visits.
            void RebuildExecutionView()
            {
                callbacksView.Clear();
                callbacksView.Add(CreateExecutionView());
            }

            void ApplyMode(int mode)
            {
                if (mode == 1)
                    RebuildExecutionView();
                pipelineView.style.display = mode == 0 ? DisplayStyle.Flex : DisplayStyle.None;
                callbacksView.style.display = mode == 1 ? DisplayStyle.Flex : DisplayStyle.None;
                showAppToggle.style.display = mode == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Index 0 is "Pipeline"; single-selection with no empty state means exactly one option is set.
            modeGroup.RegisterValueChangedCallback(evt =>
            {
                int mode = evt.newValue[0] ? 0 : 1;
                m_CombinedViewMode.value = mode;
                ApplyMode(mode);
            });

            showAppToggle.RegisterValueChangedCallback(evt =>
            {
                m_ShowPostprocessorsInBlockList.value = evt.newValue;
                UpdatePostprocessorContainersVisibility();
            });

            InjectPostprocessorsIntoBlocksUI(blocksUI);
            pipelineView.Add(blocksUI);
            container.Add(pipelineView);

            container.Add(callbacksView);

            ApplyMode(initialMode);

            return container;
        }

        void UpdatePostprocessorContainersVisibility()
        {
            bool show = m_ShowPostprocessorsInBlockList != null && m_ShowPostprocessorsInBlockList.value;
            if (m_EarlyContainer != null)
                m_EarlyContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_LateContainer != null)
                m_LateContainer.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void InjectPostprocessorsIntoBlocksUI(VisualElement blocksUI)
        {
            int threshold = AssetImportCallbackDispatch.k_PostprocessOrderThreshold;
            var scriptIcon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;

            var earlyPostprocessors = new List<PostprocessorInfo>();
            var latePostprocessors = new List<PostprocessorInfo>();

            foreach (var p in m_Postprocessors)
            {
                if (p.Priority < threshold)
                    earlyPostprocessors.Add(p);
                else
                    latePostprocessors.Add(p);
            }

            blocksUI.RegisterCallback<GeometryChangedEvent>(OnLayout);

            void OnLayout(GeometryChangedEvent _)
            {
                var blockListView = blocksUI.Q<ListView>();
                if (blockListView == null)
                    return;

                blocksUI.UnregisterCallback<GeometryChangedEvent>(OnLayout);

                var addButton = blocksUI.Q<UnityEngine.UIElements.Button>(className: UnityEditor.Experimental.AssetImporters.ImportBlocks.UiHelpers.k_AddButtonClassName);
                // The missing-types warning is a sibling of the ListView; preserve it across the Clear() below so
                // it is re-parented into the blocks section rather than dropped (which would silently hide the
                // warning about broken serialized block data on the Blocks tab).
                var missingTypesHelpBox = blocksUI.Q<UnityEngine.UIElements.HelpBox>(UnityEditor.Experimental.AssetImporters.ImportBlocks.UiHelpers.k_MissingTypesHelpBoxName);

                var wrapper = blocksUI.Q(className: UnityEditor.Experimental.AssetImporters.ImportBlocks.UiHelpers.k_ExpandFoldoutClassName) ?? blocksUI;
                wrapper.Clear();

                if (earlyPostprocessors.Count > 0)
                {
                    m_EarlyContainer = CreateSectionContainer();
                    var earlyFoldoutState = new SavedBool("AssetImporterEditor_EarlyAPPFoldout", true);
                    m_EarlyContainer.Add(CreatePostprocessorListView("Asset postprocessors (early)", earlyFoldoutState, earlyPostprocessors, scriptIcon));
                    m_EarlyContainer.style.display = m_ShowPostprocessorsInBlockList ? DisplayStyle.Flex : DisplayStyle.None;
                    wrapper.Add(m_EarlyContainer);
                }

                var blocksContainer = CreateSectionContainer();
                if (missingTypesHelpBox != null)
                    blocksContainer.Add(missingTypesHelpBox);
                blocksContainer.Add(blockListView);
                if (addButton != null)
                {
                    addButton.style.display = DisplayStyle.Flex;
                    addButton.style.marginTop = 4;
                    addButton.style.marginBottom = 2;
                    blocksContainer.Add(addButton);
                }
                wrapper.Add(blocksContainer);

                if (latePostprocessors.Count > 0)
                {
                    m_LateContainer = CreateSectionContainer();
                    var lateFoldoutState = new SavedBool("AssetImporterEditor_LateAPPFoldout", true);
                    m_LateContainer.Add(CreatePostprocessorListView("Asset postprocessors (late)", lateFoldoutState, latePostprocessors, scriptIcon));
                    m_LateContainer.style.display = m_ShowPostprocessorsInBlockList ? DisplayStyle.Flex : DisplayStyle.None;
                    wrapper.Add(m_LateContainer);
                }
            }
        }

        static ListView CreatePostprocessorListView(string title, SavedBool foldoutState, List<PostprocessorInfo> postprocessors, Texture2D scriptIcon)
        {
            var listView = new ListView();
            listView.itemsSource = postprocessors;
            listView.makeItem = () => new VisualElement();
            listView.bindItem = (element, index) =>
            {
                element.Clear();
                var p = postprocessors[index];
                element.Add(CreatePostprocessorElement(p.Name, p.Priority, scriptIcon, p.PostprocessorType, p.Methods));
            };
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.showFoldoutHeader = true;
            listView.showBoundCollectionSize = false;
            listView.headerTitle = title;
            listView.reorderable = false;
            listView.showAddRemoveFooter = false;
            listView.selectionType = SelectionType.None;
            listView.AddToClassList("pipeline-postprocessor-list");

            listView.RegisterCallback<GeometryChangedEvent>(OnFirstLayout);
            void OnFirstLayout(GeometryChangedEvent _)
            {
                var foldout = listView.Q<Foldout>();
                if (foldout == null)
                    return;
                listView.UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);

                foldout.value = foldoutState.value;
                foldout.RegisterValueChangedCallback(evt =>
                {
                    if (evt.target == foldout)
                        foldoutState.value = evt.newValue;
                });
            }

            return listView;
        }

        static VisualElement CreateSectionContainer()
        {
            var container = new VisualElement();
            container.AddToClassList("block-section-container");
            return container;
        }

        static VisualElement CreatePostprocessorElement(string fullName, int order, Texture2D scriptIcon, Type postprocessorType, string[] methods)
        {
            var shortName = fullName;
            int lastDot = fullName.LastIndexOf('.');
            if (lastDot >= 0)
                shortName = fullName.Substring(lastDot + 1);

            var template = UnityEditor.Experimental.AssetImporters.ImportBlocks.UiHelpers.GetPostprocessorElementTemplate();
            if (template == null)
                return new Label(shortName);

            var item = template.Instantiate();

            var icon = item.Q("script-icon");
            if (icon != null && scriptIcon != null)
                icon.style.backgroundImage = new StyleBackground(scriptIcon);

            var nameLabel = item.Q<Label>("name-label");
            if (nameLabel != null)
                nameLabel.text = shortName;

            var contentContainer = item.Q("content-container");
            if (contentContainer != null)
                contentContainer.style.display = DisplayStyle.None;

            var script = postprocessorType != null ? UnityEditor.Experimental.AssetImporters.ImportBlocks.UiHelpers.FindScriptForType(postprocessorType) : null;
            var scriptField = item.Q<UnityEditor.UIElements.ObjectField>("script-field");
            if (scriptField != null)
            {
                scriptField.objectType = typeof(MonoScript);
                scriptField.value = script;
                scriptField.SetEnabled(false);
            }

            var foldoutToggle = item.Q<Toggle>("foldout-toggle");
            if (foldoutToggle != null)
            {
                foldoutToggle.value = false;
                foldoutToggle.RegisterValueChangedCallback(evt =>
                {
                    if (contentContainer != null)
                        contentContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                });
            }

            var tooltipBuilder = new System.Text.StringBuilder();
            tooltipBuilder.AppendLine($"<b>{shortName}</b>");
            tooltipBuilder.AppendLine();
            tooltipBuilder.Append("<b>Full name:</b>");
            tooltipBuilder.AppendLine($"  {fullName}");
            tooltipBuilder.AppendLine();
            tooltipBuilder.Append("<b>Postprocess order:</b>");
            tooltipBuilder.AppendLine($"  {order}");
            if (methods != null && methods.Length > 0)
            {
                tooltipBuilder.AppendLine();
                tooltipBuilder.AppendLine("<b>Callbacks:</b>");
                foreach (var method in methods)
                    tooltipBuilder.AppendLine($"  - {method}");
            }

            item.tooltip = tooltipBuilder.ToString().TrimEnd();
            return item;
        }

        struct ExecutionEntry
        {
            public string Name;
            public string Type;
            public int Order;
            public bool IsHeader;
        }

        VisualElement CreateExecutionView()
        {
            // The schedule is built from a single importer, unlike the bound block UI next to it.
            if (targets.Length > 1)
                return new UnityEngine.UIElements.HelpBox(
                    "The execution order is not available when multiple assets are selected. " +
                    "Select a single asset to see its execution order.",
                    UnityEngine.UIElements.HelpBoxMessageType.Info);

            var importer = target as AssetImporter;
            if (importer == null)
                return new VisualElement();

            var callbacks = ImportPipelineOrderUtility.GetCallbacksForImporter(importer);
            if (callbacks == null)
                return new VisualElement();

            List<UnityEditor.Experimental.AssetImporters.ImportBlocks.IBlock> blocks;
            try
            {
                blocks = ImportPipelineOrderUtility.CollectBlocks(importer);
            }
            catch (InvalidOperationException e)
            {
                Debug.LogException(e);
                return new UnityEngine.UIElements.HelpBox(
                    "Cannot show the execution order because the block graph contains a circular reference. " +
                    "Switch to the Pipeline view to locate and remove the offending block reference. " +
                    "See the Console for details.",
                    UnityEngine.UIElements.HelpBoxMessageType.Error);
            }
            int threshold = AssetImportCallbackDispatch.k_PostprocessOrderThreshold;

            var entries = new List<ExecutionEntry>();

            foreach (var callbackName in callbacks)
            {
                var earlyBuf = new List<PostprocessorInfo>();
                var lateBuf = new List<PostprocessorInfo>();
                var blockBuf = new List<UnityEditor.Experimental.AssetImporters.ImportBlocks.IBlock>();

                foreach (var p in m_Postprocessors)
                {
                    if (Array.IndexOf(p.Methods, callbackName) < 0)
                        continue;
                    if (p.Priority < threshold)
                        earlyBuf.Add(p);
                    else
                        lateBuf.Add(p);
                }

                Type blockInterface = ImportPipelineOrderUtility.GetBlockInterfaceForCallback(callbackName);
                if (blockInterface != null)
                {
                    foreach (var b in blocks)
                    {
                        if (blockInterface.IsAssignableFrom(b.GetType()))
                            blockBuf.Add(b);
                    }
                }

                if (earlyBuf.Count == 0 && blockBuf.Count == 0 && lateBuf.Count == 0)
                    continue;

                entries.Add(new ExecutionEntry { Name = callbackName, IsHeader = true });

                foreach (var p in earlyBuf)
                    entries.Add(new ExecutionEntry { Name = p.Name, Type = "AssetPostprocessor", Order = p.Priority });
                foreach (var b in blockBuf)
                    entries.Add(new ExecutionEntry { Name = b.Name, Type = "Block", Order = 0 });
                foreach (var p in lateBuf)
                    entries.Add(new ExecutionEntry { Name = p.Name, Type = "AssetPostprocessor", Order = p.Priority });
            }

            var scriptIcon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;
            var blockIcon = UnityEditor.Experimental.AssetImporters.ImportBlocks.UiHelpers.GetBlockIcon();

            var viewTemplate = UnityEditor.Experimental.AssetImporters.ImportBlocks.UiHelpers.GetExecutionViewTemplate();
            var rowTemplate = UnityEditor.Experimental.AssetImporters.ImportBlocks.UiHelpers.GetExecutionRowTemplate();
            if (viewTemplate == null || rowTemplate == null)
                return new VisualElement();

            var listView = viewTemplate.Instantiate().Q<MultiColumnListView>("execution-list");
            listView.itemsSource = entries;

            // The row is what the cell styles select on, so hand it out without the template container.
            listView.columns["name"].makeCell = () => rowTemplate.Instantiate().Q<VisualElement>("execution-row");
            listView.columns["name"].bindCell = (element, index) =>
            {
                var entry = entries[index];
                var icon = element.Q("entry-icon");
                var label = element.Q<Label>("entry-label");

                var shortName = entry.Name;
                int lastDot = shortName.LastIndexOf('.');
                if (lastDot >= 0)
                    shortName = shortName.Substring(lastDot + 1);

                label.text = shortName;
                element.EnableInClassList("pipeline-execution-row--header", entry.IsHeader);

                if (!entry.IsHeader)
                {
                    var tex = entry.Type == "Block" ? blockIcon : scriptIcon;
                    icon.style.backgroundImage = tex != null ? new StyleBackground(tex) : new StyleBackground();
                }
            };

            listView.columns["order"].makeCell = () =>
            {
                var label = new Label();
                label.AddToClassList("pipeline-execution-order");
                return label;
            };
            listView.columns["order"].bindCell = (element, index) =>
            {
                var label = (Label)element;
                var entry = entries[index];
                label.text = entry.IsHeader ? "" : entry.Order.ToString();
            };

            return listView;
        }
    }

    // Part of the class handling the ImporterSelection
    public abstract partial class AssetImporterEditor
    {
        static partial class Styles
        {
            public static readonly GUIContent ImporterSelection = L10n.TextContent("Importer", null, null, null);
            public static readonly string defaultImporterName = L10n.Tr("{0} (Default)", null);
        }

        // Support for importer overrides
        List<Type> m_AvailableImporterTypes;
        const int k_MultipleSelectedImporterTypes = -1;
        int m_SelectedImporterType = k_MultipleSelectedImporterTypes;
        string[] m_AvailableImporterTypesOptions = Array.Empty<string>();

        private void DrawImporterSelectionPopup()
        {
            if (m_AvailableImporterTypes.Count < 2)
                return;
            var mixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = m_SelectedImporterType == k_MultipleSelectedImporterTypes;
            GUILayout.Label(Styles.ImporterSelection);
            var newSelection = EditorGUILayout.Popup(m_SelectedImporterType, m_AvailableImporterTypesOptions, GUILayout.MaxWidth(350));
            if (newSelection != m_SelectedImporterType)
            {
                // cancel any pending changes if we are switching the importer.
                // It's going to do a full import with new settings anyway.
                if (hasUnsavedChanges)
                {
                    DiscardChanges();
                }

                AssetDatabase.StartAssetEditing();
                foreach (AssetImporter importer in targets)
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

            #pragma warning disable UAC2001 // Avoid Linq
            var targetsPaths = targets.OfType<AssetImporter>().Select(t => t.assetPath);
#pragma warning restore UAC2001

            #pragma warning disable UAC2001 // Avoid Linq
            var typeLists = targetsPaths.Select(AssetDatabase.GetAvailableImporters).ToList();
#pragma warning restore UAC2001
            #pragma warning disable UAC2001 // Avoid Linq
            m_AvailableImporterTypes.AddRange(typeLists.Aggregate(
#pragma warning restore UAC2001
                new HashSet<Type>(typeLists[0]),
                (h, e) =>
                {
                    h.IntersectWith(e);
                    return h;
                }));

#pragma warning disable UAC2001, UAC2010 // Avoid Linq
            var defaultImporter = targetsPaths.Select(AssetDatabase.GetDefaultImporter).First();
            m_AvailableImporterTypesOptions = m_AvailableImporterTypes.Select(a => a == defaultImporter ? string.Format(Styles.defaultImporterName, defaultImporter.FullName) : a.FullName).ToArray();
#pragma warning restore UAC2001, UAC2010


            if (m_AvailableImporterTypes.Count > 0)
            {
                #pragma warning disable UAC2001 // Avoid Linq
                var selection = targets
#pragma warning restore UAC2001
                    .Select(t => t.GetType())
                    .Select(t => m_AvailableImporterTypes.IndexOf(t))
                    .Distinct();
                #pragma warning disable UAC2005 // Avoid Linq
                if (selection.Count() > 1)
#pragma warning restore UAC2005
                {
                    m_SelectedImporterType = k_MultipleSelectedImporterTypes;
                }
                else
                {
                    #pragma warning disable UAC2010 // Avoid Linq
                    m_SelectedImporterType = selection.First();
#pragma warning restore UAC2010
                }
            }

        }
    }

}
