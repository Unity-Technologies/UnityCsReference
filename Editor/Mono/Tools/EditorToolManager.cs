// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.Collections;
using UnityEditor.Actions;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    [Icon(k_IconPath)]
    public sealed class CreationToolsGroup
    {
        const string k_IconPath = "Icons/Toolbars/CreationToolsGroup.png";
        internal static readonly string k_Tooltip = L10n.Tr("Creation Tools");
        CreationToolsGroup() {}
    }

    sealed class EditorToolManager : EditorToolStateManager<EditorToolManager, EditorToolManager.EditorToolState>
    {
        [Serializable]
        internal class EditorToolState: EditorToolStateBase
        {
            [SerializeField]
            EditorTool m_ActiveTool;

            EditorActionTool m_ActiveOverride;

            Tool m_PreviousTool = Tool.Move;

            [SerializeField]
            Tool m_LastBuiltinTool = Tool.Move;

            [SerializeField]
            EditorTool m_LastCustomTool;

            [SerializeField]
            ToolVariantPrefs m_VariantPrefs = new ToolVariantPrefs();

            [SerializeField]
            List<ComponentEditor> m_ComponentTools = new List<ComponentEditor>();

            [SerializeField]
            List<ComponentEditor> m_ComponentContexts = new List<ComponentEditor>();

            [SerializeField]
            EditorToolContext m_ActiveToolContext;

            [SerializeField]
            ComponentToolCache m_PreviousComponentToolCache;

            bool m_ChangingActiveTool, m_ChangingActiveContext;

            public PivotMode pivotMode
            {
                get { return m_PivotMode; }
                set
                {
                    if (m_PivotMode != value || !EditorPivotManager.IsActivePivotModeMatchingEnum(value, stateToolOwnerType))
                    {
                        if (value == PivotMode.Custom)
                        {
                            // To match Tools.current behaviour, if Custom pivot mode is set, attempt to
                            // reactivate last custom pivot but do nothing and return if there's no last custom mode or it's invalid.
                            var activePivotMode = EditorPivotManager.GetActivePivotMode(stateToolOwnerType);
                            if (activePivotMode == null || EditorPivotManager.IsBuiltInPivotMode(activePivotMode))
                            {
                                var lastCustomMode = EditorPivotManager.GetLastCustomPivotMode(stateToolOwnerType);
                                if (lastCustomMode != null && activePivotMode != lastCustomMode)
                                {
                                    var lastCustomModeType = lastCustomMode.GetType();
                                    if (EditorPivotManager.IsPivotModeAvailable(lastCustomModeType, stateToolOwnerType))
                                        PivotManager.SetActivePivotMode(lastCustomModeType, stateToolOwnerType);
                                    else
                                    {
                                        var firstAvailable = EditorPivotManager.GetFirstAvailablePivotMode(stateToolOwnerType);
                                        if (firstAvailable != null)
                                            PivotManager.SetActivePivotMode(firstAvailable, stateToolOwnerType);
                                    }
                                }
                                else
                                    return;
                            }
                        }
                        else
                        {
                            switch (value)
                            {
                                case PivotMode.Center:
                                    PivotManager.SetActivePivotMode(typeof(CenterPivotMode), stateToolOwnerType);
                                    break;
                                case PivotMode.Pivot:
                                    PivotManager.SetActivePivotMode(typeof(PivotPointPivotMode), stateToolOwnerType);
                                    break;
                            }
                        }

                        m_PivotMode = value;

                        if (stateToolOwnerType == typeof(SceneView))
                            Tools.InvalidateHandlePosition();

                        Tools.InvokePivotModeChangedCallback(stateToolOwnerType);
                    }
                }
            }

            PivotMode m_PivotMode;

            public PivotRotation pivotRotation
            {
                get { return m_PivotRotation; }
                set
                {
                    if (m_PivotRotation != value || !EditorPivotManager.IsActivePivotRotationMatchingEnum(value, stateToolOwnerType))
                    {
                        if (value == PivotRotation.Custom)
                        {
                            // To match Tools.current behaviour, if Custom pivot mode is set, attempt to
                            // reactivate last custom pivot but do nothing and return if there's no last custom mode or it's invalid.
                            var activePivotRotation = EditorPivotManager.GetActivePivotRotation(stateToolOwnerType);
                            if (activePivotRotation == null || EditorPivotManager.IsBuiltInPivotRotation(activePivotRotation))
                            {
                                var lastCustomRotation = EditorPivotManager.GetLastCustomPivotRotation(stateToolOwnerType);
                                if (lastCustomRotation != null && activePivotRotation != lastCustomRotation)
                                {
                                    var lastCustomRotationType = lastCustomRotation.GetType();
                                    if (EditorPivotManager.IsPivotRotationAvailable(lastCustomRotationType, stateToolOwnerType))
                                        PivotManager.SetActivePivotRotation(lastCustomRotationType, stateToolOwnerType);
                                    else
                                    {
                                        var firstAvailable = EditorPivotManager.GetFirstAvailablePivotRotation(stateToolOwnerType);
                                        if (firstAvailable != null)
                                            PivotManager.SetActivePivotRotation(firstAvailable, stateToolOwnerType);
                                    }
                                }
                                else
                                    return;
                            }
                        }
                        else
                        {
                            switch (value)
                            {
                                case PivotRotation.Global:
                                    PivotManager.SetActivePivotRotation(typeof(GlobalPivotRotation), stateToolOwnerType);
                                    break;
                                case PivotRotation.Local:
                                    PivotManager.SetActivePivotRotation(typeof(LocalPivotRotation), stateToolOwnerType);
                                    break;
                                case PivotRotation.Grid:
                                    PivotManager.SetActivePivotRotation(typeof(GridPivotRotation), stateToolOwnerType);
                                    break;
                            }
                        }

                        m_PivotRotation = value;
                        Tools.InvokePivotRotationChangedCallback(stateToolOwnerType);
                    }
                }
            }

            PivotRotation m_PivotRotation;

            public EditorActionTool activeOverride
            { get => m_ActiveOverride; set => m_ActiveOverride = value; }

            public Tool previousTool
            { get => m_PreviousTool; set => m_PreviousTool = value; }

            public Tool lastBuiltinTool
            { get => m_LastBuiltinTool; set => m_LastBuiltinTool = value; }

            public EditorTool lastCustomTool
            { get => m_LastCustomTool; set => m_LastCustomTool = value; }

            public ToolVariantPrefs variantPrefs
            { get => m_VariantPrefs; set => m_VariantPrefs = value; }

            public List<ComponentEditor> componentTools
            { get => m_ComponentTools; set => m_ComponentTools = value; }

            public List<ComponentEditor> componentContexts
            { get => m_ComponentContexts; set => m_ComponentContexts = value; }

            public ComponentToolCache previousComponentToolCache
            { get => m_PreviousComponentToolCache; set => m_PreviousComponentToolCache = value; }

            EditorToolUtility.SortedContextDataCache m_SortedContextsDataCache;
            public EditorToolUtility.SortedContextDataCache sortedContextsDataCache
            {
                get => m_SortedContextsDataCache ??= new EditorToolUtility.SortedContextDataCache(this);
            }

            [SerializeField]
            List<ScriptableObject> m_SingletonObjects = new List<ScriptableObject>();

            public List<ScriptableObject> singletonObjects { get => m_SingletonObjects; set => m_SingletonObjects = value; }

            bool m_Hidden;
            public bool hidden { get => m_Hidden; set => m_Hidden = value; }

            internal EditorTool lastManipulationTool
            {
                get
                {
                    var lastToolEnum = (int)lastBuiltinTool;
                    var lastToolInstance = EditorToolUtility.GetEditorToolWithEnum((Tool)Mathf.Clamp(lastToolEnum, (int)Tool.Move, (int)Tool.Custom), stateToolOwnerType);

                    if (lastToolInstance != null)
                        return lastToolInstance;

                    // if the current context doesn't support the last built-in tool, cycle through Tool until we get a valid one
                    for (int i = (int)Tool.Move; i < (int)Tool.Custom; i++)
                    {
                        lastToolInstance = EditorToolUtility.GetEditorToolWithEnum((Tool)i, stateToolOwnerType);

                        if (lastToolInstance != null)
                        {
                            activeTool = lastToolInstance;
                            return lastToolInstance;
                        }
                    }

                    // if the current context doesn't support any tools (???) then fall back to the builtin Move Tool
                    return GetSingleton<MoveTool>();
                }
            }

            internal EditorTool activeTool
            {
                get { return m_ActiveTool; }

                set
                {
                    if (m_ChangingActiveTool)
                    {
                        // pop the changing state so that we don't lock the active tool after an exception is thrown.
                        m_ChangingActiveTool = false;
                        throw new InvalidOperationException("Attempting to set the active tool from EditorTool.OnActivate or EditorTool.OnDeactivate. This is not allowed.");
                    }

                    var tool = value;

                    if (tool == null)
                        tool = GetSingleton<NoneTool>();

                    if (tool == m_ActiveTool)
                        return;

                    m_ChangingActiveTool = true;
                    activeOverride = null;

                    ToolManager.ActiveToolWillChange(stateToolOwnerType);

                    var previous = m_ActiveTool;
                    var meta = EditorToolUtility.GetMetaData(tool.GetType(), stateToolOwnerType);

                    if (previous != null)
                    {
                        previous.Deactivate();

                        var previousMeta = EditorToolUtility.GetMetaData(previous.GetType(), stateToolOwnerType);
                        var previousEnum = EditorToolUtility.GetEnumWithEditorTool(previous, activeToolContext);
                        if (previousEnum != Tool.View
                            && previousEnum != Tool.None
                            && (EditorToolUtility.IsBuiltinOverride(previous, activeToolContext) || !EditorToolUtility.IsComponentTool(previous.GetType(), stateToolOwnerType))
                            // if the previous and current tools are from the same variant group, don't save the previous variant as previous tool
                            && (meta.variantGroup == null || previousMeta.variantGroup != meta.variantGroup))
                        {
                            previousTool = previousEnum;

                            if (EditorToolUtility.IsManipulationTool(previousEnum))
                                lastBuiltinTool = previousEnum;
                            else
                                lastCustomTool = previous;
                        }
                    }

                    m_ActiveTool = tool;

                    if (m_ActiveTool is IHasToolOwner toolWithOwner)
                        toolWithOwner.SetToolOwner(stateToolOwnerType);

                    m_ActiveTool.Activate();

                    ToolManager.ActiveToolDidChange(stateToolOwnerType);

                    if (stateToolOwnerType == typeof(SceneView))
                        activeToolChanged?.Invoke(previous, m_ActiveTool);

                    activeToolChangedForOwner?.Invoke(previous, m_ActiveTool, stateToolOwnerType);

                    Tools.SyncToolEnum();
                    if (stateToolOwnerType == typeof(SceneView))
                        Tools.InvalidateHandlePosition();

                    if (meta.variantGroup != null)
                        variantPrefs.SetPreferredVariant(meta.variantGroup, meta.editor);

                    m_ChangingActiveTool = false;
                }
            }

            internal EditorToolContext activeToolContext
            {
                get
                {
                    if (m_ActiveToolContext == null)
                    {
                        m_ActiveToolContext = GetSingleton(defaultToolContextType) as EditorToolContext;
                        ToolManager.ActiveContextDidChange(stateToolOwnerType);
                        m_ActiveToolContext.Activate();
                    }
                    return m_ActiveToolContext;
                }

                set
                {
                    if (m_ChangingActiveContext)
                    {
                        // pop the changing state so that we don't lock the active tool after an exception is thrown.
                        m_ChangingActiveContext = false;
                        throw new InvalidOperationException("Setting the active context from EditorToolContext.OnActivated or EditorToolContext.OnWillBeDeactivated is not allowed.");
                    }

                    var ctx = value == null ? GetSingleton(defaultToolContextType) as EditorToolContext: value;
                    if (ctx == activeToolContext)
                        return;

                    m_ChangingActiveContext = true;

                    // Make sure to get the active tool enum prior to setting the context, otherwise we'll be comparing
                    // apples to oranges. Ie, the transform tools will be different despite being the same `Tool` enum value.

                    var toolEnum = Tools.GetCurrent(stateToolOwnerType);
    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var wasAdditionalContextTool = toolEnum == Tool.Custom && additionalContextToolTypesCache.Contains(activeTool.GetType());
    #pragma warning restore UA2001
                    var previousCtx = m_ActiveToolContext;
                    if (previousCtx != null)
                    {
                        previousCtx.Deactivate();
                    }

                    ToolManager.ActiveContextWillChange(stateToolOwnerType);
                    m_ActiveToolContext = ctx;

                    if (ctx is IHasToolOwner ctxWithOwner)
                        ctxWithOwner.SetToolOwner(stateToolOwnerType);

                    ctx.Activate();

                    RebuildAvailableTools();

                    var active = m_ActiveTool;

                    // If the previous tool was a Move, Rotate, Scale, Rect, or Transform tool we need to resolve the tool
                    // type using the new context. Additionally, if the previous tool was null we'll take the opportunity
                    // to assign a valid tool.
                    if (EditorToolUtility.IsManipulationTool(toolEnum) || active == null || active is NoneTool)
                    {
                        var resolved = EditorToolUtility.GetEditorToolWithEnum(toolEnum, stateToolOwnerType, ctx);

                        // Always try to resolve to a valid tool when switching contexts, even if it means changing the
                        // active tool type
                        for (int i = (int)Tool.Move; (resolved == null || resolved is NoneTool) && i < (int)Tool.Custom; i++)
                            resolved = EditorToolUtility.GetEditorToolWithEnum((Tool)i, stateToolOwnerType);

                        // If resolved is still null at this point, the setter for activeTool will substitute an instance of
                        // NoneTool for us.
                        activeTool = resolved;
                    }
                    // If the previous tool was an additional tool from the context, return to the Previous Persistent Tool
                    // when moving to that new context
                    else if (wasAdditionalContextTool)
                    {
    #pragma warning disable UA2007 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        var isAdditionalContextTool = m_ActiveToolContext.GetAdditionalToolTypes().Contains(activeTool.GetType());
    #pragma warning restore UA2007

                        if(!isAdditionalContextTool)
                            RestorePreviousPersistentTool();
                    }
                    else
                    {
                        if (toolEnum == Tool.Custom)
                        {
                            // If this is a global custom tool, and it's targeting the previous context - we need to switch out of it
                            var toolMeta = EditorToolUtility.GetMetaData(active.GetType(), stateToolOwnerType);
                            if (toolMeta.targetBehaviour == typeof(NullTargetKey) && toolMeta.targetContext == previousCtx.GetType())
                                RestorePreviousPersistentTool();
                        }
                    }

                    ToolManager.ActiveContextDidChange(stateToolOwnerType);

                    m_ChangingActiveContext = false;
                }
            }

            public override void OnEnable()
            {
                base.OnEnable();

                Undo.undoRedoEvent += UndoRedoPerformed;
                ActiveEditorTracker.editorTrackerRebuilt += TrackerRebuilt;
                Selection.selectedObjectWasDestroyed += SelectedObjectWasDestroyed;
                AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;

                if (activeTool != null)
                    EditorApplication.delayCall += () => activeTool.Activate();
                if (activeToolContext != null)
                    EditorApplication.delayCall += () => activeToolContext.Activate();
            }

            public override void OnDisable()
            {
                base.OnDisable();
                activeOverride = null;

                Undo.undoRedoEvent -= UndoRedoPerformed;
                ActiveEditorTracker.editorTrackerRebuilt -= TrackerRebuilt;
                Selection.selectedObjectWasDestroyed -= SelectedObjectWasDestroyed;
                AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
            }

            internal void OnToolGUI(EditorWindow window)
            {
                if (!IsGizmoCulledBySceneCullingMasksOrFocusedScene(activeToolContext.target))
                {
                    AddDefaultHandleToAvoidExitingToolContext();
                    activeToolContext.OnToolGUI(window);
                }

                if (activeOverride != null)
                {
                    activeOverride.OnGUI(window);
                    return;
                }

                if (Tools.GetHidden(window.GetType()) || activeTool == null)
                    return;

                var current = activeTool;

                if (IsGizmoCulledBySceneCullingMasksOrFocusedScene(current.target))
                    return;

                using (new EditorGUI.DisabledScope(!current.IsAvailable() || current.isHidden))
                {
                    current.OnToolGUI(window);
                }

                var evt = Event.current;
                if (evt.type == EventType.KeyDown && evt.keyCode == KeyCode.Escape && TryPopToolState(window.GetType()))
                    evt.Use();
            }

            public void RestorePreviousPersistentTool()
            {
                activeTool = lastManipulationTool;
            }

            internal void InvokeOnSceneGUICustomEditorTools()
            {
                foreach (var context in componentContexts)
                {
                    if (IsGizmoCulledBySceneCullingMasksOrFocusedScene(context.target))
                        continue;

                    // ReSharper disable once SuspiciousTypeConversion.Global
                    if (context.editor is IDrawSelectedHandles handle)
                        handle.OnDrawHandles();
                }

                foreach (var tool in componentTools)
                {
                    if (IsGizmoCulledBySceneCullingMasksOrFocusedScene(tool.target))
                        continue;

                    // ReSharper disable once SuspiciousTypeConversion.Global
                    if (tool.editor is IDrawSelectedHandles handle)
                        handle.OnDrawHandles();
                }
            }

            internal bool IsGizmoCulledBySceneCullingMasksOrFocusedScene(UnityObject uobject)
            {
                var cmp = uobject as UnityEngine.Component;
                if (cmp == null)
                    return false;

                return StageUtility.IsGizmoCulledBySceneCullingMasksOrFocusedScene(cmp.gameObject, Camera.current);
            }

            internal void AddDefaultHandleToAvoidExitingToolContext()
            {
                if (activeToolContext.GetType() == defaultToolContextType
                    || !activeToolContext.overridesDefaultSelection)
                    return;

                int id = GUIUtility.GetControlID(FocusType.Passive);
                Event evt = Event.current;
                switch (evt.GetTypeForControl(id))
                {
                    case EventType.Layout:
                    case EventType.MouseMove:
                            HandleUtility.AddDefaultControl(id);
                        break;
                }
            }

            void UndoRedoPerformed(in UndoRedoInfo info)
            {
                RestoreCustomEditorTool();
            }

            void BeforeAssemblyReload()
            {
                if (m_ActiveTool != null)
                    m_ActiveTool.Deactivate();

                if (m_ActiveToolContext != null)
                    m_ActiveToolContext.Deactivate();
            }

            void RestoreCustomEditorTool()
            {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var restored = componentTools.FirstOrDefault(previousComponentToolCache.IsEqual);
#pragma warning restore UA2001

                if (restored != null)
                {
                    // todo Use generated Context
                    if (previousComponentToolCache.contextType != null)
                        activeToolContext = GetComponentContext(previousComponentToolCache.contextType, stateToolOwnerType);

                    activeTool = restored.GetEditor<EditorTool>();
                }

                previousComponentToolCache = ComponentToolCache.Empty;
            }

            public void TrackerRebuilt()
            {
                // when entering play mode there is an intermediate tracker rebuild where nothing is selected. ignore it.
                if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                    return;

                RebuildAvailableContexts();
                RebuildAvailableTools();
                EnsureCurrentToolIsNotNull();
            }

            void RebuildAvailableContexts()
            {
                var activeContextType = activeToolContext.GetType();
                ClearComponentContexts();

                EditorToolUtility.InstantiateComponentContexts(componentContexts, stateToolOwnerType);

                var restoredContext = componentContexts.Find(x => x.editorType == activeContextType);
                if (restoredContext != null)
                    activeToolContext = restoredContext.GetEditor<EditorToolContext>();

                sortedContextsDataCache.SetDirty();
            }

            public void EnsureCurrentToolIsNotNull()
            {
                if (m_ActiveTool == null)
                    RestorePreviousPersistentTool();
            }

            // destroy invalid custom editor tools
            void ClearCustomEditorTools()
            {
                m_ActiveOverride = null;

                foreach (var customEditorTool in m_ComponentTools)
                {
                    if (customEditorTool.editor == m_ActiveTool)
                        m_ActiveTool.Deactivate();
                    DestroyImmediate(customEditorTool.editor);
                }

                m_ComponentTools.Clear();
            }

            void RebuildAvailableTools()
            {
                ComponentToolCache activeComponentTool = new ComponentToolCache(activeToolContext, activeTool, stateToolOwnerType);
                ClearCustomEditorTools();

                EditorToolUtility.InstantiateComponentTools(activeToolContext, componentTools, stateToolOwnerType);

                if (activeComponentTool.toolType != null)
                {
                    var restoredTool = componentTools.Find(x => x.editorType == activeComponentTool.toolType);

                    if (restoredTool != null)
                    {
                        activeTool = restoredTool.GetEditor<EditorTool>();
                    }
                    else
                    {
                        previousComponentToolCache = activeComponentTool;
                        RestorePreviousPersistentTool();
                    }
                }

                EditorToolsSettingsData.instance.RefreshToolsData();
                if (stateToolOwnerType == typeof(SceneView))
                    availableToolsChanged?.Invoke();
                availableToolsChangedForOwner?.Invoke(stateToolOwnerType);
            }

            // Checks the currently instantiated (or available global type) tools for a matching instance.
            internal bool GetAvailableTool(EditorTypeAssociation typeAssociation, out EditorTool tool)
            {
                tool = null;

                // unlike the ToolManager interface that throws an exception, this should only log an error so as not to
                // prevent execution.
                if (!typeof(EditorTool).IsAssignableFrom(typeAssociation.editor) || typeAssociation.editor.IsAbstract)
                {
                    Debug.LogError($"Invalid tool type provided by context {activeToolContext}, \"{typeAssociation.editor}\". Type must be assignable to EditorTool, and not abstract.");
                    return false;
                }

                // early exit if the tool context is not applicable
                if (typeAssociation.targetContext != null && typeAssociation.targetContext != activeToolContext.GetType())
                    return false;

                // if this is a component tool
                if (typeAssociation.targetBehaviour != null && typeAssociation.targetBehaviour != typeof(NullTargetKey))
                    return (tool = GetComponentTool(typeAssociation.editor, stateToolOwnerType,  false)) != null;

                tool = (EditorTool) GetSingleton(typeAssociation.editor);

                return true;
            }

            void ClearComponentContexts()
            {
                foreach (var context in componentContexts)
                {
                    if (context.GetEditor<EditorToolContext>() == activeToolContext)
                        activeToolContext.Deactivate();
                    DestroyImmediate(context.editor);
                }

                componentContexts.Clear();
            }

            void CleanupSingletons()
            {
                for (int i = m_SingletonObjects.Count - 1; i > -1; i--)
                {
                    if (m_SingletonObjects[i] == null)
                        m_SingletonObjects.RemoveAt(i);
                }
            }

            public T GetSingleton<T>() where T : ScriptableObject
            {
                return (T)GetSingleton(typeof(T));
            }

            public ScriptableObject GetSingleton(Type type)
            {
                CleanupSingletons();
                if (type == null)
                    return null;
                var res = default(ScriptableObject);
                for (int i = 0; i < singletonObjects.Count; ++i)
                {
                    if (singletonObjects[i].GetType() == type)
                    {
                        res = singletonObjects[i];
                        break;
                    }
                }

                if (res != null)
                {
                    if (res is IHasToolOwner toolWithOwner)
                        toolWithOwner.SetToolOwner(stateToolOwnerType);
                    return res;
                }

                res = CreateInstance(type);
                res.hideFlags = HideFlags.DontSave;
                singletonObjects.Add(res);
                if (res is IHasToolOwner newToolWithOwner)
                    newToolWithOwner.SetToolOwner(stateToolOwnerType);

                return res;
            }

            void SaveComponentTool()
            {
                previousComponentToolCache = new ComponentToolCache(activeToolContext, activeTool, stateToolOwnerType);
            }

            void SelectedObjectWasDestroyed(EntityId id)
            {
#pragma warning disable UA2006 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                bool componentToolActive = componentTools.Any(
#pragma warning restore UA2006
                                               x => x?.GetEditor<EditorTool>() == activeTool)
#pragma warning disable UA2006 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                                           && activeTool.m_Targets.Any(x => x == null || x.GetEntityId() == id);
#pragma warning restore UA2006

#pragma warning disable UA2006 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                bool componentContextActive = componentContexts.Any(
#pragma warning restore UA2006
                                                  x => x?.GetEditor<EditorToolContext>() == activeToolContext)
#pragma warning disable UA2006 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                                              && activeToolContext.targets.Any(x => x == null || x.GetEntityId() == id);
#pragma warning restore UA2006

                if (componentToolActive || componentContextActive)
                {
                    SaveComponentTool();
                    RestorePreviousPersistentTool();
                }
            }

             // Collect all available tools into applicable UI categories and variant groups
            public void GetAvailableTools(List<ToolEntry> tools, EditorToolContext context = null)
            {
                if (context == null)
                    context = activeToolContext;

                // at each step, check if tool is already present as a variant, and collect available variants when appending
                // 1. collect built-in tools
                // 2. collect built-in additional tools
                // 3. collect custom global tools
                // 4. collect component tools for shared tracker
                tools.Clear();

                void AddToolEntry(Type tool, ToolEntry.Scope scope)
                {
                    var meta = EditorToolUtility.GetMetaData(tool, stateToolOwnerType);
                    var entry = new ToolEntry(meta, scope);

                    if (meta.variantGroup != null)
                    {
                        // Because this function collects all variants when appending to the list, we can safely assume that
                        // if a variant group exists in the tools list the tool is also already appended.
    #pragma warning disable UA2006 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        if (tools.Any(x => x.variantGroup == meta.variantGroup
    #pragma warning restore UA2006
                                        && x.componentTool == entry.componentTool))
                            return;

                        foreach (var variant in EditorToolUtility.GetEditorsForVariant(meta, stateToolOwnerType))
                            if (GetAvailableTool(variant, out var i))
                                entry.tools.Add(i);
                    }
                    else if (GetAvailableTool(meta, out var i))
                    {
                        entry.tools.Add(i);
                    }

                    if (entry.tools.Count > 0)
                        tools.Add(entry);
                }

                // 1. builtin (transform) tools
                for (int i = (int)Tool.View; i < (int)Tool.Custom; ++i)
                {
                    var toolType = context.ResolveTool((Tool)i);
                    if (toolType != null)
                    {
                        // If tool owner is not SceneView, and we've resolved to a builtin tool but not an override
                        // then skip this tool and make it unavailable.
                        if (stateToolOwnerType != typeof(SceneView) &&
                            (EditorToolUtility.IsManipulationToolType(toolType) || toolType == typeof(ViewModeTool)))
                        {
                            continue;
                        }

                        AddToolEntry(toolType, (ToolEntry.Scope)i);
                    }
                }

                // 2. builtin (additional) tools
                foreach(var tool in context.GetAdditionalToolTypes())
                    AddToolEntry(tool, ToolEntry.Scope.BuiltinAdditional);

                // 3. custom global tools
                foreach (var global in EditorToolUtility.GetCustomEditorToolsForType(null, stateToolOwnerType))
                {
                    // Global tool is supported if it's not targeting any context and the owner is SceneView
                    // or if it targets a context that the non-SceneView owner supports.
                    if ((global.targetContext == null && stateToolOwnerType == typeof(SceneView)) ||
                         global.targetContext == activeToolContext.GetType())
                        AddToolEntry(global.editor, global.group == null ? ToolEntry.Scope.CustomGlobal : ToolEntry.Scope.Grouped);
                }

                // Don't support component tools if this owner is not SceneView
                if (stateToolOwnerType != typeof(SceneView))
                    return;

                // 4. component tools
                foreach (var tool in componentTools)
                {
                    if ((tool.typeAssociation.targetContext == null ||
                         tool.typeAssociation.targetContext == context.GetType())
                        && tool.editorType != null // The editor type can be null on domain reload after renaming an EditorTool (UUM-113403)
                        && !tool.lockedInspector
#pragma warning disable UA2006 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        && !tools.Any(entry => entry.tools.Any(x => x == tool.editor)))
#pragma warning restore UA2006
                    {
                        AddToolEntry(tool.editorType,
                            tool.typeAssociation.group == null ? ToolEntry.Scope.Component : ToolEntry.Scope.Grouped);
                    }
                }
            }
        }

        public ToolVariantPrefs variantPrefs => defaultState.variantPrefs;

        // Mimic behavior of Tools.toolChanged for backwards compatibility until existing tools are converted to the new
        // apis.
        internal static event Action<EditorTool, EditorTool> activeToolChanged;

        internal static event Action<EditorTool, EditorTool, Type> activeToolChangedForOwner;

        // unfiltered component tools includes locked inspectors
        internal IEnumerable<ComponentEditor> componentTools => defaultState.componentTools;

        internal static IEnumerable<ComponentEditor> componentContexts => instance.defaultState.componentContexts;

        internal static IReadOnlyList<Type> additionalContextToolTypesCache = Array.Empty<Type>();

        internal static EditorTool activeTool
        {
            get => instance.defaultState.activeTool;
            set => instance.defaultState.activeTool = value;
        }

        internal static void SetActiveTool(EditorTool tool, Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                state.activeTool = tool;
        }

        internal static EditorToolContext activeToolContext
        {
            get => instance.defaultState.activeToolContext;
            set => instance.defaultState.activeToolContext = value;
        }

        internal static void SetActiveToolContext(EditorToolContext context, Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                state.activeToolContext = context;
        }

        internal static EditorToolContext GetActiveToolContext(Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                return state.activeToolContext;
            return null;
        }

        // this tool will transparently override the `OnToolGUI` method of the active tool.
        // do not expose this as public API with also considering how to handle lifecycle and active tool interop.
        // currently this is only used for EditorToolAction.
        internal static EditorActionTool activeOverride
        {
            get => instance.defaultState.activeOverride;

            set
            {
                instance.defaultState.activeOverride?.Dispose();
                instance.defaultState.activeOverride = value;
            }
        }

        internal static void SetActiveOverride(EditorActionTool editorActionToolOverride, Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                state.activeOverride = editorActionToolOverride;
        }

        [Serializable]
        public struct ComponentToolCache : ISerializationCallbackReceiver
        {
            [SerializeField]
            string m_ToolType;
            [SerializeField]
            string m_ContextType;

            [NonSerialized]
            public Type contextType;
            [NonSerialized]
            public Type toolType;

            public UnityObject targetObject;
            public UnityObject[] targetObjects;

            public static readonly ComponentToolCache Empty = new ComponentToolCache(null, null, typeof(SceneView));

            public ComponentToolCache(EditorToolContext context, EditorTool tool, Type toolOwner)
            {
                bool customTool = EditorToolUtility.IsCustomEditorTool(tool, toolOwner);
                bool customContext = EditorToolUtility.IsCustomToolContext(context);

                toolType = null;
                contextType = null;
                targetObject = null;
                targetObjects = null;

                if (customTool || customContext)
                {
                    if (context != null)
                        contextType = customContext ? context.GetType() : null;

                    if (tool != null)
                    {
                        toolType = customTool ? tool.GetType() : null;
                        targetObject = tool.target;
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        targetObjects = tool.targets.ToArray();
#pragma warning restore UA2001
                    }
                }

                m_ToolType = null;
                m_ContextType = null;
            }

            public bool IsEqual(ComponentEditor other)
            {
                var editor = other?.GetEditor<EditorTool>();

                if (editor == null || targetObjects == null || editor.targets == null)
                    return false;

                // todo need to cache ComponentEditor targets
#pragma warning disable UA2014 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return toolType == editor.GetType() && targetObjects.SequenceEqual(editor.targets);
#pragma warning restore UA2014
            }

            public override string ToString()
            {
                return $"Tool: {toolType} Context: {contextType}";
            }

            public void OnBeforeSerialize()
            {
                m_ToolType = toolType != null ? toolType.AssemblyQualifiedName : null;
                m_ContextType = contextType != null ? contextType.AssemblyQualifiedName : null;
            }

            public void OnAfterDeserialize()
            {
                if (!string.IsNullOrEmpty(m_ToolType))
                    toolType = Type.GetType(m_ToolType);
                if (!string.IsNullOrEmpty(m_ContextType))
                    contextType = Type.GetType(m_ContextType);
            }
        }

        internal static event Action availableToolsChanged;
        internal static event Action<Type> availableToolsChangedForOwner;

        EditorToolManager() {}

        // used by tests
        internal static void ForceTrackerRebuild()
        {
            instance.defaultState.TrackerRebuilt();
            foreach (var state in instance.customStates)
                state.TrackerRebuilt();
        }

        internal static T GetSingleton<T>() where T : ScriptableObject
        {
            return GetSingleton<T>(typeof(SceneView));
        }

        internal static ScriptableObject GetSingleton(Type type)
        {
            return GetSingleton(type, typeof(SceneView));
        }

        internal static T GetSingleton<T>(Type ownerType) where T : ScriptableObject
        {
            return (T)GetSingleton(typeof(T), ownerType);
        }

        internal static ScriptableObject GetSingleton(Type type, Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
                return state.GetSingleton(type);

            return null;
        }

        internal static EditorToolState GetEditorToolStateForOwner(Type ownerType)
        {
            return instance.GetOrCreateStateForType(ownerType);
        }

        public static EditorTool GetActiveTool()
        {
            return GetActiveTool(typeof(SceneView));
        }

        internal static EditorTool GetActiveTool(Type ownerType)
        {
            var state = instance.GetOrCreateStateForType(ownerType);
            if (state != null)
            {
                state.EnsureCurrentToolIsNotNull();
                return state.activeTool;
            }

            return null;
        }

        internal EditorTool lastManipulationTool
        {
            get
            {
                return defaultState.lastManipulationTool;
            }
        }

        internal static Tool previousTool => instance.defaultState.previousTool;

        internal static EditorTool lastCustomTool => instance.defaultState.lastCustomTool;

        public static void RestorePreviousPersistentTool()
        {
            RestorePreviousPersistentTool(typeof(SceneView));
        }

        public static void RestorePreviousPersistentTool(Type toolOwner)
        {
            var state = instance.GetOrCreateStateForType(toolOwner);
            if (state != null)
                state.RestorePreviousPersistentTool();
        }

        // Used by tests - EditModeAndPlayModeTests/EditorTools/EscKeyTests
        internal static bool TryPopToolState()
        {
            return TryPopToolState(typeof(SceneView));
        }

        internal static bool TryPopToolState(Type toolOwnerType)
        {
            // Tools.viewToolActive is currently only tied to SceneView tool owners.
            if (Tools.viewToolActive && toolOwnerType == typeof(SceneView))
                return false;

            var activeOwnerTool = GetActiveTool(toolOwnerType);
            var activeOwnerCtx = GetActiveToolContext(toolOwnerType);
            if(!EditorToolUtility.IsBuiltinOverride(activeOwnerTool, activeOwnerCtx))
            {
                RestorePreviousPersistentTool(toolOwnerType);
                return true;
            }

            var state = instance.GetOrCreateStateForType(toolOwnerType);
            if (state != null && ToolManager.GetActiveContextType(toolOwnerType) != state.defaultToolContextType)
            {
                //if is in a Manipulation or additional tool leaves the current context to return to GameObject Context
                //or the default context of the tool owner
                ToolManager.SetActiveContext(state.defaultToolContextType, toolOwnerType);
                return true;
            }

            return false;
        }

        internal static void OnToolGUI(EditorWindow window)
        {
            var state = instance.GetOrCreateStateForType(window.GetType());
            if (state != null)
                state.OnToolGUI(window);
        }

        internal static void InvokeOnSceneGUICustomEditorTools()
        {
            instance.defaultState.InvokeOnSceneGUICustomEditorTools();
            foreach (var state in instance.customStates)
            {
                state.InvokeOnSceneGUICustomEditorTools();
            }
        }

        // Used by tests
        public static T GetComponentContext<T>(bool searchLockedInspectors = false) where T : EditorToolContext
        {
            return GetComponentContext(typeof(T), searchLockedInspectors) as T;
        }

        // Used by tests
        public static EditorToolContext GetComponentContext(Type contextType, bool searchLockedInspectors = false)
        {
            return GetComponentContext(x => x.editorType == contextType && (searchLockedInspectors || !x.lockedInspector));
        }

        public static EditorToolContext GetComponentContext(Type contextType, Type toolOwner, bool searchLockedInspectors = false)
        {
            return GetComponentContext(toolOwner, x => x.editorType == contextType && (searchLockedInspectors || !x.lockedInspector));
        }

        // Used by tests
        internal static EditorToolContext GetComponentContext(Func<ComponentEditor, bool> predicate)
        {
            return GetComponentContext(typeof(SceneView), predicate);
        }

        internal static EditorToolContext GetComponentContext(Type toolOwner, Func<ComponentEditor, bool> predicate)
        {
            var state = instance.GetOrCreateStateForType(toolOwner);
            if (state != null)
            {
                foreach (var ctx in state.componentContexts)
                {
                    if (predicate(ctx))
                        return ctx.GetEditor<EditorToolContext>();
                }
            }

            return null;
        }

        // Used by tests
        public static void GetComponentContexts(Func<ComponentEditor, bool> predicate, List<EditorToolContext> list)
        {
            GetComponentContexts(typeof(SceneView), predicate, list);
        }

        public static void GetComponentContexts(Type toolOwner, Func<ComponentEditor, bool> predicate, List<EditorToolContext> list)
        {
            list.Clear();

            var state = instance.GetOrCreateStateForType(toolOwner);
            if (state != null)
            {
                foreach (var ctx in state.componentContexts)
                {
                    if (predicate(ctx))
                        list.Add(ctx.GetEditor<EditorToolContext>());
                }
            }
        }

        internal static int GetCustomEditorToolsCount(bool includeLockedInspectorTools)
        {
            if (includeLockedInspectorTools)
                return instance.defaultState.componentTools.Count;
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return instance.defaultState.componentTools.Count(x => !x.lockedInspector);
#pragma warning restore UA2001
        }

        // Used by tests.
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static T GetComponentTool<T>(bool searchLockedInspectors = false)
            where T : EditorTool
        {
            return GetComponentTool(typeof(T), typeof(SceneView), searchLockedInspectors) as T;
        }

        internal static T GetComponentTool<T>(Type toolOwner, bool searchLockedInspectors = false)
            where T : EditorTool
        {
            return GetComponentTool(typeof(T), toolOwner, searchLockedInspectors) as T;
        }

        internal static EditorTool GetComponentTool(Type type, Type toolOwner, bool searchLockedInspectors = false)
        {
            return GetComponentTool(x => x.editorType == type, toolOwner, searchLockedInspectors);
        }

        // Get the first component tool matching a predicate.
        internal static EditorTool GetComponentTool(Func<ComponentEditor, bool> predicate, Type toolOwner, bool searchLockedInspectors)
        {
            var ownerState = GetEditorToolStateForOwner(toolOwner);
            if (ownerState != null)
            {
                foreach (var customEditorTool in ownerState.componentTools)
                {
                    if (!searchLockedInspectors && customEditorTool.lockedInspector)
                        continue;

                    if (predicate(customEditorTool) && customEditorTool.editor is EditorTool tool)
                        return tool;
                }
            }

            return null;
        }

        // Collect all instantiated EditorTools for the current selection, not including locked inspectors. This is
        // what should be used to get component tools in 99% of cases. The exception is locked Inspectors, in which
        // case you can use `GetComponentTools(x => x.inspector == editor)`.
        public static void GetComponentToolsForSharedTracker(List<EditorTool> list)
        {
            var ctx = activeToolContext.GetType();

            GetComponentTools(x =>
            {
                var target_ctx = x.typeAssociation.targetContext;
                return (target_ctx == null || target_ctx == ctx) && x.editorToolScope == ComponentEditor.EditorToolScope.ComponentTool;
            }, list, false);
        }

        // Used by tests.
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static void GetComponentTools(List<EditorTool> list, bool searchLockedInspectors)
        {
            GetComponentTools(x => true, list, searchLockedInspectors);
        }

        internal static void GetComponentTools(Func<ComponentEditor, bool> predicate,
            List<EditorTool> list,
            bool searchLockedInspectors = false)
        {
            list.Clear();

            foreach (var customEditorTool in instance.defaultState.componentTools)
            {
                if (!searchLockedInspectors && customEditorTool.lockedInspector)
                    continue;

                if (predicate(customEditorTool) && customEditorTool.editor is EditorTool tool && (tool.IsAvailable() && !tool.isHidden))
                    list.Add(tool);
            }
        }

        // Collect all available tools into applicable UI categories and variant groups
        public static void GetAvailableTools(List<ToolEntry> tools, Type toolOwnerType, EditorToolContext context = null)
        {
            var state = instance.GetOrCreateStateForType(toolOwnerType);
            if (state != null)
                state.GetAvailableTools(tools, context);
        }

        public static void GetAvailableTools(List<ToolEntry> tools, EditorToolContext context = null)
        {
            instance.defaultState.GetAvailableTools(tools, context);
        }

        // Checks the currently instantiated (or available global type) tools for a matching instance.
        internal static bool GetAvailableTool(EditorTypeAssociation typeAssociation, out EditorTool tool)
        {
            return GetAvailableTool(typeAssociation, typeof(SceneView), out tool);
        }

        internal static bool GetAvailableTool(EditorTypeAssociation typeAssociation, Type toolOwnerType, out EditorTool tool)
        {
            tool = null;
            var state = instance.GetOrCreateStateForType(toolOwnerType);
            if (state != null)
                return state.GetAvailableTool(typeAssociation, out tool);
            return false;
        }
    }
}
