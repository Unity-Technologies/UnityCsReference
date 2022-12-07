// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A base tool for graph tools.
    /// </summary>
    class BaseGraphTool : CsoTool, IHierarchicalCommandTarget
    {
        /// <summary>
        /// Creates and initializes a new <see cref="BaseGraphTool"/>.
        /// </summary>
        /// <param name="windowID">A hash representing the tool's main window.</param>
        /// <typeparam name="T">The type of tool to create.</typeparam>
        /// <returns>The newly created tool.</returns>
        public static T Create<T>(Hash128 windowID) where T : BaseGraphTool, new()
        {
            var tool = new T();
            tool.WindowID = windowID;
            tool.Initialize();
            return tool;
        }

        string m_InstantiationStackTrace;

        protected Dictionary<string, OverlayToolbarProvider> m_ToolbarProviders;

        protected Hash128 WindowID { get; private set; }

        /// <summary>
        /// The name of the tool.
        /// </summary>
        public string Name { get; set; } = "UnnamedTool";

        /// <summary>
        /// The icon of the tool.
        /// </summary>
        public Texture2D Icon { get; set; } = EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_ScriptableObject Icon" : "ScriptableObject Icon").image as Texture2D;

        internal bool WantsTransientPrefs_Internal { get; set; }

        /// <summary>
        /// The tool configuration.
        /// </summary>
        public Preferences Preferences { get; private set; }

        /// <summary>
        /// The parent command target.
        /// </summary>
        public virtual IHierarchicalCommandTarget ParentTarget => null;

        /// <summary>
        /// The state component that holds the tool state.
        /// </summary>
        public ToolStateComponent ToolState { get; private set; }

        /// <summary>
        /// The state component that holds the graph processing state.
        /// </summary>
        public GraphProcessingStateComponent GraphProcessingState { get; private set; }

        /// <summary>
        /// The state component that holds the undo state.
        /// </summary>
        public UndoStateComponent UndoStateComponent { get; private set; }

        /// <summary>
        /// The state component holding information about which variable declarations should be highlighted.
        /// </summary>
        public DeclarationHighlighterStateComponent HighlighterState { get; private set; }

        internal string LastDispatchedCommandName_Internal => (Dispatcher as CommandDispatcher)?.LastDispatchedCommandName_Internal;

        /// <summary>
        /// Whether when the user closes a graph window without saving, a dialogue window appears and prompts them to either save, discard their changes, or cancel.
        /// </summary>
        /// <returns>Whether the unsaved changes dialogue window is enabled.</returns>
        public virtual bool EnableUnsavedChangesDialogueWindow => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseGraphTool"/> class.
        /// </summary>
        public BaseGraphTool()
        {
            m_ToolbarProviders = new Dictionary<string, OverlayToolbarProvider>();
            m_InstantiationStackTrace = Environment.StackTrace;
        }

        ~BaseGraphTool()
        {
            Debug.Assert(
                m_InstantiationStackTrace == null ||
                Undo.undoRedoPerformed.GetInvocationList().Count(d => ReferenceEquals(d.Target, this)) == 0,
                $"Unbalanced Initialize() and Dispose() calls for tool {GetType()} instantiated at {m_InstantiationStackTrace}");
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();
            Undo.undoRedoEvent += UndoRedoPerformed;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            State?.RemoveStateComponent(ToolState);
            if (disposing)
            {
                Undo.undoRedoEvent -= UndoRedoPerformed;
            }
            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override void InitDispatcher()
        {
            Dispatcher = new CommandDispatcher();
        }

        /// <inheritdoc />
        protected override void InitState()
        {
            base.InitState();

            if (WantsTransientPrefs_Internal)
            {
                Preferences = Preferences.CreateTransient_Internal(Name);
            }
            else
            {
                Preferences = Preferences.CreatePreferences(Name);
            }

            ToolState = PersistedState.GetOrCreatePersistedStateComponent<ToolStateComponent>(default, WindowID, Name);
            State.AddStateComponent(ToolState);

            GraphProcessingState = new GraphProcessingStateComponent();
            State.AddStateComponent(GraphProcessingState);

            UndoStateComponent = new UndoStateComponent(State, ToolState);
            State.AddStateComponent(UndoStateComponent);

            HighlighterState = new DeclarationHighlighterStateComponent();
            State.AddStateComponent(HighlighterState);

            this.RegisterCommandHandler<ToolStateComponent, GraphProcessingStateComponent, LoadGraphCommand>(
                LoadGraphCommand.DefaultCommandHandler, ToolState, GraphProcessingState);

            this.RegisterCommandHandler<ToolStateComponent, GraphProcessingStateComponent, UnloadGraphCommand>(
                UnloadGraphCommand.DefaultCommandHandler, ToolState, GraphProcessingState);

            this.RegisterCommandHandler<UndoStateComponent, UndoRedoCommand>(UndoRedoCommand.DefaultCommandHandler, UndoStateComponent);

            this.RegisterCommandHandler<BuildAllEditorCommand>(BuildAllEditorCommand.DefaultCommandHandler);
        }

        /// <inheritdoc />
        public override void Dispatch(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
        {
            if (command is UndoableCommand undoableCommand)
            {
                var undoString = undoableCommand.UndoString ?? "";
                UndoStateComponent.BeginOperation(undoString);
            }

            this.DispatchToHierarchy(command, diagnosticsFlags);

            if (command is UndoableCommand)
            {
                UndoStateComponent.EndOperation();
            }
        }

        /// <inheritdoc />
        public void DispatchToSelf(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
        {
            Dispatcher.Dispatch(command, diagnosticsFlags);
        }

        /// <inheritdoc />
        public override void Update()
        {
            if (Dispatcher is CommandDispatcher commandDispatcher)
            {
                var logAllCommands = Preferences?.GetBool(BoolPref.LogAllDispatchedCommands) ?? false;
                var errorRecursive = Preferences?.GetBool(BoolPref.ErrorOnRecursiveDispatch) ?? false;

                Diagnostics diagnosticFlags = Diagnostics.None;

                if (logAllCommands)
                    diagnosticFlags |= Diagnostics.LogAllCommands;
                if (errorRecursive)
                    diagnosticFlags |= Diagnostics.CheckRecursiveDispatch;

                commandDispatcher.DiagnosticFlags = diagnosticFlags;
            }

            base.Update();
        }

        void UndoRedoPerformed(in UndoRedoInfo info)
        {
            Dispatch(new UndoRedoCommand(info.isRedo));
        }

        protected virtual OverlayToolbarProvider CreateToolbarProvider(string toolbarId)
        {
            switch (toolbarId)
            {
                case MainOverlayToolbar.toolbarId:
                    return new MainToolbarProvider();
                case PanelsToolbar.toolbarId:
                    return new PanelsToolbarProvider();
                case ErrorOverlayToolbar.toolbarId:
                    return new ErrorToolbarProvider();
                case OptionsMenuToolbar.toolbarId:
                    return new OptionsToolbarProvider();
                case BreadcrumbsToolbar.toolbarId:
                    return new BreadcrumbsToolbarProvider();
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the toolbar provider for a toolbar.
        /// </summary>
        /// <param name="toolbar">The toolbar for which to get the provider.</param>
        /// <returns>The toolbar provider for the toolbar.</returns>
        public OverlayToolbarProvider GetToolbarProvider(OverlayToolbar toolbar)
        {
            if (!m_ToolbarProviders.TryGetValue(toolbar.id, out var toolbarProvider))
            {
                toolbarProvider = CreateToolbarProvider(toolbar.id);
                m_ToolbarProviders[toolbar.id] = toolbarProvider;
            }

            return toolbarProvider;
        }
    }
}
