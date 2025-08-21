// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor.EditorTools;

namespace UnityEditor
{
    struct PivotSettingDefinition
    {
        public Type type { get; }
       
        public CustomPivotAttribute attribute { get; }
        
        public Type targetTool => attribute?.targetTool;
        
        public Type targetToolContext => attribute?.targetToolContext;
        
        Texture m_Icon;

        public Texture icon
        {
            get
            {
                if (m_Icon == null)
                    m_Icon = EditorToolUtility.GetIcon(type)?.image;

                return m_Icon;
            }
        }

        public PivotSettingDefinition(Type type, CustomPivotAttribute attribute)
        {
            this.type = type;
            this.attribute = attribute;
            m_Icon = null;
        }
    }
    
    class EditorPivotManager : ScriptableSingleton<EditorPivotManager>
    {
        [SerializeField]
        CustomPivotMode m_ActivePivotMode;
        internal static CustomPivotMode activePivotMode
        {
            get
            {
                if (instance.m_ActivePivotMode == null) 
                    PivotManager.SetActivePivotMode(PivotManager.defaultPivotModeType);

                return instance.m_ActivePivotMode;
            }
        }
        
        internal static void SetActivePivotMode(Type pivotModeType)
        {
            if (instance.m_ActivePivotMode != null && instance.m_ActivePivotMode.GetType() == pivotModeType)
                return;

            CheckAndThrowIfTypeIncompatible(pivotModeType, typeof(CustomPivotMode));
            instance.m_ActivePivotMode = (CustomPivotMode)EditorToolManager.GetSingleton(pivotModeType);
            
            if (IsBuiltInPivotMode(activePivotMode))
                instance.m_LastBuiltInPivotMode = activePivotMode;

            var activePivotModeType = PivotManager.GetActivePivotMode().GetType();
            if (activePivotModeType == typeof(CenterPivotMode))
                Tools.pivotMode = PivotMode.Center;
            else if (activePivotModeType == typeof(PivotPointPivotMode))
                Tools.pivotMode = PivotMode.Pivot;
            else
            {
                Tools.pivotMode = PivotMode.Custom;
                instance.m_LastCustomPivotMode = activePivotMode;
            }
        }
        
        [SerializeField]
        CustomPivotMode m_LastCustomPivotMode;
        
        internal static CustomPivotMode lastCustomPivotMode => instance.m_LastCustomPivotMode;
        
        [SerializeField]
        CustomPivotMode m_LastBuiltInPivotMode;
        
        internal static CustomPivotMode lastBuiltInPivotMode => instance.m_LastBuiltInPivotMode;
        
        [SerializeField]
        CustomPivotRotation m_ActivePivotRotation;
        internal static CustomPivotRotation activePivotRotation
        {
            get
            {
                if (instance.m_ActivePivotRotation == null) 
                    PivotManager.SetActivePivotRotation(PivotManager.defaultPivotRotationType);

                return instance.m_ActivePivotRotation;
            }
        }
        
        internal static void SetActivePivotRotation(Type pivotRotationType)
        {
            if (instance.m_ActivePivotRotation != null && instance.m_ActivePivotRotation.GetType() == pivotRotationType)
                return;
            
            CheckAndThrowIfTypeIncompatible(pivotRotationType, typeof(CustomPivotRotation));
            instance.m_ActivePivotRotation = (CustomPivotRotation)EditorToolManager.GetSingleton(pivotRotationType);
            
            if (IsBuiltInPivotRotation(activePivotRotation))
                instance.m_LastBuiltInPivotRotation = activePivotRotation;

            var activePivotRotationType = PivotManager.GetActivePivotRotation().GetType();
            if (activePivotRotationType == typeof(GlobalPivotRotation))
                Tools.pivotRotation = PivotRotation.Global;
            else if (activePivotRotationType == typeof(LocalPivotRotation))
                Tools.pivotRotation = PivotRotation.Local;
            else
            {
                Tools.pivotRotation = PivotRotation.Custom;
                instance.m_LastCustomPivotRotation = activePivotRotation;
            }
        }
        
        [SerializeField]
        CustomPivotRotation m_LastCustomPivotRotation;
        internal static CustomPivotRotation lastCustomPivotRotation => instance.m_LastCustomPivotRotation;

        [SerializeField]
        CustomPivotRotation m_LastBuiltInPivotRotation;
        
        internal static CustomPivotRotation lastBuiltInPivotRotation => instance.m_LastBuiltInPivotRotation;
        
        // Maps a pivot setting type to its corresponding pivot setting definition.
        readonly Dictionary<Type, PivotSettingDefinition> m_TypeToPivotSettingDef;
        
        // Maps base setting type (EditorPivotMode, EditorPivotRotation, etc.) to a group of all subclassed settings.
        readonly Dictionary<Type, List<PivotSettingDefinition>> m_BaseTypeToPivotSettingDefs;
        
        internal static List<PivotSettingDefinition> pivotModeDefs => instance.m_BaseTypeToPivotSettingDefs[typeof(CustomPivotMode)];
        internal static List<PivotSettingDefinition> pivotRotationsDefs => instance.m_BaseTypeToPivotSettingDefs[typeof(CustomPivotRotation)];
        
        // Pivot settings available given the current active editor tool and context.
        List<PivotSettingDefinition> m_AvailablePivotSettings = new();
        
        internal static List<PivotSettingDefinition> availablePivotSettings => instance.m_AvailablePivotSettings;
        
        public static event Action availableSettingsChanged;
        
        EditorPivotManager()
        {
            m_TypeToPivotSettingDef = new Dictionary<Type, PivotSettingDefinition>();
            
            m_BaseTypeToPivotSettingDefs = new Dictionary<Type, List<PivotSettingDefinition>>();
            m_BaseTypeToPivotSettingDefs.Add(typeof(CustomPivotMode), new List<PivotSettingDefinition>());
            m_BaseTypeToPivotSettingDefs.Add(typeof(CustomPivotRotation), new List<PivotSettingDefinition>());
                
            var pivotSettingTypes = TypeCache.GetTypesWithAttribute<CustomPivotAttribute>();
            
            for (int i = 0; i < pivotSettingTypes.Count; ++i)
            {
                var type = pivotSettingTypes[i];

                if (m_TypeToPivotSettingDef.ContainsKey(type))
                    continue;
                
                if (typeof(CustomPivotMode).IsAssignableFrom(type) || typeof(CustomPivotRotation).IsAssignableFrom(type))
                {
                    var attr = (CustomPivotAttribute)type.GetCustomAttributes(typeof(CustomPivotAttribute), false)[0];
                    
                    if (attr.targetTool != null && !typeof(EditorTool).IsAssignableFrom(attr.targetTool))
                    {
                        Debug.LogError($"{GetType().Name} ({attr.displayName}): targetTool type argument must " +
                                       $"be of type EditorTool; attribute will be ignored.");
                        continue;
                    }

                    if (attr.targetToolContext != null && !typeof(EditorToolContext).IsAssignableFrom(attr.targetToolContext))
                    {
                        Debug.LogError($"{GetType().Name} ({attr.displayName}): targetToolContext type argument must " +
                                        $"be of type EditorToolContext; attribute will be ignored.");
                        continue;
                    }
                    
                    var definition = new PivotSettingDefinition(type, attr);
                    m_TypeToPivotSettingDef.Add(type, new PivotSettingDefinition(type, attr));
                    
                    if (typeof(CustomPivotMode).IsAssignableFrom(type))
                        m_BaseTypeToPivotSettingDefs[typeof(CustomPivotMode)].Add(definition);
                    
                    if (typeof(CustomPivotRotation).IsAssignableFrom(type))
                        m_BaseTypeToPivotSettingDefs[typeof(CustomPivotRotation)].Add(definition);
                }
                else
                {
                    Debug.LogError($"The PivotSetting attribute can only be applied to types of {nameof(CustomPivotMode)} or {nameof(CustomPivotRotation)}; " +
                                   $"attribute will be ignored.");
                }
            }

            // Sort by priority then move built-in settings to front.
            pivotModeDefs.Sort((a, b) => a.attribute.priority.CompareTo(b.attribute.priority));
            pivotModeDefs.Sort((a, b) => (!IsBuiltInPivotMode(a.type)).CompareTo(!IsBuiltInPivotMode(b.type)));
            
            pivotRotationsDefs.Sort((a, b) => a.attribute.priority.CompareTo(b.attribute.priority));
            pivotRotationsDefs.Sort((a, b) => (!IsBuiltInPivotRotation(a.type)).CompareTo(!IsBuiltInPivotRotation(b.type)));
        }

        void OnEnable()
        {
            RefreshAvailablePivotSettings();
            
            // If active settings failed to deserialize, set them to the default.
            if (m_ActivePivotMode == null)
                PivotManager.SetActivePivotMode(PivotManager.defaultPivotModeType);
            if (m_ActivePivotRotation == null)
                PivotManager.SetActivePivotRotation(PivotManager.defaultPivotRotationType);

            ToolManager.activeToolChanged += OnActiveToolChanged;
            ToolManager.activeContextChanged += OnActiveContextChanged;
        }

        void OnDisable()
        {
            ToolManager.activeToolChanged -= OnActiveToolChanged;
            ToolManager.activeContextChanged -= OnActiveContextChanged;
        }

        void OnActiveToolChanged()
        {
            FallbackActiveSettingsIfTargetsInvalid(); 
            RefreshAvailablePivotSettings();
        }

        void OnActiveContextChanged()
        {
            FallbackActiveSettingsIfTargetsInvalid();
            RefreshAvailablePivotSettings();
        }

        void FallbackActiveSettingsIfTargetsInvalid()
        {
            // If current active PivotMode is custom and incompatible with active tools, fallback to the last or default built-in pivot mode.
            if (!IsBuiltInPivotMode(activePivotMode))
            {
                if (!(m_TypeToPivotSettingDef.TryGetValue(PivotManager.GetActivePivotMode().GetType(), out var pivotModeDef) && 
                      ShouldSettingBeAvailable(pivotModeDef)))
                {
                    if (lastBuiltInPivotMode != null)
                        PivotManager.SetActivePivotMode(lastBuiltInPivotMode.GetType());
                    else
                        PivotManager.SetActivePivotMode(PivotManager.defaultPivotModeType);
                }
            }
            
            // If current active PivotRotation is custom and incompatible with active tools, fallback to the last or default built-in pivot rotation.
            if (!IsBuiltInPivotRotation(activePivotRotation))
            {
                if (!(m_TypeToPivotSettingDef.TryGetValue(PivotManager.GetActivePivotRotation().GetType(), out var pivotRotationDef) && 
                      ShouldSettingBeAvailable(pivotRotationDef)))
                {
                    if (lastBuiltInPivotRotation != null)
                        PivotManager.SetActivePivotRotation(lastBuiltInPivotRotation.GetType());
                    else
                        PivotManager.SetActivePivotRotation(PivotManager.defaultPivotRotationType);
                }
            }
        }
        
        void RefreshAvailablePivotSettings()
        {
            m_AvailablePivotSettings.Clear();

            foreach (var typeSettingDefPair in m_TypeToPivotSettingDef)
            {
                var settingDef = typeSettingDefPair.Value;
                if (ShouldSettingBeAvailable(settingDef))
                    m_AvailablePivotSettings.Add(settingDef);
            }
            
            availableSettingsChanged?.Invoke();
        }
        
        internal static void CheckAndThrowIfTypeIncompatible(Type pivotSettingType, Type expectedBasePivotSettingType)
        {
            if (!expectedBasePivotSettingType.IsAssignableFrom(pivotSettingType) || pivotSettingType.IsAbstract)
                throw new ArgumentException($"Type must be assignable to {expectedBasePivotSettingType}, and not abstract.");

            if (instance.m_TypeToPivotSettingDef.TryGetValue(pivotSettingType, out var settingDef))
            {
                var targetToolType = settingDef.targetTool;
                if (targetToolType != null && ToolManager.activeToolType != targetToolType)
                    throw new ArgumentException($"Cannot activate pivot setting as it targets {targetToolType} tool type " +
                                                $"but current active tool is of {ToolManager.activeToolType} type");
                
                var targetToolContextType = settingDef.targetToolContext;
                if (targetToolContextType != null && ToolManager.activeContextType != targetToolContextType)
                    throw new ArgumentException($"Cannot activate pivot setting as it targets {targetToolContextType} tool context type " +
                                                $"but current active tool context is of {ToolManager.activeContextType} type");
            }
            else 
            {
                throw new ArgumentException($"Cannot activate pivot setting as definition data for {pivotSettingType} could not be found." +
                                            $" Make sure the type is decorated with {nameof(CustomPivotAttribute)}.");
            }
        }
        
        bool ShouldSettingBeAvailable(PivotSettingDefinition settingDef)
        {
            var activeToolType = EditorToolManager.activeTool?.GetType();
            var activeContextType = EditorToolManager.activeToolContext?.GetType() ?? typeof(GameObjectToolContext);
            
            var noTargetToolOrContext = settingDef.targetTool == null && settingDef.targetToolContext == null;
            var targetToolMatches = settingDef.targetTool == activeToolType && settingDef.targetToolContext == null;
            var targetContextMatches = settingDef.targetToolContext == activeContextType && settingDef.targetTool == null;
            var toolAndContextMatch = settingDef.targetTool == activeToolType && settingDef.targetToolContext == activeContextType;

            return (noTargetToolOrContext || targetToolMatches || targetContextMatches || toolAndContextMatch);
        }
        
        internal static bool IsBuiltInPivotMode(CustomPivotMode pivotMode)
        {
            return (pivotMode is CenterPivotMode || pivotMode is PivotPointPivotMode);
        }
        
        internal static bool IsBuiltInPivotMode(Type pivotModeType)
        {
            return (pivotModeType == typeof(CenterPivotMode) || pivotModeType == typeof(PivotPointPivotMode));
        }
        
        internal static bool IsBuiltInPivotRotation(CustomPivotRotation pivotRotation)
        {
            return (pivotRotation is GlobalPivotRotation || pivotRotation is LocalPivotRotation);
        }
        
        internal static bool IsBuiltInPivotRotation(Type pivotRotationType)
        {
            return (pivotRotationType == typeof(GlobalPivotRotation) || pivotRotationType == typeof(LocalPivotRotation));
        }
        
        internal static bool IsBuiltInPivotSetting(Type pivotSettingType)
        {
            return IsBuiltInPivotMode(pivotSettingType) || IsBuiltInPivotRotation(pivotSettingType);
        }

        internal static bool IsPivotSettingAvailable(PivotSettingDefinition pivotSettingDef)
        {
            return availablePivotSettings.IndexOf(pivotSettingDef) != -1;
        }
        
        internal static Type GetNextPivotModeType(CustomPivotMode pivotMode)
        {
            return GetNextAvailableSettingType(pivotMode.GetType(), pivotModeDefs);
        }
        
        internal static Type GetNextPivotRotationType(CustomPivotRotation pivotRotation)
        {
            return GetNextAvailableSettingType(pivotRotation.GetType(), pivotRotationsDefs);
        }
        
        static Type GetNextAvailableSettingType(Type pivotSettingType, List<PivotSettingDefinition> pivotSettings)
        {
            for (int i = 0; i < pivotSettings.Count; ++i)
            {
                var settingDef = pivotSettings[i];
                // Find reference setting's definition
                if (settingDef.type == pivotSettingType && instance.m_AvailablePivotSettings.Contains(settingDef))
                {
                    // Find next currently available setting's definition
                    for (int j = 0; j < pivotSettings.Count; ++j)
                    {
                        var nextSettingIdx = (i + (j + 1)) % pivotSettings.Count;
                        var nextSettingDef = pivotSettings[nextSettingIdx];
                        
                        if (instance.m_AvailablePivotSettings.Contains(nextSettingDef))
                            return nextSettingDef.type;
                    }

                    break;
                }
            }
            
            return pivotSettingType;
        }
    }
}
