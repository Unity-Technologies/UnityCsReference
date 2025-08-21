// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    abstract class PivotSettingDropdown : EditorToolbarDropdown
    {
        protected readonly Dictionary<PivotSettingDefinition, GUIContent> m_DefToGUIContent = new();
        
        public PivotSettingDropdown()
        {
            RefreshAvailableSettings();
            
            RegisterCallback<AttachToPanelEvent>(AttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(DetachedFromPanel);
            
            clicked += OpenContextMenu;
        }
        
        void OnAvailablePivotSettingsChanged()
        {
            RefreshAvailableSettings();
        }

        protected abstract void OpenContextMenu();
        
        // It's assumed that pivotSettingDefs list is presorted by priority + built-in/custom
        protected void OpenContextMenu(List<PivotSettingDefinition> pivotSettingDefs)
        {
            var menu = new GenericMenu();
            for (int i = 0; i < pivotSettingDefs.Count; ++i)
            {
                var settingDef = pivotSettingDefs[i];
                if (!EditorPivotManager.IsPivotSettingAvailable(settingDef))
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
            for (int i = 0; i < pivotSettingDefs.Count; ++i)
            {
                var settingDef = pivotSettingDefs[i];
                if (EditorPivotManager.availablePivotSettings.Contains(settingDef))
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
            EditorPivotManager.availableSettingsChanged += OnAvailablePivotSettingsChanged;
            RefreshAvailableSettings();
        }

        protected virtual void DetachedFromPanel(DetachFromPanelEvent evt)
        {
            EditorPivotManager.availableSettingsChanged -= OnAvailablePivotSettingsChanged;
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
            return PivotManager.GetActivePivotMode().GetType() == pivotSettingDef.type;
        }
        
        protected override GenericMenu.MenuFunction GetMenuItemFunction(PivotSettingDefinition pivotSettingDef)
        {
            return () => PivotManager.SetActivePivotMode(pivotSettingDef.type);
        }
        
        protected override void RefreshAvailableSettings()
        {
            RefreshAvailableSettings(EditorPivotManager.pivotModeDefs);
        }

        protected override void RefreshActiveSettingUI()
        {
            RefreshActiveSettingUI(EditorPivotManager.pivotModeDefs);
        }

        protected override void AttachedToPanel(AttachToPanelEvent evt)
        {
            base.AttachedToPanel(evt);
            PivotManager.activePivotModeChanged += RefreshActiveSettingUI;
        }

        protected override void DetachedFromPanel(DetachFromPanelEvent evt)
        {
            base.DetachedFromPanel(evt);
            PivotManager.activePivotModeChanged -= RefreshActiveSettingUI;
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
            return PivotManager.GetActivePivotRotation().GetType() == pivotSettingDef.type;
        }
        
        protected override GenericMenu.MenuFunction GetMenuItemFunction(PivotSettingDefinition pivotSettingDef)
        {
            return () => PivotManager.SetActivePivotRotation(pivotSettingDef.type);
        }
        
        protected override void RefreshAvailableSettings()
        {
            RefreshAvailableSettings(EditorPivotManager.pivotRotationsDefs);
        }
        
        protected override void RefreshActiveSettingUI()
        {
            RefreshActiveSettingUI(EditorPivotManager.pivotRotationsDefs);
        }
        
        protected override void AttachedToPanel(AttachToPanelEvent evt)
        {
            base.AttachedToPanel(evt);
            PivotManager.activePivotRotationChanged += RefreshActiveSettingUI;
        }

        protected override void DetachedFromPanel(DetachFromPanelEvent evt)
        {
            base.DetachedFromPanel(evt);
            PivotManager.activePivotRotationChanged -= RefreshActiveSettingUI;
        }
    }
}
