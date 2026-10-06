// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// Settings provider whose settings live in a standalone on-disk asset that the profile references
    /// (via a <see cref="ReferencedComponent"/> proxy) instead of an embedded sub-asset.
    /// </summary>
    class ReferenceComponentSettingsProvider<T> : IBuildProfileSettingsProvider where T : ScriptableObject
    {
        const float k_InspectorIndent = 15f;

        BuildProfileSettingsProvider m_SettingsObjectInfo;

        public ReferenceComponentSettingsProvider(BuildProfileSettingsProvider settingsInfo)
        {
            m_SettingsObjectInfo = settingsInfo;
        }

        public string GetDisplayName() => m_SettingsObjectInfo.displayName;

        public int GetDisplayOrder() => m_SettingsObjectInfo.displayOrder;

        public string GetTooltip() => m_SettingsObjectInfo.tooltip;

        public bool GetIsRequired() => false;

        public bool CanAddSettings(BuildProfile profile) => m_SettingsObjectInfo.canAddSetting?.Invoke(profile) ?? false;

        public bool HasSettings(BuildProfile profile) => BuildProfileModuleUtil.HasComponent<T>(profile);

        public void OnAdd(BuildProfile profile)
        {
            var asset = m_SettingsObjectInfo.onAddSetting?.Invoke(profile);
            if (asset != null && asset is not T)
            {
                throw new InvalidOperationException(
                    $"The factory for the build profile setting {typeof(T).Name} returned a {asset.GetType().Name}.");
            }

            BuildProfileModuleUtil.AddReferenceComponent(profile, typeof(T), asset);
            EditorUtility.SetDirty(profile);
        }

        public void OnRemove(BuildProfile profile)
        {
            profile.RemoveComponent<T>();
        }

        public Action<BuildProfile> GetResetAction() => null;

        public VisualElement CreateInspectorGUI(BuildProfile profile, SerializedObject serializedObject)
        {
            var container = new VisualElement();
            var current = profile.GetComponent<T>();

            var objectField = new ObjectField(TrText.referenceComponentSource)
            {
                objectType = typeof(T),
                allowSceneObjects = false,
                value = current
            };

            var inlineInspector = new VisualElement();
            objectField.RegisterValueChangedCallback(evt =>
            {
                // The picker only offers assets of type T. Reassign the proxy's source in place; a null
                // value leaves the source unassigned but keeps the setting in the profile.
                var newReference = evt.newValue as T;
                BuildProfileModuleUtil.SetReferenceComponent(profile, typeof(T), newReference);
                EditorUtility.SetDirty(profile);
                RebuildInlineInspector(inlineInspector, newReference);
            });

            void OnUndoRedo(in UndoRedoInfo info)
            {
                var restored = profile.GetComponent<T>();
                objectField.SetValueWithoutNotify(restored);
                RebuildInlineInspector(inlineInspector, restored);
            }
            container.RegisterCallback<AttachToPanelEvent>(_ => Undo.undoRedoEvent += OnUndoRedo);
            container.RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoEvent -= OnUndoRedo);

            container.Add(objectField);
            container.Add(inlineInspector);
            RebuildInlineInspector(inlineInspector, current);
            return container;
        }

        void RebuildInlineInspector(VisualElement host, T reference)
        {
            host.Clear();
            if (reference == null)
                return;

            var inspector = new InspectorElement(reference);

            // Offset InspectorElement's Inspector-window indent (UITK padding / IMGUI hierarchy mode) so
            // the referenced asset's fields align with the Source label.
            inspector.style.marginLeft = -k_InspectorIndent;

            host.Add(inspector);
        }
    }
}
