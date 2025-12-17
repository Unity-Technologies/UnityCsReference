// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.CSO;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A base tool for graph tools.
    /// </summary>
    [UnityRestricted]
    internal class GraphTool : CsoTool, IHierarchicalCommandTarget
    {
        /// <summary>
        /// Creates and initializes a new <see cref="GraphTool"/>.
        /// </summary>
        /// <param name="windowID">A hash representing the tool's main window.</param>
        /// <typeparam name="TGraphTool">The type of tool to create.</typeparam>
        /// <returns>The newly created tool.</returns>
        public static TGraphTool Create<TGraphTool>(Hash128 windowID)
            where TGraphTool : GraphTool, new()
        {
            return Create<TGraphTool, EditorClipboardProvider>(windowID);
        }

        /// <summary>
        /// Creates and initializes a new <see cref="GraphTool"/>.
        /// </summary>
        /// <param name="windowID">A hash representing the tool's main window.</param>
        /// <typeparam name="TGraphTool">The type of tool to create.</typeparam>
        /// <typeparam name="TClipboardProvider">The type of the clipboard provider to use by the tool.</typeparam>
        /// <returns>The newly created tool.</returns>
        internal static TGraphTool Create<TGraphTool, TClipboardProvider>(Hash128 windowID)
            where TGraphTool : GraphTool, new()
            where TClipboardProvider : ClipboardProvider, new()
        {
            var tool = new TGraphTool();
            tool.WindowID = windowID;
            tool.m_UndoStateRecorder = UndoStateRecorder.GetStateRecorder(tool.GetType());
            tool.Initialize();
            tool.m_ClipboardProvider = new TClipboardProvider();
            return tool;
        }


        /// <summary>
        /// The clipboard service provider, used for copy, paste and duplicate operations.
        /// </summary>
        protected ClipboardProvider m_ClipboardProvider;

        protected Hash128 WindowID { get; private set; }
        protected UndoStateRecorder m_UndoStateRecorder;

        /// <summary>
        /// The name of the tool.
        /// </summary>
        public string Name { get; set; } = "UnnamedTool";

        /// <summary>
        /// The icon of the tool.
        /// </summary>
        public Texture2D Icon { get; set; } = EditorGUIUtilityBridge.LoadIcon($"{GraphElementHelper.k_IconFolder}GraphAsset.png");

        /// <summary>
        /// Whether multiple windows are supported.
        /// </summary>
        protected internal virtual bool SupportsMultipleWindows => true;

        internal bool WantsTransientPrefs { get; set; }

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
        /// The state component that holds the undo state.
        /// </summary>
        public UndoStateComponent UndoState { get; private set; }

        /// <summary>
        /// The state component holding information about modified external assets.
        /// </summary>
        public ExternalAssetsStateComponent ExternalAssetsState { get; private set; }

        /// <summary>
        /// The state component holding information about which variable declarations should be highlighted.
        /// </summary>
        public DeclarationHighlighterStateComponent HighlighterState { get; private set; }

        internal string LastDispatchedCommandName => (Dispatcher as CommandDispatcher)?.LastDispatchedCommandName;

        /// <summary>
        /// Whether when the user closes a graph window without saving, a dialog window appears and prompts them to either save, discard their changes, or cancel.
        /// </summary>
        /// <value>Whether the unsaved changes dialog window is enabled.</value>
        public virtual bool EnableUnsavedChangesDialogWindow => true;

        /// <summary>
        /// The clipboard service provider, used for copy, paste and duplicate operations.
        /// </summary>
        public virtual ClipboardProvider ClipboardProvider => m_ClipboardProvider ??= new EditorClipboardProvider();

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphTool"/> class.
        /// </summary>
        public GraphTool()
        {
        }


        static GraphTool()
        {
            Undo.undoRedoEvent += StaticUndoRedoCallback;
        }

        static void StaticUndoRedoCallback(in UndoRedoInfo undo)
        {
            if (undoRedoEventCallback != null)
            {
                undoRedoEventCallback(in undo);

                UndoStateRecorder.ClearNeedToRestore();
            }

        }

        static Undo.UndoRedoEventCallback undoRedoEventCallback;

        /// <inheritdoc />
        protected override void Initialize()
        {
            CreatePreferences();
            base.Initialize();
            undoRedoEventCallback += UndoRedo;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                undoRedoEventCallback -= UndoRedo;
            }

            CustomPropertyDrawerAdapter.RemoveCommandTarget(this);

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

            ToolState = PersistedState.GetOrCreatePersistedStateComponent<ToolStateComponent>(null, WindowID, Name);
            ToolState.GraphTool = this;
            State.AddStateComponent(ToolState);

            UndoState = new UndoStateComponent(State, ToolState, m_UndoStateRecorder);
            State.AddStateComponent(UndoState);

            ExternalAssetsState = new ExternalAssetsStateComponent();
            State.AddStateComponent(ExternalAssetsState);

            HighlighterState = new DeclarationHighlighterStateComponent();
            State.AddStateComponent(HighlighterState);
        }

        /// <summary>
        /// Creates the <see cref="Preferences"/> object. Derived classes can override this method to create a custom preferences object.
        /// </summary>
        protected virtual void CreatePreferences()
        {
            Preferences = new Preferences(Name, WantsTransientPrefs);
            Preferences.Initialize<BoolPref, IntPref, StringPref>();
        }

        /// <inheritdoc />
        protected override void RegisterCommandHandlers(CommandHandlerRegistrar registrar)
        {
            registrar.AddStateComponent(ToolState);
            registrar.AddStateComponent(UndoState);

            registrar.RegisterDefaultCommandHandler<LoadGraphCommand>();
            registrar.RegisterDefaultCommandHandler<UnloadGraphCommand>();
            registrar.RegisterDefaultCommandHandler<UndoRedoCommand>();
            registrar.RegisterDefaultCommandHandler<BuildAllEditorCommand>();
        }

        /// <inheritdoc />
        public override void Dispatch(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
        {
            if (command is UndoableCommand undoableCommand)
            {
                var undoString = undoableCommand.UndoString ?? "";
                UndoState.BeginOperation(undoString);
            }

            try
            {
                this.DispatchToHierarchy(command, diagnosticsFlags);
            }
            finally
            {
                if (command is UndoableCommand)
                {
                    UndoState.EndOperation();
                }
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

            // This will update the window if the GraphObject has been unloaded, for instance if the file has been changed on disk.
            if (ToolState.CurrentGraph != default && ToolState.GraphModel == null)
            {
                ToolState.ResolveGraphModel();

                if (ToolState.GraphModel != null)
                {
                    Dispatch(new LoadGraphCommand(ToolState.GraphModel, LoadGraphCommand.LoadStrategies.KeepHistory, title: ToolState.CurrentGraphLabel));
                }
                else
                {
                    Dispatch(new UnloadGraphCommand());
                }
            }

            base.Update();

            ExternalAssetsState.Reset();
        }

        void UndoRedo(in UndoRedoInfo info)
        {
            Dispatch(new UndoRedoCommand(info.isRedo));
        }

        /// <summary>
        /// Resolves a <see cref="GraphModel"/> from a <see cref="GraphReference"/>.
        /// </summary>
        /// <param name="reference">The <see cref="GraphReference"/>.</param>
        /// <returns>The <see cref="GraphModel"/> from the <see cref="GraphReference"/>.</returns>
        public virtual GraphModel ResolveGraphModelFromReference(in GraphReference reference)
        {
            return GraphReference.ResolveGraphModel(reference);
        }

        /// <summary>
        /// Creates the <see cref="GraphReference"/> for a <see cref="GraphModel"/>.
        /// </summary>
        /// <param name="graphModel">The <see cref="GraphModel"/>.</param>
        /// <returns>The <see cref="GraphReference"/> for the <see cref="GraphModel"/>.</returns>
        public virtual GraphReference GetGraphModelReference(GraphModel graphModel)
        {
            return new GraphReference(graphModel);
        }
    }
}
