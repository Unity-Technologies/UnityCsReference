// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    abstract class PivotSettingDropdown : EditorToolbarDropdown, IAccessContainerWindow
    {
        protected readonly Dictionary<PivotSettingDefinition, GUIContent> m_DefToGUIContent = new();

        protected Type ownerType => containerWindow?.GetType() ?? typeof(SceneView);

        public EditorWindow containerWindow { get; set; }

        public PivotSettingDropdown()
        {
            RegisterCallback<AttachToPanelEvent>(AttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(DetachedFromPanel);
            clicked += OpenContextMenu;
        }

        void OnAvailablePivotSettingsChanged(Type changedOwnerType)
        {
            if (changedOwnerType == ownerType)
                RefreshAvailableSettings();
        }

        protected abstract void OpenContextMenu();

        // It's assumed that pivotSettingDefs list is presorted by priority + built-in/custom
        protected void OpenContextMenu(List<PivotSettingDefinition> pivotSettingDefs)
        {
            var menu = new GenericMenu();
            var availableSettings = EditorPivotManager.GetAvailablePivotSettings(ownerType);

            for (int i = 0; i < pivotSettingDefs.Count; ++i)
            {
                var settingDef = pivotSettingDefs[i];
                if (availableSettings.IndexOf(settingDef) == -1)
                    continue;

                var settingGUIContent = m_DefToGUIContent[settingDef];
                menu.AddItem(settingGUIContent, IsSettingActivated(settingDef), GetMenuItemFunction(settingDef));

                if (EditorPivotManager.IsBuiltInPivotSetting(settingDef.type))
                {
                    var nextIdx = i + 1;
                    if (nextIdx != pivotSettingDefs.Count &&
                        !EditorPivotManager.IsBuiltInPivotSetting(pivotSettingDefs[nextIdx].type))
                    {
                        menu.AddSeparator(string.Empty);
                    }
                }
            }
            menu.DropDown(worldBound);
        }

        protected abstract bool IsSettingActivated(PivotSettingDefinition pivotSettingDef);

        protected abstract GenericMenu.MenuFunction GetMenuItemFunction(PivotSettingDefinition pivotSettingDef);

        protected abstract GUIContent GetGUIContentForPivotSetting(PivotSettingDefinition pivotSettingDef);

        protected abstract void RefreshAvailableSettings();

        protected void RefreshAvailableSettings(List<PivotSettingDefinition> pivotSettingDefs)
        {
            m_DefToGUIContent.Clear();
            var availableSettings = EditorPivotManager.GetAvailablePivotSettings(ownerType);

            for (int i = 0; i < pivotSettingDefs.Count; ++i)
            {
                var settingDef = pivotSettingDefs[i];
                if (availableSettings.Contains(settingDef))
                {
                    var guiContent = GetGUIContentForPivotSetting(settingDef);
                    m_DefToGUIContent.Add(settingDef, guiContent);
                }
            }

            RefreshActiveSettingUI(pivotSettingDefs);
        }

        protected abstract void RefreshActiveSettingUI();
        protected void RefreshActiveSettingUI(List<PivotSettingDefinition> pivotSettingDefs)
        {
            for (int i = 0; i < pivotSettingDefs.Count; ++i)
            {
                if (IsSettingActivated(pivotSettingDefs[i]) &&
                    m_DefToGUIContent.TryGetValue(pivotSettingDefs[i], out var content))
                {
                    text = content.text;
                    tooltip = content.tooltip;
                    icon = content.image as Texture2D;
                    break;
                }
            }
        }

        protected virtual void AttachedToPanel(AttachToPanelEvent evt)
        {
            EditorPivotManager.availableSettingsChangedForType += OnAvailablePivotSettingsChanged;
            RefreshAvailableSettings();
        }

        protected virtual void DetachedFromPanel(DetachFromPanelEvent evt)
        {
            EditorPivotManager.availableSettingsChangedForType -= OnAvailablePivotSettingsChanged;
        }
    }

    [EditorToolbarElement("Tool Settings/Pivot Mode")]
    sealed class PivotModeDropdown : PivotSettingDropdown
    {
        public PivotModeDropdown()
        {
            name = "Pivot Mode";
        }

        protected override GUIContent GetGUIContentForPivotSetting(PivotSettingDefinition pivotSettingDef)
        {
            var guiContent = EditorGUIUtility.TrTextContentWithIcon(pivotSettingDef.attribute.displayName,
                $"Toggle Tool Handle Position\n\n{pivotSettingDef.attribute.tooltip}",
                pivotSettingDef.icon);

            return guiContent;
        }

        protected override void OpenContextMenu()
        {
            OpenContextMenu(EditorPivotManager.pivotModeDefs);
        }

        protected override bool IsSettingActivated(PivotSettingDefinition pivotSettingDef)
        {
            return PivotManager.GetActivePivotMode(ownerType).GetType() == pivotSettingDef.type;
        }

        protected override GenericMenu.MenuFunction GetMenuItemFunction(PivotSettingDefinition pivotSettingDef)
        {
            return () => PivotManager.SetActivePivotMode(pivotSettingDef.type, ownerType);
        }

        protected override void RefreshAvailableSettings()
        {
            RefreshAvailableSettings(EditorPivotManager.pivotModeDefs);
        }

        protected override void RefreshActiveSettingUI()
        {
            RefreshActiveSettingUI(EditorPivotManager.pivotModeDefs);
        }

        void OnPivotModeChangedForOwner(Type changedOwnerType)
        {
            if (changedOwnerType == ownerType)
                RefreshActiveSettingUI();
        }

        protected override void AttachedToPanel(AttachToPanelEvent evt)
        {
            base.AttachedToPanel(evt);
            PivotManager.activePivotModeChanged += RefreshActiveSettingUI;
            PivotManager.activePivotModeChangedForOwner += OnPivotModeChangedForOwner;
        }

        protected override void DetachedFromPanel(DetachFromPanelEvent evt)
        {
            base.DetachedFromPanel(evt);
            PivotManager.activePivotModeChanged -= RefreshActiveSettingUI;
            PivotManager.activePivotModeChangedForOwner -= OnPivotModeChangedForOwner;
        }
    }

    [EditorToolbarElement("Tool Settings/Pivot Rotation")]
    sealed class PivotRotationDropdown : PivotSettingDropdown
    {
        public PivotRotationDropdown()
        {
            name = "Pivot Rotation";
        }

        protected override GUIContent GetGUIContentForPivotSetting(PivotSettingDefinition pivotSettingDef)
        {
            var guiContent = EditorGUIUtility.TrTextContentWithIcon(pivotSettingDef.attribute.displayName,
                $"Toggle Tool Handle Rotation\n\n{pivotSettingDef.attribute.tooltip}",
                pivotSettingDef.icon);

            return guiContent;
        }

        protected override void OpenContextMenu()
        {
            OpenContextMenu(EditorPivotManager.pivotRotationsDefs);
        }

        protected override bool IsSettingActivated(PivotSettingDefinition pivotSettingDef)
        {
            return PivotManager.GetActivePivotRotation(ownerType).GetType() == pivotSettingDef.type;
        }

        protected override GenericMenu.MenuFunction GetMenuItemFunction(PivotSettingDefinition pivotSettingDef)
        {
            return () => PivotManager.SetActivePivotRotation(pivotSettingDef.type, ownerType);
        }

        protected override void RefreshAvailableSettings()
        {
            RefreshAvailableSettings(EditorPivotManager.pivotRotationsDefs);
        }

        protected override void RefreshActiveSettingUI()
        {
            RefreshActiveSettingUI(EditorPivotManager.pivotRotationsDefs);
        }

        void OnPivotRotationChangedForOwner(Type changedOwnerType)
        {
            if (changedOwnerType == ownerType)
                RefreshActiveSettingUI();
        }

        protected override void AttachedToPanel(AttachToPanelEvent evt)
        {
            base.AttachedToPanel(evt);
            PivotManager.activePivotRotationChanged += RefreshActiveSettingUI;
            PivotManager.activePivotRotationChangedForOwner += OnPivotRotationChangedForOwner;
        }

        protected override void DetachedFromPanel(DetachFromPanelEvent evt)
        {
            base.DetachedFromPanel(evt);
            PivotManager.activePivotRotationChanged -= RefreshActiveSettingUI;
            PivotManager.activePivotRotationChangedForOwner -= OnPivotRotationChangedForOwner;
        }
    }
}
