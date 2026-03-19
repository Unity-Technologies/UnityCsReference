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

    class EditorPivotManager : EditorToolStateManager<EditorPivotManager, EditorPivotManager.EditorPivotState>
    {
        [Serializable]
        internal class EditorPivotState: EditorToolStateBase
        {
            [SerializeField]
            CustomPivotMode m_ActivePivotMode;

            [SerializeField]
            CustomPivotMode m_LastCustomPivotMode;

            [SerializeField]
            CustomPivotMode m_LastBuiltInPivotMode;

            [SerializeField]
            CustomPivotRotation m_ActivePivotRotation;

            [SerializeField]
            CustomPivotRotation m_LastCustomPivotRotation;

            [SerializeField]
            CustomPivotRotation m_LastBuiltInPivotRotation;

            List<PivotSettingDefinition> m_AvailablePivotSettings = new();

            // For custom pivots, we essentially want to do handle rotations same way as when in Global Pivot rotation but without
            // having to resort to storing intermediate state in m_GlobalHandleRotation for active rotation persistence/delta calculation.
            // We don't want this because m_GlobalHandleRotation can be implicitly set for Global pivots through public API (Tools.handleRotation)
            // and that would interfere with what a custom implementation might be trying to return instead.
            // ActiveRotationTracker is used instead to track the intermediate handle rotation for all custom pivot rotation implementations.
            ActiveRotationTracker m_ActiveRotationTracker;
            ActiveRotationTracker activeRotationTrackerInternal
            {
                get
                {
                    if (m_ActiveRotationTracker == null)
                        m_ActiveRotationTracker = new ActiveRotationTracker();
                    return m_ActiveRotationTracker;
                }
            }

            public CustomPivotMode activePivotMode
            {
                get
                {
                    if (m_ActivePivotMode == null)
                        SetActivePivotMode(EditorToolsSettingsData.GetLastPivotModeType(stateToolOwnerType));

                    return m_ActivePivotMode;
                }
            }

            public CustomPivotMode lastCustomPivotMode => m_LastCustomPivotMode;
            public CustomPivotMode lastBuiltInPivotMode => m_LastBuiltInPivotMode;

            public CustomPivotRotation activePivotRotation
            {
                get
                {
                    if (m_ActivePivotRotation == null)
                        SetActivePivotRotation(EditorToolsSettingsData.GetLastPivotRotationType(stateToolOwnerType));

                    return m_ActivePivotRotation;
                }
            }

            public CustomPivotRotation lastCustomPivotRotation => m_LastCustomPivotRotation;
            public CustomPivotRotation lastBuiltInPivotRotation => m_LastBuiltInPivotRotation;

            public List<PivotSettingDefinition> availablePivotSettings => m_AvailablePivotSettings;
            public ActiveRotationTracker activeRotationTracker => activeRotationTrackerInternal;

            public void SetActivePivotMode(Type pivotModeType)
            {
                if (m_ActivePivotMode != null && m_ActivePivotMode.GetType() == pivotModeType)
                    return;

                CheckAndThrowIfTypeIncompatible(pivotModeType, typeof(CustomPivotMode), stateToolOwnerType);
                var toolManagerState = EditorToolManager.instance.GetOrCreateStateForType(stateToolOwnerType);
                if (toolManagerState != null)
                {
                    m_ActivePivotMode = (CustomPivotMode)toolManagerState.GetSingleton(pivotModeType);

                    if (IsBuiltInPivotMode(m_ActivePivotMode))
                        m_LastBuiltInPivotMode = m_ActivePivotMode;

                    var activePivotModeType = m_ActivePivotMode.GetType();
                    EditorToolsSettingsData.SetLastPivotModeType(activePivotModeType, stateToolOwnerType);

                    if (activePivotModeType == typeof(CenterPivotMode))
                        toolManagerState.pivotMode = PivotMode.Center;
                    else if (activePivotModeType == typeof(PivotPointPivotMode))
                        toolManagerState.pivotMode = PivotMode.Pivot;
                    else
                    {
                        toolManagerState.pivotMode = PivotMode.Custom;
                        m_LastCustomPivotMode = m_ActivePivotMode;
                    }
                }
            }

            public void SetActivePivotRotation(Type pivotRotationType)
            {
                if (m_ActivePivotRotation != null && m_ActivePivotRotation.GetType() == pivotRotationType)
                    return;

                CheckAndThrowIfTypeIncompatible(pivotRotationType, typeof(CustomPivotRotation), stateToolOwnerType);
                var toolManagerState = EditorToolManager.instance.GetOrCreateStateForType(stateToolOwnerType);
                if (toolManagerState != null)
                {
                    m_ActivePivotRotation = (CustomPivotRotation)toolManagerState.GetSingleton(pivotRotationType);

                    if (IsBuiltInPivotRotation(m_ActivePivotRotation))
                        m_LastBuiltInPivotRotation = m_ActivePivotRotation;

                    var activePivotRotationType = m_ActivePivotRotation.GetType();
                    EditorToolsSettingsData.SetLastPivotRotationType(activePivotRotationType, stateToolOwnerType);

                    if (activePivotRotationType == typeof(GlobalPivotRotation))
                        toolManagerState.pivotRotation = PivotRotation.Global;
                    else if (activePivotRotationType == typeof(LocalPivotRotation))
                        toolManagerState.pivotRotation = PivotRotation.Local;
                    else if (activePivotRotationType == typeof(GridPivotRotation))
                        toolManagerState.pivotRotation = PivotRotation.Grid;
                    else
                    {
                        toolManagerState.pivotRotation = PivotRotation.Custom;
                        m_LastCustomPivotRotation = m_ActivePivotRotation;
                    }
                }
            }

            public override void OnEnable()
            {
                RefreshAvailablePivotSettings();

                ToolManager.activeToolChangedForOwner += OnActiveToolChangedForType;
                ToolManager.activeContextChangedForOwner += OnActiveContextChangedForType;
            }

            public override void OnDisable()
            {
                ToolManager.activeToolChangedForOwner -= OnActiveToolChangedForType;
                ToolManager.activeContextChangedForOwner -= OnActiveContextChangedForType;
            }

            void OnActiveToolChangedForType(Type ownerType)
            {
                if (ownerType != stateToolOwnerType)
                    return;

                FallbackActiveSettingsIfTargetsInvalid();
                RefreshAvailablePivotSettings();
            }

            void OnActiveContextChangedForType(Type ownerType)
            {
                if (ownerType != stateToolOwnerType)
                    return;

                FallbackActiveSettingsIfTargetsInvalid();
                RefreshAvailablePivotSettings();
            }

            void FallbackActiveSettingsIfTargetsInvalid()
            {
                // If current active PivotMode is custom and incompatible with active tools, fallback to the last or default built-in pivot mode.
                if (!IsBuiltInPivotMode(activePivotMode))
                {
                    if (!(instance.m_TypeToPivotSettingDef.TryGetValue(m_ActivePivotMode.GetType(), out var pivotModeDef) &&
                          ShouldSettingBeAvailable(pivotModeDef)))
                    {
                        if (m_LastBuiltInPivotMode != null)
                            SetActivePivotMode(m_LastBuiltInPivotMode.GetType());
                        else
                            SetActivePivotMode(PivotManager.defaultPivotModeType);
                    }
                }

                // If current active PivotRotation is custom and incompatible with active tools, fallback to the last or default built-in pivot rotation.
                if (!IsBuiltInPivotRotation(activePivotRotation))
                {
                    if (!(instance.m_TypeToPivotSettingDef.TryGetValue(m_ActivePivotRotation.GetType(), out var pivotRotationDef) &&
                          ShouldSettingBeAvailable(pivotRotationDef)))
                    {
                        if (m_LastBuiltInPivotRotation != null)
                            SetActivePivotRotation(m_LastBuiltInPivotRotation.GetType());
                        else
                            SetActivePivotRotation(PivotManager.defaultPivotRotationType);
                    }
                }
            }

            public void RefreshAvailablePivotSettings()
            {
                m_AvailablePivotSettings.Clear();

                foreach (var typeSettingDefPair in instance.m_TypeToPivotSettingDef)
                {
                    var settingDef = typeSettingDefPair.Value;
                    if (ShouldSettingBeAvailable(settingDef))
                        m_AvailablePivotSettings.Add(settingDef);
                }

                if (stateToolOwnerType == typeof(SceneView))
                    availableSettingsChanged?.Invoke();

                availableSettingsChangedForType?.Invoke(stateToolOwnerType);
            }

            bool ShouldSettingBeAvailable(PivotSettingDefinition settingDef)
            {
                var activeToolType = EditorToolManager.GetActiveTool(stateToolOwnerType)?.GetType();
                var activeContextType = EditorToolManager.GetActiveToolContext(stateToolOwnerType)?.GetType() ?? typeof(GameObjectToolContext);

                // Pivots that don't target any tool and context are only available for SceneView or their type has to match the permitted built-in pivot types.
                var isUnrestrictedPivot = settingDef.targetTool == null && settingDef.targetToolContext == null && 
                                          (stateToolOwnerType == typeof(SceneView) || k_BuiltinPivotsAllowedInCustomOwners.Contains(settingDef.type));
                
                var targetToolMatches = settingDef.targetTool == activeToolType && settingDef.targetToolContext == null;
                var targetContextMatches = settingDef.targetToolContext == activeContextType && settingDef.targetTool == null;
                var toolAndContextMatch = settingDef.targetTool == activeToolType && settingDef.targetToolContext == activeContextType;

                return (isUnrestrictedPivot || targetToolMatches || targetContextMatches || toolAndContextMatch);
            }

            public Type GetNextPivotModeType()
            {
                return GetNextAvailableSettingType(activePivotMode.GetType(), pivotModeDefs);
            }

            public Type GetNextPivotRotationType()
            {
                return GetNextAvailableSettingType(activePivotRotation.GetType(), pivotRotationsDefs);
            }

            Type GetNextAvailableSettingType(Type pivotSettingType, List<PivotSettingDefinition> pivotSettings)
            {
                for (int i = 0; i < pivotSettings.Count; ++i)
                {
                    var settingDef = pivotSettings[i];
                    // Find reference setting's definition
                    if (settingDef.type == pivotSettingType && availablePivotSettings.Contains(settingDef))
                    {
                        // Find next currently available setting's definition
                        for (int j = 0; j < pivotSettings.Count; ++j)
                        {
                            var nextSettingIdx = (i + (j + 1)) % pivotSettings.Count;
                            var nextSettingDef = pivotSettings[nextSettingIdx];

                            if (availablePivotSettings.Contains(nextSettingDef))
                                return nextSettingDef.type;
                        }

                        break;
                    }
                }

                return pivotSettingType;
            }

        }

        static readonly List<Type> k_BuiltinPivotsAllowedInCustomOwners = 
        [
            typeof(CenterPivotMode),
            typeof(PivotPointPivotMode),
            typeof(GlobalPivotRotation),
            typeof(LocalPivotRotation)
        ];

        internal static List<PivotSettingDefinition> availablePivotSettings => instance.defaultState.availablePivotSettings;
        internal static ActiveRotationTracker activeRotationTracker => instance.defaultState.activeRotationTracker;
        
        // Maps a pivot setting type to its corresponding pivot setting definition.
        Dictionary<Type, PivotSettingDefinition> m_TypeToPivotSettingDef;

        // Maps base setting type (EditorPivotMode, EditorPivotRotation, etc.) to a group of all subclassed settings.
        Dictionary<Type, List<PivotSettingDefinition>> m_BaseTypeToPivotSettingDefs;

        internal static List<PivotSettingDefinition> pivotModeDefs
        {
            get
            {
                return instance.m_BaseTypeToPivotSettingDefs[typeof(CustomPivotMode)];
            }
        }

        internal static List<PivotSettingDefinition> pivotRotationsDefs
        {
            get
            {
                return instance.m_BaseTypeToPivotSettingDefs[typeof(CustomPivotRotation)];
            }
        }

        public static event Action availableSettingsChanged;
        public static event Action<Type> availableSettingsChangedForType;

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

                    if (attr.targetToolContext != null &&
                        !typeof(EditorToolContext).IsAssignableFrom(attr.targetToolContext))
                    {
                        Debug.LogError($"{GetType().Name} ({attr.displayName}): targetToolContext type argument must " +
                                       $"be of type EditorToolContext; attribute will be ignored.");
                        continue;
                    }

                    var definition = new PivotSettingDefinition(type, attr);
                    m_TypeToPivotSettingDef.Add(type, definition);

                    if (typeof(CustomPivotMode).IsAssignableFrom(type))
                        m_BaseTypeToPivotSettingDefs[typeof(CustomPivotMode)].Add(definition);

                    if (typeof(CustomPivotRotation).IsAssignableFrom(type))
                        m_BaseTypeToPivotSettingDefs[typeof(CustomPivotRotation)].Add(definition);
                }
                else
                {
                    Debug.LogError(
                        $"The PivotSetting attribute can only be applied to types of {nameof(CustomPivotMode)} or {nameof(CustomPivotRotation)}; " +
                        $"attribute will be ignored.");
                }
            }

            // Sort by priority then move built-in settings to front.
            pivotModeDefs.Sort((a, b) => a.attribute.priority.CompareTo(b.attribute.priority));
            pivotModeDefs.Sort((a, b) => (!IsBuiltInPivotMode(a.type)).CompareTo(!IsBuiltInPivotMode(b.type)));

            pivotRotationsDefs.Sort((a, b) => a.attribute.priority.CompareTo(b.attribute.priority));
            pivotRotationsDefs.Sort((a, b) => (!IsBuiltInPivotRotation(a.type)).CompareTo(!IsBuiltInPivotRotation(b.type)));
        }

        public void SyncToolsPivotStateIfNeeded()
        {
            // Ensure the Tools pivot state is sync with EditorPivotManager state.
            /* UUM-130229:
               Multiple Tools SO instances can accumulate after a series of domain reloads.
               Due to how its singleton pattern is implemented, the last instance that receives an OnEnable call becomes the "current" one.
               This can cause the correct Tools pivot state to be dropped if the "domain reload survivor" Tools instance it not last one enabled.*/
            
            var activePivotModeSV = GetActivePivotMode(typeof(SceneView));
            if (activePivotModeSV != null)
            {
                var activePivotModeType = activePivotModeSV.GetType();
                // Set Tools pivotMode state if it does not match EditorPivotManager's state
                if (activePivotModeType == typeof(CenterPivotMode))
                    Tools.pivotMode = PivotMode.Center;
                else if (activePivotModeType == typeof(PivotPointPivotMode))
                    Tools.pivotMode = PivotMode.Pivot;
                else if (!IsBuiltInPivotMode(activePivotModeSV))
                    Tools.pivotMode = PivotMode.Custom;
                else
                    Debug.LogError("Could not sync active pivot mode with any of the existing PivotMode enum values.");
            }

            var activePivotRotationSV = GetActivePivotRotation(typeof(SceneView));
            if (activePivotRotationSV!= null)
            {
                var activeRotationType = activePivotRotationSV.GetType();
                // Set Tools pivotRotation state if it does not match EditorPivotManager's state
                if (activeRotationType == typeof(GlobalPivotRotation))
                    Tools.pivotRotation = PivotRotation.Global;
                else if (activeRotationType == typeof(LocalPivotRotation))
                    Tools.pivotRotation = PivotRotation.Local;
                else if (activeRotationType == typeof(GridPivotRotation))
                    Tools.pivotRotation = PivotRotation.Grid;
                else if (!IsBuiltInPivotRotation(activePivotRotationSV))
                    Tools.pivotRotation = PivotRotation.Custom;
                else
                    Debug.LogError("Could not sync active pivot rotation with any of the existing PivotRotation enum values.");
            }

            // For custom states, call pivot getters so that their pivots are also initialized
            foreach (var customState in customStates)
            {
                _ = customState.activePivotMode;
                _ = customState.activePivotRotation;
            }
        }
        
        internal static bool IsActivePivotModeMatchingEnum(PivotMode pivotModeEnum, Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
            {
                switch (pivotModeEnum)
                {
                    case PivotMode.Center:
                        return state.activePivotMode is CenterPivotMode;
                    case PivotMode.Pivot:
                        return state.activePivotMode is PivotPointPivotMode;
                    case PivotMode.Custom:
                        return !IsBuiltInPivotMode(state.activePivotMode);
                }
            }

            return false;
        }

        internal static bool IsActivePivotRotationMatchingEnum(PivotRotation pivotRotationEnum, Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
            {
                switch (pivotRotationEnum)
                {
                    case PivotRotation.Local:
                        return state.activePivotRotation is LocalPivotRotation;
                    case PivotRotation.Global:
                        return state.activePivotRotation is GlobalPivotRotation;
                    case PivotRotation.Grid:
                        return state.activePivotRotation is GridPivotRotation;
                    case PivotRotation.Custom:
                        return !IsBuiltInPivotRotation(state.activePivotRotation);
                }
            }

            return false;
        }

        internal static void CheckAndThrowIfTypeIncompatible(Type pivotSettingType, Type expectedBasePivotSettingType)
        {
            CheckAndThrowIfTypeIncompatible(pivotSettingType, expectedBasePivotSettingType, typeof(SceneView));
        }
        
        internal static void CheckAndThrowIfTypeIncompatible(Type pivotSettingType, Type expectedBasePivotSettingType, Type ownerType)
        {
            if (ownerType == null)
                ownerType = typeof(SceneView);

            if (!expectedBasePivotSettingType.IsAssignableFrom(pivotSettingType) || pivotSettingType.IsAbstract)
                throw new ArgumentException($"Type must be assignable to {expectedBasePivotSettingType}, and not abstract.");

            if (instance.m_TypeToPivotSettingDef.TryGetValue(pivotSettingType, out var settingDef))
            {
                var targetToolType = settingDef.targetTool;
                var activeToolType = ToolManager.GetActiveToolType(ownerType);
                if (targetToolType != null && activeToolType != targetToolType)
                    throw new ArgumentException($"Cannot activate pivot setting as it targets {targetToolType} tool type " +
                                                $"but current active tool is of {activeToolType} type");

                var targetToolContextType = settingDef.targetToolContext;
                var activeContextType = ToolManager.GetActiveContextType(ownerType);
                if (targetToolContextType != null && activeContextType != targetToolContextType)
                    throw new ArgumentException($"Cannot activate pivot setting as it targets {targetToolContextType} tool context type " +
                                                $"but current active tool context is of {activeContextType} type");
            }
            else
            {
                throw new ArgumentException($"Cannot activate pivot setting as definition data for {pivotSettingType} could not be found." +
                                            $" Make sure the type is decorated with {nameof(CustomPivotAttribute)}.");
            }
        }

        internal static bool IsBuiltInPivotMode(CustomPivotMode pivotMode)
        {
            return IsBuiltInPivotMode(pivotMode.GetType());
        }

        internal static bool IsBuiltInPivotMode(Type pivotModeType)
        {
            return (pivotModeType == typeof(CenterPivotMode) || pivotModeType == typeof(PivotPointPivotMode));
        }

        internal static bool IsBuiltInPivotRotation(CustomPivotRotation pivotRotation)
        {
            return IsBuiltInPivotRotation(pivotRotation.GetType());
        }

        internal static bool IsBuiltInPivotRotation(Type pivotRotationType)
        {
            return (pivotRotationType == typeof(GlobalPivotRotation) ||
                    pivotRotationType == typeof(LocalPivotRotation) ||
                    pivotRotationType == typeof(GridPivotRotation));
        }

        internal static bool IsBuiltInPivotSetting(Type pivotSettingType)
        {
            return IsBuiltInPivotMode(pivotSettingType) || IsBuiltInPivotRotation(pivotSettingType);
        }

        internal static bool IsPivotSettingAvailable(PivotSettingDefinition pivotSettingDef)
        {
            return availablePivotSettings.IndexOf(pivotSettingDef) != -1;
        }

        internal static bool IsPivotSettingAvailable(PivotSettingDefinition pivotSettingDef, Type ownerType)
        {
            return GetAvailablePivotSettings(ownerType).IndexOf(pivotSettingDef) != -1;
        }
        
        internal static void SetActivePivotMode(Type pivotModeType, Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                state.SetActivePivotMode(pivotModeType);
        }
        
        internal static CustomPivotMode GetLastCustomPivotMode(Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                return state.lastCustomPivotMode;

            return null;
        }

        internal static void SetActivePivotRotation(Type pivotRotationType, Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                state.SetActivePivotRotation(pivotRotationType);
        }
        
        internal static List<PivotSettingDefinition> GetAvailablePivotSettings(Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                return state.availablePivotSettings;

            return instance.defaultState.availablePivotSettings;
        }

        internal static CustomPivotMode GetActivePivotMode(Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                return state.activePivotMode;

            return null;
        }
        
        internal static CustomPivotRotation GetActivePivotRotation(Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                return state.activePivotRotation;

            return null;
        }
        
        internal static CustomPivotRotation GetLastCustomPivotRotation(Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                return state.lastCustomPivotRotation;

            return null;
        }
    }
}
